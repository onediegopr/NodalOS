using System.Net;

namespace OneBrain.Pilot;

public static class MissionControlProductHandoffExportEndpointMapper
{
    public const string MarkdownRoute = "/mission/handoff.md";
    public const string MarkdownContentType = "text/markdown; charset=utf-8";

    public static IEndpointRouteBuilder MapMissionControlProductHandoffExport(
        this IEndpointRouteBuilder endpoints,
        Func<CancellationToken, ValueTask<MissionControlProductShellSnapshot>>? snapshotProvider = null)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        snapshotProvider ??= cancellationToken =>
            MissionControlProductShellEndpointMapper.BuildSnapshotAsync(cancellationToken);

        endpoints.MapGet(MarkdownRoute, async Task<IResult> (HttpContext context) =>
        {
            if (!IsRequestAllowed(context.Connection.RemoteIpAddress))
                return Results.NotFound();

            ApplyExportHeaders(context.Response);
            var snapshot = await snapshotProvider(context.RequestAborted).ConfigureAwait(false);
            if (!snapshot.RealMissionDraft)
            {
                return Results.Content(
                    "# NODAL OS — Handoff no disponible\n\nCreá una misión real antes de exportar su estado, timeline y evidencia.\n",
                    MarkdownContentType,
                    statusCode: StatusCodes.Status409Conflict);
            }

            context.Response.Headers.ContentDisposition =
                $"attachment; filename=\"nodal-os-handoff-{FileSegment(snapshot.MissionId)}.md\"";
            return Results.Content(
                MissionControlProductHandoffMarkdownRenderer.Render(snapshot),
                MarkdownContentType,
                statusCode: StatusCodes.Status200OK);
        });

        return endpoints;
    }

    public static bool IsRequestAllowed(IPAddress? remoteAddress) =>
        MissionControlProductShellEndpointMapper.IsRequestAllowed(remoteAddress);

    private static string FileSegment(string? value)
    {
        var segment = new string((value ?? string.Empty)
            .Where(character => char.IsAsciiLetterOrDigit(character) || character is '-' or '_')
            .Take(80)
            .ToArray());
        return string.IsNullOrWhiteSpace(segment) ? "mission" : segment;
    }

    private static void ApplyExportHeaders(HttpResponse response)
    {
        response.Headers.CacheControl = "no-store";
        response.Headers.Pragma = "no-cache";
        response.Headers.XContentTypeOptions = "nosniff";
        response.Headers.ContentSecurityPolicy = "default-src 'none'; frame-ancestors 'none'; base-uri 'none'; form-action 'none'";
        response.Headers["Referrer-Policy"] = "no-referrer";
    }
}
