using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Approval;

namespace OneBrain.Recipes.Tests;

[TestClass]
[TestCategory("ApprovalHumanReview")]
[TestCategory("PhaseEApprovalHumanReview")]
public sealed class ProductLedgerPublicUiReadOnlyDisabledPreviewTests
{
    [TestMethod]
    public void PublicUiReadOnlyDisabledPreview_RendersDisabledMockRecipe()
    {
        var result = new ProductLedgerPublicUiReadOnlyDisabledPreviewPresenter().Render(ReadyRequest());

        Assert.AreEqual(ProductLedgerPublicUiReadOnlyDisabledPreviewDecision.RenderedReadOnlyDisabledMock, result.Decision);
        Assert.AreEqual(0, result.ViewModel.PublicUiReadinessPercentage);
        Assert.AreEqual(0, result.ViewModel.PublicProductCommandHandlerExposurePercentage);
        Assert.IsTrue(result.ViewModel.Actions.All(action => action.Disabled));
        Assert.IsTrue(result.ViewModel.Actions.All(action => !action.Executable));
        AssertNoExternalRelease(result.ViewModel);
    }

    [TestMethod]
    public void PublicUiReadOnlyDisabledPreview_BlocksPublicActionRecipe()
    {
        var result = new ProductLedgerPublicUiReadOnlyDisabledPreviewPresenter().Render(ReadyRequest() with
        {
            RequestsPublicUiAction = true,
            RequestsProductCommandHandler = true,
            ClaimsExternalCloudExport = true,
            ClaimsReleaseCommercial = true
        });

        Assert.AreEqual(ProductLedgerPublicUiReadOnlyDisabledPreviewDecision.Rejected, result.Decision);
        CollectionAssert.Contains(result.Blockers.ToArray(), ProductLedgerPublicUiReadOnlyDisabledPreviewBlocker.PublicUiActionRequested);
        CollectionAssert.Contains(result.Blockers.ToArray(), ProductLedgerPublicUiReadOnlyDisabledPreviewBlocker.ProductCommandHandlerRequested);
        CollectionAssert.Contains(result.Blockers.ToArray(), ProductLedgerPublicUiReadOnlyDisabledPreviewBlocker.ExternalCloudExportClaimed);
        CollectionAssert.Contains(result.Blockers.ToArray(), ProductLedgerPublicUiReadOnlyDisabledPreviewBlocker.ReleaseCommercialClaimed);
        AssertNoExternalRelease(result.ViewModel);
    }

    private static ProductLedgerPublicUiReadOnlyDisabledPreviewRequest ReadyRequest() =>
        new(
            ExplicitReadOnlyDisabledMockScope: true,
            InternalPreview: new ProductLedgerInternalOperatorUiPresenter().Render(ReadyInternalRequest()),
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
            ClaimsRawPayloadOrSecret: false);

    private static ProductLedgerInternalOperatorUiPreviewRequest ReadyInternalRequest() =>
        new(
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
            ClaimsBillingLicensingCloud: false);

    private static ProductLedgerLocalOnlyOperatorDiagnosticsResult ReadyDiagnostics() =>
        new(
            Decision: ProductLedgerLocalOnlyOperatorDiagnosticsDecision.RenderedReadOnly,
            Blockers: [],
            Sections:
            [
                Section("Runtime Local-Only Gate", "ENABLED_LOCAL_ONLY_INTERNAL", ["feature_flag=enabled:local-only-internal"]),
                Section("Product Ledger Path Policy", "ACTIVE_LOCAL_ONLY_POLICY_BOUND", ["candidate_id=ledger-public-mock-recipe-001"]),
                Section("Bounded Writer Status", "WRITER_BOUNDED_LOCAL_ONLY_SURFACE_READ_ONLY", ["operator_surface_write_allowed=false"]),
                Section("Checkpoint / Head Status", "VERIFIED_HEAD_PRESENT", ["same_boundary_trust=true"]),
                Section("Evidence Gates", "EVIDENCE_REFERENCES_FRESH_AND_WELL_FORMED", ["redaction_before_persistence=True", "authority=True"]),
                Section("Disabled Actions", "ALL_ACTIONS_DISABLED", ["enable public UI", "run destructive action", "register command handler"]),
                Section("Safe Next Step", "READ_ONLY_AUDIT_OR_STATIC_SCAN_HARDENING_ONLY", ["read-only audit"])
            ],
            ActionPreviews:
            [
                new("View local-only diagnostics snapshot", "read-only preview only", "operator visibility without execution authority", ["runtime gate"], Disabled: true, ProductiveCommandId: null, HandlerName: null, CallbackName: null)
            ],
            DisabledActions: ["public UI action", "destructive user-facing action", "product command handler"],
            SafeNextStep: "READ_ONLY_AUDIT",
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

    private static void AssertNoExternalRelease(ProductLedgerPublicUiReadOnlyDisabledPreviewViewModel viewModel)
    {
        Assert.IsTrue(viewModel.ReadOnly);
        Assert.IsTrue(viewModel.DisabledMockOnly);
        Assert.IsFalse(viewModel.PublicUiActionAvailable);
        Assert.IsFalse(viewModel.DestructiveActionAvailable);
        Assert.IsFalse(viewModel.ProductCommandHandlerAvailable);
        Assert.IsFalse(viewModel.ProviderCloudNetworkAvailable);
        Assert.IsFalse(viewModel.DbMigrationAvailable);
        Assert.IsFalse(viewModel.KmsWormExternalTrustAvailable);
        Assert.IsFalse(viewModel.BrowserCdpWcuOcrRecipesLiveAvailable);
        Assert.IsFalse(viewModel.ExternalCloudExportAvailable);
        Assert.IsFalse(viewModel.ReleaseCommercialReady);
    }
}
