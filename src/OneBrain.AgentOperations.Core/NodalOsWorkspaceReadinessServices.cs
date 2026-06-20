using System.Text.Json;
using System.Text.Json.Serialization;
using OneBrain.AgentOperations.Contracts;

namespace OneBrain.AgentOperations.Core;

public sealed class NodalOsWorkspaceReadinessService
{
    public NodalOsWorkspaceReadinessGateResult EvaluateReadiness(
        NodalOsWorkspaceLocalModel? workspace,
        NodalOsPathJailBinding? pathJail,
        NodalOsProjectImportWizardContract? importWizard,
        NodalOsWorkspaceStorageMockSummary? storageSummary,
        NodalOsWorkspaceMissionBinding? missionBinding,
        NodalOsWorkspaceMetadataIndexMock? metadataIndex,
        NodalOsSafeProjectSummaryContract? projectSummary,
        NodalOsWorkspaceHealthReport? healthReport,
        NodalOsWorkspaceReadinessState? forcedState = null)
    {
        var status = forcedState ?? InferStatus(workspace, pathJail, importWizard, storageSummary, missionBinding, metadataIndex, projectSummary, healthReport);
        var workspaceId = workspace?.WorkspaceId;
        return new()
        {
            GateId = $"workspace-readiness-{workspaceId ?? "missing"}-{status}",
            WorkspaceId = workspaceId,
            Status = status,
            ReasonsRedacted = ReasonsFor(status),
            BlockersRedacted = BlockersFor(status),
            WarningsRedacted = status == NodalOsWorkspaceReadinessState.ReadyForUserProvidedContextIntake
                ? ["Context intake remains user-provided or mock-safe only."]
                : ["Readiness gate is read-only and cannot unlock execution."],
            AllowedNextSafeCapabilitiesRedacted = AllowedNextSafeCapabilitiesFor(status),
            DisabledCapabilitiesRedacted =
            [
                "runtime execution",
                "filesystem scan",
                "directory listing",
                "file content access",
                "git command",
                "provider call",
                "cloud sync"
            ],
            HumanSummaryRedacted = HumanSummaryFor(status),
            TechnicalSummaryRedacted = "Readiness gate classifies safe next-step readiness only; no runtime, scan, provider call, cloud call, or productive mutation occurs.",
            EvidenceRefs = workspace?.EvidenceRefs ?? [],
            TimelineRefs = workspace?.TimelineRefs ?? [],
            GuardrailRefs = ["no-runtime", "no-filesystem-scan", "no-llm", "no-cloud", "positive-gate-missing"],
            ReadOnlyGate = true,
            FilesystemScanAllowed = false,
            LlmProviderCallsAllowed = false,
            CloudSyncAllowed = false,
            CanAuthorizeExecution = false,
            RuntimeExecutionAllowed = false,
            PositiveExecutionGateImplemented = false,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public NodalOsProjectUnderstandingIntakeContract CreateUserProvidedSummaryIntake(
        NodalOsWorkspaceLocalModel workspace,
        string missionId = "mission-local-preview") =>
        CreateIntake(workspace, missionId, NodalOsProjectUnderstandingIntakeSource.UserProvidedSummary, NodalOsProjectUnderstandingIntakeItemType.ProjectSummary, "Contexto provisto por el usuario o mock seguro.");

    public NodalOsProjectUnderstandingIntakeContract CreateTechStackHintIntake(NodalOsWorkspaceLocalModel workspace) =>
        CreateIntake(workspace, null, NodalOsProjectUnderstandingIntakeSource.UserProvidedTechStack, NodalOsProjectUnderstandingIntakeItemType.TechStackHint, "Tech stack hint provided by user or safe fixture.");

    public NodalOsProjectUnderstandingIntakeContract CreateFolderStructureHintIntake(NodalOsWorkspaceLocalModel workspace) =>
        CreateIntake(workspace, null, NodalOsProjectUnderstandingIntakeSource.UserProvidedFileList, NodalOsProjectUnderstandingIntakeItemType.FolderStructureHint, "Folder structure hint is user-provided and not verified.");

    public NodalOsProjectUnderstandingIntakeContract CreateFutureRealScanPlaceholder(NodalOsWorkspaceLocalModel workspace) =>
        CreateIntake(workspace, null, NodalOsProjectUnderstandingIntakeSource.FutureRealScanPlaceholder, NodalOsProjectUnderstandingIntakeItemType.FutureRealScanPlaceholder, "Future real scan placeholder; disabled until safe context boundary and approvals exist.");

    public NodalOsSafeContextBoundaryDecision ClassifyContext(
        string workspaceId,
        NodalOsContextSensitivityLevel sensitivity,
        NodalOsSafeContextUsageTarget usageTarget)
    {
        var blocked = sensitivity is NodalOsContextSensitivityLevel.SensitiveBlocked
            or NodalOsContextSensitivityLevel.SecretBlocked
            or NodalOsContextSensitivityLevel.RawPayloadBlocked
            or NodalOsContextSensitivityLevel.UnknownRequiresReview;
        var futureLlm = usageTarget is NodalOsSafeContextUsageTarget.FutureLlmPrompt
            or NodalOsSafeContextUsageTarget.FutureAdvisor
            or NodalOsSafeContextUsageTarget.FutureAssignmentEngine;

        return new()
        {
            BoundaryId = $"safe-context-{workspaceId}-{sensitivity}-{usageTarget}",
            WorkspaceId = workspaceId,
            AllowedContextRefs = blocked ? [] : ["context-ref-safe-redacted"],
            DeniedContextRefs = blocked ? ["context-ref-blocked"] : [],
            RedactionStatusRedacted = sensitivity == NodalOsContextSensitivityLevel.RedactedOnly ? "Redacted only." : "No raw values retained.",
            SensitivityLevel = sensitivity,
            ProvenanceRedacted = "User-provided or mock-safe context.",
            UsageTarget = usageTarget,
            AllowedFieldsRedacted = blocked ? [] : ["title", "summary", "evidence ref", "timeline ref"],
            DeniedFieldsRedacted = blocked ? ["raw payload", "sensitive values", "unredacted path"] : ["raw payload", "unredacted path"],
            ReasonCodesRedacted = ReasonCodesFor(sensitivity, usageTarget),
            MissingApprovalRequirementsRedacted = futureLlm ? ["Future BYOK/LLM policy approval required."] : [],
            UserConsentPlaceholderRedacted = futureLlm ? "Future user consent required before provider context use." : "No provider use requested.",
            PolicySummaryRedacted = PolicySummaryFor(sensitivity, usageTarget),
            GuardrailSummaryRedacted = ["No context authorizes execution.", "Evidence remains ref-only.", "Raw paths are redacted or fingerprinted."],
            SafeForDisplay = !blocked,
            SafeForExport = !blocked && usageTarget is NodalOsSafeContextUsageTarget.Export or NodalOsSafeContextUsageTarget.Display or NodalOsSafeContextUsageTarget.FutureEvidenceReport,
            FutureLlmPolicyRequired = futureLlm,
            ByokRequiredForFutureLlm = futureLlm,
            EvidenceRefOnly = sensitivity == NodalOsContextSensitivityLevel.EvidenceRefOnly,
            RawPathRedactedOrFingerprinted = true,
            CanAuthorizeExecution = false,
            CanBypassApproval = false,
            CallsLlmProvider = false,
            CallsCloud = false,
            ScansWorkspace = false,
            MutatesWorkspace = false,
            CreatesVectorIndex = false,
            CreatesPrompt = false,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    private static NodalOsWorkspaceReadinessState InferStatus(
        NodalOsWorkspaceLocalModel? workspace,
        NodalOsPathJailBinding? pathJail,
        NodalOsProjectImportWizardContract? importWizard,
        NodalOsWorkspaceStorageMockSummary? storageSummary,
        NodalOsWorkspaceMissionBinding? missionBinding,
        NodalOsWorkspaceMetadataIndexMock? metadataIndex,
        NodalOsSafeProjectSummaryContract? projectSummary,
        NodalOsWorkspaceHealthReport? healthReport)
    {
        if (workspace is null)
            return NodalOsWorkspaceReadinessState.BlockedByMissingWorkspace;
        if (pathJail is null || string.IsNullOrWhiteSpace(workspace.PathJailBindingId))
            return NodalOsWorkspaceReadinessState.BlockedByPathJail;
        if (healthReport?.HealthStatus == NodalOsWorkspaceHealthStatus.BlockedByRuntimeGate)
            return NodalOsWorkspaceReadinessState.BlockedByRuntimeGate;
        if (healthReport?.HealthStatus == NodalOsWorkspaceHealthStatus.BlockedByCloudQuarantine)
            return NodalOsWorkspaceReadinessState.BlockedByCloudQuarantine;
        if (storageSummary is null || missionBinding is null || importWizard is null)
            return NodalOsWorkspaceReadinessState.ReadyForReadOnlyPreview;
        if (metadataIndex is null || projectSummary is null)
            return NodalOsWorkspaceReadinessState.ReadyForMockMetadata;
        return NodalOsWorkspaceReadinessState.ReadyForUserProvidedContextIntake;
    }

    private static NodalOsProjectUnderstandingIntakeContract CreateIntake(
        NodalOsWorkspaceLocalModel workspace,
        string? missionId,
        NodalOsProjectUnderstandingIntakeSource source,
        NodalOsProjectUnderstandingIntakeItemType itemType,
        string text)
    {
        return new()
        {
            IntakeId = $"project-understanding-intake-{workspace.WorkspaceId}-{source}",
            WorkspaceId = workspace.WorkspaceId,
            MissionId = missionId,
            Source = source,
            Items =
            [
                new NodalOsProjectUnderstandingIntakeItem
                {
                    IntakeItemId = $"intake-item-{itemType}",
                    ItemType = itemType,
                    TextRedacted = text,
                    MetadataRedacted = "No se verifico estructura real.",
                    Confidence = source == NodalOsProjectUnderstandingIntakeSource.FutureRealScanPlaceholder
                        ? NodalOsProjectSummaryConfidence.Unknown
                        : NodalOsProjectSummaryConfidence.UserProvided,
                    FreshnessRedacted = "Declared by user or fixture.",
                    ProvenanceRedacted = "User-provided/mock-safe; not derived from filesystem.",
                    UserProvidedOrMockSafe = true,
                    ValidatesRealExistence = false,
                    ReadsWorkspaceContent = false
                }
            ],
            ContextDisclosureRedacted = "Contexto provisto por el usuario o mock seguro.",
            NoContentAccessDisclosureRedacted = "No se leyo ningun archivo.",
            StructureNotVerifiedDisclosureRedacted = "No se verifico estructura real.",
            NoRealUnderstandingDisclosureRedacted = "No se genero project understanding real todavia.",
            DeclaredConfidence = source == NodalOsProjectUnderstandingIntakeSource.FutureRealScanPlaceholder
                ? NodalOsProjectSummaryConfidence.Unknown
                : NodalOsProjectSummaryConfidence.UserProvided,
            DeclaredFreshnessRedacted = "Declared by user or fixture.",
            DeclaredProvenanceRedacted = "User-provided or mock-safe intake.",
            AllowedUsageRedacted = ["display safe preview", "export redacted summary", "ask follow-up questions"],
            DisallowedUsageRedacted = ["runtime execution", "file verification", "provider prompt", "cloud sync"],
            EvidenceRefs = workspace.EvidenceRefs,
            TimelineRefs = workspace.TimelineRefs,
            GuardrailRefs = ["no-file-read", "no-real-structure-validation", "no-provider-call"],
            MissingInformationRedacted = ["Real source structure not verified.", "No file contents inspected."],
            QuestionsForUserRedacted = ["Which files or areas matter most?", "What constraints should be tracked?"],
            NextSafeStepsRedacted = ["Review context boundary classification.", "Keep intake user-provided until future audit."],
            ReadsFiles = false,
            ValidatesRealStructure = false,
            UsesGit = false,
            CreatesVectorIndex = false,
            CallsLlmProvider = false,
            CreatesRealProjectUnderstanding = false,
            CanAuthorizeExecution = false,
            ChangesWorkspaceProductively = false,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    private static IReadOnlyList<string> ReasonsFor(NodalOsWorkspaceReadinessState status) => status switch
    {
        NodalOsWorkspaceReadinessState.ReadyForReadOnlyPreview => ["Workspace can be shown as read-only preview."],
        NodalOsWorkspaceReadinessState.ReadyForMockMetadata => ["Workspace has enough safe state for mock metadata."],
        NodalOsWorkspaceReadinessState.ReadyForUserProvidedContextIntake => ["Workspace can accept user-provided context intake."],
        _ => ["Workspace is not ready for the requested next safe step."]
    };

    private static IReadOnlyList<string> BlockersFor(NodalOsWorkspaceReadinessState status) => status switch
    {
        NodalOsWorkspaceReadinessState.BlockedByPathJail => ["Path jail binding is missing or invalid."],
        NodalOsWorkspaceReadinessState.BlockedByMissingWorkspace => ["Workspace model is missing."],
        NodalOsWorkspaceReadinessState.BlockedByRuntimeGate => ["Positive execution gate is not implemented."],
        NodalOsWorkspaceReadinessState.BlockedByCloudQuarantine => ["Cloud is blocked by legacy quarantine plan."],
        NodalOsWorkspaceReadinessState.BlockedByLegacySensitiveSubsystem => ["Legacy sensitive subsystem quarantine is unresolved."],
        NodalOsWorkspaceReadinessState.BlockedByRecipeRiskHardening => ["Recipe Risk Classifier hardening remains runtime-gated."],
        NodalOsWorkspaceReadinessState.NotReady => ["Required workspace readiness inputs are incomplete."],
        NodalOsWorkspaceReadinessState.Unknown => ["Readiness could not be classified."],
        _ => []
    };

    private static IReadOnlyList<string> AllowedNextSafeCapabilitiesFor(NodalOsWorkspaceReadinessState status) => status switch
    {
        NodalOsWorkspaceReadinessState.ReadyForReadOnlyPreview => ["Mission Control read-only preview"],
        NodalOsWorkspaceReadinessState.ReadyForMockMetadata => ["Workspace metadata mock"],
        NodalOsWorkspaceReadinessState.ReadyForUserProvidedContextIntake => ["User-provided context intake", "Safe context boundary classification"],
        _ => []
    };

    private static string HumanSummaryFor(NodalOsWorkspaceReadinessState status) => status switch
    {
        NodalOsWorkspaceReadinessState.ReadyForUserProvidedContextIntake => "Workspace is ready for safe user-provided context intake.",
        NodalOsWorkspaceReadinessState.ReadyForMockMetadata => "Workspace is ready for mock metadata only.",
        NodalOsWorkspaceReadinessState.ReadyForReadOnlyPreview => "Workspace is ready for read-only preview.",
        _ => "Workspace is blocked or incomplete."
    };

    private static IReadOnlyList<string> ReasonCodesFor(NodalOsContextSensitivityLevel sensitivity, NodalOsSafeContextUsageTarget target)
    {
        var reasons = new List<string> { $"sensitivity:{sensitivity}", $"target:{target}" };
        if (target is NodalOsSafeContextUsageTarget.FutureLlmPrompt or NodalOsSafeContextUsageTarget.FutureAdvisor or NodalOsSafeContextUsageTarget.FutureAssignmentEngine)
            reasons.Add("future-llm-policy-required");
        if (sensitivity is NodalOsContextSensitivityLevel.SensitiveBlocked or NodalOsContextSensitivityLevel.SecretBlocked or NodalOsContextSensitivityLevel.RawPayloadBlocked)
            reasons.Add("blocked-sensitive-or-raw");
        if (sensitivity == NodalOsContextSensitivityLevel.UnknownRequiresReview)
            reasons.Add("requires-review");
        return reasons;
    }

    private static string PolicySummaryFor(NodalOsContextSensitivityLevel sensitivity, NodalOsSafeContextUsageTarget target) =>
        target is NodalOsSafeContextUsageTarget.FutureLlmPrompt or NodalOsSafeContextUsageTarget.FutureAdvisor or NodalOsSafeContextUsageTarget.FutureAssignmentEngine
            ? "Future LLM usage requires BYOK/LLM policy and consent; no provider call is made now."
            : sensitivity is NodalOsContextSensitivityLevel.SensitiveBlocked or NodalOsContextSensitivityLevel.SecretBlocked or NodalOsContextSensitivityLevel.RawPayloadBlocked
                ? "Context is blocked from display/export."
                : "Context is safe for requested non-runtime use.";
}

public sealed class NodalOsWorkspaceReadinessValidator
{
    private readonly NodalOsRedactionService redaction = new();
    private readonly NodalOsEvidenceRefBridge evidenceBridge = new();

    public NodalOsCoreRuntimeValidationResult ValidateReadiness(NodalOsWorkspaceReadinessGateResult readiness)
    {
        var errors = new List<string>();
        AddRequired(errors, readiness.GateId, "GateId is required.");
        AddRequired(errors, readiness.HumanSummaryRedacted, "HumanSummaryRedacted is required.");
        AddRequired(errors, readiness.TechnicalSummaryRedacted, "TechnicalSummaryRedacted is required.");
        if (!readiness.ReadOnlyGate)
            errors.Add("Readiness gate must be read-only.");
        if (readiness.FilesystemScanAllowed || readiness.LlmProviderCallsAllowed || readiness.CloudSyncAllowed || readiness.CanAuthorizeExecution || readiness.RuntimeExecutionAllowed || readiness.PositiveExecutionGateImplemented)
            errors.Add("Readiness gate cannot enable scan, provider calls, cloud, execution, runtime, or positive gate.");
        ValidateTexts(errors, "ReasonsRedacted", readiness.ReasonsRedacted);
        ValidateTexts(errors, "BlockersRedacted", readiness.BlockersRedacted);
        ValidateTexts(errors, "WarningsRedacted", readiness.WarningsRedacted);
        ValidateTexts(errors, "AllowedNextSafeCapabilitiesRedacted", readiness.AllowedNextSafeCapabilitiesRedacted);
        ValidateTexts(errors, "DisabledCapabilitiesRedacted", readiness.DisabledCapabilitiesRedacted);
        ValidateText(errors, "HumanSummaryRedacted", readiness.HumanSummaryRedacted);
        ValidateText(errors, "TechnicalSummaryRedacted", readiness.TechnicalSummaryRedacted);
        ValidateEvidence(errors, readiness.EvidenceRefs);
        return Result(errors);
    }

    public NodalOsCoreRuntimeValidationResult ValidateIntake(NodalOsProjectUnderstandingIntakeContract intake)
    {
        var errors = new List<string>();
        AddRequired(errors, intake.IntakeId, "IntakeId is required.");
        AddRequired(errors, intake.WorkspaceId, "WorkspaceId is required.");
        AddRequired(errors, intake.ContextDisclosureRedacted, "ContextDisclosureRedacted is required.");
        AddRequired(errors, intake.NoContentAccessDisclosureRedacted, "NoContentAccessDisclosureRedacted is required.");
        AddRequired(errors, intake.StructureNotVerifiedDisclosureRedacted, "StructureNotVerifiedDisclosureRedacted is required.");
        AddRequired(errors, intake.NoRealUnderstandingDisclosureRedacted, "NoRealUnderstandingDisclosureRedacted is required.");
        if (intake.ReadsFiles || intake.ValidatesRealStructure || intake.UsesGit || intake.CreatesVectorIndex || intake.CallsLlmProvider || intake.CreatesRealProjectUnderstanding || intake.CanAuthorizeExecution || intake.ChangesWorkspaceProductively)
            errors.Add("Project understanding intake must not read, verify, use git, create vectors, call provider, create real understanding, authorize execution, or mutate workspace.");
        if (intake.Items.Count == 0)
            errors.Add("Project understanding intake requires at least one item.");
        foreach (var item in intake.Items)
        {
            ValidateText(errors, "TextRedacted", item.TextRedacted);
            ValidateText(errors, "MetadataRedacted", item.MetadataRedacted);
            ValidateText(errors, "FreshnessRedacted", item.FreshnessRedacted);
            ValidateText(errors, "ProvenanceRedacted", item.ProvenanceRedacted);
            if (!item.UserProvidedOrMockSafe || item.ValidatesRealExistence || item.ReadsWorkspaceContent)
                errors.Add("Project understanding intake item must be user-provided/mock-safe without real validation or content access.");
        }
        ValidateTexts(errors, "AllowedUsageRedacted", intake.AllowedUsageRedacted);
        ValidateTexts(errors, "DisallowedUsageRedacted", intake.DisallowedUsageRedacted);
        ValidateTexts(errors, "MissingInformationRedacted", intake.MissingInformationRedacted);
        ValidateTexts(errors, "QuestionsForUserRedacted", intake.QuestionsForUserRedacted);
        ValidateTexts(errors, "NextSafeStepsRedacted", intake.NextSafeStepsRedacted);
        ValidateEvidence(errors, intake.EvidenceRefs);
        return Result(errors);
    }

    public NodalOsCoreRuntimeValidationResult ValidateContextBoundary(NodalOsSafeContextBoundaryDecision boundary)
    {
        var errors = new List<string>();
        AddRequired(errors, boundary.BoundaryId, "BoundaryId is required.");
        AddRequired(errors, boundary.WorkspaceId, "WorkspaceId is required.");
        AddRequired(errors, boundary.RedactionStatusRedacted, "RedactionStatusRedacted is required.");
        AddRequired(errors, boundary.ProvenanceRedacted, "ProvenanceRedacted is required.");
        AddRequired(errors, boundary.PolicySummaryRedacted, "PolicySummaryRedacted is required.");
        if (boundary.CanAuthorizeExecution || boundary.CanBypassApproval || boundary.CallsLlmProvider || boundary.CallsCloud || boundary.ScansWorkspace || boundary.MutatesWorkspace || boundary.CreatesVectorIndex || boundary.CreatesPrompt)
            errors.Add("Safe context boundary cannot authorize execution, bypass approval, call providers/cloud, scan, mutate, create vectors, or create prompts.");
        if (boundary.SensitivityLevel is NodalOsContextSensitivityLevel.SensitiveBlocked or NodalOsContextSensitivityLevel.SecretBlocked or NodalOsContextSensitivityLevel.RawPayloadBlocked)
        {
            if (boundary.SafeForDisplay || boundary.SafeForExport || boundary.AllowedContextRefs.Count > 0)
                errors.Add("Blocked context cannot be display/export safe or allowed.");
        }
        if (boundary.SensitivityLevel == NodalOsContextSensitivityLevel.UnknownRequiresReview && !boundary.ReasonCodesRedacted.Any(reason => reason.Contains("requires-review", StringComparison.OrdinalIgnoreCase)))
            errors.Add("Unknown context must require review.");
        if (boundary.UsageTarget is NodalOsSafeContextUsageTarget.FutureLlmPrompt or NodalOsSafeContextUsageTarget.FutureAdvisor or NodalOsSafeContextUsageTarget.FutureAssignmentEngine)
        {
            if (!boundary.FutureLlmPolicyRequired || !boundary.ByokRequiredForFutureLlm)
                errors.Add("Future LLM context must require future policy and BYOK.");
        }
        if (!boundary.RawPathRedactedOrFingerprinted)
            errors.Add("Raw paths must be redacted or fingerprinted.");
        ValidateTexts(errors, "AllowedContextRefs", boundary.AllowedContextRefs);
        ValidateTexts(errors, "DeniedContextRefs", boundary.DeniedContextRefs);
        ValidateTexts(errors, "AllowedFieldsRedacted", boundary.AllowedFieldsRedacted);
        ValidateTexts(errors, "DeniedFieldsRedacted", boundary.DeniedFieldsRedacted);
        ValidateTexts(errors, "ReasonCodesRedacted", boundary.ReasonCodesRedacted);
        ValidateTexts(errors, "GuardrailSummaryRedacted", boundary.GuardrailSummaryRedacted);
        ValidateText(errors, "PolicySummaryRedacted", boundary.PolicySummaryRedacted);
        return Result(errors);
    }

    private void ValidateEvidence(List<string> errors, IEnumerable<NodalOsEvidenceBridgeRef> evidenceRefs)
    {
        foreach (var evidence in evidenceRefs)
        {
            var result = evidenceBridge.ValidateBridgeRef(evidence);
            if (!result.Accepted)
                errors.AddRange(result.Errors);
        }
    }

    private void ValidateTexts(List<string> errors, string fieldName, IEnumerable<string> values)
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

public sealed class NodalOsWorkspaceReadinessJsonSerializer
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
        WriteIndented = true
    };

    public string SerializeReadiness(NodalOsWorkspaceReadinessGateResult readiness) => JsonSerializer.Serialize(readiness, Options);
    public string SerializeIntake(NodalOsProjectUnderstandingIntakeContract intake) => JsonSerializer.Serialize(intake, Options);
    public string SerializeContextBoundary(NodalOsSafeContextBoundaryDecision boundary) => JsonSerializer.Serialize(boundary, Options);
}

public static class NodalOsWorkspaceReadinessFixtures
{
    public static NodalOsWorkspaceReadinessGateResult ReadyForUserProvidedContextIntake()
    {
        var workspace = NodalOsWorkspaceFixtures.ActiveReadOnlyWorkspace();
        var workspaceService = new NodalOsWorkspaceService();
        var metadataService = new NodalOsWorkspaceMetadataService();
        var binding = new NodalOsWorkspaceMissionBindingService().CreateBinding(workspace);
        var index = metadataService.CreateMockIndex(workspace);
        var summary = metadataService.CreateSafeProjectSummary(workspace, index, binding);
        var health = metadataService.CreateHealthReport(workspace, index, binding);
        return new NodalOsWorkspaceReadinessService().EvaluateReadiness(
            workspace,
            workspaceService.CreatePathJailBinding(workspace.WorkspaceId),
            workspaceService.CreateImportWizard(),
            NodalOsWorkspaceMissionFixtures.StorageWithDraftAndActive().Summary(),
            binding,
            index,
            summary,
            health);
    }

    public static NodalOsProjectUnderstandingIntakeContract UserProvidedSummaryIntake() =>
        new NodalOsWorkspaceReadinessService().CreateUserProvidedSummaryIntake(NodalOsWorkspaceFixtures.ActiveReadOnlyWorkspace());

    public static NodalOsSafeContextBoundaryDecision DisplaySafeContext() =>
        new NodalOsWorkspaceReadinessService().ClassifyContext("workspace-local-active", NodalOsContextSensitivityLevel.UserProvidedSafe, NodalOsSafeContextUsageTarget.Display);
}
