using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.BrowserExecutor.Cdp;

public sealed class NodalOsOcrVisionProviderRegistryService
{
    public NodalOsOcrVisionProviderRegistry CreateDefaultRegistry() =>
        new(DefaultProviders(), StoresSecrets: false, CallsRealApi: false, GrantsAuthority: false, Redacted: true);

    public IReadOnlyList<NodalOsOcrVisionProviderConfiguration> ListProviders(NodalOsOcrVisionProviderRegistry registry) =>
        registry.Providers.OrderBy(provider => provider.Policy.Priority).ThenBy(provider => provider.ProviderId.Value, StringComparer.OrdinalIgnoreCase).ToArray();

    public NodalOsOcrVisionProviderRegistry EnableModelOnly(
        NodalOsOcrVisionProviderRegistry registry,
        NodalOsOcrVisionProviderId providerId,
        bool apiKeyConfigured = false)
    {
        var providers = registry.Providers.Select(provider =>
        {
            if (!Same(provider.ProviderId, providerId))
                return provider;

            if (provider.Policy.RequiresApiKey && !apiKeyConfigured)
                return provider with
                {
                    Status = NodalOsOcrVisionProviderStatus.BlockedByPolicy,
                    Policy = provider.Policy with { Enabled = false, ApiKeyConfigured = false, DisabledReason = "api key/config state missing; no real key stored" }
                };

            return provider with
            {
                Status = provider.Kind is NodalOsOcrVisionProviderKind.LocalPaddleOcr or NodalOsOcrVisionProviderKind.LocalTesseract
                    ? NodalOsOcrVisionProviderStatus.Testing
                    : NodalOsOcrVisionProviderStatus.ShadowOnly,
                Policy = provider.Policy with { Enabled = true, ApiKeyConfigured = apiKeyConfigured, DisabledReason = "model-only enabled; no real OCR call" }
            };
        }).ToArray();
        return registry with { Providers = providers, StoresSecrets = false, CallsRealApi = false, GrantsAuthority = false };
    }

    public NodalOsOcrVisionProviderRegistry DisableModelOnly(
        NodalOsOcrVisionProviderRegistry registry,
        NodalOsOcrVisionProviderId providerId,
        string reason = "disabled by policy") =>
        registry with
        {
            Providers = registry.Providers.Select(provider => Same(provider.ProviderId, providerId)
                ? provider with { Status = NodalOsOcrVisionProviderStatus.Disabled, Policy = provider.Policy with { Enabled = false, DisabledReason = BrowserCredentialRedactor.Redact(reason) } }
                : provider).ToArray()
        };

    public IReadOnlyList<NodalOsOcrVisionProviderConfiguration> FilterProviders(
        NodalOsOcrVisionProviderRegistry registry,
        NodalOsOcrVisionProviderCapability requiredCapability,
        NodalOsOcrVisionSensitivity sensitivity,
        bool fullScreen,
        bool cropRedacted)
    {
        return ListProviders(registry)
            .Where(provider => provider.Policy.Enabled)
            .Where(provider => provider.Capabilities.HasFlag(requiredCapability))
            .Where(provider => !fullScreen || provider.Policy.AllowedForFullScreen)
            .Where(provider => cropRedacted && provider.Policy.AllowedForCrops)
            .Where(provider => sensitivity is not (NodalOsOcrVisionSensitivity.SensitiveSurface or NodalOsOcrVisionSensitivity.Credentials or NodalOsOcrVisionSensitivity.PersonalData or NodalOsOcrVisionSensitivity.Payment)
                || provider.Policy.AllowedForSensitive)
            .Where(provider => !provider.Policy.ExternalDataTransfer || sensitivity is NodalOsOcrVisionSensitivity.None or NodalOsOcrVisionSensitivity.Low)
            .ToArray();
    }

    internal static NodalOsOcrVisionProviderConfiguration CreateProviderStub(
        string id,
        NodalOsOcrVisionProviderKind kind,
        NodalOsOcrVisionProviderCapability capabilities,
        int priority,
        double confidence,
        int latency,
        decimal maxCostPerImage,
        decimal maxCostPerPage,
        string disabledReason) =>
        Provider(
            id,
            kind,
            NodalOsOcrVisionProviderStatus.Disabled,
            capabilities,
            enabled: false,
            requiresApiKey: true,
            external: true,
            sensitive: false,
            fullScreen: false,
            crops: true,
            priority,
            confidence,
            latency,
            disabledReason) with
        {
            CostProfile = new NodalOsOcrVisionProviderCostProfile(maxCostPerPage, maxCostPerImage, 0m, 0m, "USD")
        };

    private static IReadOnlyList<NodalOsOcrVisionProviderConfiguration> DefaultProviders() =>
    [
        Provider(
            "local-paddleocr-stub",
            NodalOsOcrVisionProviderKind.LocalPaddleOcr,
            NodalOsOcrVisionProviderStatus.Testing,
            NodalOsOcrVisionProviderCapability.PrintedText | NodalOsOcrVisionProviderCapability.SimpleUiCrop | NodalOsOcrVisionProviderCapability.ScreenshotUi | NodalOsOcrVisionProviderCapability.BoundingBoxes | NodalOsOcrVisionProviderCapability.StructuredJson,
            enabled: false,
            requiresApiKey: false,
            external: false,
            sensitive: true,
            fullScreen: false,
            crops: true,
            priority: 10,
            confidence: 0.86,
            latency: 1500,
            disabledReason: "local OCR is model-only/disabled-by-default; future worker candidate"),
        Provider(
            "local-tesseract-stub",
            NodalOsOcrVisionProviderKind.LocalTesseract,
            NodalOsOcrVisionProviderStatus.Testing,
            NodalOsOcrVisionProviderCapability.PrintedText | NodalOsOcrVisionProviderCapability.SimpleUiCrop | NodalOsOcrVisionProviderCapability.BoundingBoxes,
            enabled: false,
            requiresApiKey: false,
            external: false,
            sensitive: true,
            fullScreen: false,
            crops: true,
            priority: 20,
            confidence: 0.72,
            latency: 900,
            disabledReason: "local lightweight fallback is model-only/disabled-by-default"),
        Provider(
            "cloud-document-ai-disabled",
            NodalOsOcrVisionProviderKind.CloudDocumentAi,
            NodalOsOcrVisionProviderStatus.Disabled,
            NodalOsOcrVisionProviderCapability.ComplexLayout | NodalOsOcrVisionProviderCapability.Tables | NodalOsOcrVisionProviderCapability.Forms | NodalOsOcrVisionProviderCapability.Invoices | NodalOsOcrVisionProviderCapability.Receipts | NodalOsOcrVisionProviderCapability.Pdf | NodalOsOcrVisionProviderCapability.MultiPage | NodalOsOcrVisionProviderCapability.StructuredJson,
            enabled: false,
            requiresApiKey: true,
            external: true,
            sensitive: false,
            fullScreen: false,
            crops: true,
            priority: 100,
            confidence: 0.91,
            latency: 5000,
            disabledReason: "cloud OCR disabled-by-default; no API key, no real SaaS call"),
        CreateProviderStub(
            "azure-document-intelligence-disabled",
            NodalOsOcrVisionProviderKind.CloudAzureDocumentIntelligence,
            NodalOsOcrVisionProviderCapability.ComplexLayout | NodalOsOcrVisionProviderCapability.Forms | NodalOsOcrVisionProviderCapability.Invoices | NodalOsOcrVisionProviderCapability.Receipts | NodalOsOcrVisionProviderCapability.Tables | NodalOsOcrVisionProviderCapability.Pdf | NodalOsOcrVisionProviderCapability.StructuredJson,
            priority: 110,
            confidence: 0.92,
            latency: 5200,
            maxCostPerImage: 0.02m,
            maxCostPerPage: 0.05m,
            disabledReason: "Azure Document Intelligence stub disabled-by-default; no endpoint, no key, no HTTP"),
        CreateProviderStub(
            "google-document-ai-disabled",
            NodalOsOcrVisionProviderKind.CloudGoogleDocumentAi,
            NodalOsOcrVisionProviderCapability.ComplexLayout | NodalOsOcrVisionProviderCapability.Forms | NodalOsOcrVisionProviderCapability.Pdf | NodalOsOcrVisionProviderCapability.MultiPage | NodalOsOcrVisionProviderCapability.StructuredJson,
            priority: 120,
            confidence: 0.91,
            latency: 5400,
            maxCostPerImage: 0.02m,
            maxCostPerPage: 0.05m,
            disabledReason: "Google Document AI stub disabled-by-default; no endpoint, no key, no HTTP"),
        CreateProviderStub(
            "google-vision-ocr-disabled",
            NodalOsOcrVisionProviderKind.CloudGoogleVision,
            NodalOsOcrVisionProviderCapability.PrintedText | NodalOsOcrVisionProviderCapability.Handwriting | NodalOsOcrVisionProviderCapability.LowQualityImage | NodalOsOcrVisionProviderCapability.BoundingBoxes,
            priority: 130,
            confidence: 0.86,
            latency: 4200,
            maxCostPerImage: 0.015m,
            maxCostPerPage: 0.03m,
            disabledReason: "Google Vision OCR stub disabled-by-default; no endpoint, no key, no HTTP"),
        CreateProviderStub(
            "mistral-ocr-disabled",
            NodalOsOcrVisionProviderKind.CloudMistralOcr,
            NodalOsOcrVisionProviderCapability.Pdf | NodalOsOcrVisionProviderCapability.MarkdownOutput | NodalOsOcrVisionProviderCapability.ComplexLayout | NodalOsOcrVisionProviderCapability.StructuredJson,
            priority: 140,
            confidence: 0.88,
            latency: 4800,
            maxCostPerImage: 0.018m,
            maxCostPerPage: 0.04m,
            disabledReason: "Mistral OCR stub disabled-by-default; no endpoint, no key, no HTTP"),
        CreateProviderStub(
            "amazon-textract-disabled",
            NodalOsOcrVisionProviderKind.CloudAmazonTextract,
            NodalOsOcrVisionProviderCapability.Forms | NodalOsOcrVisionProviderCapability.Tables | NodalOsOcrVisionProviderCapability.IdentityDocuments | NodalOsOcrVisionProviderCapability.Invoices | NodalOsOcrVisionProviderCapability.Receipts,
            priority: 150,
            confidence: 0.9,
            latency: 5600,
            maxCostPerImage: 0.02m,
            maxCostPerPage: 0.05m,
            disabledReason: "Amazon Textract stub disabled-by-default; no endpoint, no key, no HTTP"),
        Provider(
            "cloud-openai-vision-disabled",
            NodalOsOcrVisionProviderKind.CloudOpenAiVision,
            NodalOsOcrVisionProviderStatus.Disabled,
            NodalOsOcrVisionProviderCapability.ScreenshotUi | NodalOsOcrVisionProviderCapability.Handwriting | NodalOsOcrVisionProviderCapability.MixedPrintedHandwriting | NodalOsOcrVisionProviderCapability.LowQualityImage | NodalOsOcrVisionProviderCapability.MarkdownOutput,
            enabled: false,
            requiresApiKey: true,
            external: true,
            sensitive: false,
            fullScreen: false,
            crops: true,
            priority: 200,
            confidence: 0.88,
            latency: 6000,
            disabledReason: "VLM candidate disabled-by-default; no API key, no real SaaS call"),
        Provider(
            "human-review",
            NodalOsOcrVisionProviderKind.HumanReview,
            NodalOsOcrVisionProviderStatus.Fallback,
            NodalOsOcrVisionProviderCapability.PrintedText | NodalOsOcrVisionProviderCapability.SimpleUiCrop | NodalOsOcrVisionProviderCapability.ComplexLayout | NodalOsOcrVisionProviderCapability.Handwriting | NodalOsOcrVisionProviderCapability.ScreenshotUi,
            enabled: true,
            requiresApiKey: false,
            external: false,
            sensitive: true,
            fullScreen: false,
            crops: true,
            priority: 900,
            confidence: 1.0,
            latency: 0,
            disabledReason: "human-in-the-loop fallback; no automation authority")
    ];

