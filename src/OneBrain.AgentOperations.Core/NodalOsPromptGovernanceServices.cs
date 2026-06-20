using System.Text.Json;
using System.Text.Json.Serialization;
using OneBrain.AgentOperations.Contracts;

namespace OneBrain.AgentOperations.Core;

public sealed class NodalOsPromptGovernanceService
{
    private readonly NodalOsSensitiveContentClassifier classifier = new();

    public NodalOsPromptGovernancePolicy CreatePromptGovernancePolicy(
        NodalOsPromptGovernanceState state,
        NodalOsPromptPurpose purpose = NodalOsPromptPurpose.ProjectUnderstandingFuture,
        string? workspaceId = null,
        string? missionId = null,
        string? providerSettingsRef = "provider-settings-ref-required",
        string safeContextBoundaryRef = "safe-context-boundary-required")
    {
        return new()
        {
            PromptGovernancePolicyId = $"prompt-governance-{purpose}-{state}",
            WorkspaceId = workspaceId,
            MissionId = missionId,
            ProviderSettingsRef = providerSettingsRef,
            SafeContextBoundaryRef = SafeValue(safeContextBoundaryRef),
            AllowedContextRefsRedacted =
            [
                "public-safe-context-ref",
                "user-provided-safe-context-ref",
                "evidence-ref-only-context-ref"
            ],
            DeniedContextRefsRedacted =
            [
                "credential-like-context-blocked",
                "raw-payload-context-blocked",
                "sensitive-context-blocked",
                "unknown-context-review-required"
            ],
            RequiredRedactionPolicyRef = "redaction-policy-required",
            RequiredConsentRef = "future-user-consent-required",
            RequiredProvenanceLabelsRedacted = ["provenance label required"],
            RequiredConfidenceFreshnessLabelsRedacted = ["confidence label required", "freshness label required"],
            AllowedPromptPurpose = purpose,
            DeniedPromptPurposes = DeniedPurposesFor(purpose),
            PromptConstructionStatus = state,
            HumanReviewRequirementRedacted = HumanReviewFor(state),
            EvidenceRefs = [EvidenceRef("evidence-prompt-governance-ref-only")],
            TimelineRefs = ["timeline-prompt-governance-ref-only"],
            GuardrailRefs =
            [
                "guardrail-no-final-prompt",
                "guardrail-no-provider-call",
                "guardrail-no-model-context-send",
                "guardrail-budget-required"
            ],
            DisclosuresRedacted =
            [
                "Prompt construction real is not implemented.",
                "No context was sent to a model.",
                "No provider call was made.",
                "Future prompt requires BYOK, consent, policy and budget guardrails."
            ],
            RequiresSafeContextBoundary = true,
            RequiresRedaction = true,
            RequiresConsent = true,
            RequiresProvenanceConfidenceFreshness = true,
            RequiresBudgetGuardrails = true,
            RequiresByokPolicy = true,
            RequiresHumanReview = state is NodalOsPromptGovernanceState.RequiresHumanReview
                or NodalOsPromptGovernanceState.UnknownRequiresReview
                or NodalOsPromptGovernanceState.EligibleForFuturePromptWithConsent,
            GeneratesFinalPromptText = false,
            CallsProvider = false,
            CallsLlmProvider = false,
            CallsCloud = false,
            IncludesRawPayload = false,
            IncludesRawEvidence = false,
            ReadsFilesystem = false,
            CanAuthorizeExecution = false,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public NodalOsBudgetGuardrailsDraft CreateBudgetGuardrailsDraft(
        NodalOsBudgetGuardrailStatus status = NodalOsBudgetGuardrailStatus.DraftOnly,
        NodalOsBudgetPolicyScope scope = NodalOsBudgetPolicyScope.WorkspaceFuture,
        string maxSpendPlaceholder = "max-spend-placeholder-not-configured")
    {
        return new()
        {
            BudgetPolicyId = $"budget-guardrails-{scope}-{status}",
            Scope = scope,
            BudgetStatus = status,
            CurrencyPlaceholderRedacted = "currency-placeholder",
            MaxSpendPlaceholderRedacted = SafeValue(maxSpendPlaceholder),
            MaxTokensPlaceholderRedacted = "max-token-placeholder-no-real-counting",
            MaxCallsPlaceholderRedacted = "max-call-placeholder",
            MaxRetriesPlaceholderRedacted = "max-retry-placeholder",
            MaxConcurrentRequestsPlaceholderRedacted = "max-concurrent-request-placeholder",
            ModelTierRestrictionsRedacted =
            [
                "high-cost model tiers blocked until approval",
                "unknown-cost model tiers blocked until policy",
                "managed AI requires separate policy"
            ],
            RequiresUserConfirmationAboveThreshold = true,
            CostVisibilityRequirementRedacted = "Future cost visibility required before any LLM call.",
            StopCancelRequirementRedacted = "Future stop/cancel controls required before provider use.",
            EvidenceTimelineRequirementsRedacted = ["budget policy evidence ref required", "budget decision timeline ref required"],
            DisabledStateReasonRedacted = "Draft only; BYOK does not mean free cost.",
            DisclosuresRedacted =
            [
                "BYOK does not mean cost free.",
                "Managed AI future requires separate policy.",
                "No LLM call can occur without budget guardrails."
            ],
            DraftOnly = true,
            PerformsTokenCounting = false,
            PerformsLiveCostLookup = false,
            CallsProvider = false,
            PerformsBilling = false,
            CallsCloud = false,
            UsesProviderSdk = false,
            SendsNetworkRequest = false,
            CallsLlmProvider = false,
            CanAuthorizeExecution = false,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public NodalOsModelCapabilityProfile CreateModelCapabilityProfile(
        NodalOsByokProviderKind providerKind,
        string modelIdPlaceholder = "model-placeholder-not-verified")
    {
        var isLocal = providerKind is NodalOsByokProviderKind.LocalModelFuture
            or NodalOsByokProviderKind.OllamaFuture
            or NodalOsByokProviderKind.LmStudioFuture;

        return new()
        {
            ModelCapabilityProfileId = $"model-capability-{providerKind}",
            ProviderKind = providerKind,
            ModelIdPlaceholderRedacted = SafeValue(modelIdPlaceholder),
            CapabilityFlags =
            [
                NodalOsModelCapabilityKind.ChatFuture,
                NodalOsModelCapabilityKind.ReasoningFuture,
                NodalOsModelCapabilityKind.SummarizationFuture,
                NodalOsModelCapabilityKind.ProjectUnderstandingFuture,
                NodalOsModelCapabilityKind.AssignmentPlanningFuture,
                NodalOsModelCapabilityKind.ExpertAdvisorFuture,
                NodalOsModelCapabilityKind.CodeAssistanceFuture,
                NodalOsModelCapabilityKind.VisionFuture,
                NodalOsModelCapabilityKind.EmbeddingsFuture,
                NodalOsModelCapabilityKind.ToolUseFuture,
                NodalOsModelCapabilityKind.BrowserAutomationFuture,
                NodalOsModelCapabilityKind.Unknown
            ],
            AllowedUseCasesRedacted =
            [
                "display-only policy preview",
                "future summary after consent and budget policy",
                "future code assistance after prompt governance"
            ],
            DeniedUseCasesRedacted =
            [
                "browser automation future disabled by default",
                "tool use future disabled by default",
                "embeddings future disabled until embeddings policy",
                "execution authority denied"
            ],
            RiskNotesRedacted =
            [
                "ProjectUnderstandingFuture gated by Project Understanding Policy.",
                "AssignmentPlanningFuture gated by Prompt Governance, Budget, and BYOK.",
                "ExpertAdvisorFuture is non-executor by default."
            ],
            CostTierPlaceholderRedacted = "cost-tier-placeholder-no-pricing-lookup",
            LatencyTierPlaceholderRedacted = "latency-tier-placeholder-no-live-check",
            ContextWindowTierPlaceholderRedacted = "context-window-placeholder-no-model-lookup",
            ReliabilityTierPlaceholderRedacted = "reliability-placeholder-no-provider-check",
            PrivacyModeCompatibilityRedacted = isLocal
                ? "local-model-compatible-future-policy-required"
                : "managed-provider-future-policy-required",
            ByokRequired = !isLocal,
            LocalModelPossible = isLocal,
            ManagedAiPossible = !isLocal,
            PromptGovernanceRequired = true,
            BudgetGuardrailsRequired = true,
            HumanReviewRequired = true,
            BrowserAutomationFutureEnabledByDefault = false,
            ToolUseFutureEnabledByDefault = false,
            EmbeddingsFutureEnabledBeforePolicy = false,
            ExpertAdvisorCanExecute = false,
            PerformsLiveModelCheck = false,
            PerformsLiveCostLookup = false,
            CreatesRoutingDecision = false,
            CallsProvider = false,
            CanAuthorizeExecution = false,
            EvidenceRefs = [EvidenceRef("evidence-model-capability-matrix-ref-only")],
            TimelineRefs = ["timeline-model-capability-matrix-ref-only"],
            GuardrailRefs =
            [
                "guardrail-no-model-lookup",
                "guardrail-no-pricing-lookup",
                "guardrail-no-routing",
                "guardrail-no-provider-call"
            ],
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    private string SafeValue(string value)
    {
        if (classifier.ContainsSensitiveContent(value) || value.Contains("sk-", StringComparison.OrdinalIgnoreCase))
            return "redacted-value";

        return value;
    }

    private static IReadOnlyList<NodalOsPromptPurpose> DeniedPurposesFor(NodalOsPromptPurpose allowed) =>
        Enum.GetValues<NodalOsPromptPurpose>()
            .Where(purpose => purpose != allowed)
            .ToArray();

    private static string HumanReviewFor(NodalOsPromptGovernanceState state) => state switch
    {
        NodalOsPromptGovernanceState.AllowedForPreviewOnly => "Human review remains available for preview-only policy.",
        NodalOsPromptGovernanceState.EligibleForFuturePromptWithConsent => "Human review required before any future prompt eligibility can become operational.",
        NodalOsPromptGovernanceState.RequiresHumanReview => "Human review required.",
        NodalOsPromptGovernanceState.UnknownRequiresReview => "Unknown state requires review.",
        _ => "Human review required before provider use."
    };

    private static NodalOsEvidenceBridgeRef EvidenceRef(string evidenceId) =>
        new()
        {
            EvidenceId = evidenceId,
            Kind = "prompt-governance-ref-only",
            SourceKind = NodalOsEvidenceBridgeSourceKind.Manual,
            UseKind = NodalOsEvidenceBridgeUseKind.AuditTrail,
            Authority = NodalOsEvidenceBridgeAuthority.NoAuthority,
            Sensitivity = NodalOsEvidenceSensitivity.NonSensitive,
            RedactionState = NodalOsEvidenceRedactionState.NotRequired,
            LedgerRef = $"ledger:{evidenceId}",
            Provenance = "M516-M518 prompt governance policy contract",
            CreatedAt = DateTimeOffset.UtcNow
        };
}

public sealed class NodalOsPromptGovernanceJsonSerializer
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
        WriteIndented = true
    };

    public string SerializePromptGovernance(NodalOsPromptGovernancePolicy policy) =>
        JsonSerializer.Serialize(policy, Options);

    public string SerializeBudgetGuardrails(NodalOsBudgetGuardrailsDraft draft) =>
        JsonSerializer.Serialize(draft, Options);

    public string SerializeModelCapability(NodalOsModelCapabilityProfile profile) =>
        JsonSerializer.Serialize(profile, Options);
}

public static class NodalOsPromptGovernanceFixtures
{
    public static NodalOsPromptGovernancePolicy PromptPreviewPolicy() =>
        new NodalOsPromptGovernanceService().CreatePromptGovernancePolicy(
            NodalOsPromptGovernanceState.AllowedForPreviewOnly,
            NodalOsPromptPurpose.ProjectUnderstandingFuture,
            NodalOsWorkspaceFixtures.ActiveReadOnlyWorkspace().WorkspaceId);

    public static NodalOsBudgetGuardrailsDraft BudgetDraft() =>
        new NodalOsPromptGovernanceService().CreateBudgetGuardrailsDraft();

    public static NodalOsModelCapabilityProfile OpenAiFutureProfile() =>
        new NodalOsPromptGovernanceService().CreateModelCapabilityProfile(NodalOsByokProviderKind.OpenAiFuture);
}
