using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
public sealed class BrowserHumanHandoffTests
{
    [TestMethod]
    public void BrowserHumanHandoffResumeDoesNotOccurIfTargetStale()
    {
        var target = Target(generation: 1);
        var request = Request(target);
        var session = Session(BrowserSessionState.Active);
        var observed = Target(generation: 2);

        var decision = new BrowserHumanHandoffCoordinator().TryResume(request, BrowserHumanHandoffStatus.UserCompleted, observed, session, Verified(target), DateTimeOffset.UtcNow);

        Assert.IsFalse(decision.CanResume);
        Assert.IsFalse(decision.Success);
        StringAssert.Contains(decision.Reason, "stale");
    }

    [TestMethod]
    public void BrowserHumanHandoffResumeDoesNotOccurIfSessionExpiredOrDisposed()
    {
        var target = Target();
        var request = Request(target);
        var expired = Session(BrowserSessionState.Expired);
        var disposed = Session(BrowserSessionState.Disposed);
        var coordinator = new BrowserHumanHandoffCoordinator();

        var expiredDecision = coordinator.TryResume(request, BrowserHumanHandoffStatus.UserCompleted, target, expired, Verified(target), DateTimeOffset.UtcNow);
        var disposedDecision = coordinator.TryResume(request, BrowserHumanHandoffStatus.UserCompleted, target, disposed, Verified(target), DateTimeOffset.UtcNow);

        Assert.IsFalse(expiredDecision.CanResume);
        Assert.IsFalse(disposedDecision.CanResume);
    }

    [TestMethod]
    public void BrowserHumanHandoffResumeDoesNotOccurIfUserCancelled()
    {
        var target = Target();
        var decision = new BrowserHumanHandoffCoordinator().TryResume(Request(target), BrowserHumanHandoffStatus.UserCancelled, target, Session(BrowserSessionState.Active), Verified(target), DateTimeOffset.UtcNow);

        Assert.IsFalse(decision.CanResume);
        Assert.AreEqual(BrowserHumanHandoffStatus.UserCancelled, decision.Status);
    }

    [TestMethod]
    public void BrowserHumanHandoffUserCompletedAloneIsNotSuccess()
    {
        var target = Target();
        var coordinator = new BrowserHumanHandoffCoordinator();

        var noVerification = coordinator.TryResume(Request(target), BrowserHumanHandoffStatus.UserCompleted, target, Session(BrowserSessionState.Active), null, DateTimeOffset.UtcNow);
        var uncertain = coordinator.TryResume(Request(target), BrowserHumanHandoffStatus.UserCompleted, target, Session(BrowserSessionState.Active), Verification(target, BrowserVerificationStatus.Uncertain), DateTimeOffset.UtcNow);

        Assert.IsFalse(noVerification.Success);
        Assert.IsFalse(uncertain.Success);
        Assert.IsFalse(noVerification.CanResume);
        Assert.IsFalse(uncertain.CanResume);
    }

    [TestMethod]
    public void BrowserHumanHandoffCanResumeAfterUserCompletedReObservationAndVerification()
    {
        var target = Target();
        var decision = new BrowserHumanHandoffCoordinator().TryResume(Request(target), BrowserHumanHandoffStatus.UserCompleted, target, Session(BrowserSessionState.Active), Verified(target), DateTimeOffset.UtcNow);

        Assert.IsTrue(decision.CanResume);
        Assert.IsTrue(decision.Success);
        Assert.AreEqual(BrowserHumanHandoffStatus.Resumed, decision.Status);
        Assert.IsTrue(decision.ProofRefs.Count > 0);
        Assert.IsTrue(decision.EvidenceRefs.Count > 0);
    }

    [TestMethod]
    public void BrowserHumanHandoffExtensionRelayCannotCompleteHandoffByItself()
    {
        var target = Target(source: BrowserTargetSource.ExtensionRelay);
        var request = Request(target);

        var decision = new BrowserHumanHandoffCoordinator().TryResume(request, BrowserHumanHandoffStatus.UserCompleted, target, Session(BrowserSessionState.Active), null, DateTimeOffset.UtcNow);

        Assert.IsFalse(decision.Success);
        Assert.IsFalse(decision.CanResume);
        StringAssert.Contains(decision.Reason, "verification");
    }

    private static BrowserHumanHandoffRequest Request(BrowserTargetContext target)
    {
        var action = new BrowserAction(
            ActionId: "action-1",
            IdempotencyKey: "idem-1",
            RunId: "run-1",
            StepId: "step-1",
            TargetContext: target,
            FrameId: target.FrameId,
            ActionType: BrowserActionType.Click,
            Target: new BrowserActionTarget("login", "#loginSubmit", "Ingresar", null),
            Input: null,
            ExpectedOutcome: new BrowserExpectedOutcome("manual login complete", null, "Home", null),
            RiskClass: BrowserRiskClass.Critical,
            TimeoutMs: 8000,
            RequiresApproval: true,
            CreatedAtUtc: DateTimeOffset.UtcNow);
        var boundary = new BrowserCredentialBoundaryDetector().EvaluateAction(action);
        return new BrowserHumanHandoffCoordinator().CreateRequest(boundary, action, "corr-1", "profile-1", "session-1");
    }

    private static BrowserSessionDescriptor Session(BrowserSessionState state) =>
        new(
            ManagedBrowserSessionId.New(),
            BrowserProfileId.New(),
            Owner: "test",
            CorrelationId: "corr-1",
            State: state,
            CreatedAtUtc: DateTimeOffset.UtcNow,
            ExpiresAtUtc: DateTimeOffset.UtcNow.AddMinutes(10),
            CleanupPolicy: BrowserProfileCleanupPolicy.DeleteOnClose);

    private static BrowserVerification Verified(BrowserTargetContext target) =>
        Verification(target, BrowserVerificationStatus.Verified);

    private static BrowserVerification Verification(BrowserTargetContext target, BrowserVerificationStatus status) =>
        new(
            VerificationId: "verification-1",
            RunId: "run-1",
            StepId: "step-1",
            ActionId: "action-1",
            TargetContext: target,
            ExpectedOutcome: new BrowserExpectedOutcome("manual step verified", null, "Home", null),
            PreObservationId: "before",
            PostObservationId: "after",
            Status: status,
            Confidence: status == BrowserVerificationStatus.Verified ? 0.95 : 0.4,
            EvidenceRefs: status == BrowserVerificationStatus.Verified ? ["evidence-after"] : [],
            FailureReason: status == BrowserVerificationStatus.Verified ? null : "not verified",
            VerifiedAtUtc: DateTimeOffset.UtcNow,
            ProofRefs: status == BrowserVerificationStatus.Verified ? ["proof-after"] : []);

    private static BrowserTargetContext Target(long generation = 1, BrowserTargetSource source = BrowserTargetSource.Fixture)
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
            Title: "Login fixture",
            Generation: generation,
            LivenessToken: BrowserTargetContext.CreateLivenessToken(targetId, frameId, generation),
            ObservedAtUtc: DateTimeOffset.UtcNow,
            IsActive: true,
            IsVisible: true,
            IsUserFacing: true,
            ReadyState: "complete",
            Source: source);
    }
}