    private static NodalOsOcrVisionProviderConfiguration Provider(
        string id,
        NodalOsOcrVisionProviderKind kind,
        NodalOsOcrVisionProviderStatus status,
        NodalOsOcrVisionProviderCapability capabilities,
        bool enabled,
        bool requiresApiKey,
        bool external,
        bool sensitive,
        bool fullScreen,
        bool crops,
        int priority,
        double confidence,
        int latency,
        string disabledReason)
    {
        var providerId = new NodalOsOcrVisionProviderId(id);
        return new NodalOsOcrVisionProviderConfiguration(
            providerId,
            kind,
            status,
            capabilities,
            new NodalOsOcrVisionProviderPolicy(
                enabled,
                RequiresOptIn: true,
                requiresApiKey,
                ApiKeyConfigured: false,
                ExternalDataTransfer: external,
                AllowedForSensitive: sensitive,
                AllowedForFullScreen: fullScreen,
                AllowedForCrops: crops,
                MaxCostPerPage: external ? 0.05m : 0m,
                MaxCostPerImage: external ? 0.02m : 0m,
                DailyBudget: external ? 1m : 0m,
                MonthlyBudget: external ? 10m : 0m,
                MinConfidence: confidence,
                MaxLatencyMs: latency,
                priority,
                FallbackOrder: [],
                disabledReason,
                AuditRequired: true,
                RedactionRequired: true),
            new NodalOsOcrVisionProviderCostProfile(external ? 0.05m : 0m, external ? 0.02m : 0m, external ? 1m : 0m, external ? 10m : 0m, "USD"),
            new NodalOsOcrVisionProviderPerformanceProfile(latency, confidence, ConfidenceBand(confidence)),
            new NodalOsOcrVisionProviderPrivacyProfile(external, StoresInput: false, sensitive, RequiresRedaction: true, external ? "external transfer disabled until configured" : "local/model-only; no external transfer"),
            StoresSecrets: false,
            CallsRealApi: false,
            GrantsAuthority: false,
            Redacted: true);
    }

    private static string ConfidenceBand(double confidence) => confidence >= 0.85 ? "high" : confidence >= 0.7 ? "medium" : "low";

    private static bool Same(NodalOsOcrVisionProviderId left, NodalOsOcrVisionProviderId right) =>
        string.Equals(left.Value, right.Value, StringComparison.OrdinalIgnoreCase);
}

public sealed class NodalOsOcrVisionAdminSettingsService
{
    public NodalOsOcrVisionAdminSettings CreateSettings(
        NodalOsOcrVisionProviderRegistry registry,
        IReadOnlyDictionary<string, string>? lastEvaluationSummaries = null)
    {
        var summaries = lastEvaluationSummaries ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var views = registry.Providers
            .OrderBy(provider => provider.Policy.Priority)
            .Select(provider => View(provider, summaries.TryGetValue(provider.ProviderId.Value, out var summary) ? summary : "no evaluation yet"))
            .ToArray();
        return new NodalOsOcrVisionAdminSettings(
            "ocr-vision-admin-settings",
            views,
            RoutingRules(),
            StoresApiKeys: false,
            CallsRealApi: false,
            ProviderExecutable: false,
            GrantsAuthority: false,
            Redacted: true);
    }

    public NodalOsOcrVisionAdminDecision ToggleModelOnly(
        NodalOsOcrVisionProviderToggleRequest request,
        NodalOsOcrVisionProviderConfiguration provider)
    {
        if (!request.ModelOnly)
            return Decision(NodalOsOcrVisionAdminDecisionKind.BlockedExecutableProvider, provider.ProviderId, "provider toggle must remain model-only");
        if (provider.Policy.RequiresApiKey && request.ApiKeyState is not (NodalOsOcrVisionApiKeyState.PlaceholderConfigured or NodalOsOcrVisionApiKeyState.SecretVaultRequired))
            return Decision(NodalOsOcrVisionAdminDecisionKind.BlockedRealApiKey, provider.ProviderId, "real API key missing/rejected; use placeholder/vault state only");
        return Decision(NodalOsOcrVisionAdminDecisionKind.ModelOnlyUpdated, provider.ProviderId, "model-only toggle accepted; provider remains non-executable");
    }

    public NodalOsOcrVisionAdminDecision PauseModelOnly(NodalOsOcrVisionProviderPauseRequest request) =>
        request.ModelOnly
            ? Decision(request.Pause ? NodalOsOcrVisionAdminDecisionKind.Paused : NodalOsOcrVisionAdminDecisionKind.Resumed, request.ProviderId, "pause/resume modeled only")
            : Decision(NodalOsOcrVisionAdminDecisionKind.BlockedExecutableProvider, request.ProviderId, "pause/resume cannot affect executable provider");

    public NodalOsOcrVisionAdminDecision ReorderModelOnly(NodalOsOcrVisionProviderPriorityUpdate update) =>
        update.ModelOnly
            ? Decision(NodalOsOcrVisionAdminDecisionKind.Reordered, update.ProviderId, "priority/fallback order modeled deterministically")
            : Decision(NodalOsOcrVisionAdminDecisionKind.BlockedExecutableProvider, update.ProviderId, "priority update must remain model-only");

    private static NodalOsOcrVisionProviderAdminView View(NodalOsOcrVisionProviderConfiguration provider, string evaluationSummary)
    {
        var blocked = new List<string>();
        if (!provider.Policy.Enabled)
            blocked.Add(provider.Policy.DisabledReason);
        if (provider.Policy.RequiresApiKey)
            blocked.Add("requires placeholder/vault API key state; no real key stored");
        if (provider.Policy.ExternalDataTransfer)
            blocked.Add("external transfer disabled in current phase");
        if (!provider.Policy.AllowedForFullScreen)
            blocked.Add("full-screen OCR blocked by default");
        return new NodalOsOcrVisionProviderAdminView(
            provider.ProviderId,
            DisplayName(provider),
            provider.Kind,
            provider.Status,
            provider.Policy.Enabled,
            provider.Status == NodalOsOcrVisionProviderStatus.Paused,
            DisabledByDefault: provider.RequiresExternalDataTransfer || provider.Kind is NodalOsOcrVisionProviderKind.LocalPaddleOcr or NodalOsOcrVisionProviderKind.LocalTesseract,
            provider.Capabilities,
            provider.CostProfile,
            provider.PerformanceProfile,
            provider.PrivacyProfile,
            provider.Policy.RequiresApiKey,
            provider.Policy.RequiresApiKey ? NodalOsOcrVisionApiKeyState.Missing : NodalOsOcrVisionApiKeyState.Disabled,
            provider.Policy.ExternalDataTransfer,
            provider.Policy.AllowedForSensitive,
            provider.Policy.AllowedForCrops,
            provider.Policy.AllowedForFullScreen,
            provider.Policy.Priority,
            provider.Policy.FallbackOrder,
            new NodalOsOcrVisionProviderBudgetSettings(provider.Policy.DailyBudget, provider.Policy.MonthlyBudget, provider.Policy.MaxCostPerPage, provider.Policy.MaxCostPerImage, provider.CostProfile.Currency, ModelOnly: true),
            provider.Policy.MinConfidence,
            provider.Policy.MaxLatencyMs,
            blocked.Select(BrowserCredentialRedactor.Redact).Where(b => !string.IsNullOrWhiteSpace(b)).ToArray(),
            BrowserCredentialRedactor.Redact(evaluationSummary),
            Executable: false,
            GrantsAuthority: false,
            Redacted: true);
    }

    private static IReadOnlyList<NodalOsOcrVisionProviderRoutingRuleView> RoutingRules() =>
    [
        new("rule-no-ocr", "DOM/CDP/UIA sufficient", NodalOsOcrVisionRoutingReason.NoOcrNeeded, [], [], RequiresHumanReview: false, NoAuthority: true),
        new("rule-simple-crop", "simple redacted crop", NodalOsOcrVisionRoutingReason.LocalPaddlePreferred, [new("local-paddleocr-stub")], [new("local-tesseract-stub"), new("human-review")], RequiresHumanReview: false, NoAuthority: true),
        new("rule-cloud-disabled", "complex layout/invoice/form", NodalOsOcrVisionRoutingReason.CloudCandidateDisabled, [], [new("local-paddleocr-stub"), new("human-review")], RequiresHumanReview: true, NoAuthority: true),
        new("rule-sensitive", "sensitive/redaction failed", NodalOsOcrVisionRoutingReason.RedactionFailedBlocked, [], [new("human-review")], RequiresHumanReview: true, NoAuthority: true)
    ];

    private static NodalOsOcrVisionAdminDecision Decision(NodalOsOcrVisionAdminDecisionKind kind, NodalOsOcrVisionProviderId providerId, string reason) =>
        new($"ocr-admin-{Guid.NewGuid():N}", kind, providerId, BrowserCredentialRedactor.Redact(reason), StoresApiKeys: false, CallsRealApi: false, ProviderExecutable: false, GrantsAuthority: false, Redacted: true);

    private static string DisplayName(NodalOsOcrVisionProviderConfiguration provider) =>
        provider.Kind switch
        {
            NodalOsOcrVisionProviderKind.LocalPaddleOcr => "PaddleOCR Local Stub",
            NodalOsOcrVisionProviderKind.LocalTesseract => "Tesseract Local Stub",
            NodalOsOcrVisionProviderKind.CloudAzureDocumentIntelligence => "Azure Document Intelligence Stub",
            NodalOsOcrVisionProviderKind.CloudGoogleDocumentAi => "Google Document AI Stub",
            NodalOsOcrVisionProviderKind.CloudGoogleVision => "Google Vision OCR Stub",
            NodalOsOcrVisionProviderKind.CloudOpenAiVision => "OpenAI Vision OCR/VLM Stub",
            NodalOsOcrVisionProviderKind.CloudMistralOcr => "Mistral OCR Stub",
            NodalOsOcrVisionProviderKind.CloudAmazonTextract => "Amazon Textract Stub",
            _ => provider.ProviderId.Value
        };
}

