# Preface

SafeWebCore is a **security-header middleware family** for ASP.NET Core. It writes the response headers that make a
browser refuse to do the unsafe thing — a strict `Content-Security-Policy` with a per-request nonce, HSTS, frame and
referrer rules, cross-origin isolation, Permissions-Policy — and it does it in one line, with an **A+** on
[securityheaders.com](https://securityheaders.com) as the target rather than an afterthought.

This manual describes version **1.9.0** of the library. It is written for the developer who has to ship a service, not
only for the developer who wants to read source code: every chapter explains the behaviour you can rely on, the behaviour
you should not rely on, and the code that exercises it.

## Who this manual is for

You will get the most out of it if you are comfortable with C#, `Program.cs`-style application setup and dependency
injection. No knowledge of the CSP specification is assumed. Where a term from the specification matters — nonce, strict
dynamic, report-only, reporting endpoint, network error logging — the chapter explains it in the terms a caller needs, and
stops there.

## How the manual is organised

The first four chapters are the shortest path to a working, A+-graded response:

- **Chapter 1** describes what the library does and the guarantees it makes.
- **Chapter 2** states, just as plainly, what it does not do.
- **Chapter 3** installs the package and produces the first graded response.
- **Chapter 4** explains the presets, which are the object most applications should start from.

Chapters 5 to 9 cover one part of the surface each: the options model, the Content Security Policy, nonces and the
Razor Tag Helpers, per-path policies, and per-endpoint overrides. Chapters 10 to 14 cover operations: report-only
rollout and violation reporting, the diagnostics endpoint, telemetry, custom policies with validation, and configuration
binding. Chapters 15 to 17 cover the companion packages — fraud detection, JWT bearer hardening, and the analyzers and
testing helpers. Chapter 18 is the architecture and the compatibility promise. Chapter 19 is a cookbook of complete
recipes, Chapter 20 turns the lens on this book, and Chapter 21 is the option reference.

You do not have to read the chapters in order. Each one starts from a complete example and refers to earlier chapters
only for background.

## Conventions

**One preset, then the exceptions.** Every chapter shows the shortest configuration that produces the behaviour it
describes, and only then the individual switches. A preset plus three overrides is easier to review than forty
assignments.

**Code is complete enough to compile.** Snippets omit `using` directives and the surrounding `Program.cs` boilerplate when
they are obvious, but every method call, property and argument name is the one in the documented API.

**Defaults are the contract.** Where a chapter says "by default", it quotes the initialiser in
`NetSecureHeadersOptions` or `CspOptions`. A release may never change one of those values silently — Chapter 18 explains
the promise and how it is enforced.

**Warnings are not errors.** A validation failure stops startup; a configuration that weakens a header is reported by the
diagnostics endpoint and by the analyzer package, not by an exception at runtime. Chapter 13 explains which is which.

**The manual is built from Markdown.** The chapters in `docs/manual/src` are the source of both the GitHub documentation
and the PDF. If you find a discrepancy between this text and the code, the code is the source of truth; please open an
issue so the text can be corrected. Chapter 20 describes how those files become the PDF you are reading.

> **Version note.** This manual is written against 1.9.0. The public API is covered by an approved snapshot in each
> package, so a change to a public member is a deliberate, reviewed event rather than an accident of a refactor. Code
> that compiled against 1.8.1 keeps compiling: every addition since then is additive, and the changelog records every
> addition and every change of behaviour.

## What you need

- The .NET SDK that can target **.NET 10** or **.NET 11**. The packages ship assemblies for both.
- A service you can restart. Security headers are response metadata; nothing about them needs a database, a queue or a
  deployment slot.
- For the optional diagnostics endpoint and the reports: an HTTP client, and nothing else.
- For building this manual: the .NET SDK and Chrome or Edge. Chapter 20 describes the build. That is a maintainer
  concern, not a requirement of the library.
