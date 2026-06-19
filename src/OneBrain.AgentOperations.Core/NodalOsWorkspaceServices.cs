using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using OneBrain.AgentOperations.Contracts;

namespace OneBrain.AgentOperations.Core;

public sealed class NodalOsWorkspaceValidator
{
    private readonly NodalOsRedactionService redaction = new();
    private readonly NodalOsEvidenceRefBridge evidenceBridge = new();

    public NodalOsCoreRuntimeValidationResult ValidateWorkspace(NodalOsWorkspaceLocalModel workspace)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        AddRequired(errors, workspace.WorkspaceId, "WorkspaceId is required.");
        AddRequired(errors, workspace.DisplayNameRedacted, "DisplayNameRedacted is required.");
        AddRequired(errors, workspace.LocalRootPathRedacted, "LocalRootPathRedacted is required.");
        AddRequired(errors, workspace.RootPathFingerprint, "RootPathFingerprint is required.");
        if (workspace.CreatedAt == default || workspace.UpdatedAt == default)
            errors.Add("CreatedAt and UpdatedAt are required.");
        if (!workspace.ReadOnlyPreview)
            errors.Add("Workspace model must remain read-only preview.");
        if (workspace.RuntimeExecutionAllowed)
            errors.Add("Workspace model cannot enable runtime execution.");
        if (workspace.CloudSyncAllowed)
            errors.Add("Workspace model cannot enable cloud sync.");
        if (workspace.LlmProviderCallsAllowed)
            errors.Add("Workspace model cannot enable LLM provider calls.");
        if (workspace.CanAuthorizeExecution)
            errors.Add("Workspace model cannot authorize execution.");

        ValidateSafeText(errors, "DisplayNameRedacted", workspace.DisplayNameRedacted);
        ValidateSafeText(errors, "DescriptionRedacted", workspace.DescriptionRedacted);
        ValidateSafeText(errors, "LocalRootPathRedacted", workspace.LocalRootPathRedacted);
        ValidateSafeTextCollection(errors, "GuardrailSummaryRedacted", workspace.GuardrailSummaryRedacted);
        ValidateSafeTextCollection(errors, "AllowedCapabilitiesRedacted", workspace.AllowedCapabilitiesRedacted);
        ValidateSafeTextCollection(errors, "DisabledCapabilitiesRedacted", workspace.DisabledCapabilitiesRedacted);
        ValidateSafeTextCollection(errors, "NextSafeStepsRedacted", workspace.NextSafeStepsRedacted);

        foreach (var evidence in workspace.EvidenceRefs)
        {
            var result = evidenceBridge.ValidateBridgeRef(evidence);
            if (!result.Accepted)
                errors.AddRange(result.Errors);
        }

