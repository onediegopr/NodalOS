namespace OneBrain.Core.Detection.Contracts;

/// <summary>Features de red metadata-only (Capa 1). NUNCA captura valores de headers ni body.</summary>
public record NetworkFeatures
{
    public int? BlockedStatusCode { get; init; }
    public string? Host { get; init; }
    public string[] HeaderNames { get; init; } = Array.Empty<string>();
}
