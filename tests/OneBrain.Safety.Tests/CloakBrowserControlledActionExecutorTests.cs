using OneBrain.BrowserPerception;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("CloakBrowserPerceptionRouter")]
public sealed class CloakBrowserControlledActionExecutorTests
{
    [TestMethod]
    public void ControlledActionExecutor_ClickFixtureWithPassingPreconditions_Succeeds()
    {
        var snapshot = FormSnapshot("executor-click");
        var plan = Plan(snapshot, "click fixture").Single(candidate => candidate.ActionKind == SafeBrowserActionKind.Click);
        var state = new FixturePageState();

        var result = Execute(plan, snapshot, state);

        Assert.IsTrue(result.Attempted);
        Assert.IsTrue(result.Succeeded);
        CollectionAssert.Contains(state.ClickedElementRefs.ToList(), plan.TargetLocator!.ElementRef);
        AssertNoLiveInvocation(result);
    }

    [TestMethod]
    public void ControlledActionExecutor_TypeFixtureWithPassingPreconditions_RecordsValue()
    {
        var snapshot = FormSnapshot("executor-type");
        var plan = Plan(snapshot, "type fixture").Single(candidate => candidate.ActionKind == SafeBrowserActionKind.Type);
        var state = new FixturePageState();

        var result = Execute(plan, snapshot, state, inputValue: "fixture safe text");

        Assert.IsTrue(result.Attempted);
        Assert.IsTrue(result.Succeeded);
        Assert.AreEqual("fixture safe text", state.TypedValues[plan.TargetLocator!.ElementRef]);
        AssertNoLiveInvocation(result);
    }

    [TestMethod]
    public void ControlledActionExecutor_ScrollFixture_UpdatesScrollPosition()
    {
        var snapshot = FormSnapshot("executor-scroll");
        var plan = CustomPlan(SafeBrowserActionKind.Scroll, snapshot, [Expected(BrowserActionPostconditionKind.ExpectedStateObserved, "scrolled")]);
        var state = new FixturePageState();

        var result = Execute(plan, snapshot, state, scrollDelta: 250);

        Assert.IsTrue(result.Succeeded);
        Assert.AreEqual(250, state.ScrollPosition);
        AssertNoLiveInvocation(result);
    }

    [TestMethod]
    public void ControlledActionExecutor_SelectFixture_RecordsSelectedValue()
    {
        var snapshot = FormSnapshot("executor-select");
        var plan = CustomPlan(SafeBrowserActionKind.Select, snapshot, [Expected(BrowserActionPostconditionKind.ExpectedStateObserved, "selected metadata changed")]);
        var state = new FixturePageState();

        var result = Execute(plan, snapshot, state, selectValue: "fixture-option-a");

        Assert.IsTrue(result.Succeeded);
        Assert.AreEqual("fixture-option-a", state.SelectedValues[plan.TargetLocator!.ElementRef]);
        AssertNoLiveInvocation(result);
    }

    [TestMethod]
    public void ControlledActionExecutor_WaitFixture_IncrementsWaitTicks()
    {
        var snapshot = FormSnapshot("executor-wait");
        var plan = CustomPlan(SafeBrowserActionKind.Wait, snapshot, [Expected(BrowserActionPostconditionKind.ExpectedStateObserved, "stable")]);
        var state = new FixturePageState();

        var result = Execute(plan, snapshot, state, waitTicks: 3);

        Assert.IsTrue(result.Succeeded);
        Assert.AreEqual(3, state.WaitTicks);
        AssertNoLiveInvocation(result);
    }

    [TestMethod]
    public void ControlledActionExecutor_PreconditionFailure_AbortsWithoutSideEffects()
    {
        var snapshot = FormSnapshot("executor-precondition-fail");
        var plan = CustomPlan(
            SafeBrowserActionKind.Click,
            snapshot,
            [Expected(BrowserActionPostconditionKind.ExpectedStateObserved, "clicked")],
            preconditionsSatisfied: false);
        var state = new FixturePageState();

        var result = Execute(plan, snapshot, state);

        Assert.IsFalse(result.Attempted);
        Assert.IsFalse(result.Succeeded);
        Assert.IsTrue(result.AbortedByPrecondition);
        Assert.AreEqual(0, state.ClickedElementRefs.Count);
        AssertNoLiveInvocation(result);
    }

