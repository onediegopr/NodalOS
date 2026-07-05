using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OneBrain.Core.Approval;

public enum ProductLedgerLocalBoundedApprovedActionDecision
{
    Rejected,
    BoundedLocalCompletionRecorded,
    IdempotentReplay,
    LoadedLocalOnly
}

public enum ProductLedgerLocalBoundedApprovedActionState
{
    PendingOperatorDecision,
    BoundedExecutionPendingLocalOnly,
    BoundedExecutionCompletedLocalOnly,
    BoundedExecutionBlocked,
    BoundedExecutionReplayBlocked,
    BoundedExecutionTamperBlocked
}

public enum ProductLedgerLocalBoundedApprovedActionKind
{
    BoundedInternalCompletionMarker
}

public enum ProductLedgerLocalBoundedApprovedActionBlocker
{
    MissingRequest,
    MissingExplicitLocalBoundedActionScope,
    MissingApprovalDecision,
    ApprovalDecisionNotApproved,
    ApprovalDecisionHasBlockers,
    MissingNoOpExecution,
    NoOpExecutionNotCompleted,
    MissingExecutionId,
    MissingActionId,
    MissingCandidateActionKind,
    CandidateActionMismatch,
    MissingCandidateEvidenceHash,
    CandidateEvidenceHashMismatch,
    MissingEvidenceReferences,
    NonDevelopmentMode,
    NonLocalMode,
    NonInternalMode,
    UnknownActionKind,
    RequestsPublicUiAction,
    RequestsProductCommandExecution,
    RequestsProductCommandHandler,
    RequestsProductiveServiceRegistration,
    RequestsPhysicalExport,
    RequestsFileWriteOutsideExecutionStore,
    RequestsUserFileWrite,
    RequestsShellOrSubprocess,
    ClaimsArbitraryCommandExecution,
    ClaimsArbitraryPathInput,
    ClaimsFilesystemScan,
    ClaimsProviderCloudNetwork,
    ClaimsDbMigration,
    ClaimsKmsWormExternalTrust,
    ClaimsBrowserCdpWcuOcrRecipesLive,
    ClaimsPilotRun,
    ClaimsReleaseCommercial,
    ContainsPathCommandOrNetworkPayload,
    StoreBoundaryRejected,
    StoreTamperedOrCorrupt,
    ExistingBoundedActionConflict
}

public sealed record ProductLedgerLocalBoundedApprovedActionRequest(
    bool ExplicitLocalBoundedActionScope,
    bool DevelopmentMode,
    bool LocalMode,
    bool InternalMode,
    string? ExecutionId,
    string? ActionId,
    ProductLedgerLocalBoundedApprovedActionKind? ActionKind,
    ProductLedgerLocalApprovalDecisionSnapshot? ApprovalDecision,
    ProductLedgerLocalApprovedActionExecutionSnapshot? NoOpExecution,
    ProductLedgerInternalCommandKind? CandidateActionKind,
    string? CandidateEvidenceHash,
    string? CurrentEvidenceHash,
    IReadOnlyList<string>? EvidenceReferences,
    string? ProposedPath,
    string? ProposedCommand,
    string? ProposedUrl,
    bool RequestsPublicUiAction,
    bool RequestsProductCommandExecution,
    bool RequestsProductCommandHandler,
    bool RequestsProductiveServiceRegistration,
    bool RequestsPhysicalExport,
    bool RequestsFileWriteOutsideExecutionStore,
    bool RequestsUserFileWrite,
    bool RequestsShellOrSubprocess,
    bool ClaimsArbitraryCommandExecution,
    bool ClaimsArbitraryPathInput,
    bool ClaimsFilesystemScan,
    bool ClaimsProviderCloudNetwork,
    bool ClaimsDbMigration,
    bool ClaimsKmsWormExternalTrust,
    bool ClaimsBrowserCdpWcuOcrRecipesLive,
    bool ClaimsPilotRun,
    bool ClaimsReleaseCommercial);

