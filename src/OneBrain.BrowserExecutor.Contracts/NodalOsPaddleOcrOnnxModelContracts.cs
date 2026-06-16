namespace OneBrain.BrowserExecutor.Contracts;

// M195 — PaddleOCR ONNX model acquisition plan and manifest.
// No models are downloaded by default; readiness is blocked until verified models exist.

public enum NodalOsPaddleOcrOnnxModelKind
{
    TextDetection,
    TextRecognition,
    TextDirectionClassification,
    LayoutOptional,
    TableOptional
}

public enum NodalOsPaddleOcrOnnxModelStatus
{
    Missing,
    Planned,
    Downloaded,
    Verified,
    Invalid,
    BlockedBySize,
    BlockedByLicense,
    BlockedByPolicy
}

public sealed record NodalOsPaddleOcrOnnxModelSource(
    string Origin,
    string Url,
    string LicenseIdentifier,
    string LicenseUrl,
    bool RequiresAttribution);

public sealed record NodalOsPaddleOcrOnnxModelIntegrity(
    string Algorithm,
    string Checksum,
    long FileSizeBytes);

public sealed record NodalOsPaddleOcrOnnxModelRef(
    string ModelId,
    NodalOsPaddleOcrOnnxModelKind Kind,
    string Version,
    string Language,
    NodalOsPaddleOcrOnnxModelSource Source,
    string LocalRelativePath,
    NodalOsPaddleOcrOnnxModelIntegrity Integrity,
    string InputShapeDescription,
    string OutputShapeDescription,
    int OpsetVersion,
    NodalOsPaddleOcrOnnxModelStatus Status);

public sealed record NodalOsPaddleOcrOnnxModelManifest(
    string ManifestId,
    string ContractVersion,
    IReadOnlyList<NodalOsPaddleOcrOnnxModelRef> Models,
    long TotalMaxSizeBytes,
    bool AllRequiredPresent,
    bool AllVerified,
    bool LicenseReviewed,
    bool NoAuthority,
    bool Redacted);

public sealed record NodalOsPaddleOcrOnnxModelReadiness(
    string ReadinessId,
    bool Ready,
    NodalOsPaddleOcrOnnxModelStatus Status,
    string Reason,
    bool CanRunOcr,
    bool NoAuthority,
    bool Redacted);

public sealed record NodalOsPaddleOcrOnnxModelAcquisitionPlan(
    string PlanId,
    IReadOnlyList<string> Steps,
    string RecommendedOption,
    string ModelStorageDirectory,
    long MaxTotalModelSizeBytes,
    bool RequiresLicenseReview,
    bool RequiresManualDownload,
    bool NoAuthority,
    bool Redacted);
