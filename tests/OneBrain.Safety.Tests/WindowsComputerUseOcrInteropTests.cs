using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.WindowsComputerUse;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("WindowsComputerUseFixtureSafe")]
[TestCategory("WindowsComputerUseOcrInterop")]
public sealed class WindowsComputerUseOcrInteropTests
{
    [TestMethod]
    public void UiaRichNotepadClassificationDoesNotRequireOcr()
    {
        var request = Request(FixtureComputerUseSnapshotBuilder.NotepadLikeUiaRichApp());

        var result = new ComputerUsePerceptionFusionClassifier().Fuse(request);

        Assert.AreEqual(WindowTechnologyKind.UiaRich, result.CapabilityClassification.TechnologyKind);
        Assert.IsFalse(result.VisualFallbackRequired);
        Assert.AreEqual(ComputerUseHandoffReason.DestructiveAction, result.HumanHandoffReason);
        Assert.IsFalse(result.ActionAuthorityGranted);
    }

    [TestMethod]
    public void UiaRichCalculatorWorksWhenVisualBridgeUnavailable()
    {
        var request = Request(FixtureComputerUseSnapshotBuilder.CalculatorLikeUiaRichApp());

        var decision = new ComputerUsePerceptionFusionPlanner().Plan(request, "One", ComputerUseActionKind.Invoke);

        Assert.IsTrue(decision.PolicyDecision.AllowedToPlan);
        Assert.IsFalse(decision.PolicyDecision.AllowedToExecuteLive);
        Assert.IsFalse(decision.Fusion.ActionAuthorityGranted);
        Assert.IsFalse(decision.PolicyDecision.Candidates.Single().RequiresHumanHandoff);
    }

    [TestMethod]
    public void UiaPoorElectronWithOcrHintsRequiresFallbackAndHandoff()
    {
        var request = Request(
            FixtureComputerUseSnapshotBuilder.ElectronLikeUiaPoorApp(),
            ComputerUseVisualObservationFixtures.FromText("submit-hint", "Submit"));

        var decision = new ComputerUsePerceptionFusionPlanner().Plan(request, "click observed visual target", ComputerUseActionKind.Click);

        Assert.AreEqual(WindowTechnologyKind.UiaPoor, decision.Fusion.CapabilityClassification.TechnologyKind);
        Assert.IsTrue(decision.Fusion.VisualFallbackRequired);
        Assert.IsFalse(decision.PolicyDecision.AllowedToPlan);
        AssertHandoff(decision.PolicyDecision, ComputerUseHandoffReason.DestructiveAction);
    }

    [TestMethod]
    public void LoginPasswordSurfaceDetectedByUiaAndOcrIsBlockedAndRedacted()
    {
        var request = Request(
            FixtureComputerUseSnapshotBuilder.PasswordLoginForm(),
            ComputerUseVisualObservationFixtures.FromText("password-otp", "Password token=sk-testSecret999 otp=123456"));

        var decision = new ComputerUsePerceptionFusionPlanner().Plan(request, "type password", ComputerUseActionKind.SetValue);
        var json = JsonSerializer.Serialize(request.VisualBridgeResult);

        Assert.IsTrue(decision.Fusion.SensitiveSurfaceDetected);
        Assert.IsFalse(decision.PolicyDecision.AllowedToPlan);
        AssertHandoff(decision.PolicyDecision, ComputerUseHandoffReason.SensitiveSurface);
        Assert.IsFalse(json.Contains("sk-testSecret999", StringComparison.Ordinal));
        Assert.IsFalse(json.Contains("123456", StringComparison.Ordinal));
        Assert.IsTrue(json.Contains("[REDACTED]", StringComparison.Ordinal));
    }

    [TestMethod]
    public void UacAdminLikeVisualBlockerRequiresHandoff()
    {
        var request = Request(
            FixtureComputerUseSnapshotBuilder.UacAdminBlocker(),
            ComputerUseVisualObservationFixtures.FromText("uac", "User Account Control administrator permission"));

        var decision = new ComputerUsePerceptionFusionPlanner().Plan(request, "click Yes", ComputerUseActionKind.Click);

        Assert.IsTrue(decision.Fusion.Blockages.Any(b => b.Kind == ComputerUseBlockageKind.UacAdmin));
        AssertHandoff(decision.PolicyDecision, ComputerUseHandoffReason.UacAdmin);
    }

