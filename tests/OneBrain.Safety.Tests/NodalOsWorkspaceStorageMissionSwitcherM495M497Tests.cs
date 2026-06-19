using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.AgentOperations.Contracts;
using OneBrain.AgentOperations.Core;

namespace OneBrain.Safety.Tests;

[TestClass]
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
public sealed class NodalOsWorkspaceStorageMissionSwitcherM495M497Tests
{
    private static readonly string[] ForbiddenNames = ["Nexa", "NEXA", "NODRIX", "HOTEP"];
    private static readonly string[] SecretMarkers = ["Bearer ", "Authorization:", "Cookie:", "password", "secret", "api_key", "access_token", "refresh_token", "id_token", "private key"];
    private readonly NodalOsWorkspaceService workspaceService = new();
    private readonly NodalOsWorkspaceMissionBindingService bindingService = new();
    private readonly NodalOsWorkspaceSwitcherService switcherService = new();
    private readonly NodalOsWorkspaceMissionValidator validator = new();
    private readonly NodalOsWorkspaceMissionJsonSerializer serializer = new();

    [TestMethod]
    public void StorageMock_StoresWorkspaceDraftInMemory()
    {
        var storage = new NodalOsWorkspaceStorageMock();
        var result = storage.StoreDraft(workspaceService.CreateWorkspaceDraft());

        Assert.IsTrue(result.Accepted, string.Join(" | ", result.Errors));
        Assert.AreEqual(NodalOsWorkspaceStorageMockStatus.DraftStored, result.Status);
        Assert.AreEqual(1, storage.Summary().WorkspaceCount);
        Assert.IsTrue(storage.Summary().InMemoryOnly);
    }

    [TestMethod]
    public void StorageMock_ListsAndGetsWorkspaceById()
    {
        var storage = NodalOsWorkspaceMissionFixtures.StorageWithDraftAndActive();

        Assert.AreEqual(2, storage.ListWorkspaces().Count);
        Assert.IsNotNull(storage.GetById("workspace-local-active"));
    }

    [TestMethod]
    public void StorageMock_RejectsDuplicateId()
    {
        var storage = new NodalOsWorkspaceStorageMock();
        var workspace = workspaceService.CreateWorkspaceDraft("workspace-duplicate");

        Assert.IsTrue(storage.StoreDraft(workspace).Accepted);
        var duplicate = storage.StoreDraft(workspace with { DisplayNameRedacted = "Different Display" });

        Assert.IsFalse(duplicate.Accepted);
        Assert.AreEqual(NodalOsWorkspaceStorageMockStatus.InvalidRejected, duplicate.Status);
        AssertContains(string.Join(" | ", duplicate.Errors), "Duplicate workspace id");
    }

    [TestMethod]
    public void StorageMock_RejectsDuplicateDisplayName()
    {
        var storage = new NodalOsWorkspaceStorageMock();

        Assert.IsTrue(storage.StoreDraft(workspaceService.CreateWorkspaceDraft("workspace-a")).Accepted);
        var duplicate = storage.StoreDraft(workspaceService.CreateWorkspaceDraft("workspace-b"));

        Assert.IsFalse(duplicate.Accepted);
        AssertContains(string.Join(" | ", duplicate.Errors), "display name");
    }

    [TestMethod]
    public void StorageMock_RejectsInvalidWorkspace()
    {
        var storage = new NodalOsWorkspaceStorageMock();
        var invalid = workspaceService.CreateWorkspaceDraft() with { RuntimeExecutionAllowed = true };

        var result = storage.StoreDraft(invalid);

        Assert.IsFalse(result.Accepted);
        AssertContains(string.Join(" | ", result.Errors), "runtime");
    }

    [TestMethod]
    public void StorageMock_ArchivesWorkspaceMock()
    {
        var storage = new NodalOsWorkspaceStorageMock();
        storage.StoreDraft(workspaceService.CreateWorkspaceDraft());

        var result = storage.ArchiveWorkspaceMock("workspace-local-draft");

        Assert.IsTrue(result.Accepted, string.Join(" | ", result.Errors));
        Assert.AreEqual(NodalOsWorkspaceStorageMockStatus.ArchivedMock, result.Status);
        Assert.AreEqual(1, storage.Summary().ArchivedCount);
    }

    [TestMethod]
    public void StorageMock_ClearResetWorks()
    {
        var storage = NodalOsWorkspaceMissionFixtures.StorageWithDraftAndActive();

        storage.Clear();

        Assert.AreEqual(0, storage.ListWorkspaces().Count);
        Assert.AreEqual(NodalOsWorkspaceStorageMockStatus.Empty, storage.Summary().Status);
    }

