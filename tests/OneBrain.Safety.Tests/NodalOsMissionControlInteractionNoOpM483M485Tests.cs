using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.AgentOperations.Contracts;
using OneBrain.AgentOperations.Core;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("MissionControlInteractionNoOp")]
[TestCategory("MissionControlShellReadOnly")]
[TestCategory("AuditAPreUiBoundaryNaming")]
[TestCategory("ApprovalUxHandoffObservability")]
[TestCategory("ApprovalTimelineEvidence")]
[TestCategory("CoreRuntimeRegistryEventBusRedaction")]
[TestCategory("NewTopicsIntake")]
[TestCategory("NamingCleanup")]
public sealed class NodalOsMissionControlInteractionNoOpM483M485Tests
{
    private static readonly string[] ForbiddenOperationalNames = ["Nexa", "NEXA", "NODRIX", "HOTEP"];
    private static readonly string[] SecretMarkers =
    [
        "Bearer ",
        "Authorization:",
        "Cookie:",
        "Set-Cookie:",
        "password=",
        "api_key",
        "access_token",
        "refresh_token",
        "id_token",
        "raw-body",
        "data:image/"
    ];

    private readonly NodalOsMissionControlInteractionService interactionService = new();
    private readonly NodalOsApprovalDecisionDraftService draftService = new();
    private readonly NodalOsMissionControlInteractionValidator validator = new();
    private readonly NodalOsMissionControlInteractionJsonSerializer serializer = new();

    [TestMethod]
    public void UiIntent_SelectTimelineEntry_IsNoOp()
    {
        var shell = Shell();
        var intent = interactionService.CreateIntent(
            NodalOsMissionControlUiIntentKind.SelectTimelineEntry,
            NodalOsMissionControlUiSurfaceKind.TimelineView,
            timelineEntryId: shell.Timeline.Entries.First().TimelineEntryId);

        AssertValidNoOpIntent(intent);
        Assert.AreEqual(shell.Timeline.Entries.First().TimelineEntryId, intent.TimelineEntryId);
    }

    [TestMethod]
    public void UiIntent_SelectApprovalCard_IsNoOp()
    {
        var shell = Shell();
        var intent = interactionService.CreateIntent(
            NodalOsMissionControlUiIntentKind.SelectApprovalCard,
            NodalOsMissionControlUiSurfaceKind.ApprovalDisplay,
            approvalCardId: shell.ApprovalDisplay.Cards.First().ApprovalCardId);

        AssertValidNoOpIntent(intent);
        Assert.AreEqual(shell.ApprovalDisplay.Cards.First().ApprovalCardId, intent.ApprovalCardId);
    }

    [TestMethod]
    public void UiIntent_SelectEvidenceRef_IsNoOp()
    {
        var shell = Shell();
        var intent = interactionService.CreateIntent(
            NodalOsMissionControlUiIntentKind.SelectEvidenceRef,
            NodalOsMissionControlUiSurfaceKind.EvidenceView,
            evidenceId: shell.Evidence.EvidenceRefs.First().EvidenceId);

        AssertValidNoOpIntent(intent);
        Assert.AreEqual(shell.Evidence.EvidenceRefs.First().EvidenceId, intent.EvidenceId);
    }

    [TestMethod]
    public void UiIntent_RequestExplanation_IsNoOp()
    {
        var intent = interactionService.CreateIntent(
            NodalOsMissionControlUiIntentKind.RequestExplanation,
            NodalOsMissionControlUiSurfaceKind.ApprovalDisplay,
            approvalCardId: Shell().ApprovalDisplay.Cards.First().ApprovalCardId,
            note: "User wants a policy explanation.");

        AssertValidNoOpIntent(intent);
    }

    [TestMethod]
    public void UiIntent_RequestChanges_IsNoOp()
    {
        var intent = interactionService.CreateIntent(
            NodalOsMissionControlUiIntentKind.RequestChanges,
            NodalOsMissionControlUiSurfaceKind.ApprovalDisplay,
            approvalCardId: Shell().ApprovalDisplay.Cards.First().ApprovalCardId,
            note: "User requests safer wording.");

        AssertValidNoOpIntent(intent);
    }

