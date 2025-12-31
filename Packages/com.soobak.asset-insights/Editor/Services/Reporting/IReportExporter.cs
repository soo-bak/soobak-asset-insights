namespace Soobak.AssetInsights {
  public interface IReportExporter {
    string Export(DependencyGraph graph, ReportOptions options = null);
    string ExportWhyIncluded(DependencyGraph graph, string targetAsset, string rootAsset = null);
    string ExportHeavyHitters(DependencyGraph graph, int count = 20);
  }
}
