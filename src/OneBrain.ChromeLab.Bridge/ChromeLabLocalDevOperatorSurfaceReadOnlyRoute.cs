using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace OneBrain.ChromeLab.Bridge;

public enum ChromeLabLocalDevOperatorSurfaceRouteDecision
{
    RejectedNotLocal,
    RejectedAcceptance,
    ServedReadOnlyPreview
}

public sealed record ChromeLabLocalDevOperatorSurfaceRouteResponse(
    string RouteId,
    string RoutePath,
    string Method,
    int StatusCode,
    ChromeLabLocalDevOperatorSurfaceRouteDecision Decision,
    string Status,
    bool LocalDevOnly,
    bool LoopbackOnly,
    bool ReadOnly,
    bool FailClosed,
    bool CacheDisabled,
    bool PayloadAvailable,
    ChromeLabLocalDevOperatorSurfaceResult? Surface,
    ChromeLabLocalDevOperatorSurfaceAcceptancePacket? Acceptance,
    string SafeNextStep);

public sealed class ChromeLabLocalDevOperatorSurfaceReadOnlyRoute
{
    public const string RouteId = "chromelab.local-dev.operator-surface.route.v1";
    public const string RoutePath = "/operator/local-dev/chromelab";
    public const string Method = "GET";

    public ChromeLabLocalDevOperatorSurfaceRouteResponse Handle(IPAddress? remoteAddress)
    {
        if (remoteAddress is null || !IPAddress.IsLoopback(remoteAddress))
            return RejectNotLocal();

        var surface = new ChromeLabLocalDevOperatorSurfacePresenter().Render(SafeRequest());
        var acceptance = new ChromeLabLocalDevOperatorSurfaceAcceptanceEvidence().Evaluate(surface);
        var accepted = acceptance.Decision == ChromeLabLocalDevOperatorSurfaceAcceptanceDecision.Accepted;

        return new ChromeLabLocalDevOperatorSurfaceRouteResponse(
            RouteId: RouteId,
            RoutePath: RoutePath,
            Method: Method,
            StatusCode: accepted ? StatusCodes.Status200OK : StatusCodes.Status503ServiceUnavailable,
            Decision: accepted
                ? ChromeLabLocalDevOperatorSurfaceRouteDecision.ServedReadOnlyPreview
                : ChromeLabLocalDevOperatorSurfaceRouteDecision.RejectedAcceptance,
            Status: accepted
                ? "CHROMELAB_LOCAL_DEV_OPERATOR_SURFACE_ROUTE_READY LOOPBACK_ONLY READ_ONLY FAIL_CLOSED NO_STORE"
                : "CHROMELAB_LOCAL_DEV_OPERATOR_SURFACE_ROUTE_REJECTED ACCEPTANCE_FAILED FAIL_CLOSED",
            LocalDevOnly: true,
            LoopbackOnly: true,
            ReadOnly: true,
            FailClosed: true,
            CacheDisabled: true,
            PayloadAvailable: accepted,
            Surface: accepted ? surface : null,
            Acceptance: acceptance,
            SafeNextStep: accepted
                ? "CHROMELAB_LOCAL_DEV_OPERATOR_SURFACE_ROUTE_ACCEPTANCE_OR_CLOSE"
                : "FIX_CHROMELAB_LOCAL_DEV_OPERATOR_SURFACE_ACCEPTANCE_BEFORE_ROUTE_PAYLOAD");
    }

    private static ChromeLabLocalDevOperatorSurfaceRouteResponse RejectNotLocal() =>
        new(
            RouteId: RouteId,
            RoutePath: RoutePath,
            Method: Method,
            StatusCode: StatusCodes.Status404NotFound,
            Decision: ChromeLabLocalDevOperatorSurfaceRouteDecision.RejectedNotLocal,
            Status: "CHROMELAB_LOCAL_DEV_OPERATOR_SURFACE_ROUTE_NOT_FOUND NON_LOOPBACK_REJECTED FAIL_CLOSED",
            LocalDevOnly: true,
            LoopbackOnly: true,
            ReadOnly: true,
            FailClosed: true,
            CacheDisabled: true,
            PayloadAvailable: false,
            Surface: null,
            Acceptance: null,
            SafeNextStep: "USE_LOOPBACK_FOR_CHROMELAB_LOCAL_DEV_OPERATOR_SURFACE");

    private static ChromeLabLocalDevOperatorSurfaceRequest SafeRequest() =>
        new(
            ExplicitLocalDevOperatorSurfacePrepScope: true,
            RequestsLiveBrowserExecution: false,
            RequestsChromeLaunch: false,
            RequestsCdpConnection: false,
            RequestsExternalBrowserAutomation: false,
            RequestsNetworkProvider: false,
            RequestsUserCustomerData: false,
            RequestsProductionRuntime: false,
            RequestsPublicProductPromotion: false,
            RequestsProductAuthority: false,
            RequestsApprovalOrCommandExecution: false,
            RequestsReleaseCommercial: false);
}

public static class ChromeLabLocalDevOperatorSurfaceEndpointExtensions
{
    public static IEndpointRouteBuilder MapChromeLabLocalDevOperatorSurfaceReadOnlyRoute(
        this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet(ChromeLabLocalDevOperatorSurfaceReadOnlyRoute.RoutePath, (HttpContext context) =>
        {
            ApplyNoStore(context);
            var response = new ChromeLabLocalDevOperatorSurfaceReadOnlyRoute()
                .Handle(context.Connection.RemoteIpAddress);
            return ToJsonHttpResult(response);
        }).WithName("ChromeLabLocalDevOperatorSurfaceReadOnly");

        endpoints.MapGet(ChromeLabLocalDevOperatorSurfaceHtmlRenderer.RoutePath, (HttpContext context) =>
        {
            ApplyNoStore(context);
            var response = new ChromeLabLocalDevOperatorSurfaceReadOnlyRoute()
                .Handle(context.Connection.RemoteIpAddress);
            if (response.StatusCode == StatusCodes.Status404NotFound)
                return Results.NotFound();

            var html = new ChromeLabLocalDevOperatorSurfaceHtmlRenderer().Render(response);
            return Results.Content(html.Html, html.ContentType, statusCode: html.StatusCode);
        }).WithName("ChromeLabLocalDevOperatorSurfaceHtmlReadOnly");

        return endpoints;
    }

    private static IResult ToJsonHttpResult(ChromeLabLocalDevOperatorSurfaceRouteResponse response)
    {
        if (response.StatusCode == StatusCodes.Status404NotFound)
            return Results.NotFound();

        return Results.Json(response, statusCode: response.StatusCode);
    }

    private static void ApplyNoStore(HttpContext context) =>
        context.Response.Headers.CacheControl = "no-store";
}
