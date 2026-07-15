using System.Net;
using System.Text;
using OneBrain.AgentOperations.Core.Runtime;
using OneBrain.AgentOperations.Core.Workspace;

namespace OneBrain.Pilot;

public sealed record BoundedWorkspaceAdvisorCardSnapshot(
    string SuggestionId,
    string Category,
    string Severity,
    string Title,
    string MessageRedacted,
    IReadOnlyList<string> EvidenceRefs,
    IReadOnlyList<string> ActionOptions,
    bool RequiresHumanAttention,
    bool NonExecutable,
    bool CanAuthorizeExecution);

public sealed record BoundedWorkspaceOperatorSnapshot(
    string Decision,
    bool Accepted,
    bool LocalDevOnly,
    bool ReadOnly,
    bool SecretsExcluded,
    bool RootConfigured,
    string ScanDecision,
    int FilesRead,
    int FilesSkipped,
    long TotalBytesRead,
    bool Truncated,
    IReadOnlyDictionary<string, int> ExtensionCounts,
    string EvidenceDigest,
    string MissionStatus,
    string PlanDecision,
    IReadOnlyList<string> PlanSteps,
    IReadOnlyList<string> ReviewBlockers,
    string HandoffPackId,
    string AdvisorDecision,
    string AdvisorProfile,
    int AdvisorInterventionLevel,
    IReadOnlyList<BoundedWorkspaceAdvisorCardSnapshot> AdvisorSuggestions,
    IReadOnlyList<string> AdvisorBlockers,
    bool AdvisorNonExecutor,
    bool RealFilesystemRead,
    bool FilesystemMutationAllowed,
    bool NetworkUsed,
    bool ProductAuthorityGranted);

public static class BoundedWorkspaceUnderstandingEndpointMapper
{
    public const string FeatureFlag = "NODAL_OS_BOUNDED_WORKSPACE_SURFACE_ENABLED";
    public const string RootEnvironmentVariable = "NODAL_OS_WORKSPACE_ROOT";
    public const string AdvisorProfileEnvironmentVariable = "NODAL_OS_ADVISOR_PROFILE";
    public const string AdvisorInterventionEnvironmentVariable = "NODAL_OS_ADVISOR_INTERVENTION_LEVEL";
    public const string JsonRoute = "/api/workspace/understanding";
    public const string HtmlRoute = "/workspace/understanding";

    public static IEndpointRouteBuilder MapBoundedWorkspaceUnderstanding(
        this IEndpointRouteBuilder endpoints,
        IHostEnvironment environment,
        Func<string?>? rootProvider = null,
        Func<BoundedWorkspaceAdvisorSettings>? advisorSettingsProvider = null)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentNullException.ThrowIfNull(environment);
        rootProvider ??= () => Environment.GetEnvironmentVariable(RootEnvironmentVariable);
        advisorSettingsProvider ??= () => ResolveAdvisorSettings(
            Environment.GetEnvironmentVariable(AdvisorProfileEnvironmentVariable),
            Environment.GetEnvironmentVariable(AdvisorInterventionEnvironmentVariable));

        endpoints.MapGet(JsonRoute, async (HttpContext context) =>
        {
            if (!IsRequestAllowed(environment, context.Connection.RemoteIpAddress))
                return Results.NotFound(new { decision = "BOUNDED_WORKSPACE_SURFACE_LOCAL_DEV_ONLY" });

            ApplyReadOnlyHeaders(context.Response);
            var snapshot = await BuildSnapshotAsync(
                    rootProvider(),
                    context.RequestAborted,
                    advisorSettingsProvider())
                .ConfigureAwait(false);
            return Results.Json(snapshot, statusCode: snapshot.Accepted ? StatusCodes.Status200OK : StatusCodes.Status409Conflict);
        });

