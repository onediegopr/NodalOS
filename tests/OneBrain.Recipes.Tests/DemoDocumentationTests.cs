using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OneBrain.Recipes.Tests;

[TestClass]
public sealed class DemoDocumentationTests
{
    [TestMethod]
    public void Demo_Readme_Mentions_One_Command_Script()
    {
        var readme = ReadDoc("docs/demo/product-evidence-demo.md");

        StringAssert.Contains(readme, "tools/scripts/run-demo-product-evidence.ps1");
        StringAssert.Contains(readme, "powershell -ExecutionPolicy Bypass -File");
    }

    [TestMethod]
    public void Demo_Readme_Mentions_Latest_Demo_Markdown()
    {
        var readme = ReadDoc("docs/demo/product-evidence-demo.md");

        StringAssert.Contains(readme, "LATEST_DEMO_MARKDOWN");
        StringAssert.Contains(readme, "artifacts/product-evidence-demo-reports/");
    }

    [TestMethod]
    public void Demo_Readme_Documents_Safety_Guarantees()
    {
        var readme = ReadDoc("docs/demo/product-evidence-demo.md");

        StringAssert.Contains(readme, "no browser");
        StringAssert.Contains(readme, "no web");
        StringAssert.Contains(readme, "no clicks");
        StringAssert.Contains(readme, "no login");
        StringAssert.Contains(readme, "no cookies accepted");
        StringAssert.Contains(readme, "no carrito");
        StringAssert.Contains(readme, "no compra");
        StringAssert.Contains(readme, "no pago");
    }

    [TestMethod]
    public void Demo_Readme_Documents_Samples_And_Artifacts()
    {
        var readme = ReadDoc("docs/demo/product-evidence-demo.md");

        StringAssert.Contains(readme, "samples/product-evidence/");
        StringAssert.Contains(readme, "artifacts/");
        StringAssert.Contains(readme, "Estos archivos no se commitean.");
    }

    [TestMethod]
    public void Walkthrough_Mentions_Safe_Phrases()
    {
        var walkthrough = ReadDoc("docs/demo/product-evidence-demo-walkthrough.md");

        StringAssert.Contains(walkthrough, "read-only evidence");
        StringAssert.Contains(walkthrough, "auditable local report");
        StringAssert.Contains(walkthrough, "explicit missing fields");
        StringAssert.Contains(walkthrough, "decision readiness");
        StringAssert.Contains(walkthrough, "human-review friendly");
    }

    [TestMethod]
    public void Walkthrough_Contains_Prohibited_Claims_Only_In_Prohibited_Section()
    {
        var walkthrough = ReadDoc("docs/demo/product-evidence-demo-walkthrough.md");
        var beforeSection = walkthrough.Split("## Frases prohibidas", StringSplitOptions.None)[0];

        AssertProhibitedClaimIsNotInMainCopy(beforeSection, "compra automaticamente");
        AssertProhibitedClaimIsNotInMainCopy(beforeSection, "evita todos los bloqueos");
        AssertProhibitedClaimIsNotInMainCopy(beforeSection, "extrae todos los precios");
        AssertProhibitedClaimIsNotInMainCopy(beforeSection, "100% autonomo sin supervision");

        StringAssert.Contains(walkthrough, "No usar estas frases como claims:");
    }

    [TestMethod]
    public void Demo_Docs_Do_Not_Claim_Live_Extraction()
    {
        var readme = ReadDoc("docs/demo/product-evidence-demo.md");
        var walkthrough = ReadDoc("docs/demo/product-evidence-demo-walkthrough.md");

        StringAssert.Contains(readme, "No es extraccion live");
        StringAssert.Contains(walkthrough, "La demo estable no representa todos los casos reales.");
    }

    private static void AssertProhibitedClaimIsNotInMainCopy(string text, string claim)
    {
        Assert.IsFalse(text.Contains(claim, StringComparison.OrdinalIgnoreCase), $"Prohibited claim found outside prohibited section: {claim}");
    }

    private static string ReadDoc(string relativePath)
    {
        return File.ReadAllText(GetRootPath(relativePath));
    }

    private static string GetRootPath(string relative)
    {
        var baseDir = AppContext.BaseDirectory;
        var root = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", ".."));
        return Path.Combine(root, relative);
    }
}
