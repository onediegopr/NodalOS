using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Buffers.Binary;

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
        Assert.AreEqual("IHDR", System.Text.Encoding.ASCII.GetString(bytes, 12, 4));
        Assert.IsTrue(ContainsPngChunk(bytes, "IDAT"));
        Assert.AreEqual("IEND", System.Text.Encoding.ASCII.GetString(bytes, bytes.Length - 8, 4));

        var files = Directory.GetFiles(Path.GetDirectoryName(ScreenshotPath())!, "*", SearchOption.AllDirectories)
            .Select(path => Path.GetFileName(path))
            .ToArray();
        CollectionAssert.Contains(files, "product-ledger-local-dev-visual-qa.png");
        Assert.IsFalse(files.Any(name => name.EndsWith(".har", StringComparison.OrdinalIgnoreCase)));
        Assert.IsFalse(files.Any(name => name.EndsWith(".trace", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    public void ScreenshotEvidence_PngViewportMatchesLocalOnlyCaptureContract()
    {
        var bytes = File.ReadAllBytes(ScreenshotPath());

        Assert.AreEqual(1440, BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(16, 4)));
        Assert.AreEqual(1200, BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(20, 4)));
        Assert.IsTrue(bytes.Length >= 50_000);
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

    [TestMethod]
    public void DomSnapshotFixture_RequiredVisualSectionsStayLocalOnlyAndDisabled()
    {
        var html = File.ReadAllText(VisualSnapshotPath());

        StringAssert.Contains(html, "data-testid=\"product-ledger-visual-qa-evidence\"");
        StringAssert.Contains(html, "data-testid=\"header-local-only\"");
        StringAssert.Contains(html, "data-testid=\"runtime-gate\"");
        StringAssert.Contains(html, "data-testid=\"writer\"");
        StringAssert.Contains(html, "data-testid=\"bounded-export\"");
        StringAssert.Contains(html, "data-testid=\"evidence-gates\"");
        StringAssert.Contains(html, "data-testid=\"disabled-dangerous-actions\"");
        StringAssert.Contains(html, "data-testid=\"safe-next-step\"");
        StringAssert.Contains(html, "local-only");
        StringAssert.Contains(html, "Development-only or fixture-only");
        StringAssert.Contains(html, "no telemetry");
        StringAssert.Contains(html, "no external network");
        StringAssert.Contains(html, "no release/commercial");
        StringAssert.Contains(html, "STATIC_HTML_FIXTURE_NO_BROWSER_CDP");

        Assert.AreEqual(3, Count(html, "data-executable=\"false\""));
        Assert.AreEqual(3, Count(html, "disabled aria-disabled=\"true\""));
        Assert.IsFalse(html.Contains("<" + "script", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(html.Contains("src" + "=", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(html.Contains("href" + "=", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(html.Contains("onclick" + "=", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(html.Contains("formaction" + "=", StringComparison.OrdinalIgnoreCase));
    }

    private static string ScreenshotPath() =>
        Path.Combine(
            RepoRoot(),
            "docs",
            "qa",
            "nodal-os-product-ledger-browser-local-only-screenshot-evidence-test-only",
            "product-ledger-local-dev-visual-qa.png");

    private static string VisualSnapshotPath() =>
        Path.Combine(
            RepoRoot(),
            "docs",
            "qa",
            "nodal-os-product-ledger-local-dev-visual-qa-screenshot-evidence",
            "visual-snapshot.html");

    private static bool ContainsPngChunk(byte[] bytes, string chunkName)
    {
        var expected = System.Text.Encoding.ASCII.GetBytes(chunkName);
        for (var i = 8; i <= bytes.Length - 12;)
        {
            var length = BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(i, 4));
            if (bytes.AsSpan(i + 4, 4).SequenceEqual(expected))
            {
                return true;
            }

            i += 12 + length;
        }

        return false;
    }

    private static int Count(string value, string token)
    {
        var count = 0;
        var index = 0;
        while ((index = value.IndexOf(token, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += token.Length;
        }

        return count;
    }

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
