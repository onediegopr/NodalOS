using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("OcrImageRedaction")]
[TestCategory("OcrRedactionPipeline")]
[TestCategory("OcrRedactionPrecondition")]
[TestCategory("OcrVisionRouter")]
[TestCategory("OcrVisionProviderRegistry")]
[TestCategory("OcrVisionControlledActivation")]
[TestCategory("OcrVisionActivationGate")]
[TestCategory("OcrVisionPrivacy")]
[TestCategory("OcrVisionNoAuthority")]
[TestCategory("OcrVisionBudget")]
[TestCategory("BrowserGroundingSnapshot")]
[TestCategory("LocalOcrWorkerBoundary")]
[TestCategory("PrivatePreviewReadiness")]
[TestCategory("LocalPreviewReleaseCandidate")]
[TestCategory("NodalOsNamingAudit")]
[TestCategory("BrowserRuntimePhaseGate")]
public sealed class NodalOsOcrRedactionPreconditionM181Tests
{
    private readonly NodalOsImageCropRedactor redactor = new();
    private readonly NodalOsOcrVisionProviderRegistryService registryService = new();
    private readonly NodalOsOcrVisionRouter router = new();
    private readonly NodalOsGroundingSnapshotBuilder grounding = new();
    private readonly NodalOsOcrRealActivationGate activationGate = new();

    [TestMethod]
    public void CleanSyntheticCropIsValidatedForOcrWithoutRawPersistence()
    {
        var result = Redact("plain clean synthetic crop");

        Assert.AreEqual(NodalOsImageRedactionDecision.CleanNoRedactionRequired, result.Decision);
        Assert.IsTrue(result.CropRedacted);
        Assert.IsTrue(result.SafeForOcr);
        Assert.IsTrue(result.SafeForPersistence);
        Assert.IsFalse(result.OriginalRawPersisted);
        Assert.IsFalse(result.Evidence.OriginalRawPersisted);
        Assert.IsTrue(result.NoAuthority);
    }

    [TestMethod]
    public void PasswordAndPiiCropsRequireRedactionEvidence()
    {
        var password = Redact("visible password=masked-value");
        var pii = Redact("email user@example.test phone:+1-555-0100 document_id:ABC123");

        Assert.AreEqual(NodalOsImageRedactionDecision.Redacted, password.Decision);
        Assert.AreEqual(NodalOsImageRedactionDecision.Redacted, pii.Decision);
        Assert.IsTrue(password.Findings.Any(f => f.Kind == NodalOsImageRedactionFindingKind.PasswordField));
        Assert.IsTrue(pii.Findings.Any(f => f.Kind == NodalOsImageRedactionFindingKind.EmailLikeText));
        Assert.IsTrue(pii.Findings.Any(f => f.Kind == NodalOsImageRedactionFindingKind.PhoneLikeText));
        Assert.IsTrue(pii.Findings.Any(f => f.Kind == NodalOsImageRedactionFindingKind.DocumentIdLikeText));
        Assert.IsFalse(password.OriginalRawPersisted);
        Assert.IsTrue(password.SafeForOcr);
    }

    [TestMethod]
    public void TokenJwtCookieApiKeyAndSensitiveCropsFailClosed()
    {
        var token = Redact("token=abc bearer value");
        var jwt = Redact("eyJhbGciOiJIUzI1NiJ9.payload.signature");
        var cookie = Redact("cookie sessionid=abc");
        var apiKey = Redact("api_key=sk-test-not-real");
        var sensitive = Redact("sensitive internal keyword");

        foreach (var result in new[] { token, jwt, cookie, apiKey, sensitive })
        {
            Assert.AreEqual(NodalOsImageRedactionDecision.BlockedSensitive, result.Decision);
            Assert.IsFalse(result.SafeForOcr);
            Assert.IsFalse(result.CropRedacted);
            Assert.IsFalse(result.OriginalRawPersisted);
        }
    }

