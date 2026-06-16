using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("PixelRedaction")]
[TestCategory("ImagePixelRedaction")]
[TestCategory("RealImageRedaction")]
[TestCategory("OcrImageRedaction")]
[TestCategory("OcrRedactionPrecondition")]
[TestCategory("OcrVisionNoAuthority")]
[TestCategory("OcrVisionPrivacy")]
[TestCategory("BrowserGroundingSnapshot")]
[TestCategory("PrivatePreviewReadiness")]
[TestCategory("LocalPreviewReleaseCandidate")]
[TestCategory("NodalOsNamingAudit")]
[TestCategory("BrowserRuntimePhaseGate")]
public sealed class NodalOsPixelRedactionM188Tests
{
    private const int W = 64;
    private const int H = 48;

    // ── Real synthetic image helpers (raw RGBA32) ─────────────────────────

    private static byte[] SolidImage(int width, int height, byte r, byte g, byte b)
    {
        var bytes = new byte[width * height * 4];
        for (var i = 0; i < width * height; i++)
        {
            bytes[(i * 4) + 0] = r;
            bytes[(i * 4) + 1] = g;
            bytes[(i * 4) + 2] = b;
            bytes[(i * 4) + 3] = 0xFF;
        }
        return bytes;
    }

    private static void FillRegion(byte[] rgba, int width, int x, int y, int rw, int rh, byte r, byte g, byte b)
    {
        for (var yy = y; yy < y + rh; yy++)
            for (var xx = x; xx < x + rw; xx++)
            {
                var o = ((yy * width) + xx) * 4;
                rgba[o] = r; rgba[o + 1] = g; rgba[o + 2] = b; rgba[o + 3] = 0xFF;
            }
    }

    private static bool RegionIsBlack(byte[] rgba, int width, NodalOsPixelRedactionRegion region)
    {
        for (var yy = region.Y; yy < region.Y + region.Height; yy++)
            for (var xx = region.X; xx < region.X + region.Width; xx++)
            {
                var o = ((yy * width) + xx) * 4;
                if (rgba[o] != 0 || rgba[o + 1] != 0 || rgba[o + 2] != 0 || rgba[o + 3] != 0xFF)
                    return false;
            }
        return true;
    }

    private static NodalOsPixelRedactionRequest Request(
        byte[] bytes, int width, int height,
        NodalOsOcrVisionSensitivity sensitivity,
        IReadOnlyList<NodalOsPixelRedactionRegion> regions,
        bool allowRawPersistence = false,
        bool allowFullScreen = false) =>
        new(
            $"req-{Guid.NewGuid():N}",
            bytes,
            NodalOsPixelRedactionImageFormat.RawRgba32,
            width,
            height,
            new NodalOsOcrBoundingBox(0, 0, width, height),
            default,
            sensitivity,
            regions,
            allowRawPersistence,
            allowFullScreen);

    private static NodalOsPixelRedactionRegion Region(NodalOsPixelRedactionRegionKind kind, double confidence = 0.9) =>
        new(8, 8, 16, 8, kind, confidence);

    // ── Tests ─────────────────────────────────────────────────────────────

    [TestMethod]
    public void CleanImage_NoRegions_LowSensitivity_IsCleanAndSafeForOcr()
    {
        var img = SolidImage(W, H, 255, 255, 255);
        var result = new NodalOsPixelImageRedactor().Redact(Request(img, W, H, NodalOsOcrVisionSensitivity.Low, []));

        Assert.AreEqual(NodalOsPixelRedactionDecision.CleanNoRedactionRequired, result.Decision);
        Assert.IsTrue(result.SafeForOcr);
        Assert.IsFalse(result.OriginalRawPersisted);
        Assert.IsTrue(result.NoAuthority);
        Assert.IsTrue(NodalOsPixelImageRedactor.IsValidForRealOcr(result));
    }

    [TestMethod]
    public void SensitiveRegion_IsMaskedAtPixelLevel_AndHashChanges()
    {
        var kinds = new[]
        {
            NodalOsPixelRedactionRegionKind.Password,
            NodalOsPixelRedactionRegionKind.Token,
            NodalOsPixelRedactionRegionKind.Jwt,
            NodalOsPixelRedactionRegionKind.Cookie,
            NodalOsPixelRedactionRegionKind.ApiKey,
            NodalOsPixelRedactionRegionKind.Pii
        };

        foreach (var kind in kinds)
        {
            var img = SolidImage(W, H, 255, 255, 255);
            var region = Region(kind);
            // Draw "visible sensitive text" as red pixels in the region.
            FillRegion(img, W, region.X, region.Y, region.Width, region.Height, 255, 0, 0);

            var result = new NodalOsPixelImageRedactor().Redact(Request(img, W, H, NodalOsOcrVisionSensitivity.High, [region]));

            Assert.AreEqual(NodalOsPixelRedactionDecision.RedactedPixels, result.Decision, $"kind={kind}");
            Assert.AreEqual(NodalOsPixelRedactionVerificationStatus.Verified, result.VerificationStatus, $"kind={kind}");
            Assert.AreNotEqual(result.OriginalImageHash, result.RedactedImageHash, $"redacted hash must differ from original (kind={kind})");
            Assert.IsNotNull(result.RedactedImageBytesForOcrHandoff);
            Assert.IsTrue(RegionIsBlack(result.RedactedImageBytesForOcrHandoff!, W, region), $"masked region pixels must be black (kind={kind})");
            Assert.IsFalse(result.OriginalRawPersisted);
            Assert.IsTrue(result.SafeForOcr);
            Assert.IsTrue(result.NoAuthority);
            Assert.IsTrue(NodalOsPixelImageRedactor.IsValidForRealOcr(result));
        }
    }

