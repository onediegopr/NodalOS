using OneBrain.AgentOperations.Contracts;
using OneBrain.AgentOperations.Core;
using OneBrain.AgentOperations.Core.Capabilities;
using OneBrain.Core.Models;

namespace OneBrain.AgentOperations.Core.Runtime;

public sealed record NodalOsSelectiveRuntimeFixtureResult(
    MissionPlan Plan,
    MissionRuntimeState Mission,
    MissionResumeCard ResumeCard,
    ModelRoutingResult ModelRouting,
    CapabilitySelection ModelCapability,
    IReadOnlyList<NodalOsCoreTimelineProjection> Timeline,
    RuntimeInspectorSnapshot Inspector,
    bool ApprovalRequested,
    bool ExternalIoUsed,
    bool NetworkUsed);

public sealed class NodalOsSelectiveRuntimeFixtureScenario
{
    public async ValueTask<NodalOsSelectiveRuntimeFixtureResult> RunAsync(
        CancellationToken cancellationToken = default)
    {
        var taskGraph = CreateTaskGraph();
        var plan = NodalOsMissionPlanProjector.Project(
            taskGraph,
            "Analyze one test-owned fixture and produce verified evidence.");
        var catalog = CreateCatalog();
        var capabilities = CreateCapabilities(catalog);
        var modelCapability = capabilities.Select(new CapabilitySelectionRequest(
            "model.chat",
            ["fixture-primary", "fixture-fallback"],
            [CapabilityRuntime.Model]));
        if (!modelCapability.Available)
            throw new InvalidOperationException(modelCapability.SafeMessage);

        var runtime = new LightweightMissionRuntime(
            plan,
            new MissionAuthorizationScope(
                plan.MissionId,
                new HashSet<string>(["filesystem.read", "model.chat"], StringComparer.Ordinal),
                new HashSet<MissionExecutionSurface>([MissionExecutionSurface.Filesystem]),
                MissionRiskLevel.Medium),
            "fixture-run-1");
        runtime.Start("fixture-start");
        var stepId = plan.Steps.Single().Id;
        runtime.BeginStep(stepId, "fixture-step");
        runtime.RecordToolCallStarted(stepId, "filesystem.read", "fixture-read-start");
        runtime.RecordToolCallCompleted(
            stepId,
            "filesystem.read",
            "fixture-read-complete",
            ["evidence:fixture-snapshot"]);
        runtime.RecordToolCallStarted(stepId, "model.chat", "fixture-model-start");

        using var secrets = new EphemeralSecretReferenceStore();
        var modelRouter = new PolicyAwareModelRouter(
            catalog,
            secrets,
            new FixtureFallbackModelAttemptExecutor());
        var modelRouting = await modelRouter.ExecuteAsync(
            new ModelRouteRequest(
                LogicalModel: "fixture-default",
                RequiredCapabilities: ModelCapabilities.Chat,
                RequiredContextWindow: 1,
                LocalOnly: true,
                CloudAllowed: false,
                MaximumPrivacyClass: ModelPrivacyClass.LocalOnly,
                MaximumInputCostPerMillion: 1,
                MaximumOutputCostPerMillion: 1,
                RemainingBudget: 1,
                AllowedProviderIds: ["fixture-primary", "fixture-fallback"],
                PreferSpeed: false,
                PreferQuality: true),
            new ModelExecutionRequest(
                CorrelationId: "fixture-model-route",
                MissionId: plan.MissionId,
                StepId: stepId,
                Payload: "test-owned fixture summary"),
            cancellationToken: cancellationToken).ConfigureAwait(false);

        if (!modelRouting.Success)
        {
            runtime.FailStep(stepId, modelRouting.SafeMessage, "fixture-model-failed");
            throw new InvalidOperationException(modelRouting.SafeMessage);
        }

        if (modelRouting.Attempts.Count > 1)
        {
            runtime.RecordFallback(
                stepId,
                "Primary fixture model was rate-limited; the authorized fallback completed the request.",
                "fixture-model-fallback");
        }

        runtime.RecordToolCallCompleted(
            stepId,
            "model.chat",
            "fixture-model-complete",
            ["evidence:fixture-model-response"]);
        runtime.MarkReadyForVerification(
            stepId,
            "fixture-ready",
            ["evidence:fixture-snapshot", "evidence:fixture-model-response"]);
        runtime.VerifyStep(
            stepId,
            passed: true,
            "fixture-verified",
            ["evidence:fixture-verification"]);

        var eventBus = new NodalOsCoreEventBus();
        var timeline = new NodalOsMissionEventProjectionService().Project(runtime.Events, eventBus);
        var fallbackMessages = runtime.Events
            .Where(value => value.Kind == MissionEventKind.FallbackApplied)
            .Select(value => value.Summary)
            .ToArray();
        var inspector = RuntimeInspectorProjector.TryBuild(new RuntimeInspectorInput(
            DeveloperModeEnabled: true,
            Plan: plan,
            Mission: runtime.State,
            ResumeCard: runtime.ResumeCard,
            Capabilities: capabilities.List(),
            Models: catalog.Snapshot(),
            LogicalModel: "fixture-default",
            ActiveProvider: modelRouting.SelectedCandidate?.Provider.ProviderId,
            ActiveModel: modelRouting.SelectedCandidate?.Model.ModelId,
            RecentFallbacks: fallbackMessages,
            Browser: new RuntimeInspectorBrowserStatus(
                Runtime: "CloakBrowser",
                State: "BLOCKED_EXTERNAL_CLOAKBROWSER_BINARY",
                ReconnectCount: 0,
                ActionQueueDepth: 0,
                LastError: "Pinned runtime binary is not provisioned in this environment.",
                LastLatency: null),
            ApprovalWait: null,
            RecentErrors: [],
            ApproximateMemoryBytes: 0))
            ?? throw new InvalidOperationException("Fixture runtime inspector was not created.");

        return new NodalOsSelectiveRuntimeFixtureResult(
            Plan: plan,
            Mission: runtime.State,
            ResumeCard: runtime.ResumeCard,
            ModelRouting: modelRouting,
            ModelCapability: modelCapability,
            Timeline: timeline,
            Inspector: inspector,
            ApprovalRequested: runtime.Events.Any(value => value.Kind == MissionEventKind.ApprovalRequired),
            ExternalIoUsed: false,
            NetworkUsed: false);
    }

