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
    public FixType FixType { get; set; } = FixType.None;

    public string FormattedSavings => AssetNodeModel.FormatBytes(PotentialSavings);
  }

  public enum OptimizationSeverity {
    Info,
    Warning,
    Error
  }

  /// <summary>
  /// Types of automatic fixes that can be applied to assets.
  /// </summary>
  public enum FixType {
    None,

    // Texture fixes
    TextureEnableCompression,
    TextureDisableMipmaps,

    // Audio fixes
    AudioEnableCompression,
    AudioEnableStreaming,
    AudioForceToMono,

    // Material fixes
    MaterialEnableGPUInstancing,

    // Mesh fixes
    MeshDisableReadWrite,
    MeshEnableCompression,
    MeshDisableAnimation,
    MeshDisableTangents
  }
}
