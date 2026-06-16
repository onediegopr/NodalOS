using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("OcrVisionProvider")]
[TestCategory("OcrVisionProviderRegistry")]
[TestCategory("LocalOcrProvider")]
[TestCategory("PaddleOcr")]
[TestCategory("Tesseract")]
[TestCategory("OcrVisionRouter")]
[TestCategory("OcrVisionRoutingPolicy")]
[TestCategory("OcrVisionBudget")]
[TestCategory("OcrVisionPrivacy")]
[TestCategory("OcrVisionNoAuthority")]
[TestCategory("BrowserGroundingSnapshot")]
[TestCategory("GroundingTimelineBinding")]
public sealed class NodalOsOcrVisionM172M174Tests
{
    private readonly NodalOsOcrVisionProviderRegistryService registryService = new();
    private readonly NodalOsGroundingSnapshotBuilder groundingBuilder = new();
    private readonly NodalOsOcrVisionRouter router = new();
    private readonly NodalOsImageCropRedactor redactor = new();

    [TestMethod]
    public void RegistryListsProvidersAndKeepsSaasDisabledByDefault()
    {
        var registry = registryService.CreateDefaultRegistry();

        Assert.IsTrue(registry.Providers.Count >= 5);
        Assert.IsFalse(registry.StoresSecrets);
        Assert.IsFalse(registry.CallsRealApi);
        Assert.IsFalse(registry.GrantsAuthority);
        Assert.IsTrue(registry.Providers.Where(provider => provider.Policy.ExternalDataTransfer).All(provider => !provider.Policy.Enabled));
        Assert.IsTrue(registry.Providers.Where(provider => provider.Policy.ExternalDataTransfer).All(provider => provider.Policy.RequiresApiKey));
        Assert.IsTrue(registry.Providers.All(provider => !provider.CallsRealApi));
    }

    [TestMethod]
    public void LocalProvidersAreDisabledOrTestingByDefaultAndDoNotGrantAuthority()
    {
        var registry = registryService.CreateDefaultRegistry();
        var local = registry.Providers.Where(provider => provider.Kind is NodalOsOcrVisionProviderKind.LocalPaddleOcr or NodalOsOcrVisionProviderKind.LocalTesseract).ToArray();

        Assert.AreEqual(2, local.Length);
        Assert.IsTrue(local.All(provider => provider.Status == NodalOsOcrVisionProviderStatus.Testing));
        Assert.IsTrue(local.All(provider => !provider.Policy.Enabled));
        Assert.IsTrue(local.All(provider => !provider.GrantsAuthority));
    }

    [TestMethod]
    public void ProviderRequiringApiKeyCannotEnableWithoutConfigState()
    {
        var registry = registryService.CreateDefaultRegistry();
        var enabled = registryService.EnableModelOnly(registry, new NodalOsOcrVisionProviderId("cloud-document-ai-disabled"));
        var cloud = enabled.Providers.Single(provider => provider.ProviderId.Value == "cloud-document-ai-disabled");

        Assert.AreEqual(NodalOsOcrVisionProviderStatus.BlockedByPolicy, cloud.Status);
        Assert.IsFalse(cloud.Policy.Enabled);
        StringAssert.Contains(cloud.Policy.DisabledReason, "api key");
        Assert.IsFalse(enabled.StoresSecrets);
    }

    [TestMethod]
    public void SensitiveDataBlocksCloudAndFullScreenBlockedByDefault()
    {
        var registry = registryService.EnableModelOnly(registryService.CreateDefaultRegistry(), new NodalOsOcrVisionProviderId("local-paddleocr-stub"));
        var cloudAllowed = registryService.FilterProviders(registry, NodalOsOcrVisionProviderCapability.ComplexLayout, NodalOsOcrVisionSensitivity.SensitiveSurface, fullScreen: false, cropRedacted: true);
        var fullscreenAllowed = registryService.FilterProviders(registry, NodalOsOcrVisionProviderCapability.SimpleUiCrop, NodalOsOcrVisionSensitivity.Low, fullScreen: true, cropRedacted: true);

        Assert.AreEqual(0, cloudAllowed.Count(provider => provider.Policy.ExternalDataTransfer));
        Assert.AreEqual(0, fullscreenAllowed.Count);
    }