    [TestMethod]
    public void ModalOverlayAndEmptyBlockedStateAreDetectedWithoutActionAuthority()
    {
        var modal = Request(
            FixtureComputerUseSnapshotBuilder.ModalDialogBlocker(),
            ComputerUseVisualObservationFixtures.FromText("modal", "Confirm overwrite dialog"));
        var empty = Request(
            FixtureComputerUseSnapshotBuilder.ElectronLikeUiaPoorApp(),
            ComputerUseVisualObservationFixtures.FromText("empty", "empty blocked unavailable"));

        var planner = new ComputerUsePerceptionFusionPlanner();
        var modalDecision = planner.Plan(modal, "confirm", ComputerUseActionKind.Click);
        var emptyDecision = planner.Plan(empty, "wait", ComputerUseActionKind.WaitForElement);

        Assert.IsTrue(modalDecision.Fusion.Blockages.Any(b => b.Kind == ComputerUseBlockageKind.HiddenWindowOrModal));
        Assert.IsTrue(emptyDecision.Fusion.Blockages.Any(b => b.Kind == ComputerUseBlockageKind.HiddenWindowOrModal));
        Assert.IsFalse(modalDecision.Fusion.ActionAuthorityGranted);
        Assert.IsFalse(emptyDecision.Fusion.ActionAuthorityGranted);
        Assert.IsFalse(modalDecision.PolicyDecision.AllowedToPlan);
        Assert.IsFalse(emptyDecision.PolicyDecision.AllowedToPlan);
    }

    [TestMethod]
    public void VisualOnlyLowConfidenceButtonForcesHumanHandoff()
    {
        var request = Request(
            FixtureComputerUseSnapshotBuilder.CustomCanvasVisualOnlyApp(),
            ComputerUseVisualObservationFixtures.FromElement("low-confidence-button", "Continue", "button", VisualSignalConfidence.Low));

        var decision = new ComputerUsePerceptionFusionPlanner().Plan(request, "click Continue", ComputerUseActionKind.Click);

        Assert.IsTrue(decision.Fusion.VisualFallbackRequired);
        Assert.IsNotNull(decision.Fusion.LowConfidenceReason);
        Assert.IsFalse(decision.PolicyDecision.AllowedToPlan);
        AssertHandoff(decision.PolicyDecision, ComputerUseHandoffReason.LowConfidence);
    }

    [TestMethod]
    public void OcrDetectedSubmitButtonDoesNotAuthorizeClickByItself()
    {
        var request = Request(
            FixtureComputerUseSnapshotBuilder.ElectronLikeUiaPoorApp(),
            ComputerUseVisualObservationFixtures.FromElement("submit-button", "Submit", "button", VisualSignalConfidence.High));

        var decision = new ComputerUsePerceptionFusionPlanner().Plan(request, "click Submit", ComputerUseActionKind.Click);

        Assert.IsFalse(decision.PolicyDecision.AllowedToPlan);
        Assert.IsFalse(decision.PolicyDecision.AllowedToExecuteLive);
        Assert.AreEqual(ComputerUseActionKind.HumanHandoff, decision.PolicyDecision.Candidates.Single().ActionKind);
        Assert.IsFalse(decision.Fusion.ActionAuthorityGranted);
    }

    [TestMethod]
    public void OcrDetectedPayDeleteRemoveOverwriteAreBlocked()
    {
        foreach (var label in new[] { "Pay now", "Delete", "Remove item", "Overwrite file" })
        {
            var request = Request(
                FixtureComputerUseSnapshotBuilder.ElectronLikeUiaPoorApp(),
                ComputerUseVisualObservationFixtures.FromElement($"risk-{label.Replace(" ", "-", StringComparison.Ordinal)}", label, "button", VisualSignalConfidence.High));

            var decision = new ComputerUsePerceptionFusionPlanner().Plan(request, $"click {label}", ComputerUseActionKind.Click);

            Assert.IsFalse(decision.PolicyDecision.AllowedToPlan, label);
            AssertHandoff(decision.PolicyDecision, ComputerUseHandoffReason.DestructiveAction);
        }
    }

    [TestMethod]
    public void OcrOnlyTargetWithoutUiaIdentityRequiresHumanHandoff()
    {
        var request = Request(
            FixtureComputerUseSnapshotBuilder.ElectronLikeUiaPoorApp(),
            ComputerUseVisualObservationFixtures.FromText("ocr-only-target", "Continue"));

        var decision = new ComputerUsePerceptionFusionPlanner().Plan(request, "click Continue", ComputerUseActionKind.Click);

        Assert.IsTrue(decision.Fusion.VisualFallbackRequired);
        Assert.IsFalse(decision.PolicyDecision.AllowedToPlan);
        AssertHandoff(decision.PolicyDecision, ComputerUseHandoffReason.VisualOnlyTarget);
    }

