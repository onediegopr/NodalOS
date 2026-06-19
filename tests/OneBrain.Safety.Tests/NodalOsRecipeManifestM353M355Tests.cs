using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("RecipeManifest")]
[TestCategory("AutomationJson")]
[TestCategory("FailureTaxonomy")]
[TestCategory("RunReport")]
[TestCategory("Troubleshooting")]
[TestCategory("SelectiveAbsorption")]
[TestCategory("MissionTaskDomain")]
[TestCategory("AgentWorkboard")]
public sealed class NodalOsRecipeManifestM353M355Tests
{
    private readonly NodalOsRecipeManifestValidator validator = new();

    private static readonly string RepoRoot = AppDomain.CurrentDomain.BaseDirectory.Contains("bin\\Debug") ||
                                              AppDomain.CurrentDomain.BaseDirectory.Contains("bin\\Release")
        ? Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", ".."))
        : AppDomain.CurrentDomain.BaseDirectory;

    [TestMethod]
    public void ValidReadOnlyManifest_PassesValidation()
    {
        var result = validator.Validate(NodalOsRecipeManifestFixtures.ReadOnlyRecipe());

        Assert.IsTrue(result.IsValid);
        Assert.IsTrue(result.CanExecute);
        Assert.IsFalse(result.RequiresApproval);
    }

    [TestMethod]
    public void DraftManifest_IsValidButCannotExecute()
    {
        var manifest = NodalOsRecipeManifestFixtures.ReadOnlyRecipe() with { Status = NodalOsRecipeStatus.Draft };

        var result = validator.Validate(manifest);

        Assert.IsTrue(result.IsValid);
        Assert.IsFalse(result.CanExecute);
        StringAssert.Contains(string.Join(" ", result.Warnings), "Draft");
    }

    [TestMethod]
    public void ShadowManifest_IsObserveOnly_AndCannotExecuteRealActions()
    {
        var manifest = NodalOsRecipeManifestFixtures.ReadOnlyRecipe() with { Status = NodalOsRecipeStatus.Shadow };

        var result = validator.Validate(manifest);

        Assert.IsTrue(result.IsValid);
        Assert.IsFalse(result.CanExecute);
        StringAssert.Contains(string.Join(" ", result.Warnings), "observe/simulate");
    }

    [TestMethod]
    public void SupervisedManifest_RequiresApproval()
    {
        var result = validator.Validate(NodalOsRecipeManifestFixtures.SupervisedRecipe());

        Assert.IsTrue(result.IsValid);
        Assert.IsTrue(result.CanExecute);
        Assert.IsTrue(result.RequiresApproval);
    }

    [TestMethod]
    public void ApprovedManifest_WithAllowedActions_CanExecute()
    {
        var result = validator.Validate(NodalOsRecipeManifestFixtures.ReadOnlyRecipe());

        Assert.IsTrue(result.IsValid);
        Assert.IsTrue(result.CanExecute);
    }

    [TestMethod]
    public void BlockedManifest_CannotExecute()
    {
        var result = validator.Validate(NodalOsRecipeManifestFixtures.BlockedRecipe());

        Assert.IsFalse(result.IsValid);
        Assert.IsFalse(result.CanExecute);
        StringAssert.Contains(string.Join(" ", result.Errors), "Blocked");
    }

    [TestMethod]
    public void DeprecatedManifest_CannotExecute()
    {
        var manifest = NodalOsRecipeManifestFixtures.ReadOnlyRecipe() with { Status = NodalOsRecipeStatus.Deprecated };

        var result = validator.Validate(manifest);

        Assert.IsFalse(result.IsValid);
        Assert.IsFalse(result.CanExecute);
        StringAssert.Contains(string.Join(" ", result.Errors), "Deprecated");
    }

    [TestMethod]
    public void ManifestWithDisallowedAction_Fails()
    {
        var result = validator.Validate(NodalOsRecipeManifestFixtures.UnsafeDisallowedRecipe());

        Assert.IsFalse(result.IsValid);
        StringAssert.Contains(string.Join(" ", result.Errors), "disallowed");
    }

    [TestMethod]
    public void DisallowedActionOverridesAllowedAction()
    {
        var result = validator.Validate(NodalOsRecipeManifestFixtures.UnsafeDisallowedRecipe());

        Assert.IsFalse(result.IsValid);
        StringAssert.Contains(string.Join(" ", result.Errors), "disallowed");
    }

    [TestMethod]
    public void UploadRequest_RequiresApproval()
    {
        var manifest = ManifestWithSingleStep(NodalOsRecipeActionKind.UploadRequest);

        var result = validator.Validate(manifest);

        Assert.IsTrue(result.IsValid);
        Assert.IsTrue(result.RequiresApproval);
    }

