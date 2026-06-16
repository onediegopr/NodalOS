namespace OneBrain.Core.Detection.Policy;

using OneBrain.Core.Detection.Contracts;

/// <summary>
/// Implementación de la matriz de decisiones como código C# (switch expression).
/// No YAML, no JSON interpretado. Autoridad normativa compilada.
/// </summary>
public sealed class StateSafetyPolicyEngine : ISafetyPolicyEngine
{
    public const double MINIMUM_CONFIDENCE = 0.60;

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
            InteractionState.None => StateDecision.Proceed("P-NONE"),
            _ => StateDecision.RequiresHuman("P-UNKNOWN-001")
        };

        // BUG C-1: una señal de estado concreta (no None) con confianza por debajo del piso no puede
        // avanzar el flujo. Las decisiones que avanzan (Proceed/Wait/TriggerSelectorRecovery) se degradan
        // a RequiresHuman. None es el baseline limpio (confianza 0 por construcción) y queda excluido.
        if (result.DetectedState != InteractionState.None &&
            result.ConfidenceScore < MINIMUM_CONFIDENCE &&
            decision.Type is StateDecisionType.Proceed
                or StateDecisionType.Wait
                or StateDecisionType.TriggerSelectorRecovery)
        {
            decision = StateDecision.RequiresHuman("P-LOW-CONFIDENCE-001");
        }

        return Task.FromResult(decision);
    }
}
