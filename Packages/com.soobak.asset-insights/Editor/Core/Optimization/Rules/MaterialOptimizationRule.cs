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

      // Collect all issues without yielding during material load
      var issues = new List<OptimizationIssue>();

      var material = AssetDatabase.LoadAssetAtPath<Material>(node.Path);
      if (material != null) {
        try {
          EvaluateMaterial(material, node, graph, issues);
        } finally {
          // Unload the material to free memory
          Resources.UnloadAsset(material);
        }
      }

      // Yield issues after material is unloaded
      foreach (var issue in issues) {
        yield return issue;
      }
    }

    void EvaluateMaterial(Material material, AssetNodeModel node, DependencyGraph graph, List<OptimizationIssue> issues) {

      // Check for missing shader
      if (material.shader == null || material.shader.name == "Hidden/InternalErrorShader") {
        issues.Add(new OptimizationIssue {
          RuleName = "Missing Shader",
          AssetPath = node.Path,
          AssetName = node.Name,
          Severity = OptimizationSeverity.Error,
          Message = "Material has a missing or broken shader",
          Recommendation = "Assign a valid shader to this material",
          PotentialSavings = 0,
          IsAutoFixable = false
        });
        return;
      }

      // Check for Standard shader usage (could use simpler shader)
      if (material.shader.name == "Standard" || material.shader.name == "Standard (Specular setup)") {
        var usesEmission = material.IsKeywordEnabled("_EMISSION");
        var usesBumpMap = material.IsKeywordEnabled("_NORMALMAP");
        var usesMetallic = material.IsKeywordEnabled("_METALLICGLOSSMAP");

        if (!usesEmission && !usesBumpMap && !usesMetallic) {
          issues.Add(new OptimizationIssue {
            RuleName = "Overly Complex Shader",
            AssetPath = node.Path,
            AssetName = node.Name,
            Severity = OptimizationSeverity.Info,
            Message = "Using Standard shader without advanced features",
            Recommendation = "Consider using a simpler shader like 'Unlit' or 'Mobile/Diffuse'",
            PotentialSavings = 0,
            IsAutoFixable = false
          });
        }
      }

      // Single pass through texture properties (previously was two loops)
      var shader = material.shader;
      var propertyCount = ShaderUtil.GetPropertyCount(shader);
      var unusedTextureCount = 0;
      var textureCount = 0;
      long totalTextureSize = 0;

      for (int i = 0; i < propertyCount; i++) {
        if (ShaderUtil.GetPropertyType(shader, i) != ShaderUtil.ShaderPropertyType.TexEnv)
          continue;

        var propName = ShaderUtil.GetPropertyName(shader, i);
        // Note: GetTexture returns already-loaded reference, doesn't load new texture
        var texture = material.GetTexture(propName);

        if (texture == null)
          continue;

        textureCount++;

        // Check if unused
        if (IsTextureUnused(material, propName)) {
          unusedTextureCount++;
        }

        // Calculate size from graph (no additional asset loading)
        var texturePath = AssetDatabase.GetAssetPath(texture);
        if (graph.TryGetNode(texturePath, out var texNode)) {
          totalTextureSize += texNode.SizeBytes;
        }
      }

      if (unusedTextureCount > 0) {
        issues.Add(new OptimizationIssue {
          RuleName = "Unused Texture Slots",
          AssetPath = node.Path,
          AssetName = node.Name,
          Severity = OptimizationSeverity.Warning,
          Message = $"Material has {unusedTextureCount} texture(s) that may not be used",
          Recommendation = "Review and remove unused texture references",
          PotentialSavings = 0,
          IsAutoFixable = false
        });
      }

      if (textureCount > 6) {
        issues.Add(new OptimizationIssue {
          RuleName = "Many Texture References",
          AssetPath = node.Path,
          AssetName = node.Name,
          Severity = OptimizationSeverity.Info,
          Message = $"Material references {textureCount} textures ({AssetNodeModel.FormatBytes(totalTextureSize)} total)",
          Recommendation = "Consider using texture atlasing or reducing texture count",
          PotentialSavings = 0,
          IsAutoFixable = false
        });
      }

      // Check for GPU Instancing
      if (!material.enableInstancing && ShouldEnableInstancing(node.Path)) {
        issues.Add(new OptimizationIssue {
          RuleName = "GPU Instancing Disabled",
          AssetPath = node.Path,
          AssetName = node.Name,
          Severity = OptimizationSeverity.Info,
          Message = "GPU Instancing is disabled",
          Recommendation = "Enable GPU Instancing for better batching performance",
          PotentialSavings = 0,
          IsAutoFixable = true
        });
      }

      // Check render queue issues
      if (material.renderQueue > 3000 && !material.shader.name.Contains("Transparent")) {
        issues.Add(new OptimizationIssue {
          RuleName = "High Render Queue",
          AssetPath = node.Path,
          AssetName = node.Name,
          Severity = OptimizationSeverity.Info,
          Message = $"Material has render queue {material.renderQueue} (transparent range)",
          Recommendation = "Verify render queue is intentionally set to transparent range",
          PotentialSavings = 0,
          IsAutoFixable = false
        });
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
