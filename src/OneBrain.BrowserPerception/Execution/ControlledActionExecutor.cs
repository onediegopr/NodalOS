namespace OneBrain.BrowserPerception;

public enum ControlledActionExecutionMode
{
    FixtureOnly,
    LiveDisabled
}

public sealed record ControlledActionExecutionRequest(
    SafeBrowserActionPlan ActionPlan,
    BrowserPerceptionSnapshot BeforeSnapshot,
    ControlledActionExecutionMode ExecutionMode,
    FixturePageState? FixturePageState,
    DateTimeOffset RequestedAt,
    string CorrelationId)
{
    public string? FixtureInputValue { get; init; }

    public string? FixtureSelectValue { get; init; }

    public int FixtureScrollDelta { get; init; } = 100;

    public int FixtureWaitTicks { get; init; } = 1;
}

public sealed record ControlledActionExecutionEvidenceDraft(
    string CorrelationId,
    ControlledActionExecutionMode ExecutionMode,
    string BeforeSnapshotRef,
    string AfterSnapshotRef,
    bool MetadataOnly,
    bool FixtureOnly,
    bool LiveExecutionDisabled,
    bool CdpInvoked,
    bool WebSocketInvoked,
    bool BrowserLaunched,
    bool SystemBrowserUsed,
    bool ExtensionInvoked,
    bool ExternalNavigationAttempted,
    bool ProductFilesModified,
    IReadOnlyList<string> SyntheticMarkers);

public sealed record ControlledActionExecutionResult(
    bool Attempted,
    bool Succeeded,
    SafeBrowserActionKind ActionKind,
    bool RequiresHumanHandoff,
    bool AbortedByPrecondition,
    bool FailedPostcondition,
    string Reason,
    string BeforeSnapshotRef,
    string AfterSnapshotRef,
    PreActionVerificationResult PreVerification,
    PostActionVerificationResult PostVerification,
    ControlledActionExecutionEvidenceDraft EvidenceDraft);

public sealed class FixturePageState
{
    private readonly List<string> clickedElementRefs = [];
    private readonly Dictionary<string, string> typedValues = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> selectedValues = new(StringComparer.Ordinal);
    private readonly List<string> syntheticDomChangeMarkers = [];

    public string? FocusedElementRef { get; private set; }

    public IReadOnlyList<string> ClickedElementRefs => clickedElementRefs;

    public IReadOnlyDictionary<string, string> TypedValues => typedValues;

    public IReadOnlyDictionary<string, string> SelectedValues => selectedValues;

    public int ScrollPosition { get; private set; }

    public int WaitTicks { get; private set; }

    public IReadOnlyList<string> SyntheticDomChangeMarkers => syntheticDomChangeMarkers;

    public void Focus(string elementRef)
    {
        FocusedElementRef = elementRef;
        AddSyntheticDomChangeMarker("focused");
    }

    public void Click(string elementRef, IEnumerable<string> expectedMarkers)
    {
        clickedElementRefs.Add(elementRef);
        var markers = expectedMarkers.ToArray();
        if (markers.Length == 0)
        {
            AddSyntheticDomChangeMarker("clicked");
            return;
        }

        foreach (var marker in markers)
        {
            AddSyntheticDomChangeMarker(marker);
        }
    }

    public void Type(string elementRef, string value)
    {
        typedValues[elementRef] = value;
        AddSyntheticDomChangeMarker("input metadata changed");
    }

    public void Select(string elementRef, string value)
    {
        selectedValues[elementRef] = value;
        AddSyntheticDomChangeMarker("selected metadata changed");
    }

    public void ScrollBy(int delta)
    {
        ScrollPosition += delta;
        AddSyntheticDomChangeMarker("scrolled");
    }

    public void Wait(int ticks)
    {
        WaitTicks += Math.Max(1, ticks);
        AddSyntheticDomChangeMarker("stable");
    }

    private void AddSyntheticDomChangeMarker(string marker)
    {
        if (!string.IsNullOrWhiteSpace(marker))
            syntheticDomChangeMarkers.Add(marker);
    }
}

public sealed class ControlledActionExecutor
{
    private static readonly BrowserEvidenceRedactor SensitiveInputRedactor = new();

    private static readonly SafeBrowserActionKind[] SupportedActions =
    [
        SafeBrowserActionKind.Scroll,
        SafeBrowserActionKind.Focus,
        SafeBrowserActionKind.Click,
        SafeBrowserActionKind.Type,
        SafeBrowserActionKind.Select,
        SafeBrowserActionKind.Wait
    ];

    public ControlledActionExecutionResult Execute(ControlledActionExecutionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.ActionPlan);
        ArgumentNullException.ThrowIfNull(request.BeforeSnapshot);

