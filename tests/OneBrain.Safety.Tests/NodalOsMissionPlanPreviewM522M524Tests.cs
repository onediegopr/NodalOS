using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.AgentOperations.Contracts;
using OneBrain.AgentOperations.Core;

namespace OneBrain.Safety.Tests;

[TestClass]
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
public sealed class NodalOsMissionPlanPreviewM522M524Tests
{
    private static readonly string[] ForbiddenNames = ["Nexa", "NEXA", "NODRIX", "HOTEP"];
    private static readonly string[] SensitiveMarkers = ["Bearer ", "Authorization:", "Cookie:", "password", "secret", "api_key", "access_token", "refresh_token", "id_token", "private key", "sk-", "connection string"];
    private readonly NodalOsAssignmentEngineDraftService assignmentService = new();
    private readonly NodalOsMissionPlanPreviewService service = new();
    private readonly NodalOsMissionPlanPreviewJsonSerializer serializer = new();

    [TestMethod]
    public void MissionPlanDraftPreview_CreatesPreviewFromAssignmentRequestAndTaskGraph()
    {
        var request = assignmentService.CreateAssignmentRequest();
        var taskGraph = assignmentService.CreateTaskGraphDraft(request);
        var readiness = assignmentService.CreatePlannerReadinessGate();

        var preview = service.CreateMissionPlanDraftPreview(request, taskGraph, readiness);

        Assert.AreEqual(request.AssignmentRequestId, preview.AssignmentRequestId);
        Assert.AreEqual(taskGraph.TaskGraphId, preview.TaskGraphId);
        Assert.AreEqual(request.WorkspaceId, preview.WorkspaceId);
        Assert.AreEqual(request.MissionId, preview.MissionId);
        Assert.AreEqual(NodalOsMissionPlanDraftStatus.DraftOnly, preview.DraftStatus);
        Assert.AreEqual(readiness.ReadinessState, preview.ReadinessStatus);
        Assert.IsTrue(preview.WorkItemsSummaryRedacted.Contains("draft work items", StringComparison.Ordinal));
        Assert.IsTrue(preview.DependencySummaryRedacted.Length > 0);
        Assert.IsTrue(preview.RiskSummaryRedacted.Length > 0);
        Assert.IsTrue(preview.BlockedItemsSummaryRedacted.Length > 0);
        Assert.IsTrue(preview.NextSafeStepsRedacted.Count > 0);
        AssertSafeOutput(serializer.SerializePreview(preview));
    }

    [TestMethod]
    public void MissionPlanDraftPreview_DisclosesDraftOnlyNoExecutableNoModelNoPromptNoRuntime()
    {
        var preview = NodalOsMissionPlanPreviewFixtures.Preview();

        CollectionAssert.Contains(preview.DisclosuresRedacted.ToList(), "Mission plan is draft-only.");
        CollectionAssert.Contains(preview.DisclosuresRedacted.ToList(), "No task is executable.");
        CollectionAssert.Contains(preview.DisclosuresRedacted.ToList(), "No model was called.");
        CollectionAssert.Contains(preview.DisclosuresRedacted.ToList(), "No prompt was generated.");
        CollectionAssert.Contains(preview.DisclosuresRedacted.ToList(), "No runtime action was created.");
        CollectionAssert.Contains(preview.DisclosuresRedacted.ToList(), "Human review is required before future use.");
        Assert.IsTrue(preview.DraftOnly);
        Assert.IsTrue(preview.ReadOnly);
        Assert.IsFalse(preview.TaskGraphExecutable);
        Assert.IsFalse(preview.CallsLlmProvider);
        Assert.IsFalse(preview.CreatesPrompt);
        Assert.IsFalse(preview.CreatesRuntimeAction);
        Assert.IsFalse(preview.TouchesFilesystem);
        Assert.IsFalse(preview.SchedulesWork);
        Assert.IsFalse(preview.CanAuthorizeExecution);
    }

    [TestMethod]
    public void MissionPlanDraftPreview_RendererIsStaticRedactedAndSafe()
    {
        var preview = NodalOsMissionPlanPreviewFixtures.Preview();
        var render = service.RenderMissionPlanDraftPreview(preview);

        Assert.IsTrue(render.StaticOnly);
        Assert.IsTrue(render.ReadOnly);
        Assert.IsFalse(render.ContainsRawSecrets);
        Assert.IsFalse(render.ContainsRuntimeControl);
        StringAssert.Contains(render.HtmlRedacted, "Mission plan is draft-only.");
        StringAssert.Contains(render.HtmlRedacted, "No task is executable.");
        StringAssert.Contains(render.HtmlRedacted, "No model was called.");
        AssertSafeOutput(serializer.SerializeRender(render));
    }

