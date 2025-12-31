using System.Linq;
using NUnit.Framework;

namespace Soobak.AssetInsights.Tests {
  [TestFixture]
  public class PathFinderTests {
    DependencyGraph _graph;

    [SetUp]
    public void SetUp() {
      _graph = new DependencyGraph();
    }

    [Test]
    public void FindShortestPath_SameFromTo_ReturnsSingleNode() {
      _graph.AddNode(new AssetNodeModel("Assets/a.prefab", 100));

      var path = PathFinder.FindShortestPath(_graph, "Assets/a.prefab", "Assets/a.prefab");

      Assert.IsNotNull(path);
      Assert.AreEqual(1, path.Count);
      Assert.AreEqual("a", path[0].Name);
    }

    [Test]
    public void FindShortestPath_DirectDependency_ReturnsTwoNodes() {
      _graph.AddNode(new AssetNodeModel("Assets/a.prefab", 100));
      _graph.AddNode(new AssetNodeModel("Assets/b.mat", 50));
      _graph.AddEdge("Assets/a.prefab", "Assets/b.mat");

      var path = PathFinder.FindShortestPath(_graph, "Assets/a.prefab", "Assets/b.mat");

      Assert.IsNotNull(path);
      Assert.AreEqual(2, path.Count);
      Assert.AreEqual("a", path[0].Name);
      Assert.AreEqual("b", path[1].Name);
    }

    [Test]
    public void FindShortestPath_TransitiveDependency_ReturnsFullPath() {
      _graph.AddNode(new AssetNodeModel("Assets/a.prefab", 100));
      _graph.AddNode(new AssetNodeModel("Assets/b.mat", 50));
      _graph.AddNode(new AssetNodeModel("Assets/c.png", 200));
      _graph.AddEdge("Assets/a.prefab", "Assets/b.mat");
      _graph.AddEdge("Assets/b.mat", "Assets/c.png");

      var path = PathFinder.FindShortestPath(_graph, "Assets/a.prefab", "Assets/c.png");

      Assert.IsNotNull(path);
      Assert.AreEqual(3, path.Count);
      Assert.AreEqual("a", path[0].Name);
      Assert.AreEqual("b", path[1].Name);
      Assert.AreEqual("c", path[2].Name);
    }

    [Test]
    public void FindShortestPath_NoPath_ReturnsNull() {
      _graph.AddNode(new AssetNodeModel("Assets/a.prefab", 100));
      _graph.AddNode(new AssetNodeModel("Assets/b.mat", 50));

      var path = PathFinder.FindShortestPath(_graph, "Assets/a.prefab", "Assets/b.mat");

      Assert.IsNull(path);
    }

    [Test]
    public void FindShortestPath_ChoosesShortestWhenMultiplePathsExist() {
      _graph.AddNode(new AssetNodeModel("Assets/a.prefab", 100));
      _graph.AddNode(new AssetNodeModel("Assets/b.mat", 50));
      _graph.AddNode(new AssetNodeModel("Assets/c.png", 200));
      _graph.AddNode(new AssetNodeModel("Assets/d.shader", 30));

      // Long path: a -> b -> c -> d
      _graph.AddEdge("Assets/a.prefab", "Assets/b.mat");
      _graph.AddEdge("Assets/b.mat", "Assets/c.png");
      _graph.AddEdge("Assets/c.png", "Assets/d.shader");

      // Short path: a -> d
      _graph.AddEdge("Assets/a.prefab", "Assets/d.shader");

      var path = PathFinder.FindShortestPath(_graph, "Assets/a.prefab", "Assets/d.shader");

      Assert.IsNotNull(path);
      Assert.AreEqual(2, path.Count);
    }

    [Test]
    public void FindWhyIncluded_ReturnsPathsFromAllReachableRoots() {
      _graph.AddNode(new AssetNodeModel("Assets/scene1.unity", 100));
      _graph.AddNode(new AssetNodeModel("Assets/scene2.unity", 100));
      _graph.AddNode(new AssetNodeModel("Assets/shared.mat", 50));
      _graph.AddEdge("Assets/scene1.unity", "Assets/shared.mat");
      _graph.AddEdge("Assets/scene2.unity", "Assets/shared.mat");

      var roots = new[] { "Assets/scene1.unity", "Assets/scene2.unity" };
      var result = PathFinder.FindWhyIncluded(_graph, roots, "Assets/shared.mat");

      Assert.AreEqual(2, result.Count);
      Assert.IsTrue(result.ContainsKey("Assets/scene1.unity"));
      Assert.IsTrue(result.ContainsKey("Assets/scene2.unity"));
    }

    [Test]
    public void FindWhyIncluded_ExcludesUnreachableRoots() {
      _graph.AddNode(new AssetNodeModel("Assets/scene1.unity", 100));
      _graph.AddNode(new AssetNodeModel("Assets/scene2.unity", 100));
      _graph.AddNode(new AssetNodeModel("Assets/target.mat", 50));
      _graph.AddEdge("Assets/scene1.unity", "Assets/target.mat");

      var roots = new[] { "Assets/scene1.unity", "Assets/scene2.unity" };
      var result = PathFinder.FindWhyIncluded(_graph, roots, "Assets/target.mat");

      Assert.AreEqual(1, result.Count);
      Assert.IsTrue(result.ContainsKey("Assets/scene1.unity"));
      Assert.IsFalse(result.ContainsKey("Assets/scene2.unity"));
    }

    [Test]
    public void FindReversePath_FindsPathFromTargetToRoot() {
      _graph.AddNode(new AssetNodeModel("Assets/a.prefab", 100));
      _graph.AddNode(new AssetNodeModel("Assets/b.mat", 50));
      _graph.AddNode(new AssetNodeModel("Assets/c.png", 200));
      _graph.AddEdge("Assets/a.prefab", "Assets/b.mat");
      _graph.AddEdge("Assets/b.mat", "Assets/c.png");

      var path = PathFinder.FindReversePath(_graph, "Assets/c.png", "Assets/a.prefab");

      Assert.IsNotNull(path);
      Assert.AreEqual(3, path.Count);
      Assert.AreEqual("c", path[0].Name);
      Assert.AreEqual("b", path[1].Name);
      Assert.AreEqual("a", path[2].Name);
    }

    [Test]
    public void FindReversePath_NoPath_ReturnsNull() {
      _graph.AddNode(new AssetNodeModel("Assets/a.prefab", 100));
      _graph.AddNode(new AssetNodeModel("Assets/b.mat", 50));

      var path = PathFinder.FindReversePath(_graph, "Assets/b.mat", "Assets/a.prefab");

      Assert.IsNull(path);
    }
  }
}
