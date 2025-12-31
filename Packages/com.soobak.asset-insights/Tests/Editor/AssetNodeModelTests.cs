using NUnit.Framework;

namespace Soobak.AssetInsights.Tests {
  [TestFixture]
  public class AssetNodeModelTests {
    [Test]
    public void Constructor_WithValidPath_SetsProperties() {
      var node = new AssetNodeModel("Assets/Textures/player.png", 1024);

      Assert.AreEqual("Assets/Textures/player.png", node.Path);
      Assert.AreEqual("player", node.Name);
      Assert.AreEqual(".png", node.Extension);
      Assert.AreEqual(AssetType.Texture, node.Type);
      Assert.AreEqual(1024, node.SizeBytes);
    }

    [Test]
    public void Constructor_WithNullPath_ThrowsArgumentNullException() {
      Assert.Throws<System.ArgumentNullException>(() => new AssetNodeModel(null, 0));
    }

    [Test]
    public void Constructor_WithEmptyPath_ThrowsArgumentNullException() {
      Assert.Throws<System.ArgumentNullException>(() => new AssetNodeModel("", 0));
    }

    [TestCase(".png", AssetType.Texture)]
    [TestCase(".jpg", AssetType.Texture)]
    [TestCase(".wav", AssetType.Audio)]
    [TestCase(".mp3", AssetType.Audio)]
    [TestCase(".fbx", AssetType.Model)]
    [TestCase(".mat", AssetType.Material)]
    [TestCase(".shader", AssetType.Shader)]
    [TestCase(".prefab", AssetType.Prefab)]
    [TestCase(".unity", AssetType.Scene)]
    [TestCase(".cs", AssetType.Script)]
    [TestCase(".anim", AssetType.Animation)]
    [TestCase(".asset", AssetType.ScriptableObject)]
    [TestCase(".unknown", AssetType.Other)]
    public void ResolveAssetType_ReturnsCorrectType(string extension, AssetType expected) {
      var result = AssetNodeModel.ResolveAssetType(extension);
      Assert.AreEqual(expected, result);
    }

    [TestCase(0, "0 B")]
    [TestCase(512, "512 B")]
    [TestCase(1024, "1 KB")]
    [TestCase(1536, "1.5 KB")]
    [TestCase(1048576, "1 MB")]
    [TestCase(1073741824, "1 GB")]
    public void FormatBytes_ReturnsCorrectFormat(long bytes, string expected) {
      var result = AssetNodeModel.FormatBytes(bytes);
      Assert.AreEqual(expected, result);
    }

    [Test]
    public void FormatBytes_WithNegative_ReturnsZero() {
      var result = AssetNodeModel.FormatBytes(-100);
      Assert.AreEqual("0 B", result);
    }

    [Test]
    public void Equals_WithSamePath_ReturnsTrue() {
      var node1 = new AssetNodeModel("Assets/test.png", 100);
      var node2 = new AssetNodeModel("Assets/test.png", 200);

      Assert.IsTrue(node1.Equals(node2));
      Assert.IsTrue(node1 == node2);
    }

    [Test]
    public void Equals_WithDifferentPath_ReturnsFalse() {
      var node1 = new AssetNodeModel("Assets/test1.png", 100);
      var node2 = new AssetNodeModel("Assets/test2.png", 100);

      Assert.IsFalse(node1.Equals(node2));
      Assert.IsTrue(node1 != node2);
    }

    [Test]
    public void CompareTo_SortsByDescendingSize() {
      var small = new AssetNodeModel("Assets/small.png", 100);
      var large = new AssetNodeModel("Assets/large.png", 1000);

      Assert.Less(large.CompareTo(small), 0);
      Assert.Greater(small.CompareTo(large), 0);
    }

    [Test]
    public void ToString_ReturnsNameAndSize() {
      var node = new AssetNodeModel("Assets/test.png", 1024);
      Assert.AreEqual("test (1 KB)", node.ToString());
    }
  }
}
