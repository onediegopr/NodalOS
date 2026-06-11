using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OneBrain.Recipes.Tests;

[TestClass]
public sealed class DemoRunnerScriptTests
{
    [TestMethod]
    public void Demo_Runner_Script_Exists()
    {
        Assert.IsTrue(File.Exists(GetScriptPath()));
    }

    [TestMethod]
    public void Demo_Runner_Script_References_Demo_Recipe()
    {
        var script = ReadScript();

        StringAssert.Contains(script, "tools/recipes/demo-product-evidence-report.json");
    }

    [TestMethod]
    public void Demo_Runner_Script_Validates_LastExitCode()
    {
        var script = ReadScript();

        StringAssert.Contains(script, "$LASTEXITCODE");
        StringAssert.Contains(script, "Demo recipe failed with exit code");
    }

    [TestMethod]
    public void Demo_Runner_Script_Defaults_To_Portable_Dotnet()
    {
        var script = ReadScript();

        StringAssert.Contains(script, "dotnet-sdk-11.0.100-preview.5.26302.115-win-x64");
        StringAssert.Contains(script, "dotnet.exe");
    }

    [TestMethod]
    public void Demo_Runner_Script_Does_Not_Use_Global_Dotnet_Command()
    {
        var script = ReadScript();

        Assert.IsFalse(script.Contains("& dotnet ", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(script.Contains(" dotnet run ", StringComparison.OrdinalIgnoreCase));
        StringAssert.Contains(script, "& $Dotnet run");
    }

    [TestMethod]
    public void Demo_Runner_Script_Does_Not_Open_Browser_Or_File()
    {
        var script = ReadScript();

        Assert.IsFalse(script.Contains("Start-Process", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(script.Contains("Invoke-Item", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(script.Contains("explorer.exe", StringComparison.OrdinalIgnoreCase));
    }

    private static string ReadScript()
    {
        return File.ReadAllText(GetScriptPath());
    }

    private static string GetScriptPath()
    {
        var baseDir = AppContext.BaseDirectory;
        var root = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", ".."));
        return Path.Combine(root, "tools", "scripts", "run-demo-product-evidence.ps1");
    }
}
