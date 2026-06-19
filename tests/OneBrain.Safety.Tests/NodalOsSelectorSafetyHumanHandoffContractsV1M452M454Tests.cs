using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.AgentOperations.Contracts;
using OneBrain.AgentOperations.Core;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("SelectorSafetyHumanHandoff")]
[TestCategory("AutomationEventEvidence")]
[TestCategory("AutomationLayerAdr")]
public sealed class NodalOsSelectorSafetyHumanHandoffContractsV1M452M454Tests
{
    private readonly NodalOsSelectorSafetyHumanHandoffValidator validator = new();

    [TestMethod]
    public void SelectorPolicy_RuntimeExecutionAllowedFalse()
    {
        var policy = NodalOsSelectorSafetyHumanHandoffFixtures.DefaultObservationOnlyPolicy();
        var result = validator.ValidatePolicy(policy);

        Assert.IsTrue(result.IsValid);
        Assert.IsFalse(policy.RuntimeExecutionAllowed);
        Assert.IsFalse(result.RuntimeExecutionAllowed);
    }

    [TestMethod]
    public void SelectorPolicy_RuntimeExecutionDeferredTrue()
    {
        var policy = NodalOsSelectorSafetyHumanHandoffFixtures.DefaultObservationOnlyPolicy();
        var result = validator.ValidatePolicy(policy);

        Assert.IsTrue(result.IsValid);
        Assert.IsTrue(policy.RuntimeExecutionDeferred);
        Assert.IsTrue(result.RuntimeExecutionDeferred);
    }

    [TestMethod]
    public void SelectorPolicy_ObservationOnlyRequired()
    {
        var policy = NodalOsSelectorSafetyHumanHandoffFixtures.DefaultObservationOnlyPolicy() with
        {
            ObservationOnly = false
        };
        var result = validator.ValidatePolicy(policy);

        Assert.IsFalse(result.IsValid);
        AssertContains(result.Errors, "observation-only");
    }

    [TestMethod]
    public void SelectorPolicy_RequiresEvidenceRedaction()
    {
        var policy = NodalOsSelectorSafetyHumanHandoffFixtures.DefaultObservationOnlyPolicy() with
        {
            RequiresEvidenceRedaction = false
        };
        var result = validator.ValidatePolicy(policy);

        Assert.IsFalse(result.IsValid);
        AssertContains(result.Errors, "evidence redaction");
    }

    [TestMethod]
    public void SelectorPolicy_RequiresGlobalPolicyEvaluation()
    {
        var policy = NodalOsSelectorSafetyHumanHandoffFixtures.DefaultObservationOnlyPolicy() with
        {
            RequiresGlobalPolicyEvaluation = false
        };
        var result = validator.ValidatePolicy(policy);

        Assert.IsFalse(result.IsValid);
        AssertContains(result.Errors, "global policy evaluation");
    }

    [TestMethod]
    public void SelectorPolicy_VisualOcrCannotBeFirst()
    {
        var policy = NodalOsSelectorSafetyHumanHandoffFixtures.DefaultObservationOnlyPolicy() with
        {
            PreferredStrategyOrder =
            [
                NodalOsSelectorStrategyKind.VisualCheckpointFuture,
                NodalOsSelectorStrategyKind.Semantic
            ]
        };
        var result = validator.ValidatePolicy(policy);

        Assert.IsFalse(result.IsValid);
        AssertContains(result.Errors, "cannot be first");
    }

    [TestMethod]
    public void SelectorPolicy_SemanticOrDomPreferredBeforeVisualOcr()
    {
        var policy = NodalOsSelectorSafetyHumanHandoffFixtures.DefaultObservationOnlyPolicy();
        var result = validator.ValidatePolicy(policy);
        var strategies = policy.PreferredStrategyOrder.ToArray();

        Assert.IsTrue(result.IsValid);
        Assert.IsTrue(Array.IndexOf(strategies, NodalOsSelectorStrategyKind.Semantic) <
                      Array.IndexOf(strategies, NodalOsSelectorStrategyKind.VisualCheckpointFuture));
    }

