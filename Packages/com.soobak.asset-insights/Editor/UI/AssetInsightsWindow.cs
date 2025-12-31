using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;


namespace Soobak.AssetInsights {
  public class AssetInsightsWindow : EditorWindow {
    IDependencyScanner _scanner;
    IReportExporter _exporter;
    IEnumerator _scanEnumerator;
    bool _isScanning;
    List<AssetNodeModel> _filteredNodes = new();
    string _selectedAssetPath;

    VisualElement _root;
    ProgressBar _progressBar;
    Label _statusLabel;
    Label _summaryLabel;
    ListView _assetList;
    TextField _searchField;
    Button _scanButton;
    Button _cancelButton;
    Button _exportButton;

    // View switching
    VisualElement _listContainer;
    VisualElement _graphContainer;
    VisualElement _dashboardContainer;
    DependencyGraphView _graphView;
    DashboardPanel _dashboardPanel;
    Button _listTabButton;
    Button _graphTabButton;
    Button _dashboardTabButton;
    Button _showGraphButton;
    int _currentView; // 0=list, 1=graph, 2=dashboard

    [MenuItem("Window/Asset Insights")]
    public static void ShowWindow() {
      var window = GetWindow<AssetInsightsWindow>();
      window.titleContent = new GUIContent("Asset Insights");
      window.minSize = new Vector2(500, 400);
    }

    void OnEnable() {
      _scanner = new DependencyScanner();
      _exporter = new ReportExporter();
    }

    void OnDisable() {
      CancelScan();
    }

    void CreateGUI() {
      _root = rootVisualElement;
      _root.style.paddingTop = 8;
      _root.style.paddingBottom = 8;
      _root.style.paddingLeft = 8;
      _root.style.paddingRight = 8;

      CreateToolbar();
      CreateProgressSection();
      CreateViewTabs();
      CreateListView();
      CreateGraphView();
      CreateDashboardView();
      CreateExportSection();

      SwitchToView(0);
      UpdateUI();
    }

    void CreateViewTabs() {
      var tabContainer = new VisualElement();
      tabContainer.style.flexDirection = FlexDirection.Row;
      tabContainer.style.marginBottom = 8;
      tabContainer.style.flexShrink = 0;

      _listTabButton = new Button(() => SwitchToView(0)) { text = "List" };
      _listTabButton.style.flexGrow = 1;
      tabContainer.Add(_listTabButton);

      _graphTabButton = new Button(() => SwitchToView(1)) { text = "Graph" };
      _graphTabButton.style.flexGrow = 1;
      tabContainer.Add(_graphTabButton);

      _dashboardTabButton = new Button(() => SwitchToView(2)) { text = "Dashboard" };
      _dashboardTabButton.style.flexGrow = 1;
      tabContainer.Add(_dashboardTabButton);

      _root.Add(tabContainer);
    }

    void CreateDashboardView() {
      _dashboardContainer = new VisualElement();
      _dashboardContainer.style.flexGrow = 1;
      _dashboardContainer.style.flexShrink = 1;
      _dashboardContainer.style.display = DisplayStyle.None;

      _dashboardPanel = new DashboardPanel(_scanner.Graph);
      _dashboardContainer.Add(_dashboardPanel);

      _root.Add(_dashboardContainer);
    }

    void CreateListView() {
      _listContainer = new VisualElement();
      _listContainer.style.flexGrow = 1;
      _listContainer.style.flexShrink = 1;

      CreateSearchSection(_listContainer);
      CreateListHeader(_listContainer);
      CreateAssetList(_listContainer);

      _root.Add(_listContainer);
    }

    void CreateGraphView() {
      _graphContainer = new VisualElement();
      _graphContainer.style.flexGrow = 1;
      _graphContainer.style.flexShrink = 1;
      _graphContainer.style.display = DisplayStyle.None;

      var helpLabel = new Label("Select an asset from the list and click 'Show Graph' to visualize dependencies");
      helpLabel.name = "graph-help";
      helpLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
      helpLabel.style.color = new Color(0.5f, 0.5f, 0.5f);
      helpLabel.style.marginTop = 50;
      _graphContainer.Add(helpLabel);

      _root.Add(_graphContainer);
    }

