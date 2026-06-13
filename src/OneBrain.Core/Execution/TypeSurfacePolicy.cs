using OneBrain.Core.Contracts;

namespace OneBrain.Core.Execution;

public sealed record TypeSurfaceDecision(
    bool Allowed,
    FailureKind? FailureKind,
    string Reason,
    string PatternUsed,
    bool IsPasswordBlocked,
    string? Role,
    bool SupportsValuePattern);

public static class TypeSurfacePolicy
{
    public static TypeSurfaceDecision Decide(
        string? role,
        bool valueSupported,
        bool isPassword = false,
        bool invokeSupported = false,
        bool toggleSupported = false,
        bool selectionItemSupported = false,
        bool expandCollapseSupported = false,
        bool scrollSupported = false,
        bool isEnabled = true,
        bool isOffscreen = false)
    {
        var normalizedRole = string.IsNullOrWhiteSpace(role) ? "" : role.Trim();

        if (string.IsNullOrWhiteSpace(normalizedRole))
            return Deny("empty role denied fail-closed", normalizedRole, valueSupported);

        if (isPassword)
            return Deny("password fields are blocked for safe.type", normalizedRole, valueSupported, passwordBlocked: true);

        if (!isEnabled)
            return Deny("disabled target denied for safe.type", normalizedRole, valueSupported);

        if (isOffscreen)
            return Deny("offscreen target denied for safe.type", normalizedRole, valueSupported);

        if (toggleSupported || selectionItemSupported || expandCollapseSupported || scrollSupported)
            return Deny("mutating UIA pattern present; safe.type fails closed", normalizedRole, valueSupported);

        if (!IsRoleAllowed(normalizedRole))
            return Deny("role not in safe.type surface allowlist", normalizedRole, valueSupported);

        if (!valueSupported)
            return Deny("ValuePattern required for safe.type", normalizedRole, valueSupported);

        if (invokeSupported)
            return Deny("InvokePattern is not allowed for safe.type", normalizedRole, valueSupported);

        return new TypeSurfaceDecision(
            Allowed: true,
            FailureKind: null,
            Reason: "ValuePattern.SetValue allowed",
            PatternUsed: "ValuePattern.SetValue",
            IsPasswordBlocked: false,
            Role: normalizedRole,
            SupportsValuePattern: true);
    }

    private static bool IsRoleAllowed(string role) =>
        role.Equals("Edit", StringComparison.OrdinalIgnoreCase) ||
        role.Equals("Document", StringComparison.OrdinalIgnoreCase);

    private static TypeSurfaceDecision Deny(
        string reason,
        string? role,
        bool supportsValuePattern,
        bool passwordBlocked = false) =>
        new(
            Allowed: false,
            FailureKind: FailureKind.PolicyDenied,
            Reason: reason,
            PatternUsed: "",
            IsPasswordBlocked: passwordBlocked,
            Role: string.IsNullOrWhiteSpace(role) ? null : role,
            SupportsValuePattern: supportsValuePattern);
}
