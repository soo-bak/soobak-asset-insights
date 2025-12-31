using System.Collections.Generic;

namespace Soobak.AssetInsights {
  public interface IOptimizationRule {
    string RuleName { get; }
    string Description { get; }
    OptimizationSeverity Severity { get; }

    IEnumerable<OptimizationIssue> Evaluate(AssetNodeModel node, DependencyGraph graph);
  }

  public class OptimizationIssue {
    public string RuleName { get; set; }
    public string AssetPath { get; set; }
    public string AssetName { get; set; }
    public string Message { get; set; }
    public string Recommendation { get; set; }
    public OptimizationSeverity Severity { get; set; }
    public long PotentialSavings { get; set; }
    public bool IsAutoFixable { get; set; }

    public string FormattedSavings => AssetNodeModel.FormatBytes(PotentialSavings);
  }

  public enum OptimizationSeverity {
    Info,
    Warning,
    Error
  }
}
