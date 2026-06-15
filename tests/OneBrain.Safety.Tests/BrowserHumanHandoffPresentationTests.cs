using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
public sealed class BrowserHumanHandoffPresentationTests
{
    [TestMethod]
    public void HandoffPresentationIsCreatedFromRequest()
    {
        var request = Request(BrowserHumanHandoffReason.PasswordRequired);
        var presentation = new BrowserHumanHandoffCompanionAdapter().CreatePresentation(request);

        Assert.AreEqual(request.RequestId, presentation.HandoffId);
        Assert.AreEqual(request.Context.RunId, presentation.RunId);
        Assert.AreEqual(BrowserHumanHandoffDisplayState.WaitingForUser, presentation.DisplayState);
        Assert.IsFalse(presentation.Authoritative);
        Assert.IsTrue(presentation.Validate().IsValid);
        CollectionAssert.Contains(presentation.AllowedOptions.ToList(), BrowserHumanHandoffUserOption.ContinueAfterUserAction);
    }

    [TestMethod]
    public void HandoffPresentationContainsReasonSpecificInstructions()
    {
        var adapter = new BrowserHumanHandoffCompanionAdapter();

        StringAssert.Contains(adapter.CreatePresentation(Request(BrowserHumanHandoffReason.PasswordRequired)).Instruction, "contraseña/login");
        StringAssert.Contains(adapter.CreatePresentation(Request(BrowserHumanHandoffReason.CaptchaRequired)).Instruction, "no intentará resolverlo");
        StringAssert.Contains(adapter.CreatePresentation(Request(BrowserHumanHandoffReason.TwoFactorRequired)).Instruction, "doble factor");
    }

