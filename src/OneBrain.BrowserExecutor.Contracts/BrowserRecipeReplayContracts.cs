namespace OneBrain.BrowserExecutor.Contracts;

public enum BrowserRecipeReplayMode
{
    SafeModeReadOnly,
    ProductiveReplay,
    SensitiveReplay,
    IrreversibleReplay,
    CredentialReplay,
    SubmitReplay
}

public enum BrowserRecipeReplayStepStatus
{
    Planned,
    Executed,
    Verified,
    Blocked,
    Duplicate
}

public sealed record BrowserRecipeReplayPolicy(
    BrowserRecipeReplayMode Mode,
    IReadOnlySet<string> AllowlistedHosts,
    bool RequireGate,
    bool RequireIdempotency,
    bool RequireVerification,
    bool RequireSemanticProof);

public sealed record BrowserRecipeReplayRequest(
    string RunId,
    BrowserRecorderDraftRecipe Recipe,
    BrowserRecipeReplayPolicy Policy,
    BrowserRuntimePhaseCloseReport? GateReport,
    string IdempotencyScope,
    bool TargetLive);

public sealed record BrowserRecipeReplayStep(
    string StepId,
    BrowserRecipeReplayStepStatus Status,
    BrowserRecordedActionKind ActionKind,
    string IdempotencyKey,
    IReadOnlyList<string> EvidenceRefs,
    IReadOnlyList<string> ProofRefs,
    string Reason);

public sealed record BrowserRecipeReplayPlan(IReadOnlyList<BrowserRecipeReplayStep> Steps, bool SafeMode, bool DiagnosticOnly);

public sealed record BrowserRecipeReplayEvidence(IReadOnlyList<string> EvidenceRefs, IReadOnlyList<string> ProofRefs, bool SemanticProofPresent);

public sealed record BrowserRecipeReplayResult(
    BrowserRecipeReplayMode Mode,
    IReadOnlyList<BrowserRecipeReplayStep> Steps,
    BrowserRecipeReplayEvidence Evidence,
    bool Completed,
    bool Blocked,
    bool DuplicateBlocked,
    string Reason)
{
    public bool AllowsDone =>
        Completed &&
        !Blocked &&
        !DuplicateBlocked &&
        Evidence.SemanticProofPresent &&
        Evidence.ProofRefs.Count > 0 &&
        Steps.All(s => s.Status == BrowserRecipeReplayStepStatus.Verified);
}

