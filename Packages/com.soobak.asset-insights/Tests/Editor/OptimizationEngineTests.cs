using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Soobak.AssetInsights.Tests {
  [TestFixture]
  public class OptimizationEngineTests {
    DependencyGraph _graph;

    [SetUp]
    public void SetUp() {
      _graph = new DependencyGraph();
    }

    [Test]
    public void RegisterRule_AddsCustomRule() {
      var engine = new TestableOptimizationEngine(_graph);
      var customRule = new MockOptimizationRule("CustomRule");

      engine.RegisterRule(customRule);
      _graph.AddNode(new AssetNodeModel("Assets/test.png", 100));

      var report = engine.Analyze();

      Assert.IsTrue(report.Issues.Any(i => i.RuleName == "CustomRule"));
    }

    [Test]
    public void Analyze_CachesResult() {
      var engine = new TestableOptimizationEngine(_graph);
      _graph.AddNode(new AssetNodeModel("Assets/test.png", 100));

      var report1 = engine.Analyze();
      var report2 = engine.Analyze();

      Assert.AreSame(report1, report2);
    }

    [Test]
    public void ClearCache_InvalidatesCache() {
      var engine = new TestableOptimizationEngine(_graph);
      _graph.AddNode(new AssetNodeModel("Assets/test.png", 100));

      var report1 = engine.Analyze();
      engine.ClearCache();
      var report2 = engine.Analyze();

      Assert.AreNotSame(report1, report2);
    }

    [Test]
    public void Analyze_EmptyGraph_ReturnsEmptyReport() {
      var engine = new TestableOptimizationEngine(_graph);

      var report = engine.Analyze();

      Assert.AreEqual(0, report.TotalIssues);
      Assert.AreEqual(0, report.TotalPotentialSavings);
    }

    [Test]
    public void Analyze_WithIssues_CalculatesTotals() {
      var engine = new TestableOptimizationEngine(_graph);
      engine.RegisterRule(new MockOptimizationRule("Rule1", 1000));
      engine.RegisterRule(new MockOptimizationRule("Rule2", 2000));
      _graph.AddNode(new AssetNodeModel("Assets/test.png", 100));

      var report = engine.Analyze();

      Assert.AreEqual(2, report.TotalIssues);
      Assert.AreEqual(3000, report.TotalPotentialSavings);
    }

    [Test]
    public void Analyze_SortsByServerityThenSavings() {
      var engine = new TestableOptimizationEngine(_graph);
      engine.RegisterRule(new MockOptimizationRule("Low", 100, OptimizationSeverity.Info));
      engine.RegisterRule(new MockOptimizationRule("High", 50, OptimizationSeverity.Error));
      engine.RegisterRule(new MockOptimizationRule("Medium", 200, OptimizationSeverity.Warning));
      _graph.AddNode(new AssetNodeModel("Assets/test.png", 100));

      var report = engine.Analyze();

      Assert.AreEqual("High", report.Issues[0].RuleName);
      Assert.AreEqual("Medium", report.Issues[1].RuleName);
      Assert.AreEqual("Low", report.Issues[2].RuleName);
    }

    [Test]
    public void InvalidateAsset_RemovesAssetFromCache() {
      var engine = new TestableOptimizationEngine(_graph);
      var rule = new MockOptimizationRule("Rule", 1000);
      engine.RegisterRule(rule);
      _graph.AddNode(new AssetNodeModel("Assets/test.png", 100));

      var report1 = engine.Analyze();
      Assert.AreEqual(1, report1.TotalIssues);

      // Simulate fix by changing rule behavior
      rule.SetReturnNoIssues(true);
      engine.InvalidateAsset("Assets/test.png");

      Assert.AreEqual(0, engine.LastReport.TotalIssues);
    }

    [Test]
    public void InvalidateAsset_RecalculatesTotals() {
      var engine = new TestableOptimizationEngine(_graph);
      engine.RegisterRule(new MockOptimizationRule("Rule", 1000));
      _graph.AddNode(new AssetNodeModel("Assets/a.png", 100));
      _graph.AddNode(new AssetNodeModel("Assets/b.png", 100));

      var report = engine.Analyze();
      Assert.AreEqual(2, report.TotalIssues);
      Assert.AreEqual(2000, report.TotalPotentialSavings);

      // Invalidate one asset (it will be re-analyzed with same rule)
      engine.InvalidateAsset("Assets/a.png");

      Assert.AreEqual(2, engine.LastReport.TotalIssues);
    }

    [Test]
    public void AnalyzeAsset_CachesIndividualResults() {
      var engine = new TestableOptimizationEngine(_graph);
      var rule = new MockOptimizationRule("Rule", 1000);
      engine.RegisterRule(rule);
      _graph.AddNode(new AssetNodeModel("Assets/test.png", 100));

      var issues1 = engine.AnalyzeAsset("Assets/test.png").ToList();
      var callCount1 = rule.EvaluateCallCount;

      var issues2 = engine.AnalyzeAsset("Assets/test.png").ToList();
      var callCount2 = rule.EvaluateCallCount;

      Assert.AreEqual(callCount1, callCount2); // Should use cache, not call again
    }

    [Test]
    public void AnalyzeAsset_NonExistentPath_ReturnsEmpty() {
      var engine = new TestableOptimizationEngine(_graph);

      var issues = engine.AnalyzeAsset("Assets/nonexistent.png").ToList();

      Assert.AreEqual(0, issues.Count);
    }

    [Test]
    public void LastReport_BeforeAnalyze_ReturnsNull() {
      var engine = new TestableOptimizationEngine(_graph);

      Assert.IsNull(engine.LastReport);
    }

    [Test]
    public void LastReport_AfterAnalyze_ReturnsReport() {
      var engine = new TestableOptimizationEngine(_graph);
      engine.Analyze();

      Assert.IsNotNull(engine.LastReport);
    }
  }

  /// <summary>
  /// Optimization engine without default rules for testing.
  /// </summary>
  class TestableOptimizationEngine : OptimizationEngine {
    public TestableOptimizationEngine(DependencyGraph graph) : base(graph) {
      ClearCache(); // Clear default rules' cached results
    }
  }

  /// <summary>
  /// Mock rule for testing.
  /// </summary>
  class MockOptimizationRule : IOptimizationRule {
    public string RuleName { get; }
    public string Description => "Mock rule for testing";
    public OptimizationSeverity Severity { get; }
    public int EvaluateCallCount { get; private set; }

    readonly long _potentialSavings;
    bool _returnNoIssues;

    public MockOptimizationRule(string name, long potentialSavings = 0, OptimizationSeverity severity = OptimizationSeverity.Warning) {
      RuleName = name;
      _potentialSavings = potentialSavings;
      Severity = severity;
    }

    public void SetReturnNoIssues(bool value) {
      _returnNoIssues = value;
    }

    public IEnumerable<OptimizationIssue> Evaluate(AssetNodeModel node, DependencyGraph graph) {
      EvaluateCallCount++;

      if (_returnNoIssues)
        yield break;

      yield return new OptimizationIssue {
        RuleName = RuleName,
        AssetPath = node.Path,
        AssetName = node.Name,
        Message = "Mock issue",
        Severity = Severity,
        PotentialSavings = _potentialSavings
      };
    }
  }
}
