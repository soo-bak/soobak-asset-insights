using System.Collections.Generic;

namespace Soobak.AssetInsights {
  public class HealthScoreCalculator {
    readonly DependencyGraph _graph;

    // Scoring weights
    const int UnusedAssetPenalty = 2;
    const int CircularDependencyPenalty = 10;
    const int OptimizationWarningPenalty = 1;
    const int OptimizationErrorPenalty = 3;
    const int LargeAssetPenalty = 1;
    const long LargeAssetThreshold = 10 * 1024 * 1024; // 10 MB

    public HealthScoreCalculator(DependencyGraph graph) {
      _graph = graph;
    }

    public HealthScoreResult Calculate() {
      var result = new HealthScoreResult();
      var totalPenalty = 0;

      // Analyze unused assets
      var unusedAnalyzer = new UnusedAssetAnalyzer(_graph);
      var unusedResult = unusedAnalyzer.Analyze();
      result.UnusedAssetCount = unusedResult.TotalUnusedCount;
      result.UnusedAssetSize = unusedResult.TotalUnusedSize;
      totalPenalty += unusedResult.TotalUnusedCount * UnusedAssetPenalty;
      result.Breakdown.Add(new ScoreBreakdownItem {
        Category = "Unused Assets",
        Count = unusedResult.TotalUnusedCount,
        Penalty = unusedResult.TotalUnusedCount * UnusedAssetPenalty,
        Description = $"{unusedResult.TotalUnusedCount} unused assets ({AssetNodeModel.FormatBytes(unusedResult.TotalUnusedSize)})"
      });

      // Analyze circular dependencies
      var circularDetector = new CircularDependencyDetector(_graph);
      var circularResult = circularDetector.Detect();
      result.CircularDependencyCount = circularResult.TotalCycles;
      totalPenalty += circularResult.TotalCycles * CircularDependencyPenalty;
      result.Breakdown.Add(new ScoreBreakdownItem {
        Category = "Circular Dependencies",
        Count = circularResult.TotalCycles,
        Penalty = circularResult.TotalCycles * CircularDependencyPenalty,
        Description = $"{circularResult.TotalCycles} circular dependency cycles"
      });

      // Analyze optimization opportunities
      var optimizationEngine = new OptimizationEngine(_graph);
      var optimizationReport = optimizationEngine.Analyze();
      result.OptimizationWarnings = optimizationReport.WarningCount;
      result.OptimizationErrors = optimizationReport.ErrorCount;
      result.PotentialSavings = optimizationReport.TotalPotentialSavings;

      var optPenalty = optimizationReport.WarningCount * OptimizationWarningPenalty +
                       optimizationReport.ErrorCount * OptimizationErrorPenalty;
      totalPenalty += optPenalty;
      result.Breakdown.Add(new ScoreBreakdownItem {
        Category = "Optimization Issues",
        Count = optimizationReport.TotalIssues,
        Penalty = optPenalty,
        Description = $"{optimizationReport.WarningCount} warnings, {optimizationReport.ErrorCount} errors"
      });

      // Count large assets
      var largeAssetCount = 0;
      foreach (var node in _graph.Nodes.Values) {
        if (node.SizeBytes > LargeAssetThreshold)
          largeAssetCount++;
      }
      result.LargeAssetCount = largeAssetCount;
      totalPenalty += largeAssetCount * LargeAssetPenalty;
      result.Breakdown.Add(new ScoreBreakdownItem {
        Category = "Large Assets",
        Count = largeAssetCount,
        Penalty = largeAssetCount * LargeAssetPenalty,
        Description = $"{largeAssetCount} assets over 10 MB"
      });

      // Calculate final score (100 - penalties, minimum 0)
      result.TotalPenalty = totalPenalty;
      result.Score = System.Math.Max(0, 100 - totalPenalty);
      result.Grade = CalculateGrade(result.Score);

      return result;
    }

    static HealthGrade CalculateGrade(int score) {
      return score switch {
        >= 90 => HealthGrade.A,
        >= 80 => HealthGrade.B,
        >= 70 => HealthGrade.C,
        >= 60 => HealthGrade.D,
        _ => HealthGrade.F
      };
    }
  }

  public class HealthScoreResult {
    public int Score { get; set; }
    public HealthGrade Grade { get; set; }
    public int TotalPenalty { get; set; }

    public int UnusedAssetCount { get; set; }
    public long UnusedAssetSize { get; set; }
    public int CircularDependencyCount { get; set; }
    public int OptimizationWarnings { get; set; }
    public int OptimizationErrors { get; set; }
    public long PotentialSavings { get; set; }
    public int LargeAssetCount { get; set; }

    public List<ScoreBreakdownItem> Breakdown { get; set; } = new();

    public string FormattedUnusedSize => AssetNodeModel.FormatBytes(UnusedAssetSize);
    public string FormattedSavings => AssetNodeModel.FormatBytes(PotentialSavings);
  }

  public class ScoreBreakdownItem {
    public string Category { get; set; }
    public int Count { get; set; }
    public int Penalty { get; set; }
    public string Description { get; set; }
  }

  public enum HealthGrade {
    A,
    B,
    C,
    D,
    F
  }
}
