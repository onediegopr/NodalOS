using System.Net;
using OneBrain.AgentOperations.Contracts;
using OneBrain.AgentOperations.Core.Models;
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
    bool ActionApprovalAvailable,
    string? ActionExecutionState,
    bool ActionExecuted,
    bool ActionVerified,
    bool ActionRollbackAvailable,
    bool ActionRolledBack,
    bool ByokConfigured,
    bool ModelConnectionVerified,
    bool ModelFallbackApplied,
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
        Func<NodalOsWorkspaceMissionDraftService>? missionDraftServiceFactory = null,
        Func<NodalOsWorkspaceHandoffExecutionService>? handoffExecutionServiceFactory = null,
        Func<NodalOsByokModelConfigurationService>? byokModelConfigurationServiceFactory = null)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentNullException.ThrowIfNull(environment);
        workspaceSelectionServiceFactory ??= NodalOsWorkspaceSelectionRuntime.CreateDefault;
        missionDraftServiceFactory ??= NodalOsWorkspaceMissionDraftRuntime.CreateDefault;
        handoffExecutionServiceFactory ??= NodalOsWorkspaceHandoffExecutionRuntime.CreateDefault;
        byokModelConfigurationServiceFactory ??= NodalOsByokModelConfigurationRuntime.CreateDefault;

        endpoints.MapGet(JsonRoute, async (HttpContext context) =>
        {
            if (!IsRequestAllowed(context.Connection.RemoteIpAddress))
                return Results.NotFound(new { decision = "MISSION_CONTROL_LOCAL_ONLY" });

            ApplyReadOnlyHeaders(context.Response);
            var snapshot = await BuildSnapshotAsync(
                    context.RequestAborted,
                    workspaceSelectionServiceFactory(),
                    missionDraftServiceFactory(),
                    handoffExecutionServiceFactory(),
                    byokModelConfigurationServiceFactory())
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
                    missionDraftServiceFactory(),
                    handoffExecutionServiceFactory(),
                    byokModelConfigurationServiceFactory())
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
        NodalOsWorkspaceMissionDraftService? missionDraftService = null,
        NodalOsWorkspaceHandoffExecutionService? handoffExecutionService = null,
        NodalOsByokModelConfigurationService? byokModelConfigurationService = null)
    {
        var fixtureMode = workspaceSelectionService is null &&
            missionDraftService is null &&
            handoffExecutionService is null &&
            byokModelConfigurationService is null;
        var fixture = fixtureMode
            ? await new NodalOsSelectiveRuntimeFixtureScenario()
                .RunAsync(cancellationToken)
                .ConfigureAwait(false)
            : null;
        var workspaceSelection = workspaceSelectionService is null
            ? null
            : await workspaceSelectionService.GetCurrentAsync(cancellationToken).ConfigureAwait(false);
        var missionDraft = missionDraftService is null
            ? null
            : await missionDraftService.GetCurrentAsync(cancellationToken).ConfigureAwait(false);
        var execution = handoffExecutionService is null
            ? null
            : await handoffExecutionService.GetCurrentAsync(cancellationToken).ConfigureAwait(false);
        var modelConfiguration = byokModelConfigurationService is null
            ? null
            : await byokModelConfigurationService.GetCurrentAsync(cancellationToken).ConfigureAwait(false);

        var byokConfigured = modelConfiguration?.Configured == true;
        var modelConnectionVerified = modelConfiguration?.Connected == true;
        var workspaceSelected = workspaceSelection?.Accepted == true;
        var realMissionDraft = missionDraft?.Plan is not null &&
            missionDraft.Binding is not null &&
            missionDraft.Candidate is not null &&
            !string.IsNullOrWhiteSpace(missionDraft.MissionId);
        var executionCompleted = execution?.State == NodalOsWorkspaceHandoffExecutionState.Completed && execution.Verified;
        var executionRolledBack = execution?.State == NodalOsWorkspaceHandoffExecutionState.RolledBack;
        var executionBlocked = execution?.State is NodalOsWorkspaceHandoffExecutionState.CandidateStale or
            NodalOsWorkspaceHandoffExecutionState.ResultChanged or
            NodalOsWorkspaceHandoffExecutionState.FailedClosed;
        var approvalAvailable = execution?.State == NodalOsWorkspaceHandoffExecutionState.ReadyForApproval;
        var fixtureApprovalRequested = fixture?.ApprovalRequested == true;
        var fixtureExternalIoUsed = fixture?.ExternalIoUsed == true;
        var fixtureNetworkUsed = fixture?.NetworkUsed == true;

        IReadOnlyList<MissionControlProductTimelineItem> planTimeline = realMissionDraft
            ? ProjectPlanTimeline(missionDraft!.Plan!, execution)
            : fixture is null ? [] : ProjectFixtureTimeline(fixture);
        var executionTimeline = execution?.Timeline.Count > 0
            ? ProjectExecutionTimeline(execution.Timeline, planTimeline.Count)
            : [];
        var modelTimeline = modelConfiguration?.Timeline.Count > 0
            ? ProjectExecutionTimeline(modelConfiguration.Timeline, planTimeline.Count + executionTimeline.Count)
            : [];
        var timeline = planTimeline.Concat(executionTimeline).Concat(modelTimeline).ToArray();

        var workspaceEvidence = workspaceSelection?.Workspace?.EvidenceRefs
            .Select(reference => reference.EvidenceId) ?? [];
        var missionEvidence = missionDraft?.EvidenceRefs ?? [];
        var executionEvidence = execution?.EvidenceRefs ?? [];
        var modelEvidence = modelConfiguration?.EvidenceRefs ?? [];
        var fixtureEvidence = fixture?.Mission.EvidenceRefs ?? [];
        var evidenceRefs = fixtureEvidence
            .Concat(timeline.SelectMany(value => value.EvidenceRefs))
            .Concat(workspaceEvidence)
            .Concat(missionEvidence)
            .Concat(executionEvidence)
            .Concat(modelEvidence)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.Ordinal)
            .Take(48)
            .ToArray();

        var activeProvider = modelConnectionVerified
            ? ValueOr(modelConfiguration!.SelectedProviderId, modelConfiguration.Primary?.ProviderId ?? "not selected")
            : byokConfigured
                ? modelConfiguration!.Primary?.ProviderId ?? "configured"
                : ValueOr(fixture?.Inspector.ActiveProvider, "not configured");
        var activeModel = modelConnectionVerified
            ? ValueOr(modelConfiguration!.SelectedModelId, modelConfiguration.Primary?.ModelId ?? "not selected")
            : byokConfigured
                ? modelConfiguration!.Primary?.ModelId ?? "configured"
                : ValueOr(fixture?.Inspector.ActiveModel, "not configured");
        var logicalModel = byokConfigured
            ? modelConfiguration!.LogicalModel
            : ValueOr(fixture?.Inspector.LogicalModel, "not configured");
        var recentFallback = modelConfiguration?.FallbackApplied == true
            ? $"Pre-authorized BYOK fallback selected {modelConfiguration.SelectedProviderId} / {modelConfiguration.SelectedModelId}."
            : fixture?.ResumeCard.RecentFallback ?? fixture?.Inspector.RecentFallbacks.LastOrDefault();
        var readyCapabilities = fixture?.Inspector.Capabilities.Count(value => value.Contains(":Ready:", StringComparison.Ordinal)) ?? 0;
        var totalCapabilities = fixture?.Inspector.Capabilities.Count ?? 0;
        var readyProviders = fixture?.Inspector.Providers.Count(value => value.Contains(":Ready:", StringComparison.Ordinal)) ?? 0;
        var totalProviders = fixture?.Inspector.Providers.Count ?? 0;
        var browserRuntime = fixture?.Inspector.Browser.Runtime ?? "Not configured";
        var browserState = fixture?.Inspector.Browser.State ?? "Not evaluated";
        var workspaceDisplay = workspaceSelected
            ? workspaceSelection!.DisplayNameRedacted ?? "Selected Local Workspace"
            : "No real workspace selected";
        var workspaceDetail = workspaceSelected
            ? $"Protected local selection · {workspaceSelection!.FilesRead} bounded file refs · fingerprint {Short(workspaceSelection.RootPathFingerprint, 12)}."
            : "Select and persist a local workspace to begin a real mission.";

        var context = new List<MissionControlProductContextItem>
        {
            new(
                "mission",
                "Mission",
                executionCompleted
                    ? "Verified real mission"
                    : executionRolledBack
                        ? "Real mission rolled back"
                        : realMissionDraft
                            ? "Real workspace mission"
                            : fixtureMode ? "Fixture mission" : "Not started",
                executionCompleted
                    ? "The approved handoff action completed with exact post-write verification and canonical evidence."
                    : executionRolledBack
                        ? "The approved action was reversed through its guarded restore plan and the prior state was verified."
                        : realMissionDraft
                            ? "The goal, plan and reviewed action candidate are persisted and bound to the selected workspace."
                            : fixtureMode
                                ? "The explicit development fixture proves the runtime path without product authority."
                                : "Create a mission after selecting a workspace; the product surface does not synthesize fixture work.",
                executionCompleted || executionRolledBack || realMissionDraft ? "ready" : "attention"),
            new(
                "model",
                "Active model",
                activeModel,
                modelConnectionVerified
                    ? $"{activeProvider} · logical alias {logicalModel} · real connection verified"
                    : byokConfigured
                        ? $"{activeProvider} · logical alias {logicalModel} · connection test pending"
                        : fixtureMode
                            ? $"{activeProvider} · logical alias {logicalModel} · fixture-backed"
                            : "Configure a BYOK or loopback provider before model-assisted work.",
                modelConnectionVerified ? "ready" : byokConfigured ? "attention" : "neutral"),
            new(
                "fallback",
                "Fallback policy",
                recentFallback is null ? "No fallback used" : "Fallback applied automatically",
                recentFallback ?? (fixtureMode
                    ? "The fixture route completed without changing provider."
                    : "No model route has run in the current product state."),
                recentFallback is null ? "ready" : "fallback"),
            new(
                "capabilities",
                "Capabilities",
                fixtureMode ? $"{readyCapabilities}/{totalCapabilities} ready" : "Not evaluated",
                fixtureMode
                    ? "The registry reports fixture availability; policy still controls use."
                    : "Capabilities are evaluated when a real mission is reviewed.",
                fixtureMode && readyCapabilities == totalCapabilities ? "ready" : "neutral"),
            new(
                "providers",
                "Providers",
                byokConfigured
                    ? $"{1 + (modelConfiguration!.Fallback is null ? 0 : 1)} configured"
                    : fixtureMode ? $"{readyProviders}/{totalProviders} fixture-ready" : "Not configured",
                modelConnectionVerified
                    ? "A real provider call completed through the existing policy-aware router with redacted evidence."
                    : byokConfigured
                        ? "Configuration is stored securely; run the bounded connection test before using it for missions."
                        : "Configure a BYOK or loopback provider when model assistance is needed.",
                modelConnectionVerified ? "ready" : "attention"),
            new("workspace", "Workspace", workspaceDisplay, workspaceDetail, workspaceSelected ? "ready" : "attention"),
            new(
                "model-connection",
                "BYOK connection",
                modelConnectionVerified ? "Verified" : byokConfigured ? "Test required" : "Not configured",
                modelConnectionVerified
                    ? $"{modelConfiguration!.AttemptCount} bounded attempt(s) · cost ${modelConfiguration.TotalEstimatedCost:0.########} · response hash {Short(modelConfiguration.ResponseSha256, 12)}."
                    : "Keys remain in the local secure store; only opaque references enter configuration and evidence.",
                modelConnectionVerified ? "ready" : "attention"),
            new(
                "advisor",
                "Expert Advisor",
                "Observer · non-executor",
                "Advisor context can inform the mission but cannot authorize or execute work.",
                "neutral")
        };
        if (fixture is not null)
        {
            context.Add(new MissionControlProductContextItem(
                "browser",
                browserRuntime,
                browserState,
                fixture.Inspector.Browser.LastError ?? "Browser runtime is healthy.",
                browserState.Contains("BLOCKED", StringComparison.OrdinalIgnoreCase) ? "blocked" : "ready"));
        }

        if (realMissionDraft)
        {
            var actionValue = $"{missionDraft!.Candidate!.Kind} · {missionDraft.Candidate.RelativeTargetPath}";
            var actionDetail = executionCompleted
                ? $"Executed and verified · result {Short(execution?.ResultSha256, 12)} · rollback available {execution?.RollbackAvailable}."
                : executionRolledBack
                    ? "Execution and guarded rollback both completed with exact-state verification."
                    : executionBlocked
                        ? string.Join(" ", execution?.Blockers ?? [])
                        : "Reversible candidate prepared with exact preconditions, mission-scope approval and a guarded restore plan.";
            context.Add(new MissionControlProductContextItem(
                "action",
                "Controlled action",
                actionValue,
                actionDetail,
                executionBlocked ? "blocked" : executionCompleted || executionRolledBack ? "ready" : "attention"));
        }

        var humanValue = executionCompleted
            ? "Approved once · verified"
            : executionRolledBack
                ? "Approved action rolled back"
                : executionBlocked
                    ? "Intervention required"
                    : approvalAvailable
                        ? "Mission-scope approval required"
                        : realMissionDraft
                            ? "Review execution boundary"
                            : fixtureApprovalRequested ? "Review required" : "No pending decision";
        var humanDetail = executionCompleted
            ? "The approval was bound to mission, workspace fingerprint, action id, capability, target and reviewed hashes."
            : executionRolledBack
                ? "Rollback required the exact result hash and restore-plan identity; no broad filesystem authority was granted."
                : executionBlocked
                    ? string.Join(" ", execution?.Blockers ?? ["The controlled action failed closed."])
                    : realMissionDraft
                        ? "One explicit decision unlocks only the reviewed reversible target; ordinary mission steps do not prompt again."
                        : fixtureMode
                            ? "Mission-level scope avoids per-step approval prompts for ordinary fixture work."
                            : "No approval is required until a reviewed mission proposes a sensitive action.";
        context.Add(new MissionControlProductContextItem(
            "human-control",
            "Human control",
            humanValue,
            humanDetail,
            executionBlocked
                ? "blocked"
                : executionCompleted || executionRolledBack
                    ? "ready"
                    : realMissionDraft || fixtureApprovalRequested ? "attention" : "neutral"));

        var progressPercent = executionCompleted || executionRolledBack
            ? 100
            : executionBlocked
                ? Math.Max(missionDraft?.ProgressPercent ?? 0, 50)
                : realMissionDraft
                    ? missionDraft!.ProgressPercent
                    : fixture is null
                        ? 0
                        : (int)Math.Clamp(Math.Round(fixture.Mission.Progress * 100, MidpointRounding.AwayFromZero), 0, 100);
        var missionId = realMissionDraft
            ? missionDraft!.MissionId!
            : fixture?.Mission.MissionId ?? "mission:not-started";
        var runId = execution?.OperationId is not null
            ? $"execution:{execution.OperationId}"
            : realMissionDraft
                ? $"draft:{missionId}"
                : fixture?.Mission.RunId ?? "run:not-started";
        var goal = realMissionDraft
            ? missionDraft!.GoalRedacted!
            : fixture?.Plan.Goal ?? (workspaceSelected
                ? "Create a mission for the selected workspace"
                : "Select a workspace and create a mission");
        var missionStatus = executionCompleted
            ? "Completed"
            : executionRolledBack
                ? "RolledBack"
                : executionBlocked
                    ? "Blocked"
                    : realMissionDraft
                        ? "AwaitingMissionScopeApproval"
                        : fixture?.Mission.Status.ToString() ?? (workspaceSelected ? "ReadyForMission" : "NotStarted");
        var currentStep = executionCompleted
            ? "Verified handoff and evidence"
            : executionRolledBack
                ? "Rollback verified"
                : executionBlocked
                    ? "Resolve controlled execution blocker"
                    : realMissionDraft
                        ? missionDraft!.CurrentStep
                        : fixture?.ResumeCard.CurrentStep ?? (workspaceSelected ? "Create a mission" : "Select a workspace");
        var approvalState = executionCompleted
            ? "Mission scope approved · execution verified"
            : executionRolledBack
                ? "Mission scope approved · action rolled back"
                : executionBlocked
                    ? "Controlled execution blocked"
                    : approvalAvailable
                        ? "Approve exact mission scope"
                        : realMissionDraft
                            ? missionDraft!.ApprovalState
                            : fixtureApprovalRequested ? "Human review required" : "No pending decision";
        var productMode = executionCompleted
            ? "Local private beta · verified real workspace action"
            : executionRolledBack
                ? "Local private beta · verified rollback"
                : realMissionDraft
                    ? "Local private beta · real workspace mission"
                    : workspaceSelected
                        ? "Local private beta · real workspace selected"
                        : fixtureMode ? "Local development fixture" : "Local private beta";
        var externalIoUsed = fixtureExternalIoUsed || modelConfiguration?.RealProviderCallAttempted == true;
        var networkUsed = fixtureNetworkUsed || modelConfiguration?.NetworkUsed == true;

        return new MissionControlProductShellSnapshot(
            Decision,
            Accepted: true,
            ProductMode: productMode,
            MissionId: missionId,
            RunId: runId,
            Goal: goal,
            MissionStatus: missionStatus,
            ProgressPercent: progressPercent,
            CurrentStep: currentStep,
            ApprovalState: approvalState,
            WorkspaceState: workspaceSelected ? $"{workspaceDisplay} · protected + revalidated" : "Workspace selection required",
            WorkspaceSelected: workspaceSelected,
            WorkspacePersisted: workspaceSelection?.Persisted == true,
            WorkspaceId: workspaceSelection?.WorkspaceId,
            WorkspaceFingerprint: workspaceSelection?.RootPathFingerprint,
            WorkspaceFilesRead: workspaceSelection?.FilesRead ?? 0,
            RealMissionDraft: realMissionDraft,
            MissionDraftPersisted: missionDraft?.Persisted == true,
            ActionCandidateKind: missionDraft?.Candidate?.Kind.ToString() ?? execution?.ActionKind,
            ActionCandidateTarget: missionDraft?.Candidate?.RelativeTargetPath ?? execution?.RelativeTargetPath,
            ActionExecutionEnabled: false,
            ActionApprovalAvailable: approvalAvailable,
            ActionExecutionState: execution?.State.ToString(),
            ActionExecuted: execution?.Executed == true,
            ActionVerified: execution?.Verified == true,
            ActionRollbackAvailable: execution?.RollbackAvailable == true,
            ActionRolledBack: execution?.RolledBack == true,
            ByokConfigured: byokConfigured,
            ModelConnectionVerified: modelConnectionVerified,
            ModelFallbackApplied: modelConfiguration?.FallbackApplied == true,
            LogicalModel: logicalModel,
            ActiveProvider: activeProvider,
            ActiveModel: activeModel,
            RecentFallback: recentFallback,
            BrowserRuntime: browserRuntime,
            BrowserState: browserState,
            Timeline: timeline,
            Context: context,
            EvidenceRefs: evidenceRefs,
            Diagnostics:
            [
                $"run:{runId}",
                $"mission:{missionId}",
                $"fixture-mode:{fixtureMode.ToString().ToLowerInvariant()}",
                $"real-mission-draft:{realMissionDraft.ToString().ToLowerInvariant()}",
                $"mission-draft-persisted:{(missionDraft?.Persisted == true).ToString().ToLowerInvariant()}",
                $"workspace-selected:{workspaceSelected.ToString().ToLowerInvariant()}",
                $"workspace-fingerprint:{Short(workspaceSelection?.RootPathFingerprint, 12)}",
                $"action-target:{missionDraft?.Candidate?.RelativeTargetPath ?? execution?.RelativeTargetPath ?? "none"}",
                $"action-execution-state:{execution?.State.ToString() ?? "not-configured"}",
                $"action-executed:{(execution?.Executed == true).ToString().ToLowerInvariant()}",
                $"action-verified:{(execution?.Verified == true).ToString().ToLowerInvariant()}",
                $"rollback-available:{(execution?.RollbackAvailable == true).ToString().ToLowerInvariant()}",
                $"byok-configured:{byokConfigured.ToString().ToLowerInvariant()}",
                $"model-connection-verified:{modelConnectionVerified.ToString().ToLowerInvariant()}",
                $"model-fallback-applied:{(modelConfiguration?.FallbackApplied == true).ToString().ToLowerInvariant()}",
                $"logical-model:{logicalModel}",
                $"provider:{activeProvider}",
                $"browser:{browserState}",
                $"external-io:{externalIoUsed.ToString().ToLowerInvariant()}",
                $"network:{networkUsed.ToString().ToLowerInvariant()}"
            ],
            LocalOnly: true,
            ReadOnly: true,
            FixtureBacked: fixtureMode,
            SecretsExcluded: true,
            ExternalIoUsed: externalIoUsed,
            NetworkUsed: networkUsed,
            ProductAuthorityGranted: false);
    }

    public static bool IsRequestAllowed(IPAddress? remoteAddress) =>
        remoteAddress is not null && IPAddress.IsLoopback(remoteAddress);

    private static IReadOnlyList<MissionControlProductTimelineItem> ProjectPlanTimeline(
        MissionPlan plan,
        NodalOsWorkspaceHandoffExecutionSnapshot? execution) =>
        plan.Steps.Select((step, index) => new MissionControlProductTimelineItem(
            Sequence: index + 1,
            EventId: $"plan-step:{step.Id}",
            Title: PlanStepTitle(step),
            Detail: PlanStepDetail(step, execution),
            State: PlanStepState(step, execution),
            EvidenceRefs: step.EvidenceRefs
                .Concat(execution?.EvidenceRefs ?? [])
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.Ordinal)
                .Take(8)
                .ToArray()))
        .ToArray();

    private static IReadOnlyList<MissionControlProductTimelineItem> ProjectExecutionTimeline(
        IReadOnlyList<NodalOsCoreTimelineProjection> events,
        int offset) => events
        .OrderBy(value => value.CreatedAt)
        .Select((value, index) => new MissionControlProductTimelineItem(
            Sequence: offset + index + 1,
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
        "review-action-candidate" => "Action candidate reviewed",
        "resolve-mission-scope-approval" => "Mission-scope approval",
        "execute-controlled-action" => "Controlled handoff action",
        "verify-result" => "Post-write verification",
        "record-evidence-handoff" => "Evidence and handoff",
        _ => step.Intent
    };

    private static string PlanStepDetail(
        MissionStep step,
        NodalOsWorkspaceHandoffExecutionSnapshot? execution)
    {
        if (execution is null)
            return $"{step.ExecutionSurface} · risk {step.RiskLevel}" + (step.ApprovalRequired ? " · approval required" : string.Empty);
        return step.Id switch
        {
            "resolve-mission-scope-approval" when execution.ApprovalDecisionId is not null => $"Approved once · {execution.ApprovalScope}",
            "execute-controlled-action" when execution.Executed => $"{execution.ActionKind} · {execution.RelativeTargetPath} · result {Short(execution.ResultSha256, 12)}",
            "verify-result" when execution.Verified => "Exact bytes and SHA-256 verified; no shell, network or extra target used.",
            "record-evidence-handoff" when execution.EvidenceRefs.Count > 0 => $"{execution.EvidenceRefs.Count} canonical evidence refs attached.",
            _ => $"{step.ExecutionSurface} · risk {step.RiskLevel}" + (step.ApprovalRequired ? " · approval required" : string.Empty)
        };
    }

    private static string PlanStepState(
        MissionStep step,
        NodalOsWorkspaceHandoffExecutionSnapshot? execution)
    {
        if (execution is not null)
        {
            if (execution.State == NodalOsWorkspaceHandoffExecutionState.RolledBack)
                return "complete";
            if (execution.State == NodalOsWorkspaceHandoffExecutionState.Completed && execution.Verified)
                return "complete";
            if (execution.State is NodalOsWorkspaceHandoffExecutionState.CandidateStale or NodalOsWorkspaceHandoffExecutionState.ResultChanged or NodalOsWorkspaceHandoffExecutionState.FailedClosed &&
                step.Id is "execute-controlled-action" or "verify-result" or "record-evidence-handoff")
                return "blocked";
            if (execution.ApprovalDecisionId is not null && step.Id == "resolve-mission-scope-approval")
                return "complete";
        }

        return step.Status switch
        {
            MissionStepStatus.Verified => "complete",
            MissionStepStatus.InProgress or MissionStepStatus.ReadyForVerification => "attention",
            MissionStepStatus.Blocked or MissionStepStatus.Failed or MissionStepStatus.Skipped or MissionStepStatus.Cancelled => "blocked",
            _ => "pending"
        };
    }

    private static string TimelineTitle(NodalOsCoreTimelineProjection value)
    {
        if (value.Kind == NodalOsCoreEventKind.WarningRaised &&
            value.SummaryRedacted.Contains("fallback", StringComparison.OrdinalIgnoreCase))
            return "Fallback activated";
        if (value.Kind == NodalOsCoreEventKind.EvidenceAttached &&
            value.SummaryRedacted.Contains("rollback", StringComparison.OrdinalIgnoreCase))
            return "Rollback verified";
        if (value.Kind == NodalOsCoreEventKind.EvidenceAttached &&
            value.SummaryRedacted.Contains("verif", StringComparison.OrdinalIgnoreCase))
            return "Verification and evidence";

        return value.Kind switch
        {
            NodalOsCoreEventKind.ExecutionRequestRegistered => "Mission runtime event",
            NodalOsCoreEventKind.PolicyGateEvaluated => "Policy evaluated",
            NodalOsCoreEventKind.ApprovalRequired => "Human decision required",
            NodalOsCoreEventKind.ApprovalGranted => "Mission scope approved",
            NodalOsCoreEventKind.ApprovalRejected => "Decision rejected",
            NodalOsCoreEventKind.DryRunPlanCreated => "Controlled write prepared",
            NodalOsCoreEventKind.ExecutionCompleted => "Controlled action completed",
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
        NodalOsCoreEventKind.WarningRaised => value.SummaryRedacted.Contains("fallback", StringComparison.OrdinalIgnoreCase) ? "fallback" : "attention",
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
