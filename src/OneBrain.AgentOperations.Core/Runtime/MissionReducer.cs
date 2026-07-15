namespace OneBrain.AgentOperations.Core.Runtime;

public static class MissionReducer
{
    public static MissionRuntimeState Reduce(MissionPlan plan, IReadOnlyList<MissionEventEnvelope> events)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(events);
        ValidateSequences(events);

        var steps = plan.Steps.ToDictionary(
            step => step.Id,
            step => new MissionStepRuntimeState(step.Id, step.Status, step.Attempts, step.LastFailure, step.EvidenceRefs),
            StringComparer.Ordinal);
        var status = plan.Status;
        string? currentStepId = null;
        var blockers = new List<string>();
        var evidence = new List<string>();
        MissionEventEnvelope? last = null;
        string? recentFallback = null;
        var runId = events.FirstOrDefault()?.RunId ?? string.Empty;

        foreach (var envelope in events)
        {
            if (!string.Equals(envelope.MissionId, plan.MissionId, StringComparison.Ordinal))
                throw new InvalidOperationException("Event mission id does not match the plan.");
            if (runId.Length > 0 && !string.Equals(envelope.RunId, runId, StringComparison.Ordinal))
                throw new InvalidOperationException("Events from multiple runs cannot be reduced together.");

            last = envelope;
            MergeUnique(evidence, envelope.EvidenceRefs);

            switch (envelope.Kind)
            {
                case MissionEventKind.RunStarted:
                case MissionEventKind.PlanCreated:
                    status = MissionStatus.Active;
                    break;
                case MissionEventKind.StepStarted:
                    currentStepId = envelope.StepId;
                    UpdateStep(steps, envelope.StepId, state => state with
                    {
                        Status = MissionStepStatus.InProgress,
                        Attempts = state.Attempts + 1,
                        LastFailure = null
                    });
                    status = MissionStatus.Active;
                    break;
                case MissionEventKind.StepBlocked:
                    currentStepId = envelope.StepId;
                    AddUnique(blockers, envelope.Summary);
                    UpdateStep(steps, envelope.StepId, state => state with
                    {
                        Status = MissionStepStatus.Blocked,
                        LastFailure = envelope.Summary
                    });
                    status = MissionStatus.Blocked;
                    break;
                case MissionEventKind.RunBlocked:
                    AddUnique(blockers, envelope.Summary);
                    status = MissionStatus.Blocked;
                    break;
                case MissionEventKind.StepFailed:
                    currentStepId = envelope.StepId;
                    UpdateStep(steps, envelope.StepId, state => state with
                    {
                        Status = MissionStepStatus.Failed,
                        LastFailure = envelope.Summary
                    });
                    status = MissionStatus.Active;
                    break;
                case MissionEventKind.StepReadyForVerification:
                    currentStepId = envelope.StepId;
                    UpdateStep(steps, envelope.StepId, state => state with
                    {
                        Status = MissionStepStatus.ReadyForVerification,
                        EvidenceRefs = MergeEvidence(state.EvidenceRefs, envelope.EvidenceRefs)
                    });
                    status = MissionStatus.ReadyForVerification;
                    break;
                case MissionEventKind.StepVerified:
                    UpdateStep(steps, envelope.StepId, state => state with
                    {
                        Status = MissionStepStatus.Verified,
                        EvidenceRefs = MergeEvidence(state.EvidenceRefs, envelope.EvidenceRefs),
                        LastFailure = null
                    });
                    currentStepId = FindNextStep(plan, steps);
                    status = currentStepId is null ? MissionStatus.ReadyForVerification : MissionStatus.Active;
                    blockers.Clear();
                    break;
                case MissionEventKind.RunCompleted:
                    if (steps.Values.Any(step => step.Status is not (MissionStepStatus.Verified or MissionStepStatus.Skipped)))
                        throw new InvalidOperationException("A run cannot complete before every required step is verified.");
                    status = MissionStatus.Completed;
                    currentStepId = null;
                    blockers.Clear();
                    break;
                case MissionEventKind.RunFailed:
                    status = MissionStatus.Failed;
                    AddUnique(blockers, envelope.Summary);
                    break;
                case MissionEventKind.RunCancelled:
                    status = MissionStatus.Cancelled;
                    break;
                case MissionEventKind.RunTimeout:
                    status = MissionStatus.TimedOut;
                    AddUnique(blockers, envelope.Summary);
                    break;
                case MissionEventKind.ApprovalRequired:
                    currentStepId = envelope.StepId;
                    status = MissionStatus.Blocked;
                    AddUnique(blockers, envelope.Summary);
                    break;
                case MissionEventKind.ApprovalResolved:
                    blockers.RemoveAll(value => value.Contains("approval", StringComparison.OrdinalIgnoreCase));
                    status = MissionStatus.Active;
                    break;
                case MissionEventKind.FallbackApplied:
                    recentFallback = envelope.Summary;
                    break;
            }
        }

