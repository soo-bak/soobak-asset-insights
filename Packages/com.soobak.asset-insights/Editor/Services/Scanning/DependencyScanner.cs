using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;

namespace Soobak.AssetInsights {
  public class DependencyScanner : IDependencyScanner {
    readonly DependencyGraph _graph;
    readonly ScanProgress _progress;

    public DependencyGraph Graph => _graph;
    public IScanProgress Progress => _progress;

    public DependencyScanner() {
      _graph = new DependencyGraph();
      _progress = new ScanProgress();
    }

    public DependencyScanner(DependencyGraph graph) {
      _graph = graph;
      _progress = new ScanProgress();
    }

    public IEnumerator ScanAsync(ScanOptions options = null) {
      options = options ?? ScanOptions.Default;
      _progress.Reset();

      var allAssets = AssetDatabase.GetAllAssetPaths();
      var validAssets = FilterAssets(allAssets, options);

      _progress.SetTotal(validAssets.Count);

      foreach (var path in validAssets) {
        if (_progress.IsCancelled)
          yield break;

        ProcessAsset(path);
        _progress.Increment();

        yield return null;
      }
    }

    public void ScanImmediate(ScanOptions options = null) {
      options = options ?? ScanOptions.Default;
      _progress.Reset();

      var allAssets = AssetDatabase.GetAllAssetPaths();
      var validAssets = FilterAssets(allAssets, options);

      _progress.SetTotal(validAssets.Count);

      foreach (var path in validAssets) {
        ProcessAsset(path);
        _progress.Increment();
      }
    }

    public void Clear() {
      _graph.Clear();
      _progress.Reset();
    }

    List<string> FilterAssets(string[] assets, ScanOptions options) {
      var result = new List<string>();

      foreach (var path in assets) {
        if (string.IsNullOrEmpty(path))
          continue;

        if (AssetDatabase.IsValidFolder(path))
          continue;

        if (!options.IncludePackages && path.StartsWith("Packages/"))
          continue;

        if (path.StartsWith("ProjectSettings/"))
          continue;

        result.Add(path);
      }

      return result;
    }

    void ProcessAsset(string path) {
      _progress.Report(_progress.Progress, path);

      var node = CreateNode(path);
      _graph.AddNode(node);

      var dependencies = AssetDatabase.GetDependencies(path, false);
      foreach (var depPath in dependencies) {
        if (depPath == path)
          continue;

        if (!_graph.ContainsNode(depPath)) {
          var depNode = CreateNode(depPath);
          _graph.AddNode(depNode);
        }

        _graph.AddEdge(path, depPath);
      }
    }

    AssetNodeModel CreateNode(string path) {
      long fileSize = 0;
      var fullPath = Path.GetFullPath(path);
      if (File.Exists(fullPath))
        fileSize = new FileInfo(fullPath).Length;

      return new AssetNodeModel(path, fileSize);
    }
  }
}
