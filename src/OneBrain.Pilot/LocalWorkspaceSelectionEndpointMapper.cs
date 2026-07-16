using System.Collections.Concurrent;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using OneBrain.AgentOperations.Core.Workspace;

namespace OneBrain.Pilot;

public static class LocalWorkspaceSelectionEndpointMapper
{
    public const string JsonRoute = "/api/workspace/selection";
    public const string HtmlRoute = "/workspace/select";
    public const string ClearRoute = "/workspace/clear";
    public const string TokenField = "workspaceSelectionToken";
    public const string TokenCookie = "nodal-workspace-selection-token";

    private const long MaximumFormBytes = 8 * 1024;
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromMinutes(10);
    private static readonly ConcurrentDictionary<string, DateTimeOffset> Tokens = new(StringComparer.Ordinal);

    public static IEndpointRouteBuilder MapLocalWorkspaceSelection(
        this IEndpointRouteBuilder endpoints,
        IHostEnvironment environment,
        Func<NodalOsWorkspaceSelectionService>? serviceFactory = null)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentNullException.ThrowIfNull(environment);
        serviceFactory ??= NodalOsWorkspaceSelectionRuntime.CreateDefault;

        endpoints.MapGet(JsonRoute, async Task<IResult> (HttpContext context) =>
        {
            if (!IsRequestAllowed(context.Connection.RemoteIpAddress))
                return Results.NotFound(new { decision = "WORKSPACE_SELECTION_LOCAL_ONLY" });

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
            if (!IsSameOriginPost(context.Request))
                return Results.StatusCode(StatusCodes.Status403Forbidden);
            if (!context.Request.HasFormContentType || context.Request.ContentLength is > MaximumFormBytes)
                return Results.BadRequest();

            var form = await context.Request.ReadFormAsync(context.RequestAborted).ConfigureAwait(false);
            if (!ConsumeToken(
                    context.Request.Cookies[TokenCookie],
                    form[TokenField].FirstOrDefault()))
            {
                return Results.StatusCode(StatusCodes.Status403Forbidden);
            }

            var snapshot = await serviceFactory().SelectAsync(
                    form["rootPath"].FirstOrDefault(),
                    form["displayName"].FirstOrDefault(),
                    context.RequestAborted)
                .ConfigureAwait(false);
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
            if (!IsSameOriginPost(context.Request))
                return Results.StatusCode(StatusCodes.Status403Forbidden);
            if (!context.Request.HasFormContentType || context.Request.ContentLength is > MaximumFormBytes)
                return Results.BadRequest();

            var form = await context.Request.ReadFormAsync(context.RequestAborted).ConfigureAwait(false);
            if (!ConsumeToken(
                    context.Request.Cookies[TokenCookie],
                    form[TokenField].FirstOrDefault()))
            {
                return Results.StatusCode(StatusCodes.Status403Forbidden);
            }

            await serviceFactory().ClearAsync(context.RequestAborted).ConfigureAwait(false);
            return Results.Redirect(HtmlRoute);
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

    public static string Render(NodalOsWorkspaceSelectionSnapshot snapshot, string requestToken)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentException.ThrowIfNullOrWhiteSpace(requestToken);

        var stateClass = snapshot.Accepted ? "ready" : snapshot.State == NodalOsWorkspaceSelectionState.NotConfigured ? "empty" : "blocked";
        var title = snapshot.Accepted ? "Workspace local seleccionado" : "Seleccionar workspace local";
        var explanation = snapshot.Accepted
            ? "La raíz real quedó protegida fuera de la superficie. NODAL OS solo muestra identidad, evidencia y referencias redacted."
            : "Ingresá una carpeta o repositorio local. La selección hace un scan acotado de solo lectura y persiste la raíz protegida con DPAPI.";
        var extensions = snapshot.ExtensionCounts.Count == 0
            ? "<span class=\"chip\">sin evidencia todavía</span>"
            : string.Join(string.Empty, snapshot.ExtensionCounts.OrderBy(pair => pair.Key).Select(pair => $"<span class=\"chip\">{H(pair.Key)} · {pair.Value}</span>"));
        var plan = snapshot.PlanSteps.Count == 0
            ? "<li>Seleccionar y validar una raíz local.</li>"
            : string.Join(string.Empty, snapshot.PlanSteps.Select(step => $"<li>{H(step)}</li>"));
        var blockers = snapshot.ReviewBlockers.Count == 0
            ? "<div class=\"notice ok\">Sin bloqueos dentro del alcance de selección.</div>"
            : string.Join(string.Empty, snapshot.ReviewBlockers.Select(value => $"<div class=\"notice\">{H(value)}</div>"));
        var current = snapshot.Accepted
            ? $$$"""
              <section class="card" data-section-id="current-workspace">
                <div class="card-head"><h2>Workspace activo</h2><span class="badge ready">persisted + revalidated</span></div>
                <div class="workspace-name">{{{H(snapshot.DisplayNameRedacted)}}}</div>
                <div class="metrics">
                  <div><span>Workspace ID</span><strong>{{{H(snapshot.WorkspaceId)}}}</strong></div>
                  <div><span>Root hint</span><strong>{{{H(snapshot.RootPathHintRedacted)}}}</strong></div>
                  <div><span>Fingerprint</span><strong>{{{H(Short(snapshot.RootPathFingerprint, 16))}}}</strong></div>
                  <div><span>Path jail</span><strong>{{{H(snapshot.PathJailBindingId)}}}</strong></div>
                  <div><span>Files read</span><strong>{{{snapshot.FilesRead}}}</strong></div>
                  <div><span>Bytes sampled</span><strong>{{{snapshot.TotalBytesRead}}}</strong></div>
                </div>
                <div class="chips">{{{extensions}}}</div>
              </section>
              """
            : string.Empty;
        var clearForm = snapshot.Persisted
            ? $$$"""
              <form method="post" action="{{{ClearRoute}}}" class="clear-form">
                <input type="hidden" name="{{{TokenField}}}" value="{{{H(requestToken)}}}">
                <button class="button danger" type="submit">Quitar selección local</button>
              </form>
              """
            : string.Empty;

        return $$$"""
<!doctype html>
<html lang="es">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <meta name="robots" content="noindex,nofollow">
  <title>NODAL OS — Workspace local</title>
  <style>
    :root{color-scheme:dark;--bg:#0D1117;--panel:#161B22;--card:#1C2128;--border:#30363D;--text:#F5F7FA;--muted:#AAB4C0;--blue:#4F7CFF;--violet:#7C5CFF;--ok:#00C2A8;--warn:#F0B45A;--danger:#F06A6A}*{box-sizing:border-box}body{margin:0;background:var(--bg);color:var(--text);font-family:Inter,Geist,Manrope,"Segoe UI",sans-serif}.shell{min-height:100vh;display:grid;grid-template-columns:230px minmax(0,1fr)}.side{border-right:1px solid var(--border);background:#10151c;padding:24px 16px}.brand{font-weight:900;letter-spacing:.06em}.brand small{display:block;color:var(--muted);font-weight:500;letter-spacing:0;margin-top:5px}.nav{display:grid;gap:6px;margin-top:28px}.nav a{padding:10px 12px;border-radius:9px;color:var(--muted);text-decoration:none;border:1px solid transparent}.nav a.active{color:var(--text);background:#1b2330;border-color:#35415a}.main{padding:24px;max-width:1180px;width:100%;margin:0 auto}.top{display:flex;justify-content:space-between;gap:18px;align-items:flex-start;border:1px solid var(--border);background:var(--panel);border-radius:16px;padding:22px}.eyebrow{text-transform:uppercase;letter-spacing:.13em;color:#9db0ff;font-size:11px;font-weight:800}h1{margin:8px 0 9px;font-size:32px;letter-spacing:-.035em}.sub{color:var(--muted);line-height:1.6;max-width:760px}.badge{display:inline-flex;border:1px solid var(--border);border-radius:999px;padding:6px 10px;color:var(--muted);font-size:11px}.badge.ready{color:var(--ok);border-color:rgba(0,194,168,.45)}.badge.empty{color:#aab4c0}.badge.blocked{color:var(--warn);border-color:rgba(240,180,90,.45)}.grid{display:grid;grid-template-columns:minmax(0,1.2fr) minmax(300px,.8fr);gap:18px;margin-top:18px}.card{border:1px solid var(--border);background:var(--panel);border-radius:16px;padding:18px}.card-head{display:flex;justify-content:space-between;gap:12px;align-items:center}.card h2{font-size:15px;margin:0}.workspace-name{font-size:24px;font-weight:780;margin:18px 0}.metrics{display:grid;grid-template-columns:1fr 1fr;gap:10px}.metrics div{border:1px solid var(--border);background:var(--card);border-radius:11px;padding:11px;min-width:0}.metrics span{display:block;color:var(--muted);font-size:11px;text-transform:uppercase;letter-spacing:.07em}.metrics strong{display:block;margin-top:6px;overflow-wrap:anywhere}.chips{display:flex;flex-wrap:wrap;gap:7px;margin-top:12px}.chip{display:inline-flex;border:1px solid var(--border);border-radius:999px;padding:5px 8px;color:var(--muted);font-size:11px;background:#141a21}form{display:grid;gap:13px;margin-top:16px}label{font-weight:700}label span{display:block;color:var(--muted);font-size:12px;font-weight:400;margin-top:4px;line-height:1.45}input[type=text]{width:100%;border:1px solid var(--border);border-radius:10px;background:#0f141a;color:var(--text);padding:12px 13px;font:inherit;outline:none}input[type=text]:focus{border-color:var(--blue);box-shadow:0 0 0 3px rgba(79,124,255,.16)}.button{min-height:40px;border:1px solid #405891;border-radius:10px;background:#26375f;color:#eef2ff;padding:0 14px;font-weight:760;cursor:pointer}.button.danger{border-color:rgba(240,106,106,.45);background:rgba(240,106,106,.08);color:#ffb5b5}.clear-form{margin-top:12px}.notice{border-left:3px solid var(--warn);background:#251f12;color:#eac77a;padding:10px 12px;margin-top:9px;line-height:1.5}.notice.ok{border-left-color:var(--ok);background:rgba(0,194,168,.07);color:#8fe1d3}ol{padding-left:22px;color:var(--muted);line-height:1.55}.limits{color:var(--muted);line-height:1.55;font-size:13px}.footer{margin-top:18px;color:var(--muted);font-size:12px;display:flex;justify-content:space-between;gap:12px}.footer a{color:#aabaff;text-decoration:none}@media(max-width:900px){.shell{grid-template-columns:1fr}.side{border-right:0;border-bottom:1px solid var(--border)}.nav{display:flex;overflow:auto}.grid{grid-template-columns:1fr}}@media(max-width:620px){.main{padding:14px}.top{display:block}.metrics{grid-template-columns:1fr}}
  </style>
</head>
<body data-nodal-os="workspace-selection" data-selection-state="{{{H(snapshot.State.ToString())}}}" data-persisted="{{{Bool(snapshot.Persisted)}}}" data-real-filesystem-read="{{{Bool(snapshot.RealFilesystemRead)}}}" data-workspace-filesystem-mutated="{{{Bool(snapshot.WorkspaceFilesystemMutated)}}}" data-secrets-excluded="{{{Bool(snapshot.SecretsExcluded)}}}" data-product-authority="{{{Bool(snapshot.ProductAuthorityGranted)}}}">
  <div class="shell">
    <aside class="side">
      <div class="brand">NODAL OS<small>Local workspace boundary</small></div>
      <nav class="nav"><a href="/">Mission Control</a><a class="active" href="{{{HtmlRoute}}}">Workspace</a><a href="/workspace/understanding">Understanding</a><a href="/runtime/inspector">Diagnostics</a></nav>
    </aside>
    <main class="main">
      <header class="top">
        <div><div class="eyebrow">Real local workspace · bounded read-only selection</div><h1>{{{H(title)}}}</h1><div class="sub">{{{H(explanation)}}}</div></div>
        <span class="badge {{{stateClass}}}">{{{H(snapshot.Decision)}}}</span>
      </header>
      <div class="grid">
        <div>
          {{{current}}}
          <section class="card" data-section-id="selection-form">
            <div class="card-head"><h2>{{{snapshot.Accepted ? "Cambiar workspace" : "Elegir workspace"}}}</h2><span class="badge">config local</span></div>
            <form method="post" action="{{{HtmlRoute}}}" autocomplete="off">
              <input type="hidden" name="{{{TokenField}}}" value="{{{H(requestToken)}}}">
              <label>Ruta local absoluta<span>Se usa únicamente dentro del proceso local para validar y proteger la selección. No vuelve a renderizarse ni entra en timeline, evidence o handoff.</span><input type="text" name="rootPath" maxlength="2048" required spellcheck="false" placeholder="C:\proyectos\mi-workspace"></label>
              <label>Nombre visible opcional<span>Puede ser un alias simple. Evitá rutas, secretos o datos sensibles.</span><input type="text" name="displayName" maxlength="80" spellcheck="false" placeholder="Mi producto"></label>
              <button class="button" type="submit">Validar y guardar selección</button>
            </form>
            {{{clearForm}}}
          </section>
        </div>
        <aside>
          <section class="card" data-section-id="review-plan"><div class="card-head"><h2>Plan revisado</h2><span class="badge">{{{H(snapshot.PlanDecision)}}}</span></div><ol>{{{plan}}}</ol></section>
          <section class="card" data-section-id="blockers"><div class="card-head"><h2>Guardrails</h2><span class="badge">fail-closed</span></div>{{{blockers}}}<p class="limits">No hay escritura en el workspace, shell, red, proveedor LLM, cloud sync ni product authority. Solo cambia la configuración local protegida de NODAL OS.</p></section>
        </aside>
      </div>
      <div class="footer"><span>DPAPI current-user · metadata redacted · scan acotado · no workspace mutation</span><a href="/">Volver a Mission Control</a></div>
    </main>
  </div>
</body>
</html>
""";
    }

    private static string IssueToken(HttpContext context)
    {
        CleanupExpiredTokens();
        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
        Tokens[TokenDigest(token)] = DateTimeOffset.UtcNow.Add(TokenLifetime);
        context.Response.Cookies.Append(
            TokenCookie,
            token,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = context.Request.IsHttps,
                SameSite = SameSiteMode.Strict,
                Path = "/workspace",
                MaxAge = TokenLifetime,
                IsEssential = true
            });
        return token;
    }