public abstract class NodalOsSaasOcrProviderStubBase
{
    public abstract NodalOsOcrVisionProviderConfiguration Configuration { get; }

    public NodalOsSaasOcrProviderExecutionProbe ProbeExecution(
        bool optIn = false,
        bool secretVaultConfigured = false,
        bool budgetConfigured = false,
        bool sensitive = false,
        NodalOsGroundingRedactionStatus redactionStatus = NodalOsGroundingRedactionStatus.RedactedSafe)
    {
        var reasons = new List<NodalOsSaasOcrConnectorBlockReason> { NodalOsSaasOcrConnectorBlockReason.DisabledByDefault, NodalOsSaasOcrConnectorBlockReason.CannotRunInProductionCurrentPhase };
        if (!optIn)
            reasons.Add(NodalOsSaasOcrConnectorBlockReason.CannotRunWithoutOptIn);
        if (!secretVaultConfigured)
            reasons.Add(NodalOsSaasOcrConnectorBlockReason.CannotRunWithoutSecretVault);
        if (!budgetConfigured)
            reasons.Add(NodalOsSaasOcrConnectorBlockReason.CannotRunIfBudgetMissing);
        if (sensitive)
            reasons.Add(NodalOsSaasOcrConnectorBlockReason.CannotRunOnSensitiveByDefault);
        if (redactionStatus is NodalOsGroundingRedactionStatus.RedactionFailed or NodalOsGroundingRedactionStatus.BlockedSensitive)
            reasons.Add(NodalOsSaasOcrConnectorBlockReason.CannotRunIfRedactionFailed);
        return new NodalOsSaasOcrProviderExecutionProbe(Configuration.ProviderId, Configuration.Kind, WouldCallHttp: false, StoresSecret: false, RefusesExecution: true, reasons.Distinct().ToArray(), NoAuthority: true, Redacted: true);
    }
}

public sealed class NodalOsAzureDocumentIntelligenceProviderStub : NodalOsSaasOcrProviderStubBase
{
    public override NodalOsOcrVisionProviderConfiguration Configuration => NodalOsOcrVisionProviderRegistryService.CreateProviderStub("azure-document-intelligence-disabled", NodalOsOcrVisionProviderKind.CloudAzureDocumentIntelligence, NodalOsOcrVisionProviderCapability.Forms | NodalOsOcrVisionProviderCapability.Invoices | NodalOsOcrVisionProviderCapability.Receipts | NodalOsOcrVisionProviderCapability.ComplexLayout | NodalOsOcrVisionProviderCapability.Tables | NodalOsOcrVisionProviderCapability.Pdf | NodalOsOcrVisionProviderCapability.StructuredJson, 110, 0.92, 5200, 0.02m, 0.05m, "Azure Document Intelligence stub disabled-by-default");
}

public sealed class NodalOsGoogleDocumentAiProviderStub : NodalOsSaasOcrProviderStubBase
{
    public override NodalOsOcrVisionProviderConfiguration Configuration => NodalOsOcrVisionProviderRegistryService.CreateProviderStub("google-document-ai-disabled", NodalOsOcrVisionProviderKind.CloudGoogleDocumentAi, NodalOsOcrVisionProviderCapability.Forms | NodalOsOcrVisionProviderCapability.ComplexLayout | NodalOsOcrVisionProviderCapability.Pdf | NodalOsOcrVisionProviderCapability.StructuredJson, 120, 0.91, 5400, 0.02m, 0.05m, "Google Document AI stub disabled-by-default");
}

public sealed class NodalOsGoogleVisionOcrProviderStub : NodalOsSaasOcrProviderStubBase
{
    public override NodalOsOcrVisionProviderConfiguration Configuration => NodalOsOcrVisionProviderRegistryService.CreateProviderStub("google-vision-ocr-disabled", NodalOsOcrVisionProviderKind.CloudGoogleVision, NodalOsOcrVisionProviderCapability.PrintedText | NodalOsOcrVisionProviderCapability.Handwriting | NodalOsOcrVisionProviderCapability.LowQualityImage | NodalOsOcrVisionProviderCapability.BoundingBoxes, 130, 0.86, 4200, 0.015m, 0.03m, "Google Vision OCR stub disabled-by-default");
}

public sealed class NodalOsOpenAiVisionOcrProviderStub : NodalOsSaasOcrProviderStubBase
{
    public override NodalOsOcrVisionProviderConfiguration Configuration => NodalOsOcrVisionProviderRegistryService.CreateProviderStub("cloud-openai-vision-disabled", NodalOsOcrVisionProviderKind.CloudOpenAiVision, NodalOsOcrVisionProviderCapability.ScreenshotUi | NodalOsOcrVisionProviderCapability.Handwriting | NodalOsOcrVisionProviderCapability.MixedPrintedHandwriting | NodalOsOcrVisionProviderCapability.LowQualityImage | NodalOsOcrVisionProviderCapability.MarkdownOutput, 200, 0.88, 6000, 0.02m, 0.05m, "OpenAI Vision OCR/VLM stub disabled-by-default");
}

public sealed class NodalOsMistralOcrProviderStub : NodalOsSaasOcrProviderStubBase
{
    public override NodalOsOcrVisionProviderConfiguration Configuration => NodalOsOcrVisionProviderRegistryService.CreateProviderStub("mistral-ocr-disabled", NodalOsOcrVisionProviderKind.CloudMistralOcr, NodalOsOcrVisionProviderCapability.Pdf | NodalOsOcrVisionProviderCapability.MarkdownOutput | NodalOsOcrVisionProviderCapability.ComplexLayout | NodalOsOcrVisionProviderCapability.StructuredJson, 140, 0.88, 4800, 0.018m, 0.04m, "Mistral OCR stub disabled-by-default");
}

public sealed class NodalOsAmazonTextractProviderStub : NodalOsSaasOcrProviderStubBase
{
    public override NodalOsOcrVisionProviderConfiguration Configuration => NodalOsOcrVisionProviderRegistryService.CreateProviderStub("amazon-textract-disabled", NodalOsOcrVisionProviderKind.CloudAmazonTextract, NodalOsOcrVisionProviderCapability.Forms | NodalOsOcrVisionProviderCapability.Tables | NodalOsOcrVisionProviderCapability.IdentityDocuments | NodalOsOcrVisionProviderCapability.Invoices | NodalOsOcrVisionProviderCapability.Receipts, 150, 0.9, 5600, 0.02m, 0.05m, "Amazon Textract stub disabled-by-default");
}

public sealed class NodalOsOcrVisionEvaluationHarness
{
    private readonly NodalOsOcrVisionRouter router = new();
    private readonly NodalOsGroundingSnapshotBuilder groundingBuilder = new();
    private readonly NodalOsImageCropRedactor redactor = new();

    public IReadOnlyList<NodalOsOcrVisionEvaluationFixture> DefaultFixtures() =>
    [
        Fixture("simple-ui-crop", "simple printed UI crop", NodalOsOcrVisionCaseClassification.SimpleUiCrop, NodalOsOcrVisionComplexity.Low, NodalOsOcrVisionQuality.Good, NodalOsOcrVisionSensitivity.Low, false, false, true, false, 1m, NodalOsGroundingRedactionStatus.RedactedSafe, NodalOsOcrVisionRoutingStatus.ProviderSelected, NodalOsOcrVisionRoutingReason.LocalPaddlePreferred, "local-paddleocr-stub", false),
        Fixture("simple-document-text", "simple document text", NodalOsOcrVisionCaseClassification.SimpleText, NodalOsOcrVisionComplexity.Low, NodalOsOcrVisionQuality.Good, NodalOsOcrVisionSensitivity.Low, false, false, true, false, 1m, NodalOsGroundingRedactionStatus.RedactedSafe, NodalOsOcrVisionRoutingStatus.ProviderSelected, NodalOsOcrVisionRoutingReason.LocalPaddlePreferred, "local-paddleocr-stub", false),
        Fixture("low-quality-crop", "low-quality crop", NodalOsOcrVisionCaseClassification.LowQualityImage, NodalOsOcrVisionComplexity.High, NodalOsOcrVisionQuality.Poor, NodalOsOcrVisionSensitivity.Low, false, false, true, false, 1m, NodalOsGroundingRedactionStatus.RedactedSafe, NodalOsOcrVisionRoutingStatus.AskHuman, NodalOsOcrVisionRoutingReason.VlmCandidateDisabled, null, true),
        Fixture("blurred-crop", "blurred crop", NodalOsOcrVisionCaseClassification.Blurred, NodalOsOcrVisionComplexity.High, NodalOsOcrVisionQuality.Poor, NodalOsOcrVisionSensitivity.Low, false, false, true, false, 1m, NodalOsGroundingRedactionStatus.RedactedSafe, NodalOsOcrVisionRoutingStatus.AskHuman, NodalOsOcrVisionRoutingReason.VlmCandidateDisabled, null, true),
        Fixture("skewed-crop", "skewed crop", NodalOsOcrVisionCaseClassification.Skewed, NodalOsOcrVisionComplexity.Medium, NodalOsOcrVisionQuality.Medium, NodalOsOcrVisionSensitivity.Low, false, false, true, false, 1m, NodalOsGroundingRedactionStatus.RedactedSafe, NodalOsOcrVisionRoutingStatus.AskHuman, NodalOsOcrVisionRoutingReason.AskHuman, null, true),
        Fixture("table-layout", "table layout", NodalOsOcrVisionCaseClassification.Table, NodalOsOcrVisionComplexity.High, NodalOsOcrVisionQuality.Good, NodalOsOcrVisionSensitivity.Low, false, false, true, false, 1m, NodalOsGroundingRedactionStatus.RedactedSafe, NodalOsOcrVisionRoutingStatus.CloudDisabled, NodalOsOcrVisionRoutingReason.CloudCandidateDisabled, null, true),
        Fixture("invoice-layout", "invoice layout", NodalOsOcrVisionCaseClassification.Invoice, NodalOsOcrVisionComplexity.High, NodalOsOcrVisionQuality.Good, NodalOsOcrVisionSensitivity.Low, false, false, true, false, 1m, NodalOsGroundingRedactionStatus.RedactedSafe, NodalOsOcrVisionRoutingStatus.CloudDisabled, NodalOsOcrVisionRoutingReason.CloudCandidateDisabled, null, true),
        Fixture("receipt-layout", "receipt layout", NodalOsOcrVisionCaseClassification.Receipt, NodalOsOcrVisionComplexity.High, NodalOsOcrVisionQuality.Good, NodalOsOcrVisionSensitivity.Low, false, false, true, false, 1m, NodalOsGroundingRedactionStatus.RedactedSafe, NodalOsOcrVisionRoutingStatus.CloudDisabled, NodalOsOcrVisionRoutingReason.CloudCandidateDisabled, null, true),
        Fixture("handwriting-synthetic", "handwriting sample synthetic", NodalOsOcrVisionCaseClassification.Handwriting, NodalOsOcrVisionComplexity.VeryHigh, NodalOsOcrVisionQuality.Medium, NodalOsOcrVisionSensitivity.Low, false, false, true, false, 1m, NodalOsGroundingRedactionStatus.RedactedSafe, NodalOsOcrVisionRoutingStatus.AskHuman, NodalOsOcrVisionRoutingReason.VlmCandidateDisabled, null, true),
        Fixture("mixed-handwriting-synthetic", "mixed printed/handwriting synthetic", NodalOsOcrVisionCaseClassification.MixedPrintedHandwriting, NodalOsOcrVisionComplexity.VeryHigh, NodalOsOcrVisionQuality.Medium, NodalOsOcrVisionSensitivity.Low, false, false, true, false, 1m, NodalOsGroundingRedactionStatus.RedactedSafe, NodalOsOcrVisionRoutingStatus.AskHuman, NodalOsOcrVisionRoutingReason.VlmCandidateDisabled, null, true),
        Fixture("screenshot-ui-ambiguous", "screenshot UI ambiguous", NodalOsOcrVisionCaseClassification.Unknown, NodalOsOcrVisionComplexity.Unknown, NodalOsOcrVisionQuality.Medium, NodalOsOcrVisionSensitivity.Low, false, false, true, false, 1m, NodalOsGroundingRedactionStatus.RedactedSafe, NodalOsOcrVisionRoutingStatus.AskHuman, NodalOsOcrVisionRoutingReason.AskHuman, null, true),
        Fixture("sensitive-redaction-failed", "sensitive crop redaction failed", NodalOsOcrVisionCaseClassification.SensitiveSurface, NodalOsOcrVisionComplexity.High, NodalOsOcrVisionQuality.RedactionFailed, NodalOsOcrVisionSensitivity.SensitiveSurface, false, false, true, false, 1m, NodalOsGroundingRedactionStatus.RedactionFailed, NodalOsOcrVisionRoutingStatus.Blocked, NodalOsOcrVisionRoutingReason.RedactionFailedBlocked, null, true),
        Fixture("full-screen-blocked", "full-screen request blocked", NodalOsOcrVisionCaseClassification.ScreenshotUi, NodalOsOcrVisionComplexity.Medium, NodalOsOcrVisionQuality.Good, NodalOsOcrVisionSensitivity.Low, false, false, true, true, 1m, NodalOsGroundingRedactionStatus.RedactedSafe, NodalOsOcrVisionRoutingStatus.Blocked, NodalOsOcrVisionRoutingReason.NoProviderAllowed, null, true),
        Fixture("cloud-candidate-disabled", "cloud candidate disabled", NodalOsOcrVisionCaseClassification.Form, NodalOsOcrVisionComplexity.High, NodalOsOcrVisionQuality.Good, NodalOsOcrVisionSensitivity.Low, false, false, true, false, 1m, NodalOsGroundingRedactionStatus.RedactedSafe, NodalOsOcrVisionRoutingStatus.CloudDisabled, NodalOsOcrVisionRoutingReason.CloudCandidateDisabled, null, true),
        Fixture("budget-exceeded", "budget exceeded", NodalOsOcrVisionCaseClassification.Invoice, NodalOsOcrVisionComplexity.High, NodalOsOcrVisionQuality.Good, NodalOsOcrVisionSensitivity.Low, false, false, true, false, 0m, NodalOsGroundingRedactionStatus.RedactedSafe, NodalOsOcrVisionRoutingStatus.BlockedByBudget, NodalOsOcrVisionRoutingReason.BudgetExceeded, null, true)
    ];

