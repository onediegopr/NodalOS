namespace OneBrain.ChromeLab.Bridge;

public enum ChromeLabLocalDevOperatorSurfaceAcceptanceDecision
{
    Rejected,
    Accepted
}

public sealed record ChromeLabLocalDevOperatorSurfaceAcceptancePacket(
    string EvidenceId,
    ChromeLabLocalDevOperatorSurfaceAcceptanceDecision Decision,
    IReadOnlyList<string> Findings,
    string ViewModelId,
    string SurfaceStatus,
    int ReadinessPercentage,
    IReadOnlyList<string> SectionIds,
    string? ActionId,
    string? BlockedFrontier,
    string? RequiredOperatorSignal,
    IReadOnlyList<string> RequiredEvidence,
    bool ActionDisabled,
    bool ActionExecutable,
    bool ActionWiringAbsent,
    bool LocalDevOnly,
    bool ReadOnly,
    bool FailClosed,
    bool UnsafeCapabilitiesUnavailable,
    bool ReleaseCommercialReady,
    string SafeNextStep,
    string StatusText);

public sealed class ChromeLabLocalDevOperatorSurfaceAcceptanceEvidence
{
    public const string EvidenceId = "chromelab.local-dev.operator-surface.acceptance.v1";

    private static readonly string[] RequiredSections =
    [
        "status",
        "limits",
        "blockers",
        "operator-signal",
        "safe-next-step"
    ];

    public ChromeLabLocalDevOperatorSurfaceAcceptancePacket Evaluate(
        ChromeLabLocalDevOperatorSurfaceResult? result)
    {
        if (result is null)
            return MissingResult();

        var view = result.ViewModel;
        var sectionIds = view.Sections.Select(section => section.SectionId).ToArray();
        var preview = view.ActionPreviews.Count == 1 ? view.ActionPreviews[0] : null;
        var findings = new List<string>();

        if (result.Decision != ChromeLabLocalDevOperatorSurfaceDecision.RenderedPreview)
            findings.Add("surface-not-rendered");
        if (result.Blockers.Count != 0 || view.Blockers.Count != 0)
            findings.Add("surface-has-blockers");
        if (!string.Equals(view.ViewModelId, "chromelab.local-dev.operator-surface-prep.v1", StringComparison.Ordinal))
            findings.Add("unexpected-view-model-id");
        if (view.Header.ReadinessPercentage != 27)
            findings.Add("unexpected-readiness");
        if (RequiredSections.Any(required => !sectionIds.Contains(required, StringComparer.Ordinal)))
            findings.Add("required-section-missing");
        if (preview is null)
            findings.Add("single-action-preview-required");
        if (preview is not null && (!preview.Disabled || preview.Executable))
            findings.Add("action-preview-must-stay-disabled");
        if (preview is not null &&
            (preview.ProductiveCommandId is not null || preview.HandlerId is not null || preview.CallbackName is not null))
            findings.Add("action-wiring-must-stay-absent");
        if (preview is not null && !string.Equals(
                preview.BlockedFrontier,
                "CHROMELAB_LIVE_BROWSER_EXECUTION_AUTHORITY",
                StringComparison.Ordinal))
            findings.Add("blocked-frontier-mismatch");
        if (preview is not null && !string.Equals(
                preview.RequiredOperatorSignal,
                "explicit-chromelab-local-dev-frontier",
                StringComparison.Ordinal))
            findings.Add("operator-signal-mismatch");
        if (!view.LocalDevOnly || !view.ReadOnly || !view.FailClosed)
            findings.Add("surface-boundary-mismatch");
        if (!UnsafeCapabilitiesUnavailable(view))
            findings.Add("unsafe-capability-available");
        if (view.ReleaseCommercialReady)
            findings.Add("release-commercial-must-stay-blocked");
        if (!string.Equals(
                view.SafeNextStep,
                "CHROMELAB_LOCAL_DEV_OPERATOR_SURFACE_FOLLOW_UP_OR_CLOSE",
                StringComparison.Ordinal))
            findings.Add("safe-next-step-mismatch");

        var accepted = findings.Count == 0;
        return new ChromeLabLocalDevOperatorSurfaceAcceptancePacket(
            EvidenceId: EvidenceId,
            Decision: accepted
                ? ChromeLabLocalDevOperatorSurfaceAcceptanceDecision.Accepted
                : ChromeLabLocalDevOperatorSurfaceAcceptanceDecision.Rejected,
            Findings: findings.OrderBy(value => value, StringComparer.Ordinal).ToArray(),
            ViewModelId: view.ViewModelId,
            SurfaceStatus: view.Header.Status,
            ReadinessPercentage: view.Header.ReadinessPercentage,
            SectionIds: sectionIds,
            ActionId: preview?.ActionId,
            BlockedFrontier: preview?.BlockedFrontier,
            RequiredOperatorSignal: preview?.RequiredOperatorSignal,
            RequiredEvidence: preview?.RequiredEvidence ?? [],
            ActionDisabled: preview?.Disabled ?? true,
            ActionExecutable: preview?.Executable ?? false,
            ActionWiringAbsent: preview is null ||
                (preview.ProductiveCommandId is null && preview.HandlerId is null && preview.CallbackName is null),
            LocalDevOnly: view.LocalDevOnly,
            ReadOnly: view.ReadOnly,
            FailClosed: view.FailClosed,
            UnsafeCapabilitiesUnavailable: UnsafeCapabilitiesUnavailable(view),
            ReleaseCommercialReady: view.ReleaseCommercialReady,
            SafeNextStep: view.SafeNextStep,
            StatusText: view.StatusText);
    }

    private static bool UnsafeCapabilitiesUnavailable(ChromeLabLocalDevOperatorSurfaceViewModel view) =>
        !view.LiveBrowserExecutionAvailable &&
        !view.ChromeLaunchAvailable &&
        !view.CdpConnectionAvailable &&
        !view.ExternalBrowserAutomationAvailable &&
        !view.NetworkProviderAvailable &&
        !view.UserCustomerDataAvailable &&
        !view.ProductionRuntimeAvailable &&
        !view.PublicProductPromotionAvailable &&
        !view.ProductAuthorityAvailable &&
        !view.ApprovalOrCommandExecutionAvailable;

    private static ChromeLabLocalDevOperatorSurfaceAcceptancePacket MissingResult() =>
        new(
            EvidenceId: EvidenceId,
            Decision: ChromeLabLocalDevOperatorSurfaceAcceptanceDecision.Rejected,
            Findings: ["missing-surface-result"],
            ViewModelId: string.Empty,
            SurfaceStatus: "FAIL_CLOSED",
            ReadinessPercentage: 0,
            SectionIds: [],
            ActionId: null,
            BlockedFrontier: "CHROMELAB_LOCAL_DEV_OPERATOR_SURFACE_ACCEPTANCE",
            RequiredOperatorSignal: "valid-local-dev-surface-result",
            RequiredEvidence: [],
            ActionDisabled: true,
            ActionExecutable: false,
            ActionWiringAbsent: true,
            LocalDevOnly: false,
            ReadOnly: true,
            FailClosed: true,
            UnsafeCapabilitiesUnavailable: true,
            ReleaseCommercialReady: false,
            SafeNextStep: "PROVIDE_VALID_CHROMELAB_LOCAL_DEV_OPERATOR_SURFACE_RESULT",
            StatusText: "CHROMELAB_LOCAL_DEV_OPERATOR_SURFACE_ACCEPTANCE_REJECTED FAIL_CLOSED");
}
