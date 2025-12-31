using System;
using System.IO;

namespace Soobak.AssetInsights {
  public sealed class AssetNodeModel : IEquatable<AssetNodeModel>, IComparable<AssetNodeModel> {
    public string Path { get; }
    public string Name { get; }
    public string Extension { get; }
    public AssetType Type { get; }
    public long SizeBytes { get; }

    public AssetNodeModel(string path, long sizeBytes) {
      if (string.IsNullOrEmpty(path))
        throw new ArgumentNullException(nameof(path));

      Path = path;
      Name = System.IO.Path.GetFileNameWithoutExtension(path);
      Extension = System.IO.Path.GetExtension(path).ToLowerInvariant();
      Type = ResolveAssetType(Extension);
      SizeBytes = sizeBytes;
    }

    public string FormattedSize => FormatBytes(SizeBytes);

    public static AssetType ResolveAssetType(string extension) {
      return extension switch {
        ".png" or ".jpg" or ".jpeg" or ".tga" or ".psd" or ".tiff" or ".gif" or ".bmp" or ".exr" or ".hdr"
          => AssetType.Texture,
        ".wav" or ".mp3" or ".ogg" or ".aiff" or ".aif" or ".flac"
          => AssetType.Audio,
        ".fbx" or ".obj" or ".dae" or ".3ds" or ".blend"
          => AssetType.Model,
        ".mat"
          => AssetType.Material,
        ".shader" or ".shadergraph" or ".shadersubgraph" or ".hlsl" or ".cginc"
          => AssetType.Shader,
        ".prefab"
          => AssetType.Prefab,
        ".unity"
          => AssetType.Scene,
        ".cs"
          => AssetType.Script,
        ".anim"
          => AssetType.Animation,
        ".controller" or ".overrideController"
          => AssetType.AnimatorController,
        ".asset"
          => AssetType.ScriptableObject,
        ".fontsettings" or ".ttf" or ".otf"
          => AssetType.Font,
        ".mp4" or ".mov" or ".avi" or ".webm"
          => AssetType.Video,
        ".txt" or ".json" or ".xml" or ".yaml" or ".csv"
          => AssetType.TextData,
        _ => AssetType.Other
      };
    }

    public static string FormatBytes(long bytes) {
      if (bytes < 0)
        return "0 B";

      string[] units = { "B", "KB", "MB", "GB" };
      var index = 0;
      var size = (double)bytes;

      while (size >= 1024 && index < units.Length - 1) {
        index++;
        size /= 1024;
      }

      return $"{size:0.##} {units[index]}";
    }

    public bool Equals(AssetNodeModel other) {
      if (other is null)
        return false;
      return Path == other.Path;
    }

    public override bool Equals(object obj) => Equals(obj as AssetNodeModel);

    public override int GetHashCode() => Path.GetHashCode();

    public int CompareTo(AssetNodeModel other) {
      if (other is null)
        return 1;
      return other.SizeBytes.CompareTo(SizeBytes);
    }

    public override string ToString() => $"{Name} ({FormattedSize})";

    public static bool operator ==(AssetNodeModel left, AssetNodeModel right) {
      if (left is null)
        return right is null;
      return left.Equals(right);
    }

    public static bool operator !=(AssetNodeModel left, AssetNodeModel right) => !(left == right);
  }
}
