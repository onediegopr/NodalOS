using System.Net;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;

namespace OneBrain.ChromeLab.Bridge;

public static class ChromeLabBridgeSecurity
{
    public const string TokenHeaderName = "X-Nodal-Bridge-Token";
    public const string AllowedExtensionIdsEnvironmentVariable = "NODAL_OS_CHROME_EXTENSION_IDS";
    public const string EnableLocalPairingEnvironmentVariable = "NODAL_OS_ENABLE_LOCAL_PAIRING";

    private static readonly string[] PublicPaths =
    [
        "/health",
        "/config/public",
        ChromeLabLocalDevOperatorSurfaceReadOnlyRoute.RoutePath,
        ChromeLabLocalDevOperatorSurfaceHtmlRenderer.RoutePath
    ];

    public static bool IsPublicPath(PathString path) =>
        PublicPaths.Any(candidate => path.Equals(candidate, StringComparison.Ordinal));

    public static bool IsPairingPath(PathString path) =>
        path.Equals("/pairing/local-token", StringComparison.Ordinal);

    public static bool IsExtensionWebSocketPath(PathString path) =>
        path.Equals("/ws/extension", StringComparison.Ordinal);

    public static bool IsStealthWebSocketPath(PathString path) =>
        path.Equals("/ws/stealth", StringComparison.Ordinal);

    public static bool IsLocalPairingEnabled() =>
        string.Equals(
            Environment.GetEnvironmentVariable(EnableLocalPairingEnvironmentVariable),
            "true",
            StringComparison.OrdinalIgnoreCase);

    public static bool IsAuthorized(HttpContext context, ChromeLabOptions options, bool allowQueryToken = false)
    {
        var token = context.Request.Headers[TokenHeaderName].ToString();
        if (string.IsNullOrWhiteSpace(token))
        {
            var authorization = context.Request.Headers.Authorization.ToString();
            const string bearer = "Bearer ";
            if (authorization.StartsWith(bearer, StringComparison.OrdinalIgnoreCase))
                token = authorization[bearer.Length..].Trim();
        }

        if (allowQueryToken && string.IsNullOrWhiteSpace(token))
            token = context.Request.Query["access_token"].ToString();

        return FixedTimeEquals(token, options.ConnectionToken);
    }

    public static bool IsOriginAllowed(string? origin, ChromeLabOptions options)
    {
        if (string.IsNullOrWhiteSpace(origin))
            return true;

        if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri))
            return false;

        if (string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase))
        {
            if (uri.Port != options.Port)
                return false;

            if (string.Equals(uri.Host, "localhost", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(uri.Host, "127.0.0.1", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(uri.Host, "::1", StringComparison.OrdinalIgnoreCase))
                return true;

            return options.AllowLan && IsPrivateLanAddress(uri.Host);
        }

        if (!string.Equals(uri.Scheme, "chrome-extension", StringComparison.OrdinalIgnoreCase))
            return false;

        var allowedIds = GetAllowedExtensionIds();
        return allowedIds.Contains(uri.Host, StringComparer.OrdinalIgnoreCase);
    }

    public static IReadOnlyList<string> GetAllowedExtensionIds() =>
        (Environment.GetEnvironmentVariable(AllowedExtensionIdsEnvironmentVariable) ?? string.Empty)
            .Split([',', ';', ' '], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
            .ToArray();

    public static bool FixedTimeEquals(string? received, string? expected)
    {
        if (string.IsNullOrEmpty(received) || string.IsNullOrEmpty(expected))
            return false;

        var receivedBytes = Encoding.UTF8.GetBytes(received);
        var expectedBytes = Encoding.UTF8.GetBytes(expected);
        if (receivedBytes.Length != expectedBytes.Length)
            return false;

        return CryptographicOperations.FixedTimeEquals(receivedBytes, expectedBytes);
    }

    public static bool IsFatalProtocolError(string payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
            return false;

        return payload.Contains("\"error\":\"invalid_token\"", StringComparison.Ordinal) ||
               payload.Contains("\"error\":\"protocol_version_mismatch\"", StringComparison.Ordinal) ||
               payload.Contains("\"error\":\"authentication_required\"", StringComparison.Ordinal);
    }

    private static bool IsPrivateLanAddress(string host)
    {
        if (!IPAddress.TryParse(host, out var address) || address.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)
            return false;

        var bytes = address.GetAddressBytes();
        return bytes[0] == 10 ||
               (bytes[0] == 172 && bytes[1] is >= 16 and <= 31) ||
               (bytes[0] == 192 && bytes[1] == 168);
    }
}

public sealed class ChromeLabBridgeSecurityMiddleware
{
    private readonly RequestDelegate _next;

    public ChromeLabBridgeSecurityMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ChromeLabOptions options)
    {
        context.Response.Headers.CacheControl = "no-store";

        if (HttpMethods.IsOptions(context.Request.Method))
        {
            await _next(context);
            return;
        }

        var path = context.Request.Path;
        var remote = context.Connection.RemoteIpAddress;
        var isLoopback = remote is not null && IPAddress.IsLoopback(remote);
        var origin = context.Request.Headers.Origin.ToString();

        if (!ChromeLabBridgeSecurity.IsOriginAllowed(origin, options))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return;
        }

        if (ChromeLabBridgeSecurity.IsPairingPath(path))
        {
            if (!isLoopback || !ChromeLabBridgeSecurity.IsLocalPairingEnabled())
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return;
            }

            await _next(context);
            return;
        }

        if (ChromeLabBridgeSecurity.IsPublicPath(path) ||
            ChromeLabBridgeSecurity.IsExtensionWebSocketPath(path))
        {
            await _next(context);
            return;
        }

        if (ChromeLabBridgeSecurity.IsStealthWebSocketPath(path))
        {
            if (!options.StealthEnabled ||
                !ChromeLabBridgeSecurity.IsAuthorized(context, options, allowQueryToken: true))
            {
                context.Response.StatusCode = isLoopback
                    ? StatusCodes.Status401Unauthorized
                    : StatusCodes.Status404NotFound;
                return;
            }

            await _next(context);
            return;
        }

        if (!ChromeLabBridgeSecurity.IsAuthorized(context, options))
        {
            context.Response.StatusCode = isLoopback
                ? StatusCodes.Status401Unauthorized
                : StatusCodes.Status404NotFound;
            return;
        }

        await _next(context);
    }
}
