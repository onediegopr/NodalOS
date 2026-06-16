using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
public sealed class NodalOsPrivatePreviewAuditM118M120Tests
{
    [TestMethod]
    public void CoreAuditPackExistsAndContainsNodalOsReadiness()
    {
        var text = ReadDoc("docs", "audits", "nodal-os-core-private-preview-audit-pack-m118-m120.md");

        StringAssert.Contains(text, "NODAL OS");
        StringAssert.Contains(text, "M51");
        StringAssert.Contains(text, "M65");
        StringAssert.Contains(text, "ReadyWithRestrictions");
        StringAssert.Contains(text, "SaaS public blocked");
        StringAssert.Contains(text, "External CDP general-ready: false");
    }

    [TestMethod]
    public void CoreAuditPackDoesNotDeclareProductionOrSaasReady()
    {
        var text = ReadDoc("docs", "audits", "nodal-os-core-private-preview-audit-pack-m118-m120.md");

        Assert.IsFalse(text.Contains("SaaS public ready", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(text.Contains("production ready", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(text.Contains("external CDP general-ready: true", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void CoreAuditPackSummaryJsonExistsAndMatchesReadyWithRestrictions()
    {
        using var json = JsonDocument.Parse(File.ReadAllText(SourcePath("artifacts", "private-preview-audit-pack-m118-m120", "audit-summary.json")));
        var root = json.RootElement;

        Assert.AreEqual("NODAL OS", root.GetProperty("productName").GetString());
        Assert.AreEqual("ReadyWithRestrictions", root.GetProperty("readinessDecision").GetString());
        Assert.IsFalse(root.GetProperty("externalCdpGeneralReady").GetBoolean());
        Assert.AreEqual(29, root.GetProperty("suiteResults").GetProperty("OneBrain.Safety.Tests").GetProperty("skipped").GetInt32());
    }

    [TestMethod]
    public void PrivatePreviewEvidenceFreezeCorrectSnapshotIsReadyForExternalAudit()
    {
        var result = new NodalOsPrivatePreviewEvidenceFreezeService().Freeze(DefaultSnapshot());

        Assert.AreEqual(NodalOsPrivatePreviewEvidenceFreezeStatus.ReadyForExternalAudit, result.Status);
        Assert.IsTrue(result.ReadyForExternalAudit);
        Assert.IsFalse(result.ScopeInflationDetected);
    }

    [TestMethod]
    public void ScopeInflationGuardDetectsM65GeneralReadyInflation()
    {
        var result = new NodalOsPrivatePreviewEvidenceFreezeService().Freeze(DefaultSnapshot() with { ExternalCdpGeneralReady = true });

        Assert.AreEqual(NodalOsPrivatePreviewEvidenceFreezeStatus.ScopeInflationDetected, result.Status);
        Assert.IsTrue(result.ScopeInflationDetected);
    }

    [TestMethod]
    public void ScopeInflationGuardDetectsPublicSaasAllowed()
    {
        var result = new NodalOsPrivatePreviewEvidenceFreezeService().Freeze(DefaultSnapshot() with { PublicSaasAllowed = true });

        Assert.AreEqual(NodalOsPrivatePreviewEvidenceFreezeStatus.ScopeInflationDetected, result.Status);
    }

    [TestMethod]
    public void ScopeInflationGuardDetectsCredentialsAllowed()
    {
        var result = new NodalOsPrivatePreviewEvidenceFreezeService().Freeze(DefaultSnapshot() with { RealCredentialsAllowed = true });

        Assert.AreEqual(NodalOsPrivatePreviewEvidenceFreezeStatus.ScopeInflationDetected, result.Status);
    }

    [TestMethod]
    public void PrivatePreviewEvidenceFreezeBlocksWorktreeMismatch()
    {
        var result = new NodalOsPrivatePreviewEvidenceFreezeService().Freeze(DefaultSnapshot() with { CanonicalWorktree = false });

        Assert.AreEqual(NodalOsPrivatePreviewEvidenceFreezeStatus.WorktreeMismatch, result.Status);
    }

    [TestMethod]
    public void PrivatePreviewEvidenceFreezeBlocksSkippedTestsMismatch()
    {
        var result = new NodalOsPrivatePreviewEvidenceFreezeService().Freeze(DefaultSnapshot() with { SkippedTestsActual = 30 });

        Assert.AreEqual(NodalOsPrivatePreviewEvidenceFreezeStatus.SkippedTestsMismatch, result.Status);
    }

    [TestMethod]
    public void PrivatePreviewEvidenceFreezeBlocksReleaseGateMismatch()
    {
        var result = new NodalOsPrivatePreviewEvidenceFreezeService().Freeze(DefaultSnapshot() with { ReleaseGateDecision = "NotReady" });

        Assert.AreEqual(NodalOsPrivatePreviewEvidenceFreezeStatus.ReleaseGateMismatch, result.Status);
    }

    [TestMethod]
    public void ExternalAuditPromptExistsAndAsksForCriticalReview()
    {
        var text = ReadDoc("docs", "audits", "claude-audit-prompt-private-preview-m120.md");

        StringAssert.Contains(text, "NODAL OS");
        StringAssert.Contains(text, "ReadyWithRestrictions");
        StringAssert.Contains(text, "M51");
        StringAssert.Contains(text, "M65");
        StringAssert.Contains(text, "limited");
        StringAssert.Contains(text, "Do not provide generic approval");
        StringAssert.Contains(text, "What confidence level would you assign from 1 to 10");
    }

    [TestMethod]
    public void LocalPrivatePreviewFinalReviewExistsAndDoesNotDeclareProductionReady()
    {
        var text = ReadDoc("docs", "adr", "local-private-preview-final-review-m118-m120.md");

        StringAssert.Contains(text, "ReadyWithRestrictions");
        StringAssert.Contains(text, "internal local private preview");
        StringAssert.Contains(text, "This is not production readiness");
        Assert.IsFalse(text.Contains("SaaS public ready", StringComparison.OrdinalIgnoreCase));
    }

    private static NodalOsReleaseEvidenceSnapshot DefaultSnapshot() =>
        NodalOsPrivatePreviewEvidenceFreezeService.DefaultSnapshot("2811615c00f342e5b030b4515aaa3815263a98ef");

    private static string ReadDoc(params string[] relativePath)
    {
        var path = SourcePath(relativePath);
        Assert.IsTrue(File.Exists(path), path);
        return File.ReadAllText(path);
    }

    private static string SourcePath(params string[] relativePath)
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "OneBrain.slnx")))
                return Path.Combine(new[] { current.FullName }.Concat(relativePath).ToArray());
            current = current.Parent;
        }

        Assert.Fail("Could not locate repository root.");
        return "";
    }
}
