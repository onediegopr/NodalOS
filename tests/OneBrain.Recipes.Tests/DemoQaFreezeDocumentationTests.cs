using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OneBrain.Recipes.Tests;

[TestClass]
public sealed class DemoQaFreezeDocumentationTests
{
    [TestMethod]
    public void Qa_Freeze_Checklist_Exists()
    {
        Assert.IsTrue(File.Exists(GetRootPath("docs/demo/demo-qa-freeze-checklist.md")));
    }

    [TestMethod]
    public void Qa_Freeze_Checklist_Mentions_Frozen_Commit()
    {
        var checklist = ReadDoc("docs/demo/demo-qa-freeze-checklist.md");

        StringAssert.Contains(checklist, "5916dec");
    }

    [TestMethod]
    public void Qa_Freeze_Checklist_Mentions_Expected_Test_Count()
    {
        var checklist = ReadDoc("docs/demo/demo-qa-freeze-checklist.md");

        StringAssert.Contains(checklist, "179/179 PASS");
    }

    [TestMethod]
    public void Qa_Freeze_Checklist_Mentions_Negative_Exit_Code()
    {
        var checklist = ReadDoc("docs/demo/demo-qa-freeze-checklist.md");

        StringAssert.Contains(checklist, "NEGATIVE_EXIT_CODE=1");
    }

    [TestMethod]
    public void Qa_Freeze_Checklist_Mentions_Latest_Demo_Markdown()
    {
        var checklist = ReadDoc("docs/demo/demo-qa-freeze-checklist.md");

        StringAssert.Contains(checklist, "LATEST_DEMO_MARKDOWN");
    }

    [TestMethod]
    public void Public_Storyline_Exists()
    {
        Assert.IsTrue(File.Exists(GetRootPath("docs/demo/public-storyline.md")));
    }

    [TestMethod]
    public void Public_Storyline_Contains_One_Line()
    {
        var storyline = ReadDoc("docs/demo/public-storyline.md");

        StringAssert.Contains(storyline, "ONE BRAIN turns safe local automation runs into auditable evidence reports.");
    }

    [TestMethod]
    public void Public_Storyline_Contains_Safe_Claims()
    {
        var storyline = ReadDoc("docs/demo/public-storyline.md");

        StringAssert.Contains(storyline, "read-only evidence");
        StringAssert.Contains(storyline, "auditable local report");
        StringAssert.Contains(storyline, "explicit confidence/readiness");
        StringAssert.Contains(storyline, "human-review friendly");
    }

    [TestMethod]
    public void Public_Storyline_Contains_Anti_Claims()
    {
        var storyline = ReadDoc("docs/demo/public-storyline.md");

        StringAssert.Contains(storyline, "## Anti-claims");
        StringAssert.Contains(storyline, "No usar estas frases como promesas:");
    }

    [TestMethod]
    public void Public_Storyline_Contains_Prohibited_Claims_Only_In_Anti_Claims_Section()
    {
        var storyline = ReadDoc("docs/demo/public-storyline.md");
        var beforeSection = storyline.Split("## Anti-claims", StringSplitOptions.None)[0];

        AssertProhibitedClaimIsNotInMainCopy(beforeSection, "compra automaticamente");
        AssertProhibitedClaimIsNotInMainCopy(beforeSection, "evita todos los bloqueos");
        AssertProhibitedClaimIsNotInMainCopy(beforeSection, "extrae todos los precios");
        AssertProhibitedClaimIsNotInMainCopy(beforeSection, "100% autonomo");
        AssertProhibitedClaimIsNotInMainCopy(beforeSection, "bypass");
        AssertProhibitedClaimIsNotInMainCopy(beforeSection, "garantiza precio");
    }

    private static void AssertProhibitedClaimIsNotInMainCopy(string text, string claim)
    {
        Assert.IsFalse(text.Contains(claim, StringComparison.OrdinalIgnoreCase), $"Prohibited claim found outside anti-claims section: {claim}");
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
