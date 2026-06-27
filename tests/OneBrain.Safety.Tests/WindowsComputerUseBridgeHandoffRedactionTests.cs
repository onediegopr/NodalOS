using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.WindowsComputerUse;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("WindowsComputerUseFixtureSafe")]
[TestCategory("WindowsComputerUseBridgeHandoffRedaction")]
public sealed class WindowsComputerUseBridgeHandoffRedactionTests
{
    [TestMethod]
    public void SameFixtureInputProducesStableHandoffIdAndDuplicatePrevention()
    {
        var first = BuildEnvelope("Continue");
        var second = BuildEnvelope("Continue");
        var idempotency = new ComputerUseBridgeHandoffBuilder().Compare(first, second);

        Assert.AreEqual(first.HandoffId, second.HandoffId);
        Assert.AreEqual(first.StableHandoffKey, second.StableHandoffKey);
        Assert.IsTrue(idempotency.StableHandoffKeyMatched);
        Assert.IsTrue(idempotency.DuplicatePrevented);
        Assert.IsTrue(idempotency.AuthorityRemainedFalse);
        Assert.IsTrue(idempotency.ReplayAsActionBlocked);
        AssertNoAuthority(first);
        AssertNoAuthority(second);
    }

    [TestMethod]
    public void RedactedEvidenceRemainsRedactedAfterHandoffSerializationRoundTrip()
    {
        var envelope = BuildSensitiveEnvelope();
        var builder = new ComputerUseBridgeHandoffBuilder();
        var json = builder.Serialize(envelope);
        var roundTrip = builder.Deserialize(json);
        var roundTripJson = builder.Serialize(roundTrip);

        Assert.AreEqual(envelope.HandoffId, roundTrip.HandoffId);
        Assert.AreEqual(envelope.StableHandoffKey, roundTrip.StableHandoffKey);
        Assert.AreNotEqual(ComputerUseHandoffRedactionStatus.None, roundTrip.RedactionStatus);
        Assert.IsTrue(roundTrip.SensitiveFieldsRedacted.Count > 0);
        AssertNoAuthority(roundTrip);
        AssertNoLeak(roundTripJson,
            "user@example.com",
            "hunter2",
            "sk-testSecret999",
            "ghp_fakeSecretToken999",
            "eyJabc.eyJdef.sig",
            "654321",
            "+1 212 555 0199",
            "GB82WEST12345698765432",
            @"C:\Users\diego",
            "4111 1111 1111 1111",
            "clipboard-data",
            "base64");
    }

    [TestMethod]
    public void RedactedOcrWindowTitleAndProcessPathDoNotReappearInBridgeObservation()
    {
        var envelope = BuildSensitiveEnvelope();
        var json = JsonSerializer.Serialize(envelope);

        Assert.IsTrue(envelope.BridgeObservations.All(o => o.EvidenceOnly));
        Assert.IsTrue(envelope.BridgeObservations.Any(o => o.RedactionStatus != ComputerUseRedactionStatus.None || o.SensitiveFieldsRedacted.Count > 0));
        Assert.IsTrue(envelope.BridgeObservations.All(o => !o.RawScreenshotPresent && !o.ClipboardPresent && !o.LiveProviderCalled));
        AssertNoLeak(json,
            "user@example.com",
            "ghp_fakeSecretToken999",
            "+1 212 555 0199",
            "GB82WEST12345698765432",
            @"C:\Users\diego");
    }

    [TestMethod]
    public void DuplicateEvidenceRefsDoNotGrantAuthorityOrChangeStableHandoffKey()
    {
        var snapshot = FixtureComputerUseSnapshotBuilder.NotepadLikeUiaRichApp();
        var locator = Fuse(snapshot, "Text Editor");
        var duplicated = locator with
        {
            EvidenceRefs = locator.EvidenceRefs.Concat(locator.EvidenceRefs).Concat(["locator:manual-duplicate:redacted", "locator:manual-duplicate:redacted"]).ToArray()
        };
        var evidence = new ComputerUseUnifiedEvidencePackBuilder().Build(snapshot, duplicated);
        var builder = new ComputerUseBridgeHandoffBuilder();
        var first = builder.Build(snapshot, evidence, duplicated);
        var second = builder.Build(snapshot, evidence, duplicated);

        Assert.AreEqual(first.StableHandoffKey, second.StableHandoffKey);
        Assert.AreEqual(first.EvidenceRefs.Select(r => r.RefId).Distinct(StringComparer.OrdinalIgnoreCase).Count(), first.EvidenceRefs.Count);
        AssertNoAuthority(first);
        Assert.IsFalse(builder.CanReplayAsAction(first));
    }

