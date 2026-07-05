using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OneBrain.Core.Approval;

public enum ProductLedgerLocalApprovedActionExecutionDecision
{
    Rejected,
    NoOpExecutionCompletedLocalOnly,
    IdempotentReplay,
    LoadedLocalOnly
}

public enum ProductLedgerLocalApprovedActionExecutionState
{
    CandidatePreviewOnly,
    PendingOperatorDecision,
    ExecutionPendingLocalOnly,
    NoOpExecutionCompletedLocalOnly,
    ExecutionBlocked,
    ExecutionFailedSafe,
    ExecutionReplayBlocked,
    ExecutionTamperBlocked
}

public enum ProductLedgerLocalApprovedActionExecutionBlocker
{
    MissingRequest,
    MissingExplicitLocalOnlyNoOpExecutionScope,
    MissingApprovalDecision,
    ApprovalDecisionNotApproved,
    ApprovalDecisionHasBlockers,
    MissingExecutionId,
    MissingCandidateActionKind,
    CandidateActionMismatch,
    MissingCandidateEvidenceHash,
    CandidateEvidenceHashMismatch,
    MissingEvidenceReferences,
    NonDevelopmentMode,
    NonLocalMode,
    NonInternalMode,
    RequestsBoundedActionWithoutSeparateGate,
    RequestsPublicUiAction,
    RequestsProductCommandExecution,
    RequestsProductCommandHandler,
    RequestsProductiveServiceRegistration,
    RequestsPhysicalExport,
    RequestsFileWriteOutsideExecutionStore,
    ClaimsArbitraryPathInput,
    ClaimsFilesystemScan,
    ClaimsProviderCloudNetwork,
    ClaimsDbMigration,
    ClaimsKmsWormExternalTrust,
    ClaimsBrowserCdpWcuOcrRecipesLive,
    ClaimsPilotRun,
    ClaimsReleaseCommercial,
    StoreBoundaryRejected,
    StoreTamperedOrCorrupt,
    ExistingExecutionConflict
}

public sealed record ProductLedgerLocalApprovedActionExecutionStoreOptions(
    string StoreRootPath,
    bool ExplicitLocalOnlyExecutionStore,
    bool AllowsArbitraryPathInput,
    bool AllowsFilesystemScan,
    bool AllowsExport,
    bool AllowsNetwork,
    bool AllowsDb,
    bool AllowsReleaseCommercial);

public sealed record ProductLedgerLocalApprovedActionExecutionRequest(
    bool ExplicitLocalOnlyNoOpExecutionScope,
    bool DevelopmentMode,
    bool LocalMode,
    bool InternalMode,
    string? ExecutionId,
    ProductLedgerLocalApprovalDecisionSnapshot? ApprovalDecision,
    ProductLedgerInternalCommandKind? CandidateActionKind,
    string? CandidateEvidenceHash,
    string? CurrentEvidenceHash,
    IReadOnlyList<string>? EvidenceReferences,
    bool RequestsBoundedAction,
    bool RequestsPublicUiAction,
    bool RequestsProductCommandExecution,
    bool RequestsProductCommandHandler,
    bool RequestsProductiveServiceRegistration,
    bool RequestsPhysicalExport,
    bool RequestsFileWriteOutsideExecutionStore,
    bool ClaimsArbitraryPathInput,
    bool ClaimsFilesystemScan,
    bool ClaimsProviderCloudNetwork,
    bool ClaimsDbMigration,
    bool ClaimsKmsWormExternalTrust,
    bool ClaimsBrowserCdpWcuOcrRecipesLive,
    bool ClaimsPilotRun,
    bool ClaimsReleaseCommercial);

