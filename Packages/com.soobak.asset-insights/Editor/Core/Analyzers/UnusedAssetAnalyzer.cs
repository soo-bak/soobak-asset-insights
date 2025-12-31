using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace Soobak.AssetInsights {
  public class UnusedAssetAnalyzer {
    readonly DependencyGraph _graph;

    public UnusedAssetAnalyzer(DependencyGraph graph) {
      _graph = graph;
    }

    public UnusedAssetResult Analyze() {
      var result = new UnusedAssetResult();

      var usedAssets = CollectUsedAssets();

      foreach (var node in _graph.Nodes.Values) {
        if (ShouldSkip(node.Path))
          continue;

        if (!usedAssets.Contains(node.Path)) {
          var category = CategorizeUnused(node.Path);
          result.UnusedAssets.Add(new UnusedAssetInfo {
            Path = node.Path,
            Name = node.Name,
            Type = node.Type,
            SizeBytes = node.SizeBytes,
            Category = category
          });
        }
      }

      result.UnusedAssets = result.UnusedAssets
        .OrderByDescending(a => a.SizeBytes)
        .ToList();

      result.TotalUnusedSize = result.UnusedAssets.Sum(a => a.SizeBytes);
      result.TotalUnusedCount = result.UnusedAssets.Count;
      result.TotalAssetCount = _graph.NodeCount;

      return result;
    }

    HashSet<string> CollectUsedAssets() {
      var used = new HashSet<string>();

      // Collect from build scenes
      var scenePaths = EditorBuildSettings.scenes
        .Where(s => s.enabled)
        .Select(s => s.path)
        .ToList();

      foreach (var scenePath in scenePaths) {
        if (!string.IsNullOrEmpty(scenePath))
          CollectDependenciesRecursive(scenePath, used);
      }

      // Collect from Resources folders
      var resourceAssets = AssetDatabase.FindAssets("", new[] { "Assets" })
        .Select(AssetDatabase.GUIDToAssetPath)
        .Where(p => p.Contains("/Resources/"))
        .ToList();

      foreach (var path in resourceAssets) {
        used.Add(path);
        CollectDependenciesRecursive(path, used);
      }

      // Collect from StreamingAssets
      var streamingAssets = AssetDatabase.FindAssets("", new[] { "Assets/StreamingAssets" })
        .Select(AssetDatabase.GUIDToAssetPath)
        .ToList();

      foreach (var path in streamingAssets) {
        used.Add(path);
      }

      return used;
    }

    void CollectDependenciesRecursive(string path, HashSet<string> collected) {
      if (!collected.Add(path))
        return;

      foreach (var dep in _graph.GetDependencies(path)) {
        CollectDependenciesRecursive(dep, collected);
      }
    }

    bool ShouldSkip(string path) {
      // Skip editor-only paths
      if (path.Contains("/Editor/"))
        return true;

      // Skip packages
      if (path.StartsWith("Packages/"))
        return true;

      // Skip certain file types that are often false positives
      if (path.EndsWith(".cs") || path.EndsWith(".asmdef") || path.EndsWith(".asmref"))
        return true;

      return false;
    }

    UnusedCategory CategorizeUnused(string path) {
      if (path.Contains("/Resources/"))
        return UnusedCategory.InResources;

      if (path.Contains("/Plugins/"))
        return UnusedCategory.InPlugins;

      if (path.Contains("/ThirdParty/") || path.Contains("/External/"))
        return UnusedCategory.ThirdParty;

      return UnusedCategory.ProjectAsset;
    }
  }

  public class UnusedAssetResult {
    public List<UnusedAssetInfo> UnusedAssets { get; set; } = new();
    public long TotalUnusedSize { get; set; }
    public int TotalUnusedCount { get; set; }
    public int TotalAssetCount { get; set; }

    public float UnusedPercentage =>
      TotalAssetCount > 0 ? (float)TotalUnusedCount / TotalAssetCount * 100f : 0f;
  }

  public class UnusedAssetInfo {
    public string Path { get; set; }
    public string Name { get; set; }
    public AssetType Type { get; set; }
    public long SizeBytes { get; set; }
    public UnusedCategory Category { get; set; }

    public string FormattedSize => AssetNodeModel.FormatBytes(SizeBytes);
  }

  public enum UnusedCategory {
    ProjectAsset,
    InResources,
    InPlugins,
    ThirdParty
  }
}
