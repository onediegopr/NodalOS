using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Approval;

namespace OneBrain.Recipes.Tests;

[TestClass]
[TestCategory("ApprovalHumanReview")]
[TestCategory("PhaseEApprovalHumanReview")]
public sealed class ProductLedgerLocalDevRoutePreviewTests
{
    [TestMethod]
    public void LocalDevRoutePreview_RendersSnapshotRecipe()
    {
        var result = new ProductLedgerLocalDevRoutePreview().Render(ReadyRequest());

        Assert.AreEqual(ProductLedgerLocalDevRoutePreviewDecision.RenderedLocalDevInternalPreview, result.Decision);
        StringAssert.Contains(result.HtmlSnapshot, "data-testid=\"local-dev-route-preview\"");
        StringAssert.Contains(result.HtmlSnapshot, "local-dev/internal-only");
        StringAssert.Contains(result.HtmlSnapshot, "not publicly deployed");
        StringAssert.Contains(result.HtmlSnapshot, "no telemetry");
        StringAssert.Contains(result.HtmlSnapshot, "no external network");
        StringAssert.Contains(result.HtmlSnapshot, "data-testid=\"product-ledger-operator-snapshot\"");
        StringAssert.Contains(result.HtmlSnapshot, "data-testid=\"action-destructive-write\"");
        StringAssert.Contains(result.HtmlSnapshot, "disabled aria-disabled=\"true\"");
        AssertNoForbiddenSurface(result);
    }

    [TestMethod]
    public void LocalDevRoutePreview_BlocksProductionPublicDeployAndExternalRecipe()
    {
        var result = new ProductLedgerLocalDevRoutePreview().Render(ReadyRequest() with
        {
            ClaimsProductionMode = true,
            ClaimsPublicDeploy = true,
            ClaimsTelemetryOrSync = true,
            ClaimsProviderCloudNetwork = true,
            ClaimsReleaseMode = true
        });

        Assert.AreEqual(ProductLedgerLocalDevRoutePreviewDecision.Rejected, result.Decision);
        CollectionAssert.Contains(result.Blockers.ToArray(), ProductLedgerLocalDevRoutePreviewBlocker.ProductionModeClaimed);
        CollectionAssert.Contains(result.Blockers.ToArray(), ProductLedgerLocalDevRoutePreviewBlocker.PublicDeployClaimed);
        CollectionAssert.Contains(result.Blockers.ToArray(), ProductLedgerLocalDevRoutePreviewBlocker.TelemetryOrSyncClaimed);
        CollectionAssert.Contains(result.Blockers.ToArray(), ProductLedgerLocalDevRoutePreviewBlocker.ProviderCloudNetworkClaimed);
        CollectionAssert.Contains(result.Blockers.ToArray(), ProductLedgerLocalDevRoutePreviewBlocker.ReleaseModeClaimed);
        AssertNoForbiddenSurface(result);
    }

    private static ProductLedgerLocalDevRoutePreviewRequest ReadyRequest() =>
        new(
            ExplicitLocalDevInternalPreviewScope: true,
            LocalMode: true,
            DevMode: true,
            ClaimsProductionMode: false,
            ClaimsReleaseMode: false,
            ClaimsCommercialMode: false,
            ClaimsExternalHostOrPublicOrigin: false,
            ClaimsPublicDeploy: false,
            ClaimsTelemetryOrSync: false,
            ClaimsProviderCloudNetwork: false,
            ClaimsDbMigration: false,
            ClaimsKmsWormExternalTrust: false,
            ClaimsBrowserCdpWcuOcrRecipesLive: false,
            RequestsDestructiveAction: false,
            ClaimsUnboundedPhysicalExport: false,
            ClaimsExternalCloudExport: false,
            ClaimsRawPayloadOrSecret: false,
            RenderableSnapshotRequest: ReadyRenderableRequest());

    private static ProductLedgerRenderableOperatorSurfaceRequest ReadyRenderableRequest() =>
        new(
            ExplicitLocalOnlySnapshotScope: true,
            PublicActionSurface: ReadyActionSurface(),
            ClaimsEndpointRouteController: false,
            ClaimsExternalScript: false,
            ClaimsTelemetryOrSync: false,
            ClaimsProviderCloudNetwork: false,
            ClaimsDbMigration: false,
            ClaimsKmsWormExternalTrust: false,
            ClaimsBrowserCdpWcuOcrRecipesLive: false,
            ClaimsReleaseCommercial: false,
            ClaimsRawPayloadOrSecret: false);

    private static ProductLedgerPublicUiActionResult ReadyActionSurface() =>
        new ProductLedgerPublicUiActionSurface().Execute(ReadyActionRequest(ProductLedgerPublicUiActionKind.ViewDiagnostics));

    private static ProductLedgerPublicUiActionRequest ReadyActionRequest(ProductLedgerPublicUiActionKind action) =>
        new(
            ExplicitPublicLocalOnlyNonDestructiveScope: true,
            PublicReadOnlyDisabledPreview: ReadyPublicPreview(),
            ActionKind: action,
            RawActionName: action.ToString(),
            LocalReportExportRequest: null,
            ClaimsRawPayloadOrSecret: false,
            RequestsDestructiveAction: false,
            RequestsGenericExecuteAction: false,
            RequestsProductiveServiceRegistration: false,
            ClaimsProviderCloudNetwork: false,
            ClaimsDbMigration: false,
            ClaimsKmsWormExternalTrust: false,
            ClaimsBrowserCdpWcuOcrRecipesLive: false,
            ClaimsReleaseCommercial: false,
            ClaimsExternalTelemetryOrSync: false,
            ClaimsBillingLicensingCloud: false,
            ClaimsUnboundedPhysicalExport: false,
            ClaimsExternalCloudExport: false,
            RequestsDeleteOrUnsafeOverwrite: false);

