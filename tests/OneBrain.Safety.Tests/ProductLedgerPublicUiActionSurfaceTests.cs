using System.Security.Cryptography;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Approval;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("ApprovalHumanReview")]
[TestCategory("PhaseEApprovalHumanReview")]
public sealed class ProductLedgerPublicUiActionSurfaceTests
{
    [TestMethod]
    [TestCategory("NodalOsTier1Safety")]
    [TestCategory("ProductLedger")]
    [TestCategory("PublicProductBlock")]
    public void PublicUiActionSurface_FailsClosedByDefaultUnknownAndCorrupt()
    {
        var surface = new ProductLedgerPublicUiActionSurface();
        var missing = surface.Execute(null);
        var corruptMissingKind = surface.Execute(ReadyRequest(ProductLedgerPublicUiActionKind.ViewDiagnostics) with { ActionKind = null });
        var corruptMissingRaw = surface.Execute(ReadyRequest(ProductLedgerPublicUiActionKind.ViewDiagnostics) with { RawActionName = " " });
        var unknown = surface.Execute(ReadyRequest((ProductLedgerPublicUiActionKind)999) with { RawActionName = "UnknownAction" });

        AssertRejected(missing, ProductLedgerPublicUiActionBlocker.MissingRequest);
        AssertRejected(corruptMissingKind, ProductLedgerPublicUiActionBlocker.CorruptAction);
        AssertRejected(corruptMissingRaw, ProductLedgerPublicUiActionBlocker.CorruptAction);
        AssertRejected(unknown, ProductLedgerPublicUiActionBlocker.UnknownAction);
    }

    [TestMethod]
    public void PublicUiActionSurface_AllowsOnlyLocalOnlyNonDestructiveReadActions()
    {
        var allowed = new[]
        {
            ProductLedgerPublicUiActionKind.ViewDiagnostics,
            ProductLedgerPublicUiActionKind.ViewLedgerReadiness,
            ProductLedgerPublicUiActionKind.ViewRuntimeGateStatus,
            ProductLedgerPublicUiActionKind.ViewCheckpointHeadStatus,
            ProductLedgerPublicUiActionKind.ViewEvidenceGates,
            ProductLedgerPublicUiActionKind.StaticScanPreview,
            ProductLedgerPublicUiActionKind.RequestExternalAuditPreview
        };

        foreach (var action in allowed)
        {
            var result = new ProductLedgerPublicUiActionSurface().Execute(ReadyRequest(action));

            Assert.AreEqual(ProductLedgerPublicUiActionDecision.CompletedLocalOnlyNonDestructive, result.Decision, action.ToString());
            Assert.AreEqual(action, result.ActionKind);
            Assert.IsNotNull(result.RouterPreview);
            Assert.IsNotNull(result.CommandResult);
            Assert.IsFalse(result.PhysicalExportCreated);
            Assert.IsFalse(result.FileWritePerformed);
            AssertNoDangerousSurface(result);
            StringAssert.Contains(result.StatusText, "LOCAL_ONLY");
            StringAssert.Contains(result.StatusText, "NON_DESTRUCTIVE");
        }
    }

