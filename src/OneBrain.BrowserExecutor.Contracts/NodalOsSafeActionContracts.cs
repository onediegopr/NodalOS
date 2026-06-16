namespace OneBrain.BrowserExecutor.Contracts;

public enum NodalOsActionRiskLevel
{
    None,
    Low,
    Medium,
    High,
    Critical,
    Prohibited
}

public enum NodalOsActionCategory
{
    ObserveOnly,
    ReadOnlyNavigation,
    LocalUiFocus,
    LocalPanelOpen,
    LocalDiagnosticsOpen,
    LocalEvidenceReview,
    LocalIssueTriage,
    LocalCopyToClipboardIfRedacted,
    LocalDraftOnly,
    ExternalReadOnlyTargetOwned,
    BlockedCredentialEntry,
    BlockedSubmit,
    BlockedPayment,
    BlockedDelete,
    BlockedSign,
    BlockedSensitiveSurface,
    BlockedExternalGeneral,
    BlockedProduction
}

public enum NodalOsActionApprovalRequirement
{
    NoApprovalNeededForObserveOnly,
    CoreApprovalRequired,
    HumanApprovalRequired,
    DedicatedEvidenceRequired,
    AlwaysBlocked
}

public enum NodalOsActionDecision
{
    AllowedObserveOnly,
    AllowedLocalReadOnlyWithCoreApproval,
    AllowedLocalDraftOnlyWithCoreApproval,
    Denied,
    BlockedAlways
}

public enum NodalOsActionDeniedReason
{
    None,
    MissingCoreApproval,
    IdentityNotVerified,
    PerceptionNotUsable,
    OverlayBlocked,
    EvidenceNotRedacted,
    CredentialEntryBlocked,
    SubmitBlocked,
    PaymentBlocked,
    DeleteBlocked,
    SignBlocked,
    SensitiveSurfaceBlocked,
    ExternalGeneralBlocked,
    ProductionBlocked,
    RecorderReplayBlocked,
    UnsafeActionCategory
}

public sealed record NodalOsActionPrecondition(
    string Name,
    bool Satisfied,
    string EvidenceRef);

public sealed record NodalOsActionBoundary(
    bool CoreAuthorityRequired,
    bool CoreApproved,
    bool UiAdminCompanionAuthorityBlocked,
    bool IdentityGrantsAuthority,
    bool PerceptionGrantsAuthority,
    bool ProductionScopeBlocked,
    bool ExternalGeneralBlocked);

public sealed record NodalOsSafeAction(
    string ActionId,
    NodalOsActionCategory Category,
    NodalOsActionRiskLevel RiskLevel,
    NodalOsActionApprovalRequirement ApprovalRequirement,
    IReadOnlyList<NodalOsActionPrecondition> Preconditions,
    NodalOsActionBoundary Boundary,
    bool RedactedPayloadOnly);

public sealed record NodalOsSafeActionEvidence(
    string EvidenceRef,
    string ActionId,
    NodalOsActionCategory Category,
    NodalOsActionRiskLevel RiskLevel,
    NodalOsActionDecision Decision,
    IReadOnlyList<NodalOsActionDeniedReason> DeniedReasons,
    IReadOnlyList<string> EvidenceRefs,
    string RedactionSummary,
    bool Redacted);

public sealed record NodalOsSafeActionRunRecord(
    string RunId,
    DateTimeOffset TimestampUtc,
    NodalOsSafeAction Action,
    NodalOsIdentityConfidence IdentityConfidence,
    NodalOsPerceptionReadiness PerceptionReadiness,
    NodalOsActionDecision Decision,
    IReadOnlyList<NodalOsActionDeniedReason> DeniedReasons,
    NodalOsSafeActionEvidence Evidence,
    string OperatorExplanation,
    bool ActionExecuted,
    bool SensitiveActionAuthorized);

public sealed record NodalOsActionDecisionSummary(
    string SummaryId,
    IReadOnlyList<NodalOsSafeActionRunRecord> Records,
    IReadOnlyList<NodalOsActionCategory> AllowedCategories,
    IReadOnlyList<NodalOsActionCategory> BlockedCategories,
    bool CoreAuthorityRequired,
    bool UiAdminCompanionAuthorityBlocked,
    bool SensitiveActionsAuthorized,
    bool Redacted);

public sealed record NodalOsActionBoundaryEvidence(
    string EvidenceRef,
    bool CoreApprovalBoundaryEnforced,
    bool UiAdminCompanionAuthorityBlocked,
    bool IdentityPerceptionNonAuthoritative,
    bool DangerousSurfacesBlocked,
    IReadOnlyList<string> BlockedOptions,
    bool Redacted);

public sealed record NodalOsSafeActionFixture(
    string FixtureId,
    NodalOsSafeAction Action,
    NodalOsIdentityFixture IdentityFixture,
    NodalOsRobustPerceptionFixture PerceptionFixture,
    bool ReleaseGateReadyWithRestrictions,
    bool EvidenceRedacted,
    bool OverlayBlocked,
    string OperatorExplanation);

