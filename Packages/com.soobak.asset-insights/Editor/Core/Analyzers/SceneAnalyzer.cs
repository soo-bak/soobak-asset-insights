using System.Collections.Generic;
using System.Linq;

namespace Soobak.AssetInsights {
  public class SceneAnalyzer {
    readonly DependencyGraph _graph;

    public SceneAnalyzer(DependencyGraph graph) {
      _graph = graph;
    }

    public SceneAnalysisResult Analyze(string scenePath) {
      var result = new SceneAnalysisResult { ScenePath = scenePath };

      if (!_graph.TryGetNode(scenePath, out var sceneNode))
        return result;

      result.SceneName = sceneNode.Name;

      var allDeps = _graph.GetAllDependencies(scenePath);

      foreach (var depPath in allDeps) {
        if (_graph.TryGetNode(depPath, out var node)) {
          result.Dependencies.Add(node);
        }
      }

      // Calculate breakdown by type
      var byType = result.Dependencies
        .GroupBy(n => n.Type)
        .ToDictionary(
          g => g.Key,
          g => new TypeBreakdown {
            Type = g.Key,
            Count = g.Count(),
            TotalSize = g.Sum(n => n.SizeBytes)
          }
        );

      result.TypeBreakdown = byType;
      result.TotalSize = result.Dependencies.Sum(n => n.SizeBytes);
      result.TotalCount = result.Dependencies.Count;

      return result;
    }

    public List<string> FindInclusionPath(string fromPath, string toPath) {
      if (!_graph.ContainsNode(fromPath) || !_graph.ContainsNode(toPath))
        return null;

      var queue = new Queue<List<string>>();
      var visited = new HashSet<string>();

      queue.Enqueue(new List<string> { fromPath });
      visited.Add(fromPath);

      while (queue.Count > 0) {
        var path = queue.Dequeue();
        var current = path[^1];

        if (current == toPath)
          return path;

        foreach (var dep in _graph.GetDependencies(current)) {
          if (visited.Add(dep)) {
            var newPath = new List<string>(path) { dep };
            queue.Enqueue(newPath);
          }
        }
      }

      return null;
    }

    public string FormatInclusionPath(List<string> path) {
      if (path == null || path.Count == 0)
        return "No path found";

      var names = path.Select(p => {
        if (_graph.TryGetNode(p, out var node))
          return node.Name;
        return System.IO.Path.GetFileNameWithoutExtension(p);
      });

      return string.Join(" -> ", names);
    }
  }

  public class SceneAnalysisResult {
    public string ScenePath { get; set; }
    public string SceneName { get; set; }
    public List<AssetNodeModel> Dependencies { get; set; } = new();
    public Dictionary<AssetType, TypeBreakdown> TypeBreakdown { get; set; } = new();
    public long TotalSize { get; set; }
    public int TotalCount { get; set; }

    public string FormattedSize => AssetNodeModel.FormatBytes(TotalSize);

    public IEnumerable<TypeBreakdown> GetTopTypes(int count = 5) {
      return TypeBreakdown.Values
        .OrderByDescending(t => t.TotalSize)
        .Take(count);
    }
  }

  public class TypeBreakdown {
    public AssetType Type { get; set; }
    public int Count { get; set; }
    public long TotalSize { get; set; }

    public string FormattedSize => AssetNodeModel.FormatBytes(TotalSize);

    public float GetPercentage(long totalSize) {
      return totalSize > 0 ? (float)TotalSize / totalSize * 100f : 0f;
    }
  }
}
