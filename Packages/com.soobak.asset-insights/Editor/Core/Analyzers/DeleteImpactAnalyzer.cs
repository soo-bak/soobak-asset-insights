using System.Collections.Generic;
using System.Linq;

namespace Soobak.AssetInsights {
  /// <summary>
  /// Analyzes the impact of deleting an asset, showing what would break.
  /// </summary>
  public class DeleteImpactAnalyzer {
    readonly DependencyGraph _graph;

    public DeleteImpactAnalyzer(DependencyGraph graph) {
      _graph = graph;
    }

    public DeleteImpactResult Analyze(string assetPath) {
      var result = new DeleteImpactResult { TargetPath = assetPath };

      if (!_graph.TryGetNode(assetPath, out var targetNode)) {
        result.IsValid = false;
        return result;
      }

      result.IsValid = true;
      result.TargetName = targetNode.Name;
      result.TargetType = targetNode.Type;
      result.TargetSize = targetNode.SizeBytes;

      // Find direct dependents (assets that would break)
      var directDependents = _graph.GetDependents(assetPath);
      foreach (var depPath in directDependents) {
        if (_graph.TryGetNode(depPath, out var depNode)) {
          result.DirectlyAffected.Add(new AffectedAsset {
            Path = depPath,
            Name = depNode.Name,
            Type = depNode.Type,
            Impact = ImpactLevel.Broken
          });
        }
      }

      // Find cascade effect (assets that might become orphaned)
      var orphanCandidates = FindOrphanCandidates(assetPath);
      foreach (var path in orphanCandidates) {
        if (_graph.TryGetNode(path, out var node)) {
          result.CascadeAffected.Add(new AffectedAsset {
            Path = path,
            Name = node.Name,
            Type = node.Type,
            Impact = ImpactLevel.PotentiallyOrphaned
          });
        }
      }

      // Check if this affects any scenes
      foreach (var affected in result.DirectlyAffected) {
        if (affected.Type == AssetType.Scene) {
          result.AffectedScenes.Add(affected.Path);
        }
      }

      // Collect all assets in the target's dependency tree that would also be deletable
      var dependenciesOnly = FindExclusiveDependencies(assetPath);
      foreach (var path in dependenciesOnly) {
        if (_graph.TryGetNode(path, out var node)) {
          result.SafeToDeleteTogether.Add(new AffectedAsset {
            Path = path,
            Name = node.Name,
            Type = node.Type,
            SizeBytes = node.SizeBytes,
            Impact = ImpactLevel.None
          });
        }
      }

      result.TotalAffectedCount = result.DirectlyAffected.Count + result.CascadeAffected.Count;
      result.TotalDeletableSize = result.TargetSize +
        result.SafeToDeleteTogether.Sum(a => a.SizeBytes);

      result.IsSafeToDelete = result.DirectlyAffected.Count == 0;

      return result;
    }

    HashSet<string> FindOrphanCandidates(string deletedPath) {
      var orphans = new HashSet<string>();
      var dependencies = _graph.GetDependencies(deletedPath);

      foreach (var dep in dependencies) {
        var dependents = _graph.GetDependents(dep);
        // If this asset only has the deleted asset as a dependent, it becomes orphaned
        if (dependents.Count == 1 && dependents.Contains(deletedPath)) {
          orphans.Add(dep);
        }
      }

      return orphans;
    }

    HashSet<string> FindExclusiveDependencies(string assetPath) {
      var exclusive = new HashSet<string>();
      var allDeps = _graph.GetAllDependencies(assetPath);

      foreach (var dep in allDeps) {
        var dependents = _graph.GetDependents(dep);
        // Check if all dependents are in the deletion scope
        var isExclusive = dependents.All(d => d == assetPath || allDeps.Contains(d));
        if (isExclusive) {
          exclusive.Add(dep);
        }
      }

      return exclusive;
    }

    public DeleteImpactResult AnalyzeMultiple(IEnumerable<string> assetPaths) {
      var result = new DeleteImpactResult { TargetPath = string.Join(", ", assetPaths.Take(3)) + "..." };
      result.IsValid = true;

      var pathSet = assetPaths.ToHashSet();
      var allAffected = new HashSet<string>();

      foreach (var path in pathSet) {
        var dependents = _graph.GetDependents(path);
        foreach (var dep in dependents) {
          if (!pathSet.Contains(dep)) {
            allAffected.Add(dep);
          }
        }
      }

      foreach (var path in allAffected) {
        if (_graph.TryGetNode(path, out var node)) {
          result.DirectlyAffected.Add(new AffectedAsset {
            Path = path,
            Name = node.Name,
            Type = node.Type,
            Impact = ImpactLevel.Broken
          });
        }
      }

      result.TotalAffectedCount = result.DirectlyAffected.Count;
      result.IsSafeToDelete = result.DirectlyAffected.Count == 0;

      return result;
    }
  }

  public class DeleteImpactResult {
    public string TargetPath { get; set; }
    public string TargetName { get; set; }
    public AssetType TargetType { get; set; }
    public long TargetSize { get; set; }
    public bool IsValid { get; set; }
    public bool IsSafeToDelete { get; set; }

    public List<AffectedAsset> DirectlyAffected { get; set; } = new();
    public List<AffectedAsset> CascadeAffected { get; set; } = new();
    public List<AffectedAsset> SafeToDeleteTogether { get; set; } = new();
    public List<string> AffectedScenes { get; set; } = new();

    public int TotalAffectedCount { get; set; }
    public long TotalDeletableSize { get; set; }

    public string FormattedTargetSize => AssetNodeModel.FormatBytes(TargetSize);
    public string FormattedDeletableSize => AssetNodeModel.FormatBytes(TotalDeletableSize);
  }

  public class AffectedAsset {
    public string Path { get; set; }
    public string Name { get; set; }
    public AssetType Type { get; set; }
    public long SizeBytes { get; set; }
    public ImpactLevel Impact { get; set; }

    public string FormattedSize => AssetNodeModel.FormatBytes(SizeBytes);
  }

  public enum ImpactLevel {
    None,
    PotentiallyOrphaned,
    Broken
  }
}
