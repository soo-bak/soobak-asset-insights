namespace Soobak.AssetInsights {
  public interface IScanProgress {
    float Progress { get; }
    string CurrentAsset { get; }
    int ProcessedCount { get; }
    int TotalCount { get; }
    bool IsCancelled { get; }

    void Report(float progress, string currentAsset = null);
    void SetTotal(int total);
    void Increment();
    void Cancel();
  }
}
