using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Approval;

namespace OneBrain.Recipes.Tests;

[TestClass]
[TestCategory("ApprovalHumanReview")]
[TestCategory("PhaseEApprovalHumanReview")]
public sealed class ProductLedgerInternalOperatorUiPreviewTests
{
    [TestMethod]
    public void InternalOperatorUiPreview_RendersCockpitRecipeFromDiagnosticsSurface()
    {
        var result = new ProductLedgerInternalOperatorUiPresenter().Render(ReadyRequest());

        Assert.AreEqual(ProductLedgerInternalOperatorUiPreviewDecision.RenderedPreview, result.Decision);
        Assert.AreEqual("Product Ledger Local-Only", result.ViewModel.Header.Title);
        Assert.AreEqual(82, result.ViewModel.Header.ReadinessPercentage);
        Assert.IsTrue(result.ViewModel.Sections.Any(section => section.Title == "Runtime Local-Only Gate"));
        Assert.IsTrue(result.ViewModel.Sections.Any(section => section.Title == "Bounded Writer Status"));
        Assert.IsTrue(result.ViewModel.Sections.Any(section => section.Title == "Checkpoint / Head Status"));
        Assert.IsTrue(result.ViewModel.Sections.Any(section => section.Title == "Evidence Gates"));
        Assert.IsTrue(result.ViewModel.ActionPreviews.All(action => action.Disabled));
        Assert.IsTrue(result.ViewModel.ActionPreviews.All(action => action.ProductiveCommandId is null));
        Assert.IsTrue(result.ViewModel.ActionPreviews.All(action => action.HandlerId is null));
        Assert.IsTrue(result.ViewModel.ActionPreviews.All(action => action.CallbackName is null));
        AssertNoPublicExternalRelease(result.ViewModel);
    }

    [TestMethod]
    public void InternalOperatorUiPreview_BlocksProductExternalReleaseTelemetryAndBillingRecipeClaims()
    {
        var result = new ProductLedgerInternalOperatorUiPresenter().Render(
            ReadyRequest() with
            {
                RequestsPublicUiAction = true,
                RequestsDestructiveUserFacingAction = true,
                RequestsProductCommandHandler = true,
                ClaimsProviderCloudNetwork = true,
                ClaimsDbMigration = true,
                ClaimsKmsWormExternalTrust = true,
                ClaimsBrowserCdpWcuOcrRecipesLive = true,
                ClaimsReleaseCommercial = true,
                ClaimsExternalTelemetryOrSync = true,
                ClaimsBillingLicensingCloud = true
            });

        Assert.AreEqual(ProductLedgerInternalOperatorUiPreviewDecision.Rejected, result.Decision);
        CollectionAssert.Contains(result.Blockers.ToArray(), ProductLedgerInternalOperatorUiPreviewBlocker.PublicUiActionRequested);
        CollectionAssert.Contains(result.Blockers.ToArray(), ProductLedgerInternalOperatorUiPreviewBlocker.DestructiveUserFacingActionRequested);
        CollectionAssert.Contains(result.Blockers.ToArray(), ProductLedgerInternalOperatorUiPreviewBlocker.ProductCommandHandlerRequested);
        CollectionAssert.Contains(result.Blockers.ToArray(), ProductLedgerInternalOperatorUiPreviewBlocker.ProviderCloudNetworkClaimed);
        CollectionAssert.Contains(result.Blockers.ToArray(), ProductLedgerInternalOperatorUiPreviewBlocker.DbMigrationClaimed);
        CollectionAssert.Contains(result.Blockers.ToArray(), ProductLedgerInternalOperatorUiPreviewBlocker.KmsWormExternalTrustClaimed);
        CollectionAssert.Contains(result.Blockers.ToArray(), ProductLedgerInternalOperatorUiPreviewBlocker.BrowserCdpWcuOcrRecipesLiveClaimed);
        CollectionAssert.Contains(result.Blockers.ToArray(), ProductLedgerInternalOperatorUiPreviewBlocker.ReleaseCommercialClaimed);
        CollectionAssert.Contains(result.Blockers.ToArray(), ProductLedgerInternalOperatorUiPreviewBlocker.ExternalTelemetryOrSyncClaimed);
        CollectionAssert.Contains(result.Blockers.ToArray(), ProductLedgerInternalOperatorUiPreviewBlocker.BillingLicensingCloudClaimed);
        AssertNoPublicExternalRelease(result.ViewModel);
    }

