using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserRuntime.Reliability;

namespace OneBrain.Runtime.Tests;

[TestClass]
public sealed class BrowserBridgeReliabilityTests
{
    [TestMethod]
    [TestCategory("AiriSelectiveRuntime")]
    public void ReconnectPolicyIsBoundedAndRejectsRunawayAttempts()
    {
        var policy = BrowserReconnectPolicy.Default with { MaximumJitter = TimeSpan.Zero };

        Assert.AreEqual(TimeSpan.FromMilliseconds(250), policy.DelayForAttempt(1));
        Assert.AreEqual(TimeSpan.FromSeconds(4), policy.DelayForAttempt(5));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => policy.DelayForAttempt(6));
    }

    [TestMethod]
    [TestCategory("AiriSelectiveRuntime")]
    public async Task CorrelatedCommandAcceptsOneResponseAndRejectsLateDuplicate()
    {
        var registry = new PendingBrowserCommandRegistry(["dom.read"]);
        registry.Begin("req-1", "corr-1", "dom.read", TimeSpan.FromSeconds(1));

        Assert.IsTrue(registry.TryComplete(new BrowserCommandResponse("req-1", "corr-1", true, "{}", null)));
        Assert.IsFalse(registry.TryComplete(new BrowserCommandResponse("req-1", "corr-1", true, "late", null)));
        var result = await registry.WaitAsync("req-1");

        Assert.IsTrue(result.Success);
        Assert.AreEqual("{}", result.Response?.Payload);
        Assert.AreEqual(0, registry.PendingCount);
    }

    [TestMethod]
    [TestCategory("AiriSelectiveRuntime")]
    public async Task CommandTimeoutAndCancellationAreExplicit()
    {
        var timeoutRegistry = new PendingBrowserCommandRegistry(["dom.read"]);
        timeoutRegistry.Begin("timeout", "corr", "dom.read", TimeSpan.FromMilliseconds(25));
        var timedOut = await timeoutRegistry.WaitAsync("timeout");
        Assert.IsTrue(timedOut.TimedOut);

        var cancelRegistry = new PendingBrowserCommandRegistry(["dom.read"]);
        cancelRegistry.Begin("cancel", "corr", "dom.read", TimeSpan.FromSeconds(2));
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var cancelled = await cancelRegistry.WaitAsync("cancel", cancellation.Token);
        Assert.IsTrue(cancelled.Cancelled);
    }

    [TestMethod]
    [TestCategory("AiriSelectiveRuntime")]
    public async Task PayloadLimitFailsClosedWithoutArbitraryCommandExecution()
    {
        var registry = new PendingBrowserCommandRegistry(["dom.read"]);
        Assert.ThrowsException<InvalidOperationException>(() =>
            registry.Begin("bad", "corr", "arbitrary.eval", TimeSpan.FromSeconds(1)));
        registry.Begin("req", "corr", "dom.read", TimeSpan.FromSeconds(1), maximumResponseBytes: 4);
        Assert.IsTrue(registry.TryComplete(new BrowserCommandResponse("req", "corr", true, "12345", null)));

        var result = await registry.WaitAsync("req");

        Assert.IsFalse(result.Success);
        Assert.IsTrue(result.SafeMessage.Contains("payload limit", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    [TestCategory("AiriSelectiveRuntime")]
    public void NestedFrameOffsetsResolveToGlobalCoordinates()
    {
        var frames = new[]
        {
            new BrowserFrameDescriptor("root", null, new Uri("https://example.invalid"), 0, 0, true, true),
            new BrowserFrameDescriptor("child", "root", new Uri("https://example.invalid/frame"), 10, 20, true, true),
            new BrowserFrameDescriptor("nested", "child", new Uri("https://example.invalid/nested"), 5, 6, true, true)
        };
        var target = BrowserFrameLocator.ResolveGlobalTarget(
            frames,
            new BrowserFrameElementCandidate("nested", "#target", 2, 3, 20, 10));

        Assert.AreEqual(17d, target.GlobalX);
        Assert.AreEqual(29d, target.GlobalY);
        CollectionAssert.AreEqual(new[] { "root", "child", "nested" }, target.FramePath.ToArray());
    }

    [TestMethod]
    [TestCategory("AiriSelectiveRuntime")]
    public void DetachedOrCyclicFramesFailClosed()
    {
        var detached = new[]
        {
            new BrowserFrameDescriptor("root", null, new Uri("https://example.invalid"), 0, 0, false, true)
        };
        Assert.ThrowsException<InvalidOperationException>(() =>
            BrowserFrameLocator.ResolveGlobalTarget(detached, new BrowserFrameElementCandidate("root", "#x", 0, 0, 1, 1)));

        var cycle = new[]
        {
            new BrowserFrameDescriptor("a", "b", new Uri("https://a.invalid"), 0, 0, true, true),
            new BrowserFrameDescriptor("b", "a", new Uri("https://b.invalid"), 0, 0, true, true)
        };
        Assert.ThrowsException<InvalidOperationException>(() =>
            BrowserFrameLocator.ResolveGlobalTarget(cycle, new BrowserFrameElementCandidate("a", "#x", 0, 0, 1, 1)));
    }

    [TestMethod]
    [TestCategory("AiriSelectiveRuntime")]
    public async Task WaitForElementPollsWithoutBusyLoopAndReturnsLastError()
    {
        var calls = 0;
        var found = await BrowserElementWaiter.WaitAsync(
            _ => ValueTask.FromResult(++calls == 3
                ? new BrowserElementProbeResult(true, new BrowserFrameElementCandidate("root", "#ready", 1, 1, 1, 1), null)
                : new BrowserElementProbeResult(false, null, "not ready")),
            TimeSpan.FromSeconds(1),
            TimeSpan.FromMilliseconds(1),
            delay: (_, _) => ValueTask.CompletedTask);

        Assert.IsTrue(found.Found);
        Assert.AreEqual(3, found.Attempts);

        var timedOut = await BrowserElementWaiter.WaitAsync(
            _ => ValueTask.FromResult(new BrowserElementProbeResult(false, null, "still missing")),
            TimeSpan.FromMilliseconds(30),
            TimeSpan.FromMilliseconds(5));
        Assert.IsTrue(timedOut.TimedOut);
        Assert.AreEqual("still missing", timedOut.LastError);
    }

    [TestMethod]
    [TestCategory("AiriSelectiveRuntime")]
    public async Task ControlMessagesRemainAvailableWhenDataPlaneIsSaturated()
    {
        var channels = new BrowserTransportChannels(controlCapacity: 2, dataCapacity: 1);
        Assert.IsTrue(channels.TryWriteData(new BrowserDataMessage("snapshot", "1", new byte[] { 1 })));
        Assert.IsTrue(channels.TryWriteData(new BrowserDataMessage("snapshot", "2", new byte[] { 2 })));
        await channels.WriteControlAsync(new BrowserControlMessage("cancel", "control", "stop"));

        var control = await channels.ReadControlAsync();
        var data = await channels.ReadDataAsync();

        Assert.AreEqual("cancel", control.Kind);
        Assert.AreEqual("2", data.CorrelationId);
    }
}
