using System.Security.Cryptography;
using System.Text;

namespace OneBrain.Core.Approval;

public enum ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftDecision
{
    Rejected,
    DraftCreatedUserWorkspaceAllowlistedOnly,
    IdempotentReplay
}

public enum ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftState
{
    Pending,
    DraftCreatedUserWorkspaceAllowlistedOnly,
    DraftBlocked,
    DraftReplayBlocked
}

public enum ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftActionKind
{
    LocalUserWorkspaceAllowlistedHandoffDraftCreateOnly
}

public enum ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftBlocker
{
    MissingRequest,
    MissingExplicitUserWorkspaceAllowlistedScope,
    NonDevelopmentMode,
    NonLocalMode,
    NonInternalMode,
    MissingTrustedWorkspaceRoot,
    TrustedWorkspaceRootRejected,
    MissingActionId,
    UnsafeActionId,
    MissingCandidateId,
    UnknownActionKind,
    MissingApprovalDecision,
    ApprovalDecisionNotApproved,
    ApprovalDecisionHasBlockers,
    MissingNoOpExecution,
    NoOpExecutionNotCompleted,
    MissingBoundedExecution,
    BoundedExecutionNotCompleted,
    MissingLocalApprovedHandoffDraft,
    LocalApprovedHandoffDraftNotCompleted,
    LocalApprovedHandoffDraftMismatch,
    MissingWorkspaceTestJailHandoffDraft,
    WorkspaceTestJailHandoffDraftNotCompleted,
    WorkspaceTestJailHandoffDraftMismatch,
    MissingCandidateActionKind,
    CandidateActionMismatch,
    MissingCandidateEvidenceHash,
    CandidateEvidenceHashMismatch,
    MissingEvidenceReferences,
    MissingSafeDraftContent,
    RedactionRejected,
    PathCanonicalizationFailed,
    ReparsePointRejected,
    FinalPathOutsideAllowedBoundary,
    ExistingDraftConflict,
    IoFailure,
    PayloadPathFieldRejected,
    PayloadCommandFieldRejected,
    PayloadNetworkProviderFieldRejected,
    ClaimsArbitraryPathInput,
    ClaimsFilesystemScan,
    RequestsOverwrite,
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

public sealed record ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftOptions(
    string TrustedWorkspaceRootPath,
    string WorkspaceClassification,
    bool ExplicitUserWorkspaceAllowlistedBoundary,
    bool AllowsArbitraryPathInput,
    bool AllowsFilesystemScan,
    bool AllowsOverwrite,
    bool AllowsUserSelectedPath,
    bool AllowsShellOrSubprocess,
    bool AllowsCommandExecution,
    bool AllowsNetwork,
    bool AllowsDb,
    bool AllowsKmsWormExternalTrust,
    bool AllowsReleaseCommercial);

public sealed record ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftRequest(
    bool ExplicitUserWorkspaceAllowlistedScope,
    bool DevelopmentMode,
    bool LocalMode,
    bool InternalMode,
    string? ActionId,
    string? CandidateId,
    ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftActionKind? ActionKind,
    ProductLedgerLocalApprovalDecisionSnapshot? ApprovalDecision,
    ProductLedgerLocalApprovedActionExecutionSnapshot? NoOpExecution,
    ProductLedgerLocalBoundedApprovedActionSnapshot? BoundedExecution,
    ProductLedgerLocalApprovedHandoffReportDraftSnapshot? LocalApprovedHandoffDraft,
    ProductLedgerLocalWorkspaceTestJailHandoffDraftSnapshot? WorkspaceTestJailHandoffDraft,
    ProductLedgerInternalCommandKind? CandidateActionKind,
    string? CandidateEvidenceHash,
    string? CurrentEvidenceHash,
    string? LocalApprovedHandoffDraftContentHash,
    string? WorkspaceTestJailHandoffDraftContentHash,
    string? DraftTitle,
    string? RedactedDraftSummary,
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

public sealed record ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftSnapshot(
    ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftDecision Decision,
    ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftState State,
    IReadOnlyList<ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftBlocker> Blockers,
    string ActionId,
    string CandidateId,
    string ApprovalId,
    string NoOpExecutionId,
    string BoundedExecutionId,
    string LocalApprovedHandoffDraftId,
    string WorkspaceTestJailHandoffDraftId,
    string WorkspaceId,
    string DraftId,
    string DraftRelativePath,
    string AllowedBoundary,
    string WorkspaceClassification,
    string CanonicalWorkspaceRootHash,
    string CanonicalFinalPathHash,
    string ActionKind,
    string Scope,
    bool CreateOnly,
    bool OverwriteAllowed,
    bool UserSelectedPathAllowed,
    bool PayloadControlledRootAllowed,
    bool CanonicalizationPassed,
    bool ReparseValidationPassed,
    bool UserWorkspaceAllowlistedBoundaryOnly,
    bool ShellAllowed,
    bool NetworkAllowed,
    bool ProductionAllowed,
    bool PublicProductAllowed,
    bool RedactionApplied,
    IReadOnlyList<string> EvidenceRefs,
    string ContentHash,
    string ContentHashPrefix,
    DateTimeOffset CreatedAt,
    bool LocalOnly,
    bool InternalOnly,
    bool DevelopmentOnly,
    bool FailClosed,
    bool ProductCommandExecuted,
    bool PublicUiActionAvailable,
    bool ProductCommandHandlerAvailable,
    bool ProductiveServiceRegistrationAvailable,
    bool ProviderCloudNetworkAvailable,
    bool DbMigrationAvailable,
    bool KmsWormExternalTrustAvailable,
    bool BrowserCdpWcuOcrRecipesLiveAvailable,
    bool PilotRunAvailable,
    bool ReleaseCommercialReady,
    string StatusText)
{
    public static ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftSnapshot Pending { get; } =
        new(
            Decision: ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftDecision.Rejected,
            State: ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftState.Pending,
            Blockers: [],
            ActionId: "user-workspace-allowlisted-handoff-draft.pending",
            CandidateId: "candidate.pending",
            ApprovalId: "approval-state.pending-preview-only",
            NoOpExecutionId: "approval-execution.pending-no-op",
            BoundedExecutionId: "approval-bounded-action.pending",
            LocalApprovedHandoffDraftId: "local-handoff-draft.pending",
            WorkspaceTestJailHandoffDraftId: "workspace-test-jail-draft.pending",
            WorkspaceId: ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftExecutor.ScopeId,
            DraftId: "user-workspace-allowlisted-draft.pending",
            DraftRelativePath: string.Empty,
            AllowedBoundary: ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftExecutor.AllowedRelativeOutputBoundary,
            WorkspaceClassification: ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftExecutor.WorkspaceClassification,
            CanonicalWorkspaceRootHash: "none",
            CanonicalFinalPathHash: "none",
            ActionKind: ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftActionKind.LocalUserWorkspaceAllowlistedHandoffDraftCreateOnly.ToString(),
            Scope: ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftExecutor.ScopeId,
            CreateOnly: true,
            OverwriteAllowed: false,
            UserSelectedPathAllowed: false,
            PayloadControlledRootAllowed: false,
            CanonicalizationPassed: false,
            ReparseValidationPassed: false,
            UserWorkspaceAllowlistedBoundaryOnly: true,
            ShellAllowed: false,
            NetworkAllowed: false,
            ProductionAllowed: false,
            PublicProductAllowed: false,
            RedactionApplied: false,
            EvidenceRefs: ["product-ledger-user-workspace-allowlisted-handoff-draft-pending"],
            ContentHash: string.Empty,
            ContentHashPrefix: "none",
            CreatedAt: DateTimeOffset.UnixEpoch,
            LocalOnly: true,
            InternalOnly: true,
            DevelopmentOnly: true,
            FailClosed: true,
            ProductCommandExecuted: false,
            PublicUiActionAvailable: false,
            ProductCommandHandlerAvailable: false,
            ProductiveServiceRegistrationAvailable: false,
            ProviderCloudNetworkAvailable: false,
            DbMigrationAvailable: false,
            KmsWormExternalTrustAvailable: false,
            BrowserCdpWcuOcrRecipesLiveAvailable: false,
            PilotRunAvailable: false,
            ReleaseCommercialReady: false,
            StatusText: ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftExecutor.PendingStatus);
}

public sealed class ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftExecutor
{
    public const string AllowedRelativeOutputBoundary = "docs/nodal-os/handoffs/";
    public const string WorkspaceClassification = "USER_WORKSPACE_ALLOWLISTED_BOUNDARY_ONLY";
    public const string ScopeId = "LocalInternalDevelopmentOnlyUserWorkspaceAllowlistedBoundary";

    public const string PendingStatus =
        "PRODUCT_LEDGER_USER_WORKSPACE_ALLOWLISTED_HANDOFF_DRAFT_PENDING LOCAL_ONLY INTERNAL_ONLY DEVELOPMENT_ONLY USER_WORKSPACE_ALLOWLISTED_BOUNDARY_ONLY CREATE_ONLY NO_OVERWRITE FAIL_CLOSED NO_ARBITRARY_PATH NO_USER_SELECTED_PATH NO_COMMAND_EXECUTION NO_SHELL_SUBPROCESS NO_PUBLIC_PRODUCT_PATH NO_PRODUCTION_ROUTE NO_PROVIDER_CLOUD_NETWORK NO_DB_MIGRATION NO_KMS_WORM_EXTERNAL_TRUST NO_LIVE_AUTOMATION NO_RELEASE_COMMERCIAL";

    public const string CompletedStatus =
        "PRODUCT_LEDGER_USER_WORKSPACE_ALLOWLISTED_HANDOFF_DRAFT_CREATED LOCAL_ONLY INTERNAL_ONLY DEVELOPMENT_ONLY USER_WORKSPACE_ALLOWLISTED_BOUNDARY_ONLY CREATE_ONLY NO_OVERWRITE FAIL_CLOSED REDACTION_APPLIED CANONICALIZED REPARSE_VALIDATED NO_ARBITRARY_PATH NO_USER_SELECTED_PATH NO_COMMAND_EXECUTION NO_SHELL_SUBPROCESS NO_PUBLIC_PRODUCT_PATH NO_PRODUCTION_ROUTE NO_PROVIDER_CLOUD_NETWORK NO_DB_MIGRATION NO_KMS_WORM_EXTERNAL_TRUST NO_LIVE_AUTOMATION NO_RELEASE_COMMERCIAL";

    public const string RejectedStatus =
        "PRODUCT_LEDGER_USER_WORKSPACE_ALLOWLISTED_HANDOFF_DRAFT_REJECTED FAIL_CLOSED USER_WORKSPACE_ALLOWLISTED_BOUNDARY_ONLY NO_OVERWRITE NO_ARBITRARY_PATH NO_USER_SELECTED_PATH NO_COMMAND_EXECUTION NO_SHELL_SUBPROCESS NO_PUBLIC_PRODUCT_PATH NO_PRODUCTION_ROUTE NO_PROVIDER_CLOUD_NETWORK NO_DB_MIGRATION NO_KMS_WORM_EXTERNAL_TRUST NO_LIVE_AUTOMATION NO_RELEASE_COMMERCIAL";

    private readonly ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftOptions options;
    private ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftSnapshot? lastSnapshot;

    public ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftExecutor(
        ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftOptions options)
    {
        this.options = options;
    }

    public ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftSnapshot CreateDraft(
        ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftRequest? request)
    {
        var blockers = ValidateRequest(request);
        if (blockers.Count > 0 || request is null)
        {
            return Remember(SnapshotRejected(blockers));
        }

        var createdAt = DateTimeOffset.UnixEpoch;
        var canonicalRoot = CanonicalRoot();
        if (canonicalRoot is null)
        {
            return Remember(SnapshotRejected([ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftBlocker.TrustedWorkspaceRootRejected]));
        }

        var relativePath = DraftRelativePath(request);
        var fullPath = FullDraftPath(canonicalRoot, relativePath);
        if (fullPath is null)
        {
            return Remember(SnapshotRejected([ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftBlocker.FinalPathOutsideAllowedBoundary]));
        }

        var evidenceRefs = EvidenceRefs(request);
        var content = DraftContent(request, relativePath, evidenceRefs, createdAt);
        var contentHash = HashText(content);

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
            if (ContainsReparsePoint(canonicalRoot)
                || ContainsReparsePoint(Path.Combine(canonicalRoot, "docs"))
                || ContainsReparsePoint(Path.Combine(canonicalRoot, "docs", "nodal-os"))
                || ContainsReparsePoint(Path.GetDirectoryName(fullPath)!))
            {
                return Remember(SnapshotRejected([ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftBlocker.ReparsePointRejected]));
            }

            if (File.Exists(fullPath))
            {
                var existing = File.ReadAllText(fullPath, Encoding.UTF8);
                if (string.Equals(HashText(existing), contentHash, StringComparison.Ordinal))
                {
                    return Remember(SnapshotFrom(
                        request,
                        relativePath,
                        canonicalRoot,
                        fullPath,
                        evidenceRefs,
                        contentHash,
                        createdAt,
                        ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftDecision.IdempotentReplay));
                }

                return Remember(SnapshotRejected([ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftBlocker.ExistingDraftConflict]));
            }

            using (var stream = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            using (var writer = new StreamWriter(stream, Encoding.UTF8))
            {
                writer.Write(content);
            }

            return Remember(SnapshotFrom(
                request,
                relativePath,
                canonicalRoot,
                fullPath,
                evidenceRefs,
                contentHash,
                createdAt,
                ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftDecision.DraftCreatedUserWorkspaceAllowlistedOnly));
        }
        catch (IOException)
        {
            return Remember(SnapshotRejected([ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftBlocker.IoFailure]));
        }
        catch (UnauthorizedAccessException)
        {
            return Remember(SnapshotRejected([ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftBlocker.IoFailure]));
        }
        catch (ArgumentException)
        {
            return Remember(SnapshotRejected([ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftBlocker.PathCanonicalizationFailed]));
        }
    }

    public ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftSnapshot Read() =>
        lastSnapshot ?? ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftSnapshot.Pending;

    private IReadOnlyList<ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftBlocker> ValidateRequest(
        ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftRequest? request)
    {
        var blockers = new List<ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftBlocker>();
        if (request is null)
        {
            blockers.Add(ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftBlocker.MissingRequest);
            return blockers;
        }

        if (!BoundaryAllowed())
        {
            blockers.Add(ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftBlocker.TrustedWorkspaceRootRejected);
        }

        if (!request.ExplicitUserWorkspaceAllowlistedScope)
        {
            blockers.Add(ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftBlocker.MissingExplicitUserWorkspaceAllowlistedScope);
        }

        if (!request.DevelopmentMode)
        {
            blockers.Add(ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftBlocker.NonDevelopmentMode);
        }

        if (!request.LocalMode)
        {
            blockers.Add(ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftBlocker.NonLocalMode);
        }

        if (!request.InternalMode)
        {
            blockers.Add(ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftBlocker.NonInternalMode);
        }

        if (string.IsNullOrWhiteSpace(request.ActionId) || string.IsNullOrWhiteSpace(request.CandidateId))
        {
            blockers.Add(string.IsNullOrWhiteSpace(request.ActionId)
                ? ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftBlocker.MissingActionId
                : ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftBlocker.MissingCandidateId);
        }
        else if (!IsSafeId(request.ActionId) || !IsSafeId(request.CandidateId))
        {
            blockers.Add(ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftBlocker.UnsafeActionId);
        }

        if (request.ActionKind != ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftActionKind.LocalUserWorkspaceAllowlistedHandoffDraftCreateOnly)
        {
            blockers.Add(ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftBlocker.UnknownActionKind);
        }

        if (request.CandidateActionKind is null)
        {
            blockers.Add(ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftBlocker.MissingCandidateActionKind);
        }

        AddApprovalDecisionBlockers(request, blockers);
        AddNoOpBlockers(request, blockers);
        AddBoundedExecutionBlockers(request, blockers);
        AddLocalHandoffDraftBlockers(request, blockers);
        AddWorkspaceTestJailDraftBlockers(request, blockers);
        AddEvidenceBlockers(request, blockers);
        AddPayloadBlockers(request, blockers);
        AddRedactionBlockers(request, blockers);
        return blockers.Distinct().OrderBy(blocker => blocker.ToString(), StringComparer.Ordinal).ToArray();
    }

    private static void AddApprovalDecisionBlockers(
        ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftRequest request,
        List<ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftBlocker> blockers)
    {
        var approval = request.ApprovalDecision;
        if (approval is null)
        {
            blockers.Add(ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftBlocker.MissingApprovalDecision);
            return;
        }

        if (approval.State != ProductLedgerLocalApprovalDecisionState.ApprovedLocalOnly
            || !string.Equals(approval.OperatorDecision, ProductLedgerLocalApprovalOperatorDecisionKind.Approve.ToString(), StringComparison.Ordinal))
        {
            blockers.Add(ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftBlocker.ApprovalDecisionNotApproved);
        }

        if (approval.Blockers.Count > 0 || approval.Decision == ProductLedgerLocalApprovalDecisionStoreDecision.Rejected)
        {
            blockers.Add(ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftBlocker.ApprovalDecisionHasBlockers);
        }

        if (request.CandidateActionKind is not null
            && !string.Equals(approval.CandidateActionKind, request.CandidateActionKind.Value.ToString(), StringComparison.Ordinal))
        {
            blockers.Add(ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftBlocker.CandidateActionMismatch);
        }
    }

    private static void AddNoOpBlockers(
        ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftRequest request,
        List<ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftBlocker> blockers)
    {
        var noOp = request.NoOpExecution;
        if (noOp is null)
        {
            blockers.Add(ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftBlocker.MissingNoOpExecution);
            return;
        }

        if (noOp.State != ProductLedgerLocalApprovedActionExecutionState.NoOpExecutionCompletedLocalOnly
            || noOp.Blockers.Count > 0
            || noOp.ProductCommandExecuted
            || noOp.PublicUiActionAvailable
            || noOp.ProductCommandHandlerAvailable
            || noOp.FileWriteOutsideExecutionStorePerformed)
        {
            blockers.Add(ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftBlocker.NoOpExecutionNotCompleted);
        }
    }

    private static void AddBoundedExecutionBlockers(
        ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftRequest request,
        List<ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftBlocker> blockers)
    {
        var bounded = request.BoundedExecution;
        if (bounded is null)
        {
            blockers.Add(ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftBlocker.MissingBoundedExecution);
            return;
        }

        if (bounded.State != ProductLedgerLocalBoundedApprovedActionState.BoundedExecutionCompletedLocalOnly
            || bounded.Blockers.Count > 0
            || bounded.TouchesUserFiles
            || bounded.ProductCommandExecuted
            || bounded.PublicUiActionAvailable
            || bounded.ProductCommandHandlerAvailable
            || bounded.FileWriteOutsideExecutionStorePerformed)
        {
            blockers.Add(ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftBlocker.BoundedExecutionNotCompleted);
        }
    }

    private static void AddLocalHandoffDraftBlockers(
        ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftRequest request,
        List<ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftBlocker> blockers)
    {
        var predecessor = request.LocalApprovedHandoffDraft;
        if (predecessor is null)
        {
            blockers.Add(ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftBlocker.MissingLocalApprovedHandoffDraft);
            return;
        }

        if (predecessor.State != ProductLedgerLocalApprovedHandoffReportDraftState.DraftCreatedLocalOnly
            || predecessor.Blockers.Count > 0
            || !predecessor.LocalOnly
            || !predecessor.InternalOnly
            || !predecessor.DevelopmentOnly
            || !predecessor.CreateOnly
            || predecessor.OverwriteAllowed
            || predecessor.UserFileWrite
            || predecessor.ShellAllowed
            || predecessor.NetworkAllowed
            || predecessor.ProductionAllowed
            || predecessor.PublicProductAllowed
            || !predecessor.RedactionApplied)
        {
            blockers.Add(ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftBlocker.LocalApprovedHandoffDraftNotCompleted);
        }

        if (string.IsNullOrWhiteSpace(request.LocalApprovedHandoffDraftContentHash)
            || string.IsNullOrWhiteSpace(predecessor.ContentHash)
            || !string.Equals(request.LocalApprovedHandoffDraftContentHash.Trim(), predecessor.ContentHash, StringComparison.Ordinal))
        {
            blockers.Add(ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftBlocker.LocalApprovedHandoffDraftMismatch);
        }
    }

    private static void AddWorkspaceTestJailDraftBlockers(
        ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftRequest request,
        List<ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftBlocker> blockers)
    {
        var predecessor = request.WorkspaceTestJailHandoffDraft;
        if (predecessor is null)
        {
            blockers.Add(ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftBlocker.MissingWorkspaceTestJailHandoffDraft);
            return;
        }

        if (predecessor.State != ProductLedgerLocalWorkspaceTestJailHandoffDraftState.DraftCreatedWorkspaceTestJailOnly
            || predecessor.Blockers.Count > 0
            || !predecessor.LocalOnly
            || !predecessor.InternalOnly
            || !predecessor.DevelopmentOnly
            || !predecessor.CreateOnly
            || predecessor.OverwriteAllowed
            || !predecessor.WorkspaceTestJailOnly
            || predecessor.UserSelectedPathAllowed
            || predecessor.PayloadControlledRootAllowed
            || predecessor.ShellAllowed
            || predecessor.NetworkAllowed
            || predecessor.ProductionAllowed
            || predecessor.PublicProductAllowed
            || !predecessor.RedactionApplied)
        {
            blockers.Add(ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftBlocker.WorkspaceTestJailHandoffDraftNotCompleted);
        }

        if (string.IsNullOrWhiteSpace(request.WorkspaceTestJailHandoffDraftContentHash)
            || string.IsNullOrWhiteSpace(predecessor.ContentHash)
            || !string.Equals(request.WorkspaceTestJailHandoffDraftContentHash.Trim(), predecessor.ContentHash, StringComparison.Ordinal))
        {
            blockers.Add(ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftBlocker.WorkspaceTestJailHandoffDraftMismatch);
        }
    }

    private static void AddEvidenceBlockers(
        ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftRequest request,
        List<ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftBlocker> blockers)
    {
        if (string.IsNullOrWhiteSpace(request.CandidateEvidenceHash)
            || string.IsNullOrWhiteSpace(request.CurrentEvidenceHash)
            || string.IsNullOrWhiteSpace(request.ApprovalDecision?.CandidateEvidenceHash)
            || string.IsNullOrWhiteSpace(request.NoOpExecution?.CandidateEvidenceHash)
            || string.IsNullOrWhiteSpace(request.BoundedExecution?.CandidateEvidenceHash)
            || string.IsNullOrWhiteSpace(request.WorkspaceTestJailHandoffDraft?.ContentHash))
        {
            blockers.Add(ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftBlocker.MissingCandidateEvidenceHash);
        }
        else if (!string.Equals(request.CandidateEvidenceHash.Trim(), request.CurrentEvidenceHash.Trim(), StringComparison.Ordinal)
            || !string.Equals(request.CandidateEvidenceHash.Trim(), request.ApprovalDecision.CandidateEvidenceHash, StringComparison.Ordinal)
            || !string.Equals(request.CandidateEvidenceHash.Trim(), request.NoOpExecution.CandidateEvidenceHash, StringComparison.Ordinal)
            || !string.Equals(request.CandidateEvidenceHash.Trim(), request.BoundedExecution.CandidateEvidenceHash, StringComparison.Ordinal))
        {
            blockers.Add(ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftBlocker.CandidateEvidenceHashMismatch);
        }

        if (request.EvidenceReferences is null
            || request.EvidenceReferences.Count == 0
            || request.EvidenceReferences.Any(string.IsNullOrWhiteSpace)
            || request.ApprovalDecision?.EvidenceReferences.Count == 0
            || request.NoOpExecution?.EvidenceReferences.Count == 0
            || request.BoundedExecution?.EvidenceReferences.Count == 0
            || request.LocalApprovedHandoffDraft?.EvidenceRefs.Count == 0
            || request.WorkspaceTestJailHandoffDraft?.EvidenceRefs.Count == 0)
        {
            blockers.Add(ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftBlocker.MissingEvidenceReferences);
        }

        if (string.IsNullOrWhiteSpace(request.DraftTitle)
            || string.IsNullOrWhiteSpace(request.RedactedDraftSummary))
        {
            blockers.Add(ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftBlocker.MissingSafeDraftContent);
        }
    }

    private static void AddPayloadBlockers(
        ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftRequest request,
        List<ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftBlocker> blockers)
    {
        if (ContainsPayload(request.ProposedPath) || ContainsPayload(request.ProposedRoot) || ContainsPayload(request.ProposedFilename))
        {
            blockers.Add(ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftBlocker.PayloadPathFieldRejected);
        }

        if (ContainsPayload(request.ProposedCommand))
        {
            blockers.Add(ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftBlocker.PayloadCommandFieldRejected);
        }

        if (ContainsPayload(request.ProposedUrl)
            || ContainsPayload(request.ProposedProvider)
            || ContainsPayload(request.ProposedDbMigration))
        {
            blockers.Add(ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftBlocker.PayloadNetworkProviderFieldRejected);
        }

        if (request.ClaimsArbitraryPathInput) blockers.Add(ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftBlocker.ClaimsArbitraryPathInput);
        if (request.ClaimsFilesystemScan) blockers.Add(ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftBlocker.ClaimsFilesystemScan);
        if (request.RequestsOverwrite) blockers.Add(ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftBlocker.RequestsOverwrite);
        if (request.RequestsUserSelectedPath) blockers.Add(ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftBlocker.RequestsUserSelectedPath);
        if (request.RequestsPublicUiAction) blockers.Add(ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftBlocker.RequestsPublicUiAction);
        if (request.RequestsProductCommandExecution) blockers.Add(ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftBlocker.RequestsProductCommandExecution);
        if (request.RequestsProductCommandHandler) blockers.Add(ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftBlocker.RequestsProductCommandHandler);
        if (request.RequestsProductiveServiceRegistration) blockers.Add(ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftBlocker.RequestsProductiveServiceRegistration);
        if (request.RequestsShellOrSubprocess) blockers.Add(ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftBlocker.RequestsShellOrSubprocess);
        if (request.ClaimsArbitraryCommandExecution) blockers.Add(ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftBlocker.ClaimsArbitraryCommandExecution);
        if (request.ClaimsProviderCloudNetwork) blockers.Add(ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftBlocker.ClaimsProviderCloudNetwork);
        if (request.ClaimsDbMigration) blockers.Add(ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftBlocker.ClaimsDbMigration);
        if (request.ClaimsKmsWormExternalTrust) blockers.Add(ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftBlocker.ClaimsKmsWormExternalTrust);
        if (request.ClaimsBrowserCdpWcuOcrRecipesLive) blockers.Add(ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftBlocker.ClaimsBrowserCdpWcuOcrRecipesLive);
        if (request.ClaimsPilotRun) blockers.Add(ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftBlocker.ClaimsPilotRun);
        if (request.ClaimsReleaseCommercial) blockers.Add(ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftBlocker.ClaimsReleaseCommercial);
    }

    private static void AddRedactionBlockers(
        ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftRequest request,
        List<ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftBlocker> blockers)
    {
        var redaction = new RedactionBeforePersistenceService().Evaluate(
            RedactionBeforePersistencePolicy.TestOnly,
            new DurableAuditTrailAppendOnlyMinimalRequest(
                EventKind: "ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraft",
                ActorReference: "local-internal-operator",
                ApprovalReference: request.ApprovalDecision?.ApprovalId ?? "approval-missing",
                EvidenceReferences: request.EvidenceReferences ?? [],
                Metadata: new Dictionary<string, string>
                {
                    ["actionId"] = request.ActionId ?? string.Empty,
                    ["actionKind"] = request.ActionKind?.ToString() ?? string.Empty,
                    ["candidateId"] = request.CandidateId ?? string.Empty,
                    ["candidateEvidenceHashPrefix"] = Prefix(request.CandidateEvidenceHash),
                    ["localApprovedHandoffDraftId"] = request.LocalApprovedHandoffDraft?.DraftId ?? string.Empty,
                    ["workspaceTestJailHandoffDraftId"] = request.WorkspaceTestJailHandoffDraft?.DraftId ?? string.Empty,
                    ["draftTitle"] = request.DraftTitle ?? string.Empty,
                    ["redactedDraftSummary"] = request.RedactedDraftSummary ?? string.Empty,
                    ["workspaceClassification"] = WorkspaceClassification,
                    ["allowedBoundary"] = AllowedRelativeOutputBoundary
                },
                RawPayload: null));

        if (!redaction.Succeeded || redaction.Decision != RedactionBeforePersistenceDecision.Allowed)
        {
            blockers.Add(ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftBlocker.RedactionRejected);
        }
    }

    private bool BoundaryAllowed()
    {
        if (!options.ExplicitUserWorkspaceAllowlistedBoundary
            || !string.Equals(options.WorkspaceClassification, WorkspaceClassification, StringComparison.Ordinal)
            || options.AllowsArbitraryPathInput
            || options.AllowsFilesystemScan
            || options.AllowsOverwrite
            || options.AllowsUserSelectedPath
            || options.AllowsShellOrSubprocess
            || options.AllowsCommandExecution
            || options.AllowsNetwork
            || options.AllowsDb
            || options.AllowsKmsWormExternalTrust
            || options.AllowsReleaseCommercial
            || string.IsNullOrWhiteSpace(options.TrustedWorkspaceRootPath))
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
            return Path.TrimEndingDirectorySeparator(Path.GetFullPath(options.TrustedWorkspaceRootPath));
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

    private static string? FullDraftPath(string canonicalRoot, string relativePath)
    {
        try
        {
            var boundaryRoot = Path.GetFullPath(Path.Combine(canonicalRoot, AllowedRelativeOutputBoundary.Replace('/', Path.DirectorySeparatorChar)));
            var fullPath = Path.GetFullPath(Path.Combine(canonicalRoot, relativePath.Replace('/', Path.DirectorySeparatorChar)));
            return StartsWithPath(boundaryRoot, fullPath) && fullPath.EndsWith(".md", StringComparison.OrdinalIgnoreCase)
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

    private static string DraftRelativePath(ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftRequest request) =>
        AllowedRelativeOutputBoundary
        + $"user-workspace-allowlisted-handoff-draft-{NormalizeId(request.ActionId!)}-{Prefix(request.CandidateEvidenceHash)}.md";

    private static string DraftContent(
        ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftRequest request,
        string relativePath,
        IReadOnlyList<string> evidenceRefs,
        DateTimeOffset createdAt)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# Local User Workspace Allowlisted Handoff Draft");
        builder.AppendLine();
        builder.AppendLine($"Action id: `{request.ActionId!.Trim()}`");
        builder.AppendLine($"Candidate id: `{request.CandidateId!.Trim()}`");
        builder.AppendLine($"Approval decision id: `{request.ApprovalDecision!.ApprovalId}`");
        builder.AppendLine($"No-op execution id: `{request.NoOpExecution!.ExecutionId}`");
        builder.AppendLine($"Bounded execution id: `{request.BoundedExecution!.ExecutionId}`");
        builder.AppendLine($"LocalApprovedHandoffReportDraft id: `{request.LocalApprovedHandoffDraft!.DraftId}`");
        builder.AppendLine($"LocalWorkspaceTestJailHandoffDraft id: `{request.WorkspaceTestJailHandoffDraft!.DraftId}`");
        builder.AppendLine($"Workspace classification: `{WorkspaceClassification}`");
        builder.AppendLine($"Allowed boundary: `{AllowedRelativeOutputBoundary}`");
        builder.AppendLine($"Created at UTC: `{createdAt:O}`");
        builder.AppendLine($"Safe relative path: `{relativePath}`");
        builder.AppendLine($"Content hash seed: `{Prefix(request.CandidateEvidenceHash)}`");
        builder.AppendLine();
        builder.AppendLine("## Redacted Decision And Execution Summary");
        builder.AppendLine();
        builder.AppendLine(request.RedactedDraftSummary!.Trim());
        builder.AppendLine();
        builder.AppendLine("## Evidence References");
        builder.AppendLine();
        foreach (var evidence in evidenceRefs.OrderBy(evidence => evidence, StringComparer.Ordinal))
        {
            builder.AppendLine($"- `{evidence}`");
        }

        builder.AppendLine();
        builder.AppendLine("## Negative Assertions");
        builder.AppendLine();
        builder.AppendLine("- Not production.");
        builder.AppendLine("- Not public/product.");
        builder.AppendLine("- User workspace allowlisted boundary only.");
        builder.AppendLine("- No user-selected path.");
        builder.AppendLine("- No arbitrary workspace write.");
        builder.AppendLine("- No overwrite.");
        builder.AppendLine("- No shell/subprocess.");
        builder.AppendLine("- No command execution.");
        builder.AppendLine("- No cloud/network/DB.");
        builder.AppendLine("- No KMS/WORM/compliance custody.");
        builder.AppendLine("- No Browser/CDP/WCU/OCR/Recipes live.");
        builder.AppendLine("- No Pilot /run.");
        builder.AppendLine("- No release/commercial.");
        builder.AppendLine("- No business signoff.");
        builder.AppendLine();
        builder.AppendLine("Safe next step: read-only audit of this user workspace allowlisted handoff draft action.");
        return builder.ToString();
    }

    private static IReadOnlyList<string> EvidenceRefs(
        ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftRequest request) =>
        (request.EvidenceReferences ?? [])
        .Concat(request.ApprovalDecision?.EvidenceReferences ?? [])
        .Concat(request.NoOpExecution?.EvidenceReferences ?? [])
        .Concat(request.BoundedExecution?.EvidenceReferences ?? [])
        .Concat(request.LocalApprovedHandoffDraft?.EvidenceRefs ?? [])
        .Concat(request.WorkspaceTestJailHandoffDraft?.EvidenceRefs ?? [])
        .Concat(["product-ledger-user-workspace-allowlisted-handoff-draft-created"])
        .Select(evidence => evidence.Trim())
        .Where(evidence => !string.IsNullOrWhiteSpace(evidence))
        .Distinct(StringComparer.Ordinal)
        .OrderBy(evidence => evidence, StringComparer.Ordinal)
        .ToArray();

    private static ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftSnapshot SnapshotFrom(
        ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftRequest request,
        string relativePath,
        string canonicalRoot,
        string fullPath,
        IReadOnlyList<string> evidenceRefs,
        string contentHash,
        DateTimeOffset createdAt,
        ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftDecision decision) =>
        new(
            Decision: decision,
            State: ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftState.DraftCreatedUserWorkspaceAllowlistedOnly,
            Blockers: [],
            ActionId: request.ActionId!.Trim(),
            CandidateId: request.CandidateId!.Trim(),
            ApprovalId: request.ApprovalDecision!.ApprovalId,
            NoOpExecutionId: request.NoOpExecution!.ExecutionId,
            BoundedExecutionId: request.BoundedExecution!.ExecutionId,
            LocalApprovedHandoffDraftId: request.LocalApprovedHandoffDraft!.DraftId,
            WorkspaceTestJailHandoffDraftId: request.WorkspaceTestJailHandoffDraft!.DraftId,
            WorkspaceId: ScopeId,
            DraftId: $"user-workspace-allowlisted-draft-{NormalizeId(request.ActionId!.Trim())}-{Prefix(contentHash)}",
            DraftRelativePath: relativePath,
            AllowedBoundary: AllowedRelativeOutputBoundary,
            WorkspaceClassification: WorkspaceClassification,
            CanonicalWorkspaceRootHash: HashText(canonicalRoot),
            CanonicalFinalPathHash: HashText(fullPath),
            ActionKind: ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftActionKind.LocalUserWorkspaceAllowlistedHandoffDraftCreateOnly.ToString(),
            Scope: ScopeId,
            CreateOnly: true,
            OverwriteAllowed: false,
            UserSelectedPathAllowed: false,
            PayloadControlledRootAllowed: false,
            CanonicalizationPassed: true,
            ReparseValidationPassed: true,
            UserWorkspaceAllowlistedBoundaryOnly: true,
            ShellAllowed: false,
            NetworkAllowed: false,
            ProductionAllowed: false,
            PublicProductAllowed: false,
            RedactionApplied: true,
            EvidenceRefs: evidenceRefs,
            ContentHash: contentHash,
            ContentHashPrefix: Prefix(contentHash),
            CreatedAt: createdAt,
            LocalOnly: true,
            InternalOnly: true,
            DevelopmentOnly: true,
            FailClosed: true,
            ProductCommandExecuted: false,
            PublicUiActionAvailable: false,
            ProductCommandHandlerAvailable: false,
            ProductiveServiceRegistrationAvailable: false,
            ProviderCloudNetworkAvailable: false,
            DbMigrationAvailable: false,
            KmsWormExternalTrustAvailable: false,
            BrowserCdpWcuOcrRecipesLiveAvailable: false,
            PilotRunAvailable: false,
            ReleaseCommercialReady: false,
            StatusText: CompletedStatus);

    private static ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftSnapshot SnapshotRejected(
        IReadOnlyList<ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftBlocker> blockers) =>
        ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftSnapshot.Pending with
        {
            Decision = ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftDecision.Rejected,
            State = blockers.Contains(ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftBlocker.ExistingDraftConflict)
                ? ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftState.DraftReplayBlocked
                : ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftState.DraftBlocked,
            Blockers = blockers.Distinct().OrderBy(blocker => blocker.ToString(), StringComparer.Ordinal).ToArray(),
            StatusText = RejectedStatus
        };

    private ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftSnapshot Remember(
        ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftSnapshot snapshot)
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

    private static string HashText(string material)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(material));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string Prefix(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "none" : value[..Math.Min(12, value.Length)];
}
