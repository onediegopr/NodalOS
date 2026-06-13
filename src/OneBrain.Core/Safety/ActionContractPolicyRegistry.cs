namespace OneBrain.Core.Execution;

public static class ActionContractPolicyRegistry
{
    private static readonly IActionContractPolicy Click = new ClickActionContractPolicy();
    private static readonly IActionContractPolicy Read = new ReadActionContractPolicy();
    private static readonly IActionContractPolicy Harness = new HarnessActionContractPolicy();
    private static readonly IActionContractPolicy Deny = new DenyByDefaultActionContractPolicy();

    public static IActionContractPolicy Resolve(string? actionKind)
    {
        if (string.IsNullOrWhiteSpace(actionKind))
            return Deny;

        return actionKind.Trim().ToLowerInvariant() switch
        {
            "click" => Click,
            "read" => Read,
            "benign_harness_click" => Harness,
            _ => Deny
        };
    }

    private sealed class DenyByDefaultActionContractPolicy : IActionContractPolicy
    {
        public string ActionKind => "*";

        public void Validate(RecipeSafetyContract contract, List<string> reasons)
        {
            reasons.Add("ActionKindPolicyDenied");
        }
    }

    private sealed class HarnessActionContractPolicy : IActionContractPolicy
    {
        public string ActionKind => "benign_harness_click";

        public void Validate(RecipeSafetyContract contract, List<string> reasons)
        {
            // Existing harness contracts are covered by the generic contract validator.
        }
    }
}
