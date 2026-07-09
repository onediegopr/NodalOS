using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.ChromeLab.Bridge;

namespace OneBrain.Safety.Tests;

[TestClass]
public sealed class ChromeLabLocalDevOperatorSurfaceAcceptanceEvidenceTests
{
    [TestMethod]
    public void AcceptanceEvidenceAcceptsTheBoundedLocalDevSurfaceAndPreservesVisibleMetadata()
    {
        var surface = new ChromeLabLocalDevOperatorSurfacePresenter().Render(SafeRequest());
        var packet = new ChromeLabLocalDevOperatorSurfaceAcceptanceEvidence().Evaluate(surface);

        Assert.AreEqual(ChromeLabLocalDevOperatorSurfaceAcceptanceDecision.Accepted, packet.Decision);
        Assert.AreEqual(0, packet.Findings.Count);
        Assert.AreEqual("chromelab.local-dev.operator-surface.acceptance.v1", packet.EvidenceId);
        Assert.AreEqual("chromelab.local-dev.operator-surface-prep.v1", packet.ViewModelId);
        Assert.AreEqual("LOCAL_DEV_OPERATOR_SURFACE_PREP", packet.SurfaceStatus);
        Assert.AreEqual(27, packet.ReadinessPercentage);
        CollectionAssert.AreEquivalent(
            new[] { "status", "limits", "blockers", "operator-signal", "safe-next-step" },
            packet.SectionIds.ToArray());
        Assert.AreEqual("prepare-chromelab-local-dev-operator-review", packet.ActionId);
        Assert.AreEqual("CHROMELAB_LIVE_BROWSER_EXECUTION_AUTHORITY", packet.BlockedFrontier);
        Assert.AreEqual("explicit-chromelab-local-dev-frontier", packet.RequiredOperatorSignal);
        Assert.IsTrue(packet.RequiredEvidence.Contains("operator-selected ChromeLab frontier"));
        Assert.IsTrue(packet.RequiredEvidence.Contains("focal local/dev surface tests"));
        Assert.IsTrue(packet.RequiredEvidence.Contains("forbidden activation scan"));
        Assert.IsTrue(packet.RequiredEvidence.Contains("no release/commercial GO"));
        Assert.IsTrue(packet.ActionDisabled);
        Assert.IsFalse(packet.ActionExecutable);
        Assert.IsTrue(packet.ActionWiringAbsent);
        Assert.IsTrue(packet.LocalDevOnly);
        Assert.IsTrue(packet.ReadOnly);
        Assert.IsTrue(packet.FailClosed);
        Assert.IsTrue(packet.UnsafeCapabilitiesUnavailable);
        Assert.IsFalse(packet.ReleaseCommercialReady);
        Assert.AreEqual("CHROMELAB_LOCAL_DEV_OPERATOR_SURFACE_FOLLOW_UP_OR_CLOSE", packet.SafeNextStep);
        Assert.IsTrue(packet.StatusText.Contains("CHROMELAB_LOCAL_DEV_OPERATOR_SURFACE_PREP_READY", StringComparison.Ordinal));
    }

    [TestMethod]
    public void AcceptanceEvidenceRejectsMissingSurfaceResultFailClosed()
    {
        var packet = new ChromeLabLocalDevOperatorSurfaceAcceptanceEvidence().Evaluate(null);

        Assert.AreEqual(ChromeLabLocalDevOperatorSurfaceAcceptanceDecision.Rejected, packet.Decision);
        CollectionAssert.AreEqual(new[] { "missing-surface-result" }, packet.Findings.ToArray());
        Assert.AreEqual(0, packet.ReadinessPercentage);
        Assert.IsTrue(packet.ActionDisabled);
        Assert.IsFalse(packet.ActionExecutable);
        Assert.IsTrue(packet.ActionWiringAbsent);
        Assert.IsTrue(packet.ReadOnly);
        Assert.IsTrue(packet.FailClosed);
        Assert.IsTrue(packet.UnsafeCapabilitiesUnavailable);
        Assert.IsFalse(packet.ReleaseCommercialReady);
    }

    [TestMethod]
    public void AcceptanceEvidenceRejectsUnsafeSurfaceRequestAndPreservesNonExecutionBoundary()
    {
        var request = SafeRequest() with { RequestsLiveBrowserExecution = true };
        var surface = new ChromeLabLocalDevOperatorSurfacePresenter().Render(request);
        var packet = new ChromeLabLocalDevOperatorSurfaceAcceptanceEvidence().Evaluate(surface);

        Assert.AreEqual(ChromeLabLocalDevOperatorSurfaceAcceptanceDecision.Rejected, packet.Decision);
        Assert.IsTrue(packet.Findings.Contains("surface-not-rendered"));
        Assert.IsTrue(packet.Findings.Contains("surface-has-blockers"));
        Assert.IsTrue(packet.Findings.Contains("unexpected-readiness"));
        Assert.IsTrue(packet.ActionDisabled);
        Assert.IsFalse(packet.ActionExecutable);
        Assert.IsTrue(packet.ActionWiringAbsent);
        Assert.IsTrue(packet.UnsafeCapabilitiesUnavailable);
        Assert.IsFalse(packet.ReleaseCommercialReady);
    }

    [TestMethod]
    public void AcceptanceEvidenceRejectsEnabledExecutableOrWiredActionPreview()
    {
        var surface = new ChromeLabLocalDevOperatorSurfacePresenter().Render(SafeRequest());
        var preview = surface.ViewModel.ActionPreviews.Single() with
        {
            Disabled = false,
            Executable = true,
            ProductiveCommandId = "unexpected-command"
        };
        var tampered = surface with
        {
            ViewModel = surface.ViewModel with { ActionPreviews = [preview] }
        };

        var packet = new ChromeLabLocalDevOperatorSurfaceAcceptanceEvidence().Evaluate(tampered);

        Assert.AreEqual(ChromeLabLocalDevOperatorSurfaceAcceptanceDecision.Rejected, packet.Decision);
        Assert.IsTrue(packet.Findings.Contains("action-preview-must-stay-disabled"));
        Assert.IsTrue(packet.Findings.Contains("action-wiring-must-stay-absent"));
        Assert.IsFalse(packet.ActionDisabled);
        Assert.IsTrue(packet.ActionExecutable);
        Assert.IsFalse(packet.ActionWiringAbsent);
        Assert.IsTrue(packet.UnsafeCapabilitiesUnavailable);
        Assert.IsFalse(packet.ReleaseCommercialReady);
    }

    [TestMethod]
    public void AcceptanceEvidenceSourceDoesNotWireRuntimeBrowserNetworkOrWrites()
    {
        var source = File.ReadAllText(Path.Combine(
            FindRepoRoot(),
            "src",
            "OneBrain.ChromeLab.Bridge",
            "ChromeLabLocalDevOperatorSurfaceAcceptanceEvidence.cs"));
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
            "File." + "WriteAll",
            "File." + "AppendAll",
            "Directory." + "CreateDirectory"
        };

        foreach (var forbidden in forbiddenSourcePatterns)
            Assert.IsFalse(source.Contains(forbidden, StringComparison.Ordinal), forbidden);
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

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "OneBrain.slnx")))
            dir = dir.Parent;

        Assert.IsNotNull(dir, "repo root not found");
        return dir.FullName;
    }
}
