using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("PauseClosure")]
[TestCategory("AutomationLayerIntegrationNoDivergence")]
[TestCategory("RecipeRiskClassifier")]
public sealed class NodalOsPauseClosureM463Tests
{
    [TestMethod]
    public void PauseClosureReportExists() =>
        Assert.IsTrue(File.Exists(ReportPath()));

    [TestMethod]
    public void PauseClosureArtifactExists() =>
        Assert.IsTrue(File.Exists(ArtifactPath()));

    [TestMethod]
    public void ArtifactMarksPauseClosed() =>
        AssertContains(ArtifactText(), "\"pauseClosed\": true");

    [TestMethod]
    public void ArtifactMarksReadyForCoreRoadmapReturn() =>
        AssertContains(ArtifactText(), "\"readyForCoreRoadmapReturn\": true");

    [TestMethod]
    public void ArtifactMarksReadyForNewTopicsIntake() =>
        AssertContains(ArtifactText(), "\"readyForNewTopicsIntake\": true");

    [TestMethod]
    public void ArtifactMarksAutomationLayerDesignClosed() =>
        AssertContains(ArtifactText(), "\"automationLayerDesignClosed\": true");

    [TestMethod]
    public void ArtifactMarksRuntimeImplementationDeferred() =>
        AssertContains(ArtifactText(), "\"runtimeImplementationDeferred\": true");

    [TestMethod]
    public void ArtifactMarksRecipeRiskClassifierHardeningRuntimeGated() =>
        AssertContains(ArtifactText(), "\"recipeRiskClassifierHardeningRuntimeGated\": true");

    [TestMethod]
    public void ArtifactMarksNoRecorderReplayQueueSchedulerBrowserAutomation()
    {
        var artifact = ArtifactText();

        AssertContains(artifact, "\"noRecorderImplemented\": true");
        AssertContains(artifact, "\"noReplayImplemented\": true");
        AssertContains(artifact, "\"noQueueImplemented\": true");
        AssertContains(artifact, "\"noSchedulerImplemented\": true");
        AssertContains(artifact, "\"noBrowserAutomationImplemented\": true");
    }

    [TestMethod]
    public void ArtifactMarksNoDslParserUiExecution()
    {
        var artifact = ArtifactText();

        AssertContains(artifact, "\"noDslParserImplemented\": true");
        AssertContains(artifact, "\"noUiImplemented\": true");
        AssertContains(artifact, "\"noExecutionImplemented\": true");
    }

    [TestMethod]
    public void ReportMentionsM440ThroughM462()
    {
        var report = ReportText();

        foreach (var milestone in new[] { "M440-M442", "M443-M445", "M446-M448", "M449-M451", "M452-M454", "M455-M457", "M458-M459", "M460-M462" })
            AssertContains(report, milestone);
    }

    [TestMethod]
    public void ReportMentionsRuntimeGatedClassifierHardening()
    {
        var report = ReportText();

        AssertContains(report, "Recipe Risk Classifier hardening remains runtime-gated");
        AssertContains(report, "drop table after reading status");
        AssertContains(report, "new dedicated Claude runtime audit");
    }

    [TestMethod]
    public void ReportMentionsNewTopicsRequirePlanningIntake()
    {
        var report = ReportText();

        AssertContains(report, "New documents and themes can now be incorporated");
        AssertContains(report, "through planning intake first");
    }

    [TestMethod]
    public void RoadmapUpdatedWithM463() =>
        AssertContains(RoadmapText(), "Pause Closure and Core Roadmap Re-Sync M463");

    [TestMethod]
    public void RoadmapDoesNotRecommendBrowserAutomationRuntimeNext()
    {
        var m463 = RoadmapM463Section();

        AssertContains(m463, "Do not proceed to real browser automation yet");
        Assert.IsFalse(m463.Contains("Recommended next milestone: Browser Automation", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(m463.Contains("Browser Adapter runtime next", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void RoadmapRecommendsCoreRoadmapOrNewTopicsIntake()
    {
        var m463 = RoadmapM463Section();

        AssertContains(m463, "New topics intake / unified roadmap update before implementation");
        AssertContains(m463, "Core mandatory");
    }

    private static string ReportText() =>
        File.ReadAllText(ReportPath());

    private static string ArtifactText() =>
        File.ReadAllText(ArtifactPath());

    private static string RoadmapText() =>
        File.ReadAllText(Path.Combine(RepoRoot(), "docs", "roadmap", "nodal-os-roadmap-vnext.md"));

    private static string RoadmapM463Section()
    {
        var roadmap = RoadmapText();
        const string heading = "## Pause Closure and Core Roadmap Re-Sync M463";
        var start = roadmap.IndexOf(heading, StringComparison.Ordinal);
        Assert.IsTrue(start >= 0, heading);

        var next = roadmap.IndexOf("\n## ", start + heading.Length, StringComparison.Ordinal);
        return next < 0 ? roadmap[start..] : roadmap[start..next];
    }

    private static string ReportPath() =>
        Path.Combine(RepoRoot(), "docs", "reports", "pause-closure-core-roadmap-resync-m463.md");

    private static string ArtifactPath() =>
        Path.Combine(RepoRoot(), "artifacts", "agent-operations", "m463", "pause-closure-core-roadmap-resync-summary.json");

    private static void AssertContains(string text, string expected) =>
        Assert.IsTrue(text.Contains(expected, StringComparison.Ordinal), expected);

    private static string RepoRoot()
    {
        var current = AppContext.BaseDirectory;
        while (!string.IsNullOrWhiteSpace(current))
        {
            if (File.Exists(Path.Combine(current, "OneBrain.slnx")))
                return current;

            current = Directory.GetParent(current)?.FullName;
        }

        throw new DirectoryNotFoundException("Could not locate repository root.");
    }
}
