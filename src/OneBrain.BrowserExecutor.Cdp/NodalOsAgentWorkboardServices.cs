using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.BrowserExecutor.Cdp;

public sealed class NodalOsAgentWorkboardValidator
{
    private readonly NodalOsVerificationBeforeDoneGate completionGate;

    public NodalOsAgentWorkboardValidator(NodalOsVerificationBeforeDoneGate? completionGate = null)
    {
        this.completionGate = completionGate ?? new NodalOsVerificationBeforeDoneGate();
    }

    public NexaTaskValidationResult ValidateMission(NexaMission mission)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        AddRequired(errors, mission.MissionId, "MissionId is required.");
        AddRequired(errors, mission.Title, "Mission title is required.");
        AddRequired(errors, mission.HumanOwner, "Mission human owner is required.");

        foreach (var task in mission.Tasks)
        {
            if (!string.Equals(task.MissionId, mission.MissionId, StringComparison.Ordinal))
                errors.Add($"Task {task.TaskId} belongs to mission {task.MissionId}, expected {mission.MissionId}.");
        }

        return new NexaTaskValidationResult
        {
            CanComplete = errors.Count == 0,
            Errors = errors,
            Warnings = warnings
        };
    }

    public NexaTaskValidationResult ValidateTask(NexaAgentTask task)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        AddRequired(errors, task.TaskId, "TaskId is required.");
        AddRequired(errors, task.MissionId, "Task MissionId is required.");
        AddRequired(errors, task.Title, "Task title is required.");
        AddRequired(errors, task.HumanOwner, "Task human owner is required.");

        if (task.Status == NexaAgentTaskStatus.Completed)
            errors.AddRange(ValidateTaskCanComplete(task).Errors);

        if (task.Status == NexaAgentTaskStatus.Cancelled && string.IsNullOrWhiteSpace(task.CompletionReason))
            warnings.Add("Cancelled task should include a completion reason.");

        if (task.Status == NexaAgentTaskStatus.Failed &&
            task.Blockers.Count == 0 &&
            task.EvidenceRefs.Count == 0 &&
            string.IsNullOrWhiteSpace(task.CompletionReason))
        {
            warnings.Add("Failed task should preserve blocker, evidence, or explicit failure reason.");
        }

        return new NexaTaskValidationResult
        {
            CanComplete = errors.Count == 0,
            Errors = errors,
            Warnings = warnings
        };
    }

    public NexaTaskValidationResult ValidateTaskCanComplete(NexaAgentTask task)
    {
        var gateResult = completionGate.EvaluateTask(task);
        return NodalOsCompletionGateCompatibilityAdapter.ToTaskValidationResult(gateResult);
    }

    public NexaTaskValidationResult CreateCompletionValidationResult(
        IReadOnlyList<string> errors,
        IReadOnlyList<string>? warnings = null) =>
        new()
        {
            CanComplete = errors.Count == 0,
            Errors = errors,
            Warnings = warnings ?? Array.Empty<string>()
        };

    public bool HasBlockingBlockers(NexaAgentTask task) =>
        task.Blockers.Any(b => b.Severity is NexaBlockerSeverity.Blocking or NexaBlockerSeverity.Critical);

    public bool HasPendingOrFailedRequiredVerification(NexaAgentTask task) =>
        task.VerificationChecks.Any(c =>
            c.Required &&
            c.Status is NexaVerificationStatus.Pending or NexaVerificationStatus.Failed);

    public bool HasEvidenceOrCompletionReason(NexaAgentTask task) =>
        task.EvidenceRefs.Count > 0 ||
        task.ProgressNotes.Any(n => n.EvidenceRefs.Count > 0) ||
        task.Blockers.Any(b => b.EvidenceRefs.Count > 0) ||
        task.VerificationChecks.Any(c => c.EvidenceRefs.Count > 0) ||
        !string.IsNullOrWhiteSpace(task.CompletionReason);

    private static void AddRequired(List<string> errors, string? value, string message)
    {
        if (string.IsNullOrWhiteSpace(value))
            errors.Add(message);
    }
}

public static class NodalOsAgentWorkboardFixtures
{
    private static readonly DateTimeOffset FixedTimestamp = new(2026, 6, 18, 0, 0, 0, TimeSpan.Zero);

    public static NexaMission ValidMission(string missionId = "mission-agent-ops-001") =>
        new()
        {
            MissionId = missionId,
            Title = "Agent operations mission",
            Description = "Controlled mission fixture.",
            Status = NexaMissionStatus.Ready,
            HumanOwner = "operator",
            Tasks = [ValidTask(missionId: missionId)],
            EvidenceRefs = [Evidence("evidence-mission")],
            CreatedAt = FixedTimestamp,
            UpdatedAt = FixedTimestamp
        };

