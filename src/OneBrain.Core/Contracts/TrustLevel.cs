namespace OneBrain.Core.Contracts;

public enum TrustLevel
{
    TrustedSystemState,
    UntrustedScreenText,
    InferredLowConfidence,
    ProfileVerified,
    HumanConfirmed
}
