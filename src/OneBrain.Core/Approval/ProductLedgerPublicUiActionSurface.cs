namespace OneBrain.Core.Approval;

public enum ProductLedgerPublicUiActionDecision
{
    Rejected,
    CompletedLocalOnlyNonDestructive
}

public enum ProductLedgerPublicUiActionKind
{
    ViewDiagnostics,
    ViewLedgerReadiness,
    ViewRuntimeGateStatus,
    ViewCheckpointHeadStatus,
    ViewEvidenceGates,
    StaticScanPreview,
    RequestExternalAuditPreview,
    LocalReportPhysicalExportBoundedInternal,
    EnablePublicUi,
    ExecuteAction,
    DestructiveWrite,
    RegisterCommandHandler,
    RegisterProductDI,
    ConnectProvider,
    EnableCloud,
    RunMigration,
    EnableKms,
    EnableWorm,
    EnableExternalTrust,
    RunBrowserCdp,
    RunWcu,
    RunOcr,
    RunRecipesLive,
    Release,
    CommercialLaunch,
    SyncExternal,
    TelemetryExternal,
    BillingLicensingCloud,
    UnboundedExport,
    ExternalExport,
    Delete,
    OverwriteUnsafe
}

public enum ProductLedgerPublicUiActionBlocker
{
    MissingRequest,
    MissingExplicitPublicLocalOnlyNonDestructiveScope,
    MissingPublicReadOnlyDisabledPreview,
    UnsafePublicReadOnlyDisabledPreview,
    CorruptAction,
    UnknownAction,
    ActionNotAllowed,
    RouterPreviewRejected,
    CommandHandlerRejected,
    MissingBoundedLocalReportExportRequest,
    RawPayloadOrSecretClaimed,
    PublicDestructiveActionRequested,
    GenericExecuteActionRequested,
    ProductiveServiceRegistrationRequested,
    ProviderCloudNetworkClaimed,
    DbMigrationClaimed,
    KmsWormExternalTrustClaimed,
    BrowserCdpWcuOcrRecipesLiveClaimed,
    ReleaseCommercialClaimed,
    ExternalTelemetryOrSyncClaimed,
    BillingLicensingCloudClaimed,
    UnboundedPhysicalExportClaimed,
    ExternalCloudExportClaimed,
    DeleteOrUnsafeOverwriteRequested
}

public sealed record ProductLedgerPublicUiActionRequest(
    bool ExplicitPublicLocalOnlyNonDestructiveScope,
    ProductLedgerPublicUiReadOnlyDisabledPreviewResult? PublicReadOnlyDisabledPreview,
    ProductLedgerPublicUiActionKind? ActionKind,
    string? RawActionName,
    ProductLedgerLocalReportExportRequest? LocalReportExportRequest,
    bool ClaimsRawPayloadOrSecret,
    bool RequestsDestructiveAction,
    bool RequestsGenericExecuteAction,
    bool RequestsProductiveServiceRegistration,
    bool ClaimsProviderCloudNetwork,
    bool ClaimsDbMigration,
    bool ClaimsKmsWormExternalTrust,
    bool ClaimsBrowserCdpWcuOcrRecipesLive,
    bool ClaimsReleaseCommercial,
    bool ClaimsExternalTelemetryOrSync,
    bool ClaimsBillingLicensingCloud,
    bool ClaimsUnboundedPhysicalExport,
    bool ClaimsExternalCloudExport,
    bool RequestsDeleteOrUnsafeOverwrite);

public sealed record ProductLedgerPublicUiActionButton(
    ProductLedgerPublicUiActionKind ActionKind,
    string Label,
    string RiskLabel,
    IReadOnlyList<string> RequiredEvidence,
    bool Enabled,
    bool LocalOnly,
    bool NonDestructive,
    bool Bounded,
    string DisabledReason);

