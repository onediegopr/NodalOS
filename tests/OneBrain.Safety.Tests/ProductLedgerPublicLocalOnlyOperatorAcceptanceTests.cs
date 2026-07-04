using System.Security.Cryptography;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Approval;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("ApprovalHumanReview")]
[TestCategory("PhaseEApprovalHumanReview")]
public sealed class ProductLedgerPublicLocalOnlyOperatorAcceptanceTests
{
    [TestMethod]
    public void OperatorAcceptance_FixtureOnlyWalkthroughConfirmsAllowedBlockedAndBoundedExportEvidence()
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
            Assert.IsTrue(result.PublicUiActionCompleted, action.ToString());
            Assert.IsTrue(result.LocalOnlyCommandHandlerInvoked, action.ToString());
            Assert.IsNotNull(result.RouterPreview, action.ToString());
            Assert.IsNotNull(result.CommandResult, action.ToString());
            Assert.AreEqual(ProductLedgerInternalCommandDecision.CompletedReadOnlyInMemory, result.CommandResult!.Decision, action.ToString());
            AssertSafeOperatorSurface(result);
            Assert.IsTrue(result.Buttons.Any(button => button.ActionKind == action && button.Enabled), action.ToString());
        }

        using var fixture = ExportFixture.Create();
        var reportPath = Path.Combine(fixture.AllowedRoot, "reports", "product-ledger-operator-acceptance.json");
        var exportRequest = ReadyExportRequest(fixture.AllowedRoot, reportPath);
        var exportResult = new ProductLedgerPublicUiActionSurface().Execute(
            ReadyRequest(ProductLedgerPublicUiActionKind.LocalReportPhysicalExportBoundedInternal) with
            {
                LocalReportExportRequest = exportRequest
            });

        Assert.AreEqual(ProductLedgerPublicUiActionDecision.CompletedLocalOnlyNonDestructive, exportResult.Decision);
        Assert.AreEqual(ProductLedgerInternalCommandDecision.CompletedBoundedLocalPhysicalExport, exportResult.CommandResult!.Decision);
        Assert.IsTrue(exportResult.PhysicalExportCreated);
        Assert.IsTrue(exportResult.FileWritePerformed);
        Assert.AreEqual(Hash(exportRequest.ReportContent!), exportResult.CommandResult.LocalReportExportResult!.Evidence!.ReportHash);
        Assert.AreEqual(exportResult.CommandResult.LocalReportExportResult.Evidence.ReportHash, exportResult.CommandResult.ExecutionPreview.PostWriteHash);
        Assert.IsTrue(exportResult.CommandResult.ExecutionPreview.ExportedFilePath!.StartsWith(fixture.AllowedRoot, StringComparison.OrdinalIgnoreCase));
        AssertSafeOperatorSurface(exportResult);
    }

    [TestMethod]
    public void OperatorAcceptance_UxSafetyReviewShowsDangerousActionsDisabledAndNoOverclaim()
    {
        var result = new ProductLedgerPublicUiActionSurface().Execute(ReadyRequest(ProductLedgerPublicUiActionKind.ViewDiagnostics));
        var dangerousActions = new[]
        {
            ProductLedgerPublicUiActionKind.ExecuteAction,
            ProductLedgerPublicUiActionKind.DestructiveWrite,
            ProductLedgerPublicUiActionKind.ConnectProvider,
            ProductLedgerPublicUiActionKind.RunMigration,
            ProductLedgerPublicUiActionKind.EnableKms,
            ProductLedgerPublicUiActionKind.RunBrowserCdp,
            ProductLedgerPublicUiActionKind.Release,
            ProductLedgerPublicUiActionKind.UnboundedExport,
            ProductLedgerPublicUiActionKind.ExternalExport
        };

        foreach (var action in dangerousActions)
        {
            var button = result.Buttons.Single(button => button.ActionKind == action);

            Assert.IsFalse(button.Enabled, action.ToString());
            Assert.IsFalse(button.NonDestructive, action.ToString());
            Assert.AreEqual("blocked dangerous action", button.RiskLabel);
            StringAssert.Contains(button.DisabledReason, "Blocked");
            Assert.IsTrue(button.RequiredEvidence.Contains("new explicit GO"));
        }

        var blockedCases = new Dictionary<ProductLedgerPublicUiActionRequest, ProductLedgerPublicUiActionBlocker>
        {
            [ReadyRequest(ProductLedgerPublicUiActionKind.DestructiveWrite)] = ProductLedgerPublicUiActionBlocker.PublicDestructiveActionRequested,
            [ReadyRequest(ProductLedgerPublicUiActionKind.UnboundedExport)] = ProductLedgerPublicUiActionBlocker.UnboundedPhysicalExportClaimed,
            [ReadyRequest(ProductLedgerPublicUiActionKind.ExternalExport)] = ProductLedgerPublicUiActionBlocker.ExternalCloudExportClaimed,
            [ReadyRequest(ProductLedgerPublicUiActionKind.ConnectProvider)] = ProductLedgerPublicUiActionBlocker.ProviderCloudNetworkClaimed,
            [ReadyRequest(ProductLedgerPublicUiActionKind.RunMigration)] = ProductLedgerPublicUiActionBlocker.DbMigrationClaimed,
            [ReadyRequest(ProductLedgerPublicUiActionKind.EnableKms)] = ProductLedgerPublicUiActionBlocker.KmsWormExternalTrustClaimed,
            [ReadyRequest(ProductLedgerPublicUiActionKind.RunBrowserCdp)] = ProductLedgerPublicUiActionBlocker.BrowserCdpWcuOcrRecipesLiveClaimed,
            [ReadyRequest(ProductLedgerPublicUiActionKind.Release)] = ProductLedgerPublicUiActionBlocker.ReleaseCommercialClaimed,
            [ReadyRequest(ProductLedgerPublicUiActionKind.ViewDiagnostics) with { ClaimsRawPayloadOrSecret = true }] = ProductLedgerPublicUiActionBlocker.RawPayloadOrSecretClaimed,
            [ReadyRequest(ProductLedgerPublicUiActionKind.ViewDiagnostics) with { ClaimsExternalTelemetryOrSync = true }] = ProductLedgerPublicUiActionBlocker.ExternalTelemetryOrSyncClaimed,
            [ReadyRequest(ProductLedgerPublicUiActionKind.ViewDiagnostics) with { ClaimsBillingLicensingCloud = true }] = ProductLedgerPublicUiActionBlocker.BillingLicensingCloudClaimed
        };

        foreach (var testCase in blockedCases)
        {
            var blocked = new ProductLedgerPublicUiActionSurface().Execute(testCase.Key);

            Assert.AreEqual(ProductLedgerPublicUiActionDecision.Rejected, blocked.Decision, testCase.Value.ToString());
            CollectionAssert.Contains(blocked.Blockers.ToArray(), testCase.Value, testCase.Value.ToString());
            AssertSafeOperatorSurface(blocked);
        }
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
                Section("Product Ledger Path Policy", "ACTIVE_LOCAL_ONLY_POLICY_BOUND", ["candidate_id=ledger-operator-acceptance-001"]),
                Section("Bounded Writer Status", "WRITER_BOUNDED_LOCAL_ONLY_SURFACE_READ_ONLY", ["operator_surface_write_allowed=false"]),
                Section("Checkpoint / Head Status", "VERIFIED_HEAD_PRESENT", ["same_boundary_trust=true"]),
                Section("Evidence Gates", "EVIDENCE_REFERENCES_FRESH_AND_WELL_FORMED", ["redaction_before_persistence=True", "authority=True"]),
                Section("Disabled Actions", "ALL_ACTIONS_DISABLED", ["destructive action", "unbounded export", "external/cloud export"]),
                Section("Safe Next Step", "READ_ONLY_AUDIT_OR_STATIC_SCAN_HARDENING_ONLY", ["operator acceptance"])
            ],
            ActionPreviews:
            [
                new("View local-only diagnostics snapshot", "read-only preview only", "operator visibility without execution authority", ["runtime gate"], Disabled: true, ProductiveCommandId: null, HandlerName: null, CallbackName: null)
            ],
            DisabledActions: ["destructive user-facing action", "provider/cloud/network", "release/commercial"],
            SafeNextStep: "OPERATOR_ACCEPTANCE_LOCAL_ONLY",
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
            ReportContent: "{\"kind\":\"product-ledger-operator-acceptance\",\"content\":\"redacted diagnostics\",\"scope\":\"operator local bounded\"}",
            EvidenceMetadata: new Dictionary<string, string>
            {
                ["operator"] = "acceptance-local-only",
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

    private static void AssertSafeOperatorSurface(ProductLedgerPublicUiActionResult result)
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
            var root = Path.Combine(RepoRoot(), ".tmp-product-ledger-operator-acceptance-tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            return new ExportFixture(root);
        }

        public void Dispose()
        {
            var baseRoot = Path.Combine(RepoRoot(), ".tmp-product-ledger-operator-acceptance-tests");
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
