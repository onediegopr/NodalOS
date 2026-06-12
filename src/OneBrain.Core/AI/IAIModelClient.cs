namespace OneBrain.Core.AI;

public interface IAIModelClient
{
    Task<AIModelClientResult> CompleteAsync(AIModelRoutingDecision decision, string prompt, CancellationToken cancellationToken = default);
}

public sealed record AIModelClientResult(
    bool Success,
    string Status,
    string Text,
    string Error);

public sealed class MockAIModelClient : IAIModelClient
{
    public Task<AIModelClientResult> CompleteAsync(AIModelRoutingDecision decision, string prompt, CancellationToken cancellationToken = default)
    {
        if (!decision.Success)
        {
            return Task.FromResult(new AIModelClientResult(
                Success: false,
                Status: "blocked",
                Text: "",
                Error: decision.Reason));
        }

        return Task.FromResult(new AIModelClientResult(
            Success: true,
            Status: "mock",
            Text: "mock AI response; no provider call was made",
            Error: ""));
    }
}
