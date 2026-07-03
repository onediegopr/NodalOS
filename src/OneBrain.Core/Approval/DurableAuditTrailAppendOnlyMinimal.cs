using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

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
    ExistingLedgerIntegrityFailed,
    MalformedMetadata,
    MissingStage2TestOnlyGate,
    Stage2FeatureFlagDisabled,
    Stage2ProductFeatureFlagRejected,
    MissingRedactionBeforePersistenceProof,
    RedactionBeforePersistenceRejected,
    RedactionEvidenceMismatch,
    ProductLedgerPathRejected
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

public sealed record DurableAuditTrailAppendOnlyMinimalRedactionProof(
    string PolicyReference,
    bool FieldClassificationCompleted,
    bool RedactionCompleted,
    bool CompletedBeforePersistence,
    bool Succeeded);

public sealed record DurableAuditTrailAppendOnlyMinimalStage2TestOnlyGate(
    bool ExplicitTestFixture,
    string? FeatureFlagValue,
    DurableAuditTrailAppendOnlyMinimalRedactionProof? RedactionProof,
    RedactionBeforePersistenceResult? RedactionResult = null);

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
    private static readonly ConcurrentDictionary<string, object> LedgerLocks = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Regex JwtLikePattern = new(
        @"\b[A-Za-z0-9_-]{10,}\.[A-Za-z0-9_-]{10,}\.[A-Za-z0-9_-]{10,}\b",
        RegexOptions.Compiled);
    private static readonly Regex OpenAiKeyLikePattern = new(
        @"\bsk-(proj-)?[A-Za-z0-9_-]{8,}\b",
        RegexOptions.Compiled);
    private static readonly Regex EmailLikePattern = new(
        @"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}\b",
        RegexOptions.Compiled);
    private static readonly Regex WindowsAbsolutePathPattern = new(
        @"\b[A-Za-z]:\\[^:*?""<>|\r\n]+",
        RegexOptions.Compiled);

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
        lock (GetLedgerLock(ledgerFile))
        {
            var existing = ReadEntries(ledgerFile);
            if (existing.Errors.Count > 0)
            {
                return Rejected(
                    [DurableAuditTrailAppendOnlyMinimalRejectReason.ExistingLedgerIntegrityFailed],
                    ledgerFile);
            }

            var verification = Verify(existing.Entries);
            if (!verification.Valid)
            {
                return Rejected(
                    [DurableAuditTrailAppendOnlyMinimalRejectReason.ExistingLedgerIntegrityFailed],
                    ledgerFile);
            }

            var sequence = existing.Entries.Count + 1;
            var previousHash = existing.Entries.LastOrDefault()?.EventHash ?? "genesis";
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
    }

    public DurableAuditTrailAppendOnlyMinimalResult AppendStage2TestOnly(
        DurableAuditTrailAppendOnlyMinimalPolicy policy,
        DurableAuditTrailAppendOnlyMinimalRequest request,
        DurableAuditTrailAppendOnlyMinimalStage2TestOnlyGate? gate)
    {
        var rejection = ValidateStage2TestOnlyGate(policy, request, gate);
        if (rejection.Count > 0)
        {
            return Rejected(rejection, LedgerPathOrNull(policy));
        }

        var safeRequest = gate!.RedactionResult!.SafeRequest!;
        var dataRejection = ValidateStage2SensitiveData(safeRequest);
        if (dataRejection.Count > 0)
        {
            return Rejected(dataRejection, LedgerPathOrNull(policy));
        }

        return Append(policy, safeRequest);
    }

    public DurableAuditTrailAppendOnlyMinimalVerification VerifyFile(string ledgerFile)
    {
        if (!IsUnderTempPath(ledgerFile))
        {
            return new DurableAuditTrailAppendOnlyMinimalVerification(
                Valid: false,
                EntryCount: 0,
                LastSequenceNumber: 0,
                LastHash: "genesis",
                Errors: ["ledger_outside_local_test_boundary"]);
        }

        if (!File.Exists(ledgerFile))
        {
            return new DurableAuditTrailAppendOnlyMinimalVerification(
                Valid: true,
                EntryCount: 0,
                LastSequenceNumber: 0,
                LastHash: "genesis",
                Errors: []);
        }

        var existing = ReadEntries(ledgerFile);
        if (existing.Errors.Count > 0)
        {
            return new DurableAuditTrailAppendOnlyMinimalVerification(
                Valid: false,
                EntryCount: existing.Entries.Count,
                LastSequenceNumber: existing.Entries.LastOrDefault()?.SequenceNumber ?? 0,
                LastHash: existing.Entries.LastOrDefault()?.EventHash ?? "genesis",
                Errors: existing.Errors);
        }

        return Verify(existing.Entries);
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

        if (request.EvidenceReferences is null
            || request.EvidenceReferences.Count == 0
            || request.EvidenceReferences.Any(string.IsNullOrWhiteSpace))
        {
            reasons.Add(DurableAuditTrailAppendOnlyMinimalRejectReason.MissingEvidenceReference);
        }

        if (request.Metadata is not null
            && request.Metadata.Any(pair => string.IsNullOrWhiteSpace(pair.Key) || pair.Value is null))
        {
            reasons.Add(DurableAuditTrailAppendOnlyMinimalRejectReason.MalformedMetadata);
        }

        if (!string.IsNullOrWhiteSpace(request.RawPayload))
        {
            reasons.Add(DurableAuditTrailAppendOnlyMinimalRejectReason.RawPayloadRejected);
        }

        if (ContainsSecretLikeContent(request.ActorReference)
            || ContainsSecretLikeContent(request.ApprovalReference)
            || (request.EvidenceReferences?.Any(ContainsSecretLikeContent) ?? false)
            || (request.Metadata?.Any(pair => ContainsSecretLikeContent(pair.Key) || ContainsSecretLikeContent(pair.Value)) ?? false))
        {
            reasons.Add(DurableAuditTrailAppendOnlyMinimalRejectReason.SecretLikeContentRejected);
        }

        return reasons;
    }

    private static List<DurableAuditTrailAppendOnlyMinimalRejectReason> ValidateStage2TestOnlyGate(
        DurableAuditTrailAppendOnlyMinimalPolicy policy,
        DurableAuditTrailAppendOnlyMinimalRequest request,
        DurableAuditTrailAppendOnlyMinimalStage2TestOnlyGate? gate)
    {
        var reasons = new List<DurableAuditTrailAppendOnlyMinimalRejectReason>();
        if (gate is null)
        {
            reasons.Add(DurableAuditTrailAppendOnlyMinimalRejectReason.MissingStage2TestOnlyGate);
            return reasons;
        }

        var featureFlag = new DurableAuditTrailStage2RuntimeFeatureFlag().Evaluate(
            gate.ExplicitTestFixture,
            gate.FeatureFlagValue);
        if (featureFlag.Decision != DurableAuditTrailStage2RuntimeFeatureFlagDecision.Allowed)
        {
            if (featureFlag.RejectReasons.Contains(
                    DurableAuditTrailStage2RuntimeFeatureFlagRejectReason.ProductRuntimeScopeRejected))
            {
                reasons.Add(DurableAuditTrailAppendOnlyMinimalRejectReason.Stage2ProductFeatureFlagRejected);
            }

            reasons.Add(DurableAuditTrailAppendOnlyMinimalRejectReason.Stage2FeatureFlagDisabled);
        }

        if (!string.IsNullOrWhiteSpace(policy.StorageRoot) && IsProductLedgerPath(policy.StorageRoot))
        {
            reasons.Add(DurableAuditTrailAppendOnlyMinimalRejectReason.ProductLedgerPathRejected);
        }

        if (!HasRedactionBeforePersistenceProof(gate.RedactionProof))
        {
            reasons.Add(DurableAuditTrailAppendOnlyMinimalRejectReason.MissingRedactionBeforePersistenceProof);
        }

        reasons.AddRange(ValidateRedactionBeforePersistenceResult(request, gate.RedactionResult));

        return reasons;
    }

    private static bool HasRedactionBeforePersistenceProof(DurableAuditTrailAppendOnlyMinimalRedactionProof? proof) =>
        proof is not null
        && !string.IsNullOrWhiteSpace(proof.PolicyReference)
        && proof.FieldClassificationCompleted
        && proof.RedactionCompleted
        && proof.CompletedBeforePersistence
        && proof.Succeeded;

    private static IReadOnlyList<DurableAuditTrailAppendOnlyMinimalRejectReason> ValidateRedactionBeforePersistenceResult(
        DurableAuditTrailAppendOnlyMinimalRequest request,
        RedactionBeforePersistenceResult? result)
    {
        if (result is null
            || result.Decision != RedactionBeforePersistenceDecision.Allowed
            || !result.Succeeded
            || result.Reasons is null
            || result.SafeRequest is null
            || result.Evidence is null
            || result.Evidence.Decision != RedactionBeforePersistenceDecision.Allowed
            || !string.Equals(result.Evidence.PolicyId, RedactionBeforePersistencePolicy.TestOnlyPolicyId, StringComparison.Ordinal)
            || !string.Equals(result.Evidence.PolicyVersion, RedactionBeforePersistencePolicy.TestOnlyPolicyVersion, StringComparison.Ordinal)
            || !result.Evidence.CompletedBeforePersistence
            || result.Evidence.ContainsRawValues)
        {
            var reasons = new List<DurableAuditTrailAppendOnlyMinimalRejectReason>
            {
                DurableAuditTrailAppendOnlyMinimalRejectReason.RedactionBeforePersistenceRejected
            };
            if (result?.Reasons?.Any(reason =>
                    reason is RedactionBeforePersistenceReason.SecretLikeContentRejected
                        or RedactionBeforePersistenceReason.PiiLikeContentRejected
                        or RedactionBeforePersistenceReason.PathLikeContentRejected) == true)
            {
                reasons.Add(DurableAuditTrailAppendOnlyMinimalRejectReason.SecretLikeContentRejected);
            }

            return reasons;
        }

        var expectedHash = RedactionBeforePersistenceService.ComputeCandidateHash(request);
        if (string.IsNullOrWhiteSpace(result.Evidence.CandidateHash)
            || !FixedTimeEquals(result.Evidence.CandidateHash, expectedHash)
            || !FixedTimeEquals(RedactionBeforePersistenceService.ComputeCandidateHash(result.SafeRequest), expectedHash))
        {
            return [DurableAuditTrailAppendOnlyMinimalRejectReason.RedactionEvidenceMismatch];
        }

        return [];
    }

    private static List<DurableAuditTrailAppendOnlyMinimalRejectReason> ValidateStage2SensitiveData(
        DurableAuditTrailAppendOnlyMinimalRequest request)
    {
        var values = new[]
            {
                request.ActorReference,
                request.ApprovalReference,
                request.RawPayload
            }
            .Concat(request.EvidenceReferences ?? [])
            .Concat(request.Metadata?.Keys ?? [])
            .Concat(request.Metadata?.Values ?? []);

        return values.Any(ContainsStage2SensitiveContent)
            ? [DurableAuditTrailAppendOnlyMinimalRejectReason.SecretLikeContentRejected]
            : [];
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
        var tempPath = EnsureTrailingDirectorySeparator(Path.GetFullPath(Path.GetTempPath()));
        return fullPath.StartsWith(tempPath, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsProductLedgerPath(string path)
    {
        var fullPath = Path.GetFullPath(path);
        var fragments = new[]
        {
            "product-ledger",
            "product_ledger",
            "prod-ledger",
            "production-ledger",
            "commercial-ledger"
        };

        return fragments.Any(fragment => fullPath.Contains(fragment, StringComparison.OrdinalIgnoreCase));
    }

    private static string EnsureTrailingDirectorySeparator(string path) =>
        path.EndsWith(Path.DirectorySeparatorChar)
            || path.EndsWith(Path.AltDirectorySeparatorChar)
                ? path
                : path + Path.DirectorySeparatorChar;

    private sealed record LedgerReadResult(
        IReadOnlyList<DurableAuditTrailAppendOnlyMinimalEntry> Entries,
        IReadOnlyList<string> Errors);

    private static LedgerReadResult ReadEntries(string ledgerFile)
    {
        if (!File.Exists(ledgerFile))
        {
            return new LedgerReadResult([], []);
        }

        var entries = new List<DurableAuditTrailAppendOnlyMinimalEntry>();
        var errors = new List<string>();
        var lineNumber = 0;
        foreach (var line in File.ReadLines(ledgerFile))
        {
            lineNumber++;
            if (line.Length == 0)
            {
                errors.Add($"empty_line:{lineNumber}");
                continue;
            }

            if (string.IsNullOrWhiteSpace(line))
            {
                errors.Add($"whitespace_line:{lineNumber}");
                continue;
            }

            try
            {
                var entry = JsonSerializer.Deserialize<DurableAuditTrailAppendOnlyMinimalEntry>(line, JsonOptions);
                if (entry is null)
                {
                    errors.Add($"malformed_json_line:{lineNumber}");
                    continue;
                }

                errors.AddRange(ValidateEntryShape(entry, lineNumber));

                entries.Add(entry);
            }
            catch (JsonException)
            {
                errors.Add($"malformed_json_line:{lineNumber}");
            }
            catch (NotSupportedException)
            {
                errors.Add($"unsupported_json_line:{lineNumber}");
            }
        }

        return new LedgerReadResult(entries, errors);
    }

    private static IReadOnlyList<string> ValidateEntryShape(
        DurableAuditTrailAppendOnlyMinimalEntry entry,
        int lineNumber)
    {
        var errors = new List<string>();
        if (entry.SequenceNumber <= 0)
        {
            errors.Add($"invalid_sequence_line:{lineNumber}");
        }

        AddRequiredStringErrors(errors, entry.EventId, "event_id", lineNumber);
        AddRequiredStringErrors(errors, entry.EventKind, "event_kind", lineNumber);
        AddRequiredStringErrors(errors, entry.ActorReference, "actor_reference", lineNumber);
        AddRequiredStringErrors(errors, entry.ApprovalReference, "approval_reference", lineNumber);
        AddRequiredStringErrors(errors, entry.PreviousHash, "previous_hash", lineNumber);
        AddRequiredStringErrors(errors, entry.EventHash, "event_hash", lineNumber);
        if (!string.IsNullOrWhiteSpace(entry.EventKind)
            && !string.Equals(entry.EventKind, SupportedEventKind, StringComparison.Ordinal))
        {
            errors.Add($"unexpected_event_kind_line:{lineNumber}");
        }

        if (entry.CreatedAtUtc == default)
        {
            errors.Add($"missing_created_at_line:{lineNumber}");
        }

        if (entry.EvidenceReferences is null || entry.EvidenceReferences.Count == 0)
        {
            errors.Add($"missing_evidence_references_line:{lineNumber}");
        }
        else if (entry.EvidenceReferences.Any(string.IsNullOrWhiteSpace))
        {
            errors.Add($"invalid_evidence_reference_line:{lineNumber}");
        }

        if (entry.Metadata is null)
        {
            errors.Add($"missing_metadata_line:{lineNumber}");
        }
        else if (entry.Metadata.Any(pair => string.IsNullOrWhiteSpace(pair.Key) || pair.Value is null))
        {
            errors.Add($"invalid_metadata_line:{lineNumber}");
        }

        if (errors.Count > 0)
        {
            errors.Insert(0, $"invalid_entry_shape_line:{lineNumber}");
        }

        return errors;
    }

    private static void AddRequiredStringErrors(List<string> errors, string? value, string fieldName, int lineNumber)
    {
        if (value is null)
        {
            errors.Add($"missing_{fieldName}_line:{lineNumber}");
            return;
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add($"empty_{fieldName}_line:{lineNumber}");
        }
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

    private static object GetLedgerLock(string ledgerFile) =>
        LedgerLocks.GetOrAdd(Path.GetFullPath(ledgerFile), _ => new object());

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

    private static bool ContainsSecretLikeContent(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return false;
        }

        var lowered = value.ToLowerInvariant();
        return lowered.Contains("password=", StringComparison.Ordinal)
            || lowered.Contains("token=", StringComparison.Ordinal)
            || lowered.Contains("secret=", StringComparison.Ordinal)
            || lowered.Contains("api_key", StringComparison.Ordinal)
            || lowered.Contains("apikey", StringComparison.Ordinal)
            || lowered.Contains("api-key", StringComparison.Ordinal)
            || lowered.Contains("bearer ", StringComparison.Ordinal)
            || lowered.Contains("ghp_", StringComparison.Ordinal)
            || lowered.Contains("github_pat_", StringComparison.Ordinal)
            || lowered.Contains("user id=", StringComparison.Ordinal)
            || lowered.Contains("accountkey=", StringComparison.Ordinal)
            || lowered.Contains("sharedaccesskey=", StringComparison.Ordinal)
            || lowered.Contains("defaultendpointsprotocol=", StringComparison.Ordinal)
            || lowered.Contains("authorization:", StringComparison.Ordinal)
            || lowered.Contains("cookie:", StringComparison.Ordinal)
            || lowered.Contains("begin private key", StringComparison.Ordinal)
            || lowered.Contains("begin rsa private key", StringComparison.Ordinal)
            || lowered.Contains("begin openssh private key", StringComparison.Ordinal)
            || JwtLikePattern.IsMatch(value)
            || OpenAiKeyLikePattern.IsMatch(value);
    }

    private static bool ContainsStage2SensitiveContent(string? value) =>
        ContainsSecretLikeContent(value)
        || (!string.IsNullOrWhiteSpace(value)
            && (EmailLikePattern.IsMatch(value)
                || WindowsAbsolutePathPattern.IsMatch(value)
                || value.StartsWith(@"\\", StringComparison.Ordinal)));
}
