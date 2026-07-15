using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.AgentOperations.Core.Capabilities;
using OneBrain.AgentOperations.Core.Runtime;
using OneBrain.Core.Models;
using OneBrain.Core.Runtime;

namespace OneBrain.Runtime.Tests;

[TestClass]
public sealed class RuntimeInspectorAndTelemetryTests
{
    [TestMethod]
    [TestCategory("AiriSelectiveRuntime")]
    public void InspectorIsDeveloperOnlyReadOnlyAndRedactsSecrets()
    {
        var plan = Plan();
        var runtime = Runtime(plan);
        runtime.Start("start");
        runtime.BeginStep("step-1", "begin");
        var state = runtime.State;
        var catalog = new ModelCatalogSnapshot(
            [new ModelProviderDefinition(
                "provider", "Provider", ModelProviderKind.Cloud, new Uri("https://example.invalid"),
                false, Array.Empty<SecretReference>(), ModelProviderState.Ready, 100,
                ModelPrivacyClass.AuthorizedCloud, new[] { "global" })],
            Array.Empty<ModelDefinition>(),
            Array.Empty<LogicalModelAlias>());
        var input = new RuntimeInspectorInput(
            true,
            plan,
            state,
            runtime.ResumeCard,
            [new CapabilityRecord(
                "filesystem.read", "core", null, CapabilityRuntime.Filesystem,
                CapabilityState.Ready, 100, "1", new Dictionary<string, string>())],
            catalog,
            "fast-default",
            "provider",
            "model",
            ["api_key=super-secret-value fallback applied"],
            new RuntimeInspectorBrowserStatus("CloakBrowser", "blocked-binary", 0, 0, "Bearer private-token-value", null),
            null,
            ["password=private-password-value"],
            1024);

        var hidden = RuntimeInspectorProjector.TryBuild(input with { DeveloperModeEnabled = false });
        var snapshot = RuntimeInspectorProjector.TryBuild(input);
        var text = string.Join("|", snapshot!.RecentFallbacks.Concat(snapshot.RecentErrors).Append(snapshot.Browser.LastError));

        Assert.IsNull(hidden);
        Assert.IsTrue(snapshot.LocalDevOnly);
        Assert.IsTrue(snapshot.ReadOnly);
        Assert.IsTrue(snapshot.SecretsExcluded);
        Assert.IsFalse(text.Contains("super-secret", StringComparison.Ordinal));
        Assert.IsFalse(text.Contains("private-token", StringComparison.Ordinal));
        Assert.IsFalse(text.Contains("private-password", StringComparison.Ordinal));
    }

    [TestMethod]
    [TestCategory("AiriSelectiveRuntime")]
    public async Task TelemetryFailureDoesNotBlockMissionWork()
    {
        var sink = new FailingSink();
        await using var observer = new BestEffortRuntimeSignalObserver(sink, capacity: 4);

        observer.TryObserve(RuntimeSignal.Create(
            "mission",
            "run_started",
            "corr",
            dimensions: [new KeyValuePair<string, string?>("token", "sk-fixture-5678")]));
        await sink.Called.Task.WaitAsync(TimeSpan.FromSeconds(2));

        Assert.IsTrue(sink.Attempts > 0);
    }

    [TestMethod]
    [TestCategory("AiriSelectiveRuntime")]
    public void RuntimeSignalSanitizesSecretDimensionsAtEntry()
    {
        var signal = RuntimeSignal.Create(
            "model",
            "attempt",
            "corr",
            dimensions:
            [
                new KeyValuePair<string, string?>("authorization", "Bearer abcdefghijklmnop"),
                new KeyValuePair<string, string?>("api_key", "sk-fixture-5678")
            ]);
        var projection = string.Join("|", signal.Dimensions.Values);

        Assert.IsFalse(projection.Contains("abcdefghijklmnop", StringComparison.Ordinal));
        Assert.IsFalse(projection.Contains("private-secret", StringComparison.Ordinal));
    }

    private static MissionPlan Plan() =>
        new(
            "mission",
            1,
            DateTimeOffset.UtcNow,
            "Inspect fixture",
            [new MissionStep(
                "step-1", null, "Read fixture", MissionExecutionSurface.Filesystem,
                ["filesystem.read"], [new MissionExpectedEvidence("snapshot", "fixture")],
                MissionRiskLevel.Low, false, Array.Empty<string>(), MissionStepStatus.Pending, 0, null,
                Array.Empty<string>())],
            MissionStatus.Active);

    private static LightweightMissionRuntime Runtime(MissionPlan plan) =>
        new(
            plan,
            new MissionAuthorizationScope(
                plan.MissionId,
                new HashSet<string>(["filesystem.read"], StringComparer.Ordinal),
                new HashSet<MissionExecutionSurface>([MissionExecutionSurface.Filesystem]),
                MissionRiskLevel.Medium),
            "run");

    private sealed class FailingSink : IRuntimeSignalSink
    {
        public TaskCompletionSource<bool> Called { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public int Attempts { get; private set; }

        public ValueTask WriteAsync(RuntimeSignal signal, CancellationToken cancellationToken)
        {
            Attempts++;
            Called.TrySetResult(true);
            throw new InvalidOperationException("Telemetry backend unavailable.");
        }
    }
}
