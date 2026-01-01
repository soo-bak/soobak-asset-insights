using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace Soobak.AssetInsights {
  public class DependencyGraphView : GraphView {
    readonly DependencyGraph _graph;
    readonly Dictionary<string, AssetGraphNode> _nodeMap = new();
    readonly GraphLayoutEngine _layoutEngine;

    public event Action<string> OnNodeDoubleClicked;
    public event Action<string> OnNodeSelected;

    public DependencyGraphView(DependencyGraph graph) {
      _graph = graph;
      _layoutEngine = new GraphLayoutEngine();

      SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);

      this.AddManipulator(new ContentDragger());
      this.AddManipulator(new SelectionDragger());
      this.AddManipulator(new RectangleSelector());

      var grid = new GridBackground();
      Insert(0, grid);
      grid.StretchToParentSize();

      style.flexGrow = 1;

      var minimap = new MiniMap { anchored = true };
      minimap.SetPosition(new Rect(10, 30, 200, 140));
      Add(minimap);

      graphViewChanged += OnGraphViewChanged;
    }

    GraphViewChange OnGraphViewChanged(GraphViewChange change) {
      return change;
    }

    public void ShowAssetGraph(string centerAssetPath, int depth = 2) {
      ClearGraph();

      if (!_graph.ContainsNode(centerAssetPath))
        return;

      var nodesToShow = new HashSet<string> { centerAssetPath };
      CollectConnectedNodes(centerAssetPath, depth, nodesToShow, true);
      CollectConnectedNodes(centerAssetPath, depth, nodesToShow, false);

      foreach (var path in nodesToShow) {
        if (_graph.TryGetNode(path, out var nodeModel))
          CreateNode(nodeModel, path == centerAssetPath);
      }

      foreach (var path in nodesToShow) {
        CreateEdgesForNode(path, nodesToShow);
      }

      _layoutEngine.ApplyLayout(_nodeMap.Values.ToList(), centerAssetPath);
      FrameAll();
    }

    void CollectConnectedNodes(string path, int depth, HashSet<string> collected, bool forward) {
      if (depth <= 0)
        return;

      var connected = forward
        ? _graph.GetDependencies(path)
        : _graph.GetDependents(path);

      foreach (var dep in connected) {
        if (collected.Add(dep))
          CollectConnectedNodes(dep, depth - 1, collected, forward);
      }
    }

    void CreateNode(AssetNodeModel model, bool isCenter) {
      if (_nodeMap.ContainsKey(model.Path))
        return;

      var node = new AssetGraphNode(model, isCenter);
      node.OnDoubleClicked += () => OnNodeDoubleClicked?.Invoke(model.Path);

      _nodeMap[model.Path] = node;
      AddElement(node);
    }

    void CreateEdgesForNode(string path, HashSet<string> validNodes) {
      if (!_nodeMap.TryGetValue(path, out var fromNode))
        return;

      foreach (var depPath in _graph.GetDependencies(path)) {
        if (!validNodes.Contains(depPath))
          continue;

        if (!_nodeMap.TryGetValue(depPath, out var toNode))
          continue;

        var edge = new DependencyEdge {
          output = fromNode.OutputPort,
          input = toNode.InputPort
        };

        edge.output.Connect(edge);
        edge.input.Connect(edge);
        AddElement(edge);
      }
    }

    public void ClearGraph() {
      foreach (var node in _nodeMap.Values)
        RemoveElement(node);

      foreach (var edge in edges.ToList())
        RemoveElement(edge);

      _nodeMap.Clear();
    }

    public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter) {
      return ports.Where(p =>
        p.direction != startPort.direction &&
        p.node != startPort.node
      ).ToList();
    }

    public void HighlightPath(string fromPath, string toPath) {
      foreach (var edge in edges) {
        edge.RemoveFromClassList("highlighted");
      }

      var path = FindPath(fromPath, toPath);
      if (path == null || path.Count < 2)
        return;

      for (int i = 0; i < path.Count - 1; i++) {
        var from = path[i];
        var to = path[i + 1];

        if (!_nodeMap.TryGetValue(from, out var fromNode))
          continue;

        foreach (var edge in fromNode.OutputPort.connections) {
          if (edge.input.node is AssetGraphNode targetNode &&
              targetNode.AssetPath == to) {
            edge.AddToClassList("highlighted");
          }
        }
      }
    }

    List<string> FindPath(string from, string to) {
      // Use parent dictionary to avoid O(n²) memory from copying lists
      var queue = new Queue<string>();
      var parent = new Dictionary<string, string>();

      queue.Enqueue(from);
      parent[from] = null;

      while (queue.Count > 0) {
        var current = queue.Dequeue();

        if (current == to) {
          // Reconstruct path from parent dictionary
          var path = new List<string>();
          var node = to;
          while (node != null) {
            path.Add(node);
            parent.TryGetValue(node, out node);
          }
          path.Reverse();
          return path;
        }

        foreach (var dep in _graph.GetDependencies(current)) {
          if (!parent.ContainsKey(dep)) {
            parent[dep] = current;
            queue.Enqueue(dep);
          }
        }
      }

      return null;
    }
  }
}