    private static bool ConsumeToken(string? cookieToken, string? formToken)
    {
        if (string.IsNullOrWhiteSpace(cookieToken) || string.IsNullOrWhiteSpace(formToken))
            return false;
        if (cookieToken.Length != formToken.Length)
            return false;

        var left = Encoding.UTF8.GetBytes(cookieToken);
        var right = Encoding.UTF8.GetBytes(formToken);
        try
        {
            if (!CryptographicOperations.FixedTimeEquals(left, right))
                return false;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(left);
            CryptographicOperations.ZeroMemory(right);
        }

        var digest = TokenDigest(formToken);
        if (!Tokens.TryRemove(digest, out var expiresAt))
            return false;
        return expiresAt > DateTimeOffset.UtcNow;
    }

    private static string TokenDigest(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();

    private static void CleanupExpiredTokens()
    {
        var now = DateTimeOffset.UtcNow;
        foreach (var pair in Tokens.Where(pair => pair.Value <= now).ToArray())
            Tokens.TryRemove(pair.Key, out _);
    }

    private static void ApplyHeaders(HttpResponse response)
    {
        response.Headers.CacheControl = "no-store";
        response.Headers.Pragma = "no-cache";
        response.Headers.XContentTypeOptions = "nosniff";
        response.Headers["X-Frame-Options"] = "DENY";
        response.Headers["Referrer-Policy"] = "no-referrer";
        response.Headers["Content-Security-Policy"] =
            "default-src 'none'; style-src 'unsafe-inline'; form-action 'self'; frame-ancestors 'none'; base-uri 'none'";
    }

    private static string Short(string? value, int length) =>
        string.IsNullOrWhiteSpace(value) ? "not available" : value[..Math.Min(length, value.Length)];

    private static string H(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);

    private static string Bool(bool value) => value ? "true" : "false";
}
