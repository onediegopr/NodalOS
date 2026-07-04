using System.Security.Cryptography;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Approval;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("ApprovalHumanReview")]
[TestCategory("PhaseEApprovalHumanReview")]
public sealed class ProductLedgerLocalReportExportServiceTests
{
    [TestMethod]
    public void LocalReportExport_FailsClosedByDefault()
    {
        var result = new ProductLedgerLocalReportExportService().Export(null);

        Assert.AreEqual(ProductLedgerLocalReportExportDecision.Rejected, result.Decision);
        CollectionAssert.Contains(result.Blockers.ToArray(), ProductLedgerLocalReportExportBlocker.MissingRequest);
        AssertNoPublicProductExternal(result);
        Assert.IsFalse(result.PhysicalExportCreated);
        Assert.IsFalse(result.FileWritePerformed);
    }

    [TestMethod]
    public void LocalReportExport_RejectsMissingCanonicalizationBoundaryEvidence()
    {
        using var fixture = ExportFixture.Create();
        var ready = ReadyRequest(fixture);
        var cases = new Dictionary<ProductLedgerLocalReportExportRequest, ProductLedgerLocalReportExportBlocker>
        {
            [ready with { ExplicitInternalLocalOnlyBoundedExportScope = false }] = ProductLedgerLocalReportExportBlocker.MissingExplicitInternalLocalOnlyBoundedExportScope,
            [ready with { HasOperatorInternalEvidence = false }] = ProductLedgerLocalReportExportBlocker.MissingOperatorInternalEvidence,
            [ready with { HasRedactionBeforePersistenceEvidence = false }] = ProductLedgerLocalReportExportBlocker.MissingRedactionBeforePersistenceEvidence,
            [ready with { HasSafeContentEvidence = false }] = ProductLedgerLocalReportExportBlocker.MissingSafeContentEvidence,
            [ready with { HasResolvedReparsePointEvidence = false }] = ProductLedgerLocalReportExportBlocker.ReparsePointEvidenceMissing,
            [ready with { HasTocTouMitigationEvidence = false }] = ProductLedgerLocalReportExportBlocker.TocTouMitigationMissing,
            [ready with { HardlinkOrMountAliasRiskUnresolved = true }] = ProductLedgerLocalReportExportBlocker.HardlinkOrMountAliasRiskUnresolved
        };

        foreach (var testCase in cases)
        {
            AssertBlocked(testCase.Key, testCase.Value);
        }
    }

    [TestMethod]
    public void LocalReportExport_RejectsUnsafePaths()
    {
        using var fixture = ExportFixture.Create();
        using var outside = ExportFixture.Create();
        var ready = ReadyRequest(fixture);
        var cases = new Dictionary<ProductLedgerLocalReportExportRequest, ProductLedgerLocalReportExportBlocker>
        {
            [ready with { ReportFilePath = Path.Combine(outside.AllowedRoot, "product-ledger-report.json") }] = ProductLedgerLocalReportExportBlocker.CanonicalReportPathOutsideAllowedBoundary,
            [ready with { ReportFilePath = Path.Combine(fixture.AllowedRoot, "..", "product-ledger-report.json") }] = ProductLedgerLocalReportExportBlocker.PathTraversalRisk,
            [ready with { ReportFilePath = @"\\server\share\product-ledger-report.json" }] = ProductLedgerLocalReportExportBlocker.UncNetworkPathRisk,
            [ready with { ReportFilePath = @"%TEMP%\product-ledger-report.json" }] = ProductLedgerLocalReportExportBlocker.EnvironmentVariableExpansionRisk,
            [ready with { ReportFilePath = Path.Combine(fixture.AllowedRoot, "con.json") }] = ProductLedgerLocalReportExportBlocker.WindowsReservedDeviceNameRisk,
            [ready with { ReportFilePath = Path.Combine(fixture.AllowedRoot, "product-ledger-report.json:ads") }] = ProductLedgerLocalReportExportBlocker.AlternateDataStreamRisk,
            [ready with { ReportFilePath = Path.Combine(fixture.AllowedRoot, "product-ledger-report.exe") }] = ProductLedgerLocalReportExportBlocker.UnsafeReportFileName
        };

        foreach (var testCase in cases)
        {
            AssertBlocked(testCase.Key, testCase.Value);
        }
    }

