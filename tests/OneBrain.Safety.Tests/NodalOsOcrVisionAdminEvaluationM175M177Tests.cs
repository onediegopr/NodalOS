using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("OcrVisionAdmin")]
[TestCategory("OcrVisionAdminSettings")]
[TestCategory("OcrVisionProviderSettings")]
[TestCategory("OcrVisionSaasProvider")]
[TestCategory("AzureDocumentIntelligence")]
[TestCategory("GoogleDocumentAi")]
[TestCategory("GoogleVisionOcr")]
[TestCategory("OpenAiVisionOcr")]
[TestCategory("MistralOcr")]
[TestCategory("AmazonTextract")]
[TestCategory("OcrVisionEvaluation")]
[TestCategory("OcrVisionBenchmark")]
[TestCategory("OcrVisionRouter")]
[TestCategory("OcrVisionNoAuthority")]
[TestCategory("OcrVisionPrivacy")]
[TestCategory("OcrVisionBudget")]
[TestCategory("OcrVisionProviderRegistry")]
public sealed class NodalOsOcrVisionAdminEvaluationM175M177Tests
{
    private readonly NodalOsOcrVisionProviderRegistryService registryService = new();
    private readonly NodalOsOcrVisionAdminSettingsService admin = new();

    [TestMethod]
    public void AdminViewListsProvidersWithSaasDisabledAndLocalStubsTesting()
    {
        var settings = admin.CreateSettings(registryService.CreateDefaultRegistry());

        Assert.IsTrue(settings.Providers.Count >= 10);
        Assert.IsFalse(settings.StoresApiKeys);
        Assert.IsFalse(settings.CallsRealApi);
        Assert.IsFalse(settings.ProviderExecutable);
        Assert.IsFalse(settings.GrantsAuthority);
        Assert.IsTrue(settings.Providers.Where(p => p.ExternalDataTransfer).All(p => !p.Enabled));
        Assert.IsTrue(settings.Providers.Where(p => p.ExternalDataTransfer).All(p => p.RequiresApiKey));
        Assert.IsTrue(settings.Providers.Any(p => p.Kind == NodalOsOcrVisionProviderKind.LocalPaddleOcr && p.Status == NodalOsOcrVisionProviderStatus.Testing));
        Assert.IsTrue(settings.Providers.Any(p => p.Kind == NodalOsOcrVisionProviderKind.HumanReview && p.Status == NodalOsOcrVisionProviderStatus.Fallback));
    }

    [TestMethod]
    public void AdminPausePriorityBudgetAndApiKeyHandlingRemainModelOnly()
    {
        var registry = registryService.CreateDefaultRegistry();
        var cloud = registry.Providers.Single(p => p.Kind == NodalOsOcrVisionProviderKind.CloudAzureDocumentIntelligence);
        var toggle = admin.ToggleModelOnly(new NodalOsOcrVisionProviderToggleRequest(cloud.ProviderId, Enabled: true, NodalOsOcrVisionApiKeyState.Missing, ModelOnly: true, "try enable"), cloud);
        var pause = admin.PauseModelOnly(new NodalOsOcrVisionProviderPauseRequest(new("local-paddleocr-stub"), Pause: true, "pause", ModelOnly: true));
        var reorder = admin.ReorderModelOnly(new NodalOsOcrVisionProviderPriorityUpdate(new("local-tesseract-stub"), 5, [new("local-paddleocr-stub")], ModelOnly: true));
        var settings = admin.CreateSettings(registry);
        var paddleBudget = settings.Providers.Single(p => p.ProviderId.Value == "local-paddleocr-stub").BudgetSettings;

        Assert.AreEqual(NodalOsOcrVisionAdminDecisionKind.BlockedRealApiKey, toggle.Decision);
        Assert.AreEqual(NodalOsOcrVisionAdminDecisionKind.Paused, pause.Decision);
        Assert.AreEqual(NodalOsOcrVisionAdminDecisionKind.Reordered, reorder.Decision);
        Assert.IsTrue(paddleBudget.ModelOnly);
        Assert.IsFalse(toggle.StoresApiKeys);
        Assert.IsFalse(toggle.ProviderExecutable);
        Assert.IsFalse(reorder.GrantsAuthority);
    }

