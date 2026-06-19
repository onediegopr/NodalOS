using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("ScheduledReadOnlyIntegrationNoDivergence")]
[TestCategory("ScheduledReadOnlyRunContracts")]
[TestCategory("OrchestrationInProcessFacadeV1")]
[TestCategory("BrowserAdapterProjectSkeleton")]
public sealed class NodalOsScheduledReadOnlyIntegrationNoDivergenceM440M442Tests
{
    private readonly NodalOsScheduledReadOnlyRunValidator scheduleValidator = new();
    private readonly NodalOsOrchestrationInProcessFacade facade = new();

    [TestMethod]
    public void InvalidMutableActionSchedule_IsRejectedByValidateSchedule()
    {
        var result = scheduleValidator.ValidateSchedule(NodalOsScheduledReadOnlyRunFixtures.InvalidMutableActionSchedule());

        Assert.IsFalse(result.IsValid);
        AssertContains(result.Errors, "forbidden action marker");
    }

    [TestMethod]
    public void ScheduleAllowedTargets_WithSubmitAction_IsRejected()
    {
        AssertForbiddenAllowedTarget("https://lab.nodalos.com.ar/read-only?action=submit");
    }

    [TestMethod]
    public void ScheduleAllowedTargets_WithClickAction_IsRejected()
    {
        AssertForbiddenAllowedTarget("https://lab.nodalos.com.ar/read-only?action=click");
    }

    [TestMethod]
    public void ScheduleAllowedTargets_WithDeleteAction_IsRejected()
    {
        AssertForbiddenAllowedTarget("https://lab.nodalos.com.ar/read-only?action=delete");
    }

    [TestMethod]
    public void ScheduleSummary_WithClickSubmitMutate_IsRejected()
    {
        var schedule = NodalOsScheduledReadOnlyRunFixtures.ValidManualOnlySchedule() with
        {
            Summary = "attempts to click submit and mutate state"
        };
        var result = scheduleValidator.ValidateSchedule(schedule);

        Assert.IsFalse(result.IsValid);
        AssertContains(result.Errors, "summary contains a forbidden action marker");
    }

    [TestMethod]
    public void ScheduleSummary_WithForbiddenAction_DoesNotExposeRawSensitiveValue()
    {
        const string rawSensitiveValue = "Bearer abcdefghijklmnopqrstuvwxyz";
        var schedule = NodalOsScheduledReadOnlyRunFixtures.ValidManualOnlySchedule() with
        {
            Summary = $"click submit with {rawSensitiveValue}"
        };
        var result = scheduleValidator.ValidateSchedule(schedule);
        var combinedErrors = string.Join(" | ", result.Errors);

        Assert.IsFalse(result.IsValid);
        AssertContains(result.Errors, "forbidden action marker");
        Assert.IsFalse(combinedErrors.Contains(rawSensitiveValue, StringComparison.Ordinal));
    }

    [TestMethod]
    public void PreviewForbiddenActions_StillRejected()
    {
        var preview = NodalOsScheduledReadOnlyRunFixtures.ValidDryRunPreview() with
        {
            PlannedReadOnlyOperations = ["click submit"]
        };
        var result = scheduleValidator.ValidatePreview(preview);

        Assert.IsFalse(result.IsValid);
        AssertContains(result.Errors, "forbidden");
    }

    [TestMethod]
    public void SafeReadOnlySchedule_TargetsRemainAccepted()
    {
        var schedule = NodalOsScheduledReadOnlyRunFixtures.ValidManualOnlySchedule() with
        {
            AllowedTargets = ["https://lab.nodalos.com.ar/read-only/status"]
        };

        Assert.IsTrue(scheduleValidator.ValidateSchedule(schedule).IsValid);
    }

    [TestMethod]
    public void ScheduledReadOnly_ValidScheduleRequestPreview_PreserveNoExecutionFlags()
    {
        var schedule = NodalOsScheduledReadOnlyRunFixtures.ValidManualOnlySchedule();
        var request = NodalOsScheduledReadOnlyRunFixtures.RequestFromSchedule(schedule);
        var preview = NodalOsScheduledReadOnlyRunFixtures.ValidDryRunPreview();

        Assert.IsTrue(scheduleValidator.ValidateSchedule(schedule).IsValid);
        Assert.IsTrue(scheduleValidator.ValidateRunRequest(request).IsValid);
        Assert.IsTrue(scheduleValidator.ValidatePreview(preview).IsValid);

        Assert.IsTrue(schedule.ReadOnly);
        Assert.IsFalse(schedule.RuntimeExecutionAllowed);
        Assert.IsTrue(schedule.RuntimeExecutionDeferred);
        Assert.IsTrue(schedule.RequiresGlobalPolicyEvaluation);
        Assert.IsTrue(request.ManualTriggerRequired);
        Assert.IsTrue(preview.DryRunOnly);
        Assert.IsFalse(preview.Executed);
    }

    [TestMethod]
    public void ScheduledReadOnly_ToOrchestrationFacade_NoDivergence_ExecutedFalse()
    {
        var result = facade.Dispatch(ToPrepareRunCommand());

        Assert.IsTrue(result.Accepted);
        Assert.IsFalse(result.Executed);
    }

