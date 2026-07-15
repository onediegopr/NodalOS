using OneBrain.Core.Runtime;

namespace OneBrain.AgentOperations.Core.Runtime;

public sealed class LightweightMissionRuntime
{
    private readonly MissionPlan _plan;
    private readonly MissionAuthorizationScope _authorization;
    private readonly MissionEventStream _events;
    private readonly HashSet<string> _approvedSteps = new(StringComparer.Ordinal);
    private readonly HashSet<string> _pendingApprovals = new(StringComparer.Ordinal);
    private CompactMissionMemory _memory;

    public LightweightMissionRuntime(
        MissionPlan plan,
        MissionAuthorizationScope authorization,
        string runId,
        IRuntimeSignalObserver? observer = null)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(authorization);
        if (!string.Equals(plan.MissionId, authorization.MissionId, StringComparison.Ordinal))
            throw new InvalidOperationException("Mission authorization scope must match the plan mission id.");
        ValidatePlan(plan);
        _plan = plan;
        _authorization = authorization;
        _events = new MissionEventStream(runId, plan.MissionId, observer);
        _memory = CompactMissionMemoryProjector.Empty("runtime-created");
    }

    public MissionPlan Plan => _plan;

    public CompactMissionMemory Memory => _memory;

    public IReadOnlyList<MissionEventEnvelope> Events => _events.Snapshot();

    public MissionRuntimeState State => MissionReducer.Reduce(_plan, Events);

    public MissionResumeCard ResumeCard => MissionReducer.BuildResumeCard(_plan, State, _memory);

    public void Start(string correlationId, string actor = "runtime")
    {
        if (Events.Count != 0)
            throw new InvalidOperationException("Mission runtime has already started.");
        var started = _events.Append(
            MissionEventKind.RunStarted,
            actor,
            correlationId,
            "Mission started within the authorized scope.");
        _events.Append(
            MissionEventKind.PlanCreated,
            actor,
            correlationId,
            "Mission plan loaded as guidance; completion still requires verification.",
            causationId: EventId(started));
        var first = FindFirstRunnableStep();
        _memory = CompactMissionMemoryProjector.Apply(
            _memory,
            new CompactMissionMemoryUpdate(
                Goal: _plan.Goal,
                CurrentStep: first?.Intent,
                NextStep: first?.Intent,
                CompletionCriteria: _plan.Steps
                    .SelectMany(step => step.ExpectedEvidence.Select(evidence => evidence.Description))
                    .ToArray()),
            EventId(started));
    }

    public MissionStepAuthorizationDecision EvaluateStepAuthorization(string stepId, DateTimeOffset? now = null)
    {
        if (_approvedSteps.Contains(stepId))
            return MissionStepAuthorizationDecision.AllowedWithinMission;
        return _authorization.Evaluate(GetStep(stepId), now ?? DateTimeOffset.UtcNow);
    }

    public void BeginStep(string stepId, string correlationId, string actor = "runtime")
    {
        EnsureNotTerminal();
        var step = GetStep(stepId);
        EnsureDependenciesVerified(step);
        var decision = EvaluateStepAuthorization(stepId);
        if (decision == MissionStepAuthorizationDecision.BlockedScopeExpansion)
        {
            var envelope = _events.Append(
                MissionEventKind.StepBlocked,
                actor,
                correlationId,
                "Step exceeds the authorized mission capability or surface scope.",
                stepId,
                severity: MissionEventSeverity.Warning);
            UpdateBlockedMemory(step, "Authorized mission scope does not include this step.", envelope);
            return;
        }

        if (decision == MissionStepAuthorizationDecision.ApprovalRequired)
        {
            if (_pendingApprovals.Add(stepId))
            {
                var envelope = _events.Append(
                    MissionEventKind.ApprovalRequired,
                    actor,
                    correlationId,
                    "Additional approval is required because risk or scope materially changed.",
                    stepId,
                    severity: MissionEventSeverity.Warning);
                UpdateBlockedMemory(step, "Additional approval is required.", envelope);
            }
            return;
        }

        var started = _events.Append(
            MissionEventKind.StepStarted,
            actor,
            correlationId,
            $"Step started: {step.Intent}",
            stepId);
        _memory = CompactMissionMemoryProjector.Apply(
            _memory,
            new CompactMissionMemoryUpdate(
                Status: CompactMissionMemoryStatus.Active,
                CurrentStep: step.Intent,
                NextStep: step.Intent,
                ClearBlockers: true,
                ClearLastFailure: true),
            EventId(started));
    }

    public void ResolveApproval(string stepId, bool approved, string correlationId, string actor = "operator")
    {
        EnsureNotTerminal();
        var step = GetStep(stepId);
        _pendingApprovals.Remove(stepId);
        if (!approved)
        {
            var blocked = _events.Append(
                MissionEventKind.StepBlocked,
                actor,
                correlationId,
                "Approval was not granted.",
                stepId,
                severity: MissionEventSeverity.Warning);
            UpdateBlockedMemory(step, "Approval was not granted.", blocked);
            return;
        }

        _approvedSteps.Add(stepId);
        var resolved = _events.Append(
            MissionEventKind.ApprovalResolved,
            actor,
            correlationId,
            "Required approval was granted for this step.",
            stepId);
        _memory = CompactMissionMemoryProjector.Apply(
            _memory,
            new CompactMissionMemoryUpdate(
                Status: CompactMissionMemoryStatus.Active,
                NextStep: step.Intent,
                ClearBlockers: true,
                ClearLastFailure: true),
            EventId(resolved));
    }

    public void RecordToolCallStarted(string stepId, string capabilityId, string correlationId)
    {
        EnsureStepInProgress(stepId);
        _events.Append(
            MissionEventKind.ToolCallStarted,
            "runtime",
            correlationId,
            $"Capability started: {capabilityId}",
            stepId);
    }

    public void RecordToolCallCompleted(
        string stepId,
        string capabilityId,
        string correlationId,
        IEnumerable<string>? evidenceRefs = null)
    {
        EnsureStepInProgress(stepId);
        var refs = (evidenceRefs ?? Array.Empty<string>()).Where(value => !string.IsNullOrWhiteSpace(value)).ToArray();
        var completed = _events.Append(
            MissionEventKind.ToolCallCompleted,
            "runtime",
            correlationId,
            $"Capability completed: {capabilityId}",
            stepId,
            evidenceRefs: refs);
        _memory = CompactMissionMemoryProjector.Apply(
            _memory,
            new CompactMissionMemoryUpdate(
                ConfirmedFacts: [$"Capability completed: {capabilityId}"],
                Artifacts: refs.Select(reference => new CompactMissionArtifact("evidence", capabilityId, reference)).ToArray()),
            EventId(completed));
    }

    public void RecordFallback(string stepId, string summary, string correlationId, string actor = "runtime")
    {
        EnsureStepInProgress(stepId);
        _events.Append(
            MissionEventKind.FallbackApplied,
            actor,
            correlationId,
            summary,
            stepId,
            severity: MissionEventSeverity.Warning);
    }

    public void RecordCapabilityDegraded(string stepId, string capabilityId, string correlationId)
    {
        EnsureStepInProgress(stepId);
        _events.Append(
            MissionEventKind.CapabilityDegraded,
            "runtime",
            correlationId,
            $"Capability degraded: {capabilityId}",
            stepId,
            severity: MissionEventSeverity.Warning);
    }

    public void MarkReadyForVerification(string stepId, string correlationId, IEnumerable<string> evidenceRefs)
    {
        EnsureStepInProgress(stepId);
        var evidence = evidenceRefs.Where(value => !string.IsNullOrWhiteSpace(value)).ToArray();
        if (evidence.Length == 0)
            throw new InvalidOperationException("A step cannot become ready for verification without evidence references.");
        var ready = _events.Append(
            MissionEventKind.StepReadyForVerification,
            "runtime",
            correlationId,
            "Step produced evidence and is ready for verification.",
            stepId,
            evidenceRefs: evidence);
        _memory = CompactMissionMemoryProjector.Apply(
            _memory,
            new CompactMissionMemoryUpdate(
                Artifacts: evidence.Select(reference => new CompactMissionArtifact("evidence", stepId, reference)).ToArray(),
                NextStep: "Verify current step"),
            EventId(ready));
    }

    public void VerifyStep(
        string stepId,
        bool passed,
        string correlationId,
        IEnumerable<string>? evidenceRefs = null,
        string? failureReason = null)
    {
        EnsureNotTerminal();
        var state = State;
        if (!state.Steps.TryGetValue(stepId, out var stepState) || stepState.Status != MissionStepStatus.ReadyForVerification)
            throw new InvalidOperationException("Step must be ready for verification before verification can run.");

        if (!passed)
        {
            FailStep(stepId, failureReason ?? "Verification failed.", correlationId);
            return;
        }

        var evidence = stepState.EvidenceRefs
            .Concat(evidenceRefs ?? Array.Empty<string>())
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        if (evidence.Length == 0)
            throw new InvalidOperationException("Verification cannot succeed without evidence.");

        var verified = _events.Append(
            MissionEventKind.StepVerified,
            "verification",
            correlationId,
            "Step verified from trusted evidence.",
            stepId,
            evidenceRefs: evidence);
        var next = FindFirstRunnableStep(State);
        _memory = CompactMissionMemoryProjector.Apply(
            _memory,
            new CompactMissionMemoryUpdate(
                Status: next is null ? CompactMissionMemoryStatus.Done : CompactMissionMemoryStatus.Active,
                ConfirmedFacts: [$"Verified step: {GetStep(stepId).Intent}"],
                CurrentStep: next?.Intent,
                NextStep: next?.Intent,
                ClearBlockers: true,
                ClearLastFailure: true,
                ClearNextStep: next is null),
            EventId(verified));

        if (next is null)
        {
            _events.Append(
                MissionEventKind.RunCompleted,
                "runtime",
                correlationId,
                "Mission completed after every required step was verified.",
                evidenceRefs: State.EvidenceRefs);
        }
    }

    public void FailStep(string stepId, string reason, string correlationId)
    {
        EnsureNotTerminal();
        var step = GetStep(stepId);
        var failed = _events.Append(
            MissionEventKind.StepFailed,
            "runtime",
            correlationId,
            reason,
            stepId,
            severity: MissionEventSeverity.Error);
        _memory = CompactMissionMemoryProjector.Apply(
            _memory,
            new CompactMissionMemoryUpdate(
                Status: CompactMissionMemoryStatus.Blocked,
                CurrentStep: step.Intent,
                NextStep: step.Intent,
                LastFailureReason: reason,
                Blockers: [reason]),
            EventId(failed));
    }

    public void Cancel(string correlationId, string reason = "Mission cancelled by operator.")
    {
        EnsureNotTerminal();
        var cancelled = _events.Append(
            MissionEventKind.RunCancelled,
            "operator",
            correlationId,
            reason,
            severity: MissionEventSeverity.Warning);
        _memory = CompactMissionMemoryProjector.Apply(
            _memory,
            new CompactMissionMemoryUpdate(
                Status: CompactMissionMemoryStatus.Blocked,
                Blockers: [reason],
                ClearNextStep: true),
            EventId(cancelled));
    }

    public void Timeout(string correlationId, string reason = "Mission runtime timeout reached.")
    {
        EnsureNotTerminal();
        var timeout = _events.Append(
            MissionEventKind.RunTimeout,
            "runtime",
            correlationId,
            reason,
            severity: MissionEventSeverity.Error);
        _memory = CompactMissionMemoryProjector.Apply(
            _memory,
            new CompactMissionMemoryUpdate(
                Status: CompactMissionMemoryStatus.Blocked,
                Blockers: [reason],
                LastFailureReason: reason,
                ClearNextStep: true),
            EventId(timeout));
    }

    private MissionStep GetStep(string stepId) =>
        _plan.Steps.FirstOrDefault(step => string.Equals(step.Id, stepId, StringComparison.Ordinal))
        ?? throw new KeyNotFoundException($"Unknown mission step '{stepId}'.");

    private void EnsureDependenciesVerified(MissionStep step)
    {
        var state = State;
        var missing = step.Dependencies.Where(dependency =>
            !state.Steps.TryGetValue(dependency, out var dependencyState) ||
            dependencyState.Status is not (MissionStepStatus.Verified or MissionStepStatus.Skipped)).ToArray();
        if (missing.Length > 0)
            throw new InvalidOperationException($"Step dependencies are not verified: {string.Join(",", missing)}");
    }

    private void EnsureStepInProgress(string stepId)
    {
        EnsureNotTerminal();
        var state = State;
        if (!state.Steps.TryGetValue(stepId, out var step) || step.Status != MissionStepStatus.InProgress)
            throw new InvalidOperationException("The mission step is not in progress.");
    }

    private void EnsureNotTerminal()
    {
        if (State.Status is MissionStatus.Completed or MissionStatus.Failed or MissionStatus.Cancelled or MissionStatus.TimedOut)
            throw new InvalidOperationException("The mission runtime is terminal.");
    }

    private MissionStep? FindFirstRunnableStep() => FindFirstRunnableStep(State);

    private MissionStep? FindFirstRunnableStep(MissionRuntimeState state) =>
        _plan.Steps.FirstOrDefault(step =>
            state.Steps.TryGetValue(step.Id, out var stepState) &&
            (stepState.Status is MissionStepStatus.Pending or MissionStepStatus.Failed or MissionStepStatus.Blocked) &&
            step.Dependencies.All(dependency =>
                state.Steps.TryGetValue(dependency, out var dependencyState) &&
                dependencyState.Status is MissionStepStatus.Verified or MissionStepStatus.Skipped));

    private void UpdateBlockedMemory(MissionStep step, string reason, MissionEventEnvelope envelope)
    {
        _memory = CompactMissionMemoryProjector.Apply(
            _memory,
            new CompactMissionMemoryUpdate(
                Status: CompactMissionMemoryStatus.Blocked,
                CurrentStep: step.Intent,
                NextStep: step.Intent,
                Blockers: [reason],
                LastFailureReason: reason),
            EventId(envelope));
    }

    private static string EventId(MissionEventEnvelope envelope) => $"{envelope.RunId}:{envelope.Sequence}";

    private static void ValidatePlan(MissionPlan plan)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(plan.MissionId);
        ArgumentException.ThrowIfNullOrWhiteSpace(plan.Goal);
        if (plan.Version < 1)
            throw new ArgumentOutOfRangeException(nameof(plan), "Mission plan version must be positive.");
        if (plan.Steps.Count == 0)
            throw new ArgumentException("Mission plan requires at least one step.", nameof(plan));

        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var step in plan.Steps)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(step.Id);
            ArgumentException.ThrowIfNullOrWhiteSpace(step.Intent);
            if (!ids.Add(step.Id))
                throw new ArgumentException($"Duplicate mission step id '{step.Id}'.", nameof(plan));
        }

        foreach (var step in plan.Steps)
        {
            if (step.Dependencies.Any(dependency => !ids.Contains(dependency)))
                throw new ArgumentException($"Step '{step.Id}' has an unknown dependency.", nameof(plan));
            if (step.Dependencies.Contains(step.Id, StringComparer.Ordinal))
                throw new ArgumentException($"Step '{step.Id}' cannot depend on itself.", nameof(plan));
        }

        DetectCycles(plan.Steps);
    }

    private static void DetectCycles(IReadOnlyList<MissionStep> steps)
    {
        var byId = steps.ToDictionary(step => step.Id, StringComparer.Ordinal);
        var visiting = new HashSet<string>(StringComparer.Ordinal);
        var visited = new HashSet<string>(StringComparer.Ordinal);

        bool Visit(string id)
        {
            if (visited.Contains(id))
                return false;
            if (!visiting.Add(id))
                return true;
            foreach (var dependency in byId[id].Dependencies)
            {
                if (Visit(dependency))
                    return true;
            }
            visiting.Remove(id);
            visited.Add(id);
            return false;
        }

        if (steps.Any(step => Visit(step.Id)))
            throw new ArgumentException("Mission plan contains a dependency cycle.", nameof(steps));
    }
}
