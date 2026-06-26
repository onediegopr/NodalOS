using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.WindowsComputerUse;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("WindowsComputerUseFixtureSafe")]
[TestCategory("WindowsComputerUseWin32UiaEvents")]
public sealed class WindowsComputerUseWin32UiaEventsTests
{
    [TestMethod]
    public void DisabledWin32CollectorDoesNotReadRealPcOrUseActionChannels()
    {
        var result = new DisabledWin32ContextReadOnlyCollector().Collect(new Win32ContextCollectionOptions(
            "disabled",
            AllowWindowManipulation: true,
            AllowFocusStealing: true,
            AllowInputInjection: true,
            AllowClipboard: true,
            AllowScreenshots: true));

        Assert.AreEqual(Win32ContextCollectionStatus.Disabled, result.Status);
        Assert.IsFalse(result.ReadRealPc);
        Assert.IsFalse(result.WindowManipulationUsed);
        Assert.IsFalse(result.FocusStealingUsed);
        Assert.IsFalse(result.InputInjectionUsed);
        Assert.IsFalse(result.ClipboardUsed);
        Assert.IsFalse(result.ScreenshotCaptured);
        Assert.IsFalse(result.ActionAuthority);
        Assert.IsTrue(result.Reasons.Count >= 6);
    }

    [TestMethod]
    public void DisabledUiaEventStreamDoesNotSubscribeLiveOrRegisterCallbacks()
    {
        var state = new DisabledWindowsUiAutomationEventStream().Read(new WindowsUiAutomationEventStreamOptions(
            "disabled",
            SubscribeLiveEvents: true,
            AllowActionCallbacks: true,
            AllowInvoke: true,
            AllowClick: true,
            AllowSetValue: true,
            AllowKeyboard: true,
            AllowMouse: true,
            AllowClipboard: true));

        Assert.AreEqual(WindowsUiAutomationEventStreamStatus.Disabled, state.Status);
        Assert.IsFalse(state.LiveSubscribed);
        Assert.IsFalse(state.ActionCallbackRegistered);
        Assert.IsFalse(state.ActionAuthority);
        Assert.IsTrue(state.Throttled);
        Assert.IsTrue(state.Debounced);
        Assert.IsTrue(state.Reasons.Count >= 8);
    }

    [TestMethod]
    public void NotepadLikeAppUsesUiaAndWin32WithoutOcrOrEventBlockage()
    {
        var request = Request(SimpleNotepadSnapshot(), FixtureWin32ContextFactory.NotepadActive(), FixtureWindowsUiAutomationEvents.NotepadNoBlockage());

        var decision = new ComputerUsePerceptionFusionPlanner().Plan(request, "Text Editor", ComputerUseActionKind.WaitForElement);

        Assert.AreEqual(WindowTechnologyKind.UiaRich, decision.Fusion.CapabilityClassification.TechnologyKind);
        Assert.IsTrue(decision.Fusion.ActiveWindowMatched);
        Assert.IsTrue(decision.Fusion.UiaRichnessScore > 0.8);
        Assert.IsFalse(decision.Fusion.VisualFallbackRequired);
        Assert.IsFalse(decision.Fusion.ModalOrOverlayState);
        Assert.IsTrue(decision.PolicyDecision.AllowedToPlan);
        Assert.IsFalse(decision.PolicyDecision.AllowedToExecuteLive);
        Assert.IsFalse(decision.Fusion.ActionAuthorityGranted);
    }

    [TestMethod]
    public void ElectronLikeAppUsesWin32AndOcrHintsButNoActionAuthority()
    {
        var request = Request(
            FixtureComputerUseSnapshotBuilder.ElectronLikeUiaPoorApp(),
            FixtureWin32ContextFactory.ElectronActive(),
            FixtureWindowsUiAutomationEvents.ElectronStructureChanged(),
            ComputerUseVisualObservationFixtures.FromText("electron-hint", "Continue"));

        var decision = new ComputerUsePerceptionFusionPlanner().Plan(request, "click Continue", ComputerUseActionKind.Click);

        Assert.AreEqual(WindowTechnologyKind.UiaPoor, decision.Fusion.CapabilityClassification.TechnologyKind);
        Assert.IsTrue(decision.Fusion.VisualFallbackRequired);
        Assert.IsTrue(decision.Fusion.ActiveWindowMatched);
        Assert.IsFalse(decision.PolicyDecision.AllowedToPlan);
        Assert.IsFalse(decision.Fusion.ActionAuthorityGranted);
        AssertHandoff(decision.PolicyDecision, ComputerUseHandoffReason.VisualOnlyTarget);
    }

