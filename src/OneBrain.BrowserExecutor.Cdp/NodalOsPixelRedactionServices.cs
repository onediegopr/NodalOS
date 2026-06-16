using System.Security.Cryptography;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.BrowserExecutor.Cdp;

// M188 — Real pixel redactor V2.
// Masks real pixel bytes (raw RGBA32) over candidate sensitive regions and verifies the masking.
// No OCR. No marker/string detection. Fail-closed on any uncertainty. Raw bytes never persisted.
public sealed class NodalOsPixelImageRedactor
{
    public const int MaxWidth = 1600;
    public const int MaxHeight = 1200;
    public const int BytesPerPixel = 4; // RGBA32
    private const double LowConfidenceThreshold = 0.5;

    // Opaque black mask (R=0,G=0,B=0,A=255).
    private static readonly byte[] MaskPixel = [0x00, 0x00, 0x00, 0xFF];
    private const string MaskColorHex = "#000000FF";

    public NodalOsPixelRedactionResult Redact(NodalOsPixelRedactionRequest request)
    {
        if (request is null || request.ImageBytes is null || request.ImageBytes.Length == 0)
            return Failed(request, "empty or null image bytes", NodalOsPixelRedactionRegionKind.UnknownSensitive);

        if (request.Format != NodalOsPixelRedactionImageFormat.RawRgba32)
            return Failed(request, "unsupported image format", NodalOsPixelRedactionRegionKind.UnknownSensitive);

        if (request.AllowRawPersistence)
            return Failed(request, "raw persistence requested; blocked", NodalOsPixelRedactionRegionKind.UnknownSensitive);

        if (request.Width <= 0 || request.Height <= 0)
            return Failed(request, "invalid image dimensions", NodalOsPixelRedactionRegionKind.UnknownSensitive);

        if (request.ImageBytes.Length != (long)request.Width * request.Height * BytesPerPixel)
            return Failed(request, "malformed image bytes; length does not match width*height*4", NodalOsPixelRedactionRegionKind.UnknownSensitive);

        // Full-screen / oversized is blocked by default.
        if (request.AllowFullScreen || request.Width > MaxWidth || request.Height > MaxHeight)
            return Failed(request, "full-screen or oversized image blocked by default", NodalOsPixelRedactionRegionKind.UnknownSensitive);

        // Any candidate region outside image bounds fails closed.
        foreach (var region in request.CandidateSensitiveRegions)
        {
            if (region.Width <= 0 || region.Height <= 0 ||
                region.X < 0 || region.Y < 0 ||
                region.X + region.Width > request.Width ||
                region.Y + region.Height > request.Height)
            {
                return Failed(request, "candidate sensitive region is outside image bounds", region.Kind);
            }
        }

        var sensitive = request.Sensitivity >= NodalOsOcrVisionSensitivity.SensitiveSurface;
        var regions = request.CandidateSensitiveRegions;
        var hasReliableRegion = regions.Any(region => region.Confidence >= LowConfidenceThreshold);

        var originalHash = Hash(request.ImageBytes);

        // High sensitivity without a reliable region => cannot guarantee redaction => fail-closed.
        if (sensitive && (regions.Count == 0 || !hasReliableRegion))
        {
            return new NodalOsPixelRedactionResult(
                $"pixel-redaction-{Guid.NewGuid():N}",
                NodalOsPixelRedactionDecision.BlockedSensitive,
                originalHash,
                originalHash,
                [],
                NodalOsPixelRedactionVerificationStatus.NotVerified,
                SafeForOcr: false,
                SafeForPersistence: false,
                OriginalRawPersisted: false,
                NoAuthority: true,
                [new NodalOsPixelRedactionFinding(
                    regions.Count == 0 ? NodalOsPixelRedactionRegionKind.UnknownSensitive : NodalOsPixelRedactionRegionKind.LowConfidence,
                    "[REDACTED] sensitive surface without reliable region",
                    0.95,
                    BlocksOcr: true)],
                ["pixel-redaction:blocked-sensitive:redacted"],
                Evidence($"pixel-redaction-evidence-{Guid.NewGuid():N}", originalHash, originalHash, "sensitive surface blocked; no reliable region to mask"),
                Redacted: true);
        }

        // Clean: no candidate regions and not a sensitive surface.
        if (regions.Count == 0)
        {
            return new NodalOsPixelRedactionResult(
                $"pixel-redaction-{Guid.NewGuid():N}",
                NodalOsPixelRedactionDecision.CleanNoRedactionRequired,
                originalHash,
                originalHash,
                [],
                NodalOsPixelRedactionVerificationStatus.Verified,
                SafeForOcr: true,
                SafeForPersistence: false,
                OriginalRawPersisted: false,
                NoAuthority: true,
                [],
                ["pixel-redaction:clean:redacted"],
                Evidence($"pixel-redaction-evidence-{Guid.NewGuid():N}", originalHash, originalHash, "clean crop; no sensitive region detected"),
                Redacted: true);
        }

        // Mask real pixels over each candidate region on a COPY (original is never mutated/persisted).
        var redacted = (byte[])request.ImageBytes.Clone();
        var masks = new List<NodalOsPixelRedactionMask>();
        var findings = new List<NodalOsPixelRedactionFinding>();
        foreach (var region in regions)
        {
            MaskRegion(redacted, request.Width, region);
            masks.Add(new NodalOsPixelRedactionMask(region.X, region.Y, region.Width, region.Height, MaskColorHex));
            findings.Add(new NodalOsPixelRedactionFinding(region.Kind, $"[REDACTED] {region.Kind}", region.Confidence, BlocksOcr: false));
        }

        var redactedHash = Hash(redacted);
        var verified = NodalOsPixelRedactionVerifier.VerifyRegionsMasked(redacted, request.Width, request.Height, masks) &&
                       !string.Equals(redactedHash, originalHash, StringComparison.Ordinal);

        if (!verified)
        {
            // Could not verify the pixels were actually altered => fail-closed.
            return Failed(request, "redaction verification failed; masked regions could not be confirmed altered", regions[0].Kind, originalHash);
        }

        return new NodalOsPixelRedactionResult(
            $"pixel-redaction-{Guid.NewGuid():N}",
            NodalOsPixelRedactionDecision.RedactedPixels,
            originalHash,
            redactedHash,
            masks,
            NodalOsPixelRedactionVerificationStatus.Verified,
            SafeForOcr: true,
            SafeForPersistence: true,
            OriginalRawPersisted: false,
            NoAuthority: true,
            findings,
            ["pixel-redaction:redacted-pixels:redacted"],
            Evidence($"pixel-redaction-evidence-{Guid.NewGuid():N}", originalHash, redactedHash, $"masked {masks.Count} sensitive region(s) and verified pixel alteration"),
            Redacted: true)
        {
            RedactedImageBytesForOcrHandoff = redacted
        };
    }

