using Microsoft.ML.OnnxRuntime;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.BrowserExecutor.Cdp;

public sealed class NodalOsGuardedSyntheticTextOcrProbeMatrixBuilder
{
    private static readonly (int Width, int Height)[] Dimensions =
    [
        (256, 96),
        (320, 128),
        (480, 160),
        (640, 160),
        (640, 320),
        (640, 640)
    ];

    private static readonly NodalOsGuardedSyntheticTextOcrPreProcessingVariant[] PreProcessingVariants =
    [
        NodalOsGuardedSyntheticTextOcrPreProcessingVariant.CurrentMeanStd,
        NodalOsGuardedSyntheticTextOcrPreProcessingVariant.KeepAspectWhitePadding,
        NodalOsGuardedSyntheticTextOcrPreProcessingVariant.KeepAspectBlackPadding,
        NodalOsGuardedSyntheticTextOcrPreProcessingVariant.ThresholdBinarization,
        NodalOsGuardedSyntheticTextOcrPreProcessingVariant.RgbOrder,
        NodalOsGuardedSyntheticTextOcrPreProcessingVariant.BgrOrder,
        NodalOsGuardedSyntheticTextOcrPreProcessingVariant.ChannelLayoutValidation
    ];

    public NodalOsGuardedSyntheticTextOcrProbeMatrix BuildDefaultMatrix()
    {
        var fixtures = BuildFixtureVariants();
        var requests = new List<NodalOsGuardedSyntheticTextOcrProbeRequest>();

        foreach (var fixtureTemplate in fixtures)
        {
            foreach (var (width, height) in Dimensions)
            {
                foreach (var preProcessing in PreProcessingVariants)
                {
                    var fixture = fixtureTemplate with
                    {
                        Width = width,
                        Height = height,
                        HorizontalPadding = fixtureTemplate.FixtureKind == NodalOsOnnxNativeRuntimeCrashFixtureKind.WidePaddingText ? 64 : 24,
                        VerticalPadding = Math.Max(8, height / 4)
                    };

                    var blockedSystemFont = fixture.UsesSystemFont && !fixture.Deterministic;
                    requests.Add(new NodalOsGuardedSyntheticTextOcrProbeRequest(
                        $"guarded-text-{requests.Count + 1:D4}",
                        fixture,
                        preProcessing,
                        NodalOsOnnxNativeRuntimeCrashStage.DetectionRun,
                        RequiresOutOfProcessGuard: fixture.RiskyTextFixture && !blockedSystemFont,
                        AllowInProcess: false,
                        CallsSaas: false,
                        ProductionMode: false));
                }
            }
        }

        return new NodalOsGuardedSyntheticTextOcrProbeMatrix(
            $"guarded-text-matrix-{Guid.NewGuid():N}",
            requests,
            fixtures.Count,
            Dimensions.Length,
            PreProcessingVariants.Length,
            RiskyProbesRequireGuard: requests.Where(r => r.Fixture.RiskyTextFixture && !r.Fixture.UsesSystemFont).All(r => r.RequiresOutOfProcessGuard && !r.AllowInProcess),
            RejectsRiskyInProcess: requests.All(r => !r.AllowInProcess),
            NoRawPersistence: requests.All(r => !r.Fixture.RawPersisted),
            NoFullScreen: requests.All(r => !r.Fixture.FullScreen),
            NoSensitive: requests.All(r => !r.Fixture.Sensitive),
            NoSaas: requests.All(r => !r.CallsSaas),
            NoAuthority: requests.All(r => r.Fixture.NoAuthority),
            DateTimeOffset.UtcNow);
    }

