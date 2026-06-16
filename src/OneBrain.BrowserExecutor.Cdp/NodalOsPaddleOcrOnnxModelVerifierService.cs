using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.BrowserExecutor.Cdp;

// M197 — PaddleOCR ONNX model verifier.
// Runs file existence + integrity + license + shape sanity checks and assigns status.
public sealed class NodalOsPaddleOcrOnnxModelVerifierService
{
    private readonly NodalOsPaddleOcrOnnxModelIntegrityCheckerService _integrityChecker = new();

    public NodalOsPaddleOcrOnnxModelVerificationResult Verify(
        NodalOsPaddleOcrOnnxModelRef model,
        string repositoryRoot,
        bool licenseAccepted)
    {
        var absolutePath = Path.GetFullPath(Path.Combine(repositoryRoot, model.LocalRelativePath));
        var exists = File.Exists(absolutePath);

        var integrity = _integrityChecker.Check(
            model.ModelId,
            absolutePath,
            model.Integrity.Algorithm,
            model.Integrity.Checksum,
            model.Integrity.FileSizeBytes);

        var shapeKnown = !string.IsNullOrWhiteSpace(model.InputShapeDescription) &&
                         !string.IsNullOrWhiteSpace(model.OutputShapeDescription);

        if (!exists)
        {
            return VerificationResult(model, false, false, false, licenseAccepted, shapeKnown, NodalOsPaddleOcrOnnxModelStatus.Missing, "model file missing");
        }

        if (model.Integrity.FileSizeBytes == 0)
        {
            return VerificationResult(model, true, false, false, licenseAccepted, shapeKnown, NodalOsPaddleOcrOnnxModelStatus.Invalid, "expected file size not set");
        }

        if (!integrity.Match)
        {
            return VerificationResult(model, true, false, false, licenseAccepted, shapeKnown, NodalOsPaddleOcrOnnxModelStatus.Invalid, integrity.Reason);
        }

        if (!licenseAccepted)
        {
            return VerificationResult(model, true, true, true, false, shapeKnown, NodalOsPaddleOcrOnnxModelStatus.BlockedByLicense, "license not accepted");
        }

        if (!shapeKnown)
        {
            return VerificationResult(model, true, true, true, true, false, NodalOsPaddleOcrOnnxModelStatus.Invalid, "model input/output shapes unknown");
        }

        return VerificationResult(model, true, true, true, true, true, NodalOsPaddleOcrOnnxModelStatus.Verified, "model verified");
    }

    public NodalOsPaddleOcrOnnxModelManifest VerifyManifest(
        NodalOsPaddleOcrOnnxModelManifest manifest,
        string repositoryRoot,
        bool licenseAccepted)
    {
        var verified = manifest.Models.Select(m =>
        {
            var result = Verify(m, repositoryRoot, licenseAccepted);
            return m with { Status = result.Status };
        }).ToList();

        var required = verified.Where(m => m.Kind is NodalOsPaddleOcrOnnxModelKind.TextDetection or NodalOsPaddleOcrOnnxModelKind.TextRecognition).ToList();
        var allRequiredPresent = required.All(m => m.Status != NodalOsPaddleOcrOnnxModelStatus.Missing);
        var allVerified = required.All(m => m.Status == NodalOsPaddleOcrOnnxModelStatus.Verified);

        return manifest with
        {
            Models = verified,
            AllRequiredPresent = allRequiredPresent,
            AllVerified = allVerified
        };
    }

    private static NodalOsPaddleOcrOnnxModelVerificationResult VerificationResult(
        NodalOsPaddleOcrOnnxModelRef model,
        bool fileExists,
        bool sizeMatches,
        bool checksumMatches,
        bool licenseAccepted,
        bool shapeKnown,
        NodalOsPaddleOcrOnnxModelStatus status,
        string reason)
    {
        return new NodalOsPaddleOcrOnnxModelVerificationResult(
            $"verify-{Guid.NewGuid():N}",
            model.ModelId,
            fileExists,
            sizeMatches,
            checksumMatches,
            licenseAccepted,
            shapeKnown,
            status,
            BrowserCredentialRedactor.Redact(reason),
            NoAuthority: true,
            Redacted: true);
    }
}
