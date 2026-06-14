using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Cli.Recipes;
using OneBrain.Cli.Safety;
using OneBrain.Core.Contracts;
using OneBrain.Core.Execution;
using OneBrain.Core.Identity;
using OneBrain.Core.Models;
using OneBrain.Core.Recipes;
using OneBrain.Core.Safety;

namespace OneBrain.Recipes.Tests;

[TestClass]
public sealed class SafeReadActionTests
{
    [TestMethod]
    public void SafeReadUsesReadExecutorAndWritesVariables()
    {
        var called = false;
        var result = RunWithOverrides(
            resolver: (_, _, _, _) => CreateStrongResolution(),
            readExecutor: new FakeReadExecutor(request =>
            {
                called = true;
                return SuccessfulRead(request, "invoice-123");
            }),
            () => new RecipeRunner().Run(BuildReadRecipe()));

        Assert.IsTrue(result.Success);
        Assert.IsTrue(called);
        Assert.AreEqual("true", result.Variables!["safeRead.success"]);
        Assert.AreEqual("invoice-123", result.Variables["safeRead.value"]);
        Assert.AreEqual("ValuePattern", result.Variables["safeRead.patternUsed"]);
        Assert.AreEqual("Same", result.Variables["safeRead.identity.verdict"]);
        Assert.AreNotEqual("[]", result.Variables["safeRead.evidence.ledgerJson"]);
    }

    [TestMethod]
    public void SafeReadRejectsWeakIdentity()
    {
        var result = RunWithOverrides(
            resolver: (_, _, _, _) => CreateWeakResolution(),
            readExecutor: new FakeReadExecutor(_ => throw new AssertFailedException("read executor should not run for weak identity")),
            () => new RecipeRunner().Run(BuildReadRecipe()));

        Assert.IsFalse(result.Success);
        Assert.AreEqual("false", result.Variables!["safeRead.success"]);
        Assert.AreEqual("PolicyDenied", result.Variables["safeRead.failureKind"]);
        StringAssert.Contains(result.Variables["safeRead.reason"], "strong");
    }

    [TestMethod]
    public void SafeReadRejectsBindingMismatch()
    {
        var result = RunWithOverrides(
            resolver: (_, _, _, _) => CreateStrongResolution(),
            readExecutor: new FakeReadExecutor(request =>
            {
                var observed = request.ExpectedIdentity with { RuntimeId = "42.1.10" };
                return new PatternReadResult(
                    Success: false,
                    FailureKind: FailureKind.Stale,
                    Reasons: ["InvokeTimeIdentityMismatch"],
                    PatternUsed: "ValuePattern",
                    ObservedIdentity: observed,
                    InvokeTimeIdentityChecked: true,
                    InvokeTimeIdentityVerdict: "Different",
                    InvokeTimeIdentityReason: "InvokeTimeIdentityMismatch",
                    ExpectedIdentityDigest: ElementFingerprintBuilder.Build(request.ExpectedIdentity),
                    ObservedIdentityDigest: ElementFingerprintBuilder.Build(observed));
            }),
            () => new RecipeRunner().Run(BuildReadRecipe()));

        Assert.IsFalse(result.Success);
        Assert.AreEqual("false", result.Variables!["safeRead.success"]);
        Assert.AreEqual("Stale", result.Variables["safeRead.failureKind"]);
        Assert.AreEqual("Different", result.Variables["safeRead.identity.verdict"]);
    }

    [TestMethod]
    public void SafeReadDoesNotUseClickExecutor()
    {
        var result = RunWithOverrides(
            resolver: (_, _, _, _) => CreateStrongResolution(),
            readExecutor: new FakeReadExecutor(request => SuccessfulRead(request, "read-only")),
            () => new RecipeRunner().Run(BuildReadRecipe()));

        Assert.IsTrue(result.Success);
        Assert.IsFalse(result.Variables!.ContainsKey("safeClick.method"));
        Assert.IsFalse(result.Variables.ContainsKey("safeClick.result"));
    }

    [TestMethod]
    public void SafeReadBlocksInvokeOnlyTarget()
    {
        var result = RunWithOverrides(
            resolver: (_, _, _, _) => CreateStrongResolution(),
            readExecutor: new FakeReadExecutor(request => new PatternReadResult(
                Success: false,
                FailureKind: FailureKind.PolicyDenied,
                Reasons: ["InvokePattern alone is not a read surface"],
                PatternUsed: "",
                ObservedIdentity: request.ExpectedIdentity,
                WindowFound: true,
                TargetVisible: true,
                InvokeTimeIdentityChecked: true,
                InvokeTimeIdentityVerdict: "Same",
                InvokeTimeIdentityReason: "Same",
                ExpectedIdentityDigest: ElementFingerprintBuilder.Build(request.ExpectedIdentity),
                ObservedIdentityDigest: ElementFingerprintBuilder.Build(request.ExpectedIdentity))),
            () => new RecipeRunner().Run(BuildReadRecipe()));

        Assert.IsFalse(result.Success);
        Assert.AreEqual("PolicyDenied", result.Variables!["safeRead.failureKind"]);
        StringAssert.Contains(result.Variables["safeRead.reason"], "read surface");
    }

