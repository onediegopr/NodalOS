using System.Text.Json.Serialization;

namespace OneBrain.Core.Extraction;

public sealed record ProductEvidenceArtifact
{
    public string SchemaVersion { get; init; } = "product-evidence-artifact/v1";
    public string RunId { get; init; } = "";
    public string CreatedAtUtc { get; init; } = "";
    public string RecipeId { get; init; } = "";
    public string ProfileId { get; init; } = "";
    public string SourceUrl { get; init; } = "";
    public string PageTitle { get; init; } = "";
    public ProductEvidence Evidence { get; init; } = new();
    public ProductEvidenceSafetySummary Safety { get; init; } = new();
    public ProductEvidenceValidationSummary Validation { get; init; } = new();
    public IReadOnlyList<string> Notes { get; init; } = [];
}

public sealed record ProductEvidenceSafetySummary
{
    public int Clicks { get; init; }
    public int CookiesAccepted { get; init; }
    public IReadOnlyList<string> LoginSignals { get; init; } = [];
    public IReadOnlyList<string> CartSignals { get; init; } = [];
    public IReadOnlyList<string> BuySignals { get; init; } = [];
    public IReadOnlyList<string> PaymentSignals { get; init; } = [];
    public IReadOnlyList<string> WhatsappSignals { get; init; } = [];
}

public sealed record ProductEvidenceValidationSummary
{
    public bool Success { get; init; }
    public string Status { get; init; } = "diagnostic";
    public string Confidence { get; init; } = "diagnostic";
    public IReadOnlyList<string> BlockedOrMissingFields { get; init; } = [];
}

public sealed record ProductEvidenceArtifactInput
{
    public ProductEvidence Evidence { get; init; } = new();
    public string RecipeId { get; init; } = "";
    public string? ProfileId { get; init; }
    public string? SourceUrl { get; init; }
    public string? PageTitle { get; init; }
    public string? RunId { get; init; }
    public DateTimeOffset? CreatedAtUtc { get; init; }
    public IReadOnlyList<string> Notes { get; init; } = [];
}

public sealed record ProductEvidenceArtifactWriteResult
{
    public bool Success { get; init; }
    public string Path { get; init; } = "";
    public string RelativePath { get; init; } = "";
    public string RunId { get; init; } = "";
    public string Error { get; init; } = "";
    [JsonIgnore]
    public ProductEvidenceArtifact? Artifact { get; init; }
}
