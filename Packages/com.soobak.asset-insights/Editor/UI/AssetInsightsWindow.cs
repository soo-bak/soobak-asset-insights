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
    HealthScoreResult _cachedHealthResult;

    // Sorting
    enum SortColumn { Name, Refs, Deps, Size }
    SortColumn _sortColumn = SortColumn.Size;
    bool _sortAscending = false;
    Label _nameHeader;
    Label _refsHeader;
    Label _depsHeader;
    Label _sizeHeader;

    VisualElement _root;
    ProgressBar _progressBar;
    Label _statusLabel;
    Label _summaryLabel;
    ListView _assetList;
    TextField _searchField;
    Button _scanButton;
    Button _cancelButton;
    Button _exportButton;
    AssetPreviewPanel _previewPanel;
    FilterPanel _filterPanel;
    Button _filterToggleButton;
    bool _filterVisible;

    // Cached analysis data for filtering
    HashSet<string> _unusedAssets;
    HashSet<string> _circularAssets;
    BuildInclusionAnalyzer _buildAnalyzer;
    OptimizationEngine _cachedOptimizationEngine;

    // Search debouncing
    double _lastSearchTime;
    string _pendingSearch;
    const double SearchDebounceDelay = 0.15; // 150ms

    // View switching
    VisualElement _listContainer;
    VisualElement _dashboardContainer;
    VisualElement _settingsContainer;
    DashboardPanel _dashboardPanel;
    SettingsPanel _settingsPanel;
    Button _listTabButton;
    Button _dashboardTabButton;
    Button _settingsTabButton;
    Button _showGraphButton;
    int _currentView; // 0=list, 1=dashboard, 2=settings

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
      CreateDashboardView();
      CreateSettingsView();
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

      _dashboardTabButton = new Button(() => SwitchToView(1)) { text = "Dashboard" };
      _dashboardTabButton.style.flexGrow = 1;
      tabContainer.Add(_dashboardTabButton);

      _settingsTabButton = new Button(() => SwitchToView(2)) { text = "Settings" };
      _settingsTabButton.style.flexGrow = 1;
      tabContainer.Add(_settingsTabButton);

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

    void CreateSettingsView() {
      _settingsContainer = new VisualElement();
      _settingsContainer.style.flexGrow = 1;
      _settingsContainer.style.flexShrink = 1;
      _settingsContainer.style.display = DisplayStyle.None;

      _settingsPanel = new SettingsPanel();
      _settingsContainer.Add(_settingsPanel);

      _root.Add(_settingsContainer);
    }

    void CreateListView() {
      _listContainer = new VisualElement();
      _listContainer.style.flexGrow = 1;
      _listContainer.style.flexShrink = 1;
      _listContainer.style.flexDirection = FlexDirection.Row;

      // Left side - list
      var listSection = new VisualElement();
      listSection.style.flexGrow = 1;
      listSection.style.flexShrink = 1;

      CreateSearchSection(listSection);
      CreateListHeader(listSection);
      CreateAssetList(listSection);

      _listContainer.Add(listSection);

      // Right side - preview panel
      _previewPanel = new AssetPreviewPanel(_scanner.Graph);
      _listContainer.Add(_previewPanel);

      _root.Add(_listContainer);
    }

    void SwitchToView(int view) {
      _currentView = view;

      _listContainer.style.display = view == 0 ? DisplayStyle.Flex : DisplayStyle.None;
      _dashboardContainer.style.display = view == 1 ? DisplayStyle.Flex : DisplayStyle.None;
      _settingsContainer.style.display = view == 2 ? DisplayStyle.Flex : DisplayStyle.None;

      var activeColor = new Color(0.3f, 0.5f, 0.7f);
      _listTabButton.style.backgroundColor = view == 0 ? activeColor : StyleKeyword.Null;
      _dashboardTabButton.style.backgroundColor = view == 1 ? activeColor : StyleKeyword.Null;
      _settingsTabButton.style.backgroundColor = view == 2 ? activeColor : StyleKeyword.Null;

      if (view == 1 && _cachedHealthResult != null)
        _dashboardPanel?.SetResult(_cachedHealthResult);
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

      _filterToggleButton = new Button(ToggleFilter) { text = "Filter" };
      _filterToggleButton.style.width = 60;
      container.Add(_filterToggleButton);

      _showGraphButton = new Button(OnShowGraphClicked) { text = "Show Graph" };
      _showGraphButton.style.width = 90;
      _showGraphButton.SetEnabled(false);
      container.Add(_showGraphButton);

      parent.Add(container);

      // Filter panel (initially hidden)
      _filterPanel = new FilterPanel();
      _filterPanel.style.display = DisplayStyle.None;
      _filterPanel.OnFilterChanged += RefreshAssetList;
      parent.Add(_filterPanel);
    }

    void ToggleFilter() {
      _filterVisible = !_filterVisible;
      _filterPanel.style.display = _filterVisible ? DisplayStyle.Flex : DisplayStyle.None;
      _filterToggleButton.style.backgroundColor = _filterVisible
        ? new Color(0.3f, 0.5f, 0.7f)
        : StyleKeyword.Null;
    }

    void OnShowGraphClicked() {
      if (!string.IsNullOrEmpty(_selectedAssetPath) && _scanner.Graph.NodeCount > 0) {
        DependencyGraphWindow.Show(_scanner.Graph, _selectedAssetPath);
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

      _nameHeader = CreateSortableHeader("Asset", SortColumn.Name);
      _nameHeader.style.flexGrow = 1;
      header.Add(_nameHeader);

      _refsHeader = CreateSortableHeader("Refs", SortColumn.Refs);
      _refsHeader.style.width = 50;
      _refsHeader.style.unityTextAlign = TextAnchor.MiddleRight;
      _refsHeader.tooltip = "Dependents (assets that reference this) - Click to sort";
      header.Add(_refsHeader);

      _depsHeader = CreateSortableHeader("Deps", SortColumn.Deps);
      _depsHeader.style.width = 50;
      _depsHeader.style.unityTextAlign = TextAnchor.MiddleRight;
      _depsHeader.tooltip = "Dependencies (assets this references) - Click to sort";
      header.Add(_depsHeader);

      _sizeHeader = CreateSortableHeader("Size", SortColumn.Size);
      _sizeHeader.style.width = 70;
      _sizeHeader.style.unityTextAlign = TextAnchor.MiddleRight;
      _sizeHeader.tooltip = "File size - Click to sort";
      header.Add(_sizeHeader);

      parent.Add(header);
      UpdateSortHeaders();
    }

    Label CreateSortableHeader(string text, SortColumn column) {
      var label = new Label(text);
      label.style.unityFontStyleAndWeight = FontStyle.Bold;
      label.style.paddingLeft = 2;
      label.style.paddingRight = 2;

      label.RegisterCallback<MouseEnterEvent>(e => {
        label.style.backgroundColor = new Color(0.3f, 0.3f, 0.3f);
      });
      label.RegisterCallback<MouseLeaveEvent>(e => {
        label.style.backgroundColor = StyleKeyword.Null;
      });
      label.RegisterCallback<ClickEvent>(e => {
        if (_sortColumn == column) {
          _sortAscending = !_sortAscending;
        } else {
          _sortColumn = column;
          _sortAscending = column == SortColumn.Name; // Name defaults ascending, others descending
        }
        UpdateSortHeaders();
        RefreshAssetList();
      });

      return label;
    }

    void UpdateSortHeaders() {
      string arrow = _sortAscending ? " ▲" : " ▼";

      _nameHeader.text = "Asset" + (_sortColumn == SortColumn.Name ? arrow : "");
      _refsHeader.text = "Refs" + (_sortColumn == SortColumn.Refs ? arrow : "");
      _depsHeader.text = "Deps" + (_sortColumn == SortColumn.Deps ? arrow : "");
      _sizeHeader.text = "Size" + (_sortColumn == SortColumn.Size ? arrow : "");
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

      // Build status indicator
      var buildDot = new VisualElement { name = "buildDot" };
      buildDot.style.width = 6;
      buildDot.style.height = 6;
      buildDot.style.borderTopLeftRadius = 3;
      buildDot.style.borderTopRightRadius = 3;
      buildDot.style.borderBottomLeftRadius = 3;
      buildDot.style.borderBottomRightRadius = 3;
      buildDot.style.marginRight = 4;
      buildDot.style.flexShrink = 0;
      item.Add(buildDot);

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

      // Build status indicator
      var buildDot = element.Q<VisualElement>("buildDot");
      if (_buildAnalyzer != null) {
        var status = _buildAnalyzer.GetStatus(node.Path);
        var (color, tooltip) = status switch {
          BuildInclusionStatus.IncludedInBuild => (new Color(0.4f, 0.8f, 0.4f), "Included in build"),
          BuildInclusionStatus.Resources => (new Color(0.4f, 0.7f, 1f), "In Resources folder"),
          BuildInclusionStatus.StreamingAssets => (new Color(0.8f, 0.6f, 1f), "In StreamingAssets"),
          BuildInclusionStatus.EditorOnly => (new Color(0.6f, 0.6f, 0.6f), "Editor only"),
          _ => (new Color(1f, 0.5f, 0.3f), "Not included in build")
        };
        buildDot.style.backgroundColor = color;
        buildDot.tooltip = tooltip;
      }

      var icon = element.Q<Image>("icon");
      var assetIcon = AssetDatabase.GetCachedIcon(node.Path);
      icon.image = assetIcon;

      var nameLabel = element.Q<Label>("name");
      nameLabel.text = node.Name;

      var pathLabel = element.Q<Label>("path");
      var folderPath = System.IO.Path.GetDirectoryName(node.Path);
      pathLabel.text = folderPath ?? "";

      var refsLabel = element.Q<Label>("refs");
      refsLabel.text = node.DependentCount > 0 ? node.DependentCount.ToString() : "-";

      var depsLabel = element.Q<Label>("deps");
      depsLabel.text = node.DependencyCount > 0 ? node.DependencyCount.ToString() : "-";

      var sizeLabel = element.Q<Label>("size");
      sizeLabel.text = node.FormattedSize;
    }

    void OnSearchChanged(ChangeEvent<string> evt) {
      // Debounce search to avoid excessive refreshes during typing
      _pendingSearch = evt.newValue;
      _lastSearchTime = EditorApplication.timeSinceStartup;
      EditorApplication.delayCall += ProcessDebouncedSearch;
    }

    void ProcessDebouncedSearch() {
      if (EditorApplication.timeSinceStartup - _lastSearchTime >= SearchDebounceDelay) {
        if (_searchField?.value == _pendingSearch) {
          RefreshAssetList();
        }
      }
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
      _previewPanel?.ShowAsset(_selectedAssetPath);
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
      _progressBar.title = $"Analyzing...";
      _statusLabel.text = "Calculating health score...";

      // Run analysis after scan (delayed to allow UI update)
      EditorApplication.delayCall += () => {
        var calculator = new HealthScoreCalculator(graph);
        _cachedHealthResult = calculator.Calculate();

        // Cache analysis data for filtering
        var unusedAnalyzer = new UnusedAssetAnalyzer(graph);
        var unusedResult = unusedAnalyzer.Analyze();
        _unusedAssets = unusedResult.UnusedAssets.Select(a => a.Path).ToHashSet();

        var circularDetector = new CircularDependencyDetector(graph);
        var circularResult = circularDetector.Detect();
        _circularAssets = circularResult.AssetsInCycles;

        // Build inclusion analysis
        _buildAnalyzer = new BuildInclusionAnalyzer(graph);
        _buildAnalyzer.Analyze();

        // Cache optimization engine for filtering
        _cachedOptimizationEngine = new OptimizationEngine(graph);
        _cachedOptimizationEngine.Analyze();

        _progressBar.title = $"Complete - {graph.NodeCount} assets ({AssetNodeModel.FormatBytes(graph.GetTotalSize())})";
        _statusLabel.text = $"Health: {_cachedHealthResult.Grade} ({_cachedHealthResult.Score}/100)";

        UpdateSummary();

        _scanButton.SetEnabled(true);
        _cancelButton.SetEnabled(false);
        _exportButton.SetEnabled(graph.NodeCount > 0);

        // Update dashboard with cached result
        _dashboardPanel?.SetResult(_cachedHealthResult);

        RefreshAssetList();
      };
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

      // Use single allocation for filtered list
      var nodes = _scanner.Graph.Nodes.Values;

      // Apply text search using cached lowercase
      if (!string.IsNullOrEmpty(search)) {
        _filteredNodes = new List<AssetNodeModel>(nodes.Count);
        foreach (var n in nodes) {
          if (n.NameLower.Contains(search) || n.PathLower.Contains(search))
            _filteredNodes.Add(n);
        }
      } else {
        _filteredNodes = new List<AssetNodeModel>(nodes);
      }

      // Apply filter panel filters
      if (_filterPanel != null && _filterVisible) {
        var engine = _filterPanel.HasIssuesOnly ? _cachedOptimizationEngine : null;
        var filtered = new List<AssetNodeModel>(_filteredNodes.Count);
        foreach (var n in _filteredNodes) {
          if (_filterPanel.PassesFilter(n, _scanner.Graph, _unusedAssets, _circularAssets, engine))
            filtered.Add(n);
        }
        _filteredNodes = filtered;
      }

      // Apply sorting using cached counts (avoid LINQ allocations)
      SortFilteredNodes();

      _assetList.itemsSource = _filteredNodes;
      _assetList.RefreshItems();
    }

    void SortFilteredNodes() {
      switch (_sortColumn) {
        case SortColumn.Name:
          _filteredNodes.Sort((a, b) => _sortAscending
            ? string.Compare(a.Name, b.Name, System.StringComparison.Ordinal)
            : string.Compare(b.Name, a.Name, System.StringComparison.Ordinal));
          break;
        case SortColumn.Refs:
          _filteredNodes.Sort((a, b) => _sortAscending
            ? a.DependentCount.CompareTo(b.DependentCount)
            : b.DependentCount.CompareTo(a.DependentCount));
          break;
        case SortColumn.Deps:
          _filteredNodes.Sort((a, b) => _sortAscending
            ? a.DependencyCount.CompareTo(b.DependencyCount)
            : b.DependencyCount.CompareTo(a.DependencyCount));
          break;
        case SortColumn.Size:
          _filteredNodes.Sort((a, b) => _sortAscending
            ? a.SizeBytes.CompareTo(b.SizeBytes)
            : b.SizeBytes.CompareTo(a.SizeBytes));
          break;
      }
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
