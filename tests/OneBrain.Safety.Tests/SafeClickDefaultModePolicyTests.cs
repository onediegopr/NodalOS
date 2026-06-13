using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Execution;

namespace OneBrain.Safety.Tests;

[TestClass]
public sealed class SafeClickDefaultModePolicyTests
{
    [TestMethod]
    public void ParseDesktopEligible()
    {
        Assert.AreEqual(SafeClickDefaultMode.DesktopEligible, SafeClickDefaultModePolicy.Parse("desktop-eligible"));
        Assert.AreEqual("desktop-eligible", SafeClickDefaultModePolicy.ToWireValue(SafeClickDefaultMode.DesktopEligible));
    }

    [TestMethod]
    public void ParseAllEligible()
    {
        Assert.AreEqual(SafeClickDefaultMode.AllEligible, SafeClickDefaultModePolicy.Parse("all-eligible"));
        Assert.AreEqual("all-eligible", SafeClickDefaultModePolicy.ToWireValue(SafeClickDefaultMode.AllEligible));
    }

    [TestMethod]
    public void ParseUnknownOrEmptyIsDisabled()
    {
        Assert.AreEqual(SafeClickDefaultMode.Disabled, SafeClickDefaultModePolicy.Parse(""));
        Assert.AreEqual(SafeClickDefaultMode.Disabled, SafeClickDefaultModePolicy.Parse("desktop"));
    }
}
