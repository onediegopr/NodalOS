using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Approval;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("ApprovalHumanReview")]
[TestCategory("PhaseEApprovalHumanReview")]
public sealed class ProductLedgerInternalCommandPreviewRouterTests
{
    [TestMethod]
    public void InternalCommandPreviewRouter_FailsClosedOnUnknownAndCorruptCommands()
    {
        var router = new ProductLedgerInternalCommandPreviewRouter();

        var missing = router.Preview(null);
        var corruptMissingKind = router.Preview(ReadyRequest(ProductLedgerInternalCommandKind.ViewDiagnostics) with { CommandKind = null });
        var corruptMissingRaw = router.Preview(ReadyRequest(ProductLedgerInternalCommandKind.ViewDiagnostics) with { RawCommandName = " " });
        var unknown = router.Preview(
            ReadyRequest((ProductLedgerInternalCommandKind)999) with { RawCommandName = "UnknownCommand" });

        AssertRejected(missing, ProductLedgerInternalCommandPreviewBlocker.MissingRequest);
        AssertRejected(corruptMissingKind, ProductLedgerInternalCommandPreviewBlocker.CorruptCommand);
        AssertRejected(corruptMissingRaw, ProductLedgerInternalCommandPreviewBlocker.CorruptCommand);
        AssertRejected(unknown, ProductLedgerInternalCommandPreviewBlocker.UnknownCommand);
    }

    [TestMethod]
    public void InternalCommandPreviewRouter_AllowsKnownCommandsOnlyAsDisabledNoOpReadOnlyPreviews()
    {
        var router = new ProductLedgerInternalCommandPreviewRouter();
        foreach (var command in AllowedCommands())
        {
            var result = router.Preview(ReadyRequest(command));

            Assert.AreEqual(ProductLedgerInternalCommandPreviewDecision.PreviewedNoOpReadOnly, result.Decision, command.ToString());
            Assert.AreEqual(0, result.Blockers.Count, command.ToString());
            Assert.AreEqual(command, result.Preview.CommandKind);
            StringAssert.Contains(result.Preview.CommandId, "preview-only.product-ledger");
            Assert.IsTrue(result.Preview.Disabled);
            Assert.IsFalse(result.Preview.Executable);
            Assert.IsNull(result.Preview.ProductiveCommandId);
            Assert.IsNull(result.Preview.HandlerId);
            Assert.IsNull(result.Preview.CallbackName);
            Assert.IsTrue(result.Preview.RequiredEvidence.Count > 0);
            AssertNoProductExternalRelease(result);
        }
    }

