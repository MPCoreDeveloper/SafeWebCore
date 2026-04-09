# Copilot Instructions

## Project Guidelines
- Project requirement: remain 100% backward compatible for all changes and releases.
- Gebruik altijd xUnit v3 voor tests in deze repository; target blijft .NET 10 met C# 14 conventies.
- Bij `SafeWebCore.FraudDetection`, verwacht een injecteerbare mailclient voor notificaties en event-driven notificaties zodat teams een eigen mailmodule als consumer kunnen koppelen, in plaats van alleen logging of een vaste mailclient.
