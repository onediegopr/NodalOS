using System.Net;
using OneBrain.AgentOperations.Contracts;
using OneBrain.AgentOperations.Core.Runtime;

namespace OneBrain.Pilot;

public sealed record MissionControlProductTimelineItem(
    int Sequence,
    string EventId,
    string Title,
    string Detail,
    string State,
    IReadOnlyList<string> EvidenceRefs);

public sealed record MissionControlProductContextItem(
    string Id,
    string Label,
    string Value,
    string Detail,
    string State);

public sealed record MissionControlProductShellSnapshot(
    string Decision,
    bool Accepted,
    string ProductMode,
    string MissionId,
    string RunId,
    string Goal,
    string MissionStatus,
    int ProgressPercent,
    string CurrentStep,
    string ApprovalState,
    string WorkspaceState,
    string LogicalModel,
    string ActiveProvider,
    string ActiveModel,
    string? RecentFallback,
    string BrowserRuntime,
    string BrowserState,
    IReadOnlyList<MissionControlProductTimelineItem> Timeline,
    IReadOnlyList<MissionControlProductContextItem> Context,
    IReadOnlyList<string> EvidenceRefs,
    IReadOnlyList<string> Diagnostics,
    bool LocalOnly,
    bool ReadOnly,
    bool FixtureBacked,
    bool SecretsExcluded,
    bool ExternalIoUsed,
    bool NetworkUsed,
    bool ProductAuthorityGranted);

public static class MissionControlProductShellEndpointMapper
{
    public const string JsonRoute = "/api/mission-control";
    public const string HtmlRoute = "/";
    public const string LegacyPilotRoute = "/pilot/legacy";
    public const string Decision = "GO_MISSION_CONTROL_PRODUCT_SHELL_V1_READY";

    public static IEndpointRouteBuilder MapMissionControlProductShell(
        this IEndpointRouteBuilder endpoints,
        IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentNullException.ThrowIfNull(environment);

        endpoints.MapGet(JsonRoute, async (HttpContext context) =>
        {
            if (!IsRequestAllowed(context.Connection.RemoteIpAddress))
                return Results.NotFound(new { decision = "MISSION_CONTROL_LOCAL_ONLY" });

            ApplyReadOnlyHeaders(context.Response);
            var snapshot = await BuildSnapshotAsync(context.RequestAborted).ConfigureAwait(false);
            return Results.Json(snapshot);
        });

        endpoints.MapGet(HtmlRoute, async (HttpContext context) =>
        {
            if (!IsRequestAllowed(context.Connection.RemoteIpAddress))
                return Results.NotFound();

            ApplyReadOnlyHeaders(context.Response);
            var snapshot = await BuildSnapshotAsync(context.RequestAborted).ConfigureAwait(false);
            return Results.Content(
                MissionControlProductShellHtmlRenderer.Render(snapshot),
                "text/html; charset=utf-8");
        });

        endpoints.MapGet(LegacyPilotRoute, (HttpContext context) =>
        {
            if (!IsRequestAllowed(context.Connection.RemoteIpAddress))
                return Results.NotFound();

            ApplyReadOnlyHeaders(context.Response);
            return Results.Content(PilotHomePageRenderer.Render(), "text/html; charset=utf-8");
        });

        return endpoints;
    }

    public static async ValueTask<MissionControlProductShellSnapshot> BuildSnapshotAsync(
        CancellationToken cancellationToken = default)
    {
        var result = await new NodalOsSelectiveRuntimeFixtureScenario()
            .RunAsync(cancellationToken)
            .ConfigureAwait(false);

        var timeline = result.Timeline
            .OrderBy(value => value.CreatedAt)
            .Select((value, index) => new MissionControlProductTimelineItem(
                Sequence: index + 1,
                EventId: value.EventId,
                Title: TimelineTitle(value),
                Detail: value.SummaryRedacted,
                State: TimelineState(value),
                EvidenceRefs: value.EvidenceRefs
                    .Select(reference => reference.EvidenceId)
                    .Where(reference => !string.IsNullOrWhiteSpace(reference))
                    .Distinct(StringComparer.Ordinal)
                    .Take(8)
                    .ToArray()))
            .ToArray();

        var evidenceRefs = result.Mission.EvidenceRefs
            .Concat(timeline.SelectMany(value => value.EvidenceRefs))
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.Ordinal)
            .Take(24)
            .ToArray();