    [TestMethod]
    public void CropOcrAllowedOnlyIfRedactedAndPriorityOrderDeterministic()
    {
        var registry = registryService.EnableModelOnly(registryService.CreateDefaultRegistry(), new NodalOsOcrVisionProviderId("local-paddleocr-stub"));
        registry = registryService.EnableModelOnly(registry, new NodalOsOcrVisionProviderId("local-tesseract-stub"));
        var redactedCrop = registryService.FilterProviders(registry, NodalOsOcrVisionProviderCapability.SimpleUiCrop, NodalOsOcrVisionSensitivity.Low, fullScreen: false, cropRedacted: true);
        var unredactedCrop = registryService.FilterProviders(registry, NodalOsOcrVisionProviderCapability.SimpleUiCrop, NodalOsOcrVisionSensitivity.Low, fullScreen: false, cropRedacted: false);

        Assert.AreEqual("local-paddleocr-stub", redactedCrop.First().ProviderId.Value);
        Assert.AreEqual(0, unredactedCrop.Count);
    }

    [TestMethod]
    public void PaddleOcrStubAcceptsRedactedCropAndDoesNotCallExternalApi()
    {
        var provider = new NodalOsPaddleOcrLocalProviderStub();
        var result = provider.Recognize(LocalRequest());

        Assert.AreEqual(NodalOsLocalOcrStatus.CompletedStub, result.Status);
        Assert.AreEqual(NodalOsOcrEngineHint.PaddleOcr, result.Engine);
        Assert.IsFalse(result.CallsExternalApi);
        Assert.AreEqual(NodalOsOcrAuthorityFlag.NoAuthority, result.AuthorityFlag);
        Assert.IsFalse(result.CanApproveAction);
        Assert.IsFalse(result.CanClick);
        Assert.IsFalse(result.CanSubmit);
    }

    [TestMethod]
    public void TesseractStubFallbackReturnsLowerCapabilityConfidence()
    {
        var paddle = new NodalOsPaddleOcrLocalProviderStub().Recognize(LocalRequest(minConfidence: 0.7));
        var tesseract = new NodalOsTesseractLocalProviderStub().Recognize(LocalRequest(minConfidence: 0.7));

        Assert.AreEqual(NodalOsOcrEngineHint.Tesseract, tesseract.Engine);
        Assert.IsTrue(tesseract.Confidence.Value < paddle.Confidence.Value);
        Assert.IsFalse(tesseract.CallsExternalApi);
    }

    [TestMethod]
    public void LocalOcrBlocksRedactionFailedFullscreenAndUnredactedCrop()
    {
        var provider = new NodalOsPaddleOcrLocalProviderStub();

        Assert.AreEqual(NodalOsLocalOcrStatus.BlockedByRedaction, provider.Recognize(LocalRequest(redaction: NodalOsGroundingRedactionStatus.RedactionFailed)).Status);
        Assert.AreEqual(NodalOsLocalOcrStatus.BlockedFullScreen, provider.Recognize(LocalRequest(fullScreen: true)).Status);
        Assert.AreEqual(NodalOsLocalOcrStatus.BlockedByRedaction, provider.Recognize(LocalRequest(cropRedacted: false)).Status);
    }

    [TestMethod]
    public void LocalOcrLowConfidenceRequiresHumanReview()
    {
        var result = new NodalOsTesseractLocalProviderStub().Recognize(LocalRequest(minConfidence: 0.95));

        Assert.AreEqual(NodalOsLocalOcrStatus.LowConfidenceNeedsHuman, result.Status);
        Assert.IsTrue(result.RequiresHumanReview);
        Assert.IsFalse(result.CanApproveAction);
    }

    [TestMethod]
    public void RouterReturnsNoOcrNeededWhenDomCdpUiaSufficient()
    {
        var decision = router.Route(RoutingRequest(domSufficient: true), registryService.CreateDefaultRegistry());

        Assert.AreEqual(NodalOsOcrVisionRoutingStatus.NoOcrNeeded, decision.Status);
        Assert.AreEqual(NodalOsOcrVisionRoutingReason.NoOcrNeeded, decision.Reason);
        Assert.IsTrue(decision.NoAuthority);
        Assert.IsFalse(decision.CallsRealSaas);
    }

