namespace OneBrain.BrowserExecutor.Contracts;

public sealed record NodalOsAssistedVerificationFixtureCase(
    string FixtureId,
    NodalOsAssistedVerificationRequest Request,
    NodalOsAssistedVerificationDecision ExpectedDecision,
    string Description);

public sealed record NodalOsAssistedVerificationFixtureExecutionResult(
    string FixtureId,
    NodalOsAssistedVerificationRiskLevel RiskLevel,
    string? OcrSignalState,
    string? OcrRecognizedText,
    NodalOsAssistedVerificationSignalKind? NonOcrSignalKind,
    string? NonOcrExpectedValue,
    bool CorroborationSatisfied,
    NodalOsAssistedVerificationDecision Decision,
    NodalOsAssistedVerificationDecision ExpectedDecision,
    bool MatchedExpectation,
    string Reason,
    IReadOnlyList<string> Warnings,
    bool ActionsAllowed,
    bool CanProduceActionPlan,
    bool CanProduceSafeAction,
    bool CanApproveClick,
    bool CanApproveSubmit,
    bool CanApproveSend,
    bool CanApproveDelete,
    bool CanApprovePay,
    bool CanApproveSign,
    bool NoAuthority,
    bool EvidenceOnly);

public sealed record NodalOsAssistedVerificationFixtureExecutionSummary(
    string SummaryId,
    IReadOnlyList<NodalOsAssistedVerificationFixtureExecutionResult> Fixtures,
    int FixturesTotal,
    int PassingFixturesPassed,
    int FailingFixturesRejected,
    int UnexpectedPasses,
    int UnexpectedFailures);