    [TestMethod]
    public void LocalReportExport_RejectsOverwriteWithoutExplicitSafePolicy()
    {
        using var fixture = ExportFixture.Create();
        var ready = ReadyRequest(fixture);
        Directory.CreateDirectory(Path.GetDirectoryName(ready.ReportFilePath!)!);
        File.WriteAllText(ready.ReportFilePath!, "existing", Encoding.UTF8);

        var result = new ProductLedgerLocalReportExportService().Export(ready);

        Assert.AreEqual(ProductLedgerLocalReportExportDecision.Blocked, result.Decision);
        CollectionAssert.Contains(result.Blockers.ToArray(), ProductLedgerLocalReportExportBlocker.ExistingFileRequiresExplicitSafeOverwrite);
        AssertNoPublicProductExternal(result);
    }

    [TestMethod]
    public void LocalReportExport_RejectsUnsafeContentMetadataAndClaims()
    {
        using var fixture = ExportFixture.Create();
        var ready = ReadyRequest(fixture);
        var unsafePublicReady = "public" + "-ready";
        var cases = new Dictionary<ProductLedgerLocalReportExportRequest, ProductLedgerLocalReportExportBlocker>
        {
            [ready with { ReportContent = "raw payload should never export" }] = ProductLedgerLocalReportExportBlocker.UnsafeReportContent,
            [ready with { ReportContent = "secret marker" }] = ProductLedgerLocalReportExportBlocker.UnsafeReportContent,
            [ready with { EvidenceMetadata = new Dictionary<string, string> { ["raw"] = "redacted" } }] = ProductLedgerLocalReportExportBlocker.UnsafeEvidenceMetadata,
            [ready with { EvidenceMetadata = new Dictionary<string, string> { ["ref"] = unsafePublicReady } }] = ProductLedgerLocalReportExportBlocker.UnsafeEvidenceMetadata,
            [ready with { ReportContent = "external trust claim" }] = ProductLedgerLocalReportExportBlocker.UnsafeReportContent,
            [ready with { ReportContent = "release-ready" }] = ProductLedgerLocalReportExportBlocker.UnsafeReportContent,
            [ready with { ClaimsProviderCloudNetwork = true }] = ProductLedgerLocalReportExportBlocker.ProviderCloudNetworkClaimed,
            [ready with { ClaimsDbMigration = true }] = ProductLedgerLocalReportExportBlocker.DbMigrationClaimed,
            [ready with { ClaimsKmsWormExternalTrust = true }] = ProductLedgerLocalReportExportBlocker.KmsWormExternalTrustClaimed,
            [ready with { ClaimsBrowserCdpWcuOcrRecipesLive = true }] = ProductLedgerLocalReportExportBlocker.BrowserCdpWcuOcrRecipesLiveClaimed,
            [ready with { ClaimsReleaseCommercial = true }] = ProductLedgerLocalReportExportBlocker.ReleaseCommercialClaimed,
            [ready with { RequestsPublicUiAction = true }] = ProductLedgerLocalReportExportBlocker.PublicUiActionRequested,
            [ready with { RequestsDestructiveAction = true }] = ProductLedgerLocalReportExportBlocker.DestructiveActionRequested,
            [ready with { RequestsProductCommandHandler = true }] = ProductLedgerLocalReportExportBlocker.ProductCommandHandlerRequested,
            [ready with { ClaimsExternalExport = true }] = ProductLedgerLocalReportExportBlocker.ExternalExportClaimed,
            [ready with { ClaimsUnboundedPhysicalExport = true }] = ProductLedgerLocalReportExportBlocker.UnboundedPhysicalExportClaimed
        };

        foreach (var testCase in cases)
        {
            AssertBlocked(testCase.Key, testCase.Value);
        }
    }