    [TestMethod]
    public void DownloadRequest_RequiresApproval()
    {
        var manifest = ManifestWithSingleStep(NodalOsRecipeActionKind.DownloadRequest);

        var result = validator.Validate(manifest);

        Assert.IsTrue(result.IsValid);
        Assert.IsTrue(result.RequiresApproval);
    }

    [TestMethod]
    public void SensitiveType_WithSensitiveActionsBlocked_Fails()
    {
        var manifest = ManifestWithSingleStep(
            NodalOsRecipeActionKind.Type,
            targetsSensitiveField: true,
            sensitiveActionsBlocked: true);

        var result = validator.Validate(manifest);

        Assert.IsFalse(result.IsValid);
        StringAssert.Contains(string.Join(" ", result.Errors), "Sensitive action");
    }

    [TestMethod]
    public void SensitiveType_WithApprovalRequired_IsFlagged()
    {
        var manifest = ManifestWithSingleStep(
            NodalOsRecipeActionKind.Type,
            targetsSensitiveField: true,
            sensitiveActionsBlocked: false);

        var result = validator.Validate(manifest);

        Assert.IsTrue(result.IsValid);
        Assert.IsTrue(result.RequiresApproval);
    }

    [TestMethod]
    public void MaxRuntimeSteps_Exceeded_Fails()
    {
        var manifest = NodalOsRecipeManifestFixtures.ReadOnlyRecipe() with
        {
            Policy = NodalOsRecipeManifestFixtures.ReadOnlyRecipe().Policy with { MaxRuntimeSteps = 1 }
        };

        var result = validator.Validate(manifest);

        Assert.IsFalse(result.IsValid);
        StringAssert.Contains(string.Join(" ", result.Errors), "MaxRuntimeSteps");
    }

    [TestMethod]
    public void AllowedDomains_RejectsOutsideDomain()
    {
        var manifest = NodalOsRecipeManifestFixtures.ReadOnlyRecipe() with
        {
            Steps =
            [
                NodalOsRecipeManifestFixtures.Step(
                    "step-outside",
                    0,
                    "Outside domain",
                    NodalOsRecipeActionKind.Read,
                    "https://outside.invalid/fixture")
            ]
        };

        var result = validator.Validate(manifest);

        Assert.IsFalse(result.IsValid);
        StringAssert.Contains(string.Join(" ", result.Errors), "AllowedDomains");
    }

    [TestMethod]
    public void ManifestSerializesAndDeserializes()
    {
        var manifest = NodalOsRecipeManifestFixtures.ReadOnlyRecipe();

        var json = NodalOsRecipeManifestJsonSerializer.Serialize(manifest);
        var roundTrip = NodalOsRecipeManifestJsonSerializer.Deserialize(json);

        Assert.AreEqual(manifest.RecipeId, roundTrip.RecipeId);
        Assert.AreEqual(manifest.Status, roundTrip.Status);
        StringAssert.Contains(json, "Approved");
    }

    [TestMethod]
    public void SuccessCriteria_FailureSignals_EvidenceRequirements_ArePreserved()
    {
        var manifest = NodalOsRecipeManifestFixtures.ReadOnlyRecipe();
        var roundTrip = NodalOsRecipeManifestJsonSerializer.Deserialize(
            NodalOsRecipeManifestJsonSerializer.Serialize(manifest));

        CollectionAssert.Contains(roundTrip.SuccessCriteria.ToList(), "fixture observed");
        CollectionAssert.Contains(roundTrip.FailureSignals.ToList(), "fixture unavailable");
        CollectionAssert.Contains(roundTrip.EvidenceRequirements.ToList(), "run-report");
    }

    [TestMethod]
    public void ManifestWithSecretLikeField_FailsOrSanitizes()
    {
        var manifest = NodalOsRecipeManifestFixtures.ReadOnlyRecipe() with
        {
            EvidenceRequirements = ["access_token"]
        };

        var result = validator.Validate(manifest);

        Assert.IsFalse(result.IsValid);
        StringAssert.Contains(string.Join(" ", result.Errors), "secret-like");
    }

    [TestMethod]
    public void RecipeManifest_DoesNotBypassGlobalPolicyFlag()
    {
        var manifest = NodalOsRecipeManifestFixtures.ReadOnlyRecipe() with
        {
            Policy = NodalOsRecipeManifestFixtures.ReadOnlyRecipe().Policy with
            {
                AllowedActionKinds = [NodalOsRecipeActionKind.Read],
                DisallowedActionKinds = [NodalOsRecipeActionKind.Read]
            },
            Steps = [NodalOsRecipeManifestFixtures.Step("step-read", 0, "Read", NodalOsRecipeActionKind.Read)]
        };

        var result = validator.Validate(manifest);

        Assert.IsFalse(result.IsValid);
        StringAssert.Contains(string.Join(" ", result.Errors), "disallowed");
    }