    [TestMethod]
    public void ControlledActionExecutor_PostconditionFailure_ReturnsFailedPostcondition()
    {
        var snapshot = FormSnapshot("executor-postcondition-fail");
        var plan = CustomPlan(SafeBrowserActionKind.Scroll, snapshot, [Expected(BrowserActionPostconditionKind.ElementAppeared, "new element")]);
        var state = new FixturePageState();

        var result = Execute(plan, snapshot, state);

        Assert.IsTrue(result.Attempted);
        Assert.IsFalse(result.Succeeded);
        Assert.IsTrue(result.FailedPostcondition);
        Assert.IsTrue(result.PostVerification.FailedPostconditions.Count > 0);
        AssertNoLiveInvocation(result);
    }

    [TestMethod]
    public void ControlledActionExecutor_HumanHandoffBlockagePlans_DoNotExecute()
    {
        var fixtures = new[]
        {
            new BrowserPerceptionFixture("executor-captcha", "https://example.test/captcha", "Captcha", HumanVerificationDetected: true),
            new BrowserPerceptionFixture("executor-2fa", "https://example.test/2fa", "2FA", TwoFactorDetected: true),
            new BrowserPerceptionFixture("executor-antibot", "https://example.test/anti-bot", "Anti Bot", AntiBotDetected: true),
            new BrowserPerceptionFixture("executor-login", "https://example.test/login", "Login", LoginFormDetected: true, FormsCount: 1, InputsCount: 1)
        };

        foreach (var fixture in fixtures)
        {
            var snapshot = Snapshot(fixture);
            var plan = Plan(snapshot, "inspect fixture").Single();
            var state = new FixturePageState();

            var result = Execute(plan, snapshot, state);

            Assert.IsFalse(result.Attempted);
            Assert.IsTrue(result.RequiresHumanHandoff);
            Assert.AreEqual(0, state.SyntheticDomChangeMarkers.Count);
            AssertNoLiveInvocation(result);
        }
    }

    [TestMethod]
    public void ControlledActionExecutor_LowConfidenceHumanHandoffPlan_DoesNotExecute()
    {
        var snapshot = Snapshot(new BrowserPerceptionFixture("executor-low-confidence", "https://example.test/unknown", "Unknown"));
        var plan = Plan(snapshot, "unknown fixture").Single();
        var state = new FixturePageState();

        var result = Execute(plan, snapshot, state);

        Assert.IsFalse(result.Attempted);
        Assert.IsTrue(result.RequiresHumanHandoff);
        Assert.AreEqual(0, state.SyntheticDomChangeMarkers.Count);
        AssertNoLiveInvocation(result);
    }

    [TestMethod]
    public void ControlledActionExecutor_LiveDisabledMode_Aborts()
    {
        var snapshot = FormSnapshot("executor-live-disabled");
        var plan = Plan(snapshot, "click fixture").Single(candidate => candidate.ActionKind == SafeBrowserActionKind.Click);
        var state = new FixturePageState();

        var result = Execute(plan, snapshot, state, mode: ControlledActionExecutionMode.LiveDisabled);

        Assert.IsFalse(result.Attempted);
        Assert.IsTrue(result.AbortedByPrecondition);
        Assert.AreEqual(BrowserActionPreconditionKind.LiveExecutionDisabled, result.PreVerification.FailedPreconditions.Single().Kind);
        AssertNoLiveInvocation(result);
    }

    [TestMethod]
    public void ControlledActionExecutor_MissingFixturePageState_AbortsByExternalPageGuard()
    {
        var snapshot = FormSnapshot("executor-missing-state");
        var plan = Plan(snapshot, "click fixture").Single(candidate => candidate.ActionKind == SafeBrowserActionKind.Click);

        var result = Execute(plan, snapshot, fixtureState: null);

        Assert.IsFalse(result.Attempted);
        Assert.IsTrue(result.AbortedByPrecondition);
        Assert.AreEqual(BrowserActionPreconditionKind.FixtureOrControlledPageOnly, result.PreVerification.FailedPreconditions.Single().Kind);
        AssertNoLiveInvocation(result);
    }

