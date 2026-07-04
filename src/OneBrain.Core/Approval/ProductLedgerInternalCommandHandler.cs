namespace OneBrain.Core.Approval;

public enum ProductLedgerInternalCommandDecision
{
    Rejected,
    CompletedReadOnlyInMemory
}

public enum ProductLedgerInternalCommandBlocker
{
    MissingRequest,
    MissingExplicitInternalLocalOnlyNonDestructiveScope,
    MissingRouterPreview,
    RouterPreviewRejected,
    CommandNotAllowedForInternalHandler,
    ExecutablePreviewRejected,
    ProductiveCommandIdRejected,
    HandlerOrCallbackRejected,
    InvalidPreviewCommandId,
    PublicUiActionRequested,
    DestructiveActionRequested,
    ProductCommandHandlerRequested,
    ProductiveServiceRegistrationRequested,
    ProviderCloudNetworkClaimed,
    DbMigrationClaimed,
    KmsWormExternalTrustClaimed,
    BrowserCdpWcuOcrRecipesLiveClaimed,
    ReleaseCommercialClaimed,
    ExternalTelemetryOrSyncClaimed,
    BillingLicensingCloudClaimed,
    PhysicalExportRequested,
    FileWriteRequested,
    AppendOutsideBoundedWriterClaimed
}

public sealed record ProductLedgerInternalCommandRequest(
    bool ExplicitInternalLocalOnlyNonDestructiveScope,
    ProductLedgerInternalCommandPreviewResult? RouterPreview,
    bool RequestsPublicUiAction,
    bool RequestsDestructiveAction,
    bool RequestsProductCommandHandler,
    bool RequestsProductiveServiceRegistration,
    bool ClaimsProviderCloudNetwork,
    bool ClaimsDbMigration,
    bool ClaimsKmsWormExternalTrust,
    bool ClaimsBrowserCdpWcuOcrRecipesLive,
    bool ClaimsReleaseCommercial,
    bool ClaimsExternalTelemetryOrSync,
    bool ClaimsBillingLicensingCloud,
    bool RequestsPhysicalExport,
    bool RequestsFileWrite,
    bool ClaimsAppendOutsideBoundedWriter);

public sealed record ProductLedgerInternalCommandExecutionPreview(
    ProductLedgerInternalCommandKind CommandKind,
    string CommandId,
    string ResultKind,
    IReadOnlyList<string> Lines,
    IReadOnlyList<string> RequiredEvidence,
    bool InMemoryOnly,
    bool PhysicalExportCreated,
    bool FileWritePerformed,
    bool ExecutableCallbackInvoked);

public sealed record ProductLedgerInternalCommandResult(
    ProductLedgerInternalCommandDecision Decision,
    IReadOnlyList<ProductLedgerInternalCommandBlocker> Blockers,
    ProductLedgerInternalCommandExecutionPreview ExecutionPreview,
    bool InternalOnly,
    bool LocalOnly,
    bool NonDestructive,
    bool ReadOnlyOrInMemory,
    bool FailClosed,
    bool PublicCommandExposureAvailable,
    bool PublicUiActionAvailable,
    bool DestructiveActionAvailable,
    bool ProductCommandHandlerAvailable,
    bool ProductiveServiceRegistrationAvailable,
    bool ProviderCloudNetworkAvailable,
    bool DbMigrationAvailable,
    bool KmsWormExternalTrustAvailable,
    bool BrowserCdpWcuOcrRecipesLiveAvailable,
    bool ExternalTelemetryOrSyncAvailable,
    bool BillingLicensingCloudAvailable,
    bool ReleaseCommercialReady,
    bool PhysicalExportCreated,
    bool FileWritePerformed,
    string StatusText);

public sealed class ProductLedgerInternalCommandHandler
{
    public const string ReadyStatus =
        "PRODUCT_LEDGER_INTERNAL_COMMAND_HANDLER_LOCAL_ONLY_NON_DESTRUCTIVE_READY INTERNAL_LOCAL_ONLY NON_DESTRUCTIVE READ_ONLY_OR_IN_MEMORY NO_PUBLIC_UI_ACTION NO_PRODUCT_COMMAND_HANDLER_EXPOSURE NO_PROVIDER_CLOUD_NETWORK NO_DB_MIGRATION NO_KMS_WORM_EXTERNAL_TRUST NO_LIVE_AUTOMATION NO_RELEASE_COMMERCIAL";

