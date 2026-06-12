using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Memory;

namespace OneBrain.Recipes.Tests;

[TestClass]
public sealed class ProcessMemoryStoreTests
{
    [TestMethod]
    public void Write_And_Read_Process_Memory_Under_Artifacts()
    {
        var temp = CreateTempDir();
        var entry = ProcessMemoryDemoFixture.CreateDemoReportEntry();

        var result = ProcessMemoryStore.Write(temp, entry);
        var entries = ProcessMemoryStore.ReadAll(temp);

        Assert.IsTrue(result.Success, result.Error);
        StringAssert.Contains(result.RelativePath.Replace('\\', '/'), "artifacts/process-memory/");
        StringAssert.Contains(File.ReadAllText(result.Path), ProcessMemoryStore.SchemaVersion);
        Assert.AreEqual(1, entries.Count);
        Assert.AreEqual(entry.Id, entries[0].Id);
    }

    [TestMethod]
    public void Write_Fails_Closed_On_Secret_Like_Content()
    {
        var temp = CreateTempDir();
        var secret = "sk-" + "test-memory-secret-1234567890";
        var entry = ProcessMemoryDemoFixture.CreateDemoReportEntry() with
        {
            Notes = [$"provider key {secret}"]
        };

        var result = ProcessMemoryStore.Write(temp, entry);

        Assert.IsFalse(result.Success);
        StringAssert.Contains(result.Error, "secret-like");
    }

    [TestMethod]
    public void Write_Stores_Artifact_Paths_Relative_And_Safe()
    {
        var temp = CreateTempDir();
        var absolute = Path.Combine(temp, "artifacts", "process-memory", "demo.json");
        var entry = ProcessMemoryDemoFixture.CreateDemoReportEntry() with
        {
            ArtifactPaths = [absolute],
            EvidenceLinks = [new ProcessMemoryEvidenceLink("artifact", absolute, "demo")]
        };

        var result = ProcessMemoryStore.Write(temp, entry);
        var read = ProcessMemoryStore.ReadAll(temp).Single();

        Assert.IsTrue(result.Success, result.Error);
        Assert.AreEqual("artifacts/process-memory/demo.json", read.ArtifactPaths.Single());
        Assert.AreEqual("artifacts/process-memory/demo.json", read.EvidenceLinks.Single().RelativePath);
        Assert.IsFalse(File.ReadAllText(result.Path).Contains("C:\\Users\\", StringComparison.OrdinalIgnoreCase));
    }

    private static string CreateTempDir()
    {
        var path = Path.Combine(Path.GetTempPath(), "onebrain-process-memory-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}
