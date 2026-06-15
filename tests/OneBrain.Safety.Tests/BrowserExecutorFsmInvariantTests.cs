using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
public sealed class BrowserExecutorFsmInvariantTests
{
    [TestMethod]
    public async Task BrowserExecutorFsmInvariantUncertainFailedAndExecutedAreNotSuccess()
    {
        var target = Target();
        var action = Action(target, BrowserActionType.Click, BrowserRiskClass.Low);

        var uncertain = await Runner(new ChromeCdpActionResult(action.ActionId, true, "Executed", Evidence(action)), Verification(action, BrowserVerificationStatus.Uncertain, "uncertain")).ExecuteAsync(Request(action, target));
        var failed = await Runner(new ChromeCdpActionResult(action.ActionId, true, "Executed", Evidence(action)), Verification(action, BrowserVerificationStatus.Failed, "failed")).ExecuteAsync(Request(action, target));
        var proofless = await Runner(new ChromeCdpActionResult(action.ActionId, true, "Executed", Evidence(action)), Verification(action, BrowserVerificationStatus.Verified, proofRefs: [])).ExecuteAsync(Request(action, target));

        Assert.IsFalse(uncertain.Success);
        Assert.AreEqual(BrowserExecutorStepState.Uncertain, uncertain.FinalState);
        Assert.IsFalse(failed.Success);
        Assert.AreEqual(BrowserExecutorStepState.Failed, failed.FinalState);
        Assert.IsFalse(proofless.Success);
        Assert.AreNotEqual(BrowserExecutorStepState.Verified, proofless.FinalState);
    }

    [TestMethod]
    public async Task BrowserExecutorFsmInvariantBlockedRequiresHumanAndCriticalDoNotExecute()
    {
        var target = Target();
        var missingIdempotency = Action(target, BrowserActionType.Click, BrowserRiskClass.Low, idempotencyKey: "");
        var critical = Action(target, BrowserActionType.Click, BrowserRiskClass.Critical);

        var blockedDispatcher = new StubDispatcher(new ChromeCdpActionResult(missingIdempotency.ActionId, true, "Executed", Evidence(missingIdempotency)), Verification(missingIdempotency, BrowserVerificationStatus.Verified));
        var blocked = await new BrowserExecutorStepRunner(new BrowserExecutorPolicyGate(), blockedDispatcher, blockedDispatcher).ExecuteAsync(Request(missingIdempotency, target));

        var criticalDispatcher = new StubDispatcher(new ChromeCdpActionResult(critical.ActionId, true, "Executed", Evidence(critical)), Verification(critical, BrowserVerificationStatus.Verified));
        var requiresHuman = await new BrowserExecutorStepRunner(new BrowserExecutorPolicyGate(), criticalDispatcher, criticalDispatcher).ExecuteAsync(Request(critical, target));

        Assert.IsFalse(blocked.Success);
        Assert.AreEqual(BrowserExecutorStepState.Blocked, blocked.FinalState);
        Assert.AreEqual(0, blockedDispatcher.ExecuteCount);
        Assert.IsFalse(requiresHuman.Success);
        Assert.AreEqual(BrowserExecutorStepState.ApprovalRequired, requiresHuman.FinalState);
        Assert.AreEqual(0, criticalDispatcher.ExecuteCount);
    }

    [TestMethod]
    public async Task BrowserExecutorFsmInvariantTargetStaleBlocksModifyingAction()
    {
        var actionTarget = Target(generation: 1);
        var currentTarget = Target(generation: 2);
        var action = Action(actionTarget, BrowserActionType.Click, BrowserRiskClass.Low);
        var dispatcher = new StubDispatcher(new ChromeCdpActionResult(action.ActionId, true, "Executed", Evidence(action)), Verification(action, BrowserVerificationStatus.Verified));

        var result = await new BrowserExecutorStepRunner(new BrowserExecutorPolicyGate(), dispatcher, dispatcher).ExecuteAsync(Request(action, currentTarget));

        Assert.IsFalse(result.Success);
        Assert.AreEqual(BrowserRuntimeErrorCode.TargetStale, result.ErrorCode);
        Assert.AreEqual(0, dispatcher.ExecuteCount);
    }

    [TestMethod]
    public async Task BrowserExecutorFsmInvariantSuccessRequiresVerifiedProofAndEvidence()
    {
        var target = Target();
        var action = Action(target, BrowserActionType.Click, BrowserRiskClass.Low);
        var runner = Runner(new ChromeCdpActionResult(action.ActionId, true, "Executed", Evidence(action)), Verification(action, BrowserVerificationStatus.Verified));

        var result = await runner.ExecuteAsync(Request(action, target));

        Assert.IsTrue(result.Success);
        Assert.AreEqual(BrowserExecutorStepState.Verified, result.FinalState);
        Assert.IsTrue(result.Verification!.HasSemanticProof);
        Assert.IsTrue(result.Evidence.HasEvidenceBeforeSuccess);
    }

