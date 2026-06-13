using OneBrain.Core.Contracts;

namespace OneBrain.Core.Execution;

public sealed class ReadActionContractPolicy : IActionContractPolicy
{
    public string ActionKind => "read";

    public void Validate(RecipeSafetyContract contract, List<string> reasons)
    {
        if (!contract.Reversible)
            reasons.Add("ReadMustBeReversible");
        if (contract.MaxActions != 1)
            reasons.Add("ReadMaxActionsMustBeOne");
        if (contract.ApprovalRef == null)
            reasons.Add("ReadRequiresApprovalRef");
        if (contract.ExpectedIdentity == null || !contract.ExpectedIdentity.IsStrong)
            reasons.Add("ReadRequiresStrongIdentity");
        if (contract.Selector == null)
            reasons.Add("ReadRequiresSelector");
        if (contract.ActionCeiling != ActionCeiling.ReadOnly)
            reasons.Add("ReadRequiresReadOnlyCeiling");
        if (contract.Provenance != Provenance.Uia)
            reasons.Add("ReadRequiresUiaProvenance");
        if (contract.TrustLevel < TrustLevel.ProfileVerified)
            reasons.Add("ReadRequiresProfileVerifiedTrust");
    }
}