    public NodalOsOcrVisionBenchmarkReport Run(NodalOsOcrVisionProviderRegistry registry)
    {
        var cases = DefaultFixtures().Select(fixture => new NodalOsOcrVisionEvaluationCase(fixture.FixtureId, fixture, ToRequest(fixture))).ToArray();
        var results = cases.Select(@case => Evaluate(@case, registry)).ToArray();
        return new NodalOsOcrVisionBenchmarkReport(
            "ocr-vision-evaluation-m177",
            DateTimeOffset.UtcNow,
            results,
            results.Length,
            results.Count(result => result.Passed),
            CallsRealOcr: false,
            CallsRealSaas: false,
            NoAuthority: true,
            Redacted: true);
    }

    public void WriteReport(NodalOsOcrVisionBenchmarkReport report, string artifactJsonPath, string markdownPath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(artifactJsonPath)!);
        Directory.CreateDirectory(Path.GetDirectoryName(markdownPath)!);
        File.WriteAllText(artifactJsonPath, System.Text.Json.JsonSerializer.Serialize(report, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
        File.WriteAllText(markdownPath, Markdown(report));
    }

    private NodalOsOcrVisionEvaluationResult Evaluate(NodalOsOcrVisionEvaluationCase evaluationCase, NodalOsOcrVisionProviderRegistry registry)
    {
        var decision = router.Route(evaluationCase.RoutingRequest, registry);
        var expected = evaluationCase.Fixture.ExpectedOutput;
        var providerMatch = expected.ExpectedProviderId is null || decision.SelectedProviderId?.Value == expected.ExpectedProviderId;
        var metrics = new[]
        {
            Metric("routing correctness", (decision.Status == expected.ExpectedStatus && decision.Reason == expected.ExpectedReason && providerMatch).ToString(), decision.Status == expected.ExpectedStatus && decision.Reason == expected.ExpectedReason && providerMatch),
            Metric("no real OCR", "false", true),
            Metric("no real SaaS", decision.CallsRealSaas.ToString(), !decision.CallsRealSaas),
            Metric("no authority", decision.NoAuthority.ToString(), decision.NoAuthority),
            Metric("human escalation", decision.RequiresHumanApproval.ToString(), decision.RequiresHumanApproval == expected.RequiresHumanEscalation || expected.RequiresHumanEscalation == false)
        };
        return new NodalOsOcrVisionEvaluationResult(
            $"ocr-eval-{evaluationCase.CaseId}",
            evaluationCase.Fixture,
            decision,
            Scores(decision),
            metrics,
            metrics.All(metric => metric.Passed),
            CallsRealOcr: false,
            CallsRealSaas: false,
            NoAuthority: true,
            Redacted: true);
    }

    private NodalOsOcrVisionRoutingRequest ToRequest(NodalOsOcrVisionEvaluationFixture fixture)
    {
        var snapshot = groundingBuilder.Create($"snapshot-{fixture.FixtureId}", redactionStatus: fixture.RedactionStatus);
        var request = new NodalOsOcrVisionRoutingRequest(
            fixture.FixtureId,
            fixture.DomCdpUiaSufficient,
            fixture.ScreenshotHashDiffOnly,
            snapshot,
            "crop:redacted",
            fixture.CropRedacted,
            fixture.FullScreen,
            fixture.CaseClassification,
            DocumentType(fixture.CaseClassification),
            fixture.Complexity,
            fixture.Quality,
            fixture.Sensitivity,
            fixture.Budget,
            RequiredConfidence: 0.75,
            AllowsCloud: false,
            Redacted: true);
        return request with { RedactionResult = RedactionFor(fixture, snapshot) };
    }

    private NodalOsImageCropRedactionResult RedactionFor(NodalOsOcrVisionEvaluationFixture fixture, NodalOsBrowserGroundingSnapshot snapshot)
    {
        if (fixture.RedactionStatus == NodalOsGroundingRedactionStatus.RedactionFailed)
            return redactor.Redact(RedactionRequest(fixture, snapshot, "uncertain unknown_sensitive"));
        if (fixture.Sensitivity >= NodalOsOcrVisionSensitivity.SensitiveSurface)
            return redactor.Redact(RedactionRequest(fixture, snapshot, "sensitive"));
        return redactor.Redact(RedactionRequest(fixture, snapshot, fixture.CropRedacted ? "clean local fixture crop" : "uncertain"));
    }

    private NodalOsImageCropRedactionRequest RedactionRequest(NodalOsOcrVisionEvaluationFixture fixture, NodalOsBrowserGroundingSnapshot snapshot, string marker) =>
        new(
            $"redaction-{fixture.FixtureId}",
            snapshot.SnapshotId,
            fixture.CropRedacted ? "crop:redacted" : null,
            System.Text.Encoding.UTF8.GetBytes(marker),
            new NodalOsOcrBoundingBox(0, 0, fixture.FullScreen ? 1920 : 320, fixture.FullScreen ? 1080 : 160),
            "m177-synthetic-fixture",
            fixture.Sensitivity,
            NodalOsOcrPurpose.EvidenceDebug,
            AllowPersistence: false,
            AllowFullScreen: fixture.FullScreen,
            redactor.DefaultPolicy());

    private static NodalOsOcrVisionEvaluationFixture Fixture(
        string id,
        string description,
        NodalOsOcrVisionCaseClassification classification,
        NodalOsOcrVisionComplexity complexity,
        NodalOsOcrVisionQuality quality,
        NodalOsOcrVisionSensitivity sensitivity,
        bool domSufficient,
        bool hashOnly,
        bool cropRedacted,
        bool fullScreen,
        decimal budget,
        NodalOsGroundingRedactionStatus redaction,
        NodalOsOcrVisionRoutingStatus expectedStatus,
        NodalOsOcrVisionRoutingReason expectedReason,
        string? expectedProvider,
        bool human) =>
        new(id, description, classification, complexity, quality, sensitivity, domSufficient, hashOnly, cropRedacted, fullScreen, budget, redaction, new NodalOsOcrVisionExpectedOutput(expectedStatus, expectedReason, expectedProvider, human, NoAuthority: true), Synthetic: true, Redacted: true);

    private static IReadOnlyList<NodalOsOcrVisionProviderScore> Scores(NodalOsOcrVisionRoutingDecision decision) =>
        decision.FallbackPlan.Providers.Select(provider => new NodalOsOcrVisionProviderScore(provider, decision.ExpectedConfidenceBand, "model-only", decision.EstimatedCost, decision.Risk, decision.SelectedProviderId == provider, Fallback: true, NoAuthority: true)).ToArray();

    private static NodalOsOcrVisionEvaluationMetric Metric(string name, string value, bool passed) => new(name, value, passed);

    private static NodalOsOcrVisionDocumentType DocumentType(NodalOsOcrVisionCaseClassification classification) =>
        classification switch
        {
            NodalOsOcrVisionCaseClassification.Invoice => NodalOsOcrVisionDocumentType.Invoice,
            NodalOsOcrVisionCaseClassification.Receipt => NodalOsOcrVisionDocumentType.Receipt,
            NodalOsOcrVisionCaseClassification.Table => NodalOsOcrVisionDocumentType.Table,
            NodalOsOcrVisionCaseClassification.Form => NodalOsOcrVisionDocumentType.Form,
            NodalOsOcrVisionCaseClassification.Handwriting => NodalOsOcrVisionDocumentType.Handwriting,
            NodalOsOcrVisionCaseClassification.MixedPrintedHandwriting => NodalOsOcrVisionDocumentType.MixedPrintedHandwriting,
            _ => NodalOsOcrVisionDocumentType.UiCrop
        };

    private static string Markdown(NodalOsOcrVisionBenchmarkReport report)
    {
        var lines = new List<string>
        {
            "# OCR/Vision Evaluation M177",
            "",
            $"Total cases: {report.TotalCases}",
            $"Passed cases: {report.PassedCases}",
            "No real OCR executed: true",
            "No SaaS OCR executed: true",
            "No-authority: true",
            "",
            "| Fixture | Status | Reason | Provider | Passed |",
            "| --- | --- | --- | --- | --- |"
        };
        lines.AddRange(report.Results.Select(result => $"| {result.Fixture.FixtureId} | {result.Decision.Status} | {result.Decision.Reason} | {result.Decision.SelectedProviderId?.Value ?? "-"} | {result.Passed} |"));
        return string.Join(Environment.NewLine, lines);
    }
}

public sealed class NodalOsImageCropRedactor
{
    public NodalOsImageRedactionPolicy DefaultPolicy() =>
        new(
            AllowPersistence: false,
            AllowFullScreen: false,
            BlockSensitiveByDefault: true,
            FailClosedOnUncertainty: true,
            PersistRawImage: false,
            NoAuthority: true);

