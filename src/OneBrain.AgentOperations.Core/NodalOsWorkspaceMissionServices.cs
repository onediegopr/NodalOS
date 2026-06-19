using System.Text.Json;
using System.Text.Json.Serialization;
using OneBrain.AgentOperations.Contracts;

namespace OneBrain.AgentOperations.Core;

public sealed class NodalOsWorkspaceStorageMock
{
    private readonly Dictionary<string, NodalOsWorkspaceLocalModel> workspaces = new(StringComparer.Ordinal);
    private readonly HashSet<string> archived = new(StringComparer.Ordinal);
    private readonly NodalOsWorkspaceValidator workspaceValidator = new();
    private int invalidRejectedCount;

    public NodalOsWorkspaceStorageMockResult StoreDraft(NodalOsWorkspaceLocalModel workspace)
    {
        var errors = ValidateForStorage(workspace);
        if (workspaces.ContainsKey(workspace.WorkspaceId))
            errors.Add("Duplicate workspace id is rejected.");
        if (workspaces.Values.Any(existing => string.Equals(existing.DisplayNameRedacted, workspace.DisplayNameRedacted, StringComparison.OrdinalIgnoreCase)))
            errors.Add("Duplicate workspace display name is rejected.");

        if (errors.Count > 0)
        {
            invalidRejectedCount++;
            return Result(false, NodalOsWorkspaceStorageMockStatus.InvalidRejected, workspace, errors);
        }

        workspaces[workspace.WorkspaceId] = workspace with
        {
            Status = workspace.Status == NodalOsWorkspaceStatus.ActiveReadOnly
                ? NodalOsWorkspaceStatus.ActiveReadOnly
                : NodalOsWorkspaceStatus.Draft
        };

        return Result(true, StatusFor(workspaces[workspace.WorkspaceId]), workspaces[workspace.WorkspaceId], []);
    }

    public NodalOsWorkspaceStorageMockResult UpdateDraft(NodalOsWorkspaceLocalModel workspace)
    {
        var errors = ValidateForStorage(workspace);
        if (!workspaces.ContainsKey(workspace.WorkspaceId))
            errors.Add("Workspace id does not exist in mock storage.");
        if (workspaces.Values.Any(existing =>
            !string.Equals(existing.WorkspaceId, workspace.WorkspaceId, StringComparison.Ordinal) &&
            string.Equals(existing.DisplayNameRedacted, workspace.DisplayNameRedacted, StringComparison.OrdinalIgnoreCase)))
            errors.Add("Duplicate workspace display name is rejected.");

        if (errors.Count > 0)
        {
            invalidRejectedCount++;
            return Result(false, NodalOsWorkspaceStorageMockStatus.InvalidRejected, workspace, errors);
        }

        workspaces[workspace.WorkspaceId] = workspace;
        return Result(true, StatusFor(workspace), workspace, []);
    }

    public NodalOsWorkspaceLocalModel? GetById(string workspaceId) =>
        workspaces.TryGetValue(workspaceId, out var workspace) ? workspace : null;

    public IReadOnlyList<NodalOsWorkspaceLocalModel> ListWorkspaces() =>
        workspaces.Values.OrderBy(workspace => workspace.WorkspaceId, StringComparer.Ordinal).ToArray();

    public NodalOsWorkspaceStorageMockResult ArchiveWorkspaceMock(string workspaceId)
    {
        if (!workspaces.TryGetValue(workspaceId, out var workspace))
        {
            invalidRejectedCount++;
            return Result(false, NodalOsWorkspaceStorageMockStatus.InvalidRejected, null, ["Workspace id does not exist in mock storage."]);
        }

        var archivedWorkspace = workspace with { Status = NodalOsWorkspaceStatus.Archived };
        workspaces[workspaceId] = archivedWorkspace;
        archived.Add(workspaceId);
        return Result(true, NodalOsWorkspaceStorageMockStatus.ArchivedMock, archivedWorkspace, []);
    }

    public void Clear()
    {
        workspaces.Clear();
        archived.Clear();
        invalidRejectedCount = 0;
    }

