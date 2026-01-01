using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace Soobak.AssetInsights {
  /// <summary>
  /// Analyzes whether assets will be included in the final build.
  /// </summary>
  public class BuildInclusionAnalyzer {
    readonly DependencyGraph _graph;
    HashSet<string> _buildIncludedAssets;
    HashSet<string> _resourcesAssets;
    HashSet<string> _streamingAssets;
    HashSet<string> _editorOnlyAssets;

    public BuildInclusionAnalyzer(DependencyGraph graph) {
      _graph = graph;
    }

    public BuildInclusionResult Analyze() {
      _buildIncludedAssets = new HashSet<string>();
      _resourcesAssets = new HashSet<string>();
      _streamingAssets = new HashSet<string>();
      _editorOnlyAssets = new HashSet<string>();

      // Get all scene paths in build settings
      var buildScenes = EditorBuildSettings.scenes
        .Where(s => s.enabled)
        .Select(s => s.path)
        .ToList();

      // Traverse from build scenes
      foreach (var scenePath in buildScenes) {
        TraverseDependencies(scenePath);
      }

      // Add Resources folder contents
      foreach (var node in _graph.Nodes.Values) {
        if (IsInResourcesFolder(node.Path)) {
          _resourcesAssets.Add(node.Path);
          TraverseDependencies(node.Path);
        }
      }

      // Check StreamingAssets
      foreach (var node in _graph.Nodes.Values) {
        if (node.Path.StartsWith("Assets/StreamingAssets/")) {
          _streamingAssets.Add(node.Path);
          _buildIncludedAssets.Add(node.Path);
        }
      }

      // Identify editor-only assets
      foreach (var node in _graph.Nodes.Values) {
        if (IsEditorOnly(node.Path)) {
          _editorOnlyAssets.Add(node.Path);
        }
      }

      return new BuildInclusionResult {
        IncludedAssets = _buildIncludedAssets,
        ResourcesAssets = _resourcesAssets,
        StreamingAssets = _streamingAssets,
        EditorOnlyAssets = _editorOnlyAssets,
        BuildScenes = buildScenes.ToHashSet(),
        TotalIncludedCount = _buildIncludedAssets.Count,
        TotalIncludedSize = _buildIncludedAssets
          .Where(p => _graph.TryGetNode(p, out _))
          .Sum(p => _graph.Nodes[p].SizeBytes)
      };
    }

    void TraverseDependencies(string assetPath) {
      if (_buildIncludedAssets.Contains(assetPath))
        return;

      if (IsEditorOnly(assetPath))
        return;

      _buildIncludedAssets.Add(assetPath);

      var deps = _graph.GetDependencies(assetPath);
      foreach (var dep in deps) {
        TraverseDependencies(dep);
      }
    }

    bool IsInResourcesFolder(string path) {
      return path.Contains("/Resources/");
    }

    bool IsEditorOnly(string path) {
      // Editor folders
      if (path.Contains("/Editor/"))
        return true;

      // Editor-only scripts
      if (path.EndsWith(".cs")) {
        var importer = AssetImporter.GetAtPath(path);
        if (importer is PluginImporter pluginImporter) {
          return !pluginImporter.GetCompatibleWithAnyPlatform();
        }
      }

      // Editor assembly definitions
      if (path.EndsWith(".asmdef")) {
        var asmdef = AssetDatabase.LoadAssetAtPath<UnityEditorInternal.AssemblyDefinitionAsset>(path);
        if (asmdef != null) {
          var json = asmdef.text;
          if (json.Contains("\"Editor\"") && json.Contains("\"includePlatforms\""))
            return true;
        }
      }

      return false;
    }

    public BuildInclusionStatus GetStatus(string assetPath) {
      if (_buildIncludedAssets == null)
        Analyze();

      if (_editorOnlyAssets.Contains(assetPath))
        return BuildInclusionStatus.EditorOnly;

      if (_streamingAssets.Contains(assetPath))
        return BuildInclusionStatus.StreamingAssets;

      if (_resourcesAssets.Contains(assetPath))
        return BuildInclusionStatus.Resources;

      if (_buildIncludedAssets.Contains(assetPath))
        return BuildInclusionStatus.IncludedInBuild;

      return BuildInclusionStatus.NotIncluded;
    }
  }

  public class BuildInclusionResult {
    public HashSet<string> IncludedAssets { get; set; } = new();
    public HashSet<string> ResourcesAssets { get; set; } = new();
    public HashSet<string> StreamingAssets { get; set; } = new();
    public HashSet<string> EditorOnlyAssets { get; set; } = new();
    public HashSet<string> BuildScenes { get; set; } = new();
    public int TotalIncludedCount { get; set; }
    public long TotalIncludedSize { get; set; }

    public string FormattedSize => AssetNodeModel.FormatBytes(TotalIncludedSize);
  }

  public enum BuildInclusionStatus {
    NotIncluded,
    IncludedInBuild,
    Resources,
    StreamingAssets,
    EditorOnly
  }
}
