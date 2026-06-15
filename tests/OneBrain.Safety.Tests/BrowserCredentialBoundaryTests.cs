using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
public sealed class BrowserCredentialBoundaryTests
{
    [TestMethod]
    public void BrowserCredentialPasswordFieldRequiresHuman()
    {
        var decision = new BrowserCredentialBoundaryDetector().EvaluateObservation(Observation(
            actionables:
            [
                Element("password", "input", "textbox", "Contraseña", ["input[type=password]"])
            ],
            forms: [new FormSummary("login-form", "main", 2, "Login form", ["password"])]));

        Assert.AreEqual(BrowserCredentialBoundaryDecisionKind.RequiresHuman, decision.Decision);
        Assert.AreEqual(BrowserHumanHandoffReason.PasswordRequired, decision.HandoffReason);
        Assert.IsTrue(decision.Boundary.Validate().IsValid);
    }

    [TestMethod]
    public void BrowserCredentialLoginFormSubmitIsBlockedBeforeExecution()
    {
        var target = Target();
        var action = Action(target, BrowserActionType.Click, "#loginSubmit", "Ingresar login submit");
        var decision = new BrowserCredentialBoundaryDetector().EvaluateAction(action);
        var dispatcher = new StubDispatcher();
        var runner = new BrowserExecutorStepRunner(new BrowserExecutorPolicyGate(), dispatcher, dispatcher);

        var result = runner.ExecuteAsync(new BrowserExecutorStepRequest(
            "corr-login",
            action,
            Capabilities(BrowserRiskClass.Critical),
            target,
            CredentialBoundaryDecision: decision,
            ProfileId: "profile-1",
            SessionId: "session-1")).GetAwaiter().GetResult();

        Assert.IsFalse(result.Success);
        Assert.AreEqual(BrowserExecutorStepState.RequiresHuman, result.FinalState);
        Assert.IsNotNull(result.HumanHandoff);
        Assert.AreEqual(0, dispatcher.ExecuteCount);
    }

    [TestMethod]
    public void BrowserCredentialOtpAndTwoFactorRequireHuman()
    {
        var decision = new BrowserCredentialBoundaryDetector().EvaluateObservation(Observation(
            text: "Verificación en dos pasos. Ingrese el código OTP.",
            actionables: [Element("otp", "input", "textbox", "Código OTP", ["#otp", "autocomplete=one-time-code"])]));

        Assert.AreEqual(BrowserCredentialBoundaryDecisionKind.RequiresHuman, decision.Decision);
        Assert.AreEqual(BrowserHumanHandoffReason.TwoFactorRequired, decision.HandoffReason);
    }

    [TestMethod]
    public void BrowserCredentialCaptchaRequiresHuman()
    {
        var decision = new BrowserCredentialBoundaryDetector().EvaluateObservation(Observation(
            text: "Complete el CAPTCHA. No soy un robot.",
            actionables: [Element("captcha", "div", "group", "captcha challenge", ["#captcha"])]));

        Assert.AreEqual(BrowserCredentialBoundaryDecisionKind.RequiresHuman, decision.Decision);
        Assert.AreEqual(BrowserHumanHandoffReason.CaptchaRequired, decision.HandoffReason);
    }

    [TestMethod]
    public void BrowserCredentialClaveFiscalRequiresHuman()
    {
        var decision = new BrowserCredentialBoundaryDetector().EvaluateObservation(Observation(text: "Ingrese su clave fiscal para continuar."));

        Assert.AreEqual(BrowserCredentialBoundaryDecisionKind.RequiresHuman, decision.Decision);
        Assert.AreEqual(BrowserHumanHandoffReason.ClaveFiscalRequired, decision.HandoffReason);
    }

    [TestMethod]
    public void BrowserCredentialFinancialActionRequiresHuman()
    {
        var decision = new BrowserCredentialBoundaryDetector().EvaluateAction(Action(Target(), BrowserActionType.Click, "#payNow", "Pagar ahora payment"));

        Assert.AreEqual(BrowserCredentialBoundaryDecisionKind.RequiresHuman, decision.Decision);
        Assert.AreEqual(BrowserHumanHandoffReason.FinancialActionRequired, decision.HandoffReason);
    }

