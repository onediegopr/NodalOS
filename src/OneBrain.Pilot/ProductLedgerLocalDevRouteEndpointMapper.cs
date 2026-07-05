using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Hosting;
using OneBrain.Core.Approval;

namespace OneBrain.Pilot;

public static class ProductLedgerLocalDevRouteEndpointMapper
{
    public const string LocalOnlyRouteResponseEvidenceMode =
        "LOCAL_ONLY_DEVELOPMENT_ONLY_HTTP_RESPONSE_PREVIEW_NO_EXECUTION";

    public static IEndpointRouteBuilder MapProductLedgerLocalDevRoutePreview(
        this IEndpointRouteBuilder endpoints,
        IHostEnvironment environment)
        => endpoints.MapProductLedgerLocalDevRoutePreview(environment, ProductLedgerOperatorSurfaceReadModelSource.FixtureSafe);

    public static IEndpointRouteBuilder MapProductLedgerLocalDevRoutePreview(
        this IEndpointRouteBuilder endpoints,
        IHostEnvironment environment,
        ProductLedgerOperatorSurfaceReadModelSource readModelSource)
    {
        if (!environment.IsDevelopment())
        {
            return endpoints;
        }

        endpoints.MapGet(
            ProductLedgerLocalDevRoutePreview.RouteTemplatePreview,
            () => RenderProductLedgerLocalDevRoutePreview(readModelSource));
        return endpoints;
    }

    public static IResult RenderProductLedgerLocalDevRoutePreview()
        => RenderProductLedgerLocalDevRoutePreview(ProductLedgerOperatorSurfaceReadModelSource.FixtureSafe);

    public static IResult RenderProductLedgerLocalDevRoutePreview(
        ProductLedgerOperatorSurfaceReadModelSource readModelSource)
    {
        var result = new ProductLedgerLocalDevRoutePreview().Render(
            ProductLedgerLocalDevRoutePreview.CreateDefaultLocalDevRequest(),
            readModelSource);

        return result.Decision == ProductLedgerLocalDevRoutePreviewDecision.RenderedLocalDevInternalPreview
            ? Results.Content(result.HtmlSnapshot, result.ContentType)
            : Results.NotFound();
    }
}
