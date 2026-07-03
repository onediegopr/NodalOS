using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace OneBrain.Core.Approval;

public enum DurableAuditTrailAppendOnlyMinimalDecision
{
    Appended,
    Rejected
}

public enum DurableAuditTrailAppendOnlyMinimalRejectReason
{
    Disabled,
    EmptyStorageRoot,
    StorageRootOutsideLocalTestBoundary,
    UnexpectedEventKind,
    MissingActorReference,
    MissingApprovalReference,
    MissingEvidenceReference,
    RawPayloadRejected,
    SecretLikeContentRejected,
    ExistingLedgerIntegrityFailed
}

public sealed record DurableAuditTrailAppendOnlyMinimalPolicy(
    bool Enabled,
    string StorageRoot,
    bool AllowLocalTestStorageOnly = true);

public sealed record DurableAuditTrailAppendOnlyMinimalRequest(
    string EventKind,
    string ActorReference,
    string ApprovalReference,
    IReadOnlyList<string> EvidenceReferences,
    IReadOnlyDictionary<string, string>? Metadata = null,
    string? RawPayload = null);

public sealed record DurableAuditTrailAppendOnlyMinimalEntry(
    long SequenceNumber,
    string EventId,
    string EventKind,
    DateTimeOffset CreatedAtUtc,
    string ActorReference,
    string ApprovalReference,
    IReadOnlyList<string> EvidenceReferences,
    IReadOnlyDictionary<string, string> Metadata,
    string PreviousHash,
    string EventHash);

public sealed record DurableAuditTrailAppendOnlyMinimalResult(
    DurableAuditTrailAppendOnlyMinimalDecision Decision,
    IReadOnlyList<DurableAuditTrailAppendOnlyMinimalRejectReason> RejectReasons,
    DurableAuditTrailAppendOnlyMinimalEntry? Entry,
    string? LedgerFile,
    int AppendWriteCount,
    int PersistedEventCount,
    bool ProductActionAllowed,
    bool NetworkAllowed,
    bool DbMigrationAllowed,
    bool CommandHandlerRegistered,
    bool ReleaseCommercialReady);

public sealed record DurableAuditTrailAppendOnlyMinimalVerification(
    bool Valid,
    int EntryCount,
    long LastSequenceNumber,
    string LastHash,
    IReadOnlyList<string> Errors);

public sealed class DurableAuditTrailAppendOnlyMinimal
{
    public const string SupportedEventKind = "approval.reviewed";
    private const string LedgerFileName = "durable-audit-trail.append-only.jsonl";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public DurableAuditTrailAppendOnlyMinimalResult Append(
        DurableAuditTrailAppendOnlyMinimalPolicy policy,
        DurableAuditTrailAppendOnlyMinimalRequest request)
    {
        var rejection = ValidateRequest(policy, request);
        if (rejection.Count > 0)
        {
            return Rejected(rejection, LedgerPathOrNull(policy));
        }

        Directory.CreateDirectory(policy.StorageRoot);
        var ledgerFile = Path.Combine(policy.StorageRoot, LedgerFileName);
        var existing = ReadEntries(ledgerFile);
        var verification = Verify(existing);
        if (!verification.Valid)
        {
            return Rejected(
                [DurableAuditTrailAppendOnlyMinimalRejectReason.ExistingLedgerIntegrityFailed],
                ledgerFile);
        }

        var sequence = existing.Count + 1;
        var previousHash = existing.LastOrDefault()?.EventHash ?? "genesis";
        var entryWithoutHash = new DurableAuditTrailAppendOnlyMinimalEntry(
            SequenceNumber: sequence,
            EventId: $"audit-trail-{Guid.NewGuid():N}",
            EventKind: request.EventKind,
            CreatedAtUtc: DateTimeOffset.UtcNow,
            ActorReference: Redact(request.ActorReference),
            ApprovalReference: Redact(request.ApprovalReference),
            EvidenceReferences: request.EvidenceReferences.Select(Redact).ToArray(),
            Metadata: RedactMetadata(request.Metadata ?? new Dictionary<string, string>()),
            PreviousHash: previousHash,
            EventHash: string.Empty);
        var entry = entryWithoutHash with
        {
            EventHash = ComputeHash(entryWithoutHash)
        };

        File.AppendAllText(ledgerFile, JsonSerializer.Serialize(entry, JsonOptions) + Environment.NewLine);

        return new DurableAuditTrailAppendOnlyMinimalResult(
            Decision: DurableAuditTrailAppendOnlyMinimalDecision.Appended,
            RejectReasons: [],
            Entry: entry,
            LedgerFile: ledgerFile,
            AppendWriteCount: 1,
            PersistedEventCount: 1,
            ProductActionAllowed: false,
            NetworkAllowed: false,
            DbMigrationAllowed: false,
            CommandHandlerRegistered: false,
            ReleaseCommercialReady: false);
    }

