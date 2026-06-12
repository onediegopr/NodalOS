using OneBrain.Core.Approval;
using OneBrain.Core.History;
using OneBrain.Core.Recording;

namespace OneBrain.Core.Flows;

public static class SupervisedPlaybackService
{
    public static SupervisedPlaybackSession Start(PromotedCandidateFlow flow, DateTimeOffset? startedAtUtc = null)
    {
        var timestamp = (startedAtUtc ?? DateTimeOffset.UtcNow).UtcDateTime.ToString("o");
        return new SupervisedPlaybackSession(
            PlaybackId: $"playback-{Guid.NewGuid():N}",
            FlowId: flow.FlowId,
            StartedAtUtc: timestamp,
            EndedAtUtc: null,
            Status: SupervisedPlaybackStatuses.Running,
            CurrentStepNumber: flow.Steps.Count == 0 ? 0 : flow.Steps[0].StepNumber,
            Steps: flow.Steps.Select(step => new SupervisedPlaybackStepState(
                StepNumber: step.StepNumber,
                Status: SupervisedPlaybackStepStatuses.Pending,
                Decision: "",
                EvidenceSummary: "",
                ApprovalRequestId: null,
                ApprovalDecisionId: null,
                UpdatedAtUtc: timestamp,
                Notes: [])).ToList(),
            SafetyCounters: RunSafetyCounters.Zero,
            ArtifactPaths: [],
            Notes:
            [
                "supervised playback v0",
                "no autonomous playback",
                "no clicks, login, cookies, cart, purchase, or payment"
            ]);
    }

    public static SupervisedPlaybackActionResult ConfirmStep(
        PromotedCandidateFlow flow,
        SupervisedPlaybackSession session,
        int stepNumber,
        ApprovalDecision? approvalDecision = null,
        DateTimeOffset? now = null)
    {
        var step = FindStep(flow, stepNumber);
        if (step == null)
            return BuildResult(flow, session, false, $"step {stepNumber} not found", [], RunHistoryStatuses.Blocked, "step not found", now);

        if (session.Status is SupervisedPlaybackStatuses.Aborted or SupervisedPlaybackStatuses.Succeeded)
            return BuildResult(flow, session, false, "playback is not running", [], RunHistoryStatuses.Blocked, "playback not running", now);

        if (step.RequiresApproval && approvalDecision?.Decision != ApprovalDecisionKinds.Approved)
        {
            var blocked = UpdateStep(session, stepNumber, SupervisedPlaybackStepStatuses.Blocked, "approval required", step, approvalDecision, now);
            blocked = blocked with { Status = SupervisedPlaybackStatuses.Blocked, EndedAtUtc = Timestamp(now) };
            return BuildResult(flow, blocked, false, "approval required before this step can continue", EvidenceFor(step, "blocked before action"), RunHistoryStatuses.Blocked, "approval required", now);
        }

        if (!step.HasSafeExecutor && step.ExecutionMode != CandidateFlowStepExecutionModes.FixtureOnly)
        {
            var blocked = UpdateStep(session, stepNumber, SupervisedPlaybackStepStatuses.Blocked, "no safe executor", step, approvalDecision, now);
            blocked = blocked with { Status = SupervisedPlaybackStatuses.Blocked, EndedAtUtc = Timestamp(now) };
            return BuildResult(flow, blocked, false, "no safe executor exists for this action in v0", EvidenceFor(step, "blocked: no safe executor"), RunHistoryStatuses.Blocked, "no safe executor", now);
        }

        var updated = UpdateStep(session, stepNumber, SupervisedPlaybackStepStatuses.Confirmed, "human confirmed", step, approvalDecision, now);
        updated = Advance(flow, updated, now);
        return BuildResult(flow, updated, true, "step confirmed under supervision", EvidenceFor(step, "confirmed without real UI action"), updated.Status == SupervisedPlaybackStatuses.Succeeded ? RunHistoryStatuses.Succeeded : RunHistoryStatuses.Running, "", now);
    }

    public static SupervisedPlaybackActionResult SkipStep(PromotedCandidateFlow flow, SupervisedPlaybackSession session, int stepNumber, DateTimeOffset? now = null)
    {
        var step = FindStep(flow, stepNumber);
        if (step == null)
            return BuildResult(flow, session, false, $"step {stepNumber} not found", [], RunHistoryStatuses.Blocked, "step not found", now);

        if (!step.CanSkip)
        {
            var blocked = UpdateStep(session, stepNumber, SupervisedPlaybackStepStatuses.Blocked, "skip not allowed", step, null, now);
            blocked = blocked with { Status = SupervisedPlaybackStatuses.Blocked, EndedAtUtc = Timestamp(now) };
            return BuildResult(flow, blocked, false, "skip not allowed by policy", EvidenceFor(step, "blocked: skip not allowed"), RunHistoryStatuses.Blocked, "skip not allowed", now);
        }

        var updated = UpdateStep(session, stepNumber, SupervisedPlaybackStepStatuses.Skipped, "human skipped", step, null, now);
        updated = Advance(flow, updated, now);
        return BuildResult(flow, updated, true, "step skipped under supervision", EvidenceFor(step, "skipped by human"), updated.Status == SupervisedPlaybackStatuses.Succeeded ? RunHistoryStatuses.Succeeded : RunHistoryStatuses.Running, "", now);
    }

