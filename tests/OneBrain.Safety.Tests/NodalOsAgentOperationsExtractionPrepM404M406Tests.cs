using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("AgentOperationsExtractionPrep")]
[TestCategory("PackageRegistryWorkerIntegration")]
[TestCategory("WorkerBoundary")]
[TestCategory("InternalSkillRegistry")]
[TestCategory("PackageSkillManifest")]
public sealed class NodalOsAgentOperationsExtractionPrepM404M406Tests
{
    [TestMethod]
    public void ExtractionDiscoveryReportExists()
    {
        Assert.IsTrue(File.Exists(DiscoveryReportPath()));
    }

    [TestMethod]
    public void ExtractionPlanExists()
    {
        Assert.IsTrue(File.Exists(ExtractionPlanPath()));
    }

    [TestMethod]
    public void ArtifactExists()
    {
        Assert.IsTrue(File.Exists(ArtifactPath()));
    }

    [TestMethod]
    public void ArtifactMarksNoNamespaceMove()
    {
        StringAssert.Contains(ReadArtifact(), "\"noNamespaceMoveImplemented\": true");
    }

    [TestMethod]
    public void ArtifactMarksNoBroadRename()
    {
        StringAssert.Contains(ReadArtifact(), "\"noBroadRenameImplemented\": true");
    }

    [TestMethod]
    public void ArtifactMarksNoRuntimeBehaviorChange()
    {
        StringAssert.Contains(ReadArtifact(), "\"noRuntimeBehaviorChange\": true");
    }

    [TestMethod]
    public void ArtifactDefinesTargetContractsBoundary()
    {
        StringAssert.Contains(ReadArtifact(), "\"targetContractsBoundaryDefined\": true");
    }

    [TestMethod]
    public void ArtifactDefinesTargetCoreBoundary()
    {
        StringAssert.Contains(ReadArtifact(), "\"targetCoreBoundaryDefined\": true");
    }

    [TestMethod]
    public void ArtifactDefinesTargetBrowserAdapterBoundary()
    {
        StringAssert.Contains(ReadArtifact(), "\"targetBrowserAdapterBoundaryDefined\": true");
    }

    [TestMethod]
    public void ArtifactDocumentsCompatibilityShims()
    {
        StringAssert.Contains(ReadArtifact(), "\"compatibilityShimsRequired\": true");
    }

    [TestMethod]
    public void ExtractionPlanMentionsOneBrainAgentOperationsContracts()
    {
        StringAssert.Contains(ReadExtractionPlan(), "OneBrain.AgentOperations.Contracts");
    }

    [TestMethod]
    public void ExtractionPlanMentionsOneBrainAgentOperationsCore()
    {
        StringAssert.Contains(ReadExtractionPlan(), "OneBrain.AgentOperations.Core");
    }

    [TestMethod]
    public void ExtractionPlanMentionsBrowserAdapterBoundary()
    {
        StringAssert.Contains(ReadExtractionPlan(), "OneBrain.AgentOperations.Adapters.Browser");
    }

    [TestMethod]
    public void ReportsUseNodalOsName_NotNexaProjectName()
    {
        var discovery = File.ReadAllText(DiscoveryReportPath());
        var plan = ReadExtractionPlan();

        StringAssert.Contains(discovery, "Project: NODAL OS");
        StringAssert.Contains(plan, "Project: NODAL OS");
        Assert.IsFalse(discovery.Contains("Project: NEXA", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(plan.Contains("Project: NEXA", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void RoadmapUpdatedWithM404M406()
    {
        var roadmap = File.ReadAllText(Path.Combine(FindRepoRoot(), "docs", "roadmap", "nodal-os-roadmap-vnext.md"));

        StringAssert.Contains(roadmap, "Agent Operations Extraction Prep M406");
        StringAssert.Contains(roadmap, "M407-M409 Agent Operations Extraction Phase 1 Contracts");
    }

    private static string ReadArtifact() =>
        File.ReadAllText(ArtifactPath());

    private static string ReadExtractionPlan() =>
        File.ReadAllText(ExtractionPlanPath());

    private static string DiscoveryReportPath() =>
        Path.Combine(FindRepoRoot(), "docs", "reports", "agent-operations-extraction-discovery-m404.md");

    private static string ExtractionPlanPath() =>
        Path.Combine(FindRepoRoot(), "docs", "architecture", "agent-operations-extraction-prep-plan.md");

    private static string ArtifactPath() =>
        Path.Combine(FindRepoRoot(), "artifacts", "agent-operations", "m406", "agent-operations-extraction-prep-summary.json");

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "OneBrain.slnx")))
                return directory.FullName;

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate repository root.");
    }
}
