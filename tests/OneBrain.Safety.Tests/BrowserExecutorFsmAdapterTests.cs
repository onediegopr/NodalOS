using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;
using OneBrain.Core.Execution;

namespace OneBrain.Safety.Tests;

[TestClass]
public sealed class BrowserExecutorFsmAdapterTests
{
    [TestMethod]
    public async Task BrowserExecutorFsmReadOnlyStepPassesThroughStatesAndVerifies()
    {
        var target = CreateTarget();
        var action = CreateAction(target, BrowserActionType.Read, BrowserRiskClass.ReadOnly, idempotencyKey: "");
        var dispatcher = new StubBrowserDispatcher(
            new ChromeCdpActionResult(action.ActionId, true, "Executed", Evidence(action)),
            Verification(action, BrowserVerificationStatus.Verified));
        var runner = CreateRunner(dispatcher);

        var result = await runner.ExecuteAsync(new BrowserExecutorStepRequest("corr-1", action, Capabilities(), target));

        Assert.IsTrue(result.Success);
        Assert.AreEqual(BrowserExecutorStepState.Verified, result.FinalState);
        CollectionAssert.Contains(result.StateHistory.ToList(), BrowserExecutorStepState.PolicyChecking);
        CollectionAssert.Contains(result.StateHistory.ToList(), BrowserExecutorStepState.Executing);
        CollectionAssert.Contains(result.StateHistory.ToList(), BrowserExecutorStepState.Executed);
        CollectionAssert.Contains(result.StateHistory.ToList(), BrowserExecutorStepState.Verifying);
        Assert.AreEqual(1, dispatcher.ExecuteCount);
        Assert.IsTrue(result.Evidence.HasEvidenceBeforeSuccess);
        Assert.IsTrue(result.Evidence.CoreLedger.Entries.Any(entry => entry.ToState == StepState.Succeeded));
    }

    [TestMethod]
    public async Task PolicyGateRunsBeforeExecutionAndBlocksMissingIdempotency()
    {
        var target = CreateTarget();
        var action = CreateAction(target, BrowserActionType.Click, BrowserRiskClass.Low, idempotencyKey: "");
        var dispatcher = new StubBrowserDispatcher(
            new ChromeCdpActionResult(action.ActionId, true, "Executed", Evidence(action)),
            Verification(action, BrowserVerificationStatus.Verified));
        var runner = CreateRunner(dispatcher);

        var result = await runner.ExecuteAsync(new BrowserExecutorStepRequest("corr-2", action, Capabilities(), target));

        Assert.IsFalse(result.Success);
        Assert.AreEqual(BrowserExecutorStepState.Blocked, result.FinalState);
        Assert.AreEqual(BrowserRuntimeErrorCode.IdempotencyRejected, result.ErrorCode);
        Assert.AreEqual(0, dispatcher.ExecuteCount);
        var history = result.StateHistory.ToList();
        Assert.IsTrue(history.IndexOf(BrowserExecutorStepState.PolicyChecking) < history.IndexOf(BrowserExecutorStepState.Blocked));
    }

    [TestMethod]
    public async Task CriticalActionWithoutApprovalRequiresHumanAndDoesNotExecute()
    {
        var target = CreateTarget();
        var action = CreateAction(target, BrowserActionType.Click, BrowserRiskClass.Critical, requiresApproval: true);
        var dispatcher = new StubBrowserDispatcher(
            new ChromeCdpActionResult(action.ActionId, true, "Executed", Evidence(action)),
            Verification(action, BrowserVerificationStatus.Verified));

        var result = await CreateRunner(dispatcher).ExecuteAsync(new BrowserExecutorStepRequest("corr-3", action, Capabilities(riskLimit: BrowserRiskClass.Critical), target));

        Assert.IsFalse(result.Success);
        Assert.AreEqual(BrowserExecutorStepState.ApprovalRequired, result.FinalState);
        Assert.IsNotNull(result.HumanHandoff);
        Assert.AreEqual(0, dispatcher.ExecuteCount);
    }

