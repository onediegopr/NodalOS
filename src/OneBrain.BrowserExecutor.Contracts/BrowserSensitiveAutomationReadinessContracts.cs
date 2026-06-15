namespace OneBrain.BrowserExecutor.Contracts;

public enum BrowserSensitiveAutomationReadinessStatus
{
    ReadyForSimulationOnly,
    ReadyForExternalLowRiskOnly,
    ReadyForSensitiveReadOnlyPilotWithConditions,
    BlockedForSensitiveRealPilot,
    ReadyForProductAdminTrack,
    RequiresArchitectureAudit
}

public enum BrowserSensitiveAutomationReadinessCheckStatus
{
    Passed,
    Warning,
    Blocked
}

public enum BrowserSensitiveAutomationRiskSeverity
{
    Low,
    Medium,
    High,
    Critical
}

public enum BrowserSensitiveAutomationRiskProbability
{
    Low,
    Medium,
    High
}

public enum BrowserSensitiveAutomationRiskState
{
    Open,
    Mitigated,
    Blocked
}

public enum BrowserSensitiveAutomationRoadmapOption
{
    M25BExternalTestOwnedTarget,
    M33BSensitiveReadOnlyRealPilot,
    M34BSensitiveDocumentRealPilot,
    M35CriticalSubmitGate,
    AdminLicensingProductTrack,
    WebView2CefArchitecture,
    ProductiveVaultOsBackedImplementation
}

public enum BrowserSensitiveAutomationRecommendationKind
{
    Advance,
    DoNotAdvance,
    AdvanceWithConditions
}

public sealed record BrowserSensitiveAutomationReadinessCheck(
    string Name,
    BrowserSensitiveAutomationReadinessCheckStatus Status,
    string Detail,
    string EvidenceRef);

public sealed record BrowserSensitiveAutomationRisk(
    string RiskId,
    string Title,
    BrowserSensitiveAutomationRiskSeverity Severity,
    BrowserSensitiveAutomationRiskProbability Probability,
    BrowserSensitiveAutomationRiskState State,
    string Mitigation,
    string RecommendedMilestone);

public sealed record BrowserSensitiveAutomationRiskRegister(IReadOnlyList<BrowserSensitiveAutomationRisk> Risks)
{
    public bool Includes(string riskId) =>
        Risks.Any(risk => risk.RiskId.Equals(riskId, StringComparison.OrdinalIgnoreCase));
}

public sealed record BrowserSensitiveAutomationNextStepRecommendation(
    BrowserSensitiveAutomationRoadmapOption Option,
    string Benefit,
    string Risk,
    IReadOnlyList<string> Preconditions,
    IReadOnlyList<string> Blockers,
    IReadOnlyList<string> RequiredTests,
    BrowserSensitiveAutomationRecommendationKind Recommendation);

public sealed record BrowserSensitiveAutomationDecisionMatrix(IReadOnlyList<BrowserSensitiveAutomationNextStepRecommendation> Options)
{
    public BrowserSensitiveAutomationNextStepRecommendation For(BrowserSensitiveAutomationRoadmapOption option) =>
        Options.Single(item => item.Option == option);
}

public sealed record BrowserSensitiveAutomationReadinessRequest(
    BrowserRuntimePhaseCloseReport PhaseGateReport,
    bool M25BExternalLowRiskTargetAvailable,
    bool M28ExternalWorkflowEnabled,
    bool SensitiveReadOnlySimulationComplete,
    bool SensitiveDocumentSimulationComplete,
    bool SensitiveRealPilotDecisionApproved,
    bool ProductAdminTrackRequested);

public sealed record BrowserSensitiveAutomationReadinessReport(
    BrowserSensitiveAutomationReadinessStatus Status,
    IReadOnlyList<BrowserSensitiveAutomationReadinessCheck> Checks,
    BrowserSensitiveAutomationRiskRegister RiskRegister,
    BrowserSensitiveAutomationDecisionMatrix DecisionMatrix,
    BrowserSensitiveAutomationNextStepRecommendation RecommendedNextStep,
    bool BlocksSensitiveRealPilot,
    bool BlocksSensitiveDocumentRealPilot,
    bool M25BBlocked,
    bool M28ExternalWorkflowBlocked,
    bool Redacted,
    string Summary)
{
    public bool IsCheckpointComplete =>
        Checks.Count > 0 &&
        RiskRegister.Includes("R1") &&
        DecisionMatrix.Options.Count >= 7 &&
        Redacted;
}
