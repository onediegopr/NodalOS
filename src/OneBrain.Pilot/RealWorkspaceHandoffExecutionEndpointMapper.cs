using System.Collections.Concurrent;
using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using OneBrain.AgentOperations.Core.Workspace;

namespace OneBrain.Pilot;

public static class RealWorkspaceHandoffExecutionEndpointMapper
{
    public const string JsonRoute = "/api/mission/execution";
    public const string HtmlRoute = "/mission/execution";
    public const string RollbackRoute = "/mission/rollback";
    public const string ClearRoute = "/mission/execution/clear";
    public const string TokenField = "handoffExecutionToken";
    public const string TokenCookie = "nodal-handoff-execution-token";

    private const long MaximumFormBytes = 4 * 1024;
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromMinutes(10);
    private static readonly ConcurrentDictionary<string, DateTimeOffset> Tokens = new(StringComparer.Ordinal);

    public static IEndpointRouteBuilder MapRealWorkspaceHandoffExecution(
        this IEndpointRouteBuilder endpoints,
        IHostEnvironment environment,
        Func<NodalOsWorkspaceHandoffExecutionService>? serviceFactory = null)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentNullException.ThrowIfNull(environment);
        serviceFactory ??= NodalOsWorkspaceHandoffExecutionRuntime.CreateDefault;

        endpoints.MapGet(JsonRoute, async Task<IResult> (HttpContext context) =>
        {
            if (!IsRequestAllowed(context.Connection.RemoteIpAddress))
                return Results.NotFound(new { decision = "WORKSPACE_HANDOFF_EXECUTION_LOCAL_ONLY" });

            ApplyHeaders(context.Response);
            var snapshot = await serviceFactory().GetCurrentAsync(context.RequestAborted).ConfigureAwait(false);
            return Results.Json(snapshot);
        });

        endpoints.MapGet(HtmlRoute, async Task<IResult> (HttpContext context) =>
        {
            if (!IsRequestAllowed(context.Connection.RemoteIpAddress))
                return Results.NotFound();

            ApplyHeaders(context.Response);
            var snapshot = await serviceFactory().GetCurrentAsync(context.RequestAborted).ConfigureAwait(false);
            var token = IssueToken(context);
            return Results.Content(Render(snapshot, token), "text/html; charset=utf-8");
        });

        endpoints.MapPost(HtmlRoute, async Task<IResult> (HttpContext context) =>
        {
            if (!IsRequestAllowed(context.Connection.RemoteIpAddress))
                return Results.NotFound();

            ApplyHeaders(context.Response);
            var form = await ReadValidatedFormAsync(context).ConfigureAwait(false);
            if (form is null)
                return Results.StatusCode(StatusCodes.Status403Forbidden);

            var snapshot = await serviceFactory().ApproveAndExecuteAsync(
                    cancellationToken: context.RequestAborted)
                .ConfigureAwait(false);
            if (snapshot.Accepted)
                return Results.Redirect(HtmlRoute);

            var token = IssueToken(context);
            return Results.Content(
                Render(snapshot, token),
                "text/html; charset=utf-8",
                statusCode: StatusCodes.Status422UnprocessableEntity);
        });

        endpoints.MapPost(RollbackRoute, async Task<IResult> (HttpContext context) =>
        {
            if (!IsRequestAllowed(context.Connection.RemoteIpAddress))
                return Results.NotFound();

            ApplyHeaders(context.Response);
            var form = await ReadValidatedFormAsync(context).ConfigureAwait(false);
            if (form is null)
                return Results.StatusCode(StatusCodes.Status403Forbidden);

            var snapshot = await serviceFactory().RollbackAsync(context.RequestAborted).ConfigureAwait(false);
            if (snapshot.Accepted)
                return Results.Redirect(HtmlRoute);

            var token = IssueToken(context);
            return Results.Content(
                Render(snapshot, token),
                "text/html; charset=utf-8",
                statusCode: StatusCodes.Status422UnprocessableEntity);
        });

        endpoints.MapPost(ClearRoute, async Task<IResult> (HttpContext context) =>
        {
            if (!IsRequestAllowed(context.Connection.RemoteIpAddress))
                return Results.NotFound();

            ApplyHeaders(context.Response);
            var form = await ReadValidatedFormAsync(context).ConfigureAwait(false);
            if (form is null)
                return Results.StatusCode(StatusCodes.Status403Forbidden);

            var snapshot = await serviceFactory().ClearAsync(context.RequestAborted).ConfigureAwait(false);
            if (snapshot.Accepted)
                return Results.Redirect(RealWorkspaceMissionDraftEndpointMapper.HtmlRoute);

            var token = IssueToken(context);
            return Results.Content(
                Render(snapshot, token),
                "text/html; charset=utf-8",
                statusCode: StatusCodes.Status422UnprocessableEntity);
        });

