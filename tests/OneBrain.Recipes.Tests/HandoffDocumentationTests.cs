using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OneBrain.Recipes.Tests;

[TestClass]
public sealed class HandoffDocumentationTests
{
    [TestMethod]
    public void Handoff_Document_Exists()
    {
        Assert.IsTrue(File.Exists(GetRootPath("docs/handoffs/one-brain-release-demo-handoff.md")));
    }

    [TestMethod]
    public void Handoff_Mentions_Current_Commit()
    {
        var handoff = ReadHandoff();

        StringAssert.Contains(handoff, "02d90b0");
    }

    [TestMethod]
    public void Handoff_Mentions_Expected_Test_Count()
    {
        var handoff = ReadHandoff();

        StringAssert.Contains(handoff, "189/189 PASS");
    }

    [TestMethod]
    public void Handoff_Mentions_Negative_Exit_Code()
    {
        var handoff = ReadHandoff();

        StringAssert.Contains(handoff, "NEGATIVE_EXIT_CODE=1");
    }

    [TestMethod]
    public void Handoff_Mentions_Demo_Runner()
    {
        var handoff = ReadHandoff();

        StringAssert.Contains(handoff, "run-demo-product-evidence.ps1");
    }

    [TestMethod]
    public void Handoff_Mentions_Latest_Demo_Markdown()
    {
        var handoff = ReadHandoff();

        StringAssert.Contains(handoff, "LATEST_DEMO_MARKDOWN");
    }

    [TestMethod]
    public void Handoff_Mentions_Artifacts_Ignored()
    {
        var handoff = ReadHandoff();

        StringAssert.Contains(handoff, "artifacts/");
        StringAssert.Contains(handoff, "ignorado");
    }

    [TestMethod]
    public void Handoff_Mentions_Firefox_Quarantined()
    {
        var handoff = ReadHandoff();

        StringAssert.Contains(handoff, "Firefox fixture quarantined");
    }

    [TestMethod]
    public void Handoff_Mentions_Safety_Guarantees()
    {
        var handoff = ReadHandoff();

        StringAssert.Contains(handoff, "No commercial clicks.");
        StringAssert.Contains(handoff, "No login.");
        StringAssert.Contains(handoff, "No pago.");
    }

    [TestMethod]
    public void Handoff_Mentions_Next_Hitos()
    {
        var handoff = ReadHandoff();

        StringAssert.Contains(handoff, "HITO-079+080");
        StringAssert.Contains(handoff, "HITO-089+090");
    }

    [TestMethod]
    public void Handoff_Contains_Prohibited_Claims_Only_In_Limits_Section()
    {
        var handoff = ReadHandoff();
        var beforeSection = handoff.Split("## Claims prohibidos como promesas positivas", StringSplitOptions.None)[0];

        AssertProhibitedClaimIsNotInMainCopy(beforeSection, "compra automaticamente");
        AssertProhibitedClaimIsNotInMainCopy(beforeSection, "extrae todos los precios");
        AssertProhibitedClaimIsNotInMainCopy(beforeSection, "100% autonomo");
        AssertProhibitedClaimIsNotInMainCopy(beforeSection, "garantiza precio");
        AssertBypassIsOnlyTechnicalCommand(beforeSection);
    }

    private static void AssertProhibitedClaimIsNotInMainCopy(string text, string claim)
    {
        Assert.IsFalse(text.Contains(claim, StringComparison.OrdinalIgnoreCase), $"Prohibited claim found outside limits section: {claim}");
    }

    private static void AssertBypassIsOnlyTechnicalCommand(string text)
    {
        var lines = text.Split(["\r\n", "\n"], StringSplitOptions.None);
        foreach (var line in lines)
        {
            if (line.Contains("bypass", StringComparison.OrdinalIgnoreCase) &&
                !line.Contains("ExecutionPolicy Bypass", StringComparison.OrdinalIgnoreCase))
            {
                Assert.Fail("Prohibited claim found outside limits section: bypass");
            }
        }
    }

    private static string ReadHandoff()
    {
        return File.ReadAllText(GetRootPath("docs/handoffs/one-brain-release-demo-handoff.md"));
    }

    private static string GetRootPath(string relative)
    {
        var baseDir = AppContext.BaseDirectory;
        var root = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", ".."));
        return Path.Combine(root, relative);
    }
}