    [TestMethod]
    public void NoExecutionRuntimeIntroduced()
    {
        var contracts = File.ReadAllText(SourcePath(
            "src", "OneBrain.AgentOperations.Contracts", "NodalOsRecipeManifestContracts.cs"));
        var services = File.ReadAllText(SourcePath(
            "src", "OneBrain.AgentOperations.Core", "NodalOsRecipeManifestServices.cs"));
        var combined = contracts + services;

        Assert.IsFalse(combined.Contains("RecipeRunner", StringComparison.Ordinal));
        Assert.IsFalse(combined.Contains("BrowserRecipeReplay", StringComparison.Ordinal));
        Assert.IsFalse(combined.Contains("ChromeDevToolsProtocol", StringComparison.Ordinal));
        Assert.IsFalse(combined.Contains("IWebDriver", StringComparison.Ordinal));
    }

    [TestMethod]
    public void Artifact_ValidatesM355Flags()
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(SourcePath(
            "artifacts", "agent-operations", "m355", "recipe-manifest-v1-summary.json")));
        var root = doc.RootElement;

        Assert.AreEqual("M353-M355", root.GetProperty("milestone").GetString());
        Assert.IsTrue(root.GetProperty("recipeManifestModelCreated").GetBoolean());
        Assert.IsTrue(root.GetProperty("recipeStepManifestCreated").GetBoolean());
        Assert.IsTrue(root.GetProperty("recipePolicyManifestCreated").GetBoolean());
        Assert.IsTrue(root.GetProperty("jsonSerializerCreated").GetBoolean());
        Assert.IsTrue(root.GetProperty("validatorCreated").GetBoolean());
        Assert.IsTrue(root.GetProperty("readOnlyFixtureCreated").GetBoolean());
        Assert.IsTrue(root.GetProperty("supervisedFixtureCreated").GetBoolean());
        Assert.IsTrue(root.GetProperty("blockedFixtureCreated").GetBoolean());
        Assert.IsTrue(root.GetProperty("unsafeFixtureCreated").GetBoolean());
        Assert.IsTrue(root.GetProperty("draftCannotExecute").GetBoolean());
        Assert.IsTrue(root.GetProperty("shadowObserveOnly").GetBoolean());
        Assert.IsTrue(root.GetProperty("supervisedRequiresApproval").GetBoolean());
        Assert.IsTrue(root.GetProperty("approvedRequiresPolicyCompliance").GetBoolean());
        Assert.IsTrue(root.GetProperty("uploadRequiresApproval").GetBoolean());
        Assert.IsTrue(root.GetProperty("downloadRequiresApproval").GetBoolean());
        Assert.IsTrue(root.GetProperty("sensitiveTypeBlockedOrApprovalRequired").GetBoolean());
        Assert.IsTrue(root.GetProperty("disallowedOverridesAllowed").GetBoolean());
        Assert.IsTrue(root.GetProperty("allowedDomainsValidated").GetBoolean());
        Assert.IsTrue(root.GetProperty("maxRuntimeStepsValidated").GetBoolean());
        Assert.IsTrue(root.GetProperty("secretLikeFieldsRejectedOrSanitized").GetBoolean());
        Assert.IsTrue(root.GetProperty("noExecutionRuntimeImplemented").GetBoolean());
        Assert.IsTrue(root.GetProperty("noOrchestrationApiImplemented").GetBoolean());
        Assert.IsTrue(root.GetProperty("noScheduledRunsImplemented").GetBoolean());
        Assert.IsTrue(root.GetProperty("noStepLibraryImplemented").GetBoolean());
        Assert.IsTrue(root.GetProperty("noRuntimeActionsIntroduced").GetBoolean());
        Assert.AreEqual("M356-M358 Blocker + Progress Reporting Contract", root.GetProperty("nextRecommendedMilestone").GetString());
    }

    private static NodalOsRecipeManifest ManifestWithSingleStep(
        NodalOsRecipeActionKind actionKind,
        bool targetsSensitiveField = false,
        bool sensitiveActionsBlocked = false) =>
        NodalOsRecipeManifestFixtures.ReadOnlyRecipe() with
        {
            RecipeId = $"recipe-{actionKind}",
            Steps =
            [
                NodalOsRecipeManifestFixtures.Step(
                    "step-single",
                    0,
                    $"{actionKind} fixture",
                    actionKind,
                    "https://example.invalid/fixture",
                    targetsSensitiveField: targetsSensitiveField)
            ],
            Policy = new NodalOsRecipePolicyManifest
            {
                AllowedActionKinds = [actionKind],
                SensitiveActionsBlocked = sensitiveActionsBlocked,
                MaxRuntimeSteps = 2
            }
        };

    private static string SourcePath(params string[] parts) => Path.Combine(new[] { RepoRoot }.Concat(parts).ToArray());
}