public sealed record ProductLedgerLocalBoundedApprovedActionSnapshot(
    ProductLedgerLocalBoundedApprovedActionDecision Decision,
    ProductLedgerLocalBoundedApprovedActionState State,
    IReadOnlyList<ProductLedgerLocalBoundedApprovedActionBlocker> Blockers,
    string ExecutionId,
    string ActionId,
    string ActionKind,
    string ApprovalId,
    string NoOpExecutionId,
    string CandidateActionKind,
    string CandidateEvidenceHash,
    string CandidateEvidenceHashPrefix,
    string ResultHashPrefix,
    IReadOnlyList<string> EvidenceReferences,
    bool LocalOnly,
    bool InternalOnly,
    bool DevelopmentOnly,
    bool DefaultOff,
    bool FailClosed,
    bool NonDestructive,
    bool BoundedInternalCompletionMarker,
    bool TouchesUserFiles,
    bool ShellOrSubprocessAllowed,
    bool ArbitraryCommandExecutionAllowed,
    bool ProductCommandExecuted,
    bool PublicUiActionAvailable,
    bool ProductCommandHandlerAvailable,
    bool ProductiveServiceRegistrationAvailable,
    bool PhysicalExportCreated,
    bool FileWriteOutsideExecutionStorePerformed,
    bool ProviderCloudNetworkAvailable,
    bool DbMigrationAvailable,
    bool KmsWormExternalTrustAvailable,
    bool BrowserCdpWcuOcrRecipesLiveAvailable,
    bool PilotRunAvailable,
    bool ReleaseCommercialReady,
    string StatusText)
{
    public static ProductLedgerLocalBoundedApprovedActionSnapshot Pending { get; } =
        new(
            Decision: ProductLedgerLocalBoundedApprovedActionDecision.Rejected,
            State: ProductLedgerLocalBoundedApprovedActionState.PendingOperatorDecision,
            Blockers: [],
            ExecutionId: "approval-bounded-action.pending",
            ActionId: "bounded-internal-completion-marker.pending",
            ActionKind: ProductLedgerLocalBoundedApprovedActionKind.BoundedInternalCompletionMarker.ToString(),
            ApprovalId: "approval-state.pending-preview-only",
            NoOpExecutionId: "approval-execution.pending-no-op",
            CandidateActionKind: ProductLedgerInternalCommandKind.ViewLedgerReadiness.ToString(),
            CandidateEvidenceHash: string.Empty,
            CandidateEvidenceHashPrefix: "none",
            ResultHashPrefix: "none",
            EvidenceReferences: ["product-ledger-local-bounded-approved-action-boundary"],
            LocalOnly: true,
            InternalOnly: true,
            DevelopmentOnly: true,
            DefaultOff: true,
            FailClosed: true,
            NonDestructive: true,
            BoundedInternalCompletionMarker: true,
            TouchesUserFiles: false,
            ShellOrSubprocessAllowed: false,
            ArbitraryCommandExecutionAllowed: false,
            ProductCommandExecuted: false,
            PublicUiActionAvailable: false,
            ProductCommandHandlerAvailable: false,
            ProductiveServiceRegistrationAvailable: false,
            PhysicalExportCreated: false,
            FileWriteOutsideExecutionStorePerformed: false,
            ProviderCloudNetworkAvailable: false,
            DbMigrationAvailable: false,
            KmsWormExternalTrustAvailable: false,
            BrowserCdpWcuOcrRecipesLiveAvailable: false,
            PilotRunAvailable: false,
            ReleaseCommercialReady: false,
            StatusText: ProductLedgerLocalBoundedApprovedActionExecutor.PendingStatus);
}

public sealed class ProductLedgerLocalBoundedApprovedActionExecutor
{
    public const string PendingStatus =
        "PRODUCT_LEDGER_LOCAL_BOUNDED_APPROVED_ACTION_PENDING LOCAL_ONLY INTERNAL_ONLY DEVELOPMENT_ONLY DEFAULT_OFF FAIL_CLOSED BOUNDED_INTERNAL_COMPLETION_MARKER NON_DESTRUCTIVE NO_USER_FILE_WRITE NO_COMMAND_EXECUTION NO_SHELL_SUBPROCESS NO_PUBLIC_UI_ACTION NO_PRODUCT_COMMAND_HANDLER NO_EXPORT NO_PROVIDER_CLOUD_NETWORK NO_DB_MIGRATION NO_KMS_WORM_EXTERNAL_TRUST NO_LIVE_AUTOMATION NO_RELEASE_COMMERCIAL";

