namespace OneBrain.Core.Recipes;

public enum RecipeTriggerKind
{
    Manual,
    WorkitemDue,
    FileCreatedFuture,
    FileChangedFuture,
    DownloadCompletedFuture,
    BrowserUrlMatchedFuture,
    BrowserElementAppearedFuture,
    BrowserDomChangedFuture,
    DesktopWindowAppearedFuture,
    DesktopElementAppearedFuture,
    HotkeyFuture,
    ClipboardChangedFuture,
    EmailReceivedFuture,
    ConnectorEventFuture,
    ScheduleFuture,
    ExternalWebhookFuture,
    ManualCheckpointResolved,
    Unknown
}

public enum RecipeDetectorKind
{
    FixtureDetector,
    ManualSignal,
    WorkitemStateSignal,
    FutureFileWatcher,
    FutureBrowserObserver,
    FutureDesktopObserver,
    FutureHotkeyObserver,
    FutureClipboardObserver,
    FutureEmailObserver,
    FutureConnectorObserver,
    FutureScheduleObserver,
    FutureWebhookObserver,
    Unknown
}

public enum RecipeTriggerStatus
{
    Draft,
    Defined,
    ObserveOnlyReady,
    ObservationCreated,
    ManualAcknowledgementRequired,
    FutureGated,
    Disabled,
    Blocked,
    UnknownBlocked
}

public enum RecipeTriggerSource
{
    Fixture,
    Manual,
    WorkitemQueue,
    ManualCheckpoint,
    FutureFileSystem,
    FutureBrowser,
    FutureDesktop,
    FutureHotkey,
    FutureClipboard,
    FutureEmail,
    FutureConnector,
    FutureSchedule,
    FutureWebhook,
    AuthChallenge,
    Unknown
}

public enum RecipeTriggerScope
{
    Recipe,
    Mission,
    Workspace,
    WorkitemQueue,
    Tool,
    ExternalSystemRef,
    Unknown
}

public enum RecipeTriggerRunMode
{
    NoRun,
    CreateObservationOnly,
    CreateWorkitemDraftOnly,
    SuggestRecipeOnly,
    ManualAcknowledgeOnly,
    FutureAutoRunBlocked
}

public enum RecipeTriggerSafetyMode
{
    ObserveOnly,
    FixtureOnly,
    PreviewOnly,
    ManualAcknowledgeOnly,
    FutureGated,
    Disabled,
    Blocked
}

public enum RecipeTriggerBlockedReason
{
    None,
    UnknownTrigger,
    UnknownDetector,
    FutureDetectorGated,
    Disabled,
    MissingToolTrust,
    MissingSecretRef,
    RawSecretDetected,
    AutoRunBlocked,
    WorkitemProcessingBlocked,
    LiveRuntimeBlocked,
    ChallengeRequiresHuman,
    PolicyBlocked
}

public enum RecipeTriggerEvidenceSourceKind
{
    FixtureEventRef,
    ManualSignalRef,
    WorkitemStateRef,
    FutureFileEventRef,
    FutureBrowserEventRef,
    FutureDesktopEventRef,
    FutureHotkeyEventRef,
    FutureConnectorEventRef,
    FutureScheduleEventRef,
    FutureWebhookEventRef
}

public enum RecipeTriggerTimelineEventKind
{
    TriggerDefined,
    DetectorDefined,
    TriggerReadinessEvaluated,
    DetectorObservationCreated,
    TriggerObservationCreated,
    TriggerBlocked,
    TriggerFutureGated,
    TriggerManualAcknowledgementRequired,
    WorkitemDueObserved,
    ManualCheckpointObserved,
    TriggerEvidenceCapturedRef,
    TriggerRunNotStartedByPolicy,
    TriggerDisabled,
    UnknownTriggerBlocked
}

public sealed record RecipeTriggerRef(string TriggerId);

public sealed record RecipeDetectorRef(string DetectorId);