    [TestMethod]
    public void StorageMock_PreservesWorkspaceRefs()
    {
        var storage = new NodalOsWorkspaceStorageMock();
        var workspace = workspaceService.CreateWorkspaceDraft();
        storage.StoreDraft(workspace);
        var stored = storage.GetById(workspace.WorkspaceId);

        Assert.IsNotNull(stored);
        Assert.AreEqual(workspace.PathJailBindingId, stored.PathJailBindingId);
        Assert.AreEqual(workspace.ActiveMissionRefs.Count, stored.ActiveMissionRefs.Count);
        Assert.AreEqual(workspace.EvidenceRefs.Count, stored.EvidenceRefs.Count);
        Assert.AreEqual(workspace.TimelineRefs.Count, stored.TimelineRefs.Count);
    }

    [TestMethod]
    public void StorageMock_SerializationIsSafeAndNoExternalState()
    {
        var storage = NodalOsWorkspaceMissionFixtures.StorageWithDraftAndActive();
        var json = serializer.SerializeStorageSummary(storage.Summary());

        AssertSafeSerialized(json);
        Assert.IsFalse(storage.Summary().RealFilesystemTouched);
        Assert.IsFalse(storage.Summary().DatabaseUsed);
        Assert.IsFalse(storage.Summary().CloudSyncAllowed);
        Assert.IsFalse(storage.Summary().RuntimeExecutionAllowed);
    }

    [TestMethod]
    public void MissionBinding_CreatesWorkspaceMissionBinding()
    {
        var binding = bindingService.CreateBinding(workspaceService.CreateActiveReadOnlyWorkspace());
        var result = validator.ValidateMissionBinding(binding);

        Assert.IsTrue(result.IsValid, string.Join(" | ", result.Errors));
        Assert.AreEqual("workspace-local-active", binding.WorkspaceId);
        Assert.AreEqual("mission-local-preview", binding.MissionId);
        Assert.AreEqual(NodalOsWorkspaceMissionBindingStatus.BoundReadOnly, binding.Status);
    }

    [TestMethod]
    public void MissionBinding_RequiresWorkspaceAndMissionIds()
    {
        var binding = bindingService.CreateBinding(workspaceService.CreateActiveReadOnlyWorkspace()) with
        {
            WorkspaceId = "",
            MissionId = ""
        };

        var result = validator.ValidateMissionBinding(binding);

        Assert.IsFalse(result.IsValid);
        AssertContains(string.Join(" | ", result.Errors), "WorkspaceId");
        AssertContains(string.Join(" | ", result.Errors), "MissionId");
    }

    [TestMethod]
    public void MissionBinding_PreservesRefs()
    {
        var binding = bindingService.CreateBinding(workspaceService.CreateActiveReadOnlyWorkspace());

        Assert.IsFalse(string.IsNullOrWhiteSpace(binding.PathJailBindingId));
        Assert.IsTrue(binding.ActiveTimelineRefs.Count > 0);
        Assert.IsTrue(binding.ActiveApprovalRefs.Count > 0);
        Assert.IsTrue(binding.ActiveEvidenceRefs.Count > 0);
        Assert.IsTrue(binding.ObservabilityReportRefs.Count > 0);
        Assert.IsTrue(binding.UiStateRefs.Count > 0);
    }

    [TestMethod]
    public void MissionBinding_IsReadOnlyNoRuntimeNoTaskGraphNoFilesystem()
    {
        var binding = bindingService.CreateBinding(workspaceService.CreateActiveReadOnlyWorkspace());

        Assert.IsTrue(binding.ReadOnlyPreview);
        Assert.IsFalse(binding.CanAuthorizeExecution);
        Assert.IsFalse(binding.RuntimeExecutionAllowed);
        Assert.IsFalse(binding.CloudSyncAllowed);
        Assert.IsFalse(binding.LlmProviderCallsAllowed);
        Assert.IsFalse(binding.TaskGraphCreated);
        Assert.IsFalse(binding.TouchesFilesystem);
        Assert.IsFalse(binding.MutatesExecutionRegistryRuntime);
    }

    [TestMethod]
    public void MissionBinding_SerializationIsSafe()
    {
        var json = serializer.SerializeMissionBinding(bindingService.CreateBinding(workspaceService.CreateActiveReadOnlyWorkspace()));

        AssertSafeSerialized(json);
    }

