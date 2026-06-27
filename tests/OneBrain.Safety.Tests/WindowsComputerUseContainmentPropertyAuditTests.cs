using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.WindowsComputerUse;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("WindowsComputerUseFixtureSafe")]
[TestCategory("WindowsComputerUseContainmentPropertyAudit")]
public sealed class WindowsComputerUseContainmentPropertyAuditTests
{
    private static readonly string[] RequiredProperties =
    [
        "NO_DESKTOP_LIVE_AUTOMATION",
        "NO_REAL_MOUSE_KEYBOARD_CONTROL",
        "NO_REAL_SCREENSHOT_CAPTURE",
        "NO_P_INVOKE_OR_DLLIMPORT",
        "NO_FLAUI_OR_LIVE_UIA",
        "NO_CLIPBOARD_REAL",
        "NO_BROWSER_LIVE_AUTOMATION",
        "NO_CDP_LIVE_EXECUTION",
        "NO_SAFE_INJECTION_LIVE",
        "NO_LOCATOR_ACTION_AUTHORITY",
        "NO_EVENT_TRIGGERED_EXECUTION",
        "NO_OCR_ACTION_AUTHORITY",
        "NO_WIN32_ACTION_AUTHORITY",
        "NO_EVIDENCE_ACTION_AUTHORITY",
        "NO_PROVIDER_NETWORK_LIVE",
        "NO_PUBLIC_RELEASE_UNLOCK",
        "NO_PAID_BETA_UNLOCK"
    ];

    [TestMethod]
    public void ContainmentPropertyCatalogRepresentsAllRequiredNegativeProperties()
    {
        var ids = ComputerUseContainmentPropertyCatalog.Properties.Select(p => p.Id).ToArray();

        CollectionAssert.AreEquivalent(RequiredProperties, ids);
        Assert.IsTrue(ComputerUseContainmentPropertyCatalog.Properties.All(p =>
            !string.IsNullOrWhiteSpace(p.SourceOfTruth) &&
            !string.IsNullOrWhiteSpace(p.ExpectedScan) &&
            !string.IsNullOrWhiteSpace(p.EvidenceAllowed) &&
            !string.IsNullOrWhiteSpace(p.EvidenceProhibited) &&
            !string.IsNullOrWhiteSpace(p.FailureMode) &&
            !string.IsNullOrWhiteSpace(p.Blocks)));
    }

    [TestMethod]
    public void RedactorRemovesCredentialPhoneFiscalBankAndCustomerIdentifiers()
    {
        var redactor = new ComputerUseEvidenceRedactor();
        var input = "email=user@example.com password=hunter2 api_key=sk-testSecret999 token=ghp_fakeSecretToken999 jwt=eyJabc.eyJdef.sig otp=123456 phone +1 212 555 0199 iban=GB82WEST12345698765432 tax id=CUIT-20-12345678-9 path=C:\\Users\\diego\\Customer\\file.txt card 4111 1111 1111 1111 ssn 123-45-6789";

        var result = redactor.Redact(input);

        AssertNoLeak(result.Value, "user@example.com", "hunter2", "sk-testSecret999", "ghp_fakeSecretToken999", "eyJabc.eyJdef.sig", "123456", "+1 212 555 0199", "GB82WEST12345698765432", @"C:\Users\diego", "4111 1111 1111 1111", "123-45-6789");
        CollectionAssert.IsSubsetOf(new[] { "email", "password", "api-key", "token", "jwt", "otp", "phone-number", "fiscal-bank", "windows-user-profile", "credit-card", "ssn" }, result.SensitiveFieldsRedacted.ToArray());
    }

    [TestMethod]
    public void EvidencePacksNeverContainRawScreenshotClipboardSecretsOrDisabledTamperGuard()
    {
        var snapshot = Snapshot(Element("continueButton", "runtime-continue", "Continue", "Button", invoke: true));
        var visual = ComputerUseVisualObservationFixtures.FromText("secret-hint", "email=user@example.com token=ghp_fakeSecretToken999 phone +1 212 555 0199 iban=GB82WEST12345698765432");
        var locator = Fuse(snapshot, "Continue", FixtureWin32ContextFactory.NotepadActive(), FixtureWindowsUiAutomationEvents.NotepadNoBlockage(), visual);
        var plan = new ComputerUseSafeActionPlanner().Plan(snapshot, "Continue", ComputerUseActionKind.Click);
        var unified = new ComputerUseUnifiedEvidencePackBuilder().Build(snapshot, locator, plan);
        var context = new ComputerUseReadOnlyContextEvidenceBuilder().BuildWin32ContextEvidence(FixtureWin32ContextFactory.LoginActiveWithSensitiveTitle());
        var json = JsonSerializer.Serialize(new { unified, context });

        Assert.IsFalse(unified.RawScreenshotPresent);
        Assert.IsFalse(unified.ClipboardPresent);
        Assert.IsFalse(unified.ActionAuthorityGranted);
        Assert.IsFalse(string.IsNullOrWhiteSpace(unified.TamperGuardHash));
        Assert.IsTrue(unified.AuditLogBypassGuard);
        Assert.IsFalse(context.RawScreenshotStored);
        Assert.IsFalse(context.ClipboardCaptured);
        Assert.IsFalse(context.ActionAuthority);
        AssertNoLeak(json, "user@example.com", "ghp_fakeSecretToken999", "+1 212 555 0199", "GB82WEST12345698765432", @"C:\Users\diego", "base64", "clipboard-data");
    }

