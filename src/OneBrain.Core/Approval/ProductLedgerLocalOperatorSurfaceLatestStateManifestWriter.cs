using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OneBrain.Core.Approval;

public enum ProductLedgerLocalOperatorSurfaceLatestStateManifestDecision
{
    Rejected,
    ManifestCreatedLocalOnly,
    IdempotentReplay
}

public enum ProductLedgerLocalOperatorSurfaceLatestStateManifestState
{
    Pending,
    ManifestCreatedLocalOnly,
    ManifestBlocked,
    ManifestReplayBlocked
}

public enum ProductLedgerLocalOperatorSurfaceLatestStateManifestActionKind
{
    LocalOperatorSurfaceLatestStateManifestCreateOnly
}

public enum ProductLedgerLocalOperatorSurfaceLatestStateManifestBlocker
{
    MissingRequest,
    MissingExplicitManifestScope,
    NonDevelopmentMode,
    NonLocalMode,
    NonInternalMode,
    OutputBoundaryRejected,
    MissingManifestId,
    MissingActionId,
    UnsafeManifestId,
    UnsafeActionId,
    UnknownActionKind,
    MissingSourceSnapshot,
    SourceSnapshotNotCreated,
    MissingExpectedSourceSnapshotHash,
    SourceSnapshotHashMismatch,
    MissingExpectedSourceSnapshotCheckpoint,
    SourceSnapshotCheckpointMismatch,
    SourceSnapshotNotHistoricalEvidence,
    SourceSnapshotAuthorityClaimed,
    SourceSnapshotPathRejected,
    MissingEvidenceReferences,
    RedactionRejected,
    PathCanonicalizationFailed,
    ReparsePointRejected,
    FinalPathOutsideAllowedBoundary,
    ExistingManifestConflict,
    ExistingManifestCorrupt,
    IoFailure,
    PayloadPathFieldRejected,
    PayloadCommandFieldRejected,
    PayloadNetworkProviderFieldRejected,
    ClaimsArbitraryPathInput,
    ClaimsFilesystemScan,
    RequestsOverwrite,
    RequestsLatestPointer,
    RequestsLatestPointerOverwrite,
    RequestsReadPrecedence,
    RequestsUserSelectedPath,
    RequestsPublicUiAction,
    RequestsProductCommandExecution,
    RequestsProductCommandHandler,
    RequestsProductiveServiceRegistration,
    RequestsShellOrSubprocess,
    ClaimsArbitraryCommandExecution,
    ClaimsProviderCloudNetwork,
    ClaimsDbMigration,
    ClaimsKmsWormExternalTrust,
    ClaimsBrowserCdpWcuOcrRecipesLive,
    ClaimsPilotRun,
    ClaimsReleaseCommercial,
    ClaimsLiveAuthority,
    ClaimsProductAuthority,
    ClaimsComplianceCustody,
    ClaimsCloudBackedDurability
}

public sealed record ProductLedgerLocalOperatorSurfaceLatestStateManifestOptions(
    string WorkspaceRootPath,
    bool ExplicitLatestStateManifestBoundary,
    bool AllowsArbitraryPathInput,
    bool AllowsFilesystemScan,
    bool AllowsOverwrite,
    bool AllowsLatestPointer,
    bool AllowsLatestPointerOverwrite,
    bool AllowsReadPrecedence,
    bool AllowsUserSelectedPath,
    bool AllowsShellOrSubprocess,
    bool AllowsCommandExecution,
    bool AllowsNetwork,
    bool AllowsDb,
    bool AllowsKmsWormExternalTrust,
    bool AllowsReleaseCommercial);

public sealed record ProductLedgerLocalOperatorSurfaceLatestStateManifestRequest(
    bool ExplicitLatestStateManifestScope,
    bool DevelopmentMode,
    bool LocalMode,
    bool InternalMode,
    string? ManifestId,
    string? ActionId,
    ProductLedgerLocalOperatorSurfaceLatestStateManifestActionKind? ActionKind,
    ProductLedgerLocalOperatorSurfaceLatestStateSnapshotResult? SourceSnapshot,
    string? ExpectedSourceSnapshotContentHash,
    string? ExpectedSourceSnapshotCheckpointHash,
    IReadOnlyList<string>? EvidenceReferences,
    string? ProposedPath,
    string? ProposedRoot,
    string? ProposedFilename,
    string? ProposedCommand,
    string? ProposedUrl,
    string? ProposedProvider,
    string? ProposedDbMigration,
    bool ClaimsArbitraryPathInput,
    bool ClaimsFilesystemScan,
    bool RequestsOverwrite,
    bool RequestsLatestPointer,
    bool RequestsLatestPointerOverwrite,
    bool RequestsReadPrecedence,
    bool RequestsUserSelectedPath,
    bool RequestsPublicUiAction,
    bool RequestsProductCommandExecution,
    bool RequestsProductCommandHandler,
    bool RequestsProductiveServiceRegistration,
    bool RequestsShellOrSubprocess,
    bool ClaimsArbitraryCommandExecution,
    bool ClaimsProviderCloudNetwork,
    bool ClaimsDbMigration,
    bool ClaimsKmsWormExternalTrust,
    bool ClaimsBrowserCdpWcuOcrRecipesLive,
    bool ClaimsPilotRun,
    bool ClaimsReleaseCommercial,
    bool ClaimsLiveAuthority,
    bool ClaimsProductAuthority,
    bool ClaimsComplianceCustody,
    bool ClaimsCloudBackedDurability);

