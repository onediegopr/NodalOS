using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Approval;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("ApprovalHumanReview")]
[TestCategory("PhaseEApprovalHumanReview")]
public sealed class ProductLedgerPublicUiReadOnlyDisabledPreviewTests
{
    [TestMethod]
    public void PublicUiReadOnlyDisabledPreview_FailsClosedByDefault()
    {
        var result = new ProductLedgerPublicUiReadOnlyDisabledPreviewPresenter().Render(null);

        Assert.AreEqual(ProductLedgerPublicUiReadOnlyDisabledPreviewDecision.Rejected, result.Decision);
        CollectionAssert.Contains(result.Blockers.ToArray(), ProductLedgerPublicUiReadOnlyDisabledPreviewBlocker.MissingRequest);
        AssertNoPublicProductExternal(result.ViewModel);
    }

    [TestMethod]
    public void PublicUiReadOnlyDisabledPreview_RendersReadOnlyMockWithAllActionsDisabled()
    {
        var result = new ProductLedgerPublicUiReadOnlyDisabledPreviewPresenter().Render(ReadyRequest());

        Assert.AreEqual(ProductLedgerPublicUiReadOnlyDisabledPreviewDecision.RenderedReadOnlyDisabledMock, result.Decision);
        Assert.AreEqual(0, result.ViewModel.PublicUiReadinessPercentage);
        Assert.AreEqual(0, result.ViewModel.PublicProductCommandHandlerExposurePercentage);
        Assert.IsTrue(result.ViewModel.Actions.Count > 0);
        Assert.IsTrue(result.ViewModel.Actions.All(action => action.Disabled));
        Assert.IsTrue(result.ViewModel.Actions.All(action => !action.Executable));
        Assert.IsTrue(result.ViewModel.Actions.All(action => action.ProductiveCommandId is null));
        Assert.IsTrue(result.ViewModel.Actions.All(action => action.HandlerId is null));
        Assert.IsTrue(result.ViewModel.Actions.All(action => action.CallbackName is null));
        StringAssert.Contains(result.ViewModel.SafeNextStep, "REQUIRES_NEW_EXPLICIT_GO");
        AssertNoPublicProductExternal(result.ViewModel);
    }

    [TestMethod]
    public void PublicUiReadOnlyDisabledPreview_BlocksMissingStaleOrUnsafeReadiness()
    {
        var ready = ReadyRequest();
        var cases = new Dictionary<ProductLedgerPublicUiReadOnlyDisabledPreviewRequest, ProductLedgerPublicUiReadOnlyDisabledPreviewBlocker>
        {
            [ready with { ExplicitReadOnlyDisabledMockScope = false }] = ProductLedgerPublicUiReadOnlyDisabledPreviewBlocker.MissingExplicitReadOnlyDisabledMockScope,
            [ready with { InternalPreview = null }] = ProductLedgerPublicUiReadOnlyDisabledPreviewBlocker.MissingInternalPreview,
            [ready with { HasPublicSurfaceReadinessPacket = false }] = ProductLedgerPublicUiReadOnlyDisabledPreviewBlocker.MissingPublicSurfaceReadinessPacket,
            [ready with { ReadinessPacketFreshAndConsistent = false }] = ProductLedgerPublicUiReadOnlyDisabledPreviewBlocker.StaleOrInconsistentReadinessPacket
        };

        foreach (var testCase in cases)
        {
            AssertRejected(testCase.Key, testCase.Value);
        }
    }

