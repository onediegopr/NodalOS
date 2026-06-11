using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Cli.Browser;

namespace OneBrain.Recipes.Tests;

[TestClass]
public sealed class BrowserSelectionTests
{
    [TestMethod]
    public void Resolve_Edge_Returns_msedge()
    {
        var result = BrowserSession.ResolveProcessName("edge");
        Assert.AreEqual("msedge", result);
    }

    [TestMethod]
    public void Resolve_Chrome_Returns_chrome()
    {
        var result = BrowserSession.ResolveProcessName("chrome");
        Assert.AreEqual("chrome", result);
    }

    [TestMethod]
    public void Resolve_Firefox_Returns_firefox()
    {
        var result = BrowserSession.ResolveProcessName("firefox");
        Assert.AreEqual("firefox", result);
    }

    [TestMethod]
    public void Resolve_Firefox_CaseInsensitive()
    {
        var result = BrowserSession.ResolveProcessName("Firefox");
        Assert.AreEqual("firefox", result);
    }

    [TestMethod]
    public void Resolve_Unknown_Returns_Null()
    {
        var result = BrowserSession.ResolveProcessName("safari");
        Assert.IsNull(result);
    }

    [TestMethod]
    public void Resolve_Empty_Returns_Null()
    {
        var result = BrowserSession.ResolveProcessName("");
        Assert.IsNull(result);
    }
}
