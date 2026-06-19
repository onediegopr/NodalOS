using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("StepLibrary")]
[TestCategory("RecipeManifest")]
[TestCategory("RunReport")]
[TestCategory("FailureTaxonomy")]
[TestCategory("AgentProgressReporting")]
[TestCategory("VerificationBeforeDone")]
[TestCategory("MissionTaskDomain")]
public sealed class NodalOsStepLibraryV1M368M370Tests
{
    private readonly NodalOsStepLibrary library = new();
    private readonly NodalOsStepLibraryValidator validator = new();

    private static readonly string RepoRoot = AppDomain.CurrentDomain.BaseDirectory.Contains("bin\\Debug") ||
                                              AppDomain.CurrentDomain.BaseDirectory.Contains("bin\\Release")
        ? Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", ".."))
        : AppDomain.CurrentDomain.BaseDirectory;

    [TestMethod]
    public void EveryStepKind_HasDefinition()
    {
        foreach (var kind in Enum.GetValues<NodalOsStepKind>())
            Assert.AreEqual(kind, library.GetDefinition(kind).StepKind);
    }

    [TestMethod]
    public void EveryStepDefinition_HasRiskMetadata()
    {
        foreach (var definition in library.GetAllDefinitions())
            Assert.IsTrue(Enum.IsDefined(definition.RiskLevel));
    }

    [TestMethod]
    public void EveryStepDefinition_HasCapabilityMetadata()
    {
        foreach (var definition in library.GetAllDefinitions())
            Assert.IsTrue(definition.Capabilities.Count > 0);
    }

    [TestMethod]
    public void Navigate_Definition_IsAllowed_WithNavigationFailures()
    {
        var definition = library.GetDefinition(NodalOsStepKind.Navigate);

        Assert.IsTrue(definition.IsAllowedInV1);
        CollectionAssert.Contains(definition.PossibleFailureKinds.ToList(), NexaFailureKind.NavigationTimeout);
        CollectionAssert.Contains(definition.PossibleFailureKinds.ToList(), NexaFailureKind.PageLoadFailed);
    }

    [TestMethod]
    public void Read_Definition_IsReadOnly()
    {
        var definition = library.GetDefinition(NodalOsStepKind.Read);

        Assert.IsTrue(definition.IsReadOnlyCapable);
        CollectionAssert.Contains(definition.Capabilities.ToList(), NodalOsStepCapabilityKind.ReadOnly);
    }

    [TestMethod]
    public void Extract_Definition_IsReadOnlyCapable()
    {
        var definition = library.GetDefinition(NodalOsStepKind.Extract);

        Assert.IsTrue(definition.IsReadOnlyCapable);
        CollectionAssert.Contains(definition.Capabilities.ToList(), NodalOsStepCapabilityKind.Extraction);
    }

    [TestMethod]
    public void AskHuman_IsFirstClassAllowedStep()
    {
        var definition = library.GetDefinition(NodalOsStepKind.AskHuman);

        Assert.IsTrue(definition.IsAllowedInV1);
        CollectionAssert.Contains(definition.PossibleFailureKinds.ToList(), NexaFailureKind.HumanInputRequired);
    }

    [TestMethod]
    public void Stop_IsFirstClassAllowedStep()
    {
        var definition = library.GetDefinition(NodalOsStepKind.Stop);

        Assert.IsTrue(definition.IsAllowedInV1);
        Assert.AreEqual(NodalOsStepRiskLevel.None, definition.RiskLevel);
    }

    [TestMethod]
    public void DownloadRequest_RequiresApproval()
    {
        var result = validator.Validate(Context(NodalOsStepKind.DownloadRequest, requiresFileDownload: true));

        Assert.IsTrue(result.IsValid);
        Assert.IsTrue(result.RequiresApproval);
        CollectionAssert.Contains(result.FailureKinds.ToList(), NexaFailureKind.DownloadBlocked);
    }

    [TestMethod]
    public void UploadRequest_RequiresApproval()
    {
        var result = validator.Validate(Context(NodalOsStepKind.UploadRequest, requiresFileUpload: true));

        Assert.IsTrue(result.IsValid);
        Assert.IsTrue(result.RequiresApproval);
        CollectionAssert.Contains(result.FailureKinds.ToList(), NexaFailureKind.UploadBlocked);
    }

