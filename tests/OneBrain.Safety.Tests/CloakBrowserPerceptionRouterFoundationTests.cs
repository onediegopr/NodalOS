using OneBrain.BrowserPerception;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("CloakBrowserPerceptionRouter")]
public sealed class CloakBrowserPerceptionRouterFoundationTests
{
    [TestMethod]
    public void LegacyFormRoutesDomFirst()
    {
        var decision = Route(new BrowserPerceptionFixture(
            FixtureId: "legacy-form",
            Url: "https://example.test/form?token=secret",
            Title: "Legacy Form",
            FormsCount: 1,
            InputsCount: 2,
            ButtonsCount: 1,
            TextPreview: "Simple form preview"));

        Assert.AreEqual(BrowserPerceptionStrategy.DOM_FIRST, decision.Strategy);
        Assert.IsFalse(decision.HumanHandoffRequired);
    }

    [TestMethod]
    public void SpaInteractiveRoutesAccessibilityFirst()
    {
        var decision = Route(new BrowserPerceptionFixture(
            FixtureId: "spa",
            Url: "https://example.test/app",
            Title: "SPA",
            ButtonsCount: 3,
            SemanticControlCount: 3,
            IsSpa: true));

        Assert.AreEqual(BrowserPerceptionStrategy.ACCESSIBILITY_FIRST, decision.Strategy);
        CollectionAssert.Contains(decision.RequiredSignals.ToList(), PerceptionSignalKind.ACCESSIBILITY);
    }

    [TestMethod]
    public void IframeMarkerRoutesFrameTargetRequired()
    {
        var decision = Route(new BrowserPerceptionFixture(
            FixtureId: "iframe",
            Url: "https://example.test/frame",
            Title: "Iframe",
            HasIframe: true));

        Assert.AreEqual(BrowserPerceptionStrategy.FRAME_TARGET_REQUIRED, decision.Strategy);
    }

    [TestMethod]
    public void ShadowDomMarkerRoutesShadowDomRequired()
    {
        var decision = Route(new BrowserPerceptionFixture(
            FixtureId: "shadow",
            Url: "https://example.test/shadow",
            Title: "Shadow",
            HasShadowDom: true));

        Assert.AreEqual(BrowserPerceptionStrategy.SHADOW_DOM_REQUIRED, decision.Strategy);
    }

    [TestMethod]
    public void CanvasMarkerRoutesVisualRequired()
    {
        var decision = Route(new BrowserPerceptionFixture(
            FixtureId: "canvas",
            Url: "https://example.test/canvas",
            Title: "Canvas",
            HasCanvas: true));

        Assert.AreEqual(BrowserPerceptionStrategy.VISUAL_REQUIRED, decision.Strategy);
    }

    [TestMethod]
    public void ConsoleCriticalErrorRoutesConsoleDiagnosis()
    {
        var decision = Route(new BrowserPerceptionFixture(
            FixtureId: "console",
            Url: "https://example.test/error",
            Title: "Console Error",
            HasConsoleCriticalError: true));

        Assert.AreEqual(BrowserPerceptionStrategy.CONSOLE_DIAGNOSIS_REQUIRED, decision.Strategy);
    }

    [TestMethod]
    public void FailedNetworkServerErrorRoutesNetworkDiagnosis()
    {
        var decision = Route(new BrowserPerceptionFixture(
            FixtureId: "network-500",
            Url: "https://example.test/api",
            Title: "Network Error",
            HasNetworkCriticalFailure: true,
            NetworkStatusCode: 500));

        Assert.AreEqual(BrowserPerceptionStrategy.NETWORK_DIAGNOSIS_REQUIRED, decision.Strategy);
        Assert.IsFalse(decision.HumanHandoffRequired);
    }

    [TestMethod]
    public void FailedNetworkAuthRoutesHumanHandoff()
    {
        foreach (var statusCode in new[] { 401, 403 })
        {
            var decision = Route(new BrowserPerceptionFixture(
                FixtureId: "network-auth",
                Url: "https://example.test/private",
                Title: "Auth Required",
                HasNetworkCriticalFailure: true,
                NetworkStatusCode: statusCode));

            Assert.AreEqual(BrowserPerceptionStrategy.HUMAN_HANDOFF_REQUIRED, decision.Strategy);
            Assert.IsTrue(decision.HumanHandoffRequired);
        }
    }

