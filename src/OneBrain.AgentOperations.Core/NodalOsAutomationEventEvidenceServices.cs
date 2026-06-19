using System.Text.Json;
using System.Text.Json.Serialization;
using OneBrain.AgentOperations.Contracts;

namespace OneBrain.AgentOperations.Core;

public sealed class NodalOsAutomationEventEvidenceValidator
{
    private static readonly string[] RequiredHandoffOptions =
    [
        "Continue",
        "Pause",
        "ChangeInstruction",
        "CopyTechnicalLog"
    ];

    private readonly NodalOsRedactionService redaction;
    private readonly NodalOsEvidenceRefBridge evidenceBridge;

    public NodalOsAutomationEventEvidenceValidator()
        : this(new NodalOsRedactionService())
    {
    }

    public NodalOsAutomationEventEvidenceValidator(NodalOsRedactionService redaction)
        : this(redaction, new NodalOsEvidenceRefBridge(redaction))
    {
    }

    public NodalOsAutomationEventEvidenceValidator(
        NodalOsRedactionService redaction,
        NodalOsEvidenceRefBridge evidenceBridge)
    {
        this.redaction = redaction;
        this.evidenceBridge = evidenceBridge;
    }

    public NodalOsAutomationEventEvidenceValidationResult ValidateEvent(NodalOsAutomationEvent automationEvent)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        AddRequired(errors, automationEvent.EventId, "EventId is required.");
        if (automationEvent.CreatedAt == default)
            errors.Add("CreatedAt is required.");

        ValidateNoRuntimeExecution(
            automationEvent.RuntimeExecutionAllowed,
            automationEvent.RuntimeExecutionDeferred,
            errors);

        if (!automationEvent.RequiresGlobalPolicyEvaluation)
            errors.Add("Automation events must require global policy evaluation.");
        if (!automationEvent.RequiresEvidenceRedaction)
            errors.Add("Automation events must require evidence redaction.");

        if (string.IsNullOrWhiteSpace(automationEvent.MissionId) &&
            string.IsNullOrWhiteSpace(automationEvent.TaskId) &&
            string.IsNullOrWhiteSpace(automationEvent.RecipeId) &&
            string.IsNullOrWhiteSpace(automationEvent.StepId))
        {
            warnings.Add("Automation event has no MissionId, TaskId, RecipeId, or StepId; timeline context is generic.");
        }

        ValidateSafeText(errors, "HumanSummary", automationEvent.HumanSummary);
        ValidateSafeText(errors, "TechnicalSummary", automationEvent.TechnicalSummary);
        ValidateEvidenceRefs(automationEvent.EvidenceRefs, errors, warnings);
        ValidateTimelineCompatibility(automationEvent.MissionId, automationEvent.TaskId, automationEvent.RecipeId, automationEvent.StepId, warnings);

