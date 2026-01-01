using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Soobak.AssetInsights {
  public class SettingsPanel : VisualElement {
    TextField _newPathField;
    TextField _newPatternField;
    TextField _newWhitelistField;
    ListView _ignoredPathsList;
    ListView _ignoredPatternsList;
    ListView _whitelistedPathsList;

    public SettingsPanel() {
      BuildUI();
    }

    void BuildUI() {
      style.flexGrow = 1;
      style.paddingTop = 8;
      style.paddingBottom = 8;
      style.paddingLeft = 12;
      style.paddingRight = 12;

      var scrollView = new ScrollView(ScrollViewMode.Vertical);
      scrollView.style.flexGrow = 1;
      Add(scrollView);

      // Toggle options
      scrollView.Add(CreateSection("General Options", CreateToggleOptions()));

      // Ignored Paths
      scrollView.Add(CreateSection("Ignored Paths", CreateIgnoredPathsSection()));

      // Ignored Patterns
      scrollView.Add(CreateSection("Ignored Patterns", CreateIgnoredPatternsSection()));

      // Whitelisted Paths
      scrollView.Add(CreateSection("Whitelisted Paths (Priority)", CreateWhitelistedPathsSection()));

      // Reset button
      var resetBtn = new Button(OnResetClicked) { text = "Reset to Defaults" };
      resetBtn.style.marginTop = 16;
      resetBtn.style.alignSelf = Align.FlexEnd;
      scrollView.Add(resetBtn);
    }

    VisualElement CreateSection(string title, VisualElement content) {
      var section = new VisualElement();
      section.style.marginBottom = 16;
      section.style.paddingTop = 8;
      section.style.paddingBottom = 8;
      section.style.paddingLeft = 10;
      section.style.paddingRight = 10;
      section.style.backgroundColor = new Color(0.18f, 0.18f, 0.18f);
      section.style.borderTopLeftRadius = 4;
      section.style.borderTopRightRadius = 4;
      section.style.borderBottomLeftRadius = 4;
      section.style.borderBottomRightRadius = 4;

      var header = new Label(title);
      header.style.fontSize = 13;
      header.style.unityFontStyleAndWeight = FontStyle.Bold;
      header.style.marginBottom = 8;
      section.Add(header);

      section.Add(content);
      return section;
    }

    VisualElement CreateToggleOptions() {
      var container = new VisualElement();
      var settings = AssetInsightsSettings.instance;

      var editorToggle = new Toggle("Ignore Editor folders") { value = settings.IgnoreEditorAssets };
      editorToggle.RegisterValueChangedCallback(e => settings.SetIgnoreEditorAssets(e.newValue));
      container.Add(editorToggle);

      var testToggle = new Toggle("Ignore Test folders") { value = settings.IgnoreTestAssets };
      testToggle.RegisterValueChangedCallback(e => settings.SetIgnoreTestAssets(e.newValue));
      container.Add(testToggle);

      var streamingToggle = new Toggle("Ignore StreamingAssets") { value = settings.IgnoreStreamingAssets };
      streamingToggle.RegisterValueChangedCallback(e => settings.SetIgnoreStreamingAssets(e.newValue));
      container.Add(streamingToggle);

      return container;
    }

    VisualElement CreateIgnoredPathsSection() {
      var container = new VisualElement();
      var settings = AssetInsightsSettings.instance;

      var desc = new Label("Assets in these folders will be excluded from analysis");
      desc.style.fontSize = 10;
      desc.style.color = new Color(0.6f, 0.6f, 0.6f);
      desc.style.marginBottom = 6;
      container.Add(desc);

      // List
      var listContainer = new VisualElement();
      listContainer.style.maxHeight = 120;
      listContainer.style.marginBottom = 6;

      foreach (var path in settings.IgnoredPaths) {
        listContainer.Add(CreateRemovableItem(path, () => {
          settings.RemoveIgnoredPath(path);
          Refresh();
        }));
      }
      container.Add(listContainer);

      // Add new
      var addRow = new VisualElement();
      addRow.style.flexDirection = FlexDirection.Row;

      _newPathField = new TextField();
      _newPathField.style.flexGrow = 1;
      _newPathField.value = "Assets/";
      addRow.Add(_newPathField);

      var addBtn = new Button(() => {
        if (!string.IsNullOrWhiteSpace(_newPathField.value)) {
          settings.AddIgnoredPath(_newPathField.value);
          _newPathField.value = "Assets/";
          Refresh();
        }
      }) { text = "Add" };
      addBtn.style.width = 50;
      addRow.Add(addBtn);

      container.Add(addRow);
      return container;
    }

    VisualElement CreateIgnoredPatternsSection() {
      var container = new VisualElement();
      var settings = AssetInsightsSettings.instance;

      var desc = new Label("Files matching these patterns will be excluded (* = any, ? = single char)");
      desc.style.fontSize = 10;
      desc.style.color = new Color(0.6f, 0.6f, 0.6f);
      desc.style.marginBottom = 6;
      container.Add(desc);

      // List
      var listContainer = new VisualElement();
      listContainer.style.maxHeight = 100;
      listContainer.style.marginBottom = 6;

      foreach (var pattern in settings.IgnoredPatterns) {
        listContainer.Add(CreateRemovableItem(pattern, () => {
          settings.RemoveIgnoredPattern(pattern);
          Refresh();
        }));
      }
      container.Add(listContainer);

      // Add new
      var addRow = new VisualElement();
      addRow.style.flexDirection = FlexDirection.Row;

      _newPatternField = new TextField();
      _newPatternField.style.flexGrow = 1;
      _newPatternField.value = "*.tmp";
      addRow.Add(_newPatternField);

      var addBtn = new Button(() => {
        if (!string.IsNullOrWhiteSpace(_newPatternField.value)) {
          settings.AddIgnoredPattern(_newPatternField.value);
          _newPatternField.value = "*.tmp";
          Refresh();
        }
      }) { text = "Add" };
      addBtn.style.width = 50;
      addRow.Add(addBtn);

      container.Add(addRow);
      return container;
    }

    VisualElement CreateWhitelistedPathsSection() {
      var container = new VisualElement();
      var settings = AssetInsightsSettings.instance;

      var desc = new Label("Assets in these folders will ALWAYS be included (overrides ignore rules)");
      desc.style.fontSize = 10;
      desc.style.color = new Color(0.6f, 0.6f, 0.6f);
      desc.style.marginBottom = 6;
      container.Add(desc);

      // List
      var listContainer = new VisualElement();
      listContainer.style.maxHeight = 100;
      listContainer.style.marginBottom = 6;

      foreach (var path in settings.WhitelistedPaths) {
        listContainer.Add(CreateRemovableItem(path, () => {
          settings.RemoveWhitelistedPath(path);
          Refresh();
        }));
      }

      if (settings.WhitelistedPaths.Count == 0) {
        var empty = new Label("(none)");
        empty.style.color = new Color(0.5f, 0.5f, 0.5f);
        empty.style.fontStyleAndWeight = FontStyle.Italic;
        listContainer.Add(empty);
      }

      container.Add(listContainer);

      // Add new
      var addRow = new VisualElement();
      addRow.style.flexDirection = FlexDirection.Row;

      _newWhitelistField = new TextField();
      _newWhitelistField.style.flexGrow = 1;
      _newWhitelistField.value = "Assets/";
      addRow.Add(_newWhitelistField);

      var addBtn = new Button(() => {
        if (!string.IsNullOrWhiteSpace(_newWhitelistField.value)) {
          settings.AddWhitelistedPath(_newWhitelistField.value);
          _newWhitelistField.value = "Assets/";
          Refresh();
        }
      }) { text = "Add" };
      addBtn.style.width = 50;
      addRow.Add(addBtn);

      container.Add(addRow);
      return container;
    }

    VisualElement CreateRemovableItem(string text, System.Action onRemove) {
      var row = new VisualElement();
      row.style.flexDirection = FlexDirection.Row;
      row.style.alignItems = Align.Center;
      row.style.paddingTop = 2;
      row.style.paddingBottom = 2;

      var label = new Label(text);
      label.style.flexGrow = 1;
      label.style.overflow = Overflow.Hidden;
      label.style.textOverflow = TextOverflow.Ellipsis;
      row.Add(label);

      var removeBtn = new Button(onRemove) { text = "X" };
      removeBtn.style.width = 20;
      removeBtn.style.height = 18;
      removeBtn.style.fontSize = 10;
      removeBtn.style.paddingTop = 0;
      removeBtn.style.paddingBottom = 0;
      removeBtn.style.paddingLeft = 0;
      removeBtn.style.paddingRight = 0;
      row.Add(removeBtn);

      return row;
    }

    void OnResetClicked() {
      if (EditorUtility.DisplayDialog("Reset Settings",
        "Are you sure you want to reset all settings to defaults?", "Reset", "Cancel")) {
        AssetInsightsSettings.instance.ResetToDefaults();
        Refresh();
      }
    }

    void Refresh() {
      Clear();
      BuildUI();
    }
  }
}