    [TestMethod]
    public void ControlledActionExecutor_SensitiveInput_AbortsAndRequiresHumanHandoff()
    {
        var sensitiveValues = new[]
        {
            "password fixture value",
            "token fixture value",
            "API key fixture value",
            "OTP fixture value",
            "cvv fixture value",
            "ssn fixture value",
            "pin fixture value",
            "secret answer fixture value",
            "sk-test_secret_1234567890",
            "ghp_fakeSecretToken123456789",
            "eyJhbGciOiJIUzI1NiJ9.eyJzdWIiOiIxMjMifQ.signature",
            "Bearer abcdefghijklmnopqrstuvwxyz123456",
            "4111 1111 1111 1111",
            "123-45-6789",
            "abcdefghijklmnopqrstuvwxyz1234567890"
        };
        var snapshot = FormSnapshot("executor-sensitive-input");
        var plan = Plan(snapshot, "type fixture").Single(candidate => candidate.ActionKind == SafeBrowserActionKind.Type);

        foreach (var value in sensitiveValues)
        {
            var state = new FixturePageState();
            var result = Execute(plan, snapshot, state, inputValue: value);

            Assert.IsFalse(result.Attempted);
            Assert.IsTrue(result.RequiresHumanHandoff);
            Assert.AreEqual(0, state.TypedValues.Count);
            Assert.AreEqual(BrowserActionPreconditionKind.SensitiveInputSafe, result.PreVerification.FailedPreconditions.Single().Kind);
            AssertNoLiveInvocation(result);
        }
    }

    [TestMethod]
    public void ControlledActionExecutor_LiveSourceSnapshot_AbortsDespiteFixtureState()
    {
        var snapshot = FormSnapshot("executor-live-source") with { Source = "live" };
        var plan = Plan(FormSnapshot("executor-live-source-plan"), "click fixture").Single(candidate => candidate.ActionKind == SafeBrowserActionKind.Click);
        var state = new FixturePageState();

        var result = Execute(plan, snapshot, state);

        Assert.IsFalse(result.Attempted);
        Assert.IsFalse(result.Succeeded);
        Assert.IsTrue(result.AbortedByPrecondition);
        Assert.AreEqual(BrowserActionPreconditionKind.FixtureOrControlledPageOnly, result.PreVerification.FailedPreconditions.Single().Kind);
        Assert.AreEqual(0, state.SyntheticDomChangeMarkers.Count);
        AssertNoLiveInvocation(result);
    }

    [TestMethod]
    public void ControlledActionExecutor_UnsupportedAction_Aborts()
    {
        var snapshot = FormSnapshot("executor-unsupported");
        var plan = CustomPlan((SafeBrowserActionKind)999, snapshot, []);
        var state = new FixturePageState();

        var result = Execute(plan, snapshot, state);

        Assert.IsFalse(result.Attempted);
        Assert.IsFalse(result.Succeeded);
        Assert.AreEqual(BrowserActionPreconditionKind.SupportedAction, result.PreVerification.FailedPreconditions.Single().Kind);
        AssertNoLiveInvocation(result);
    }

    [TestMethod]
    public void ControlledActionExecutor_NoCdpWebSocketBrowserOrExtensionInvocationFlagsRemainFalse()
    {
        var snapshot = FormSnapshot("executor-no-live-flags");
        var plan = Plan(snapshot, "click fixture").Single(candidate => candidate.ActionKind == SafeBrowserActionKind.Click);
        var state = new FixturePageState();

        var result = Execute(plan, snapshot, state);

        AssertNoLiveInvocation(result);
        Assert.IsFalse(result.EvidenceDraft.ExternalNavigationAttempted);
        Assert.IsFalse(result.EvidenceDraft.ProductFilesModified);
        Assert.IsTrue(result.EvidenceDraft.MetadataOnly);
        Assert.IsTrue(result.EvidenceDraft.LiveExecutionDisabled);
    }