    [TestMethod]
    public void LocalReportExport_WritesOnlyBoundedLocalReportAndVerifiesHash()
    {
        using var fixture = ExportFixture.Create();
        var request = ReadyRequest(fixture);
        var result = new ProductLedgerLocalReportExportService().Export(request);

        Assert.AreEqual(ProductLedgerLocalReportExportDecision.ExportedBoundedLocal, result.Decision);
        Assert.AreEqual(0, result.Blockers.Count);
        Assert.IsTrue(result.PhysicalExportCreated);
        Assert.IsTrue(result.FileWritePerformed);
        Assert.IsTrue(result.PostWriteHashVerified);
        Assert.IsNotNull(result.Evidence);
        Assert.IsTrue(result.Evidence!.CanonicalReportFilePath.StartsWith(fixture.AllowedRoot, StringComparison.OrdinalIgnoreCase));
        Assert.AreEqual(Hash(request.ReportContent!), result.Evidence.ReportHash);
        Assert.AreEqual(result.Evidence.ReportHash, Hash(File.ReadAllText(request.ReportFilePath!, Encoding.UTF8)));
        AssertNoPublicProductExternal(result);
    }

    [TestMethod]
    public void LocalReportExport_SourceKeepsWriteSurfaceIsolatedAndNoExternalRuntimeClaims()
    {
        var source = File.ReadAllText(Path.Combine(
            RepoRoot(),
            "src",
            "OneBrain.Core",
            "Approval",
            "ProductLedgerLocalReportExportService.cs"));
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
            "PublicUiActionAvailable:" + " true",
            "DestructiveActionAvailable:" + " true",
            "ProductCommandHandlerAvailable:" + " true",
            "ProviderCloudNetworkAvailable:" + " true",
            "DbMigrationAvailable:" + " true",
            "KmsWormExternalTrustAvailable:" + " true",
            "BrowserCdpWcuOcrRecipesLiveAvailable:" + " true",
            "ReleaseCommercialReady:" + " true",
            "product" + "-ready:",
            "public" + "-ready:"
        };

        foreach (var fragment in forbiddenFragments)
        {
            Assert.IsFalse(source.Contains(fragment, StringComparison.OrdinalIgnoreCase), fragment);
        }

        StringAssert.Contains(source, "Directory.CreateDirectory");
        StringAssert.Contains(source, "File.WriteAllText");
        StringAssert.Contains(source, "PostWriteHashVerified: exported");
    }

    private static void AssertBlocked(ProductLedgerLocalReportExportRequest request, ProductLedgerLocalReportExportBlocker blocker)
    {
        var result = new ProductLedgerLocalReportExportService().Export(request);

        Assert.AreEqual(ProductLedgerLocalReportExportDecision.Blocked, result.Decision, blocker.ToString());
        CollectionAssert.Contains(result.Blockers.ToArray(), blocker, blocker.ToString());
        Assert.IsFalse(result.PhysicalExportCreated);
        Assert.IsFalse(result.FileWritePerformed);
        AssertNoPublicProductExternal(result);
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

    private static void AssertNoPublicProductExternal(ProductLedgerLocalReportExportResult result)
    {
        Assert.IsTrue(result.InternalOnly);
        Assert.IsTrue(result.LocalOnly);
        Assert.IsTrue(result.Bounded);
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

    private static string Hash(string value)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(hash).ToLowerInvariant();
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
            var root = Path.Combine(RepoRoot(), ".tmp-product-ledger-local-report-export-tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            return new ExportFixture(root);
        }

        public void Dispose()
        {
            var baseRoot = Path.Combine(RepoRoot(), ".tmp-product-ledger-local-report-export-tests");
            if (AllowedRoot.StartsWith(baseRoot, StringComparison.OrdinalIgnoreCase) && Directory.Exists(AllowedRoot))
            {
                Directory.Delete(AllowedRoot, recursive: true);
            }
        }
    }
}
