using OneBrain.Core.History;
using OneBrain.Core.Perception;
using OneBrain.Core.Recipes;
using OneBrain.Core.Runtime;
using OneBrain.Core.Verification;

namespace OneBrain.Core.Skills;

public enum TeachNodalCaptureSessionState
{
    Created,
    Armed,
    Capturing,
    ReviewReady,
    Cancelled
}

public enum TeachNodalCaptureSource
{
    Uia,
    Cdp,
    ApplicationScopedPointer,
    ApplicationScopedKeyboard,
    OperatorAnnotation
}

public sealed record TeachNodalCaptureScope(
    string CaptureId,
    string TitleRedacted,
    string WorkspaceScope,
    string AppProfileId,
    int AppProfileVersion,
    string ApplicationRef,
    TeachNodalSurface Surface,
    IReadOnlySet<string> AuthorizedCapabilities,
    ReliableRecipeRiskProfile RiskProfile,
    int MaximumSteps = TeachNodalCompilerV1.MaximumSteps)
{
    public bool GrantsExecutionAuthority => false;
    public bool CanExpandMissionScope => false;
    public bool GlobalCaptureAllowed => false;
}

public sealed record TeachNodalCaptureConsent(
    bool ExplicitOptIn,
    DateTimeOffset GrantedAtUtc,
    DateTimeOffset ExpiresAtUtc,
    IReadOnlySet<TeachNodalCaptureSource> AllowedSources,
    string ConsentEvidenceRef)
{
    public bool Permanent => false;
    public bool GrantsExecutionAuthority => false;
}

public sealed record TeachNodalCaptureObservation(
    long Sequence,
    DateTimeOffset CapturedAtUtc,
    TeachNodalCaptureSource Source,
    TrustedControlSource IntentSource,
    string StepId,
    TeachNodalObservedAction Action,
    CognitiveSnapshotV2 Before,
    CognitiveSnapshotV2 After,
    SemanticVerificationPlan VerificationPlan,
    bool ActionExecuted,
    bool ActionRejected,
    bool UserInterrupted,
    TimeSpan VerificationElapsed,
    IReadOnlyList<string> EvidenceRefs,
    bool TargetApplicationForeground,
    bool GlobalHookUsed = false,
    bool RawKeyboardPayloadPresent = false,
    bool RawPointerCoordinatesPresent = false,
    bool RawScreenshotPresent = false,
    bool RawDomPresent = false);

public sealed record TeachNodalCaptureReview(
    TeachNodalCaptureSessionState State,
    TeachNodalDemonstration Demonstration,
    bool ExplicitOptInRecorded,
    bool ApplicationScopeBound,
    bool CanCompileVerifiedSkill,
    int ObservationCount,
    int PerStepApprovalsRequested,
    IReadOnlyList<string> Findings,
    IReadOnlyList<string> EvidenceRefs,
    bool RawInputStored,
    bool GlobalHooksUsed,
    bool ExecutionAuthorityGranted);

public sealed class TeachNodalCaptureSessionV1
{
    public static readonly TimeSpan MaximumConsentLifetime = TimeSpan.FromHours(8);

    private readonly object _gate = new();
    private readonly List<CapturedStep> _observations = [];
    private TeachNodalCaptureConsent? _consent;
    private DateTimeOffset? _startedAtUtc;

    public TeachNodalCaptureSessionV1(TeachNodalCaptureScope scope)
    {
        Scope = NormalizeScope(scope ?? throw new ArgumentNullException(nameof(scope)));
    }

    public TeachNodalCaptureScope Scope { get; }
    public TeachNodalCaptureSessionState State { get; private set; } = TeachNodalCaptureSessionState.Created;

    public int ObservationCount
    {
        get
        {
            lock (_gate)
                return _observations.Count;
        }
    }

