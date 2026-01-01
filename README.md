# Asset Insights

[![Build](https://github.com/soo-bak/soobak-asset-insights/actions/workflows/unity-tests.yml/badge.svg)](https://github.com/soo-bak/soobak-asset-insights/actions/workflows/unity-tests.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![Unity 6+](https://img.shields.io/badge/Unity-6%2B-black.svg)](https://unity.com/)

A Unity Editor tool for analyzing asset dependencies, tracking size impact, and generating shareable reports.

---

## Why Asset Insights?

- **Reduce Build Size** - Identify and remove unused assets wasting storage
- **Catch Issues Early** - Detect circular dependencies before they cause problems
- **Understand Your Project** - Visualize which assets depend on what
- **Optimize Assets** - Find oversized textures and uncompressed audio
- **Team Collaboration** - Export reports in Markdown, Mermaid, or JSON

---

## Features

### Dependency Analysis
- **Why Included?** - Trace the complete dependency path from root assets to any target
- **Heavy Hitters** - Identify size-heavy assets that impact build size
- **Dependency Graph** - Visualize asset relationships in a bidirectional graph view

### Dashboard
- **Type Breakdown** - See distribution of assets by file type with size percentages
- **Largest Assets** - Quick view of top 10 space-consuming files
- **Unused Assets** - Find assets not referenced by any scene
- **Optimization Issues** - Detect oversized textures, uncompressed audio
- **Circular Dependencies** - Visualize dependency cycles with clear chain display
- **Duplicate Assets** - Find files with the same name in multiple locations

### Report Generation
- **Markdown** - Clean tables for documentation and team sharing
- **Mermaid** - Flowchart diagrams for visual dependency mapping
- **JSON** - Structured data for custom tooling integration

### Large Project Support
- Async scanning with progress tracking
- Cancellation support for long operations
- Configurable filters (by type, path, size)
- Sortable columns (Name, Refs, Deps, Size)

## Installation

### Via Git URL (UPM)
1. Open Package Manager (`Window > Package Manager`)
2. Click `+` > `Add package from git URL`
3. Enter: `https://github.com/soo-bak/soobak-asset-insights.git?path=Packages/com.soobak.asset-insights`

### Manual Installation
1. Clone this repository
2. Copy `Packages/com.soobak.asset-insights` to your project's `Packages` folder

## Usage

### Main Window
Open via `Window > Asset Insights`

1. Click **Scan Project** to analyze all assets
2. Browse assets sorted by size
3. Use search to filter results
4. Export reports in your preferred format

### Context Menu
Right-click any asset in Project window:
- **Asset Insights > Why Included?** - Shows dependency paths
- **Asset Insights > Show Dependencies** - Lists direct dependencies

### Scripting API

```csharp
using Soobak.AssetInsights;

// Scan project
var scanner = new DependencyScanner();
scanner.ScanImmediate();

// Find dependency paths
var paths = PathFinder.FindWhyIncluded(scanner.Graph, "Assets/Texture.png");

// Generate reports
var exporter = new ReportExporter();
var markdown = exporter.ExportHeavyHitters(scanner.Graph, 20);
```

## Architecture

```
Packages/com.soobak.asset-insights/
├── Editor/
│   ├── Core/           # Models, Graph, PathFinder
│   ├── Services/       # Scanner, Exporter (with interfaces)
│   └── UI/             # EditorWindows, Context menus
└── Tests/
    └── Editor/         # NUnit test suites
```

### Design Principles
- **Interface-based** - `IDependencyScanner`, `IReportExporter` for testability
- **Layered** - Clear separation between Core, Services, and UI
- **Async-ready** - Coroutine-based scanning for large projects

## Requirements

- Unity 6 (6000.0) or later
- Editor Coroutines package (auto-installed via dependencies)

## Contributing

Contributions are welcome! Please see [CONTRIBUTING.md](CONTRIBUTING.md) for guidelines.

## License

MIT License - see [LICENSE](LICENSE) for details.