public sealed record RecipeTriggerDefinition(
    string TriggerId,
    RecipeTriggerKind TriggerKind,
    RecipeDetectorRef DetectorRef,
    string? RecipeId,
    string? RecipeVersion,
    string? WorkitemQueueId,
    string? WorkitemId,
    RecipeTriggerSource Source,
    RecipeTriggerScope Scope,
    RecipeTriggerSafetyMode SafetyMode,
    RecipeTriggerRunMode RunMode,
    RecipeTriggerStatus Status,
    string ConditionSummary,
    string? EventPayloadSchemaRef,
    IReadOnlyList<string> RequiredToolTrustRefs,
    IReadOnlyList<string> RequiredSecretRefs,
    IReadOnlyList<string> EvidenceRequirementRefs,
    IReadOnlyList<string> TimelineProjectionRefs,
    IReadOnlyList<string> ApprovalRefs,
    IReadOnlyList<string> HumanInterventionRefs,
    IReadOnlyList<RecipeTriggerRunMode> AllowedOutcomes,
    IReadOnlyList<RecipeTriggerRunMode> BlockedOutcomes,
    DateTimeOffset? CreatedAt = null,
    DateTimeOffset? UpdatedAt = null)
{
    public bool CanStartRecipeRun => false;
    public bool CanProcessWorkitems => false;
    public bool CanAdvanceRunStep => false;
    public bool LiveRuntimeEnabled => false;
    public bool CreatesWatcherOrListener => false;
}

public sealed record RecipeDetectorDefinition(
    string DetectorId,
    RecipeDetectorKind DetectorKind,
    RecipeTriggerSource Source,
    RecipeTriggerScope Scope,
    RecipeTriggerSafetyMode SafetyMode,
    string ConditionSummary,
    string? EventPayloadSchemaRef,
    IReadOnlyList<string> RequiredToolTrustRefs,
    IReadOnlyList<string> RequiredSecretRefs,
    IReadOnlyList<string> EvidenceRequirementRefs,
    DateTimeOffset? CreatedAt = null,
    DateTimeOffset? UpdatedAt = null)
{
    public bool CreatesLiveSubscription => false;
    public bool CreatesWatcherOrListener => false;
    public bool LiveRuntimeEnabled => false;
}

public sealed record RecipeDetectorObservation(
    string ObservationId,
    string DetectorId,
    RecipeDetectorKind DetectorKind,
    RecipeTriggerSource Source,
    string RedactedSummary,
    IReadOnlyList<string> EvidenceRefs,
    bool RawPayloadPresent = false,
    bool RawSecretDetected = false)
{
    public bool LiveRuntimeEnabled => false;
    public bool StartsRecipeRun => false;
}

public sealed record RecipeTriggerObservation(
    string ObservationId,
    string TriggerId,
    RecipeTriggerKind TriggerKind,
    RecipeTriggerRunMode Outcome,
    string RedactedSummary,
    IReadOnlyList<string> EvidenceRefs,
    IReadOnlyList<string> TimelineRefs,
    string? SuggestedRecipeRunDraftRef = null,
    string? WorkitemDraftRef = null,
    bool FutureAutoRunRequested = false,
    bool RawPayloadPresent = false,
    bool RawSecretDetected = false)
{
    public bool StartedRecipeRun => false;
    public bool ProcessedWorkitem => false;
    public bool AdvancedRunStep => false;
    public bool LiveRuntimeEnabled => false;
    public bool ActionAuthorityGranted => false;
}

public sealed record RecipeTriggerPolicy(
    IReadOnlyList<string> AvailableToolTrustRefs,
    IReadOnlyList<string> AvailableSecretRefs,
    bool RawSecretDetected = false,
    bool FutureDetectorsAllowed = false,
    bool AutoRunAllowed = false,
    bool ApprovalPresent = false)
{
    public bool LiveRuntimeEnabled => false;
}

