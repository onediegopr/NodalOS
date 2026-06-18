using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("MissionTaskDomain")]
[TestCategory("AgentWorkboard")]
[TestCategory("Mission")]
[TestCategory("AgentTask")]
[TestCategory("BlockerReport")]
[TestCategory("VerificationCheck")]
[TestCategory("ProgressNote")]
public sealed class NodalOsMissionTaskDomainM347M349Tests
{
    private readonly NodalOsAgentWorkboardValidator validator = new();

    private static readonly string RepoRoot = AppDomain.CurrentDomain.BaseDirectory.Contains("bin\\Debug") ||
                                              AppDomain.CurrentDomain.BaseDirectory.Contains("bin\\Release")
        ? Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", ".."))
        : AppDomain.CurrentDomain.BaseDirectory;

    [TestMethod]
    public void CanCreateValidMission()
    {
        var mission = NodalOsAgentWorkboardFixtures.ValidMission();

        var result = validator.ValidateMission(mission);

        Assert.IsTrue(result.CanComplete);
        Assert.AreEqual(0, result.Errors.Count);
        Assert.AreEqual("operator", mission.HumanOwner);
    }

    [TestMethod]
    public void CanCreateValidAgentTask()
    {
        var task = NodalOsAgentWorkboardFixtures.ValidTask();

        var result = validator.ValidateTask(task);

        Assert.IsTrue(result.CanComplete);
        Assert.AreEqual(NexaAgentTaskStatus.Ready, task.Status);
    }

    [TestMethod]
    public void CanAddProgressNote()
    {
        var task = NodalOsAgentWorkboardFixtures.TaskWithProgressNote();

        Assert.AreEqual(1, task.ProgressNotes.Count);
        Assert.AreEqual("codex", task.ProgressNotes[0].Author);
        Assert.AreEqual(1, task.ProgressNotes[0].EvidenceRefs.Count);
    }

    [TestMethod]
    public void CanAddBlockerReport()
    {
        var task = NodalOsAgentWorkboardFixtures.TaskWithBlocker();

        Assert.AreEqual(1, task.Blockers.Count);
        Assert.AreEqual(NexaBlockerKind.TestFailure, task.Blockers[0].Kind);
        Assert.AreEqual(NexaBlockerSeverity.Warning, task.Blockers[0].Severity);
    }

    [TestMethod]
    public void CanAddVerificationCheck()
    {
        var task = NodalOsAgentWorkboardFixtures.TaskWithVerificationCheck();

        Assert.AreEqual(1, task.VerificationChecks.Count);
        Assert.IsTrue(task.VerificationChecks[0].Required);
        Assert.AreEqual(NexaVerificationStatus.Passed, task.VerificationChecks[0].Status);
    }

    [TestMethod]
    public void CompletedTask_WithPassedVerificationAndEvidence_CanComplete()
    {
        var task = NodalOsAgentWorkboardFixtures.CompletedTaskWithEvidenceAndPassedVerification();

        var result = validator.ValidateTaskCanComplete(task);

        Assert.IsTrue(result.CanComplete);
        Assert.AreEqual(0, result.Errors.Count);
    }

    [TestMethod]
    public void CompletedTask_WithoutEvidenceOrReason_CannotComplete()
    {
        var task = NodalOsAgentWorkboardFixtures.CompletedTaskWithoutEvidence();

        var result = validator.ValidateTaskCanComplete(task);

        Assert.IsFalse(result.CanComplete);
        StringAssert.Contains(string.Join(" ", result.Errors), "evidence or explicit completion reason");
    }

    [TestMethod]
    public void CompletedTask_WithBlockingBlocker_CannotComplete()
    {
        var task = NodalOsAgentWorkboardFixtures.CompletedTaskWithBlockingBlocker();

        var result = validator.ValidateTaskCanComplete(task);

        Assert.IsFalse(result.CanComplete);
        StringAssert.Contains(string.Join(" ", result.Errors), "Blocking or critical blocker");
    }

