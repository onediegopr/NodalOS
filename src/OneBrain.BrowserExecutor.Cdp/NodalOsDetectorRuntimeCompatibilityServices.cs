using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.BrowserExecutor.Cdp;

public sealed class NodalOsDetectorRuntimeCompatibilityExperimentBuilder
{
    public IReadOnlyList<NodalOsDetectorRuntimeExperiment> BuildMinimalMatrix()
    {
        var shape = new[] { 1, 3, 640, 640 };
        var tensors = new[]
        {
            NodalOsDetectorRuntimeProbeTensorKind.Zero,
            NodalOsDetectorRuntimeProbeTensorKind.Ones,
            NodalOsDetectorRuntimeProbeTensorKind.Gradient,
            NodalOsDetectorRuntimeProbeTensorKind.SyntheticText,
            NodalOsDetectorRuntimeProbeTensorKind.CurrentPreprocessedSyntheticText,
            NodalOsDetectorRuntimeProbeTensorKind.SafeRectangle,
            NodalOsDetectorRuntimeProbeTensorKind.SafeCircle
        };

        var options = new[]
        {
            Option(NodalOsDetectorRuntimeSessionOptionKind.Default),
            Option(NodalOsDetectorRuntimeSessionOptionKind.GraphOptimizationDisabled),
            Option(NodalOsDetectorRuntimeSessionOptionKind.GraphOptimizationBasic),
            Option(NodalOsDetectorRuntimeSessionOptionKind.SingleThreaded),
            Option(NodalOsDetectorRuntimeSessionOptionKind.MemoryPatternDisabled),
            Option(NodalOsDetectorRuntimeSessionOptionKind.CpuArenaDisabled)
        };

        var experiments = new List<NodalOsDetectorRuntimeExperiment>();
        foreach (var tensor in tensors)
        {
            foreach (var option in options)
            {
                experiments.Add(new NodalOsDetectorRuntimeExperiment(
                    $"det-runtime-{tensor}-{option.OptionKind}-{Guid.NewGuid():N}",
                    tensor,
                    NodalOsDetectorRuntimeProbeLayout.Nchw,
                    shape,
                    option,
                    RequiresOutOfProcessGuard: true,
                    AllowInProcess: false,
                    Synthetic: true,
                    FullScreen: false,
                    Sensitive: false,
                    RawPersisted: false,
                    NoAuthority: true));
            }
        }

        experiments.Add(new NodalOsDetectorRuntimeExperiment(
            $"det-runtime-nhwc-skipped-{Guid.NewGuid():N}",
            NodalOsDetectorRuntimeProbeTensorKind.Zero,
            NodalOsDetectorRuntimeProbeLayout.Nhwc,
            [1, 640, 640, 3],
            Option(NodalOsDetectorRuntimeSessionOptionKind.Default),
            RequiresOutOfProcessGuard: false,
            AllowInProcess: false,
            Synthetic: true,
            FullScreen: false,
            Sensitive: false,
            RawPersisted: false,
            NoAuthority: true));

        return experiments;
    }

    public static NodalOsDetectorRuntimeSessionOptionsMetadata Option(NodalOsDetectorRuntimeSessionOptionKind kind) =>
        kind switch
        {
            NodalOsDetectorRuntimeSessionOptionKind.GraphOptimizationDisabled => new(kind, "graph optimization disabled", true, true, false, null, null, false, false),
            NodalOsDetectorRuntimeSessionOptionKind.GraphOptimizationBasic => new(kind, "graph optimization basic", true, false, true, null, null, false, false),
            NodalOsDetectorRuntimeSessionOptionKind.SingleThreaded => new(kind, "intra-op/inter-op threads = 1", true, false, false, 1, 1, false, false),
            NodalOsDetectorRuntimeSessionOptionKind.MemoryPatternDisabled => new(kind, "memory pattern disabled", true, false, false, null, null, true, false),
            NodalOsDetectorRuntimeSessionOptionKind.CpuArenaDisabled => new(kind, "CPU arena disabled", true, false, false, null, null, false, true),
            _ => new(kind, "default CPU session options", true, false, false, null, null, false, false)
        };
}

