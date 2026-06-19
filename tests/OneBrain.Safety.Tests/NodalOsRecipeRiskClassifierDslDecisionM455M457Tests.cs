using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.AgentOperations.Contracts;
using OneBrain.AgentOperations.Core;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("RecipeRiskClassifier")]
[TestCategory("SelectorSafetyHumanHandoff")]
[TestCategory("AutomationEventEvidence")]
[TestCategory("AutomationLayerAdr")]
public sealed class NodalOsRecipeRiskClassifierDslDecisionM455M457Tests
{
    private readonly NodalOsRecipeRiskClassifier classifier = new();

    [TestMethod]
    public void ReadOnlyStep_LowRisk_NoRuntimeAuthority()
    {
        var classification = classifier.ClassifyStep(NodalOsRecipeRiskClassifierFixtures.ReadOnlyExtractionStep());

        Assert.AreEqual(NodalOsRecipeRiskLevel.Low, classification.RiskLevel);
        Assert.IsFalse(classification.RuntimeExecutionAllowed);
        Assert.IsFalse(classification.CanAuthorizeAction);
    }

    [TestMethod]
    public void ExtractionStep_NoRuntimeAuthority()
    {
        var classification = classifier.ClassifyStep(NodalOsRecipeRiskClassifierFixtures.ReadOnlyExtractionStep());

        CollectionAssert.Contains(classification.Categories.ToArray(), NodalOsRecipeStepRiskCategory.Extraction);
        Assert.IsTrue(classification.RuntimeExecutionDeferred);
        Assert.IsFalse(classification.CanAuthorizeAction);
    }

    [TestMethod]
    public void FormFillStep_RequiresApproval()
    {
        var classification = classifier.ClassifyStep(NodalOsRecipeRiskClassifierFixtures.FormFillStep());

        Assert.AreEqual(NodalOsRecipeRiskLevel.Medium, classification.RiskLevel);
        Assert.AreEqual(NodalOsRecipeApprovalRequirement.RequiredBeforeExecution, classification.ApprovalRequirement);
    }

    [TestMethod]
    public void SubmitStep_HighRiskRequiresApproval()
    {
        var classification = classifier.ClassifyStep(NodalOsRecipeRiskClassifierFixtures.SubmitStep());

        Assert.AreEqual(NodalOsRecipeRiskLevel.High, classification.RiskLevel);
        Assert.AreEqual(NodalOsRecipeApprovalRequirement.RequiredBeforeExecution, classification.ApprovalRequirement);
    }

    [TestMethod]
    public void PurchasePaymentStep_CriticalRequiresApproval()
    {
        var classification = classifier.ClassifyStep(NodalOsRecipeRiskClassifierFixtures.PurchasePaymentStep());

        Assert.AreEqual(NodalOsRecipeRiskLevel.Critical, classification.RiskLevel);
        Assert.AreEqual(NodalOsRecipeApprovalRequirement.RequiredBeforePublishSendPayment, classification.ApprovalRequirement);
    }

    [TestMethod]
    public void DeleteStep_CriticalRequiresApproval()
    {
        var classification = classifier.ClassifyStep(NodalOsRecipeRiskClassifierFixtures.DeleteStep());

        Assert.AreEqual(NodalOsRecipeRiskLevel.Critical, classification.RiskLevel);
        Assert.AreEqual(NodalOsRecipeApprovalRequirement.RequiredBeforeDestructiveAction, classification.ApprovalRequirement);
    }

    [TestMethod]
    public void PublishSendStep_RequiresApproval()
    {
        var classification = classifier.ClassifyStep(NodalOsRecipeRiskClassifierFixtures.PublishSendStep());

        Assert.AreEqual(NodalOsRecipeRiskLevel.High, classification.RiskLevel);
        Assert.AreEqual(NodalOsRecipeApprovalRequirement.RequiredBeforePublishSendPayment, classification.ApprovalRequirement);
    }

    [TestMethod]
    public void CredentialLoginStep_RequiresHumanHandoff()
    {
        var classification = classifier.ClassifyStep(NodalOsRecipeRiskClassifierFixtures.CredentialLoginStep());

        Assert.IsTrue(classification.RequiresHumanHandoff);
        CollectionAssert.Contains(classification.Categories.ToArray(), NodalOsRecipeStepRiskCategory.CredentialOrLogin);
    }

