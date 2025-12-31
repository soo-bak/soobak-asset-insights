using NUnit.Framework;

namespace Soobak.AssetInsights.Tests {
  public class ReportExporterTests {
    DependencyGraph _graph;
    ReportExporter _exporter;

    [SetUp]
    public void SetUp() {
      _graph = new DependencyGraph();
      _exporter = new ReportExporter();
    }

    [Test]
    public void Export_Markdown_ContainsHeader() {
      _graph.AddNode(new AssetNodeModel("Assets/Test.png", 1000));

      var result = _exporter.Export(_graph);

      Assert.IsTrue(result.Contains("# Asset Insights Report"));
      Assert.IsTrue(result.Contains("Total Assets"));
    }

    [Test]
    public void Export_Mermaid_ContainsMermaidBlock() {
      _graph.AddNode(new AssetNodeModel("Assets/Test.png", 1000));
      var options = new ReportOptions { Format = ReportFormat.Mermaid };

      var result = _exporter.Export(_graph, options);

      Assert.IsTrue(result.Contains("```mermaid"));
      Assert.IsTrue(result.Contains("flowchart TD"));
    }

    [Test]
    public void Export_Json_ContainsValidStructure() {
      _graph.AddNode(new AssetNodeModel("Assets/Test.png", 1000));
      var options = new ReportOptions { Format = ReportFormat.Json };

      var result = _exporter.Export(_graph, options);

      Assert.IsTrue(result.Contains("\"nodeCount\":"));
      Assert.IsTrue(result.Contains("\"nodes\":"));
    }

    [Test]
    public void ExportHeavyHitters_ContainsTable() {
      _graph.AddNode(new AssetNodeModel("Assets/Large.png", 10000000));
      _graph.AddNode(new AssetNodeModel("Assets/Small.png", 1000));

      var result = _exporter.ExportHeavyHitters(_graph, 10);

      Assert.IsTrue(result.Contains("# Heavy Hitters Report"));
      Assert.IsTrue(result.Contains("Large.png"));
      Assert.IsTrue(result.Contains("| # | Size |"));
    }

    [Test]
    public void ExportHeavyHitters_OrdersBySize() {
      _graph.AddNode(new AssetNodeModel("Assets/Small.png", 1000));
      _graph.AddNode(new AssetNodeModel("Assets/Large.png", 10000000));
      _graph.AddNode(new AssetNodeModel("Assets/Medium.png", 100000));

      var result = _exporter.ExportHeavyHitters(_graph, 10);

      int largePos = result.IndexOf("Large.png");
      int mediumPos = result.IndexOf("Medium.png");
      int smallPos = result.IndexOf("Small.png");

      Assert.IsTrue(largePos < mediumPos);
      Assert.IsTrue(mediumPos < smallPos);
    }

    [Test]
    public void ExportWhyIncluded_WithNoPath_ShowsNotFound() {
      _graph.AddNode(new AssetNodeModel("Assets/Isolated.png", 1000));

      var result = _exporter.ExportWhyIncluded(_graph, "Assets/Isolated.png");

      Assert.IsTrue(result.Contains("No dependency paths found"));
    }

    [Test]
    public void ExportWhyIncluded_WithPath_ShowsDependencyChain() {
      _graph.AddNode(new AssetNodeModel("Assets/Scene.unity", 1000));
      _graph.AddNode(new AssetNodeModel("Assets/Prefab.prefab", 500));
      _graph.AddNode(new AssetNodeModel("Assets/Texture.png", 2000));
      _graph.AddEdge("Assets/Scene.unity", "Assets/Prefab.prefab");
      _graph.AddEdge("Assets/Prefab.prefab", "Assets/Texture.png");

      var result = _exporter.ExportWhyIncluded(_graph, "Assets/Texture.png");

      Assert.IsTrue(result.Contains("Scene.unity"));
      Assert.IsTrue(result.Contains("Prefab.prefab"));
      Assert.IsTrue(result.Contains("Texture.png"));
    }

    [Test]
    public void Export_WithSizeBreakdown_ShowsTypeGroups() {
      _graph.AddNode(new AssetNodeModel("Assets/Tex1.png", 1000));
      _graph.AddNode(new AssetNodeModel("Assets/Tex2.png", 2000));
      _graph.AddNode(new AssetNodeModel("Assets/Script.cs", 500));

      var options = new ReportOptions { IncludeSizeBreakdown = true };
      var result = _exporter.Export(_graph, options);

      Assert.IsTrue(result.Contains("Size Breakdown by Type"));
      Assert.IsTrue(result.Contains("Texture"));
      Assert.IsTrue(result.Contains("Script"));
    }

    [Test]
    public void Export_EmptyGraph_HandlesGracefully() {
      var result = _exporter.Export(_graph);

      Assert.IsTrue(result.Contains("Total Assets:** 0"));
    }
  }
}