    public NodalOsImageCropRedactionResult Redact(NodalOsImageCropRedactionRequest request)
    {
        if (request.AllowFullScreen || request.Bounds.Width > 1600 || request.Bounds.Height > 1200)
            return Result(request, NodalOsImageRedactionDecision.RedactionFailed, [Finding(NodalOsImageRedactionFindingKind.RedactionEngineUncertain, "full-screen or oversized crop", 0.99, blocks: true)], 0.2);

        if (request.AllowPersistence || request.Policy.PersistRawImage || request.Policy.AllowPersistence)
            return Result(request, NodalOsImageRedactionDecision.RedactionFailed, [Finding(NodalOsImageRedactionFindingKind.RedactionEngineUncertain, "raw persistence requested", 0.99, blocks: true)], 0.2);

        var marker = DecodeFixture(request.SyntheticImageBytes);
        if (string.IsNullOrWhiteSpace(marker))
            return Result(request, NodalOsImageRedactionDecision.RedactionFailed, [Finding(NodalOsImageRedactionFindingKind.RedactionEngineUncertain, "empty crop bytes", 0.8, blocks: true)], 0.1);

        var findings = Findings(marker).ToArray();
        if (findings.Any(finding => finding.Kind is NodalOsImageRedactionFindingKind.RedactionEngineUncertain or NodalOsImageRedactionFindingKind.UnknownSensitivePattern or NodalOsImageRedactionFindingKind.LowConfidence))
            return Result(request, NodalOsImageRedactionDecision.RedactionFailed, findings, 0.35);

        if (request.Sensitivity >= NodalOsOcrVisionSensitivity.SensitiveSurface && request.Policy.BlockSensitiveByDefault)
            return Result(request, NodalOsImageRedactionDecision.BlockedSensitive, findings.Length == 0 ? [Finding(NodalOsImageRedactionFindingKind.SensitiveKeyword, "sensitive surface", 0.95, blocks: true)] : findings, 0.85);

        if (findings.Any(finding => finding.BlocksOcr))
            return Result(request, NodalOsImageRedactionDecision.BlockedSensitive, findings, 0.9);

        if (findings.Length > 0)
            return Result(request, NodalOsImageRedactionDecision.Redacted, findings, 0.92);

        return Result(request, NodalOsImageRedactionDecision.CleanNoRedactionRequired, [], 0.98);
    }

    public static bool IsValidForOcr(NodalOsImageCropRedactionResult? result) =>
        result is
        {
            Decision: NodalOsImageRedactionDecision.Redacted or NodalOsImageRedactionDecision.CleanNoRedactionRequired,
            CropRedacted: true,
            SafeForOcr: true,
            OriginalRawPersisted: false,
            NoAuthority: true
        };

    public static bool IsValidForPersistence(NodalOsImageCropRedactionResult? result) =>
        IsValidForOcr(result) && result!.SafeForPersistence && !result.Evidence.OriginalRawPersisted;

    private static IReadOnlyList<NodalOsImageRedactionFinding> Findings(string marker)
    {
        var normalized = marker.ToLowerInvariant();
        var findings = new List<NodalOsImageRedactionFinding>();
        AddIf(findings, normalized.Contains("password") || normalized.Contains("passwd") || normalized.Contains("pwd="), NodalOsImageRedactionFindingKind.PasswordField, "password-like field", blocks: false);
        AddIf(findings, normalized.Contains("credential") || normalized.Contains("login_secret"), NodalOsImageRedactionFindingKind.CredentialLikeText, "credential-like text", blocks: false);
        AddIf(findings, normalized.Contains("token=") || normalized.Contains("bearer ") || normalized.Contains("access_token"), NodalOsImageRedactionFindingKind.TokenLikeText, "token-like text", blocks: true);
        AddIf(findings, marker.Contains("eyJ", StringComparison.Ordinal) && marker.Count(ch => ch == '.') >= 2, NodalOsImageRedactionFindingKind.JwtLikeText, "jwt-like text", blocks: true);
        AddIf(findings, normalized.Contains("cookie") || normalized.Contains("sessionid"), NodalOsImageRedactionFindingKind.CookieLikeText, "cookie-like text", blocks: true);
        AddIf(findings, normalized.Contains("api_key") || normalized.Contains("apikey") || normalized.Contains("sk-"), NodalOsImageRedactionFindingKind.ApiKeyLikeText, "api-key-like text", blocks: true);
        AddIf(findings, marker.Contains('@') && marker.Contains('.', StringComparison.Ordinal), NodalOsImageRedactionFindingKind.EmailLikeText, "email-like text", blocks: false);
        AddIf(findings, normalized.Contains("phone:") || normalized.Contains("+1-") || normalized.Contains("+54"), NodalOsImageRedactionFindingKind.PhoneLikeText, "phone-like text", blocks: false);
        AddIf(findings, normalized.Contains("4111 1111") || normalized.Contains("credit_card"), NodalOsImageRedactionFindingKind.CreditCardLikeText, "credit-card-like text", blocks: true);
        AddIf(findings, normalized.Contains("document_id") || normalized.Contains("dni:") || normalized.Contains("ssn:"), NodalOsImageRedactionFindingKind.DocumentIdLikeText, "document-id-like text", blocks: false);
        AddIf(findings, normalized.Contains("secret") || normalized.Contains("sensitive"), NodalOsImageRedactionFindingKind.SensitiveKeyword, "sensitive keyword", blocks: true);
        AddIf(findings, normalized.Contains("unknown_sensitive"), NodalOsImageRedactionFindingKind.UnknownSensitivePattern, "unknown sensitive pattern", blocks: true);
        AddIf(findings, normalized.Contains("low_confidence"), NodalOsImageRedactionFindingKind.LowConfidence, "low confidence redaction", blocks: true);
        AddIf(findings, normalized.Contains("uncertain") || normalized.Contains("blurred_secret"), NodalOsImageRedactionFindingKind.RedactionEngineUncertain, "redaction uncertain", blocks: true);
        return findings;
    }

    private static void AddIf(List<NodalOsImageRedactionFinding> findings, bool condition, NodalOsImageRedactionFindingKind kind, string preview, bool blocks)
    {
        if (condition)
            findings.Add(Finding(kind, preview, 0.93, blocks));
    }

    private static NodalOsImageRedactionFinding Finding(NodalOsImageRedactionFindingKind kind, string preview, double confidence, bool blocks) =>
        new(kind, BrowserCredentialRedactor.Redact(preview), null, confidence, blocks);

    private static NodalOsImageCropRedactionResult Result(
        NodalOsImageCropRedactionRequest request,
        NodalOsImageRedactionDecision decision,
        IReadOnlyList<NodalOsImageRedactionFinding> findings,
        double confidence)
    {
        var safe = decision is NodalOsImageRedactionDecision.Redacted or NodalOsImageRedactionDecision.CleanNoRedactionRequired;
        var hash = ModelOnlyHash(request.SyntheticImageBytes);
        return new NodalOsImageCropRedactionResult(
            $"crop-redaction-{Guid.NewGuid():N}",
            decision,
            CropRedacted: safe,
            SafeForOcr: safe,
            SafeForPersistence: safe && !request.AllowPersistence && !request.Policy.PersistRawImage,
            findings,
            RedactedBytesRef: safe ? $"redacted-bytes:{hash}" : string.Empty,
            OriginalRawPersisted: false,
            new NodalOsImageRedactionEvidence(
                $"redaction-evidence-{Guid.NewGuid():N}",
                [new NodalOsGroundingEvidenceRef($"redaction:{request.RequestId}:{decision}", "redacted crop evidence; raw bytes not persisted", Redacted: true)],
                BrowserCredentialRedactor.Redact($"decision={decision}; findings={findings.Count}; raw persisted=false"),
                hash,
                OriginalRawPersisted: false,
                Redacted: true),
            new NodalOsOcrConfidence(confidence),
            NoAuthority: true);
    }

    private static string DecodeFixture(byte[] bytes) =>
        bytes.Length == 0 ? string.Empty : System.Text.Encoding.UTF8.GetString(bytes);

    private static string ModelOnlyHash(byte[] bytes)
    {
        var hash = System.Security.Cryptography.SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}

public interface NodalOsLocalOcrProvider
{
    NodalOsOcrVisionProviderId ProviderId { get; }
    NodalOsOcrEngineHint Engine { get; }
    NodalOsLocalOcrResult Recognize(NodalOsLocalOcrRequest request);
}

public abstract class NodalOsLocalOcrProviderStubBase : NodalOsLocalOcrProvider
{
    public abstract NodalOsOcrVisionProviderId ProviderId { get; }
    public abstract NodalOsOcrEngineHint Engine { get; }
    protected abstract double StubConfidence { get; }

    public NodalOsLocalOcrResult Recognize(NodalOsLocalOcrRequest request)
    {
        var blocked = BlockedStatus(request);
        if (blocked is not null)
            return Result(request, blocked.Value, [], StubConfidence, ["request blocked by OCR policy"]);

        var confidence = StubConfidence;
        var status = confidence < request.MinConfidence
            ? NodalOsLocalOcrStatus.LowConfidenceNeedsHuman
            : NodalOsLocalOcrStatus.CompletedStub;
        string[] warnings = status == NodalOsLocalOcrStatus.LowConfidenceNeedsHuman
            ? ["low confidence requires human review"]
            : ["model-only OCR stub; no real OCR executed"];
        return Result(
            request,
            status,
            [new NodalOsOcrTextBlock("block-1", "redacted local fixture text", new NodalOsOcrBoundingBox(request.Region.X, request.Region.Y, request.Region.Width, request.Region.Height), new NodalOsOcrConfidence(confidence), request.LanguageHints.FirstOrDefault(NodalOsOcrLanguage.Unknown), Redacted: true)],
            confidence,
            warnings);
    }