    [TestMethod]
    public void ModalDialogFromWin32AndStructureChangedRequiresHandoff()
    {
        var request = Request(
            FixtureComputerUseSnapshotBuilder.ModalDialogBlocker(),
            FixtureWin32ContextFactory.ModalDialog(),
            FixtureWindowsUiAutomationEvents.ModalAppeared());

        var decision = new ComputerUsePerceptionFusionPlanner().Plan(request, "confirm overwrite", ComputerUseActionKind.Click);

        Assert.IsTrue(decision.Fusion.ModalOrOverlayState);
        Assert.IsTrue(decision.Fusion.Blockages.Any(b => b.Kind == ComputerUseBlockageKind.HiddenWindowOrModal));
        Assert.IsFalse(decision.PolicyDecision.AllowedToPlan);
        AssertHandoff(decision.PolicyDecision, ComputerUseHandoffReason.DestructiveAction);
    }

    [TestMethod]
    public void PasswordLoginSurfaceFromUiaOcrAndEventIsRedactedAndBlocked()
    {
        var request = Request(
            FixtureComputerUseSnapshotBuilder.PasswordLoginForm(),
            FixtureWin32ContextFactory.LoginActiveWithSensitiveTitle(),
            FixtureWindowsUiAutomationEvents.LoginSensitiveValueChanged(),
            ComputerUseVisualObservationFixtures.FromText("ocr-login", "Password token=sk-testSecret999 email=user@example.com"));

        var decision = new ComputerUsePerceptionFusionPlanner().Plan(request, "type password", ComputerUseActionKind.SetValue);
        var eventEvidence = new ComputerUseReadOnlyContextEvidenceBuilder().BuildUiaEventEvidence(request.UiaEvents!);
        var json = JsonSerializer.Serialize(eventEvidence);

        Assert.IsTrue(decision.Fusion.SensitiveSurfaceDetected);
        Assert.IsFalse(decision.PolicyDecision.AllowedToPlan);
        AssertHandoff(decision.PolicyDecision, ComputerUseHandoffReason.SensitiveSurface);
        Assert.IsFalse(json.Contains("hunter2", StringComparison.Ordinal));
        Assert.IsFalse(json.Contains("sk-testSecret999", StringComparison.Ordinal));
        Assert.IsTrue(json.Contains("[REDACTED]", StringComparison.Ordinal));
    }

    [TestMethod]
    public void UacAdminLikeWin32AndUiaEventRequireHandoff()
    {
        var request = Request(
            FixtureComputerUseSnapshotBuilder.UacAdminBlocker(),
            FixtureWin32ContextFactory.UacAdminLike(),
            FixtureWindowsUiAutomationEvents.UacBlocked());

        var decision = new ComputerUsePerceptionFusionPlanner().Plan(request, "click Yes", ComputerUseActionKind.Click);

        Assert.IsTrue(decision.Fusion.Blockages.Any(b => b.Kind == ComputerUseBlockageKind.UacAdmin));
        Assert.IsFalse(decision.PolicyDecision.AllowedToPlan);
        AssertHandoff(decision.PolicyDecision, ComputerUseHandoffReason.UacAdmin);
    }

    [TestMethod]
    public void EmptyBlockedStateCombinesOcrAndStaleEvent()
    {
        var request = Request(
            FixtureComputerUseSnapshotBuilder.ElectronLikeUiaPoorApp(),
            FixtureWin32ContextFactory.EmptyBlocked(),
            FixtureWindowsUiAutomationEvents.EmptyBlockedStale(),
            ComputerUseVisualObservationFixtures.FromText("empty", "empty blocked unavailable"));

        var decision = new ComputerUsePerceptionFusionPlanner().Plan(request, "wait", ComputerUseActionKind.WaitForElement);

        Assert.IsTrue(decision.Fusion.BlockageDetected);
        Assert.IsTrue(decision.Fusion.EventDerivedConfidence <= 0.55);
        Assert.IsTrue(decision.Fusion.Blockages.Any(b => b.Kind == ComputerUseBlockageKind.HiddenWindowOrModal));
        Assert.IsFalse(decision.PolicyDecision.AllowedToPlan);
    }

