using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.WindowsComputerUse;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("WindowsComputerUseFixtureSafe")]
[TestCategory("WindowsComputerUseReadOnlyLiveDesignGate")]
public sealed class WindowsComputerUseReadOnlyLiveDesignGateTests
{
    private static readonly string[] CanonicalGateNames =
    [
        "WCU_LIVE_READ_DISABLED_BY_DEFAULT",
        "WCU_LIVE_READ_DEV_FLAG_REQUIRED",
        "WCU_NO_INPUT_INJECTION_GATE",
        "WCU_NO_WINDOW_MANIPULATION_GATE",
        "WCU_NO_CLIPBOARD_GATE",
        "WCU_NO_RAW_SCREENSHOT_GATE",
        "WCU_NO_CREDENTIAL_VALUE_CAPTURE_GATE",
        "WCU_NO_UAC_ADMIN_AUTOMATION_GATE",
        "WCU_EVENT_STREAM_NO_ACTION_TRIGGER_GATE",
        "WCU_EVIDENCE_REDACTION_REQUIRED_GATE",
        "WCU_AUDIT_LOG_REQUIRED_GATE",
        "WCU_KILL_SWITCH_REQUIRED_GATE",
        "WCU_ALLOWLISTED_TEST_APPS_ONLY_GATE",
        "WCU_HUMAN_OPERATOR_CONFIRMATION_GATE"
    ];

    [TestMethod]
    public void LiveReadGateDefaultIsDisabledAndFailClosed()
    {
        var result = ComputerUseReadOnlyLiveGateCatalog.Evaluate(new ComputerUseReadOnlyLiveGateRequest());

        Assert.IsFalse(result.LiveReadPermitted);
        Assert.IsFalse(result.ActionAuthorityGranted);
        Assert.IsFalse(result.ProductAutomationEnabled);
        Assert.IsTrue(result.RequiresAudit);
        Assert.AreEqual(ComputerUseReadOnlyLiveReadinessClassification.NotReadyForLive, result.Readiness);
        Assert.IsTrue(result.KillSwitch.GlobalDisabled);
        Assert.IsTrue(result.KillSwitch.ProviderDisabled);
        Assert.IsTrue(result.KillSwitch.EventStreamDisabled);
        Assert.IsTrue(result.KillSwitch.VisualOcrDisabled);
        Assert.IsTrue(result.KillSwitch.EmergencyFailClosed);
    }

    [TestMethod]
    public void AllRequiredGateNamesAreRepresented()
    {
        var names = ComputerUseReadOnlyLiveGateCatalog.RequiredGates.Select(g => g.CanonicalName).ToArray();

        CollectionAssert.AreEquivalent(CanonicalGateNames, names);
        Assert.IsTrue(ComputerUseReadOnlyLiveGateCatalog.RequiredGates.All(g => g.RequiredForPrototype));
    }

    [TestMethod]
    public void ForbiddenLiveReadOptionsRemainBlocked()
    {
        var result = ComputerUseReadOnlyLiveGateCatalog.Evaluate(new ComputerUseReadOnlyLiveGateRequest(
            DevFlagEnabled: true,
            HumanOperatorConfirmed: true,
            AuditLogConfigured: true,
            EvidenceRedactionRequired: false,
            AllowlistedTestAppOnly: true,
            AllowsInputInjection: true,
            AllowsWindowManipulation: true,
            AllowsClipboard: true,
            AllowsRawScreenshots: true,
            AllowsCredentialValueCapture: true,
            AllowsUacAdminAutomation: true,
            EventStreamCanTriggerActions: true,
            AttemptsLiveRead: true));

        AssertBlocked(result, "WCU_NO_INPUT_INJECTION_GATE");
        AssertBlocked(result, "WCU_NO_WINDOW_MANIPULATION_GATE");
        AssertBlocked(result, "WCU_NO_CLIPBOARD_GATE");
        AssertBlocked(result, "WCU_NO_RAW_SCREENSHOT_GATE");
        AssertBlocked(result, "WCU_NO_CREDENTIAL_VALUE_CAPTURE_GATE");
        AssertBlocked(result, "WCU_NO_UAC_ADMIN_AUTOMATION_GATE");
        AssertBlocked(result, "WCU_EVENT_STREAM_NO_ACTION_TRIGGER_GATE");
        AssertBlocked(result, "WCU_EVIDENCE_REDACTION_REQUIRED_GATE");
        Assert.IsFalse(result.LiveReadPermitted);
        Assert.IsFalse(result.ActionAuthorityGranted);
    }

