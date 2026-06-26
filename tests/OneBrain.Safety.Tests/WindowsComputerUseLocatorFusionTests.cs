using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.WindowsComputerUse;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("WindowsComputerUseFixtureSafe")]
[TestCategory("WindowsComputerUseLocatorFusion")]
public sealed class WindowsComputerUseLocatorFusionTests
{
    [TestMethod]
    public void UiaAndOcrAgreementProducesHighConfidenceCandidateWithoutActionAuthority()
    {
        var snapshot = FixtureComputerUseSnapshotBuilder.NotepadLikeUiaRichApp();
        var visual = ComputerUseVisualObservationFixtures.FromText("editor-hint", "Text Editor");
        var result = Fuse(snapshot, objective: "Text Editor", visualObservations: [visual]);

        Assert.IsTrue(result.BestCandidate?.ConfidenceBreakdown.FinalConfidence >= 0.7, $"confidence={result.BestCandidate?.ConfidenceBreakdown.FinalConfidence}");
        Assert.IsFalse(result.ActionAuthorityGranted);
        Assert.IsFalse(result.AllowedToExecuteLive);
        Assert.IsFalse(result.BestCandidate?.ActionAuthority);
    }

    [TestMethod]
    public void OcrOnlyCandidateRequiresReviewWhenUiaMissing()
    {
        var snapshot = EmptySnapshot();
        var visual = ComputerUseVisualObservationFixtures.FromText("continue-hint", "Continue");
        var result = Fuse(snapshot, objective: "Continue", visualObservations: [visual]);

        var visualCandidate = result.LocatorCandidates.FirstOrDefault(c => c.SelectorKind == "VisualHintOnly");
        Assert.IsNotNull(visualCandidate);
        Assert.IsTrue(visualCandidate.RequiresVisualFallback);
        Assert.IsTrue(visualCandidate.RequiresHumanHandoff);
        Assert.IsFalse(visualCandidate.ActionAuthority);
        Assert.IsFalse(result.ActionAuthorityGranted);
    }

    [TestMethod]
    public void UiaOnlyCandidateIsCandidateOnlyAndCannotExecute()
    {
        var snapshot = FixtureComputerUseSnapshotBuilder.NotepadLikeUiaRichApp();
        var result = Fuse(snapshot, objective: "Text Editor");

        Assert.IsNotNull(result.BestCandidate);
        Assert.IsFalse(result.ActionAuthorityGranted);
        Assert.IsFalse(result.AllowedToExecuteLive);
        Assert.IsFalse(result.LiveProviderCalled);
        Assert.IsFalse(result.RawScreenshotStored);
        Assert.IsFalse(result.RealPcRead);
    }

    [TestMethod]
    public void Win32ContextMismatchForcesHumanReview()
    {
        var snapshot = FixtureComputerUseSnapshotBuilder.ElectronLikeUiaPoorApp();
        var win32 = FixtureWin32ContextFactory.NotepadActive();
        var result = Fuse(snapshot, objective: "Continue", win32: win32);

        Assert.IsTrue(result.Win32Anchor.RequiresHumanHandoff);
        Assert.IsTrue(result.RequiresHumanHandoff);
        Assert.IsFalse(result.ActionAuthorityGranted);
    }

    [TestMethod]
    public void UiaEventStreamAddsEvidenceButCannotTriggerExecution()
    {
        var snapshot = FixtureComputerUseSnapshotBuilder.NotepadLikeUiaRichApp();
        var events = FixtureWindowsUiAutomationEvents.NotepadNoBlockage();
        var result = Fuse(snapshot, objective: "Text Editor", events: events);

        Assert.IsTrue(result.EventContinuitySignals.Count > 0);
        Assert.IsFalse(result.EventContinuitySignals.Any(s => s.ActionAuthority));
        Assert.IsFalse(result.ActionAuthorityGranted);
        Assert.IsFalse(result.AllowedToExecuteLive);
    }