    [TestMethod]
    public void RedactionUncertainFailsClosed()
    {
        var result = Redact("blurred_secret uncertain unknown_sensitive low_confidence");

        Assert.AreEqual(NodalOsImageRedactionDecision.RedactionFailed, result.Decision);
        Assert.IsFalse(result.SafeForOcr);
        Assert.IsFalse(result.SafeForPersistence);
        Assert.IsTrue(result.Findings.Any(f => f.Kind == NodalOsImageRedactionFindingKind.RedactionEngineUncertain));
    }

    [TestMethod]
    public void OcrRoutingWithoutVerifiedRedactionResultIsBlocked()
    {
        var registry = EnabledLocalRegistry();
        var decision = router.Route(RoutingRequest(redaction: null), registry);

        Assert.AreEqual(NodalOsOcrVisionRoutingStatus.Blocked, decision.Status);
        Assert.AreEqual(NodalOsOcrVisionRoutingReason.RedactionFailedBlocked, decision.Reason);
        Assert.IsTrue(decision.RequiresHumanApproval);
        Assert.IsTrue(decision.NoAuthority);
    }

    [TestMethod]
    public void OcrRoutingWithRedactionFailedOrBlockedSensitiveIsBlocked()
    {
        var registry = EnabledLocalRegistry();
        var failed = router.Route(RoutingRequest(redaction: Redact("uncertain unknown_sensitive")), registry);
        var sensitive = router.Route(RoutingRequest(redaction: Redact("token=abc")), registry);

        Assert.AreEqual(NodalOsOcrVisionRoutingStatus.Blocked, failed.Status);
        Assert.AreEqual(NodalOsOcrVisionRoutingReason.RedactionFailedBlocked, failed.Reason);
        Assert.AreEqual(NodalOsOcrVisionRoutingStatus.AskHuman, sensitive.Status);
        Assert.AreEqual(NodalOsOcrVisionRoutingReason.SensitiveCloudBlocked, sensitive.Reason);
        Assert.IsFalse(failed.CallsRealSaas);
        Assert.IsFalse(sensitive.CallsRealSaas);
    }

    [TestMethod]
    public void OcrRoutingWithValidRedactionResultCanSelectLocalStubAndFullScreenStillBlocks()
    {
        var registry = EnabledLocalRegistry();
        var valid = router.Route(RoutingRequest(redaction: Redact("clean local crop")), registry);
        var fullscreen = router.Route(RoutingRequest(redaction: Redact("clean local crop"), fullScreen: true), registry);

        Assert.AreEqual(NodalOsOcrVisionRoutingStatus.ProviderSelected, valid.Status);
        Assert.AreEqual("local-paddleocr-stub", valid.SelectedProviderId?.Value);
        Assert.AreEqual(NodalOsOcrVisionRoutingStatus.Blocked, fullscreen.Status);
        Assert.IsTrue(valid.NoAuthority);
    }

    [TestMethod]
    public void CloudDetectionUsesExplicitExternalTransferPolicy()
    {
        var externalNonCloudName = Provider("external-document-disabled", NodalOsOcrVisionProviderKind.DisabledStub, external: true, enabled: true);
        var localCloudNamed = Provider("cloud-fake-local", NodalOsOcrVisionProviderKind.LocalOpenSource, external: false, enabled: true);
        var registry = new NodalOsOcrVisionProviderRegistry([externalNonCloudName, localCloudNamed], StoresSecrets: false, CallsRealApi: false, GrantsAuthority: false, Redacted: true);

        var externalSensitive = registryService.FilterProviders(registry, NodalOsOcrVisionProviderCapability.SimpleUiCrop, NodalOsOcrVisionSensitivity.SensitiveSurface, fullScreen: false, cropRedacted: true);
        var localLowRisk = registryService.FilterProviders(registry, NodalOsOcrVisionProviderCapability.SimpleUiCrop, NodalOsOcrVisionSensitivity.Low, fullScreen: false, cropRedacted: true);

        Assert.IsTrue(externalNonCloudName.RequiresExternalDataTransfer);
        Assert.IsFalse(localCloudNamed.RequiresExternalDataTransfer);
        Assert.IsFalse(externalSensitive.Any(p => p.ProviderId.Value == "external-document-disabled"));
        Assert.IsTrue(localLowRisk.Any(p => p.ProviderId.Value == "cloud-fake-local"));
    }

