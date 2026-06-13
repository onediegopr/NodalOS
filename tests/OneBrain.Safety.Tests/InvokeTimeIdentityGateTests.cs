using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Contracts;
using OneBrain.Core.Execution;
using OneBrain.Core.Models;

namespace OneBrain.Safety.Tests;

[TestClass]
public sealed class InvokeTimeIdentityGateTests
{
    [TestMethod]
    public void InvokeTimeGateAllowsSame()
    {
        var expected = CreateIdentity();
        var observed = CreateIdentity();

        var decision = InvokeTimeIdentityGate.Evaluate(expected, observed);

        Assert.IsTrue(decision.Checked);
        Assert.IsTrue(decision.Allowed);
        Assert.AreEqual("Same", decision.Verdict);
    }

    [TestMethod]
    public void InvokeTimeGateBlocksMissingExpected()
    {
        var decision = InvokeTimeIdentityGate.Evaluate(null, CreateIdentity());

        Assert.IsFalse(decision.Allowed);
        Assert.AreEqual("InvokeTimeExpectedIdentityRequired", decision.Reason);
    }

    [TestMethod]
    public void InvokeTimeGateBlocksWeakExpected()
    {
        var decision = InvokeTimeIdentityGate.Evaluate(CreateIdentity(runtimeId: ""), CreateIdentity());

        Assert.IsFalse(decision.Allowed);
        Assert.AreEqual("InvokeTimeExpectedIdentityRequired", decision.Reason);
    }

    [TestMethod]
    public void InvokeTimeGateBlocksMissingObserved()
    {
        var decision = InvokeTimeIdentityGate.Evaluate(CreateIdentity(), null);

        Assert.IsFalse(decision.Allowed);
        Assert.AreEqual("InvokeTimeObservedIdentityRequired", decision.Reason);
    }

    [TestMethod]
    public void InvokeTimeGateBlocksWeakObserved()
    {
        var decision = InvokeTimeIdentityGate.Evaluate(CreateIdentity(), CreateIdentity(runtimeId: ""));

        Assert.IsFalse(decision.Allowed);
        Assert.AreEqual("InvokeTimeObservedIdentityRequired", decision.Reason);
    }

    [TestMethod]
    public void InvokeTimeGateBlocksLikelySame()
    {
        var decision = InvokeTimeIdentityGate.Evaluate(
            CreateIdentity(runtimeId: "old-runtime"),
            CreateIdentity(runtimeId: "new-runtime"));

        Assert.IsFalse(decision.Allowed);
        Assert.AreEqual("InvokeTimeIdentityMismatch", decision.Reason);
        Assert.AreEqual("LikelySame", decision.Verdict);
    }

    [TestMethod]
    public void InvokeTimeGateBlocksDifferent()
    {
        var decision = InvokeTimeIdentityGate.Evaluate(
            CreateIdentity(runtimeId: "old-runtime", automationId: "old", name: "Old"),
            CreateIdentity(runtimeId: "new-runtime", automationId: "new", name: "New"));

        Assert.IsFalse(decision.Allowed);
        Assert.AreEqual("InvokeTimeIdentityMismatch", decision.Reason);
    }

    [TestMethod]
    public void InvokeTimeGateBlocksAmbiguous()
    {
        var weakExpected = CreateIdentity(runtimeId: "");
        var decision = InvokeTimeIdentityGate.Evaluate(weakExpected, CreateIdentity(runtimeId: ""));

        Assert.IsFalse(decision.Allowed);
        Assert.AreEqual("InvokeTimeExpectedIdentityRequired", decision.Reason);
    }

    [TestMethod]
    public void InvokeTimeGateBlocksUnknown()
    {
        var decision = InvokeTimeIdentityGate.Evaluate(
            CreateIdentity(runtimeId: "old-runtime", automationId: "old"),
            CreateIdentity(runtimeId: "new-runtime", automationId: "", name: ""));

        Assert.IsFalse(decision.Allowed);
        Assert.AreEqual("InvokeTimeIdentityMismatch", decision.Reason);
    }

    [TestMethod]
    public void UiaPatternExecutorStillUsesInvokeTimeGate()
    {
        var path = Path.Combine(
            FindRepoRoot(),
            "src",
            "OneBrain.Actions",
            "Uia",
            "UiaPatternExecutor.cs");

        var source = File.ReadAllText(path);

        StringAssert.Contains(source, "InvokeTimeIdentityGate.Evaluate");
    }

    private static ElementIdentity CreateIdentity(
        string runtimeId = "42.1.9",
        string automationId = "more-information-link",
        string name = "More information...")
    {
        return new ElementIdentity(runtimeId, "Hyperlink", name, automationId)
        {
            Role = "Hyperlink",
            ControlType = "Hyperlink",
            ClassName = "Chrome_RenderWidgetHostHWND",
            AncestorPath = "Window:ONE Brain > Pane:Catalog > Document:Main",
            Provenance = Provenance.Uia
        };
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
