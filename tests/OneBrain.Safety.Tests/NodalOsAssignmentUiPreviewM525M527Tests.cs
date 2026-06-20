using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.AgentOperations.Contracts;
using OneBrain.AgentOperations.Core;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("AssignmentUi")]
[TestCategory("TaskGraphInteraction")]
[TestCategory("PlannerUxAcceptance")]
[TestCategory("MissionPlanPreview")]
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
public sealed class NodalOsAssignmentUiPreviewM525M527Tests
{
    private static readonly string[] ForbiddenNames = ["Nexa", "NEXA", "NODRIX", "HOTEP"];
    private static readonly string[] SensitiveMarkers =
    [
        "Bear" + "er ",
        "Authorization:",
        "Cook" + "ie:",
        "password",
        "raw " + "secret",
        "api" + "_key",
        "access" + "_token",
        "refresh" + "_token",
        "private key",
        "s" + "k-",
        "connection string"
    ];

    private readonly NodalOsAssignmentUiPreviewService previewService = new();
    private readonly NodalOsAssignmentUiPreviewJsonSerializer previewSerializer = new();
    private readonly NodalOsTaskGraphInteractionNoOpService interactionService = new();
    private readonly NodalOsTaskGraphInteractionNoOpJsonSerializer interactionSerializer = new();
    private readonly NodalOsPlannerUxAcceptanceService acceptanceService = new();
    private readonly NodalOsPlannerUxAcceptanceJsonSerializer acceptanceSerializer = new();

    [TestMethod]
    public void AssignmentUiPreview_DeclaresDraftOnlyNonAuthoritativeAndEveryWorkItemNonExecutable()
    {
        var preview = NodalOsAssignmentUiPreviewFixtures.Preview();

        Assert.IsTrue(preview.DraftOnly);
        Assert.IsTrue(preview.ReadOnly);
        Assert.IsFalse(preview.IsAuthoritative);
        Assert.IsFalse(preview.CanAuthorizeExecution);
        Assert.IsFalse(preview.RuntimeExecutionAllowed);
        Assert.IsFalse(preview.PlannerExecutionAllowed);
        Assert.IsFalse(preview.LlmCallAllowed);
        Assert.IsFalse(preview.FilesystemAccessAllowed);
        Assert.IsFalse(preview.NetworkAccessAllowed);
        Assert.IsTrue(preview.WorkItems.Count > 0);
        Assert.IsTrue(preview.WorkItems.All(workItem => !workItem.CanExecute));
        Assert.IsTrue(preview.WorkItems.All(workItem => !workItem.IsAuthoritative));
        AssertSafeOutput(previewSerializer.SerializePreview(preview));
    }

    [TestMethod]
    public void AssignmentUiPreview_IncludesPlannerRuntimeLlmAndFilesystemBlockedDisclosures()
    {
        var preview = NodalOsAssignmentUiPreviewFixtures.Preview();

        StringAssert.Contains(preview.Header.DraftOnlyDisclosureRedacted, "draft-only");
        StringAssert.Contains(preview.Header.RuntimeBlockedDisclosureRedacted, "blocked");
        StringAssert.Contains(preview.Header.LlmBlockedDisclosureRedacted, "blocked");
        StringAssert.Contains(preview.Header.FilesystemBlockedDisclosureRedacted, "blocked");
        StringAssert.Contains(preview.ExplanationPanel.CannotExecuteExplanationRedacted, "CanExecute=false");
        StringAssert.Contains(preview.ExplanationPanel.ApprovalDoesNotUnlockRuntimeRedacted, "does not unlock runtime");
        StringAssert.Contains(preview.ExplanationPanel.PlannerNotImplementedRedacted, "not implemented");
        Assert.IsTrue(preview.ReviewPanel.InputRefsRedacted.Count > 0);
        Assert.IsTrue(preview.ReviewPanel.OutputRefsRedacted.Count > 0);
        Assert.IsTrue(preview.ReviewPanel.GuardrailRefs.Count > 0);
        Assert.IsTrue(preview.ReviewPanel.MissingReadinessRedacted.Count > 0);
        Assert.IsTrue(preview.ReviewPanel.UserReviewOptionsAreNoOp);
    }

