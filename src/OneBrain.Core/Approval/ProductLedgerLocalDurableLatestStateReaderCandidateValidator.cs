using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OneBrain.Core.Approval;

public enum ProductLedgerLocalDurableLatestStateReaderCandidateDecision
{
    Rejected,
    ValidatedCandidateNotAuthority
}

public enum ProductLedgerLocalDurableLatestStateReaderCandidateState
{
    Pending,
    CandidateUnavailable,
    CandidateValidatedNotAuthority,
    CandidateBlocked
}

public enum ProductLedgerLocalDurableLatestStateReaderCandidateBlocker
{
    MissingRequest,
    MissingExplicitReaderCandidateScope,
    NonDevelopmentMode,
    NonLocalMode,
    NonInternalMode,
    BoundaryRejected,
    QueryOverrideRejected,
    HeaderOverrideRejected,
    MissingSourceManifest,
    SourceManifestNotCreated,
    SourceManifestPathRejected,
    SourceManifestFileMissing,
    SourceManifestCorrupt,
    SourceManifestHashMismatch,
    SourceManifestCheckpointMismatch,
    SourceManifestClaimsAuthority,
    SourceManifestClaimsLatestPointer,
    SourceManifestClaimsReadPrecedence,
    MissingSnapshotEntry,
    SourceSnapshotPathRejected,
    SourceSnapshotFileMissing,
    SourceSnapshotCorrupt,
    SourceSnapshotHashMismatch,
    SourceSnapshotCheckpointMismatch,
    SourceSnapshotNotHistoricalEvidence,
    SourceSnapshotClaimsAuthority,
    MissingEvidenceReferences,
    UnsafeMetadata,
    PathCanonicalizationFailed,
    ReparsePointRejected,
    IoFailure
}

public sealed record ProductLedgerLocalDurableLatestStateReaderCandidateOptions(
    string WorkspaceRootPath,
    bool ExplicitReaderCandidateBoundary,
    bool AllowsArbitraryPathInput,
    bool AllowsFilesystemScan,
    bool AllowsLatestPointer,
    bool AllowsLatestPointerOverwrite,
    bool AllowsReadPrecedence,
    bool AllowsAuthority,
    bool AllowsProductAuthority,
    bool AllowsPublicProduct,
    bool AllowsProductionRoute,
    bool AllowsShellOrSubprocess,
    bool AllowsCommandExecution,
    bool AllowsNetwork,
    bool AllowsDb,
    bool AllowsKmsWormExternalTrust,
    bool AllowsReleaseCommercial);

public sealed record ProductLedgerLocalDurableLatestStateReaderCandidateRequest(
    bool ExplicitReaderCandidateScope,
    bool DevelopmentMode,
    bool LocalMode,
    bool InternalMode,
    ProductLedgerLocalOperatorSurfaceLatestStateManifestResult? SourceManifest,
    bool QueryOverridePresent,
    bool HeaderOverridePresent);

public sealed record ProductLedgerLocalDurableLatestStateReaderCandidateValidation(
    bool ManifestSchemaValid,
    bool ManifestHashValid,
    bool ManifestCheckpointValid,
    bool SnapshotSchemaValid,
    bool SnapshotHashValid,
    bool SnapshotCheckpointValid,
    bool SafeRelativePathsOnly,
    bool StaleAware,
    bool TamperDetected,
    bool CorruptionDetected,
    bool NotAuthority);

