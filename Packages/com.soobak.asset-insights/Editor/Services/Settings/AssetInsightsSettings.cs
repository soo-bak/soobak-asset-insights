using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace Soobak.AssetInsights {
  [FilePath("ProjectSettings/AssetInsightsSettings.asset", FilePathAttribute.Location.ProjectFolder)]
  public class AssetInsightsSettings : ScriptableSingleton<AssetInsightsSettings> {
    [SerializeField] List<string> _ignoredPaths = new() {
      "Assets/Plugins/",
      "Assets/ThirdParty/",
      "Assets/Editor Default Resources/"
    };

    [SerializeField] List<string> _ignoredPatterns = new() {
      "*_backup.*",
      "*.tmp",
      "*.bak"
    };

    [SerializeField] List<string> _whitelistedPaths = new();

    [SerializeField] bool _ignoreEditorAssets = true;
    [SerializeField] bool _ignoreTestAssets = true;
    [SerializeField] bool _ignoreStreamingAssets = false;

    public IReadOnlyList<string> IgnoredPaths => _ignoredPaths;
    public IReadOnlyList<string> IgnoredPatterns => _ignoredPatterns;
    public IReadOnlyList<string> WhitelistedPaths => _whitelistedPaths;
    public bool IgnoreEditorAssets => _ignoreEditorAssets;
    public bool IgnoreTestAssets => _ignoreTestAssets;
    public bool IgnoreStreamingAssets => _ignoreStreamingAssets;

    List<Regex> _compiledPatterns;

    public void AddIgnoredPath(string path) {
      if (!_ignoredPaths.Contains(path)) {
        _ignoredPaths.Add(path);
        Save();
      }
    }

    public void RemoveIgnoredPath(string path) {
      if (_ignoredPaths.Remove(path)) {
        Save();
      }
    }

    public void AddIgnoredPattern(string pattern) {
      if (!_ignoredPatterns.Contains(pattern)) {
        _ignoredPatterns.Add(pattern);
        _compiledPatterns = null;
        Save();
      }
    }

    public void RemoveIgnoredPattern(string pattern) {
      if (_ignoredPatterns.Remove(pattern)) {
        _compiledPatterns = null;
        Save();
      }
    }

    public void AddWhitelistedPath(string path) {
      if (!_whitelistedPaths.Contains(path)) {
        _whitelistedPaths.Add(path);
        Save();
      }
    }

    public void RemoveWhitelistedPath(string path) {
      if (_whitelistedPaths.Remove(path)) {
        Save();
      }
    }

    public void SetIgnoreEditorAssets(bool value) {
      _ignoreEditorAssets = value;
      Save();
    }

    public void SetIgnoreTestAssets(bool value) {
      _ignoreTestAssets = value;
      Save();
    }

    public void SetIgnoreStreamingAssets(bool value) {
      _ignoreStreamingAssets = value;
      Save();
    }

    public bool ShouldIgnore(string assetPath) {
      // Whitelist takes priority
      foreach (var whitePath in _whitelistedPaths) {
        if (assetPath.StartsWith(whitePath, StringComparison.OrdinalIgnoreCase))
          return false;
      }

      // Check ignored paths
      foreach (var ignorePath in _ignoredPaths) {
        if (assetPath.StartsWith(ignorePath, StringComparison.OrdinalIgnoreCase))
          return true;
      }

      // Check Editor folders
      if (_ignoreEditorAssets && IsEditorAsset(assetPath))
        return true;

      // Check Test folders
      if (_ignoreTestAssets && IsTestAsset(assetPath))
        return true;

      // Check StreamingAssets
      if (_ignoreStreamingAssets && assetPath.StartsWith("Assets/StreamingAssets/"))
        return true;

      // Check patterns
      if (MatchesIgnoredPattern(assetPath))
        return true;

      return false;
    }

    bool IsEditorAsset(string path) {
      return path.Contains("/Editor/") ||
             path.EndsWith("/Editor") ||
             path.StartsWith("Assets/Editor/");
    }

    bool IsTestAsset(string path) {
      return path.Contains("/Tests/") ||
             path.Contains("/Test/") ||
             path.EndsWith("/Tests") ||
             path.EndsWith("/Test");
    }

    bool MatchesIgnoredPattern(string assetPath) {
      if (_ignoredPatterns.Count == 0)
        return false;

      CompilePatternsIfNeeded();

      var fileName = Path.GetFileName(assetPath);
      foreach (var regex in _compiledPatterns) {
        if (regex.IsMatch(fileName))
          return true;
      }

      return false;
    }

    void CompilePatternsIfNeeded() {
      if (_compiledPatterns != null)
        return;

      _compiledPatterns = new List<Regex>();
      foreach (var pattern in _ignoredPatterns) {
        try {
          var regexPattern = "^" + Regex.Escape(pattern)
            .Replace("\\*", ".*")
            .Replace("\\?", ".") + "$";
          _compiledPatterns.Add(new Regex(regexPattern, RegexOptions.IgnoreCase));
        } catch (Exception) {
          // Invalid pattern, skip
        }
      }
    }

    public void Save() {
      Save(true);
    }

    public void ResetToDefaults() {
      _ignoredPaths = new List<string> {
        "Assets/Plugins/",
        "Assets/ThirdParty/",
        "Assets/Editor Default Resources/"
      };
      _ignoredPatterns = new List<string> {
        "*_backup.*",
        "*.tmp",
        "*.bak"
      };
      _whitelistedPaths = new List<string>();
      _ignoreEditorAssets = true;
      _ignoreTestAssets = true;
      _ignoreStreamingAssets = false;
      _compiledPatterns = null;
      Save();
    }
  }
}
