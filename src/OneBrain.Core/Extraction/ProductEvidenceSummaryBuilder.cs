namespace OneBrain.Core.Extraction;

public static class ProductEvidenceSummaryBuilder
{
    public const string SchemaVersion = "product-evidence-summary/v1";

    public static ProductEvidenceSummary Build(IEnumerable<ProductEvidenceSummarySource> sources, DateTimeOffset? createdAtUtc = null)
    {
        var sourceList = sources.ToList();
        var items = new List<ProductEvidenceSummaryItem>();
        var invalidArtifacts = new List<string>();

        foreach (var source in sourceList)
        {
            if (source.Artifact == null)
            {
                invalidArtifacts.Add(FormatInvalidArtifact(source));
                continue;
            }

            var artifact = source.Artifact;
            var evidence = artifact.Evidence;
            var price = NullIfBlank(evidence.Price);
            var currency = NullIfBlank(evidence.Currency);
            var stock = NullIfBlank(evidence.Stock);

            items.Add(new ProductEvidenceSummaryItem
            {
                RecipeId = artifact.RecipeId,
                ProfileId = artifact.ProfileId,
                SourceUrl = artifact.SourceUrl,
                ProductName = NullIfBlank(evidence.ProductName),
                Category = NullIfBlank(evidence.Category),
                Price = price,
                Currency = currency,
                Stock = stock,
                ExtractionStatus = FirstNonEmpty(evidence.ExtractionStatus, artifact.Validation.Status, "diagnostic"),
                ExtractionConfidence = FirstNonEmpty(evidence.ExtractionConfidence, artifact.Validation.Confidence, "diagnostic"),
                BlockedOrMissingFields = evidence.BlockedOrMissingFields.Count > 0
                    ? evidence.BlockedOrMissingFields
                    : artifact.Validation.BlockedOrMissingFields,
                HasPrice = price != null,
                HasCurrency = currency != null,
                HasStock = stock != null,
                RawSignalCount = evidence.RawSignals.Count,
                SafetySummary = artifact.Safety,
                ArtifactPath = source.ArtifactPath
            });
        }

        var totals = new ProductEvidenceSummaryTotals
        {
            ProductsWithPrice = items.Count(item => item.HasPrice),
            ProductsMissingPrice = items.Count(item => !item.HasPrice),
            ProductsWithMediumConfidence = items.Count(item => item.ExtractionConfidence.Equals("medium", StringComparison.OrdinalIgnoreCase)),
            ProductsWithHighConfidence = items.Count(item => item.ExtractionConfidence.Equals("high", StringComparison.OrdinalIgnoreCase)),
            ProductsWithDiagnosticStatus = items.Count(item => item.ExtractionStatus.Equals("diagnostic", StringComparison.OrdinalIgnoreCase)),
            SafetyClicksTotal = items.Sum(item => item.SafetySummary.Clicks),
            SafetyPaymentsSignalsTotal = items.Sum(item => item.SafetySummary.PaymentSignals.Count),
            ArtifactsWithWarnings = items.Count(item => item.BlockedOrMissingFields.Count > 0) + invalidArtifacts.Count
        };

        var notes = new List<string>();
        if (sourceList.Count == 0)
            notes.Add("diagnostic: no product evidence artifacts found");
        if (invalidArtifacts.Count > 0)
            notes.Add("diagnostic: invalid artifacts were skipped");
        if (items.Any(item => !item.HasPrice && item.RawSignalCount > 0))
            notes.Add("rawSignals are not treated as visible normalized price");

        return new ProductEvidenceSummary
        {
            SchemaVersion = SchemaVersion,
            CreatedAtUtc = (createdAtUtc ?? DateTimeOffset.UtcNow).UtcDateTime.ToString("o"),
            SourceArtifactCount = sourceList.Count,
            ValidArtifactCount = items.Count,
            InvalidArtifactCount = invalidArtifacts.Count,
            Items = items,
            Totals = totals,
            InvalidArtifacts = invalidArtifacts,
            Notes = notes
        };
    }

    private static string FormatInvalidArtifact(ProductEvidenceSummarySource source)
    {
        var path = string.IsNullOrWhiteSpace(source.ArtifactPath) ? "unknown" : source.ArtifactPath;
        var error = string.IsNullOrWhiteSpace(source.Error) ? "invalid artifact" : source.Error;
        return $"{path}: {error}";
    }

    private static string? NullIfBlank(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        if (value.Trim().Equals("null", StringComparison.OrdinalIgnoreCase)) return null;
        return value.Trim();
    }

    private static string FirstNonEmpty(params string[] values)
    {
        return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim() ?? "";
    }
}