        var verified = steps.Values.Count(step => step.Status is MissionStepStatus.Verified or MissionStepStatus.Skipped);
        return new MissionRuntimeState(
            runId,
            plan.MissionId,
            status,
            currentStepId,
            steps,
            blockers,
            evidence,
            last,
            recentFallback,
            verified,
            steps.Count);
    }

    public static MissionReconcilerResult Reconcile(MissionPlan plan, MissionRuntimeState state)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(state);

        if (state.Status is MissionStatus.Failed or MissionStatus.Cancelled or MissionStatus.TimedOut)
            return new MissionReconcilerResult(MissionReconcilerDecision.Failed, state.LastEvent?.Summary ?? "Mission is terminal.");
        if (state.Status == MissionStatus.ReadyForVerification)
            return new MissionReconcilerResult(
                MissionReconcilerDecision.ReadyForVerification,
                "The current step has evidence and requires verification.",
                state.CurrentStepId);
        if (state.Status == MissionStatus.Blocked)
        {
            var step = state.CurrentStepId is null
                ? null
                : plan.Steps.FirstOrDefault(value => string.Equals(value.Id, state.CurrentStepId, StringComparison.Ordinal));
            return step?.ExecutionSurface == MissionExecutionSurface.Human || step?.RiskLevel == MissionRiskLevel.High
                ? new MissionReconcilerResult(
                    MissionReconcilerDecision.NeedsHumanIntervention,
                    state.Blockers.FirstOrDefault() ?? "Human intervention is required.",
                    state.CurrentStepId)
                : new MissionReconcilerResult(
                    MissionReconcilerDecision.Blocked,
                    state.Blockers.FirstOrDefault() ?? "The mission is blocked.",
                    state.CurrentStepId);
        }

        if (state.CurrentStepId is { } currentId && state.Steps.TryGetValue(currentId, out var current) &&
            current.Status == MissionStepStatus.Failed)
        {
            return current.Attempts < 3
                ? new MissionReconcilerResult(MissionReconcilerDecision.Replan, current.LastFailure ?? "The step failed.", currentId)
                : new MissionReconcilerResult(MissionReconcilerDecision.Failed, current.LastFailure ?? "Retry limit reached.", currentId);
        }

        return new MissionReconcilerResult(
            MissionReconcilerDecision.Continue,
            "Continue within the authorized mission scope.",
            state.CurrentStepId);
    }

    public static MissionResumeCard BuildResumeCard(
        MissionPlan plan,
        MissionRuntimeState state,
        CompactMissionMemory? memory)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(state);
        return new MissionResumeCard(
            plan.MissionId,
            plan.Goal,
            state.Status,
            state.Progress,
            state.CurrentStepId ?? memory?.CurrentStep,
            state.LastEvent?.Summary ?? "Mission has not started.",
            state.Blockers.FirstOrDefault() ?? memory?.Blockers.FirstOrDefault(),
            memory?.NextStep ?? FindNextStep(plan, state.Steps),
            state.RecentFallback,
            state.EvidenceRefs.TakeLast(6).ToArray());
    }

    private static void ValidateSequences(IReadOnlyList<MissionEventEnvelope> events)
    {
        long expected = 1;
        foreach (var envelope in events)
        {
            if (envelope.Sequence != expected)
                throw new InvalidOperationException(
                    $"Mission event sequence must be monotonic. Expected {expected}, found {envelope.Sequence}.");
            expected++;
        }
    }

    private static void UpdateStep(
        IDictionary<string, MissionStepRuntimeState> steps,
        string? stepId,
        Func<MissionStepRuntimeState, MissionStepRuntimeState> update)
    {
        if (string.IsNullOrWhiteSpace(stepId) || !steps.TryGetValue(stepId, out var existing))
            throw new InvalidOperationException("Mission event references an unknown step.");
        steps[stepId] = update(existing);
    }

    private static string? FindNextStep(MissionPlan plan, IReadOnlyDictionary<string, MissionStepRuntimeState> steps)
    {
        foreach (var step in plan.Steps)
        {
            if (!steps.TryGetValue(step.Id, out var state) || state.Status is MissionStepStatus.Verified or MissionStepStatus.Skipped)
                continue;
            var dependenciesSatisfied = step.Dependencies.All(dependency =>
                steps.TryGetValue(dependency, out var dependencyState) &&
                dependencyState.Status is MissionStepStatus.Verified or MissionStepStatus.Skipped);
            if (dependenciesSatisfied)
                return step.Id;
        }
        return null;
    }

    private static IReadOnlyList<string> MergeEvidence(IReadOnlyList<string> existing, IReadOnlyList<string> next) =>
        existing.Concat(next).Distinct(StringComparer.Ordinal).TakeLast(12).ToArray();

    private static void MergeUnique(ICollection<string> destination, IEnumerable<string> values)
    {
        foreach (var value in values)
            AddUnique(destination, value);
    }

    private static void AddUnique(ICollection<string> values, string value)
    {
        if (!values.Any(existing => string.Equals(existing, value, StringComparison.Ordinal)))
            values.Add(value);
    }
}
