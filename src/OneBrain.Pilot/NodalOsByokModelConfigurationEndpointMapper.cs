using System.Collections.Concurrent;
using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using OneBrain.AgentOperations.Core.Models;

namespace OneBrain.Pilot;

public static class NodalOsByokModelConfigurationEndpointMapper
{
    public const string HtmlRoute = "/models/config";
    public const string JsonRoute = "/api/models/config";
    public const string TestRoute = "/models/test";
    public const string ClearRoute = "/models/clear";
    public const string TokenField = "byokModelToken";
    public const string TokenCookie = "nodal-byok-model-token";

    private const long MaximumFormBytes = 32 * 1024;
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromMinutes(10);
    private static readonly ConcurrentDictionary<string, DateTimeOffset> Tokens = new(StringComparer.Ordinal);

    public static IEndpointRouteBuilder MapNodalOsByokModelConfiguration(
        this IEndpointRouteBuilder endpoints,
        IHostEnvironment environment,
        Func<NodalOsByokModelConfigurationService>? serviceFactory = null)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentNullException.ThrowIfNull(environment);
        serviceFactory ??= NodalOsByokModelConfigurationRuntime.CreateDefault;

        endpoints.MapGet(JsonRoute, async Task<IResult> (HttpContext context) =>
        {
            if (!IsRequestAllowed(context.Connection.RemoteIpAddress))
                return Results.NotFound(new { decision = "BYOK_MODEL_CONFIGURATION_LOCAL_ONLY" });
            ApplyHeaders(context.Response);
            return Results.Json(await serviceFactory().GetCurrentAsync(context.RequestAborted).ConfigureAwait(false));
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
            if (!TryProviderType(form["primaryProviderType"].FirstOrDefault(), out var primaryType))
                return Results.BadRequest();
            var enableFallback = form.ContainsKey("enableFallback");
            NodalOsByokProviderType? fallbackType = null;
            if (enableFallback)
            {
                if (!TryProviderType(form["fallbackProviderType"].FirstOrDefault(), out var parsedFallback))
                    return Results.BadRequest();
                fallbackType = parsedFallback;
            }

            var request = new NodalOsByokModelConfigurationRequest(
                form["primaryProviderId"].FirstOrDefault() ?? string.Empty,
                form["primaryDisplayName"].FirstOrDefault() ?? string.Empty,
                primaryType,
                form["primaryEndpoint"].FirstOrDefault() ?? string.Empty,
                form["primaryModelId"].FirstOrDefault() ?? string.Empty,
                form["primaryApiKey"].FirstOrDefault(),
                enableFallback,
                form["fallbackProviderId"].FirstOrDefault(),
                form["fallbackDisplayName"].FirstOrDefault(),
                fallbackType,
                form["fallbackEndpoint"].FirstOrDefault(),
                form["fallbackModelId"].FirstOrDefault(),
                form["fallbackApiKey"].FirstOrDefault(),
                form.ContainsKey("cloudAllowed"),
                Decimal(form["maximumTotalCostUsd"].FirstOrDefault(), 1m),
                Integer(form["perAttemptTimeoutSeconds"].FirstOrDefault(), 30),
                Decimal(form["primaryInputCostPerMillion"].FirstOrDefault(), 0m),
                Decimal(form["primaryOutputCostPerMillion"].FirstOrDefault(), 0m),
                Decimal(form["fallbackInputCostPerMillion"].FirstOrDefault(), 0m),
                Decimal(form["fallbackOutputCostPerMillion"].FirstOrDefault(), 0m));
            var snapshot = await serviceFactory().ConfigureAsync(request, context.RequestAborted).ConfigureAwait(false);
            return snapshot.Accepted
                ? Results.Redirect(HtmlRoute)
                : Results.Content(
                    Render(snapshot, IssueToken(context)),
                    "text/html; charset=utf-8",
                    statusCode: StatusCodes.Status422UnprocessableEntity);
        });

