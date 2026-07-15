using OneBrain.AgentOperations.Contracts;
using OneBrain.Core.Runtime;

namespace OneBrain.AgentOperations.Core.Runtime;

public static class NodalOsMissionPlanProjector
{
    private static readonly string[] KnownCapabilityPrefixes =
    [
        "reasoning.",
        "coding.",
        "filesystem.",
        "desktop.",
        "browser.",
        "terminal.",
        "model.",
        "evidence.",
        "verification."
    ];

    public static MissionPlan Project(
        NodalOsTaskGraphDraft taskGraph,
        string goal,
        int version = 1)
    {
        ArgumentNullException.ThrowIfNull(taskGraph);
        ArgumentException.ThrowIfNullOrWhiteSpace(goal);
        if (version < 1)
            throw new ArgumentOutOfRangeException(nameof(version));

        var projectedTasks = taskGraph.Tasks
            .Where(task => task.TaskKind != NodalOsAssignmentTaskKind.FutureExecutionPlaceholder)
            .ToArray();
        if (projectedTasks.Length == 0)
            throw new InvalidOperationException("The task graph has no safe planning steps to project.");

        var projectedIds = projectedTasks.Select(task => task.TaskId).ToHashSet(StringComparer.Ordinal);
        var steps = projectedTasks.Select(task => new MissionStep(
            Id: SafeRuntimeText.Sanitize(task.TaskId, 120),
            ParentId: null,
            Intent: SafeRuntimeText.Sanitize(task.TitleRedacted, 240),
            ExecutionSurface: ResolveSurface(task),
            AllowedCapabilities: ResolveCapabilities(task),
            ExpectedEvidence: ResolveExpectedEvidence(task),
            RiskLevel: task.RiskLevel switch
            {
                NodalOsAssignmentRiskLevel.Low => MissionRiskLevel.Low,
                NodalOsAssignmentRiskLevel.Medium => MissionRiskLevel.Medium,
                _ => MissionRiskLevel.High
            },
            ApprovalRequired: task.RequiresApproval || task.RiskLevel is NodalOsAssignmentRiskLevel.High or NodalOsAssignmentRiskLevel.UnknownRequiresReview,
            Dependencies: task.DependencyIds
                .Where(projectedIds.Contains)
                .Select(value => SafeRuntimeText.Sanitize(value, 120))
                .Distinct(StringComparer.Ordinal)
                .ToArray(),
            Status: task.Status switch
            {
                NodalOsAssignmentTaskStatus.Blocked => MissionStepStatus.Blocked,
                NodalOsAssignmentTaskStatus.ArchivedMock => MissionStepStatus.Skipped,
                _ => MissionStepStatus.Pending
            },
            Attempts: 0,
            LastFailure: task.Status == NodalOsAssignmentTaskStatus.Blocked
                ? SafeRuntimeText.Sanitize(task.BlockedByIds.FirstOrDefault(), 240)
                : null,
            // TaskGraph evidence documents planning provenance. Runtime evidence starts empty
            // and is promoted only after a capability result and verification.
            EvidenceRefs: Array.Empty<string>())).ToArray();

        return new MissionPlan(
            MissionId: SafeRuntimeText.Sanitize(taskGraph.MissionId, 120),
            Version: version,
            CreatedAt: taskGraph.CreatedAt == default ? DateTimeOffset.UtcNow : taskGraph.CreatedAt,
            Goal: SafeRuntimeText.Sanitize(goal, 500),
            Steps: steps,
            Status: MissionStatus.Active);
    }

    private static IReadOnlyList<string> ResolveCapabilities(NodalOsAssignmentTaskDraft task)
    {
        var capabilities = task.AllowedCapabilitiesRedacted
            .Select(value => SafeRuntimeText.Sanitize(value, 120).ToLowerInvariant())
            .Where(value => KnownCapabilityPrefixes.Any(prefix => value.StartsWith(prefix, StringComparison.Ordinal)))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        return capabilities.Length == 0 ? ["reasoning.plan"] : capabilities;
    }

    private static IReadOnlyList<MissionExpectedEvidence> ResolveExpectedEvidence(NodalOsAssignmentTaskDraft task)
    {
        var expected = task.EvidenceRefs
            .Select(reference => new MissionExpectedEvidence(
                SafeRuntimeText.Sanitize(reference.Kind, 80),
                SafeRuntimeText.Sanitize(reference.EvidenceId, 160)))
            .ToArray();
        return expected.Length == 0
            ? [new MissionExpectedEvidence("verification", "Trusted verification result for the projected step")]
            : expected;
    }

    private static MissionExecutionSurface ResolveSurface(NodalOsAssignmentTaskDraft task)
    {
        var capabilities = task.AllowedCapabilitiesRedacted;
        if (capabilities.Any(value => value.StartsWith("filesystem.", StringComparison.OrdinalIgnoreCase)))
            return MissionExecutionSurface.Filesystem;
        if (capabilities.Any(value => value.StartsWith("browser.", StringComparison.OrdinalIgnoreCase)))
            return MissionExecutionSurface.BrowserDom;
        if (capabilities.Any(value => value.StartsWith("desktop.", StringComparison.OrdinalIgnoreCase)))
            return MissionExecutionSurface.Desktop;
        if (capabilities.Any(value => value.StartsWith("terminal.", StringComparison.OrdinalIgnoreCase)))
            return MissionExecutionSurface.Terminal;
        return task.TaskKind == NodalOsAssignmentTaskKind.DocumentationDraft
            ? MissionExecutionSurface.Coding
            : MissionExecutionSurface.Reasoning;
    }
}
