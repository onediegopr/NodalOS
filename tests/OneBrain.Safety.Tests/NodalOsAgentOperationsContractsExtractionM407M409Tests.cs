using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("AgentOperationsContractsExtraction")]
[TestCategory("PackageRegistryWorkerIntegration")]
[TestCategory("WorkerBoundary")]
[TestCategory("InternalSkillRegistry")]
[TestCategory("PackageSkillManifest")]
[TestCategory("VerificationBeforeDone")]
[TestCategory("RunReport")]
[TestCategory("MissionTaskDomain")]
public sealed class NodalOsAgentOperationsContractsExtractionM407M409Tests
{
    private static readonly string[] MovedContracts =
    [
        "NodalOsAgentWorkboardContracts.cs",
        "NodalOsFailureTaxonomyContracts.cs",
        "NodalOsRunReportContracts.cs",
        "NodalOsRecipeManifestContracts.cs",
        "NodalOsVerificationBeforeDoneContracts.cs",
        "NodalOsAgentProgressReportingContracts.cs",
        "NodalOsStepLibraryContracts.cs",
        "NodalOsRedactionContracts.cs",
        "NodalOsEvidenceRefBridgeContracts.cs",
        "NodalOsPackageSkillManifestContracts.cs",
        "NodalOsInternalSkillRegistryContracts.cs",
        "NodalOsWorkerBoundaryContracts.cs"
    ];

    [TestMethod]
    public void AgentOperationsContractsProjectExists()
    {
        Assert.IsTrue(File.Exists(AgentOperationsContractsProjectPath()));
    }

