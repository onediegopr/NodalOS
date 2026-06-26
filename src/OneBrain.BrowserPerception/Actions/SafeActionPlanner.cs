namespace OneBrain.BrowserPerception;

public enum SafeBrowserActionKind
{
    Scroll,
    Focus,
    Click,
    Type,
    Select,
    Wait,
    HumanHandoff
}

public sealed record SafeBrowserActionEvidencePlan(
    string SnapshotBeforeRef,
    string ExpectedSnapshotAfterRef,
    bool MetadataOnly,
    bool RedactionRequired,
    bool NoSensitivePayloadGuarantee);

public sealed record SafeBrowserActionPlan(
    SafeBrowserActionKind ActionKind,
    ElementLocator? TargetLocator,
    LocatorStrategy LocatorStrategy,
    double Confidence,
    string Reason,
    IReadOnlyList<BrowserActionPrecondition> Preconditions,
    IReadOnlyList<BrowserActionPostcondition> ExpectedPostconditions,
    bool RequiresHumanApproval,
    bool RequiresHumanHandoff,
    bool CanExecuteInFixtureOnly,
    bool ProhibitedOnExternalPages,
    SafeBrowserActionEvidencePlan EvidencePlan)
{
    public bool PlanOnly => true;

    public bool ExecutesAction => false;
}

public sealed class SafeActionPlanner
{
    private const double MinimumConfidence = 0.5;

    public IReadOnlyList<SafeBrowserActionPlan> PlanActions(
        PageTechnologyProfile profile,
        BrowserPerceptionSnapshot snapshot,
        string objective)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(snapshot);

        var blockages = new BlockageDetector().DetectBlockages(snapshot);
        if (blockages.Any(blockage => blockage.RequiresHumanHandoff))
        {
            return [BuildHumanHandoffPlan(snapshot, "Human handoff blockage prevents safe action planning.")];
        }

        if (RequiresSensitiveOrBypassAction(objective))
        {
            return [BuildHumanHandoffPlan(snapshot, "Objective requests sensitive data entry, credentials, verification, or bypass.")];
        }

        var locatorStrategy = new LocatorEngine().SelectLocatorStrategy(profile, snapshot);
        if (locatorStrategy.HumanHandoffRequired || IsLowConfidenceOrContradictory(profile, snapshot))
        {
            return [BuildHumanHandoffPlan(snapshot, locatorStrategy.Reason)];
        }

        if (profile.UsesCanvas)
        {
            return [BuildHumanHandoffPlan(snapshot, "Visual/canvas surfaces are not actionable in this read-only block.")];
        }

        if (snapshot.Forms.FormsCount > 0 && snapshot.Forms.InputsCount > 0)
        {
            return BuildLegacyFormPlans(snapshot, locatorStrategy);
        }

        if (profile.LooksLikeSpa || profile.HasSemanticAccessibility)
        {
            return BuildSpaPlans(snapshot, locatorStrategy);
        }

