using System.Security.Cryptography;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.BrowserExecutor.Cdp;

// M195 — PaddleOCR ONNX model manifest, integrity and acquisition plan.
// Models are NOT downloaded automatically. Readiness stays blocked until verified models exist.
public sealed class NodalOsPaddleOcrOnnxModelService
{
    public const string ManifestVersion = "paddleocr-onnx-model-manifest.v1";
    public const long DefaultMaxTotalModelSizeBytes = 250 * 1024 * 1024; // 250 MB

    public NodalOsPaddleOcrOnnxModelManifest CreatePlaceholderManifest() =>
        new(
            $"manifest-{Guid.NewGuid():N}",
            ManifestVersion,
           
            [
                new(
                    "paddleocr-det-onnx",
                    NodalOsPaddleOcrOnnxModelKind.TextDetection,
                    "v4",
                    "multi",
                    new NodalOsPaddleOcrOnnxModelSource(
                        "PaddleOCR official or Paddle2ONNX conversion",
                        "https://github.com/PaddlePaddle/PaddleOCR",
                        "Apache-2.0",
                        "https://github.com/PaddlePaddle/PaddleOCR/blob/main/LICENSE",
                        RequiresAttribution: true),
                    "tools/ocr-worker/models/onnx/ch_PP-OCRv4_det.onnx",
                    new NodalOsPaddleOcrOnnxModelIntegrity("SHA-256", "", 0),
                    "NCHW [1,3,?,?]",
                    "detection maps",
                    17,
                    NodalOsPaddleOcrOnnxModelStatus.Missing),
                new(
                    "paddleocr-rec-onnx",
                    NodalOsPaddleOcrOnnxModelKind.TextRecognition,
                    "v4",
                    "en",
                    new NodalOsPaddleOcrOnnxModelSource(
                        "PaddleOCR official or Paddle2ONNX conversion",
                        "https://github.com/PaddlePaddle/PaddleOCR",
                        "Apache-2.0",
                        "https://github.com/PaddlePaddle/PaddleOCR/blob/main/LICENSE",
                        RequiresAttribution: true),
                    "tools/ocr-worker/models/onnx/ch_PP-OCRv4_rec.onnx",
                    new NodalOsPaddleOcrOnnxModelIntegrity("SHA-256", "", 0),
                    "NCHW [1,3,32,320]",
                    "text logits",
                    17,
                    NodalOsPaddleOcrOnnxModelStatus.Missing),
                new(
                    "paddleocr-cls-onnx",
                    NodalOsPaddleOcrOnnxModelKind.TextDirectionClassification,
                    "v2",
                    "multi",
                    new NodalOsPaddleOcrOnnxModelSource(
                        "PaddleOCR official or Paddle2ONNX conversion",
                        "https://github.com/PaddlePaddle/PaddleOCR",
                        "Apache-2.0",
                        "https://github.com/PaddlePaddle/PaddleOCR/blob/main/LICENSE",
                        RequiresAttribution: true),
                    "tools/ocr-worker/models/onnx/ch_ppocr_mobile_v2.0_cls.onnx",
                    new NodalOsPaddleOcrOnnxModelIntegrity("SHA-256", "", 0),
                    "NCHW [1,3,48,192]",
                    "direction logits",
                    17,
                    NodalOsPaddleOcrOnnxModelStatus.Missing)
            ],
            TotalMaxSizeBytes: DefaultMaxTotalModelSizeBytes,
            AllRequiredPresent: false,
            AllVerified: false,
            LicenseReviewed: false,
            NoAuthority: true,
            Redacted: true);

    public NodalOsPaddleOcrOnnxModelReadiness Evaluate(NodalOsPaddleOcrOnnxModelManifest manifest)
    {
        if (!manifest.LicenseReviewed)
            return Readiness(NodalOsPaddleOcrOnnxModelStatus.BlockedByLicense, "license not reviewed");

        var required = manifest.Models.Where(m => m.Kind is NodalOsPaddleOcrOnnxModelKind.TextDetection or NodalOsPaddleOcrOnnxModelKind.TextRecognition).ToList();
        if (required.Any(m => m.Status == NodalOsPaddleOcrOnnxModelStatus.Missing))
            return Readiness(NodalOsPaddleOcrOnnxModelStatus.Missing, "required detection/recognition models missing");

        if (required.Any(m => m.Status != NodalOsPaddleOcrOnnxModelStatus.Verified))
            return Readiness(required.First(m => m.Status != NodalOsPaddleOcrOnnxModelStatus.Verified).Status, "required models not verified");

        var totalSize = manifest.Models.Sum(m => m.Integrity.FileSizeBytes);
        if (totalSize > manifest.TotalMaxSizeBytes)
            return Readiness(NodalOsPaddleOcrOnnxModelStatus.BlockedBySize, $"total model size {totalSize} exceeds limit {manifest.TotalMaxSizeBytes}");

        return Readiness(NodalOsPaddleOcrOnnxModelStatus.Verified, "required models present and verified", canRunOcr: true);
    }

    public NodalOsPaddleOcrOnnxModelAcquisitionPlan AcquisitionPlan() =>
        new(
            $"plan-{Guid.NewGuid():N}",
            [
                "A. Download pre-converted PaddleOCR ONNX models from official/community source if available and license-compatible.",
                "B. Convert PaddleOCR models to ONNX using Paddle2ONNX in a controlled Python 3.10/3.11 environment.",
                "C. If ONNX path fails, fall back to C++ Paddle Inference local worker.",
                "D. Verify SHA-256 checksums against manifest.",
                "E. Store models in tools/ocr-worker/models/onnx/ or external configured cache.",
                "F. Review Apache-2.0 license and attribution requirements.",
                "G. Do NOT commit large model files to Git without explicit decision."
            ],
            RecommendedOption: "A or B with checksum verification and license review",
            ModelStorageDirectory: "tools/ocr-worker/models/onnx",
            MaxTotalModelSizeBytes: DefaultMaxTotalModelSizeBytes,
            RequiresLicenseReview: true,
            RequiresManualDownload: true,
            NoAuthority: true,
            Redacted: true);

    public bool VerifyChecksum(string filePath, string expectedChecksum, string algorithm = "SHA-256")
    {
        if (!File.Exists(filePath))
            return false;
        if (string.IsNullOrWhiteSpace(expectedChecksum))
            return false;

        using var stream = File.OpenRead(filePath);
        var hash = algorithm.ToUpperInvariant() switch
        {
            "SHA-256" => Convert.ToHexStringLower(SHA256.HashData(stream)),
            "SHA-512" => Convert.ToHexStringLower(SHA512.HashData(stream)),
            _ => throw new NotSupportedException($"hash algorithm {algorithm} not supported")
        };
        return hash.Equals(expectedChecksum, StringComparison.OrdinalIgnoreCase);
    }

    private static NodalOsPaddleOcrOnnxModelReadiness Readiness(
        NodalOsPaddleOcrOnnxModelStatus status,
        string reason,
        bool canRunOcr = false) =>
        new(
            $"readiness-{Guid.NewGuid():N}",
            canRunOcr,
            status,
            BrowserCredentialRedactor.Redact(reason),
            canRunOcr,
            NoAuthority: true,
            Redacted: true);
}
