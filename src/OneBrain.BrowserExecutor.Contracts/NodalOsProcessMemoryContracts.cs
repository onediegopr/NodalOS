namespace OneBrain.BrowserExecutor.Contracts;

public enum NodalOsMemoryScope
{
    LocalFixtureOnly,
    PrivatePreviewLocal,
    TargetOwnedRedacted,
    BlockedSensitive,
    BlockedCredentials,
    BlockedProduction,
    BlockedExternalGeneral
}

public enum NodalOsMemoryConfidence
{
    Unknown,
    Low,
    Medium,
    High,
    VerifiedFixturePattern,
    VerifiedRedactedLocalPattern
}

public enum NodalOsMemoryDeniedReason
{
    None,
    CredentialDetected,
    CookieDetected,
    TokenDetected,
    PaymentInfoDetected,
    PersonalDataDetected,
    CustomerDataDetected,
    RawDomOrBodyDetected,
    RawUiaSensitiveTreeDetected,
    UnredactedLogDetected,
    SubmitPayloadDetected,
    ScreenshotWithSecretDetected,
    ProductionScopeBlocked,
    ExternalGeneralBlocked,
    SensitiveScopeBlocked,
    RecorderReplayProductiveBlocked
}

public enum NodalOsMemoryRedactionPolicy
{
    RedactedMetadataOnly,
    RedactedLocalFixtureOnly,
    RejectSensitiveRawValues,
    RejectProductionLearning
}

public sealed record NodalOsMemoryEvidence(
    string EvidenceRef,
    string Source,
    DateTimeOffset TimestampUtc,
    bool Redacted);

public sealed record NodalOsWorkflowStepMemory(
    string StepId,
    string RedactedActionCategory,
    string RedactedSurfaceIdentity,
    string IdentityFingerprintRef,
    string PerceptionSummaryRef,
    string SafeActionDecisionRef,
    string OperatorDecisionRef,
    string? IssueRef,
    DateTimeOffset TimestampUtc,
    NodalOsMemoryConfidence Confidence,
    IReadOnlyList<string> EvidenceRefs);

public sealed record NodalOsProcessMemoryEntry(
    string EntryId,
    NodalOsMemoryScope Scope,
    NodalOsWorkflowStepMemory Step,
    IReadOnlyList<NodalOsMemoryRedactionPolicy> RedactionPolicies,
    IReadOnlyList<NodalOsMemoryDeniedReason> DeniedReasons,
    bool ActionAuthorityGranted,
    bool CoreApprovalStillRequired,
    bool Accepted,
    bool Redacted);

public sealed record NodalOsWorkflowPattern(
    string WorkflowId,
    IReadOnlyList<NodalOsWorkflowStepMemory> Steps,
    NodalOsMemoryScope Scope,
    NodalOsMemoryConfidence Confidence,
    IReadOnlyList<string> AllowedDecisions,
    IReadOnlyList<string> BlockedDecisions,
    IReadOnlyList<NodalOsMemoryDeniedReason> DeniedReasons,
    IReadOnlyList<string> EvidenceRefs,
    string RecommendedNextStep,
    bool RecorderReplayProductiveEnabled,
    bool Redacted);

public sealed record NodalOsProcessMemory(
    string MemoryId,
    IReadOnlyList<NodalOsProcessMemoryEntry> Entries,
    IReadOnlyList<NodalOsWorkflowPattern> Patterns,
    bool LocalOnly,
    bool ProductionLearningBlocked,
    bool RecorderReplayProductiveBlocked,
    bool ActionAuthorityGranted,
    bool Redacted);

public sealed record NodalOsWorkflowLearningSummary(
    string SummaryId,
    IReadOnlyList<NodalOsWorkflowPattern> Patterns,
    bool WorkflowFixtureLearningReady,
    bool LocalOnly,
    bool RecorderReplayProductiveBlocked,
    bool SensitiveLearningBlocked,
    bool ActionAuthorityGranted,
    bool Redacted);

public sealed record NodalOsProcessMemorySummary(
    string SummaryId,
    int AcceptedEntries,
    int RejectedEntries,
    IReadOnlyList<NodalOsMemoryScope> Scopes,
    IReadOnlyList<NodalOsMemoryDeniedReason> DeniedReasons,
    bool MemoryLocalOnlyReady,
    bool ProductionLearningBlocked,
    bool RecorderReplayProductiveBlocked,
    bool SensitiveLearningBlocked,
    bool ActionAuthorityGranted,
    bool Redacted);

public sealed record NodalOsProcessMemoryEvidenceReview(
    string ReviewId,
    NodalOsProcessMemorySummary MemorySummary,
    NodalOsWorkflowLearningSummary WorkflowSummary,
    IReadOnlyList<string> EvidenceRefs,
    bool ContainsSensitiveRawValues,
    bool ReadyForPrivatePreviewSignal,
    bool Redacted);

public enum NodalOsWorkflowLearningReadiness
{
    NotReady,
    ReadyLocalFixtureOnly,
    BlockedBySensitiveContent,
    BlockedByProductionScope,
    BlockedByRecorderReplay,
    RequiresHumanReview
}

public sealed record NodalOsWorkflowFixture(
    string WorkflowId,
    NodalOsMemoryScope Scope,
    IReadOnlyList<NodalOsSafeActionRunRecord> ActionRecords,
    bool ContainsCredential,
    bool ContainsCookie,
    bool ContainsToken,
    bool ContainsPaymentInfo,
    bool ContainsPersonalOrCustomerData,
    bool ContainsRawDomOrBody,
    bool ContainsRawUiaSensitiveTree,
    bool ContainsUnredactedLogs,
    bool ContainsSubmitPayload,
    bool ContainsScreenshotWithSecret,
    bool RecorderReplayProductiveRequested,
    bool AmbiguousPerception,
    string RecommendedNextStep);