    [TestMethod]
    public void AgentOperationsContractsProjectDoesNotReferenceBrowserExecutorCdp()
    {
        var project = File.ReadAllText(AgentOperationsContractsProjectPath());

        Assert.IsFalse(project.Contains("OneBrain.BrowserExecutor.Cdp", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(project.Contains("BrowserExecutor.Cdp", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void AgentOperationsContractsProjectDoesNotReferenceChromeCdp()
    {
        var project = File.ReadAllText(AgentOperationsContractsProjectPath());

        Assert.IsFalse(project.Contains("ChromeCdp", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(project.Contains("Cdp", StringComparison.Ordinal));
    }

    [TestMethod]
    public void BrowserExecutorCdpReferencesAgentOperationsContracts()
    {
        var project = File.ReadAllText(Path.Combine(FindRepoRoot(), "src", "OneBrain.BrowserExecutor.Cdp", "OneBrain.BrowserExecutor.Cdp.csproj"));

        StringAssert.Contains(project, "OneBrain.AgentOperations.Contracts");
    }

    [TestMethod]
    public void SafetyTestsCanUseAgentOperationsContracts()
    {
        var project = File.ReadAllText(Path.Combine(FindRepoRoot(), "tests", "OneBrain.Safety.Tests", "OneBrain.Safety.Tests.csproj"));

        StringAssert.Contains(project, "OneBrain.AgentOperations.Contracts");
    }

    [TestMethod]
    public void MovedContractsCompile()
    {
        foreach (var contract in MovedContracts)
        {
            Assert.IsTrue(File.Exists(Path.Combine(AgentOperationsContractsDirectory(), contract)), contract);
            Assert.IsFalse(File.Exists(Path.Combine(BrowserExecutorContractsDirectory(), contract)), contract);
        }
    }

    [TestMethod]
    public void AgentWorkboardContractStillUsable()
    {
        var mission = new NexaMission
        {
            MissionId = "mission-m407",
            Title = "Contracts extraction",
            Status = NexaMissionStatus.Ready,
            HumanOwner = "operator",
            CreatedAt = DateTimeOffset.UnixEpoch,
            UpdatedAt = DateTimeOffset.UnixEpoch
        };

        Assert.AreEqual("mission-m407", mission.MissionId);
        Assert.AreEqual(NexaMissionStatus.Ready, mission.Status);
    }

    [TestMethod]
    public void RunReportContractStillUsable()
    {
        var report = new NexaRunReport
        {
            RunId = "run-m407",
            Goal = "Validate contract extraction",
            Status = NexaRunStatus.Completed,
            StartedAt = DateTimeOffset.UnixEpoch,
            CompletedAt = DateTimeOffset.UnixEpoch,
            Steps = []
        };

        Assert.AreEqual(NexaRunStatus.Completed, report.Status);
        Assert.AreEqual(0, report.Steps.Count);
    }

    [TestMethod]
    public void RecipeManifestContractStillUsable()
    {
        var manifest = new NodalOsRecipeManifest
        {
            RecipeId = "recipe-m407",
            Name = "Contract only recipe",
            Version = "1.0.0",
            Status = NodalOsRecipeStatus.Draft,
            GoalTemplate = "Validate contract boundary",
            Steps = [],
            Policy = new NodalOsRecipePolicyManifest(),
            CreatedAt = DateTimeOffset.UnixEpoch,
            UpdatedAt = DateTimeOffset.UnixEpoch
        };

        Assert.AreEqual("recipe-m407", manifest.RecipeId);
        Assert.AreEqual(NodalOsRecipeStatus.Draft, manifest.Status);
    }

    [TestMethod]
    public void StepLibraryContractStillUsable()
    {
        var step = new NodalOsStepDefinition
        {
            StepKind = NodalOsStepKind.Read,
            Name = "Read",
            Description = "Read-only catalog step",
            RiskLevel = NodalOsStepRiskLevel.Low,
            Capabilities = [NodalOsStepCapabilityKind.ReadOnly],
            IsReadOnlyCapable = true,
            RequiresApprovalByDefault = false,
            IsSensitiveByDefault = false,
            IsCatalogAvailableInV1 = true,
            IsAllowedInV1 = true
        };

        Assert.IsTrue(step.IsReadOnlyCapable);
        Assert.IsTrue(step.RuntimeExecutionDeferred);
    }

    [TestMethod]
    public void PackageSkillManifestContractStillUsable()
    {
        var package = new NodalOsPackageManifest
        {
            PackageId = "package-m407",
            Name = "Contract package",
            Version = "1.0.0",
            Status = NodalOsPackageStatus.Draft,
            Publisher = "NODAL OS",
            Provenance = "test",
            InternalOnly = true,
            RuntimeExecutionAllowed = false,
            RuntimeExecutionDeferred = true,
            RequiresGlobalPolicyEvaluation = true,
            CreatedAt = DateTimeOffset.UnixEpoch,
            UpdatedAt = DateTimeOffset.UnixEpoch
        };

        Assert.IsFalse(package.RuntimeExecutionAllowed);
        Assert.IsTrue(package.RuntimeExecutionDeferred);
    }

    [TestMethod]
    public void InternalSkillRegistryContractStillUsable()
    {
        var snapshot = new NodalOsInternalSkillRegistrySnapshot
        {
            RegistryId = "registry-m407",
            Version = "1.0.0",
            Entries = [],
            CreatedAt = DateTimeOffset.UnixEpoch
        };

        Assert.AreEqual("registry-m407", snapshot.RegistryId);
        Assert.AreEqual(0, snapshot.Entries.Count);
    }

    [TestMethod]
    public void WorkerBoundaryContractStillUsable()
    {
        var worker = new NodalOsWorkerBoundaryManifest
        {
            WorkerId = "worker-m407",
            Name = "Contract worker",
            Version = "1.0.0",
            Status = NodalOsWorkerStatus.Draft,
            BoundaryKind = NodalOsWorkerBoundaryKind.InProcessAdapter,
            Provenance = "test",
            InternalOnly = true,
            RuntimeExecutionAllowed = false,
            RuntimeExecutionDeferred = true,
            RequiresGlobalPolicyEvaluation = true,
            CanAuthorizeActions = false,
            CreatedAt = DateTimeOffset.UnixEpoch,
            UpdatedAt = DateTimeOffset.UnixEpoch
        };

        Assert.IsFalse(worker.RuntimeExecutionAllowed);
        Assert.IsFalse(worker.CanAuthorizeActions);
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
        File.ReadAllText(Path.Combine(FindRepoRoot(), "artifacts", "agent-operations", "m409", "agent-operations-contracts-extraction-summary.json"));

    private static string AgentOperationsContractsProjectPath() =>
        Path.Combine(AgentOperationsContractsDirectory(), "OneBrain.AgentOperations.Contracts.csproj");

    private static string AgentOperationsContractsDirectory() =>
        Path.Combine(FindRepoRoot(), "src", "OneBrain.AgentOperations.Contracts");

    private static string BrowserExecutorContractsDirectory() =>
        Path.Combine(FindRepoRoot(), "src", "OneBrain.BrowserExecutor.Contracts");

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
