using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Cli.Recipes;
using OneBrain.Cli.Safety;
using OneBrain.Core.Execution;
using OneBrain.Core.Identity;
using OneBrain.Core.Contracts;
using OneBrain.Core.Models;
using OneBrain.Core.Recipes;
using OneBrain.Core.Safety;

namespace OneBrain.Recipes.Tests;

[TestClass]
public sealed class SafeTypeActionTests
{
    [TestMethod]
    public void SafeTypeUsesTypeExecutorAndWritesVariables()
    {
        var called = false;
        var result = RunWithOverrides(
            resolver: (_, _, _, _) => CreateStrongResolution(),
            typeExecutor: new FakeTypeExecutor(request =>
            {
                called = true;
                return SuccessfulType(request);
            }),
            () => new RecipeRunner().Run(BuildTypeRecipe()));

        Assert.IsTrue(result.Success);
        Assert.IsTrue(called);
        Assert.AreEqual("true", result.Variables!["safeType.success"]);
        Assert.AreEqual("invoice-123", result.Variables["safeType.valueAfter"]);
        Assert.AreEqual(ComputeDigest("invoice-123"), result.Variables["safeType.approvedTextDigest"]);
        Assert.AreEqual("ValuePattern.SetValue", result.Variables["safeType.patternUsed"]);
        Assert.AreEqual("Same", result.Variables["safeType.identity.verdict"]);
        Assert.AreEqual("true", result.Variables["safeType.ownership.checked"]);
        Assert.AreEqual("true", result.Variables["safeType.ownership.allowed"]);
        Assert.AreEqual("true", result.Variables["safeType.mutationObserved"]);
        Assert.AreNotEqual("[]", result.Variables["safeType.evidence.ledgerJson"]);
    }

    [TestMethod]
    public void SafeTypeRejectsRuntimeValueDifferentFromApprovedValue()
    {
        var called = false;
        var result = RunWithOverrides(
            resolver: (_, _, _, _) => CreateStrongResolution(),
            typeExecutor: new FakeTypeExecutor(_ =>
            {
                called = true;
                throw new AssertFailedException("type executor should not run for unapproved text");
            }),
            () => new RecipeRunner().Run(BuildTypeRecipe(stepText: "other-value")));

        Assert.IsFalse(result.Success);
        Assert.IsFalse(called);
        Assert.AreEqual("false", result.Variables!["safeType.success"]);
        Assert.AreEqual("PolicyDenied", result.Variables["safeType.failureKind"]);
        Assert.AreEqual("safe.type text does not match approved text digest", result.Variables["safeType.reason"]);
    }

    [TestMethod]
    public void SafeTypeRejectsWeakIdentity()
    {
        var result = RunWithOverrides(
            resolver: (_, _, _, _) => CreateWeakResolution(),
            typeExecutor: new FakeTypeExecutor(_ => throw new AssertFailedException("type executor should not run for weak identity")),
            () => new RecipeRunner().Run(BuildTypeRecipe()));

        Assert.IsFalse(result.Success);
        Assert.AreEqual("false", result.Variables!["safeType.success"]);
        Assert.AreEqual("PolicyDenied", result.Variables["safeType.failureKind"]);
        StringAssert.Contains(result.Variables["safeType.reason"], "strong");
    }

    [TestMethod]
    public void SafeTypeBindingUsesLiveResolvedIdentity()
    {
        var calls = 0;
        var called = false;
        var result = RunWithOverrides(
            resolver: (_, _, _, _) =>
            {
                calls++;
                return calls == 1 ? CreateStrongResolution() : CreateStrongResolution(runtimeId: "42.1.10");
            },
            typeExecutor: new FakeTypeExecutor(_ =>
            {
                called = true;
                throw new AssertFailedException("type executor should not run when live binding mismatches");
            }),
            () => new RecipeRunner().Run(BuildTypeRecipe()));

        Assert.IsFalse(result.Success);
        Assert.IsFalse(called);
        Assert.AreEqual("Stale", result.Variables!["safeType.failureKind"]);
        StringAssert.Contains(result.Variables["safeType.reason"], "runtime id differs");
    }

