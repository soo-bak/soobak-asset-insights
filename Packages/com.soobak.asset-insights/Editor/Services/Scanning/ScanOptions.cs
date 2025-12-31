using System.Collections.Generic;

namespace Soobak.AssetInsights {
  public class ScanOptions {
    public HashSet<AssetType> IncludeTypes { get; set; }
    public HashSet<AssetType> ExcludeTypes { get; set; }
    public HashSet<string> IncludePaths { get; set; }
    public HashSet<string> ExcludePaths { get; set; }
    public long MinFileSize { get; set; }
    public bool IncludePackages { get; set; }

    public ScanOptions() {
      IncludeTypes = new HashSet<AssetType>();
      ExcludeTypes = new HashSet<AssetType>();
      IncludePaths = new HashSet<string>();
      ExcludePaths = new HashSet<string>();
      MinFileSize = 0;
      IncludePackages = false;
    }

    public static ScanOptions Default => new ScanOptions();

    public bool ShouldInclude(AssetNodeModel node) {
      if (ExcludeTypes.Count > 0 && ExcludeTypes.Contains(node.Type))
        return false;

      if (IncludeTypes.Count > 0 && !IncludeTypes.Contains(node.Type))
        return false;

      if (MinFileSize > 0 && node.FileSize < MinFileSize)
        return false;

      if (!IncludePackages && node.Path.StartsWith("Packages/"))
        return false;

      foreach (var excludePath in ExcludePaths) {
        if (node.Path.StartsWith(excludePath))
          return false;
      }

      if (IncludePaths.Count > 0) {
        bool matched = false;
        foreach (var includePath in IncludePaths) {
          if (node.Path.StartsWith(includePath)) {
            matched = true;
            break;
          }
        }
        if (!matched)
          return false;
      }

      return true;
    }
  }
}
