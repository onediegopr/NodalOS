using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("SelectiveAbsorptionClosure")]
[TestCategory("RoadmapResync")]
[TestCategory("SelectiveAbsorption")]
[TestCategory("RecipeManifest")]
[TestCategory("RunReport")]
[TestCategory("FailureTaxonomy")]
[TestCategory("MissionTaskDomain")]
[TestCategory("AgentWorkboard")]
public sealed class NodalOsSelectiveAbsorptionClosureM356M358Tests
{
    private static readonly string RepoRoot = AppDomain.CurrentDomain.BaseDirectory.Contains("bin\\Debug") ||
                                              AppDomain.CurrentDomain.BaseDirectory.Contains("bin\\Release")
        ? Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", ".."))
        : AppDomain.CurrentDomain.BaseDirectory;

    [TestMethod]
    public void ClosureReportExists()
    {
        Assert.IsTrue(File.Exists(SourcePath("docs", "reports", "selective-absorption-final-closure-m358.md")));
    }

    [TestMethod]
    public void RoadmapResyncReportExists()
    {
        Assert.IsTrue(File.Exists(SourcePath("docs", "reports", "nodal-os-roadmap-resync-post-selective-absorption-m358.md")));
    }

    [TestMethod]
    public void ArtifactExists()
    {
        Assert.IsTrue(File.Exists(SourcePath("artifacts", "agent-operations", "m358", "selective-absorption-roadmap-resync-summary.json")));
    }

    [TestMethod]
    public void Artifact_MarksImmediateScopeClosed()
    {
        using var doc = LoadArtifact();
        var root = doc.RootElement;

        Assert.AreEqual("M356-M358", root.GetProperty("milestone").GetString());
        Assert.AreEqual("SELECTIVE_ABSORPTION_CLOSED_READY_FOR_HYBRID_PRIORITY_ROADMAP", root.GetProperty("readinessDecision").GetString());
        Assert.IsTrue(root.GetProperty("selectiveAbsorptionImmediateScopeClosed").GetBoolean());
        Assert.AreEqual("decision_and_domain_model_ready", root.GetProperty("botBoardAbsorptionStatus").GetString());
        Assert.AreEqual("run_report_failure_taxonomy_recipe_manifest_ready", root.GetProperty("axiomAbsorptionStatus").GetString());
        Assert.AreEqual("roadmap_note_only", root.GetProperty("robomotionAbsorptionStatus").GetString());
        Assert.IsTrue(root.GetProperty("ocrLineClosed").GetBoolean());
    }

    [TestMethod]
    public void Artifact_ConfirmsDeferredRuntimeSurfacesFalse()
    {
        using var doc = LoadArtifact();
        var root = doc.RootElement;

        Assert.IsFalse(root.GetProperty("recipeExecutionImplemented").GetBoolean());
        Assert.IsFalse(root.GetProperty("workboardUiImplemented").GetBoolean());
        Assert.IsFalse(root.GetProperty("orchestrationApiImplemented").GetBoolean());
        Assert.IsFalse(root.GetProperty("scheduledRunsImplemented").GetBoolean());
        Assert.IsFalse(root.GetProperty("packageRegistryImplemented").GetBoolean());
        Assert.IsFalse(root.GetProperty("cloudRuntimeImplemented").GetBoolean());
        Assert.IsFalse(root.GetProperty("captchaSolvingImplemented").GetBoolean());
        Assert.IsFalse(root.GetProperty("botBypassingImplemented").GetBoolean());
    }

    [TestMethod]
    public void Artifact_IncludesNextMilestoneAndPercentages()
    {
        using var doc = LoadArtifact();
        var root = doc.RootElement;

        Assert.AreEqual("Hybrid priority roadmap", root.GetProperty("recommendedNextPath").GetString());
        Assert.AreEqual("M359-M361 Browser Runtime Flake Hardening", root.GetProperty("recommendedNextMilestone").GetString());
        Assert.AreEqual(68, root.GetProperty("agentOperationsPlatformPercent").GetInt32());
        Assert.AreEqual(85, root.GetProperty("nodalOsGlobalPercent").GetInt32());
    }

    [TestMethod]
    public void ReportsUseCurrentProjectName()
    {
        var closure = File.ReadAllText(SourcePath("docs", "reports", "selective-absorption-final-closure-m358.md"));
        var resync = File.ReadAllText(SourcePath("docs", "reports", "nodal-os-roadmap-resync-post-selective-absorption-m358.md"));

        StringAssert.Contains(closure, "NODAL OS");
        StringAssert.Contains(resync, "NODAL OS");
        Assert.IsFalse(closure.Contains("NEXA", StringComparison.Ordinal));
        Assert.IsFalse(resync.Contains("NEXA", StringComparison.Ordinal));
    }

    [TestMethod]
    public void RoadmapVNext_ContainsPostSelectiveAbsorptionResync()
    {
        var roadmap = File.ReadAllText(SourcePath("docs", "roadmap", "nodal-os-roadmap-vnext.md"));

        StringAssert.Contains(roadmap, "Post-Selective Absorption Re-Sync M358");
        StringAssert.Contains(roadmap, "Hybrid priority roadmap");
        StringAssert.Contains(roadmap, "M359-M361 Browser Runtime Flake Hardening");
    }

    private static JsonDocument LoadArtifact() =>
        JsonDocument.Parse(File.ReadAllText(SourcePath(
            "artifacts",
            "agent-operations",
            "m358",
            "selective-absorption-roadmap-resync-summary.json")));

    private static string SourcePath(params string[] parts) => Path.Combine(new[] { RepoRoot }.Concat(parts).ToArray());
}
