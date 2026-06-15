using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
public sealed class BrowserRealSiteReadOnlyTests
{
    [TestMethod]
    public void BrowserRealSiteReadOnlyScenarioDoesNotLiveInsideGenericExecutor()
    {
        var executorSource = File.ReadAllText(SourcePath("src", "OneBrain.BrowserExecutor.Cdp", "ChromeCdpBrowserExecutor.cs"));
        var scenarioPath = SourcePath("src", "OneBrain.BrowserExecutor.Cdp", "BrowserRealSiteReadOnlyScenario.cs");

        Assert.IsTrue(File.Exists(scenarioPath));
        Assert.IsFalse(executorSource.Contains("MercadoLibre", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(executorSource.Contains("mercadolibre", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(executorSource.Contains("BrowserRealSiteReadOnlyScenario", StringComparison.Ordinal));
    }

    [TestMethod]
    public void BrowserRealSiteReadOnlyPolicyBlocksSensitiveActions()
    {
        var scenario = new BrowserRealSiteReadOnlyScenario();
        var action = Action(
            BrowserActionType.Click,
            new BrowserActionTarget("buy", "#buyButton", "Comprar ahora", null),
            BrowserRiskClass.Critical,
            requiresApproval: true);

        var decision = scenario.EvaluateAction(action);

        Assert.IsFalse(decision.Allowed);
        Assert.IsTrue(decision.RequiresApproval || decision.ErrorCode == BrowserRuntimeErrorCode.ActionRejected);
    }

    [TestMethod]
    public void BrowserRealSiteReadOnlyPolicyAllowsPublicNavigationAndRead()
    {
        var scenario = new BrowserRealSiteReadOnlyScenario();
        var navigate = Action(
            BrowserActionType.Navigate,
            new BrowserActionTarget("public-url", null, "public URL", "https://www.example.com/"),
            BrowserRiskClass.Low,
            idempotencyKey: "idem-nav");
        var read = Action(
            BrowserActionType.Read,
            new BrowserActionTarget("body", "body", "body", null),
            BrowserRiskClass.ReadOnly);

        Assert.IsTrue(scenario.EvaluateAction(navigate).Allowed);
        Assert.IsTrue(scenario.EvaluateAction(read).Allowed);
    }

    [TestMethod]
    public void BrowserRealSiteReadOnlyVerificationUncertainDoesNotMarkSuccess()
    {
        var scenario = new BrowserRealSiteReadOnlyScenario();
        var options = BrowserRealSiteReadOnlyScenario.MercadoLibrePublicSearch("sonoff rf bridge");
        var observation = Observation("https://listado.mercadolibre.com.ar/other", "Other", "unrelated public content");

        var verification = scenario.VerifyObservation(observation, options, "verify", null);
        var report = new BrowserRealSiteReadOnlyReport(
            "run",
            options.SiteName,
            BrowserRealSiteReadOnlyStatus.Uncertain,
            BrowserRuntimeErrorCode.VerificationUncertain,
            observation.Url,
            observation.Title,
            verification.Status,
            [],
            [],
            [],
            CleanupCompleted: true,
            UsedServiceWorker: false,
            UsedRealProfile: false);

        Assert.AreEqual(BrowserVerificationStatus.Uncertain, verification.Status);
        Assert.IsFalse(verification.AllowsStepDone());
        Assert.IsFalse(report.Success);
    }

    [TestMethod]
    public void BrowserRealSiteReadOnlyDetectsLoginCaptchaAndAntiBotBlocks()
    {
        var scenario = new BrowserRealSiteReadOnlyScenario();

        var captcha = scenario.DetectBlocking(Observation("https://public.example/", "Verify", "captcha verify you are human"));
        var login = scenario.DetectBlocking(Observation("https://public.example/login", "Login", "inicia sesión para continuar password"));
        var accessDenied = scenario.DetectBlocking(Observation("https://public.example/", "Denied", "access denied request blocked"));

        Assert.IsTrue(captcha.Blocked);
        Assert.AreEqual(BrowserRealSiteReadOnlyStatus.RequiresHuman, captcha.Status);
        Assert.IsTrue(login.Blocked);
        Assert.AreEqual(BrowserRealSiteReadOnlyStatus.RequiresHuman, login.Status);
        Assert.IsTrue(accessDenied.Blocked);
        Assert.AreEqual(BrowserRealSiteReadOnlyStatus.Blocked, accessDenied.Status);
    }

    [TestMethod]
    public void BrowserRealSiteReadOnlyEvidenceShapeIncludesBeforeAfterVerificationAndPolicy()
    {
        var target = Target("run-evidence", new Uri("https://www.example.com/"));
        var evidence = new[]
        {
            Evidence("run-evidence", "before", null, null, target),
            Evidence("run-evidence", "navigate", "action-nav", null, target),
            Evidence("run-evidence", "after", null, null, target),
            Evidence("run-evidence", "verify", null, "verification-1", target)
        };
        var report = new BrowserRealSiteReadOnlyReport(
            "run-evidence",
            "public read-only",
            BrowserRealSiteReadOnlyStatus.Verified,
            BrowserRuntimeErrorCode.None,
            target.Url,
            target.Title,
            BrowserVerificationStatus.Verified,
            evidence,
            ["real-site read-only action allowed"],
            ["verified public URL/title/text"],
            CleanupCompleted: true,
            UsedServiceWorker: false,
            UsedRealProfile: false);

        Assert.IsTrue(report.Success);
        Assert.IsTrue(report.Evidence.Any(item => item.StepId == "before"));
        Assert.IsTrue(report.Evidence.Any(item => item.ActionId == "action-nav"));
        Assert.IsTrue(report.Evidence.Any(item => item.StepId == "after"));
        Assert.IsTrue(report.Evidence.Any(item => item.VerificationId == "verification-1"));
        Assert.IsTrue(report.PolicyDecisions.Count > 0);
        Assert.IsTrue(report.Evidence.All(item => item.RedactionApplied));
    }

    [TestMethod]
    public void BrowserRealSiteReadOnlyExtensionRelayCannotMarkSuccess()
    {
        var serviceWorker = File.ReadAllText(SourcePath("browser-extension", "onebrain-chrome-lab", "service_worker.js"));

        StringAssert.Contains(serviceWorker, "authoritative: false");
        StringAssert.Contains(serviceWorker, "verificationStatus: 'NotVerified'");
        StringAssert.Contains(serviceWorker, "normalizeCoreRunStatus(message)");
    }

    [TestMethod]
    public void BrowserRealSiteReadOnlyCleanupPathIsExplicit()
    {
        var scenarioSource = File.ReadAllText(SourcePath("src", "OneBrain.BrowserExecutor.Cdp", "BrowserRealSiteReadOnlyScenario.cs"));

        StringAssert.Contains(scenarioSource, "await using var session");
        StringAssert.Contains(scenarioSource, "UsedRealProfile: false");
        StringAssert.Contains(scenarioSource, "UsedServiceWorker: false");
        StringAssert.Contains(scenarioSource, "CleanupCompleted");
    }

    [TestMethod]
    public async Task BrowserRealSiteReadOnlyLiveSmokeIsOptIn()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable("ONEBRAIN_BROWSER_LIVE_READONLY"), "1", StringComparison.Ordinal))
            Assert.Inconclusive("Live read-only browser test skipped. Set ONEBRAIN_BROWSER_LIVE_READONLY=1 to run it.");

        var browserPath = ChromeCdpBrowserLauncher.FindBrowserExecutable();
        if (browserPath is null)
            Assert.Inconclusive("Chrome/Edge executable is not available in this environment.");

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(45));
        var report = await new BrowserRealSiteReadOnlyScenario().RunLiveAsync(
            BrowserRealSiteReadOnlyScenario.MercadoLibrePublicSearch(),
            browserPath,
            cts.Token);

        Console.WriteLine($"M6 live read-only status={report.Status}; verification={report.VerificationStatus}; error={report.ErrorCode}; finalUrl={report.FinalUrl}; evidence={report.Evidence.Count}; cleanup={report.CleanupCompleted}");
        Assert.IsTrue(report.CleanupCompleted);
        Assert.IsFalse(report.UsedRealProfile);
        Assert.IsFalse(report.UsedServiceWorker);
        Assert.IsTrue(report.Status is BrowserRealSiteReadOnlyStatus.Verified or BrowserRealSiteReadOnlyStatus.Blocked or BrowserRealSiteReadOnlyStatus.RequiresHuman or BrowserRealSiteReadOnlyStatus.Uncertain or BrowserRealSiteReadOnlyStatus.TimedOut);
        if (report.Status == BrowserRealSiteReadOnlyStatus.Verified)
        {
            Assert.AreEqual(BrowserVerificationStatus.Verified, report.VerificationStatus);
            Assert.IsTrue(report.Evidence.Count > 0);
        }
        else
        {
            Assert.IsFalse(report.Success);
        }
    }

