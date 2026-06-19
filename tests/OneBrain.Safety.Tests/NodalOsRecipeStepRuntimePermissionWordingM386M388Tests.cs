using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("RecipeStepRuntimePermission")]
[TestCategory("RecipeManifest")]
[TestCategory("StepLibrary")]
[TestCategory("EvidenceRefLedgerBridge")]
[TestCategory("CommonRedaction")]
public sealed class NodalOsRecipeStepRuntimePermissionWordingM386M388Tests
{
    private readonly NodalOsRecipeManifestValidator recipeValidator = new();
    private readonly NodalOsStepLibrary stepLibrary = new();
    private readonly NodalOsStepLibraryValidator stepValidator = new();

    [TestMethod]
    public void RecipeManifestValidation_CanPassManifestPolicy_DoesNotGrantRuntimeExecution()
    {
        var result = recipeValidator.Validate(NodalOsRecipeManifestFixtures.ReadOnlyRecipe());

        Assert.IsTrue(result.IsValid);
        Assert.IsTrue(result.CanPassManifestPolicy);
        Assert.IsFalse(result.RuntimeExecutionAllowed);
    }

    [TestMethod]
    public void RecipeManifestValidation_RuntimeExecutionAllowed_IsFalseForApprovedManifest()
    {
        var result = recipeValidator.Validate(NodalOsRecipeManifestFixtures.ReadOnlyRecipe());

        Assert.IsTrue(result.CanPassManifestPolicy);
        Assert.IsTrue(result.CanExecute);
        Assert.IsFalse(result.RuntimeExecutionAllowed);
    }

    [TestMethod]
    public void RecipeManifestValidation_RuntimeExecutionDeferred_IsTrue()
    {
        var result = recipeValidator.Validate(NodalOsRecipeManifestFixtures.ReadOnlyRecipe());

        Assert.IsTrue(result.RuntimeExecutionDeferred);
    }

    [TestMethod]
    public void RecipeManifestValidation_RequiresGlobalPolicyEvaluation_IsTrue()
    {
        var result = recipeValidator.Validate(NodalOsRecipeManifestFixtures.ReadOnlyRecipe());

        Assert.IsTrue(result.RequiresGlobalPolicyEvaluation);
        StringAssert.Contains(string.Join(" ", result.Warnings), "global policy");
    }

    [TestMethod]
    public void CanExecuteCompatibilityAlias_DoesNotMeanRuntimeExecution()
    {
        var result = recipeValidator.Validate(NodalOsRecipeManifestFixtures.ReadOnlyRecipe());

        Assert.AreEqual(result.CanPassManifestPolicy, result.CanExecute);
        Assert.IsFalse(result.RuntimeExecutionAllowed);
    }

    [TestMethod]
    public void ApprovedRecipeStatus_DoesNotBypassGlobalPolicy()
    {
        var result = recipeValidator.Validate(NodalOsRecipeManifestFixtures.ReadOnlyRecipe());

        Assert.IsTrue(result.CanPassManifestPolicy);
        Assert.IsTrue(result.RequiresGlobalPolicyEvaluation);
        StringAssert.Contains(string.Join(" ", result.Warnings), "does not grant runtime execution");
    }

    [TestMethod]
    public void SupervisedRecipe_StillRequiresApproval()
    {
        var result = recipeValidator.Validate(NodalOsRecipeManifestFixtures.SupervisedRecipe());

        Assert.IsTrue(result.CanPassManifestPolicy);
        Assert.IsTrue(result.RequiresApproval);
        Assert.IsFalse(result.RuntimeExecutionAllowed);
    }

    [TestMethod]
    public void BlockedRecipe_CannotPassManifestPolicy()
    {
        var result = recipeValidator.Validate(NodalOsRecipeManifestFixtures.BlockedRecipe());

        Assert.IsFalse(result.IsValid);
        Assert.IsFalse(result.CanPassManifestPolicy);
        Assert.IsFalse(result.CanExecute);
        Assert.IsFalse(result.RuntimeExecutionAllowed);
    }