public sealed record RecipeTriggerDecision(
    RecipeTriggerRunMode Outcome,
    RecipeTriggerBlockedReason BlockedReason,
    string Summary,
    bool StartsRecipeRun = false,
    bool ProcessesWorkitem = false,
    bool CreatesWatcher = false,
    bool CreatesScheduler = false,
    bool CreatesHook = false,
    bool CreatesListener = false,
    bool LiveRuntimeEnabled = false,
    bool ActionAuthorityGranted = false);

public sealed record RecipeTriggerReadiness(
    bool IsReady,
    RecipeTriggerStatus Status,
    RecipeTriggerDecision Decision,
    IReadOnlyList<RecipeReadinessIssue> BlockingIssues,
    IReadOnlyList<RecipeReadinessIssue> Warnings)
{
    public bool LiveRuntimeEnabled => false;
    public bool ActionAuthorityGranted => false;
}

public sealed record RecipeTriggerEligibility(
    bool EligibleForObservation,
    bool EligibleForDraftSuggestion,
    bool AutoRunBlocked,
    RecipeTriggerReadiness Readiness)
{
    public bool StartsRecipeRun => false;
    public bool ProcessesWorkitem => false;
    public bool LiveRuntimeEnabled => false;
}

public sealed record RecipeTriggerEvidenceRef(string TriggerEvidenceId);

public sealed record RecipeTriggerEvidence(
    string TriggerEvidenceId,
    string TriggerId,
    string ObservationId,
    RecipeTriggerEvidenceSourceKind SourceKind,
    string RefId,
    string RedactedSummary,
    RecipeEvidenceRedactionStatus RedactionStatus,
    bool RawPayloadEmbedded = false,
    bool SecretValuesIncluded = false,
    bool RequiresRealCapture = false)
{
    public bool ReferenceOnly => !RawPayloadEmbedded && !SecretValuesIncluded && !RequiresRealCapture;
    public bool LiveRuntimeEnabled => false;
}

public sealed record RecipeDetectorObservationEvidence(
    string EvidenceId,
    string DetectorObservationId,
    RecipeTriggerEvidenceSourceKind SourceKind,
    string RefId,
    string RedactedSummary,
    bool RawPayloadEmbedded = false,
    bool SecretValuesIncluded = false)
{
    public bool ReferenceOnly => !RawPayloadEmbedded && !SecretValuesIncluded;
}

public sealed record RecipeTriggerTimelineEvent(
    string EventId,
    string TriggerId,
    RecipeTriggerTimelineEventKind EventKind,
    RecipeTimelineProjectionStatus Status,
    string Summary,
    IReadOnlyList<string> EvidenceRefs,
    IReadOnlyList<string> RedactionRefs,
    DateTimeOffset? Timestamp,
    RecipeTimelineEventSeverity Severity)
{
    public bool LiveRuntimeEnabled => false;
    public bool ActionAuthorityGranted => false;
}

public sealed record RecipeTriggerTimelineProjection(
    string ProjectionId,
    string TriggerId,
    RecipeTimelineProjectionStatus Status,
    IReadOnlyList<RecipeTriggerTimelineEvent> Events,
    string RedactedSummary,
    bool ObservationOnly = true)
{
    public bool LiveRuntimeEnabled => false;
    public bool StartsRecipeRun => false;
}