    [TestMethod]
    public void DpiMismatchIsWarningHandoff()
    {
        var request = Request(SimpleNotepadSnapshot(), FixtureWin32ContextFactory.DpiMismatch(), FixtureWindowsUiAutomationEvents.NotepadNoBlockage());

        var decision = new ComputerUsePerceptionFusionPlanner().Plan(request, "Text Editor", ComputerUseActionKind.WaitForElement);

        Assert.IsTrue(decision.Fusion.Blockages.Any(b => b.Kind == ComputerUseBlockageKind.DpiMonitorMismatch));
        Assert.IsFalse(decision.PolicyDecision.AllowedToPlan);
        AssertHandoff(decision.PolicyDecision, ComputerUseHandoffReason.LowConfidence);
    }

    [TestMethod]
    public void Win32TitleAndProcessPathAreRedactedInEvidence()
    {
        var win32 = FixtureWin32ContextFactory.LoginActiveWithSensitiveTitle();
        var pack = new ComputerUseReadOnlyContextEvidenceBuilder().BuildWin32ContextEvidence(win32);
        var json = JsonSerializer.Serialize(pack);

        Assert.IsTrue(pack.Redacted);
        Assert.IsFalse(pack.RawScreenshotStored);
        Assert.IsFalse(pack.ClipboardCaptured);
        Assert.IsFalse(pack.ActionAuthority);
        Assert.IsFalse(json.Contains("user@example.com", StringComparison.Ordinal));
        Assert.IsFalse(json.Contains(@"C:\Users\diego", StringComparison.Ordinal));
        Assert.IsTrue(json.Contains("[REDACTED]", StringComparison.Ordinal));
    }

    [TestMethod]
    public void EventPayloadApiKeyTokenAndPasswordAreRedacted()
    {
        var events = new FixtureWindowsUiAutomationEventStream([
            FixtureWindowsUiAutomationEvents.Event("secret-event", WindowsUiAutomationEventKind.SensitiveValueChanged, "field", "Password", "Edit", "Value", "api key=sk-testSecret999 token=ghp_fakeSecretToken999 password=hunter2")
        ]).Read(new WindowsUiAutomationEventStreamOptions("fixture"));

        var pack = new ComputerUseReadOnlyContextEvidenceBuilder().BuildUiaEventEvidence(events);
        var json = JsonSerializer.Serialize(pack);

        Assert.IsFalse(pack.ActionAuthority);
        Assert.IsFalse(pack.EventTriggeredExecution);
        Assert.IsFalse(json.Contains("sk-testSecret999", StringComparison.Ordinal));
        Assert.IsFalse(json.Contains("ghp_fakeSecretToken999", StringComparison.Ordinal));
        Assert.IsFalse(json.Contains("hunter2", StringComparison.Ordinal));
        Assert.IsTrue(json.Contains("[REDACTED]", StringComparison.Ordinal));
    }

