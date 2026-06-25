using OneBrain.BrowserExecutor.Contracts;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("NativeBrowserSkillsFoundation")]
[TestCategory("M1269A")]
[TestCategory("M1270A")]
[TestCategory("M1271A")]
[TestCategory("M1272A")]
[TestCategory("M1273A")]
[TestCategory("M1274A")]
[TestCategory("M1275A")]
[TestCategory("M1276A")]
[TestCategory("M1277A")]
[TestCategory("M1278A")]
[TestCategory("M1279A")]
[TestCategory("M1280A")]
public sealed class NativeBrowserSkillsFoundationM1269ATests
{
    private const string SidepanelHtmlPath = "browser-extension/onebrain-chrome-lab/sidepanel.html";
    private const string ContractsProjectPath = "src/OneBrain.BrowserExecutor.Contracts/OneBrain.BrowserExecutor.Contracts.csproj";
    private const string RoadmapPath = "docs/roadmap/native-browser-skills-product-roadmap.md";
    private const string ReportPath = "docs/reports/m1280a-native-browser-skills-foundation-product-first.md";

    [TestMethod]
    public void BrowserSkillManifestValidatesRequiredFieldsAndCapabilities()
    {
        var manifest = ValidManifest();

        var result = manifest.Validate();

        Assert.IsTrue(result.IsValid, string.Join(Environment.NewLine, result.Errors));
        Assert.IsTrue(manifest.Capabilities.Contains(BrowserSkillCapability.CdpStateSnapshot));
        Assert.AreEqual(BrowserSkillStatus.DescriptorOnly, manifest.Status);
    }

    [TestMethod]
    public void CapabilityEnvelopeRejectsBrowserActDependencyAndRuntimeActivation()
    {
        var clean = ValidEnvelope();
        var invalid = clean with { BrowserActDependencyPresent = true, RuntimeActive = true };

        Assert.IsTrue(clean.Validate().IsValid);

        var result = invalid.Validate();
        Assert.IsFalse(result.IsValid);
        CollectionAssert.Contains(result.Errors.ToList(), "BrowserAct dependency must not be present in native browser skills foundation.");
        CollectionAssert.Contains(result.Errors.ToList(), "Native browser skills foundation is descriptor-only and cannot activate runtime.");
    }

    [TestMethod]
    public void BrowserStateSnapshotAndIndexedElementValidateShapeAndSecretBoundary()
    {
        var snapshot = new BrowserStateSnapshot(
            "https://demo.nodal.local/mission",
            "Mission Control",
            DateTimeOffset.UtcNow,
            [
                new BrowserIndexedElement("el-run", "button", "Run demo", "#runSafeDemoBtn", "Run demo", "primary demo action")
            ]);

        Assert.IsTrue(snapshot.Validate().IsValid);

        var unsafeSnapshot = snapshot with
        {
            Elements =
            [
                new BrowserIndexedElement("el-secret", "input", "api_key=not-real-but-secret-shaped", "#api", "", "")
            ]
        };

        Assert.IsFalse(unsafeSnapshot.Validate().IsValid);
    }

    [TestMethod]
    public void SessionFrictionAndRecoveryModelsStayDescriptive()
    {
        var friction = new AccessFrictionEvent(
            "friction-captcha",
            BrowserAccessFrictionType.Captcha,
            "visual challenge detected",
            HumanTakeoverNeeded: true,
            "ask user to continue manually");
        var session = new BrowserSkillSessionDescriptor(
            "session-native-skills",
            BrowserSessionSkillStatus.NeedsHuman,
            DateTimeOffset.UtcNow,
            new BrowserSessionResilienceReport("resilience-one", CanRetry: true, "degraded", "", "retry observation later"),
            [friction]);
        var recovery = new BlockedFlowRecoveryPlan(
            "recovery-captcha",
            "challenge needs human input",
            HumanTakeoverNeeded: true,
            "pause and let user finish the challenge");
        var takeover = new HumanTakeoverRequest(
            "handoff-captcha",
            "session-native-skills",
            BrowserAccessFrictionType.Captcha,
            "Human input needed for the challenge.",
            DateTimeOffset.UtcNow);

        Assert.IsTrue(friction.Validate().IsValid);
        Assert.IsTrue(session.Validate().IsValid);
        Assert.IsTrue(recovery.Validate().IsValid);
        Assert.IsTrue(takeover.Validate().IsValid);
    }

