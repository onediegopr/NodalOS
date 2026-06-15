using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
public sealed class BrowserExecutorContractsTests
{
    [TestMethod]
    public void TargetContextValidatesLivenessAndStaleGeneration()
    {
        var target = CreateTarget(generation: 1);
        var next = CreateTarget(generation: 2);

        Assert.IsTrue(target.Validate().IsValid);
        Assert.IsTrue(target.IsStaleComparedTo(next));
        Assert.AreNotEqual(target.LivenessToken, next.LivenessToken);

        var subframe = CreateTarget(frameId: "frame-child", parentFrameId: "frame-main");
        Assert.IsTrue(subframe.Validate().IsValid);
        Assert.AreEqual("frame-main", subframe.ParentFrameId);
    }

    [TestMethod]
    public void TargetContextRejectsMissingCriticalIdsAndSecretLikeValues()
    {
        var invalid = CreateTarget(targetId: "", title: "authorization: bearer value");

        var result = invalid.Validate();

        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(error => error.Contains("TargetId", StringComparison.Ordinal)));
        Assert.IsTrue(result.Errors.Any(error => error.Contains("secrets", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    public void ObservationRedactsSecretsAndEnforcesPayloadLimits()
    {
        var observation = CreateObservation(
            visibleText: "Search page password=secret",
            actionables:
            [
                new ActionableElement(
                    "candidate-1",
                    "frame-main",
                    "textbox",
                    "input",
                    "api_key=secret",
                    "Search",
                    "Search",
                    ["input[name=q]"],
                    null,
                    IsVisible: true,
                    IsEnabled: true,
                    RiskHints: [],
                    Confidence: 0.94)
            ]);

        Assert.IsFalse(observation.Validate().IsValid);

        var redacted = observation.Redact();
        Assert.IsTrue(redacted.SensitivityRedactionApplied);
        Assert.IsFalse(SecretRedactor.ContainsSecret(redacted.VisibleTextSummary));
        Assert.IsFalse(redacted.ActionableElements[0].ContainsSecret());
    }

    [TestMethod]
    public void ObservationDoesNotRepresentStepDone()
    {
        var properties = typeof(BrowserObservation).GetProperties().Select(property => property.Name).ToList();

        CollectionAssert.DoesNotContain(properties, "Done");
        CollectionAssert.DoesNotContain(properties, "Success");
    }

    [TestMethod]
    public void BrowserActionRequiresIdsIdempotencyTimeoutExpectedOutcomeAndApproval()
    {
        var currentTarget = CreateTarget(generation: 2);
        var staleAction = CreateAction(
            targetContext: CreateTarget(generation: 1),
            riskClass: BrowserRiskClass.Critical,
            requiresApproval: false,
            idempotencyKey: "",
            timeoutMs: 0,
            expectedOutcome: new BrowserExpectedOutcome("", null, null, null));

        var result = staleAction.Validate(currentTarget);

        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(error => error.Contains("IdempotencyKey", StringComparison.Ordinal)));
        Assert.IsTrue(result.Errors.Any(error => error.Contains("Critical", StringComparison.Ordinal)));
        Assert.IsTrue(result.Errors.Any(error => error.Contains("TimeoutMs", StringComparison.Ordinal)));
        Assert.IsTrue(result.Errors.Any(error => error.Contains("ExpectedOutcome", StringComparison.Ordinal)));
        Assert.IsTrue(result.Errors.Any(error => error.Contains("stale", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    public void ReadOnlyActionCannotCarryModifyingInput()
    {
        var action = CreateAction(
            actionType: BrowserActionType.Read,
            riskClass: BrowserRiskClass.ReadOnly,
            input: new BrowserActionInput("typed value", "typed value", HasModifyingValue: true));

        var result = action.Validate();

        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(error => error.Contains("ReadOnly", StringComparison.Ordinal)));
    }

    [TestMethod]
    public void VerificationUncertainIsNotDone()
    {
        var verified = CreateVerification(BrowserVerificationStatus.Verified);
        var uncertain = CreateVerification(BrowserVerificationStatus.Uncertain);
        var failed = CreateVerification(BrowserVerificationStatus.Failed, failureReason: "not observed");
        var skipped = CreateVerification(BrowserVerificationStatus.Skipped);

        Assert.IsTrue(verified.AllowsStepDone());
        Assert.IsFalse(uncertain.AllowsStepDone());
        Assert.IsFalse(failed.AllowsStepDone());
        Assert.IsFalse(skipped.AllowsStepDone());
        Assert.IsTrue(skipped.AllowsStepDone(allowSkippedByPolicy: true));
    }

    [TestMethod]
    public void ActionAttemptDoesNotEqualVerified()
    {
        var action = CreateAction();
        var actionProperties = typeof(BrowserAction).GetProperties().Select(property => property.Name).ToList();

        Assert.IsTrue(action.Validate().IsValid);
        CollectionAssert.DoesNotContain(actionProperties, "Verified");
        CollectionAssert.DoesNotContain(actionProperties, "Success");

        var uncertain = CreateVerification(BrowserVerificationStatus.Uncertain);
        Assert.IsFalse(uncertain.AllowsStepDone());
    }

    [TestMethod]
    public void VerificationRequiresEvidenceOrFailureReason()
    {
        var invalid = CreateVerification(BrowserVerificationStatus.Verified, evidenceRefs: []);

        Assert.IsFalse(invalid.Validate().IsValid);
    }

    [TestMethod]
    public void EvidenceRequiresRedactionAndRejectsSecretPayload()
    {
        var evidence = new BrowserEvidence(
            EvidenceId: "evidence-1",
            RunId: "run-1",
            StepId: "step-1",
            ActionId: "action-1",
            VerificationId: "verification-1",
            TargetContext: CreateTarget(),
            EvidenceType: BrowserEvidenceType.DomSnapshot,
            CreatedAtUtc: DateTimeOffset.UtcNow,
            Summary: "snapshot",
            PayloadRef: null,
            InlinePayload: "cookie: session=value",
            RedactionApplied: false,
            SensitivityLevel: BrowserSensitivityLevel.Secret);

        var result = evidence.Validate();

        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(error => error.Contains("redaction", StringComparison.OrdinalIgnoreCase)));
        Assert.IsTrue(result.Errors.Any(error => error.Contains("secret", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    public void HeartbeatRequiresRoundTripAndDetectsGenerationMismatch()
    {
        var expected = CreateTarget(generation: 1);
        var observed = CreateTarget(generation: 2);

        var socketOnly = new BrowserHeartbeat("heartbeat-1", "session-1", null, null, null, DateTimeOffset.UtcNow, null, BrowserHeartbeatStatus.Alive, "socket open");
        var stale = BrowserHeartbeat.FromTargetComparison("heartbeat-2", "session-1", expected, observed, DateTimeOffset.UtcNow, 10);

        Assert.IsFalse(socketOnly.IsStrongAlive);
        Assert.AreEqual(BrowserHeartbeatStatus.Stale, stale.Status);
        Assert.IsFalse(stale.IsStrongAlive);
    }

    [TestMethod]
    public void IdempotencyRejectsDuplicateAndSameKeyDifferentFingerprint()
    {
        var ledger = new BrowserIdempotencyLedger();
        var now = DateTimeOffset.UtcNow;
        var first = CreateAction(idempotencyKey: "idem-1", targetContext: CreateTarget(generation: 1));
        var duplicate = first;
        var changed = CreateAction(idempotencyKey: "idem-1", targetContext: CreateTarget(generation: 2));

        var firstDecision = ledger.TryBegin(first, now);
        var duplicateDecision = ledger.TryBegin(duplicate, now);
        var changedDecision = ledger.TryBegin(changed, now);
        ledger.Complete("idem-1");
        var completedDecision = ledger.TryBegin(duplicate, now);

        Assert.IsTrue(firstDecision.Allowed);
        Assert.IsFalse(duplicateDecision.Allowed);
        Assert.AreEqual(BrowserReplayStatus.RejectedDuplicate, duplicateDecision.Status);
        Assert.IsFalse(changedDecision.Allowed);
        Assert.AreEqual(BrowserReplayStatus.Failed, changedDecision.Status);
        Assert.IsFalse(completedDecision.Allowed);
        Assert.AreEqual(BrowserReplayStatus.Completed, completedDecision.Status);
    }

    [TestMethod]
    public void CapabilityRiskGatingBlocksUnsupportedTrustedInputAndRisk()
    {
        var readOnlyExecutor = new BrowserExecutorCapabilities(
            ExecutorId: "executor-1",
            ExecutorKind: BrowserExecutorKind.Cdp,
            Capabilities: new HashSet<BrowserActionType> { BrowserActionType.Read, BrowserActionType.Click },
            RiskLimit: BrowserRiskClass.ReadOnly,
            SupportsTrustedInput: false,
            SupportsDomSnapshot: true,
            SupportsAccessibilitySnapshot: true,
            SupportsScreenshots: false,
            SupportsFrames: true,
            SupportsDownloads: false,
            SupportsFileUpload: false,
            RequiresBrowserLaunch: true,
            RequiresRemoteDebugging: true,
            CanAttachExistingBrowser: false,
            CanUsePersistentProfile: true,
            CanUseRealUserProfile: false);

        var click = CreateAction(actionType: BrowserActionType.Click, riskClass: BrowserRiskClass.Low);
        var read = CreateAction(actionType: BrowserActionType.Read, riskClass: BrowserRiskClass.ReadOnly, idempotencyKey: "");

        Assert.IsFalse(readOnlyExecutor.CanExecute(click));
        Assert.IsTrue(readOnlyExecutor.CanExecute(read));
    }

    [TestMethod]
    public void ChromeLauncherPolicyRequiresLocalhostConsentPortCleanupAndRedaction()
    {
        var invalid = new ChromeLauncherPolicy(
            LaunchMode: BrowserLaunchMode.AttachExisting,
            CdpHost: "0.0.0.0",
            CdpPort: null,
            BindLocalhostOnly: true,
            ProfileMode: BrowserProfileMode.RealUserProfile,
            UserDataDir: "C:/Users/diego/AppData/Local/Google/Chrome/User Data",
            LoadExtension: false,
            AllowAttachExisting: true,
            RequireExplicitUserConsentForRealProfile: false,
            CleanupPolicy: BrowserCleanupPolicy.Manual,
            PortSecurityPolicy: BrowserPortSecurityPolicy.LocalhostOnly,
            SensitiveDataLoggingPolicy: BrowserSensitiveDataLoggingPolicy.DisabledForTestOnly);

        var result = invalid.Validate();

        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(error => error.Contains("localhost", StringComparison.OrdinalIgnoreCase)));
        Assert.IsTrue(result.Errors.Any(error => error.Contains("consent", StringComparison.OrdinalIgnoreCase)));
        Assert.IsTrue(result.Errors.Any(error => error.Contains("CdpPort", StringComparison.Ordinal)));
        Assert.IsTrue(result.Errors.Any(error => error.Contains("redact", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    public void ContractsSerializeAndDeserialize()
    {
        var target = CreateTarget();
        var observation = CreateObservation();
        var action = CreateAction();
        var verification = CreateVerification(BrowserVerificationStatus.Verified);

        RoundTrip(target);
        RoundTrip(observation);
        RoundTrip(action);
        RoundTrip(verification);
    }

    private static void RoundTrip<T>(T value)
    {
        var json = JsonSerializer.Serialize(value);
        var clone = JsonSerializer.Deserialize<T>(json);
        Assert.IsNotNull(clone);
    }

    private static BrowserTargetContext CreateTarget(
        string targetId = "target-1",
        string frameId = "frame-main",
        string? parentFrameId = null,
        long generation = 1,
        string title = "Fixture")
    {
        return new BrowserTargetContext(
            RunId: "run-1",
            BrowserId: "browser-1",
            BrowserSessionId: "session-1",
            BrowserContextId: "context-1",
            WindowId: "window-1",
            TargetId: targetId,
            PageId: "page-1",
            TabId: "tab-1",
            FrameId: frameId,
            ParentFrameId: parentFrameId,
            Url: new Uri("https://example.test/page"),
            Title: title,
            Generation: generation,
            LivenessToken: BrowserTargetContext.CreateLivenessToken(targetId, frameId, generation),
            ObservedAtUtc: DateTimeOffset.UtcNow,
            IsActive: true,
            IsVisible: true,
            IsUserFacing: true,
            ReadyState: "complete",
            Source: BrowserTargetSource.Cdp);
    }

    private static BrowserObservation CreateObservation(string visibleText = "Visible page text", IReadOnlyList<ActionableElement>? actionables = null)
    {
        return new BrowserObservation(
            ObservationId: "observation-1",
            RunId: "run-1",
            TargetContext: CreateTarget(),
            ObservedAtUtc: DateTimeOffset.UtcNow,
            Url: new Uri("https://example.test/page"),
            Title: "Fixture",
            ReadyState: "complete",
            FrameCount: 1,
            MainFrameId: "frame-main",
            VisibleTextSummary: visibleText,
            ActionableElements: actionables ??
                [
                    new ActionableElement(
                        "candidate-1",
                        "frame-main",
                        "button",
                        "button",
                        "Search",
                        "Search",
                        "Search",
                        ["button[data-testid=search]"],
                        new ElementBoundingBox(10, 20, 100, 40),
                        IsVisible: true,
                        IsEnabled: true,
                        RiskHints: [],
                        Confidence: 0.95)
                ],
            Forms: [new FormSummary("form-1", "frame-main", 1, "Search", [])],
            Links: [new LinkSummary("link-1", "frame-main", new Uri("https://example.test/next"), "Next")],
            Warnings: [],
            PayloadLimitApplied: false,
            SensitivityRedactionApplied: false,
            EvidenceRefs: ["evidence-1"]);
    }

    private static BrowserAction CreateAction(
        BrowserTargetContext? targetContext = null,
        BrowserActionType actionType = BrowserActionType.Click,
        BrowserRiskClass riskClass = BrowserRiskClass.Low,
        bool requiresApproval = false,
        string idempotencyKey = "idem-1",
        int timeoutMs = 8000,
        BrowserExpectedOutcome? expectedOutcome = null,
        BrowserActionInput? input = null)
    {
        return new BrowserAction(
            ActionId: "action-1",
            IdempotencyKey: idempotencyKey,
            RunId: "run-1",
            StepId: "step-1",
            TargetContext: targetContext ?? CreateTarget(),
            FrameId: "frame-main",
            ActionType: actionType,
            Target: new BrowserActionTarget("candidate-1", "button[data-testid=search]", "Search", null),
            Input: input,
            ExpectedOutcome: expectedOutcome ?? new BrowserExpectedOutcome("Search results appear", null, "results", null),
            RiskClass: riskClass,
            TimeoutMs: timeoutMs,
            RequiresApproval: requiresApproval,
            CreatedAtUtc: DateTimeOffset.UtcNow);
    }

    private static BrowserVerification CreateVerification(
        BrowserVerificationStatus status,
        IReadOnlyList<string>? evidenceRefs = null,
        string? failureReason = null)
    {
        return new BrowserVerification(
            VerificationId: "verification-1",
            RunId: "run-1",
            StepId: "step-1",
            ActionId: "action-1",
            TargetContext: CreateTarget(),
            ExpectedOutcome: new BrowserExpectedOutcome("Search results appear", null, "results", null),
            PreObservationId: "observation-before",
            PostObservationId: "observation-after",
            Status: status,
            Confidence: status == BrowserVerificationStatus.Verified ? 0.98 : 0.4,
            EvidenceRefs: evidenceRefs ?? ["evidence-1"],
            FailureReason: failureReason,
            VerifiedAtUtc: DateTimeOffset.UtcNow);
    }
}
