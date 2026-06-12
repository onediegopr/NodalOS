using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Models;
using OneBrain.Observation.Uia;

namespace OneBrain.Recipes.Tests;

[TestClass]
public sealed class RuntimeIdWaitEventsTests
{
    [TestMethod]
    public void RuntimeId_Array_Formats_With_Dots()
    {
        Assert.AreEqual("42.66818.4.13", UiaTreeWalker.FormatRuntimeId([42, 66818, 4, 13]));
    }

    [TestMethod]
    public void RuntimeId_Null_Or_Empty_Formats_As_Empty_String()
    {
        Assert.AreEqual("", UiaTreeWalker.FormatRuntimeId(null));
        Assert.AreEqual("", UiaTreeWalker.FormatRuntimeId([]));
    }

    [TestMethod]
    public void Old_UiElementSnapshot_Json_Without_RuntimeId_Deserializes_With_Default()
    {
        var json = """
        {
          "Ref": "@e1",
          "Role": "Button",
          "Name": "Enviar",
          "AutomationId": "Send",
          "ClassName": "Button",
          "Bounds": { "Left": 1, "Top": 2, "Right": 3, "Bottom": 4 },
          "IsEnabled": true,
          "IsOffscreen": false,
          "IsKeyboardFocusable": true,
          "Patterns": [ "Invoke" ],
          "Actions": [ "invoke" ]
        }
        """;

        var snapshot = JsonSerializer.Deserialize<UiElementSnapshot>(json);

        Assert.IsNotNull(snapshot);
        Assert.AreEqual("", snapshot.RuntimeId);
    }

    [TestMethod]
    public void ElementIdentity_Matches_Strong_When_RuntimeId_Is_Present()
    {
        var a = new ElementIdentity("1.2.3", "Button", "Enviar", "Send");
        var b = new ElementIdentity("1.2.3", "Edit", "Otro", "Other");

        Assert.IsTrue(a.IsStrong);
        Assert.IsTrue(a.MatchesStrong(b));
    }

    [TestMethod]
    public void ElementIdentity_Matches_Weak_When_Role_Name_And_AutomationId_Match()
    {
        var a = new ElementIdentity("", "Button", "Enviar", "Send");
        var b = new ElementIdentity("", "Button", "Enviar", "Send");

        Assert.IsFalse(a.IsStrong);
        Assert.IsTrue(a.MatchesWeak(b));
    }

    [TestMethod]
    public void Snapshot_Cache_Property_Set_Includes_RuntimeId()
    {
        CollectionAssert.Contains(UiaSnapshotPropertySet.DefaultPropertyNames.ToList(), "RuntimeId");
    }

    [TestMethod]
    public void WaitCore_Timeout_When_Condition_Never_Matches()
    {
        var result = UiaEventWaiter.WaitCore(
            () => new UiaWaitCheck(false, "", null),
            _ => new DisposableStub(),
            timeoutMs: 10,
            intervalMs: 1,
            CancellationToken.None);

        Assert.IsFalse(result.Success);
        Assert.IsTrue(result.UsedEvents);
        Assert.IsFalse(result.FellBackToPolling);
    }

    [TestMethod]
    public void WaitCore_Falls_Back_To_Polling_When_Subscription_Fails()
    {
        var result = UiaEventWaiter.WaitCore(
            () => new UiaWaitCheck(false, "", null),
            _ => throw new InvalidOperationException("subscription failed"),
            timeoutMs: 10,
            intervalMs: 1,
            CancellationToken.None);

        Assert.IsFalse(result.Success);
        Assert.IsFalse(result.UsedEvents);
        Assert.IsTrue(result.FellBackToPolling);
    }

    [TestMethod]
    public void WaitCore_ForcePolling_Does_Not_Use_Events()
    {
        var result = UiaEventWaiter.WaitCore(
            () => new UiaWaitCheck(false, "", null),
            _ => throw new InvalidOperationException("must not subscribe"),
            timeoutMs: 10,
            intervalMs: 1,
            CancellationToken.None,
            forcePolling: true);

        Assert.IsFalse(result.Success);
        Assert.IsFalse(result.UsedEvents);
        Assert.IsFalse(result.FellBackToPolling);
    }

    [TestMethod]
    public void WaitCore_Does_Not_Throw_When_Subscription_Fails()
    {
        var result = UiaEventWaiter.WaitCore(
            () => new UiaWaitCheck(false, "", null),
            _ => throw new NotSupportedException("provider rejected event subscription"),
            timeoutMs: 10,
            intervalMs: 1,
            CancellationToken.None);

        Assert.IsFalse(result.Success);
        Assert.IsTrue(result.FellBackToPolling);
    }

    [TestMethod]
    public void Hito_Documentation_States_Event_Scope_And_Fallback()
    {
        var root = FindRepoRoot();
        var doc = File.ReadAllText(Path.Combine(root, "docs", "hitos", "hito-116-117-runtimeid-wait-events-v0.md"));

        StringAssert.Contains(doc, "RuntimeId");
        StringAssert.Contains(doc, "PropertyChangedEvent(Name)");
        StringAssert.Contains(doc, "WindowOpenedEvent");
        StringAssert.Contains(doc, "fallback");
        StringAssert.Contains(doc, "no clicks reales");
        StringAssert.Contains(doc, "no playback libre");
    }

    private sealed class DisposableStub : IDisposable
    {
        public void Dispose()
        {
        }
    }

    private static string FindRepoRoot()
    {
        var dir = AppContext.BaseDirectory;
        while (dir is not null && !File.Exists(Path.Combine(dir, "OneBrain.slnx")))
            dir = Directory.GetParent(dir)?.FullName;

        Assert.IsNotNull(dir);
        return dir;
    }
}