    [TestMethod]
    public void CloudProviderCannotBecomeExecutableByToggleAlone()
    {
        var provider = registryService.CreateDefaultRegistry().Providers.Single(p => p.Kind == NodalOsOcrVisionProviderKind.CloudOpenAiVision);
        var decision = admin.ToggleModelOnly(new NodalOsOcrVisionProviderToggleRequest(provider.ProviderId, Enabled: true, NodalOsOcrVisionApiKeyState.PlaceholderConfigured, ModelOnly: false, "execute"), provider);

        Assert.AreEqual(NodalOsOcrVisionAdminDecisionKind.BlockedExecutableProvider, decision.Decision);
        Assert.IsFalse(decision.CallsRealApi);
        Assert.IsFalse(decision.ProviderExecutable);
        Assert.IsFalse(decision.GrantsAuthority);
    }

    [TestMethod]
    public void SaasProviderStubsAreDisabledRequireOptInKeyAndRefuseExecution()
    {
        foreach (var stub in SaasStubs())
        {
            var config = stub.Configuration;
            var probe = stub.ProbeExecution();

            Assert.IsFalse(config.Policy.Enabled);
            Assert.IsTrue(config.Policy.RequiresApiKey);
            Assert.IsTrue(config.Policy.RequiresOptIn);
            Assert.IsTrue(config.Policy.ExternalDataTransfer);
            Assert.IsFalse(config.Policy.AllowedForSensitive);
            Assert.IsFalse(config.CallsRealApi);
            Assert.IsFalse(config.StoresSecrets);
            Assert.IsTrue(probe.RefusesExecution);
            Assert.IsFalse(probe.WouldCallHttp);
            Assert.IsFalse(probe.StoresSecret);
            Assert.IsTrue(probe.NoAuthority);
            Assert.IsTrue(probe.BlockReasons.Contains(NodalOsSaasOcrConnectorBlockReason.CannotRunWithoutOptIn));
            Assert.IsTrue(probe.BlockReasons.Contains(NodalOsSaasOcrConnectorBlockReason.CannotRunWithoutSecretVault));
        }
    }

    [TestMethod]
    public void SaasProviderBlocksSensitiveRedactionFailedBudgetMissingAndCurrentProductionPhase()
    {
        var probe = new NodalOsAzureDocumentIntelligenceProviderStub().ProbeExecution(optIn: true, secretVaultConfigured: true, budgetConfigured: false, sensitive: true, redactionStatus: NodalOsGroundingRedactionStatus.RedactionFailed);

        Assert.IsTrue(probe.BlockReasons.Contains(NodalOsSaasOcrConnectorBlockReason.CannotRunOnSensitiveByDefault));
        Assert.IsTrue(probe.BlockReasons.Contains(NodalOsSaasOcrConnectorBlockReason.CannotRunIfRedactionFailed));
        Assert.IsTrue(probe.BlockReasons.Contains(NodalOsSaasOcrConnectorBlockReason.CannotRunIfBudgetMissing));
        Assert.IsTrue(probe.BlockReasons.Contains(NodalOsSaasOcrConnectorBlockReason.CannotRunInProductionCurrentPhase));
        Assert.IsFalse(probe.WouldCallHttp);
    }