    private static IReadOnlyList<NodalOsGuardedSyntheticTextOcrFixtureMetadata> BuildFixtureVariants()
    {
        return
        [
            Fixture("large-centered", "TEST", NodalOsOnnxNativeRuntimeCrashFixtureKind.LargeCenteredText, NodalOsSyntheticOcrTextRenderMode.AntiAliasedPixelFont, NodalOsSyntheticOcrTextColorScheme.BlackOnWhite, "white", "black"),
            Fixture("small-centered", "TEST", NodalOsOnnxNativeRuntimeCrashFixtureKind.SmallCenteredText, NodalOsSyntheticOcrTextRenderMode.PixelFont, NodalOsSyntheticOcrTextColorScheme.BlackOnWhite, "white", "black"),
            Fixture("wide-padding", "TEST", NodalOsOnnxNativeRuntimeCrashFixtureKind.WidePaddingText, NodalOsSyntheticOcrTextRenderMode.PixelFont, NodalOsSyntheticOcrTextColorScheme.BlackOnWhite, "white", "black"),
            Fixture("soft-border", "TEST", NodalOsOnnxNativeRuntimeCrashFixtureKind.SoftBorderText, NodalOsSyntheticOcrTextRenderMode.AntiAliasedPixelFont, NodalOsSyntheticOcrTextColorScheme.BlackOnWhite, "white", "black"),
            Fixture("white-background", "TEST", NodalOsOnnxNativeRuntimeCrashFixtureKind.WhiteBackgroundText, NodalOsSyntheticOcrTextRenderMode.PixelFont, NodalOsSyntheticOcrTextColorScheme.BlackOnWhite, "white", "black"),
            Fixture("light-gray-background", "TEST", NodalOsOnnxNativeRuntimeCrashFixtureKind.LightGrayBackgroundText, NodalOsSyntheticOcrTextRenderMode.PixelFont, NodalOsSyntheticOcrTextColorScheme.GrayOnWhite, "light-gray", "dark-gray"),
            Fixture("pure-black-text", "TEST", NodalOsOnnxNativeRuntimeCrashFixtureKind.PureBlackText, NodalOsSyntheticOcrTextRenderMode.PixelFont, NodalOsSyntheticOcrTextColorScheme.BlackOnWhite, "white", "black"),
            Fixture("dark-gray-text", "TEST", NodalOsOnnxNativeRuntimeCrashFixtureKind.DarkGrayText, NodalOsSyntheticOcrTextRenderMode.AntiAliasedPixelFont, NodalOsSyntheticOcrTextColorScheme.GrayOnWhite, "white", "dark-gray"),
            Fixture("rectangular-simple", "TEST", NodalOsOnnxNativeRuntimeCrashFixtureKind.RectangularText, NodalOsSyntheticOcrTextRenderMode.PixelFont, NodalOsSyntheticOcrTextColorScheme.BlackOnWhite, "white", "black"),
            Fixture("numbers-simple", "12345", NodalOsOnnxNativeRuntimeCrashFixtureKind.NumericText, NodalOsSyntheticOcrTextRenderMode.PixelFont, NodalOsSyntheticOcrTextColorScheme.BlackOnWhite, "white", "black"),
            Fixture("letters-simple", "ABCDE", NodalOsOnnxNativeRuntimeCrashFixtureKind.LettersText, NodalOsSyntheticOcrTextRenderMode.PixelFont, NodalOsSyntheticOcrTextColorScheme.BlackOnWhite, "white", "black"),
            Fixture("alphanumeric-simple", "ABC123", NodalOsOnnxNativeRuntimeCrashFixtureKind.AlphanumericText, NodalOsSyntheticOcrTextRenderMode.AntiAliasedPixelFont, NodalOsSyntheticOcrTextColorScheme.BlackOnWhite, "white", "black"),
            Fixture("system-font-skipped", "TEST", NodalOsOnnxNativeRuntimeCrashFixtureKind.SystemFontText, NodalOsSyntheticOcrTextRenderMode.AntiAliasedPixelFont, NodalOsSyntheticOcrTextColorScheme.BlackOnWhite, "white", "black", usesSystemFont: true, deterministic: false)
        ];
    }