    public const string CompletedStatus =
        "PRODUCT_LEDGER_LOCAL_BOUNDED_APPROVED_ACTION_COMPLETED LOCAL_ONLY INTERNAL_ONLY DEVELOPMENT_ONLY DEFAULT_OFF FAIL_CLOSED BOUNDED_INTERNAL_COMPLETION_MARKER NON_DESTRUCTIVE NO_USER_FILE_WRITE NO_COMMAND_EXECUTION NO_SHELL_SUBPROCESS NO_PUBLIC_UI_ACTION NO_PRODUCT_COMMAND_HANDLER NO_EXPORT NO_PROVIDER_CLOUD_NETWORK NO_DB_MIGRATION NO_KMS_WORM_EXTERNAL_TRUST NO_LIVE_AUTOMATION NO_RELEASE_COMMERCIAL";

    public const string RejectedStatus =
        "PRODUCT_LEDGER_LOCAL_BOUNDED_APPROVED_ACTION_REJECTED FAIL_CLOSED NO_USER_FILE_WRITE NO_COMMAND_EXECUTION NO_SHELL_SUBPROCESS NO_PUBLIC_UI_ACTION NO_PRODUCT_COMMAND_HANDLER NO_EXPORT NO_PROVIDER_CLOUD_NETWORK NO_DB_MIGRATION NO_KMS_WORM_EXTERNAL_TRUST NO_LIVE_AUTOMATION NO_RELEASE_COMMERCIAL";

    private const string StoreFileName = "product-ledger-local-bounded-approved-action.json";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    static ProductLedgerLocalBoundedApprovedActionExecutor()
    {
        JsonOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
    }

    private readonly ProductLedgerLocalApprovedActionExecutionStoreOptions options;

    public ProductLedgerLocalBoundedApprovedActionExecutor(
        ProductLedgerLocalApprovedActionExecutionStoreOptions options)
    {
        this.options = options;
    }

    public ProductLedgerLocalBoundedApprovedActionSnapshot ExecuteBoundedCompletionMarker(
        ProductLedgerLocalBoundedApprovedActionRequest? request)
    {
        var blockers = ValidateRequest(request);
        if (blockers.Count > 0 || request is null)
        {
            return SnapshotRejected(blockers);
        }

        var envelope = EnvelopeFrom(request);
        var existing = File.Exists(StateFilePath()) ? ReadEnvelope() : null;
        if (existing?.Decision == ProductLedgerLocalBoundedApprovedActionDecision.LoadedLocalOnly)
        {
            if (SameBoundedAction(existing, envelope))
            {
                return existing with
                {
                    Decision = ProductLedgerLocalBoundedApprovedActionDecision.IdempotentReplay,
                    StatusText = CompletedStatus
                };
            }

            return existing with
            {
                Decision = ProductLedgerLocalBoundedApprovedActionDecision.Rejected,
                State = ProductLedgerLocalBoundedApprovedActionState.BoundedExecutionReplayBlocked,
                Blockers = [ProductLedgerLocalBoundedApprovedActionBlocker.ExistingBoundedActionConflict],
                StatusText = RejectedStatus
            };
        }

        if (existing?.Blockers.Contains(ProductLedgerLocalBoundedApprovedActionBlocker.StoreTamperedOrCorrupt) == true)
        {
            return existing;
        }

        Directory.CreateDirectory(options.StoreRootPath);
        File.WriteAllText(StateFilePath(), JsonSerializer.Serialize(envelope, JsonOptions), Encoding.UTF8);
        return SnapshotFrom(
            envelope,
            ProductLedgerLocalBoundedApprovedActionDecision.BoundedLocalCompletionRecorded,
            []);
    }

    public ProductLedgerLocalBoundedApprovedActionSnapshot Read()
    {
        if (!BoundaryAllowed())
        {
            return SnapshotRejected([ProductLedgerLocalBoundedApprovedActionBlocker.StoreBoundaryRejected]);
        }

        if (!File.Exists(StateFilePath()))
        {
            return ProductLedgerLocalBoundedApprovedActionSnapshot.Pending;
        }

        return ReadEnvelope();
    }

