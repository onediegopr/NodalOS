using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.AgentOperations.Contracts;
using OneBrain.AgentOperations.Core;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("AutomationEventEvidence")]
[TestCategory("AutomationLayerAdr")]
[TestCategory("ScheduledReadOnlyIntegrationNoDivergence")]
public sealed class NodalOsAutomationEventEvidenceContractsV1M449M451Tests
{
    private readonly NodalOsAutomationEventEvidenceValidator validator = new();

    [TestMethod]
    public void AutomationEvent_RuntimeExecutionAllowedFalse()
    {
        var automationEvent = NodalOsAutomationEventEvidenceFixtures.StepStartedEvent();
        var result = validator.ValidateEvent(automationEvent);

        Assert.IsTrue(result.IsValid);
        Assert.IsFalse(result.RuntimeExecutionAllowed);
        Assert.IsFalse(automationEvent.RuntimeExecutionAllowed);
    }

    [TestMethod]
    public void AutomationEvent_RuntimeExecutionDeferredTrue()
    {
        var automationEvent = NodalOsAutomationEventEvidenceFixtures.StepCompletedEvent();
        var result = validator.ValidateEvent(automationEvent);

        Assert.IsTrue(result.IsValid);
        Assert.IsTrue(result.RuntimeExecutionDeferred);
        Assert.IsTrue(automationEvent.RuntimeExecutionDeferred);
    }

    [TestMethod]
    public void AutomationEvent_RequiresPolicyEvaluation()
    {
        var automationEvent = NodalOsAutomationEventEvidenceFixtures.StepFailedEvent();
        var result = validator.ValidateEvent(automationEvent);

        Assert.IsTrue(result.IsValid);
        Assert.IsTrue(result.RequiresGlobalPolicyEvaluation);
        Assert.IsTrue(automationEvent.RequiresGlobalPolicyEvaluation);
    }

    [TestMethod]
    public void AutomationEvent_RequiresEvidenceRedaction()
    {
        var automationEvent = NodalOsAutomationEventEvidenceFixtures.HandoffRequiredEvent();
        var result = validator.ValidateEvent(automationEvent);

        Assert.IsTrue(result.IsValid);
        Assert.IsTrue(result.RequiresEvidenceRedaction);
        Assert.IsTrue(automationEvent.RequiresEvidenceRedaction);
    }

