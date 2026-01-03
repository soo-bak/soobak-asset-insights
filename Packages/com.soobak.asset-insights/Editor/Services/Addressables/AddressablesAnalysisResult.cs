using System.Collections.Generic;
using System.Linq;

namespace Soobak.AssetInsights {
  /// <summary>
  /// Contains the complete analysis result for Addressables.
  /// </summary>
  public class AddressablesAnalysisResult {
    public List<AddressableGroupModel> Groups { get; } = new();
    public List<AddressableDuplicateInfo> DuplicateAssets { get; } = new();
    public Dictionary<(string sourceGroup, string targetGroup), List<CrossGroupDependency>> CrossGroupDependencies { get; } = new();
    public List<ImplicitDependencyInfo> ImplicitDependencies { get; } = new();

    public int TotalGroups => Groups.Count;
    public int TotalEntries => Groups.Sum(g => g.Entries.Count);
    public long TotalSizeBytes => Groups.Sum(g => g.TotalSizeBytes);
    public int TotalDuplicates => DuplicateAssets.Count;
    public int TotalCrossGroupDependencies => CrossGroupDependencies.Values.Sum(l => l.Count);
    public int TotalImplicitDependencies => ImplicitDependencies.Count;

    public string FormattedTotalSize => AssetNodeModel.FormatBytes(TotalSizeBytes);
  }

  /// <summary>
  /// Represents an Addressable asset group.
  /// </summary>
  public class AddressableGroupModel {
    public string Name { get; set; }
    public List<AddressableEntryModel> Entries { get; } = new();
    public long TotalSizeBytes { get; set; }

    public int EntryCount => Entries.Count;
    public string FormattedSize => AssetNodeModel.FormatBytes(TotalSizeBytes);
  }

  /// <summary>
  /// Represents a single Addressable asset entry.
  /// </summary>
  public class AddressableEntryModel {
    public string AssetPath { get; set; }
    public string Address { get; set; }
    public string GroupName { get; set; }
    public List<string> Labels { get; set; } = new();
    public long SizeBytes { get; set; }
    public List<string> Dependencies { get; set; } = new();

    public string AssetName => System.IO.Path.GetFileNameWithoutExtension(AssetPath);
    public string FormattedSize => AssetNodeModel.FormatBytes(SizeBytes);
  }

  /// <summary>
  /// Information about an asset that appears in multiple groups.
  /// </summary>
  public class AddressableDuplicateInfo {
    public string AssetPath { get; set; }
    public List<string> GroupNames { get; set; } = new();

    public string AssetName => System.IO.Path.GetFileNameWithoutExtension(AssetPath);
    public int GroupCount => GroupNames.Count;
  }

  /// <summary>
  /// Represents a dependency that crosses group boundaries.
  /// </summary>
  public class CrossGroupDependency {
    public string SourceAsset { get; set; }
    public string SourceGroup { get; set; }
    public string TargetAsset { get; set; }
    public string TargetGroup { get; set; }

    public string SourceAssetName => System.IO.Path.GetFileNameWithoutExtension(SourceAsset);
    public string TargetAssetName => System.IO.Path.GetFileNameWithoutExtension(TargetAsset);
  }

  /// <summary>
  /// Information about an asset that is not explicitly in any group
  /// but is referenced by an Addressable asset.
  /// </summary>
  public class ImplicitDependencyInfo {
    public string AssetPath { get; set; }
    public string ReferencedBy { get; set; }
    public string ReferencedByGroup { get; set; }

    public string AssetName => System.IO.Path.GetFileNameWithoutExtension(AssetPath);
    public string ReferencedByName => System.IO.Path.GetFileNameWithoutExtension(ReferencedBy);
  }
}
