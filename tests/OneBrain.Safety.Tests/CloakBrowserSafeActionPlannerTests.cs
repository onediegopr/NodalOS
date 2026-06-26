using OneBrain.BrowserPerception;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("CloakBrowserPerceptionRouter")]
public sealed class CloakBrowserSafeActionPlannerTests
{
    [TestMethod]
    public void SafeActionPlanner_CaptchaMarker_ReturnsHumanHandoffOnly()
    {
        var plans = Plan(new BrowserPerceptionFixture(
            FixtureId: "safe-captcha",
            Url: "https://example.test/captcha",
            Title: "Captcha",
            HumanVerificationDetected: true));

        AssertHumanHandoffOnly(plans);
    }

    [TestMethod]
    public void SafeActionPlanner_TwoFactorMarker_ReturnsHumanHandoffOnly()
    {
        var plans = Plan(new BrowserPerceptionFixture(
            FixtureId: "safe-2fa",
            Url: "https://example.test/2fa",
            Title: "2FA",
            TwoFactorDetected: true));

        AssertHumanHandoffOnly(plans);
    }

    [TestMethod]
    public void SafeActionPlanner_AntiBotMarker_ReturnsHumanHandoffOnly()
    {
        var plans = Plan(new BrowserPerceptionFixture(
            FixtureId: "safe-antibot",
            Url: "https://example.test/anti-bot",
            Title: "Anti Bot",
            AntiBotDetected: true));

        AssertHumanHandoffOnly(plans);
    }

    [TestMethod]
    public void SafeActionPlanner_LoginAuthRequired_ReturnsHumanHandoffOnly()
    {
        var plans = Plan(new BrowserPerceptionFixture(
            FixtureId: "safe-login",
            Url: "https://example.test/login",
            Title: "Login",
            LoginFormDetected: true,
            FormsCount: 1,
            InputsCount: 2));

        AssertHumanHandoffOnly(plans);
    }

    [TestMethod]
    public void SafeActionPlanner_LegacyForm_ReturnsTheoreticalTypeAndClick()
    {
        var plans = Plan(new BrowserPerceptionFixture(
            FixtureId: "safe-form",
            Url: "https://example.test/form",
            Title: "Form",
            FormsCount: 1,
            InputsCount: 2,
            ButtonsCount: 1));

        CollectionAssert.Contains(plans.Select(plan => plan.ActionKind).ToList(), SafeBrowserActionKind.Type);
        CollectionAssert.Contains(plans.Select(plan => plan.ActionKind).ToList(), SafeBrowserActionKind.Click);
        Assert.IsTrue(plans.All(plan => plan.LocatorStrategy.Strategy == LocatorStrategyKind.Css));
        Assert.IsTrue(plans.All(plan => plan.ProhibitedOnExternalPages));
        Assert.IsTrue(plans.All(plan => plan.CanExecuteInFixtureOnly));
        Assert.IsTrue(plans.All(plan => plan.PlanOnly && !plan.ExecutesAction));
    }

    [TestMethod]
    public void SafeActionPlanner_SpaFixture_ReturnsAccessibilityLocatorPlans()
    {
        var plans = Plan(new BrowserPerceptionFixture(
            FixtureId: "safe-spa",
            Url: "https://example.test/app",
            Title: "SPA",
            IsSpa: true,
            SemanticControlCount: 3,
            ButtonsCount: 2));

        Assert.IsTrue(plans.Count >= 1);
        Assert.IsTrue(plans.All(plan => plan.LocatorStrategy.Strategy == LocatorStrategyKind.Accessibility));
        Assert.IsTrue(plans.All(plan => plan.TargetLocator?.Type == ElementLocatorType.Accessibility));
    }

    [TestMethod]
    public void SafeActionPlanner_LowConfidenceProfile_ReturnsHumanHandoff()
    {
        var plans = Plan(new BrowserPerceptionFixture(
            FixtureId: "safe-unknown",
            Url: "https://example.test/unknown",
            Title: "Unknown"));

        AssertHumanHandoffOnly(plans);
    }