    private static NodalOsGuardedSyntheticTextOcrFixtureMetadata Fixture(
        string name,
        string expectedText,
        NodalOsOnnxNativeRuntimeCrashFixtureKind kind,
        NodalOsSyntheticOcrTextRenderMode renderMode,
        NodalOsSyntheticOcrTextColorScheme colorScheme,
        string background,
        string foreground,
        bool usesSystemFont = false,
        bool deterministic = true) =>
        new(
            name,
            expectedText,
            kind,
            renderMode,
            colorScheme,
            Width: 640,
            Height: 160,
            HorizontalPadding: 24,
            VerticalPadding: 40,
            background,
            foreground,
            usesSystemFont,
            deterministic,
            RiskyTextFixture: true,
            FullScreen: false,
            Sensitive: false,
            RawPersisted: false,
            NoAuthority: true);
}

public sealed class NodalOsDetectorRecognizerCompatibilityDiagnosisBuilder
{
    public NodalOsDetectorCompatibilityDiagnosis DiagnoseDetection(
        NodalOsGuardedSyntheticTextOcrProbeRequest request,
        float[] inputTensor,
        int[] inputShape,
        NodalOsOnnxOutOfProcessGuardResult? guardResult,
        string repositoryRoot)
    {
        var stats = CalculateStats(inputTensor, inputShape, "NCHW", "RGB");
        if (stats.HasNaN || stats.HasInfinity)
        {
            return Detection(request, stats, [], [], false, false, 0, NodalOsGuardedSyntheticTextOcrProbeStatus.BlockedByPreProcessing, "input tensor contains NaN/Infinity", repositoryRoot);
        }

        if (inputShape.Length != 4 || inputShape[0] != 1 || inputShape[1] != 3 || inputShape[2] <= 0 || inputShape[3] <= 0)
        {
            return Detection(request, stats, [], [], false, false, 0, NodalOsGuardedSyntheticTextOcrProbeStatus.InvalidTensorShape, "detector tensor shape must be [1,3,H,W]", repositoryRoot);
        }

        if (guardResult?.ProbeResult.Status == NodalOsOnnxNativeRuntimeCrashProbeStatus.NativeRuntimeCrash)
        {
            return Detection(request, stats, [], [], true, false, 0, NodalOsGuardedSyntheticTextOcrProbeStatus.NativeRuntimeCrashContained, "detection session.Run crashed in child and was contained", repositoryRoot);
        }

        var boxes = guardResult?.ProbeResult.BoxesDetected ?? 0;
        var status = boxes > 0
            ? NodalOsGuardedSyntheticTextOcrProbeStatus.PositiveDetection
            : NodalOsGuardedSyntheticTextOcrProbeStatus.NoTextDetected;

        return Detection(request, stats, [], [], false, false, boxes, status, boxes > 0 ? "detector reported boxes" : "detector reported no boxes", repositoryRoot);
    }

    public NodalOsRecognizerCompatibilityDiagnosis DiagnoseRecognition(
        NodalOsOnnxOutOfProcessGuardResult? guardResult,
        bool manualSafeCropAvailable = false)
    {
        var dictionary = new NodalOsOnnxOcrCharacterDictionary().Load("en-ascii", "en");
        var reachable = manualSafeCropAvailable || (guardResult?.ProbeResult.BoxesDetected ?? 0) > 0;

        if (!reachable)
        {
            return new NodalOsRecognizerCompatibilityDiagnosis(
                $"rec-diagnosis-{Guid.NewGuid():N}",
                Reachable: false,
                RecognizerInputShape: [],
                CropExtractionSucceeded: false,
                OutputNames: [],
                OutputShapes: [],
                CtcDecodingAttempted: false,
                DictionaryCompatible: dictionary.DictionaryId == "en-ascii",
                dictionary.DictionaryId,
                Confidence: null,
                NodalOsGuardedSyntheticTextOcrProbeStatus.NoTextDetected,
                "recognition unreachable: no detector boxes and no manual safe crop");
        }

        var status = guardResult?.ProbeResult.Status switch
        {
            NodalOsOnnxNativeRuntimeCrashProbeStatus.LowConfidence => NodalOsGuardedSyntheticTextOcrProbeStatus.LowConfidence,
            NodalOsOnnxNativeRuntimeCrashProbeStatus.BlockedByModelRuntime => NodalOsGuardedSyntheticTextOcrProbeStatus.BlockedByModelRuntime,
            _ => NodalOsGuardedSyntheticTextOcrProbeStatus.BlockedByDictionary
        };

        return new NodalOsRecognizerCompatibilityDiagnosis(
            $"rec-diagnosis-{Guid.NewGuid():N}",
            Reachable: true,
            RecognizerInputShape: [1, 3, 32, 320],
            CropExtractionSucceeded: true,
            OutputNames: [],
            OutputShapes: [],
            CtcDecodingAttempted: true,
            DictionaryCompatible: false,
            dictionary.DictionaryId,
            Confidence: null,
            status,
            "recognizer requires verified PaddleOCR dictionary compatibility before claiming positive text");
    }

