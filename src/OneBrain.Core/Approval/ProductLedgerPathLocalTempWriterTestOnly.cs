using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace OneBrain.Core.Approval;

public enum ProductLedgerPathLocalTempWriterDecision
{
    Rejected,
    Blocked,
    WrittenLocalTempTestOnly
}

public enum ProductLedgerPathLocalTempWriterBlocker
{
    MissingRequest,
    MissingWriterScaffoldResult,
    FailedWriterScaffoldResult,
    MissingCandidateId,
    InvalidCandidateId,
    MissingLocalTempRoot,
    LocalTempRootOutsideSystemTemp,
    MissingExplicitLocalTempTestOnlyMode,
    LocalTempClaimedAsProductLedgerPath,
    MissingRedactionBeforePersistenceEvidence,
    MissingFailureReplayRollbackEvidence,
    MissingSafePayloadHash,
    UnsafePayloadHash,
    UnsafeEvidenceMetadata,
    ExistingLocalTempLedgerInvalid,
    ProductWriteRequested,
    RuntimeEnablementRequested,
    ProductLedgerPathActivationRequested,
    ProductServiceRegistrationRequested,
    ProductCommandHandlerRequested,
    UiProductActionRequested,
    ProviderCloudNetworkClaimed,
    WormKmsExternalTrustClaimed,
    ReleaseCommercialReadinessClaimed
}

public sealed record ProductLedgerPathLocalTempWriterRequest(
    ProductLedgerPathWriterScaffoldResult? WriterScaffoldResult,
    string? CandidateId,
    string? LocalTempRoot,
    string? SafePayloadHash,
    IReadOnlyDictionary<string, string>? EvidenceMetadata,
    bool ExplicitLocalTempTestOnlyMode,
    bool ClaimsLocalTempAsProductLedgerPath,
    bool HasRedactionBeforePersistenceEvidence,
    bool HasFailureReplayRollbackEvidence,
    bool RequestsWriterActivation,
    bool RequestsRuntimeEnablement,
    bool RequestsProductLedgerPathActivation,
    bool RequestsProductServiceRegistration,
    bool RequestsProductCommandHandler,
    bool RequestsUiProductAction,
    bool ClaimsProviderCloudNetwork,
    bool ClaimsWormKmsExternalTrust,
    bool ClaimsReleaseCommercialReadiness);

public sealed record ProductLedgerPathLocalTempWriterEntry(
    int Sequence,
    string CandidateId,
    string SafePayloadHash,
    IReadOnlyDictionary<string, string> EvidenceMetadata,
    string PreviousEntryHash,
    string EntryHash,
    string StatusText);

public sealed record ProductLedgerPathLocalTempWriterCheckpoint(
    int HeadSequence,
    string HeadEntryHash,
    string LedgerHash,
    string StatusText);

