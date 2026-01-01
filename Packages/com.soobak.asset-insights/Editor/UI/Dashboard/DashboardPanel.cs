using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Soobak.AssetInsights {
  public class DashboardPanel : VisualElement {
    readonly DependencyGraph _graph;
    ScrollView _scrollView;
    HealthScoreResult _cachedResult;

    public DashboardPanel(DependencyGraph graph) {
      _graph = graph;
      BuildUI();
    }

    void BuildUI() {
      style.flexGrow = 1;

      _scrollView = new ScrollView(ScrollViewMode.Vertical);
      _scrollView.style.flexGrow = 1;
      Add(_scrollView);
    }

    public void SetResult(HealthScoreResult result) {
      _cachedResult = result;
      Refresh();
    }

    public void Refresh() {
      _scrollView.Clear();

      if (_graph.NodeCount == 0) {
        var placeholder = new Label("Scan project first to see insights");
        placeholder.style.unityTextAlign = TextAnchor.MiddleCenter;
        placeholder.style.color = new Color(0.5f, 0.5f, 0.5f);
        placeholder.style.marginTop = 50;
        _scrollView.Add(placeholder);
        return;
      }

      // Type Breakdown
      AddTypeBreakdownSection();

      // Largest Assets
      AddLargestAssetsSection();

      // Unused Assets
      AddUnusedAssetsSection();

      // Optimization Issues
      AddOptimizationIssuesSection();

      // Circular Dependencies
      AddCircularDependenciesSection();

      // Duplicate Assets
      AddDuplicateAssetsSection();
    }

    void AddTypeBreakdownSection() {
      var section = CreateSection("Type Breakdown");

      var byType = _graph.GetSizeByType()
        .OrderByDescending(kv => kv.Value.totalSize)
        .ToList();

      var totalSize = _graph.GetTotalSize();

      foreach (var kv in byType) {
        var row = CreateRow();

        var typeLabel = new Label(kv.Key.ToString());
        typeLabel.style.width = 120;
        typeLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        row.Add(typeLabel);

        var countLabel = new Label($"{kv.Value.count} files");
        countLabel.style.width = 80;
        countLabel.style.color = new Color(0.6f, 0.6f, 0.6f);
        row.Add(countLabel);

        var sizeLabel = new Label(AssetNodeModel.FormatBytes(kv.Value.totalSize));
        sizeLabel.style.width = 80;
        row.Add(sizeLabel);

        // Progress bar for percentage
        var percent = totalSize > 0 ? (float)kv.Value.totalSize / totalSize : 0;
        var bar = new VisualElement();
        bar.style.flexGrow = 1;
        bar.style.height = 8;
        bar.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f);
        bar.style.borderTopLeftRadius = 4;
        bar.style.borderTopRightRadius = 4;
        bar.style.borderBottomLeftRadius = 4;
        bar.style.borderBottomRightRadius = 4;

        var fill = new VisualElement();
        fill.style.width = Length.Percent(percent * 100);
        fill.style.height = Length.Percent(100);
        fill.style.backgroundColor = GetTypeColor(kv.Key);
        fill.style.borderTopLeftRadius = 4;
        fill.style.borderBottomLeftRadius = 4;
        bar.Add(fill);

        row.Add(bar);

        var percentLabel = new Label($"{percent:P0}");
        percentLabel.style.width = 50;
        percentLabel.style.unityTextAlign = TextAnchor.MiddleRight;
        row.Add(percentLabel);

        section.Add(row);
      }

      _scrollView.Add(section);
    }

    void AddLargestAssetsSection() {
      var section = CreateSection("Largest Assets (Top 10)");

      var largest = _graph.GetNodesBySize(10);

      foreach (var node in largest) {
        var row = CreateClickableRow(node.Path);

        var icon = new Image();
        icon.image = AssetDatabase.GetCachedIcon(node.Path);
        icon.style.width = 16;
        icon.style.height = 16;
        icon.style.marginRight = 6;
        row.Add(icon);

        var nameLabel = new Label(node.Name);
        nameLabel.style.flexGrow = 1;
        nameLabel.style.overflow = Overflow.Hidden;
        nameLabel.style.textOverflow = TextOverflow.Ellipsis;
        row.Add(nameLabel);

        var sizeLabel = new Label(node.FormattedSize);
        sizeLabel.style.width = 80;
        sizeLabel.style.unityTextAlign = TextAnchor.MiddleRight;
        sizeLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        row.Add(sizeLabel);

        section.Add(row);
      }

      _scrollView.Add(section);
    }

    void AddUnusedAssetsSection() {
      var section = CreateSection("Unused Assets");

      var analyzer = new UnusedAssetAnalyzer(_graph);
      var result = analyzer.Analyze();

      if (result.TotalUnusedCount == 0) {
        section.Add(CreateEmptyMessage("No unused assets found"));
      } else {
        var summary = new Label($"{result.TotalUnusedCount} unused assets ({AssetNodeModel.FormatBytes(result.TotalUnusedSize)})");
        summary.style.marginBottom = 8;
        summary.style.color = new Color(1f, 0.6f, 0.4f);
        section.Add(summary);

        var shown = 0;
        foreach (var info in result.UnusedAssets.Take(20)) {
          var row = CreateClickableRow(info.Path);

          var icon = new Image();
          icon.image = AssetDatabase.GetCachedIcon(info.Path);
          icon.style.width = 16;
          icon.style.height = 16;
          icon.style.marginRight = 6;
          row.Add(icon);

          var nameLabel = new Label(info.Name);
          nameLabel.style.flexGrow = 1;
          nameLabel.style.overflow = Overflow.Hidden;
          nameLabel.style.textOverflow = TextOverflow.Ellipsis;
          row.Add(nameLabel);

          var sizeLabel = new Label(info.FormattedSize);
          sizeLabel.style.width = 60;
          sizeLabel.style.unityTextAlign = TextAnchor.MiddleRight;
          row.Add(sizeLabel);

          section.Add(row);
          shown++;
        }

        if (result.TotalUnusedCount > shown) {
          var more = new Label($"... and {result.TotalUnusedCount - shown} more");
          more.style.color = new Color(0.5f, 0.5f, 0.5f);
          more.style.marginTop = 4;
          section.Add(more);
        }
      }

      _scrollView.Add(section);
    }

    void AddOptimizationIssuesSection() {
      var section = CreateSection("Optimization Issues");

      var engine = new OptimizationEngine(_graph);
      var report = engine.Analyze();

      if (report.TotalIssues == 0) {
        section.Add(CreateEmptyMessage("No optimization issues found"));
      } else {
        foreach (var issue in report.Issues.Take(20)) {
          var row = CreateClickableRow(issue.AssetPath);
          row.style.flexDirection = FlexDirection.Column;
          row.style.alignItems = Align.Stretch;

          var headerRow = new VisualElement();
          headerRow.style.flexDirection = FlexDirection.Row;
          headerRow.style.alignItems = Align.Center;

          var severityDot = new VisualElement();
          severityDot.style.width = 8;
          severityDot.style.height = 8;
          severityDot.style.borderTopLeftRadius = 4;
          severityDot.style.borderTopRightRadius = 4;
          severityDot.style.borderBottomLeftRadius = 4;
          severityDot.style.borderBottomRightRadius = 4;
          severityDot.style.marginRight = 6;
          severityDot.style.backgroundColor = issue.Severity == OptimizationSeverity.Error
            ? new Color(1f, 0.3f, 0.3f)
            : new Color(1f, 0.7f, 0.3f);
          headerRow.Add(severityDot);

          var nameLabel = new Label(issue.AssetName);
          nameLabel.style.flexGrow = 1;
          nameLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
          headerRow.Add(nameLabel);

          if (issue.PotentialSavings > 0) {
            var savingsLabel = new Label($"-{AssetNodeModel.FormatBytes(issue.PotentialSavings)}");
            savingsLabel.style.color = new Color(0.4f, 0.8f, 0.4f);
            headerRow.Add(savingsLabel);
          }

          row.Add(headerRow);

          var messageLabel = new Label($"{issue.Message}");
          messageLabel.style.color = new Color(0.6f, 0.6f, 0.6f);
          messageLabel.style.fontSize = 11;
          messageLabel.style.marginLeft = 14;
          row.Add(messageLabel);

          var recLabel = new Label($"→ {issue.Recommendation}");
          recLabel.style.color = new Color(0.5f, 0.7f, 0.9f);
          recLabel.style.fontSize = 11;
          recLabel.style.marginLeft = 14;
          row.Add(recLabel);

          section.Add(row);
        }

        if (report.TotalIssues > 20) {
          var more = new Label($"... and {report.TotalIssues - 20} more issues");
          more.style.color = new Color(0.5f, 0.5f, 0.5f);
          more.style.marginTop = 4;
          section.Add(more);
        }
      }

      _scrollView.Add(section);
    }

    void AddCircularDependenciesSection() {
      var section = CreateSection("Circular Dependencies");

      var detector = new CircularDependencyDetector(_graph);
      var result = detector.Detect();

      if (result.TotalCycles == 0) {
        section.Add(CreateEmptyMessage("No circular dependencies found"));
      } else {
        var summary = new Label($"{result.TotalCycles} cycles detected");
        summary.style.marginBottom = 8;
        summary.style.color = new Color(1f, 0.4f, 0.4f);
        section.Add(summary);

        var shown = 0;
        foreach (var cycle in result.Cycles.Take(10)) {
          var cycleContainer = new VisualElement();
          cycleContainer.style.marginBottom = 8;
          cycleContainer.style.paddingLeft = 8;
          cycleContainer.style.borderLeftWidth = 2;
          cycleContainer.style.borderLeftColor = new Color(1f, 0.4f, 0.4f);

          for (int i = 0; i < cycle.AssetPaths.Count; i++) {
            var path = cycle.AssetPaths[i];
            var row = CreateClickableRow(path);

            var arrow = new Label(i == 0 ? "●" : "↓");
            arrow.style.width = 16;
            arrow.style.color = new Color(1f, 0.4f, 0.4f);
            row.Add(arrow);

            var name = System.IO.Path.GetFileNameWithoutExtension(path);
            var nameLabel = new Label(name);
            nameLabel.style.flexGrow = 1;
            row.Add(nameLabel);

            cycleContainer.Add(row);
          }

          section.Add(cycleContainer);
          shown++;
        }

        if (result.TotalCycles > shown) {
          var more = new Label($"... and {result.TotalCycles - shown} more cycles");
          more.style.color = new Color(0.5f, 0.5f, 0.5f);
          more.style.marginTop = 4;
          section.Add(more);
        }
      }

      _scrollView.Add(section);
    }

    void AddDuplicateAssetsSection() {
      var section = CreateSection("Duplicate Assets");

      var duplicates = FindDuplicates();

      if (duplicates.Count == 0) {
        section.Add(CreateEmptyMessage("No duplicate assets found"));
      } else {
        foreach (var group in duplicates.Take(10)) {
          var groupContainer = new VisualElement();
          groupContainer.style.marginBottom = 8;
          groupContainer.style.paddingLeft = 8;
          groupContainer.style.borderLeftWidth = 2;
          groupContainer.style.borderLeftColor = new Color(0.8f, 0.6f, 0.2f);

          var headerLabel = new Label($"{group.Count()} files with same name: {group.Key}");
          headerLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
          headerLabel.style.marginBottom = 4;
          groupContainer.Add(headerLabel);

          foreach (var node in group) {
            var row = CreateClickableRow(node.Path);

            var pathLabel = new Label(node.Path);
            pathLabel.style.flexGrow = 1;
            pathLabel.style.fontSize = 11;
            pathLabel.style.overflow = Overflow.Hidden;
            pathLabel.style.textOverflow = TextOverflow.Ellipsis;
            row.Add(pathLabel);

            var sizeLabel = new Label(node.FormattedSize);
            sizeLabel.style.width = 60;
            sizeLabel.style.unityTextAlign = TextAnchor.MiddleRight;
            row.Add(sizeLabel);

            groupContainer.Add(row);
          }

          section.Add(groupContainer);
        }

        if (duplicates.Count > 10) {
          var more = new Label($"... and {duplicates.Count - 10} more duplicate groups");
          more.style.color = new Color(0.5f, 0.5f, 0.5f);
          more.style.marginTop = 4;
          section.Add(more);
        }
      }

      _scrollView.Add(section);
    }

    List<IGrouping<string, AssetNodeModel>> FindDuplicates() {
      return _graph.Nodes.Values
        .GroupBy(n => n.Name)
        .Where(g => g.Count() > 1)
        .OrderByDescending(g => g.Sum(n => n.SizeBytes))
        .ToList();
    }

    VisualElement CreateSection(string title) {
      var section = new VisualElement();
      section.style.marginBottom = 16;
      section.style.paddingTop = 12;
      section.style.paddingBottom = 12;
      section.style.paddingLeft = 12;
      section.style.paddingRight = 12;
      section.style.backgroundColor = new Color(0.18f, 0.18f, 0.18f);
      section.style.borderTopLeftRadius = 6;
      section.style.borderTopRightRadius = 6;
      section.style.borderBottomLeftRadius = 6;
      section.style.borderBottomRightRadius = 6;

      var header = new Label(title);
      header.style.fontSize = 14;
      header.style.unityFontStyleAndWeight = FontStyle.Bold;
      header.style.marginBottom = 8;
      header.style.paddingBottom = 8;
      header.style.borderBottomWidth = 1;
      header.style.borderBottomColor = new Color(0.3f, 0.3f, 0.3f);
      section.Add(header);

      return section;
    }

    VisualElement CreateRow() {
      var row = new VisualElement();
      row.style.flexDirection = FlexDirection.Row;
      row.style.alignItems = Align.Center;
      row.style.paddingTop = 4;
      row.style.paddingBottom = 4;
      return row;
    }

    VisualElement CreateClickableRow(string assetPath) {
      var row = CreateRow();
      row.style.paddingLeft = 4;
      row.style.paddingRight = 4;
      row.style.borderTopLeftRadius = 3;
      row.style.borderTopRightRadius = 3;
      row.style.borderBottomLeftRadius = 3;
      row.style.borderBottomRightRadius = 3;

      row.RegisterCallback<MouseEnterEvent>(e => {
        row.style.backgroundColor = new Color(0.25f, 0.25f, 0.25f);
      });
      row.RegisterCallback<MouseLeaveEvent>(e => {
        row.style.backgroundColor = StyleKeyword.Null;
      });
      row.RegisterCallback<ClickEvent>(e => {
        var asset = AssetDatabase.LoadAssetAtPath<Object>(assetPath);
        if (asset != null) {
          EditorGUIUtility.PingObject(asset);
          Selection.activeObject = asset;
        }
      });

      return row;
    }

    Label CreateEmptyMessage(string message) {
      var label = new Label(message);
      label.style.color = new Color(0.4f, 0.7f, 0.4f);
      label.style.unityFontStyleAndWeight = FontStyle.Italic;
      return label;
    }

    Color GetTypeColor(AssetType type) {
      return type switch {
        AssetType.Texture => new Color(0.4f, 0.7f, 1f),
        AssetType.Audio => new Color(0.4f, 1f, 0.7f),
        AssetType.Model => new Color(1f, 0.7f, 0.4f),
        AssetType.Material => new Color(0.7f, 0.4f, 1f),
        AssetType.Prefab => new Color(0.4f, 0.8f, 0.8f),
        AssetType.Scene => new Color(1f, 0.5f, 0.5f),
        AssetType.Script => new Color(0.7f, 0.9f, 0.4f),
        AssetType.Shader => new Color(0.9f, 0.4f, 0.7f),
        _ => new Color(0.5f, 0.5f, 0.5f)
      };
    }
  }
}
