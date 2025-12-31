using NUnit.Framework;

namespace Soobak.AssetInsights.Tests {
  public class ScanOptionsTests {
    [Test]
    public void Default_HasExpectedValues() {
      var options = ScanOptions.Default;

      Assert.AreEqual(0, options.IncludeTypes.Count);
      Assert.AreEqual(0, options.ExcludeTypes.Count);
      Assert.AreEqual(0, options.MinFileSize);
      Assert.IsFalse(options.IncludePackages);
    }

    [Test]
    public void ShouldInclude_WithNoFilters_ReturnsTrue() {
      var options = new ScanOptions();
      var node = new AssetNodeModel("Assets/Test.png", 1000);

      Assert.IsTrue(options.ShouldInclude(node));
    }

    [Test]
    public void ShouldInclude_ExcludesPackages_ByDefault() {
      var options = new ScanOptions();
      var node = new AssetNodeModel("Packages/com.unity.test/Test.cs", 1000);

      Assert.IsFalse(options.ShouldInclude(node));
    }

    [Test]
    public void ShouldInclude_IncludesPackages_WhenEnabled() {
      var options = new ScanOptions { IncludePackages = true };
      var node = new AssetNodeModel("Packages/com.unity.test/Test.cs", 1000);

      Assert.IsTrue(options.ShouldInclude(node));
    }

    [Test]
    public void ShouldInclude_RespectsExcludeTypes() {
      var options = new ScanOptions();
      options.ExcludeTypes.Add(AssetType.Texture);

      var texture = new AssetNodeModel("Assets/Test.png", 1000);
      var script = new AssetNodeModel("Assets/Test.cs", 1000);

      Assert.IsFalse(options.ShouldInclude(texture));
      Assert.IsTrue(options.ShouldInclude(script));
    }

    [Test]
    public void ShouldInclude_RespectsIncludeTypes() {
      var options = new ScanOptions();
      options.IncludeTypes.Add(AssetType.Texture);

      var texture = new AssetNodeModel("Assets/Test.png", 1000);
      var script = new AssetNodeModel("Assets/Test.cs", 1000);

      Assert.IsTrue(options.ShouldInclude(texture));
      Assert.IsFalse(options.ShouldInclude(script));
    }

    [Test]
    public void ShouldInclude_RespectsMinFileSize() {
      var options = new ScanOptions { MinFileSize = 5000 };

      var small = new AssetNodeModel("Assets/Small.png", 1000);
      var large = new AssetNodeModel("Assets/Large.png", 10000);

      Assert.IsFalse(options.ShouldInclude(small));
      Assert.IsTrue(options.ShouldInclude(large));
    }

    [Test]
    public void ShouldInclude_RespectsExcludePaths() {
      var options = new ScanOptions();
      options.ExcludePaths.Add("Assets/Plugins/");

      var plugin = new AssetNodeModel("Assets/Plugins/External.dll", 1000);
      var normal = new AssetNodeModel("Assets/Scripts/Test.cs", 1000);

      Assert.IsFalse(options.ShouldInclude(plugin));
      Assert.IsTrue(options.ShouldInclude(normal));
    }

    [Test]
    public void ShouldInclude_RespectsIncludePaths() {
      var options = new ScanOptions();
      options.IncludePaths.Add("Assets/Art/");

      var art = new AssetNodeModel("Assets/Art/Texture.png", 1000);
      var script = new AssetNodeModel("Assets/Scripts/Test.cs", 1000);

      Assert.IsTrue(options.ShouldInclude(art));
      Assert.IsFalse(options.ShouldInclude(script));
    }
  }
}
