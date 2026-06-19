using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using OneBrain.AgentOperations.Contracts;

namespace OneBrain.AgentOperations.Core;

public sealed class NodalOsApprovalUxHandoffObservabilityValidator
{
    private readonly NodalOsRedactionService redaction;
    private readonly NodalOsApprovalTimelineEvidenceValidator approvalTimelineValidator;

    public NodalOsApprovalUxHandoffObservabilityValidator()
        : this(new NodalOsRedactionService())
    {
    }

    public NodalOsApprovalUxHandoffObservabilityValidator(NodalOsRedactionService redaction)
        : this(redaction, new NodalOsApprovalTimelineEvidenceValidator(redaction))
    {
    }

    public NodalOsApprovalUxHandoffObservabilityValidator(
        NodalOsRedactionService redaction,
        NodalOsApprovalTimelineEvidenceValidator approvalTimelineValidator)
    {
        this.redaction = redaction;
        this.approvalTimelineValidator = approvalTimelineValidator;
    }

    public NodalOsCoreRuntimeValidationResult ValidateApprovalCardPreview(NodalOsApprovalCardPreview preview)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        AddRequired(errors, preview.PreviewCardId, "PreviewCardId is required.");
        AddRequired(errors, preview.ApprovalCardId, "ApprovalCardId is required.");
        AddRequired(errors, preview.TitleRedacted, "TitleRedacted is required.");
        AddRequired(errors, preview.ShortSummaryRedacted, "ShortSummaryRedacted is required.");
        AddRequired(errors, preview.FullExplanationRedacted, "FullExplanationRedacted is required.");
        AddRequired(errors, preview.PolicyGateReasonRedacted, "PolicyGateReasonRedacted is required.");
        AddRequired(errors, preview.ExpectedEvidenceRedacted, "ExpectedEvidenceRedacted is required.");
        if (!preview.RollbackAvailable)
            AddRequired(errors, preview.NoRollbackReasonRedacted, "NoRollbackReasonRedacted is required when rollback is unavailable.");
        if (preview.CreatedAt == default)
            errors.Add("CreatedAt is required.");

        ValidateNoAuthority(preview.RuntimeExecutionAllowed, preview.RuntimeExecutionDeferred, preview.CanAuthorizeExecution, errors);
        if (preview.UserOptions.Count == 0)
            errors.Add("Approval UX preview requires user options.");
        if (preview.AffectedResourcesRedacted.Count == 0)
            errors.Add("Approval UX preview requires affected resources or a safe no-resource explanation in the source approval card.");

        ValidateSafeText(errors, "TitleRedacted", preview.TitleRedacted);
        ValidateSafeText(errors, "ShortSummaryRedacted", preview.ShortSummaryRedacted);
        ValidateSafeText(errors, "FullExplanationRedacted", preview.FullExplanationRedacted);
        ValidateSafeText(errors, "PolicyGateReasonRedacted", preview.PolicyGateReasonRedacted);
        ValidateSafeText(errors, "NoRollbackReasonRedacted", preview.NoRollbackReasonRedacted);
        ValidateSafeText(errors, "ExpectedEvidenceRedacted", preview.ExpectedEvidenceRedacted);
        foreach (var resource in preview.AffectedResourcesRedacted)
            ValidateSafeText(errors, "AffectedResourcesRedacted", resource);
        ValidateEvidenceRefs(preview.EvidenceRefs, errors, warnings);