public sealed record ProductLedgerPathLocalTempWriterResult(
    ProductLedgerPathLocalTempWriterDecision Decision,
    IReadOnlyList<ProductLedgerPathLocalTempWriterBlocker> Blockers,
    ProductLedgerPathLocalTempWriterEntry? Entry,
    string? LocalTempLedgerPath,
    bool LocalTempTestOnlyWriteCompleted,
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

public sealed class ProductLedgerPathLocalTempWriterTestOnly
{
    public const string LocalTempWriterStatus =
        "LOCAL_TEMP_WRITER_TEST_ONLY NOT_PRODUCT_LEDGER_PATH NO_ACTIVE_PRODUCT_LEDGER_PATH NO_PRODUCT_LEDGER_WRITE NO_PRODUCT_RUNTIME_ENABLEMENT NO_PRODUCT_SERVICE_REGISTRATION NO_COMMAND_HANDLERS NO_UI_PRODUCT_ACTIONS NO_RELEASE_COMMERCIAL";

    private const string LedgerFileName = "product-ledger-path-local-temp-writer-test-only.jsonl";
    private const string CheckpointFileName = "product-ledger-path-local-temp-writer-test-only.head.json";
    private const string GenesisHash = "GENESIS";

    private static readonly Regex CandidateIdPattern = new(
        @"^[a-z0-9][a-z0-9._-]{2,63}$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

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

    public ProductLedgerPathLocalTempWriterResult Append(ProductLedgerPathLocalTempWriterRequest? request)
    {
        var blockers = new List<ProductLedgerPathLocalTempWriterBlocker>();
        if (request is null)
        {
            blockers.Add(ProductLedgerPathLocalTempWriterBlocker.MissingRequest);
            return Result(blockers, null, null);
        }

        AddScaffoldBlockers(request, blockers);
        AddIdentityAndBoundaryBlockers(request, blockers);
        AddEvidenceBlockers(request, blockers);
        AddNoEnableBlockers(request, blockers);

        var ledgerPath = blockers.Count == 0 ? LocalTempLedgerPath(request.LocalTempRoot!) : null;
        ProductLedgerPathLocalTempWriterEntry? entry = null;

        if (blockers.Count == 0)
        {
            try
            {
                entry = AppendLocalTempEntry(request, ledgerPath!);
            }
            catch (InvalidDataException)
            {
                blockers.Add(ProductLedgerPathLocalTempWriterBlocker.ExistingLocalTempLedgerInvalid);
                entry = null;
            }
        }

        return Result(blockers, entry, blockers.Count == 0 ? ledgerPath : null);
    }

    public IReadOnlyList<ProductLedgerPathLocalTempWriterEntry> ReadLocalTempEntries(string localTempRoot)
    {
        var ledgerPath = LocalTempLedgerPath(localTempRoot);
        var entries = File.Exists(ledgerPath) ? ReadAndVerifyEntries(ledgerPath) : [];
        VerifyExistingCheckpoint(localTempRoot, entries);
        return entries;
    }

    private static void AddScaffoldBlockers(
        ProductLedgerPathLocalTempWriterRequest request,
        List<ProductLedgerPathLocalTempWriterBlocker> blockers)
    {
        var scaffold = request.WriterScaffoldResult;
        if (scaffold is null)
        {
            blockers.Add(ProductLedgerPathLocalTempWriterBlocker.MissingWriterScaffoldResult);
            return;
        }

        if (scaffold.Decision != ProductLedgerPathWriterScaffoldDecision.DisabledWriterScaffoldReady
            || !scaffold.DisabledWriterScaffoldReady
            || scaffold.Blockers.Count > 0
            || scaffold.ProductLedgerPathActive
            || scaffold.ProductLedgerWriteAllowed
            || scaffold.ProductRuntimeEnabled
            || scaffold.ProductServiceRegistrationAllowed
            || scaffold.ProductCommandHandlersAllowed
            || scaffold.UiProductActionsAllowed
            || scaffold.DbProviderCloudNetworkAllowed
            || scaffold.KmsWormExternalTrustAllowed
            || scaffold.LiveAutomationAllowed
            || scaffold.ReleaseCommercialReady)
        {
            blockers.Add(ProductLedgerPathLocalTempWriterBlocker.FailedWriterScaffoldResult);
        }
    }

    private static void AddIdentityAndBoundaryBlockers(
        ProductLedgerPathLocalTempWriterRequest request,
        List<ProductLedgerPathLocalTempWriterBlocker> blockers)
    {
        if (string.IsNullOrWhiteSpace(request.CandidateId))
        {
            blockers.Add(ProductLedgerPathLocalTempWriterBlocker.MissingCandidateId);
        }
        else if (!CandidateIdPattern.IsMatch(request.CandidateId))
        {
            blockers.Add(ProductLedgerPathLocalTempWriterBlocker.InvalidCandidateId);
        }

        if (string.IsNullOrWhiteSpace(request.LocalTempRoot))
        {
            blockers.Add(ProductLedgerPathLocalTempWriterBlocker.MissingLocalTempRoot);
        }
        else if (!IsUnderSystemTemp(request.LocalTempRoot))
        {
            blockers.Add(ProductLedgerPathLocalTempWriterBlocker.LocalTempRootOutsideSystemTemp);
        }

        if (!request.ExplicitLocalTempTestOnlyMode)
        {
            blockers.Add(ProductLedgerPathLocalTempWriterBlocker.MissingExplicitLocalTempTestOnlyMode);
        }

        if (request.ClaimsLocalTempAsProductLedgerPath)
        {
            blockers.Add(ProductLedgerPathLocalTempWriterBlocker.LocalTempClaimedAsProductLedgerPath);
        }
    }

    private static void AddEvidenceBlockers(
        ProductLedgerPathLocalTempWriterRequest request,
        List<ProductLedgerPathLocalTempWriterBlocker> blockers)
    {
        if (!request.HasRedactionBeforePersistenceEvidence)
        {
            blockers.Add(ProductLedgerPathLocalTempWriterBlocker.MissingRedactionBeforePersistenceEvidence);
        }

        if (!request.HasFailureReplayRollbackEvidence)
        {
            blockers.Add(ProductLedgerPathLocalTempWriterBlocker.MissingFailureReplayRollbackEvidence);
        }

        if (string.IsNullOrWhiteSpace(request.SafePayloadHash))
        {
            blockers.Add(ProductLedgerPathLocalTempWriterBlocker.MissingSafePayloadHash);
        }
        else if (!SafeHashPattern.IsMatch(request.SafePayloadHash))
        {
            blockers.Add(ProductLedgerPathLocalTempWriterBlocker.UnsafePayloadHash);
        }

        var metadata = request.EvidenceMetadata;
        if (metadata is null || metadata.Count == 0)
        {
            blockers.Add(ProductLedgerPathLocalTempWriterBlocker.UnsafeEvidenceMetadata);
            return;
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
                blockers.Add(ProductLedgerPathLocalTempWriterBlocker.UnsafeEvidenceMetadata);
                return;
            }
        }
    }

    private static void AddNoEnableBlockers(
        ProductLedgerPathLocalTempWriterRequest request,
        List<ProductLedgerPathLocalTempWriterBlocker> blockers)
    {
        if (request.RequestsWriterActivation)
        {
            blockers.Add(ProductLedgerPathLocalTempWriterBlocker.ProductWriteRequested);
        }

        if (request.RequestsRuntimeEnablement)
        {
            blockers.Add(ProductLedgerPathLocalTempWriterBlocker.RuntimeEnablementRequested);
        }

        if (request.RequestsProductLedgerPathActivation)
        {
            blockers.Add(ProductLedgerPathLocalTempWriterBlocker.ProductLedgerPathActivationRequested);
        }

        if (request.RequestsProductServiceRegistration)
        {
            blockers.Add(ProductLedgerPathLocalTempWriterBlocker.ProductServiceRegistrationRequested);
        }

        if (request.RequestsProductCommandHandler)
        {
            blockers.Add(ProductLedgerPathLocalTempWriterBlocker.ProductCommandHandlerRequested);
        }

        if (request.RequestsUiProductAction)
        {
            blockers.Add(ProductLedgerPathLocalTempWriterBlocker.UiProductActionRequested);
        }

        if (request.ClaimsProviderCloudNetwork)
        {
            blockers.Add(ProductLedgerPathLocalTempWriterBlocker.ProviderCloudNetworkClaimed);
        }

        if (request.ClaimsWormKmsExternalTrust)
        {
            blockers.Add(ProductLedgerPathLocalTempWriterBlocker.WormKmsExternalTrustClaimed);
        }

        if (request.ClaimsReleaseCommercialReadiness)
        {
            blockers.Add(ProductLedgerPathLocalTempWriterBlocker.ReleaseCommercialReadinessClaimed);
        }
    }

    private static ProductLedgerPathLocalTempWriterEntry AppendLocalTempEntry(
        ProductLedgerPathLocalTempWriterRequest request,
        string ledgerPath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(ledgerPath)!);
        var localTempRoot = Path.GetDirectoryName(ledgerPath)!;
        var existing = File.Exists(ledgerPath) ? ReadAndVerifyEntries(ledgerPath) : [];
        VerifyExistingCheckpoint(localTempRoot, existing);
        var sequence = existing.Count + 1;
        var previousHash = existing.Count == 0 ? GenesisHash : existing[^1].EntryHash;
        var metadata = request.EvidenceMetadata!
            .OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase);
        var entryHash = HashEntry(sequence, request.CandidateId!, request.SafePayloadHash!, metadata, previousHash);
        var entry = new ProductLedgerPathLocalTempWriterEntry(
            Sequence: sequence,
            CandidateId: request.CandidateId!,
            SafePayloadHash: request.SafePayloadHash!.ToLowerInvariant(),
            EvidenceMetadata: metadata,
            PreviousEntryHash: previousHash,
            EntryHash: entryHash,
            StatusText: LocalTempWriterStatus);

        File.AppendAllText(ledgerPath, JsonSerializer.Serialize(entry, JsonOptions) + Environment.NewLine, Encoding.UTF8);
        WriteCheckpoint(localTempRoot, existing.Concat([entry]).ToArray());
        return entry;
    }

    private static void VerifyExistingCheckpoint(
        string localTempRoot,
        IReadOnlyList<ProductLedgerPathLocalTempWriterEntry> entries)
    {
        var checkpointPath = LocalTempCheckpointPath(localTempRoot);
        if (!File.Exists(checkpointPath))
        {
            if (entries.Count > 0)
            {
                throw new InvalidDataException("invalid local-temp ledger");
            }

            return;
        }

        var checkpoint = JsonSerializer.Deserialize<ProductLedgerPathLocalTempWriterCheckpoint>(
            File.ReadAllText(checkpointPath, Encoding.UTF8),
            JsonOptions) ?? throw new InvalidDataException("invalid local-temp ledger");
        var expectedHeadSequence = entries.Count;
        var expectedHeadHash = entries.Count == 0 ? GenesisHash : entries[^1].EntryHash;
        var expectedLedgerHash = HashLedger(entries);
        if (checkpoint.HeadSequence != expectedHeadSequence
            || checkpoint.HeadEntryHash != expectedHeadHash
            || checkpoint.LedgerHash != expectedLedgerHash
            || checkpoint.StatusText != LocalTempWriterStatus)
        {
            throw new InvalidDataException("invalid local-temp ledger");
        }
    }

    private static void WriteCheckpoint(
        string localTempRoot,
        IReadOnlyList<ProductLedgerPathLocalTempWriterEntry> entries)
    {
        var checkpoint = new ProductLedgerPathLocalTempWriterCheckpoint(
            HeadSequence: entries.Count,
            HeadEntryHash: entries.Count == 0 ? GenesisHash : entries[^1].EntryHash,
            LedgerHash: HashLedger(entries),
            StatusText: LocalTempWriterStatus);
        File.WriteAllText(LocalTempCheckpointPath(localTempRoot), JsonSerializer.Serialize(checkpoint, JsonOptions), Encoding.UTF8);
    }

    private static IReadOnlyList<ProductLedgerPathLocalTempWriterEntry> ReadAndVerifyEntries(string ledgerPath)
    {
        var entries = new List<ProductLedgerPathLocalTempWriterEntry>();
        foreach (var line in File.ReadLines(ledgerPath))
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                throw new InvalidDataException("invalid local-temp ledger");
            }

            var entry = JsonSerializer.Deserialize<ProductLedgerPathLocalTempWriterEntry>(line, JsonOptions)
                ?? throw new InvalidDataException("invalid local-temp ledger");
            if (string.IsNullOrWhiteSpace(entry.CandidateId)
                || string.IsNullOrWhiteSpace(entry.SafePayloadHash)
                || entry.EvidenceMetadata is null
                || string.IsNullOrWhiteSpace(entry.PreviousEntryHash)
                || string.IsNullOrWhiteSpace(entry.EntryHash)
                || string.IsNullOrWhiteSpace(entry.StatusText))
            {
                throw new InvalidDataException("invalid local-temp ledger");
            }

            var expectedSequence = entries.Count + 1;
            var expectedPreviousHash = entries.Count == 0 ? GenesisHash : entries[^1].EntryHash;
            var expectedHash = HashEntry(
                entry.Sequence,
                entry.CandidateId,
                entry.SafePayloadHash,
                entry.EvidenceMetadata,
                entry.PreviousEntryHash);
            if (entry.Sequence != expectedSequence
                || entry.PreviousEntryHash != expectedPreviousHash
                || entry.EntryHash != expectedHash
                || entry.StatusText != LocalTempWriterStatus)
            {
                throw new InvalidDataException("invalid local-temp ledger");
            }

            entries.Add(entry);
        }

        return entries;
    }

    private static ProductLedgerPathLocalTempWriterResult Result(
        IReadOnlyList<ProductLedgerPathLocalTempWriterBlocker> blockers,
        ProductLedgerPathLocalTempWriterEntry? entry,
        string? ledgerPath)
    {
        var distinct = blockers.Distinct().OrderBy(blocker => blocker.ToString(), StringComparer.Ordinal).ToArray();
        var written = distinct.Length == 0 && entry is not null;
        return new ProductLedgerPathLocalTempWriterResult(
            Decision: written
                ? ProductLedgerPathLocalTempWriterDecision.WrittenLocalTempTestOnly
                : (distinct.Contains(ProductLedgerPathLocalTempWriterBlocker.MissingRequest)
                    ? ProductLedgerPathLocalTempWriterDecision.Rejected
                    : ProductLedgerPathLocalTempWriterDecision.Blocked),
            Blockers: distinct,
            Entry: entry,
            LocalTempLedgerPath: ledgerPath,
            LocalTempTestOnlyWriteCompleted: written,
            ProductLedgerPathActive: false,
            ProductLedgerWriteAllowed: false,
            ProductRuntimeEnabled: false,
            ProductServiceRegistrationAllowed: false,
            ProductCommandHandlersAllowed: false,
            UiProductActionsAllowed: false,
            DbProviderCloudNetworkAllowed: false,
            KmsWormExternalTrustAllowed: false,
            LiveAutomationAllowed: false,
            ReleaseCommercialReady: false,
            StatusText: LocalTempWriterStatus);
    }

    private static bool IsUnderSystemTemp(string path)
    {
        var candidate = Path.GetFullPath(path);
        var tempRoot = Path.GetFullPath(Path.GetTempPath());
        return candidate.StartsWith(tempRoot, StringComparison.OrdinalIgnoreCase);
    }

    private static string LocalTempLedgerPath(string root) =>
        Path.Combine(Path.GetFullPath(root), LedgerFileName);

    private static string LocalTempCheckpointPath(string root) =>
        Path.Combine(Path.GetFullPath(root), CheckpointFileName);

    private static string HashLedger(IReadOnlyList<ProductLedgerPathLocalTempWriterEntry> entries)
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
}
