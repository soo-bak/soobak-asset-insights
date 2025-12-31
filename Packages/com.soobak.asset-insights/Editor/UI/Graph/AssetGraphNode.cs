using System;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace Soobak.AssetInsights {
  public class AssetGraphNode : Node {
    public string AssetPath { get; }
    public Port InputPort { get; private set; }
    public Port OutputPort { get; private set; }

    public event Action OnDoubleClicked;

    public AssetGraphNode(AssetNodeModel model, bool isCenter = false) {
      AssetPath = model.Path;
      title = model.Name;
      tooltip = $"{model.Path}\n{model.FormattedSize}";

      SetupPorts();
      SetupContent(model, isCenter);
      SetupStyle(model.Type, isCenter);
      SetupInteraction();

      RefreshExpandedState();
      RefreshPorts();
    }

    void SetupPorts() {
      InputPort = Port.Create<Edge>(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(bool));
      InputPort.portName = "";
      InputPort.portColor = new Color(0.4f, 0.7f, 1f);
      inputContainer.Add(InputPort);

      OutputPort = Port.Create<Edge>(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, typeof(bool));
      OutputPort.portName = "";
      OutputPort.portColor = new Color(1f, 0.7f, 0.4f);
      outputContainer.Add(OutputPort);
    }

    void SetupContent(AssetNodeModel model, bool isCenter) {
      var container = new VisualElement();
      container.style.flexDirection = FlexDirection.Row;
      container.style.alignItems = Align.Center;
      container.style.paddingLeft = 4;
      container.style.paddingRight = 4;
      container.style.paddingTop = 4;
      container.style.paddingBottom = 4;

      var icon = new Image();
      icon.style.width = 24;
      icon.style.height = 24;
      icon.style.marginRight = 6;

      var assetIcon = AssetDatabase.GetCachedIcon(model.Path);
      if (assetIcon != null)
        icon.image = assetIcon;

      container.Add(icon);

      var infoContainer = new VisualElement();

      var typeLabel = new Label(model.Type.ToString());
      typeLabel.style.fontSize = 9;
      typeLabel.style.color = new Color(0.6f, 0.6f, 0.6f);
      infoContainer.Add(typeLabel);

      var sizeLabel = new Label(model.FormattedSize);
      sizeLabel.style.fontSize = 10;
      sizeLabel.style.color = new Color(0.7f, 0.7f, 0.7f);
      infoContainer.Add(sizeLabel);

      container.Add(infoContainer);

      extensionContainer.Add(container);
    }

    void SetupStyle(AssetType type, bool isCenter) {
      var color = GetTypeColor(type);

      if (isCenter) {
        style.borderLeftWidth = 3;
        style.borderRightWidth = 3;
        style.borderTopWidth = 3;
        style.borderBottomWidth = 3;
        style.borderLeftColor = new Color(1f, 0.8f, 0.2f);
        style.borderRightColor = new Color(1f, 0.8f, 0.2f);
        style.borderTopColor = new Color(1f, 0.8f, 0.2f);
        style.borderBottomColor = new Color(1f, 0.8f, 0.2f);
      }

      titleContainer.style.backgroundColor = color;
    }

    void SetupInteraction() {
      RegisterCallback<MouseDownEvent>(evt => {
        if (evt.clickCount == 2) {
          OnDoubleClicked?.Invoke();
          evt.StopPropagation();
        }
      });
    }

    static Color GetTypeColor(AssetType type) {
      return type switch {
        AssetType.Texture => new Color(0.2f, 0.4f, 0.6f, 0.8f),
        AssetType.Material => new Color(0.5f, 0.3f, 0.5f, 0.8f),
        AssetType.Shader => new Color(0.6f, 0.3f, 0.3f, 0.8f),
        AssetType.Model => new Color(0.3f, 0.5f, 0.3f, 0.8f),
        AssetType.Prefab => new Color(0.3f, 0.4f, 0.5f, 0.8f),
        AssetType.Scene => new Color(0.5f, 0.4f, 0.2f, 0.8f),
        AssetType.Script => new Color(0.3f, 0.5f, 0.5f, 0.8f),
        AssetType.Audio => new Color(0.5f, 0.3f, 0.2f, 0.8f),
        AssetType.Animation => new Color(0.4f, 0.4f, 0.3f, 0.8f),
        AssetType.ScriptableObject => new Color(0.4f, 0.3f, 0.4f, 0.8f),
        _ => new Color(0.3f, 0.3f, 0.3f, 0.8f)
      };
    }

    public void SetHighlighted(bool highlighted) {
      if (highlighted) {
        AddToClassList("highlighted");
        style.borderLeftWidth = 2;
        style.borderRightWidth = 2;
        style.borderTopWidth = 2;
        style.borderBottomWidth = 2;
        style.borderLeftColor = new Color(0.2f, 0.8f, 0.2f);
        style.borderRightColor = new Color(0.2f, 0.8f, 0.2f);
        style.borderTopColor = new Color(0.2f, 0.8f, 0.2f);
        style.borderBottomColor = new Color(0.2f, 0.8f, 0.2f);
      } else {
        RemoveFromClassList("highlighted");
        style.borderLeftWidth = 0;
        style.borderRightWidth = 0;
        style.borderTopWidth = 0;
        style.borderBottomWidth = 0;
      }
    }
  }
}
