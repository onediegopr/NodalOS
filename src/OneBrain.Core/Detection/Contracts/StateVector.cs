namespace OneBrain.Core.Detection.Contracts;

/// <summary>Vector de scores por dimensión de estado (Capa 2).</summary>
public record StateVector
{
    public double CaptchaScore { get; init; }
    public double TwoFactorScore { get; init; }
    public double AntiBotScore { get; init; }
    public double LoadingScore { get; init; }
    public double LayoutChangedScore { get; init; }
    public double ModalScore { get; init; }
    public double HoneypotScore { get; init; }
    public double TimeoutScore { get; init; }
}