public sealed record ProductLedgerLocalOperatorSurfaceLatestStateManifestResult(
    ProductLedgerLocalOperatorSurfaceLatestStateManifestDecision Decision,
    ProductLedgerLocalOperatorSurfaceLatestStateManifestState State,
    IReadOnlyList<ProductLedgerLocalOperatorSurfaceLatestStateManifestBlocker> Blockers,
    string ManifestId,
    string ActionId,
    string ManifestRelativePath,
    string AllowedBoundary,
    string InputSnapshotBoundary,
    string Classification,
    string StalePolicy,
    string SourceSnapshotId,
    string SourceSnapshotRelativePath,
    string SourceSnapshotContentHash,
    string SourceSnapshotContentHashPrefix,
    string SourceSnapshotCheckpointHash,
    string SourceSnapshotCheckpointHashPrefix,
    string ManifestContentHash,
    string ManifestContentHashPrefix,
    string CheckpointHash,
    string CheckpointHashPrefix,
    IReadOnlyList<ProductLedgerLocalOperatorSurfaceLatestStateManifestEntry> Entries,
    IReadOnlyList<string> EvidenceRefs,
    IReadOnlyList<string> NegativeFlags,
    bool CreateOnly,
    bool Versioned,
    bool JsonOnly,
    bool OverwriteAllowed,
    bool LatestPointerAvailable,
    bool LatestPointerOverwriteAllowed,
    bool ReadPrecedenceAllowed,
    bool UserSelectedPathAllowed,
    bool PayloadControlledRootAllowed,
    bool CanonicalizationPassed,
    bool ReparseValidationPassed,
    bool RedactionApplied,
    bool LocalOnly,
    bool InternalOnly,
    bool DevelopmentOnly,
    bool HistoricalIndexEvidenceOnly,
    bool AuthorityLiveProduct,
    bool ProductAuthority,
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
    DateTimeOffset CreatedAtUtc,
    string StatusText)
{
    public static ProductLedgerLocalOperatorSurfaceLatestStateManifestResult Pending { get; } =
        new(
            Decision: ProductLedgerLocalOperatorSurfaceLatestStateManifestDecision.Rejected,
            State: ProductLedgerLocalOperatorSurfaceLatestStateManifestState.Pending,
            Blockers: [],
            ManifestId: "operator-surface-latest-state-manifest.pending",
            ActionId: "operator-surface-latest-state-manifest-action.pending",
            ManifestRelativePath: string.Empty,
            AllowedBoundary: ProductLedgerLocalOperatorSurfaceLatestStateManifestWriter.AllowedRelativeOutputBoundary,
            InputSnapshotBoundary: ProductLedgerLocalOperatorSurfaceLatestStateSnapshotExecutor.AllowedRelativeOutputBoundary,
            Classification: ProductLedgerLocalOperatorSurfaceLatestStateManifestWriter.Classification,
            StalePolicy: ProductLedgerLocalOperatorSurfaceLatestStateManifestWriter.StalePolicy,
            SourceSnapshotId: "none",
            SourceSnapshotRelativePath: string.Empty,
            SourceSnapshotContentHash: string.Empty,
            SourceSnapshotContentHashPrefix: "none",
            SourceSnapshotCheckpointHash: string.Empty,
            SourceSnapshotCheckpointHashPrefix: "none",
            ManifestContentHash: string.Empty,
            ManifestContentHashPrefix: "none",
            CheckpointHash: string.Empty,
            CheckpointHashPrefix: "none",
            Entries: [],
            EvidenceRefs: ["product-ledger-operator-surface-latest-state-manifest-pending"],
            NegativeFlags: ProductLedgerLocalOperatorSurfaceLatestStateManifestWriter.NegativeFlags,
            CreateOnly: true,
            Versioned: true,
            JsonOnly: true,
            OverwriteAllowed: false,
            LatestPointerAvailable: false,
            LatestPointerOverwriteAllowed: false,
            ReadPrecedenceAllowed: false,
            UserSelectedPathAllowed: false,
            PayloadControlledRootAllowed: false,
            CanonicalizationPassed: false,
            ReparseValidationPassed: false,
            RedactionApplied: false,
            LocalOnly: true,
            InternalOnly: true,
            DevelopmentOnly: true,
            HistoricalIndexEvidenceOnly: true,
            AuthorityLiveProduct: false,
            ProductAuthority: false,
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
            CreatedAtUtc: DateTimeOffset.UnixEpoch,
            StatusText: ProductLedgerLocalOperatorSurfaceLatestStateManifestWriter.PendingStatus);
}

public sealed record ProductLedgerLocalOperatorSurfaceLatestStateManifestEntry(
    string SourceSnapshotId,
    string SourceSnapshotRelativePath,
    string SourceSnapshotContentHash,
    string SourceSnapshotCheckpointHash,
    string SourceSnapshotClassification,
    string CandidateFreshness,
    bool HistoricalEvidenceOnly,
    bool NotAuthority);

public sealed class ProductLedgerLocalOperatorSurfaceLatestStateManifestWriter
{
    public const string AllowedRelativeOutputBoundary = "docs/test-output/product-ledger/operator-surface-latest-state-manifests/";
    public const string Classification = "LOCAL_INTERNAL_DEV_ONLY_VERSIONED_MANIFEST_NOT_AUTHORITY";
    public const string StalePolicy = "MANIFESTS_ARE_HISTORICAL_INDEX_EVIDENCE_ONLY_NOT_LIVE_PRODUCT_AUTHORITY";
    public const string ScopeId = "LocalOperatorSurfaceLatestStateManifestCreateOnly";

    public const string PendingStatus =
        "PRODUCT_LEDGER_OPERATOR_SURFACE_LATEST_STATE_MANIFEST_PENDING LOCAL_ONLY INTERNAL_ONLY DEVELOPMENT_ONLY VERSIONED_MANIFEST_NOT_AUTHORITY JSON_ONLY CREATE_ONLY NO_OVERWRITE NO_LATEST_POINTER NO_LATEST_POINTER_OVERWRITE NO_READ_PRECEDENCE NO_PUBLIC_PRODUCT_PATH NO_PRODUCTION_ROUTE NO_COMMAND_EXECUTION NO_SHELL_SUBPROCESS NO_PROVIDER_CLOUD_NETWORK NO_DB_MIGRATION NO_KMS_WORM_EXTERNAL_TRUST NO_LIVE_AUTOMATION NO_RELEASE_COMMERCIAL";

