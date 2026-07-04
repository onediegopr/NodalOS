using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("ApprovalHumanReview")]
[TestCategory("PhaseEApprovalHumanReview")]
public sealed class ProductLedgerBrowserLocalOnlyScreenshotEvidenceTests
{
    private const string ExpectedSha256 = "dfa67a2d279e878704db4a5916708dc195ce2b59e21a7893b4149d481a56d80e";

    [TestMethod]
    public void ScreenshotEvidence_ArtifactPathIsLocalDocsQaOnly()
    {
        var repo = RepoRoot();
        var screenshot = ScreenshotPath();
        var qaRoot = Path.Combine(repo, "docs", "qa", "nodal-os-product-ledger-browser-local-only-screenshot-evidence-test-only");

        Assert.IsTrue(screenshot.StartsWith(qaRoot, StringComparison.OrdinalIgnoreCase));
        Assert.IsTrue(File.Exists(screenshot));
        Assert.IsTrue(new FileInfo(screenshot).Length > 10_000);
        Assert.AreEqual(ExpectedSha256, Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(screenshot))).ToLowerInvariant());
    }

    [TestMethod]
    public void ScreenshotEvidence_IsPngAndHasNoExternalArtifactCompanions()
    {
        var bytes = File.ReadAllBytes(ScreenshotPath());

        CollectionAssert.AreEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47 }, bytes.Take(4).ToArray());

        var files = Directory.GetFiles(Path.GetDirectoryName(ScreenshotPath())!, "*", SearchOption.AllDirectories)
            .Select(path => Path.GetFileName(path))
            .ToArray();
        CollectionAssert.Contains(files, "product-ledger-local-dev-visual-qa.png");
        Assert.IsFalse(files.Any(name => name.EndsWith(".har", StringComparison.OrdinalIgnoreCase)));
        Assert.IsFalse(files.Any(name => name.EndsWith(".trace", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    public void ScreenshotEvidence_ReportKeepsNoExternalNoTelemetryNoProductBrowserClaims()
    {
        var report = File.ReadAllText(Path.Combine(
            RepoRoot(),
            "docs",
            "qa",
            "nodal-os-product-ledger-browser-local-only-screenshot-evidence-test-only",
            "report.json"));

        StringAssert.Contains(report, "\"screenshotCaptured\": true");
        StringAssert.Contains(report, "\"browserLocalOnlyTestOnly\": true");
        StringAssert.Contains(report, "\"externalNetworkAdded\": false");
        StringAssert.Contains(report, "\"telemetrySyncAdded\": false");
        StringAssert.Contains(report, "\"productiveBrowserCdpUsed\": false");
        StringAssert.Contains(report, "\"releaseCommercialReady\": false");
        Assert.IsFalse(report.Contains("http" + "://", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(report.Contains("https" + "://", StringComparison.OrdinalIgnoreCase));
    }

    private static string ScreenshotPath() =>
        Path.Combine(
            RepoRoot(),
            "docs",
            "qa",
            "nodal-os-product-ledger-browser-local-only-screenshot-evidence-test-only",
            "product-ledger-local-dev-visual-qa.png");

    private static string RepoRoot()
    {
        var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "OneBrain.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        Assert.Fail("repo root not found");
        return string.Empty;
    }
}
