using OneBrain.BrowserPerception;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("CloakBrowserPerceptionRouter")]
public sealed class CloakBrowserLocatorBlockageTests
{
    [TestMethod]
    public void LocatorEngine_SpaPage_SelectsAccessibilityStrategy()
    {
        var snapshot = Snapshot(new BrowserPerceptionFixture(
            FixtureId: "locator-spa",
            Url: "https://example.test/app",
            Title: "SPA",
            IsSpa: true,
            SemanticControlCount: 4,
            ButtonsCount: 2));

        var strategy = SelectLocatorStrategy(snapshot);

        Assert.AreEqual(LocatorStrategyKind.Accessibility, strategy.Strategy);
        Assert.IsFalse(strategy.HumanHandoffRequired);
    }

    [TestMethod]
    public void LocatorEngine_LegacyForm_SelectsCssStrategy()
    {
        var snapshot = Snapshot(new BrowserPerceptionFixture(
            FixtureId: "locator-form",
            Url: "https://example.test/form",
            Title: "Form",
            FormsCount: 1,
            InputsCount: 2,
            ButtonsCount: 1));

        var strategy = SelectLocatorStrategy(snapshot);

        Assert.AreEqual(LocatorStrategyKind.Css, strategy.Strategy);
    }

    [TestMethod]
    public void LocatorEngine_IframePage_SelectsFrameTargetRequired()
    {
        var strategy = SelectLocatorStrategy(Snapshot(new BrowserPerceptionFixture(
            FixtureId: "locator-frame",
            Url: "https://example.test/frame",
            Title: "Frame",
            HasIframe: true)));

        Assert.AreEqual(LocatorStrategyKind.FrameTargetRequired, strategy.Strategy);
    }

    [TestMethod]
    public void LocatorEngine_ShadowDomPage_SelectsShadowPiercingRequired()
    {
        var strategy = SelectLocatorStrategy(Snapshot(new BrowserPerceptionFixture(
            FixtureId: "locator-shadow",
            Url: "https://example.test/shadow",
            Title: "Shadow",
            HasShadowDom: true)));

        Assert.AreEqual(LocatorStrategyKind.ShadowPiercingRequired, strategy.Strategy);
    }

    [TestMethod]
    public void LocatorEngine_CanvasPage_SelectsVisualStrategy()
    {
        var strategy = SelectLocatorStrategy(Snapshot(new BrowserPerceptionFixture(
            FixtureId: "locator-canvas",
            Url: "https://example.test/canvas",
            Title: "Canvas",
            HasCanvas: true)));

        Assert.AreEqual(LocatorStrategyKind.Visual, strategy.Strategy);
    }

    [TestMethod]
    public void LocatorEngine_CaptchaOrAuthPage_SelectsHumanHandoff()
    {
        var strategy = SelectLocatorStrategy(Snapshot(new BrowserPerceptionFixture(
            FixtureId: "locator-captcha",
            Url: "https://example.test/challenge",
            Title: "Challenge",
            HumanVerificationDetected: true,
            IsSpa: true,
            SemanticControlCount: 3)));

        Assert.AreEqual(LocatorStrategyKind.HumanHandoff, strategy.Strategy);
        Assert.IsTrue(strategy.HumanHandoffRequired);
    }

    [TestMethod]
    public void LocatorEngine_ContradictorySignals_SelectsHumanHandoff()
    {
        var strategy = SelectLocatorStrategy(Snapshot(new BrowserPerceptionFixture(
            FixtureId: "locator-contradictory",
            Url: "https://example.test/complex",
            Title: "Complex",
            HasIframe: true,
            HasShadowDom: true,
            HasCanvas: true,
            IsSpa: true,
            SemanticControlCount: 2)));

        Assert.AreEqual(LocatorStrategyKind.HumanHandoff, strategy.Strategy);
        Assert.IsTrue(strategy.HumanHandoffRequired);
    }

    [TestMethod]
    public void LocatorEngine_GenerateElementLocators_ReturnsCandidatesOnly()
    {
        var strategy = new LocatorStrategy(
            LocatorStrategyKind.Accessibility,
            0.82,
            "test",
            [PerceptionSignalKind.ACCESSIBILITY],
            HumanHandoffRequired: false);
        var elements = new[]
        {
            new InteractiveElementSnapshot(
                ElementRef: "element:submit",
                TagName: "button",
                Role: "button",
                AccessibleName: "Submit",
                Text: "Submit",
                Id: "submit-button",
                Name: "submit",
                CssClasses: ["primary"],
                BoundingBox: new BrowserBoundingBoxMetadata(10, 20, 100, 30))
        };

        var locators = new LocatorEngine().GenerateElementLocators(elements, strategy);

        Assert.IsTrue(locators.Count >= 3);
        Assert.IsTrue(locators.All(locator => locator.CandidateOnly));
        Assert.IsTrue(locators.All(locator => !locator.ExecutesAction));
    }