        var activeProvider = ValueOr(result.Inspector.ActiveProvider, "not selected");
        var activeModel = ValueOr(result.Inspector.ActiveModel, "not selected");
        var logicalModel = ValueOr(result.Inspector.LogicalModel, "not selected");
        var recentFallback = result.ResumeCard.RecentFallback ?? result.Inspector.RecentFallbacks.LastOrDefault();
        var readyCapabilities = result.Inspector.Capabilities.Count(value => value.Contains(":Ready:", StringComparison.Ordinal));
        var totalCapabilities = result.Inspector.Capabilities.Count;
        var readyProviders = result.Inspector.Providers.Count(value => value.Contains(":Ready:", StringComparison.Ordinal));
        var totalProviders = result.Inspector.Providers.Count;

        var context = new[]
        {
            new MissionControlProductContextItem(
                "model",
                "Active model",
                activeModel,
                $"{activeProvider} · logical alias {logicalModel}",
                "active"),
            new MissionControlProductContextItem(
                "fallback",
                "Fallback policy",
                recentFallback is null ? "No fallback used" : "Fallback applied automatically",
                recentFallback ?? "The current route completed without changing provider.",
                recentFallback is null ? "ready" : "fallback"),
            new MissionControlProductContextItem(
                "capabilities",
                "Capabilities",
                $"{readyCapabilities}/{totalCapabilities} ready",
                "The registry reports availability; policy still controls use.",
                readyCapabilities == totalCapabilities ? "ready" : "attention"),
            new MissionControlProductContextItem(
                "providers",
                "Providers",
                $"{readyProviders}/{totalProviders} ready",
                "Local fixture providers prove routing and fallback without external calls.",
                readyProviders > 0 ? "ready" : "attention"),
            new MissionControlProductContextItem(
                "workspace",
                "Workspace",
                "Fixture-backed preview",
                "Explicit real local workspace selection and persistence is the next product gate.",
                "attention"),
            new MissionControlProductContextItem(
                "advisor",
                "Expert Advisor",
                "Observer · non-executor",
                "Advisor context can inform the mission but cannot authorize or execute work.",
                "neutral"),
            new MissionControlProductContextItem(
                "browser",
                result.Inspector.Browser.Runtime,
                result.Inspector.Browser.State,
                result.Inspector.Browser.LastError ?? "Browser runtime is healthy.",
                result.Inspector.Browser.State.Contains("BLOCKED", StringComparison.OrdinalIgnoreCase) ? "blocked" : "ready"),
            new MissionControlProductContextItem(
                "human-control",
                "Human control",
                result.ApprovalRequested ? "Review required" : "No pending decision",
                result.ApprovalRequested
                    ? "The mission is waiting at a material boundary."
                    : "Mission-level scope avoids per-step approval prompts for ordinary work.",
                result.ApprovalRequested ? "attention" : "ready")
        };

        var progressPercent = (int)Math.Clamp(
            Math.Round(result.Mission.Progress * 100, MidpointRounding.AwayFromZero),
            0,
            100);