    private static ProductLedgerInternalOperatorUiPreviewRequest ReadyRequest() =>
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
                Section("Runtime Local-Only Gate", "ENABLED_LOCAL_ONLY_INTERNAL", ["feature_flag=enabled:local-only-internal", "default_off=True"]),
                Section("Product Ledger Path Policy", "ACTIVE_LOCAL_ONLY_POLICY_BOUND", ["candidate_id=ledger-candidate-ui-preview-recipe-001"]),
                Section("Bounded Writer Status", "WRITER_BOUNDED_LOCAL_ONLY_SURFACE_READ_ONLY", ["writer_policy_allows_bounded_local_only=True", "operator_surface_write_allowed=false"]),
                Section("Checkpoint / Head Status", "VERIFIED_HEAD_PRESENT", ["same_boundary_trust=true", "tail_deletion_limitation=local-only"]),
                Section("Evidence Gates", "EVIDENCE_REFERENCES_FRESH_AND_WELL_FORMED", ["redaction_before_persistence=True", "retention=True", "authority=True", "replay_failure=True", "rollback_non_rollback=True"]),
                Section("Disabled Actions", "ALL_ACTIONS_DISABLED", ["enable public UI", "run destructive action", "register command handler", "connect provider/cloud", "create DB migration", "enable KMS/WORM", "enable Browser/CDP/WCU/OCR/Recipes live", "release/commercial"]),
                Section("Safe Next Step", "READ_ONLY_AUDIT_OR_STATIC_SCAN_HARDENING_ONLY", ["read-only audit", "property/corpus hardening", "static scan hardening", "operator docs", "manual external review packet"])
            ],
            ActionPreviews:
            [
                new("View local-only diagnostics snapshot", "read-only preview only", "operator visibility without execution authority", ["runtime gate"], Disabled: true, ProductiveCommandId: null, HandlerName: null, CallbackName: null)
            ],
            DisabledActions: ["public UI action", "destructive user-facing action", "product command handler"],
            SafeNextStep: "EXTERNAL_AUDIT_READ_ONLY_THEN_STATIC_SCAN_HARDENING",
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

    private static void AssertNoPublicExternalRelease(ProductLedgerInternalOperatorUiPreviewViewModel viewModel)
    {
        Assert.IsTrue(viewModel.LocalOnly);
        Assert.IsTrue(viewModel.InternalOnly);
        Assert.IsTrue(viewModel.ReadOnly);
        Assert.IsTrue(viewModel.FailClosed);
        Assert.IsFalse(viewModel.PublicUiActionAvailable);
        Assert.IsFalse(viewModel.DestructiveUserFacingActionAvailable);
        Assert.IsFalse(viewModel.ProductCommandHandlerAvailable);
        Assert.IsFalse(viewModel.ProductiveServiceRegistrationAvailable);
        Assert.IsFalse(viewModel.ProviderCloudNetworkAvailable);
        Assert.IsFalse(viewModel.DbMigrationAvailable);
        Assert.IsFalse(viewModel.KmsWormExternalTrustAvailable);
        Assert.IsFalse(viewModel.BrowserCdpWcuOcrRecipesLiveAvailable);
        Assert.IsFalse(viewModel.ExternalTelemetryOrSyncAvailable);
        Assert.IsFalse(viewModel.BillingLicensingCloudAvailable);
        Assert.IsFalse(viewModel.ReleaseCommercialReady);
    }
}
