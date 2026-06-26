namespace OneBrain.WindowsComputerUse;

public enum ComputerUseReadOnlyLiveGateId
{
    WcuLiveReadDisabledByDefault,
    WcuLiveReadDevFlagRequired,
    WcuNoInputInjectionGate,
    WcuNoWindowManipulationGate,
    WcuNoClipboardGate,
    WcuNoRawScreenshotGate,
    WcuNoCredentialValueCaptureGate,
    WcuNoUacAdminAutomationGate,
    WcuEventStreamNoActionTriggerGate,
    WcuEvidenceRedactionRequiredGate,
    WcuAuditLogRequiredGate,
    WcuKillSwitchRequiredGate,
    WcuAllowlistedTestAppsOnlyGate,
    WcuHumanOperatorConfirmationGate
}

public enum ComputerUseReadOnlyLiveGateStatus
{
    Pass,
    Blocked,
    RequiresHumanDecision,
    NotEvaluated
}

public enum ComputerUseReadOnlyLiveReadinessClassification
{
    ReadyForDesignReview,
    ReadyForAudit,
    NotReadyForLive,
    BlockedByPolicy,
    RequiresHumanDecision
}

public sealed record ComputerUseReadOnlyLiveGateDefinition(
    ComputerUseReadOnlyLiveGateId GateId,
    string CanonicalName,
    string Requirement,
    bool RequiredForPrototype);

public sealed record ComputerUseReadOnlyLiveKillSwitchState(
    bool GlobalDisabled,
    bool ProviderDisabled,
    bool EventStreamDisabled,
    bool VisualOcrDisabled,
    bool EvidenceCaptureDisabled,
    bool EmergencyFailClosed);

public sealed record ComputerUseReadOnlyLiveGateRequest(
    bool DevFlagEnabled = false,
    bool HumanOperatorConfirmed = false,
    bool AuditLogConfigured = false,
    bool EvidenceRedactionRequired = true,
    bool AllowlistedTestAppOnly = false,
    bool AllowsInputInjection = false,
    bool AllowsWindowManipulation = false,
    bool AllowsClipboard = false,
    bool AllowsRawScreenshots = false,
    bool AllowsCredentialValueCapture = false,
    bool AllowsUacAdminAutomation = false,
    bool EventStreamCanTriggerActions = false,
    bool AttemptsLiveRead = false,
    ComputerUseReadOnlyLiveKillSwitchState? KillSwitch = null);

public sealed record ComputerUseReadOnlyLiveGateEvaluation(
    ComputerUseReadOnlyLiveGateId GateId,
    string CanonicalName,
    ComputerUseReadOnlyLiveGateStatus Status,
    string Reason,
    bool ActionAuthorityGranted);

public sealed record ComputerUseReadOnlyLiveDesignGateResult(
    IReadOnlyList<ComputerUseReadOnlyLiveGateEvaluation> Gates,
    ComputerUseReadOnlyLiveReadinessClassification Readiness,
    bool LiveReadPermitted,
    bool ActionAuthorityGranted,
    bool ProductAutomationEnabled,
    bool RequiresAudit,
    bool RequiresHumanDecision,
    ComputerUseReadOnlyLiveKillSwitchState KillSwitch,
    IReadOnlyList<string> Reasons);

