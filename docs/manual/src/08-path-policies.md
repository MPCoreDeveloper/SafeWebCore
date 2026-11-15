# 8. Path Policies

One application rarely has one header policy. The marketing site, the JSON API and the file download endpoint want
different CSP values, and a single global configuration forces the loosest of them on everyone. Path policies fix that
with a list of prefix-and-options pairs.

## The model

```csharp
public sealed class PathPolicyOptions
{
    public string PathPrefix { get; set; } = string.Empty;
    public NetSecureHeadersOptions Options { get; set; } = new();
}
```

```csharp
builder.Services.AddNetSecureHeaders(options =>
{
    options.ApplyPreset(SecurePresets.Mvc());

    options.PathPolicies =
    [
        SecurePresets.ApiPath("/api"),
        SecurePresets.OwaspApiPath("/api/admin"),
        new PathPolicyOptions
        {
            PathPrefix = "/downloads",
            Options = SecurePresets.ApiMinimal()
        }
    ];
});
```

## The matching rules

Four rules, all of them observable:

1. **The prefix is normalised at startup.** A missing leading slash is added, so `api` and `/api` are the same prefix.
2. **Matching is segment-aware and case-insensitive.** `/api` matches `/api`, `/api/` and `/api/orders/42`, and does
   **not** match `/apix` or `/apiary`.
3. **The longest matching prefix wins.** With `/api` and `/api/admin` configured, a request to `/api/admin/users` gets
   the second one, whatever order they were declared in. The resolver sorts once, at startup, by prefix length.
4. **No match means the global options.** A request that matches nothing keeps the configuration you passed to
   `AddNetSecureHeaders`, which is also what the diagnostics endpoint reports as "uses global policy".

Resolution happens per request but allocates nothing: the policies are pre-built — prefix, effective options, CSP
template and Reporting-Endpoints value — into an internal list that the middleware walks by index and exits on the first
match.

## What a policy changes

A path policy carries a complete `NetSecureHeadersOptions`, not a delta. That is deliberate: it means a policy can be
built by a preset (`ApiPath`) or by hand, and there is no merge order to reason about. The cost is that a policy which
only wants to change one header still has to start from a complete object — `SecurePresets.ApiPath(...)` is the
convenience for the common case:

```csharp
options.PathPolicies =
[
    SecurePresets.ApiPath("/api", api =>
    {
        api.EnableNel = true;
        api.NelValue = """{"report_to":"default","max_age":2592000}""";
    })
];
```

## Duplicates and empty prefixes fail at startup

The validator (Chapter 5) rejects two policies whose prefixes normalise to the same string, and any policy whose prefix
is null, empty or whitespace:

```text
Duplicate path policy prefix '/api' is not allowed. Path prefixes are normalized, so values like '/api' and 'api' collide.
Path policy prefix must not be null, empty, or whitespace. Fix: use a concrete path such as '/api' or '/admin'.
```

A duplicate would make the winner depend on declaration order, which is exactly the kind of invisible rule this library
refuses to have.

## When not to use a path policy

A path policy is the right tool when the split is **structural** — an API area, a downloads area, an admin area. It is
the wrong tool for a single endpoint that needs an exception; use the endpoint metadata of Chapter 9 instead, because a
policy per endpoint turns the configuration into a routing table nobody reviews.
