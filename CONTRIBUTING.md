# Contributing to SafeWebCore

Thank you for your interest in contributing!

## How to Contribute

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/my-feature`)
3. Commit your changes (`git commit -m 'Add my feature'`)
4. Push to the branch (`git push origin feature/my-feature`)
5. Open a Pull Request

## Development Setup

### Prerequisites

- .NET 10 SDK
- Visual Studio 2026 or later / VS Code with C# Dev Kit

### Build

```bash
dotnet build
```

### Test

```bash
dotnet test
```

## Coding Standards

- Target .NET 10 / C# 14
- Use modern C# features (primary constructors, collection expressions, Lock class)
- Follow the conventions in `.editorconfig`
- Async methods must end with `Async` suffix
- All public APIs must have XML documentation

## Reporting Issues

Use [GitHub Issues](https://github.com/MPCoreDeveloper/SafeWebCore/issues) with the provided templates.

## Code of Conduct

Be respectful and constructive. We follow the [Contributor Covenant](https://www.contributor-covenant.org/).
