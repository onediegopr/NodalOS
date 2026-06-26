using System.Text.Json;
using OneBrain.BrowserPerception;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("CloakBrowserPerceptionRouter")]
public sealed class CloakBrowserEvidencePackTests
{
    [TestMethod]
    public void BrowserEvidenceCollector_PlanOnly_RecordsPlanWithoutExecution()
    {
        var snapshot = FormSnapshot("evidence-plan-only");
        var profile = new PageCapabilityClassifier().Classify(snapshot);
        var decision = new StrategyRouter().Route(profile, snapshot);
        var plan = Plan(snapshot).Single(candidate => candidate.ActionKind == SafeBrowserActionKind.Click);

        var pack = new BrowserEvidenceCollector().CollectFromPlanOnly(decision, plan, snapshot);

        Assert.AreEqual(BrowserEvidenceKind.PlanOnly, pack.EvidenceKind);
        Assert.IsNull(pack.ActionSucceeded);
        Assert.IsNull(pack.SnapshotAfter);
        AssertNoLiveInvocation(pack);
    }

    [TestMethod]
    public void BrowserEvidenceCollector_FixtureExecutionSucceeded_RecordsAfterSnapshot()
    {
        var snapshot = FormSnapshot("evidence-execution-success");
        var plan = Plan(snapshot).Single(candidate => candidate.ActionKind == SafeBrowserActionKind.Click);
        var state = new FixturePageState();
        var result = Execute(plan, snapshot, state);
        var after = AfterSnapshot(snapshot, result);

        var pack = new BrowserEvidenceCollector().CollectFromExecution(result, snapshot, after);

        Assert.AreEqual(BrowserEvidenceKind.FixtureExecutionSucceeded, pack.EvidenceKind);
        Assert.IsTrue(pack.ActionSucceeded);
        Assert.IsNotNull(pack.SnapshotAfter);
        Assert.AreEqual(BrowserEvidenceRedactionStatus.None, pack.RedactionStatus);
        AssertNoLiveInvocation(pack);
    }

    [TestMethod]
    public void BrowserEvidenceCollector_FixtureExecutionFailedByPostcondition_RecordsFailure()
    {
        var snapshot = FormSnapshot("evidence-post-fail");
        var plan = CustomPlan(SafeBrowserActionKind.Scroll, snapshot, [Expected(BrowserActionPostconditionKind.ElementAppeared, "new element")]);
        var state = new FixturePageState();
        var result = Execute(plan, snapshot, state);
        var after = AfterSnapshot(snapshot, result);

        var pack = new BrowserEvidenceCollector().CollectFromExecution(result, snapshot, after);

        Assert.AreEqual(BrowserEvidenceKind.VerificationFailed, pack.EvidenceKind);
        Assert.IsFalse(pack.ActionSucceeded);
        Assert.IsTrue(pack.EvidenceSummary.Contains("succeeded=False", StringComparison.OrdinalIgnoreCase));
        AssertNoLiveInvocation(pack);
    }

    [TestMethod]
    public void BrowserEvidenceCollector_CaptchaBlockage_RecordsHumanHandoff()
    {
        var snapshot = Snapshot(new BrowserPerceptionFixture("evidence-captcha", "https://example.test/captcha", "Captcha", HumanVerificationDetected: true));
        var blockage = new BlockageDetector().DetectBlockages(snapshot).Single(candidate => candidate.BlockageKind == BlockageKind.Captcha);

        var pack = new BrowserEvidenceCollector().CollectFromBlockage(blockage, snapshot);

        Assert.AreEqual(BrowserEvidenceKind.BlockageDetected, pack.EvidenceKind);
        Assert.IsTrue(pack.HumanHandoffTriggered);
        Assert.AreEqual(BlockageKind.Captcha.ToString(), pack.BlockageReport);
        AssertNoLiveInvocation(pack);
    }

    [TestMethod]
    public void BrowserEvidenceCollector_TwoFactorAntiBotAndLoginBlockages_RecordHumanHandoff()
    {
        var fixtures = new[]
        {
            new BrowserPerceptionFixture("evidence-2fa", "https://example.test/2fa", "2FA", TwoFactorDetected: true),
            new BrowserPerceptionFixture("evidence-antibot", "https://example.test/anti-bot", "Anti Bot", AntiBotDetected: true),
            new BrowserPerceptionFixture("evidence-login", "https://example.test/login", "Login", LoginFormDetected: true, FormsCount: 1, InputsCount: 1)
        };

        foreach (var fixture in fixtures)
        {
            var snapshot = Snapshot(fixture);
            var blockage = new BlockageDetector().DetectBlockages(snapshot).Single(candidate => candidate.RequiresHumanHandoff);

            var pack = new BrowserEvidenceCollector().CollectFromBlockage(blockage, snapshot);

            Assert.IsTrue(pack.HumanHandoffTriggered);
            AssertNoLiveInvocation(pack);
        }
    }

