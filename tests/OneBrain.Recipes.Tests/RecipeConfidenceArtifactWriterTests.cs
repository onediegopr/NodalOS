using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Approval;
using OneBrain.Core.Confidence;

namespace OneBrain.Recipes.Tests;

[TestClass]
public sealed class RecipeConfidenceArtifactWriterTests
{
    [TestMethod]
    public void Write_Creates_File_Under_Artifacts_Recipe_Confidence()
    {
        var temp = CreateTempDir();
        var profile = BusinessFlowDemoFixture.CreateConfidenceProfile();

        var result = RecipeConfidenceArtifactWriter.Write(temp, profile);

        Assert.IsTrue(result.Success, result.Error);
        Assert.IsTrue(File.Exists(result.Path));
        StringAssert.Contains(result.RelativePath.Replace('\\', '/'), "artifacts/recipe-confidence/");
        StringAssert.Contains(File.ReadAllText(result.Path), RecipeConfidenceArtifactWriter.SchemaVersion);
    }

    [TestMethod]
    public void Writer_Uses_Unique_Path_When_File_Exists()
    {
        var temp = CreateTempDir();
        var profile = BusinessFlowDemoFixture.CreateConfidenceProfile();

        var first = RecipeConfidenceArtifactWriter.Write(temp, profile);
        var second = RecipeConfidenceArtifactWriter.Write(temp, profile);

        Assert.IsTrue(first.Success);
        Assert.IsTrue(second.Success);
        Assert.AreNotEqual(first.Path, second.Path);
    }

    [TestMethod]
    public void Confidence_Artifact_Contains_No_Executable_Action()
    {
        var temp = CreateTempDir();
        var result = RecipeConfidenceArtifactWriter.Write(temp, BusinessFlowDemoFixture.CreateConfidenceProfile());
        var json = File.ReadAllText(result.Path);

        StringAssert.Contains(json, "blocked");
        Assert.IsFalse(json.Contains("safe.click", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(json.Contains("submit", StringComparison.OrdinalIgnoreCase));
    }

    private static string CreateTempDir()
    {
        var path = Path.Combine(Path.GetTempPath(), "onebrain-confidence-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}
