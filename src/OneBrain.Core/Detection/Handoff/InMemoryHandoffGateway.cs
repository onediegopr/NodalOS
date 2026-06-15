namespace OneBrain.Core.Detection.Handoff;

using OneBrain.Core.Detection.Contracts;

/// <summary>Implementación in-memory del gateway de handoff para tests.</summary>
public class InMemoryHandoffGateway : IHumanHandoffGateway
{
    private HandoffResult? _nextResult;

    public void SetNextResult(HandoffResult result) => _nextResult = result;

    public Task<HandoffResult> RequestInterventionAsync(StateHandoffRequest request, CancellationToken ct = default)
    {
        return Task.FromResult(_nextResult ?? HandoffResult.Timeout);
    }
}