    [TestMethod]
    public void UiIntent_CopyTechnicalLog_IsNoOp()
    {
        var intent = interactionService.CreateIntent(
            NodalOsMissionControlUiIntentKind.CopyTechnicalLogIntent,
            NodalOsMissionControlUiSurfaceKind.ObservabilityLogPreview,
            observabilityReportId: "runtime-observability-report-fixture",
            note: "Copy intent captured but clipboard integration remains disabled.");

        AssertValidNoOpIntent(intent);
    }

    [TestMethod]
    public void UiIntent_AllExpectedKinds_CanBeRepresentedAsNoOp()
    {
        var kinds = new[]
        {
            NodalOsMissionControlUiIntentKind.ExpandTimelineEntry,
            NodalOsMissionControlUiIntentKind.CollapseTimelineEntry,
            NodalOsMissionControlUiIntentKind.SwitchNavigationSection,
            NodalOsMissionControlUiIntentKind.OpenObservabilityLogPreview,
            NodalOsMissionControlUiIntentKind.DeferApproval,
            NodalOsMissionControlUiIntentKind.AcknowledgeWarning,
            NodalOsMissionControlUiIntentKind.OpenGuardrailsSummary
        };

        foreach (var kind in kinds)
            AssertValidNoOpIntent(interactionService.CreateIntent(kind, NodalOsMissionControlUiSurfaceKind.Shell));
    }

    [TestMethod]
    public void NoOpEvent_CreatedFromIntent_DoesNotAuthorizeOrExecute()
    {
        var intent = NodalOsMissionControlInteractionFixtures.SelectTimelineIntent();
        var noOpEvent = interactionService.CreateNoOpEvent(intent);

        Assert.IsTrue(validator.ValidateNoOpEvent(noOpEvent).IsValid);
        Assert.IsTrue(noOpEvent.IsNoOp);
        Assert.IsFalse(noOpEvent.CanAuthorizeExecution);
        Assert.IsFalse(noOpEvent.RuntimeExecutionAllowed);
        Assert.IsTrue(noOpEvent.RuntimeExecutionDeferred);
        Assert.IsFalse(noOpEvent.RequiresPositiveExecutionGate);
        Assert.AreEqual(intent.IntentId, noOpEvent.IntentId);
    }

    [TestMethod]
    public void IntentSerialization_IsRedactedAndUsesOnlyNodalOsOperationalNaming()
    {
        var intent = interactionService.CreateIntent(
            NodalOsMissionControlUiIntentKind.SelectEvidenceRef,
            NodalOsMissionControlUiSurfaceKind.EvidenceView,
            note: "Authorization: Bearer abcdefghijklmnopqrstuvwxyz",
            metadata: new Dictionary<string, string> { ["api_key"] = "raw-fixture-key" });

        var json = serializer.SerializeIntent(intent);

        AssertContains(json, "[REDACTED]");
        AssertDoesNotContainSecretsOrForbiddenNames(json);
    }

    [TestMethod]
    public void IntentsDoNotMutateRegistry()
    {
        var registry = new NodalOsExecutionRegistry();
        var entry = registry.Register(NodalOsCoreRuntimeFixtures.ExecutionRequest());
        var before = entry.State;

        _ = interactionService.CreateIntent(
            NodalOsMissionControlUiIntentKind.SelectTimelineEntry,
            NodalOsMissionControlUiSurfaceKind.TimelineView,
            timelineEntryId: "timeline-entry-fixture");

        Assert.AreEqual(before, registry.TryGet(entry.RegistryEntryId)?.State);
    }

    [TestMethod]
    public void ApprovalDraft_ApproveRejectChangesExplanationAndDefer_AreNoOp()
    {
        var kinds = new[]
        {
            NodalOsApprovalDecisionKind.Approve,
            NodalOsApprovalDecisionKind.Reject,
            NodalOsApprovalDecisionKind.RequestChanges,
            NodalOsApprovalDecisionKind.RequestExplanation,
            NodalOsApprovalDecisionKind.Defer,
            NodalOsApprovalDecisionKind.HumanHandoffRequired
        };

        foreach (var kind in kinds)
            AssertValidNoOpDraft(NodalOsMissionControlInteractionFixtures.ApprovalDecisionDraft(kind));
    }