    [TestMethod]
    public void LocatorEngine_GenerateElementLocators_RedactsSecretLikeMetadata()
    {
        var strategy = new LocatorStrategy(
            LocatorStrategyKind.FrameTargetRequired,
            0.8,
            "fixture strategy",
            [PerceptionSignalKind.FRAME_TREE, PerceptionSignalKind.DOM],
            HumanHandoffRequired: false);
        var elements = new[]
        {
            new InteractiveElementSnapshot(
                ElementRef: "secret-element",
                TagName: "button",
                Role: "button",
                AccessibleName: "deploy ghp_fakeSecretToken123456789",
                Text: "click sk-test_secret_1234567890",
                Id: "safe-id",
                FrameId: "frame Bearer abcdefghijklmnopqrstuvwxyz123456",
                ShadowRootHint: "shadow eyJhbGciOiJIUzI1NiJ9.eyJzdWIiOiIxMjMifQ.signature",
                IsVisible: true,
                IsEnabled: true)
        };

        var joined = string.Join(" | ", new LocatorEngine().GenerateElementLocators(elements, strategy).Select(locator => locator.Value));

        Assert.IsFalse(joined.Contains("ghp_fakeSecretToken123456789", StringComparison.Ordinal));
        Assert.IsFalse(joined.Contains("sk-test_secret_1234567890", StringComparison.Ordinal));
        Assert.IsFalse(joined.Contains("Bearer abcdefghijklmnopqrstuvwxyz123456", StringComparison.Ordinal));
        Assert.IsFalse(joined.Contains("eyJhbGciOiJIUzI1NiJ9.eyJzdWIiOiIxMjMifQ.signature", StringComparison.Ordinal));
        Assert.IsTrue(joined.Contains(BrowserEvidenceRedactor.RedactedValue, StringComparison.Ordinal));
    }

    [TestMethod]
    public void LocatorEngine_GenerateElementLocators_KeepsNormalMetadata()
    {
        var strategy = new LocatorStrategy(
            LocatorStrategyKind.Accessibility,
            0.8,
            "fixture strategy",
            [PerceptionSignalKind.ACCESSIBILITY],
            HumanHandoffRequired: false);
        var elements = new[]
        {
            new InteractiveElementSnapshot(
                ElementRef: "normal-element",
                TagName: "button",
                Role: "button",
                AccessibleName: "Save changes",
                Text: "Save changes",
                IsVisible: true,
                IsEnabled: true)
        };

        var joined = string.Join(" | ", new LocatorEngine().GenerateElementLocators(elements, strategy).Select(locator => locator.Value));

        StringAssert.Contains(joined, "Save changes");
        Assert.IsFalse(joined.Contains(BrowserEvidenceRedactor.RedactedValue, StringComparison.Ordinal));
    }

    [TestMethod]
    public void BlockageDetector_CaptchaMarker_HumanHandoff()
    {
        var blockages = Detect(new BrowserPerceptionFixture(
            FixtureId: "block-captcha",
            Url: "https://example.test/captcha",
            Title: "Captcha",
            HumanVerificationDetected: true));

        AssertCriticalHuman(blockages, BlockageKind.Captcha);
    }

    [TestMethod]
    public void BlockageDetector_LoginForm_HumanHandoff()
    {
        var blockages = Detect(new BrowserPerceptionFixture(
            FixtureId: "block-login",
            Url: "https://example.test/login",
            Title: "Login",
            LoginFormDetected: true,
            FormsCount: 1,
            InputsCount: 2));

        AssertCriticalHuman(blockages, BlockageKind.Login);
    }

    [TestMethod]
    public void BlockageDetector_TwoFactorMarker_HumanHandoff()
    {
        var blockages = Detect(new BrowserPerceptionFixture(
            FixtureId: "block-2fa",
            Url: "https://example.test/2fa",
            Title: "2FA",
            TwoFactorDetected: true));

        AssertCriticalHuman(blockages, BlockageKind.TwoFactor);
    }

    [TestMethod]
    public void BlockageDetector_AntiBotMarker_HumanHandoff()
    {
        var blockages = Detect(new BrowserPerceptionFixture(
            FixtureId: "block-antibot",
            Url: "https://example.test/anti-bot",
            Title: "Anti Bot",
            AntiBotDetected: true));

        AssertCriticalHuman(blockages, BlockageKind.AntiBot);
    }

    [TestMethod]
    public void BlockageDetector_ForbiddenStatus_AccessDenied()
    {
        var blockage = Detect(new BrowserPerceptionFixture(
            FixtureId: "block-403",
            Url: "https://example.test/private",
            Title: "Forbidden",
            HasNetworkCriticalFailure: true,
            NetworkStatusCode: 403)).Single(report => report.BlockageKind == BlockageKind.AccessDenied);

        Assert.AreEqual(BrowserPerceptionSeverity.Critical, blockage.Severity);
        Assert.IsFalse(blockage.CanContinueAutomatically);
    }