    [TestMethod]
    public void TypeSensitiveField_RequiresApproval()
    {
        var result = validator.Validate(Context(NodalOsStepKind.Type, targetsSensitiveField: true));

        Assert.IsTrue(result.IsValid);
        Assert.IsTrue(result.RequiresApproval);
        CollectionAssert.Contains(result.FailureKinds.ToList(), NexaFailureKind.SensitiveDataRisk);
    }

    [TestMethod]
    public void TypeSensitiveField_WithSensitiveActionsBlocked_IsBlocked()
    {
        var result = validator.Validate(Context(
            NodalOsStepKind.Type,
            targetsSensitiveField: true,
            globalSensitiveActionsBlocked: true));

        Assert.IsFalse(result.IsValid);
        Assert.IsFalse(result.IsAllowed);
        CollectionAssert.Contains(result.FailureKinds.ToList(), NexaFailureKind.PolicyBlocked);
    }

    [TestMethod]
    public void SubmitLikeContext_IsBlocked()
    {
        var result = validator.Validate(Context(NodalOsStepKind.Click, isSubmitLike: true));

        Assert.IsFalse(result.IsValid);
        CollectionAssert.Contains(result.FailureKinds.ToList(), NexaFailureKind.PolicyBlocked);
        CollectionAssert.Contains(result.FailureKinds.ToList(), NexaFailureKind.ApprovalRequired);
    }

    [TestMethod]
    public void LoginRelatedContext_IsBlocked()
    {
        var result = validator.Validate(Context(NodalOsStepKind.Type, isLoginRelated: true));

        Assert.IsFalse(result.IsValid);
        CollectionAssert.Contains(result.FailureKinds.ToList(), NexaFailureKind.LoginRequired);
        CollectionAssert.Contains(result.FailureKinds.ToList(), NexaFailureKind.HumanInputRequired);
    }

    [TestMethod]
    public void CaptchaOrTwoFactorContext_IsBlocked()
    {
        var result = validator.Validate(Context(NodalOsStepKind.AskHuman, isCaptchaOrTwoFactorRelated: true));

        Assert.IsFalse(result.IsValid);
        CollectionAssert.Contains(result.FailureKinds.ToList(), NexaFailureKind.CaptchaDetected);
        CollectionAssert.Contains(result.FailureKinds.ToList(), NexaFailureKind.TwoFactorRequired);
    }

    [TestMethod]
    public void DisallowedSensitiveStep_MapsToFailureTaxonomy()
    {
        var result = validator.Validate(Context(
            NodalOsStepKind.Type,
            targetsSensitiveField: true,
            globalSensitiveActionsBlocked: true));

        CollectionAssert.Contains(result.FailureKinds.ToList(), NexaFailureKind.SensitiveDataRisk);
        CollectionAssert.Contains(result.FailureKinds.ToList(), NexaFailureKind.PolicyBlocked);
    }

    [TestMethod]
    public void RecipeActionKind_MapsToStepKind()
    {
        foreach (var actionKind in Enum.GetValues<NodalOsRecipeActionKind>())
        {
            Assert.IsTrue(library.TryMapFromRecipeActionKind(actionKind, out var stepKind));
            Assert.AreEqual(actionKind.ToString(), stepKind.ToString());
        }
    }

    [TestMethod]
    public void StepKind_MapsToRunStepActionKind()
    {
        Assert.AreEqual("navigate", library.MapToRunStepActionKind(NodalOsStepKind.Navigate));
        Assert.AreEqual("ask-human", library.MapToRunStepActionKind(NodalOsStepKind.AskHuman));
        Assert.AreEqual("download-request", library.MapToRunStepActionKind(NodalOsStepKind.DownloadRequest));
    }

    [TestMethod]
    public void DefinitionCoverageValidation_Passes()
    {
        Assert.IsTrue(validator.ValidateDefinitionCoverage());
    }

    [TestMethod]
    public void StepMetadata_SerializesToJson()
    {
        var definition = library.GetDefinition(NodalOsStepKind.Read);

        var json = JsonSerializer.Serialize(definition);
        var roundTrip = JsonSerializer.Deserialize<NodalOsStepDefinition>(json);

        Assert.IsNotNull(roundTrip);
        Assert.AreEqual(definition.StepKind, roundTrip.StepKind);
    }

