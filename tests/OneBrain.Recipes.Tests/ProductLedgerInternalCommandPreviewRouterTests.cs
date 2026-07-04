using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Approval;

namespace OneBrain.Recipes.Tests;

[TestClass]
[TestCategory("ApprovalHumanReview")]
[TestCategory("PhaseEApprovalHumanReview")]
public sealed class ProductLedgerInternalCommandPreviewRouterTests
{
    [TestMethod]
    public void InternalCommandPreviewRouter_MapsOperatorUiActionsToNoOpReadOnlyPreviewsRecipe()
    {
        var ui = new ProductLedgerInternalOperatorUiPresenter().Render(ReadyUiRequest());
        var previews = new ProductLedgerInternalCommandPreviewRouter().PreviewAllowedCommands(ui.ViewModel);
        var mapped = new ProductLedgerInternalOperatorUiPresenter().Render(ReadyUiRequest() with { CommandPreviews = previews });

        Assert.AreEqual(ProductLedgerInternalOperatorUiPreviewDecision.RenderedPreview, mapped.Decision);
        Assert.AreEqual(11, previews.Count);
        Assert.AreEqual(11, mapped.ViewModel.ActionPreviews.Count);
        Assert.IsTrue(previews.All(result => result.Decision == ProductLedgerInternalCommandPreviewDecision.PreviewedNoOpReadOnly));
        Assert.IsTrue(previews.All(result => result.Preview.Disabled));
        Assert.IsTrue(previews.All(result => !result.Preview.Executable));
        Assert.IsTrue(previews.All(result => result.Preview.HandlerId is null));
        Assert.IsTrue(previews.All(result => result.Preview.CallbackName is null));
        Assert.IsTrue(mapped.ViewModel.ActionPreviews.All(action => action.Disabled));
        AssertNoProductExternalRelease(previews[0]);
        AssertNoPublicUiOrRelease(mapped.ViewModel);
    }

    [TestMethod]
    public void InternalCommandPreviewRouter_BlocksForbiddenCommandRecipe()
    {
        var result = new ProductLedgerInternalCommandPreviewRouter().Preview(
            ReadyRequest(ProductLedgerInternalCommandKind.RegisterCommandHandler) with
            {
                RequestsProductCommandHandler = true,
                ClaimsProviderCloudNetwork = true,
                ClaimsReleaseCommercial = true
            });

        Assert.AreEqual(ProductLedgerInternalCommandPreviewDecision.Rejected, result.Decision);
        CollectionAssert.Contains(result.Blockers.ToArray(), ProductLedgerInternalCommandPreviewBlocker.ProductCommandHandlerRequested);
        CollectionAssert.Contains(result.Blockers.ToArray(), ProductLedgerInternalCommandPreviewBlocker.ProviderCloudNetworkClaimed);
        CollectionAssert.Contains(result.Blockers.ToArray(), ProductLedgerInternalCommandPreviewBlocker.ReleaseCommercialClaimed);
        Assert.IsTrue(result.Preview.Disabled);
        Assert.IsFalse(result.Preview.Executable);
        Assert.IsNull(result.Preview.HandlerId);
        Assert.IsNull(result.Preview.CallbackName);
        AssertNoProductExternalRelease(result);
    }

    private static ProductLedgerInternalCommandPreviewRequest ReadyRequest(ProductLedgerInternalCommandKind command) =>
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

    private static ProductLedgerInternalOperatorUiPreviewRequest ReadyUiRequest() =>
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
                Section("Product Ledger Path Policy", "ACTIVE_LOCAL_ONLY_POLICY_BOUND", ["candidate_id=ledger-candidate-command-router-recipe-001"]),
                Section("Bounded Writer Status", "WRITER_BOUNDED_LOCAL_ONLY_SURFACE_READ_ONLY", ["operator_surface_write_allowed=false"]),
                Section("Checkpoint / Head Status", "VERIFIED_HEAD_PRESENT", ["same_boundary_trust=true"]),
                Section("Evidence Gates", "EVIDENCE_REFERENCES_FRESH_AND_WELL_FORMED", ["redaction_before_persistence=True", "retention=True", "authority=True"]),
                Section("Disabled Actions", "ALL_ACTIONS_DISABLED", ["enable public UI", "run destructive action", "register command handler"]),
                Section("Safe Next Step", "READ_ONLY_AUDIT_OR_STATIC_SCAN_HARDENING_ONLY", ["read-only audit", "static scan hardening"])
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

    private static void AssertNoProductExternalRelease(ProductLedgerInternalCommandPreviewResult result)
    {
        Assert.IsTrue(result.LocalOnly);
        Assert.IsTrue(result.InternalOnly);
        Assert.IsTrue(result.NoOp);
        Assert.IsTrue(result.ReadOnly);
        Assert.IsFalse(result.PublicUiActionAvailable);
        Assert.IsFalse(result.DestructiveActionAvailable);
        Assert.IsFalse(result.ProductCommandHandlerAvailable);
        Assert.IsFalse(result.ProviderCloudNetworkAvailable);
        Assert.IsFalse(result.DbMigrationAvailable);
        Assert.IsFalse(result.KmsWormExternalTrustAvailable);
        Assert.IsFalse(result.BrowserCdpWcuOcrRecipesLiveAvailable);
        Assert.IsFalse(result.ReleaseCommercialReady);
    }

    private static void AssertNoPublicUiOrRelease(ProductLedgerInternalOperatorUiPreviewViewModel viewModel)
    {
        Assert.IsTrue(viewModel.LocalOnly);
        Assert.IsTrue(viewModel.InternalOnly);
        Assert.IsTrue(viewModel.ReadOnly);
        Assert.IsFalse(viewModel.PublicUiActionAvailable);
        Assert.IsFalse(viewModel.DestructiveUserFacingActionAvailable);
        Assert.IsFalse(viewModel.ProductCommandHandlerAvailable);
        Assert.IsFalse(viewModel.ProviderCloudNetworkAvailable);
        Assert.IsFalse(viewModel.DbMigrationAvailable);
        Assert.IsFalse(viewModel.KmsWormExternalTrustAvailable);
        Assert.IsFalse(viewModel.BrowserCdpWcuOcrRecipesLiveAvailable);
        Assert.IsFalse(viewModel.ReleaseCommercialReady);
    }
}
