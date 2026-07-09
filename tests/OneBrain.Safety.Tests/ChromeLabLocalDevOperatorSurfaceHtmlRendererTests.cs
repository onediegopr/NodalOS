using System.Net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.ChromeLab.Bridge;

namespace OneBrain.Safety.Tests;

[TestClass]
public sealed class ChromeLabLocalDevOperatorSurfaceHtmlRendererTests
{
    [TestMethod]
    public void RendererProducesVisibleReadOnlyOperatorSurfaceFromAcceptedRoute()
    {
        var route = new ChromeLabLocalDevOperatorSurfaceReadOnlyRoute().Handle(IPAddress.Loopback);
        var result = new ChromeLabLocalDevOperatorSurfaceHtmlRenderer().Render(route);

        Assert.AreEqual(ChromeLabLocalDevOperatorSurfaceHtmlDecision.Rendered, result.Decision);
        Assert.AreEqual(200, result.StatusCode);
        Assert.AreEqual("text/html; charset=utf-8", result.ContentType);
        Assert.AreEqual(0, result.Findings.Count);
        Assert.IsTrue(result.LocalDevOnly);
        Assert.IsTrue(result.ReadOnly);
        Assert.IsTrue(result.FailClosed);
        Assert.IsTrue(result.ScriptsAbsent);
        Assert.IsTrue(result.FormsAbsent);
        Assert.IsTrue(result.ExternalResourcesAbsent);
        Assert.IsTrue(result.Html.Contains("ChromeLab Local/Dev Operator Surface", StringComparison.Ordinal));
        Assert.IsTrue(result.Html.Contains("Readiness: <strong>27%</strong>", StringComparison.Ordinal));
        Assert.IsTrue(result.Html.Contains("data-section-id=\"status\"", StringComparison.Ordinal));
        Assert.IsTrue(result.Html.Contains("data-section-id=\"limits\"", StringComparison.Ordinal));
        Assert.IsTrue(result.Html.Contains("data-section-id=\"blockers\"", StringComparison.Ordinal));
        Assert.IsTrue(result.Html.Contains("data-section-id=\"operator-signal\"", StringComparison.Ordinal));
        Assert.IsTrue(result.Html.Contains("data-section-id=\"safe-next-step\"", StringComparison.Ordinal));
        Assert.IsTrue(result.Html.Contains("CHROMELAB_LIVE_BROWSER_EXECUTION_AUTHORITY", StringComparison.Ordinal));
        Assert.IsTrue(result.Html.Contains("explicit-chromelab-local-dev-frontier", StringComparison.Ordinal));
        Assert.IsTrue(result.Html.Contains("aria-disabled=\"true\"", StringComparison.Ordinal));
        Assert.IsTrue(result.Html.Contains("Action unavailable in this read-only surface.", StringComparison.Ordinal));
        Assert.AreEqual(
            "CHROMELAB_LOCAL_DEV_OPERATOR_HTML_VIEW_ACCEPTANCE_OR_CLOSE",
            result.SafeNextStep);
    }

    [TestMethod]
    public void RendererRejectsMissingOrNonLoopbackRouteResponseFailClosed()
    {
        var renderer = new ChromeLabLocalDevOperatorSurfaceHtmlRenderer();
        var missing = renderer.Render(null);
        var nonLoopback = renderer.Render(
            new ChromeLabLocalDevOperatorSurfaceReadOnlyRoute().Handle(IPAddress.Parse("10.2.3.4")));

        Assert.AreEqual(ChromeLabLocalDevOperatorSurfaceHtmlDecision.Rejected, missing.Decision);
        Assert.AreEqual(503, missing.StatusCode);
        Assert.IsTrue(missing.Findings.Contains("missing-route-response"));
        Assert.IsTrue(missing.ScriptsAbsent);
        Assert.IsTrue(missing.FormsAbsent);
        Assert.IsTrue(missing.ExternalResourcesAbsent);

        Assert.AreEqual(ChromeLabLocalDevOperatorSurfaceHtmlDecision.Rejected, nonLoopback.Decision);
        Assert.AreEqual(503, nonLoopback.StatusCode);
        Assert.IsTrue(nonLoopback.Findings.Contains("route-preview-not-served"));
        Assert.IsTrue(nonLoopback.Findings.Contains("route-payload-unavailable"));
        Assert.IsTrue(nonLoopback.Findings.Contains("surface-or-acceptance-missing"));
        Assert.IsTrue(nonLoopback.ScriptsAbsent);
        Assert.IsTrue(nonLoopback.FormsAbsent);
        Assert.IsTrue(nonLoopback.ExternalResourcesAbsent);
    }

    [TestMethod]
    public void EndpointRegistrationKeepsHtmlViewGetOnlyAndNoStore()
    {
        var source = File.ReadAllText(Path.Combine(
            FindRepoRoot(),
            "src",
            "OneBrain.ChromeLab.Bridge",
            "ChromeLabLocalDevOperatorSurfaceReadOnlyRoute.cs"));

        Assert.AreEqual(1, Count(source, "ChromeLabLocalDevOperatorSurfaceHtmlRenderer.RoutePath"));
        Assert.IsTrue(source.Contains("Results.Content", StringComparison.Ordinal));
        Assert.IsTrue(source.Contains("CacheControl = \"no-store\"", StringComparison.Ordinal));
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