public sealed record ProductLedgerLocalApprovedActionExecutionSnapshot(
    ProductLedgerLocalApprovedActionExecutionDecision Decision,
    ProductLedgerLocalApprovedActionExecutionState State,
    IReadOnlyList<ProductLedgerLocalApprovedActionExecutionBlocker> Blockers,
    string ExecutionId,
    string ApprovalId,
    string CandidateActionKind,
    string CandidateEvidenceHash,
    string CandidateEvidenceHashPrefix,
    string ExecutionResultHashPrefix,
    IReadOnlyList<string> EvidenceReferences,
    bool LocalOnly,
    bool InternalOnly,
    bool DevelopmentOnly,
    bool DefaultOff,
    bool FailClosed,
    bool NoOpOnly,
    bool BoundedActionExecuted,
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
    public static ProductLedgerLocalApprovedActionExecutionSnapshot Pending { get; } =
        new(
            Decision: ProductLedgerLocalApprovedActionExecutionDecision.Rejected,
            State: ProductLedgerLocalApprovedActionExecutionState.PendingOperatorDecision,
            Blockers: [],
            ExecutionId: "approval-execution.pending-no-op",
            ApprovalId: "approval-state.pending-preview-only",
            CandidateActionKind: ProductLedgerInternalCommandKind.ViewLedgerReadiness.ToString(),
            CandidateEvidenceHash: string.Empty,
            CandidateEvidenceHashPrefix: "none",
            ExecutionResultHashPrefix: "none",
            EvidenceReferences: ["product-ledger-local-approved-no-op-execution-boundary"],
            LocalOnly: true,
            InternalOnly: true,
            DevelopmentOnly: true,
            DefaultOff: true,
            FailClosed: true,
            NoOpOnly: true,
            BoundedActionExecuted: false,
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
            StatusText: ProductLedgerLocalApprovedActionNoOpExecutor.PendingStatus);
}

public sealed class ProductLedgerLocalApprovedActionNoOpExecutor
{
    public const string PendingStatus =
        "PRODUCT_LEDGER_LOCAL_APPROVED_ACTION_EXECUTION_PENDING LOCAL_ONLY INTERNAL_ONLY DEVELOPMENT_ONLY DEFAULT_OFF FAIL_CLOSED NO_OP_ONLY NO_COMMAND_EXECUTION NO_PUBLIC_UI_ACTION NO_PRODUCT_COMMAND_HANDLER NO_EXPORT NO_PROVIDER_CLOUD_NETWORK NO_DB_MIGRATION NO_KMS_WORM_EXTERNAL_TRUST NO_LIVE_AUTOMATION NO_RELEASE_COMMERCIAL";

    public const string CompletedStatus =
        "PRODUCT_LEDGER_LOCAL_APPROVED_NO_OP_EXECUTION_COMPLETED LOCAL_ONLY INTERNAL_ONLY DEVELOPMENT_ONLY DEFAULT_OFF FAIL_CLOSED NO_OP_ONLY NO_COMMAND_EXECUTION NO_PUBLIC_UI_ACTION NO_PRODUCT_COMMAND_HANDLER NO_EXPORT NO_PROVIDER_CLOUD_NETWORK NO_DB_MIGRATION NO_KMS_WORM_EXTERNAL_TRUST NO_LIVE_AUTOMATION NO_RELEASE_COMMERCIAL";

    public const string RejectedStatus =
        "PRODUCT_LEDGER_LOCAL_APPROVED_ACTION_EXECUTION_REJECTED FAIL_CLOSED NO_COMMAND_EXECUTION NO_PUBLIC_UI_ACTION NO_PRODUCT_COMMAND_HANDLER NO_EXPORT NO_PROVIDER_CLOUD_NETWORK NO_DB_MIGRATION NO_KMS_WORM_EXTERNAL_TRUST NO_LIVE_AUTOMATION NO_RELEASE_COMMERCIAL";

    private const string StoreFileName = "product-ledger-local-approved-no-op-execution.json";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    static ProductLedgerLocalApprovedActionNoOpExecutor()
    {
        JsonOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
    }

    private readonly ProductLedgerLocalApprovedActionExecutionStoreOptions options;

    public ProductLedgerLocalApprovedActionNoOpExecutor(
        ProductLedgerLocalApprovedActionExecutionStoreOptions options)
    {
        this.options = options;
    }