    public DurableAuditTrailAppendOnlyMinimalVerification VerifyFile(string ledgerFile)
    {
        if (!File.Exists(ledgerFile))
        {
            return new DurableAuditTrailAppendOnlyMinimalVerification(
                Valid: true,
                EntryCount: 0,
                LastSequenceNumber: 0,
                LastHash: "genesis",
                Errors: []);
        }

        return Verify(ReadEntries(ledgerFile));
    }

    private static List<DurableAuditTrailAppendOnlyMinimalRejectReason> ValidateRequest(
        DurableAuditTrailAppendOnlyMinimalPolicy policy,
        DurableAuditTrailAppendOnlyMinimalRequest request)
    {
        var reasons = new List<DurableAuditTrailAppendOnlyMinimalRejectReason>();
        if (!policy.Enabled)
        {
            reasons.Add(DurableAuditTrailAppendOnlyMinimalRejectReason.Disabled);
        }

        if (string.IsNullOrWhiteSpace(policy.StorageRoot))
        {
            reasons.Add(DurableAuditTrailAppendOnlyMinimalRejectReason.EmptyStorageRoot);
        }
        else if (policy.AllowLocalTestStorageOnly && !IsUnderTempPath(policy.StorageRoot))
        {
            reasons.Add(DurableAuditTrailAppendOnlyMinimalRejectReason.StorageRootOutsideLocalTestBoundary);
        }

        if (!string.Equals(request.EventKind, SupportedEventKind, StringComparison.Ordinal))
        {
            reasons.Add(DurableAuditTrailAppendOnlyMinimalRejectReason.UnexpectedEventKind);
        }

        if (string.IsNullOrWhiteSpace(request.ActorReference))
        {
            reasons.Add(DurableAuditTrailAppendOnlyMinimalRejectReason.MissingActorReference);
        }

        if (string.IsNullOrWhiteSpace(request.ApprovalReference))
        {
            reasons.Add(DurableAuditTrailAppendOnlyMinimalRejectReason.MissingApprovalReference);
        }

        if (request.EvidenceReferences.Count == 0 || request.EvidenceReferences.Any(string.IsNullOrWhiteSpace))
        {
            reasons.Add(DurableAuditTrailAppendOnlyMinimalRejectReason.MissingEvidenceReference);
        }

        if (!string.IsNullOrWhiteSpace(request.RawPayload))
        {
            reasons.Add(DurableAuditTrailAppendOnlyMinimalRejectReason.RawPayloadRejected);
        }

        if (ContainsSecretLikeContent(request.ActorReference)
            || ContainsSecretLikeContent(request.ApprovalReference)
            || request.EvidenceReferences.Any(ContainsSecretLikeContent)
            || (request.Metadata?.Any(pair => ContainsSecretLikeContent(pair.Key) || ContainsSecretLikeContent(pair.Value)) ?? false))
        {
            reasons.Add(DurableAuditTrailAppendOnlyMinimalRejectReason.SecretLikeContentRejected);
        }

        return reasons;
    }

    private static DurableAuditTrailAppendOnlyMinimalResult Rejected(
        IReadOnlyList<DurableAuditTrailAppendOnlyMinimalRejectReason> reasons,
        string? ledgerFile) =>
        new(
            Decision: DurableAuditTrailAppendOnlyMinimalDecision.Rejected,
            RejectReasons: reasons,
            Entry: null,
            LedgerFile: ledgerFile,
            AppendWriteCount: 0,
            PersistedEventCount: 0,
            ProductActionAllowed: false,
            NetworkAllowed: false,
            DbMigrationAllowed: false,
            CommandHandlerRegistered: false,
            ReleaseCommercialReady: false);