    [TestMethod]
    public void AssignmentUiPreview_RenderedHtmlIsStaticLocalAndContainsNoRuntimeScript()
    {
        var preview = NodalOsAssignmentUiPreviewFixtures.Preview();
        var render = previewService.Render(preview);

        Assert.IsTrue(render.StaticOnly);
        Assert.IsFalse(render.ContainsScript);
        Assert.IsFalse(render.ContainsExternalResource);
        Assert.IsFalse(render.CallsNetwork);
        StringAssert.Contains(render.HtmlRedacted, "CanExecute=false");
        StringAssert.Contains(render.HtmlRedacted, "IsAuthoritative=false");
        AssertDoesNotContain(render.HtmlRedacted, "<script");
        AssertDoesNotContain(render.HtmlRedacted, "https://");
        AssertDoesNotContain(render.HtmlRedacted, "http://");
        AssertDoesNotContain(render.HtmlRedacted, "cdn");
        AssertSafeOutput(previewSerializer.SerializeRender(render));
    }

    [TestMethod]
    public void TaskGraphInteraction_SelectExpandFilterAndSortAreNoOp()
    {
        var kinds = new[]
        {
            NodalOsTaskGraphInteractionKind.SelectWorkItem,
            NodalOsTaskGraphInteractionKind.ExpandWorkItem,
            NodalOsTaskGraphInteractionKind.CollapseWorkItem,
            NodalOsTaskGraphInteractionKind.FilterByStatus,
            NodalOsTaskGraphInteractionKind.FilterByRisk,
            NodalOsTaskGraphInteractionKind.FilterByBlocker,
            NodalOsTaskGraphInteractionKind.SortVisualOrder
        };

        foreach (var kind in kinds)
        {
            var result = Apply(kind);

            Assert.IsTrue(result.IsNoOp);
            Assert.IsFalse(result.MutatesState);
            Assert.IsFalse(result.CanAuthorizeExecution);
            Assert.IsFalse(result.RuntimeExecutionAllowed);
            Assert.IsFalse(result.PlannerExecutionAllowed);
            Assert.IsFalse(result.LlmCallAllowed);
            Assert.IsFalse(result.FilesystemAccessAllowed);
            Assert.IsFalse(result.NetworkAccessAllowed);
            AssertSafeOutput(interactionSerializer.SerializeResult(result));
        }
    }

    [TestMethod]
    public void TaskGraphInteraction_DraftNoteAndReviseDraftDoNotMutateOrCreatePlannerExecution()
    {
        var noteRequest = interactionService.CreateRequest(
            NodalOsTaskGraphInteractionKind.AddDraftNote,
            draftNote: "review note only");
        var reviseRequest = interactionService.CreateRequest(NodalOsTaskGraphInteractionKind.AskToReviseDraft);

        foreach (var result in new[] { interactionService.Apply(noteRequest), interactionService.Apply(reviseRequest) })
        {
            Assert.IsTrue(result.IsNoOp);
            Assert.IsFalse(result.MutatesState);
            Assert.IsFalse(result.CreatesExecutionRequest);
            Assert.IsFalse(result.CreatesPrompt);
            Assert.IsFalse(result.PlannerExecutionAllowed);
            Assert.IsFalse(result.UsesClipboard);
            Assert.IsFalse(result.CanAuthorizeExecution);
        }
    }

    [TestMethod]
    public void TaskGraphInteraction_AllKindsCannotCallProviderNetworkFilesystemOrRuntime()
    {
        foreach (var kind in Enum.GetValues<NodalOsTaskGraphInteractionKind>())
        {
            var result = Apply(kind);

            Assert.AreEqual(kind, result.InteractionKind);
            Assert.IsTrue(result.IsNoOp);
            Assert.IsFalse(result.RuntimeExecutionAllowed);
            Assert.IsFalse(result.LlmCallAllowed);
            Assert.IsFalse(result.NetworkAccessAllowed);
            Assert.IsFalse(result.FilesystemAccessAllowed);
            Assert.IsFalse(result.CreatesPrompt);
            Assert.IsFalse(result.CreatesExecutionRequest);
            Assert.IsTrue(result.GuardrailRefs.Count > 0);
        }
    }