    [TestMethod]
    public void CaptchaTwoFactorStep_RequiresHumanHandoff()
    {
        var classification = classifier.ClassifyStep(NodalOsRecipeRiskClassifierFixtures.CaptchaTwoFactorStep());

        Assert.IsTrue(classification.RequiresHumanHandoff);
        CollectionAssert.Contains(classification.Categories.ToArray(), NodalOsRecipeStepRiskCategory.CaptchaOrTwoFactor);
    }

    [TestMethod]
    public void FileSystemMutationStep_RequiresApprovalAndEvidence()
    {
        var classification = classifier.ClassifyStep(NodalOsRecipeRiskClassifierFixtures.FileSystemMutationStep());

        Assert.AreEqual(NodalOsRecipeRiskLevel.High, classification.RiskLevel);
        Assert.AreEqual(NodalOsRecipeApprovalRequirement.RequiredBeforeDestructiveAction, classification.ApprovalRequirement);
        Assert.IsTrue(classification.RequiresEvidenceRedaction);
        Assert.IsTrue(classification.EvidenceRefs.Count > 0);
    }

    [TestMethod]
    public void BrowserAutomationFutureStep_RuntimeDeferred()
    {
        var classification = classifier.ClassifyStep(NodalOsRecipeRiskClassifierFixtures.BrowserAutomationFutureStep());

        CollectionAssert.Contains(classification.Categories.ToArray(), NodalOsRecipeStepRiskCategory.BrowserAutomationFuture);
        Assert.IsFalse(classification.RuntimeExecutionAllowed);
        Assert.IsTrue(classification.RuntimeExecutionDeferred);
    }

    [TestMethod]
    public void UnsupportedStep_RejectedHighOrCritical()
    {
        var classification = classifier.ClassifyStep(NodalOsRecipeRiskClassifierFixtures.UnsupportedStep());

        CollectionAssert.Contains(classification.Categories.ToArray(), NodalOsRecipeStepRiskCategory.Unsupported);
        Assert.IsTrue(classification.RiskLevel >= NodalOsRecipeRiskLevel.High);
    }

    [TestMethod]
    public void RiskProfile_OverallRiskIsMaxStepRisk()
    {
        var profile = NodalOsRecipeRiskClassifierFixtures.MixedRiskRecipeProfile();

        Assert.AreEqual(
            profile.StepClassifications.Max(classification => classification.RiskLevel),
            profile.OverallRiskLevel);
    }

    [TestMethod]
    public void RiskProfile_RequiresHumanApprovalIfAnyStepRequiresApproval()
    {
        var profile = NodalOsRecipeRiskClassifierFixtures.MixedRiskRecipeProfile();

        Assert.IsTrue(profile.StepClassifications.Any(classification =>
            classification.ApprovalRequirement != NodalOsRecipeApprovalRequirement.NotRequiredForObservation));
        Assert.IsTrue(profile.RequiresHumanApproval);
    }

    [TestMethod]
    public void RiskProfile_RequiresHumanHandoffIfAnyStepRequiresHandoff()
    {
        var profile = NodalOsRecipeRiskClassifierFixtures.MixedRiskRecipeProfile();

        Assert.IsTrue(profile.StepClassifications.Any(classification => classification.RequiresHumanHandoff));
        Assert.IsTrue(profile.RequiresHumanHandoff);
    }

    [TestMethod]
    public void RiskProfile_CannotAuthorizeAction()
    {
        var profile = NodalOsRecipeRiskClassifierFixtures.MixedRiskRecipeProfile();

        Assert.IsFalse(profile.CanAuthorizeAction);
        Assert.IsFalse(classifier.ValidateRiskProfile(profile).CanAuthorizeAction);
    }

    [TestMethod]
    public void Classification_RuntimeExecutionAllowedFalse()
    {
        var classification = classifier.ClassifyStep(NodalOsRecipeRiskClassifierFixtures.SubmitStep());

        Assert.IsFalse(classification.RuntimeExecutionAllowed);
        Assert.IsTrue(classifier.ValidateClassification(classification).IsValid);
    }

