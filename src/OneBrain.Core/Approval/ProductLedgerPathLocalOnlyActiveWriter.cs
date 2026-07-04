using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace OneBrain.Core.Approval;

public enum ProductLedgerPathLocalOnlyActivationDecision
{
    Rejected,
    Blocked,
    ActivatedLocalOnly
}

public enum ProductLedgerPathLocalOnlyWriterDecision
{
    Rejected,
    Blocked,
    AppendedLocalOnly
}

public enum ProductLedgerPathLocalOnlyBlocker
{
    MissingRequest,
    MissingPersistedCandidateResult,
    FailedPersistedCandidateResult,
    MissingExplicitLocalOnlyActivationMode,
    MissingAuthorityEvidence,
    MissingRedactionBeforePersistenceEvidence,
    MissingFailureReplayRollbackEvidence,
    MissingRetentionEvidence,
    RuntimeFlagNotDefaultOff,
    RuntimeEnablementRequested,
    ProductServiceRegistrationRequested,
    ProductCommandHandlerRequested,
    UiProductActionRequested,
    ProviderCloudNetworkClaimed,
    WormKmsExternalTrustClaimed,
    DbMigrationClaimed,
    BrowserCdpWcuOcrRecipesLiveClaimed,
    ReleaseCommercialReadinessClaimed,
    LocalTempClaimedAsProductLedgerPath,
    MissingActivationResult,
    FailedActivationResult,
    MissingSafePayloadHash,
    UnsafePayloadHash,
    UnsafeEvidenceMetadata,
    ExistingLedgerInvalid
}

public sealed record ProductLedgerPathLocalOnlyActivationRequest(
    ProductLedgerPathPersistedCandidateResult? PersistedCandidateResult,
    bool ExplicitLocalOnlyActivationMode,
    bool HasAuthorityEvidence,
    bool HasRedactionBeforePersistenceEvidence,
    bool HasFailureReplayRollbackEvidence,
    bool HasRetentionEvidence,
    bool LocalRuntimeFlagDefaultOff,
    bool RequestsRuntimeEnablement,
    bool RequestsProductServiceRegistration,
    bool RequestsProductCommandHandler,
    bool RequestsUiProductAction,
    bool ClaimsProviderCloudNetwork,
    bool ClaimsWormKmsExternalTrust,
    bool ClaimsDbMigration,
    bool ClaimsBrowserCdpWcuOcrRecipesLive,
    bool ClaimsReleaseCommercialReadiness,
    bool ClaimsLocalTempAsProductLedgerPath);

public sealed record ProductLedgerPathLocalOnlyActivationResult(
    ProductLedgerPathLocalOnlyActivationDecision Decision,
    IReadOnlyList<ProductLedgerPathLocalOnlyBlocker> Blockers,
    string? CandidateId,
    string? ActiveLedgerRootPath,
    string? ActiveLedgerFilePath,
    string? ActiveCheckpointFilePath,
    bool LocalRuntimeFlagDefaultOff,
    bool ProductLedgerPathActive,
    bool ProductLedgerWriteAllowed,
    bool ProductRuntimeEnabled,
    bool ProductServiceRegistrationAllowed,
    bool ProductCommandHandlersAllowed,
    bool UiProductActionsAllowed,
    bool DbProviderCloudNetworkAllowed,
    bool KmsWormExternalTrustAllowed,
    bool LiveAutomationAllowed,
    bool ReleaseCommercialReady,
    string StatusText);

public sealed record ProductLedgerPathLocalOnlyAppendRequest(
    ProductLedgerPathLocalOnlyActivationResult? ActivationResult,
    string? SafePayloadHash,
    IReadOnlyDictionary<string, string>? EvidenceMetadata,
    bool RuntimeFlagStillDefaultOff,
    bool RequestsRuntimeEnablement,
    bool RequestsProductServiceRegistration,
    bool RequestsProductCommandHandler,
    bool RequestsUiProductAction,
    bool ClaimsProviderCloudNetwork,
    bool ClaimsWormKmsExternalTrust,
    bool ClaimsDbMigration,
    bool ClaimsBrowserCdpWcuOcrRecipesLive,
    bool ClaimsReleaseCommercialReadiness);

