using System.Text.Json;
using System.Text.Json.Serialization;
using OneBrain.AgentOperations.Contracts;

namespace OneBrain.AgentOperations.Core;

public sealed class NodalOsWorkspaceMetadataService
{
    private readonly NodalOsWorkspaceMissionBindingService bindingService = new();

    public NodalOsWorkspaceMetadataIndexMock CreateEmptyIndex(string workspaceId = "workspace-local-active") => new()
    {
        IndexId = $"metadata-index-{workspaceId}",
        WorkspaceId = workspaceId,
        Status = NodalOsWorkspaceMetadataIndexStatus.Empty,
        SourceType = NodalOsWorkspaceMetadataSourceType.Mock,
        RedactionSummaryRedacted = ["No workspace metadata values retained yet."],
        GuardrailSummaryRedacted = ["No scan.", "No content access.", "No runtime."],
        DisabledCapabilitiesRedacted = ["scan workspace", "enumerate folders", "read content", "run commands", "call provider"],
        MockOnly = true,
        IsSourceOfTruthForExecution = false,
        RealFilesystemScanAllowed = false,
        DirectoryEnumerationAllowed = false,
        FileContentAccessAllowed = false,
        FileFingerprintingAllowed = false,
        ShellCommandAllowed = false,
        LlmProviderCallsAllowed = false,
        CloudSyncAllowed = false,
        VectorIndexAllowed = false,
        ProductivePersistenceAllowed = false,
        GeneratedAt = DateTimeOffset.UtcNow
    };

    public NodalOsWorkspaceMetadataIndexMock CreateMockIndex(NodalOsWorkspaceLocalModel workspace) => new()
    {
        IndexId = $"metadata-index-{workspace.WorkspaceId}",
        WorkspaceId = workspace.WorkspaceId,
        Status = NodalOsWorkspaceMetadataIndexStatus.MockIndexed,
        IndexedItemRefsMock = ["item-ref-docs-overview", "item-ref-safe-config-summary", "item-ref-mission-notes"],
        ProjectTypeHintsMock = ["desktop-local-first", "mission-control-preview"],
        ItemCategorySummariesMock = ["docs refs: 1", "config refs: 1", "mission notes refs: 1"],
        TechnologyHintsMock = ["dotnet workspace hint from user-provided metadata"],
        DocumentationHintsMock = ["README ref expected later", "architecture refs expected later"],
        RiskHintsMock = ["runtime disabled", "filesystem scan deferred", "cloud disabled"],
        KnownMissionRefs = workspace.ActiveMissionRefs,
        KnownWorkspaceRefs = [workspace.WorkspaceId],
        EvidenceRefs = workspace.EvidenceRefs,
        TimelineRefs = workspace.TimelineRefs,
        SourceType = NodalOsWorkspaceMetadataSourceType.Mock,
        RedactionSummaryRedacted = ["Metadata values are fixture-safe and redacted."],
        GuardrailSummaryRedacted = workspace.GuardrailSummaryRedacted,
        DisabledCapabilitiesRedacted = workspace.DisabledCapabilitiesRedacted,
        MockOnly = true,
        IsSourceOfTruthForExecution = false,
        RealFilesystemScanAllowed = false,
        DirectoryEnumerationAllowed = false,
        FileContentAccessAllowed = false,
        FileFingerprintingAllowed = false,
        ShellCommandAllowed = false,
        LlmProviderCallsAllowed = false,
        CloudSyncAllowed = false,
        VectorIndexAllowed = false,
        ProductivePersistenceAllowed = false,
        GeneratedAt = DateTimeOffset.UtcNow
    };

    public NodalOsWorkspaceMetadataIndexMock CreateRequiresRealScanLaterIndex(NodalOsWorkspaceLocalModel workspace) =>
        CreateMockIndex(workspace) with
        {
            Status = NodalOsWorkspaceMetadataIndexStatus.RequiresRealScanLater,
            GuardrailSummaryRedacted = ["Real project metadata requires future approved workspace context boundary."],
            DisabledCapabilitiesRedacted = ["real scan", "content access", "folder enumeration", "provider inference"]
        };

