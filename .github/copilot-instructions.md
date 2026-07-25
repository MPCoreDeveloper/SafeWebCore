# Copilot Instructions

## Project Guidelines
- Project requirement: remain 100% backward compatible for all changes and releases.
- Gebruik altijd xUnit v3 voor tests in deze repository; target blijft .NET 10 met C# 14 conventies.
- Voor SafeWebCore planning en wijzigingen, handhaaf 100% backward compatibility voor alle wijzigingen en releases.
- Bij `SafeWebCore.FraudDetection`, verwacht een injecteerbare mailclient voor notificaties en event-driven notificaties zodat teams een eigen mailmodule als consumer kunnen koppelen, in plaats van alleen logging of een vaste mailclient.

## API Response Guidelines
- Voor SafeWebCore API routes, stuur alleen headers die logisch/nodig zijn voor API-responses en vermijd onnodige browsergerichte headers.