    [TestMethod]
    public void WorkspaceSwitcher_CreatesValidListItem()
    {
        var item = switcherService.CreateItem(workspaceService.CreateActiveReadOnlyWorkspace(), isActive: true);
        var result = validator.ValidateSwitcherItem(item);

        Assert.IsTrue(result.IsValid, string.Join(" | ", result.Errors));
        Assert.IsTrue(item.IsActive);
        Assert.AreEqual(NodalOsWorkspaceSwitcherItemStatus.ActiveReadOnly, item.Status);
    }

    [TestMethod]
    public void WorkspaceSwitcher_MarksArchivedAndBlockedItems()
    {
        var archived = switcherService.CreateItem(workspaceService.CreateWorkspaceDraft() with { Status = NodalOsWorkspaceStatus.Archived });
        var blocked = switcherService.CreateItem(workspaceService.CreateWorkspaceDraft("workspace-blocked") with
        {
            DisplayNameRedacted = "Blocked Workspace Preview",
            Status = NodalOsWorkspaceStatus.Blocked
        });

        Assert.IsTrue(archived.IsArchived);
        Assert.IsTrue(blocked.IsBlocked);
    }

    [TestMethod]
    public void WorkspaceSwitcher_ShowsPrivacyPathJailAndCounts()
    {
        var item = switcherService.CreateItem(workspaceService.CreateActiveReadOnlyWorkspace(), isActive: true);

        AssertContains(item.PrivacyBadgeRedacted, "Local-first");
        AssertContains(item.PathJailStatusRedacted, "Path jail");
        Assert.AreEqual(1, item.ActiveMissionCount);
        Assert.AreEqual(1, item.PendingApprovalCount);
        Assert.AreEqual(1, item.EvidenceCount);
    }

    [TestMethod]
    public void WorkspaceSwitcher_CreatesSwitchIntentNoOp()
    {
        var intent = switcherService.CreateSwitchIntent("workspace-local-active");
        var result = validator.ValidateSwitchIntent(intent);

        Assert.IsTrue(result.IsValid, string.Join(" | ", result.Errors));
        Assert.IsTrue(intent.IsNoOp);
        Assert.IsFalse(intent.CanAuthorizeExecution);
        Assert.IsFalse(intent.RuntimeExecutionAllowed);
        Assert.IsFalse(intent.CloudSyncAllowed);
        Assert.IsFalse(intent.FilesystemAccessAllowed);
    }

    [TestMethod]
    public void WorkspaceSwitcher_SwitchResultIsPreviewMock()
    {
        var switcher = NodalOsWorkspaceMissionFixtures.WorkspaceSwitcher();
        var preview = switcher.SwitchResultPreview;

        Assert.IsTrue(preview.PreviewOnly);
        Assert.IsTrue(preview.MockOnly);
        Assert.IsFalse(preview.StateChangedProductively);
        Assert.IsFalse(preview.CanAuthorizeExecution);
        Assert.IsFalse(preview.RuntimeExecutionAllowed);
    }

    [TestMethod]
    public void WorkspaceSwitcher_NewWorkspaceAndImportOptionsAreNoOp()
    {
        var newWorkspaceIntent = switcherService.CreateSwitchIntent("workspace-new", NodalOsWorkspaceSwitcherOptionKind.NewWorkspaceDraft);
        var importIntent = switcherService.CreateSwitchIntent("workspace-import", NodalOsWorkspaceSwitcherOptionKind.ImportProjectWizardMock);

        Assert.AreEqual(NodalOsWorkspaceSwitcherOptionKind.NewWorkspaceDraft, newWorkspaceIntent.OptionKind);
        Assert.AreEqual(NodalOsWorkspaceSwitcherOptionKind.ImportProjectWizardMock, importIntent.OptionKind);
        Assert.IsTrue(newWorkspaceIntent.IsNoOp);
        Assert.IsTrue(importIntent.IsNoOp);
        Assert.IsFalse(newWorkspaceIntent.RuntimeExecutionAllowed);
        Assert.IsFalse(importIntent.RuntimeExecutionAllowed);
    }

    [TestMethod]
    public void WorkspaceSwitcher_ContractValidAndSerializedSafe()
    {
        var switcher = NodalOsWorkspaceMissionFixtures.WorkspaceSwitcher();
        var result = validator.ValidateSwitcher(switcher);
        var json = serializer.SerializeSwitcher(switcher);

        Assert.IsTrue(result.IsValid, string.Join(" | ", result.Errors));
        Assert.IsTrue(switcher.ReadOnlyPreview);
        Assert.IsTrue(switcher.MockOnly);
        Assert.IsFalse(switcher.FilesystemAccessAllowed);
        AssertSafeSerialized(json);
    }

