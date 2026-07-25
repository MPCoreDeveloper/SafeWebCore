# Backward Compatibility Policy

`SafeWebCore` and `SafeWebCore.FraudDetection` are maintained with a **100% backward compatibility** commitment for supported releases.

This policy defines what that commitment means for source code, package updates, configuration, and runtime behavior.

---

## Compatibility promise

For supported releases, the project will not intentionally introduce breaking changes to existing consumers unless such a change is explicitly called out as a future major-version decision and is approved as an exception to this policy.

The default expectation for normal roadmap work is:

- additive changes only
- opt-in behavior for new capabilities
- unchanged defaults for existing consumers
- unchanged public APIs for existing consumers

---

## Public API rules

The following changes are considered breaking and are not allowed in normal releases:

- removing a public type, member, option, attribute, extension method, or preset
- renaming a public type, member, option, attribute, extension method, or preset
- changing the meaning of a supported public API in a way that breaks existing callers
- tightening API requirements in a way that invalidates existing valid code
- changing namespaces or package identities for existing public entry points

The following changes are allowed when implemented carefully:

- adding new public APIs
- adding new overloads
- adding new opt-in extension methods
- adding new packages such as tooling, analyzers, diagnostics, or testing helpers
- adding new additive options with safe defaults that preserve existing behavior

All public APIs must continue to include XML documentation.

### Public API baseline tracking

`SafeWebCore` and `SafeWebCore.FraudDetection` use `Microsoft.CodeAnalysis.PublicApiAnalyzers` to track the declared public surface via `PublicAPI.Shipped.txt` and `PublicAPI.Unshipped.txt`.

- `RS0037` is treated as a hard error: removing or renaming a symbol listed in `PublicAPI.Shipped.txt` is a breaking change.
- During the initial rollout phase, `RS0016` (public symbol not listed) and `RS0017` (listed symbol not found) are treated as warnings.
- New public API should be added to `PublicAPI.Unshipped.txt` first and promoted to `Shipped.txt` on release when the change is intentional.
- See the project `.csproj` files for the exact analyzer configuration.

This mechanism helps enforce the backward compatibility rules above.

---

## Configuration and options rules

Existing configuration paths must remain supported.

This includes:

- current registration methods
- current preset helpers
- existing option names and semantics
- existing default values
- supported `appsettings.json` binding patterns

The following are not allowed without a new explicit opt-in path:

- changing the default value of an existing option
- changing preset behavior in a way that weakens or relaxes existing documented output for current users
- reinterpreting an existing option to mean something different
- requiring consumers to migrate configuration for an otherwise routine upgrade

Preferred approach for new behavior:

1. add a new option or helper
2. keep the old path working
3. document the new path as an additive convenience

---

## Runtime behavior rules

Existing runtime behavior must remain stable unless the consumer explicitly opts into new behavior.

Examples:

- existing middleware registration should continue to produce the same documented behavior
- existing preset methods should continue to emit the same effective security posture unless a new preset/helper is introduced
- existing fraud-detection registration and notification flows must remain functional
- existing CSP reporting integrations must continue to work with the current abstractions

Bug fixes are allowed, but they should:

- correct objectively incorrect behavior
- preserve the intended contract of the feature
- avoid surprising working applications
- be covered by tests so regressions are visible

---

## Performance and diagnostics rules

Performance-sensitive code paths must not regress materially as part of compatibility-safe feature work.

When adding new capabilities:

- keep hot-path overhead neutral where possible
- prefer opt-in diagnostics and observability hooks
- avoid forcing new dependencies into existing runtime packages
- validate meaningful performance-sensitive changes with benchmarks when appropriate

---

## Documentation rules

Documentation for new features must preserve trust for existing consumers.

Docs should:

- clearly state when a new API is optional
- avoid implying that existing supported APIs are obsolete unless they are formally deprecated
- explain compatibility expectations for upgrades
- include upgrade-safe examples when multiple setup styles are supported

---

## Release and review checklist

Before shipping roadmap work, verify:

- public API compatibility has been reviewed
- existing tests still pass
- new tests cover the added behavior
- docs are updated
- changelog entries accurately describe additive changes
- default behavior for existing consumers remains unchanged unless the feature is explicitly opt-in

---

## Exception handling

Any proposal that would break this policy must be treated as an exception case.

That requires:

- explicit discussion
- clear migration guidance
- prominent release-note communication
- a deliberate versioning decision rather than an incidental change

Until such an exception is approved, contributors should assume this policy is mandatory.