    [TestMethod]
    public async Task CriticalActionWithApprovalCanExecuteAndVerify()
    {
        var target = CreateTarget();
        var action = CreateAction(target, BrowserActionType.Click, BrowserRiskClass.Critical, requiresApproval: true);
        var dispatcher = new StubBrowserDispatcher(
            new ChromeCdpActionResult(action.ActionId, true, "Executed", Evidence(action)),
            Verification(action, BrowserVerificationStatus.Verified));
        var approval = new BrowserApprovalGrant("approval-1", true, "test", DateTimeOffset.UtcNow);

        var result = await CreateRunner(dispatcher).ExecuteAsync(new BrowserExecutorStepRequest("corr-4", action, Capabilities(riskLimit: BrowserRiskClass.Critical), target, approval));

        Assert.IsTrue(result.Success);
        Assert.AreEqual(BrowserExecutorStepState.Verified, result.FinalState);
        Assert.AreEqual(1, dispatcher.ExecuteCount);
    }

    [TestMethod]
    public async Task ExecutedButVerificationUncertainDoesNotMarkSuccess()
    {
        var target = CreateTarget();
        var action = CreateAction(target, BrowserActionType.Click, BrowserRiskClass.Low);
        var dispatcher = new StubBrowserDispatcher(
            new ChromeCdpActionResult(action.ActionId, true, "Executed", Evidence(action)),
            Verification(action, BrowserVerificationStatus.Uncertain, "cannot prove outcome"));

        var result = await CreateRunner(dispatcher).ExecuteAsync(new BrowserExecutorStepRequest("corr-5", action, Capabilities(), target));

        Assert.IsFalse(result.Success);
        Assert.AreEqual(BrowserExecutorStepState.Uncertain, result.FinalState);
        Assert.AreEqual(BrowserRuntimeErrorCode.VerificationUncertain, result.ErrorCode);
        Assert.IsFalse(result.Verification!.AllowsStepDone());
    }

    [TestMethod]
    public async Task ExecutedButVerificationFailedDoesNotMarkSuccess()
    {
        var target = CreateTarget();
        var action = CreateAction(target, BrowserActionType.Click, BrowserRiskClass.Low);
        var dispatcher = new StubBrowserDispatcher(
            new ChromeCdpActionResult(action.ActionId, true, "Executed", Evidence(action)),
            Verification(action, BrowserVerificationStatus.Failed, "expected text missing"));

        var result = await CreateRunner(dispatcher).ExecuteAsync(new BrowserExecutorStepRequest("corr-6", action, Capabilities(), target));

        Assert.IsFalse(result.Success);
        Assert.AreEqual(BrowserExecutorStepState.Failed, result.FinalState);
        Assert.AreEqual(BrowserRuntimeErrorCode.VerificationFailed, result.ErrorCode);
    }

    [TestMethod]
    public async Task StaleTargetBlocksBeforeExecution()
    {
        var actionTarget = CreateTarget(generation: 1);
        var currentTarget = CreateTarget(generation: 2);
        var action = CreateAction(actionTarget, BrowserActionType.Click, BrowserRiskClass.Low);
        var dispatcher = new StubBrowserDispatcher(
            new ChromeCdpActionResult(action.ActionId, true, "Executed", Evidence(action)),
            Verification(action, BrowserVerificationStatus.Verified));

        var result = await CreateRunner(dispatcher).ExecuteAsync(new BrowserExecutorStepRequest("corr-7", action, Capabilities(), currentTarget));

        Assert.IsFalse(result.Success);
        Assert.AreEqual(BrowserExecutorStepState.Blocked, result.FinalState);
        Assert.AreEqual(BrowserRuntimeErrorCode.TargetStale, result.ErrorCode);
        Assert.AreEqual(0, dispatcher.ExecuteCount);
    }

