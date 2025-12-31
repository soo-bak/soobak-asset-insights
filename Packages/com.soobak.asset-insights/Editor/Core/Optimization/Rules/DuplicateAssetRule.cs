using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using UnityEditor;

namespace Soobak.AssetInsights {
  public class DuplicateAssetRule : IOptimizationRule {
    static readonly Dictionary<string, List<string>> _hashCache = new();
    static bool _cacheBuilt;

    public string RuleName => "Duplicate Asset";
    public string Description => "Detects duplicate assets with same content";
    public OptimizationSeverity Severity => OptimizationSeverity.Warning;

    public IEnumerable<OptimizationIssue> Evaluate(AssetNodeModel node, DependencyGraph graph) {
      // Only check for certain asset types that commonly get duplicated
      if (node.Type != AssetType.Texture &&
          node.Type != AssetType.Audio &&
          node.Type != AssetType.Model)
        yield break;

      // Build cache once for the entire graph
      if (!_cacheBuilt) {
        BuildHashCache(graph);
        _cacheBuilt = true;
      }

      var hash = ComputeFileHash(node.Path);
      if (string.IsNullOrEmpty(hash))
        yield break;

      if (_hashCache.TryGetValue(hash, out var duplicates) && duplicates.Count > 1) {
        // Only report on the first occurrence to avoid duplicate issues
        if (duplicates[0] != node.Path)
          yield break;

        var otherPaths = duplicates.FindAll(p => p != node.Path);

        yield return new OptimizationIssue {
          RuleName = RuleName,
          AssetPath = node.Path,
          AssetName = node.Name,
          Severity = Severity,
          Message = $"Duplicate of {otherPaths.Count} other file(s)",
          Recommendation = $"Consolidate with: {string.Join(", ", otherPaths)}",
          PotentialSavings = node.SizeBytes * otherPaths.Count,
          IsAutoFixable = false
        };
      }
    }

    void BuildHashCache(DependencyGraph graph) {
      _hashCache.Clear();

      foreach (var node in graph.Nodes.Values) {
        if (node.Type != AssetType.Texture &&
            node.Type != AssetType.Audio &&
            node.Type != AssetType.Model)
          continue;

        var hash = ComputeFileHash(node.Path);
        if (string.IsNullOrEmpty(hash))
          continue;

        if (!_hashCache.ContainsKey(hash))
          _hashCache[hash] = new List<string>();

        _hashCache[hash].Add(node.Path);
      }
    }

    static string ComputeFileHash(string assetPath) {
      var fullPath = Path.GetFullPath(assetPath);
      if (!File.Exists(fullPath))
        return null;

      try {
        using var stream = File.OpenRead(fullPath);
        using var md5 = MD5.Create();
        var hash = md5.ComputeHash(stream);
        return System.BitConverter.ToString(hash).Replace("-", "");
      } catch {
        return null;
      }
    }

    public static void ClearCache() {
      _hashCache.Clear();
      _cacheBuilt = false;
    }
  }
}
