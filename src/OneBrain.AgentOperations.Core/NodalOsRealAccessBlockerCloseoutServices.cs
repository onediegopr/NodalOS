using System.Text.Json;
using System.Text.Json.Serialization;
using OneBrain.AgentOperations.Contracts;

namespace OneBrain.AgentOperations.Core;

public sealed class NodalOsRealAccessBlockerCloseoutService
{
    public NodalOsRealAccessBlockerCloseout CreateCloseout(
        NodalOsConsentLedgerUiPreview ledgerPreview,
        NodalOsCapabilityAuditChecklist checklist) =>
        new()
        {
            CloseoutId = "real-access-blocker-closeout-m569",
            LedgerUiPreviewRef = ledgerPreview.PreviewId,
            CapabilityAuditChecklistRef = checklist.ChecklistId,
            FailClosedAcceptancePackRef = "fail-closed-acceptance-pack-m566",
            OperationalAccessAuditAdrRef = "operational-access-audit-adr-m559",
            RealScanReadinessAdrRef = "real-scan-readiness-adr-m554",
            BlockerStatus = NodalOsRealAccessBlockerStatus.GovernanceBaselineClosed,
            ClosedAsGovernanceBaseline = true,
            RealAccessStillBlocked = true,
            Blockers = Enum.GetValues<NodalOsRealAccessBlockerCategory>().Select((category, index) => new NodalOsRealAccessBlocker
            {
                BlockerId = $"real-access-blocker-{index + 1:000}-{category}",
                Category = category,
                BlocksRealAccess = true,
                UserFacingExplanationRedacted = $"{category} remains unresolved before operational use.",
                EvidenceRef = $"evidence-ref-real-access-blocker-{index + 1:000}",
                TimelineRef = $"timeline-ref-real-access-blocker-{index + 1:000}"
            }).ToArray(),
            Decision = CreateDecision()
        };

    private static NodalOsRealAccessBlockerCloseoutDecision CreateDecision() =>
        new()
        {
            DecisionId = "real-access-blocker-closeout-decision-m569",
            GovernanceBaselineReady = true,
            RealAccessStillBlocked = true,
            ReadyForRealFilesystemAccess = false,
            ReadyForRealScan = false,
            ReadyForRealPathJail = false,
            ReadyForDirectoryListing = false,
            ReadyForFileRead = false,
            ReadyForFileHash = false,
            ReadyForIndexing = false,
            ReadyForRepresentationBuild = false,
            ReadyForLlmContext = false,
            ReadyForCloud = false,
            ReadyForRuntime = false,
            RecommendedNextPhaseRedacted = "Audit checkpoint before any operational implementation."
        };
}

public sealed class NodalOsRealAccessBlockerCloseoutJsonSerializer
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
        WriteIndented = true
    };

    public string Serialize(NodalOsRealAccessBlockerCloseout closeout) => JsonSerializer.Serialize(closeout, Options);
}
