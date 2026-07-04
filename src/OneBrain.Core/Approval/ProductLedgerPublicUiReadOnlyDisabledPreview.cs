namespace OneBrain.Core.Approval;

public enum ProductLedgerPublicUiReadOnlyDisabledPreviewDecision
{
    Rejected,
    RenderedReadOnlyDisabledMock
}

public enum ProductLedgerPublicUiReadOnlyDisabledPreviewBlocker
{
    MissingRequest,
    MissingExplicitReadOnlyDisabledMockScope,
    MissingInternalPreview,
    UnsafeInternalPreview,
    MissingPublicSurfaceReadinessPacket,
    StaleOrInconsistentReadinessPacket,
    ExecutableActionPreviewRejected,
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
    ExternalCloudExportClaimed,
    UnboundedPhysicalExportClaimed,
    RawPayloadOrSecretClaimed
}

public sealed record ProductLedgerPublicUiReadOnlyDisabledPreviewRequest(
    bool ExplicitReadOnlyDisabledMockScope,
    ProductLedgerInternalOperatorUiPreviewResult? InternalPreview,
    bool HasPublicSurfaceReadinessPacket,
    bool ReadinessPacketFreshAndConsistent,
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
    bool ClaimsExternalCloudExport,
    bool ClaimsUnboundedPhysicalExport,
    bool ClaimsRawPayloadOrSecret);

public sealed record ProductLedgerPublicUiReadOnlyDisabledPreviewAction(
    string ActionId,
    string Label,
    string DisabledReason,
    IReadOnlyList<string> RequiredEvidence,
    bool Disabled,
    bool Executable,
    string? ProductiveCommandId,
    string? HandlerId,
    string? CallbackName);

public sealed record ProductLedgerPublicUiReadOnlyDisabledPreviewViewModel(
    string ViewModelId,
    string Title,
    string Status,
    int PublicUiReadinessPercentage,
    int PublicProductCommandHandlerExposurePercentage,
    IReadOnlyList<string> Notices,
    IReadOnlyList<string> Sections,
    IReadOnlyList<ProductLedgerPublicUiReadOnlyDisabledPreviewAction> Actions,
    IReadOnlyList<string> Warnings,
    string SafeNextStep,
    bool ReadOnly,
    bool DisabledMockOnly,
    bool FailClosed,
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
    bool ExternalCloudExportAvailable,
    bool UnboundedPhysicalExportAvailable,
    bool RawPayloadOrSecretAvailable,
    bool ReleaseCommercialReady,
    string StatusText);

public sealed record ProductLedgerPublicUiReadOnlyDisabledPreviewResult(
    ProductLedgerPublicUiReadOnlyDisabledPreviewDecision Decision,
    IReadOnlyList<ProductLedgerPublicUiReadOnlyDisabledPreviewBlocker> Blockers,
    ProductLedgerPublicUiReadOnlyDisabledPreviewViewModel ViewModel);

public sealed class ProductLedgerPublicUiReadOnlyDisabledPreviewPresenter
{
    public const string ReadyStatus =
        "PRODUCT_LEDGER_PUBLIC_UI_READ_ONLY_DISABLED_MOCK_PREVIEW_READY DESIGN_SAFE READ_ONLY DISABLED_MOCK_ONLY NO_PUBLIC_UI_ACTION NO_PRODUCT_COMMAND_HANDLER_EXPOSURE NO_DESTRUCTIVE_ACTION NO_EXTERNAL_CLOUD_EXPORT NO_PROVIDER_CLOUD_NETWORK NO_DB_MIGRATION NO_KMS_WORM_EXTERNAL_TRUST NO_LIVE_AUTOMATION NO_RELEASE_COMMERCIAL";

