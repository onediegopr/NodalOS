using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.AgentOperations.Contracts;
using OneBrain.AgentOperations.Core;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("AssignmentReviewHistory")]
[TestCategory("HandoffCompare")]
[TestCategory("PlannerGovernance")]
[TestCategory("AssignmentReview")]
[TestCategory("PlannerHandoff")]
[TestCategory("AssignmentSafetyAudit")]
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
public sealed class NodalOsAssignmentPlannerGovernanceM531M533Tests
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

    private readonly NodalOsAssignmentReviewHistoryMockService historyService = new();
    private readonly NodalOsAssignmentReviewHistoryMockJsonSerializer historySerializer = new();
    private readonly NodalOsHandoffComparePreviewService compareService = new();
    private readonly NodalOsHandoffComparePreviewJsonSerializer compareSerializer = new();
    private readonly NodalOsPlannerGovernanceCloseoutService closeoutService = new();
    private readonly NodalOsPlannerGovernanceCloseoutJsonSerializer closeoutSerializer = new();

    [TestMethod]
    public void AssignmentReviewHistoryMock_StoresHistoryEntriesInMockStoreOnly()
    {
        var collection = CreateHistoryCollection();

        Assert.AreEqual(2, collection.Entries.Count);
        Assert.IsNotNull(collection.LatestEntry);
        Assert.IsNotNull(collection.PreviousEntry);
        Assert.IsTrue(collection.MockStoreOnly);
        Assert.IsFalse(collection.ProductivePersistenceUsed);
        Assert.IsFalse(collection.FilesystemUsed);
        Assert.IsFalse(collection.CloudUsed);
        Assert.IsFalse(collection.BrowserStorageUsed);
        Assert.IsFalse(collection.UsageMetricsUsed);
        Assert.IsFalse(collection.ClipboardUsed);
        Assert.IsTrue(collection.DiffCandidateRefs.Count >= 2);
        AssertSafeOutput(historySerializer.SerializeCollection(collection));
    }

    [TestMethod]
    public void AssignmentReviewHistoryEntries_AreDraftOnlyMockOnlyAndNonAuthoritative()
    {
        var collection = CreateHistoryCollection();

        foreach (var entry in collection.Entries)
        {
            Assert.IsTrue(entry.DraftOnly);
            Assert.IsFalse(entry.IsAuthoritative);
            Assert.IsTrue(entry.IsMockOnly);
            Assert.IsFalse(entry.CanRestoreAsAuthoritative);
            Assert.IsFalse(entry.CanAuthorizeExecution);
            Assert.IsFalse(entry.CanTriggerPlanner);
            Assert.IsFalse(entry.CanTriggerRuntime);
            Assert.IsFalse(entry.CanTriggerLlm);
            Assert.IsFalse(entry.CanAccessFilesystem);
            AssertSafeOutput(historySerializer.SerializeEntry(entry));
        }
    }

    [TestMethod]
    public void AssignmentReviewHistoryRestore_RemainsVisualMockAndCannotTriggerOperationalCapabilities()
    {
        var collection = CreateHistoryCollection();
        var restore = historyService.Restore(collection.LatestEntry!.HistoryEntryId);

        Assert.IsTrue(restore.VisualMockOnly);
        Assert.IsTrue(restore.DraftOnly);
        Assert.IsFalse(restore.IsAuthoritative);
        Assert.IsFalse(restore.CreatesExecutionRequest);
        Assert.IsFalse(restore.CreatesPrompt);
        Assert.IsFalse(restore.CallsPlanner);
        Assert.IsFalse(restore.CallsLlm);
        Assert.IsFalse(restore.CallsRuntime);
        Assert.IsFalse(restore.MutatesFilesystem);
        StringAssert.Contains(restore.UserFacingExplanationRedacted, "visual/mock");
        AssertSafeOutput(historySerializer.SerializeRestore(restore));
    }

    [TestMethod]
    public void AssignmentReviewHistorySerialization_IsDeterministicAndRedacted()
    {
        var collection = CreateHistoryCollection("Bear" + "er value " + "s" + "k-test");

        var first = historySerializer.SerializeCollection(collection);
        var second = historySerializer.SerializeCollection(collection);

        Assert.AreEqual(first, second);
        AssertSafeOutput(first);
    }

    [TestMethod]
    public void HandoffComparePreview_ComparesRefsAndMetadataOnly()
    {
        var result = CreateCompareResult();

        Assert.IsTrue(result.RefOnly);
        Assert.IsFalse(result.ContainsRawPayload);
        Assert.IsFalse(result.VerifiesEvidenceContent);
        Assert.IsFalse(result.CallsLlm);
        Assert.IsFalse(result.MutatesFilesystem);
        Assert.IsFalse(result.CallsNetwork);
        Assert.IsFalse(result.ProductivePersistenceUsed);
        AssertSafeOutput(compareSerializer.SerializeResult(result));
    }

    [TestMethod]
    public void HandoffComparePreview_ReportsChangedQuestionsReadinessEvidenceTimelineContextAndGuardrails()
    {
        var result = CreateCompareResult();

        Assert.IsTrue(result.ChangedOpenQuestionsRedacted.Count > 0);
        Assert.IsTrue(result.ChangedMissingReadinessGatesRedacted.Count > 0);
        Assert.IsTrue(result.ChangedEvidenceRefs.Count > 0);
        Assert.IsTrue(result.ChangedTimelineRefs.Count > 0);
        Assert.IsTrue(result.ChangedContextRefsRedacted.Count > 0);
        Assert.IsTrue(result.ChangedGuardrailsRedacted.Count > 0);
        Assert.IsTrue(result.UnchangedSectionsRedacted.Count > 0);
        Assert.IsTrue(result.UnverifiedClaimsRedacted.Any(text => text.Contains("unverified", StringComparison.Ordinal)));
    }

    [TestMethod]
    public void HandoffComparePreview_RenderIsStaticRedactedAndHasNoExternalScript()
    {
        var render = compareService.Render(CreateCompareResult());

        Assert.IsTrue(render.Deterministic);
        Assert.IsFalse(render.ContainsExternalResource);
        Assert.IsFalse(render.ContainsScript);
        AssertDoesNotContain(render.HtmlRedacted, "<script");
        AssertDoesNotContain(render.HtmlRedacted, "https://");
        AssertDoesNotContain(render.HtmlRedacted, "http://");
        AssertDoesNotContain(render.HtmlRedacted, "cdn");
        AssertSafeOutput(compareSerializer.SerializeRender(render));
    }

    [TestMethod]
    public void PlannerGovernanceCloseout_IncludesAllM519ThroughM533PreviewAndMockPieces()
    {
        var closeout = CreateCloseout();

        Assert.IsTrue(closeout.Status.AssignmentContractsReady);
        Assert.IsTrue(closeout.Status.TaskGraphDraftReady);
        Assert.IsTrue(closeout.Status.MissionPlanPreviewReady);
        Assert.IsTrue(closeout.Status.ReviewCardsReady);
        Assert.IsTrue(closeout.Status.UiPreviewReady);
        Assert.IsTrue(closeout.Status.NoOpInteractionsReady);
        Assert.IsTrue(closeout.Status.MockPersistenceReady);
        Assert.IsTrue(closeout.Status.HandoffReady);
        Assert.IsTrue(closeout.Status.SafetyAuditReady);
        Assert.IsTrue(closeout.Status.HistoryMockReady);
        Assert.IsTrue(closeout.Status.HandoffComparePreviewReady);
        Assert.IsFalse(closeout.Status.PlannerRuntimeImplemented);
        AssertSafeOutput(closeoutSerializer.Serialize(closeout));
    }

    [TestMethod]
    public void PlannerGovernanceCloseout_DeclaresNotReadyForRuntimePlannerModelAndFilesystem()
    {
        var closeout = CreateCloseout();
        var decisions = closeout.Decisions.ToList();

        CollectionAssert.Contains(decisions, NodalOsPlannerGovernanceCloseoutDecision.ReadyForNextGovernedPhase);
        CollectionAssert.Contains(decisions, NodalOsPlannerGovernanceCloseoutDecision.NotReadyForRuntime);
        CollectionAssert.Contains(decisions, NodalOsPlannerGovernanceCloseoutDecision.NotReadyForRealPlanner);
        CollectionAssert.Contains(decisions, NodalOsPlannerGovernanceCloseoutDecision.NotReadyForLlmCalls);
        CollectionAssert.Contains(decisions, NodalOsPlannerGovernanceCloseoutDecision.NotReadyForFilesystem);
        CollectionAssert.Contains(decisions, NodalOsPlannerGovernanceCloseoutDecision.RequiredNextAudit);
        CollectionAssert.Contains(decisions, NodalOsPlannerGovernanceCloseoutDecision.RequiredBeforeRealPlanner);
        CollectionAssert.Contains(decisions, NodalOsPlannerGovernanceCloseoutDecision.RequiredBeforeLlm);
        CollectionAssert.Contains(decisions, NodalOsPlannerGovernanceCloseoutDecision.RequiredBeforeFilesystem);
        CollectionAssert.Contains(decisions, NodalOsPlannerGovernanceCloseoutDecision.RequiredBeforeRuntime);
        Assert.IsTrue(closeout.Status.RuntimeExecutionBlocked);
        Assert.IsTrue(closeout.Status.LlmPromptBlocked);
        Assert.IsTrue(closeout.Status.FilesystemBlocked);
        Assert.IsTrue(closeout.Status.CloudBlocked);
    }

    [TestMethod]
    public void PlannerGovernanceCloseout_CannotAuthorizeOrCallOperationalCapabilities()
    {
        var closeout = CreateCloseout();

        Assert.IsTrue(closeout.DraftOnly);
        Assert.IsFalse(closeout.CanAuthorizeExecution);
        Assert.IsFalse(closeout.CanCallPlanner);
        Assert.IsFalse(closeout.CanCallLlm);
        Assert.IsFalse(closeout.CanAccessFilesystem);
        Assert.IsFalse(closeout.CanCallCloud);
        Assert.IsTrue(closeout.AuditTriggersRedacted.Count > 0);
        Assert.IsTrue(closeout.RecommendedNextStagesRedacted.Count > 0);
    }

    [TestMethod]
    public void Artifacts_MarkPlannerGovernanceCloseoutReadyAndRemainStatic()
    {
        var history = System.IO.File.ReadAllText(PathFor("artifacts", "agent-operations", "m533", "assignment-review-history-summary.json"));
        var compareMarkdown = System.IO.File.ReadAllText(PathFor("artifacts", "agent-operations", "m533", "handoff-compare-preview.md"));
        var compareJson = System.IO.File.ReadAllText(PathFor("artifacts", "agent-operations", "m533", "handoff-compare-summary.json"));
        var closeout = System.IO.File.ReadAllText(PathFor("artifacts", "agent-operations", "m533", "planner-governance-closeout.json"));
        var html = System.IO.File.ReadAllText(PathFor("artifacts", "agent-operations", "m533", "planner-governance-closeout-preview.html"));

        AssertContains(history, "\"historyMockReady\": true");
        AssertContains(compareJson, "\"comparesRefsAndMetadataOnly\": true");
        AssertContains(closeout, "\"plannerGovernanceCloseout\": true");
        AssertContains(closeout, "\"plannerRuntimeImplemented\": false");
        AssertContains(compareMarkdown, "NODAL OS Handoff Compare Preview");
        AssertDoesNotContain(html, "<script");
        AssertDoesNotContain(html, "https://");
        AssertDoesNotContain(html, "http://");
        AssertDoesNotContain(html, "cdn");
        AssertSafeOutput(history + compareMarkdown + compareJson + closeout + html);
    }

    [TestMethod]
    public void Boundary_NewPlannerGovernanceFiles_DoNotReferenceForbiddenRuntimeOrStoragePrimitives()
    {
        var source = NewSource();

        AssertDoesNotContain(source, "OneBrain." + "BrowserExecutor" + ".Cdp");
        AssertDoesNotContain(source, "Http" + "Client");
        AssertDoesNotContain(source, "Client" + "WebSocket");
        AssertDoesNotContain(source, "Process" + ".Start");
        AssertDoesNotContain(source, "System.Diagnostics." + "Process");
        AssertDoesNotContain(source, "Background" + "Service");
        AssertDoesNotContain(source, "Task.Run");
        AssertDoesNotContain(source, "ProviderSdkClient");
        AssertDoesNotContain(source, "CloudSync");
        AssertDoesNotContain(source, "FinalPromptTextBuilder");
        AssertDoesNotContain(source, "ExecutableTaskGraph");
        AssertDoesNotContain(source, "EvidenceVerificationRuntime");
        AssertDoesNotContain(source, "ProductiveDb");
        AssertDoesNotContain(source, "File.Write");
        AssertDoesNotContain(source, "File.Read");
        AssertDoesNotContain(source, "Directory.");
        AssertDoesNotContain(source, "git ");
        AssertDoesNotContain(source, "sche" + "duler");
        AssertDoesNotContain(source, "wor" + "ker");
        AssertDoesNotContain(source, "que" + "ue");
        AssertDoesNotContain(source, "tele" + "metry");
    }

    private NodalOsAssignmentReviewHistoryCollection CreateHistoryCollection(string label = "second review")
    {
        historyService.Clear();
        var snapshot = NodalOsAssignmentReviewHandoffSafetyFixtures.Snapshot();
        var handoff = NodalOsAssignmentReviewHandoffSafetyFixtures.Handoff();
        historyService.Store(historyService.CreateEntry(snapshot, handoff, "initial review"));
        return historyService.Store(historyService.CreateEntry(snapshot, handoff, label));
    }

    private NodalOsHandoffCompareResult CreateCompareResult()
    {
        var handoff = NodalOsAssignmentReviewHandoffSafetyFixtures.Handoff();
        var changed = handoff with
        {
            SelectedBlockersRedacted = [..handoff.SelectedBlockersRedacted, "new blocker ref"],
            OpenQuestionsRedacted = [..handoff.OpenQuestionsRedacted, "Which draft risk should be clarified next?"],
            MissingReadinessGatesRedacted = [..handoff.MissingReadinessGatesRedacted, "future readiness gate ref"],
            EvidenceRefs = [..handoff.EvidenceRefs, "evidence-compare-ref-only"],
            TimelineRefs = [..handoff.TimelineRefs, "timeline-compare-ref-only"],
            ContextRefsRedacted = [..handoff.ContextRefsRedacted, "context-compare-ref-only"],
            GuardrailRefs = [..handoff.GuardrailRefs, "guardrail-compare-ref-only"]
        };

        var request = compareService.CreateRequest(handoff.HandoffPackId, changed.HandoffPackId);
        return compareService.Compare(request, handoff, changed);
    }

    private NodalOsPlannerGovernanceCloseoutPack CreateCloseout()
    {
        var history = CreateHistoryCollection();
        var compare = CreateCompareResult();
        var audit = NodalOsAssignmentReviewHandoffSafetyFixtures.Audit();
        return closeoutService.CreateCloseout(history, compare, audit);
    }

    private static string NewSource()
    {
        var root = FindRepoRoot();
        var files = new[]
        {
            Path.Combine(root, "src", "OneBrain.AgentOperations.Contracts", "NodalOsAssignmentReviewHistoryMockContracts.cs"),
            Path.Combine(root, "src", "OneBrain.AgentOperations.Contracts", "NodalOsHandoffComparePreviewContracts.cs"),
            Path.Combine(root, "src", "OneBrain.AgentOperations.Contracts", "NodalOsPlannerGovernanceCloseoutContracts.cs"),
            Path.Combine(root, "src", "OneBrain.AgentOperations.Core", "NodalOsAssignmentReviewHistoryMockServices.cs"),
            Path.Combine(root, "src", "OneBrain.AgentOperations.Core", "NodalOsHandoffComparePreviewServices.cs"),
            Path.Combine(root, "src", "OneBrain.AgentOperations.Core", "NodalOsPlannerGovernanceCloseoutServices.cs")
        };

        return string.Join(Environment.NewLine, files.Select(System.IO.File.ReadAllText));
    }

    private static void AssertSafeOutput(string value)
    {
        foreach (var name in ForbiddenNames)
            AssertDoesNotContain(value, name);

        foreach (var marker in SensitiveMarkers)
            AssertDoesNotContain(value, marker);
    }

    private static void AssertContains(string value, string expected) =>
        StringAssert.Contains(value, expected);

    private static void AssertDoesNotContain(string value, string forbidden) =>
        Assert.IsFalse(value.Contains(forbidden, StringComparison.OrdinalIgnoreCase), $"Unexpected content: {forbidden}");

    private static string PathFor(params string[] segments) =>
        Path.Combine([FindRepoRoot(), .. segments]);

    private static string FindRepoRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null && !System.IO.File.Exists(Path.Combine(current.FullName, "OneBrain.slnx")))
            current = current.Parent;

        return current?.FullName ?? throw new InvalidOperationException("Repository root not found.");
    }
}