    public static SupervisedPlaybackActionResult Abort(PromotedCandidateFlow flow, SupervisedPlaybackSession session, string reason, DateTimeOffset? now = null)
    {
        var timestamp = Timestamp(now);
        var updatedSteps = session.Steps.Select(step => step.Status == SupervisedPlaybackStepStatuses.Pending
            ? step with
            {
                Status = SupervisedPlaybackStepStatuses.Aborted,
                Decision = "aborted",
                EvidenceSummary = SensitiveTextSanitizer.Sanitize(reason),
                UpdatedAtUtc = timestamp,
                Notes = ["human aborted playback"]
            }
            : step).ToList();

        var updated = session with
        {
            Status = SupervisedPlaybackStatuses.Aborted,
            EndedAtUtc = timestamp,
            Steps = updatedSteps,
            Notes = session.Notes.Concat(["aborted by human"]).ToList()
        };

        return BuildResult(flow, updated, true, "playback aborted by human", [SensitiveTextSanitizer.Sanitize(reason)], RunHistoryStatuses.Blocked, "aborted by human", now);
    }

    private static SupervisedPlaybackSession UpdateStep(
        SupervisedPlaybackSession session,
        int stepNumber,
        string status,
        string decision,
        PromotedFlowStep step,
        ApprovalDecision? approvalDecision,
        DateTimeOffset? now)
    {
        var timestamp = Timestamp(now);
        var updatedSteps = session.Steps.Select(item => item.StepNumber == stepNumber
            ? item with
            {
                Status = status,
                Decision = decision,
                EvidenceSummary = string.Join("; ", EvidenceFor(step, decision)),
                ApprovalDecisionId = approvalDecision?.ApprovalDecisionId,
                ApprovalRequestId = approvalDecision?.ApprovalRequestId,
                UpdatedAtUtc = timestamp,
                Notes = step.Notes.Concat(["no real UI action executed"]).ToList()
            }
            : item).ToList();

        return session with
        {
            Steps = updatedSteps,
            ArtifactPaths = session.ArtifactPaths.Concat([$"artifacts/supervised-playback/{session.PlaybackId}-step-{stepNumber:00}.json"]).Distinct(StringComparer.OrdinalIgnoreCase).ToList()
        };
    }

    private static SupervisedPlaybackSession Advance(PromotedCandidateFlow flow, SupervisedPlaybackSession session, DateTimeOffset? now)
    {
        var next = flow.Steps
            .Select(step => step.StepNumber)
            .FirstOrDefault(stepNumber =>
                session.Steps.First(state => state.StepNumber == stepNumber).Status == SupervisedPlaybackStepStatuses.Pending);

        if (next == 0)
        {
            return session with
            {
                Status = SupervisedPlaybackStatuses.Succeeded,
                CurrentStepNumber = 0,
                EndedAtUtc = Timestamp(now)
            };
        }

        return session with
        {
            Status = SupervisedPlaybackStatuses.Running,
            CurrentStepNumber = next
        };
    }

    private static PromotedFlowStep? FindStep(PromotedCandidateFlow flow, int stepNumber)
    {
        return flow.Steps.FirstOrDefault(step => step.StepNumber == stepNumber);
    }

    private static IReadOnlyList<string> EvidenceFor(PromotedFlowStep step, string eventLabel)
    {
        return step.EvidenceLabels
            .Concat(
            [
                $"step={step.StepNumber}",
                $"event={SensitiveTextSanitizer.Sanitize(eventLabel)}",
                "execution=supervised_v0_no_real_action"
            ])
            .ToList();
    }

    private static SupervisedPlaybackActionResult BuildResult(
        PromotedCandidateFlow flow,
        SupervisedPlaybackSession session,
        bool success,
        string message,
        IReadOnlyList<string> evidence,
        string runStatus,
        string errorSummary,
        DateTimeOffset? now)
    {
        var timestamp = Timestamp(now);
        var record = new RunHistoryRecord(
            RunId: $"run-{session.PlaybackId}",
            StartedAtUtc: session.StartedAtUtc,
            EndedAtUtc: session.EndedAtUtc ?? timestamp,
            Status: runStatus,
            Source: RunHistorySources.Pilot,
            RecipeId: null,
            CandidateFlowId: flow.CandidateFlowId,
            ApprovalRequestId: session.Steps.Select(step => step.ApprovalRequestId).FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)),
            ApprovalDecisionId: session.Steps.Select(step => step.ApprovalDecisionId).FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)),
            RecordingSessionId: null,
            TimelineId: flow.TimelineId,
            ConfidenceId: null,
            AiRoutingDecisionId: null,
            ExitCode: null,
            SafetyCounters: session.SafetyCounters,
            ArtifactPaths: session.ArtifactPaths,
            ErrorSummary: SensitiveTextSanitizer.Sanitize(errorSummary),
            Notes:
            [
                "supervised playback v0",
                SensitiveTextSanitizer.Sanitize(message),
                "0 clicks, 0 cookies, 0 login, 0 cart, 0 purchase, 0 payment"
            ]);

        return new SupervisedPlaybackActionResult(
            Success: success,
            Session: session,
            Message: SensitiveTextSanitizer.Sanitize(message),
            Evidence: evidence.Select(SensitiveTextSanitizer.Sanitize).ToList(),
            RunHistory: record);
    }

    private static string Timestamp(DateTimeOffset? now)
    {
        return (now ?? DateTimeOffset.UtcNow).UtcDateTime.ToString("o");
    }
}