    [TestMethod]
    public void EvidenceTimelineAndContextLinksRemainRefOnlyNoRawPayload()
    {
        var preview = NodalOsAssignmentUiPreviewFixtures.Preview();
        var evidenceResult = Apply(NodalOsTaskGraphInteractionKind.ShowEvidenceRefs);
        var timelineResult = Apply(NodalOsTaskGraphInteractionKind.ShowTimelineRefs);

        Assert.IsTrue(preview.EvidenceRefs.Count > 0);
        Assert.IsTrue(preview.TimelineRefs.Count > 0);
        Assert.IsTrue(preview.ContextRefsRedacted.Count > 0);
        Assert.IsTrue(evidenceResult.EvidenceRefs.Count > 0);
        Assert.IsTrue(timelineResult.TimelineRefs.Count > 0);
        AssertSafeOutput(previewSerializer.SerializePreview(preview));
        AssertSafeOutput(interactionSerializer.SerializeResult(evidenceResult));
        AssertSafeOutput(interactionSerializer.SerializeResult(timelineResult));
    }

    [TestMethod]
    public void PlannerUxAcceptancePack_CoversRequiredStatesAndExplainsApprovalDoesNotUnlockRuntime()
    {
        var pack = acceptanceService.CreateAcceptancePack();
        var states = pack.CoveredStates.ToList();

        CollectionAssert.Contains(states, NodalOsPlannerUxAcceptanceState.EmptyAssignment);
        CollectionAssert.Contains(states, NodalOsPlannerUxAcceptanceState.DraftAvailable);
        CollectionAssert.Contains(states, NodalOsPlannerUxAcceptanceState.BlockedByPlannerReadiness);
        CollectionAssert.Contains(states, NodalOsPlannerUxAcceptanceState.BlockedByRuntimeDisabled);
        CollectionAssert.Contains(states, NodalOsPlannerUxAcceptanceState.BlockedByLlmDisabled);
        CollectionAssert.Contains(states, NodalOsPlannerUxAcceptanceState.BlockedByFilesystemDisabled);
        CollectionAssert.Contains(states, NodalOsPlannerUxAcceptanceState.EvidenceRefsMissing);
        CollectionAssert.Contains(states, NodalOsPlannerUxAcceptanceState.ContextNeedsReview);
        CollectionAssert.Contains(states, NodalOsPlannerUxAcceptanceState.WorkItemDependencyBlocked);
        CollectionAssert.Contains(states, NodalOsPlannerUxAcceptanceState.AllWorkItemsDraftOnly);
        Assert.IsTrue(pack.Criteria.Count >= 8);
        Assert.IsTrue(pack.Criteria.All(criterion => criterion.PassedByContract));
        Assert.IsTrue(pack.UserFacingExplanationsRedacted.Any(text => text.Contains("does not unlock runtime", StringComparison.Ordinal)));
        Assert.IsFalse(pack.CanAuthorizeRuntime);
        Assert.IsFalse(pack.CanTriggerLlmProvider);
        Assert.IsFalse(pack.CanAccessFilesystem);
        Assert.IsFalse(pack.CanCallNetwork);
        Assert.IsFalse(pack.ApprovalUnlocksRuntime);
        AssertSafeOutput(acceptanceSerializer.Serialize(pack));
    }

    [TestMethod]
    public void Serialization_RedactsAdversarialInputsAndStaysSafe()
    {
        var unsafeValues = new[]
        {
            "Bear" + "er abcdefghijklmnop",
            "Authorization: value",
            "Cook" + "ie: session=value",
            "password=value",
            "raw " + "secret=value",
            "api" + "_key=value",
            "access" + "_token=value",
            "refresh" + "_token=value",
            "private key",
            "s" + "k-test-value",
            "connection string"
        };

        foreach (var unsafeValue in unsafeValues)
        {
            var request = interactionService.CreateRequest(
                NodalOsTaskGraphInteractionKind.AddDraftNote,
                assignmentUiPreviewId: unsafeValue,
                workItemId: unsafeValue,
                draftNote: unsafeValue);
            var result = interactionService.Apply(request);

            AssertSafeOutput(interactionSerializer.SerializeRequest(request));
            AssertSafeOutput(interactionSerializer.SerializeResult(result));
        }
    }