    private IReadOnlyList<ProductLedgerLocalBoundedApprovedActionBlocker> ValidateRequest(
        ProductLedgerLocalBoundedApprovedActionRequest? request)
    {
        var blockers = new List<ProductLedgerLocalBoundedApprovedActionBlocker>();
        if (request is null)
        {
            blockers.Add(ProductLedgerLocalBoundedApprovedActionBlocker.MissingRequest);
            return blockers;
        }

        if (!BoundaryAllowed())
        {
            blockers.Add(ProductLedgerLocalBoundedApprovedActionBlocker.StoreBoundaryRejected);
        }

        if (!request.ExplicitLocalBoundedActionScope)
        {
            blockers.Add(ProductLedgerLocalBoundedApprovedActionBlocker.MissingExplicitLocalBoundedActionScope);
        }

        if (!request.DevelopmentMode)
        {
            blockers.Add(ProductLedgerLocalBoundedApprovedActionBlocker.NonDevelopmentMode);
        }

        if (!request.LocalMode)
        {
            blockers.Add(ProductLedgerLocalBoundedApprovedActionBlocker.NonLocalMode);
        }

        if (!request.InternalMode)
        {
            blockers.Add(ProductLedgerLocalBoundedApprovedActionBlocker.NonInternalMode);
        }

        if (string.IsNullOrWhiteSpace(request.ExecutionId))
        {
            blockers.Add(ProductLedgerLocalBoundedApprovedActionBlocker.MissingExecutionId);
        }

        if (string.IsNullOrWhiteSpace(request.ActionId))
        {
            blockers.Add(ProductLedgerLocalBoundedApprovedActionBlocker.MissingActionId);
        }

        if (request.ActionKind != ProductLedgerLocalBoundedApprovedActionKind.BoundedInternalCompletionMarker)
        {
            blockers.Add(ProductLedgerLocalBoundedApprovedActionBlocker.UnknownActionKind);
        }

        if (request.CandidateActionKind is null)
        {
            blockers.Add(ProductLedgerLocalBoundedApprovedActionBlocker.MissingCandidateActionKind);
        }

        AddApprovalDecisionBlockers(request, blockers);
        AddNoOpBlockers(request, blockers);
        AddEvidenceBlockers(request, blockers);
        AddAuthorityBlockers(request, blockers);
        return blockers.Distinct().OrderBy(blocker => blocker.ToString(), StringComparer.Ordinal).ToArray();
    }

    private static void AddApprovalDecisionBlockers(
        ProductLedgerLocalBoundedApprovedActionRequest request,
        List<ProductLedgerLocalBoundedApprovedActionBlocker> blockers)
    {
        var approval = request.ApprovalDecision;
        if (approval is null)
        {
            blockers.Add(ProductLedgerLocalBoundedApprovedActionBlocker.MissingApprovalDecision);
            return;
        }

        if (approval.State != ProductLedgerLocalApprovalDecisionState.ApprovedLocalOnly
            || !string.Equals(approval.OperatorDecision, ProductLedgerLocalApprovalOperatorDecisionKind.Approve.ToString(), StringComparison.Ordinal))
        {
            blockers.Add(ProductLedgerLocalBoundedApprovedActionBlocker.ApprovalDecisionNotApproved);
        }

        if (approval.Blockers.Count > 0
            || approval.Decision == ProductLedgerLocalApprovalDecisionStoreDecision.Rejected)
        {
            blockers.Add(ProductLedgerLocalBoundedApprovedActionBlocker.ApprovalDecisionHasBlockers);
        }

        if (request.CandidateActionKind is not null
            && !string.Equals(approval.CandidateActionKind, request.CandidateActionKind.Value.ToString(), StringComparison.Ordinal))
        {
            blockers.Add(ProductLedgerLocalBoundedApprovedActionBlocker.CandidateActionMismatch);
        }
    }

    private static void AddNoOpBlockers(
        ProductLedgerLocalBoundedApprovedActionRequest request,
        List<ProductLedgerLocalBoundedApprovedActionBlocker> blockers)
    {
        var noOp = request.NoOpExecution;
        if (noOp is null)
        {
            blockers.Add(ProductLedgerLocalBoundedApprovedActionBlocker.MissingNoOpExecution);
            return;
        }

        if (noOp.State != ProductLedgerLocalApprovedActionExecutionState.NoOpExecutionCompletedLocalOnly
            || noOp.Blockers.Count > 0
            || noOp.ProductCommandExecuted
            || noOp.PublicUiActionAvailable
            || noOp.ProductCommandHandlerAvailable
            || noOp.FileWriteOutsideExecutionStorePerformed)
        {
            blockers.Add(ProductLedgerLocalBoundedApprovedActionBlocker.NoOpExecutionNotCompleted);
        }

        if (request.CandidateActionKind is not null
            && !string.Equals(noOp.CandidateActionKind, request.CandidateActionKind.Value.ToString(), StringComparison.Ordinal))
        {
            blockers.Add(ProductLedgerLocalBoundedApprovedActionBlocker.CandidateActionMismatch);
        }
    }

