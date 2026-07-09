using System.Net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.ChromeLab.Bridge;

namespace OneBrain.Safety.Tests;

[TestClass]
public sealed class ChromeLabLocalDevOperatorSurfaceReadOnlyRouteTests
{
    [TestMethod]
    public void LoopbackRouteServesAcceptedReadOnlySurfaceAndEvidence()
    {
        var response = new ChromeLabLocalDevOperatorSurfaceReadOnlyRoute().Handle(IPAddress.Loopback);

        Assert.AreEqual(ChromeLabLocalDevOperatorSurfaceRouteDecision.ServedReadOnlyPreview, response.Decision);
        Assert.AreEqual(200, response.StatusCode);
        Assert.AreEqual("chromelab.local-dev.operator-surface.route.v1", response.RouteId);
        Assert.AreEqual("/operator/local-dev/chromelab", response.RoutePath);
        Assert.AreEqual("GET", response.Method);
        Assert.IsTrue(response.LocalDevOnly);
        Assert.IsTrue(response.LoopbackOnly);
        Assert.IsTrue(response.ReadOnly);
        Assert.IsTrue(response.FailClosed);
        Assert.IsTrue(response.CacheDisabled);
        Assert.IsTrue(response.PayloadAvailable);
        Assert.IsNotNull(response.Surface);
        Assert.IsNotNull(response.Acceptance);
        Assert.AreEqual(
            ChromeLabLocalDevOperatorSurfaceDecision.RenderedPreview,
            response.Surface.Decision);
        Assert.AreEqual(
            ChromeLabLocalDevOperatorSurfaceAcceptanceDecision.Accepted,
            response.Acceptance.Decision);
        Assert.AreEqual(27, response.Acceptance.ReadinessPercentage);
        Assert.IsTrue(response.Acceptance.ActionDisabled);
        Assert.IsFalse(response.Acceptance.ActionExecutable);
        Assert.IsTrue(response.Acceptance.ActionWiringAbsent);
        Assert.IsTrue(response.Acceptance.UnsafeCapabilitiesUnavailable);
        Assert.IsFalse(response.Acceptance.ReleaseCommercialReady);
        Assert.AreEqual(
            "CHROMELAB_LOCAL_DEV_OPERATOR_SURFACE_ROUTE_ACCEPTANCE_OR_CLOSE",
            response.SafeNextStep);
    }

    [TestMethod]
    public void Ipv6LoopbackIsAcceptedByTheSameBoundedRoute()
    {
        var response = new ChromeLabLocalDevOperatorSurfaceReadOnlyRoute().Handle(IPAddress.IPv6Loopback);

        Assert.AreEqual(ChromeLabLocalDevOperatorSurfaceRouteDecision.ServedReadOnlyPreview, response.Decision);
        Assert.AreEqual(200, response.StatusCode);
        Assert.IsTrue(response.PayloadAvailable);
        Assert.IsNotNull(response.Acceptance);
        Assert.AreEqual(
            ChromeLabLocalDevOperatorSurfaceAcceptanceDecision.Accepted,
            response.Acceptance.Decision);
    }

    [TestMethod]
    public void NonLoopbackAndMissingRemoteAddressesAreRejectedWithoutPayload()
    {
        foreach (var address in new IPAddress?[] { null, IPAddress.Parse("192.168.10.25"), IPAddress.Parse("10.0.0.8") })
        {
            var response = new ChromeLabLocalDevOperatorSurfaceReadOnlyRoute().Handle(address);

            Assert.AreEqual(ChromeLabLocalDevOperatorSurfaceRouteDecision.RejectedNotLocal, response.Decision);
            Assert.AreEqual(404, response.StatusCode);
            Assert.IsTrue(response.LocalDevOnly);
            Assert.IsTrue(response.LoopbackOnly);
            Assert.IsTrue(response.ReadOnly);
            Assert.IsTrue(response.FailClosed);
            Assert.IsTrue(response.CacheDisabled);
            Assert.IsFalse(response.PayloadAvailable);
            Assert.IsNull(response.Surface);
            Assert.IsNull(response.Acceptance);
            Assert.AreEqual(
                "USE_LOOPBACK_FOR_CHROMELAB_LOCAL_DEV_OPERATOR_SURFACE",
                response.SafeNextStep);
        }
    }

    [TestMethod]
    public void ProgramRegistersTheRouteExactlyOnceThroughTheReadOnlyGetExtension()
    {
        var program = File.ReadAllText(Path.Combine(
            FindRepoRoot(),
            "src",
            "OneBrain.ChromeLab.Bridge",
            "Program.cs"));
        const string registration = "app.MapChromeLabLocalDevOperatorSurfaceReadOnlyRoute();";

        Assert.AreEqual(1, Count(program, registration));
    }

    [TestMethod]
    public void RouteSourceKeepsLoopbackGetNoStoreAndNoMutationVerbBoundary()
    {
        var source = File.ReadAllText(Path.Combine(
            FindRepoRoot(),
            "src",
            "OneBrain.ChromeLab.Bridge",
            "ChromeLabLocalDevOperatorSurfaceReadOnlyRoute.cs"));

        Assert.IsTrue(source.Contains("IPAddress.IsLoopback", StringComparison.Ordinal));
        Assert.IsTrue(source.Contains("endpoints.MapGet", StringComparison.Ordinal));
        Assert.IsTrue(source.Contains("RemoteIpAddress", StringComparison.Ordinal));
        Assert.IsTrue(source.Contains("no-store", StringComparison.Ordinal));
        Assert.IsFalse(source.Contains("endpoints.MapPost", StringComparison.Ordinal));
        Assert.IsFalse(source.Contains("endpoints.MapPut", StringComparison.Ordinal));
        Assert.IsFalse(source.Contains("endpoints.MapDelete", StringComparison.Ordinal));
    }

    private static int Count(string source, string value)
    {
        var count = 0;
        var offset = 0;
        while ((offset = source.IndexOf(value, offset, StringComparison.Ordinal)) >= 0)
        {
            count++;
            offset += value.Length;
        }

        return count;
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