    [TestMethod]
    public void BrowserEvidenceCollector_HumanHandoff_RecordsDecisionAndBlockage()
    {
        var snapshot = Snapshot(new BrowserPerceptionFixture("evidence-handoff", "https://example.test/captcha", "Captcha", HumanVerificationDetected: true));
        var profile = new PageCapabilityClassifier().Classify(snapshot);
        var decision = new StrategyRouter().Route(profile, snapshot);
        var blockage = decision.Blockages.Single(candidate => candidate.RequiresHumanHandoff);

        var pack = new BrowserEvidenceCollector().CollectFromHumanHandoff(decision, blockage, snapshot);

        Assert.AreEqual(BrowserEvidenceKind.HumanHandoff, pack.EvidenceKind);
        Assert.IsTrue(pack.HumanHandoffTriggered);
        Assert.AreEqual(decision.Strategy.ToString(), pack.StrategyDecision);
        AssertNoLiveInvocation(pack);
    }

    [TestMethod]
    public void BrowserEvidenceCollector_VerificationFailure_RecordsFailedVerification()
    {
        var snapshot = FormSnapshot("evidence-verification-failure");
        var plan = CustomPlan(SafeBrowserActionKind.Click, snapshot, [], preconditionsSatisfied: false);
        var pre = new BrowserActionVerifier().VerifyPreconditions(plan, snapshot);

        var pack = new BrowserEvidenceCollector().CollectFromVerificationFailure(plan, pre, null, snapshot);

        Assert.AreEqual(BrowserEvidenceKind.VerificationFailed, pack.EvidenceKind);
        Assert.IsFalse(pack.ActionSucceeded);
        Assert.IsTrue(pack.EvidenceSummary.Contains("Verification failed", StringComparison.OrdinalIgnoreCase));
        AssertNoLiveInvocation(pack);
    }

    [TestMethod]
    public void BrowserEvidenceRedactor_FieldNamePassword_RedactsEvenInnocentValue()
    {
        var snapshot = FormSnapshot("evidence-password-redaction");

        var pack = new BrowserEvidenceCollector().CollectFromSensitiveField("password", "fixture innocent value", snapshot);
        var json = JsonSerializer.Serialize(pack);

        Assert.AreEqual(BrowserEvidenceRedactionStatus.Partial, pack.RedactionStatus);
        CollectionAssert.Contains(pack.SensitiveFieldsRedacted.ToList(), "password");
        Assert.IsFalse(json.Contains("fixture innocent value", StringComparison.Ordinal));
        Assert.IsTrue(json.Contains(BrowserEvidenceRedactor.RedactedValue, StringComparison.Ordinal));
    }

    [TestMethod]
    public void BrowserEvidenceRedactor_TokenApiKeyAndSecretPatterns_DoNotAppearInSerializedJson()
    {
        var snapshot = FormSnapshot("evidence-token-redaction");
        var secretValues = new[]
        {
            ("access_token", "sk-abcdefghijklmnopqrstuvwxyz123456"),
            ("api_key", "ghp_abcdefghijklmnopqrstuvwxyz123456"),
            ("authorization", "Bearer abcdefghijklmnopqrstuvwxyz0123456789"),
            ("session", "eyJhbGciOiJIUzI1NiJ9.eyJzdWIiOiIxMjMifQ.signature")
        };

        foreach (var (fieldName, value) in secretValues)
        {
            var pack = new BrowserEvidenceCollector().CollectFromSensitiveField(fieldName, value, snapshot);
            var json = JsonSerializer.Serialize(pack);

            Assert.AreEqual(BrowserEvidenceRedactionStatus.Partial, pack.RedactionStatus);
            Assert.IsFalse(json.Contains(value, StringComparison.Ordinal));
            Assert.IsTrue(pack.SensitiveFieldsRedacted.Any(field => field.Contains(fieldName, StringComparison.OrdinalIgnoreCase)));
        }
    }