    [TestMethod]
    public void SimpleSafeCropSelectsLocalPaddleAndFallsBackToTesseract()
    {
        var registry = registryService.EnableModelOnly(registryService.CreateDefaultRegistry(), new NodalOsOcrVisionProviderId("local-paddleocr-stub"));
        registry = registryService.EnableModelOnly(registry, new NodalOsOcrVisionProviderId("local-tesseract-stub"));
        var paddle = router.Route(RoutingRequest(), registry);
        var tesseractRegistry = registryService.DisableModelOnly(registry, new NodalOsOcrVisionProviderId("local-paddleocr-stub"));
        var tesseract = router.Route(RoutingRequest(), tesseractRegistry);

        Assert.AreEqual("local-paddleocr-stub", paddle.SelectedProviderId?.Value);
        Assert.AreEqual(NodalOsOcrVisionRoutingReason.LocalPaddlePreferred, paddle.Reason);
        Assert.AreEqual("local-tesseract-stub", tesseract.SelectedProviderId?.Value);
        Assert.AreEqual(NodalOsOcrVisionRoutingReason.TesseractFallback, tesseract.Reason);
    }

    [TestMethod]
    public void ComplexLayoutAndHandwritingDoNotCallCloudWhenDisabled()
    {
        var registry = registryService.CreateDefaultRegistry();
        var complex = router.Route(RoutingRequest(@case: NodalOsOcrVisionCaseClassification.Invoice, complexity: NodalOsOcrVisionComplexity.High), registry);
        var handwriting = router.Route(RoutingRequest(@case: NodalOsOcrVisionCaseClassification.Handwriting, complexity: NodalOsOcrVisionComplexity.VeryHigh), registry);

        Assert.AreEqual(NodalOsOcrVisionRoutingStatus.CloudDisabled, complex.Status);
        Assert.AreEqual(NodalOsOcrVisionRoutingReason.CloudCandidateDisabled, complex.Reason);
        Assert.AreEqual(NodalOsOcrVisionRoutingStatus.AskHuman, handwriting.Status);
        Assert.AreEqual(NodalOsOcrVisionRoutingReason.VlmCandidateDisabled, handwriting.Reason);
        Assert.IsFalse(complex.CallsRealSaas);
        Assert.IsFalse(handwriting.CallsRealSaas);
    }

    [TestMethod]
    public void RouterBlocksSensitiveCloudRedactionFailedAndBudgetExceeded()
    {
        var registry = registryService.CreateDefaultRegistry();
        var sensitive = router.Route(RoutingRequest(sensitivity: NodalOsOcrVisionSensitivity.SensitiveSurface, @case: NodalOsOcrVisionCaseClassification.ComplexLayout), registry);
        var redactionFailed = router.Route(RoutingRequest(quality: NodalOsOcrVisionQuality.RedactionFailed, redaction: NodalOsGroundingRedactionStatus.RedactionFailed), registry);
        var budget = router.Route(RoutingRequest(@case: NodalOsOcrVisionCaseClassification.Invoice, complexity: NodalOsOcrVisionComplexity.High, budget: 0m), registry);

        Assert.AreEqual(NodalOsOcrVisionRoutingReason.SensitiveCloudBlocked, sensitive.Reason);
        Assert.AreEqual(NodalOsOcrVisionRoutingStatus.Blocked, redactionFailed.Status);
        Assert.AreEqual(NodalOsOcrVisionRoutingStatus.BlockedByBudget, budget.Status);
        Assert.IsTrue(redactionFailed.RequiresHumanApproval);
        Assert.IsTrue(budget.RequiresHumanApproval);
    }

    [TestMethod]
    public void OcrVisionAdrAndRunbookExist()
    {
        var adr = File.ReadAllText(SourcePath("docs", "adr", "ocr-vision-provider-router-m172-m174.md"));
        var runbook = File.ReadAllText(SourcePath("docs", "runbooks", "nodal-os-internal-preview-operator-ux-guide-m148-m150.md"));

        StringAssert.Contains(adr, "DOM/CDP/UIA first");
        StringAssert.Contains(adr, "SaaS disabled-by-default");
        StringAssert.Contains(adr, "PaddleOCR");
        StringAssert.Contains(adr, "no-authority");
        StringAssert.Contains(runbook, "Reading OCR/Vision Routing");
        StringAssert.Contains(runbook, "OCR does not authorize actions");
    }