    public void Arm(TeachNodalCaptureConsent consent, DateTimeOffset nowUtc)
    {
        ArgumentNullException.ThrowIfNull(consent);
        lock (_gate)
        {
            RequireState(TeachNodalCaptureSessionState.Created);
            if (!consent.ExplicitOptIn)
                throw new InvalidOperationException("Teach NODAL capture requires explicit operator opt-in.");
            if (consent.GrantedAtUtc > nowUtc || consent.ExpiresAtUtc <= nowUtc)
                throw new InvalidOperationException("Teach NODAL capture consent is not active.");
            if (consent.ExpiresAtUtc <= consent.GrantedAtUtc ||
                consent.ExpiresAtUtc - consent.GrantedAtUtc > MaximumConsentLifetime)
            {
                throw new InvalidOperationException("Teach NODAL capture consent exceeds the bounded session lifetime.");
            }
            ArgumentNullException.ThrowIfNull(consent.AllowedSources);
            if (consent.AllowedSources.Count == 0)
                throw new InvalidOperationException("At least one application-scoped capture source is required.");

            _consent = consent with
            {
                AllowedSources = new HashSet<TeachNodalCaptureSource>(consent.AllowedSources),
                ConsentEvidenceRef = Identifier(consent.ConsentEvidenceRef, 160, "consent evidence reference")
            };
            State = TeachNodalCaptureSessionState.Armed;
        }
    }

    public void Start(DateTimeOffset startedAtUtc)
    {
        lock (_gate)
        {
            RequireState(TeachNodalCaptureSessionState.Armed);
            var consent = _consent ?? throw new InvalidOperationException("Teach NODAL capture consent is missing.");
            if (startedAtUtc < consent.GrantedAtUtc || startedAtUtc >= consent.ExpiresAtUtc)
                throw new InvalidOperationException("Teach NODAL capture cannot start outside the consent window.");
            _startedAtUtc = startedAtUtc;
            State = TeachNodalCaptureSessionState.Capturing;
        }
    }

    public void Observe(TeachNodalCaptureObservation observation)
    {
        ArgumentNullException.ThrowIfNull(observation);
        lock (_gate)
        {
            RequireState(TeachNodalCaptureSessionState.Capturing);
            _observations.Add(NormalizeObservation(observation));
        }
    }

    public TeachNodalCaptureReview Complete(DateTimeOffset completedAtUtc)
    {
        lock (_gate)
        {
            RequireState(TeachNodalCaptureSessionState.Capturing);
            var consent = _consent ?? throw new InvalidOperationException("Teach NODAL capture consent is missing.");
            if (_observations.Count == 0)
                throw new InvalidOperationException("Teach NODAL capture needs at least one observation before review.");
            if (_startedAtUtc is null || completedAtUtc < _startedAtUtc || completedAtUtc >= consent.ExpiresAtUtc)
                throw new InvalidOperationException("Teach NODAL capture completion time is outside the active consent window.");

            var demonstration = new TeachNodalDemonstration(
                DemonstrationId: Scope.CaptureId,
                TitleRedacted: Scope.TitleRedacted,
                WorkspaceScope: Scope.WorkspaceScope,
                AppProfileId: Scope.AppProfileId,
                AppProfileVersion: Scope.AppProfileVersion,
                Surface: Scope.Surface,
                Steps: _observations.Select(ToDemonstrationStep).ToArray(),
                AuthorizedCapabilities: Scope.AuthorizedCapabilities,
                RiskProfile: Scope.RiskProfile,
                ObservedAtUtc: _observations.Min(value => value.Observation.CapturedAtUtc),
                EvidenceRefs: Evidence(
                    _observations.SelectMany(value => value.EvidenceRefs)
                        .Append(consent.ConsentEvidenceRef)));
            var findings = _observations
                .Where(value => !value.VerificationReport.Success)
                .Select(value => $"Step '{value.Observation.StepId}' remains review-only because semantic verification did not pass.")
                .Concat(_observations.Where(value => value.Observation.Action.UserCorrectionMarker)
                    .Select(value => $"Step '{value.Observation.StepId}' contains an operator correction marker."))
                .Concat(_observations.Where(value => !string.IsNullOrWhiteSpace(value.Observation.Action.AmbiguityReasonRedacted))
                    .Select(value => $"Step '{value.Observation.StepId}' contains an ambiguous target and needs review."))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();
            var canCompile = _observations.All(value =>
                value.VerificationReport.Success &&
                !value.Observation.Action.UserCorrectionMarker &&
                string.IsNullOrWhiteSpace(value.Observation.Action.AmbiguityReasonRedacted) &&
                value.Observation.Before.StateFingerprint != value.Observation.After.StateFingerprint &&
                !value.Observation.Before.HasBlockingConflicts &&
                !value.Observation.After.HasBlockingConflicts);

            State = TeachNodalCaptureSessionState.ReviewReady;
            return new TeachNodalCaptureReview(
                State,
                demonstration,
                ExplicitOptInRecorded: true,
                ApplicationScopeBound: true,
                CanCompileVerifiedSkill: canCompile,
                ObservationCount: _observations.Count,
                PerStepApprovalsRequested: 0,
                Findings: findings,
                EvidenceRefs: demonstration.EvidenceRefs,
                RawInputStored: false,
                GlobalHooksUsed: false,
                ExecutionAuthorityGranted: false);
        }
    }

