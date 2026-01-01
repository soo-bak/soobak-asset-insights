using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Soobak.AssetInsights {
  public class MaterialOptimizationRule : IOptimizationRule {
    public string RuleName => "Material Optimization";
    public string Description => "Detects material issues like unused properties and shader problems";
    public OptimizationSeverity Severity => OptimizationSeverity.Warning;

    static readonly HashSet<string> CommonUnusedProperties = new() {
      "_MainTex", "_BumpMap", "_MetallicGlossMap", "_OcclusionMap",
      "_EmissionMap", "_DetailMask", "_DetailAlbedoMap", "_DetailNormalMap"
    };

    public IEnumerable<OptimizationIssue> Evaluate(AssetNodeModel node, DependencyGraph graph) {
      if (node.Type != AssetType.Material)
        yield break;

      var material = AssetDatabase.LoadAssetAtPath<Material>(node.Path);
      if (material == null)
        yield break;

      // Check for missing shader
      if (material.shader == null || material.shader.name == "Hidden/InternalErrorShader") {
        yield return new OptimizationIssue {
          RuleName = "Missing Shader",
          AssetPath = node.Path,
          AssetName = node.Name,
          Severity = OptimizationSeverity.Error,
          Message = "Material has a missing or broken shader",
          Recommendation = "Assign a valid shader to this material",
          PotentialSavings = 0,
          IsAutoFixable = false
        };
        yield break;
      }

      // Check for Standard shader usage (could use simpler shader)
      if (material.shader.name == "Standard" || material.shader.name == "Standard (Specular setup)") {
        var usesEmission = material.IsKeywordEnabled("_EMISSION");
        var usesBumpMap = material.IsKeywordEnabled("_NORMALMAP");
        var usesMetallic = material.IsKeywordEnabled("_METALLICGLOSSMAP");

        if (!usesEmission && !usesBumpMap && !usesMetallic) {
          yield return new OptimizationIssue {
            RuleName = "Overly Complex Shader",
            AssetPath = node.Path,
            AssetName = node.Name,
            Severity = OptimizationSeverity.Info,
            Message = "Using Standard shader without advanced features",
            Recommendation = "Consider using a simpler shader like 'Unlit' or 'Mobile/Diffuse'",
            PotentialSavings = 0,
            IsAutoFixable = false
          };
        }
      }

      // Check for unused texture slots with assigned textures
      var unusedTextureCount = 0;
      var shader = material.shader;
      var propertyCount = ShaderUtil.GetPropertyCount(shader);

      for (int i = 0; i < propertyCount; i++) {
        if (ShaderUtil.GetPropertyType(shader, i) != ShaderUtil.ShaderPropertyType.TexEnv)
          continue;

        var propName = ShaderUtil.GetPropertyName(shader, i);
        var texture = material.GetTexture(propName);

        if (texture == null)
          continue;

        // Check if the texture is actually used based on keywords
        if (IsTextureUnused(material, propName)) {
          unusedTextureCount++;
        }
      }

      if (unusedTextureCount > 0) {
        yield return new OptimizationIssue {
          RuleName = "Unused Texture Slots",
          AssetPath = node.Path,
          AssetName = node.Name,
          Severity = OptimizationSeverity.Warning,
          Message = $"Material has {unusedTextureCount} texture(s) that may not be used",
          Recommendation = "Review and remove unused texture references",
          PotentialSavings = 0,
          IsAutoFixable = false
        };
      }

      // Check for materials with many texture references (potential memory hog)
      var textureCount = 0;
      long totalTextureSize = 0;

      for (int i = 0; i < propertyCount; i++) {
        if (ShaderUtil.GetPropertyType(shader, i) != ShaderUtil.ShaderPropertyType.TexEnv)
          continue;

        var propName = ShaderUtil.GetPropertyName(shader, i);
        var texture = material.GetTexture(propName);

        if (texture != null) {
          textureCount++;
          var texturePath = AssetDatabase.GetAssetPath(texture);
          if (graph.TryGetNode(texturePath, out var texNode)) {
            totalTextureSize += texNode.SizeBytes;
          }
        }
      }

      if (textureCount > 6) {
        yield return new OptimizationIssue {
          RuleName = "Many Texture References",
          AssetPath = node.Path,
          AssetName = node.Name,
          Severity = OptimizationSeverity.Info,
          Message = $"Material references {textureCount} textures ({AssetNodeModel.FormatBytes(totalTextureSize)} total)",
          Recommendation = "Consider using texture atlasing or reducing texture count",
          PotentialSavings = 0,
          IsAutoFixable = false
        };
      }

      // Check for GPU Instancing
      if (!material.enableInstancing && ShouldEnableInstancing(node.Path)) {
        yield return new OptimizationIssue {
          RuleName = "GPU Instancing Disabled",
          AssetPath = node.Path,
          AssetName = node.Name,
          Severity = OptimizationSeverity.Info,
          Message = "GPU Instancing is disabled",
          Recommendation = "Enable GPU Instancing for better batching performance",
          PotentialSavings = 0,
          IsAutoFixable = true
        };
      }

      // Check render queue issues
      if (material.renderQueue > 3000 && !material.shader.name.Contains("Transparent")) {
        yield return new OptimizationIssue {
          RuleName = "High Render Queue",
          AssetPath = node.Path,
          AssetName = node.Name,
          Severity = OptimizationSeverity.Info,
          Message = $"Material has render queue {material.renderQueue} (transparent range)",
          Recommendation = "Verify render queue is intentionally set to transparent range",
          PotentialSavings = 0,
          IsAutoFixable = false
        };
      }
    }

    bool IsTextureUnused(Material material, string propName) {
      // Check common cases where textures are assigned but features are disabled
      return propName switch {
        "_BumpMap" => !material.IsKeywordEnabled("_NORMALMAP"),
        "_EmissionMap" => !material.IsKeywordEnabled("_EMISSION"),
        "_MetallicGlossMap" => !material.IsKeywordEnabled("_METALLICGLOSSMAP"),
        "_ParallaxMap" => !material.IsKeywordEnabled("_PARALLAXMAP"),
        "_OcclusionMap" => false, // Usually always used if assigned
        "_DetailMask" => !material.IsKeywordEnabled("_DETAIL_MULX2"),
        "_DetailAlbedoMap" => !material.IsKeywordEnabled("_DETAIL_MULX2"),
        "_DetailNormalMap" => !material.IsKeywordEnabled("_DETAIL_MULX2"),
        _ => false
      };
    }

    bool ShouldEnableInstancing(string path) {
      // Environment, props, and foliage typically benefit from instancing
      return path.Contains("/Environment/") ||
             path.Contains("/Props/") ||
             path.Contains("/Foliage/") ||
             path.Contains("/Vegetation/") ||
             path.Contains("/Buildings/");
    }
  }
}