    public ProductLedgerLocalApprovedActionExecutionSnapshot ExecuteNoOp(
        ProductLedgerLocalApprovedActionExecutionRequest? request)
    {
        var blockers = ValidateRequest(request);
        if (blockers.Count > 0 || request is null)
        {
            return SnapshotRejected(blockers);
        }

        var envelope = EnvelopeFrom(request);
        var existing = File.Exists(StateFilePath()) ? ReadEnvelope() : null;
        if (existing?.Decision == ProductLedgerLocalApprovedActionExecutionDecision.LoadedLocalOnly)
        {
            if (SameExecution(existing, envelope))
            {
                return existing with
                {
                    Decision = ProductLedgerLocalApprovedActionExecutionDecision.IdempotentReplay,
                    StatusText = CompletedStatus
                };
            }

            return existing with
            {
                Decision = ProductLedgerLocalApprovedActionExecutionDecision.Rejected,
                State = ProductLedgerLocalApprovedActionExecutionState.ExecutionReplayBlocked,
                Blockers = [ProductLedgerLocalApprovedActionExecutionBlocker.ExistingExecutionConflict],
                StatusText = RejectedStatus
            };
        }

        if (existing?.Blockers.Contains(ProductLedgerLocalApprovedActionExecutionBlocker.StoreTamperedOrCorrupt) == true)
        {
            return existing;
        }

        Directory.CreateDirectory(options.StoreRootPath);
        File.WriteAllText(StateFilePath(), JsonSerializer.Serialize(envelope, JsonOptions), Encoding.UTF8);
        return SnapshotFrom(
            envelope,
            ProductLedgerLocalApprovedActionExecutionDecision.NoOpExecutionCompletedLocalOnly,
            []);
    }

    public ProductLedgerLocalApprovedActionExecutionSnapshot Read()
    {
        if (!BoundaryAllowed())
        {
            return SnapshotRejected([ProductLedgerLocalApprovedActionExecutionBlocker.StoreBoundaryRejected]);
        }

        if (!File.Exists(StateFilePath()))
        {
            return ProductLedgerLocalApprovedActionExecutionSnapshot.Pending;
        }

        return ReadEnvelope();
    }

    private IReadOnlyList<ProductLedgerLocalApprovedActionExecutionBlocker> ValidateRequest(
        ProductLedgerLocalApprovedActionExecutionRequest? request)
    {
        var blockers = new List<ProductLedgerLocalApprovedActionExecutionBlocker>();
        if (request is null)
        {
            blockers.Add(ProductLedgerLocalApprovedActionExecutionBlocker.MissingRequest);
            return blockers;
        }

        if (!BoundaryAllowed())
        {
            blockers.Add(ProductLedgerLocalApprovedActionExecutionBlocker.StoreBoundaryRejected);
        }

        if (!request.ExplicitLocalOnlyNoOpExecutionScope)
        {
            blockers.Add(ProductLedgerLocalApprovedActionExecutionBlocker.MissingExplicitLocalOnlyNoOpExecutionScope);
        }

        if (!request.DevelopmentMode)
        {
            blockers.Add(ProductLedgerLocalApprovedActionExecutionBlocker.NonDevelopmentMode);
        }

        if (!request.LocalMode)
        {
            blockers.Add(ProductLedgerLocalApprovedActionExecutionBlocker.NonLocalMode);
        }

        if (!request.InternalMode)
        {
            blockers.Add(ProductLedgerLocalApprovedActionExecutionBlocker.NonInternalMode);
        }

        if (string.IsNullOrWhiteSpace(request.ExecutionId))
        {
            blockers.Add(ProductLedgerLocalApprovedActionExecutionBlocker.MissingExecutionId);
        }

        if (request.CandidateActionKind is null)
        {
            blockers.Add(ProductLedgerLocalApprovedActionExecutionBlocker.MissingCandidateActionKind);
        }

        AddApprovalDecisionBlockers(request, blockers);
        AddEvidenceBlockers(request, blockers);
        AddAuthorityBlockers(request, blockers);
        return blockers.Distinct().OrderBy(blocker => blocker.ToString(), StringComparer.Ordinal).ToArray();
    }

