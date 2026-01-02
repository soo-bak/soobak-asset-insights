# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0] - 2025-01-02

### Added

- **Dependency Analysis**
  - "Why Included?" feature to trace complete dependency paths
  - Heavy Hitters view to identify size-heavy assets
  - Bidirectional dependency graph visualization

- **Dashboard**
  - Project health score calculation
  - Type breakdown with size percentages
  - Largest assets view (top 10)
  - Unused assets detection with one-click delete
  - Optimization issues detection
  - Circular dependency visualization
  - Duplicate assets detection

- **Auto-Fix**
  - One-click fixes for oversized textures
  - Audio compression optimization
  - Material and mesh optimization rules

- **UI/UX**
  - Multi-select and bulk delete functionality
  - Expandable lists with Show More/Less
  - Keyboard shortcuts (Ctrl+F, F5, Ctrl+A, Delete, Ctrl+1/2/3)
  - Dashboard to List navigation with double-click
  - Sortable columns (Name, Refs, Deps, Size)
  - Context menu integration ("Why Included?", "Show Dependencies")

- **Report Export**
  - Markdown format for documentation
  - Mermaid diagrams for visual mapping
  - JSON format for tooling integration

- **Large Project Support**
  - Async scanning with progress tracking
  - Cancellation support for long operations
  - Configurable filters (by type, path, size)

### Performance

- Cached CircularDependencyDetector results
- Pre-cached lowercase strings for search optimization
- Search debouncing (150ms) and filter slider debouncing (100ms)
- Optimized LINQ chains with List.Sort()
- Incremental cache updates after fixes
- GPU memory optimization for texture analysis
- Memory cleanup with EditorUtility.UnloadUnusedAssetsImmediate()

### Fixed

- Resolved critical memory leaks in editor windows
- Fixed GPU memory leak in texture analysis
- Eliminated duplicate analyzer executions

[1.0.0]: https://github.com/soo-bak/soobak-asset-insights/releases/tag/v1.0.0
