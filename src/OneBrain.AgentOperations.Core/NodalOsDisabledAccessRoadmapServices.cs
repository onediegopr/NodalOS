using System.Text.Json;
using System.Text.Json.Serialization;
using OneBrain.AgentOperations.Contracts;

namespace OneBrain.AgentOperations.Core;

public sealed class NodalOsDisabledAccessRoadmapService
{
    public NodalOsDisabledAccessRoadmap CreateRoadmap(
        NodalOsAuditCheckpointReview checkpoint,
        NodalOsProductiveConsentDesignDraft design) =>
        new()
        {
            RoadmapId = "disabled-access-roadmap-m572",
            GovernanceBaselineRef = checkpoint.GovernanceBaselineRef,
            AuditCheckpointRef = checkpoint.CheckpointId,
            ProductiveConsentDesignRef = design.DesignId,
            RoadmapStatus = NodalOsDisabledAccessRoadmapStatus.DesignRoadmapReady,
            IsRoadmapOnly = true,
            CanAuthorizeImplementation = false,
            CanEnableRealAccess = false,
            Phases = Enum.GetValues<NodalOsDisabledAccessPhaseKind>().Select((phase, index) => new NodalOsDisabledAccessRoadmapPhase
            {
                PhaseId = $"disabled-access-roadmap-phase-{index + 1:000}-{phase}",
                PhaseKind = phase,
                DescriptionRedacted = $"{phase} remains disabled by default.",
                DisabledByDefault = true,
                UsesRealFilesystem = false,
                EnablesRealAccess = false,
                RequiresFutureAudit = true
            }).ToArray(),
            Blockers = Enum.GetValues<NodalOsDisabledAccessRoadmapBlockerKind>(),
            Decision = new()
            {
                DecisionId = "disabled-access-roadmap-decision-m572",
                ReadyForNextGovernedDesignPhase = true,
                ReadyForRealImplementation = false,
                ReadyForFilesystemAccess = false,
                ReadyForRealScan = false,
                ReadyForLlmContext = false,
                RecommendedNextMilestoneRedacted = "Productive consent design review."
            }
        };
}

public sealed class NodalOsDisabledAccessRoadmapJsonSerializer
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
        WriteIndented = true
    };

    public string Serialize(NodalOsDisabledAccessRoadmap roadmap) => JsonSerializer.Serialize(roadmap, Options);
}
