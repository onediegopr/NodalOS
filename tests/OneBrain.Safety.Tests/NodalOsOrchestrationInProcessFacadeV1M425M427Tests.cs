using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("OrchestrationInProcessFacadeV1")]
[TestCategory("OrchestrationInProcessFacadeAdr")]
[TestCategory("OrchestrationCommandContracts")]
[TestCategory("EvidenceRefLedgerBridge")]
[TestCategory("CommonRedaction")]
public sealed class NodalOsOrchestrationInProcessFacadeV1M425M427Tests
{
    [TestMethod]
    public void Facade_Dispatch_ValidCommand_ReturnsAcceptedButNotExecuted()
    {
        var command = NodalOsOrchestrationCommandFixtures.PrepareRunCommand();
        var result = Facade().Dispatch(command);

        Assert.IsTrue(result.Accepted);
        Assert.IsFalse(result.Executed);
    }

    [TestMethod]
    public void Facade_Dispatch_InvalidCommand_ReturnsNotAcceptedAndNotExecuted()
    {
        var command = NodalOsOrchestrationCommandFixtures.PrepareRunCommand() with
        {
            CommandId = string.Empty
        };

        var result = Facade().Dispatch(command);

        Assert.IsFalse(result.Accepted);
        Assert.IsFalse(result.Executed);
        Assert.AreEqual(NodalOsOrchestrationState.Blocked, result.State);
        Assert.IsTrue(result.Errors.Count > 0);
    }

    [TestMethod]
    public void Facade_Result_ExecutedAlwaysFalse()
    {
        var commands = new[]
        {
            NodalOsOrchestrationCommandFixtures.CreateMissionCommand(),
            NodalOsOrchestrationCommandFixtures.PrepareRunCommand(),
            NodalOsOrchestrationCommandFixtures.ValidateRecipeManifestCommand(),
            NodalOsOrchestrationCommandFixtures.QuerySkillRegistryCommand(),
            NodalOsOrchestrationCommandFixtures.PrepareWorkerRequestCommand(),
            NodalOsOrchestrationCommandFixtures.AttachEvidenceCommand(),
            NodalOsOrchestrationCommandFixtures.CancelRunCommand(),
            NodalOsOrchestrationCommandFixtures.RequestHumanDecisionCommand()
        };

        var facade = Facade();

        foreach (var command in commands)
        {
            var result = facade.Dispatch(command);
            Assert.IsFalse(result.Executed, $"Executed should be false for {command.Kind}");
        }
    }

    [TestMethod]
    public void Facade_Result_RuntimeExecutionDeferredAlwaysTrue()
    {
        var command = NodalOsOrchestrationCommandFixtures.PrepareRunCommand();
        var result = Facade().Dispatch(command);

        Assert.IsTrue(result.RuntimeExecutionDeferred);
    }

    [TestMethod]
    public void Facade_NeverReturnsRuntimeExecutionAllowedTrue()
    {
        var command = NodalOsOrchestrationCommandFixtures.PrepareRunCommand();
        var validation = new NodalOsOrchestrationCommandValidator().ValidateCommand(command);

        Assert.IsFalse(validation.RuntimeExecutionAllowed);
    }