        return endpoints;
    }

    public static bool IsRequestAllowed(IPAddress? remoteAddress) =>
        remoteAddress is not null && IPAddress.IsLoopback(remoteAddress);

    public static bool IsSameOriginPost(HttpRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var originValue = request.Headers.Origin.ToString();
        if (!Uri.TryCreate(originValue, UriKind.Absolute, out var origin))
            return false;
        var expectedPort = request.Host.Port ??
            (string.Equals(request.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) ? 443 : 80);
        return string.Equals(origin.Scheme, request.Scheme, StringComparison.OrdinalIgnoreCase) &&
               string.Equals(origin.Host, request.Host.Host, StringComparison.OrdinalIgnoreCase) &&
               origin.Port == expectedPort;
    }

    public static string Render(NodalOsWorkspaceHandoffExecutionSnapshot snapshot, string requestToken)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentException.ThrowIfNullOrWhiteSpace(requestToken);

        var stateClass = snapshot.State switch
        {
            NodalOsWorkspaceHandoffExecutionState.Completed => "ready",
            NodalOsWorkspaceHandoffExecutionState.RolledBack => "rolled-back",
            NodalOsWorkspaceHandoffExecutionState.ReadyForApproval => "attention",
            _ => "blocked"
        };
        var title = snapshot.State switch
        {
            NodalOsWorkspaceHandoffExecutionState.Completed => "Handoff ejecutado y verificado",
            NodalOsWorkspaceHandoffExecutionState.RolledBack => "Rollback ejecutado y verificado",
            NodalOsWorkspaceHandoffExecutionState.ReadyForApproval => "Aprobar acción reversible",
            _ => "Ejecución bloqueada"
        };
        var explanation = snapshot.State switch
        {
            NodalOsWorkspaceHandoffExecutionState.Completed => "La acción aprobada terminó con verificación exacta. La evidencia y el restore plan permanecen ligados a esta misión.",
            NodalOsWorkspaceHandoffExecutionState.RolledBack => "El workspace volvió al estado previo verificado mediante un rollback guardado por operación y hash.",
            NodalOsWorkspaceHandoffExecutionState.ReadyForApproval => "Esta aprobación aplica una sola vez al mission id, workspace fingerprint, action id, capability, target relativo y hashes revisados.",
            _ => "NODAL OS falló cerrado. Revisá el bloqueo antes de recrear la misión o volver a intentar."
        };
        var blockers = snapshot.Blockers.Count == 0
            ? "<div class=\"notice ok\">Sin bloqueos dentro del alcance revisado.</div>"
            : string.Join(string.Empty, snapshot.Blockers.Select(value => $"<div class=\"notice\">{H(value)}</div>"));
        var evidence = snapshot.EvidenceRefs.Count == 0
            ? "<span class=\"chip\">pendiente</span>"
            : string.Join(string.Empty, snapshot.EvidenceRefs.Select(value => $"<span class=\"chip\">{H(value)}</span>"));
        var timeline = snapshot.Timeline.Count == 0
            ? "<li>Approval, execution and verification events will appear after the operator decision.</li>"
            : string.Join(string.Empty, snapshot.Timeline.Select(item => $"<li><strong>{H(item.Kind.ToString())}</strong><span>{H(item.SummaryRedacted)}</span></li>"));
        var primaryAction = snapshot.State == NodalOsWorkspaceHandoffExecutionState.ReadyForApproval
            ? $"<form method=\"post\" action=\"{HtmlRoute}\"><input type=\"hidden\" name=\"{TokenField}\" value=\"{H(requestToken)}\"><button class=\"button primary\" type=\"submit\">Aprobar alcance y ejecutar</button></form>"
            : string.Empty;
        var rollbackAction = snapshot.RollbackAvailable
            ? $"<form method=\"post\" action=\"{RollbackRoute}\"><input type=\"hidden\" name=\"{TokenField}\" value=\"{H(requestToken)}\"><button class=\"button danger\" type=\"submit\">Restaurar estado anterior</button></form>"
            : string.Empty;
        var clearAction = snapshot.State is NodalOsWorkspaceHandoffExecutionState.RolledBack or NodalOsWorkspaceHandoffExecutionState.FailedClosed or NodalOsWorkspaceHandoffExecutionState.ResultChanged
            ? $"<form method=\"post\" action=\"{ClearRoute}\"><input type=\"hidden\" name=\"{TokenField}\" value=\"{H(requestToken)}\"><button class=\"button\" type=\"submit\">Cerrar registro activo</button></form>"
            : string.Empty;

        const string template = """
<!doctype html>
<html lang="es">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <meta name="robots" content="noindex,nofollow">
  <title>NODAL OS — Approved handoff execution</title>
  <style>
    :root{color-scheme:dark;--bg:#0D1117;--panel:#161B22;--card:#1C2128;--border:#30363D;--text:#F5F7FA;--muted:#AAB4C0;--blue:#4F7CFF;--violet:#7C5CFF;--ok:#00C2A8;--warn:#F0B45A;--danger:#F06A6A}*{box-sizing:border-box}body{margin:0;background:var(--bg);color:var(--text);font-family:Inter,Geist,Manrope,"Segoe UI",sans-serif}.shell{min-height:100vh;display:grid;grid-template-columns:230px minmax(0,1fr)}.side{border-right:1px solid var(--border);background:#10151c;padding:24px 16px}.brand{font-weight:900;letter-spacing:.06em}.brand small{display:block;color:var(--muted);font-weight:500;letter-spacing:0;margin-top:5px}.nav{display:grid;gap:6px;margin-top:28px}.nav a{padding:10px 12px;border-radius:9px;color:var(--muted);text-decoration:none;border:1px solid transparent}.nav a.active{color:var(--text);background:#1b2330;border-color:#35415a}.main{padding:24px;max-width:1180px;width:100%;margin:0 auto}.top{display:flex;justify-content:space-between;gap:18px;align-items:flex-start;border:1px solid var(--border);background:var(--panel);border-radius:16px;padding:22px}.eyebrow{text-transform:uppercase;letter-spacing:.13em;color:#9db0ff;font-size:11px;font-weight:800}h1{margin:8px 0 9px;font-size:32px;letter-spacing:-.035em}.sub{color:var(--muted);line-height:1.6;max-width:760px}.badge{display:inline-flex;border:1px solid var(--border);border-radius:999px;padding:6px 10px;color:var(--muted);font-size:11px}.badge.ready{color:var(--ok);border-color:rgba(0,194,168,.45)}.badge.rolled-back{color:#b9aaff;border-color:rgba(124,92,255,.45)}.badge.attention{color:var(--warn);border-color:rgba(240,180,90,.45)}.badge.blocked{color:var(--danger);border-color:rgba(240,106,106,.45)}.grid{display:grid;grid-template-columns:minmax(0,1.15fr) minmax(320px,.85fr);gap:18px;margin-top:18px}.card{border:1px solid var(--border);background:var(--panel);border-radius:16px;padding:18px}.card h2{font-size:15px;margin:0 0 14px}.metrics{display:grid;grid-template-columns:1fr 1fr;gap:10px}.metric{border:1px solid var(--border);background:var(--card);border-radius:11px;padding:11px;min-width:0}.metric span{display:block;color:var(--muted);font-size:11px;text-transform:uppercase;letter-spacing:.07em}.metric strong{display:block;margin-top:6px;overflow-wrap:anywhere}.scope{margin-top:14px;border-left:3px solid var(--blue);background:#151d2a;padding:12px;line-height:1.55;color:#dce5ff;overflow-wrap:anywhere}.notice{border-left:3px solid var(--warn);background:#251f12;color:#eac77a;padding:10px 12px;margin-top:9px;line-height:1.5}.notice.ok{border-left-color:var(--ok);background:rgba(0,194,168,.07);color:#8fe1d3}.actions{display:flex;flex-wrap:wrap;gap:9px;margin-top:16px}.actions form{margin:0}.button{min-height:40px;border:1px solid #405891;border-radius:10px;background:#202a44;color:#eef2ff;padding:0 14px;font-weight:760;cursor:pointer}.button.primary{background:#26375f}.button.danger{border-color:rgba(240,106,106,.45);background:rgba(240,106,106,.08);color:#ffb5b5}.chips{display:flex;flex-wrap:wrap;gap:7px}.chip{display:inline-flex;border:1px solid var(--border);border-radius:999px;padding:5px 8px;color:var(--muted);font-size:11px;background:#141a21;overflow-wrap:anywhere}.timeline{list-style:none;padding:0;margin:0;display:grid;gap:8px}.timeline li{border:1px solid var(--border);background:var(--card);border-radius:10px;padding:10px}.timeline strong{display:block;font-size:12px}.timeline span{display:block;color:var(--muted);margin-top:4px;line-height:1.45}.footer{margin-top:18px;color:var(--muted);font-size:12px;display:flex;justify-content:space-between;gap:12px}.footer a{color:#aabaff;text-decoration:none}@media(max-width:900px){.shell{grid-template-columns:1fr}.side{border-right:0;border-bottom:1px solid var(--border)}.nav{display:flex;overflow:auto}.grid{grid-template-columns:1fr}}@media(max-width:620px){.main{padding:14px}.top{display:block}.metrics{grid-template-columns:1fr}}
  </style>
</head>
<body data-nodal-os="real-workspace-handoff-execution" data-state="@@STATE@@" data-persisted="@@PERSISTED@@" data-executed="@@EXECUTED@@" data-verified="@@VERIFIED@@" data-rollback-available="@@ROLLBACK_AVAILABLE@@" data-rolled-back="@@ROLLED_BACK@@" data-workspace-filesystem-mutated="@@WORKSPACE_MUTATED@@" data-product-authority="@@PRODUCT_AUTHORITY@@">
  <div class="shell">
    <aside class="side"><div class="brand">NODAL OS<small>Mission-scoped safe execution</small></div><nav class="nav"><a href="/">Mission Control</a><a href="/workspace/select">Workspace</a><a href="/mission/new">Mission</a><a class="active" href="/mission/execution">Approval & execution</a></nav></aside>
    <main class="main">
      <header class="top"><div><div class="eyebrow">One mission · one target · exact verification</div><h1>@@TITLE@@</h1><div class="sub">@@EXPLANATION@@</div></div><span class="badge @@STATE_CLASS@@">@@DECISION@@</span></header>
      <div class="grid">
        <div>
          <section class="card"><h2>Alcance aprobado</h2><div class="metrics"><div class="metric"><span>Misión</span><strong>@@MISSION_ID@@</strong></div><div class="metric"><span>Acción</span><strong>@@ACTION_KIND@@</strong></div><div class="metric"><span>Target relativo</span><strong>@@TARGET@@</strong></div><div class="metric"><span>Estado</span><strong>@@STATE@@</strong></div><div class="metric"><span>Proposed hash</span><strong>@@PROPOSED_HASH@@</strong></div><div class="metric"><span>Result hash</span><strong>@@RESULT_HASH@@</strong></div></div><div class="scope">@@APPROVAL_SCOPE@@</div><div class="actions">@@PRIMARY_ACTION@@@@ROLLBACK_ACTION@@@@CLEAR_ACTION@@</div></section>
          <section class="card"><h2>Bloqueos y guardrails</h2>@@BLOCKERS@@</section>
        </div>
        <aside>
          <section class="card"><h2>Evidencia</h2><div class="chips">@@EVIDENCE@@</div></section>
          <section class="card"><h2>Timeline canónica</h2><ul class="timeline">@@TIMELINE@@</ul></section>
        </aside>
      </div>
      <div class="footer"><span>Atomic write · exact hash · app-local restore · no shell · no network · no product authority</span><a href="/">Volver a Mission Control</a></div>
    </main>
  </div>
</body>
</html>
""";

        return template
            .Replace("@@STATE@@", H(snapshot.State.ToString()), StringComparison.Ordinal)
            .Replace("@@PERSISTED@@", Bool(snapshot.Persisted), StringComparison.Ordinal)
            .Replace("@@EXECUTED@@", Bool(snapshot.Executed), StringComparison.Ordinal)
            .Replace("@@VERIFIED@@", Bool(snapshot.Verified), StringComparison.Ordinal)
            .Replace("@@ROLLBACK_AVAILABLE@@", Bool(snapshot.RollbackAvailable), StringComparison.Ordinal)
            .Replace("@@ROLLED_BACK@@", Bool(snapshot.RolledBack), StringComparison.Ordinal)
            .Replace("@@WORKSPACE_MUTATED@@", Bool(snapshot.WorkspaceFilesystemMutated), StringComparison.Ordinal)
            .Replace("@@PRODUCT_AUTHORITY@@", Bool(snapshot.ProductAuthorityGranted), StringComparison.Ordinal)
            .Replace("@@TITLE@@", H(title), StringComparison.Ordinal)
            .Replace("@@EXPLANATION@@", H(explanation), StringComparison.Ordinal)
            .Replace("@@STATE_CLASS@@", stateClass, StringComparison.Ordinal)
            .Replace("@@DECISION@@", H(snapshot.Decision), StringComparison.Ordinal)
            .Replace("@@MISSION_ID@@", H(snapshot.MissionId ?? "not configured"), StringComparison.Ordinal)
            .Replace("@@ACTION_KIND@@", H(snapshot.ActionKind ?? "not configured"), StringComparison.Ordinal)
            .Replace("@@TARGET@@", H(snapshot.RelativeTargetPath), StringComparison.Ordinal)
            .Replace("@@PROPOSED_HASH@@", H(Short(snapshot.ProposedSha256, 16)), StringComparison.Ordinal)
            .Replace("@@RESULT_HASH@@", H(Short(snapshot.ResultSha256, 16)), StringComparison.Ordinal)
            .Replace("@@APPROVAL_SCOPE@@", H(snapshot.ApprovalScope ?? "Approval scope becomes available after a reviewed mission draft."), StringComparison.Ordinal)
            .Replace("@@PRIMARY_ACTION@@", primaryAction, StringComparison.Ordinal)
            .Replace("@@ROLLBACK_ACTION@@", rollbackAction, StringComparison.Ordinal)
            .Replace("@@CLEAR_ACTION@@", clearAction, StringComparison.Ordinal)
            .Replace("@@BLOCKERS@@", blockers, StringComparison.Ordinal)
            .Replace("@@EVIDENCE@@", evidence, StringComparison.Ordinal)
            .Replace("@@TIMELINE@@", timeline, StringComparison.Ordinal);
    }

    private static async ValueTask<IFormCollection?> ReadValidatedFormAsync(HttpContext context)
    {
        if (!IsSameOriginPost(context.Request) ||
            !context.Request.HasFormContentType ||
            context.Request.ContentLength is > MaximumFormBytes)
        {
            return null;
        }

        var form = await context.Request.ReadFormAsync(context.RequestAborted).ConfigureAwait(false);
        return ConsumeToken(
            context.Request.Cookies[TokenCookie],
            form[TokenField].FirstOrDefault())
            ? form
            : null;
    }

    private static string IssueToken(HttpContext context)
    {
        CleanupExpiredTokens();
        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
        Tokens[token] = DateTimeOffset.UtcNow.Add(TokenLifetime);
        context.Response.Cookies.Append(
            TokenCookie,
            token,
            new CookieOptions
            {
                HttpOnly = true,
                SameSite = SameSiteMode.Strict,
                Secure = context.Request.IsHttps,
                IsEssential = true,
                MaxAge = TokenLifetime,
                Path = "/"
            });
        return token;
    }

    private static bool ConsumeToken(string? cookieToken, string? formToken)
    {
        CleanupExpiredTokens();
        if (string.IsNullOrWhiteSpace(cookieToken) || string.IsNullOrWhiteSpace(formToken) ||
            cookieToken.Length != formToken.Length ||
            !CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(cookieToken),
                Encoding.UTF8.GetBytes(formToken)))
        {
            return false;
        }

        return Tokens.TryRemove(cookieToken, out var expiresAt) && expiresAt >= DateTimeOffset.UtcNow;
    }

    private static void CleanupExpiredTokens()
    {
        var now = DateTimeOffset.UtcNow;
        foreach (var pair in Tokens)
        {
            if (pair.Value < now)
                Tokens.TryRemove(pair.Key, out _);
        }
    }

    private static void ApplyHeaders(HttpResponse response)
    {
        response.Headers.CacheControl = "no-store";
        response.Headers.Pragma = "no-cache";
        response.Headers.XContentTypeOptions = "nosniff";
        response.Headers["X-Frame-Options"] = "DENY";
        response.Headers["Referrer-Policy"] = "no-referrer";
        response.Headers["Content-Security-Policy"] =
            "default-src 'none'; style-src 'unsafe-inline'; img-src 'none'; font-src 'none'; connect-src 'none'; frame-ancestors 'none'; base-uri 'none'; form-action 'self'";
    }

    private static string H(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);

    private static string Bool(bool value) => value ? "true" : "false";

    private static string Short(string? value, int length) =>
        string.IsNullOrWhiteSpace(value) ? "not available" : value[..Math.Min(length, value.Length)];
}