    [TestMethod]
    public void InternalCommandPreviewRouter_BlocksForbiddenCommandsExplicitly()
    {
        var cases = new Dictionary<ProductLedgerInternalCommandKind, ProductLedgerInternalCommandPreviewBlocker>
        {
            [ProductLedgerInternalCommandKind.EnablePublicUi] = ProductLedgerInternalCommandPreviewBlocker.PublicUiActionRequested,
            [ProductLedgerInternalCommandKind.ExecuteAction] = ProductLedgerInternalCommandPreviewBlocker.DestructiveActionRequested,
            [ProductLedgerInternalCommandKind.DestructiveWrite] = ProductLedgerInternalCommandPreviewBlocker.DestructiveActionRequested,
            [ProductLedgerInternalCommandKind.RegisterCommandHandler] = ProductLedgerInternalCommandPreviewBlocker.ProductCommandHandlerRequested,
            [ProductLedgerInternalCommandKind.RegisterProductDI] = ProductLedgerInternalCommandPreviewBlocker.ProductiveServiceRegistrationRequested,
            [ProductLedgerInternalCommandKind.ConnectProvider] = ProductLedgerInternalCommandPreviewBlocker.ProviderCloudNetworkClaimed,
            [ProductLedgerInternalCommandKind.EnableCloud] = ProductLedgerInternalCommandPreviewBlocker.ProviderCloudNetworkClaimed,
            [ProductLedgerInternalCommandKind.RunMigration] = ProductLedgerInternalCommandPreviewBlocker.DbMigrationClaimed,
            [ProductLedgerInternalCommandKind.EnableKms] = ProductLedgerInternalCommandPreviewBlocker.KmsWormExternalTrustClaimed,
            [ProductLedgerInternalCommandKind.EnableWorm] = ProductLedgerInternalCommandPreviewBlocker.KmsWormExternalTrustClaimed,
            [ProductLedgerInternalCommandKind.EnableExternalTrust] = ProductLedgerInternalCommandPreviewBlocker.KmsWormExternalTrustClaimed,
            [ProductLedgerInternalCommandKind.RunBrowserCdp] = ProductLedgerInternalCommandPreviewBlocker.BrowserCdpWcuOcrRecipesLiveClaimed,
            [ProductLedgerInternalCommandKind.RunWcu] = ProductLedgerInternalCommandPreviewBlocker.BrowserCdpWcuOcrRecipesLiveClaimed,
            [ProductLedgerInternalCommandKind.RunOcr] = ProductLedgerInternalCommandPreviewBlocker.BrowserCdpWcuOcrRecipesLiveClaimed,
            [ProductLedgerInternalCommandKind.RunRecipesLive] = ProductLedgerInternalCommandPreviewBlocker.BrowserCdpWcuOcrRecipesLiveClaimed,
            [ProductLedgerInternalCommandKind.Release] = ProductLedgerInternalCommandPreviewBlocker.ReleaseCommercialClaimed,
            [ProductLedgerInternalCommandKind.CommercialLaunch] = ProductLedgerInternalCommandPreviewBlocker.ReleaseCommercialClaimed,
            [ProductLedgerInternalCommandKind.SyncExternal] = ProductLedgerInternalCommandPreviewBlocker.ExternalTelemetryOrSyncClaimed,
            [ProductLedgerInternalCommandKind.TelemetryExternal] = ProductLedgerInternalCommandPreviewBlocker.ExternalTelemetryOrSyncClaimed,
            [ProductLedgerInternalCommandKind.BillingLicensingCloud] = ProductLedgerInternalCommandPreviewBlocker.BillingLicensingCloudClaimed
        };

        var router = new ProductLedgerInternalCommandPreviewRouter();
        foreach (var testCase in cases)
        {
            AssertRejected(router.Preview(ReadyRequest(testCase.Key)), testCase.Value);
        }
    }

    [TestMethod]
    public void InternalCommandPreviewRouter_BlocksProductiveClaimsAndWriterExecutionOutsidePolicy()
    {
        var ready = ReadyRequest(ProductLedgerInternalCommandKind.ViewDiagnostics);
        var cases = new Dictionary<ProductLedgerInternalCommandPreviewRequest, ProductLedgerInternalCommandPreviewBlocker>
        {
            [ready with { ExplicitInternalLocalOnlyNoOpReadOnlyScope = false }] = ProductLedgerInternalCommandPreviewBlocker.MissingExplicitInternalLocalOnlyNoOpReadOnlyScope,
            [ready with { RequestsPublicUiAction = true }] = ProductLedgerInternalCommandPreviewBlocker.PublicUiActionRequested,
            [ready with { RequestsDestructiveAction = true }] = ProductLedgerInternalCommandPreviewBlocker.DestructiveActionRequested,
            [ready with { RequestsProductCommandHandler = true }] = ProductLedgerInternalCommandPreviewBlocker.ProductCommandHandlerRequested,
            [ready with { RequestsProductiveServiceRegistration = true }] = ProductLedgerInternalCommandPreviewBlocker.ProductiveServiceRegistrationRequested,
            [ready with { ClaimsProviderCloudNetwork = true }] = ProductLedgerInternalCommandPreviewBlocker.ProviderCloudNetworkClaimed,
            [ready with { ClaimsDbMigration = true }] = ProductLedgerInternalCommandPreviewBlocker.DbMigrationClaimed,
            [ready with { ClaimsKmsWormExternalTrust = true }] = ProductLedgerInternalCommandPreviewBlocker.KmsWormExternalTrustClaimed,
            [ready with { ClaimsBrowserCdpWcuOcrRecipesLive = true }] = ProductLedgerInternalCommandPreviewBlocker.BrowserCdpWcuOcrRecipesLiveClaimed,
            [ready with { ClaimsReleaseCommercial = true }] = ProductLedgerInternalCommandPreviewBlocker.ReleaseCommercialClaimed,
            [ready with { ClaimsExternalTelemetryOrSync = true }] = ProductLedgerInternalCommandPreviewBlocker.ExternalTelemetryOrSyncClaimed,
            [ready with { ClaimsBillingLicensingCloud = true }] = ProductLedgerInternalCommandPreviewBlocker.BillingLicensingCloudClaimed,
            [ready with { ClaimsWriterExecutionOutsideValidatedLocalOnlyPolicy = true }] = ProductLedgerInternalCommandPreviewBlocker.WriterExecutionOutsideValidatedLocalOnlyPolicyClaimed
        };

        var router = new ProductLedgerInternalCommandPreviewRouter();
        foreach (var testCase in cases)
        {
            AssertRejected(router.Preview(testCase.Key), testCase.Value);
        }
    }