    [TestMethod]
    public void BrowserCredentialUnknownSensitivePromptFailsClosed()
    {
        var decision = new BrowserCredentialBoundaryDetector().EvaluateObservation(Observation(text: "Unknown sensitive prompt. Human decision needed."));

        Assert.AreEqual(BrowserCredentialBoundaryDecisionKind.FailClosed, decision.Decision);
        Assert.IsTrue(decision.BlocksAutomation);
        Assert.AreEqual(BrowserHumanHandoffReason.UnknownSensitivePrompt, decision.HandoffReason);
    }

    [TestMethod]
    public void BrowserCredentialTypeTextIntoPasswordFieldDoesNotExecuteAutomatically()
    {
        var target = Target();
        var action = Action(target, BrowserActionType.TypeText, "#password", "password field", "not-a-real-secret");
        var decision = new BrowserCredentialBoundaryDetector().EvaluateAction(action);
        var dispatcher = new StubDispatcher();

        var result = new BrowserExecutorStepRunner(new BrowserExecutorPolicyGate(), dispatcher, dispatcher)
            .ExecuteAsync(new BrowserExecutorStepRequest("corr-password", action, Capabilities(BrowserRiskClass.Critical), target, CredentialBoundaryDecision: decision))
            .GetAwaiter()
            .GetResult();

        Assert.IsFalse(result.Success);
        Assert.AreEqual(BrowserExecutorStepState.RequiresHuman, result.FinalState);
        Assert.AreEqual(0, dispatcher.ExecuteCount);
    }

    [TestMethod]
    public void BrowserCredentialHandoffRequestIncludesContextAndNoSecrets()
    {
        var target = Target(title: "Login password=secret");
        var action = Action(target, BrowserActionType.Click, "#loginSubmit", "Ingresar");
        var decision = new BrowserCredentialBoundaryDetector().EvaluateAction(action);

        var request = new BrowserHumanHandoffCoordinator().CreateRequest(decision, action, "corr-1", "profile-1", "session-1");

        Assert.AreEqual(action.RunId, request.Context.RunId);
        Assert.AreEqual(action.ActionId, request.Context.ActionId);
        Assert.AreEqual("corr-1", request.Context.CorrelationId);
        Assert.AreEqual("profile-1", request.Context.ProfileId);
        Assert.AreEqual("session-1", request.Context.SessionId);
        Assert.AreEqual(target.FrameId, request.Context.FrameId);
        Assert.IsTrue(request.RedactionApplied);
        Assert.IsTrue(request.Validate().IsValid);
        Assert.IsFalse(BrowserCredentialRedactor.ContainsSecret(request.Context.RedactedTitle));
    }