    [TestMethod]
    public async Task RuntimeErrorMapsToBlockedOrTimedOut()
    {
        var target = CreateTarget();
        var action = CreateAction(target, BrowserActionType.Click, BrowserRiskClass.Low);
        var dispatcher = new StubBrowserDispatcher(
            new ChromeCdpActionResult(action.ActionId, false, "ActionTimeout", Evidence(action), "timeout while clicking"),
            Verification(action, BrowserVerificationStatus.Verified));

        var result = await CreateRunner(dispatcher).ExecuteAsync(new BrowserExecutorStepRequest("corr-8", action, Capabilities(), target));

        Assert.IsFalse(result.Success);
        Assert.AreEqual(BrowserExecutorStepState.TimedOut, result.FinalState);
        Assert.AreEqual(BrowserRuntimeErrorCode.ActionTimeout, result.ErrorCode);
    }

    [TestMethod]
    public async Task CancellationMapsToTimedOutAndDoesNotVerify()
    {
        var target = CreateTarget();
        var action = CreateAction(target, BrowserActionType.Click, BrowserRiskClass.Low);
        var dispatcher = new CancellingBrowserDispatcher();

        var result = await CreateRunner(dispatcher).ExecuteAsync(new BrowserExecutorStepRequest("corr-9", action, Capabilities(), target));

        Assert.IsFalse(result.Success);
        Assert.AreEqual(BrowserExecutorStepState.TimedOut, result.FinalState);
        Assert.AreEqual(BrowserRuntimeErrorCode.ActionTimeout, result.ErrorCode);
        Assert.AreEqual(0, dispatcher.VerifyCount);
    }

    [TestMethod]
    public async Task FixtureCdpStepRunsUnderFsmAdapterAndCleansUp()
    {
        var browser = ChromeCdpBrowserLauncher.FindBrowserExecutable();
        if (browser is null)
            Assert.Inconclusive("Chrome/Edge executable is not available in this environment.");

        await using var session = await new ChromeCdpBrowserLauncher().LaunchAsync(new ChromeCdpOptions(browser));
        await using var page = await session.CreatePageAsync(FixtureUri());
        var target = await page.GetCurrentTargetContextAsync("fsm-fixture");
        var action = CreateAction(target, BrowserActionType.TypeText, BrowserRiskClass.Low, selector: "#nameInput", input: "FSM", expected: new BrowserExpectedOutcome("input contains FSM", null, null, "FSM"));
        var runner = CreateRunner(new ChromeCdpBrowserActionDispatcher(page));

        var result = await runner.ExecuteAsync(new BrowserExecutorStepRequest("corr-fixture", action, Capabilities(), target));

        Assert.IsTrue(result.Success, result.Message);
        Assert.AreEqual(BrowserExecutorStepState.Verified, result.FinalState);
        Assert.IsTrue(result.Evidence.HasEvidenceBeforeSuccess);
    }

    private static BrowserExecutorStepRunner CreateRunner(StubBrowserDispatcher dispatcher) =>
        new(new BrowserExecutorPolicyGate(), dispatcher, dispatcher);

    private static BrowserExecutorStepRunner CreateRunner(CancellingBrowserDispatcher dispatcher) =>
        new(new BrowserExecutorPolicyGate(), dispatcher, dispatcher);

    private static BrowserExecutorStepRunner CreateRunner(ChromeCdpBrowserActionDispatcher dispatcher) =>
        new(new BrowserExecutorPolicyGate(), dispatcher, dispatcher);

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

    private static BrowserAction CreateAction(
        BrowserTargetContext target,
        BrowserActionType type,
        BrowserRiskClass risk,
        string idempotencyKey = "idem-1",
        bool requiresApproval = false,
        string selector = "#applyButton",
        string? input = null,
        BrowserExpectedOutcome? expected = null) =>
        new(
            ActionId: "action-1",
            IdempotencyKey: idempotencyKey,
            RunId: "run-1",
            StepId: "step-1",
            TargetContext: target,
            FrameId: target.FrameId,
            ActionType: type,
            Target: new BrowserActionTarget("candidate-1", selector, selector, null),
            Input: input is null ? null : new BrowserActionInput(input, input, HasModifyingValue: true),
            ExpectedOutcome: expected ?? new BrowserExpectedOutcome("expected fixture result", null, "Result", null),
            RiskClass: risk,
            TimeoutMs: 8000,
            RequiresApproval: requiresApproval,
            CreatedAtUtc: DateTimeOffset.UtcNow);