    private static ProductLedgerPublicUiReadOnlyDisabledPreviewResult ReadyPublicPreview() =>
        new ProductLedgerPublicUiReadOnlyDisabledPreviewPresenter().Render(
            new ProductLedgerPublicUiReadOnlyDisabledPreviewRequest(
                ExplicitReadOnlyDisabledMockScope: true,
                InternalPreview: ReadyInternalPreview(),
                HasPublicSurfaceReadinessPacket: true,
                ReadinessPacketFreshAndConsistent: true,
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
                ClaimsExternalCloudExport: false,
                ClaimsUnboundedPhysicalExport: false,
                ClaimsRawPayloadOrSecret: false));

    private static ProductLedgerInternalOperatorUiPreviewResult ReadyInternalPreview() =>
        new ProductLedgerInternalOperatorUiPresenter().Render(
            new ProductLedgerInternalOperatorUiPreviewRequest(
                ExplicitInternalLocalOnlyReadOnlyPreviewScope: true,
                Diagnostics: ReadyDiagnostics(),
                RequestsPublicUiAction: false,
                RequestsDestructiveUserFacingAction: false,
                RequestsProductCommandHandler: false,
                RequestsProductiveServiceRegistration: false,
                ClaimsProviderCloudNetwork: false,
                ClaimsDbMigration: false,
                ClaimsKmsWormExternalTrust: false,
                ClaimsBrowserCdpWcuOcrRecipesLive: false,
                ClaimsReleaseCommercial: false,
                ClaimsExternalTelemetryOrSync: false,
                ClaimsBillingLicensingCloud: false));

    private static ProductLedgerLocalOnlyOperatorDiagnosticsResult ReadyDiagnostics() =>
        new(
            Decision: ProductLedgerLocalOnlyOperatorDiagnosticsDecision.RenderedReadOnly,
            Blockers: [],
            Sections:
            [
                Section("Runtime Local-Only Gate", "ENABLED_LOCAL_ONLY_INTERNAL", ["feature_flag=enabled:local-only-internal"]),
                Section("Product Ledger Path Policy", "ACTIVE_LOCAL_ONLY_POLICY_BOUND", ["candidate_id=ledger-local-dev-route-recipe-001"]),
                Section("Bounded Writer Status", "WRITER_BOUNDED_LOCAL_ONLY_SURFACE_READ_ONLY", ["operator_surface_write_allowed=false"]),
                Section("Checkpoint / Head Status", "VERIFIED_HEAD_PRESENT", ["same_boundary_trust=true"]),
                Section("Evidence Gates", "EVIDENCE_REFERENCES_FRESH_AND_WELL_FORMED", ["redaction_before_persistence=True", "authority=True"]),
                Section("Disabled Actions", "ALL_ACTIONS_DISABLED", ["destructive action", "unbounded export", "external/cloud export"]),
                Section("Safe Next Step", "LOCAL_DEV_ROUTE_EXTERNAL_AUDIT_READ_ONLY", ["snapshot fixture"])
            ],
            ActionPreviews:
            [
                new("View local-only diagnostics snapshot", "read-only preview only", "operator visibility without execution authority", ["runtime gate"], Disabled: true, ProductiveCommandId: null, HandlerName: null, CallbackName: null)
            ],
            DisabledActions: ["destructive user-facing action", "provider/cloud/network", "release/commercial"],
            SafeNextStep: "LOCAL_DEV_ROUTE_INTERNAL_ENDPOINT_PREVIEW",
            ReadOnly: true,
            LocalOnly: true,
            InternalOnly: true,
            FailClosed: true,
            PublicUiActionAvailable: false,
            DestructiveUserFacingActionAvailable: false,
            ProductCommandHandlerAvailable: false,
            ProductiveServiceRegistrationAvailable: false,
            ProviderCloudNetworkAvailable: false,
            DbMigrationAvailable: false,
            KmsWormExternalTrustAvailable: false,
            BrowserCdpWcuOcrRecipesLiveAvailable: false,
            ReleaseCommercialReady: false,
            StatusText: ProductLedgerLocalOnlyOperatorDiagnosticsPresenter.ReadyStatus);

    private static ProductLedgerLocalOnlyOperatorDiagnosticsSection Section(
        string title,
        string status,
        IReadOnlyList<string> lines) =>
        new(title, status, lines, ProductLedgerLocalOnlyOperatorDiagnosticsSeverity.Info);

    private static void AssertNoForbiddenSurface(ProductLedgerLocalDevRoutePreviewResult result)
    {
        Assert.IsTrue(result.LocalOnly);
        Assert.IsTrue(result.DevOnly);
        Assert.IsTrue(result.InternalOnly);
        Assert.IsTrue(result.ReadOnly);
        Assert.IsTrue(result.NonDestructive);
        Assert.IsTrue(result.FailClosed);
        Assert.IsFalse(result.PublicDeployAvailable);
        Assert.IsFalse(result.ExternalNetworkAvailable);
        Assert.IsFalse(result.TelemetryOrSyncAvailable);
        Assert.IsFalse(result.ProviderCloudNetworkAvailable);
        Assert.IsFalse(result.DbMigrationAvailable);
        Assert.IsFalse(result.KmsWormExternalTrustAvailable);
        Assert.IsFalse(result.BrowserCdpWcuOcrRecipesLiveAvailable);
        Assert.IsFalse(result.DestructiveActionAvailable);
        Assert.IsFalse(result.UnboundedPhysicalExportAvailable);
        Assert.IsFalse(result.ExternalCloudExportAvailable);
        Assert.IsFalse(result.ReleaseCommercialReady);
    }
}
