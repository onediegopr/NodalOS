using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Cli.Recipes;
using OneBrain.Core.Approval;
using OneBrain.Core.Recipes;

namespace OneBrain.Recipes.Tests;

[TestClass]
public sealed class LegacyQuarantineRecipeTests
{
    [TestMethod]
    public void ActvTypeBlockedByDefault()
    {
        var result = new RecipeRunner().Run(BuildSingleStepRecipe("actv.type", "legacyType"));

        Assert.IsFalse(result.Success);
        Assert.AreEqual("true", result.Variables!["legacy.blocked"]);
        Assert.AreEqual("actv.type", result.Variables["legacy.stepKind"]);
        Assert.AreEqual("actv.type", result.Variables["legacy.surface"]);
        Assert.AreEqual("false", result.Variables["legacy.guard.allowed"]);
        Assert.AreEqual("true", result.Variables["legacyType.legacyBlocked"]);
        StringAssert.Contains(result.Variables["legacy.reason"], "LegacyQuarantined");
    }

    [TestMethod]
    public void ActvInvokeBlockedByDefault()
    {
        var result = new RecipeRunner().Run(BuildSingleStepRecipe("actv.invoke", "legacyInvoke"));

        Assert.IsFalse(result.Success);
        Assert.AreEqual("true", result.Variables!["legacy.blocked"]);
        Assert.AreEqual("actv.invoke", result.Variables["legacy.stepKind"]);
        Assert.AreEqual("true", result.Variables["legacyInvoke.legacyBlocked"]);
    }

    [TestMethod]
    public void KeyBlockedByDefaultBeforeReadingTextOrSendingInput()
    {
        var result = new RecipeRunner().Run(BuildSingleStepRecipe("key", "legacyKey"));

        Assert.IsFalse(result.Success);
        Assert.AreEqual("true", result.Variables!["legacy.blocked"]);
        Assert.AreEqual("key", result.Variables["legacy.stepKind"]);
        Assert.AreEqual("true", result.Variables["legacyKey.legacyBlocked"]);
        Assert.AreEqual("PolicyDenied", result.Variables["legacyKey.failureKind"]);
    }

    [TestMethod]
    public void LegacyBlockedDoesNotReportSuccess()
    {
        var result = new RecipeRunner().Run(BuildSingleStepRecipe("actv.type", "legacyType"));

        Assert.IsFalse(result.Success);
        Assert.AreEqual("false", result.Variables!["legacy.success"]);
        Assert.AreEqual("false", result.Variables["legacyType.success"]);
        Assert.AreEqual("true", result.Variables["legacy.optInRequired"]);
    }

    [TestMethod]
    public void LegacyStepsClassifiedOrQuarantined()
    {
        foreach (var kind in new[] { "actv.type", "actv.invoke", "key" })
        {
            Assert.AreEqual(ActionSensitivity.Sensitive, SensitiveActionClassifier.ClassifyStepKind(kind), kind);
        }
    }

    [TestMethod]
    public void SourceScanLegacyHandlersRequireGuardBeforeLegacyExecutors()
    {
        var source = File.ReadAllText(Path.Combine(FindRepoRoot(), "src", "OneBrain.Cli", "Recipes", "RecipeRunner.cs"));

        var actvType = ExtractMethod(source, "ExecuteActvType", "ExecuteActvInvoke");
        var actvInvoke = ExtractMethod(source, "ExecuteActvInvoke", "ExecuteKey");
        var key = ExtractMethod(source, "ExecuteKey", "EvaluateLegacyExecution");

        AssertGuardBefore(actvType, "BasicActionVerifier");
        AssertGuardBefore(actvInvoke, "BasicActionVerifier");
        AssertGuardBefore(key, "UiaActionExecutor");
    }

    [TestMethod]
    public void SourceScanSafeHandlersNoLegacyFallback()
    {
        var source = File.ReadAllText(Path.Combine(FindRepoRoot(), "src", "OneBrain.Cli", "Recipes", "RecipeRunner.cs"));

        AssertSafeMethodHasNoLegacySymbols(source, "ExecuteSafeClick", "BlockSafeClickLegacyRetired");
        AssertSafeMethodHasNoLegacySymbols(source, "ExecuteSafeRead", "ExecuteSafeType");
        AssertSafeMethodHasNoLegacySymbols(source, "ExecuteSafeType", "ExecuteSafeClick");
    }

    [TestMethod]
    public void SourceScanSafePathTransitiveHelpersNoLegacySymbols()
    {
        var source = File.ReadAllText(Path.Combine(FindRepoRoot(), "src", "OneBrain.Cli", "Recipes", "RecipeRunner.cs"));

        AssertNoLegacySymbols(
            ExtractMember(
                source,
                "private (FailureKind FailureKind, string BlockReason, string Reason)? ValidateSafeExecutorManifest(",
                "private RecipeSafetyContract? BuildSafeExecutorContract("),
            "ValidateSafeExecutorManifest");
        AssertNoLegacySymbols(
            ExtractMember(
                source,
                "private RecipeSafetyContract? BuildSafeExecutorContract(",
                "private IReadOnlyList<ElementIdentity> BuildSafeExecutorCandidates("),
            "BuildSafeExecutorContract");
        AssertNoLegacySymbols(
            ExtractMember(
                source,
                "private SafeTypeLiveTarget ResolveSafeTypeLiveTarget(",
                "private static FailureKind MapDesktopTargetObserveFailureKind("),
            "ResolveSafeTypeLiveTarget");
    }