public static class RecipeTriggerPolicyEvaluator
{
    public static RecipeTriggerReadiness Evaluate(
        RecipeTriggerDefinition trigger,
        RecipeDetectorDefinition detector,
        RecipeTriggerPolicy policy)
    {
        var blocking = new List<RecipeReadinessIssue>();
        var warnings = new List<RecipeReadinessIssue>();

        if (trigger.TriggerKind == RecipeTriggerKind.Unknown)
            blocking.Add(Issue("unknown-trigger", RecipeReadinessStatus.BlockedByProtectedScope, "Unknown trigger kind is blocked."));

        if (detector.DetectorKind == RecipeDetectorKind.Unknown)
            blocking.Add(Issue("unknown-detector", RecipeReadinessStatus.BlockedByProtectedScope, "Unknown detector kind is blocked."));

        if (trigger.SafetyMode is RecipeTriggerSafetyMode.Disabled or RecipeTriggerSafetyMode.Blocked ||
            detector.SafetyMode is RecipeTriggerSafetyMode.Disabled or RecipeTriggerSafetyMode.Blocked)
            blocking.Add(Issue("trigger-disabled-or-blocked", RecipeReadinessStatus.BlockedByProtectedScope, "Trigger or detector is disabled/blocked."));

        if (trigger.RunMode == RecipeTriggerRunMode.FutureAutoRunBlocked || policy.AutoRunAllowed)
            blocking.Add(Issue("trigger-autorun-blocked", RecipeReadinessStatus.BlockedLiveRuntimeDisabled, "Trigger observations cannot start recipe runs in Phase 6."));

        if (policy.RawSecretDetected)
            blocking.Add(Issue("trigger-raw-secret-detected", RecipeReadinessStatus.BlockedMissingSecretReference, "Trigger payloads must not contain raw secret values."));

        foreach (var toolRef in trigger.RequiredToolTrustRefs.Concat(detector.RequiredToolTrustRefs).Distinct())
        {
            if (!policy.AvailableToolTrustRefs.Contains(toolRef))
                blocking.Add(Issue("trigger-missing-tool-trust", RecipeReadinessStatus.BlockedMissingToolTrust, "Trigger requires tool trust refs by id only."));
        }

        foreach (var secretRef in trigger.RequiredSecretRefs.Concat(detector.RequiredSecretRefs).Distinct())
        {
            if (!policy.AvailableSecretRefs.Contains(secretRef))
                blocking.Add(Issue("trigger-missing-secret-ref", RecipeReadinessStatus.BlockedMissingSecretReference, "Trigger requires secret refs by id only."));
        }

        if (IsFutureTrigger(trigger.TriggerKind) || IsFutureDetector(detector.DetectorKind) || IsFutureSource(trigger.Source) || IsFutureSource(detector.Source))
        {
            blocking.Add(Issue("trigger-future-gated", RecipeReadinessStatus.BlockedLiveRuntimeDisabled, "Future trigger/detector sources are future-gated and observe-only."));
        }

        if (IsChallenge(trigger))
            blocking.Add(Issue("trigger-challenge-human-required", RecipeReadinessStatus.BlockedRiskGate, "2FA/CAPTCHA/challenge triggers map to human/block; automation remains unavailable."));

        if (blocking.Count > 0)
        {
            var first = blocking[0];
            var reason = ReasonFor(first.IssueId);
            var status = reason switch
            {
                RecipeTriggerBlockedReason.FutureDetectorGated => RecipeTriggerStatus.FutureGated,
                RecipeTriggerBlockedReason.Disabled => RecipeTriggerStatus.Disabled,
                RecipeTriggerBlockedReason.UnknownTrigger or RecipeTriggerBlockedReason.UnknownDetector => RecipeTriggerStatus.UnknownBlocked,
                _ => RecipeTriggerStatus.Blocked
            };

            return new(
                false,
                status,
                BlockedDecision(reason, first.Message),
                blocking,
                warnings);
        }

        var outcome = trigger.TriggerKind switch
        {
            RecipeTriggerKind.WorkitemDue => RecipeTriggerRunMode.CreateObservationOnly,
            RecipeTriggerKind.ManualCheckpointResolved => RecipeTriggerRunMode.ManualAcknowledgeOnly,
            RecipeTriggerKind.Manual => RecipeTriggerRunMode.SuggestRecipeOnly,
            _ => RecipeTriggerRunMode.CreateObservationOnly
        };

        return new(
            true,
            trigger.TriggerKind == RecipeTriggerKind.ManualCheckpointResolved ? RecipeTriggerStatus.ManualAcknowledgementRequired : RecipeTriggerStatus.ObserveOnlyReady,
            new RecipeTriggerDecision(outcome, RecipeTriggerBlockedReason.None, "Trigger is eligible for fixture-safe observation only."),
            [],
            warnings);
    }

