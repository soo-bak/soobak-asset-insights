using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Soobak.AssetInsights {
  public class WhyIncludedWindow : EditorWindow {
    string _targetAsset;
    DependencyGraph _graph;
    IReportExporter _exporter;
    List<List<string>> _paths;

    ScrollView _pathsContainer;
    Label _titleLabel;

    public static void ShowWindow(string targetAsset, DependencyGraph graph) {
      var window = GetWindow<WhyIncludedWindow>();
      window.titleContent = new GUIContent("Why Included");
      window.minSize = new Vector2(400, 300);
      window._targetAsset = targetAsset;
      window._graph = graph;
      window.RefreshPaths();
    }

    void OnEnable() {
      _exporter = new ReportExporter();
    }

    void CreateGUI() {
      var root = rootVisualElement;
      root.style.paddingTop = 8;
      root.style.paddingBottom = 8;
      root.style.paddingLeft = 8;
      root.style.paddingRight = 8;

      _titleLabel = new Label();
      _titleLabel.style.fontSize = 14;
      _titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
      _titleLabel.style.marginBottom = 8;
      root.Add(_titleLabel);

      _pathsContainer = new ScrollView();
      _pathsContainer.style.flexGrow = 1;
      root.Add(_pathsContainer);

      var buttonContainer = new VisualElement();
      buttonContainer.style.flexDirection = FlexDirection.Row;
      buttonContainer.style.marginTop = 8;

      var exportButton = new Button(ExportReport) { text = "Export Report" };
      exportButton.style.flexGrow = 1;
      buttonContainer.Add(exportButton);

      var copyButton = new Button(CopyToClipboard) { text = "Copy" };
      copyButton.style.width = 60;
      buttonContainer.Add(copyButton);

      root.Add(buttonContainer);

      if (!string.IsNullOrEmpty(_targetAsset))
        RefreshPaths();
    }

    void RefreshPaths() {
      if (_graph == null || string.IsNullOrEmpty(_targetAsset))
        return;

      _paths = PathFinder.FindWhyIncluded(_graph, _targetAsset);

      if (_titleLabel != null)
        _titleLabel.text = $"Why is '{System.IO.Path.GetFileName(_targetAsset)}' included?";

      if (_pathsContainer == null)
        return;

      _pathsContainer.Clear();

      if (_paths.Count == 0) {
        var noPathLabel = new Label("No dependency paths found. This asset may be a root asset.");
        noPathLabel.style.color = new Color(0.6f, 0.6f, 0.6f);
        _pathsContainer.Add(noPathLabel);
        return;
      }

      var summaryLabel = new Label($"Found {_paths.Count} dependency path(s):");
      summaryLabel.style.marginBottom = 8;
      _pathsContainer.Add(summaryLabel);

      for (int i = 0; i < _paths.Count; i++) {
        var path = _paths[i];
        var pathContainer = CreatePathElement(i + 1, path);
        _pathsContainer.Add(pathContainer);
      }
    }

    VisualElement CreatePathElement(int index, List<string> path) {
      var container = new VisualElement();
      container.style.marginBottom = 12;
      container.style.paddingLeft = 8;
      container.style.paddingRight = 8;
      container.style.paddingTop = 8;
      container.style.paddingBottom = 8;
      container.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.3f);
      container.style.borderTopLeftRadius = 4;
      container.style.borderTopRightRadius = 4;
      container.style.borderBottomLeftRadius = 4;
      container.style.borderBottomRightRadius = 4;

      var header = new Label($"Path {index}");
      header.style.unityFontStyleAndWeight = FontStyle.Bold;
      header.style.marginBottom = 4;
      container.Add(header);

      for (int i = 0; i < path.Count; i++) {
        var assetPath = path[i];
        var isLast = i == path.Count - 1;

        var itemContainer = new VisualElement();
        itemContainer.style.flexDirection = FlexDirection.Row;
        itemContainer.style.alignItems = Align.Center;
        itemContainer.style.marginLeft = i * 16;

        var arrow = new Label(i > 0 ? "\u2514\u2500 " : "");
        arrow.style.width = i > 0 ? 24 : 0;
        arrow.style.color = new Color(0.5f, 0.5f, 0.5f);
        itemContainer.Add(arrow);

        var assetName = System.IO.Path.GetFileName(assetPath);
        var button = new Button(() => PingAsset(assetPath)) { text = assetName };
        button.style.backgroundColor = Color.clear;
        button.style.borderTopWidth = 0;
        button.style.borderBottomWidth = 0;
        button.style.borderLeftWidth = 0;
        button.style.borderRightWidth = 0;
        button.style.color = isLast ? new Color(1f, 0.8f, 0.3f) : Color.white;

        if (isLast)
          button.style.unityFontStyleAndWeight = FontStyle.Bold;

        itemContainer.Add(button);

        container.Add(itemContainer);
      }

      return container;
    }

    void PingAsset(string path) {
      var asset = AssetDatabase.LoadAssetAtPath<Object>(path);
      if (asset != null)
        EditorGUIUtility.PingObject(asset);
    }

    void ExportReport() {
      if (_graph == null || string.IsNullOrEmpty(_targetAsset))
        return;

      var report = _exporter.ExportWhyIncluded(_graph, _targetAsset);
      var path = EditorUtility.SaveFilePanel("Export Why Included", "", "why-included.md", "md");

      if (!string.IsNullOrEmpty(path)) {
        System.IO.File.WriteAllText(path, report);
        EditorUtility.RevealInFinder(path);
      }
    }

    void CopyToClipboard() {
      if (_graph == null || string.IsNullOrEmpty(_targetAsset))
        return;

      var report = _exporter.ExportWhyIncluded(_graph, _targetAsset);
      EditorGUIUtility.systemCopyBuffer = report;
      Debug.Log("Report copied to clipboard");
    }
  }
}