public sealed record ProductLedgerPathLocalOnlyEntry(
    int Sequence,
    string CandidateId,
    string SafePayloadHash,
    IReadOnlyDictionary<string, string> EvidenceMetadata,
    string PreviousEntryHash,
    string EntryHash,
    string StatusText);

public sealed record ProductLedgerPathLocalOnlyCheckpoint(
    int HeadSequence,
    string HeadEntryHash,
    string LedgerHash,
    string StatusText);

public sealed record ProductLedgerPathLocalOnlyAppendResult(
    ProductLedgerPathLocalOnlyWriterDecision Decision,
    IReadOnlyList<ProductLedgerPathLocalOnlyBlocker> Blockers,
    ProductLedgerPathLocalOnlyEntry? Entry,
    string? ActiveLedgerFilePath,
    string? ActiveCheckpointFilePath,
    bool ProductLedgerPathActive,
    bool ProductLedgerWriteAllowed,
    bool ProductRuntimeEnabled,
    bool ProductServiceRegistrationAllowed,
    bool ProductCommandHandlersAllowed,
    bool UiProductActionsAllowed,
    bool DbProviderCloudNetworkAllowed,
    bool KmsWormExternalTrustAllowed,
    bool LiveAutomationAllowed,
    bool ReleaseCommercialReady,
    string StatusText);

public sealed class ProductLedgerPathLocalOnlyActiveWriter
{
    public const string ActiveLocalOnlyStatus =
        "ACTIVE_PRODUCT_LEDGER_PATH_LOCAL_ONLY POLICY_BOUND WRITER_BOUNDED_LOCAL_ONLY RUNTIME_FLAG_DEFAULT_OFF NO_PRODUCT_SERVICE_REGISTRATION NO_COMMAND_HANDLERS NO_UI_PRODUCT_ACTIONS NO_DB_MIGRATION NO_PROVIDER_CLOUD_NETWORK NO_WORM_KMS_EXTERNAL_TRUST NO_RELEASE_COMMERCIAL";

    private const string LedgerFileName = "product-ledger.local-only.jsonl";
    private const string CheckpointFileName = "product-ledger.local-only.head.json";
    private const string GenesisHash = "GENESIS";

