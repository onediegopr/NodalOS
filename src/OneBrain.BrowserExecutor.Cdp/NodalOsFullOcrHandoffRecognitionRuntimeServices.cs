using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.BrowserExecutor.Cdp;

public sealed class NodalOsFullOcrRuntimeDecisionService
{
    public NodalOsFullOcrRuntimeDecisionReport Decide(NodalOsFullOcrRuntimeDecisionInput input)
    {
        if (!input.ModelsVerified ||
            !input.RiskyOcrNeverRanInProcess ||
            !input.ParentSurvived ||
            !input.TempCleanup ||
            !input.NoRawPersistence ||
            !input.NoFullScreen ||
            !input.NoSensitive ||
            !input.NoSaas ||
            !input.NoAuthority)
        {
            return Report(NodalOsFullOcrRuntimeDecision.NotReady, input, "runtime safety/readiness gates not satisfied");
        }

        var handoffFailure = input.HandoffResults.Any(r => r.Status is
            NodalOsFullOcrHandoffProbeStatus.BlockedByInvalidBox or
            NodalOsFullOcrHandoffProbeStatus.BlockedByOutOfBoundsCrop or
            NodalOsFullOcrHandoffProbeStatus.BlockedByEmptyCrop or
            NodalOsFullOcrHandoffProbeStatus.BlockedByRecognizerTensorShape);

        if (handoffFailure)
            return Report(NodalOsFullOcrRuntimeDecision.ReadyForHandoffFix, input, "box/crop handoff blocks before recognizer runtime");

        var recognizerCrash = input.RecognizerResults.Any(r => r.Status == NodalOsRecognizerRuntimeProbeStatus.NativeRuntimeCrashContained) ||
                              input.HandoffResults.Any(r => r.Status == NodalOsFullOcrHandoffProbeStatus.NativeRuntimeCrashContained);
        var recognizerSucceeded = input.RecognizerResults.Any(r => r.Status is
            NodalOsRecognizerRuntimeProbeStatus.RunSucceeded or
            NodalOsRecognizerRuntimeProbeStatus.OutputMetadataCaptured) ||
            input.HandoffResults.Any(r => r.Status == NodalOsFullOcrHandoffProbeStatus.RecognizerRunSucceeded);

        if (recognizerCrash && !recognizerSucceeded)
            return Report(NodalOsFullOcrRuntimeDecision.ReadyForRecognizerRuntimeExperiment, input, "recognizer session.Run crashes on safe tensors/crops");

        if (input.DictionaryCompatibility.Status is NodalOsOcrDictionaryCompatibilityStatus.ClassCountMismatch or
            NodalOsOcrDictionaryCompatibilityStatus.MissingDictionary or
            NodalOsOcrDictionaryCompatibilityStatus.DictionaryUnverified)
        {
            return recognizerSucceeded
                ? Report(NodalOsFullOcrRuntimeDecision.ReadyForDictionaryCompletion, input, "recognizer runtime works but dictionary/CTC compatibility blocks decode")
                : Report(NodalOsFullOcrRuntimeDecision.BlockedByDictionary, input, "dictionary is incompatible and recognizer runtime has no positive run evidence");
        }

        if (recognizerCrash)
            return Report(NodalOsFullOcrRuntimeDecision.BlockedByModelRuntime, input, "recognizer runtime remains crash-prone");

        if (!recognizerSucceeded && input.RecognizerResults.Count > 0)
            return Report(NodalOsFullOcrRuntimeDecision.ReadyForRecognizerModelReplacement, input, "recognizer runtime did not produce usable output metadata");

        return Report(NodalOsFullOcrRuntimeDecision.ReadyForPostProcessingFix, input, "runtime and dictionary gates are clear; remaining issue is downstream postprocessing");
    }