    [TestMethod]
    public void CompletedTask_WithCriticalBlocker_CannotComplete()
    {
        var task = NodalOsAgentWorkboardFixtures.CompletedTaskWithEvidenceAndPassedVerification() with
        {
            Blockers = [NodalOsAgentWorkboardFixtures.Blocker(severity: NexaBlockerSeverity.Critical)]
        };

        var result = validator.ValidateTaskCanComplete(task);

        Assert.IsFalse(result.CanComplete);
        Assert.IsTrue(validator.HasBlockingBlockers(task));
    }

    [TestMethod]
    public void CompletedTask_WithPendingRequiredVerification_CannotComplete()
    {
        var task = NodalOsAgentWorkboardFixtures.CompletedTaskWithPendingVerification();

        var result = validator.ValidateTaskCanComplete(task);

        Assert.IsFalse(result.CanComplete);
        Assert.IsTrue(validator.HasPendingOrFailedRequiredVerification(task));
    }

    [TestMethod]
    public void CompletedTask_WithFailedRequiredVerification_CannotComplete()
    {
        var task = NodalOsAgentWorkboardFixtures.ValidTask(status: NexaAgentTaskStatus.Completed) with
        {
            VerificationChecks = [NodalOsAgentWorkboardFixtures.Verification(status: NexaVerificationStatus.Failed)],
            EvidenceRefs = [NodalOsAgentWorkboardFixtures.Evidence("evidence-failed")]
        };

        var result = validator.ValidateTaskCanComplete(task);

        Assert.IsFalse(result.CanComplete);
        Assert.IsTrue(validator.HasPendingOrFailedRequiredVerification(task));
    }

    [TestMethod]
    public void CompletedTask_WithSkippedRequiredVerificationWithoutReason_CannotComplete()
    {
        var task = NodalOsAgentWorkboardFixtures.CompletedTaskWithSkippedVerificationNoReason();

        var result = validator.ValidateTaskCanComplete(task);

        Assert.IsFalse(result.CanComplete);
        StringAssert.Contains(string.Join(" ", result.Errors), "Skipped required verification");
    }

    [TestMethod]
    public void CompletedTask_WithSkippedRequiredVerificationWithReason_CanComplete()
    {
        var task = NodalOsAgentWorkboardFixtures.ValidTask(status: NexaAgentTaskStatus.Completed) with
        {
            VerificationChecks = [NodalOsAgentWorkboardFixtures.Verification(status: NexaVerificationStatus.SkippedWithReason, detail: "Superseded by operator-approved report.")],
            CompletionReason = "Closed with explicit reason."
        };

        var result = validator.ValidateTaskCanComplete(task);

        Assert.IsTrue(result.CanComplete);
    }

    [TestMethod]
    public void CancelledTask_WithoutReason_ProducesWarning()
    {
        var task = NodalOsAgentWorkboardFixtures.ValidTask(status: NexaAgentTaskStatus.Cancelled);

        var result = validator.ValidateTask(task);

        Assert.AreEqual(0, result.Errors.Count);
        Assert.IsTrue(result.Warnings.Any(w => w.Contains("Cancelled task", StringComparison.Ordinal)));
    }

    [TestMethod]
    public void ModelsSerializeToJson_AndPreserveEvidenceRefs()
    {
        var task = NodalOsAgentWorkboardFixtures.CompletedTaskWithEvidenceAndPassedVerification();

        var json = JsonSerializer.Serialize(task);
        var roundTrip = JsonSerializer.Deserialize<NexaAgentTask>(json);

        Assert.IsNotNull(roundTrip);
        Assert.AreEqual(task.TaskId, roundTrip.TaskId);
        Assert.AreEqual(1, roundTrip.EvidenceRefs.Count);
        Assert.AreEqual("evidence-completion", roundTrip.EvidenceRefs[0].EvidenceId);
    }