    private static ControlledActionExecutionResult Execute(
        SafeBrowserActionPlan plan,
        BrowserPerceptionSnapshot snapshot,
        FixturePageState? fixtureState,
        ControlledActionExecutionMode mode = ControlledActionExecutionMode.FixtureOnly,
        string? inputValue = null,
        string? selectValue = null,
        int scrollDelta = 100,
        int waitTicks = 1)
    {
        var request = new ControlledActionExecutionRequest(
            plan,
            snapshot,
            mode,
            fixtureState,
            RequestedAt: DateTimeOffset.UtcNow,
            CorrelationId: "executor-test")
        {
            FixtureInputValue = inputValue,
            FixtureSelectValue = selectValue,
            FixtureScrollDelta = scrollDelta,
            FixtureWaitTicks = waitTicks
        };

        return new ControlledActionExecutor().Execute(request);
    }

    private static IReadOnlyList<SafeBrowserActionPlan> Plan(BrowserPerceptionSnapshot snapshot, string objective)
    {
        var profile = new PageCapabilityClassifier().BuildTechnologyProfile(snapshot);
        return new SafeActionPlanner().PlanActions(profile, snapshot, objective);
    }

    private static SafeBrowserActionPlan CustomPlan(
        SafeBrowserActionKind actionKind,
        BrowserPerceptionSnapshot snapshot,
        IReadOnlyList<BrowserActionPostcondition> expectedPostconditions,
        bool preconditionsSatisfied = true,
        double confidence = 0.8)
    {
        var locatorStrategy = new LocatorStrategy(
            LocatorStrategyKind.Css,
            0.8,
            "fixture custom locator strategy",
            [PerceptionSignalKind.DOM],
            HumanHandoffRequired: false);
        var locator = new ElementLocator(
            ElementLocatorType.Css,
            "#fixture-target",
            0.8,
            "fixture target",
            "fixture:target");

        return new SafeBrowserActionPlan(
            actionKind,
            locator,
            locatorStrategy,
            confidence,
            "custom fixture action plan",
            Preconditions:
            [
                new BrowserActionPrecondition(
                    BrowserActionPreconditionKind.FixtureOrControlledPageOnly,
                    "fixture-safe-read-only",
                    snapshot.Source,
                    preconditionsSatisfied,
                    preconditionsSatisfied ? "Fixture precondition satisfied." : "Fixture precondition failed.")
            ],
            expectedPostconditions,
            RequiresHumanApproval: true,
            RequiresHumanHandoff: false,
            CanExecuteInFixtureOnly: true,
            ProhibitedOnExternalPages: true,
            EvidencePlan: new SafeBrowserActionEvidencePlan(snapshot.SnapshotId, "pending:post-action-snapshot", true, true, true));
    }

    private static BrowserActionPostcondition Expected(
        BrowserActionPostconditionKind kind,
        string expected) =>
        new(kind, expected, Actual: "not-evaluated", Satisfied: false, Reason: "Expected fixture postcondition.");

    private static BrowserPerceptionSnapshot FormSnapshot(string fixtureId) =>
        Snapshot(new BrowserPerceptionFixture(
            fixtureId,
            "https://example.test/form",
            "Fixture Form",
            FormsCount: 1,
            InputsCount: 1,
            ButtonsCount: 1,
            TextPreview: "ready"));

    private static BrowserPerceptionSnapshot Snapshot(BrowserPerceptionFixture fixture) =>
        new BrowserPerceptionSnapshotBuilder().FromFixture(fixture);

    private static void AssertNoLiveInvocation(ControlledActionExecutionResult result)
    {
        Assert.IsFalse(result.EvidenceDraft.CdpInvoked);
        Assert.IsFalse(result.EvidenceDraft.WebSocketInvoked);
        Assert.IsFalse(result.EvidenceDraft.BrowserLaunched);
        Assert.IsFalse(result.EvidenceDraft.SystemBrowserUsed);
        Assert.IsFalse(result.EvidenceDraft.ExtensionInvoked);
    }
}