    public const string RejectedStatus =
        "PRODUCT_LEDGER_INTERNAL_COMMAND_HANDLER_LOCAL_ONLY_NON_DESTRUCTIVE_REJECTED FAIL_CLOSED NO_PUBLIC_UI_ACTION NO_PRODUCT_COMMAND_HANDLER_EXPOSURE NO_PROVIDER_CLOUD_NETWORK NO_DB_MIGRATION NO_KMS_WORM_EXTERNAL_TRUST NO_LIVE_AUTOMATION NO_RELEASE_COMMERCIAL";

    private static readonly ProductLedgerInternalCommandKind[] AllowedCommands =
    [
        ProductLedgerInternalCommandKind.ViewDiagnostics,
        ProductLedgerInternalCommandKind.ViewLedgerReadiness,
        ProductLedgerInternalCommandKind.ViewRuntimeGateStatus,
        ProductLedgerInternalCommandKind.ViewCheckpointHeadStatus,
        ProductLedgerInternalCommandKind.ViewEvidenceGates,
        ProductLedgerInternalCommandKind.StaticScanPreview,
        ProductLedgerInternalCommandKind.RequestExternalAuditPreview,
        ProductLedgerInternalCommandKind.LocalReportPreviewInMemory
    ];

    public ProductLedgerInternalCommandResult Execute(ProductLedgerInternalCommandRequest? request)
    {
        var blockers = new List<ProductLedgerInternalCommandBlocker>();
        if (request is null)
        {
            blockers.Add(ProductLedgerInternalCommandBlocker.MissingRequest);
            return Result(blockers, null);
        }

        AddRequestBlockers(request, blockers);
        AddRouterPreviewBlockers(request.RouterPreview, blockers);
        return Result(blockers, request.RouterPreview);
    }

    private static void AddRequestBlockers(
        ProductLedgerInternalCommandRequest request,
        List<ProductLedgerInternalCommandBlocker> blockers)
    {
        if (!request.ExplicitInternalLocalOnlyNonDestructiveScope)
        {
            blockers.Add(ProductLedgerInternalCommandBlocker.MissingExplicitInternalLocalOnlyNonDestructiveScope);
        }

        if (request.RequestsPublicUiAction)
        {
            blockers.Add(ProductLedgerInternalCommandBlocker.PublicUiActionRequested);
        }

        if (request.RequestsDestructiveAction)
        {
            blockers.Add(ProductLedgerInternalCommandBlocker.DestructiveActionRequested);
        }

        if (request.RequestsProductCommandHandler)
        {
            blockers.Add(ProductLedgerInternalCommandBlocker.ProductCommandHandlerRequested);
        }

        if (request.RequestsProductiveServiceRegistration)
        {
            blockers.Add(ProductLedgerInternalCommandBlocker.ProductiveServiceRegistrationRequested);
        }

        if (request.ClaimsProviderCloudNetwork)
        {
            blockers.Add(ProductLedgerInternalCommandBlocker.ProviderCloudNetworkClaimed);
        }

        if (request.ClaimsDbMigration)
        {
            blockers.Add(ProductLedgerInternalCommandBlocker.DbMigrationClaimed);
        }

        if (request.ClaimsKmsWormExternalTrust)
        {
            blockers.Add(ProductLedgerInternalCommandBlocker.KmsWormExternalTrustClaimed);
        }

        if (request.ClaimsBrowserCdpWcuOcrRecipesLive)
        {
            blockers.Add(ProductLedgerInternalCommandBlocker.BrowserCdpWcuOcrRecipesLiveClaimed);
        }

        if (request.ClaimsReleaseCommercial)
        {
            blockers.Add(ProductLedgerInternalCommandBlocker.ReleaseCommercialClaimed);
        }

        if (request.ClaimsExternalTelemetryOrSync)
        {
            blockers.Add(ProductLedgerInternalCommandBlocker.ExternalTelemetryOrSyncClaimed);
        }

        if (request.ClaimsBillingLicensingCloud)
        {
            blockers.Add(ProductLedgerInternalCommandBlocker.BillingLicensingCloudClaimed);
        }

        if (request.RequestsPhysicalExport)
        {
            blockers.Add(ProductLedgerInternalCommandBlocker.PhysicalExportRequested);
        }

        if (request.RequestsFileWrite)
        {
            blockers.Add(ProductLedgerInternalCommandBlocker.FileWriteRequested);
        }

        if (request.ClaimsAppendOutsideBoundedWriter)
        {
            blockers.Add(ProductLedgerInternalCommandBlocker.AppendOutsideBoundedWriterClaimed);
        }
    }