    [TestMethod]
    public void ReplayedUiaEventOcrOnlyTargetAndHighConfidenceLocatorCannotTriggerAction()
    {
        var snapshot = FixtureComputerUseSnapshotBuilder.ElectronLikeUiaPoorApp();
        var visual = ComputerUseVisualObservationFixtures.FromElement("submit", "Submit", "button", VisualSignalConfidence.High);
        var events = new FixtureWindowsUiAutomationEventStream(FixtureWindowsUiAutomationEvents.ActiveWindowChanged("Electron App")).Read(new WindowsUiAutomationEventStreamOptions("replay"));
        var bridge = new ComputerUseFixtureVisualPerceptionBridge([visual]).Observe(snapshot);
        var locator = new ComputerUseLocatorFusionEngine().Fuse(new ComputerUseLocatorFusionInput(snapshot, "Submit", FixtureWin32ContextFactory.ElectronActive(), events, bridge));
        var evidence = new ComputerUseUnifiedEvidencePackBuilder().Build(snapshot, locator);
        var envelope = new ComputerUseBridgeHandoffBuilder().Build(snapshot, evidence, locator, bridge, "Replayed OCR/event/locator evidence only.");

        Assert.IsTrue(locator.VisualFallbackRequired);
        Assert.IsFalse(events.ActionAuthority);
        Assert.IsFalse(events.Events.Any(e => e.CanTriggerExecution || e.ActionAuthority));
        Assert.IsFalse(bridge.ActionAuthority);
        Assert.IsFalse(locator.ActionAuthorityGranted);
        Assert.IsFalse(locator.AllowedToExecuteLive);
        Assert.IsFalse(locator.BestCandidate?.ActionAuthority ?? false);
        Assert.AreEqual(ComputerUseHandoffReplaySafety.ReplayBlockedNoAction, envelope.ReplaySafety);
        Assert.IsFalse(envelope.ReplayCanExecuteAction);
        Assert.IsFalse(new ComputerUseBridgeHandoffBuilder().CanReplayAsAction(envelope));
        AssertNoAuthority(envelope);
    }

    [TestMethod]
    public void BridgeObservationAndHandoffEnvelopeCannotBecomeExecutionRequest()
    {
        var envelope = BuildEnvelope("Text Editor");
        var policy = envelope.TransferPolicy;

        Assert.IsTrue(policy.EvidenceOnly);
        Assert.IsTrue(policy.RequiresRedaction);
        Assert.IsTrue(policy.RequiresHumanHandoff);
        Assert.IsFalse(policy.AllowNetworkTransfer);
        Assert.IsFalse(policy.AllowProviderCall);
        Assert.IsFalse(policy.AllowProcessExecution);
        Assert.IsFalse(policy.AllowActionAuthority);
        Assert.IsFalse(policy.AllowRawScreenshot);
        Assert.IsFalse(policy.AllowClipboard);
        Assert.IsTrue(envelope.BridgeObservations.All(o => !o.ActionAuthority && !o.RawScreenshotPresent && !o.ClipboardPresent && !o.LiveProviderCalled));
        AssertNoAuthority(envelope);
    }

    [TestMethod]
    public void ReportJsonAndNextPromptKeepLiveBlockedAndContainmentOnly()
    {
        var repoRoot = FindRepoRoot();
        var reportJson = File.ReadAllText(Path.Combine(repoRoot, "docs", "qa", "computer-use", "wcu-containment-property-audit-002-bridge-handoff-redaction", "report.json"));
        var nextPrompt = File.ReadAllText(Path.Combine(repoRoot, "docs", "prompts", "computer-use", "next-wcu-containment-property-audit-003-prompt.md"));
        var matrix = File.ReadAllText(Path.Combine(repoRoot, "docs", "architecture", "computer-use", "windows-computer-use-bridge-handoff-idempotency-matrix-v1.md"));

        StringAssert.Contains(reportJson, "\"live_prototype_authorized\": false");
        StringAssert.Contains(reportJson, "\"live_remains_blocked\": true");
        StringAssert.Contains(reportJson, "\"bridge_handoff_idempotency\": \"LOCKED\"");
        StringAssert.Contains(reportJson, "\"redaction_persistence\": \"LOCKED\"");
        StringAssert.Contains(reportJson, "\"replay_as_action\": \"BLOCKED\"");
        StringAssert.Contains(reportJson, "\"sidepanel_hash_debt_touched\": false");
        StringAssert.Contains(reportJson, "\"wcu_031_036_reopened\": false");
        StringAssert.Contains(reportJson, "\"wcu_037_044_status\": \"BLOCKED_PENDING_HUMAN_POLICY_DECISION_AND_EXTERNAL_GO\"");
        AssertNoLiveGo(reportJson);
        AssertNoLiveGo(nextPrompt);
        StringAssert.Contains(nextPrompt, "containment-only");
        Assert.IsFalse(nextPrompt.Contains("READ-ONLY LIVE PROTOTYPE GATED", StringComparison.OrdinalIgnoreCase));
        StringAssert.Contains(matrix.ToLowerInvariant(), "same fixture input");
        StringAssert.Contains(matrix.ToLowerInvariant(), "replay safety");
    }

