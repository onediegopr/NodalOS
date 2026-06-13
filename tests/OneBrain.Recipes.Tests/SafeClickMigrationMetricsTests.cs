using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Cli.Recipes;
using OneBrain.Cli.Safety;
using OneBrain.Core.Recipes;
using OneBrain.Core.Safety;

namespace OneBrain.Recipes.Tests;

[TestClass]
public sealed class SafeClickMigrationMetricsTests
{
    [TestMethod]
    public void MigrationReportDoesNotChangeExecution()
    {
        var baseline = BuildDesktopLegacyRecipe(includeDesktopObserve: false);
        var withDesktopObserve = BuildDesktopLegacyRecipe(includeDesktopObserve: true);

        var baselineResult = new RecipeRunner().Run(baseline);
        var observeResult = RunWithDesktopObserveOverride(
            (_, _, _) => CreateStrongDesktopObservation(),
            () => new RecipeRunner().Run(withDesktopObserve));

        Assert.AreEqual(baselineResult.Success, observeResult.Success);
        Assert.AreEqual(baselineResult.Variables!["safeClick.result"], observeResult.Variables!["safeClick.result"]);
        Assert.AreEqual(baselineResult.Variables["safeClick.reason"], observeResult.Variables["safeClick.reason"]);
    }

    [TestMethod]
    public void DesktopIdentityFeedsMigrationMetrics()
    {
        var result = RunWithDesktopObserveOverride(
            (_, _, _) => CreateStrongDesktopObservation(),
            () => new RecipeRunner().Run(BuildDesktopLegacyRecipe(includeDesktopObserve: true)));

        Assert.AreEqual("1", result.Variables!["safeClick.migration.total"]);
        Assert.AreEqual("1", result.Variables["safeClick.migration.desktopUiaObservable"]);
        Assert.AreEqual("1", result.Variables["safeClick.migration.desktopUiaStrong"]);
        Assert.AreEqual("0", result.Variables["safeClick.migration.desktopUiaWeak"]);
        Assert.AreEqual("1", result.Variables["safeClick.migration.notEligibleForFsm"]);
    }

    [TestMethod]
    public void MigrationMetricsVariablesAreWritten()
    {
        var result = new RecipeRunner().Run(BuildDesktopLegacyRecipe(includeDesktopObserve: false));

        Assert.IsTrue(result.Variables!.ContainsKey("safeClick.migration.total"));
        Assert.IsTrue(result.Variables.ContainsKey("safeClick.migration.blockingReasons"));
        Assert.IsTrue(result.Variables.ContainsKey("safeClick.migration.readinessPercent"));
        Assert.IsTrue(result.Variables.ContainsKey("safeClick.migration.summary"));
    }

    private static RecipeDefinition BuildDesktopLegacyRecipe(bool includeDesktopObserve)
    {
        var preflight = ClickPreflightEvaluator.Evaluate("Categorias");
        var steps = new List<RecipeStepDefinition>();

        if (includeDesktopObserve)
        {
            steps.Add(new RecipeStepDefinition
            {
                Id = "observe",
                Kind = "target.observe.desktop",
                SaveAs = "clickPreflight",
                Args = new Dictionary<string, string>
                {
                    ["targetText"] = "Categorias",
                    ["processName"] = "explorer",
                    ["window"] = "ONE Brain"
                }
            });
        }

        steps.Add(new RecipeStepDefinition
        {
            Id = "approval",
            Kind = "approval.manifest",
            SaveAs = "approval",
            Args = new Dictionary<string, string>
            {
                ["from"] = "clickPreflight",
                ["mode"] = "controlled"
            }
        });

        steps.Add(new RecipeStepDefinition
        {
            Id = "safe-click",
            Kind = "safe.click",
            SaveAs = "safeClick",
            Args = new Dictionary<string, string>
            {
                ["targettext"] = "Categorias",
                ["mode"] = "controlled",
                ["approvalprefix"] = "approval",
                ["proc"] = "process-that-does-not-exist-onebrain"
            }
        });

        return new RecipeDefinition("desktop-legacy-migration")
        {
            Variables = new Dictionary<string, string>
            {
                ["clickPreflight.evidenceJson"] = preflight.EvidenceJson ?? ""
            },
            Steps = steps
        };
    }

    private static T RunWithDesktopObserveOverride<T>(
        Func<string, string?, string?, DesktopTargetObservationResult> resolver,
        Func<T> action)
    {
        var field = typeof(RecipeRunner).GetField("s_targetObserveDesktopOverride", BindingFlags.Static | BindingFlags.NonPublic);
        Assert.IsNotNull(field);

        var previous = field.GetValue(null);
        field.SetValue(null, resolver);
        try
        {
            return action();
        }
        finally
        {
            field.SetValue(null, previous);
        }
    }

    private static DesktopTargetObservationResult CreateStrongDesktopObservation()
    {
        return new DesktopTargetObservationResult
        {
            Found = true,
            CandidateCount = 1,
            Reason = "exact desktop observe match",
            SelectedName = "Categorias",
            SelectedControlType = "Button",
            SelectedBoundingRect = "10,10,120,24",
            SelectedRuntimeId = "42.7.9",
            SelectedAutomationId = "categories-button",
            SelectedClassName = "Button",
            SelectedFrameworkId = "WPF",
            SelectedAncestorPath = "Window:ONE Brain > Pane:Catalog",
            SelectedProcessName = "explorer",
            SelectedWindowTitle = "ONE Brain",
            SelectedHelpTextPresent = true,
            SelectedLegacyNamePresent = true,
            HasInvoke = true
        };
    }
}