    private static void AddRouterPreviewBlockers(
        ProductLedgerInternalCommandPreviewResult? routerPreview,
        List<ProductLedgerInternalCommandBlocker> blockers)
    {
        if (routerPreview is null)
        {
            blockers.Add(ProductLedgerInternalCommandBlocker.MissingRouterPreview);
            return;
        }

        if (routerPreview.Decision != ProductLedgerInternalCommandPreviewDecision.PreviewedNoOpReadOnly
            || routerPreview.Blockers.Count > 0
            || !routerPreview.LocalOnly
            || !routerPreview.InternalOnly
            || !routerPreview.NoOp
            || !routerPreview.ReadOnly
            || !routerPreview.NonDestructive
            || routerPreview.PublicUiActionAvailable
            || routerPreview.DestructiveActionAvailable
            || routerPreview.ProductCommandHandlerAvailable
            || routerPreview.ProductiveServiceRegistrationAvailable
            || routerPreview.ProviderCloudNetworkAvailable
            || routerPreview.DbMigrationAvailable
            || routerPreview.KmsWormExternalTrustAvailable
            || routerPreview.BrowserCdpWcuOcrRecipesLiveAvailable
            || routerPreview.ExternalTelemetryOrSyncAvailable
            || routerPreview.BillingLicensingCloudAvailable
            || routerPreview.ReleaseCommercialReady)
        {
            blockers.Add(ProductLedgerInternalCommandBlocker.RouterPreviewRejected);
        }

        if (!AllowedCommands.Contains(routerPreview.Preview.CommandKind))
        {
            blockers.Add(ProductLedgerInternalCommandBlocker.CommandNotAllowedForInternalHandler);
        }

        if (!routerPreview.Preview.Disabled || routerPreview.Preview.Executable)
        {
            blockers.Add(ProductLedgerInternalCommandBlocker.ExecutablePreviewRejected);
        }

        if (routerPreview.Preview.ProductiveCommandId is not null)
        {
            blockers.Add(ProductLedgerInternalCommandBlocker.ProductiveCommandIdRejected);
        }

        if (routerPreview.Preview.HandlerId is not null || routerPreview.Preview.CallbackName is not null)
        {
            blockers.Add(ProductLedgerInternalCommandBlocker.HandlerOrCallbackRejected);
        }

        if (!routerPreview.Preview.CommandId.StartsWith("preview-only.product-ledger.", StringComparison.Ordinal))
        {
            blockers.Add(ProductLedgerInternalCommandBlocker.InvalidPreviewCommandId);
        }
    }

    private static ProductLedgerInternalCommandResult Result(
        IReadOnlyList<ProductLedgerInternalCommandBlocker> blockers,
        ProductLedgerInternalCommandPreviewResult? routerPreview)
    {
        var distinct = blockers.Distinct().OrderBy(blocker => blocker.ToString(), StringComparer.Ordinal).ToArray();
        var completed = distinct.Length == 0 && routerPreview is not null;
        return new ProductLedgerInternalCommandResult(
            Decision: completed
                ? ProductLedgerInternalCommandDecision.CompletedReadOnlyInMemory
                : ProductLedgerInternalCommandDecision.Rejected,
            Blockers: distinct,
            ExecutionPreview: CreateExecutionPreview(routerPreview, completed),
            InternalOnly: true,
            LocalOnly: true,
            NonDestructive: true,
            ReadOnlyOrInMemory: true,
            FailClosed: true,
            PublicCommandExposureAvailable: false,
            PublicUiActionAvailable: false,
            DestructiveActionAvailable: false,
            ProductCommandHandlerAvailable: false,
            ProductiveServiceRegistrationAvailable: false,
            ProviderCloudNetworkAvailable: false,
            DbMigrationAvailable: false,
            KmsWormExternalTrustAvailable: false,
            BrowserCdpWcuOcrRecipesLiveAvailable: false,
            ExternalTelemetryOrSyncAvailable: false,
            BillingLicensingCloudAvailable: false,
            ReleaseCommercialReady: false,
            PhysicalExportCreated: false,
            FileWritePerformed: false,
            StatusText: completed ? ReadyStatus : RejectedStatus);
    }

