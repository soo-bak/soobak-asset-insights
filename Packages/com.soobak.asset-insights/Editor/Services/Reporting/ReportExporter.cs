using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Soobak.AssetInsights {
  public class ReportExporter : IReportExporter {
    public string Export(DependencyGraph graph, ReportOptions options = null) {
      options = options ?? ReportOptions.Default;

      switch (options.Format) {
        case ReportFormat.Mermaid:
          return ExportMermaid(graph, options);
        case ReportFormat.Json:
          return ExportJson(graph, options);
        default:
          return ExportMarkdown(graph, options);
      }
    }

    public string ExportWhyIncluded(DependencyGraph graph, string targetAsset, string rootAsset = null) {
      var sb = new StringBuilder();
      sb.AppendLine("# Why Included Report");
      sb.AppendLine();
      sb.AppendLine($"**Target Asset:** `{targetAsset}`");
      sb.AppendLine();

      var paths = PathFinder.FindWhyIncluded(graph, targetAsset, rootAsset);

      if (paths.Count == 0) {
        sb.AppendLine("No dependency paths found.");
        return sb.ToString();
      }

      sb.AppendLine($"Found **{paths.Count}** dependency path(s):");
      sb.AppendLine();

      int pathIndex = 1;
      foreach (var path in paths) {
        sb.AppendLine($"### Path {pathIndex++}");
        sb.AppendLine("```");
        for (int i = 0; i < path.Count; i++) {
          var indent = new string(' ', i * 2);
          var arrow = i < path.Count - 1 ? " →" : " (target)";
          sb.AppendLine($"{indent}{path[i]}{arrow}");
        }
        sb.AppendLine("```");
        sb.AppendLine();
      }

      return sb.ToString();
    }

    public string ExportHeavyHitters(DependencyGraph graph, int count = 20) {
      var sb = new StringBuilder();
      sb.AppendLine("# Heavy Hitters Report");
      sb.AppendLine();

      var heavyHitters = graph.GetHeavyHitters(count);

      if (heavyHitters.Count == 0) {
        sb.AppendLine("No assets found.");
        return sb.ToString();
      }

      long totalSize = heavyHitters.Sum(n => n.FileSize);

      sb.AppendLine($"Top **{heavyHitters.Count}** largest assets (Total: {FormatSize(totalSize)}):");
      sb.AppendLine();
      sb.AppendLine("| # | Size | Type | Asset Path |");
      sb.AppendLine("|---|------|------|------------|");

      int rank = 1;
      foreach (var node in heavyHitters) {
        sb.AppendLine($"| {rank++} | {node.FormattedSize} | {node.Type} | `{node.Path}` |");
      }

      sb.AppendLine();
      sb.AppendLine("## Size by Type");
      sb.AppendLine();

      var byType = heavyHitters
        .GroupBy(n => n.Type)
        .Select(g => new { Type = g.Key, Size = g.Sum(n => n.FileSize), Count = g.Count() })
        .OrderByDescending(x => x.Size);

      sb.AppendLine("| Type | Count | Total Size |");
      sb.AppendLine("|------|-------|------------|");

      foreach (var group in byType) {
        sb.AppendLine($"| {group.Type} | {group.Count} | {FormatSize(group.Size)} |");
      }

      return sb.ToString();
    }

    string ExportMarkdown(DependencyGraph graph, ReportOptions options) {
      var sb = new StringBuilder();
      sb.AppendLine("# Asset Insights Report");
      sb.AppendLine();
      sb.AppendLine($"**Total Assets:** {graph.NodeCount}");
      sb.AppendLine($"**Total Dependencies:** {graph.EdgeCount}");
      sb.AppendLine();

      if (options.IncludeSizeBreakdown) {
        sb.AppendLine("## Size Breakdown by Type");
        sb.AppendLine();
        AppendSizeBreakdown(sb, graph, options);
      }

      if (options.TopHeavyHittersCount > 0) {
        sb.AppendLine("## Heavy Hitters");
        sb.AppendLine();
        AppendHeavyHitters(sb, graph, options);
      }

      if (options.IncludeDependencyPaths && !string.IsNullOrEmpty(options.TargetAsset)) {
        sb.AppendLine("## Dependency Paths");
        sb.AppendLine();
        AppendDependencyPaths(sb, graph, options);
      }

      return sb.ToString();
    }

    string ExportMermaid(DependencyGraph graph, ReportOptions options) {
      var sb = new StringBuilder();
      sb.AppendLine("```mermaid");
      sb.AppendLine("flowchart TD");

      var nodes = graph.GetAllNodes().Take(50);
      var nodeIds = new Dictionary<string, string>();
      int nodeIndex = 0;

      foreach (var node in nodes) {
        var id = $"N{nodeIndex++}";
        nodeIds[node.Path] = id;

        var label = node.Name;
        if (label.Length > 20)
          label = label.Substring(0, 17) + "...";

        sb.AppendLine($"    {id}[\"{label}<br/>{node.FormattedSize}\"]");
      }

      sb.AppendLine();

      foreach (var node in nodes) {
        if (!nodeIds.TryGetValue(node.Path, out var fromId))
          continue;

        var deps = graph.GetDependencies(node.Path);
        foreach (var dep in deps) {
          if (nodeIds.TryGetValue(dep, out var toId)) {
            sb.AppendLine($"    {fromId} --> {toId}");
          }
        }
      }

      sb.AppendLine("```");
      return sb.ToString();
    }

    string ExportJson(DependencyGraph graph, ReportOptions options) {
      var sb = new StringBuilder();
      sb.AppendLine("{");
      sb.AppendLine($"  \"nodeCount\": {graph.NodeCount},");
      sb.AppendLine($"  \"edgeCount\": {graph.EdgeCount},");
      sb.AppendLine("  \"nodes\": [");

      var nodes = graph.GetAllNodes().ToList();
      for (int i = 0; i < nodes.Count; i++) {
        var node = nodes[i];
        var comma = i < nodes.Count - 1 ? "," : "";
        sb.AppendLine($"    {{\"path\": \"{EscapeJson(node.Path)}\", \"size\": {node.FileSize}, \"type\": \"{node.Type}\"}}{comma}");
      }

      sb.AppendLine("  ]");
      sb.AppendLine("}");
      return sb.ToString();
    }

    void AppendSizeBreakdown(StringBuilder sb, DependencyGraph graph, ReportOptions options) {
      var nodes = graph.GetAllNodes();

      if (options.FilterTypes.Count > 0)
        nodes = nodes.Where(n => options.FilterTypes.Contains(n.Type));

      var byType = nodes
        .GroupBy(n => n.Type)
        .Select(g => new { Type = g.Key, Size = g.Sum(n => n.FileSize), Count = g.Count() })
        .OrderByDescending(x => x.Size);

      sb.AppendLine("| Type | Count | Total Size |");
      sb.AppendLine("|------|-------|------------|");

      foreach (var group in byType) {
        sb.AppendLine($"| {group.Type} | {group.Count} | {FormatSize(group.Size)} |");
      }

      sb.AppendLine();
    }

    void AppendHeavyHitters(StringBuilder sb, DependencyGraph graph, ReportOptions options) {
      var nodes = graph.GetAllNodes();

      if (options.FilterTypes.Count > 0)
        nodes = nodes.Where(n => options.FilterTypes.Contains(n.Type));

      var heavyHitters = nodes
        .OrderByDescending(n => n.FileSize)
        .Take(options.TopHeavyHittersCount);

      sb.AppendLine("| # | Size | Type | Asset Path |");
      sb.AppendLine("|---|------|------|------------|");

      int rank = 1;
      foreach (var node in heavyHitters) {
        sb.AppendLine($"| {rank++} | {node.FormattedSize} | {node.Type} | `{node.Path}` |");
      }

      sb.AppendLine();
    }

    void AppendDependencyPaths(StringBuilder sb, DependencyGraph graph, ReportOptions options) {
      var paths = PathFinder.FindWhyIncluded(graph, options.TargetAsset);

      if (paths.Count == 0) {
        sb.AppendLine("No dependency paths found.");
        return;
      }

      foreach (var path in paths.Take(5)) {
        sb.AppendLine("```");
        sb.AppendLine(string.Join(" → ", path));
        sb.AppendLine("```");
        sb.AppendLine();
      }
    }

    string FormatSize(long bytes) {
      string[] sizes = { "B", "KB", "MB", "GB" };
      int order = 0;
      double size = bytes;

      while (size >= 1024 && order < sizes.Length - 1) {
        order++;
        size /= 1024;
      }

      return $"{size:0.##} {sizes[order]}";
    }

    string EscapeJson(string value) {
      return value
        .Replace("\\", "\\\\")
        .Replace("\"", "\\\"")
        .Replace("\n", "\\n")
        .Replace("\r", "\\r")
        .Replace("\t", "\\t");
    }
  }
}
