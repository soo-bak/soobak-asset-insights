using UnityEngine;
using UnityEngine.UIElements;

namespace Soobak.AssetInsights {
  public class DashboardPanel : VisualElement {
    readonly DependencyGraph _graph;
    HealthScoreResult _lastResult;

    Label _scoreLabel;
    Label _gradeLabel;
    VisualElement _scoreGauge;
    VisualElement _breakdownContainer;

    public DashboardPanel(DependencyGraph graph) {
      _graph = graph;
      BuildUI();
    }

    void BuildUI() {
      style.flexGrow = 1;
      style.paddingTop = 16;
      style.paddingBottom = 16;
      style.paddingLeft = 16;
      style.paddingRight = 16;

      // Score section
      var scoreSection = new VisualElement();
      scoreSection.style.alignItems = Align.Center;
      scoreSection.style.marginBottom = 24;

      _gradeLabel = new Label("--");
      _gradeLabel.style.fontSize = 64;
      _gradeLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
      _gradeLabel.style.color = new Color(0.5f, 0.5f, 0.5f);
      scoreSection.Add(_gradeLabel);

      _scoreLabel = new Label("Score: --/100");
      _scoreLabel.style.fontSize = 16;
      _scoreLabel.style.color = new Color(0.7f, 0.7f, 0.7f);
      scoreSection.Add(_scoreLabel);

      // Score gauge bar
      var gaugeContainer = new VisualElement();
      gaugeContainer.style.width = Length.Percent(100);
      gaugeContainer.style.height = 12;
      gaugeContainer.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f);
      gaugeContainer.style.borderTopLeftRadius = 6;
      gaugeContainer.style.borderTopRightRadius = 6;
      gaugeContainer.style.borderBottomLeftRadius = 6;
      gaugeContainer.style.borderBottomRightRadius = 6;
      gaugeContainer.style.marginTop = 8;

      _scoreGauge = new VisualElement();
      _scoreGauge.style.height = Length.Percent(100);
      _scoreGauge.style.width = Length.Percent(0);
      _scoreGauge.style.backgroundColor = new Color(0.2f, 0.7f, 0.2f);
      _scoreGauge.style.borderTopLeftRadius = 6;
      _scoreGauge.style.borderBottomLeftRadius = 6;
      gaugeContainer.Add(_scoreGauge);

      scoreSection.Add(gaugeContainer);
      Add(scoreSection);

      // Breakdown section
      var breakdownHeader = new Label("Score Breakdown");
      breakdownHeader.style.fontSize = 14;
      breakdownHeader.style.unityFontStyleAndWeight = FontStyle.Bold;
      breakdownHeader.style.marginBottom = 8;
      Add(breakdownHeader);

      _breakdownContainer = new VisualElement();
      Add(_breakdownContainer);

      // Summary section
      var summarySection = new VisualElement();
      summarySection.style.marginTop = 16;
      summarySection.style.paddingTop = 16;
      summarySection.style.borderTopWidth = 1;
      summarySection.style.borderTopColor = new Color(0.3f, 0.3f, 0.3f);
      summarySection.name = "summary";
      Add(summarySection);
    }

    public void Refresh() {
      if (_graph.NodeCount == 0) {
        _gradeLabel.text = "--";
        _scoreLabel.text = "Scan project first";
        _scoreGauge.style.width = Length.Percent(0);
        _breakdownContainer.Clear();
        return;
      }

      var calculator = new HealthScoreCalculator(_graph);
      _lastResult = calculator.Calculate();

      UpdateScoreDisplay();
      UpdateBreakdown();
      UpdateSummary();
    }

    void UpdateScoreDisplay() {
      _gradeLabel.text = _lastResult.Grade.ToString();
      _gradeLabel.style.color = GetGradeColor(_lastResult.Grade);
      _scoreLabel.text = $"Score: {_lastResult.Score}/100";
      _scoreGauge.style.width = Length.Percent(_lastResult.Score);
      _scoreGauge.style.backgroundColor = GetGradeColor(_lastResult.Grade);
    }

    void UpdateBreakdown() {
      _breakdownContainer.Clear();

      foreach (var item in _lastResult.Breakdown) {
        var row = new VisualElement();
        row.style.flexDirection = FlexDirection.Row;
        row.style.justifyContent = Justify.SpaceBetween;
        row.style.marginBottom = 4;
        row.style.paddingTop = 4;
        row.style.paddingBottom = 4;

        var leftContainer = new VisualElement();
        leftContainer.style.flexDirection = FlexDirection.Row;
        leftContainer.style.alignItems = Align.Center;

        var categoryLabel = new Label(item.Category);
        categoryLabel.style.width = 140;
        categoryLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        leftContainer.Add(categoryLabel);

        var descLabel = new Label(item.Description);
        descLabel.style.color = new Color(0.6f, 0.6f, 0.6f);
        descLabel.style.fontSize = 11;
        leftContainer.Add(descLabel);

        row.Add(leftContainer);

        var penaltyLabel = new Label(item.Penalty > 0 ? $"-{item.Penalty}" : "0");
        penaltyLabel.style.color = item.Penalty > 0 ? new Color(1f, 0.4f, 0.4f) : new Color(0.4f, 0.8f, 0.4f);
        penaltyLabel.style.unityTextAlign = TextAnchor.MiddleRight;
        penaltyLabel.style.width = 40;
        row.Add(penaltyLabel);

        _breakdownContainer.Add(row);
      }
    }

    void UpdateSummary() {
      var summary = this.Q<VisualElement>("summary");
      summary.Clear();

      var statsContainer = new VisualElement();
      statsContainer.style.flexDirection = FlexDirection.Row;
      statsContainer.style.justifyContent = Justify.SpaceAround;

      AddStat(statsContainer, "Unused", $"{_lastResult.FormattedUnusedSize}");
      AddStat(statsContainer, "Potential Savings", $"{_lastResult.FormattedSavings}");
      AddStat(statsContainer, "Cycles", $"{_lastResult.CircularDependencyCount}");
      AddStat(statsContainer, "Large Files", $"{_lastResult.LargeAssetCount}");

      summary.Add(statsContainer);
    }

    void AddStat(VisualElement container, string label, string value) {
      var stat = new VisualElement();
      stat.style.alignItems = Align.Center;

      var valueLabel = new Label(value);
      valueLabel.style.fontSize = 18;
      valueLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
      stat.Add(valueLabel);

      var labelElement = new Label(label);
      labelElement.style.fontSize = 10;
      labelElement.style.color = new Color(0.5f, 0.5f, 0.5f);
      stat.Add(labelElement);

      container.Add(stat);
    }

    static Color GetGradeColor(HealthGrade grade) {
      return grade switch {
        HealthGrade.A => new Color(0.2f, 0.8f, 0.2f),
        HealthGrade.B => new Color(0.5f, 0.8f, 0.2f),
        HealthGrade.C => new Color(0.8f, 0.8f, 0.2f),
        HealthGrade.D => new Color(0.8f, 0.5f, 0.2f),
        HealthGrade.F => new Color(0.8f, 0.2f, 0.2f),
        _ => new Color(0.5f, 0.5f, 0.5f)
      };
    }
  }
}
