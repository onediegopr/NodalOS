using System.Text.Json;
using System.Text.Json.Serialization;
using OneBrain.AgentOperations.Contracts;

namespace OneBrain.AgentOperations.Core;

public sealed class NodalOsUserContextService
{
    private readonly NodalOsWorkspaceReadinessService readinessService = new();
    private readonly NodalOsUserContextValidator validator = new();

    public NodalOsUserProvidedContextCapture CreateCapture(
        NodalOsWorkspaceLocalModel workspace,
        NodalOsUserContextCaptureType captureType,
        string contentRedacted = "User-provided safe context preview.",
        NodalOsContextSensitivityLevel sensitivity = NodalOsContextSensitivityLevel.UserProvidedSafe)
    {
        var boundary = readinessService.ClassifyContext(workspace.WorkspaceId, sensitivity, NodalOsSafeContextUsageTarget.Display);
        var capture = new NodalOsUserProvidedContextCapture
        {
            ContextCaptureId = $"context-capture-{workspace.WorkspaceId}-{captureType}",
            WorkspaceId = workspace.WorkspaceId,
            MissionId = workspace.ActiveMissionRefs.FirstOrDefault(),
            SubmittedByActorRedacted = "local preview user",
            CaptureType = captureType,
            ContentRedacted = contentRedacted,
            MetadataRedacted = "Context is user-provided and not verified against the filesystem.",
            DeclaredProvenanceRedacted = "user-provided",
            DeclaredConfidence = NodalOsProjectSummaryConfidence.UserProvided,
            DeclaredFreshnessRedacted = "Declared by user at capture time.",
            SensitivityLevel = sensitivity,
            BoundaryDecision = boundary,
            AllowedUsageRedacted = boundary.SafeForDisplay ? ["display in Mission Control", "review card", "evidence-linked summary"] : [],
            DisallowedUsageRedacted = ["runtime execution", "provider prompt", "filesystem verification", "cloud sync"],
            MissingInformationRedacted = ["Real source structure not verified.", "No file contents inspected."],
            StaticQuestionsRedacted = StaticQuestionsFor(captureType),
            EvidenceRefs = workspace.EvidenceRefs,
            TimelineRefs = workspace.TimelineRefs,
            GuardrailRefs = ["user-provided-only", "no-file-read", "safe-context-boundary"],
            ValidationResult = ValidationResult([]),
            UserProvidedOnly = true,
            FilesystemVerificationAllowed = false,
            ReadsFiles = false,
            UsesGit = false,
            CreatesVectorIndex = false,
            CallsLlmProvider = false,
            CreatesPrompt = false,
            CreatesRealProjectUnderstanding = false,
            CanAuthorizeExecution = false,
            ChangesWorkspaceProductively = false,
            CreatedAt = DateTimeOffset.UtcNow
        };

        return capture with { ValidationResult = validator.ValidateCapture(capture) };
    }