    public static ModelCatalog CreateCatalog() =>
        new(
            providers:
            [
                new ModelProviderDefinition(
                    "fixture-primary",
                    "Fixture Primary",
                    ModelProviderKind.Local,
                    new Uri("http://127.0.0.1/primary"),
                    false,
                    [],
                    ModelProviderState.Ready,
                    100,
                    ModelPrivacyClass.LocalOnly,
                    ["local"]),
                new ModelProviderDefinition(
                    "fixture-fallback",
                    "Fixture Fallback",
                    ModelProviderKind.Local,
                    new Uri("http://127.0.0.1/fallback"),
                    false,
                    [],
                    ModelProviderState.Ready,
                    90,
                    ModelPrivacyClass.LocalOnly,
                    ["local"])
            ],
            models:
            [
                new ModelDefinition(
                    "fixture-primary-chat",
                    "fixture-primary",
                    "fixture-primary-chat",
                    4096,
                    ModelCapabilities.Chat | ModelCapabilities.StructuredOutput,
                    ModelPrivacyClass.LocalOnly,
                    0,
                    0,
                    90,
                    95,
                    true),
                new ModelDefinition(
                    "fixture-fallback-chat",
                    "fixture-fallback",
                    "fixture-fallback-chat",
                    4096,
                    ModelCapabilities.Chat | ModelCapabilities.StructuredOutput,
                    ModelPrivacyClass.LocalOnly,
                    0,
                    0,
                    80,
                    90,
                    true)
            ],
            aliases:
            [
                new LogicalModelAlias(
                    "fixture-default",
                    ModelCapabilities.Chat,
                    true,
                    0,
                    0)
            ]);

    private static CapabilityRegistry CreateCapabilities(ModelCatalog catalog)
    {
        var registry = new CapabilityRegistry();
        registry.Register(new CapabilityRecord(
            "filesystem.read",
            "core",
            null,
            CapabilityRuntime.Filesystem,
            CapabilityState.Ready,
            100,
            "1",
            new Dictionary<string, string> { ["scope"] = "fixture-only" }));
        foreach (var provider in catalog.Snapshot().Providers)
        {
            registry.Register(new CapabilityRecord(
                "model.chat",
                provider.ProviderId,
                null,
                CapabilityRuntime.Model,
                provider.State == ModelProviderState.Ready ? CapabilityState.Ready : CapabilityState.Degraded,
                provider.HealthScore,
                "1",
                new Dictionary<string, string> { ["privacy"] = provider.PrivacyClass.ToString() }));
        }
        registry.Register(new CapabilityRecord(
            "browser.dom.read",
            "cloakbrowser",
            null,
            CapabilityRuntime.Browser,
            CapabilityState.Unavailable,
            0,
            "146.0.7680.177.5",
            new Dictionary<string, string> { ["blocker"] = "external-binary" }));
        return registry;
    }

