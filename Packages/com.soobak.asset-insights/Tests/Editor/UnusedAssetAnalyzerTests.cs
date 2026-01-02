using NUnit.Framework;

namespace Soobak.AssetInsights.Tests {
  [TestFixture]
  public class UnusedAssetAnalyzerTests {
    DependencyGraph _graph;
    UnusedAssetAnalyzer _analyzer;

    [SetUp]
    public void SetUp() {
      _graph = new DependencyGraph();
      _analyzer = new UnusedAssetAnalyzer(_graph);
    }

    [Test]
    public void Analyze_EmptyGraph_ReturnsEmptyResult() {
      var result = _analyzer.Analyze();

      Assert.AreEqual(0, result.TotalUnusedCount);
      Assert.AreEqual(0, result.TotalUnusedSize);
      Assert.AreEqual(0, result.TotalAssetCount);
    }

    [Test]
    public void Analyze_SkipsEditorAssets() {
      _graph.AddNode(new AssetNodeModel("Assets/Editor/EditorScript.cs", 100));

      var result = _analyzer.Analyze();

      // Editor assets should be skipped, not counted as unused
      Assert.AreEqual(0, result.TotalUnusedCount);
    }

    [Test]
    public void Analyze_SkipsPackageAssets() {
      _graph.AddNode(new AssetNodeModel("Packages/com.unity.test/test.cs", 100));

      var result = _analyzer.Analyze();

      Assert.AreEqual(0, result.TotalUnusedCount);
    }

    [Test]
    public void Analyze_SkipsScriptFiles() {
      _graph.AddNode(new AssetNodeModel("Assets/Scripts/MyScript.cs", 100));
      _graph.AddNode(new AssetNodeModel("Assets/MyAssembly.asmdef", 50));
      _graph.AddNode(new AssetNodeModel("Assets/MyRef.asmref", 30));

      var result = _analyzer.Analyze();

      Assert.AreEqual(0, result.TotalUnusedCount);
    }

    [Test]
    public void UnusedAssetResult_UnusedPercentage_CalculatesCorrectly() {
      var result = new UnusedAssetResult {
        TotalUnusedCount = 25,
        TotalAssetCount = 100
      };

      Assert.AreEqual(25f, result.UnusedPercentage);
    }

    [Test]
    public void UnusedAssetResult_UnusedPercentage_ZeroAssets_ReturnsZero() {
      var result = new UnusedAssetResult {
        TotalUnusedCount = 0,
        TotalAssetCount = 0
      };

      Assert.AreEqual(0f, result.UnusedPercentage);
    }

    [Test]
    public void UnusedAssetInfo_FormattedSize_ReturnsReadableString() {
      var info = new UnusedAssetInfo {
        SizeBytes = 1024 * 1024 // 1 MB
      };

      Assert.AreEqual("1.00 MB", info.FormattedSize);
    }
  }
}
