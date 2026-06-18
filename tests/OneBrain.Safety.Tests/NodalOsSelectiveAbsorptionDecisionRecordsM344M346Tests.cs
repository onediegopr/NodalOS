using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("SelectiveAbsorption")]
[TestCategory("AgentWorkboard")]
[TestCategory("AxiomBenchmark")]
[TestCategory("RobomotionRoadmap")]
public sealed class NodalOsSelectiveAbsorptionDecisionRecordsM344M346Tests
{
    private static readonly string RepoRoot = AppDomain.CurrentDomain.BaseDirectory.Contains("bin\\Debug") ||
                                              AppDomain.CurrentDomain.BaseDirectory.Contains("bin\\Release")
        ? Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", ".."))
        : AppDomain.CurrentDomain.BaseDirectory;

    [TestMethod]
    public void DecisionRecords_AndReport_Exist()
    {
        Assert.IsTrue(File.Exists(SourcePath("docs", "architecture", "agent-workboard-decision-record.md")));
        Assert.IsTrue(File.Exists(SourcePath("docs", "architecture", "axiom-browser-automation-benchmark-decision.md")));
        Assert.IsTrue(File.Exists(SourcePath("docs", "architecture", "robomotion-package-skill-worker-roadmap-note.md")));
        Assert.IsTrue(File.Exists(SourcePath("docs", "reports", "selective-absorption-decision-records-m346.md")));
        Assert.IsTrue(File.Exists(SourcePath("artifacts", "agent-operations", "m346", "selective-absorption-decisions-summary.json")));
    }

    [TestMethod]
    public void Artifact_ValidatesSelectiveAbsorptionFlags()
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(SourcePath(
            "artifacts", "agent-operations", "m346", "selective-absorption-decisions-summary.json")));
        var root = doc.RootElement;

        Assert.AreEqual("M344-M346", root.GetProperty("milestone").GetString());
        Assert.AreEqual("SELECTIVE_ABSORPTION_DECISIONS_READY", root.GetProperty("readinessDecision").GetString());
        Assert.IsTrue(root.GetProperty("botBoardDecisionRecordCreated").GetBoolean());
        Assert.IsTrue(root.GetProperty("axiomDecisionRecordCreated").GetBoolean());
        Assert.IsTrue(root.GetProperty("robomotionRoadmapNoteCreated").GetBoolean());
        Assert.IsTrue(root.GetProperty("coreGoverns").GetBoolean());
        Assert.IsTrue(root.GetProperty("uiShowsDoesNotDecide").GetBoolean());
        Assert.IsTrue(root.GetProperty("policyDecides").GetBoolean());
        Assert.IsTrue(root.GetProperty("evidenceRecords").GetBoolean());
        Assert.IsTrue(root.GetProperty("verificationCloses").GetBoolean());
        Assert.IsTrue(root.GetProperty("humanApprovalForSensitiveActions").GetBoolean());
        Assert.IsTrue(root.GetProperty("noFullWorkboardUiImplemented").GetBoolean());
        Assert.IsTrue(root.GetProperty("noOrchestrationApiImplemented").GetBoolean());
        Assert.IsTrue(root.GetProperty("noScheduledRunsImplemented").GetBoolean());
        Assert.IsTrue(root.GetProperty("noPackageRegistryImplemented").GetBoolean());
        Assert.IsTrue(root.GetProperty("noMultiWorkerRuntimeImplemented").GetBoolean());
        Assert.IsTrue(root.GetProperty("noCloudRuntimeImplemented").GetBoolean());
        Assert.IsTrue(root.GetProperty("noCaptchaSolving").GetBoolean());
        Assert.IsTrue(root.GetProperty("noBotBypassing").GetBoolean());
        Assert.IsTrue(root.GetProperty("noLocalAi").GetBoolean());
        Assert.AreEqual("M347-M349 Mission / Task Domain Model", root.GetProperty("nextRecommendedMilestone").GetString());
    }

    [TestMethod]
    public void Report_AndDecisionRecords_StateGovernedNonImplementationScope()
    {
        var workboard = File.ReadAllText(SourcePath("docs", "architecture", "agent-workboard-decision-record.md"));
        var axiom = File.ReadAllText(SourcePath("docs", "architecture", "axiom-browser-automation-benchmark-decision.md"));
        var robomotion = File.ReadAllText(SourcePath("docs", "architecture", "robomotion-package-skill-worker-roadmap-note.md"));
        var report = File.ReadAllText(SourcePath("docs", "reports", "selective-absorption-decision-records-m346.md"));

        StringAssert.Contains(workboard, "BotBoard is treated as a conceptual benchmark only.");
        StringAssert.Contains(workboard, "No task should be marked done without verification evidence");
        StringAssert.Contains(axiom, "Axiom is a benchmark");
        StringAssert.Contains(axiom, "recipe manifest cannot bypass policy");
        StringAssert.Contains(robomotion, "roadmap intent only");
        StringAssert.Contains(report, "no orchestration API");
        StringAssert.Contains(report, "M347-M349 Mission / Task Domain Model");
    }

    private static string SourcePath(params string[] parts) => Path.Combine(new[] { RepoRoot }.Concat(parts).ToArray());
}