        endpoints.MapPost(TestRoute, async Task<IResult> (HttpContext context) =>
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

            var snapshot = await serviceFactory().TestConnectionAsync(context.RequestAborted).ConfigureAwait(false);
            return Results.Content(
                Render(snapshot, IssueToken(context)),
                "text/html; charset=utf-8",
                statusCode: snapshot.Connected ? StatusCodes.Status200OK :
                    snapshot.Cancelled ? StatusCodes.Status409Conflict : StatusCodes.Status422UnprocessableEntity);
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

    public static string Render(NodalOsByokModelConfigurationSnapshot snapshot, string token)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentException.ThrowIfNullOrWhiteSpace(token);

        var primary = snapshot.Primary;
        var fallback = snapshot.Fallback;
        var stateClass = snapshot.Connected ? "ready" : snapshot.State is NodalOsByokConfigurationState.ConnectionFailed or NodalOsByokConfigurationState.FailedClosed ? "blocked" : "attention";
        var blockers = snapshot.Blockers.Count == 0
            ? string.Empty
            : $"<div class=\"notice blocked\"><strong>Bloqueo</strong><ul>{string.Join(string.Empty, snapshot.Blockers.Select(value => $"<li>{H(value)}</li>"))}</ul></div>";
        var attempts = snapshot.AttemptSummaries.Count == 0
            ? "<span class=\"muted\">Sin intentos todavía.</span>"
            : string.Join(string.Empty, snapshot.AttemptSummaries.Select(value => $"<code>{H(value)}</code>"));
        var evidence = snapshot.EvidenceRefs.Count == 0
            ? "<span class=\"muted\">La evidencia aparecerá después del test real.</span>"
            : string.Join(string.Empty, snapshot.EvidenceRefs.Select(value => $"<span class=\"chip\">{H(value)}</span>"));
        var timeline = snapshot.Timeline.Count == 0
            ? "<span class=\"muted\">Sin eventos de conexión.</span>"
            : string.Join(string.Empty, snapshot.Timeline.Select(value =>
                $"<article class=\"event\"><strong>{H(value.Kind.ToString())}</strong><span>{H(value.SummaryRedacted)}</span></article>"));
        var fallbackChecked = fallback is null ? string.Empty : " checked";
        var cloudChecked = snapshot.CloudAllowed ? " checked" : string.Empty;
        var testForm = snapshot.Configured
            ? $"<form method=\"post\" action=\"{TestRoute}\" class=\"inline-form\"><input type=\"hidden\" name=\"{TokenField}\" value=\"{H(token)}\"><button class=\"primary\" type=\"submit\">Probar conexión real</button></form>"
            : string.Empty;
        var clearForm = snapshot.Configured
            ? $"<form method=\"post\" action=\"{ClearRoute}\" class=\"inline-form\"><input type=\"hidden\" name=\"{TokenField}\" value=\"{H(token)}\"><button class=\"danger\" type=\"submit\">Eliminar configuración y credenciales</button></form>"
            : string.Empty;

        const string template = """
<!doctype html>
<html lang="es">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <title>NODAL OS — Modelos BYOK</title>
  <style>
    :root { color-scheme: dark; --bg:#0D1117; --panel:#161B22; --card:#1C2128; --border:#30363D; --text:#F5F7FA; --muted:#AAB4C0; --blue:#4F7CFF; --violet:#7C5CFF; --ok:#00C2A8; --warn:#F0B45A; --bad:#F06A6A; }
    * { box-sizing:border-box; }
    body { margin:0; min-height:100vh; background:var(--bg); color:var(--text); font:14px/1.5 Inter, Geist, Manrope, "Segoe UI", sans-serif; }
    a { color:#AFC0FF; text-decoration:none; }
    .shell { min-height:100vh; display:grid; grid-template-columns:220px minmax(0,1fr); }
    aside { border-right:1px solid var(--border); padding:24px 16px; background:#10151C; }
    .brand { font-weight:900; letter-spacing:.08em; margin-bottom:24px; }
    nav { display:grid; gap:6px; }
    nav a { padding:9px 11px; border-radius:9px; color:var(--muted); }
    nav a.active { background:#1B2330; color:var(--text); border:1px solid #35415A; }
    main { padding:24px; max-width:1240px; width:100%; margin:0 auto; }
    .hero,.card { border:1px solid var(--border); border-radius:16px; background:var(--panel); }
    .hero { padding:22px; display:flex; justify-content:space-between; gap:18px; align-items:flex-start; }
    .eyebrow { color:#91A9FF; text-transform:uppercase; letter-spacing:.13em; font-size:11px; font-weight:850; }
    h1 { margin:7px 0; font-size:34px; letter-spacing:-.04em; }
    h2 { margin:0; font-size:15px; }
    h3 { margin:16px 0 8px; font-size:13px; }
    .sub,.muted { color:var(--muted); }
    .badge,.chip { display:inline-flex; border:1px solid var(--border); border-radius:999px; padding:5px 9px; font-size:11px; }
    .badge.ready { color:var(--ok); border-color:rgba(0,194,168,.45); }
    .badge.attention { color:var(--warn); border-color:rgba(240,180,90,.45); }
    .badge.blocked { color:var(--bad); border-color:rgba(240,106,106,.45); }
    .grid { margin-top:18px; display:grid; grid-template-columns:minmax(0,1.45fr) minmax(320px,.75fr); gap:18px; align-items:start; }
    .stack { display:grid; gap:18px; }
    .card { padding:18px; }
    .card-head { display:flex; justify-content:space-between; gap:12px; align-items:center; padding-bottom:13px; border-bottom:1px solid var(--border); }
    form.config { display:grid; gap:14px; margin-top:16px; }
    fieldset { border:1px solid var(--border); border-radius:13px; padding:14px; display:grid; gap:12px; }
    legend { color:#C8D4FF; padding:0 7px; font-weight:800; }
    .fields { display:grid; grid-template-columns:1fr 1fr; gap:11px; }
    label { display:grid; gap:6px; color:var(--muted); font-size:12px; }
    label.full { grid-column:1/-1; }
    input,select { width:100%; min-height:39px; border:1px solid var(--border); border-radius:9px; background:#0F141A; color:var(--text); padding:0 10px; font:inherit; }
    input[type=checkbox] { width:auto; min-height:auto; }
    .check { display:flex; align-items:center; gap:8px; }
    .actions { display:flex; flex-wrap:wrap; gap:9px; }
    .secondary-actions { margin-top:12px; }
    button { min-height:39px; border-radius:9px; padding:0 13px; font:inherit; font-weight:800; cursor:pointer; }
    .primary { border:1px solid #405891; background:#26375F; color:#EEF2FF; }
    .danger { border:1px solid rgba(240,106,106,.45); background:rgba(240,106,106,.08); color:#FFB5B5; }
    .inline-form { display:inline-flex; margin:0; }
    .notice { margin-top:14px; border-left:3px solid var(--warn); background:#251F12; color:#EAC77A; padding:10px 12px; border-radius:8px; }
    .notice.blocked { border-color:var(--bad); background:rgba(240,106,106,.08); color:#FFB5B5; }
    .metric-list { display:grid; gap:9px; margin-top:13px; }
    .metric { border:1px solid var(--border); border-radius:10px; background:var(--card); padding:10px; }
    .metric span { display:block; color:var(--muted); font-size:11px; text-transform:uppercase; letter-spacing:.08em; }
    .metric strong { display:block; margin-top:5px; overflow-wrap:anywhere; }
    code { display:block; margin-top:7px; border:1px solid var(--border); border-radius:8px; padding:8px; background:#0F141A; color:#D8E0EA; overflow-wrap:anywhere; }
    .evidence { display:flex; flex-wrap:wrap; gap:7px; margin-top:12px; }
    .event { display:grid; gap:4px; border-left:2px solid var(--violet); padding:7px 10px; margin-top:8px; background:var(--card); border-radius:0 8px 8px 0; }
    .event span { color:var(--muted); }
    .footer { margin-top:18px; color:var(--muted); font-size:12px; display:flex; justify-content:space-between; }
    @media(max-width:900px){ .shell{display:block}.shell>aside{border-right:0;border-bottom:1px solid var(--border)}nav{display:flex;overflow:auto}.grid,.fields{grid-template-columns:1fr}main{padding:14px} }
  </style>
</head>
<body data-nodal-os="byok-model-configuration" data-state="@@STATE@@" data-configured="@@CONFIGURED@@" data-connected="@@CONNECTED@@" data-fallback-applied="@@FALLBACK_APPLIED@@" data-real-provider-call="@@REAL_PROVIDER_CALL@@" data-network-used="@@NETWORK_USED@@" data-secrets-excluded="@@SECRETS_EXCLUDED@@" data-product-authority="@@PRODUCT_AUTHORITY@@">
<div class="shell">
  <aside><div class="brand">NODAL OS</div><nav><a href="/">Mission Control</a><a href="/workspace/select">Workspace</a><a href="/mission/new">Mission</a><a class="active" href="/models/config">Models</a><a href="/#evidence">Evidence</a></nav></aside>
  <main>
    <header class="hero"><div><div class="eyebrow">BYOK · secure local configuration</div><h1>Modelos bajo tu control</h1><div class="sub">La clave se guarda como referencia opaca mediante el secure store local. NODAL OS prueba una llamada real sin persistir prompts, respuestas ni credenciales.</div></div><span class="badge @@STATE_CLASS@@">@@DECISION@@</span></header>
    @@BLOCKERS@@
    <div class="grid">
      <div class="stack">
        <section class="card"><div class="card-head"><h2>Proveedor principal y fallback autorizado</h2><span class="badge">standard_task</span></div>
          <form class="config" method="post" action="/models/config" autocomplete="off">
            <input type="hidden" name="byokModelToken" value="@@TOKEN@@">
            <fieldset><legend>Principal</legend><div class="fields">
              <label>Provider ID<input name="primaryProviderId" maxlength="80" required value="@@PRIMARY_ID@@" placeholder="openai"></label>
              <label>Nombre visible<input name="primaryDisplayName" maxlength="100" required value="@@PRIMARY_NAME@@" placeholder="Proveedor principal"></label>
              <label>Tipo<select name="primaryProviderType">@@PRIMARY_TYPE_OPTIONS@@</select></label>
              <label>Modelo<input name="primaryModelId" maxlength="160" required value="@@PRIMARY_MODEL@@" placeholder="model-id"></label>
              <label class="full">Endpoint OpenAI-compatible<input name="primaryEndpoint" maxlength="500" required value="@@PRIMARY_ENDPOINT@@" placeholder="https://provider.example/v1"></label>
              <label class="full">API key · dejar vacío para conservar la existente<input type="password" name="primaryApiKey" maxlength="8192" value="" autocomplete="new-password" placeholder="No se mostrará ni persistirá en JSON"></label>
              <label>Input USD / 1M<input name="primaryInputCostPerMillion" inputmode="decimal" value="@@PRIMARY_INPUT_COST@@"></label>
              <label>Output USD / 1M<input name="primaryOutputCostPerMillion" inputmode="decimal" value="@@PRIMARY_OUTPUT_COST@@"></label>
            </div></fieldset>
            <label class="check"><input type="checkbox" name="enableFallback"@@FALLBACK_CHECKED@@> Activar fallback preautorizado</label>
            <fieldset><legend>Fallback opcional</legend><div class="fields">
              <label>Provider ID<input name="fallbackProviderId" maxlength="80" value="@@FALLBACK_ID@@" placeholder="local-fallback"></label>
              <label>Nombre visible<input name="fallbackDisplayName" maxlength="100" value="@@FALLBACK_NAME@@" placeholder="Fallback"></label>
              <label>Tipo<select name="fallbackProviderType">@@FALLBACK_TYPE_OPTIONS@@</select></label>
              <label>Modelo<input name="fallbackModelId" maxlength="160" value="@@FALLBACK_MODEL@@" placeholder="model-id"></label>
              <label class="full">Endpoint<input name="fallbackEndpoint" maxlength="500" value="@@FALLBACK_ENDPOINT@@" placeholder="http://127.0.0.1:11434/v1"></label>
              <label class="full">API key opcional<input type="password" name="fallbackApiKey" maxlength="8192" value="" autocomplete="new-password"></label>
              <label>Input USD / 1M<input name="fallbackInputCostPerMillion" inputmode="decimal" value="@@FALLBACK_INPUT_COST@@"></label>
              <label>Output USD / 1M<input name="fallbackOutputCostPerMillion" inputmode="decimal" value="@@FALLBACK_OUTPUT_COST@@"></label>
            </div></fieldset>
            <fieldset><legend>Política</legend><div class="fields"><label class="check"><input type="checkbox" name="cloudAllowed"@@CLOUD_CHECKED@@> Permitir cloud para estas rutas</label><label>Costo máximo del test USD<input name="maximumTotalCostUsd" inputmode="decimal" value="@@MAX_COST@@"></label><label>Timeout por intento<input name="perAttemptTimeoutSeconds" inputmode="numeric" value="@@TIMEOUT@@"></label></div></fieldset>
            <div class="actions"><button class="primary" type="submit">Guardar configuración segura</button></div>
          </form>
          <div class="actions secondary-actions">@@TEST_FORM@@@@CLEAR_FORM@@</div>
        </section>
      </div>
      <aside class="stack">
        <section class="card"><div class="card-head"><h2>Estado real</h2><span class="badge @@STATE_CLASS@@">@@STATE@@</span></div><div class="metric-list">
          <div class="metric"><span>Principal</span><strong>@@PRIMARY_SUMMARY@@</strong></div><div class="metric"><span>Fallback</span><strong>@@FALLBACK_SUMMARY@@</strong></div><div class="metric"><span>Credencial</span><strong>@@CREDENTIAL_STATE@@</strong></div><div class="metric"><span>Última ruta</span><strong>@@SELECTED_ROUTE@@</strong></div><div class="metric"><span>Intentos / costo</span><strong>@@ATTEMPT_COUNT@@ · $@@TOTAL_COST@@</strong></div><div class="metric"><span>Response SHA-256</span><strong>@@RESPONSE_SHA@@</strong></div>
        </div><h3>Intentos redacted</h3>@@ATTEMPTS@@</section>
        <section class="card"><div class="card-head"><h2>Evidencia</h2><span class="badge">reference-only</span></div><div class="evidence">@@EVIDENCE@@</div></section>
        <section class="card"><div class="card-head"><h2>Timeline</h2><span class="badge">canonical projection</span></div>@@TIMELINE@@</section>
      </aside>
    </div>
    <div class="footer"><span>Local-first · secret reference only · bounded provider test</span><a href="/">Volver a Mission Control</a></div>
  </main>
</div>
</body></html>
""";

        return template
            .Replace("@@STATE@@", H(snapshot.State.ToString()), StringComparison.Ordinal)
            .Replace("@@CONFIGURED@@", Bool(snapshot.Configured), StringComparison.Ordinal)
            .Replace("@@CONNECTED@@", Bool(snapshot.Connected), StringComparison.Ordinal)
            .Replace("@@FALLBACK_APPLIED@@", Bool(snapshot.FallbackApplied), StringComparison.Ordinal)
            .Replace("@@REAL_PROVIDER_CALL@@", Bool(snapshot.RealProviderCallAttempted), StringComparison.Ordinal)
            .Replace("@@NETWORK_USED@@", Bool(snapshot.NetworkUsed), StringComparison.Ordinal)
            .Replace("@@SECRETS_EXCLUDED@@", Bool(snapshot.SecretsExcluded), StringComparison.Ordinal)
            .Replace("@@PRODUCT_AUTHORITY@@", Bool(snapshot.ProductAuthorityGranted), StringComparison.Ordinal)
            .Replace("@@STATE_CLASS@@", stateClass, StringComparison.Ordinal)
            .Replace("@@DECISION@@", H(snapshot.Decision), StringComparison.Ordinal)
            .Replace("@@BLOCKERS@@", blockers, StringComparison.Ordinal)
            .Replace("@@TOKEN@@", H(token), StringComparison.Ordinal)
            .Replace("@@PRIMARY_ID@@", H(primary?.ProviderId ?? ""), StringComparison.Ordinal)
            .Replace("@@PRIMARY_NAME@@", H(primary?.DisplayNameRedacted ?? ""), StringComparison.Ordinal)
            .Replace("@@PRIMARY_MODEL@@", H(primary?.ModelId ?? ""), StringComparison.Ordinal)
            .Replace("@@PRIMARY_ENDPOINT@@", H(primary?.EndpointRedacted ?? ""), StringComparison.Ordinal)
            .Replace("@@PRIMARY_TYPE_OPTIONS@@", TypeOptions(primary?.ProviderType), StringComparison.Ordinal)
            .Replace("@@PRIMARY_INPUT_COST@@", Number(primary?.InputCostPerMillion ?? 0), StringComparison.Ordinal)
            .Replace("@@PRIMARY_OUTPUT_COST@@", Number(primary?.OutputCostPerMillion ?? 0), StringComparison.Ordinal)
            .Replace("@@FALLBACK_CHECKED@@", fallbackChecked, StringComparison.Ordinal)
            .Replace("@@FALLBACK_ID@@", H(fallback?.ProviderId ?? ""), StringComparison.Ordinal)
            .Replace("@@FALLBACK_NAME@@", H(fallback?.DisplayNameRedacted ?? ""), StringComparison.Ordinal)
            .Replace("@@FALLBACK_MODEL@@", H(fallback?.ModelId ?? ""), StringComparison.Ordinal)
            .Replace("@@FALLBACK_ENDPOINT@@", H(fallback?.EndpointRedacted ?? ""), StringComparison.Ordinal)
            .Replace("@@FALLBACK_TYPE_OPTIONS@@", TypeOptions(fallback?.ProviderType), StringComparison.Ordinal)
            .Replace("@@FALLBACK_INPUT_COST@@", Number(fallback?.InputCostPerMillion ?? 0), StringComparison.Ordinal)
            .Replace("@@FALLBACK_OUTPUT_COST@@", Number(fallback?.OutputCostPerMillion ?? 0), StringComparison.Ordinal)
            .Replace("@@CLOUD_CHECKED@@", cloudChecked, StringComparison.Ordinal)
            .Replace("@@MAX_COST@@", Number(snapshot.MaximumTotalCostUsd == 0 ? 1m : snapshot.MaximumTotalCostUsd), StringComparison.Ordinal)
            .Replace("@@TIMEOUT@@", snapshot.PerAttemptTimeoutSeconds.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal)
            .Replace("@@TEST_FORM@@", testForm, StringComparison.Ordinal)
            .Replace("@@CLEAR_FORM@@", clearForm, StringComparison.Ordinal)
            .Replace("@@PRIMARY_SUMMARY@@", H(primary is null ? "not configured" : $"{primary.ProviderId} · {primary.ModelId}"), StringComparison.Ordinal)
            .Replace("@@FALLBACK_SUMMARY@@", H(fallback is null ? "not configured" : $"{fallback.ProviderId} · {fallback.ModelId}"), StringComparison.Ordinal)
            .Replace("@@CREDENTIAL_STATE@@", H(primary?.CredentialConfigured == true ? $"configured in {primary.CredentialStoreId}" : primary?.Local == true ? "not required" : "not configured"), StringComparison.Ordinal)
            .Replace("@@SELECTED_ROUTE@@", H(snapshot.SelectedProviderId is null ? "not tested" : $"{snapshot.SelectedProviderId} · {snapshot.SelectedModelId}"), StringComparison.Ordinal)
            .Replace("@@ATTEMPT_COUNT@@", snapshot.AttemptCount.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal)
            .Replace("@@TOTAL_COST@@", Number(snapshot.TotalEstimatedCost), StringComparison.Ordinal)
            .Replace("@@RESPONSE_SHA@@", H(Short(snapshot.ResponseSha256, 20)), StringComparison.Ordinal)
            .Replace("@@ATTEMPTS@@", attempts, StringComparison.Ordinal)
            .Replace("@@EVIDENCE@@", evidence, StringComparison.Ordinal)
            .Replace("@@TIMELINE@@", timeline, StringComparison.Ordinal);
    }

