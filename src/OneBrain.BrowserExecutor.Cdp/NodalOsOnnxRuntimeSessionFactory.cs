using System.Diagnostics;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.BrowserExecutor.Cdp;

// M202 — ONNX Runtime .NET session factory.
// Creates InferenceSession for verified models. No real OCR inference unless explicitly allowed by policy.
public sealed class NodalOsOnnxRuntimeSessionFactory
{
    public NodalOsOnnxModelSessionSmokeResult CreateSession(
        NodalOsPaddleOcrOnnxModelRef model,
        string repositoryRoot,
        NodalOsOnnxRuntimeSessionPolicy policy)
    {
        var stopwatch = Stopwatch.StartNew();
        var absolutePath = Path.GetFullPath(Path.Combine(repositoryRoot, model.LocalRelativePath));

        if (!File.Exists(absolutePath))
        {
            stopwatch.Stop();
            return SmokeResult(model.ModelId, absolutePath, NodalOsOnnxModelSessionSmokeStatus.ModelMissing, "model file not found", false, null, null, policy, stopwatch.Elapsed);
        }

        if (!policy.AllowRealModelLoad)
        {
            stopwatch.Stop();
            return SmokeResult(model.ModelId, absolutePath, NodalOsOnnxModelSessionSmokeStatus.PolicyBlocked, "policy blocks model load", false, null, null, policy, stopwatch.Elapsed);
        }

        try
        {
            using var session = new InferenceSession(absolutePath);
            var runtimeVersion = typeof(InferenceSession).Assembly.GetName().Version?.ToString();
            var provider = session.InputMetadata.Count > 0 ? "CPU" : null;

            var inputNames = session.InputMetadata.Keys.ToList();
            var outputNames = session.OutputMetadata.Keys.ToList();
            var inputShapes = session.InputMetadata.Values.Select(m => m.Dimensions.ToArray()).ToList();
            var outputShapes = session.OutputMetadata.Values.Select(m => m.Dimensions.ToArray()).ToList();

            stopwatch.Stop();
            return SmokeResult(
                model.ModelId,
                absolutePath,
                NodalOsOnnxModelSessionSmokeStatus.Success,
                "session created and metadata inspected",
                true,
                runtimeVersion,
                provider,
                policy,
                stopwatch.Elapsed,
                inputNames,
                outputNames,
                inputShapes,
                outputShapes);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return SmokeResult(model.ModelId, absolutePath, NodalOsOnnxModelSessionSmokeStatus.SessionLoadFailed, $"session load failed: {ex.Message}", false, null, null, policy, stopwatch.Elapsed);
        }
    }

    public NodalOsOnnxModelSessionSmokeResult RunDummyInference(
        NodalOsPaddleOcrOnnxModelRef model,
        string repositoryRoot,
        NodalOsOnnxRuntimeSessionPolicy policy)
    {
        var sessionResult = CreateSession(model, repositoryRoot, policy);
        if (sessionResult.Status != NodalOsOnnxModelSessionSmokeStatus.Success)
            return sessionResult with { DummyInferenceRun = false, RealImageInferenceRun = false };

        if (!policy.AllowDummyInference)
        {
            return sessionResult with
            {
                Status = NodalOsOnnxModelSessionSmokeStatus.PolicyBlocked,
                Reason = "policy blocks dummy inference",
                DummyInferenceRun = false,
                RealImageInferenceRun = false
            };
        }

        try
        {
            var absolutePath = Path.GetFullPath(Path.Combine(repositoryRoot, model.LocalRelativePath));
            using var session = new InferenceSession(absolutePath);

            var inputs = new List<NamedOnnxValue>();
            foreach (var meta in session.InputMetadata)
            {
                var shape = meta.Value.Dimensions.ToArray();
                // Replace dynamic dimensions with 1 for smoke.
                for (var i = 0; i < shape.Length; i++)
                {
                    if (shape[i] <= 0)
                        shape[i] = 1;
                }

                var length = shape.Aggregate(1, (a, b) => a * b);
                var tensor = new DenseTensor<float>(shape);
                for (var i = 0; i < length; i++)
                    tensor.Buffer.Span[i] = 0.0f;

                inputs.Add(NamedOnnxValue.CreateFromTensor(meta.Key, tensor));
            }

            using var results = session.Run(inputs);
            var outputNames = results.Select(r => r.Name).ToList();

            return sessionResult with
            {
                DummyInferenceRun = true,
                RealImageInferenceRun = false,
                Reason = "dummy inference completed; no real image processed",
                OutputNames = outputNames
            };
        }
        catch (Exception ex)
        {
            return sessionResult with
            {
                Status = NodalOsOnnxModelSessionSmokeStatus.SessionLoadFailed,
                Reason = $"dummy inference failed: {ex.Message}",
                DummyInferenceRun = false,
                RealImageInferenceRun = false
            };
        }
    }

    private static NodalOsOnnxModelSessionSmokeResult SmokeResult(
        string modelId,
        string modelPath,
        NodalOsOnnxModelSessionSmokeStatus status,
        string reason,
        bool sessionCreated,
        string? runtimeVersion,
        string? provider,
        NodalOsOnnxRuntimeSessionPolicy policy,
        TimeSpan duration,
        IReadOnlyList<string>? inputNames = null,
        IReadOnlyList<string>? outputNames = null,
        IReadOnlyList<int[]>? inputShapes = null,
        IReadOnlyList<int[]>? outputShapes = null)
    {
        return new NodalOsOnnxModelSessionSmokeResult(
            $"smoke-{Guid.NewGuid():N}",
            status,
            modelId,
            modelPath,
            sessionCreated,
            runtimeVersion,
            provider,
            inputNames ?? Array.Empty<string>(),
            outputNames ?? Array.Empty<string>(),
            inputShapes ?? Array.Empty<int[]>(),
            outputShapes ?? Array.Empty<int[]>(),
            DummyInferenceRun: false,
            RealImageInferenceRun: false,
            BrowserCredentialRedactor.Redact(reason),
            policy.NoAuthority,
            duration);
    }
}
