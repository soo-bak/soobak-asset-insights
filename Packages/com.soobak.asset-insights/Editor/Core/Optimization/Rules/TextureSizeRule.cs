using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Soobak.AssetInsights {
  public class TextureSizeRule : IOptimizationRule {
    const int MaxRecommendedSize = 2048;
    const int UITextureMaxSize = 1024;

    public string RuleName => "Large Texture";
    public string Description => "Detects textures larger than recommended sizes";
    public OptimizationSeverity Severity => OptimizationSeverity.Warning;

    public IEnumerable<OptimizationIssue> Evaluate(AssetNodeModel node, DependencyGraph graph) {
      if (node.Type != AssetType.Texture)
        yield break;

      var importer = AssetImporter.GetAtPath(node.Path) as TextureImporter;
      if (importer == null)
        yield break;

      var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(node.Path);
      if (texture == null)
        yield break;

      var maxDimension = Mathf.Max(texture.width, texture.height);
      var isUITexture = node.Path.Contains("/UI/") || node.Path.Contains("/Sprites/");
      var threshold = isUITexture ? UITextureMaxSize : MaxRecommendedSize;

      if (maxDimension > threshold) {
        var currentSize = node.SizeBytes;
        var reductionFactor = (float)threshold / maxDimension;
        var estimatedNewSize = (long)(currentSize * reductionFactor * reductionFactor);
        var savings = currentSize - estimatedNewSize;

        yield return new OptimizationIssue {
          RuleName = RuleName,
          AssetPath = node.Path,
          AssetName = node.Name,
          Severity = Severity,
          Message = $"Texture is {texture.width}x{texture.height} (max dimension: {maxDimension}px)",
          Recommendation = $"Consider reducing to {threshold}px or less for {(isUITexture ? "UI textures" : "general textures")}",
          PotentialSavings = savings,
          IsAutoFixable = false
        };
      }

      // Check for uncompressed textures
      if (importer.textureCompression == TextureImporterCompression.Uncompressed) {
        yield return new OptimizationIssue {
          RuleName = "Uncompressed Texture",
          AssetPath = node.Path,
          AssetName = node.Name,
          Severity = OptimizationSeverity.Warning,
          Message = "Texture is uncompressed",
          Recommendation = "Enable compression to reduce memory usage",
          PotentialSavings = node.SizeBytes / 4, // Rough estimate
          IsAutoFixable = true
        };
      }

      // Check for mipmaps on UI textures
      if (isUITexture && importer.mipmapEnabled) {
        yield return new OptimizationIssue {
          RuleName = "Unnecessary Mipmaps",
          AssetPath = node.Path,
          AssetName = node.Name,
          Severity = OptimizationSeverity.Info,
          Message = "UI texture has mipmaps enabled",
          Recommendation = "Disable mipmaps for UI textures to save memory",
          PotentialSavings = node.SizeBytes / 3,
          IsAutoFixable = true
        };
      }
    }
  }
}
