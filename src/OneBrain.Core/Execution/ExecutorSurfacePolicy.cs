using OneBrain.Core.Contracts;

namespace OneBrain.Core.Execution;

public sealed record SurfaceDecision(
    bool Allowed,
    FailureKind? FailureKind,
    string Reason,
    string? Role,
    string RequiredPattern);

public static class ExecutorSurfacePolicy
{
    private const string InvokePattern = "InvokePattern";

    public static SurfaceDecision Decide(string? role, bool invokeSupported)
    {
        var normalizedRole = role?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedRole))
        {
            return Deny(
                role: null,
                reason: "empty role denied fail-closed");
        }

        if (!IsAllowlistedRole(normalizedRole))
        {
            return Deny(
                role: normalizedRole,
                reason: "role not in executor surface allowlist");
        }

        if (!invokeSupported)
        {
            return Deny(
                role: normalizedRole,
                reason: "role allowlisted but does not support InvokePattern");
        }

        return new SurfaceDecision(
            Allowed: true,
            FailureKind: null,
            Reason: "executor surface allowlisted",
            Role: normalizedRole,
            RequiredPattern: InvokePattern);
    }

    /// <summary>
    /// True when the role is in the executor surface allowlist, independent of pattern support.
    /// Fail-closed: empty/unknown roles return false.
    /// </summary>
    public static bool IsRoleAllowed(string? role)
    {
        var normalizedRole = role?.Trim();
        return !string.IsNullOrWhiteSpace(normalizedRole) && IsAllowlistedRole(normalizedRole);
    }

    private static bool IsAllowlistedRole(string role)
    {
        return role.Equals("Button", StringComparison.OrdinalIgnoreCase) ||
               role.Equals("Hyperlink", StringComparison.OrdinalIgnoreCase) ||
               role.Equals("MenuItem", StringComparison.OrdinalIgnoreCase);
    }

    private static SurfaceDecision Deny(string? role, string reason)
    {
        return new SurfaceDecision(
            Allowed: false,
            FailureKind: FailureKind.PolicyDenied,
            Reason: reason,
            Role: role,
            RequiredPattern: InvokePattern);
    }
}