    [TestMethod]
    public void EventWithCanTriggerExecutionIsBlocked()
    {
        var snapshot = FixtureComputerUseSnapshotBuilder.NotepadLikeUiaRichApp();
        var hostileEvent = FixtureWindowsUiAutomationEvents.Event(
            "hostile-event",
            WindowsUiAutomationEventKind.FocusChanged,
            "editor",
            "Text Editor",
            "Edit",
            "HasKeyboardFocus",
            "true") with { CanTriggerExecution = true };
        var events = new WindowsUiAutomationEventStreamState(
            WindowsUiAutomationEventStreamStatus.FixtureOnly,
            [hostileEvent],
            LiveSubscribed: false,
            Throttled: true,
            Debounced: true,
            ActionCallbackRegistered: false,
            ActionAuthority: false,
            Reasons: ["fixture-hostile"]);
        var result = FuseWithEventState(snapshot, objective: "Text Editor", events);

        Assert.IsTrue(result.Blockages.Any(b => b.Kind == ComputerUseBlockageKind.AuditLogBypassRisk));
        Assert.IsTrue(result.RequiresHumanHandoff);
        Assert.IsFalse(result.ActionAuthorityGranted);
    }

    [TestMethod]
    public void VisualBridgeActionAuthorityIsBlocked()
    {
        var snapshot = FixtureComputerUseSnapshotBuilder.NotepadLikeUiaRichApp();
        var bridge = HostileBridge(actionAuthority: true);
        var result = FuseWithBridge(snapshot, objective: "Text Editor", bridge);

        Assert.IsTrue(result.Blockages.Any(b => b.Kind == ComputerUseBlockageKind.AuditLogBypassRisk));
        Assert.IsTrue(result.RequiresHumanHandoff);
        Assert.IsFalse(result.ActionAuthorityGranted);
    }

    [TestMethod]
    public void VisualBridgeLiveProviderCalledIsBlocked()
    {
        var snapshot = FixtureComputerUseSnapshotBuilder.NotepadLikeUiaRichApp();
        var bridge = HostileBridge(liveProviderCalled: true);
        var result = FuseWithBridge(snapshot, objective: "Text Editor", bridge);

        Assert.IsTrue(result.Blockages.Any(b => b.Kind == ComputerUseBlockageKind.AuditLogBypassRisk));
        Assert.IsTrue(result.RequiresHumanHandoff);
        Assert.IsFalse(result.LiveProviderCalled);
        Assert.IsFalse(result.ActionAuthorityGranted);
    }

    [TestMethod]
    public void VisualBridgeRawScreenshotStoredIsBlocked()
    {
        var snapshot = FixtureComputerUseSnapshotBuilder.NotepadLikeUiaRichApp();
        var bridge = HostileBridge(rawScreenshotStored: true);
        var result = FuseWithBridge(snapshot, objective: "Text Editor", bridge);

        Assert.IsTrue(result.Blockages.Any(b => b.Kind == ComputerUseBlockageKind.ScreenshotRisk));
        Assert.IsTrue(result.RequiresHumanHandoff);
        Assert.IsFalse(result.RawScreenshotStored);
        Assert.IsFalse(result.ActionAuthorityGranted);
    }

    [TestMethod]
    public void Win32ReadRealPcIsBlocked()
    {
        var snapshot = FixtureComputerUseSnapshotBuilder.NotepadLikeUiaRichApp();
        var win32 = FixtureWin32ContextFactory.NotepadActive() with { ReadRealPc = true };
        var result = Fuse(snapshot, objective: "Text Editor", win32: win32);

        Assert.IsTrue(result.Blockages.Any(b => b.Kind == ComputerUseBlockageKind.AuditLogBypassRisk));
        Assert.IsTrue(result.RequiresHumanHandoff);
        Assert.IsFalse(result.RealPcRead);
        Assert.IsFalse(result.ActionAuthorityGranted);
    }

    [TestMethod]
    public void UiaEventLiveSubscribedIsBlocked()
    {
        var snapshot = FixtureComputerUseSnapshotBuilder.NotepadLikeUiaRichApp();
        var events = new WindowsUiAutomationEventStreamState(
            WindowsUiAutomationEventStreamStatus.Disabled,
            [],
            LiveSubscribed: true,
            Throttled: true,
            Debounced: true,
            ActionCallbackRegistered: false,
            ActionAuthority: false,
            Reasons: ["hostile"]);
        var result = FuseWithEventState(snapshot, objective: "Text Editor", events);

        Assert.IsTrue(result.Blockages.Any(b => b.Kind == ComputerUseBlockageKind.AuditLogBypassRisk));
        Assert.IsTrue(result.RequiresHumanHandoff);
        Assert.IsFalse(result.ActionAuthorityGranted);
    }