        return Result(errors, warnings);
    }

    public NodalOsCoreRuntimeValidationResult ValidateApprovalUxPreview(NodalOsApprovalUxPreview preview)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        AddRequired(errors, preview.PreviewId, "PreviewId is required.");
        if (!string.Equals(preview.ProjectOperationalName, "NODAL OS", StringComparison.Ordinal))
            errors.Add("Project operational name must be NODAL OS.");
        if (preview.CreatedAt == default)
            errors.Add("CreatedAt is required.");
        ValidateNoAuthority(preview.RuntimeExecutionAllowed, preview.RuntimeExecutionDeferred, preview.CanAuthorizeExecution, errors);
        if (preview.Cards.Count == 0)
            errors.Add("Approval UX preview requires at least one card preview.");

        foreach (var card in preview.Cards)
            Merge(ValidateApprovalCardPreview(card), errors, warnings);
        foreach (var entry in preview.TimelineEntries)
            Merge(approvalTimelineValidator.ValidateTimelineEntry(entry), errors, warnings);

        return Result(errors, warnings);
    }

    public NodalOsCoreRuntimeValidationResult ValidateHandoffDataPack(NodalOsHandoffDataPack pack)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        AddRequired(errors, pack.PackId, "PackId is required.");
        if (!string.Equals(pack.ProjectOperationalName, "NODAL OS", StringComparison.Ordinal))
            errors.Add("Project operational name must be NODAL OS.");
        AddRequired(errors, pack.RequestedSummaryRedacted, "RequestedSummaryRedacted is required.");
        AddRequired(errors, pack.DecisionSummaryRedacted, "DecisionSummaryRedacted is required.");
        AddRequired(errors, pack.RedactionSummaryRedacted, "RedactionSummaryRedacted is required.");
        AddRequired(errors, pack.GuardrailsSummaryRedacted, "GuardrailsSummaryRedacted is required.");
        if (pack.CreatedAt == default)
            errors.Add("CreatedAt is required.");
        if (pack.CloudRequired)
            errors.Add("Handoff data pack cannot require cloud.");
        ValidateNoRuntime(pack.RuntimeExecutionAllowed, pack.RuntimeExecutionDeferred, errors);

        ValidateSafeText(errors, "RequestedSummaryRedacted", pack.RequestedSummaryRedacted);
        ValidateSafeText(errors, "DecisionSummaryRedacted", pack.DecisionSummaryRedacted);
        ValidateSafeText(errors, "RedactionSummaryRedacted", pack.RedactionSummaryRedacted);
        ValidateSafeText(errors, "GuardrailsSummaryRedacted", pack.GuardrailsSummaryRedacted);
        ValidateSafeText(errors, "TestsValidationSummaryRedacted", pack.TestsValidationSummaryRedacted);
        ValidateSafeList(errors, "ProposedActionsRedacted", pack.ProposedActionsRedacted);
        ValidateSafeList(errors, "PendingItemsRedacted", pack.PendingItemsRedacted);
        ValidateSafeList(errors, "WarningsRedacted", pack.WarningsRedacted);
        ValidateSafeList(errors, "FailuresRedacted", pack.FailuresRedacted);
        ValidateSafeList(errors, "HumanHandoffRequirementsRedacted", pack.HumanHandoffRequirementsRedacted);
        ValidateSafeList(errors, "NextStepsRedacted", pack.NextStepsRedacted);

        foreach (var entry in pack.RegistryEntries)
            ValidateNoRuntime(entry.RuntimeExecutionAllowed, entry.RuntimeExecutionDeferred, errors);
        foreach (var preview in pack.ApprovalPreviews)
            Merge(ValidateApprovalCardPreview(preview), errors, warnings);
        foreach (var decision in pack.ApprovalDecisions)
            Merge(approvalTimelineValidator.ValidateApprovalDecision(decision), errors, warnings);
        foreach (var entry in pack.TimelineEntries)
            Merge(approvalTimelineValidator.ValidateTimelineEntry(entry), errors, warnings);
        ValidateEvidenceRefs(pack.EvidenceRefs, errors, warnings);

        return Result(errors, warnings);
    }

    public NodalOsCoreRuntimeValidationResult ValidateRuntimeObservabilityReport(NodalOsRuntimeObservabilityReport report)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        AddRequired(errors, report.ReportId, "ReportId is required.");
        if (!string.Equals(report.ProjectOperationalName, "NODAL OS", StringComparison.Ordinal))
            errors.Add("Project operational name must be NODAL OS.");
        AddRequired(errors, report.UserRequestRedacted, "UserRequestRedacted is required.");
        AddRequired(errors, report.SystemInterpretationRedacted, "SystemInterpretationRedacted is required.");
        AddRequired(errors, report.ExecutionRegistrySummaryRedacted, "ExecutionRegistrySummaryRedacted is required.");
        AddRequired(errors, report.EventBusSummaryRedacted, "EventBusSummaryRedacted is required.");
        AddRequired(errors, report.TimelineSummaryRedacted, "TimelineSummaryRedacted is required.");
        AddRequired(errors, report.ApprovalSummaryRedacted, "ApprovalSummaryRedacted is required.");
        AddRequired(errors, report.EvidenceSummaryRedacted, "EvidenceSummaryRedacted is required.");
        AddRequired(errors, report.RedactionAppliedSummaryRedacted, "RedactionAppliedSummaryRedacted is required.");
        AddRequired(errors, report.GuardrailsSummaryRedacted, "GuardrailsSummaryRedacted is required.");
        AddRequired(errors, report.ValidationSummaryRedacted, "ValidationSummaryRedacted is required.");
        AddRequired(errors, report.NextRecommendedActionRedacted, "NextRecommendedActionRedacted is required.");
        if (report.CreatedAt == default)
            errors.Add("CreatedAt is required.");
        if (report.UiRequired)
            errors.Add("Runtime observability report cannot require UI.");
        if (report.CloudRequired)
            errors.Add("Runtime observability report cannot require cloud.");
        if (report.LlmProviderCallRequired)
            errors.Add("Runtime observability report cannot require LLM provider calls.");
        ValidateNoRuntime(report.RuntimeExecutionAllowed, report.RuntimeExecutionDeferred, errors);

        ValidateSafeText(errors, "UserRequestRedacted", report.UserRequestRedacted);
        ValidateSafeText(errors, "SystemInterpretationRedacted", report.SystemInterpretationRedacted);
        ValidateSafeText(errors, "ExecutionRegistrySummaryRedacted", report.ExecutionRegistrySummaryRedacted);
        ValidateSafeText(errors, "EventBusSummaryRedacted", report.EventBusSummaryRedacted);
        ValidateSafeText(errors, "TimelineSummaryRedacted", report.TimelineSummaryRedacted);
        ValidateSafeText(errors, "ApprovalSummaryRedacted", report.ApprovalSummaryRedacted);
        ValidateSafeText(errors, "EvidenceSummaryRedacted", report.EvidenceSummaryRedacted);
        ValidateSafeText(errors, "RedactionAppliedSummaryRedacted", report.RedactionAppliedSummaryRedacted);
        ValidateSafeText(errors, "GuardrailsSummaryRedacted", report.GuardrailsSummaryRedacted);
        ValidateSafeText(errors, "ValidationSummaryRedacted", report.ValidationSummaryRedacted);
        ValidateSafeText(errors, "NextRecommendedActionRedacted", report.NextRecommendedActionRedacted);
        ValidateSafeList(errors, "BlockedActionsRedacted", report.BlockedActionsRedacted);
        ValidateSafeList(errors, "FailuresRedacted", report.FailuresRedacted);
        ValidateSafeList(errors, "WarningsRedacted", report.WarningsRedacted);
        ValidateSafeList(errors, "HumanHandoffRequirementsRedacted", report.HumanHandoffRequirementsRedacted);

        return Result(errors, warnings);
    }

    public string Redact(string? value) => redaction.RedactValue(value).Value;

    private void ValidateEvidenceRefs(
        IReadOnlyList<NodalOsEvidenceBridgeRef> evidenceRefs,
        List<string> errors,
        List<string> warnings)
    {
        foreach (var evidenceRef in evidenceRefs)
        {
            var attachment = new NodalOsEvidenceRegistryAttachment
            {
                AttachmentId = $"validation-{Guid.NewGuid():N}",
                AttachmentKind = NodalOsEvidenceAttachmentKind.CoreEvent,
                EventId = "validation-event",
                EvidenceRef = evidenceRef,
                RawPayloadPersisted = false,
                RequiresEvidenceRedaction = true,
                CreatedAt = DateTimeOffset.UtcNow
            };

            Merge(approvalTimelineValidator.ValidateEvidenceAttachment(attachment), errors, warnings);
        }
    }

    private void ValidateSafeList(List<string> errors, string fieldName, IReadOnlyList<string> values)
    {
        foreach (var value in values)
            ValidateSafeText(errors, fieldName, value);
    }

    private void ValidateSafeText(List<string> errors, string fieldName, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return;

        if (redaction.ContainsSensitiveField(fieldName, value) || redaction.ContainsSensitiveContent(value))
            errors.Add($"{fieldName} contains sensitive content and must be redacted before UX/handoff/observability export.");
    }

    private static void ValidateNoAuthority(
        bool runtimeExecutionAllowed,
        bool runtimeExecutionDeferred,
        bool canAuthorizeExecution,
        List<string> errors)
    {
        ValidateNoRuntime(runtimeExecutionAllowed, runtimeExecutionDeferred, errors);
        if (canAuthorizeExecution)
            errors.Add("Approval UX preview cannot authorize execution.");
    }

    private static void ValidateNoRuntime(
        bool runtimeExecutionAllowed,
        bool runtimeExecutionDeferred,
        List<string> errors)
    {
        if (runtimeExecutionAllowed)
            errors.Add("UX/handoff/observability contracts cannot grant runtime execution.");
        if (!runtimeExecutionDeferred)
            errors.Add("UX/handoff/observability contracts must keep runtime execution deferred.");
    }

    private static void AddRequired(List<string> errors, string? value, string message)
    {
        if (string.IsNullOrWhiteSpace(value))
            errors.Add(message);
    }

    private static void Merge(NodalOsCoreRuntimeValidationResult result, List<string> errors, List<string> warnings)
    {
        errors.AddRange(result.Errors);
        warnings.AddRange(result.Warnings);
    }

    private static NodalOsCoreRuntimeValidationResult Result(List<string> errors, List<string> warnings) =>
        new()
        {
            IsValid = errors.Count == 0,
            RuntimeExecutionAllowed = false,
            RuntimeExecutionDeferred = true,
            RequiresGlobalPolicyEvaluation = true,
            RequiresEvidenceRedaction = true,
            Errors = errors.Distinct(StringComparer.Ordinal).ToArray(),
            Warnings = warnings.Distinct(StringComparer.Ordinal).ToArray()
        };
}

