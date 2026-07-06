using System.Security.Cryptography;
using System.Text;

namespace OneBrain.Core.Approval;

public enum ProductLedgerLocalApprovedHandoffReportDraftDecision
{
    Rejected,
    DraftCreatedLocalOnly,
    IdempotentReplay
}

public enum ProductLedgerLocalApprovedHandoffReportDraftState
{
    Pending,
    DraftCreatedLocalOnly,
    DraftBlocked,
    DraftReplayBlocked
}

public enum ProductLedgerLocalApprovedHandoffReportDraftActionKind
{
    LocalApprovedHandoffReportDraft
}

public enum ProductLedgerLocalApprovedHandoffReportDraftBlocker
{
    MissingRequest,
    MissingExplicitLocalApprovedHandoffDraftScope,
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
    ApprovalDecisionMismatch,
    MissingNoOpExecution,
    NoOpExecutionNotCompleted,
    NoOpExecutionMismatch,
    MissingBoundedExecution,
    BoundedExecutionNotCompleted,
    BoundedExecutionMismatch,
    MissingCandidateActionKind,
    CandidateActionMismatch,
    MissingCandidateEvidenceHash,
    CandidateEvidenceHashMismatch,
    MissingEvidenceReferences,
    MissingSafeDraftContent,
    RedactionRejected,
    OutputBoundaryRejected,
    ExistingDraftConflict,
    IoFailure,
    PayloadPathFieldRejected,
    PayloadCommandFieldRejected,
    PayloadNetworkProviderFieldRejected,
    ClaimsArbitraryPathInput,
    ClaimsFilesystemScan,
    RequestsOverwrite,
    RequestsUserFileWrite,
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

public sealed record ProductLedgerLocalApprovedHandoffReportDraftScope(
    string ScopeId,
    bool LocalOnly,
    bool InternalOnly,
    bool DevelopmentOnly,
    bool CreateOnly,
    bool OverwriteAllowed,
    bool UserFileWrite,
    bool ShellAllowed,
    bool NetworkAllowed,
    bool ProductionAllowed,
    bool PublicProductAllowed);

public sealed record ProductLedgerLocalApprovedHandoffReportDraftOptions(
    string OutputRootPath,
    bool ExplicitLocalApprovedHandoffDraftBoundary,
    bool AllowsArbitraryPathInput,
    bool AllowsFilesystemScan,
    bool AllowsOverwrite,
    bool AllowsUserFileWrite,
    bool AllowsShellOrSubprocess,
    bool AllowsCommandExecution,
    bool AllowsNetwork,
    bool AllowsDb,
    bool AllowsKmsWormExternalTrust,
    bool AllowsReleaseCommercial);

public sealed record ProductLedgerLocalApprovedHandoffReportDraftRequest(
    bool ExplicitLocalApprovedHandoffDraftScope,
    bool DevelopmentMode,
    bool LocalMode,
    bool InternalMode,
    string? ActionId,
    string? CandidateId,
    ProductLedgerLocalApprovedHandoffReportDraftActionKind? ActionKind,
    ProductLedgerLocalApprovalDecisionSnapshot? ApprovalDecision,
    ProductLedgerLocalApprovedActionExecutionSnapshot? NoOpExecution,
    ProductLedgerLocalBoundedApprovedActionSnapshot? BoundedExecution,
    ProductLedgerInternalCommandKind? CandidateActionKind,
    string? CandidateEvidenceHash,
    string? CurrentEvidenceHash,
    string? DraftTitle,
    string? RedactedDraftSummary,
    IReadOnlyList<string>? EvidenceReferences,
    string? ProposedPath,
    string? ProposedCommand,
    string? ProposedUrl,
    string? ProposedProvider,
    string? ProposedDbMigration,
    bool ClaimsArbitraryPathInput,
    bool ClaimsFilesystemScan,
    bool RequestsOverwrite,
    bool RequestsUserFileWrite,
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

public sealed record ProductLedgerLocalApprovedHandoffReportDraftSnapshot(
    ProductLedgerLocalApprovedHandoffReportDraftDecision Decision,
    ProductLedgerLocalApprovedHandoffReportDraftState State,
    IReadOnlyList<ProductLedgerLocalApprovedHandoffReportDraftBlocker> Blockers,
    string ActionId,
    string CandidateId,
    string ApprovalId,
    string NoOpExecutionId,
    string BoundedExecutionId,
    string DraftId,
    string DraftRelativePath,
    string OutputBoundary,
    string ActionKind,
    string Scope,
    bool CreateOnly,
    bool OverwriteAllowed,
    bool UserFileWrite,
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
    public static ProductLedgerLocalApprovedHandoffReportDraftSnapshot Pending { get; } =
        new(
            Decision: ProductLedgerLocalApprovedHandoffReportDraftDecision.Rejected,
            State: ProductLedgerLocalApprovedHandoffReportDraftState.Pending,
            Blockers: [],
            ActionId: "local-approved-handoff-draft.pending",
            CandidateId: "candidate.pending",
            ApprovalId: "approval-state.pending-preview-only",
            NoOpExecutionId: "approval-execution.pending-no-op",
            BoundedExecutionId: "approval-bounded-action.pending",
            DraftId: "draft.pending",
            DraftRelativePath: string.Empty,
            OutputBoundary: ProductLedgerLocalApprovedHandoffReportDraftExecutor.AllowedRelativeOutputBoundary,
            ActionKind: ProductLedgerLocalApprovedHandoffReportDraftActionKind.LocalApprovedHandoffReportDraft.ToString(),
            Scope: ProductLedgerLocalApprovedHandoffReportDraftExecutor.ScopeId,
            CreateOnly: true,
            OverwriteAllowed: false,
            UserFileWrite: false,
            ShellAllowed: false,
            NetworkAllowed: false,
            ProductionAllowed: false,
            PublicProductAllowed: false,
            RedactionApplied: false,
            EvidenceRefs: ["product-ledger-local-approved-handoff-report-draft-pending"],
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
            StatusText: ProductLedgerLocalApprovedHandoffReportDraftExecutor.PendingStatus);
}

public sealed class ProductLedgerLocalApprovedHandoffReportDraftExecutor
{
    public const string AllowedRelativeOutputBoundary =
        "docs/test-output/product-ledger/approved-local-handoff-drafts/";

    public const string ScopeId = "LocalInternalDevelopmentOnly";

    public const string PendingStatus =
        "PRODUCT_LEDGER_LOCAL_APPROVED_HANDOFF_REPORT_DRAFT_PENDING LOCAL_ONLY INTERNAL_ONLY DEVELOPMENT_ONLY CREATE_ONLY NO_OVERWRITE FAIL_CLOSED NO_ARBITRARY_PATH NO_USER_FILE_WRITE NO_COMMAND_EXECUTION NO_SHELL_SUBPROCESS NO_PUBLIC_PRODUCT_PATH NO_PRODUCTION_ROUTE NO_PROVIDER_CLOUD_NETWORK NO_DB_MIGRATION NO_KMS_WORM_EXTERNAL_TRUST NO_LIVE_AUTOMATION NO_RELEASE_COMMERCIAL";

    public const string CompletedStatus =
        "PRODUCT_LEDGER_LOCAL_APPROVED_HANDOFF_REPORT_DRAFT_CREATED LOCAL_ONLY INTERNAL_ONLY DEVELOPMENT_ONLY CREATE_ONLY NO_OVERWRITE FAIL_CLOSED REDACTION_APPLIED NO_ARBITRARY_PATH NO_USER_FILE_WRITE NO_COMMAND_EXECUTION NO_SHELL_SUBPROCESS NO_PUBLIC_PRODUCT_PATH NO_PRODUCTION_ROUTE NO_PROVIDER_CLOUD_NETWORK NO_DB_MIGRATION NO_KMS_WORM_EXTERNAL_TRUST NO_LIVE_AUTOMATION NO_RELEASE_COMMERCIAL";

    public const string RejectedStatus =
        "PRODUCT_LEDGER_LOCAL_APPROVED_HANDOFF_REPORT_DRAFT_REJECTED FAIL_CLOSED NO_OVERWRITE NO_ARBITRARY_PATH NO_USER_FILE_WRITE NO_COMMAND_EXECUTION NO_SHELL_SUBPROCESS NO_PUBLIC_PRODUCT_PATH NO_PRODUCTION_ROUTE NO_PROVIDER_CLOUD_NETWORK NO_DB_MIGRATION NO_KMS_WORM_EXTERNAL_TRUST NO_LIVE_AUTOMATION NO_RELEASE_COMMERCIAL";

    private readonly ProductLedgerLocalApprovedHandoffReportDraftOptions options;
    private ProductLedgerLocalApprovedHandoffReportDraftSnapshot? lastSnapshot;

    public ProductLedgerLocalApprovedHandoffReportDraftExecutor(
        ProductLedgerLocalApprovedHandoffReportDraftOptions options)
    {
        this.options = options;
    }

    public ProductLedgerLocalApprovedHandoffReportDraftSnapshot CreateDraft(
        ProductLedgerLocalApprovedHandoffReportDraftRequest? request)
    {
        var blockers = ValidateRequest(request);
        if (blockers.Count > 0 || request is null)
        {
            return Remember(SnapshotRejected(blockers));
        }

        var createdAt = DateTimeOffset.UnixEpoch;
        var relativePath = DraftRelativePath(request);
        var fullPath = FullDraftPath(relativePath);
        var evidenceRefs = EvidenceRefs(request);
        var content = DraftContent(request, relativePath, evidenceRefs, createdAt);
        var contentHash = HashText(content);

        try
        {
            if (File.Exists(fullPath))
            {
                var existing = File.ReadAllText(fullPath, Encoding.UTF8);
                if (string.Equals(HashText(existing), contentHash, StringComparison.Ordinal))
                {
                    return Remember(SnapshotFrom(
                        request,
                        relativePath,
                        evidenceRefs,
                        contentHash,
                        createdAt,
                        ProductLedgerLocalApprovedHandoffReportDraftDecision.IdempotentReplay));
                }

                return Remember(SnapshotRejected([ProductLedgerLocalApprovedHandoffReportDraftBlocker.ExistingDraftConflict]));
            }

            Directory.CreateDirectory(Path.GetFullPath(options.OutputRootPath));
            using (var stream = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            using (var writer = new StreamWriter(stream, Encoding.UTF8))
            {
                writer.Write(content);
            }

            return Remember(SnapshotFrom(
                request,
                relativePath,
                evidenceRefs,
                contentHash,
                createdAt,
                ProductLedgerLocalApprovedHandoffReportDraftDecision.DraftCreatedLocalOnly));
        }
        catch (IOException)
        {
            return Remember(SnapshotRejected([ProductLedgerLocalApprovedHandoffReportDraftBlocker.IoFailure]));
        }
        catch (UnauthorizedAccessException)
        {
            return Remember(SnapshotRejected([ProductLedgerLocalApprovedHandoffReportDraftBlocker.IoFailure]));
        }
    }

    public ProductLedgerLocalApprovedHandoffReportDraftSnapshot Read() =>
        lastSnapshot ?? ProductLedgerLocalApprovedHandoffReportDraftSnapshot.Pending;

    private IReadOnlyList<ProductLedgerLocalApprovedHandoffReportDraftBlocker> ValidateRequest(
        ProductLedgerLocalApprovedHandoffReportDraftRequest? request)
    {
        var blockers = new List<ProductLedgerLocalApprovedHandoffReportDraftBlocker>();
        if (request is null)
        {
            blockers.Add(ProductLedgerLocalApprovedHandoffReportDraftBlocker.MissingRequest);
            return blockers;
        }

        if (!BoundaryAllowed())
        {
            blockers.Add(ProductLedgerLocalApprovedHandoffReportDraftBlocker.OutputBoundaryRejected);
        }

        if (!request.ExplicitLocalApprovedHandoffDraftScope)
        {
            blockers.Add(ProductLedgerLocalApprovedHandoffReportDraftBlocker.MissingExplicitLocalApprovedHandoffDraftScope);
        }

        if (!request.DevelopmentMode)
        {
            blockers.Add(ProductLedgerLocalApprovedHandoffReportDraftBlocker.NonDevelopmentMode);
        }

        if (!request.LocalMode)
        {
            blockers.Add(ProductLedgerLocalApprovedHandoffReportDraftBlocker.NonLocalMode);
        }

        if (!request.InternalMode)
        {
            blockers.Add(ProductLedgerLocalApprovedHandoffReportDraftBlocker.NonInternalMode);
        }

        if (string.IsNullOrWhiteSpace(request.ActionId))
        {
            blockers.Add(ProductLedgerLocalApprovedHandoffReportDraftBlocker.MissingActionId);
        }
        else if (!IsSafeId(request.ActionId))
        {
            blockers.Add(ProductLedgerLocalApprovedHandoffReportDraftBlocker.UnsafeActionId);
        }

        if (string.IsNullOrWhiteSpace(request.CandidateId))
        {
            blockers.Add(ProductLedgerLocalApprovedHandoffReportDraftBlocker.MissingCandidateId);
        }
        else if (!IsSafeId(request.CandidateId))
        {
            blockers.Add(ProductLedgerLocalApprovedHandoffReportDraftBlocker.UnsafeActionId);
        }

        if (request.ActionKind != ProductLedgerLocalApprovedHandoffReportDraftActionKind.LocalApprovedHandoffReportDraft)
        {
            blockers.Add(ProductLedgerLocalApprovedHandoffReportDraftBlocker.UnknownActionKind);
        }

        if (request.CandidateActionKind is null)
        {
            blockers.Add(ProductLedgerLocalApprovedHandoffReportDraftBlocker.MissingCandidateActionKind);
        }

        AddApprovalDecisionBlockers(request, blockers);
        AddNoOpBlockers(request, blockers);
        AddBoundedExecutionBlockers(request, blockers);
        AddEvidenceBlockers(request, blockers);
        AddPayloadBlockers(request, blockers);
        AddRedactionBlockers(request, blockers);
        return blockers.Distinct().OrderBy(blocker => blocker.ToString(), StringComparer.Ordinal).ToArray();
    }

    private static void AddApprovalDecisionBlockers(
        ProductLedgerLocalApprovedHandoffReportDraftRequest request,
        List<ProductLedgerLocalApprovedHandoffReportDraftBlocker> blockers)
    {
        var approval = request.ApprovalDecision;
        if (approval is null)
        {
            blockers.Add(ProductLedgerLocalApprovedHandoffReportDraftBlocker.MissingApprovalDecision);
            return;
        }

        if (approval.State != ProductLedgerLocalApprovalDecisionState.ApprovedLocalOnly
            || !string.Equals(approval.OperatorDecision, ProductLedgerLocalApprovalOperatorDecisionKind.Approve.ToString(), StringComparison.Ordinal))
        {
            blockers.Add(ProductLedgerLocalApprovedHandoffReportDraftBlocker.ApprovalDecisionNotApproved);
        }

        if (approval.Blockers.Count > 0
            || approval.Decision == ProductLedgerLocalApprovalDecisionStoreDecision.Rejected)
        {
            blockers.Add(ProductLedgerLocalApprovedHandoffReportDraftBlocker.ApprovalDecisionHasBlockers);
        }

        if (request.CandidateActionKind is not null
            && !string.Equals(approval.CandidateActionKind, request.CandidateActionKind.Value.ToString(), StringComparison.Ordinal))
        {
            blockers.Add(ProductLedgerLocalApprovedHandoffReportDraftBlocker.CandidateActionMismatch);
        }
    }

    private static void AddNoOpBlockers(
        ProductLedgerLocalApprovedHandoffReportDraftRequest request,
        List<ProductLedgerLocalApprovedHandoffReportDraftBlocker> blockers)
    {
        var noOp = request.NoOpExecution;
        if (noOp is null)
        {
            blockers.Add(ProductLedgerLocalApprovedHandoffReportDraftBlocker.MissingNoOpExecution);
            return;
        }

        if (noOp.State != ProductLedgerLocalApprovedActionExecutionState.NoOpExecutionCompletedLocalOnly
            || noOp.Blockers.Count > 0
            || noOp.ProductCommandExecuted
            || noOp.PublicUiActionAvailable
            || noOp.ProductCommandHandlerAvailable
            || noOp.FileWriteOutsideExecutionStorePerformed)
        {
            blockers.Add(ProductLedgerLocalApprovedHandoffReportDraftBlocker.NoOpExecutionNotCompleted);
        }

        if (request.CandidateActionKind is not null
            && !string.Equals(noOp.CandidateActionKind, request.CandidateActionKind.Value.ToString(), StringComparison.Ordinal))
        {
            blockers.Add(ProductLedgerLocalApprovedHandoffReportDraftBlocker.CandidateActionMismatch);
        }
    }

    private static void AddBoundedExecutionBlockers(
        ProductLedgerLocalApprovedHandoffReportDraftRequest request,
        List<ProductLedgerLocalApprovedHandoffReportDraftBlocker> blockers)
    {
        var bounded = request.BoundedExecution;
        if (bounded is null)
        {
            blockers.Add(ProductLedgerLocalApprovedHandoffReportDraftBlocker.MissingBoundedExecution);
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
            blockers.Add(ProductLedgerLocalApprovedHandoffReportDraftBlocker.BoundedExecutionNotCompleted);
        }

        if (request.CandidateActionKind is not null
            && !string.Equals(bounded.CandidateActionKind, request.CandidateActionKind.Value.ToString(), StringComparison.Ordinal))
        {
            blockers.Add(ProductLedgerLocalApprovedHandoffReportDraftBlocker.CandidateActionMismatch);
        }
    }

    private static void AddEvidenceBlockers(
        ProductLedgerLocalApprovedHandoffReportDraftRequest request,
        List<ProductLedgerLocalApprovedHandoffReportDraftBlocker> blockers)
    {
        if (string.IsNullOrWhiteSpace(request.CandidateEvidenceHash)
            || string.IsNullOrWhiteSpace(request.CurrentEvidenceHash)
            || string.IsNullOrWhiteSpace(request.ApprovalDecision?.CandidateEvidenceHash)
            || string.IsNullOrWhiteSpace(request.NoOpExecution?.CandidateEvidenceHash)
            || string.IsNullOrWhiteSpace(request.BoundedExecution?.CandidateEvidenceHash))
        {
            blockers.Add(ProductLedgerLocalApprovedHandoffReportDraftBlocker.MissingCandidateEvidenceHash);
        }
        else if (!string.Equals(request.CandidateEvidenceHash.Trim(), request.CurrentEvidenceHash.Trim(), StringComparison.Ordinal)
            || !string.Equals(request.CandidateEvidenceHash.Trim(), request.ApprovalDecision.CandidateEvidenceHash, StringComparison.Ordinal)
            || !string.Equals(request.CandidateEvidenceHash.Trim(), request.NoOpExecution.CandidateEvidenceHash, StringComparison.Ordinal)
            || !string.Equals(request.CandidateEvidenceHash.Trim(), request.BoundedExecution.CandidateEvidenceHash, StringComparison.Ordinal))
        {
            blockers.Add(ProductLedgerLocalApprovedHandoffReportDraftBlocker.CandidateEvidenceHashMismatch);
        }

        if (request.EvidenceReferences is null
            || request.EvidenceReferences.Count == 0
            || request.EvidenceReferences.Any(string.IsNullOrWhiteSpace)
            || request.ApprovalDecision?.EvidenceReferences.Count == 0
            || request.NoOpExecution?.EvidenceReferences.Count == 0
            || request.BoundedExecution?.EvidenceReferences.Count == 0)
        {
            blockers.Add(ProductLedgerLocalApprovedHandoffReportDraftBlocker.MissingEvidenceReferences);
        }

        if (string.IsNullOrWhiteSpace(request.DraftTitle)
            || string.IsNullOrWhiteSpace(request.RedactedDraftSummary))
        {
            blockers.Add(ProductLedgerLocalApprovedHandoffReportDraftBlocker.MissingSafeDraftContent);
        }
    }

    private static void AddPayloadBlockers(
        ProductLedgerLocalApprovedHandoffReportDraftRequest request,
        List<ProductLedgerLocalApprovedHandoffReportDraftBlocker> blockers)
    {
        if (ContainsPayload(request.ProposedPath))
        {
            blockers.Add(ProductLedgerLocalApprovedHandoffReportDraftBlocker.PayloadPathFieldRejected);
        }

        if (ContainsPayload(request.ProposedCommand))
        {
            blockers.Add(ProductLedgerLocalApprovedHandoffReportDraftBlocker.PayloadCommandFieldRejected);
        }

        if (ContainsPayload(request.ProposedUrl)
            || ContainsPayload(request.ProposedProvider)
            || ContainsPayload(request.ProposedDbMigration))
        {
            blockers.Add(ProductLedgerLocalApprovedHandoffReportDraftBlocker.PayloadNetworkProviderFieldRejected);
        }

        if (request.ClaimsArbitraryPathInput)
        {
            blockers.Add(ProductLedgerLocalApprovedHandoffReportDraftBlocker.ClaimsArbitraryPathInput);
        }

        if (request.ClaimsFilesystemScan)
        {
            blockers.Add(ProductLedgerLocalApprovedHandoffReportDraftBlocker.ClaimsFilesystemScan);
        }

        if (request.RequestsOverwrite)
        {
            blockers.Add(ProductLedgerLocalApprovedHandoffReportDraftBlocker.RequestsOverwrite);
        }

        if (request.RequestsUserFileWrite)
        {
            blockers.Add(ProductLedgerLocalApprovedHandoffReportDraftBlocker.RequestsUserFileWrite);
        }

        if (request.RequestsPublicUiAction)
        {
            blockers.Add(ProductLedgerLocalApprovedHandoffReportDraftBlocker.RequestsPublicUiAction);
        }

        if (request.RequestsProductCommandExecution)
        {
            blockers.Add(ProductLedgerLocalApprovedHandoffReportDraftBlocker.RequestsProductCommandExecution);
        }

        if (request.RequestsProductCommandHandler)
        {
            blockers.Add(ProductLedgerLocalApprovedHandoffReportDraftBlocker.RequestsProductCommandHandler);
        }

        if (request.RequestsProductiveServiceRegistration)
        {
            blockers.Add(ProductLedgerLocalApprovedHandoffReportDraftBlocker.RequestsProductiveServiceRegistration);
        }

        if (request.RequestsShellOrSubprocess)
        {
            blockers.Add(ProductLedgerLocalApprovedHandoffReportDraftBlocker.RequestsShellOrSubprocess);
        }

        if (request.ClaimsArbitraryCommandExecution)
        {
            blockers.Add(ProductLedgerLocalApprovedHandoffReportDraftBlocker.ClaimsArbitraryCommandExecution);
        }

        if (request.ClaimsProviderCloudNetwork)
        {
            blockers.Add(ProductLedgerLocalApprovedHandoffReportDraftBlocker.ClaimsProviderCloudNetwork);
        }

        if (request.ClaimsDbMigration)
        {
            blockers.Add(ProductLedgerLocalApprovedHandoffReportDraftBlocker.ClaimsDbMigration);
        }

        if (request.ClaimsKmsWormExternalTrust)
        {
            blockers.Add(ProductLedgerLocalApprovedHandoffReportDraftBlocker.ClaimsKmsWormExternalTrust);
        }

        if (request.ClaimsBrowserCdpWcuOcrRecipesLive)
        {
            blockers.Add(ProductLedgerLocalApprovedHandoffReportDraftBlocker.ClaimsBrowserCdpWcuOcrRecipesLive);
        }

        if (request.ClaimsPilotRun)
        {
            blockers.Add(ProductLedgerLocalApprovedHandoffReportDraftBlocker.ClaimsPilotRun);
        }

        if (request.ClaimsReleaseCommercial)
        {
            blockers.Add(ProductLedgerLocalApprovedHandoffReportDraftBlocker.ClaimsReleaseCommercial);
        }
    }

    private static void AddRedactionBlockers(
        ProductLedgerLocalApprovedHandoffReportDraftRequest request,
        List<ProductLedgerLocalApprovedHandoffReportDraftBlocker> blockers)
    {
        var redaction = new RedactionBeforePersistenceService().Evaluate(
            RedactionBeforePersistencePolicy.TestOnly,
            new DurableAuditTrailAppendOnlyMinimalRequest(
                EventKind: "ProductLedgerLocalApprovedHandoffReportDraft",
                ActorReference: "local-internal-operator",
                ApprovalReference: request.ApprovalDecision?.ApprovalId ?? "approval-missing",
                EvidenceReferences: request.EvidenceReferences ?? [],
                Metadata: new Dictionary<string, string>
                {
                    ["actionId"] = request.ActionId ?? string.Empty,
                    ["actionKind"] = request.ActionKind?.ToString() ?? string.Empty,
                    ["candidateId"] = request.CandidateId ?? string.Empty,
                    ["candidateEvidenceHashPrefix"] = Prefix(request.CandidateEvidenceHash),
                    ["draftTitle"] = request.DraftTitle ?? string.Empty,
                    ["redactedDraftSummary"] = request.RedactedDraftSummary ?? string.Empty,
                    ["outputBoundary"] = AllowedRelativeOutputBoundary
                },
                RawPayload: null));

        if (!redaction.Succeeded || redaction.Decision != RedactionBeforePersistenceDecision.Allowed)
        {
            blockers.Add(ProductLedgerLocalApprovedHandoffReportDraftBlocker.RedactionRejected);
        }
    }

    private bool BoundaryAllowed()
    {
        if (!options.ExplicitLocalApprovedHandoffDraftBoundary
            || options.AllowsArbitraryPathInput
            || options.AllowsFilesystemScan
            || options.AllowsOverwrite
            || options.AllowsUserFileWrite
            || options.AllowsShellOrSubprocess
            || options.AllowsCommandExecution
            || options.AllowsNetwork
            || options.AllowsDb
            || options.AllowsKmsWormExternalTrust
            || options.AllowsReleaseCommercial
            || string.IsNullOrWhiteSpace(options.OutputRootPath))
        {
            return false;
        }

        var full = Path.GetFullPath(options.OutputRootPath);
        var normalized = full.Replace('\\', '/');
        return normalized.EndsWith(AllowedRelativeOutputBoundary.TrimEnd('/'), StringComparison.OrdinalIgnoreCase)
            && full.IndexOf("..", StringComparison.Ordinal) < 0
            && !Path.GetPathRoot(full)!.Equals(full, StringComparison.OrdinalIgnoreCase);
    }

    private string FullDraftPath(string relativePath)
    {
        var outputRoot = Path.GetFullPath(options.OutputRootPath);
        var fullPath = Path.GetFullPath(Path.Combine(outputRoot, Path.GetFileName(relativePath)));
        return fullPath.StartsWith(outputRoot, StringComparison.OrdinalIgnoreCase)
            ? fullPath
            : throw new InvalidOperationException("draft output escaped allowlisted boundary");
    }

    private static string DraftRelativePath(ProductLedgerLocalApprovedHandoffReportDraftRequest request) =>
        AllowedRelativeOutputBoundary
        + $"local-approved-handoff-draft-{request.ActionId!.Trim()}-{Prefix(request.CandidateEvidenceHash)}.md";

    private static string DraftContent(
        ProductLedgerLocalApprovedHandoffReportDraftRequest request,
        string relativePath,
        IReadOnlyList<string> evidenceRefs,
        DateTimeOffset createdAt)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# Local Approved Handoff Report Draft");
        builder.AppendLine();
        builder.AppendLine($"Action id: `{request.ActionId!.Trim()}`");
        builder.AppendLine($"Candidate id: `{request.CandidateId!.Trim()}`");
        builder.AppendLine($"Approval decision id: `{request.ApprovalDecision!.ApprovalId}`");
        builder.AppendLine($"No-op execution id: `{request.NoOpExecution!.ExecutionId}`");
        builder.AppendLine($"Bounded execution id: `{request.BoundedExecution!.ExecutionId}`");
        builder.AppendLine($"Created at UTC: `{createdAt:O}`");
        builder.AppendLine($"Draft relative path: `{relativePath}`");
        builder.AppendLine("Classification: local/internal/development-only.");
        builder.AppendLine("Boundary: create-only, no-overwrite, allowlisted docs/test-output.");
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
        builder.AppendLine("- No shell/subprocess.");
        builder.AppendLine("- No command execution.");
        builder.AppendLine("- No cloud/network/DB.");
        builder.AppendLine("- No KMS/WORM/compliance custody.");
        builder.AppendLine("- No Browser/CDP/WCU/OCR/Recipes live.");
        builder.AppendLine("- No Pilot /run.");
        builder.AppendLine("- No release/commercial.");
        builder.AppendLine("- No business signoff.");
        builder.AppendLine();
        builder.AppendLine("Safe next step: local-only read-only audit of this generated draft action.");
        return builder.ToString();
    }

    private static IReadOnlyList<string> EvidenceRefs(
        ProductLedgerLocalApprovedHandoffReportDraftRequest request) =>
        (request.EvidenceReferences ?? [])
        .Concat(request.ApprovalDecision?.EvidenceReferences ?? [])
        .Concat(request.NoOpExecution?.EvidenceReferences ?? [])
        .Concat(request.BoundedExecution?.EvidenceReferences ?? [])
        .Concat(["product-ledger-local-approved-handoff-report-draft-created"])
        .Select(evidence => evidence.Trim())
        .Where(evidence => !string.IsNullOrWhiteSpace(evidence))
        .Distinct(StringComparer.Ordinal)
        .OrderBy(evidence => evidence, StringComparer.Ordinal)
        .ToArray();

    private static ProductLedgerLocalApprovedHandoffReportDraftSnapshot SnapshotFrom(
        ProductLedgerLocalApprovedHandoffReportDraftRequest request,
        string relativePath,
        IReadOnlyList<string> evidenceRefs,
        string contentHash,
        DateTimeOffset createdAt,
        ProductLedgerLocalApprovedHandoffReportDraftDecision decision) =>
        new(
            Decision: decision,
            State: ProductLedgerLocalApprovedHandoffReportDraftState.DraftCreatedLocalOnly,
            Blockers: [],
            ActionId: request.ActionId!.Trim(),
            CandidateId: request.CandidateId!.Trim(),
            ApprovalId: request.ApprovalDecision!.ApprovalId,
            NoOpExecutionId: request.NoOpExecution!.ExecutionId,
            BoundedExecutionId: request.BoundedExecution!.ExecutionId,
            DraftId: $"draft-{request.ActionId!.Trim()}-{Prefix(contentHash)}",
            DraftRelativePath: relativePath,
            OutputBoundary: AllowedRelativeOutputBoundary,
            ActionKind: ProductLedgerLocalApprovedHandoffReportDraftActionKind.LocalApprovedHandoffReportDraft.ToString(),
            Scope: ScopeId,
            CreateOnly: true,
            OverwriteAllowed: false,
            UserFileWrite: false,
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

    private static ProductLedgerLocalApprovedHandoffReportDraftSnapshot SnapshotRejected(
        IReadOnlyList<ProductLedgerLocalApprovedHandoffReportDraftBlocker> blockers) =>
        ProductLedgerLocalApprovedHandoffReportDraftSnapshot.Pending with
        {
            Decision = ProductLedgerLocalApprovedHandoffReportDraftDecision.Rejected,
            State = blockers.Contains(ProductLedgerLocalApprovedHandoffReportDraftBlocker.ExistingDraftConflict)
                ? ProductLedgerLocalApprovedHandoffReportDraftState.DraftReplayBlocked
                : ProductLedgerLocalApprovedHandoffReportDraftState.DraftBlocked,
            Blockers = blockers.Distinct().OrderBy(blocker => blocker.ToString(), StringComparer.Ordinal).ToArray(),
            StatusText = RejectedStatus
        };

    private ProductLedgerLocalApprovedHandoffReportDraftSnapshot Remember(
        ProductLedgerLocalApprovedHandoffReportDraftSnapshot snapshot)
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

    private static string HashText(string material)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(material));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string Prefix(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "none" : value[..Math.Min(12, value.Length)];
}
