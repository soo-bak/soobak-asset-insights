using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Soobak.AssetInsights {
  public class AssetPreviewPanel : VisualElement {
    readonly DependencyGraph _graph;

    VisualElement _previewContainer;
    Image _previewImage;
    Label _nameLabel;
    Label _pathLabel;
    Label _typeLabel;
    Label _sizeLabel;
    VisualElement _detailsContainer;
    VisualElement _depsContainer;
    VisualElement _refsContainer;
    VisualElement _issuesContainer;

    string _currentPath;
    Editor _previewEditor;

    // Cached optimization engine - passed from parent to avoid duplicate creation
    OptimizationEngine _cachedEngine;

    public AssetPreviewPanel(DependencyGraph graph) {
      _graph = graph;
      BuildUI();
    }

    public void SetOptimizationEngine(OptimizationEngine engine) {
      _cachedEngine = engine;
    }

    void BuildUI() {
      style.width = 280;
      style.minWidth = 200;
      style.borderLeftWidth = 1;
      style.borderLeftColor = new Color(0.2f, 0.2f, 0.2f);
      style.paddingLeft = 8;
      style.paddingRight = 8;
      style.paddingTop = 8;

      var scrollView = new ScrollView(ScrollViewMode.Vertical);
      scrollView.style.flexGrow = 1;
      Add(scrollView);

      // Preview image
      _previewContainer = new VisualElement();
      _previewContainer.style.height = 150;
      _previewContainer.style.backgroundColor = new Color(0.15f, 0.15f, 0.15f);
      _previewContainer.style.borderTopLeftRadius = 4;
      _previewContainer.style.borderTopRightRadius = 4;
      _previewContainer.style.borderBottomLeftRadius = 4;
      _previewContainer.style.borderBottomRightRadius = 4;
      _previewContainer.style.marginBottom = 8;
      _previewContainer.style.alignItems = Align.Center;
      _previewContainer.style.justifyContent = Justify.Center;

      _previewImage = new Image();
      _previewImage.style.maxWidth = Length.Percent(100);
      _previewImage.style.maxHeight = Length.Percent(100);
      _previewImage.scaleMode = ScaleMode.ScaleToFit;
      _previewContainer.Add(_previewImage);

      scrollView.Add(_previewContainer);

      // Basic info
      _nameLabel = new Label();
      _nameLabel.style.fontSize = 14;
      _nameLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
      _nameLabel.style.whiteSpace = WhiteSpace.Normal;
      _nameLabel.style.marginBottom = 4;
      scrollView.Add(_nameLabel);

      _pathLabel = new Label();
      _pathLabel.style.fontSize = 10;
      _pathLabel.style.color = new Color(0.6f, 0.6f, 0.6f);
      _pathLabel.style.whiteSpace = WhiteSpace.Normal;
      _pathLabel.style.marginBottom = 8;
      scrollView.Add(_pathLabel);

      // Type and size row
      var infoRow = new VisualElement();
      infoRow.style.flexDirection = FlexDirection.Row;
      infoRow.style.marginBottom = 12;

      _typeLabel = new Label();
      _typeLabel.style.flexGrow = 1;
      infoRow.Add(_typeLabel);

      _sizeLabel = new Label();
      _sizeLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
      infoRow.Add(_sizeLabel);

      scrollView.Add(infoRow);

      // Details sections
      _detailsContainer = new VisualElement();
      scrollView.Add(_detailsContainer);

      // Show placeholder
      ShowPlaceholder();
    }

    void ShowPlaceholder() {
      _previewImage.image = null;
      _nameLabel.text = "Select an asset";
      _pathLabel.text = "Click on an asset in the list to see details";
      _typeLabel.text = "";
      _sizeLabel.text = "";
      _detailsContainer.Clear();
    }

    public void ShowAsset(string assetPath) {
      if (string.IsNullOrEmpty(assetPath)) {
        ShowPlaceholder();
        return;
      }

      _currentPath = assetPath;

      if (!_graph.TryGetNode(assetPath, out var node)) {
        ShowPlaceholder();
        return;
      }

      // Update preview image
      UpdatePreviewImage(assetPath);

      // Basic info
      _nameLabel.text = node.Name;
      _pathLabel.text = System.IO.Path.GetDirectoryName(assetPath);
      _typeLabel.text = node.Type.ToString();
      _sizeLabel.text = node.FormattedSize;

      // Clear and rebuild details
      _detailsContainer.Clear();

      // Dependencies section
      var deps = _graph.GetDependencies(assetPath);
      _detailsContainer.Add(CreateCollapsibleSection(
        $"Dependencies ({deps.Count})",
        deps.Take(10).ToList(),
        deps.Count > 10 ? $"... and {deps.Count - 10} more" : null
      ));

      // Dependents section
      var refs = _graph.GetDependents(assetPath);
      _detailsContainer.Add(CreateCollapsibleSection(
        $"Referenced By ({refs.Count})",
        refs.Take(10).ToList(),
        refs.Count > 10 ? $"... and {refs.Count - 10} more" : null
      ));

      // Optimization issues - use cached engine to avoid duplicate creation
      if (_cachedEngine != null) {
        var issues = _cachedEngine.AnalyzeAsset(assetPath).ToList();
        if (issues.Count > 0) {
          _detailsContainer.Add(CreateIssuesSection(issues));
        }
      }

      // Action buttons
      _detailsContainer.Add(CreateActionButtons(assetPath));
    }

    void UpdatePreviewImage(string assetPath) {
      // Clean up previous editor
      if (_previewEditor != null) {
        Object.DestroyImmediate(_previewEditor);
        _previewEditor = null;
      }

      // Clear previous image reference to allow GC
      _previewImage.image = null;

      var asset = AssetDatabase.LoadAssetAtPath<Object>(assetPath);
      if (asset == null) {
        return;
      }

      // Try to get asset preview - these are cached by Unity internally
      // We just display them, Unity manages the texture lifecycle
      var preview = AssetPreview.GetAssetPreview(asset);
      if (preview != null) {
        _previewImage.image = preview;
      } else {
        // Fallback to mini thumbnail
        var icon = AssetPreview.GetMiniThumbnail(asset);
        _previewImage.image = icon;
      }
    }

    VisualElement CreateCollapsibleSection(string title, System.Collections.Generic.List<string> paths, string moreText) {
      var section = new VisualElement();
      section.style.marginBottom = 8;
      section.style.paddingTop = 6;
      section.style.paddingBottom = 6;
      section.style.paddingLeft = 8;
      section.style.paddingRight = 8;
      section.style.backgroundColor = new Color(0.18f, 0.18f, 0.18f);
      section.style.borderTopLeftRadius = 4;
      section.style.borderTopRightRadius = 4;
      section.style.borderBottomLeftRadius = 4;
      section.style.borderBottomRightRadius = 4;

      var header = new Label(title);
      header.style.unityFontStyleAndWeight = FontStyle.Bold;
      header.style.fontSize = 11;
      header.style.marginBottom = 4;
      section.Add(header);

      if (paths.Count == 0) {
        var empty = new Label("None");
        empty.style.color = new Color(0.5f, 0.5f, 0.5f);
        empty.style.fontStyleAndWeight = FontStyle.Italic;
        empty.style.fontSize = 10;
        section.Add(empty);
      } else {
        foreach (var path in paths) {
          var row = CreateAssetRow(path);
          section.Add(row);
        }

        if (!string.IsNullOrEmpty(moreText)) {
          var more = new Label(moreText);
          more.style.color = new Color(0.5f, 0.5f, 0.5f);
          more.style.fontSize = 10;
          more.style.marginTop = 4;
          section.Add(more);
        }
      }

      return section;
    }

    VisualElement CreateAssetRow(string assetPath) {
      var row = new VisualElement();
      row.style.flexDirection = FlexDirection.Row;
      row.style.alignItems = Align.Center;
      row.style.paddingTop = 2;
      row.style.paddingBottom = 2;
      row.style.paddingLeft = 2;
      row.style.paddingRight = 2;
      row.style.borderTopLeftRadius = 2;
      row.style.borderTopRightRadius = 2;
      row.style.borderBottomLeftRadius = 2;
      row.style.borderBottomRightRadius = 2;

      row.RegisterCallback<MouseEnterEvent>(e => row.style.backgroundColor = new Color(0.25f, 0.25f, 0.25f));
      row.RegisterCallback<MouseLeaveEvent>(e => row.style.backgroundColor = StyleKeyword.Null);
      row.RegisterCallback<ClickEvent>(e => {
        var asset = AssetDatabase.LoadAssetAtPath<Object>(assetPath);
        if (asset != null) {
          EditorGUIUtility.PingObject(asset);
          Selection.activeObject = asset;
        }
      });

      var icon = new Image();
      icon.image = AssetDatabase.GetCachedIcon(assetPath);
      icon.style.width = 14;
      icon.style.height = 14;
      icon.style.marginRight = 4;
      row.Add(icon);

      var name = System.IO.Path.GetFileNameWithoutExtension(assetPath);
      var nameLabel = new Label(name);
      nameLabel.style.fontSize = 10;
      nameLabel.style.overflow = Overflow.Hidden;
      nameLabel.style.textOverflow = TextOverflow.Ellipsis;
      nameLabel.style.flexGrow = 1;
      row.Add(nameLabel);

      return row;
    }

    VisualElement CreateIssuesSection(System.Collections.Generic.List<OptimizationIssue> issues) {
      var section = new VisualElement();
      section.style.marginBottom = 8;
      section.style.paddingTop = 6;
      section.style.paddingBottom = 6;
      section.style.paddingLeft = 8;
      section.style.paddingRight = 8;
      section.style.backgroundColor = new Color(0.2f, 0.15f, 0.15f);
      section.style.borderTopLeftRadius = 4;
      section.style.borderTopRightRadius = 4;
      section.style.borderBottomLeftRadius = 4;
      section.style.borderBottomRightRadius = 4;

      var header = new Label($"Issues ({issues.Count})");
      header.style.unityFontStyleAndWeight = FontStyle.Bold;
      header.style.fontSize = 11;
      header.style.marginBottom = 4;
      header.style.color = new Color(1f, 0.7f, 0.7f);
      section.Add(header);

      foreach (var issue in issues.Take(5)) {
        var issueRow = new VisualElement();
        issueRow.style.marginBottom = 4;

        var severityColor = issue.Severity switch {
          OptimizationSeverity.Error => new Color(1f, 0.4f, 0.4f),
          OptimizationSeverity.Warning => new Color(1f, 0.7f, 0.4f),
          _ => new Color(0.7f, 0.7f, 0.7f)
        };

        var ruleLabel = new Label(issue.RuleName);
        ruleLabel.style.fontSize = 10;
        ruleLabel.style.color = severityColor;
        ruleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        issueRow.Add(ruleLabel);

        var msgLabel = new Label(issue.Message);
        msgLabel.style.fontSize = 9;
        msgLabel.style.color = new Color(0.6f, 0.6f, 0.6f);
        msgLabel.style.whiteSpace = WhiteSpace.Normal;
        issueRow.Add(msgLabel);

        section.Add(issueRow);
      }

      return section;
    }

    VisualElement CreateActionButtons(string assetPath) {
      var container = new VisualElement();
      container.style.marginTop = 8;
      container.style.flexDirection = FlexDirection.Row;

      var graphBtn = new Button(() => {
        DependencyGraphWindow.Show(_graph, assetPath);
      }) { text = "Show Graph" };
      graphBtn.style.flexGrow = 1;
      container.Add(graphBtn);

      var selectBtn = new Button(() => {
        var asset = AssetDatabase.LoadAssetAtPath<Object>(assetPath);
        if (asset != null) {
          Selection.activeObject = asset;
          EditorGUIUtility.PingObject(asset);
        }
      }) { text = "Select" };
      selectBtn.style.flexGrow = 1;
      container.Add(selectBtn);

      return container;
    }

    public void Cleanup() {
      // Clean up editor
      if (_previewEditor != null) {
        Object.DestroyImmediate(_previewEditor);
        _previewEditor = null;
      }

      // Clear image reference to allow texture GC
      if (_previewImage != null) {
        _previewImage.image = null;
      }

      // Clear cached references
      _cachedEngine = null;
      _currentPath = null;

      // Clear details container
      _detailsContainer?.Clear();
    }

    // Keep OnDisable for backward compatibility
    public void OnDisable() {
      Cleanup();
    }
  }
}