public sealed class NodalOsApprovalUxPreviewService
{
    private readonly NodalOsApprovalUxHandoffObservabilityValidator validator;

    public NodalOsApprovalUxPreviewService()
        : this(new NodalOsApprovalUxHandoffObservabilityValidator())
    {
    }

    public NodalOsApprovalUxPreviewService(NodalOsApprovalUxHandoffObservabilityValidator validator) =>
        this.validator = validator;

    public NodalOsApprovalCardPreview CreateCardPreview(
        NodalOsApprovalCard card,
        IReadOnlyList<NodalOsTimelineEntry> timelineEntries)
    {
        var relatedTimeline = timelineEntries
            .Where(entry => string.Equals(entry.ApprovalCardId, card.ApprovalCardId, StringComparison.Ordinal) ||
                            string.Equals(entry.EventId, card.EventId, StringComparison.Ordinal) ||
                            string.Equals(entry.ExecutionRegistryEntryId, card.ExecutionRegistryEntryId, StringComparison.Ordinal))
            .ToArray();

        return new()
        {
            PreviewCardId = $"approval-preview-card-{Guid.NewGuid():N}",
            ApprovalCardId = card.ApprovalCardId,
            ExecutionRegistryEntryId = card.ExecutionRegistryEntryId,
            EventId = card.EventId,
            TimelineEntryIds = relatedTimeline.Select(entry => entry.TimelineEntryId).ToArray(),
            TitleRedacted = validator.Redact($"{card.RequestedAction} approval required"),
            ShortSummaryRedacted = validator.Redact(card.PolicyGateReasonRedacted),
            FullExplanationRedacted = validator.Redact(card.HumanExplanationRedacted),
            Severity = card.Severity,
            Status = card.Status,
            RequestedAction = card.RequestedAction,
            AffectedResourcesRedacted = card.AffectedResourcesRedacted.Select(validator.Redact).ToArray(),
            PolicyGateReasonRedacted = validator.Redact(card.PolicyGateReasonRedacted),
            UserOptions = card.UserOptions,
            RollbackAvailable = !string.IsNullOrWhiteSpace(card.RollbackPlanRedacted),
            NoRollbackReasonRedacted = string.IsNullOrWhiteSpace(card.RollbackPlanRedacted)
                ? "No rollback plan is available in this contract preview."
                : null,
            ExpectedEvidenceRedacted = validator.Redact(card.EvidencePlanRedacted ?? "Evidence refs and timeline entries will be retained."),
            RequiresAttention = card.Status is NodalOsApprovalStatus.PendingHumanDecision or NodalOsApprovalStatus.HumanHandoffRequired,
            Blocked = card.Status is NodalOsApprovalStatus.Rejected or NodalOsApprovalStatus.HumanHandoffRequired,
            RequiresHuman = card.Status is NodalOsApprovalStatus.PendingHumanDecision or NodalOsApprovalStatus.HumanHandoffRequired,
            RuntimeExecutionAllowed = false,
            RuntimeExecutionDeferred = true,
            CanAuthorizeExecution = false,
            EvidenceRefs = card.EvidenceRefs.Concat(relatedTimeline.SelectMany(entry => entry.EvidenceRefs)).DistinctBy(e => e.EvidenceId).ToArray(),
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public NodalOsApprovalUxPreview CreatePreview(
        IReadOnlyList<NodalOsApprovalCard> cards,
        IReadOnlyList<NodalOsTimelineEntry> timelineEntries) =>
        new()
        {
            PreviewId = $"approval-ux-preview-{Guid.NewGuid():N}",
            ProjectOperationalName = "NODAL OS",
            Cards = cards.Select(card => CreateCardPreview(card, timelineEntries)).ToArray(),
            TimelineEntries = timelineEntries,
            RuntimeExecutionAllowed = false,
            RuntimeExecutionDeferred = true,
            CanAuthorizeExecution = false,
            CreatedAt = DateTimeOffset.UtcNow
        };
}

public sealed class NodalOsHandoffDataPackService
{
    private readonly NodalOsApprovalUxHandoffObservabilityValidator validator;

    public NodalOsHandoffDataPackService()
        : this(new NodalOsApprovalUxHandoffObservabilityValidator())
    {
    }

    public NodalOsHandoffDataPackService(NodalOsApprovalUxHandoffObservabilityValidator validator) =>
        this.validator = validator;

    public NodalOsHandoffDataPack CreateDataPack(
        IReadOnlyList<NodalOsExecutionRegistryEntry> registryEntries,
        IReadOnlyList<NodalOsApprovalCardPreview> approvalPreviews,
        IReadOnlyList<NodalOsApprovalDecision> approvalDecisions,
        IReadOnlyList<NodalOsTimelineEntry> timelineEntries,
        string requestedSummary,
        string decisionSummary,
        IReadOnlyList<string>? proposedActions = null,
        IReadOnlyList<string>? pendingItems = null,
        IReadOnlyList<string>? warnings = null,
        IReadOnlyList<string>? failures = null,
        IReadOnlyList<string>? humanHandoffRequirements = null,
        IReadOnlyList<string>? nextSteps = null,
        string? milestone = null,
        string? currentDecision = null,
        string? testsValidationSummary = null) =>
        new()
        {
            PackId = $"handoff-pack-{Guid.NewGuid():N}",
            ProjectOperationalName = "NODAL OS",
            Milestone = milestone,
            CurrentDecision = currentDecision,
            RequestedSummaryRedacted = validator.Redact(requestedSummary),
            DecisionSummaryRedacted = validator.Redact(decisionSummary),
            ProposedActionsRedacted = RedactList(proposedActions),
            PendingItemsRedacted = RedactList(pendingItems),
            RegistryEntries = registryEntries,
            ApprovalPreviews = approvalPreviews,
            ApprovalDecisions = approvalDecisions,
            TimelineEntries = timelineEntries,
            EvidenceRefs = registryEntries.SelectMany(entry => entry.EvidenceRefs)
                .Concat(approvalPreviews.SelectMany(preview => preview.EvidenceRefs))
                .Concat(approvalDecisions.SelectMany(decision => decision.EvidenceRefs))
                .Concat(timelineEntries.SelectMany(entry => entry.EvidenceRefs))
                .DistinctBy(e => e.EvidenceId)
                .ToArray(),
            WarningsRedacted = RedactList(warnings),
            FailuresRedacted = RedactList(failures),
            HumanHandoffRequirementsRedacted = RedactList(humanHandoffRequirements),
            RedactionSummaryRedacted = "All export-facing fields are redacted through NODAL OS common redaction.",
            GuardrailsSummaryRedacted = "No runtime, UI, cloud, LLM, scheduler, worker, browser automation, recorder, replay, queue, or DSL parser is introduced.",
            TestsValidationSummaryRedacted = validator.Redact(testsValidationSummary),
            NextStepsRedacted = RedactList(nextSteps),
            CloudRequired = false,
            RuntimeExecutionAllowed = false,
            RuntimeExecutionDeferred = true,
            CreatedAt = DateTimeOffset.UtcNow
        };

    public string RenderMarkdown(NodalOsHandoffDataPack pack)
    {
        var safe = Sanitize(pack);
        var builder = new StringBuilder();
        builder.AppendLine($"# NODAL OS Handoff Data Pack {safe.PackId}");
        builder.AppendLine();
        builder.AppendLine($"- Requested: {safe.RequestedSummaryRedacted}");
        builder.AppendLine($"- Decision: {safe.DecisionSummaryRedacted}");
        builder.AppendLine($"- Registry entries: {safe.RegistryEntries.Count}");
        builder.AppendLine($"- Timeline entries: {safe.TimelineEntries.Count}");
        builder.AppendLine($"- Approval previews: {safe.ApprovalPreviews.Count}");
        builder.AppendLine($"- Evidence refs: {safe.EvidenceRefs.Count}");
        builder.AppendLine($"- Guardrails: {safe.GuardrailsSummaryRedacted}");
        builder.AppendLine($"- Safe to share: evidence is ref-only and text is redacted.");
        return builder.ToString();
    }

    private NodalOsHandoffDataPack Sanitize(NodalOsHandoffDataPack pack) =>
        pack with
        {
            RequestedSummaryRedacted = validator.Redact(pack.RequestedSummaryRedacted),
            DecisionSummaryRedacted = validator.Redact(pack.DecisionSummaryRedacted),
            ProposedActionsRedacted = RedactList(pack.ProposedActionsRedacted),
            PendingItemsRedacted = RedactList(pack.PendingItemsRedacted),
            WarningsRedacted = RedactList(pack.WarningsRedacted),
            FailuresRedacted = RedactList(pack.FailuresRedacted),
            HumanHandoffRequirementsRedacted = RedactList(pack.HumanHandoffRequirementsRedacted),
            RedactionSummaryRedacted = validator.Redact(pack.RedactionSummaryRedacted),
            GuardrailsSummaryRedacted = validator.Redact(pack.GuardrailsSummaryRedacted),
            TestsValidationSummaryRedacted = validator.Redact(pack.TestsValidationSummaryRedacted),
            NextStepsRedacted = RedactList(pack.NextStepsRedacted),
            CloudRequired = false,
            RuntimeExecutionAllowed = false,
            RuntimeExecutionDeferred = true
        };

    private IReadOnlyList<string> RedactList(IReadOnlyList<string>? values) =>
        (values ?? Array.Empty<string>()).Select(validator.Redact).ToArray();
}

public sealed class NodalOsRuntimeObservabilityReportService
{
    private readonly NodalOsApprovalUxHandoffObservabilityValidator validator;

    public NodalOsRuntimeObservabilityReportService()
        : this(new NodalOsApprovalUxHandoffObservabilityValidator())
    {
    }

    public NodalOsRuntimeObservabilityReportService(NodalOsApprovalUxHandoffObservabilityValidator validator) =>
        this.validator = validator;

    public NodalOsRuntimeObservabilityReport CreateReport(
        string userRequest,
        string systemInterpretation,
        IReadOnlyList<NodalOsExecutionRegistryEntry> registryEntries,
        IReadOnlyList<NodalOsCoreEvent> coreEvents,
        IReadOnlyList<NodalOsTimelineEntry> timelineEntries,
        IReadOnlyList<NodalOsApprovalCardPreview> approvalPreviews,
        IReadOnlyList<NodalOsEvidenceBridgeRef> evidenceRefs,
        IReadOnlyList<string>? blockedActions = null,
        IReadOnlyList<string>? failures = null,
        IReadOnlyList<string>? warnings = null,
        IReadOnlyList<string>? humanHandoffRequirements = null,
        string validationSummary = "Contract validation completed.",
        string nextRecommendedAction = "Run AUDIT-A before UI real or Mission Control shell.") =>
        new()
        {
            ReportId = $"runtime-observability-report-{Guid.NewGuid():N}",
            ProjectOperationalName = "NODAL OS",
            UserRequestRedacted = validator.Redact(userRequest),
            SystemInterpretationRedacted = validator.Redact(systemInterpretation),
            RegistryEntryIds = registryEntries.Select(entry => entry.RegistryEntryId).ToArray(),
            EventIds = coreEvents.Select(coreEvent => coreEvent.EventId).ToArray(),
            TimelineEntryIds = timelineEntries.Select(entry => entry.TimelineEntryId).ToArray(),
            ApprovalCardIds = approvalPreviews.Select(preview => preview.ApprovalCardId).ToArray(),
            EvidenceIds = evidenceRefs.Select(evidenceRef => evidenceRef.EvidenceId).ToArray(),
            ExecutionRegistrySummaryRedacted = validator.Redact($"{registryEntries.Count} registry entries retained without execution authority."),
            EventBusSummaryRedacted = validator.Redact($"{coreEvents.Count} canonical events retained without side effects."),
            TimelineSummaryRedacted = validator.Redact($"{timelineEntries.Count} timeline entries projected from canonical events."),
            ApprovalSummaryRedacted = validator.Redact($"{approvalPreviews.Count} approval previews prepared for future UI."),
            EvidenceSummaryRedacted = validator.Redact($"{evidenceRefs.Count} evidence refs retained as metadata/ref-only."),
            RedactionAppliedSummaryRedacted = "Common redaction applied to all report-facing text fields.",
            GuardrailsSummaryRedacted = "No runtime, UI, cloud, LLM, scheduler, worker, browser automation, recorder, replay, queue, or DSL parser is introduced.",
            BlockedActionsRedacted = RedactList(blockedActions),
            FailuresRedacted = RedactList(failures),
            WarningsRedacted = RedactList(warnings),
            HumanHandoffRequirementsRedacted = RedactList(humanHandoffRequirements),
            ValidationSummaryRedacted = validator.Redact(validationSummary),
            NextRecommendedActionRedacted = validator.Redact(nextRecommendedAction),
            RuntimeExecutionAllowed = false,
            RuntimeExecutionDeferred = true,
            UiRequired = false,
            CloudRequired = false,
            LlmProviderCallRequired = false,
            CreatedAt = DateTimeOffset.UtcNow
        };

    public string RenderText(NodalOsRuntimeObservabilityReport report)
    {
        var safe = Sanitize(report);
        var builder = new StringBuilder();
        builder.AppendLine($"NODAL OS Runtime Observability Report: {safe.ReportId}");
        builder.AppendLine($"Request: {safe.UserRequestRedacted}");
        builder.AppendLine($"Interpretation: {safe.SystemInterpretationRedacted}");
        builder.AppendLine($"Registry: {safe.ExecutionRegistrySummaryRedacted}");
        builder.AppendLine($"Events: {safe.EventBusSummaryRedacted}");
        builder.AppendLine($"Timeline: {safe.TimelineSummaryRedacted}");
        builder.AppendLine($"Approval: {safe.ApprovalSummaryRedacted}");
        builder.AppendLine($"Evidence: {safe.EvidenceSummaryRedacted}");
        builder.AppendLine($"Guardrails: {safe.GuardrailsSummaryRedacted}");
        builder.AppendLine($"Next: {safe.NextRecommendedActionRedacted}");
        return builder.ToString();
    }

    private NodalOsRuntimeObservabilityReport Sanitize(NodalOsRuntimeObservabilityReport report) =>
        report with
        {
            UserRequestRedacted = validator.Redact(report.UserRequestRedacted),
            SystemInterpretationRedacted = validator.Redact(report.SystemInterpretationRedacted),
            ExecutionRegistrySummaryRedacted = validator.Redact(report.ExecutionRegistrySummaryRedacted),
            EventBusSummaryRedacted = validator.Redact(report.EventBusSummaryRedacted),
            TimelineSummaryRedacted = validator.Redact(report.TimelineSummaryRedacted),
            ApprovalSummaryRedacted = validator.Redact(report.ApprovalSummaryRedacted),
            EvidenceSummaryRedacted = validator.Redact(report.EvidenceSummaryRedacted),
            RedactionAppliedSummaryRedacted = validator.Redact(report.RedactionAppliedSummaryRedacted),
            GuardrailsSummaryRedacted = validator.Redact(report.GuardrailsSummaryRedacted),
            BlockedActionsRedacted = RedactList(report.BlockedActionsRedacted),
            FailuresRedacted = RedactList(report.FailuresRedacted),
            WarningsRedacted = RedactList(report.WarningsRedacted),
            HumanHandoffRequirementsRedacted = RedactList(report.HumanHandoffRequirementsRedacted),
            ValidationSummaryRedacted = validator.Redact(report.ValidationSummaryRedacted),
            NextRecommendedActionRedacted = validator.Redact(report.NextRecommendedActionRedacted),
            RuntimeExecutionAllowed = false,
            RuntimeExecutionDeferred = true,
            UiRequired = false,
            CloudRequired = false,
            LlmProviderCallRequired = false
        };

    private IReadOnlyList<string> RedactList(IReadOnlyList<string>? values) =>
        (values ?? Array.Empty<string>()).Select(validator.Redact).ToArray();
}

public sealed class NodalOsApprovalUxHandoffObservabilityJsonSerializer
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
        WriteIndented = true
    };

