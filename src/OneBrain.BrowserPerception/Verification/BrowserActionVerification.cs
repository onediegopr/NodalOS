namespace OneBrain.BrowserPerception;

public enum BrowserActionPreconditionKind
{
    NoHumanHandoffBlockage,
    TargetLocatorPresent,
    TargetVisible,
    TargetEnabled,
    PageStable,
    ConfidenceAboveThreshold,
    FixtureOrControlledPageOnly
}

public enum BrowserActionPostconditionKind
{
    UrlChanged,
    DomChanged,
    ElementAppeared,
    ElementDisappeared,
    InputValueChanged,
    NetworkSettled,
    NoCriticalConsoleError,
    ExpectedStateObserved
}

public sealed record BrowserActionPrecondition(
    BrowserActionPreconditionKind Kind,
    string Expected,
    string Actual,
    bool Satisfied,
    string Reason);

public sealed record BrowserActionPostcondition(
    BrowserActionPostconditionKind Kind,
    string Expected,
    string Actual,
    bool Satisfied,
    string Reason);

public sealed record PreActionVerificationResult(
    bool CanProceed,
    IReadOnlyList<BrowserActionPrecondition> FailedPreconditions,
    string Reason,
    bool RequiresHumanHandoff);

public sealed record PostActionVerificationResult(
    bool ActionSucceeded,
    IReadOnlyList<BrowserActionPostcondition> FailedPostconditions,
    string Reason,
    bool RequiresHumanHandoff);

public sealed class BrowserActionVerifier
{
    private const double MinimumConfidence = 0.5;

    public PreActionVerificationResult VerifyPreconditions(
        SafeBrowserActionPlan plan,
        BrowserPerceptionSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(snapshot);

        var failed = new List<BrowserActionPrecondition>();
        var blockages = new BlockageDetector().DetectBlockages(snapshot);
        if (plan.RequiresHumanHandoff || blockages.Any(blockage => blockage.RequiresHumanHandoff))
        {
            failed.Add(new BrowserActionPrecondition(
                BrowserActionPreconditionKind.NoHumanHandoffBlockage,
                "no human handoff",
                "human handoff required",
                Satisfied: false,
                "Human handoff blockage prevents pre-action approval."));
        }

        if (plan.ActionKind != SafeBrowserActionKind.HumanHandoff && plan.TargetLocator is null)
        {
            failed.Add(new BrowserActionPrecondition(
                BrowserActionPreconditionKind.TargetLocatorPresent,
                "present",
                "missing",
                Satisfied: false,
                "Target locator is required for non-handoff plans."));
        }

        if (plan.Confidence < MinimumConfidence)
        {
            failed.Add(new BrowserActionPrecondition(
                BrowserActionPreconditionKind.ConfidenceAboveThreshold,
                ">=0.5",
                plan.Confidence.ToString("0.00"),
                Satisfied: false,
                "Plan confidence is below threshold."));
        }

        if (!IsFixtureSafe(snapshot))
        {
            failed.Add(new BrowserActionPrecondition(
                BrowserActionPreconditionKind.FixtureOrControlledPageOnly,
                "fixture-safe-read-only",
                snapshot.Source,
                Satisfied: false,
                "Verification is limited to fixture or controlled snapshots."));
        }

        failed.AddRange(plan.Preconditions.Where(precondition => !precondition.Satisfied));

        var distinctFailed = failed
            .GroupBy(precondition => precondition.Kind)
            .Select(group => group.First())
            .ToArray();

        return new PreActionVerificationResult(
            CanProceed: distinctFailed.Length == 0,
            FailedPreconditions: distinctFailed,
            Reason: distinctFailed.Length == 0 ? "All preconditions satisfied in fixture metadata." : "One or more preconditions failed.",
            RequiresHumanHandoff: distinctFailed.Any(precondition => precondition.Kind == BrowserActionPreconditionKind.NoHumanHandoffBlockage));
    }

