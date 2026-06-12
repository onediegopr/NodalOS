using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.History;

namespace OneBrain.Recipes.Tests;

[TestClass]
public sealed class RunHistoryStoreTests
{
    [TestMethod]
    public void Write_And_Read_Run_History_Under_Artifacts()
    {
        var temp = CreateTempDir();
        var record = CreateRecord();

        var result = RunHistoryStore.Write(temp, record);
        var records = RunHistoryStore.ReadAll(temp);

        Assert.IsTrue(result.Success, result.Error);
        Assert.IsTrue(File.Exists(result.Path));
        StringAssert.Contains(result.RelativePath.Replace('\\', '/'), "artifacts/run-history/");
        StringAssert.Contains(File.ReadAllText(result.Path), RunHistoryStore.SchemaVersion);
        Assert.AreEqual(1, records.Count);
        Assert.AreEqual(record.RunId, records[0].RunId);
    }

    [TestMethod]
    public void Writer_Preserves_Safety_Counters()
    {
        var temp = CreateTempDir();
        var record = CreateRecord() with
        {
            SafetyCounters = new RunSafetyCounters(1, 2, 3, 4, 5, 6)
        };

        var result = RunHistoryStore.Write(temp, record);
        var read = RunHistoryStore.ReadAll(temp).Single();

        Assert.IsTrue(result.Success, result.Error);
        Assert.AreEqual(1, read.SafetyCounters.Clicks);
        Assert.AreEqual(2, read.SafetyCounters.CookiesAccepted);
        Assert.AreEqual(6, read.SafetyCounters.Payment);
    }

    [TestMethod]
    public void Writer_Stores_Artifact_Paths_Relative_And_Safe()
    {
        var temp = CreateTempDir();
        var absoluteArtifact = Path.Combine(temp, "artifacts", "reports", "demo.html");
        var record = CreateRecord() with
        {
            ArtifactPaths = [absoluteArtifact]
        };

        var result = RunHistoryStore.Write(temp, record);
        var read = RunHistoryStore.ReadAll(temp).Single();

        Assert.IsTrue(result.Success, result.Error);
        Assert.AreEqual("artifacts/reports/demo.html", read.ArtifactPaths.Single());
        Assert.IsFalse(File.ReadAllText(result.Path).Contains("C:\\Users\\", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void Writer_Fails_Closed_On_Secret_Like_Content()
    {
        var temp = CreateTempDir();
        var record = CreateRecord() with
        {
            Notes = ["provider key " + "sk-" + "test-secret-123456789 should not be stored"]
        };

        var result = RunHistoryStore.Write(temp, record);

        Assert.IsFalse(result.Success);
        StringAssert.Contains(result.Error, "secret-like");
    }

    private static RunHistoryRecord CreateRecord()
    {
        return new RunHistoryRecord(
            RunId: "run-123",
            StartedAtUtc: "2026-06-12T11:00:00Z",
            EndedAtUtc: "2026-06-12T11:00:01Z",
            Status: RunHistoryStatuses.Succeeded,
            Source: RunHistorySources.Pilot,
            RecipeId: "demo-product-evidence-report",
            CandidateFlowId: null,
            ApprovalRequestId: null,
            ApprovalDecisionId: null,
            RecordingSessionId: null,
            TimelineId: null,
            ConfidenceId: "confidence-1",
            AiRoutingDecisionId: "ai-audit-1",
            ExitCode: 0,
            SafetyCounters: RunSafetyCounters.Zero,
            ArtifactPaths: ["artifacts/product-evidence-demo-reports/demo.md"],
            ErrorSummary: "",
            Notes: ["no secrets stored"]);
    }

    private static string CreateTempDir()
    {
        var path = Path.Combine(Path.GetTempPath(), "onebrain-run-history-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}