    private readonly NodalOsApprovalUxHandoffObservabilityValidator validator;
    private readonly NodalOsCoreRuntimeJsonSerializer coreSerializer = new();
    private readonly NodalOsApprovalTimelineEvidenceJsonSerializer approvalTimelineSerializer = new();

    public NodalOsApprovalUxHandoffObservabilityJsonSerializer()
        : this(new NodalOsApprovalUxHandoffObservabilityValidator())
    {
    }

    public NodalOsApprovalUxHandoffObservabilityJsonSerializer(NodalOsApprovalUxHandoffObservabilityValidator validator) =>
        this.validator = validator;

    public string SerializeApprovalUxPreview(NodalOsApprovalUxPreview preview) =>
        JsonSerializer.Serialize(Sanitize(preview), Options);

    public NodalOsApprovalUxPreview? DeserializeApprovalUxPreview(string json) =>
        JsonSerializer.Deserialize<NodalOsApprovalUxPreview>(json, Options);

    public string SerializeHandoffDataPack(NodalOsHandoffDataPack pack) =>
        JsonSerializer.Serialize(Sanitize(pack), Options);

    public NodalOsHandoffDataPack? DeserializeHandoffDataPack(string json) =>
        JsonSerializer.Deserialize<NodalOsHandoffDataPack>(json, Options);