public sealed class NodalOsDetectorRuntimeCompatibilityDecisionService
{
    public NodalOsDetectorModelCompatibilityFinding Decide(
        IReadOnlyList<NodalOsDetectorRuntimeCompatibilityResult> results,
        bool modelVerified,
        bool parentSurvived,
        bool tempCleanup,
        bool noRawPersistence,
        bool noAuthority)
    {
        if (!modelVerified || !parentSurvived || !tempCleanup || !noRawPersistence || !noAuthority || results.Count == 0)
        {
            return Finding(NodalOsDetectorRuntimeCompatibilityDecision.NotReady, modelVerified, results, false, false, false, false,
                "model/gate prerequisites not satisfied");
        }

        var runResults = results.Where(r => r.RunAttempted).ToList();
        var succeeded = runResults.Where(r => r.Status is NodalOsDetectorRuntimeCompatibilityStatus.RunSucceeded or NodalOsDetectorRuntimeCompatibilityStatus.OutputMetadataCaptured).ToList();
        var crashes = runResults.Where(r => r.Status == NodalOsDetectorRuntimeCompatibilityStatus.NativeRuntimeCrashContained).ToList();
        var optionSuccess = succeeded.Any(r => r.Experiment.SessionOptions.OptionKind != NodalOsDetectorRuntimeSessionOptionKind.Default);
        var defaultCrash = crashes.Any(r => r.Experiment.SessionOptions.OptionKind == NodalOsDetectorRuntimeSessionOptionKind.Default);
        var allCrash = runResults.Count > 0 && crashes.Count == runResults.Count;
        var tensorSpecificCrash = crashes.Count > 0 && succeeded.Count > 0;
        var metadataMismatch = results.Any(r => r.Status == NodalOsDetectorRuntimeCompatibilityStatus.OutputMetadataCaptured && r.OutputShapes.Count > 0 && r.OutputShapes.All(s => s.Length != 4));

        if (optionSuccess && defaultCrash)
            return Finding(NodalOsDetectorRuntimeCompatibilityDecision.ReadyForSessionOptionsFix, modelVerified, results, optionSuccess, allCrash, tensorSpecificCrash, metadataMismatch,
                "a non-default session option avoided a default crash");

        if (metadataMismatch)
            return Finding(NodalOsDetectorRuntimeCompatibilityDecision.ReadyForPostProcessingFix, modelVerified, results, optionSuccess, allCrash, tensorSpecificCrash, true,
                "detector output metadata does not match postprocessor expectations");

        if (tensorSpecificCrash)
        {
            var textCrashes = crashes.Any(r => r.Experiment.TensorKind is NodalOsDetectorRuntimeProbeTensorKind.SyntheticText or NodalOsDetectorRuntimeProbeTensorKind.CurrentPreprocessedSyntheticText);
            var safeSucceeded = succeeded.Any(r => r.Experiment.TensorKind is NodalOsDetectorRuntimeProbeTensorKind.Zero or NodalOsDetectorRuntimeProbeTensorKind.Ones or NodalOsDetectorRuntimeProbeTensorKind.SafeRectangle or NodalOsDetectorRuntimeProbeTensorKind.SafeCircle);
            var decision = textCrashes && safeSucceeded
                ? NodalOsDetectorRuntimeCompatibilityDecision.ReadyForRendererFix
                : NodalOsDetectorRuntimeCompatibilityDecision.ReadyForPreProcessingFix;
            return Finding(decision, modelVerified, results, optionSuccess, allCrash, true, metadataMismatch,
                "crash depends on tensor content");
        }

        if (allCrash)
            return Finding(NodalOsDetectorRuntimeCompatibilityDecision.ReadyForOnnxRuntimeVersionExperiment, modelVerified, results, optionSuccess, true, false, metadataMismatch,
                "all detector run attempts crashed before postprocessing; isolate ONNX Runtime version next");

        if (succeeded.Count == 0 && crashes.Count > 0)
            return Finding(NodalOsDetectorRuntimeCompatibilityDecision.BlockedByModelRuntime, modelVerified, results, optionSuccess, false, false, metadataMismatch,
                "detector remains blocked by model runtime");

        return Finding(NodalOsDetectorRuntimeCompatibilityDecision.ReadyForDetectorModelReplacement, modelVerified, results, optionSuccess, false, false, metadataMismatch,
            "current detector model compatibility remains inconclusive");
    }

    private static NodalOsDetectorModelCompatibilityFinding Finding(
        NodalOsDetectorRuntimeCompatibilityDecision decision,
        bool modelVerified,
        IReadOnlyList<NodalOsDetectorRuntimeCompatibilityResult> results,
        bool optionSuccess,
        bool allCrash,
        bool tensorSpecificCrash,
        bool metadataMismatch,
        string reason) =>
        new(
            $"det-runtime-finding-{Guid.NewGuid():N}",
            decision,
            modelVerified,
            results.Any(r => r.Status is NodalOsDetectorRuntimeCompatibilityStatus.RunSucceeded or NodalOsDetectorRuntimeCompatibilityStatus.OutputMetadataCaptured),
            optionSuccess,
            allCrash,
            tensorSpecificCrash,
            metadataMismatch,
            ShadowModeBlocked: true,
            ProductiveOcrBlocked: true,
            NoAuthority: results.Count == 0 || results.All(r => r.NoAuthority),
            BrowserCredentialRedactor.Redact(reason));
}
