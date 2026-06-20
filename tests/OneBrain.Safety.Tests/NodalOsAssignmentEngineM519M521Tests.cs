using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.AgentOperations.Contracts;
using OneBrain.AgentOperations.Core;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("AssignmentEngine")]
[TestCategory("PromptGovernance")]
[TestCategory("ByokProvider")]
[TestCategory("ProjectUnderstandingPolicy")]
[TestCategory("ContextIntakePreview")]
[TestCategory("UserContext")]
[TestCategory("WorkspaceReadinessContext")]
[TestCategory("WorkspaceMetadataHealth")]
[TestCategory("WorkspaceStorageMissionSwitcher")]
[TestCategory("WorkspaceLocalModel")]
[TestCategory("MissionControlVisualPolish")]
[TestCategory("MissionControlGuidance")]
[TestCategory("MissionControlInteractionNoOp")]
[TestCategory("MissionControlShellReadOnly")]
[TestCategory("AuditAPreUiBoundaryNaming")]
[TestCategory("ApprovalUxHandoffObservability")]
[TestCategory("ApprovalTimelineEvidence")]
[TestCategory("CoreRuntimeRegistryEventBusRedaction")]
[TestCategory("NewTopicsIntake")]
[TestCategory("NamingCleanup")]
public sealed class NodalOsAssignmentEngineM519M521Tests
{
    private static readonly string[] ForbiddenNames = ["Nexa", "NEXA", "NODRIX", "HOTEP"];
    private static readonly string[] SensitiveMarkers = ["Bearer ", "Authorization:", "Cookie:", "password", "secret", "api_key", "access_token", "refresh_token", "id_token", "private key", "sk-", "connection string"];
    private readonly NodalOsAssignmentEngineDraftService service = new();
    private readonly NodalOsAssignmentEngineJsonSerializer serializer = new();

    [TestMethod]
    public void AssignmentEngine_CreateAllRequiredStatesAsDraftOnlyNoAuthority()
    {
        foreach (var state in Enum.GetValues<NodalOsAssignmentState>())
        {
            var request = service.CreateAssignmentRequest(state);

            Assert.AreEqual(state, request.AssignmentState);
            Assert.IsTrue(request.DraftOnly);
            Assert.IsFalse(request.CallsLlmProvider);
            Assert.IsFalse(request.CreatesPrompt);
            Assert.IsFalse(request.CallsModelPlanner);
            Assert.IsFalse(request.ExecutesTasks);
            Assert.IsFalse(request.CreatesAuthoritativeTasks);
            Assert.IsFalse(request.MutatesWorkspace);
            Assert.IsFalse(request.MutatesRuntimeRegistry);
            Assert.IsFalse(request.CanAuthorizeExecution);
            AssertSafeOutput(serializer.SerializeAssignmentRequest(request));
        }
    }

    [TestMethod]
    public void AssignmentEngine_DisclosesNoModelNoPromptNoExecutableTaskGraph()
    {
        var request = NodalOsAssignmentEngineFixtures.AssignmentRequest();

        CollectionAssert.Contains(request.DisclosuresRedacted.ToList(), "Assignment Engine runtime is not implemented.");
        CollectionAssert.Contains(request.DisclosuresRedacted.ToList(), "No model was called.");
        CollectionAssert.Contains(request.DisclosuresRedacted.ToList(), "No prompt was generated.");
        CollectionAssert.Contains(request.DisclosuresRedacted.ToList(), "No task is executable.");
        CollectionAssert.Contains(request.DisclosuresRedacted.ToList(), "TaskGraph is draft-only.");
        Assert.IsTrue(request.UserProvidedContextRefsRedacted.Count > 0);
        Assert.IsTrue(request.SafeContextBoundaryRefs.Count > 0);
        Assert.IsTrue(request.PromptGovernanceRefs.Count > 0);
        Assert.IsTrue(request.BudgetPolicyRefs.Count > 0);
        Assert.IsTrue(request.ModelCapabilityProfileRefs.Count > 0);
    }

