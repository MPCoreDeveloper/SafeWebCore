# Documentation Archive

This folder contains obsolete or superseded documentation that has been removed from active use.

## Contents

### `implementation-plan-v1.2.md`
- **Reason archived:** All v1.2 features have been implemented and are now documented in the active documentation.
- **What it was:** Detailed phase-based implementation plan for v1.2 features (CSP Report-Only, Path Policies, Startup Validation, TagHelpers, Typed Builders, CSP Reporting, Endpoint Overrides, Optional Headers, Presets).
- **Where to find current documentation:** See `/docs/presets.md`, `/docs/advanced-configuration.md`, `/docs/csp-configuration.md`, and `/docs/getting-started.md`.

### `roadmap-v1.2.md`
- **Reason archived:** v1.2 roadmap is now complete. All planned features are implemented.
- **What it was:** High-level feature roadmap for the v1.2 milestone.
- **Where to find current documentation:** See `CHANGELOG.md` for completed features and current capabilities.

## Future Archive Candidates

- Future v1.3+ planning documents can be archived once features are shipped and documentation is integrated into the main guides.

## Active Documentation Structure

```
docs/
├── README.md                    # Documentation index
├── getting-started.md           # Installation and quick start (updated with v1.1.0+ features)
├── security-headers.md          # Header-by-header reference
├── csp-configuration.md         # CSP builder, nonces, directives, performance (v1.1.0+ enhancements)
├── presets.md                   # All five presets documented with comparison table
├── advanced-configuration.md    # Path policies, report-only, reporting, testing, troubleshooting
├── benchmarks.md                # Performance benchmarking guide
├── examples.md                  # Three example projects (MinimalApi, MvcApp, ApiService)
└── archive/                     # This folder
    ├── implementation-plan-v1.2.md
    └── roadmap-v1.2.md
```

## Update History

- **2025-01-28:** Archived v1.2 planning documents; updated all active docs with latest features