    [TestMethod]
    public void InternalCommandPreviewRouter_UiPreviewMappingPreservesDisabledNoOpStatus()
    {
        var ui = new ProductLedgerInternalOperatorUiPresenter().Render(ReadyUiRequest());
        var previews = new ProductLedgerInternalCommandPreviewRouter().PreviewAllowedCommands(ui.ViewModel);
        var mapped = new ProductLedgerInternalOperatorUiPresenter().Render(ReadyUiRequest() with { CommandPreviews = previews });

        Assert.AreEqual(ProductLedgerInternalOperatorUiPreviewDecision.RenderedPreview, mapped.Decision);
        Assert.AreEqual(AllowedCommands().Length, mapped.ViewModel.ActionPreviews.Count);
        foreach (var action in mapped.ViewModel.ActionPreviews)
        {
            Assert.IsTrue(action.Disabled);
            Assert.IsNull(action.ProductiveCommandId);
            Assert.IsNull(action.HandlerId);
            Assert.IsNull(action.CallbackName);
            StringAssert.Contains(action.ActionId, "preview-only.product-ledger");
        }
    }

    [TestMethod]
    public void InternalCommandPreviewRouter_UiPreviewMappingFailsClosedOnExecutablePreview()
    {
        var ui = new ProductLedgerInternalOperatorUiPresenter().Render(ReadyUiRequest());
        var previews = new ProductLedgerInternalCommandPreviewRouter().PreviewAllowedCommands(ui.ViewModel).ToArray();
        previews[0] = previews[0] with { Preview = previews[0].Preview with { Executable = true, HandlerId = "handler" } };

        var mapped = new ProductLedgerInternalOperatorUiPresenter().Render(ReadyUiRequest() with { CommandPreviews = previews });

        Assert.AreEqual(ProductLedgerInternalOperatorUiPreviewDecision.Rejected, mapped.Decision);
        CollectionAssert.Contains(mapped.Blockers.ToArray(), ProductLedgerInternalOperatorUiPreviewBlocker.UnsafeCommandPreviewRouter);
        Assert.IsTrue(mapped.ViewModel.ActionPreviews.All(action => action.Disabled));
    }

    [TestMethod]
    public void InternalCommandPreviewRouter_SourceHasNoProductHandlerPublicActionNetworkDbKmsLiveReleaseOrOverclaim()
    {
        var source = File.ReadAllText(Path.Combine(
            RepoRoot(),
            "src",
            "OneBrain.Core",
            "Approval",
            "ProductLedgerInternalCommandPreviewRouter.cs"));
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
            "Executable:" + " true",
            "PublicUiActionAvailable:" + " true",
            "DestructiveActionAvailable:" + " true",
            "ProductCommandHandlerAvailable:" + " true",
            "ProductiveServiceRegistrationAvailable:" + " true",
            "ProviderCloudNetworkAvailable:" + " true",
            "DbMigrationAvailable:" + " true",
            "KmsWormExternalTrustAvailable:" + " true",
            "BrowserCdpWcuOcrRecipesLiveAvailable:" + " true",
            "ExternalTelemetryOrSyncAvailable:" + " true",
            "BillingLicensingCloudAvailable:" + " true",
            "ReleaseCommercialReady:" + " true",
            "product" + "-ready",
            "public" + "-ready"
        };

