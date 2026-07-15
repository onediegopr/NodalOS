using System.Net;
using OneBrain.AgentOperations.Core.Runtime;

namespace OneBrain.Pilot;

public static class SelectiveRuntimeInspectorEndpointMapper
{
    public const string FeatureFlag = "NODAL_OS_RUNTIME_INSPECTOR_ENABLED";
    public const string JsonRoute = "/api/runtime/inspector";
    public const string HtmlRoute = "/runtime/inspector";

    public static IEndpointRouteBuilder MapSelectiveRuntimeInspector(
        this IEndpointRouteBuilder endpoints,
        IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentNullException.ThrowIfNull(environment);

        endpoints.MapGet(JsonRoute, async (HttpContext context) =>
        {
            if (!IsRequestAllowed(environment, context.Connection.RemoteIpAddress))
                return Results.NotFound(new { decision = "RUNTIME_INSPECTOR_LOCAL_DEV_ONLY" });

            ApplyReadOnlyHeaders(context.Response);
            var result = await new NodalOsControlledFixtureVerticalSliceScenario()
                .RunAsync(context.RequestAborted)
                .ConfigureAwait(false);
            return Results.Json(result.ToInspectorSnapshot());
        });

        endpoints.MapGet(HtmlRoute, async (HttpContext context) =>
        {
            if (!IsRequestAllowed(environment, context.Connection.RemoteIpAddress))
                return Results.NotFound();

            ApplyReadOnlyHeaders(context.Response);
            var result = await new NodalOsControlledFixtureVerticalSliceScenario()
                .RunAsync(context.RequestAborted)
                .ConfigureAwait(false);
            return Results.Content(
                RuntimeInspectorHtmlRenderer.Render(result),
                "text/html; charset=utf-8");
        });

        return endpoints;
    }

    public static bool IsEnabled(IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(environment);
        if (environment.IsDevelopment())
            return true;

        var value = Environment.GetEnvironmentVariable(FeatureFlag);
        return string.Equals(value, "1", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(value, "true", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(value, "yes", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsRequestAllowed(IHostEnvironment environment, IPAddress? remoteAddress) =>
        IsEnabled(environment) &&
        remoteAddress is not null && IPAddress.IsLoopback(remoteAddress);

    private static void ApplyReadOnlyHeaders(HttpResponse response)
    {
        response.Headers.CacheControl = "no-store";
        response.Headers.Pragma = "no-cache";
        response.Headers.XContentTypeOptions = "nosniff";
    }
}