    [TestMethod]
    public void SafeActionPlanner_FinancialIdentityAndAuthObjectives_ReturnHumanHandoff()
    {
        var blockedObjectives = new[]
        {
            "enter credit card",
            "fill card number",
            "provide ssn",
            "use social security number",
            "answer secret question",
            "authenticate account",
            "signin now",
            "sign in",
            "verify account",
            "complete verification"
        };

        foreach (var objective in blockedObjectives)
        {
            var plans = Plan(new BrowserPerceptionFixture(
                FixtureId: "safe-sensitive-objective",
                Url: "https://example.test/form",
                Title: "Form",
                FormsCount: 1,
                InputsCount: 1,
                ButtonsCount: 1), objective);

            AssertHumanHandoffOnly(plans);
        }
    }

    [TestMethod]
    public void BrowserActionVerifier_PreconditionsSatisfied_CanProceed()
    {
        var snapshot = Snapshot(new BrowserPerceptionFixture(
            FixtureId: "verify-pre-ok",
            Url: "https://example.test/form",
            Title: "Form",
            FormsCount: 1,
            InputsCount: 1,
            ButtonsCount: 1));
        var plan = Plan(snapshot, "fill fixture form").Single(plan => plan.ActionKind == SafeBrowserActionKind.Type);

        var result = new BrowserActionVerifier().VerifyPreconditions(plan, snapshot);

        Assert.IsTrue(result.CanProceed);
        Assert.AreEqual(0, result.FailedPreconditions.Count);
        Assert.IsFalse(result.RequiresHumanHandoff);
    }

    [TestMethod]
    public void BrowserActionVerifier_TargetLocatorMissing_CannotProceed()
    {
        var snapshot = Snapshot(new BrowserPerceptionFixture(
            FixtureId: "verify-missing-target",
            Url: "https://example.test/form",
            Title: "Form",
            FormsCount: 1,
            InputsCount: 1,
            ButtonsCount: 1));
        var plan = new SafeBrowserActionPlan(
            SafeBrowserActionKind.Click,
            TargetLocator: null,
            new LocatorStrategy(LocatorStrategyKind.Css, 0.8, "test", [PerceptionSignalKind.DOM], HumanHandoffRequired: false),
            Confidence: 0.8,
            Reason: "missing target test",
            Preconditions: [],
            ExpectedPostconditions: [],
            RequiresHumanApproval: true,
            RequiresHumanHandoff: false,
            CanExecuteInFixtureOnly: true,
            ProhibitedOnExternalPages: true,
            EvidencePlan: new SafeBrowserActionEvidencePlan(snapshot.SnapshotId, "pending", true, true, true));

        var result = new BrowserActionVerifier().VerifyPreconditions(plan, snapshot);

        Assert.IsFalse(result.CanProceed);
        Assert.IsTrue(result.FailedPreconditions.Any(precondition => precondition.Kind == BrowserActionPreconditionKind.TargetLocatorPresent));
    }

    [TestMethod]
    public void BrowserActionVerifier_HumanHandoffBlockage_CannotProceed()
    {
        var snapshot = Snapshot(new BrowserPerceptionFixture(
            FixtureId: "verify-human",
            Url: "https://example.test/captcha",
            Title: "Captcha",
            HumanVerificationDetected: true));
        var plan = Plan(snapshot, "inspect challenge").Single();

        var result = new BrowserActionVerifier().VerifyPreconditions(plan, snapshot);

        Assert.IsFalse(result.CanProceed);
        Assert.IsTrue(result.RequiresHumanHandoff);
    }