    [TestMethod]
    public void SelectorCandidate_RawSecretRejected()
    {
        var result = validator.ValidateSelectorCandidate(NodalOsSelectorSafetyHumanHandoffFixtures.SecretSelector());

        Assert.IsFalse(result.IsValid);
        AssertContains(result.Errors, "raw secrets");
    }

    [TestMethod]
    public void SelectorCandidate_RawCookieRejected()
    {
        var result = validator.ValidateSelectorCandidate(NodalOsSelectorSafetyHumanHandoffFixtures.CookieSelector());

        Assert.IsFalse(result.IsValid);
        AssertContains(result.Errors, "raw cookies");
    }

    [TestMethod]
    public void SelectorCandidate_RawHeaderRejected()
    {
        var result = validator.ValidateSelectorCandidate(NodalOsSelectorSafetyHumanHandoffFixtures.HeaderSelector());

        Assert.IsFalse(result.IsValid);
        AssertContains(result.Errors, "raw headers");
    }

    [TestMethod]
    public void SelectorCandidate_RawBodyRejected()
    {
        var result = validator.ValidateSelectorCandidate(NodalOsSelectorSafetyHumanHandoffFixtures.BodySelector());

        Assert.IsFalse(result.IsValid);
        AssertContains(result.Errors, "raw private bodies");
    }

    [TestMethod]
    public void SelectorCandidate_UnstableRejectedOrRequiresHumanReview()
    {
        var evaluation = validator.EvaluateSelector(
            NodalOsSelectorSafetyHumanHandoffFixtures.DefaultObservationOnlyPolicy(),
            NodalOsSelectorSafetyHumanHandoffFixtures.UnstableSelector());

        Assert.AreEqual(NodalOsSelectorSafetyDecision.RejectedUnstable, evaluation.Decision);
        Assert.IsTrue(evaluation.RequiresHumanReview);
    }

    [TestMethod]
    public void SelectorCandidate_MutableIntentRejected()
    {
        var evaluation = validator.EvaluateSelector(
            NodalOsSelectorSafetyHumanHandoffFixtures.DefaultObservationOnlyPolicy(),
            NodalOsSelectorSafetyHumanHandoffFixtures.MutableIntentSelector());

        Assert.AreEqual(NodalOsSelectorSafetyDecision.RejectedMutableIntent, evaluation.Decision);
        Assert.AreEqual(NodalOsSelectorRiskKind.High, evaluation.RiskKind);
    }

    [TestMethod]
    public void SelectorCandidate_EvidenceRefsValidateViaBridge()
    {
        var candidate = NodalOsSelectorSafetyHumanHandoffFixtures.SafeSemanticSelector() with
        {
            EvidenceRefs = [InvalidEvidenceRef()]
        };
        var bridgeResult = new NodalOsEvidenceRefBridge().ValidateBridgeRef(candidate.EvidenceRefs[0]);
        var result = validator.ValidateSelectorCandidate(candidate);

        Assert.IsFalse(bridgeResult.Accepted);
        Assert.IsFalse(result.IsValid);
    }

    [TestMethod]
    public void SelectorEvaluation_CannotAuthorizeAction()
    {
        var evaluation = SafeEvaluation();

        Assert.IsFalse(evaluation.CanAuthorizeAction);
        Assert.IsFalse(validator.ValidateSelectorEvaluation(evaluation).CanAuthorizeAction);
    }

    [TestMethod]
    public void SelectorEvaluation_ObservationOnly()
    {
        var evaluation = SafeEvaluation();

        Assert.IsTrue(evaluation.ObservationOnly);
        Assert.IsTrue(validator.ValidateSelectorEvaluation(evaluation).IsValid);
    }

    [TestMethod]
    public void SelectorEvaluation_RuntimeExecutionAllowedFalse()
    {
        var evaluation = SafeEvaluation();

        Assert.IsFalse(evaluation.RuntimeExecutionAllowed);
        Assert.IsTrue(evaluation.RuntimeExecutionDeferred);
    }