    [TestMethod]
    public void PublicUiReadOnlyDisabledPreview_BlocksPublicProductExternalReleaseAndRawClaims()
    {
        var ready = ReadyRequest();
        var cases = new Dictionary<ProductLedgerPublicUiReadOnlyDisabledPreviewRequest, ProductLedgerPublicUiReadOnlyDisabledPreviewBlocker>
        {
            [ready with { RequestsPublicUiAction = true }] = ProductLedgerPublicUiReadOnlyDisabledPreviewBlocker.PublicUiActionRequested,
            [ready with { RequestsDestructiveAction = true }] = ProductLedgerPublicUiReadOnlyDisabledPreviewBlocker.DestructiveActionRequested,
            [ready with { RequestsProductCommandHandler = true }] = ProductLedgerPublicUiReadOnlyDisabledPreviewBlocker.ProductCommandHandlerRequested,
            [ready with { RequestsProductiveServiceRegistration = true }] = ProductLedgerPublicUiReadOnlyDisabledPreviewBlocker.ProductiveServiceRegistrationRequested,
            [ready with { ClaimsProviderCloudNetwork = true }] = ProductLedgerPublicUiReadOnlyDisabledPreviewBlocker.ProviderCloudNetworkClaimed,
            [ready with { ClaimsDbMigration = true }] = ProductLedgerPublicUiReadOnlyDisabledPreviewBlocker.DbMigrationClaimed,
            [ready with { ClaimsKmsWormExternalTrust = true }] = ProductLedgerPublicUiReadOnlyDisabledPreviewBlocker.KmsWormExternalTrustClaimed,
            [ready with { ClaimsBrowserCdpWcuOcrRecipesLive = true }] = ProductLedgerPublicUiReadOnlyDisabledPreviewBlocker.BrowserCdpWcuOcrRecipesLiveClaimed,
            [ready with { ClaimsReleaseCommercial = true }] = ProductLedgerPublicUiReadOnlyDisabledPreviewBlocker.ReleaseCommercialClaimed,
            [ready with { ClaimsExternalCloudExport = true }] = ProductLedgerPublicUiReadOnlyDisabledPreviewBlocker.ExternalCloudExportClaimed,
            [ready with { ClaimsUnboundedPhysicalExport = true }] = ProductLedgerPublicUiReadOnlyDisabledPreviewBlocker.UnboundedPhysicalExportClaimed,
            [ready with { ClaimsRawPayloadOrSecret = true }] = ProductLedgerPublicUiReadOnlyDisabledPreviewBlocker.RawPayloadOrSecretClaimed
        };

        foreach (var testCase in cases)
        {
            AssertRejected(testCase.Key, testCase.Value);
        }
    }

    [TestMethod]
    public void PublicUiReadOnlyDisabledPreview_BlocksExecutableInternalActionPreview()
    {
        var internalPreview = ReadyInternalPreview();
        var unsafePreview = internalPreview with
        {
            ViewModel = internalPreview.ViewModel with
            {
                ActionPreviews =
                [
                    internalPreview.ViewModel.ActionPreviews[0] with
                    {
                        Disabled = false,
                        ProductiveCommandId = "product.execute",
                        HandlerId = "handler",
                        CallbackName = "callback"
                    }
                ]
            }
        };

        var result = new ProductLedgerPublicUiReadOnlyDisabledPreviewPresenter().Render(ReadyRequest() with { InternalPreview = unsafePreview });

        Assert.AreEqual(ProductLedgerPublicUiReadOnlyDisabledPreviewDecision.Rejected, result.Decision);
        CollectionAssert.Contains(result.Blockers.ToArray(), ProductLedgerPublicUiReadOnlyDisabledPreviewBlocker.ExecutableActionPreviewRejected);
        AssertNoPublicProductExternal(result.ViewModel);
    }

