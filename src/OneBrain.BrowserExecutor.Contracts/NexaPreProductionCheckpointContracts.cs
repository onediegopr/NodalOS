namespace OneBrain.BrowserExecutor.Contracts;

public enum NexaPreProductionStatus
{
    ReadyForLocalPrivatePilot,
    ReadyForProductAdminPrivatePreview,
    ReadyForExternalLowRiskTargetSetup,
    ReadyForVaultHardening,
    ReadyForWebView2CefArchitecture,
    BlockedForSensitiveRealPilot,
    BlockedForPublicSaas,
    BlockedForRealBilling,
    RequiresExternalAudit
}

public enum NexaPreProductionCapabilityKind
{
    BrowserRuntime,
    CDPLiveReadOnly,
    SafeDownload,
    SafeUpload,
    DocumentWorkflowSandbox,
    RecorderReadOnly,
    ReplaySafeMode,
    SensitiveSimulation,
    SensitiveRealPilot,
    ProductAdmin,
    Licensing,
    TenantGovernance,
    AuditExport,
    Diagnostics,
    PackagingDryRun,
    PublicApiBoundary,
    VaultOsBackedMinimal,
    ReleaseUpdateModel,
    BillingMock,
    OnboardingMock
}

public sealed record NexaPreProductionCapabilityStatus(
    NexaPreProductionCapabilityKind Capability,
    bool Available,
    bool ProductionEnabled,
    string Status,
    bool Redacted);

public sealed record NexaPreProductionRisk(
    string RiskId,
    string Description,
    string Severity,
    string Mitigation,
    string RecommendedMilestone,
    bool Open);

public sealed record NexaPreProductionRiskRegister(IReadOnlyList<NexaPreProductionRisk> Risks);

public sealed record NexaPreProductionDecisionOption(
    string OptionId,
    string Benefit,
    string Risk,
    IReadOnlyList<string> Preconditions,
    IReadOnlyList<string> Blockers,
    string Recommendation,
    IReadOnlyList<string> SuggestedMilestones);

public sealed record NexaPreProductionDecisionMatrix(IReadOnlyList<NexaPreProductionDecisionOption> Options);

public sealed record NexaPreProductionBlocker(string BlockerId, string Reason, bool BlocksProduction);

public sealed record NexaPreProductionRecommendation(
    NexaPreProductionStatus Status,
    string NextRoadmapPath,
    bool ExternalAuditRequired);

public sealed record NexaPreProductionCheckpointReport(
    string ReportId,
    IReadOnlyList<NexaPreProductionCapabilityStatus> Capabilities,
    NexaPreProductionRiskRegister RiskRegister,
    NexaPreProductionDecisionMatrix DecisionMatrix,
    IReadOnlyList<NexaPreProductionBlocker> Blockers,
    NexaPreProductionRecommendation Recommendation,
    bool Redacted,
    bool ContainsSecretsCookiesBodies)
{
    public ContractValidationResult Validate()
    {
        var errors = new List<string>();
        BrowserSafeIdentifierValidator.RequireSafe(ReportId, nameof(ReportId), errors);
        if (!Redacted)
            errors.Add("Pre-production checkpoint report must be redacted.");
        if (ContainsSecretsCookiesBodies)
            errors.Add("Pre-production checkpoint report cannot contain secrets, cookies, or bodies.");
        if (Capabilities.Count == 0)
            errors.Add("Pre-production checkpoint requires a capability matrix.");
        if (RiskRegister.Risks.Count == 0)
            errors.Add("Pre-production checkpoint requires a risk register.");
        if (DecisionMatrix.Options.Count == 0)
            errors.Add("Pre-production checkpoint requires a decision matrix.");
        return errors.Count == 0 ? ContractValidationResult.Valid : new ContractValidationResult(false, errors);
    }
}

public sealed record NexaPreProductionCheckpointRequest(
    bool M25BExternalLowRiskTargetAvailable,
    bool SensitiveRealPilotDecisionApproved,
    bool PublicSaasEnabled,
    bool RealBillingEnabled,
    bool RealEmailEnabled,
    bool AutoUpdateRealEnabled,
    bool ProductiveRecorderReplayEnabled,
    bool ProfileRawEnabled,
    bool RealClientCredentialsEnabled);
