using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.ChromeLab.Bridge;

namespace OneBrain.Safety.Tests;

[TestClass]
public sealed class ChromeLabLocalDevOperatorSurfaceTests
{
    [TestMethod]
    public void LocalDevOperatorSurfaceRendersStatusLimitsBlockersAndNextSignal()
    {
        var result = new ChromeLabLocalDevOperatorSurfacePresenter().Render(SafeRequest());

        Assert.AreEqual(ChromeLabLocalDevOperatorSurfaceDecision.RenderedPreview, result.Decision);
        Assert.AreEqual(0, result.Blockers.Count);
        Assert.AreEqual("chromelab.local-dev.operator-surface-prep.v1", result.ViewModel.ViewModelId);
        Assert.AreEqual(27, result.ViewModel.Header.ReadinessPercentage);
        Assert.IsTrue(result.ViewModel.StatusText.Contains("CHROMELAB_LOCAL_DEV_OPERATOR_SURFACE_PREP_READY", StringComparison.Ordinal));
        Assert.IsTrue(result.ViewModel.Sections.Any(section => section.Title == "ChromeLab Local/Dev Operator Surface Prep"));
        Assert.IsTrue(result.ViewModel.Sections.Any(section => section.Title == "Limits"));
        Assert.IsTrue(result.ViewModel.Sections.Any(section => section.Title == "Blocked Live Browser Actions"));
        Assert.IsTrue(result.ViewModel.Sections.Any(section => section.Title == "Required Operator Signal"));
        Assert.IsTrue(result.ViewModel.Sections.Any(section => section.Title == "Safe Next Step"));
        Assert.AreEqual("CHROMELAB_LOCAL_DEV_OPERATOR_SURFACE_FOLLOW_UP_OR_CLOSE", result.ViewModel.SafeNextStep);
    }

    [TestMethod]
    public void LocalDevOperatorSurfaceActionPreviewIsDisabledAndNonExecutable()
    {
        var result = new ChromeLabLocalDevOperatorSurfacePresenter().Render(SafeRequest());
        var preview = result.ViewModel.ActionPreviews.Single();

        Assert.AreEqual("prepare-chromelab-local-dev-operator-review", preview.ActionId);
        Assert.AreEqual("CHROMELAB_LIVE_BROWSER_EXECUTION_AUTHORITY", preview.BlockedFrontier);
        Assert.AreEqual("explicit-chromelab-local-dev-frontier", preview.RequiredOperatorSignal);
        Assert.IsTrue(preview.Disabled);
        Assert.IsFalse(preview.Executable);
        Assert.IsNull(preview.ProductiveCommandId);
        Assert.IsNull(preview.HandlerId);
        Assert.IsNull(preview.CallbackName);
        Assert.IsTrue(preview.RequiredEvidence.Contains("forbidden activation scan"));
    }

    [TestMethod]
    public void LocalDevOperatorSurfaceKeepsAllLiveAndProductCapabilitiesUnavailable()
    {
        var view = new ChromeLabLocalDevOperatorSurfacePresenter().Render(SafeRequest()).ViewModel;

        Assert.IsTrue(view.LocalDevOnly);
        Assert.IsTrue(view.ReadOnly);
        Assert.IsTrue(view.FailClosed);
        Assert.IsFalse(view.LiveBrowserExecutionAvailable);
        Assert.IsFalse(view.ChromeLaunchAvailable);
        Assert.IsFalse(view.CdpConnectionAvailable);
        Assert.IsFalse(view.ExternalBrowserAutomationAvailable);
        Assert.IsFalse(view.NetworkProviderAvailable);
        Assert.IsFalse(view.UserCustomerDataAvailable);
        Assert.IsFalse(view.ProductionRuntimeAvailable);
        Assert.IsFalse(view.PublicProductPromotionAvailable);
        Assert.IsFalse(view.ProductAuthorityAvailable);
        Assert.IsFalse(view.ApprovalOrCommandExecutionAvailable);
        Assert.IsFalse(view.ReleaseCommercialReady);
    }

    [TestMethod]
    public void LocalDevOperatorSurfaceFailsClosedForMissingOrUnsafeRequests()
    {
        var presenter = new ChromeLabLocalDevOperatorSurfacePresenter();

        var missing = presenter.Render(null);
        Assert.AreEqual(ChromeLabLocalDevOperatorSurfaceDecision.Rejected, missing.Decision);
        Assert.IsTrue(missing.Blockers.Contains(ChromeLabLocalDevOperatorSurfaceBlocker.MissingRequest));

        foreach (var unsafeCase in UnsafeRequests())
        {
            var result = presenter.Render(unsafeCase.Request);

            Assert.AreEqual(ChromeLabLocalDevOperatorSurfaceDecision.Rejected, result.Decision, unsafeCase.Name);
            Assert.IsTrue(result.Blockers.Contains(unsafeCase.Expected), unsafeCase.Name);
            Assert.AreEqual(0, result.ViewModel.Header.ReadinessPercentage, unsafeCase.Name);
            Assert.AreEqual("FIX_BLOCKERS_BEFORE_CHROMELAB_LOCAL_DEV_OPERATOR_SURFACE_PREP", result.ViewModel.SafeNextStep, unsafeCase.Name);
            Assert.IsTrue(result.ViewModel.ActionPreviews.All(preview => preview.Disabled && !preview.Executable), unsafeCase.Name);
            Assert.IsTrue(result.ViewModel.ActionPreviews.All(preview =>
                preview.ProductiveCommandId is null && preview.HandlerId is null && preview.CallbackName is null), unsafeCase.Name);
        }
    }

