using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.WindowsComputerUse;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("WindowsComputerUseFixtureSafe")]
[TestCategory("WindowsComputerUseLocatorFusionEvidence")]
public sealed class WindowsComputerUseLocatorFusionEvidenceTests
{
    [TestMethod]
    public void StableAutomationIdWithWin32ProcessMatchGetsHighConfidenceButNoActionAuthority()
    {
        var snapshot = BasicSnapshot(Element("continueButton", "runtime-continue", "Continue", "Button", invoke: true));
        var result = Fuse(snapshot, "Continue", MatchingWin32(), FixtureWindowsUiAutomationEvents.NotepadNoBlockage());
        var plan = new ComputerUseSafeActionPlanner().Plan(snapshot, "Continue", ComputerUseActionKind.Click);
        var pack = new ComputerUseUnifiedEvidencePackBuilder().Build(snapshot, result, plan);

        Assert.IsNotNull(result.BestCandidate);
        Assert.AreEqual("AutomationId+Process+WindowContext", result.BestCandidate!.SelectorKind);
        Assert.IsTrue(result.BestCandidate.ConfidenceBreakdown.FinalConfidence >= 0.85);
        Assert.IsFalse(result.ActionAuthorityGranted);
        Assert.IsFalse(result.BestCandidate.ActionAuthority);
        Assert.IsFalse(pack.ActionAuthorityGranted);
        Assert.IsFalse(plan.AllowedToExecuteLive);
    }

    [TestMethod]
    public void DuplicateButtonsWithSameNameCreateAmbiguityAndHandoff()
    {
        var snapshot = BasicSnapshot(
            Element("continueA", "runtime-a", "Continue", "Button", invoke: true),
            Element("continueB", "runtime-b", "Continue", "Button", invoke: true));

        var result = Fuse(snapshot, "Continue", MatchingWin32(), FixtureWindowsUiAutomationEvents.NotepadNoBlockage());
        var json = JsonSerializer.Serialize(result);

        Assert.IsTrue(result.Ambiguity.IsAmbiguous);
        Assert.AreEqual(2, result.Ambiguity.CompetingCandidateIds.Count);
        CollectionAssert.Contains(result.HandoffReasons.ToList(), ComputerUseLocatorHandoffReason.AmbiguousTarget);
        Assert.IsTrue(result.RequiresHumanHandoff);
        Assert.IsFalse(json.Contains("user@example.com", StringComparison.Ordinal));
    }

    [TestMethod]
    public void RuntimeIdChangedButAncestryStableGetsMediumConfidence()
    {
        var snapshot = BasicSnapshot(Element("", "runtime-new", "Continue", "Button", invoke: true));

        var result = Fuse(snapshot, "Continue", MatchingWin32(), FixtureWindowsUiAutomationEvents.NotepadNoBlockage());

        Assert.IsNotNull(result.BestCandidate);
        Assert.AreEqual("RuntimeId+Ancestry", result.BestCandidate!.SelectorKind);
        Assert.IsTrue(result.BestCandidate.ConfidenceBreakdown.FinalConfidence >= 0.6);
        Assert.IsTrue(result.BestCandidate.ConfidenceBreakdown.FinalConfidence < 0.9);
        Assert.IsFalse(result.ActionAuthorityGranted);
    }

    [TestMethod]
    public void OcrTextMatchWithoutUiaTargetIsVisualHintOnlyAndNoAction()
    {
        var snapshot = BasicSnapshot([]);
        var visual = ComputerUseVisualObservationFixtures.FromText("ocr-continue", "Continue");

        var result = Fuse(snapshot, "Continue", MatchingWin32(), [], visual);
        var plan = new ComputerUseSafeActionPlanner().Plan(snapshot, "Continue", ComputerUseActionKind.Click);
        var pack = new ComputerUseUnifiedEvidencePackBuilder().Build(snapshot, result, plan);

        Assert.AreEqual("VisualHintOnly", result.BestCandidate?.SelectorKind);
        Assert.IsTrue(result.VisualFallbackRequired);
        Assert.IsTrue(result.RequiresHumanHandoff);
        Assert.IsFalse(result.ActionAuthorityGranted);
        Assert.IsFalse(pack.ActionAuthorityGranted);
        Assert.IsFalse(plan.AllowedToExecuteLive);
    }

    [TestMethod]
    public void ModalAppearedAfterSnapshotCreatesStaleRiskAndHandoff()
    {
        var snapshot = BasicSnapshot(Element("continueButton", "runtime-continue", "Continue", "Button", invoke: true));

        var result = Fuse(snapshot, "Continue", MatchingWin32(), FixtureWindowsUiAutomationEvents.ModalAppeared());

        Assert.IsTrue(result.StaleElementRisk.IsStale);
        CollectionAssert.Contains(result.HandoffReasons.ToList(), ComputerUseLocatorHandoffReason.StaleElement);
        CollectionAssert.Contains(result.HandoffReasons.ToList(), ComputerUseLocatorHandoffReason.BlockageDetected);
        Assert.IsTrue(result.RequiresHumanHandoff);
    }

