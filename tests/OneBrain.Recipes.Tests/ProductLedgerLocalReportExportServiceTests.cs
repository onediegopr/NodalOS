using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Approval;

namespace OneBrain.Recipes.Tests;

[TestClass]
[TestCategory("ApprovalHumanReview")]
[TestCategory("PhaseEApprovalHumanReview")]
public sealed class ProductLedgerLocalReportExportServiceTests
{
    [TestMethod]
    public void LocalReportExport_WritesBoundedDiagnosticReportRecipe()
    {
        using var fixture = ExportFixture.Create();
        var request = ReadyRequest(fixture);

        var result = new ProductLedgerLocalReportExportService().Export(request);

        Assert.AreEqual(ProductLedgerLocalReportExportDecision.ExportedBoundedLocal, result.Decision);
        Assert.IsTrue(File.Exists(request.ReportFilePath));
        Assert.IsTrue(result.PostWriteHashVerified);
        Assert.IsTrue(result.Evidence!.CanonicalReportFilePath.StartsWith(fixture.AllowedRoot, StringComparison.OrdinalIgnoreCase));
        AssertNoExternal(result);
    }

    [TestMethod]
    public void LocalReportExport_BlocksExternalAndUnsafeReportRecipe()
    {
        using var fixture = ExportFixture.Create();
        var result = new ProductLedgerLocalReportExportService().Export(ReadyRequest(fixture) with
        {
            ReportContent = "raw payload with secret",
            ClaimsProviderCloudNetwork = true,
            ClaimsKmsWormExternalTrust = true,
            ClaimsReleaseCommercial = true,
            RequestsProductCommandHandler = true
        });

        Assert.AreEqual(ProductLedgerLocalReportExportDecision.Blocked, result.Decision);
        CollectionAssert.Contains(result.Blockers.ToArray(), ProductLedgerLocalReportExportBlocker.UnsafeReportContent);
        CollectionAssert.Contains(result.Blockers.ToArray(), ProductLedgerLocalReportExportBlocker.ProviderCloudNetworkClaimed);
        CollectionAssert.Contains(result.Blockers.ToArray(), ProductLedgerLocalReportExportBlocker.KmsWormExternalTrustClaimed);
        CollectionAssert.Contains(result.Blockers.ToArray(), ProductLedgerLocalReportExportBlocker.ReleaseCommercialClaimed);
        CollectionAssert.Contains(result.Blockers.ToArray(), ProductLedgerLocalReportExportBlocker.ProductCommandHandlerRequested);
        AssertNoExternal(result);
    }

    private static ProductLedgerLocalReportExportRequest ReadyRequest(ExportFixture fixture) =>
        new(
            AllowedRootPath: fixture.AllowedRoot,
            ReportFilePath: Path.Combine(fixture.AllowedRoot, "reports", "product-ledger-diagnostics.json"),
            ReportContent: "{\"kind\":\"product-ledger-diagnostic-report\",\"content\":\"redacted diagnostics\",\"scope\":\"internal local bounded\"}",
            EvidenceMetadata: new Dictionary<string, string>
            {
                ["operator"] = "internal-local-only",
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

    private static void AssertNoExternal(ProductLedgerLocalReportExportResult result)
    {
        Assert.IsTrue(result.InternalOnly);
        Assert.IsTrue(result.LocalOnly);
        Assert.IsTrue(result.Bounded);
        Assert.IsFalse(result.PublicUiActionAvailable);
        Assert.IsFalse(result.DestructiveActionAvailable);
        Assert.IsFalse(result.ProductCommandHandlerAvailable);
        Assert.IsFalse(result.ProviderCloudNetworkAvailable);
        Assert.IsFalse(result.DbMigrationAvailable);
        Assert.IsFalse(result.KmsWormExternalTrustAvailable);
        Assert.IsFalse(result.BrowserCdpWcuOcrRecipesLiveAvailable);
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
                "ProductLedgerLocalReportExportService.cs")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        Assert.Fail("repo root not found");
        return string.Empty;
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
            var root = Path.Combine(RepoRoot(), ".tmp-product-ledger-local-report-export-recipes", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            return new ExportFixture(root);
        }

        public void Dispose()
        {
            var baseRoot = Path.Combine(RepoRoot(), ".tmp-product-ledger-local-report-export-recipes");
            if (AllowedRoot.StartsWith(baseRoot, StringComparison.OrdinalIgnoreCase) && Directory.Exists(AllowedRoot))
            {
                Directory.Delete(AllowedRoot, recursive: true);
            }
        }
    }
}