    public NodalOsWorkspaceStorageMockSummary Summary()
    {
        var status =
            workspaces.Count == 0 ? NodalOsWorkspaceStorageMockStatus.Empty :
            workspaces.Values.Any(workspace => workspace.Status == NodalOsWorkspaceStatus.ActiveReadOnly) ? NodalOsWorkspaceStorageMockStatus.ActiveReadOnlyStored :
            workspaces.Values.Any(workspace => workspace.Status == NodalOsWorkspaceStatus.Draft) ? NodalOsWorkspaceStorageMockStatus.DraftStored :
            archived.Count > 0 ? NodalOsWorkspaceStorageMockStatus.ArchivedMock :
            NodalOsWorkspaceStorageMockStatus.Empty;

        return new()
        {
            StorageId = "workspace-storage-mock",
            Status = status,
            WorkspaceCount = workspaces.Count,
            ArchivedCount = archived.Count,
            InvalidRejectedCount = invalidRejectedCount,
            InMemoryOnly = true,
            FixtureSafe = true,
            ProductivePersistenceAllowed = false,
            RealFilesystemTouched = false,
            DatabaseUsed = false,
            CloudSyncAllowed = false,
            RuntimeExecutionAllowed = false,
            SensitiveValuesStored = false,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    private List<string> ValidateForStorage(NodalOsWorkspaceLocalModel workspace)
    {
        var result = workspaceValidator.ValidateWorkspace(workspace);
        return result.Errors.ToList();
    }

    private NodalOsWorkspaceStorageMockResult Result(
        bool accepted,
        NodalOsWorkspaceStorageMockStatus status,
        NodalOsWorkspaceLocalModel? workspace,
        IReadOnlyList<string> errors) => new()
    {
        Accepted = accepted,
        Status = status,
        Workspace = workspace,
        Errors = errors,
        Warnings = accepted ? ["In-memory fixture-safe storage only."] : [],
        Summary = Summary()
    };

    private static NodalOsWorkspaceStorageMockStatus StatusFor(NodalOsWorkspaceLocalModel workspace) =>
        workspace.Status == NodalOsWorkspaceStatus.ActiveReadOnly
            ? NodalOsWorkspaceStorageMockStatus.ActiveReadOnlyStored
            : NodalOsWorkspaceStorageMockStatus.DraftStored;
}

public sealed class NodalOsWorkspaceMissionBindingService
{
    public NodalOsWorkspaceMissionBinding CreateBinding(
        NodalOsWorkspaceLocalModel workspace,
        string missionId = "mission-local-preview")
    {
        var now = DateTimeOffset.UtcNow;
        return new()
        {
            BindingId = $"binding-{workspace.WorkspaceId}-{missionId}",
            WorkspaceId = workspace.WorkspaceId,
            MissionId = missionId,
            Status = NodalOsWorkspaceMissionBindingStatus.BoundReadOnly,
            MissionTitleRedacted = "Mission Control workspace preview",
            MissionSummaryRedacted = "Read-only mission binding for local workspace context.",
            ActiveTimelineRefs = workspace.TimelineRefs,
            ActiveApprovalRefs = ["approval-preview-local"],
            ActiveEvidenceRefs = workspace.EvidenceRefs,
            ObservabilityReportRefs = ["observability-local-preview"],
            UiStateRefs = string.IsNullOrWhiteSpace(workspace.UiStateRef) ? [] : [workspace.UiStateRef],
            PathJailBindingId = workspace.PathJailBindingId,
            AllowedCapabilitiesRedacted = ["preview workspace", "show timeline refs", "show evidence refs"],
            DisabledCapabilitiesRedacted = ["runtime execution", "TaskGraph creation", "LLM call", "filesystem access"],
            NextSafeStepsRedacted = ["Review workspace switcher.", "Keep Mission Control in read-only mode."],
            GuardrailSummaryRedacted = workspace.GuardrailSummaryRedacted,
            ReadOnlyPreview = true,
            CanAuthorizeExecution = false,
            RuntimeExecutionAllowed = false,
            CloudSyncAllowed = false,
            LlmProviderCallsAllowed = false,
            TaskGraphCreated = false,
            TouchesFilesystem = false,
            MutatesExecutionRegistryRuntime = false,
            CreatedAt = now,
            UpdatedAt = now
        };
    }
}

public sealed class NodalOsWorkspaceSwitcherService
{
    public NodalOsWorkspaceSwitcherListItem CreateItem(NodalOsWorkspaceLocalModel workspace, bool isActive = false)
    {
        var status =
            workspace.Status == NodalOsWorkspaceStatus.Archived ? NodalOsWorkspaceSwitcherItemStatus.Archived :
            workspace.Status == NodalOsWorkspaceStatus.Blocked ? NodalOsWorkspaceSwitcherItemStatus.Blocked :
            isActive ? NodalOsWorkspaceSwitcherItemStatus.ActiveReadOnly :
            NodalOsWorkspaceSwitcherItemStatus.AvailableReadOnly;

        return new()
        {
            WorkspaceId = workspace.WorkspaceId,
            DisplayNameRedacted = workspace.DisplayNameRedacted,
            Status = status,
            HealthSummaryRedacted = status == NodalOsWorkspaceSwitcherItemStatus.Blocked
                ? "Blocked until guardrails are resolved."
                : "Ready for read-only Mission Control preview.",
            PrivacyBadgeRedacted = "Local-first / private preview",
            PathJailStatusRedacted = string.IsNullOrWhiteSpace(workspace.PathJailBindingId)
                ? "Path jail binding pending."
                : "Path jail binding attached.",
            LastOpenedAtMock = DateTimeOffset.UtcNow,
            ActiveMissionCount = workspace.ActiveMissionRefs.Count,
            PendingApprovalCount = isActive ? 1 : 0,
            EvidenceCount = workspace.EvidenceRefs.Count,
            DisabledCapabilitiesSummaryRedacted = workspace.DisabledCapabilitiesRedacted,
            IsActive = isActive,
            IsArchived = status == NodalOsWorkspaceSwitcherItemStatus.Archived,
            IsBlocked = status == NodalOsWorkspaceSwitcherItemStatus.Blocked,
            ReadOnlyPreview = true,
            RuntimeExecutionAllowed = false,
            CanAuthorizeExecution = false
        };
    }

    public NodalOsWorkspaceSwitchIntent CreateSwitchIntent(
        string workspaceId,
        NodalOsWorkspaceSwitcherOptionKind optionKind = NodalOsWorkspaceSwitcherOptionKind.SelectWorkspace) => new()
    {
        IntentId = $"workspace-switch-intent-{workspaceId}-{optionKind}",
        WorkspaceId = workspaceId,
        OptionKind = optionKind,
        IsNoOp = true,
        CanAuthorizeExecution = false,
        RuntimeExecutionAllowed = false,
        CloudSyncAllowed = false,
        FilesystemAccessAllowed = false,
        CreatedAt = DateTimeOffset.UtcNow
    };

    public NodalOsWorkspaceSwitchResultPreview PreviewSwitch(
        IReadOnlyList<NodalOsWorkspaceSwitcherListItem> items,
        string selectedWorkspaceId,
        string activeWorkspaceId)
    {
        var selected = items.First(item => string.Equals(item.WorkspaceId, selectedWorkspaceId, StringComparison.Ordinal));
        return new()
        {
            ResultId = $"workspace-switch-preview-{selectedWorkspaceId}",
            SelectedWorkspaceId = selectedWorkspaceId,
            ActiveWorkspaceId = activeWorkspaceId,
            PreviewOnly = true,
            MockOnly = true,
            StateChangedProductively = false,
            CanAuthorizeExecution = false,
            RuntimeExecutionAllowed = false,
            SelectedItem = selected,
            WarningsRedacted = ["Switch preview is no-op and does not change productive state."],
            NextSafeStepsRedacted = ["Confirm read-only workspace context before future implementation."],
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public NodalOsWorkspaceSwitcherContract CreateSwitcher(
        IReadOnlyList<NodalOsWorkspaceLocalModel> workspaces,
        string activeWorkspaceId)
    {
        var items = workspaces
            .Select(workspace => CreateItem(workspace, string.Equals(workspace.WorkspaceId, activeWorkspaceId, StringComparison.Ordinal)))
            .ToArray();
        var selectedId = items.FirstOrDefault(item => item.IsActive)?.WorkspaceId ?? items.First().WorkspaceId;
        var intent = CreateSwitchIntent(selectedId);

        return new()
        {
            SwitcherId = "workspace-switcher-local-preview",
            ActiveWorkspaceId = activeWorkspaceId,
            Items = items,
            UserOptions =
            [
                NodalOsWorkspaceSwitcherOptionKind.SelectWorkspace,
                NodalOsWorkspaceSwitcherOptionKind.PreviewWorkspace,
                NodalOsWorkspaceSwitcherOptionKind.ArchiveWorkspaceMock,
                NodalOsWorkspaceSwitcherOptionKind.RequestExplanation,
                NodalOsWorkspaceSwitcherOptionKind.OpenGuardrails,
                NodalOsWorkspaceSwitcherOptionKind.NewWorkspaceDraft,
                NodalOsWorkspaceSwitcherOptionKind.ImportProjectWizardMock
            ],
            SwitchIntent = intent,
            SwitchResultPreview = PreviewSwitch(items, selectedId, activeWorkspaceId),
            StorageSummary = new NodalOsWorkspaceStorageMockSummary
            {
                StorageId = "workspace-storage-mock",
                Status = items.Any(item => item.IsActive)
                    ? NodalOsWorkspaceStorageMockStatus.ActiveReadOnlyStored
                    : NodalOsWorkspaceStorageMockStatus.DraftStored,
                WorkspaceCount = items.Length,
                ArchivedCount = items.Count(item => item.IsArchived),
                InvalidRejectedCount = 0,
                InMemoryOnly = true,
                FixtureSafe = true,
                ProductivePersistenceAllowed = false,
                RealFilesystemTouched = false,
                DatabaseUsed = false,
                CloudSyncAllowed = false,
                RuntimeExecutionAllowed = false,
                SensitiveValuesStored = false,
                CreatedAt = DateTimeOffset.UtcNow
            },
            ReadOnlyPreview = true,
            MockOnly = true,
            CloudSyncAllowed = false,
            RuntimeExecutionAllowed = false,
            FilesystemAccessAllowed = false,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }
}

public sealed class NodalOsWorkspaceMissionValidator
{
    private readonly NodalOsRedactionService redaction = new();
    private readonly NodalOsEvidenceRefBridge evidenceBridge = new();

    public NodalOsCoreRuntimeValidationResult ValidateStorageSummary(NodalOsWorkspaceStorageMockSummary summary)
    {
        var errors = new List<string>();
        AddRequired(errors, summary.StorageId, "StorageId is required.");
        if (!summary.InMemoryOnly || !summary.FixtureSafe)
            errors.Add("Storage mock must be in-memory and fixture-safe.");
        if (summary.ProductivePersistenceAllowed || summary.RealFilesystemTouched || summary.DatabaseUsed || summary.CloudSyncAllowed || summary.RuntimeExecutionAllowed || summary.SensitiveValuesStored)
            errors.Add("Storage mock cannot persist productively, touch filesystem, use DB/cloud/runtime, or store sensitive values.");
        return Result(errors);
    }

    public NodalOsCoreRuntimeValidationResult ValidateMissionBinding(NodalOsWorkspaceMissionBinding binding)
    {
        var errors = new List<string>();
        AddRequired(errors, binding.BindingId, "BindingId is required.");
        AddRequired(errors, binding.WorkspaceId, "WorkspaceId is required.");
        AddRequired(errors, binding.MissionId, "MissionId is required.");
        AddRequired(errors, binding.MissionTitleRedacted, "MissionTitleRedacted is required.");
        AddRequired(errors, binding.MissionSummaryRedacted, "MissionSummaryRedacted is required.");
        if (!binding.ReadOnlyPreview)
            errors.Add("Mission binding must remain read-only preview.");
        if (binding.CanAuthorizeExecution || binding.RuntimeExecutionAllowed || binding.CloudSyncAllowed || binding.LlmProviderCallsAllowed)
            errors.Add("Mission binding cannot authorize execution or enable runtime, cloud, or LLM calls.");
        if (binding.TaskGraphCreated || binding.TouchesFilesystem || binding.MutatesExecutionRegistryRuntime)
            errors.Add("Mission binding cannot create TaskGraph, touch filesystem, or mutate execution registry runtime.");
        ValidateSafeText(errors, "MissionTitleRedacted", binding.MissionTitleRedacted);
        ValidateSafeText(errors, "MissionSummaryRedacted", binding.MissionSummaryRedacted);
        ValidateSafeTextCollection(errors, "AllowedCapabilitiesRedacted", binding.AllowedCapabilitiesRedacted);
        ValidateSafeTextCollection(errors, "DisabledCapabilitiesRedacted", binding.DisabledCapabilitiesRedacted);
        ValidateSafeTextCollection(errors, "NextSafeStepsRedacted", binding.NextSafeStepsRedacted);
        ValidateSafeTextCollection(errors, "GuardrailSummaryRedacted", binding.GuardrailSummaryRedacted);
        foreach (var evidence in binding.ActiveEvidenceRefs)
        {
            var result = evidenceBridge.ValidateBridgeRef(evidence);
            if (!result.Accepted)
                errors.AddRange(result.Errors);
        }
        return Result(errors);
    }

    public NodalOsCoreRuntimeValidationResult ValidateSwitcher(NodalOsWorkspaceSwitcherContract switcher)
    {
        var errors = new List<string>();
        AddRequired(errors, switcher.SwitcherId, "SwitcherId is required.");
        AddRequired(errors, switcher.ActiveWorkspaceId, "ActiveWorkspaceId is required.");
        if (!switcher.ReadOnlyPreview || !switcher.MockOnly)
            errors.Add("Workspace switcher must remain read-only mock-only.");
        if (switcher.CloudSyncAllowed || switcher.RuntimeExecutionAllowed || switcher.FilesystemAccessAllowed)
            errors.Add("Workspace switcher cannot enable cloud, runtime, or filesystem access.");
        if (switcher.Items.Count == 0)
            errors.Add("Workspace switcher requires at least one item.");
        if (switcher.UserOptions.Count == 0)
            errors.Add("Workspace switcher requires user options.");
        errors.AddRange(ValidateSwitchIntent(switcher.SwitchIntent).Errors);
        errors.AddRange(ValidateSwitchResultPreview(switcher.SwitchResultPreview).Errors);
        errors.AddRange(ValidateStorageSummary(switcher.StorageSummary).Errors);
        foreach (var item in switcher.Items)
            errors.AddRange(ValidateSwitcherItem(item).Errors);
        return Result(errors);
    }

    public NodalOsCoreRuntimeValidationResult ValidateSwitcherItem(NodalOsWorkspaceSwitcherListItem item)
    {
        var errors = new List<string>();
        AddRequired(errors, item.WorkspaceId, "WorkspaceId is required.");
        AddRequired(errors, item.DisplayNameRedacted, "DisplayNameRedacted is required.");
        AddRequired(errors, item.HealthSummaryRedacted, "HealthSummaryRedacted is required.");
        AddRequired(errors, item.PrivacyBadgeRedacted, "PrivacyBadgeRedacted is required.");
        AddRequired(errors, item.PathJailStatusRedacted, "PathJailStatusRedacted is required.");
        if (!item.ReadOnlyPreview || item.RuntimeExecutionAllowed || item.CanAuthorizeExecution)
            errors.Add("Workspace switcher item must remain read-only and non-authoritative.");
        ValidateSafeText(errors, "DisplayNameRedacted", item.DisplayNameRedacted);
        ValidateSafeText(errors, "HealthSummaryRedacted", item.HealthSummaryRedacted);
        ValidateSafeText(errors, "PrivacyBadgeRedacted", item.PrivacyBadgeRedacted);
        ValidateSafeText(errors, "PathJailStatusRedacted", item.PathJailStatusRedacted);
        ValidateSafeTextCollection(errors, "DisabledCapabilitiesSummaryRedacted", item.DisabledCapabilitiesSummaryRedacted);
        return Result(errors);
    }

    public NodalOsCoreRuntimeValidationResult ValidateSwitchIntent(NodalOsWorkspaceSwitchIntent intent)
    {
        var errors = new List<string>();
        AddRequired(errors, intent.IntentId, "IntentId is required.");
        AddRequired(errors, intent.WorkspaceId, "WorkspaceId is required.");
        if (!intent.IsNoOp)
            errors.Add("Workspace switch intent must be no-op.");
        if (intent.CanAuthorizeExecution || intent.RuntimeExecutionAllowed || intent.CloudSyncAllowed || intent.FilesystemAccessAllowed)
            errors.Add("Workspace switch intent cannot authorize execution or access runtime, cloud, or filesystem.");
        return Result(errors);
    }

    public NodalOsCoreRuntimeValidationResult ValidateSwitchResultPreview(NodalOsWorkspaceSwitchResultPreview preview)
    {
        var errors = new List<string>();
        AddRequired(errors, preview.ResultId, "ResultId is required.");
        AddRequired(errors, preview.SelectedWorkspaceId, "SelectedWorkspaceId is required.");
        AddRequired(errors, preview.ActiveWorkspaceId, "ActiveWorkspaceId is required.");
        if (!preview.PreviewOnly || !preview.MockOnly)
            errors.Add("Workspace switch result must remain preview/mock-only.");
        if (preview.StateChangedProductively || preview.CanAuthorizeExecution || preview.RuntimeExecutionAllowed)
            errors.Add("Workspace switch result cannot change productive state or authorize runtime.");
        ValidateSafeTextCollection(errors, "WarningsRedacted", preview.WarningsRedacted);
        ValidateSafeTextCollection(errors, "NextSafeStepsRedacted", preview.NextSafeStepsRedacted);
        errors.AddRange(ValidateSwitcherItem(preview.SelectedItem).Errors);
        return Result(errors);
    }

    private void ValidateSafeText(List<string> errors, string fieldName, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return;
        if (redaction.ContainsSensitiveField(fieldName, value) || redaction.ContainsSensitiveContent(value))
            errors.Add($"{fieldName} contains sensitive content.");
    }

    private void ValidateSafeTextCollection(List<string> errors, string fieldName, IEnumerable<string> values)
    {
        foreach (var value in values)
            ValidateSafeText(errors, fieldName, value);
    }

    private static void AddRequired(List<string> errors, string? value, string message)
    {
        if (string.IsNullOrWhiteSpace(value))
            errors.Add(message);
    }

    private static NodalOsCoreRuntimeValidationResult Result(List<string> errors) => new()
    {
        IsValid = errors.Count == 0,
        RuntimeExecutionAllowed = false,
        RuntimeExecutionDeferred = true,
        RequiresGlobalPolicyEvaluation = true,
        RequiresEvidenceRedaction = true,
        Errors = errors,
        Warnings = []
    };
}

public sealed class NodalOsWorkspaceMissionJsonSerializer
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
        WriteIndented = true
    };

    public string SerializeStorageSummary(NodalOsWorkspaceStorageMockSummary summary) => JsonSerializer.Serialize(summary, Options);
    public string SerializeStorageResult(NodalOsWorkspaceStorageMockResult result) => JsonSerializer.Serialize(result, Options);
    public string SerializeMissionBinding(NodalOsWorkspaceMissionBinding binding) => JsonSerializer.Serialize(binding, Options);
    public string SerializeSwitcherItem(NodalOsWorkspaceSwitcherListItem item) => JsonSerializer.Serialize(item, Options);
    public string SerializeSwitchIntent(NodalOsWorkspaceSwitchIntent intent) => JsonSerializer.Serialize(intent, Options);
    public string SerializeSwitcher(NodalOsWorkspaceSwitcherContract switcher) => JsonSerializer.Serialize(switcher, Options);
}

public static class NodalOsWorkspaceMissionFixtures
{
    public static NodalOsWorkspaceStorageMock StorageWithDraftAndActive()
    {
        var workspaceService = new NodalOsWorkspaceService();
        var storage = new NodalOsWorkspaceStorageMock();
        storage.StoreDraft(workspaceService.CreateWorkspaceDraft("workspace-local-draft"));
        storage.StoreDraft(workspaceService.CreateActiveReadOnlyWorkspace() with { DisplayNameRedacted = "Local Workspace Active Preview" });
        return storage;
    }

    public static NodalOsWorkspaceMissionBinding MissionBinding() =>
        new NodalOsWorkspaceMissionBindingService().CreateBinding(NodalOsWorkspaceFixtures.ActiveReadOnlyWorkspace());

    public static NodalOsWorkspaceSwitcherContract WorkspaceSwitcher()
    {
        var storage = StorageWithDraftAndActive();
        return new NodalOsWorkspaceSwitcherService()
            .CreateSwitcher(storage.ListWorkspaces(), "workspace-local-active");
    }
}