    private static BrowserTargetContext CreateTarget(long generation = 1)
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
            Url: new Uri("file:///fixture/basic-form.html"),
            Title: "Fixture",
            Generation: generation,
            LivenessToken: BrowserTargetContext.CreateLivenessToken(targetId, frameId, generation),
            ObservedAtUtc: DateTimeOffset.UtcNow,
            IsActive: null,
            IsVisible: null,
            IsUserFacing: null,
            ReadyState: "complete",
            Source: BrowserTargetSource.Cdp);
    }

    private static BrowserEvidence Evidence(BrowserAction action) =>
        new(
            EvidenceId: Guid.NewGuid().ToString("N"),
            RunId: action.RunId,
            StepId: action.StepId,
            ActionId: action.ActionId,
            VerificationId: null,
            TargetContext: action.TargetContext,
            EvidenceType: BrowserEvidenceType.CdpEvent,
            CreatedAtUtc: DateTimeOffset.UtcNow,
            Summary: "test action evidence",
            PayloadRef: null,
            InlinePayload: null,
            RedactionApplied: true,
            SensitivityLevel: BrowserSensitivityLevel.Low);

    private static BrowserVerification Verification(BrowserAction action, BrowserVerificationStatus status, string? failureReason = null) =>
        new(
            VerificationId: Guid.NewGuid().ToString("N"),
            RunId: action.RunId,
            StepId: action.StepId,
            ActionId: action.ActionId,
            TargetContext: action.TargetContext,
            ExpectedOutcome: action.ExpectedOutcome,
            PreObservationId: "before",
            PostObservationId: "after",
            Status: status,
            Confidence: status == BrowserVerificationStatus.Verified ? 0.95 : 0.2,
            EvidenceRefs: status == BrowserVerificationStatus.Verified ? ["verification-evidence"] : [],
            FailureReason: failureReason,
            VerifiedAtUtc: DateTimeOffset.UtcNow);

    private static Uri FixtureUri()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "OneBrain.slnx")))
            dir = dir.Parent;

        Assert.IsNotNull(dir, "repo root not found");
        return new Uri(Path.Combine(dir.FullName, "tests", "fixtures", "browser-executor", "basic-form.html"));
    }

    private sealed class StubBrowserDispatcher(ChromeCdpActionResult actionResult, BrowserVerification verification) : IBrowserActionDispatcher, IBrowserVerificationDispatcher
    {
        public int ExecuteCount { get; private set; }
        public int VerifyCount { get; private set; }

        public Task<ChromeCdpActionResult> ExecuteActionAsync(BrowserAction action, CancellationToken cancellationToken)
        {
            ExecuteCount++;
            return Task.FromResult(actionResult);
        }

        public Task<BrowserVerification> VerifyAsync(BrowserAction action, CancellationToken cancellationToken)
        {
            VerifyCount++;
            return Task.FromResult(verification);
        }
    }

    private sealed class CancellingBrowserDispatcher : IBrowserActionDispatcher, IBrowserVerificationDispatcher
    {
        public int VerifyCount { get; private set; }

        public Task<ChromeCdpActionResult> ExecuteActionAsync(BrowserAction action, CancellationToken cancellationToken) =>
            throw new OperationCanceledException();

        public Task<BrowserVerification> VerifyAsync(BrowserAction action, CancellationToken cancellationToken)
        {
            VerifyCount++;
            return Task.FromResult(Verification(action, BrowserVerificationStatus.Verified));
        }
    }
}
