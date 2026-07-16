using System.Collections.Concurrent;
using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using OneBrain.AgentOperations.Core.Workspace;

namespace OneBrain.Pilot;

public static class RealWorkspaceMissionDraftEndpointMapper
{
    public const string JsonRoute = "/api/mission/draft";
    public const string HtmlRoute = "/mission/new";
    public const string ClearRoute = "/mission/clear";
    public const string TokenField = "missionDraftToken";
    public const string TokenCookie = "nodal-mission-draft-token";

    private const long MaximumFormBytes = 8 * 1024;
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromMinutes(10);
    private static readonly ConcurrentDictionary<string, DateTimeOffset> Tokens = new(StringComparer.Ordinal);

    public static IEndpointRouteBuilder MapRealWorkspaceMissionDraft(
        this IEndpointRouteBuilder endpoints,
        IHostEnvironment environment,
        Func<NodalOsWorkspaceMissionDraftService>? serviceFactory = null)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentNullException.ThrowIfNull(environment);
        serviceFactory ??= NodalOsWorkspaceMissionDraftRuntime.CreateDefault;

        endpoints.MapGet(JsonRoute, async Task<IResult> (HttpContext context) =>
        {
            if (!IsRequestAllowed(context.Connection.RemoteIpAddress))
                return Results.NotFound(new { decision = "REAL_WORKSPACE_MISSION_LOCAL_ONLY" });

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
            return Results.Content(Render(snapshot, IssueToken(context)), "text/html; charset=utf-8");
        });

        endpoints.MapPost(HtmlRoute, async Task<IResult> (HttpContext context) =>
        {
            if (!IsRequestAllowed(context.Connection.RemoteIpAddress))
                return Results.NotFound();

            ApplyHeaders(context.Response);
            if (!LocalWorkspaceSelectionEndpointMapper.IsSameOriginPost(context.Request))
                return Results.StatusCode(StatusCodes.Status403Forbidden);
            if (!IsBoundedFormRequest(context.Request))
                return Results.BadRequest();

            var form = await context.Request.ReadFormAsync(context.RequestAborted).ConfigureAwait(false);
            if (!ConsumeToken(context.Request.Cookies[TokenCookie], form[TokenField].FirstOrDefault()))
                return Results.StatusCode(StatusCodes.Status403Forbidden);

            var snapshot = await serviceFactory().CreateAsync(
                    form["goal"].FirstOrDefault(),
                    context.RequestAborted)
                .ConfigureAwait(false);
            if (snapshot.Accepted)
                return Results.Redirect(HtmlRoute);

            return Results.Content(
                Render(snapshot, IssueToken(context)),
                "text/html; charset=utf-8",
                statusCode: StatusCodes.Status422UnprocessableEntity);
        });

        endpoints.MapPost(ClearRoute, async Task<IResult> (HttpContext context) =>
        {
            if (!IsRequestAllowed(context.Connection.RemoteIpAddress))
                return Results.NotFound();

            ApplyHeaders(context.Response);
            if (!LocalWorkspaceSelectionEndpointMapper.IsSameOriginPost(context.Request))
                return Results.StatusCode(StatusCodes.Status403Forbidden);
            if (!IsBoundedFormRequest(context.Request))
                return Results.BadRequest();

            var form = await context.Request.ReadFormAsync(context.RequestAborted).ConfigureAwait(false);
            if (!ConsumeToken(context.Request.Cookies[TokenCookie], form[TokenField].FirstOrDefault()))
                return Results.StatusCode(StatusCodes.Status403Forbidden);

            await serviceFactory().ClearAsync(context.RequestAborted).ConfigureAwait(false);
            return Results.Redirect(HtmlRoute);
        });