    private static NodalOsFullOcrRuntimeDecisionReport Report(
        NodalOsFullOcrRuntimeDecision decision,
        NodalOsFullOcrRuntimeDecisionInput input,
        string reason)
    {
        var recognizerSucceeded = input.RecognizerResults.Any(r => r.Status is
            NodalOsRecognizerRuntimeProbeStatus.RunSucceeded or
            NodalOsRecognizerRuntimeProbeStatus.OutputMetadataCaptured) ||
            input.HandoffResults.Any(r => r.Status == NodalOsFullOcrHandoffProbeStatus.RecognizerRunSucceeded);
        var handoffFailure = input.HandoffResults.Any(r => r.Status is
            NodalOsFullOcrHandoffProbeStatus.BlockedByInvalidBox or
            NodalOsFullOcrHandoffProbeStatus.BlockedByOutOfBoundsCrop or
            NodalOsFullOcrHandoffProbeStatus.BlockedByEmptyCrop or
            NodalOsFullOcrHandoffProbeStatus.BlockedByRecognizerTensorShape);
        var recognizerCrash = input.RecognizerResults.Any(r => r.Status == NodalOsRecognizerRuntimeProbeStatus.NativeRuntimeCrashContained) ||
                              input.HandoffResults.Any(r => r.Status == NodalOsFullOcrHandoffProbeStatus.NativeRuntimeCrashContained);

        return new NodalOsFullOcrRuntimeDecisionReport(
            $"full-ocr-runtime-decision-{Guid.NewGuid():N}",
            decision,
            ShadowModeBlocked: true,
            ProductiveOcrBlocked: true,
            DetectorIsolatedRunSucceeded: input.HandoffResults.Any(r => r.DetectorRunSucceeded),
            DetectorPostProcessingProducedBoxes: input.HandoffResults.Any(r => r.DetectorPostProcessingReached && r.BoxWidth > 0 && r.BoxHeight > 0),
            handoffFailure,
            recognizerCrash,
            recognizerSucceeded,
            input.DictionaryCompatibility.Status is NodalOsOcrDictionaryCompatibilityStatus.ClassCountMismatch,
            input.NoAuthority && input.HandoffResults.All(r => r.NoAuthority) && input.RecognizerResults.All(r => r.NoAuthority),
            BrowserCredentialRedactor.Redact(reason));
    }
}

public sealed class NodalOsRecognizerRuntimeExperimentBuilder
{
    public IReadOnlyList<NodalOsRecognizerRuntimeExperiment> BuildMinimalMatrix()
    {
        var tensors = new[]
        {
            NodalOsRecognizerRuntimeTensorKind.Zero,
            NodalOsRecognizerRuntimeTensorKind.Ones,
            NodalOsRecognizerRuntimeTensorKind.Gradient,
            NodalOsRecognizerRuntimeTensorKind.Checker,
            NodalOsRecognizerRuntimeTensorKind.SyntheticTextCrop,
            NodalOsRecognizerRuntimeTensorKind.HighContrastManualCrop,
            NodalOsRecognizerRuntimeTensorKind.DetectorDerivedCrop
        };

        var options = new[]
        {
            Option(NodalOsRecognizerRuntimeSessionOptionKind.Default),
            Option(NodalOsRecognizerRuntimeSessionOptionKind.GraphOptimizationDisabled),
            Option(NodalOsRecognizerRuntimeSessionOptionKind.GraphOptimizationBasic),
            Option(NodalOsRecognizerRuntimeSessionOptionKind.SingleThreaded),
            Option(NodalOsRecognizerRuntimeSessionOptionKind.MemoryPatternDisabled),
            Option(NodalOsRecognizerRuntimeSessionOptionKind.CpuArenaDisabled),
            Option(NodalOsRecognizerRuntimeSessionOptionKind.SequentialExecution),
            Option(NodalOsRecognizerRuntimeSessionOptionKind.DeterministicMinimal)
        };

        var experiments = new List<NodalOsRecognizerRuntimeExperiment>();
        foreach (var tensor in tensors)
        foreach (var option in options)
        {
            experiments.Add(Experiment(tensor, NodalOsRecognizerRuntimeProbeLayout.Nchw, NodalOsRecognizerRuntimeShapeKind.CurrentPipelineFixed, [1, 3, 32, 320], option));
        }

        experiments.Add(Experiment(NodalOsRecognizerRuntimeTensorKind.Zero, NodalOsRecognizerRuntimeProbeLayout.Nchw, NodalOsRecognizerRuntimeShapeKind.PaddleOcrCandidate640, [1, 3, 32, 640], Option(NodalOsRecognizerRuntimeSessionOptionKind.Default)));
        experiments.Add(Experiment(NodalOsRecognizerRuntimeTensorKind.Zero, NodalOsRecognizerRuntimeProbeLayout.Nhwc, NodalOsRecognizerRuntimeShapeKind.CurrentPipelineFixed, [1, 32, 320, 3], Option(NodalOsRecognizerRuntimeSessionOptionKind.Default), requiresGuard: false));
        experiments.Add(Experiment(NodalOsRecognizerRuntimeTensorKind.Zero, NodalOsRecognizerRuntimeProbeLayout.Nchw, NodalOsRecognizerRuntimeShapeKind.Invalid, [1, 1, 0, 0], Option(NodalOsRecognizerRuntimeSessionOptionKind.Default), requiresGuard: false));

        return experiments;
    }