    [TestMethod]
    public void Classification_RuntimeExecutionDeferredTrue()
    {
        var classification = classifier.ClassifyStep(NodalOsRecipeRiskClassifierFixtures.SubmitStep());

        Assert.IsTrue(classification.RuntimeExecutionDeferred);
    }

    [TestMethod]
    public void Classification_RequiresGlobalPolicyEvaluation()
    {
        var classification = classifier.ClassifyStep(NodalOsRecipeRiskClassifierFixtures.SubmitStep());

        Assert.IsTrue(classification.RequiresGlobalPolicyEvaluation);
    }

    [TestMethod]
    public void Classification_EvidenceRefsValidateViaBridge()
    {
        var classification = classifier.ClassifyStep(NodalOsRecipeRiskClassifierFixtures.ReadOnlyExtractionStep()) with
        {
            EvidenceRefs = [InvalidEvidenceRef()]
        };
        var bridgeResult = new NodalOsEvidenceRefBridge().ValidateBridgeRef(classification.EvidenceRefs[0]);
        var result = classifier.ValidateClassification(classification);

        Assert.IsFalse(bridgeResult.Accepted);
        Assert.IsFalse(result.IsValid);
    }

    [TestMethod]
    public void DslDecision_DslIsNotRuntime()
    {
        var decision = NodalOsRecipeRiskClassifierFixtures.DslRepresentationOnlyDecision();

        Assert.IsFalse(decision.DslIsRuntime);
        Assert.IsTrue(classifier.ValidateDslDecision(decision).IsValid);
    }

    [TestMethod]
    public void DslDecision_ParserDeferred()
    {
        var decision = NodalOsRecipeRiskClassifierFixtures.DslRepresentationOnlyDecision();

        Assert.IsFalse(decision.ParserImplemented);
        CollectionAssert.Contains(decision.Decisions.ToArray(), NodalOsRecipeDslDecision.ParserDeferred);
    }

    [TestMethod]
    public void DslDecision_DirectExecutionForbidden()
    {
        var decision = NodalOsRecipeRiskClassifierFixtures.DslRepresentationOnlyDecision();

        Assert.IsFalse(decision.DirectExecutionAllowed);
        CollectionAssert.Contains(decision.Decisions.ToArray(), NodalOsRecipeDslDecision.DirectExecutionForbidden);
    }

    [TestMethod]
    public void DslDecision_ImportRequiresValidation()
    {
        var decision = NodalOsRecipeRiskClassifierFixtures.DslRepresentationOnlyDecision();

        Assert.IsTrue(decision.ImportRequiresValidation);
        CollectionAssert.Contains(decision.Decisions.ToArray(), NodalOsRecipeDslDecision.ImportRequiresValidation);
    }

    [TestMethod]
    public void DslDecision_JsonCanonicalModelRequired()
    {
        var decision = NodalOsRecipeRiskClassifierFixtures.DslRepresentationOnlyDecision();

        Assert.IsTrue(decision.JsonCanonicalModelRequired);
        CollectionAssert.Contains(decision.Decisions.ToArray(), NodalOsRecipeDslDecision.JsonCanonicalModelRequired);
    }

    [TestMethod]
    public void InvalidDslDirectExecutionDecision_IsRejected()
    {
        var result = classifier.ValidateDslDecision(NodalOsRecipeRiskClassifierFixtures.InvalidDslDirectExecutionDecision());

        Assert.IsFalse(result.IsValid);
        AssertContains(result.Errors, "Direct DSL execution is forbidden");
    }