    private static bool IsBoundedFormRequest(HttpRequest request)
    {
        if (!request.HasFormContentType)
            return false;
        if (request.ContentLength is null or <= 0 or > MaximumFormBytes)
            return false;
        return true;
    }

    private static string IssueToken(HttpContext context)
    {
        var now = DateTimeOffset.UtcNow;
        foreach (var expired in Tokens.Where(pair => pair.Value <= now).Select(pair => pair.Key).ToArray())
            Tokens.TryRemove(expired, out _);
        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
        Tokens[token] = now.Add(TokenLifetime);
        context.Response.Cookies.Append(TokenCookie, token, new CookieOptions
        {
            HttpOnly = true,
            IsEssential = true,
            SameSite = SameSiteMode.Strict,
            Secure = context.Request.IsHttps,
            MaxAge = TokenLifetime,
            Path = "/"
        });
        return token;
    }

    private static bool ConsumeToken(string? cookieToken, string? formToken)
    {
        if (string.IsNullOrWhiteSpace(cookieToken) || string.IsNullOrWhiteSpace(formToken) ||
            cookieToken.Length != formToken.Length || cookieToken.Length != 64)
            return false;
        var left = Encoding.UTF8.GetBytes(cookieToken);
        var right = Encoding.UTF8.GetBytes(formToken);
        try
        {
            if (!CryptographicOperations.FixedTimeEquals(left, right))
                return false;
            return Tokens.TryRemove(cookieToken, out var expiresAt) && expiresAt > DateTimeOffset.UtcNow;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(left);
            CryptographicOperations.ZeroMemory(right);
        }
    }