    public static NodalOsRecognizerRuntimeSessionOptionsMetadata Option(NodalOsRecognizerRuntimeSessionOptionKind kind) =>
        kind switch
        {
            NodalOsRecognizerRuntimeSessionOptionKind.GraphOptimizationDisabled => new(kind, "graph optimization disabled", true, true, false, null, null, false, false, false, false),
            NodalOsRecognizerRuntimeSessionOptionKind.GraphOptimizationBasic => new(kind, "graph optimization basic", true, false, true, null, null, false, false, false, false),
            NodalOsRecognizerRuntimeSessionOptionKind.SingleThreaded => new(kind, "intra-op/inter-op threads = 1", true, false, false, 1, 1, false, false, false, false),
            NodalOsRecognizerRuntimeSessionOptionKind.MemoryPatternDisabled => new(kind, "memory pattern disabled", true, false, false, null, null, true, false, false, false),
            NodalOsRecognizerRuntimeSessionOptionKind.CpuArenaDisabled => new(kind, "CPU arena disabled", true, false, false, null, null, false, true, false, false),
            NodalOsRecognizerRuntimeSessionOptionKind.SequentialExecution => new(kind, "sequential execution", true, false, false, null, null, false, false, true, false),
            NodalOsRecognizerRuntimeSessionOptionKind.DeterministicMinimal => new(kind, "graph disabled, single-threaded, memory pattern/arena disabled, sequential", true, true, false, 1, 1, true, true, true, true),
            _ => new(kind, "default CPU session options", true, false, false, null, null, false, false, false, false)
        };

    private static NodalOsRecognizerRuntimeExperiment Experiment(
        NodalOsRecognizerRuntimeTensorKind tensor,
        NodalOsRecognizerRuntimeProbeLayout layout,
        NodalOsRecognizerRuntimeShapeKind shapeKind,
        int[] shape,
        NodalOsRecognizerRuntimeSessionOptionsMetadata option,
        bool requiresGuard = true) =>
        new(
            $"rec-runtime-{tensor}-{shapeKind}-{layout}-{option.OptionKind}-{Guid.NewGuid():N}",
            tensor,
            layout,
            shapeKind,
            shape,
            option,
            RequiresOutOfProcessGuard: requiresGuard,
            AllowInProcess: false,
            Synthetic: true,
            FullScreen: false,
            Sensitive: false,
            RawPersisted: false,
            NoAuthority: true);
}