    private static readonly Regex SafeHashPattern = new(
        @"^[a-f0-9]{64}$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex MetadataKeyPattern = new(
        @"^[a-z0-9][a-z0-9._-]{1,63}$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly string[] UnsafeMetadataMarkers =
    [
        "raw",
        "secret",
        "password",
        "api_key",
        "apikey",
        "token",
        "bearer",
        "client_secret",
        "private_key"
    ];

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public ProductLedgerPathLocalOnlyActivationResult Activate(ProductLedgerPathLocalOnlyActivationRequest? request)
    {
        var blockers = new List<ProductLedgerPathLocalOnlyBlocker>();
        if (request is null)
        {
            blockers.Add(ProductLedgerPathLocalOnlyBlocker.MissingRequest);
            return ActivationResult(blockers, null, null);
        }

        AddCandidateBlockers(request, blockers);
        AddActivationEvidenceBlockers(request, blockers);
        AddNoExternalOrRuntimeBlockers(request, blockers);

        var record = request.PersistedCandidateResult?.Candidate;
        return ActivationResult(blockers, record, blockers.Count == 0 ? record?.CanonicalCandidatePath : null);
    }

    public ProductLedgerPathLocalOnlyAppendResult Append(ProductLedgerPathLocalOnlyAppendRequest? request)
    {
        var blockers = new List<ProductLedgerPathLocalOnlyBlocker>();
        if (request is null)
        {
            blockers.Add(ProductLedgerPathLocalOnlyBlocker.MissingRequest);
            return AppendResult(blockers, null, null, null);
        }

        AddActivationBlockers(request, blockers);
        AddRuntimeAndExternalBlockers(request, blockers);
        AddPayloadAndMetadataBlockers(request, blockers);

        ProductLedgerPathLocalOnlyEntry? entry = null;
        var ledgerPath = request.ActivationResult?.ActiveLedgerFilePath;
        var checkpointPath = request.ActivationResult?.ActiveCheckpointFilePath;
        if (blockers.Count == 0)
        {
            try
            {
                entry = AppendEntry(request, request.ActivationResult!);
            }
            catch (InvalidDataException)
            {
                blockers.Add(ProductLedgerPathLocalOnlyBlocker.ExistingLedgerInvalid);
                entry = null;
            }
        }

        return AppendResult(blockers, entry, blockers.Count == 0 ? ledgerPath : null, blockers.Count == 0 ? checkpointPath : null);
    }

    public IReadOnlyList<ProductLedgerPathLocalOnlyEntry> ReadVerified(ProductLedgerPathLocalOnlyActivationResult activation)
    {
        if (activation.Decision != ProductLedgerPathLocalOnlyActivationDecision.ActivatedLocalOnly
            || string.IsNullOrWhiteSpace(activation.ActiveLedgerFilePath)
            || string.IsNullOrWhiteSpace(activation.ActiveLedgerRootPath))
        {
            throw new InvalidDataException("activation not ready");
        }

        var entries = File.Exists(activation.ActiveLedgerFilePath)
            ? ReadAndVerifyEntries(activation.ActiveLedgerFilePath)
            : [];
        VerifyExistingCheckpoint(activation.ActiveLedgerRootPath, entries);
        return entries;
    }

    private static void AddCandidateBlockers(
        ProductLedgerPathLocalOnlyActivationRequest request,
        List<ProductLedgerPathLocalOnlyBlocker> blockers)
    {
        var result = request.PersistedCandidateResult;
        if (result is null)
        {
            blockers.Add(ProductLedgerPathLocalOnlyBlocker.MissingPersistedCandidateResult);
            return;
        }

        if (result.Decision != ProductLedgerPathPersistedCandidateDecision.PersistedCandidateNoWrite
            || !result.CandidatePersistedNoWrite
            || result.Blockers.Count > 0
            || result.Candidate is null
            || result.ProductLedgerPathActive
            || result.ProductLedgerWriteAllowed
            || result.ProductRuntimeEnabled
            || result.ProductServiceRegistrationAllowed
            || result.ProductCommandHandlersAllowed
            || result.UiProductActionsAllowed
            || result.DbProviderCloudNetworkAllowed
            || result.KmsWormExternalTrustAllowed
            || result.LiveAutomationAllowed
            || result.ReleaseCommercialReady
            || IsUnderLocalTemp(result.Candidate.CanonicalCandidatePath))
        {
            blockers.Add(ProductLedgerPathLocalOnlyBlocker.FailedPersistedCandidateResult);
        }
    }

    private static void AddActivationEvidenceBlockers(
        ProductLedgerPathLocalOnlyActivationRequest request,
        List<ProductLedgerPathLocalOnlyBlocker> blockers)
    {
        if (!request.ExplicitLocalOnlyActivationMode)
        {
            blockers.Add(ProductLedgerPathLocalOnlyBlocker.MissingExplicitLocalOnlyActivationMode);
        }

        if (!request.HasAuthorityEvidence)
        {
            blockers.Add(ProductLedgerPathLocalOnlyBlocker.MissingAuthorityEvidence);
        }

        if (!request.HasRedactionBeforePersistenceEvidence)
        {
            blockers.Add(ProductLedgerPathLocalOnlyBlocker.MissingRedactionBeforePersistenceEvidence);
        }

        if (!request.HasFailureReplayRollbackEvidence)
        {
            blockers.Add(ProductLedgerPathLocalOnlyBlocker.MissingFailureReplayRollbackEvidence);
        }

        if (!request.HasRetentionEvidence)
        {
            blockers.Add(ProductLedgerPathLocalOnlyBlocker.MissingRetentionEvidence);
        }

        if (!request.LocalRuntimeFlagDefaultOff)
        {
            blockers.Add(ProductLedgerPathLocalOnlyBlocker.RuntimeFlagNotDefaultOff);
        }

        if (request.ClaimsLocalTempAsProductLedgerPath)
        {
            blockers.Add(ProductLedgerPathLocalOnlyBlocker.LocalTempClaimedAsProductLedgerPath);
        }
    }

    private static void AddNoExternalOrRuntimeBlockers(
        ProductLedgerPathLocalOnlyActivationRequest request,
        List<ProductLedgerPathLocalOnlyBlocker> blockers)
    {
        AddRuntimeAndExternalBlockers(
            request.RequestsRuntimeEnablement,
            request.RequestsProductServiceRegistration,
            request.RequestsProductCommandHandler,
            request.RequestsUiProductAction,
            request.ClaimsProviderCloudNetwork,
            request.ClaimsWormKmsExternalTrust,
            request.ClaimsDbMigration,
            request.ClaimsBrowserCdpWcuOcrRecipesLive,
            request.ClaimsReleaseCommercialReadiness,
            blockers);
    }

    private static void AddActivationBlockers(
        ProductLedgerPathLocalOnlyAppendRequest request,
        List<ProductLedgerPathLocalOnlyBlocker> blockers)
    {
        var activation = request.ActivationResult;
        if (activation is null)
        {
            blockers.Add(ProductLedgerPathLocalOnlyBlocker.MissingActivationResult);
            return;
        }

        if (activation.Decision != ProductLedgerPathLocalOnlyActivationDecision.ActivatedLocalOnly
            || activation.Blockers.Count > 0
            || string.IsNullOrWhiteSpace(activation.CandidateId)
            || string.IsNullOrWhiteSpace(activation.ActiveLedgerRootPath)
            || string.IsNullOrWhiteSpace(activation.ActiveLedgerFilePath)
            || string.IsNullOrWhiteSpace(activation.ActiveCheckpointFilePath)
            || !activation.LocalRuntimeFlagDefaultOff
            || !activation.ProductLedgerPathActive
            || !activation.ProductLedgerWriteAllowed
            || activation.ProductRuntimeEnabled
            || activation.ProductServiceRegistrationAllowed
            || activation.ProductCommandHandlersAllowed
            || activation.UiProductActionsAllowed
            || activation.DbProviderCloudNetworkAllowed
            || activation.KmsWormExternalTrustAllowed
            || activation.LiveAutomationAllowed
            || activation.ReleaseCommercialReady)
        {
            blockers.Add(ProductLedgerPathLocalOnlyBlocker.FailedActivationResult);
        }
    }

    private static void AddRuntimeAndExternalBlockers(
        ProductLedgerPathLocalOnlyAppendRequest request,
        List<ProductLedgerPathLocalOnlyBlocker> blockers)
    {
        if (!request.RuntimeFlagStillDefaultOff)
        {
            blockers.Add(ProductLedgerPathLocalOnlyBlocker.RuntimeFlagNotDefaultOff);
        }

        AddRuntimeAndExternalBlockers(
            request.RequestsRuntimeEnablement,
            request.RequestsProductServiceRegistration,
            request.RequestsProductCommandHandler,
            request.RequestsUiProductAction,
            request.ClaimsProviderCloudNetwork,
            request.ClaimsWormKmsExternalTrust,
            request.ClaimsDbMigration,
            request.ClaimsBrowserCdpWcuOcrRecipesLive,
            request.ClaimsReleaseCommercialReadiness,
            blockers);
    }

    private static void AddRuntimeAndExternalBlockers(
        bool requestsRuntimeEnablement,
        bool requestsProductServiceRegistration,
        bool requestsProductCommandHandler,
        bool requestsUiProductAction,
        bool claimsProviderCloudNetwork,
        bool claimsWormKmsExternalTrust,
        bool claimsDbMigration,
        bool claimsBrowserCdpWcuOcrRecipesLive,
        bool claimsReleaseCommercialReadiness,
        List<ProductLedgerPathLocalOnlyBlocker> blockers)
    {
        if (requestsRuntimeEnablement)
        {
            blockers.Add(ProductLedgerPathLocalOnlyBlocker.RuntimeEnablementRequested);
        }

        if (requestsProductServiceRegistration)
        {
            blockers.Add(ProductLedgerPathLocalOnlyBlocker.ProductServiceRegistrationRequested);
        }

        if (requestsProductCommandHandler)
        {
            blockers.Add(ProductLedgerPathLocalOnlyBlocker.ProductCommandHandlerRequested);
        }

        if (requestsUiProductAction)
        {
            blockers.Add(ProductLedgerPathLocalOnlyBlocker.UiProductActionRequested);
        }

        if (claimsProviderCloudNetwork)
        {
            blockers.Add(ProductLedgerPathLocalOnlyBlocker.ProviderCloudNetworkClaimed);
        }

        if (claimsWormKmsExternalTrust)
        {
            blockers.Add(ProductLedgerPathLocalOnlyBlocker.WormKmsExternalTrustClaimed);
        }

        if (claimsDbMigration)
        {
            blockers.Add(ProductLedgerPathLocalOnlyBlocker.DbMigrationClaimed);
        }

        if (claimsBrowserCdpWcuOcrRecipesLive)
        {
            blockers.Add(ProductLedgerPathLocalOnlyBlocker.BrowserCdpWcuOcrRecipesLiveClaimed);
        }

        if (claimsReleaseCommercialReadiness)
        {
            blockers.Add(ProductLedgerPathLocalOnlyBlocker.ReleaseCommercialReadinessClaimed);
        }
    }

    private static void AddPayloadAndMetadataBlockers(
        ProductLedgerPathLocalOnlyAppendRequest request,
        List<ProductLedgerPathLocalOnlyBlocker> blockers)
    {
        if (string.IsNullOrWhiteSpace(request.SafePayloadHash))
        {
            blockers.Add(ProductLedgerPathLocalOnlyBlocker.MissingSafePayloadHash);
        }
        else if (!IsSafePayloadHash(request.SafePayloadHash))
        {
            blockers.Add(ProductLedgerPathLocalOnlyBlocker.UnsafePayloadHash);
        }

        if (!IsSafeMetadata(request.EvidenceMetadata))
        {
            blockers.Add(ProductLedgerPathLocalOnlyBlocker.UnsafeEvidenceMetadata);
        }
    }

    private static ProductLedgerPathLocalOnlyEntry AppendEntry(
        ProductLedgerPathLocalOnlyAppendRequest request,
        ProductLedgerPathLocalOnlyActivationResult activation)
    {
        Directory.CreateDirectory(activation.ActiveLedgerRootPath!);
        var existing = File.Exists(activation.ActiveLedgerFilePath!)
            ? ReadAndVerifyEntries(activation.ActiveLedgerFilePath!)
            : [];
        VerifyExistingCheckpoint(activation.ActiveLedgerRootPath!, existing);
        var sequence = existing.Count + 1;
        var previousHash = existing.Count == 0 ? GenesisHash : existing[^1].EntryHash;
        var metadata = request.EvidenceMetadata!
            .OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase);
        var entryHash = HashEntry(sequence, activation.CandidateId!, request.SafePayloadHash!, metadata, previousHash);
        var entry = new ProductLedgerPathLocalOnlyEntry(
            Sequence: sequence,
            CandidateId: activation.CandidateId!,
            SafePayloadHash: request.SafePayloadHash!.ToLowerInvariant(),
            EvidenceMetadata: metadata,
            PreviousEntryHash: previousHash,
            EntryHash: entryHash,
            StatusText: ActiveLocalOnlyStatus);

        File.AppendAllText(activation.ActiveLedgerFilePath!, JsonSerializer.Serialize(entry, JsonOptions) + Environment.NewLine, Encoding.UTF8);
        WriteCheckpoint(activation.ActiveLedgerRootPath!, existing.Concat([entry]).ToArray());
        return entry;
    }