    [TestMethod]
    public void ApprovalDraft_PreservesApprovalCardEvidenceAndTimelineRefs()
    {
        var shell = Shell();
        var card = shell.ApprovalDisplay.Cards.First();
        var draft = draftService.CreateDraft(
            card.ApprovalCardId,
            NodalOsApprovalDecisionKind.RequestChanges,
            requestedChanges: "Clarify affected resource.",
            evidenceRefs: card.EvidenceRefs,
            timelineEntryIds: card.TimelineEntryIds);

        Assert.AreEqual(card.ApprovalCardId, draft.ApprovalCardId);
        Assert.IsTrue(draft.EvidenceRefs.Count > 0);
        Assert.IsTrue(draft.TimelineEntryIds.Count > 0);
        AssertValidNoOpDraft(draft);
    }

    [TestMethod]
    public void ApprovalDraft_SerializedNoteIsRedacted()
    {
        var draft = draftService.CreateDraft(
            Shell().ApprovalDisplay.Cards.First().ApprovalCardId,
            NodalOsApprovalDecisionKind.Reject,
            userNote: "password=super-secret",
            reason: "Cookie: session=abc123");

        var json = serializer.SerializeDecisionDraft(draft);

        AssertContains(json, "[REDACTED]");
        AssertDoesNotContainSecretsOrForbiddenNames(json);
    }

    [TestMethod]
    public void ApprovalDraft_DoesNotChangeApprovalOrRegistry()
    {
        var shell = Shell();
        var card = shell.ApprovalDisplay.Cards.First();
        var registry = new NodalOsExecutionRegistry();
        var entry = registry.Register(NodalOsCoreRuntimeFixtures.ExecutionRequest());

        _ = draftService.CreateDraft(card.ApprovalCardId, NodalOsApprovalDecisionKind.Approve);

        Assert.AreEqual(NodalOsApprovalStatus.PendingHumanDecision, card.Status);
        Assert.AreEqual(NodalOsExecutionRegistryState.Registered, registry.TryGet(entry.RegistryEntryId)?.State);
    }

    [TestMethod]
    public void ApprovalDraft_RequiresPositiveGateForAnyFutureExecution()
    {
        var draft = NodalOsMissionControlInteractionFixtures.ApprovalDecisionDraft();

        Assert.IsTrue(draft.RequiresPositiveExecutionGateForFutureExecution);
        Assert.IsFalse(draft.CanAuthorizeExecution);
        Assert.IsFalse(draft.RuntimeExecutionAllowed);
    }

    [TestMethod]
    public void UiState_CreatesValidReadOnlyState()
    {
        var state = NodalOsMissionControlInteractionFixtures.UiState();

        Assert.IsTrue(validator.ValidateUiState(state).IsValid);
        Assert.IsTrue(state.ReadOnlyUi);
        Assert.IsFalse(state.CanAuthorizeExecution);
        Assert.IsFalse(state.RuntimeExecutionAllowed);
        Assert.IsTrue(state.MockPersistenceOnly);
    }

    [TestMethod]
    public void UiState_InMemoryStore_SavesAndRestoresSelections()
    {
        var store = new NodalOsMissionControlUiStateStore();
        var state = store.CreateDefault(Shell());
        var saved = store.Save(state);
        var restored = store.TryGet(saved.StateId);

        Assert.IsNotNull(restored);
        Assert.AreEqual(saved.ActiveNavigationSection, restored.ActiveNavigationSection);
        Assert.AreEqual(saved.SelectedTimelineEntryId, restored.SelectedTimelineEntryId);
        Assert.AreEqual(saved.SelectedApprovalCardId, restored.SelectedApprovalCardId);
        Assert.AreEqual(saved.SelectedEvidenceId, restored.SelectedEvidenceId);
        CollectionAssert.AreEqual(saved.ExpandedTimelineEntryIds.ToArray(), restored.ExpandedTimelineEntryIds.ToArray());
    }

    [TestMethod]
    public void UiState_PreservesFiltersAndPanelState()
    {
        var state = NodalOsMissionControlInteractionFixtures.UiState();

        Assert.IsTrue(state.ActiveFiltersRedacted.ContainsKey("severity"));
        Assert.IsTrue(state.PanelCollapsed.ContainsKey("rightPanel"));
        Assert.IsTrue(state.LogPreviewOpen);
    }