    [TestMethod]
    public void Boundary_NewWorkspaceMissionFiles_DoNotReferenceRuntimePrimitives()
    {
        var source = NewWorkspaceMissionSource();

        AssertDoesNotContain(source, "OneBrain.BrowserExecutor.Cdp");
        AssertDoesNotContain(source, "HttpClient");
        AssertDoesNotContain(source, "ClientWebSocket");
        AssertDoesNotContain(source, "Process.Start");
        AssertDoesNotContain(source, "System.Diagnostics.Process");
        AssertDoesNotContain(source, "BackgroundService");
        AssertDoesNotContain(source, "Task.Run");
        AssertDoesNotContain(source, "ExecuteAsync");
        AssertDoesNotContain(source, "OpenAI");
        AssertDoesNotContain(source, "TelemetryClient");
        AssertDoesNotContain(source, "AnalyticsClient");
        AssertDoesNotContain(source, "File.Write");
        AssertDoesNotContain(source, "Directory.CreateDirectory");
    }

    [TestMethod]
    public void ExistingSafetyContinuity_PreviousWorkspaceAndMissionControlRemainSafe()
    {
        var workspace = NodalOsWorkspaceFixtures.WorkspaceDraft();
        var shell = NodalOsMissionControlShellFixtures.ShellPreview();
        var visual = new NodalOsMissionControlVisualService().CreateResponsiveDesktopLayoutSpec();
        var guidance = NodalOsMissionControlGuidanceFixtures.GuardrailExplainers();
        var intent = NodalOsMissionControlInteractionFixtures.SelectTimelineIntent();

        Assert.IsTrue(workspace.ReadOnlyPreview);
        Assert.IsFalse(shell.CanAuthorizeExecution);
        Assert.IsFalse(visual.CanExecuteOrMutateState);
        Assert.IsTrue(guidance.All(explainer => !explainer.CanUnlockExecution));
        Assert.IsTrue(intent.IsNoOp);
        Assert.IsFalse(intent.RuntimeExecutionAllowed);
    }

    [TestMethod]
    public void ArtifactMarksWorkspaceStorageMissionSwitcherMock()
    {
        var artifact = File.ReadAllText(PathFor("artifacts", "agent-operations", "m497", "workspace-storage-mission-switcher-summary.json"));

        AssertContains(artifact, "\"workspaceStorageMock\": true");
        AssertContains(artifact, "\"missionBinding\": true");
        AssertContains(artifact, "\"workspaceSwitcherContract\": true");
        AssertContains(artifact, "\"workspaceStorageMockOnly\": true");
        AssertContains(artifact, "\"missionBindingReadOnly\": true");
        AssertContains(artifact, "\"workspaceSwitcherNoOp\": true");
        AssertContains(artifact, "\"fileReadWriteDeleteIntroduced\": false");
    }

    private static void AssertSafeSerialized(string text)
    {
        foreach (var marker in SecretMarkers)
            AssertDoesNotContain(text, marker);
        foreach (var name in ForbiddenNames)
            AssertDoesNotContain(text, name);
    }

    private static string NewWorkspaceMissionSource() =>
        string.Join(Environment.NewLine,
            new[]
            {
                PathFor("src", "OneBrain.AgentOperations.Contracts", "NodalOsWorkspaceMissionContracts.cs"),
                PathFor("src", "OneBrain.AgentOperations.Core", "NodalOsWorkspaceMissionServices.cs")
            }.Select(File.ReadAllText));

    private static void AssertContains(string text, string expected) =>
        Assert.IsTrue(text.Contains(expected, StringComparison.OrdinalIgnoreCase), expected);

    private static void AssertDoesNotContain(string text, string unexpected) =>
        Assert.IsFalse(text.Contains(unexpected, StringComparison.Ordinal), unexpected);

    private static string PathFor(params string[] parts)
    {
        var root = AppContext.BaseDirectory;
        for (var i = 0; i < 10; i++)
        {
            var candidate = Path.Combine(new[] { root }.Concat(parts).ToArray());
            if (File.Exists(candidate) || Directory.Exists(candidate))
                return candidate;
            var parent = Directory.GetParent(root);
            if (parent is null)
                break;
            root = parent.FullName;
        }

        return Path.Combine(new[] { AppContext.BaseDirectory }.Concat(parts).ToArray());
    }
}