    [TestMethod]
    public void SafeTypeExecutorStillPerformsInvokeTimeGateAfterBinding()
    {
        var result = RunWithOverrides(
            resolver: (_, _, _, _) => CreateStrongResolution(),
            typeExecutor: new FakeTypeExecutor(request =>
            {
                var observed = request.ExpectedIdentity with { RuntimeId = "42.1.10" };
                return new TypeExecutionResult(
                    Success: false,
                    FailureKind: FailureKind.Stale,
                    Reasons: ["InvokeTimeIdentityMismatch"],
                    ValueBefore: "before",
                    ValueAfter: "before",
                    ApprovedTextDigest: request.ApprovedTextDigest,
                    PatternUsed: "",
                    ObservedIdentity: observed,
                    IdentityVerdict: "Different",
                    InvokeTimeIdentityChecked: true,
                    InvokeTimeIdentityVerdict: "Different",
                    InvokeTimeIdentityReason: "InvokeTimeIdentityMismatch",
                    ExpectedIdentityDigest: ElementFingerprintBuilder.Build(request.ExpectedIdentity),
                    ObservedIdentityDigest: ElementFingerprintBuilder.Build(observed),
                    MutationObserved: false,
                    SurfaceAllowed: false,
                    SurfaceReason: "",
                    OwnershipChecked: false,
                    OwnershipAllowed: false,
                    WindowFound: true,
                    TargetVisible: true);
            }),
            () => new RecipeRunner().Run(BuildTypeRecipe()));

        Assert.IsFalse(result.Success);
        Assert.AreEqual("Stale", result.Variables!["safeType.failureKind"]);
        Assert.AreEqual("Different", result.Variables["safeType.identity.verdict"]);
        Assert.AreEqual("false", result.Variables["safeType.mutationObserved"]);
    }

    [TestMethod]
    public void SafeTypeLegacyDispatchPathBlocked()
    {
        var result = RunWithOverrides(
            resolver: (_, _, _, _) => CreateStrongResolution(),
            typeExecutor: new FakeTypeExecutor(_ => throw new AssertFailedException("type executor should not run for legacy dispatch")),
            () => new RecipeRunner().Run(BuildTypeRecipe(dispatchPath: "legacy")));

        Assert.IsFalse(result.Success);
        Assert.AreEqual("safe.type legacy dispatch is not allowed", result.Variables!["safeType.reason"]);
    }

    [TestMethod]
    public void SafeTypeNoFallbackToLegacyOrClickVariables()
    {
        var result = RunWithOverrides(
            resolver: (_, _, _, _) => CreateStrongResolution(),
            typeExecutor: new FakeTypeExecutor(request => SuccessfulType(request)),
            () => new RecipeRunner().Run(BuildTypeRecipe()));

        Assert.IsTrue(result.Success);
        Assert.IsFalse(result.Variables!.ContainsKey("safeClick.method"));
        Assert.IsFalse(result.Variables.ContainsKey("safeClick.result"));
    }

    [TestMethod]
    public void SafeTypeClassifiedSensitive()
    {
        Assert.IsTrue(OneBrain.Core.Approval.SensitiveActionClassifier.IsSensitiveStepKind("safe.type"));
    }

    [TestMethod]
    public void SourceScanRecipeRunnerSafeTypeDoesNotCallLegacyExecutors()
    {
        var source = File.ReadAllText(Path.Combine(FindRepoRoot(), "src", "OneBrain.Cli", "Recipes", "RecipeRunner.cs"));
        var start = source.IndexOf("private RecipeStepRunResult ExecuteSafeType(", StringComparison.Ordinal);
        var end = source.IndexOf("private RecipeStepRunResult ExecuteSafeClick(", StringComparison.Ordinal);
        Assert.IsTrue(start >= 0);
        Assert.IsTrue(end > start);
        var body = source[start..end];

        Assert.IsFalse(body.Contains("UiaActionExecutor", StringComparison.Ordinal));
        Assert.IsFalse(body.Contains("BasicActionVerifier", StringComparison.Ordinal));
        Assert.IsFalse(body.Contains("el.Click", StringComparison.Ordinal));
        Assert.IsFalse(body.Contains(".Click(", StringComparison.Ordinal));
        Assert.IsFalse(body.Contains("SendInput", StringComparison.Ordinal));
        Assert.IsFalse(body.Contains("SendKeys", StringComparison.Ordinal));
        Assert.IsFalse(body.Contains("GetClickablePoint", StringComparison.Ordinal));
        Assert.IsFalse(body.Contains("actv.type", StringComparison.Ordinal));
        Assert.IsFalse(body.Contains("actv.invoke", StringComparison.Ordinal));
    }

