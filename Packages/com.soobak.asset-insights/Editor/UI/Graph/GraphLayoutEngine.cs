using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Soobak.AssetInsights {
  public class GraphLayoutEngine {
    const float NodeWidth = 180f;
    const float NodeHeight = 80f;
    const float HorizontalSpacing = 250f;
    const float VerticalSpacing = 120f;
    const int MaxIterations = 100;
    const float RepulsionStrength = 5000f;
    const float AttractionStrength = 0.01f;
    const float Damping = 0.85f;

    public void ApplyLayout(List<AssetGraphNode> nodes, string centerPath) {
      if (nodes == null || nodes.Count == 0)
        return;

      if (nodes.Count <= 10) {
        ApplyRadialLayout(nodes, centerPath);
      } else {
        ApplyForceDirectedLayout(nodes, centerPath);
      }
    }

    void ApplyRadialLayout(List<AssetGraphNode> nodes, string centerPath) {
      var centerNode = nodes.FirstOrDefault(n => n.AssetPath == centerPath);
      if (centerNode == null && nodes.Count > 0)
        centerNode = nodes[0];

      var centerPos = new Vector2(400, 300);
      centerNode?.SetPosition(new Rect(centerPos.x, centerPos.y, NodeWidth, NodeHeight));

      var otherNodes = nodes.Where(n => n != centerNode).ToList();
      if (otherNodes.Count == 0)
        return;

      var angleStep = 360f / otherNodes.Count;
      var radius = Mathf.Max(200f, otherNodes.Count * 30f);

      for (int i = 0; i < otherNodes.Count; i++) {
        var angle = i * angleStep * Mathf.Deg2Rad;
        var x = centerPos.x + Mathf.Cos(angle) * radius;
        var y = centerPos.y + Mathf.Sin(angle) * radius;
        otherNodes[i].SetPosition(new Rect(x, y, NodeWidth, NodeHeight));
      }
    }

    void ApplyForceDirectedLayout(List<AssetGraphNode> nodes, string centerPath) {
      var positions = new Dictionary<AssetGraphNode, Vector2>();
      var velocities = new Dictionary<AssetGraphNode, Vector2>();

      var centerNode = nodes.FirstOrDefault(n => n.AssetPath == centerPath);
      var centerPos = new Vector2(400, 300);

      foreach (var node in nodes) {
        if (node == centerNode) {
          positions[node] = centerPos;
        } else {
          positions[node] = centerPos + new Vector2(
            Random.Range(-200f, 200f),
            Random.Range(-200f, 200f)
          );
        }
        velocities[node] = Vector2.zero;
      }

      var connections = BuildConnectionMap(nodes);

      for (int iter = 0; iter < MaxIterations; iter++) {
        var forces = new Dictionary<AssetGraphNode, Vector2>();
        foreach (var node in nodes)
          forces[node] = Vector2.zero;

        foreach (var node1 in nodes) {
          foreach (var node2 in nodes) {
            if (node1 == node2)
              continue;

            var diff = positions[node1] - positions[node2];
            var dist = Mathf.Max(diff.magnitude, 1f);
            var force = diff.normalized * (RepulsionStrength / (dist * dist));
            forces[node1] += force;
          }
        }

        foreach (var node in nodes) {
          if (!connections.TryGetValue(node, out var connected))
            continue;

          foreach (var other in connected) {
            var diff = positions[other] - positions[node];
            var dist = diff.magnitude;
            var force = diff.normalized * dist * AttractionStrength;
            forces[node] += force;
          }
        }

        if (centerNode != null) {
          forces[centerNode] = Vector2.zero;
        }

        foreach (var node in nodes) {
          if (node == centerNode)
            continue;

          velocities[node] = (velocities[node] + forces[node]) * Damping;
          positions[node] += velocities[node];
        }
      }

      foreach (var node in nodes) {
        node.SetPosition(new Rect(positions[node].x, positions[node].y, NodeWidth, NodeHeight));
      }
    }

    Dictionary<AssetGraphNode, List<AssetGraphNode>> BuildConnectionMap(List<AssetGraphNode> nodes) {
      var map = new Dictionary<AssetGraphNode, List<AssetGraphNode>>();

      foreach (var node in nodes) {
        map[node] = new List<AssetGraphNode>();

        foreach (var edge in node.OutputPort.connections) {
          if (edge.input.node is AssetGraphNode connected)
            map[node].Add(connected);
        }

        foreach (var edge in node.InputPort.connections) {
          if (edge.output.node is AssetGraphNode connected)
            map[node].Add(connected);
        }
      }

      return map;
    }

    public void ApplyHierarchicalLayout(List<AssetGraphNode> nodes, string rootPath) {
      var levels = new Dictionary<AssetGraphNode, int>();
      var rootNode = nodes.FirstOrDefault(n => n.AssetPath == rootPath);

      if (rootNode == null)
        return;

      AssignLevels(nodes, rootNode, levels, 0);

      var nodesByLevel = nodes.GroupBy(n => levels.GetValueOrDefault(n, 0))
        .OrderBy(g => g.Key)
        .ToList();

      foreach (var group in nodesByLevel) {
        var level = group.Key;
        var levelNodes = group.ToList();
        var startY = -(levelNodes.Count - 1) * VerticalSpacing / 2;

        for (int i = 0; i < levelNodes.Count; i++) {
          var x = level * HorizontalSpacing;
          var y = startY + i * VerticalSpacing + 300;
          levelNodes[i].SetPosition(new Rect(x, y, NodeWidth, NodeHeight));
        }
      }
    }

    void AssignLevels(List<AssetGraphNode> nodes, AssetGraphNode current, Dictionary<AssetGraphNode, int> levels, int level) {
      if (levels.ContainsKey(current))
        return;

      levels[current] = level;

      foreach (var edge in current.OutputPort.connections) {
        if (edge.input.node is AssetGraphNode connected && nodes.Contains(connected))
          AssignLevels(nodes, connected, levels, level + 1);
      }
    }
  }
}