    private static void AddEvidenceBlockers(
        ProductLedgerLocalBoundedApprovedActionRequest request,
        List<ProductLedgerLocalBoundedApprovedActionBlocker> blockers)
    {
        if (string.IsNullOrWhiteSpace(request.CandidateEvidenceHash)
            || string.IsNullOrWhiteSpace(request.CurrentEvidenceHash)
            || string.IsNullOrWhiteSpace(request.ApprovalDecision?.CandidateEvidenceHash)
            || string.IsNullOrWhiteSpace(request.NoOpExecution?.CandidateEvidenceHash))
        {
            blockers.Add(ProductLedgerLocalBoundedApprovedActionBlocker.MissingCandidateEvidenceHash);
        }
        else if (!string.Equals(request.CandidateEvidenceHash.Trim(), request.CurrentEvidenceHash.Trim(), StringComparison.Ordinal)
            || !string.Equals(request.CandidateEvidenceHash.Trim(), request.ApprovalDecision.CandidateEvidenceHash, StringComparison.Ordinal)
            || !string.Equals(request.CandidateEvidenceHash.Trim(), request.NoOpExecution.CandidateEvidenceHash, StringComparison.Ordinal))
        {
            blockers.Add(ProductLedgerLocalBoundedApprovedActionBlocker.CandidateEvidenceHashMismatch);
        }

        if (request.EvidenceReferences is null
            || request.EvidenceReferences.Count == 0
            || request.EvidenceReferences.Any(string.IsNullOrWhiteSpace)
            || request.ApprovalDecision?.EvidenceReferences.Count == 0
            || request.NoOpExecution?.EvidenceReferences.Count == 0)
        {
            blockers.Add(ProductLedgerLocalBoundedApprovedActionBlocker.MissingEvidenceReferences);
        }
    }

    private static void AddAuthorityBlockers(
        ProductLedgerLocalBoundedApprovedActionRequest request,
        List<ProductLedgerLocalBoundedApprovedActionBlocker> blockers)
    {
        if (request.RequestsPublicUiAction)
        {
            blockers.Add(ProductLedgerLocalBoundedApprovedActionBlocker.RequestsPublicUiAction);
        }

        if (request.RequestsProductCommandExecution)
        {
            blockers.Add(ProductLedgerLocalBoundedApprovedActionBlocker.RequestsProductCommandExecution);
        }

        if (request.RequestsProductCommandHandler)
        {
            blockers.Add(ProductLedgerLocalBoundedApprovedActionBlocker.RequestsProductCommandHandler);
        }

        if (request.RequestsProductiveServiceRegistration)
        {
            blockers.Add(ProductLedgerLocalBoundedApprovedActionBlocker.RequestsProductiveServiceRegistration);
        }

        if (request.RequestsPhysicalExport)
        {
            blockers.Add(ProductLedgerLocalBoundedApprovedActionBlocker.RequestsPhysicalExport);
        }

        if (request.RequestsFileWriteOutsideExecutionStore)
        {
            blockers.Add(ProductLedgerLocalBoundedApprovedActionBlocker.RequestsFileWriteOutsideExecutionStore);
        }

        if (request.RequestsUserFileWrite)
        {
            blockers.Add(ProductLedgerLocalBoundedApprovedActionBlocker.RequestsUserFileWrite);
        }

        if (request.RequestsShellOrSubprocess)
        {
            blockers.Add(ProductLedgerLocalBoundedApprovedActionBlocker.RequestsShellOrSubprocess);
        }

        if (request.ClaimsArbitraryCommandExecution)
        {
            blockers.Add(ProductLedgerLocalBoundedApprovedActionBlocker.ClaimsArbitraryCommandExecution);
        }

        if (request.ClaimsArbitraryPathInput)
        {
            blockers.Add(ProductLedgerLocalBoundedApprovedActionBlocker.ClaimsArbitraryPathInput);
        }

        if (request.ClaimsFilesystemScan)
        {
            blockers.Add(ProductLedgerLocalBoundedApprovedActionBlocker.ClaimsFilesystemScan);
        }

        if (request.ClaimsProviderCloudNetwork)
        {
            blockers.Add(ProductLedgerLocalBoundedApprovedActionBlocker.ClaimsProviderCloudNetwork);
        }

        if (request.ClaimsDbMigration)
        {
            blockers.Add(ProductLedgerLocalBoundedApprovedActionBlocker.ClaimsDbMigration);
        }

        if (request.ClaimsKmsWormExternalTrust)
        {
            blockers.Add(ProductLedgerLocalBoundedApprovedActionBlocker.ClaimsKmsWormExternalTrust);
        }

        if (request.ClaimsBrowserCdpWcuOcrRecipesLive)
        {
            blockers.Add(ProductLedgerLocalBoundedApprovedActionBlocker.ClaimsBrowserCdpWcuOcrRecipesLive);
        }

        if (request.ClaimsPilotRun)
        {
            blockers.Add(ProductLedgerLocalBoundedApprovedActionBlocker.ClaimsPilotRun);
        }

        if (request.ClaimsReleaseCommercial)
        {
            blockers.Add(ProductLedgerLocalBoundedApprovedActionBlocker.ClaimsReleaseCommercial);
        }

        if (ContainsPayload(request.ProposedPath)
            || ContainsPayload(request.ProposedCommand)
            || ContainsPayload(request.ProposedUrl))
        {
            blockers.Add(ProductLedgerLocalBoundedApprovedActionBlocker.ContainsPathCommandOrNetworkPayload);
        }
    }

