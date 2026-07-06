using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OneBrain.Core.Approval;

public enum ProductLedgerLocalOperatorSurfaceLatestStateSnapshotDecision
{
    Rejected,
    SnapshotCreatedLocalOnly,
    IdempotentReplay
}

public enum ProductLedgerLocalOperatorSurfaceLatestStateSnapshotState
{
    Pending,
    SnapshotCreatedLocalOnly,
    SnapshotBlocked,
    SnapshotReplayBlocked
}

public enum ProductLedgerLocalOperatorSurfaceLatestStateSnapshotActionKind
{
    LocalOperatorSurfaceLatestStateSnapshotCreateOnly
}

public enum ProductLedgerLocalOperatorSurfaceLatestStateSnapshotBlocker
{
    MissingRequest,
    MissingExplicitSnapshotScope,
    NonDevelopmentMode,
    NonLocalMode,
    NonInternalMode,
    OutputBoundaryRejected,
    MissingSnapshotId,
    MissingActionId,
    UnsafeSnapshotId,
    UnsafeActionId,
    UnknownActionKind,
    MissingOperatorSurface,
    MissingOperatorSurfaceModelHash,
    OperatorSurfaceModelHashMismatch,
    MissingApprovalDecision,
    ApprovalDecisionNotApproved,
    MissingNoOpExecution,
    NoOpExecutionNotCompleted,
    MissingBoundedExecution,
    BoundedExecutionNotCompleted,
    MissingLocalApprovedHandoffDraft,
    LocalApprovedHandoffDraftNotCompleted,
    MissingWorkspaceTestJailHandoffDraft,
    WorkspaceTestJailHandoffDraftNotCompleted,
    MissingUserWorkspaceAllowlistedHandoffDraft,
    UserWorkspaceAllowlistedHandoffDraftNotCompleted,
    MissingEvidenceReferences,
    RedactionRejected,
    PathCanonicalizationFailed,
    ReparsePointRejected,
    FinalPathOutsideAllowedBoundary,
    ExistingSnapshotConflict,
    ExistingSnapshotCorrupt,
    IoFailure,
    PayloadPathFieldRejected,
    PayloadCommandFieldRejected,
    PayloadNetworkProviderFieldRejected,
    ClaimsArbitraryPathInput,
    ClaimsFilesystemScan,
    RequestsOverwrite,
    RequestsLatestPointerOverwrite,
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
    ClaimsReleaseCommercial
}

public sealed record ProductLedgerLocalOperatorSurfaceLatestStateSnapshotOptions(
    string WorkspaceRootPath,
    bool ExplicitLatestStateSnapshotBoundary,
    bool AllowsArbitraryPathInput,
    bool AllowsFilesystemScan,
    bool AllowsOverwrite,
    bool AllowsLatestPointerOverwrite,
    bool AllowsUserSelectedPath,
    bool AllowsShellOrSubprocess,
    bool AllowsCommandExecution,
    bool AllowsNetwork,
    bool AllowsDb,
    bool AllowsKmsWormExternalTrust,
    bool AllowsReleaseCommercial);

public sealed record ProductLedgerLocalOperatorSurfaceLatestStateSnapshotRequest(
    bool ExplicitLatestStateSnapshotScope,
    bool DevelopmentMode,
    bool LocalMode,
    bool InternalMode,
    string? SnapshotId,
    string? ActionId,
    ProductLedgerLocalOperatorSurfaceLatestStateSnapshotActionKind? ActionKind,
    ProductLedgerOperatorSurfaceModel? OperatorSurface,
    string? OperatorSurfaceModelHash,
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
    bool RequestsLatestPointerOverwrite,
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
    bool ClaimsReleaseCommercial);

public sealed record ProductLedgerLocalOperatorSurfaceLatestStateSnapshotResult(
    ProductLedgerLocalOperatorSurfaceLatestStateSnapshotDecision Decision,
    ProductLedgerLocalOperatorSurfaceLatestStateSnapshotState State,
    IReadOnlyList<ProductLedgerLocalOperatorSurfaceLatestStateSnapshotBlocker> Blockers,
    string SnapshotId,
    string ActionId,
    string SnapshotRelativePath,
    string AllowedBoundary,
    string Classification,
    string StaleStateClassification,
    string OperatorSurfaceModelHash,
    string OperatorSurfaceModelHashPrefix,
    string SnapshotContentHash,
    string SnapshotContentHashPrefix,
    string CheckpointHash,
    string CheckpointHashPrefix,
    IReadOnlyList<string> SourceChainIds,
    IReadOnlyList<string> SourceChainContentHashes,
    IReadOnlyList<string> SafeRelativePathsOnly,
    IReadOnlyList<string> EvidenceRefs,
    IReadOnlyList<string> NegativeFlags,
    bool CreateOnly,
    bool Versioned,
    bool JsonOnly,
    bool OverwriteAllowed,
    bool LatestPointerOverwriteAllowed,
    bool UserSelectedPathAllowed,
    bool PayloadControlledRootAllowed,
    bool CanonicalizationPassed,
    bool ReparseValidationPassed,
    bool RedactionApplied,
    bool LocalOnly,
    bool InternalOnly,
    bool DevelopmentOnly,
    bool HistoricalEvidenceOnly,
    bool AuthorityLiveProduct,
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
    DateTimeOffset CreatedAtUtc,
    string StatusText)
{
    public static ProductLedgerLocalOperatorSurfaceLatestStateSnapshotResult Pending { get; } =
        new(
            Decision: ProductLedgerLocalOperatorSurfaceLatestStateSnapshotDecision.Rejected,
            State: ProductLedgerLocalOperatorSurfaceLatestStateSnapshotState.Pending,
            Blockers: [],
            SnapshotId: "operator-surface-latest-state-snapshot.pending",
            ActionId: "operator-surface-latest-state-snapshot-action.pending",
            SnapshotRelativePath: string.Empty,
            AllowedBoundary: ProductLedgerLocalOperatorSurfaceLatestStateSnapshotExecutor.AllowedRelativeOutputBoundary,
            Classification: ProductLedgerLocalOperatorSurfaceLatestStateSnapshotExecutor.Classification,
            StaleStateClassification: ProductLedgerLocalOperatorSurfaceLatestStateSnapshotExecutor.StaleStateClassification,
            OperatorSurfaceModelHash: string.Empty,
            OperatorSurfaceModelHashPrefix: "none",
            SnapshotContentHash: string.Empty,
            SnapshotContentHashPrefix: "none",
            CheckpointHash: string.Empty,
            CheckpointHashPrefix: "none",
            SourceChainIds: [],
            SourceChainContentHashes: [],
            SafeRelativePathsOnly: [],
            EvidenceRefs: ["product-ledger-operator-surface-latest-state-snapshot-pending"],
            NegativeFlags: ProductLedgerLocalOperatorSurfaceLatestStateSnapshotExecutor.NegativeFlags,
            CreateOnly: true,
            Versioned: true,
            JsonOnly: true,
            OverwriteAllowed: false,
            LatestPointerOverwriteAllowed: false,
            UserSelectedPathAllowed: false,
            PayloadControlledRootAllowed: false,
            CanonicalizationPassed: false,
            ReparseValidationPassed: false,
            RedactionApplied: false,
            LocalOnly: true,
            InternalOnly: true,
            DevelopmentOnly: true,
            HistoricalEvidenceOnly: true,
            AuthorityLiveProduct: false,
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
            CreatedAtUtc: DateTimeOffset.UnixEpoch,
            StatusText: ProductLedgerLocalOperatorSurfaceLatestStateSnapshotExecutor.PendingStatus);
}

