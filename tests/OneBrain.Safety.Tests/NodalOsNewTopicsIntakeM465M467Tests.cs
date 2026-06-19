using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("NewTopicsIntake")]
[TestCategory("PauseClosure")]
[TestCategory("NamingCleanup")]
public sealed class NodalOsNewTopicsIntakeM465M467Tests
{
    [TestMethod]
    public void NewTopicsIntakeReportExists() =>
        Assert.IsTrue(File.Exists(IntakePath()));

    [TestMethod]
    public void UnifiedRoadmapPostPauseExists() =>
        Assert.IsTrue(File.Exists(UnifiedRoadmapPath()));

    [TestMethod]
    public void ArtifactExists() =>
        Assert.IsTrue(File.Exists(ArtifactPath()));

    [TestMethod]
    public void ArtifactOperationalProjectNameIsNodalOs() =>
        AssertContains(ArtifactText(), "\"operationalProjectName\": \"NODAL OS\"");

    [TestMethod]
    public void ArtifactMarksPauseClosedClean() =>
        AssertContains(ArtifactText(), "\"pauseWasClosedClean\": true");

    [TestMethod]
    public void ArtifactMarksNodrixExternalInputOnly() =>
        AssertContains(ArtifactText(), "\"nodrixTreatedAsExternalInputOnly\": true");

    [TestMethod]
    public void ArtifactMarksHotepExternalVisualInputOnly() =>
        AssertContains(ArtifactText(), "\"hotepTreatedAsExternalVisualInputOnly\": true");

    [TestMethod]
    public void ArtifactMarksNoNamingMix() =>
        AssertContains(ArtifactText(), "\"noNamingMix\": true");

    [TestMethod]
    public void ArtifactMarksNoRuntimeImplemented() =>
        AssertContains(ArtifactText(), "\"noRuntimeImplemented\": true");

    [TestMethod]
    public void ArtifactMarksNoUiImplemented() =>
        AssertContains(ArtifactText(), "\"noUiImplemented\": true");

    [TestMethod]
    public void ArtifactMarksNoCloudImplemented() =>
        AssertContains(ArtifactText(), "\"noCloudImplemented\": true");

    [TestMethod]
    public void ArtifactMarksNoAutomationRuntimeImplemented() =>
        AssertContains(ArtifactText(), "\"noAutomationRuntimeImplemented\": true");

    [TestMethod]
    public void UnifiedRoadmapRecommendsCoreReturn()
    {
        var roadmap = UnifiedRoadmapText();

        AssertContains(roadmap, "Subphase 1 - Core Mandatory Immediate");
        AssertContains(roadmap, "Execution Registry + EventBus");
        AssertContains(roadmap, "M468-M470 Core Runtime Registry EventBus Redaction Planning or Execution Registry + EventBus");
    }

    [TestMethod]
    public void UnifiedRoadmapDefersAutomationRuntime()
    {
        var roadmap = UnifiedRoadmapText();

        AssertContains(roadmap, "Automation runtime remains deferred");
        AssertContains(roadmap, "Do not proceed to UI implementation, cloud implementation, browser automation runtime");
    }

    [TestMethod]
    public void UnifiedRoadmapMentionsClassifierHardeningRuntimeGated()
    {
        var roadmap = UnifiedRoadmapText();

        AssertContains(roadmap, "Classifier hardening is runtime-gated");
        AssertContains(roadmap, "Recipe Risk Classifier hardening");
    }

    [TestMethod]
    public void RoadmapVNextReferencesM465M467()
    {
        var roadmap = File.ReadAllText(Path.Combine(RepoRoot(), "docs", "roadmap", "nodal-os-roadmap-vnext.md"));

        AssertContains(roadmap, "M465-M467 New Topics Intake + Unified Roadmap Update");
        AssertContains(roadmap, "nodal-os-unified-roadmap-post-pause.md");
    }

    private static string IntakePath() =>
        Path.Combine(RepoRoot(), "docs", "reports", "new-topics-intake-m465.md");

    private static string UnifiedRoadmapPath() =>
        Path.Combine(RepoRoot(), "docs", "roadmap", "nodal-os-unified-roadmap-post-pause.md");

    private static string ArtifactPath() =>
        Path.Combine(RepoRoot(), "artifacts", "agent-operations", "m467", "new-topics-intake-unified-roadmap-summary.json");

    private static string ArtifactText() =>
        File.ReadAllText(ArtifactPath());

    private static string UnifiedRoadmapText() =>
        File.ReadAllText(UnifiedRoadmapPath());

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
