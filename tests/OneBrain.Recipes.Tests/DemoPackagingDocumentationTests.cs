using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OneBrain.Recipes.Tests;

[TestClass]
public sealed class DemoPackagingDocumentationTests
{
    [TestMethod]
    public void Demo_Pack_Readme_Exists()
    {
        Assert.IsTrue(File.Exists(GetRootPath("demo/README.md")));
    }

    [TestMethod]
    public void Demo_Pack_Readme_Mentions_One_Command_Script()
    {
        var readme = ReadDoc("demo/README.md");

        StringAssert.Contains(readme, "tools/scripts/run-demo-product-evidence.ps1");
    }

    [TestMethod]
    public void Demo_Pack_Readme_Mentions_Latest_Demo_Markdown()
    {
        var readme = ReadDoc("demo/README.md");

        StringAssert.Contains(readme, "LATEST_DEMO_MARKDOWN");
    }

    [TestMethod]
    public void Demo_Pack_Readme_Mentions_Samples_And_Artifacts()
    {
        var readme = ReadDoc("demo/README.md");

        StringAssert.Contains(readme, "samples/product-evidence/");
        StringAssert.Contains(readme, "artifacts/");
    }

    [TestMethod]
    public void Release_Snapshot_Exists()
    {
        Assert.IsTrue(File.Exists(GetRootPath("docs/releases/demo-snapshot-current.md")));
    }

    [TestMethod]
    public void Release_Snapshot_Mentions_Base_Commit()
    {
        var snapshot = ReadDoc("docs/releases/demo-snapshot-current.md");

        StringAssert.Contains(snapshot, "428127a");
    }

    [TestMethod]
    public void Release_Snapshot_Contains_Safety_Guarantees()
    {
        var snapshot = ReadDoc("docs/releases/demo-snapshot-current.md");

        StringAssert.Contains(snapshot, "No clicks comerciales.");
        StringAssert.Contains(snapshot, "No login.");
        StringAssert.Contains(snapshot, "No cookies accepted.");
        StringAssert.Contains(snapshot, "No carrito.");
        StringAssert.Contains(snapshot, "No compra.");
        StringAssert.Contains(snapshot, "No pago.");
    }

    [TestMethod]
    public void Release_Snapshot_Mentions_No_Checkout()
    {
        var snapshot = ReadDoc("docs/releases/demo-snapshot-current.md");

        StringAssert.Contains(snapshot, "No checkout.");
    }

    [TestMethod]
    public void Demo_Pack_Documents_Runtime_Artifacts_As_Ignored()
    {
        var pack = ReadDoc("demo/product-evidence-demo-pack.md");

        StringAssert.Contains(pack, "artifacts/");
        StringAssert.Contains(pack, "ignorado por Git");
    }

    [TestMethod]
    public void Release_Snapshot_Contains_Prohibited_Claims_Only_In_Limits_Section()
    {
        var snapshot = ReadDoc("docs/releases/demo-snapshot-current.md");
        var beforeSection = snapshot.Split("## Claims y limites que no se deben vender como promesas", StringSplitOptions.None)[0];

        AssertProhibitedClaimIsNotInMainCopy(beforeSection, "compra automaticamente");
        AssertProhibitedClaimIsNotInMainCopy(beforeSection, "evita todos los bloqueos");
        AssertProhibitedClaimIsNotInMainCopy(beforeSection, "extrae todos los precios");
        AssertProhibitedClaimIsNotInMainCopy(beforeSection, "100% autonomo sin supervision");
        AssertProhibitedClaimIsNotInMainCopyExcept(beforeSection, "bypass", "ExecutionPolicy Bypass");
        AssertProhibitedClaimIsNotInMainCopy(beforeSection, "garantiza precio");
    }

    private static void AssertProhibitedClaimIsNotInMainCopy(string text, string claim)
    {
        Assert.IsFalse(text.Contains(claim, StringComparison.OrdinalIgnoreCase), $"Prohibited claim found outside limits section: {claim}");
    }

    private static void AssertProhibitedClaimIsNotInMainCopyExcept(string text, string claim, string allowedContext)
    {
        var lines = text.Split(["\r\n", "\n"], StringSplitOptions.None);
        foreach (var line in lines)
        {
            if (line.Contains(claim, StringComparison.OrdinalIgnoreCase) &&
                !line.Contains(allowedContext, StringComparison.OrdinalIgnoreCase))
            {
                Assert.Fail($"Prohibited claim found outside limits section: {claim}");
            }
        }
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