    [TestMethod]
    public void EvaluationHarnessCoversFixturesAndGeneratesReport()
    {
        var registry = registryService.EnableModelOnly(registryService.CreateDefaultRegistry(), new("local-paddleocr-stub"));
        registry = registryService.EnableModelOnly(registry, new("local-tesseract-stub"));
        var harness = new NodalOsOcrVisionEvaluationHarness();
        var report = harness.Run(registry);

        Assert.IsTrue(report.TotalCases >= 15);
        Assert.AreEqual(report.TotalCases, report.PassedCases);
        Assert.IsFalse(report.CallsRealOcr);
        Assert.IsFalse(report.CallsRealSaas);
        Assert.IsTrue(report.NoAuthority);
        Assert.IsTrue(report.Results.Any(r => r.Fixture.FixtureId == "simple-ui-crop" && r.Decision.SelectedProviderId?.Value == "local-paddleocr-stub"));
        Assert.IsTrue(report.Results.Any(r => r.Fixture.FixtureId == "invoice-layout" && r.Decision.Status == NodalOsOcrVisionRoutingStatus.CloudDisabled));
        Assert.IsTrue(report.Results.Any(r => r.Fixture.FixtureId == "handwriting-synthetic" && r.Decision.Status == NodalOsOcrVisionRoutingStatus.AskHuman));
        Assert.IsTrue(report.Results.Any(r => r.Fixture.FixtureId == "sensitive-redaction-failed" && r.Decision.Status == NodalOsOcrVisionRoutingStatus.Blocked));
        Assert.IsTrue(report.Results.Any(r => r.Fixture.FixtureId == "full-screen-blocked" && r.Decision.Status == NodalOsOcrVisionRoutingStatus.Blocked));
        Assert.IsTrue(report.Results.Any(r => r.Fixture.FixtureId == "budget-exceeded" && r.Decision.Status == NodalOsOcrVisionRoutingStatus.BlockedByBudget));
    }

    [TestMethod]
    public void EvaluationHarnessCanSelectTesseractWhenPaddleUnavailable()
    {
        var registry = registryService.EnableModelOnly(registryService.CreateDefaultRegistry(), new("local-tesseract-stub"));
        var report = new NodalOsOcrVisionEvaluationHarness().Run(registry);
        var simple = report.Results.Single(r => r.Fixture.FixtureId == "simple-ui-crop");

        Assert.AreEqual("local-tesseract-stub", simple.Decision.SelectedProviderId?.Value);
        Assert.AreEqual(NodalOsOcrVisionRoutingReason.TesseractFallback, simple.Decision.Reason);
        Assert.IsFalse(simple.CallsRealOcr);
        Assert.IsFalse(simple.CallsRealSaas);
    }

    [TestMethod]
    public void EvaluationArtifactsAndAdrRunbookExist()
    {
        var harness = new NodalOsOcrVisionEvaluationHarness();
        var registry = registryService.EnableModelOnly(registryService.CreateDefaultRegistry(), new("local-paddleocr-stub"));
        registry = registryService.EnableModelOnly(registry, new("local-tesseract-stub"));
        var report = harness.Run(registry);
        var json = SourcePath("artifacts", "ocr-vision-evaluation", "m177", "ocr-vision-evaluation-summary.json");
        var markdown = SourcePath("docs", "reports", "ocr-vision-evaluation-m177.md");
        harness.WriteReport(report, json, markdown);

        Assert.IsTrue(File.Exists(json));
        Assert.IsTrue(File.Exists(markdown));
        StringAssert.Contains(File.ReadAllText(markdown), "No real OCR executed: true");
        StringAssert.Contains(File.ReadAllText(SourcePath("docs", "adr", "ocr-vision-admin-saas-evaluation-m175-m177.md")), "SaaS disabled-by-default");
        StringAssert.Contains(File.ReadAllText(SourcePath("docs", "runbooks", "nodal-os-internal-preview-operator-ux-guide-m148-m150.md")), "Reading OCR/Vision Provider Settings");
    }

    private static IReadOnlyList<NodalOsSaasOcrProviderStubBase> SaasStubs() =>
    [
        new NodalOsAzureDocumentIntelligenceProviderStub(),
        new NodalOsGoogleDocumentAiProviderStub(),
        new NodalOsGoogleVisionOcrProviderStub(),
        new NodalOsOpenAiVisionOcrProviderStub(),
        new NodalOsMistralOcrProviderStub(),
        new NodalOsAmazonTextractProviderStub()
    ];

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