    private ProductLedgerLocalBoundedApprovedActionEnvelope EnvelopeFrom(
        ProductLedgerLocalBoundedApprovedActionRequest request)
    {
        var approval = request.ApprovalDecision!;
        var noOp = request.NoOpExecution!;
        var candidateHash = request.CandidateEvidenceHash!.Trim();
        var evidence = request.EvidenceReferences!
            .Concat(approval.EvidenceReferences)
            .Concat(noOp.EvidenceReferences)
            .Concat(["product-ledger-local-bounded-approved-action-completion-marker"])
            .Select(value => value.Trim())
            .Distinct(StringComparer.Ordinal)
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();
        var envelope = new ProductLedgerLocalBoundedApprovedActionEnvelope(
            SchemaVersion: 1,
            State: ProductLedgerLocalBoundedApprovedActionState.BoundedExecutionCompletedLocalOnly,
            ExecutionId: request.ExecutionId!.Trim(),
            ActionId: request.ActionId!.Trim(),
            ActionKind: request.ActionKind!.Value.ToString(),
            ApprovalId: approval.ApprovalId,
            NoOpExecutionId: noOp.ExecutionId,
            CandidateActionKind: request.CandidateActionKind!.Value.ToString(),
            CandidateEvidenceHash: candidateHash,
            EvidenceReferences: evidence,
            LocalOnly: true,
            InternalOnly: true,
            DevelopmentOnly: true,
            DefaultOff: true,
            FailClosed: true,
            NonDestructive: true,
            BoundedInternalCompletionMarker: true,
            TouchesUserFiles: false,
            ShellOrSubprocessAllowed: false,
            ArbitraryCommandExecutionAllowed: false,
            ProductCommandExecuted: false,
            PublicUiActionAvailable: false,
            ProductCommandHandlerAvailable: false,
            ProductiveServiceRegistrationAvailable: false,
            PhysicalExportCreated: false,
            FileWriteOutsideExecutionStorePerformed: false,
            ProviderCloudNetworkAvailable: false,
            DbMigrationAvailable: false,
            KmsWormExternalTrustAvailable: false,
            BrowserCdpWcuOcrRecipesLiveAvailable: false,
            PilotRunAvailable: false,
            ReleaseCommercialReady: false,
            ResultHash: string.Empty);
        return envelope with { ResultHash = Hash(envelope with { ResultHash = string.Empty }) };
    }