    [TestMethod]
    public void AuthoritySourcesRemainZeroEvenWithHighConfidenceSignals()
    {
        var snapshot = Snapshot(Element("continueButton", "runtime-continue", "Continue", "Button", invoke: true));
        var visual = new ComputerUseFixtureVisualPerceptionBridge([
            ComputerUseVisualObservationFixtures.FromElement("continue", "Continue", "button", VisualSignalConfidence.VerifiedFixture)
        ]).Observe(snapshot);
        var events = new FixtureWindowsUiAutomationEventStream(FixtureWindowsUiAutomationEvents.NotepadNoBlockage()).Read(new WindowsUiAutomationEventStreamOptions("fixture"));
        var win32 = FixtureWin32ContextFactory.NotepadActive();
        var locator = new ComputerUseLocatorFusionEngine().Fuse(new ComputerUseLocatorFusionInput(snapshot, "Continue", win32, events, visual));
        var evidence = new ComputerUseUnifiedEvidencePackBuilder().Build(snapshot, locator);
        var gate = ComputerUseReadOnlyLiveGateCatalog.Evaluate(new ComputerUseReadOnlyLiveGateRequest());
        var audit = ComputerUseExternalAuditReconciliation.Current();

        Assert.IsTrue(locator.BestCandidate?.ConfidenceBreakdown.FinalConfidence >= 0.85);
        Assert.IsFalse(visual.ActionAuthority);
        Assert.IsFalse(visual.LiveProviderCalled);
        Assert.IsFalse(events.ActionAuthority);
        Assert.IsFalse(events.Events.Any(e => e.CanTriggerExecution || e.ActionAuthority));
        Assert.IsFalse(win32.ActionAuthority);
        Assert.IsFalse(locator.ActionAuthorityGranted);
        Assert.IsFalse(locator.AllowedToExecuteLive);
        Assert.IsFalse(locator.BestCandidate!.ActionAuthority);
        Assert.IsFalse(evidence.ActionAuthorityGranted);
        Assert.IsFalse(gate.LiveReadPermitted);
        Assert.IsFalse(gate.ActionAuthorityGranted);
        Assert.IsFalse(gate.ProductAutomationEnabled);
        Assert.IsFalse(audit.LiveReadPermitted);
        Assert.IsFalse(audit.ActionAuthorityGranted);
        Assert.IsFalse(audit.ProductAutomationEnabled);
    }

    [TestMethod]
    public void OcrOnlyTargetAndEventDerivedTargetRemainHandoffOnly()
    {
        var snapshot = Snapshot([]);
        var ocrOnly = ComputerUseVisualObservationFixtures.FromElement("submit", "Submit", "button", VisualSignalConfidence.High);
        var locator = Fuse(snapshot, "Submit", FixtureWin32ContextFactory.NotepadActive(), FixtureWindowsUiAutomationEvents.ActiveWindowChanged("Fixture App"), ocrOnly);

        Assert.AreEqual("VisualHintOnly", locator.BestCandidate?.SelectorKind);
        Assert.IsTrue(locator.RequiresHumanHandoff);
        Assert.IsTrue(locator.VisualFallbackRequired);
        Assert.IsTrue(locator.EventContinuitySignals.Count > 0);
        Assert.IsFalse(locator.ActionAuthorityGranted);
        Assert.IsFalse(locator.AllowedToExecuteLive);
        Assert.IsFalse(locator.EventContinuitySignals.Any(e => e.ActionAuthority));
        Assert.IsFalse(locator.VisualHintMatches.Any(h => h.ActionAuthority));
    }

