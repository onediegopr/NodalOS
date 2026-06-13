using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Cli.Recipes;
using OneBrain.Cli.Safety;
using OneBrain.Core.Approval;
using OneBrain.Core.Contracts;
using OneBrain.Core.Recipes;
using OneBrain.Core.Safety;

namespace OneBrain.Recipes.Tests;

[TestClass]
public sealed class TargetObserveTests
{
    [TestMethod]
    public void WebTargetResultSelectedIdentityMapsToStrongElementIdentity()
    {
        var identity = WebTargetResultIdentityMapper.ToSelectedIdentity(CreateStrongResolution());

        Assert.IsNotNull(identity);
        Assert.IsTrue(identity.IsStrong);
        Assert.AreEqual("42.1.9", identity.RuntimeId);
        Assert.AreEqual("categories-button", identity.AutomationId);
        Assert.AreEqual("Categorias", identity.Name);
    }

    [TestMethod]
    public void WebTargetResultWithoutRuntimeIdMapsToWeakIdentity()
    {
        var identity = WebTargetResultIdentityMapper.ToSelectedIdentity(CreateWeakResolution());
        var strength = WebTargetResultIdentityMapper.ResolveIdentityStrength(CreateWeakResolution());

        Assert.IsNotNull(identity);
        Assert.IsFalse(identity.IsStrong);
        Assert.AreEqual("categories-button", identity.AutomationId);
        Assert.AreEqual(IdentityStrength.Weak, strength);
    }

    [TestMethod]
    public void WebTargetResultIdentityMappingDoesNotExposeLegacyValue()
    {
        var identity = WebTargetResultIdentityMapper.ToSelectedIdentity(CreateStrongResolution());

        Assert.IsNotNull(identity);
        Assert.AreEqual("", identity.LegacyValue);
        Assert.IsNull(identity.SiblingIndex);
        Assert.AreEqual("", identity.ParentFingerprint);
    }

