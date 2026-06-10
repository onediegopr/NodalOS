using System.Drawing;
using OneBrain.Core.Visual;

namespace OneBrain.Observation.Visual;

public sealed class VisualVerifier
{
    public VisualVerificationResult Verify(string beforePath, string afterPath, double threshold = 0.005, string? outputDiffPath = null)
    {
        try
        {
            if (!File.Exists(beforePath))
                return Fail($"Before image not found: {beforePath}");
            if (!File.Exists(afterPath))
                return Fail($"After image not found: {afterPath}");

            using var before = new Bitmap(beforePath);
            using var after = new Bitmap(afterPath);

            if (before.Width != after.Width || before.Height != after.Height)
            {
                return new VisualVerificationResult(
                    true, "Images differ in dimensions.", false,
                    beforePath, afterPath,
                    Changed: true, ChangedRatio: 1.0, Threshold: threshold,
                    TotalPixels: 0, ChangedPixels: 0,
                    OutputDiffPath: null,
                    Notes: $"Dimension mismatch: {before.Width}x{before.Height} vs {after.Width}x{after.Height}");
            }

            int totalPixels = before.Width * before.Height;
            int changedPixels = 0;
            int stride = Math.Max(1, (int)Math.Sqrt(totalPixels / 200_000));

            Bitmap? diff = null;
            Graphics? diffGfx = null;

            if (outputDiffPath != null)
            {
                diff = new Bitmap(before.Width, before.Height);
                diffGfx = Graphics.FromImage(diff);
                diffGfx.DrawImage(before, 0, 0);
            }

            try
            {
                for (int y = 0; y < before.Height; y += stride)
                {
                    for (int x = 0; x < before.Width; x += stride)
                    {
                        var bp = before.GetPixel(x, y);
                        var ap = after.GetPixel(x, y);

                        if (bp != ap)
                        {
                            changedPixels += stride * stride;
                            if (diff != null)
                            {
                                for (int dy = 0; dy < stride && y + dy < before.Height; dy++)
                                    for (int dx = 0; dx < stride && x + dx < before.Width; dx++)
                                        diff.SetPixel(x + dx, y + dy, Color.Red);
                            }
                        }
                    }
                }
            }
            finally
            {
                diffGfx?.Dispose();
            }

            changedPixels = Math.Min(changedPixels, totalPixels);
            double ratio = (double)changedPixels / totalPixels;
            bool changed = ratio > threshold;

            string? savedDiffPath = null;
            if (diff != null && outputDiffPath != null)
            {
                var dir = Path.GetDirectoryName(outputDiffPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                diff.Save(outputDiffPath, System.Drawing.Imaging.ImageFormat.Png);
                savedDiffPath = outputDiffPath;
            }
            diff?.Dispose();

            return new VisualVerificationResult(
                true, changed ? "Visual difference above threshold." : "No significant visual difference.",
                true,
                beforePath, afterPath,
                Changed: changed, ChangedRatio: Math.Round(ratio, 6), Threshold: threshold,
                TotalPixels: totalPixels, ChangedPixels: changedPixels,
                OutputDiffPath: savedDiffPath,
                Notes: changed
                    ? $"ChangedRatio {Math.Round(ratio, 6)} exceeds threshold {threshold}."
                    : $"ChangedRatio {Math.Round(ratio, 6)} within threshold {threshold}.");
        }
        catch (Exception ex)
        {
            return Fail($"Verification error: {ex.Message}");
        }
    }

    private static VisualVerificationResult Fail(string message)
        => new VisualVerificationResult(
            false, message, false,
            null, null,
            Changed: false, ChangedRatio: 0, Threshold: 0,
            TotalPixels: 0, ChangedPixels: 0,
            OutputDiffPath: null,
            Notes: $"Exception: {message}");
}

public sealed record VisualVerificationResult(
    bool Success,
    string Message,
    bool ImagesLoaded,
    string? Before,
    string? After,
    bool Changed,
    double ChangedRatio,
    double Threshold,
    int TotalPixels,
    int ChangedPixels,
    string? OutputDiffPath,
    string? Notes);