    [TestMethod]
    public void ScheduledReadOnly_ToOrchestrationFacade_NoDivergence_RuntimeDeferred()
    {
        var command = ToPrepareRunCommand();
        var result = facade.Dispatch(command);

        Assert.IsFalse(command.RuntimeExecutionAllowed);
        Assert.IsTrue(command.RuntimeExecutionDeferred);
        Assert.IsTrue(result.RuntimeExecutionDeferred);
    }

    [TestMethod]
    public void ScheduledReadOnly_ToOrchestrationFacade_NoDivergence_RequiresPolicy()
    {
        var command = ToPrepareRunCommand();
        var validation = new NodalOsOrchestrationCommandValidator().ValidateCommand(command);

        Assert.IsTrue(command.RequiresGlobalPolicyEvaluation);
        Assert.IsTrue(validation.RequiresGlobalPolicyEvaluation);
        Assert.IsTrue(validation.IsValid);
    }

    [TestMethod]
    public void ScheduledReadOnly_ToProgressReportOrRunReport_DoesNotGrantAuthority()
    {
        var evidenceRef = NodalOsScheduledReadOnlyRunFixtures.ValidEvidenceRef();
        var runReport = new NexaRunReport
        {
            RunId = "run-scheduled-readonly-001",
            MissionId = "mission-internal-001",
            TaskId = "task-internal-001",
            RecipeId = "recipe-readonly-001",
            Goal = "Produce scheduled read-only report metadata.",
            Status = NexaRunStatus.Planned,
            StartedAt = NodalOsScheduledReadOnlyRunFixtures.FixedTimestamp,
            Steps = [],
            EvidenceRefs = [],
            FinalSummary = "Report metadata only. No runtime execution."
        };
        var progressReport = new NodalOsAgentProgressReport
        {
            ReportId = "progress-scheduled-readonly-001",
            MissionId = "mission-internal-001",
            TaskId = "task-internal-001",
            RunId = runReport.RunId,
            Kind = NodalOsAgentProgressReportKind.Diagnostic,
            Status = NodalOsAgentProgressReportStatus.Informational,
            Summary = "Progress metadata only. No runtime authority.",
            EvidenceRefs = [],
            CreatedAt = NodalOsScheduledReadOnlyRunFixtures.FixedTimestamp
        };

        Assert.AreEqual(NodalOsEvidenceBridgeAuthority.NoAuthority, evidenceRef.Authority);
        Assert.AreEqual(NexaRunStatus.Planned, runReport.Status);
        Assert.AreEqual(NodalOsAgentProgressReportStatus.Informational, progressReport.Status);
    }

    [TestMethod]
    public void ScheduledReadOnly_InvalidMutableSchedule_CannotReachFacadeAsAccepted()
    {
        var scheduleValidation = scheduleValidator.ValidateSchedule(NodalOsScheduledReadOnlyRunFixtures.InvalidMutableActionSchedule());

        Assert.IsFalse(scheduleValidation.IsValid);
        Assert.IsFalse(scheduleValidation.CanPassSchedulePolicy);
    }

    [TestMethod]
    public void ScheduledReadOnly_EvidenceRefsValidateViaBridgeAcrossLayers()
    {
        var evidenceRef = NodalOsScheduledReadOnlyRunFixtures.ValidEvidenceRef();
        var bridgeResult = new NodalOsEvidenceRefBridge().ValidateBridgeRef(evidenceRef);
        var command = NodalOsOrchestrationCommandFixtures.AttachEvidenceCommand() with
        {
            EvidenceRefs = [evidenceRef]
        };
        var facadeResult = facade.Dispatch(command);

        Assert.IsTrue(bridgeResult.Accepted);
        Assert.IsTrue(facadeResult.Accepted);
        Assert.IsFalse(facadeResult.Executed);
    }

    [TestMethod]
    public void ScheduledReadOnly_CommonRedactionAcrossLayers()
    {
        const string rawSensitiveValue = "Bearer abcdefghijklmnopqrstuvwxyz";
        var schedule = NodalOsScheduledReadOnlyRunFixtures.ValidManualOnlySchedule() with
        {
            AllowedTargets = [$"https://lab.nodalos.com.ar/read-only?access_token={rawSensitiveValue}"]
        };
        var scheduleResult = scheduleValidator.ValidateSchedule(schedule);
        var command = ToPrepareRunCommand() with
        {
            Summary = $"internal summary {rawSensitiveValue}"
        };
        var facadeResult = facade.Dispatch(command);

        Assert.IsFalse(scheduleResult.IsValid);
        Assert.IsFalse(facadeResult.Accepted);
        Assert.IsFalse(string.Join(" | ", facadeResult.Errors).Contains(rawSensitiveValue, StringComparison.Ordinal));
    }

    [TestMethod]
    public void AgentOperationsContracts_DoesNotReferenceBrowserExecutorCdp()
    {
        AssertProjectDoesNotReference(ProjectPath("OneBrain.AgentOperations.Contracts"), "BrowserExecutor.Cdp");
    }

