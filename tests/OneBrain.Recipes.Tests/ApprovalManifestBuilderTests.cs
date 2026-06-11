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
    public void ExecutionAllowedInThisHito_Always_False()
    {
        foreach (var text in new[] { "Comprar ahora", "Ver descripción", "Categorías", "foo-bar" })
        {
            var pr = ClickPreflightEvaluator.Evaluate(text);
            var m = ApprovalManifestBuilder.Build(pr);
            Assert.IsFalse(m.ExecutionAllowedInThisHito, $"Expected false for '{text}'");
        }
    }
}
