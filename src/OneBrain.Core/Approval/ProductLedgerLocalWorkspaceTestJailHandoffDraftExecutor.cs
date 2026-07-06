using System.Security.Cryptography;
using System.Text;

namespace OneBrain.Core.Approval;

public enum ProductLedgerLocalWorkspaceTestJailHandoffDraftDecision
{
    Rejected,
    DraftCreatedWorkspaceTestJailOnly,
    IdempotentReplay
}

public enum ProductLedgerLocalWorkspaceTestJailHandoffDraftState
{
    Pending,
    DraftCreatedWorkspaceTestJailOnly,
    DraftBlocked,
    DraftReplayBlocked
}

public enum ProductLedgerLocalWorkspaceTestJailHandoffDraftActionKind
{
    LocalWorkspaceTestJailHandoffDraftCreateOnly
}

public enum ProductLedgerLocalWorkspaceTestJailHandoffDraftBlocker
{
    MissingRequest,
    MissingExplicitWorkspaceTestJailScope,
    NonDevelopmentMode,
    NonLocalMode,
    NonInternalMode,
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
    MissingPredecessorDraft,
    PredecessorDraftNotCompleted,
    PredecessorDraftMismatch,
    MissingCandidateActionKind,
    CandidateActionMismatch,
    MissingCandidateEvidenceHash,
    CandidateEvidenceHashMismatch,
    MissingEvidenceReferences,
    MissingSafeDraftContent,
    RedactionRejected,
    WorkspaceTestJailBoundaryRejected,
    PathCanonicalizationFailed,
    ReparsePointRejected,
    FinalPathOutsideJail,
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

public sealed record ProductLedgerLocalWorkspaceTestJailHandoffDraftOptions(
    string WorkspaceTestJailRootPath,
    bool ExplicitWorkspaceTestJailBoundary,
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

public sealed record ProductLedgerLocalWorkspaceTestJailHandoffDraftRequest(
    bool ExplicitWorkspaceTestJailScope,
    bool DevelopmentMode,
    bool LocalMode,
    bool InternalMode,
    string? ActionId,
    string? CandidateId,
    ProductLedgerLocalWorkspaceTestJailHandoffDraftActionKind? ActionKind,
    ProductLedgerLocalApprovalDecisionSnapshot? ApprovalDecision,
    ProductLedgerLocalApprovedActionExecutionSnapshot? NoOpExecution,
    ProductLedgerLocalBoundedApprovedActionSnapshot? BoundedExecution,
    ProductLedgerLocalApprovedHandoffReportDraftSnapshot? PredecessorDraft,
    ProductLedgerInternalCommandKind? CandidateActionKind,
    string? CandidateEvidenceHash,
    string? CurrentEvidenceHash,
    string? PredecessorDraftContentHash,
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

public sealed record ProductLedgerLocalWorkspaceTestJailHandoffDraftSnapshot(
    ProductLedgerLocalWorkspaceTestJailHandoffDraftDecision Decision,
    ProductLedgerLocalWorkspaceTestJailHandoffDraftState State,
    IReadOnlyList<ProductLedgerLocalWorkspaceTestJailHandoffDraftBlocker> Blockers,
    string ActionId,
    string CandidateId,
    string ApprovalId,
    string NoOpExecutionId,
    string BoundedExecutionId,
    string PredecessorDraftId,
    string WorkspaceTestJailId,
    string DraftId,
    string DraftRelativePath,
    string WorkspaceTestJailBoundary,
    string CanonicalJailRootHash,
    string CanonicalFinalPathHash,
    string ActionKind,
    string Scope,
    bool CreateOnly,
    bool OverwriteAllowed,
    bool UserSelectedPathAllowed,
    bool PayloadControlledRootAllowed,
    bool CanonicalizationPassed,
    bool ReparseValidationPassed,
    bool WorkspaceTestJailOnly,
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
    public static ProductLedgerLocalWorkspaceTestJailHandoffDraftSnapshot Pending { get; } =
        new(
            Decision: ProductLedgerLocalWorkspaceTestJailHandoffDraftDecision.Rejected,
            State: ProductLedgerLocalWorkspaceTestJailHandoffDraftState.Pending,
            Blockers: [],
            ActionId: "workspace-test-jail-handoff-draft.pending",
            CandidateId: "candidate.pending",
            ApprovalId: "approval-state.pending-preview-only",
            NoOpExecutionId: "approval-execution.pending-no-op",
            BoundedExecutionId: "approval-bounded-action.pending",
            PredecessorDraftId: "draft.pending",
            WorkspaceTestJailId: ProductLedgerLocalWorkspaceTestJailHandoffDraftExecutor.ScopeId,
            DraftId: "workspace-draft.pending",
            DraftRelativePath: string.Empty,
            WorkspaceTestJailBoundary: ProductLedgerLocalWorkspaceTestJailHandoffDraftExecutor.AllowedRelativeOutputBoundary,
            CanonicalJailRootHash: "none",
            CanonicalFinalPathHash: "none",
            ActionKind: ProductLedgerLocalWorkspaceTestJailHandoffDraftActionKind.LocalWorkspaceTestJailHandoffDraftCreateOnly.ToString(),
            Scope: ProductLedgerLocalWorkspaceTestJailHandoffDraftExecutor.ScopeId,
            CreateOnly: true,
            OverwriteAllowed: false,
            UserSelectedPathAllowed: false,
            PayloadControlledRootAllowed: false,
            CanonicalizationPassed: false,
            ReparseValidationPassed: false,
            WorkspaceTestJailOnly: true,
            ShellAllowed: false,
            NetworkAllowed: false,
            ProductionAllowed: false,
            PublicProductAllowed: false,
            RedactionApplied: false,
            EvidenceRefs: ["product-ledger-workspace-test-jail-handoff-draft-pending"],
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
            StatusText: ProductLedgerLocalWorkspaceTestJailHandoffDraftExecutor.PendingStatus);
}

public sealed class ProductLedgerLocalWorkspaceTestJailHandoffDraftExecutor
{
    public const string AllowedRelativeOutputBoundary =
        ".nodal/product-ledger/handoff-drafts/";

    public const string ScopeId = "LocalInternalDevelopmentOnlyWorkspaceTestJail";

    public const string PendingStatus =
        "PRODUCT_LEDGER_WORKSPACE_TEST_JAIL_HANDOFF_DRAFT_PENDING LOCAL_ONLY INTERNAL_ONLY DEVELOPMENT_ONLY WORKSPACE_TEST_JAIL_ONLY CREATE_ONLY NO_OVERWRITE FAIL_CLOSED NO_ARBITRARY_PATH NO_USER_SELECTED_PATH NO_COMMAND_EXECUTION NO_SHELL_SUBPROCESS NO_PUBLIC_PRODUCT_PATH NO_PRODUCTION_ROUTE NO_PROVIDER_CLOUD_NETWORK NO_DB_MIGRATION NO_KMS_WORM_EXTERNAL_TRUST NO_LIVE_AUTOMATION NO_RELEASE_COMMERCIAL";

    public const string CompletedStatus =
        "PRODUCT_LEDGER_WORKSPACE_TEST_JAIL_HANDOFF_DRAFT_CREATED LOCAL_ONLY INTERNAL_ONLY DEVELOPMENT_ONLY WORKSPACE_TEST_JAIL_ONLY CREATE_ONLY NO_OVERWRITE FAIL_CLOSED REDACTION_APPLIED CANONICALIZED REPARSE_VALIDATED NO_ARBITRARY_PATH NO_USER_SELECTED_PATH NO_COMMAND_EXECUTION NO_SHELL_SUBPROCESS NO_PUBLIC_PRODUCT_PATH NO_PRODUCTION_ROUTE NO_PROVIDER_CLOUD_NETWORK NO_DB_MIGRATION NO_KMS_WORM_EXTERNAL_TRUST NO_LIVE_AUTOMATION NO_RELEASE_COMMERCIAL";

    public const string RejectedStatus =
        "PRODUCT_LEDGER_WORKSPACE_TEST_JAIL_HANDOFF_DRAFT_REJECTED FAIL_CLOSED WORKSPACE_TEST_JAIL_ONLY NO_OVERWRITE NO_ARBITRARY_PATH NO_USER_SELECTED_PATH NO_COMMAND_EXECUTION NO_SHELL_SUBPROCESS NO_PUBLIC_PRODUCT_PATH NO_PRODUCTION_ROUTE NO_PROVIDER_CLOUD_NETWORK NO_DB_MIGRATION NO_KMS_WORM_EXTERNAL_TRUST NO_LIVE_AUTOMATION NO_RELEASE_COMMERCIAL";

    private readonly ProductLedgerLocalWorkspaceTestJailHandoffDraftOptions options;
    private ProductLedgerLocalWorkspaceTestJailHandoffDraftSnapshot? lastSnapshot;

    public ProductLedgerLocalWorkspaceTestJailHandoffDraftExecutor(
        ProductLedgerLocalWorkspaceTestJailHandoffDraftOptions options)
    {
        this.options = options;
    }

    public ProductLedgerLocalWorkspaceTestJailHandoffDraftSnapshot CreateDraft(
        ProductLedgerLocalWorkspaceTestJailHandoffDraftRequest? request)
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
            return Remember(SnapshotRejected([ProductLedgerLocalWorkspaceTestJailHandoffDraftBlocker.WorkspaceTestJailBoundaryRejected]));
        }

        var relativePath = DraftRelativePath(request);
        var fullPath = FullDraftPath(canonicalRoot, relativePath);
        if (fullPath is null)
        {
            return Remember(SnapshotRejected([ProductLedgerLocalWorkspaceTestJailHandoffDraftBlocker.FinalPathOutsideJail]));
        }

        var evidenceRefs = EvidenceRefs(request);
        var content = DraftContent(request, relativePath, evidenceRefs, createdAt);
        var contentHash = HashText(content);

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
            if (ContainsReparsePoint(canonicalRoot) || ContainsReparsePoint(Path.GetDirectoryName(fullPath)!))
            {
                return Remember(SnapshotRejected([ProductLedgerLocalWorkspaceTestJailHandoffDraftBlocker.ReparsePointRejected]));
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
                        ProductLedgerLocalWorkspaceTestJailHandoffDraftDecision.IdempotentReplay));
                }

