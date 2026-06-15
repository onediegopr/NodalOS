namespace OneBrain.Core.Detection.Contracts;

/// <summary>
/// Estados de interacción observables (sensor puro).
/// No incluye valores de decisión (Proceed, Abort, etc.) — esos están en StateDecisionType.
/// </summary>
public enum InteractionState
{
    None = 0,

    // Estados de desafío de seguridad
    CaptchaChallenge,
    TwoFactorRequired,
    AntiBotBlock,

    // Estados de interfaz
    Loading,
    LayoutChanged,
    ModalOverlay,
    HoneypotDetected,

    // Estados de error/navegación
    TimeoutOrHang,
    JavaScriptDialog,
    FrameNavigated
}