    void SwitchToView(int view) {
      _currentView = view;

      _listContainer.style.display = view == 0 ? DisplayStyle.Flex : DisplayStyle.None;
      _graphContainer.style.display = view == 1 ? DisplayStyle.Flex : DisplayStyle.None;
      _dashboardContainer.style.display = view == 2 ? DisplayStyle.Flex : DisplayStyle.None;

      var activeColor = new Color(0.3f, 0.5f, 0.7f);
      _listTabButton.style.backgroundColor = view == 0 ? activeColor : StyleKeyword.Null;
      _graphTabButton.style.backgroundColor = view == 1 ? activeColor : StyleKeyword.Null;
      _dashboardTabButton.style.backgroundColor = view == 2 ? activeColor : StyleKeyword.Null;

      if (view == 1 && !string.IsNullOrEmpty(_selectedAssetPath))
        ShowAssetInGraph(_selectedAssetPath);

      if (view == 2)
        _dashboardPanel?.Refresh();
    }

    void ShowAssetInGraph(string assetPath) {
      if (_scanner.Graph.NodeCount == 0)
        return;

      var helpLabel = _graphContainer.Q<Label>("graph-help");
      if (helpLabel != null)
        helpLabel.style.display = DisplayStyle.None;

      if (_graphView == null) {
        _graphView = new DependencyGraphView(_scanner.Graph);
        _graphView.OnNodeDoubleClicked += path => {
          EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<Object>(path));
        };
        _graphContainer.Add(_graphView);
      }

      _graphView.ShowAssetGraph(assetPath, 2);
    }

    void CreateToolbar() {
      var toolbar = new VisualElement();
      toolbar.style.flexDirection = FlexDirection.Row;
      toolbar.style.marginBottom = 8;
      toolbar.style.flexShrink = 0;

      _scanButton = new Button(StartScan) { text = "Scan Project" };
      _scanButton.style.flexGrow = 1;
      toolbar.Add(_scanButton);

      _cancelButton = new Button(CancelScan) { text = "Cancel" };
      _cancelButton.style.width = 80;
      _cancelButton.SetEnabled(false);
      toolbar.Add(_cancelButton);

      _root.Add(toolbar);
    }

    void CreateProgressSection() {
      var container = new VisualElement();
      container.style.marginBottom = 8;
      container.style.flexShrink = 0;

      _progressBar = new ProgressBar();
      _progressBar.title = "Ready";
      _progressBar.style.height = 20;
      container.Add(_progressBar);

      _statusLabel = new Label();
      _statusLabel.style.fontSize = 10;
      _statusLabel.style.color = new Color(0.6f, 0.6f, 0.6f);
      container.Add(_statusLabel);

      _summaryLabel = new Label();
      _summaryLabel.style.fontSize = 11;
      _summaryLabel.style.marginTop = 4;
      _summaryLabel.style.whiteSpace = WhiteSpace.Normal;
      _summaryLabel.style.display = DisplayStyle.None;
      container.Add(_summaryLabel);

      _root.Add(container);
    }

    void CreateSearchSection(VisualElement parent) {
      var container = new VisualElement();
      container.style.flexDirection = FlexDirection.Row;
      container.style.marginBottom = 8;
      container.style.flexShrink = 0;

      _searchField = new TextField();
      _searchField.style.flexGrow = 1;
      _searchField.RegisterValueChangedCallback(OnSearchChanged);
      container.Add(_searchField);

      var placeholder = new Label("Search assets...");
      placeholder.style.position = Position.Absolute;
      placeholder.style.left = 4;
      placeholder.style.top = 2;
      placeholder.style.color = new Color(0.5f, 0.5f, 0.5f);
      placeholder.pickingMode = PickingMode.Ignore;
      _searchField.Add(placeholder);

      _searchField.RegisterCallback<FocusInEvent>(e => placeholder.style.display = DisplayStyle.None);
      _searchField.RegisterCallback<FocusOutEvent>(e => {
        if (string.IsNullOrEmpty(_searchField.value))
          placeholder.style.display = DisplayStyle.Flex;
      });

      _showGraphButton = new Button(OnShowGraphClicked) { text = "Show Graph" };
      _showGraphButton.style.width = 90;
      _showGraphButton.SetEnabled(false);
      container.Add(_showGraphButton);

      parent.Add(container);
    }