public sealed record ProductLedgerPublicUiActionResult(
    ProductLedgerPublicUiActionDecision Decision,
    IReadOnlyList<ProductLedgerPublicUiActionBlocker> Blockers,
    ProductLedgerPublicUiActionKind ActionKind,
    IReadOnlyList<ProductLedgerPublicUiActionButton> Buttons,
    ProductLedgerInternalCommandPreviewResult? RouterPreview,
    ProductLedgerInternalCommandResult? CommandResult,
    bool LocalOnly,
    bool NonDestructive,
    bool Bounded,
    bool FailClosed,
    bool PublicUiActionCompleted,
    bool LocalOnlyCommandHandlerInvoked,
    bool DestructiveActionAvailable,
    bool UnboundedPhysicalExportAvailable,
    bool ExternalCloudExportAvailable,
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

public sealed class ProductLedgerPublicUiActionSurface
{
    public const string ReadyStatus =
        "PRODUCT_LEDGER_PUBLIC_UI_ACTIONS_COMMAND_HANDLER_LOCAL_ONLY_NON_DESTRUCTIVE_READY LOCAL_ONLY NON_DESTRUCTIVE BOUNDED FAIL_CLOSED ROUTER_HANDLER_MEDIATED NO_DESTRUCTIVE_ACTION NO_UNBOUNDED_EXPORT NO_EXTERNAL_CLOUD_EXPORT NO_PROVIDER_CLOUD_NETWORK NO_DB_MIGRATION NO_KMS_WORM_EXTERNAL_TRUST NO_LIVE_AUTOMATION NO_RELEASE_COMMERCIAL";

    public const string RejectedStatus =
        "PRODUCT_LEDGER_PUBLIC_UI_ACTIONS_COMMAND_HANDLER_LOCAL_ONLY_NON_DESTRUCTIVE_REJECTED FAIL_CLOSED NO_DESTRUCTIVE_ACTION NO_UNBOUNDED_EXPORT NO_EXTERNAL_CLOUD_EXPORT NO_PROVIDER_CLOUD_NETWORK NO_DB_MIGRATION NO_KMS_WORM_EXTERNAL_TRUST NO_LIVE_AUTOMATION NO_RELEASE_COMMERCIAL";

    private static readonly ProductLedgerPublicUiActionKind[] AllowedActions =
    [
        ProductLedgerPublicUiActionKind.ViewDiagnostics,
        ProductLedgerPublicUiActionKind.ViewLedgerReadiness,
        ProductLedgerPublicUiActionKind.ViewRuntimeGateStatus,
        ProductLedgerPublicUiActionKind.ViewCheckpointHeadStatus,
        ProductLedgerPublicUiActionKind.ViewEvidenceGates,
        ProductLedgerPublicUiActionKind.StaticScanPreview,
        ProductLedgerPublicUiActionKind.RequestExternalAuditPreview,
        ProductLedgerPublicUiActionKind.LocalReportPhysicalExportBoundedInternal
    ];

    public ProductLedgerPublicUiActionResult Execute(ProductLedgerPublicUiActionRequest? request)
    {
        var blockers = new List<ProductLedgerPublicUiActionBlocker>();
        if (request is null)
        {
            blockers.Add(ProductLedgerPublicUiActionBlocker.MissingRequest);
            return Result(blockers, ProductLedgerPublicUiActionKind.ViewDiagnostics, null, null);
        }

        AddRequestBlockers(request, blockers);
        AddPreviewBlockers(request.PublicReadOnlyDisabledPreview, blockers);
        var action = ResolveAction(request, blockers);
        AddActionBlockers(action, request, blockers);

        ProductLedgerInternalCommandPreviewResult? routerPreview = null;
        ProductLedgerInternalCommandResult? commandResult = null;
        if (blockers.Count == 0)
        {
            routerPreview = new ProductLedgerInternalCommandPreviewRouter().Preview(RouterRequest(Map(action)));
            if (routerPreview.Decision != ProductLedgerInternalCommandPreviewDecision.PreviewedNoOpReadOnly
                || routerPreview.Blockers.Count > 0)
            {
                blockers.Add(ProductLedgerPublicUiActionBlocker.RouterPreviewRejected);
            }
        }

        if (blockers.Count == 0 && routerPreview is not null)
        {
            var commandRequest = CommandRequest(routerPreview, request.LocalReportExportRequest);
            commandResult = new ProductLedgerInternalCommandHandler().Execute(commandRequest);
            if (commandResult.Decision == ProductLedgerInternalCommandDecision.Rejected
                || commandResult.Blockers.Count > 0
                || !commandResult.LocalOnly
                || !commandResult.NonDestructive
                || !commandResult.FailClosed
                || commandResult.DestructiveActionAvailable
                || commandResult.ProviderCloudNetworkAvailable
                || commandResult.DbMigrationAvailable
                || commandResult.KmsWormExternalTrustAvailable
                || commandResult.BrowserCdpWcuOcrRecipesLiveAvailable
                || commandResult.ExternalTelemetryOrSyncAvailable
                || commandResult.BillingLicensingCloudAvailable
                || commandResult.ReleaseCommercialReady)
            {
                blockers.Add(ProductLedgerPublicUiActionBlocker.CommandHandlerRejected);
            }
        }

        return Result(blockers, action, routerPreview, commandResult);
    }

    public IReadOnlyList<ProductLedgerPublicUiActionButton> Buttons() =>
    [
        .. AllowedActions.Select(action => Button(action, enabled: true)),
        Button(ProductLedgerPublicUiActionKind.ExecuteAction, enabled: false),
        Button(ProductLedgerPublicUiActionKind.DestructiveWrite, enabled: false),
        Button(ProductLedgerPublicUiActionKind.ConnectProvider, enabled: false),
        Button(ProductLedgerPublicUiActionKind.RunMigration, enabled: false),
        Button(ProductLedgerPublicUiActionKind.EnableKms, enabled: false),
        Button(ProductLedgerPublicUiActionKind.RunBrowserCdp, enabled: false),
        Button(ProductLedgerPublicUiActionKind.Release, enabled: false),
        Button(ProductLedgerPublicUiActionKind.UnboundedExport, enabled: false),
        Button(ProductLedgerPublicUiActionKind.ExternalExport, enabled: false)
    ];

    private static void AddRequestBlockers(
        ProductLedgerPublicUiActionRequest request,
        List<ProductLedgerPublicUiActionBlocker> blockers)
    {
        if (!request.ExplicitPublicLocalOnlyNonDestructiveScope)
        {
            blockers.Add(ProductLedgerPublicUiActionBlocker.MissingExplicitPublicLocalOnlyNonDestructiveScope);
        }

        if (request.ClaimsRawPayloadOrSecret)
        {
            blockers.Add(ProductLedgerPublicUiActionBlocker.RawPayloadOrSecretClaimed);
        }

        if (request.RequestsDestructiveAction)
        {
            blockers.Add(ProductLedgerPublicUiActionBlocker.PublicDestructiveActionRequested);
        }

        if (request.RequestsGenericExecuteAction)
        {
            blockers.Add(ProductLedgerPublicUiActionBlocker.GenericExecuteActionRequested);
        }

        if (request.RequestsProductiveServiceRegistration)
        {
            blockers.Add(ProductLedgerPublicUiActionBlocker.ProductiveServiceRegistrationRequested);
        }

        if (request.ClaimsProviderCloudNetwork)
        {
            blockers.Add(ProductLedgerPublicUiActionBlocker.ProviderCloudNetworkClaimed);
        }

        if (request.ClaimsDbMigration)
        {
            blockers.Add(ProductLedgerPublicUiActionBlocker.DbMigrationClaimed);
        }

        if (request.ClaimsKmsWormExternalTrust)
        {
            blockers.Add(ProductLedgerPublicUiActionBlocker.KmsWormExternalTrustClaimed);
        }

        if (request.ClaimsBrowserCdpWcuOcrRecipesLive)
        {
            blockers.Add(ProductLedgerPublicUiActionBlocker.BrowserCdpWcuOcrRecipesLiveClaimed);
        }

        if (request.ClaimsReleaseCommercial)
        {
            blockers.Add(ProductLedgerPublicUiActionBlocker.ReleaseCommercialClaimed);
        }

        if (request.ClaimsExternalTelemetryOrSync)
        {
            blockers.Add(ProductLedgerPublicUiActionBlocker.ExternalTelemetryOrSyncClaimed);
        }

        if (request.ClaimsBillingLicensingCloud)
        {
            blockers.Add(ProductLedgerPublicUiActionBlocker.BillingLicensingCloudClaimed);
        }

        if (request.ClaimsUnboundedPhysicalExport)
        {
            blockers.Add(ProductLedgerPublicUiActionBlocker.UnboundedPhysicalExportClaimed);
        }

        if (request.ClaimsExternalCloudExport)
        {
            blockers.Add(ProductLedgerPublicUiActionBlocker.ExternalCloudExportClaimed);
        }

        if (request.RequestsDeleteOrUnsafeOverwrite)
        {
            blockers.Add(ProductLedgerPublicUiActionBlocker.DeleteOrUnsafeOverwriteRequested);
        }
    }

    private static void AddPreviewBlockers(
        ProductLedgerPublicUiReadOnlyDisabledPreviewResult? preview,
        List<ProductLedgerPublicUiActionBlocker> blockers)
    {
        if (preview is null)
        {
            blockers.Add(ProductLedgerPublicUiActionBlocker.MissingPublicReadOnlyDisabledPreview);
            return;
        }

        var viewModel = preview.ViewModel;
        if (preview.Decision != ProductLedgerPublicUiReadOnlyDisabledPreviewDecision.RenderedReadOnlyDisabledMock
            || preview.Blockers.Count > 0
            || !viewModel.ReadOnly
            || !viewModel.DisabledMockOnly
            || !viewModel.FailClosed
            || viewModel.PublicUiActionAvailable
            || viewModel.DestructiveActionAvailable
            || viewModel.ProductCommandHandlerAvailable
            || viewModel.ProductiveServiceRegistrationAvailable
            || viewModel.ProviderCloudNetworkAvailable
            || viewModel.DbMigrationAvailable
            || viewModel.KmsWormExternalTrustAvailable
            || viewModel.BrowserCdpWcuOcrRecipesLiveAvailable
            || viewModel.ExternalTelemetryOrSyncAvailable
            || viewModel.BillingLicensingCloudAvailable
            || viewModel.ExternalCloudExportAvailable
            || viewModel.UnboundedPhysicalExportAvailable
            || viewModel.RawPayloadOrSecretAvailable
            || viewModel.ReleaseCommercialReady)
        {
            blockers.Add(ProductLedgerPublicUiActionBlocker.UnsafePublicReadOnlyDisabledPreview);
        }
    }

    private static ProductLedgerPublicUiActionKind ResolveAction(
        ProductLedgerPublicUiActionRequest request,
        List<ProductLedgerPublicUiActionBlocker> blockers)
    {
        if (request.ActionKind is null || string.IsNullOrWhiteSpace(request.RawActionName))
        {
            blockers.Add(ProductLedgerPublicUiActionBlocker.CorruptAction);
            return ProductLedgerPublicUiActionKind.ViewDiagnostics;
        }

        if (!Enum.IsDefined(request.ActionKind.Value)
            || !string.Equals(request.RawActionName.Trim(), request.ActionKind.Value.ToString(), StringComparison.Ordinal))
        {
            blockers.Add(ProductLedgerPublicUiActionBlocker.UnknownAction);
            return request.ActionKind.Value;
        }

        return request.ActionKind.Value;
    }

    private static void AddActionBlockers(
        ProductLedgerPublicUiActionKind action,
        ProductLedgerPublicUiActionRequest request,
        List<ProductLedgerPublicUiActionBlocker> blockers)
    {
        if (!AllowedActions.Contains(action))
        {
            AddBlockedActionBlocker(action, blockers);
            return;
        }

        if (action == ProductLedgerPublicUiActionKind.LocalReportPhysicalExportBoundedInternal
            && request.LocalReportExportRequest is null)
        {
            blockers.Add(ProductLedgerPublicUiActionBlocker.MissingBoundedLocalReportExportRequest);
        }
    }

    private static void AddBlockedActionBlocker(
        ProductLedgerPublicUiActionKind action,
        List<ProductLedgerPublicUiActionBlocker> blockers)
    {
        switch (action)
        {
            case ProductLedgerPublicUiActionKind.ExecuteAction:
                blockers.Add(ProductLedgerPublicUiActionBlocker.GenericExecuteActionRequested);
                break;
            case ProductLedgerPublicUiActionKind.DestructiveWrite:
            case ProductLedgerPublicUiActionKind.Delete:
                blockers.Add(ProductLedgerPublicUiActionBlocker.PublicDestructiveActionRequested);
                break;
            case ProductLedgerPublicUiActionKind.RegisterCommandHandler:
            case ProductLedgerPublicUiActionKind.RegisterProductDI:
            case ProductLedgerPublicUiActionKind.EnablePublicUi:
                blockers.Add(ProductLedgerPublicUiActionBlocker.ActionNotAllowed);
                break;
            case ProductLedgerPublicUiActionKind.ConnectProvider:
            case ProductLedgerPublicUiActionKind.EnableCloud:
                blockers.Add(ProductLedgerPublicUiActionBlocker.ProviderCloudNetworkClaimed);
                break;
            case ProductLedgerPublicUiActionKind.RunMigration:
                blockers.Add(ProductLedgerPublicUiActionBlocker.DbMigrationClaimed);
                break;
            case ProductLedgerPublicUiActionKind.EnableKms:
            case ProductLedgerPublicUiActionKind.EnableWorm:
            case ProductLedgerPublicUiActionKind.EnableExternalTrust:
                blockers.Add(ProductLedgerPublicUiActionBlocker.KmsWormExternalTrustClaimed);
                break;
            case ProductLedgerPublicUiActionKind.RunBrowserCdp:
            case ProductLedgerPublicUiActionKind.RunWcu:
            case ProductLedgerPublicUiActionKind.RunOcr:
            case ProductLedgerPublicUiActionKind.RunRecipesLive:
                blockers.Add(ProductLedgerPublicUiActionBlocker.BrowserCdpWcuOcrRecipesLiveClaimed);
                break;
            case ProductLedgerPublicUiActionKind.Release:
            case ProductLedgerPublicUiActionKind.CommercialLaunch:
                blockers.Add(ProductLedgerPublicUiActionBlocker.ReleaseCommercialClaimed);
                break;
            case ProductLedgerPublicUiActionKind.SyncExternal:
            case ProductLedgerPublicUiActionKind.TelemetryExternal:
                blockers.Add(ProductLedgerPublicUiActionBlocker.ExternalTelemetryOrSyncClaimed);
                break;
            case ProductLedgerPublicUiActionKind.BillingLicensingCloud:
                blockers.Add(ProductLedgerPublicUiActionBlocker.BillingLicensingCloudClaimed);
                break;
            case ProductLedgerPublicUiActionKind.UnboundedExport:
            case ProductLedgerPublicUiActionKind.OverwriteUnsafe:
                blockers.Add(ProductLedgerPublicUiActionBlocker.UnboundedPhysicalExportClaimed);
                break;
            case ProductLedgerPublicUiActionKind.ExternalExport:
                blockers.Add(ProductLedgerPublicUiActionBlocker.ExternalCloudExportClaimed);
                break;
        }
    }

    private static ProductLedgerInternalCommandPreviewRequest RouterRequest(ProductLedgerInternalCommandKind command) =>
        new(
            ExplicitInternalLocalOnlyNoOpReadOnlyScope: true,
            CommandKind: command,
            RawCommandName: command.ToString(),
            SourcePreview: null,
            RequestsPublicUiAction: false,
            RequestsDestructiveAction: false,
            RequestsProductCommandHandler: false,
            RequestsProductiveServiceRegistration: false,
            ClaimsProviderCloudNetwork: false,
            ClaimsDbMigration: false,
            ClaimsKmsWormExternalTrust: false,
            ClaimsBrowserCdpWcuOcrRecipesLive: false,
            ClaimsReleaseCommercial: false,
            ClaimsExternalTelemetryOrSync: false,
            ClaimsBillingLicensingCloud: false,
            ClaimsWriterExecutionOutsideValidatedLocalOnlyPolicy: false);

    private static ProductLedgerInternalCommandRequest CommandRequest(
        ProductLedgerInternalCommandPreviewResult routerPreview,
        ProductLedgerLocalReportExportRequest? exportRequest)
    {
        var boundedExport =
            routerPreview.Preview.CommandKind == ProductLedgerInternalCommandKind.LocalReportPhysicalExportBoundedInternal;
        return new ProductLedgerInternalCommandRequest(
            ExplicitInternalLocalOnlyNonDestructiveScope: true,
            RouterPreview: routerPreview,
            RequestsPublicUiAction: false,
            RequestsDestructiveAction: false,
            RequestsProductCommandHandler: false,
            RequestsProductiveServiceRegistration: false,
            ClaimsProviderCloudNetwork: false,
            ClaimsDbMigration: false,
            ClaimsKmsWormExternalTrust: false,
            ClaimsBrowserCdpWcuOcrRecipesLive: false,
            ClaimsReleaseCommercial: false,
            ClaimsExternalTelemetryOrSync: false,
            ClaimsBillingLicensingCloud: false,
            RequestsPhysicalExport: boundedExport,
            RequestsFileWrite: boundedExport,
            ClaimsAppendOutsideBoundedWriter: false,
            LocalReportExportRequest: exportRequest);
    }

    private static ProductLedgerInternalCommandKind Map(ProductLedgerPublicUiActionKind action) =>
        action switch
        {
            ProductLedgerPublicUiActionKind.ViewDiagnostics => ProductLedgerInternalCommandKind.ViewDiagnostics,
            ProductLedgerPublicUiActionKind.ViewLedgerReadiness => ProductLedgerInternalCommandKind.ViewLedgerReadiness,
            ProductLedgerPublicUiActionKind.ViewRuntimeGateStatus => ProductLedgerInternalCommandKind.ViewRuntimeGateStatus,
            ProductLedgerPublicUiActionKind.ViewCheckpointHeadStatus => ProductLedgerInternalCommandKind.ViewCheckpointHeadStatus,
            ProductLedgerPublicUiActionKind.ViewEvidenceGates => ProductLedgerInternalCommandKind.ViewEvidenceGates,
            ProductLedgerPublicUiActionKind.StaticScanPreview => ProductLedgerInternalCommandKind.StaticScanPreview,
            ProductLedgerPublicUiActionKind.RequestExternalAuditPreview => ProductLedgerInternalCommandKind.RequestExternalAuditPreview,
            ProductLedgerPublicUiActionKind.LocalReportPhysicalExportBoundedInternal => ProductLedgerInternalCommandKind.LocalReportPhysicalExportBoundedInternal,
            _ => ProductLedgerInternalCommandKind.ExecuteAction
        };

    private ProductLedgerPublicUiActionResult Result(
        IReadOnlyList<ProductLedgerPublicUiActionBlocker> blockers,
        ProductLedgerPublicUiActionKind action,
        ProductLedgerInternalCommandPreviewResult? routerPreview,
        ProductLedgerInternalCommandResult? commandResult)
    {
        var distinct = blockers.Distinct().OrderBy(blocker => blocker.ToString(), StringComparer.Ordinal).ToArray();
        var completed = distinct.Length == 0 && commandResult is not null;
        var physicalExportCreated = completed && commandResult!.PhysicalExportCreated;
        var fileWritePerformed = completed && commandResult!.FileWritePerformed;
        return new ProductLedgerPublicUiActionResult(
            Decision: completed
                ? ProductLedgerPublicUiActionDecision.CompletedLocalOnlyNonDestructive
                : ProductLedgerPublicUiActionDecision.Rejected,
            Blockers: distinct,
            ActionKind: action,
            Buttons: Buttons(),
            RouterPreview: completed ? routerPreview : null,
            CommandResult: completed ? commandResult : null,
            LocalOnly: true,
            NonDestructive: true,
            Bounded: true,
            FailClosed: true,
            PublicUiActionCompleted: completed,
            LocalOnlyCommandHandlerInvoked: completed,
            DestructiveActionAvailable: false,
            UnboundedPhysicalExportAvailable: false,
            ExternalCloudExportAvailable: false,
            ProviderCloudNetworkAvailable: false,
            DbMigrationAvailable: false,
            KmsWormExternalTrustAvailable: false,
            BrowserCdpWcuOcrRecipesLiveAvailable: false,
            ExternalTelemetryOrSyncAvailable: false,
            BillingLicensingCloudAvailable: false,
            ReleaseCommercialReady: false,
            PhysicalExportCreated: physicalExportCreated,
            FileWritePerformed: fileWritePerformed,
            StatusText: completed ? ReadyStatus : RejectedStatus);
    }

    private static ProductLedgerPublicUiActionButton Button(
        ProductLedgerPublicUiActionKind action,
        bool enabled) =>
        new(
            ActionKind: action,
            Label: Label(action),
            RiskLabel: enabled ? "local-only non-destructive" : "blocked dangerous action",
            RequiredEvidence: RequiredEvidence(action),
            Enabled: enabled,
            LocalOnly: true,
            NonDestructive: enabled,
            Bounded: action == ProductLedgerPublicUiActionKind.LocalReportPhysicalExportBoundedInternal,
            DisabledReason: enabled
                ? "Enabled only through local-only router and command handler."
                : "Blocked by public local-only safety policy.");

    private static IReadOnlyList<string> RequiredEvidence(ProductLedgerPublicUiActionKind action) =>
        action switch
        {
            ProductLedgerPublicUiActionKind.ViewDiagnostics => ["public disabled preview", "operator diagnostics"],
            ProductLedgerPublicUiActionKind.ViewLedgerReadiness => ["public disabled preview", "ledger readiness"],
            ProductLedgerPublicUiActionKind.ViewRuntimeGateStatus => ["public disabled preview", "runtime local-only gate"],
            ProductLedgerPublicUiActionKind.ViewCheckpointHeadStatus => ["public disabled preview", "checkpoint/head evidence"],
            ProductLedgerPublicUiActionKind.ViewEvidenceGates => ["public disabled preview", "redaction/retention/authority evidence"],
            ProductLedgerPublicUiActionKind.StaticScanPreview => ["public disabled preview", "static scan packet"],
            ProductLedgerPublicUiActionKind.RequestExternalAuditPreview => ["public disabled preview", "read-only audit packet"],
            ProductLedgerPublicUiActionKind.LocalReportPhysicalExportBoundedInternal => ["public disabled preview", "bounded export policy", "post-write hash"],
            _ => ["new explicit GO", "safety audit"]
        };

    private static string Label(ProductLedgerPublicUiActionKind action) =>
        action switch
        {
            ProductLedgerPublicUiActionKind.ViewDiagnostics => "View diagnostics",
            ProductLedgerPublicUiActionKind.ViewLedgerReadiness => "View ledger readiness",
            ProductLedgerPublicUiActionKind.ViewRuntimeGateStatus => "View runtime gate status",
            ProductLedgerPublicUiActionKind.ViewCheckpointHeadStatus => "View checkpoint/head status",
            ProductLedgerPublicUiActionKind.ViewEvidenceGates => "View evidence gates",
            ProductLedgerPublicUiActionKind.StaticScanPreview => "Static scan preview",
            ProductLedgerPublicUiActionKind.RequestExternalAuditPreview => "Request external audit preview",
            ProductLedgerPublicUiActionKind.LocalReportPhysicalExportBoundedInternal => "Bounded local report export",
            _ => $"{action} blocked"
        };
}
