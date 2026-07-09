using System.Net;
using System.Text;

namespace OneBrain.ChromeLab.Bridge;

public enum ChromeLabLocalDevOperatorSurfaceHtmlDecision
{
    Rejected,
    Rendered
}

public sealed record ChromeLabLocalDevOperatorSurfaceHtmlResult(
    ChromeLabLocalDevOperatorSurfaceHtmlDecision Decision,
    int StatusCode,
    string ContentType,
    string Html,
    IReadOnlyList<string> Findings,
    bool LocalDevOnly,
    bool ReadOnly,
    bool FailClosed,
    bool ScriptsAbsent,
    bool FormsAbsent,
    bool ExternalResourcesAbsent,
    string SafeNextStep);

public sealed class ChromeLabLocalDevOperatorSurfaceHtmlRenderer
{
    public const string RoutePath = "/operator/local-dev/chromelab/view";
    public const string ContentType = "text/html; charset=utf-8";

    public ChromeLabLocalDevOperatorSurfaceHtmlResult Render(
        ChromeLabLocalDevOperatorSurfaceRouteResponse? route)
    {
        var findings = Validate(route);
        if (findings.Count != 0 || route?.Surface is null || route.Acceptance is null)
            return Rejected(findings);

        var view = route.Surface.ViewModel;
        var preview = view.ActionPreviews.Single();
        var html = new StringBuilder();
        html.AppendLine("<!doctype html>");
        html.AppendLine("<html lang=\"en\">");
        html.AppendLine("<head>");
        html.AppendLine("<meta charset=\"utf-8\">");
        html.AppendLine("<meta name=\"viewport\" content=\"width=device-width,initial-scale=1\">");
        html.AppendLine($"<title>{Encode(view.Header.Title)} Operator Surface</title>");
        html.AppendLine("<style>");
        html.AppendLine("body{font-family:system-ui,sans-serif;margin:0;background:#0d1117;color:#f5f7fa}main{max-width:980px;margin:0 auto;padding:32px}header,section,aside{background:#161b22;border:1px solid #30363d;border-radius:12px;padding:20px;margin-bottom:16px}h1,h2{margin-top:0}.meta{color:#aab4c0}.badge{display:inline-block;padding:4px 8px;border:1px solid #4f8dff;border-radius:999px;margin:2px}.blocked{border-color:#8b3a3a}.action{opacity:.72}.value{font-family:ui-monospace,monospace;color:#8ab4ff}ul{padding-left:22px}");
        html.AppendLine("</style>");
        html.AppendLine("</head>");
        html.AppendLine("<body><main>");
        html.AppendLine("<header>");
        html.AppendLine($"<h1>{Encode(view.Header.Title)} Operator Surface</h1>");
        html.AppendLine($"<p class=\"meta\">Status: <span class=\"value\">{Encode(view.Header.Status)}</span></p>");
        html.AppendLine($"<p class=\"meta\">Readiness: <strong>{view.Header.ReadinessPercentage}%</strong></p>");
        html.AppendLine("<p>");
        foreach (var notice in view.Header.Notices)
            html.AppendLine($"<span class=\"badge\">{Encode(notice)}</span>");
        html.AppendLine("</p></header>");

        foreach (var section in view.Sections)
        {
            var cssClass = section.Severity == ChromeLabLocalDevOperatorSurfaceSeverity.Blocker
                ? " class=\"blocked\""
                : string.Empty;
            html.AppendLine($"<section{cssClass} data-section-id=\"{Encode(section.SectionId)}\">");
            html.AppendLine($"<h2>{Encode(section.Title)}</h2>");
            html.AppendLine($"<p class=\"value\">{Encode(section.Status)}</p><ul>");
            foreach (var line in section.Lines)
                html.AppendLine($"<li>{Encode(line)}</li>");
            html.AppendLine("</ul></section>");
        }

        html.AppendLine("<aside class=\"action blocked\" aria-disabled=\"true\">");
        html.AppendLine($"<h2>{Encode(preview.Label)}</h2>");
        html.AppendLine($"<p>Risk: <span class=\"value\">{Encode(preview.RiskLabel)}</span></p>");
        html.AppendLine($"<p>{Encode(preview.BlockedReason)}</p>");
        html.AppendLine($"<p>Blocked frontier: <span class=\"value\">{Encode(preview.BlockedFrontier)}</span></p>");
        html.AppendLine($"<p>Required operator signal: <span class=\"value\">{Encode(preview.RequiredOperatorSignal)}</span></p>");
        html.AppendLine("<h2>Required evidence</h2><ul>");
        foreach (var evidence in preview.RequiredEvidence)
            html.AppendLine($"<li>{Encode(evidence)}</li>");
        html.AppendLine("</ul><p><strong>Action unavailable in this read-only surface.</strong></p></aside>");

        html.AppendLine("<section>");
        html.AppendLine("<h2>Acceptance Evidence</h2>");
        html.AppendLine($"<p class=\"value\">{Encode(route.Acceptance.EvidenceId)}</p>");
        html.AppendLine($"<p>Decision: {Encode(route.Acceptance.Decision.ToString())}</p>");
        html.AppendLine($"<p>Safe next step: <span class=\"value\">{Encode(route.SafeNextStep)}</span></p>");
        html.AppendLine("</section>");
        html.AppendLine("</main></body></html>");

        var rendered = html.ToString();
        return new ChromeLabLocalDevOperatorSurfaceHtmlResult(
            Decision: ChromeLabLocalDevOperatorSurfaceHtmlDecision.Rendered,
            StatusCode: 200,
            ContentType: ContentType,
            Html: rendered,
            Findings: [],
            LocalDevOnly: true,
            ReadOnly: true,
            FailClosed: true,
            ScriptsAbsent: !rendered.Contains("<script", StringComparison.OrdinalIgnoreCase),
            FormsAbsent: !rendered.Contains("<form", StringComparison.OrdinalIgnoreCase),
            ExternalResourcesAbsent:
                !rendered.Contains("http://", StringComparison.OrdinalIgnoreCase) &&
                !rendered.Contains("https://", StringComparison.OrdinalIgnoreCase),
            SafeNextStep: "CHROMELAB_LOCAL_DEV_OPERATOR_HTML_VIEW_ACCEPTANCE_OR_CLOSE");
    }

