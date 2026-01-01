using System.Collections.Generic;
using System.Linq;

namespace Soobak.AssetInsights {
  /// <summary>
  /// Detects circular dependencies using Tarjan's strongly connected components algorithm.
  /// </summary>
  public class CircularDependencyDetector {
    readonly DependencyGraph _graph;
    CircularDependencyResult _cachedResult;

    int _index;
    Stack<string> _stack;
    Dictionary<string, int> _indices;
    Dictionary<string, int> _lowLinks;
    HashSet<string> _onStack;
    List<List<string>> _sccs;

    public CircularDependencyDetector(DependencyGraph graph) {
      _graph = graph;
    }

    public void ClearCache() {
      _cachedResult = null;
    }

    public CircularDependencyResult Detect() {
      // Return cached result if available
      if (_cachedResult != null)
        return _cachedResult;

      var result = new CircularDependencyResult();

      _index = 0;
      _stack = new Stack<string>();
      _indices = new Dictionary<string, int>();
      _lowLinks = new Dictionary<string, int>();
      _onStack = new HashSet<string>();
      _sccs = new List<List<string>>();

      // Run Tarjan's algorithm on all nodes
      foreach (var path in _graph.Nodes.Keys) {
        if (!_indices.ContainsKey(path)) {
          StrongConnect(path);
        }
      }

      // Filter to only cycles (SCCs with more than 1 node or self-referencing)
      foreach (var scc in _sccs) {
        if (scc.Count > 1) {
          result.Cycles.Add(new DependencyCycle {
            AssetPaths = scc,
            CycleSize = scc.Count
          });
        } else if (scc.Count == 1) {
          // Check for self-reference
          var path = scc[0];
          if (_graph.GetDependencies(path).Contains(path)) {
            result.Cycles.Add(new DependencyCycle {
              AssetPaths = scc,
              CycleSize = 1,
              IsSelfReference = true
            });
          }
        }
      }

      result.TotalCycles = result.Cycles.Count;
      result.TotalAssetsInCycles = result.Cycles
        .SelectMany(c => c.AssetPaths)
        .Distinct()
        .Count();

      // Build set of all assets in cycles for quick lookup
      result.AssetsInCycles = result.Cycles
        .SelectMany(c => c.AssetPaths)
        .ToHashSet();

      _cachedResult = result;
      return result;
    }

    void StrongConnect(string v) {
      _indices[v] = _index;
      _lowLinks[v] = _index;
      _index++;
      _stack.Push(v);
      _onStack.Add(v);

      foreach (var w in _graph.GetDependencies(v)) {
        if (!_indices.ContainsKey(w)) {
          StrongConnect(w);
          _lowLinks[v] = System.Math.Min(_lowLinks[v], _lowLinks[w]);
        } else if (_onStack.Contains(w)) {
          _lowLinks[v] = System.Math.Min(_lowLinks[v], _indices[w]);
        }
      }

      if (_lowLinks[v] == _indices[v]) {
        var scc = new List<string>();
        string w;
        do {
          w = _stack.Pop();
          _onStack.Remove(w);
          scc.Add(w);
        } while (w != v);

        _sccs.Add(scc);
      }
    }

    public bool IsInCycle(string assetPath) {
      var result = Detect();
      return result.AssetsInCycles.Contains(assetPath);
    }

    public List<string> GetCycleContaining(string assetPath) {
      var result = Detect();
      foreach (var cycle in result.Cycles) {
        if (cycle.AssetPaths.Contains(assetPath))
          return cycle.AssetPaths;
      }
      return null;
    }
  }

  public class CircularDependencyResult {
    public List<DependencyCycle> Cycles { get; set; } = new();
    public int TotalCycles { get; set; }
    public int TotalAssetsInCycles { get; set; }
    public HashSet<string> AssetsInCycles { get; set; } = new();

    public bool HasCycles => TotalCycles > 0;
  }

  public class DependencyCycle {
    public List<string> AssetPaths { get; set; } = new();
    public int CycleSize { get; set; }
    public bool IsSelfReference { get; set; }

    /// <summary>
    /// Returns the cycle path in actual dependency order.
    /// Each element depends on (references) the next element.
    /// </summary>
    public List<string> GetOrderedCyclePath(DependencyGraph graph) {
      if (AssetPaths.Count <= 1)
        return AssetPaths;

      var sccSet = AssetPaths.ToHashSet();
      var visited = new HashSet<string>();
      var path = new List<string>();

      // Start from first node and follow dependencies within SCC
      var current = AssetPaths[0];
      while (!visited.Contains(current)) {
        visited.Add(current);
        path.Add(current);

        // Find next node in cycle (a dependency that's also in the SCC)
        var deps = graph.GetDependencies(current);
        var nextInCycle = deps.FirstOrDefault(d => sccSet.Contains(d) && !visited.Contains(d));

        if (nextInCycle == null) {
          // No unvisited nodes, find the one that closes the cycle
          nextInCycle = deps.FirstOrDefault(d => sccSet.Contains(d));
          if (nextInCycle != null && path.Count < AssetPaths.Count) {
            // Try from a different starting point
            break;
          }
          break;
        }
        current = nextInCycle;
      }

      // If we didn't get all nodes, just return original order
      if (path.Count < AssetPaths.Count)
        return AssetPaths;

      return path;
    }

    public string GetFormattedCycle(DependencyGraph graph) {
      var orderedPaths = GetOrderedCyclePath(graph);
      var names = orderedPaths.Select(p => {
        if (graph.TryGetNode(p, out var node))
          return node.Name;
        return System.IO.Path.GetFileNameWithoutExtension(p);
      }).ToList();

      if (names.Count > 0)
        names.Add(names[0]); // Show cycle back to start

      return string.Join(" → ", names);
    }
  }
}
