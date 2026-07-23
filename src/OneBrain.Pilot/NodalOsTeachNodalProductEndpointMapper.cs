using System.Collections.Concurrent;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using OneBrain.Core.Skills;

namespace OneBrain.Pilot;

public static class NodalOsTeachNodalProductEndpointMapper
{
    public const string HtmlRoute = "/teach";
    public const string JsonRoute = "/api/teach";
    public const string BindRoute = "/teach/bind";
    public const string CaptureRoute = "/teach/capture";
    public const string FinishRoute = "/teach/finish";
    public const string ReviewRoute = "/teach/review";
    public const string SaveRoute = "/teach/save";
    public const string DiscardRoute = "/teach/discard";
    public const string TokenField = "teachNodalToken";
    public const string TokenCookie = "nodal-teach-token";

    private const long MaximumFormBytes = 64 * 1024;
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromMinutes(10);
    private static readonly ConcurrentDictionary<string, DateTimeOffset> Tokens = new(StringComparer.Ordinal);
    private static readonly JsonSerializerOptions ApiJsonOptions = CreateApiJsonOptions();

    public static IEndpointRouteBuilder MapNodalOsTeachNodalProductSurface(
        this IEndpointRouteBuilder endpoints,
        Func<NodalOsTeachNodalProductService> serviceFactory)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentNullException.ThrowIfNull(serviceFactory);

        endpoints.MapGet(JsonRoute, (HttpContext context) =>
        {
            if (!IsRequestAllowed(context.Connection.RemoteIpAddress))
                return Results.NotFound(new { decision = "TEACH_NODAL_LOCAL_ONLY" });
            ApplyHeaders(context.Response);
            return Results.Json(serviceFactory().GetSnapshot(), ApiJsonOptions);
        });

        endpoints.MapGet(HtmlRoute, (HttpContext context) =>
        {
            if (!IsRequestAllowed(context.Connection.RemoteIpAddress))
                return Results.NotFound();
            ApplyHeaders(context.Response);
            return Results.Content(
                NodalOsTeachNodalProductHtmlRenderer.Render(serviceFactory().GetSnapshot(), IssueToken(context)),
                "text/html; charset=utf-8");
        });

        endpoints.MapPost(BindRoute, async Task<IResult> (HttpContext context) =>
        {
            var form = await ReadAuthorizedFormAsync(context).ConfigureAwait(false);
            if (form is null)
                return Results.StatusCode(StatusCodes.Status403Forbidden);
            var snapshot = await serviceFactory().BindAsync(
                new NodalOsTeachNodalBindRequest(
                    form["workflowTitle"].FirstOrDefault() ?? string.Empty,
                    form["appProfileName"].FirstOrDefault() ?? string.Empty),
                context.RequestAborted).ConfigureAwait(false);
            return Result(context, snapshot);
        });

        endpoints.MapPost(CaptureRoute, async Task<IResult> (HttpContext context) =>
        {
            var form = await ReadAuthorizedFormAsync(context).ConfigureAwait(false);
            if (form is null)
                return Results.StatusCode(StatusCodes.Status403Forbidden);
            if (!Enum.TryParse<TeachNodalActionKind>(form["kind"].FirstOrDefault(), true, out var kind) ||
                !Enum.IsDefined(kind))
                return Results.BadRequest();
            var snapshot = await serviceFactory().CaptureStepAsync(
                new NodalOsTeachNodalCaptureStepRequest(
                    kind,
                    form["intent"].FirstOrDefault() ?? string.Empty,
                    form["targetLabel"].FirstOrDefault() ?? string.Empty,
                    form["targetRole"].FirstOrDefault() ?? string.Empty,
                    form["parameterName"].FirstOrDefault(),
                    form["parameterReference"].FirstOrDefault(),
                    form.ContainsKey("secretByReference")),
                context.RequestAborted).ConfigureAwait(false);
            return Result(context, snapshot);
        });