    [TestMethod]
    public void TargetObserveIdentityVarsCanBuildApprovedIdentityInput()
    {
        var runner = new RecipeRunner();

        RunWithTargetObserveResolverOverride(
            (_, _, _, _) => CreateStrongResolution(),
            () => runner.Run(new RecipeDefinition("target-observe-input")
            {
                Variables = BuildOwnedSessionVariables(),
                Steps =
                [
                    new RecipeStepDefinition
                    {
                        Id = "observe",
                        Kind = "target.observe",
                        SaveAs = "observed",
                        Args = new Dictionary<string, string>
                        {
                            ["targetText"] = "Categorias",
                            ["proc"] = "msedge"
                        }
                    }
                ]
            }));

        var method = typeof(RecipeRunner).GetMethod("TryReadApprovedIdentityInput", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(method);
        var input = (ApprovedIdentityInput?)method.Invoke(runner, ["observed"]);

        Assert.IsNotNull(input);
        Assert.IsNotNull(input.Identity);
        Assert.AreEqual("42.1.9", input.Identity.RuntimeId);
        Assert.AreEqual("web-uia", input.Source);
    }

    [TestMethod]
    public void TargetObserveWritesIdentityVariablesFromSyntheticWebTargetResult()
    {
        var result = RunWithTargetObserveResolverOverride(
            (_, _, _, _) => CreateStrongResolution(),
            () => new RecipeRunner().Run(new RecipeDefinition("target-observe")
            {
                Variables = BuildOwnedSessionVariables(),
                Steps =
                [
                    new RecipeStepDefinition
                    {
                        Id = "observe",
                        Kind = "target.observe",
                        SaveAs = "observed",
                        Args = new Dictionary<string, string>
                        {
                            ["targetText"] = "Categorias",
                            ["proc"] = "msedge"
                        }
                    }
                ]
            }));

        Assert.IsTrue(result.Success);
        Assert.AreEqual("Categorias", result.Variables!["observed.targetText"]);
        Assert.AreEqual("true", result.Variables["observed.resolution.found"]);
        Assert.AreEqual("42.1.9", result.Variables["observed.identity.runtimeId"]);
        Assert.AreEqual("categories-button", result.Variables["observed.identity.automationId"]);
        Assert.AreEqual("Categorias", result.Variables["observed.identity.name"]);
        Assert.AreEqual("Button", result.Variables["observed.identity.controlType"]);
        Assert.AreEqual("Chrome_RenderWidgetHostHWND", result.Variables["observed.identity.className"]);
        Assert.AreEqual("UIA", result.Variables["observed.identity.frameworkId"]);
        Assert.AreEqual("Window:ONE Brain > Pane:Catalog > Document:Main", result.Variables["observed.identity.ancestorPath"]);
        Assert.AreEqual("msedge", result.Variables["observed.identity.processName"]);
        Assert.AreEqual("ONE Brain", result.Variables["observed.identity.windowTitle"]);
        Assert.AreEqual("web-uia", result.Variables["observed.identity.source"]);
        Assert.AreEqual("Strong", result.Variables["observed.identity.strength"]);
        Assert.AreEqual("true", result.Variables["observed.identity.helpTextPresent"]);
        Assert.AreEqual("true", result.Variables["observed.identity.legacyNamePresent"]);
    }

    [TestMethod]
    public void TargetObserveIsReadOnlyAndDoesNotClick()
    {
        var result = RunWithTargetObserveResolverOverride(
            (_, _, _, _) => CreateStrongResolution(),
            () => new RecipeRunner().Run(new RecipeDefinition("target-observe-read-only")
            {
                Variables = BuildOwnedSessionVariables(),
                Steps =
                [
                    new RecipeStepDefinition
                    {
                        Id = "observe",
                        Kind = "target.observe",
                        SaveAs = "observed",
                        Args = new Dictionary<string, string>
                        {
                            ["targetText"] = "Categorias",
                            ["proc"] = "msedge"
                        }
                    }
                ]
            }));

        Assert.IsTrue(result.Steps.Single().Success);
        Assert.IsFalse(result.Variables!.ContainsKey("observed.executed"));
        Assert.IsFalse(result.Variables.ContainsKey("safeClick.result"));
    }

    [TestMethod]
    public void ApprovalManifestConsumesTargetObserveIdentityAndEmitsV3Strong()
    {
        var preflight = ClickPreflightEvaluator.Evaluate("Categorias");

        var result = RunWithTargetObserveResolverOverride(
            (_, _, _, _) => CreateStrongResolution(),
            () => new RecipeRunner().Run(new RecipeDefinition("observe-then-approval")
            {
                Variables = new Dictionary<string, string>(BuildOwnedSessionVariables())
                {
                    ["clickPreflight.evidenceJson"] = preflight.EvidenceJson ?? ""
                },
                Steps =
                [
                    new RecipeStepDefinition
                    {
                        Id = "observe",
                        Kind = "target.observe",
                        SaveAs = "clickPreflight",
                        Args = new Dictionary<string, string>
                        {
                            ["targetText"] = "Categorias",
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
                            ["from"] = "clickPreflight",
                            ["mode"] = "controlled"
                        }
                    }
                ]
            }));

        Assert.IsTrue(result.Success);
        Assert.AreEqual("approval-v3", result.Variables!["approval.identity.schemaVersion"]);
        Assert.AreEqual("Strong", result.Variables["approval.identity.strength"]);
        Assert.IsTrue(result.Variables.ContainsKey("approval.identity.digest"));
        Assert.IsTrue(result.Variables.ContainsKey("approval.identity.bindingHash"));
        Assert.IsTrue(result.Variables.ContainsKey("approval.identity.selector"));
        Assert.AreEqual("approval-v2", result.Variables["approval.policyVersion"]);
    }

    [TestMethod]
    public void ApprovalManifestWithoutObserveStillV2Compatible()
    {
        var preflight = ClickPreflightEvaluator.Evaluate("Categorias");

        var result = new RecipeRunner().Run(new RecipeDefinition("approval-without-observe")
        {
            Variables = new Dictionary<string, string>
            {
                ["clickPreflight.evidenceJson"] = preflight.EvidenceJson ?? ""
            },
            Steps =
            [
                new RecipeStepDefinition
                {
                    Id = "approval",
                    Kind = "approval.manifest",
                    SaveAs = "approval",
                    Args = new Dictionary<string, string>
                    {
                        ["from"] = "clickPreflight",
                        ["mode"] = "controlled"
                    }
                }
            ]
        });

        Assert.IsTrue(result.Success);
        Assert.IsFalse(result.Variables!.ContainsKey("approval.identity.schemaVersion"));
        Assert.AreEqual("approval-v2", result.Variables["approval.policyVersion"]);
    }

    [TestMethod]
    public void SafeClickResultUnchanged()
    {
        var preflight = ClickPreflightEvaluator.Evaluate("Categorias");

        var baseline = new RecipeRunner().Run(new RecipeDefinition("safe-click-baseline")
        {
            Variables = new Dictionary<string, string>
            {
                ["clickPreflight.evidenceJson"] = preflight.EvidenceJson ?? ""
            },
            Steps =
            [
                new RecipeStepDefinition
                {
                    Id = "approval",
                    Kind = "approval.manifest",
                    SaveAs = "approval",
                    Args = new Dictionary<string, string>
                    {
                        ["from"] = "clickPreflight",
                        ["mode"] = "controlled"
                    }
                },
                new RecipeStepDefinition
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
                }
            ]
        });

        var withObserve = new RecipeRunner().Run(new RecipeDefinition("safe-click-with-observe")
        {
            Variables = new Dictionary<string, string>
            {
                ["clickPreflight.evidenceJson"] = preflight.EvidenceJson ?? ""
            },
            Steps =
            [
                new RecipeStepDefinition
                {
                    Id = "approval",
                    Kind = "approval.manifest",
                    SaveAs = "approval",
                    Args = new Dictionary<string, string>
                    {
                        ["from"] = "clickPreflight",
                        ["mode"] = "controlled"
                    }
                },
                new RecipeStepDefinition
                {
                    Id = "observe",
                    Kind = "target.observe",
                    SaveAs = "observed",
                    Args = new Dictionary<string, string>
                    {
                        ["targetText"] = "Categorias",
                        ["proc"] = "msedge"
                    }
                },
                new RecipeStepDefinition
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
                }
            ]
        });