    private static string? LedgerPathOrNull(DurableAuditTrailAppendOnlyMinimalPolicy policy) =>
        string.IsNullOrWhiteSpace(policy.StorageRoot)
            ? null
            : Path.Combine(policy.StorageRoot, LedgerFileName);

    private static bool IsUnderTempPath(string path)
    {
        var fullPath = Path.GetFullPath(path);
        var tempPath = Path.GetFullPath(Path.GetTempPath());
        return fullPath.StartsWith(tempPath, StringComparison.OrdinalIgnoreCase);
    }

    private static IReadOnlyList<DurableAuditTrailAppendOnlyMinimalEntry> ReadEntries(string ledgerFile)
    {
        if (!File.Exists(ledgerFile))
        {
            return [];
        }

        var entries = new List<DurableAuditTrailAppendOnlyMinimalEntry>();
        foreach (var line in File.ReadLines(ledgerFile))
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var entry = JsonSerializer.Deserialize<DurableAuditTrailAppendOnlyMinimalEntry>(line, JsonOptions);
            if (entry is not null)
            {
                entries.Add(entry);
            }
        }

        return entries;
    }

    private static DurableAuditTrailAppendOnlyMinimalVerification Verify(
        IReadOnlyList<DurableAuditTrailAppendOnlyMinimalEntry> entries)
    {
        var errors = new List<string>();
        var previousHash = "genesis";
        long expectedSequence = 1;
        foreach (var entry in entries)
        {
            if (entry.SequenceNumber != expectedSequence)
            {
                errors.Add($"sequence_mismatch:{entry.SequenceNumber}");
            }

            if (!string.Equals(entry.PreviousHash, previousHash, StringComparison.Ordinal))
            {
                errors.Add($"previous_hash_mismatch:{entry.SequenceNumber}");
            }

            var expectedHash = ComputeHash(entry with { EventHash = string.Empty });
            if (!FixedTimeEquals(entry.EventHash, expectedHash))
            {
                errors.Add($"event_hash_mismatch:{entry.SequenceNumber}");
            }

            previousHash = entry.EventHash;
            expectedSequence++;
        }

        var last = entries.LastOrDefault();
        return new DurableAuditTrailAppendOnlyMinimalVerification(
            Valid: errors.Count == 0,
            EntryCount: entries.Count,
            LastSequenceNumber: last?.SequenceNumber ?? 0,
            LastHash: last?.EventHash ?? "genesis",
            Errors: errors);
    }

    private static string ComputeHash(DurableAuditTrailAppendOnlyMinimalEntry entry)
    {
        var material = JsonSerializer.Serialize(entry, JsonOptions);
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(material));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static bool FixedTimeEquals(string left, string right)
    {
        var leftBytes = Encoding.UTF8.GetBytes(left);
        var rightBytes = Encoding.UTF8.GetBytes(right);
        return leftBytes.Length == rightBytes.Length
            && CryptographicOperations.FixedTimeEquals(leftBytes, rightBytes);
    }

    private static IReadOnlyDictionary<string, string> RedactMetadata(IReadOnlyDictionary<string, string> metadata)
    {
        var redacted = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var pair in metadata.OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            redacted[Redact(pair.Key)] = Redact(pair.Value);
        }

        return redacted;
    }

    private static string Redact(string value) =>
        ContainsSecretLikeContent(value) ? "[REDACTED]" : value;

    private static bool ContainsSecretLikeContent(string value)
    {
        var lowered = value.ToLowerInvariant();
        return lowered.Contains("password=", StringComparison.Ordinal)
            || lowered.Contains("token=", StringComparison.Ordinal)
            || lowered.Contains("secret=", StringComparison.Ordinal)
            || lowered.Contains("authorization:", StringComparison.Ordinal)
            || lowered.Contains("cookie:", StringComparison.Ordinal)
            || lowered.Contains("begin private key", StringComparison.Ordinal);
    }
}
