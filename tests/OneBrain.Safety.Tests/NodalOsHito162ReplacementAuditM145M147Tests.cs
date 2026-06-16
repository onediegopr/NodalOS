using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
public sealed class NodalOsHito162ReplacementAuditM145M147Tests
{
    [TestMethod]
    public void Hito162ReplacementAuditDocumentExistsAndCoversM133M144()
    {
        var text = ReadDoc("docs", "audits", "hito-162-replacement-audit-m145-m147.md");

        StringAssert.Contains(text, "M133-M135");
        StringAssert.Contains(text, "M136-M138");
        StringAssert.Contains(text, "M139-M141");
        StringAssert.Contains(text, "M142-M144");
        StringAssert.Contains(text, "HITO-162 replacement is stable local fixture-first");
    }

    [TestMethod]
    public void Hito162ReplacementAuditListsActiveBlockersAndNoScopeInflation()
    {
        var text = ReadDoc("docs", "audits", "hito-162-replacement-audit-m145-m147.md");

        StringAssert.Contains(text, "production/SaaS public blocked");
        StringAssert.Contains(text, "productive recorder/replay blocked");
        StringAssert.Contains(text, "external CDP general-ready blocked");
        Assert.IsFalse(text.Contains("production ready", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(text.Contains("external general CDP ready", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void CrossSignalConsistencyConsistentFixtureChainPasses()
    {
        var result = new NodalOsCrossSignalConsistencyGate().Evaluate(
            NodalOsCrossSignalConsistencyGate.ConsistentFixtureInput());

        Assert.AreEqual(NodalOsCrossSignalConsistencyStatus.Consistent, result.Status);
        Assert.AreEqual(NodalOsHito162ReplacementReadiness.StableLocalFixtureFirst, result.Readiness);
        Assert.IsTrue(result.Consistent);
        Assert.IsFalse(result.ActionAuthorityGranted);
    }

    [TestMethod]
    public void CrossSignalConsistencyHighIdentityButBlockedPerceptionDeniesAction()
    {
        var input = NodalOsCrossSignalConsistencyGate.ConsistentFixtureInput() with
        {
            PerceptionReadiness = NodalOsPerceptionReadiness.Blocked,
            ActionDecision = NodalOsActionDecision.AllowedLocalReadOnlyWithCoreApproval
        };

        var result = new NodalOsCrossSignalConsistencyGate().Evaluate(input);

        Assert.AreEqual(NodalOsCrossSignalConsistencyStatus.PerceptionActionMismatch, result.Status);
        Assert.IsTrue(result.Mismatches.Contains(NodalOsCrossSignalMismatch.PerceptionActionMismatch));
    }

    [TestMethod]
    public void CrossSignalConsistencyHighPerceptionWithoutCoreDeniesAction()
    {
        var input = NodalOsCrossSignalConsistencyGate.ConsistentFixtureInput() with
        {
            CoreApproved = false,
            ActionDecision = NodalOsActionDecision.AllowedLocalReadOnlyWithCoreApproval
        };

        var result = new NodalOsCrossSignalConsistencyGate().Evaluate(input);

        Assert.AreEqual(NodalOsCrossSignalConsistencyStatus.MissingCoreAuthority, result.Status);
        Assert.IsTrue(result.Mismatches.Contains(NodalOsCrossSignalMismatch.MissingCoreAuthority));
    }

    [TestMethod]
    public void CrossSignalConsistencyBlockedActionCannotCreateAcceptedMemory()
    {
        var input = NodalOsCrossSignalConsistencyGate.ConsistentFixtureInput() with
        {
            ActionDecision = NodalOsActionDecision.BlockedAlways,
            ActionDeniedReasons = [NodalOsActionDeniedReason.SubmitBlocked],
            MemoryAccepted = true
        };

        var result = new NodalOsCrossSignalConsistencyGate().Evaluate(input);

        Assert.AreEqual(NodalOsCrossSignalConsistencyStatus.ActionMemoryMismatch, result.Status);
        Assert.IsTrue(result.Mismatches.Contains(NodalOsCrossSignalMismatch.ActionMemoryMismatch));
    }

    [TestMethod]
    public void CrossSignalConsistencyMemoryCannotUnblockBlockedAction()
    {
        var input = NodalOsCrossSignalConsistencyGate.ConsistentFixtureInput() with
        {
            ActionDecision = NodalOsActionDecision.Denied,
            ActionDeniedReasons = [NodalOsActionDeniedReason.OverlayBlocked],
            MemoryConfidence = NodalOsMemoryConfidence.VerifiedFixturePattern,
            MemoryAccepted = true
        };

        var result = new NodalOsCrossSignalConsistencyGate().Evaluate(input);

        Assert.AreEqual(NodalOsCrossSignalConsistencyStatus.ActionMemoryMismatch, result.Status);
    }

    [TestMethod]
    public void CrossSignalConsistencySensitiveSurfaceTriggersScopeInflation()
    {
        var input = NodalOsCrossSignalConsistencyGate.ConsistentFixtureInput() with
        {
            SensitiveSurface = true
        };

        var result = new NodalOsCrossSignalConsistencyGate().Evaluate(input);

        Assert.AreEqual(NodalOsCrossSignalConsistencyStatus.ScopeInflationDetected, result.Status);
        Assert.IsTrue(result.ScopeInflationDetected);
    }

    [TestMethod]
    public void CrossSignalConsistencyUnredactedEvidenceBlocksMemorySummary()
    {
        var input = NodalOsCrossSignalConsistencyGate.ConsistentFixtureInput() with
        {
            MemoryRedacted = false
        };

        var result = new NodalOsCrossSignalConsistencyGate().Evaluate(input);

        Assert.AreEqual(NodalOsCrossSignalConsistencyStatus.ActionMemoryMismatch, result.Status);
        Assert.IsTrue(result.Mismatches.Contains(NodalOsCrossSignalMismatch.UnredactedEvidence));
    }

    [TestMethod]
    public void CrossSignalConsistencyAmbiguousStateRequiresHumanReview()
    {
        var input = NodalOsCrossSignalConsistencyGate.ConsistentFixtureInput() with
        {
            AmbiguousState = true
        };

        var result = new NodalOsCrossSignalConsistencyGate().Evaluate(input);

        Assert.AreEqual(NodalOsCrossSignalConsistencyStatus.RequiresHumanReview, result.Status);
        Assert.IsTrue(result.Mismatches.Contains(NodalOsCrossSignalMismatch.RequiresHumanReview));
    }

    [TestMethod]
    public void Hito162ReplacementFinalReviewExistsAndDefinesNextPhase()
    {
        var text = ReadDoc("docs", "adr", "hito-162-replacement-final-review-m145-m147.md");

        StringAssert.Contains(text, "Hito162ReplacementStable");
        StringAssert.Contains(text, "Product/Admin polish");
        StringAssert.Contains(text, "embedded runtime evaluation");
        StringAssert.Contains(text, "Chromium fork");
    }

    [TestMethod]
    public void Hito162ReplacementReadinessRoadmapsKeepExternalGeneralBlocked()
    {
        var sequence = ReadDoc("docs", "roadmap", "nodal-os-hito-162-replacement-sequence.md");
        var vnext = ReadDoc("docs", "roadmap", "nodal-os-roadmap-vnext.md");

        StringAssert.Contains(sequence, "HITO-162 replacement stable local fixture-first");
        StringAssert.Contains(sequence, "embedded runtime");
        StringAssert.Contains(sequence, "Chromium fork");
        StringAssert.Contains(vnext, "External CDP general-ready remains false");
        StringAssert.Contains(vnext, "Production, SaaS public");
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