        return new MissionControlProductShellSnapshot(
            Decision,
            Accepted: true,
            ProductMode: "Local product preview",
            MissionId: result.Mission.MissionId,
            RunId: result.Mission.RunId,
            Goal: result.Plan.Goal,
            MissionStatus: result.Mission.Status.ToString(),
            ProgressPercent: progressPercent,
            CurrentStep: result.ResumeCard.CurrentStep ?? "Mission complete",
            ApprovalState: result.ApprovalRequested
                ? "Human review required"
                : "Mission scope authorized · no per-step prompt",
            WorkspaceState: "Fixture workspace · real selection next",
            LogicalModel: logicalModel,
            ActiveProvider: activeProvider,
            ActiveModel: activeModel,
            RecentFallback: recentFallback,
            BrowserRuntime: result.Inspector.Browser.Runtime,
            BrowserState: result.Inspector.Browser.State,
            Timeline: timeline,
            Context: context,
            EvidenceRefs: evidenceRefs,
            Diagnostics:
            [
                $"run:{result.Mission.RunId}",
                $"mission:{result.Mission.MissionId}",
                $"logical-model:{logicalModel}",
                $"provider:{activeProvider}",
                $"browser:{result.Inspector.Browser.State}",
                $"external-io:{result.ExternalIoUsed.ToString().ToLowerInvariant()}",
                $"network:{result.NetworkUsed.ToString().ToLowerInvariant()}"
            ],
            LocalOnly: true,
            ReadOnly: true,
            FixtureBacked: true,
            SecretsExcluded: true,
            ExternalIoUsed: result.ExternalIoUsed,
            NetworkUsed: result.NetworkUsed,
            ProductAuthorityGranted: false);
    }

    public static bool IsRequestAllowed(IPAddress? remoteAddress) =>
        remoteAddress is not null && IPAddress.IsLoopback(remoteAddress);

    private static string TimelineTitle(NodalOsCoreTimelineProjection value)
    {
        if (value.Kind == NodalOsCoreEventKind.WarningRaised &&
            value.SummaryRedacted.Contains("fallback", StringComparison.OrdinalIgnoreCase))
        {
            return "Fallback activated";
        }

        if (value.Kind == NodalOsCoreEventKind.EvidenceAttached &&
            value.SummaryRedacted.Contains("verif", StringComparison.OrdinalIgnoreCase))
        {
            return "Verification and evidence";
        }

        return value.Kind switch
        {
            NodalOsCoreEventKind.ExecutionRequestRegistered => "Mission runtime event",
            NodalOsCoreEventKind.PolicyGateEvaluated => "Policy evaluated",
            NodalOsCoreEventKind.ApprovalRequired => "Human decision required",
            NodalOsCoreEventKind.ApprovalGranted => "Mission scope approved",
            NodalOsCoreEventKind.ApprovalRejected => "Decision rejected",
            NodalOsCoreEventKind.DryRunPlanCreated => "Plan prepared",
            NodalOsCoreEventKind.ExecutionCompleted => "Mission completed",
            NodalOsCoreEventKind.ExecutionFailed => "Execution failed",
            NodalOsCoreEventKind.EvidenceAttached => "Evidence attached",
            NodalOsCoreEventKind.WarningRaised => "Attention",
            NodalOsCoreEventKind.HumanHandoffRequired => "Human handoff required",
            NodalOsCoreEventKind.RedactionApplied => "Sensitive data redacted",
            _ => "Mission event"
        };
    }

    private static string TimelineState(NodalOsCoreTimelineProjection value) => value.Kind switch
    {
        NodalOsCoreEventKind.ExecutionFailed or NodalOsCoreEventKind.HumanHandoffRequired => "blocked",
        NodalOsCoreEventKind.ApprovalRequired => "attention",
        NodalOsCoreEventKind.WarningRaised => value.SummaryRedacted.Contains("fallback", StringComparison.OrdinalIgnoreCase)
            ? "fallback"
            : "attention",
        _ => "complete"
    };

    private static string ValueOr(string? value, string fallback) =>
        string.IsNullOrWhiteSpace(value) ? fallback : value;

    private static void ApplyReadOnlyHeaders(HttpResponse response)
    {
        response.Headers.CacheControl = "no-store";
        response.Headers.Pragma = "no-cache";
        response.Headers.XContentTypeOptions = "nosniff";
        response.Headers.XFrameOptions = "DENY";
        response.Headers["Referrer-Policy"] = "no-referrer";
        response.Headers["Content-Security-Policy"] =
            "default-src 'none'; style-src 'unsafe-inline'; img-src 'none'; font-src 'none'; connect-src 'none'; frame-ancestors 'none'; base-uri 'none'; form-action 'none'";
    }
}