    [TestMethod]
    public void BrowserActionVerifier_PostconditionMatchingExpectedState_Succeeds()
    {
        var before = Snapshot(new BrowserPerceptionFixture(
            FixtureId: "verify-post-before",
            Url: "https://example.test/app",
            Title: "Before",
            IsSpa: true,
            SemanticControlCount: 2,
            TextPreview: "ready"));
        var after = Snapshot(new BrowserPerceptionFixture(
            FixtureId: "verify-post-after",
            Url: "https://example.test/app",
            Title: "After",
            IsSpa: true,
            SemanticControlCount: 2,
            TextPreview: "ready clicked"));
        var plan = Plan(before, "click fixture action").First(plan => plan.ActionKind == SafeBrowserActionKind.Click);

        var result = new BrowserActionVerifier().VerifyPostconditions(plan, before, after);

        Assert.IsTrue(result.ActionSucceeded);
        Assert.AreEqual(0, result.FailedPostconditions.Count);
    }

    [TestMethod]
    public void BrowserActionVerifier_PostconditionMismatch_Fails()
    {
        var before = Snapshot(new BrowserPerceptionFixture(
            FixtureId: "verify-post-mismatch-before",
            Url: "https://example.test/app",
            Title: "Before",
            IsSpa: true,
            SemanticControlCount: 2,
            TextPreview: "ready"));
        var after = Snapshot(new BrowserPerceptionFixture(
            FixtureId: "verify-post-mismatch-after",
            Url: "https://example.test/app",
            Title: "After",
            IsSpa: true,
            SemanticControlCount: 2,
            TextPreview: "unchanged"));
        var plan = Plan(before, "click fixture action").First(plan => plan.ActionKind == SafeBrowserActionKind.Click);

        var result = new BrowserActionVerifier().VerifyPostconditions(plan, before, after);

        Assert.IsFalse(result.ActionSucceeded);
        Assert.IsTrue(result.FailedPostconditions.Count > 0);
    }

    [TestMethod]
    public void SafeActionPlanner_AllPlansAreProhibitedOnExternalPages()
    {
        var plans = Plan(new BrowserPerceptionFixture(
            FixtureId: "safe-external",
            Url: "https://example.test/form",
            Title: "Form",
            FormsCount: 1,
            InputsCount: 1,
            ButtonsCount: 1));

        Assert.IsTrue(plans.All(plan => plan.ProhibitedOnExternalPages));
    }

    [TestMethod]
    public void SafeActionPlanner_DoesNotExecuteActionsOrCreateSideEffects()
    {
        var plans = Plan(new BrowserPerceptionFixture(
            FixtureId: "safe-side-effects",
            Url: "https://example.test/form",
            Title: "Form",
            FormsCount: 1,
            InputsCount: 1,
            ButtonsCount: 1));

        Assert.IsTrue(plans.All(plan => plan.PlanOnly));
        Assert.IsTrue(plans.All(plan => !plan.ExecutesAction));
        Assert.IsTrue(plans.All(plan => plan.EvidencePlan.MetadataOnly));
        Assert.IsTrue(plans.All(plan => plan.EvidencePlan.NoSensitivePayloadGuarantee));
    }

    private static IReadOnlyList<SafeBrowserActionPlan> Plan(BrowserPerceptionFixture fixture, string objective = "use fixture") =>
        Plan(Snapshot(fixture), objective);

    private static IReadOnlyList<SafeBrowserActionPlan> Plan(BrowserPerceptionSnapshot snapshot, string objective)
    {
        var profile = new PageCapabilityClassifier().BuildTechnologyProfile(snapshot);
        return new SafeActionPlanner().PlanActions(profile, snapshot, objective);
    }

    private static BrowserPerceptionSnapshot Snapshot(BrowserPerceptionFixture fixture) =>
        new BrowserPerceptionSnapshotBuilder().FromFixture(fixture);

    private static void AssertHumanHandoffOnly(IReadOnlyList<SafeBrowserActionPlan> plans)
    {
        Assert.AreEqual(1, plans.Count);
        Assert.AreEqual(SafeBrowserActionKind.HumanHandoff, plans[0].ActionKind);
        Assert.IsTrue(plans[0].RequiresHumanHandoff);
        Assert.IsTrue(plans[0].RequiresHumanApproval);
        Assert.IsTrue(plans[0].ProhibitedOnExternalPages);
        Assert.IsTrue(plans[0].PlanOnly);
        Assert.IsFalse(plans[0].ExecutesAction);
    }
}
