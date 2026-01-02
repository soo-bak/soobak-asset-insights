using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Soobak.AssetInsights {
  /// <summary>
  /// Service for applying automatic fixes to assets based on optimization issues.
  /// </summary>
  public static class AssetFixer {
    /// <summary>
    /// Result of a fix operation.
    /// </summary>
    public class FixResult {
      public bool Success { get; set; }
      public string Message { get; set; }
      public string AssetPath { get; set; }
      public FixType FixType { get; set; }
    }

    /// <summary>
    /// Applies a fix to a single optimization issue.
    /// </summary>
    public static FixResult ApplyFix(OptimizationIssue issue) {
      if (issue == null || !issue.IsAutoFixable || issue.FixType == FixType.None) {
        return new FixResult {
          Success = false,
          Message = "Issue is not auto-fixable",
          AssetPath = issue?.AssetPath ?? "",
          FixType = issue?.FixType ?? FixType.None
        };
      }

      try {
        return issue.FixType switch {
          // Texture fixes
          FixType.TextureEnableCompression => ApplyTextureCompression(issue.AssetPath),
          FixType.TextureDisableMipmaps => DisableTextureMipmaps(issue.AssetPath),

          // Audio fixes
          FixType.AudioEnableCompression => EnableAudioCompression(issue.AssetPath),
          FixType.AudioEnableStreaming => EnableAudioStreaming(issue.AssetPath),
          FixType.AudioForceToMono => ForceAudioToMono(issue.AssetPath),

          // Material fixes
          FixType.MaterialEnableGPUInstancing => EnableGPUInstancing(issue.AssetPath),

          // Mesh fixes
          FixType.MeshDisableReadWrite => DisableMeshReadWrite(issue.AssetPath),
          FixType.MeshEnableCompression => EnableMeshCompression(issue.AssetPath),
          FixType.MeshDisableAnimation => DisableMeshAnimation(issue.AssetPath),
          FixType.MeshDisableTangents => DisableMeshTangents(issue.AssetPath),

          _ => new FixResult {
            Success = false,
            Message = $"Unknown fix type: {issue.FixType}",
            AssetPath = issue.AssetPath,
            FixType = issue.FixType
          }
        };
      } catch (Exception ex) {
        return new FixResult {
          Success = false,
          Message = $"Error applying fix: {ex.Message}",
          AssetPath = issue.AssetPath,
          FixType = issue.FixType
        };
      }
    }

    /// <summary>
    /// Applies fixes to multiple issues.
    /// </summary>
    public static List<FixResult> ApplyFixes(IEnumerable<OptimizationIssue> issues) {
      var results = new List<FixResult>();

      foreach (var issue in issues) {
        results.Add(ApplyFix(issue));
      }

      // Refresh asset database once after all fixes
      AssetDatabase.Refresh();

      return results;
    }

    #region Texture Fixes

    static FixResult ApplyTextureCompression(string assetPath) {
      var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
      if (importer == null) {
        return new FixResult {
          Success = false,
          Message = "Failed to get TextureImporter",
          AssetPath = assetPath,
          FixType = FixType.TextureEnableCompression
        };
      }

      importer.textureCompression = TextureImporterCompression.Compressed;
      importer.SaveAndReimport();

      return new FixResult {
        Success = true,
        Message = "Enabled texture compression",
        AssetPath = assetPath,
        FixType = FixType.TextureEnableCompression
      };
    }

    static FixResult DisableTextureMipmaps(string assetPath) {
      var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
      if (importer == null) {
        return new FixResult {
          Success = false,
          Message = "Failed to get TextureImporter",
          AssetPath = assetPath,
          FixType = FixType.TextureDisableMipmaps
        };
      }

      importer.mipmapEnabled = false;
      importer.SaveAndReimport();

      return new FixResult {
        Success = true,
        Message = "Disabled mipmaps",
        AssetPath = assetPath,
        FixType = FixType.TextureDisableMipmaps
      };
    }

    #endregion

    #region Audio Fixes

    static FixResult EnableAudioCompression(string assetPath) {
      var importer = AssetImporter.GetAtPath(assetPath) as AudioImporter;
      if (importer == null) {
        return new FixResult {
          Success = false,
          Message = "Failed to get AudioImporter",
          AssetPath = assetPath,
          FixType = FixType.AudioEnableCompression
        };
      }

      var settings = importer.defaultSampleSettings;
      settings.compressionFormat = AudioCompressionFormat.Vorbis;
      settings.quality = 0.7f; // Good balance between quality and size
      importer.defaultSampleSettings = settings;
      importer.SaveAndReimport();

      return new FixResult {
        Success = true,
        Message = "Enabled Vorbis compression",
        AssetPath = assetPath,
        FixType = FixType.AudioEnableCompression
      };
    }

    static FixResult EnableAudioStreaming(string assetPath) {
      var importer = AssetImporter.GetAtPath(assetPath) as AudioImporter;
      if (importer == null) {
        return new FixResult {
          Success = false,
          Message = "Failed to get AudioImporter",
          AssetPath = assetPath,
          FixType = FixType.AudioEnableStreaming
        };
      }

      var settings = importer.defaultSampleSettings;
      settings.loadType = AudioClipLoadType.Streaming;
      importer.defaultSampleSettings = settings;
      importer.SaveAndReimport();

      return new FixResult {
        Success = true,
        Message = "Enabled audio streaming",
        AssetPath = assetPath,
        FixType = FixType.AudioEnableStreaming
      };
    }

    static FixResult ForceAudioToMono(string assetPath) {
      var importer = AssetImporter.GetAtPath(assetPath) as AudioImporter;
      if (importer == null) {
        return new FixResult {
          Success = false,
          Message = "Failed to get AudioImporter",
          AssetPath = assetPath,
          FixType = FixType.AudioForceToMono
        };
      }

      importer.forceToMono = true;
      importer.SaveAndReimport();

      return new FixResult {
        Success = true,
        Message = "Forced audio to mono",
        AssetPath = assetPath,
        FixType = FixType.AudioForceToMono
      };
    }

    #endregion

    #region Material Fixes

    static FixResult EnableGPUInstancing(string assetPath) {
      var material = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
      if (material == null) {
        return new FixResult {
          Success = false,
          Message = "Failed to load Material",
          AssetPath = assetPath,
          FixType = FixType.MaterialEnableGPUInstancing
        };
      }

      try {
        material.enableInstancing = true;
        EditorUtility.SetDirty(material);
        AssetDatabase.SaveAssetIfDirty(material);

        return new FixResult {
          Success = true,
          Message = "Enabled GPU Instancing",
          AssetPath = assetPath,
          FixType = FixType.MaterialEnableGPUInstancing
        };
      } finally {
        Resources.UnloadAsset(material);
      }
    }

    #endregion

    #region Mesh Fixes

    static FixResult DisableMeshReadWrite(string assetPath) {
      var importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
      if (importer == null) {
        return new FixResult {
          Success = false,
          Message = "Failed to get ModelImporter",
          AssetPath = assetPath,
          FixType = FixType.MeshDisableReadWrite
        };
      }

      importer.isReadable = false;
      importer.SaveAndReimport();

      return new FixResult {
        Success = true,
        Message = "Disabled Read/Write",
        AssetPath = assetPath,
        FixType = FixType.MeshDisableReadWrite
      };
    }

    static FixResult EnableMeshCompression(string assetPath) {
      var importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
      if (importer == null) {
        return new FixResult {
          Success = false,
          Message = "Failed to get ModelImporter",
          AssetPath = assetPath,
          FixType = FixType.MeshEnableCompression
        };
      }

      importer.meshCompression = ModelImporterMeshCompression.Medium;
      importer.SaveAndReimport();

      return new FixResult {
        Success = true,
        Message = "Enabled mesh compression",
        AssetPath = assetPath,
        FixType = FixType.MeshEnableCompression
      };
    }

    static FixResult DisableMeshAnimation(string assetPath) {
      var importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
      if (importer == null) {
        return new FixResult {
          Success = false,
          Message = "Failed to get ModelImporter",
          AssetPath = assetPath,
          FixType = FixType.MeshDisableAnimation
        };
      }

      importer.importAnimation = false;
      importer.SaveAndReimport();

      return new FixResult {
        Success = true,
        Message = "Disabled animation import",
        AssetPath = assetPath,
        FixType = FixType.MeshDisableAnimation
      };
    }

    static FixResult DisableMeshTangents(string assetPath) {
      var importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
      if (importer == null) {
        return new FixResult {
          Success = false,
          Message = "Failed to get ModelImporter",
          AssetPath = assetPath,
          FixType = FixType.MeshDisableTangents
        };
      }

      importer.importTangents = ModelImporterTangents.None;
      importer.SaveAndReimport();

      return new FixResult {
        Success = true,
        Message = "Disabled tangent import",
        AssetPath = assetPath,
        FixType = FixType.MeshDisableTangents
      };
    }

    #endregion

    /// <summary>
    /// Gets a human-readable description of a fix type.
    /// </summary>
    public static string GetFixDescription(FixType fixType) {
      return fixType switch {
        FixType.TextureEnableCompression => "Enable Compression",
        FixType.TextureDisableMipmaps => "Disable Mipmaps",
        FixType.AudioEnableCompression => "Enable Compression",
        FixType.AudioEnableStreaming => "Enable Streaming",
        FixType.AudioForceToMono => "Force to Mono",
        FixType.MaterialEnableGPUInstancing => "Enable GPU Instancing",
        FixType.MeshDisableReadWrite => "Disable Read/Write",
        FixType.MeshEnableCompression => "Enable Compression",
        FixType.MeshDisableAnimation => "Disable Animation",
        FixType.MeshDisableTangents => "Disable Tangents",
        _ => "Unknown Fix"
      };
    }
  }
}