    [TestMethod]
    public void BlockerIsSerializableAndVisible()
    {
        var blocker = NodalOsAgentWorkboardFixtures.Blocker(severity: NexaBlockerSeverity.Blocking);

        var json = JsonSerializer.Serialize(blocker, new JsonSerializerOptions
        {
            Converters = { new JsonStringEnumConverter() }
        });

        StringAssert.Contains(json, "Blocking");
        StringAssert.Contains(json, "Blocker fixture.");
    }

    [TestMethod]
    public void HumanOwnerRequired()
    {
        var task = NodalOsAgentWorkboardFixtures.ValidTask() with { HumanOwner = "" };

        var result = validator.ValidateTask(task);

        Assert.IsFalse(result.CanComplete);
        StringAssert.Contains(string.Join(" ", result.Errors), "human owner");
    }

    [TestMethod]
    public void VerificationBeforeDonePolicy_IsEnforced()
    {
        var task = NodalOsAgentWorkboardFixtures.ValidTask(status: NexaAgentTaskStatus.Completed) with
        {
            VerificationChecks = [NodalOsAgentWorkboardFixtures.Verification(status: NexaVerificationStatus.Pending)],
            CompletionReason = "Cannot close while verification is pending."
        };

        var result = validator.ValidateTask(task);

        Assert.IsFalse(result.CanComplete);
        StringAssert.Contains(string.Join(" ", result.Errors), "Pending or failed required verification");
    }

    [TestMethod]
    public void Artifact_ValidatesM349Flags()
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(SourcePath(
            "artifacts", "agent-operations", "m349", "mission-task-domain-summary.json")));
        var root = doc.RootElement;

        Assert.AreEqual("M347-M349", root.GetProperty("milestone").GetString());
        Assert.AreEqual("MISSION_TASK_DOMAIN_READY", root.GetProperty("readinessDecision").GetString());
        Assert.IsTrue(root.GetProperty("missionModelCreated").GetBoolean());
        Assert.IsTrue(root.GetProperty("taskModelCreated").GetBoolean());
        Assert.IsTrue(root.GetProperty("progressNoteModelCreated").GetBoolean());
        Assert.IsTrue(root.GetProperty("blockerReportModelCreated").GetBoolean());
        Assert.IsTrue(root.GetProperty("verificationCheckModelCreated").GetBoolean());
        Assert.IsTrue(root.GetProperty("evidenceRefModelCreated").GetBoolean());
        Assert.IsTrue(root.GetProperty("validationServiceCreated").GetBoolean());
        Assert.IsTrue(root.GetProperty("completedRequiresVerification").GetBoolean());
        Assert.IsTrue(root.GetProperty("completedRequiresEvidenceOrReason").GetBoolean());
        Assert.IsTrue(root.GetProperty("blockingBlockerPreventsCompletion").GetBoolean());
        Assert.IsTrue(root.GetProperty("criticalBlockerPreventsCompletion").GetBoolean());
        Assert.IsTrue(root.GetProperty("pendingVerificationPreventsCompletion").GetBoolean());
        Assert.IsTrue(root.GetProperty("failedVerificationPreventsCompletion").GetBoolean());
        Assert.IsTrue(root.GetProperty("humanOwnerRequired").GetBoolean());
        Assert.IsTrue(root.GetProperty("noUiImplemented").GetBoolean());
        Assert.IsTrue(root.GetProperty("noOrchestrationApiImplemented").GetBoolean());
        Assert.IsTrue(root.GetProperty("noRuntimeActionsIntroduced").GetBoolean());
        Assert.AreEqual("M350-M352 Failure Taxonomy + Run Report V1", root.GetProperty("nextRecommendedMilestone").GetString());
    }

    private static string SourcePath(params string[] parts) => Path.Combine(new[] { RepoRoot }.Concat(parts).ToArray());
}