    [TestMethod]
    [TestCategory("NodalOsTier1Safety")]
    [TestCategory("ProductLedger")]
    [TestCategory("PublicProductBlock")]
    [TestCategory("CommandExecutionBlock")]
    [TestCategory("ReleaseCommercialBlock")]
    public void PublicUiActionSurface_BlocksDangerousActionCommands()
    {
        var cases = new Dictionary<ProductLedgerPublicUiActionKind, ProductLedgerPublicUiActionBlocker>
        {
            [ProductLedgerPublicUiActionKind.EnablePublicUi] = ProductLedgerPublicUiActionBlocker.ActionNotAllowed,
            [ProductLedgerPublicUiActionKind.ExecuteAction] = ProductLedgerPublicUiActionBlocker.GenericExecuteActionRequested,
            [ProductLedgerPublicUiActionKind.DestructiveWrite] = ProductLedgerPublicUiActionBlocker.PublicDestructiveActionRequested,
            [ProductLedgerPublicUiActionKind.RegisterCommandHandler] = ProductLedgerPublicUiActionBlocker.ActionNotAllowed,
            [ProductLedgerPublicUiActionKind.RegisterProductDI] = ProductLedgerPublicUiActionBlocker.ActionNotAllowed,
            [ProductLedgerPublicUiActionKind.ConnectProvider] = ProductLedgerPublicUiActionBlocker.ProviderCloudNetworkClaimed,
            [ProductLedgerPublicUiActionKind.EnableCloud] = ProductLedgerPublicUiActionBlocker.ProviderCloudNetworkClaimed,
            [ProductLedgerPublicUiActionKind.RunMigration] = ProductLedgerPublicUiActionBlocker.DbMigrationClaimed,
            [ProductLedgerPublicUiActionKind.EnableKms] = ProductLedgerPublicUiActionBlocker.KmsWormExternalTrustClaimed,
            [ProductLedgerPublicUiActionKind.EnableWorm] = ProductLedgerPublicUiActionBlocker.KmsWormExternalTrustClaimed,
            [ProductLedgerPublicUiActionKind.EnableExternalTrust] = ProductLedgerPublicUiActionBlocker.KmsWormExternalTrustClaimed,
            [ProductLedgerPublicUiActionKind.RunBrowserCdp] = ProductLedgerPublicUiActionBlocker.BrowserCdpWcuOcrRecipesLiveClaimed,
            [ProductLedgerPublicUiActionKind.RunWcu] = ProductLedgerPublicUiActionBlocker.BrowserCdpWcuOcrRecipesLiveClaimed,
            [ProductLedgerPublicUiActionKind.RunOcr] = ProductLedgerPublicUiActionBlocker.BrowserCdpWcuOcrRecipesLiveClaimed,
            [ProductLedgerPublicUiActionKind.RunRecipesLive] = ProductLedgerPublicUiActionBlocker.BrowserCdpWcuOcrRecipesLiveClaimed,
            [ProductLedgerPublicUiActionKind.Release] = ProductLedgerPublicUiActionBlocker.ReleaseCommercialClaimed,
            [ProductLedgerPublicUiActionKind.CommercialLaunch] = ProductLedgerPublicUiActionBlocker.ReleaseCommercialClaimed,
            [ProductLedgerPublicUiActionKind.SyncExternal] = ProductLedgerPublicUiActionBlocker.ExternalTelemetryOrSyncClaimed,
            [ProductLedgerPublicUiActionKind.TelemetryExternal] = ProductLedgerPublicUiActionBlocker.ExternalTelemetryOrSyncClaimed,
            [ProductLedgerPublicUiActionKind.BillingLicensingCloud] = ProductLedgerPublicUiActionBlocker.BillingLicensingCloudClaimed,
            [ProductLedgerPublicUiActionKind.UnboundedExport] = ProductLedgerPublicUiActionBlocker.UnboundedPhysicalExportClaimed,
            [ProductLedgerPublicUiActionKind.ExternalExport] = ProductLedgerPublicUiActionBlocker.ExternalCloudExportClaimed,
            [ProductLedgerPublicUiActionKind.Delete] = ProductLedgerPublicUiActionBlocker.PublicDestructiveActionRequested,
            [ProductLedgerPublicUiActionKind.OverwriteUnsafe] = ProductLedgerPublicUiActionBlocker.UnboundedPhysicalExportClaimed
        };

        foreach (var testCase in cases)
        {
            AssertRejected(new ProductLedgerPublicUiActionSurface().Execute(ReadyRequest(testCase.Key)), testCase.Value);
        }
    }