    private static BrowserExecutorStepRunner Runner(ChromeCdpActionResult result, BrowserVerification verification)
    {
        var dispatcher = new StubDispatcher(result, verification);
        return new BrowserExecutorStepRunner(new BrowserExecutorPolicyGate(), dispatcher, dispatcher);
    }

    private static BrowserExecutorStepRequest Request(BrowserAction action, BrowserTargetContext current) =>
        new("corr-invariant", action, Capabilities(), current);

    private static BrowserExecutorCapabilities Capabilities() =>
        new(
            ExecutorId: "invariant-test",
            ExecutorKind: BrowserExecutorKind.Cdp,
            Capabilities: new HashSet<BrowserActionType> { BrowserActionType.Click, BrowserActionType.Read },
            RiskLimit: BrowserRiskClass.Critical,
            SupportsTrustedInput: true,
            SupportsDomSnapshot: true,
            SupportsAccessibilitySnapshot: false,
            SupportsScreenshots: false,
            SupportsFrames: true,
            SupportsDownloads: false,
            SupportsFileUpload: false,
            RequiresBrowserLaunch: false,
            RequiresRemoteDebugging: false,
            CanAttachExistingBrowser: false,
            CanUsePersistentProfile: false,
            CanUseRealUserProfile: false);

    private static BrowserAction Action(BrowserTargetContext target, BrowserActionType type, BrowserRiskClass risk, string idempotencyKey = "idem-invariant") =>
        new(
            ActionId: "action-" + Guid.NewGuid().ToString("N"),
            IdempotencyKey: idempotencyKey,
            RunId: target.RunId,
            StepId: "step",
            TargetContext: target,
            FrameId: target.FrameId,
            ActionType: type,
            Target: new BrowserActionTarget("candidate", "#button", "button", null),
            Input: null,
            ExpectedOutcome: new BrowserExpectedOutcome("verified proof", null, "verified", null),
            RiskClass: risk,
            TimeoutMs: 8000,
            RequiresApproval: risk == BrowserRiskClass.Critical,
            CreatedAtUtc: DateTimeOffset.UtcNow);

    private static BrowserTargetContext Target(long generation = 1)
    {
        const string targetId = "target-invariant";
        return new BrowserTargetContext(
            RunId: "run-invariant",
            BrowserId: "browser",
            BrowserSessionId: "session",
            BrowserContextId: null,
            WindowId: null,
            TargetId: targetId,
            PageId: targetId,
            TabId: null,
            FrameId: "main",
            ParentFrameId: null,
            Url: new Uri("https://example.com/"),
            Title: "Example",
            Generation: generation,
            LivenessToken: BrowserTargetContext.CreateLivenessToken(targetId, "main", generation),
            ObservedAtUtc: DateTimeOffset.UtcNow,
            IsActive: null,
            IsVisible: null,
            IsUserFacing: null,
            ReadyState: "complete",
            Source: BrowserTargetSource.Cdp);
    }

    private static BrowserEvidence Evidence(BrowserAction action) =>
        new(
            EvidenceId: "evidence-" + Guid.NewGuid().ToString("N"),
            RunId: action.RunId,
            StepId: action.StepId,
            ActionId: action.ActionId,
            VerificationId: null,
            TargetContext: action.TargetContext,
            EvidenceType: BrowserEvidenceType.CdpEvent,
            CreatedAtUtc: DateTimeOffset.UtcNow,
            Summary: "executed",
            PayloadRef: null,
            InlinePayload: null,
            RedactionApplied: true,
            SensitivityLevel: BrowserSensitivityLevel.Low);

    private static BrowserVerification Verification(BrowserAction action, BrowserVerificationStatus status, string? failureReason = null, IReadOnlyList<string>? proofRefs = null) =>
        new(
            VerificationId: "verification-" + Guid.NewGuid().ToString("N"),
            RunId: action.RunId,
            StepId: action.StepId,
            ActionId: action.ActionId,
            TargetContext: action.TargetContext,
            ExpectedOutcome: action.ExpectedOutcome,
            PreObservationId: "before",
            PostObservationId: "after",
            Status: status,
            Confidence: status == BrowserVerificationStatus.Verified ? 0.95 : 0.2,
            EvidenceRefs: status == BrowserVerificationStatus.Verified ? ["evidence-verification"] : [],
            FailureReason: failureReason,
            VerifiedAtUtc: DateTimeOffset.UtcNow,
            ProofRefs: proofRefs ?? (status == BrowserVerificationStatus.Verified ? ["proof-verification"] : []));

    private sealed class StubDispatcher(ChromeCdpActionResult result, BrowserVerification verification) : IBrowserActionDispatcher, IBrowserVerificationDispatcher
    {
        public int ExecuteCount { get; private set; }
        public Task<ChromeCdpActionResult> ExecuteActionAsync(BrowserAction action, CancellationToken cancellationToken)
        {
            ExecuteCount++;
            return Task.FromResult(result);
        }

        public Task<BrowserVerification> VerifyAsync(BrowserAction action, CancellationToken cancellationToken) => Task.FromResult(verification);
    }
}
