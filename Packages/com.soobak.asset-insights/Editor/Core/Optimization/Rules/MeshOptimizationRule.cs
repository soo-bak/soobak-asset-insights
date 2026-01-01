using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Soobak.AssetInsights {
  public class MeshOptimizationRule : IOptimizationRule {
    const int HighPolyThreshold = 100000;
    const int MediumPolyThreshold = 50000;
    const int MobilePolyThreshold = 10000;

    public string RuleName => "Mesh Optimization";
    public string Description => "Detects high polygon meshes and import settings issues";
    public OptimizationSeverity Severity => OptimizationSeverity.Warning;

    public IEnumerable<OptimizationIssue> Evaluate(AssetNodeModel node, DependencyGraph graph) {
      if (node.Type != AssetType.Model)
        yield break;

      var importer = AssetImporter.GetAtPath(node.Path) as ModelImporter;
      if (importer == null)
        yield break;

      // Load the model to check mesh data
      var gameObjects = AssetDatabase.LoadAllAssetsAtPath(node.Path);
      var totalVertices = 0;
      var totalTriangles = 0;
      var meshCount = 0;

      foreach (var obj in gameObjects) {
        if (obj is Mesh mesh) {
          totalVertices += mesh.vertexCount;
          totalTriangles += mesh.triangles.Length / 3;
          meshCount++;
        }
      }

      if (meshCount == 0)
        yield break;

      // Check for high poly count
      if (totalTriangles > HighPolyThreshold) {
        yield return new OptimizationIssue {
          RuleName = "High Polygon Mesh",
          AssetPath = node.Path,
          AssetName = node.Name,
          Severity = OptimizationSeverity.Error,
          Message = $"Mesh has {totalTriangles:N0} triangles ({totalVertices:N0} vertices)",
          Recommendation = $"Consider LOD or mesh decimation (target: <{HighPolyThreshold:N0} triangles)",
          PotentialSavings = EstimateMeshSavings(node.SizeBytes, totalTriangles, HighPolyThreshold),
          IsAutoFixable = false
        };
      } else if (totalTriangles > MediumPolyThreshold) {
        yield return new OptimizationIssue {
          RuleName = "Medium-High Polygon Mesh",
          AssetPath = node.Path,
          AssetName = node.Name,
          Severity = OptimizationSeverity.Warning,
          Message = $"Mesh has {totalTriangles:N0} triangles ({totalVertices:N0} vertices)",
          Recommendation = "Consider adding LOD levels for better performance",
          PotentialSavings = 0,
          IsAutoFixable = false
        };
      }

      // Check Read/Write enabled unnecessarily
      if (importer.isReadable) {
        yield return new OptimizationIssue {
          RuleName = "Read/Write Enabled",
          AssetPath = node.Path,
          AssetName = node.Name,
          Severity = OptimizationSeverity.Warning,
          Message = "Mesh has Read/Write enabled (doubles memory usage)",
          Recommendation = "Disable Read/Write if mesh is not modified at runtime",
          PotentialSavings = node.SizeBytes / 2,
          IsAutoFixable = true
        };
      }

      // Check for mesh compression
      if (importer.meshCompression == ModelImporterMeshCompression.Off) {
        yield return new OptimizationIssue {
          RuleName = "Uncompressed Mesh",
          AssetPath = node.Path,
          AssetName = node.Name,
          Severity = OptimizationSeverity.Info,
          Message = "Mesh compression is disabled",
          Recommendation = "Enable mesh compression to reduce file size",
          PotentialSavings = node.SizeBytes / 4,
          IsAutoFixable = true
        };
      }

      // Check for unnecessary normals/tangents
      if (importer.importNormals == ModelImporterNormals.Import ||
          importer.importNormals == ModelImporterNormals.Calculate) {
        // Check if it's a simple object that might not need tangents
        if (importer.importTangents == ModelImporterTangents.Import ||
            importer.importTangents == ModelImporterTangents.CalculateMikk) {
          var isUIOrSimple = node.Path.Contains("/UI/") ||
                             node.Path.Contains("/Simple/") ||
                             node.Path.Contains("/Icons/");
          if (isUIOrSimple) {
            yield return new OptimizationIssue {
              RuleName = "Unnecessary Tangents",
              AssetPath = node.Path,
              AssetName = node.Name,
              Severity = OptimizationSeverity.Info,
              Message = "Simple mesh has tangent import enabled",
              Recommendation = "Disable tangent import for simple UI meshes",
              PotentialSavings = node.SizeBytes / 8,
              IsAutoFixable = true
            };
          }
        }
      }

      // Check for animation import on static meshes
      if (importer.importAnimation) {
        var hasAnimations = false;
        foreach (var obj in gameObjects) {
          if (obj is AnimationClip) {
            hasAnimations = true;
            break;
          }
        }

        if (!hasAnimations) {
          yield return new OptimizationIssue {
            RuleName = "Unnecessary Animation Import",
            AssetPath = node.Path,
            AssetName = node.Name,
            Severity = OptimizationSeverity.Info,
            Message = "Animation import enabled but no animations found",
            Recommendation = "Disable animation import for static meshes",
            PotentialSavings = 0,
            IsAutoFixable = true
          };
        }
      }
    }

    long EstimateMeshSavings(long currentSize, int currentTris, int targetTris) {
      if (currentTris <= targetTris)
        return 0;

      var ratio = (float)targetTris / currentTris;
      return (long)(currentSize * (1f - ratio));
    }
  }
}