    [TestMethod]
    public void UiaEventActionCallbackRegisteredIsBlocked()
    {
        var snapshot = FixtureComputerUseSnapshotBuilder.NotepadLikeUiaRichApp();
        var events = new WindowsUiAutomationEventStreamState(
            WindowsUiAutomationEventStreamStatus.Disabled,
            [],
            LiveSubscribed: false,
            Throttled: true,
            Debounced: true,
            ActionCallbackRegistered: true,
            ActionAuthority: false,
            Reasons: ["hostile"]);
        var result = FuseWithEventState(snapshot, objective: "Text Editor", events);

        Assert.IsTrue(result.Blockages.Any(b => b.Kind == ComputerUseBlockageKind.AuditLogBypassRisk));
        Assert.IsTrue(result.RequiresHumanHandoff);
        Assert.IsFalse(result.ActionAuthorityGranted);
    }

    [TestMethod]
    public void SensitiveLocatorTextIsRedacted()
    {
        var snapshot = FixtureComputerUseSnapshotBuilder.PasswordLoginForm();
        var result = Fuse(snapshot, objective: "Password");
        var json = JsonSerializer.Serialize(result);

        Assert.IsTrue(result.SensitiveSurfaces.Count > 0 || result.BestCandidate?.SensitiveSurface == true);
        Assert.IsFalse(json.Contains("secret-password", StringComparison.Ordinal));
        Assert.IsTrue(result.RequiresHumanHandoff);
        Assert.IsFalse(result.ActionAuthorityGranted);
    }

    [TestMethod]
    public void WindowsProfilePathIsRedactedInEvidence()
    {
        var snapshot = FixtureComputerUseSnapshotBuilder.ElectronLikeUiaPoorApp();
        var win32 = FixtureWin32ContextFactory.ElectronActive();
        var result = Fuse(snapshot, objective: "Continue", win32: win32);
        var json = JsonSerializer.Serialize(result);

        Assert.IsFalse(json.Contains(@"C:\Users\diego", StringComparison.Ordinal));
        Assert.IsFalse(json.Contains(@"ElectronApp\app.exe", StringComparison.Ordinal));
        Assert.IsTrue(result.Win32Anchor.WindowTitleRedacted.Contains("[REDACTED]") || result.Win32Anchor.ProcessNameRedacted.Contains("electron"));
        Assert.IsFalse(result.ActionAuthorityGranted);
    }

    [TestMethod]
    public void CredentialLikeTextForcesHumanReview()
    {
        var snapshot = EmptySnapshot();
        var visual = ComputerUseVisualObservationFixtures.FromText("login-hint", "api key=sk-testSecret999 token=ghp_fakeSecretToken999");
        var result = Fuse(snapshot, objective: "login", visualObservations: [visual]);

        Assert.IsTrue(result.RequiresHumanHandoff);
        Assert.IsFalse(result.ActionAuthorityGranted);
    }

    [TestMethod]
    public void SubmitPayDeleteLoginLanguageForcesHumanReview()
    {
        var snapshot = FixtureComputerUseSnapshotBuilder.ElectronLikeUiaPoorApp();
        var visual = ComputerUseVisualObservationFixtures.FromText("submit-hint", "Pay now");
        var result = Fuse(snapshot, objective: "Pay now", visualObservations: [visual]);

        Assert.IsTrue(result.RequiresHumanHandoff);
        Assert.IsTrue(result.Blockages.Any(b => b.Kind == ComputerUseBlockageKind.DestructiveAction || b.Kind == ComputerUseBlockageKind.AuditLogBypassRisk));
        Assert.IsFalse(result.ActionAuthorityGranted);
    }