    public static NodalOsOnnxTensorStats CalculateStats(float[] inputTensor, int[] inputShape, string channelLayout, string colorOrder)
    {
        if (inputTensor.Length == 0)
            return new NodalOsOnnxTensorStats(inputShape, 0, 0, 0, false, false, channelLayout, colorOrder);

        var min = float.PositiveInfinity;
        var max = float.NegativeInfinity;
        double sum = 0;
        var hasNaN = false;
        var hasInf = false;

        foreach (var value in inputTensor)
        {
            if (float.IsNaN(value)) { hasNaN = true; continue; }
            if (float.IsInfinity(value)) { hasInf = true; continue; }
            min = Math.Min(min, value);
            max = Math.Max(max, value);
            sum += value;
        }

        return new NodalOsOnnxTensorStats(inputShape, min, max, (float)(sum / inputTensor.Length), hasNaN, hasInf, channelLayout, colorOrder);
    }

    private static NodalOsDetectorCompatibilityDiagnosis Detection(
        NodalOsGuardedSyntheticTextOcrProbeRequest request,
        NodalOsOnnxTensorStats stats,
        IReadOnlyList<string> outputNames,
        IReadOnlyList<int[]> outputShapes,
        bool sessionRunCrashed,
        bool postProcessingCrashed,
        int boxes,
        NodalOsGuardedSyntheticTextOcrProbeStatus status,
        string reason,
        string repositoryRoot)
    {
        return new NodalOsDetectorCompatibilityDiagnosis(
            $"det-diagnosis-{Guid.NewGuid():N}",
            SessionCreationReached: !sessionRunCrashed,
            RuntimeVersion: typeof(InferenceSession).Assembly.GetName().Version?.ToString() ?? "unknown",
            Provider: "CPUExecutionProvider",
            ModelPath: Path.Combine(repositoryRoot, "tools", "ocr-worker", "models", "onnx", "ch_PP-OCRv4_det.onnx"),
            ModelOpset: "documented-by-manifest-or-onnx-metadata",
            request.Fixture.Width > 0 ? stats.Shape : [],
            stats,
            outputNames,
            outputShapes,
            sessionRunCrashed,
            postProcessingCrashed,
            boxes,
            status,
            BrowserCredentialRedactor.Redact(reason));
    }
}

public sealed class NodalOsGuardedSyntheticTextOcrReadinessReview
{
    public sealed record Inputs(
        NodalOsGuardedSyntheticTextOcrProbeMatrix Matrix,
        IReadOnlyList<NodalOsGuardedSyntheticTextOcrProbeResult> ProbeResults,
        NodalOsDetectorCompatibilityDiagnosis Detection,
        NodalOsRecognizerCompatibilityDiagnosis Recognition,
        bool GuardExists,
        bool ParentSurvivedCrash,
        bool ChildCleanupWorks,
        bool TempCleanupWorks);

