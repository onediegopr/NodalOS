using OneBrain.Core.Contracts;

namespace OneBrain.Core.Execution;

public sealed class TypeActionContractPolicy : IActionContractPolicy
{
    public string ActionKind => "type";

    public void Validate(RecipeSafetyContract contract, List<string> reasons)
    {
        if (contract.Reversible)
            reasons.Add("TypeMustBeIrreversible");
        if (contract.MaxActions != 1)
            reasons.Add("TypeMaxActionsMustBeOne");
        if (contract.ApprovalRef == null)
            reasons.Add("TypeRequiresApprovalRef");
        if (contract.ExpectedIdentity == null || !contract.ExpectedIdentity.IsStrong)
            reasons.Add("TypeRequiresStrongIdentity");
        if (contract.Selector == null)
            reasons.Add("TypeRequiresSelector");
        if (contract.ActionCeiling != ActionCeiling.FullActionWithPreflight)
            reasons.Add("TypeRequiresFullActionWithPreflight");
        if (contract.Provenance != Provenance.Uia)
            reasons.Add("TypeRequiresUiaProvenance");
        if (contract.TrustLevel < TrustLevel.ProfileVerified)
            reasons.Add("TypeRequiresProfileVerifiedTrust");
        if (string.IsNullOrWhiteSpace(contract.ApprovedValueDigest))
            reasons.Add("TypeRequiresApprovedText");
    }
}