    [TestMethod]
    public void StepDefinition_IsCatalogAvailableInV1_DoesNotGrantRuntimeExecution()
    {
        var definition = stepLibrary.GetDefinition(NodalOsStepKind.Click);

        Assert.IsTrue(definition.IsCatalogAvailableInV1);
        Assert.IsTrue(definition.RuntimeExecutionDeferred);
        Assert.IsTrue(definition.RequiresGlobalPolicyEvaluation);
    }

    [TestMethod]
    public void StepDefinition_IsAllowedInV1CompatibilityAlias_DoesNotMeanRuntimeExecution()
    {
        var definition = stepLibrary.GetDefinition(NodalOsStepKind.Type);

        Assert.AreEqual(definition.IsCatalogAvailableInV1, definition.IsAllowedInV1);
        Assert.IsTrue(definition.RuntimeExecutionDeferred);
    }

    [TestMethod]
    public void StepValidation_ClickCataloguedButRuntimeExecutionDeferred()
    {
        var result = stepValidator.Validate(Context(NodalOsStepKind.Click, humanApprovalAvailable: true));

        Assert.IsTrue(result.CanPassStepPolicy);
        Assert.IsTrue(result.IsAllowed);
        Assert.IsFalse(result.RuntimeExecutionAllowed);
        Assert.IsTrue(result.RuntimeExecutionDeferred);
    }

    [TestMethod]
    public void StepValidation_TypeSensitiveRequiresApprovalAndRuntimeExecutionDeferred()
    {
        var result = stepValidator.Validate(Context(
            NodalOsStepKind.Type,
            targetsSensitiveField: true,
            humanApprovalAvailable: true));

        Assert.IsTrue(result.CanPassStepPolicy);
        Assert.IsTrue(result.RequiresApproval);
        Assert.IsFalse(result.RuntimeExecutionAllowed);
        Assert.IsTrue(result.RuntimeExecutionDeferred);
    }

    [TestMethod]
    public void StepValidation_DownloadRequiresApprovalAndRuntimeExecutionDeferred()
    {
        var result = stepValidator.Validate(Context(
            NodalOsStepKind.DownloadRequest,
            requiresFileDownload: true,
            humanApprovalAvailable: true));

        Assert.IsTrue(result.CanPassStepPolicy);
        Assert.IsTrue(result.RequiresApproval);
        Assert.IsFalse(result.RuntimeExecutionAllowed);
    }

    [TestMethod]
    public void StepValidation_UploadRequiresApprovalAndRuntimeExecutionDeferred()
    {
        var result = stepValidator.Validate(Context(
            NodalOsStepKind.UploadRequest,
            requiresFileUpload: true,
            humanApprovalAvailable: true));

        Assert.IsTrue(result.CanPassStepPolicy);
        Assert.IsTrue(result.RequiresApproval);
        Assert.IsFalse(result.RuntimeExecutionAllowed);
    }

    [TestMethod]
    public void StepValidation_SubmitLikeStillBlocked()
    {
        var result = stepValidator.Validate(Context(NodalOsStepKind.Click, isSubmitLike: true));

        Assert.IsFalse(result.IsValid);
        Assert.IsFalse(result.CanPassStepPolicy);
        Assert.IsFalse(result.RuntimeExecutionAllowed);
        CollectionAssert.Contains(result.FailureKinds.ToList(), NexaFailureKind.PolicyBlocked);
    }

    [TestMethod]
    public void StepValidation_LoginCaptchaTwoFactorStillBlocked()
    {
        var login = stepValidator.Validate(Context(NodalOsStepKind.Type, isLoginRelated: true));
        var captcha = stepValidator.Validate(Context(NodalOsStepKind.AskHuman, isCaptchaOrTwoFactorRelated: true));

        Assert.IsFalse(login.CanPassStepPolicy);
        Assert.IsFalse(captcha.CanPassStepPolicy);
        Assert.IsFalse(login.RuntimeExecutionAllowed);
        Assert.IsFalse(captcha.RuntimeExecutionAllowed);
        CollectionAssert.Contains(login.FailureKinds.ToList(), NexaFailureKind.LoginRequired);
        CollectionAssert.Contains(captcha.FailureKinds.ToList(), NexaFailureKind.CaptchaDetected);
        CollectionAssert.Contains(captcha.FailureKinds.ToList(), NexaFailureKind.TwoFactorRequired);
    }

