using System.Text.Json;
using System.Text.Json.Serialization;
using OneBrain.AgentOperations.Contracts;

namespace OneBrain.AgentOperations.Core;

public sealed class NodalOsConsentLedgerUiPreviewService
{
    public NodalOsConsentLedgerUiPreview CreatePreview(
        NodalOsConsentScopeLedgerMock ledger,
        string workspaceRef = "workspace-ref-m567",
        string missionRef = "mission-ref-m567") =>
        new()
        {
            PreviewId = "consent-ledger-ui-preview-m567",
            LedgerRef = ledger.LedgerId,
            CapabilityGateUiReviewRef = "capability-gate-ui-review-m564",
            FailClosedAcceptancePackRef = "fail-closed-acceptance-pack-m566",
            WorkspaceRef = workspaceRef,
            MissionRef = missionRef,
            IsStaticPreview = true,
            IsReadOnly = true,
            IsNoOp = true,
            UsesProductivePersistence = false,
            UsesRealFilesystem = false,
            CanPersistConsent = false,
            CanAuthorizeCapability = false,
            CanAuthorizeFilesystemAccess = false,
            CanAuthorizeLlmContext = false,
            CanSendToCloud = false,
            UiSectionsRedacted =
            [
                "Ledger summary.",
                "Consent entries by capability.",
                "Scope status.",
                "Freshness status.",
                "Revocation status.",
                "Risk summary.",
                "Fail-closed summary.",
                "Missing requirements.",
                "Blocked capabilities.",
                "User-facing explanations.",
                "Next required audits."
            ],
            EntryCards = ledger.Entries.Select(CreateCard).ToArray(),
            ReviewOptions = Enum.GetValues<NodalOsConsentLedgerUiReviewOption>().Select(ApplyOption).ToArray()
        };

    public NodalOsConsentLedgerUiReviewOptionResult ApplyOption(NodalOsConsentLedgerUiReviewOption option) =>
        new()
        {
            OptionResultId = $"consent-ledger-ui-option-{option}",
            Option = option,
            IsNoOp = true,
            MutatesState = false,
            PersistsConsent = false,
            AuthorizesCapability = false,
            AuthorizesFilesystemAccess = false,
            AuthorizesLlmContext = false,
            SendsToCloud = false
        };

    private static NodalOsConsentLedgerEntryCard CreateCard(NodalOsConsentScopeEntry entry) =>
        new()
        {
            CardId = $"consent-ledger-entry-card-{entry.Capability}",
            Capability = entry.Capability,
            ConsentStatus = entry.ConsentStatus,
            ScopeStatus = entry.ScopeStatus,
            FreshnessStatus = entry.FreshnessStatus,
            RevocationStatus = entry.RevocationStatus,
            IsMockOnly = true,
            IsAuthoritative = false,
            CanAuthorizeRealUse = false,
            UserFacingPurposeRedacted = entry.UserFacingPurposeRedacted,
            RiskSummaryRedacted = entry.RiskSummaryRedacted,
            ReviewStatus = NodalOsConsentLedgerEntryReviewStatus.DraftReview
        };
}

public sealed class NodalOsConsentLedgerUiPreviewJsonSerializer
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
        WriteIndented = true
    };

    public string Serialize(NodalOsConsentLedgerUiPreview preview) => JsonSerializer.Serialize(preview, Options);
    public string SerializeOption(NodalOsConsentLedgerUiReviewOptionResult result) => JsonSerializer.Serialize(result, Options);
}
