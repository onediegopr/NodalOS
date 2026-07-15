using OneBrain.AgentOperations.Core.Capabilities;
using OneBrain.Core.Models;
using OneBrain.Core.Runtime;

namespace OneBrain.AgentOperations.Core.Runtime;

public sealed record RuntimeInspectorBrowserStatus(
    string Runtime,
    string State,
    int ReconnectCount,
    int ActionQueueDepth,
    string? LastError,
    TimeSpan? LastLatency);

public sealed record RuntimeInspectorInput(
    bool DeveloperModeEnabled,
    MissionPlan Plan,
    MissionRuntimeState Mission,
    MissionResumeCard ResumeCard,
    IReadOnlyList<CapabilityRecord> Capabilities,
    ModelCatalogSnapshot Models,
    string? LogicalModel,
    string? ActiveProvider,
    string? ActiveModel,
    IReadOnlyList<string> RecentFallbacks,
    RuntimeInspectorBrowserStatus Browser,
    TimeSpan? ApprovalWait,
    IReadOnlyList<string> RecentErrors,
    long ApproximateMemoryBytes);

public sealed record RuntimeInspectorSnapshot(
    string MissionId,
    string RunId,
    string Goal,
    string MissionStatus,
    string? CurrentStep,
    double Progress,
    IReadOnlyList<string> PlanSteps,
    IReadOnlyList<string> Capabilities,
    IReadOnlyList<string> Providers,
    string? LogicalModel,
    string? ActiveProvider,
    string? ActiveModel,
    IReadOnlyList<string> RecentFallbacks,
    RuntimeInspectorBrowserStatus Browser,
    TimeSpan? ApprovalWait,
    IReadOnlyList<string> RecentErrors,
    IReadOnlyList<string> EvidenceRefs,
    long ApproximateMemoryBytes,
    bool LocalDevOnly,
    bool ReadOnly,
    bool SecretsExcluded);

public static class RuntimeInspectorProjector
{
    public static RuntimeInspectorSnapshot? TryBuild(RuntimeInspectorInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (!input.DeveloperModeEnabled)
            return null;

        return new RuntimeInspectorSnapshot(
            MissionId: SafeRuntimeText.Sanitize(input.Mission.MissionId, 120),
            RunId: SafeRuntimeText.Sanitize(input.Mission.RunId, 120),
            Goal: SafeRuntimeText.Sanitize(input.Plan.Goal, 240),
            MissionStatus: input.Mission.Status.ToString(),
            CurrentStep: string.IsNullOrWhiteSpace(input.ResumeCard.CurrentStep)
                ? null
                : SafeRuntimeText.Sanitize(input.ResumeCard.CurrentStep, 240),
            Progress: input.Mission.Progress,
            PlanSteps: input.Plan.Steps
                .Select(step => $"{SafeRuntimeText.Sanitize(step.Id, 80)}:{step.Status}:{SafeRuntimeText.Sanitize(step.Intent, 160)}")
                .Take(20)
                .ToArray(),
            Capabilities: input.Capabilities
                .Select(record => $"{SafeRuntimeText.Sanitize(record.CapabilityId, 120)}:{record.State}:{record.HealthScore}")
                .Take(40)
                .ToArray(),
            Providers: input.Models.Providers
                .Select(provider => $"{SafeRuntimeText.Sanitize(provider.ProviderId, 120)}:{provider.State}:{provider.HealthScore}")
                .Take(20)
                .ToArray(),
            LogicalModel: Optional(input.LogicalModel, 120),
            ActiveProvider: Optional(input.ActiveProvider, 120),
            ActiveModel: Optional(input.ActiveModel, 120),
            RecentFallbacks: input.RecentFallbacks.Select(value => SafeRuntimeText.Sanitize(value, 240)).TakeLast(8).ToArray(),
            Browser: input.Browser with
            {
                Runtime = SafeRuntimeText.Sanitize(input.Browser.Runtime, 120),
                State = SafeRuntimeText.Sanitize(input.Browser.State, 80),
                LastError = Optional(input.Browser.LastError, 240)
            },
            ApprovalWait: input.ApprovalWait,
            RecentErrors: input.RecentErrors.Select(value => SafeRuntimeText.Sanitize(value, 240)).TakeLast(8).ToArray(),
            EvidenceRefs: input.Mission.EvidenceRefs.Select(value => SafeRuntimeText.Sanitize(value, 160)).TakeLast(12).ToArray(),
            ApproximateMemoryBytes: Math.Max(0, input.ApproximateMemoryBytes),
            LocalDevOnly: true,
            ReadOnly: true,
            SecretsExcluded: true);
    }

    private static string? Optional(string? value, int maximumLength)
    {
        var sanitized = SafeRuntimeText.Sanitize(value, maximumLength);
        return sanitized.Length == 0 ? null : sanitized;
    }
}
