namespace OneBrain.Core.Detection.Perception;

using OneBrain.Core.Detection.Contracts;

/// <summary>
/// Detector de estados de interacción (sensor puro).
/// Orquesta los sensores CDP y emite StateDetectionResult.
/// </summary>
public class InteractionStateDetector : IInteractionStateDetector
{
    private readonly ICdpStructuralAnalyzer _structural;
    private readonly INetworkFeatureExtractor _network;
    private readonly IStateScoringEngine? _scoring;

    public InteractionStateDetector(
        ICdpStructuralAnalyzer structural,
        INetworkFeatureExtractor network,
        IStateScoringEngine? scoring = null)
    {
        _structural = structural;
        _network = network;
        _scoring = scoring;
    }

    public async Task<StateDetectionResult> AssessPreFlightAsync(TargetContext ctx, CancellationToken ct = default)
    {
        var structural = await _structural.AnalyzeAsync(ctx, ct);
        var network = await _network.ExtractAsync(ctx, ct);

        if (_scoring != null)
            return _scoring.Score(structural, network);

        return NoScoringFallback(structural, network);
    }

    public async Task<StateDetectionResult> AssessInFlightAsync(TargetContext ctx, TimeSpan timeout, CancellationToken ct = default)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(timeout);

        try
        {
            var structural = await _structural.AnalyzeAsync(ctx, cts.Token);
            var network = await _network.ExtractAsync(ctx, cts.Token);

            if (_scoring != null)
                return _scoring.Score(structural, network);

            return NoScoringFallback(structural, network);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            return new StateDetectionResult
            {
                DetectedState = InteractionState.TimeoutOrHang,
                ConfidenceScore = 1.0,
                DetectedAt = DateTimeOffset.UtcNow
            };
        }
    }

    private static StateDetectionResult NoScoringFallback(StructuralFeatures s, NetworkFeatures n)
    {
        InteractionState state = InteractionState.None;
        double confidence = 0.0;

        if (s.HasCaptchaIframe || s.HasCaptchaDiv) { state = InteractionState.CaptchaChallenge; confidence = 0.95; }
        else if (n.BlockedStatusCode is 401 or 403 or 429) { state = InteractionState.AntiBotBlock; confidence = 0.90; }
        else if (s.HasTwoFactorFields) { state = InteractionState.TwoFactorRequired; confidence = 0.85; }
        else if (s.HasHoneypotFields) { state = InteractionState.HoneypotDetected; confidence = 0.80; }
        else if (s.HasLoadingOverlay) { state = InteractionState.Loading; confidence = 0.70; }
        else if (s.HasModalOverlay) { state = InteractionState.ModalOverlay; confidence = 0.60; }

        return new StateDetectionResult
        {
            DetectedState = state,
            ConfidenceScore = confidence,
            DetectedAt = DateTimeOffset.UtcNow
        };
    }
}