    [TestMethod]
    public void AgentOperationsCore_DoesNotReferenceBrowserExecutorCdp()
    {
        AssertProjectDoesNotReference(ProjectPath("OneBrain.AgentOperations.Core"), "BrowserExecutor.Cdp");
    }

    [TestMethod]
    public void AgentOperationsAdaptersBrowser_DoesNotReferenceBrowserExecutorCdp()
    {
        AssertProjectDoesNotReference(ProjectPath("OneBrain.AgentOperations.Adapters.Browser"), "BrowserExecutor.Cdp");
    }

    [TestMethod]
    public void AgentOperationsProjects_DoNotReferenceChromeCdpPackages()
    {
        foreach (var projectName in new[]
                 {
                     "OneBrain.AgentOperations.Contracts",
                     "OneBrain.AgentOperations.Core",
                     "OneBrain.AgentOperations.Adapters.Browser"
                 })
        {
            var project = File.ReadAllText(ProjectPath(projectName));
            Assert.IsFalse(project.Contains("Chrome", StringComparison.OrdinalIgnoreCase), projectName);
            Assert.IsFalse(project.Contains("Playwright", StringComparison.OrdinalIgnoreCase), projectName);
            Assert.IsFalse(project.Contains("Puppeteer", StringComparison.OrdinalIgnoreCase), projectName);
            Assert.IsFalse(project.Contains("Selenium", StringComparison.OrdinalIgnoreCase), projectName);
        }
    }

    [TestMethod]
    public void BrowserExecutorCdp_RemainsTemporaryHostOnly()
    {
        var cdpProject = File.ReadAllText(Path.Combine(FindRepoRoot(), "src", "OneBrain.BrowserExecutor.Cdp", "OneBrain.BrowserExecutor.Cdp.csproj"));

        StringAssert.Contains(cdpProject, "OneBrain.AgentOperations.Core");
        Assert.IsTrue(File.Exists(Path.Combine(FindRepoRoot(), "src", "OneBrain.BrowserExecutor.Cdp", "ChromeCdpBrowserExecutor.cs")));
    }

    [TestMethod]
    public void NoSchedulerTimerBackgroundWorkerApiUiExecutionIntroduced()
    {
        var coreSource = File.ReadAllText(Path.Combine(FindRepoRoot(), "src", "OneBrain.AgentOperations.Core", "NodalOsScheduledReadOnlyRunServices.cs"));
        var adapterMarker = File.ReadAllText(Path.Combine(FindRepoRoot(), "src", "OneBrain.AgentOperations.Adapters.Browser", "NodalOsBrowserAgentOperationsAdapterBoundary.cs"));

        Assert.IsFalse(coreSource.Contains("System.Threading.Timer", StringComparison.Ordinal));
        Assert.IsFalse(coreSource.Contains("PeriodicTimer", StringComparison.Ordinal));
        Assert.IsFalse(coreSource.Contains("BackgroundService", StringComparison.Ordinal));
        Assert.IsFalse(coreSource.Contains("IHostedService", StringComparison.Ordinal));
        Assert.IsFalse(coreSource.Contains("MapGet", StringComparison.Ordinal));
        StringAssert.Contains(adapterMarker, "RuntimeBehaviorImplemented = false");
        StringAssert.Contains(adapterMarker, "OrchestrationApiImplemented = false");
        StringAssert.Contains(adapterMarker, "ExecutionImplemented = false");
    }

    private void AssertForbiddenAllowedTarget(string target)
    {
        var schedule = NodalOsScheduledReadOnlyRunFixtures.ValidManualOnlySchedule() with
        {
            AllowedTargets = [target]
        };
        var result = scheduleValidator.ValidateSchedule(schedule);

        Assert.IsFalse(result.IsValid, target);
        AssertContains(result.Errors, "allowed target contains a forbidden action marker");
    }

    private static NodalOsOrchestrationCommandEnvelope ToPrepareRunCommand()
    {
        var schedule = NodalOsScheduledReadOnlyRunFixtures.ValidManualOnlySchedule();
        return NodalOsOrchestrationCommandFixtures.PrepareRunCommand() with
        {
            MissionId = schedule.MissionId,
            TaskId = schedule.TaskId,
            RecipeId = schedule.RecipeId,
            SkillId = schedule.SkillId,
            EvidenceRefs = schedule.EvidenceRefs,
            Summary = "Prepare scheduled read-only run contract only."
        };
    }

    private static void AssertProjectDoesNotReference(string projectPath, string value)
    {
        var project = File.ReadAllText(projectPath);
        Assert.IsFalse(project.Contains(value, StringComparison.OrdinalIgnoreCase), projectPath);
    }

    private static string ProjectPath(string projectName) =>
        Path.Combine(FindRepoRoot(), "src", projectName, $"{projectName}.csproj");

    private static void AssertContains(IEnumerable<string> values, string expected)
    {
        Assert.IsTrue(
            values.Any(value => value.Contains(expected, StringComparison.OrdinalIgnoreCase)),
            $"Expected a validation message containing '{expected}'.");
    }

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