    private static PatternReadResult SuccessfulRead(PatternReadRequest request, string value)
    {
        var digest = ElementFingerprintBuilder.Build(request.ExpectedIdentity);
        return new PatternReadResult(
            Success: true,
            FailureKind: null,
            Reasons: ["read ok"],
            Value: value,
            PatternUsed: "ValuePattern",
            ObservedIdentity: request.ExpectedIdentity,
            WindowFound: true,
            TargetVisible: true,
            MutationObserved: false,
            Signals: ["read.mutationObserved=false"],
            InvokeTimeIdentityChecked: true,
            InvokeTimeIdentityVerdict: "Same",
            InvokeTimeIdentityReason: "Same",
            ExpectedIdentityDigest: digest,
            ObservedIdentityDigest: digest);
    }

    private static RecipeDefinition BuildReadRecipe()
    {
        return new RecipeDefinition("safe-read")
        {
            Variables = new Dictionary<string, string>
            {
                ["browser.hwnd"] = "1234",
                ["browser.owned"] = "true",
                ["browser.process"] = "msedge"
            },
            Steps =
            [
                new RecipeStepDefinition
                {
                    Id = "preflight",
                    Kind = "preflight.click",
                    SaveAs = "readPreflight",
                    Args = new Dictionary<string, string> { ["targettext"] = "ver" }
                },
                new RecipeStepDefinition
                {
                    Id = "observe",
                    Kind = "target.observe",
                    SaveAs = "readPreflight",
                    Args = new Dictionary<string, string>
                    {
                        ["targetText"] = "Account number",
                        ["proc"] = "msedge"
                    }
                },
                new RecipeStepDefinition
                {
                    Id = "approval",
                    Kind = "approval.manifest",
                    SaveAs = "approval",
                    Args = new Dictionary<string, string>
                    {
                        ["from"] = "readPreflight",
                        ["mode"] = "controlled"
                    }
                },
                new RecipeStepDefinition
                {
                    Id = "safe-read",
                    Kind = "safe.read",
                    SaveAs = "safeRead",
                    Args = new Dictionary<string, string>
                    {
                        ["targettext"] = "Account number",
                        ["approvalprefix"] = "approval"
                    }
                }
            ]
        };
    }

    private static T RunWithOverrides<T>(
        Func<IntPtr, string, string, int, WebTargetResult> resolver,
        IUiaReadExecutor readExecutor,
        Func<T> action)
    {
        var resolverField = GetField("s_targetObserveResolverOverride");
        var readExecutorFactoryField = GetField("s_safeReadExecutorFactoryOverride");

        var previousResolver = resolverField.GetValue(null);
        var previousReadExecutorFactory = readExecutorFactoryField.GetValue(null);

        resolverField.SetValue(null, resolver);
        readExecutorFactoryField.SetValue(null, (Func<IUiaReadExecutor>)(() => readExecutor));

        try
        {
            return action();
        }
        finally
        {
            resolverField.SetValue(null, previousResolver);
            readExecutorFactoryField.SetValue(null, previousReadExecutorFactory);
        }
    }

    private static FieldInfo GetField(string name)
    {
        var field = typeof(RecipeRunner).GetField(name, BindingFlags.Static | BindingFlags.NonPublic);
        Assert.IsNotNull(field, $"static field '{name}' not found");
        return field;
    }

    private static WebTargetResult CreateStrongResolution()
    {
        return new WebTargetResult
        {
            Found = true,
            CandidateCount = 1,
            WindowsSearched = 1,
            SelectedName = "Account number",
            SelectedControlType = "Edit",
            SelectedHwnd = "4321",
            SelectedBoundingRect = "10,10,120,24",
            SelectedRuntimeId = "42.1.9",
            SelectedAutomationId = "account-number",
            SelectedClassName = "TextBox",
            SelectedFrameworkId = "UIA",
            SelectedAncestorPath = "Window:ONE Brain > Pane:Form",
            SelectedProcessName = "msedge",
            SelectedWindowTitle = "ONE Brain",
            HasInvoke = false,
            Reason = "exact match",
            ShadowEngineFound = true,
            ShadowEngineVerdict = "Same",
            ShadowAgreesWithLegacy = true,
            ShadowEngineSelectedName = "Account number",
            ShadowReasons = "runtime id matches"
        };
    }

    private static WebTargetResult CreateWeakResolution()
    {
        return new WebTargetResult
        {
            Found = true,
            CandidateCount = 1,
            WindowsSearched = 1,
            SelectedName = "Account number",
            SelectedControlType = "Edit",
            SelectedHwnd = "4321",
            SelectedBoundingRect = "10,10,120,24",
            SelectedRuntimeId = "",
            SelectedAutomationId = "account-number",
            SelectedClassName = "TextBox",
            SelectedFrameworkId = "UIA",
            SelectedAncestorPath = "Window:ONE Brain > Pane:Form",
            SelectedProcessName = "msedge",
            SelectedWindowTitle = "ONE Brain",
            HasInvoke = false,
            Reason = "weak match",
            ShadowEngineFound = true,
            ShadowEngineVerdict = "LikelySame",
            ShadowAgreesWithLegacy = true,
            ShadowEngineSelectedName = "Account number",
            ShadowReasons = "runtime id missing"
        };
    }

    private sealed class FakeReadExecutor(Func<PatternReadRequest, PatternReadResult> handler) : IUiaReadExecutor
    {
        public PatternReadResult Read(PatternReadRequest request) => handler(request);
    }
}
