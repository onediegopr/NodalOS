using System.Text.Json;
using System.Text.Json.Serialization;
using OneBrain.AgentOperations.Contracts;

namespace OneBrain.AgentOperations.Core;

public sealed class NodalOsRealScanReadinessAdrService
{
    public NodalOsRealScanReadinessAdrSummary CreateSummary() =>
        new()
        {
            AdrId = "real-scan-readiness-adr-m554",
            DecisionRecordPath = "docs/architecture/real-scan-readiness-after-synthetic-dry-run-adr.md",
            DecisionStatus = NodalOsRealScanReadinessDecisionStatus.RealScanNotReadySyntheticBaselineReady,
            RealScanReady = false,
            SyntheticBaselineReady = true,
            FuturePathJailPrototypeMayProceedIfDisabledByDefault = true,
            RealFilesystemAccessBlocked = true,
            DirectoryEnumerationBlocked = true,
            ContentAccessBlocked = true,
            ContentFingerprintingBlocked = true,
            IndexingBlocked = true,
            RepresentationBuildBlocked = true,
            LlmContextBlocked = true,
            CloudBlocked = true,
            ProviderBlocked = true,
            RuntimeBlocked = true,
            FixtureCoverageNecessaryNotSufficient = true,
            RequiredNextMilestonesRedacted =
            [
                "Disabled-by-default path jail prototype gate.",
                "Explicit consent enforcement gate.",
                "No-mutation and cancellation proof.",
                "Evidence and timeline emission audit.",
                "Sensitive-data and exclusion enforcement audit."
            ],
            AuditTriggersRedacted =
            [
                "Any operational workspace access marker.",
                "Any context build marker.",
                "Any provider, cloud, or runtime marker."
            ],
            RollbackDisableStrategyRedacted =
            [
                "Default disabled switch for future prototype.",
                "Audit failure blocks enablement.",
                "Consent withdrawal blocks operational behavior."
            ]
        };
}

public sealed class NodalOsRealScanReadinessAdrJsonSerializer
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
        WriteIndented = true
    };

    public string SerializeSummary(NodalOsRealScanReadinessAdrSummary summary) =>
        JsonSerializer.Serialize(summary, Options);
}

