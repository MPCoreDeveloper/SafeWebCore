# 7. Nonces and Tag Helpers

A nonce is the difference between a policy that is strict and a policy that is usable. SafeWebCore generates one per
request, exposes it to your code, and — in Razor — injects it into the tags that need it.

## What the middleware does per request

1. Generates **32 bytes** of cryptographically secure randomness (256 bits; the CSP Level 3 minimum is 128).
2. Base64-encodes them into **44 characters** (`NonceService.NonceLength`).
3. Stores the string in `HttpContext.Items` under `NetSecureHeaders.CspNonceKey` (`"CspNonce"`).
4. Substitutes it for every `{nonce}` in the policy it writes.

The generation path is allocation-light on purpose: `GenerateNonce()` uses `stackalloc` for both buffers and allocates
only the resulting string, and `TryWriteNonce(Span<char>, out int)` writes into a caller-supplied span and allocates
nothing at all. The latter exists for a response that is composed into a pooled buffer.

```csharp
public string GenerateNonce();                                   // one allocation: the string
public bool TryWriteNonce(Span<char> destination, out int written); // zero allocations
```

## Reading the nonce

`GetCspNonce()` is the supported way; it returns `null` when CSP is disabled or the middleware did not run for this
request.

```csharp
app.Use(async (context, next) =>
{
    if (context.GetCspNonce() is { } nonce)
    {
        context.Response.Headers["X-Csp-Nonce-Length"] = nonce.Length.ToString(CultureInfo.InvariantCulture);
    }

    await next();
});
```

In Razor, `Context` is the `HttpContext`, so the same extension works inline:

```cshtml
<script nonce="@Context.GetCspNonce()">
    window.bootstrap = { analytics: false };
</script>
```

## The Tag Helpers

Two tag helpers add the attribute automatically, so a view never has to remember:

| Tag Helper | Applies to |
| --- | --- |
| `CspScriptNonceTagHelper` | `<script>` elements that do not already carry a `nonce` |
| `CspStyleNonceTagHelper` | `<style>` elements that do not already carry a `nonce` |

Register them once, in `_ViewImports.cshtml`:

```cshtml
@addTagHelper *, SafeWebCore
```

After that, a view writes ordinary markup and the attribute appears:

```cshtml
<script>
    // runs, because the middleware's nonce is on the tag
    document.getElementById("clock").textContent = new Date().toLocaleTimeString();
</script>
```

An explicit `nonce` value on the element is left alone: the helpers add, never overwrite. That makes them safe next to a
component that manages its own nonce.

## What `'strict-dynamic'` changes

With `'strict-dynamic'` in `script-src`, a script that carries the nonce may load further scripts without listing their
URLs, and host allow-lists in `script-src` are ignored by browsers that implement it. Two consequences follow, and both
are worth designing for:

- A `<script src="https://cdn.example.com/app.js" nonce="…">` is allowed; the same tag without the nonce is not, even
  though the host is reachable.
- Code that injects a `<script>` element from a nonced script works; the same injection from a non-script context
  (a form handler in HTML) does not.

Removing `'strict-dynamic'` returns the policy to host allow-listing, which is weaker but easier to retrofit. Both are
one string; Chapter 10 is how you find out which one your site actually needs.

## Rules that keep the nonce a nonce

- **Never cache it.** A nonce that outlives its request is a nonce an attacker can read from a cached page.
- **Never log it.** It is not a secret in the cryptographic sense, but a logged nonce is a published one.
- **Never reuse it across responses**, including error pages or a cached HTML fragment. The middleware generates a new
  one per request, which is exactly the property that makes the scheme work.
- **Do not put it in a URL.** It belongs in the header and in the attribute, nowhere else.