    public string SerializeRuntimeObservabilityReport(NodalOsRuntimeObservabilityReport report) =>
        JsonSerializer.Serialize(Sanitize(report), Options);

    public NodalOsRuntimeObservabilityReport? DeserializeRuntimeObservabilityReport(string json) =>
        JsonSerializer.Deserialize<NodalOsRuntimeObservabilityReport>(json, Options);

    private NodalOsApprovalUxPreview Sanitize(NodalOsApprovalUxPreview preview) =>
        preview with
        {
            ProjectOperationalName = "NODAL OS",
            RuntimeExecutionAllowed = false,
            RuntimeExecutionDeferred = true,
            CanAuthorizeExecution = false,
            Cards = preview.Cards.Select(Sanitize).ToArray()
        };

    private NodalOsApprovalCardPreview Sanitize(NodalOsApprovalCardPreview preview) =>
        preview with
        {
            TitleRedacted = validator.Redact(preview.TitleRedacted),
            ShortSummaryRedacted = validator.Redact(preview.ShortSummaryRedacted),
            FullExplanationRedacted = validator.Redact(preview.FullExplanationRedacted),
            AffectedResourcesRedacted = preview.AffectedResourcesRedacted.Select(validator.Redact).ToArray(),
            PolicyGateReasonRedacted = validator.Redact(preview.PolicyGateReasonRedacted),
            NoRollbackReasonRedacted = validator.Redact(preview.NoRollbackReasonRedacted),
            ExpectedEvidenceRedacted = validator.Redact(preview.ExpectedEvidenceRedacted),
            RuntimeExecutionAllowed = false,
            RuntimeExecutionDeferred = true,
            CanAuthorizeExecution = false
        };