    [TestMethod]
    public void RegionOutsideBounds_FailsClosed()
    {
        var img = SolidImage(W, H, 255, 255, 255);
        var region = new NodalOsPixelRedactionRegion(W - 4, H - 4, 16, 16, NodalOsPixelRedactionRegionKind.Password, 0.9);

        var result = new NodalOsPixelImageRedactor().Redact(Request(img, W, H, NodalOsOcrVisionSensitivity.High, [region]));

        Assert.AreEqual(NodalOsPixelRedactionDecision.RedactionFailed, result.Decision);
        Assert.IsFalse(result.SafeForOcr);
        Assert.IsFalse(NodalOsPixelImageRedactor.IsValidForRealOcr(result));
    }

    [TestMethod]
    public void MalformedImageBytes_FailsClosed()
    {
        var img = new byte[(W * H * 4) - 10]; // wrong length
        var result = new NodalOsPixelImageRedactor().Redact(Request(img, W, H, NodalOsOcrVisionSensitivity.Low, []));

        Assert.AreEqual(NodalOsPixelRedactionDecision.RedactionFailed, result.Decision);
        Assert.IsFalse(result.SafeForOcr);
    }

    [TestMethod]
    public void EmptyImageBytes_FailsClosed()
    {
        var result = new NodalOsPixelImageRedactor().Redact(Request([], W, H, NodalOsOcrVisionSensitivity.Low, []));
        Assert.AreEqual(NodalOsPixelRedactionDecision.RedactionFailed, result.Decision);
    }

    [TestMethod]
    public void OversizedFullScreen_IsBlocked()
    {
        var big = SolidImage(2000, 20, 255, 255, 255);
        var result = new NodalOsPixelImageRedactor().Redact(Request(big, 2000, 20, NodalOsOcrVisionSensitivity.Low, []));

        Assert.AreEqual(NodalOsPixelRedactionDecision.RedactionFailed, result.Decision);
        Assert.IsFalse(result.SafeForOcr);
    }

    [TestMethod]
    public void AllowFullScreen_IsBlocked()
    {
        var img = SolidImage(W, H, 255, 255, 255);
        var result = new NodalOsPixelImageRedactor().Redact(Request(img, W, H, NodalOsOcrVisionSensitivity.Low, [], allowFullScreen: true));
        Assert.AreEqual(NodalOsPixelRedactionDecision.RedactionFailed, result.Decision);
    }

    [TestMethod]
    public void RawPersistenceRequested_FailsClosed()
    {
        var img = SolidImage(W, H, 255, 255, 255);
        var result = new NodalOsPixelImageRedactor().Redact(Request(img, W, H, NodalOsOcrVisionSensitivity.Low, [], allowRawPersistence: true));

        Assert.AreEqual(NodalOsPixelRedactionDecision.RedactionFailed, result.Decision);
        Assert.IsFalse(result.OriginalRawPersisted);
    }

    [TestMethod]
    public void HighSensitivity_NoRegion_IsBlockedSensitive()
    {
        var img = SolidImage(W, H, 255, 255, 255);
        var result = new NodalOsPixelImageRedactor().Redact(Request(img, W, H, NodalOsOcrVisionSensitivity.Credentials, []));

        Assert.AreEqual(NodalOsPixelRedactionDecision.BlockedSensitive, result.Decision);
        Assert.IsFalse(result.SafeForOcr);
        Assert.IsFalse(NodalOsPixelImageRedactor.IsValidForRealOcr(result));
    }

    [TestMethod]
    public void HighSensitivity_OnlyLowConfidenceRegion_IsBlockedSensitive()
    {
        var img = SolidImage(W, H, 255, 255, 255);
        var region = Region(NodalOsPixelRedactionRegionKind.LowConfidence, confidence: 0.2);

        var result = new NodalOsPixelImageRedactor().Redact(Request(img, W, H, NodalOsOcrVisionSensitivity.PersonalData, [region]));

        Assert.AreEqual(NodalOsPixelRedactionDecision.BlockedSensitive, result.Decision);
        Assert.IsFalse(result.SafeForOcr);
    }

    [TestMethod]
    public void BlockedAndFailed_AreNeverValidForOcr_AndPreserveNoAuthority()
    {
        var img = SolidImage(W, H, 255, 255, 255);
        var blocked = new NodalOsPixelImageRedactor().Redact(Request(img, W, H, NodalOsOcrVisionSensitivity.Credentials, []));
        var failed = new NodalOsPixelImageRedactor().Redact(Request([], W, H, NodalOsOcrVisionSensitivity.Low, []));

        Assert.IsFalse(NodalOsPixelImageRedactor.IsValidForRealOcr(blocked));
        Assert.IsFalse(NodalOsPixelImageRedactor.IsValidForRealOcr(failed));
        Assert.IsTrue(blocked.NoAuthority);
        Assert.IsTrue(failed.NoAuthority);
    }
}
