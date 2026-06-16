using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.BrowserExecutor.Cdp;

// M197 — PaddleOCR ONNX model readiness service.
// Aggregates catalog + verification into a production readiness decision.
public sealed class NodalOsPaddleOcrOnnxModelReadinessService
{
    private readonly NodalOsPaddleOcrOnnxModelCatalogService _catalog = new();
    private readonly NodalOsPaddleOcrOnnxModelVerifierService _verifier = new();

    public NodalOsPaddleOcrOnnxModelReadinessDetail Evaluate(
        NodalOsPaddleOcrOnnxModelManifest manifest,
        string repositoryRoot,
        bool licenseAccepted)
    {
        var verifiedManifest = _verifier.VerifyManifest(manifest, repositoryRoot, licenseAccepted);
        var catalog = _catalog.BuildCatalog(verifiedManifest);

        var totalSize = verifiedManifest.Models.Sum(m => m.Integrity.FileSizeBytes);
        var sizeWithinLimits = totalSize <= verifiedManifest.TotalMaxSizeBytes;
        var shapesKnown = verifiedManifest.Models
            .Where(m => m.Kind is NodalOsPaddleOcrOnnxModelKind.TextDetection or NodalOsPaddleOcrOnnxModelKind.TextRecognition)
            .All(m => !string.IsNullOrWhiteSpace(m.InputShapeDescription) && !string.IsNullOrWhiteSpace(m.OutputShapeDescription));

        if (!licenseAccepted)
        {
            return Readiness(
                catalog,
                NodalOsPaddleOcrOnnxModelStatus.BlockedByLicense,
                "license review required before models can be verified",
                canRunOcr: false);
        }

        if (!catalog.AllRequiredPresent)
        {
            return Readiness(
                catalog,
                NodalOsPaddleOcrOnnxModelStatus.Missing,
                "required detection/recognition models are missing",
                canRunOcr: false);
        }

        if (!catalog.AllRequiredVerified)
        {
            var firstNotVerified = catalog.RequiredModels.First(m => m.Status != NodalOsPaddleOcrOnnxModelStatus.Verified);
            return Readiness(
                catalog,
                firstNotVerified.Status,
                $"model {firstNotVerified.ModelId} is not verified: {firstNotVerified.Status}",
                canRunOcr: false);
        }

        if (!sizeWithinLimits)
        {
            return Readiness(
                catalog,
                NodalOsPaddleOcrOnnxModelStatus.BlockedBySize,
                $"total model size {totalSize} exceeds limit {verifiedManifest.TotalMaxSizeBytes}",
                canRunOcr: false);
        }

        if (!shapesKnown)
        {
            return Readiness(
                catalog,
                NodalOsPaddleOcrOnnxModelStatus.Invalid,
                "required model shapes are not fully documented",
                canRunOcr: false);
        }

        return Readiness(
            catalog,
            NodalOsPaddleOcrOnnxModelStatus.Verified,
            "required models present, verified, licensed, and within size limits",
            canRunOcr: true);
    }

    private static NodalOsPaddleOcrOnnxModelReadinessDetail Readiness(
        NodalOsPaddleOcrOnnxModelCatalog catalog,
        NodalOsPaddleOcrOnnxModelStatus status,
        string reason,
        bool canRunOcr)
    {
        var totalSize = catalog.RequiredModels.Sum(m => m.Integrity.FileSizeBytes);
        var sizeWithinLimits = totalSize <= catalog.TotalMaxSizeBytes;
        var shapesKnown = catalog.RequiredModels
            .All(m => !string.IsNullOrWhiteSpace(m.InputShapeDescription) && !string.IsNullOrWhiteSpace(m.OutputShapeDescription));

        return new NodalOsPaddleOcrOnnxModelReadinessDetail(
            $"model-readiness-{Guid.NewGuid():N}",
            canRunOcr,
            status,
            BrowserCredentialRedactor.Redact(reason),
            canRunOcr,
            catalog.AllRequiredVerified,
            catalog.LicenseReviewed,
            sizeWithinLimits,
            shapesKnown,
            NoAuthority: true,
            Redacted: true);
    }
}