    public NodalOsSafeProjectSummaryContract CreateSafeProjectSummary(
        NodalOsWorkspaceLocalModel workspace,
        NodalOsWorkspaceMetadataIndexMock index,
        NodalOsWorkspaceMissionBinding? binding = null)
    {
        binding ??= bindingService.CreateBinding(workspace);
        return new()
        {
            ProjectSummaryId = $"safe-project-summary-{workspace.WorkspaceId}",
            WorkspaceId = workspace.WorkspaceId,
            MissionIds = binding.MissionId.Length == 0 ? [] : [binding.MissionId],
            TitleRedacted = workspace.DisplayNameRedacted,
            ShortSummaryRedacted = "Resumen basado en metadata segura/mock, no en escaneo real.",
            StatusRedacted = "Project understanding real aun no esta habilitado.",
            ProjectTypeHintsRedacted = index.ProjectTypeHintsMock,
            RiskSummaryRedacted = index.RiskHintsMock.Count == 0 ? ["Runtime and filesystem access remain disabled."] : index.RiskHintsMock,
            ReadinessSummaryRedacted = ["Workspace preview is ready for safe display.", "Runtime remains unavailable by design."],
            MissingInformationRedacted = ["Real source tree metadata is not available.", "No file contents were inspected."],
            DisabledCapabilitiesRedacted = index.DisabledCapabilitiesRedacted,
            NextSafeStepsRedacted = ["Review workspace health report.", "Keep project summary as mock-safe until context boundary exists."],
            EvidenceRefs = index.EvidenceRefs,
            TimelineRefs = index.TimelineRefs,
            ObservabilityRefs = binding.ObservabilityReportRefs,
            WorkspaceHealthRef = $"workspace-health-{workspace.WorkspaceId}",
            RedactionSummaryRedacted = index.RedactionSummaryRedacted,
            Confidence = NodalOsProjectSummaryConfidence.Mock,
            BasisDisclosureRedacted = "Resumen basado en metadata segura/mock, no en escaneo real.",
            ProjectKnowledgeDisclosureRedacted = "Project understanding real aun no esta habilitado.",
            NoContentAccessDisclosureRedacted = "No se leyo ningun archivo.",
            SafeToDisplay = true,
            CanAuthorizeExecution = false,
            RuntimeExecutionAllowed = false,
            RealFilesystemScanAllowed = false,
            LlmProviderCallsAllowed = false,
            CloudSyncAllowed = false,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public NodalOsWorkspaceHealthReport CreateHealthReport(
        NodalOsWorkspaceLocalModel workspace,
        NodalOsWorkspaceMetadataIndexMock index,
        NodalOsWorkspaceMissionBinding binding,
        NodalOsWorkspaceHealthStatus status = NodalOsWorkspaceHealthStatus.HealthyMock)
    {
        var requiresAttention = status is not NodalOsWorkspaceHealthStatus.HealthyMock;
        return new()
        {
            HealthReportId = $"workspace-health-{workspace.WorkspaceId}-{status}",
            WorkspaceId = workspace.WorkspaceId,
            HealthStatus = status,
            PathJailStatusRedacted = string.IsNullOrWhiteSpace(workspace.PathJailBindingId)
                ? "Path jail validation needed."
                : "Path jail binding present.",
            MetadataIndexStatusRedacted = index.Status.ToString(),
            MissionBindingStatusRedacted = binding.Status.ToString(),
            UiStateStatusRedacted = string.IsNullOrWhiteSpace(workspace.UiStateRef)
                ? "UI state preview missing."
                : "UI state preview linked.",
            EvidenceTimelineStatusRedacted = $"Evidence refs: {workspace.EvidenceRefs.Count}; timeline refs: {workspace.TimelineRefs.Count}.",
            DisabledCapabilitiesRedacted = workspace.DisabledCapabilitiesRedacted,
            BlockersRedacted = BlockersFor(status),
            WarningsRedacted = requiresAttention ? ["Human review may be needed before continuing."] : ["Mock health only; no real scan performed."],
            NextSafeStepsRedacted = NextStepsFor(status),
            GuardrailRefs = ["no-runtime", "no-filesystem-scan", "no-cloud", "no-llm", "evidence-ref-only"],
            RedactionSummaryRedacted = ["Health report uses redacted refs and mock-safe summaries."],
            RequiresAction = requiresAttention,
            RequiresHumanAttention = status is NodalOsWorkspaceHealthStatus.BlockedByGuardrail
                or NodalOsWorkspaceHealthStatus.BlockedByRuntimeGate
                or NodalOsWorkspaceHealthStatus.BlockedByCloudQuarantine,
            ReadOnlyReport = true,
            MutatesState = false,
            CanAuthorizeExecution = false,
            RuntimeExecutionAllowed = false,
            RealFilesystemScanAllowed = false,
            LlmProviderCallsAllowed = false,
            CloudSyncAllowed = false,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    private static IReadOnlyList<string> BlockersFor(NodalOsWorkspaceHealthStatus status) => status switch
    {
        NodalOsWorkspaceHealthStatus.BlockedByGuardrail => ["Blocked by active guardrail."],
        NodalOsWorkspaceHealthStatus.BlockedByRuntimeGate => ["Positive execution gate is not implemented."],
        NodalOsWorkspaceHealthStatus.BlockedByCloudQuarantine => ["Cloud remains blocked by legacy quarantine plan."],
        NodalOsWorkspaceHealthStatus.NeedsWorkspaceValidation => ["Workspace validation is required."],
        NodalOsWorkspaceHealthStatus.NeedsPathJailValidation => ["Path jail validation is required."],
        NodalOsWorkspaceHealthStatus.NeedsMetadata => ["Metadata index is required."],
        NodalOsWorkspaceHealthStatus.Unknown => ["Workspace health is unknown."],
        _ => []
    };

    private static IReadOnlyList<string> NextStepsFor(NodalOsWorkspaceHealthStatus status) => status switch
    {
        NodalOsWorkspaceHealthStatus.HealthyMock => ["Continue with safe context boundary planning."],
        NodalOsWorkspaceHealthStatus.NeedsWorkspaceValidation => ["Validate workspace contract."],
        NodalOsWorkspaceHealthStatus.NeedsPathJailValidation => ["Validate path jail contract."],
        NodalOsWorkspaceHealthStatus.NeedsMetadata => ["Create mock metadata index."],
        NodalOsWorkspaceHealthStatus.BlockedByGuardrail => ["Review guardrail explainers."],
        NodalOsWorkspaceHealthStatus.BlockedByRuntimeGate => ["Plan positive execution authorization gate before runtime."],
        NodalOsWorkspaceHealthStatus.BlockedByCloudQuarantine => ["Resolve legacy quarantine before cloud/licensing/BYOK."],
        _ => ["Request explanation."]
    };
}

public sealed class NodalOsWorkspaceMetadataValidator
{
    private readonly NodalOsRedactionService redaction = new();
    private readonly NodalOsEvidenceRefBridge evidenceBridge = new();

    public NodalOsCoreRuntimeValidationResult ValidateMetadataIndex(NodalOsWorkspaceMetadataIndexMock index)
    {
        var errors = new List<string>();
        AddRequired(errors, index.IndexId, "IndexId is required.");
        AddRequired(errors, index.WorkspaceId, "WorkspaceId is required.");
        if (!index.MockOnly)
            errors.Add("Metadata index must remain mock-only.");
        if (index.IsSourceOfTruthForExecution)
            errors.Add("Metadata index cannot be source of truth for execution.");
        if (index.RealFilesystemScanAllowed || index.DirectoryEnumerationAllowed || index.FileContentAccessAllowed || index.FileFingerprintingAllowed)
            errors.Add("Metadata index cannot inspect real workspace content.");
        if (index.ShellCommandAllowed || index.LlmProviderCallsAllowed || index.CloudSyncAllowed || index.VectorIndexAllowed || index.ProductivePersistenceAllowed)
            errors.Add("Metadata index cannot use shell, provider calls, cloud, vector index, or productive persistence.");
        ValidateTextCollection(errors, "IndexedItemRefsMock", index.IndexedItemRefsMock);
        ValidateTextCollection(errors, "ProjectTypeHintsMock", index.ProjectTypeHintsMock);
        ValidateTextCollection(errors, "ItemCategorySummariesMock", index.ItemCategorySummariesMock);
        ValidateTextCollection(errors, "TechnologyHintsMock", index.TechnologyHintsMock);
        ValidateTextCollection(errors, "DocumentationHintsMock", index.DocumentationHintsMock);
        ValidateTextCollection(errors, "RiskHintsMock", index.RiskHintsMock);
        ValidateTextCollection(errors, "RedactionSummaryRedacted", index.RedactionSummaryRedacted);
        ValidateTextCollection(errors, "GuardrailSummaryRedacted", index.GuardrailSummaryRedacted);
        ValidateTextCollection(errors, "DisabledCapabilitiesRedacted", index.DisabledCapabilitiesRedacted);
        foreach (var evidence in index.EvidenceRefs)
        {
            var result = evidenceBridge.ValidateBridgeRef(evidence);
            if (!result.Accepted)
                errors.AddRange(result.Errors);
        }
        return Result(errors);
    }

    public NodalOsCoreRuntimeValidationResult ValidateProjectSummary(NodalOsSafeProjectSummaryContract summary)
    {
        var errors = new List<string>();
        AddRequired(errors, summary.ProjectSummaryId, "ProjectSummaryId is required.");
        AddRequired(errors, summary.WorkspaceId, "WorkspaceId is required.");
        AddRequired(errors, summary.TitleRedacted, "TitleRedacted is required.");
        AddRequired(errors, summary.ShortSummaryRedacted, "ShortSummaryRedacted is required.");
        AddRequired(errors, summary.BasisDisclosureRedacted, "BasisDisclosureRedacted is required.");
        AddRequired(errors, summary.ProjectKnowledgeDisclosureRedacted, "ProjectKnowledgeDisclosureRedacted is required.");
        AddRequired(errors, summary.NoContentAccessDisclosureRedacted, "NoContentAccessDisclosureRedacted is required.");
        if (!summary.SafeToDisplay)
            errors.Add("Project summary must be safe to display.");
        if (summary.CanAuthorizeExecution || summary.RuntimeExecutionAllowed || summary.RealFilesystemScanAllowed || summary.LlmProviderCallsAllowed || summary.CloudSyncAllowed)
            errors.Add("Project summary cannot authorize execution or enable runtime, scan, provider calls, or cloud.");
        ValidateText(errors, "TitleRedacted", summary.TitleRedacted);
        ValidateText(errors, "ShortSummaryRedacted", summary.ShortSummaryRedacted);
        ValidateText(errors, "StatusRedacted", summary.StatusRedacted);
        ValidateText(errors, "BasisDisclosureRedacted", summary.BasisDisclosureRedacted);
        ValidateText(errors, "ProjectKnowledgeDisclosureRedacted", summary.ProjectKnowledgeDisclosureRedacted);
        ValidateText(errors, "NoContentAccessDisclosureRedacted", summary.NoContentAccessDisclosureRedacted);
        ValidateTextCollection(errors, "RiskSummaryRedacted", summary.RiskSummaryRedacted);
        ValidateTextCollection(errors, "ReadinessSummaryRedacted", summary.ReadinessSummaryRedacted);
        ValidateTextCollection(errors, "MissingInformationRedacted", summary.MissingInformationRedacted);
        ValidateTextCollection(errors, "DisabledCapabilitiesRedacted", summary.DisabledCapabilitiesRedacted);
        ValidateTextCollection(errors, "NextSafeStepsRedacted", summary.NextSafeStepsRedacted);
        foreach (var evidence in summary.EvidenceRefs)
        {
            var result = evidenceBridge.ValidateBridgeRef(evidence);
            if (!result.Accepted)
                errors.AddRange(result.Errors);
        }
        return Result(errors);
    }

    public NodalOsCoreRuntimeValidationResult ValidateHealthReport(NodalOsWorkspaceHealthReport report)
    {
        var errors = new List<string>();
        AddRequired(errors, report.HealthReportId, "HealthReportId is required.");
        AddRequired(errors, report.WorkspaceId, "WorkspaceId is required.");
        AddRequired(errors, report.PathJailStatusRedacted, "PathJailStatusRedacted is required.");
        AddRequired(errors, report.MetadataIndexStatusRedacted, "MetadataIndexStatusRedacted is required.");
        AddRequired(errors, report.MissionBindingStatusRedacted, "MissionBindingStatusRedacted is required.");
        if (!report.ReadOnlyReport)
            errors.Add("Workspace health report must remain read-only.");
        if (report.MutatesState || report.CanAuthorizeExecution || report.RuntimeExecutionAllowed || report.RealFilesystemScanAllowed || report.LlmProviderCallsAllowed || report.CloudSyncAllowed)
            errors.Add("Workspace health report cannot mutate state or enable execution, scan, provider calls, or cloud.");
        ValidateText(errors, "PathJailStatusRedacted", report.PathJailStatusRedacted);
        ValidateText(errors, "MetadataIndexStatusRedacted", report.MetadataIndexStatusRedacted);
        ValidateText(errors, "MissionBindingStatusRedacted", report.MissionBindingStatusRedacted);
        ValidateTextCollection(errors, "BlockersRedacted", report.BlockersRedacted);
        ValidateTextCollection(errors, "WarningsRedacted", report.WarningsRedacted);
        ValidateTextCollection(errors, "NextSafeStepsRedacted", report.NextSafeStepsRedacted);
        ValidateTextCollection(errors, "RedactionSummaryRedacted", report.RedactionSummaryRedacted);
        return Result(errors);
    }

    private void ValidateTextCollection(List<string> errors, string fieldName, IEnumerable<string> values)
    {
        foreach (var value in values)
            ValidateText(errors, fieldName, value);
    }

    private void ValidateText(List<string> errors, string fieldName, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return;
        if (redaction.ContainsSensitiveField(fieldName, value) || redaction.ContainsSensitiveContent(value))
            errors.Add($"{fieldName} contains sensitive content.");
        if (value.Contains(':') && value.Contains('\\'))
            errors.Add($"{fieldName} appears to contain a raw path.");
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

public sealed class NodalOsWorkspaceMetadataJsonSerializer
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
        WriteIndented = true
    };

    public string SerializeMetadataIndex(NodalOsWorkspaceMetadataIndexMock index) => JsonSerializer.Serialize(index, Options);
    public string SerializeProjectSummary(NodalOsSafeProjectSummaryContract summary) => JsonSerializer.Serialize(summary, Options);
    public string SerializeHealthReport(NodalOsWorkspaceHealthReport report) => JsonSerializer.Serialize(report, Options);
}

public static class NodalOsWorkspaceMetadataFixtures
{
    public static NodalOsWorkspaceMetadataIndexMock MockIndex()
    {
        var workspace = NodalOsWorkspaceFixtures.ActiveReadOnlyWorkspace();
        return new NodalOsWorkspaceMetadataService().CreateMockIndex(workspace);
    }

    public static NodalOsSafeProjectSummaryContract ProjectSummary()
    {
        var workspace = NodalOsWorkspaceFixtures.ActiveReadOnlyWorkspace();
        var service = new NodalOsWorkspaceMetadataService();
        return service.CreateSafeProjectSummary(workspace, service.CreateMockIndex(workspace));
    }

    public static NodalOsWorkspaceHealthReport HealthReport(NodalOsWorkspaceHealthStatus status = NodalOsWorkspaceHealthStatus.HealthyMock)
    {
        var workspace = NodalOsWorkspaceFixtures.ActiveReadOnlyWorkspace();
        var metadataService = new NodalOsWorkspaceMetadataService();
        var binding = new NodalOsWorkspaceMissionBindingService().CreateBinding(workspace);
        return metadataService.CreateHealthReport(workspace, metadataService.CreateMockIndex(workspace), binding, status);
    }
}
