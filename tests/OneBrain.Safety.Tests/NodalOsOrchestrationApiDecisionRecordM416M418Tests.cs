using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("OrchestrationApiDecisionRecord")]
[TestCategory("BrowserAdapterBoundary")]
[TestCategory("AgentOperationsCoreServicesExtraction")]
[TestCategory("PackageRegistryWorkerIntegration")]
public sealed class NodalOsOrchestrationApiDecisionRecordM416M418Tests
{
    [TestMethod]
    public void OrchestrationBoundaryDiscoveryReportExists()
    {
        Assert.IsTrue(File.Exists(DiscoveryReportPath()));
    }

    [TestMethod]
    public void OrchestrationApiAdrExists()
    {
        Assert.IsTrue(File.Exists(AdrPath()));
    }

    [TestMethod]
    public void ArtifactExists()
    {
        Assert.IsTrue(File.Exists(ArtifactPath()));
    }

    [TestMethod]
    public void ArtifactMarksImplementationDeferred()
    {
        StringAssert.Contains(ReadArtifact(), "\"implementationDeferred\": true");
    }

    [TestMethod]
    public void ArtifactMarksNoApiImplemented()
    {
        StringAssert.Contains(ReadArtifact(), "\"noApiImplemented\": true");
    }

    [TestMethod]
    public void ArtifactMarksNoHttpImplemented()
    {
        StringAssert.Contains(ReadArtifact(), "\"noHttpImplemented\": true");
    }

    [TestMethod]
    public void ArtifactMarksNoSchedulerImplemented()
    {
        StringAssert.Contains(ReadArtifact(), "\"noSchedulerImplemented\": true");
    }

    [TestMethod]
    public void ArtifactMarksNoWorkerRuntime()
    {
        StringAssert.Contains(ReadArtifact(), "\"noWorkerRuntimeImplemented\": true");
    }

    [TestMethod]
    public void ArtifactMarksNoRecipeSkillStepExecution()
    {
        var artifact = ReadArtifact();

        StringAssert.Contains(artifact, "\"noRecipeExecutionImplemented\": true");
        StringAssert.Contains(artifact, "\"noSkillExecutionImplemented\": true");
        StringAssert.Contains(artifact, "\"noStepExecutionImplemented\": true");
    }

    [TestMethod]
    public void ArtifactMarksNoUi()
    {
        StringAssert.Contains(ReadArtifact(), "\"noUiImplemented\": true");
    }

    [TestMethod]
    public void ArtifactRequiresPolicyGate()
    {
        StringAssert.Contains(ReadArtifact(), "\"policyGateRequired\": true");
    }

    [TestMethod]
    public void ArtifactRequiresApprovalGate()
    {
        StringAssert.Contains(ReadArtifact(), "\"approvalGateRequired\": true");
    }

    [TestMethod]
    public void ArtifactRequiresEvidenceGate()
    {
        StringAssert.Contains(ReadArtifact(), "\"evidenceGateRequired\": true");
    }

    [TestMethod]
    public void ArtifactRequiresVerificationBeforeDone()
    {
        StringAssert.Contains(ReadArtifact(), "\"verificationBeforeDoneRequired\": true");
    }

    [TestMethod]
    public void ArtifactSaysRegistryVisibleDoesNotGrantRuntimePermission()
    {
        StringAssert.Contains(ReadArtifact(), "\"registryVisibleDoesNotGrantRuntimePermission\": true");
    }

    [TestMethod]
    public void ArtifactSaysWorkerHealthyDoesNotGrantRuntimePermission()
    {
        StringAssert.Contains(ReadArtifact(), "\"workerHealthyDoesNotGrantRuntimePermission\": true");
    }

    [TestMethod]
    public void ArtifactSaysRecipeApprovedDoesNotGrantRuntimePermission()
    {
        StringAssert.Contains(ReadArtifact(), "\"recipeApprovedDoesNotGrantRuntimePermission\": true");
    }

    [TestMethod]
    public void ArtifactSaysSkillApprovedDoesNotGrantRuntimePermission()
    {
        StringAssert.Contains(ReadArtifact(), "\"skillApprovedDoesNotGrantRuntimePermission\": true");
    }

    [TestMethod]
    public void AdrDefinesFutureCommandsButNoImplementation()
    {
        var adr = ReadAdr();

        StringAssert.Contains(adr, "CreateMission");
        StringAssert.Contains(adr, "PrepareRun");
        StringAssert.Contains(adr, "RequestHumanDecision");
        StringAssert.Contains(adr, "EvaluateVerificationBeforeDone");
        StringAssert.Contains(adr, "Conceptual only");
        StringAssert.Contains(adr, "No HTTP endpoint");
        StringAssert.Contains(adr, "No execution");
    }

    [TestMethod]
    public void AdrUsesNodalOsName_NotNexaProjectName()
    {
        var adr = ReadAdr();
        var discovery = File.ReadAllText(DiscoveryReportPath());

        StringAssert.Contains(adr, "Project: NODAL OS");
        StringAssert.Contains(discovery, "Project: NODAL OS");
        Assert.IsFalse(adr.Contains("Project: NEXA", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(discovery.Contains("Project: NEXA", StringComparison.OrdinalIgnoreCase));
    }

    private static string ReadArtifact() =>
        File.ReadAllText(ArtifactPath());

    private static string ReadAdr() =>
        File.ReadAllText(AdrPath());

    private static string DiscoveryReportPath() =>
        Path.Combine(FindRepoRoot(), "docs", "reports", "orchestration-api-boundary-discovery-m416.md");

    private static string AdrPath() =>
        Path.Combine(FindRepoRoot(), "docs", "architecture", "orchestration-api-decision-record.md");

    private static string ArtifactPath() =>
        Path.Combine(FindRepoRoot(), "artifacts", "agent-operations", "m418", "orchestration-api-decision-record-summary.json");

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
