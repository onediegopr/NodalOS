using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Safety;

namespace OneBrain.Recipes.Tests;

[TestClass]
public sealed class ApprovalManifestBuilderTests
{
    [TestMethod]
    public void Blocked_Preflight_Produces_Blocked_Manifest()
    {
        var pr = ClickPreflightEvaluator.Evaluate("Comprar ahora");
        var m = ApprovalManifestBuilder.Build(pr);
        Assert.IsTrue(m.Blocked);
        Assert.IsFalse(m.Allowed);
        Assert.IsFalse(m.ExecutionAllowedInThisHito);
    }

    [TestMethod]
    public void RequiresApproval_Produces_Required_Manifest()
    {
        var pr = ClickPreflightEvaluator.Evaluate("Ver descripción");
        var m = ApprovalManifestBuilder.Build(pr);
        Assert.IsTrue(m.Required);
        Assert.IsFalse(m.Blocked);
        Assert.IsFalse(m.ExecutionAllowedInThisHito);
    }

    [TestMethod]
    public void Manifest_Has_V2_PolicyVersion()
    {
        var pr = ClickPreflightEvaluator.Evaluate("Comprar ahora");
        var m = ApprovalManifestBuilder.Build(pr);
        Assert.AreEqual("approval-v2", m.PolicyVersion);
    }

    [TestMethod]
    public void Manifest_HumanReadable_Not_Empty()
    {
        var pr = ClickPreflightEvaluator.Evaluate("Comprar ahora");
        var m = ApprovalManifestBuilder.Build(pr);
        Assert.IsTrue(m.HumanReadableText.Length > 0);
        StringAssert.Contains(m.HumanReadableText, "BLOCKED");
    }

    [TestMethod]
    public void RequiresReview_Never_Executable()
    {
        foreach (var mode in new[] { "controlled", "nonCommercialWeb", "commercialWeb" })
        {
            var pr = ClickPreflightEvaluator.Evaluate("foo-bar");
            var m = ApprovalManifestBuilder.Build(pr, mode);
            Assert.IsTrue(pr.RequiresReview);
            Assert.IsFalse(m.ExecutionAllowedInThisHito, $"Expected false for mode '{mode}'");
        }
    }

    [TestMethod]
    public void Controlled_Navigation_Can_Be_Executable()
    {
        var pr = ClickPreflightEvaluator.Evaluate("Ver descripción");
        var m = ApprovalManifestBuilder.Build(pr, "controlled");
        Assert.AreEqual("requiresApproval", pr.Decision);
        Assert.IsTrue(m.ExecutionAllowedInThisHito);
    }

    [TestMethod]
    public void Manifest_Includes_Binding_Fields()
    {
        var pr = ClickPreflightEvaluator.Evaluate("Ver descripción");
        var m = ApprovalManifestBuilder.Build(pr, "nonCommercialWeb");

        Assert.AreEqual("Ver descripción", m.TargetText);
        Assert.AreEqual("nonCommercialWeb", m.Mode);
        Assert.AreEqual("requiresApproval", m.Decision);
        Assert.AreEqual("navigation-candidate", m.RiskCategory);
        Assert.AreEqual("medium", m.RiskLevel);
        Assert.IsFalse(string.IsNullOrWhiteSpace(m.EvidenceHash));
        StringAssert.Contains(m.ManifestJson!, "\"evidenceHash\"");
    }
}
