using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OneBrain.Safety.Tests;

[TestClass]
public sealed class NodalOsRoadmapReconciliationM94M96Tests
{
    [TestMethod]
    public void RoadmapReconciliationDocumentExistsAndKeepsHito162Paused()
    {
        var text = ReadDoc("docs", "roadmap", "nodal-os-roadmap-reconciliation-m94-m96.md");

        StringAssert.Contains(text, "HITO-162");
        StringAssert.Contains(text, "paused/not forgotten");
        StringAssert.Contains(text, "M51 is closed with strict scope");
        StringAssert.Contains(text, "external HTTP read-only proof only");
        StringAssert.Contains(text, "M65 remains deferred");
        StringAssert.Contains(text, "no Chrome/CDP external navigation");
        StringAssert.Contains(text, "visible rename from NEXA to NODAL OS was handled in M97-M99");
    }

    [TestMethod]
    public void LegacyHitoAbsorptionMatrixExistsAndClassifiesCriticalItems()
    {
        var text = ReadDoc("docs", "roadmap", "nodal-os-legacy-hito-absorption-matrix.md");

        StringAssert.Contains(text, "HITO-162");
        StringAssert.Contains(text, "UnknownNeedsAudit");
        StringAssert.Contains(text, "M51");
        StringAssert.Contains(text, "Closed with strict HTTP-only scope");
        StringAssert.Contains(text, "M65");
        StringAssert.Contains(text, "Closed with target-owned Chrome/CDP/DOM read-only scope");
        StringAssert.Contains(text, "External Chrome/CDP/DOM proof");
        StringAssert.Contains(text, "Closed only for target-owned");
        StringAssert.Contains(text, "Rename NEXA to NODAL OS");
        StringAssert.Contains(text, "Visible rename completed");
        StringAssert.Contains(text, "Recommended action");
    }

    [TestMethod]
    public void NodalOsRoadmapVNextExistsAndDefinesNextBlocks()
    {
        var text = ReadDoc("docs", "roadmap", "nodal-os-roadmap-vnext.md");

        StringAssert.Contains(text, "NODAL OS engineering: 97%");
        StringAssert.Contains(text, "External HTTP read-only proof readiness: 90-95%");
        StringAssert.Contains(text, "Browser Runtime local/sandbox: 97%");
        StringAssert.Contains(text, "Security/evidence integrity: 92-95%");
        StringAssert.Contains(text, "M51: closed with strict HTTP read-only scope");
        StringAssert.Contains(text, "M65: closed with limited target-owned Chrome/CDP/DOM read-only scope");
        StringAssert.Contains(text, "External CDP general-ready: false");
        StringAssert.Contains(text, "M115/M116/M117");
        StringAssert.Contains(text, "M118/M119/M120");
        StringAssert.Contains(text, "M121/M122/M123");
        StringAssert.Contains(text, "M124+");
        StringAssert.Contains(text, "HITO-162 is paused/not forgotten");
    }

    [TestMethod]
    public void NodalOsRoadmapVNextKeepsSecurityRestrictionsVisible()
    {
        var text = ReadDoc("docs", "roadmap", "nodal-os-roadmap-vnext.md");

        StringAssert.Contains(text, "No SaaS public");
        StringAssert.Contains(text, "No public API real");
        StringAssert.Contains(text, "No billing real");
        StringAssert.Contains(text, "No email real");
        StringAssert.Contains(text, "No real customer credentials");
        StringAssert.Contains(text, "No sensitive sites");
        StringAssert.Contains(text, "No submit/pay/sign/delete");
        StringAssert.Contains(text, "No productive recorder/replay");
        StringAssert.Contains(text, "No Chrome/CDP general-ready claim from target-owned proof");
        StringAssert.Contains(text, "No Chromium fork planned now");
    }

    private static string ReadDoc(params string[] relativePath)
    {
        var root = FindRepoRoot();
        var path = Path.Combine(new[] { root }.Concat(relativePath).ToArray());
        Assert.IsTrue(File.Exists(path), path);
        return File.ReadAllText(path);
    }

    private static string FindRepoRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "OneBrain.slnx")))
                return current.FullName;
            current = current.Parent;
        }

        Assert.Fail("Could not locate repository root.");
        return "";
    }
}