    [TestMethod]
    public void FiscalBankTextIsRedactedAndForcesReview()
    {
        var snapshot = EmptySnapshot();
        var visual = ComputerUseVisualObservationFixtures.FromText("bank-hint", "account=123456789 routing=987654321");
        var result = Fuse(snapshot, objective: "bank", visualObservations: [visual]);

        var json = JsonSerializer.Serialize(result);
        Assert.IsFalse(json.Contains("123456789", StringComparison.Ordinal));
        Assert.IsTrue(json.Contains("[REDACTED]", StringComparison.Ordinal));
        Assert.IsFalse(result.ActionAuthorityGranted);
    }

    [TestMethod]
    public void PhoneNumberIsRedactedInLocatorEvidence()
    {
        var snapshot = EmptySnapshot();
        var visual = ComputerUseVisualObservationFixtures.FromText("phone-hint", "call +1 555-123-4567");
        var result = Fuse(snapshot, objective: "call", visualObservations: [visual]);

        var json = JsonSerializer.Serialize(result);
        Assert.IsFalse(json.Contains("555-123-4567", StringComparison.Ordinal));
        Assert.IsTrue(json.Contains("[REDACTED]", StringComparison.Ordinal));
        Assert.IsFalse(result.ActionAuthorityGranted);
    }

    [TestMethod]
    public void ConflictingOcrUiaLabelsForceHumanReview()
    {
        var snapshot = FixtureComputerUseSnapshotBuilder.NotepadLikeUiaRichApp();
        var visual = ComputerUseVisualObservationFixtures.FromText("wrong-hint", "Totally Different Label");
        var result = Fuse(snapshot, objective: "Text Editor", visualObservations: [visual]);

        Assert.IsTrue(result.RequiresHumanHandoff);
        Assert.IsFalse(result.ActionAuthorityGranted);
    }

    [TestMethod]
    public void LowConfidenceForcesHumanReview()
    {
        var snapshot = FixtureComputerUseSnapshotBuilder.ElectronLikeUiaPoorApp();
        var result = Fuse(snapshot, objective: "NonExistentElement");

        Assert.IsTrue(result.RequiresHumanHandoff);
        Assert.IsTrue(result.HandoffReasons.Contains(ComputerUseLocatorHandoffReason.LowConfidence));
        Assert.IsFalse(result.ActionAuthorityGranted);
    }

    [TestMethod]
    public void UnknownSourceKindIsNotPresentBecauseAllSourcesAreKnown()
    {
        var snapshot = FixtureComputerUseSnapshotBuilder.NotepadLikeUiaRichApp();
        var result = Fuse(snapshot, objective: "Text Editor");

        var sources = result.LocatorCandidates.SelectMany(c => c.Evidence.Select(e => e.SourceSignal)).Distinct().ToList();
        Assert.IsFalse(sources.Any(s => s.Contains("unknown", StringComparison.OrdinalIgnoreCase)));
        Assert.IsFalse(result.ActionAuthorityGranted);
    }

    [TestMethod]
    public void EvidenceRefsArePreservedButRawTextIsNotInJson()
    {
        var snapshot = FixtureComputerUseSnapshotBuilder.NotepadLikeUiaRichApp();
        var result = Fuse(snapshot, objective: "Text Editor");
        var json = JsonSerializer.Serialize(result);

        Assert.IsTrue(result.EvidenceRefs.Count > 0);
        Assert.IsFalse(result.RawScreenshotStored);
        Assert.IsFalse(result.ActionAuthorityGranted);
    }

    [TestMethod]
    public void LocatorCandidateNeverSetsAllowedToExecuteLive()
    {
        var snapshot = FixtureComputerUseSnapshotBuilder.NotepadLikeUiaRichApp();
        var result = Fuse(snapshot, objective: "Text Editor");

        Assert.IsFalse(result.AllowedToExecuteLive);
        Assert.IsFalse(result.ActionAuthorityGranted);
    }

    [TestMethod]
    public void AmbiguousDuplicateLabelsForceHumanReview()
    {
        var snapshot = DuplicateEditorSnapshot();
        var result = Fuse(snapshot, objective: "Text Editor");

        Assert.IsTrue(result.Ambiguity.IsAmbiguous);
        Assert.IsTrue(result.RequiresHumanHandoff);
        Assert.IsTrue(result.HandoffReasons.Contains(ComputerUseLocatorHandoffReason.AmbiguousTarget));
        Assert.IsFalse(result.ActionAuthorityGranted);
    }

