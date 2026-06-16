using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
public sealed class NodalOsLocalPreviewReleaseCandidateM151M153Tests
{
    [TestMethod]
    public void LocalPreviewFinalAuditPackExistsAndContainsRequiredScope()
    {
        var text = ReadDoc("docs", "audits", "nodal-os-local-private-preview-final-audit-m151-m153.md");

        StringAssert.Contains(text, "NODAL OS");
        StringAssert.Contains(text, "M51");
        StringAssert.Contains(text, "M65");
        StringAssert.Contains(text, "HITO-162 replacement stable local fixture-first");
        StringAssert.Contains(text, "production blocked");
        StringAssert.Contains(text, "SaaS public blocked");
    }

    [TestMethod]
    public void LocalPreviewFinalAuditPackDoesNotDeclareProductionReady()
    {
        var text = ReadDoc("docs", "audits", "nodal-os-local-private-preview-final-audit-m151-m153.md");

        Assert.IsFalse(text.Contains("production ready: true", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(text.Contains("SaaS public ready", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(text.Contains("external CDP general-ready: true", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void FinalAuditSummaryJsonExistsAndMatchesReleaseCandidate()
    {
        using var json = JsonDocument.Parse(ReadDoc("artifacts", "local-private-preview-final-audit-m151-m153", "final-audit-summary.json"));
        var root = json.RootElement;

        Assert.AreEqual("NODAL OS", root.GetProperty("productName").GetString());
        Assert.AreEqual("FrozenReadyForInternalLocalUseVerified", root.GetProperty("readinessState").GetString());
        Assert.AreEqual("FrozenReadyForInternalLocalUseVerified", root.GetProperty("recommendation").GetString());
        Assert.IsFalse(root.GetProperty("externalGeneralCdpReady").GetBoolean());
        Assert.AreEqual(29, root.GetProperty("suiteResults").GetProperty("OneBrain.Safety.Tests").GetProperty("skipped").GetInt32());
    }

    [TestMethod]
    public void ReleaseCandidateFreezeValidStateIsFrozenReadyForExternalAudit()
    {
        var result = new NodalOsLocalPreviewReleaseCandidateFreezeService().Freeze(DefaultCandidate());

        Assert.AreEqual(NodalOsReleaseCandidateDecision.FrozenReadyForExternalAudit, result.Decision);
        Assert.AreEqual(NodalOsLocalPreviewReleaseCandidateState.FrozenReadyForExternalAudit, result.State);
        Assert.IsTrue(result.ReadyForClaudeAudit);
        Assert.IsFalse(result.ScopeInflationDetected);
    }

    [TestMethod]
    public void ReleaseCandidateFreezeBlocksProductionEnabled()
    {
        var candidate = DefaultCandidate() with
        {
            Scope = NodalOsLocalPreviewReleaseCandidateFreezeService.DefaultScope() with { ProductionEnabled = true }
        };

        var result = new NodalOsLocalPreviewReleaseCandidateFreezeService().Freeze(candidate);

        Assert.AreEqual(NodalOsReleaseCandidateDecision.BlockedByScopeInflation, result.Decision);
    }

    [TestMethod]
    public void ReleaseCandidateFreezeBlocksExternalGeneralCdp()
    {
        var candidate = DefaultCandidate() with
        {
            Scope = NodalOsLocalPreviewReleaseCandidateFreezeService.DefaultScope() with { ExternalGeneralCdpReady = true }
        };

        var result = new NodalOsLocalPreviewReleaseCandidateFreezeService().Freeze(candidate);

        Assert.AreEqual(NodalOsReleaseCandidateDecision.BlockedByScopeInflation, result.Decision);
    }

    [TestMethod]
    public void ReleaseCandidateFreezeBlocksMissingM65Evidence()
    {
        var candidate = DefaultCandidate() with { M51M65EvidenceVerified = false };

        var result = new NodalOsLocalPreviewReleaseCandidateFreezeService().Freeze(candidate);

        Assert.AreEqual(NodalOsReleaseCandidateDecision.BlockedByMissingEvidence, result.Decision);
    }

    [TestMethod]
    public void ReleaseCandidateFreezeBlocksHighIssue()
    {
        var candidate = DefaultCandidate() with { HasHighOrCriticalIssues = true };

        var result = new NodalOsLocalPreviewReleaseCandidateFreezeService().Freeze(candidate);

        Assert.AreEqual(NodalOsReleaseCandidateDecision.BlockedBySecurity, result.Decision);
    }

    [TestMethod]
    public void ReleaseCandidateFreezeBlocksWorktreeMismatch()
    {
        var candidate = DefaultCandidate() with { WorktreeCanonical = false };

        var result = new NodalOsLocalPreviewReleaseCandidateFreezeService().Freeze(candidate);

        Assert.AreEqual(NodalOsReleaseCandidateDecision.BlockedByWorktree, result.Decision);
    }

    [TestMethod]
    public void ClaudeFinalAuditPromptExistsAndAsksForHardAudit()
    {
        var text = ReadDoc("docs", "audits", "claude-final-audit-prompt-local-preview-rc-m153.md");

        StringAssert.Contains(text, "hard audit");
        StringAssert.Contains(text, "GO/NO GO");
        StringAssert.Contains(text, "confidence 1-10");
        StringAssert.Contains(text, "M51");
        StringAssert.Contains(text, "M65");
        StringAssert.Contains(text, "HITO-162 replacement");
        StringAssert.Contains(text, "active blockers");
    }

    [TestMethod]
    public void ReleaseCandidateReviewExistsAndDoesNotDeclareSaasOrProductionReady()
    {
        var text = ReadDoc("docs", "adr", "local-private-preview-release-candidate-review-m151-m153.md");

        StringAssert.Contains(text, "Release Candidate local/private preview");
        StringAssert.Contains(text, "FrozenReadyForExternalAudit");
        StringAssert.Contains(text, "Claude final audit");
        Assert.IsFalse(text.Contains("SaaS public ready", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(text.Contains("production ready: true", StringComparison.OrdinalIgnoreCase));
    }

    private static NodalOsLocalPreviewReleaseCandidate DefaultCandidate() =>
        NodalOsLocalPreviewReleaseCandidateFreezeService.DefaultCandidate("ee0948b98afb353cd0da32a7082200379780ddb2");

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
