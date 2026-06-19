using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("AgentOperationsCoreServicesExtraction")]
[TestCategory("AgentOperationsContractsExtraction")]
[TestCategory("PackageRegistryWorkerIntegration")]
[TestCategory("WorkerBoundary")]
[TestCategory("InternalSkillRegistry")]
[TestCategory("PackageSkillManifest")]
[TestCategory("VerificationBeforeDone")]
[TestCategory("RunReport")]
[TestCategory("MissionTaskDomain")]
public sealed class NodalOsAgentOperationsCoreServicesExtractionM410M412Tests
{
    private static readonly string[] MovedServices =
    [
        "NodalOsAgentWorkboardServices.cs",
        "NodalOsRunReportingServices.cs",
        "NodalOsRecipeManifestServices.cs",
        "NodalOsVerificationBeforeDoneGate.cs",
        "NodalOsAgentProgressReportingServices.cs",
        "NodalOsStepLibraryServices.cs",
        "NodalOsPackageSkillManifestServices.cs",
        "NodalOsInternalSkillRegistryServices.cs",
        "NodalOsWorkerBoundaryServices.cs",
        "NodalOsEvidenceRefBridgeServices.cs",
        "NodalOsRedactionServices.cs"
    ];

    [TestMethod]
    public void AgentOperationsCoreProjectExists()
    {
        Assert.IsTrue(File.Exists(CoreProjectPath()));
    }

    [TestMethod]
    public void AgentOperationsCoreReferencesAgentOperationsContracts()
    {
        StringAssert.Contains(File.ReadAllText(CoreProjectPath()), "OneBrain.AgentOperations.Contracts");
    }

