# Contributing to Asset Insights

Thank you for your interest in contributing to Asset Insights! This document provides guidelines and instructions for contributing.

## Getting Started

### Prerequisites

- Unity 6 (6000.0) or later
- Git

### Development Setup

1. Fork and clone the repository:
   ```bash
   git clone https://github.com/YOUR_USERNAME/soobak-asset-insights.git
   ```

2. Open the project in Unity (the root folder, not the package folder)

3. The package is located at `Packages/com.soobak.asset-insights/`

4. Run tests via `Window > General > Test Runner`

## Code Style

We use a consistent code style enforced by `.editorconfig`:

- **Indentation**: 2 spaces (no tabs)
- **Braces**: K&R style (opening brace on same line)
- **Line endings**: LF
- **Final newline**: Required

### Example

```csharp
public class Example {
  readonly DependencyGraph _graph;

  public Example(DependencyGraph graph) {
    _graph = graph;
  }

  public void DoSomething() {
    if (_graph.NodeCount > 0) {
      // Process nodes
    } else {
      // Handle empty case
    }
  }
}
```

## Project Structure

```
Packages/com.soobak.asset-insights/
├── Editor/
│   ├── Core/           # Models, Graph, PathFinder
│   │   ├── Analyzers/  # Analysis algorithms
│   │   ├── Models/     # Data models
│   │   └── Optimization/ # Optimization rules
│   ├── Services/       # Scanner, Exporter interfaces
│   └── UI/             # EditorWindows, Panels
│       ├── Dashboard/  # Dashboard components
│       ├── Graph/      # Graph visualization
│       └── List/       # List view components
└── Tests/
    └── Editor/         # NUnit tests
```

## Making Changes

### Branch Naming

- `feature/description` - New features
- `fix/description` - Bug fixes
- `docs/description` - Documentation updates
- `refactor/description` - Code refactoring

### Commit Messages

We use [Conventional Commits](https://www.conventionalcommits.org/) with [Gitmoji](https://gitmoji.dev/):

```
:emoji: type: description

Examples:
:sparkles: feat: add circular dependency detection
:bug: fix: correct path calculation in graph view
:recycle: refactor: simplify scanner interface
:memo: docs: update README with new features
:white_check_mark: test: add tests for PathFinder
```

Common types:
- `feat` - New feature
- `fix` - Bug fix
- `docs` - Documentation
- `refactor` - Code refactoring
- `test` - Adding tests
- `style` - Code style changes
- `perf` - Performance improvements

## Pull Request Process

1. Create a feature branch from `main`
2. Make your changes following the code style
3. Add tests for new functionality
4. Update documentation if needed
5. Run all tests to ensure they pass
6. Submit a pull request

### PR Checklist

- [ ] Code follows project style (2-space indent, K&R braces)
- [ ] Self-reviewed the code
- [ ] Added/updated tests if applicable
- [ ] Documentation updated if needed
- [ ] PR title follows conventional commit format

## Testing

### Running Tests

1. Open Unity Test Runner: `Window > General > Test Runner`
2. Select `EditMode` tab
3. Click `Run All`

### Writing Tests

Tests are located in `Tests/Editor/`. Use NUnit framework:

```csharp
using NUnit.Framework;

namespace Soobak.AssetInsights.Tests {
  public class MyFeatureTests {
    [Test]
    public void MyMethod_WhenCondition_ShouldExpectedResult() {
      // Arrange
      var input = new MyClass();

      // Act
      var result = input.MyMethod();

      // Assert
      Assert.AreEqual(expected, result);
    }
  }
}
```

## Reporting Issues

### Bug Reports

Use the [Bug Report template](https://github.com/soo-bak/soobak-asset-insights/issues/new?template=bug_report.yml) and include:

- Unity version
- Operating system
- Steps to reproduce
- Expected vs actual behavior

### Feature Requests

Use the [Feature Request template](https://github.com/soo-bak/soobak-asset-insights/issues/new?template=feature_request.yml) and describe:

- The problem you're trying to solve
- Your proposed solution
- Alternatives considered

## Questions?

Feel free to open a [Discussion](https://github.com/soo-bak/soobak-asset-insights/discussions) or reach out via issues.

## License

By contributing, you agree that your contributions will be licensed under the MIT License.