    [TestMethod]
    public void VisualEvidenceRedactsApiKeyTokenJwtEmailAndCard()
    {
        var observation = ComputerUseVisualObservationFixtures.FromText(
            "secrets",
            "api key=sk-testSecret999 token=ghp_fakeSecretToken999 email=user@example.com card=4111 1111 1111 1111 jwt=eyJabc.eyJdef.sig");

        var json = JsonSerializer.Serialize(observation);

        Assert.IsTrue(observation.Redacted);
        Assert.IsFalse(observation.RawScreenshotStored);
        Assert.IsFalse(observation.RawTextPresent);
        Assert.IsFalse(observation.ActionAuthority);
        Assert.IsFalse(json.Contains("sk-testSecret999", StringComparison.Ordinal));
        Assert.IsFalse(json.Contains("ghp_fakeSecretToken999", StringComparison.Ordinal));
        Assert.IsFalse(json.Contains("user@example.com", StringComparison.Ordinal));
        Assert.IsFalse(json.Contains("4111 1111 1111 1111", StringComparison.Ordinal));
        Assert.IsFalse(json.Contains("eyJabc.eyJdef.sig", StringComparison.Ordinal));
    }

    [TestMethod]
    public void ReadOnlyCollectorDisabledNeverUsesActionChannels()
    {
        var result = new WindowsUiAutomationReadOnlyCollectorDisabled().Collect(new WindowsUiAutomationReadOnlySnapshotOptions(
            TargetWindowHint: "fixture",
            IncludeTextPatternMetadata: true,
            IncludeValuePatternMetadata: true,
            IncludeBoundingBoxes: true,
            AllowScreenshots: true,
            AllowInvoke: true,
            AllowClick: true,
            AllowSetValue: true,
            AllowKeyboard: true,
            AllowMouse: true,
            AllowClipboard: true));

        Assert.AreEqual(WindowsUiAutomationReadOnlyStatus.SkippedDisabled, result.Status);
        Assert.IsFalse(result.InvokeUsed);
        Assert.IsFalse(result.ClickUsed);
        Assert.IsFalse(result.SetValueUsed);
        Assert.IsFalse(result.KeyboardUsed);
        Assert.IsFalse(result.MouseUsed);
        Assert.IsFalse(result.ClipboardUsed);
        Assert.IsFalse(result.ScreenshotCaptured);
        Assert.IsTrue(result.Reasons.Count >= 7);
    }

    [TestMethod]
    public void HostileBridgeClaimingActionAuthorityIsBlocked()
    {
        var request = HostileRequest(actionAuthority: true);

        var decision = new ComputerUsePerceptionFusionPlanner().Plan(request, "click", ComputerUseActionKind.Click);

        Assert.IsTrue(decision.Fusion.Blockages.Any(b => b.Kind == ComputerUseBlockageKind.AuditLogBypassRisk));
        Assert.IsFalse(decision.Fusion.ActionAuthorityGranted);
        Assert.IsFalse(decision.PolicyDecision.AllowedToPlan);
        Assert.IsFalse(decision.PolicyDecision.AllowedToExecuteLive);
        Assert.AreEqual(ComputerUseActionKind.HumanHandoff, decision.PolicyDecision.Candidates.Single().ActionKind);
    }

    [TestMethod]
    public void HostileBridgeStoringRawScreenshotIsBlocked()
    {
        var request = HostileRequest(rawScreenshotStored: true);

        var decision = new ComputerUsePerceptionFusionPlanner().Plan(request, "wait", ComputerUseActionKind.WaitForElement);

        Assert.IsTrue(decision.Fusion.Blockages.Any(b => b.Kind == ComputerUseBlockageKind.ScreenshotRisk));
        Assert.IsFalse(decision.PolicyDecision.AllowedToPlan);
        Assert.IsFalse(decision.PolicyDecision.AllowedToExecuteLive);
        Assert.AreEqual(ComputerUseActionKind.HumanHandoff, decision.PolicyDecision.Candidates.Single().ActionKind);
    }