        return Result(
            errors,
            warnings,
            runtimeExecutionAllowed: false,
            runtimeExecutionDeferred: automationEvent.RuntimeExecutionDeferred,
            requiresGlobalPolicyEvaluation: automationEvent.RequiresGlobalPolicyEvaluation,
            requiresEvidenceRedaction: automationEvent.RequiresEvidenceRedaction);
    }

    public NodalOsAutomationEventEvidenceValidationResult ValidateEvidence(NodalOsAutomationEvidence evidence)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        AddRequired(errors, evidence.EvidenceId, "EvidenceId is required.");
        if (evidence.CreatedAt == default)
            errors.Add("CreatedAt is required.");

        if (!evidence.Redacted)
            errors.Add("Automation evidence must be redacted.");
        if (evidence.ContainsRawSecret)
            errors.Add("Automation evidence cannot contain raw secrets.");
        if (evidence.ContainsRawCookie)
            errors.Add("Automation evidence cannot contain raw cookies.");
        if (evidence.ContainsRawHeader)
            errors.Add("Automation evidence cannot contain raw headers.");
        if (evidence.ContainsRawBody)
            errors.Add("Automation evidence cannot contain raw private bodies.");

        ValidateSafeText(errors, "SelectorPath", evidence.SelectorPath);
        ValidateSafeText(errors, "DomSnapshotRedacted", evidence.DomSnapshotRedacted);
        ValidateSafeText(errors, "StepLogRedacted", evidence.StepLogRedacted);
        ValidateNetworkMetadata(errors, evidence.NetworkMetadataRedacted);
        ValidateSafeText(errors, "HumanNoteRedacted", evidence.HumanNoteRedacted);
        ValidateScreenshotReference(errors, evidence);
        ValidateEvidenceRefs(evidence.EvidenceRefs, errors, warnings);
        ValidateTimelineCompatibility(evidence.MissionId, evidence.TaskId, evidence.RecipeId, evidence.StepId, warnings);

        return Result(
            errors,
            warnings,
            runtimeExecutionAllowed: false,
            runtimeExecutionDeferred: true,
            requiresGlobalPolicyEvaluation: true,
            requiresEvidenceRedaction: evidence.Redacted);
    }

    public NodalOsAutomationEventEvidenceValidationResult ValidateHandoffState(NodalOsAutomationHandoffState handoff)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        AddRequired(errors, handoff.HandoffId, "HandoffId is required.");
        if (handoff.CreatedAt == default)
            errors.Add("CreatedAt is required.");
        if (!handoff.RequiresHumanAction)
            errors.Add("Automation handoff must require human action.");

        ValidateNoRuntimeExecution(
            handoff.RuntimeExecutionAllowed,
            handoff.RuntimeExecutionDeferred,
            errors);

        AddRequired(errors, handoff.HumanReadableBlocker, "HumanReadableBlocker is required.");
        if (IsGenericBlockedOnly(handoff.HumanReadableBlocker))
            errors.Add("Human handoff blocker must explain the specific reason, not only say blocked.");
        ValidateSafeText(errors, "HumanReadableBlocker", handoff.HumanReadableBlocker);

        if (!RequiredHandoffOptions.All(option =>
                handoff.UserOptions.Any(userOption => string.Equals(userOption, option, StringComparison.OrdinalIgnoreCase))))
        {
            errors.Add("Human handoff must include Continue, Pause, ChangeInstruction, and CopyTechnicalLog user options.");
        }

        foreach (var option in handoff.UserOptions)
            ValidateSafeText(errors, "UserOptions", option);

        ValidateEvidenceRefs(handoff.EvidenceRefs, errors, warnings);
        ValidateTimelineCompatibility(handoff.MissionId, handoff.TaskId, handoff.RecipeId, handoff.StepId, warnings);

        return Result(
            errors,
            warnings,
            runtimeExecutionAllowed: false,
            runtimeExecutionDeferred: handoff.RuntimeExecutionDeferred,
            requiresGlobalPolicyEvaluation: true,
            requiresEvidenceRedaction: true);
    }

    public void ValidateEvidenceRefs(
        IReadOnlyList<NodalOsEvidenceBridgeRef> evidenceRefs,
        List<string> errors,
        List<string> warnings)
    {
        foreach (var evidenceRef in evidenceRefs)
        {
            var bridgeResult = evidenceBridge.ValidateBridgeRef(evidenceRef);
            errors.AddRange(bridgeResult.Errors.Select(SanitizeError));
            warnings.AddRange(bridgeResult.Warnings.Select(SanitizeError));
        }
    }

    public void ValidateNoRawSecrets(string? value, List<string> errors, string fieldName) =>
        ValidateSafeText(errors, fieldName, value);

    public void ValidateTimelineCompatibility(
        string? missionId,
        string? taskId,
        string? recipeId,
        string? stepId,
        List<string> warnings)
    {
        if (string.IsNullOrWhiteSpace(missionId) &&
            string.IsNullOrWhiteSpace(taskId) &&
            string.IsNullOrWhiteSpace(recipeId) &&
            string.IsNullOrWhiteSpace(stepId))
        {
            warnings.Add("Automation contract has generic Mission Control timeline context.");
        }
    }

    private void ValidateSafeText(List<string> errors, string fieldName, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return;

        if (redaction.ContainsSensitiveField(fieldName, value) || redaction.ContainsSensitiveContent(value))
            errors.Add($"{fieldName} contains sensitive content and must be redacted before automation evidence persistence.");
    }

    private void ValidateNetworkMetadata(List<string> errors, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return;

        ValidateSafeText(errors, "NetworkMetadataRedacted", value);
        if (value.Contains("authorization", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("cookie", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("set-cookie", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("body", StringComparison.OrdinalIgnoreCase))
        {
            errors.Add("Network metadata must not contain Authorization, Cookie, Set-Cookie, or body content.");
        }
    }

    private static void ValidateScreenshotReference(List<string> errors, NodalOsAutomationEvidence evidence)
    {
        if (evidence.Kind != NodalOsAutomationEvidenceKind.ScreenshotReferenceFuture)
            return;

        if (string.IsNullOrWhiteSpace(evidence.ScreenshotRefFuture))
            errors.Add("Screenshot evidence must be a future reference, not inline binary content.");
        if (LooksLikeInlineBinary(evidence.ScreenshotRefFuture))
            errors.Add("Screenshot evidence must be reference-only and cannot contain inline binary content.");
    }

    private static bool LooksLikeInlineBinary(string? value) =>
        !string.IsNullOrWhiteSpace(value) &&
        (value.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase) ||
         value.Length > 512);

    private static bool IsGenericBlockedOnly(string? value) =>
        string.Equals(value?.Trim(), "blocked", StringComparison.OrdinalIgnoreCase);

    private static void ValidateNoRuntimeExecution(
        bool runtimeExecutionAllowed,
        bool runtimeExecutionDeferred,
        List<string> errors)
    {
        if (runtimeExecutionAllowed)
            errors.Add("Automation contracts cannot allow runtime execution.");
        if (!runtimeExecutionDeferred)
            errors.Add("Automation contracts must keep runtime execution deferred.");
    }

    private string SanitizeError(string value) =>
        redaction.RedactValue(value).Value;

    private static void AddRequired(List<string> errors, string? value, string message)
    {
        if (string.IsNullOrWhiteSpace(value))
            errors.Add(message);
    }

    private static NodalOsAutomationEventEvidenceValidationResult Result(
        List<string> errors,
        List<string> warnings,
        bool runtimeExecutionAllowed,
        bool runtimeExecutionDeferred,
        bool requiresGlobalPolicyEvaluation,
        bool requiresEvidenceRedaction) =>
        new()
        {
            IsValid = errors.Count == 0,
            RuntimeExecutionAllowed = runtimeExecutionAllowed,
            RuntimeExecutionDeferred = runtimeExecutionDeferred,
            RequiresGlobalPolicyEvaluation = requiresGlobalPolicyEvaluation,
            RequiresEvidenceRedaction = requiresEvidenceRedaction,
            Errors = errors.Distinct(StringComparer.Ordinal).ToArray(),
            Warnings = warnings.Distinct(StringComparer.Ordinal).ToArray()
        };
}

public sealed class NodalOsAutomationEventEvidenceJsonSerializer
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
        WriteIndented = true
    };

    public string SerializeEvent(NodalOsAutomationEvent automationEvent) =>
        JsonSerializer.Serialize(automationEvent, Options);

    public NodalOsAutomationEvent? DeserializeEvent(string json) =>
        JsonSerializer.Deserialize<NodalOsAutomationEvent>(json, Options);

    public string SerializeEvidence(NodalOsAutomationEvidence evidence) =>
        JsonSerializer.Serialize(evidence, Options);

    public NodalOsAutomationEvidence? DeserializeEvidence(string json) =>
        JsonSerializer.Deserialize<NodalOsAutomationEvidence>(json, Options);

    public string SerializeHandoffState(NodalOsAutomationHandoffState handoff) =>
        JsonSerializer.Serialize(handoff, Options);

    public NodalOsAutomationHandoffState? DeserializeHandoffState(string json) =>
        JsonSerializer.Deserialize<NodalOsAutomationHandoffState>(json, Options);
}

