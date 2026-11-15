# 2. What SafeWebCore Is Not

A library is defined as much by what it refuses to do. Each line below is a deliberate boundary, not a gap on a
roadmap; if your requirement falls on the wrong side of one, Chapter 4 or Chapter 13 will usually point at the supported
way to get what you need.

**It is not a web application firewall.** The headers it writes constrain what a *browser* will execute or load. They do
not inspect a request, score it, or block it. Traffic that never reaches a browser — an API client, a scraper, a
`curl` — is unaffected by any header here.

**It does not sanitize your HTML, and it does not rewrite it.** `'strict-dynamic'` and a nonce stop injected scripts from
running; they do not make an injected string safe. Markup that must run with the nonce gets the attribute from the Razor
Tag Helpers (Chapter 7), from `GetCspNonce()`, or it does not run. There is no output rewriting in the middleware.

**It is not an authentication or authorization mechanism.** `SafeWebCore.JwtBearer` hardens and validates the
configuration of the JWT handler you already have (Chapter 16). It does not issue, refresh or revoke tokens, and it does
not decide who may call what.

**It does not store, aggregate or forward CSP violation reports.** `UseCspReport()` parses the report, hands it to every
registered `ICspReportSink` and answers `204`. Where a report goes — a log, a table, an alert — is your decision, made by
writing a sink (Chapter 10).

**It does not remove a header that a different layer adds later.** `RemoveServerHeader` and `RemoveXPoweredBy` delete the
header in `OnStarting`, which catches Kestrel and most modules, but a reverse proxy, a CDN or an IIS module that appends
after that point is out of reach. Chapter 11's diagnostics endpoint says so in its warnings, and no configuration here
changes it.

**It does not submit your domain to the HSTS preload list.** `HstsValue` can carry `preload`, which tells a browser the
site is a candidate; the submission is a form on `hstspreload.org` and a decision about every subdomain you own.

**It does not test your site against a browser.** The tests in this repository assert the headers on a response. Whether
a page actually works under the policy is a browser question, and the honest way to answer it is report-only mode
(Chapter 10) on real traffic.

**It does not make a weak policy strong.** If an application needs `'unsafe-inline'`, or `'unsafe-eval'`, or a wildcard
source, the middleware will write it — `CspOptions` accepts any string — and the analyzer package will tell you that the
result is weak (`SWC003`, `SWC004`, Chapter 17). It cannot invent a policy that your front end does not support.

**It does not replace a dependency or platform update.** HSTS, COOP/COEP and Trusted Types are only as good as the
runtime underneath them; the framework version you run is still your decision.

> **The boundary in one sentence:** SafeWebCore owns the response headers and the policy they describe. Everything the
> headers protect against happens in the browser; everything that produces the HTML, the token or the request is
> somebody else's component.
