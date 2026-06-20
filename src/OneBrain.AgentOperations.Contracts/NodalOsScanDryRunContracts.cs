namespace OneBrain.AgentOperations.Contracts;

public enum NodalOsScanDryRunEventPreviewKind
{
    WouldStart,
    WouldValidatePathJail,
    WouldApplyConsentScope,
    WouldApplySecretPolicy,
    WouldApplyExclusions,
    WouldEmitEvidenceRef,
    WouldEmitTimelineRef,
    WouldStopBlocked
}

public sealed record NodalOsScanDryRunRequest
{
    public required string DryRunId { get; init; }

    public required string WorkspaceRef { get; init; }

    public required string MissionRef { get; init; }

    public required string PathJailPreconditionsRef { get; init; }

    public required string ConsentScopePreviewRef { get; init; }

    public required string SecretDetectionPolicyPreviewRef { get; init; }

    public required string ExclusionPolicyPackRef { get; init; }

    public required bool IsDryRunOnly { get; init; }

    public required bool UsesRealFilesystem { get; init; }

    public required bool PerformsDirectoryListing { get; init; }

    public required bool PerformsFileRead { get; init; }

    public required bool PerformsFileHash { get; init; }

    public required bool PerformsIndexing { get; init; }

    public required bool PerformsVectorization { get; init; }

    public required bool BuildsLlmContext { get; init; }
}

public sealed record NodalOsScanDryRunEventPreview
{
    public required string EventPreviewId { get; init; }

    public required NodalOsScanDryRunEventPreviewKind Kind { get; init; }

    public required string SummaryRedacted { get; init; }

    public required bool EmitsToRealEventBus { get; init; }

    public required bool ProductivePersistenceUsed { get; init; }
}

public sealed record NodalOsScanDryRunResult
{
    public required string DryRunResultId { get; init; }

    public required string DryRunRef { get; init; }

    public required string RealScanAuditGateRef { get; init; }

    public required bool GateStillBlocked { get; init; }

    public required bool ReadyForRealDryRun { get; init; }

    public required bool ReadyForRealScan { get; init; }

    public required bool ReadyForFileRead { get; init; }

    public required bool ReadyForIndexing { get; init; }

    public required bool ReadyForVectorization { get; init; }

    public required bool ReadyForLlmContext { get; init; }

    public required string EstimatedOnlyScopeSummaryRedacted { get; init; }

    public IReadOnlyList<string> BlockedCapabilitiesRedacted { get; init; } = [];

    public IReadOnlyList<string> RequiredNextGatesRedacted { get; init; } = [];

    public required string UserFacingExplanationRedacted { get; init; }

    public IReadOnlyList<string> EvidenceRefs { get; init; } = [];

    public IReadOnlyList<string> TimelineRefs { get; init; } = [];

    public IReadOnlyList<string> GuardrailRefs { get; init; } = [];

    public IReadOnlyList<NodalOsScanDryRunEventPreview> EventsPreview { get; init; } = [];
}