    [TestMethod]
    public void PublicUiReadOnlyDisabledPreview_SourceHasNoEndpointHandlerNetworkDbKmsLiveReleaseOrWrite()
    {
        var source = File.ReadAllText(Path.Combine(
            RepoRoot(),
            "src",
            "OneBrain.Core",
            "Approval",
            "ProductLedgerPublicUiReadOnlyDisabledPreview.cs"));
        var forbiddenFragments = new[]
        {
            "IService" + "Collection",
            "Add" + "Singleton",
            "Add" + "Scoped",
            "Add" + "Transient",
            "IHosted" + "Service",
            "ICommand" + "Handler",
            "Handle" + "Async(",
            "Control" + "ler",
            "Map" + "Post",
            "Map" + "Get",
            "Http" + "Client",
            "Web" + "Socket",
            "Db" + "Context",
            "Migration" + "Builder",
            "Kms" + "Client",
            "Worm" + "Store",
            "File.Write" + "AllText",
            "File.Append" + "AllText",
            "Directory.Create" + "Directory",
            "PublicUiActionAvailable:" + " true",
            "DestructiveActionAvailable:" + " true",
            "ProductCommandHandlerAvailable:" + " true",
            "ProviderCloudNetworkAvailable:" + " true",
            "DbMigrationAvailable:" + " true",
            "KmsWormExternalTrustAvailable:" + " true",
            "BrowserCdpWcuOcrRecipesLiveAvailable:" + " true",
            "ExternalCloudExportAvailable:" + " true",
            "ReleaseCommercialReady:" + " true"
        };

        foreach (var fragment in forbiddenFragments)
        {
            Assert.IsFalse(source.Contains(fragment, StringComparison.Ordinal), fragment);
        }

        StringAssert.Contains(source, "READ_ONLY_DISABLED_MOCK_PREVIEW_READY");
        StringAssert.Contains(source, "DISABLED_MOCK_ONLY");
    }

    private static ProductLedgerPublicUiReadOnlyDisabledPreviewRequest ReadyRequest() =>
        new(
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
            ClaimsRawPayloadOrSecret: false);

    private static ProductLedgerInternalOperatorUiPreviewResult ReadyInternalPreview() =>
        new ProductLedgerInternalOperatorUiPresenter().Render(ReadyInternalRequest());

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
                Section("Product Ledger Path Policy", "ACTIVE_LOCAL_ONLY_POLICY_BOUND", ["candidate_id=ledger-public-mock-001"]),
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

    private static void AssertRejected(
        ProductLedgerPublicUiReadOnlyDisabledPreviewRequest request,
        ProductLedgerPublicUiReadOnlyDisabledPreviewBlocker blocker)
    {
        var result = new ProductLedgerPublicUiReadOnlyDisabledPreviewPresenter().Render(request);

        Assert.AreEqual(ProductLedgerPublicUiReadOnlyDisabledPreviewDecision.Rejected, result.Decision, blocker.ToString());
        CollectionAssert.Contains(result.Blockers.ToArray(), blocker, blocker.ToString());
        AssertNoPublicProductExternal(result.ViewModel);
    }

    private static void AssertNoPublicProductExternal(ProductLedgerPublicUiReadOnlyDisabledPreviewViewModel viewModel)
    {
        Assert.IsTrue(viewModel.ReadOnly);
        Assert.IsTrue(viewModel.DisabledMockOnly);
        Assert.IsTrue(viewModel.FailClosed);
        Assert.IsFalse(viewModel.PublicUiActionAvailable);
        Assert.IsFalse(viewModel.DestructiveActionAvailable);
        Assert.IsFalse(viewModel.ProductCommandHandlerAvailable);
        Assert.IsFalse(viewModel.ProductiveServiceRegistrationAvailable);
        Assert.IsFalse(viewModel.ProviderCloudNetworkAvailable);
        Assert.IsFalse(viewModel.DbMigrationAvailable);
        Assert.IsFalse(viewModel.KmsWormExternalTrustAvailable);
        Assert.IsFalse(viewModel.BrowserCdpWcuOcrRecipesLiveAvailable);
        Assert.IsFalse(viewModel.ExternalTelemetryOrSyncAvailable);
        Assert.IsFalse(viewModel.BillingLicensingCloudAvailable);
        Assert.IsFalse(viewModel.ExternalCloudExportAvailable);
        Assert.IsFalse(viewModel.UnboundedPhysicalExportAvailable);
        Assert.IsFalse(viewModel.RawPayloadOrSecretAvailable);
        Assert.IsFalse(viewModel.ReleaseCommercialReady);
    }

    private static string RepoRoot()
    {
        var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(
                directory.FullName,
                "src",
                "OneBrain.Core",
                "Approval",
                "ProductLedgerPublicUiReadOnlyDisabledPreview.cs")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        Assert.Fail("repo root not found");
        return string.Empty;
    }
}