    private NodalOsHandoffDataPack Sanitize(NodalOsHandoffDataPack pack) =>
        pack with
        {
            ProjectOperationalName = "NODAL OS",
            RequestedSummaryRedacted = validator.Redact(pack.RequestedSummaryRedacted),
            DecisionSummaryRedacted = validator.Redact(pack.DecisionSummaryRedacted),
            ProposedActionsRedacted = pack.ProposedActionsRedacted.Select(validator.Redact).ToArray(),
            PendingItemsRedacted = pack.PendingItemsRedacted.Select(validator.Redact).ToArray(),
            RegistryEntries = pack.RegistryEntries.Select(SanitizeRegistryEntry).ToArray(),
            ApprovalPreviews = pack.ApprovalPreviews.Select(Sanitize).ToArray(),
            ApprovalDecisions = pack.ApprovalDecisions.Select(SanitizeApprovalDecision).ToArray(),
            TimelineEntries = pack.TimelineEntries.Select(SanitizeTimelineEntry).ToArray(),
            WarningsRedacted = pack.WarningsRedacted.Select(validator.Redact).ToArray(),
            FailuresRedacted = pack.FailuresRedacted.Select(validator.Redact).ToArray(),
            HumanHandoffRequirementsRedacted = pack.HumanHandoffRequirementsRedacted.Select(validator.Redact).ToArray(),
            RedactionSummaryRedacted = validator.Redact(pack.RedactionSummaryRedacted),
            GuardrailsSummaryRedacted = validator.Redact(pack.GuardrailsSummaryRedacted),
            TestsValidationSummaryRedacted = validator.Redact(pack.TestsValidationSummaryRedacted),
            NextStepsRedacted = pack.NextStepsRedacted.Select(validator.Redact).ToArray(),
            CloudRequired = false,
            RuntimeExecutionAllowed = false,
            RuntimeExecutionDeferred = true
        };