    [TestMethod]
    public void LocalDevOperatorSurfaceSourceDoesNotWireLiveBrowserOrProductActivation()
    {
        var source = File.ReadAllText(Path.Combine(FindRepoRoot(), "src", "OneBrain.ChromeLab.Bridge", "ChromeLabLocalDevOperatorSurface.cs"));
        var forbiddenSourcePatterns = new[]
        {
            "Process." + "Start",
            "Diagnostics.Process",
            "new " + "HttpClient",
            "WebSocket." + "Create",
            "Client" + "WebSocket",
            "Chrome" + "Driver",
            "Connect" + "Async",
            "Map" + "Get(",
            "Map" + "Post(",
            "Add" + "Singleton",
            "Add" + "HostedService",
            "ICommand" + "Handler",
            "File." + "WriteAll",
            "File." + "AppendAll",
            "Directory." + "CreateDirectory"
        };

        foreach (var forbidden in forbiddenSourcePatterns)
        {
            Assert.IsFalse(source.Contains(forbidden, StringComparison.Ordinal), forbidden);
        }

        Assert.IsFalse(source.Contains("ReleaseCommercialReady: " + "true", StringComparison.Ordinal));
        Assert.IsFalse(source.Contains("Executable: " + "true", StringComparison.Ordinal));
        Assert.IsFalse(source.Contains("Disabled: " + "false", StringComparison.Ordinal));
    }

    private static ChromeLabLocalDevOperatorSurfaceRequest SafeRequest() =>
        new(
            ExplicitLocalDevOperatorSurfacePrepScope: true,
            RequestsLiveBrowserExecution: false,
            RequestsChromeLaunch: false,
            RequestsCdpConnection: false,
            RequestsExternalBrowserAutomation: false,
            RequestsNetworkProvider: false,
            RequestsUserCustomerData: false,
            RequestsProductionRuntime: false,
            RequestsPublicProductPromotion: false,
            RequestsProductAuthority: false,
            RequestsApprovalOrCommandExecution: false,
            RequestsReleaseCommercial: false);

    private static IEnumerable<(string Name, ChromeLabLocalDevOperatorSurfaceRequest Request, ChromeLabLocalDevOperatorSurfaceBlocker Expected)> UnsafeRequests()
    {
        yield return ("missing explicit local/dev scope", SafeRequest() with { ExplicitLocalDevOperatorSurfacePrepScope = false }, ChromeLabLocalDevOperatorSurfaceBlocker.MissingExplicitLocalDevOperatorSurfacePrepScope);
        yield return ("live browser execution", SafeRequest() with { RequestsLiveBrowserExecution = true }, ChromeLabLocalDevOperatorSurfaceBlocker.LiveBrowserExecutionRequested);
        yield return ("Chrome launch", SafeRequest() with { RequestsChromeLaunch = true }, ChromeLabLocalDevOperatorSurfaceBlocker.ChromeLaunchRequested);
        yield return ("CDP connection", SafeRequest() with { RequestsCdpConnection = true }, ChromeLabLocalDevOperatorSurfaceBlocker.CdpConnectionRequested);
        yield return ("external browser automation", SafeRequest() with { RequestsExternalBrowserAutomation = true }, ChromeLabLocalDevOperatorSurfaceBlocker.ExternalBrowserAutomationRequested);
        yield return ("network provider", SafeRequest() with { RequestsNetworkProvider = true }, ChromeLabLocalDevOperatorSurfaceBlocker.NetworkProviderRequested);
        yield return ("user customer data", SafeRequest() with { RequestsUserCustomerData = true }, ChromeLabLocalDevOperatorSurfaceBlocker.UserCustomerDataRequested);
        yield return ("production runtime", SafeRequest() with { RequestsProductionRuntime = true }, ChromeLabLocalDevOperatorSurfaceBlocker.ProductionRuntimeRequested);
        yield return ("public product promotion", SafeRequest() with { RequestsPublicProductPromotion = true }, ChromeLabLocalDevOperatorSurfaceBlocker.PublicProductPromotionRequested);
        yield return ("product authority", SafeRequest() with { RequestsProductAuthority = true }, ChromeLabLocalDevOperatorSurfaceBlocker.ProductAuthorityRequested);
        yield return ("approval or command execution", SafeRequest() with { RequestsApprovalOrCommandExecution = true }, ChromeLabLocalDevOperatorSurfaceBlocker.ApprovalOrCommandExecutionRequested);
        yield return ("release commercial", SafeRequest() with { RequestsReleaseCommercial = true }, ChromeLabLocalDevOperatorSurfaceBlocker.ReleaseCommercialRequested);
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