        endpoints.MapPost(FinishRoute, async Task<IResult> (HttpContext context) =>
        {
            var form = await ReadAuthorizedFormAsync(context).ConfigureAwait(false);
            if (form is null)
                return Results.StatusCode(StatusCodes.Status403Forbidden);
            return Result(context, await serviceFactory().FinishAsync(context.RequestAborted).ConfigureAwait(false));
        });

        endpoints.MapPost(ReviewRoute, async Task<IResult> (HttpContext context) =>
        {
            var form = await ReadAuthorizedFormAsync(context).ConfigureAwait(false);
            if (form is null)
                return Results.StatusCode(StatusCodes.Status403Forbidden);
            var intents = new Dictionary<string, string>(StringComparer.Ordinal);
            var targets = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var pair in form)
            {
                if (pair.Key.StartsWith("stepIntent_", StringComparison.Ordinal))
                    intents[pair.Key["stepIntent_".Length..]] = pair.Value.FirstOrDefault() ?? string.Empty;
                else if (pair.Key.StartsWith("stepTarget_", StringComparison.Ordinal))
                    targets[pair.Key["stepTarget_".Length..]] = pair.Value.FirstOrDefault() ?? string.Empty;
            }

            var snapshot = await serviceFactory().UpdateProposalAsync(
                new NodalOsTeachNodalProposalEditRequest(
                    form["proposalTitle"].FirstOrDefault() ?? string.Empty,
                    form["proposalSummary"].FirstOrDefault() ?? string.Empty,
                    intents,
                    targets),
                context.RequestAborted).ConfigureAwait(false);
            return Result(context, snapshot);
        });

        endpoints.MapPost(SaveRoute, async Task<IResult> (HttpContext context) =>
        {
            var form = await ReadAuthorizedFormAsync(context).ConfigureAwait(false);
            if (form is null)
                return Results.StatusCode(StatusCodes.Status403Forbidden);
            return Result(context, await serviceFactory().SaveAsync(context.RequestAborted).ConfigureAwait(false));
        });

        endpoints.MapPost(DiscardRoute, async Task<IResult> (HttpContext context) =>
        {
            var form = await ReadAuthorizedFormAsync(context).ConfigureAwait(false);
            if (form is null)
                return Results.StatusCode(StatusCodes.Status403Forbidden);
            await serviceFactory().DiscardAsync(context.RequestAborted).ConfigureAwait(false);
            return Results.Redirect(HtmlRoute);
        });

        return endpoints;
    }

    private static async Task<IFormCollection?> ReadAuthorizedFormAsync(HttpContext context)
    {
        if (!IsRequestAllowed(context.Connection.RemoteIpAddress))
            return null;
        ApplyHeaders(context.Response);
        if (!LocalWorkspaceSelectionEndpointMapper.IsSameOriginPost(context.Request))
            return null;
        if (!context.Request.HasFormContentType ||
            context.Request.ContentLength is null or <= 0 or > MaximumFormBytes)
            return null;
        var form = await context.Request.ReadFormAsync(context.RequestAborted).ConfigureAwait(false);
        return ConsumeToken(context.Request.Cookies[TokenCookie], form[TokenField].FirstOrDefault())
            ? form
            : null;
    }

    private static IResult Result(HttpContext context, NodalOsTeachNodalProductSnapshot snapshot)
    {
        if (snapshot.State != NodalOsTeachNodalProductState.FailedClosed)
            return Results.Redirect(HtmlRoute);
        return Results.Content(
            NodalOsTeachNodalProductHtmlRenderer.Render(snapshot, IssueToken(context)),
            "text/html; charset=utf-8",
            statusCode: StatusCodes.Status422UnprocessableEntity);
    }

    private static bool IsRequestAllowed(IPAddress? remoteAddress) =>
        remoteAddress is not null && IPAddress.IsLoopback(remoteAddress);

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
            cookieToken.Length != 64 || cookieToken.Length != formToken.Length)
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

    private static JsonSerializerOptions CreateApiJsonOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
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
}