public sealed class NodalOsRecognizerRuntimeCompatibilityDecisionService
{
    public NodalOsRecognizerModelCompatibilityFinding Decide(
        IReadOnlyList<NodalOsRecognizerRuntimeCompatibilityResult> results,
        bool recognizerModelVerified,
        bool parentSurvived,
        bool tempCleanup,
        bool noRawPersistence,
        bool noAuthority,
        NodalOsOcrDictionaryCompatibilityResult dictionary)
    {
        if (!recognizerModelVerified || !parentSurvived || !tempCleanup || !noRawPersistence || !noAuthority || results.Count == 0)
            return Finding(NodalOsRecognizerRuntimeCompatibilityDecision.NotReady, recognizerModelVerified, results, dictionary, "recognizer runtime gates not satisfied");

        var invalidShape = results.Any(r => r.Status == NodalOsRecognizerRuntimeProbeStatus.InvalidTensorShape);
        if (invalidShape && results.All(r => r.Status is NodalOsRecognizerRuntimeProbeStatus.InvalidTensorShape or NodalOsRecognizerRuntimeProbeStatus.UnsupportedLayout or NodalOsRecognizerRuntimeProbeStatus.Skipped))
            return Finding(NodalOsRecognizerRuntimeCompatibilityDecision.BlockedByRecognizerInputShape, recognizerModelVerified, results, dictionary, "recognizer input shape is invalid before runtime");

        var runResults = results.Where(r => r.Experiment.RequiresOutOfProcessGuard && r.RunAttempted).ToList();
        var successes = runResults.Where(r => r.Status is NodalOsRecognizerRuntimeProbeStatus.RunSucceeded or NodalOsRecognizerRuntimeProbeStatus.OutputMetadataCaptured or NodalOsRecognizerRuntimeProbeStatus.BlockedByDictionaryClassCountMismatch).ToList();
        var crashes = runResults.Where(r => r.Status == NodalOsRecognizerRuntimeProbeStatus.NativeRuntimeCrashContained).ToList();
        var defaultCrash = crashes.Any(r => r.Experiment.SessionOptions.OptionKind == NodalOsRecognizerRuntimeSessionOptionKind.Default);
        var optionSuccess = successes.Any(r => r.Experiment.SessionOptions.OptionKind != NodalOsRecognizerRuntimeSessionOptionKind.Default);

        if (optionSuccess && defaultCrash)
            return Finding(NodalOsRecognizerRuntimeCompatibilityDecision.ReadyForRecognizerSessionOptionsFix, recognizerModelVerified, results, dictionary, "a non-default recognizer session option avoided the crash");

        var coreTensors = new[]
        {
            NodalOsRecognizerRuntimeTensorKind.Zero,
            NodalOsRecognizerRuntimeTensorKind.Ones,
            NodalOsRecognizerRuntimeTensorKind.Gradient
        };
        var coreDefault = runResults.Where(r => coreTensors.Contains(r.Experiment.TensorKind) && r.Experiment.SessionOptions.OptionKind == NodalOsRecognizerRuntimeSessionOptionKind.Default).ToList();
        var allCoreCrashed = coreDefault.Count >= 3 && coreDefault.All(r => r.Status == NodalOsRecognizerRuntimeProbeStatus.NativeRuntimeCrashContained);
        if (allCoreCrashed)
            return Finding(NodalOsRecognizerRuntimeCompatibilityDecision.ReadyForOnnxRuntimeVersionExperiment, recognizerModelVerified, results, dictionary, "zero/ones/gradient crash in recognizer session.Run; isolate ONNX Runtime version before replacing model");

        var cropCrashes = crashes.Any(r => r.Experiment.TensorKind is NodalOsRecognizerRuntimeTensorKind.SyntheticTextCrop or NodalOsRecognizerRuntimeTensorKind.HighContrastManualCrop or NodalOsRecognizerRuntimeTensorKind.DetectorDerivedCrop);
        var safeSucceeded = successes.Any(r => r.Experiment.TensorKind is NodalOsRecognizerRuntimeTensorKind.Zero or NodalOsRecognizerRuntimeTensorKind.Ones or NodalOsRecognizerRuntimeTensorKind.Gradient);
        if (cropCrashes && safeSucceeded)
            return Finding(NodalOsRecognizerRuntimeCompatibilityDecision.ReadyForRecognizerPreprocessingFix, recognizerModelVerified, results, dictionary, "recognizer crash depends on crop/preprocessing tensor content");

        if (successes.Count > 0 && dictionary.Status is NodalOsOcrDictionaryCompatibilityStatus.ClassCountMismatch or NodalOsOcrDictionaryCompatibilityStatus.MissingDictionary or NodalOsOcrDictionaryCompatibilityStatus.DictionaryUnverified)
            return Finding(NodalOsRecognizerRuntimeCompatibilityDecision.ReadyForDictionaryCompletion, recognizerModelVerified, results, dictionary, "recognizer run works but dictionary/CTC gate blocks decode");

        if (crashes.Count > 0)
            return Finding(NodalOsRecognizerRuntimeCompatibilityDecision.BlockedByRecognizerModelRuntime, recognizerModelVerified, results, dictionary, "recognizer runtime remains blocked by native crash");

        return Finding(NodalOsRecognizerRuntimeCompatibilityDecision.ReadyForRecognizerModelReplacement, recognizerModelVerified, results, dictionary, "recognizer model compatibility remains inconclusive");
    }