        foreach (var fragment in forbiddenFragments)
        {
            Assert.IsFalse(source.Contains(fragment, StringComparison.OrdinalIgnoreCase), fragment);
        }

        StringAssert.Contains(source, "NO_OP");
        StringAssert.Contains(source, "PREVIEW_ONLY");
    }

    private static ProductLedgerInternalCommandKind[] AllowedCommands() =>
    [
        ProductLedgerInternalCommandKind.ViewDiagnostics,
        ProductLedgerInternalCommandKind.ViewLedgerReadiness,
        ProductLedgerInternalCommandKind.ViewRuntimeGateStatus,
        ProductLedgerInternalCommandKind.ViewCheckpointHeadStatus,
        ProductLedgerInternalCommandKind.ViewEvidenceGates,
        ProductLedgerInternalCommandKind.ExportDisabledLocalReportPreview,
        ProductLedgerInternalCommandKind.RequestExternalAuditPreview,
        ProductLedgerInternalCommandKind.StaticScanPreview,
        ProductLedgerInternalCommandKind.PropertyCorpusHardeningPreview,
        ProductLedgerInternalCommandKind.LocalReportPreviewInMemory,
        ProductLedgerInternalCommandKind.LocalReportPhysicalExportBoundedInternal
    ];

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
                Section("Product Ledger Path Policy", "ACTIVE_LOCAL_ONLY_POLICY_BOUND", ["candidate_id=ledger-candidate-command-router-001"]),
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

    private static void AssertRejected(
        ProductLedgerInternalCommandPreviewResult result,
        ProductLedgerInternalCommandPreviewBlocker blocker)
    {
        Assert.AreEqual(ProductLedgerInternalCommandPreviewDecision.Rejected, result.Decision, blocker.ToString());
        CollectionAssert.Contains(result.Blockers.ToArray(), blocker, blocker.ToString());
        StringAssert.Contains(result.StatusText, "REJECTED");
        Assert.IsTrue(result.Preview.Disabled);
        Assert.IsFalse(result.Preview.Executable);
        Assert.IsNull(result.Preview.ProductiveCommandId);
        Assert.IsNull(result.Preview.HandlerId);
        Assert.IsNull(result.Preview.CallbackName);
        AssertNoProductExternalRelease(result);
    }

    private static void AssertNoProductExternalRelease(ProductLedgerInternalCommandPreviewResult result)
    {
        Assert.IsTrue(result.LocalOnly);
        Assert.IsTrue(result.InternalOnly);
        Assert.IsTrue(result.NoOp);
        Assert.IsTrue(result.ReadOnly);
        Assert.IsTrue(result.NonDestructive);
        Assert.IsTrue(result.FailClosed);
        Assert.IsFalse(result.PublicUiActionAvailable);
        Assert.IsFalse(result.DestructiveActionAvailable);
        Assert.IsFalse(result.ProductCommandHandlerAvailable);
        Assert.IsFalse(result.ProductiveServiceRegistrationAvailable);
        Assert.IsFalse(result.ProviderCloudNetworkAvailable);
        Assert.IsFalse(result.DbMigrationAvailable);
        Assert.IsFalse(result.KmsWormExternalTrustAvailable);
        Assert.IsFalse(result.BrowserCdpWcuOcrRecipesLiveAvailable);
        Assert.IsFalse(result.ExternalTelemetryOrSyncAvailable);
        Assert.IsFalse(result.BillingLicensingCloudAvailable);
        Assert.IsFalse(result.ReleaseCommercialReady);
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
                "ProductLedgerInternalCommandPreviewRouter.cs")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        Assert.Fail("repo root not found");
        return string.Empty;
    }
}