    // A pixel-redaction result is only valid to hand off to a (future, real) OCR worker when it was
    // truly redacted (or clean), verified, safe, and never persisted raw.
    public static bool IsValidForRealOcr(NodalOsPixelRedactionResult? result) =>
        result is
        {
            Decision: NodalOsPixelRedactionDecision.RedactedPixels or NodalOsPixelRedactionDecision.CleanNoRedactionRequired,
            VerificationStatus: NodalOsPixelRedactionVerificationStatus.Verified,
            SafeForOcr: true,
            OriginalRawPersisted: false,
            NoAuthority: true
        };

    private static void MaskRegion(byte[] rgba, int imageWidth, NodalOsPixelRedactionRegion region)
    {
        for (var y = region.Y; y < region.Y + region.Height; y++)
        {
            for (var x = region.X; x < region.X + region.Width; x++)
            {
                var offset = ((y * imageWidth) + x) * BytesPerPixel;
                rgba[offset] = MaskPixel[0];
                rgba[offset + 1] = MaskPixel[1];
                rgba[offset + 2] = MaskPixel[2];
                rgba[offset + 3] = MaskPixel[3];
            }
        }
    }

    private static string Hash(byte[] bytes) =>
        Convert.ToHexStringLower(SHA256.HashData(bytes));

    private NodalOsPixelRedactionResult Failed(
        NodalOsPixelRedactionRequest? request,
        string reason,
        NodalOsPixelRedactionRegionKind kind,
        string? originalHash = null)
    {
        var hash = originalHash ?? (request?.ImageBytes is { Length: > 0 } bytes ? Hash(bytes) : "");
        return new NodalOsPixelRedactionResult(
            $"pixel-redaction-{Guid.NewGuid():N}",
            NodalOsPixelRedactionDecision.RedactionFailed,
            hash,
            hash,
            [],
            NodalOsPixelRedactionVerificationStatus.VerificationFailed,
            SafeForOcr: false,
            SafeForPersistence: false,
            OriginalRawPersisted: false,
            NoAuthority: true,
            [new NodalOsPixelRedactionFinding(kind, $"[REDACTED] {reason}", 0.99, BlocksOcr: true)],
            ["pixel-redaction:failed:redacted"],
            Evidence($"pixel-redaction-evidence-{Guid.NewGuid():N}", hash, hash, reason),
            Redacted: true);
    }

    private static NodalOsPixelRedactionEvidence Evidence(string id, string originalHash, string redactedHash, string summary) =>
        new(id, originalHash, redactedHash, OriginalRawPersisted: false, [$"{id}:redacted"], summary, Redacted: true);
}

public static class NodalOsPixelRedactionVerifier
{
    private static readonly byte[] MaskPixel = [0x00, 0x00, 0x00, 0xFF];

    // Confirms every pixel inside each mask region equals the mask color.
    public static bool VerifyRegionsMasked(byte[] rgba, int width, int height, IReadOnlyList<NodalOsPixelRedactionMask> masks)
    {
        if (masks.Count == 0)
            return true;

        foreach (var mask in masks)
        {
            for (var y = mask.Y; y < mask.Y + mask.Height; y++)
            {
                for (var x = mask.X; x < mask.X + mask.Width; x++)
                {
                    if (x < 0 || y < 0 || x >= width || y >= height)
                        return false;

                    var offset = ((y * width) + x) * NodalOsPixelImageRedactor.BytesPerPixel;
                    if (rgba[offset] != MaskPixel[0] ||
                        rgba[offset + 1] != MaskPixel[1] ||
                        rgba[offset + 2] != MaskPixel[2] ||
                        rgba[offset + 3] != MaskPixel[3])
                    {
                        return false;
                    }
                }
            }
        }

        return true;
    }
}
