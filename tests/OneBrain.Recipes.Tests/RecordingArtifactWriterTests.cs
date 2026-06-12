using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Recording;

namespace OneBrain.Recipes.Tests;

[TestClass]
public sealed class RecordingArtifactWriterTests
{
    [TestMethod]
    public void WriteRecording_Creates_File_Under_Artifacts_Recordings()
    {
        var temp = CreateTempDir();
        var session = RecordingDemoFixture.CreateSession();

        var result = RecordingArtifactWriter.WriteRecording(temp, session);

        Assert.IsTrue(result.Success, result.Error);
        Assert.IsTrue(File.Exists(result.Path));
        StringAssert.Contains(result.RelativePath.Replace('\\', '/'), "artifacts/recordings/");
        StringAssert.Contains(File.ReadAllText(result.Path), RecordingArtifactWriter.RecordingSchemaVersion);
    }

    [TestMethod]
    public void WriteTimeline_Creates_File_Under_Artifacts_Recipe_Timelines()
    {
        var temp = CreateTempDir();
        var timeline = RecordingDemoFixture.CreateTimeline();

        var result = RecordingArtifactWriter.WriteTimeline(temp, timeline);

        Assert.IsTrue(result.Success, result.Error);
        Assert.IsTrue(File.Exists(result.Path));
        StringAssert.Contains(result.RelativePath.Replace('\\', '/'), "artifacts/recipe-timelines/");
        StringAssert.Contains(File.ReadAllText(result.Path), RecordingArtifactWriter.TimelineSchemaVersion);
    }

    [TestMethod]
    public void Writer_Uses_Unique_Path_When_File_Exists()
    {
        var temp = CreateTempDir();
        var session = RecordingDemoFixture.CreateSession();

        var first = RecordingArtifactWriter.WriteRecording(temp, session);
        var second = RecordingArtifactWriter.WriteRecording(temp, session);

        Assert.IsTrue(first.Success);
        Assert.IsTrue(second.Success);
        Assert.AreNotEqual(first.Path, second.Path);
    }

    [TestMethod]
    public void Recording_Artifact_Contains_No_Playback_Instruction()
    {
        var temp = CreateTempDir();
        var result = RecordingArtifactWriter.WriteTimeline(temp, RecordingDemoFixture.CreateTimeline());
        var json = File.ReadAllText(result.Path);

        StringAssert.Contains(json, "candidate timeline only");
        Assert.IsFalse(json.Contains("safe.click", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(json.Contains("checkout", StringComparison.OrdinalIgnoreCase));
    }

    private static string CreateTempDir()
    {
        var path = Path.Combine(Path.GetTempPath(), "onebrain-recording-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}
