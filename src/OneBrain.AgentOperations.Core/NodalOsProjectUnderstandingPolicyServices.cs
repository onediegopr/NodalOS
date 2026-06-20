using System.Text.Json;
using System.Text.Json.Serialization;
using OneBrain.AgentOperations.Contracts;

namespace OneBrain.AgentOperations.Core;

public sealed class NodalOsProjectUnderstandingPolicyService
{
    public NodalOsRealScanPreconditions CreateRealScanPreconditions(
        NodalOsWorkspaceLocalModel workspace,
        NodalOsPathJailBinding? pathJail,
        NodalOsRealScanPreconditionState? forcedState = null)
    {
        var state = forcedState ?? (pathJail is null
            ? NodalOsRealScanPreconditionState.BlockedByMissingPathJail
            : NodalOsRealScanPreconditionState.EligibleForPreviewOnly);

        return new()
        {
            PreconditionsId = $"real-scan-preconditions-{workspace.WorkspaceId}-{state}",
            WorkspaceId = workspace.WorkspaceId,
            PathJailBindingId = pathJail?.JailId,
            State = state,
            ExplicitConsentPlaceholderRedacted = state == NodalOsRealScanPreconditionState.BlockedByMissingConsent
                ? "Future explicit consent is missing."
                : "Future consent placeholder only; no productive consent requested.",
            ScanScopeRedacted = "Future scan scope must be explicit, path-jail-bound, previewed, and cancellable.",
            ExcludedPatternsRedacted =
            [
                ".git",
                "node_modules",
                "bin",
                "obj",
                ".next",
                "dist",
                "build",
                "vendor",
                "dependency folders"
            ],
            MaxFileCount = 1000,
            MaxFileSizeBytes = 1_048_576,
            BinaryFilePolicyRedacted = "Binary files blocked until separate policy.",
            CredentialDetectionPolicyRedacted = "Credential-like value detection policy required before scan eligibility.",
            RedactionPolicyRedacted = "Redaction must run before display, export, evidence report, or future provider use.",
            SymlinkPolicyRedacted = "Symlink following disabled until reviewed policy exists.",
            CaseSensitivityPolicyRedacted = "Case sensitivity must be declared per platform before scan.",
            AuditEvidenceRequirementsRedacted = ["scan preview evidence ref", "consent placeholder ref", "path jail ref"],
            WorkspaceValidated = true,
            PathJailValidated = pathJail is not null,
            ExplicitConsentRecorded = false,
            PreviewBeforeScanRequired = true,
            CancelStopRequired = true,
            NoMutationGuaranteed = true,
            NoCloudUpload = true,
            NoLlmCall = true,
            NoEmbeddingsUntilSeparatePolicy = true,
            AllowsSymlinkFollowing = false,
            PerformsRealScan = false,
            ListsFilesystem = false,
            ReadsFiles = false,
            HashesFiles = false,
            UsesGit = false,
            MutatesFilesystem = false,
            CanAuthorizeExecution = false,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public NodalOsContextToLlmGovernanceDraft CreateContextToLlmGovernanceDraft(
        NodalOsWorkspaceLocalModel workspace,
        NodalOsContextToLlmGovernanceState state,
        NodalOsSafeContextUsageTarget usageTarget = NodalOsSafeContextUsageTarget.FutureLlmPrompt)
    {
        return new()
        {
            GovernanceId = $"context-to-llm-governance-{workspace.WorkspaceId}-{state}",
            WorkspaceId = workspace.WorkspaceId,
            State = state,
            UsageTarget = usageTarget,
            PotentialFutureContextRedacted =
            [
                "public-safe summary",
                "user-provided safe context",
                "workspace metadata safe hints",
                "evidence refs",
                "timeline refs"
            ],
            ProhibitedContextRedacted =
            [
                "credential-like values",
                "raw payload",
                "sensitive context",
                "unknown sensitivity context",
                "raw evidence payload",
                "unredacted filesystem paths"
            ],
            RedactionRequirementRedacted = "Redaction required before any future provider context package.",
            UserConsentRequirementRedacted = "Explicit future user consent required.",
            ByokRequirementRedacted = "Future BYOK/provider configuration required; not implemented in this block.",
            BudgetPolicyRequirementRedacted = "Future budget and cost guardrails required.",
            PromptGovernanceRequirementRedacted = "Future prompt governance required before any prompt can exist.",
            EvidenceRequirementsRedacted = ["evidence refs required", "timeline registration required"],
            ProvenanceConfidenceFreshnessRequirementsRedacted = ["provenance label required", "confidence label required", "freshness label required"],
            HumanReviewRequirementsRedacted = HumanReviewFor(state),
            TimelineRegistrationRequirementsRedacted = ["future provider-use intent must be timeline-registered before execution eligibility"],
            BlockReasonsRedacted = BlockReasonsFor(state),
            RequiresRedaction = true,
            RequiresUserConsent = true,
            RequiresFutureByok = true,
            RequiresPromptGovernance = true,
            RequiresBudgetGuardrails = true,
            RequiresHumanReview = state is NodalOsContextToLlmGovernanceState.RequiresHumanReview
                or NodalOsContextToLlmGovernanceState.UnknownRequiresReview
                or NodalOsContextToLlmGovernanceState.EligibleForFutureLlmWithConsent,
            CreatesPrompt = false,
            CallsLlmProvider = false,
            SendsNetworkData = false,
            CallsCloud = false,
            CanAuthorizeExecution = false,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    private static IReadOnlyList<string> HumanReviewFor(NodalOsContextToLlmGovernanceState state) => state switch
    {
        NodalOsContextToLlmGovernanceState.AllowedForDisplayOnly => ["Display-only context remains reviewable in Mission Control."],
        NodalOsContextToLlmGovernanceState.AllowedForExportOnly => ["Export-only context requires redaction review."],
        _ => ["Human review required before any future provider usage."]
    };

    private static IReadOnlyList<string> BlockReasonsFor(NodalOsContextToLlmGovernanceState state) => state switch
    {
        NodalOsContextToLlmGovernanceState.NotAllowed => ["Future provider usage is not allowed."],
        NodalOsContextToLlmGovernanceState.BlockedUntilByokConfigured => ["BYOK/provider configuration missing."],
        NodalOsContextToLlmGovernanceState.BlockedUntilPromptPolicyDefined => ["Prompt governance missing."],
        NodalOsContextToLlmGovernanceState.BlockedUntilBudgetPolicyDefined => ["Budget guardrails missing."],
        NodalOsContextToLlmGovernanceState.BlockedBySecret => ["Credential-like context blocked."],
        NodalOsContextToLlmGovernanceState.BlockedByRawPayload => ["Raw payload blocked."],
        NodalOsContextToLlmGovernanceState.BlockedBySensitiveContext => ["Sensitive context blocked."],
        NodalOsContextToLlmGovernanceState.UnknownRequiresReview => ["Unknown context requires review."],
        _ => []
    };
}

public sealed class NodalOsProjectUnderstandingPolicyJsonSerializer
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
        WriteIndented = true
    };

    public string SerializeRealScanPreconditions(NodalOsRealScanPreconditions preconditions) => JsonSerializer.Serialize(preconditions, Options);

    public string SerializeContextToLlmGovernance(NodalOsContextToLlmGovernanceDraft governance) => JsonSerializer.Serialize(governance, Options);
}

public static class NodalOsProjectUnderstandingPolicyFixtures
{
    public static NodalOsRealScanPreconditions PreviewOnlyPreconditions() =>
        new NodalOsProjectUnderstandingPolicyService().CreateRealScanPreconditions(
            NodalOsWorkspaceFixtures.ActiveReadOnlyWorkspace(),
            NodalOsWorkspaceFixtures.PathJailBinding());

    public static NodalOsContextToLlmGovernanceDraft FutureLlmGovernance() =>
        new NodalOsProjectUnderstandingPolicyService().CreateContextToLlmGovernanceDraft(
            NodalOsWorkspaceFixtures.ActiveReadOnlyWorkspace(),
            NodalOsContextToLlmGovernanceState.EligibleForFutureLlmWithConsent);
}
