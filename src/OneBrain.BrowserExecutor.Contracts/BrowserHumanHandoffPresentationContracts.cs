namespace OneBrain.BrowserExecutor.Contracts;

public enum BrowserHumanHandoffDisplayState
{
    WaitingForUser,
    UserCompletedPendingVerification,
    Cancelled,
    Expired,
    Failed,
    Resumed,
    Blocked,
    Disconnected
}

public enum BrowserHumanHandoffUserOption
{
    ContinueAfterUserAction,
    Cancel,
    CopyDiagnosticLog
}

public enum BrowserHumanHandoffUiEventKind
{
    HandoffCreated,
    HandoffUpdated,
    HandoffUserCompleted,
    HandoffCancelled,
    HandoffExpired,
    HandoffResumeRequested,
    HandoffResumeVerified,
    HandoffResumeRejected,
    HandoffDisconnected
}

public sealed record BrowserHumanHandoffPresentation(
    string HandoffId,
    string RunId,
    string ActionId,
    string CorrelationId,
    BrowserHumanHandoffReason Reason,
    BrowserHumanHandoffStatus Status,
    BrowserHumanHandoffDisplayState DisplayState,
    string SafeTitle,
    string SafeUrl,
    string Instruction,
    string ExpectedUserAction,
    IReadOnlyList<BrowserHumanHandoffUserOption> AllowedOptions,
    IReadOnlyList<string> BlockedOptions,
    DateTimeOffset ExpiresAtUtc,
    bool RedactionApplied,
    IReadOnlyList<string> EvidenceRefs,
    IReadOnlyList<string> ProofRefs,
    string RuntimeKind,
    string Source,
    bool Authoritative)
{
    public bool CanUserSignalCompleted =>
        DisplayState == BrowserHumanHandoffDisplayState.WaitingForUser &&
        AllowedOptions.Contains(BrowserHumanHandoffUserOption.ContinueAfterUserAction);

    public ContractValidationResult Validate()
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(HandoffId))
            errors.Add("HandoffId is required.");
        if (string.IsNullOrWhiteSpace(RunId))
            errors.Add("RunId is required.");
        if (string.IsNullOrWhiteSpace(ActionId))
            errors.Add("ActionId is required.");
        if (string.IsNullOrWhiteSpace(CorrelationId))
            errors.Add("CorrelationId is required.");
        if (string.IsNullOrWhiteSpace(Instruction))
            errors.Add("Instruction is required.");
        if (!RedactionApplied)
            errors.Add("Presentation requires redaction.");
        if (Authoritative)
            errors.Add("Companion presentation must be non-authoritative.");
        if (BrowserCredentialRedactor.ContainsSecret(SafeTitle) ||
            BrowserCredentialRedactor.ContainsSecret(SafeUrl) ||
            BrowserCredentialRedactor.ContainsSecret(Instruction) ||
            BrowserCredentialRedactor.ContainsSecret(ExpectedUserAction) ||
            BlockedOptions.Any(BrowserCredentialRedactor.ContainsSecret))
            errors.Add("Presentation contains unredacted secret-like content.");
        return errors.Count == 0 ? ContractValidationResult.Valid : new ContractValidationResult(false, errors);
    }
}

public sealed record BrowserHumanHandoffUiEvent(
    BrowserHumanHandoffUiEventKind Kind,
    string HandoffId,
    string RunId,
    string ActionId,
    string CorrelationId,
    string RuntimeKind,
    string Source,
    bool Authoritative,
    BrowserVerificationStatus? VerificationStatus,
    IReadOnlyList<string> EvidenceRefs,
    IReadOnlyList<string> ProofRefs,
    bool Redacted,
    string Diagnostics)
{
    public bool CanMarkSuccess =>
        Authoritative &&
        Kind == BrowserHumanHandoffUiEventKind.HandoffResumeVerified &&
        VerificationStatus == BrowserVerificationStatus.Verified &&
        ProofRefs.Count > 0 &&
        EvidenceRefs.Count > 0;

    public ContractValidationResult Validate()
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(HandoffId))
            errors.Add("HandoffId is required.");
        if (string.IsNullOrWhiteSpace(RunId))
            errors.Add("RunId is required.");
        if (string.IsNullOrWhiteSpace(ActionId))
            errors.Add("ActionId is required.");
        if (Kind is BrowserHumanHandoffUiEventKind.HandoffUserCompleted or BrowserHumanHandoffUiEventKind.HandoffCancelled &&
            Authoritative)
            errors.Add("Companion user events must be non-authoritative.");
        if (!Redacted)
            errors.Add("Handoff UI event requires redaction.");
        if (BrowserCredentialRedactor.ContainsSecret(Diagnostics))
            errors.Add("Diagnostics contain unredacted secret-like content.");
        return errors.Count == 0 ? ContractValidationResult.Valid : new ContractValidationResult(false, errors);
    }
}

public sealed record BrowserHumanHandoffProtocolEnvelope(
    string MessageType,
    BrowserHumanHandoffUiEvent Event,
    BrowserHumanHandoffPresentation? Presentation,
    DateTimeOffset CreatedAtUtc)
{
    public bool RelayAcceptedIsNotVerified =>
        Event.Kind != BrowserHumanHandoffUiEventKind.HandoffResumeVerified || !Event.CanMarkSuccess;
}
