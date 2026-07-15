using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.AgentOperations.Core.Capabilities;

namespace OneBrain.Runtime.Tests;

[TestClass]
public sealed class CapabilityRegistryTests
{
    [TestMethod]
    [TestCategory("AiriSelectiveRuntime")]
    public void RegisterGetAndListExposeOnlyRealRecords()
    {
        var registry = new CapabilityRegistry();
        registry.Register(Record("browser.dom.read", "cloakbrowser", CapabilityState.Ready, 90));

        Assert.IsTrue(registry.TryGet("browser.dom.read", "cloakbrowser", null, out var record));
        Assert.AreEqual(CapabilityState.Ready, record.State);
        Assert.AreEqual(1, registry.List().Count);
    }

    [TestMethod]
    [TestCategory("AiriSelectiveRuntime")]
    public void SelectionPrefersReadyThenUsesDegradedFallback()
    {
        var registry = new CapabilityRegistry();
        registry.Register(Record("model.chat", "provider-a", CapabilityState.Degraded, 95, CapabilityRuntime.Model));
        registry.Register(Record("model.chat", "provider-b", CapabilityState.Ready, 80, CapabilityRuntime.Model));

        var selection = registry.Select(new CapabilitySelectionRequest(
            "model.chat",
            Array.Empty<string>(),
            [CapabilityRuntime.Model]));

        Assert.IsTrue(selection.Available);
        Assert.AreEqual("provider-b", selection.Selected?.ProviderId);
        Assert.AreEqual("provider-a", selection.Fallbacks.Single().ProviderId);
    }

    [TestMethod]
    [TestCategory("AiriSelectiveRuntime")]
    public void ProviderAndRuntimeScopeCannotBeBypassed()
    {
        var registry = new CapabilityRegistry();
        registry.Register(Record("browser.action.execute", "chromelab", CapabilityState.Ready, 100));

        var selection = registry.Select(new CapabilitySelectionRequest(
            "browser.action.execute",
            ["cloakbrowser"],
            [CapabilityRuntime.Browser]));

        Assert.IsFalse(selection.Available);
        Assert.AreEqual("CAPABILITY_UNAVAILABLE", selection.Decision);
    }

    [TestMethod]
    [TestCategory("AiriSelectiveRuntime")]
    public void DegradedCapabilityDoesNotBreakUnrelatedRuntimeSelection()
    {
        var registry = new CapabilityRegistry();
        registry.Register(Record("browser.dom.read", "cloakbrowser", CapabilityState.Degraded, 50));
        registry.Register(Record("filesystem.read", "core", CapabilityState.Ready, 100, CapabilityRuntime.Filesystem));

        var filesystem = registry.Select(new CapabilitySelectionRequest(
            "filesystem.read",
            Array.Empty<string>(),
            [CapabilityRuntime.Filesystem]));

        Assert.IsTrue(filesystem.Available);
        Assert.AreEqual("core", filesystem.Selected?.ProviderId);
    }

    [TestMethod]
    [TestCategory("AiriSelectiveRuntime")]
    public void WithdrawnCapabilityIsNotSelectedAndCannotBeSilentlyReactivated()
    {
        var registry = new CapabilityRegistry();
        registry.Register(Record("verification.run", "core", CapabilityState.Ready, 100, CapabilityRuntime.Verification));
        Assert.IsTrue(registry.Withdraw("verification.run", "core", null));

        var selection = registry.Select(new CapabilitySelectionRequest(
            "verification.run",
            Array.Empty<string>(),
            [CapabilityRuntime.Verification]));

        Assert.IsFalse(selection.Available);
        Assert.ThrowsExactly<InvalidOperationException>(() =>
            registry.UpdateHealth("verification.run", "core", null, CapabilityState.Ready, 100));
    }

    private static CapabilityRecord Record(
        string capability,
        string provider,
        CapabilityState state,
        int health,
        CapabilityRuntime runtime = CapabilityRuntime.Browser) =>
        new(
            capability,
            provider,
            null,
            runtime,
            state,
            health,
            "1",
            new Dictionary<string, string> { ["scope"] = "local" });
}
