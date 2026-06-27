using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Recipes;

namespace OneBrain.Recipes.Tests;

[TestClass]
[TestCategory("ReliableRecipeFinalAuditHardening")]
public sealed class ReliableRecipeFinalAuditHardeningTests
{
    private static readonly string[] ReliableRecipeSourceFiles =
    [
        "src/OneBrain.Core/Recipes/ReliableRecipeFoundationContracts.cs",
        "src/OneBrain.Core/Recipes/ReliableRecipeQualityPreflightContracts.cs",
        "src/OneBrain.Core/Recipes/ReliableRecipeLabViewModels.cs",
        "src/OneBrain.Core/Recipes/ReliableRecipeRecorderDraftContracts.cs",
        "src/OneBrain.Core/Recipes/ReliableRecipeEvalHarnessFixtureContracts.cs",
        "src/OneBrain.Core/Recipes/ReliableRecipeSandboxReadinessReports.cs",
        "src/OneBrain.Core/Recipes/ReliableRecipePerceptionIntegrationReports.cs",
        "src/OneBrain.Core/Recipes/ReliableRecipeDryRunAdapterReadiness.cs",
        "src/OneBrain.Core/Recipes/ReliableRecipeStructuredPrerequisites.cs",
        "src/OneBrain.Core/Recipes/ReliableRecipeStructuredPrerequisiteAuthoring.cs",
        "src/OneBrain.Core/Recipes/ReliableRecipeOperatorReviewPacks.cs",
        "src/OneBrain.Core/Recipes/ReliableRecipeNoRuntimeCloseoutReports.cs",
        "src/OneBrain.Core/Recipes/ReliableRecipeLabAuditSurface.cs"
    ];

    [TestMethod]
    public void M1M13ReliableRecipeSourceFilesContainNoExecutableRuntimePrimitives()
    {
        var forbidden = new Dictionary<string, Regex>
        {
            ["DllImport"] = new(@"\[DllImport", RegexOptions.Compiled),
            ["ProcessStart"] = new(@"\bProcess\.Start\s*\(", RegexOptions.Compiled),
            ["ProcessStartInfo"] = new(@"\bProcessStartInfo\b", RegexOptions.Compiled),
            ["HttpClient"] = new(@"\bHttpClient\b", RegexOptions.Compiled),
            ["WebSocket"] = new(@"\bWebSocket\b", RegexOptions.Compiled),
            ["TcpClient"] = new(@"\bTcpClient\b", RegexOptions.Compiled),
            ["UdpClient"] = new(@"\bUdpClient\b", RegexOptions.Compiled),
            ["Socket"] = new(@"\bSocket\b", RegexOptions.Compiled),
            ["SetWindowsHookEx"] = new(@"\bSetWindowsHookEx\s*\(", RegexOptions.Compiled),
            ["SendInput"] = new(@"\bSendInput\s*\(", RegexOptions.Compiled),
            ["SetCursorPos"] = new(@"\bSetCursorPos\s*\(", RegexOptions.Compiled),
            ["ClipboardApi"] = new(@"\bClipboard\.", RegexOptions.Compiled),
            ["CopyFromScreen"] = new(@"\bGraphics\.CopyFromScreen\s*\(", RegexOptions.Compiled),
            ["FileSystemWatcher"] = new(@"\bFileSystemWatcher\b", RegexOptions.Compiled),
            ["EnvironmentSecretRead"] = new(@"\bEnvironment\.GetEnvironmentVariable\s*\(", RegexOptions.Compiled),
            ["FileWrite"] = new(@"\bFile\.(Write|WriteAllText|WriteAllBytes|Create|Delete|Move|Copy)\b", RegexOptions.Compiled),
            ["DirectoryMutation"] = new(@"\bDirectory\.(CreateDirectory|Delete|Move)\b", RegexOptions.Compiled)
        };

        var hits = new List<string>();
        foreach (var relativePath in ReliableRecipeSourceFiles)
        {
            var text = File.ReadAllText(Path.Combine(RepoRoot(), relativePath.Replace('/', Path.DirectorySeparatorChar)));
            foreach (var (name, pattern) in forbidden)
            {
                if (pattern.IsMatch(text))
                    hits.Add($"{name}: {relativePath}");
            }
        }

        Assert.AreEqual(0, hits.Count, "M1-M13 Reliable Recipe source files must stay no-runtime: " + string.Join(", ", hits));
    }

