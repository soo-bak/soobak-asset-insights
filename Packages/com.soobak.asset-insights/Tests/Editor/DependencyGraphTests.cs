using System.Linq;
using NUnit.Framework;

namespace Soobak.AssetInsights.Tests {
  [TestFixture]
  public class DependencyGraphTests {
    DependencyGraph _graph;

    [SetUp]
    public void SetUp() {
      _graph = new DependencyGraph();
    }

    [Test]
    public void AddNode_IncreasesNodeCount() {
      var node = new AssetNodeModel("Assets/test.png", 100);
      _graph.AddNode(node);

      Assert.AreEqual(1, _graph.NodeCount);
    }

    [Test]
    public void AddNode_WithNull_ThrowsArgumentNullException() {
      Assert.Throws<System.ArgumentNullException>(() => _graph.AddNode(null));
    }

    [Test]
    public void AddNode_DuplicatePath_DoesNotAddTwice() {
      var node1 = new AssetNodeModel("Assets/test.png", 100);
      var node2 = new AssetNodeModel("Assets/test.png", 200);

      _graph.AddNode(node1);
      _graph.AddNode(node2);

      Assert.AreEqual(1, _graph.NodeCount);
    }

    [Test]
    public void AddEdge_IncreasesEdgeCount() {
      _graph.AddNode(new AssetNodeModel("Assets/a.prefab", 100));
      _graph.AddNode(new AssetNodeModel("Assets/b.mat", 50));

      _graph.AddEdge("Assets/a.prefab", "Assets/b.mat");

      Assert.AreEqual(1, _graph.EdgeCount);
    }

    [Test]
    public void AddEdge_SameFromTo_DoesNothing() {
      _graph.AddNode(new AssetNodeModel("Assets/a.prefab", 100));
      _graph.AddEdge("Assets/a.prefab", "Assets/a.prefab");

      Assert.AreEqual(0, _graph.EdgeCount);
    }

    [Test]
    public void AddEdge_WithMissingNode_ThrowsInvalidOperationException() {
      _graph.AddNode(new AssetNodeModel("Assets/a.prefab", 100));

      Assert.Throws<System.InvalidOperationException>(
        () => _graph.AddEdge("Assets/a.prefab", "Assets/missing.mat"));
    }

    [Test]
    public void GetDependencies_ReturnsDirectDependencies() {
      _graph.AddNode(new AssetNodeModel("Assets/a.prefab", 100));
      _graph.AddNode(new AssetNodeModel("Assets/b.mat", 50));
      _graph.AddNode(new AssetNodeModel("Assets/c.png", 200));

      _graph.AddEdge("Assets/a.prefab", "Assets/b.mat");
      _graph.AddEdge("Assets/a.prefab", "Assets/c.png");

      var deps = _graph.GetDependencies("Assets/a.prefab");

      Assert.AreEqual(2, deps.Count);
      Assert.IsTrue(deps.Contains("Assets/b.mat"));
      Assert.IsTrue(deps.Contains("Assets/c.png"));
    }

    [Test]
    public void GetDependents_ReturnsNodesThatDependOnThis() {
      _graph.AddNode(new AssetNodeModel("Assets/a.prefab", 100));
      _graph.AddNode(new AssetNodeModel("Assets/b.prefab", 100));
      _graph.AddNode(new AssetNodeModel("Assets/shared.mat", 50));

      _graph.AddEdge("Assets/a.prefab", "Assets/shared.mat");
      _graph.AddEdge("Assets/b.prefab", "Assets/shared.mat");

      var dependents = _graph.GetDependents("Assets/shared.mat");

      Assert.AreEqual(2, dependents.Count);
      Assert.IsTrue(dependents.Contains("Assets/a.prefab"));
      Assert.IsTrue(dependents.Contains("Assets/b.prefab"));
    }

    [Test]
    public void GetAllDependencies_ReturnsTransitiveDependencies() {
      _graph.AddNode(new AssetNodeModel("Assets/a.prefab", 100));
      _graph.AddNode(new AssetNodeModel("Assets/b.mat", 50));
      _graph.AddNode(new AssetNodeModel("Assets/c.png", 200));

      _graph.AddEdge("Assets/a.prefab", "Assets/b.mat");
      _graph.AddEdge("Assets/b.mat", "Assets/c.png");

      var allDeps = _graph.GetAllDependencies("Assets/a.prefab");

      Assert.AreEqual(2, allDeps.Count);
      Assert.IsTrue(allDeps.Contains("Assets/b.mat"));
      Assert.IsTrue(allDeps.Contains("Assets/c.png"));
    }

    [Test]
    public void GetNodesBySize_ReturnsSortedByDescendingSize() {
      _graph.AddNode(new AssetNodeModel("Assets/small.png", 100));
      _graph.AddNode(new AssetNodeModel("Assets/medium.png", 500));
      _graph.AddNode(new AssetNodeModel("Assets/large.png", 1000));

      var sorted = _graph.GetNodesBySize();

      Assert.AreEqual("large", sorted[0].Name);
      Assert.AreEqual("medium", sorted[1].Name);
      Assert.AreEqual("small", sorted[2].Name);
    }

    [Test]
    public void GetNodesBySize_WithTopN_LimitsResults() {
      _graph.AddNode(new AssetNodeModel("Assets/a.png", 100));
      _graph.AddNode(new AssetNodeModel("Assets/b.png", 200));
      _graph.AddNode(new AssetNodeModel("Assets/c.png", 300));

      var top2 = _graph.GetNodesBySize(2);

      Assert.AreEqual(2, top2.Count);
    }

    [Test]
    public void GetSizeByType_GroupsByAssetType() {
      _graph.AddNode(new AssetNodeModel("Assets/a.png", 100));
      _graph.AddNode(new AssetNodeModel("Assets/b.png", 200));
      _graph.AddNode(new AssetNodeModel("Assets/c.wav", 500));

      var byType = _graph.GetSizeByType();

      Assert.AreEqual(300, byType[AssetType.Texture].totalSize);
      Assert.AreEqual(2, byType[AssetType.Texture].count);
      Assert.AreEqual(500, byType[AssetType.Audio].totalSize);
      Assert.AreEqual(1, byType[AssetType.Audio].count);
    }

    [Test]
    public void GetTotalSize_SumsAllNodeSizes() {
      _graph.AddNode(new AssetNodeModel("Assets/a.png", 100));
      _graph.AddNode(new AssetNodeModel("Assets/b.png", 200));

      Assert.AreEqual(300, _graph.GetTotalSize());
    }

    [Test]
    public void Clear_RemovesAllNodesAndEdges() {
      _graph.AddNode(new AssetNodeModel("Assets/a.png", 100));
      _graph.AddNode(new AssetNodeModel("Assets/b.png", 200));
      _graph.AddEdge("Assets/a.png", "Assets/b.png");

      _graph.Clear();

      Assert.AreEqual(0, _graph.NodeCount);
      Assert.AreEqual(0, _graph.EdgeCount);
    }
  }
}
