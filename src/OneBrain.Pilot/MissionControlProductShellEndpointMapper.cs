using System.Net;
using OneBrain.AgentOperations.Contracts;
using OneBrain.AgentOperations.Core.Runtime;
using OneBrain.AgentOperations.Core.Workspace;

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
    bool WorkspaceSelected,
    bool WorkspacePersisted,
    string? WorkspaceId,
    string? WorkspaceFingerprint,
    int WorkspaceFilesRead,
    bool RealMissionDraft,
    bool MissionDraftPersisted,
    string? ActionCandidateKind,
    string? ActionCandidateTarget,
    bool ActionExecutionEnabled,
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
        IHostEnvironment environment,
        Func<NodalOsWorkspaceSelectionService>? workspaceSelectionServiceFactory = null,
        Func<NodalOsWorkspaceMissionDraftService>? missionDraftServiceFactory = null)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentNullException.ThrowIfNull(environment);
        workspaceSelectionServiceFactory ??= NodalOsWorkspaceSelectionRuntime.CreateDefault;
        missionDraftServiceFactory ??= NodalOsWorkspaceMissionDraftRuntime.CreateDefault;

        endpoints.MapGet(JsonRoute, async (HttpContext context) =>
        {
            if (!IsRequestAllowed(context.Connection.RemoteIpAddress))
                return Results.NotFound(new { decision = "MISSION_CONTROL_LOCAL_ONLY" });

            ApplyReadOnlyHeaders(context.Response);
            var snapshot = await BuildSnapshotAsync(
                    context.RequestAborted,
                    workspaceSelectionServiceFactory(),
                    missionDraftServiceFactory())
                .ConfigureAwait(false);
            return Results.Json(snapshot);
        });

        endpoints.MapGet(HtmlRoute, async (HttpContext context) =>
        {
            if (!IsRequestAllowed(context.Connection.RemoteIpAddress))
                return Results.NotFound();

            ApplyReadOnlyHeaders(context.Response);
            var snapshot = await BuildSnapshotAsync(
                    context.RequestAborted,
                    workspaceSelectionServiceFactory(),
                    missionDraftServiceFactory())
                .ConfigureAwait(false);
            return Results.Content(
                MissionControlProductShellHtmlRenderer.Render(snapshot),
                "text/html; charset=utf-8");
        }).WithOrder(-100);

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
        CancellationToken cancellationToken = default,
        NodalOsWorkspaceSelectionService? workspaceSelectionService = null,
        NodalOsWorkspaceMissionDraftService? missionDraftService = null)
    {
        var result = await new NodalOsSelectiveRuntimeFixtureScenario()
            .RunAsync(cancellationToken)
            .ConfigureAwait(false);
        var workspaceSelection = workspaceSelectionService is null
            ? null
            : await workspaceSelectionService.GetCurrentAsync(cancellationToken).ConfigureAwait(false);
        var missionDraft = missionDraftService is null
            ? null
            : await missionDraftService.GetCurrentAsync(cancellationToken).ConfigureAwait(false);
        var workspaceSelected = workspaceSelection?.Accepted == true;
        var realMissionDraft = missionDraft?.Accepted == true &&
            missionDraft.Plan is not null &&
            missionDraft.Binding is not null &&
            missionDraft.Candidate is not null;

        var timeline = realMissionDraft
            ? ProjectPlanTimeline(missionDraft!.Plan!)
            : ProjectFixtureTimeline(result);

        var workspaceEvidence = workspaceSelection?.Workspace?.EvidenceRefs
            .Select(reference => reference.EvidenceId)
            ?? [];
        var missionEvidence = missionDraft?.EvidenceRefs ?? [];
        var evidenceRefs = result.Mission.EvidenceRefs
            .Concat(timeline.SelectMany(value => value.EvidenceRefs))
            .Concat(workspaceEvidence)
            .Concat(missionEvidence)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.Ordinal)
            .Take(32)
            .ToArray();

        var activeProvider = ValueOr(result.Inspector.ActiveProvider, "not selected");
        var activeModel = ValueOr(result.Inspector.ActiveModel, "not selected");
        var logicalModel = ValueOr(result.Inspector.LogicalModel, "not selected");
        var recentFallback = result.ResumeCard.RecentFallback ?? result.Inspector.RecentFallbacks.LastOrDefault();
        var readyCapabilities = result.Inspector.Capabilities.Count(value => value.Contains(":Ready:", StringComparison.Ordinal));
        var totalCapabilities = result.Inspector.Capabilities.Count;
        var readyProviders = result.Inspector.Providers.Count(value => value.Contains(":Ready:", StringComparison.Ordinal));
        var totalProviders = result.Inspector.Providers.Count;
        var workspaceDisplay = workspaceSelected
            ? workspaceSelection!.DisplayNameRedacted ?? "Selected Local Workspace"
            : "No real workspace selected";
        var workspaceDetail = workspaceSelected
            ? $"Protected local selection · {workspaceSelection!.FilesRead} bounded file refs · fingerprint {Short(workspaceSelection.RootPathFingerprint, 12)}."
            : "Select and persist a real local workspace before moving the mission runtime off fixtures.";

        var context = new List<MissionControlProductContextItem>
        {
            new(
                "mission",
                "Mission",
                realMissionDraft ? "Real workspace draft" : "Fixture mission",
                realMissionDraft
                    ? "The goal, plan and action candidate are persisted and bound to the selected workspace."
                    : "Create a real workspace mission draft to replace the runtime fixture projection.",
                realMissionDraft ? "ready" : "attention"),
            new(
                "model",
                "Active model",
                activeModel,
                $"{activeProvider} · logical alias {logicalModel}",
                "active"),
            new(
                "fallback",
                "Fallback policy",
                recentFallback is null ? "No fallback used" : "Fallback applied automatically",
                recentFallback ?? "The current route completed without changing provider.",
                recentFallback is null ? "ready" : "fallback"),
            new(
                "capabilities",
                "Capabilities",
                $"{readyCapabilities}/{totalCapabilities} ready",
                "The registry reports availability; policy still controls use.",
                readyCapabilities == totalCapabilities ? "ready" : "attention"),
            new(
                "providers",
                "Providers",
                $"{readyProviders}/{totalProviders} ready",
                "Local fixture providers prove routing and fallback without external calls.",
                readyProviders > 0 ? "ready" : "attention"),
            new(
                "workspace",
                "Workspace",
                workspaceDisplay,
                workspaceDetail,
                workspaceSelected ? "ready" : "attention"),
            new(
                "advisor",
                "Expert Advisor",
                "Observer · non-executor",
                "Advisor context can inform the mission but cannot authorize or execute work.",
                "neutral"),
            new(
                "browser",
                result.Inspector.Browser.Runtime,
                result.Inspector.Browser.State,
                result.Inspector.Browser.LastError ?? "Browser runtime is healthy.",
                result.Inspector.Browser.State.Contains("BLOCKED", StringComparison.OrdinalIgnoreCase) ? "blocked" : "ready")
        };

        if (realMissionDraft)
        {
            context.Add(new MissionControlProductContextItem(
                "action",
                "Reviewed action",
                $"{missionDraft!.Candidate!.Kind} · {missionDraft.Candidate.RelativeTargetPath}",
                "Reversible candidate prepared with preconditions, rollback and expected evidence. Execution remains disabled.",
                "attention"));
        }
        context.Add(new MissionControlProductContextItem(
            "human-control",
            "Human control",
            realMissionDraft ? "Mission-scope approval required" : result.ApprovalRequested ? "Review required" : "No pending decision",
            realMissionDraft
                ? "Approval must bind mission, workspace fingerprint, action id, capability and relative target before execution."
                : result.ApprovalRequested
                    ? "The mission is waiting at a material boundary."
                    : "Mission-level scope avoids per-step approval prompts for ordinary work.",
            realMissionDraft || result.ApprovalRequested ? "attention" : "ready"));

        var progressPercent = realMissionDraft
            ? missionDraft!.ProgressPercent
            : (int)Math.Clamp(
                Math.Round(result.Mission.Progress * 100, MidpointRounding.AwayFromZero),
                0,
                100);
        var missionId = realMissionDraft ? missionDraft!.MissionId! : result.Mission.MissionId;
        var runId = realMissionDraft ? $"draft:{missionId}" : result.Mission.RunId;
        var goal = realMissionDraft ? missionDraft!.GoalRedacted! : result.Plan.Goal;
        var missionStatus = realMissionDraft ? "AwaitingMissionScopeApproval" : result.Mission.Status.ToString();
        var currentStep = realMissionDraft ? missionDraft!.CurrentStep : result.ResumeCard.CurrentStep ?? "Mission complete";
        var approvalState = realMissionDraft
            ? missionDraft!.ApprovalState
            : result.ApprovalRequested
                ? "Human review required"
                : "Mission scope authorized · no per-step prompt";

        return new MissionControlProductShellSnapshot(
            Decision,
            Accepted: true,
            ProductMode: realMissionDraft
                ? "Local product preview · real workspace mission draft"
                : workspaceSelected
                    ? "Local product preview · real workspace selected"
                    : "Local product preview",
            MissionId: missionId,
            RunId: runId,
            Goal: goal,
            MissionStatus: missionStatus,
            ProgressPercent: progressPercent,
            CurrentStep: currentStep,
            ApprovalState: approvalState,
            WorkspaceState: workspaceSelected
                ? $"{workspaceDisplay} · protected + revalidated"
                : "Workspace selection required",
            WorkspaceSelected: workspaceSelected,
            WorkspacePersisted: workspaceSelection?.Persisted == true,
            WorkspaceId: workspaceSelection?.WorkspaceId,
            WorkspaceFingerprint: workspaceSelection?.RootPathFingerprint,
            WorkspaceFilesRead: workspaceSelection?.FilesRead ?? 0,
            RealMissionDraft: realMissionDraft,
            MissionDraftPersisted: missionDraft?.Persisted == true,
            ActionCandidateKind: missionDraft?.Candidate?.Kind.ToString(),
            ActionCandidateTarget: missionDraft?.Candidate?.RelativeTargetPath,
            ActionExecutionEnabled: missionDraft?.Candidate?.ExecutionEnabled == true,
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
                $"run:{runId}",
                $"mission:{missionId}",
                $"real-mission-draft:{realMissionDraft.ToString().ToLowerInvariant()}",
                $"mission-draft-persisted:{(missionDraft?.Persisted == true).ToString().ToLowerInvariant()}",
                $"workspace-selected:{workspaceSelected.ToString().ToLowerInvariant()}",
                $"workspace-fingerprint:{Short(workspaceSelection?.RootPathFingerprint, 12)}",
                $"action-target:{missionDraft?.Candidate?.RelativeTargetPath ?? "none"}",
                $"action-execution-enabled:{(missionDraft?.Candidate?.ExecutionEnabled == true).ToString().ToLowerInvariant()}",
                $"logical-model:{logicalModel}",
                $"provider:{activeProvider}",
                $"browser:{result.Inspector.Browser.State}",
                $"external-io:{result.ExternalIoUsed.ToString().ToLowerInvariant()}",
                $"network:{result.NetworkUsed.ToString().ToLowerInvariant()}"
            ],
            LocalOnly: true,
            ReadOnly: true,
            FixtureBacked: !realMissionDraft,
            SecretsExcluded: true,
            ExternalIoUsed: result.ExternalIoUsed,
            NetworkUsed: result.NetworkUsed,
            ProductAuthorityGranted: false);
    }

    public static bool IsRequestAllowed(IPAddress? remoteAddress) =>
        remoteAddress is not null && IPAddress.IsLoopback(remoteAddress);

    private static IReadOnlyList<MissionControlProductTimelineItem> ProjectPlanTimeline(MissionPlan plan) =>
        plan.Steps.Select((step, index) => new MissionControlProductTimelineItem(
            Sequence: index + 1,
            EventId: $"plan-step:{step.Id}",
            Title: PlanStepTitle(step),
            Detail: $"{step.ExecutionSurface} · risk {step.RiskLevel}" +
                    (step.ApprovalRequired ? " · approval required" : string.Empty),
            State: PlanStepState(step.Status),
            EvidenceRefs: step.EvidenceRefs
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.Ordinal)
                .Take(8)
                .ToArray()))
        .ToArray();

    private static IReadOnlyList<MissionControlProductTimelineItem> ProjectFixtureTimeline(
        NodalOsSelectiveRuntimeFixtureResult result) => result.Timeline
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

    private static string PlanStepTitle(MissionStep step) => step.Id switch
    {
        "workspace-context-reviewed" => "Workspace context verified",
        "review-action-candidate" => "Action candidate ready for review",
        "resolve-mission-scope-approval" => "Mission-scope approval pending",
        "execute-controlled-action" => "Controlled action pending",
        "verify-result" => "Verification pending",
        "record-evidence-handoff" => "Evidence and handoff pending",
        _ => step.Intent
    };

    private static string PlanStepState(MissionStepStatus status) => status switch
    {
        MissionStepStatus.Verified => "complete",
        MissionStepStatus.InProgress or MissionStepStatus.ReadyForVerification => "attention",
        MissionStepStatus.Blocked or MissionStepStatus.Failed => "blocked",
        MissionStepStatus.Skipped or MissionStepStatus.Cancelled => "blocked",
        _ => "pending"
    };

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

    private static string Short(string? value, int length) =>
        string.IsNullOrWhiteSpace(value) ? "not-selected" : value[..Math.Min(length, value.Length)];

    private static void ApplyReadOnlyHeaders(HttpResponse response)
    {
        response.Headers.CacheControl = "no-store";
        response.Headers.Pragma = "no-cache";
        response.Headers.XContentTypeOptions = "nosniff";
        response.Headers["X-Frame-Options"] = "DENY";
        response.Headers["Referrer-Policy"] = "no-referrer";
        response.Headers["Content-Security-Policy"] =
            "default-src 'none'; style-src 'unsafe-inline'; img-src 'none'; font-src 'none'; connect-src 'none'; frame-ancestors 'none'; base-uri 'none'; form-action 'none'";
    }
}
