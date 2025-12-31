using System.Collections.Generic;

namespace Soobak.AssetInsights {
  public static class PathFinder {
    public static List<AssetNodeModel> FindShortestPath(
      DependencyGraph graph,
      string from,
      string to) {
      if (from == to) {
        if (graph.TryGetNode(from, out var node))
          return new List<AssetNodeModel> { node };
        return null;
      }

      var visited = new HashSet<string>();
      var parent = new Dictionary<string, string>();
      var queue = new Queue<string>();

      queue.Enqueue(from);
      visited.Add(from);
      parent[from] = null;

      while (queue.Count > 0) {
        var current = queue.Dequeue();

        if (current == to)
          return ReconstructPath(graph, parent, to);

        foreach (var dep in graph.GetDependencies(current)) {
          if (visited.Add(dep)) {
            parent[dep] = current;
            queue.Enqueue(dep);
          }
        }
      }

      return null;
    }

    public static Dictionary<string, List<AssetNodeModel>> FindWhyIncluded(
      DependencyGraph graph,
      IEnumerable<string> roots,
      string target) {
      var result = new Dictionary<string, List<AssetNodeModel>>();

      foreach (var root in roots) {
        var path = FindShortestPath(graph, root, target);
        if (path != null && path.Count > 0)
          result[root] = path;
      }

      return result;
    }

    public static List<AssetNodeModel> FindReversePath(
      DependencyGraph graph,
      string target,
      string root) {
      if (target == root) {
        if (graph.TryGetNode(target, out var node))
          return new List<AssetNodeModel> { node };
        return null;
      }

      var visited = new HashSet<string>();
      var parent = new Dictionary<string, string>();
      var queue = new Queue<string>();

      queue.Enqueue(target);
      visited.Add(target);
      parent[target] = null;

      while (queue.Count > 0) {
        var current = queue.Dequeue();

        if (current == root)
          return ReconstructPath(graph, parent, root);

        foreach (var dep in graph.GetDependents(current)) {
          if (visited.Add(dep)) {
            parent[dep] = current;
            queue.Enqueue(dep);
          }
        }
      }

      return null;
    }

    static List<AssetNodeModel> ReconstructPath(
      DependencyGraph graph,
      Dictionary<string, string> parent,
      string target) {
      var path = new List<AssetNodeModel>();
      var current = target;

      while (current != null) {
        if (graph.TryGetNode(current, out var node))
          path.Add(node);
        current = parent.TryGetValue(current, out var p) ? p : null;
      }

      path.Reverse();
      return path;
    }
  }
}
