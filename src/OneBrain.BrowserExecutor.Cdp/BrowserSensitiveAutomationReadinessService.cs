using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.BrowserExecutor.Cdp;

public sealed class BrowserSensitiveAutomationReadinessService
{
    public BrowserSensitiveAutomationReadinessReport Evaluate(BrowserSensitiveAutomationReadinessRequest request)
    {
        var state = request.PhaseGateReport.ObservedState;
        var checks = Checks(request, state);
        var risks = RiskRegister();
        var matrix = DecisionMatrix(request);
        var m25Blocked = !request.M25BExternalLowRiskTargetAvailable;
        var m28Blocked = !request.M28ExternalWorkflowEnabled;
        var blocksSensitivePilot = !request.SensitiveRealPilotDecisionApproved || m25Blocked || request.PhaseGateReport.Passed != true;
        var status = Status(request, blocksSensitivePilot, m25Blocked);
        var recommended = Recommended(matrix, request, m25Blocked);
        var summary = BrowserCredentialRedactor.Redact(status switch
        {
            BrowserSensitiveAutomationReadinessStatus.ReadyForSimulationOnly => "Sensitive automation is ready for local/sandbox simulation only; real sensitive pilots remain blocked.",
            BrowserSensitiveAutomationReadinessStatus.ReadyForExternalLowRiskOnly => "External low-risk validation can proceed before sensitive real pilots.",
            BrowserSensitiveAutomationReadinessStatus.ReadyForProductAdminTrack => "Product/admin track can advance with conditions while sensitive real pilots remain blocked.",
            BrowserSensitiveAutomationReadinessStatus.ReadyForSensitiveReadOnlyPilotWithConditions => "Sensitive read-only pilot requires explicit approved decision, legal/compliance controls, and no irreversible actions.",
            BrowserSensitiveAutomationReadinessStatus.RequiresArchitectureAudit => "Architecture audit is required before advancing.",
            _ => "Sensitive real pilot is blocked."
        });

        return new BrowserSensitiveAutomationReadinessReport(
            status,
            checks,
            risks,
            matrix,
            recommended,
            BlocksSensitiveRealPilot: blocksSensitivePilot,
            BlocksSensitiveDocumentRealPilot: blocksSensitivePilot || !request.SensitiveDocumentSimulationComplete,
            M25BBlocked: m25Blocked,
            M28ExternalWorkflowBlocked: m28Blocked,
            Redacted: true,
            summary);
    }

    private static BrowserSensitiveAutomationReadinessStatus Status(BrowserSensitiveAutomationReadinessRequest request, bool blocksSensitivePilot, bool m25Blocked)
    {
        if (request.PhaseGateReport.Passed != true)
            return BrowserSensitiveAutomationReadinessStatus.RequiresArchitectureAudit;
        if (request.ProductAdminTrackRequested && blocksSensitivePilot)
            return BrowserSensitiveAutomationReadinessStatus.ReadyForProductAdminTrack;
        if (blocksSensitivePilot)
            return m25Blocked
                ? BrowserSensitiveAutomationReadinessStatus.ReadyForSimulationOnly
                : BrowserSensitiveAutomationReadinessStatus.ReadyForExternalLowRiskOnly;
        return BrowserSensitiveAutomationReadinessStatus.ReadyForSensitiveReadOnlyPilotWithConditions;
    }

    private static IReadOnlyList<BrowserSensitiveAutomationReadinessCheck> Checks(BrowserSensitiveAutomationReadinessRequest request, BrowserRuntimeObservedState? state)
    {
        var checks = new List<BrowserSensitiveAutomationReadinessCheck>
        {
            Check("M25B external low-risk target status", request.M25BExternalLowRiskTargetAvailable, "external test-owned target available", "evidence:m25b-target"),
            Check("M28 external workflow status", request.M28ExternalWorkflowEnabled, "external document workflow enabled", "evidence:m28-external"),
            Check("sensitive real pilot status", request.SensitiveRealPilotDecisionApproved, "sensitive real pilot decision approved", "evidence:sensitive-pilot-decision"),
            Check("vault mode", state?.VaultState == BrowserRuntimeVaultState.MinimalSandboxActive, "vault is minimal sandbox, not production", "evidence:vault-mode"),
            Check("profile mode", state?.ProfileState == BrowserRuntimeProfileState.UserProfileControlledWithConsent, "controlled profile with consent", "evidence:profile-mode"),
            Check("consent mode", state?.ControlledProfileConsentValid == true && state.MinimalSandboxVaultConsentValid, "scoped consent available", "evidence:consent-mode"),
            Check("gate status", request.PhaseGateReport.Passed, "runtime phase gate passed", "evidence:phase-gate"),
            Check("audit ledger HMAC/head seal", state?.UsesHmacLedgerIntegrity == true && state.AuditLedgerHeadSealAvailable && state.AuditLedgerHeadSealValid, "HMAC/head seal active", "audit:m17-hmac-head"),
            Check("network metadata-only", state?.NetworkCaptureMode == BrowserNetworkCaptureMode.MetadataOnly, "network capture is metadata-only", "evidence:network"),
            Check("bodies disabled", state?.RequestBodyCaptureSupported == false && state.ResponseBodyCaptureSupported == false, "bodies unsupported", "evidence:no-bodies"),
            Check("sensitive headers presence-only", state?.SensitiveHeaderValueCaptureSupported == false, "sensitive header values unsupported", "evidence:headers-presence-only"),
            Check("safe download", state?.SafeDownloadAllowed == true, "safe download policy available", "evidence:safe-download"),
            Check("safe upload", state?.SafeUploadAllowed == true, "safe upload policy available", "evidence:safe-upload"),
            Check("recorder mode", state?.RecorderState == BrowserRuntimeRecorderState.ReadOnlyPrototypeActive && state.RecorderAllowed, "recorder read-only prototype only", "evidence:recorder"),
            Check("replay mode", state?.ReplayState == BrowserRuntimeReplayState.SafeModeReadOnlyActive && state.ReplayAllowed, "replay safe-mode read-only only", "evidence:replay"),
            Check("sensitive policy status", state?.SensitiveSitesPolicyDefined == true, "sensitive policy defined", "evidence:m32-policy"),
            Check("sensitive simulation status", request.SensitiveReadOnlySimulationComplete && request.SensitiveDocumentSimulationComplete, "M33A/M34A simulations complete", "evidence:m33a-m34a"),
            Check("Browser-004.x legacy isolation", state?.Browser004xLegacyIsolated == true, "legacy isolated", "evidence:legacy-isolated")
        };
        return checks;
    }