    [TestMethod]
    [TestCategory("NodalOsTier1Safety")]
    [TestCategory("ProductLedger")]
    [TestCategory("PublicProductBlock")]
    [TestCategory("CommandExecutionBlock")]
    [TestCategory("ReleaseCommercialBlock")]
    public void PublicUiActionSurface_BlocksUnsafeClaimsAndPreviewState()
    {
        var ready = ReadyRequest(ProductLedgerPublicUiActionKind.ViewDiagnostics);
        var claimCases = new Dictionary<ProductLedgerPublicUiActionRequest, ProductLedgerPublicUiActionBlocker>
        {
            [ready with { ExplicitPublicLocalOnlyNonDestructiveScope = false }] = ProductLedgerPublicUiActionBlocker.MissingExplicitPublicLocalOnlyNonDestructiveScope,
            [ready with { PublicReadOnlyDisabledPreview = null }] = ProductLedgerPublicUiActionBlocker.MissingPublicReadOnlyDisabledPreview,
            [ready with { ClaimsRawPayloadOrSecret = true }] = ProductLedgerPublicUiActionBlocker.RawPayloadOrSecretClaimed,
            [ready with { RequestsDestructiveAction = true }] = ProductLedgerPublicUiActionBlocker.PublicDestructiveActionRequested,
            [ready with { RequestsGenericExecuteAction = true }] = ProductLedgerPublicUiActionBlocker.GenericExecuteActionRequested,
            [ready with { RequestsProductiveServiceRegistration = true }] = ProductLedgerPublicUiActionBlocker.ProductiveServiceRegistrationRequested,
            [ready with { ClaimsProviderCloudNetwork = true }] = ProductLedgerPublicUiActionBlocker.ProviderCloudNetworkClaimed,
            [ready with { ClaimsDbMigration = true }] = ProductLedgerPublicUiActionBlocker.DbMigrationClaimed,
            [ready with { ClaimsKmsWormExternalTrust = true }] = ProductLedgerPublicUiActionBlocker.KmsWormExternalTrustClaimed,
            [ready with { ClaimsBrowserCdpWcuOcrRecipesLive = true }] = ProductLedgerPublicUiActionBlocker.BrowserCdpWcuOcrRecipesLiveClaimed,
            [ready with { ClaimsReleaseCommercial = true }] = ProductLedgerPublicUiActionBlocker.ReleaseCommercialClaimed,
            [ready with { ClaimsExternalTelemetryOrSync = true }] = ProductLedgerPublicUiActionBlocker.ExternalTelemetryOrSyncClaimed,
            [ready with { ClaimsBillingLicensingCloud = true }] = ProductLedgerPublicUiActionBlocker.BillingLicensingCloudClaimed,
            [ready with { ClaimsUnboundedPhysicalExport = true }] = ProductLedgerPublicUiActionBlocker.UnboundedPhysicalExportClaimed,
            [ready with { ClaimsExternalCloudExport = true }] = ProductLedgerPublicUiActionBlocker.ExternalCloudExportClaimed,
            [ready with { RequestsDeleteOrUnsafeOverwrite = true }] = ProductLedgerPublicUiActionBlocker.DeleteOrUnsafeOverwriteRequested,
            [ready with { PublicReadOnlyDisabledPreview = UnsafePublicPreview() }] = ProductLedgerPublicUiActionBlocker.UnsafePublicReadOnlyDisabledPreview
        };

        foreach (var testCase in claimCases)
        {
            AssertRejected(new ProductLedgerPublicUiActionSurface().Execute(testCase.Key), testCase.Value);
        }
    }

