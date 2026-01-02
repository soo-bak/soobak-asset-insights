using System.Linq;
using NUnit.Framework;

namespace Soobak.AssetInsights.Tests {
  [TestFixture]
  public class CircularDependencyDetectorTests {
    DependencyGraph _graph;
    CircularDependencyDetector _detector;

    [SetUp]
    public void SetUp() {
      _graph = new DependencyGraph();
      _detector = new CircularDependencyDetector(_graph);
    }

    [Test]
    public void Detect_NoCycles_ReturnsEmptyResult() {
      _graph.AddNode(new AssetNodeModel("Assets/a.prefab", 100));
      _graph.AddNode(new AssetNodeModel("Assets/b.mat", 50));
      _graph.AddEdge("Assets/a.prefab", "Assets/b.mat");

      var result = _detector.Detect();

      Assert.IsFalse(result.HasCycles);
      Assert.AreEqual(0, result.TotalCycles);
    }

    [Test]
    public void Detect_SimpleCycle_DetectsCycle() {
      _graph.AddNode(new AssetNodeModel("Assets/a.prefab", 100));
      _graph.AddNode(new AssetNodeModel("Assets/b.mat", 50));
      _graph.AddEdge("Assets/a.prefab", "Assets/b.mat");
      _graph.AddEdge("Assets/b.mat", "Assets/a.prefab");

      var result = _detector.Detect();

      Assert.IsTrue(result.HasCycles);
      Assert.AreEqual(1, result.TotalCycles);
      Assert.AreEqual(2, result.TotalAssetsInCycles);
    }

    [Test]
    public void Detect_ThreeNodeCycle_DetectsCycle() {
      _graph.AddNode(new AssetNodeModel("Assets/a.prefab", 100));
      _graph.AddNode(new AssetNodeModel("Assets/b.mat", 50));
      _graph.AddNode(new AssetNodeModel("Assets/c.png", 200));
      _graph.AddEdge("Assets/a.prefab", "Assets/b.mat");
      _graph.AddEdge("Assets/b.mat", "Assets/c.png");
      _graph.AddEdge("Assets/c.png", "Assets/a.prefab");

      var result = _detector.Detect();

      Assert.IsTrue(result.HasCycles);
      Assert.AreEqual(1, result.TotalCycles);
      Assert.AreEqual(3, result.TotalAssetsInCycles);
    }

    [Test]
    public void Detect_MultipleCycles_DetectsAll() {
      // Cycle 1: A -> B -> A
      _graph.AddNode(new AssetNodeModel("Assets/a.prefab", 100));
      _graph.AddNode(new AssetNodeModel("Assets/b.mat", 50));
      _graph.AddEdge("Assets/a.prefab", "Assets/b.mat");
      _graph.AddEdge("Assets/b.mat", "Assets/a.prefab");

      // Cycle 2: C -> D -> C (separate)
      _graph.AddNode(new AssetNodeModel("Assets/c.prefab", 100));
      _graph.AddNode(new AssetNodeModel("Assets/d.mat", 50));
      _graph.AddEdge("Assets/c.prefab", "Assets/d.mat");
      _graph.AddEdge("Assets/d.mat", "Assets/c.prefab");

      var result = _detector.Detect();

      Assert.IsTrue(result.HasCycles);
      Assert.AreEqual(2, result.TotalCycles);
      Assert.AreEqual(4, result.TotalAssetsInCycles);
    }

    [Test]
    public void IsInCycle_AssetInCycle_ReturnsTrue() {
      _graph.AddNode(new AssetNodeModel("Assets/a.prefab", 100));
      _graph.AddNode(new AssetNodeModel("Assets/b.mat", 50));
      _graph.AddEdge("Assets/a.prefab", "Assets/b.mat");
      _graph.AddEdge("Assets/b.mat", "Assets/a.prefab");

      Assert.IsTrue(_detector.IsInCycle("Assets/a.prefab"));
      Assert.IsTrue(_detector.IsInCycle("Assets/b.mat"));
    }

    [Test]
    public void IsInCycle_AssetNotInCycle_ReturnsFalse() {
      _graph.AddNode(new AssetNodeModel("Assets/a.prefab", 100));
      _graph.AddNode(new AssetNodeModel("Assets/b.mat", 50));
      _graph.AddEdge("Assets/a.prefab", "Assets/b.mat");

      Assert.IsFalse(_detector.IsInCycle("Assets/a.prefab"));
      Assert.IsFalse(_detector.IsInCycle("Assets/b.mat"));
    }

    [Test]
    public void GetCycleContaining_AssetInCycle_ReturnsCycle() {
      _graph.AddNode(new AssetNodeModel("Assets/a.prefab", 100));
      _graph.AddNode(new AssetNodeModel("Assets/b.mat", 50));
      _graph.AddEdge("Assets/a.prefab", "Assets/b.mat");
      _graph.AddEdge("Assets/b.mat", "Assets/a.prefab");

      var cycle = _detector.GetCycleContaining("Assets/a.prefab");

      Assert.IsNotNull(cycle);
      Assert.AreEqual(2, cycle.Count);
      Assert.Contains("Assets/a.prefab", cycle);
      Assert.Contains("Assets/b.mat", cycle);
    }

    [Test]
    public void GetCycleContaining_AssetNotInCycle_ReturnsNull() {
      _graph.AddNode(new AssetNodeModel("Assets/a.prefab", 100));
      _graph.AddNode(new AssetNodeModel("Assets/b.mat", 50));
      _graph.AddEdge("Assets/a.prefab", "Assets/b.mat");

      var cycle = _detector.GetCycleContaining("Assets/a.prefab");

      Assert.IsNull(cycle);
    }

    [Test]
    public void Detect_CachesResult() {
      _graph.AddNode(new AssetNodeModel("Assets/a.prefab", 100));

      var result1 = _detector.Detect();
      var result2 = _detector.Detect();

      Assert.AreSame(result1, result2);
    }

    [Test]
    public void ClearCache_InvalidatesCache() {
      _graph.AddNode(new AssetNodeModel("Assets/a.prefab", 100));

      var result1 = _detector.Detect();
      _detector.ClearCache();
      var result2 = _detector.Detect();

      Assert.AreNotSame(result1, result2);
    }

    [Test]
    public void Detect_EmptyGraph_ReturnsEmptyResult() {
      var result = _detector.Detect();

      Assert.IsFalse(result.HasCycles);
      Assert.AreEqual(0, result.TotalCycles);
      Assert.AreEqual(0, result.TotalAssetsInCycles);
    }

    [Test]
    public void GetFormattedCycle_ReturnsReadableString() {
      _graph.AddNode(new AssetNodeModel("Assets/a.prefab", 100));
      _graph.AddNode(new AssetNodeModel("Assets/b.mat", 50));
      _graph.AddEdge("Assets/a.prefab", "Assets/b.mat");
      _graph.AddEdge("Assets/b.mat", "Assets/a.prefab");

      var result = _detector.Detect();
      var formatted = result.Cycles[0].GetFormattedCycle(_graph);

      Assert.IsNotNull(formatted);
      Assert.IsTrue(formatted.Contains("→"));
    }
  }
}
