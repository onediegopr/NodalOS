using System.Text.Json;
using System.Text.Json.Serialization;

namespace OneBrain.Core.Extraction;

public sealed record ProductEvidenceInput
{
    public string? SourceUrl { get; init; }
    public string? SourceProfileId { get; init; }
    public string? PageTitle { get; init; }
    public string? VisibleText { get; init; }
    public string? CategoryHint { get; init; }
    public string? RawSignals { get; init; }
}

public sealed record ProductEvidence
{
    public string? SourceUrl { get; init; }
    public string? SourceProfileId { get; init; }
    public string? PageTitle { get; init; }
    public string? ProductName { get; init; }
    public string? Brand { get; init; }
    public string? Sku { get; init; }
    public string? Category { get; init; }
    public string? Description { get; init; }
    public string? Price { get; init; }
    public string? Currency { get; init; }
    public string? Availability { get; init; }
    public string? Stock { get; init; }
    public string? Seller { get; init; }
    public IReadOnlyList<string> ContactSignals { get; init; } = [];
    public IReadOnlyList<string> WhatsappSignals { get; init; } = [];
    public IReadOnlyList<string> CartSignals { get; init; } = [];
    public IReadOnlyList<string> BuySignals { get; init; } = [];
    public IReadOnlyList<string> PaymentSignals { get; init; } = [];
    public IReadOnlyList<string> LoginSignals { get; init; } = [];
    public IReadOnlyList<string> CookieSignals { get; init; } = [];
    public IReadOnlyList<string> GeolocSignals { get; init; } = [];
    public IReadOnlyList<string> PopupSignals { get; init; } = [];
    public string? EvidenceTextSample { get; init; }
    public IReadOnlyList<string> RawSignals { get; init; } = [];
    public IReadOnlyList<string> BlockedOrMissingFields { get; init; } = [];
    public string ExtractionConfidence { get; init; } = "low";
    public string ExtractionStatus { get; init; } = "diagnostic";
    public IReadOnlyList<string> ExtractionNotes { get; init; } = [];

    public string ToJson()
    {
        return JsonSerializer.Serialize(this, new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = false
        });
    }
}
