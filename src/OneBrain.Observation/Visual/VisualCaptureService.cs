using System.Drawing;
using System.Drawing.Imaging;
using OneBrain.Core.Visual;

namespace OneBrain.Observation.Visual;

public sealed class VisualCaptureService
{
    private readonly RegionSelector _regionSelector = new();

    public VisualCaptureResult Capture(VisualCaptureRequest request)
    {
        try
        {
            if (request.FullScreen && !request.AllowFullScreen)
            {
                return new VisualCaptureResult(
                    false,
                    "Fullscreen capture requires --allow-fullscreen flag.",
                    Notes: "Fullscreen capture requires explicit --allow-fullscreen.");
            }

            var region = _regionSelector.Resolve(request);
            if (region == null)
            {
                return new VisualCaptureResult(
                    false,
                    request.Target != null
                        ? $"Element not found for selector in process '{request.ProcessName}'."
                        : request.ProcessName != null
                            ? $"Window not found for process '{request.ProcessName}'."
                            : "No valid region could be resolved.",
                    Notes: "Ensure the target window is visible and the process/selector is correct.");
            }

            if (!region.IsValid)
            {
                return new VisualCaptureResult(
                    false,
                    $"Invalid region dimensions: {region.Width}x{region.Height} (must be > 0).",
                    Region: region,
                    Notes: "The resolved region has zero or negative dimensions.");
            }

            var outputPath = request.OutputPath
                ?? GenerateDefaultPath(region.Source);

            var dir = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            using var bitmap = new Bitmap(region.Width, region.Height);
            using var graphics = Graphics.FromImage(bitmap);

            graphics.CopyFromScreen(
                region.Left, region.Top,
                0, 0,
                new System.Drawing.Size(region.Width, region.Height),
                CopyPixelOperation.SourceCopy);

            bitmap.Save(outputPath, ImageFormat.Png);

            var fileInfo = new FileInfo(outputPath);
            var capturedAt = DateTime.UtcNow.ToString("o");

            return new VisualCaptureResult(
                Success: true,
                Message: "Captured successfully.",
                OutputPath: outputPath,
                Region: region,
                Width: region.Width,
                Height: region.Height,
                Bytes: fileInfo.Exists ? fileInfo.Length : 0,
                CapturedAtUtc: capturedAt,
                Notes: request.FullScreen
                    ? "Fullscreen capture requires explicit --allow-fullscreen."
                    : region.Source == "element"
                        ? "Captured element region only."
                        : "Captured window region only.");
        }
        catch (Exception ex)
        {
            return new VisualCaptureResult(
                false,
                $"Capture failed: {ex.Message}",
                Notes: $"Exception: {ex.GetType().Name}");
        }
    }

    private static string GenerateDefaultPath(string kind)
    {
        var timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
        var dir = Path.Combine(Directory.GetCurrentDirectory(), "artifacts", "visual");

        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        return Path.Combine(dir, $"{timestamp}-{kind}.png");
    }
}
