namespace OneBrain.BrowserExecutor.Contracts;

// M188 — Real image/pixel redaction V2.
// Operates on real synthetic pixel bytes (raw RGBA32), not marker/string fixtures.
// Fail-closed: when redaction of sensitive regions cannot be verified, the decision is RedactionFailed.

public enum NodalOsPixelRedactionImageFormat
{
    RawRgba32
}

public enum NodalOsPixelRedactionDecision
{
    RedactedPixels,
    CleanNoRedactionRequired,
    BlockedSensitive,
    RedactionFailed
}

public enum NodalOsPixelRedactionVerificationStatus
{
    Verified,
    NotVerified,
    VerificationFailed
}

public enum NodalOsPixelRedactionRegionKind
{
    Password,
    Token,
    Jwt,
    Cookie,
    ApiKey,
    Pii,
    Email,
    Phone,
    DocumentId,
    CreditCard,
    UnknownSensitive,
    LowConfidence
}

public sealed record NodalOsPixelRedactionRegion(
    int X,
    int Y,
    int Width,
    int Height,
    NodalOsPixelRedactionRegionKind Kind,
    double Confidence);

public sealed record NodalOsPixelRedactionMask(
    int X,
    int Y,
    int Width,
    int Height,
    string MaskColorHex);

public sealed record NodalOsPixelRedactionFinding(
    NodalOsPixelRedactionRegionKind Kind,
    string RedactedPreview,
    double Confidence,
    bool BlocksOcr);

public sealed record NodalOsPixelRedactionEvidence(
    string EvidenceId,
    string OriginalImageHash,
    string RedactedImageHash,
    bool OriginalRawPersisted,
    IReadOnlyList<string> EvidenceRefs,
    string Summary,
    bool Redacted);

public sealed record NodalOsPixelRedactionRequest(
    string RequestId,
    byte[] ImageBytes,
    NodalOsPixelRedactionImageFormat Format,
    int Width,
    int Height,
    NodalOsOcrBoundingBox CropBounds,
    NodalOsOcrPurpose IntendedPurpose,
    NodalOsOcrVisionSensitivity Sensitivity,
    IReadOnlyList<NodalOsPixelRedactionRegion> CandidateSensitiveRegions,
    bool AllowRawPersistence,
    bool AllowFullScreen);

public sealed record NodalOsPixelRedactionResult(
    string ResultId,
    NodalOsPixelRedactionDecision Decision,
    string OriginalImageHash,
    string RedactedImageHash,
    IReadOnlyList<NodalOsPixelRedactionMask> MaskRegionsApplied,
    NodalOsPixelRedactionVerificationStatus VerificationStatus,
    bool SafeForOcr,
    bool SafeForPersistence,
    bool OriginalRawPersisted,
    bool NoAuthority,
    IReadOnlyList<NodalOsPixelRedactionFinding> Findings,
    IReadOnlyList<string> EvidenceRefs,
    NodalOsPixelRedactionEvidence Evidence,
    bool Redacted)
{
    // In-memory only handoff of the REDACTED bytes for a future OCR worker.
    // The ORIGINAL raw bytes are never carried (only their hash). Never written to disk by this layer.
    public byte[]? RedactedImageBytesForOcrHandoff { get; init; }
}
