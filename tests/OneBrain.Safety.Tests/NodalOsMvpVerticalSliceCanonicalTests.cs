using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OneBrain.Safety.Tests;

[TestClass]
public sealed class NodalOsMvpVerticalSliceCanonicalTests
{
    [TestMethod]
    [TestCategory("NodalOsTier1Safety")]
    [TestCategory("Architecture")]
    [TestCategory("MvpVerticalSlice")]
    public void CanonicalDocumentFreezesOneOrderedVerticalSlice()
    {
        var doc = ReadDoc();
        var stages = new[]
        {
            "Workspace",
            "Mission",
            "Plan",
            "Approval",
            "Controlled Action",
            "Verification",
            "Evidence/Timeline",
            "Handoff"
        };

        var previous = -1;
        foreach (var stage in stages)
        {
            Assert.AreEqual(1, Count(doc, $"| {stage} |"), $"stage row must be unique: {stage}");
            var current = doc.IndexOf($"| {stage} |", StringComparison.Ordinal);
            Assert.IsTrue(current > previous, $"stage order must keep {stage} after the previous stage");
            previous = current;
        }

        AssertContains(doc, "Order is load-bearing");
        AssertContains(doc, "approval precedes any sensitive controlled action");
        AssertContains(doc, "verification precedes evidence promotion");
        AssertContains(doc, "handoff is read-only and never mutates the result");
    }

    [TestMethod]
    [TestCategory("NodalOsTier1Safety")]
    [TestCategory("Architecture")]
    [TestCategory("MvpVerticalSlice")]
    [TestCategory("NoProductAuthority")]
    public void CanonicalDocumentKeepsChromeLabLabOnlyAndCloakBrowserAsBrowserRuntime()
    {
        var doc = ReadDoc();

        AssertContains(doc, "CloakBrowser with pinned direct CDP is the only canonical product browser runtime");
        AssertContains(doc, "ChromeLab and the extension are `LAB_LEGACY_TRANSITION` only");
        AssertContains(doc, "System Chrome/Edge is never a silent product-runtime fallback");
        AssertContains(doc, "Playwright default Chromium is never a product-runtime substitute");
        AssertContains(doc, "| ChromeLab bridge/extension | `LAB_ONLY` |");
        AssertContains(doc, "| BrowserRuntime/CloakBrowser direct CDP | `CANONICAL_KEEP` |");
    }

    [TestMethod]
    [TestCategory("NodalOsTier1Safety")]
    [TestCategory("Architecture")]
    [TestCategory("MvpVerticalSlice")]
    [TestCategory("NoDoubleTruth")]
    public void CanonicalDocumentKeepsProductLedgerAsEvidenceAndNotAuthority()
    {
        var doc = ReadDoc();

        AssertContains(doc, "Product Ledger remains local/dev review evidence/read model/supporting timeline until a separate product-authority GO");
        AssertContains(doc, "It is not latest pointer authority, read precedence authority, product authority or release authority");
        AssertContains(doc, "| Product Ledger local/dev surfaces | `LEGACY_READ_ONLY` | supporting evidence/read model only |");
    }

    [TestMethod]
    [TestCategory("NodalOsTier1Safety")]
    [TestCategory("Architecture")]
    [TestCategory("MvpVerticalSlice")]
    public void GovernanceDocumentRecordsExternalGitHubBlockerAndRealChecks()
    {
        var governance = File.ReadAllText(Path.Combine(FindRepoRoot(), "docs", "operations", "branch-governance.md"));

        AssertContains(governance, "Decision: `BLOCKED_EXTERNAL_GITHUB_REMOTE_SETTINGS`.");
        AssertContains(governance, "gh auth status");
        AssertContains(governance, "gh repo edit onediegopr/NodalOS --default-branch main");
        AssertContains(governance, "`chromelab-security`");
        AssertContains(governance, "`secret-scan`");
        AssertContains(governance, "Force push is blocked");
        AssertContains(governance, "Branch deletion is blocked");
    }

    private static string ReadDoc() =>
        File.ReadAllText(Path.Combine(
            FindRepoRoot(),
            "docs",
            "architecture",
            "nodal-os-mvp-vertical-slice-canonical.md"));

    private static void AssertContains(string source, string value) =>
        Assert.IsTrue(source.Contains(value, StringComparison.Ordinal), $"Missing expected text: {value}");

    private static int Count(string source, string value)
    {
        var count = 0;
        var offset = 0;
        while ((offset = source.IndexOf(value, offset, StringComparison.Ordinal)) >= 0)
        {
            count++;
            offset += value.Length;
        }

        return count;
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "OneBrain.slnx")))
            dir = dir.Parent;

        Assert.IsNotNull(dir, "repo root not found");
        return dir.FullName;
    }
}