    [TestMethod]
    public void DisabledCollectorsRemainDisabledUnderLiveGate()
    {
        var uia = new WindowsUiAutomationReadOnlyCollectorDisabled().Collect(new WindowsUiAutomationReadOnlySnapshotOptions(
            "disabled",
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
        var win32 = new DisabledWin32ContextReadOnlyCollector().Collect(new Win32ContextCollectionOptions(
            "disabled",
            AllowWindowManipulation: true,
            AllowFocusStealing: true,
            AllowInputInjection: true,
            AllowClipboard: true,
            AllowScreenshots: true));
        var events = new DisabledWindowsUiAutomationEventStream().Read(new WindowsUiAutomationEventStreamOptions(
            "disabled",
            SubscribeLiveEvents: true,
            AllowActionCallbacks: true,
            AllowInvoke: true,
            AllowClick: true,
            AllowSetValue: true,
            AllowKeyboard: true,
            AllowMouse: true,
            AllowClipboard: true));
        var visual = new ComputerUseVisualPerceptionBridgeDisabled().Observe(FixtureComputerUseSnapshotBuilder.NotepadLikeUiaRichApp());

        Assert.AreEqual(WindowsUiAutomationReadOnlyStatus.SkippedDisabled, uia.Status);
        Assert.IsFalse(uia.InvokeUsed || uia.ClickUsed || uia.SetValueUsed || uia.KeyboardUsed || uia.MouseUsed || uia.ClipboardUsed || uia.ScreenshotCaptured);
        Assert.AreEqual(Win32ContextCollectionStatus.Disabled, win32.Status);
        Assert.IsFalse(win32.ReadRealPc || win32.WindowManipulationUsed || win32.FocusStealingUsed || win32.InputInjectionUsed || win32.ClipboardUsed || win32.ScreenshotCaptured || win32.ActionAuthority);
        Assert.AreEqual(WindowsUiAutomationEventStreamStatus.Disabled, events.Status);
        Assert.IsFalse(events.LiveSubscribed || events.ActionCallbackRegistered || events.ActionAuthority);
        Assert.IsFalse(visual.LiveProviderCalled || visual.RawScreenshotStored || visual.ActionAuthority);
    }

    [TestMethod]
    public void LocatorConfidenceEvidenceAndOcrCannotGrantLiveAuthority()
    {
        var snapshot = FixtureComputerUseSnapshotBuilder.NotepadLikeUiaRichApp();
        var visual = new ComputerUseFixtureVisualPerceptionBridge([
            ComputerUseVisualObservationFixtures.FromText("continue", "Continue", VisualSignalConfidence.High)
        ]).Observe(snapshot);
        var eventState = new FixtureWindowsUiAutomationEventStream(FixtureWindowsUiAutomationEvents.NotepadNoBlockage()).Read(new WindowsUiAutomationEventStreamOptions("fixture"));
        var locator = new ComputerUseLocatorFusionEngine().Fuse(new ComputerUseLocatorFusionInput(
            snapshot,
            "Continue",
            FixtureWin32ContextFactory.NotepadActive(),
            eventState,
            visual));
        var dryRun = new ComputerUseSafeActionPlanner().Plan(snapshot, "Continue", ComputerUseActionKind.Click);
        var evidence = new ComputerUseUnifiedEvidencePackBuilder().Build(snapshot, locator, dryRun);

        Assert.IsFalse(locator.ActionAuthorityGranted);
        Assert.IsFalse(locator.AllowedToExecuteLive);
        Assert.IsFalse(locator.LiveProviderCalled);
        Assert.IsFalse(locator.RealPcRead);
        Assert.IsFalse(locator.RawScreenshotStored);
        Assert.IsFalse(dryRun.AllowedToExecuteLive);
        Assert.IsFalse(evidence.ActionAuthorityGranted);
        Assert.IsFalse(evidence.RawScreenshotPresent);
        Assert.IsFalse(evidence.ClipboardPresent);
    }

    [TestMethod]
    public void GateDocsAndReportContainCanonicalGateNames()
    {
        var repoRoot = FindRepoRoot();
        var gatesDoc = File.ReadAllText(Path.Combine(repoRoot, "docs", "architecture", "computer-use", "windows-computer-use-read-only-live-gates.md"));
        var reportJson = File.ReadAllText(Path.Combine(repoRoot, "docs", "qa", "computer-use", "wcu-031-036-read-only-live-design-gate-audit-pack", "report.json"));
        using var parsed = JsonDocument.Parse(reportJson);

        foreach (var name in CanonicalGateNames)
        {
            StringAssert.Contains(gatesDoc, name);
            StringAssert.Contains(reportJson, name);
        }

        Assert.AreEqual("GO_WCU_READ_ONLY_LIVE_DESIGN_GATE_AUDIT_PACK_READY", parsed.RootElement.GetProperty("decision").GetString());
    }

    private static void AssertBlocked(ComputerUseReadOnlyLiveDesignGateResult result, string canonicalName)
    {
        var gate = result.Gates.Single(g => g.CanonicalName == canonicalName);
        Assert.AreEqual(ComputerUseReadOnlyLiveGateStatus.Blocked, gate.Status, canonicalName);
        Assert.IsFalse(gate.ActionAuthorityGranted, canonicalName);
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
