using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Soobak.AssetInsights {
  /// <summary>
  /// Headless runner for CI/CD integration.
  /// Usage: Unity -batchmode -executeMethod Soobak.AssetInsights.HeadlessRunner.Run
  ///
  /// Arguments:
  ///   -outputPath [path]   Output path for JSON report (default: asset-insights-report.json)
  ///   -failOnScore [score] Exit with error code 1 if score is below threshold (default: 0, disabled)
  ///   -format [format]     Output format: json, markdown (default: json)
  /// </summary>
  public static class HeadlessRunner {
    public static void Run() {
      var args = Environment.GetCommandLineArgs();
      var outputPath = GetArgValue(args, "-outputPath", "asset-insights-report.json");
      var failOnScore = int.Parse(GetArgValue(args, "-failOnScore", "0"));
      var format = GetArgValue(args, "-format", "json");

      Debug.Log("[Asset Insights] Starting headless scan...");

      try {
        var scanner = new DependencyScanner();
        var options = new ScanOptions();

        // Run scan synchronously
        var enumerator = scanner.ScanAsync(options);
        while (enumerator.MoveNext()) {
          // Process scan
        }

        Debug.Log($"[Asset Insights] Scanned {scanner.Graph.NodeCount} assets");

        // Generate report
        var report = GenerateReport(scanner.Graph);

        // Write output
        var output = format.ToLower() == "markdown"
          ? GenerateMarkdownReport(report)
          : GenerateJsonReport(report);

        File.WriteAllText(outputPath, output);
        Debug.Log($"[Asset Insights] Report written to: {outputPath}");

        // Print summary
        PrintSummary(report);

        // Check fail threshold
        if (failOnScore > 0 && report.HealthScore.Score < failOnScore) {
          Debug.LogError($"[Asset Insights] Health score {report.HealthScore.Score} is below threshold {failOnScore}");
          EditorApplication.Exit(1);
          return;
        }

        Debug.Log("[Asset Insights] Scan completed successfully");
        EditorApplication.Exit(0);
      } catch (Exception ex) {
        Debug.LogError($"[Asset Insights] Error: {ex.Message}");
        Debug.LogException(ex);
        EditorApplication.Exit(1);
      }
    }

    static string GetArgValue(string[] args, string key, string defaultValue) {
      for (int i = 0; i < args.Length - 1; i++) {
        if (args[i] == key)
          return args[i + 1];
      }
      return defaultValue;
    }

    static CIReport GenerateReport(DependencyGraph graph) {
      var report = new CIReport();

      // Basic stats
      report.TotalAssets = graph.NodeCount;
      report.TotalSize = graph.GetTotalSize();
      report.TotalEdges = graph.EdgeCount;

      // Health score
      var healthCalculator = new HealthScoreCalculator(graph);
      report.HealthScore = healthCalculator.Calculate();

      // Unused assets
      var unusedAnalyzer = new UnusedAssetAnalyzer(graph);
      report.UnusedAssets = unusedAnalyzer.Analyze();

      // Circular dependencies
      var circularDetector = new CircularDependencyDetector(graph);
      report.CircularDependencies = circularDetector.Detect();

      // Optimization issues
      var optimizationEngine = new OptimizationEngine(graph);
      report.Optimization = optimizationEngine.Analyze();

      // Size by type
      report.SizeByType = graph.GetSizeByType();

      return report;
    }

    static void PrintSummary(CIReport report) {
      Debug.Log("=== Asset Insights Summary ===");
      Debug.Log($"Health Grade: {report.HealthScore.Grade} ({report.HealthScore.Score}/100)");
      Debug.Log($"Total Assets: {report.TotalAssets}");
      Debug.Log($"Total Size: {AssetNodeModel.FormatBytes(report.TotalSize)}");
      Debug.Log($"Unused Assets: {report.UnusedAssets.TotalUnusedCount} ({AssetNodeModel.FormatBytes(report.UnusedAssets.TotalUnusedSize)})");
      Debug.Log($"Circular Dependencies: {report.CircularDependencies.TotalCycles}");
      Debug.Log($"Optimization Issues: {report.Optimization.TotalIssues}");
      Debug.Log($"Potential Savings: {AssetNodeModel.FormatBytes(report.Optimization.TotalPotentialSavings)}");
    }

    static string GenerateJsonReport(CIReport report) {
      return JsonUtility.ToJson(new CIReportJson {
        timestamp = DateTime.UtcNow.ToString("o"),
        totalAssets = report.TotalAssets,
        totalSizeBytes = report.TotalSize,
        healthScore = report.HealthScore.Score,
        healthGrade = report.HealthScore.Grade.ToString(),
        unusedAssetCount = report.UnusedAssets.TotalUnusedCount,
        unusedAssetSizeBytes = report.UnusedAssets.TotalUnusedSize,
        circularDependencyCount = report.CircularDependencies.TotalCycles,
        optimizationIssueCount = report.Optimization.TotalIssues,
        potentialSavingsBytes = report.Optimization.TotalPotentialSavings
      }, true);
    }

    static string GenerateMarkdownReport(CIReport report) {
      var sb = new System.Text.StringBuilder();

      sb.AppendLine("# Asset Insights Report");
      sb.AppendLine();
      sb.AppendLine($"**Generated:** {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
      sb.AppendLine();

      sb.AppendLine("## Health Score");
      sb.AppendLine();
      sb.AppendLine($"| Grade | Score |");
      sb.AppendLine($"|-------|-------|");
      sb.AppendLine($"| **{report.HealthScore.Grade}** | {report.HealthScore.Score}/100 |");
      sb.AppendLine();

      sb.AppendLine("## Summary");
      sb.AppendLine();
      sb.AppendLine($"| Metric | Value |");
      sb.AppendLine($"|--------|-------|");
      sb.AppendLine($"| Total Assets | {report.TotalAssets} |");
      sb.AppendLine($"| Total Size | {AssetNodeModel.FormatBytes(report.TotalSize)} |");
      sb.AppendLine($"| Unused Assets | {report.UnusedAssets.TotalUnusedCount} |");
      sb.AppendLine($"| Unused Size | {AssetNodeModel.FormatBytes(report.UnusedAssets.TotalUnusedSize)} |");
      sb.AppendLine($"| Circular Dependencies | {report.CircularDependencies.TotalCycles} |");
      sb.AppendLine($"| Optimization Issues | {report.Optimization.TotalIssues} |");
      sb.AppendLine($"| Potential Savings | {AssetNodeModel.FormatBytes(report.Optimization.TotalPotentialSavings)} |");
      sb.AppendLine();

      if (report.Optimization.TotalIssues > 0) {
        sb.AppendLine("## Top Optimization Issues");
        sb.AppendLine();
        var count = 0;
        foreach (var issue in report.Optimization.Issues) {
          if (count++ >= 10) break;
          sb.AppendLine($"- **{issue.RuleName}**: {issue.AssetName} - {issue.Message}");
        }
        sb.AppendLine();
      }

      sb.AppendLine("---");
      sb.AppendLine("*Generated by Asset Insights*");

      return sb.ToString();
    }
  }

  class CIReport {
    public int TotalAssets;
    public long TotalSize;
    public int TotalEdges;
    public HealthScoreResult HealthScore;
    public UnusedAssetResult UnusedAssets;
    public CircularDependencyResult CircularDependencies;
    public OptimizationReport Optimization;
    public System.Collections.Generic.Dictionary<AssetType, (long totalSize, int count)> SizeByType;
  }

  [Serializable]
  class CIReportJson {
    public string timestamp;
    public int totalAssets;
    public long totalSizeBytes;
    public int healthScore;
    public string healthGrade;
    public int unusedAssetCount;
    public long unusedAssetSizeBytes;
    public int circularDependencyCount;
    public int optimizationIssueCount;
    public long potentialSavingsBytes;
  }
}