    private static NodalOsLocalOcrStatus? BlockedStatus(NodalOsLocalOcrRequest request)
    {
        if (request.RedactionStatus is NodalOsGroundingRedactionStatus.RedactionFailed or NodalOsGroundingRedactionStatus.BlockedSensitive)
            return NodalOsLocalOcrStatus.BlockedByRedaction;
        if (!NodalOsImageCropRedactor.IsValidForOcr(request.RedactionResult))
            return NodalOsLocalOcrStatus.BlockedByRedaction;
        if (request.FullScreen)
            return NodalOsLocalOcrStatus.BlockedFullScreen;
        if (!request.CropRedacted || string.IsNullOrWhiteSpace(request.CropRef))
            return NodalOsLocalOcrStatus.BlockedUnredactedCrop;
        if (string.IsNullOrWhiteSpace(request.ScreenshotRef))
            return NodalOsLocalOcrStatus.BlockedByRedaction;
        return null;
    }

    private NodalOsLocalOcrResult Result(
        NodalOsLocalOcrRequest request,
        NodalOsLocalOcrStatus status,
        IReadOnlyList<NodalOsOcrTextBlock> blocks,
        double confidence,
        IReadOnlyList<string> warnings)
    {
        var requiresHuman = status is not NodalOsLocalOcrStatus.CompletedStub || confidence < request.MinConfidence || request.Sensitivity >= NodalOsOcrVisionSensitivity.High;
        return new NodalOsLocalOcrResult(
            $"ocr-result-{Guid.NewGuid():N}",
            ProviderId,
            Engine,
            status,
            blocks,
            new NodalOsOcrConfidence(confidence),
            request.LanguageHints.FirstOrDefault(NodalOsOcrLanguage.Unknown),
            warnings.Select(BrowserCredentialRedactor.Redact).ToArray(),
            [new NodalOsGroundingEvidenceRef($"ocr:{request.RequestId}:redacted", "local OCR stub evidence", Redacted: true)],
            new NodalOsOcrRedactionSummary(request.RedactionStatus, ScreenshotSafe: !string.IsNullOrWhiteSpace(request.ScreenshotRef), request.CropRedacted, ContainsSensitive: request.Sensitivity >= NodalOsOcrVisionSensitivity.SensitiveSurface, "OCR result redacted; no raw screenshot/body/credentials"),
            NodalOsOcrAuthorityFlag.NoAuthority,
            requiresHuman,
            CanApproveAction: false,
            CanClick: false,
            CanSubmit: false,
            CallsExternalApi: false,
            Redacted: true);
    }
}

public sealed class NodalOsPaddleOcrLocalProviderStub : NodalOsLocalOcrProviderStubBase
{
    public override NodalOsOcrVisionProviderId ProviderId => new("local-paddleocr-stub");
    public override NodalOsOcrEngineHint Engine => NodalOsOcrEngineHint.PaddleOcr;
    protected override double StubConfidence => 0.88;
}

public sealed class NodalOsTesseractLocalProviderStub : NodalOsLocalOcrProviderStubBase
{
    public override NodalOsOcrVisionProviderId ProviderId => new("local-tesseract-stub");
    public override NodalOsOcrEngineHint Engine => NodalOsOcrEngineHint.Tesseract;
    protected override double StubConfidence => 0.74;
}

public sealed class NodalOsOcrVisionRouter
{
    private readonly NodalOsOcrVisionProviderRegistryService registryService = new();

    public NodalOsOcrVisionRoutingDecision Route(
        NodalOsOcrVisionRoutingRequest request,
        NodalOsOcrVisionProviderRegistry registry)
    {
        if (request.DomCdpUiaSufficient || request.ScreenshotHashDiffOnly)
            return Decision(request, NodalOsOcrVisionRoutingStatus.NoOcrNeeded, null, NodalOsOcrVisionRoutingReason.NoOcrNeeded, "DOM/CDP/UIA or screenshot hash/diff is sufficient; OCR not needed.", 0m);

        if (request.Quality is NodalOsOcrVisionQuality.RedactionFailed ||
            request.GroundingSnapshot?.RedactionStatus is NodalOsGroundingRedactionStatus.RedactionFailed or NodalOsGroundingRedactionStatus.BlockedSensitive)
            return Decision(request, NodalOsOcrVisionRoutingStatus.Blocked, null, NodalOsOcrVisionRoutingReason.RedactionFailedBlocked, "redaction failed; OCR blocked", 0m, human: true, risk: NodalOsGroundingRisk.Prohibited);

        if (request.FullScreen)
            return Decision(request, NodalOsOcrVisionRoutingStatus.Blocked, null, NodalOsOcrVisionRoutingReason.NoProviderAllowed, "full-screen OCR is blocked by default; use redacted crops", 0m, human: true, risk: NodalOsGroundingRisk.High);

        if (request.RedactionResult is null)
            return Decision(request, NodalOsOcrVisionRoutingStatus.Blocked, null, NodalOsOcrVisionRoutingReason.RedactionFailedBlocked, "verified crop redaction result is required before OCR routing", 0m, human: true, risk: NodalOsGroundingRisk.High);

        if (request.RedactionResult.Decision == NodalOsImageRedactionDecision.RedactionFailed)
            return Decision(request, NodalOsOcrVisionRoutingStatus.Blocked, null, NodalOsOcrVisionRoutingReason.RedactionFailedBlocked, "crop redactor failed; OCR blocked", 0m, human: true, risk: NodalOsGroundingRisk.Prohibited);

        if (request.RedactionResult.Decision == NodalOsImageRedactionDecision.BlockedSensitive)
            return Decision(request, NodalOsOcrVisionRoutingStatus.AskHuman, null, NodalOsOcrVisionRoutingReason.SensitiveCloudBlocked, "crop redactor blocked sensitive content; human review required", 0m, human: true, risk: NodalOsGroundingRisk.High);

        if (!NodalOsImageCropRedactor.IsValidForOcr(request.RedactionResult))
            return Decision(request, NodalOsOcrVisionRoutingStatus.Blocked, null, NodalOsOcrVisionRoutingReason.RedactionFailedBlocked, "verified crop redaction result is not safe for OCR", 0m, human: true, risk: NodalOsGroundingRisk.High);

        if (request.RedactionResult.Confidence.Value < request.RequiredConfidence)
            return Decision(request, NodalOsOcrVisionRoutingStatus.AskHuman, null, NodalOsOcrVisionRoutingReason.LowConfidenceNeedsHuman, "redaction confidence below OCR threshold; human review required", 0m, human: true, risk: NodalOsGroundingRisk.Medium);

        if (!request.CropRedacted || string.IsNullOrWhiteSpace(request.CropRef))
            return Decision(request, NodalOsOcrVisionRoutingStatus.Blocked, null, NodalOsOcrVisionRoutingReason.NoProviderAllowed, "redacted crop required before OCR", 0m, human: true, risk: NodalOsGroundingRisk.High);

        if (request.MaxEstimatedCost < EstimatedCost(request))
            return Decision(request, NodalOsOcrVisionRoutingStatus.BlockedByBudget, null, NodalOsOcrVisionRoutingReason.BudgetExceeded, "estimated OCR cost exceeds request budget", EstimatedCost(request), human: true, risk: NodalOsGroundingRisk.Medium);

        if (request.Sensitivity >= NodalOsOcrVisionSensitivity.SensitiveSurface)
            return SensitiveDecision(request, registry);

        return request.CaseClassification switch
        {
            NodalOsOcrVisionCaseClassification.SimpleText or NodalOsOcrVisionCaseClassification.SimpleUiCrop or NodalOsOcrVisionCaseClassification.ScreenshotUi
                => SelectLocal(request, registry),
            NodalOsOcrVisionCaseClassification.ComplexLayout or NodalOsOcrVisionCaseClassification.Table or NodalOsOcrVisionCaseClassification.Form or NodalOsOcrVisionCaseClassification.Invoice or NodalOsOcrVisionCaseClassification.Receipt
                => Decision(request, NodalOsOcrVisionRoutingStatus.CloudDisabled, null, NodalOsOcrVisionRoutingReason.CloudCandidateDisabled, "cloud document OCR candidate remains disabled-by-default; use local fallback or human review", EstimatedCost(request), human: true, fallback: LocalFallback(registry)),
            NodalOsOcrVisionCaseClassification.Handwriting or NodalOsOcrVisionCaseClassification.MixedPrintedHandwriting or NodalOsOcrVisionCaseClassification.LowQualityImage or NodalOsOcrVisionCaseClassification.Blurred
                => Decision(request, NodalOsOcrVisionRoutingStatus.AskHuman, null, NodalOsOcrVisionRoutingReason.VlmCandidateDisabled, "VLM candidate disabled-by-default; ask human or future configured fallback", EstimatedCost(request), human: true, fallback: HumanFallback()),
            _ => Decision(request, NodalOsOcrVisionRoutingStatus.AskHuman, null, NodalOsOcrVisionRoutingReason.AskHuman, "unknown OCR case requires human review", EstimatedCost(request), human: true, fallback: HumanFallback())
        };
    }

    private NodalOsOcrVisionRoutingDecision SelectLocal(NodalOsOcrVisionRoutingRequest request, NodalOsOcrVisionProviderRegistry registry)
    {
        var providers = registryService.FilterProviders(registry, NodalOsOcrVisionProviderCapability.SimpleUiCrop, request.Sensitivity, request.FullScreen, request.CropRedacted);
        var paddle = providers.FirstOrDefault(provider => provider.Kind == NodalOsOcrVisionProviderKind.LocalPaddleOcr);
        if (paddle is not null)
            return Decision(request, NodalOsOcrVisionRoutingStatus.ProviderSelected, paddle.ProviderId, NodalOsOcrVisionRoutingReason.LocalPaddlePreferred, "simple redacted crop routes to local PaddleOCR stub", 0m, fallback: LocalFallback(registry));

        var tesseract = providers.FirstOrDefault(provider => provider.Kind == NodalOsOcrVisionProviderKind.LocalTesseract);
        if (tesseract is not null)
            return Decision(request, NodalOsOcrVisionRoutingStatus.LocalFallbackSelected, tesseract.ProviderId, NodalOsOcrVisionRoutingReason.TesseractFallback, "PaddleOCR unavailable; Tesseract stub fallback selected", 0m, fallback: LocalFallback(registry));

        return Decision(request, NodalOsOcrVisionRoutingStatus.AskHuman, null, NodalOsOcrVisionRoutingReason.NoProviderAllowed, "no local OCR provider enabled for redacted crop", 0m, human: true, fallback: HumanFallback());
    }