    [TestMethod]
    public void Serializer_RoundTripsStepClassificationProfileDslDecision()
    {
        var serializer = new NodalOsRecipeRiskClassifierJsonSerializer();
        var input = NodalOsRecipeRiskClassifierFixtures.SubmitStep();
        var classification = classifier.ClassifyStep(input);
        var profile = NodalOsRecipeRiskClassifierFixtures.MixedRiskRecipeProfile();
        var decision = NodalOsRecipeRiskClassifierFixtures.DslRepresentationOnlyDecision();

        Assert.AreEqual(input.StepId, serializer.DeserializeStepInput(serializer.SerializeStepInput(input))?.StepId);
        Assert.AreEqual(classification.RiskLevel, serializer.DeserializeClassification(serializer.SerializeClassification(classification))?.RiskLevel);
        Assert.AreEqual(profile.OverallRiskLevel, serializer.DeserializeProfile(serializer.SerializeProfile(profile))?.OverallRiskLevel);
        Assert.AreEqual(decision.DecisionId, serializer.DeserializeDslDecision(serializer.SerializeDslDecision(decision))?.DecisionId);
    }

    [TestMethod]
    public void NoDslParserRuntimeRecorderReplayQueueSchedulerBrowserAutomationUiExecutionImplemented()
    {
        var artifact = File.ReadAllText(ArtifactPath());

        AssertContains(artifact, "\"noDslParserImplemented\": true");
        AssertContains(artifact, "\"noRecorderImplemented\": true");
        AssertContains(artifact, "\"noReplayImplemented\": true");
        AssertContains(artifact, "\"noQueueImplemented\": true");
        AssertContains(artifact, "\"noSchedulerImplemented\": true");
        AssertContains(artifact, "\"noBrowserAutomationImplemented\": true");
        AssertContains(artifact, "\"noUiImplemented\": true");
        AssertContains(artifact, "\"noExecutionImplemented\": true");
    }

    [TestMethod]
    public void NoTagUiDependencyAdded()
    {
        foreach (var project in Directory.GetFiles(RepoRoot(), "*.csproj", SearchOption.AllDirectories))
        {
            if (IsBuildOutput(project))
                continue;

            Assert.IsFalse(File.ReadAllText(project).Contains("TagUI", StringComparison.OrdinalIgnoreCase), project);
        }
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
        var contracts = File.ReadAllText(Path.Combine(RepoRoot(), "src", "OneBrain.AgentOperations.Contracts", "NodalOsRecipeRiskClassifierContracts.cs"));
        var services = File.ReadAllText(Path.Combine(RepoRoot(), "src", "OneBrain.AgentOperations.Core", "NodalOsRecipeRiskClassifierServices.cs"));

        AssertContains(contracts, "public enum NodalOsRecipeStepRiskCategory");
        AssertContains(contracts, "public sealed record NodalOsRecipeRiskProfile");
        AssertContains(services, "public sealed class NodalOsRecipeRiskClassifier");
        Assert.IsFalse(contracts.Contains("public enum RecipeRisk", StringComparison.Ordinal));
        Assert.IsFalse(services.Contains("public sealed class RecipeRisk", StringComparison.Ordinal));
    }

    [TestMethod]
    public void UsesNodalOsName_NotNexa()
    {
        foreach (var path in new[] { AuditPath(), AdrPath(), ReportPath(), ArtifactPath() })
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
            Kind = "recipe-risk-contract",
            Ref = "ledger:recipe-risk-contract",
            Hash = "sha256:recipe-risk-contract",
            SourceKind = NodalOsEvidenceBridgeSourceKind.RecipeManifest,
            UseKind = NodalOsEvidenceBridgeUseKind.AuditTrail,
            Authority = NodalOsEvidenceBridgeAuthority.NoAuthority,
            Sensitivity = NodalOsEvidenceSensitivity.Sensitive,
            RedactionState = NodalOsEvidenceRedactionState.RedactionRequired,
            LedgerRef = "ledger:recipe-risk-contract",
            Provenance = "NODAL OS:RecipeRiskClassifier:ContractOnly",
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
        Path.Combine(RepoRoot(), "docs", "reports", "recipe-risk-classifier-dsl-decision-audit-m455.md");

    private static string AdrPath() =>
        Path.Combine(RepoRoot(), "docs", "architecture", "recipe-dsl-decision-record.md");

    private static string ReportPath() =>
        Path.Combine(RepoRoot(), "docs", "reports", "recipe-risk-classifier-dsl-decision-m457.md");

    private static string ArtifactPath() =>
        Path.Combine(RepoRoot(), "artifacts", "agent-operations", "m457", "recipe-risk-classifier-dsl-decision-summary.json");

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