    private static BrowserAction Action(
        BrowserActionType type,
        BrowserActionTarget actionTarget,
        BrowserRiskClass risk,
        bool requiresApproval = false,
        string idempotencyKey = "") =>
        new(
            ActionId: "action-" + Guid.NewGuid().ToString("N"),
            IdempotencyKey: idempotencyKey,
            RunId: "run-real-site",
            StepId: "step-real-site",
            TargetContext: Target("run-real-site", new Uri("https://www.example.com/")),
            FrameId: "main",
            ActionType: type,
            Target: actionTarget,
            Input: null,
            ExpectedOutcome: new BrowserExpectedOutcome("public read-only expectation", "example.com", "Example", null),
            RiskClass: risk,
            TimeoutMs: 8000,
            RequiresApproval: requiresApproval,
            CreatedAtUtc: DateTimeOffset.UtcNow);

    private static BrowserObservation Observation(string url, string title, string text)
    {
        var target = Target("run-observe", new Uri(url), title);
        return new BrowserObservation(
            ObservationId: "observation-" + Guid.NewGuid().ToString("N"),
            RunId: target.RunId,
            TargetContext: target,
            ObservedAtUtc: DateTimeOffset.UtcNow,
            Url: target.Url,
            Title: title,
            ReadyState: "complete",
            FrameCount: 1,
            MainFrameId: "main",
            VisibleTextSummary: text,
            ActionableElements: [],
            Forms: [],
            Links: [],
            Warnings: [],
            PayloadLimitApplied: false,
            SensitivityRedactionApplied: true,
            EvidenceRefs: ["evidence-observation"]);
    }