    [TestMethod]
    public void TaskGraphReviewCards_CreateCardsForRequiredKindsAndFutureExecutionIsBlocked()
    {
        var taskGraph = NodalOsAssignmentEngineFixtures.TaskGraphDraft();
        var cards = service.CreateTaskGraphReviewCards(taskGraph);
        var kinds = cards.Select(card => card.TaskKind).ToList();

        CollectionAssert.Contains(kinds, NodalOsAssignmentTaskKind.AnalysisDraft);
        CollectionAssert.Contains(kinds, NodalOsAssignmentTaskKind.DocumentationDraft);
        CollectionAssert.Contains(kinds, NodalOsAssignmentTaskKind.PlanningDraft);
        CollectionAssert.Contains(kinds, NodalOsAssignmentTaskKind.RiskAssessmentDraft);
        CollectionAssert.Contains(kinds, NodalOsAssignmentTaskKind.HandoffDraft);
        CollectionAssert.Contains(kinds, NodalOsAssignmentTaskKind.AdvisorSuggestionDraft);
        Assert.AreEqual(
            NodalOsTaskGraphReviewCardState.FutureExecutionBlocked,
            cards.Single(card => card.TaskKind == NodalOsAssignmentTaskKind.FutureExecutionPlaceholder).ReviewState);
        AssertSafeOutput(serializer.SerializeReviewCards(cards));
    }

    [TestMethod]
    public void TaskGraphReviewCards_AreNonAuthoritativeNoOpAndBlockFutureRuntimeLlmFilesystem()
    {
        var cards = NodalOsMissionPlanPreviewFixtures.ReviewCards();

        Assert.IsTrue(cards.All(card => card.NonAuthoritative));
        Assert.IsTrue(cards.All(card => card.UserOptionsAreNoOp));
        Assert.IsTrue(cards.All(card => !card.CanExecute));
        Assert.IsTrue(cards.All(card => card.RequiresHumanReview));
        Assert.IsTrue(cards.All(card => card.UserOptions.Count == Enum.GetValues<NodalOsTaskGraphReviewOptionKind>().Length));
        Assert.IsTrue(cards.Any(card => card.RequiresFutureLlm));
        Assert.IsTrue(cards.Any(card => card.RequiresFutureRuntime));
        Assert.IsTrue(cards.All(card => card.DisabledCapabilitiesRedacted.Any(value => value.Contains("future runtime blocked", StringComparison.Ordinal))));
        Assert.IsTrue(cards.All(card => card.DisabledCapabilitiesRedacted.Any(value => value.Contains("future model call blocked", StringComparison.Ordinal))));
        Assert.IsTrue(cards.All(card => card.DisabledCapabilitiesRedacted.Any(value => value.Contains("future filesystem access blocked", StringComparison.Ordinal))));
        Assert.IsTrue(cards.Any(card => card.DependencyIds.Count > 0));
        Assert.IsTrue(cards.Any(card => card.BlockersRedacted.Count > 0));
    }

    [TestMethod]
    public void AssignmentEvidenceLinking_CreatesAllRequiredLinkTypesAsRefOnly()
    {
        foreach (var linkType in Enum.GetValues<NodalOsAssignmentEvidenceLinkType>())
        {
            var link = service.CreateAssignmentEvidenceLink(linkType);

            Assert.AreEqual(linkType, link.LinkType);
            Assert.AreEqual(NodalOsAssignmentEvidenceLinkStatus.LinkedRefOnly, link.LinkStatus);
            Assert.IsTrue(link.RefOnly);
            Assert.IsFalse(link.ContainsRawEvidencePayload);
            Assert.IsFalse(link.ContainsInlineScreenshot);
            Assert.IsFalse(link.ContainsRawDom);
            Assert.IsFalse(link.ContainsRawNetwork);
            Assert.IsFalse(link.ReadsFiles);
            Assert.IsFalse(link.VerifiesRealContent);
            Assert.IsFalse(link.ConvertsPlanToAuthoritativeTruth);
            Assert.IsFalse(link.CallsLlmProvider);
            Assert.IsFalse(link.CallsCloud);
            Assert.IsFalse(link.ExecutesRuntime);
            StringAssert.Contains(link.ValidationResultRedacted, "unverified");
            AssertSafeOutput(serializer.SerializeEvidenceLink(link));
        }
    }