    private static ProductLedgerPathLocalOnlyActivationResult ActivationResult(
        IReadOnlyList<ProductLedgerPathLocalOnlyBlocker> blockers,
        ProductLedgerPathPersistedCandidateRecord? record,
        string? activeLedgerRootPath)
    {
        var distinct = blockers.Distinct().OrderBy(blocker => blocker.ToString(), StringComparer.Ordinal).ToArray();
        var active = distinct.Length == 0 && record is not null && !string.IsNullOrWhiteSpace(activeLedgerRootPath);
        return new ProductLedgerPathLocalOnlyActivationResult(
            Decision: active
                ? ProductLedgerPathLocalOnlyActivationDecision.ActivatedLocalOnly
                : (distinct.Contains(ProductLedgerPathLocalOnlyBlocker.MissingRequest)
                    ? ProductLedgerPathLocalOnlyActivationDecision.Rejected
                    : ProductLedgerPathLocalOnlyActivationDecision.Blocked),
            Blockers: distinct,
            CandidateId: active ? record!.CandidateId : null,
            ActiveLedgerRootPath: active ? activeLedgerRootPath : null,
            ActiveLedgerFilePath: active ? Path.Combine(activeLedgerRootPath!, LedgerFileName) : null,
            ActiveCheckpointFilePath: active ? Path.Combine(activeLedgerRootPath!, CheckpointFileName) : null,
            LocalRuntimeFlagDefaultOff: active,
            ProductLedgerPathActive: active,
            ProductLedgerWriteAllowed: active,
            ProductRuntimeEnabled: false,
            ProductServiceRegistrationAllowed: false,
            ProductCommandHandlersAllowed: false,
            UiProductActionsAllowed: false,
            DbProviderCloudNetworkAllowed: false,
            KmsWormExternalTrustAllowed: false,
            LiveAutomationAllowed: false,
            ReleaseCommercialReady: false,
            StatusText: ActiveLocalOnlyStatus);
    }

