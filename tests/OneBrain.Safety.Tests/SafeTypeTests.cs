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
public sealed class SafeTypeTests
{
    [TestMethod]
    public void TypePolicyRegistryResolvesType()
    {
        Assert.IsInstanceOfType(ActionContractPolicyRegistry.Resolve("type"), typeof(TypeActionContractPolicy));
    }

    [TestMethod]
    public void TypePolicyRequiresStrongIdentity()
    {
        var validation = new ContractValidator().Validate(CreateTypeContract(CreateIdentity(runtimeId: "")));

        Assert.IsFalse(validation.IsValid);
        CollectionAssert.Contains(validation.Reasons.ToList(), "TypeRequiresStrongIdentity");
    }

    [TestMethod]
    public void TypePolicyRequiresSelector()
    {
        var validation = new ContractValidator().Validate(CreateTypeContract() with { Selector = null });

        Assert.IsFalse(validation.IsValid);
        CollectionAssert.Contains(validation.Reasons.ToList(), "TypeRequiresSelector");
    }

    [TestMethod]
    public void TypePolicyRequiresApprovalRef()
    {
        var validation = new ContractValidator().Validate(CreateTypeContract() with { ApprovalRef = null });

        Assert.IsFalse(validation.IsValid);
        CollectionAssert.Contains(validation.Reasons.ToList(), "TypeRequiresApprovalRef");
    }

    [TestMethod]
    public void TypePolicyRequiresApprovedText()
    {
        var validation = new ContractValidator().Validate(CreateTypeContract() with { ApprovedValueDigest = "" });

        Assert.IsFalse(validation.IsValid);
        CollectionAssert.Contains(validation.Reasons.ToList(), "TypeRequiresApprovedText");
    }

    [TestMethod]
    public void TypePolicyRequiresApprovedInputBindingHash()
    {
        var validation = new ContractValidator().Validate(CreateTypeContract() with { ApprovedInputBindingHash = "" });

        Assert.IsFalse(validation.IsValid);
        CollectionAssert.Contains(validation.Reasons.ToList(), "TypeRequiresApprovedInputBindingHash");
    }

    [TestMethod]
    public void TypePolicyRequiresUiaProfileVerifiedFullAction()
    {
        var badProvenance = new ContractValidator().Validate(CreateTypeContract() with { Provenance = Provenance.Inferred });
        var badTrust = new ContractValidator().Validate(CreateTypeContract() with { TrustLevel = TrustLevel.InferredLowConfidence });
        var badCeiling = new ContractValidator().Validate(CreateTypeContract() with { ActionCeiling = ActionCeiling.ReadOnly });

        Assert.IsFalse(badProvenance.IsValid);
        Assert.IsFalse(badTrust.IsValid);
        Assert.IsFalse(badCeiling.IsValid);
        CollectionAssert.Contains(badProvenance.Reasons.ToList(), "TypeRequiresUiaProvenance");
        CollectionAssert.Contains(badTrust.Reasons.ToList(), "TypeRequiresProfileVerifiedTrust");
        CollectionAssert.Contains(badCeiling.Reasons.ToList(), "TypeRequiresFullActionWithPreflight");
    }

    [TestMethod]
    public void TypePolicyRejectsReversibleTrueAndRequiresMaxActionsOne()
    {
        var reversible = new ContractValidator().Validate(CreateTypeContract() with { Reversible = true });
        var tooMany = new ContractValidator().Validate(CreateTypeContract() with { MaxActions = 2 });

        Assert.IsFalse(reversible.IsValid);
        Assert.IsFalse(tooMany.IsValid);
        CollectionAssert.Contains(reversible.Reasons.ToList(), "TypeMustBeIrreversible");
        CollectionAssert.Contains(tooMany.Reasons.ToList(), "TypeMaxActionsMustBeOne");
    }

    [TestMethod]
    public void TypePolicyAcceptsValidSafeTypeContract()
    {
        var validation = new ContractValidator().Validate(CreateTypeContract());

        Assert.IsTrue(validation.IsValid, string.Join(", ", validation.Reasons));
    }

    [TestMethod]
    public void UnknownActionKindStillDenied()
    {
        var validation = new ContractValidator().Validate(CreateTypeContract() with { ActionKind = "form.fill" });

        Assert.IsFalse(validation.IsValid);
        CollectionAssert.Contains(validation.Reasons.ToList(), "ActionKindPolicyDenied");
    }

    [TestMethod]
    public void SafeTypeVerifierRequiresSuccessAndSameIdentity()
    {
        var contract = CreateTypeContract();
        var verifier = new SafeTypeStepVerifier();
        var success = verifier.Verify(contract, SuccessfulTypeResult(contract.ExpectedIdentity!), "approved");
        var mismatch = verifier.Verify(contract, SuccessfulTypeResult(contract.ExpectedIdentity!) with
        {
            InvokeTimeIdentityVerdict = "Different"
        }, "approved");

        Assert.IsTrue(success.Success, string.Join(", ", success.Reasons));
        Assert.IsFalse(mismatch.Success);
        CollectionAssert.Contains(mismatch.Reasons.ToList(), "valueAfterMatchesApproved=true");
    }