    [TestMethod]
    [TestCategory("NodalOsTier1Safety")]
    [TestCategory("ProductLedger")]
    [TestCategory("PublicProductBlock")]
    [TestCategory("CommandExecutionBlock")]
    public void PublicUiActionSurface_RejectsCasingWhitespaceAndUnsafeExportCorpus()
    {
        using var fixture = ExportFixture.Create();
        var ready = ReadyRequest(ProductLedgerPublicUiActionKind.ViewDiagnostics);
        var wrongCase = ready with { RawActionName = "viewdiagnostics" };
        var wrongCommand = ready with { RawActionName = " ExecuteAction " };
        var safeWhitespace = ready with { RawActionName = " ViewDiagnostics " };
        var unsafeContent = ReadyRequest(ProductLedgerPublicUiActionKind.LocalReportPhysicalExportBoundedInternal) with
        {
            LocalReportExportRequest = ReadyExportRequest(
                fixture.AllowedRoot,
                Path.Combine(fixture.AllowedRoot, "reports", "product-ledger-public-local-action.json")) with
            {
                ReportContent = "raw payload should not export"
            }
        };
        var unsafeMetadata = ReadyRequest(ProductLedgerPublicUiActionKind.LocalReportPhysicalExportBoundedInternal) with
        {
            LocalReportExportRequest = ReadyExportRequest(
                fixture.AllowedRoot,
                Path.Combine(fixture.AllowedRoot, "reports", "product-ledger-public-local-action-metadata.json")) with
            {
                EvidenceMetadata = new Dictionary<string, string> { ["token"] = "redacted" }
            }
        };

        AssertRejected(new ProductLedgerPublicUiActionSurface().Execute(wrongCase), ProductLedgerPublicUiActionBlocker.UnknownAction);
        AssertRejected(new ProductLedgerPublicUiActionSurface().Execute(wrongCommand), ProductLedgerPublicUiActionBlocker.UnknownAction);

        var whitespaceResult = new ProductLedgerPublicUiActionSurface().Execute(safeWhitespace);
        Assert.AreEqual(ProductLedgerPublicUiActionDecision.CompletedLocalOnlyNonDestructive, whitespaceResult.Decision);
        AssertNoDangerousSurface(whitespaceResult);

        AssertRejected(new ProductLedgerPublicUiActionSurface().Execute(unsafeContent), ProductLedgerPublicUiActionBlocker.CommandHandlerRejected);
        AssertRejected(new ProductLedgerPublicUiActionSurface().Execute(unsafeMetadata), ProductLedgerPublicUiActionBlocker.CommandHandlerRejected);
    }

    [TestMethod]
    public void PublicUiActionSurface_AllowsBoundedLocalReportExportAndVerifiesHash()
    {
        using var fixture = ExportFixture.Create();
        var reportPath = Path.Combine(fixture.AllowedRoot, "reports", "product-ledger-public-local-action.json");
        var exportRequest = ReadyExportRequest(fixture.AllowedRoot, reportPath);
        var result = new ProductLedgerPublicUiActionSurface().Execute(
            ReadyRequest(ProductLedgerPublicUiActionKind.LocalReportPhysicalExportBoundedInternal) with
            {
                LocalReportExportRequest = exportRequest
            });

        Assert.AreEqual(ProductLedgerPublicUiActionDecision.CompletedLocalOnlyNonDestructive, result.Decision);
        Assert.AreEqual(ProductLedgerInternalCommandDecision.CompletedBoundedLocalPhysicalExport, result.CommandResult!.Decision);
        Assert.IsTrue(result.PhysicalExportCreated);
        Assert.IsTrue(result.FileWritePerformed);
        Assert.IsTrue(File.Exists(reportPath));
        Assert.AreEqual(Hash(exportRequest.ReportContent!), result.CommandResult.LocalReportExportResult!.Evidence!.ReportHash);
        Assert.AreEqual(result.CommandResult.LocalReportExportResult.Evidence.ReportHash, result.CommandResult.ExecutionPreview.PostWriteHash);
        Assert.IsTrue(result.CommandResult.ExecutionPreview.ExportedFilePath!.StartsWith(fixture.AllowedRoot, StringComparison.OrdinalIgnoreCase));
        AssertNoDangerousSurface(result);
    }

    [TestMethod]
    public void PublicUiActionSurface_RejectsBoundedExportWithoutRequestOrOutsideBoundary()
    {
        using var fixture = ExportFixture.Create();
        using var outside = ExportFixture.Create();
        var missing = new ProductLedgerPublicUiActionSurface().Execute(
            ReadyRequest(ProductLedgerPublicUiActionKind.LocalReportPhysicalExportBoundedInternal));
        var outsidePath = Path.Combine(outside.AllowedRoot, "product-ledger-public-local-action.json");
        var outsideResult = new ProductLedgerPublicUiActionSurface().Execute(
            ReadyRequest(ProductLedgerPublicUiActionKind.LocalReportPhysicalExportBoundedInternal) with
            {
                LocalReportExportRequest = ReadyExportRequest(fixture.AllowedRoot, outsidePath)
            });

        AssertRejected(missing, ProductLedgerPublicUiActionBlocker.MissingBoundedLocalReportExportRequest);
        AssertRejected(outsideResult, ProductLedgerPublicUiActionBlocker.CommandHandlerRejected);
        Assert.IsFalse(File.Exists(outsidePath));
    }