    [TestMethod]
    public void WindowProcessChangedCreatesStaleRiskAndHandoff()
    {
        var snapshot = BasicSnapshot(Element("continueButton", "runtime-continue", "Continue", "Button", invoke: true));

        var result = Fuse(snapshot, "Continue", FixtureWin32ContextFactory.ElectronActive(), FixtureWindowsUiAutomationEvents.ActiveWindowChanged("Electron App"));

        Assert.IsFalse(result.Win32Anchor.ActiveWindowMatched);
        Assert.IsTrue(result.StaleElementRisk.IsStale);
        CollectionAssert.Contains(result.HandoffReasons.ToList(), ComputerUseLocatorHandoffReason.StaleElement);
        Assert.IsFalse(result.ActionAuthorityGranted);
    }

    [TestMethod]
    public void SensitiveFieldTargetIsBlockedAndRedacted()
    {
        var snapshot = BasicSnapshot(Element("password", "runtime-password", "Password", "Edit", value: true, password: true, credential: true));

        var result = Fuse(snapshot, "Password", MatchingWin32(), FixtureWindowsUiAutomationEvents.LoginSensitiveValueChanged());
        var pack = new ComputerUseUnifiedEvidencePackBuilder().Build(snapshot, result);
        var json = JsonSerializer.Serialize(pack);

        CollectionAssert.Contains(result.HandoffReasons.ToList(), ComputerUseLocatorHandoffReason.SensitiveSurface);
        Assert.IsTrue(result.BestCandidate!.SensitiveSurface);
        Assert.IsFalse(result.ActionAuthorityGranted);
        Assert.IsFalse(json.Contains("hunter2", StringComparison.Ordinal));
        Assert.IsFalse(json.Contains("sk-testSecret999", StringComparison.Ordinal));
    }

    [TestMethod]
    public void UacAdminLikeBlockerAlwaysHandoff()
    {
        var result = Fuse(FixtureComputerUseSnapshotBuilder.UacAdminBlocker(), "Yes", FixtureWin32ContextFactory.UacAdminLike(), FixtureWindowsUiAutomationEvents.UacBlocked());

        CollectionAssert.Contains(result.HandoffReasons.ToList(), ComputerUseLocatorHandoffReason.UacAdmin);
        Assert.IsTrue(result.RequiresHumanHandoff);
        Assert.IsFalse(result.ActionAuthorityGranted);
    }

    [TestMethod]
    public void EmailInWindowTitleAndUsernamePathAreRedactedInEvidence()
    {
        var snapshot = FixtureComputerUseSnapshotBuilder.PasswordLoginForm();
        var win32 = FixtureWin32ContextFactory.LoginActiveWithSensitiveTitle();
        var result = Fuse(snapshot, "Sign in", win32, FixtureWindowsUiAutomationEvents.NotepadNoBlockage());
        var contextPack = new ComputerUseReadOnlyContextEvidenceBuilder().BuildWin32ContextEvidence(win32);
        var unifiedPack = new ComputerUseUnifiedEvidencePackBuilder().Build(snapshot, result);
        var json = JsonSerializer.Serialize(new { result, contextPack, unifiedPack });

        Assert.IsFalse(json.Contains("user@example.com", StringComparison.Ordinal));
        Assert.IsFalse(json.Contains(@"C:\Users\diego", StringComparison.Ordinal));
        Assert.IsTrue(json.Contains("[REDACTED]", StringComparison.Ordinal));
    }

    [TestMethod]
    public void ApiKeyTokenJwtInUiaEventPayloadAreRedacted()
    {
        var state = new FixtureWindowsUiAutomationEventStream([
            FixtureWindowsUiAutomationEvents.Event("secret-event", WindowsUiAutomationEventKind.SensitiveValueChanged, "field", "Password", "Edit", "Value", "api_key=sk-testSecret999 token=ghp_fakeSecretToken999 jwt=eyJabc.eyJdef.sig")
        ]).Read(new WindowsUiAutomationEventStreamOptions("fixture"));

        var pack = new ComputerUseReadOnlyContextEvidenceBuilder().BuildUiaEventEvidence(state);
        var json = JsonSerializer.Serialize(pack);

        Assert.IsFalse(json.Contains("sk-testSecret999", StringComparison.Ordinal));
        Assert.IsFalse(json.Contains("ghp_fakeSecretToken999", StringComparison.Ordinal));
        Assert.IsFalse(json.Contains("eyJabc.eyJdef.sig", StringComparison.Ordinal));
        Assert.IsTrue(json.Contains("[REDACTED]", StringComparison.Ordinal));
    }