    public const string RejectedStatus =
        "PRODUCT_LEDGER_PUBLIC_UI_READ_ONLY_DISABLED_MOCK_PREVIEW_REJECTED FAIL_CLOSED ACTIONS_DISABLED NO_PUBLIC_UI_ACTION NO_PRODUCT_COMMAND_HANDLER_EXPOSURE NO_DESTRUCTIVE_ACTION NO_EXTERNAL_CLOUD_EXPORT NO_PROVIDER_CLOUD_NETWORK NO_DB_MIGRATION NO_KMS_WORM_EXTERNAL_TRUST NO_LIVE_AUTOMATION NO_RELEASE_COMMERCIAL";

    public ProductLedgerPublicUiReadOnlyDisabledPreviewResult Render(
        ProductLedgerPublicUiReadOnlyDisabledPreviewRequest? request)
    {
        var blockers = new List<ProductLedgerPublicUiReadOnlyDisabledPreviewBlocker>();
        if (request is null)
        {
            blockers.Add(ProductLedgerPublicUiReadOnlyDisabledPreviewBlocker.MissingRequest);
            return Result(blockers, null);
        }

        AddRequestBlockers(request, blockers);
        AddInternalPreviewBlockers(request.InternalPreview, blockers);
        return Result(blockers, request.InternalPreview);
    }

    private static void AddRequestBlockers(
        ProductLedgerPublicUiReadOnlyDisabledPreviewRequest request,
        List<ProductLedgerPublicUiReadOnlyDisabledPreviewBlocker> blockers)
    {
        if (!request.ExplicitReadOnlyDisabledMockScope)
        {
            blockers.Add(ProductLedgerPublicUiReadOnlyDisabledPreviewBlocker.MissingExplicitReadOnlyDisabledMockScope);
        }

        if (!request.HasPublicSurfaceReadinessPacket)
        {
            blockers.Add(ProductLedgerPublicUiReadOnlyDisabledPreviewBlocker.MissingPublicSurfaceReadinessPacket);
        }

        if (!request.ReadinessPacketFreshAndConsistent)
        {
            blockers.Add(ProductLedgerPublicUiReadOnlyDisabledPreviewBlocker.StaleOrInconsistentReadinessPacket);
        }

        if (request.RequestsPublicUiAction)
        {
            blockers.Add(ProductLedgerPublicUiReadOnlyDisabledPreviewBlocker.PublicUiActionRequested);
        }

        if (request.RequestsDestructiveAction)
        {
            blockers.Add(ProductLedgerPublicUiReadOnlyDisabledPreviewBlocker.DestructiveActionRequested);
        }

        if (request.RequestsProductCommandHandler)
        {
            blockers.Add(ProductLedgerPublicUiReadOnlyDisabledPreviewBlocker.ProductCommandHandlerRequested);
        }

        if (request.RequestsProductiveServiceRegistration)
        {
            blockers.Add(ProductLedgerPublicUiReadOnlyDisabledPreviewBlocker.ProductiveServiceRegistrationRequested);
        }

        if (request.ClaimsProviderCloudNetwork)
        {
            blockers.Add(ProductLedgerPublicUiReadOnlyDisabledPreviewBlocker.ProviderCloudNetworkClaimed);
        }

        if (request.ClaimsDbMigration)
        {
            blockers.Add(ProductLedgerPublicUiReadOnlyDisabledPreviewBlocker.DbMigrationClaimed);
        }

        if (request.ClaimsKmsWormExternalTrust)
        {
            blockers.Add(ProductLedgerPublicUiReadOnlyDisabledPreviewBlocker.KmsWormExternalTrustClaimed);
        }

        if (request.ClaimsBrowserCdpWcuOcrRecipesLive)
        {
            blockers.Add(ProductLedgerPublicUiReadOnlyDisabledPreviewBlocker.BrowserCdpWcuOcrRecipesLiveClaimed);
        }

        if (request.ClaimsReleaseCommercial)
        {
            blockers.Add(ProductLedgerPublicUiReadOnlyDisabledPreviewBlocker.ReleaseCommercialClaimed);
        }

        if (request.ClaimsExternalTelemetryOrSync)
        {
            blockers.Add(ProductLedgerPublicUiReadOnlyDisabledPreviewBlocker.ExternalTelemetryOrSyncClaimed);
        }

        if (request.ClaimsBillingLicensingCloud)
        {
            blockers.Add(ProductLedgerPublicUiReadOnlyDisabledPreviewBlocker.BillingLicensingCloudClaimed);
        }

        if (request.ClaimsExternalCloudExport)
        {
            blockers.Add(ProductLedgerPublicUiReadOnlyDisabledPreviewBlocker.ExternalCloudExportClaimed);
        }

        if (request.ClaimsUnboundedPhysicalExport)
        {
            blockers.Add(ProductLedgerPublicUiReadOnlyDisabledPreviewBlocker.UnboundedPhysicalExportClaimed);
        }

        if (request.ClaimsRawPayloadOrSecret)
        {
            blockers.Add(ProductLedgerPublicUiReadOnlyDisabledPreviewBlocker.RawPayloadOrSecretClaimed);
        }
    }