public static class NodalOsAutomationEventEvidenceFixtures
{
    public static NodalOsAutomationEvent StepStartedEvent() =>
        Event(NodalOsAutomationEventKind.AutomationStepStarted, "Automation step started.");

    public static NodalOsAutomationEvent StepCompletedEvent() =>
        Event(NodalOsAutomationEventKind.AutomationStepCompleted, "Automation step completed as contract-only event.");

    public static NodalOsAutomationEvent StepFailedEvent() =>
        Event(NodalOsAutomationEventKind.AutomationStepFailed, "Automation step failed before runtime execution.");

    public static NodalOsAutomationEvent HandoffRequiredEvent() =>
        Event(NodalOsAutomationEventKind.AutomationHandoffRequired, "Human handoff is required.");

    public static NodalOsAutomationEvidence SelectorEvidence() =>
        Evidence(NodalOsAutomationEvidenceKind.SelectorEvidence) with
        {
            SelectorPath = "dom://main/button[data-testid='continue']"
        };

    public static NodalOsAutomationEvidence DomSnapshotRedactedEvidence() =>
        Evidence(NodalOsAutomationEvidenceKind.DomSnapshotRedacted) with
        {
            DomSnapshotRedacted = "<main><button>[REDACTED_SAFE_LABEL]</button></main>"
        };

    public static NodalOsAutomationEvidence StepLogEvidence() =>
        Evidence(NodalOsAutomationEvidenceKind.StepLog) with
        {
            StepLogRedacted = "Step observed expected read-only state."
        };

    public static NodalOsAutomationEvidence NetworkMetadataRedactedEvidence() =>
        Evidence(NodalOsAutomationEvidenceKind.NetworkMetadataRedacted) with
        {
            NetworkMetadataRedacted = "GET https://example.test/status 200 content-type=application/json"
        };

    public static NodalOsAutomationEvidence HumanNoteEvidence() =>
        Evidence(NodalOsAutomationEvidenceKind.HumanNote) with
        {
            HumanNoteRedacted = "Operator confirmed read-only observation."
        };

    public static NodalOsAutomationHandoffState LoginRequiredHandoff() =>
        Handoff(NodalOsAutomationHandoffReason.LoginRequired, "Login is required before the automation can continue safely.");

