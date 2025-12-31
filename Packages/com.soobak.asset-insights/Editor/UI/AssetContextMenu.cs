using UnityEditor;
using UnityEngine;

namespace Soobak.AssetInsights {
  public static class AssetContextMenu {
    static DependencyScanner _cachedScanner;

    [MenuItem("Assets/Asset Insights/Why Included?", false, 1000)]
    static void ShowWhyIncluded() {
      var selected = Selection.activeObject;
      if (selected == null)
        return;

      var path = AssetDatabase.GetAssetPath(selected);
      if (string.IsNullOrEmpty(path))
        return;

      EnsureGraphScanned();

      if (_cachedScanner.Graph.NodeCount == 0) {
        EditorUtility.DisplayDialog("No Data",
          "Please scan the project first using Window > Asset Insights.", "OK");
        return;
      }

      WhyIncludedWindow.ShowWindow(path, _cachedScanner.Graph);
    }

    [MenuItem("Assets/Asset Insights/Why Included?", true)]
    static bool ValidateWhyIncluded() {
      return Selection.activeObject != null;
    }

    [MenuItem("Assets/Asset Insights/Show Dependencies", false, 1001)]
    static void ShowDependencies() {
      var selected = Selection.activeObject;
      if (selected == null)
        return;

      var path = AssetDatabase.GetAssetPath(selected);
      if (string.IsNullOrEmpty(path))
        return;

      var deps = AssetDatabase.GetDependencies(path, true);
      var message = $"Dependencies of {System.IO.Path.GetFileName(path)}:\n\n";

      foreach (var dep in deps) {
        if (dep != path)
          message += $"  - {dep}\n";
      }

      if (deps.Length <= 1)
        message += "(No dependencies)";

      Debug.Log(message);
    }

    [MenuItem("Assets/Asset Insights/Show Dependencies", true)]
    static bool ValidateShowDependencies() {
      return Selection.activeObject != null;
    }

    static void EnsureGraphScanned() {
      if (_cachedScanner == null)
        _cachedScanner = new DependencyScanner();

      if (_cachedScanner.Graph.NodeCount == 0)
        _cachedScanner.ScanImmediate();
    }
  }
}
