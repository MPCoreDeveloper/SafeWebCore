# SafeWebCore Examples

This directory contains three complete, runnable ASP.NET Core applications demonstrating SafeWebCore in different integration patterns.

---

## Quick Start

All examples are located in `/examples/` and reference the local `src/SafeWebCore` project directly.

### Run an example

```bash
cd examples/<ProjectName>
dotnet run
```

Then open `http://localhost:5000` in your browser and inspect the response headers using DevTools → Network → Response Headers.

---

## 📌 Minimal API Example

**Location:** `examples/MinimalApi/`

The fastest way to add security headers to a Minimal API application. Demonstrates:

- ✅ One-line A+ setup with `AddNetSecureHeadersStrictAPlus()`
- ✅ CSP directive customization using `with { ... }` syntax
- ✅ Per-request nonce access via `HttpContext.GetCspNonce()`
- ✅ CSP violation reporting with `UseCspReport()` middleware
- ✅ Endpoint exclusion using `SkipNetSecureHeaders()` for health probes
- ✅ Inline nonce injection in HTML responses

### Key files

| File | Purpose |
|------|---------|
| `Program.cs` | Application setup, minimal API routes, nonce usage |
| `README.md` | Feature overview and verification commands |

### Run

```bash
cd examples/MinimalApi
dotnet run
```

Verify headers:
```bash
curl -sI http://localhost:5000 | grep -E "content-security|strict-transport|x-frame|permissions"
```

---

## 🎨 MVC Example

**Location:** `examples/MvcApp/`

Demonstrates SafeWebCore in a full ASP.NET Core MVC application with Razor Views. Shows:

- ✅ MVC preset configuration with `AddNetSecureHeadersMvcPreset()`
- ✅ Typed policy builders for `Referrer-Policy`, `Permissions-Policy`, and `Cross-Origin-*` headers
- ✅ Path-based policy overrides (e.g., `/public` routes in report-only CSP mode)
- ✅ `[CspNonce]` controller attribute for automatic nonce injection into `ViewData`
- ✅ Razor TagHelpers for automatic nonce injection on `<script>` and `<style>` tags
- ✅ Mixed explicit (ViewData) and automatic (TagHelper) nonce usage

### Key files

| File | Purpose |
|------|---------|
| `Program.cs` | Service registration, preset configuration, path policies, typed builders |
| `Controllers/HomeController.cs` | `[CspNonce]` attribute example |
| `Views/_ViewImports.cshtml` | TagHelper registration |
| `Views/Home/Index.cshtml` | Nonce injection patterns (explicit and automatic) |
| `Views/Shared/_Layout.cshtml` | Layout with nonce TagHelpers |
| `README.md` | Feature overview and TagHelper patterns |

### Run

```bash
cd examples/MvcApp
dotnet run
```

Open `http://localhost:5000` and inspect the page source to see nonce injection.

---

## 🔌 API Service Example

**Location:** `examples/ApiService/`

Demonstrates SafeWebCore in a Web API application with controllers. Shows:

- ✅ API preset configuration with `AddNetSecureHeadersApiPreset()`
- ✅ Optional headers like `X-Robots-Tag` to prevent search engine indexing
- ✅ Path-based policies for internal routes (`/internal`)
- ✅ Custom `ICspReportSink` implementation (`JsonFileCspReportSink`) for CSP violations
- ✅ Endpoint-level header overrides:
  - `[SkipNetSecureHeaders]` for health probes
  - `[CspMode(CspEndpointMode.ReportOnly)]` for metrics endpoints
- ✅ Standard secured endpoints with no overrides

### Key files

| File | Purpose |
|------|---------|
| `Program.cs` | Service registration, API preset, path policies, custom sink |
| `Controllers/AdminController.cs` | Health and metrics endpoints with overrides (`[SkipNetSecureHeaders]`, `[CspMode]`) |
| `Controllers/ProductsController.cs` | Standard secured API endpoint |
| `Infrastructure/JsonFileCspReportSink.cs` | Custom CSP violation sink writing JSON-lines to disk |
| `README.md` | Feature overview and endpoint verification |

### Run

```bash
cd examples/ApiService
dotnet run
```

Exercise the endpoints:

```bash
# Fully secured
curl -sI http://localhost:5000/api/products

# No security headers
curl -sI http://localhost:5000/admin/health

# Report-only CSP
curl -sI http://localhost:5000/admin/metrics

# Path policy
curl -sI http://localhost:5000/internal/config
```

CSP violations are logged to `csp-violations.jsonl` next to the binary.

---

## 📊 Feature Matrix

| Feature | MinimalApi | MvcApp | ApiService |
|---------|:----------:|:------:|:----------:|
| **Strict A+ preset** | ✅ | | |
| **API preset** | | | ✅ |
| **MVC preset** | | ✅ | |
| **CSP nonce via `GetCspNonce()`** | ✅ | | |
| **CSP nonce via `[CspNonce]` attribute** | | ✅ | |
| **TagHelper nonce injection** | | ✅ | |
| **Typed `ReferrerPolicyBuilder`** | | ✅ | |
| **Typed `PermissionsPolicyBuilder`** | | ✅ | |
| **Typed `CrossOriginPolicyBuilder`** | | ✅ | |
| **Path-based policies** | ✅ | ✅ | ✅ |
| **CSP report endpoint** | ✅ | | |
| **Custom `ICspReportSink`** | | | ✅ |
| **Endpoint overrides (`[Skip]`, `[CspMode]`)** | | | ✅ |

---

## 🔍 Verifying Headers

### Using DevTools (browser)

1. Open the application in your browser
2. Open **DevTools** (F12)
3. Go to **Network** tab
4. Reload the page
5. Click on the request
6. Go to **Response Headers** tab
7. Look for headers like `Content-Security-Policy`, `Strict-Transport-Security`, etc.

### Using curl (command line)

```bash
# View all response headers
curl -sI http://localhost:5000 | less

# View specific headers
curl -sI http://localhost:5000 | grep -i content-security
curl -sI http://localhost:5000 | grep -i strict-transport
```

### Using online tools

- [securityheaders.com](https://securityheaders.com) — Paste your domain URL to get a security grade
- [csp-evaluator.withgoogle.com](https://csp-evaluator.withgoogle.com) — Paste a CSP header to check for issues

---

## 🚀 Next Steps

- Review the **[Getting Started](getting-started.md)** guide for installation and basic setup
- Explore **[CSP Configuration](csp-configuration.md)** for advanced nonce patterns and directive customization
- Read **[Advanced Configuration](advanced-configuration.md)** for custom policies, CSP reporting, and troubleshooting
- Check **[Presets](presets.md)** for all available preset profiles and their defaults
