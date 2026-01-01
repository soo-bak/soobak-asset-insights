using UnityEditor;
using UnityEngine;

namespace Soobak.AssetInsights {
  public class DependencyGraphWindow : EditorWindow {
    DependencyGraph _graph;
    string _assetPath;
    DependencyGraphView _graphView;

    public static void Show(DependencyGraph graph, string assetPath) {
      var window = GetWindow<DependencyGraphWindow>();
      window._graph = graph;
      window._assetPath = assetPath;
      window.titleContent = new GUIContent($"Graph: {System.IO.Path.GetFileName(assetPath)}");
      window.minSize = new Vector2(600, 400);
      window.BuildGraph();
    }

    void BuildGraph() {
      if (_graph == null || string.IsNullOrEmpty(_assetPath))
        return;

      rootVisualElement.Clear();

      _graphView = new DependencyGraphView(_graph);
      _graphView.OnNodeDoubleClicked += path => {
        EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<Object>(path));
      };
      rootVisualElement.Add(_graphView);

      _graphView.ShowAssetGraph(_assetPath, 2);
    }
  }
}