    private static NodalOsRecognizerModelCompatibilityFinding Finding(
        NodalOsRecognizerRuntimeCompatibilityDecision decision,
        bool modelVerified,
        IReadOnlyList<NodalOsRecognizerRuntimeCompatibilityResult> results,
        NodalOsOcrDictionaryCompatibilityResult dictionary,
        string reason)
    {
        var runResults = results.Where(r => r.Experiment.RequiresOutOfProcessGuard && r.RunAttempted).ToList();
        var successes = runResults.Where(r => r.Status is NodalOsRecognizerRuntimeProbeStatus.RunSucceeded or NodalOsRecognizerRuntimeProbeStatus.OutputMetadataCaptured or NodalOsRecognizerRuntimeProbeStatus.BlockedByDictionaryClassCountMismatch).ToList();
        var crashes = runResults.Where(r => r.Status == NodalOsRecognizerRuntimeProbeStatus.NativeRuntimeCrashContained).ToList();
        var coreKinds = new[] { NodalOsRecognizerRuntimeTensorKind.Zero, NodalOsRecognizerRuntimeTensorKind.Ones, NodalOsRecognizerRuntimeTensorKind.Gradient };
        var coreDefault = runResults.Where(r => coreKinds.Contains(r.Experiment.TensorKind) && r.Experiment.SessionOptions.OptionKind == NodalOsRecognizerRuntimeSessionOptionKind.Default).ToList();

        return new NodalOsRecognizerModelCompatibilityFinding(
            $"rec-runtime-finding-{Guid.NewGuid():N}",
            decision,
            modelVerified,
            successes.Count > 0,
            successes.Any(r => r.Experiment.SessionOptions.OptionKind != NodalOsRecognizerRuntimeSessionOptionKind.Default) &&
                crashes.Any(r => r.Experiment.SessionOptions.OptionKind == NodalOsRecognizerRuntimeSessionOptionKind.Default),
            coreDefault.Count >= 3 && coreDefault.All(r => r.Status == NodalOsRecognizerRuntimeProbeStatus.NativeRuntimeCrashContained),
            crashes.Any(r => r.Experiment.TensorKind is NodalOsRecognizerRuntimeTensorKind.SyntheticTextCrop or NodalOsRecognizerRuntimeTensorKind.HighContrastManualCrop or NodalOsRecognizerRuntimeTensorKind.DetectorDerivedCrop) &&
                successes.Any(r => coreKinds.Contains(r.Experiment.TensorKind)),
            results.Any(r => r.Status == NodalOsRecognizerRuntimeProbeStatus.InvalidTensorShape),
            dictionary.Status is NodalOsOcrDictionaryCompatibilityStatus.ClassCountMismatch,
            ShadowModeBlocked: true,
            ProductiveOcrBlocked: true,
            NoAuthority: results.Count == 0 || results.All(r => r.NoAuthority),
            BrowserCredentialRedactor.Redact(reason));
    }
}

[Obsolete("Historical ONNX runtime version experiment. Active OCR path is pinned to ONNX Runtime 1.22.1.")]
public sealed class NodalOsOnnxRuntimeVersionExperimentPlanner
{
    public NodalOsOnnxRuntimeVersionExperimentPlan CreateDefaultPlan(string packageReferenceProject) =>
        new(
            BaselineVersion: "1.18.1",
            CandidateVersions: ["1.18.1", "1.22.1", "1.23.2", "1.25.0"],
            PackageReferenceProject: packageReferenceProject,
            Reversible: true,
            CpuProviderOnly: true,
            ProductiveOcrBlocked: true,
            NoAuthority: true);
}