        return Result(errors, warnings);
    }

    public NodalOsCoreRuntimeValidationResult ValidatePathJailBinding(NodalOsPathJailBinding binding)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        AddRequired(errors, binding.JailId, "JailId is required.");
        AddRequired(errors, binding.WorkspaceId, "WorkspaceId is required.");
        AddRequired(errors, binding.RootPathRedacted, "RootPathRedacted is required.");
        AddRequired(errors, binding.CanonicalRootFingerprint, "CanonicalRootFingerprint is required.");
        if (binding.CreatedAt == default)
            errors.Add("CreatedAt is required.");
        if (binding.CanMutateFilesystem)
            errors.Add("Path jail binding cannot mutate filesystem in M492-M494.");
        if (binding.CanExecuteShell)
            errors.Add("Path jail binding cannot execute shell.");
        if (binding.CanAccessOutsideJail)
            errors.Add("Path jail binding cannot access outside jail.");
        if (!binding.RequiresPositiveExecutionGate)
            errors.Add("Future path operations must require positive execution gate.");
        if (!binding.RequiresApprovalForFutureMutations)
            errors.Add("Future path mutations must require approval.");
        if (binding.RealFilesystemAccessAllowed)
            errors.Add("Path jail binding must not access real filesystem in M492-M494.");

        ValidateSafeText(errors, "RootPathRedacted", binding.RootPathRedacted);
        ValidateSafeText(errors, "SymlinkPolicyRedacted", binding.SymlinkPolicyRedacted);
        ValidateSafeText(errors, "CaseSensitivityNoteRedacted", binding.CaseSensitivityNoteRedacted);
        ValidateSafeTextCollection(errors, "AllowedPathPolicyRedacted", binding.AllowedPathPolicyRedacted);
        ValidateSafeTextCollection(errors, "DeniedPathPolicyRedacted", binding.DeniedPathPolicyRedacted);
        ValidateSafeTextCollection(errors, "DisabledOperationsRedacted", binding.DisabledOperationsRedacted);

        return Result(errors, warnings);
    }

    public NodalOsPathJailValidationResult ValidateRelativePath(NodalOsPathJailBinding binding, string? relativePathRedacted)
    {
        var errors = new List<string>();
        var warnings = new List<string>();
        var path = relativePathRedacted ?? string.Empty;

        if (string.IsNullOrWhiteSpace(path))
            errors.Add("Relative path is required.");
        if (path.StartsWith(@"\\", StringComparison.Ordinal) || path.StartsWith("//", StringComparison.Ordinal))
            errors.Add("UNC paths are rejected.");
        if (path.Length > 1 && path[1] == ':')
            errors.Add("Windows drive-qualified paths are rejected.");
        if (path.StartsWith("/", StringComparison.Ordinal) || path.StartsWith("\\", StringComparison.Ordinal))
            errors.Add("Absolute paths are rejected.");
        if (path.Contains("..", StringComparison.Ordinal) || Uri.UnescapeDataString(path).Contains("..", StringComparison.Ordinal))
            errors.Add("Path traversal is rejected.");
        if (path.Contains('\\') && path.Contains('/'))
            errors.Add("Mixed separator traversal is rejected.");
        if (redaction.ContainsSensitiveContent(path) || ContainsSensitivePathMarker(path))
            errors.Add("Path contains sensitive token-like content.");

        ValidateSafeText(errors, "RelativePathRedacted", path);
        return new()
        {
            IsValid = errors.Count == 0,
            IsInsideJail = errors.Count == 0,
            FilesystemTouched = false,
            CanMutateFilesystem = false,
            Errors = errors,
            Warnings = warnings
        };
    }

    public NodalOsCoreRuntimeValidationResult ValidateImportWizard(NodalOsProjectImportWizardContract wizard)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        AddRequired(errors, wizard.WizardId, "WizardId is required.");
        AddRequired(errors, wizard.SelectedPathRedacted, "SelectedPathRedacted is required.");
        if (wizard.CurrentStep != NodalOsProjectImportWizardStepKind.ChooseLocalFolder)
            warnings.Add("Wizard fixture is expected to start at choose local folder.");
        if (wizard.CreatedAt == default)
            errors.Add("CreatedAt is required.");
        if (!wizard.ReadOnlyPreview || !wizard.ProjectImportMockOnly)
            errors.Add("Project import wizard must remain read-only mock-only.");
        if (wizard.ScansFilesystem || wizard.CreatesFolders || wizard.ImportsFiles || wizard.ProductivePersistenceAllowed)
            errors.Add("Project import wizard cannot scan, create folders, import files or persist productively.");
        if (wizard.RuntimeExecutionAllowed || wizard.CloudSyncAllowed || wizard.LlmProviderCallsAllowed)
            errors.Add("Project import wizard cannot enable runtime, cloud or LLM calls.");
        if (wizard.Steps.Count < 8)
            errors.Add("Project import wizard must define the expected eight-step flow.");
        if (wizard.UserOptions.Count == 0)
            errors.Add("Project import wizard must expose user options.");
        if (wizard.NoOpIntents.Any(intent => !intent.IsNoOp || intent.CanAuthorizeExecution || intent.RuntimeExecutionAllowed))
            errors.Add("Project import wizard intents must remain no-op and non-authoritative.");

        ValidateSafeText(errors, "SelectedPathRedacted", wizard.SelectedPathRedacted);
        ValidateSafeTextCollection(errors, "WarningsRedacted", wizard.WarningsRedacted);
        ValidateSafeTextCollection(errors, "BlockersRedacted", wizard.BlockersRedacted);
        ValidateSafeTextCollection(errors, "DisabledFutureOptionsRedacted", wizard.DisabledFutureOptionsRedacted);
        ValidateSafeTextCollection(errors, "GuardrailExplainersRedacted", wizard.GuardrailExplainersRedacted);
        errors.AddRange(ValidateWorkspace(wizard.WorkspaceDraft).Errors);

        foreach (var step in wizard.Steps)
        {
            ValidateSafeText(errors, "StepTitleRedacted", step.TitleRedacted);
            ValidateSafeText(errors, "StepExplanationRedacted", step.ExplanationRedacted);
            if (!step.IsNoOp || step.RuntimeExecutionAllowed || step.FilesystemAccessAllowed)
                errors.Add("Wizard steps must be no-op without runtime or filesystem access.");
        }

        return Result(errors, warnings);
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

    private static bool ContainsSensitivePathMarker(string path)
    {
        var lower = path.ToLowerInvariant();
        return lower.Contains("token", StringComparison.Ordinal) ||
            lower.Contains("password", StringComparison.Ordinal) ||
            lower.Contains("secret", StringComparison.Ordinal) ||
            lower.Contains("api_key", StringComparison.Ordinal) ||
            lower.Contains("apikey", StringComparison.Ordinal) ||
            lower.Contains("access_token", StringComparison.Ordinal) ||
            lower.Contains("refresh_token", StringComparison.Ordinal) ||
            lower.Contains("id_token", StringComparison.Ordinal);
    }

    private static void AddRequired(List<string> errors, string? value, string message)
    {
        if (string.IsNullOrWhiteSpace(value))
            errors.Add(message);
    }

    private static NodalOsCoreRuntimeValidationResult Result(List<string> errors, List<string> warnings) => new()
    {
        IsValid = errors.Count == 0,
        RuntimeExecutionAllowed = false,
        RuntimeExecutionDeferred = true,
        RequiresGlobalPolicyEvaluation = true,
        RequiresEvidenceRedaction = true,
        Errors = errors,
        Warnings = warnings
    };
}