    [TestMethod]
    public void UiState_SerializationIsRedactedAndMockOnly()
    {
        var state = NodalOsMissionControlInteractionFixtures.UiState() with
        {
            SelectedMissionId = "mission password=super-secret",
            ActiveFiltersRedacted = new Dictionary<string, string> { ["Authorization"] = "Bearer abcdefghijklmnopqrstuvwxyz" }
        };

        var json = serializer.SerializeUiState(state);

        AssertContains(json, "[REDACTED]");
        AssertContains(json, "\"mockPersistenceOnly\": true");
        AssertContains(json, "\"cloudPersistenceAllowed\": false");
        AssertContains(json, "\"productiveDatabasePersistenceAllowed\": false");
        AssertDoesNotContainSecretsOrForbiddenNames(json);
    }

    [TestMethod]
    public void UiState_UsesNoCloudDbOrForbiddenPath()
    {
        var state = NodalOsMissionControlInteractionFixtures.UiState();
        var json = serializer.SerializeUiState(state);

        Assert.IsFalse(state.CloudPersistenceAllowed);
        Assert.IsFalse(state.ProductiveDatabasePersistenceAllowed);
        AssertDoesNotContain(json, "C:\\Users\\diego\\OneDrive\\PERSONAL\\ONE Brain\\Codigo");
    }

    [TestMethod]
    public void Boundary_NewInteractionFiles_DoNotReferenceBrowserExecutorOrRuntimePrimitives()
    {
        var text = NewInteractionSourceText();

        AssertDoesNotContain(text, "OneBrain.BrowserExecutor.Cdp");
        AssertDoesNotContain(text, "using OneBrain.BrowserExecutor.Cdp");
        AssertDoesNotContain(text, "HttpClient");
        AssertDoesNotContain(text, "ClientWebSocket");
        AssertDoesNotContain(text, "Process.Start");
        AssertDoesNotContain(text, "System.Diagnostics.Process");
        AssertDoesNotContain(text, "BackgroundService");
        AssertDoesNotContain(text, "Task.Run");
        AssertDoesNotContain(text, "new Timer(");
        AssertDoesNotContain(text, "new Thread(");
    }

    [TestMethod]
    public void Boundary_NoExecutionWiringOrExternalCallsIntroduced()
    {
        var text = NewInteractionSourceText();

        AssertDoesNotContain(text, "ExecuteAsync");
        AssertDoesNotContain(text, "RunAsync");
        AssertDoesNotContain(text, "SendAsync");
        AssertDoesNotContain(text, "OpenAI");
        AssertDoesNotContain(text, "CloudClient");
        AssertDoesNotContain(text, "CloudSyncService");
        AssertDoesNotContain(text, "cloud://");
        AssertDoesNotContain(text, "BrowserAutomation");
        AssertDoesNotContain(text, "DslParser");
    }

    [TestMethod]
    public void ExistingSafety_ShellReadOnlyAndObservabilityRemainRedacted()
    {
        var shell = Shell();
        var shellValidation = new NodalOsMissionControlShellValidator().ValidateShell(shell);
        var html = new NodalOsMissionControlShellService().RenderReadOnlyHtml(shell);

        Assert.IsTrue(shellValidation.IsValid, string.Join(" | ", shellValidation.Errors));
        AssertContains(html, "Read-only preview");
        AssertDoesNotContainSecretsOrForbiddenNames(html);
        Assert.IsTrue(shell.ApprovalDisplay.Cards.All(card => !card.CanAuthorizeExecution));
    }

    [TestMethod]
    public void ArtifactMarksNoOpInteractionsAndNoRuntime()
    {
        var artifact = File.ReadAllText(ArtifactPath());

        AssertContains(artifact, "\"decision\": \"MISSION_CONTROL_INTERACTION_NOOP_READY\"");
        AssertContains(artifact, "\"projectOperationalName\": \"NODAL OS\"");
        AssertContains(artifact, "\"missionControlNoOpIntents\": true");
        AssertContains(artifact, "\"approvalDecisionDrafting\": true");
        AssertContains(artifact, "\"uiStatePersistenceMock\": true");
        AssertContains(artifact, "\"allInteractionsNoOp\": true");
        AssertContains(artifact, "\"approvalDraftsNonAuthoritative\": true");
        AssertContains(artifact, "\"uiStateMockOnly\": true");
        AssertContains(artifact, "\"canAuthorizeExecution\": false");
        AssertContains(artifact, "\"runtimeExecutionAllowed\": false");
        AssertContains(artifact, "\"positiveExecutionGateImplemented\": false");
        AssertContains(artifact, "\"browserExecutorCdpReferenced\": false");
    }