    public void Cancel(DateTimeOffset cancelledAtUtc)
    {
        lock (_gate)
        {
            if (State is TeachNodalCaptureSessionState.ReviewReady or TeachNodalCaptureSessionState.Cancelled)
                throw new InvalidOperationException($"Teach NODAL capture cannot cancel from terminal state '{State}'.");
            if (_startedAtUtc is not null && cancelledAtUtc < _startedAtUtc)
                throw new InvalidOperationException("Teach NODAL capture cancellation time is invalid.");
            _observations.Clear();
            _consent = null;
            _startedAtUtc = null;
            State = TeachNodalCaptureSessionState.Cancelled;
        }
    }

    private CapturedStep NormalizeObservation(TeachNodalCaptureObservation observation)
    {
        var consent = _consent ?? throw new InvalidOperationException("Teach NODAL capture consent is missing.");
        ArgumentNullException.ThrowIfNull(observation.Action);
        ArgumentNullException.ThrowIfNull(observation.Before);
        ArgumentNullException.ThrowIfNull(observation.After);
        ArgumentNullException.ThrowIfNull(observation.VerificationPlan);
        ArgumentNullException.ThrowIfNull(observation.EvidenceRefs);

        if (_startedAtUtc is null || observation.CapturedAtUtc < _startedAtUtc || observation.CapturedAtUtc >= consent.ExpiresAtUtc)
            throw new InvalidOperationException("Capture observation is outside the active consent window.");
        if (observation.Sequence != _observations.Count + 1L)
            throw new InvalidOperationException("Capture observations must use a contiguous monotonic sequence.");
        if (!consent.AllowedSources.Contains(observation.Source))
            throw new InvalidOperationException("Capture source is outside the operator-approved scope.");
        if (observation.IntentSource is not (TrustedControlSource.UserInstruction or TrustedControlSource.OperatorDecision))
            throw new InvalidOperationException("Captured intent must come from trusted user or operator authority.");
        if (!observation.TargetApplicationForeground || !observation.Before.IsForeground || !observation.After.IsForeground)
            throw new InvalidOperationException("Capture is limited to the explicitly bound foreground application.");
        if (observation.GlobalHookUsed || observation.RawKeyboardPayloadPresent || observation.RawPointerCoordinatesPresent ||
            observation.RawScreenshotPresent || observation.RawDomPresent)
        {
            throw new InvalidOperationException("Raw input, global hooks, screenshots, DOM payloads or coordinates cannot enter Teach NODAL capture memory.");
        }
        if (observation.ActionExecuted && observation.ActionRejected)
            throw new InvalidOperationException("A captured action cannot be both executed and rejected.");
        if (observation.VerificationElapsed < TimeSpan.Zero)
            throw new InvalidOperationException("Semantic verification elapsed time cannot be negative.");
        if (_observations.Count >= Scope.MaximumSteps)
            throw new InvalidOperationException("Teach NODAL capture reached the bounded step limit.");
        if (!string.IsNullOrWhiteSpace(observation.VerificationPlan.AppProfileRef) &&
            !string.Equals(observation.VerificationPlan.AppProfileRef, Scope.AppProfileId, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Semantic verification plan does not belong to the bound application profile.");
        }
        if (!string.Equals(observation.Before.Application.ApplicationRef, Scope.ApplicationRef, StringComparison.Ordinal) ||
            !string.Equals(observation.After.Application.ApplicationRef, Scope.ApplicationRef, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Capture observation does not belong to the bound application.");
        }
        if (!observation.Before.SecretsExcluded || !observation.After.SecretsExcluded ||
            observation.Before.ContainsRawScreenshot || observation.After.ContainsRawScreenshot ||
            observation.Before.ContainsRawDom || observation.After.ContainsRawDom ||
            observation.Before.ObservedContentCanChangeMissionGoal || observation.After.ObservedContentCanChangeMissionGoal)
        {
            throw new InvalidOperationException("Capture snapshots crossed a raw-data, secret or control-authority boundary.");
        }
        if (observation.Before.HasBlockingConflicts || observation.After.HasBlockingConflicts)
            throw new InvalidOperationException("Blocking perception conflicts require review before capture can continue.");

        var stepId = Identifier(observation.StepId, 120, "step id");
        if (_observations.Any(value => string.Equals(value.Observation.StepId, stepId, StringComparison.Ordinal)))
            throw new InvalidOperationException("Capture step ids must be unique.");
        var action = NormalizeAction(observation.Action);
        if (!Scope.AuthorizedCapabilities.Contains(action.CapabilityId))
            throw new InvalidOperationException("Captured action capability is outside the already authorized capture scope.");
        var evidenceRefs = Evidence(observation.EvidenceRefs
            .Concat(observation.Before.EvidenceRefs)
            .Concat(observation.After.EvidenceRefs));
        if (evidenceRefs.Count == 0)
            throw new InvalidOperationException("Capture observation requires reference-only evidence.");

        var report = new SemanticVerifierV2().Verify(
            observation.VerificationPlan,
            new SemanticVerificationContext(
                observation.Before,
                observation.After,
                observation.ActionExecuted,
                observation.ActionRejected,
                observation.UserInterrupted,
                observation.VerificationElapsed,
                evidenceRefs));
        var normalized = observation with
        {
            StepId = stepId,
            Action = action,
            EvidenceRefs = Evidence(evidenceRefs.Concat(report.EvidenceRefs))
        };
        return new CapturedStep(normalized, report, normalized.EvidenceRefs);
    }

    private static TeachNodalObservedAction NormalizeAction(TeachNodalObservedAction action)
    {
        ArgumentNullException.ThrowIfNull(action.Parameters);
        ArgumentNullException.ThrowIfNull(action.SelectorAliasRefs);
        var actionId = Identifier(action.ActionId, 120, "action id");
        var intent = SafeText(action.IntentRedacted, 500, "trusted intent");
        var capability = Identifier(action.CapabilityId, 160, "capability id");
        var operation = Identifier(action.Operation, 120, "operation");
        var target = Identifier(action.SemanticTargetRef, 160, "semantic target");
        if (action.TargetLabelSource is TrustedControlSource.SystemPolicy or TrustedControlSource.UserInstruction or TrustedControlSource.OperatorDecision)
            throw new InvalidOperationException("Captured target labels must remain observed data rather than control authority.");
        var label = SafeText(action.TargetLabelRedacted, 500, "observed target label", allowPromptLikeContent: true);
        var role = SafeText(action.TargetRoleRedacted, 120, "observed target role", allowPromptLikeContent: true);
        if (action.Parameters.Count > TeachNodalCompilerV1.MaximumParametersPerStep)
            throw new InvalidOperationException("Capture action exceeds the bounded parameter limit.");
        var parameters = action.Parameters.Select(NormalizeParameter).OrderBy(value => value.Name, StringComparer.Ordinal).ToArray();
        var selectors = action.SelectorAliasRefs
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => Identifier(value, 160, "selector alias reference"))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();
        if (selectors.Length == 0)
            throw new InvalidOperationException("Capture action needs at least one semantic selector reference.");

        return action with
        {
            ActionId = actionId,
            IntentRedacted = intent,
            CapabilityId = capability,
            Operation = operation,
            SemanticTargetRef = target,
            TargetLabelRedacted = label,
            TargetRoleRedacted = role,
            Parameters = parameters,
            SelectorAliasRefs = selectors,
            Confidence = Math.Clamp(action.Confidence, 0d, 1d),
            AmbiguityReasonRedacted = string.IsNullOrWhiteSpace(action.AmbiguityReasonRedacted)
                ? null
                : SafeText(action.AmbiguityReasonRedacted, 300, "ambiguity reason", allowPromptLikeContent: true)
        };
    }

    private static TeachNodalParameterObservation NormalizeParameter(TeachNodalParameterObservation parameter)
    {
        ArgumentNullException.ThrowIfNull(parameter);
        var name = Identifier(parameter.Name, 80, "parameter name").ToUpperInvariant();
        var placeholder = SafeText(parameter.Placeholder, 100, "parameter placeholder");
        var valueRef = SafeText(parameter.ValueRef, 240, "parameter value reference");
        if (!string.Equals(placeholder, $"{{{name}}}", StringComparison.Ordinal))
            throw new InvalidOperationException($"Capture parameter '{name}' placeholder must be '{{{name}}}'.");
        if (parameter.SecretByReference)
        {
            if (parameter.Source != TrustedControlSource.OperatorDecision ||
                !(valueRef.StartsWith("secret-ref:", StringComparison.Ordinal) || valueRef.StartsWith("secret://", StringComparison.Ordinal)))
            {
                throw new InvalidOperationException("Secret capture parameters require an opaque reference from an operator decision.");
            }
        }
        else if (!(valueRef.StartsWith("variable-ref:", StringComparison.Ordinal) || valueRef.StartsWith("literal-ref:", StringComparison.Ordinal)))
        {
            throw new InvalidOperationException("Capture parameters must use variable or literal references instead of raw input.");
        }
        return parameter with { Name = name, Placeholder = placeholder, ValueRef = valueRef };
    }

    private static TeachNodalDemonstrationStep ToDemonstrationStep(CapturedStep captured) =>
        new(
            captured.Observation.StepId,
            captured.Observation.Before,
            captured.Observation.Action,
            captured.Observation.After,
            captured.Observation.VerificationPlan,
            captured.VerificationReport,
            captured.EvidenceRefs);

    private static TeachNodalCaptureScope NormalizeScope(TeachNodalCaptureScope scope)
    {
        ArgumentNullException.ThrowIfNull(scope.AuthorizedCapabilities);
        if (scope.AppProfileVersion < 1)
            throw new ArgumentOutOfRangeException(nameof(scope.AppProfileVersion), "Application profile version must be positive.");
        if (scope.AuthorizedCapabilities.Count == 0)
            throw new ArgumentException("Capture scope requires at least one authorized capability.", nameof(scope));
        if (scope.MaximumSteps is < 1 or > TeachNodalCompilerV1.MaximumSteps)
            throw new ArgumentOutOfRangeException(nameof(scope.MaximumSteps), "Capture step limit is outside the supported range.");
        return scope with
        {
            CaptureId = Identifier(scope.CaptureId, 120, "capture id"),
            TitleRedacted = SafeText(scope.TitleRedacted, 240, "capture title"),
            WorkspaceScope = Identifier(scope.WorkspaceScope, 160, "workspace scope"),
            AppProfileId = Identifier(scope.AppProfileId, 160, "app profile id"),
            ApplicationRef = Identifier(scope.ApplicationRef, 160, "application reference"),
            AuthorizedCapabilities = new HashSet<string>(scope.AuthorizedCapabilities.Select(value =>
                Identifier(value, 160, "authorized capability")), StringComparer.Ordinal)
        };
    }

    private void RequireState(TeachNodalCaptureSessionState expected)
    {
        if (State != expected)
            throw new InvalidOperationException($"Teach NODAL capture expected state '{expected}' but was '{State}'.");
    }

    private static IReadOnlyList<string> Evidence(IEnumerable<string> values) => values
        .Where(value => !string.IsNullOrWhiteSpace(value))
        .Select(value => Identifier(value, 160, "evidence reference"))
        .Distinct(StringComparer.Ordinal)
        .OrderBy(value => value, StringComparer.Ordinal)
        .Take(512)
        .ToArray();

    private static string Identifier(string? value, int maximumLength, string field)
    {
        var normalized = SafeRuntimeText.Sanitize(value, maximumLength);
        if (normalized.Length == 0)
            throw new ArgumentException($"{field} is required.");
        if (HistorySanitizer.ContainsSecretLikeContent(value) || ContainsLocalPath(value))
            throw new ArgumentException($"{field} contains secret-like or local-path content.");
        return normalized;
    }

    private static string SafeText(string? value, int maximumLength, string field, bool allowPromptLikeContent = false)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException($"{field} is required.");
        if (HistorySanitizer.ContainsSecretLikeContent(value) || ContainsLocalPath(value))
            throw new ArgumentException($"{field} contains secret-like or local-path content.");
        var normalized = SafeRuntimeText.Sanitize(value, maximumLength);
        if (!allowPromptLikeContent && normalized.Length == 0)
            throw new ArgumentException($"{field} is required.");
        return normalized;
    }

    private static bool ContainsLocalPath(string? value) =>
        !string.IsNullOrWhiteSpace(value) &&
        HistorySanitizer.SanitizeText(value).Contains("[LOCAL_PATH]", StringComparison.Ordinal);

    private sealed record CapturedStep(
        TeachNodalCaptureObservation Observation,
        SemanticVerificationReport VerificationReport,
        IReadOnlyList<string> EvidenceRefs);
}