    [TestMethod]
    public void AgentOperationsCoreDoesNotReferenceBrowserExecutorCdp()
    {
        var project = File.ReadAllText(CoreProjectPath());

        Assert.IsFalse(project.Contains("OneBrain.BrowserExecutor.Cdp", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(project.Contains("BrowserExecutor.Cdp", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void AgentOperationsCoreDoesNotReferenceChromeCdp()
    {
        var project = File.ReadAllText(CoreProjectPath());

        Assert.IsFalse(project.Contains("ChromeCdp", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void BrowserExecutorCdpReferencesAgentOperationsCore()
    {
        var project = File.ReadAllText(Path.Combine(FindRepoRoot(), "src", "OneBrain.BrowserExecutor.Cdp", "OneBrain.BrowserExecutor.Cdp.csproj"));

        StringAssert.Contains(project, "OneBrain.AgentOperations.Core");
    }

    [TestMethod]
    public void SafetyTestsReferenceAgentOperationsCore()
    {
        var project = File.ReadAllText(Path.Combine(FindRepoRoot(), "tests", "OneBrain.Safety.Tests", "OneBrain.Safety.Tests.csproj"));

        StringAssert.Contains(project, "OneBrain.AgentOperations.Core");
    }

    [TestMethod]
    public void MovedCoreServicesCompile()
    {
        foreach (var service in MovedServices)
        {
            Assert.IsTrue(File.Exists(Path.Combine(CoreDirectory(), service)), service);
            Assert.IsFalse(File.Exists(Path.Combine(BrowserExecutorCdpDirectory(), service)), service);
        }
    }

    [TestMethod]
    public void WorkboardValidatorStillUsable()
    {
        var result = new NodalOsAgentWorkboardValidator()
            .ValidateTaskCanComplete(NodalOsAgentWorkboardFixtures.CompletedTaskWithEvidenceAndPassedVerification());

        Assert.IsTrue(result.CanComplete);
    }

    [TestMethod]
    public void RunReportBuilderStillUsable()
    {
        var report = new NodalOsRunReportBuilder()
            .CreateSuccessfulRun("run-m410", "Validate core extraction");

        Assert.AreEqual(NexaRunStatus.Completed, report.Status);
    }

    [TestMethod]
    public void RecipeManifestValidatorStillUsable()
    {
        var result = new NodalOsRecipeManifestValidator()
            .Validate(NodalOsRecipeManifestFixtures.ReadOnlyRecipe());

        Assert.IsTrue(result.IsValid);
    }

    [TestMethod]
    public void VerificationBeforeDoneGateStillUsable()
    {
        var result = new NodalOsVerificationBeforeDoneGate()
            .EvaluateTask(NodalOsAgentWorkboardFixtures.CompletedTaskWithEvidenceAndPassedVerification());

        Assert.IsTrue(result.CanMarkDone);
    }

    [TestMethod]
    public void AgentProgressReportValidatorStillUsable()
    {
        var report = new NodalOsAgentProgressReportBuilder().CreateProgress(
            "progress-m410",
            "mission-m410",
            "task-m410",
            "Validate core extraction");

        var result = new NodalOsAgentProgressReportValidator().Validate(report);

        Assert.IsTrue(result.IsValid);
    }

    [TestMethod]
    public void StepLibraryStillUsable()
    {
        var definition = new NodalOsStepLibrary().GetDefinition(NodalOsStepKind.Read);

        Assert.AreEqual(NodalOsStepKind.Read, definition.StepKind);
    }

    [TestMethod]
    public void PackageSkillManifestValidatorStillUsable()
    {
        var result = new NodalOsPackageSkillManifestValidator()
            .ValidatePackage(NodalOsPackageSkillManifestFixtures.InternalReadOnlyPackage());

        Assert.IsTrue(result.IsValid);
    }

    [TestMethod]
    public void InternalSkillRegistryBuilderStillUsable()
    {
        var result = new NodalOsInternalSkillRegistryBuilder()
            .AddPackage(NodalOsPackageSkillManifestFixtures.InternalReadOnlyPackage())
            .BuildValidatedSnapshot("registry-m410", "1.0.0", DateTimeOffset.UnixEpoch);

        Assert.IsTrue(result.Validation.IsValid);
    }

    [TestMethod]
    public void WorkerBoundaryValidatorStillUsable()
    {
        var result = new NodalOsWorkerBoundaryValidator()
            .ValidateManifest(NodalOsWorkerBoundaryFixtures.ValidRegisteredWorker());

        Assert.IsTrue(result.IsValid);
    }

    [TestMethod]
    public void EvidenceRefBridgeStillUsable()
    {
        var result = new NodalOsEvidenceRefBridge().BridgeFromEvidenceRef(
            NodalOsAgentWorkboardFixtures.Evidence("evidence-m410"),
            NodalOsEvidenceBridgeSourceKind.AgentOperation,
            NodalOsEvidenceBridgeUseKind.Auxiliary);

        Assert.IsTrue(result.Accepted);
    }

    [TestMethod]
    public void RedactionServiceStillUsable()
    {
        var result = new NodalOsRedactionService().RedactField("authorization", "Bearer fake-token-value");

        Assert.IsTrue(result.WasRedacted);
    }

    [TestMethod]
    public void NoBrowserRuntimeSmokeMoved()
    {
        Assert.IsTrue(File.Exists(Path.Combine(BrowserExecutorCdpDirectory(), "BrowserRuntimeSmoke.cs")));
        Assert.IsFalse(File.Exists(Path.Combine(CoreDirectory(), "BrowserRuntimeSmoke.cs")));
    }

    [TestMethod]
    public void NoChromeCdpBrowserExecutorMoved()
    {
        Assert.IsTrue(File.Exists(Path.Combine(BrowserExecutorCdpDirectory(), "ChromeCdpBrowserExecutor.cs")));
        Assert.IsFalse(File.Exists(Path.Combine(CoreDirectory(), "ChromeCdpBrowserExecutor.cs")));
    }

    [TestMethod]
    public void NoRuntimeBehaviorChangeArtifactFlag()
    {
        StringAssert.Contains(ReadArtifact(), "\"noRuntimeBehaviorChange\": true");
    }

    [TestMethod]
    public void NoUiOrOrchestrationArtifactFlag()
    {
        var artifact = ReadArtifact();

        StringAssert.Contains(artifact, "\"noUiImplemented\": true");
        StringAssert.Contains(artifact, "\"noOrchestrationApiImplemented\": true");
        StringAssert.Contains(artifact, "\"noExecutionImplemented\": true");
    }

    private static string ReadArtifact() =>
        File.ReadAllText(Path.Combine(FindRepoRoot(), "artifacts", "agent-operations", "m412", "agent-operations-core-services-extraction-summary.json"));

    private static string CoreProjectPath() =>
        Path.Combine(CoreDirectory(), "OneBrain.AgentOperations.Core.csproj");

    private static string CoreDirectory() =>
        Path.Combine(FindRepoRoot(), "src", "OneBrain.AgentOperations.Core");

    private static string BrowserExecutorCdpDirectory() =>
        Path.Combine(FindRepoRoot(), "src", "OneBrain.BrowserExecutor.Cdp");

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
