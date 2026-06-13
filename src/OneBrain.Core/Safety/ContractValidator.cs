using OneBrain.Core.Contracts;

namespace OneBrain.Core.Execution;

public sealed class ContractValidator
{
    public ContractValidation Validate(RecipeSafetyContract? contract)
    {
        var reasons = new List<string>();
        if (contract == null)
            return Invalid("contract is required");

        if (contract.SchemaVersion != 1)
            reasons.Add("schema version must be 1");
        if (string.IsNullOrWhiteSpace(contract.ContractId))
            reasons.Add("contract id is required");
        if (string.IsNullOrWhiteSpace(contract.ActionKind))
            reasons.Add("action kind is required");
        if (contract.ExpectedIdentity == null)
            reasons.Add("expected identity is required");
        if (contract.Selector == null)
            reasons.Add("selector is required");
        if (contract.WindowConstraints == null)
            reasons.Add("window constraints are required");
        if (contract.MaxActions != 1)
            reasons.Add("max actions must be 1");
        if (contract.ApprovalRef == null)
            reasons.Add("approval binding is required");
        if (contract.ActionCeiling == ActionCeiling.None)
            reasons.Add("action ceiling is required");
        if (contract.ActionCeiling > SourceActionPolicy.Resolve(contract.Provenance))
            reasons.Add("action ceiling exceeds source action policy");
        ActionContractPolicyRegistry.Resolve(contract.ActionKind).Validate(contract, reasons);

        return reasons.Count == 0
            ? new ContractValidation(true, null, ["contract valid"])
            : new ContractValidation(false, FailureKind.PolicyDenied, reasons);
    }

    private static ContractValidation Invalid(string reason) =>
        new(false, FailureKind.PolicyDenied, [reason]);

}