    [TestMethod]
    public void HandoffPresentationRedactsSecrets()
    {
        var request = Request(BrowserHumanHandoffReason.PasswordRequired, title: "Login password=abc123", url: "https://local.test/login?token=abc123&password=secret");
        var presentation = new BrowserHumanHandoffCompanionAdapter().CreatePresentation(request);

        Assert.IsFalse(presentation.SafeTitle.Contains("abc123", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(presentation.SafeUrl.Contains("abc123", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(presentation.SafeUrl.Contains("secret", StringComparison.OrdinalIgnoreCase));
        Assert.IsTrue(presentation.RedactionApplied);
        Assert.IsTrue(presentation.Validate().IsValid);
    }

    [TestMethod]
    public void CompanionUserCompletedDoesNotMarkSuccessAndIsNonAuthoritative()
    {
        var envelope = new BrowserHumanHandoffCompanionAdapter().UserCompleted(Request(BrowserHumanHandoffReason.PasswordRequired));

        Assert.AreEqual("handoff.userCompleted", envelope.MessageType);
        Assert.IsFalse(envelope.Event.Authoritative);
        Assert.IsFalse(envelope.Event.CanMarkSuccess);
        Assert.AreEqual(BrowserHumanHandoffDisplayState.UserCompletedPendingVerification, envelope.Presentation!.DisplayState);
        Assert.IsTrue(envelope.Event.Validate().IsValid);
    }

    [TestMethod]
    public void ResumeRequiresReObservationAndVerification()
    {
        var request = Request(BrowserHumanHandoffReason.PasswordRequired);
        var adapter = new BrowserHumanHandoffCompanionAdapter();
        var decision = new BrowserHumanHandoffResumeDecision(false, false, BrowserHumanHandoffStatus.UserCompleted, "user completed is not success without re-observation and verification", [], []);

        var envelope = adapter.ResumeResult(request, decision, null);

        Assert.AreEqual("handoff.resumeRejected", envelope.MessageType);
        Assert.IsFalse(envelope.Event.CanMarkSuccess);
        Assert.AreEqual(BrowserHumanHandoffDisplayState.Failed, envelope.Presentation!.DisplayState);
    }

    [TestMethod]
    public void ResumeRejectedIfTargetStaleOrVerificationUncertain()
    {
        var request = Request(BrowserHumanHandoffReason.PasswordRequired);
        var adapter = new BrowserHumanHandoffCompanionAdapter();

        var stale = adapter.ResumeResult(request, new BrowserHumanHandoffResumeDecision(false, false, BrowserHumanHandoffStatus.Failed, "target context is stale", [], []), null);
        var uncertain = adapter.ResumeResult(request, new BrowserHumanHandoffResumeDecision(false, false, BrowserHumanHandoffStatus.UserCompleted, "verification uncertain", [], []), Verification(BrowserVerificationStatus.Uncertain));

        Assert.AreEqual("handoff.resumeRejected", stale.MessageType);
        Assert.AreEqual("handoff.resumeRejected", uncertain.MessageType);
        Assert.IsFalse(stale.Event.CanMarkSuccess);
        Assert.IsFalse(uncertain.Event.CanMarkSuccess);
    }

    [TestMethod]
    public void ResumeVerifiedOnlyWithProofAndEvidence()
    {
        var request = Request(BrowserHumanHandoffReason.PasswordRequired);
        var verification = Verification(BrowserVerificationStatus.Verified);
        var decision = new BrowserHumanHandoffResumeDecision(true, true, BrowserHumanHandoffStatus.Resumed, "verified", ["evidence-1"], ["proof-1"]);

        var envelope = new BrowserHumanHandoffCompanionAdapter().ResumeResult(request, decision, verification);

        Assert.AreEqual("handoff.resumeVerified", envelope.MessageType);
        Assert.IsTrue(envelope.Event.Authoritative);
        Assert.IsTrue(envelope.Event.CanMarkSuccess);
        Assert.AreEqual(BrowserHumanHandoffDisplayState.Resumed, envelope.Presentation!.DisplayState);
    }

    [TestMethod]
    public void CancelledExpiredDisconnectedAndRelayAcceptedAreNotDone()
    {
        var request = Request(BrowserHumanHandoffReason.PasswordRequired);
        var adapter = new BrowserHumanHandoffCompanionAdapter();

        var cancelled = adapter.Cancelled(request);
        var expired = adapter.Expired(request);
        var disconnected = adapter.Disconnected(request);
        var created = adapter.Created(request);

        Assert.IsFalse(cancelled.Event.CanMarkSuccess);
        Assert.IsFalse(expired.Event.CanMarkSuccess);
        Assert.IsFalse(disconnected.Event.CanMarkSuccess);
        Assert.IsTrue(created.RelayAcceptedIsNotVerified);
        Assert.AreEqual(BrowserHumanHandoffDisplayState.Disconnected, disconnected.Presentation!.DisplayState);
    }

    [TestMethod]
    public void ServiceWorkerOrSidepanelCannotSendVerifiedAuthoritative()
    {
        var request = Request(BrowserHumanHandoffReason.PasswordRequired);
        var uiEvent = new BrowserHumanHandoffUiEvent(
            BrowserHumanHandoffUiEventKind.HandoffResumeVerified,
            request.RequestId,
            request.Context.RunId,
            request.Context.ActionId,
            request.Context.CorrelationId,
            BrowserHumanHandoffCompanionAdapter.RuntimeKind,
            BrowserHumanHandoffCompanionAdapter.CompanionSource,
            Authoritative: false,
            VerificationStatus: BrowserVerificationStatus.Verified,
            EvidenceRefs: ["evidence"],
            ProofRefs: ["proof"],
            Redacted: true,
            Diagnostics: "sidepanel tried verified");

        Assert.IsFalse(uiEvent.CanMarkSuccess);
        Assert.IsTrue(uiEvent.Validate().IsValid);
    }

    private static BrowserHumanHandoffRequest Request(BrowserHumanHandoffReason reason, string title = "Login fixture", string url = "https://local.test/login")
    {
        var target = Target(title, url);
        return new BrowserHumanHandoffRequest(
            RequestId: "handoff-1",
            Status: BrowserHumanHandoffStatus.Created,
            Reason: reason,
            Context: new BrowserHumanHandoffContext("run-1", "action-1", "corr-1", "profile-1", "session-1", target, target.FrameId, BrowserCredentialRedactor.Redact(url), BrowserCredentialRedactor.Redact(title), true),
            ResumeToken: new BrowserHumanHandoffResumeToken("resume-1", "handoff-1", "corr-1", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddMinutes(10)),
            Instruction: new BrowserHumanHandoffInstruction("manual intervention required", ["continue", "cancel"], ["verified after observe"]),
            CreatedAtUtc: DateTimeOffset.UtcNow,
            ExpiresAtUtc: DateTimeOffset.UtcNow.AddMinutes(10),
            RedactionApplied: true,
            EvidenceRefs: ["handoff-created"],
            ProofRefs: ["proof-handoff-created"]);
    }

    private static BrowserVerification Verification(BrowserVerificationStatus status) =>
        new(
            VerificationId: "verification-1",
            RunId: "run-1",
            StepId: "step-1",
            ActionId: "action-1",
            TargetContext: Target(),
            ExpectedOutcome: new BrowserExpectedOutcome("handoff verified", null, "home", null),
            PreObservationId: "before",
            PostObservationId: "after",
            Status: status,
            Confidence: status == BrowserVerificationStatus.Verified ? 0.95 : 0.4,
            EvidenceRefs: status == BrowserVerificationStatus.Verified ? ["evidence-verified"] : [],
            FailureReason: status == BrowserVerificationStatus.Verified ? null : "uncertain",
            VerifiedAtUtc: DateTimeOffset.UtcNow,
            ProofRefs: status == BrowserVerificationStatus.Verified ? ["proof-verified"] : []);

    private static BrowserTargetContext Target(string title = "Login fixture", string url = "https://local.test/login")
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
            Url: new Uri(url),
            Title: title,
            Generation: 1,
            LivenessToken: BrowserTargetContext.CreateLivenessToken(targetId, frameId, 1),
            ObservedAtUtc: DateTimeOffset.UtcNow,
            IsActive: true,
            IsVisible: true,
            IsUserFacing: true,
            ReadyState: "complete",
            Source: BrowserTargetSource.Fixture);
    }
}
