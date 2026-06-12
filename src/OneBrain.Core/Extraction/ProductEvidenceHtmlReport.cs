namespace OneBrain.Core.Extraction;

public sealed record ProductEvidenceHtmlWriteResult
{
    public bool Success { get; init; }
    public string Path { get; init; } = "";
    public string RelativePath { get; init; } = "";
    public string Error { get; init; } = "";
    public string Html { get; init; } = "";
}