    [TestMethod]
    public void HumanHandoff_LoginHasSpecificBlocker()
    {
        var handoff = NodalOsSelectorSafetyHumanHandoffFixtures.LoginRequiredHandoff();

        Assert.IsTrue(validator.ValidateHumanHandoff(handoff).IsValid);
        Assert.AreEqual(NodalOsAutomationHandoffReason.LoginRequired, handoff.Reason);
        Assert.IsTrue(handoff.HumanReadableBlockerRedacted.Contains("Login", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void HumanHandoff_CaptchaHasSpecificBlocker()
    {
        var handoff = NodalOsSelectorSafetyHumanHandoffFixtures.CaptchaRequiredHandoff();

        Assert.IsTrue(validator.ValidateHumanHandoff(handoff).IsValid);
        Assert.AreEqual(NodalOsAutomationHandoffReason.CaptchaRequired, handoff.Reason);
        Assert.IsTrue(handoff.HumanReadableBlockerRedacted.Contains("Captcha", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void HumanHandoff_TwoFactorHasSpecificBlocker()
    {
        var handoff = NodalOsSelectorSafetyHumanHandoffFixtures.TwoFactorRequiredHandoff();

        Assert.IsTrue(validator.ValidateHumanHandoff(handoff).IsValid);
        Assert.AreEqual(NodalOsAutomationHandoffReason.TwoFactorRequired, handoff.Reason);
        Assert.IsTrue(handoff.HumanReadableBlockerRedacted.Contains("Two-factor", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void HumanHandoff_GenericBlockedRejected()
    {
        var result = validator.ValidateHumanHandoff(NodalOsSelectorSafetyHumanHandoffFixtures.GenericBlockedHandoffInvalid());

        Assert.IsFalse(result.IsValid);
        AssertContains(result.Errors, "exact blocker");
    }

    [TestMethod]
    public void HumanHandoff_UserOptionsRequired()
    {
        var handoff = NodalOsSelectorSafetyHumanHandoffFixtures.LoginRequiredHandoff() with
        {
            UserOptions = [NodalOsHumanHandoffUserOptionKind.PauseMission]
        };
        var result = validator.ValidateHumanHandoff(handoff);

        Assert.IsFalse(result.IsValid);
        AssertContains(result.Errors, "at least two");
    }

    [TestMethod]
    public void HumanHandoff_CopyTechnicalLogOptionPresent()
    {
        var handoff = NodalOsSelectorSafetyHumanHandoffFixtures.PolicyBlockedHandoff();

        CollectionAssert.Contains(handoff.UserOptions.ToArray(), NodalOsHumanHandoffUserOptionKind.CopyTechnicalLog);
    }

    [TestMethod]
    public void HumanHandoff_CannotAuthorizeAction()
    {
        var handoff = NodalOsSelectorSafetyHumanHandoffFixtures.LoginRequiredHandoff();

        Assert.IsFalse(handoff.CanAuthorizeAction);
        Assert.IsFalse(validator.ValidateHumanHandoff(handoff).CanAuthorizeAction);
    }

    [TestMethod]
    public void HumanHandoff_RuntimeExecutionAllowedFalse()
    {
        var handoff = NodalOsSelectorSafetyHumanHandoffFixtures.LoginRequiredHandoff();

        Assert.IsFalse(handoff.RuntimeExecutionAllowed);
        Assert.IsTrue(handoff.RuntimeExecutionDeferred);
    }

    [TestMethod]
    public void HumanHandoff_EvidenceRefsValidateViaBridge()
    {
        var handoff = NodalOsSelectorSafetyHumanHandoffFixtures.LoginRequiredHandoff() with
        {
            EvidenceRefs = [InvalidEvidenceRef()]
        };
        var bridgeResult = new NodalOsEvidenceRefBridge().ValidateBridgeRef(handoff.EvidenceRefs[0]);
        var result = validator.ValidateHumanHandoff(handoff);

        Assert.IsFalse(bridgeResult.Accepted);
        Assert.IsFalse(result.IsValid);
    }

    [TestMethod]
    public void Serializer_RoundTripsPolicyCandidateEvaluationHandoff()
    {
        var serializer = new NodalOsSelectorSafetyHumanHandoffJsonSerializer();
        var policy = NodalOsSelectorSafetyHumanHandoffFixtures.DefaultObservationOnlyPolicy();
        var candidate = NodalOsSelectorSafetyHumanHandoffFixtures.SafeSemanticSelector();
        var evaluation = validator.EvaluateSelector(policy, candidate);
        var handoff = NodalOsSelectorSafetyHumanHandoffFixtures.LoginRequiredHandoff();

        Assert.AreEqual(policy.PolicyId, serializer.DeserializePolicy(serializer.SerializePolicy(policy))?.PolicyId);
        Assert.AreEqual(candidate.SelectorId, serializer.DeserializeCandidate(serializer.SerializeCandidate(candidate))?.SelectorId);
        Assert.AreEqual(evaluation.Decision, serializer.DeserializeEvaluation(serializer.SerializeEvaluation(evaluation))?.Decision);
        Assert.AreEqual(handoff.Reason, serializer.DeserializeHandoff(serializer.SerializeHandoff(handoff))?.Reason);
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
        var contracts = File.ReadAllText(Path.Combine(RepoRoot(), "src", "OneBrain.AgentOperations.Contracts", "NodalOsSelectorSafetyHumanHandoffContracts.cs"));
        var services = File.ReadAllText(Path.Combine(RepoRoot(), "src", "OneBrain.AgentOperations.Core", "NodalOsSelectorSafetyHumanHandoffServices.cs"));

        AssertContains(contracts, "public enum NodalOsSelectorStrategyKind");
        AssertContains(contracts, "public sealed record NodalOsHumanHandoffContract");
        AssertContains(services, "public sealed class NodalOsSelectorSafetyHumanHandoffValidator");
        Assert.IsFalse(contracts.Contains("public enum Selector", StringComparison.Ordinal));
        Assert.IsFalse(services.Contains("public sealed class Selector", StringComparison.Ordinal));
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

    private NodalOsSelectorSafetyEvaluation SafeEvaluation() =>
        validator.EvaluateSelector(
            NodalOsSelectorSafetyHumanHandoffFixtures.DefaultObservationOnlyPolicy(),
            NodalOsSelectorSafetyHumanHandoffFixtures.SafeSemanticSelector());

    private static NodalOsEvidenceBridgeRef InvalidEvidenceRef() =>
        new()
        {
            EvidenceId = "evidence-invalid-redaction-required",
            Kind = "selector-handoff-contract",
            Ref = "ledger:selector-handoff-contract",
            Hash = "sha256:selector-handoff-contract",
            SourceKind = NodalOsEvidenceBridgeSourceKind.AgentOperation,
            UseKind = NodalOsEvidenceBridgeUseKind.AuditTrail,
            Authority = NodalOsEvidenceBridgeAuthority.NoAuthority,
            Sensitivity = NodalOsEvidenceSensitivity.Sensitive,
            RedactionState = NodalOsEvidenceRedactionState.RedactionRequired,
            LedgerRef = "ledger:selector-handoff-contract",
            Provenance = "NODAL OS:SelectorSafety:ContractOnly",
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
        Path.Combine(RepoRoot(), "docs", "reports", "selector-safety-human-handoff-contracts-v1-audit-m452.md");

    private static string ReportPath() =>
        Path.Combine(RepoRoot(), "docs", "reports", "selector-safety-human-handoff-contracts-v1-m454.md");

    private static string ArtifactPath() =>
        Path.Combine(RepoRoot(), "artifacts", "agent-operations", "m454", "selector-safety-human-handoff-contracts-v1-summary.json");

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