    [TestMethod]
    public void Facade_HighRiskCommandWithoutApproval_IsRejected()
    {
        var command = NodalOsOrchestrationCommandFixtures.PrepareRunCommand() with
        {
            RiskLevel = NodalOsOrchestrationCommandRiskLevel.High,
            RequiresHumanApproval = false
        };

        var result = Facade().Dispatch(command);

        Assert.IsFalse(result.Accepted);
        Assert.AreEqual(NodalOsOrchestrationState.Blocked, result.State);
        Assert.IsTrue(result.Errors.Any(e => e.Contains("approval", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    public void Facade_CriticalRiskCommandWithoutApproval_IsRejected()
    {
        var command = NodalOsOrchestrationCommandFixtures.PrepareRunCommand() with
        {
            RiskLevel = NodalOsOrchestrationCommandRiskLevel.Critical,
            RequiresHumanApproval = false
        };

        var result = Facade().Dispatch(command);

        Assert.IsFalse(result.Accepted);
        Assert.AreEqual(NodalOsOrchestrationState.Blocked, result.State);
        Assert.IsTrue(result.Errors.Any(e => e.Contains("approval", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    public void Facade_CommandWithInvalidEvidenceRef_IsRejected()
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

        var result = Facade().Dispatch(command);

        Assert.IsFalse(result.Accepted);
        Assert.AreEqual(NodalOsOrchestrationState.Blocked, result.State);
        Assert.IsTrue(result.Errors.Count > 0);
    }

    [TestMethod]
    public void Facade_CommandWithSensitiveSummary_IsSanitizedOrRejected()
    {
        var command = NodalOsOrchestrationCommandFixtures.CreateMissionCommand() with
        {
            Summary = "Authorization: Bearer fake-token-value-123456"
        };

        var result = Facade().Dispatch(command);

        Assert.IsFalse(result.Accepted);
        Assert.AreEqual(NodalOsOrchestrationState.Blocked, result.State);
    }

    [TestMethod]
    public void Facade_PrepareRun_DoesNotStartRun()
    {
        var command = NodalOsOrchestrationCommandFixtures.PrepareRunCommand();
        var result = Facade().Dispatch(command);

        Assert.IsTrue(result.Accepted);
        Assert.IsFalse(result.Executed);
        Assert.AreEqual(NodalOsOrchestrationState.Prepared, result.State);
    }

    [TestMethod]
    public void Facade_PrepareWorkerRequest_DoesNotCallWorkerRuntime()
    {
        var command = NodalOsOrchestrationCommandFixtures.PrepareWorkerRequestCommand();
        var result = Facade().Dispatch(command);

        Assert.IsTrue(result.Accepted);
        Assert.IsFalse(result.Executed);
        Assert.AreEqual(NodalOsOrchestrationState.Prepared, result.State);
    }

    [TestMethod]
    public void Facade_QuerySkillRegistry_DoesNotGrantRuntimePermission()
    {
        var command = NodalOsOrchestrationCommandFixtures.QuerySkillRegistryCommand();
        var result = Facade().Dispatch(command);

        Assert.IsTrue(result.Accepted);
        Assert.IsFalse(result.Executed);
        Assert.IsTrue(result.Warnings.Any(w => w.Contains("catalog lookup", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    public void Facade_ValidateRecipeManifest_DoesNotExecuteRecipe()
    {
        var command = NodalOsOrchestrationCommandFixtures.ValidateRecipeManifestCommand();
        var result = Facade().Dispatch(command);

        Assert.IsTrue(result.Accepted);
        Assert.IsFalse(result.Executed);
        Assert.AreEqual(NodalOsOrchestrationState.Completed, result.State);
    }

    [TestMethod]
    public void Facade_ValidateSkill_DoesNotExecuteSkill()
    {
        var command = NodalOsOrchestrationCommandFixtures.ValidateRecipeManifestCommand() with
        {
            Kind = NodalOsOrchestrationCommandKind.ValidateSkill
        };
        var result = Facade().Dispatch(command);

        Assert.IsTrue(result.Accepted);
        Assert.IsFalse(result.Executed);
        Assert.AreEqual(NodalOsOrchestrationState.Completed, result.State);
    }

    [TestMethod]
    public void Facade_PauseResumeCancel_AreContractOnly()
    {
        var facade = Facade();

        var pause = NodalOsOrchestrationCommandFixtures.CancelRunCommand() with
        {
            Kind = NodalOsOrchestrationCommandKind.PauseRun
        };
        var resume = pause with { Kind = NodalOsOrchestrationCommandKind.ResumeRun };
        var cancel = NodalOsOrchestrationCommandFixtures.CancelRunCommand();

        foreach (var command in new[] { pause, resume, cancel })
        {
            var result = facade.Dispatch(command);
            Assert.IsTrue(result.Accepted, $"{command.Kind} should be accepted");
            Assert.IsFalse(result.Executed, $"{command.Kind} Executed should be false");
            Assert.AreEqual(NodalOsOrchestrationState.Completed, result.State, $"{command.Kind} should be Completed");
            Assert.IsTrue(result.Warnings.Any(w => w.Contains("contract-only", StringComparison.OrdinalIgnoreCase)),
                $"{command.Kind} should have contract-only warning");
        }
    }

    [TestMethod]
    public void Facade_AttachEvidence_DoesNotAuthorizeAction()
    {
        var command = NodalOsOrchestrationCommandFixtures.AttachEvidenceCommand();
        var result = Facade().Dispatch(command);

        Assert.IsTrue(result.Accepted);
        Assert.IsFalse(result.Executed);
        Assert.AreEqual(NodalOsOrchestrationState.Completed, result.State);
    }

    [TestMethod]
    public void Facade_EvaluateVerificationBeforeDone_DoesNotCloseAutomatically()
    {
        var command = NodalOsOrchestrationCommandFixtures.PrepareRunCommand() with
        {
            Kind = NodalOsOrchestrationCommandKind.EvaluateVerificationBeforeDone
        };
        var result = Facade().Dispatch(command);

        Assert.IsTrue(result.Accepted);
        Assert.IsFalse(result.Executed);
        Assert.AreEqual(NodalOsOrchestrationState.Completed, result.State);
        Assert.IsTrue(result.Warnings.Any(w => w.Contains("does not close", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    public void Facade_DoesNotReturnRunningFutureAsActiveState()
    {
        var facade = Facade();

        foreach (var kind in Enum.GetValues<NodalOsOrchestrationCommandKind>())
        {
            var command = NodalOsOrchestrationCommandFixtures.CreateMissionCommand() with
            {
                Kind = kind
            };
            var result = facade.Dispatch(command);
            Assert.AreNotEqual(NodalOsOrchestrationState.RunningFuture, result.State,
                $"RunningFuture should not be returned for {kind}");
        }
    }

    [TestMethod]
    public void Facade_DoesNotReturnPausedFutureAsActiveState()
    {
        var facade = Facade();

        foreach (var kind in Enum.GetValues<NodalOsOrchestrationCommandKind>())
        {
            var command = NodalOsOrchestrationCommandFixtures.CreateMissionCommand() with
            {
                Kind = kind
            };
            var result = facade.Dispatch(command);
            Assert.AreNotEqual(NodalOsOrchestrationState.PausedFuture, result.State,
                $"PausedFuture should not be returned for {kind}");
        }
    }

    [TestMethod]
    public void Facade_CompletedMeansContractHandlingOnly()
    {
        var command = NodalOsOrchestrationCommandFixtures.ValidateRecipeManifestCommand();
        var result = Facade().Dispatch(command);

        Assert.AreEqual(NodalOsOrchestrationState.Completed, result.State);
        Assert.IsFalse(result.Executed);
        Assert.IsTrue(result.Accepted);
    }

    [TestMethod]
    public void Facade_AcceptedDoesNotMeanExecuted()
    {
        var command = NodalOsOrchestrationCommandFixtures.PrepareRunCommand();
        var result = Facade().Dispatch(command);

        Assert.IsTrue(result.Accepted);
        Assert.IsFalse(result.Executed);
    }

    [TestMethod]
    public void Facade_DoesNotReferenceBrowserExecutorCdp()
    {
        var facadeFile = File.ReadAllText(Path.Combine(FindRepoRoot(), "src", "OneBrain.AgentOperations.Core", "NodalOsOrchestrationInProcessFacade.cs"));

        Assert.IsFalse(facadeFile.Contains("ChromeCdpBrowserExecutor", StringComparison.Ordinal));
        Assert.IsFalse(facadeFile.Contains("BrowserRuntime", StringComparison.Ordinal));
        Assert.IsFalse(facadeFile.Contains("CdpClient", StringComparison.Ordinal));
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
        var facadeFile = File.ReadAllText(Path.Combine(FindRepoRoot(), "src", "OneBrain.AgentOperations.Core", "NodalOsOrchestrationInProcessFacade.cs"));

        StringAssert.Contains(facadeFile, "NodalOsOrchestrationInProcessFacade");
        Assert.IsFalse(facadeFile.Contains("public sealed class Nexa", StringComparison.Ordinal));
    }

    private static NodalOsOrchestrationInProcessFacade Facade() => new();

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
            "m427",
            "orchestration-in-process-facade-v1-summary.json"));

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