        var beforeRef = request.BeforeSnapshot.SnapshotId;
        var afterRef = "not-created";

        if (request.ExecutionMode != ControlledActionExecutionMode.FixtureOnly)
        {
            return Abort(
                request,
                beforeRef,
                afterRef,
                "Live execution is disabled; fixture-only mode is required.",
                BrowserActionPreconditionKind.LiveExecutionDisabled,
                requiresHumanHandoff: false);
        }

        if (request.FixturePageState is null || !IsFixtureSafe(request.BeforeSnapshot))
        {
            return Abort(
                request,
                beforeRef,
                afterRef,
                "External/live page execution is blocked; fixture state and fixture snapshot are required.",
                BrowserActionPreconditionKind.FixtureOrControlledPageOnly,
                requiresHumanHandoff: false);
        }

        if (request.ActionPlan.RequiresHumanHandoff || request.ActionPlan.ActionKind == SafeBrowserActionKind.HumanHandoff)
        {
            return Abort(
                request,
                beforeRef,
                afterRef,
                "Human handoff plan cannot be executed, even in fixture mode.",
                BrowserActionPreconditionKind.NoHumanHandoffBlockage,
                requiresHumanHandoff: true);
        }

        if (!SupportedActions.Contains(request.ActionPlan.ActionKind))
        {
            return Abort(
                request,
                beforeRef,
                afterRef,
                "Unsupported action kind is blocked by fixture executor.",
                BrowserActionPreconditionKind.SupportedAction,
                requiresHumanHandoff: false);
        }

        if (request.ActionPlan.ActionKind == SafeBrowserActionKind.Type && ContainsSensitiveInput(request.FixtureInputValue))
        {
            return Abort(
                request,
                beforeRef,
                afterRef,
                "Sensitive input is blocked and requires human handoff.",
                BrowserActionPreconditionKind.SensitiveInputSafe,
                requiresHumanHandoff: true);
        }

        var verifier = new BrowserActionVerifier();
        var preVerification = verifier.VerifyPreconditions(request.ActionPlan, request.BeforeSnapshot);
        if (!preVerification.CanProceed)
        {
            return BuildResult(
                request,
                attempted: false,
                succeeded: false,
                requiresHumanHandoff: preVerification.RequiresHumanHandoff,
                abortedByPrecondition: true,
                failedPostcondition: false,
                reason: preVerification.Reason,
                beforeRef,
                afterRef,
                preVerification,
                EmptyPostVerification("Action was not attempted because preconditions failed."),
                request.FixturePageState);
        }

        ExecuteFixtureAction(request, request.FixturePageState);
        var afterSnapshot = BuildAfterSnapshot(request.BeforeSnapshot, request.FixturePageState, request.CorrelationId, request.RequestedAt);
        afterRef = afterSnapshot.SnapshotId;
        var postVerification = verifier.VerifyPostconditions(request.ActionPlan, request.BeforeSnapshot, afterSnapshot);