    [TestMethod]
    public void CaptchaOrTwoFactorMarkerRoutesHumanHandoff()
    {
        var decision = Route(new BrowserPerceptionFixture(
            FixtureId: "human-verification",
            Url: "https://example.test/challenge",
            Title: "Verification",
            HumanVerificationDetected: true,
            AntiBotDetected: true,
            HasNetworkCriticalFailure: true,
            HasConsoleCriticalError: true));

        Assert.AreEqual(BrowserPerceptionStrategy.HUMAN_HANDOFF_REQUIRED, decision.Strategy);
        Assert.IsTrue(decision.HumanHandoffRequired);
    }

    [TestMethod]
    public void UnknownPageRoutesUnsupportedHighRisk()
    {
        var decision = Route(new BrowserPerceptionFixture(
            FixtureId: "unknown",
            Url: "https://example.test/unknown",
            Title: "Unknown"));

        Assert.AreEqual(BrowserPerceptionStrategy.UNSUPPORTED_OR_HIGH_RISK, decision.Strategy);
    }

    [TestMethod]
    public void SnapshotIsMetadataOnlyAndRedacted()
    {
        var snapshot = Snapshot(new BrowserPerceptionFixture(
            FixtureId: "redaction",
            Url: "https://example.test/path?apiKey=secret",
            Title: "Redaction",
            TextPreview: new string('x', 300)));

        Assert.IsTrue(snapshot.Redacted);
        Assert.IsFalse(snapshot.ExtensionUsed);
        Assert.IsFalse(snapshot.SystemBrowserUsed);
        Assert.IsFalse(snapshot.StoresRawDom);
        Assert.IsFalse(snapshot.StoresSensitivePayloads);
        Assert.IsFalse(snapshot.Screenshot.RawScreenshotStored);
        Assert.IsFalse(snapshot.StorageMetadata.ValuesCaptured);
        Assert.AreEqual("https://example.test/path", snapshot.PageUrlRedacted);
        Assert.IsTrue(snapshot.PageTextPreviewRedacted.Length <= 243);
    }

    [TestMethod]
    public void EvidencePackIsPlaceholderMetadataOnly()
    {
        var snapshot = Snapshot(new BrowserPerceptionFixture(
            FixtureId: "evidence",
            Url: "https://example.test/evidence",
            Title: "Evidence",
            FormsCount: 1));
        var profile = new PageCapabilityClassifier().Classify(snapshot);
        var decision = new StrategyRouter().Route(profile);

        var evidence = new BrowserEvidencePackBuilder().Build(snapshot, decision);

        Assert.AreEqual("perception-fixture-evidence", evidence.SnapshotBeforeRef);
        Assert.IsNull(evidence.SnapshotAfterRef);
        Assert.AreEqual(BrowserEvidenceRedactionStatus.None, evidence.RedactionStatus);
        Assert.IsTrue(evidence.NoSensitivePayloadGuarantee);
    }

    [TestMethod]
    public void HumanHandoffHasPriorityOverNetworkConsoleAndVisualSignals()
    {
        var snapshot = Snapshot(new BrowserPerceptionFixture(
            FixtureId: "priority",
            Url: "https://example.test/priority",
            Title: "Priority",
            HumanVerificationDetected: true,
            HasNetworkCriticalFailure: true,
            HasConsoleCriticalError: true,
            HasCanvas: true));
        var profile = new PageCapabilityClassifier().Classify(snapshot);

        var decision = new StrategyRouter().Route(profile);

        Assert.AreEqual(BrowserPerceptionStrategy.HUMAN_HANDOFF_REQUIRED, decision.Strategy);
        Assert.IsTrue(decision.HumanHandoffRequired);
    }

    [TestMethod]
    public void ContractsDoNotUsePageFingerprinterTerminology()
    {
        var exportedNames = string.Join(
            "|",
            typeof(BrowserPerceptionSnapshot).Name,
            typeof(PageCapabilityProfile).Name,
            typeof(PageTechnologyProfile).Name,
            typeof(PageCapabilityClassifier).Name,
            typeof(StrategyRouterDecision).Name,
            typeof(BrowserEvidencePack).Name,
            typeof(BlockageReport).Name);

        Assert.IsFalse(exportedNames.Contains("PageFingerprinter", StringComparison.Ordinal));
    }

    private static StrategyRouterDecision Route(BrowserPerceptionFixture fixture)
    {
        var snapshot = Snapshot(fixture);
        var profile = new PageCapabilityClassifier().Classify(snapshot);
        return new StrategyRouter().Route(profile);
    }

    private static BrowserPerceptionSnapshot Snapshot(BrowserPerceptionFixture fixture) =>
        new BrowserPerceptionSnapshotBuilder().FromFixture(fixture);
}