    [TestMethod]
    public void TaskGraphDraft_CreatesRequiredTaskKindsAndAllTasksAreNonExecutable()
    {
        var graph = NodalOsAssignmentEngineFixtures.TaskGraphDraft();
        var kinds = graph.Tasks.Select(task => task.TaskKind).ToList();

        Assert.AreEqual(NodalOsAssignmentTaskGraphStatus.DraftOnly, graph.GraphStatus);
        CollectionAssert.Contains(kinds, NodalOsAssignmentTaskKind.AnalysisDraft);
        CollectionAssert.Contains(kinds, NodalOsAssignmentTaskKind.DocumentationDraft);
        CollectionAssert.Contains(kinds, NodalOsAssignmentTaskKind.PlanningDraft);
        CollectionAssert.Contains(kinds, NodalOsAssignmentTaskKind.RiskAssessmentDraft);
        CollectionAssert.Contains(kinds, NodalOsAssignmentTaskKind.HandoffDraft);
        CollectionAssert.Contains(kinds, NodalOsAssignmentTaskKind.AdvisorSuggestionDraft);
        CollectionAssert.Contains(kinds, NodalOsAssignmentTaskKind.FutureExecutionPlaceholder);
        Assert.IsTrue(graph.Tasks.All(task => !task.CanExecute));
        Assert.AreEqual(
            NodalOsAssignmentTaskStatus.Blocked,
            graph.Tasks.Single(task => task.TaskKind == NodalOsAssignmentTaskKind.FutureExecutionPlaceholder).Status);
        Assert.IsFalse(graph.Executable);
        Assert.IsFalse(graph.ResolvesDependenciesProductively);
        Assert.IsFalse(graph.CallsLlmProvider);
        Assert.IsFalse(graph.CallsRuntime);
        Assert.IsFalse(graph.TouchesFilesystem);
        Assert.IsFalse(graph.CreatesAuthoritativeApproval);
        Assert.IsFalse(graph.CanAuthorizeExecution);
        AssertSafeOutput(serializer.SerializeTaskGraph(graph));
    }

    [TestMethod]
    public void TaskGraphDraft_RepresentsDependenciesApprovalFutureLlmAndRuntimeFlagsWithoutAuthority()
    {
        var graph = NodalOsAssignmentEngineFixtures.TaskGraphDraft();

        Assert.IsTrue(graph.DependenciesRedacted.Count > 0);
        Assert.IsTrue(graph.ApprovalRefs.Count > 0);
        Assert.IsTrue(graph.ContextRefsRedacted.Count > 0);
        Assert.IsTrue(graph.GuardrailRefs.Count > 0);
        Assert.IsTrue(graph.Tasks.Any(task => task.DependencyIds.Count > 0));
        Assert.IsTrue(graph.Tasks.Any(task => task.RequiresApproval));
        Assert.IsTrue(graph.Tasks.Any(task => task.RequiresLlmFuture));
        Assert.IsTrue(graph.Tasks.Any(task => task.RequiresRuntimeFuture));
        Assert.IsFalse(graph.Tasks.Any(task => task.CanExecute));
    }

    [TestMethod]
    public void PlannerReadiness_CreateAllRequiredStatesAndAlwaysBlocksRuntimeExecution()
    {
        foreach (var state in Enum.GetValues<NodalOsPlannerReadinessState>())
        {
            var readiness = service.CreatePlannerReadinessGate(state);

            Assert.AreEqual(state, readiness.ReadinessState);
            Assert.IsFalse(readiness.CallsLlmProvider);
            Assert.IsFalse(readiness.CreatesPrompt);
            Assert.IsFalse(readiness.CreatesAuthoritativePlan);
            Assert.IsFalse(readiness.ExecutesRuntime);
            Assert.IsFalse(readiness.PerformsWorkDispatch);
            Assert.IsTrue(readiness.FutureRuntimeExecutionBlocked);
            Assert.IsFalse(readiness.CanAuthorizeExecution);
            CollectionAssert.Contains(readiness.DisabledPlanningModes.ToList(), NodalOsPlanningMode.FutureRuntimeExecution);
            AssertSafeOutput(serializer.SerializePlannerReadiness(readiness));
        }
    }

    [TestMethod]
    public void PlannerReadiness_FutureLlmPlanningRemainsNoCallNoPrompt()
    {
        var readiness = service.CreatePlannerReadinessGate(NodalOsPlannerReadinessState.ReadyForFutureLlmPlanningWithConsent);

        CollectionAssert.Contains(readiness.AllowedNextPlanningModes.ToList(), NodalOsPlanningMode.FutureLlmDraftWithConsent);
        CollectionAssert.Contains(readiness.DisabledPlanningModes.ToList(), NodalOsPlanningMode.FutureRuntimeExecution);
        Assert.IsFalse(readiness.CallsLlmProvider);
        Assert.IsFalse(readiness.CreatesPrompt);
        Assert.IsFalse(readiness.CreatesAuthoritativePlan);
        Assert.IsFalse(readiness.ExecutesRuntime);
        Assert.IsTrue(readiness.EvidenceRefs.Count > 0);
        Assert.IsTrue(readiness.TimelineRefs.Count > 0);
        Assert.IsTrue(readiness.GuardrailRefs.Count > 0);
    }

    [TestMethod]
    public void AssignmentAndTaskGraph_SanitizeAdversarialInputs()
    {
        var unsafeValues = new[]
        {
            "Bearer abcdefghijklmnop",
            "Authorization: value",
            "Cookie: session=value",
            "password=value",
            "secret=value",
            "api_key=value",
            "access_token=value",
            "refresh_token=value",
            "id_token=value",
            "private key",
            "sk-test-value",
            "connection string"
        };

        foreach (var unsafeValue in unsafeValues)
        {
            var request = service.CreateAssignmentRequest(userContextRef: unsafeValue, workspaceId: unsafeValue, missionId: unsafeValue);
            var graph = service.CreateTaskGraphDraft(request);

            AssertSafeOutput(serializer.SerializeAssignmentRequest(request));
            AssertSafeOutput(serializer.SerializeTaskGraph(graph));
        }
    }