    [TestMethod]
    public void Boundary_NewAssignmentUiFiles_DoNotReferenceForbiddenRuntimeOrUiPrimitives()
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
        AssertDoesNotContain(source, "SchedulerRuntime");
        AssertDoesNotContain(source, "WorkerRuntime");
        AssertDoesNotContain(source, "RecorderRuntime");
        AssertDoesNotContain(source, "ReplayRuntime");
        AssertDoesNotContain(source, "DslParserRuntime");
        AssertDoesNotContain(source, "ProviderSdkClient");
        AssertDoesNotContain(source, "CloudSync");
        AssertDoesNotContain(source, "Environment.GetEnvironmentVariable");
        AssertDoesNotContain(source, "FinalPromptTextBuilder");
        AssertDoesNotContain(source, "RoutingEngine");
        AssertDoesNotContain(source, "RealPlanner");
        AssertDoesNotContain(source, "ExecutableTaskGraph");
        AssertDoesNotContain(source, "DependencyScheduler");
        AssertDoesNotContain(source, "EvidenceVerificationRuntime");
        AssertDoesNotContain(source, "File.Write");
        AssertDoesNotContain(source, "File.Read");
        AssertDoesNotContain(source, "Directory.");
        AssertDoesNotContain(source, "git ");
        AssertDoesNotContain(source, "React");
    }

    [TestMethod]
    public void ArtifactMarksAssignmentUiNoopAcceptanceReady()
    {
        var artifact = System.IO.File.ReadAllText(PathFor("artifacts", "agent-operations", "m527", "assignment-ui-noop-acceptance-summary.json"));

        AssertContains(artifact, "\"assignmentUiPreview\": true");
        AssertContains(artifact, "\"taskGraphInteractionNoOp\": true");
        AssertContains(artifact, "\"plannerUxAcceptancePack\": true");
        AssertContains(artifact, "\"interactionsAreNoOp\": true");
        AssertContains(artifact, "\"taskGraphExecutable\": false");
        AssertContains(artifact, "\"plannerRealIntroduced\": false");
        AssertContains(artifact, "\"promptCreationIntroduced\": false");
        AssertContains(artifact, "\"llmProviderCallsIntroduced\": false");
    }

    private NodalOsTaskGraphInteractionNoOpResult Apply(NodalOsTaskGraphInteractionKind kind)
    {
        var request = interactionService.CreateRequest(kind);
        return interactionService.Apply(request);
    }

    private static void AssertSafeOutput(string text)
    {
        foreach (var marker in SensitiveMarkers)
            AssertDoesNotContain(text, marker);
        foreach (var name in ForbiddenNames)
            AssertDoesNotContain(text, name);
    }

    private static string NewSource() =>
        System.IO.File.ReadAllText(PathFor("src", "OneBrain.AgentOperations.Contracts", "NodalOsAssignmentUiPreviewContracts.cs")) +
        System.IO.File.ReadAllText(PathFor("src", "OneBrain.AgentOperations.Contracts", "NodalOsTaskGraphInteractionNoOpContracts.cs")) +
        System.IO.File.ReadAllText(PathFor("src", "OneBrain.AgentOperations.Contracts", "NodalOsPlannerUxAcceptanceContracts.cs")) +
        System.IO.File.ReadAllText(PathFor("src", "OneBrain.AgentOperations.Core", "NodalOsAssignmentUiPreviewServices.cs")) +
        System.IO.File.ReadAllText(PathFor("src", "OneBrain.AgentOperations.Core", "NodalOsTaskGraphInteractionNoOpServices.cs")) +
        System.IO.File.ReadAllText(PathFor("src", "OneBrain.AgentOperations.Core", "NodalOsPlannerUxAcceptanceServices.cs"));

    private static string PathFor(params string[] parts) =>
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", Path.Combine(parts));

    private static void AssertContains(string text, string expected) =>
        StringAssert.Contains(text, expected);

    private static void AssertDoesNotContain(string text, string unexpected) =>
        Assert.IsFalse(text.Contains(unexpected, StringComparison.Ordinal), $"Unexpected marker found: {unexpected}");
}
