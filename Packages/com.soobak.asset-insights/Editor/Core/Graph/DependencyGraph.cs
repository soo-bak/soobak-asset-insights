using System;
using System.Collections.Generic;
using System.Linq;

namespace Soobak.AssetInsights {
  public sealed class DependencyGraph {
    readonly Dictionary<string, AssetNodeModel> _nodes = new();
    readonly Dictionary<string, HashSet<string>> _forward = new();
    readonly Dictionary<string, HashSet<string>> _reverse = new();

    public IReadOnlyDictionary<string, AssetNodeModel> Nodes => _nodes;
    public int NodeCount => _nodes.Count;
    public int EdgeCount => _forward.Values.Sum(e => e.Count);

    public void AddNode(AssetNodeModel node) {
      if (node == null)
        throw new ArgumentNullException(nameof(node));

      if (_nodes.ContainsKey(node.Path))
        return;

      _nodes[node.Path] = node;
      _forward[node.Path] = new HashSet<string>();
      _reverse[node.Path] = new HashSet<string>();
    }

    public void AddEdge(string from, string to) {
      if (string.IsNullOrEmpty(from))
        throw new ArgumentNullException(nameof(from));
      if (string.IsNullOrEmpty(to))
        throw new ArgumentNullException(nameof(to));
      if (from == to)
        return;
      if (!_nodes.ContainsKey(from))
        throw new InvalidOperationException($"Node not found: {from}");
      if (!_nodes.ContainsKey(to))
        throw new InvalidOperationException($"Node not found: {to}");

      _forward[from].Add(to);
      _reverse[to].Add(from);
    }

    public bool TryGetNode(string path, out AssetNodeModel node) {
      return _nodes.TryGetValue(path, out node);
    }

    public bool ContainsNode(string path) {
      return _nodes.ContainsKey(path);
    }

    public IReadOnlyCollection<string> GetDependencies(string path) {
      if (_forward.TryGetValue(path, out var deps))
        return deps;
      return Array.Empty<string>();
    }

    public IReadOnlyCollection<string> GetDependents(string path) {
      if (_reverse.TryGetValue(path, out var deps))
        return deps;
      return Array.Empty<string>();
    }

    public HashSet<string> GetAllDependencies(string path) {
      var result = new HashSet<string>();
      var queue = new Queue<string>();
      queue.Enqueue(path);

      while (queue.Count > 0) {
        var current = queue.Dequeue();
        foreach (var dep in GetDependencies(current)) {
          if (result.Add(dep))
            queue.Enqueue(dep);
        }
      }

      return result;
    }

    public List<AssetNodeModel> GetNodesBySize(int topN = int.MaxValue) {
      return _nodes.Values
        .OrderByDescending(n => n.SizeBytes)
        .Take(topN)
        .ToList();
    }

    public Dictionary<AssetType, (long totalSize, int count)> GetSizeByType() {
      return _nodes.Values
        .GroupBy(n => n.Type)
        .ToDictionary(
          g => g.Key,
          g => (totalSize: g.Sum(n => n.SizeBytes), count: g.Count())
        );
    }

    public long GetTotalSize() {
      return _nodes.Values.Sum(n => n.SizeBytes);
    }

    public void Clear() {
      _nodes.Clear();
      _forward.Clear();
      _reverse.Clear();
    }
  }
}