    [TestMethod]
    [TestCategory("NodalOsTier1Safety")]
    [TestCategory("ProductLedger")]
    [TestCategory("PublicProductBlock")]
    [TestCategory("CommandExecutionBlock")]
    [TestCategory("ReleaseCommercialBlock")]
    public void PublicUiActionSurface_DangerousButtonsRenderDisabled()
    {
        var result = new ProductLedgerPublicUiActionSurface().Execute(ReadyRequest(ProductLedgerPublicUiActionKind.ViewDiagnostics));

        var dangerous = result.Buttons.Where(button => button.ActionKind is
            ProductLedgerPublicUiActionKind.ExecuteAction
            or ProductLedgerPublicUiActionKind.DestructiveWrite
            or ProductLedgerPublicUiActionKind.ConnectProvider
            or ProductLedgerPublicUiActionKind.RunMigration
            or ProductLedgerPublicUiActionKind.EnableKms
            or ProductLedgerPublicUiActionKind.RunBrowserCdp
            or ProductLedgerPublicUiActionKind.Release
            or ProductLedgerPublicUiActionKind.UnboundedExport
            or ProductLedgerPublicUiActionKind.ExternalExport).ToArray();

        Assert.IsTrue(dangerous.Length > 0);
        Assert.IsTrue(dangerous.All(button => !button.Enabled));
        Assert.IsTrue(dangerous.All(button => !button.NonDestructive));
        Assert.IsTrue(dangerous.All(button => button.DisabledReason.Contains("Blocked", StringComparison.Ordinal)));
    }

    [TestMethod]
    [TestCategory("NodalOsTier1Safety")]
    [TestCategory("ProductLedger")]
    [TestCategory("StaticGuard")]
    [TestCategory("PublicProductBlock")]
    [TestCategory("CommandExecutionBlock")]
    [TestCategory("ReleaseCommercialBlock")]
    public void PublicUiActionSurface_SourceHasNoNetworkDbKmsLiveReleaseRawOrOverclaim()
    {
        var source = File.ReadAllText(Path.Combine(
            RepoRoot(),
            "src",
            "OneBrain.Core",
            "Approval",
            "ProductLedgerPublicUiActionSurface.cs"));
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
            "DestructiveActionAvailable:" + " true",
            "UnboundedPhysicalExportAvailable:" + " true",
            "ExternalCloudExportAvailable:" + " true",
            "ProviderCloudNetworkAvailable:" + " true",
            "DbMigrationAvailable:" + " true",
            "KmsWormExternalTrustAvailable:" + " true",
            "BrowserCdpWcuOcrRecipesLiveAvailable:" + " true",
            "ReleaseCommercialReady:" + " true",
            "product" + "-ready",
            "release-ready",
            "commercial-ready"
        };

        foreach (var fragment in forbiddenFragments)
        {
            Assert.IsFalse(source.Contains(fragment, StringComparison.OrdinalIgnoreCase), fragment);
        }

