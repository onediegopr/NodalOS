using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Approval;
using OneBrain.Core.Contracts;
using OneBrain.Core.Models;
using OneBrain.Core.Safety;
using OneBrain.Core.Selectors;

namespace OneBrain.Safety.Tests;

[TestClass]
public sealed class IdentityBindingHashTests
{
    [TestMethod]
    public void IdentityBindingHashIsDeterministic()
    {
        var selector = CreateSelector("categories-button", "Categorias");

        var left = ApprovalManifestBuilder.ComputeIdentityBindingHash("digest-a", selector, "click", "controlled", "web-shadow", IdentityStrength.Weak);
        var right = ApprovalManifestBuilder.ComputeIdentityBindingHash("digest-a", selector, "click", "controlled", "web-shadow", IdentityStrength.Weak);

        Assert.AreEqual(left, right);
    }

    [TestMethod]
    public void IdentityBindingHashChangesWhenDigestChanges()
    {
        var selector = CreateSelector("categories-button", "Categorias");

        var left = ApprovalManifestBuilder.ComputeIdentityBindingHash("digest-a", selector, "click", "controlled", "web-shadow", IdentityStrength.Weak);
        var right = ApprovalManifestBuilder.ComputeIdentityBindingHash("digest-b", selector, "click", "controlled", "web-shadow", IdentityStrength.Weak);

        Assert.AreNotEqual(left, right);
    }

    [TestMethod]
    public void IdentityBindingHashChangesWhenSelectorChanges()
    {
        var left = ApprovalManifestBuilder.ComputeIdentityBindingHash("digest-a", CreateSelector("categories-button", "Categorias"), "click", "controlled", "web-shadow", IdentityStrength.Weak);
        var right = ApprovalManifestBuilder.ComputeIdentityBindingHash("digest-a", CreateSelector("different-button", "Categorias"), "click", "controlled", "web-shadow", IdentityStrength.Weak);

        Assert.AreNotEqual(left, right);
    }

    [TestMethod]
    public void IdentityBindingHashChangesWhenActionKindChanges()
    {
        var selector = CreateSelector("categories-button", "Categorias");

        var left = ApprovalManifestBuilder.ComputeIdentityBindingHash("digest-a", selector, "click", "controlled", "web-shadow", IdentityStrength.Weak);
        var right = ApprovalManifestBuilder.ComputeIdentityBindingHash("digest-a", selector, "invoke", "controlled", "web-shadow", IdentityStrength.Weak);

        Assert.AreNotEqual(left, right);
    }

    [TestMethod]
    public void IdentityBindingHashChangesWhenModeChanges()
    {
        var selector = CreateSelector("categories-button", "Categorias");

        var left = ApprovalManifestBuilder.ComputeIdentityBindingHash("digest-a", selector, "click", "controlled", "web-shadow", IdentityStrength.Weak);
        var right = ApprovalManifestBuilder.ComputeIdentityBindingHash("digest-a", selector, "click", "nonCommercialWeb", "web-shadow", IdentityStrength.Weak);

        Assert.AreNotEqual(left, right);
    }

    [TestMethod]
    public void IdentityBindingHashDoesNotChangeEvidenceHash()
    {
        var preflight = ClickPreflightEvaluator.Evaluate("Categorias");
        var withoutIdentity = ApprovalManifestBuilder.Build(preflight, "controlled");
        var withIdentity = ApprovalManifestBuilder.Build(preflight, "controlled", new ApprovedIdentityInput(
            new ElementIdentity
            {
                Name = "Categorias",
                ControlType = "Button",
                AutomationId = "categories-button",
                Provenance = Provenance.Inferred
            },
            "web-shadow",
            null));

        Assert.AreEqual(withoutIdentity.EvidenceHash, withIdentity.EvidenceHash);
        Assert.IsFalse(string.IsNullOrWhiteSpace(withIdentity.IdentityBindingHash));
    }

    private static SelectorDefinition CreateSelector(string automationId, string name)
    {
        return new SelectorDefinition
        {
            SchemaVersion = 1,
            Provenance = Provenance.Inferred,
            Role = "Button",
            Name = name,
            AutomationId = automationId,
            ExpectedIdentity = new ElementIdentity
            {
                Name = name,
                ControlType = "Button",
                AutomationId = automationId,
                Provenance = Provenance.Inferred
            }
        };
    }
}