        Assert.AreEqual(baseline.Success, withObserve.Success);
        Assert.AreEqual(baseline.Variables!["safeClick.result"], withObserve.Variables!["safeClick.result"]);
        Assert.AreEqual(baseline.Variables["safeClick.reason"], withObserve.Variables["safeClick.reason"]);
        Assert.AreEqual(baseline.Steps.Last().Message, withObserve.Steps.Last().Message);
    }

    [TestMethod]
    public void TargetObserveNotFoundIsSafe()
    {
        var result = RunWithTargetObserveResolverOverride(
            (_, _, _, _) => new WebTargetResult
            {
                Found = false,
                CandidateCount = 0,
                Reason = "not found in 3 windows"
            },
            () => new RecipeRunner().Run(new RecipeDefinition("target-observe-not-found")
            {
                Variables = BuildOwnedSessionVariables(),
                Steps =
                [
                    new RecipeStepDefinition
                    {
                        Id = "observe",
                        Kind = "target.observe",
                        SaveAs = "observed",
                        Args = new Dictionary<string, string>
                        {
                            ["targetText"] = "Categorias",
                            ["proc"] = "msedge"
                        }
                    }
                ]
            }));

        Assert.IsTrue(result.Success);
        Assert.IsTrue(result.Steps.Single().Success);
        Assert.AreEqual("false", result.Variables!["observed.resolution.found"]);
        Assert.AreEqual(FailureKind.NotFound.ToString(), result.Variables["observed.failureKind"]);
    }

    private static T RunWithTargetObserveResolverOverride<T>(
        Func<IntPtr, string, string, int, WebTargetResult> resolver,
        Func<T> action)
    {
        var field = typeof(RecipeRunner).GetField("s_targetObserveResolverOverride", BindingFlags.Static | BindingFlags.NonPublic);
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

    private static Dictionary<string, string> BuildOwnedSessionVariables()
    {
        return new Dictionary<string, string>
        {
            ["browser.hwnd"] = "1234",
            ["browser.owned"] = "true",
            ["browser.process"] = "msedge"
        };
    }

    private static WebTargetResult CreateStrongResolution()
    {
        return new WebTargetResult
        {
            Found = true,
            CandidateCount = 1,
            WindowsSearched = 1,
            SelectedName = "Categorias",
            SelectedControlType = "Button",
            SelectedBoundingRect = "10,10,120,24",
            SelectedRuntimeId = "42.1.9",
            SelectedAutomationId = "categories-button",
            SelectedClassName = "Chrome_RenderWidgetHostHWND",
            SelectedHelpText = "Abrir categorias",
            SelectedLegacyName = "Categorias",
            SelectedFrameworkId = "UIA",
            SelectedAncestorPath = "Window:ONE Brain > Pane:Catalog > Document:Main",
            SelectedProcessName = "msedge",
            SelectedWindowTitle = "ONE Brain",
            SelectedHelpTextPresent = true,
            SelectedLegacyNamePresent = true,
            HasInvoke = true,
            Reason = "exact match",
            ShadowEngineFound = true,
            ShadowEngineVerdict = "Same",
            ShadowAgreesWithLegacy = true,
            ShadowEngineSelectedName = "Categorias",
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
            SelectedName = "Categorias",
            SelectedControlType = "Button",
            SelectedBoundingRect = "10,10,120,24",
            SelectedAutomationId = "categories-button",
            SelectedClassName = "Chrome_RenderWidgetHostHWND",
            SelectedFrameworkId = "UIA",
            SelectedAncestorPath = "Window:ONE Brain > Pane:Catalog > Document:Main",
            SelectedProcessName = "msedge",
            SelectedWindowTitle = "ONE Brain",
            SelectedHelpTextPresent = false,
            SelectedLegacyNamePresent = false,
            HasInvoke = true,
            Reason = "exact match"
        };
    }
}
