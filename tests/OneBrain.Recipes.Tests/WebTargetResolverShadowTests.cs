using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Cli.Safety;
using OneBrain.Core.Selectors.Web;

namespace OneBrain.Recipes.Tests;

[TestClass]
public sealed class WebTargetResolverShadowTests
{
    [TestMethod]
    public void ApplyShadowParity_Preserves_Legacy_Fields()
    {
        var legacy = new WebTargetResult
        {
            Found = true,
            CandidateCount = 1,
            WindowsSearched = 3,
            SelectedName = "More information...",
            SelectedControlType = "Hyperlink",
            SelectedHwnd = "1234",
            SelectedBoundingRect = "10,10,120,24",
            SelectedRuntimeId = "42.1.9",
            SelectedAutomationId = "help-link",
            SelectedClassName = "Chrome_RenderWidgetHostHWND",
            SelectedHelpText = "More information help",
            SelectedLegacyName = "More information...",
            SelectedFrameworkId = "UIA",
            SelectedAncestorPath = "Window:ONE Brain > Pane:Main > Document:App",
            SelectedProcessName = "msedge",
            SelectedWindowTitle = "ONE Brain",
            SelectedHelpTextPresent = true,
            SelectedLegacyNamePresent = true,
            HasInvoke = true,
            HasClickablePoint = false,
            CandidatesJson = "[{}]",
            Reason = "exact match",
            ChildHwndDiagnostics = ["diag-1"]
        };

        var parity = new WebSelectorParity
        {
            EngineFound = true,
            EngineVerdict = "LikelySame",
            EngineSelectedName = "More information...",
            AgreesWithLegacy = true,
            Reasons = ["shadow agrees"]
        };

        var method = typeof(WebTargetResolver).GetMethod(
            "ApplyShadowParity",
            BindingFlags.NonPublic | BindingFlags.Static);

        Assert.IsNotNull(method);
        var projected = (WebTargetResult?)method.Invoke(null, [legacy, parity]);

        Assert.IsNotNull(projected);
        Assert.IsTrue(projected.Found);
        Assert.AreEqual(legacy.CandidateCount, projected.CandidateCount);
        Assert.AreEqual(legacy.WindowsSearched, projected.WindowsSearched);
        Assert.AreEqual(legacy.SelectedName, projected.SelectedName);
        Assert.AreEqual(legacy.SelectedControlType, projected.SelectedControlType);
        Assert.AreEqual(legacy.SelectedHwnd, projected.SelectedHwnd);
        Assert.AreEqual(legacy.SelectedBoundingRect, projected.SelectedBoundingRect);
        Assert.AreEqual(legacy.SelectedRuntimeId, projected.SelectedRuntimeId);
        Assert.AreEqual(legacy.SelectedAutomationId, projected.SelectedAutomationId);
        Assert.AreEqual(legacy.SelectedClassName, projected.SelectedClassName);
        Assert.AreEqual(legacy.SelectedHelpText, projected.SelectedHelpText);
        Assert.AreEqual(legacy.SelectedLegacyName, projected.SelectedLegacyName);
        Assert.AreEqual(legacy.SelectedFrameworkId, projected.SelectedFrameworkId);
        Assert.AreEqual(legacy.SelectedAncestorPath, projected.SelectedAncestorPath);
        Assert.AreEqual(legacy.SelectedProcessName, projected.SelectedProcessName);
        Assert.AreEqual(legacy.SelectedWindowTitle, projected.SelectedWindowTitle);
        Assert.AreEqual(legacy.SelectedHelpTextPresent, projected.SelectedHelpTextPresent);
        Assert.AreEqual(legacy.SelectedLegacyNamePresent, projected.SelectedLegacyNamePresent);
        Assert.AreEqual(legacy.HasInvoke, projected.HasInvoke);
        Assert.AreEqual(legacy.HasClickablePoint, projected.HasClickablePoint);
        Assert.AreEqual(legacy.CandidatesJson, projected.CandidatesJson);
        Assert.AreEqual(legacy.Reason, projected.Reason);
        CollectionAssert.AreEqual(legacy.ChildHwndDiagnostics.ToList(), projected.ChildHwndDiagnostics.ToList());
        Assert.AreEqual("LikelySame", projected.ShadowEngineVerdict);
        Assert.AreEqual("More information...", projected.ShadowEngineSelectedName);
        Assert.IsTrue(projected.ShadowAgreesWithLegacy);
    }
}