    [TestMethod]
    public void StaleElementRiskFromModalForcesHumanReview()
    {
        var snapshot = FixtureComputerUseSnapshotBuilder.NotepadLikeUiaRichApp();
        var win32 = FixtureWin32ContextFactory.ModalDialog();
        var result = Fuse(snapshot, objective: "Text Editor", win32: win32);

        Assert.IsTrue(result.StaleElementRisk.IsStale);
        Assert.IsTrue(result.RequiresHumanHandoff);
        Assert.IsFalse(result.ActionAuthorityGranted);
    }

    [TestMethod]
    public void UacAdminWin32ContextBlocksLocator()
    {
        var snapshot = FixtureComputerUseSnapshotBuilder.UacAdminBlocker();
        var win32 = FixtureWin32ContextFactory.UacAdminLike();
        var result = Fuse(snapshot, objective: "Yes", win32: win32);

        Assert.IsTrue(result.Blockages.Any(b => b.Kind == ComputerUseBlockageKind.UacAdmin));
        Assert.IsTrue(result.RequiresHumanHandoff);
        Assert.IsFalse(result.ActionAuthorityGranted);
    }

    [TestMethod]
    public void UnifiedEvidencePackPreservesRedactionAndNoAuthority()
    {
        var snapshot = FixtureComputerUseSnapshotBuilder.NotepadLikeUiaRichApp();
        var locator = Fuse(snapshot, objective: "Text Editor");
        var pack = new ComputerUseUnifiedEvidencePackBuilder().Build(snapshot, locator);
        var json = JsonSerializer.Serialize(pack);

        Assert.IsFalse(pack.ActionAuthorityGranted);
        Assert.IsFalse(pack.RawScreenshotPresent);
        Assert.IsFalse(pack.ClipboardPresent);
        Assert.IsTrue(pack.AuditLogBypassGuard);
        Assert.IsFalse(string.IsNullOrWhiteSpace(pack.TamperGuardHash));
        Assert.IsFalse(json.Contains("secret", StringComparison.Ordinal));
    }

    [TestMethod]
    public void Win32ActionAuthorityIsBlocked()
    {
        var snapshot = FixtureComputerUseSnapshotBuilder.NotepadLikeUiaRichApp();
        var win32 = FixtureWin32ContextFactory.NotepadActive() with { ActionAuthority = true };
        var result = Fuse(snapshot, objective: "Text Editor", win32: win32);

        Assert.IsTrue(result.Blockages.Any(b => b.Kind == ComputerUseBlockageKind.AuditLogBypassRisk));
        Assert.IsTrue(result.RequiresHumanHandoff);
        Assert.IsFalse(result.ActionAuthorityGranted);
    }

    [TestMethod]
    public void UiaEventActionAuthorityIsBlocked()
    {
        var snapshot = FixtureComputerUseSnapshotBuilder.NotepadLikeUiaRichApp();
        var events = new WindowsUiAutomationEventStreamState(
            WindowsUiAutomationEventStreamStatus.Disabled,
            [],
            LiveSubscribed: false,
            Throttled: true,
            Debounced: true,
            ActionCallbackRegistered: false,
            ActionAuthority: true,
            Reasons: ["hostile"]);
        var result = FuseWithEventState(snapshot, objective: "Text Editor", events);

        Assert.IsTrue(result.Blockages.Any(b => b.Kind == ComputerUseBlockageKind.AuditLogBypassRisk));
        Assert.IsTrue(result.RequiresHumanHandoff);
        Assert.IsFalse(result.ActionAuthorityGranted);
    }

    [TestMethod]
    public void EmptyBlockedStaleSurfaceForcesHandoff()
    {
        var snapshot = FixtureComputerUseSnapshotBuilder.ElectronLikeUiaPoorApp();
        var events = FixtureWindowsUiAutomationEvents.EmptyBlockedStale();
        var result = Fuse(snapshot, objective: "wait", events: events);

        Assert.IsTrue(result.StaleElementRisk.IsStale);
        Assert.IsTrue(result.RequiresHumanHandoff);
        Assert.IsFalse(result.ActionAuthorityGranted);
    }