        return endpoints;
    }

    public static bool IsRequestAllowed(IPAddress? remoteAddress) =>
        remoteAddress is not null && IPAddress.IsLoopback(remoteAddress);

    public static string Render(NodalOsWorkspaceMissionDraftSnapshot snapshot, string token)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentException.ThrowIfNullOrWhiteSpace(token);

        var stateClass = snapshot.Accepted
            ? "ready"
            : snapshot.State is NodalOsWorkspaceMissionDraftState.WorkspaceRequired or NodalOsWorkspaceMissionDraftState.GoalRejected
                ? "attention"
                : snapshot.State == NodalOsWorkspaceMissionDraftState.NotConfigured
                    ? "empty"
                    : "blocked";
        var title = snapshot.Accepted ? "Misión lista para revisión" : "Crear misión sobre el workspace real";
        var description = snapshot.Accepted
            ? "La misión y el candidato reversible quedaron persistidos. Todavía no existe aprobación ni ejecución."
            : "Definí un objetivo claro. NODAL OS lo liga al workspace seleccionado y prepara una acción acotada para revisión.";
        var currentMission = RenderCurrentMission(snapshot);
        var plan = RenderPlan(snapshot);
        var candidate = RenderCandidate(snapshot);
        var blockers = snapshot.ReviewBlockers.Count == 0
            ? "<div class=\"notice ok\">Sin bloqueos de preparación. La ejecución sigue cerrada.</div>"
            : string.Join(string.Empty, snapshot.ReviewBlockers.Select(value => $"<div class=\"notice\">{H(value)}</div>"));
        var clear = snapshot.Persisted ? RenderClearForm(token) : string.Empty;

        const string template = """
<!doctype html>
<html lang="es">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <meta name="robots" content="noindex,nofollow">
  <title>NODAL OS — Mission Draft</title>
  <style>
    :root {
      color-scheme: dark;
      --bg: #0D1117;
      --panel: #161B22;
      --card: #1C2128;
      --border: #30363D;
      --text: #F5F7FA;
      --muted: #AAB4C0;
      --blue: #4F7CFF;
      --violet: #7C5CFF;
      --ok: #00C2A8;
      --warn: #F0B45A;
      --danger: #F06A6A;
    }
    * { box-sizing: border-box; }
    body { margin: 0; background: var(--bg); color: var(--text); font-family: Inter, Geist, Manrope, "Segoe UI", sans-serif; }
    a { color: inherit; text-decoration: none; }
    .shell { min-height: 100vh; display: grid; grid-template-columns: 230px minmax(0, 1fr); }
    .side { border-right: 1px solid var(--border); background: #10151C; padding: 24px 16px; }
    .brand { font-weight: 900; letter-spacing: .06em; }
    .brand small { display: block; color: var(--muted); font-weight: 500; letter-spacing: 0; margin-top: 5px; }
    .nav { display: grid; gap: 6px; margin-top: 28px; }
    .nav a { padding: 10px 12px; border-radius: 9px; color: var(--muted); border: 1px solid transparent; }
    .nav a.active { color: var(--text); background: #1B2330; border-color: #35415A; }
    main { padding: 24px; max-width: 1260px; width: 100%; margin: 0 auto; }
    .hero { border: 1px solid var(--border); border-radius: 16px; background: var(--panel); padding: 22px; display: flex; justify-content: space-between; align-items: flex-start; gap: 18px; }
    .eyebrow { color: #9DB0FF; font-size: 11px; font-weight: 800; text-transform: uppercase; letter-spacing: .13em; }
    h1 { margin: 8px 0 9px; font-size: 32px; letter-spacing: -.035em; }
    .sub { color: var(--muted); line-height: 1.6; max-width: 800px; }
    .badge { display: inline-flex; border: 1px solid var(--border); border-radius: 999px; padding: 6px 10px; color: var(--muted); font-size: 11px; }
    .badge.ready { color: var(--ok); border-color: rgba(0,194,168,.45); }
    .badge.attention { color: var(--warn); border-color: rgba(240,180,90,.45); }
    .badge.blocked { color: var(--danger); border-color: rgba(240,106,106,.45); }
    .grid { display: grid; grid-template-columns: minmax(0, 1.2fr) minmax(340px, .8fr); gap: 18px; margin-top: 18px; }
    .stack { display: grid; gap: 18px; align-content: start; }
    .card { border: 1px solid var(--border); border-radius: 16px; background: var(--panel); padding: 18px; }
    .card-head { display: flex; align-items: center; justify-content: space-between; gap: 12px; }
    .card h2 { margin: 0; font-size: 15px; }
    .metrics { margin-top: 14px; display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 10px; }
    .metric { border: 1px solid var(--border); background: var(--card); border-radius: 11px; padding: 11px; min-width: 0; }
    .metric span { color: var(--muted); font-size: 11px; text-transform: uppercase; letter-spacing: .07em; display: block; }
    .metric strong { display: block; margin-top: 6px; overflow-wrap: anywhere; }
    .goal { margin-top: 14px; color: var(--text); font-size: 18px; line-height: 1.5; }
    form { display: grid; gap: 13px; margin-top: 16px; }
    label { font-weight: 750; }
    label span { display: block; margin: 4px 0 9px; color: var(--muted); font-size: 12px; line-height: 1.45; font-weight: 400; }
    textarea { width: 100%; min-height: 130px; resize: vertical; border: 1px solid var(--border); border-radius: 11px; background: #0F141A; color: var(--text); font: inherit; padding: 12px 13px; outline: none; }
    textarea:focus { border-color: var(--blue); box-shadow: 0 0 0 3px rgba(79,124,255,.16); }
    button { min-height: 40px; border-radius: 10px; padding: 0 14px; font: inherit; font-weight: 760; }
    .primary { border: 1px solid #405891; background: #26375F; color: #EEF2FF; cursor: pointer; }
    .disabled { border: 1px solid var(--border); background: var(--card); color: #6E7681; cursor: not-allowed; }
    .danger { border: 1px solid rgba(240,106,106,.45); background: rgba(240,106,106,.08); color: #FFB5B5; cursor: pointer; }
    .clear-form { margin-top: 10px; }
    .plan { margin-top: 14px; display: grid; gap: 10px; }
    .step { border: 1px solid var(--border); border-radius: 11px; background: var(--card); padding: 11px; }
    .step-top { display: flex; justify-content: space-between; gap: 10px; align-items: center; }
    .step-title { font-weight: 720; }
    .step-detail { margin-top: 6px; color: var(--muted); font-size: 12px; line-height: 1.45; }
    .candidate { margin-top: 14px; display: grid; gap: 10px; }
    .candidate-row { border: 1px solid var(--border); border-radius: 11px; background: var(--card); padding: 11px; }
    .candidate-row span { display: block; color: var(--muted); font-size: 11px; text-transform: uppercase; letter-spacing: .07em; }
    .candidate-row strong { display: block; margin-top: 6px; overflow-wrap: anywhere; }
    ul { color: var(--muted); line-height: 1.5; padding-left: 20px; }
    pre { margin: 12px 0 0; white-space: pre-wrap; overflow-wrap: anywhere; max-height: 320px; overflow: auto; background: #0F141A; border: 1px solid var(--border); border-radius: 11px; padding: 12px; color: #D8E0EA; font-family: "Cascadia Code", Consolas, monospace; font-size: 12px; }
    .notice { border-left: 3px solid var(--warn); background: #251F12; color: #EAC77A; padding: 10px 12px; margin-top: 9px; line-height: 1.5; }
    .notice.ok { border-left-color: var(--ok); background: rgba(0,194,168,.07); color: #8FE1D3; }
    .footer { margin-top: 18px; color: var(--muted); font-size: 12px; display: flex; justify-content: space-between; gap: 12px; }
    .footer a { color: #AABAFF; }
    @media (max-width: 940px) {
      .shell { grid-template-columns: 1fr; }
      .side { border-right: 0; border-bottom: 1px solid var(--border); }
      .nav { display: flex; overflow: auto; }
      .grid { grid-template-columns: 1fr; }
    }
    @media (max-width: 620px) {
      main { padding: 14px; }
      .hero { display: block; }
      .metrics { grid-template-columns: 1fr; }
    }
  </style>
</head>
<body data-nodal-os="real-workspace-mission-draft" data-state="@@STATE@@" data-persisted="@@PERSISTED@@" data-rehydrated="@@REHYDRATED@@" data-candidate-state="@@CANDIDATE_STATE@@" data-execution-enabled="@@EXECUTION_ENABLED@@" data-workspace-mutated="@@WORKSPACE_MUTATED@@" data-product-authority="@@PRODUCT_AUTHORITY@@">
  <div class="shell">
    <aside class="side">
      <div class="brand">NODAL OS<small>Mission scope review</small></div>
      <nav class="nav"><a href="/">Mission Control</a><a href="/workspace/select">Workspace</a><a class="active" href="/mission/new">Mission</a><a href="/runtime/inspector">Diagnostics</a></nav>
    </aside>
    <main>
      <header class="hero">
        <div><div class="eyebrow">Real workspace mission · review before execution</div><h1>@@TITLE@@</h1><div class="sub">@@DESCRIPTION@@</div></div>
        <span class="badge @@STATE_CLASS@@">@@DECISION@@</span>
      </header>
      <div class="grid">
        <div class="stack">
          @@CURRENT_MISSION@@
          <section class="card" data-section-id="mission-form">
            <div class="card-head"><h2>@@FORM_HEADING@@</h2><span class="badge">local config</span></div>
            <form method="post" action="/mission/new" autocomplete="off">
              <input type="hidden" name="missionDraftToken" value="@@TOKEN@@">
              <label>Objetivo de la misión<span>No incluyas credenciales ni rutas absolutas. Datos sensibles detectados se redaccionan antes de persistir.</span><textarea name="goal" minlength="8" maxlength="600" required placeholder="Preparar un handoff claro del estado actual del proyecto y próximos pasos."></textarea></label>
              <button class="primary" type="submit">Crear misión y candidato revisable</button>
            </form>
            @@CLEAR_FORM@@
          </section>
          @@CANDIDATE@@
        </div>
        <aside class="stack">
          <section class="card" data-section-id="plan"><div class="card-head"><h2>Plan de misión</h2><span class="badge">reviewed draft</span></div>@@PLAN@@</section>
          <section class="card" data-section-id="guardrails"><div class="card-head"><h2>Guardrails</h2><span class="badge">execution closed</span></div>@@BLOCKERS@@<ul><li>Target relativo único: NODAL_HANDOFF.md.</li><li>Sin shell, red, cloud ni provider call.</li><li>Aprobación ligada a misión, workspace fingerprint, action id y target.</li><li>Precondición y rollback se vuelven a validar al ejecutar.</li></ul><button class="disabled" type="button" disabled>Aprobar y ejecutar — próximo bloque</button></section>
        </aside>
      </div>
      <div class="footer"><span>Real workspace read · persisted mission draft · zero workspace mutation</span><a href="/">Volver a Mission Control</a></div>
    </main>
  </div>
</body>
</html>
""";

        return template
            .Replace("@@STATE@@", H(snapshot.State.ToString()), StringComparison.Ordinal)
            .Replace("@@PERSISTED@@", Bool(snapshot.Persisted), StringComparison.Ordinal)
            .Replace("@@REHYDRATED@@", Bool(snapshot.Rehydrated), StringComparison.Ordinal)
            .Replace("@@CANDIDATE_STATE@@", H(snapshot.Candidate?.State.ToString() ?? "None"), StringComparison.Ordinal)
            .Replace("@@EXECUTION_ENABLED@@", Bool(snapshot.Candidate?.ExecutionEnabled == true), StringComparison.Ordinal)
            .Replace("@@WORKSPACE_MUTATED@@", Bool(snapshot.WorkspaceFilesystemMutated), StringComparison.Ordinal)
            .Replace("@@PRODUCT_AUTHORITY@@", Bool(snapshot.ProductAuthorityGranted), StringComparison.Ordinal)
            .Replace("@@TITLE@@", H(title), StringComparison.Ordinal)
            .Replace("@@DESCRIPTION@@", H(description), StringComparison.Ordinal)
            .Replace("@@STATE_CLASS@@", H(stateClass), StringComparison.Ordinal)
            .Replace("@@DECISION@@", H(snapshot.Decision), StringComparison.Ordinal)
            .Replace("@@CURRENT_MISSION@@", currentMission, StringComparison.Ordinal)
            .Replace("@@FORM_HEADING@@", snapshot.Accepted ? "Reemplazar misión activa" : "Nueva misión", StringComparison.Ordinal)
            .Replace("@@TOKEN@@", H(token), StringComparison.Ordinal)
            .Replace("@@CLEAR_FORM@@", clear, StringComparison.Ordinal)
            .Replace("@@CANDIDATE@@", candidate, StringComparison.Ordinal)
            .Replace("@@PLAN@@", plan, StringComparison.Ordinal)
            .Replace("@@BLOCKERS@@", blockers, StringComparison.Ordinal);
    }

    private static string RenderCurrentMission(NodalOsWorkspaceMissionDraftSnapshot snapshot)
    {
        if (snapshot.Binding is null || snapshot.Plan is null)
            return string.Empty;

        const string template = """
<section class="card" data-section-id="current-mission">
  <div class="card-head"><h2>Misión activa</h2><span class="badge ready">persisted + revalidated</span></div>
  <div class="goal">@@GOAL@@</div>
  <div class="metrics">
    <div class="metric"><span>Mission ID</span><strong>@@MISSION_ID@@</strong></div>
    <div class="metric"><span>Workspace</span><strong>@@WORKSPACE@@</strong></div>
    <div class="metric"><span>Progress</span><strong>@@PROGRESS@@%</strong></div>
    <div class="metric"><span>Approval</span><strong>@@APPROVAL@@</strong></div>
  </div>
</section>
""";
        return template
            .Replace("@@GOAL@@", H(snapshot.GoalRedacted), StringComparison.Ordinal)
            .Replace("@@MISSION_ID@@", H(snapshot.MissionId), StringComparison.Ordinal)
            .Replace("@@WORKSPACE@@", H(snapshot.WorkspaceDisplayNameRedacted), StringComparison.Ordinal)
            .Replace("@@PROGRESS@@", snapshot.ProgressPercent.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal)
            .Replace("@@APPROVAL@@", H(snapshot.ApprovalState), StringComparison.Ordinal);
    }

    private static string RenderPlan(NodalOsWorkspaceMissionDraftSnapshot snapshot)
    {
        if (snapshot.Plan is null)
            return "<div class=\"notice\">Create a mission draft to produce the reviewed plan.</div>";

        var builder = new StringBuilder("<div class=\"plan\">");
        foreach (var step in snapshot.Plan.Steps)
        {
            builder.Append("<article class=\"step\"><div class=\"step-top\"><span class=\"step-title\">")
                .Append(H(step.Intent))
                .Append("</span><span class=\"badge ")
                .Append(StepClass(step.Status))
                .Append("\">")
                .Append(H(step.Status.ToString()))
                .Append("</span></div><div class=\"step-detail\">")
                .Append(H(step.ExecutionSurface.ToString()))
                .Append(" · risk ")
                .Append(H(step.RiskLevel.ToString()))
                .Append(step.ApprovalRequired ? " · approval required" : string.Empty)
                .Append("</div></article>");
        }
        return builder.Append("</div>").ToString();
    }

    private static string RenderCandidate(NodalOsWorkspaceMissionDraftSnapshot snapshot)
    {
        var candidate = snapshot.Candidate;
        if (candidate is null)
            return string.Empty;

        var preconditions = string.Join(string.Empty, candidate.PreconditionsRedacted.Select(value => $"<li>{H(value)}</li>"));
        var evidence = string.Join(string.Empty, candidate.ExpectedEvidence.Select(value => $"<li>{H(value.Kind)} — {H(value.Description)}</li>"));
        var existingHash = candidate.ExistingSha256 is null ? "not applicable" : Short(candidate.ExistingSha256, 16);

        const string template = """
<section class="card" data-section-id="action-candidate">
  <div class="card-head"><h2>Acción reversible para revisión</h2><span class="badge attention">@@KIND@@ · @@RISK@@</span></div>
  <div class="candidate">
    <div class="candidate-row"><span>Relative target</span><strong>@@TARGET@@</strong></div>
    <div class="candidate-row"><span>Precondition</span><strong>@@TARGET_STATE@@ · current SHA @@EXISTING_SHA@@</strong></div>
    <div class="candidate-row"><span>Proposed content</span><strong>@@PROPOSED_BYTES@@ bytes · SHA @@PROPOSED_SHA@@</strong></div>
    <div class="candidate-row"><span>Approval scope</span><strong>@@APPROVAL_SCOPE@@</strong></div>
    <div class="candidate-row"><span>Rollback</span><strong>@@ROLLBACK@@</strong></div>
  </div>
  <h3>Preconditions</h3><ul>@@PRECONDITIONS@@</ul>
  <h3>Expected evidence</h3><ul>@@EVIDENCE@@</ul>
  <h3>Contenido propuesto</h3><pre>@@CONTENT@@</pre>
</section>
""";

        return template
            .Replace("@@KIND@@", H(candidate.Kind.ToString()), StringComparison.Ordinal)
            .Replace("@@RISK@@", H(candidate.RiskLevel.ToString()), StringComparison.Ordinal)
            .Replace("@@TARGET@@", H(candidate.RelativeTargetPath), StringComparison.Ordinal)
            .Replace("@@TARGET_STATE@@", candidate.TargetExists ? "must exist and match exact hash" : "must not exist", StringComparison.Ordinal)
            .Replace("@@EXISTING_SHA@@", H(existingHash), StringComparison.Ordinal)
            .Replace("@@PROPOSED_BYTES@@", candidate.ProposedBytes.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal)
            .Replace("@@PROPOSED_SHA@@", H(Short(candidate.ProposedSha256, 16)), StringComparison.Ordinal)
            .Replace("@@APPROVAL_SCOPE@@", H(candidate.RequiredApprovalScope), StringComparison.Ordinal)
            .Replace("@@ROLLBACK@@", H(candidate.RollbackPlanRedacted), StringComparison.Ordinal)
            .Replace("@@PRECONDITIONS@@", preconditions, StringComparison.Ordinal)
            .Replace("@@EVIDENCE@@", evidence, StringComparison.Ordinal)
            .Replace("@@CONTENT@@", H(candidate.ProposedContentRedacted), StringComparison.Ordinal);
    }

    private static string RenderClearForm(string token) =>
        $"<form method=\"post\" action=\"{ClearRoute}\" class=\"clear-form\"><input type=\"hidden\" name=\"{TokenField}\" value=\"{H(token)}\"><button class=\"danger\" type=\"submit\">Descartar misión draft</button></form>";

    private static string StepClass(MissionStepStatus status) => status switch
    {
        MissionStepStatus.Verified => "ready",
        MissionStepStatus.InProgress or MissionStepStatus.ReadyForVerification => "attention",
        MissionStepStatus.Blocked or MissionStepStatus.Failed => "blocked",
        _ => string.Empty
    };

    private static bool IsBoundedFormRequest(HttpRequest request) =>
        request.HasFormContentType && request.ContentLength is > 0 and <= MaximumFormBytes;

    private static string IssueToken(HttpContext context)
    {
        CleanupExpiredTokens();
        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
        Tokens[Digest(token)] = DateTimeOffset.UtcNow.Add(TokenLifetime);
        context.Response.Cookies.Append(
            TokenCookie,
            token,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = context.Request.IsHttps,
                SameSite = SameSiteMode.Strict,
                Path = "/mission",
                MaxAge = TokenLifetime,
                IsEssential = true
            });
        return token;
    }

    private static bool ConsumeToken(string? cookieToken, string? formToken)
    {
        if (string.IsNullOrWhiteSpace(cookieToken) || string.IsNullOrWhiteSpace(formToken) || cookieToken.Length != formToken.Length)
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

        return Tokens.TryRemove(Digest(formToken), out var expiresAt) && expiresAt > DateTimeOffset.UtcNow;
    }

    private static string Digest(string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        try
        {
            return Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        }
        finally
        {
            CryptographicOperations.ZeroMemory(bytes);
        }
    }

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

    private static string Short(string value, int length) => value[..Math.Min(length, value.Length)];

    private static string H(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);

    private static string Bool(bool value) => value ? "true" : "false";
}
