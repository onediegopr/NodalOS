namespace OneBrain.Core.Contracts;

public sealed record MetricsSnapshot
{
    public int SchemaVersion { get; init; } = 1;
    public long SnapshotDurationMs { get; init; }
    public int NodeCount { get; init; }
    public bool CacheUsed { get; init; }
    public Provenance Provenance { get; init; }
    public DateTimeOffset CapturedAtUtc { get; init; }
}