    private static IReadOnlyList<string> Validate(ChromeLabLocalDevOperatorSurfaceRouteResponse? route)
    {
        var findings = new List<string>();
        if (route is null)
            return ["missing-route-response"];
        if (route.Decision != ChromeLabLocalDevOperatorSurfaceRouteDecision.ServedReadOnlyPreview)
            findings.Add("route-preview-not-served");
        if (route.StatusCode != 200 || !route.PayloadAvailable)
            findings.Add("route-payload-unavailable");
        if (!route.LocalDevOnly || !route.LoopbackOnly || !route.ReadOnly || !route.FailClosed)
            findings.Add("route-boundary-mismatch");
        if (!route.CacheDisabled)
            findings.Add("route-cache-must-stay-disabled");
        if (route.Surface is null || route.Acceptance is null)
            findings.Add("surface-or-acceptance-missing");
        if (route.Acceptance is not null &&
            route.Acceptance.Decision != ChromeLabLocalDevOperatorSurfaceAcceptanceDecision.Accepted)
            findings.Add("surface-acceptance-rejected");
        if (route.Surface?.ViewModel.ActionPreviews.Count != 1)
            findings.Add("single-action-preview-required");

        return findings.OrderBy(value => value, StringComparer.Ordinal).ToArray();
    }

    private static ChromeLabLocalDevOperatorSurfaceHtmlResult Rejected(IReadOnlyList<string> findings) =>
        new(
            Decision: ChromeLabLocalDevOperatorSurfaceHtmlDecision.Rejected,
            StatusCode: 503,
            ContentType: ContentType,
            Html: "<!doctype html><html lang=\"en\"><head><meta charset=\"utf-8\"><title>ChromeLab Local/Dev Unavailable</title></head><body><main><h1>ChromeLab Local/Dev Unavailable</h1><p>The read-only operator surface failed closed.</p></main></body></html>",
            Findings: findings,
            LocalDevOnly: true,
            ReadOnly: true,
            FailClosed: true,
            ScriptsAbsent: true,
            FormsAbsent: true,
            ExternalResourcesAbsent: true,
            SafeNextStep: "FIX_CHROMELAB_LOCAL_DEV_OPERATOR_SURFACE_BEFORE_HTML_VIEW");

    private static string Encode(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);
}
