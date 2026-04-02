using System.Text;

namespace SafeWebCore.Builder;

/// <summary>
/// Fluent builder for composing typed <c>Permissions-Policy</c> header values.
/// </summary>
public sealed class PermissionsPolicyBuilder
{
    private readonly Dictionary<PermissionsFeature, string[]> _rules = [];

    /// <summary>
    /// Disables the specified feature by emitting an empty allow-list (<c>()</c>).
    /// </summary>
    /// <param name="feature">The browser feature to disable.</param>
    /// <returns>This builder for chaining.</returns>
    public PermissionsPolicyBuilder Disable(PermissionsFeature feature)
    {
        _rules[feature] = [];
        return this;
    }

    /// <summary>
    /// Allows the feature only for same-origin requests.
    /// </summary>
    /// <param name="feature">The browser feature to allow for same-origin.</param>
    /// <returns>This builder for chaining.</returns>
    public PermissionsPolicyBuilder AllowSelf(PermissionsFeature feature)
    {
        _rules[feature] = ["self"];
        return this;
    }

    /// <summary>
    /// Allows the feature for the provided allow-list entries.
    /// </summary>
    /// <param name="feature">The browser feature to configure.</param>
    /// <param name="allowList">Allow-list entries such as <c>self</c>, <c>*</c>, or origins.</param>
    /// <returns>This builder for chaining.</returns>
    public PermissionsPolicyBuilder Allow(PermissionsFeature feature, params string[] allowList)
    {
        ArgumentNullException.ThrowIfNull(allowList);

        var normalizedAllowList = allowList
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Select(NormalizeAllowListValue)
            .ToArray();

        _rules[feature] = normalizedAllowList;
        return this;
    }

    /// <summary>
    /// Builds the final <c>Permissions-Policy</c> header value.
    /// </summary>
    /// <returns>A comma-separated permissions policy header value.</returns>
    public string Build()
    {
        var sb = new StringBuilder(256);

        foreach (var (feature, allowList) in _rules)
        {
            if (sb.Length > 0)
                sb.Append(", ");

            sb.Append(ToPolicyToken(feature)).Append("=(");

            if (allowList.Length > 0)
            {
                for (var index = 0; index < allowList.Length; index++)
                {
                    if (index > 0)
                        sb.Append(' ');

                    sb.Append(allowList[index]);
                }
            }

            sb.Append(')');
        }

        return sb.ToString();
    }

    private static string NormalizeAllowListValue(string value)
    {
        var trimmedValue = value.Trim();
        if (trimmedValue is "self" or "*")
            return trimmedValue;

        return trimmedValue.StartsWith('"') && trimmedValue.EndsWith('"')
            ? trimmedValue
            : $"\"{trimmedValue}\"";
    }

    private static string ToPolicyToken(PermissionsFeature feature) => feature switch
    {
        PermissionsFeature.Accelerometer => "accelerometer",
        PermissionsFeature.AmbientLightSensor => "ambient-light-sensor",
        PermissionsFeature.Autoplay => "autoplay",
        PermissionsFeature.Battery => "battery",
        PermissionsFeature.Camera => "camera",
        PermissionsFeature.CrossOriginIsolated => "cross-origin-isolated",
        PermissionsFeature.DisplayCapture => "display-capture",
        PermissionsFeature.DocumentDomain => "document-domain",
        PermissionsFeature.EncryptedMedia => "encrypted-media",
        PermissionsFeature.ExecutionWhileNotRendered => "execution-while-not-rendered",
        PermissionsFeature.ExecutionWhileOutOfViewport => "execution-while-out-of-viewport",
        PermissionsFeature.Fullscreen => "fullscreen",
        PermissionsFeature.Geolocation => "geolocation",
        PermissionsFeature.Gyroscope => "gyroscope",
        PermissionsFeature.Hid => "hid",
        PermissionsFeature.IdleDetection => "idle-detection",
        PermissionsFeature.Magnetometer => "magnetometer",
        PermissionsFeature.Microphone => "microphone",
        PermissionsFeature.Midi => "midi",
        PermissionsFeature.NavigationOverride => "navigation-override",
        PermissionsFeature.Payment => "payment",
        PermissionsFeature.PictureInPicture => "picture-in-picture",
        PermissionsFeature.PublickeyCredentialsGet => "publickey-credentials-get",
        PermissionsFeature.ScreenWakeLock => "screen-wake-lock",
        PermissionsFeature.Serial => "serial",
        PermissionsFeature.SyncXhr => "sync-xhr",
        PermissionsFeature.Usb => "usb",
        PermissionsFeature.WebShare => "web-share",
        PermissionsFeature.XrSpatialTracking => "xr-spatial-tracking",
        _ => throw new ArgumentOutOfRangeException(nameof(feature), feature, null)
    };
}