public sealed record ProductLedgerLocalDurableLatestStateReaderCandidateResult(
    ProductLedgerLocalDurableLatestStateReaderCandidateDecision Decision,
    ProductLedgerLocalDurableLatestStateReaderCandidateState State,
    IReadOnlyList<ProductLedgerLocalDurableLatestStateReaderCandidateBlocker> Blockers,
    string CandidateId,
    DateTimeOffset CreatedAtUtc,
    string SourceManifestId,
    string SourceManifestRelativePath,
    string SourceManifestHash,
    string SourceManifestHashPrefix,
    string SourceManifestCheckpointHash,
    string SourceManifestCheckpointHashPrefix,
    IReadOnlyList<string> SourceSnapshotIds,
    IReadOnlyList<string> SourceSnapshotRelativePaths,
    IReadOnlyList<string> SourceSnapshotHashes,
    IReadOnlyList<string> SourceSnapshotHashPrefixes,
    IReadOnlyList<string> SourceEvidenceRefs,
    ProductLedgerLocalDurableLatestStateReaderCandidateValidation Validation,
    string ValidationState,
    string StaleState,
    string TamperState,
    string CorruptionState,
    string Classification,
    IReadOnlyList<string> NegativeFlags,
    bool LocalOnly,
    bool InternalOnly,
    bool DevelopmentOnly,
    bool ReadOnly,
    bool CandidateEvidenceOnly,
    bool Authority,
    bool LiveAuthority,
    bool ProductAuthority,
    bool ReadPrecedence,
    bool LatestPointer,
    bool LatestPointerOverwrite,
    bool ProductionAllowed,
    bool PublicProductAllowed,
    bool ShellAllowed,
    bool CommandExecutionAllowed,
    bool ProviderCloudNetworkAvailable,
    bool DbMigrationAvailable,
    bool KmsWormExternalTrustAvailable,
    bool BrowserCdpWcuOcrRecipesLiveAvailable,
    bool PilotRunAvailable,
    bool ReleaseCommercialReady,
    bool ComplianceCustody,
    bool CloudBackedDurability,
    string SafeNextStep,
    string StatusText)
{
    public static ProductLedgerLocalDurableLatestStateReaderCandidateResult Pending { get; } =
        new(
            Decision: ProductLedgerLocalDurableLatestStateReaderCandidateDecision.Rejected,
            State: ProductLedgerLocalDurableLatestStateReaderCandidateState.Pending,
            Blockers: [],
            CandidateId: "durable-latest-state-reader-candidate.pending",
            CreatedAtUtc: DateTimeOffset.UnixEpoch,
            SourceManifestId: "none",
            SourceManifestRelativePath: string.Empty,
            SourceManifestHash: string.Empty,
            SourceManifestHashPrefix: "none",
            SourceManifestCheckpointHash: string.Empty,
            SourceManifestCheckpointHashPrefix: "none",
            SourceSnapshotIds: [],
            SourceSnapshotRelativePaths: [],
            SourceSnapshotHashes: [],
            SourceSnapshotHashPrefixes: [],
            SourceEvidenceRefs: ["product-ledger-durable-latest-state-reader-candidate-pending"],
            Validation: ProductLedgerLocalDurableLatestStateReaderCandidateValidator.PendingValidation,
            ValidationState: "pending",
            StaleState: "stale-aware-pending",
            TamperState: "not-evaluated",
            CorruptionState: "not-evaluated",
            Classification: ProductLedgerLocalDurableLatestStateReaderCandidateValidator.Classification,
            NegativeFlags: ProductLedgerLocalDurableLatestStateReaderCandidateValidator.NegativeFlags,
            LocalOnly: true,
            InternalOnly: true,
            DevelopmentOnly: true,
            ReadOnly: true,
            CandidateEvidenceOnly: true,
            Authority: false,
            LiveAuthority: false,
            ProductAuthority: false,
            ReadPrecedence: false,
            LatestPointer: false,
            LatestPointerOverwrite: false,
            ProductionAllowed: false,
            PublicProductAllowed: false,
            ShellAllowed: false,
            CommandExecutionAllowed: false,
            ProviderCloudNetworkAvailable: false,
            DbMigrationAvailable: false,
            KmsWormExternalTrustAvailable: false,
            BrowserCdpWcuOcrRecipesLiveAvailable: false,
            PilotRunAvailable: false,
            ReleaseCommercialReady: false,
            ComplianceCustody: false,
            CloudBackedDurability: false,
            SafeNextStep: "explicit-go-required-before-read-precedence-or-product-authority",
            StatusText: ProductLedgerLocalDurableLatestStateReaderCandidateValidator.PendingStatus);
}

public sealed class ProductLedgerLocalDurableLatestStateReaderCandidateValidator
{
    public const string Classification = "LOCAL_INTERNAL_DEV_ONLY_READER_CANDIDATE_NOT_AUTHORITY";
    public const string ScopeId = "LocalDurableLatestStateReaderCandidateNotAuthority";

    public const string PendingStatus =
        "PRODUCT_LEDGER_DURABLE_LATEST_STATE_READER_CANDIDATE_PENDING LOCAL_ONLY INTERNAL_ONLY DEVELOPMENT_ONLY READ_ONLY CANDIDATE_EVIDENCE_ONLY NOT_AUTHORITY NO_LATEST_POINTER NO_READ_PRECEDENCE NO_PUBLIC_PRODUCT_PATH NO_PRODUCTION_ROUTE";

    public const string ValidatedStatus =
        "PRODUCT_LEDGER_DURABLE_LATEST_STATE_READER_CANDIDATE_VALIDATED LOCAL_ONLY INTERNAL_ONLY DEVELOPMENT_ONLY READ_ONLY CANDIDATE_EVIDENCE_ONLY NOT_AUTHORITY NOT_LIVE_AUTHORITY NOT_PRODUCT_AUTHORITY STALE_AWARE TAMPER_CHECKED CORRUPTION_CHECKED NO_LATEST_POINTER NO_LATEST_POINTER_OVERWRITE NO_READ_PRECEDENCE NO_PUBLIC_PRODUCT_PATH NO_PRODUCTION_ROUTE NO_COMMAND_EXECUTION NO_SHELL_SUBPROCESS NO_PROVIDER_CLOUD_NETWORK NO_DB_MIGRATION NO_KMS_WORM_EXTERNAL_TRUST NO_LIVE_AUTOMATION NO_RELEASE_COMMERCIAL";

