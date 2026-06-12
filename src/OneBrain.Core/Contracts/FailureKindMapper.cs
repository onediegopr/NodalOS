namespace OneBrain.Core.Contracts;

public static class FailureKindMapper
{
    public static FailureKind FromMessage(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return FailureKind.Unverified;

        var normalized = message.Trim().ToLowerInvariant();

        if (normalized.Contains("not found", StringComparison.Ordinal) ||
            normalized.Contains("no encontrado", StringComparison.Ordinal))
            return FailureKind.NotFound;

        if (normalized.Contains("timeout", StringComparison.Ordinal) ||
            normalized.Contains("timed out", StringComparison.Ordinal))
            return FailureKind.Timeout;

        if (normalized.Contains("blocked by safety", StringComparison.Ordinal) ||
            normalized.Contains("policy", StringComparison.Ordinal))
            return FailureKind.PolicyDenied;

        if (normalized.Contains("cancelled", StringComparison.Ordinal) ||
            normalized.Contains("cancelado", StringComparison.Ordinal) ||
            normalized.Contains("cancel", StringComparison.Ordinal))
            return FailureKind.Cancelled;

        if (normalized.Contains("ambiguous", StringComparison.Ordinal) ||
            normalized.Contains("ambiguo", StringComparison.Ordinal))
            return FailureKind.Ambiguous;

        return FailureKind.Unverified;
    }
}