    private static bool TryProviderType(string? value, out NodalOsByokProviderType type) =>
        Enum.TryParse(value, ignoreCase: true, out type) && Enum.IsDefined(type);

    private static decimal Decimal(string? value, decimal fallback) =>
        decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed) ? parsed : fallback;

    private static int Integer(string? value, int fallback) =>
        int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) ? parsed : fallback;

    private static string TypeOptions(string? selected)
    {
        var localSelected = string.IsNullOrWhiteSpace(selected) ||
            string.Equals(selected, nameof(NodalOsByokProviderType.OpenAiCompatibleLocal), StringComparison.Ordinal);
        var local = localSelected ? " selected" : string.Empty;
        var cloud = string.Equals(selected, nameof(NodalOsByokProviderType.OpenAiCompatibleCloud), StringComparison.Ordinal) ? " selected" : string.Empty;
        return $"<option value=\"OpenAiCompatibleLocal\"{local}>OpenAI-compatible local · loopback</option><option value=\"OpenAiCompatibleCloud\"{cloud}>OpenAI-compatible cloud · HTTPS + key</option>";
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
    private static string Number(decimal value) => value.ToString("0.########", CultureInfo.InvariantCulture);
    private static string Short(string? value, int length) => string.IsNullOrWhiteSpace(value) ? "not available" : value[..Math.Min(length, value.Length)];
}
