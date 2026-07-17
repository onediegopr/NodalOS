using System.Globalization;
using System.Net;
using System.Text;

namespace OneBrain.Pilot;

public static class NodalOsLocalDiagnosticsEndpointMapper
{
    public const string HtmlRoute = "/settings/diagnostics";

    private const long MaximumFormBytes = 2 * 1024;

    public static IEndpointRouteBuilder MapNodalOsLocalDiagnostics(
        this IEndpointRouteBuilder endpoints,
        Func<NodalOsLocalDiagnostics>? diagnosticsFactory = null,
        Func<bool>? packagedProvider = null)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        diagnosticsFactory ??= NodalOsLocalDiagnostics.CreateDefault;
        packagedProvider ??= NodalOsDesktopLaunchRuntime.IsPackaged;

        endpoints.MapGet(HtmlRoute, (HttpContext context) =>
        {
            if (!IsRequestAllowed(context.Connection.RemoteIpAddress))
                return Results.NotFound();

            ApplyHeaders(context.Response);
            return Results.Content(
                Render(diagnosticsFactory().ReadSnapshot()),
                "text/html; charset=utf-8");
        });

        endpoints.MapPost(HtmlRoute, async Task<IResult> (HttpContext context) =>
        {
            if (!IsRequestAllowed(context.Connection.RemoteIpAddress))
                return Results.NotFound();

            ApplyHeaders(context.Response);
            if (!RealWorkspaceHandoffExecutionEndpointMapper.IsSameOriginPost(context.Request) ||
                !context.Request.HasFormContentType ||
                context.Request.ContentLength is > MaximumFormBytes)
            {
                return Results.StatusCode(StatusCodes.Status403Forbidden);
            }

            IFormCollection form;
            try
            {
                form = await context.Request.ReadFormAsync(context.RequestAborted).ConfigureAwait(false);
            }
            catch (InvalidDataException)
            {
                return Results.BadRequest();
            }

            var diagnostics = diagnosticsFactory();
            var action = form["action"].FirstOrDefault();
            try
            {
                switch (action)
                {
                    case "enable":
                        diagnostics.Enable(packagedProvider());
                        break;
                    case "disable":
                        diagnostics.Disable(packagedProvider());
                        break;
                    case "clear":
                        diagnostics.Clear();
                        break;
                    default:
                        return Results.BadRequest();
                }
            }
            catch (IOException)
            {
                return Results.Content(
                    Render(diagnostics.ReadSnapshot(), "No se pudo actualizar el almacenamiento local de diagnósticos."),
                    "text/html; charset=utf-8",
                    statusCode: StatusCodes.Status503ServiceUnavailable);
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Content(
                    Render(diagnostics.ReadSnapshot(), "El almacenamiento local de diagnósticos no está disponible para este usuario."),
                    "text/html; charset=utf-8",
                    statusCode: StatusCodes.Status503ServiceUnavailable);
            }

            return Results.Redirect(HtmlRoute);
        });

