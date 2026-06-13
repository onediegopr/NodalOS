namespace OneBrain.Core.Execution;

public interface IActionContractPolicy
{
    string ActionKind { get; }

    void Validate(RecipeSafetyContract contract, List<string> reasons);
}