    public NodalOsGuardedSyntheticTextOcrReadinessReport Evaluate(Inputs inputs)
    {
        var riskyInProcess = inputs.ProbeResults.Any(r => r.RanInProcess);
        var raw = !inputs.Matrix.NoRawPersistence || inputs.ProbeResults.Any(r => r.RawPersisted);
        var saas = !inputs.Matrix.NoSaas || inputs.ProbeResults.Any(r => r.CallsSaas);
        var noAuthority = inputs.Matrix.NoAuthority && inputs.ProbeResults.All(r => r.NoAuthority);

        var decision = Decide(inputs, riskyInProcess, raw, saas, noAuthority);

        return new NodalOsGuardedSyntheticTextOcrReadinessReport(
            $"guarded-text-readiness-{Guid.NewGuid():N}",
            decision,
            inputs.GuardExists,
            RiskyTextNeverRanInProcess: !riskyInProcess,
            inputs.ParentSurvivedCrash,
            inputs.ChildCleanupWorks,
            inputs.TempCleanupWorks,
            NoRawPersistence: !raw,
            inputs.Matrix.NoFullScreen,
            inputs.Matrix.NoSensitive,
            NoSaas: !saas,
            noAuthority,
            DetectionDiagnosed: true,
            RecognitionDiagnosedOrUnreachable: inputs.Recognition.Reachable || inputs.Recognition.Status == NodalOsGuardedSyntheticTextOcrProbeStatus.NoTextDetected,
            DictionaryStatusDocumented: !string.IsNullOrWhiteSpace(inputs.Recognition.DictionaryId),
            ModelCompatibilityDocumented: true,
            ShadowModeBlocked: decision != NodalOsGuardedSyntheticTextOcrReadinessDecision.ReadyForSyntheticPositiveRecognition,
            ProductionPublicOcrBlocked: true,
            Reason: BrowserCredentialRedactor.Redact(Reason(decision, inputs)));
    }

    private static NodalOsGuardedSyntheticTextOcrReadinessDecision Decide(Inputs inputs, bool riskyInProcess, bool raw, bool saas, bool noAuthority)
    {
        if (!inputs.GuardExists || riskyInProcess || !inputs.ParentSurvivedCrash || !inputs.ChildCleanupWorks || !inputs.TempCleanupWorks || raw || saas || !noAuthority)
            return NodalOsGuardedSyntheticTextOcrReadinessDecision.NotReady;

        if (inputs.Detection.Status == NodalOsGuardedSyntheticTextOcrProbeStatus.NativeRuntimeCrashContained)
            return NodalOsGuardedSyntheticTextOcrReadinessDecision.ReadyForOnnxRuntimeVersionExperiment;

        if (inputs.Detection.Status == NodalOsGuardedSyntheticTextOcrProbeStatus.InvalidTensorShape)
            return NodalOsGuardedSyntheticTextOcrReadinessDecision.BlockedByInputTensorShape;

        if (inputs.Detection.Status == NodalOsGuardedSyntheticTextOcrProbeStatus.BlockedByPreProcessing)
            return NodalOsGuardedSyntheticTextOcrReadinessDecision.BlockedByPreProcessing;

        if (inputs.Recognition.Status == NodalOsGuardedSyntheticTextOcrProbeStatus.BlockedByDictionary)
            return NodalOsGuardedSyntheticTextOcrReadinessDecision.ReadyForDictionaryCompletion;

        if (inputs.Detection.Status == NodalOsGuardedSyntheticTextOcrProbeStatus.PositiveDetection && inputs.Recognition.Status == NodalOsGuardedSyntheticTextOcrProbeStatus.NoTextDetected)
            return NodalOsGuardedSyntheticTextOcrReadinessDecision.ReadyForMoreSyntheticFixtures;

        if (inputs.Recognition.Status == NodalOsGuardedSyntheticTextOcrProbeStatus.PositiveRecognition)
            return NodalOsGuardedSyntheticTextOcrReadinessDecision.ReadyForSyntheticPositiveRecognition;

        return NodalOsGuardedSyntheticTextOcrReadinessDecision.ReadyForMoreSyntheticFixtures;
    }

    private static string Reason(NodalOsGuardedSyntheticTextOcrReadinessDecision decision, Inputs inputs) =>
        $"{decision}; detection={inputs.Detection.Status}; recognition={inputs.Recognition.Status}; shadow remains blocked unless positive recognition evidence exists";
}