    public static RecipeTriggerObservation CreateObservation(
        RecipeTriggerDefinition trigger,
        string observationId,
        string redactedSummary,
        IReadOnlyList<string>? evidenceRefs = null)
    {
        var outcome = trigger.TriggerKind switch
        {
            RecipeTriggerKind.WorkitemDue => RecipeTriggerRunMode.CreateObservationOnly,
            RecipeTriggerKind.ManualCheckpointResolved => RecipeTriggerRunMode.ManualAcknowledgeOnly,
            RecipeTriggerKind.Manual when !string.IsNullOrWhiteSpace(trigger.RecipeId) => RecipeTriggerRunMode.SuggestRecipeOnly,
            _ => RecipeTriggerRunMode.CreateObservationOnly
        };

        return new(
            observationId,
            trigger.TriggerId,
            trigger.TriggerKind,
            outcome,
            redactedSummary,
            evidenceRefs ?? [],
            TimelineRefs: [],
            SuggestedRecipeRunDraftRef: outcome == RecipeTriggerRunMode.SuggestRecipeOnly ? $"recipe-run-draft:{trigger.RecipeId}" : null,
            WorkitemDraftRef: outcome == RecipeTriggerRunMode.CreateWorkitemDraftOnly ? $"workitem-draft:{trigger.WorkitemQueueId}" : null,
            FutureAutoRunRequested: trigger.RunMode == RecipeTriggerRunMode.FutureAutoRunBlocked);
    }

    private static RecipeTriggerDecision BlockedDecision(RecipeTriggerBlockedReason reason, string summary) =>
        new(RecipeTriggerRunMode.NoRun, reason, summary);

    private static RecipeTriggerBlockedReason ReasonFor(string issueId) =>
        issueId switch
        {
            "unknown-trigger" => RecipeTriggerBlockedReason.UnknownTrigger,
            "unknown-detector" => RecipeTriggerBlockedReason.UnknownDetector,
            "trigger-disabled-or-blocked" => RecipeTriggerBlockedReason.Disabled,
            "trigger-autorun-blocked" => RecipeTriggerBlockedReason.AutoRunBlocked,
            "trigger-raw-secret-detected" => RecipeTriggerBlockedReason.RawSecretDetected,
            "trigger-missing-tool-trust" => RecipeTriggerBlockedReason.MissingToolTrust,
            "trigger-missing-secret-ref" => RecipeTriggerBlockedReason.MissingSecretRef,
            "trigger-future-gated" => RecipeTriggerBlockedReason.FutureDetectorGated,
            "trigger-challenge-human-required" => RecipeTriggerBlockedReason.ChallengeRequiresHuman,
            _ => RecipeTriggerBlockedReason.PolicyBlocked
        };

    private static bool IsFutureTrigger(RecipeTriggerKind kind) =>
        kind.ToString().Contains("Future", StringComparison.Ordinal);

    private static bool IsFutureDetector(RecipeDetectorKind kind) =>
        kind.ToString().StartsWith("Future", StringComparison.Ordinal);

    private static bool IsFutureSource(RecipeTriggerSource source) =>
        source.ToString().StartsWith("Future", StringComparison.Ordinal);

    private static bool IsChallenge(RecipeTriggerDefinition trigger) =>
        trigger.Source == RecipeTriggerSource.AuthChallenge ||
        trigger.ConditionSummary.Contains("captcha", StringComparison.OrdinalIgnoreCase) ||
        trigger.ConditionSummary.Contains("2fa", StringComparison.OrdinalIgnoreCase) ||
        trigger.ConditionSummary.Contains("challenge", StringComparison.OrdinalIgnoreCase);