    private static void AddApprovalDecisionBlockers(
        ProductLedgerLocalApprovedActionExecutionRequest request,
        List<ProductLedgerLocalApprovedActionExecutionBlocker> blockers)
    {
        var approval = request.ApprovalDecision;
        if (approval is null)
        {
            blockers.Add(ProductLedgerLocalApprovedActionExecutionBlocker.MissingApprovalDecision);
            return;
        }

        if (approval.State != ProductLedgerLocalApprovalDecisionState.ApprovedLocalOnly
            || !string.Equals(approval.OperatorDecision, ProductLedgerLocalApprovalOperatorDecisionKind.Approve.ToString(), StringComparison.Ordinal))
        {
            blockers.Add(ProductLedgerLocalApprovedActionExecutionBlocker.ApprovalDecisionNotApproved);
        }

        if (approval.Blockers.Count > 0
            || approval.Decision == ProductLedgerLocalApprovalDecisionStoreDecision.Rejected)
        {
            blockers.Add(ProductLedgerLocalApprovedActionExecutionBlocker.ApprovalDecisionHasBlockers);
        }

        if (request.CandidateActionKind is not null
            && !string.Equals(approval.CandidateActionKind, request.CandidateActionKind.Value.ToString(), StringComparison.Ordinal))
        {
            blockers.Add(ProductLedgerLocalApprovedActionExecutionBlocker.CandidateActionMismatch);
        }
    }

    private static void AddEvidenceBlockers(
        ProductLedgerLocalApprovedActionExecutionRequest request,
        List<ProductLedgerLocalApprovedActionExecutionBlocker> blockers)
    {
        if (string.IsNullOrWhiteSpace(request.CandidateEvidenceHash)
            || string.IsNullOrWhiteSpace(request.CurrentEvidenceHash)
            || string.IsNullOrWhiteSpace(request.ApprovalDecision?.CandidateEvidenceHash))
        {
            blockers.Add(ProductLedgerLocalApprovedActionExecutionBlocker.MissingCandidateEvidenceHash);
        }
        else if (!string.Equals(request.CandidateEvidenceHash.Trim(), request.CurrentEvidenceHash.Trim(), StringComparison.Ordinal)
            || !string.Equals(request.CandidateEvidenceHash.Trim(), request.ApprovalDecision.CandidateEvidenceHash, StringComparison.Ordinal))
        {
            blockers.Add(ProductLedgerLocalApprovedActionExecutionBlocker.CandidateEvidenceHashMismatch);
        }

        if (request.EvidenceReferences is null
            || request.EvidenceReferences.Count == 0
            || request.EvidenceReferences.Any(string.IsNullOrWhiteSpace)
            || request.ApprovalDecision?.EvidenceReferences.Count == 0)
        {
            blockers.Add(ProductLedgerLocalApprovedActionExecutionBlocker.MissingEvidenceReferences);
        }
    }

    private static void AddAuthorityBlockers(
        ProductLedgerLocalApprovedActionExecutionRequest request,
        List<ProductLedgerLocalApprovedActionExecutionBlocker> blockers)
    {
        if (request.RequestsBoundedAction)
        {
            blockers.Add(ProductLedgerLocalApprovedActionExecutionBlocker.RequestsBoundedActionWithoutSeparateGate);
        }

        if (request.RequestsPublicUiAction)
        {
            blockers.Add(ProductLedgerLocalApprovedActionExecutionBlocker.RequestsPublicUiAction);
        }

        if (request.RequestsProductCommandExecution)
        {
            blockers.Add(ProductLedgerLocalApprovedActionExecutionBlocker.RequestsProductCommandExecution);
        }

        if (request.RequestsProductCommandHandler)
        {
            blockers.Add(ProductLedgerLocalApprovedActionExecutionBlocker.RequestsProductCommandHandler);
        }

        if (request.RequestsProductiveServiceRegistration)
        {
            blockers.Add(ProductLedgerLocalApprovedActionExecutionBlocker.RequestsProductiveServiceRegistration);
        }

        if (request.RequestsPhysicalExport)
        {
            blockers.Add(ProductLedgerLocalApprovedActionExecutionBlocker.RequestsPhysicalExport);
        }

        if (request.RequestsFileWriteOutsideExecutionStore)
        {
            blockers.Add(ProductLedgerLocalApprovedActionExecutionBlocker.RequestsFileWriteOutsideExecutionStore);
        }

        if (request.ClaimsArbitraryPathInput)
        {
            blockers.Add(ProductLedgerLocalApprovedActionExecutionBlocker.ClaimsArbitraryPathInput);
        }

        if (request.ClaimsFilesystemScan)
        {
            blockers.Add(ProductLedgerLocalApprovedActionExecutionBlocker.ClaimsFilesystemScan);
        }

        if (request.ClaimsProviderCloudNetwork)
        {
            blockers.Add(ProductLedgerLocalApprovedActionExecutionBlocker.ClaimsProviderCloudNetwork);
        }

        if (request.ClaimsDbMigration)
        {
            blockers.Add(ProductLedgerLocalApprovedActionExecutionBlocker.ClaimsDbMigration);
        }

        if (request.ClaimsKmsWormExternalTrust)
        {
            blockers.Add(ProductLedgerLocalApprovedActionExecutionBlocker.ClaimsKmsWormExternalTrust);
        }

        if (request.ClaimsBrowserCdpWcuOcrRecipesLive)
        {
            blockers.Add(ProductLedgerLocalApprovedActionExecutionBlocker.ClaimsBrowserCdpWcuOcrRecipesLive);
        }

        if (request.ClaimsPilotRun)
        {
            blockers.Add(ProductLedgerLocalApprovedActionExecutionBlocker.ClaimsPilotRun);
        }

        if (request.ClaimsReleaseCommercial)
        {
            blockers.Add(ProductLedgerLocalApprovedActionExecutionBlocker.ClaimsReleaseCommercial);
        }
    }

