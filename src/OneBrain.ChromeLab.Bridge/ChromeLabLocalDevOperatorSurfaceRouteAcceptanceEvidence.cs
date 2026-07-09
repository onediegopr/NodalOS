namespace OneBrain.ChromeLab.Bridge;

public enum ChromeLabLocalDevOperatorSurfaceRouteAcceptanceDecision
{
    Rejected,
    Accepted
}

public sealed record ChromeLabLocalDevOperatorSurfaceRouteAcceptancePacket(
    string EvidenceId,
    ChromeLabLocalDevOperatorSurfaceRouteAcceptanceDecision Decision,
    IReadOnlyList<string> Findings,
    string RouteId,
    string RoutePath,
    string Method,
    int StatusCode,
    string? SurfaceEvidenceId,
    string? ViewModelId,
    int ReadinessPercentage,
    bool LocalDevOnly,
    bool LoopbackOnly,
    bool ReadOnly,
    bool FailClosed,
    bool CacheDisabled,
    bool PayloadAvailable,
    bool ActionDisabled,
    bool ActionExecutable,
    bool ActionWiringAbsent,
    bool UnsafeCapabilitiesUnavailable,
    bool ReleaseCommercialReady,
    string SafeNextStep,
    string Status);

public sealed class ChromeLabLocalDevOperatorSurfaceRouteAcceptanceEvidence
{
    public const string EvidenceId = "chromelab.local-dev.operator-surface.route.acceptance.v1";