    public static NodalOsAutomationHandoffState CaptchaRequiredHandoff() =>
        Handoff(NodalOsAutomationHandoffReason.CaptchaRequired, "Captcha challenge requires user decision.");

    public static NodalOsAutomationHandoffState TwoFactorRequiredHandoff() =>
        Handoff(NodalOsAutomationHandoffReason.TwoFactorRequired, "Two-factor authentication requires user action.");

    public static NodalOsAutomationEvidence InvalidRawCookieEvidence() =>
        Evidence(NodalOsAutomationEvidenceKind.NetworkMetadataRedacted) with
        {
            ContainsRawCookie = true,
            NetworkMetadataRedacted = "cookie: session=abc123"
        };

    public static NodalOsAutomationEvidence InvalidRawBodyEvidence() =>
        Evidence(NodalOsAutomationEvidenceKind.DomSnapshotRedacted) with
        {
            ContainsRawBody = true,
            DomSnapshotRedacted = "private body: account payload"
        };

    public static NodalOsAutomationEvidence InvalidRawHeaderEvidence() =>
        Evidence(NodalOsAutomationEvidenceKind.NetworkMetadataRedacted) with
        {
            ContainsRawHeader = true,
            NetworkMetadataRedacted = "authorization: Bearer abcdefghijkl"
        };

    public static NodalOsAutomationEvidence InvalidRawSecretEvidence() =>
        Evidence(NodalOsAutomationEvidenceKind.StepLog) with
        {
            ContainsRawSecret = true,
            StepLogRedacted = "password=super-secret-value"
        };

    private static NodalOsAutomationEvent Event(NodalOsAutomationEventKind kind, string humanSummary) =>
        new()
        {
            EventId = $"auto-event-{Guid.NewGuid():N}",
            MissionId = "mission-automation",
            TaskId = "task-automation",
            StepId = "step-automation",
            Kind = kind,
            RuntimeExecutionAllowed = false,
            RuntimeExecutionDeferred = true,
            RequiresGlobalPolicyEvaluation = true,
            RequiresEvidenceRedaction = true,
            EvidenceRefs = [ValidEvidenceRef()],
            HumanSummary = humanSummary,
            TechnicalSummary = "Contract-only automation event for Mission Control timeline.",
            CreatedAt = DateTimeOffset.UtcNow
        };

    private static NodalOsAutomationEvidence Evidence(NodalOsAutomationEvidenceKind kind) =>
        new()
        {
            EvidenceId = $"auto-evidence-{Guid.NewGuid():N}",
            EventId = "auto-event-fixture",
            MissionId = "mission-automation",
            TaskId = "task-automation",
            StepId = "step-automation",
            Kind = kind,
            Redacted = true,
            ContainsRawSecret = false,
            ContainsRawCookie = false,
            ContainsRawHeader = false,
            ContainsRawBody = false,
            EvidenceRefs = [ValidEvidenceRef()],
            CreatedAt = DateTimeOffset.UtcNow
        };

    private static NodalOsAutomationHandoffState Handoff(
        NodalOsAutomationHandoffReason reason,
        string blocker) =>
        new()
        {
            HandoffId = $"handoff-{Guid.NewGuid():N}",
            EventId = "auto-event-fixture",
            MissionId = "mission-automation",
            TaskId = "task-automation",
            StepId = "step-automation",
            Reason = reason,
            RequiresHumanAction = true,
            RuntimeExecutionAllowed = false,
            RuntimeExecutionDeferred = true,
            CanContinueAfterHumanAction = true,
            HumanReadableBlocker = blocker,
            UserOptions = ["Continue", "Pause", "ChangeInstruction", "CopyTechnicalLog"],
            EvidenceRefs = [ValidEvidenceRef()],
            CreatedAt = DateTimeOffset.UtcNow
        };

    private static NodalOsEvidenceBridgeRef ValidEvidenceRef() =>
        new()
        {
            EvidenceId = $"evidence-{Guid.NewGuid():N}",
            Kind = "automation-contract",
            Ref = "ledger:automation-contract",
            Hash = "sha256:automation-contract",
            SourceKind = NodalOsEvidenceBridgeSourceKind.AgentOperation,
            UseKind = NodalOsEvidenceBridgeUseKind.AuditTrail,
            Authority = NodalOsEvidenceBridgeAuthority.NoAuthority,
            Sensitivity = NodalOsEvidenceSensitivity.NonSensitive,
            RedactionState = NodalOsEvidenceRedactionState.NotRequired,
            LedgerRef = "ledger:automation-contract",
            Provenance = "NODAL OS:AutomationLayer:ContractOnly",
            CreatedAt = DateTimeOffset.UtcNow
        };
}