    [TestMethod]
    public void BlockageDetector_RateLimitStatus_BlocksAutomaticContinuation()
    {
        var blockage = Detect(new BrowserPerceptionFixture(
            FixtureId: "block-429",
            Url: "https://example.test/rate-limit",
            Title: "Rate Limit",
            HasNetworkCriticalFailure: true,
            NetworkStatusCode: 429)).Single(report => report.BlockageKind == BlockageKind.RateLimit);

        Assert.AreEqual(BrowserPerceptionSeverity.Critical, blockage.Severity);
        Assert.IsFalse(blockage.CanContinueAutomatically);
        StringAssert.Contains(blockage.Reason, "no bypass");
    }

    [TestMethod]
    public void BlockageDetector_CookieWall_IsWarningOnly()
    {
        var blockage = Detect(new BrowserPerceptionFixture(
            FixtureId: "block-cookie",
            Url: "https://example.test/cookie",
            Title: "Cookie",
            CookieWallDetected: true)).Single(report => report.BlockageKind == BlockageKind.CookieWall);

        Assert.AreEqual(BrowserPerceptionSeverity.Warning, blockage.Severity);
        Assert.IsTrue(blockage.CanContinueAutomatically);
        Assert.IsFalse(blockage.RequiresHumanHandoff);
    }

    [TestMethod]
    public void BlockageDetector_ConsoleCriticalError_ConsoleError()
    {
        var blockage = Detect(new BrowserPerceptionFixture(
            FixtureId: "block-console",
            Url: "https://example.test/console",
            Title: "Console",
            HasConsoleCriticalError: true)).Single(report => report.BlockageKind == BlockageKind.ConsoleError);

        Assert.AreEqual(BrowserPerceptionSeverity.Critical, blockage.Severity);
        Assert.IsFalse(blockage.CanContinueAutomatically);
    }

    [TestMethod]
    public void BlockageDetector_NetworkServerError_NetworkFailure()
    {
        var blockage = Detect(new BrowserPerceptionFixture(
            FixtureId: "block-500",
            Url: "https://example.test/server",
            Title: "Server",
            HasNetworkCriticalFailure: true,
            NetworkStatusCode: 500)).Single(report => report.BlockageKind == BlockageKind.NetworkFailure);

        Assert.AreEqual(BrowserPerceptionSeverity.Critical, blockage.Severity);
        Assert.IsFalse(blockage.CanContinueAutomatically);
    }

    [TestMethod]
    public void BlockageDetector_NormalizesSignalNameBeforeMatching()
    {
        var snapshot = Snapshot(new BrowserPerceptionFixture(
            FixtureId: "block-normalized-signal",
            Url: "https://example.test/challenge",
            Title: "Challenge")) with
        {
            Signals =
            [
                new PerceptionSignal(
                    PerceptionSignalKind.HIT_TEST,
                    "  CaPtChA  ",
                    BrowserPerceptionSeverity.Critical,
                    "fixture signal")
            ]
        };

        var blockage = new BlockageDetector().DetectBlockages(snapshot).Single(report => report.BlockageKind == BlockageKind.Captcha);

        Assert.IsTrue(blockage.RequiresHumanHandoff);
    }

    [TestMethod]
    public void StrategyRouter_PrioritizesHumanHandoffOverLocatorStrategy()
    {
        var snapshot = Snapshot(new BrowserPerceptionFixture(
            FixtureId: "router-priority",
            Url: "https://example.test/challenge",
            Title: "Challenge",
            HumanVerificationDetected: true,
            IsSpa: true,
            SemanticControlCount: 3));
        var classifier = new PageCapabilityClassifier();
        var profile = classifier.Classify(snapshot);

        var decision = new StrategyRouter().Route(profile, snapshot);

        Assert.AreEqual(BrowserPerceptionStrategy.HUMAN_HANDOFF_REQUIRED, decision.Strategy);
        Assert.IsTrue(decision.HumanHandoffRequired);
        Assert.AreEqual(LocatorStrategyKind.HumanHandoff, decision.LocatorStrategy?.Strategy);
        Assert.IsTrue(decision.Blockages.Any(blockage => blockage.BlockageKind == BlockageKind.Captcha));
    }

    private static LocatorStrategy SelectLocatorStrategy(BrowserPerceptionSnapshot snapshot)
    {
        var profile = new PageCapabilityClassifier().BuildTechnologyProfile(snapshot);
        return new LocatorEngine().SelectLocatorStrategy(profile, snapshot);
    }

    private static IReadOnlyList<BlockageReport> Detect(BrowserPerceptionFixture fixture) =>
        new BlockageDetector().DetectBlockages(Snapshot(fixture));

    private static BrowserPerceptionSnapshot Snapshot(BrowserPerceptionFixture fixture) =>
        new BrowserPerceptionSnapshotBuilder().FromFixture(fixture);

    private static void AssertCriticalHuman(IReadOnlyList<BlockageReport> blockages, BlockageKind expected)
    {
        var blockage = blockages.Single(report => report.BlockageKind == expected);
        Assert.AreEqual(BrowserPerceptionSeverity.Critical, blockage.Severity);
        Assert.IsFalse(blockage.CanContinueAutomatically);
        Assert.IsTrue(blockage.RequiresHumanHandoff);
    }
}