    private static RecipeReadinessIssue Issue(string id, RecipeReadinessStatus status, string message) =>
        new(id, status, RecipeReadinessIssueSeverity.Blocking, message);
}

public static class RecipeTriggerTimelineProjector
{
    public static RecipeTriggerTimelineProjection FromReadiness(
        string projectionId,
        RecipeTriggerDefinition trigger,
        RecipeTriggerReadiness readiness)
    {
        var kind = readiness.Status switch
        {
            RecipeTriggerStatus.FutureGated => RecipeTriggerTimelineEventKind.TriggerFutureGated,
            RecipeTriggerStatus.Disabled => RecipeTriggerTimelineEventKind.TriggerDisabled,
            RecipeTriggerStatus.UnknownBlocked => RecipeTriggerTimelineEventKind.UnknownTriggerBlocked,
            RecipeTriggerStatus.Blocked => RecipeTriggerTimelineEventKind.TriggerBlocked,
            RecipeTriggerStatus.ManualAcknowledgementRequired => RecipeTriggerTimelineEventKind.TriggerManualAcknowledgementRequired,
            _ => RecipeTriggerTimelineEventKind.TriggerReadinessEvaluated
        };

        var ev = new RecipeTriggerTimelineEvent(
            $"event:{projectionId}:readiness",
            trigger.TriggerId,
            kind,
            readiness.Status == RecipeTriggerStatus.ObserveOnlyReady ? RecipeTimelineProjectionStatus.Projected : RecipeTimelineProjectionStatus.BlockedLiveRuntimeDisabled,
            readiness.Decision.Summary,
            EvidenceRefs: [],
            RedactionRefs: [],
            Timestamp: null,
            readiness.IsReady ? RecipeTimelineEventSeverity.Info : RecipeTimelineEventSeverity.Blocking);

        return new(
            projectionId,
            trigger.TriggerId,
            readiness.IsReady ? RecipeTimelineProjectionStatus.Projected : RecipeTimelineProjectionStatus.BlockedLiveRuntimeDisabled,
            [ev],
            "Trigger readiness projected as observe-only metadata.");
    }

    public static RecipeTriggerTimelineProjection FromObservation(
        string projectionId,
        RecipeTriggerObservation observation)
    {
        var events = new List<RecipeTriggerTimelineEvent>
        {
            new(
                $"event:{projectionId}:observation",
                observation.TriggerId,
                observation.TriggerKind == RecipeTriggerKind.WorkitemDue
                    ? RecipeTriggerTimelineEventKind.WorkitemDueObserved
                    : observation.TriggerKind == RecipeTriggerKind.ManualCheckpointResolved
                        ? RecipeTriggerTimelineEventKind.ManualCheckpointObserved
                        : RecipeTriggerTimelineEventKind.TriggerObservationCreated,
                RecipeTimelineProjectionStatus.Projected,
                observation.RedactedSummary,
                observation.EvidenceRefs,
                RedactionRefs: [],
                Timestamp: null,
                RecipeTimelineEventSeverity.Info)
        };

        if (observation.FutureAutoRunRequested)
        {
            events.Add(new(
                $"event:{projectionId}:run-not-started",
                observation.TriggerId,
                RecipeTriggerTimelineEventKind.TriggerRunNotStartedByPolicy,
                RecipeTimelineProjectionStatus.BlockedLiveRuntimeDisabled,
                "Future autorun request was observed but no recipe run was started by policy.",
                observation.EvidenceRefs,
                RedactionRefs: [],
                Timestamp: null,
                RecipeTimelineEventSeverity.Blocking));
        }

        return new(
            projectionId,
            observation.TriggerId,
            events.Any(e => e.EventKind == RecipeTriggerTimelineEventKind.TriggerRunNotStartedByPolicy)
                ? RecipeTimelineProjectionStatus.BlockedLiveRuntimeDisabled
                : RecipeTimelineProjectionStatus.Projected,
            events,
            "Trigger observation projected without starting a run.");
    }
}