    private ProductLedgerLocalApprovedActionExecutionEnvelope EnvelopeFrom(
        ProductLedgerLocalApprovedActionExecutionRequest request)
    {
        var approval = request.ApprovalDecision!;
        var candidateHash = request.CandidateEvidenceHash!.Trim();
        var evidence = request.EvidenceReferences!
            .Concat(approval.EvidenceReferences)
            .Select(value => value.Trim())
            .Distinct(StringComparer.Ordinal)
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();
        var envelope = new ProductLedgerLocalApprovedActionExecutionEnvelope(
            SchemaVersion: 1,
            State: ProductLedgerLocalApprovedActionExecutionState.NoOpExecutionCompletedLocalOnly,
            ExecutionId: request.ExecutionId!.Trim(),
            ApprovalId: approval.ApprovalId,
            CandidateActionKind: request.CandidateActionKind!.Value.ToString(),
            CandidateEvidenceHash: candidateHash,
            EvidenceReferences: evidence,
            LocalOnly: true,
            InternalOnly: true,
            DevelopmentOnly: true,
            DefaultOff: true,
            FailClosed: true,
            NoOpOnly: true,
            BoundedActionExecuted: false,
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
            ExecutionResultHash: string.Empty);
        return envelope with { ExecutionResultHash = Hash(envelope with { ExecutionResultHash = string.Empty }) };
    }

    private ProductLedgerLocalApprovedActionExecutionSnapshot ReadEnvelope()
    {
        try
        {
            var envelope = JsonSerializer.Deserialize<ProductLedgerLocalApprovedActionExecutionEnvelope>(
                File.ReadAllText(StateFilePath(), Encoding.UTF8),
                JsonOptions);
            if (envelope is null)
            {
                return SnapshotRejected([ProductLedgerLocalApprovedActionExecutionBlocker.StoreTamperedOrCorrupt])
                    with { State = ProductLedgerLocalApprovedActionExecutionState.ExecutionTamperBlocked };
            }

            var actual = Hash(envelope with { ExecutionResultHash = string.Empty });
            return string.Equals(actual, envelope.ExecutionResultHash, StringComparison.Ordinal)
                ? SnapshotFrom(envelope, ProductLedgerLocalApprovedActionExecutionDecision.LoadedLocalOnly, [])
                : SnapshotRejected([ProductLedgerLocalApprovedActionExecutionBlocker.StoreTamperedOrCorrupt])
                    with { State = ProductLedgerLocalApprovedActionExecutionState.ExecutionTamperBlocked };
        }
        catch (JsonException)
        {
            return SnapshotRejected([ProductLedgerLocalApprovedActionExecutionBlocker.StoreTamperedOrCorrupt])
                with { State = ProductLedgerLocalApprovedActionExecutionState.ExecutionTamperBlocked };
        }
        catch (IOException)
        {
            return SnapshotRejected([ProductLedgerLocalApprovedActionExecutionBlocker.StoreTamperedOrCorrupt])
                with { State = ProductLedgerLocalApprovedActionExecutionState.ExecutionTamperBlocked };
        }
        catch (UnauthorizedAccessException)
        {
            return SnapshotRejected([ProductLedgerLocalApprovedActionExecutionBlocker.StoreTamperedOrCorrupt])
                with { State = ProductLedgerLocalApprovedActionExecutionState.ExecutionTamperBlocked };
        }
    }