    private static ProductLedgerPathLocalOnlyAppendResult AppendResult(
        IReadOnlyList<ProductLedgerPathLocalOnlyBlocker> blockers,
        ProductLedgerPathLocalOnlyEntry? entry,
        string? ledgerPath,
        string? checkpointPath)
    {
        var distinct = blockers.Distinct().OrderBy(blocker => blocker.ToString(), StringComparer.Ordinal).ToArray();
        var appended = distinct.Length == 0 && entry is not null;
        return new ProductLedgerPathLocalOnlyAppendResult(
            Decision: appended
                ? ProductLedgerPathLocalOnlyWriterDecision.AppendedLocalOnly
                : (distinct.Contains(ProductLedgerPathLocalOnlyBlocker.MissingRequest)
                    ? ProductLedgerPathLocalOnlyWriterDecision.Rejected
                    : ProductLedgerPathLocalOnlyWriterDecision.Blocked),
            Blockers: distinct,
            Entry: entry,
            ActiveLedgerFilePath: ledgerPath,
            ActiveCheckpointFilePath: checkpointPath,
            ProductLedgerPathActive: appended,
            ProductLedgerWriteAllowed: appended,
            ProductRuntimeEnabled: false,
            ProductServiceRegistrationAllowed: false,
            ProductCommandHandlersAllowed: false,
            UiProductActionsAllowed: false,
            DbProviderCloudNetworkAllowed: false,
            KmsWormExternalTrustAllowed: false,
            LiveAutomationAllowed: false,
            ReleaseCommercialReady: false,
            StatusText: ActiveLocalOnlyStatus);
    }

