using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Cli.Diagnostics;
using OneBrain.Core.Contracts;

namespace OneBrain.Recipes.Tests;

[TestClass]
public sealed class MetricsBaselineFailureKindTests
{
    [TestMethod]
    public void MetricsSnapshot_Serializes_And_Deserializes_Stably()
    {
        var snapshot = new MetricsSnapshot
        {
            SnapshotDurationMs = 42,
            NodeCount = 128,
            CacheUsed = true,
            Provenance = Provenance.Fixture,
            CapturedAtUtc = new DateTimeOffset(2026, 6, 12, 12, 0, 0, TimeSpan.Zero)
        };

        var json = JsonSerializer.Serialize(snapshot);
        var roundtrip = JsonSerializer.Deserialize<MetricsSnapshot>(json);

        Assert.IsTrue(json.Contains("\"SchemaVersion\":1", StringComparison.Ordinal));
        Assert.IsNotNull(roundtrip);
        Assert.AreEqual(snapshot.SchemaVersion, roundtrip.SchemaVersion);
        Assert.AreEqual(snapshot.SnapshotDurationMs, roundtrip.SnapshotDurationMs);
        Assert.AreEqual(snapshot.NodeCount, roundtrip.NodeCount);
        Assert.AreEqual(snapshot.CacheUsed, roundtrip.CacheUsed);
        Assert.AreEqual(snapshot.Provenance, roundtrip.Provenance);
        Assert.AreEqual(snapshot.CapturedAtUtc, roundtrip.CapturedAtUtc);
    }

    [TestMethod]
    public void FailureKindMapper_Maps_Historical_Messages()
    {
        Assert.AreEqual(FailureKind.NotFound, FailureKindMapper.FromMessage("target not found"));
        Assert.AreEqual(FailureKind.NotFound, FailureKindMapper.FromMessage("proceso no encontrado"));
        Assert.AreEqual(FailureKind.Timeout, FailureKindMapper.FromMessage("operation timed out"));
        Assert.AreEqual(FailureKind.PolicyDenied, FailureKindMapper.FromMessage("blocked by safety policy"));
        Assert.AreEqual(FailureKind.Cancelled, FailureKindMapper.FromMessage("usuario cancelado"));
        Assert.AreEqual(FailureKind.Ambiguous, FailureKindMapper.FromMessage("selector ambiguo"));
    }

    [TestMethod]
    public void FailureKindMapper_Maps_Null_And_Empty_To_Unverified()
    {
        Assert.AreEqual(FailureKind.Unverified, FailureKindMapper.FromMessage(null));
        Assert.AreEqual(FailureKind.Unverified, FailureKindMapper.FromMessage(""));
        Assert.AreEqual(FailureKind.Unverified, FailureKindMapper.FromMessage("   "));
    }

    [TestMethod]
    public void BaselineDiagnosticRunner_Summarize_Computes_Stable_Metrics()
    {
        var capturedAt = new DateTimeOffset(2026, 6, 12, 18, 0, 0, TimeSpan.Zero);
        var snapshots = new[]
        {
            new MetricsSnapshot { SnapshotDurationMs = 10, NodeCount = 100, CacheUsed = true, Provenance = Provenance.Uia, CapturedAtUtc = capturedAt },
            new MetricsSnapshot { SnapshotDurationMs = 20, NodeCount = 140, CacheUsed = true, Provenance = Provenance.Uia, CapturedAtUtc = capturedAt.AddSeconds(1) },
            new MetricsSnapshot { SnapshotDurationMs = 30, NodeCount = 160, CacheUsed = true, Provenance = Provenance.Uia, CapturedAtUtc = capturedAt.AddSeconds(2) },
            new MetricsSnapshot { SnapshotDurationMs = 40, NodeCount = 200, CacheUsed = true, Provenance = Provenance.Uia, CapturedAtUtc = capturedAt.AddSeconds(3) }
        };

        var result = BaselineDiagnosticRunner.Summarize("notepad", 4, snapshots);

        Assert.AreEqual("ok", result.Status);
        Assert.AreEqual(4, result.IterationsRequested);
        Assert.AreEqual(4, result.IterationsCompleted);
        Assert.AreEqual(20, result.P50SnapshotDurationMs);
        Assert.AreEqual(40, result.P95SnapshotDurationMs);
        Assert.AreEqual(10, result.MinSnapshotDurationMs);
        Assert.AreEqual(40, result.MaxSnapshotDurationMs);
        Assert.AreEqual(150d, result.AverageNodeCount);
        Assert.AreEqual(capturedAt.AddSeconds(3), result.CapturedAtUtc);
    }

    [TestMethod]
    public void BaselineDiagnosticRunner_Process_Not_Found_Returns_NotFound()
    {
        var result = BaselineDiagnosticRunner.Run("process-that-does-not-exist-onebrain", 3);

        Assert.AreEqual("process-not-found", result.Status);
        Assert.AreEqual(FailureKind.NotFound, result.FailureKind);
        Assert.AreEqual(0, result.IterationsCompleted);
    }

    [TestMethod]
    public void BaselineDiagnosticRunner_CliSerialization_Uses_String_FailureKind()
    {
        var result = BaselineDiagnosticRunner.Run("process-that-does-not-exist-onebrain", 2);

        var json = BaselineDiagnosticRunner.SerializeForCli(result);

        StringAssert.Contains(json, "\"FailureKind\": \"NotFound\"");
        Assert.IsFalse(json.Contains("\"FailureKind\": 1", StringComparison.Ordinal));
    }
}
