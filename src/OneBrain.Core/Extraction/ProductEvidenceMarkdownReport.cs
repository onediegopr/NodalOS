namespace OneBrain.Core.Extraction;

public sealed record ProductEvidenceMarkdownWriteResult
{
    public bool Success { get; init; }
    public string Path { get; init; } = "";
    public string RelativePath { get; init; } = "";
    public string Error { get; init; } = "";
    public string Markdown { get; init; } = "";
}