    [TestMethod]
    public void ActivationGateUsesExplicitExternalTransferPolicyAndKeepsRealOcrFalse()
    {
        var externalNoCloudName = activationGate.Evaluate(Readiness(requiresExternalTransfer: true));
        var localCloudNamed = activationGate.Evaluate(Readiness(requiresExternalTransfer: false));

        Assert.AreEqual(NodalOsOcrActivationDecisionKind.BlockedByDefault, externalNoCloudName.Decision);
        Assert.AreEqual(NodalOsOcrActivationDecisionKind.ReadyForSyntheticOnly, localCloudNamed.Decision);
        Assert.IsFalse(externalNoCloudName.RealOcrEnabled);
        Assert.IsFalse(externalNoCloudName.RealSaasEnabled);
        Assert.IsFalse(localCloudNamed.RealOcrEnabled);
        Assert.IsTrue(localCloudNamed.NoAuthority);
    }

    [TestMethod]
    public void SaasRemainsDisabledAndActivationGateRealOcrFalse()
    {
        var registry = registryService.CreateDefaultRegistry();
        var saas = registry.Providers.Where(p => p.RequiresExternalDataTransfer).ToArray();
        var decision = activationGate.Evaluate(activationGate.CurrentPhaseReadiness());

        Assert.IsTrue(saas.Length > 0);
        Assert.IsTrue(saas.All(p => !p.Policy.Enabled));
        Assert.IsTrue(saas.All(p => !p.CallsRealApi));
        Assert.AreEqual(NodalOsOcrActivationDecisionKind.BlockedByDefault, decision.Decision);
        Assert.IsFalse(decision.RealOcrEnabled);
        Assert.IsFalse(decision.RealSaasEnabled);
    }

    [TestMethod]
    public void AdrAndReportExist()
    {
        var adr = File.ReadAllText(SourcePath("docs", "adr", "ocr-real-redaction-precondition-m181.md"));
        var report = File.ReadAllText(SourcePath("docs", "reports", "ocr-redaction-precondition-readiness-m181.md"));

        StringAssert.Contains(adr, "CropRedacted cannot be an assumed flag");
        StringAssert.Contains(adr, "fail-closed");
        StringAssert.Contains(adr, "Policy.ExternalDataTransfer");
        StringAssert.Contains(report, "OCR real remains disabled");
        StringAssert.Contains(report, "worker skeleton");
    }

    private NodalOsImageCropRedactionResult Redact(string marker, NodalOsOcrVisionSensitivity sensitivity = NodalOsOcrVisionSensitivity.Low) =>
        redactor.Redact(new NodalOsImageCropRedactionRequest(
            $"redaction-{Guid.NewGuid():N}",
            new NodalOsGroundingSnapshotId("snapshot-redaction"),
            "crop:redacted",
            System.Text.Encoding.UTF8.GetBytes(marker),
            new NodalOsOcrBoundingBox(1, 2, 320, 120),
            "m181-test-fixture",
            sensitivity,
            NodalOsOcrPurpose.EvidenceDebug,
            AllowPersistence: false,
            AllowFullScreen: false,
            redactor.DefaultPolicy()));

    private NodalOsOcrVisionRoutingRequest RoutingRequest(NodalOsImageCropRedactionResult? redaction, bool fullScreen = false)
    {
        var request = new NodalOsOcrVisionRoutingRequest(
            "routing-m181",
            DomCdpUiaSufficient: false,
            ScreenshotHashDiffOnly: false,
            grounding.Create("snapshot-m181", redactionStatus: NodalOsGroundingRedactionStatus.RedactedSafe),
            "crop:redacted",
            CropRedacted: redaction?.CropRedacted ?? true,
            fullScreen,
            NodalOsOcrVisionCaseClassification.SimpleUiCrop,
            NodalOsOcrVisionDocumentType.UiCrop,
            NodalOsOcrVisionComplexity.Low,
            NodalOsOcrVisionQuality.Good,
            NodalOsOcrVisionSensitivity.Low,
            MaxEstimatedCost: 1m,
            RequiredConfidence: 0.75,
            AllowsCloud: false,
            Redacted: true);
        return request with { RedactionResult = redaction };
    }

