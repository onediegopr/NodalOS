using OneBrain.Core.Contracts;
using OneBrain.Core.Perception;
using OneBrain.Core.Verification;

namespace OneBrain.Core.Skills;

public enum ExecutableSkillState
{
    Observed,
    Candidate,
    Supervised,
    Verified,
    Degraded,
    Invalidated,
    Archived
}

public enum VerifiedSkillTransitionState
{
    Verified,
    Degraded,
    Invalidated
}

public enum SkillRecoveryKind
{
    ReobserveApplication,
    AlternateSelectorAlias,
    AlternatePerceptionChannel,
    RegroundRelevantRegion,
    HumanHandoff
}

public enum SkillPromotionDecision
{
    CreatedVerifiedSkill,
    AddedVerifiedTransition,
    ReverifiedExistingTransition,
    Rejected
}

public enum SkillFailureDecision
{
    IgnoredExternalInterruption,
    TransitionDegraded,
    TransitionInvalidated,
    SkillInvalidated,
    Rejected
}

public enum SkillRepairDecision
{
    Repaired,
    Rejected
}

public enum SkillReplayDecision
{
    Ready,
    SkillNotFound,
    SkillNotVerified,
    CurrentStateNotFound,
    TransitionDegraded,
    ProfileMismatch,
    ProfileVersionMismatch,
    CapabilityNotAuthorized,
    Ambiguous,
    Archived,
    Invalidated
}

public sealed record SkillParameterBinding(
    string Name,
    string ValueRef,
    bool SecretByReference = false,
    bool RawValuePresent = false);

public sealed record SkillRecoveryAlternative(
    string RecoveryId,
    SkillRecoveryKind Kind,
    string SummaryRedacted,
    string? SelectorAliasRef,
    IReadOnlyList<string> EvidenceRefs,
    bool RequiresOperatorDecision = false);

public sealed record SkillActionTemplate(
    string TemplateId,
    string CapabilityId,
    string Operation,
    string SemanticTargetRef,
    IReadOnlyList<SkillParameterBinding> Parameters,
    IReadOnlyList<string> SelectorAliasRefs,
    IReadOnlyList<SkillRecoveryAlternative> RecoveryAlternatives,
    string RiskLevel,
    bool RequiresExistingMissionAuthorization = true)
{
    public bool GrantsExecutionAuthority => false;
    public bool CanBypassPolicy => false;
}

public sealed record VerifiedSkillTransition(
    string TransitionId,
    string FromStateFingerprint,
    string ToStateFingerprint,
    SkillActionTemplate Action,
    string VerificationPlanId,
    int AppProfileVersion,
    VerifiedSkillTransitionState State,
    int SuccessfulRuns,
    int FailedRuns,
    SemanticVerificationFailureClass? LastFailureClass,
    FailureKind? LastFailureKind,
    DateTimeOffset LastVerifiedAtUtc,
    DateTimeOffset? LastFailureAtUtc,
    IReadOnlyList<string> EvidenceRefs,
    string? SupersededByTransitionId,
    string TransitionFingerprint)
{
    public bool ReplayEligible =>
        State == VerifiedSkillTransitionState.Verified &&
        string.IsNullOrWhiteSpace(SupersededByTransitionId);

    public bool GrantsExecutionAuthority => false;
}

public sealed record ExecutableSkill(
    string SkillId,
    int Version,
    string TitleRedacted,
    string AppProfileId,
    int AppProfileVersion,
    string? RecipeId,
    string? ProcessMemoryId,
    string? LastRunId,
    ExecutableSkillState State,
    IReadOnlySet<string> RequiredCapabilities,
    string RiskLevel,
    IReadOnlyList<VerifiedSkillTransition> Transitions,
    IReadOnlyList<string> EvidenceRefs,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    string SkillFingerprint)
{
    public bool ReplayEligible =>
        State is ExecutableSkillState.Verified or ExecutableSkillState.Degraded &&
        Transitions.Any(transition => transition.ReplayEligible);

    public bool LiveExecutionAuthorityGranted => false;
    public bool CanBypassPolicy => false;
    public bool CanExpandMissionScope => false;
}

public sealed record SkillCandidateRequest(
    string SkillId,
    string TitleRedacted,
    string AppProfileId,
    int AppProfileVersion,
    string? RecipeId,
    string? ProcessMemoryId,
    string? RunId,
    IReadOnlySet<string> RequiredCapabilities,
    string RiskLevel,
    DateTimeOffset ObservedAtUtc,
    IReadOnlyList<string> EvidenceRefs,
    ExecutableSkillState InitialState = ExecutableSkillState.Candidate);

public sealed record SkillPromotionRequest(
    SkillCandidateRequest Candidate,
    CognitiveSnapshotV2 Before,
    CognitiveSnapshotV2 After,
    SkillActionTemplate Action,
    SemanticVerificationPlan VerificationPlan,
    SemanticVerificationReport VerificationReport,
    DateTimeOffset VerifiedAtUtc,
    IReadOnlyList<string> EvidenceRefs);

public sealed record SkillPromotionResult(
    SkillPromotionDecision Decision,
    string Code,
    string Reason,
    ExecutableSkill? Skill,
    VerifiedSkillTransition? Transition,
    IReadOnlyList<string> EvidenceRefs);

public sealed record SkillTransitionFailureObservation(
    string SkillId,
    string TransitionId,
    SemanticVerificationFailureClass FailureClass,
    FailureKind FailureKind,
    DateTimeOffset FailedAtUtc,
    IReadOnlyList<string> EvidenceRefs,
    string ReasonRedacted);

public sealed record SkillFailureResult(
    SkillFailureDecision Decision,
    string Code,
    string Reason,
    ExecutableSkill? Skill,
    VerifiedSkillTransition? Transition,
    IReadOnlyList<string> EvidenceRefs);

public sealed record SkillRepairRequest(
    string SkillId,
    string TransitionId,
    CognitiveSnapshotV2 Before,
    CognitiveSnapshotV2 After,
    SkillActionTemplate RepairedAction,
    SemanticVerificationPlan VerificationPlan,
    SemanticVerificationReport VerificationReport,
    DateTimeOffset RepairedAtUtc,
    IReadOnlyList<string> EvidenceRefs);

public sealed record SkillRepairResult(
    SkillRepairDecision Decision,
    string Code,
    string Reason,
    ExecutableSkill? Skill,
    VerifiedSkillTransition? SupersededTransition,
    VerifiedSkillTransition? RepairedTransition,
    IReadOnlyList<string> EvidenceRefs);

public sealed record SkillReplayRequest(
    string SkillId,
    string AppProfileId,
    int AppProfileVersion,
    string CurrentStateFingerprint,
    IReadOnlySet<string> AuthorizedCapabilities);

public sealed record SkillReplaySelection(
    SkillReplayDecision Decision,
    string Code,
    string Reason,
    ExecutableSkill? Skill,
    VerifiedSkillTransition? Transition,
    IReadOnlyList<string> EvidenceRefs)
{
    public bool ActionAuthorityGranted => false;
    public bool RequiresExistingMissionAuthorization => true;
    public bool Ready => Decision == SkillReplayDecision.Ready && Transition is not null;
}