public sealed class NodalOsWorkspaceService
{
    public NodalOsWorkspaceLocalModel CreateWorkspaceDraft(string workspaceId = "workspace-local-draft") =>
        Workspace(workspaceId, NodalOsWorkspaceStatus.Draft);

    public NodalOsWorkspaceLocalModel CreateActiveReadOnlyWorkspace() =>
        Workspace("workspace-local-active", NodalOsWorkspaceStatus.ActiveReadOnly);

    public NodalOsPathJailBinding CreatePathJailBinding(string workspaceId = "workspace-local-draft") => new()
    {
        JailId = "jail-local-preview",
        WorkspaceId = workspaceId,
        RootPathRedacted = @"C:\NODAL_OS_WORKSPACE_REDACTED",
        CanonicalRootFingerprint = Fingerprint(@"C:\NODAL_OS_WORKSPACE_REDACTED"),
        AllowedPathPolicyRedacted = ["Relative paths only.", "Fixture-safe textual validation only."],
        DeniedPathPolicyRedacted = ["Traversal, absolute paths, UNC paths, shell and network access are denied."],
        SymlinkPolicyRedacted = "Symlink resolution is deferred and not evaluated against the real filesystem.",
        CaseSensitivityNoteRedacted = "Case behavior is documented only; no real filesystem probing occurs.",
        AllowedOperationTypesFuture =
        [
            NodalOsPathJailOperationKind.ReadMetadataFuture,
            NodalOsPathJailOperationKind.ReadFileFuture,
            NodalOsPathJailOperationKind.CreateTextFileFuture,
            NodalOsPathJailOperationKind.UpdateTextFileFuture,
            NodalOsPathJailOperationKind.ExportReportFuture
        ],
        DisabledOperationsRedacted = ["delete", "overwrite", "shell", "network", "browser runtime", "cloud sync", "external process"],
        CanMutateFilesystem = false,
        CanExecuteShell = false,
        CanAccessOutsideJail = false,
        RequiresPositiveExecutionGate = true,
        RequiresApprovalForFutureMutations = true,
        RealFilesystemAccessAllowed = false,
        CreatedAt = DateTimeOffset.UtcNow
    };

    public NodalOsPathJailValidationResult ValidateRelativePath(NodalOsPathJailBinding binding, string relativePathRedacted) =>
        new NodalOsWorkspaceValidator().ValidateRelativePath(binding, relativePathRedacted);