    private ProductLedgerLocalBoundedApprovedActionSnapshot ReadEnvelope()
    {
        try
        {
            var envelope = JsonSerializer.Deserialize<ProductLedgerLocalBoundedApprovedActionEnvelope>(
                File.ReadAllText(StateFilePath(), Encoding.UTF8),
                JsonOptions);
            if (envelope is null)
            {
                return SnapshotRejected([ProductLedgerLocalBoundedApprovedActionBlocker.StoreTamperedOrCorrupt])
                    with { State = ProductLedgerLocalBoundedApprovedActionState.BoundedExecutionTamperBlocked };
            }

            var actual = Hash(envelope with { ResultHash = string.Empty });
            return string.Equals(actual, envelope.ResultHash, StringComparison.Ordinal)
                ? SnapshotFrom(envelope, ProductLedgerLocalBoundedApprovedActionDecision.LoadedLocalOnly, [])
                : SnapshotRejected([ProductLedgerLocalBoundedApprovedActionBlocker.StoreTamperedOrCorrupt])
                    with { State = ProductLedgerLocalBoundedApprovedActionState.BoundedExecutionTamperBlocked };
        }
        catch (JsonException)
        {
            return SnapshotRejected([ProductLedgerLocalBoundedApprovedActionBlocker.StoreTamperedOrCorrupt])
                with { State = ProductLedgerLocalBoundedApprovedActionState.BoundedExecutionTamperBlocked };
        }
        catch (IOException)
        {
            return SnapshotRejected([ProductLedgerLocalBoundedApprovedActionBlocker.StoreTamperedOrCorrupt])
                with { State = ProductLedgerLocalBoundedApprovedActionState.BoundedExecutionTamperBlocked };
        }
        catch (UnauthorizedAccessException)
        {
            return SnapshotRejected([ProductLedgerLocalBoundedApprovedActionBlocker.StoreTamperedOrCorrupt])
                with { State = ProductLedgerLocalBoundedApprovedActionState.BoundedExecutionTamperBlocked };
        }
    }

    private static bool SameBoundedAction(
        ProductLedgerLocalBoundedApprovedActionSnapshot existing,
        ProductLedgerLocalBoundedApprovedActionEnvelope envelope) =>
        string.Equals(existing.ExecutionId, envelope.ExecutionId, StringComparison.Ordinal)
        && string.Equals(existing.ActionId, envelope.ActionId, StringComparison.Ordinal)
        && string.Equals(existing.ActionKind, envelope.ActionKind, StringComparison.Ordinal)
        && string.Equals(existing.ApprovalId, envelope.ApprovalId, StringComparison.Ordinal)
        && string.Equals(existing.NoOpExecutionId, envelope.NoOpExecutionId, StringComparison.Ordinal)
        && string.Equals(existing.CandidateActionKind, envelope.CandidateActionKind, StringComparison.Ordinal)
        && string.Equals(existing.CandidateEvidenceHash, envelope.CandidateEvidenceHash, StringComparison.Ordinal)
        && string.Equals(existing.ResultHashPrefix, Prefix(envelope.ResultHash), StringComparison.Ordinal);

    private static ProductLedgerLocalBoundedApprovedActionSnapshot SnapshotFrom(
        ProductLedgerLocalBoundedApprovedActionEnvelope envelope,
        ProductLedgerLocalBoundedApprovedActionDecision decision,
        IReadOnlyList<ProductLedgerLocalBoundedApprovedActionBlocker> blockers) =>
        new(
            Decision: blockers.Count == 0 ? decision : ProductLedgerLocalBoundedApprovedActionDecision.Rejected,
            State: blockers.Count == 0 ? envelope.State : ProductLedgerLocalBoundedApprovedActionState.BoundedExecutionBlocked,
            Blockers: blockers.OrderBy(blocker => blocker.ToString(), StringComparer.Ordinal).ToArray(),
            ExecutionId: envelope.ExecutionId,
            ActionId: envelope.ActionId,
            ActionKind: envelope.ActionKind,
            ApprovalId: envelope.ApprovalId,
            NoOpExecutionId: envelope.NoOpExecutionId,
            CandidateActionKind: envelope.CandidateActionKind,
            CandidateEvidenceHash: envelope.CandidateEvidenceHash,
            CandidateEvidenceHashPrefix: Prefix(envelope.CandidateEvidenceHash),
            ResultHashPrefix: Prefix(envelope.ResultHash),
            EvidenceReferences: envelope.EvidenceReferences,
            LocalOnly: envelope.LocalOnly,
            InternalOnly: envelope.InternalOnly,
            DevelopmentOnly: envelope.DevelopmentOnly,
            DefaultOff: envelope.DefaultOff,
            FailClosed: envelope.FailClosed,
            NonDestructive: envelope.NonDestructive,
            BoundedInternalCompletionMarker: envelope.BoundedInternalCompletionMarker,
            TouchesUserFiles: envelope.TouchesUserFiles,
            ShellOrSubprocessAllowed: envelope.ShellOrSubprocessAllowed,
            ArbitraryCommandExecutionAllowed: envelope.ArbitraryCommandExecutionAllowed,
            ProductCommandExecuted: envelope.ProductCommandExecuted,
            PublicUiActionAvailable: envelope.PublicUiActionAvailable,
            ProductCommandHandlerAvailable: envelope.ProductCommandHandlerAvailable,
            ProductiveServiceRegistrationAvailable: envelope.ProductiveServiceRegistrationAvailable,
            PhysicalExportCreated: envelope.PhysicalExportCreated,
            FileWriteOutsideExecutionStorePerformed: envelope.FileWriteOutsideExecutionStorePerformed,
            ProviderCloudNetworkAvailable: envelope.ProviderCloudNetworkAvailable,
            DbMigrationAvailable: envelope.DbMigrationAvailable,
            KmsWormExternalTrustAvailable: envelope.KmsWormExternalTrustAvailable,
            BrowserCdpWcuOcrRecipesLiveAvailable: envelope.BrowserCdpWcuOcrRecipesLiveAvailable,
            PilotRunAvailable: envelope.PilotRunAvailable,
            ReleaseCommercialReady: envelope.ReleaseCommercialReady,
            StatusText: blockers.Count == 0 ? CompletedStatus : RejectedStatus);

