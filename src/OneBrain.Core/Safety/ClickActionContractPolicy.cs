using OneBrain.Core.Contracts;

namespace OneBrain.Core.Execution;

public sealed class ClickActionContractPolicy : IActionContractPolicy
{
    public string ActionKind => "click";

    public void Validate(RecipeSafetyContract contract, List<string> reasons)
    {
        if (contract.Reversible)
            reasons.Add("ClickMustBeIrreversible");
        if (contract.MaxActions != 1)
            reasons.Add("ClickMaxActionsMustBeOne");
        if (contract.ApprovalRef == null)
            reasons.Add("ClickRequiresApprovalRef");
        if (contract.ExpectedIdentity == null || !contract.ExpectedIdentity.IsStrong)
            reasons.Add("ClickRequiresStrongIdentity");
        if (contract.Selector == null)
            reasons.Add("ClickRequiresSelector");
        if (contract.ActionCeiling != ActionCeiling.FullActionWithPreflight)
            reasons.Add("ClickRequiresFullActionWithPreflight");
        if (contract.Provenance != Provenance.Uia)
            reasons.Add("ClickRequiresUiaProvenance");
        if (contract.TrustLevel < TrustLevel.ProfileVerified)
            reasons.Add("ClickRequiresProfileVerifiedTrust");
    }
}
