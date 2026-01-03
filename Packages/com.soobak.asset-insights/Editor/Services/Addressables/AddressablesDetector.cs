using System;
using System.Reflection;
using UnityEditor;

namespace Soobak.AssetInsights {
  /// <summary>
  /// Detects whether the Addressables package is installed and provides
  /// reflection-based access to Addressables types without hard dependencies.
  /// </summary>
  public static class AddressablesDetector {
    static bool? _isInstalled;
    static Type _settingsType;
    static Type _settingsDefaultObjectType;
    static Type _groupType;
    static Type _entryType;

    /// <summary>
    /// Returns true if the Addressables package is installed.
    /// </summary>
    public static bool IsInstalled {
      get {
        if (_isInstalled.HasValue)
          return _isInstalled.Value;

        _isInstalled = DetectAddressables();
        return _isInstalled.Value;
      }
    }

    /// <summary>
    /// Gets the AddressableAssetSettings type via reflection.
    /// </summary>
    public static Type SettingsType => _settingsType;

    /// <summary>
    /// Gets the AddressableAssetSettingsDefaultObject type via reflection.
    /// </summary>
    public static Type SettingsDefaultObjectType => _settingsDefaultObjectType;

    /// <summary>
    /// Gets the AddressableAssetGroup type via reflection.
    /// </summary>
    public static Type GroupType => _groupType;

    /// <summary>
    /// Gets the AddressableAssetEntry type via reflection.
    /// </summary>
    public static Type EntryType => _entryType;

    static bool DetectAddressables() {
      try {
        // Try to find the Addressables editor assembly
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        Assembly addressablesAssembly = null;

        foreach (var asm in assemblies) {
          if (asm.GetName().Name == "Unity.Addressables.Editor") {
            addressablesAssembly = asm;
            break;
          }
        }

        if (addressablesAssembly == null)
          return false;

        // Cache the types we need
        _settingsType = addressablesAssembly.GetType("UnityEditor.AddressableAssets.Settings.AddressableAssetSettings");
        _settingsDefaultObjectType = addressablesAssembly.GetType("UnityEditor.AddressableAssets.AddressableAssetSettingsDefaultObject");
        _groupType = addressablesAssembly.GetType("UnityEditor.AddressableAssets.Settings.AddressableAssetGroup");
        _entryType = addressablesAssembly.GetType("UnityEditor.AddressableAssets.Settings.AddressableAssetEntry");

        return _settingsType != null && _settingsDefaultObjectType != null;
      } catch {
        return false;
      }
    }

    /// <summary>
    /// Gets the current AddressableAssetSettings instance.
    /// Returns null if Addressables is not installed or not configured.
    /// </summary>
    public static object GetSettings() {
      if (!IsInstalled || _settingsDefaultObjectType == null)
        return null;

      try {
        var settingsProp = _settingsDefaultObjectType.GetProperty("Settings", BindingFlags.Public | BindingFlags.Static);
        return settingsProp?.GetValue(null);
      } catch {
        return null;
      }
    }

    /// <summary>
    /// Clears the cached detection result. Useful for editor scripts that
    /// need to re-detect after package installation changes.
    /// </summary>
    public static void ClearCache() {
      _isInstalled = null;
      _settingsType = null;
      _settingsDefaultObjectType = null;
      _groupType = null;
      _entryType = null;
    }
  }
}
