namespace OneBrain.BrowserExecutor.Contracts;

public enum NexaPrivatePreviewOperatorStepKind
{
    StartSession,
    ValidateConfigProfile,
    ValidateMockLicense,
    ValidateTenantWorkspace,
    OpenLocalProductShell,
    CheckDiagnostics,
    CheckFeatureFlags,
    CheckAuditIntegrity,
    UsePrivateLocalApi,
    GenerateSessionSummary,
    CloseSession
}

public enum NexaPrivatePreviewOperatorDecisionKind
{
    Allowed,
    Blocked
}

public enum NexaPrivatePreviewOperatorFinalStatus
{
    Completed,
    Blocked
}

public sealed record NexaPrivatePreviewOperatorSession(
    string SessionId,
    string OperatorId,
    NexaRole Role,
    string TenantId,
    string WorkspaceId,
    NexaConfigurationProfileKind ConfigProfile,
    string LicenseSummaryRedacted,
    string FeatureSummaryRedacted,
    bool LocalOnly,
    bool SingleTenant);

public sealed record NexaPrivatePreviewOperatorStep(
    NexaPrivatePreviewOperatorStepKind Step,
    bool Passed,
    string ReasonRedacted);

public sealed record NexaPrivatePreviewOperatorEvidence(
    IReadOnlyList<string> DiagnosticsRefs,
    IReadOnlyList<string> AuditRefs,
    IReadOnlyList<string> GateRefs,
    bool Redacted);

public sealed record NexaPrivatePreviewOperatorDecision(
    NexaPrivatePreviewOperatorDecisionKind Decision,
    IReadOnlyList<string> ReasonCodes,
    bool Redacted);

public sealed record NexaPrivatePreviewOperatorFlow(
    string FlowId,
    NexaPrivatePreviewOperatorSession Session,
    IReadOnlyList<NexaPrivatePreviewOperatorStep> Steps,
    NexaPrivatePreviewOperatorEvidence Evidence,
    bool UsedPrivateLocalApiInProcess,
    bool PublicSaasEnabled,
    bool RealBillingEnabled,
    bool RealEmailEnabled,
    bool SensitiveRealPilotEnabled,
    bool ProductiveRecorderReplayEnabled,
    bool RequiresExternalMode,
    bool M51ExternalProofDeferred,
    bool ContainsSecretsCookiesBodies,
    bool Redacted);

public sealed record NexaPrivatePreviewOperatorResult(
    NexaPrivatePreviewOperatorDecision Decision,
    NexaPrivatePreviewOperatorFlow Flow,
    NexaPrivatePreviewOperatorFinalStatus FinalStatus,
    string SessionSummaryRedacted,
    bool Redacted);

public enum NexaPrivatePreviewIssueCategory
{
    Security,
    Product,
    Ux,
    Runtime,
    Vault,
    Api,
    BillingEmailSandbox,
    DiagnosticsAudit,
    DocumentationRunbook
}

public enum NexaPrivatePreviewIssueSeverity
{
    Info,
    Low,
    Medium,
    High,
    Critical,
    Blocker
}

public enum NexaPrivatePreviewIssueDecisionKind
{
    Accept,
    Reject,
    NeedsMoreInfo,
    Defer,
    FixBeforeNextPreview,
    SecurityBlocker,
    ReleaseBlocker
}

public enum NexaPrivatePreviewIssueDisposition
{
    Open,
    Accepted,
    Deferred,
    Blocked,
    Closed
}

public sealed record NexaPrivatePreviewIssueAction(
    string ActionId,
    string OwnerRole,
    string SummaryRedacted,
    bool RequiredBeforeNextPreview);

public sealed record NexaPrivatePreviewIssueTriage(
    string TriageId,
    NexaPrivatePreviewIssueCategory Category,
    NexaPrivatePreviewIssueSeverity Severity,
    string SummaryRedacted,
    bool SecretCookieBodyLeak,
    bool CrossTenantAccess,
    bool VaultRawExposure,
    bool SupportCanSeeSecret,
    bool PublicApiExposure,
    bool RealBillingEmailEnabled,
    bool SensitiveRealPilotEnabled,
    bool BuildOrTestFailed,
    bool GateFailed,
    bool DiagnosticsUnavailable,
    bool AuditIntegrityUnavailable,
    bool RunbookMissing,
    bool ApiRoleEnforcementBroken,
    bool ContainsSecretsCookiesBodies);

public sealed record NexaPrivatePreviewIssueDecision(
    NexaPrivatePreviewIssueDecisionKind Decision,
    NexaPrivatePreviewIssueDisposition Disposition,
    IReadOnlyList<string> ReasonCodes,
    IReadOnlyList<NexaPrivatePreviewIssueAction> Actions,
    bool Redacted);

public enum NexaPrivatePreviewGoNoGoDecisionKind
{
    GoForNextLocalPreview,
    GoForPrivateAdminPreview,
    GoForExternalTargetSetup,
    GoForVaultHardening,
    NoGoSecurityBlocker,
    NoGoReleaseBlocker,
    NoGoMissingEvidence
}

public enum NexaPrivatePreviewNextStageRecommendation
{
    ContinueLocalPrivatePreview,
    CreateExternalTestOwnedTarget,
    HardenVaultFurther,
    PreparePrivateAdminPreview,
    StopForExternalAudit
}

public sealed record NexaPrivatePreviewExitCriteria(
    bool BuildOk,
    bool SuiteOk,
    bool GateOk,
    bool NoCriticalHighSecurityBlockers,
    bool NoUnresolvedReleaseBlockers,
    bool AuditKeyCustodyOk,
    bool DiagnosticsRedactionOk,
    bool TenantGovernanceOk,
    bool PrivateLocalApiRoleEnforcementOk,
    bool SupportMetadataOnly,
    bool BillingEmailMockOrSandbox,
    bool PublicSaasDisabled,
    bool PublicApiListenerDisabled,
    bool M51ExternalProofStatusExplicit,
    bool M51ExternalProofDeferred,
    bool ContainsSecretsCookiesBodies);

public sealed record NexaPrivatePreviewGoNoGoReport(
    string ReportId,
    NexaPrivatePreviewExitCriteria Criteria,
    IReadOnlyList<NexaPrivatePreviewIssueDecision> IssueDecisions,
    NexaPrivatePreviewGoNoGoDecisionKind Decision,
    NexaPrivatePreviewNextStageRecommendation Recommendation,
    IReadOnlyList<string> Blockers,
    bool PublicSaasStillDisabled,
    bool RealBillingStillDisabled,
    bool RealEmailStillDisabled,
    bool M51DeferredExplicit,
    bool Redacted);
