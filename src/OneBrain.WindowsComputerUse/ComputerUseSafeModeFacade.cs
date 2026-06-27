namespace OneBrain.WindowsComputerUse;

public sealed class ComputerUseSafeModeFacade
{
    public ComputerUseActiveSafeModeStatus GetStatus() =>
        ComputerUseActiveSafeModeCatalog.Current();

    public ComputerUseSafeModeReadiness GetReadiness() =>
        ComputerUseActiveSafeModeCatalog.Readiness();

    public ComputerUseSafeModeRequestResult EvaluateRequestedMode(ComputerUseRequestedMode requestedMode) =>
        requestedMode switch
        {
            ComputerUseRequestedMode.SafeMode => Ready(requestedMode, "Safe mode control-plane/perception foundation is available."),
            ComputerUseRequestedMode.Containment => Ready(requestedMode, "Containment status is available in safe mode."),
            ComputerUseRequestedMode.PerceptionFoundation => Ready(requestedMode, "Perception foundation status is available in safe mode."),
            ComputerUseRequestedMode.LiveRead => CreateBlockedLiveAttemptResult(requestedMode, ComputerUseActiveSafeModeCatalog.BlockedLiveReason),
            ComputerUseRequestedMode.ActionExecution => CreateBlockedLiveAttemptResult(requestedMode, "ACTION_EXECUTION_DISABLED_BY_POLICY"),
            ComputerUseRequestedMode.ProductAutomation => CreateBlockedLiveAttemptResult(requestedMode, "PRODUCT_AUTOMATION_DISABLED_BY_POLICY"),
            ComputerUseRequestedMode.BrowserLiveCdp => CreateBlockedLiveAttemptResult(requestedMode, "BROWSER_LIVE_CDP_DISABLED_BY_POLICY"),
            _ => CreateBlockedLiveAttemptResult(requestedMode, "UNKNOWN_MODE_FAIL_CLOSED")
        };

    public ComputerUseSafeModeRequestResult CreateBlockedLiveAttemptResult(string reason) =>
        CreateBlockedLiveAttemptResult(ComputerUseRequestedMode.LiveRead, reason);

    public ComputerUseSafeModeRequestResult CreateBlockedLiveAttemptResult(ComputerUseRequestedMode requestedMode, string reason) =>
        new(
            requestedMode,
            ComputerUseSafeModeRequestStatus.BlockedByPolicy,
            reason,
            IsUsable: false,
            LiveReadPermitted: false,
            ActionAuthorityGranted: false,
            ProductAutomationEnabled: false,
            LiveDesktopAutomationEnabled: false,
            RealMouseKeyboardEnabled: false,
            RawScreenshotCaptureEnabled: false,
            ClipboardEnabled: false,
            LiveProviderCalled: false,
            RawScreenshotPresent: false,
            ClipboardPresent: false,
            ComputerUseActiveSafeModeCatalog.FailureBehavior,
            ComputerUseActiveSafeModeCatalog.IfCalledBehavior);

    public ComputerUseSafeModeEvidenceSummary CreateSafeModeEvidenceSummary(
        string summaryId,
        IReadOnlyList<string>? sourceSignals = null) =>
        new(
            summaryId,
            ComputerUseActiveSafeModeCatalog.ReadySafeMode,
            sourceSignals ?? [],
            Redacted: true,
            RawScreenshotPresent: false,
            ClipboardPresent: false,
            LiveProviderCalled: false,
            ActionAuthorityGranted: false,
            LiveReadPermitted: false,
            ProductAutomationEnabled: false);

    private static ComputerUseSafeModeRequestResult Ready(ComputerUseRequestedMode requestedMode, string reason) =>
        new(
            requestedMode,
            ComputerUseSafeModeRequestStatus.ReadySafeMode,
            reason,
            IsUsable: true,
            LiveReadPermitted: false,
            ActionAuthorityGranted: false,
            ProductAutomationEnabled: false,
            LiveDesktopAutomationEnabled: false,
            RealMouseKeyboardEnabled: false,
            RawScreenshotCaptureEnabled: false,
            ClipboardEnabled: false,
            LiveProviderCalled: false,
            RawScreenshotPresent: false,
            ClipboardPresent: false,
            ComputerUseActiveSafeModeCatalog.FailureBehavior,
            ComputerUseActiveSafeModeCatalog.IfCalledBehavior);
}