    [TestMethod]
    public void AssignmentEvidenceLinking_CreatesAllRequiredStatusesAndBlocksUnsafeEvidence()
    {
        foreach (var status in Enum.GetValues<NodalOsAssignmentEvidenceLinkStatus>())
        {
            var link = service.CreateAssignmentEvidenceLink(status: status);

            Assert.AreEqual(status, link.LinkStatus);
            Assert.IsTrue(link.RefOnly);
            AssertSafeOutput(serializer.SerializeEvidenceLink(link));
        }

        var unsafeLink = service.CreateAssignmentEvidenceLink(
            evidenceRefId: "unsafe-raw-screenshot-dom-network-payload",
            status: NodalOsAssignmentEvidenceLinkStatus.LinkedRefOnly);

        Assert.AreEqual(NodalOsAssignmentEvidenceLinkStatus.BlockedUnsafeEvidence, unsafeLink.LinkStatus);
        Assert.IsFalse(unsafeLink.ContainsRawEvidencePayload);
        Assert.IsFalse(unsafeLink.ContainsInlineScreenshot);
        Assert.IsFalse(unsafeLink.ContainsRawDom);
        Assert.IsFalse(unsafeLink.ContainsRawNetwork);
        StringAssert.Contains(unsafeLink.ValidationResultRedacted, "Blocked unsafe evidence ref.");
        AssertSafeOutput(serializer.SerializeEvidenceLink(unsafeLink));
    }

    [TestMethod]
    public void MissionPlanReviewAndEvidence_SanitizeAdversarialInputs()
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
            var request = assignmentService.CreateAssignmentRequest(
                workspaceId: unsafeValue,
                missionId: unsafeValue,
                userContextRef: unsafeValue);
            var taskGraph = assignmentService.CreateTaskGraphDraft(request);
            var preview = service.CreateMissionPlanDraftPreview(request, taskGraph);
            var cards = service.CreateTaskGraphReviewCards(taskGraph);
            var link = service.CreateAssignmentEvidenceLink(
                assignmentRequestId: unsafeValue,
                taskGraphId: unsafeValue,
                workItemId: unsafeValue,
                evidenceRefId: unsafeValue,
                timelineRefId: unsafeValue,
                contextRefId: unsafeValue,
                reason: unsafeValue);

            AssertSafeOutput(serializer.SerializePreview(preview));
            AssertSafeOutput(serializer.SerializeReviewCards(cards));
            AssertSafeOutput(serializer.SerializeEvidenceLink(link));
        }
    }

    [TestMethod]
    public void Boundary_NewMissionPlanPreviewFiles_DoNotReferenceForbiddenRuntimeOrPlannerPrimitives()
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
        AssertDoesNotContain(source, "PricingLookup");
        AssertDoesNotContain(source, "ModelAvailabilityLookup");
        AssertDoesNotContain(source, "RoutingEngine");
        AssertDoesNotContain(source, "RealPlanner");
        AssertDoesNotContain(source, "ExecutableTaskGraph");
        AssertDoesNotContain(source, "DependencyScheduler");
        AssertDoesNotContain(source, "EvidenceVerificationRuntime");
        AssertDoesNotContain(source, "File.Write");
        AssertDoesNotContain(source, "File.Read");
        AssertDoesNotContain(source, "Directory.");
        AssertDoesNotContain(source, "git ");
        AssertDoesNotContain(source, "embeddings client");
        AssertDoesNotContain(source, "React");
    }

    [TestMethod]
    public void ExistingSafetyContinuity_AssignmentPromptByokAndShellRemainNoAuthority()
    {
        var assignment = NodalOsAssignmentEngineFixtures.AssignmentRequest();
        var prompt = NodalOsPromptGovernanceFixtures.PromptPreviewPolicy();
        var byok = NodalOsByokProviderFixtures.ReferenceOnlySettings();
        var shell = NodalOsMissionControlShellFixtures.ShellPreview();

        Assert.IsTrue(assignment.DraftOnly);
        Assert.IsFalse(assignment.CanAuthorizeExecution);
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
    public void ArtifactMarksMissionPlanDraftReviewReady()
    {
        var artifact = System.IO.File.ReadAllText(PathFor("artifacts", "agent-operations", "m524", "mission-plan-preview-summary.json"));

        AssertContains(artifact, "\"missionPlanDraftPreview\": true");
        AssertContains(artifact, "\"taskGraphReviewCards\": true");
        AssertContains(artifact, "\"assignmentEvidenceLinking\": true");
        AssertContains(artifact, "\"missionPlanDraftOnly\": true");
        AssertContains(artifact, "\"taskGraphExecutable\": false");
        AssertContains(artifact, "\"assignmentEvidenceLinkingRefOnly\": true");
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
        System.IO.File.ReadAllText(PathFor("src", "OneBrain.AgentOperations.Contracts", "NodalOsMissionPlanPreviewContracts.cs")) +
        System.IO.File.ReadAllText(PathFor("src", "OneBrain.AgentOperations.Core", "NodalOsMissionPlanPreviewServices.cs"));

    private static string PathFor(params string[] parts) =>
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", Path.Combine(parts));

    private static void AssertContains(string text, string expected) =>
        StringAssert.Contains(text, expected);

    private static void AssertDoesNotContain(string text, string unexpected) =>
        Assert.IsFalse(text.Contains(unexpected, StringComparison.Ordinal), $"Unexpected marker found: {unexpected}");
}
