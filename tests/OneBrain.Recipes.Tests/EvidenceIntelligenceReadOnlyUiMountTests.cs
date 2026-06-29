using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Evidence;

namespace OneBrain.Recipes.Tests;

[TestClass]
[TestCategory("EvidenceIntelligence")]
[TestCategory("EvidenceIntelligenceReadOnlyUiMount")]
public sealed class EvidenceIntelligenceReadOnlyUiMountTests
{
    [TestMethod]
    public void UiMountUsesReadOnlyPresenterAndDeterministicFixture()
    {
        var mount = EvidenceIntelligenceReadOnlyUiMount.CreateFixture();

        Assert.AreEqual(EvidenceIntelligenceReadOnlyUiMount.MountId, mount.MountId);
        Assert.AreEqual("#evidenceIntelligenceSurface", mount.Route);
        Assert.AreEqual("Evidence Intelligence", mount.NavigationLabel);
        Assert.AreEqual("evidence-intelligence.read-only-surface.v1", mount.Surface.SurfaceId);
        Assert.IsTrue(mount.RouteVisible);
        Assert.IsTrue(mount.UsesPresenter);
        Assert.IsTrue(mount.UsesDeterministicFixture);
        Assert.IsTrue(mount.Surface.SearchPanel.ResultCount > 0);
        Assert.IsTrue(mount.Surface.SearchPanel.Results.Any(result => result.EvidenceId == "ev.uia.delete"));
    }

    [TestMethod]
    public void UiMountExposesRequiredReadOnlyLocalNoRuntimeNotices()
    {
        var mount = EvidenceIntelligenceReadOnlyUiMount.CreateFixture();
        var text = MountText(mount);

        foreach (var expected in new[]
        {
            "READ_ONLY",
            "LOCAL_ONLY",
            "NO_RUNTIME",
            "Read-only.",
            "Local fixture / local evidence only.",
            "Semantic backend disabled.",
            "No runtime actions.",
            "No browser/CDP automation.",
            "No WCU live.",
            "No OCR live.",
            "No filesystem writes.",
            "No provider/cloud calls.",
            "Human approval required for any real-world action."
        })
        {
            StringAssert.Contains(text, expected);
        }
    }

    [TestMethod]
    public void UiMountDoesNotEnableRuntimeProviderPersistenceOrSemanticBackend()
    {
        var mount = EvidenceIntelligenceReadOnlyUiMount.CreateFixture();

        Assert.IsTrue(mount.ReadOnly);
        Assert.IsTrue(mount.LocalOnly);
        Assert.IsFalse(mount.RuntimeEnabled);
        Assert.IsFalse(mount.BrowserCdpAutomationEnabled);
        Assert.IsFalse(mount.WcuLiveEnabled);
        Assert.IsFalse(mount.OcrLiveEnabled);
        Assert.IsFalse(mount.ProviderCloudEnabled);
        Assert.IsFalse(mount.DurablePersistenceEnabled);
        Assert.IsFalse(mount.SemanticVectorBackendEnabled);
        Assert.IsFalse(mount.FilesystemWritesEnabled);
        Assert.IsFalse(mount.Surface.RuntimeEnabled);
        Assert.IsFalse(mount.Surface.ActionExecutionEnabled);
        Assert.IsFalse(mount.Surface.UsesProviderOrCloud());
        Assert.IsFalse(mount.Surface.SemanticNotice.SemanticSearchAvailable);
    }

    [TestMethod]
    public void UiMountIncludesRenderableEmptyMissingStaleAndLowConfidenceSections()
    {
        var mount = EvidenceIntelligenceReadOnlyUiMount.CreateFixture();
        var text = MountText(mount);

        StringAssert.Contains(text, "Evidence Index Summary");
        StringAssert.Contains(text, "Lexical Search Results");
        StringAssert.Contains(text, "Claim Scan Verdict");
        StringAssert.Contains(text, "Action Scan Verdict");
        StringAssert.Contains(text, "Contradictions");
        StringAssert.Contains(text, "Missing/Stale/Low-Confidence Evidence");
        StringAssert.Contains(text, "Typed Evidence Graph");
        StringAssert.Contains(text, "Action Readiness Matrix");
        StringAssert.Contains(text, "Required Human Actions");
        StringAssert.Contains(text, "Safe Next Step");
        Assert.IsTrue(mount.Surface.IndexSummary.StaleOrUnknownCount > 0);
        Assert.IsTrue(mount.Surface.IndexSummary.RedactedCount > 0);
        Assert.IsTrue(mount.Surface.ActionScanPanel.BlockingReasons.Count > 0);
    }

    [TestMethod]
    public void UiMountDoesNotExposeLiveActionLabelsOrFakeSemanticClaims()
    {
        var mount = EvidenceIntelligenceReadOnlyUiMount.CreateFixture();
        var text = MountText(mount);
        var forbidden = new[]
        {
            "executes " + "actions",
            "runs browser " + "automation",
            "live CDP " + "automation",
            "semantic search " + "enabled",
            "writes " + "files",
            "auto-" + "apply",
            "production automation " + "ready"
        };

        foreach (var term in forbidden)
        {
            Assert.IsFalse(text.Contains(term, StringComparison.OrdinalIgnoreCase), term);
        }
    }

    private static string MountText(EvidenceIntelligenceReadOnlyUiMountViewModel mount)
    {
        var parts = new List<string>
        {
            mount.MountId,
            mount.Route,
            mount.NavigationLabel,
            mount.Surface.Header.Title,
            mount.Surface.SemanticNotice.Copy,
            mount.Surface.NoRuntimeNotice,
            mount.Surface.SearchPanel.EmptyState,
            mount.Surface.ActionScanPanel.SafeNextStep,
            mount.Surface.GraphSummary.Summary
        };
        parts.AddRange(mount.StatusBadges);
        parts.AddRange(mount.SafetyNotices);
        parts.AddRange(mount.VisibleSections);
        parts.AddRange(mount.AllowedUiActions);
        parts.AddRange(mount.ForbiddenUiActions);
        parts.AddRange(mount.Surface.LocalOnlyNotices);
        return string.Join(" ", parts);
    }
}

internal static class EvidenceIntelligenceSurfaceViewModelTestExtensions
{
    public static bool UsesProviderOrCloud(this EvidenceIntelligenceSurfaceViewModel surface) =>
        surface.CallsProviderOrNetwork || surface.UsesCloud;
}
