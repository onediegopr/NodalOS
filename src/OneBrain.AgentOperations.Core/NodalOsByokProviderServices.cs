using System.Text.Json;
using System.Text.Json.Serialization;
using OneBrain.AgentOperations.Contracts;

namespace OneBrain.AgentOperations.Core;

public sealed class NodalOsByokProviderSettingsService
{
    private readonly NodalOsSensitiveContentClassifier classifier = new();

    public NodalOsByokProviderSettings CreateProviderSettings(
        NodalOsByokProviderKind providerKind,
        NodalOsByokProviderKeyStatus keyStatus = NodalOsByokProviderKeyStatus.NotConfigured,
        string? workspaceId = null,
        NodalOsByokProviderScope scope = NodalOsByokProviderScope.WorkspaceFuture,
        string credentialReferencePlaceholder = "credential-ref-not-configured",
        string endpointPolicyPlaceholder = "endpoint policy placeholder only")
    {
        var safeCredentialRef = SafeValue(credentialReferencePlaceholder);
        var safeEndpointPolicy = SafeValue(endpointPolicyPlaceholder);

        return new()
        {
            ProviderSettingsId = $"byok-provider-settings-{providerKind}-{keyStatus}",
            WorkspaceId = workspaceId,
            Scope = scope,
            ProviderKind = providerKind,
            ModelSelectionPlaceholderRedacted = "future model selection placeholder only",
            EndpointPolicyPlaceholderRedacted = safeEndpointPolicy,
            CredentialReferencePlaceholderRedacted = safeCredentialRef,
            ProviderKeyStatus = keyStatus,
            CapabilitiesDeclared =
            [
                NodalOsByokProviderCapability.ChatFuture,
                NodalOsByokProviderCapability.ReasoningFuture,
                NodalOsByokProviderCapability.CodeAssistanceFuture,
                NodalOsByokProviderCapability.ProjectUnderstandingFuture,
                NodalOsByokProviderCapability.AssignmentFuture,
                NodalOsByokProviderCapability.AdvisorFuture
            ],
            DisabledCapabilitiesRedacted =
            [
                "embeddings future disabled until separate policy",
                "vision future disabled until consent, redaction, and evidence policy",
                "routing disabled until governance exists"
            ],
            BudgetPolicyRef = "budget-policy-future-required",
            PromptGovernanceRef = "prompt-governance-future-required",
            ConsentRef = "consent-future-required",
            RedactionPolicyRef = "redaction-policy-required",
            SafeContextBoundaryRef = "safe-context-boundary-required",
            EvidenceRefs = [EvidenceRef("evidence-byok-provider-settings-ref-only")],
            TimelineRefs = ["timeline-byok-provider-settings-ref-only"],
            GuardrailRefs =
            [
                "guardrail-no-provider-call",
                "guardrail-no-network",
                "guardrail-no-raw-credential",
                "guardrail-no-prompt"
            ],
            ReferenceOnly = true,
            StoresRawCredential = false,
            CallsProvider = false,
            SendsNetworkRequest = false,
            CreatesPrompt = false,
            CreatesEmbeddings = false,
            RoutesLlmTraffic = false,
            CallsCloud = false,
            CanAuthorizeExecution = false,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public NodalOsProviderTestConnectionPreview CreateTestConnectionPreview(
        NodalOsByokProviderSettings settings,
        NodalOsProviderTestConnectionState state = NodalOsProviderTestConnectionState.MockOnly,
        string endpointTarget = "endpoint-target-redacted")
    {
        return new()
        {
            TestConnectionPreviewId = $"provider-test-connection-{settings.ProviderSettingsId}-{state}",
            ProviderSettingsRef = settings.ProviderSettingsId,
            State = state,
            CredentialRefStatusRedacted = SafeValue(settings.CredentialReferencePlaceholderRedacted),
            ModelTargetPlaceholderRedacted = settings.ModelSelectionPlaceholderRedacted,
            EndpointTargetRedacted = SafeValue(endpointTarget),
            PreflightChecksRedacted =
            [
                "provider settings ref exists",
                "credential ref status only",
                "user consent placeholder required",
                "budget policy placeholder required",
                "prompt governance placeholder required",
                "network remains disabled"
            ],
            UserConsentRequirementRedacted = "Future explicit user consent required before any provider test.",
            NetworkDisabledStatusRedacted = "Network is disabled in this contract preview.",
            DryRunMockStatusRedacted = "Mock-only dry run; no provider connection is attempted.",
            ExpectedSafeResultRedacted = "Expected result is explanatory only.",
            ErrorRedactionPolicyRedacted = "Future errors must be redacted before logs, evidence, timeline, or handoff.",
            EvidenceRefs = [EvidenceRef("evidence-provider-test-connection-ref-only")],
            TimelineRefs = ["timeline-provider-test-connection-ref-only"],
            ObservabilityRefs = ["observability-provider-test-connection-redacted"],
            GuardrailRefs =
            [
                "guardrail-test-connection-disabled",
                "guardrail-no-network",
                "guardrail-no-provider-sdk",
                "guardrail-no-env-read"
            ],
            ActionDisabled = true,
            MockOnly = true,
            PerformsNetworkRequest = false,
            UsesProviderSdk = false,
            ReadsEnvironmentVariables = false,
            CreatesPrompt = false,
            CallsLlmProvider = false,
            StoresRawCredential = false,
            CanAuthorizeExecution = false,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    private string SafeValue(string value)
    {
        if (classifier.ContainsSensitiveContent(value) || value.Contains("sk-", StringComparison.OrdinalIgnoreCase))
            return "redacted-value";

        return value;
    }

    private static NodalOsEvidenceBridgeRef EvidenceRef(string evidenceId) =>
        new()
        {
            EvidenceId = evidenceId,
            Kind = "provider-governance-ref-only",
            SourceKind = NodalOsEvidenceBridgeSourceKind.Manual,
            UseKind = NodalOsEvidenceBridgeUseKind.AuditTrail,
            Authority = NodalOsEvidenceBridgeAuthority.NoAuthority,
            Sensitivity = NodalOsEvidenceSensitivity.NonSensitive,
            RedactionState = NodalOsEvidenceRedactionState.NotRequired,
            LedgerRef = $"ledger:{evidenceId}",
            Provenance = "M513-M515 BYOK provider policy contract",
            CreatedAt = DateTimeOffset.UtcNow
        };
}

public sealed class NodalOsByokProviderJsonSerializer
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
        WriteIndented = true
    };

    public string SerializeProviderSettings(NodalOsByokProviderSettings settings) =>
        JsonSerializer.Serialize(settings, Options);

    public string SerializeTestConnectionPreview(NodalOsProviderTestConnectionPreview preview) =>
        JsonSerializer.Serialize(preview, Options);
}

public static class NodalOsByokProviderFixtures
{
    public static NodalOsByokProviderSettings ReferenceOnlySettings() =>
        new NodalOsByokProviderSettingsService().CreateProviderSettings(
            NodalOsByokProviderKind.OpenAiFuture,
            NodalOsByokProviderKeyStatus.ReferenceOnlyConfigured,
            NodalOsWorkspaceFixtures.ActiveReadOnlyWorkspace().WorkspaceId,
            credentialReferencePlaceholder: "credential-ref-not-configured");

    public static NodalOsProviderTestConnectionPreview MockOnlyTestConnection() =>
        new NodalOsByokProviderSettingsService().CreateTestConnectionPreview(ReferenceOnlySettings());
}
