using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OneBrain.Recipes.Tests;

[TestClass]
public sealed class ProductEvidenceHtmlDemoDocumentationTests
{
    [TestMethod]
    public void Demo_Docs_Mention_Html_Report_And_No_Auto_Open()
    {
        var root = GetRepoRoot();
        var demoReadme = File.ReadAllText(Path.Combine(root, "demo", "README.md"));
        var demoDoc = File.ReadAllText(Path.Combine(root, "docs", "demo", "product-evidence-demo.md"));
        var pack = File.ReadAllText(Path.Combine(root, "demo", "product-evidence-demo-pack.md"));

        var combined = (demoReadme + "\n" + demoDoc + "\n" + pack).ToLowerInvariant();

        StringAssert.Contains(combined, "html");
        StringAssert.Contains(combined, "artifacts/product-evidence-demo-html-reports");
        StringAssert.Contains(combined, "samples/product-evidence-html");
        StringAssert.Contains(combined, "no se abre navegador");
    }

    [TestMethod]
    public void Release_And_Handoff_Mention_Html_Output_And_Snapshot_Fixture()
    {
        var root = GetRepoRoot();
        var release = File.ReadAllText(Path.Combine(root, "docs", "releases", "demo-snapshot-current.md"));
        var handoff = File.ReadAllText(Path.Combine(root, "docs", "handoffs", "one-brain-release-demo-handoff.md"));

        var combined = (release + "\n" + handoff).ToLowerInvariant();

        StringAssert.Contains(combined, "html");
        StringAssert.Contains(combined, "samples/product-evidence-html");
        StringAssert.Contains(combined, "artifacts/product-evidence-demo-html-reports");
        StringAssert.Contains(combined, "no se abre");
    }

    [TestMethod]
    public void Docs_Do_Not_Use_Forbidden_Claims_As_Positive_Promises()
    {
        var root = GetRepoRoot();
        var files = new[]
        {
            Path.Combine(root, "demo", "README.md"),
            Path.Combine(root, "demo", "product-evidence-demo-pack.md"),
            Path.Combine(root, "docs", "demo", "product-evidence-demo.md"),
            Path.Combine(root, "docs", "releases", "demo-snapshot-current.md"),
            Path.Combine(root, "docs", "handoffs", "one-brain-release-demo-handoff.md")
        };

        foreach (var file in files)
        {
            var text = File.ReadAllText(file).ToLowerInvariant();
            AssertClaimOnlyAppearsAsLimit(text, "compra automaticamente", file);
            AssertClaimOnlyAppearsAsLimit(text, "evita todos los bloqueos", file);
            AssertClaimOnlyAppearsAsLimit(text, "extrae todos los precios", file);
            AssertClaimOnlyAppearsAsLimit(text, "100% autonomo", file);
            AssertClaimOnlyAppearsAsLimit(text, "bypass", file);
            AssertClaimOnlyAppearsAsLimit(text, "garantiza precio", file);
        }
    }

    private static void AssertClaimOnlyAppearsAsLimit(string text, string claim, string file)
    {
        var index = text.IndexOf(claim, StringComparison.Ordinal);
        if (index < 0)
            return;

        var contextStart = Math.Max(0, index - 160);
        var contextLength = Math.Min(text.Length - contextStart, claim.Length + 320);
        var context = text.Substring(contextStart, contextLength);
        var isTechnicalBypass = claim.Equals("bypass", StringComparison.Ordinal) &&
            context.Contains("executionpolicy bypass", StringComparison.Ordinal);
        var isLimited = isTechnicalBypass ||
            context.Contains("no ", StringComparison.Ordinal) ||
            context.Contains("sin ", StringComparison.Ordinal) ||
            context.Contains("limite", StringComparison.Ordinal) ||
            context.Contains("anti-claim", StringComparison.Ordinal) ||
            context.Contains("claims prohibidos", StringComparison.Ordinal) ||
            context.Contains("no se deben vender", StringComparison.Ordinal);

        Assert.IsTrue(isLimited, $"Forbidden claim '{claim}' appears outside a limitation context in {file}.");
    }

    private static string GetRepoRoot()
    {
        var current = AppContext.BaseDirectory;
        while (!string.IsNullOrWhiteSpace(current))
        {
            if (File.Exists(Path.Combine(current, "OneBrain.slnx")))
                return current;

            var parent = Directory.GetParent(current);
            if (parent == null)
                break;
            current = parent.FullName;
        }

        throw new DirectoryNotFoundException("Could not find repository root.");
    }
}