    public const string CompletedStatus =
        "PRODUCT_LEDGER_OPERATOR_SURFACE_LATEST_STATE_MANIFEST_CREATED LOCAL_ONLY INTERNAL_ONLY DEVELOPMENT_ONLY VERSIONED_MANIFEST_NOT_AUTHORITY HISTORICAL_INDEX_EVIDENCE_ONLY JSON_ONLY CREATE_ONLY NO_OVERWRITE NO_LATEST_POINTER NO_LATEST_POINTER_OVERWRITE NO_READ_PRECEDENCE REDACTION_APPLIED HASH_CHECKPOINTED NO_PUBLIC_PRODUCT_PATH NO_PRODUCTION_ROUTE NO_COMMAND_EXECUTION NO_SHELL_SUBPROCESS NO_PROVIDER_CLOUD_NETWORK NO_DB_MIGRATION NO_KMS_WORM_EXTERNAL_TRUST NO_LIVE_AUTOMATION NO_RELEASE_COMMERCIAL";

    public const string RejectedStatus =
        "PRODUCT_LEDGER_OPERATOR_SURFACE_LATEST_STATE_MANIFEST_REJECTED FAIL_CLOSED VERSIONED_MANIFEST_NOT_AUTHORITY HISTORICAL_INDEX_EVIDENCE_ONLY JSON_ONLY CREATE_ONLY NO_OVERWRITE NO_LATEST_POINTER NO_LATEST_POINTER_OVERWRITE NO_READ_PRECEDENCE NO_PUBLIC_PRODUCT_PATH NO_PRODUCTION_ROUTE NO_COMMAND_EXECUTION NO_SHELL_SUBPROCESS NO_PROVIDER_CLOUD_NETWORK NO_DB_MIGRATION NO_KMS_WORM_EXTERNAL_TRUST NO_LIVE_AUTOMATION NO_RELEASE_COMMERCIAL";

    public static IReadOnlyList<string> NegativeFlags { get; } =
    [
        "not production",
        "not public/product",
        "not live authority",
        "not product authority",
        "no latest pointer",
        "no latest pointer overwrite",
        "no read precedence",
        "manifests are historical index/evidence only",
        "no shell/subprocess",
        "no command execution",
        "no cloud/network/DB",
        "no KMS/WORM/compliance custody",
        "no Browser/CDP/WCU/OCR/Recipes live",
        "no Pilot /run",
        "no release/commercial",
        "no overwrite",
        "no user-selected path"
    ];