    [TestMethod]
    public void Boundary_NewAssignmentEngineFiles_DoNotReferenceForbiddenRuntimeOrPlannerPrimitives()
    {
        var source = NewSource();

        AssertDoesNotContain(source, "OneBrain.BrowserExecutor.Cdp");
        AssertDoesNotContain(source, "HttpClient");
        AssertDoesNotContain(source, "ClientWebSocket");
        AssertDoesNotContain(source, "Process.Start");
        AssertDoesNotContain(source, "System.Diagnostics.Process");
        AssertDoesNotContain(source, "BackgroundService");
        AssertDoesNotContain(source, "Task.Run");
        AssertDoesNotContain(source, "IHostedService");
        AssertDoesNotContain(source, "QueueClient");
        AssertDoesNotContain(source, "RecorderRuntime");
        AssertDoesNotContain(source, "ReplayRuntime");
        AssertDoesNotContain(source, "DslParserRuntime");
        AssertDoesNotContain(source, "ProviderClient");
        AssertDoesNotContain(source, "ProviderSdkClient");
        AssertDoesNotContain(source, "CloudSync");
        AssertDoesNotContain(source, "Environment.GetEnvironmentVariable");
        AssertDoesNotContain(source, "FinalPromptTextBuilder");
        AssertDoesNotContain(source, "TokenCounter");
        AssertDoesNotContain(source, "PricingLookup");
        AssertDoesNotContain(source, "ModelAvailabilityLookup");
        AssertDoesNotContain(source, "RoutingEngine");
        AssertDoesNotContain(source, "RealPlanner");
        AssertDoesNotContain(source, "ExecutableTaskGraph");
        AssertDoesNotContain(source, "DependencyScheduler");
        AssertDoesNotContain(source, "File.Write");
        AssertDoesNotContain(source, "File.Read");
        AssertDoesNotContain(source, "Directory.");
        AssertDoesNotContain(source, "git ");
        AssertDoesNotContain(source, "embeddings client");
    }

    [TestMethod]
    public void ExistingSafetyContinuity_PromptByokAndMissionControlRemainNoAuthority()
    {
        var prompt = NodalOsPromptGovernanceFixtures.PromptPreviewPolicy();
        var byok = NodalOsByokProviderFixtures.ReferenceOnlySettings();
        var shell = NodalOsMissionControlShellFixtures.ShellPreview();

        Assert.IsFalse(prompt.GeneratesFinalPromptText);
        Assert.IsFalse(prompt.CallsLlmProvider);
        Assert.IsFalse(prompt.CanAuthorizeExecution);
        Assert.IsFalse(byok.CallsProvider);
        Assert.IsFalse(byok.SendsNetworkRequest);
        Assert.IsFalse(byok.CanAuthorizeExecution);
        Assert.IsTrue(shell.ReadOnlyUi);
        Assert.IsFalse(shell.RuntimeExecutionAllowed);
    }

    [TestMethod]
    public void ArtifactMarksAssignmentEngineTaskGraphDraftReady()
    {
        var artifact = System.IO.File.ReadAllText(PathFor("artifacts", "agent-operations", "m521", "assignment-engine-taskgraph-summary.json"));

        AssertContains(artifact, "\"assignmentEngineContracts\": true");
        AssertContains(artifact, "\"taskGraphDraft\": true");
        AssertContains(artifact, "\"plannerReadinessGate\": true");
        AssertContains(artifact, "\"assignmentEngineRuntimeIntroduced\": false");
        AssertContains(artifact, "\"taskGraphExecutable\": false");
        AssertContains(artifact, "\"plannerRealIntroduced\": false");
        AssertContains(artifact, "\"promptCreationIntroduced\": false");
        AssertContains(artifact, "\"llmProviderCallsIntroduced\": false");
    }

    private static void AssertSafeOutput(string text)
    {
        foreach (var marker in SensitiveMarkers)
            AssertDoesNotContain(text, marker);
        foreach (var name in ForbiddenNames)
            AssertDoesNotContain(text, name);
    }

    private static string NewSource() =>
        System.IO.File.ReadAllText(PathFor("src", "OneBrain.AgentOperations.Contracts", "NodalOsAssignmentEngineContracts.cs")) +
        System.IO.File.ReadAllText(PathFor("src", "OneBrain.AgentOperations.Core", "NodalOsAssignmentEngineServices.cs"));

    private static string PathFor(params string[] parts) =>
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", Path.Combine(parts));

    private static void AssertContains(string text, string expected) =>
        StringAssert.Contains(text, expected);

    private static void AssertDoesNotContain(string text, string unexpected) =>
        Assert.IsFalse(text.Contains(unexpected, StringComparison.Ordinal), $"Unexpected marker found: {unexpected}");
}
