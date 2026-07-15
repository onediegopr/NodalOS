using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Models;
using OneBrain.Core.Perception;
using OneBrain.Core.Recipes;
using OneBrain.Core.Runtime;
using OneBrain.Core.Skills;
using OneBrain.Core.Verification;
using OneBrain.Observation.Teaching;

namespace OneBrain.Runtime.Tests;

[TestClass]
[TestCategory("LivingSkills")]
[TestCategory("TeachNodalWindowsObservation")]
public sealed class TeachNodalWindowsObservationAdapterV1Tests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-07-15T12:00:00Z");
    private static readonly IntPtr Hwnd = new(4242);

    [TestMethod]
    public void BoundForegroundWindowProducesVerifiedSkillEvenWhenWindowTitleChanges()
    {
        var adapter = Adapter(
        [
            Snapshot("Fixture Editor — Draft", 101, foreground: true, "empty"),
            Snapshot("Fixture Editor — Draft", 101, foreground: true, "empty"),
            Snapshot("Fixture Editor — Saved", 101, foreground: true, "saved")
        ]);
        var binding = adapter.Bind(Hwnd, "fixture-binding", "fixture-editor", 1, "evidence:binding", Now);
        var session = adapter.CreateCaptureSession(
            binding,
            "teach-windows-fixture",
            "Save one fixture document",
            "workspace.fixture",
            TeachNodalSurface.DesktopFixture,
            new HashSet<string>(StringComparer.Ordinal) { "desktop.uia.action" },
            ReliableRecipeRiskProfile.ReadOnly,
            maximumSteps: 4);
        session.Arm(Consent(), Now);
        session.Start(Now);

        var token = adapter.BeginStep(binding, "save-document", "evidence:before-save", Now.AddSeconds(1));
        var documentRef = ElementRef(token.Before, "document-state");
        var targetRef = ElementRef(token.Before, "save-button");
        var observation = adapter.CompleteStepAndObserve(
            session,
            binding,
            token,
            sequence: 1,
            source: TeachNodalCaptureSource.Uia,
            intentSource: TrustedControlSource.UserInstruction,
            Action("save-document", targetRef),
            Plan("save-document", documentRef, targetRef, "saved"),
            actionExecuted: true,
            actionRejected: false,
            userInterrupted: false,
            evidenceRefs: ["evidence:verified:save-document"],
            completedAtUtc: Now.AddSeconds(2));
        var review = session.Complete(Now.AddSeconds(3));
        var compilation = new TeachNodalCompilerV1().Compile(review.Demonstration);

        Assert.AreEqual("fixture-binding", binding.BindingId);
        Assert.AreEqual(101, binding.ProcessId);
        Assert.AreEqual(binding.ApplicationRef, token.Before.Application.ApplicationRef);
        Assert.AreEqual(binding.ApplicationRef, observation.After.Application.ApplicationRef);
        Assert.AreNotEqual(token.Before.Application.WindowTitleRedacted, observation.After.Application.WindowTitleRedacted);
        Assert.AreNotEqual(token.Before.StateFingerprint, observation.After.StateFingerprint);
        Assert.AreEqual(0, adapter.PendingStepCount);
        Assert.IsFalse(binding.GlobalHooksUsed);
        Assert.IsFalse(binding.RawInputCaptured);
        Assert.IsFalse(binding.RawScreenshotCaptured);
        Assert.IsFalse(binding.RawDomCaptured);
        Assert.IsFalse(binding.ExecutionAuthorityGranted);
        Assert.IsFalse(token.GrantsExecutionAuthority);
        Assert.IsFalse(token.CapturesInput);
        Assert.AreEqual(TeachNodalCaptureSessionState.ReviewReady, review.State);
        Assert.IsTrue(review.CanCompileVerifiedSkill);
        Assert.AreEqual(0, review.PerStepApprovalsRequested);
        Assert.IsTrue(compilation.Decision is TeachNodalCompilationDecision.CompiledVerifiedSkill or
            TeachNodalCompilationDecision.CompiledVerifiedSkillRecipeNeedsReview);
        Assert.AreEqual(ExecutableSkillState.Verified, compilation.Skill?.State);
        Assert.AreEqual(1, compilation.Skill?.Transitions.Count);
        Assert.IsFalse(compilation.Skill?.LiveExecutionAuthorityGranted ?? true);
    }

    [TestMethod]
    public void AdapterRejectsZeroHandleBackgroundWindowAndInvalidProcessIdentity()
    {
        Assert.ThrowsExactly<ArgumentException>(() => Adapter([]).Bind(
            IntPtr.Zero,
            "binding",
            "fixture-editor",
            1,
            "evidence:binding",
            Now));
        Assert.ThrowsExactly<InvalidOperationException>(() => Adapter(
        [
            Snapshot("Fixture Editor", 101, foreground: false, "empty")
        ]).Bind(Hwnd, "binding", "fixture-editor", 1, "evidence:binding", Now));
        Assert.ThrowsExactly<InvalidOperationException>(() => Adapter(
        [
            Snapshot("Fixture Editor", 0, foreground: true, "empty")
        ]).Bind(Hwnd, "binding", "fixture-editor", 1, "evidence:binding", Now));
    }

    [TestMethod]
    public void CompletionFailsClosedWhenBoundWindowLosesForegroundOrChangesProcess()
    {
        var foregroundLoss = Adapter(
        [
            Snapshot("Fixture Editor", 101, foreground: true, "empty"),
            Snapshot("Fixture Editor", 101, foreground: true, "empty"),
            Snapshot("Fixture Editor", 101, foreground: false, "saved")
        ]);
        var binding = foregroundLoss.Bind(Hwnd, "binding", "fixture-editor", 1, "evidence:binding", Now);
        var token = foregroundLoss.BeginStep(binding, "save", "evidence:before", Now.AddSeconds(1));
        Assert.ThrowsExactly<InvalidOperationException>(() => Complete(foregroundLoss, binding, token));
        Assert.AreEqual(1, foregroundLoss.PendingStepCount);
        Assert.IsTrue(foregroundLoss.CancelStep(binding, token.TokenId));

        var processChange = Adapter(
        [
            Snapshot("Fixture Editor", 101, foreground: true, "empty"),
            Snapshot("Fixture Editor", 101, foreground: true, "empty"),
            Snapshot("Other App", 202, foreground: true, "saved", processName: "other-app")
        ]);
        var secondBinding = processChange.Bind(Hwnd, "binding-2", "fixture-editor", 1, "evidence:binding-2", Now);
        var secondToken = processChange.BeginStep(secondBinding, "save", "evidence:before", Now.AddSeconds(1));
        Assert.ThrowsExactly<InvalidOperationException>(() => Complete(processChange, secondBinding, secondToken));
        Assert.AreEqual(1, processChange.PendingStepCount);
    }

    [TestMethod]
    public void AdapterRejectsUntrustedIntentControlLabelsUnsupportedSourcesAndExpiredSteps()
    {
        var adapter = Adapter(
        [
            Snapshot("Fixture Editor", 101, foreground: true, "empty"),
            Snapshot("Fixture Editor", 101, foreground: true, "empty")
        ]);
        var binding = adapter.Bind(Hwnd, "binding", "fixture-editor", 1, "evidence:binding", Now);
        var token = adapter.BeginStep(
            binding,
            "save",
            "evidence:before",
            Now.AddSeconds(1),
            maximumDuration: TimeSpan.FromSeconds(2));
        var targetRef = ElementRef(token.Before, "save-button");
        var plan = Plan("save", ElementRef(token.Before, "document-state"), targetRef, "saved");
        var observedAction = Action("save", targetRef);

        Assert.ThrowsExactly<InvalidOperationException>(() => adapter.CompleteStep(
            binding, token, TeachNodalCaptureSource.Cdp, TrustedControlSource.UserInstruction,
            observedAction, plan, true, false, false, ["evidence:verified:save"], Now.AddSeconds(2)));
        Assert.ThrowsExactly<InvalidOperationException>(() => adapter.CompleteStep(
            binding, token, TeachNodalCaptureSource.Uia, TrustedControlSource.VisualObservation,
            observedAction, plan, true, false, false, ["evidence:verified:save"], Now.AddSeconds(2)));
        Assert.ThrowsExactly<InvalidOperationException>(() => adapter.CompleteStep(
            binding, token, TeachNodalCaptureSource.Uia, TrustedControlSource.UserInstruction,
            observedAction with { TargetLabelSource = TrustedControlSource.OperatorDecision },
            plan, true, false, false, ["evidence:verified:save"], Now.AddSeconds(2)));
        Assert.ThrowsExactly<InvalidOperationException>(() => adapter.CompleteStep(
            binding, token, TeachNodalCaptureSource.Uia, TrustedControlSource.UserInstruction,
            observedAction, plan, true, false, false, ["evidence:verified:save"], Now.AddSeconds(4)));
        Assert.AreEqual(1, adapter.PendingStepCount);
        Assert.IsTrue(adapter.CancelStep(binding, token.TokenId));
    }

    [TestMethod]
    public void TokenCannotReplayReleaseOrRebindWhilePending()
    {
        var adapter = Adapter(
        [
            Snapshot("Fixture Editor", 101, foreground: true, "empty"),
            Snapshot("Fixture Editor", 101, foreground: true, "empty"),
            Snapshot("Fixture Editor", 101, foreground: true, "saved"),
            Snapshot("Fixture Editor", 101, foreground: true, "saved")
        ]);
        var binding = adapter.Bind(Hwnd, "binding", "fixture-editor", 1, "evidence:binding", Now);
        var token = adapter.BeginStep(binding, "save", "evidence:before", Now.AddSeconds(1));

        Assert.ThrowsExactly<InvalidOperationException>(() => adapter.ReleaseBinding(binding));
        Assert.ThrowsExactly<InvalidOperationException>(() => adapter.Bind(
            new IntPtr(5252), "other-binding", "fixture-editor", 1, "evidence:other", Now.AddSeconds(1)));

        var observation = Complete(adapter, binding, token);
        Assert.IsFalse(observation.GlobalHookUsed);
        Assert.IsFalse(observation.RawKeyboardPayloadPresent);
        Assert.IsFalse(observation.RawPointerCoordinatesPresent);
        Assert.IsFalse(observation.RawScreenshotPresent);
        Assert.IsFalse(observation.RawDomPresent);
        Assert.ThrowsExactly<InvalidOperationException>(() => Complete(adapter, binding, token));
        adapter.ReleaseBinding(binding);
        Assert.IsNull(adapter.Binding);
    }

    [TestMethod]
    public void MismatchedCaptureSessionCannotConsumeWindowsObservation()
    {
        var adapter = Adapter(
        [
            Snapshot("Fixture Editor", 101, foreground: true, "empty"),
            Snapshot("Fixture Editor", 101, foreground: true, "empty")
        ]);
        var binding = adapter.Bind(Hwnd, "binding", "fixture-editor", 1, "evidence:binding", Now);
        var token = adapter.BeginStep(binding, "save", "evidence:before", Now.AddSeconds(1));
        var mismatched = new TeachNodalCaptureSessionV1(new TeachNodalCaptureScope(
            "other-capture",
            "Other capture",
            "workspace.fixture",
            "other-profile",
            1,
            "other-app",
            TeachNodalSurface.DesktopFixture,
            new HashSet<string>(StringComparer.Ordinal) { "desktop.uia.action" },
            ReliableRecipeRiskProfile.ReadOnly,
            2));

        var targetRef = ElementRef(token.Before, "save-button");
        Assert.ThrowsExactly<InvalidOperationException>(() => adapter.CompleteStepAndObserve(
            mismatched,
            binding,
            token,
            1,
            TeachNodalCaptureSource.Uia,
            TrustedControlSource.UserInstruction,
            Action("save", targetRef),
            Plan("save", ElementRef(token.Before, "document-state"), targetRef, "saved"),
            true,
            false,
            false,
            ["evidence:verified:save"],
            Now.AddSeconds(2)));
        Assert.AreEqual(1, adapter.PendingStepCount);
    }

    private static TeachNodalCaptureObservation Complete(
        TeachNodalWindowsObservationAdapterV1 adapter,
        TeachNodalWindowsApplicationBinding binding,
        TeachNodalWindowsStepToken token)
    {
        var targetRef = ElementRef(token.Before, "save-button");
        return adapter.CompleteStep(
            binding,
            token,
            TeachNodalCaptureSource.Uia,
            TrustedControlSource.UserInstruction,
            Action("save", targetRef),
            Plan("save", ElementRef(token.Before, "document-state"), targetRef, "saved"),
            actionExecuted: true,
            actionRejected: false,
            userInterrupted: false,
            evidenceRefs: ["evidence:verified:save"],
            completedAtUtc: Now.AddSeconds(2));
    }

    private static TeachNodalWindowsObservationAdapterV1 Adapter(IEnumerable<CognitiveSnapshot> snapshots)
    {
        var queue = new Queue<CognitiveSnapshot>(snapshots);
        return new TeachNodalWindowsObservationAdapterV1((_, _) =>
            queue.Count == 0 ? null : queue.Dequeue());
    }

    private static TeachNodalCaptureConsent Consent() => new(
        ExplicitOptIn: true,
        GrantedAtUtc: Now,
        ExpiresAtUtc: Now.AddMinutes(30),
        AllowedSources: new HashSet<TeachNodalCaptureSource> { TeachNodalCaptureSource.Uia },
        ConsentEvidenceRef: "evidence:capture-consent");

    private static TeachNodalObservedAction Action(string id, string targetRef) => new(
        ActionId: id,
        Kind: TeachNodalActionKind.Click,
        IntentRedacted: "Save the fixture document.",
        CapabilityId: "desktop.uia.action",
        Operation: "invoke",
        SemanticTargetRef: targetRef,
        TargetLabelRedacted: "Save",
        TargetRoleRedacted: "Button",
        TargetLabelSource: TrustedControlSource.UiaObservation,
        Parameters: [],
        SelectorAliasRefs: ["app-profile:fixture-editor:save"],
        Confidence: 0.98d);

    private static SemanticVerificationPlan Plan(
        string id,
        string documentRef,
        string targetRef,
        string expected) => new(
        PlanId: "verify-" + id,
        Preconditions: [Rule(id + "-target", SemanticVerificationRuleKind.ElementPresent, targetRef)],
        ExpectedTransition:
        [
            Rule(id + "-changed", SemanticVerificationRuleKind.PropertyChanged, documentRef, "name"),
            Rule(id + "-fingerprint", SemanticVerificationRuleKind.StateFingerprintChanged)
        ],
        ExpectedOutcome:
        [
            Rule(id + "-outcome", SemanticVerificationRuleKind.PropertyEquals, documentRef, "name", expected),
            Rule(id + "-conflicts", SemanticVerificationRuleKind.NoBlockingConflicts)
        ],
        ForbiddenSideEffects: [],
        RequiredEvidenceRefs: ["evidence:verified:" + id],
        Timeout: TimeSpan.FromSeconds(30),
        AppProfileRef: "fixture-editor",
        RequireActionExecuted: true,
        AllowProcessChange: false,
        FailOnBlockingConflicts: true);

    private static string ElementRef(CognitiveSnapshotV2 snapshot, string automationId) =>
        snapshot.Elements.Single(element =>
            element.CanonicalProperties.TryGetValue("automationId", out var value) &&
            string.Equals(value, automationId, StringComparison.Ordinal)).SemanticRef;

    private static CognitiveSnapshot Snapshot(
        string title,
        int processId,
        bool foreground,
        string documentState,
        string processName = "fixture-editor") => new(
        new WindowSnapshot(
            Title: title,
            ProcessName: processName,
            ProcessId: processId,
            Bounds: new WindowBounds(0, 0, 1280, 720),
            IsForeground: foreground),
        [
            Element("document-state", "Document", documentState, "document-runtime"),
            Element("save-button", "Button", "Save", "save-runtime")
        ],
        TreeTruncated: false);

    private static UiElementSnapshot Element(
        string automationId,
        string role,
        string name,
        string runtimeId) => new(
        Ref: "@" + automationId,
        Role: role,
        Name: name,
        AutomationId: automationId,
        ClassName: role,
        Bounds: new WindowBounds(10, 10, 200, 50),
        IsEnabled: true,
        IsOffscreen: false,
        IsKeyboardFocusable: true,
        Patterns: role == "Button" ? ["Invoke"] : ["Value"],
        Actions: role == "Button" ? ["invoke"] : ["read_value"],
        RuntimeId: runtimeId);

    private static SemanticVerificationRule Rule(
        string id,
        SemanticVerificationRuleKind kind,
        string? subject = null,
        string? property = null,
        string? expected = null) => new(
        RuleId: id,
        Kind: kind,
        SubjectRef: subject,
        Property: property,
        ExpectedValueRedacted: expected,
        Required: true);
}