    private static IReadOnlyList<ProductLedgerPathLocalOnlyEntry> ReadAndVerifyEntries(string ledgerPath)
    {
        var entries = new List<ProductLedgerPathLocalOnlyEntry>();
        foreach (var line in File.ReadLines(ledgerPath))
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                throw new InvalidDataException("invalid local-only ledger");
            }

            var entry = JsonSerializer.Deserialize<ProductLedgerPathLocalOnlyEntry>(line, JsonOptions)
                ?? throw new InvalidDataException("invalid local-only ledger");
            if (string.IsNullOrWhiteSpace(entry.CandidateId)
                || string.IsNullOrWhiteSpace(entry.SafePayloadHash)
                || entry.EvidenceMetadata is null
                || string.IsNullOrWhiteSpace(entry.PreviousEntryHash)
                || string.IsNullOrWhiteSpace(entry.EntryHash)
                || entry.StatusText != ActiveLocalOnlyStatus
                || !IsSafePayloadHash(entry.SafePayloadHash)
                || !IsSafeMetadata(entry.EvidenceMetadata))
            {
                throw new InvalidDataException("invalid local-only ledger");
            }

            var expectedSequence = entries.Count + 1;
            var expectedPreviousHash = entries.Count == 0 ? GenesisHash : entries[^1].EntryHash;
            var expectedHash = HashEntry(entry.Sequence, entry.CandidateId, entry.SafePayloadHash, entry.EvidenceMetadata, entry.PreviousEntryHash);
            if (entry.Sequence != expectedSequence
                || entry.PreviousEntryHash != expectedPreviousHash
                || entry.EntryHash != expectedHash)
            {
                throw new InvalidDataException("invalid local-only ledger");
            }