    private static void AddInternalPreviewBlockers(
        ProductLedgerInternalOperatorUiPreviewResult? internalPreview,
        List<ProductLedgerPublicUiReadOnlyDisabledPreviewBlocker> blockers)
    {
        if (internalPreview is null)
        {
            blockers.Add(ProductLedgerPublicUiReadOnlyDisabledPreviewBlocker.MissingInternalPreview);
            return;
        }

        var viewModel = internalPreview.ViewModel;
        if (internalPreview.Decision != ProductLedgerInternalOperatorUiPreviewDecision.RenderedPreview
            || internalPreview.Blockers.Count > 0
            || !viewModel.ReadOnly
            || !viewModel.FailClosed
            || viewModel.PublicUiActionAvailable
            || viewModel.DestructiveUserFacingActionAvailable
            || viewModel.ProductCommandHandlerAvailable
            || viewModel.ProductiveServiceRegistrationAvailable
            || viewModel.ProviderCloudNetworkAvailable
            || viewModel.DbMigrationAvailable
            || viewModel.KmsWormExternalTrustAvailable
            || viewModel.BrowserCdpWcuOcrRecipesLiveAvailable
            || viewModel.ExternalTelemetryOrSyncAvailable
            || viewModel.BillingLicensingCloudAvailable
            || viewModel.ReleaseCommercialReady)
        {
            blockers.Add(ProductLedgerPublicUiReadOnlyDisabledPreviewBlocker.UnsafeInternalPreview);
        }

        if (viewModel.ActionPreviews.Any(action => !action.Disabled
            || action.ProductiveCommandId is not null
            || action.HandlerId is not null
            || action.CallbackName is not null))
        {
            blockers.Add(ProductLedgerPublicUiReadOnlyDisabledPreviewBlocker.ExecutableActionPreviewRejected);
        }
    }

    private static ProductLedgerPublicUiReadOnlyDisabledPreviewResult Result(
        IReadOnlyList<ProductLedgerPublicUiReadOnlyDisabledPreviewBlocker> blockers,
        ProductLedgerInternalOperatorUiPreviewResult? internalPreview)
    {
        var distinct = blockers.Distinct().OrderBy(blocker => blocker.ToString(), StringComparer.Ordinal).ToArray();
        var rendered = distinct.Length == 0 && internalPreview is not null;
        return new ProductLedgerPublicUiReadOnlyDisabledPreviewResult(
            Decision: rendered
                ? ProductLedgerPublicUiReadOnlyDisabledPreviewDecision.RenderedReadOnlyDisabledMock
                : ProductLedgerPublicUiReadOnlyDisabledPreviewDecision.Rejected,
            Blockers: distinct,
            ViewModel: rendered
                ? ReadyViewModel(internalPreview!.ViewModel)
                : BlockedViewModel(distinct));
    }

