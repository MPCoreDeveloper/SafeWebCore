# SafeWebCore

A .NET 10 library for building secure web applications with sensible defaults.

## Features

- Content Security Policy (CSP) middleware
- Security header management
- Zero-configuration secure defaults

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)

### Installation

```bash
dotnet add package SafeWebCore
```

### Usage

```csharp
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.UseSafeWebCore();

app.Run();
```

## Building

```bash
dotnet build
```

## Testing

```bash
dotnet test
```

## Project Structure

```
src/
  SafeWebCore/          # Main library
tests/
  SafeWebCore.Tests/    # Unit tests (xUnit)
docs/                            # Documentation
.github/                         # GitHub templates and workflows
.editorconfig                    # Code style settings
Directory.Build.props            # Shared MSBuild properties
SafeWebCore.slnx        # Solution file
```

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md) for guidelines.

## License

This project is licensed under the MIT License. See [LICENSE](LICENSE) for details.

## Changelog

See [CHANGELOG.md](CHANGELOG.md) for release history.