    private static ComputerUseHandoffEnvelope BuildEnvelope(string objective)
    {
        var snapshot = FixtureComputerUseSnapshotBuilder.NotepadLikeUiaRichApp();
        var locator = Fuse(snapshot, objective);
        var evidence = new ComputerUseUnifiedEvidencePackBuilder().Build(snapshot, locator);
        return new ComputerUseBridgeHandoffBuilder().Build(snapshot, evidence, locator);
    }

    private static ComputerUseHandoffEnvelope BuildSensitiveEnvelope()
    {
        var snapshot = FixtureComputerUseSnapshotBuilder.PasswordLoginForm();
        var visual = ComputerUseVisualObservationFixtures.FromText("sensitive-ocr", "email=user@example.com password=hunter2 api_key=sk-testSecret999 token=ghp_fakeSecretToken999 jwt=eyJabc.eyJdef.sig otp=654321 phone +1 212 555 0199 iban=GB82WEST12345698765432 card 4111 1111 1111 1111 clipboard=clipboard-data");
        var bridge = new ComputerUseFixtureVisualPerceptionBridge([visual]).Observe(snapshot);
        var events = new FixtureWindowsUiAutomationEventStream(FixtureWindowsUiAutomationEvents.LoginSensitiveValueChanged()).Read(new WindowsUiAutomationEventStreamOptions("sensitive"));
        var locator = new ComputerUseLocatorFusionEngine().Fuse(new ComputerUseLocatorFusionInput(snapshot, "Sign in", FixtureWin32ContextFactory.LoginActiveWithSensitiveTitle(), events, bridge));
        var evidence = new ComputerUseUnifiedEvidencePackBuilder().Build(snapshot, locator);
        return new ComputerUseBridgeHandoffBuilder().Build(snapshot, evidence, locator, bridge, @"handoff for user@example.com token=ghp_fakeSecretToken999 path=C:\Users\diego\Customer\file.txt phone +1 212 555 0199 iban=GB82WEST12345698765432");
    }

    private static ComputerUseLocatorFusionResult Fuse(ComputerUseSnapshot snapshot, string objective)
    {
        var bridge = new ComputerUseVisualPerceptionBridgeDisabled().Observe(snapshot);
        var events = new FixtureWindowsUiAutomationEventStream(FixtureWindowsUiAutomationEvents.NotepadNoBlockage()).Read(new WindowsUiAutomationEventStreamOptions("fixture"));
        return new ComputerUseLocatorFusionEngine().Fuse(new ComputerUseLocatorFusionInput(snapshot, objective, FixtureWin32ContextFactory.NotepadActive(), events, bridge));
    }

    private static void AssertNoAuthority(ComputerUseHandoffEnvelope envelope)
    {
        Assert.IsFalse(envelope.LiveReadPermitted);
        Assert.IsFalse(envelope.ActionAuthorityGranted);
        Assert.IsFalse(envelope.ProductAutomationEnabled);
        Assert.IsFalse(envelope.RawScreenshotPresent);
        Assert.IsFalse(envelope.ClipboardPresent);
        Assert.IsFalse(envelope.ReplayCanExecuteAction);
        Assert.IsFalse(envelope.TransferPolicy.AllowActionAuthority);
        Assert.IsFalse(envelope.TransferPolicy.AllowProcessExecution);
        Assert.IsFalse(envelope.TransferPolicy.AllowProviderCall);
    }

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