public sealed class ProductLedgerLocalOperatorSurfaceLatestStateSnapshotExecutor
{
    public const string AllowedRelativeOutputBoundary = "docs/test-output/product-ledger/operator-surface-latest-state-snapshots/";
    public const string Classification = "LOCAL_INTERNAL_DEV_ONLY_HISTORICAL_SNAPSHOT";
    public const string StaleStateClassification = "STALE_SNAPSHOTS_ARE_HISTORICAL_EVIDENCE_ONLY_NOT_LIVE_PRODUCT_AUTHORITY";
    public const string ScopeId = "LocalOperatorSurfaceLatestStateSnapshotCreateOnly";

    public const string PendingStatus =
        "PRODUCT_LEDGER_OPERATOR_SURFACE_LATEST_STATE_SNAPSHOT_PENDING LOCAL_ONLY INTERNAL_ONLY DEVELOPMENT_ONLY HISTORICAL_EVIDENCE_ONLY JSON_ONLY VERSIONED_CREATE_ONLY NO_OVERWRITE NO_LATEST_POINTER_OVERWRITE NO_PUBLIC_PRODUCT_PATH NO_PRODUCTION_ROUTE NO_COMMAND_EXECUTION NO_SHELL_SUBPROCESS NO_PROVIDER_CLOUD_NETWORK NO_DB_MIGRATION NO_KMS_WORM_EXTERNAL_TRUST NO_LIVE_AUTOMATION NO_RELEASE_COMMERCIAL";

    public const string CompletedStatus =
        "PRODUCT_LEDGER_OPERATOR_SURFACE_LATEST_STATE_SNAPSHOT_CREATED LOCAL_ONLY INTERNAL_ONLY DEVELOPMENT_ONLY HISTORICAL_EVIDENCE_ONLY JSON_ONLY VERSIONED_CREATE_ONLY NO_OVERWRITE NO_LATEST_POINTER_OVERWRITE REDACTION_APPLIED HASH_CHECKPOINTED STALE_STATE_CLASSIFIED NO_PUBLIC_PRODUCT_PATH NO_PRODUCTION_ROUTE NO_COMMAND_EXECUTION NO_SHELL_SUBPROCESS NO_PROVIDER_CLOUD_NETWORK NO_DB_MIGRATION NO_KMS_WORM_EXTERNAL_TRUST NO_LIVE_AUTOMATION NO_RELEASE_COMMERCIAL";

    public const string RejectedStatus =
        "PRODUCT_LEDGER_OPERATOR_SURFACE_LATEST_STATE_SNAPSHOT_REJECTED FAIL_CLOSED HISTORICAL_EVIDENCE_ONLY JSON_ONLY VERSIONED_CREATE_ONLY NO_OVERWRITE NO_LATEST_POINTER_OVERWRITE NO_PUBLIC_PRODUCT_PATH NO_PRODUCTION_ROUTE NO_COMMAND_EXECUTION NO_SHELL_SUBPROCESS NO_PROVIDER_CLOUD_NETWORK NO_DB_MIGRATION NO_KMS_WORM_EXTERNAL_TRUST NO_LIVE_AUTOMATION NO_RELEASE_COMMERCIAL";

    public static IReadOnlyList<string> NegativeFlags { get; } =
    [
        "not production",
        "not public/product",
        "not authority live/product",
        "stale snapshots are historical evidence only",
        "no shell/subprocess",
        "no command execution",
        "no cloud/network/DB",
        "no KMS/WORM/compliance custody",
        "no Browser/CDP/WCU/OCR/Recipes live",
        "no Pilot /run",
        "no release/commercial",
        "no latest pointer overwrite",
        "no overwrite",
        "no user-selected path"
    ];

