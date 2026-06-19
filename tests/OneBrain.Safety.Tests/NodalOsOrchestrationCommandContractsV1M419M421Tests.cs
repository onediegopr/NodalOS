using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("OrchestrationCommandContracts")]
[TestCategory("OrchestrationApiDecisionRecord")]
[TestCategory("EvidenceRefLedgerBridge")]
[TestCategory("PackageRegistryWorkerIntegration")]
[TestCategory("WorkerBoundary")]
public sealed class NodalOsOrchestrationCommandContractsV1M419M421Tests
{
    [TestMethod]
    public void CommandEnvelope_RuntimeExecutionAllowed_IsFalse()
    {
        var command = NodalOsOrchestrationCommandFixtures.PrepareRunCommand();

        Assert.IsFalse(command.RuntimeExecutionAllowed);
        Assert.IsTrue(Validator().ValidateCommand(command).IsValid);
    }

    [TestMethod]
    public void CommandEnvelope_RuntimeExecutionDeferred_IsTrue()
    {
        var command = NodalOsOrchestrationCommandFixtures.PrepareRunCommand();

        Assert.IsTrue(command.RuntimeExecutionDeferred);
    }

    [TestMethod]
    public void CommandEnvelope_RequiresGlobalPolicyEvaluation()
    {
        var command = NodalOsOrchestrationCommandFixtures.PrepareRunCommand();

        Assert.IsTrue(command.RequiresGlobalPolicyEvaluation);
    }

    [TestMethod]
    public void HighRiskCommand_RequiresHumanApproval()
    {
        var command = NodalOsOrchestrationCommandFixtures.PrepareRunCommand() with
        {
            RiskLevel = NodalOsOrchestrationCommandRiskLevel.High,
            RequiresHumanApproval = false
        };

        Assert.IsFalse(Validator().ValidateCommand(command).IsValid);
    }

    [TestMethod]
    public void CriticalRiskCommand_RequiresHumanApproval()
    {
        var command = NodalOsOrchestrationCommandFixtures.PrepareRunCommand() with
        {
            RiskLevel = NodalOsOrchestrationCommandRiskLevel.Critical,
            RequiresHumanApproval = false
        };

        Assert.IsFalse(Validator().ValidateCommand(command).IsValid);
    }

    [TestMethod]
    public void AttachEvidenceCommand_RequiresEvidenceRefs()
    {
        var command = NodalOsOrchestrationCommandFixtures.AttachEvidenceCommand() with
        {
            EvidenceRefs = []
        };

        Assert.IsFalse(Validator().ValidateCommand(command).IsValid);
    }

    [TestMethod]
    public void PrepareWorkerRequest_RequiresWorkerAndSkillOrPackage()
    {
        var command = NodalOsOrchestrationCommandFixtures.PrepareWorkerRequestCommand() with
        {
            WorkerId = null,
            SkillId = null,
            PackageId = null
        };

        Assert.IsFalse(Validator().ValidateCommand(command).IsValid);
    }

    [TestMethod]
    public void PrepareRun_RequiresMissionOrTaskAndRecipeOrSkill()
    {
        var command = NodalOsOrchestrationCommandFixtures.PrepareRunCommand() with
        {
            MissionId = null,
            TaskId = null,
            RecipeId = null,
            SkillId = null
        };

        Assert.IsFalse(Validator().ValidateCommand(command).IsValid);
    }

