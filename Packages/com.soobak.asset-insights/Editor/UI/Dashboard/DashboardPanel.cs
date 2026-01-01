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

    // Cached analysis results to prevent repeated heavy operations
    UnusedAssetResult _cachedUnusedResult;
    OptimizationReport _cachedOptimizationReport;
    OptimizationEngine _cachedOptimizationEngine;
    CircularDependencyResult _cachedCircularResult;
    ResourcesLoadResult _cachedResourcesResult;
    List<IGrouping<string, AssetNodeModel>> _cachedDuplicates;
    HashSet<string> _cachedUnusedAssets;
    HashSet<string> _cachedCircularAssets;
    bool _analysisDirty = true;
    bool _hasExternalCache;

    public DashboardPanel(DependencyGraph graph) {
      _graph = graph;
      BuildUI();
    }

    /// <summary>
    /// Set cached analysis data from parent window to avoid duplicate computation.
    /// </summary>
    public void SetCachedAnalysisData(
      OptimizationEngine engine,
      HashSet<string> unusedAssets,
      HashSet<string> circularAssets,
      UnusedAssetResult unusedResult = null,
      CircularDependencyResult circularResult = null) {
      _cachedOptimizationEngine = engine;
      _cachedOptimizationReport = engine?.LastReport;
      _cachedUnusedAssets = unusedAssets;
      _cachedCircularAssets = circularAssets;
      _cachedUnusedResult = unusedResult;
      _cachedCircularResult = circularResult;
      _hasExternalCache = true;
    }

    void BuildUI() {
      style.flexGrow = 1;

      _scrollView = new ScrollView(ScrollViewMode.Vertical);
      _scrollView.style.flexGrow = 1;
      Add(_scrollView);
    }

    public void SetResult(HealthScoreResult result) {
      // Only mark dirty if result actually changed
      if (_cachedResult != result) {
        _cachedResult = result;
        _analysisDirty = true;
      }
      Refresh();
    }

    public void InvalidateCache() {
      _analysisDirty = true;
      _cachedUnusedResult = null;
      _cachedOptimizationReport = null;
      _cachedCircularResult = null;
      _cachedResourcesResult = null;
      _cachedDuplicates = null;
      // Don't clear external cache - it's managed by parent window
      if (!_hasExternalCache) {
        _cachedOptimizationEngine = null;
        _cachedUnusedAssets = null;
        _cachedCircularAssets = null;
      }
    }

    void RunAnalysisIfNeeded() {
      if (!_analysisDirty)
        return;

      _analysisDirty = false;

      // When external cache is available from parent window, prefer it completely.
      // This prevents duplicate expensive analysis operations.

      // Optimization analysis - use cached if available
      if (_cachedOptimizationReport == null) {
        if (_cachedOptimizationEngine != null) {
          _cachedOptimizationReport = _cachedOptimizationEngine.LastReport ?? _cachedOptimizationEngine.Analyze();
        } else {
          var engine = new OptimizationEngine(_graph);
          _cachedOptimizationReport = engine.Analyze();
          _cachedOptimizationEngine = engine;
        }
      }

      // Unused analysis - only run if not provided by parent
      if (_cachedUnusedResult == null) {
        var unusedAnalyzer = new UnusedAssetAnalyzer(_graph);
        _cachedUnusedResult = unusedAnalyzer.Analyze();
      }

      // Circular dependency detection - only run if not provided by parent
      if (_cachedCircularResult == null) {
        var detector = new CircularDependencyDetector(_graph);
        _cachedCircularResult = detector.Detect();
      }

      // Resources.Load detection (lightweight, always run if needed)
      if (_cachedResourcesResult == null) {
        var resourcesDetector = new ResourcesLoadDetector(_graph);
        _cachedResourcesResult = resourcesDetector.Detect();
      }

      // Duplicate detection (lightweight in-memory grouping)
      if (_cachedDuplicates == null) {
        _cachedDuplicates = FindDuplicatesInternal();
      }
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

      // Run analysis once and cache results
      RunAnalysisIfNeeded();

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

      // Dynamic Loading
      AddResourcesLoadSection();
    }

    void AddTypeBreakdownSection() {
      var byType = _graph.GetSizeByType()
        .OrderByDescending(kv => kv.Value.totalSize)
        .ToList();

      var totalSize = _graph.GetTotalSize();

      var section = CreateSection(
        $"Type Breakdown ({byType.Count} types)",
        "Distribution of assets by file type and their storage usage"
      );

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
      var largest = _graph.GetNodesBySize(10);
      var totalLargeSize = largest.Sum(n => n.SizeBytes);

      var section = CreateSection(
        $"Largest Assets (Top 10)",
        $"Files consuming the most storage space ({AssetNodeModel.FormatBytes(totalLargeSize)} total)"
      );

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
      var result = _cachedUnusedResult;
      if (result == null) return;

      var headerText = result.TotalUnusedCount == 0
        ? "Unused Assets"
        : $"Unused Assets ({result.TotalUnusedCount})";

      var section = CreateSection(
        headerText,
        "Assets not referenced by any scene or other asset - safe to delete"
      );

      if (result.TotalUnusedCount == 0) {
        section.Add(CreateEmptyMessage("No unused assets found"));
      } else {
        var summary = new Label($"Total wasted space: {AssetNodeModel.FormatBytes(result.TotalUnusedSize)}");
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
      var report = _cachedOptimizationReport;
      if (report == null) return;

      var headerText = report.TotalIssues == 0
        ? "Optimization Issues"
        : $"Optimization Issues ({report.TotalIssues})";

      var potentialSavings = report.Issues.Sum(i => i.PotentialSavings);
      var description = potentialSavings > 0
        ? $"Potential size reduction: {AssetNodeModel.FormatBytes(potentialSavings)}"
        : "Texture, audio, and import settings that can be improved";

      var section = CreateSection(headerText, description);

      if (report.TotalIssues == 0) {
        section.Add(CreateEmptyMessage("No optimization issues found"));
      } else {
        // Fix All button for auto-fixable issues
        var fixableIssues = report.Issues.Where(i => i.IsAutoFixable).ToList();
        if (fixableIssues.Count > 0) {
          var fixAllRow = new VisualElement();
          fixAllRow.style.flexDirection = FlexDirection.Row;
          fixAllRow.style.justifyContent = Justify.FlexEnd;
          fixAllRow.style.marginBottom = 8;

          var fixAllBtn = new Button(() => {
            if (EditorUtility.DisplayDialog(
              "Fix All Issues",
              $"Apply {fixableIssues.Count} automatic fix(es)?\n\nThis will modify import settings for multiple assets.",
              "Fix All", "Cancel")) {
              var results = AssetFixer.ApplyFixes(fixableIssues);
              var successCount = results.Count(r => r.Success);
              EditorUtility.DisplayDialog(
                "Fix Complete",
                $"Successfully applied {successCount}/{results.Count} fixes.",
                "OK");
              // Invalidate cache and refresh
              InvalidateCache();
              Refresh();
            }
          });
          fixAllBtn.text = $"Fix All ({fixableIssues.Count})";
          fixAllBtn.style.backgroundColor = new Color(0.2f, 0.5f, 0.3f);
          fixAllBtn.style.paddingLeft = 12;
          fixAllBtn.style.paddingRight = 12;
          fixAllRow.Add(fixAllBtn);

          section.Add(fixAllRow);
        }

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
            savingsLabel.style.marginRight = 8;
            headerRow.Add(savingsLabel);
          }

          // Add fix button if auto-fixable
          if (issue.IsAutoFixable) {
            var fixBtn = CreateFixButton(issue);
            headerRow.Add(fixBtn);
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

    Button CreateFixButton(OptimizationIssue issue) {
      var fixBtn = new Button(() => {
        var result = AssetFixer.ApplyFix(issue);
        if (result.Success) {
          EditorUtility.DisplayDialog("Fix Applied", result.Message, "OK");
          InvalidateCache();
          Refresh();
        } else {
          EditorUtility.DisplayDialog("Fix Failed", result.Message, "OK");
        }
      });
      fixBtn.text = "Fix";
      fixBtn.tooltip = AssetFixer.GetFixDescription(issue.FixType);
      fixBtn.style.fontSize = 10;
      fixBtn.style.height = 18;
      fixBtn.style.paddingLeft = 8;
      fixBtn.style.paddingRight = 8;
      fixBtn.style.backgroundColor = new Color(0.2f, 0.5f, 0.3f);
      return fixBtn;
    }

    void AddCircularDependenciesSection() {
      var result = _cachedCircularResult;
      if (result == null) return;

      var headerText = result.TotalCycles == 0
        ? "Circular Dependencies"
        : $"Circular Dependencies ({result.TotalCycles})";

      var section = CreateSection(
        headerText,
        "Assets that reference each other in a loop - can cause loading issues"
      );

      if (result.TotalCycles == 0) {
        section.Add(CreateEmptyMessage("No circular dependencies found"));
      } else {
        var shown = 0;
        foreach (var cycle in result.Cycles.Take(10)) {
          var cycleContainer = new VisualElement();
          cycleContainer.style.marginBottom = 12;
          cycleContainer.style.paddingTop = 8;
          cycleContainer.style.paddingBottom = 8;
          cycleContainer.style.paddingLeft = 12;
          cycleContainer.style.paddingRight = 12;
          cycleContainer.style.backgroundColor = new Color(0.15f, 0.12f, 0.12f);
          cycleContainer.style.borderTopLeftRadius = 4;
          cycleContainer.style.borderTopRightRadius = 4;
          cycleContainer.style.borderBottomLeftRadius = 4;
          cycleContainer.style.borderBottomRightRadius = 4;

          // Get ordered cycle path
          var orderedPaths = cycle.GetOrderedCyclePath(_graph);

          // Show cycle chain visualization: A → B → C → A
          var chainRow = new VisualElement();
          chainRow.style.flexDirection = FlexDirection.Row;
          chainRow.style.flexWrap = Wrap.Wrap;
          chainRow.style.alignItems = Align.Center;
          chainRow.style.marginBottom = 8;

          for (int i = 0; i < orderedPaths.Count; i++) {
            var path = orderedPaths[i];
            var name = System.IO.Path.GetFileNameWithoutExtension(path);

            var nameLabel = new Label(name);
            nameLabel.style.color = new Color(1f, 0.7f, 0.7f);
            nameLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            chainRow.Add(nameLabel);

            // Add arrow (including closing arrow back to first)
            var arrowLabel = new Label(" → ");
            arrowLabel.style.color = new Color(1f, 0.4f, 0.4f);
            chainRow.Add(arrowLabel);
          }

          // Add first name again to close the cycle
          if (orderedPaths.Count > 0) {
            var firstName = System.IO.Path.GetFileNameWithoutExtension(orderedPaths[0]);
            var closeLabel = new Label(firstName);
            closeLabel.style.color = new Color(1f, 0.7f, 0.7f);
            closeLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            chainRow.Add(closeLabel);
          }

          cycleContainer.Add(chainRow);

          // Show details for each asset in the cycle
          for (int i = 0; i < orderedPaths.Count; i++) {
            var path = orderedPaths[i];
            var nextPath = orderedPaths[(i + 1) % orderedPaths.Count];

            var row = CreateClickableRow(path);

            var icon = new Image();
            icon.image = AssetDatabase.GetCachedIcon(path);
            icon.style.width = 14;
            icon.style.height = 14;
            icon.style.marginRight = 4;
            row.Add(icon);

            var name = System.IO.Path.GetFileNameWithoutExtension(path);
            var nameLabel = new Label(name);
            nameLabel.style.width = 120;
            nameLabel.style.overflow = Overflow.Hidden;
            nameLabel.style.textOverflow = TextOverflow.Ellipsis;
            row.Add(nameLabel);

            var refLabel = new Label("references");
            refLabel.style.color = new Color(0.5f, 0.5f, 0.5f);
            refLabel.style.fontSize = 10;
            refLabel.style.marginLeft = 4;
            refLabel.style.marginRight = 4;
            row.Add(refLabel);

            var nextName = System.IO.Path.GetFileNameWithoutExtension(nextPath);
            var nextLabel = new Label(nextName);
            nextLabel.style.color = new Color(0.8f, 0.6f, 0.6f);
            row.Add(nextLabel);

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
      var duplicates = _cachedDuplicates;
      if (duplicates == null) return;

      var headerText = duplicates.Count == 0
        ? "Duplicate Assets"
        : $"Duplicate Assets ({duplicates.Count} groups)";

      var wastedSize = duplicates.Sum(g => g.Skip(1).Sum(n => n.SizeBytes));
      var description = wastedSize > 0
        ? $"Same file names in multiple locations - {AssetNodeModel.FormatBytes(wastedSize)} wasted"
        : "Files with the same name in multiple locations";

      var section = CreateSection(headerText, description);

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

    List<IGrouping<string, AssetNodeModel>> FindDuplicatesInternal() {
      return _graph.Nodes.Values
        .GroupBy(n => n.Name)
        .Where(g => g.Count() > 1)
        .OrderByDescending(g => g.Sum(n => n.SizeBytes))
        .ToList();
    }

    void AddResourcesLoadSection() {
      var result = _cachedResourcesResult;
      if (result == null) return;

      var headerText = result.TotalReferences == 0
        ? "Dynamic Loading (Resources.Load)"
        : $"Dynamic Loading ({result.TotalReferences} calls)";

      var section = CreateSection(
        headerText,
        "Assets loaded via Resources.Load() at runtime - ensure they exist in Resources folder"
      );

      if (result.TotalReferences == 0) {
        section.Add(CreateEmptyMessage("No Resources.Load calls found"));
      } else {
        // Summary
        var summaryRow = new VisualElement();
        summaryRow.style.flexDirection = FlexDirection.Row;
        summaryRow.style.marginBottom = 8;

        var resolvedLabel = new Label($"Resolved: {result.TotalResolved}");
        resolvedLabel.style.color = new Color(0.4f, 0.8f, 0.4f);
        resolvedLabel.style.marginRight = 16;
        summaryRow.Add(resolvedLabel);

        if (result.TotalUnresolved > 0) {
          var unresolvedLabel = new Label($"Unresolved: {result.TotalUnresolved}");
          unresolvedLabel.style.color = new Color(1f, 0.6f, 0.4f);
          summaryRow.Add(unresolvedLabel);
        }

        section.Add(summaryRow);

        // Show references
        var shown = 0;
        foreach (var reference in result.References.Take(15)) {
          var refContainer = new VisualElement();
          refContainer.style.marginBottom = 6;
          refContainer.style.paddingLeft = 8;
          refContainer.style.borderLeftWidth = 2;
          refContainer.style.borderLeftColor = reference.IsResolved
            ? new Color(0.4f, 0.8f, 0.4f)
            : new Color(1f, 0.6f, 0.4f);

          // Script info
          var scriptRow = new VisualElement();
          scriptRow.style.flexDirection = FlexDirection.Row;

          var scriptLabel = new Label(reference.ScriptName);
          scriptLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
          scriptLabel.style.fontSize = 11;
          scriptRow.Add(scriptLabel);

          var lineLabel = new Label($" :{reference.LineNumber}");
          lineLabel.style.color = new Color(0.5f, 0.5f, 0.5f);
          lineLabel.style.fontSize = 10;
          scriptRow.Add(lineLabel);

          refContainer.Add(scriptRow);

          // Resource path
          var pathLabel = new Label($"Resources.Load(\"{reference.ResourcePath}\")");
          pathLabel.style.fontSize = 10;
          pathLabel.style.color = new Color(0.7f, 0.7f, 0.7f);
          refContainer.Add(pathLabel);

          // Resolved asset (if any)
          if (reference.IsResolved) {
            var resolvedRow = CreateClickableRow(reference.ResolvedAssetPath);
            var arrow = new Label("→ ");
            arrow.style.color = new Color(0.4f, 0.8f, 0.4f);
            arrow.style.fontSize = 10;
            resolvedRow.Insert(0, arrow);
            refContainer.Add(resolvedRow);
          }

          section.Add(refContainer);
          shown++;
        }

        if (result.TotalReferences > shown) {
          var more = new Label($"... and {result.TotalReferences - shown} more");
          more.style.color = new Color(0.5f, 0.5f, 0.5f);
          more.style.marginTop = 4;
          section.Add(more);
        }
      }

      _scrollView.Add(section);
    }

    VisualElement CreateSection(string title, string description = null) {
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

      var headerContainer = new VisualElement();
      headerContainer.style.marginBottom = 8;
      headerContainer.style.paddingBottom = 8;
      headerContainer.style.borderBottomWidth = 1;
      headerContainer.style.borderBottomColor = new Color(0.3f, 0.3f, 0.3f);

      var header = new Label(title);
      header.style.fontSize = 14;
      header.style.unityFontStyleAndWeight = FontStyle.Bold;
      headerContainer.Add(header);

      if (!string.IsNullOrEmpty(description)) {
        var desc = new Label(description);
        desc.style.fontSize = 11;
        desc.style.color = new Color(0.6f, 0.6f, 0.6f);
        desc.style.marginTop = 2;
        headerContainer.Add(desc);
      }

      section.Add(headerContainer);

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

    /// <summary>
    /// Cleanup resources when the panel is being destroyed.
    /// </summary>
    public void Cleanup() {
      // Clear all cached data
      _cachedResult = null;
      _cachedUnusedResult = null;
      _cachedOptimizationReport = null;
      _cachedCircularResult = null;
      _cachedResourcesResult = null;
      _cachedDuplicates = null;

      // Don't null external cache references - they're managed by parent
      if (!_hasExternalCache) {
        _cachedOptimizationEngine = null;
        _cachedUnusedAssets = null;
        _cachedCircularAssets = null;
      }

      // Clear UI
      _scrollView?.Clear();
    }
  }
}