    private static BrowserSensitiveAutomationReadinessCheck Check(string name, bool passed, string detail, string evidenceRef) =>
        new(name, passed ? BrowserSensitiveAutomationReadinessCheckStatus.Passed : BrowserSensitiveAutomationReadinessCheckStatus.Blocked, BrowserCredentialRedactor.Redact(detail), evidenceRef);

    private static BrowserSensitiveAutomationRiskRegister RiskRegister() =>
        new(
        [
            Risk("R1", "M25B sin target externo test-owned", BrowserSensitiveAutomationRiskSeverity.High, BrowserSensitiveAutomationRiskProbability.High, "Define test-owned external target", "M25B"),
            Risk("R2", "No hay piloto sensible real todavía", BrowserSensitiveAutomationRiskSeverity.High, BrowserSensitiveAutomationRiskProbability.High, "Run compliance/legal checkpoint before pilot", "M33B/M34B"),
            Risk("R3", "Vault minimal es SandboxLocalEncrypted, no vault productivo final", BrowserSensitiveAutomationRiskSeverity.High, BrowserSensitiveAutomationRiskProbability.Medium, "Implement OS-backed productive vault", "Productive Vault"),
            Risk("R4", "No DPAPI/Windows Credential Manager real", BrowserSensitiveAutomationRiskSeverity.Medium, BrowserSensitiveAutomationRiskProbability.Medium, "Choose and implement OS-backed provider", "Productive Vault"),
            Risk("R5", "No WebView2/CEF", BrowserSensitiveAutomationRiskSeverity.Medium, BrowserSensitiveAutomationRiskProbability.Medium, "Architect embedded runtime separately", "WebView2/CEF"),
            Risk("R6", "No producto/admin/licensing integrado", BrowserSensitiveAutomationRiskSeverity.Medium, BrowserSensitiveAutomationRiskProbability.High, "Build admin/licensing/product controls", "Product/Admin"),
            Risk("R7", "Recorder/replay siguen safe/read-only, no productivos", BrowserSensitiveAutomationRiskSeverity.High, BrowserSensitiveAutomationRiskProbability.Medium, "Keep productive replay blocked until audit", "Recorder/Replay"),
            Risk("R8", "Sitios críticos requieren compliance/legales/operativas", BrowserSensitiveAutomationRiskSeverity.Critical, BrowserSensitiveAutomationRiskProbability.High, "Obtain compliance/legal/operations approval", "Sensitive Pilot"),
            Risk("R9", "Submit/pay/sign/delete siguen bloqueados", BrowserSensitiveAutomationRiskSeverity.Critical, BrowserSensitiveAutomationRiskProbability.High, "Create critical submit gate before enabling", "M35 Critical Submit Gate"),
            Risk("R10", "External workflow real M28 sigue bloqueado", BrowserSensitiveAutomationRiskSeverity.High, BrowserSensitiveAutomationRiskProbability.High, "Unblock M25B target before external workflow", "M28 External")
        ]);

    private static BrowserSensitiveAutomationRisk Risk(string id, string title, BrowserSensitiveAutomationRiskSeverity severity, BrowserSensitiveAutomationRiskProbability probability, string mitigation, string milestone) =>
        new(id, BrowserCredentialRedactor.Redact(title), severity, probability, BrowserSensitiveAutomationRiskState.Open, BrowserCredentialRedactor.Redact(mitigation), milestone);