    private static NodalOsTaskGraphDraft CreateTaskGraph()
    {
        var createdAt = DateTimeOffset.UtcNow;
        return new NodalOsTaskGraphDraft
        {
            TaskGraphId = "fixture-task-graph",
            AssignmentRequestId = "fixture-assignment",
            WorkspaceId = "fixture-workspace",
            MissionId = "fixture-mission",
            GraphStatus = NodalOsAssignmentTaskGraphStatus.ReadyForManualReview,
            Tasks =
            [
                new NodalOsAssignmentTaskDraft
                {
                    TaskId = "fixture-analyze",
                    TitleRedacted = "Analyze test-owned fixture",
                    SummaryRedacted = "Read a local fixture and route a simulated model response.",
                    TaskKind = NodalOsAssignmentTaskKind.AnalysisDraft,
                    Status = NodalOsAssignmentTaskStatus.ReadyForManualReview,
                    DependencyIds = [],
                    BlockedByIds = [],
                    RiskLevel = NodalOsAssignmentRiskLevel.Low,
                    AllowedCapabilitiesRedacted = ["filesystem.read", "model.chat"],
                    DisabledCapabilitiesRedacted = ["filesystem.write.safe", "browser.action.execute", "terminal.execute"],
                    SuggestedAssigneeType = NodalOsSuggestedAssigneeType.FutureAssignmentPlanner,
                    EvidenceRefs = [Evidence("evidence:fixture-plan")],
                    TimelineRefs = ["timeline:fixture-plan"],
                    RequiresApproval = false,
                    RequiresLlmFuture = false,
                    RequiresRuntimeFuture = false,
                    RequiresFilesystemFuture = false,
                    CanExecute = false
                }
            ],
            DependenciesRedacted = [],
            RiskNotesRedacted = ["Fixture-only, no external IO and no mutation."],
            EvidenceRefs = [Evidence("evidence:fixture-task-graph")],
            TimelineRefs = ["timeline:fixture-task-graph"],
            ApprovalRefs = [],
            ContextRefsRedacted = ["fixture-context"],
            GuardrailRefs = ["no-network", "no-mutation", "verification-before-completion"],
            HumanReviewRequirementRedacted = "Mission scope authorization covers this low-risk fixture.",
            ReadinessGateResultRedacted = "Fixture-safe runtime integration is eligible.",
            DraftOnly = true,
            Executable = false,
            ResolvesDependenciesProductively = false,
            CallsLlmProvider = false,
            CallsRuntime = false,
            TouchesFilesystem = false,
            CreatesAuthoritativeApproval = false,
            CanAuthorizeExecution = false,
            CreatedAt = createdAt
        };
    }

    private static NodalOsEvidenceBridgeRef Evidence(string id) =>
        new()
        {
            EvidenceId = id,
            Kind = "fixture-ref",
            SourceKind = NodalOsEvidenceBridgeSourceKind.Manual,
            UseKind = NodalOsEvidenceBridgeUseKind.AuditTrail,
            Authority = NodalOsEvidenceBridgeAuthority.NoAuthority,
            Sensitivity = NodalOsEvidenceSensitivity.NonSensitive,
            RedactionState = NodalOsEvidenceRedactionState.NotRequired,
            LedgerRef = null,
            Provenance = "Selective runtime local fixture.",
            CreatedAt = DateTimeOffset.UtcNow
        };

    private sealed class FixtureFallbackModelAttemptExecutor : IModelAttemptExecutor
    {
        public ValueTask<ModelAttemptResult> ExecuteAsync(
            ModelAttemptContext context,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromResult(
                string.Equals(context.Candidate.Provider.ProviderId, "fixture-primary", StringComparison.Ordinal)
                    ? ModelAttemptResult.FromHttpStatus(429, "Fixture primary provider was rate-limited.")
                    : ModelAttemptResult.Succeeded(
                        response: new { summary = "Fixture analyzed" },
                        inputTokens: 12,
                        outputTokens: 8,
                        estimatedCost: 0,
                        safeMessage: "Fixture fallback provider completed the request."));
        }
    }
}
