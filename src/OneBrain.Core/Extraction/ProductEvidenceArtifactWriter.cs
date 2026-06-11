using System.Text.Json;

namespace OneBrain.Core.Extraction;

public static class ProductEvidenceArtifactWriter
{
    public const string SchemaVersion = "product-evidence-artifact/v1";
    public const string RelativeArtifactDirectory = "artifacts/product-evidence";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public static ProductEvidenceArtifact Build(ProductEvidenceArtifactInput input)
    {
        var evidence = input.Evidence;
        var runId = string.IsNullOrWhiteSpace(input.RunId) ? Guid.NewGuid().ToString("N") : input.RunId.Trim();
        var createdAtUtc = input.CreatedAtUtc ?? DateTimeOffset.UtcNow;

        var notes = input.Notes
            .Concat(evidence.ExtractionNotes)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        AddNoteIfMissing(notes, "raw public HTML signal, not visible UIA price",
            evidence.RawSignals.Any(signal => signal.Contains("price=", StringComparison.OrdinalIgnoreCase)));

        return new ProductEvidenceArtifact
        {
            SchemaVersion = SchemaVersion,
            RunId = runId,
            CreatedAtUtc = createdAtUtc.UtcDateTime.ToString("o"),
            RecipeId = Clean(input.RecipeId),
            ProfileId = FirstNonEmpty(input.ProfileId, evidence.SourceProfileId),
            SourceUrl = FirstNonEmpty(input.SourceUrl, evidence.SourceUrl),
            PageTitle = FirstNonEmpty(input.PageTitle, evidence.PageTitle),
            Evidence = evidence,
            Safety = new ProductEvidenceSafetySummary
            {
                Clicks = 0,
                CookiesAccepted = 0,
                LoginSignals = evidence.LoginSignals,
                CartSignals = evidence.CartSignals,
                BuySignals = evidence.BuySignals,
                PaymentSignals = evidence.PaymentSignals,
                WhatsappSignals = evidence.WhatsappSignals
            },
            Validation = new ProductEvidenceValidationSummary
            {
                Success = true,
                Status = evidence.ExtractionStatus,
                Confidence = evidence.ExtractionConfidence,
                BlockedOrMissingFields = evidence.BlockedOrMissingFields
            },
            Notes = notes
        };
    }

    public static ProductEvidenceArtifactWriteResult Write(string baseDirectory, ProductEvidenceArtifactInput input)
    {
        try
        {
            var artifact = Build(input);
            var root = GetArtifactRoot(baseDirectory);
            Directory.CreateDirectory(root);

            var fileName = BuildFileName(artifact);
            var fullPath = Path.GetFullPath(Path.Combine(root, fileName));
            EnsureInsideRoot(root, fullPath);

            File.WriteAllText(fullPath, JsonSerializer.Serialize(artifact, JsonOptions));

            return new ProductEvidenceArtifactWriteResult
            {
                Success = true,
                Path = fullPath,
                RelativePath = Path.Combine(RelativeArtifactDirectory, fileName).Replace('\\', '/'),
                RunId = artifact.RunId,
                Artifact = artifact
            };
        }
        catch (Exception ex)
        {
            return new ProductEvidenceArtifactWriteResult
            {
                Success = false,
                Error = ex.Message
            };
        }
    }

    public static string GetArtifactRoot(string baseDirectory)
    {
        var fullBase = Path.GetFullPath(baseDirectory);
        return Path.GetFullPath(Path.Combine(fullBase, "artifacts", "product-evidence"));
    }

    public static string BuildFileName(ProductEvidenceArtifact artifact)
    {
        var created = DateTimeOffset.TryParse(artifact.CreatedAtUtc, out var parsed)
            ? parsed.UtcDateTime.ToString("yyyyMMdd-HHmmss")
            : DateTimeOffset.UtcNow.UtcDateTime.ToString("yyyyMMdd-HHmmss");

        return string.Join("-",
            created,
            SanitizeSegment(artifact.RecipeId),
            SanitizeSegment(artifact.ProfileId),
            SanitizeSegment(artifact.RunId)) + ".json";
    }

    private static void EnsureInsideRoot(string root, string fullPath)
    {
        var fullRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (!fullPath.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("artifact path escaped artifacts/product-evidence root");
    }

    private static string SanitizeSegment(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "unknown";

        var chars = value.Trim().Select(c =>
            char.IsLetterOrDigit(c) || c is '-' or '_' ? char.ToLowerInvariant(c) : '-').ToArray();
        var sanitized = new string(chars).Trim('-');
        while (sanitized.Contains("--", StringComparison.Ordinal))
            sanitized = sanitized.Replace("--", "-", StringComparison.Ordinal);

        return string.IsNullOrWhiteSpace(sanitized) ? "unknown" : sanitized;
    }

    private static string FirstNonEmpty(params string?[] values)
    {
        return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim() ?? "";
    }

    private static string Clean(string? value) => string.IsNullOrWhiteSpace(value) ? "" : value.Trim();

    private static void AddNoteIfMissing(List<string> notes, string note, bool condition)
    {
        if (!condition) return;
        if (!notes.Contains(note, StringComparer.OrdinalIgnoreCase))
            notes.Add(note);
    }
}