    [TestMethod]
    public void StealthProxyAndCaptchaDescriptorsDoNotEnableRuntime()
    {
        var stealth = new StealthProfile(false, BrowserSkillDescriptorMode.FutureDescriptorOnly, "future descriptor only", "native", "not active");
        var proxy = new ProxyRouteProfile(false, BrowserSkillDescriptorMode.FutureDescriptorOnly, "future descriptor only", "native", "not active");
        var captcha = new CaptchaHandlingStrategy("captcha-human-only", CaptchaHandlingMode.HumanTakeoverOnly, AutoSolveAllowed: false, "detect and hand off only");
        var challenge = new CaptchaChallengeEvent("captcha-event", "https://demo.nodal.local/challenge", CaptchaHandlingMode.HumanTakeoverOnly, AutoSolveAllowed: false, "descriptor only");

        Assert.IsTrue(stealth.Validate().IsValid);
        Assert.IsTrue(proxy.Validate().IsValid);
        Assert.IsTrue(captcha.Validate().IsValid);
        Assert.IsTrue(challenge.Validate().IsValid);

        Assert.IsFalse((stealth with { Available = true }).Validate().IsValid);
        Assert.IsFalse((captcha with { AutoSolveAllowed = true }).Validate().IsValid);
    }

    [TestMethod]
    public void NetworkAndCdpCandidatesRemainMetadataAndDescriptorOnly()
    {
        var network = new NetworkEvidenceCandidate(
            "network-candidate",
            "GET",
            "https://demo.nodal.local/status",
            StatusCode: 200,
            MetadataOnly: true,
            Redacted: true);
        var cdp = new CdpOperationCandidate(
            "cdp-candidate",
            CdpOperationKind.ReadState,
            "#demoTimeline",
            RuntimeExecutable: false,
            "read planned timeline state");

        Assert.IsTrue(network.Validate().IsValid);
        Assert.IsTrue(cdp.Validate().IsValid);
        Assert.IsFalse((network with { MetadataOnly = false }).Validate().IsValid);
        Assert.IsFalse((cdp with { RuntimeExecutable = true }).Validate().IsValid);
    }

    [TestMethod]
    public void BrowserActDependencyIsAbsentFromProjectAndNativeRoadmapIsReferenceOnly()
    {
        var project = ReadRepoText(ContractsProjectPath);
        var roadmap = ReadRepoText(RoadmapPath);
        var report = ReadRepoText(ReportPath);

        Assert.IsFalse(project.Contains("BrowserAct", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(project.Contains("browser-act", StringComparison.OrdinalIgnoreCase));
        StringAssert.Contains(roadmap, "BrowserAct: reference only");
        StringAssert.Contains(roadmap, "No BrowserAct dependency");
        StringAssert.Contains(report, "BrowserAct runtime: no integrado");
    }

    [TestMethod]
    public void SidepanelShowsBrowserSkillsDiscreetlyInAdvancedModeAndKeepsDemoHooks()
    {
        var html = ReadRepoText(SidepanelHtmlPath);

        StringAssert.Contains(html, "Browser Skills");
        StringAssert.Contains(html, "Base futura para habilidades de navegador.");
        StringAssert.Contains(html, "Sin runtime activo en esta demo.");
        StringAssert.Contains(html, "BrowserAct");
        StringAssert.Contains(html, "reference only");
        StringAssert.Contains(html, "Run demo");
        StringAssert.Contains(html, "demoGuidanceCard");
        StringAssert.Contains(html, "demoReadyCard");
    }

    private static BrowserSkillManifest ValidManifest() => new(
        "native-browser-skills",
        "Native Browser Skills",
        "Descriptor-first browser skill foundation for future CDP capabilities.",
        [
            BrowserSkillCapability.CdpStateSnapshot,
            BrowserSkillCapability.IndexedElements,
            BrowserSkillCapability.SessionResilience,
            BrowserSkillCapability.AccessFrictionDetection
        ],
        BrowserSkillStatus.DescriptorOnly,
        "0.1.0");

    private static BrowserSkillCapabilityEnvelope ValidEnvelope() => new(
        ValidManifest(),
        BrowserActDependencyPresent: false,
        BrowserActReferenceOnly: true,
        RuntimeActive: false,
        StealthProfiles:
        [
            new StealthProfile(false, BrowserSkillDescriptorMode.FutureDescriptorOnly, "future descriptor only", "native", "not active")
        ],
        ProxyRoutes:
        [
            new ProxyRouteProfile(false, BrowserSkillDescriptorMode.FutureDescriptorOnly, "future descriptor only", "native", "not active")
        ],
        CaptchaStrategies:
        [
            new CaptchaHandlingStrategy("captcha-human-only", CaptchaHandlingMode.HumanTakeoverOnly, AutoSolveAllowed: false, "detect and hand off only")
        ]);

    private static string ReadRepoText(string relativePath) =>
        File.ReadAllText(Path.Combine(RepoRoot(), relativePath));

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(Environment.CurrentDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "OneBrain.slnx")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? Environment.CurrentDirectory;
    }
}
