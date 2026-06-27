namespace OneBrain.WindowsComputerUse;

public enum ComputerUseRequestedMode
{
    SafeMode,
    Containment,
    PerceptionFoundation,
    LiveRead,
    ActionExecution,
    ProductAutomation,
    BrowserLiveCdp
}

public enum ComputerUseSafeModeRequestStatus
{
    ReadySafeMode,
    BlockedByPolicy
}

public sealed record ComputerUseActiveSafeModeStatus(
    string CapabilityName,
    string CapabilityStatus,
    string SupportedMode,
    bool IsUsable,
    bool LiveReadPermitted,
    bool ActionAuthorityGranted,
    bool ProductAutomationEnabled,
    bool LiveDesktopAutomationEnabled,
    bool RealMouseKeyboardEnabled,
    bool RawScreenshotCaptureEnabled,
    string FailureBehavior,
    string IfCalledBehavior,
    string BlockedLiveReason,
    bool OperatorQaRequiredForLive,
    bool ExternalGoRequiredForLive,
    bool HumanPolicyDecisionRequiredForLive,
    string Wcu037044Status);

public sealed record ComputerUseSafeModeReadiness(
    string CapabilityName,
    bool SafeModeUsable,
    bool ContainmentPerceptionFoundationReady,
    bool EvidenceRedactionReady,
    bool BridgeHandoffReady,
    bool StaticBoundaryReady,
    bool LiveReadPermitted,
    bool ActionAuthorityGranted,
    bool ProductAutomationEnabled,
    IReadOnlyList<string> SupportedSafeModeSurfaces);

public sealed record ComputerUseSafeModeRequestResult(
    ComputerUseRequestedMode RequestedMode,
    ComputerUseSafeModeRequestStatus Status,
    string Reason,
    bool IsUsable,
    bool LiveReadPermitted,
    bool ActionAuthorityGranted,
    bool ProductAutomationEnabled,
    bool LiveDesktopAutomationEnabled,
    bool RealMouseKeyboardEnabled,
    bool RawScreenshotCaptureEnabled,
    bool ClipboardEnabled,
    bool LiveProviderCalled,
    bool RawScreenshotPresent,
    bool ClipboardPresent,
    string FailureBehavior,
    string IfCalledBehavior);

public sealed record ComputerUseSafeModeEvidenceSummary(
    string SummaryId,
    string CapabilityStatus,
    IReadOnlyList<string> SourceSignals,
    bool Redacted,
    bool RawScreenshotPresent,
    bool ClipboardPresent,
    bool LiveProviderCalled,
    bool ActionAuthorityGranted,
    bool LiveReadPermitted,
    bool ProductAutomationEnabled);

public sealed record ComputerUseSafeModeProductClaimResult(
    string ClaimText,
    bool Allowed,
    string Reason,
    bool ActionAuthorityGranted,
    bool LiveReadPermitted,
    bool ProductAutomationEnabled);

public static class ComputerUseActiveSafeModeCatalog
{
    public const string CapabilityName = "Windows Computer Use Control Plane";
    public const string ReadySafeMode = "READY_SAFE_MODE";
    public const string SupportedMode = "CONTAINMENT_PERCEPTION_FOUNDATION";
    public const string FailureBehavior = "FAIL_CLOSED";
    public const string IfCalledBehavior = "RETURNS_STRUCTURED_SAFE_MODE_RESULT";
    public const string BlockedLiveReason = "DISABLED_BY_POLICY_AND_EXTERNAL_NO_GO";

    public static ComputerUseActiveSafeModeStatus Current() =>
        new(
            CapabilityName,
            ReadySafeMode,
            SupportedMode,
            IsUsable: true,
            LiveReadPermitted: false,
            ActionAuthorityGranted: false,
            ProductAutomationEnabled: false,
            LiveDesktopAutomationEnabled: false,
            RealMouseKeyboardEnabled: false,
            RawScreenshotCaptureEnabled: false,
            FailureBehavior,
            IfCalledBehavior,
            BlockedLiveReason,
            OperatorQaRequiredForLive: true,
            ExternalGoRequiredForLive: true,
            HumanPolicyDecisionRequiredForLive: true,
            ComputerUseExternalAuditReconciliation.BlockedLivePrototypeStatus);

    public static ComputerUseSafeModeReadiness Readiness() =>
        new(
            CapabilityName,
            SafeModeUsable: true,
            ContainmentPerceptionFoundationReady: true,
            EvidenceRedactionReady: true,
            BridgeHandoffReady: true,
            StaticBoundaryReady: true,
            LiveReadPermitted: false,
            ActionAuthorityGranted: false,
            ProductAutomationEnabled: false,
            SupportedSafeModeSurfaces:
            [
                "containment",
                "perception-foundation",
                "redacted-evidence",
                "handoff-status",
                "static-boundary-status"
            ]);
}

public static class ComputerUseSafeModeProductClaimCatalog
{
    public static IReadOnlyList<string> AllowedClaims { get; } =
    [
        "Computer Use Control Plane - Safe Mode Ready",
        "Perception/Evidence/Redaction/Handoff foundation ready",
        "Live desktop control disabled by policy"
    ];

    public static IReadOnlyList<string> ProhibitedClaims { get; } =
    [
        "Controla tu PC real",
        "Live desktop automation ready",
        "Mouse/keyboard automation ready",
        "FlaUI/UIA live ready",
        "Screenshots/live screen capture ready",
        "real PC automation ready",
        "live desktop control ready"
    ];

    public static ComputerUseSafeModeProductClaimResult Evaluate(string claimText)
    {
        var prohibited = ProhibitedClaims.FirstOrDefault(claim => claimText.Contains(claim, StringComparison.OrdinalIgnoreCase));
        if (prohibited is not null)
        {
            return Result(claimText, Allowed: false, $"Prohibited live/product automation claim: {prohibited}.");
        }

        var allowed = AllowedClaims.Any(claim => claimText.Contains(claim, StringComparison.OrdinalIgnoreCase));
        return Result(
            claimText,
            allowed,
            allowed
                ? "Allowed safe-mode product boundary claim."
                : "Unknown claim; fail closed until reviewed.");
    }

    private static ComputerUseSafeModeProductClaimResult Result(string claimText, bool Allowed, string reason) =>
        new(
            claimText,
            Allowed,
            reason,
            ActionAuthorityGranted: false,
            LiveReadPermitted: false,
            ProductAutomationEnabled: false);
}
