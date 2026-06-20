using System.Text.Json;
using System.Text.Json.Serialization;
using OneBrain.AgentOperations.Contracts;

namespace OneBrain.AgentOperations.Core;

public sealed class NodalOsScanConsentReviewCardsService
{
    public IReadOnlyList<NodalOsScanConsentReviewCard> CreateCards(
        NodalOsConsentRequestDraft consent,
        NodalOsScopePreviewContract scope) =>
        Enum.GetValues<NodalOsScanConsentReviewStatus>()
            .Select((status, index) => CreateCard(consent, scope, status, index + 1))
            .ToArray();

    public NodalOsScanConsentReviewResult ApplyOption(
        NodalOsScanConsentReviewCard card,
        NodalOsScanConsentReviewOption option) =>
        new()
        {
            ReviewResultId = $"scan-consent-review-result-{option}",
            CardRef = card.CardId,
            SelectedOption = option,
            IsNoOp = true,
            MutatesState = false,
            AuthorizesRealScan = false,
            AuthorizesDirectoryListing = false,
            AuthorizesFileRead = false,
            AuthorizesFileHash = false,
            AuthorizesIndexing = false,
            AuthorizesVectorization = false,
            AuthorizesLlmContext = false,
            AuthorizesCloud = false,
            RequiresFutureExplicitGate = true,
            UserFacingExplanationRedacted = "Review option is recorded as preview intent only and cannot authorize operational scan behavior.",
            GuardrailRefs =
            [
                "guardrail-consent-review-no-op",
                "guardrail-consent-review-non-authorizing",
                "guardrail-future-explicit-gate-required"
            ]
        };

    private static NodalOsScanConsentReviewCard CreateCard(
        NodalOsConsentRequestDraft consent,
        NodalOsScopePreviewContract scope,
        NodalOsScanConsentReviewStatus status,
        int index) =>
        new()
        {
            CardId = $"scan-consent-review-card-{index:000}",
            ConsentRequestRef = consent.ConsentRequestId,
            ScopePreviewRef = scope.ScopePreviewId,
            RiskLevelRedacted = status is NodalOsScanConsentReviewStatus.Draft ? "Review" : "Blocked",
            PurposeRedacted = consent.UserFacingPurposeRedacted,
            RequestedCapabilityRedacted = consent.RequestedCapability.ToString(),
            DataExposureExplanationRedacted = consent.DataExposureExplanationRedacted,
            NoMutationGuarantee = true,
            LlmDisabledDisclosureRedacted = "LLM context remains disabled.",
            CloudDisabledDisclosureRedacted = "Cloud remains disabled.",
            FilesystemDisabledDisclosureRedacted = "Filesystem access remains disabled.",
            ReviewStatus = status,
            IsNoOp = true,
            CanAuthorizeRealScan = false,
            CanAuthorizeFileRead = false,
            CanAuthorizeIndexing = false,
            CanAuthorizeVectorization = false,
            CanAuthorizeLlmContext = false,
            GuardrailRefs =
            [
                "guardrail-review-card-no-op",
                "guardrail-review-card-non-authorizing",
                "guardrail-real-scan-gate-blocked"
            ]
        };
}

public sealed class NodalOsScanConsentReviewCardsJsonSerializer
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
        WriteIndented = true
    };

    public string SerializeCards(IReadOnlyList<NodalOsScanConsentReviewCard> cards) =>
        JsonSerializer.Serialize(cards, Options);

    public string SerializeResult(NodalOsScanConsentReviewResult result) =>
        JsonSerializer.Serialize(result, Options);
}

public static class NodalOsScanConsentReviewCardsFixtures
{
    public static IReadOnlyList<NodalOsScanConsentReviewCard> Cards() =>
        new NodalOsScanConsentReviewCardsService().CreateCards(
            NodalOsConsentScopePreviewFixtures.Consent(),
            NodalOsConsentScopePreviewFixtures.Scope());
}