    public const string RejectedStatus =
        "PRODUCT_LEDGER_DURABLE_LATEST_STATE_READER_CANDIDATE_REJECTED FAIL_CLOSED READ_ONLY CANDIDATE_EVIDENCE_ONLY NOT_AUTHORITY NO_LATEST_POINTER NO_READ_PRECEDENCE NO_PUBLIC_PRODUCT_PATH NO_PRODUCTION_ROUTE NO_COMMAND_EXECUTION NO_SHELL_SUBPROCESS NO_PROVIDER_CLOUD_NETWORK NO_DB_MIGRATION NO_KMS_WORM_EXTERNAL_TRUST NO_LIVE_AUTOMATION NO_RELEASE_COMMERCIAL";

    public static ProductLedgerLocalDurableLatestStateReaderCandidateValidation PendingValidation { get; } =
        new(
            ManifestSchemaValid: false,
            ManifestHashValid: false,
            ManifestCheckpointValid: false,
            SnapshotSchemaValid: false,
            SnapshotHashValid: false,
            SnapshotCheckpointValid: false,
            SafeRelativePathsOnly: false,
            StaleAware: true,
            TamperDetected: false,
            CorruptionDetected: false,
            NotAuthority: true);

    public static IReadOnlyList<string> NegativeFlags { get; } =
    [
        "not production",
        "not public/product",
        "not authority",
        "not live authority",
        "not product authority",
        "no latest pointer",
        "no latest pointer overwrite",
        "no read precedence",
        "candidate evidence only",
        "stale-aware",
        "no shell/subprocess",
        "no command execution",
        "no cloud/network/DB",
        "no KMS/WORM/compliance custody",
        "no Browser/CDP/WCU/OCR/Recipes live",
        "no Pilot /run",
        "no release/commercial",
        "read-only"
    ];

