namespace OneBrain.Core.Detection.Contracts;

/// <summary>Features estructurales crudos extraídos del DOM vía CDP (Capa 1).</summary>
public record StructuralFeatures
{
    public bool HasCaptchaIframe { get; init; }
    public bool HasCaptchaDiv { get; init; }
    public bool HasTwoFactorFields { get; init; }
    public bool HasHoneypotFields { get; init; }
    public bool HasLoadingOverlay { get; init; }
    public bool HasModalOverlay { get; init; }
    public bool IsNetworkBlocked { get; init; }
    public bool IsJavaScriptDialogOpen { get; init; }
    public bool IsFrameNavigated { get; init; }
    public string? DomSnippet { get; init; }
}