    [TestMethod]
    public void ExecuteSafeClickLegacyRemovedOrBlocked()
    {
        var source = File.ReadAllText(Path.Combine(FindRepoRoot(), "src", "OneBrain.Cli", "Recipes", "RecipeRunner.cs"));
        var methodStart = source.IndexOf("private RecipeStepRunResult ExecuteSafeClickLegacy(", StringComparison.Ordinal);

        if (methodStart < 0)
            return;

        var method = ExtractMethod(source, "ExecuteSafeClickLegacy", "ExecuteSafeClickSafeExecutor");
        StringAssert.Contains(method, "SafeClickLegacyRetirementPolicyEvaluator");
        StringAssert.Contains(method, "BlockSafeClickLegacyRetired");
    }

    [TestMethod]
    public void GetClickablePointNotReachableFromSafeHandlers()
    {
        var source = File.ReadAllText(Path.Combine(FindRepoRoot(), "src", "OneBrain.Cli", "Recipes", "RecipeRunner.cs"));

        Assert.IsFalse(ExtractMethod(source, "ExecuteSafeClick", "BlockSafeClickLegacyRetired").Contains("GetClickablePoint", StringComparison.Ordinal));
        Assert.IsFalse(ExtractMethod(source, "ExecuteSafeRead", "ExecuteSafeType").Contains("GetClickablePoint", StringComparison.Ordinal));
        Assert.IsFalse(ExtractMethod(source, "ExecuteSafeType", "ExecuteSafeClick").Contains("GetClickablePoint", StringComparison.Ordinal));
    }

    private static RecipeDefinition BuildSingleStepRecipe(string kind, string saveAs)
    {
        return new RecipeDefinition("legacy-quarantine")
        {
            Steps =
            [
                new RecipeStepDefinition
                {
                    Id = "legacy-step",
                    Kind = kind,
                    SaveAs = saveAs,
                    Args = new Dictionary<string, string>
                    {
                        ["allowLegacyActions"] = "false"
                    }
                }
            ]
        };
    }

    private static void AssertGuardBefore(string method, string legacySymbol)
    {
        var guardIndex = method.IndexOf("EvaluateLegacyExecution", StringComparison.Ordinal);
        var legacyIndex = method.IndexOf(legacySymbol, StringComparison.Ordinal);

        Assert.IsTrue(guardIndex >= 0, "guard not found");
        Assert.IsTrue(legacyIndex > guardIndex, $"{legacySymbol} appears before guard");
    }

    private static void AssertSafeMethodHasNoLegacySymbols(string source, string startMethod, string endMethod)
    {
        var method = ExtractMethod(source, startMethod, endMethod);
        AssertNoLegacySymbols(method, startMethod);
    }

    private static void AssertNoLegacySymbols(string method, string name)
    {
        foreach (var symbol in new[]
                 {
                     "new UiaActionExecutor",
                     "new BasicActionVerifier",
                     "ExecuteSafeClickLegacy(",
                     "actv.type",
                     "actv.invoke",
                     "SendInput",
                     "SendKeys",
                     "KeyboardInput",
                     "keybd_event",
                     "SetCursorPos",
                     "mouse_event",
                     "Clipboard",
                     "GetClickablePoint",
                     "el.Click",
                     ".Click("
                 })
        {
            Assert.IsFalse(method.Contains(symbol, StringComparison.Ordinal), $"{name} contains {symbol}");
        }
    }

    private static string ExtractMember(string source, string startSignature, string endSignature)
    {
        var start = source.IndexOf(startSignature, StringComparison.Ordinal);
        var end = source.IndexOf(endSignature, StringComparison.Ordinal);

        Assert.IsTrue(start >= 0, $"{startSignature} not found");
        Assert.IsTrue(end > start, $"{endSignature} not found after {startSignature}");
        return source[start..end];
    }

    private static string ExtractMethod(string source, string startMethod, string endMethod)
    {
        var start = source.IndexOf($"private RecipeStepRunResult {startMethod}(", StringComparison.Ordinal);
        if (start < 0)
            start = source.IndexOf($"private static RecipeStepRunResult {startMethod}(", StringComparison.Ordinal);
        if (start < 0)
            start = source.IndexOf($"private LegacyExecutionDecision {startMethod}(", StringComparison.Ordinal);

        var end = source.IndexOf($"private RecipeStepRunResult {endMethod}(", StringComparison.Ordinal);
        if (end < 0)
            end = source.IndexOf($"private static RecipeStepRunResult {endMethod}(", StringComparison.Ordinal);
        if (end < 0)
            end = source.IndexOf($"private LegacyExecutionDecision {endMethod}(", StringComparison.Ordinal);

        Assert.IsTrue(start >= 0, $"{startMethod} not found");
        Assert.IsTrue(end > start, $"{endMethod} not found after {startMethod}");
        return source[start..end];
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "OneBrain.slnx")))
            dir = dir.Parent;

        Assert.IsNotNull(dir, "repo root not found");
        return dir.FullName;
    }
}
