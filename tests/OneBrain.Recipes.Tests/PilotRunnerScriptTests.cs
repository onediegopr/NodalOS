using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OneBrain.Recipes.Tests;

[TestClass]
public sealed class PilotRunnerScriptTests
{
    [TestMethod]
    public void Pilot_Runner_Script_Exists()
    {
        Assert.IsTrue(File.Exists(GetScriptPath()));
    }

    [TestMethod]
    public void Pilot_Runner_Script_Uses_Portable_Dotnet_By_Default()
    {
        var script = ReadScript();

        StringAssert.Contains(script, "dotnet-sdk-11.0.100-preview.5.26302.115-win-x64");
        StringAssert.Contains(script, "dotnet.exe");
        StringAssert.Contains(script, "& $Dotnet run --project src/OneBrain.Pilot");
    }

    [TestMethod]
    public void Pilot_Runner_Script_Prints_Url_And_Does_Not_Open_Browser()
    {
        var script = ReadScript();

        StringAssert.Contains(script, "ONE_BRAIN_PILOT_URL");
        StringAssert.Contains(script, "No browser will be opened automatically.");
        Assert.IsFalse(script.Contains("Start-Process", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(script.Contains("Invoke-Item", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(script.Contains("explorer.exe", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void Pilot_Runner_Script_Validates_Repository()
    {
        var script = ReadScript();

        StringAssert.Contains(script, "onediegopr/OneBrain");
        StringAssert.Contains(script, "git remote get-url origin");
    }

    private static string ReadScript()
    {
        return File.ReadAllText(GetScriptPath());
    }

    private static string GetScriptPath()
    {
        var baseDir = AppContext.BaseDirectory;
        var root = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", ".."));
        return Path.Combine(root, "tools", "scripts", "run-onebrain-pilot.ps1");
    }
}