    private static ProductLedgerLocalBoundedApprovedActionSnapshot SnapshotRejected(
        IReadOnlyList<ProductLedgerLocalBoundedApprovedActionBlocker> blockers) =>
        ProductLedgerLocalBoundedApprovedActionSnapshot.Pending with
        {
            Decision = ProductLedgerLocalBoundedApprovedActionDecision.Rejected,
            State = ProductLedgerLocalBoundedApprovedActionState.BoundedExecutionBlocked,
            Blockers = blockers.Distinct().OrderBy(blocker => blocker.ToString(), StringComparer.Ordinal).ToArray(),
            StatusText = RejectedStatus
        };

    private bool BoundaryAllowed()
    {
        if (!options.ExplicitLocalOnlyExecutionStore
            || options.AllowsArbitraryPathInput
            || options.AllowsFilesystemScan
            || options.AllowsExport
            || options.AllowsNetwork
            || options.AllowsDb
            || options.AllowsReleaseCommercial
            || string.IsNullOrWhiteSpace(options.StoreRootPath))
        {
            return false;
        }

        var full = Path.GetFullPath(options.StoreRootPath);
        return full.IndexOf("..", StringComparison.Ordinal) < 0
            && !Path.GetPathRoot(full)!.Equals(full, StringComparison.OrdinalIgnoreCase);
    }

    private string StateFilePath() =>
        Path.Combine(Path.GetFullPath(options.StoreRootPath), StoreFileName);

    private static bool ContainsPayload(string? value) =>
        !string.IsNullOrWhiteSpace(value);

    private static string Hash(ProductLedgerLocalBoundedApprovedActionEnvelope envelope)
    {
        var json = JsonSerializer.Serialize(envelope, JsonOptions);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json))).ToLowerInvariant();
    }

    private static string Prefix(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "none" : value[..Math.Min(12, value.Length)];

    private sealed record ProductLedgerLocalBoundedApprovedActionEnvelope(
        int SchemaVersion,
        ProductLedgerLocalBoundedApprovedActionState State,
        string ExecutionId,
        string ActionId,
        string ActionKind,
        string ApprovalId,
        string NoOpExecutionId,
        string CandidateActionKind,
        string CandidateEvidenceHash,
        IReadOnlyList<string> EvidenceReferences,
        bool LocalOnly,
        bool InternalOnly,
        bool DevelopmentOnly,
        bool DefaultOff,
        bool FailClosed,
        bool NonDestructive,
        bool BoundedInternalCompletionMarker,
        bool TouchesUserFiles,
        bool ShellOrSubprocessAllowed,
        bool ArbitraryCommandExecutionAllowed,
        bool ProductCommandExecuted,
        bool PublicUiActionAvailable,
        bool ProductCommandHandlerAvailable,
        bool ProductiveServiceRegistrationAvailable,
        bool PhysicalExportCreated,
        bool FileWriteOutsideExecutionStorePerformed,
        bool ProviderCloudNetworkAvailable,
        bool DbMigrationAvailable,
        bool KmsWormExternalTrustAvailable,
        bool BrowserCdpWcuOcrRecipesLiveAvailable,
        bool PilotRunAvailable,
        bool ReleaseCommercialReady,
        string ResultHash);
}
