namespace OneBrain.Core.Detection.Scoring;

using OneBrain.Core.Detection.Contracts;

public class CaptchaSignatureRule : IScoringRule
{
    public string RuleId => "RULE-CAPTCHA-001";
    public double Weight => 0.40;
    public InteractionState TargetState => InteractionState.CaptchaChallenge;

    public double Evaluate(StructuralFeatures features)
    {
        if (features.HasCaptchaIframe || features.HasCaptchaDiv)
            return 1.0;
        return 0.0;
    }
}

public class TwoFactorRule : IScoringRule
{
    public string RuleId => "RULE-2FA-001";
    public double Weight => 0.35;
    public InteractionState TargetState => InteractionState.TwoFactorRequired;

    public double Evaluate(StructuralFeatures features)
    {
        if (features.HasTwoFactorFields) return 1.0;
        return 0.0;
    }
}

public class AntiBotRule : IScoringRule
{
    public string RuleId => "RULE-ANTIBOT-001";
    public double Weight => 0.50;
    public InteractionState TargetState => InteractionState.AntiBotBlock;

    public double Evaluate(StructuralFeatures features)
    {
        if (features.IsNetworkBlocked) return 1.0;
        return 0.0;
    }
}

public class LoadingRule : IScoringRule
{
    public string RuleId => "RULE-LOADING-001";
    public double Weight => 0.20;
    public InteractionState TargetState => InteractionState.Loading;

    public double Evaluate(StructuralFeatures features)
    {
        if (features.HasLoadingOverlay) return 1.0;
        return 0.0;
    }
}

public class LayoutChangedRule : IScoringRule
{
    public string RuleId => "RULE-LAYOUT-001";
    public double Weight => 0.15;
    public InteractionState TargetState => InteractionState.LayoutChanged;

    public double Evaluate(StructuralFeatures features)
    {
        if (features.IsFrameNavigated) return 1.0;
        return 0.0;
    }
}

public class ModalOverlayRule : IScoringRule
{
    public string RuleId => "RULE-MODAL-001";
    public double Weight => 0.25;
    public InteractionState TargetState => InteractionState.ModalOverlay;

    public double Evaluate(StructuralFeatures features)
    {
        if (features.HasModalOverlay) return 1.0;
        if (features.IsJavaScriptDialogOpen) return 0.8;
        return 0.0;
    }
}

public class HoneypotRule : IScoringRule
{
    public string RuleId => "RULE-HONEYPOT-001";
    public double Weight => 0.30;
    public InteractionState TargetState => InteractionState.HoneypotDetected;

    public double Evaluate(StructuralFeatures features)
    {
        if (features.HasHoneypotFields) return 1.0;
        return 0.0;
    }
}

public class TimeoutRule : IScoringRule
{
    public string RuleId => "RULE-TIMEOUT-001";
    public double Weight => 0.10;
    public InteractionState TargetState => InteractionState.TimeoutOrHang;

    public double Evaluate(StructuralFeatures features) => 0.0; // Solo se activa en timeout real
}
