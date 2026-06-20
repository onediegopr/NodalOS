using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.AgentOperations.Contracts;
using OneBrain.AgentOperations.Core;

namespace OneBrain.Safety.Tests;

[TestClass]
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
public sealed class NodalOsAssignmentReviewHandoffSafetyM528M530Tests
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

    private readonly NodalOsAssignmentReviewPersistenceMockService persistence = new();
    private readonly NodalOsAssignmentReviewPersistenceMockJsonSerializer persistenceSerializer = new();
    private readonly NodalOsPlannerHandoffService handoffService = new();
    private readonly NodalOsPlannerHandoffJsonSerializer handoffSerializer = new();
    private readonly NodalOsAssignmentSafetyAuditPackService auditService = new();
    private readonly NodalOsAssignmentSafetyAuditPackJsonSerializer auditSerializer = new();

    [TestMethod]
    public void ReviewPersistenceMock_StoresReviewStateWithoutProductivePersistence()
    {
        var snapshot = CreateStoredSnapshot();

        Assert.IsTrue(snapshot.MockStorageOnly);
        Assert.IsFalse(snapshot.ProductivePersistenceUsed);
        Assert.IsFalse(snapshot.FilesystemUsed);
        Assert.IsFalse(snapshot.CloudUsed);
        Assert.IsFalse(snapshot.BrowserStorageUsed);
        Assert.IsFalse(snapshot.ClipboardUsed);
        Assert.IsFalse(snapshot.CreatesExecutionRequest);
        Assert.IsFalse(snapshot.CreatesPrompt);
        Assert.IsTrue(snapshot.RestoredInteractionsRemainNoOp);
        Assert.IsNotNull(persistence.Get(snapshot.Session.ReviewSessionId));
        AssertSafeOutput(persistenceSerializer.SerializeSnapshot(snapshot));
    }

    [TestMethod]
    public void ReviewPersistenceMock_RestoresSelectedExpandedFilterAndNoteState()
    {
        var snapshot = CreateStoredSnapshot();
        var restored = persistence.Rehydrate(snapshot.Session.ReviewSessionId);

        Assert.AreEqual(snapshot.State.SelectedWorkItemId, restored.RestoredState.SelectedWorkItemId);
        Assert.IsTrue(restored.RestoredState.ExpandedWorkItemIds.Count > 0);
        Assert.IsTrue(restored.RestoredState.FiltersRedacted.Count > 0);
        Assert.IsTrue(restored.RestoredState.DraftNotesRedacted.Count > 0);
        Assert.IsTrue(restored.RestoredState.VisibleEvidenceRefs.Count > 0);
        Assert.IsTrue(restored.RestoredState.VisibleTimelineRefs.Count > 0);
        Assert.IsTrue(restored.RestoredState.VisibleContextRefsRedacted.Count > 0);
        Assert.IsTrue(restored.DraftOnly);
        Assert.IsFalse(restored.IsAuthoritative);
        Assert.IsFalse(restored.CanAuthorizeExecution);
        Assert.IsFalse(restored.CreatesExecutionRequest);
        Assert.IsFalse(restored.NotesCanBecomePrompts);
        Assert.IsTrue(restored.RestoredInteractionsRemainNoOp);
        AssertSafeOutput(persistenceSerializer.SerializeRehydration(restored));
    }

    [TestMethod]
    public void ReviewSession_RemainsDraftOnlyNonAuthoritativeAndCannotAuthorizeExecution()
    {
        var snapshot = CreateStoredSnapshot();

        Assert.IsTrue(snapshot.Session.DraftOnly);
        Assert.IsFalse(snapshot.Session.IsAuthoritative);
        Assert.IsFalse(snapshot.Session.MutatesRuntimeState);
        Assert.IsFalse(snapshot.Session.CanAuthorizeExecution);
        Assert.AreEqual(new DateTimeOffset(2026, 6, 20, 0, 0, 0, TimeSpan.Zero), snapshot.Session.CreatedAt);
    }

    [TestMethod]
    public void PlannerHandoff_DeclaresNonExecutableNonAuthoritativeAndRefsOnly()
    {
        var handoff = CreateHandoff();

        Assert.IsTrue(handoff.DraftOnly);
        Assert.IsFalse(handoff.IsAuthoritative);
        Assert.IsFalse(handoff.Executable);
        Assert.IsFalse(handoff.PlannerRuntimeUsed);
        Assert.IsFalse(handoff.CallsLlmProvider);
        Assert.IsFalse(handoff.CreatesPrompt);
        Assert.IsFalse(handoff.RuntimeExecutionAllowed);
        Assert.IsFalse(handoff.FilesystemAccessUsed);
        Assert.IsFalse(handoff.VerifiesEvidenceContent);
        Assert.IsTrue(handoff.MissionRef.Length > 0);
        Assert.IsTrue(handoff.AssignmentRef.Length > 0);
        Assert.IsTrue(handoff.TaskGraphDraftRefs.Count > 0);
        Assert.IsTrue(handoff.ReviewSessionRefs.Count > 0);
        AssertSafeOutput(handoffSerializer.SerializeHandoff(handoff));
    }

    [TestMethod]
    public void PlannerHandoff_IncludesBlockersOpenQuestionsMissingGatesAndRefOnlyEvidence()
    {
        var handoff = CreateHandoff();

        Assert.IsTrue(handoff.OpenQuestionsRedacted.Count > 0);
        Assert.IsTrue(handoff.MissingReadinessGatesRedacted.Count > 0);
        Assert.IsTrue(handoff.EvidenceRefs.Count > 0);
        Assert.IsTrue(handoff.TimelineRefs.Count > 0);
        Assert.IsTrue(handoff.ContextRefsRedacted.Count > 0);
        Assert.IsTrue(handoff.DisclaimersRedacted.Any(text => text.Contains("Non-executable", StringComparison.Ordinal)));
        Assert.IsTrue(handoff.DisclaimersRedacted.Any(text => text.Contains("No model call", StringComparison.Ordinal)));
        Assert.IsTrue(handoff.DisclaimersRedacted.Any(text => text.Contains("No prompt", StringComparison.Ordinal)));
        Assert.IsTrue(handoff.DisclaimersRedacted.Any(text => text.Contains("Filesystem access was not used", StringComparison.Ordinal)));
        Assert.IsTrue(handoff.EvidenceRefsOnlyRedacted.Contains("refs only", StringComparison.Ordinal));
        Assert.IsTrue(handoff.WhatIsNotVerifiedRedacted.Contains("not verified", StringComparison.Ordinal));
    }

    [TestMethod]
    public void PlannerHandoff_RenderOutputsDeterministicMarkdownAndStaticHtml()
    {
        var render = handoffService.Render(CreateHandoff());

        Assert.IsTrue(render.Deterministic);
        Assert.IsFalse(render.ContainsRawPayload);
        Assert.IsFalse(render.ContainsExternalResource);
        StringAssert.Contains(render.MarkdownRedacted, "What was reviewed");
        StringAssert.Contains(render.MarkdownRedacted, "What cannot execute");
        AssertDoesNotContain(render.HtmlRedacted, "<script");
        AssertDoesNotContain(render.HtmlRedacted, "https://");
        AssertDoesNotContain(render.HtmlRedacted, "http://");
        AssertDoesNotContain(render.HtmlRedacted, "cdn");
        AssertSafeOutput(handoffSerializer.SerializeRender(render));
    }

    [TestMethod]
    public void AssignmentSafetyAuditPack_ChecksNoOpNoPlannerNoModelNoRuntimeNoFilesystemNetworkCloud()
    {
        var snapshot = CreateStoredSnapshot();
        var handoff = CreateHandoff();
        var audit = auditService.CreateAudit(snapshot, handoff);

        Assert.AreEqual(NodalOsAssignmentSafetyAuditStatus.Pass, audit.OverallStatus);
        Assert.IsFalse(audit.ExecutionBoundaryCrossed);
        Assert.IsFalse(audit.PlannerRuntimeIntroduced);
        Assert.IsFalse(audit.PromptOrModelIntroduced);
        Assert.IsFalse(audit.FilesystemOrNetworkIntroduced);
        Assert.IsFalse(audit.ProductivePersistenceIntroduced);
        Assert.IsTrue(audit.Findings.Count >= Enum.GetValues<NodalOsAssignmentSafetyAuditDimension>().Length);
        Assert.IsTrue(audit.Findings.All(finding => finding.Status == NodalOsAssignmentSafetyAuditStatus.Pass));
        AssertSafeOutput(auditSerializer.Serialize(audit));
    }

    [TestMethod]
    public void AssignmentSafetyAuditPack_FailsWhenExecutionCapableMarkerIsIntroduced()
    {
        var snapshot = CreateStoredSnapshot();
        var handoff = CreateHandoff();
        var audit = auditService.CreateAudit(snapshot, handoff, "CanExecute=true");

        Assert.AreEqual(NodalOsAssignmentSafetyAuditStatus.Fail, audit.OverallStatus);
        Assert.IsTrue(audit.ExecutionBoundaryCrossed);
        Assert.IsTrue(audit.Findings.All(finding => finding.Status == NodalOsAssignmentSafetyAuditStatus.Fail));
    }

    [TestMethod]
    public void Serialization_RedactsAdversarialInputsAndRemainsDeterministic()
    {
        var unsafeValue = "Bear" + "er value " + "s" + "k-test";
        var preview = NodalOsAssignmentUiPreviewFixtures.Preview();
        var interaction = NodalOsAssignmentUiPreviewFixtures.Interaction(NodalOsTaskGraphInteractionKind.AddDraftNote);
        var snapshot = persistence.CreateSnapshot(preview with
        {
            Header = preview.Header with { AssignmentIdRef = unsafeValue, MissionIdRef = unsafeValue }
        }, interaction);

        var first = persistenceSerializer.SerializeSnapshot(snapshot);
        var second = persistenceSerializer.SerializeSnapshot(snapshot);

        Assert.AreEqual(first, second);
        AssertSafeOutput(first);
    }

    [TestMethod]
    public void Boundary_NewAssignmentReviewHandoffFiles_DoNotReferenceForbiddenRuntimeOrStoragePrimitives()
    {
        var source = NewSource();

        AssertDoesNotContain(source, "OneBrain.BrowserExecutor.Cdp");
        AssertDoesNotContain(source, "HttpClient");
        AssertDoesNotContain(source, "ClientWebSocket");
        AssertDoesNotContain(source, "Process.Start");
        AssertDoesNotContain(source, "System.Diagnostics.Process");
        AssertDoesNotContain(source, "BackgroundService");
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
        AssertDoesNotContain(source, "React");
    }

    [TestMethod]
    public void ArtifactMarksAssignmentReviewHandoffSafetyReady()
    {
        var summary = System.IO.File.ReadAllText(PathFor("artifacts", "agent-operations", "m530", "assignment-review-handoff-summary.json"));
        var audit = System.IO.File.ReadAllText(PathFor("artifacts", "agent-operations", "m530", "assignment-safety-audit-pack.json"));
        var handoff = System.IO.File.ReadAllText(PathFor("artifacts", "agent-operations", "m530", "assignment-planner-handoff.md"));
        var html = System.IO.File.ReadAllText(PathFor("artifacts", "agent-operations", "m530", "assignment-review-handoff-preview.html"));

        AssertContains(summary, "\"assignmentReviewPersistenceMock\": true");
        AssertContains(summary, "\"plannerHandoffContract\": true");
        AssertContains(summary, "\"assignmentSafetyAuditPack\": true");
        AssertContains(summary, "\"productivePersistenceIntroduced\": false");
        AssertContains(summary, "\"plannerRuntimeIntroduced\": false");
        AssertContains(audit, "\"overallStatus\": \"Pass\"");
        AssertContains(handoff, "NODAL OS Planner Handoff");
        AssertDoesNotContain(html, "<script");
        AssertDoesNotContain(html, "https://");
        AssertSafeOutput(summary + audit + handoff + html);
    }

    private NodalOsAssignmentReviewSnapshot CreateStoredSnapshot()
    {
        var preview = NodalOsAssignmentUiPreviewFixtures.Preview();
        var interaction = NodalOsAssignmentUiPreviewFixtures.Interaction(NodalOsTaskGraphInteractionKind.AddDraftNote);
        return persistence.Store(persistence.CreateSnapshot(preview, interaction));
    }

    private NodalOsPlannerHandoffPack CreateHandoff()
    {
        var preview = NodalOsAssignmentUiPreviewFixtures.Preview();
        var snapshot = CreateStoredSnapshot();
        var acceptance = NodalOsAssignmentUiPreviewFixtures.AcceptancePack();
        return handoffService.CreateHandoff(preview, snapshot, acceptance);
    }

    private static void AssertSafeOutput(string text)
    {
        foreach (var marker in SensitiveMarkers)
            AssertDoesNotContain(text, marker);
        foreach (var name in ForbiddenNames)
            AssertDoesNotContain(text, name);
    }

    private static string NewSource() =>
        System.IO.File.ReadAllText(PathFor("src", "OneBrain.AgentOperations.Contracts", "NodalOsAssignmentReviewPersistenceMockContracts.cs")) +
        System.IO.File.ReadAllText(PathFor("src", "OneBrain.AgentOperations.Contracts", "NodalOsPlannerHandoffContracts.cs")) +
        System.IO.File.ReadAllText(PathFor("src", "OneBrain.AgentOperations.Contracts", "NodalOsAssignmentSafetyAuditPackContracts.cs")) +
        System.IO.File.ReadAllText(PathFor("src", "OneBrain.AgentOperations.Core", "NodalOsAssignmentReviewPersistenceMockServices.cs")) +
        System.IO.File.ReadAllText(PathFor("src", "OneBrain.AgentOperations.Core", "NodalOsPlannerHandoffServices.cs")) +
        System.IO.File.ReadAllText(PathFor("src", "OneBrain.AgentOperations.Core", "NodalOsAssignmentSafetyAuditPackServices.cs"));

    private static string PathFor(params string[] parts) =>
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", Path.Combine(parts));

    private static void AssertContains(string text, string expected) =>
        StringAssert.Contains(text, expected);

    private static void AssertDoesNotContain(string text, string unexpected) =>
        Assert.IsFalse(text.Contains(unexpected, StringComparison.OrdinalIgnoreCase), $"Unexpected marker found: {unexpected}");
}
