using System.Collections.Generic;
using System.Linq;

namespace Soobak.AssetInsights {
  public class OptimizationEngine {
    readonly List<IOptimizationRule> _rules = new();
    readonly DependencyGraph _graph;

    public OptimizationEngine(DependencyGraph graph) {
      _graph = graph;
      RegisterDefaultRules();
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
      var report = new OptimizationReport();

      foreach (var node in _graph.Nodes.Values) {
        foreach (var rule in _rules) {
          var issues = rule.Evaluate(node, _graph);
          report.Issues.AddRange(issues);
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

      return report;
    }

    public IEnumerable<OptimizationIssue> AnalyzeAsset(string assetPath) {
      if (!_graph.TryGetNode(assetPath, out var node))
        yield break;

      foreach (var rule in _rules) {
        foreach (var issue in rule.Evaluate(node, _graph)) {
          yield return issue;
        }
      }
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