    [TestMethod]
    public void AutomationEvent_EvidenceRefsValidateViaBridge()
    {
        var invalidEvidenceRef = InvalidEvidenceRef();
        var bridgeResult = new NodalOsEvidenceRefBridge().ValidateBridgeRef(invalidEvidenceRef);
        var automationEvent = NodalOsAutomationEventEvidenceFixtures.StepStartedEvent() with
        {
            EvidenceRefs = [invalidEvidenceRef]
        };
        var result = validator.ValidateEvent(automationEvent);

        Assert.IsFalse(bridgeResult.Accepted);
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Count > 0);
    }

    [TestMethod]
    public void AutomationEvent_RawSecretSummaryRejectedOrRedacted()
    {
        const string rawSecret = "Bearer abcdefghijklmnopqrstuvwxyz";
        var automationEvent = NodalOsAutomationEventEvidenceFixtures.StepStartedEvent() with
        {
            HumanSummary = $"Observed {rawSecret}"
        };
        var result = validator.ValidateEvent(automationEvent);

        Assert.IsFalse(result.IsValid);
        AssertContains(result.Errors, "HumanSummary contains sensitive content");
        Assert.IsFalse(string.Join(" | ", result.Errors).Contains(rawSecret, StringComparison.Ordinal));
    }

    [TestMethod]
    public void AutomationEvidence_RedactedRequired()
    {
        var evidence = NodalOsAutomationEventEvidenceFixtures.StepLogEvidence() with
        {
            Redacted = false
        };
        var result = validator.ValidateEvidence(evidence);

        Assert.IsFalse(result.IsValid);
        AssertContains(result.Errors, "must be redacted");
    }

    [TestMethod]
    public void AutomationEvidence_RawSecretRejected()
    {
        var result = validator.ValidateEvidence(NodalOsAutomationEventEvidenceFixtures.InvalidRawSecretEvidence());

        Assert.IsFalse(result.IsValid);
        AssertContains(result.Errors, "raw secrets");
    }

    [TestMethod]
    public void AutomationEvidence_RawCookieRejected()
    {
        var result = validator.ValidateEvidence(NodalOsAutomationEventEvidenceFixtures.InvalidRawCookieEvidence());

        Assert.IsFalse(result.IsValid);
        AssertContains(result.Errors, "raw cookies");
    }

    [TestMethod]
    public void AutomationEvidence_RawHeaderRejected()
    {
        var result = validator.ValidateEvidence(NodalOsAutomationEventEvidenceFixtures.InvalidRawHeaderEvidence());

        Assert.IsFalse(result.IsValid);
        AssertContains(result.Errors, "raw headers");
    }

    [TestMethod]
    public void AutomationEvidence_RawBodyRejected()
    {
        var result = validator.ValidateEvidence(NodalOsAutomationEventEvidenceFixtures.InvalidRawBodyEvidence());

        Assert.IsFalse(result.IsValid);
        AssertContains(result.Errors, "raw private bodies");
    }

    [TestMethod]
    public void AutomationEvidence_SelectorPathSecretRejected()
    {
        var evidence = NodalOsAutomationEventEvidenceFixtures.SelectorEvidence() with
        {
            SelectorPath = "css=input[data-token='Bearer abcdefghijklmnopqrstuvwxyz']"
        };
        var result = validator.ValidateEvidence(evidence);

        Assert.IsFalse(result.IsValid);
        AssertContains(result.Errors, "SelectorPath contains sensitive content");
    }

    [TestMethod]
    public void AutomationEvidence_DomSnapshotRawSecretRejected()
    {
        var evidence = NodalOsAutomationEventEvidenceFixtures.DomSnapshotRedactedEvidence() with
        {
            DomSnapshotRedacted = "<input name='password' value='super-secret-value' />"
        };
        var result = validator.ValidateEvidence(evidence);

        Assert.IsFalse(result.IsValid);
        AssertContains(result.Errors, "DomSnapshotRedacted contains sensitive content");
    }

    [TestMethod]
    public void AutomationEvidence_NetworkAuthorizationRejected()
    {
        var evidence = NodalOsAutomationEventEvidenceFixtures.NetworkMetadataRedactedEvidence() with
        {
            NetworkMetadataRedacted = "GET /status Authorization: Bearer abcdefghijklmnopqrstuvwxyz"
        };
        var result = validator.ValidateEvidence(evidence);

        Assert.IsFalse(result.IsValid);
        AssertContains(result.Errors, "Network metadata must not contain Authorization");
    }

    [TestMethod]
    public void AutomationEvidence_ScreenshotIsReferenceOnly()
    {
        var valid = NodalOsAutomationEventEvidenceFixtures.SelectorEvidence() with
        {
            Kind = NodalOsAutomationEvidenceKind.ScreenshotReferenceFuture,
            ScreenshotRefFuture = "ledger:screenshot-future-reference"
        };
        var invalid = valid with
        {
            ScreenshotRefFuture = "data:image/png;base64,abcdef"
        };

        Assert.IsTrue(validator.ValidateEvidence(valid).IsValid);
        Assert.IsFalse(validator.ValidateEvidence(invalid).IsValid);
    }

    [TestMethod]
    public void AutomationEvidence_EvidenceRefsValidateViaBridge()
    {
        var invalidEvidenceRef = InvalidEvidenceRef();
        var bridgeResult = new NodalOsEvidenceRefBridge().ValidateBridgeRef(invalidEvidenceRef);
        var evidence = NodalOsAutomationEventEvidenceFixtures.StepLogEvidence() with
        {
            EvidenceRefs = [invalidEvidenceRef]
        };
        var result = validator.ValidateEvidence(evidence);

        Assert.IsFalse(bridgeResult.Accepted);
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Count > 0);
    }

    [TestMethod]
    public void Handoff_LoginRequiresHumanAction()
    {
        var handoff = NodalOsAutomationEventEvidenceFixtures.LoginRequiredHandoff();
        var result = validator.ValidateHandoffState(handoff);

        Assert.IsTrue(result.IsValid);
        Assert.IsTrue(handoff.RequiresHumanAction);
        Assert.AreEqual(NodalOsAutomationHandoffReason.LoginRequired, handoff.Reason);
    }

    [TestMethod]
    public void Handoff_CaptchaRequiresHumanAction()
    {
        var handoff = NodalOsAutomationEventEvidenceFixtures.CaptchaRequiredHandoff();
        var result = validator.ValidateHandoffState(handoff);

        Assert.IsTrue(result.IsValid);
        Assert.IsTrue(handoff.RequiresHumanAction);
        Assert.AreEqual(NodalOsAutomationHandoffReason.CaptchaRequired, handoff.Reason);
    }

    [TestMethod]
    public void Handoff_TwoFactorRequiresHumanAction()
    {
        var handoff = NodalOsAutomationEventEvidenceFixtures.TwoFactorRequiredHandoff();
        var result = validator.ValidateHandoffState(handoff);

        Assert.IsTrue(result.IsValid);
        Assert.IsTrue(handoff.RequiresHumanAction);
        Assert.AreEqual(NodalOsAutomationHandoffReason.TwoFactorRequired, handoff.Reason);
    }

    [TestMethod]
    public void Handoff_HasClearUserOptions()
    {
        var handoff = NodalOsAutomationEventEvidenceFixtures.LoginRequiredHandoff();

        CollectionAssert.AreEquivalent(
            new[] { "Continue", "Pause", "ChangeInstruction", "CopyTechnicalLog" },
            handoff.UserOptions.ToArray());
    }

    [TestMethod]
    public void Handoff_GenericBlockedWithoutReasonRejected()
    {
        var handoff = NodalOsAutomationEventEvidenceFixtures.LoginRequiredHandoff() with
        {
            HumanReadableBlocker = "blocked"
        };
        var result = validator.ValidateHandoffState(handoff);

        Assert.IsFalse(result.IsValid);
        AssertContains(result.Errors, "specific reason");
    }

    [TestMethod]
    public void Handoff_RuntimeExecutionAllowedFalse()
    {
        var handoff = NodalOsAutomationEventEvidenceFixtures.LoginRequiredHandoff();
        var result = validator.ValidateHandoffState(handoff);

        Assert.IsTrue(result.IsValid);
        Assert.IsFalse(result.RuntimeExecutionAllowed);
        Assert.IsFalse(handoff.RuntimeExecutionAllowed);
    }

    [TestMethod]
    public void Handoff_RuntimeExecutionDeferredTrue()
    {
        var handoff = NodalOsAutomationEventEvidenceFixtures.LoginRequiredHandoff();
        var result = validator.ValidateHandoffState(handoff);

        Assert.IsTrue(result.IsValid);
        Assert.IsTrue(result.RuntimeExecutionDeferred);
        Assert.IsTrue(handoff.RuntimeExecutionDeferred);
    }

    [TestMethod]
    public void Serializer_RoundTripsEventEvidenceHandoff()
    {
        var serializer = new NodalOsAutomationEventEvidenceJsonSerializer();
        var automationEvent = NodalOsAutomationEventEvidenceFixtures.StepStartedEvent();
        var evidence = NodalOsAutomationEventEvidenceFixtures.StepLogEvidence();
        var handoff = NodalOsAutomationEventEvidenceFixtures.LoginRequiredHandoff();

        Assert.AreEqual(automationEvent.Kind, serializer.DeserializeEvent(serializer.SerializeEvent(automationEvent))?.Kind);
        Assert.AreEqual(evidence.Kind, serializer.DeserializeEvidence(serializer.SerializeEvidence(evidence))?.Kind);
        Assert.AreEqual(handoff.Reason, serializer.DeserializeHandoffState(serializer.SerializeHandoffState(handoff))?.Reason);
    }

    [TestMethod]
    public void NoRecorderReplayQueueSchedulerBrowserAutomationUiExecutionImplemented()
    {
        var artifact = File.ReadAllText(ArtifactPath());

        AssertContains(artifact, "\"noRecorderImplemented\": true");
        AssertContains(artifact, "\"noReplayImplemented\": true");
        AssertContains(artifact, "\"noQueueImplemented\": true");
        AssertContains(artifact, "\"noSchedulerImplemented\": true");
        AssertContains(artifact, "\"noBrowserAutomationImplemented\": true");
        AssertContains(artifact, "\"noUiImplemented\": true");
        AssertContains(artifact, "\"noExecutionImplemented\": true");
    }

    [TestMethod]
    public void NoRpaDependenciesAdded()
    {
        var forbidden = new[] { "UI.Vision", "UIVision", "TagUI", "OpenRPA", "OpenIAP", "Kantu" };

        foreach (var project in Directory.GetFiles(RepoRoot(), "*.csproj", SearchOption.AllDirectories))
        {
            if (IsBuildOutput(project))
                continue;

            var text = File.ReadAllText(project);
            foreach (var dependency in forbidden)
                Assert.IsFalse(text.Contains(dependency, StringComparison.OrdinalIgnoreCase), $"{dependency} found in {project}");
        }
    }

    [TestMethod]
    public void NewTypesUseNodalOsPrefix()
    {
        var contracts = File.ReadAllText(Path.Combine(RepoRoot(), "src", "OneBrain.AgentOperations.Contracts", "NodalOsAutomationEventEvidenceContracts.cs"));
        var services = File.ReadAllText(Path.Combine(RepoRoot(), "src", "OneBrain.AgentOperations.Core", "NodalOsAutomationEventEvidenceServices.cs"));

        AssertContains(contracts, "public enum NodalOsAutomationEventKind");
        AssertContains(contracts, "public sealed record NodalOsAutomationEvent");
        AssertContains(services, "public sealed class NodalOsAutomationEventEvidenceValidator");
        Assert.IsFalse(contracts.Contains("public enum Automation", StringComparison.Ordinal));
        Assert.IsFalse(services.Contains("public sealed class Automation", StringComparison.Ordinal));
    }

    [TestMethod]
    public void UsesNodalOsName_NotNexa()
    {
        foreach (var path in new[] { AuditPath(), ReportPath(), ArtifactPath() })
        {
            var text = File.ReadAllText(path);

            Assert.IsTrue(
                text.Contains("NODAL OS", StringComparison.Ordinal) ||
                text.Contains("NODAL OS", StringComparison.Ordinal),
                path);
            Assert.IsFalse(text.Contains("NEXA", StringComparison.OrdinalIgnoreCase), path);
        }
    }

    private static NodalOsEvidenceBridgeRef InvalidEvidenceRef() =>
        new()
        {
            EvidenceId = "evidence-invalid-redaction-required",
            Kind = "automation-contract",
            Ref = "ledger:automation-contract",
            Hash = "sha256:automation-contract",
            SourceKind = NodalOsEvidenceBridgeSourceKind.AgentOperation,
            UseKind = NodalOsEvidenceBridgeUseKind.AuditTrail,
            Authority = NodalOsEvidenceBridgeAuthority.NoAuthority,
            Sensitivity = NodalOsEvidenceSensitivity.Sensitive,
            RedactionState = NodalOsEvidenceRedactionState.RedactionRequired,
            LedgerRef = "ledger:automation-contract",
            Provenance = "NODAL OS:AutomationLayer:ContractOnly",
            CreatedAt = DateTimeOffset.UtcNow
        };

    private static void AssertContains(IEnumerable<string> values, string expected) =>
        Assert.IsTrue(
            values.Any(value => value.Contains(expected, StringComparison.OrdinalIgnoreCase)),
            $"Expected validation message containing '{expected}'.");

    private static void AssertContains(string text, string expected) =>
        Assert.IsTrue(text.Contains(expected, StringComparison.Ordinal), expected);

    private static bool IsBuildOutput(string path) =>
        path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase) ||
        path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase);

    private static string AuditPath() =>
        Path.Combine(RepoRoot(), "docs", "reports", "automation-event-evidence-schema-contracts-v1-audit-m449.md");

    private static string ReportPath() =>
        Path.Combine(RepoRoot(), "docs", "reports", "automation-event-evidence-schema-contracts-v1-m451.md");

    private static string ArtifactPath() =>
        Path.Combine(RepoRoot(), "artifacts", "agent-operations", "m451", "automation-event-evidence-schema-contracts-v1-summary.json");

    private static string RepoRoot()
    {
        var current = AppContext.BaseDirectory;
        while (!string.IsNullOrWhiteSpace(current))
        {
            if (File.Exists(Path.Combine(current, "OneBrain.slnx")))
                return current;

            current = Directory.GetParent(current)?.FullName;
        }

        throw new DirectoryNotFoundException("Could not locate repository root.");
    }
}