    [TestMethod]
    public void StepValidationResult_SerializesToJson()
    {
        var result = validator.Validate(Context(NodalOsStepKind.DownloadRequest));

        var json = JsonSerializer.Serialize(result);
        var roundTrip = JsonSerializer.Deserialize<NodalOsStepValidationResult>(json);

        Assert.IsNotNull(roundTrip);
        Assert.AreEqual(result.RequiresApproval, roundTrip.RequiresApproval);
    }

    [TestMethod]
    public void Sanitizer_RemovesOrRejectsSecretLikeContent()
    {
        var unsafeValue = "authorization bearer token cookie=value api_key=123";

        Assert.IsTrue(NodalOsStepLibrarySanitizer.ContainsSecretLikeContent(unsafeValue));

        var sanitized = NodalOsStepLibrarySanitizer.SanitizeLabelOrDescription(unsafeValue);

        Assert.IsFalse(NodalOsStepLibrarySanitizer.ContainsSecretLikeContent(sanitized));
    }

    [TestMethod]
    public void StepLibrary_DoesNotIntroduceExecutionRuntime()
    {
        using var doc = LoadArtifact();
        Assert.IsTrue(doc.RootElement.GetProperty("noExecutionRuntimeImplemented").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("noRuntimeActionsIntroduced").GetBoolean());
    }

    [TestMethod]
    public void StepLibrary_DoesNotBypassRecipePolicy()
    {
        using var doc = LoadArtifact();
        Assert.IsTrue(doc.RootElement.GetProperty("noRecipeExecutionImplemented").GetBoolean());
    }

    [TestMethod]
    public void StepLibrary_DoesNotBypassGlobalPolicy()
    {
        using var doc = LoadArtifact();
        var root = doc.RootElement;

        Assert.AreEqual("M368-M370", root.GetProperty("milestone").GetString());
        Assert.AreEqual("STEP_LIBRARY_V1_READY_WITH_EXECUTION_DEFERRED", root.GetProperty("readinessDecision").GetString());
        Assert.IsTrue(root.GetProperty("stepLibraryCreated").GetBoolean());
        Assert.IsTrue(root.GetProperty("stepDefinitionsCreated").GetBoolean());
        Assert.IsTrue(root.GetProperty("stepValidatorCreated").GetBoolean());
        Assert.IsTrue(root.GetProperty("stepSanitizerCreated").GetBoolean());
        Assert.IsTrue(root.GetProperty("recipeActionMappingCreated").GetBoolean());
        Assert.IsTrue(root.GetProperty("runStepMappingCreated").GetBoolean());
        Assert.IsTrue(root.GetProperty("allStepKindsHaveDefinitions").GetBoolean());
        Assert.IsTrue(root.GetProperty("allStepsHaveRiskMetadata").GetBoolean());
        Assert.IsTrue(root.GetProperty("downloadRequiresApproval").GetBoolean());
        Assert.IsTrue(root.GetProperty("uploadRequiresApproval").GetBoolean());
        Assert.IsTrue(root.GetProperty("sensitiveTypeRequiresApprovalOrBlocked").GetBoolean());
        Assert.IsTrue(root.GetProperty("submitLikeBlocked").GetBoolean());
        Assert.IsTrue(root.GetProperty("loginRelatedBlocked").GetBoolean());
        Assert.IsTrue(root.GetProperty("captchaTwoFactorBlocked").GetBoolean());
        Assert.IsTrue(root.GetProperty("askHumanFirstClass").GetBoolean());
        Assert.IsTrue(root.GetProperty("stopFirstClass").GetBoolean());
        Assert.IsTrue(root.GetProperty("noSubmitPayDeletePublishSendSignStepInV1").GetBoolean());
        Assert.IsTrue(root.GetProperty("noUiImplemented").GetBoolean());
        Assert.IsTrue(root.GetProperty("noOrchestrationApiImplemented").GetBoolean());
        Assert.AreEqual("M371-M373 Core Legacy Reference Graph or Claude Agent Operations Audit", root.GetProperty("nextRecommendedMilestone").GetString());
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

    private static JsonDocument LoadArtifact() =>
        JsonDocument.Parse(File.ReadAllText(SourcePath(
            "artifacts", "agent-operations", "m370", "step-library-v1-summary.json")));

    private static string SourcePath(params string[] parts) => Path.Combine(new[] { RepoRoot }.Concat(parts).ToArray());
}