public static class ComputerUseReadOnlyLiveGateCatalog
{
    public static IReadOnlyList<ComputerUseReadOnlyLiveGateDefinition> RequiredGates { get; } =
    [
        Gate(ComputerUseReadOnlyLiveGateId.WcuLiveReadDisabledByDefault, "WCU_LIVE_READ_DISABLED_BY_DEFAULT", "Live read remains disabled unless all future audit gates pass."),
        Gate(ComputerUseReadOnlyLiveGateId.WcuLiveReadDevFlagRequired, "WCU_LIVE_READ_DEV_FLAG_REQUIRED", "A future prototype requires explicit dev-only opt-in."),
        Gate(ComputerUseReadOnlyLiveGateId.WcuNoInputInjectionGate, "WCU_NO_INPUT_INJECTION_GATE", "Mouse, keyboard, SendInput, and equivalent input channels are prohibited."),
        Gate(ComputerUseReadOnlyLiveGateId.WcuNoWindowManipulationGate, "WCU_NO_WINDOW_MANIPULATION_GATE", "Foreground, focus, z-order, PostMessage, SendMessage, and window manipulation are prohibited."),
        Gate(ComputerUseReadOnlyLiveGateId.WcuNoClipboardGate, "WCU_NO_CLIPBOARD_GATE", "Clipboard reads and writes are prohibited."),
        Gate(ComputerUseReadOnlyLiveGateId.WcuNoRawScreenshotGate, "WCU_NO_RAW_SCREENSHOT_GATE", "Raw screenshot persistence is prohibited by default."),
        Gate(ComputerUseReadOnlyLiveGateId.WcuNoCredentialValueCaptureGate, "WCU_NO_CREDENTIAL_VALUE_CAPTURE_GATE", "Credential values, OTPs, tokens, and secret values must not be captured."),
        Gate(ComputerUseReadOnlyLiveGateId.WcuNoUacAdminAutomationGate, "WCU_NO_UAC_ADMIN_AUTOMATION_GATE", "UAC/admin prompts require human handoff and cannot be automated."),
        Gate(ComputerUseReadOnlyLiveGateId.WcuEventStreamNoActionTriggerGate, "WCU_EVENT_STREAM_NO_ACTION_TRIGGER_GATE", "UIA events are observations and cannot trigger execution."),
        Gate(ComputerUseReadOnlyLiveGateId.WcuEvidenceRedactionRequiredGate, "WCU_EVIDENCE_REDACTION_REQUIRED_GATE", "Evidence must be redacted before reporting or persistence."),
        Gate(ComputerUseReadOnlyLiveGateId.WcuAuditLogRequiredGate, "WCU_AUDIT_LOG_REQUIRED_GATE", "Any future prototype requires an audit log before collection."),
        Gate(ComputerUseReadOnlyLiveGateId.WcuKillSwitchRequiredGate, "WCU_KILL_SWITCH_REQUIRED_GATE", "Global and per-provider fail-closed kill switches are required."),
        Gate(ComputerUseReadOnlyLiveGateId.WcuAllowlistedTestAppsOnlyGate, "WCU_ALLOWLISTED_TEST_APPS_ONLY_GATE", "Future live-read prototype scope is limited to allowlisted test apps."),
        Gate(ComputerUseReadOnlyLiveGateId.WcuHumanOperatorConfirmationGate, "WCU_HUMAN_OPERATOR_CONFIRMATION_GATE", "A human operator must explicitly confirm any future gated read-only prototype run.")
    ];