    [TestMethod]
    public void BrowserEvidenceRedactor_OtpContext_RedactsValue()
    {
        var snapshot = FormSnapshot("evidence-otp-redaction");

        var pack = new BrowserEvidenceCollector().CollectFromSensitiveField("otp", "123456", snapshot);
        var json = JsonSerializer.Serialize(pack);

        Assert.AreEqual(BrowserEvidenceRedactionStatus.Partial, pack.RedactionStatus);
        Assert.IsFalse(json.Contains("123456", StringComparison.Ordinal));
        CollectionAssert.Contains(pack.SensitiveFieldsRedacted.ToList(), "otp");
    }

    [TestMethod]
    public void BrowserEvidenceCollector_NoSensitiveInput_RedactionStatusNone()
    {
        var snapshot = FormSnapshot("evidence-no-sensitive");

        var pack = new BrowserEvidenceCollector().CollectFromSensitiveField("label", "plain fixture text", snapshot);

        Assert.AreEqual(BrowserEvidenceRedactionStatus.None, pack.RedactionStatus);
        Assert.AreEqual("Sensitive field label: plain fixture text", pack.EvidenceSummary);
        Assert.AreEqual(0, pack.SensitiveFieldsRedacted.Count);
    }

    [TestMethod]
    public void BrowserEvidencePack_SerializationRoundTrip_DoesNotReintroduceSecrets()
    {
        var snapshot = FormSnapshot("evidence-roundtrip");
        var pack = new BrowserEvidenceCollector().CollectFromSensitiveField("client_secret", "sk-abcdefghijklmnopqrstuvwxyz123456", snapshot);
        var json = JsonSerializer.Serialize(pack);
        var roundTrip = JsonSerializer.Deserialize<BrowserEvidencePack>(json);
        var jsonAgain = JsonSerializer.Serialize(roundTrip);

        Assert.IsNotNull(roundTrip);
        Assert.AreEqual(pack.EvidenceKind, roundTrip.EvidenceKind);
        Assert.AreEqual(pack.RedactionStatus, roundTrip.RedactionStatus);
        Assert.IsFalse(jsonAgain.Contains("sk-abcdefghijklmnopqrstuvwxyz123456", StringComparison.Ordinal));
        Assert.IsTrue(jsonAgain.Contains(BrowserEvidenceRedactor.RedactedValue, StringComparison.Ordinal));
    }

    [TestMethod]
    public void BrowserEvidenceCollector_NoLiveInvocationFlagsRemainFalse()
    {
        var snapshot = FormSnapshot("evidence-no-live");
        var plan = Plan(snapshot).Single(candidate => candidate.ActionKind == SafeBrowserActionKind.Click);
        var result = Execute(plan, snapshot, new FixturePageState());
        var pack = new BrowserEvidenceCollector().CollectFromExecution(result, snapshot, AfterSnapshot(snapshot, result));

        AssertNoLiveInvocation(pack);
        Assert.IsTrue(pack.MetadataOnly);
        Assert.IsTrue(pack.FixtureOnly);
        Assert.IsTrue(pack.LiveExecutionDisabled);
    }

    [TestMethod]
    public void BrowserEvidenceRedactor_SummaryContainingSkToken_RedactsToken()
    {
        var redactor = new BrowserEvidenceRedactor();
        var result = redactor.RedactSummary("summary with sk-abcdefghijklmnopqrstuvwxyz123456 token");

        Assert.AreEqual(BrowserEvidenceRedactionStatus.Partial, result.Status);
        Assert.IsFalse(result.Value.Contains("sk-abcdefghijklmnopqrstuvwxyz123456", StringComparison.Ordinal));
        Assert.IsTrue(result.Value.Contains(BrowserEvidenceRedactor.RedactedValue, StringComparison.Ordinal));
    }