    [TestMethod]
    public void OtpPasswordCardAndSsnInVisualHintsAreRedacted()
    {
        var ocr = ComputerUseVisualObservationFixtures.FromText("ocr-secret", "otp=123456 password=hunter2 card 4111 1111 1111 1111 ssn 123-45-6789");
        var snapshot = BasicSnapshot([]);

        var result = Fuse(snapshot, "Continue", MatchingWin32(), [], ocr);
        var pack = new ComputerUseUnifiedEvidencePackBuilder().Build(snapshot, result);
        var json = JsonSerializer.Serialize(new { ocr, result, pack });

        Assert.IsFalse(json.Contains("123456", StringComparison.Ordinal));
        Assert.IsFalse(json.Contains("hunter2", StringComparison.Ordinal));
        Assert.IsFalse(json.Contains("4111 1111 1111 1111", StringComparison.Ordinal));
        Assert.IsFalse(json.Contains("123-45-6789", StringComparison.Ordinal));
        Assert.IsTrue(json.Contains("[REDACTED]", StringComparison.Ordinal));
    }

    [TestMethod]
    public void DuplicateTargetAmbiguityDoesNotLeakSensitiveLabels()
    {
        var snapshot = BasicSnapshot(
            Element("deleteA", "runtime-delete-a", "Delete user@example.com", "Button", invoke: true),
            Element("deleteB", "runtime-delete-b", "Delete user@example.com", "Button", invoke: true));

        var result = Fuse(snapshot, "Delete", MatchingWin32(), FixtureWindowsUiAutomationEvents.NotepadNoBlockage());
        var json = JsonSerializer.Serialize(result);

        Assert.IsTrue(result.Ambiguity.IsAmbiguous);
        Assert.IsFalse(json.Contains("user@example.com", StringComparison.Ordinal));
        Assert.IsTrue(json.Contains("[REDACTED]", StringComparison.Ordinal));
    }

