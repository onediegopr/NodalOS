using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.BrowserExecutor.Cdp;

// M202 — ONNX model session smoke tester.
// Verifies models and loads ONNX Runtime sessions (metadata + optional dummy inference, no real OCR).
public sealed class NodalOsOnnxModelSessionSmokeTester
{
    private readonly NodalOsPaddleOcrOnnxModelVerifierService _verifier = new();
    private readonly NodalOsOnnxRuntimeSessionFactory _sessionFactory = new();

    public IReadOnlyList<NodalOsOnnxModelSessionSmokeResult> SmokeTestAll(
        NodalOsPaddleOcrOnnxModelManifest manifest,
        string repositoryRoot,
        bool licenseAccepted,
        NodalOsOnnxRuntimeSessionPolicy policy)
    {
        var results = new List<NodalOsOnnxModelSessionSmokeResult>();
        var verifiedManifest = _verifier.VerifyManifest(manifest, repositoryRoot, licenseAccepted);

        foreach (var model in verifiedManifest.Models)
        {
            if (model.Kind is not (NodalOsPaddleOcrOnnxModelKind.TextDetection or NodalOsPaddleOcrOnnxModelKind.TextRecognition or NodalOsPaddleOcrOnnxModelKind.TextDirectionClassification))
                continue;

            if (model.Status != NodalOsPaddleOcrOnnxModelStatus.Verified)
            {
                results.Add(new NodalOsOnnxModelSessionSmokeResult(
                    $"smoke-{Guid.NewGuid():N}",
                    model.Status switch
                    {
                        NodalOsPaddleOcrOnnxModelStatus.Missing => NodalOsOnnxModelSessionSmokeStatus.ModelMissing,
                        _ => NodalOsOnnxModelSessionSmokeStatus.ModelUnverified
                    },
                    model.ModelId,
                    Path.GetFullPath(Path.Combine(repositoryRoot, model.LocalRelativePath)),
                    SessionCreated: false,
                    RuntimeVersion: null,
                    Provider: null,
                    Array.Empty<string>(),
                    Array.Empty<string>(),
                    Array.Empty<int[]>(),
                    Array.Empty<int[]>(),
                    DummyInferenceRun: false,
                    RealImageInferenceRun: false,
                    BrowserCredentialRedactor.Redact($"model not verified: {model.Status}"),
                    policy.NoAuthority,
                    TimeSpan.Zero));
                continue;
            }

            var result = _sessionFactory.RunDummyInference(model, repositoryRoot, policy);
            results.Add(result);
        }

        return results;
    }

    public NodalOsOnnxModelSessionReadiness EvaluateReadiness(
        IReadOnlyList<NodalOsOnnxModelSessionSmokeResult> results)
    {
        var required = results.Where(r => r.ModelId is "paddleocr-det-onnx" or "paddleocr-rec-onnx").ToList();
        var allRequiredVerified = required.All(r => r.Status != NodalOsOnnxModelSessionSmokeStatus.ModelMissing && r.Status != NodalOsOnnxModelSessionSmokeStatus.ModelUnverified);
        var allSessionsLoaded = required.All(r => r.SessionCreated);
        var metadataMatches = required.All(r => r.InputNames.Count > 0 && r.OutputNames.Count > 0);

        if (!allRequiredVerified)
        {
            return Readiness(
                false,
                NodalOsOnnxModelSessionSmokeStatus.ModelMissing,
                "required models missing or unverified",
                allRequiredVerified,
                allSessionsLoaded,
                metadataMatches);
        }

        if (!allSessionsLoaded)
        {
            return Readiness(
                false,
                NodalOsOnnxModelSessionSmokeStatus.SessionLoadFailed,
                "one or more sessions failed to load",
                allRequiredVerified,
                allSessionsLoaded,
                metadataMatches);
        }

        if (!metadataMatches)
        {
            return Readiness(
                false,
                NodalOsOnnxModelSessionSmokeStatus.InputOutputMismatch,
                "input/output metadata mismatch or empty",
                allRequiredVerified,
                allSessionsLoaded,
                metadataMatches);
        }

        return Readiness(
            true,
            NodalOsOnnxModelSessionSmokeStatus.Success,
            "all required models verified and ONNX sessions loaded",
            allRequiredVerified,
            allSessionsLoaded,
            metadataMatches);
    }

    private static NodalOsOnnxModelSessionReadiness Readiness(
        bool ready,
        NodalOsOnnxModelSessionSmokeStatus status,
        string reason,
        bool allRequiredModelsVerified,
        bool allSessionsLoaded,
        bool metadataMatchesManifest)
    {
        return new NodalOsOnnxModelSessionReadiness(
            $"session-readiness-{Guid.NewGuid():N}",
            ready,
            status,
            BrowserCredentialRedactor.Redact(reason),
            allRequiredModelsVerified,
            allSessionsLoaded,
            metadataMatchesManifest,
            NoAuthority: true);
    }
}