    private NodalOsOcrVisionRoutingDecision SensitiveDecision(NodalOsOcrVisionRoutingRequest request, NodalOsOcrVisionProviderRegistry registry)
    {
        var local = registryService.FilterProviders(registry, NodalOsOcrVisionProviderCapability.SimpleUiCrop, request.Sensitivity, request.FullScreen, request.CropRedacted)
            .FirstOrDefault(provider => provider.Policy.ExternalDataTransfer == false);
        if (local is not null)
            return Decision(request, NodalOsOcrVisionRoutingStatus.ProviderSelected, local.ProviderId, NodalOsOcrVisionRoutingReason.SensitiveCloudBlocked, "sensitive surface blocks cloud; local-only stub selected", 0m, human: true, fallback: HumanFallback(), risk: NodalOsGroundingRisk.High);

        return Decision(request, NodalOsOcrVisionRoutingStatus.AskHuman, null, NodalOsOcrVisionRoutingReason.SensitiveCloudBlocked, "sensitive surface blocks cloud and no local provider is allowed", 0m, human: true, fallback: HumanFallback(), risk: NodalOsGroundingRisk.High);
    }

    private static decimal EstimatedCost(NodalOsOcrVisionRoutingRequest request) =>
        request.Complexity is NodalOsOcrVisionComplexity.High or NodalOsOcrVisionComplexity.VeryHigh ? 0.02m : 0m;

    private static NodalOsOcrVisionFallbackPlan LocalFallback(NodalOsOcrVisionProviderRegistry registry)
    {
        var providers = registry.Providers
            .Where(provider => provider.Kind is NodalOsOcrVisionProviderKind.LocalPaddleOcr or NodalOsOcrVisionProviderKind.LocalTesseract)
            .Select(provider => provider.ProviderId)
            .ToArray();
        return new NodalOsOcrVisionFallbackPlan(providers, HumanReviewFallback: true, "local OCR stubs first, human review if low confidence or blocked");
    }

    private static NodalOsOcrVisionFallbackPlan HumanFallback() =>
        new([new NodalOsOcrVisionProviderId("human-review")], HumanReviewFallback: true, "human-in-the-loop required");

    private static NodalOsOcrVisionRoutingDecision Decision(
        NodalOsOcrVisionRoutingRequest request,
        NodalOsOcrVisionRoutingStatus status,
        NodalOsOcrVisionProviderId? providerId,
        NodalOsOcrVisionRoutingReason reason,
        string blockedReason,
        decimal estimatedCost,
        bool human = false,
        NodalOsOcrVisionFallbackPlan? fallback = null,
        NodalOsGroundingRisk risk = NodalOsGroundingRisk.Low)
    {
        var refs = request.GroundingSnapshot?.EvidenceRefs ?? [];
        return new NodalOsOcrVisionRoutingDecision(
            $"ocr-route-{Guid.NewGuid():N}",
            status,
            providerId,
            fallback ?? HumanFallback(),
            reason,
            new NodalOsOcrVisionBudgetDecision(estimatedCost <= request.MaxEstimatedCost, estimatedCost, request.MaxEstimatedCost, estimatedCost <= request.MaxEstimatedCost ? "within budget" : "budget exceeded"),
            estimatedCost,
            request.RequiredConfidence >= 0.85 ? "high" : request.RequiredConfidence >= 0.7 ? "medium" : "low",
            risk,
            "OCR/Vision routing is redacted, crop-first, no-authority; SaaS disabled unless future configured approval exists.",
            RequiresHumanApproval: human || status is NodalOsOcrVisionRoutingStatus.AskHuman or NodalOsOcrVisionRoutingStatus.Blocked or NodalOsOcrVisionRoutingStatus.BlockedByBudget,
            NoAuthority: true,
            BrowserCredentialRedactor.Redact(blockedReason),
            refs,
            CallsRealSaas: false,
            Redacted: true);
    }
}

public sealed class NodalOsLocalOcrWorkerBoundaryService
{
    public NodalOsLocalOcrWorkerContract CreateModelOnlyContract(
        NodalOsLocalOcrWorkerHealthStatus healthStatus = NodalOsLocalOcrWorkerHealthStatus.NotInstalled,
        bool enabled = false) =>
        new(
            "local-ocr-worker-boundary-v1",
            NodalOsOcrVisionActivationState.ModelOnly,
            Health(healthStatus, enabled),
            NodalOsLocalOcrWorkerCapability.RedactedCrops |
            NodalOsLocalOcrWorkerCapability.SyntheticFixtures |
            NodalOsLocalOcrWorkerCapability.PrintedText |
            NodalOsLocalOcrWorkerCapability.BoundingBoxes |
            NodalOsLocalOcrWorkerCapability.JsonContract,
            new NodalOsLocalOcrWorkerRuntimeProfile(
                "future local worker",
                "json contract over local process/container/cli/loopback",
                "model-only",
                MaxLatencyMs: 2500,
                MaxMemoryMb: 1024,
                PythonCoupledToCore: false,
                InvokesExternalProcess: false),
            new NodalOsLocalOcrWorkerInvocationPolicy(
                OnlyRedactedCropsByDefault: true,
                AllowFullScreen: false,
                AllowSensitiveSurfaces: false,
                MaxImageWidth: 1600,
                MaxImageHeight: 1200,
                MaxPages: 1,
                MaxLatencyMs: 2500,
                MaxMemoryMb: 1024,
                AllowedEngines: [NodalOsOcrEngineHint.PaddleOcr, NodalOsOcrEngineHint.Tesseract, NodalOsOcrEngineHint.DisabledStub],
                PersistRawImages: false,
                RedactedEvidenceOnly: true,
                NoAuthority: true),
            JsonBoundary: true,
            CorePythonDecoupled: true,
            CallsRealOcr: false,
            CallsRealSaas: false,
            NoAuthority: true);

    public NodalOsLocalOcrWorkerResponse InvokeModelOnly(
        NodalOsLocalOcrWorkerContract contract,
        NodalOsLocalOcrWorkerRequest request)
    {
        var error = Validate(contract, request);
        if (error != NodalOsLocalOcrWorkerError.None)
            return Response(contract, request, error, [], 0d, ["local OCR worker invocation blocked"]);

        var confidence = request.Engine == NodalOsOcrEngineHint.Tesseract ? 0.74d : 0.88d;
        var status = confidence < 0.8d
            ? NodalOsLocalOcrWorkerHealthStatus.Degraded
            : NodalOsLocalOcrWorkerHealthStatus.Available;
        var warnings = confidence < 0.8d
            ? new[] { "low confidence local worker fixture requires human review" }
            : new[] { "model-only worker response; no OCR runtime invoked" };
        return new NodalOsLocalOcrWorkerResponse(
            $"local-ocr-worker-response-{Guid.NewGuid():N}",
            status,
            NodalOsLocalOcrWorkerError.None,
            [new NodalOsOcrTextBlock("worker-block-1", "redacted worker fixture text", request.Region, new NodalOsOcrConfidence(confidence), NodalOsOcrLanguage.English, Redacted: true)],
            new NodalOsOcrConfidence(confidence),
            warnings.Select(BrowserCredentialRedactor.Redact).ToArray(),
            [new NodalOsGroundingEvidenceRef($"local-worker:{request.RequestId}:redacted", "local worker model-only evidence", Redacted: true)],
            RequiresHumanReview: confidence < 0.8d,
            InvokedExternalProcess: false,
            CallsRealOcr: false,
            CallsRealSaas: false,
            CanApproveAction: false,
            CanClick: false,
            CanSubmit: false,
            NoAuthority: true,
            Redacted: true);
    }

    private static NodalOsLocalOcrWorkerHealth Health(NodalOsLocalOcrWorkerHealthStatus status, bool enabled)
    {
        var installed = status is not NodalOsLocalOcrWorkerHealthStatus.NotInstalled;
        var available = status == NodalOsLocalOcrWorkerHealthStatus.Available && enabled;
        var effectiveStatus = installed && !enabled
            ? NodalOsLocalOcrWorkerHealthStatus.InstalledButDisabled
            : status;
        return new NodalOsLocalOcrWorkerHealth(
            effectiveStatus,
            installed,
            enabled,
            available,
            BrowserCredentialRedactor.Redact(Reason(effectiveStatus)),
            InvokesExternalProcess: false,
            StoresSecrets: false,
            NoAuthority: true);
    }

    private static string Reason(NodalOsLocalOcrWorkerHealthStatus status) =>
        status switch
        {
            NodalOsLocalOcrWorkerHealthStatus.NotInstalled => "local OCR worker is not installed; OCR real disabled",
            NodalOsLocalOcrWorkerHealthStatus.InstalledButDisabled => "local OCR worker is installed but disabled by policy",
            NodalOsLocalOcrWorkerHealthStatus.Available => "local OCR worker boundary available for model-only synthetic requests",
            NodalOsLocalOcrWorkerHealthStatus.Degraded => "local OCR worker degraded; human review required",
            NodalOsLocalOcrWorkerHealthStatus.VersionMismatch => "local OCR worker version mismatch",
            NodalOsLocalOcrWorkerHealthStatus.BlockedByPolicy => "local OCR worker blocked by policy",
            _ => "local OCR worker error"
        };

    private static NodalOsLocalOcrWorkerError Validate(NodalOsLocalOcrWorkerContract contract, NodalOsLocalOcrWorkerRequest request)
    {
        if (contract.Health.Status == NodalOsLocalOcrWorkerHealthStatus.NotInstalled)
            return NodalOsLocalOcrWorkerError.WorkerNotInstalled;
        if (!contract.Health.Enabled || !contract.Health.Available)
            return NodalOsLocalOcrWorkerError.WorkerDisabled;
        if (request.RedactionStatus is NodalOsGroundingRedactionStatus.RedactionFailed or NodalOsGroundingRedactionStatus.BlockedSensitive || !request.CropRedacted)
            return NodalOsLocalOcrWorkerError.RedactionFailed;
        if (!NodalOsImageCropRedactor.IsValidForOcr(request.RedactionResult))
            return NodalOsLocalOcrWorkerError.RedactionFailed;
        if (request.FullScreen && !contract.InvocationPolicy.AllowFullScreen)
            return NodalOsLocalOcrWorkerError.FullScreenBlocked;
        if (request.Sensitivity >= NodalOsOcrVisionSensitivity.SensitiveSurface && !contract.InvocationPolicy.AllowSensitiveSurfaces)
            return NodalOsLocalOcrWorkerError.SensitiveSurfaceBlocked;
        if (request.ImageWidth > contract.InvocationPolicy.MaxImageWidth || request.ImageHeight > contract.InvocationPolicy.MaxImageHeight)
            return NodalOsLocalOcrWorkerError.ImageTooLarge;
        if (request.Pages > contract.InvocationPolicy.MaxPages)
            return NodalOsLocalOcrWorkerError.PageLimitExceeded;
        if (request.MaxLatencyMs > contract.InvocationPolicy.MaxLatencyMs)
            return NodalOsLocalOcrWorkerError.LatencyLimitExceeded;
        if (!contract.InvocationPolicy.AllowedEngines.Contains(request.Engine))
            return NodalOsLocalOcrWorkerError.EngineNotAllowed;
        if (request.PersistRawImage || !request.Redacted)
            return NodalOsLocalOcrWorkerError.PolicyBlocked;
        return NodalOsLocalOcrWorkerError.None;
    }