    [TestMethod]
    public void UnifiedEvidencePackContainsNoRawScreenshotBytesOrClipboardData()
    {
        var snapshot = BasicSnapshot(Element("continueButton", "runtime-continue", "Continue", "Button", invoke: true));
        var result = Fuse(snapshot, "Continue", MatchingWin32(), FixtureWindowsUiAutomationEvents.NotepadNoBlockage());

        var pack = new ComputerUseUnifiedEvidencePackBuilder().Build(snapshot, result);
        var json = JsonSerializer.Serialize(pack);

        Assert.IsFalse(pack.RawScreenshotPresent);
        Assert.IsFalse(pack.ClipboardPresent);
        Assert.IsFalse(json.Contains("base64", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(json.Contains("clipboard-data", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void EventDerivedAndWin32DerivedSignalsCannotAuthorizeAction()
    {
        var snapshot = BasicSnapshot(Element("continueButton", "runtime-continue", "Continue", "Button", invoke: true));

        var result = Fuse(snapshot, "Continue", MatchingWin32(), FixtureWindowsUiAutomationEvents.ActiveWindowChanged("Fixture App"));

        Assert.IsTrue(result.Win32Anchor.ActiveWindowMatched);
        Assert.IsTrue(result.EventContinuitySignals.Any());
        Assert.IsFalse(result.Win32Anchor.ActionAuthority);
        Assert.IsFalse(result.EventContinuitySignals.Any(e => e.ActionAuthority));
        Assert.IsFalse(result.ActionAuthorityGranted);
    }

    [TestMethod]
    public void PaymentDeleteSubmitVisualRiskAlwaysBlockedHandoff()
    {
        var snapshot = BasicSnapshot([]);
        var visual = ComputerUseVisualObservationFixtures.FromText("pay-delete-submit", "Pay submit delete card=4111111111111111");

        var result = Fuse(snapshot, "Pay", MatchingWin32(), [], visual);

        CollectionAssert.Contains(result.HandoffReasons.ToList(), ComputerUseLocatorHandoffReason.SensitiveSurface);
        CollectionAssert.Contains(result.HandoffReasons.ToList(), ComputerUseLocatorHandoffReason.BlockageDetected);
        Assert.IsTrue(result.Blockages.Any(b => b.Kind == ComputerUseBlockageKind.DestructiveAction));
        Assert.IsFalse(result.ActionAuthorityGranted);
    }

    [TestMethod]
    public void NonBestCandidateSensitiveEvidenceIsCapturedInUnifiedPack()
    {
        var snapshot = BasicSnapshot(
            Element("continueButton", "runtime-continue", "Continue", "Button", invoke: true),
            Element("", "runtime-delete", "Delete user@example.com", "Button", invoke: true));

        var result = Fuse(snapshot, "Continue", MatchingWin32(), FixtureWindowsUiAutomationEvents.NotepadNoBlockage());
        var pack = new ComputerUseUnifiedEvidencePackBuilder().Build(snapshot, result);

        Assert.IsTrue(result.LocatorCandidates.Count >= 2, "Expected multiple candidates.");
        Assert.AreEqual("continueButton", result.BestCandidate?.Identity?.AutomationId, "Expected the non-sensitive candidate to rank first.");
        Assert.IsTrue(pack.SensitiveFieldsRedacted.Contains("email", StringComparer.OrdinalIgnoreCase), "Sensitive field from a non-best candidate must be consolidated in the unified evidence pack.");
        Assert.IsFalse(pack.ActionAuthorityGranted);
    }

    private static ComputerUseLocatorFusionResult Fuse(
        ComputerUseSnapshot snapshot,
        string objective,
        Win32ContextCollectionResult win32,
        IReadOnlyList<WindowsUiAutomationEvent> events,
        params RedactedVisualObservation[] observations)
    {
        IComputerUseVisualPerceptionBridge bridge = observations.Length == 0
            ? new ComputerUseVisualPerceptionBridgeDisabled()
            : new ComputerUseFixtureVisualPerceptionBridge(observations);
        var eventState = new FixtureWindowsUiAutomationEventStream(events).Read(new WindowsUiAutomationEventStreamOptions(snapshot.Scenario));

        return new ComputerUseLocatorFusionEngine().Fuse(new ComputerUseLocatorFusionInput(
            snapshot,
            objective,
            win32,
            eventState,
            bridge.Observe(snapshot)));
    }

    private static ComputerUseSnapshot BasicSnapshot(params UiElementNode[] elements) =>
        BasicSnapshot((IReadOnlyList<UiElementNode>)elements);

    private static ComputerUseSnapshot BasicSnapshot(IReadOnlyList<UiElementNode> elements) =>
        new(
            SnapshotId: "wcu-fixture-locator-basic",
            Source: ComputerUseSnapshotSource.Fixture,
            CapturedAtUtc: DateTimeOffset.UnixEpoch,
            Scenario: "locator-fusion-basic",
            Windows:
            [
                new WindowContext(
                    "fixture-main",
                    "fixture-app",
                    "Fixture App",
                    "FixtureWindow",
                    new UiElementBounds(0, 0, 1024, 768),
                    IsAllowlisted: true,
                    IsModal: false,
                    IsUacLike: false,
                    IsRemoteDesktopLike: false,
                    DpiScale: 1.0,
                    elements)
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

    private static UiElementNode Element(
        string automationId,
        string runtimeId,
        string name,
        string controlType,
        bool invoke = false,
        bool value = false,
        bool password = false,
        bool credential = false) =>
        new(
            new UiElementIdentity(automationId, runtimeId, name, controlType, controlType, "fixture-app", ["fixture-app", controlType, name]),
            new UiElementBounds(10, 10, 120, 32),
            new UiAutomationPatternCapabilities(invoke, value, SupportsScroll: false, SupportsSelection: false, SupportsText: value, SupportsFocus: true),
            IsVisible: true,
            IsEnabled: true,
            IsPasswordField: password,
            IsCredentialField: credential);

    private static Win32ContextCollectionResult MatchingWin32()
    {
        var active = new Win32WindowContext(
            new Win32WindowIdentity("hwnd-fixture", "Fixture App", "FixtureWindow", IsForeground: true, IsTopLevel: true, IsVisible: true, IsEnabled: true),
            new Win32ProcessContext(ProcessId: 1234, "fixture-app", @"C:\Program Files\Fixture\fixture.exe", IsAllowlisted: true),
            new Win32MonitorContext("fixture-monitor", 0, 0, 1920, 1080, IsPrimary: true),
            new Win32DpiContext(1.0, 96, 96, MismatchDetected: false),
            Win32WindowPlacement.Normal,
            new Win32ModalContext(IsModal: false, OwnerHwndOpaque: null, "No modal owner."),
            ZOrderIndex: 0,
            ActionAuthority: false,
            Redacted: true,
            EvidenceRefs: ["win32:hwnd-fixture:redacted"]);

        return new Win32ContextCollectionResult(
            Win32ContextCollectionStatus.FixtureOnly,
            active,
            [active],
            ReadRealPc: false,
            WindowManipulationUsed: false,
            FocusStealingUsed: false,
            InputInjectionUsed: false,
            ClipboardUsed: false,
            ScreenshotCaptured: false,
            ActionAuthority: false,
            Reasons: ["Fixture Win32 context metadata only."]);
    }
}