    void OnShowGraphClicked() {
      if (!string.IsNullOrEmpty(_selectedAssetPath)) {
        SwitchToView(1);
        ShowAssetInGraph(_selectedAssetPath);
      }
    }

    void CreateListHeader(VisualElement parent) {
      var header = new VisualElement();
      header.style.flexDirection = FlexDirection.Row;
      header.style.paddingLeft = 4;
      header.style.paddingRight = 4;
      header.style.marginBottom = 4;
      header.style.flexShrink = 0;
      header.style.borderBottomWidth = 1;
      header.style.borderBottomColor = new Color(0.3f, 0.3f, 0.3f);
      header.style.paddingBottom = 4;

      var assetHeader = new Label("Asset");
      assetHeader.style.flexGrow = 1;
      assetHeader.style.unityFontStyleAndWeight = FontStyle.Bold;
      header.Add(assetHeader);

      var refsHeader = new Label("Refs");
      refsHeader.style.width = 45;
      refsHeader.style.unityTextAlign = TextAnchor.MiddleRight;
      refsHeader.style.unityFontStyleAndWeight = FontStyle.Bold;
      refsHeader.tooltip = "Dependents (assets that reference this)";
      header.Add(refsHeader);

      var depsHeader = new Label("Deps");
      depsHeader.style.width = 45;
      depsHeader.style.unityTextAlign = TextAnchor.MiddleRight;
      depsHeader.style.unityFontStyleAndWeight = FontStyle.Bold;
      depsHeader.tooltip = "Dependencies (assets this references)";
      header.Add(depsHeader);

      var sizeHeader = new Label("Size");
      sizeHeader.style.width = 65;
      sizeHeader.style.unityTextAlign = TextAnchor.MiddleRight;
      sizeHeader.style.unityFontStyleAndWeight = FontStyle.Bold;
      header.Add(sizeHeader);

      parent.Add(header);
    }

    void CreateAssetList(VisualElement parent) {
      _assetList = new ListView();
      _assetList.style.flexGrow = 1;
      _assetList.style.flexShrink = 1;
      _assetList.style.minHeight = 100;
      _assetList.selectionType = SelectionType.Single;
      _assetList.makeItem = MakeAssetItem;
      _assetList.bindItem = BindAssetItem;
      _assetList.fixedItemHeight = 36;
      _assetList.selectionChanged += OnAssetSelected;

      parent.Add(_assetList);
    }

    void CreateExportSection() {
      var container = new VisualElement();
      container.style.flexDirection = FlexDirection.Row;
      container.style.marginTop = 8;
      container.style.flexShrink = 0;
      container.style.minHeight = 24;

      _exportButton = new Button(ShowExportMenu) { text = "Export Report" };
      _exportButton.style.flexGrow = 1;
      _exportButton.SetEnabled(false);
      container.Add(_exportButton);

      var heavyButton = new Button(ExportHeavyHitters) { text = "Heavy Hitters" };
      heavyButton.style.width = 100;
      container.Add(heavyButton);

      _root.Add(container);
    }