[Obsolete("Historical ONNX runtime version experiment. Active OCR path is pinned to ONNX Runtime 1.22.1.")]
public sealed class NodalOsOnnxRuntimeVersionDecisionService
{
    public NodalOsOnnxRuntimeVersionDecisionReport Decide(
        IReadOnlyList<NodalOsOnnxRuntimeVersionExperimentResult> results,
        string baselineVersion,
        string finalPackageVersion,
        bool detectorSanityRequired,
        bool dictionaryMismatch,
        bool parentSurvived,
        bool noRawPersistence,
        bool noAuthority)
    {
        if (results.Count == 0 || !parentSurvived || !noRawPersistence || !noAuthority || !detectorSanityRequired)
        {
            return Report(
                NodalOsOnnxRuntimeVersionDecision.NotReady,
                baselineVersion,
                finalPackageVersion,
                results,
                detectorSanityRequired,
                dictionaryMismatch,
                "runtime version experiment gates not satisfied");
        }

        if (results.Any(r => !r.RestoreSucceeded || r.Status == NodalOsOnnxRuntimeVersionExperimentStatus.RuntimeVersionRestoreFailed))
        {
            return Report(
                NodalOsOnnxRuntimeVersionDecision.BlockedByRuntimeRestore,
                baselineVersion,
                finalPackageVersion,
                results,
                detectorSanityRequired,
                dictionaryMismatch,
                "one or more ONNX Runtime candidate versions failed restore");
        }

        if (results.Any(r => !r.BuildSucceeded || r.Status == NodalOsOnnxRuntimeVersionExperimentStatus.BuildFailed))
        {
            return Report(
                NodalOsOnnxRuntimeVersionDecision.BlockedByRuntimeRestore,
                baselineVersion,
                finalPackageVersion,
                results,
                detectorSanityRequired,
                dictionaryMismatch,
                "one or more ONNX Runtime candidate versions failed build");
        }

        var successful = results
            .Where(r => r.RestoreSucceeded && r.BuildSucceeded && r.DetectorSanitySucceeded && r.AnyRecognizerRunSucceeded)
            .ToList();

        if (successful.Count > 0 && dictionaryMismatch)
        {
            return Report(
                NodalOsOnnxRuntimeVersionDecision.ReadyForDictionaryCompletion,
                baselineVersion,
                finalPackageVersion,
                results,
                detectorSanityRequired,
                dictionaryMismatch,
                "a runtime version avoided recognizer crash; dictionary/CTC remains the next blocking gate");
        }

        if (successful.Count > 0)
        {
            return Report(
                NodalOsOnnxRuntimeVersionDecision.ReadyForOnnxRuntimeUpgrade,
                baselineVersion,
                finalPackageVersion,
                results,
                detectorSanityRequired,
                dictionaryMismatch,
                "a runtime version avoided recognizer crash and detector sanity stayed green");
        }

        var allVersionsCrashed = results.All(r =>
            r.RestoreSucceeded &&
            r.BuildSucceeded &&
            r.DetectorSanitySucceeded &&
            !r.AnyRecognizerRunSucceeded &&
            r.AnyRecognizerCrashContained);

        if (allVersionsCrashed)
        {
            return Report(
                NodalOsOnnxRuntimeVersionDecision.ReadyForRecognizerModelReplacement,
                baselineVersion,
                finalPackageVersion,
                results,
                detectorSanityRequired,
                dictionaryMismatch,
                "all tested ONNX Runtime versions preserved detector sanity but recognizer still crashed");
        }

        return Report(
            NodalOsOnnxRuntimeVersionDecision.BlockedByRecognizerModelRuntime,
            baselineVersion,
            finalPackageVersion,
            results,
            detectorSanityRequired,
            dictionaryMismatch,
            "recognizer runtime remains blocked without a clean runtime upgrade candidate");
    }

    private static NodalOsOnnxRuntimeVersionDecisionReport Report(
        NodalOsOnnxRuntimeVersionDecision decision,
        string baselineVersion,
        string finalPackageVersion,
        IReadOnlyList<NodalOsOnnxRuntimeVersionExperimentResult> results,
        bool detectorSanityRequired,
        bool dictionaryMismatch,
        string reason) =>
        new(
            $"onnx-runtime-version-decision-{Guid.NewGuid():N}",
            decision,
            baselineVersion,
            finalPackageVersion,
            BranchLeftAtBaseline: string.Equals(baselineVersion, finalPackageVersion, StringComparison.OrdinalIgnoreCase),
            AnyVersionAvoidedCrash: results.Any(r => r.AnyRecognizerRunSucceeded && r.DetectorSanitySucceeded),
            detectorSanityRequired,
            dictionaryMismatch,
            ShadowModeBlocked: true,
            ProductiveOcrBlocked: true,
            NoAuthority: results.Count == 0 || results.All(r => r.NoAuthority),
            BrowserCredentialRedactor.Redact(reason));
}
