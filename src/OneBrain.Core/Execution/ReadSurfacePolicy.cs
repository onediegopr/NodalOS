using OneBrain.Core.Contracts;

namespace OneBrain.Core.Execution;

public sealed record ReadSurfaceDecision(
    bool Allowed,
    FailureKind? FailureKind,
    string Reason,
    string PatternUsed,
    string? Role);

public static class ReadSurfacePolicy
{
    public static ReadSurfaceDecision Decide(
        string? role,
        bool valueSupported,
        bool textSupported,
        bool invokeSupported = false,
        bool mutationPatternSupported = false)
    {
        var normalizedRole = string.IsNullOrWhiteSpace(role) ? null : role.Trim();

        if (mutationPatternSupported)
        {
            return Deny(
                "mutation pattern present; read surface fails closed",
                normalizedRole);
        }

        if (valueSupported)
        {
            return new ReadSurfaceDecision(
                Allowed: true,
                FailureKind: null,
                Reason: "ValuePattern read allowed",
                PatternUsed: "ValuePattern",
                Role: normalizedRole);
        }

        if (textSupported)
        {
            return new ReadSurfaceDecision(
                Allowed: true,
                FailureKind: null,
                Reason: "TextPattern read allowed",
                PatternUsed: "TextPattern",
                Role: normalizedRole);
        }

        if (invokeSupported)
        {
            return Deny(
                "InvokePattern alone is not a read surface",
                normalizedRole);
        }

        return Deny(
            "no read-only UIA pattern is available",
            normalizedRole);
    }

    private static ReadSurfaceDecision Deny(string reason, string? role) =>
        new(
            Allowed: false,
            FailureKind: FailureKind.PolicyDenied,
            Reason: reason,
            PatternUsed: "",
            Role: role);
}