        endpoints.MapGet(HtmlRoute, async (HttpContext context) =>
        {
            if (!IsRequestAllowed(environment, context.Connection.RemoteIpAddress))
                return Results.NotFound();

            ApplyReadOnlyHeaders(context.Response);
            var snapshot = await BuildSnapshotAsync(
                    rootProvider(),
                    context.RequestAborted,
                    advisorSettingsProvider())
                .ConfigureAwait(false);
            return Results.Content(
                Render(snapshot),
                "text/html; charset=utf-8",
                statusCode: snapshot.Accepted ? StatusCodes.Status200OK : StatusCodes.Status409Conflict);
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
        IsEnabled(environment) && remoteAddress is not null && IPAddress.IsLoopback(remoteAddress);

    public static BoundedWorkspaceAdvisorSettings ResolveAdvisorSettings(
        string? profileValue,
        string? interventionValue)
    {
        var profile = BoundedWorkspaceAdvisorProfile.Balanced;
        if (!string.IsNullOrWhiteSpace(profileValue))
        {
            var normalized = new string(profileValue.Where(char.IsLetterOrDigit).ToArray());
            if (Enum.TryParse<BoundedWorkspaceAdvisorProfile>(normalized, ignoreCase: true, out var parsed))
                profile = parsed;
        }

        var intervention = int.TryParse(interventionValue, out var parsedIntervention)
            ? Math.Clamp(parsedIntervention, 0, 100)
            : 50;
        return new BoundedWorkspaceAdvisorSettings(profile, intervention);
    }

    public static async ValueTask<BoundedWorkspaceOperatorSnapshot> BuildSnapshotAsync(
        string? configuredRoot,
        CancellationToken cancellationToken = default,
        BoundedWorkspaceAdvisorSettings? advisorSettings = null)
    {
        if (string.IsNullOrWhiteSpace(configuredRoot))
            return Blocked("BLOCKED_BOUNDED_WORKSPACE_ROOT_NOT_CONFIGURED", "Workspace root is not configured.", advisorSettings);

        var mission = await new NodalOsBoundedWorkspaceMissionScenario()
            .RunAsync(configuredRoot, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        var planning = mission.Completed && mission.VerificationPassed
            ? new BoundedWorkspacePlanningContextService().Build(
                mission.Scan,
                workspaceId: "workspace-local-configured",
                missionId: mission.Plan.MissionId)
            : null;
        var accepted = mission.Completed &&
                       mission.VerificationPassed &&
                       planning?.Accepted == true &&
                       !mission.FilesystemMutationAllowed &&
                       !mission.NetworkUsed &&
                       !mission.ProductAuthorityGranted;
        var decision = accepted
            ? "GO_BOUNDED_WORKSPACE_OPERATOR_SURFACE_READY"
            : mission.Decision;
        var advisor = planning is null
            ? null
            : new BoundedWorkspaceAdvisorService().Evaluate(
                mission.Scan,
                planning,
                advisorSettings ?? new BoundedWorkspaceAdvisorSettings());
        var advisorCards = advisor?.Suggestions.Select(ToCard).ToArray()
            ?? Array.Empty<BoundedWorkspaceAdvisorCardSnapshot>();

        return new BoundedWorkspaceOperatorSnapshot(
            Decision: decision,
            Accepted: accepted,
            LocalDevOnly: true,
            ReadOnly: true,
            SecretsExcluded: mission.Scan.SecretsExcluded,
            RootConfigured: true,
            ScanDecision: mission.Scan.Decision.ToString(),
            FilesRead: mission.Scan.FilesRead,
            FilesSkipped: mission.Scan.FilesSkipped,
            TotalBytesRead: mission.Scan.TotalBytesRead,
            Truncated: mission.Scan.Truncated,
            ExtensionCounts: mission.Scan.ExtensionCounts,
            EvidenceDigest: mission.Scan.EvidenceDigest,
            MissionStatus: mission.Mission.Status.ToString(),
            PlanDecision: planning?.Decision ?? "BLOCKED_BOUNDED_WORKSPACE_PLAN_NOT_CREATED",
            PlanSteps: planning?.MissionPlan?.Steps.Select(step => step.Intent).ToArray() ?? Array.Empty<string>(),
            ReviewBlockers: planning?.Blockers ?? mission.Scan.Findings,
            HandoffPackId: mission.Handoff.HandoffPackId,
            AdvisorDecision: advisor?.Decision ?? "BLOCKED_EXPERT_ADVISOR_INPUT_FAIL_CLOSED",
            AdvisorProfile: advisor?.Profile.ToString() ?? (advisorSettings?.Profile ?? BoundedWorkspaceAdvisorProfile.Balanced).ToString(),
            AdvisorInterventionLevel: advisor?.InterventionLevel ?? advisorSettings?.InterventionLevel ?? 50,
            AdvisorSuggestions: advisorCards,
            AdvisorBlockers: advisor?.Blockers ?? ["Reviewed planning context is not available."],
            AdvisorNonExecutor: advisor?.NonExecutor ?? true,
            RealFilesystemRead: mission.Scan.RealFilesystemRead,
            FilesystemMutationAllowed: false,
            NetworkUsed: false,
            ProductAuthorityGranted: false);
    }

    public static string Render(BoundedWorkspaceOperatorSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        var extensions = snapshot.ExtensionCounts.Count == 0
            ? "<li>none</li>"
            : string.Join(Environment.NewLine, snapshot.ExtensionCounts.Select(pair => $"<li><strong>{Html(pair.Key)}</strong><span>{pair.Value}</span></li>"));
        var plan = snapshot.PlanSteps.Count == 0
            ? "<li>Plan review unavailable until bounded evidence is verified.</li>"
            : string.Join(Environment.NewLine, snapshot.PlanSteps.Select(step => $"<li>{Html(step)}</li>"));
        var blockers = snapshot.ReviewBlockers.Count == 0
            ? "<p class=\"muted\">No blocker inside this bounded review.</p>"
            : string.Join(Environment.NewLine, snapshot.ReviewBlockers.Select(value => $"<p class=\"notice\">{Html(value)}</p>"));
        var advisor = snapshot.AdvisorSuggestions.Count == 0
            ? "<p class=\"muted\">No material deterministic finding at the current profile and intervention level.</p>"
            : string.Join(Environment.NewLine, snapshot.AdvisorSuggestions.Select(RenderAdvisorCard));
        var advisorBlockers = snapshot.AdvisorBlockers.Count == 0
            ? string.Empty
            : string.Join(Environment.NewLine, snapshot.AdvisorBlockers.Select(value => $"<p class=\"notice\">{Html(value)}</p>"));
        var export = snapshot.Accepted
            ? $"<a class=\"download\" data-action-id=\"download-verified-handoff\" href=\"{BoundedWorkspaceHandoffExportEndpointMapper.MarkdownRoute}\" download>Download verified handoff (.md)</a>"
            : "<p class=\"muted\">Export becomes available after bounded evidence and mission verification pass.</p>";
        var statusClass = snapshot.Accepted ? "ok" : "blocked";

        return $$$"""
            <!doctype html>
            <html lang="en">
            <head>
              <meta charset="utf-8">
              <meta name="viewport" content="width=device-width,initial-scale=1">
              <meta name="robots" content="noindex,nofollow">
              <title>NODAL OS Workspace Understanding</title>
              <style>
                :root{color-scheme:dark;font-family:Inter,ui-sans-serif,system-ui,sans-serif;background:#0d1117;color:#f5f7fa}*{box-sizing:border-box}body{margin:0;background:#0d1117}.shell{min-height:100vh;display:grid;grid-template-columns:250px minmax(0,1fr) 300px;grid-template-rows:auto 1fr auto}.top{grid-column:1/-1;padding:18px 24px;border-bottom:1px solid #30363d;background:#161b22;display:flex;justify-content:space-between;align-items:center;gap:16px}.top h1{font-size:18px;margin:0}.badge,.chip{display:inline-flex;padding:6px 10px;border:1px solid #4f7cff;border-radius:999px;color:#b8c7ff}.badge.ok{border-color:#2ea043;color:#7ee787}.badge.blocked{border-color:#d29922;color:#e3b341}.chip{font-size:11px;padding:4px 8px}.side,.right{padding:20px;background:#161b22}.side{border-right:1px solid #30363d}.right{border-left:1px solid #30363d}.main{padding:24px;min-width:0}.card{background:#1c2128;border:1px solid #30363d;border-radius:12px;padding:16px;margin-bottom:16px}.metric{display:flex;justify-content:space-between;gap:16px;padding:8px 0;border-bottom:1px solid #30363d}.metric:last-child{border-bottom:0}.metric strong{word-break:break-word;text-align:right}.list{list-style:none;padding:0;margin:0}.list li{display:flex;justify-content:space-between;gap:16px;padding:7px 0;border-bottom:1px solid #30363d}.plan li{margin:8px 0;color:#d0d7de}.notice{border-left:3px solid #d29922;padding:8px 10px;background:#251f12;color:#e3b341}.muted{color:#8b949e}.advisor-card{border:1px solid #30363d;border-left:3px solid #7c5cff;border-radius:10px;padding:14px;margin-top:12px;background:#171b28}.advisor-card.high{border-left-color:#f85149}.advisor-card.medium{border-left-color:#d29922}.advisor-card.low{border-left-color:#4f7cff}.advisor-head{display:flex;justify-content:space-between;gap:12px;align-items:flex-start}.advisor-head h3{margin:4px 0 0;font-size:15px}.advisor-card p{color:#aab4c0;line-height:1.5}.advisor-actions{display:flex;flex-wrap:wrap;gap:6px}.download{display:inline-flex;align-items:center;justify-content:center;padding:10px 14px;border:1px solid #4f7cff;border-radius:10px;background:#202a44;color:#dbe5ff;font-weight:700;text-decoration:none}.download:focus-visible{outline:2px solid #7c5cff;outline-offset:3px}.bottom{grid-column:1/-1;padding:12px 24px;border-top:1px solid #30363d;background:#161b22;color:#8b949e;font-size:13px}@media(max-width:900px){.shell{grid-template-columns:1fr}.top,.bottom{grid-column:1}.side,.right{border:0;border-bottom:1px solid #30363d}}
              </style>
            </head>
            <body>
              <div class="shell" data-nodal-os="bounded-workspace-understanding" data-local-dev-only="true" data-read-only="true" data-product-authority="false">
                <header class="top">
                  <div><h1>Bounded Workspace Understanding</h1><div class="muted">Explicit local root · bounded evidence · reviewed plan</div></div>
                  <span class="badge {{{statusClass}}}">{{{Html(snapshot.Decision)}}}</span>
                </header>
                <aside class="side" data-section-id="workspace">
                  <h2>Workspace</h2>
                  <div class="metric"><span>Root configured</span><strong>{{{snapshot.RootConfigured}}}</strong></div>
                  <div class="metric"><span>Scan</span><strong>{{{Html(snapshot.ScanDecision)}}}</strong></div>
                  <div class="metric"><span>Files read</span><strong>{{{snapshot.FilesRead}}}</strong></div>
                  <div class="metric"><span>Bytes sampled</span><strong>{{{snapshot.TotalBytesRead}}}</strong></div>
                  <div class="metric"><span>Truncated</span><strong>{{{snapshot.Truncated}}}</strong></div>
                  <h3>Extensions</h3><ul class="list">{{{extensions}}}</ul>
                </aside>
                <main class="main">
                  <section class="card" data-section-id="mission"><h2>Mission</h2><div class="metric"><span>Status</span><strong>{{{Html(snapshot.MissionStatus)}}}</strong></div><div class="metric"><span>Plan decision</span><strong>{{{Html(snapshot.PlanDecision)}}}</strong></div></section>
                  <section class="card" data-section-id="plan"><h2>Reviewed plan</h2><ol class="plan">{{{plan}}}</ol></section>
                  <section class="card" data-section-id="advisor"><div class="advisor-head"><div><span class="chip">non-executor</span><h2>Expert Advisor</h2></div><span class="chip">{{{Html(snapshot.AdvisorProfile)}}} · {{{snapshot.AdvisorInterventionLevel}}}</span></div><p class="muted">{{{Html(snapshot.AdvisorDecision)}}}</p>{{{advisorBlockers}}}{{{advisor}}}</section>
                  <section class="card" data-section-id="blockers"><h2>Scope and blockers</h2>{{{blockers}}}</section>
                  <section class="card" data-section-id="evidence"><h2>Evidence</h2><div class="metric"><span>Digest</span><strong>{{{Html(snapshot.EvidenceDigest)}}}</strong></div><div class="metric"><span>Handoff</span><strong>{{{Html(snapshot.HandoffPackId)}}}</strong></div></section>
                  <section class="card" data-section-id="export"><h2>Export</h2>{{{export}}}</section>
                </main>
                <aside class="right" data-section-id="boundaries">
                  <h2>Boundaries</h2>
                  <div class="metric"><span>Secrets</span><strong>{{{snapshot.SecretsExcluded}}}</strong></div>
                  <div class="metric"><span>Advisor executor</span><strong>{{{!snapshot.AdvisorNonExecutor}}}</strong></div>
                  <div class="metric"><span>Filesystem read</span><strong>{{{snapshot.RealFilesystemRead}}}</strong></div>
                  <div class="metric"><span>Mutation</span><strong>{{{snapshot.FilesystemMutationAllowed}}}</strong></div>
                  <div class="metric"><span>Network</span><strong>{{{snapshot.NetworkUsed}}}</strong></div>
                  <div class="metric"><span>Product authority</span><strong>{{{snapshot.ProductAuthorityGranted}}}</strong></div>
                </aside>
                <footer class="bottom">Local/dev read-only operator surface. Expert Advisor is deterministic and non-executing. Root paths and raw secret-bearing content are never rendered. The only enabled action downloads the verified redacted handoff. No scripts, forms, external resources, provider calls or execution authority.</footer>
              </div>
            </body>
            </html>
            """;
    }

    private static string RenderAdvisorCard(BoundedWorkspaceAdvisorCardSnapshot card)
    {
        var actions = string.Join("", card.ActionOptions.Select(value => $"<span class=\"chip\">{Html(value)}</span>"));
        return $"""
            <article class="advisor-card {Html(card.Severity.ToLowerInvariant())}" data-advisor-id="{Html(card.SuggestionId)}">
              <div class="advisor-head"><div><span class="chip">{Html(card.Category)}</span><h3>{Html(card.Title)}</h3></div><span class="chip">{Html(card.Severity)}</span></div>
              <p>{Html(card.MessageRedacted)}</p>
              <div class="advisor-actions" aria-label="Read-only suggestion options">{actions}</div>
            </article>
            """;
    }

    private static BoundedWorkspaceAdvisorCardSnapshot ToCard(BoundedWorkspaceAdvisorSuggestion suggestion) => new(
        SuggestionId: suggestion.SuggestionId,
        Category: suggestion.Category.ToString(),
        Severity: suggestion.Severity.ToString(),
        Title: suggestion.Title,
        MessageRedacted: suggestion.MessageRedacted,
        EvidenceRefs: suggestion.EvidenceRefs,
        ActionOptions: suggestion.ActionOptions.Select(value => value.ToString()).ToArray(),
        RequiresHumanAttention: suggestion.RequiresHumanAttention,
        NonExecutable: suggestion.NonExecutable,
        CanAuthorizeExecution: suggestion.CanAuthorizeExecution);

    private static BoundedWorkspaceOperatorSnapshot Blocked(
        string decision,
        string blocker,
        BoundedWorkspaceAdvisorSettings? advisorSettings = null)
    {
        advisorSettings ??= new BoundedWorkspaceAdvisorSettings();
        return new BoundedWorkspaceOperatorSnapshot(
            Decision: decision,
            Accepted: false,
            LocalDevOnly: true,
            ReadOnly: true,
            SecretsExcluded: true,
            RootConfigured: false,
            ScanDecision: "NotStarted",
            FilesRead: 0,
            FilesSkipped: 0,
            TotalBytesRead: 0,
            Truncated: false,
            ExtensionCounts: new Dictionary<string, int>(),
            EvidenceDigest: string.Empty,
            MissionStatus: "Blocked",
            PlanDecision: "BLOCKED_BOUNDED_WORKSPACE_PLAN_NOT_CREATED",
            PlanSteps: Array.Empty<string>(),
            ReviewBlockers: [blocker],
            HandoffPackId: string.Empty,
            AdvisorDecision: "BLOCKED_EXPERT_ADVISOR_INPUT_FAIL_CLOSED",
            AdvisorProfile: advisorSettings.Profile.ToString(),
            AdvisorInterventionLevel: advisorSettings.InterventionLevel,
            AdvisorSuggestions: Array.Empty<BoundedWorkspaceAdvisorCardSnapshot>(),
            AdvisorBlockers: ["Reviewed planning context is not available."],
            AdvisorNonExecutor: true,
            RealFilesystemRead: false,
            FilesystemMutationAllowed: false,
            NetworkUsed: false,
            ProductAuthorityGranted: false);
    }

    private static void ApplyReadOnlyHeaders(HttpResponse response)
    {
        response.Headers.CacheControl = "no-store";
        response.Headers.Pragma = "no-cache";
        response.Headers.XContentTypeOptions = "nosniff";
        response.Headers.ContentSecurityPolicy = "default-src 'none'; style-src 'unsafe-inline'; frame-ancestors 'none'; base-uri 'none'; form-action 'none'";
        response.Headers["Referrer-Policy"] = "no-referrer";
    }

    private static string Html(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);
}