        return BuildResult(
            request,
            attempted: true,
            succeeded: postVerification.ActionSucceeded,
            requiresHumanHandoff: postVerification.RequiresHumanHandoff,
            abortedByPrecondition: false,
            failedPostcondition: !postVerification.ActionSucceeded,
            reason: postVerification.Reason,
            beforeRef,
            afterRef,
            preVerification,
            postVerification,
            request.FixturePageState);
    }

    private static void ExecuteFixtureAction(
        ControlledActionExecutionRequest request,
        FixturePageState state)
    {
        var elementRef = request.ActionPlan.TargetLocator?.ElementRef ?? "fixture:document";
        switch (request.ActionPlan.ActionKind)
        {
            case SafeBrowserActionKind.Focus:
                state.Focus(elementRef);
                break;
            case SafeBrowserActionKind.Click:
                state.Click(elementRef, ExpectedStateMarkers(request.ActionPlan));
                break;
            case SafeBrowserActionKind.Type:
                state.Type(elementRef, SafeFixtureValue(request.FixtureInputValue, "fixture typed value"));
                break;
            case SafeBrowserActionKind.Select:
                state.Select(elementRef, SafeFixtureValue(request.FixtureSelectValue, "fixture option"));
                break;
            case SafeBrowserActionKind.Scroll:
                state.ScrollBy(request.FixtureScrollDelta);
                break;
            case SafeBrowserActionKind.Wait:
                state.Wait(request.FixtureWaitTicks);
                break;
        }
    }

    private static BrowserPerceptionSnapshot BuildAfterSnapshot(
        BrowserPerceptionSnapshot before,
        FixturePageState state,
        string correlationId,
        DateTimeOffset requestedAt)
    {
        var markerPreview = string.Join(' ', state.SyntheticDomChangeMarkers);
        var textPreview = string.IsNullOrWhiteSpace(before.PageTextPreviewRedacted)
            ? markerPreview
            : string.Concat(before.PageTextPreviewRedacted, " ", markerPreview).Trim();

        return before with
        {
            SnapshotId = before.SnapshotId + ":fixture-after:" + RedactRef(correlationId),
            CapturedAt = requestedAt,
            PageTextPreviewRedacted = textPreview,
            Lifecycle = before.Lifecycle with
            {
                ReadyState = "complete",
                NetworkIdle = true
            }
        };
    }

    private static ControlledActionExecutionResult Abort(
        ControlledActionExecutionRequest request,
        string beforeRef,
        string afterRef,
        string reason,
        BrowserActionPreconditionKind failedKind,
        bool requiresHumanHandoff)
    {
        var preVerification = new PreActionVerificationResult(
            CanProceed: false,
            FailedPreconditions:
            [
                new BrowserActionPrecondition(
                    failedKind,
                    Expected: "allowed fixture execution",
                    Actual: "blocked",
                    Satisfied: false,
                    Reason: reason)
            ],
            Reason: reason,
            RequiresHumanHandoff: requiresHumanHandoff);

        return BuildResult(
            request,
            attempted: false,
            succeeded: false,
            requiresHumanHandoff,
            abortedByPrecondition: true,
            failedPostcondition: false,
            reason,
            beforeRef,
            afterRef,
            preVerification,
            EmptyPostVerification("Action was not attempted."),
            request.FixturePageState);
    }

    private static ControlledActionExecutionResult BuildResult(
        ControlledActionExecutionRequest request,
        bool attempted,
        bool succeeded,
        bool requiresHumanHandoff,
        bool abortedByPrecondition,
        bool failedPostcondition,
        string reason,
        string beforeRef,
        string afterRef,
        PreActionVerificationResult preVerification,
        PostActionVerificationResult postVerification,
        FixturePageState? state)
    {
        return new ControlledActionExecutionResult(
            attempted,
            succeeded,
            request.ActionPlan.ActionKind,
            requiresHumanHandoff,
            abortedByPrecondition,
            failedPostcondition,
            reason,
            beforeRef,
            afterRef,
            preVerification,
            postVerification,
            BuildEvidenceDraft(request, beforeRef, afterRef, state));
    }

    private static ControlledActionExecutionEvidenceDraft BuildEvidenceDraft(
        ControlledActionExecutionRequest request,
        string beforeRef,
        string afterRef,
        FixturePageState? state) =>
        new(
            request.CorrelationId,
            request.ExecutionMode,
            beforeRef,
            afterRef,
            MetadataOnly: true,
            FixtureOnly: request.ExecutionMode == ControlledActionExecutionMode.FixtureOnly,
            LiveExecutionDisabled: true,
            CdpInvoked: false,
            WebSocketInvoked: false,
            BrowserLaunched: false,
            SystemBrowserUsed: false,
            ExtensionInvoked: false,
            ExternalNavigationAttempted: false,
            ProductFilesModified: false,
            SyntheticMarkers: state?.SyntheticDomChangeMarkers.ToArray() ?? []);

    private static PostActionVerificationResult EmptyPostVerification(string reason) =>
        new(
            ActionSucceeded: false,
            FailedPostconditions: [],
            reason,
            RequiresHumanHandoff: false);

    private static IEnumerable<string> ExpectedStateMarkers(SafeBrowserActionPlan plan) =>
        plan.ExpectedPostconditions
            .Where(postcondition => postcondition.Kind == BrowserActionPostconditionKind.ExpectedStateObserved)
            .Select(postcondition => postcondition.Expected)
            .Where(expected => !string.IsNullOrWhiteSpace(expected));

    private static string SafeFixtureValue(string? candidate, string fallback) =>
        string.IsNullOrWhiteSpace(candidate) ? fallback : candidate.Trim();

    private static bool ContainsSensitiveInput(string? value)
        => SensitiveInputRedactor.ContainsSensitiveValue(value);

    private static bool IsFixtureSafe(BrowserPerceptionSnapshot snapshot) =>
        string.Equals(snapshot.Source, "fixture-safe-read-only", StringComparison.Ordinal);

    private static string RedactRef(string value) =>
        string.IsNullOrWhiteSpace(value)
            ? "missing-correlation"
            : new string(value.Where(character => char.IsLetterOrDigit(character) || character is '-' or '_').Take(80).ToArray());
}