    [TestMethod]
    public void DocsAndReportsDoNotConvertContainmentPassIntoLiveGo()
    {
        var repoRoot = FindRepoRoot();
        var qaReport = File.ReadAllText(Path.Combine(repoRoot, "docs", "qa", "computer-use", "wcu-containment-property-audit-001-redaction-evidence-no-live", "report.json"));
        var noGoReport = File.ReadAllText(Path.Combine(repoRoot, "docs", "qa", "computer-use", "wcu-037a-external-audit-nogo-reconciliation", "report.json"));
        var nextPrompt = File.ReadAllText(Path.Combine(repoRoot, "docs", "prompts", "computer-use", "next-wcu-containment-property-audit-002-prompt.md"));
        var blockedPrompt = File.ReadAllText(Path.Combine(repoRoot, "docs", "prompts", "computer-use", "next-wcu-read-only-live-prototype-gated-prompt.md"));

        AssertNoLiveGo(qaReport);
        AssertNoLiveGo(noGoReport);
        AssertNoLiveGo(nextPrompt);
        AssertNoLiveGo(blockedPrompt);
        StringAssert.Contains(qaReport, "\"live_prototype_authorized\": false");
        StringAssert.Contains(qaReport, "\"sidepanel_hash_debt_touched\": false");
        StringAssert.Contains(blockedPrompt, "BLOCKED_PENDING_HUMAN_POLICY_DECISION_AND_EXTERNAL_GO");
        Assert.IsFalse(nextPrompt.Contains("READ-ONLY LIVE PROTOTYPE GATED", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void ContainmentMatrixListsEveryNegativeProperty()
    {
        var repoRoot = FindRepoRoot();
        var matrix = File.ReadAllText(Path.Combine(repoRoot, "docs", "architecture", "computer-use", "windows-computer-use-containment-property-matrix-v1.md"));

        foreach (var property in RequiredProperties)
        {
            StringAssert.Contains(matrix, property);
        }
    }

    private static ComputerUseLocatorFusionResult Fuse(
        ComputerUseSnapshot snapshot,
        string objective,
        Win32ContextCollectionResult win32,
        IReadOnlyList<WindowsUiAutomationEvent> events,
        params RedactedVisualObservation[] observations)
    {
        var bridge = observations.Length == 0
            ? new ComputerUseVisualPerceptionBridgeDisabled().Observe(snapshot)
            : new ComputerUseFixtureVisualPerceptionBridge(observations).Observe(snapshot);
        var eventState = new FixtureWindowsUiAutomationEventStream(events).Read(new WindowsUiAutomationEventStreamOptions(snapshot.Scenario));

        return new ComputerUseLocatorFusionEngine().Fuse(new ComputerUseLocatorFusionInput(snapshot, objective, win32, eventState, bridge));
    }

    private static ComputerUseSnapshot Snapshot(params UiElementNode[] elements) =>
        Snapshot((IReadOnlyList<UiElementNode>)elements);

    private static ComputerUseSnapshot Snapshot(IReadOnlyList<UiElementNode> elements) =>
        new(
            SnapshotId: "wcu-containment-property-fixture",
            Source: ComputerUseSnapshotSource.Fixture,
            CapturedAtUtc: DateTimeOffset.UnixEpoch,
            Scenario: "containment-property-audit",
            Windows:
            [
                new WindowContext(
                    "fixture-main",
                    "notepad",
                    "Untitled - Notepad",
                    "Notepad",
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

    private static UiElementNode Element(string automationId, string runtimeId, string name, string controlType, bool invoke = false) =>
        new(
            new UiElementIdentity(automationId, runtimeId, name, controlType, controlType, "notepad", ["notepad", controlType, name]),
            new UiElementBounds(10, 10, 120, 32),
            new UiAutomationPatternCapabilities(invoke, SupportsValue: false, SupportsScroll: false, SupportsSelection: false, SupportsText: false, SupportsFocus: true),
            IsVisible: true,
            IsEnabled: true,
            IsPasswordField: false,
            IsCredentialField: false);

    private static void AssertNoLeak(string value, params string[] forbidden)
    {
        foreach (var item in forbidden)
        {
            Assert.IsFalse(value.Contains(item, StringComparison.OrdinalIgnoreCase), item);
        }
    }

    private static void AssertNoLiveGo(string value)
    {
        Assert.IsFalse(value.Contains("GO_WCU_READ_ONLY_LIVE_PROTOTYPE_GATED_ALLOWED", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(value.Contains("safe to start live implementation", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(value.Contains("\"live_prototype_authorized\": true", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(value.Contains("\"ProductAutomationEnabled\": true", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(value.Contains("public release unlocked", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(value.Contains("paid beta unlocked", StringComparison.OrdinalIgnoreCase));
    }

    private static string FindRepoRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "OneBrain.slnx")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        Assert.Fail("Could not locate repository root from test output directory.");
        return AppContext.BaseDirectory;
    }
}