    private static readonly JsonSerializerOptions ManifestJsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never
    };

    private readonly ProductLedgerLocalOperatorSurfaceLatestStateManifestOptions options;
    private ProductLedgerLocalOperatorSurfaceLatestStateManifestResult? lastManifest;

    public ProductLedgerLocalOperatorSurfaceLatestStateManifestWriter(
        ProductLedgerLocalOperatorSurfaceLatestStateManifestOptions options)
    {
        this.options = options;
    }

    public ProductLedgerLocalOperatorSurfaceLatestStateManifestResult CreateManifest(
        ProductLedgerLocalOperatorSurfaceLatestStateManifestRequest? request)
    {
        var blockers = ValidateRequest(request);
        if (blockers.Count > 0 || request is null || request.SourceSnapshot is null)
        {
            return Remember(ManifestRejected(blockers));
        }

        var createdAt = DateTimeOffset.UnixEpoch;
        var canonicalRoot = CanonicalRoot();
        if (canonicalRoot is null)
        {
            return Remember(ManifestRejected([ProductLedgerLocalOperatorSurfaceLatestStateManifestBlocker.OutputBoundaryRejected]));
        }

        var relativePath = ManifestRelativePath(request);
        var fullPath = FullManifestPath(canonicalRoot, relativePath);
        if (fullPath is null)
        {
            return Remember(ManifestRejected([ProductLedgerLocalOperatorSurfaceLatestStateManifestBlocker.FinalPathOutsideAllowedBoundary]));
        }

        var evidenceRefs = EvidenceRefs(request);
        var entries = Entries(request.SourceSnapshot);
        var checkpointHash = HashText(request.SourceSnapshot.SnapshotContentHash + "|" + request.SourceSnapshot.CheckpointHash + "|" + string.Join("|", evidenceRefs));
        var payload = Payload(request, relativePath, entries, evidenceRefs, checkpointHash, createdAt);
        var contentHash = ContentHashForPayload(payload);
        var finalPayload = payload with { ManifestContentHash = contentHash };
        var finalJson = JsonSerializer.Serialize(finalPayload, ManifestJsonOptions);

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
            if (ContainsReparsePoint(canonicalRoot)
                || ContainsReparsePoint(Path.Combine(canonicalRoot, "docs"))
                || ContainsReparsePoint(Path.Combine(canonicalRoot, "docs", "test-output"))
                || ContainsReparsePoint(Path.Combine(canonicalRoot, "docs", "test-output", "product-ledger"))
                || ContainsReparsePoint(Path.GetDirectoryName(fullPath)!))
            {
                return Remember(ManifestRejected([ProductLedgerLocalOperatorSurfaceLatestStateManifestBlocker.ReparsePointRejected]));
            }

            if (File.Exists(fullPath))
            {
                ProductLedgerLocalOperatorSurfaceLatestStateManifestPayload? existingPayload;
                try
                {
                    var existing = File.ReadAllText(fullPath, Encoding.UTF8);
                    existingPayload = JsonSerializer.Deserialize<ProductLedgerLocalOperatorSurfaceLatestStateManifestPayload>(existing, ManifestJsonOptions);
                }
                catch (JsonException)
                {
                    return Remember(ManifestRejected([ProductLedgerLocalOperatorSurfaceLatestStateManifestBlocker.ExistingManifestCorrupt]));
                }

                var existingBlockers = ValidatePayload(existingPayload);
                if (existingBlockers.Count > 0)
                {
                    return Remember(ManifestRejected(existingBlockers));
                }

                if (existingPayload is not null
                    && string.Equals(existingPayload.ManifestContentHash, contentHash, StringComparison.Ordinal)
                    && ExistingPayloadMatches(existingPayload, request, relativePath, entries, evidenceRefs, checkpointHash))
                {
                    return Remember(ManifestFrom(
                        request,
                        relativePath,
                        entries,
                        evidenceRefs,
                        contentHash,
                        checkpointHash,
                        existingPayload.CreatedAtUtc,
                        ProductLedgerLocalOperatorSurfaceLatestStateManifestDecision.IdempotentReplay));
                }

                return Remember(ManifestRejected([ProductLedgerLocalOperatorSurfaceLatestStateManifestBlocker.ExistingManifestConflict]));
            }

            using (var stream = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            using (var writer = new StreamWriter(stream, Encoding.UTF8))
            {
                writer.Write(finalJson);
            }

            return Remember(ManifestFrom(
                request,
                relativePath,
                entries,
                evidenceRefs,
                contentHash,
                checkpointHash,
                createdAt,
                ProductLedgerLocalOperatorSurfaceLatestStateManifestDecision.ManifestCreatedLocalOnly));
        }
        catch (IOException)
        {
            return Remember(ManifestRejected([ProductLedgerLocalOperatorSurfaceLatestStateManifestBlocker.IoFailure]));
        }
        catch (UnauthorizedAccessException)
        {
            return Remember(ManifestRejected([ProductLedgerLocalOperatorSurfaceLatestStateManifestBlocker.IoFailure]));
        }
        catch (ArgumentException)
        {
            return Remember(ManifestRejected([ProductLedgerLocalOperatorSurfaceLatestStateManifestBlocker.PathCanonicalizationFailed]));
        }
    }

    public ProductLedgerLocalOperatorSurfaceLatestStateManifestResult Read() =>
        lastManifest ?? ProductLedgerLocalOperatorSurfaceLatestStateManifestResult.Pending;

    public static IReadOnlyList<ProductLedgerLocalOperatorSurfaceLatestStateManifestBlocker> ValidatePayload(
        ProductLedgerLocalOperatorSurfaceLatestStateManifestPayload? payload)
    {
        var blockers = new List<ProductLedgerLocalOperatorSurfaceLatestStateManifestBlocker>();
        if (payload is null)
        {
            blockers.Add(ProductLedgerLocalOperatorSurfaceLatestStateManifestBlocker.ExistingManifestCorrupt);
            return blockers;
        }

        if (payload.Entries is null
            || payload.EvidenceRefs is null
            || payload.NegativeFlags is null)
        {
            blockers.Add(ProductLedgerLocalOperatorSurfaceLatestStateManifestBlocker.ExistingManifestCorrupt);
            return blockers;
        }

        if (payload.SchemaVersion != 1
            || !string.Equals(payload.Classification, Classification, StringComparison.Ordinal)
            || !string.Equals(payload.StalePolicy, StalePolicy, StringComparison.Ordinal)
            || string.IsNullOrWhiteSpace(payload.ManifestContentHash)
            || string.IsNullOrWhiteSpace(payload.CheckpointHash)
            || payload.EvidenceRefs.Count == 0
            || payload.Entries.Count == 0)
        {
            blockers.Add(ProductLedgerLocalOperatorSurfaceLatestStateManifestBlocker.ExistingManifestCorrupt);
        }

        if (payload.AuthorityLiveProduct
            || payload.ProductAuthority
            || payload.PublicProduct
            || payload.Production
            || payload.ReleaseCommercial
            || payload.ComplianceCustody
            || payload.CloudBackedDurability
            || payload.LatestPointer
            || payload.LatestPointerOverwrite
            || payload.ReadPrecedence)
        {
            blockers.Add(ProductLedgerLocalOperatorSurfaceLatestStateManifestBlocker.SourceSnapshotAuthorityClaimed);
        }

        foreach (var text in payload.EvidenceRefs.Concat(payload.Entries.Select(entry => entry.SourceSnapshotRelativePath)))
        {
            if (!IsSafeMetadata(text))
            {
                blockers.Add(ProductLedgerLocalOperatorSurfaceLatestStateManifestBlocker.RedactionRejected);
            }
        }

        return blockers.Distinct().OrderBy(blocker => blocker.ToString(), StringComparer.Ordinal).ToArray();
    }

    private IReadOnlyList<ProductLedgerLocalOperatorSurfaceLatestStateManifestBlocker> ValidateRequest(
        ProductLedgerLocalOperatorSurfaceLatestStateManifestRequest? request)
    {
        var blockers = new List<ProductLedgerLocalOperatorSurfaceLatestStateManifestBlocker>();
        if (request is null)
        {
            blockers.Add(ProductLedgerLocalOperatorSurfaceLatestStateManifestBlocker.MissingRequest);
            return blockers;
        }

        if (!BoundaryAllowed()) blockers.Add(ProductLedgerLocalOperatorSurfaceLatestStateManifestBlocker.OutputBoundaryRejected);
        if (!request.ExplicitLatestStateManifestScope) blockers.Add(ProductLedgerLocalOperatorSurfaceLatestStateManifestBlocker.MissingExplicitManifestScope);
        if (!request.DevelopmentMode) blockers.Add(ProductLedgerLocalOperatorSurfaceLatestStateManifestBlocker.NonDevelopmentMode);
        if (!request.LocalMode) blockers.Add(ProductLedgerLocalOperatorSurfaceLatestStateManifestBlocker.NonLocalMode);
        if (!request.InternalMode) blockers.Add(ProductLedgerLocalOperatorSurfaceLatestStateManifestBlocker.NonInternalMode);
        if (string.IsNullOrWhiteSpace(request.ManifestId)) blockers.Add(ProductLedgerLocalOperatorSurfaceLatestStateManifestBlocker.MissingManifestId);
        else if (!IsSafeId(request.ManifestId)) blockers.Add(ProductLedgerLocalOperatorSurfaceLatestStateManifestBlocker.UnsafeManifestId);
        if (string.IsNullOrWhiteSpace(request.ActionId)) blockers.Add(ProductLedgerLocalOperatorSurfaceLatestStateManifestBlocker.MissingActionId);
        else if (!IsSafeId(request.ActionId)) blockers.Add(ProductLedgerLocalOperatorSurfaceLatestStateManifestBlocker.UnsafeActionId);
        if (request.ActionKind != ProductLedgerLocalOperatorSurfaceLatestStateManifestActionKind.LocalOperatorSurfaceLatestStateManifestCreateOnly)
        {
            blockers.Add(ProductLedgerLocalOperatorSurfaceLatestStateManifestBlocker.UnknownActionKind);
        }

        AddSourceSnapshotBlockers(request, blockers);
        AddPayloadBlockers(request, blockers);
        AddClaimBlockers(request, blockers);
        AddRedactionBlockers(request, blockers);

        return blockers.Distinct().OrderBy(blocker => blocker.ToString(), StringComparer.Ordinal).ToArray();
    }

    private static void AddSourceSnapshotBlockers(
        ProductLedgerLocalOperatorSurfaceLatestStateManifestRequest request,
        List<ProductLedgerLocalOperatorSurfaceLatestStateManifestBlocker> blockers)
    {
        var snapshot = request.SourceSnapshot;
        if (snapshot is null)
        {
            blockers.Add(ProductLedgerLocalOperatorSurfaceLatestStateManifestBlocker.MissingSourceSnapshot);
            return;
        }

        if (snapshot.Decision is not ProductLedgerLocalOperatorSurfaceLatestStateSnapshotDecision.SnapshotCreatedLocalOnly
            and not ProductLedgerLocalOperatorSurfaceLatestStateSnapshotDecision.IdempotentReplay)
        {
            blockers.Add(ProductLedgerLocalOperatorSurfaceLatestStateManifestBlocker.SourceSnapshotNotCreated);
        }

        if (string.IsNullOrWhiteSpace(request.ExpectedSourceSnapshotContentHash))
        {
            blockers.Add(ProductLedgerLocalOperatorSurfaceLatestStateManifestBlocker.MissingExpectedSourceSnapshotHash);
        }
        else if (!string.Equals(snapshot.SnapshotContentHash, request.ExpectedSourceSnapshotContentHash, StringComparison.Ordinal))
        {
            blockers.Add(ProductLedgerLocalOperatorSurfaceLatestStateManifestBlocker.SourceSnapshotHashMismatch);
        }

        if (string.IsNullOrWhiteSpace(request.ExpectedSourceSnapshotCheckpointHash))
        {
            blockers.Add(ProductLedgerLocalOperatorSurfaceLatestStateManifestBlocker.MissingExpectedSourceSnapshotCheckpoint);
        }
        else if (!string.Equals(snapshot.CheckpointHash, request.ExpectedSourceSnapshotCheckpointHash, StringComparison.Ordinal))
        {
            blockers.Add(ProductLedgerLocalOperatorSurfaceLatestStateManifestBlocker.SourceSnapshotCheckpointMismatch);
        }

        if (!snapshot.HistoricalEvidenceOnly)
        {
            blockers.Add(ProductLedgerLocalOperatorSurfaceLatestStateManifestBlocker.SourceSnapshotNotHistoricalEvidence);
        }

        if (snapshot.AuthorityLiveProduct || snapshot.PublicProductAllowed || snapshot.ProductionAllowed || snapshot.ReleaseCommercialReady)
        {
            blockers.Add(ProductLedgerLocalOperatorSurfaceLatestStateManifestBlocker.SourceSnapshotAuthorityClaimed);
        }

        if (!IsSafeSnapshotRelativePath(snapshot.SnapshotRelativePath))
        {
            blockers.Add(ProductLedgerLocalOperatorSurfaceLatestStateManifestBlocker.SourceSnapshotPathRejected);
        }
    }

    private static void AddPayloadBlockers(
        ProductLedgerLocalOperatorSurfaceLatestStateManifestRequest request,
        List<ProductLedgerLocalOperatorSurfaceLatestStateManifestBlocker> blockers)
    {
        if (ContainsPayload(request.ProposedPath) || ContainsPayload(request.ProposedRoot) || ContainsPayload(request.ProposedFilename))
        {
            blockers.Add(ProductLedgerLocalOperatorSurfaceLatestStateManifestBlocker.PayloadPathFieldRejected);
        }

        if (ContainsPayload(request.ProposedCommand))
        {
            blockers.Add(ProductLedgerLocalOperatorSurfaceLatestStateManifestBlocker.PayloadCommandFieldRejected);
        }

        if (ContainsPayload(request.ProposedUrl) || ContainsPayload(request.ProposedProvider) || ContainsPayload(request.ProposedDbMigration))
        {
            blockers.Add(ProductLedgerLocalOperatorSurfaceLatestStateManifestBlocker.PayloadNetworkProviderFieldRejected);
        }

        if (request.EvidenceReferences is null || request.EvidenceReferences.Count == 0)
        {
            blockers.Add(ProductLedgerLocalOperatorSurfaceLatestStateManifestBlocker.MissingEvidenceReferences);
        }
    }

    private static void AddClaimBlockers(
        ProductLedgerLocalOperatorSurfaceLatestStateManifestRequest request,
        List<ProductLedgerLocalOperatorSurfaceLatestStateManifestBlocker> blockers)
    {
        if (request.ClaimsArbitraryPathInput) blockers.Add(ProductLedgerLocalOperatorSurfaceLatestStateManifestBlocker.ClaimsArbitraryPathInput);
        if (request.ClaimsFilesystemScan) blockers.Add(ProductLedgerLocalOperatorSurfaceLatestStateManifestBlocker.ClaimsFilesystemScan);
        if (request.RequestsOverwrite) blockers.Add(ProductLedgerLocalOperatorSurfaceLatestStateManifestBlocker.RequestsOverwrite);
        if (request.RequestsLatestPointer) blockers.Add(ProductLedgerLocalOperatorSurfaceLatestStateManifestBlocker.RequestsLatestPointer);
        if (request.RequestsLatestPointerOverwrite) blockers.Add(ProductLedgerLocalOperatorSurfaceLatestStateManifestBlocker.RequestsLatestPointerOverwrite);
        if (request.RequestsReadPrecedence) blockers.Add(ProductLedgerLocalOperatorSurfaceLatestStateManifestBlocker.RequestsReadPrecedence);
        if (request.RequestsUserSelectedPath) blockers.Add(ProductLedgerLocalOperatorSurfaceLatestStateManifestBlocker.RequestsUserSelectedPath);
        if (request.RequestsPublicUiAction) blockers.Add(ProductLedgerLocalOperatorSurfaceLatestStateManifestBlocker.RequestsPublicUiAction);
        if (request.RequestsProductCommandExecution) blockers.Add(ProductLedgerLocalOperatorSurfaceLatestStateManifestBlocker.RequestsProductCommandExecution);
        if (request.RequestsProductCommandHandler) blockers.Add(ProductLedgerLocalOperatorSurfaceLatestStateManifestBlocker.RequestsProductCommandHandler);
        if (request.RequestsProductiveServiceRegistration) blockers.Add(ProductLedgerLocalOperatorSurfaceLatestStateManifestBlocker.RequestsProductiveServiceRegistration);
        if (request.RequestsShellOrSubprocess) blockers.Add(ProductLedgerLocalOperatorSurfaceLatestStateManifestBlocker.RequestsShellOrSubprocess);
        if (request.ClaimsArbitraryCommandExecution) blockers.Add(ProductLedgerLocalOperatorSurfaceLatestStateManifestBlocker.ClaimsArbitraryCommandExecution);
        if (request.ClaimsProviderCloudNetwork) blockers.Add(ProductLedgerLocalOperatorSurfaceLatestStateManifestBlocker.ClaimsProviderCloudNetwork);
        if (request.ClaimsDbMigration) blockers.Add(ProductLedgerLocalOperatorSurfaceLatestStateManifestBlocker.ClaimsDbMigration);
        if (request.ClaimsKmsWormExternalTrust) blockers.Add(ProductLedgerLocalOperatorSurfaceLatestStateManifestBlocker.ClaimsKmsWormExternalTrust);
        if (request.ClaimsBrowserCdpWcuOcrRecipesLive) blockers.Add(ProductLedgerLocalOperatorSurfaceLatestStateManifestBlocker.ClaimsBrowserCdpWcuOcrRecipesLive);
        if (request.ClaimsPilotRun) blockers.Add(ProductLedgerLocalOperatorSurfaceLatestStateManifestBlocker.ClaimsPilotRun);
        if (request.ClaimsReleaseCommercial) blockers.Add(ProductLedgerLocalOperatorSurfaceLatestStateManifestBlocker.ClaimsReleaseCommercial);
        if (request.ClaimsLiveAuthority) blockers.Add(ProductLedgerLocalOperatorSurfaceLatestStateManifestBlocker.ClaimsLiveAuthority);
        if (request.ClaimsProductAuthority) blockers.Add(ProductLedgerLocalOperatorSurfaceLatestStateManifestBlocker.ClaimsProductAuthority);
        if (request.ClaimsComplianceCustody) blockers.Add(ProductLedgerLocalOperatorSurfaceLatestStateManifestBlocker.ClaimsComplianceCustody);
        if (request.ClaimsCloudBackedDurability) blockers.Add(ProductLedgerLocalOperatorSurfaceLatestStateManifestBlocker.ClaimsCloudBackedDurability);
    }

    private static void AddRedactionBlockers(
        ProductLedgerLocalOperatorSurfaceLatestStateManifestRequest request,
        List<ProductLedgerLocalOperatorSurfaceLatestStateManifestBlocker> blockers)
    {
        var redaction = new RedactionBeforePersistenceService().Evaluate(
            RedactionBeforePersistencePolicy.TestOnly,
            new DurableAuditTrailAppendOnlyMinimalRequest(
                EventKind: "ProductLedgerLocalOperatorSurfaceLatestStateManifest",
                ActorReference: "local-internal-operator",
                ApprovalReference: request.SourceSnapshot?.SnapshotId ?? "snapshot-missing",
                EvidenceReferences: request.EvidenceReferences ?? [],
                Metadata: new Dictionary<string, string>
                {
                    ["manifestId"] = request.ManifestId ?? string.Empty,
                    ["actionId"] = request.ActionId ?? string.Empty,
                    ["actionKind"] = request.ActionKind?.ToString() ?? string.Empty,
                    ["sourceSnapshotHashPrefix"] = Prefix(request.ExpectedSourceSnapshotContentHash),
                    ["classification"] = Classification,
                    ["allowedBoundary"] = AllowedRelativeOutputBoundary,
                    ["stalePolicy"] = StalePolicy
                },
                RawPayload: null));

        if (!redaction.Succeeded || redaction.Decision != RedactionBeforePersistenceDecision.Allowed)
        {
            blockers.Add(ProductLedgerLocalOperatorSurfaceLatestStateManifestBlocker.RedactionRejected);
        }
    }

    private bool BoundaryAllowed()
    {
        if (!options.ExplicitLatestStateManifestBoundary
            || options.AllowsArbitraryPathInput
            || options.AllowsFilesystemScan
            || options.AllowsOverwrite
            || options.AllowsLatestPointer
            || options.AllowsLatestPointerOverwrite
            || options.AllowsReadPrecedence
            || options.AllowsUserSelectedPath
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

        var normalized = full.Replace('\\', '/');
        return normalized.IndexOf("/.git", StringComparison.OrdinalIgnoreCase) < 0
            && full.IndexOf("..", StringComparison.Ordinal) < 0
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

    private static string? FullManifestPath(string canonicalRoot, string relativePath)
    {
        try
        {
            var boundaryRoot = Path.GetFullPath(Path.Combine(canonicalRoot, AllowedRelativeOutputBoundary.Replace('/', Path.DirectorySeparatorChar)));
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

    private static string ManifestRelativePath(ProductLedgerLocalOperatorSurfaceLatestStateManifestRequest request) =>
        AllowedRelativeOutputBoundary
        + $"operator-surface-latest-state-manifest-{NormalizeId(request.ManifestId!)}-{Prefix(request.ExpectedSourceSnapshotContentHash)}.json";

    private static IReadOnlyList<ProductLedgerLocalOperatorSurfaceLatestStateManifestEntry> Entries(
        ProductLedgerLocalOperatorSurfaceLatestStateSnapshotResult snapshot) =>
    [
        new(
            SourceSnapshotId: snapshot.SnapshotId,
            SourceSnapshotRelativePath: snapshot.SnapshotRelativePath,
            SourceSnapshotContentHash: snapshot.SnapshotContentHash,
            SourceSnapshotCheckpointHash: snapshot.CheckpointHash,
            SourceSnapshotClassification: snapshot.Classification,
            CandidateFreshness: "unknown-historical-evidence-only",
            HistoricalEvidenceOnly: true,
            NotAuthority: true)
    ];

    private static ProductLedgerLocalOperatorSurfaceLatestStateManifestPayload Payload(
        ProductLedgerLocalOperatorSurfaceLatestStateManifestRequest request,
        string relativePath,
        IReadOnlyList<ProductLedgerLocalOperatorSurfaceLatestStateManifestEntry> entries,
        IReadOnlyList<string> evidenceRefs,
        string checkpointHash,
        DateTimeOffset createdAt) =>
        new(
            SchemaVersion: 1,
            ManifestId: request.ManifestId!.Trim(),
            ActionId: request.ActionId!.Trim(),
            CreatedAtUtc: createdAt,
            ManifestVersion: "v1",
            Classification: Classification,
            StalePolicy: StalePolicy,
            InputSnapshotBoundary: ProductLedgerLocalOperatorSurfaceLatestStateSnapshotExecutor.AllowedRelativeOutputBoundary,
            SafeRelativeManifestPath: relativePath,
            Entries: entries,
            EvidenceRefs: evidenceRefs,
            BlockerSummaryRedacted: "none",
            WarningSummaryRedacted: "manifest is historical index/evidence only; not authority live/product; no read precedence",
            NegativeFlags: NegativeFlags,
            ManifestContentHash: string.Empty,
            CheckpointHash: checkpointHash,
            LocalInternalDevelopmentOnly: true,
            HistoricalIndexEvidenceOnly: true,
            AuthorityLiveProduct: false,
            ProductAuthority: false,
            PublicProduct: false,
            Production: false,
            ReleaseCommercial: false,
            ComplianceCustody: false,
            CloudBackedDurability: false,
            LatestPointer: false,
            LatestPointerOverwrite: false,
            ReadPrecedence: false);

    private static string ContentHashForPayload(ProductLedgerLocalOperatorSurfaceLatestStateManifestPayload payload) =>
        HashText(JsonSerializer.Serialize(payload with { ManifestContentHash = string.Empty }, ManifestJsonOptions));

    private static bool ExistingPayloadMatches(
        ProductLedgerLocalOperatorSurfaceLatestStateManifestPayload payload,
        ProductLedgerLocalOperatorSurfaceLatestStateManifestRequest request,
        string relativePath,
        IReadOnlyList<ProductLedgerLocalOperatorSurfaceLatestStateManifestEntry> entries,
        IReadOnlyList<string> evidenceRefs,
        string checkpointHash) =>
        payload.SchemaVersion == 1
        && string.Equals(payload.ManifestId, request.ManifestId?.Trim(), StringComparison.Ordinal)
        && string.Equals(payload.ActionId, request.ActionId?.Trim(), StringComparison.Ordinal)
        && string.Equals(payload.Classification, Classification, StringComparison.Ordinal)
        && string.Equals(payload.StalePolicy, StalePolicy, StringComparison.Ordinal)
        && string.Equals(payload.SafeRelativeManifestPath, relativePath, StringComparison.Ordinal)
        && payload.Entries.SequenceEqual(entries)
        && payload.EvidenceRefs.SequenceEqual(evidenceRefs, StringComparer.Ordinal)
        && payload.NegativeFlags.SequenceEqual(NegativeFlags, StringComparer.Ordinal)
        && string.Equals(payload.CheckpointHash, checkpointHash, StringComparison.Ordinal)
        && string.Equals(payload.ManifestContentHash, ContentHashForPayload(payload), StringComparison.Ordinal)
        && payload.LocalInternalDevelopmentOnly
        && payload.HistoricalIndexEvidenceOnly
        && !payload.AuthorityLiveProduct
        && !payload.ProductAuthority
        && !payload.PublicProduct
        && !payload.Production
        && !payload.ReleaseCommercial
        && !payload.ComplianceCustody
        && !payload.CloudBackedDurability
        && !payload.LatestPointer
        && !payload.LatestPointerOverwrite
        && !payload.ReadPrecedence;

    private static IReadOnlyList<string> EvidenceRefs(ProductLedgerLocalOperatorSurfaceLatestStateManifestRequest request) =>
        (request.EvidenceReferences ?? [])
        .Concat(request.SourceSnapshot?.EvidenceRefs ?? [])
        .Concat(["product-ledger-operator-surface-latest-state-manifest-created"])
        .Select(evidence => evidence.Trim())
        .Where(evidence => !string.IsNullOrWhiteSpace(evidence))
        .Distinct(StringComparer.Ordinal)
        .OrderBy(evidence => evidence, StringComparer.Ordinal)
        .ToArray();

    private static ProductLedgerLocalOperatorSurfaceLatestStateManifestResult ManifestFrom(
        ProductLedgerLocalOperatorSurfaceLatestStateManifestRequest request,
        string relativePath,
        IReadOnlyList<ProductLedgerLocalOperatorSurfaceLatestStateManifestEntry> entries,
        IReadOnlyList<string> evidenceRefs,
        string contentHash,
        string checkpointHash,
        DateTimeOffset createdAt,
        ProductLedgerLocalOperatorSurfaceLatestStateManifestDecision decision) =>
        new(
            Decision: decision,
            State: ProductLedgerLocalOperatorSurfaceLatestStateManifestState.ManifestCreatedLocalOnly,
            Blockers: [],
            ManifestId: request.ManifestId!.Trim(),
            ActionId: request.ActionId!.Trim(),
            ManifestRelativePath: relativePath,
            AllowedBoundary: AllowedRelativeOutputBoundary,
            InputSnapshotBoundary: ProductLedgerLocalOperatorSurfaceLatestStateSnapshotExecutor.AllowedRelativeOutputBoundary,
            Classification: Classification,
            StalePolicy: StalePolicy,
            SourceSnapshotId: request.SourceSnapshot!.SnapshotId,
            SourceSnapshotRelativePath: request.SourceSnapshot.SnapshotRelativePath,
            SourceSnapshotContentHash: request.SourceSnapshot.SnapshotContentHash,
            SourceSnapshotContentHashPrefix: Prefix(request.SourceSnapshot.SnapshotContentHash),
            SourceSnapshotCheckpointHash: request.SourceSnapshot.CheckpointHash,
            SourceSnapshotCheckpointHashPrefix: Prefix(request.SourceSnapshot.CheckpointHash),
            ManifestContentHash: contentHash,
            ManifestContentHashPrefix: Prefix(contentHash),
            CheckpointHash: checkpointHash,
            CheckpointHashPrefix: Prefix(checkpointHash),
            Entries: entries,
            EvidenceRefs: evidenceRefs,
            NegativeFlags: NegativeFlags,
            CreateOnly: true,
            Versioned: true,
            JsonOnly: true,
            OverwriteAllowed: false,
            LatestPointerAvailable: false,
            LatestPointerOverwriteAllowed: false,
            ReadPrecedenceAllowed: false,
            UserSelectedPathAllowed: false,
            PayloadControlledRootAllowed: false,
            CanonicalizationPassed: true,
            ReparseValidationPassed: true,
            RedactionApplied: true,
            LocalOnly: true,
            InternalOnly: true,
            DevelopmentOnly: true,
            HistoricalIndexEvidenceOnly: true,
            AuthorityLiveProduct: false,
            ProductAuthority: false,
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
            CreatedAtUtc: createdAt,
            StatusText: CompletedStatus);

    private static ProductLedgerLocalOperatorSurfaceLatestStateManifestResult ManifestRejected(
        IReadOnlyList<ProductLedgerLocalOperatorSurfaceLatestStateManifestBlocker> blockers) =>
        ProductLedgerLocalOperatorSurfaceLatestStateManifestResult.Pending with
        {
            Decision = ProductLedgerLocalOperatorSurfaceLatestStateManifestDecision.Rejected,
            State = blockers.Contains(ProductLedgerLocalOperatorSurfaceLatestStateManifestBlocker.ExistingManifestConflict)
                || blockers.Contains(ProductLedgerLocalOperatorSurfaceLatestStateManifestBlocker.ExistingManifestCorrupt)
                    ? ProductLedgerLocalOperatorSurfaceLatestStateManifestState.ManifestReplayBlocked
                    : ProductLedgerLocalOperatorSurfaceLatestStateManifestState.ManifestBlocked,
            Blockers = blockers.Distinct().OrderBy(blocker => blocker.ToString(), StringComparer.Ordinal).ToArray(),
            StatusText = RejectedStatus
        };

    private ProductLedgerLocalOperatorSurfaceLatestStateManifestResult Remember(
        ProductLedgerLocalOperatorSurfaceLatestStateManifestResult manifest)
    {
        lastManifest = manifest;
        return manifest;
    }

    private static bool ContainsPayload(string? value) =>
        !string.IsNullOrWhiteSpace(value);

    private static bool IsSafeId(string value)
    {
        var trimmed = value.Trim();
        return trimmed.Length is > 0 and <= 80
            && trimmed.All(ch => char.IsAsciiLetterOrDigit(ch) || ch is '-' or '_' or '.')
            && !trimmed.Contains("..", StringComparison.Ordinal)
            && !trimmed.Contains(':', StringComparison.Ordinal)
            && !trimmed.Contains('\\', StringComparison.Ordinal)
            && !trimmed.Contains('/', StringComparison.Ordinal);
    }

    private static bool IsSafeSnapshotRelativePath(string value) =>
        value.StartsWith(ProductLedgerLocalOperatorSurfaceLatestStateSnapshotExecutor.AllowedRelativeOutputBoundary, StringComparison.Ordinal)
        && value.EndsWith(".json", StringComparison.Ordinal)
        && IsSafeMetadata(value);

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

    private static string NormalizeId(string value)
    {
        var chars = value.Trim().ToLowerInvariant()
            .Select(ch => char.IsAsciiLetterOrDigit(ch) ? ch : '-')
            .ToArray();
        return new string(chars).Trim('-');
    }

    private static string HashText(string material)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(material));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string Prefix(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "none" : value[..Math.Min(12, value.Length)];
}

public sealed record ProductLedgerLocalOperatorSurfaceLatestStateManifestPayload(
    int SchemaVersion,
    string ManifestId,
    string ActionId,
    DateTimeOffset CreatedAtUtc,
    string ManifestVersion,
    string Classification,
    string StalePolicy,
    string InputSnapshotBoundary,
    string SafeRelativeManifestPath,
    IReadOnlyList<ProductLedgerLocalOperatorSurfaceLatestStateManifestEntry> Entries,
    IReadOnlyList<string> EvidenceRefs,
    string BlockerSummaryRedacted,
    string WarningSummaryRedacted,
    IReadOnlyList<string> NegativeFlags,
    string ManifestContentHash,
    string CheckpointHash,
    bool LocalInternalDevelopmentOnly,
    bool HistoricalIndexEvidenceOnly,
    bool AuthorityLiveProduct,
    bool ProductAuthority,
    bool PublicProduct,
    bool Production,
    bool ReleaseCommercial,
    bool ComplianceCustody,
    bool CloudBackedDurability,
    bool LatestPointer,
    bool LatestPointerOverwrite,
    bool ReadPrecedence);
