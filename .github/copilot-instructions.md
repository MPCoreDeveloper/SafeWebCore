# Copilot Instructions

## Project Guidelines
- Project requirement: remain 100% backward compatible for all changes and releases.
- Gebruik altijd xUnit v3 voor tests in deze repository; target blijft .NET 10 met C# 14 conventies.
- Voor SafeWebCore planning en wijzigingen, handhaaf 100% backward compatibility voor alle wijzigingen en releases.
- Bij `SafeWebCore.FraudDetection`, verwacht een injecteerbare mailclient voor notificaties en event-driven notificaties zodat teams een eigen mailmodule als consumer kunnen koppelen, in plaats van alleen logging of een vaste mailclient.

## API Response Guidelines
- Voor SafeWebCore API routes, stuur alleen headers die logisch/nodig zijn voor API-responses en vermijd onnodige browsergerichte headers.

## Spec-driven workflow
- Werk plannen uit met GitHub Spec Kit: `/speckit-specify` → `/speckit-plan` → `/speckit-tasks` → `/speckit-implement`.
- `.specify/memory/constitution.md` bepaalt wat een spec mag beloven (achterwaartse compatibiliteit, additive API, tests, documentatie).
- Deze repository gebruikt de Cline-integratie. Voor Copilot: `specify integration switch copilot` en daarna terug met `specify integration switch cline`.
- Zie `docs/development/spec-kit-workflow.md` voor de volledige workflow en de guardrails.
