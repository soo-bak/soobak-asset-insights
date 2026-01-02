using NUnit.Framework;

namespace Soobak.AssetInsights.Tests {
  [TestFixture]
  public class HealthScoreCalculatorTests {
    DependencyGraph _graph;
    HealthScoreCalculator _calculator;

    [SetUp]
    public void SetUp() {
      _graph = new DependencyGraph();
      _calculator = new HealthScoreCalculator(_graph);
    }

    [Test]
    public void Calculate_EmptyGraph_Returns100() {
      // Set empty precomputed results to avoid Unity API calls
      _calculator.SetPrecomputedResults(
        new UnusedAssetResult(),
        new CircularDependencyResult(),
        new OptimizationReport()
      );

      var result = _calculator.Calculate();

      Assert.AreEqual(100, result.Score);
      Assert.AreEqual(HealthGrade.A, result.Grade);
    }

    [Test]
    public void Calculate_WithUnusedAssets_ReducesScore() {
      var unusedResult = new UnusedAssetResult {
        TotalUnusedCount = 10,
        TotalUnusedSize = 1000
      };
      _calculator.SetPrecomputedResults(
        unusedResult,
        new CircularDependencyResult(),
        new OptimizationReport()
      );

      var result = _calculator.Calculate();

      // 10 unused assets * 2 penalty = 20 penalty
      Assert.AreEqual(80, result.Score);
      Assert.AreEqual(10, result.UnusedAssetCount);
    }

    [Test]
    public void Calculate_WithCircularDependencies_ReducesScore() {
      var circularResult = new CircularDependencyResult {
        TotalCycles = 3
      };
      _calculator.SetPrecomputedResults(
        new UnusedAssetResult(),
        circularResult,
        new OptimizationReport()
      );

      var result = _calculator.Calculate();

      // 3 cycles * 10 penalty = 30 penalty
      Assert.AreEqual(70, result.Score);
      Assert.AreEqual(3, result.CircularDependencyCount);
    }

    [Test]
    public void Calculate_WithOptimizationWarnings_ReducesScore() {
      var optimizationReport = new OptimizationReport {
        TotalIssues = 5,
        IssuesBySeverity = new() {
          { OptimizationSeverity.Warning, 5 }
        }
      };
      _calculator.SetPrecomputedResults(
        new UnusedAssetResult(),
        new CircularDependencyResult(),
        optimizationReport
      );

      var result = _calculator.Calculate();

      // 5 warnings * 1 penalty = 5 penalty
      Assert.AreEqual(95, result.Score);
      Assert.AreEqual(5, result.OptimizationWarnings);
    }

    [Test]
    public void Calculate_WithOptimizationErrors_ReducesScoreMore() {
      var optimizationReport = new OptimizationReport {
        TotalIssues = 5,
        IssuesBySeverity = new() {
          { OptimizationSeverity.Error, 5 }
        }
      };
      _calculator.SetPrecomputedResults(
        new UnusedAssetResult(),
        new CircularDependencyResult(),
        optimizationReport
      );

      var result = _calculator.Calculate();

      // 5 errors * 3 penalty = 15 penalty
      Assert.AreEqual(85, result.Score);
      Assert.AreEqual(5, result.OptimizationErrors);
    }

    [Test]
    public void Calculate_WithLargeAssets_ReducesScore() {
      // Add a large asset (> 10 MB)
      _graph.AddNode(new AssetNodeModel("Assets/large.png", 15 * 1024 * 1024));

      _calculator.SetPrecomputedResults(
        new UnusedAssetResult(),
        new CircularDependencyResult(),
        new OptimizationReport()
      );

      var result = _calculator.Calculate();

      // 1 large asset * 1 penalty = 1 penalty
      Assert.AreEqual(99, result.Score);
      Assert.AreEqual(1, result.LargeAssetCount);
    }

    [Test]
    public void Calculate_ScoreNeverBelowZero() {
      var unusedResult = new UnusedAssetResult {
        TotalUnusedCount = 100 // 200 penalty
      };
      _calculator.SetPrecomputedResults(
        unusedResult,
        new CircularDependencyResult(),
        new OptimizationReport()
      );

      var result = _calculator.Calculate();

      Assert.AreEqual(0, result.Score);
      Assert.AreEqual(HealthGrade.F, result.Grade);
    }

    [Test]
    public void Calculate_GradeA_Score90OrAbove() {
      _calculator.SetPrecomputedResults(
        new UnusedAssetResult { TotalUnusedCount = 5 }, // 10 penalty
        new CircularDependencyResult(),
        new OptimizationReport()
      );

      var result = _calculator.Calculate();

      Assert.AreEqual(90, result.Score);
      Assert.AreEqual(HealthGrade.A, result.Grade);
    }

    [Test]
    public void Calculate_GradeB_Score80To89() {
      _calculator.SetPrecomputedResults(
        new UnusedAssetResult { TotalUnusedCount = 6 }, // 12 penalty
        new CircularDependencyResult(),
        new OptimizationReport()
      );

      var result = _calculator.Calculate();

      Assert.AreEqual(88, result.Score);
      Assert.AreEqual(HealthGrade.B, result.Grade);
    }

    [Test]
    public void Calculate_GradeC_Score70To79() {
      _calculator.SetPrecomputedResults(
        new UnusedAssetResult { TotalUnusedCount = 12 }, // 24 penalty
        new CircularDependencyResult(),
        new OptimizationReport()
      );

      var result = _calculator.Calculate();

      Assert.AreEqual(76, result.Score);
      Assert.AreEqual(HealthGrade.C, result.Grade);
    }

    [Test]
    public void Calculate_GradeD_Score60To69() {
      _calculator.SetPrecomputedResults(
        new UnusedAssetResult { TotalUnusedCount = 17 }, // 34 penalty
        new CircularDependencyResult(),
        new OptimizationReport()
      );

      var result = _calculator.Calculate();

      Assert.AreEqual(66, result.Score);
      Assert.AreEqual(HealthGrade.D, result.Grade);
    }

    [Test]
    public void Calculate_GradeF_ScoreBelow60() {
      _calculator.SetPrecomputedResults(
        new UnusedAssetResult { TotalUnusedCount = 25 }, // 50 penalty
        new CircularDependencyResult(),
        new OptimizationReport()
      );

      var result = _calculator.Calculate();

      Assert.AreEqual(50, result.Score);
      Assert.AreEqual(HealthGrade.F, result.Grade);
    }

    [Test]
    public void Calculate_BreakdownContainsAllCategories() {
      _calculator.SetPrecomputedResults(
        new UnusedAssetResult(),
        new CircularDependencyResult(),
        new OptimizationReport()
      );

      var result = _calculator.Calculate();

      Assert.AreEqual(4, result.Breakdown.Count);
    }

    [Test]
    public void Calculate_CombinedPenalties() {
      var unusedResult = new UnusedAssetResult { TotalUnusedCount = 5 }; // 10
      var circularResult = new CircularDependencyResult { TotalCycles = 1 }; // 10
      var optimizationReport = new OptimizationReport {
        IssuesBySeverity = new() {
          { OptimizationSeverity.Warning, 2 }, // 2
          { OptimizationSeverity.Error, 1 } // 3
        }
      };

      _calculator.SetPrecomputedResults(unusedResult, circularResult, optimizationReport);

      var result = _calculator.Calculate();

      // 10 + 10 + 2 + 3 = 25 penalty
      Assert.AreEqual(75, result.Score);
      Assert.AreEqual(25, result.TotalPenalty);
    }
  }
}
