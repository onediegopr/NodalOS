using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using OneBrain.Core.Contracts;
using OneBrain.Observation;
using OneBrain.Observation.Sessions;
using OneBrain.Observation.Uia;
using OneBrain.Observation.Windows;

namespace OneBrain.Cli.Diagnostics;

public sealed record BaselineDiagnosticResult
{
    public int SchemaVersion { get; init; } = 1;
    public string Process { get; init; } = "";
    public int IterationsRequested { get; init; }
    public int IterationsCompleted { get; init; }
    public long P50SnapshotDurationMs { get; init; }
    public long P95SnapshotDurationMs { get; init; }
    public long MinSnapshotDurationMs { get; init; }
    public long MaxSnapshotDurationMs { get; init; }
    public double AverageNodeCount { get; init; }
    public DateTimeOffset CapturedAtUtc { get; init; }
    public string Status { get; init; } = "failed";
    public FailureKind? FailureKind { get; init; }
    public string Message { get; init; } = "";
}

public static class BaselineDiagnosticRunner
{
    private static readonly JsonSerializerOptions CliJsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public static BaselineDiagnosticResult Run(string processName, int iterations)
    {
        var requestedIterations = iterations;
        var effectiveIterations = Math.Max(1, iterations);
        var finder = new WindowFinder();
        var hwnd = finder.FindWindow(processName, null);

        if (hwnd == IntPtr.Zero)
            return BuildTerminal(processName, requestedIterations, 0, "process-not-found", FailureKind.NotFound, "target process window not found");

        try
        {
            using var session = new PerceptionSession();
            var reader = new CognitiveSnapshotReader();
            var snapshots = new List<MetricsSnapshot>(effectiveIterations);

            for (var i = 0; i < effectiveIterations; i++)
            {
                var stopwatch = Stopwatch.StartNew();
                var snapshot = reader.ReadFromHwnd(session, hwnd, processName);
                stopwatch.Stop();

                if (snapshot is null)
                {
                    return BuildTerminal(
                        processName,
                        requestedIterations,
                        snapshots.Count,
                        "pending-manual",
                        FailureKind.SourceUnavailable,
                        "snapshot source unavailable for the selected process");
                }

                snapshots.Add(new MetricsSnapshot
                {
                    SnapshotDurationMs = stopwatch.ElapsedMilliseconds,
                    NodeCount = snapshot.Elements.Count,
                    CacheUsed = UiaSnapshotOptions.Default.UseCacheRequest,
                    Provenance = Provenance.Uia,
                    CapturedAtUtc = DateTimeOffset.UtcNow
                });
            }

            return Summarize(processName, requestedIterations, snapshots);
        }
        catch (Exception ex)
        {
            var mapped = FailureKindMapper.FromMessage(ex.Message);
            var failureKind = mapped == FailureKind.Unverified ? FailureKind.SourceUnavailable : mapped;
            var status = failureKind == FailureKind.SourceUnavailable ? "pending-manual" : "failed";
            return BuildTerminal(processName, requestedIterations, 0, status, failureKind, ex.Message);
        }
    }

    public static BaselineDiagnosticResult Summarize(
        string processName,
        int iterationsRequested,
        IReadOnlyList<MetricsSnapshot> snapshots)
    {
        if (snapshots.Count == 0)
            return BuildTerminal(processName, iterationsRequested, 0, "failed", FailureKind.Unverified, "no snapshots collected");

        var orderedDurations = snapshots.Select(snapshot => snapshot.SnapshotDurationMs).OrderBy(value => value).ToArray();

        return new BaselineDiagnosticResult
        {
            Process = processName,
            IterationsRequested = iterationsRequested,
            IterationsCompleted = snapshots.Count,
            P50SnapshotDurationMs = Percentile(orderedDurations, 0.50),
            P95SnapshotDurationMs = Percentile(orderedDurations, 0.95),
            MinSnapshotDurationMs = orderedDurations.First(),
            MaxSnapshotDurationMs = orderedDurations.Last(),
            AverageNodeCount = Math.Round(snapshots.Average(snapshot => snapshot.NodeCount), 2),
            CapturedAtUtc = snapshots.Last().CapturedAtUtc,
            Status = "ok",
            FailureKind = null,
            Message = ""
        };
    }

    public static string SerializeForCli(BaselineDiagnosticResult result)
    {
        return JsonSerializer.Serialize(result, CliJsonOptions);
    }

    private static BaselineDiagnosticResult BuildTerminal(
        string processName,
        int iterationsRequested,
        int iterationsCompleted,
        string status,
        FailureKind failureKind,
        string message)
    {
        return new BaselineDiagnosticResult
        {
            Process = processName,
            IterationsRequested = iterationsRequested,
            IterationsCompleted = iterationsCompleted,
            CapturedAtUtc = DateTimeOffset.UtcNow,
            Status = status,
            FailureKind = failureKind,
            Message = message
        };
    }

    private static long Percentile(IReadOnlyList<long> sortedDurations, double percentile)
    {
        if (sortedDurations.Count == 0)
            return 0;

        var rank = (int)Math.Ceiling(percentile * sortedDurations.Count) - 1;
        rank = Math.Clamp(rank, 0, sortedDurations.Count - 1);
        return sortedDurations[rank];
    }
}
