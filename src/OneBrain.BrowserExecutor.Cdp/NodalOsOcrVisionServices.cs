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
