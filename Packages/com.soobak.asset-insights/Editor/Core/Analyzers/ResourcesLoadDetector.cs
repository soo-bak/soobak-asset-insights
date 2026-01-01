using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;

namespace Soobak.AssetInsights {
  /// <summary>
  /// Detects Resources.Load() calls in scripts to identify dynamically loaded assets.
  /// </summary>
  public class ResourcesLoadDetector {
    readonly DependencyGraph _graph;

    // Patterns to match Resources.Load calls
    static readonly Regex[] LoadPatterns = {
      // Resources.Load("path")
      new Regex(@"Resources\.Load\s*(?:<[^>]+>)?\s*\(\s*""([^""]+)""", RegexOptions.Compiled),
      // Resources.Load<Type>("path")
      new Regex(@"Resources\.Load<[^>]+>\s*\(\s*""([^""]+)""", RegexOptions.Compiled),
      // Resources.LoadAll("path")
      new Regex(@"Resources\.LoadAll\s*(?:<[^>]+>)?\s*\(\s*""([^""]+)""", RegexOptions.Compiled),
      // Resources.LoadAsync("path")
      new Regex(@"Resources\.LoadAsync\s*(?:<[^>]+>)?\s*\(\s*""([^""]+)""", RegexOptions.Compiled),
    };

    // Pattern for variable-based loads (harder to resolve)
    static readonly Regex VariableLoadPattern = new Regex(
      @"Resources\.Load\s*(?:<[^>]+>)?\s*\(\s*(\w+)",
      RegexOptions.Compiled
    );

    public ResourcesLoadDetector(DependencyGraph graph) {
      _graph = graph;
    }

    public ResourcesLoadResult Detect() {
      var result = new ResourcesLoadResult();

      // Find all C# scripts
      var scripts = _graph.Nodes.Values
        .Where(n => n.Type == AssetType.Script && n.Path.EndsWith(".cs"))
        .ToList();

      foreach (var script in scripts) {
        try {
          var fullPath = Path.GetFullPath(script.Path);
          if (!File.Exists(fullPath))
            continue;

          var content = File.ReadAllText(fullPath);
          var references = FindResourcesReferences(content, script.Path);

          foreach (var reference in references) {
            result.References.Add(reference);

            // Try to resolve the resource path to an actual asset
            var resolvedPath = ResolveResourcePath(reference.ResourcePath);
            if (resolvedPath != null) {
              reference.ResolvedAssetPath = resolvedPath;
              result.ResolvedAssets.Add(resolvedPath);
            } else {
              result.UnresolvedPaths.Add(reference.ResourcePath);
            }
          }
        } catch {
          // Ignore read errors
        }
      }

      result.TotalReferences = result.References.Count;
      result.TotalResolved = result.ResolvedAssets.Count;
      result.TotalUnresolved = result.UnresolvedPaths.Count;

      return result;
    }

    List<ResourcesLoadReference> FindResourcesReferences(string content, string scriptPath) {
      var references = new List<ResourcesLoadReference>();
      var lines = content.Split('\n');

      for (int lineNum = 0; lineNum < lines.Length; lineNum++) {
        var line = lines[lineNum];

        // Skip comments
        if (line.TrimStart().StartsWith("//"))
          continue;

        foreach (var pattern in LoadPatterns) {
          var matches = pattern.Matches(line);
          foreach (Match match in matches) {
            if (match.Groups.Count > 1) {
              references.Add(new ResourcesLoadReference {
                ScriptPath = scriptPath,
                LineNumber = lineNum + 1,
                ResourcePath = match.Groups[1].Value,
                FullLine = line.Trim(),
                IsDirectPath = true
              });
            }
          }
        }

        // Check for variable-based loads
        if (line.Contains("Resources.Load") && !references.Any(r => r.LineNumber == lineNum + 1)) {
          var varMatch = VariableLoadPattern.Match(line);
          if (varMatch.Success && !varMatch.Groups[1].Value.StartsWith("\"")) {
            references.Add(new ResourcesLoadReference {
              ScriptPath = scriptPath,
              LineNumber = lineNum + 1,
              ResourcePath = $"<variable: {varMatch.Groups[1].Value}>",
              FullLine = line.Trim(),
              IsDirectPath = false
            });
          }
        }
      }

      return references;
    }

    string ResolveResourcePath(string resourcePath) {
      if (string.IsNullOrEmpty(resourcePath) || resourcePath.StartsWith("<"))
        return null;

      // Try common extensions
      var extensions = new[] { "", ".prefab", ".png", ".jpg", ".mat", ".asset", ".txt", ".json" };
      var resourcesFolders = new[] { "Assets/Resources/", "Assets/*/Resources/" };

      foreach (var ext in extensions) {
        var testPath = $"Assets/Resources/{resourcePath}{ext}";
        if (_graph.ContainsNode(testPath))
          return testPath;

        // Check with file search
        var found = AssetDatabase.FindAssets(Path.GetFileNameWithoutExtension(resourcePath))
          .Select(AssetDatabase.GUIDToAssetPath)
          .FirstOrDefault(p => p.Contains("/Resources/") && p.Contains(resourcePath));

        if (found != null)
          return found;
      }

      return null;
    }

    public bool IsLoadedViaResources(string assetPath) {
      var result = Detect();
      return result.ResolvedAssets.Contains(assetPath);
    }
  }

  public class ResourcesLoadResult {
    public List<ResourcesLoadReference> References { get; set; } = new();
    public HashSet<string> ResolvedAssets { get; set; } = new();
    public HashSet<string> UnresolvedPaths { get; set; } = new();
    public int TotalReferences { get; set; }
    public int TotalResolved { get; set; }
    public int TotalUnresolved { get; set; }
  }

  public class ResourcesLoadReference {
    public string ScriptPath { get; set; }
    public int LineNumber { get; set; }
    public string ResourcePath { get; set; }
    public string ResolvedAssetPath { get; set; }
    public string FullLine { get; set; }
    public bool IsDirectPath { get; set; }

    public string ScriptName => Path.GetFileNameWithoutExtension(ScriptPath);
    public bool IsResolved => ResolvedAssetPath != null;
  }
}
