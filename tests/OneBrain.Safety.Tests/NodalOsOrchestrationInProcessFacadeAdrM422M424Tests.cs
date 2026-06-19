using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("OrchestrationInProcessFacadeAdr")]
[TestCategory("OrchestrationApiDecisionRecord")]
public sealed class NodalOsOrchestrationInProcessFacadeAdrM422M424Tests
{
    [TestMethod]
    public void FacadeBoundaryDiscoveryReportExists()
    {
        Assert.IsTrue(File.Exists(DiscoveryReportPath()));
    }

    [TestMethod]
    public void FacadeAdrExists()
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
    public void ArtifactMarksNoFacadeImplemented()
    {
        StringAssert.Contains(ReadArtifact(), "\"noFacadeImplemented\": true");
    }

    [TestMethod]
    public void ArtifactMarksNoDispatcherImplemented()
    {
        StringAssert.Contains(ReadArtifact(), "\"noDispatcherImplemented\": true");
    }

    [TestMethod]
    public void ArtifactMarksNoRuntimeEngine()
    {
        StringAssert.Contains(ReadArtifact(), "\"noRuntimeEngineImplemented\": true");
    }

    [TestMethod]
    public void ArtifactMarksNoApiHttpScheduler()
    {
        var artifact = ReadArtifact();

        StringAssert.Contains(artifact, "\"noApiImplemented\": true");
        StringAssert.Contains(artifact, "\"noHttpImplemented\": true");
        StringAssert.Contains(artifact, "\"noSchedulerImplemented\": true");
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
    public void ArtifactDefinesNoExecutionInvariant()
    {
        StringAssert.Contains(ReadArtifact(), "\"noExecutionInvariantDefined\": true");
    }

    [TestMethod]
    public void ArtifactDefinesPolicyGateLocation()
    {
        StringAssert.Contains(ReadArtifact(), "\"policyGateLocationDefined\": true");
    }

    [TestMethod]
    public void ArtifactDefinesApprovalGateLocation()
    {
        StringAssert.Contains(ReadArtifact(), "\"approvalGateLocationDefined\": true");
    }

    [TestMethod]
    public void ArtifactDefinesEvidenceGateLocation()
    {
        StringAssert.Contains(ReadArtifact(), "\"evidenceGateLocationDefined\": true");
    }

    [TestMethod]
    public void ArtifactPreservesVerificationBeforeDoneGate()
    {
        StringAssert.Contains(ReadArtifact(), "\"verificationBeforeDoneGatePreserved\": true");
    }

    [TestMethod]
    public void ArtifactSaysAcceptedDoesNotMeanExecuted()
    {
        StringAssert.Contains(ReadArtifact(), "\"acceptedDoesNotMeanExecuted\": true");
    }

    [TestMethod]
    public void ArtifactSaysCompletedMeansContractHandlingOnly()
    {
        StringAssert.Contains(ReadArtifact(), "\"completedMeansContractHandlingOnly\": true");
    }

    [TestMethod]
    public void ArtifactSaysRunningFutureStillFutureOnly()
    {
        StringAssert.Contains(ReadArtifact(), "\"runningFutureStillFutureOnly\": true");
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
        Path.Combine(FindRepoRoot(), "docs", "reports", "orchestration-in-process-facade-boundary-discovery-m422.md");

    private static string AdrPath() =>
        Path.Combine(FindRepoRoot(), "docs", "architecture", "orchestration-in-process-facade-decision-record.md");

    private static string ArtifactPath() =>
        Path.Combine(FindRepoRoot(), "artifacts", "agent-operations", "m424", "orchestration-in-process-facade-adr-summary.json");

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