    private static ProductLedgerPublicUiReadOnlyDisabledPreviewViewModel ReadyViewModel(
        ProductLedgerInternalOperatorUiPreviewViewModel source) =>
        ViewModel(
            rendered: true,
            status: "READ_ONLY_DISABLED_MOCK_ONLY",
            sections:
            [
                "Runtime local-only gate: " + source.Header.Status,
                "Product Ledger writer: internal/local-only only",
                "Bounded local report export: internal bounded only",
                "Evidence: redaction, retention, authority, failure/replay and rollback required",
                "Public UI actions: disabled",
                "Product command handlers: not exposed"
            ],
            actions: source.ActionPreviews.Select(action => new ProductLedgerPublicUiReadOnlyDisabledPreviewAction(
                ActionId: action.ActionId,
                Label: action.Label,
                DisabledReason: "Public mock is read-only and cannot execute actions.",
                RequiredEvidence: action.RequiredEvidence,
                Disabled: true,
                Executable: false,
                ProductiveCommandId: null,
                HandlerId: null,
                CallbackName: null)).ToArray(),
            warnings:
            [
                "Public UI readiness remains 0%.",
                "Product command handler exposure remains 0%.",
                "Local-only evidence is not WORM/compliance-grade custody."
            ]);

    private static ProductLedgerPublicUiReadOnlyDisabledPreviewViewModel BlockedViewModel(
        IReadOnlyList<ProductLedgerPublicUiReadOnlyDisabledPreviewBlocker> blockers) =>
        ViewModel(
            rendered: false,
            status: "FAIL_CLOSED",
            sections: blockers.Select(blocker => blocker.ToString()).ToArray(),
            actions: DisabledFallbackActions(),
            warnings: ["Fail-closed public mock does not expose execution authority."]);

    private static ProductLedgerPublicUiReadOnlyDisabledPreviewViewModel ViewModel(
        bool rendered,
        string status,
        IReadOnlyList<string> sections,
        IReadOnlyList<ProductLedgerPublicUiReadOnlyDisabledPreviewAction> actions,
        IReadOnlyList<string> warnings) =>
        new(
            ViewModelId: "product-ledger.public-ui.read-only-disabled-mock.v1",
            Title: "Product Ledger Public Surface Preview",
            Status: status,
            PublicUiReadinessPercentage: 0,
            PublicProductCommandHandlerExposurePercentage: 0,
            Notices:
            [
                "design-safe read-only mock",
                "all actions disabled",
                "no public UI action",
                "no product command handler exposure",
                "no external/cloud export",
                "no release/commercial"
            ],
            Sections: sections,
            Actions: actions,
            Warnings: warnings,
            SafeNextStep: rendered
                ? "PUBLIC_UI_ACTION_OR_PRODUCT_COMMAND_HANDLER_EXPOSURE_REQUIRES_NEW_EXPLICIT_GO"
                : "FIX_BLOCKERS_THEN_READ_ONLY_AUDIT",
            ReadOnly: true,
            DisabledMockOnly: true,
            FailClosed: true,
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
            ExternalCloudExportAvailable: false,
            UnboundedPhysicalExportAvailable: false,
            RawPayloadOrSecretAvailable: false,
            ReleaseCommercialReady: false,
            StatusText: rendered ? ReadyStatus : RejectedStatus);

    private static IReadOnlyList<ProductLedgerPublicUiReadOnlyDisabledPreviewAction> DisabledFallbackActions() =>
    [
        new("public-ui-action", "Public UI action", "Requires new explicit GO.", ["public surface audit"], true, false, null, null, null),
        new("product-command-handler", "Product command handler", "Requires new explicit GO.", ["command exposure audit"], true, false, null, null, null),
        new("destructive-action", "Destructive action", "Requires new explicit GO.", ["destructive action approval model"], true, false, null, null, null),
        new("external-cloud-export", "External/cloud export", "Requires new explicit GO.", ["external export policy"], true, false, null, null, null)
    ];
}