    [TestMethod]
    public void M13PresenterReadOnlyActionLabelsContainNoLiveActions()
    {
        var surface = ReliableRecipeLabAuditSurfacePresenter.CreateDefault();
        var forbiddenLabels = new[]
        {
            "Run",
            "Execute",
            "Start adapter",
            "Launch browser",
            "Connect CDP",
            "Replay",
            "Record live",
            "Capture screen",
            "Enable runtime",
            "Approve runtime",
            "Production ready",
            "Automation ready",
            "Live validated"
        };

        foreach (var label in surface.ReadOnlyActionLabels)
        {
            foreach (var forbiddenLabel in forbiddenLabels)
                Assert.IsFalse(label.Contains(forbiddenLabel, StringComparison.OrdinalIgnoreCase), label);
        }

        Assert.IsFalse(surface.RuntimeActionExposed);
        Assert.IsFalse(surface.RuntimeEnabled);
        Assert.IsFalse(surface.AdapterRuntimeEnabled);
    }

    [TestMethod]
    public void M13TimelineLabelMatchesRowsAndRepresentsCloseoutAndPresenter()
    {
        var surface = ReliableRecipeLabAuditSurfacePresenter.CreateDefault();
        var timeline = surface.Sections.Single(s => s.Kind == ReliableRecipeLabAuditSectionKind.TimelinePreview);

        Assert.AreEqual("M1-M13", timeline.Eyebrow);
        Assert.AreEqual("13", timeline.Metrics.Single(m => m.Label == "Milestones").Value);
        CollectionAssert.AreEqual(Enumerable.Range(1, 13).Select(i => $"M{i}").ToArray(), surface.Timeline.Select(m => m.BlockId).ToArray());
        Assert.IsTrue(surface.Timeline.Single(m => m.BlockId == "M12").Summary.Contains("closeout", StringComparison.OrdinalIgnoreCase));
        Assert.IsTrue(surface.Timeline.Single(m => m.BlockId == "M13").Summary.Contains("presenter", StringComparison.OrdinalIgnoreCase));
        Assert.IsTrue(surface.Timeline.All(m => m.ReadOnly && !m.RuntimeEnabled));
    }

    [TestMethod]
    public void ExistingRuntimeScopesRemainExplicitlyScopedAsProtectedNotAbsent()
    {
        var docs = string.Join(
            "\n",
            File.ReadAllText(Path.Combine(RepoRoot(), "docs", "architecture", "automation", "read-only-recipe-lab-ui-audit-integration-v1.md")),
            File.ReadAllText(Path.Combine(RepoRoot(), "docs", "qa", "m13-read-only-recipe-lab-ui-audit-integration.md")));

        Assert.IsTrue(docs.Contains("M1-M13 did not add or enable runtime", StringComparison.Ordinal));
        Assert.IsTrue(docs.Contains("existing protected runtime scopes remain present and untouched", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(docs.Contains("the repo has no runtime", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(docs.Contains("no browser runtime exists", StringComparison.OrdinalIgnoreCase));
    }

    private static string RepoRoot()
    {
        var current = AppContext.BaseDirectory;
        while (!string.IsNullOrWhiteSpace(current))
        {
            if (File.Exists(Path.Combine(current, "OneBrain.slnx")))
                return current;

            current = Directory.GetParent(current)?.FullName;
        }

        Assert.Fail("Could not locate repo root.");
        return "";
    }
}