        return [BuildHumanHandoffPlan(snapshot, "No fixture-safe action plan is available for this page shape.")];
    }

    private static IReadOnlyList<SafeBrowserActionPlan> BuildLegacyFormPlans(
        BrowserPerceptionSnapshot snapshot,
        LocatorStrategy locatorStrategy)
    {
        var inputLocator = new ElementLocator(
            ElementLocatorType.Css,
            "input[name=\"fixture-input\"]",
            0.72,
            "Theoretical fixture input candidate.",
            "fixture:input:0");
        var buttonLocator = new ElementLocator(
            ElementLocatorType.Css,
            "button[type=\"button\"]",
            0.7,
            "Theoretical fixture button candidate.",
            "fixture:button:0");

        return
        [
            BuildPlan(
                SafeBrowserActionKind.Type,
                inputLocator,
                locatorStrategy,
                0.72,
                "Theoretical fixture-only type plan for a legacy form input.",
                snapshot,
                [Expected(BrowserActionPostconditionKind.InputValueChanged, "input metadata changed")]),
            BuildPlan(
                SafeBrowserActionKind.Click,
                buttonLocator,
                locatorStrategy,
                0.7,
                "Theoretical fixture-only click plan for a legacy form button.",
                snapshot,
                [Expected(BrowserActionPostconditionKind.ExpectedStateObserved, "submitted")])
        ];
    }

    private static IReadOnlyList<SafeBrowserActionPlan> BuildSpaPlans(
        BrowserPerceptionSnapshot snapshot,
        LocatorStrategy locatorStrategy)
    {
        var locator = new ElementLocator(
            ElementLocatorType.Accessibility,
            "button:fixture action",
            0.82,
            "Theoretical accessibility candidate.",
            "fixture:spa-action:0");

        return
        [
            BuildPlan(
                SafeBrowserActionKind.Focus,
                locator,
                locatorStrategy,
                0.78,
                "Theoretical fixture-only focus plan using accessibility locator metadata.",
                snapshot,
                [Expected(BrowserActionPostconditionKind.ExpectedStateObserved, "focused")]),
            BuildPlan(
                SafeBrowserActionKind.Click,
                locator,
                locatorStrategy,
                0.76,
                "Theoretical fixture-only click plan using accessibility locator metadata.",
                snapshot,
                [Expected(BrowserActionPostconditionKind.ExpectedStateObserved, "clicked")])
        ];
    }

    private static SafeBrowserActionPlan BuildHumanHandoffPlan(
        BrowserPerceptionSnapshot snapshot,
        string reason)
    {
        var locatorStrategy = LocatorStrategy.HumanHandoff(reason);
        return new SafeBrowserActionPlan(
            SafeBrowserActionKind.HumanHandoff,
            TargetLocator: null,
            locatorStrategy,
            locatorStrategy.Confidence,
            reason,
            Preconditions:
            [
                new BrowserActionPrecondition(
                    BrowserActionPreconditionKind.NoHumanHandoffBlockage,
                    Expected: "no human handoff",
                    Actual: "human handoff required",
                    Satisfied: false,
                    Reason: reason)
            ],
            ExpectedPostconditions: [],
            RequiresHumanApproval: true,
            RequiresHumanHandoff: true,
            CanExecuteInFixtureOnly: true,
            ProhibitedOnExternalPages: true,
            EvidencePlan: BuildEvidencePlan(snapshot));
    }

    private static SafeBrowserActionPlan BuildPlan(
        SafeBrowserActionKind actionKind,
        ElementLocator targetLocator,
        LocatorStrategy locatorStrategy,
        double confidence,
        string reason,
        BrowserPerceptionSnapshot snapshot,
        IReadOnlyList<BrowserActionPostcondition> expectedPostconditions)
    {
        return new SafeBrowserActionPlan(
            actionKind,
            targetLocator,
            locatorStrategy,
            confidence,
            reason,
            Preconditions:
            [
                new BrowserActionPrecondition(BrowserActionPreconditionKind.NoHumanHandoffBlockage, "none", "none", true, "No human handoff blockage detected."),
                new BrowserActionPrecondition(BrowserActionPreconditionKind.TargetLocatorPresent, "present", "present", true, "Target locator candidate exists."),
                new BrowserActionPrecondition(BrowserActionPreconditionKind.TargetVisible, "visible", "visible", true, "Fixture metadata marks target visible."),
                new BrowserActionPrecondition(BrowserActionPreconditionKind.TargetEnabled, "enabled", "enabled", true, "Fixture metadata marks target enabled."),
                new BrowserActionPrecondition(BrowserActionPreconditionKind.PageStable, "stable", snapshot.Lifecycle.ReadyState, snapshot.Lifecycle.ReadyState == "complete", "Snapshot lifecycle state is used; no live page is queried."),
                new BrowserActionPrecondition(BrowserActionPreconditionKind.ConfidenceAboveThreshold, ">=0.5", confidence.ToString("0.00"), confidence >= MinimumConfidence, "Plan confidence threshold."),
                new BrowserActionPrecondition(BrowserActionPreconditionKind.FixtureOrControlledPageOnly, "fixture-safe-read-only", snapshot.Source, IsFixtureSafe(snapshot), "Planner is fixture-safe only.")
            ],
            expectedPostconditions,
            RequiresHumanApproval: true,
            RequiresHumanHandoff: false,
            CanExecuteInFixtureOnly: true,
            ProhibitedOnExternalPages: true,
            EvidencePlan: BuildEvidencePlan(snapshot));
    }

    private static BrowserActionPostcondition Expected(
        BrowserActionPostconditionKind kind,
        string expected) =>
        new(kind, expected, Actual: "not-evaluated", Satisfied: false, Reason: "Expected postcondition for future verification.");

    private static SafeBrowserActionEvidencePlan BuildEvidencePlan(BrowserPerceptionSnapshot snapshot) =>
        new(
            SnapshotBeforeRef: snapshot.SnapshotId,
            ExpectedSnapshotAfterRef: "pending:post-action-snapshot",
            MetadataOnly: true,
            RedactionRequired: true,
            NoSensitivePayloadGuarantee: !snapshot.StoresRawDom && !snapshot.StoresSensitivePayloads && !snapshot.StorageMetadata.ValuesCaptured);

    private static bool IsLowConfidenceOrContradictory(
        PageTechnologyProfile profile,
        BrowserPerceptionSnapshot snapshot)
    {
        var complexSignals = CountTrue(profile.UsesFrames, profile.UsesShadowDom, profile.UsesCanvas, profile.LooksLikeSpa);
        var noActionableMetadata =
            !profile.HasSemanticAccessibility
            && !profile.LooksLikeServerRendered
            && snapshot.Forms.FormsCount == 0
            && snapshot.InteractiveElements.ElementsCount == 0;

        return complexSignals >= 3 || noActionableMetadata;
    }

    private static bool RequiresSensitiveOrBypassAction(string objective)
    {
        if (string.IsNullOrWhiteSpace(objective))
            return false;

        var normalized = objective.ToLowerInvariant();
        var blockedTerms = new[]
        {
            "password",
            "credential",
            "secret",
            "token",
            "api key",
            "apikey",
            "pin",
            "cvv",
            "cvc",
            "ssn",
            "social security",
            "credit card",
            "card number",
            "cardnumber",
            "debit card",
            "secret answer",
            "secret question",
            "2fa",
            "two factor",
            "captcha",
            "anti-bot",
            "antibot",
            "authenticate",
            "signin",
            "login",
            "sign in",
            "verify",
            "verification",
            "bypass"
        };

        return blockedTerms.Any(normalized.Contains);
    }

    private static bool IsFixtureSafe(BrowserPerceptionSnapshot snapshot) =>
        string.Equals(snapshot.Source, "fixture-safe-read-only", StringComparison.Ordinal);

    private static int CountTrue(params bool[] values) =>
        values.Count(value => value);
}
