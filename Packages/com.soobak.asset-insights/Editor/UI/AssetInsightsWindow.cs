using System.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;


namespace Soobak.AssetInsights {
  public class AssetInsightsWindow : EditorWindow {
    IDependencyScanner _scanner;
    IReportExporter _exporter;
    IEnumerator _scanEnumerator;
    bool _isScanning;

    VisualElement _root;
    ProgressBar _progressBar;
    Label _statusLabel;
    ListView _assetList;
    TextField _searchField;
    Button _scanButton;
    Button _cancelButton;
    Button _exportButton;

    [MenuItem("Window/Asset Insights")]
    public static void ShowWindow() {
      var window = GetWindow<AssetInsightsWindow>();
      window.titleContent = new GUIContent("Asset Insights");
      window.minSize = new Vector2(400, 300);
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
      CreateSearchSection();
      CreateAssetList();
      CreateExportSection();

      UpdateUI();
    }

    void CreateToolbar() {
      var toolbar = new VisualElement();
      toolbar.style.flexDirection = FlexDirection.Row;
      toolbar.style.marginBottom = 8;

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

      _progressBar = new ProgressBar();
      _progressBar.title = "Ready";
      _progressBar.style.height = 20;
      container.Add(_progressBar);

      _statusLabel = new Label();
      _statusLabel.style.fontSize = 10;
      _statusLabel.style.color = new Color(0.6f, 0.6f, 0.6f);
      container.Add(_statusLabel);

      _root.Add(container);
    }

    void CreateSearchSection() {
      var container = new VisualElement();
      container.style.flexDirection = FlexDirection.Row;
      container.style.marginBottom = 8;

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

      _root.Add(container);
    }

    void CreateAssetList() {
      _assetList = new ListView();
      _assetList.style.flexGrow = 1;
      _assetList.style.minHeight = 200;
      _assetList.selectionType = SelectionType.Single;
      _assetList.makeItem = MakeAssetItem;
      _assetList.bindItem = BindAssetItem;
      _assetList.fixedItemHeight = 24;
      _assetList.selectionChanged += OnAssetSelected;

      _root.Add(_assetList);
    }

    void CreateExportSection() {
      var container = new VisualElement();
      container.style.flexDirection = FlexDirection.Row;
      container.style.marginTop = 8;

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

      var icon = new VisualElement { name = "icon" };
      icon.style.width = 16;
      icon.style.height = 16;
      icon.style.marginRight = 4;
      item.Add(icon);

      var nameLabel = new Label { name = "name" };
      nameLabel.style.flexGrow = 1;
      nameLabel.style.overflow = Overflow.Hidden;
      nameLabel.style.textOverflow = TextOverflow.Ellipsis;
      item.Add(nameLabel);

      var sizeLabel = new Label { name = "size" };
      sizeLabel.style.width = 60;
      sizeLabel.style.unityTextAlign = TextAnchor.MiddleRight;
      item.Add(sizeLabel);

      var typeLabel = new Label { name = "type" };
      typeLabel.style.width = 80;
      typeLabel.style.unityTextAlign = TextAnchor.MiddleRight;
      typeLabel.style.color = new Color(0.6f, 0.6f, 0.6f);
      item.Add(typeLabel);

      return item;
    }

    void BindAssetItem(VisualElement element, int index) {
      var nodes = _scanner.Graph.GetNodesBySize(_scanner.Graph.NodeCount);
      if (index >= nodes.Count)
        return;

      var node = nodes[index];

      var nameLabel = element.Q<Label>("name");
      nameLabel.text = node.Name;

      var sizeLabel = element.Q<Label>("size");
      sizeLabel.text = node.FormattedSize;

      var typeLabel = element.Q<Label>("type");
      typeLabel.text = node.Type.ToString();
    }

    void OnSearchChanged(ChangeEvent<string> evt) {
      RefreshAssetList();
    }

    void OnAssetSelected(System.Collections.Generic.IEnumerable<object> selection) {
      foreach (var item in selection) {
        if (item is AssetNodeModel node)
          EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<Object>(node.Path));
      }
    }

    void StartScan() {
      CancelScan();
      _scanner.Clear();
      _isScanning = true;

      var options = new ScanOptions();
      _scanEnumerator = _scanner.ScanAsync(options);

      _scanButton.SetEnabled(false);
      _cancelButton.SetEnabled(true);

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

      _progressBar.value = 100f;
      _progressBar.title = $"Complete - {_scanner.Graph.NodeCount} assets";
      _statusLabel.text = "";

      _scanButton.SetEnabled(true);
      _cancelButton.SetEnabled(false);
      _exportButton.SetEnabled(_scanner.Graph.NodeCount > 0);

      RefreshAssetList();
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
      var nodes = _scanner.Graph.GetNodesBySize(_scanner.Graph.NodeCount);

      if (!string.IsNullOrEmpty(search)) {
        nodes = nodes.FindAll(n =>
          n.Name.ToLowerInvariant().Contains(search) ||
          n.Path.ToLowerInvariant().Contains(search));
      }

      _assetList.itemsSource = nodes;
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