    private static readonly JsonSerializerOptions CandidateJsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never
    };

    private readonly ProductLedgerLocalDurableLatestStateReaderCandidateOptions options;
    private ProductLedgerLocalDurableLatestStateReaderCandidateResult? lastCandidate;

    public ProductLedgerLocalDurableLatestStateReaderCandidateValidator(
        ProductLedgerLocalDurableLatestStateReaderCandidateOptions options)
    {
        this.options = options;
    }

    public ProductLedgerLocalDurableLatestStateReaderCandidateResult Validate(
        ProductLedgerLocalDurableLatestStateReaderCandidateRequest? request)
    {
        var blockers = ValidateRequestShell(request);
        if (blockers.Count > 0 || request is null || request.SourceManifest is null)
        {
            return Remember(Rejected(blockers, request?.SourceManifest));
        }

        var canonicalRoot = CanonicalRoot();
        if (canonicalRoot is null)
        {
            return Remember(Rejected([ProductLedgerLocalDurableLatestStateReaderCandidateBlocker.BoundaryRejected], request.SourceManifest));
        }

        var manifestPath = FullPath(
            canonicalRoot,
            request.SourceManifest.ManifestRelativePath,
            ProductLedgerLocalOperatorSurfaceLatestStateManifestWriter.AllowedRelativeOutputBoundary);
        if (manifestPath is null)
        {
            return Remember(Rejected([ProductLedgerLocalDurableLatestStateReaderCandidateBlocker.SourceManifestPathRejected], request.SourceManifest));
        }

        try
        {
            if (ContainsReparsePoint(canonicalRoot)
                || ContainsReparsePoint(Path.Combine(canonicalRoot, "docs"))
                || ContainsReparsePoint(Path.Combine(canonicalRoot, "docs", "test-output"))
                || ContainsReparsePoint(Path.Combine(canonicalRoot, "docs", "test-output", "product-ledger"))
                || ContainsReparsePoint(Path.Combine(canonicalRoot, "docs", "test-output", "product-ledger", "operator-surface-latest-state-manifests"))
                || ContainsReparsePoint(Path.Combine(canonicalRoot, "docs", "test-output", "product-ledger", "operator-surface-latest-state-snapshots")))
            {
                return Remember(Rejected([ProductLedgerLocalDurableLatestStateReaderCandidateBlocker.ReparsePointRejected], request.SourceManifest));
            }

            if (!File.Exists(manifestPath))
            {
                return Remember(Rejected([ProductLedgerLocalDurableLatestStateReaderCandidateBlocker.SourceManifestFileMissing], request.SourceManifest));
            }

            var manifestJson = File.ReadAllText(manifestPath, Encoding.UTF8);
            if (!IsSafeSerializedEvidence(manifestJson))
            {
                return Remember(Rejected([ProductLedgerLocalDurableLatestStateReaderCandidateBlocker.UnsafeMetadata], request.SourceManifest));
            }

            ProductLedgerLocalOperatorSurfaceLatestStateManifestPayload? manifestPayload;
            try
            {
                manifestPayload = JsonSerializer.Deserialize<ProductLedgerLocalOperatorSurfaceLatestStateManifestPayload>(
                    manifestJson,
                    CandidateJsonOptions);
            }
            catch (JsonException)
            {
                return Remember(Rejected([ProductLedgerLocalDurableLatestStateReaderCandidateBlocker.SourceManifestCorrupt], request.SourceManifest, corruptionDetected: true));
            }

            var manifestBlockers = ManifestBlockers(manifestPayload, request.SourceManifest);
            if (manifestBlockers.Count > 0 || manifestPayload is null)
            {
                return Remember(Rejected(manifestBlockers, request.SourceManifest, corruptionDetected: manifestBlockers.Contains(ProductLedgerLocalDurableLatestStateReaderCandidateBlocker.SourceManifestCorrupt), tamperDetected: manifestBlockers.Contains(ProductLedgerLocalDurableLatestStateReaderCandidateBlocker.SourceManifestHashMismatch)));
            }

            var snapshotPayloads = new List<ProductLedgerLocalOperatorSurfaceLatestStateSnapshotPayload>();
            foreach (var entry in manifestPayload.Entries)
            {
                var snapshotPath = FullPath(
                    canonicalRoot,
                    entry.SourceSnapshotRelativePath,
                    ProductLedgerLocalOperatorSurfaceLatestStateSnapshotExecutor.AllowedRelativeOutputBoundary);
                if (snapshotPath is null)
                {
                    return Remember(Rejected([ProductLedgerLocalDurableLatestStateReaderCandidateBlocker.SourceSnapshotPathRejected], request.SourceManifest));
                }

                if (!File.Exists(snapshotPath))
                {
                    return Remember(Rejected([ProductLedgerLocalDurableLatestStateReaderCandidateBlocker.SourceSnapshotFileMissing], request.SourceManifest));
                }

                var snapshotJson = File.ReadAllText(snapshotPath, Encoding.UTF8);
                if (!IsSafeSerializedEvidence(snapshotJson))
                {
                    return Remember(Rejected([ProductLedgerLocalDurableLatestStateReaderCandidateBlocker.UnsafeMetadata], request.SourceManifest));
                }

                ProductLedgerLocalOperatorSurfaceLatestStateSnapshotPayload? snapshotPayload;
                try
                {
                    snapshotPayload = JsonSerializer.Deserialize<ProductLedgerLocalOperatorSurfaceLatestStateSnapshotPayload>(
                        snapshotJson,
                        CandidateJsonOptions);
                }
                catch (JsonException)
                {
                    return Remember(Rejected([ProductLedgerLocalDurableLatestStateReaderCandidateBlocker.SourceSnapshotCorrupt], request.SourceManifest, corruptionDetected: true));
                }

                var snapshotBlockers = SnapshotBlockers(snapshotPayload, entry);
                if (snapshotBlockers.Count > 0 || snapshotPayload is null)
                {
                    return Remember(Rejected(snapshotBlockers, request.SourceManifest, corruptionDetected: snapshotBlockers.Contains(ProductLedgerLocalDurableLatestStateReaderCandidateBlocker.SourceSnapshotCorrupt), tamperDetected: snapshotBlockers.Contains(ProductLedgerLocalDurableLatestStateReaderCandidateBlocker.SourceSnapshotHashMismatch)));
                }

                snapshotPayloads.Add(snapshotPayload);
            }

            return Remember(Validated(request.SourceManifest, manifestPayload, snapshotPayloads));
        }
        catch (IOException)
        {
            return Remember(Rejected([ProductLedgerLocalDurableLatestStateReaderCandidateBlocker.IoFailure], request.SourceManifest));
        }
        catch (UnauthorizedAccessException)
        {
            return Remember(Rejected([ProductLedgerLocalDurableLatestStateReaderCandidateBlocker.IoFailure], request.SourceManifest));
        }
        catch (ArgumentException)
        {
            return Remember(Rejected([ProductLedgerLocalDurableLatestStateReaderCandidateBlocker.PathCanonicalizationFailed], request.SourceManifest));
        }
    }

    public ProductLedgerLocalDurableLatestStateReaderCandidateResult Read() =>
        lastCandidate ?? ProductLedgerLocalDurableLatestStateReaderCandidateResult.Pending;

    private IReadOnlyList<ProductLedgerLocalDurableLatestStateReaderCandidateBlocker> ValidateRequestShell(
        ProductLedgerLocalDurableLatestStateReaderCandidateRequest? request)
    {
        var blockers = new List<ProductLedgerLocalDurableLatestStateReaderCandidateBlocker>();
        if (request is null)
        {
            blockers.Add(ProductLedgerLocalDurableLatestStateReaderCandidateBlocker.MissingRequest);
            return blockers;
        }

        if (!BoundaryAllowed()) blockers.Add(ProductLedgerLocalDurableLatestStateReaderCandidateBlocker.BoundaryRejected);
        if (!request.ExplicitReaderCandidateScope) blockers.Add(ProductLedgerLocalDurableLatestStateReaderCandidateBlocker.MissingExplicitReaderCandidateScope);
        if (!request.DevelopmentMode) blockers.Add(ProductLedgerLocalDurableLatestStateReaderCandidateBlocker.NonDevelopmentMode);
        if (!request.LocalMode) blockers.Add(ProductLedgerLocalDurableLatestStateReaderCandidateBlocker.NonLocalMode);
        if (!request.InternalMode) blockers.Add(ProductLedgerLocalDurableLatestStateReaderCandidateBlocker.NonInternalMode);
        if (request.QueryOverridePresent) blockers.Add(ProductLedgerLocalDurableLatestStateReaderCandidateBlocker.QueryOverrideRejected);
        if (request.HeaderOverridePresent) blockers.Add(ProductLedgerLocalDurableLatestStateReaderCandidateBlocker.HeaderOverrideRejected);
        if (request.SourceManifest is null)
        {
            blockers.Add(ProductLedgerLocalDurableLatestStateReaderCandidateBlocker.MissingSourceManifest);
        }
        else if (request.SourceManifest.Decision != ProductLedgerLocalOperatorSurfaceLatestStateManifestDecision.ManifestCreatedLocalOnly
            && request.SourceManifest.Decision != ProductLedgerLocalOperatorSurfaceLatestStateManifestDecision.IdempotentReplay)
        {
            blockers.Add(ProductLedgerLocalDurableLatestStateReaderCandidateBlocker.SourceManifestNotCreated);
        }

        return blockers.Distinct().OrderBy(blocker => blocker.ToString(), StringComparer.Ordinal).ToArray();
    }

    private static IReadOnlyList<ProductLedgerLocalDurableLatestStateReaderCandidateBlocker> ManifestBlockers(
        ProductLedgerLocalOperatorSurfaceLatestStateManifestPayload? payload,
        ProductLedgerLocalOperatorSurfaceLatestStateManifestResult sourceManifest)
    {
        var blockers = ProductLedgerLocalOperatorSurfaceLatestStateManifestWriter.ValidatePayload(payload)
            .Select(_ => ProductLedgerLocalDurableLatestStateReaderCandidateBlocker.SourceManifestCorrupt)
            .ToList();
        if (payload is null)
        {
            blockers.Add(ProductLedgerLocalDurableLatestStateReaderCandidateBlocker.SourceManifestCorrupt);
            return blockers.Distinct().ToArray();
        }

        if (!IsSafeManifestRelativePath(payload.SafeRelativeManifestPath)
            || !string.Equals(payload.SafeRelativeManifestPath, sourceManifest.ManifestRelativePath, StringComparison.Ordinal))
        {
            blockers.Add(ProductLedgerLocalDurableLatestStateReaderCandidateBlocker.SourceManifestPathRejected);
        }

        if (!string.Equals(payload.ManifestContentHash, ManifestContentHash(payload), StringComparison.Ordinal)
            || !string.Equals(payload.ManifestContentHash, sourceManifest.ManifestContentHash, StringComparison.Ordinal))
        {
            blockers.Add(ProductLedgerLocalDurableLatestStateReaderCandidateBlocker.SourceManifestHashMismatch);
        }

        if (!string.Equals(payload.CheckpointHash, sourceManifest.CheckpointHash, StringComparison.Ordinal))
        {
            blockers.Add(ProductLedgerLocalDurableLatestStateReaderCandidateBlocker.SourceManifestCheckpointMismatch);
        }

        if (payload.AuthorityLiveProduct || payload.ProductAuthority || payload.PublicProduct || payload.Production)
        {
            blockers.Add(ProductLedgerLocalDurableLatestStateReaderCandidateBlocker.SourceManifestClaimsAuthority);
        }

        if (payload.LatestPointer || payload.LatestPointerOverwrite)
        {
            blockers.Add(ProductLedgerLocalDurableLatestStateReaderCandidateBlocker.SourceManifestClaimsLatestPointer);
        }

        if (payload.ReadPrecedence)
        {
            blockers.Add(ProductLedgerLocalDurableLatestStateReaderCandidateBlocker.SourceManifestClaimsReadPrecedence);
        }

        if (payload.Entries.Count == 0)
        {
            blockers.Add(ProductLedgerLocalDurableLatestStateReaderCandidateBlocker.MissingSnapshotEntry);
        }

        if (payload.EvidenceRefs.Count == 0)
        {
            blockers.Add(ProductLedgerLocalDurableLatestStateReaderCandidateBlocker.MissingEvidenceReferences);
        }

        foreach (var text in payload.EvidenceRefs
            .Concat(payload.NegativeFlags)
            .Concat(payload.Entries.Select(entry => entry.SourceSnapshotRelativePath)))
        {
            if (!IsSafeMetadata(text))
            {
                blockers.Add(ProductLedgerLocalDurableLatestStateReaderCandidateBlocker.UnsafeMetadata);
            }
        }

        return blockers.Distinct().OrderBy(blocker => blocker.ToString(), StringComparer.Ordinal).ToArray();
    }

    private static IReadOnlyList<ProductLedgerLocalDurableLatestStateReaderCandidateBlocker> SnapshotBlockers(
        ProductLedgerLocalOperatorSurfaceLatestStateSnapshotPayload? payload,
        ProductLedgerLocalOperatorSurfaceLatestStateManifestEntry entry)
    {
        var blockers = new List<ProductLedgerLocalDurableLatestStateReaderCandidateBlocker>();
        if (payload is null
            || payload.SourceChainIds is null
            || payload.SourceChainContentHashes is null
            || payload.SafeRelativePathsOnly is null
            || payload.EvidenceRefs is null
            || payload.NegativeFlags is null)
        {
            blockers.Add(ProductLedgerLocalDurableLatestStateReaderCandidateBlocker.SourceSnapshotCorrupt);
            return blockers;
        }

        if (payload.SchemaVersion != 1
            || !string.Equals(payload.Classification, ProductLedgerLocalOperatorSurfaceLatestStateSnapshotExecutor.Classification, StringComparison.Ordinal)
            || !string.Equals(payload.StaleStateClassification, ProductLedgerLocalOperatorSurfaceLatestStateSnapshotExecutor.StaleStateClassification, StringComparison.Ordinal)
            || string.IsNullOrWhiteSpace(payload.SnapshotContentHash)
            || string.IsNullOrWhiteSpace(payload.CheckpointHash)
            || string.IsNullOrWhiteSpace(payload.SafeRelativeSnapshotPath)
            || payload.EvidenceRefs.Count == 0)
        {
            blockers.Add(ProductLedgerLocalDurableLatestStateReaderCandidateBlocker.SourceSnapshotCorrupt);
        }

        if (!IsSafeSnapshotRelativePath(payload.SafeRelativeSnapshotPath)
            || !string.Equals(payload.SafeRelativeSnapshotPath, entry.SourceSnapshotRelativePath, StringComparison.Ordinal))
        {
            blockers.Add(ProductLedgerLocalDurableLatestStateReaderCandidateBlocker.SourceSnapshotPathRejected);
        }

        if (!string.Equals(payload.SnapshotContentHash, SnapshotContentHash(payload), StringComparison.Ordinal)
            || !string.Equals(payload.SnapshotContentHash, entry.SourceSnapshotContentHash, StringComparison.Ordinal))
        {
            blockers.Add(ProductLedgerLocalDurableLatestStateReaderCandidateBlocker.SourceSnapshotHashMismatch);
        }

        if (!string.Equals(payload.CheckpointHash, entry.SourceSnapshotCheckpointHash, StringComparison.Ordinal))
        {
            blockers.Add(ProductLedgerLocalDurableLatestStateReaderCandidateBlocker.SourceSnapshotCheckpointMismatch);
        }

        if (!payload.HistoricalEvidenceOnly)
        {
            blockers.Add(ProductLedgerLocalDurableLatestStateReaderCandidateBlocker.SourceSnapshotNotHistoricalEvidence);
        }

        if (payload.AuthorityLiveProduct || payload.PublicProduct || payload.Production)
        {
            blockers.Add(ProductLedgerLocalDurableLatestStateReaderCandidateBlocker.SourceSnapshotClaimsAuthority);
        }

        foreach (var text in payload.EvidenceRefs
            .Concat(payload.NegativeFlags)
            .Concat(payload.SafeRelativePathsOnly)
            .Concat([payload.SafeRelativeSnapshotPath]))
        {
            if (!IsSafeMetadata(text))
            {
                blockers.Add(ProductLedgerLocalDurableLatestStateReaderCandidateBlocker.UnsafeMetadata);
            }
        }

        return blockers.Distinct().OrderBy(blocker => blocker.ToString(), StringComparer.Ordinal).ToArray();
    }

    private ProductLedgerLocalDurableLatestStateReaderCandidateResult Validated(
        ProductLedgerLocalOperatorSurfaceLatestStateManifestResult manifest,
        ProductLedgerLocalOperatorSurfaceLatestStateManifestPayload manifestPayload,
        IReadOnlyList<ProductLedgerLocalOperatorSurfaceLatestStateSnapshotPayload> snapshots)
    {
        var evidenceRefs = manifestPayload.EvidenceRefs
            .Concat(snapshots.SelectMany(snapshot => snapshot.EvidenceRefs))
            .Concat(["product-ledger-durable-latest-state-reader-candidate-validated-not-authority"])
            .Distinct(StringComparer.Ordinal)
            .OrderBy(evidence => evidence, StringComparer.Ordinal)
            .ToArray();
        var snapshotIds = snapshots.Select(snapshot => snapshot.SnapshotId).ToArray();
        var snapshotPaths = snapshots.Select(snapshot => snapshot.SafeRelativeSnapshotPath).ToArray();
        var snapshotHashes = snapshots.Select(snapshot => snapshot.SnapshotContentHash).ToArray();
        var candidateId = "durable-latest-state-reader-candidate-" + Prefix(manifest.ManifestContentHash);
        return new(
            Decision: ProductLedgerLocalDurableLatestStateReaderCandidateDecision.ValidatedCandidateNotAuthority,
            State: ProductLedgerLocalDurableLatestStateReaderCandidateState.CandidateValidatedNotAuthority,
            Blockers: [],
            CandidateId: candidateId,
            CreatedAtUtc: DateTimeOffset.UnixEpoch,
            SourceManifestId: manifest.ManifestId,
            SourceManifestRelativePath: manifest.ManifestRelativePath,
            SourceManifestHash: manifest.ManifestContentHash,
            SourceManifestHashPrefix: Prefix(manifest.ManifestContentHash),
            SourceManifestCheckpointHash: manifest.CheckpointHash,
            SourceManifestCheckpointHashPrefix: Prefix(manifest.CheckpointHash),
            SourceSnapshotIds: snapshotIds,
            SourceSnapshotRelativePaths: snapshotPaths,
            SourceSnapshotHashes: snapshotHashes,
            SourceSnapshotHashPrefixes: snapshotHashes.Select(Prefix).ToArray(),
            SourceEvidenceRefs: evidenceRefs,
            Validation: new(
                ManifestSchemaValid: true,
                ManifestHashValid: true,
                ManifestCheckpointValid: true,
                SnapshotSchemaValid: true,
                SnapshotHashValid: true,
                SnapshotCheckpointValid: true,
                SafeRelativePathsOnly: true,
                StaleAware: true,
                TamperDetected: false,
                CorruptionDetected: false,
                NotAuthority: true),
            ValidationState: "validated-candidate-not-authority",
            StaleState: "stale-aware-historical-evidence-only",
            TamperState: "tamper-not-detected",
            CorruptionState: "corruption-not-detected",
            Classification: Classification,
            NegativeFlags: NegativeFlags,
            LocalOnly: true,
            InternalOnly: true,
            DevelopmentOnly: true,
            ReadOnly: true,
            CandidateEvidenceOnly: true,
            Authority: false,
            LiveAuthority: false,
            ProductAuthority: false,
            ReadPrecedence: false,
            LatestPointer: false,
            LatestPointerOverwrite: false,
            ProductionAllowed: false,
            PublicProductAllowed: false,
            ShellAllowed: false,
            CommandExecutionAllowed: false,
            ProviderCloudNetworkAvailable: false,
            DbMigrationAvailable: false,
            KmsWormExternalTrustAvailable: false,
            BrowserCdpWcuOcrRecipesLiveAvailable: false,
            PilotRunAvailable: false,
            ReleaseCommercialReady: false,
            ComplianceCustody: false,
            CloudBackedDurability: false,
            SafeNextStep: "explicit-go-required-before-read-precedence-or-product-authority",
            StatusText: ValidatedStatus);
    }

    private static ProductLedgerLocalDurableLatestStateReaderCandidateResult Rejected(
        IReadOnlyList<ProductLedgerLocalDurableLatestStateReaderCandidateBlocker> blockers,
        ProductLedgerLocalOperatorSurfaceLatestStateManifestResult? manifest,
        bool corruptionDetected = false,
        bool tamperDetected = false)
    {
        var distinct = blockers.Distinct().OrderBy(blocker => blocker.ToString(), StringComparer.Ordinal).ToArray();
        return ProductLedgerLocalDurableLatestStateReaderCandidateResult.Pending with
        {
            State = distinct.Contains(ProductLedgerLocalDurableLatestStateReaderCandidateBlocker.MissingSourceManifest)
                || distinct.Contains(ProductLedgerLocalDurableLatestStateReaderCandidateBlocker.SourceManifestNotCreated)
                    ? ProductLedgerLocalDurableLatestStateReaderCandidateState.CandidateUnavailable
                    : ProductLedgerLocalDurableLatestStateReaderCandidateState.CandidateBlocked,
            Blockers = distinct,
            SourceManifestId = manifest?.ManifestId ?? "none",
            SourceManifestRelativePath = manifest?.ManifestRelativePath ?? string.Empty,
            SourceManifestHash = manifest?.ManifestContentHash ?? string.Empty,
            SourceManifestHashPrefix = Prefix(manifest?.ManifestContentHash),
            SourceManifestCheckpointHash = manifest?.CheckpointHash ?? string.Empty,
            SourceManifestCheckpointHashPrefix = Prefix(manifest?.CheckpointHash),
            Validation = PendingValidation with
            {
                TamperDetected = tamperDetected,
                CorruptionDetected = corruptionDetected
            },
            ValidationState = "blocked-fail-closed",
            TamperState = tamperDetected ? "tamper-detected" : "tamper-not-validated",
            CorruptionState = corruptionDetected ? "corruption-detected" : "corruption-not-validated",
            StatusText = RejectedStatus
        };
    }

    private ProductLedgerLocalDurableLatestStateReaderCandidateResult Remember(
        ProductLedgerLocalDurableLatestStateReaderCandidateResult result)
    {
        lastCandidate = result;
        return result;
    }

    private bool BoundaryAllowed()
    {
        if (!options.ExplicitReaderCandidateBoundary
            || options.AllowsArbitraryPathInput
            || options.AllowsFilesystemScan
            || options.AllowsLatestPointer
            || options.AllowsLatestPointerOverwrite
            || options.AllowsReadPrecedence
            || options.AllowsAuthority
            || options.AllowsProductAuthority
            || options.AllowsPublicProduct
            || options.AllowsProductionRoute
            || options.AllowsShellOrSubprocess
            || options.AllowsCommandExecution
            || options.AllowsNetwork
            || options.AllowsDb
            || options.AllowsKmsWormExternalTrust
            || options.AllowsReleaseCommercial
            || string.IsNullOrWhiteSpace(options.WorkspaceRootPath))
        {
            return false;
        }

        var full = CanonicalRoot();
        if (full is null)
        {
            return false;
        }

        return full.IndexOf("..", StringComparison.Ordinal) < 0
            && !Path.GetPathRoot(full)!.Equals(full, StringComparison.OrdinalIgnoreCase);
    }

    private string? CanonicalRoot()
    {
        try
        {
            return Path.TrimEndingDirectorySeparator(Path.GetFullPath(options.WorkspaceRootPath));
        }
        catch (ArgumentException)
        {
            return null;
        }
        catch (NotSupportedException)
        {
            return null;
        }
    }

    private static string? FullPath(string canonicalRoot, string relativePath, string allowedBoundary)
    {
        try
        {
            if (!IsSafeRelativePath(relativePath, allowedBoundary))
            {
                return null;
            }

            var boundaryRoot = Path.GetFullPath(Path.Combine(canonicalRoot, allowedBoundary.Replace('/', Path.DirectorySeparatorChar)));
            var fullPath = Path.GetFullPath(Path.Combine(canonicalRoot, relativePath.Replace('/', Path.DirectorySeparatorChar)));
            return StartsWithPath(boundaryRoot, fullPath) && fullPath.EndsWith(".json", StringComparison.OrdinalIgnoreCase)
                ? fullPath
                : null;
        }
        catch (ArgumentException)
        {
            return null;
        }
        catch (NotSupportedException)
        {
            return null;
        }
    }

    private static bool StartsWithPath(string root, string path)
    {
        var normalizedRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath(root)) + Path.DirectorySeparatorChar;
        var normalizedPath = Path.GetFullPath(path);
        return normalizedPath.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase);
    }

    private static bool ContainsReparsePoint(string path)
    {
        try
        {
            return Directory.Exists(path)
                && (File.GetAttributes(path) & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint;
        }
        catch (IOException)
        {
            return true;
        }
        catch (UnauthorizedAccessException)
        {
            return true;
        }
    }

    private static bool IsSafeManifestRelativePath(string value) =>
        IsSafeRelativePath(value, ProductLedgerLocalOperatorSurfaceLatestStateManifestWriter.AllowedRelativeOutputBoundary);

    private static bool IsSafeSnapshotRelativePath(string value) =>
        IsSafeRelativePath(value, ProductLedgerLocalOperatorSurfaceLatestStateSnapshotExecutor.AllowedRelativeOutputBoundary);

    private static bool IsSafeRelativePath(string value, string allowedBoundary) =>
        value.StartsWith(allowedBoundary, StringComparison.Ordinal)
        && value.EndsWith(".json", StringComparison.Ordinal)
        && !value.EndsWith("/latest" + ".json", StringComparison.OrdinalIgnoreCase)
        && IsSafeMetadata(value);

    private static bool IsSafeSerializedEvidence(string value) =>
        !value.Contains(@"C:\Users\", StringComparison.OrdinalIgnoreCase)
        && !value.Contains(@"\\", StringComparison.Ordinal)
        && !value.Contains("password=", StringComparison.OrdinalIgnoreCase)
        && !value.Contains("api_key", StringComparison.OrdinalIgnoreCase)
        && !value.Contains("apikey", StringComparison.OrdinalIgnoreCase)
        && !value.Contains("secret=", StringComparison.OrdinalIgnoreCase)
        && !value.Contains("token=", StringComparison.OrdinalIgnoreCase)
        && !value.Contains("http://", StringComparison.OrdinalIgnoreCase)
        && !value.Contains("https://", StringComparison.OrdinalIgnoreCase);

    private static bool IsSafeMetadata(string value) =>
        !string.IsNullOrWhiteSpace(value)
        && !value.Contains("..", StringComparison.Ordinal)
        && !value.Contains(':', StringComparison.Ordinal)
        && !value.Contains('\\', StringComparison.Ordinal)
        && !value.Contains("password", StringComparison.OrdinalIgnoreCase)
        && !value.Contains("token", StringComparison.OrdinalIgnoreCase)
        && !value.Contains("secret", StringComparison.OrdinalIgnoreCase)
        && !value.Contains("http://", StringComparison.OrdinalIgnoreCase)
        && !value.Contains("https://", StringComparison.OrdinalIgnoreCase);

    private static string ManifestContentHash(ProductLedgerLocalOperatorSurfaceLatestStateManifestPayload payload) =>
        HashText(JsonSerializer.Serialize(payload with { ManifestContentHash = string.Empty }, CandidateJsonOptions));

    private static string SnapshotContentHash(ProductLedgerLocalOperatorSurfaceLatestStateSnapshotPayload payload) =>
        HashText(JsonSerializer.Serialize(payload with { SnapshotContentHash = string.Empty }, CandidateJsonOptions));

    private static string HashText(string material)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(material));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string Prefix(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "none" : value[..Math.Min(12, value.Length)];
}