    [TestMethod]
    public void QuerySkillRegistry_DoesNotGrantRuntimePermission()
    {
        var command = NodalOsOrchestrationCommandFixtures.QuerySkillRegistryCommand();
        var result = Validator().ValidateCommand(command);

        Assert.IsTrue(result.IsValid);
        Assert.IsFalse(result.RuntimeExecutionAllowed);
        Assert.IsTrue(result.Warnings.Any(warning => warning.Contains("catalog lookup", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    public void PauseResumeCancel_AreContractOnly()
    {
        var pause = NodalOsOrchestrationCommandFixtures.CancelRunCommand() with
        {
            Kind = NodalOsOrchestrationCommandKind.PauseRun
        };
        var resume = pause with { Kind = NodalOsOrchestrationCommandKind.ResumeRun };
        var cancel = NodalOsOrchestrationCommandFixtures.CancelRunCommand();

        AssertContractOnlyWarning(pause);
        AssertContractOnlyWarning(resume);
        AssertContractOnlyWarning(cancel);
    }

    [TestMethod]
    public void CommandEvidenceRefs_ValidateViaBridge()
    {
        var command = NodalOsOrchestrationCommandFixtures.AttachEvidenceCommand();

        Assert.IsTrue(Validator().ValidateCommand(command).IsValid);
    }

    [TestMethod]
    public void CommandWithInvalidEvidenceRef_Fails()
    {
        var command = NodalOsOrchestrationCommandFixtures.AttachEvidenceCommand() with
        {
            EvidenceRefs =
            [
                NodalOsOrchestrationCommandFixtures.ValidEvidenceRef() with
                {
                    EvidenceId = string.Empty
                }
            ]
        };

        Assert.IsFalse(Validator().ValidateCommand(command).IsValid);
    }

    [TestMethod]
    public void CommandSummarySecret_IsRejectedOrRedacted()
    {
        var command = NodalOsOrchestrationCommandFixtures.CreateMissionCommand() with
        {
            Summary = "Authorization: Bearer fake-token-value-123456"
        };
        var validation = Validator().ValidateCommand(command);
        var json = Serializer().SerializeCommand(command);

        Assert.IsFalse(validation.IsValid);
        StringAssert.Contains(json, "[REDACTED]");
        Assert.IsFalse(json.Contains("fake-token-value-123456", StringComparison.Ordinal));
    }

    [TestMethod]
    public void CommandResult_ExecutedMustBeFalseInV1()
    {
        var result = NodalOsOrchestrationCommandFixtures.FutureRunningResultInvalidForExecution();

        Assert.IsFalse(Validator().ValidateResult(result).IsValid);
    }

    [TestMethod]
    public void CommandResult_RuntimeExecutionDeferred_IsTrue()
    {
        var result = NodalOsOrchestrationCommandFixtures.CompletedContractResult();

        Assert.IsTrue(result.RuntimeExecutionDeferred);
    }

    [TestMethod]
    public void CommandResult_AcceptedDoesNotMeanExecuted()
    {
        var result = NodalOsOrchestrationCommandFixtures.CompletedContractResult();
        var validation = Validator().ValidateResult(result);

        Assert.IsTrue(result.Accepted);
        Assert.IsFalse(result.Executed);
        Assert.IsTrue(validation.Warnings.Any(warning => warning.Contains("does not mean executed", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    public void CommandResult_CompletedMeansContractHandlingOnly()
    {
        var result = NodalOsOrchestrationCommandFixtures.CompletedContractResult();
        var validation = Validator().ValidateResult(result);

        Assert.AreEqual(NodalOsOrchestrationState.Completed, result.State);
        Assert.IsTrue(validation.Warnings.Any(warning => warning.Contains("contract handling completed only", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    public void CommandResult_EvidenceRefs_ValidateViaBridge()
    {
        var result = NodalOsOrchestrationCommandFixtures.CompletedContractResult();

        Assert.IsTrue(Validator().ValidateResult(result).IsValid);
    }

    [TestMethod]
    public void CommandAndResult_SerializeDeserialize()
    {
        var serializer = Serializer();
        var command = NodalOsOrchestrationCommandFixtures.PrepareRunCommand();
        var result = NodalOsOrchestrationCommandFixtures.CompletedContractResult();

        var commandRoundTrip = serializer.DeserializeCommand(serializer.SerializeCommand(command));
        var resultRoundTrip = serializer.DeserializeResult(serializer.SerializeResult(result));

        Assert.AreEqual(command.CommandId, commandRoundTrip.CommandId);
        Assert.AreEqual(result.ResultId, resultRoundTrip.ResultId);
    }

    [TestMethod]
    public void NoApiOrHttpImplemented()
    {
        AssertNoFileNameContains("OrchestrationApi");
        AssertNoFileNameContains("OrchestrationController");
        AssertNoFileNameContains("OrchestrationHttp");
    }

    [TestMethod]
    public void NoSchedulerImplemented()
    {
        AssertNoFileNameContains("OrchestrationScheduler");
    }

    [TestMethod]
    public void NoWorkerRuntimeImplemented()
    {
        var artifact = ReadArtifact();

        StringAssert.Contains(artifact, "\"noWorkerRuntimeImplemented\": true");
    }

    [TestMethod]
    public void NoRecipeSkillStepExecutionImplemented()
    {
        var artifact = ReadArtifact();

        StringAssert.Contains(artifact, "\"noRecipeExecutionImplemented\": true");
        StringAssert.Contains(artifact, "\"noSkillExecutionImplemented\": true");
        StringAssert.Contains(artifact, "\"noStepExecutionImplemented\": true");
    }

    [TestMethod]
    public void NoUiImplemented()
    {
        StringAssert.Contains(ReadArtifact(), "\"noUiImplemented\": true");
    }

    [TestMethod]
    public void NewTypesUseNodalOsPrefix()
    {
        var contracts = File.ReadAllText(Path.Combine(FindRepoRoot(), "src", "OneBrain.AgentOperations.Contracts", "NodalOsOrchestrationCommandContracts.cs"));
        var services = File.ReadAllText(Path.Combine(FindRepoRoot(), "src", "OneBrain.AgentOperations.Core", "NodalOsOrchestrationCommandServices.cs"));

        StringAssert.Contains(contracts, "NodalOsOrchestrationCommandEnvelope");
        StringAssert.Contains(services, "NodalOsOrchestrationCommandValidator");
        Assert.IsFalse(contracts.Contains("public sealed record Nexa", StringComparison.Ordinal));
        Assert.IsFalse(services.Contains("public sealed class Nexa", StringComparison.Ordinal));
    }

    private static NodalOsOrchestrationCommandValidator Validator() => new();

    private static NodalOsOrchestrationCommandJsonSerializer Serializer() => new();

    private static void AssertContractOnlyWarning(NodalOsOrchestrationCommandEnvelope command)
    {
        var validation = Validator().ValidateCommand(command);

        Assert.IsTrue(validation.IsValid);
        Assert.IsTrue(validation.Warnings.Any(warning => warning.Contains("contract-only", StringComparison.OrdinalIgnoreCase)));
    }

    private static void AssertNoFileNameContains(string pattern)
    {
        var files = Directory.EnumerateFiles(Path.Combine(FindRepoRoot(), "src"), "*", SearchOption.AllDirectories)
            .Where(path => path.Contains(pattern, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        Assert.AreEqual(0, files.Length, string.Join(Environment.NewLine, files));
    }

    private static string ReadArtifact() =>
        File.ReadAllText(Path.Combine(
            FindRepoRoot(),
            "artifacts",
            "agent-operations",
            "m421",
            "orchestration-command-contracts-v1-summary.json"));

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