    [TestMethod]
    public void ReportAndRoadmapsReferenceM483M485AndNextEmptyStatesMilestone()
    {
        var report = File.ReadAllText(PathFor("docs", "reports", "mission-control-interaction-noop-m483-m485.md"));
        var vnext = File.ReadAllText(PathFor("docs", "roadmap", "nodal-os-roadmap-vnext.md"));
        var unified = File.ReadAllText(PathFor("docs", "roadmap", "nodal-os-unified-roadmap-post-pause.md"));

        AssertContains(report, "M483-M485");
        AssertContains(report, "no-op");
        AssertContains(vnext, "M483-M485 Mission Control Interaction No-Op Events");
        AssertContains(vnext, "M486-M488");
        AssertContains(unified, "M483-M485");
        AssertContains(unified, "M486-M488");
    }

    private static NodalOsMissionControlShellPreview Shell() =>
        NodalOsMissionControlShellFixtures.ShellPreview();

    private void AssertValidNoOpIntent(NodalOsMissionControlUiIntent intent)
    {
        Assert.IsTrue(validator.ValidateIntent(intent).IsValid);
        Assert.IsTrue(intent.IsNoOp);
        Assert.IsFalse(intent.CanAuthorizeExecution);
        Assert.IsFalse(intent.RuntimeExecutionAllowed);
        Assert.IsTrue(intent.RuntimeExecutionDeferred);
        Assert.IsFalse(intent.RequiresPositiveExecutionGate);
    }

    private void AssertValidNoOpDraft(NodalOsApprovalDecisionDraft draft)
    {
        Assert.IsTrue(validator.ValidateDecisionDraft(draft).IsValid);
        Assert.IsTrue(draft.IsNoOp);
        Assert.IsFalse(draft.CanAuthorizeExecution);
        Assert.IsFalse(draft.RuntimeExecutionAllowed);
        Assert.IsTrue(draft.RuntimeExecutionDeferred);
        Assert.IsTrue(draft.RequiresPositiveExecutionGateForFutureExecution);
    }

    private static string NewInteractionSourceText() =>
        string.Join(Environment.NewLine,
            new[]
            {
                PathFor("src", "OneBrain.AgentOperations.Contracts", "NodalOsMissionControlInteractionContracts.cs"),
                PathFor("src", "OneBrain.AgentOperations.Core", "NodalOsMissionControlInteractionServices.cs")
            }.Select(File.ReadAllText));

    private static string ArtifactPath() =>
        PathFor("artifacts", "agent-operations", "m485", "mission-control-interaction-noop-summary.json");

    private static void AssertDoesNotContainSecretsOrForbiddenNames(string text)
    {
        foreach (var marker in SecretMarkers)
            AssertDoesNotContain(text, marker);
        foreach (var forbidden in ForbiddenOperationalNames)
            AssertDoesNotContain(text, forbidden);
    }

    private static void AssertContains(string text, string expected) =>
        Assert.IsTrue(text.Contains(expected, StringComparison.Ordinal), expected);

    private static void AssertDoesNotContain(string text, string unexpected) =>
        Assert.IsFalse(text.Contains(unexpected, StringComparison.Ordinal), unexpected);

    private static string PathFor(params string[] parts) =>
        Path.Combine(new[] { RepoRoot() }.Concat(parts).ToArray());

    private static string RepoRoot()
    {
        var current = AppContext.BaseDirectory;
        while (!string.IsNullOrWhiteSpace(current))
        {
            if (File.Exists(Path.Combine(current, "OneBrain.slnx")))
                return current;

            current = Directory.GetParent(current)?.FullName;
        }

        throw new DirectoryNotFoundException("Could not locate repository root.");
    }
}
