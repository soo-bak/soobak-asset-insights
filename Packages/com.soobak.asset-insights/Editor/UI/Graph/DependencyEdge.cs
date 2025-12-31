using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace Soobak.AssetInsights {
  public class DependencyEdge : Edge {
    public DependencyEdge() {
      edgeControl.edgeWidth = 2;

      RegisterCallback<MouseEnterEvent>(OnMouseEnter);
      RegisterCallback<MouseLeaveEvent>(OnMouseLeave);
    }

    void OnMouseEnter(MouseEnterEvent evt) {
      edgeControl.edgeWidth = 4;
      edgeControl.inputColor = new Color(1f, 0.9f, 0.3f);
      edgeControl.outputColor = new Color(1f, 0.9f, 0.3f);
    }

    void OnMouseLeave(MouseLeaveEvent evt) {
      edgeControl.edgeWidth = 2;
      UpdateEdgeColor();
    }

    void UpdateEdgeColor() {
      var defaultInputColor = new Color(0.4f, 0.7f, 1f);
      var defaultOutputColor = new Color(1f, 0.7f, 0.4f);

      if (ClassListContains("highlighted")) {
        edgeControl.inputColor = new Color(0.2f, 1f, 0.2f);
        edgeControl.outputColor = new Color(0.2f, 1f, 0.2f);
        edgeControl.edgeWidth = 3;
      } else {
        edgeControl.inputColor = defaultInputColor;
        edgeControl.outputColor = defaultOutputColor;
      }
    }

    public override void OnSelected() {
      base.OnSelected();
      edgeControl.edgeWidth = 3;
    }

    public override void OnUnselected() {
      base.OnUnselected();
      UpdateEdgeColor();
    }
  }
}
