using System.Collections.Generic;

namespace Soobak.AssetInsights {
  public class ReportOptions {
    public ReportFormat Format { get; set; }
    public int TopHeavyHittersCount { get; set; }
    public bool IncludeDependencyPaths { get; set; }
    public bool IncludeSizeBreakdown { get; set; }
    public HashSet<AssetType> FilterTypes { get; set; }
    public string TargetAsset { get; set; }

    public ReportOptions() {
      Format = ReportFormat.Markdown;
      TopHeavyHittersCount = 20;
      IncludeDependencyPaths = true;
      IncludeSizeBreakdown = true;
      FilterTypes = new HashSet<AssetType>();
      TargetAsset = null;
    }

    public static ReportOptions Default => new ReportOptions();
  }
}
