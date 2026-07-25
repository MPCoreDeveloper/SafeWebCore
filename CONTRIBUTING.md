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
- All roadmap work must preserve 100% backward compatibility unless an explicit exception is approved

## Backward Compatibility Policy

This repository treats backward compatibility as a hard requirement for normal releases.

- Read [docs/development/backward-compatibility-policy.md](docs/development/backward-compatibility-policy.md) before changing public APIs, defaults, presets, or configuration behavior.
- Prefer additive, opt-in changes over behavioral changes to existing consumers.
- Keep existing registration methods, presets, and configuration paths working.

### Public API surface changes

We use `Microsoft.CodeAnalysis.PublicApiAnalyzers` to track public surface via `PublicAPI.Shipped.txt` and `PublicAPI.Unshipped.txt`.

- Removing or renaming a symbol listed in `PublicAPI.Shipped.txt` is a hard error (`RS0037`).
- During the initial adoption phase, new undeclared public symbols (`RS0016`) and symbols listed but not found (`RS0017`) are warnings.
- Add intentional new public API to `PublicAPI.Unshipped.txt` and promote it to `Shipped.txt` on release.
- See the library `.csproj` files for the current analyzer configuration.

## Reporting Issues

Use [GitHub Issues](https://github.com/MPCoreDeveloper/SafeWebCore/issues) with the provided templates.

## Code of Conduct

Be respectful and constructive. We follow the [Contributor Covenant](https://www.contributor-covenant.org/).