    public PostActionVerificationResult VerifyPostconditions(
        SafeBrowserActionPlan plan,
        BrowserPerceptionSnapshot beforeSnapshot,
        BrowserPerceptionSnapshot afterSnapshot)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(beforeSnapshot);
        ArgumentNullException.ThrowIfNull(afterSnapshot);

        if (plan.RequiresHumanHandoff)
        {
            return new PostActionVerificationResult(
                ActionSucceeded: false,
                FailedPostconditions:
                [
                    new BrowserActionPostcondition(
                        BrowserActionPostconditionKind.ExpectedStateObserved,
                        "no human handoff",
                        "human handoff required",
                        Satisfied: false,
                        "Human handoff plans cannot be verified as successful actions.")
                ],
                Reason: "Human handoff required.",
                RequiresHumanHandoff: true);
        }

        var evaluated = plan.ExpectedPostconditions.Select(condition => Evaluate(condition, beforeSnapshot, afterSnapshot)).ToArray();
        var failed = evaluated.Where(condition => !condition.Satisfied).ToArray();

        return new PostActionVerificationResult(
            ActionSucceeded: failed.Length == 0,
            FailedPostconditions: failed,
            Reason: failed.Length == 0 ? "All expected postconditions matched fixture snapshots." : "One or more postconditions did not match.",
            RequiresHumanHandoff: false);
    }

    private static BrowserActionPostcondition Evaluate(
        BrowserActionPostcondition condition,
        BrowserPerceptionSnapshot before,
        BrowserPerceptionSnapshot after)
    {
        return condition.Kind switch
        {
            BrowserActionPostconditionKind.UrlChanged => WithActual(
                condition,
                before.PageUrlRedacted != after.PageUrlRedacted,
                $"{before.PageUrlRedacted} -> {after.PageUrlRedacted}"),
            BrowserActionPostconditionKind.DomChanged => WithActual(
                condition,
                before.PageTextPreviewRedacted != after.PageTextPreviewRedacted
                    || before.InteractiveElements.ElementsCount != after.InteractiveElements.ElementsCount,
                "metadata-diff"),
            BrowserActionPostconditionKind.ElementAppeared => WithActual(
                condition,
                after.InteractiveElements.ElementsCount > before.InteractiveElements.ElementsCount,
                after.InteractiveElements.ElementsCount.ToString()),
            BrowserActionPostconditionKind.ElementDisappeared => WithActual(
                condition,
                after.InteractiveElements.ElementsCount < before.InteractiveElements.ElementsCount,
                after.InteractiveElements.ElementsCount.ToString()),
            BrowserActionPostconditionKind.InputValueChanged => WithActual(
                condition,
                before.PageTextPreviewRedacted != after.PageTextPreviewRedacted,
                "input-metadata-redacted"),
            BrowserActionPostconditionKind.NetworkSettled => WithActual(
                condition,
                after.Lifecycle.NetworkIdle && after.Network.CriticalFailureCount == 0,
                $"networkIdle={after.Lifecycle.NetworkIdle};criticalNetwork={after.Network.CriticalFailureCount}"),
            BrowserActionPostconditionKind.NoCriticalConsoleError => WithActual(
                condition,
                after.Console.CriticalErrorCount == 0,
                after.Console.CriticalErrorCount.ToString()),
            BrowserActionPostconditionKind.ExpectedStateObserved => WithActual(
                condition,
                after.PageTextPreviewRedacted.Contains(condition.Expected, StringComparison.OrdinalIgnoreCase),
                after.PageTextPreviewRedacted),
            _ => condition with { Satisfied = false, Reason = "Unsupported postcondition kind." }
        };
    }

    private static BrowserActionPostcondition WithActual(
        BrowserActionPostcondition condition,
        bool satisfied,
        string actual) =>
        condition with
        {
            Actual = actual,
            Satisfied = satisfied,
            Reason = satisfied ? "Postcondition matched fixture snapshots." : "Postcondition did not match fixture snapshots."
        };

    private static bool IsFixtureSafe(BrowserPerceptionSnapshot snapshot) =>
        string.Equals(snapshot.Source, "fixture-safe-read-only", StringComparison.Ordinal);
}