    [TestMethod]
    public void BrowserEvidenceRedactor_JwtCreditCardAndSsnPatterns_AreRedacted()
    {
        var redactor = new BrowserEvidenceRedactor();
        var values = new[]
        {
            "jwt eyJhbGciOiJIUzI1NiJ9.eyJzdWIiOiIxMjMifQ.signature",
            "card 4111 1111 1111 1111",
            "card 4111-1111-1111-1111",
            "ssn 123-45-6789"
        };

        foreach (var value in values)
        {
            var result = redactor.RedactSummary(value);

            Assert.AreEqual(BrowserEvidenceRedactionStatus.Partial, result.Status);
            Assert.IsTrue(result.Value.Contains(BrowserEvidenceRedactor.RedactedValue, StringComparison.Ordinal));
            Assert.IsFalse(result.Value.Contains("4111 1111 1111 1111", StringComparison.Ordinal));
            Assert.IsFalse(result.Value.Contains("4111-1111-1111-1111", StringComparison.Ordinal));
            Assert.IsFalse(result.Value.Contains("123-45-6789", StringComparison.Ordinal));
            Assert.IsFalse(result.Value.Contains("eyJhbGciOiJIUzI1NiJ9.eyJzdWIiOiIxMjMifQ.signature", StringComparison.Ordinal));
        }
    }

    [TestMethod]
    public void BrowserEvidenceCollector_ExecutionResultRedactionMetadata_IsMerged()
    {
        var snapshot = FormSnapshot("evidence-execution-result-redaction");
        var plan = CustomPlan(SafeBrowserActionKind.Click, snapshot, []);
        var pre = new PreActionVerificationResult(true, [], "pre ok", RequiresHumanHandoff: false);
        var post = new PostActionVerificationResult(
            false,
            [],
            "post failed token=sk-abcdefghijklmnopqrstuvwxyz123456",
            RequiresHumanHandoff: false);

        var pack = new BrowserEvidenceCollector().CollectFromVerificationFailure(plan, pre, post, snapshot);
        var json = JsonSerializer.Serialize(pack);

        Assert.AreEqual(BrowserEvidenceRedactionStatus.Partial, pack.RedactionStatus);
        CollectionAssert.Contains(pack.SensitiveFieldsRedacted.ToList(), "secret-pattern");
        Assert.IsFalse(json.Contains("sk-abcdefghijklmnopqrstuvwxyz123456", StringComparison.Ordinal));
        Assert.IsTrue(json.Contains(BrowserEvidenceRedactor.RedactedValue, StringComparison.Ordinal));
    }

    [TestMethod]
    public void BrowserEvidenceRedactor_NormalFieldText_DoesNotRedact()
    {
        var redactor = new BrowserEvidenceRedactor();
        var result = redactor.RedactField("title", "plain fixture title");

        Assert.AreEqual(BrowserEvidenceRedactionStatus.None, result.Status);
        Assert.AreEqual("plain fixture title", result.Value);
    }

    private static ControlledActionExecutionResult Execute(
        SafeBrowserActionPlan plan,
        BrowserPerceptionSnapshot snapshot,
        FixturePageState state)
    {
        var request = new ControlledActionExecutionRequest(
            plan,
            snapshot,
            ControlledActionExecutionMode.FixtureOnly,
            state,
            RequestedAt: DateTimeOffset.UtcNow,
            CorrelationId: "evidence-test");

        return new ControlledActionExecutor().Execute(request);
    }

    private static BrowserPerceptionSnapshot AfterSnapshot(
        BrowserPerceptionSnapshot before,
        ControlledActionExecutionResult result) =>
        before with
        {
            SnapshotId = result.AfterSnapshotRef,
            PageTextPreviewRedacted = before.PageTextPreviewRedacted + " " + string.Join(' ', result.EvidenceDraft.SyntheticMarkers)
        };

    private static IReadOnlyList<SafeBrowserActionPlan> Plan(BrowserPerceptionSnapshot snapshot)
    {
        var profile = new PageCapabilityClassifier().BuildTechnologyProfile(snapshot);
        return new SafeActionPlanner().PlanActions(profile, snapshot, "fixture action");
    }

    private static SafeBrowserActionPlan CustomPlan(
        SafeBrowserActionKind actionKind,
        BrowserPerceptionSnapshot snapshot,
        IReadOnlyList<BrowserActionPostcondition> expectedPostconditions,
        bool preconditionsSatisfied = true)
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
            0.8,
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

    private static void AssertNoLiveInvocation(BrowserEvidencePack pack)
    {
        Assert.IsFalse(pack.CdpInvoked);
        Assert.IsFalse(pack.WebSocketInvoked);
        Assert.IsFalse(pack.BrowserLaunched);
        Assert.IsFalse(pack.SystemBrowserUsed);
        Assert.IsFalse(pack.ExtensionInvoked);
        Assert.IsFalse(pack.ExternalNavigationAttempted);
        Assert.IsFalse(pack.ProductFilesModified);
    }
}
