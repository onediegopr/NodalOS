using OneBrain.Core.Skills;

namespace OneBrain.Pilot;

public enum NodalOsTeachNodalProductState
{
    Empty,
    Bound,
    Capturing,
    ReviewReady,
    Saved,
    FailedClosed
}

public enum NodalOsTeachNodalProposalKind
{
    NewSkill,
    UpdateCandidate
}

public sealed record NodalOsTeachNodalBindRequest(
    string WorkflowTitle,
    string AppProfileName);

public sealed record NodalOsTeachNodalCaptureStepRequest(
    TeachNodalActionKind Kind,
    string Intent,
    string TargetLabel,
    string TargetRole,
    string? ParameterName,
    string? ParameterReference,
    bool SecretByReference);

public sealed record NodalOsTeachNodalProposalEditRequest(
    string Title,
    string Summary,
    int ExpectedVersion,
    DateTimeOffset ExpectedUpdatedAtUtc,
    IReadOnlyDictionary<string, string> StepIntents,
    IReadOnlyDictionary<string, string> StepTargets);

public sealed record NodalOsTeachNodalProductStepSnapshot(
    string StepId,
    string Kind,
    string Intent,
    string TargetLabel,
    string TargetRole,
    IReadOnlyList<string> ParameterRefs,
    string BeforeFingerprint,
    string AfterFingerprint,
    bool StateChanged,
    bool Verified,
    string EvidenceRef);

public sealed record NodalOsTeachNodalProductProposal(
    string DraftId,
    int Version,
    DateTimeOffset? BaseDraftUpdatedAtUtc,
    NodalOsTeachNodalProposalKind Kind,
    string Title,
    string Summary,
    string AppProfileId,
    string ApplicationRef,
    string ProcessNameRedacted,
    string CompilationDecision,
    string CompilationCode,
    string SkillFingerprint,
    IReadOnlyList<NodalOsTeachNodalProductStepSnapshot> Steps,
    IReadOnlyList<string> Findings,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    bool ReviewRequired,
    bool SaveAllowed,
    bool ScriptsIncluded,
    bool RawInputStored,
    bool RawScreenshotStored,
    bool RawDomStored,
    bool GlobalHooksUsed,
    bool ExecutionAuthorityGranted,
    bool ProductAuthorityGranted);

public sealed record NodalOsTeachNodalSavedDraftSummary(
    string DraftId,
    int Version,
    string Title,
    string AppProfileId,
    int StepCount,
    DateTimeOffset UpdatedAtUtc);

public sealed record NodalOsTeachNodalProductSnapshot(
    string Decision,
    NodalOsTeachNodalProductState State,
    bool Bound,
    string BoundApplication,
    string BoundProcess,
    string AppProfileId,
    int ObservationCount,
    NodalOsTeachNodalProductProposal? Proposal,
    IReadOnlyList<NodalOsTeachNodalSavedDraftSummary> SavedDrafts,
    IReadOnlyList<string> Findings,
    bool ExplicitOptInRecorded,
    bool ApplicationScopeBound,
    bool VoiceEngineAdded,
    bool VideoStored,
    bool AudioStored,
    bool RawInputStored,
    bool GlobalHooksUsed,
    bool ReplayEnabled,
    bool ExecutionAuthorityGranted,
    bool ProductAuthorityGranted);
