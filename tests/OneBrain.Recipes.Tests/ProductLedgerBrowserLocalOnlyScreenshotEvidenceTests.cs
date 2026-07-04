using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OneBrain.Recipes.Tests;

[TestClass]
[TestCategory("ApprovalHumanReview")]
[TestCategory("PhaseEApprovalHumanReview")]
public sealed class ProductLedgerBrowserLocalOnlyScreenshotEvidenceTests
{
    [TestMethod]
    public void ScreenshotEvidence_RecipeArtifactExistsAndIsLocalOnly()
    {
        var repo = RepoRoot();
        var qaRoot = Path.Combine(repo, "docs", "qa", "nodal-os-product-ledger-browser-local-only-screenshot-evidence-test-only");
        var screenshot = Path.Combine(qaRoot, "product-ledger-local-dev-visual-qa.png");
        var report = File.ReadAllText(Path.Combine(qaRoot, "report.json"));

        Assert.IsTrue(File.Exists(screenshot));
        Assert.IsTrue(screenshot.StartsWith(qaRoot, StringComparison.OrdinalIgnoreCase));
        StringAssert.Contains(report, "\"browserLocalOnlyTestOnly\": true");
        StringAssert.Contains(report, "\"publicDeployAdded\": false");
        StringAssert.Contains(report, "\"externalNetworkAdded\": false");
        StringAssert.Contains(report, "\"productiveBrowserCdpUsed\": false");
    }

    [TestMethod]
    public void DomSnapshotFixture_RecipeKeepsVisualEvidenceNonExecutable()
    {
        var html = File.ReadAllText(Path.Combine(
            RepoRoot(),
            "docs",
            "qa",
            "nodal-os-product-ledger-local-dev-visual-qa-screenshot-evidence",
            "visual-snapshot.html"));

        StringAssert.Contains(html, "Product Ledger Operator Surface Snapshot");
        StringAssert.Contains(html, "data-testid=\"runtime-gate\"");
        StringAssert.Contains(html, "data-testid=\"bounded-export\"");
        StringAssert.Contains(html, "data-testid=\"evidence-gates\"");
        StringAssert.Contains(html, "data-testid=\"disabled-dangerous-actions\"");
        StringAssert.Contains(html, "STATIC_HTML_FIXTURE_NO_BROWSER_CDP");
        Assert.IsFalse(html.Contains("<" + "script", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(html.Contains("onclick" + "=", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(html.Contains("formaction" + "=", StringComparison.OrdinalIgnoreCase));
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