    public static ComputerUseReadOnlyLiveDesignGateResult Evaluate(ComputerUseReadOnlyLiveGateRequest request)
    {
        var killSwitch = request.KillSwitch ?? DisabledKillSwitch();
        var evaluations = new List<ComputerUseReadOnlyLiveGateEvaluation>();

        evaluations.Add(Evaluate(ComputerUseReadOnlyLiveGateId.WcuLiveReadDisabledByDefault, !request.AttemptsLiveRead || killSwitch.GlobalDisabled, "Live read is disabled by default."));
        evaluations.Add(Evaluate(ComputerUseReadOnlyLiveGateId.WcuLiveReadDevFlagRequired, request.DevFlagEnabled && !killSwitch.GlobalDisabled, "Explicit dev-only flag is required for any future prototype."));
        evaluations.Add(Evaluate(ComputerUseReadOnlyLiveGateId.WcuNoInputInjectionGate, !request.AllowsInputInjection, "Input injection remains prohibited."));
        evaluations.Add(Evaluate(ComputerUseReadOnlyLiveGateId.WcuNoWindowManipulationGate, !request.AllowsWindowManipulation, "Window manipulation remains prohibited."));
        evaluations.Add(Evaluate(ComputerUseReadOnlyLiveGateId.WcuNoClipboardGate, !request.AllowsClipboard, "Clipboard access remains prohibited."));
        evaluations.Add(Evaluate(ComputerUseReadOnlyLiveGateId.WcuNoRawScreenshotGate, !request.AllowsRawScreenshots, "Raw screenshot persistence remains prohibited."));
        evaluations.Add(Evaluate(ComputerUseReadOnlyLiveGateId.WcuNoCredentialValueCaptureGate, !request.AllowsCredentialValueCapture, "Credential value capture remains prohibited."));
        evaluations.Add(Evaluate(ComputerUseReadOnlyLiveGateId.WcuNoUacAdminAutomationGate, !request.AllowsUacAdminAutomation, "UAC/admin automation remains prohibited."));
        evaluations.Add(Evaluate(ComputerUseReadOnlyLiveGateId.WcuEventStreamNoActionTriggerGate, !request.EventStreamCanTriggerActions, "Events cannot trigger actions."));
        evaluations.Add(Evaluate(ComputerUseReadOnlyLiveGateId.WcuEvidenceRedactionRequiredGate, request.EvidenceRedactionRequired, "Evidence redaction is required."));
        evaluations.Add(Evaluate(ComputerUseReadOnlyLiveGateId.WcuAuditLogRequiredGate, request.AuditLogConfigured, "Audit log is required before any future prototype."));
        evaluations.Add(Evaluate(ComputerUseReadOnlyLiveGateId.WcuKillSwitchRequiredGate, killSwitch.GlobalDisabled && killSwitch.ProviderDisabled && killSwitch.EventStreamDisabled && killSwitch.VisualOcrDisabled && killSwitch.EmergencyFailClosed, "Fail-closed kill switches are required."));
        evaluations.Add(Evaluate(ComputerUseReadOnlyLiveGateId.WcuAllowlistedTestAppsOnlyGate, request.AllowlistedTestAppOnly, "Only allowlisted test apps are in scope."));
        evaluations.Add(Evaluate(ComputerUseReadOnlyLiveGateId.WcuHumanOperatorConfirmationGate, request.HumanOperatorConfirmed, "Human operator confirmation is required."));

        var blocked = evaluations.Any(e => e.Status == ComputerUseReadOnlyLiveGateStatus.Blocked);
        var requiresHumanDecision = !request.HumanOperatorConfirmed || evaluations.Any(e => e.Status == ComputerUseReadOnlyLiveGateStatus.RequiresHumanDecision);
        var readiness = blocked
            ? ComputerUseReadOnlyLiveReadinessClassification.NotReadyForLive
            : requiresHumanDecision
                ? ComputerUseReadOnlyLiveReadinessClassification.RequiresHumanDecision
                : ComputerUseReadOnlyLiveReadinessClassification.ReadyForAudit;

        return new ComputerUseReadOnlyLiveDesignGateResult(
            evaluations,
            readiness,
            LiveReadPermitted: false,
            ActionAuthorityGranted: false,
            ProductAutomationEnabled: false,
            RequiresAudit: true,
            RequiresHumanDecision: requiresHumanDecision,
            killSwitch,
            evaluations.Where(e => e.Status != ComputerUseReadOnlyLiveGateStatus.Pass).Select(e => e.Reason).Distinct(StringComparer.OrdinalIgnoreCase).ToArray());
    }

    public static ComputerUseReadOnlyLiveKillSwitchState DisabledKillSwitch() =>
        new(
            GlobalDisabled: true,
            ProviderDisabled: true,
            EventStreamDisabled: true,
            VisualOcrDisabled: true,
            EvidenceCaptureDisabled: false,
            EmergencyFailClosed: true);

    private static ComputerUseReadOnlyLiveGateDefinition Gate(ComputerUseReadOnlyLiveGateId gateId, string canonicalName, string requirement) =>
        new(gateId, canonicalName, requirement, RequiredForPrototype: true);

    private static ComputerUseReadOnlyLiveGateEvaluation Evaluate(ComputerUseReadOnlyLiveGateId gateId, bool passed, string reason)
    {
        var definition = RequiredGates.Single(g => g.GateId == gateId);
        return new ComputerUseReadOnlyLiveGateEvaluation(
            gateId,
            definition.CanonicalName,
            passed ? ComputerUseReadOnlyLiveGateStatus.Pass : ComputerUseReadOnlyLiveGateStatus.Blocked,
            reason,
            ActionAuthorityGranted: false);
    }
}
