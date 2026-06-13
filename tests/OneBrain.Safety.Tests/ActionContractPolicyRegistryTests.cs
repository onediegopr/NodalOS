using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Approval;
using OneBrain.Core.Contracts;
using OneBrain.Core.Execution;
using OneBrain.Core.Identity;
using OneBrain.Core.Models;
using OneBrain.Core.Safety;
using OneBrain.Core.Selectors;

namespace OneBrain.Safety.Tests;

[TestClass]
public sealed class ActionContractPolicyRegistryTests
{
    [TestMethod]
    public void PolicyRegistryResolvesClick()
    {
        Assert.IsInstanceOfType(ActionContractPolicyRegistry.Resolve("click"), typeof(ClickActionContractPolicy));
    }

    [TestMethod]
    public void PolicyRegistryResolvesRead()
    {
        Assert.IsInstanceOfType(ActionContractPolicyRegistry.Resolve("read"), typeof(ReadActionContractPolicy));
    }

    [TestMethod]
    public void UnknownActionKindDenied()
    {
        var validation = new ContractValidator().Validate(CreateReadContract() with { ActionKind = "safe.type" });

        Assert.IsFalse(validation.IsValid);
        CollectionAssert.Contains(validation.Reasons.ToList(), "ActionKindPolicyDenied");
    }

    [TestMethod]
    public void ClickPolicyStillRejectsReversibleTrue()
    {
        var validation = new ContractValidator().Validate(CreateClickContract() with { Reversible = true });

        Assert.IsFalse(validation.IsValid);
        CollectionAssert.Contains(validation.Reasons.ToList(), "ClickMustBeIrreversible");
    }

    [TestMethod]
    public void ClickPolicyStillRequiresMaxActionsOne()
    {
        var validation = new ContractValidator().Validate(CreateClickContract() with { MaxActions = 2 });

        Assert.IsFalse(validation.IsValid);
        CollectionAssert.Contains(validation.Reasons.ToList(), "ClickMaxActionsMustBeOne");
    }

    [TestMethod]
    public void ClickPolicyStillRequiresStrongIdentity()
    {
        var validation = new ContractValidator().Validate(CreateClickContract(CreateIdentity(runtimeId: "")));

        Assert.IsFalse(validation.IsValid);
        CollectionAssert.Contains(validation.Reasons.ToList(), "ClickRequiresStrongIdentity");
    }

    [TestMethod]
    public void ReadPolicyRequiresStrongIdentity()
    {
        var validation = new ContractValidator().Validate(CreateReadContract(CreateIdentity(runtimeId: "")));

        Assert.IsFalse(validation.IsValid);
        CollectionAssert.Contains(validation.Reasons.ToList(), "ReadRequiresStrongIdentity");
    }

    [TestMethod]
    public void ReadPolicyRequiresSelector()
    {
        var validation = new ContractValidator().Validate(CreateReadContract() with { Selector = null });

        Assert.IsFalse(validation.IsValid);
        CollectionAssert.Contains(validation.Reasons.ToList(), "ReadRequiresSelector");
    }

    [TestMethod]
    public void ReadPolicyIsReadOnly()
    {
        var valid = new ContractValidator().Validate(CreateReadContract());
        var mutating = new ContractValidator().Validate(CreateReadContract() with { ActionCeiling = ActionCeiling.FullActionWithPreflight });

        Assert.IsTrue(valid.IsValid);
        Assert.IsFalse(mutating.IsValid);
        CollectionAssert.Contains(mutating.Reasons.ToList(), "ReadRequiresReadOnlyCeiling");
    }

    private static RecipeSafetyContract CreateClickContract(ElementIdentity? identity = null)
    {
        identity ??= CreateIdentity();
        var selector = SelectorEngine.GenerateSelector(identity);
        return CreateContract("click", identity, selector, reversible: false, ActionCeiling.FullActionWithPreflight);
    }

    private static RecipeSafetyContract CreateReadContract(ElementIdentity? identity = null)
    {
        identity ??= CreateIdentity();
        var selector = SelectorEngine.GenerateSelector(identity);
        return CreateContract("read", identity, selector, reversible: true, ActionCeiling.ReadOnly);
    }

    private static RecipeSafetyContract CreateContract(
        string actionKind,
        ElementIdentity identity,
        SelectorDefinition selector,
        bool reversible,
        ActionCeiling ceiling)
    {
        return new RecipeSafetyContract(
            SchemaVersion: 1,
            ContractId: $"contract-{actionKind}",
            ActionKind: actionKind,
            ExpectedIdentity: identity,
            Selector: selector,
            WindowConstraints: new ExecutionWindowConstraints(false, true),
            Reversible: reversible,
            MaxActions: 1,
            ActionCeiling: ceiling,
            Provenance: Provenance.Uia,
            TrustLevel: TrustLevel.ProfileVerified,
            ApprovalRef: new ApprovalBinding(
                "approval-1",
                ElementFingerprintBuilder.Build(identity),
                selector,
                actionKind,
                "controlled",
                ApprovalManifestBuilder.PolicyVersion,
                "evidence"));
    }

    private static ElementIdentity CreateIdentity(string runtimeId = "42.1.9")
    {
        return new ElementIdentity(runtimeId, "Hyperlink", "More information...", "more-information-link")
        {
            Role = "Hyperlink",
            ControlType = "Hyperlink",
            ClassName = "Chrome_RenderWidgetHostHWND",
            AncestorPath = "Window:ONE Brain > Pane:Catalog > Document:Main",
            Provenance = Provenance.Uia
        };
    }
}
