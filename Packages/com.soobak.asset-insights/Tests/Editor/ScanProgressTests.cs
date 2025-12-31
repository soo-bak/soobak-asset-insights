using NUnit.Framework;

namespace Soobak.AssetInsights.Tests {
  public class ScanProgressTests {
    [Test]
    public void Initial_ValuesAreZero() {
      var progress = new ScanProgress();

      Assert.AreEqual(0f, progress.Progress);
      Assert.AreEqual(0, progress.ProcessedCount);
      Assert.AreEqual(0, progress.TotalCount);
      Assert.IsNull(progress.CurrentAsset);
      Assert.IsFalse(progress.IsCancelled);
    }

    [Test]
    public void SetTotal_SetsCorrectly() {
      var progress = new ScanProgress();

      progress.SetTotal(100);

      Assert.AreEqual(100, progress.TotalCount);
    }

    [Test]
    public void Increment_UpdatesProgressCorrectly() {
      var progress = new ScanProgress();
      progress.SetTotal(4);

      progress.Increment();
      Assert.AreEqual(1, progress.ProcessedCount);
      Assert.AreEqual(0.25f, progress.Progress, 0.001f);

      progress.Increment();
      Assert.AreEqual(2, progress.ProcessedCount);
      Assert.AreEqual(0.5f, progress.Progress, 0.001f);
    }

    [Test]
    public void Report_UpdatesProgressAndAsset() {
      var progress = new ScanProgress();

      progress.Report(0.5f, "Assets/Test.png");

      Assert.AreEqual(0.5f, progress.Progress);
      Assert.AreEqual("Assets/Test.png", progress.CurrentAsset);
    }

    [Test]
    public void Report_WithoutAsset_PreservesCurrentAsset() {
      var progress = new ScanProgress();
      progress.Report(0.3f, "Assets/First.png");

      progress.Report(0.6f);

      Assert.AreEqual(0.6f, progress.Progress);
      Assert.AreEqual("Assets/First.png", progress.CurrentAsset);
    }

    [Test]
    public void Cancel_SetsCancelledFlag() {
      var progress = new ScanProgress();

      progress.Cancel();

      Assert.IsTrue(progress.IsCancelled);
    }

    [Test]
    public void Reset_ClearsAllValues() {
      var progress = new ScanProgress();
      progress.SetTotal(100);
      progress.Increment();
      progress.Report(0.5f, "Assets/Test.png");
      progress.Cancel();

      progress.Reset();

      Assert.AreEqual(0f, progress.Progress);
      Assert.AreEqual(0, progress.ProcessedCount);
      Assert.AreEqual(0, progress.TotalCount);
      Assert.IsNull(progress.CurrentAsset);
      Assert.IsFalse(progress.IsCancelled);
    }
  }
}