        return endpoints;
    }

    public static bool IsRequestAllowed(IPAddress? remoteAddress) =>
        MissionControlProductShellEndpointMapper.IsRequestAllowed(remoteAddress);

    internal static string Render(NodalOsLocalDiagnosticsSnapshot snapshot, string? error = null)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        var enabled = snapshot.Enabled ? "true" : "false";
        var status = snapshot.Enabled ? "Activados" : "Desactivados";
        var statusClass = snapshot.Enabled ? "enabled" : "disabled";
        var primaryAction = snapshot.Enabled
            ? Form("disable", "Desactivar diagnósticos", "button danger")
            : Form("enable", "Activar diagnósticos locales", "button primary");
        var clearAction = snapshot.EventCount > 0
            ? Form("clear", "Borrar eventos locales", "button")
            : string.Empty;
        var lastRecorded = snapshot.LastRecordedAt?.ToString("u", CultureInfo.InvariantCulture) ?? "Sin eventos";
        var errorBox = string.IsNullOrWhiteSpace(error)
            ? string.Empty
            : $"<div class=\"notice error\">{H(error)}</div>";
        var events = snapshot.RecentEvents.Count == 0
            ? "<div class=\"empty\">Todavía no hay eventos locales. Al activar esta opción, el próximo inicio y los errores futuros se registrarán sin mensaje, stack trace, rutas, prompts ni contenido del workspace.</div>"
            : string.Join(string.Empty, snapshot.RecentEvents.Select(item => $"""
                <li>
                  <div><strong>{H(Label(item.Kind))}</strong><span>{H(item.Outcome)}</span></div>
                  <code>{H(item.Code)}</code>
                  <time>{H(item.RecordedAt.ToString("u", CultureInfo.InvariantCulture))}</time>
                </li>
                """));

        return $"""
<!doctype html>
<html lang="es">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <meta name="robots" content="noindex,nofollow">
  <title>NODAL OS — Diagnósticos locales</title>
  <style>
    :root{{color-scheme:dark;--bg:#0D1117;--panel:#161B22;--card:#1C2128;--border:#30363D;--text:#F5F7FA;--muted:#AAB4C0;--blue:#4F7CFF;--ok:#00C2A8;--warn:#F0B45A;--danger:#F06A6A}}*{{box-sizing:border-box}}body{{margin:0;background:var(--bg);color:var(--text);font:14px/1.5 Inter,Geist,Manrope,"Segoe UI",sans-serif}}a{{color:#AFC0FF;text-decoration:none}}.shell{{max-width:980px;margin:0 auto;padding:26px}}.top{{display:flex;justify-content:space-between;gap:18px;align-items:flex-start;border:1px solid var(--border);border-radius:16px;background:var(--panel);padding:22px}}.eyebrow{{color:#9DB0FF;text-transform:uppercase;letter-spacing:.12em;font-size:11px;font-weight:800}}h1{{margin:8px 0;font-size:32px;letter-spacing:-.03em}}.sub{{color:var(--muted);max-width:720px}}.badge{{border:1px solid var(--border);border-radius:999px;padding:6px 10px;font-size:12px}}.badge.enabled{{color:var(--ok);border-color:rgba(0,194,168,.45)}}.badge.disabled{{color:var(--muted)}}.grid{{display:grid;grid-template-columns:1fr 1fr;gap:18px;margin-top:18px}}.card{{border:1px solid var(--border);border-radius:16px;background:var(--panel);padding:18px}}.card h2{{margin:0 0 14px;font-size:15px}}.metrics{{display:grid;grid-template-columns:repeat(2,1fr);gap:10px}}.metric{{border:1px solid var(--border);border-radius:11px;background:var(--card);padding:12px}}.metric span{{display:block;color:var(--muted);font-size:11px;text-transform:uppercase;letter-spacing:.07em}}.metric strong{{display:block;margin-top:6px;overflow-wrap:anywhere}}.notice{{border-left:3px solid var(--blue);background:#151D2A;padding:12px;margin-top:14px;color:#DCE5FF}}.notice.error{{border-left-color:var(--danger);background:rgba(240,106,106,.08);color:#FFC1C1}}.actions{{display:flex;flex-wrap:wrap;gap:9px;margin-top:16px}}form{{margin:0}}.button{{min-height:40px;border:1px solid #405891;border-radius:10px;background:#202A44;color:#EEF2FF;padding:0 14px;font-weight:750;cursor:pointer}}.button.primary{{background:#26375F}}.button.danger{{border-color:rgba(240,106,106,.45);background:rgba(240,106,106,.08);color:#FFB5B5}}ul{{list-style:none;padding:0;margin:0;display:grid;gap:8px}}li{{display:grid;grid-template-columns:1fr auto;gap:8px;border:1px solid var(--border);border-radius:10px;background:var(--card);padding:11px}}li div{{display:flex;gap:8px;align-items:center}}li span,li time{{color:var(--muted);font-size:12px}}li code{{grid-column:2;grid-row:1;font:12px "Cascadia Code",Consolas,monospace;color:#C5D0F5}}li time{{grid-column:1 / -1}}.empty{{color:var(--muted);line-height:1.6}}.boundary{{margin-top:14px;border:1px solid var(--border);border-radius:12px;background:var(--card);padding:12px;color:var(--muted)}}.footer{{margin-top:18px;display:flex;justify-content:space-between;gap:12px;color:var(--muted);font-size:12px}}@media(max-width:760px){{.shell{{padding:14px}}.top{{display:block}}.grid{{grid-template-columns:1fr}}}}
  </style>
</head>
<body data-nodal-os="local-diagnostics-settings" data-diagnostics-enabled="{enabled}" data-local-only="true" data-redacted="true" data-network-used="false" data-product-authority="false">
  <main class="shell">
    <header class="top">
      <div><div class="eyebrow">Private beta · local-first</div><h1>Diagnósticos locales</h1><div class="sub">Registrá inicios y errores mínimos para depurar la beta. Está desactivado por defecto, no envía datos y nunca guarda mensajes de excepción, stack traces, rutas, prompts, respuestas de modelo ni contenido del workspace.</div></div>
      <span class="badge {statusClass}">{status}</span>
    </header>
    {errorBox}
    <section class="grid">
      <article class="card">
        <h2>Control</h2>
        <div class="metrics">
          <div class="metric"><span>Estado</span><strong>{status}</strong></div>
          <div class="metric"><span>Último evento</span><strong>{H(lastRecorded)}</strong></div>
          <div class="metric"><span>Inicios</span><strong>{snapshot.StartupCount}</strong></div>
          <div class="metric"><span>Errores</span><strong>{snapshot.ErrorCount}</strong></div>
        </div>
        <div class="notice">Los archivos permanecen bajo <code>%LOCALAPPDATA%\NodalOS\diagnostics</code>. Compartirlos fuera del dispositivo sigue siendo una decisión humana.</div>
        <div class="actions">{primaryAction}{clearAction}</div>
      </article>
      <article class="card">
        <h2>Qué se conserva</h2>
        <div class="boundary">Fecha UTC, tipo de evento permitido, resultado, tipo técnico de excepción, modo packaged y versión del producto. Retención acotada a 200 eventos y 128 KB.</div>
        <div class="boundary">Sin nube, upload, identificador de usuario, hostname, dirección IP, query string, request body, path local, secreto o payload externo.</div>
      </article>
    </section>
    <section class="card" style="margin-top:18px">
      <h2>Eventos recientes · {snapshot.EventCount}</h2>
      <ul>{events}</ul>
    </section>
    <footer class="footer"><a href="/">← Mission Control</a><span>Opt-in · redacted · local-only</span></footer>
  </main>
</body>
</html>
""";
    }

    private static string Form(string action, string label, string className) => $"""
        <form method="post" action="{HtmlRoute}">
          <input type="hidden" name="action" value="{H(action)}">
          <button class="{H(className)}" type="submit">{H(label)}</button>
        </form>
        """;

    private static string Label(string kind) => kind switch
    {
        "startup" => "Inicio listo",
        "shutdown" => "Cierre solicitado",
        "request-error" => "Error de solicitud",
        "process-error" => "Error de proceso",
        "diagnostics" => "Configuración",
        _ => "Evento local"
    };

    private static void ApplyHeaders(HttpResponse response)
    {
        response.Headers.CacheControl = "no-store";
        response.Headers.Pragma = "no-cache";
        response.Headers.XContentTypeOptions = "nosniff";
        response.Headers.ContentSecurityPolicy = "default-src 'none'; style-src 'unsafe-inline'; form-action 'self'; frame-ancestors 'none'; base-uri 'none'";
        response.Headers["Referrer-Policy"] = "no-referrer";
    }

    private static string H(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);
}