    private NodalOsExecutionRegistryEntry SanitizeRegistryEntry(NodalOsExecutionRegistryEntry entry) =>
        coreSerializer.DeserializeEntry(coreSerializer.SerializeEntry(entry)) ?? entry with
        {
            RuntimeExecutionAllowed = false,
            RuntimeExecutionDeferred = true
        };

    private NodalOsApprovalDecision SanitizeApprovalDecision(NodalOsApprovalDecision decision) =>
        approvalTimelineSerializer.DeserializeApprovalDecision(approvalTimelineSerializer.SerializeApprovalDecision(decision)) ?? decision with
        {
            RuntimeExecutionAllowed = false,
            RuntimeExecutionDeferred = true,
            CanAuthorizeExecution = false
        };

    private NodalOsTimelineEntry SanitizeTimelineEntry(NodalOsTimelineEntry entry) =>
        approvalTimelineSerializer.DeserializeTimelineEntry(approvalTimelineSerializer.SerializeTimelineEntry(entry)) ?? entry with
        {
            RuntimeExecutionAllowed = false,
            RuntimeExecutionDeferred = true
        };

    private NodalOsRuntimeObservabilityReport Sanitize(NodalOsRuntimeObservabilityReport report) =>
        report with
        {
            ProjectOperationalName = "NODAL OS",
            UserRequestRedacted = validator.Redact(report.UserRequestRedacted),
            SystemInterpretationRedacted = validator.Redact(report.SystemInterpretationRedacted),
            ExecutionRegistrySummaryRedacted = validator.Redact(report.ExecutionRegistrySummaryRedacted),
            EventBusSummaryRedacted = validator.Redact(report.EventBusSummaryRedacted),
            TimelineSummaryRedacted = validator.Redact(report.TimelineSummaryRedacted),
            ApprovalSummaryRedacted = validator.Redact(report.ApprovalSummaryRedacted),
            EvidenceSummaryRedacted = validator.Redact(report.EvidenceSummaryRedacted),
            RedactionAppliedSummaryRedacted = validator.Redact(report.RedactionAppliedSummaryRedacted),
            GuardrailsSummaryRedacted = validator.Redact(report.GuardrailsSummaryRedacted),
            BlockedActionsRedacted = report.BlockedActionsRedacted.Select(validator.Redact).ToArray(),
            FailuresRedacted = report.FailuresRedacted.Select(validator.Redact).ToArray(),
            WarningsRedacted = report.WarningsRedacted.Select(validator.Redact).ToArray(),
            HumanHandoffRequirementsRedacted = report.HumanHandoffRequirementsRedacted.Select(validator.Redact).ToArray(),
            ValidationSummaryRedacted = validator.Redact(report.ValidationSummaryRedacted),
            NextRecommendedActionRedacted = validator.Redact(report.NextRecommendedActionRedacted),
            RuntimeExecutionAllowed = false,
            RuntimeExecutionDeferred = true,
            UiRequired = false,
            CloudRequired = false,
            LlmProviderCallRequired = false
        };
}