    [TestMethod]
    public void EvidencePacksNeverContainRawScreenshotBytesOrClipboard()
    {
        var builder = new ComputerUseReadOnlyContextEvidenceBuilder();
        var win32Pack = builder.BuildWin32ContextEvidence(FixtureWin32ContextFactory.NotepadActive());
        var eventPack = builder.BuildUiaEventEvidence(new FixtureWindowsUiAutomationEventStream(FixtureWindowsUiAutomationEvents.NotepadNoBlockage()).Read(new WindowsUiAutomationEventStreamOptions("fixture")));

        Assert.IsFalse(win32Pack.RawScreenshotStored);
        Assert.IsFalse(eventPack.RawScreenshotStored);
        Assert.IsFalse(win32Pack.ClipboardCaptured);
        Assert.IsFalse(eventPack.ClipboardCaptured);
        Assert.IsFalse(builder.Serialize(win32Pack).Contains("base64", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(builder.Serialize(eventPack).Contains("base64", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void UiaEventsCannotCreateActionAuthorization()
    {
        var request = Request(
            SimpleNotepadSnapshot(),
            FixtureWin32ContextFactory.NotepadActive(),
            FixtureWindowsUiAutomationEvents.ActiveWindowChanged("Untitled - Notepad"));

        var decision = new ComputerUsePerceptionFusionPlanner().Plan(request, "Text Editor", ComputerUseActionKind.WaitForElement);

        Assert.IsTrue(decision.Fusion.EvidenceRefs.Any());
        Assert.IsFalse(decision.Fusion.ActionAuthorityGranted);
        Assert.IsFalse(decision.PolicyDecision.AllowedToExecuteLive);
        Assert.IsFalse(request.UiaEvents!.Events.Any(e => e.CanTriggerExecution || e.ActionAuthority));
    }

    [TestMethod]
    public void WrongActiveWindowLowersConfidenceAndRequiresHandoff()
    {
        var wrongWin32 = FixtureWin32ContextFactory.ElectronActive();
        var request = Request(SimpleNotepadSnapshot(), wrongWin32, FixtureWindowsUiAutomationEvents.ActiveWindowChanged("Electron App"));

        var decision = new ComputerUsePerceptionFusionPlanner().Plan(request, "Text Editor", ComputerUseActionKind.WaitForElement);

        Assert.IsFalse(decision.Fusion.ActiveWindowMatched);
        Assert.IsTrue(decision.Fusion.Blockages.Any(b => b.Kind == ComputerUseBlockageKind.LowConfidenceLocator));
        Assert.IsFalse(decision.PolicyDecision.AllowedToPlan);
        AssertHandoff(decision.PolicyDecision, ComputerUseHandoffReason.LowConfidence);
    }

    private static ComputerUsePerceptionFusionRequest Request(
        ComputerUseSnapshot snapshot,
        Win32ContextCollectionResult win32,
        IReadOnlyList<WindowsUiAutomationEvent> events,
        params RedactedVisualObservation[] observations)
    {
        IComputerUseVisualPerceptionBridge bridge = observations.Length == 0
            ? new ComputerUseVisualPerceptionBridgeDisabled()
            : new ComputerUseFixtureVisualPerceptionBridge(observations);
        var eventState = new FixtureWindowsUiAutomationEventStream(events).Read(new WindowsUiAutomationEventStreamOptions(snapshot.Scenario));

        return new ComputerUsePerceptionFusionRequest(snapshot, bridge.Observe(snapshot), win32, eventState);
    }

    private static ComputerUseSnapshot SimpleNotepadSnapshot() =>
        new(
            SnapshotId: "wcu-fixture-simple-notepad",
            Source: ComputerUseSnapshotSource.Fixture,
            CapturedAtUtc: DateTimeOffset.UnixEpoch,
            Scenario: "simple-notepad-uia-rich",
            Windows:
            [
                new WindowContext(
                    "notepad-main",
                    "notepad",
                    "Untitled - Notepad",
                    "Notepad",
                    new UiElementBounds(0, 0, 1024, 768),
                    IsAllowlisted: true,
                    IsModal: false,
                    IsUacLike: false,
                    IsRemoteDesktopLike: false,
                    DpiScale: 1.0,
                    Elements:
                    [
                        new UiElementNode(
                            new UiElementIdentity("editor", "runtime-editor", "Text Editor", "Edit", "Edit", "notepad", ["notepad", "Edit", "Text Editor"]),
                            new UiElementBounds(10, 10, 800, 600),
                            new UiAutomationPatternCapabilities(SupportsInvoke: false, SupportsValue: true, SupportsScroll: true, SupportsSelection: true, SupportsText: true, SupportsFocus: true),
                            IsVisible: true,
                            IsEnabled: true)
                    ])
            ],
            ClipboardCaptured: false,
            ScreenshotPersisted: false,
            OcrTextPersisted: false,
            RealMouseUsed: false,
            RealKeyboardUsed: false,
            LiveUiaActionUsed: false,
            ShellOrSubprocessUsed: false,
            ProductUiEnabledActions: false,
            Redacted: true);

    private static void AssertHandoff(ComputerUsePolicyDecision decision, ComputerUseHandoffReason expectedReason)
    {
        Assert.IsFalse(decision.AllowedToPlan);
        Assert.IsFalse(decision.AllowedToExecuteLive);
        Assert.AreEqual(ComputerUseActionKind.HumanHandoff, decision.Candidates.Single().ActionKind);
        CollectionAssert.Contains(decision.Candidates.Single().HandoffReasons.ToList(), expectedReason);
    }
}