            entries.Add(entry);
        }

        return entries;
    }

    private static void VerifyExistingCheckpoint(string activeLedgerRootPath, IReadOnlyList<ProductLedgerPathLocalOnlyEntry> entries)
    {
        var checkpointPath = Path.Combine(activeLedgerRootPath, CheckpointFileName);
        if (!File.Exists(checkpointPath))
        {
            if (entries.Count > 0)
            {
                throw new InvalidDataException("invalid local-only ledger");
            }

            return;
        }

        var checkpoint = JsonSerializer.Deserialize<ProductLedgerPathLocalOnlyCheckpoint>(
            File.ReadAllText(checkpointPath, Encoding.UTF8),
            JsonOptions) ?? throw new InvalidDataException("invalid local-only ledger");
        var expectedHeadHash = entries.Count == 0 ? GenesisHash : entries[^1].EntryHash;
        var expectedLedgerHash = HashLedger(entries);
        if (checkpoint.HeadSequence != entries.Count
            || checkpoint.HeadEntryHash != expectedHeadHash
            || checkpoint.LedgerHash != expectedLedgerHash
            || checkpoint.StatusText != ActiveLocalOnlyStatus)
        {
            throw new InvalidDataException("invalid local-only ledger");
        }
    }

    private static void WriteCheckpoint(string activeLedgerRootPath, IReadOnlyList<ProductLedgerPathLocalOnlyEntry> entries)
    {
        var checkpoint = new ProductLedgerPathLocalOnlyCheckpoint(
            HeadSequence: entries.Count,
            HeadEntryHash: entries.Count == 0 ? GenesisHash : entries[^1].EntryHash,
            LedgerHash: HashLedger(entries),
            StatusText: ActiveLocalOnlyStatus);
        File.WriteAllText(Path.Combine(activeLedgerRootPath, CheckpointFileName), JsonSerializer.Serialize(checkpoint, JsonOptions), Encoding.UTF8);
    }

    private static string HashLedger(IReadOnlyList<ProductLedgerPathLocalOnlyEntry> entries)
    {
        var material = string.Join("\n", entries.Select(entry => $"{entry.Sequence}:{entry.EntryHash}"));
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(material));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string HashEntry(
        int sequence,
        string candidateId,
        string safePayloadHash,
        IReadOnlyDictionary<string, string> metadata,
        string previousHash)
    {
        var metadataText = string.Join(
            "\n",
            metadata.OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
                .Select(pair => $"{pair.Key}={pair.Value}"));
        var material = $"{sequence}\n{candidateId}\n{safePayloadHash.ToLowerInvariant()}\n{metadataText}\n{previousHash}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(material));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static bool IsSafePayloadHash(string? safePayloadHash) =>
        !string.IsNullOrWhiteSpace(safePayloadHash) && SafeHashPattern.IsMatch(safePayloadHash);

    private static bool IsSafeMetadata(IReadOnlyDictionary<string, string>? metadata)
    {
        if (metadata is null || metadata.Count == 0)
        {
            return false;
        }

        foreach (var pair in metadata)
        {
            if (!MetadataKeyPattern.IsMatch(pair.Key)
                || string.IsNullOrWhiteSpace(pair.Value)
                || pair.Value.Length > 128
                || pair.Value.Contains("..", StringComparison.Ordinal)
                || pair.Value.Contains('\\', StringComparison.Ordinal)
                || UnsafeMetadataMarkers.Any(marker =>
                    pair.Key.Contains(marker, StringComparison.OrdinalIgnoreCase)
                    || pair.Value.Contains(marker, StringComparison.OrdinalIgnoreCase)))
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsUnderLocalTemp(string path)
    {
        var candidate = Path.GetFullPath(path);
        var tempRoot = Path.GetFullPath(Path.GetTempPath());
        return candidate.StartsWith(tempRoot, StringComparison.OrdinalIgnoreCase);
    }
}