public static class NodalOsApprovalUxHandoffObservabilityFixtures
{
    public static NodalOsApprovalCardPreview ApprovalCardPreview() =>
        new NodalOsApprovalUxPreviewService().CreateCardPreview(
            NodalOsApprovalTimelineEvidenceFixtures.ApprovalCard(),
            [NodalOsApprovalTimelineEvidenceFixtures.TimelineEntry(NodalOsCoreEventKind.ApprovalRequired)]);

    public static NodalOsApprovalUxPreview ApprovalUxPreview() =>
        new NodalOsApprovalUxPreviewService().CreatePreview(
            [NodalOsApprovalTimelineEvidenceFixtures.ApprovalCard()],
            [NodalOsApprovalTimelineEvidenceFixtures.TimelineEntry(NodalOsCoreEventKind.ApprovalRequired)]);

    public static NodalOsHandoffDataPack HandoffDataPack()
    {
        var registryEntry = RegistryEntry();
        var preview = ApprovalCardPreview() with { ExecutionRegistryEntryId = registryEntry.RegistryEntryId };
        var timeline = NodalOsApprovalTimelineEvidenceFixtures.TimelineEntry(NodalOsCoreEventKind.ApprovalRequired) with
        {
            ExecutionRegistryEntryId = registryEntry.RegistryEntryId,
            ApprovalCardId = preview.ApprovalCardId
        };

        return new NodalOsHandoffDataPackService().CreateDataPack(
            [registryEntry],
            [preview],
            [NodalOsApprovalTimelineEvidenceFixtures.ApprovalDecision()],
            [timeline],
            "User requested a future guarded action.",
            "Approval is pending and runtime remains deferred.",
            proposedActions: ["Show future approval card preview."],
            pendingItems: ["Run AUDIT-A before UI real."],
            warnings: ["No runtime execution is available."],
            failures: [],
            humanHandoffRequirements: ["Human approval required before sensitive future action."],
            nextSteps: ["Run Claude full project architecture and safety audit."],
            milestone: "M474-M476",
            currentDecision: "APPROVAL_UX_HANDOFF_OBSERVABILITY_FOUNDATION_READY",
            testsValidationSummary: "Contract tests validate no-runtime guardrails.");
    }

    public static NodalOsRuntimeObservabilityReport RuntimeObservabilityReport()
    {
        var registryEntry = RegistryEntry();
        var coreEvent = NodalOsCoreRuntimeFixtures.CoreEvent(
            NodalOsCoreEventKind.ApprovalRequired,
            registryEntry.RegistryEntryId,
            registryEntry.RequestId);
        var timeline = NodalOsApprovalTimelineEvidenceFixtures.TimelineEntry(NodalOsCoreEventKind.ApprovalRequired) with
        {
            EventId = coreEvent.EventId,
            ExecutionRegistryEntryId = registryEntry.RegistryEntryId
        };
        var preview = ApprovalCardPreview() with
        {
            EventId = coreEvent.EventId,
            ExecutionRegistryEntryId = registryEntry.RegistryEntryId,
            TimelineEntryIds = [timeline.TimelineEntryId]
        };
        var evidenceRefs = registryEntry.EvidenceRefs
            .Concat(coreEvent.EvidenceRefs)
            .Concat(timeline.EvidenceRefs)
            .Concat(preview.EvidenceRefs)
            .DistinctBy(e => e.EvidenceId)
            .ToArray();

        return new NodalOsRuntimeObservabilityReportService().CreateReport(
            "User requested approval handoff preview.",
            "System prepared no-runtime observability report.",
            [registryEntry],
            [coreEvent],
            [timeline],
            [preview],
            evidenceRefs,
            blockedActions: ["Runtime execution remains blocked."],
            failures: [],
            warnings: ["UI real is deferred."],
            humanHandoffRequirements: ["Approval remains human-gated."]);
    }

    private static NodalOsExecutionRegistryEntry RegistryEntry()
    {
        var registry = new NodalOsExecutionRegistry();
        return registry.Register(NodalOsCoreRuntimeFixtures.ExecutionRequest());
    }
}