    public ChromeLabLocalDevOperatorSurfaceRouteAcceptancePacket Evaluate(
        ChromeLabLocalDevOperatorSurfaceRouteResponse? response)
    {
        if (response is null)
            return MissingResponse();

        var surface = response.Surface;
        var acceptance = response.Acceptance;
        var preview = surface?.ViewModel.ActionPreviews.Count == 1
            ? surface.ViewModel.ActionPreviews[0]
            : null;
        var findings = new List<string>();

        if (response.Decision != ChromeLabLocalDevOperatorSurfaceRouteDecision.ServedReadOnlyPreview)
            findings.Add("route-preview-not-served");
        if (response.StatusCode != 200)
            findings.Add("route-status-not-ok");
        if (!string.Equals(response.RouteId, ChromeLabLocalDevOperatorSurfaceReadOnlyRoute.RouteId, StringComparison.Ordinal))
            findings.Add("route-id-mismatch");
        if (!string.Equals(response.RoutePath, ChromeLabLocalDevOperatorSurfaceReadOnlyRoute.RoutePath, StringComparison.Ordinal))
            findings.Add("route-path-mismatch");
        if (!string.Equals(response.Method, ChromeLabLocalDevOperatorSurfaceReadOnlyRoute.Method, StringComparison.Ordinal))
            findings.Add("route-method-mismatch");
        if (!response.LocalDevOnly || !response.LoopbackOnly || !response.ReadOnly || !response.FailClosed)
            findings.Add("route-boundary-mismatch");
        if (!response.CacheDisabled)
            findings.Add("route-cache-must-stay-disabled");
        if (!response.PayloadAvailable || surface is null || acceptance is null)
            findings.Add("accepted-route-payload-missing");
        if (acceptance is not null &&
            acceptance.Decision != ChromeLabLocalDevOperatorSurfaceAcceptanceDecision.Accepted)
            findings.Add("surface-acceptance-rejected");
        if (surface is not null && acceptance is not null &&
            (!string.Equals(surface.ViewModel.ViewModelId, acceptance.ViewModelId, StringComparison.Ordinal) ||
             surface.ViewModel.Header.ReadinessPercentage != acceptance.ReadinessPercentage))
            findings.Add("surface-acceptance-mismatch");
        if (preview is null)
            findings.Add("single-action-preview-required");
        if (preview is not null && (!preview.Disabled || preview.Executable))
            findings.Add("action-preview-must-stay-disabled");
        if (preview is not null &&
            (preview.ProductiveCommandId is not null || preview.HandlerId is not null || preview.CallbackName is not null))
            findings.Add("action-wiring-must-stay-absent");
        if (acceptance is not null && !acceptance.UnsafeCapabilitiesUnavailable)
            findings.Add("unsafe-capability-available");
        if (acceptance?.ReleaseCommercialReady == true)
            findings.Add("release-commercial-must-stay-blocked");
        if (!string.Equals(
                response.SafeNextStep,
                "CHROMELAB_LOCAL_DEV_OPERATOR_SURFACE_ROUTE_ACCEPTANCE_OR_CLOSE",
                StringComparison.Ordinal))
            findings.Add("safe-next-step-mismatch");

        var accepted = findings.Count == 0;
        return new ChromeLabLocalDevOperatorSurfaceRouteAcceptancePacket(
            EvidenceId: EvidenceId,
            Decision: accepted
                ? ChromeLabLocalDevOperatorSurfaceRouteAcceptanceDecision.Accepted
                : ChromeLabLocalDevOperatorSurfaceRouteAcceptanceDecision.Rejected,
            Findings: findings.OrderBy(value => value, StringComparer.Ordinal).ToArray(),
            RouteId: response.RouteId,
            RoutePath: response.RoutePath,
            Method: response.Method,
            StatusCode: response.StatusCode,
            SurfaceEvidenceId: acceptance?.EvidenceId,
            ViewModelId: acceptance?.ViewModelId,
            ReadinessPercentage: acceptance?.ReadinessPercentage ?? 0,
            LocalDevOnly: response.LocalDevOnly,
            LoopbackOnly: response.LoopbackOnly,
            ReadOnly: response.ReadOnly,
            FailClosed: response.FailClosed,
            CacheDisabled: response.CacheDisabled,
            PayloadAvailable: response.PayloadAvailable,
            ActionDisabled: preview?.Disabled ?? true,
            ActionExecutable: preview?.Executable ?? false,
            ActionWiringAbsent: preview is null ||
                (preview.ProductiveCommandId is null && preview.HandlerId is null && preview.CallbackName is null),
            UnsafeCapabilitiesUnavailable: acceptance?.UnsafeCapabilitiesUnavailable ?? true,
            ReleaseCommercialReady: acceptance?.ReleaseCommercialReady ?? false,
            SafeNextStep: response.SafeNextStep,
            Status: response.Status);
    }

    private static ChromeLabLocalDevOperatorSurfaceRouteAcceptancePacket MissingResponse() =>
        new(
            EvidenceId: EvidenceId,
            Decision: ChromeLabLocalDevOperatorSurfaceRouteAcceptanceDecision.Rejected,
            Findings: ["missing-route-response"],
            RouteId: string.Empty,
            RoutePath: ChromeLabLocalDevOperatorSurfaceReadOnlyRoute.RoutePath,
            Method: ChromeLabLocalDevOperatorSurfaceReadOnlyRoute.Method,
            StatusCode: 0,
            SurfaceEvidenceId: null,
            ViewModelId: null,
            ReadinessPercentage: 0,
            LocalDevOnly: true,
            LoopbackOnly: true,
            ReadOnly: true,
            FailClosed: true,
            CacheDisabled: true,
            PayloadAvailable: false,
            ActionDisabled: true,
            ActionExecutable: false,
            ActionWiringAbsent: true,
            UnsafeCapabilitiesUnavailable: true,
            ReleaseCommercialReady: false,
            SafeNextStep: "PROVIDE_VALID_CHROMELAB_LOCAL_DEV_OPERATOR_SURFACE_ROUTE_RESPONSE",
            Status: "CHROMELAB_LOCAL_DEV_OPERATOR_SURFACE_ROUTE_ACCEPTANCE_REJECTED FAIL_CLOSED");
}