    [TestMethod]
    public void SafeTypeVerifierRequiresOwnershipMutationAndReadback()
    {
        var contract = CreateTypeContract();
        var verifier = new SafeTypeStepVerifier();

        Assert.IsFalse(verifier.Verify(contract, SuccessfulTypeResult(contract.ExpectedIdentity!) with { OwnershipAllowed = false }, "approved").Success);
        Assert.IsFalse(verifier.Verify(contract, SuccessfulTypeResult(contract.ExpectedIdentity!) with { MutationObserved = false }, "approved").Success);
        Assert.IsFalse(verifier.Verify(contract, SuccessfulTypeResult(contract.ExpectedIdentity!) with { ValueAfter = "other" }, "approved").Success);
    }

    [TestMethod]
    public void SourceScanTypeExecutorUsesSetValueOnly()
    {
        var source = File.ReadAllText(Path.Combine(FindRepoRoot(), "src", "OneBrain.Actions", "Uia", "UiaTypeExecutor.cs"));

        StringAssert.Contains(source, "SetValue");
        Assert.IsFalse(source.Contains("SendInput", StringComparison.Ordinal));
        Assert.IsFalse(source.Contains("SendKeys", StringComparison.Ordinal));
        Assert.IsFalse(source.Contains("KeyboardInput", StringComparison.Ordinal));
        Assert.IsFalse(source.Contains("keybd_event", StringComparison.Ordinal));
        Assert.IsFalse(source.Contains("SetCursorPos", StringComparison.Ordinal));
        Assert.IsFalse(source.Contains("mouse_event", StringComparison.Ordinal));
        Assert.IsFalse(source.Contains("GetClickablePoint", StringComparison.Ordinal));
        Assert.IsFalse(source.Contains("Clipboard", StringComparison.Ordinal));
        Assert.IsFalse(source.Contains("UiaActionExecutor", StringComparison.Ordinal));
        Assert.IsFalse(source.Contains("BasicActionVerifier", StringComparison.Ordinal));
        Assert.IsFalse(source.Contains("el.Click", StringComparison.Ordinal));
        Assert.IsFalse(source.Contains(".Patterns.Invoke.Pattern", StringComparison.Ordinal));
    }

    [TestMethod]
    public void SourceScanReadExecutorHasNoMutationCalls()
    {
        var source = File.ReadAllText(Path.Combine(FindRepoRoot(), "src", "OneBrain.Actions", "Uia", "UiaReadExecutor.cs"));

        Assert.IsFalse(source.Contains("SetValue", StringComparison.Ordinal));
        Assert.IsFalse(source.Contains(".Patterns.Invoke.Pattern", StringComparison.Ordinal));
        Assert.IsFalse(source.Contains("SendInput", StringComparison.Ordinal));
        Assert.IsFalse(source.Contains("GetClickablePoint", StringComparison.Ordinal));
        Assert.IsFalse(source.Contains("Clipboard", StringComparison.Ordinal));
        Assert.IsFalse(source.Contains("UiaActionExecutor", StringComparison.Ordinal));
        Assert.IsFalse(source.Contains("el.Click", StringComparison.Ordinal));
    }

    private static TypeExecutionResult SuccessfulTypeResult(ElementIdentity identity)
    {
        var digest = ElementFingerprintBuilder.Build(identity);
        return new TypeExecutionResult(
            Success: true,
            FailureKind: null,
            Reasons: ["type ok"],
            ValueBefore: "before",
            ValueAfter: "approved",
            ApprovedTextDigest: "approved-digest",
            PatternUsed: "ValuePattern.SetValue",
            ObservedIdentity: identity,
            IdentityVerdict: "Same",
            InvokeTimeIdentityChecked: true,
            InvokeTimeIdentityVerdict: "Same",
            InvokeTimeIdentityReason: "Same",
            ExpectedIdentityDigest: digest,
            ObservedIdentityDigest: digest,
            MutationObserved: true,
            SurfaceAllowed: true,
            SurfaceReason: "type surface allowed",
            OwnershipChecked: true,
            OwnershipAllowed: true,
            WindowFound: true,
            TargetVisible: true);
    }

    private static RecipeSafetyContract CreateTypeContract(ElementIdentity? identity = null)
    {
        identity ??= CreateIdentity();
        var selector = SelectorEngine.GenerateSelector(identity);
        return new RecipeSafetyContract(
            SchemaVersion: 1,
            ContractId: "contract-type",
            ActionKind: "type",
            ExpectedIdentity: identity,
            Selector: selector,
            WindowConstraints: new ExecutionWindowConstraints(false, true),
            Reversible: false,
            MaxActions: 1,
            ActionCeiling: ActionCeiling.FullActionWithPreflight,
            Provenance: Provenance.Uia,
            TrustLevel: TrustLevel.ProfileVerified,
            ApprovalRef: new ApprovalBinding(
                "approval-1",
                ElementFingerprintBuilder.Build(identity),
                selector,
                "type",
                "controlled",
                ApprovalManifestBuilder.PolicyVersion,
                "evidence"),
            ApprovedValueDigest: "approved-digest",
            ApprovedInputBindingHash: "approved-input-binding",
            ApprovedInputBindingVersion: ApprovedInputBindingHashBuilder.BindingVersion,
            ApprovedInputDigestAlgorithm: ApprovedInputBindingHashBuilder.DigestAlgorithm);
    }

    private static ElementIdentity CreateIdentity(string runtimeId = "42.1.9")
    {
        return new ElementIdentity(runtimeId, "Edit", "Account number", "account-number")
        {
            Role = "Edit",
            ControlType = "Edit",
            ClassName = "TextBox",
            AncestorPath = "Window:ONE Brain > Pane:Form",
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