    public NodalOsProjectImportWizardContract CreateImportWizard() 
    {
        var workspace = CreateWorkspaceDraft();
        var jail = CreatePathJailBinding(workspace.WorkspaceId);
        var validator = new NodalOsWorkspaceValidator();
        return new()
        {
            WizardId = "import-wizard-local-preview",
            CurrentStep = NodalOsProjectImportWizardStepKind.ChooseLocalFolder,
            Steps =
            [
                Step(NodalOsProjectImportWizardStepKind.ChooseLocalFolder, "Choose local folder", "Folder selection is represented as a redacted mock path."),
                Step(NodalOsProjectImportWizardStepKind.PreviewWorkspaceMetadata, "Preview workspace metadata", "Metadata is generated from fixture-safe values."),
                Step(NodalOsProjectImportWizardStepKind.ValidatePathJail, "Validate path jail", "Path jail validation is textual and does not touch the filesystem."),
                Step(NodalOsProjectImportWizardStepKind.ExplainPrivacyLocalFirst, "Explain local-first privacy", "Workspace remains local-only and redacted."),
                Step(NodalOsProjectImportWizardStepKind.ExplainDisabledRuntimeCloudLlm, "Explain disabled runtime, cloud and LLM", "Runtime, cloud and provider calls stay disabled."),
                Step(NodalOsProjectImportWizardStepKind.ConfirmReadOnlyImportPreview, "Confirm read-only import preview", "Import preview cannot scan or import files."),
                Step(NodalOsProjectImportWizardStepKind.CreateWorkspaceDraftMock, "Create workspace draft mock", "Draft is mock-safe and non-productive."),
                Step(NodalOsProjectImportWizardStepKind.ShowNextSafeSteps, "Show next safe steps", "Next step is workspace storage mock and mission binding.")
            ],
            SelectedPathRedacted = workspace.LocalRootPathRedacted,
            WorkspaceDraft = workspace with { PathJailBindingId = jail.JailId, ImportWizardRef = "import-wizard-local-preview" },
            WorkspaceValidation = validator.ValidateWorkspace(workspace),
            PathJailValidation = validator.ValidateRelativePath(jail, "src/README.md"),
            WarningsRedacted = ["Read-only preview only."],
            BlockersRedacted = ["Runtime execution requires future positive gate."],
            DisabledFutureOptionsRedacted = ["real file picker", "filesystem scan", "folder build", "cloud sync", "LLM provider calls"],
            GuardrailExplainersRedacted = ["No runtime.", "No filesystem mutation.", "Path jail contract-only."],
            UserOptions =
            [
                NodalOsProjectImportWizardOptionKind.ContinuePreview,
                NodalOsProjectImportWizardOptionKind.Back,
                NodalOsProjectImportWizardOptionKind.Cancel,
                NodalOsProjectImportWizardOptionKind.RequestExplanation,
                NodalOsProjectImportWizardOptionKind.OpenGuardrailsSummary
            ],
            NoOpIntents = [OpenGuardrailsIntent()],
            ReadOnlyPreview = true,
            ProjectImportMockOnly = true,
            ScansFilesystem = false,
            CreatesFolders = false,
            ImportsFiles = false,
            ProductivePersistenceAllowed = false,
            RuntimeExecutionAllowed = false,
            CloudSyncAllowed = false,
            LlmProviderCallsAllowed = false,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    private static NodalOsWorkspaceLocalModel Workspace(string workspaceId, NodalOsWorkspaceStatus status) => new()
    {
        WorkspaceId = workspaceId,
        DisplayNameRedacted = "Local Workspace Preview",
        DescriptionRedacted = "Read-only local workspace draft for Mission Control.",
        LocalRootPathRedacted = @"C:\NODAL_OS_WORKSPACE_REDACTED",
        RootPathFingerprint = Fingerprint(@"C:\NODAL_OS_WORKSPACE_REDACTED"),
        Status = status,
        PrivacyMode = NodalOsWorkspacePrivacyMode.LocalOnly,
        ReadOnlyPreview = true,
        RuntimeExecutionAllowed = false,
        CloudSyncAllowed = false,
        LlmProviderCallsAllowed = false,
        CanAuthorizeExecution = false,
        PathJailBindingId = "jail-local-preview",
        ActiveMissionRefs = ["mission-local-preview"],
        EvidenceRefs = [Evidence()],
        TimelineRefs = ["timeline-local-preview"],
        UiStateRef = "ui-state-local-preview",
        ImportWizardRef = "import-wizard-local-preview",
        GuardrailSummaryRedacted = ["No runtime.", "No cloud.", "No LLM.", "No filesystem mutation."],
        AllowedCapabilitiesRedacted = ["preview workspace metadata", "validate path contract", "show next safe steps"],
        DisabledCapabilitiesRedacted = ["scan files", "create folders", "import files", "execute shell", "sync cloud"],
        NextSafeStepsRedacted = ["Review path jail binding.", "Continue with workspace storage mock."],
        CreatedAt = DateTimeOffset.UtcNow,
        UpdatedAt = DateTimeOffset.UtcNow
    };

    private static NodalOsProjectImportWizardStep Step(NodalOsProjectImportWizardStepKind kind, string title, string explanation) => new()
    {
        StepKind = kind,
        TitleRedacted = title,
        ExplanationRedacted = explanation,
        IsNoOp = true,
        RuntimeExecutionAllowed = false,
        FilesystemAccessAllowed = false
    };

    private static NodalOsEvidenceBridgeRef Evidence() => new()
    {
        EvidenceId = "evidence-workspace-ref",
        Kind = "WorkspaceMetadataRef",
        Ref = "workspace-metadata-ref-only",
        SourceKind = NodalOsEvidenceBridgeSourceKind.AgentOperation,
        UseKind = NodalOsEvidenceBridgeUseKind.AuditTrail,
        Authority = NodalOsEvidenceBridgeAuthority.NoAuthority,
        Sensitivity = NodalOsEvidenceSensitivity.NonSensitive,
        RedactionState = NodalOsEvidenceRedactionState.Redacted,
        CreatedAt = DateTimeOffset.UtcNow
    };

    private static NodalOsMissionControlUiIntent OpenGuardrailsIntent() => new()
    {
        IntentId = "intent-open-workspace-guardrails",
        IntentKind = NodalOsMissionControlUiIntentKind.OpenGuardrailsSummary,
        SourceSurface = NodalOsMissionControlUiSurfaceKind.GuardrailsSummary,
        ActorRedacted = "local preview user",
        MissionId = "mission-local-preview",
        NoteRedacted = "Open workspace guardrails summary.",
        MetadataRedacted = new Dictionary<string, string> { ["workspace"] = "workspace-local-draft" },
        IsNoOp = true,
        CanAuthorizeExecution = false,
        RuntimeExecutionAllowed = false,
        RuntimeExecutionDeferred = true,
        RequiresPositiveExecutionGate = false,
        CreatedAt = DateTimeOffset.UtcNow
    };

    private static string Fingerprint(string value)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(hash)[..16].ToLowerInvariant();
    }
}

public sealed class NodalOsWorkspaceJsonSerializer
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
        WriteIndented = true
    };

    public string SerializeWorkspace(NodalOsWorkspaceLocalModel workspace) => JsonSerializer.Serialize(workspace, Options);
    public NodalOsWorkspaceLocalModel? DeserializeWorkspace(string json) => JsonSerializer.Deserialize<NodalOsWorkspaceLocalModel>(json, Options);
    public string SerializePathJailBinding(NodalOsPathJailBinding binding) => JsonSerializer.Serialize(binding, Options);
    public NodalOsPathJailBinding? DeserializePathJailBinding(string json) => JsonSerializer.Deserialize<NodalOsPathJailBinding>(json, Options);
    public string SerializeImportWizard(NodalOsProjectImportWizardContract wizard) => JsonSerializer.Serialize(wizard, Options);
    public NodalOsProjectImportWizardContract? DeserializeImportWizard(string json) => JsonSerializer.Deserialize<NodalOsProjectImportWizardContract>(json, Options);
}

public static class NodalOsWorkspaceFixtures
{
    public static NodalOsWorkspaceLocalModel WorkspaceDraft() => new NodalOsWorkspaceService().CreateWorkspaceDraft();
    public static NodalOsWorkspaceLocalModel ActiveReadOnlyWorkspace() => new NodalOsWorkspaceService().CreateActiveReadOnlyWorkspace();
    public static NodalOsPathJailBinding PathJailBinding() => new NodalOsWorkspaceService().CreatePathJailBinding();
    public static NodalOsProjectImportWizardContract ImportWizard() => new NodalOsWorkspaceService().CreateImportWizard();
}
