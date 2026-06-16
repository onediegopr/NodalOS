using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OneBrain.Safety.Tests;

[TestClass]
public sealed class NodalOsHito162RewriteM130M132Tests
{
    [TestMethod]
    public void Hito162IntentRecoveryDocumentExists()
    {
        var text = ReadDoc("docs", "roadmap", "hito-162-intent-recovery-m130-m132.md");

        StringAssert.Contains(text, "HITO-162");
        StringAssert.Contains(text, "paused/not forgotten/UnknownNeedsAudit");
        StringAssert.Contains(text, "Identity/Fingerprint v2");
        StringAssert.Contains(text, "robust perception");
    }

    [TestMethod]
    public void Hito162IntentRecoveryMentionsHito161LastReliablePoint()
    {
        var text = ReadDoc("docs", "roadmap", "hito-162-intent-recovery-m130-m132.md");

        StringAssert.Contains(text, "HITO-161 is the last reliable point");
        StringAssert.Contains(text, "approved input binding unification");
    }

    [TestMethod]
    public void Hito162IntentRecoveryMentionsNodalOsCurrentState()
    {
        var text = ReadDoc("docs", "roadmap", "hito-162-intent-recovery-m130-m132.md");

        StringAssert.Contains(text, "M51 closed external HTTP read-only proof");
        StringAssert.Contains(text, "M65 closed limited target-owned Chrome/CDP/DOM read-only proof");
        StringAssert.Contains(text, "Product/Admin private preview local");
        StringAssert.Contains(text, "ReadyWithRestrictions");
    }

    [TestMethod]
    public void Hito162IntentRecoveryDoesNotDeclareHito162ClosedOrResumed()
    {
        var text = ReadDoc("docs", "roadmap", "hito-162-intent-recovery-m130-m132.md");

        StringAssert.Contains(text, "does not resume or close HITO-162");
        StringAssert.Contains(text, "not implemented functionality");
        Assert.IsFalse(text.Contains("HITO-162 CERRADO", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(text.Contains("HITO-162 closed", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void Hito162MappingDocumentExists()
    {
        var text = ReadDoc("docs", "roadmap", "hito-162-to-nodal-os-mapping-m130-m132.md");

        StringAssert.Contains(text, "HITO-162");
        StringAssert.Contains(text, "NeedsRewrite");
        StringAssert.Contains(text, "UnknownNeedsAudit");
    }

    [TestMethod]
    public void Hito162MappingMapsToNeedsRewriteOrUnknownNeedsAudit()
    {
        var text = ReadDoc("docs", "roadmap", "hito-162-to-nodal-os-mapping-m130-m132.md");

        StringAssert.Contains(text, "HITO-162 | Identity/Fingerprint v2");
        StringAssert.Contains(text, "Paused/not forgotten/UnknownNeedsAudit");
        StringAssert.Contains(text, "M133-M135");
    }

    [TestMethod]
    public void Hito162MappingIncludesM51M65AndPrivatePreview()
    {
        var text = ReadDoc("docs", "roadmap", "hito-162-to-nodal-os-mapping-m130-m132.md");

        StringAssert.Contains(text, "External HTTP proof M51");
        StringAssert.Contains(text, "Closed with HTTP read-only scope");
        StringAssert.Contains(text, "External CDP proof M65");
        StringAssert.Contains(text, "Closed with limited target-owned scope");
        StringAssert.Contains(text, "Product/Admin private preview");
    }

    [TestMethod]
    public void Hito162MappingIncludesNextHitoSuggestions()
    {
        var text = ReadDoc("docs", "roadmap", "hito-162-to-nodal-os-mapping-m130-m132.md");

        StringAssert.Contains(text, "M133-M135");
        StringAssert.Contains(text, "M136-M138");
        StringAssert.Contains(text, "M139-M141");
        StringAssert.Contains(text, "Recommended Next Action");
    }

    [TestMethod]
    public void Hito162ReplacementSequenceExists()
    {
        var text = ReadDoc("docs", "roadmap", "nodal-os-hito-162-replacement-sequence.md");

        StringAssert.Contains(text, "Do not resume HITO-162 as-is");
        StringAssert.Contains(text, "M133-M135");
        StringAssert.Contains(text, "M136-M138");
        StringAssert.Contains(text, "M139-M141");
        StringAssert.Contains(text, "M142-M144");
    }

    [TestMethod]
    public void Hito162ReplacementSequenceDefinesNextBlocksAndBlockers()
    {
        var text = ReadDoc("docs", "roadmap", "nodal-os-hito-162-replacement-sequence.md");

        StringAssert.Contains(text, "Identity/Fingerprint v2 Local Fixture-First");
        StringAssert.Contains(text, "Robust Perception Stabilization");
        StringAssert.Contains(text, "Safe Action Expansion Design and Local Fixtures");
        StringAssert.Contains(text, "No SaaS public");
        StringAssert.Contains(text, "No external CDP general-ready claim");
    }

    [TestMethod]
    public void Hito162RewriteDecisionAdrExists()
    {
        var text = ReadDoc("docs", "adr", "hito-162-rewrite-decision-m130-m132.md");

        StringAssert.Contains(text, "HITO-162 will not be resumed blindly");
        StringAssert.Contains(text, "M133-M135");
        StringAssert.Contains(text, "M136-M138");
        StringAssert.Contains(text, "external CDP general-ready");
    }

    [TestMethod]
    public void Hito162ReplacementSequenceIncludesPercentages()
    {
        var text = ReadDoc("docs", "roadmap", "nodal-os-hito-162-replacement-sequence.md");

        StringAssert.Contains(text, "NODAL OS general: 98%");
        StringAssert.Contains(text, "Private preview local: 98%");
        StringAssert.Contains(text, "Security/evidence integrity: 95-97%");
        StringAssert.Contains(text, "HITO-162 replacement readiness: 70-80%");
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
