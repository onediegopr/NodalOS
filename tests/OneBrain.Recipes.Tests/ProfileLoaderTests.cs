using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Profiles;

namespace OneBrain.Recipes.Tests;

[TestClass]
public sealed class ProfileLoaderTests
{
    private readonly ProfileLoader _loader = new();

    [TestMethod]
    public void Loads_ExampleCom_Profile()
    {
        var path = GetRootPath("tools/profiles/web/example-com.json");
        var result = _loader.Load(path);
        Assert.IsTrue(result.Success, result.Error ?? "unknown error");
        Assert.IsNotNull(result.Profile);
        Assert.AreEqual("example-com", result.Profile!.Id);
        Assert.AreEqual("web", result.Profile.Type);
    }

    [TestMethod]
    public void Loads_Wikipedia_Profile()
    {
        var path = GetRootPath("tools/profiles/web/wikipedia-automation.json");
        var result = _loader.Load(path);
        Assert.IsTrue(result.Success, result.Error ?? "unknown error");
        Assert.AreEqual("wikipedia-automation", result.Profile!.Id);
    }

    [TestMethod]
    public void Fails_On_Missing_File()
    {
        var result = _loader.Load("tools/profiles/web/__nonexistent__.json");
        Assert.IsFalse(result.Success);
        StringAssert.Contains(result.Error, "not found");
    }

    [TestMethod]
    public void Fails_On_Invalid_Profile()
    {
        var tmpPath = Path.Combine(Path.GetTempPath(), "test-invalid-profile.json");
        File.WriteAllText(tmpPath, "{\"id\":\"\",\"type\":\"invalid\"}");
        try
        {
            var result = _loader.Load(tmpPath);
            Assert.IsFalse(result.Success);
        }
        finally
        {
            File.Delete(tmpPath);
        }
    }

    [TestMethod]
    public void ToVariables_Produces_Correct_Prefix()
    {
        var path = GetRootPath("tools/profiles/web/example-com.json");
        var result = _loader.Load(path);
        Assert.IsTrue(result.Success);
        var vars = _loader.ToVariables(result.Profile!);
        Assert.AreEqual("example-com", vars["profile.example-com.id"]);
        Assert.AreEqual("https://example.com", vars["profile.example-com.url"]);
    }

    [TestMethod]
    public void ToVariables_With_Prefix_Uses_Provided_Prefix()
    {
        var path = GetRootPath("tools/profiles/web/example-com.json");
        var result = _loader.Load(path);
        Assert.IsTrue(result.Success);
        var vars = _loader.ToVariables(result.Profile!, "profile.wiki");
        Assert.AreEqual("example-com", vars["profile.wiki.id"]);
        Assert.AreEqual("https://example.com", vars["profile.wiki.url"]);
        Assert.AreEqual("Example", vars["profile.wiki.expected.titleContains"]);
    }

    [TestMethod]
    public void ToVariables_Without_Prefix_FallsBack_To_ProfileId()
    {
        var path = GetRootPath("tools/profiles/web/example-com.json");
        var result = _loader.Load(path);
        var vars = _loader.ToVariables(result.Profile!);
        Assert.AreEqual("https://example.com", vars["profile.example-com.url"]);
    }

    private static string GetRootPath(string relative)
    {
        // Tests run from bin/Debug/netXX-windows. Navigate up 4 levels to solution root.
        var baseDir = AppContext.BaseDirectory;
        var root = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", ".."));
        return Path.Combine(root, relative);
    }
}