    private static TypeExecutionResult SuccessfulType(TypeExecutionRequest request)
    {
        var digest = ElementFingerprintBuilder.Build(request.ExpectedIdentity);
        return new TypeExecutionResult(
            Success: true,
            FailureKind: null,
            Reasons: ["type ok"],
            ValueBefore: "before",
            ValueAfter: request.ApprovedText,
            ApprovedTextDigest: request.ApprovedTextDigest,
            PatternUsed: "ValuePattern.SetValue",
            ObservedIdentity: request.ExpectedIdentity,
            IdentityVerdict: "Same",
            InvokeTimeIdentityChecked: true,
            InvokeTimeIdentityVerdict: "Same",
            InvokeTimeIdentityReason: "Same",
            ExpectedIdentityDigest: digest,
            ObservedIdentityDigest: digest,
            MutationObserved: true,
            SurfaceAllowed: true,
            SurfaceReason: "type surface allowed",
            OwnershipChecked: true,
            OwnershipAllowed: true,
            WindowFound: true,
            TargetVisible: true,
            Signals: ["type.mutationObserved=true"]);
    }

    private static RecipeDefinition BuildTypeRecipe(
        string approvedText = "invoice-123",
        string stepText = "invoice-123",
        string dispatchPath = "")
    {
        var args = new Dictionary<string, string>
        {
            ["targettext"] = "Account number",
            ["approvalprefix"] = "approval",
            ["text"] = stepText
        };
        if (!string.IsNullOrWhiteSpace(dispatchPath))
            args["dispatchPath"] = dispatchPath;

        return new RecipeDefinition("safe-type")
        {
            Variables = new Dictionary<string, string>
            {
                ["browser.hwnd"] = "1234",
                ["browser.owned"] = "true",
                ["browser.process"] = "msedge",
                ["typePreflight.approvedText"] = approvedText
            },
            Steps =
            [
                new RecipeStepDefinition
                {
                    Id = "preflight",
                    Kind = "preflight.click",
                    SaveAs = "typePreflight",
                    Args = new Dictionary<string, string> { ["targettext"] = "Account number" }
                },
                new RecipeStepDefinition
                {
                    Id = "observe",
                    Kind = "target.observe",
                    SaveAs = "typePreflight",
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
                        ["from"] = "typePreflight",
                        ["mode"] = "controlled"
                    }
                },
                new RecipeStepDefinition
                {
                    Id = "safe-type",
                    Kind = "safe.type",
                    SaveAs = "safeType",
                    Args = args
                }
            ]
        };
    }

    private static T RunWithOverrides<T>(
        Func<IntPtr, string, string, int, WebTargetResult> resolver,
        IUiaTypeExecutor typeExecutor,
        Func<T> action)
    {
        var resolverField = GetField("s_targetObserveResolverOverride");
        var typeExecutorFactoryField = GetField("s_safeTypeExecutorFactoryOverride");

        var previousResolver = resolverField.GetValue(null);
        var previousTypeExecutorFactory = typeExecutorFactoryField.GetValue(null);

        resolverField.SetValue(null, resolver);
        typeExecutorFactoryField.SetValue(null, (Func<IUiaTypeExecutor>)(() => typeExecutor));

        try
        {
            return action();
        }
        finally
        {
            resolverField.SetValue(null, previousResolver);
            typeExecutorFactoryField.SetValue(null, previousTypeExecutorFactory);
        }
    }

    private static FieldInfo GetField(string name)
    {
        var field = typeof(RecipeRunner).GetField(name, BindingFlags.Static | BindingFlags.NonPublic);
        Assert.IsNotNull(field, $"static field '{name}' not found");
        return field;
    }

    private static WebTargetResult CreateStrongResolution(string runtimeId = "42.1.9")
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
            SelectedRuntimeId = runtimeId,
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
        return CreateStrongResolution(runtimeId: "");
    }

    private static string ComputeDigest(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "OneBrain.slnx")))
            dir = dir.Parent;

        Assert.IsNotNull(dir, "repo root not found");
        return dir.FullName;
    }

    private sealed class FakeTypeExecutor(Func<TypeExecutionRequest, TypeExecutionResult> handler) : IUiaTypeExecutor
    {
        public TypeExecutionResult Type(TypeExecutionRequest request) => handler(request);
    }
}