                return Remember(SnapshotRejected([ProductLedgerLocalWorkspaceTestJailHandoffDraftBlocker.ExistingDraftConflict]));
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
                ProductLedgerLocalWorkspaceTestJailHandoffDraftDecision.DraftCreatedWorkspaceTestJailOnly));
        }
        catch (IOException)
        {
            return Remember(SnapshotRejected([ProductLedgerLocalWorkspaceTestJailHandoffDraftBlocker.IoFailure]));
        }
        catch (UnauthorizedAccessException)
        {
            return Remember(SnapshotRejected([ProductLedgerLocalWorkspaceTestJailHandoffDraftBlocker.IoFailure]));
        }
        catch (ArgumentException)
        {
            return Remember(SnapshotRejected([ProductLedgerLocalWorkspaceTestJailHandoffDraftBlocker.PathCanonicalizationFailed]));
        }
    }

    public ProductLedgerLocalWorkspaceTestJailHandoffDraftSnapshot Read() =>
        lastSnapshot ?? ProductLedgerLocalWorkspaceTestJailHandoffDraftSnapshot.Pending;

    private IReadOnlyList<ProductLedgerLocalWorkspaceTestJailHandoffDraftBlocker> ValidateRequest(
        ProductLedgerLocalWorkspaceTestJailHandoffDraftRequest? request)
    {
        var blockers = new List<ProductLedgerLocalWorkspaceTestJailHandoffDraftBlocker>();
        if (request is null)
        {
            blockers.Add(ProductLedgerLocalWorkspaceTestJailHandoffDraftBlocker.MissingRequest);
            return blockers;
        }

        if (!BoundaryAllowed())
        {
            blockers.Add(ProductLedgerLocalWorkspaceTestJailHandoffDraftBlocker.WorkspaceTestJailBoundaryRejected);
        }

        if (!request.ExplicitWorkspaceTestJailScope)
        {
            blockers.Add(ProductLedgerLocalWorkspaceTestJailHandoffDraftBlocker.MissingExplicitWorkspaceTestJailScope);
        }

        if (!request.DevelopmentMode)
        {
            blockers.Add(ProductLedgerLocalWorkspaceTestJailHandoffDraftBlocker.NonDevelopmentMode);
        }

        if (!request.LocalMode)
        {
            blockers.Add(ProductLedgerLocalWorkspaceTestJailHandoffDraftBlocker.NonLocalMode);
        }

        if (!request.InternalMode)
        {
            blockers.Add(ProductLedgerLocalWorkspaceTestJailHandoffDraftBlocker.NonInternalMode);
        }

        if (string.IsNullOrWhiteSpace(request.ActionId))
        {
            blockers.Add(ProductLedgerLocalWorkspaceTestJailHandoffDraftBlocker.MissingActionId);
        }
        else if (!IsSafeId(request.ActionId))
        {
            blockers.Add(ProductLedgerLocalWorkspaceTestJailHandoffDraftBlocker.UnsafeActionId);
        }

        if (string.IsNullOrWhiteSpace(request.CandidateId))
        {
            blockers.Add(ProductLedgerLocalWorkspaceTestJailHandoffDraftBlocker.MissingCandidateId);
        }
        else if (!IsSafeId(request.CandidateId))
        {
            blockers.Add(ProductLedgerLocalWorkspaceTestJailHandoffDraftBlocker.UnsafeActionId);
        }

        if (request.ActionKind != ProductLedgerLocalWorkspaceTestJailHandoffDraftActionKind.LocalWorkspaceTestJailHandoffDraftCreateOnly)
        {
            blockers.Add(ProductLedgerLocalWorkspaceTestJailHandoffDraftBlocker.UnknownActionKind);
        }

        if (request.CandidateActionKind is null)
        {
            blockers.Add(ProductLedgerLocalWorkspaceTestJailHandoffDraftBlocker.MissingCandidateActionKind);
        }

        AddApprovalDecisionBlockers(request, blockers);
        AddNoOpBlockers(request, blockers);
        AddBoundedExecutionBlockers(request, blockers);
        AddPredecessorBlockers(request, blockers);
        AddEvidenceBlockers(request, blockers);
        AddPayloadBlockers(request, blockers);
        AddRedactionBlockers(request, blockers);
        return blockers.Distinct().OrderBy(blocker => blocker.ToString(), StringComparer.Ordinal).ToArray();
    }

    private static void AddApprovalDecisionBlockers(
        ProductLedgerLocalWorkspaceTestJailHandoffDraftRequest request,
        List<ProductLedgerLocalWorkspaceTestJailHandoffDraftBlocker> blockers)
    {
        var approval = request.ApprovalDecision;
        if (approval is null)
        {
            blockers.Add(ProductLedgerLocalWorkspaceTestJailHandoffDraftBlocker.MissingApprovalDecision);
            return;
        }

        if (approval.State != ProductLedgerLocalApprovalDecisionState.ApprovedLocalOnly
            || !string.Equals(approval.OperatorDecision, ProductLedgerLocalApprovalOperatorDecisionKind.Approve.ToString(), StringComparison.Ordinal))
        {
            blockers.Add(ProductLedgerLocalWorkspaceTestJailHandoffDraftBlocker.ApprovalDecisionNotApproved);
        }

        if (approval.Blockers.Count > 0
            || approval.Decision == ProductLedgerLocalApprovalDecisionStoreDecision.Rejected)
        {
            blockers.Add(ProductLedgerLocalWorkspaceTestJailHandoffDraftBlocker.ApprovalDecisionHasBlockers);
        }

        if (request.CandidateActionKind is not null
            && !string.Equals(approval.CandidateActionKind, request.CandidateActionKind.Value.ToString(), StringComparison.Ordinal))
        {
            blockers.Add(ProductLedgerLocalWorkspaceTestJailHandoffDraftBlocker.CandidateActionMismatch);
        }
    }

    private static void AddNoOpBlockers(
        ProductLedgerLocalWorkspaceTestJailHandoffDraftRequest request,
        List<ProductLedgerLocalWorkspaceTestJailHandoffDraftBlocker> blockers)
    {
        var noOp = request.NoOpExecution;
        if (noOp is null)
        {
            blockers.Add(ProductLedgerLocalWorkspaceTestJailHandoffDraftBlocker.MissingNoOpExecution);
            return;
        }

        if (noOp.State != ProductLedgerLocalApprovedActionExecutionState.NoOpExecutionCompletedLocalOnly
            || noOp.Blockers.Count > 0
            || noOp.ProductCommandExecuted
            || noOp.PublicUiActionAvailable
            || noOp.ProductCommandHandlerAvailable
            || noOp.FileWriteOutsideExecutionStorePerformed)
        {
            blockers.Add(ProductLedgerLocalWorkspaceTestJailHandoffDraftBlocker.NoOpExecutionNotCompleted);
        }

        if (request.CandidateActionKind is not null
            && !string.Equals(noOp.CandidateActionKind, request.CandidateActionKind.Value.ToString(), StringComparison.Ordinal))
        {
            blockers.Add(ProductLedgerLocalWorkspaceTestJailHandoffDraftBlocker.CandidateActionMismatch);
        }
    }

    private static void AddBoundedExecutionBlockers(
        ProductLedgerLocalWorkspaceTestJailHandoffDraftRequest request,
        List<ProductLedgerLocalWorkspaceTestJailHandoffDraftBlocker> blockers)
    {
        var bounded = request.BoundedExecution;
        if (bounded is null)
        {
            blockers.Add(ProductLedgerLocalWorkspaceTestJailHandoffDraftBlocker.MissingBoundedExecution);
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
            blockers.Add(ProductLedgerLocalWorkspaceTestJailHandoffDraftBlocker.BoundedExecutionNotCompleted);
        }

        if (request.CandidateActionKind is not null
            && !string.Equals(bounded.CandidateActionKind, request.CandidateActionKind.Value.ToString(), StringComparison.Ordinal))
        {
            blockers.Add(ProductLedgerLocalWorkspaceTestJailHandoffDraftBlocker.CandidateActionMismatch);
        }
    }

    private static void AddPredecessorBlockers(
        ProductLedgerLocalWorkspaceTestJailHandoffDraftRequest request,
        List<ProductLedgerLocalWorkspaceTestJailHandoffDraftBlocker> blockers)
    {
        var predecessor = request.PredecessorDraft;
        if (predecessor is null)
        {
            blockers.Add(ProductLedgerLocalWorkspaceTestJailHandoffDraftBlocker.MissingPredecessorDraft);
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
            blockers.Add(ProductLedgerLocalWorkspaceTestJailHandoffDraftBlocker.PredecessorDraftNotCompleted);
        }

        if (string.IsNullOrWhiteSpace(request.PredecessorDraftContentHash)
            || string.IsNullOrWhiteSpace(predecessor.ContentHash)
            || !string.Equals(request.PredecessorDraftContentHash.Trim(), predecessor.ContentHash, StringComparison.Ordinal))
        {
            blockers.Add(ProductLedgerLocalWorkspaceTestJailHandoffDraftBlocker.PredecessorDraftMismatch);
        }
    }

    private static void AddEvidenceBlockers(
        ProductLedgerLocalWorkspaceTestJailHandoffDraftRequest request,
        List<ProductLedgerLocalWorkspaceTestJailHandoffDraftBlocker> blockers)
    {
        if (string.IsNullOrWhiteSpace(request.CandidateEvidenceHash)
            || string.IsNullOrWhiteSpace(request.CurrentEvidenceHash)
            || string.IsNullOrWhiteSpace(request.ApprovalDecision?.CandidateEvidenceHash)
            || string.IsNullOrWhiteSpace(request.NoOpExecution?.CandidateEvidenceHash)
            || string.IsNullOrWhiteSpace(request.BoundedExecution?.CandidateEvidenceHash))
        {
            blockers.Add(ProductLedgerLocalWorkspaceTestJailHandoffDraftBlocker.MissingCandidateEvidenceHash);
        }
        else if (!string.Equals(request.CandidateEvidenceHash.Trim(), request.CurrentEvidenceHash.Trim(), StringComparison.Ordinal)
            || !string.Equals(request.CandidateEvidenceHash.Trim(), request.ApprovalDecision.CandidateEvidenceHash, StringComparison.Ordinal)
            || !string.Equals(request.CandidateEvidenceHash.Trim(), request.NoOpExecution.CandidateEvidenceHash, StringComparison.Ordinal)
            || !string.Equals(request.CandidateEvidenceHash.Trim(), request.BoundedExecution.CandidateEvidenceHash, StringComparison.Ordinal))
        {
            blockers.Add(ProductLedgerLocalWorkspaceTestJailHandoffDraftBlocker.CandidateEvidenceHashMismatch);
        }

        if (request.EvidenceReferences is null
            || request.EvidenceReferences.Count == 0
            || request.EvidenceReferences.Any(string.IsNullOrWhiteSpace)
            || request.ApprovalDecision?.EvidenceReferences.Count == 0
            || request.NoOpExecution?.EvidenceReferences.Count == 0
            || request.BoundedExecution?.EvidenceReferences.Count == 0
            || request.PredecessorDraft?.EvidenceRefs.Count == 0)
        {
            blockers.Add(ProductLedgerLocalWorkspaceTestJailHandoffDraftBlocker.MissingEvidenceReferences);
        }

        if (string.IsNullOrWhiteSpace(request.DraftTitle)
            || string.IsNullOrWhiteSpace(request.RedactedDraftSummary))
        {
            blockers.Add(ProductLedgerLocalWorkspaceTestJailHandoffDraftBlocker.MissingSafeDraftContent);
        }
    }

    private static void AddPayloadBlockers(
        ProductLedgerLocalWorkspaceTestJailHandoffDraftRequest request,
        List<ProductLedgerLocalWorkspaceTestJailHandoffDraftBlocker> blockers)
    {
        if (ContainsPayload(request.ProposedPath) || ContainsPayload(request.ProposedRoot) || ContainsPayload(request.ProposedFilename))
        {
            blockers.Add(ProductLedgerLocalWorkspaceTestJailHandoffDraftBlocker.PayloadPathFieldRejected);
        }

        if (ContainsPayload(request.ProposedCommand))
        {
            blockers.Add(ProductLedgerLocalWorkspaceTestJailHandoffDraftBlocker.PayloadCommandFieldRejected);
        }

        if (ContainsPayload(request.ProposedUrl)
            || ContainsPayload(request.ProposedProvider)
            || ContainsPayload(request.ProposedDbMigration))
        {
            blockers.Add(ProductLedgerLocalWorkspaceTestJailHandoffDraftBlocker.PayloadNetworkProviderFieldRejected);
        }

        if (request.ClaimsArbitraryPathInput)
        {
            blockers.Add(ProductLedgerLocalWorkspaceTestJailHandoffDraftBlocker.ClaimsArbitraryPathInput);
        }

        if (request.ClaimsFilesystemScan)
        {
            blockers.Add(ProductLedgerLocalWorkspaceTestJailHandoffDraftBlocker.ClaimsFilesystemScan);
        }

        if (request.RequestsOverwrite)
        {
            blockers.Add(ProductLedgerLocalWorkspaceTestJailHandoffDraftBlocker.RequestsOverwrite);
        }

        if (request.RequestsUserSelectedPath)
        {
            blockers.Add(ProductLedgerLocalWorkspaceTestJailHandoffDraftBlocker.RequestsUserSelectedPath);
        }

        if (request.RequestsPublicUiAction)
        {
            blockers.Add(ProductLedgerLocalWorkspaceTestJailHandoffDraftBlocker.RequestsPublicUiAction);
        }

        if (request.RequestsProductCommandExecution)
        {
            blockers.Add(ProductLedgerLocalWorkspaceTestJailHandoffDraftBlocker.RequestsProductCommandExecution);
        }

        if (request.RequestsProductCommandHandler)
        {
            blockers.Add(ProductLedgerLocalWorkspaceTestJailHandoffDraftBlocker.RequestsProductCommandHandler);
        }

        if (request.RequestsProductiveServiceRegistration)
        {
            blockers.Add(ProductLedgerLocalWorkspaceTestJailHandoffDraftBlocker.RequestsProductiveServiceRegistration);
        }

        if (request.RequestsShellOrSubprocess)
        {
            blockers.Add(ProductLedgerLocalWorkspaceTestJailHandoffDraftBlocker.RequestsShellOrSubprocess);
        }

        if (request.ClaimsArbitraryCommandExecution)
        {
            blockers.Add(ProductLedgerLocalWorkspaceTestJailHandoffDraftBlocker.ClaimsArbitraryCommandExecution);
        }

        if (request.ClaimsProviderCloudNetwork)
        {
            blockers.Add(ProductLedgerLocalWorkspaceTestJailHandoffDraftBlocker.ClaimsProviderCloudNetwork);
        }

        if (request.ClaimsDbMigration)
        {
            blockers.Add(ProductLedgerLocalWorkspaceTestJailHandoffDraftBlocker.ClaimsDbMigration);
        }

        if (request.ClaimsKmsWormExternalTrust)
        {
            blockers.Add(ProductLedgerLocalWorkspaceTestJailHandoffDraftBlocker.ClaimsKmsWormExternalTrust);
        }

        if (request.ClaimsBrowserCdpWcuOcrRecipesLive)
        {
            blockers.Add(ProductLedgerLocalWorkspaceTestJailHandoffDraftBlocker.ClaimsBrowserCdpWcuOcrRecipesLive);
        }

        if (request.ClaimsPilotRun)
        {
            blockers.Add(ProductLedgerLocalWorkspaceTestJailHandoffDraftBlocker.ClaimsPilotRun);
        }

        if (request.ClaimsReleaseCommercial)
        {
            blockers.Add(ProductLedgerLocalWorkspaceTestJailHandoffDraftBlocker.ClaimsReleaseCommercial);
        }
    }

    private static void AddRedactionBlockers(
        ProductLedgerLocalWorkspaceTestJailHandoffDraftRequest request,
        List<ProductLedgerLocalWorkspaceTestJailHandoffDraftBlocker> blockers)
    {
        var redaction = new RedactionBeforePersistenceService().Evaluate(
            RedactionBeforePersistencePolicy.TestOnly,
            new DurableAuditTrailAppendOnlyMinimalRequest(
                EventKind: "ProductLedgerLocalWorkspaceTestJailHandoffDraft",
                ActorReference: "local-internal-operator",
                ApprovalReference: request.ApprovalDecision?.ApprovalId ?? "approval-missing",
                EvidenceReferences: request.EvidenceReferences ?? [],
                Metadata: new Dictionary<string, string>
                {
                    ["actionId"] = request.ActionId ?? string.Empty,
                    ["actionKind"] = request.ActionKind?.ToString() ?? string.Empty,
                    ["candidateId"] = request.CandidateId ?? string.Empty,
                    ["candidateEvidenceHashPrefix"] = Prefix(request.CandidateEvidenceHash),
                    ["predecessorDraftId"] = request.PredecessorDraft?.DraftId ?? string.Empty,
                    ["draftTitle"] = request.DraftTitle ?? string.Empty,
                    ["redactedDraftSummary"] = request.RedactedDraftSummary ?? string.Empty,
                    ["workspaceBoundary"] = AllowedRelativeOutputBoundary
                },
                RawPayload: null));

        if (!redaction.Succeeded || redaction.Decision != RedactionBeforePersistenceDecision.Allowed)
        {
            blockers.Add(ProductLedgerLocalWorkspaceTestJailHandoffDraftBlocker.RedactionRejected);
        }
    }

    private bool BoundaryAllowed()
    {
        if (!options.ExplicitWorkspaceTestJailBoundary
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
            || string.IsNullOrWhiteSpace(options.WorkspaceTestJailRootPath))
        {
            return false;
        }

        var full = CanonicalRoot();
        if (full is null)
        {
            return false;
        }

        var normalized = full.Replace('\\', '/');
        return (normalized.Contains("/docs/test-output/product-ledger/workspace-test-jail", StringComparison.OrdinalIgnoreCase)
                || normalized.Contains("/.tmp-product-ledger-workspace-test-jail-tests/", StringComparison.OrdinalIgnoreCase))
            && full.IndexOf("..", StringComparison.Ordinal) < 0
            && !Path.GetPathRoot(full)!.Equals(full, StringComparison.OrdinalIgnoreCase);
    }

    private string? CanonicalRoot()
    {
        try
        {
            return Path.TrimEndingDirectorySeparator(Path.GetFullPath(options.WorkspaceTestJailRootPath));
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
            var fullPath = Path.GetFullPath(Path.Combine(canonicalRoot, relativePath.Replace('/', Path.DirectorySeparatorChar)));
            return StartsWithPath(canonicalRoot, fullPath) && fullPath.EndsWith(".md", StringComparison.OrdinalIgnoreCase)
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
            return (File.GetAttributes(path) & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint;
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

    private static string DraftRelativePath(ProductLedgerLocalWorkspaceTestJailHandoffDraftRequest request) =>
        AllowedRelativeOutputBoundary
        + $"workspace-test-jail-handoff-draft-{NormalizeId(request.ActionId!)}-{Prefix(request.CandidateEvidenceHash)}.md";

    private static string DraftContent(
        ProductLedgerLocalWorkspaceTestJailHandoffDraftRequest request,
        string relativePath,
        IReadOnlyList<string> evidenceRefs,
        DateTimeOffset createdAt)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# Local Workspace Test-Jail Handoff Draft");
        builder.AppendLine();
        builder.AppendLine($"Action id: `{request.ActionId!.Trim()}`");
        builder.AppendLine($"Candidate id: `{request.CandidateId!.Trim()}`");
        builder.AppendLine($"Approval decision id: `{request.ApprovalDecision!.ApprovalId}`");
        builder.AppendLine($"No-op execution id: `{request.NoOpExecution!.ExecutionId}`");
        builder.AppendLine($"Bounded execution id: `{request.BoundedExecution!.ExecutionId}`");
        builder.AppendLine($"Predecessor draft id: `{request.PredecessorDraft!.DraftId}`");
        builder.AppendLine($"Created at UTC: `{createdAt:O}`");
        builder.AppendLine($"Draft relative path: `{relativePath}`");
        builder.AppendLine("Classification: local/internal/development-only workspace test-jail only.");
        builder.AppendLine("Boundary: create-only, no-overwrite, no arbitrary path, no user-selected path.");
        builder.AppendLine();
        builder.AppendLine("## Redacted Summary");
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
        builder.AppendLine("- Workspace test-jail only.");
        builder.AppendLine("- No arbitrary workspace write.");
        builder.AppendLine("- No user-selected path.");
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
        builder.AppendLine("Safe next step: read-only audit of this workspace test-jail draft action.");
        return builder.ToString();
    }

    private static IReadOnlyList<string> EvidenceRefs(
        ProductLedgerLocalWorkspaceTestJailHandoffDraftRequest request) =>
        (request.EvidenceReferences ?? [])
        .Concat(request.ApprovalDecision?.EvidenceReferences ?? [])
        .Concat(request.NoOpExecution?.EvidenceReferences ?? [])
        .Concat(request.BoundedExecution?.EvidenceReferences ?? [])
        .Concat(request.PredecessorDraft?.EvidenceRefs ?? [])
        .Concat(["product-ledger-workspace-test-jail-handoff-draft-created"])
        .Select(evidence => evidence.Trim())
        .Where(evidence => !string.IsNullOrWhiteSpace(evidence))
        .Distinct(StringComparer.Ordinal)
        .OrderBy(evidence => evidence, StringComparer.Ordinal)
        .ToArray();

    private static ProductLedgerLocalWorkspaceTestJailHandoffDraftSnapshot SnapshotFrom(
        ProductLedgerLocalWorkspaceTestJailHandoffDraftRequest request,
        string relativePath,
        string canonicalRoot,
        string fullPath,
        IReadOnlyList<string> evidenceRefs,
        string contentHash,
        DateTimeOffset createdAt,
        ProductLedgerLocalWorkspaceTestJailHandoffDraftDecision decision) =>
        new(
            Decision: decision,
            State: ProductLedgerLocalWorkspaceTestJailHandoffDraftState.DraftCreatedWorkspaceTestJailOnly,
            Blockers: [],
            ActionId: request.ActionId!.Trim(),
            CandidateId: request.CandidateId!.Trim(),
            ApprovalId: request.ApprovalDecision!.ApprovalId,
            NoOpExecutionId: request.NoOpExecution!.ExecutionId,
            BoundedExecutionId: request.BoundedExecution!.ExecutionId,
            PredecessorDraftId: request.PredecessorDraft!.DraftId,
            WorkspaceTestJailId: ScopeId,
            DraftId: $"workspace-test-jail-draft-{NormalizeId(request.ActionId!.Trim())}-{Prefix(contentHash)}",
            DraftRelativePath: relativePath,
            WorkspaceTestJailBoundary: AllowedRelativeOutputBoundary,
            CanonicalJailRootHash: HashText(canonicalRoot),
            CanonicalFinalPathHash: HashText(fullPath),
            ActionKind: ProductLedgerLocalWorkspaceTestJailHandoffDraftActionKind.LocalWorkspaceTestJailHandoffDraftCreateOnly.ToString(),
            Scope: ScopeId,
            CreateOnly: true,
            OverwriteAllowed: false,
            UserSelectedPathAllowed: false,
            PayloadControlledRootAllowed: false,
            CanonicalizationPassed: true,
            ReparseValidationPassed: true,
            WorkspaceTestJailOnly: true,
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

    private static ProductLedgerLocalWorkspaceTestJailHandoffDraftSnapshot SnapshotRejected(
        IReadOnlyList<ProductLedgerLocalWorkspaceTestJailHandoffDraftBlocker> blockers) =>
        ProductLedgerLocalWorkspaceTestJailHandoffDraftSnapshot.Pending with
        {
            Decision = ProductLedgerLocalWorkspaceTestJailHandoffDraftDecision.Rejected,
            State = blockers.Contains(ProductLedgerLocalWorkspaceTestJailHandoffDraftBlocker.ExistingDraftConflict)
                ? ProductLedgerLocalWorkspaceTestJailHandoffDraftState.DraftReplayBlocked
                : ProductLedgerLocalWorkspaceTestJailHandoffDraftState.DraftBlocked,
            Blockers = blockers.Distinct().OrderBy(blocker => blocker.ToString(), StringComparer.Ordinal).ToArray(),
            StatusText = RejectedStatus
        };

    private ProductLedgerLocalWorkspaceTestJailHandoffDraftSnapshot Remember(
        ProductLedgerLocalWorkspaceTestJailHandoffDraftSnapshot snapshot)
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
