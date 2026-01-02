using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Soobak.AssetInsights {
  public class OptimizationEngine {
    readonly List<IOptimizationRule> _rules = new();
    readonly DependencyGraph _graph;
    OptimizationReport _cachedReport;
    readonly Dictionary<string, List<OptimizationIssue>> _assetIssueCache = new();

    // Batch processing settings
    const int CleanupBatchSize = 50; // Cleanup memory every N heavy assets
    int _heavyAssetCounter;

    /// <summary>
    /// Returns the cached report from the last Analyze() call, or null if not yet analyzed.
    /// </summary>
    public OptimizationReport LastReport => _cachedReport;

    public OptimizationEngine(DependencyGraph graph) {
      _graph = graph;
      RegisterDefaultRules();
    }

    public void ClearCache() {
      _cachedReport = null;
      _assetIssueCache.Clear();
    }

    /// <summary>
    /// Invalidates cache for a specific asset only.
    /// More efficient than ClearCache() when only one asset changed.
    /// </summary>
    public void InvalidateAsset(string assetPath) {
      _assetIssueCache.Remove(assetPath);
      // Clear report cache since totals may have changed
      _cachedReport = null;
    }

    void RegisterDefaultRules() {
      _rules.Add(new TextureSizeRule());
      _rules.Add(new AudioCompressionRule());
      _rules.Add(new DuplicateAssetRule());
      _rules.Add(new MeshOptimizationRule());
      _rules.Add(new MaterialOptimizationRule());
    }

    public void RegisterRule(IOptimizationRule rule) {
      _rules.Add(rule);
    }

    public OptimizationReport Analyze() {
      // Return cached report if available
      if (_cachedReport != null)
        return _cachedReport;

      var report = new OptimizationReport();
      _heavyAssetCounter = 0;

      foreach (var node in _graph.Nodes.Values) {
        var nodeIssues = AnalyzeAssetCached(node.Path).ToList();
        report.Issues.AddRange(nodeIssues);

        // Track heavy assets (those that load actual Unity objects)
        if (IsHeavyAssetType(node.Type)) {
          _heavyAssetCounter++;

          // Periodically clean up memory to prevent GPU memory exhaustion
          if (_heavyAssetCounter >= CleanupBatchSize) {
            _heavyAssetCounter = 0;
            EditorUtility.UnloadUnusedAssetsImmediate();
          }
        }
      }

      report.Issues = report.Issues
        .OrderByDescending(i => i.Severity)
        .ThenByDescending(i => i.PotentialSavings)
        .ToList();

      report.TotalIssues = report.Issues.Count;
      report.TotalPotentialSavings = report.Issues.Sum(i => i.PotentialSavings);
      report.IssuesBySeverity = report.Issues
        .GroupBy(i => i.Severity)
        .ToDictionary(g => g.Key, g => g.Count());

      // Final cleanup after all analysis
      EditorUtility.UnloadUnusedAssetsImmediate();

      _cachedReport = report;
      return report;
    }

    /// <summary>
    /// Asset types that require loading actual Unity objects (textures, materials, meshes).
    /// These consume GPU/CPU memory and need periodic cleanup.
    /// </summary>
    static bool IsHeavyAssetType(AssetType type) {
      return type == AssetType.Texture ||
             type == AssetType.Material ||
             type == AssetType.Model;
    }

    public IEnumerable<OptimizationIssue> AnalyzeAsset(string assetPath) {
      return AnalyzeAssetCached(assetPath);
    }

    IEnumerable<OptimizationIssue> AnalyzeAssetCached(string assetPath) {
      // Check cache first
      if (_assetIssueCache.TryGetValue(assetPath, out var cachedIssues))
        return cachedIssues;

      if (!_graph.TryGetNode(assetPath, out var node))
        return System.Array.Empty<OptimizationIssue>();

      var issues = new List<OptimizationIssue>();
      foreach (var rule in _rules) {
        issues.AddRange(rule.Evaluate(node, _graph));
      }

      _assetIssueCache[assetPath] = issues;
      return issues;
    }
  }

  public class OptimizationReport {
    public List<OptimizationIssue> Issues { get; set; } = new();
    public int TotalIssues { get; set; }
    public long TotalPotentialSavings { get; set; }
    public Dictionary<OptimizationSeverity, int> IssuesBySeverity { get; set; } = new();

    public string FormattedSavings => AssetNodeModel.FormatBytes(TotalPotentialSavings);

    public int ErrorCount => IssuesBySeverity.GetValueOrDefault(OptimizationSeverity.Error);
    public int WarningCount => IssuesBySeverity.GetValueOrDefault(OptimizationSeverity.Warning);
    public int InfoCount => IssuesBySeverity.GetValueOrDefault(OptimizationSeverity.Info);
  }
}