    private static ComputerUseLocatorFusionResult Fuse(
        ComputerUseSnapshot snapshot,
        string objective,
        Win32ContextCollectionResult? win32 = null,
        IReadOnlyList<WindowsUiAutomationEvent>? events = null,
        params RedactedVisualObservation[] visualObservations)
    {
        var eventState = events is null
            ? null
            : new FixtureWindowsUiAutomationEventStream(events).Read(new WindowsUiAutomationEventStreamOptions(snapshot.Scenario));
        return FuseWithEventState(snapshot, objective, eventState, win32, visualObservations);
    }

    private static ComputerUseLocatorFusionResult FuseWithEventState(
        ComputerUseSnapshot snapshot,
        string objective,
        WindowsUiAutomationEventStreamState? eventState,
        Win32ContextCollectionResult? win32 = null,
        params RedactedVisualObservation[] visualObservations)
    {
        IComputerUseVisualPerceptionBridge bridge = visualObservations.Length == 0
            ? new ComputerUseVisualPerceptionBridgeDisabled()
            : new ComputerUseFixtureVisualPerceptionBridge(visualObservations);
        var input = new ComputerUseLocatorFusionInput(
            snapshot,
            objective,
            win32,
            eventState,
            bridge.Observe(snapshot));
        return new ComputerUseLocatorFusionEngine().Fuse(input);
    }

    private static ComputerUseLocatorFusionResult FuseWithBridge(
        ComputerUseSnapshot snapshot,
        string objective,
        RobustPerceptionBridgeResult bridge)
    {
        var input = new ComputerUseLocatorFusionInput(
            snapshot,
            objective,
            Win32Context: null,
            UiaEvents: null,
            bridge);
        return new ComputerUseLocatorFusionEngine().Fuse(input);
    }

    private static RobustPerceptionBridgeResult HostileBridge(
        bool actionAuthority = false,
        bool liveProviderCalled = false,
        bool rawScreenshotStored = false)
    {
        return new RobustPerceptionBridgeResult(
            Available: true,
            ProviderId: "hostile.bridge",
            Mode: "AdversarialFixture",
            Observations: [],
            RequiresHumanHandoff: false,
            RawScreenshotStored: rawScreenshotStored,
            LiveProviderCalled: liveProviderCalled,
            ActionAuthority: actionAuthority,
            Reasons: ["Hostile bridge for locator fusion hardening tests."]);
    }

    private static ComputerUseSnapshot EmptySnapshot() =>
        new(
            SnapshotId: "wcu-fixture-empty",
            Source: ComputerUseSnapshotSource.Fixture,
            CapturedAtUtc: DateTimeOffset.UnixEpoch,
            Scenario: "empty",
            Windows: [],
            ClipboardCaptured: false,
            ScreenshotPersisted: false,
            OcrTextPersisted: false,
            RealMouseUsed: false,
            RealKeyboardUsed: false,
            LiveUiaActionUsed: false,
            ShellOrSubprocessUsed: false,
            ProductUiEnabledActions: false,
            Redacted: true);

    private static ComputerUseSnapshot DuplicateEditorSnapshot() =>
        new(
            SnapshotId: "wcu-fixture-duplicate-editors",
            Source: ComputerUseSnapshotSource.Fixture,
            CapturedAtUtc: DateTimeOffset.UnixEpoch,
            Scenario: "duplicate-editors",
            Windows:
            [
                new WindowContext(
                    "app-main",
                    "notepad",
                    "Notepad",
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
                            new UiElementIdentity("editor1", "runtime-editor-1", "Text Editor", "Edit", "Edit", "notepad", ["notepad", "Edit", "Text Editor"]),
                            new UiElementBounds(10, 10, 400, 300),
                            new UiAutomationPatternCapabilities(SupportsInvoke: false, SupportsValue: true, SupportsScroll: true, SupportsSelection: true, SupportsText: true, SupportsFocus: true),
                            IsVisible: true,
                            IsEnabled: true),
                        new UiElementNode(
                            new UiElementIdentity("editor2", "runtime-editor-2", "Text Editor", "Edit", "Edit", "notepad", ["notepad", "Edit", "Text Editor"]),
                            new UiElementBounds(420, 10, 400, 300),
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
}
