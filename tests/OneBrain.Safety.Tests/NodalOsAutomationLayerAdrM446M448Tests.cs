using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("AutomationLayerAdr")]
public sealed class NodalOsAutomationLayerAdrM446M448Tests
{
    [TestMethod]
    public void AutomationLayerDiscoveryExists() =>
        Assert.IsTrue(File.Exists(DiscoveryPath()));

    [TestMethod]
    public void AutomationLayerAdrExists() =>
        Assert.IsTrue(File.Exists(AdrPath()));

    [TestMethod]
    public void AutomationLayerArtifactExists() =>
        Assert.IsTrue(File.Exists(ArtifactPath()));

    [TestMethod]
    public void ArtifactMarksUiVisionReferenceOnly() =>
        AssertArtifactFlag("\"uivisionReferenceOnly\": true");

    [TestMethod]
    public void ArtifactMarksTagUiReferenceOnly() =>
        AssertArtifactFlag("\"taguiReferenceOnly\": true");

    [TestMethod]
    public void ArtifactMarksOpenRpaReferenceOnly() =>
        AssertArtifactFlag("\"openRpaReferenceOnly\": true");

    [TestMethod]
    public void ArtifactMarksNoCopiedCode() =>
        AssertArtifactFlag("\"noCopiedCode\": true");

    [TestMethod]
    public void ArtifactMarksNoExternalRpaDependency() =>
        AssertArtifactFlag("\"noExternalRpaDependency\": true");

    [TestMethod]
    public void ArtifactMarksNoAgplCodeIncluded() =>
        AssertArtifactFlag("\"noAgplCodeIncluded\": true");

    [TestMethod]
    public void ArtifactMarksNoClassicRpaIdentity() =>
        AssertArtifactFlag("\"noClassicRpaIdentity\": true");

    [TestMethod]
    public void ArtifactMarksMissionControlFirst() =>
        AssertArtifactFlag("\"missionControlFirst\": true");

    [TestMethod]
    public void ArtifactMarksApprovalFirst() =>
        AssertArtifactFlag("\"approvalFirst\": true");

    [TestMethod]
    public void ArtifactMarksEvidenceFirst() =>
        AssertArtifactFlag("\"evidenceFirst\": true");

    [TestMethod]
    public void ArtifactMarksTimelineFirst() =>
        AssertArtifactFlag("\"timelineFirst\": true");

    [TestMethod]
    public void ArtifactMarksLocalFirst() =>
        AssertArtifactFlag("\"localFirst\": true");

    [TestMethod]
    public void ArtifactMarksRecorderFutureOnly() =>
        AssertArtifactFlag("\"recorderFutureOnly\": true");

    [TestMethod]
    public void ArtifactMarksRecipeDslFutureOnly() =>
        AssertArtifactFlag("\"recipeDslFutureOnly\": true");

    [TestMethod]
    public void ArtifactMarksWorkQueueFutureOnly() =>
        AssertArtifactFlag("\"workQueueFutureOnly\": true");

    [TestMethod]
    public void ArtifactMarksTriggerPolicyFutureOnly() =>
        AssertArtifactFlag("\"triggerPolicyFutureOnly\": true");

    [TestMethod]
    public void ArtifactMarksNoRecorderImplemented() =>
        AssertArtifactFlag("\"noRecorderImplemented\": true");

    [TestMethod]
    public void ArtifactMarksNoReplayImplemented() =>
        AssertArtifactFlag("\"noReplayImplemented\": true");

    [TestMethod]
    public void ArtifactMarksNoQueueImplemented() =>
        AssertArtifactFlag("\"noQueueImplemented\": true");

    [TestMethod]
    public void ArtifactMarksNoSchedulerImplemented() =>
        AssertArtifactFlag("\"noSchedulerImplemented\": true");

    [TestMethod]
    public void ArtifactMarksNoBrowserAutomationImplemented() =>
        AssertArtifactFlag("\"noBrowserAutomationImplemented\": true");

    [TestMethod]
    public void ArtifactMarksNoUiImplemented() =>
        AssertArtifactFlag("\"noUiImplemented\": true");

    [TestMethod]
    public void ArtifactMarksNoExecutionImplemented() =>
        AssertArtifactFlag("\"noExecutionImplemented\": true");

    [TestMethod]
    public void AdrRejectsClassicRpaDesignerIdentity()
    {
        var adr = ReadAdr();

        AssertContains(adr, "not classic RPA");
        AssertContains(adr, "No workflow designer");
        AssertContains(adr, "not a classic RPA suite");
    }

    [TestMethod]
    public void AdrSaysDslIsNotRuntime() =>
        AssertContains(ReadAdr(), "The DSL is representation, not runtime.");

    [TestMethod]
    public void AdrSaysImportDoesNotMeanDirectExecution() =>
        AssertContains(ReadAdr(), "Recipe import never means direct execution.");

    [TestMethod]
    public void AdrSaysVisualOcrOnlyFallback() =>
        AssertContains(ReadAdr(), "Visual/OCR is only fallback or evidence.");

    [TestMethod]
    public void AdrUsesNodrixOrNodalOsName_NotNexa()
    {
        var adr = ReadAdr();

        Assert.IsTrue(
            adr.Contains("NODRIX", StringComparison.Ordinal) ||
            adr.Contains("NODAL OS", StringComparison.Ordinal));
        Assert.IsFalse(adr.Contains("NEXA", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void NoRpaDependenciesAddedToCsproj()
    {
        var forbidden = new[]
        {
            "UI.Vision",
            "UIVision",
            "TagUI",
            "OpenRPA",
            "OpenIAP",
            "Kantu"
        };

        foreach (var project in Directory.GetFiles(RepoRoot(), "*.csproj", SearchOption.AllDirectories))
        {
            if (project.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase) ||
                project.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var text = File.ReadAllText(project);
            foreach (var dependency in forbidden)
                Assert.IsFalse(text.Contains(dependency, StringComparison.OrdinalIgnoreCase), $"{dependency} found in {project}");
        }
    }

    private static void AssertArtifactFlag(string expected) =>
        AssertContains(File.ReadAllText(ArtifactPath()), expected);

    private static string ReadAdr() =>
        File.ReadAllText(AdrPath());

    private static void AssertContains(string text, string expected) =>
        Assert.IsTrue(text.Contains(expected, StringComparison.Ordinal), expected);

    private static string DiscoveryPath() =>
        Path.Combine(RepoRoot(), "docs", "reports", "automation-layer-rpa-references-discovery-m446.md");

    private static string AdrPath() =>
        Path.Combine(RepoRoot(), "docs", "architecture", "nodrix-automation-layer-decision-record.md");

    private static string ArtifactPath() =>
        Path.Combine(RepoRoot(), "artifacts", "agent-operations", "m448", "automation-layer-rpa-references-adr-summary.json");

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
