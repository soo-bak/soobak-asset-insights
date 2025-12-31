namespace Soobak.AssetInsights {
  public class ScanProgress : IScanProgress {
    public float Progress { get; private set; }
    public string CurrentAsset { get; private set; }
    public int ProcessedCount { get; private set; }
    public int TotalCount { get; private set; }
    public bool IsCancelled { get; private set; }

    public void Report(float progress, string currentAsset = null) {
      Progress = progress;
      if (currentAsset != null)
        CurrentAsset = currentAsset;
    }

    public void SetTotal(int total) {
      TotalCount = total;
    }

    public void Increment() {
      ProcessedCount++;
      if (TotalCount > 0)
        Progress = (float)ProcessedCount / TotalCount;
    }

    public void Cancel() {
      IsCancelled = true;
    }

    public void Reset() {
      Progress = 0f;
      CurrentAsset = null;
      ProcessedCount = 0;
      TotalCount = 0;
      IsCancelled = false;
    }
  }
}
