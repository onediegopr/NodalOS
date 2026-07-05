using System.Text.RegularExpressions;

namespace OneBrain.Core.Approval;

public sealed record ProductLedgerPathLocalOnlyMetadataGuardResult(
    bool Allowed,
    IReadOnlyDictionary<string, string> SafeMetadata,
    IReadOnlyList<ProductLedgerPathLocalOnlyBlocker> Blockers,
    bool RedactionApplied,
    bool RetentionBounded);

public static class ProductLedgerPathLocalOnlyMetadataGuard
{
    public const int MaxInputMetadataFields = 12;
    public const int MaxPersistedMetadataFields = 16;
    public const int MaxMetadataValueLength = 128;
    public const int MaxLedgerEntries = 1024;
    public const long MaxLedgerBytes = 1_048_576;

    private static readonly Regex MetadataKeyPattern = new(
        @"^[a-z0-9][a-z0-9._-]{1,63}$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex EmailLikePattern = new(
        @"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}\b",
        RegexOptions.Compiled);

    private static readonly Regex SecretAssignmentLikePattern = new(
        @"\b(password|token|secret|api[\s_-]?key|client[\s_-]?secret)\s*[:=]",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex BearerLikePattern = new(
        @"\bbearer\s+[A-Za-z0-9._~+/=-]{8,}\b",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex WindowsAbsolutePathPattern = new(
        @"\b[A-Za-z]:\\[^:*?""<>|\r\n]+",
        RegexOptions.Compiled);

    private static readonly string[] SensitiveKeyMarkers =
    [
        "secret",
        "password",
        "api_key",
        "apikey",
        "api-key",
        "token",
        "bearer",
        "client_secret",
        "private_key",
        "email"
    ];

    private static readonly string[] RetentionOverclaimMarkers =
    [
        "unbounded",
        "infinite",
        "forever",
        "compliance",
        "custody",
        "worm",
        "kms",
        "external trust",
        "cloud retention",
        "legal deletion"
    ];

    public static ProductLedgerPathLocalOnlyMetadataGuardResult Evaluate(
        IReadOnlyDictionary<string, string>? metadata,
        int? existingEntryCount = null,
        long? existingLedgerBytes = null)
    {
        var blockers = new List<ProductLedgerPathLocalOnlyBlocker>();
        var safe = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var redactionApplied = false;
        var redactedOrdinal = 1;

        if (metadata is null || metadata.Count == 0)
        {
            blockers.Add(ProductLedgerPathLocalOnlyBlocker.UnsafeEvidenceMetadata);
        }
        else if (metadata.Count > MaxInputMetadataFields)
        {
            blockers.Add(ProductLedgerPathLocalOnlyBlocker.RetentionLimitExceeded);
        }

        if (existingEntryCount >= MaxLedgerEntries || existingLedgerBytes >= MaxLedgerBytes)
        {
            blockers.Add(ProductLedgerPathLocalOnlyBlocker.RetentionLimitExceeded);
        }

        if (blockers.Count == 0)
        {
            foreach (var pair in metadata!)
            {
                if (IsMalformed(pair.Key, pair.Value))
                {
                    blockers.Add(ProductLedgerPathLocalOnlyBlocker.UnsafeEvidenceMetadata);
                    continue;
                }

                if (IsRawPayloadClaim(pair.Key) || ContainsPathLikeContent(pair.Value))
                {
                    blockers.Add(ProductLedgerPathLocalOnlyBlocker.UnsafeEvidenceMetadata);
                    continue;
                }

                if (ContainsRetentionOverclaim(pair.Key) || ContainsRetentionOverclaim(pair.Value))
                {
                    blockers.Add(ProductLedgerPathLocalOnlyBlocker.RetentionLimitExceeded);
                    continue;
                }

                if (IsSensitiveKey(pair.Key) || ContainsSensitiveContent(pair.Value))
                {
                    safe[$"redaction.field{redactedOrdinal:00}"] = "redacted-sensitive";
                    redactedOrdinal++;
                    redactionApplied = true;
                    continue;
                }

                if (pair.Value.Length > MaxMetadataValueLength)
                {
                    blockers.Add(ProductLedgerPathLocalOnlyBlocker.RetentionLimitExceeded);
                    continue;
                }

                safe[pair.Key] = pair.Value;
            }
        }

        if (blockers.Count == 0)
        {
            safe["redaction.applied"] = redactionApplied ? "true" : "false";
            safe["retention.mode"] = "bounded-local";
            safe["retention.max-fields"] = MaxPersistedMetadataFields.ToString();
            safe["retention.max-value-chars"] = MaxMetadataValueLength.ToString();

            if (safe.Count > MaxPersistedMetadataFields || safe.Values.Sum(value => value.Length) > 768)
            {
                blockers.Add(ProductLedgerPathLocalOnlyBlocker.RetentionLimitExceeded);
            }
        }

        var distinct = blockers.Distinct().OrderBy(blocker => blocker.ToString(), StringComparer.Ordinal).ToArray();
        return new ProductLedgerPathLocalOnlyMetadataGuardResult(
            Allowed: distinct.Length == 0,
            SafeMetadata: distinct.Length == 0
                ? safe.OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase)
                : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
            Blockers: distinct,
            RedactionApplied: redactionApplied,
            RetentionBounded: distinct.Length == 0);
    }

    private static bool IsMalformed(string? key, string? value) =>
        string.IsNullOrWhiteSpace(key)
        || string.IsNullOrWhiteSpace(value)
        || !MetadataKeyPattern.IsMatch(key)
        || value.Contains("..", StringComparison.Ordinal);

    private static bool IsRawPayloadClaim(string key) =>
        key.Contains("raw", StringComparison.OrdinalIgnoreCase)
        || key.Contains("payload", StringComparison.OrdinalIgnoreCase)
        || key.Contains("content", StringComparison.OrdinalIgnoreCase);

    private static bool IsSensitiveKey(string key) =>
        SensitiveKeyMarkers.Any(marker => key.Contains(marker, StringComparison.OrdinalIgnoreCase));

    private static bool ContainsSensitiveContent(string value) =>
        SensitiveKeyMarkers.Any(marker => value.Contains(marker, StringComparison.OrdinalIgnoreCase))
        || SecretAssignmentLikePattern.IsMatch(value)
        || BearerLikePattern.IsMatch(value)
        || EmailLikePattern.IsMatch(value)
        || ContainsConnectionStringLikeContent(value)
        || IsHighEntropyLike(value);

    private static bool ContainsConnectionStringLikeContent(string value)
    {
        var lowered = value.ToLowerInvariant();
        return lowered.Contains("defaultendpointsprotocol=", StringComparison.Ordinal)
            || lowered.Contains("accountkey=", StringComparison.Ordinal)
            || lowered.Contains("sharedaccesskey=", StringComparison.Ordinal)
            || (lowered.Contains("server=", StringComparison.Ordinal)
                && lowered.Contains("user id=", StringComparison.Ordinal))
            || (lowered.Contains("server=", StringComparison.Ordinal)
                && lowered.Contains("password=", StringComparison.Ordinal));
    }

    private static bool IsHighEntropyLike(string value)
    {
        if (value.Length < 32 || value.Any(char.IsWhiteSpace))
        {
            return false;
        }

        var accepted = value.Count(ch => char.IsLetterOrDigit(ch) || ch is '_' or '-' or '+' or '/' or '=');
        if (accepted != value.Length)
        {
            return false;
        }

        var classes = 0;
        if (value.Any(char.IsLower)) classes++;
        if (value.Any(char.IsUpper)) classes++;
        if (value.Any(char.IsDigit)) classes++;
        if (value.Any(ch => !char.IsLetterOrDigit(ch))) classes++;
        return classes >= 3;
    }

    private static bool ContainsPathLikeContent(string value) =>
        WindowsAbsolutePathPattern.IsMatch(value)
        || value.TrimStart().StartsWith(@"\\", StringComparison.Ordinal)
        || value.Contains('\\', StringComparison.Ordinal);

    private static bool ContainsRetentionOverclaim(string value) =>
        RetentionOverclaimMarkers.Any(marker => value.Contains(marker, StringComparison.OrdinalIgnoreCase));
}