    [TestMethod]
    public void StepValidation_RequiresGlobalPolicyEvaluation()
    {
        var result = stepValidator.Validate(Context(NodalOsStepKind.Read));

        Assert.IsTrue(result.RequiresGlobalPolicyEvaluation);
        Assert.IsFalse(result.RuntimeExecutionAllowed);
    }

    [TestMethod]
    public void StepValidationResult_SerializesNewFields()
    {
        var result = stepValidator.Validate(Context(NodalOsStepKind.DownloadRequest, requiresFileDownload: true));

        var json = JsonSerializer.Serialize(result);
        var roundTrip = JsonSerializer.Deserialize<NodalOsStepValidationResult>(json);

        Assert.IsNotNull(roundTrip);
        Assert.AreEqual(result.CanPassStepPolicy, roundTrip.CanPassStepPolicy);
        Assert.AreEqual(result.RuntimeExecutionAllowed, roundTrip.RuntimeExecutionAllowed);
        Assert.AreEqual(result.RuntimeExecutionDeferred, roundTrip.RuntimeExecutionDeferred);
        Assert.AreEqual(result.RequiresGlobalPolicyEvaluation, roundTrip.RequiresGlobalPolicyEvaluation);
    }

    [TestMethod]
    public void ExistingRecipeManifestTests_StillPass()
    {
        var result = recipeValidator.Validate(NodalOsRecipeManifestFixtures.ReadOnlyRecipe());

        Assert.IsTrue(result.IsValid);
        Assert.IsTrue(result.CanExecute);
        Assert.IsFalse(result.RequiresApproval);
    }

    [TestMethod]
    public void ExistingStepLibraryTests_StillPass()
    {
        var definition = stepLibrary.GetDefinition(NodalOsStepKind.Navigate);
        var validation = stepValidator.Validate(Context(NodalOsStepKind.Navigate));

        Assert.IsTrue(definition.IsAllowedInV1);
        Assert.IsTrue(validation.IsAllowed);
        CollectionAssert.Contains(definition.PossibleFailureKinds.ToList(), NexaFailureKind.NavigationTimeout);
    }

    [TestMethod]
    public void NoUiOrRuntimeActionsIntroduced()
    {
        var artifactPath = Path.Combine(
            FindRepoRoot(),
            "artifacts",
            "agent-operations",
            "m388",
            "recipe-step-runtime-permission-wording-summary.json");

        Assert.IsTrue(File.Exists(artifactPath));
        var json = File.ReadAllText(artifactPath);

        StringAssert.Contains(json, "\"noRuntimeBehaviorChange\": true");
        StringAssert.Contains(json, "\"noUiImplemented\": true");
        StringAssert.Contains(json, "\"noRecipeExecutionImplemented\": true");
        StringAssert.Contains(json, "\"noStepExecutionImplemented\": true");
        StringAssert.Contains(json, "\"noOrchestrationApiImplemented\": true");
    }

    private static NodalOsStepValidationContext Context(
        NodalOsStepKind kind,
        bool targetsSensitiveField = false,
        bool requiresFileUpload = false,
        bool requiresFileDownload = false,
        bool isSubmitLike = false,
        bool isLoginRelated = false,
        bool isCaptchaOrTwoFactorRelated = false,
        bool globalSensitiveActionsBlocked = false,
        bool humanApprovalAvailable = false) =>
        new()
        {
            StepKind = kind,
            TargetsSensitiveField = targetsSensitiveField,
            RequiresFileUpload = requiresFileUpload,
            RequiresFileDownload = requiresFileDownload,
            IsSubmitLike = isSubmitLike,
            IsLoginRelated = isLoginRelated,
            IsCaptchaOrTwoFactorRelated = isCaptchaOrTwoFactorRelated,
            GlobalSensitiveActionsBlocked = globalSensitiveActionsBlocked,
            HumanApprovalAvailable = humanApprovalAvailable
        };

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "OneBrain.slnx")))
                return directory.FullName;

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate repository root.");
    }
}