    private static bool SameExecution(
        ProductLedgerLocalApprovedActionExecutionSnapshot existing,
        ProductLedgerLocalApprovedActionExecutionEnvelope envelope) =>
        string.Equals(existing.ExecutionId, envelope.ExecutionId, StringComparison.Ordinal)
        && string.Equals(existing.ApprovalId, envelope.ApprovalId, StringComparison.Ordinal)
        && string.Equals(existing.CandidateActionKind, envelope.CandidateActionKind, StringComparison.Ordinal)
        && string.Equals(existing.CandidateEvidenceHash, envelope.CandidateEvidenceHash, StringComparison.Ordinal)
        && string.Equals(existing.ExecutionResultHashPrefix, Prefix(envelope.ExecutionResultHash), StringComparison.Ordinal);

    private static ProductLedgerLocalApprovedActionExecutionSnapshot SnapshotFrom(
        ProductLedgerLocalApprovedActionExecutionEnvelope envelope,
        ProductLedgerLocalApprovedActionExecutionDecision decision,
        IReadOnlyList<ProductLedgerLocalApprovedActionExecutionBlocker> blockers) =>
        new(
            Decision: blockers.Count == 0 ? decision : ProductLedgerLocalApprovedActionExecutionDecision.Rejected,
            State: blockers.Count == 0 ? envelope.State : ProductLedgerLocalApprovedActionExecutionState.ExecutionBlocked,
            Blockers: blockers.OrderBy(blocker => blocker.ToString(), StringComparer.Ordinal).ToArray(),
            ExecutionId: envelope.ExecutionId,
            ApprovalId: envelope.ApprovalId,
            CandidateActionKind: envelope.CandidateActionKind,
            CandidateEvidenceHash: envelope.CandidateEvidenceHash,
            CandidateEvidenceHashPrefix: Prefix(envelope.CandidateEvidenceHash),
            ExecutionResultHashPrefix: Prefix(envelope.ExecutionResultHash),
            EvidenceReferences: envelope.EvidenceReferences,
            LocalOnly: envelope.LocalOnly,
            InternalOnly: envelope.InternalOnly,
            DevelopmentOnly: envelope.DevelopmentOnly,
            DefaultOff: envelope.DefaultOff,
            FailClosed: envelope.FailClosed,
            NoOpOnly: envelope.NoOpOnly,
            BoundedActionExecuted: envelope.BoundedActionExecuted,
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

    private static ProductLedgerLocalApprovedActionExecutionSnapshot SnapshotRejected(
        IReadOnlyList<ProductLedgerLocalApprovedActionExecutionBlocker> blockers) =>
        ProductLedgerLocalApprovedActionExecutionSnapshot.Pending with
        {
            Decision = ProductLedgerLocalApprovedActionExecutionDecision.Rejected,
            State = ProductLedgerLocalApprovedActionExecutionState.ExecutionBlocked,
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

    private static string Hash(ProductLedgerLocalApprovedActionExecutionEnvelope envelope)
    {
        var json = JsonSerializer.Serialize(envelope, JsonOptions);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json))).ToLowerInvariant();
    }

    private static string Prefix(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "none" : value[..Math.Min(12, value.Length)];

    private sealed record ProductLedgerLocalApprovedActionExecutionEnvelope(
        int SchemaVersion,
        ProductLedgerLocalApprovedActionExecutionState State,
        string ExecutionId,
        string ApprovalId,
        string CandidateActionKind,
        string CandidateEvidenceHash,
        IReadOnlyList<string> EvidenceReferences,
        bool LocalOnly,
        bool InternalOnly,
        bool DevelopmentOnly,
        bool DefaultOff,
        bool FailClosed,
        bool NoOpOnly,
        bool BoundedActionExecuted,
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
        string ExecutionResultHash);
}