    VisualElement MakeAssetItem() {
      var item = new VisualElement();
      item.style.flexDirection = FlexDirection.Row;
      item.style.alignItems = Align.Center;
      item.style.paddingLeft = 4;
      item.style.height = 36;

      var icon = new Image { name = "icon" };
      icon.style.width = 20;
      icon.style.height = 20;
      icon.style.marginRight = 6;
      icon.style.flexShrink = 0;
      item.Add(icon);

      var nameContainer = new VisualElement();
      nameContainer.style.flexGrow = 1;
      nameContainer.style.overflow = Overflow.Hidden;

      var nameLabel = new Label { name = "name" };
      nameLabel.style.overflow = Overflow.Hidden;
      nameLabel.style.textOverflow = TextOverflow.Ellipsis;
      nameLabel.style.fontSize = 12;
      nameContainer.Add(nameLabel);

      var pathLabel = new Label { name = "path" };
      pathLabel.style.overflow = Overflow.Hidden;
      pathLabel.style.textOverflow = TextOverflow.Ellipsis;
      pathLabel.style.fontSize = 10;
      pathLabel.style.color = new Color(0.5f, 0.5f, 0.5f);
      nameContainer.Add(pathLabel);

      item.Add(nameContainer);

      var refsLabel = new Label { name = "refs" };
      refsLabel.style.width = 45;
      refsLabel.style.unityTextAlign = TextAnchor.MiddleRight;
      refsLabel.style.color = new Color(0.4f, 0.7f, 1f);
      refsLabel.style.flexShrink = 0;
      item.Add(refsLabel);

      var depsLabel = new Label { name = "deps" };
      depsLabel.style.width = 45;
      depsLabel.style.unityTextAlign = TextAnchor.MiddleRight;
      depsLabel.style.color = new Color(1f, 0.7f, 0.4f);
      depsLabel.style.flexShrink = 0;
      item.Add(depsLabel);

      var sizeLabel = new Label { name = "size" };
      sizeLabel.style.width = 65;
      sizeLabel.style.unityTextAlign = TextAnchor.MiddleRight;
      sizeLabel.style.flexShrink = 0;
      item.Add(sizeLabel);

      return item;
    }

    void BindAssetItem(VisualElement element, int index) {
      if (index >= _filteredNodes.Count)
        return;

      var node = _filteredNodes[index];

      var icon = element.Q<Image>("icon");
      var assetIcon = AssetDatabase.GetCachedIcon(node.Path);
      icon.image = assetIcon;

      var nameLabel = element.Q<Label>("name");
      nameLabel.text = node.Name;

      var pathLabel = element.Q<Label>("path");
      var folderPath = System.IO.Path.GetDirectoryName(node.Path);
      pathLabel.text = folderPath ?? "";

      var refsLabel = element.Q<Label>("refs");
      var refsCount = _scanner.Graph.GetDependents(node.Path).Count;
      refsLabel.text = refsCount > 0 ? refsCount.ToString() : "-";

      var depsLabel = element.Q<Label>("deps");
      var depsCount = _scanner.Graph.GetDependencies(node.Path).Count;
      depsLabel.text = depsCount > 0 ? depsCount.ToString() : "-";

      var sizeLabel = element.Q<Label>("size");
      sizeLabel.text = node.FormattedSize;
    }

    void OnSearchChanged(ChangeEvent<string> evt) {
      RefreshAssetList();
    }

    void OnAssetSelected(IEnumerable<object> selection) {
      _selectedAssetPath = null;

      foreach (var item in selection) {
        if (item is AssetNodeModel node) {
          _selectedAssetPath = node.Path;
          EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<Object>(node.Path));
          break;
        }
      }

