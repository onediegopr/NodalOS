using System.Net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.ChromeLab.Bridge;

namespace OneBrain.Safety.Tests;

[TestClass]
public sealed class ChromeLabLocalDevOperatorSurfaceHtmlAcceptanceEvidenceTests
{
    [TestMethod]
    public void AcceptanceEvidenceAcceptsTheBoundedHtmlViewAndVisibleSafetyMetadata()
    {
        var route = new ChromeLabLocalDevOperatorSurfaceReadOnlyRoute().Handle(IPAddress.Loopback);
        var html = new ChromeLabLocalDevOperatorSurfaceHtmlRenderer().Render(route);
        var packet = new ChromeLabLocalDevOperatorSurfaceHtmlAcceptanceEvidence().Evaluate(html);

        Assert.AreEqual(ChromeLabLocalDevOperatorSurfaceHtmlAcceptanceDecision.Accepted, packet.Decision);
        Assert.AreEqual(0, packet.Findings.Count);
        Assert.AreEqual("chromelab.local-dev.operator-surface.html.acceptance.v1", packet.EvidenceId);
        Assert.AreEqual(200, packet.StatusCode);
        Assert.AreEqual("text/html; charset=utf-8", packet.ContentType);
        Assert.AreEqual(27, packet.ReadinessPercentage);
        CollectionAssert.AreEquivalent(
            new[] { "status", "limits", "blockers", "operator-signal", "safe-next-step" },
            packet.SectionIds.ToArray());
        Assert.AreEqual("CHROMELAB_LIVE_BROWSER_EXECUTION_AUTHORITY", packet.BlockedFrontier);
        Assert.AreEqual("explicit-chromelab-local-dev-frontier", packet.RequiredOperatorSignal);
        Assert.IsTrue(packet.LocalDevOnly);
        Assert.IsTrue(packet.ReadOnly);
        Assert.IsTrue(packet.FailClosed);
        Assert.IsTrue(packet.ScriptsAbsent);
        Assert.IsTrue(packet.FormsAbsent);
        Assert.IsTrue(packet.ExternalResourcesAbsent);
        Assert.IsTrue(packet.DisabledActionVisible);
        Assert.IsTrue(packet.AcceptanceEvidenceVisible);
        Assert.AreEqual(
            "CHROMELAB_LOCAL_DEV_OPERATOR_HTML_VIEW_ACCEPTANCE_OR_CLOSE",
            packet.SafeNextStep);
    }

    [TestMethod]
    public void AcceptanceEvidenceRejectsMissingHtmlResultFailClosed()
    {
        var packet = new ChromeLabLocalDevOperatorSurfaceHtmlAcceptanceEvidence().Evaluate(null);

        Assert.AreEqual(ChromeLabLocalDevOperatorSurfaceHtmlAcceptanceDecision.Rejected, packet.Decision);
        CollectionAssert.AreEqual(new[] { "missing-html-result" }, packet.Findings.ToArray());
        Assert.AreEqual(0, packet.StatusCode);
        Assert.AreEqual(0, packet.ReadinessPercentage);
        Assert.IsTrue(packet.LocalDevOnly);
        Assert.IsTrue(packet.ReadOnly);
        Assert.IsTrue(packet.FailClosed);
        Assert.IsTrue(packet.ScriptsAbsent);
        Assert.IsTrue(packet.FormsAbsent);
        Assert.IsTrue(packet.ExternalResourcesAbsent);
        Assert.IsFalse(packet.DisabledActionVisible);
        Assert.IsFalse(packet.AcceptanceEvidenceVisible);
    }
}
