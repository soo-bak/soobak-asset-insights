using System.Collections;

namespace Soobak.AssetInsights {
  public interface IDependencyScanner {
    DependencyGraph Graph { get; }
    IScanProgress Progress { get; }

    IEnumerator ScanAsync(ScanOptions options = null);
    void ScanImmediate(ScanOptions options = null);
    void Clear();
  }
}
