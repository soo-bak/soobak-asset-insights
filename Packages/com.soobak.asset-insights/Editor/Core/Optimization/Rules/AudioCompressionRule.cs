using System.Collections.Generic;
using UnityEditor;

namespace Soobak.AssetInsights {
  public class AudioCompressionRule : IOptimizationRule {
    const long LargeAudioThreshold = 1024 * 1024; // 1 MB

    public string RuleName => "Audio Compression";
    public string Description => "Detects uncompressed or poorly optimized audio";
    public OptimizationSeverity Severity => OptimizationSeverity.Warning;

    public IEnumerable<OptimizationIssue> Evaluate(AssetNodeModel node, DependencyGraph graph) {
      if (node.Type != AssetType.Audio)
        yield break;

      var importer = AssetImporter.GetAtPath(node.Path) as AudioImporter;
      if (importer == null)
        yield break;

      var settings = importer.defaultSampleSettings;

      // Check for uncompressed audio
      if (settings.compressionFormat == AudioCompressionFormat.PCM) {
        yield return new OptimizationIssue {
          RuleName = RuleName,
          AssetPath = node.Path,
          AssetName = node.Name,
          Severity = OptimizationSeverity.Warning,
          Message = "Audio is uncompressed (PCM)",
          Recommendation = "Use Vorbis or ADPCM compression for most audio",
          PotentialSavings = node.SizeBytes * 3 / 4, // Vorbis typically 75% smaller
          IsAutoFixable = true
        };
      }

      // Check for large audio files that should be streaming
      if (node.SizeBytes > LargeAudioThreshold && settings.loadType != AudioClipLoadType.Streaming) {
        yield return new OptimizationIssue {
          RuleName = "Large Audio Not Streaming",
          AssetPath = node.Path,
          AssetName = node.Name,
          Severity = OptimizationSeverity.Warning,
          Message = $"Large audio file ({node.FormattedSize}) is not set to streaming",
          Recommendation = "Enable streaming for large audio files to reduce memory usage",
          PotentialSavings = 0, // Memory savings, not disk
          IsAutoFixable = true
        };
      }

      // Check for stereo audio that could be mono
      if (importer.forceToMono == false && node.Path.Contains("/SFX/")) {
        yield return new OptimizationIssue {
          RuleName = "Stereo SFX",
          AssetPath = node.Path,
          AssetName = node.Name,
          Severity = OptimizationSeverity.Info,
          Message = "Sound effect is stereo",
          Recommendation = "Consider using mono for sound effects to save 50% size",
          PotentialSavings = node.SizeBytes / 2,
          IsAutoFixable = true
        };
      }
    }
  }
}
