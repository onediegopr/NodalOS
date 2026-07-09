using System.Net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.ChromeLab.Bridge;

namespace OneBrain.Safety.Tests;

[TestClass]
public sealed class ChromeLabLocalDevOperatorSurfaceRouteAcceptanceEvidenceTests
{
    [TestMethod]
    public void AcceptanceEvidenceAcceptsTheLoopbackReadOnlyRouteAndPreservesSurfaceMetadata()
    {
        var route = new ChromeLabLocalDevOperatorSurfaceReadOnlyRoute().Handle(IPAddress.Loopback);
        var packet = new ChromeLabLocalDevOperatorSurfaceRouteAcceptanceEvidence().Evaluate(route);

        Assert.AreEqual(ChromeLabLocalDevOperatorSurfaceRouteAcceptanceDecision.Accepted, packet.Decision);
        Assert.AreEqual(0, packet.Findings.Count);
        Assert.AreEqual("chromelab.local-dev.operator-surface.route.acceptance.v1", packet.EvidenceId);
        Assert.AreEqual("chromelab.local-dev.operator-surface.route.v1", packet.RouteId);
        Assert.AreEqual("/operator/local-dev/chromelab", packet.RoutePath);
        Assert.AreEqual("GET", packet.Method);
        Assert.AreEqual(200, packet.StatusCode);
        Assert.AreEqual("chromelab.local-dev.operator-surface.acceptance.v1", packet.SurfaceEvidenceId);
        Assert.AreEqual("chromelab.local-dev.operator-surface-prep.v1", packet.ViewModelId);
        Assert.AreEqual(27, packet.ReadinessPercentage);
        Assert.IsTrue(packet.LocalDevOnly);
        Assert.IsTrue(packet.LoopbackOnly);
        Assert.IsTrue(packet.ReadOnly);
        Assert.IsTrue(packet.FailClosed);
        Assert.IsTrue(packet.CacheDisabled);
        Assert.IsTrue(packet.PayloadAvailable);
        Assert.IsTrue(packet.ActionDisabled);
        Assert.IsFalse(packet.ActionExecutable);
        Assert.IsTrue(packet.ActionWiringAbsent);
        Assert.IsTrue(packet.UnsafeCapabilitiesUnavailable);
        Assert.IsFalse(packet.ReleaseCommercialReady);
        Assert.AreEqual(
            "CHROMELAB_LOCAL_DEV_OPERATOR_SURFACE_ROUTE_ACCEPTANCE_OR_CLOSE",
            packet.SafeNextStep);
    }

    [TestMethod]
    public void AcceptanceEvidenceRejectsMissingRouteResponseFailClosed()
    {
        var packet = new ChromeLabLocalDevOperatorSurfaceRouteAcceptanceEvidence().Evaluate(null);

        Assert.AreEqual(ChromeLabLocalDevOperatorSurfaceRouteAcceptanceDecision.Rejected, packet.Decision);
        CollectionAssert.AreEqual(new[] { "missing-route-response" }, packet.Findings.ToArray());
        Assert.AreEqual(0, packet.StatusCode);
        Assert.IsTrue(packet.LocalDevOnly);
        Assert.IsTrue(packet.LoopbackOnly);
        Assert.IsTrue(packet.ReadOnly);
        Assert.IsTrue(packet.FailClosed);
        Assert.IsTrue(packet.CacheDisabled);
        Assert.IsFalse(packet.PayloadAvailable);
        Assert.IsTrue(packet.ActionDisabled);
        Assert.IsFalse(packet.ActionExecutable);
        Assert.IsTrue(packet.ActionWiringAbsent);
        Assert.IsTrue(packet.UnsafeCapabilitiesUnavailable);
        Assert.IsFalse(packet.ReleaseCommercialReady);
    }

    [TestMethod]
    public void AcceptanceEvidenceRejectsNonLoopbackRouteResultWithoutOpeningPayload()
    {
        var route = new ChromeLabLocalDevOperatorSurfaceReadOnlyRoute().Handle(IPAddress.Parse("192.168.1.55"));
        var packet = new ChromeLabLocalDevOperatorSurfaceRouteAcceptanceEvidence().Evaluate(route);

        Assert.AreEqual(ChromeLabLocalDevOperatorSurfaceRouteAcceptanceDecision.Rejected, packet.Decision);
        Assert.IsTrue(packet.Findings.Contains("route-preview-not-served"));
        Assert.IsTrue(packet.Findings.Contains("route-status-not-ok"));
        Assert.IsTrue(packet.Findings.Contains("accepted-route-payload-missing"));
        Assert.IsTrue(packet.Findings.Contains("single-action-preview-required"));
        Assert.IsFalse(packet.PayloadAvailable);
        Assert.IsTrue(packet.ActionDisabled);
        Assert.IsFalse(packet.ActionExecutable);
        Assert.IsTrue(packet.ActionWiringAbsent);
        Assert.IsTrue(packet.UnsafeCapabilitiesUnavailable);
        Assert.IsFalse(packet.ReleaseCommercialReady);
    }
}