    private static NodalOsLocalOcrWorkerResponse Response(
        NodalOsLocalOcrWorkerContract contract,
        NodalOsLocalOcrWorkerRequest request,
        NodalOsLocalOcrWorkerError error,
        IReadOnlyList<NodalOsOcrTextBlock> blocks,
        double confidence,
        IReadOnlyList<string> warnings) =>
        new(
            $"local-ocr-worker-response-{Guid.NewGuid():N}",
            contract.Health.Status,
            error,
            blocks,
            new NodalOsOcrConfidence(confidence),
            warnings.Select(BrowserCredentialRedactor.Redact).ToArray(),
            [new NodalOsGroundingEvidenceRef($"local-worker:{request.RequestId}:blocked", "local worker blocked evidence", Redacted: true)],
            RequiresHumanReview: true,
            InvokedExternalProcess: false,
            CallsRealOcr: false,
            CallsRealSaas: false,
            CanApproveAction: false,
            CanClick: false,
            CanSubmit: false,
            NoAuthority: true,
            Redacted: true);
}

public sealed class NodalOsOcrRealActivationGate
{
    public NodalOsOcrActivationDecision Evaluate(NodalOsOcrActivationReadiness readiness)
    {
        var requirements = Requirements(readiness);
        var decision = DecisionKind(readiness, requirements);
        var state = ActivationState(decision);
        return new NodalOsOcrActivationDecision(
            $"ocr-activation-{Guid.NewGuid():N}",
            decision,
            state,
            requirements,
            BrowserCredentialRedactor.Redact(Reason(decision)),
            RealOcrEnabled: false,
            RealSaasEnabled: false,
            NoAuthority: true,
            RequiresHumanReview: decision.ToString().StartsWith("Blocked", StringComparison.OrdinalIgnoreCase),
            Redacted: true);
    }

    public NodalOsOcrActivationReadiness CurrentPhaseReadiness() =>
        new(
            new NodalOsOcrVisionProviderId("local-paddleocr-stub"),
            NodalOsOcrVisionProviderKind.LocalPaddleOcr,
            new NodalOsOcrActivationScope(NodalOsOcrActivationScopeKind.BlockedCurrentPhase, LocalOnly: true, AllowsSaas: false, AllowsFullScreen: false, AllowsSensitive: false, "current phase blocks real OCR; model-only fixture-first remains active"),
            ProviderExplicitlyEnabled: false,
            LocalWorkerInstalled: false,
            LocalWorkerAvailable: false,
            OptIn: false,
            RedactionGatePassed: false,
            SensitivePolicyPassed: true,
            FullScreenDisabledOrApproved: true,
            BudgetConfigured: false,
            PrivacyProfileAccepted: false,
            new NodalOsOcrActivationAuditEvidence(Present: false, [], "no real OCR activation audit exists", Redacted: true),
            NoAuthorityConfirmed: true,
            HumanEscalationPolicyConfigured: true,
            EvaluationHarnessPassed: true,
            RollbackPauseConfigured: false,
            CurrentPhaseAllowsSaasReal: false,
            Redacted: true);

    private static IReadOnlyList<NodalOsOcrActivationRequirement> Requirements(NodalOsOcrActivationReadiness readiness) =>
    [
        Requirement("provider explicitly enabled", readiness.ProviderExplicitlyEnabled, "provider flag enabled", "provider remains disabled/model-only"),
        Requirement("local worker installed", readiness.LocalWorkerInstalled, "worker installed", "worker not installed"),
        Requirement("local worker available", readiness.LocalWorkerAvailable, "worker available", "worker unavailable"),
        Requirement("opt-in", readiness.OptIn, "operator/admin opt-in present", "missing opt-in"),
        Requirement("redaction gate", readiness.RedactionGatePassed, "redaction passed", "redaction failed or not evaluated"),
        Requirement("sensitive policy", readiness.SensitivePolicyPassed, "sensitive policy passed", "sensitive surface blocked"),
        Requirement("fullscreen policy", readiness.FullScreenDisabledOrApproved, "full-screen disabled or explicitly approved", "full-screen not approved"),
        Requirement("budget", readiness.BudgetConfigured, "budget configured", "budget missing"),
        Requirement("privacy", readiness.PrivacyProfileAccepted, "privacy accepted", "privacy profile not accepted"),
        Requirement("audit evidence", readiness.AuditEvidence.Present, readiness.AuditEvidence.Summary, "audit evidence missing"),
        Requirement("no-authority", readiness.NoAuthorityConfirmed, "OCR no-authority confirmed", "OCR authority violation"),
        Requirement("human escalation", readiness.HumanEscalationPolicyConfigured, "human escalation policy configured", "human escalation missing"),
        Requirement("evaluation harness", readiness.EvaluationHarnessPassed, "evaluation harness passed", "evaluation harness missing/failing"),
        Requirement("rollback/pause", readiness.RollbackPauseConfigured, "rollback/pause configured", "rollback/pause missing")
    ];

    private static NodalOsOcrActivationDecisionKind DecisionKind(
        NodalOsOcrActivationReadiness readiness,
        IReadOnlyList<NodalOsOcrActivationRequirement> requirements)
    {
        if (readiness.RequiresExternalDataTransfer && !readiness.CurrentPhaseAllowsSaasReal)
            return NodalOsOcrActivationDecisionKind.BlockedByDefault;
        if (!readiness.ProviderExplicitlyEnabled)
            return NodalOsOcrActivationDecisionKind.BlockedByDefault;
        if (!readiness.OptIn)
            return NodalOsOcrActivationDecisionKind.BlockedByMissingOptIn;
        if (!readiness.LocalWorkerInstalled || !readiness.LocalWorkerAvailable)
            return NodalOsOcrActivationDecisionKind.BlockedByMissingWorker;
        if (!readiness.RedactionGatePassed)
            return NodalOsOcrActivationDecisionKind.BlockedByRedaction;
        if (!readiness.SensitivePolicyPassed || readiness.Scope.AllowsSensitive)
            return NodalOsOcrActivationDecisionKind.BlockedBySensitivePolicy;
        if (!readiness.FullScreenDisabledOrApproved || readiness.Scope.AllowsFullScreen)
            return NodalOsOcrActivationDecisionKind.BlockedByRedaction;
        if (!readiness.BudgetConfigured)
            return NodalOsOcrActivationDecisionKind.BlockedByBudget;
        if (!readiness.PrivacyProfileAccepted)
            return NodalOsOcrActivationDecisionKind.BlockedByPrivacy;
        if (!readiness.AuditEvidence.Present || !readiness.EvaluationHarnessPassed || !readiness.RollbackPauseConfigured)
            return NodalOsOcrActivationDecisionKind.BlockedByMissingAudit;
        if (!readiness.NoAuthorityConfirmed)
            return NodalOsOcrActivationDecisionKind.BlockedByNoAuthorityViolation;
        if (!readiness.HumanEscalationPolicyConfigured)
            return NodalOsOcrActivationDecisionKind.BlockedByMissingAudit;
        return readiness.Scope.Kind switch
        {
            NodalOsOcrActivationScopeKind.SyntheticOnly => NodalOsOcrActivationDecisionKind.ReadyForSyntheticOnly,
            NodalOsOcrActivationScopeKind.RedactedCropShadow => NodalOsOcrActivationDecisionKind.ReadyForRedactedCropShadow,
            NodalOsOcrActivationScopeKind.ControlledLocalUse => NodalOsOcrActivationDecisionKind.ReadyForControlledLocalUse,
            _ => NodalOsOcrActivationDecisionKind.BlockedByDefault
        };
    }

    private static NodalOsOcrVisionActivationState ActivationState(NodalOsOcrActivationDecisionKind decision) =>
        decision switch
        {
            NodalOsOcrActivationDecisionKind.ReadyForSyntheticOnly => NodalOsOcrVisionActivationState.LocalWorkerEnabledForSynthetic,
            NodalOsOcrActivationDecisionKind.ReadyForRedactedCropShadow => NodalOsOcrVisionActivationState.LocalWorkerEnabledForRedactedCrops,
            NodalOsOcrActivationDecisionKind.ReadyForControlledLocalUse => NodalOsOcrVisionActivationState.LocalWorkerAvailable,
            NodalOsOcrActivationDecisionKind.BlockedByBudget => NodalOsOcrVisionActivationState.BlockedByBudget,
            NodalOsOcrActivationDecisionKind.BlockedByPrivacy => NodalOsOcrVisionActivationState.BlockedByPrivacy,
            NodalOsOcrActivationDecisionKind.BlockedByMissingAudit => NodalOsOcrVisionActivationState.BlockedByMissingAudit,
            _ => NodalOsOcrVisionActivationState.BlockedByPolicy
        };

    private static string Reason(NodalOsOcrActivationDecisionKind decision) =>
        decision switch
        {
            NodalOsOcrActivationDecisionKind.BlockedByDefault => "real OCR is blocked by default in current phase",
            NodalOsOcrActivationDecisionKind.BlockedByMissingOptIn => "real OCR activation requires explicit opt-in",
            NodalOsOcrActivationDecisionKind.BlockedByMissingWorker => "local OCR worker is missing or unavailable",
            NodalOsOcrActivationDecisionKind.BlockedByRedaction => "redaction/crop policy blocks OCR activation",
            NodalOsOcrActivationDecisionKind.BlockedBySensitivePolicy => "sensitive policy blocks OCR activation",
            NodalOsOcrActivationDecisionKind.BlockedByBudget => "budget is missing or insufficient",
            NodalOsOcrActivationDecisionKind.BlockedByPrivacy => "privacy profile has not been accepted",
            NodalOsOcrActivationDecisionKind.BlockedByMissingAudit => "audit/evaluation/rollback evidence missing",
            NodalOsOcrActivationDecisionKind.BlockedByNoAuthorityViolation => "OCR authority violation detected",
            NodalOsOcrActivationDecisionKind.ReadyForSyntheticOnly => "future local worker could run synthetic-only OCR under gate",
            NodalOsOcrActivationDecisionKind.ReadyForRedactedCropShadow => "future local worker could run redacted crop shadow OCR under gate",
            NodalOsOcrActivationDecisionKind.ReadyForControlledLocalUse => "future local worker could run controlled local OCR under gate",
            _ => "OCR activation decision"
        };

    private static NodalOsOcrActivationRequirement Requirement(string id, bool satisfied, string evidence, string missingReason) =>
        new(id, satisfied, BrowserCredentialRedactor.Redact(evidence), BrowserCredentialRedactor.Redact(satisfied ? string.Empty : missingReason));
}