    private static ProductLedgerInternalCommandExecutionPreview CreateExecutionPreview(
        ProductLedgerInternalCommandPreviewResult? routerPreview,
        bool completed)
    {
        var command = routerPreview?.Preview.CommandKind ?? ProductLedgerInternalCommandKind.ViewDiagnostics;
        return new ProductLedgerInternalCommandExecutionPreview(
            CommandKind: command,
            CommandId: routerPreview?.Preview.CommandId ?? "preview-only.product-ledger.rejected",
            ResultKind: completed ? ResultKind(command) : "BLOCKED_FAIL_CLOSED",
            Lines: completed ? Lines(command) : ["Command rejected before execution authority."],
            RequiredEvidence: routerPreview?.Preview.RequiredEvidence ?? ["router preview", "safety tests"],
            InMemoryOnly: true,
            PhysicalExportCreated: false,
            FileWritePerformed: false,
            ExecutableCallbackInvoked: false);
    }

    private static string ResultKind(ProductLedgerInternalCommandKind command) =>
        command switch
        {
            ProductLedgerInternalCommandKind.ViewDiagnostics => "DIAGNOSTICS_READ_ONLY_IN_MEMORY",
            ProductLedgerInternalCommandKind.ViewLedgerReadiness => "LEDGER_READINESS_READ_ONLY_IN_MEMORY",
            ProductLedgerInternalCommandKind.ViewRuntimeGateStatus => "RUNTIME_GATE_STATUS_READ_ONLY_IN_MEMORY",
            ProductLedgerInternalCommandKind.ViewCheckpointHeadStatus => "CHECKPOINT_HEAD_STATUS_READ_ONLY_IN_MEMORY",
            ProductLedgerInternalCommandKind.ViewEvidenceGates => "EVIDENCE_GATES_READ_ONLY_IN_MEMORY",
            ProductLedgerInternalCommandKind.StaticScanPreview => "STATIC_SCAN_PREVIEW_IN_MEMORY",
            ProductLedgerInternalCommandKind.RequestExternalAuditPreview => "EXTERNAL_AUDIT_REQUEST_PREVIEW_IN_MEMORY",
            ProductLedgerInternalCommandKind.LocalReportPreviewInMemory => "LOCAL_REPORT_PREVIEW_IN_MEMORY_NO_FILE",
            _ => "BLOCKED_FAIL_CLOSED"
        };

    private static IReadOnlyList<string> Lines(ProductLedgerInternalCommandKind command) =>
        command switch
        {
            ProductLedgerInternalCommandKind.ViewDiagnostics => ["INTERNAL_LOCAL_ONLY", "NON_DESTRUCTIVE", "READ_ONLY_OR_IN_MEMORY", "NO_PUBLIC_UI_ACTION"],
            ProductLedgerInternalCommandKind.ViewLedgerReadiness => ["Product Ledger local-only readiness preview.", "No writer execution occurs."],
            ProductLedgerInternalCommandKind.ViewRuntimeGateStatus => ["Runtime local-only gate preview.", "Default-off boundary remains in force."],
            ProductLedgerInternalCommandKind.ViewCheckpointHeadStatus => ["Checkpoint/head status preview.", "Same-boundary local trust limitation remains explicit."],
            ProductLedgerInternalCommandKind.ViewEvidenceGates => ["Evidence gates preview.", "Redaction, retention, authority, failure/replay and rollback remain required."],
            ProductLedgerInternalCommandKind.StaticScanPreview => ["Static scan preview.", "No scan process is launched by this handler."],
            ProductLedgerInternalCommandKind.RequestExternalAuditPreview => ["External audit request preview.", "No external model or service is contacted."],
            ProductLedgerInternalCommandKind.LocalReportPreviewInMemory => ["Local report preview in memory.", "No physical export or file write occurs."],
            _ => ["Command rejected before execution authority."]
        };
}