    private NodalOsOcrVisionProviderRegistry EnabledLocalRegistry()
    {
        var registry = registryService.EnableModelOnly(registryService.CreateDefaultRegistry(), new("local-paddleocr-stub"));
        return registryService.EnableModelOnly(registry, new("local-tesseract-stub"));
    }

    private static NodalOsOcrVisionProviderConfiguration Provider(string id, NodalOsOcrVisionProviderKind kind, bool external, bool enabled)
    {
        var providerId = new NodalOsOcrVisionProviderId(id);
        return new NodalOsOcrVisionProviderConfiguration(
            providerId,
            kind,
            enabled ? NodalOsOcrVisionProviderStatus.Testing : NodalOsOcrVisionProviderStatus.Disabled,
            NodalOsOcrVisionProviderCapability.SimpleUiCrop,
            new NodalOsOcrVisionProviderPolicy(
                enabled,
                RequiresOptIn: true,
                RequiresApiKey: external,
                ApiKeyConfigured: false,
                ExternalDataTransfer: external,
                AllowedForSensitive: false,
                AllowedForFullScreen: false,
                AllowedForCrops: true,
                MaxCostPerPage: 0m,
                MaxCostPerImage: 0m,
                DailyBudget: 0m,
                MonthlyBudget: 0m,
                MinConfidence: 0.8,
                MaxLatencyMs: 1000,
                Priority: external ? 10 : 20,
                FallbackOrder: [],
                DisabledReason: "test provider",
                AuditRequired: true,
                RedactionRequired: true),
            new NodalOsOcrVisionProviderCostProfile(0m, 0m, 0m, 0m, "USD"),
            new NodalOsOcrVisionProviderPerformanceProfile(1000, 0.8, "medium"),
            new NodalOsOcrVisionProviderPrivacyProfile(external, StoresInput: false, AllowsSensitive: false, RequiresRedaction: true, external ? "external transfer" : "local"),
            StoresSecrets: false,
            CallsRealApi: false,
            GrantsAuthority: false,
            Redacted: true);
    }

    private static NodalOsOcrActivationReadiness Readiness(bool requiresExternalTransfer) =>
        new NodalOsOcrActivationReadiness(
            new NodalOsOcrVisionProviderId(requiresExternalTransfer ? "external-noncloud-name" : "cloud-fake-local"),
            NodalOsOcrVisionProviderKind.LocalOpenSource,
            new NodalOsOcrActivationScope(NodalOsOcrActivationScopeKind.SyntheticOnly, LocalOnly: !requiresExternalTransfer, AllowsSaas: requiresExternalTransfer, AllowsFullScreen: false, AllowsSensitive: false, "synthetic-only"),
            ProviderExplicitlyEnabled: true,
            LocalWorkerInstalled: true,
            LocalWorkerAvailable: true,
            OptIn: true,
            RedactionGatePassed: true,
            SensitivePolicyPassed: true,
            FullScreenDisabledOrApproved: true,
            BudgetConfigured: true,
            PrivacyProfileAccepted: true,
            new NodalOsOcrActivationAuditEvidence(Present: true, [new NodalOsGroundingEvidenceRef("m181:audit:redacted", "m181 audit fixture", Redacted: true)], "audit present", Redacted: true),
            NoAuthorityConfirmed: true,
            HumanEscalationPolicyConfigured: true,
            EvaluationHarnessPassed: true,
            RollbackPauseConfigured: true,
            CurrentPhaseAllowsSaasReal: false,
            Redacted: true)
        {
            RequiresExternalDataTransfer = requiresExternalTransfer
        };

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