    [TestMethod]
    public void BrowserCredentialEvidenceAndRedactionDoNotExposeSecrets()
    {
        var raw = "password=abc123 token=xyz cookie: session authorization: bearer abc cuit 20-12345678-1 dni 12345678";
        var redacted = BrowserCredentialRedactor.Redact(raw);

        Assert.IsFalse(redacted.Contains("abc123", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(redacted.Contains("xyz", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(redacted.Contains("session", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(redacted.Contains("20-12345678-1", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(BrowserCredentialRedactor.ContainsSecret(redacted));
    }

    [TestMethod]
    public void BrowserCredentialSensitiveSubmitWithoutHumanIsBlocked()
    {
        var target = Target();
        var action = Action(target, BrowserActionType.Click, "#submitSensitive", "submit sensitive login form");
        var decision = new BrowserCredentialBoundaryDetector().EvaluateAction(action);

        Assert.IsTrue(decision.RequiresHuman);
        Assert.AreEqual(BrowserHumanHandoffReason.SensitiveSubmitRequired, decision.HandoffReason);
    }

    private static BrowserObservation Observation(
        string text = "Fixture page",
        IReadOnlyList<ActionableElement>? actionables = null,
        IReadOnlyList<FormSummary>? forms = null) =>
        new(
            ObservationId: "observation-1",
            RunId: "run-1",
            TargetContext: Target(),
            ObservedAtUtc: DateTimeOffset.UtcNow,
            Url: new Uri("file:///fixture/login-form.html"),
            Title: "Credential fixture",
            ReadyState: "complete",
            FrameCount: 1,
            MainFrameId: "main",
            VisibleTextSummary: text,
            ActionableElements: actionables ?? [],
            Forms: forms ?? [],
            Links: [],
            Warnings: [],
            PayloadLimitApplied: false,
            SensitivityRedactionApplied: true,
            EvidenceRefs: ["evidence-observation"]);

    private static ActionableElement Element(string id, string tag, string role, string label, IReadOnlyList<string> selectors) =>
        new(id, "main", role, tag, "", label, label, selectors, null, true, true, [], 0.95);

    private static BrowserAction Action(BrowserTargetContext target, BrowserActionType type, string selector, string text, string? input = null) =>
        new(
            ActionId: "action-1",
            IdempotencyKey: "idem-1",
            RunId: "run-1",
            StepId: "step-1",
            TargetContext: target,
            FrameId: target.FrameId,
            ActionType: type,
            Target: new BrowserActionTarget("candidate-1", selector, text, null),
            Input: input is null ? null : new BrowserActionInput(input, input, HasModifyingValue: true),
            ExpectedOutcome: new BrowserExpectedOutcome("sensitive action must not auto execute", null, null, null),
            RiskClass: BrowserRiskClass.Critical,
            TimeoutMs: 8000,
            RequiresApproval: true,
            CreatedAtUtc: DateTimeOffset.UtcNow);

    private static BrowserExecutorCapabilities Capabilities(BrowserRiskClass riskLimit = BrowserRiskClass.High) =>
        new(
            ExecutorId: "test-browser-executor",
            ExecutorKind: BrowserExecutorKind.Cdp,
            Capabilities: new HashSet<BrowserActionType> { BrowserActionType.Read, BrowserActionType.Click, BrowserActionType.TypeText, BrowserActionType.WaitFor },
            RiskLimit: riskLimit,
            SupportsTrustedInput: true,
            SupportsDomSnapshot: true,
            SupportsAccessibilitySnapshot: false,
            SupportsScreenshots: false,
            SupportsFrames: true,
            SupportsDownloads: false,
            SupportsFileUpload: false,
            RequiresBrowserLaunch: true,
            RequiresRemoteDebugging: true,
            CanAttachExistingBrowser: false,
            CanUsePersistentProfile: false,
            CanUseRealUserProfile: false);

    private static BrowserTargetContext Target(long generation = 1, string title = "Credential fixture")
    {
        const string targetId = "target-1";
        const string frameId = "main";
        return new BrowserTargetContext(
            RunId: "run-1",
            BrowserId: "browser-1",
            BrowserSessionId: "session-1",
            BrowserContextId: null,
            WindowId: null,
            TargetId: targetId,
            PageId: targetId,
            TabId: null,
            FrameId: frameId,
            ParentFrameId: null,
            Url: new Uri("file:///fixture/login-form.html"),
            Title: title,
            Generation: generation,
            LivenessToken: BrowserTargetContext.CreateLivenessToken(targetId, frameId, generation),
            ObservedAtUtc: DateTimeOffset.UtcNow,
            IsActive: true,
            IsVisible: true,
            IsUserFacing: true,
            ReadyState: "complete",
            Source: BrowserTargetSource.Fixture);
    }

    private sealed class StubDispatcher : IBrowserActionDispatcher, IBrowserVerificationDispatcher
    {
        public int ExecuteCount { get; private set; }

        public Task<ChromeCdpActionResult> ExecuteActionAsync(BrowserAction action, CancellationToken cancellationToken)
        {
            ExecuteCount++;
            return Task.FromResult(new ChromeCdpActionResult(action.ActionId, true, "Executed", Evidence(action)));
        }

        public Task<BrowserVerification> VerifyAsync(BrowserAction action, CancellationToken cancellationToken) =>
            Task.FromResult(Verification(action, BrowserVerificationStatus.Verified));

        private static BrowserEvidence Evidence(BrowserAction action) =>
            new(Guid.NewGuid().ToString("N"), action.RunId, action.StepId, action.ActionId, null, action.TargetContext, BrowserEvidenceType.InputEvent, DateTimeOffset.UtcNow, "stub", null, null, true, BrowserSensitivityLevel.Low);

        private static BrowserVerification Verification(BrowserAction action, BrowserVerificationStatus status) =>
            new("verification-1", action.RunId, action.StepId, action.ActionId, action.TargetContext, action.ExpectedOutcome, null, "observation-after", status, 0.99, ["evidence-verify"], null, DateTimeOffset.UtcNow, ProofRefs: ["proof-verify"]);
    }
}
