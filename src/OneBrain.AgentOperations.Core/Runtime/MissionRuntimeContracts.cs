using OneBrain.Core.Runtime;

namespace OneBrain.AgentOperations.Core.Runtime;

public enum MissionExecutionSurface
{
    Reasoning,
    Coding,
    Filesystem,
    Desktop,
    BrowserDom,
    Terminal,
    Human
}

public enum MissionRiskLevel
{
    Low,
    Medium,
    High
}

public enum MissionStepStatus
{
    Pending,
    InProgress,
    Blocked,
    ReadyForVerification,
    Verified,
    Failed,
    Skipped,
    Cancelled
}

public enum MissionStatus
{
    Active,
    Blocked,
    ReadyForVerification,
    Completed,
    Failed,
    Cancelled,
    TimedOut
}

public enum MissionReconcilerDecision
{
    Continue,
    Replan,
    Blocked,
    NeedsHumanIntervention,
    ReadyForVerification,
    Failed
}

public enum MissionEventKind
{
    RunStarted,
    PlanCreated,
    StepStarted,
    ToolCallStarted,
    ToolCallCompleted,
    FallbackApplied,
    CapabilityDegraded,
    ApprovalRequired,
    ApprovalResolved,
    StepBlocked,
    StepFailed,
    StepReadyForVerification,
    StepVerified,
    RunCompleted,
    RunBlocked,
    RunFailed,
    RunCancelled,
    RunTimeout
}

public enum MissionEventSeverity
{
    Info,
    Warning,
    Error
}

public enum MissionStepAuthorizationDecision
{
    AllowedWithinMission,
    ApprovalRequired,
    BlockedScopeExpansion
}

public sealed record MissionExpectedEvidence(string Kind, string Description);

public sealed record MissionStep(
    string Id,
    string? ParentId,
    string Intent,
    MissionExecutionSurface ExecutionSurface,
    IReadOnlyList<string> AllowedCapabilities,
    IReadOnlyList<MissionExpectedEvidence> ExpectedEvidence,
    MissionRiskLevel RiskLevel,
    bool ApprovalRequired,
    IReadOnlyList<string> Dependencies,
    MissionStepStatus Status,
    int Attempts,
    string? LastFailure,
    IReadOnlyList<string> EvidenceRefs);

public sealed record MissionPlan(
    string MissionId,
    int Version,
    DateTimeOffset CreatedAt,
    string Goal,
    IReadOnlyList<MissionStep> Steps,
    MissionStatus Status);

public sealed record MissionAuthorizationScope(
    string MissionId,
    IReadOnlySet<string> AllowedCapabilities,
    IReadOnlySet<MissionExecutionSurface> AllowedSurfaces,
    MissionRiskLevel MaximumRiskWithoutAdditionalApproval,
    DateTimeOffset? ExpiresAt = null)
{
    public MissionStepAuthorizationDecision Evaluate(MissionStep step, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(step);
        if (!string.Equals(step.Id, step.Id.Trim(), StringComparison.Ordinal))
            return MissionStepAuthorizationDecision.BlockedScopeExpansion;
        if (ExpiresAt is { } expiresAt && expiresAt <= now)
            return MissionStepAuthorizationDecision.ApprovalRequired;
        if (!AllowedSurfaces.Contains(step.ExecutionSurface))
            return MissionStepAuthorizationDecision.BlockedScopeExpansion;
        if (step.AllowedCapabilities.Any(capability => !AllowedCapabilities.Contains(capability)))
            return MissionStepAuthorizationDecision.BlockedScopeExpansion;
        if (step.ApprovalRequired || (int)step.RiskLevel > (int)MaximumRiskWithoutAdditionalApproval)
            return MissionStepAuthorizationDecision.ApprovalRequired;
        return MissionStepAuthorizationDecision.AllowedWithinMission;
    }
}

public sealed record MissionEventEnvelope(
    string RunId,
    long Sequence,
    DateTimeOffset Timestamp,
    MissionEventKind Kind,
    string Actor,
    string MissionId,
    string? StepId,
    string CorrelationId,
    string? CausationId,
    string Summary,
    IReadOnlyList<string> EvidenceRefs,
    MissionEventSeverity Severity)
{
    public MissionEventEnvelope Sanitize() => this with
    {
        Actor = SafeRuntimeText.Sanitize(Actor, 80),
        MissionId = SafeRuntimeText.Sanitize(MissionId, 120),
        StepId = string.IsNullOrWhiteSpace(StepId) ? null : SafeRuntimeText.Sanitize(StepId, 120),
        CorrelationId = SafeRuntimeText.Sanitize(CorrelationId, 120),
        CausationId = string.IsNullOrWhiteSpace(CausationId) ? null : SafeRuntimeText.Sanitize(CausationId, 120),
        Summary = SafeRuntimeText.Sanitize(Summary),
        EvidenceRefs = EvidenceRefs
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => SafeRuntimeText.Sanitize(value, 160))
            .Distinct(StringComparer.Ordinal)
            .Take(12)
            .ToArray()
    };
}

public sealed record MissionStepRuntimeState(
    string StepId,
    MissionStepStatus Status,
    int Attempts,
    string? LastFailure,
    IReadOnlyList<string> EvidenceRefs);

public sealed record MissionRuntimeState(
    string RunId,
    string MissionId,
    MissionStatus Status,
    string? CurrentStepId,
    IReadOnlyDictionary<string, MissionStepRuntimeState> Steps,
    IReadOnlyList<string> Blockers,
    IReadOnlyList<string> EvidenceRefs,
    MissionEventEnvelope? LastEvent,
    string? RecentFallback,
    int VerifiedStepCount,
    int TotalStepCount)
{
    public double Progress => TotalStepCount == 0 ? 0 : (double)VerifiedStepCount / TotalStepCount;
}

public sealed record MissionReconcilerResult(
    MissionReconcilerDecision Decision,
    string Reason,
    string? StepId = null);

public sealed record MissionResumeCard(
    string MissionId,
    string Goal,
    MissionStatus Status,
    double Progress,
    string? CurrentStep,
    string LastResult,
    string? Blocker,
    string? NextAction,
    string? RecentFallback,
    IReadOnlyList<string> EvidenceRefs);