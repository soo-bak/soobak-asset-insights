using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Soobak.AssetInsights {
  public class FilterPanel : VisualElement {
    public event Action OnFilterChanged;

    // Debouncing for slider changes
    double _lastFilterChangeTime;
    bool _filterChangePending;
    const double FilterDebounceDelay = 0.15; // 150ms

    // Type filters
    readonly Dictionary<AssetType, Toggle> _typeToggles = new();

    // Size filters
    Slider _minSizeSlider;
    Slider _maxSizeSlider;
    Label _minSizeLabel;
    Label _maxSizeLabel;

    // Other filters
    Toggle _hasIssuesToggle;
    Toggle _unusedOnlyToggle;
    Toggle _circularOnlyToggle;

    // State
    public HashSet<AssetType> EnabledTypes { get; } = new();
    public long MinSize { get; private set; }
    public long MaxSize { get; private set; } = long.MaxValue;
    public bool HasIssuesOnly { get; private set; }
    public bool UnusedOnly { get; private set; }
    public bool CircularOnly { get; private set; }

    public FilterPanel() {
      BuildUI();
      ResetFilters();
    }

    void NotifyFilterChanged() {
      OnFilterChanged?.Invoke();
    }

    void NotifyFilterChangedDebounced() {
      _lastFilterChangeTime = EditorApplication.timeSinceStartup;

      // Only schedule one callback
      if (!_filterChangePending) {
        _filterChangePending = true;
        EditorApplication.delayCall += CheckAndFireFilterChange;
      }
    }

    void CheckAndFireFilterChange() {
      // Check if enough time has passed since last change
      double elapsed = EditorApplication.timeSinceStartup - _lastFilterChangeTime;
      if (elapsed >= FilterDebounceDelay) {
        // Enough time passed, fire the event
        _filterChangePending = false;
        OnFilterChanged?.Invoke();
      } else {
        // Not enough time, schedule another check (but limit to prevent infinite loops)
        EditorApplication.delayCall += CheckAndFireFilterChange;
      }
    }

    public void Cleanup() {
      _filterChangePending = false;
    }

    void BuildUI() {
      style.paddingTop = 8;
      style.paddingBottom = 8;
      style.paddingLeft = 8;
      style.paddingRight = 8;
      style.backgroundColor = new Color(0.18f, 0.18f, 0.18f);
      style.borderTopLeftRadius = 4;
      style.borderTopRightRadius = 4;
      style.borderBottomLeftRadius = 4;
      style.borderBottomRightRadius = 4;
      style.marginBottom = 8;

      // Header with reset button
      var headerRow = new VisualElement();
      headerRow.style.flexDirection = FlexDirection.Row;
      headerRow.style.marginBottom = 8;

      var header = new Label("Filters");
      header.style.unityFontStyleAndWeight = FontStyle.Bold;
      header.style.flexGrow = 1;
      headerRow.Add(header);

      var resetBtn = new Button(ResetFilters) { text = "Reset" };
      resetBtn.style.fontSize = 10;
      resetBtn.style.height = 18;
      headerRow.Add(resetBtn);

      Add(headerRow);

      // Type filters
      Add(CreateTypeFilters());

      // Size filters
      Add(CreateSizeFilters());

      // Special filters
      Add(CreateSpecialFilters());
    }

    VisualElement CreateTypeFilters() {
      var container = new VisualElement();
      container.style.marginBottom = 8;

      var label = new Label("Asset Types");
      label.style.fontSize = 11;
      label.style.marginBottom = 4;
      container.Add(label);

      var grid = new VisualElement();
      grid.style.flexDirection = FlexDirection.Row;
      grid.style.flexWrap = Wrap.Wrap;

      var types = new[] {
        AssetType.Texture, AssetType.Audio, AssetType.Model,
        AssetType.Material, AssetType.Prefab, AssetType.Scene,
        AssetType.Script, AssetType.Shader, AssetType.Animation,
        AssetType.ScriptableObject
      };

      foreach (var type in types) {
        var toggle = new Toggle(type.ToString());
        toggle.value = true;
        toggle.style.marginRight = 8;
        toggle.style.fontSize = 10;
        toggle.RegisterValueChangedCallback(e => {
          if (e.newValue)
            EnabledTypes.Add(type);
          else
            EnabledTypes.Remove(type);
          NotifyFilterChanged();
        });
        _typeToggles[type] = toggle;
        EnabledTypes.Add(type);
        grid.Add(toggle);
      }

      container.Add(grid);
      return container;
    }

    VisualElement CreateSizeFilters() {
      var container = new VisualElement();
      container.style.marginBottom = 8;

      var label = new Label("File Size");
      label.style.fontSize = 11;
      label.style.marginBottom = 4;
      container.Add(label);

      // Min size
      var minRow = new VisualElement();
      minRow.style.flexDirection = FlexDirection.Row;
      minRow.style.alignItems = Align.Center;
      minRow.style.marginBottom = 4;

      var minLabel = new Label("Min:");
      minLabel.style.width = 30;
      minLabel.style.fontSize = 10;
      minRow.Add(minLabel);

      _minSizeSlider = new Slider(0, 100);
      _minSizeSlider.style.flexGrow = 1;
      _minSizeSlider.value = 0;
      _minSizeSlider.RegisterValueChangedCallback(e => {
        MinSize = SliderToBytes(e.newValue);
        _minSizeLabel.text = FormatSize(MinSize);
        NotifyFilterChangedDebounced();
      });
      minRow.Add(_minSizeSlider);

      _minSizeLabel = new Label("0 B");
      _minSizeLabel.style.width = 60;
      _minSizeLabel.style.fontSize = 10;
      _minSizeLabel.style.unityTextAlign = TextAnchor.MiddleRight;
      minRow.Add(_minSizeLabel);

      container.Add(minRow);

      // Max size
      var maxRow = new VisualElement();
      maxRow.style.flexDirection = FlexDirection.Row;
      maxRow.style.alignItems = Align.Center;

      var maxLabel = new Label("Max:");
      maxLabel.style.width = 30;
      maxLabel.style.fontSize = 10;
      maxRow.Add(maxLabel);

      _maxSizeSlider = new Slider(0, 100);
      _maxSizeSlider.style.flexGrow = 1;
      _maxSizeSlider.value = 100;
      _maxSizeSlider.RegisterValueChangedCallback(e => {
        MaxSize = e.newValue >= 100 ? long.MaxValue : SliderToBytes(e.newValue);
        _maxSizeLabel.text = MaxSize == long.MaxValue ? "No limit" : FormatSize(MaxSize);
        NotifyFilterChangedDebounced();
      });
      maxRow.Add(_maxSizeSlider);

      _maxSizeLabel = new Label("No limit");
      _maxSizeLabel.style.width = 60;
      _maxSizeLabel.style.fontSize = 10;
      _maxSizeLabel.style.unityTextAlign = TextAnchor.MiddleRight;
      maxRow.Add(_maxSizeLabel);

      container.Add(maxRow);
      return container;
    }

    VisualElement CreateSpecialFilters() {
      var container = new VisualElement();

      var label = new Label("Special Filters");
      label.style.fontSize = 11;
      label.style.marginBottom = 4;
      container.Add(label);

      _hasIssuesToggle = new Toggle("Has optimization issues");
      _hasIssuesToggle.style.fontSize = 10;
      _hasIssuesToggle.RegisterValueChangedCallback(e => {
        HasIssuesOnly = e.newValue;
        NotifyFilterChanged();
      });
      container.Add(_hasIssuesToggle);

      _unusedOnlyToggle = new Toggle("Unused assets only");
      _unusedOnlyToggle.style.fontSize = 10;
      _unusedOnlyToggle.RegisterValueChangedCallback(e => {
        UnusedOnly = e.newValue;
        NotifyFilterChanged();
      });
      container.Add(_unusedOnlyToggle);

      _circularOnlyToggle = new Toggle("In circular dependency");
      _circularOnlyToggle.style.fontSize = 10;
      _circularOnlyToggle.RegisterValueChangedCallback(e => {
        CircularOnly = e.newValue;
        NotifyFilterChanged();
      });
      container.Add(_circularOnlyToggle);

      return container;
    }

    void ResetFilters() {
      EnabledTypes.Clear();
      foreach (var kvp in _typeToggles) {
        kvp.Value.SetValueWithoutNotify(true);
        EnabledTypes.Add(kvp.Key);
      }

      _minSizeSlider.SetValueWithoutNotify(0);
      _maxSizeSlider.SetValueWithoutNotify(100);
      _minSizeLabel.text = "0 B";
      _maxSizeLabel.text = "No limit";
      MinSize = 0;
      MaxSize = long.MaxValue;

      _hasIssuesToggle.SetValueWithoutNotify(false);
      _unusedOnlyToggle.SetValueWithoutNotify(false);
      _circularOnlyToggle.SetValueWithoutNotify(false);
      HasIssuesOnly = false;
      UnusedOnly = false;
      CircularOnly = false;

      NotifyFilterChanged();
    }

    // Convert slider value (0-100) to bytes (logarithmic scale)
    long SliderToBytes(float value) {
      if (value <= 0) return 0;
      if (value >= 100) return long.MaxValue;

      // Logarithmic scale: 0=0, 25=1KB, 50=1MB, 75=100MB, 100=10GB
      var exponent = value / 10f; // 0-10
      return (long)Math.Pow(10, exponent);
    }

    string FormatSize(long bytes) {
      return AssetNodeModel.FormatBytes(bytes);
    }

    public bool PassesFilter(AssetNodeModel node, DependencyGraph graph,
      HashSet<string> unusedAssets = null, HashSet<string> circularAssets = null,
      OptimizationEngine engine = null) {

      // Type filter
      if (!EnabledTypes.Contains(node.Type))
        return false;

      // Size filter
      if (node.SizeBytes < MinSize)
        return false;
      if (MaxSize != long.MaxValue && node.SizeBytes > MaxSize)
        return false;

      // Unused filter
      if (UnusedOnly && unusedAssets != null && !unusedAssets.Contains(node.Path))
        return false;

      // Circular filter
      if (CircularOnly && circularAssets != null && !circularAssets.Contains(node.Path))
        return false;

      // Issues filter
      if (HasIssuesOnly && engine != null) {
        var issues = engine.AnalyzeAsset(node.Path);
        if (!issues.Any())
          return false;
      }

      return true;
    }
  }
}
