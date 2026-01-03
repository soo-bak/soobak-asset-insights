using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;

namespace Soobak.AssetInsights {
  /// <summary>
  /// Scans Addressable asset groups and entries using reflection.
  /// Works without hard dependency on the Addressables package.
  /// </summary>
  public class AddressablesScanner {
    readonly DependencyGraph _graph;

    public AddressablesScanner(DependencyGraph graph) {
      _graph = graph;
    }

    /// <summary>
    /// Scans all Addressable groups and returns analysis result.
    /// Returns null if Addressables is not installed or not configured.
    /// </summary>
    public AddressablesAnalysisResult Scan() {
      if (!AddressablesDetector.IsInstalled)
        return null;

      var settings = AddressablesDetector.GetSettings();
      if (settings == null)
        return null;

      var result = new AddressablesAnalysisResult();

      try {
        // Get groups collection via reflection
        var groupsProperty = settings.GetType().GetProperty("groups");
        if (groupsProperty == null)
          return null;

        var groups = groupsProperty.GetValue(settings) as System.Collections.IList;
        if (groups == null)
          return null;

        foreach (var group in groups) {
          if (group == null)
            continue;

          var groupModel = ScanGroup(group);
          if (groupModel != null) {
            result.Groups.Add(groupModel);
          }
        }

        // Analyze for duplicates and cross-group dependencies
        AnalyzeDuplicates(result);
        AnalyzeCrossGroupDependencies(result);
      } catch (Exception e) {
        UnityEngine.Debug.LogWarning($"[Asset Insights] Error scanning Addressables: {e.Message}");
        return null;
      }

      return result;
    }

    AddressableGroupModel ScanGroup(object group) {
      try {
        var nameProperty = group.GetType().GetProperty("Name");
        var entriesProperty = group.GetType().GetProperty("entries");

        if (nameProperty == null || entriesProperty == null)
          return null;

        var groupName = nameProperty.GetValue(group) as string ?? "Unknown";
        var entries = entriesProperty.GetValue(group) as System.Collections.IList;

        var model = new AddressableGroupModel {
          Name = groupName
        };

        if (entries != null) {
          foreach (var entry in entries) {
            var entryModel = ScanEntry(entry, groupName);
            if (entryModel != null) {
              model.Entries.Add(entryModel);
              model.TotalSizeBytes += entryModel.SizeBytes;
            }
          }
        }

        return model;
      } catch {
        return null;
      }
    }

    AddressableEntryModel ScanEntry(object entry, string groupName) {
      try {
        var assetPathProperty = entry.GetType().GetProperty("AssetPath");
        var addressProperty = entry.GetType().GetProperty("address");
        var labelsProperty = entry.GetType().GetProperty("labels");

        if (assetPathProperty == null)
          return null;

        var assetPath = assetPathProperty.GetValue(entry) as string;
        if (string.IsNullOrEmpty(assetPath))
          return null;

        var address = addressProperty?.GetValue(entry) as string ?? assetPath;
        var labels = new List<string>();

        if (labelsProperty != null) {
          var labelsValue = labelsProperty.GetValue(entry) as System.Collections.IEnumerable;
          if (labelsValue != null) {
            foreach (var label in labelsValue) {
              if (label != null)
                labels.Add(label.ToString());
            }
          }
        }

        // Calculate size
        long sizeBytes = 0;
        if (File.Exists(assetPath)) {
          sizeBytes = new FileInfo(assetPath).Length;
        }

        // Get dependencies from the graph if available
        var dependencies = new List<string>();
        if (_graph.TryGetNode(assetPath, out _)) {
          var deps = _graph.GetDependencies(assetPath);
          if (deps != null)
            dependencies.AddRange(deps);
        }

        return new AddressableEntryModel {
          AssetPath = assetPath,
          Address = address,
          GroupName = groupName,
          Labels = labels,
          SizeBytes = sizeBytes,
          Dependencies = dependencies
        };
      } catch {
        return null;
      }
    }

    void AnalyzeDuplicates(AddressablesAnalysisResult result) {
      // Find assets that appear in multiple groups
      var assetToGroups = new Dictionary<string, List<string>>();

      foreach (var group in result.Groups) {
        foreach (var entry in group.Entries) {
          if (!assetToGroups.TryGetValue(entry.AssetPath, out var groupList)) {
            groupList = new List<string>();
            assetToGroups[entry.AssetPath] = groupList;
          }
          groupList.Add(group.Name);
        }
      }

      foreach (var kvp in assetToGroups) {
        if (kvp.Value.Count > 1) {
          result.DuplicateAssets.Add(new AddressableDuplicateInfo {
            AssetPath = kvp.Key,
            GroupNames = kvp.Value
          });
        }
      }
    }

    void AnalyzeCrossGroupDependencies(AddressablesAnalysisResult result) {
      // Build a map of asset path to group name
      var assetToGroup = new Dictionary<string, string>();
      foreach (var group in result.Groups) {
        foreach (var entry in group.Entries) {
          assetToGroup[entry.AssetPath] = group.Name;
        }
      }

      // Find dependencies that cross group boundaries
      foreach (var group in result.Groups) {
        foreach (var entry in group.Entries) {
          foreach (var dep in entry.Dependencies) {
            if (assetToGroup.TryGetValue(dep, out var depGroup)) {
              if (depGroup != group.Name) {
                // Cross-group dependency found
                var key = (group.Name, depGroup);
                if (!result.CrossGroupDependencies.TryGetValue(key, out var deps)) {
                  deps = new List<CrossGroupDependency>();
                  result.CrossGroupDependencies[key] = deps;
                }
                deps.Add(new CrossGroupDependency {
                  SourceAsset = entry.AssetPath,
                  SourceGroup = group.Name,
                  TargetAsset = dep,
                  TargetGroup = depGroup
                });
              }
            } else {
              // This dependency is not explicitly in any group (implicit dependency)
              result.ImplicitDependencies.Add(new ImplicitDependencyInfo {
                AssetPath = dep,
                ReferencedBy = entry.AssetPath,
                ReferencedByGroup = group.Name
              });
            }
          }
        }
      }
    }
  }
}
