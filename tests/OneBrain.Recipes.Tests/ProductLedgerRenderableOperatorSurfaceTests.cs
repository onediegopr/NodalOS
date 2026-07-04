using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Approval;

namespace OneBrain.Recipes.Tests;

[TestClass]
[TestCategory("ApprovalHumanReview")]
[TestCategory("PhaseEApprovalHumanReview")]
public sealed class ProductLedgerRenderableOperatorSurfaceTests
{
    [TestMethod]
    public void RenderableOperatorSurface_RendersSnapshotRecipe()
    {
        var result = new ProductLedgerRenderableOperatorSurfaceRenderer().Render(ReadyRequest());

        Assert.AreEqual(ProductLedgerRenderableOperatorSurfaceDecision.RenderedSnapshot, result.Decision);
        Assert.AreEqual(100, result.Model.RenderableOperatorSnapshotPercent);
        StringAssert.Contains(result.HtmlSnapshot, "data-testid=\"product-ledger-operator-snapshot\"");
        StringAssert.Contains(result.HtmlSnapshot, "data-testid=\"runtime-gate\"");
        StringAssert.Contains(result.HtmlSnapshot, "data-testid=\"bounded-export\"");
        StringAssert.Contains(result.HtmlSnapshot, "data-testid=\"action-destructive-write\"");
        StringAssert.Contains(result.HtmlSnapshot, "disabled aria-disabled=\"true\"");
        AssertNoExternalRelease(result);
    }

    [TestMethod]
    public void RenderableOperatorSurface_BlocksExternalClaimRecipe()
    {
        var result = new ProductLedgerRenderableOperatorSurfaceRenderer().Render(ReadyRequest() with
        {
            ClaimsEndpointRouteController = true,
            ClaimsTelemetryOrSync = true,
            ClaimsReleaseCommercial = true
        });

        Assert.AreEqual(ProductLedgerRenderableOperatorSurfaceDecision.Rejected, result.Decision);
        CollectionAssert.Contains(result.Blockers.ToArray(), ProductLedgerRenderableOperatorSurfaceBlocker.EndpointRouteControllerClaimed);
        CollectionAssert.Contains(result.Blockers.ToArray(), ProductLedgerRenderableOperatorSurfaceBlocker.TelemetryOrSyncClaimed);
        CollectionAssert.Contains(result.Blockers.ToArray(), ProductLedgerRenderableOperatorSurfaceBlocker.ReleaseCommercialClaimed);
        AssertNoExternalRelease(result);
    }

    private static ProductLedgerRenderableOperatorSurfaceRequest ReadyRequest() =>
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
                Section("Product Ledger Path Policy", "ACTIVE_LOCAL_ONLY_POLICY_BOUND", ["candidate_id=ledger-renderable-snapshot-recipe-001"]),
                Section("Bounded Writer Status", "WRITER_BOUNDED_LOCAL_ONLY_SURFACE_READ_ONLY", ["operator_surface_write_allowed=false"]),
                Section("Checkpoint / Head Status", "VERIFIED_HEAD_PRESENT", ["same_boundary_trust=true"]),
                Section("Evidence Gates", "EVIDENCE_REFERENCES_FRESH_AND_WELL_FORMED", ["redaction_before_persistence=True", "authority=True"]),
                Section("Disabled Actions", "ALL_ACTIONS_DISABLED", ["destructive action", "unbounded export", "external/cloud export"]),
                Section("Safe Next Step", "DOM_CONTRACT_HARDENING_OR_READ_ONLY_AUDIT_ONLY", ["snapshot fixture"])
            ],
            ActionPreviews:
            [
                new("View local-only diagnostics snapshot", "read-only preview only", "operator visibility without execution authority", ["runtime gate"], Disabled: true, ProductiveCommandId: null, HandlerName: null, CallbackName: null)
            ],
            DisabledActions: ["destructive user-facing action", "provider/cloud/network", "release/commercial"],
            SafeNextStep: "RENDERABLE_OPERATOR_SURFACE_SNAPSHOT",
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

    private static void AssertNoExternalRelease(ProductLedgerRenderableOperatorSurfaceResult result)
    {
        Assert.IsTrue(result.Model.LocalOnly);
        Assert.IsTrue(result.Model.InternalOnly);
        Assert.IsTrue(result.Model.SnapshotOnly);
        Assert.IsFalse(result.Model.EndpointRouteControllerAvailable);
        Assert.IsFalse(result.Model.ExternalScriptAvailable);
        Assert.IsFalse(result.Model.TelemetryOrSyncAvailable);
        Assert.IsFalse(result.Model.ProviderCloudNetworkAvailable);
        Assert.IsFalse(result.Model.DbMigrationAvailable);
        Assert.IsFalse(result.Model.KmsWormExternalTrustAvailable);
        Assert.IsFalse(result.Model.BrowserCdpWcuOcrRecipesLiveAvailable);
        Assert.IsFalse(result.Model.ReleaseCommercialReady);
    }
}