    private NodalOsLocalOcrRequest LocalRequest(
        NodalOsGroundingRedactionStatus redaction = NodalOsGroundingRedactionStatus.RedactedSafe,
        bool fullScreen = false,
        bool cropRedacted = true,
        double minConfidence = 0.7) =>
        new NodalOsLocalOcrRequest(
            "ocr-request-1",
            new NodalOsGroundingSnapshotId("snapshot-1"),
            cropRedacted ? "crop:redacted" : "",
            redaction == NodalOsGroundingRedactionStatus.RedactedSafe ? "screenshot:redacted" : null,
            new NodalOsOcrBoundingBox(1, 2, 120, 40),
            [NodalOsOcrLanguage.English, NodalOsOcrLanguage.Spanish],
            NodalOsOcrVisionDocumentType.UiCrop,
            NodalOsOcrVisionSensitivity.Low,
            redaction,
            MaxCost: 0m,
            minConfidence,
            AllowCloudFallback: false,
            fullScreen,
            cropRedacted,
            NodalOsOcrPurpose.EvidenceDebug,
            Redacted: true)
        {
            RedactionResult = redaction == NodalOsGroundingRedactionStatus.RedactedSafe && cropRedacted
                ? ValidRedactionResult()
                : null
        };

    private NodalOsOcrVisionRoutingRequest RoutingRequest(
        bool domSufficient = false,
        NodalOsOcrVisionCaseClassification @case = NodalOsOcrVisionCaseClassification.SimpleUiCrop,
        NodalOsOcrVisionComplexity complexity = NodalOsOcrVisionComplexity.Low,
        NodalOsOcrVisionQuality quality = NodalOsOcrVisionQuality.Good,
        NodalOsOcrVisionSensitivity sensitivity = NodalOsOcrVisionSensitivity.Low,
        NodalOsGroundingRedactionStatus redaction = NodalOsGroundingRedactionStatus.RedactedSafe,
        decimal budget = 1m)
    {
        var snapshot = groundingBuilder.Create("route-snapshot", redactionStatus: redaction);
        var request = new NodalOsOcrVisionRoutingRequest(
            "routing-request-1",
            domSufficient,
            ScreenshotHashDiffOnly: false,
            snapshot,
            "crop:redacted",
            CropRedacted: true,
            FullScreen: false,
            @case,
            NodalOsOcrVisionDocumentType.UiCrop,
            complexity,
            quality,
            sensitivity,
            budget,
            RequiredConfidence: 0.75,
            AllowsCloud: false,
            Redacted: true);
        return request with
        {
            RedactionResult = redaction == NodalOsGroundingRedactionStatus.RedactedSafe
                ? ValidRedactionResult()
                : RedactionResult("uncertain unknown_sensitive")
        };
    }

    private NodalOsImageCropRedactionResult ValidRedactionResult() =>
        RedactionResult("clean local fixture crop");

    private NodalOsImageCropRedactionResult RedactionResult(string marker) =>
        redactor.Redact(new NodalOsImageCropRedactionRequest(
            "redaction-test",
            new NodalOsGroundingSnapshotId("snapshot-1"),
            "crop:redacted",
            System.Text.Encoding.UTF8.GetBytes(marker),
            new NodalOsOcrBoundingBox(1, 2, 120, 40),
            "test-fixture",
            NodalOsOcrVisionSensitivity.Low,
            NodalOsOcrPurpose.EvidenceDebug,
            AllowPersistence: false,
            AllowFullScreen: false,
            redactor.DefaultPolicy()));

    private static string SourcePath(params string[] relativePath)
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "OneBrain.slnx")))
                return Path.Combine(new[] { current.FullName }.Concat(relativePath).ToArray());
            current = current.Parent;
        }

        Assert.Fail("Could not locate repository root.");
        return "";
    }
}
