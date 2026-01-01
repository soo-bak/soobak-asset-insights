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

      // Collect all issues first, then yield them after unloading the texture
      var issues = new List<OptimizationIssue>();
      var isUITexture = node.Path.Contains("/UI/") || node.Path.Contains("/Sprites/");

      // Load texture to check dimensions
      var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(node.Path);
      if (texture != null) {
        try {
          var maxDimension = Mathf.Max(texture.width, texture.height);
          var threshold = isUITexture ? UITextureMaxSize : MaxRecommendedSize;

          if (maxDimension > threshold) {
            var currentSize = node.SizeBytes;
            var reductionFactor = (float)threshold / maxDimension;
            var estimatedNewSize = (long)(currentSize * reductionFactor * reductionFactor);
            var savings = currentSize - estimatedNewSize;

            issues.Add(new OptimizationIssue {
              RuleName = RuleName,
              AssetPath = node.Path,
              AssetName = node.Name,
              Severity = Severity,
              Message = $"Texture is {texture.width}x{texture.height} (max dimension: {maxDimension}px)",
              Recommendation = $"Consider reducing to {threshold}px or less for {(isUITexture ? "UI textures" : "general textures")}",
              PotentialSavings = savings,
              IsAutoFixable = false
            });
          }
        } finally {
          // CRITICAL: Unload the texture to free GPU/CPU memory
          Resources.UnloadAsset(texture);
        }
      }

      // Check importer settings (doesn't require loaded texture)
      if (importer.textureCompression == TextureImporterCompression.Uncompressed) {
        issues.Add(new OptimizationIssue {
          RuleName = "Uncompressed Texture",
          AssetPath = node.Path,
          AssetName = node.Name,
          Severity = OptimizationSeverity.Warning,
          Message = "Texture is uncompressed",
          Recommendation = "Enable compression to reduce memory usage",
          PotentialSavings = node.SizeBytes / 4,
          IsAutoFixable = true,
          FixType = FixType.TextureEnableCompression
        });
      }

      // Check for mipmaps on UI textures
      if (isUITexture && importer.mipmapEnabled) {
        issues.Add(new OptimizationIssue {
          RuleName = "Unnecessary Mipmaps",
          AssetPath = node.Path,
          AssetName = node.Name,
          Severity = OptimizationSeverity.Info,
          Message = "UI texture has mipmaps enabled",
          Recommendation = "Disable mipmaps for UI textures to save memory",
          PotentialSavings = node.SizeBytes / 3,
          IsAutoFixable = true,
          FixType = FixType.TextureDisableMipmaps
        });
      }

      // Yield all collected issues
      foreach (var issue in issues) {
        yield return issue;
      }
    }
  }
}