    private static BrowserTargetContext Target(string runId, Uri url, string title = "Public page") =>
        new(
            RunId: runId,
            BrowserId: "test-browser",
            BrowserSessionId: "test-session",
            BrowserContextId: null,
            WindowId: null,
            TargetId: "target-1",
            PageId: "page-1",
            TabId: null,
            FrameId: "main",
            ParentFrameId: null,
            Url: url,
            Title: title,
            Generation: 1,
            LivenessToken: BrowserTargetContext.CreateLivenessToken("target-1", "main", 1),
            ObservedAtUtc: DateTimeOffset.UtcNow,
            IsActive: null,
            IsVisible: null,
            IsUserFacing: null,
            ReadyState: "complete",
            Source: BrowserTargetSource.Cdp);

    private static BrowserEvidence Evidence(string runId, string stepId, string? actionId, string? verificationId, BrowserTargetContext target) =>
        new(
            EvidenceId: "evidence-" + Guid.NewGuid().ToString("N"),
            RunId: runId,
            StepId: stepId,
            ActionId: actionId,
            VerificationId: verificationId,
            TargetContext: target,
            EvidenceType: verificationId is null ? BrowserEvidenceType.TextExtract : BrowserEvidenceType.VerificationResult,
            CreatedAtUtc: DateTimeOffset.UtcNow,
            Summary: stepId,
            PayloadRef: null,
            InlinePayload: null,
            RedactionApplied: true,
            SensitivityLevel: BrowserSensitivityLevel.Low);

    private static string SourcePath(params string[] segments)
    {
        var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "OneBrain.slnx")))
            directory = directory.Parent;

        Assert.IsNotNull(directory, "Repository root was not found.");
        return Path.Combine(new[] { directory!.FullName }.Concat(segments).ToArray());
    }
}