    private static BrowserSensitiveAutomationDecisionMatrix DecisionMatrix(BrowserSensitiveAutomationReadinessRequest request)
    {
        var targetBlockers = request.M25BExternalLowRiskTargetAvailable ? Array.Empty<string>() : ["No test-owned external target is configured."];
        return new BrowserSensitiveAutomationDecisionMatrix(
        [
            Option(BrowserSensitiveAutomationRoadmapOption.M25BExternalTestOwnedTarget, "Unblocks real low-risk external validation.", "Low-to-medium if target is test-owned.", ["Test-owned target", "synthetic credentials", "allowlist"], targetBlockers, ["BrowserExternalLowRiskAuthLive"], request.M25BExternalLowRiskTargetAvailable ? BrowserSensitiveAutomationRecommendationKind.Advance : BrowserSensitiveAutomationRecommendationKind.AdvanceWithConditions),
            Option(BrowserSensitiveAutomationRoadmapOption.M33BSensitiveReadOnlyRealPilot, "Validates sensitive read-only pilot.", "High compliance and operational risk.", ["M25B validated", "legal/compliance approval", "pilot decision", "single approval"], request.SensitiveRealPilotDecisionApproved && request.M25BExternalLowRiskTargetAvailable ? [] : ["No approved sensitive pilot decision or M25B target."], ["SensitiveSitePolicy", "BrowserSensitiveReadOnlySimulation", "BrowserRuntimePhaseGate"], BrowserSensitiveAutomationRecommendationKind.DoNotAdvance),
            Option(BrowserSensitiveAutomationRoadmapOption.M34BSensitiveDocumentRealPilot, "Validates sensitive document handling.", "Critical data leakage and compliance risk.", ["M33B read-only pilot", "document handling policy", "safe download/upload", "pilot decision"], request.SensitiveRealPilotDecisionApproved && request.M25BExternalLowRiskTargetAvailable ? [] : ["No approved sensitive document pilot decision or M25B target."], ["BrowserSensitiveDocumentSimulation", "BrowserSafeDownload", "BrowserSafeUpload"], BrowserSensitiveAutomationRecommendationKind.DoNotAdvance),
            Option(BrowserSensitiveAutomationRoadmapOption.M35CriticalSubmitGate, "Defines irreversible action governance.", "Critical if used to enable submit/pay/sign/delete prematurely.", ["Double approval model", "typed confirmation", "legal review"], ["Irreversible actions remain prohibited."], ["SensitiveSitePolicy", "BrowserRuntimePhaseGate"], BrowserSensitiveAutomationRecommendationKind.AdvanceWithConditions),
            Option(BrowserSensitiveAutomationRoadmapOption.AdminLicensingProductTrack, "Builds product controls without touching sensitive real sites.", "Medium product complexity, low browser-risk.", ["Admin policy model", "licensing states", "tenant controls"], [], ["BrowserRuntimePhaseGate", "BrowserAudit"], BrowserSensitiveAutomationRecommendationKind.AdvanceWithConditions),
            Option(BrowserSensitiveAutomationRoadmapOption.WebView2CefArchitecture, "Prepares embedded runtime architecture.", "Medium architecture risk.", ["Runtime boundary design", "companion authority unchanged"], ["No implementation in sensitive pilot path."], ["BrowserRuntimePhaseGate"], BrowserSensitiveAutomationRecommendationKind.AdvanceWithConditions),
            Option(BrowserSensitiveAutomationRoadmapOption.ProductiveVaultOsBackedImplementation, "Moves vault from sandbox to OS-backed provider.", "High secret custody risk.", ["DPAPI/Credential Manager design", "key custody policy", "audit review"], ["No real secrets until provider is audited."], ["BrowserVault", "BrowserAudit", "BrowserConsent"], BrowserSensitiveAutomationRecommendationKind.AdvanceWithConditions)
        ]);
    }

    private static BrowserSensitiveAutomationNextStepRecommendation Option(BrowserSensitiveAutomationRoadmapOption option, string benefit, string risk, IReadOnlyList<string> preconditions, IReadOnlyList<string> blockers, IReadOnlyList<string> tests, BrowserSensitiveAutomationRecommendationKind recommendation) =>
        new(option, BrowserCredentialRedactor.Redact(benefit), BrowserCredentialRedactor.Redact(risk), preconditions, blockers.Select(BrowserCredentialRedactor.Redact).ToArray(), tests, recommendation);

    private static BrowserSensitiveAutomationNextStepRecommendation Recommended(BrowserSensitiveAutomationDecisionMatrix matrix, BrowserSensitiveAutomationReadinessRequest request, bool m25Blocked)
    {
        if (m25Blocked)
            return matrix.For(BrowserSensitiveAutomationRoadmapOption.M25BExternalTestOwnedTarget);
        if (request.ProductAdminTrackRequested)
            return matrix.For(BrowserSensitiveAutomationRoadmapOption.AdminLicensingProductTrack);
        return matrix.For(BrowserSensitiveAutomationRoadmapOption.M35CriticalSubmitGate);
    }
}