    public static NexaAgentTask ValidTask(
        string taskId = "task-agent-ops-001",
        string missionId = "mission-agent-ops-001",
        NexaAgentTaskStatus status = NexaAgentTaskStatus.Ready) =>
        new()
        {
            TaskId = taskId,
            MissionId = missionId,
            Title = "Agent task fixture",
            Description = "Controlled task fixture.",
            Status = status,
            AssignedAgent = "codex",
            HumanOwner = "operator",
            Priority = 10,
            CreatedAt = FixedTimestamp,
            UpdatedAt = FixedTimestamp
        };

    public static NexaAgentTask CompletedTaskWithEvidenceAndPassedVerification() =>
        ValidTask(status: NexaAgentTaskStatus.Completed) with
        {
            VerificationChecks = [Verification(status: NexaVerificationStatus.Passed, evidenceRefs: [Evidence("evidence-verification")])],
            EvidenceRefs = [Evidence("evidence-completion")]
        };

    public static NexaAgentTask CompletedTaskWithoutEvidence() =>
        ValidTask(status: NexaAgentTaskStatus.Completed) with
        {
            VerificationChecks = [Verification(status: NexaVerificationStatus.Passed)]
        };

    public static NexaAgentTask CompletedTaskWithBlockingBlocker() =>
        CompletedTaskWithEvidenceAndPassedVerification() with
        {
            Blockers = [Blocker(severity: NexaBlockerSeverity.Blocking)]
        };

    public static NexaAgentTask CompletedTaskWithPendingVerification() =>
        ValidTask(status: NexaAgentTaskStatus.Completed) with
        {
            VerificationChecks = [Verification(status: NexaVerificationStatus.Pending)],
            EvidenceRefs = [Evidence("evidence-pending")]
        };

    public static NexaAgentTask CompletedTaskWithSkippedVerificationNoReason() =>
        ValidTask(status: NexaAgentTaskStatus.Completed) with
        {
            VerificationChecks = [Verification(status: NexaVerificationStatus.SkippedWithReason, detail: null)],
            EvidenceRefs = [Evidence("evidence-skipped")]
        };

    public static NexaAgentTask TaskWithProgressNote() =>
        ValidTask() with { ProgressNotes = [ProgressNote()] };

    public static NexaAgentTask TaskWithBlocker() =>
        ValidTask(status: NexaAgentTaskStatus.Blocked) with { Blockers = [Blocker()] };

    public static NexaAgentTask TaskWithVerificationCheck() =>
        ValidTask(status: NexaAgentTaskStatus.InReview) with { VerificationChecks = [Verification()] };

    public static NexaProgressNote ProgressNote(
        string noteId = "note-agent-ops-001",
        string taskId = "task-agent-ops-001") =>
        new()
        {
            NoteId = noteId,
            TaskId = taskId,
            Author = "codex",
            Summary = "Progress note fixture.",
            Detail = "No runtime action.",
            EvidenceRefs = [Evidence("evidence-note")],
            CreatedAt = FixedTimestamp
        };

    public static NexaBlockerReport Blocker(
        string blockerId = "blocker-agent-ops-001",
        string taskId = "task-agent-ops-001",
        NexaBlockerSeverity severity = NexaBlockerSeverity.Warning) =>
        new()
        {
            BlockerId = blockerId,
            TaskId = taskId,
            Kind = NexaBlockerKind.TestFailure,
            Summary = "Blocker fixture.",
            Detail = "Controlled blocker.",
            Severity = severity,
            SuggestedResolution = NexaBlockerResolutionMode.StopWithEvidence,
            EvidenceRefs = [Evidence("evidence-blocker")],
            CreatedAt = FixedTimestamp
        };

    public static NexaVerificationCheck Verification(
        string checkId = "verification-agent-ops-001",
        string taskId = "task-agent-ops-001",
        NexaVerificationStatus status = NexaVerificationStatus.Passed,
        bool required = true,
        string? detail = "Verification fixture.",
        IReadOnlyList<NexaEvidenceRef>? evidenceRefs = null) =>
        new()
        {
            CheckId = checkId,
            TaskId = taskId,
            Label = "Verification fixture",
            Status = status,
            Required = required,
            Detail = detail,
            EvidenceRefs = evidenceRefs ?? Array.Empty<NexaEvidenceRef>()
        };

    public static NexaEvidenceRef Evidence(string evidenceId = "evidence-agent-ops-001") =>
        new()
        {
            EvidenceId = evidenceId,
            Kind = "test-report",
            Ref = $"artifact://agent-operations/{evidenceId}",
            Hash = "sha256:fixture",
            CreatedAt = FixedTimestamp
        };
}