      _showGraphButton?.SetEnabled(!string.IsNullOrEmpty(_selectedAssetPath));
    }

    void StartScan() {
      CancelScan();
      _scanner.Clear();
      _isScanning = true;

      var options = new ScanOptions();
      _scanEnumerator = _scanner.ScanAsync(options);

      _scanButton.SetEnabled(false);
      _cancelButton.SetEnabled(true);
      _summaryLabel.style.display = DisplayStyle.None;

      EditorApplication.update += OnScanUpdate;
      UpdateUI();
    }

    void OnScanUpdate() {
      if (!_isScanning || _scanEnumerator == null) {
        StopScanUpdate();
        return;
      }

      if (_scanEnumerator.MoveNext()) {
        var progress = _scanner.Progress;
        _progressBar.value = progress.Progress * 100f;
        _progressBar.title = $"Scanning... {progress.ProcessedCount}/{progress.TotalCount}";
        _statusLabel.text = progress.CurrentAsset ?? "";

        if (progress.IsCancelled) {
          StopScanUpdate();
        }
      } else {
        OnScanComplete();
      }
    }

    void OnScanComplete() {
      StopScanUpdate();

      var graph = _scanner.Graph;
      _progressBar.value = 100f;
      _progressBar.title = $"Complete - {graph.NodeCount} assets ({AssetNodeModel.FormatBytes(graph.GetTotalSize())})";
      _statusLabel.text = "";

      UpdateSummary();

      _scanButton.SetEnabled(true);
      _cancelButton.SetEnabled(false);
      _exportButton.SetEnabled(graph.NodeCount > 0);

      // Reset graph view for new scan data
      if (_graphView != null) {
        _graphContainer.Remove(_graphView);
        _graphView = null;
      }
      var helpLabel = _graphContainer?.Q<Label>("graph-help");
      if (helpLabel != null)
        helpLabel.style.display = DisplayStyle.Flex;

      // Refresh dashboard if visible
      if (_currentView == 2)
        _dashboardPanel?.Refresh();

      RefreshAssetList();
    }

    void UpdateSummary() {
      var byType = _scanner.Graph.GetSizeByType();
      var topTypes = byType
        .OrderByDescending(kv => kv.Value.totalSize)
        .Take(4)
        .Select(kv => $"{kv.Key}: {kv.Value.count}")
        .ToList();

      if (topTypes.Count > 0) {
        _summaryLabel.text = string.Join(" | ", topTypes);
        _summaryLabel.style.display = DisplayStyle.Flex;
      }
    }

    void StopScanUpdate() {
      EditorApplication.update -= OnScanUpdate;
      _scanEnumerator = null;
      _isScanning = false;
    }

    void CancelScan() {
      if (_isScanning) {
        StopScanUpdate();

        if (_scanner?.Progress is ScanProgress progress)
          progress.Cancel();
      }

      UpdateUI();
    }

    void RefreshAssetList() {
      var search = _searchField?.value?.ToLowerInvariant() ?? "";
      _filteredNodes = _scanner.Graph.GetNodesBySize(_scanner.Graph.NodeCount);

      if (!string.IsNullOrEmpty(search)) {
        _filteredNodes = _filteredNodes.FindAll(n =>
          n.Name.ToLowerInvariant().Contains(search) ||
          n.Path.ToLowerInvariant().Contains(search));
      }

      _assetList.itemsSource = _filteredNodes;
      _assetList.RefreshItems();
    }

    void UpdateUI() {
      _scanButton?.SetEnabled(!_isScanning);
      _cancelButton?.SetEnabled(_isScanning);
    }

    void ShowExportMenu() {
      var menu = new GenericMenu();
      menu.AddItem(new GUIContent("Markdown"), false, () => ExportReport(ReportFormat.Markdown));
      menu.AddItem(new GUIContent("Mermaid"), false, () => ExportReport(ReportFormat.Mermaid));
      menu.AddItem(new GUIContent("JSON"), false, () => ExportReport(ReportFormat.Json));
      menu.ShowAsContext();
    }

    void ExportReport(ReportFormat format) {
      var options = new ReportOptions { Format = format };
      var report = _exporter.Export(_scanner.Graph, options);

      var ext = format == ReportFormat.Json ? "json" : "md";
      var path = EditorUtility.SaveFilePanel("Export Report", "", $"asset-report.{ext}", ext);

      if (!string.IsNullOrEmpty(path)) {
        System.IO.File.WriteAllText(path, report);
        EditorUtility.RevealInFinder(path);
      }
    }

    void ExportHeavyHitters() {
      if (_scanner.Graph.NodeCount == 0) {
        EditorUtility.DisplayDialog("No Data", "Please scan the project first.", "OK");
        return;
      }

      var report = _exporter.ExportHeavyHitters(_scanner.Graph, 50);
      var path = EditorUtility.SaveFilePanel("Export Heavy Hitters", "", "heavy-hitters.md", "md");

      if (!string.IsNullOrEmpty(path)) {
        System.IO.File.WriteAllText(path, report);
        EditorUtility.RevealInFinder(path);
      }
    }
  }
}
