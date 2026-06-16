using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.BrowserExecutor.Cdp;

// M198 — ONNX OCR detector post-processor.
// Simplified DBNet-style segmentation decoding. No fake boxes; unknown shape => UnsupportedModelShape.
public sealed class NodalOsOnnxOcrDetectorPostProcessor
{
    public const float DefaultThreshold = 0.3f;
    public const int MinBoxSize = 4;
    public const int MaxBoxCount = 300;

    public NodalOsOnnxOcrDetectorPostProcessingResult Decode(
        float[] output,
        int[] outputShape,
        int cropWidth,
        int cropHeight,
        float threshold = DefaultThreshold,
        int? maxBoxes = null)
    {
        maxBoxes ??= MaxBoxCount;

        if (output is null || output.Length == 0 || outputShape is null || outputShape.Length == 0)
        {
            return Failure(NodalOsOnnxOcrPostProcessingStatus.InvalidOutput, "empty detector output");
        }

        // Expected shapes: [1,1,H,W], [1,H,W], [H,W].
        int h, w;
        if (outputShape.Length == 4 && outputShape[0] == 1 && outputShape[1] == 1)
        {
            h = outputShape[2];
            w = outputShape[3];
        }
        else if (outputShape.Length == 3 && outputShape[0] == 1)
        {
            h = outputShape[1];
            w = outputShape[2];
        }
        else if (outputShape.Length == 2)
        {
            h = outputShape[0];
            w = outputShape[1];
        }
        else
        {
            return Failure(NodalOsOnnxOcrPostProcessingStatus.UnsupportedModelShape, $"unsupported detector output shape [{string.Join(",", outputShape)}]");
        }

        if (output.Length != h * w)
        {
            return Failure(NodalOsOnnxOcrPostProcessingStatus.UnsupportedModelShape, "detector output length does not match shape");
        }

        var scaleX = (float)cropWidth / w;
        var scaleY = (float)cropHeight / h;

        var mask = new bool[h, w];
        for (var y = 0; y < h; y++)
        {
            for (var x = 0; x < w; x++)
            {
                mask[y, x] = output[y * w + x] >= threshold;
            }
        }

        var visited = new bool[h, w];
        var boxes = new List<NodalOsOnnxOcrTextBox>();
        var boxId = 0;

        for (var y = 0; y < h; y++)
        {
            for (var x = 0; x < w; x++)
            {
                if (!mask[y, x] || visited[y, x])
                    continue;

                var component = FloodFill(mask, visited, x, y, h, w);
                if (component.Count < MinBoxSize)
                    continue;

                var minX = component.Min(p => p.X);
                var maxX = component.Max(p => p.X);
                var minY = component.Min(p => p.Y);
                var maxY = component.Max(p => p.Y);

                var poly = new List<float>
                {
                    minX * scaleX, minY * scaleY,
                    maxX * scaleX, minY * scaleY,
                    maxX * scaleX, maxY * scaleY,
                    minX * scaleX, maxY * scaleY
                };

                var confidence = component.Average(p => output[p.Y * w + p.X]);
                boxes.Add(new NodalOsOnnxOcrTextBox(
                    $"box-{boxId++}",
                    poly,
                    confidence,
                    CropX: (int)(minX * scaleX),
                    CropY: (int)(minY * scaleY),
                    CropWidth: (int)((maxX - minX + 1) * scaleX),
                    CropHeight: (int)((maxY - minY + 1) * scaleY),
                    Valid: true));

                if (boxes.Count >= maxBoxes.Value)
                {
                    return new NodalOsOnnxOcrDetectorPostProcessingResult(
                        $"det-post-{Guid.NewGuid():N}",
                        NodalOsOnnxOcrPostProcessingStatus.BoxCountExceeded,
                        boxes,
                        NoAuthority: true,
                        Redacted: true,
                        $"box count exceeded limit {maxBoxes.Value}");
                }
            }
        }

        var status = boxes.Count == 0 ? NodalOsOnnxOcrPostProcessingStatus.Empty : NodalOsOnnxOcrPostProcessingStatus.Success;
        return new NodalOsOnnxOcrDetectorPostProcessingResult(
            $"det-post-{Guid.NewGuid():N}",
            status,
            boxes,
            NoAuthority: true,
            Redacted: true,
            status == NodalOsOnnxOcrPostProcessingStatus.Empty ? "no text regions above threshold" : "detector boxes decoded");
    }

    private static List<(int X, int Y)> FloodFill(bool[,] mask, bool[,] visited, int startX, int startY, int h, int w)
    {
        var stack = new Stack<(int X, int Y)>();
        stack.Push((startX, startY));
        visited[startY, startX] = true;
        var result = new List<(int, int)> { (startX, startY) };

        while (stack.Count > 0)
        {
            var (x, y) = stack.Pop();
            foreach (var (nx, ny) in new[] { (x - 1, y), (x + 1, y), (x, y - 1), (x, y + 1) })
            {
                if (nx >= 0 && nx < w && ny >= 0 && ny < h && mask[ny, nx] && !visited[ny, nx])
                {
                    visited[ny, nx] = true;
                    stack.Push((nx, ny));
                    result.Add((nx, ny));
                }
            }
        }

        return result;
    }

    private static NodalOsOnnxOcrDetectorPostProcessingResult Failure(NodalOsOnnxOcrPostProcessingStatus status, string reason)
    {
        return new NodalOsOnnxOcrDetectorPostProcessingResult(
            $"det-post-{Guid.NewGuid():N}",
            status,
            Array.Empty<NodalOsOnnxOcrTextBox>(),
            NoAuthority: true,
            Redacted: true,
            BrowserCredentialRedactor.Redact(reason));
    }
}
