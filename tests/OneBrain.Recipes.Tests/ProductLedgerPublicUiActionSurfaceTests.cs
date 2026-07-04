using System.Security.Cryptography;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Approval;

namespace OneBrain.Recipes.Tests;

[TestClass]
[TestCategory("ApprovalHumanReview")]
[TestCategory("PhaseEApprovalHumanReview")]
public sealed class ProductLedgerPublicUiActionSurfaceTests
{
    [TestMethod]
    public void PublicUiActionSurface_CompletesLocalOnlyReadActionsRecipe()
    {
        var diagnostics = new ProductLedgerPublicUiActionSurface().Execute(ReadyRequest(ProductLedgerPublicUiActionKind.ViewDiagnostics));
        var readiness = new ProductLedgerPublicUiActionSurface().Execute(ReadyRequest(ProductLedgerPublicUiActionKind.ViewLedgerReadiness));

        Assert.AreEqual(ProductLedgerPublicUiActionDecision.CompletedLocalOnlyNonDestructive, diagnostics.Decision);
        Assert.AreEqual(ProductLedgerPublicUiActionDecision.CompletedLocalOnlyNonDestructive, readiness.Decision);
        Assert.AreEqual(ProductLedgerInternalCommandDecision.CompletedReadOnlyInMemory, diagnostics.CommandResult!.Decision);
        Assert.AreEqual(ProductLedgerInternalCommandDecision.CompletedReadOnlyInMemory, readiness.CommandResult!.Decision);
        AssertNoExternalRelease(diagnostics);
        AssertNoExternalRelease(readiness);
    }

    [TestMethod]
    public void PublicUiActionSurface_BlocksDangerousPublicActionRecipe()
    {
        var result = new ProductLedgerPublicUiActionSurface().Execute(ReadyRequest(ProductLedgerPublicUiActionKind.ExecuteAction) with
        {
            RequestsGenericExecuteAction = true,
            ClaimsExternalCloudExport = true,
            ClaimsReleaseCommercial = true
        });

        Assert.AreEqual(ProductLedgerPublicUiActionDecision.Rejected, result.Decision);
        CollectionAssert.Contains(result.Blockers.ToArray(), ProductLedgerPublicUiActionBlocker.GenericExecuteActionRequested);
        CollectionAssert.Contains(result.Blockers.ToArray(), ProductLedgerPublicUiActionBlocker.ExternalCloudExportClaimed);
        CollectionAssert.Contains(result.Blockers.ToArray(), ProductLedgerPublicUiActionBlocker.ReleaseCommercialClaimed);
        AssertNoExternalRelease(result);
    }

    [TestMethod]
    public void PublicUiActionSurface_CompletesBoundedLocalExportRecipe()
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
        Assert.AreEqual(Hash(exportRequest.ReportContent!), result.CommandResult.LocalReportExportResult!.Evidence!.ReportHash);
        AssertNoExternalRelease(result);
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
                Section("Product Ledger Path Policy", "ACTIVE_LOCAL_ONLY_POLICY_BOUND", ["candidate_id=ledger-public-action-recipe-001"]),
                Section("Bounded Writer Status", "WRITER_BOUNDED_LOCAL_ONLY_SURFACE_READ_ONLY", ["operator_surface_write_allowed=false"]),
                Section("Checkpoint / Head Status", "VERIFIED_HEAD_PRESENT", ["same_boundary_trust=true"]),
                Section("Evidence Gates", "EVIDENCE_REFERENCES_FRESH_AND_WELL_FORMED", ["redaction_before_persistence=True", "authority=True"]),
                Section("Disabled Actions", "ALL_ACTIONS_DISABLED", ["destructive action", "provider/cloud/network", "release/commercial"]),
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

    private static void AssertNoExternalRelease(ProductLedgerPublicUiActionResult result)
    {
        Assert.IsTrue(result.LocalOnly);
        Assert.IsTrue(result.NonDestructive);
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
            var root = Path.Combine(RepoRoot(), ".tmp-product-ledger-public-action-recipes", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            return new ExportFixture(root);
        }

        public void Dispose()
        {
            var baseRoot = Path.Combine(RepoRoot(), ".tmp-product-ledger-public-action-recipes");
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
