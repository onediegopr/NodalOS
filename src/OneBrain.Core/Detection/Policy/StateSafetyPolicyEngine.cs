namespace OneBrain.Core.Detection.Policy;

using OneBrain.Core.Detection.Contracts;

/// <summary>
/// Implementación de la matriz de decisiones como código C# (switch expression).
/// No YAML, no JSON interpretado. Autoridad normativa compilada.
/// </summary>
public sealed class StateSafetyPolicyEngine : ISafetyPolicyEngine
{
    private const double MINIMUM_CONFIDENCE = 0.60;

    public Task<StateDecision> EvaluateStateAsync(StateDetectionResult result, CancellationToken ct = default)
    {
        var decision = result.DetectedState switch
        {
            InteractionState.CaptchaChallenge => StateDecision.RequiresHuman("P-CAPTCHA-001"),
            InteractionState.TwoFactorRequired => StateDecision.RequiresHuman("P-2FA-001"),
            InteractionState.AntiBotBlock => StateDecision.RequiresHuman("P-ANTIBOT-001"),
            InteractionState.Loading => StateDecision.Wait(TimeSpan.FromSeconds(2), "P-LOADING-001"),
            InteractionState.LayoutChanged => StateDecision.TriggerSelectorRecovery("P-LAYOUT-001"),
            InteractionState.ModalOverlay => StateDecision.Proceed("P-MODAL-001"),
            InteractionState.HoneypotDetected => StateDecision.Abort("P-HONEYPOT-001"),
            InteractionState.TimeoutOrHang => StateDecision.Wait(TimeSpan.FromSeconds(3), "P-TIMEOUT-001"),
            InteractionState.JavaScriptDialog => StateDecision.RequiresHuman("P-JSDIALOG-001"),
            InteractionState.FrameNavigated => StateDecision.TriggerSelectorRecovery("P-NAVIGATION-001"),
            InteractionState.None => result.ConfidenceScore >= MINIMUM_CONFIDENCE
                ? StateDecision.Proceed("P-NONE-HIGH-CONFIDENCE")
                : StateDecision.Proceed("P-NONE"),
            _ => result.ConfidenceScore >= MINIMUM_CONFIDENCE
                ? StateDecision.Proceed()
                : StateDecision.RequiresHuman("P-LOW-CONFIDENCE-001")
        };

        return Task.FromResult(decision);
    }
}