    private static readonly JsonSerializerOptions SnapshotJsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never
    };

    private readonly ProductLedgerLocalOperatorSurfaceLatestStateSnapshotOptions options;
    private ProductLedgerLocalOperatorSurfaceLatestStateSnapshotResult? lastSnapshot;

    public ProductLedgerLocalOperatorSurfaceLatestStateSnapshotExecutor(
        ProductLedgerLocalOperatorSurfaceLatestStateSnapshotOptions options)
    {
        this.options = options;
    }

    public ProductLedgerLocalOperatorSurfaceLatestStateSnapshotResult CreateSnapshot(
        ProductLedgerLocalOperatorSurfaceLatestStateSnapshotRequest? request)
    {
        var blockers = ValidateRequest(request);
        if (blockers.Count > 0 || request is null || request.OperatorSurface is null)
        {
            return Remember(SnapshotRejected(blockers));
        }

        var createdAt = DateTimeOffset.UnixEpoch;
        var canonicalRoot = CanonicalRoot();
        if (canonicalRoot is null)
        {
            return Remember(SnapshotRejected([ProductLedgerLocalOperatorSurfaceLatestStateSnapshotBlocker.OutputBoundaryRejected]));
        }

        var operatorSurfaceHash = ComputeOperatorSurfaceModelHash(request.OperatorSurface);
        var relativePath = SnapshotRelativePath(request, operatorSurfaceHash);
        var fullPath = FullSnapshotPath(canonicalRoot, relativePath);
        if (fullPath is null)
        {
            return Remember(SnapshotRejected([ProductLedgerLocalOperatorSurfaceLatestStateSnapshotBlocker.FinalPathOutsideAllowedBoundary]));
        }

        var sourceChainIds = SourceChainIds(request.OperatorSurface);
        var sourceChainHashes = SourceChainContentHashes(request.OperatorSurface);
        var safeRelativePaths = SafeRelativePaths(request.OperatorSurface);
        var evidenceRefs = EvidenceRefs(request);
        var checkpointHash = HashText(string.Join("|", sourceChainIds) + "|" + string.Join("|", sourceChainHashes) + "|" + operatorSurfaceHash);
        var payload = Payload(request, relativePath, operatorSurfaceHash, checkpointHash, sourceChainIds, sourceChainHashes, safeRelativePaths, evidenceRefs, createdAt);
        var contentHash = ContentHashForPayload(payload);
        var finalPayload = payload with { SnapshotContentHash = contentHash };
        var finalJson = JsonSerializer.Serialize(finalPayload, SnapshotJsonOptions);

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
            if (ContainsReparsePoint(canonicalRoot)
                || ContainsReparsePoint(Path.Combine(canonicalRoot, "docs"))
                || ContainsReparsePoint(Path.Combine(canonicalRoot, "docs", "test-output"))
                || ContainsReparsePoint(Path.Combine(canonicalRoot, "docs", "test-output", "product-ledger"))
                || ContainsReparsePoint(Path.GetDirectoryName(fullPath)!))
            {
                return Remember(SnapshotRejected([ProductLedgerLocalOperatorSurfaceLatestStateSnapshotBlocker.ReparsePointRejected]));
            }

            if (File.Exists(fullPath))
            {
                string existing;
                ProductLedgerLocalOperatorSurfaceLatestStateSnapshotPayload? existingPayload;
                try
                {
                    existing = File.ReadAllText(fullPath, Encoding.UTF8);
                    existingPayload = JsonSerializer.Deserialize<ProductLedgerLocalOperatorSurfaceLatestStateSnapshotPayload>(existing, SnapshotJsonOptions);
                }
                catch (JsonException)
                {
                    return Remember(SnapshotRejected([ProductLedgerLocalOperatorSurfaceLatestStateSnapshotBlocker.ExistingSnapshotCorrupt]));
                }

                if (existingPayload is not null
                    && ExistingPayloadMatches(
                        existingPayload,
                        request,
                        relativePath,
                        operatorSurfaceHash,
                        checkpointHash,
                        sourceChainIds,
                        sourceChainHashes,
                        safeRelativePaths,
                        evidenceRefs))
                {
                    return Remember(SnapshotFrom(
                        request,
                        relativePath,
                        operatorSurfaceHash,
                        contentHash,
                        checkpointHash,
                        sourceChainIds,
                        sourceChainHashes,
                        safeRelativePaths,
                        evidenceRefs,
                        existingPayload.CreatedAtUtc,
                        ProductLedgerLocalOperatorSurfaceLatestStateSnapshotDecision.IdempotentReplay));
                }

                return Remember(SnapshotRejected([ProductLedgerLocalOperatorSurfaceLatestStateSnapshotBlocker.ExistingSnapshotConflict]));
            }

            using (var stream = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            using (var writer = new StreamWriter(stream, Encoding.UTF8))
            {
                writer.Write(finalJson);
            }

            return Remember(SnapshotFrom(
                request,
                relativePath,
                operatorSurfaceHash,
                contentHash,
                checkpointHash,
                sourceChainIds,
                sourceChainHashes,
                safeRelativePaths,
                evidenceRefs,
                createdAt,
                ProductLedgerLocalOperatorSurfaceLatestStateSnapshotDecision.SnapshotCreatedLocalOnly));
        }
        catch (IOException)
        {
            return Remember(SnapshotRejected([ProductLedgerLocalOperatorSurfaceLatestStateSnapshotBlocker.IoFailure]));
        }
        catch (UnauthorizedAccessException)
        {
            return Remember(SnapshotRejected([ProductLedgerLocalOperatorSurfaceLatestStateSnapshotBlocker.IoFailure]));
        }
        catch (ArgumentException)
        {
            return Remember(SnapshotRejected([ProductLedgerLocalOperatorSurfaceLatestStateSnapshotBlocker.PathCanonicalizationFailed]));
        }
    }

    public ProductLedgerLocalOperatorSurfaceLatestStateSnapshotResult Read() =>
        lastSnapshot ?? ProductLedgerLocalOperatorSurfaceLatestStateSnapshotResult.Pending;

    public static string ComputeOperatorSurfaceModelHash(ProductLedgerOperatorSurfaceModel model)
    {
        var projection = new
        {
            model.SurfaceId,
            model.RoutePath,
            model.Scope,
            readModelMode = model.ReadModelMode.ToString(),
            model.LedgerAuthority,
            model.LedgerAuthorityBoundaryStatus,
            model.LedgerVerificationStatus,
            model.CheckpointStatus,
            model.LedgerPathClassification,
            model.LedgerEntryCount,
            model.LedgerHeadSequence,
            model.LedgerHeadHashPrefix,
            model.LedgerHashPrefix,
            approval = model.ApprovalDecisionState.Decision.ToString(),
            approvalId = model.ApprovalDecisionState.ApprovalId,
            noOp = model.ApprovedActionExecutionState.Decision.ToString(),
            noOpId = model.ApprovedActionExecutionState.ExecutionId,
            bounded = model.BoundedApprovedActionState.Decision.ToString(),
            boundedId = model.BoundedApprovedActionState.ExecutionId,
            localDraft = model.HandoffReportDraftState.Decision.ToString(),
            localDraftId = model.HandoffReportDraftState.DraftId,
            workspaceDraft = model.WorkspaceTestJailHandoffDraftState.Decision.ToString(),
            workspaceDraftId = model.WorkspaceTestJailHandoffDraftState.DraftId,
            userWorkspaceDraft = model.UserWorkspaceAllowlistedHandoffDraftState.Decision.ToString(),
            userWorkspaceDraftId = model.UserWorkspaceAllowlistedHandoffDraftState.DraftId
        };
        return HashText(JsonSerializer.Serialize(projection, SnapshotJsonOptions));
    }

    private IReadOnlyList<ProductLedgerLocalOperatorSurfaceLatestStateSnapshotBlocker> ValidateRequest(
        ProductLedgerLocalOperatorSurfaceLatestStateSnapshotRequest? request)
    {
        var blockers = new List<ProductLedgerLocalOperatorSurfaceLatestStateSnapshotBlocker>();
        if (request is null)
        {
            blockers.Add(ProductLedgerLocalOperatorSurfaceLatestStateSnapshotBlocker.MissingRequest);
            return blockers;
        }

        if (!BoundaryAllowed())
        {
            blockers.Add(ProductLedgerLocalOperatorSurfaceLatestStateSnapshotBlocker.OutputBoundaryRejected);
        }

        if (!request.ExplicitLatestStateSnapshotScope) blockers.Add(ProductLedgerLocalOperatorSurfaceLatestStateSnapshotBlocker.MissingExplicitSnapshotScope);
        if (!request.DevelopmentMode) blockers.Add(ProductLedgerLocalOperatorSurfaceLatestStateSnapshotBlocker.NonDevelopmentMode);
        if (!request.LocalMode) blockers.Add(ProductLedgerLocalOperatorSurfaceLatestStateSnapshotBlocker.NonLocalMode);
        if (!request.InternalMode) blockers.Add(ProductLedgerLocalOperatorSurfaceLatestStateSnapshotBlocker.NonInternalMode);

        if (string.IsNullOrWhiteSpace(request.SnapshotId)) blockers.Add(ProductLedgerLocalOperatorSurfaceLatestStateSnapshotBlocker.MissingSnapshotId);
        else if (!IsSafeId(request.SnapshotId)) blockers.Add(ProductLedgerLocalOperatorSurfaceLatestStateSnapshotBlocker.UnsafeSnapshotId);

        if (string.IsNullOrWhiteSpace(request.ActionId)) blockers.Add(ProductLedgerLocalOperatorSurfaceLatestStateSnapshotBlocker.MissingActionId);
        else if (!IsSafeId(request.ActionId)) blockers.Add(ProductLedgerLocalOperatorSurfaceLatestStateSnapshotBlocker.UnsafeActionId);

        if (request.ActionKind != ProductLedgerLocalOperatorSurfaceLatestStateSnapshotActionKind.LocalOperatorSurfaceLatestStateSnapshotCreateOnly)
        {
            blockers.Add(ProductLedgerLocalOperatorSurfaceLatestStateSnapshotBlocker.UnknownActionKind);
        }

        if (request.OperatorSurface is null)
        {
            blockers.Add(ProductLedgerLocalOperatorSurfaceLatestStateSnapshotBlocker.MissingOperatorSurface);
        }
        else
        {
            var modelHash = ComputeOperatorSurfaceModelHash(request.OperatorSurface);
            if (string.IsNullOrWhiteSpace(request.OperatorSurfaceModelHash))
            {
                blockers.Add(ProductLedgerLocalOperatorSurfaceLatestStateSnapshotBlocker.MissingOperatorSurfaceModelHash);
            }
            else if (!string.Equals(request.OperatorSurfaceModelHash.Trim(), modelHash, StringComparison.Ordinal))
            {
                blockers.Add(ProductLedgerLocalOperatorSurfaceLatestStateSnapshotBlocker.OperatorSurfaceModelHashMismatch);
            }

            AddChainBlockers(request.OperatorSurface, blockers);
        }

        AddEvidenceBlockers(request, blockers);
        AddPayloadBlockers(request, blockers);
        AddRedactionBlockers(request, blockers);
        return blockers.Distinct().OrderBy(blocker => blocker.ToString(), StringComparer.Ordinal).ToArray();
    }

    private static void AddChainBlockers(
        ProductLedgerOperatorSurfaceModel model,
        List<ProductLedgerLocalOperatorSurfaceLatestStateSnapshotBlocker> blockers)
    {
        if (model.ApprovalDecisionState.State != ProductLedgerLocalApprovalDecisionState.ApprovedLocalOnly)
        {
            blockers.Add(ProductLedgerLocalOperatorSurfaceLatestStateSnapshotBlocker.ApprovalDecisionNotApproved);
        }

        if (model.ApprovedActionExecutionState.State != ProductLedgerLocalApprovedActionExecutionState.NoOpExecutionCompletedLocalOnly)
        {
            blockers.Add(ProductLedgerLocalOperatorSurfaceLatestStateSnapshotBlocker.NoOpExecutionNotCompleted);
        }

        if (model.BoundedApprovedActionState.State != ProductLedgerLocalBoundedApprovedActionState.BoundedExecutionCompletedLocalOnly)
        {
            blockers.Add(ProductLedgerLocalOperatorSurfaceLatestStateSnapshotBlocker.BoundedExecutionNotCompleted);
        }

        if (model.HandoffReportDraftState.State != ProductLedgerLocalApprovedHandoffReportDraftState.DraftCreatedLocalOnly)
        {
            blockers.Add(ProductLedgerLocalOperatorSurfaceLatestStateSnapshotBlocker.LocalApprovedHandoffDraftNotCompleted);
        }

        if (model.WorkspaceTestJailHandoffDraftState.State != ProductLedgerLocalWorkspaceTestJailHandoffDraftState.DraftCreatedWorkspaceTestJailOnly)
        {
            blockers.Add(ProductLedgerLocalOperatorSurfaceLatestStateSnapshotBlocker.WorkspaceTestJailHandoffDraftNotCompleted);
        }

        if (model.UserWorkspaceAllowlistedHandoffDraftState.State != ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftState.DraftCreatedUserWorkspaceAllowlistedOnly)
        {
            blockers.Add(ProductLedgerLocalOperatorSurfaceLatestStateSnapshotBlocker.UserWorkspaceAllowlistedHandoffDraftNotCompleted);
        }
    }

    private static void AddEvidenceBlockers(
        ProductLedgerLocalOperatorSurfaceLatestStateSnapshotRequest request,
        List<ProductLedgerLocalOperatorSurfaceLatestStateSnapshotBlocker> blockers)
    {
        if (request.OperatorSurface is null)
        {
            return;
        }

        if (request.EvidenceReferences is null
            || request.EvidenceReferences.Count == 0
            || request.EvidenceReferences.Any(string.IsNullOrWhiteSpace)
            || request.OperatorSurface.EvidenceRefs.Count == 0
            || request.OperatorSurface.ApprovalDecisionState.EvidenceReferences.Count == 0
            || request.OperatorSurface.ApprovedActionExecutionState.EvidenceReferences.Count == 0
            || request.OperatorSurface.BoundedApprovedActionState.EvidenceReferences.Count == 0
            || request.OperatorSurface.HandoffReportDraftState.EvidenceRefs.Count == 0
            || request.OperatorSurface.WorkspaceTestJailHandoffDraftState.EvidenceRefs.Count == 0
            || request.OperatorSurface.UserWorkspaceAllowlistedHandoffDraftState.EvidenceRefs.Count == 0)
        {
            blockers.Add(ProductLedgerLocalOperatorSurfaceLatestStateSnapshotBlocker.MissingEvidenceReferences);
        }
    }

    private static void AddPayloadBlockers(
        ProductLedgerLocalOperatorSurfaceLatestStateSnapshotRequest request,
        List<ProductLedgerLocalOperatorSurfaceLatestStateSnapshotBlocker> blockers)
    {
        if (ContainsPayload(request.ProposedPath) || ContainsPayload(request.ProposedRoot) || ContainsPayload(request.ProposedFilename))
        {
            blockers.Add(ProductLedgerLocalOperatorSurfaceLatestStateSnapshotBlocker.PayloadPathFieldRejected);
        }

        if (ContainsPayload(request.ProposedCommand))
        {
            blockers.Add(ProductLedgerLocalOperatorSurfaceLatestStateSnapshotBlocker.PayloadCommandFieldRejected);
        }

        if (ContainsPayload(request.ProposedUrl)
            || ContainsPayload(request.ProposedProvider)
            || ContainsPayload(request.ProposedDbMigration))
        {
            blockers.Add(ProductLedgerLocalOperatorSurfaceLatestStateSnapshotBlocker.PayloadNetworkProviderFieldRejected);
        }

        if (request.ClaimsArbitraryPathInput) blockers.Add(ProductLedgerLocalOperatorSurfaceLatestStateSnapshotBlocker.ClaimsArbitraryPathInput);
        if (request.ClaimsFilesystemScan) blockers.Add(ProductLedgerLocalOperatorSurfaceLatestStateSnapshotBlocker.ClaimsFilesystemScan);
        if (request.RequestsOverwrite) blockers.Add(ProductLedgerLocalOperatorSurfaceLatestStateSnapshotBlocker.RequestsOverwrite);
        if (request.RequestsLatestPointerOverwrite) blockers.Add(ProductLedgerLocalOperatorSurfaceLatestStateSnapshotBlocker.RequestsLatestPointerOverwrite);
        if (request.RequestsUserSelectedPath) blockers.Add(ProductLedgerLocalOperatorSurfaceLatestStateSnapshotBlocker.RequestsUserSelectedPath);
        if (request.RequestsPublicUiAction) blockers.Add(ProductLedgerLocalOperatorSurfaceLatestStateSnapshotBlocker.RequestsPublicUiAction);
        if (request.RequestsProductCommandExecution) blockers.Add(ProductLedgerLocalOperatorSurfaceLatestStateSnapshotBlocker.RequestsProductCommandExecution);
        if (request.RequestsProductCommandHandler) blockers.Add(ProductLedgerLocalOperatorSurfaceLatestStateSnapshotBlocker.RequestsProductCommandHandler);
        if (request.RequestsProductiveServiceRegistration) blockers.Add(ProductLedgerLocalOperatorSurfaceLatestStateSnapshotBlocker.RequestsProductiveServiceRegistration);
        if (request.RequestsShellOrSubprocess) blockers.Add(ProductLedgerLocalOperatorSurfaceLatestStateSnapshotBlocker.RequestsShellOrSubprocess);
        if (request.ClaimsArbitraryCommandExecution) blockers.Add(ProductLedgerLocalOperatorSurfaceLatestStateSnapshotBlocker.ClaimsArbitraryCommandExecution);
        if (request.ClaimsProviderCloudNetwork) blockers.Add(ProductLedgerLocalOperatorSurfaceLatestStateSnapshotBlocker.ClaimsProviderCloudNetwork);
        if (request.ClaimsDbMigration) blockers.Add(ProductLedgerLocalOperatorSurfaceLatestStateSnapshotBlocker.ClaimsDbMigration);
        if (request.ClaimsKmsWormExternalTrust) blockers.Add(ProductLedgerLocalOperatorSurfaceLatestStateSnapshotBlocker.ClaimsKmsWormExternalTrust);
        if (request.ClaimsBrowserCdpWcuOcrRecipesLive) blockers.Add(ProductLedgerLocalOperatorSurfaceLatestStateSnapshotBlocker.ClaimsBrowserCdpWcuOcrRecipesLive);
        if (request.ClaimsPilotRun) blockers.Add(ProductLedgerLocalOperatorSurfaceLatestStateSnapshotBlocker.ClaimsPilotRun);
        if (request.ClaimsReleaseCommercial) blockers.Add(ProductLedgerLocalOperatorSurfaceLatestStateSnapshotBlocker.ClaimsReleaseCommercial);
    }

    private static void AddRedactionBlockers(
        ProductLedgerLocalOperatorSurfaceLatestStateSnapshotRequest request,
        List<ProductLedgerLocalOperatorSurfaceLatestStateSnapshotBlocker> blockers)
    {
        var redaction = new RedactionBeforePersistenceService().Evaluate(
            RedactionBeforePersistencePolicy.TestOnly,
            new DurableAuditTrailAppendOnlyMinimalRequest(
                EventKind: "ProductLedgerLocalOperatorSurfaceLatestStateSnapshot",
                ActorReference: "local-internal-operator",
                ApprovalReference: request.OperatorSurface?.ApprovalDecisionState.ApprovalId ?? "approval-missing",
                EvidenceReferences: request.EvidenceReferences ?? [],
                Metadata: new Dictionary<string, string>
                {
                    ["snapshotId"] = request.SnapshotId ?? string.Empty,
                    ["actionId"] = request.ActionId ?? string.Empty,
                    ["actionKind"] = request.ActionKind?.ToString() ?? string.Empty,
                    ["operatorSurfaceModelHashPrefix"] = Prefix(request.OperatorSurfaceModelHash),
                    ["classification"] = Classification,
                    ["allowedBoundary"] = AllowedRelativeOutputBoundary,
                    ["staleStateClassification"] = StaleStateClassification
                },
                RawPayload: null));

        if (!redaction.Succeeded || redaction.Decision != RedactionBeforePersistenceDecision.Allowed)
        {
            blockers.Add(ProductLedgerLocalOperatorSurfaceLatestStateSnapshotBlocker.RedactionRejected);
        }
    }

    private bool BoundaryAllowed()
    {
        if (!options.ExplicitLatestStateSnapshotBoundary
            || options.AllowsArbitraryPathInput
            || options.AllowsFilesystemScan
            || options.AllowsOverwrite
            || options.AllowsLatestPointerOverwrite
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

    private static string? FullSnapshotPath(string canonicalRoot, string relativePath)
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

    private static string SnapshotRelativePath(
        ProductLedgerLocalOperatorSurfaceLatestStateSnapshotRequest request,
        string operatorSurfaceHash) =>
        AllowedRelativeOutputBoundary
        + $"operator-surface-latest-state-snapshot-{NormalizeId(request.SnapshotId!)}-{Prefix(operatorSurfaceHash)}.json";

    private static ProductLedgerLocalOperatorSurfaceLatestStateSnapshotPayload Payload(
        ProductLedgerLocalOperatorSurfaceLatestStateSnapshotRequest request,
        string relativePath,
        string operatorSurfaceHash,
        string checkpointHash,
        IReadOnlyList<string> sourceChainIds,
        IReadOnlyList<string> sourceChainHashes,
        IReadOnlyList<string> safeRelativePaths,
        IReadOnlyList<string> evidenceRefs,
        DateTimeOffset createdAt) =>
        new(
            SchemaVersion: 1,
            SnapshotId: request.SnapshotId!.Trim(),
            ActionId: request.ActionId!.Trim(),
            CreatedAtUtc: createdAt,
            Classification: Classification,
            StaleStateClassification: StaleStateClassification,
            SourceSurfaceModelVersion: request.OperatorSurface!.SurfaceId,
            SourceChainIds: sourceChainIds,
            SourceChainContentHashes: sourceChainHashes,
            SafeRelativePathsOnly: safeRelativePaths,
            EvidenceRefs: evidenceRefs,
            DecisionStateSummaryRedacted: Summary("approval", request.OperatorSurface.ApprovalDecisionState.State.ToString(), request.OperatorSurface.ApprovalDecisionState.ApprovalId),
            NoOpExecutionStateSummaryRedacted: Summary("noop", request.OperatorSurface.ApprovedActionExecutionState.State.ToString(), request.OperatorSurface.ApprovedActionExecutionState.ExecutionId),
            BoundedActionStateSummaryRedacted: Summary("bounded", request.OperatorSurface.BoundedApprovedActionState.State.ToString(), request.OperatorSurface.BoundedApprovedActionState.ExecutionId),
            LocalHandoffDraftStateSummaryRedacted: Summary("local-handoff", request.OperatorSurface.HandoffReportDraftState.State.ToString(), request.OperatorSurface.HandoffReportDraftState.DraftId),
            WorkspaceTestJailDraftStateSummaryRedacted: Summary("workspace-test-jail", request.OperatorSurface.WorkspaceTestJailHandoffDraftState.State.ToString(), request.OperatorSurface.WorkspaceTestJailHandoffDraftState.DraftId),
            UserWorkspaceAllowlistedDraftStateSummaryRedacted: Summary("user-workspace-allowlisted", request.OperatorSurface.UserWorkspaceAllowlistedHandoffDraftState.State.ToString(), request.OperatorSurface.UserWorkspaceAllowlistedHandoffDraftState.DraftId),
            BlockerSummaryRedacted: "none",
            WarningSummaryRedacted: "stale snapshots are historical evidence only; not authority live/product",
            NegativeFlags: NegativeFlags,
            OperatorSurfaceModelHash: operatorSurfaceHash,
            SnapshotContentHash: string.Empty,
            CheckpointHash: checkpointHash,
            SafeRelativeSnapshotPath: relativePath,
            LocalInternalDevelopmentOnly: true,
            HistoricalEvidenceOnly: true,
            AuthorityLiveProduct: false,
            PublicProduct: false,
            Production: false,
            ReleaseCommercial: false,
            ComplianceCustody: false);

    private static string ContentHashForPayload(ProductLedgerLocalOperatorSurfaceLatestStateSnapshotPayload payload) =>
        HashText(JsonSerializer.Serialize(payload with { SnapshotContentHash = string.Empty }, SnapshotJsonOptions));

    private static bool ExistingPayloadMatches(
        ProductLedgerLocalOperatorSurfaceLatestStateSnapshotPayload payload,
        ProductLedgerLocalOperatorSurfaceLatestStateSnapshotRequest request,
        string relativePath,
        string operatorSurfaceHash,
        string checkpointHash,
        IReadOnlyList<string> sourceChainIds,
        IReadOnlyList<string> sourceChainHashes,
        IReadOnlyList<string> safeRelativePaths,
        IReadOnlyList<string> evidenceRefs) =>
        payload.SchemaVersion == 1
        && string.Equals(payload.SnapshotId, request.SnapshotId?.Trim(), StringComparison.Ordinal)
        && string.Equals(payload.ActionId, request.ActionId?.Trim(), StringComparison.Ordinal)
        && string.Equals(payload.Classification, Classification, StringComparison.Ordinal)
        && string.Equals(payload.StaleStateClassification, StaleStateClassification, StringComparison.Ordinal)
        && string.Equals(payload.SourceSurfaceModelVersion, request.OperatorSurface?.SurfaceId, StringComparison.Ordinal)
        && payload.SourceChainIds.SequenceEqual(sourceChainIds, StringComparer.Ordinal)
        && payload.SourceChainContentHashes.SequenceEqual(sourceChainHashes, StringComparer.Ordinal)
        && payload.SafeRelativePathsOnly.SequenceEqual(safeRelativePaths, StringComparer.Ordinal)
        && payload.EvidenceRefs.SequenceEqual(evidenceRefs, StringComparer.Ordinal)
        && payload.NegativeFlags.SequenceEqual(NegativeFlags, StringComparer.Ordinal)
        && string.Equals(payload.OperatorSurfaceModelHash, operatorSurfaceHash, StringComparison.Ordinal)
        && string.Equals(payload.SnapshotContentHash, ContentHashForPayload(payload), StringComparison.Ordinal)
        && string.Equals(payload.CheckpointHash, checkpointHash, StringComparison.Ordinal)
        && string.Equals(payload.SafeRelativeSnapshotPath, relativePath, StringComparison.Ordinal)
        && payload.LocalInternalDevelopmentOnly
        && payload.HistoricalEvidenceOnly
        && !payload.AuthorityLiveProduct
        && !payload.PublicProduct
        && !payload.Production
        && !payload.ReleaseCommercial
        && !payload.ComplianceCustody;

    private static IReadOnlyList<string> SourceChainIds(ProductLedgerOperatorSurfaceModel model) =>
    [
        model.ApprovalDecisionState.ApprovalId,
        model.ApprovedActionExecutionState.ExecutionId,
        model.BoundedApprovedActionState.ExecutionId,
        model.HandoffReportDraftState.DraftId,
        model.WorkspaceTestJailHandoffDraftState.DraftId,
        model.UserWorkspaceAllowlistedHandoffDraftState.DraftId
    ];

    private static IReadOnlyList<string> SourceChainContentHashes(ProductLedgerOperatorSurfaceModel model) =>
    [
        model.ApprovalDecisionState.DecisionHashPrefix,
        model.ApprovedActionExecutionState.ExecutionResultHashPrefix,
        model.BoundedApprovedActionState.ResultHashPrefix,
        model.HandoffReportDraftState.ContentHashPrefix,
        model.WorkspaceTestJailHandoffDraftState.ContentHashPrefix,
        model.UserWorkspaceAllowlistedHandoffDraftState.ContentHashPrefix
    ];

    private static IReadOnlyList<string> SafeRelativePaths(ProductLedgerOperatorSurfaceModel model) =>
    [
        model.HandoffReportDraftState.DraftRelativePath,
        model.WorkspaceTestJailHandoffDraftState.DraftRelativePath,
        model.UserWorkspaceAllowlistedHandoffDraftState.DraftRelativePath
    ];

    private static IReadOnlyList<string> EvidenceRefs(ProductLedgerLocalOperatorSurfaceLatestStateSnapshotRequest request) =>
        (request.EvidenceReferences ?? [])
        .Concat(request.OperatorSurface?.EvidenceRefs.Select(evidence => evidence.EvidenceId) ?? [])
        .Concat(request.OperatorSurface?.ApprovalDecisionState.EvidenceReferences ?? [])
        .Concat(request.OperatorSurface?.ApprovedActionExecutionState.EvidenceReferences ?? [])
        .Concat(request.OperatorSurface?.BoundedApprovedActionState.EvidenceReferences ?? [])
        .Concat(request.OperatorSurface?.HandoffReportDraftState.EvidenceRefs ?? [])
        .Concat(request.OperatorSurface?.WorkspaceTestJailHandoffDraftState.EvidenceRefs ?? [])
        .Concat(request.OperatorSurface?.UserWorkspaceAllowlistedHandoffDraftState.EvidenceRefs ?? [])
        .Concat(["product-ledger-operator-surface-latest-state-snapshot-created"])
        .Select(evidence => evidence.Trim())
        .Where(evidence => !string.IsNullOrWhiteSpace(evidence))
        .Distinct(StringComparer.Ordinal)
        .OrderBy(evidence => evidence, StringComparer.Ordinal)
        .ToArray();

    private static ProductLedgerLocalOperatorSurfaceLatestStateSnapshotResult SnapshotFrom(
        ProductLedgerLocalOperatorSurfaceLatestStateSnapshotRequest request,
        string relativePath,
        string operatorSurfaceHash,
        string contentHash,
        string checkpointHash,
        IReadOnlyList<string> sourceChainIds,
        IReadOnlyList<string> sourceChainHashes,
        IReadOnlyList<string> safeRelativePaths,
        IReadOnlyList<string> evidenceRefs,
        DateTimeOffset createdAt,
        ProductLedgerLocalOperatorSurfaceLatestStateSnapshotDecision decision) =>
        new(
            Decision: decision,
            State: ProductLedgerLocalOperatorSurfaceLatestStateSnapshotState.SnapshotCreatedLocalOnly,
            Blockers: [],
            SnapshotId: request.SnapshotId!.Trim(),
            ActionId: request.ActionId!.Trim(),
            SnapshotRelativePath: relativePath,
            AllowedBoundary: AllowedRelativeOutputBoundary,
            Classification: Classification,
            StaleStateClassification: StaleStateClassification,
            OperatorSurfaceModelHash: operatorSurfaceHash,
            OperatorSurfaceModelHashPrefix: Prefix(operatorSurfaceHash),
            SnapshotContentHash: contentHash,
            SnapshotContentHashPrefix: Prefix(contentHash),
            CheckpointHash: checkpointHash,
            CheckpointHashPrefix: Prefix(checkpointHash),
            SourceChainIds: sourceChainIds,
            SourceChainContentHashes: sourceChainHashes,
            SafeRelativePathsOnly: safeRelativePaths,
            EvidenceRefs: evidenceRefs,
            NegativeFlags: NegativeFlags,
            CreateOnly: true,
            Versioned: true,
            JsonOnly: true,
            OverwriteAllowed: false,
            LatestPointerOverwriteAllowed: false,
            UserSelectedPathAllowed: false,
            PayloadControlledRootAllowed: false,
            CanonicalizationPassed: true,
            ReparseValidationPassed: true,
            RedactionApplied: true,
            LocalOnly: true,
            InternalOnly: true,
            DevelopmentOnly: true,
            HistoricalEvidenceOnly: true,
            AuthorityLiveProduct: false,
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
            CreatedAtUtc: createdAt,
            StatusText: CompletedStatus);

    private static ProductLedgerLocalOperatorSurfaceLatestStateSnapshotResult SnapshotRejected(
        IReadOnlyList<ProductLedgerLocalOperatorSurfaceLatestStateSnapshotBlocker> blockers) =>
        ProductLedgerLocalOperatorSurfaceLatestStateSnapshotResult.Pending with
        {
            Decision = ProductLedgerLocalOperatorSurfaceLatestStateSnapshotDecision.Rejected,
            State = blockers.Contains(ProductLedgerLocalOperatorSurfaceLatestStateSnapshotBlocker.ExistingSnapshotConflict)
                || blockers.Contains(ProductLedgerLocalOperatorSurfaceLatestStateSnapshotBlocker.ExistingSnapshotCorrupt)
                    ? ProductLedgerLocalOperatorSurfaceLatestStateSnapshotState.SnapshotReplayBlocked
                    : ProductLedgerLocalOperatorSurfaceLatestStateSnapshotState.SnapshotBlocked,
            Blockers = blockers.Distinct().OrderBy(blocker => blocker.ToString(), StringComparer.Ordinal).ToArray(),
            StatusText = RejectedStatus
        };

    private ProductLedgerLocalOperatorSurfaceLatestStateSnapshotResult Remember(
        ProductLedgerLocalOperatorSurfaceLatestStateSnapshotResult snapshot)
    {
        lastSnapshot = snapshot;
        return snapshot;
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

    private static string NormalizeId(string value)
    {
        var chars = value.Trim().ToLowerInvariant()
            .Select(ch => char.IsAsciiLetterOrDigit(ch) ? ch : '-')
            .ToArray();
        return new string(chars).Trim('-');
    }

    private static string Summary(string kind, string state, string id) =>
        $"{kind}:{state}:{id}";

    private static string HashText(string material)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(material));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string Prefix(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "none" : value[..Math.Min(12, value.Length)];
}

public sealed record ProductLedgerLocalOperatorSurfaceLatestStateSnapshotPayload(
    int SchemaVersion,
    string SnapshotId,
    string ActionId,
    DateTimeOffset CreatedAtUtc,
    string Classification,
    string StaleStateClassification,
    string SourceSurfaceModelVersion,
    IReadOnlyList<string> SourceChainIds,
    IReadOnlyList<string> SourceChainContentHashes,
    IReadOnlyList<string> SafeRelativePathsOnly,
    IReadOnlyList<string> EvidenceRefs,
    string DecisionStateSummaryRedacted,
    string NoOpExecutionStateSummaryRedacted,
    string BoundedActionStateSummaryRedacted,
    string LocalHandoffDraftStateSummaryRedacted,
    string WorkspaceTestJailDraftStateSummaryRedacted,
    string UserWorkspaceAllowlistedDraftStateSummaryRedacted,
    string BlockerSummaryRedacted,
    string WarningSummaryRedacted,
    IReadOnlyList<string> NegativeFlags,
    string OperatorSurfaceModelHash,
    string SnapshotContentHash,
    string CheckpointHash,
    string SafeRelativeSnapshotPath,
    bool LocalInternalDevelopmentOnly,
    bool HistoricalEvidenceOnly,
    bool AuthorityLiveProduct,
    bool PublicProduct,
    bool Production,
    bool ReleaseCommercial,
    bool ComplianceCustody);