        StringAssert.Contains(source, "ROUTER_HANDLER_MEDIATED");
        StringAssert.Contains(source, "NO_EXTERNAL_CLOUD_EXPORT");
    }

    private static ProductLedgerPublicUiActionRequest ReadyRequest(ProductLedgerPublicUiActionKind action) =>
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

    private static ProductLedgerPublicUiReadOnlyDisabledPreviewResult UnsafePublicPreview()
    {
        var ready = ReadyPublicPreview();
        return ready with
        {
            ViewModel = ready.ViewModel with
            {
                PublicUiActionAvailable = true
            }
        };
    }

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
                Section("Product Ledger Path Policy", "ACTIVE_LOCAL_ONLY_POLICY_BOUND", ["candidate_id=ledger-public-action-001"]),
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
            DisabledActions: ["destructive user-facing action", "provider/cloud/network", "release/commercial"],
            SafeNextStep: "PUBLIC_LOCAL_ONLY_ACTIONS",
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

    private static ProductLedgerLocalReportExportRequest ReadyExportRequest(string root, string reportPath) =>
        new(
            AllowedRootPath: root,
            ReportFilePath: reportPath,
            ReportContent: "{\"kind\":\"product-ledger-diagnostic-report\",\"content\":\"redacted diagnostics\",\"scope\":\"public local bounded\"}",
            EvidenceMetadata: new Dictionary<string, string>
            {
                ["operator"] = "public-local-only",
                ["redaction"] = "redacted-before-export",
                ["boundary"] = "canonicalized"
            },
            ExplicitInternalLocalOnlyBoundedExportScope: true,
            HasOperatorInternalEvidence: true,
            HasRedactionBeforePersistenceEvidence: true,
            HasSafeContentEvidence: true,
            HasResolvedReparsePointEvidence: true,
            HasTocTouMitigationEvidence: true,
            HardlinkOrMountAliasRiskUnresolved: false,
            AllowOverwriteExisting: false,
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
            ClaimsExternalExport: false,
            ClaimsUnboundedPhysicalExport: false);

    private static void AssertRejected(ProductLedgerPublicUiActionResult result, ProductLedgerPublicUiActionBlocker blocker)
    {
        Assert.AreEqual(ProductLedgerPublicUiActionDecision.Rejected, result.Decision, blocker.ToString());
        CollectionAssert.Contains(result.Blockers.ToArray(), blocker, blocker.ToString());
        Assert.IsFalse(result.PublicUiActionCompleted);
        Assert.IsFalse(result.LocalOnlyCommandHandlerInvoked);
        AssertNoDangerousSurface(result);
    }

    private static void AssertNoDangerousSurface(ProductLedgerPublicUiActionResult result)
    {
        Assert.IsTrue(result.LocalOnly);
        Assert.IsTrue(result.NonDestructive);
        Assert.IsTrue(result.Bounded);
        Assert.IsTrue(result.FailClosed);
        Assert.IsFalse(result.DestructiveActionAvailable);
        Assert.IsFalse(result.UnboundedPhysicalExportAvailable);
        Assert.IsFalse(result.ExternalCloudExportAvailable);
        Assert.IsFalse(result.ProviderCloudNetworkAvailable);
        Assert.IsFalse(result.DbMigrationAvailable);
        Assert.IsFalse(result.KmsWormExternalTrustAvailable);
        Assert.IsFalse(result.BrowserCdpWcuOcrRecipesLiveAvailable);
        Assert.IsFalse(result.ExternalTelemetryOrSyncAvailable);
        Assert.IsFalse(result.BillingLicensingCloudAvailable);
        Assert.IsFalse(result.ReleaseCommercialReady);
    }

    private static string Hash(string content)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(content));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private sealed class ExportFixture : IDisposable
    {
        private ExportFixture(string allowedRoot)
        {
            AllowedRoot = allowedRoot;
        }

        public string AllowedRoot { get; }

        public static ExportFixture Create()
        {
            var root = Path.Combine(RepoRoot(), ".tmp-product-ledger-public-action-tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            return new ExportFixture(root);
        }

        public void Dispose()
        {
            var baseRoot = Path.Combine(RepoRoot(), ".tmp-product-ledger-public-action-tests");
            if (AllowedRoot.StartsWith(baseRoot, StringComparison.OrdinalIgnoreCase) && Directory.Exists(AllowedRoot))
            {
                Directory.Delete(AllowedRoot, recursive: true);
            }
        }
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
                "ProductLedgerPublicUiActionSurface.cs")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        Assert.Fail("repo root not found");
        return string.Empty;
    }
}