    public NodalOsContextReviewCard CreateReviewCard(
        NodalOsUserProvidedContextCapture capture,
        NodalOsContextReviewCardStatus? forcedStatus = null)
    {
        var status = forcedStatus ?? StatusFrom(capture.BoundaryDecision);
        return new()
        {
            ReviewCardId = $"context-review-{capture.ContextCaptureId}",
            ContextCaptureId = capture.ContextCaptureId,
            WorkspaceId = capture.WorkspaceId,
            MissionId = capture.MissionId,
            CardTitleRedacted = $"Review {capture.CaptureType}",
            SummaryRedacted = capture.ContentRedacted,
            DetailsRedacted = "User-provided context, not verified against filesystem.",
            ProvenanceLabelRedacted = capture.DeclaredProvenanceRedacted,
            ConfidenceLabelRedacted = capture.DeclaredConfidence.ToString(),
            FreshnessLabelRedacted = capture.DeclaredFreshnessRedacted,
            SensitivityLabelRedacted = capture.SensitivityLevel.ToString(),
            Status = status,
            AllowedUsageChipsRedacted = capture.AllowedUsageRedacted,
            DisallowedUsageChipsRedacted = capture.DisallowedUsageRedacted,
            MissingInformationRedacted = capture.MissingInformationRedacted,
            QuestionsForUserRedacted = capture.StaticQuestionsRedacted,
            WarningsRedacted = status is NodalOsContextReviewCardStatus.RequiresReview or NodalOsContextReviewCardStatus.BlockedSensitive or NodalOsContextReviewCardStatus.BlockedSecret or NodalOsContextReviewCardStatus.BlockedRawPayload
                ? ["Review required before display/export."]
                : ["Context is safe for read-only review only."],
            GuardrailRefs = capture.GuardrailRefs,
            EvidenceRefs = capture.EvidenceRefs,
            TimelineRefs = capture.TimelineRefs,
            UserOptions =
            [
                NodalOsContextReviewOptionKind.AcceptForDisplay,
                NodalOsContextReviewOptionKind.MarkNeedsClarification,
                NodalOsContextReviewOptionKind.EditDraft,
                NodalOsContextReviewOptionKind.DiscardDraft,
                NodalOsContextReviewOptionKind.RequestExplanation,
                NodalOsContextReviewOptionKind.LinkEvidenceRef,
                NodalOsContextReviewOptionKind.OpenGuardrails
            ],
            UserOptionsAreNoOp = true,
            UserProvidedAndNotVerified = true,
            CanAuthorizeExecution = false,
            RuntimeExecutionAllowed = false,
            CallsLlmProvider = false,
            CreatesPrompt = false,
            MutatesRuntime = false,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public NodalOsContextEvidenceLink CreateEvidenceLink(
        NodalOsUserProvidedContextCapture capture,
        NodalOsContextReviewCard? reviewCard,
        NodalOsEvidenceBridgeRef evidenceRef,
        NodalOsContextEvidenceLinkType linkType = NodalOsContextEvidenceLinkType.SupportsContext)
    {
        var candidate = new NodalOsContextEvidenceLink
        {
            LinkId = $"context-evidence-link-{capture.ContextCaptureId}-{evidenceRef.EvidenceId}-{linkType}",
            ContextCaptureId = capture.ContextCaptureId,
            ReviewCardId = reviewCard?.ReviewCardId,
            EvidenceRefId = evidenceRef.EvidenceId,
            TimelineRefId = capture.TimelineRefs.FirstOrDefault(),
            WorkspaceId = capture.WorkspaceId,
            MissionId = capture.MissionId,
            LinkType = linkType,
            LinkStatus = NodalOsContextEvidenceLinkStatus.LinkedRefOnly,
            LinkReasonRedacted = "Claim unverified; evidence ref linked for review only.",
            ProvenanceRedacted = "user-provided context with evidence reference.",
            ValidationResult = ValidationResult([]),
            RefOnly = true,
            IncludesRawPayload = false,
            IncludesScreenshotInline = false,
            IncludesDomRaw = false,
            IncludesNetworkRaw = false,
            ReadsFiles = false,
            ValidatesRealContent = false,
            ConvertsClaimToAuthoritativeTruth = false,
            CanAuthorizeExecution = false,
            CallsLlmProvider = false,
            CallsCloud = false,
            CreatedAt = DateTimeOffset.UtcNow
        };

        return candidate with { ValidationResult = validator.ValidateEvidenceLink(candidate, evidenceRef) };
    }

    public NodalOsContextEvidenceLink CreateUnsafeEvidenceLink(
        NodalOsUserProvidedContextCapture capture,
        string evidenceRefId = "unsafe-evidence-ref") => new()
    {
        LinkId = $"context-evidence-link-{capture.ContextCaptureId}-{evidenceRefId}-blocked",
        ContextCaptureId = capture.ContextCaptureId,
        EvidenceRefId = evidenceRefId,
        WorkspaceId = capture.WorkspaceId,
        MissionId = capture.MissionId,
        LinkType = NodalOsContextEvidenceLinkType.RelatedEvidenceRef,
        LinkStatus = NodalOsContextEvidenceLinkStatus.BlockedUnsafeEvidence,
        LinkReasonRedacted = "Unsafe evidence is blocked; claim remains unverified.",
        ProvenanceRedacted = "blocked unsafe evidence reference",
        ValidationResult = ValidationResult(["Unsafe evidence is blocked."]),
        RefOnly = false,
        IncludesRawPayload = true,
        IncludesScreenshotInline = true,
        IncludesDomRaw = true,
        IncludesNetworkRaw = true,
        ReadsFiles = false,
        ValidatesRealContent = false,
        ConvertsClaimToAuthoritativeTruth = false,
        CanAuthorizeExecution = false,
        CallsLlmProvider = false,
        CallsCloud = false,
        CreatedAt = DateTimeOffset.UtcNow
    };

    private static NodalOsContextReviewCardStatus StatusFrom(NodalOsSafeContextBoundaryDecision boundary) =>
        boundary.SensitivityLevel switch
        {
            NodalOsContextSensitivityLevel.SensitiveBlocked => NodalOsContextReviewCardStatus.BlockedSensitive,
            NodalOsContextSensitivityLevel.SecretBlocked => NodalOsContextReviewCardStatus.BlockedSecret,
            NodalOsContextSensitivityLevel.RawPayloadBlocked => NodalOsContextReviewCardStatus.BlockedRawPayload,
            NodalOsContextSensitivityLevel.UnknownRequiresReview => NodalOsContextReviewCardStatus.RequiresReview,
            _ when boundary.SafeForExport => NodalOsContextReviewCardStatus.SafeForExport,
            _ when boundary.SafeForDisplay => NodalOsContextReviewCardStatus.SafeForDisplay,
            _ => NodalOsContextReviewCardStatus.RequiresReview
        };

    private static IReadOnlyList<string> StaticQuestionsFor(NodalOsUserContextCaptureType captureType) => captureType switch
    {
        NodalOsUserContextCaptureType.UserTechStack => ["Which versions matter?", "Which runtime is authoritative?"],
        NodalOsUserContextCaptureType.UserFolderStructureHint => ["Which folders are important?", "Which folders should be ignored?"],
        NodalOsUserContextCaptureType.UserImportantFileHint => ["Why is this file important?", "Should this remain ref-only?"],
        NodalOsUserContextCaptureType.UserRiskNote => ["What impact does this risk have?", "Is human review needed?"],
        _ => ["What should Mission Control remember?", "What evidence ref supports this claim?"]
    };

    private static NodalOsCoreRuntimeValidationResult ValidationResult(IReadOnlyList<string> errors) => new()
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

public sealed class NodalOsUserContextValidator
{
    private readonly NodalOsRedactionService redaction = new();
    private readonly NodalOsEvidenceRefBridge evidenceBridge = new();
    private readonly NodalOsWorkspaceReadinessValidator boundaryValidator = new();

    public NodalOsCoreRuntimeValidationResult ValidateCapture(NodalOsUserProvidedContextCapture capture)
    {
        var errors = new List<string>();
        AddRequired(errors, capture.ContextCaptureId, "ContextCaptureId is required.");
        AddRequired(errors, capture.WorkspaceId, "WorkspaceId is required.");
        AddRequired(errors, capture.SubmittedByActorRedacted, "SubmittedByActorRedacted is required.");
        AddRequired(errors, capture.DeclaredProvenanceRedacted, "DeclaredProvenanceRedacted is required.");
        if (!capture.UserProvidedOnly)
            errors.Add("Capture must be user-provided only.");
        if (capture.FilesystemVerificationAllowed || capture.ReadsFiles || capture.UsesGit || capture.CreatesVectorIndex || capture.CallsLlmProvider || capture.CreatesPrompt || capture.CreatesRealProjectUnderstanding || capture.CanAuthorizeExecution || capture.ChangesWorkspaceProductively)
            errors.Add("Capture cannot verify filesystem, read files, use git, create vectors/prompts, call providers, create real understanding, authorize execution, or mutate workspace.");
        ValidateText(errors, "ContentRedacted", capture.ContentRedacted);
        ValidateText(errors, "MetadataRedacted", capture.MetadataRedacted);
        ValidateText(errors, "DeclaredProvenanceRedacted", capture.DeclaredProvenanceRedacted);
        ValidateTexts(errors, "AllowedUsageRedacted", capture.AllowedUsageRedacted);
        ValidateTexts(errors, "DisallowedUsageRedacted", capture.DisallowedUsageRedacted);
        ValidateTexts(errors, "MissingInformationRedacted", capture.MissingInformationRedacted);
        ValidateTexts(errors, "StaticQuestionsRedacted", capture.StaticQuestionsRedacted);
        ValidateEvidence(errors, capture.EvidenceRefs);
        errors.AddRange(boundaryValidator.ValidateContextBoundary(capture.BoundaryDecision).Errors);
        return Result(errors);
    }

    public NodalOsCoreRuntimeValidationResult ValidateReviewCard(NodalOsContextReviewCard card)
    {
        var errors = new List<string>();
        AddRequired(errors, card.ReviewCardId, "ReviewCardId is required.");
        AddRequired(errors, card.ContextCaptureId, "ContextCaptureId is required.");
        AddRequired(errors, card.WorkspaceId, "WorkspaceId is required.");
        AddRequired(errors, card.CardTitleRedacted, "CardTitleRedacted is required.");
        if (!card.UserOptionsAreNoOp || !card.UserProvidedAndNotVerified)
            errors.Add("Review card options must be no-op and context must be user-provided/not verified.");
        if (card.CanAuthorizeExecution || card.RuntimeExecutionAllowed || card.CallsLlmProvider || card.CreatesPrompt || card.MutatesRuntime)
            errors.Add("Review card cannot authorize execution, runtime, provider calls, prompts, or runtime mutation.");
        ValidateText(errors, "CardTitleRedacted", card.CardTitleRedacted);
        ValidateText(errors, "SummaryRedacted", card.SummaryRedacted);
        ValidateText(errors, "DetailsRedacted", card.DetailsRedacted);
        ValidateTexts(errors, "AllowedUsageChipsRedacted", card.AllowedUsageChipsRedacted);
        ValidateTexts(errors, "DisallowedUsageChipsRedacted", card.DisallowedUsageChipsRedacted);
        ValidateTexts(errors, "MissingInformationRedacted", card.MissingInformationRedacted);
        ValidateTexts(errors, "QuestionsForUserRedacted", card.QuestionsForUserRedacted);
        ValidateTexts(errors, "WarningsRedacted", card.WarningsRedacted);
        ValidateEvidence(errors, card.EvidenceRefs);
        return Result(errors);
    }

    public NodalOsCoreRuntimeValidationResult ValidateEvidenceLink(NodalOsContextEvidenceLink link, NodalOsEvidenceBridgeRef? evidenceRef = null)
    {
        var errors = new List<string>();
        AddRequired(errors, link.LinkId, "LinkId is required.");
        AddRequired(errors, link.ContextCaptureId, "ContextCaptureId is required.");
        AddRequired(errors, link.EvidenceRefId, "EvidenceRefId is required.");
        AddRequired(errors, link.WorkspaceId, "WorkspaceId is required.");
        if (!link.RefOnly || link.IncludesRawPayload || link.IncludesScreenshotInline || link.IncludesDomRaw || link.IncludesNetworkRaw)
            errors.Add("Context evidence link must be ref-only and cannot include raw payload, screenshot inline, DOM raw, or network raw.");
        if (link.ReadsFiles || link.ValidatesRealContent || link.ConvertsClaimToAuthoritativeTruth || link.CanAuthorizeExecution || link.CallsLlmProvider || link.CallsCloud)
            errors.Add("Context evidence link cannot read files, validate real content, convert claim to truth, authorize execution, call provider, or call cloud.");
        ValidateText(errors, "LinkReasonRedacted", link.LinkReasonRedacted);
        ValidateText(errors, "ProvenanceRedacted", link.ProvenanceRedacted);
        if (evidenceRef is not null)
        {
            var result = evidenceBridge.ValidateBridgeRef(evidenceRef);
            if (!result.Accepted)
                errors.AddRange(result.Errors);
        }
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
        if (ContainsRawPayloadMarker(value))
            errors.Add($"{fieldName} contains raw payload marker.");
    }

    private static bool ContainsRawPayloadMarker(string value)
    {
        var lower = value.ToLowerInvariant();
        return lower.Contains("authorization:", StringComparison.Ordinal) ||
            lower.Contains("cookie:", StringComparison.Ordinal) ||
            lower.Contains("set-cookie:", StringComparison.Ordinal) ||
            lower.Contains("private key", StringComparison.Ordinal) ||
            lower.Contains("raw payload", StringComparison.Ordinal) ||
            lower.Contains("<html", StringComparison.Ordinal) ||
            lower.Contains("<input", StringComparison.Ordinal);
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

public sealed class NodalOsUserContextJsonSerializer
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
        WriteIndented = true
    };

    public string SerializeCapture(NodalOsUserProvidedContextCapture capture) => JsonSerializer.Serialize(capture, Options);
    public string SerializeReviewCard(NodalOsContextReviewCard card) => JsonSerializer.Serialize(card, Options);
    public string SerializeEvidenceLink(NodalOsContextEvidenceLink link) => JsonSerializer.Serialize(link, Options);
}

public static class NodalOsUserContextFixtures
{
    public static NodalOsUserProvidedContextCapture UserSummaryCapture() =>
        new NodalOsUserContextService().CreateCapture(NodalOsWorkspaceFixtures.ActiveReadOnlyWorkspace(), NodalOsUserContextCaptureType.UserSummary);

    public static NodalOsContextReviewCard SafeReviewCard() =>
        new NodalOsUserContextService().CreateReviewCard(UserSummaryCapture());

    public static NodalOsContextEvidenceLink EvidenceLink()
    {
        var capture = UserSummaryCapture();
        return new NodalOsUserContextService().CreateEvidenceLink(capture, SafeReviewCard(), capture.EvidenceRefs[0]);
    }
}