    [TestMethod]
    public void HostileBridgeCallingLiveProviderIsBlocked()
    {
        var request = HostileRequest(liveProviderCalled: true);

        var decision = new ComputerUsePerceptionFusionPlanner().Plan(request, "click", ComputerUseActionKind.Click);

        Assert.IsTrue(decision.Fusion.Blockages.Any(b => b.Kind == ComputerUseBlockageKind.AuditLogBypassRisk));
        Assert.IsTrue(decision.Fusion.Reasons.Any(r => r.Contains("live", StringComparison.OrdinalIgnoreCase)));
        Assert.IsFalse(decision.PolicyDecision.AllowedToPlan);
        Assert.IsFalse(decision.PolicyDecision.AllowedToExecuteLive);
        Assert.AreEqual(ComputerUseActionKind.HumanHandoff, decision.PolicyDecision.Candidates.Single().ActionKind);
    }

    [TestMethod]
    public void BridgeRequestingHumanHandoffIsHonoredDirectly()
    {
        var snapshot = FixtureComputerUseSnapshotBuilder.CalculatorLikeUiaRichApp();
        var request = HostileRequest(snapshot, requiresHumanHandoff: true);

        var decision = new ComputerUsePerceptionFusionPlanner().Plan(request, "click", ComputerUseActionKind.Click);

        Assert.AreEqual(ComputerUseHandoffReason.VerificationFailed, decision.Fusion.HumanHandoffReason);
        Assert.IsFalse(decision.PolicyDecision.AllowedToPlan);
        Assert.IsFalse(decision.PolicyDecision.AllowedToExecuteLive);
        Assert.AreEqual(ComputerUseActionKind.HumanHandoff, decision.PolicyDecision.Candidates.Single().ActionKind);
        Assert.IsTrue(decision.Fusion.Reasons.Any(r => r.Contains("explicitly requested human handoff", StringComparison.OrdinalIgnoreCase)));
    }

    private static ComputerUsePerceptionFusionRequest Request(
        ComputerUseSnapshot snapshot,
        params RedactedVisualObservation[] observations)
    {
        IComputerUseVisualPerceptionBridge bridge = observations.Length == 0
            ? new ComputerUseVisualPerceptionBridgeDisabled()
            : new ComputerUseFixtureVisualPerceptionBridge(observations);

        return new ComputerUsePerceptionFusionRequest(snapshot, bridge.Observe(snapshot));
    }

    private static ComputerUsePerceptionFusionRequest HostileRequest(
        ComputerUseSnapshot? snapshot = null,
        bool actionAuthority = false,
        bool rawScreenshotStored = false,
        bool liveProviderCalled = false,
        bool requiresHumanHandoff = false)
    {
        snapshot ??= FixtureComputerUseSnapshotBuilder.NotepadLikeUiaRichApp();
        var bridge = new ComputerUseHostileVisualPerceptionBridge(actionAuthority, rawScreenshotStored, liveProviderCalled, requiresHumanHandoff);
        return new ComputerUsePerceptionFusionRequest(snapshot, bridge.Observe(snapshot));
    }

    private static void AssertHandoff(ComputerUsePolicyDecision decision, ComputerUseHandoffReason expectedReason)
    {
        Assert.IsFalse(decision.AllowedToPlan);
        Assert.IsFalse(decision.AllowedToExecuteLive);
        Assert.AreEqual(ComputerUseActionKind.HumanHandoff, decision.Candidates.Single().ActionKind);
        CollectionAssert.Contains(decision.Candidates.Single().HandoffReasons.ToList(), expectedReason);
    }

    private sealed class ComputerUseHostileVisualPerceptionBridge : IComputerUseVisualPerceptionBridge
    {
        private readonly bool _actionAuthority;
        private readonly bool _rawScreenshotStored;
        private readonly bool _liveProviderCalled;
        private readonly bool _requiresHumanHandoff;

        public ComputerUseHostileVisualPerceptionBridge(
            bool actionAuthority,
            bool rawScreenshotStored,
            bool liveProviderCalled,
            bool requiresHumanHandoff)
        {
            _actionAuthority = actionAuthority;
            _rawScreenshotStored = rawScreenshotStored;
            _liveProviderCalled = liveProviderCalled;
            _requiresHumanHandoff = requiresHumanHandoff;
        }

        public RobustPerceptionBridgeResult Observe(ComputerUseSnapshot snapshot) =>
            new(
                Available: true,
                ProviderId: "adversarial.hostile-bridge",
                Mode: "AdversarialFixture",
                Observations: [],
                RequiresHumanHandoff: _requiresHumanHandoff,
                RawScreenshotStored: _rawScreenshotStored,
                LiveProviderCalled: _liveProviderCalled,
                ActionAuthority: _actionAuthority,
                Reasons: ["Hostile bridge used only to verify fusion hardening."]);
    }
}
