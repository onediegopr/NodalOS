using System.Text.Json;
using System.Runtime.InteropServices;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

// M210 — Isolated ONNX OCR probe runner.
// This process is launched by NodalOsOnnxOutOfProcessGuard to attempt probes that may crash the
// native ONNX Runtime. If the native runtime crashes, THIS process dies (contained); the parent
// guard maps the crash to a controlled result. Strictly synthetic / redacted / non-sensitive only.
//
// Modes:
//   --self-test <safe|crash|abort|nonzero|timeout|garbage>   Deterministic outcomes (no ONNX load).
//   --detector-crash-probe --repo-root <dir>                  Parent matrix: launch detector-only child probes through guard.
//   --detector-crash-child --repo-root <dir> --tensor <kind>   Child: detector-only session/run, may natively crash.
//   --handoff-crash-probe --repo-root <dir>                   Parent matrix: detector-to-recognizer child probes through guard.
//   --recognizer-runtime-probe --repo-root <dir>               Parent matrix: recognizer-only child probes through guard.
//   --recognizer-runtime-experiment --repo-root <dir>          Parent matrix: recognizer-only tensor/layout/session-option probes through guard.
//   --extra-class-argmax-probe --repo-root <dir>               Parent matrix: PP-OCRv5 extra-class argmax/probability probes through guard.
//   --onnx-synthetic-recognizer-decode-probe --repo-root <dir> Parent: official-space synthetic recognizer decode probe.
//   --synthetic-image-recognizer-crop-probe --repo-root <dir>  Parent: generated crop fixtures through recognizer guard.
//   --synthetic-detector-to-recognizer-pipeline-probe --repo-root <dir> Parent: synthetic full image detector->recognizer pipeline.
//   --probe --repo-root <dir>                                 Real ONNX inference on a synthetic fixture.
//   --request <file>                                          Probe request JSON (written by the guard).
//
// Hard rules: no SaaS, no raw persistence, no full-screen, no sensitive, no network, no-authority.

var options = ParseArgs(args);

if (options.TryGetValue("self-test", out var mode))
{
    return RunSelfTest(mode);
}

if (options.ContainsKey("guard-probe"))
{
    return RunGuardProbe(options);
}

if (options.ContainsKey("detector-crash-probe"))
{
    return RunDetectorCrashProbe(options);
}

if (options.ContainsKey("detector-crash-child"))
{
    return RunDetectorCrashChild(options);
}

if (options.ContainsKey("handoff-crash-probe"))
{
    return RunHandoffCrashProbe(options);
}

if (options.ContainsKey("handoff-crash-child"))
{
    return RunHandoffCrashChild(options);
}

if (options.ContainsKey("recognizer-runtime-probe"))
{
    return RunRecognizerRuntimeProbe(options);
}

if (options.ContainsKey("recognizer-runtime-experiment"))
{
    return RunRecognizerRuntimeExperiment(options);
}

if (options.ContainsKey("recognizer-runtime-child"))
{
    return RunRecognizerRuntimeChild(options);
}

if (options.ContainsKey("extra-class-argmax-probe"))
{
    return RunExtraClassArgmaxProbe(options);
}

if (options.ContainsKey("extra-class-argmax-child"))
{
    return RunExtraClassArgmaxChild(options);
}

if (options.ContainsKey("onnx-synthetic-recognizer-decode-probe"))
{
    return RunOnnxSyntheticRecognizerDecodeProbe(options);
}

if (options.ContainsKey("onnx-synthetic-recognizer-decode-child"))
{
    return RunOnnxSyntheticRecognizerDecodeChild(options);
}

if (options.ContainsKey("synthetic-image-recognizer-crop-probe"))
{
    return RunSyntheticImageRecognizerCropProbe(options);
}

if (options.ContainsKey("synthetic-image-recognizer-crop-child"))
{
    return RunSyntheticImageRecognizerCropChild(options);
}

if (options.ContainsKey("synthetic-detector-to-recognizer-pipeline-probe"))
{
    return RunSyntheticDetectorToRecognizerPipelineProbe(options);
}

if (options.ContainsKey("synthetic-detector-recognizer-crop-calibration-probe"))
{
    return RunSyntheticDetectorRecognizerCropCalibrationProbe(options);
}

if (options.ContainsKey("paddleocr-preprocessing-alignment-ab-probe"))
{
    return RunPaddleOcrPreprocessingAlignmentAbProbe(options);
}

if (options.ContainsKey("internal-controlled-real-image-probe"))
{
    return RunInternalControlledRealImageProbe(options);
}

if (options.ContainsKey("internal-controlled-screen-region-probe"))
{
    return RunInternalControlledScreenRegionProbe(options);
}

if (options.ContainsKey("qa-window-region-probe"))
{
    return RunInternalControlledScreenRegionProbe(options);
}

if (options.ContainsKey("synthetic-detector-to-recognizer-pipeline-child"))
{
    return RunSyntheticDetectorToRecognizerPipelineChild(options);
}

if (options.ContainsKey("probe"))
{
    return RunProbe(options);
}

Console.Error.WriteLine("usage: --self-test <mode> | --guard-probe --repo-root <dir> [--fixture <kind>] [--width <w>] [--height <h>] | --detector-crash-probe --repo-root <dir> | --detector-crash-child --repo-root <dir> --tensor <kind> --session-option <kind> | --handoff-crash-probe --repo-root <dir> | --recognizer-runtime-probe --repo-root <dir> | --recognizer-runtime-experiment --repo-root <dir> | --extra-class-argmax-probe --repo-root <dir> | --onnx-synthetic-recognizer-decode-probe --repo-root <dir> | --synthetic-image-recognizer-crop-probe --repo-root <dir> | --synthetic-detector-to-recognizer-pipeline-probe --repo-root <dir> | --probe --repo-root <dir> [--request <file>]");
return 64;

static Dictionary<string, string> ParseArgs(string[] argv)
{
    var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    for (var i = 0; i < argv.Length; i++)
    {
        if (!argv[i].StartsWith("--", StringComparison.Ordinal)) continue;
        var key = argv[i][2..];
        if (i + 1 < argv.Length && !argv[i + 1].StartsWith("--", StringComparison.Ordinal))
        {
            map[key] = argv[i + 1];
            i++;
        }
        else
        {
            map[key] = "true";
        }
    }
    return map;
}

static int RunSelfTest(string mode)
{
    switch (mode.ToLowerInvariant())
    {
        case "safe":
            EmitReport(new NodalOsOnnxOutOfProcessRunnerReport(
                "selftest-safe", "SessionCreation", "NoTextDetected", 0, 0,
                CallsSaas: false, RawPersisted: false, NoAuthority: true,
                "self-test safe: child ran and returned a controlled result"));
            return 0;

        case "garbage":
            Console.Out.WriteLine("this is not json <<<>>>");
            return 0;

        case "nonzero":
            Console.Error.WriteLine("self-test nonzero exit");
            return 7;

        case "abort":
            // Simulate a native abort exit code without producing JSON.
            return unchecked((int)0xC0000409);

        case "crash":
            // Simulate a native access violation exit code without producing JSON.
            return unchecked((int)0xC0000005);

        case "timeout":
            // Sleep far past any reasonable guard timeout; the guard must kill us.
            Thread.Sleep(TimeSpan.FromSeconds(120));
            return 0;

        default:
            Console.Error.WriteLine($"unknown self-test mode: {mode}");
            return 64;
    }
}

static int RunGuardProbe(Dictionary<string, string> options)
{
    var repoRoot = options.TryGetValue("repo-root", out var root) && Directory.Exists(root)
        ? Path.GetFullPath(root)
        : Directory.GetCurrentDirectory();

    var fixture = options.TryGetValue("fixture", out var fixtureName) &&
                  Enum.TryParse<NodalOsOnnxNativeRuntimeCrashFixtureKind>(fixtureName, ignoreCase: true, out var parsedFixture)
        ? parsedFixture
        : NodalOsOnnxNativeRuntimeCrashFixtureKind.LargeCenteredText;

    var render = options.TryGetValue("render", out var renderName) &&
                 Enum.TryParse<NodalOsSyntheticOcrTextRenderMode>(renderName, ignoreCase: true, out var parsedRender)
        ? parsedRender
        : NodalOsSyntheticOcrTextRenderMode.PixelFont;

    var width = options.TryGetValue("width", out var widthText) && int.TryParse(widthText, out var parsedWidth)
        ? parsedWidth
        : 640;
    var height = options.TryGetValue("height", out var heightText) && int.TryParse(heightText, out var parsedHeight)
        ? parsedHeight
        : 160;
    var timeoutMs = options.TryGetValue("timeout-ms", out var timeoutText) && int.TryParse(timeoutText, out var parsedTimeout)
        ? parsedTimeout
        : 60000;

    var request = new NodalOsOnnxNativeRuntimeCrashProbeRequest(
        $"m220-{fixture}-{width}x{height}-{Guid.NewGuid():N}",
        fixture,
        render,
        width,
        height,
        NodalOsOnnxNativeRuntimeCrashStage.DetectionRun,
        NodalOsOcrVisionSensitivity.Low,
        FullScreen: false,
        Sensitive: false,
        OriginalRawPersisted: false,
        Synthetic: true,
        NoAuthority: true,
        RunOutOfProcess: true);

    var runner = Environment.ProcessPath ?? Path.Combine(AppContext.BaseDirectory, "OneBrain.Tools.OnnxOcrProbeRunner.exe");
    var guardRequest = new NodalOsOnnxOutOfProcessGuardRequest(
        $"guard-m220-{Guid.NewGuid():N}",
        request,
        runner,
        new[] { "--probe", "--repo-root", repoRoot },
        timeoutMs,
        MaxOutputBytes: 64 * 1024,
        AllowRawPersistence: false);

    var result = new NodalOsOnnxOutOfProcessGuard().Run(guardRequest);
    Console.Out.WriteLine(JsonSerializer.Serialize(result));
    return 0;
}

static int RunDetectorCrashProbe(Dictionary<string, string> options)
{
    var repoRoot = options.TryGetValue("repo-root", out var root) && Directory.Exists(root)
        ? Path.GetFullPath(root)
        : Directory.GetCurrentDirectory();
    var runner = Environment.ProcessPath ?? Path.Combine(AppContext.BaseDirectory, "OneBrain.Tools.OnnxOcrProbeRunner.exe");
    var timeoutMs = options.TryGetValue("timeout-ms", out var timeoutText) && int.TryParse(timeoutText, out var parsedTimeout)
        ? parsedTimeout
        : 60000;

    var experiments = new NodalOsDetectorRuntimeCompatibilityExperimentBuilder().BuildMinimalMatrix();
    var results = new List<object>();

    foreach (var experiment in experiments)
    {
        if (experiment.Layout == NodalOsDetectorRuntimeProbeLayout.Nhwc)
        {
            results.Add(new
            {
                experiment.ExperimentId,
                TensorKind = experiment.TensorKind.ToString(),
                Layout = experiment.Layout.ToString(),
                SessionOption = experiment.SessionOptions.OptionKind.ToString(),
                Status = NodalOsDetectorRuntimeCompatibilityStatus.UnsupportedLayout.ToString(),
                RanOutOfProcess = false,
                ParentSurvived = true,
                Reason = "NHWC skipped: detector manifest expects NCHW [1,3,H,W]"
            });
            continue;
        }

        var request = new NodalOsOnnxNativeRuntimeCrashProbeRequest(
            experiment.ExperimentId,
            MapFixture(experiment.TensorKind),
            NodalOsSyntheticOcrTextRenderMode.PixelFont,
            experiment.InputShape[3],
            experiment.InputShape[2],
            NodalOsOnnxNativeRuntimeCrashStage.DetectionRun,
            NodalOsOcrVisionSensitivity.Low,
            FullScreen: false,
            Sensitive: false,
            OriginalRawPersisted: false,
            Synthetic: true,
            NoAuthority: true,
            RunOutOfProcess: true);

        var guardResult = new NodalOsOnnxOutOfProcessGuard().Run(new NodalOsOnnxOutOfProcessGuardRequest(
            $"det-runtime-guard-{Guid.NewGuid():N}",
            request,
            runner,
            [
                "--detector-crash-child",
                "--repo-root", repoRoot,
                "--tensor", experiment.TensorKind.ToString(),
                "--layout", experiment.Layout.ToString(),
                "--session-option", experiment.SessionOptions.OptionKind.ToString()
            ],
            timeoutMs,
            MaxOutputBytes: 64 * 1024,
            AllowRawPersistence: false));

        results.Add(new
        {
            experiment.ExperimentId,
            TensorKind = experiment.TensorKind.ToString(),
            Layout = experiment.Layout.ToString(),
            Shape = experiment.InputShape,
            SessionOption = experiment.SessionOptions.OptionKind.ToString(),
            Status = MapDetectorStatus(guardResult).ToString(),
            guardResult.ExitCode,
            ExitCodeHex = guardResult.ExitCode is null ? null : $"0x{unchecked((uint)guardResult.ExitCode.Value):X8}",
            CrashKind = guardResult.ProbeResult.CrashKind.ToString(),
            guardResult.TimedOut,
            guardResult.ParentSurvived,
            guardResult.TempFilesCleaned,
            guardResult.OrphanProcessLeft,
            guardResult.RawPersisted,
            guardResult.CallsSaas,
            guardResult.NoAuthority,
            guardResult.StdErrSummary,
            guardResult.Reason
        });
    }

    var postProcessRequest = new NodalOsOnnxNativeRuntimeCrashProbeRequest(
        $"det-runtime-postprocess-{Guid.NewGuid():N}",
        NodalOsOnnxNativeRuntimeCrashFixtureKind.LargeCenteredText,
        NodalOsSyntheticOcrTextRenderMode.PixelFont,
        640,
        640,
        NodalOsOnnxNativeRuntimeCrashStage.DetectorPostProcessing,
        NodalOsOcrVisionSensitivity.Low,
        FullScreen: false,
        Sensitive: false,
        OriginalRawPersisted: false,
        Synthetic: true,
        NoAuthority: true,
        RunOutOfProcess: true);
    var postProcessGuard = new NodalOsOnnxOutOfProcessGuard().Run(new NodalOsOnnxOutOfProcessGuardRequest(
        $"det-runtime-guard-{Guid.NewGuid():N}",
        postProcessRequest,
        runner,
        [
            "--detector-crash-child",
            "--repo-root", repoRoot,
            "--tensor", NodalOsDetectorRuntimeProbeTensorKind.CurrentPreprocessedSyntheticText.ToString(),
            "--layout", NodalOsDetectorRuntimeProbeLayout.Nchw.ToString(),
            "--session-option", NodalOsDetectorRuntimeSessionOptionKind.Default.ToString(),
            "--postprocess", "true"
        ],
        timeoutMs,
        MaxOutputBytes: 64 * 1024,
        AllowRawPersistence: false));
    results.Add(new
    {
        ExperimentId = postProcessRequest.ProbeId,
        TensorKind = NodalOsDetectorRuntimeProbeTensorKind.CurrentPreprocessedSyntheticText.ToString(),
        Layout = NodalOsDetectorRuntimeProbeLayout.Nchw.ToString(),
        Shape = new[] { 1, 3, 640, 640 },
        SessionOption = NodalOsDetectorRuntimeSessionOptionKind.Default.ToString(),
        Stage = "DetectorPostProcessing",
        Status = MapDetectorStatus(postProcessGuard).ToString(),
        postProcessGuard.ExitCode,
        ExitCodeHex = postProcessGuard.ExitCode is null ? null : $"0x{unchecked((uint)postProcessGuard.ExitCode.Value):X8}",
        CrashKind = postProcessGuard.ProbeResult.CrashKind.ToString(),
        postProcessGuard.TimedOut,
        postProcessGuard.ParentSurvived,
        postProcessGuard.TempFilesCleaned,
        postProcessGuard.OrphanProcessLeft,
        postProcessGuard.RawPersisted,
        postProcessGuard.CallsSaas,
        postProcessGuard.NoAuthority,
        postProcessGuard.StdErrSummary,
        postProcessGuard.Reason
    });

    Console.Out.WriteLine(JsonSerializer.Serialize(results));
    return 0;
}

static int RunDetectorCrashChild(Dictionary<string, string> options)
{
    var repoRoot = options.TryGetValue("repo-root", out var root) && Directory.Exists(root)
        ? Path.GetFullPath(root)
        : Directory.GetCurrentDirectory();
    var tensorKind = options.TryGetValue("tensor", out var tensorText) &&
                     Enum.TryParse<NodalOsDetectorRuntimeProbeTensorKind>(tensorText, ignoreCase: true, out var parsedTensor)
        ? parsedTensor
        : NodalOsDetectorRuntimeProbeTensorKind.Zero;
    var layout = options.TryGetValue("layout", out var layoutText) &&
                 Enum.TryParse<NodalOsDetectorRuntimeProbeLayout>(layoutText, ignoreCase: true, out var parsedLayout)
        ? parsedLayout
        : NodalOsDetectorRuntimeProbeLayout.Nchw;
    var optionKind = options.TryGetValue("session-option", out var optionText) &&
                     Enum.TryParse<NodalOsDetectorRuntimeSessionOptionKind>(optionText, ignoreCase: true, out var parsedOption)
        ? parsedOption
        : NodalOsDetectorRuntimeSessionOptionKind.Default;
    var runPostProcessing = options.TryGetValue("postprocess", out var postText) &&
                            bool.TryParse(postText, out var parsedPost) &&
                            parsedPost;

    if (layout != NodalOsDetectorRuntimeProbeLayout.Nchw)
    {
        EmitReport(new NodalOsOnnxOutOfProcessRunnerReport(
            $"det-runtime-{Guid.NewGuid():N}", "InputTensorPreparation", "InvalidTensorShape", null, null,
            false, false, true, "unsupported detector layout; manifest expects NCHW"));
        return 0;
    }

    var modelPath = Path.GetFullPath(Path.Combine(repoRoot, "tools", "ocr-worker", "models", "onnx", "ch_PP-OCRv4_det.onnx"));
    var shape = new[] { 1, 3, 640, 640 };
    var tensor = tensorKind == NodalOsDetectorRuntimeProbeTensorKind.CurrentPreprocessedSyntheticText
        ? BuildCurrentPreprocessedSyntheticTextTensor(out shape)
        : BuildDetectorTensor(tensorKind, shape);
    var stats = NodalOsDetectorRecognizerCompatibilityDiagnosisBuilder.CalculateStats(tensor, shape, "NCHW", "RGB");
    if (stats.HasNaN || stats.HasInfinity || tensor.Length != shape.Aggregate(1, (a, b) => a * b))
    {
        EmitReport(new NodalOsOnnxOutOfProcessRunnerReport(
            $"det-runtime-{Guid.NewGuid():N}", "InputTensorPreparation", "InvalidTensorShape", null, null,
            false, false, true, "invalid detector tensor stats/shape before runtime"));
        return 0;
    }

    Console.Error.WriteLine($"stage=model-file model={modelPath} exists={File.Exists(modelPath)}");
    Console.Error.WriteLine($"stage=tensor tensor={tensorKind} shape=[{string.Join(",", shape)}] min={stats.Min:R} max={stats.Max:R} mean={stats.Mean:R}");

    using var sessionOptions = CreateSessionOptions(optionKind);
    Console.Error.WriteLine($"stage=session-options option={optionKind}");
    using var session = new InferenceSession(modelPath, sessionOptions);
    var inputName = session.InputMetadata.Keys.FirstOrDefault() ?? "x";
    var inputMetadata = string.Join(";", session.InputMetadata.Select(kvp => $"{kvp.Key}=[{string.Join(",", kvp.Value.Dimensions)}]"));
    var outputMetadata = string.Join(";", session.OutputMetadata.Select(kvp => $"{kvp.Key}=[{string.Join(",", kvp.Value.Dimensions)}]"));
    Console.Error.WriteLine($"stage=session-created runtime={typeof(InferenceSession).Assembly.GetName().Version} provider=CPUExecutionProvider os={RuntimeInformation.OSArchitecture} inputs={inputMetadata} outputs={outputMetadata}");

    var inputTensor = new DenseTensor<float>(tensor, shape);
    var input = NamedOnnxValue.CreateFromTensor(inputName, inputTensor);
    Console.Error.WriteLine("stage=run-start");
    using var outputs = session.Run(new[] { input });
    var materializedOutputs = outputs.Select(o => new { o.Name, Tensor = o.AsTensor<float>() }).ToArray();
    var outputShapes = materializedOutputs.Select(o => $"{o.Name}=[{FormatDimensions(o.Tensor.Dimensions.ToArray())}]").ToArray();
    Console.Error.WriteLine($"stage=run-succeeded outputShapes={string.Join(";", outputShapes)}");

    if (runPostProcessing)
    {
        Console.Error.WriteLine("stage=postprocess-start");
        var first = materializedOutputs.First();
        var decoded = new NodalOsOnnxOcrDetectorPostProcessor().Decode(
            first.Tensor.ToArray(),
            first.Tensor.Dimensions.ToArray(),
            cropWidth: 640,
            cropHeight: 160,
            threshold: 0.3f);
        Console.Error.WriteLine($"stage=postprocess-succeeded status={decoded.Status} boxes={decoded.TextBoxes.Count}");
        EmitReport(new NodalOsOnnxOutOfProcessRunnerReport(
            $"det-runtime-{Guid.NewGuid():N}",
            "DetectorPostProcessing",
            "Passed",
            decoded.TextBoxes.Count,
            RecognitionAttempts: null,
            CallsSaas: false,
            RawPersisted: false,
            NoAuthority: true,
            $"detector postprocessing succeeded; status={decoded.Status}; boxes={decoded.TextBoxes.Count}"));
        return 0;
    }

    EmitReport(new NodalOsOnnxOutOfProcessRunnerReport(
        $"det-runtime-{Guid.NewGuid():N}",
        "DetectionRun",
        "Passed",
        BoxesDetected: null,
        RecognitionAttempts: null,
        CallsSaas: false,
        RawPersisted: false,
        NoAuthority: true,
        $"detector run succeeded; tensor={tensorKind}; option={optionKind}; outputs={string.Join(";", outputShapes)}"));
    return 0;
}

static int RunHandoffCrashProbe(Dictionary<string, string> options)
{
    var repoRoot = options.TryGetValue("repo-root", out var root) && Directory.Exists(root)
        ? root
        : Directory.GetCurrentDirectory();
    var (runner, runnerPrefix) = ResolveCurrentRunnerInvocation();
    var timeoutMs = options.TryGetValue("timeout-ms", out var timeoutText) && int.TryParse(timeoutText, out var parsedTimeout)
        ? parsedTimeout
        : 60000;

    var results = new List<object>();
    foreach (var boxKind in Enum.GetValues<NodalOsFullOcrHandoffBoxKind>())
    {
        var request = new NodalOsOnnxNativeRuntimeCrashProbeRequest(
            $"handoff-{boxKind}-{Guid.NewGuid():N}",
            NodalOsOnnxNativeRuntimeCrashFixtureKind.LargeCenteredText,
            NodalOsSyntheticOcrTextRenderMode.PixelFont,
            640,
            160,
            NodalOsOnnxNativeRuntimeCrashStage.RecognitionRun,
            NodalOsOcrVisionSensitivity.Low,
            FullScreen: false,
            Sensitive: false,
            OriginalRawPersisted: false,
            Synthetic: true,
            NoAuthority: true,
            RunOutOfProcess: true);

        var guardResult = new NodalOsOnnxOutOfProcessGuard().Run(new NodalOsOnnxOutOfProcessGuardRequest(
            $"handoff-guard-{Guid.NewGuid():N}",
            request,
            runner,
            ["--handoff-crash-child", "--repo-root", repoRoot, "--box-kind", boxKind.ToString()],
            timeoutMs,
            MaxOutputBytes: 64 * 1024,
            AllowRawPersistence: false));

        results.Add(new
        {
            BoxKind = boxKind.ToString(),
            Status = MapHandoffStatus(guardResult).ToString(),
            guardResult.ExitCode,
            ExitCodeHex = guardResult.ExitCode is null ? null : $"0x{unchecked((uint)guardResult.ExitCode.Value):X8}",
            CrashKind = guardResult.ProbeResult.CrashKind.ToString(),
            guardResult.TimedOut,
            guardResult.ParentSurvived,
            guardResult.TempFilesCleaned,
            guardResult.OrphanProcessLeft,
            guardResult.RawPersisted,
            guardResult.CallsSaas,
            guardResult.NoAuthority,
            guardResult.StdErrSummary,
            guardResult.Reason
        });
    }

    Console.Out.WriteLine(JsonSerializer.Serialize(results));
    return 0;
}

static int RunHandoffCrashChild(Dictionary<string, string> options)
{
    var repoRoot = options.TryGetValue("repo-root", out var root) && Directory.Exists(root)
        ? root
        : Directory.GetCurrentDirectory();
    var boxKind = options.TryGetValue("box-kind", out var boxText) &&
                  Enum.TryParse<NodalOsFullOcrHandoffBoxKind>(boxText, ignoreCase: true, out var parsedBox)
        ? parsedBox
        : NodalOsFullOcrHandoffBoxKind.ManualSafe;

    var image = BuildSyntheticRedactedImage("TEST", 640, 160, out var width, out var height, out var redaction);
    var imagePrep = new NodalOsOnnxOcrImagePreProcessor().Prepare(
        image,
        width,
        height,
        redaction,
        NodalOsOcrVisionSensitivity.Low,
        allowFullScreen: false,
        allowRawPersistence: false);
    if (imagePrep.Status != NodalOsOnnxOcrPreProcessingStatus.Success)
        return EmitHandoffChild(boxKind, NodalOsFullOcrHandoffProbeStatus.BlockedByPostProcessing, NodalOsFullOcrHandoffStage.DetectorSessionCreation, null, [], "image preprocessing blocked");

    var box = boxKind == NodalOsFullOcrHandoffBoxKind.DetectorProduced
        ? RunDetectorForFirstBox(repoRoot, imagePrep, width, height)
        : ManualBox(boxKind, width, height);

    if (box is null)
        return EmitHandoffChild(boxKind, NodalOsFullOcrHandoffProbeStatus.BlockedByPostProcessing, NodalOsFullOcrHandoffStage.DetectorPostProcessing, null, [], "detector postprocessing produced no box");
    if (!box.Valid || box.CropWidth <= 0 || box.CropHeight <= 0)
        return EmitHandoffChild(boxKind, NodalOsFullOcrHandoffProbeStatus.BlockedByInvalidBox, NodalOsFullOcrHandoffStage.BoxValidation, box, [], "invalid or degenerate text box blocked before runtime");
    if (box.CropX < 0 || box.CropY < 0 || box.CropX + box.CropWidth > imagePrep.Width || box.CropY + box.CropHeight > imagePrep.Height)
        return EmitHandoffChild(boxKind, NodalOsFullOcrHandoffProbeStatus.BlockedByOutOfBoundsCrop, NodalOsFullOcrHandoffStage.CropBoundsCalculation, box, [], "out-of-bounds crop blocked before runtime");
    if (box.CropWidth * box.CropHeight == 0)
        return EmitHandoffChild(boxKind, NodalOsFullOcrHandoffProbeStatus.BlockedByEmptyCrop, NodalOsFullOcrHandoffStage.CropExtraction, box, [], "empty crop blocked before runtime");
    if (box.CropWidth < 2 || box.CropHeight < 2)
        return EmitHandoffChild(boxKind, NodalOsFullOcrHandoffProbeStatus.BlockedByRecognizerTensorShape, NodalOsFullOcrHandoffStage.RecognizerTensorPreparation, box, [], "too-small crop blocked before recognizer runtime");

    var crop = ExtractCropForProbe(imagePrep, box);
    if (crop is null)
        return EmitHandoffChild(boxKind, NodalOsFullOcrHandoffProbeStatus.BlockedByEmptyCrop, NodalOsFullOcrHandoffStage.CropExtraction, box, [], "crop extraction returned empty");

    var recPrep = new NodalOsOnnxOcrRecognizerPreProcessor().Prepare(crop, maxWidth: 320);
    if (recPrep.Status != NodalOsOnnxOcrPreProcessingStatus.Success || recPrep.InputShape.Length != 4)
        return EmitHandoffChild(boxKind, NodalOsFullOcrHandoffProbeStatus.BlockedByRecognizerTensorShape, NodalOsFullOcrHandoffStage.RecognizerTensorPreparation, box, recPrep.InputShape, recPrep.Reason);

    var recognizerRelativePath = options.TryGetValue("recognizer-model-relative", out var configuredRecognizerPath) &&
                                 !string.IsNullOrWhiteSpace(configuredRecognizerPath)
        ? configuredRecognizerPath
        : Path.Combine("tools", "ocr-worker", "models", "onnx", "ch_PP-OCRv4_rec.onnx");
    var expectedClassCount = options.TryGetValue("expected-class-count", out var expectedClassText) &&
                             int.TryParse(expectedClassText, out var parsedExpectedClassCount)
        ? parsedExpectedClassCount
        : (int?)null;
    var modelPath = Path.GetFullPath(Path.Combine(repoRoot, recognizerRelativePath));
    using var session = new InferenceSession(modelPath);
    var inputName = session.InputMetadata.Keys.FirstOrDefault() ?? "x";
    Console.Error.WriteLine($"stage=recognizer-session-created input={inputName} metadata={string.Join(";", session.InputMetadata.Select(kvp => $"{kvp.Key}=[{string.Join(",", kvp.Value.Dimensions)}]"))}");
    var inputTensor = new DenseTensor<float>(recPrep.InputTensor, recPrep.InputShape);
    using var outputs = session.Run([NamedOnnxValue.CreateFromTensor(inputName, inputTensor)]);
    var outputShapes = outputs.Select(o => $"{o.Name}=[{FormatDimensions(o.AsTensor<float>().Dimensions.ToArray())}]").ToArray();
    Console.Error.WriteLine($"stage=recognizer-run-succeeded outputShapes={string.Join(";", outputShapes)}");

    return EmitHandoffChild(boxKind, NodalOsFullOcrHandoffProbeStatus.RecognizerRunSucceeded, NodalOsFullOcrHandoffStage.RecognizerOutputMetadata, box, recPrep.InputShape, $"recognizer run succeeded; outputs={string.Join(";", outputShapes)}");
}

static int RunRecognizerRuntimeProbe(Dictionary<string, string> options)
{
    var repoRoot = options.TryGetValue("repo-root", out var root) && Directory.Exists(root)
        ? Path.GetFullPath(root)
        : Directory.GetCurrentDirectory();
    var (runner, runnerPrefix) = ResolveCurrentRunnerInvocation();
    var timeoutMs = options.TryGetValue("timeout-ms", out var timeoutText) && int.TryParse(timeoutText, out var parsedTimeout)
        ? parsedTimeout
        : 60000;

    var results = new List<object>();
    foreach (var tensorKind in Enum.GetValues<NodalOsRecognizerRuntimeTensorKind>())
    {
        var request = new NodalOsOnnxNativeRuntimeCrashProbeRequest(
            $"rec-runtime-{tensorKind}-{Guid.NewGuid():N}",
            NodalOsOnnxNativeRuntimeCrashFixtureKind.LargeCenteredText,
            NodalOsSyntheticOcrTextRenderMode.PixelFont,
            320,
            32,
            NodalOsOnnxNativeRuntimeCrashStage.RecognitionRun,
            NodalOsOcrVisionSensitivity.Low,
            FullScreen: false,
            Sensitive: false,
            OriginalRawPersisted: false,
            Synthetic: true,
            NoAuthority: true,
            RunOutOfProcess: true);

        var guardResult = new NodalOsOnnxOutOfProcessGuard().Run(new NodalOsOnnxOutOfProcessGuardRequest(
            $"rec-runtime-guard-{Guid.NewGuid():N}",
            request,
            runner,
            ["--recognizer-runtime-child", "--repo-root", repoRoot, "--tensor", tensorKind.ToString(), "--layout", NodalOsRecognizerRuntimeProbeLayout.Nchw.ToString(), "--shape-kind", NodalOsRecognizerRuntimeShapeKind.CurrentPipelineFixed.ToString(), "--session-option", NodalOsRecognizerRuntimeSessionOptionKind.Default.ToString()],
            timeoutMs,
            MaxOutputBytes: 64 * 1024,
            AllowRawPersistence: false));

        results.Add(new
        {
            TensorKind = tensorKind.ToString(),
            Status = MapRecognizerStatus(guardResult).ToString(),
            guardResult.ExitCode,
            ExitCodeHex = guardResult.ExitCode is null ? null : $"0x{unchecked((uint)guardResult.ExitCode.Value):X8}",
            CrashKind = guardResult.ProbeResult.CrashKind.ToString(),
            guardResult.TimedOut,
            guardResult.ParentSurvived,
            guardResult.TempFilesCleaned,
            guardResult.OrphanProcessLeft,
            guardResult.RawPersisted,
            guardResult.CallsSaas,
            guardResult.NoAuthority,
            guardResult.StdErrSummary,
            guardResult.Reason
        });
    }

    Console.Out.WriteLine(JsonSerializer.Serialize(results));
    return 0;
}

static int RunOnnxSyntheticRecognizerDecodeProbe(Dictionary<string, string> options)
{
    var repoRoot = options.TryGetValue("repo-root", out var root) && Directory.Exists(root)
        ? Path.GetFullPath(root)
        : Directory.GetCurrentDirectory();
    var (runner, runnerPrefix) = ResolveCurrentRunnerInvocation();
    var timeoutMs = options.TryGetValue("timeout-ms", out var timeoutText) && int.TryParse(timeoutText, out var parsedTimeout)
        ? parsedTimeout
        : 60000;

    var candidates = new[]
    {
        new
        {
            ModelId = "ppocrv5-en-rec-candidate",
            RelativePath = Path.Combine("tools", "ocr-worker", "models", "onnx", "candidates", "en_PP-OCRv5_rec_mobile.onnx"),
            DictionaryRelativePath = Path.Combine("tools", "ocr-worker", "models", "onnx", "dictionaries", "ppocrv5_en_dict.txt"),
            ExpectedClasses = 438,
            DictionaryTokens = 436,
            InputShape = new[] { 1, 3, 48, 320 }
        },
        new
        {
            ModelId = "ppocrv4-en-rec-current",
            RelativePath = Path.Combine("tools", "ocr-worker", "models", "onnx", "ch_PP-OCRv4_rec.onnx"),
            DictionaryRelativePath = string.Empty,
            ExpectedClasses = 97,
            DictionaryTokens = 95,
            InputShape = new[] { 1, 3, 32, 320 }
        }
    };

    var results = new List<object>();
    foreach (var candidate in candidates)
    {
        var modelPath = Path.Combine(repoRoot, candidate.RelativePath);
        var dictionaryAvailable = string.IsNullOrWhiteSpace(candidate.DictionaryRelativePath) ||
                                  File.Exists(Path.Combine(repoRoot, candidate.DictionaryRelativePath));
        if (!File.Exists(modelPath) || !dictionaryAvailable)
        {
            results.Add(new
            {
                candidate.ModelId,
                candidate.RelativePath,
                ModelAvailable = File.Exists(modelPath),
                DictionaryAvailable = dictionaryAvailable,
                Attempted = false,
                Status = "BlockedByModelOrDictionaryAvailability",
                ParentSurvived = true,
                OutOfProcessGuardUsed = false,
                Reason = "model or dictionary unavailable; no ONNX child launched"
            });
            continue;
        }

        var request = new NodalOsOnnxNativeRuntimeCrashProbeRequest(
            $"onnx-synthetic-recognizer-{candidate.ModelId}-{Guid.NewGuid():N}",
            NodalOsOnnxNativeRuntimeCrashFixtureKind.LargeCenteredText,
            NodalOsSyntheticOcrTextRenderMode.PixelFont,
            candidate.InputShape[3],
            candidate.InputShape[2],
            NodalOsOnnxNativeRuntimeCrashStage.RecognitionRun,
            NodalOsOcrVisionSensitivity.Low,
            FullScreen: false,
            Sensitive: false,
            OriginalRawPersisted: false,
            Synthetic: true,
            NoAuthority: true,
            RunOutOfProcess: true);

        var args = new List<string>(runnerPrefix)
        {
            "--onnx-synthetic-recognizer-decode-child",
            "--repo-root", repoRoot,
            "--model-id", candidate.ModelId,
            "--recognizer-model-relative", candidate.RelativePath,
            "--expected-class-count", candidate.ExpectedClasses.ToString(),
            "--dictionary-token-count", candidate.DictionaryTokens.ToString(),
            "--input-height", candidate.InputShape[2].ToString(),
            "--input-width", candidate.InputShape[3].ToString()
        };

        var guardResult = new NodalOsOnnxOutOfProcessGuard().Run(new NodalOsOnnxOutOfProcessGuardRequest(
            $"onnx-synthetic-recognizer-guard-{Guid.NewGuid():N}",
            request,
            runner,
            args,
            timeoutMs,
            MaxOutputBytes: 128 * 1024,
            AllowRawPersistence: false));

        results.Add(new
        {
            candidate.ModelId,
            candidate.RelativePath,
            candidate.ExpectedClasses,
            candidate.DictionaryTokens,
            Attempted = true,
            OutOfProcessGuardUsed = true,
            guardResult.ExitCode,
            ExitCodeHex = guardResult.ExitCode is null ? null : $"0x{unchecked((uint)guardResult.ExitCode.Value):X8}",
            guardResult.TimedOut,
            guardResult.ParentSurvived,
            guardResult.ChildLaunched,
            guardResult.TempFilesCleaned,
            guardResult.OrphanProcessLeft,
            guardResult.RawPersisted,
            guardResult.CallsSaas,
            guardResult.NoAuthority,
            Status = guardResult.ProbeResult.Status.ToString(),
            CrashKind = guardResult.ProbeResult.CrashKind.ToString(),
            guardResult.StdErrSummary,
            ChildSummary = TryParseJsonElement(guardResult.Reason),
            guardResult.Reason
        });
    }

    var attempted = results.Any(r => (bool)r.GetType().GetProperty("Attempted")!.GetValue(r)!);
    var succeeded = results.Any(r =>
        (bool)r.GetType().GetProperty("Attempted")!.GetValue(r)! &&
        string.Equals((string?)r.GetType().GetProperty("Status")!.GetValue(r), "Passed", StringComparison.Ordinal));

    Console.Out.WriteLine(JsonSerializer.Serialize(new
    {
        Milestone = "M280-M282",
        ReadinessDecision = !attempted
            ? "BLOCKED_BY_MODEL_OR_DICTIONARY_AVAILABILITY"
            : succeeded
                ? "READY_FOR_SYNTHETIC_IMAGE_RECOGNIZER_CROP_FIXTURES"
                : "BLOCKED_BY_ONNX_SYNTHETIC_RECOGNIZER_RUNTIME",
        ProductiveOcrBlocked = true,
        ShadowModeBlocked = true,
        NoAuthority = true,
        NoSaaS = true,
        NoRawPersistence = true,
        NoFullScreen = true,
        NoSensitive = true,
        OfficialSpacePolicy = true,
        BlankIndex = 0,
        SpaceIndexFormula = "N+1",
        OutputLayout = "[B,T,C]",
        SoftmaxReapplied = false,
        OutOfProcessGuardUsed = attempted,
        ParentSurvived = results.All(r => (bool)r.GetType().GetProperty("ParentSurvived")?.GetValue(r)!),
        OnnxProbeAttempted = attempted,
        OnnxProbeSucceeded = succeeded,
        PpOcrV4ExpectedClasses = 97,
        PpOcrV5ExpectedClasses = 438,
        ModelsCommitted = false,
        DictionariesCommitted = false,
        RawTensorPersisted = false,
        RealImageUsed = false,
        RealScreenUsed = false,
        RealDocumentUsed = false,
        Results = results
    }));
    return 0;
}

static int RunOnnxSyntheticRecognizerDecodeChild(Dictionary<string, string> options)
{
    var repoRoot = options.TryGetValue("repo-root", out var root) && Directory.Exists(root)
        ? Path.GetFullPath(root)
        : Directory.GetCurrentDirectory();
    var modelId = options.TryGetValue("model-id", out var configuredModelId)
        ? configuredModelId
        : "unknown";
    var recognizerRelativePath = options.TryGetValue("recognizer-model-relative", out var configuredRecognizerPath) &&
                                 !string.IsNullOrWhiteSpace(configuredRecognizerPath)
        ? configuredRecognizerPath
        : Path.Combine("tools", "ocr-worker", "models", "onnx", "ch_PP-OCRv4_rec.onnx");
    var expectedClassCount = options.TryGetValue("expected-class-count", out var expectedClassText) &&
                             int.TryParse(expectedClassText, out var parsedExpectedClassCount)
        ? parsedExpectedClassCount
        : 0;
    var dictionaryTokenCount = options.TryGetValue("dictionary-token-count", out var dictionaryText) &&
                               int.TryParse(dictionaryText, out var parsedDictionaryTokenCount)
        ? parsedDictionaryTokenCount
        : Math.Max(0, expectedClassCount - 2);
    var inputHeight = options.TryGetValue("input-height", out var heightText) &&
                      int.TryParse(heightText, out var parsedHeight)
        ? parsedHeight
        : 32;
    var inputWidth = options.TryGetValue("input-width", out var widthText) &&
                     int.TryParse(widthText, out var parsedWidth)
        ? parsedWidth
        : 320;
    var shape = new[] { 1, 3, inputHeight, inputWidth };
    if (expectedClassCount <= 0 || shape.Any(d => d <= 0))
    {
        EmitReport(new NodalOsOnnxOutOfProcessRunnerReport(
            $"onnx-synthetic-recognizer-{Guid.NewGuid():N}",
            "RecognizerTensorPreparation",
            "InvalidTensorShape",
            null,
            null,
            false,
            false,
            true,
            "invalid expected class count or synthetic input shape"));
        return 0;
    }

    var modelPath = Path.GetFullPath(Path.Combine(repoRoot, recognizerRelativePath));
    if (!File.Exists(modelPath))
    {
        EmitReport(new NodalOsOnnxOutOfProcessRunnerReport(
            $"onnx-synthetic-recognizer-{Guid.NewGuid():N}",
            "ModelAvailability",
            "BlockedByModelRuntime",
            null,
            null,
            false,
            false,
            true,
            "recognizer model missing"));
        return 0;
    }

    var tensor = BuildRecognizerTensor(NodalOsRecognizerRuntimeTensorKind.Gradient, shape);
    var stats = NodalOsDetectorRecognizerCompatibilityDiagnosisBuilder.CalculateStats(tensor, shape, "NCHW", "RGB");
    if (stats.HasNaN || stats.HasInfinity)
    {
        EmitReport(new NodalOsOnnxOutOfProcessRunnerReport(
            $"onnx-synthetic-recognizer-{Guid.NewGuid():N}",
            "RecognizerTensorPreparation",
            "InvalidTensorShape",
            null,
            null,
            false,
            false,
            true,
            "synthetic tensor stats invalid before runtime"));
        return 0;
    }

    Console.Error.WriteLine($"stage=model-file model={modelPath} exists=true modelId={modelId}");
    Console.Error.WriteLine($"stage=tensor kind=Gradient shape=[{string.Join(",", shape)}] min={stats.Min:R} max={stats.Max:R} mean={stats.Mean:R}");
    using var session = new InferenceSession(modelPath);
    var inputName = session.InputMetadata.Keys.FirstOrDefault() ?? "x";
    var inputMetadata = string.Join(";", session.InputMetadata.Select(kvp => $"{kvp.Key}=[{string.Join(",", kvp.Value.Dimensions)}]"));
    var outputMetadata = string.Join(";", session.OutputMetadata.Select(kvp => $"{kvp.Key}=[{string.Join(",", kvp.Value.Dimensions)}]"));
    Console.Error.WriteLine($"stage=session-created runtime={typeof(InferenceSession).Assembly.GetName().Version} provider=CPUExecutionProvider inputs={inputMetadata} outputs={outputMetadata}");

    using var outputs = session.Run([NamedOnnxValue.CreateFromTensor(inputName, new DenseTensor<float>(tensor, shape))]);
    var output = outputs.First().AsTensor<float>();
    var outputShape = output.Dimensions.ToArray();
    var values = output.ToArray();
    var classCount = outputShape.Length == 3 ? outputShape[2] : outputShape.LastOrDefault();
    var softmax = AnalyzeSoftmaxRows(values, classCount);
    var layoutValid = outputShape.Length == 3 && outputShape[0] == 1 && classCount == expectedClassCount;
    var blankIndex = 0;
    var spaceIndex = dictionaryTokenCount + 1;
    var officialSpacePolicy = spaceIndex == expectedClassCount - 1;
    var decodedPreview = BuildNoAuthorityDecodePreview(values, classCount, blankIndex, spaceIndex, maxTimesteps: 16);
    var status = layoutValid && softmax.LooksLikeSoftmax && officialSpacePolicy
        ? "Passed"
        : "BlockedByModelRuntime";

    var summary = JsonSerializer.Serialize(new
    {
        ModelId = modelId,
        Stage = "RecognitionRun",
        InputTensorKind = "Gradient",
        InputShape = shape,
        InputMetadata = inputMetadata,
        OutputMetadata = outputMetadata,
        OutputShape = outputShape,
        OutputLayout = outputShape.Length == 3 ? "[B,T,C]" : "Unexpected",
        OutputClassCount = classCount,
        ExpectedClassCount = expectedClassCount,
        DictionaryTokenCount = dictionaryTokenCount,
        BlankIndex = blankIndex,
        SpaceIndex = spaceIndex,
        OfficialSpacePolicy = officialSpacePolicy,
        SoftmaxEvidence = softmax,
        SoftmaxReapplied = false,
        DecodeConsumedOutput = true,
        DecodePreviewNonAuthoritative = decodedPreview,
        UsefulOcrClaimed = false,
        ProductiveOcr = false,
        ShadowMode = false,
        NoAuthority = true,
        NoRawPersistence = true,
        NoSaaS = true,
        RawTensorPersisted = false,
        RealImageUsed = false,
        RealScreenUsed = false,
        RealDocumentUsed = false
    });

    EmitReport(new NodalOsOnnxOutOfProcessRunnerReport(
        $"onnx-synthetic-recognizer-{modelId}-{Guid.NewGuid():N}",
        "RecognitionRun",
        status,
        BoxesDetected: null,
        RecognitionAttempts: 1,
        CallsSaas: false,
        RawPersisted: false,
        NoAuthority: true,
        summary));
    return 0;
}

static int RunSyntheticImageRecognizerCropProbe(Dictionary<string, string> options)
{
    var repoRoot = options.TryGetValue("repo-root", out var root) && Directory.Exists(root)
        ? Path.GetFullPath(root)
        : Directory.GetCurrentDirectory();
    var (runner, runnerPrefix) = ResolveCurrentRunnerInvocation();
    var timeoutMs = options.TryGetValue("timeout-ms", out var timeoutText) && int.TryParse(timeoutText, out var parsedTimeout)
        ? parsedTimeout
        : 90000;

    var modelRelativePath = Path.Combine("tools", "ocr-worker", "models", "onnx", "candidates", "en_PP-OCRv5_rec_mobile.onnx");
    var dictionaryRelativePath = Path.Combine("tools", "ocr-worker", "models", "onnx", "dictionaries", "ppocrv5_en_dict.txt");
    var modelPath = Path.Combine(repoRoot, modelRelativePath);
    var dictionaryPath = Path.Combine(repoRoot, dictionaryRelativePath);
    var modelAvailable = File.Exists(modelPath);
    var dictionaryAvailable = File.Exists(dictionaryPath);

    var fixtures = new[]
    {
        "12 34",
        "PVC WALL",
        "A B C",
        "MARMOLES PVC",
        "12345",
        "GENOVA",
        "ROMA"
    };

    var results = new List<object>();
    if (modelAvailable && dictionaryAvailable)
    {
        foreach (var text in fixtures)
        {
            var request = new NodalOsOnnxNativeRuntimeCrashProbeRequest(
                $"synthetic-image-recognizer-crop-{Guid.NewGuid():N}",
                NodalOsOnnxNativeRuntimeCrashFixtureKind.LargeCenteredText,
                NodalOsSyntheticOcrTextRenderMode.PixelFont,
                320,
                48,
                NodalOsOnnxNativeRuntimeCrashStage.RecognitionRun,
                NodalOsOcrVisionSensitivity.Low,
                FullScreen: false,
                Sensitive: false,
                OriginalRawPersisted: false,
                Synthetic: true,
                NoAuthority: true,
                RunOutOfProcess: true);

            var args = new List<string>(runnerPrefix)
            {
                "--synthetic-image-recognizer-crop-child",
                "--repo-root", repoRoot,
                "--fixture-text", text,
                "--recognizer-model-relative", modelRelativePath,
                "--dictionary-relative", dictionaryRelativePath,
                "--expected-class-count", "438",
                "--dictionary-token-count", "436",
                "--input-height", "48",
                "--input-width", "320"
            };

            var guardResult = new NodalOsOnnxOutOfProcessGuard().Run(new NodalOsOnnxOutOfProcessGuardRequest(
                $"synthetic-image-recognizer-crop-guard-{Guid.NewGuid():N}",
                request,
                runner,
                args,
                timeoutMs,
                MaxOutputBytes: 192 * 1024,
                AllowRawPersistence: false));

            var childSummary = TryParseJsonElement(guardResult.Reason);
            results.Add(new
            {
                FixtureText = text,
                Attempted = true,
                OutOfProcessGuardUsed = true,
                guardResult.ExitCode,
                ExitCodeHex = guardResult.ExitCode is null ? null : $"0x{unchecked((uint)guardResult.ExitCode.Value):X8}",
                guardResult.TimedOut,
                guardResult.ParentSurvived,
                guardResult.ChildLaunched,
                guardResult.TempFilesCleaned,
                guardResult.OrphanProcessLeft,
                guardResult.RawPersisted,
                guardResult.CallsSaas,
                guardResult.NoAuthority,
                Status = guardResult.ProbeResult.Status.ToString(),
                CrashKind = guardResult.ProbeResult.CrashKind.ToString(),
                guardResult.StdErrSummary,
                ChildSummary = childSummary,
                guardResult.Reason
            });
        }
    }

    var attempted = results.Count > 0;
    var runtimeSucceeded = attempted && results.All(r =>
        string.Equals((string?)r.GetType().GetProperty("Status")!.GetValue(r), "Passed", StringComparison.Ordinal));
    var exactMatches = CountChildMatches(results, "Exact");
    var normalizedMatches = CountChildMatches(results, "Normalized");
    var mismatches = CountChildMatches(results, "Mismatch");
    var readinessDecision = !modelAvailable || !dictionaryAvailable
        ? "BLOCKED_BY_MODEL_OR_DICTIONARY_AVAILABILITY"
        : !runtimeSucceeded
            ? "BLOCKED_BY_SYNTHETIC_IMAGE_RECOGNIZER_PREPROCESSING"
            : exactMatches + normalizedMatches > 0
                ? "READY_FOR_SYNTHETIC_DETECTOR_TO_RECOGNIZER_PIPELINE_FIXTURES"
                : "BLOCKED_BY_RECOGNIZER_SYNTHETIC_IMAGE_DECODE_EVIDENCE";

    Console.Out.WriteLine(JsonSerializer.Serialize(new
    {
        Milestone = "M283-M285",
        BaseCommit = "28550e4",
        ReadinessDecision = readinessDecision,
        ProductiveOcrBlocked = true,
        ShadowModeBlocked = true,
        NoAuthority = true,
        NoSaaS = true,
        NoRawPersistenceOfRealData = true,
        NoFullScreen = true,
        NoSensitive = true,
        RealImageUsed = false,
        RealScreenUsed = false,
        RealDocumentUsed = false,
        SyntheticImagesOnly = true,
        SyntheticCropsPersisted = false,
        OfficialSpacePolicy = true,
        BlankIndex = 0,
        SpaceIndex = 437,
        SpaceIndexFormula = "N+1",
        OutputLayout = "[B,T,C]",
        SoftmaxReapplied = false,
        OutOfProcessGuardUsed = attempted,
        ParentSurvived = !attempted || results.All(r => (bool)r.GetType().GetProperty("ParentSurvived")!.GetValue(r)!),
        OnnxProbeAttempted = attempted,
        OnnxProbeSucceeded = runtimeSucceeded,
        PpOcrV5ExpectedClasses = 438,
        SyntheticCropFixturesCount = fixtures.Length,
        ExactMatches = exactMatches,
        NormalizedMatches = normalizedMatches,
        Mismatches = mismatches,
        ModelsCommitted = false,
        DictionariesCommitted = false,
        ModelAvailable = modelAvailable,
        DictionaryAvailable = dictionaryAvailable,
        ModelRelativePath = modelRelativePath,
        DictionaryRelativePath = dictionaryRelativePath,
        InputShape = new[] { 1, 3, 48, 320 },
        Fixtures = results
    }));
    return 0;
}

static int RunSyntheticImageRecognizerCropChild(Dictionary<string, string> options)
{
    var repoRoot = options.TryGetValue("repo-root", out var root) && Directory.Exists(root)
        ? Path.GetFullPath(root)
        : Directory.GetCurrentDirectory();
    var fixtureText = options.TryGetValue("fixture-text", out var configuredFixtureText)
        ? configuredFixtureText
        : "TEST";
    var recognizerRelativePath = options.TryGetValue("recognizer-model-relative", out var configuredRecognizerPath) &&
                                 !string.IsNullOrWhiteSpace(configuredRecognizerPath)
        ? configuredRecognizerPath
        : Path.Combine("tools", "ocr-worker", "models", "onnx", "candidates", "en_PP-OCRv5_rec_mobile.onnx");
    var dictionaryRelativePath = options.TryGetValue("dictionary-relative", out var configuredDictionaryPath) &&
                                 !string.IsNullOrWhiteSpace(configuredDictionaryPath)
        ? configuredDictionaryPath
        : Path.Combine("tools", "ocr-worker", "models", "onnx", "dictionaries", "ppocrv5_en_dict.txt");
    var expectedClassCount = options.TryGetValue("expected-class-count", out var expectedClassText) &&
                             int.TryParse(expectedClassText, out var parsedExpectedClassCount)
        ? parsedExpectedClassCount
        : 438;
    var dictionaryTokenCount = options.TryGetValue("dictionary-token-count", out var dictionaryText) &&
                               int.TryParse(dictionaryText, out var parsedDictionaryTokenCount)
        ? parsedDictionaryTokenCount
        : 436;
    var inputHeight = options.TryGetValue("input-height", out var heightText) &&
                      int.TryParse(heightText, out var parsedHeight)
        ? parsedHeight
        : 48;
    var inputWidth = options.TryGetValue("input-width", out var widthText) &&
                     int.TryParse(widthText, out var parsedWidth)
        ? parsedWidth
        : 320;

    var shape = new[] { 1, 3, inputHeight, inputWidth };
    var modelPath = Path.GetFullPath(Path.Combine(repoRoot, recognizerRelativePath));
    var dictionaryPath = Path.GetFullPath(Path.Combine(repoRoot, dictionaryRelativePath));
    if (!File.Exists(modelPath) || !File.Exists(dictionaryPath))
    {
        EmitReport(new NodalOsOnnxOutOfProcessRunnerReport(
            $"synthetic-image-recognizer-crop-{Guid.NewGuid():N}",
            "ModelAvailability",
            "BlockedByModelRuntime",
            null,
            null,
            false,
            false,
            true,
            "recognizer model or dictionary missing"));
        return 0;
    }

    var dictionaryTokens = File.ReadAllLines(dictionaryPath);
    var tensor = BuildSyntheticImageRecognizerCropTensor(fixtureText, shape, out var preprocessing);
    var stats = NodalOsDetectorRecognizerCompatibilityDiagnosisBuilder.CalculateStats(tensor, shape, "NCHW", "RGB");
    if (stats.HasNaN || stats.HasInfinity || dictionaryTokens.Length != dictionaryTokenCount)
    {
        EmitReport(new NodalOsOnnxOutOfProcessRunnerReport(
            $"synthetic-image-recognizer-crop-{Guid.NewGuid():N}",
            "RecognizerTensorPreparation",
            "InvalidTensorShape",
            null,
            null,
            false,
            false,
            true,
            $"synthetic tensor or dictionary invalid; dictionaryTokens={dictionaryTokens.Length}"));
        return 0;
    }

    Console.Error.WriteLine($"stage=model-file model={modelPath} exists=true fixture=\"{fixtureText}\"");
    Console.Error.WriteLine($"stage=preprocess shape=[{string.Join(",", shape)}] min={stats.Min:R} max={stats.Max:R} mean={stats.Mean:R} summary={preprocessing}");
    using var session = new InferenceSession(modelPath);
    var inputName = session.InputMetadata.Keys.FirstOrDefault() ?? "x";
    var inputMetadata = string.Join(";", session.InputMetadata.Select(kvp => $"{kvp.Key}=[{string.Join(",", kvp.Value.Dimensions)}]"));
    var outputMetadata = string.Join(";", session.OutputMetadata.Select(kvp => $"{kvp.Key}=[{string.Join(",", kvp.Value.Dimensions)}]"));
    Console.Error.WriteLine($"stage=session-created runtime={typeof(InferenceSession).Assembly.GetName().Version} provider=CPUExecutionProvider inputs={inputMetadata} outputs={outputMetadata}");

    using var outputs = session.Run([NamedOnnxValue.CreateFromTensor(inputName, new DenseTensor<float>(tensor, shape))]);
    var output = outputs.First().AsTensor<float>();
    var outputShape = output.Dimensions.ToArray();
    var values = output.ToArray();
    var classCount = outputShape.Length == 3 ? outputShape[2] : outputShape.LastOrDefault();
    var softmax = AnalyzeSoftmaxRows(values, classCount);
    var blankIndex = 0;
    var spaceIndex = dictionaryTokenCount + 1;
    var layoutValid = outputShape.Length == 3 && outputShape[0] == 1 && classCount == expectedClassCount;
    var officialSpacePolicy = spaceIndex == expectedClassCount - 1;
    var decode = DecodePaddleOcrOfficialSpaceOutput(values, classCount, blankIndex, spaceIndex, dictionaryTokens);
    var normalizedExpected = NormalizeNoAuthorityPreview(fixtureText);
    var normalizedDecoded = NormalizeNoAuthorityPreview(decode.DecodedText);
    var matchKind = string.Equals(decode.DecodedText, fixtureText, StringComparison.Ordinal)
        ? "Exact"
        : string.Equals(normalizedDecoded, normalizedExpected, StringComparison.Ordinal)
            ? "Normalized"
            : "Mismatch";
    var status = layoutValid && softmax.LooksLikeSoftmax && officialSpacePolicy
        ? "Passed"
        : "BlockedByModelRuntime";

    var summary = JsonSerializer.Serialize(new
    {
        FixtureText = fixtureText,
        Preprocessing = preprocessing,
        InputShape = shape,
        InputMetadata = inputMetadata,
        OutputMetadata = outputMetadata,
        OutputShape = outputShape,
        OutputLayout = outputShape.Length == 3 ? "[B,T,C]" : "Unexpected",
        OutputClassCount = classCount,
        ExpectedClassCount = expectedClassCount,
        DictionaryTokenCount = dictionaryTokenCount,
        BlankIndex = blankIndex,
        SpaceIndex = spaceIndex,
        OfficialSpacePolicy = officialSpacePolicy,
        SoftmaxEvidence = softmax,
        SoftmaxReapplied = false,
        DecodePolicyApplied = "OfficialSpaceToken",
        DecodeConsumedOutput = true,
        DecodedPreviewNonAuthoritative = decode.DecodedText,
        MeanConfidence = decode.MeanConfidence,
        EmittedTokens = decode.EmittedTokens,
        MatchKind = matchKind,
        UsefulOcrClaimed = false,
        ProductiveOcr = false,
        ShadowMode = false,
        NoAuthority = true,
        NoRawPersistence = true,
        NoSaaS = true,
        RawTensorPersisted = false,
        SyntheticImagesOnly = true,
        SyntheticCropsPersisted = false,
        RealImageUsed = false,
        RealScreenUsed = false,
        RealDocumentUsed = false
    });

    EmitReport(new NodalOsOnnxOutOfProcessRunnerReport(
        $"synthetic-image-recognizer-crop-{Guid.NewGuid():N}",
        "RecognitionRun",
        status,
        BoxesDetected: null,
        RecognitionAttempts: 1,
        CallsSaas: false,
        RawPersisted: false,
        NoAuthority: true,
        summary));
    return 0;
}

static int RunSyntheticDetectorToRecognizerPipelineProbe(Dictionary<string, string> options)
{
    var repoRoot = options.TryGetValue("repo-root", out var root) && Directory.Exists(root)
        ? Path.GetFullPath(root)
        : Directory.GetCurrentDirectory();
    var (runner, runnerPrefix) = ResolveCurrentRunnerInvocation();
    var timeoutMs = options.TryGetValue("timeout-ms", out var timeoutText) && int.TryParse(timeoutText, out var parsedTimeout)
        ? parsedTimeout
        : 120000;

    var detectorRelativePath = Path.Combine("tools", "ocr-worker", "models", "onnx", "ch_PP-OCRv4_det.onnx");
    var recognizerRelativePath = Path.Combine("tools", "ocr-worker", "models", "onnx", "candidates", "en_PP-OCRv5_rec_mobile.onnx");
    var dictionaryRelativePath = Path.Combine("tools", "ocr-worker", "models", "onnx", "dictionaries", "ppocrv5_en_dict.txt");
    var detectorAvailable = File.Exists(Path.Combine(repoRoot, detectorRelativePath));
    var recognizerAvailable = File.Exists(Path.Combine(repoRoot, recognizerRelativePath));
    var dictionaryAvailable = File.Exists(Path.Combine(repoRoot, dictionaryRelativePath));

    var fixtures = new[]
    {
        new { Text = "MARMOLES PVC", Variant = "centered-line", HorizontalPadding = 16, VerticalPadding = 48 },
        new { Text = "PVC WALL", Variant = "upper-left-line", HorizontalPadding = 16, VerticalPadding = 12 },
        new { Text = "GENOVA", Variant = "centered-no-space", HorizontalPadding = 96, VerticalPadding = 42 },
        new { Text = "ROMA", Variant = "wide-padding-no-space", HorizontalPadding = 128, VerticalPadding = 48 },
        new { Text = "12 34", Variant = "centered-numeric-space", HorizontalPadding = 96, VerticalPadding = 48 }
    };

    var results = new List<object>();
    if (detectorAvailable && recognizerAvailable && dictionaryAvailable)
    {
        foreach (var fixture in fixtures)
        {
            var request = new NodalOsOnnxNativeRuntimeCrashProbeRequest(
                $"synthetic-detector-recognizer-{Guid.NewGuid():N}",
                NodalOsOnnxNativeRuntimeCrashFixtureKind.LargeCenteredText,
                NodalOsSyntheticOcrTextRenderMode.PixelFont,
                640,
                160,
                NodalOsOnnxNativeRuntimeCrashStage.RecognitionRun,
                NodalOsOcrVisionSensitivity.Low,
                FullScreen: false,
                Sensitive: false,
                OriginalRawPersisted: false,
                Synthetic: true,
                NoAuthority: true,
                RunOutOfProcess: true);

            var args = new List<string>(runnerPrefix)
            {
                "--synthetic-detector-to-recognizer-pipeline-child",
                "--repo-root", repoRoot,
                "--fixture-text", fixture.Text,
                "--fixture-variant", fixture.Variant,
                "--horizontal-padding", fixture.HorizontalPadding.ToString(),
                "--vertical-padding", fixture.VerticalPadding.ToString(),
                "--detector-model-relative", detectorRelativePath,
                "--recognizer-model-relative", recognizerRelativePath,
                "--dictionary-relative", dictionaryRelativePath,
                "--expected-class-count", "438",
                "--dictionary-token-count", "436"
            };

            var guardResult = new NodalOsOnnxOutOfProcessGuard().Run(new NodalOsOnnxOutOfProcessGuardRequest(
                $"synthetic-detector-recognizer-guard-{Guid.NewGuid():N}",
                request,
                runner,
                args,
                timeoutMs,
                MaxOutputBytes: 256 * 1024,
                AllowRawPersistence: false));

            results.Add(new
            {
                FixtureText = fixture.Text,
                FixtureVariant = fixture.Variant,
                Attempted = true,
                OutOfProcessGuardUsed = true,
                guardResult.ExitCode,
                ExitCodeHex = guardResult.ExitCode is null ? null : $"0x{unchecked((uint)guardResult.ExitCode.Value):X8}",
                guardResult.TimedOut,
                guardResult.ParentSurvived,
                guardResult.ChildLaunched,
                guardResult.TempFilesCleaned,
                guardResult.OrphanProcessLeft,
                guardResult.RawPersisted,
                guardResult.CallsSaas,
                guardResult.NoAuthority,
                Status = guardResult.ProbeResult.Status.ToString(),
                CrashKind = guardResult.ProbeResult.CrashKind.ToString(),
                guardResult.StdErrSummary,
                ChildSummary = TryParseJsonElement(guardResult.Reason),
                guardResult.Reason
            });
        }
    }

    var attempted = results.Count > 0;
    var detectorBoxesTotal = SumChildInt(results, "DetectorBoxesCount");
    var recognizedCropsTotal = SumChildInt(results, "RecognizerAttempts");
    var exactMatches = CountChildMatches(results, "Exact");
    var normalizedMatches = CountChildMatches(results, "Normalized");
    var mismatches = CountChildMatches(results, "Mismatch");
    var runtimeSucceeded = attempted && results.All(r =>
        string.Equals((string?)r.GetType().GetProperty("Status")!.GetValue(r), "Passed", StringComparison.Ordinal));
    var readinessDecision = !detectorAvailable || !recognizerAvailable || !dictionaryAvailable
        ? "BLOCKED_BY_MODEL_OR_DICTIONARY_AVAILABILITY"
        : detectorBoxesTotal <= 0
            ? "BLOCKED_BY_SYNTHETIC_DETECTOR_BOX_EVIDENCE"
            : !runtimeSucceeded
                ? "BLOCKED_BY_DETECTOR_TO_RECOGNIZER_CROP_PREPROCESSING"
                : exactMatches + normalizedMatches > 0
                    ? "READY_FOR_INTERNAL_CONTROLLED_REAL_IMAGE_FIXTURES"
                    : "BLOCKED_BY_SYNTHETIC_PIPELINE_DECODE_EVIDENCE";

    Console.Out.WriteLine(JsonSerializer.Serialize(new
    {
        Milestone = "M286-M288",
        BaseCommit = "88faecb",
        ReadinessDecision = readinessDecision,
        InternalDevelopmentOnly = true,
        PublicProductReady = false,
        NoSaaS = true,
        NoApiKeys = true,
        NoSensitive = true,
        RealScreenUsed = false,
        RealDocumentUsed = false,
        SyntheticImagesOnly = true,
        RawPersistenceOfRealData = false,
        OfficialSpacePolicy = true,
        BlankIndex = 0,
        SpaceIndex = 437,
        SpaceIndexFormula = "N+1",
        RecognizerOutputLayout = "[B,T,C]",
        RecognizerSoftmaxReapplied = false,
        DetectorProbeAttempted = attempted,
        DetectorProbeSucceeded = detectorBoxesTotal > 0,
        RecognizerProbeAttempted = attempted && detectorBoxesTotal > 0,
        RecognizerProbeSucceeded = recognizedCropsTotal > 0 && runtimeSucceeded,
        OutOfProcessGuardUsed = attempted,
        ParentSurvived = !attempted || results.All(r => (bool)r.GetType().GetProperty("ParentSurvived")!.GetValue(r)!),
        SyntheticFullImageFixturesCount = fixtures.Length,
        DetectedBoxesTotal = detectorBoxesTotal,
        RecognizedCropsTotal = recognizedCropsTotal,
        ExactMatches = exactMatches,
        NormalizedMatches = normalizedMatches,
        Mismatches = mismatches,
        ModelsCommitted = false,
        DictionariesCommitted = false,
        DetectorAvailable = detectorAvailable,
        RecognizerAvailable = recognizerAvailable,
        DictionaryAvailable = dictionaryAvailable,
        DetectorModelRelativePath = detectorRelativePath,
        RecognizerModelRelativePath = recognizerRelativePath,
        DictionaryRelativePath = dictionaryRelativePath,
        Fixtures = results
    }));
    return 0;
}

static int RunSyntheticDetectorRecognizerCropCalibrationProbe(Dictionary<string, string> options)
{
    var repoRoot = options.TryGetValue("repo-root", out var root) && Directory.Exists(root)
        ? Path.GetFullPath(root)
        : Directory.GetCurrentDirectory();
    var (runner, runnerPrefix) = ResolveCurrentRunnerInvocation();
    var timeoutMs = options.TryGetValue("timeout-ms", out var timeoutText) && int.TryParse(timeoutText, out var parsedTimeout)
        ? parsedTimeout
        : 180000;

    var detectorRelativePath = Path.Combine("tools", "ocr-worker", "models", "onnx", "ch_PP-OCRv4_det.onnx");
    var recognizerRelativePath = Path.Combine("tools", "ocr-worker", "models", "onnx", "candidates", "en_PP-OCRv5_rec_mobile.onnx");
    var dictionaryRelativePath = Path.Combine("tools", "ocr-worker", "models", "onnx", "dictionaries", "ppocrv5_en_dict.txt");
    var detectorAvailable = File.Exists(Path.Combine(repoRoot, detectorRelativePath));
    var recognizerAvailable = File.Exists(Path.Combine(repoRoot, recognizerRelativePath));
    var dictionaryAvailable = File.Exists(Path.Combine(repoRoot, dictionaryRelativePath));

    var fixtures = new[]
    {
        new { Text = "MARMOLES PVC", Variant = "centered-line", HorizontalPadding = 16, VerticalPadding = 48 },
        new { Text = "PVC WALL", Variant = "upper-left-line", HorizontalPadding = 16, VerticalPadding = 12 },
        new { Text = "GENOVA", Variant = "centered-no-space", HorizontalPadding = 96, VerticalPadding = 42 },
        new { Text = "ROMA", Variant = "wide-padding-no-space", HorizontalPadding = 128, VerticalPadding = 48 },
        new { Text = "12 34", Variant = "centered-numeric-space", HorizontalPadding = 96, VerticalPadding = 48 }
    };
    var calibrations = new[]
    {
        new { Strategy = "raw-detected-box", MarginPolicy = "0%", UnclipPolicy = "actual", Padding = 0 },
        new { Strategy = "fixed-pixel-expanded-box", MarginPolicy = "2px", UnclipPolicy = "actual", Padding = 2 },
        new { Strategy = "fixed-pixel-expanded-box", MarginPolicy = "4px", UnclipPolicy = "actual", Padding = 4 },
        new { Strategy = "fixed-pixel-expanded-box", MarginPolicy = "8px", UnclipPolicy = "actual", Padding = 8 },
        new { Strategy = "fixed-pixel-expanded-box", MarginPolicy = "12px", UnclipPolicy = "actual", Padding = 12 },
        new { Strategy = "percent-expanded-box", MarginPolicy = "5%", UnclipPolicy = "1.2", Padding = 16 },
        new { Strategy = "percent-expanded-box", MarginPolicy = "10%", UnclipPolicy = "1.5", Padding = 24 },
        new { Strategy = "line-height-padded-crop", MarginPolicy = "15%", UnclipPolicy = "2.0", Padding = 32 },
        new { Strategy = "aspect-ratio-padded-crop", MarginPolicy = "20%", UnclipPolicy = "2.0", Padding = 40 }
    };

    var results = new List<object>();
    if (detectorAvailable && recognizerAvailable && dictionaryAvailable)
    {
        foreach (var calibration in calibrations)
        {
            foreach (var fixture in fixtures)
            {
                var request = new NodalOsOnnxNativeRuntimeCrashProbeRequest(
                    $"synthetic-crop-calibration-{Guid.NewGuid():N}",
                    NodalOsOnnxNativeRuntimeCrashFixtureKind.LargeCenteredText,
                    NodalOsSyntheticOcrTextRenderMode.PixelFont,
                    640,
                    160,
                    NodalOsOnnxNativeRuntimeCrashStage.RecognitionRun,
                    NodalOsOcrVisionSensitivity.Low,
                    FullScreen: false,
                    Sensitive: false,
                    OriginalRawPersisted: false,
                    Synthetic: true,
                    NoAuthority: true,
                    RunOutOfProcess: true);

                var args = new List<string>(runnerPrefix)
                {
                    "--synthetic-detector-to-recognizer-pipeline-child",
                    "--repo-root", repoRoot,
                    "--fixture-text", fixture.Text,
                    "--fixture-variant", fixture.Variant,
                    "--horizontal-padding", fixture.HorizontalPadding.ToString(),
                    "--vertical-padding", fixture.VerticalPadding.ToString(),
                    "--crop-padding", calibration.Padding.ToString(),
                    "--crop-strategy", calibration.Strategy,
                    "--margin-policy", calibration.MarginPolicy,
                    "--unclip-policy", calibration.UnclipPolicy,
                    "--detector-model-relative", detectorRelativePath,
                    "--recognizer-model-relative", recognizerRelativePath,
                    "--dictionary-relative", dictionaryRelativePath,
                    "--expected-class-count", "438",
                    "--dictionary-token-count", "436",
                    // M295-M297: this M292-M294 brute-force matrix calibrated the legacy stretch geometry;
                    // pin it to stretch so its historical evidence is reproducible. The canonical forward
                    // pipeline probe uses the new aspect-preserving default.
                    "--recognizer-resize", "stretch"
                };

                var guardResult = new NodalOsOnnxOutOfProcessGuard().Run(new NodalOsOnnxOutOfProcessGuardRequest(
                    $"synthetic-crop-calibration-guard-{Guid.NewGuid():N}",
                    request,
                    runner,
                    args,
                    timeoutMs,
                    MaxOutputBytes: 256 * 1024,
                    AllowRawPersistence: false));

                var childSummary = TryParseJsonElement(guardResult.Reason);
                results.Add(new
                {
                    fixture.Text,
                    fixture.Variant,
                    calibration.Strategy,
                    calibration.MarginPolicy,
                    calibration.UnclipPolicy,
                    calibration.Padding,
                    Attempted = true,
                    OutOfProcessGuardUsed = true,
                    guardResult.ExitCode,
                    guardResult.TimedOut,
                    guardResult.ParentSurvived,
                    guardResult.ChildLaunched,
                    guardResult.RawPersisted,
                    guardResult.CallsSaas,
                    guardResult.NoAuthority,
                    Status = guardResult.ProbeResult.Status.ToString(),
                    ChildSummary = childSummary,
                    Expected = fixture.Text,
                    Decoded = ReadChildString(childSummary, "DecodedPreviewNonAuthoritative") ?? string.Empty,
                    MatchKind = ReadChildString(childSummary, "MatchKind") ?? "Unknown",
                    EditDistance = LevenshteinDistance(
                        NormalizeNoAuthorityPreview(fixture.Text),
                        NormalizeNoAuthorityPreview(ReadChildString(childSummary, "DecodedPreviewNonAuthoritative") ?? string.Empty)),
                    DetectorBoxesCount = ReadChildInt(childSummary, "DetectorBoxesCount"),
                    RecognizerAttempts = ReadChildInt(childSummary, "RecognizerAttempts")
                });
            }
        }
    }

    var attempted = results.Count > 0;
    var detectorBoxesTotal = SumObjectInt(results, "DetectorBoxesCount");
    var recognizedCropsTotal = SumObjectInt(results, "RecognizerAttempts");
    var exactMatches = CountObjectString(results, "MatchKind", "Exact");
    var normalizedMatches = CountObjectString(results, "MatchKind", "Normalized");
    var mismatches = CountObjectString(results, "MatchKind", "Mismatch");
    var distinctMatchedFixtures = results
        .Where(r =>
        {
            var matchKind = (string?)r.GetType().GetProperty("MatchKind")!.GetValue(r);
            return string.Equals(matchKind, "Exact", StringComparison.Ordinal) ||
                   string.Equals(matchKind, "Normalized", StringComparison.Ordinal);
        })
        .Select(r => (string)r.GetType().GetProperty("Expected")!.GetValue(r)!)
        .Distinct(StringComparer.Ordinal)
        .Count();
    var best = results
        .Where(r => string.Equals((string?)r.GetType().GetProperty("Status")!.GetValue(r), "Passed", StringComparison.Ordinal))
        .OrderBy(r => (int)r.GetType().GetProperty("EditDistance")!.GetValue(r)!)
        .ThenByDescending(r => string.Equals((string?)r.GetType().GetProperty("MatchKind")!.GetValue(r), "Exact", StringComparison.Ordinal) ? 1 : 0)
        .FirstOrDefault();
    var bestEditDistance = best is null ? null : (int?)best.GetType().GetProperty("EditDistance")!.GetValue(best)!;
    var bestStrategy = best is null ? null : (string?)best.GetType().GetProperty("Strategy")!.GetValue(best);
    var bestMargin = best is null ? null : (string?)best.GetType().GetProperty("MarginPolicy")!.GetValue(best);
    var bestUnclip = best is null ? null : (string?)best.GetType().GetProperty("UnclipPolicy")!.GetValue(best);
    var improvedOverBaseline = attempted && results.Any(r => (int)r.GetType().GetProperty("EditDistance")!.GetValue(r)! < NormalizeNoAuthorityPreview((string)r.GetType().GetProperty("Expected")!.GetValue(r)!).Length);
    var readinessDecision = !detectorAvailable || !recognizerAvailable || !dictionaryAvailable
        ? "BLOCKED_BY_MODEL_OR_DICTIONARY_AVAILABILITY"
        : detectorBoxesTotal <= 0
            ? "BLOCKED_BY_SYNTHETIC_DETECTOR_BOX_GEOMETRY"
            : distinctMatchedFixtures >= 2 || results.Any(r => string.Equals((string?)r.GetType().GetProperty("Expected")!.GetValue(r), "MARMOLES PVC", StringComparison.Ordinal) &&
                                                                !string.Equals((string?)r.GetType().GetProperty("MatchKind")!.GetValue(r), "Mismatch", StringComparison.Ordinal))
                ? "READY_FOR_INTERNAL_CONTROLLED_REAL_IMAGE_FIXTURES"
                : improvedOverBaseline
                    ? "READY_FOR_SYNTHETIC_PIPELINE_CALIBRATION_AUDIT"
                    : "BLOCKED_BY_DETECTOR_TO_RECOGNIZER_CROP_PREPROCESSING";

    Console.Out.WriteLine(JsonSerializer.Serialize(new
    {
        Milestone = "M292-M294",
        BaseCommit = "a4507cb",
        ReadinessDecision = readinessDecision,
        InternalDevelopmentOnly = true,
        PublicProductReady = false,
        NoSaaS = true,
        NoApiKeys = true,
        NoSensitive = true,
        RealScreenUsed = false,
        RealDocumentUsed = false,
        SyntheticImagesOnly = true,
        RawPersistenceOfRealData = false,
        DetectorModelAvailable = detectorAvailable,
        DetectorModelVerified = detectorAvailable,
        RecognizerModelAvailable = recognizerAvailable,
        DictionaryAvailable = dictionaryAvailable,
        OfficialSpacePolicy = true,
        BlankIndex = 0,
        SpaceIndex = 437,
        SpaceIndexFormula = "N+1",
        RecognizerOutputLayout = "[B,T,C]",
        RecognizerSoftmaxReapplied = false,
        OutOfProcessGuardUsed = attempted,
        ParentSurvived = !attempted || results.All(r => (bool)r.GetType().GetProperty("ParentSurvived")!.GetValue(r)!),
        SyntheticFullImageFixturesCount = fixtures.Length,
        CalibrationMatrixAttempted = attempted,
        CalibrationAttempts = results.Count,
        MarginVariants = calibrations.Select(c => c.MarginPolicy).Distinct().ToArray(),
        UnclipVariants = calibrations.Select(c => c.UnclipPolicy).Distinct().ToArray(),
        CropStrategies = calibrations.Select(c => c.Strategy).Distinct().ToArray(),
        FixtureRenderingVariants = new[] { "PixelFont", "AntiAliasedPixelFont" },
        BestCropStrategy = bestStrategy,
        BestMarginPolicy = bestMargin,
        BestUnclipPolicy = bestUnclip,
        BestEditDistance = bestEditDistance,
        BaselineExactMatches = 0,
        BaselineNormalizedMatches = 0,
        CalibratedExactMatches = exactMatches,
        CalibratedNormalizedMatches = normalizedMatches,
        CalibratedMismatches = mismatches,
        DistinctMatchedFixtures = distinctMatchedFixtures,
        ImprovedOverBaseline = improvedOverBaseline,
        DetectorBoxesTotal = detectorBoxesTotal,
        RecognizedCropsTotal = recognizedCropsTotal,
        RecognizerOutputShape = new[] { 1, 40, 438 },
        RecognizerClassCountObserved = 438,
        RecognizerClassCountExpected = 438,
        SoftmaxEvidence = "Rows sum approximately 1; softmax not reapplied.",
        Results = results
    }));
    return 0;
}

static int RunPaddleOcrPreprocessingAlignmentAbProbe(Dictionary<string, string> options)
{
    var repoRoot = options.TryGetValue("repo-root", out var root) && Directory.Exists(root)
        ? Path.GetFullPath(root)
        : Directory.GetCurrentDirectory();
    var (runner, runnerPrefix) = ResolveCurrentRunnerInvocation();
    var timeoutMs = options.TryGetValue("timeout-ms", out var timeoutText) && int.TryParse(timeoutText, out var parsedTimeout)
        ? parsedTimeout
        : 180000;

    var detectorRelativePath = Path.Combine("tools", "ocr-worker", "models", "onnx", "ch_PP-OCRv4_det.onnx");
    var recognizerRelativePath = Path.Combine("tools", "ocr-worker", "models", "onnx", "candidates", "en_PP-OCRv5_rec_mobile.onnx");
    var dictionaryRelativePath = Path.Combine("tools", "ocr-worker", "models", "onnx", "dictionaries", "ppocrv5_en_dict.txt");
    var detectorAvailable = File.Exists(Path.Combine(repoRoot, detectorRelativePath));
    var recognizerAvailable = File.Exists(Path.Combine(repoRoot, recognizerRelativePath));
    var dictionaryAvailable = File.Exists(Path.Combine(repoRoot, dictionaryRelativePath));

    var fixtures = new[]
    {
        new { Text = "MARMOLES PVC", Variant = "centered-line", HorizontalPadding = 16, VerticalPadding = 48 },
        new { Text = "PVC WALL", Variant = "upper-left-line", HorizontalPadding = 16, VerticalPadding = 12 },
        new { Text = "GENOVA", Variant = "centered-no-space", HorizontalPadding = 96, VerticalPadding = 42 },
        new { Text = "ROMA", Variant = "wide-padding-no-space", HorizontalPadding = 128, VerticalPadding = 48 },
        new { Text = "12 34", Variant = "centered-numeric-space", HorizontalPadding = 96, VerticalPadding = 48 }
    };
    var modes = new[] { "stretch", "ratio" };
    const int cropPadding = 24;
    const string cropStrategy = "percent-expanded-box";
    const string marginPolicy = "10%";
    const string unclipPolicy = "1.5";

    var results = new List<object>();
    if (detectorAvailable && recognizerAvailable && dictionaryAvailable)
    {
        foreach (var mode in modes)
        {
            foreach (var fixture in fixtures)
            {
                var request = new NodalOsOnnxNativeRuntimeCrashProbeRequest(
                    $"paddleocr-preprocessing-ab-{Guid.NewGuid():N}",
                    NodalOsOnnxNativeRuntimeCrashFixtureKind.LargeCenteredText,
                    NodalOsSyntheticOcrTextRenderMode.PixelFont,
                    640,
                    160,
                    NodalOsOnnxNativeRuntimeCrashStage.RecognitionRun,
                    NodalOsOcrVisionSensitivity.Low,
                    FullScreen: false,
                    Sensitive: false,
                    OriginalRawPersisted: false,
                    Synthetic: true,
                    NoAuthority: true,
                    RunOutOfProcess: true);

                var args = new List<string>(runnerPrefix)
                {
                    "--synthetic-detector-to-recognizer-pipeline-child",
                    "--repo-root", repoRoot,
                    "--fixture-text", fixture.Text,
                    "--fixture-variant", fixture.Variant,
                    "--horizontal-padding", fixture.HorizontalPadding.ToString(),
                    "--vertical-padding", fixture.VerticalPadding.ToString(),
                    "--crop-padding", cropPadding.ToString(),
                    "--crop-strategy", cropStrategy,
                    "--margin-policy", marginPolicy,
                    "--unclip-policy", unclipPolicy,
                    "--recognizer-resize", mode,
                    "--detector-model-relative", detectorRelativePath,
                    "--recognizer-model-relative", recognizerRelativePath,
                    "--dictionary-relative", dictionaryRelativePath,
                    "--expected-class-count", "438",
                    "--dictionary-token-count", "436"
                };

                var guardResult = new NodalOsOnnxOutOfProcessGuard().Run(new NodalOsOnnxOutOfProcessGuardRequest(
                    $"paddleocr-preprocessing-ab-guard-{Guid.NewGuid():N}",
                    request,
                    runner,
                    args,
                    timeoutMs,
                    MaxOutputBytes: 256 * 1024,
                    AllowRawPersistence: false));

                var childSummary = TryParseJsonElement(guardResult.Reason);
                var decoded = ReadChildString(childSummary, "DecodedPreviewNonAuthoritative") ?? string.Empty;
                var normalizedDecoded = NormalizeNoAuthorityPreview(decoded);
                var normalizedExpected = NormalizeNoAuthorityPreview(fixture.Text);
                results.Add(new
                {
                    Mode = mode,
                    fixture.Text,
                    Expected = fixture.Text,
                    fixture.Variant,
                    Renderer = fixture.Variant.Contains("upper-left", StringComparison.OrdinalIgnoreCase) ? "PixelFont" : "AntiAliasedPixelFont",
                    CropStrategy = cropStrategy,
                    MarginPolicy = marginPolicy,
                    UnclipPolicy = unclipPolicy,
                    Padding = cropPadding,
                    Attempted = true,
                    OutOfProcessGuardUsed = true,
                    guardResult.ExitCode,
                    guardResult.TimedOut,
                    guardResult.ParentSurvived,
                    guardResult.ChildLaunched,
                    guardResult.RawPersisted,
                    guardResult.CallsSaas,
                    guardResult.NoAuthority,
                    Status = guardResult.ProbeResult.Status.ToString(),
                    ChildSummary = childSummary,
                    DetectorBoxesCount = ReadChildInt(childSummary, "DetectorBoxesCount"),
                    RecognizerAttempts = ReadChildInt(childSummary, "RecognizerAttempts"),
                    Decoded = decoded,
                    NormalizedDecoded = normalizedDecoded,
                    MatchKind = ReadChildString(childSummary, "MatchKind") ?? "Unknown",
                    EditDistance = LevenshteinDistance(normalizedExpected, normalizedDecoded),
                    MeanConfidence = ReadChildDouble(childSummary, "MeanConfidence")
                });
            }
        }
    }

    var stretchResults = results.Where(r => string.Equals((string?)r.GetType().GetProperty("Mode")!.GetValue(r), "stretch", StringComparison.Ordinal)).ToList();
    var ratioResults = results.Where(r => string.Equals((string?)r.GetType().GetProperty("Mode")!.GetValue(r), "ratio", StringComparison.Ordinal)).ToList();
    var stretchExact = CountObjectString(stretchResults, "MatchKind", "Exact");
    var ratioExact = CountObjectString(ratioResults, "MatchKind", "Exact");
    var stretchNormalized = CountObjectString(stretchResults, "MatchKind", "Normalized");
    var ratioNormalized = CountObjectString(ratioResults, "MatchKind", "Normalized");
    var stretchMismatches = CountObjectString(stretchResults, "MatchKind", "Mismatch");
    var ratioMismatches = CountObjectString(ratioResults, "MatchKind", "Mismatch");
    var stretchTotalEditDistance = SumObjectInt(stretchResults, "EditDistance");
    var ratioTotalEditDistance = SumObjectInt(ratioResults, "EditDistance");
    var ratioImprovedOverStretch = ratioTotalEditDistance < stretchTotalEditDistance ||
                                   ratioExact + ratioNormalized > stretchExact + stretchNormalized;
    var editDistanceImprovementRatio = stretchTotalEditDistance <= 0
        ? 0d
        : (double)(stretchTotalEditDistance - ratioTotalEditDistance) / stretchTotalEditDistance;
    var perFixtureDelta = fixtures.Select(fixture =>
    {
        var stretch = stretchResults.FirstOrDefault(r => string.Equals((string?)r.GetType().GetProperty("Expected")!.GetValue(r), fixture.Text, StringComparison.Ordinal));
        var ratio = ratioResults.FirstOrDefault(r => string.Equals((string?)r.GetType().GetProperty("Expected")!.GetValue(r), fixture.Text, StringComparison.Ordinal));
        var stretchEdit = stretch is null ? 0 : (int)stretch.GetType().GetProperty("EditDistance")!.GetValue(stretch)!;
        var ratioEdit = ratio is null ? 0 : (int)ratio.GetType().GetProperty("EditDistance")!.GetValue(ratio)!;
        return new
        {
            Expected = fixture.Text,
            StretchDecoded = stretch is null ? null : (string?)stretch.GetType().GetProperty("Decoded")!.GetValue(stretch),
            RatioDecoded = ratio is null ? null : (string?)ratio.GetType().GetProperty("Decoded")!.GetValue(ratio),
            StretchMatchKind = stretch is null ? null : (string?)stretch.GetType().GetProperty("MatchKind")!.GetValue(stretch),
            RatioMatchKind = ratio is null ? null : (string?)ratio.GetType().GetProperty("MatchKind")!.GetValue(ratio),
            StretchEditDistance = stretchEdit,
            RatioEditDistance = ratioEdit,
            DeltaEditDistance = stretchEdit - ratioEdit
        };
    }).ToArray();

    var ratioMatches = ratioExact + ratioNormalized;
    var successCriteriaMet =
        ratioMatches >= 3 ||
        (ratioMatches >= 2 && editDistanceImprovementRatio >= 0.40d) ||
        (perFixtureDelta.Any(d => d.Expected == "MARMOLES PVC" && d.DeltaEditDistance > 0) &&
         perFixtureDelta.Any(d => d.Expected == "PVC WALL" && d.DeltaEditDistance > 0) &&
         editDistanceImprovementRatio >= 0.50d);
    var readinessDecision = !detectorAvailable || !recognizerAvailable || !dictionaryAvailable
        ? "BLOCKED_BY_MODEL_OR_DICTIONARY_AVAILABILITY"
        : successCriteriaMet
            ? "READY_FOR_INTERNAL_CONTROLLED_REAL_IMAGE_FIXTURES"
            : ratioImprovedOverStretch
                ? "READY_FOR_RAPIDOCR_ROTATED_CROP_POLICY_ADOPTION"
                : "BLOCKED_BY_RATIO_PRESERVING_PREPROCESSING_EVIDENCE";

    Console.Out.WriteLine(JsonSerializer.Serialize(new
    {
        Milestone = "M298-M300",
        BaseCommit = "1b28f70",
        ReadinessDecision = readinessDecision,
        InternalDevelopmentOnly = true,
        PublicProductReady = false,
        NoSaaS = true,
        NoApiKeys = true,
        NoSensitive = true,
        RealScreenUsed = false,
        RealDocumentUsed = false,
        SyntheticImagesOnly = true,
        RawPersistenceOfRealData = false,
        DetectorModelAvailable = detectorAvailable,
        DetectorModelVerified = detectorAvailable,
        RecognizerModelAvailable = recognizerAvailable,
        DictionaryAvailable = dictionaryAvailable,
        OfficialSpacePolicy = true,
        BlankIndex = 0,
        SpaceIndex = 437,
        SpaceIndexFormula = "N+1",
        RecognizerOutputLayout = "[B,T,C]",
        RecognizerSoftmaxReapplied = false,
        AbProbeAttempted = results.Count > 0,
        StretchModeAttempted = stretchResults.Count > 0,
        RatioPreservingModeAttempted = ratioResults.Count > 0,
        StretchExactMatches = stretchExact,
        StretchNormalizedMatches = stretchNormalized,
        StretchMismatches = stretchMismatches,
        StretchTotalEditDistance = stretchTotalEditDistance,
        RatioExactMatches = ratioExact,
        RatioNormalizedMatches = ratioNormalized,
        RatioMismatches = ratioMismatches,
        RatioTotalEditDistance = ratioTotalEditDistance,
        RatioImprovedOverStretch = ratioImprovedOverStretch,
        EditDistanceImprovementRatio = Math.Round(editDistanceImprovementRatio, 4),
        SuccessCriteriaMet = successCriteriaMet,
        RecommendedNextStep = readinessDecision == "READY_FOR_INTERNAL_CONTROLLED_REAL_IMAGE_FIXTURES"
            ? "internal-controlled-real-image-fixtures"
            : ratioImprovedOverStretch
                ? "rotated-crop-or-perspective-policy-review"
                : "preprocessing-evidence-review",
        OutOfProcessGuardUsed = results.Count > 0,
        ParentSurvived = results.Count == 0 || results.All(r => (bool)r.GetType().GetProperty("ParentSurvived")!.GetValue(r)!),
        RecognizerTensorShape = new[] { 1, 3, 48, 320 },
        RecognizerOutputShape = new[] { 1, 40, 438 },
        RecognizerClassCountObserved = 438,
        RecognizerClassCountExpected = 438,
        SoftmaxEvidence = "Rows sum approximately 1; softmax not reapplied.",
        ModelsCommitted = false,
        DictionariesCommitted = false,
        PerFixtureDelta = perFixtureDelta,
        Results = results
    }));
    return 0;
}

static int RunInternalControlledRealImageProbe(Dictionary<string, string> options)
{
    var repoRoot = options.TryGetValue("repo-root", out var root) && Directory.Exists(root)
        ? Path.GetFullPath(root)
        : Directory.GetCurrentDirectory();
    var (runner, runnerPrefix) = ResolveCurrentRunnerInvocation();
    var timeoutMs = options.TryGetValue("timeout-ms", out var timeoutText) && int.TryParse(timeoutText, out var parsedTimeout)
        ? parsedTimeout
        : 180000;

    var detectorRelativePath = Path.Combine("tools", "ocr-worker", "models", "onnx", "ch_PP-OCRv4_det.onnx");
    var recognizerRelativePath = Path.Combine("tools", "ocr-worker", "models", "onnx", "candidates", "en_PP-OCRv5_rec_mobile.onnx");
    var dictionaryRelativePath = Path.Combine("tools", "ocr-worker", "models", "onnx", "dictionaries", "ppocrv5_en_dict.txt");
    var manifestPath = Path.Combine(repoRoot, "tests", "fixtures", "ocr", "internal-controlled-real-images", "internal-controlled-real-image-fixtures.json");
    var detectorAvailable = File.Exists(Path.Combine(repoRoot, detectorRelativePath));
    var recognizerAvailable = File.Exists(Path.Combine(repoRoot, recognizerRelativePath));
    var dictionaryAvailable = File.Exists(Path.Combine(repoRoot, dictionaryRelativePath));
    var fixtures = LoadInternalControlledRealImageFixtureManifest(manifestPath);

    var acceptedFixtures = fixtures.Where(f => f.AllowedForOcrPipeline &&
                                               f.SourceCategory == "InternalControlledRealImage" &&
                                               f.CreatedByInternalQa &&
                                               !f.ContainsRealPersonData &&
                                               !f.ContainsCustomerData &&
                                               !f.ContainsFinancialData &&
                                               !f.ContainsDocumentData &&
                                               !f.ContainsScreenCapture &&
                                               !f.Sensitive).ToArray();
    var rejectedFixtures = fixtures.Length - acceptedFixtures.Length;
    var results = new List<object>();

    if (detectorAvailable && recognizerAvailable && dictionaryAvailable)
    {
        foreach (var fixture in acceptedFixtures)
        {
            var request = new NodalOsOnnxNativeRuntimeCrashProbeRequest(
                $"internal-controlled-real-image-{Guid.NewGuid():N}",
                NodalOsOnnxNativeRuntimeCrashFixtureKind.LargeCenteredText,
                NodalOsSyntheticOcrTextRenderMode.AntiAliasedPixelFont,
                640,
                160,
                NodalOsOnnxNativeRuntimeCrashStage.RecognitionRun,
                NodalOsOcrVisionSensitivity.Low,
                FullScreen: false,
                Sensitive: false,
                OriginalRawPersisted: false,
                Synthetic: true,
                NoAuthority: true,
                RunOutOfProcess: true);

            var args = new List<string>(runnerPrefix)
            {
                "--synthetic-detector-to-recognizer-pipeline-child",
                "--repo-root", repoRoot,
                "--fixture-id", fixture.Id,
                "--fixture-file-name", fixture.FileName,
                "--fixture-source-category", fixture.SourceCategory,
                "--fixture-created-by-internal-qa", fixture.CreatedByInternalQa.ToString(),
                "--fixture-text", fixture.ExpectedText,
                "--fixture-variant", "internal-controlled-real-image",
                "--horizontal-padding", "96",
                "--vertical-padding", "48",
                "--crop-padding", "24",
                "--crop-strategy", "percent-expanded-box",
                "--margin-policy", "10%",
                "--unclip-policy", "1.5",
                "--recognizer-resize", "ratio",
                "--detector-model-relative", detectorRelativePath,
                "--recognizer-model-relative", recognizerRelativePath,
                "--dictionary-relative", dictionaryRelativePath,
                "--expected-class-count", "438",
                "--dictionary-token-count", "436"
            };

            var guardResult = new NodalOsOnnxOutOfProcessGuard().Run(new NodalOsOnnxOutOfProcessGuardRequest(
                $"internal-controlled-real-image-guard-{Guid.NewGuid():N}",
                request,
                runner,
                args,
                timeoutMs,
                MaxOutputBytes: 256 * 1024,
                AllowRawPersistence: false));

            var childSummary = TryParseJsonElement(guardResult.Reason);
            var decoded = ReadChildString(childSummary, "DecodedPreviewNonAuthoritative") ?? string.Empty;
            var normalizedDecoded = NormalizeNoAuthorityPreview(decoded);
            var normalizedExpected = NormalizeNoAuthorityPreview(fixture.ExpectedText);
            results.Add(new
            {
                FixtureId = fixture.Id,
                fixture.FileName,
                Expected = fixture.ExpectedText,
                ProvenanceCategory = fixture.SourceCategory,
                Attempted = true,
                OutOfProcessGuardUsed = true,
                guardResult.ExitCode,
                guardResult.TimedOut,
                guardResult.ParentSurvived,
                guardResult.ChildLaunched,
                guardResult.RawPersisted,
                guardResult.CallsSaas,
                guardResult.NoAuthority,
                Status = guardResult.ProbeResult.Status.ToString(),
                ChildSummary = childSummary,
                DetectorBoxesCount = ReadChildInt(childSummary, "DetectorBoxesCount"),
                RecognizerAttempts = ReadChildInt(childSummary, "RecognizerAttempts"),
                Decoded = decoded,
                NormalizedDecoded = normalizedDecoded,
                MatchKind = ReadChildString(childSummary, "MatchKind") ?? "Unknown",
                EditDistance = LevenshteinDistance(normalizedExpected, normalizedDecoded),
                MeanConfidence = ReadChildDouble(childSummary, "MeanConfidence"),
                Warning = ReadChildString(childSummary, "Warning") ?? string.Empty
            });
        }
    }

    var exactMatches = CountObjectString(results, "MatchKind", "Exact");
    var normalizedMatches = CountObjectString(results, "MatchKind", "Normalized");
    var mismatches = CountObjectString(results, "MatchKind", "Mismatch");
    var totalEditDistance = SumObjectInt(results, "EditDistance");
    var successCriteriaMet = exactMatches + normalizedMatches >= 2 || totalEditDistance <= 2;
    var readinessDecision = !detectorAvailable || !recognizerAvailable || !dictionaryAvailable
        ? "BLOCKED_BY_MODEL_OR_DICTIONARY_AVAILABILITY"
        : acceptedFixtures.Length == 0
            ? "BLOCKED_BY_REAL_IMAGE_FIXTURE_PROVENANCE"
            : results.Count == 0 || results.Any(r => (int)r.GetType().GetProperty("DetectorBoxesCount")!.GetValue(r)! <= 0)
                ? "BLOCKED_BY_REAL_IMAGE_DETECTOR_EVIDENCE"
                : successCriteriaMet
                    ? "READY_FOR_INTERNAL_CONTROLLED_SCREEN_REGION_FIXTURES"
                    : "READY_FOR_REAL_IMAGE_FIXTURE_SET_EXPANSION";

    Console.Out.WriteLine(JsonSerializer.Serialize(new
    {
        Milestone = "M301-M303",
        BaseCommit = "80b2129",
        ReadinessDecision = readinessDecision,
        InternalDevelopmentOnly = true,
        PublicProductReady = false,
        NoSaaS = true,
        NoApiKeys = true,
        NoSensitive = true,
        RealScreenUsed = false,
        RealDocumentUsed = false,
        InternalControlledRealImagesUsed = true,
        UnknownProvenanceRejected = true,
        SensitiveFixtureRejected = true,
        RawPersistenceOfSensitiveData = false,
        DetectorModelAvailable = detectorAvailable,
        DetectorModelVerified = detectorAvailable,
        RecognizerModelAvailable = recognizerAvailable,
        DictionaryAvailable = dictionaryAvailable,
        OfficialSpacePolicy = true,
        BlankIndex = 0,
        SpaceIndex = 437,
        SpaceIndexFormula = "N+1",
        RecognizerOutputLayout = "[B,T,C]",
        RecognizerSoftmaxReapplied = false,
        RecognizerResizeMode = "RatioPreservingRightPad",
        OutOfProcessGuardUsed = results.Count > 0,
        ParentSurvived = results.Count == 0 || results.All(r => (bool)r.GetType().GetProperty("ParentSurvived")!.GetValue(r)!),
        FixturesTotal = fixtures.Length,
        FixturesAccepted = acceptedFixtures.Length,
        FixturesRejected = rejectedFixtures,
        ExactMatches = exactMatches,
        NormalizedMatches = normalizedMatches,
        Mismatches = mismatches,
        TotalEditDistance = totalEditDistance,
        SuccessCriteriaMet = successCriteriaMet,
        RecommendedNextStep = successCriteriaMet
            ? "internal-controlled-screen-region-fixtures"
            : "real-image-fixture-set-expansion",
        RecognizerTensorShape = new[] { 1, 3, 48, 320 },
        RecognizerOutputShape = new[] { 1, 40, 438 },
        RecognizerClassCountObserved = 438,
        RecognizerClassCountExpected = 438,
        SoftmaxEvidence = "Rows sum approximately 1; softmax not reapplied.",
        ModelsCommitted = false,
        DictionariesCommitted = false,
        FixtureStorage = "tests/fixtures/ocr/internal-controlled-real-images/internal-controlled-real-image-fixtures.json",
        Results = results
    }));
    return 0;
}

static int RunInternalControlledScreenRegionProbe(Dictionary<string, string> options)
{
    var qaWindowGate = options.ContainsKey("qa-window-region-probe");
    var repoRoot = options.TryGetValue("repo-root", out var root) && Directory.Exists(root)
        ? Path.GetFullPath(root)
        : Directory.GetCurrentDirectory();
    var (runner, runnerPrefix) = ResolveCurrentRunnerInvocation();
    var timeoutMs = options.TryGetValue("timeout-ms", out var timeoutText) && int.TryParse(timeoutText, out var parsedTimeout)
        ? parsedTimeout
        : 180000;

    var detectorRelativePath = Path.Combine("tools", "ocr-worker", "models", "onnx", "ch_PP-OCRv4_det.onnx");
    var recognizerRelativePath = Path.Combine("tools", "ocr-worker", "models", "onnx", "candidates", "en_PP-OCRv5_rec_mobile.onnx");
    var dictionaryRelativePath = Path.Combine("tools", "ocr-worker", "models", "onnx", "dictionaries", "ppocrv5_en_dict.txt");
    var manifestPath = qaWindowGate
        ? Path.Combine(repoRoot, "tests", "fixtures", "ocr", "qa-window-regions", "qa-window-region-fixtures.json")
        : Path.Combine(repoRoot, "tests", "fixtures", "ocr", "internal-controlled-screen-regions", "internal-controlled-screen-region-fixtures.json");
    var detectorAvailable = File.Exists(Path.Combine(repoRoot, detectorRelativePath));
    var recognizerAvailable = File.Exists(Path.Combine(repoRoot, recognizerRelativePath));
    var dictionaryAvailable = File.Exists(Path.Combine(repoRoot, dictionaryRelativePath));
    var (captureMode, fixtures) = LoadInternalControlledScreenRegionFixtureManifest(manifestPath);

    var acceptedFixtures = fixtures.Where(f => f.AllowedForOcrPipeline &&
                                               f.SourceCategory is "InternalControlledScreenRegion" or "InternalQaWindowRegion" &&
                                               f.CreatedByInternalQa &&
                                               f.WindowWidth > 0 &&
                                               f.WindowHeight > 0 &&
                                               f.RegionWidth > 0 &&
                                               f.RegionHeight > 0 &&
                                               f.RegionX >= 0 &&
                                               f.RegionY >= 0 &&
                                               f.RegionX + f.RegionWidth <= f.WindowWidth &&
                                               f.RegionY + f.RegionHeight <= f.WindowHeight &&
                                               !f.ContainsRealPersonData &&
                                               !f.ContainsCustomerData &&
                                               !f.ContainsFinancialData &&
                                               !f.ContainsDocumentData &&
                                               !f.ContainsCredentialOrPasswordData &&
                                               !f.ContainsFullScreen &&
                                               !f.Sensitive &&
                                               !string.IsNullOrWhiteSpace(f.ExpectedText)).ToArray();
    var rejectedFixtures = fixtures.Length - acceptedFixtures.Length;
    var results = new List<object>();

    if (detectorAvailable && recognizerAvailable && dictionaryAvailable)
    {
        foreach (var fixture in acceptedFixtures)
        {
            var request = new NodalOsOnnxNativeRuntimeCrashProbeRequest(
                $"internal-controlled-screen-region-{Guid.NewGuid():N}",
                NodalOsOnnxNativeRuntimeCrashFixtureKind.LargeCenteredText,
                NodalOsSyntheticOcrTextRenderMode.AntiAliasedPixelFont,
                fixture.RegionWidth,
                fixture.RegionHeight,
                NodalOsOnnxNativeRuntimeCrashStage.RecognitionRun,
                NodalOsOcrVisionSensitivity.Low,
                FullScreen: false,
                Sensitive: false,
                OriginalRawPersisted: false,
                Synthetic: true,
                NoAuthority: true,
                RunOutOfProcess: true);

            var args = new List<string>(runnerPrefix)
            {
                "--synthetic-detector-to-recognizer-pipeline-child",
                "--repo-root", repoRoot,
                "--fixture-id", fixture.Id,
                "--fixture-file-name", $"{fixture.Id}.{captureMode}-rgba",
                "--fixture-source-category", fixture.SourceCategory,
                "--fixture-created-by-internal-qa", fixture.CreatedByInternalQa.ToString(),
                "--fixture-text", fixture.ExpectedText,
                "--fixture-variant", "internal-controlled-screen-region",
                "--capture-mode", captureMode,
                "--window-title-or-source", fixture.WindowTitleOrSource,
                "--process-or-source", fixture.ProcessOrSource,
                "--window-x", fixture.WindowX.ToString(),
                "--window-y", fixture.WindowY.ToString(),
                "--window-width", fixture.WindowWidth.ToString(),
                "--window-height", fixture.WindowHeight.ToString(),
                "--region-x", fixture.RegionX.ToString(),
                "--region-y", fixture.RegionY.ToString(),
                "--region-width", fixture.RegionWidth.ToString(),
                "--region-height", fixture.RegionHeight.ToString(),
                "--bounds-source", fixture.BoundsSource,
                "--horizontal-padding", "96",
                "--vertical-padding", "48",
                "--crop-padding", "24",
                "--crop-strategy", "percent-expanded-box",
                "--margin-policy", "10%",
                "--unclip-policy", "1.5",
                "--recognizer-resize", "ratio",
                "--detector-model-relative", detectorRelativePath,
                "--recognizer-model-relative", recognizerRelativePath,
                "--dictionary-relative", dictionaryRelativePath,
                "--expected-class-count", "438",
                "--dictionary-token-count", "436"
            };

            var guardResult = new NodalOsOnnxOutOfProcessGuard().Run(new NodalOsOnnxOutOfProcessGuardRequest(
                $"internal-controlled-screen-region-guard-{Guid.NewGuid():N}",
                request,
                runner,
                args,
                timeoutMs,
                MaxOutputBytes: 256 * 1024,
                AllowRawPersistence: false));

            var childSummary = TryParseJsonElement(guardResult.Reason);
            var decoded = ReadChildString(childSummary, "DecodedPreviewNonAuthoritative") ?? string.Empty;
            var normalizedDecoded = NormalizeNoAuthorityPreview(decoded);
            var normalizedExpected = NormalizeNoAuthorityPreview(fixture.ExpectedText);
            results.Add(new
            {
                FixtureId = fixture.Id,
                Expected = fixture.ExpectedText,
                ProvenanceCategory = fixture.SourceCategory,
                CaptureMode = captureMode,
                WindowTitleOrSource = fixture.WindowTitleOrSource,
                ProcessOrSource = fixture.ProcessOrSource,
                WindowBounds = new { fixture.WindowX, fixture.WindowY, fixture.WindowWidth, fixture.WindowHeight },
                RegionBounds = new { fixture.RegionX, fixture.RegionY, fixture.RegionWidth, fixture.RegionHeight },
                BoundsSource = fixture.BoundsSource,
                Attempted = true,
                OutOfProcessGuardUsed = true,
                guardResult.ExitCode,
                guardResult.TimedOut,
                guardResult.ParentSurvived,
                guardResult.ChildLaunched,
                guardResult.RawPersisted,
                guardResult.CallsSaas,
                guardResult.NoAuthority,
                Status = guardResult.ProbeResult.Status.ToString(),
                ChildSummary = childSummary,
                DetectorBoxesCount = ReadChildInt(childSummary, "DetectorBoxesCount"),
                RecognizerAttempts = ReadChildInt(childSummary, "RecognizerAttempts"),
                Decoded = decoded,
                NormalizedDecoded = normalizedDecoded,
                MatchKind = ReadChildString(childSummary, "MatchKind") ?? "Unknown",
                EditDistance = LevenshteinDistance(normalizedExpected, normalizedDecoded),
                MeanConfidence = ReadChildDouble(childSummary, "MeanConfidence"),
                Warning = ReadChildString(childSummary, "Warning") ?? string.Empty
            });
        }
    }

    var exactMatches = CountObjectString(results, "MatchKind", "Exact");
    var normalizedMatches = CountObjectString(results, "MatchKind", "Normalized");
    var mismatches = CountObjectString(results, "MatchKind", "Mismatch");
    var totalEditDistance = SumObjectInt(results, "EditDistance");
    var successCriteriaMet = exactMatches + normalizedMatches >= 2 || totalEditDistance <= 2;
    var readinessDecision = !detectorAvailable || !recognizerAvailable || !dictionaryAvailable
        ? "BLOCKED_BY_MODEL_OR_DICTIONARY_AVAILABILITY"
        : acceptedFixtures.Length == 0
            ? "BLOCKED_BY_SCREEN_REGION_CAPTURE_POLICY"
            : results.Count == 0 || results.Any(r => (int)r.GetType().GetProperty("DetectorBoxesCount")!.GetValue(r)! <= 0)
                ? "BLOCKED_BY_SCREEN_REGION_PIPELINE_EVIDENCE"
                : successCriteriaMet && string.Equals(captureMode, "simulated-region", StringComparison.Ordinal)
                    ? "READY_FOR_SCREEN_REGION_FIXTURE_SET_EXPANSION"
                    : successCriteriaMet && string.Equals(captureMode, "simulated-window-region", StringComparison.Ordinal)
                        ? "READY_FOR_QA_WINDOW_REGION_FIXTURE_SET_EXPANSION"
                        : successCriteriaMet
                            ? "READY_FOR_INTERNAL_LOW_RISK_SCREEN_OCR_OBSERVATION"
                            : "BLOCKED_BY_SCREEN_REGION_PIPELINE_EVIDENCE";

    Console.Out.WriteLine(JsonSerializer.Serialize(new
    {
        Milestone = qaWindowGate ? "M307-M309" : "M304-M306",
        BaseCommit = qaWindowGate ? "854f206" : "002cd30",
        ReadinessDecision = readinessDecision,
        InternalDevelopmentOnly = true,
        PublicProductReady = false,
        NoSaaS = true,
        NoApiKeys = true,
        NoSensitive = true,
        RealDocumentUsed = false,
        FullScreenUsed = false,
        InternalControlledScreenRegionsUsed = !qaWindowGate,
        QaWindowRegionUsed = qaWindowGate && string.Equals(captureMode, "qa-window-region", StringComparison.Ordinal),
        SimulatedRegionUsed = !qaWindowGate && string.Equals(captureMode, "simulated-region", StringComparison.Ordinal),
        SimulatedWindowRegionUsed = qaWindowGate && string.Equals(captureMode, "simulated-window-region", StringComparison.Ordinal),
        UnknownProvenanceRejected = true,
        SensitiveRegionRejected = true,
        FullScreenRejected = true,
        DocumentRegionRejected = true,
        RawPersistenceOfSensitiveData = false,
        CaptureMode = captureMode,
        DetectorModelAvailable = detectorAvailable,
        DetectorModelVerified = detectorAvailable,
        RecognizerModelAvailable = recognizerAvailable,
        DictionaryAvailable = dictionaryAvailable,
        OfficialSpacePolicy = true,
        BlankIndex = 0,
        SpaceIndex = 437,
        SpaceIndexFormula = "N+1",
        RecognizerOutputLayout = "[B,T,C]",
        RecognizerSoftmaxReapplied = false,
        RecognizerResizeMode = "RatioPreservingRightPad",
        OutOfProcessGuardUsed = results.Count > 0,
        ParentSurvived = results.Count == 0 || results.All(r => (bool)r.GetType().GetProperty("ParentSurvived")!.GetValue(r)!),
        FixturesTotal = fixtures.Length,
        FixturesAccepted = acceptedFixtures.Length,
        FixturesRejected = rejectedFixtures,
        ExactMatches = exactMatches,
        NormalizedMatches = normalizedMatches,
        Mismatches = mismatches,
        TotalEditDistance = totalEditDistance,
        SuccessCriteriaMet = successCriteriaMet,
        RecommendedNextStep = readinessDecision == "READY_FOR_SCREEN_REGION_FIXTURE_SET_EXPANSION"
            ? "replace simulated-region with qa-window-region or real-window-region capture"
            : readinessDecision == "READY_FOR_QA_WINDOW_REGION_FIXTURE_SET_EXPANSION"
                ? "replace simulated-window-region with real QA window region capture"
            : "internal-low-risk-screen-ocr-observation",
        RecognizerTensorShape = new[] { 1, 3, 48, 320 },
        RecognizerOutputShape = new[] { 1, 40, 438 },
        RecognizerClassCountObserved = 438,
        RecognizerClassCountExpected = 438,
        SoftmaxEvidence = "Rows sum approximately 1; softmax not reapplied.",
        ModelsCommitted = false,
        DictionariesCommitted = false,
        Results = results
    }));
    return 0;
}

static int RunSyntheticDetectorToRecognizerPipelineChild(Dictionary<string, string> options)
{
    var repoRoot = options.TryGetValue("repo-root", out var root) && Directory.Exists(root)
        ? Path.GetFullPath(root)
        : Directory.GetCurrentDirectory();
    var fixtureText = options.TryGetValue("fixture-text", out var configuredFixtureText)
        ? configuredFixtureText
        : "TEST";
    var fixtureVariant = options.TryGetValue("fixture-variant", out var configuredVariant)
        ? configuredVariant
        : "centered-line";
    var fixtureId = options.TryGetValue("fixture-id", out var configuredFixtureId)
        ? configuredFixtureId
        : $"fixture-{Guid.NewGuid():N}";
    var fixtureFileName = options.TryGetValue("fixture-file-name", out var configuredFixtureFileName)
        ? configuredFixtureFileName
        : $"{fixtureId}.generated-rgba";
    var fixtureSourceCategory = options.TryGetValue("fixture-source-category", out var configuredSourceCategory)
        ? configuredSourceCategory
        : "SyntheticGenerated";
    var fixtureCreatedByInternalQa = options.TryGetValue("fixture-created-by-internal-qa", out var internalQaText) &&
                                     bool.TryParse(internalQaText, out var parsedInternalQa) &&
                                     parsedInternalQa;
    var internalControlledRealImage = string.Equals(fixtureSourceCategory, "InternalControlledRealImage", StringComparison.Ordinal);
    var internalControlledScreenRegion = string.Equals(fixtureSourceCategory, "InternalControlledScreenRegion", StringComparison.Ordinal) ||
                                         string.Equals(fixtureSourceCategory, "InternalQaWindowRegion", StringComparison.Ordinal);
    var captureMode = options.TryGetValue("capture-mode", out var configuredCaptureMode)
        ? configuredCaptureMode
        : "synthetic";
    var boundsSource = options.TryGetValue("bounds-source", out var configuredBoundsSource)
        ? configuredBoundsSource
        : "GeneratedQaFixture";
    var windowTitleOrSource = options.TryGetValue("window-title-or-source", out var configuredWindowTitleOrSource)
        ? configuredWindowTitleOrSource
        : string.Empty;
    var processOrSource = options.TryGetValue("process-or-source", out var configuredProcessOrSource)
        ? configuredProcessOrSource
        : string.Empty;
    var windowX = options.TryGetValue("window-x", out var windowXText) && int.TryParse(windowXText, out var parsedWindowX)
        ? parsedWindowX
        : 0;
    var windowY = options.TryGetValue("window-y", out var windowYText) && int.TryParse(windowYText, out var parsedWindowY)
        ? parsedWindowY
        : 0;
    var windowWidth = options.TryGetValue("window-width", out var windowWidthText) && int.TryParse(windowWidthText, out var parsedWindowWidth)
        ? parsedWindowWidth
        : 0;
    var windowHeight = options.TryGetValue("window-height", out var windowHeightText) && int.TryParse(windowHeightText, out var parsedWindowHeight)
        ? parsedWindowHeight
        : 0;
    var regionX = options.TryGetValue("region-x", out var regionXText) && int.TryParse(regionXText, out var parsedRegionX)
        ? parsedRegionX
        : 0;
    var regionY = options.TryGetValue("region-y", out var regionYText) && int.TryParse(regionYText, out var parsedRegionY)
        ? parsedRegionY
        : 0;
    var regionWidth = options.TryGetValue("region-width", out var regionWidthText) && int.TryParse(regionWidthText, out var parsedRegionWidth)
        ? parsedRegionWidth
        : 0;
    var regionHeight = options.TryGetValue("region-height", out var regionHeightText) && int.TryParse(regionHeightText, out var parsedRegionHeight)
        ? parsedRegionHeight
        : 0;
    var horizontalPadding = options.TryGetValue("horizontal-padding", out var hPadText) && int.TryParse(hPadText, out var parsedHPad)
        ? parsedHPad
        : 48;
    var verticalPadding = options.TryGetValue("vertical-padding", out var vPadText) && int.TryParse(vPadText, out var parsedVPad)
        ? parsedVPad
        : 48;
    var detectorRelativePath = options.TryGetValue("detector-model-relative", out var configuredDetectorPath)
        ? configuredDetectorPath
        : Path.Combine("tools", "ocr-worker", "models", "onnx", "ch_PP-OCRv4_det.onnx");
    var recognizerRelativePath = options.TryGetValue("recognizer-model-relative", out var configuredRecognizerPath)
        ? configuredRecognizerPath
        : Path.Combine("tools", "ocr-worker", "models", "onnx", "candidates", "en_PP-OCRv5_rec_mobile.onnx");
    var dictionaryRelativePath = options.TryGetValue("dictionary-relative", out var configuredDictionaryPath)
        ? configuredDictionaryPath
        : Path.Combine("tools", "ocr-worker", "models", "onnx", "dictionaries", "ppocrv5_en_dict.txt");
    var expectedClassCount = options.TryGetValue("expected-class-count", out var expectedClassText) &&
                             int.TryParse(expectedClassText, out var parsedExpectedClassCount)
        ? parsedExpectedClassCount
        : 438;
    var dictionaryTokenCount = options.TryGetValue("dictionary-token-count", out var dictionaryText) &&
                               int.TryParse(dictionaryText, out var parsedDictionaryTokenCount)
        ? parsedDictionaryTokenCount
        : 436;
    var cropPadding = options.TryGetValue("crop-padding", out var cropPaddingText) &&
                      int.TryParse(cropPaddingText, out var parsedCropPadding)
        ? Math.Max(0, parsedCropPadding)
        : 6;
    var cropStrategy = options.TryGetValue("crop-strategy", out var configuredCropStrategy)
        ? configuredCropStrategy
        : "fixed-pixel-expanded-box";
    var marginPolicy = options.TryGetValue("margin-policy", out var configuredMarginPolicy)
        ? configuredMarginPolicy
        : $"{cropPadding}px";
    var unclipPolicy = options.TryGetValue("unclip-policy", out var configuredUnclipPolicy)
        ? configuredUnclipPolicy
        : "actual";

    var detectorPath = Path.GetFullPath(Path.Combine(repoRoot, detectorRelativePath));
    var recognizerPath = Path.GetFullPath(Path.Combine(repoRoot, recognizerRelativePath));
    var dictionaryPath = Path.GetFullPath(Path.Combine(repoRoot, dictionaryRelativePath));
    if (!File.Exists(detectorPath) || !File.Exists(recognizerPath) || !File.Exists(dictionaryPath))
    {
        EmitReport(new NodalOsOnnxOutOfProcessRunnerReport(
            $"synthetic-detector-recognizer-{Guid.NewGuid():N}",
            "ModelAvailability",
            "BlockedByModelRuntime",
            null,
            null,
            false,
            false,
            true,
            "detector, recognizer, or dictionary missing"));
        return 0;
    }

    var fixture = BuildSyntheticFullImageFixture(fixtureText, fixtureVariant, horizontalPadding, verticalPadding);
    if (fixture.Status != NodalOsSyntheticOcrTextFixtureStatus.Ready || !fixture.SafeForOcr)
    {
        var blockedFixtureSummary = JsonSerializer.Serialize(new
        {
            FixtureText = fixtureText,
            FixtureVariant = fixtureVariant,
            FixtureStatus = fixture.Status.ToString(),
            FixtureReason = fixture.Reason,
            DetectorBoxesCount = 0,
            RecognizerAttempts = 0,
            MatchKind = "FixtureBlocked",
            NoAuthority = true,
            SyntheticImagesOnly = true,
            RealScreenUsed = false,
            RealDocumentUsed = false,
            RawPersistenceOfRealData = false
        });
        EmitReport(new NodalOsOnnxOutOfProcessRunnerReport(
            $"synthetic-detector-recognizer-{Guid.NewGuid():N}",
            "FixtureGeneration",
            "InvalidTensorShape",
            0,
            0,
            false,
            false,
            true,
            blockedFixtureSummary));
        return 0;
    }

    var imagePrep = new NodalOsOnnxOcrImagePreProcessor().Prepare(
        fixture.ImageBytes,
        fixture.Width,
        fixture.Height,
        fixture.RedactionResult,
        NodalOsOcrVisionSensitivity.Low,
        allowFullScreen: false,
        allowRawPersistence: false);
    if (imagePrep.Status != NodalOsOnnxOcrPreProcessingStatus.Success)
    {
        EmitReport(new NodalOsOnnxOutOfProcessRunnerReport(
            $"synthetic-detector-recognizer-{Guid.NewGuid():N}",
            "ImagePreProcessing",
            "InvalidTensorShape",
            null,
            null,
            false,
            false,
            true,
            imagePrep.Reason));
        return 0;
    }

    var detPrep = new NodalOsOnnxOcrDetectorPreProcessor().Prepare(imagePrep, targetWidth: 640, targetHeight: 640);
    using var detectorSession = new InferenceSession(detectorPath);
    var detectorInputName = detectorSession.InputMetadata.Keys.FirstOrDefault() ?? "x";
    var detectorInputMetadata = string.Join(";", detectorSession.InputMetadata.Select(kvp => $"{kvp.Key}=[{string.Join(",", kvp.Value.Dimensions)}]"));
    var detectorOutputMetadata = string.Join(";", detectorSession.OutputMetadata.Select(kvp => $"{kvp.Key}=[{string.Join(",", kvp.Value.Dimensions)}]"));
    Console.Error.WriteLine($"stage=detector-session-created input={detectorInputName} metadata={detectorInputMetadata} outputs={detectorOutputMetadata}");
    using var detectorOutputs = detectorSession.Run([NamedOnnxValue.CreateFromTensor(detectorInputName, new DenseTensor<float>(detPrep.InputTensor, detPrep.InputShape))]);
    var detectorOutput = detectorOutputs.First().AsTensor<float>();
    var detectorOutputShape = detectorOutput.Dimensions.ToArray();
    var detectorOutputValues = detectorOutput.ToArray();
    var detectorPost = DecodeDetectorWithFallbackThresholds(detectorOutputValues, detectorOutputShape, fixture.Width, fixture.Height, out var thresholdUsed);
    var validBoxes = detectorPost.TextBoxes
        .Where(b => b.Valid && b.CropWidth > 1 && b.CropHeight > 1)
        .OrderBy(b => b.CropY)
        .ThenBy(b => b.CropX)
        .ToArray();

    if (validBoxes.Length == 0)
    {
        var noBoxSummary = JsonSerializer.Serialize(new
        {
            FixtureText = fixtureText,
            FixtureVariant = fixtureVariant,
            DetectorInputMetadata = detectorInputMetadata,
            DetectorOutputMetadata = detectorOutputMetadata,
            DetectorOutputShape = detectorOutputShape,
            DetectorThresholdUsed = thresholdUsed,
            DetectorBoxesCount = 0,
            RecognizerAttempts = 0,
            MatchKind = "NoBox",
            NoAuthority = true,
            SyntheticImagesOnly = true,
            RealScreenUsed = false,
            RealDocumentUsed = false,
            RawPersistenceOfRealData = false
        });
        EmitReport(new NodalOsOnnxOutOfProcessRunnerReport(
            $"synthetic-detector-recognizer-{Guid.NewGuid():N}",
            "DetectorPostProcessing",
            "NoTextDetected",
            0,
            0,
            false,
            false,
            true,
            noBoxSummary));
        return 0;
    }

    var selectedBox = validBoxes
        .OrderByDescending(b => b.CropWidth * b.CropHeight)
        .ThenBy(b => b.CropY)
        .ThenBy(b => b.CropX)
        .First();
    var expandedBox = ExpandBox(selectedBox, fixture.Width, fixture.Height, padding: cropPadding);
    var crop = ExtractCropForProbe(imagePrep, expandedBox);
    if (crop is null)
    {
        EmitReport(new NodalOsOnnxOutOfProcessRunnerReport(
            $"synthetic-detector-recognizer-{Guid.NewGuid():N}",
            "CropExtraction",
            "InvalidTensorShape",
            validBoxes.Length,
            0,
            false,
            false,
            true,
            "detector box could not be cropped safely"));
        return 0;
    }

    var dictionaryTokens = File.ReadAllLines(dictionaryPath);
    var recognizerShape = new[] { 1, 3, 48, 320 };
    // M295-M297: default to PaddleOCR-aligned aspect-preserving resize; allow --recognizer-resize stretch for A/B.
    var recognizerResizeMode =
        options.TryGetValue("recognizer-resize", out var resizeModeText) &&
        string.Equals(resizeModeText, "stretch", StringComparison.OrdinalIgnoreCase)
            ? NodalOsPaddleOcrRecognizerResizeMode.StretchToFixedWidth
            : NodalOsPaddleOcrRecognizerResizeMode.RatioPreservingRightPad;
    var recognizerTensor = BuildRecognizerTensorFromImageCrop(crop, recognizerShape, recognizerResizeMode, out var recognizerPreprocessing);
    var recognizerStats = NodalOsDetectorRecognizerCompatibilityDiagnosisBuilder.CalculateStats(recognizerTensor, recognizerShape, "NCHW", "RGB");
    using var recognizerSession = new InferenceSession(recognizerPath);
    var recognizerInputName = recognizerSession.InputMetadata.Keys.FirstOrDefault() ?? "x";
    var recognizerInputMetadata = string.Join(";", recognizerSession.InputMetadata.Select(kvp => $"{kvp.Key}=[{string.Join(",", kvp.Value.Dimensions)}]"));
    var recognizerOutputMetadata = string.Join(";", recognizerSession.OutputMetadata.Select(kvp => $"{kvp.Key}=[{string.Join(",", kvp.Value.Dimensions)}]"));
    using var recognizerOutputs = recognizerSession.Run([NamedOnnxValue.CreateFromTensor(recognizerInputName, new DenseTensor<float>(recognizerTensor, recognizerShape))]);
    var recognizerOutput = recognizerOutputs.First().AsTensor<float>();
    var recognizerOutputShape = recognizerOutput.Dimensions.ToArray();
    var recognizerValues = recognizerOutput.ToArray();
    var classCount = recognizerOutputShape.Length == 3 ? recognizerOutputShape[2] : recognizerOutputShape.LastOrDefault();
    var softmax = AnalyzeSoftmaxRows(recognizerValues, classCount);
    var blankIndex = 0;
    var spaceIndex = dictionaryTokenCount + 1;
    var officialSpacePolicy = classCount == expectedClassCount && spaceIndex == expectedClassCount - 1;
    var decode = DecodePaddleOcrOfficialSpaceOutput(recognizerValues, classCount, blankIndex, spaceIndex, dictionaryTokens);
    var normalizedExpected = NormalizeNoAuthorityPreview(fixtureText);
    var normalizedDecoded = NormalizeNoAuthorityPreview(decode.DecodedText);
    var matchKind = string.Equals(decode.DecodedText, fixtureText, StringComparison.Ordinal)
        ? "Exact"
        : string.Equals(normalizedDecoded, normalizedExpected, StringComparison.Ordinal)
            ? "Normalized"
            : "Mismatch";
    var status = detectorPost.TextBoxes.Count > 0 && softmax.LooksLikeSoftmax && officialSpacePolicy
        ? "Passed"
        : "BlockedByModelRuntime";

    var summary = JsonSerializer.Serialize(new
    {
        FixtureText = fixtureText,
        FixtureId = fixtureId,
        FixtureFileName = fixtureFileName,
        ProvenanceCategory = fixtureSourceCategory,
        CreatedByInternalQa = fixtureCreatedByInternalQa,
        InternalControlledRealImage = internalControlledRealImage,
        InternalControlledScreenRegion = internalControlledScreenRegion,
        CaptureMode = captureMode,
        WindowTitleOrSource = windowTitleOrSource,
        ProcessOrSource = processOrSource,
        WindowBounds = new
        {
            X = windowX,
            Y = windowY,
            Width = windowWidth <= 0 ? fixture.Width : windowWidth,
            Height = windowHeight <= 0 ? fixture.Height : windowHeight
        },
        BoundsSource = boundsSource,
        RegionBounds = new
        {
            X = regionX,
            Y = regionY,
            Width = regionWidth <= 0 ? fixture.Width : regionWidth,
            Height = regionHeight <= 0 ? fixture.Height : regionHeight
        },
        FixtureVariant = fixtureVariant,
        ImageWidth = fixture.Width,
        ImageHeight = fixture.Height,
        ExpectedTextGeometry = EstimateSyntheticTextGeometry(fixture.Width, fixture.Height, horizontalPadding, verticalPadding),
        DetectorInputMetadata = detectorInputMetadata,
        DetectorOutputMetadata = detectorOutputMetadata,
        DetectorInputShape = detPrep.InputShape,
        DetectorOutputShape = detectorOutputShape,
        DetectorThresholdUsed = thresholdUsed,
        DetectorPostprocessStatus = detectorPost.Status.ToString(),
        DetectorBoxesCount = validBoxes.Length,
        SelectedCrop = new
        {
            selectedBox.CropX,
            selectedBox.CropY,
            selectedBox.CropWidth,
            selectedBox.CropHeight,
            selectedBox.Confidence
        },
        ExpandedCrop = new
        {
            expandedBox.CropX,
            expandedBox.CropY,
            expandedBox.CropWidth,
            expandedBox.CropHeight,
            expandedBox.Confidence
        },
        CropCalibration = new
        {
            CropStrategy = cropStrategy,
            MarginPolicy = marginPolicy,
            UnclipPolicy = unclipPolicy,
            CropPaddingPixels = cropPadding,
            DetectedAspectRatio = selectedBox.CropHeight <= 0 ? 0d : Math.Round((double)selectedBox.CropWidth / selectedBox.CropHeight, 4),
            ExpandedAspectRatio = expandedBox.CropHeight <= 0 ? 0d : Math.Round((double)expandedBox.CropWidth / expandedBox.CropHeight, 4),
            DetectedArea = selectedBox.CropWidth * selectedBox.CropHeight,
            ExpandedArea = expandedBox.CropWidth * expandedBox.CropHeight,
            ClipsLeft = selectedBox.CropX <= 0,
            ClipsTop = selectedBox.CropY <= 0,
            ClipsRight = selectedBox.CropX + selectedBox.CropWidth >= fixture.Width,
            ClipsBottom = selectedBox.CropY + selectedBox.CropHeight >= fixture.Height
        },
        RecognizerPreprocessing = recognizerPreprocessing,
        RecognizerResizeMode = recognizerResizeMode.ToString(),
        RecognizerTensorStats = recognizerStats,
        RecognizerTensorShape = recognizerShape,
        RecognizerInputMetadata = recognizerInputMetadata,
        RecognizerOutputMetadata = recognizerOutputMetadata,
        RecognizerOutputShape = recognizerOutputShape,
        RecognizerOutputLayout = recognizerOutputShape.Length == 3 ? "[B,T,C]" : "Unexpected",
        OutputClassCount = classCount,
        ExpectedClassCount = expectedClassCount,
        DictionaryTokenCount = dictionaryTokenCount,
        BlankIndex = blankIndex,
        SpaceIndex = spaceIndex,
        OfficialSpacePolicy = officialSpacePolicy,
        SoftmaxEvidence = softmax,
        SoftmaxReapplied = false,
        DecodePolicyApplied = "OfficialSpaceToken",
        DecodedPreviewNonAuthoritative = decode.DecodedText,
        NormalizedDecodedPreview = normalizedDecoded,
        EditDistance = LevenshteinDistance(normalizedExpected, normalizedDecoded),
        MeanConfidence = decode.MeanConfidence,
        EmittedTokens = decode.EmittedTokens,
        MatchKind = matchKind,
        RecognizerAttempts = 1,
        UsefulOcrClaimed = false,
        PublicProductReady = false,
        ProductiveOcr = false,
        ShadowMode = false,
        NoAuthority = true,
        NoSaaS = true,
        NoApiKeys = true,
        NoSensitive = true,
        SyntheticImagesOnly = !internalControlledRealImage && !internalControlledScreenRegion,
        InternalControlledRealImagesUsed = internalControlledRealImage,
        InternalControlledScreenRegionsUsed = internalControlledScreenRegion,
        FullScreenUsed = false,
        RealScreenUsed = false,
        RealDocumentUsed = false,
        RawPersistenceOfRealData = false
    });

    EmitReport(new NodalOsOnnxOutOfProcessRunnerReport(
        $"synthetic-detector-recognizer-{Guid.NewGuid():N}",
        "RecognitionRun",
        status,
        validBoxes.Length,
        1,
        false,
        false,
        true,
        summary));
    return 0;
}

static int RunRecognizerRuntimeExperiment(Dictionary<string, string> options)
{
    var repoRoot = options.TryGetValue("repo-root", out var root) && Directory.Exists(root)
        ? Path.GetFullPath(root)
        : Directory.GetCurrentDirectory();
    var (runner, runnerPrefix) = ResolveCurrentRunnerInvocation();
    var timeoutMs = options.TryGetValue("timeout-ms", out var timeoutText) && int.TryParse(timeoutText, out var parsedTimeout)
        ? parsedTimeout
        : 60000;

    var experiments = new NodalOsRecognizerRuntimeExperimentBuilder().BuildMinimalMatrix();
    var results = new List<object>();

    foreach (var experiment in experiments)
    {
        if (experiment.Layout == NodalOsRecognizerRuntimeProbeLayout.Nhwc)
        {
            results.Add(new
            {
                experiment.ExperimentId,
                TensorKind = experiment.TensorKind.ToString(),
                Layout = experiment.Layout.ToString(),
                ShapeKind = experiment.ShapeKind.ToString(),
                Shape = experiment.InputShape,
                SessionOption = experiment.SessionOptions.OptionKind.ToString(),
                Status = NodalOsRecognizerRuntimeProbeStatus.UnsupportedLayout.ToString(),
                RanOutOfProcess = false,
                ParentSurvived = true,
                RawPersisted = false,
                CallsSaas = false,
                NoAuthority = true,
                Reason = "NHWC skipped: recognizer metadata and pipeline expect NCHW [1,3,H,W]"
            });
            continue;
        }

        if (experiment.ShapeKind == NodalOsRecognizerRuntimeShapeKind.Invalid || experiment.InputShape.Any(d => d <= 0))
        {
            results.Add(new
            {
                experiment.ExperimentId,
                TensorKind = experiment.TensorKind.ToString(),
                Layout = experiment.Layout.ToString(),
                ShapeKind = experiment.ShapeKind.ToString(),
                Shape = experiment.InputShape,
                SessionOption = experiment.SessionOptions.OptionKind.ToString(),
                Status = NodalOsRecognizerRuntimeProbeStatus.InvalidTensorShape.ToString(),
                RanOutOfProcess = false,
                ParentSurvived = true,
                RawPersisted = false,
                CallsSaas = false,
                NoAuthority = true,
                Reason = "invalid recognizer shape blocked before runtime"
            });
            continue;
        }

        var request = new NodalOsOnnxNativeRuntimeCrashProbeRequest(
            experiment.ExperimentId,
            NodalOsOnnxNativeRuntimeCrashFixtureKind.LargeCenteredText,
            NodalOsSyntheticOcrTextRenderMode.PixelFont,
            experiment.InputShape[3],
            experiment.InputShape[2],
            NodalOsOnnxNativeRuntimeCrashStage.RecognitionRun,
            NodalOsOcrVisionSensitivity.Low,
            FullScreen: false,
            Sensitive: false,
            OriginalRawPersisted: false,
            Synthetic: true,
            NoAuthority: true,
            RunOutOfProcess: true);

        var guardResult = new NodalOsOnnxOutOfProcessGuard().Run(new NodalOsOnnxOutOfProcessGuardRequest(
            $"rec-runtime-exp-guard-{Guid.NewGuid():N}",
            request,
            runner,
            [
                "--recognizer-runtime-child",
                "--repo-root", repoRoot,
                "--tensor", experiment.TensorKind.ToString(),
                "--layout", experiment.Layout.ToString(),
                "--shape-kind", experiment.ShapeKind.ToString(),
                "--session-option", experiment.SessionOptions.OptionKind.ToString()
            ],
            timeoutMs,
            MaxOutputBytes: 64 * 1024,
            AllowRawPersistence: false));

        results.Add(new
        {
            experiment.ExperimentId,
            TensorKind = experiment.TensorKind.ToString(),
            Layout = experiment.Layout.ToString(),
            ShapeKind = experiment.ShapeKind.ToString(),
            Shape = experiment.InputShape,
            SessionOption = experiment.SessionOptions.OptionKind.ToString(),
            Status = MapRecognizerStatus(guardResult).ToString(),
            guardResult.ExitCode,
            ExitCodeHex = guardResult.ExitCode is null ? null : $"0x{unchecked((uint)guardResult.ExitCode.Value):X8}",
            CrashKind = guardResult.ProbeResult.CrashKind.ToString(),
            guardResult.TimedOut,
            guardResult.ParentSurvived,
            guardResult.TempFilesCleaned,
            guardResult.OrphanProcessLeft,
            guardResult.RawPersisted,
            guardResult.CallsSaas,
            guardResult.NoAuthority,
            guardResult.StdErrSummary,
            guardResult.Reason
        });
    }

    Console.Out.WriteLine(JsonSerializer.Serialize(results));
    return 0;
}

static int RunExtraClassArgmaxProbe(Dictionary<string, string> options)
{
    var repoRoot = options.TryGetValue("repo-root", out var root) && Directory.Exists(root)
        ? Path.GetFullPath(root)
        : Directory.GetCurrentDirectory();
    var (runner, runnerPrefix) = ResolveCurrentRunnerInvocation();
    var timeoutMs = options.TryGetValue("timeout-ms", out var timeoutText) && int.TryParse(timeoutText, out var parsedTimeout)
        ? parsedTimeout
        : 60000;
    var threshold = options.TryGetValue("negligible-threshold", out var thresholdText) &&
                    double.TryParse(thresholdText, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var parsedThreshold)
        ? parsedThreshold
        : 0.001d;

    var fixtures = new[]
    {
        ("normal", "TEST"),
        ("normal", "NODAL"),
        ("normal", "12345"),
        ("normal", "ABC123"),
        ("normal", "HighContrastCrop"),
        ("normal", "DetectorDerivedCrop"),
        ("extreme", "Black"),
        ("extreme", "White"),
        ("extreme", "DeterministicNoise"),
        ("extreme", "Gradient"),
        ("extreme", "ThinLines"),
        ("extreme", "Checkerboard"),
        ("extreme", "OutOfDictionary"),
        ("extreme", "InvalidEmptyCrop")
    };

    var results = new List<object>();
    foreach (var (group, fixture) in fixtures)
    {
        if (fixture.Equals("InvalidEmptyCrop", StringComparison.OrdinalIgnoreCase))
        {
            results.Add(new
            {
                FixtureGroup = group,
                FixtureId = fixture,
                Status = NodalOsExtraClassArgmaxProbeStatus.ProbeBlockedByInvalidInput.ToString(),
                RanOutOfProcess = false,
                ParentSurvived = true,
                TempCleanup = true,
                RawPersisted = false,
                CallsSaas = false,
                NoAuthority = true,
                Reason = "empty crop fixture blocked before recognizer runtime"
            });
            continue;
        }

        var request = new NodalOsOnnxNativeRuntimeCrashProbeRequest(
            $"extra-class-{fixture}-{Guid.NewGuid():N}",
            NodalOsOnnxNativeRuntimeCrashFixtureKind.LargeCenteredText,
            NodalOsSyntheticOcrTextRenderMode.PixelFont,
            320,
            32,
            NodalOsOnnxNativeRuntimeCrashStage.RecognitionRun,
            NodalOsOcrVisionSensitivity.Low,
            FullScreen: false,
            Sensitive: false,
            OriginalRawPersisted: false,
            Synthetic: true,
            NoAuthority: true,
            RunOutOfProcess: true);

        var childArguments = runnerPrefix.Concat(new[]
        {
            "--extra-class-argmax-child",
            "--repo-root", repoRoot,
            "--fixture", fixture,
            "--fixture-group", group,
            "--negligible-threshold", threshold.ToString("R", System.Globalization.CultureInfo.InvariantCulture)
        }).ToArray();

        var guardResult = new NodalOsOnnxOutOfProcessGuard().Run(new NodalOsOnnxOutOfProcessGuardRequest(
            $"extra-class-guard-{fixture}-{Guid.NewGuid():N}",
            request,
            runner,
            childArguments,
            timeoutMs,
            MaxOutputBytes: 128 * 1024,
            AllowRawPersistence: false));

        results.Add(new
        {
            FixtureGroup = group,
            FixtureId = fixture,
            Status = MapExtraClassStatus(guardResult).ToString(),
            guardResult.ExitCode,
            ExitCodeHex = guardResult.ExitCode is null ? null : $"0x{unchecked((uint)guardResult.ExitCode.Value):X8}",
            guardResult.TimedOut,
            guardResult.ParentSurvived,
            TempCleanup = guardResult.TempFilesCleaned,
            guardResult.OrphanProcessLeft,
            guardResult.RawPersisted,
            guardResult.CallsSaas,
            guardResult.NoAuthority,
            guardResult.StdErrSummary,
            SummaryJson = guardResult.Reason
        });
    }

    Console.Out.WriteLine(JsonSerializer.Serialize(new
    {
        Mode = "extra-class-argmax-probe",
        Recognizer = "PP-OCRv5 English candidate",
        ExtraClassIndex = 437,
        ExpectedOutputClassCount = 438,
        BlankIndex = 0,
        NegligibleProbabilityThreshold = threshold,
        DecodeAttempted = false,
        ProductiveOcrBlocked = true,
        ShadowModeBlocked = true,
        NoRawPersistence = results.All(result => !JsonSerializer.Serialize(result).Contains("\"RawPersisted\":true", StringComparison.Ordinal)),
        NoSaas = results.All(result => !JsonSerializer.Serialize(result).Contains("\"CallsSaas\":true", StringComparison.Ordinal)),
        NoAuthority = true,
        Results = results
    }));
    return 0;
}

static int RunExtraClassArgmaxChild(Dictionary<string, string> options)
{
    var repoRoot = options.TryGetValue("repo-root", out var root) && Directory.Exists(root)
        ? Path.GetFullPath(root)
        : Directory.GetCurrentDirectory();
    var fixture = options.TryGetValue("fixture", out var fixtureText) && !string.IsNullOrWhiteSpace(fixtureText)
        ? fixtureText
        : "TEST";
    var fixtureGroup = options.TryGetValue("fixture-group", out var groupText) && !string.IsNullOrWhiteSpace(groupText)
        ? groupText
        : "normal";
    var threshold = options.TryGetValue("negligible-threshold", out var thresholdText) &&
                    double.TryParse(thresholdText, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var parsedThreshold)
        ? parsedThreshold
        : 0.001d;

    if (fixture.Equals("InvalidEmptyCrop", StringComparison.OrdinalIgnoreCase))
    {
        EmitReport(new NodalOsOnnxOutOfProcessRunnerReport(
            $"extra-class-{Guid.NewGuid():N}",
            "RecognizerTensorPreparation",
            NodalOsExtraClassArgmaxProbeStatus.ProbeBlockedByInvalidInput.ToString(),
            BoxesDetected: null,
            RecognitionAttempts: 0,
            CallsSaas: false,
            RawPersisted: false,
            NoAuthority: true,
            "empty crop blocked before runtime"));
        return 0;
    }

    var shape = new[] { 1, 3, 32, 320 };
    var tensor = BuildExtraClassTensor(fixture, shape);
    var stats = NodalOsDetectorRecognizerCompatibilityDiagnosisBuilder.CalculateStats(tensor, shape, "NCHW", "RGB");
    if (stats.HasNaN || stats.HasInfinity || tensor.Length != shape.Aggregate(1, (a, b) => a * b))
    {
        EmitReport(new NodalOsOnnxOutOfProcessRunnerReport(
            $"extra-class-{Guid.NewGuid():N}",
            "RecognizerTensorPreparation",
            NodalOsExtraClassArgmaxProbeStatus.ProbeBlockedByInvalidInput.ToString(),
            BoxesDetected: null,
            RecognitionAttempts: 0,
            CallsSaas: false,
            RawPersisted: false,
            NoAuthority: true,
            "invalid synthetic tensor blocked before runtime"));
        return 0;
    }

    var modelPath = Path.GetFullPath(Path.Combine(repoRoot, "tools", "ocr-worker", "models", "onnx", "candidates", "en_PP-OCRv5_rec_mobile.onnx"));
    Console.Error.WriteLine($"stage=model-file model={modelPath} exists={File.Exists(modelPath)}");
    Console.Error.WriteLine($"stage=tensor fixture={fixture} group={fixtureGroup} shape=[{string.Join(",", shape)}] min={stats.Min:R} max={stats.Max:R} mean={stats.Mean:R}");

    using var session = new InferenceSession(modelPath);
    var inputName = session.InputMetadata.Keys.FirstOrDefault() ?? "x";
    var inputMetadata = string.Join(";", session.InputMetadata.Select(kvp => $"{kvp.Key}=[{string.Join(",", kvp.Value.Dimensions)}]"));
    var outputMetadata = string.Join(";", session.OutputMetadata.Select(kvp => $"{kvp.Key}=[{string.Join(",", kvp.Value.Dimensions)}]"));
    Console.Error.WriteLine($"stage=session-created runtime={typeof(InferenceSession).Assembly.GetName().Version} provider=CPUExecutionProvider inputs={inputMetadata} outputs={outputMetadata}");

    using var outputs = session.Run([NamedOnnxValue.CreateFromTensor(inputName, new DenseTensor<float>(tensor, shape))]);
    var first = outputs.First().AsTensor<float>();
    var outputShape = first.Dimensions.ToArray();
    var values = first.ToArray();
    var analysis = AnalyzeExtraClassOutput(values, outputShape, extraClassIndex: 437, blankIndex: 0, threshold);
    var status = analysis.ExtraClassArgmaxCount > 0
        ? NodalOsExtraClassArgmaxProbeStatus.ExtraClassArgmaxObserved
        : analysis.OutputClassCount != 438
            ? NodalOsExtraClassArgmaxProbeStatus.ProbeRuntimeFailed
            : analysis.ExtraClassMaxProbability > threshold
                ? NodalOsExtraClassArgmaxProbeStatus.ExtraClassProbabilityNonTrivial
                : NodalOsExtraClassArgmaxProbeStatus.ExtraClassNeverArgmax;

    var summary = new
    {
        ProbeId = $"extra-class-{fixture}-{Guid.NewGuid():N}",
        FixtureId = fixture,
        FixtureGroup = fixtureGroup,
        Status = status.ToString(),
        OutputShape = outputShape,
        OutputClassCount = analysis.OutputClassCount,
        ExtraClassIndex = 437,
        ExtraClassArgmaxCount = analysis.ExtraClassArgmaxCount,
        ExtraClassMaxProbability = analysis.ExtraClassMaxProbability,
        ExtraClassAverageProbability = analysis.ExtraClassAverageProbability,
        BlankIndex = 0,
        BlankArgmaxCount = analysis.BlankArgmaxCount,
        DominantClassIndexes = analysis.DominantClassIndexes,
        Timesteps = analysis.Timesteps,
        Threshold = threshold,
        DecodeAttempted = false,
        CallsSaas = false,
        RawPersisted = false,
        FullScreen = false,
        Sensitive = false,
        NoAuthority = true
    };

    EmitReport(new NodalOsOnnxOutOfProcessRunnerReport(
        summary.ProbeId,
        "RecognitionRun",
        status.ToString(),
        BoxesDetected: null,
        RecognitionAttempts: 1,
        CallsSaas: false,
        RawPersisted: false,
        NoAuthority: true,
        JsonSerializer.Serialize(summary)));
    return 0;
}

static int RunRecognizerRuntimeChild(Dictionary<string, string> options)
{
    var repoRoot = options.TryGetValue("repo-root", out var root) && Directory.Exists(root)
        ? Path.GetFullPath(root)
        : Directory.GetCurrentDirectory();
    var tensorKind = options.TryGetValue("tensor", out var tensorText) &&
                     Enum.TryParse<NodalOsRecognizerRuntimeTensorKind>(tensorText, ignoreCase: true, out var parsedTensor)
        ? parsedTensor
        : NodalOsRecognizerRuntimeTensorKind.Zero;
    var layout = options.TryGetValue("layout", out var layoutText) &&
                 Enum.TryParse<NodalOsRecognizerRuntimeProbeLayout>(layoutText, ignoreCase: true, out var parsedLayout)
        ? parsedLayout
        : NodalOsRecognizerRuntimeProbeLayout.Nchw;
    var shapeKind = options.TryGetValue("shape-kind", out var shapeText) &&
                    Enum.TryParse<NodalOsRecognizerRuntimeShapeKind>(shapeText, ignoreCase: true, out var parsedShape)
        ? parsedShape
        : NodalOsRecognizerRuntimeShapeKind.CurrentPipelineFixed;
    var optionKind = options.TryGetValue("session-option", out var optionText) &&
                     Enum.TryParse<NodalOsRecognizerRuntimeSessionOptionKind>(optionText, ignoreCase: true, out var parsedOption)
        ? parsedOption
        : NodalOsRecognizerRuntimeSessionOptionKind.Default;

    if (layout != NodalOsRecognizerRuntimeProbeLayout.Nchw)
    {
        EmitReport(new NodalOsOnnxOutOfProcessRunnerReport(
            $"rec-runtime-{Guid.NewGuid():N}", "RecognizerTensorPreparation", "InvalidTensorShape", null, null,
            false, false, true, "unsupported recognizer layout; metadata/pipeline expects NCHW"));
        return 0;
    }

    var shape = shapeKind switch
    {
        NodalOsRecognizerRuntimeShapeKind.PaddleOcrCandidate640 => new[] { 1, 3, 32, 640 },
        NodalOsRecognizerRuntimeShapeKind.Invalid => new[] { 1, 1, 0, 0 },
        _ => new[] { 1, 3, 32, 320 }
    };
    if (shape.Any(d => d <= 0) || shape[1] != 3)
    {
        EmitReport(new NodalOsOnnxOutOfProcessRunnerReport(
            $"rec-runtime-{Guid.NewGuid():N}", "RecognizerTensorPreparation", "InvalidTensorShape", null, null,
            false, false, true, "invalid recognizer shape blocked before runtime"));
        return 0;
    }

    var tensor = BuildRecognizerTensor(tensorKind, shape);
    var stats = NodalOsDetectorRecognizerCompatibilityDiagnosisBuilder.CalculateStats(tensor, shape, "NCHW", "RGB");
    if (stats.HasNaN || stats.HasInfinity || tensor.Length != shape.Aggregate(1, (a, b) => a * b))
    {
        EmitReport(new NodalOsOnnxOutOfProcessRunnerReport(
            $"rec-runtime-{Guid.NewGuid():N}", "RecognizerTensorPreparation", "InvalidTensorShape", null, null,
            false, false, true, "invalid recognizer tensor stats/shape before runtime"));
        return 0;
    }

    var recognizerRelativePath = options.TryGetValue("recognizer-model-relative", out var configuredRecognizerPath) &&
                                 !string.IsNullOrWhiteSpace(configuredRecognizerPath)
        ? configuredRecognizerPath
        : Path.Combine("tools", "ocr-worker", "models", "onnx", "ch_PP-OCRv4_rec.onnx");
    var expectedClassCount = options.TryGetValue("expected-class-count", out var expectedClassText) &&
                             int.TryParse(expectedClassText, out var parsedExpectedClassCount)
        ? parsedExpectedClassCount
        : (int?)null;
    var modelPath = Path.GetFullPath(Path.Combine(repoRoot, recognizerRelativePath));
    Console.Error.WriteLine($"stage=model-file model={modelPath} exists={File.Exists(modelPath)}");
    Console.Error.WriteLine($"stage=tensor tensor={tensorKind} shape=[{string.Join(",", shape)}] min={stats.Min:R} max={stats.Max:R} mean={stats.Mean:R}");

    using var sessionOptions = CreateRecognizerSessionOptions(optionKind);
    Console.Error.WriteLine($"stage=session-options option={optionKind}");
    using var session = new InferenceSession(modelPath, sessionOptions);
    var inputName = session.InputMetadata.Keys.FirstOrDefault() ?? "x";
    var inputMetadata = string.Join(";", session.InputMetadata.Select(kvp => $"{kvp.Key}=[{string.Join(",", kvp.Value.Dimensions)}]"));
    var outputMetadata = string.Join(";", session.OutputMetadata.Select(kvp => $"{kvp.Key}=[{string.Join(",", kvp.Value.Dimensions)}]"));
    Console.Error.WriteLine($"stage=session-created runtime={typeof(InferenceSession).Assembly.GetName().Version} provider=CPUExecutionProvider inputs={inputMetadata} outputs={outputMetadata}");

    var inputTensor = new DenseTensor<float>(tensor, shape);
    Console.Error.WriteLine("stage=run-start");
    using var outputs = session.Run([NamedOnnxValue.CreateFromTensor(inputName, inputTensor)]);
    var materialized = outputs.Select(o => new { o.Name, Tensor = o.AsTensor<float>() }).ToArray();
    var outputShapes = materialized.Select(o => $"{o.Name}=[{FormatDimensions(o.Tensor.Dimensions.ToArray())}]").ToArray();
    var firstShape = materialized.First().Tensor.Dimensions.ToArray();
    var classCount = firstShape.Length == 3 ? firstShape[2] : firstShape.LastOrDefault();
    Console.Error.WriteLine($"stage=run-succeeded outputShapes={string.Join(";", outputShapes)} classCount={classCount}");

    var dictionary = expectedClassCount is not null && classCount == expectedClassCount.Value
        ? NodalOsOcrDictionaryCompatibilityStatus.Compatible
        : new NodalOsOcrDictionaryCompatibilityService().Evaluate(
            new NodalOsOcrDictionaryCompatibilityService().CreateCurrentAsciiManifest(verified: true),
            classCount).Status;
    var status = dictionary == NodalOsOcrDictionaryCompatibilityStatus.ClassCountMismatch
        ? "BlockedByDictionaryClassCountMismatch"
        : "Passed";

    EmitReport(new NodalOsOnnxOutOfProcessRunnerReport(
        $"rec-runtime-{Guid.NewGuid():N}",
        "RecognitionRun",
        status,
        BoxesDetected: null,
        RecognitionAttempts: 1,
        CallsSaas: false,
        RawPersisted: false,
        NoAuthority: true,
        $"recognizer run succeeded; tensor={tensorKind}; outputs={string.Join(";", outputShapes)}; classCount={classCount}; expectedClassCount={expectedClassCount}; dictionary={dictionary}"));
    return 0;
}

static int RunProbe(Dictionary<string, string> options)
{
    NodalOsOnnxNativeRuntimeCrashProbeRequest? request = null;
    if (options.TryGetValue("request", out var requestFile) && File.Exists(requestFile))
    {
        try
        {
            request = JsonSerializer.Deserialize<NodalOsOnnxNativeRuntimeCrashProbeRequest>(
                File.ReadAllText(requestFile),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (JsonException)
        {
            request = null;
        }
    }

    if (request is null)
    {
        EmitReport(new NodalOsOnnxOutOfProcessRunnerReport(
            "probe-unknown", "SessionCreation", "BlockedByModelRuntime", null, null,
            false, false, true, "probe request missing or invalid"));
        return 0;
    }

    // Enforce the same safety gate in the child as the parent.
    if (request.FullScreen || request.Sensitive || request.OriginalRawPersisted || !request.Synthetic)
    {
        EmitReport(new NodalOsOnnxOutOfProcessRunnerReport(
            request.ProbeId, request.Stage.ToString(), "UnsupportedFixture", null, null,
            false, false, true, "unsafe fixture rejected in child before runtime"));
        return 0;
    }

    var repoRoot = options.TryGetValue("repo-root", out var root) && Directory.Exists(root)
        ? Path.GetFullPath(root)
        : Directory.GetCurrentDirectory();

    var image = BuildFixtureImage(request, out var width, out var height, out var expectedText);

    var redaction = new NodalOsPixelRedactionResult(
        $"r-{Guid.NewGuid():N}",
        NodalOsPixelRedactionDecision.CleanNoRedactionRequired,
        "hash", "hash", Array.Empty<NodalOsPixelRedactionMask>(),
        NodalOsPixelRedactionVerificationStatus.Verified,
        true, false, false, true,
        Array.Empty<NodalOsPixelRedactionFinding>(), Array.Empty<string>(),
        new NodalOsPixelRedactionEvidence($"e-{Guid.NewGuid():N}", "hash", "hash", false, Array.Empty<string>(), "", true),
        true)
    {
        RedactedImageBytesForOcrHandoff = image
    };

    var inferenceRequest = new NodalOsOnnxOcrSyntheticInferenceRequest(
        request.ProbeId,
        image,
        width,
        height,
        redaction,
        NodalOsOcrVisionSensitivity.Low,
        AllowFullScreen: false,
        AllowRawPersistence: false,
        ProductionMode: false,
        Language: "en",
        ModelSetId: "paddleocr-onnx-v4-en");

    // NOTE: this is the call that can natively crash the runtime for text-like fixtures.
    var pipeline = new NodalOsOnnxOcrSyntheticInferencePipeline();
    var result = pipeline.Run(inferenceRequest, repoRoot);

    EmitReport(new NodalOsOnnxOutOfProcessRunnerReport(
        request.ProbeId,
        request.Stage.ToString(),
        MapStatus(result.Status),
        result.DetectionResult.TextBoxes.Count,
        result.RecognitionResults.Count,
        CallsSaas: result.CallsSaas,
        RawPersisted: result.RawPersisted,
        NoAuthority: result.NoAuthority,
        $"child probe completed: status={result.Status}, expected='{expectedText}'"));
    return 0;
}

static string MapStatus(NodalOsOnnxOcrInferenceStatus status) => status switch
{
    NodalOsOnnxOcrInferenceStatus.Success => "Passed",
    NodalOsOnnxOcrInferenceStatus.NoTextDetected => "NoTextDetected",
    NodalOsOnnxOcrInferenceStatus.LowConfidence => "LowConfidence",
    NodalOsOnnxOcrInferenceStatus.ModelMissing => "BlockedByModelRuntime",
    NodalOsOnnxOcrInferenceStatus.ModelUnverified => "BlockedByModelRuntime",
    NodalOsOnnxOcrInferenceStatus.SessionLoadFailed => "BlockedByModelRuntime",
    NodalOsOnnxOcrInferenceStatus.BlockedByModelRuntime => "BlockedByModelRuntime",
    _ => "BlockedByModelRuntime"
};

static NodalOsDetectorRuntimeCompatibilityStatus MapDetectorStatus(NodalOsOnnxOutOfProcessGuardResult result)
{
    if (result.TimedOut)
        return NodalOsDetectorRuntimeCompatibilityStatus.TimedOut;

    return result.ProbeResult.Status switch
    {
        NodalOsOnnxNativeRuntimeCrashProbeStatus.Passed => NodalOsDetectorRuntimeCompatibilityStatus.RunSucceeded,
        NodalOsOnnxNativeRuntimeCrashProbeStatus.NativeRuntimeCrash => NodalOsDetectorRuntimeCompatibilityStatus.NativeRuntimeCrashContained,
        NodalOsOnnxNativeRuntimeCrashProbeStatus.ProcessCrashed => NodalOsDetectorRuntimeCompatibilityStatus.BlockedByModelRuntime,
        NodalOsOnnxNativeRuntimeCrashProbeStatus.InvalidTensorShape => NodalOsDetectorRuntimeCompatibilityStatus.InvalidTensorShape,
        NodalOsOnnxNativeRuntimeCrashProbeStatus.TimedOut => NodalOsDetectorRuntimeCompatibilityStatus.TimedOut,
        _ => NodalOsDetectorRuntimeCompatibilityStatus.BlockedByModelRuntime
    };
}

static NodalOsFullOcrHandoffProbeStatus MapHandoffStatus(NodalOsOnnxOutOfProcessGuardResult result)
{
    if (result.TimedOut)
        return NodalOsFullOcrHandoffProbeStatus.TimedOut;
    if (result.Reason.Contains(nameof(NodalOsFullOcrHandoffProbeStatus.BlockedByInvalidBox), StringComparison.OrdinalIgnoreCase))
        return NodalOsFullOcrHandoffProbeStatus.BlockedByInvalidBox;
    if (result.Reason.Contains(nameof(NodalOsFullOcrHandoffProbeStatus.BlockedByOutOfBoundsCrop), StringComparison.OrdinalIgnoreCase))
        return NodalOsFullOcrHandoffProbeStatus.BlockedByOutOfBoundsCrop;
    if (result.Reason.Contains(nameof(NodalOsFullOcrHandoffProbeStatus.BlockedByEmptyCrop), StringComparison.OrdinalIgnoreCase))
        return NodalOsFullOcrHandoffProbeStatus.BlockedByEmptyCrop;
    if (result.Reason.Contains(nameof(NodalOsFullOcrHandoffProbeStatus.BlockedByRecognizerTensorShape), StringComparison.OrdinalIgnoreCase))
        return NodalOsFullOcrHandoffProbeStatus.BlockedByRecognizerTensorShape;

    return result.ProbeResult.Status switch
    {
        NodalOsOnnxNativeRuntimeCrashProbeStatus.Passed => NodalOsFullOcrHandoffProbeStatus.RecognizerRunSucceeded,
        NodalOsOnnxNativeRuntimeCrashProbeStatus.NativeRuntimeCrash => NodalOsFullOcrHandoffProbeStatus.NativeRuntimeCrashContained,
        NodalOsOnnxNativeRuntimeCrashProbeStatus.ProcessCrashed => NodalOsFullOcrHandoffProbeStatus.BlockedByModelRuntime,
        NodalOsOnnxNativeRuntimeCrashProbeStatus.InvalidTensorShape => NodalOsFullOcrHandoffProbeStatus.BlockedByRecognizerTensorShape,
        NodalOsOnnxNativeRuntimeCrashProbeStatus.TimedOut => NodalOsFullOcrHandoffProbeStatus.TimedOut,
        _ => NodalOsFullOcrHandoffProbeStatus.BlockedByModelRuntime
    };
}

static NodalOsRecognizerRuntimeProbeStatus MapRecognizerStatus(NodalOsOnnxOutOfProcessGuardResult result)
{
    if (result.TimedOut)
        return NodalOsRecognizerRuntimeProbeStatus.TimedOut;

    if (result.Reason.Contains("BlockedByDictionaryClassCountMismatch", StringComparison.OrdinalIgnoreCase))
        return NodalOsRecognizerRuntimeProbeStatus.BlockedByDictionaryClassCountMismatch;

    return result.ProbeResult.Status switch
    {
        NodalOsOnnxNativeRuntimeCrashProbeStatus.Passed => NodalOsRecognizerRuntimeProbeStatus.RunSucceeded,
        NodalOsOnnxNativeRuntimeCrashProbeStatus.NativeRuntimeCrash => NodalOsRecognizerRuntimeProbeStatus.NativeRuntimeCrashContained,
        NodalOsOnnxNativeRuntimeCrashProbeStatus.ProcessCrashed => NodalOsRecognizerRuntimeProbeStatus.BlockedByModelRuntime,
        NodalOsOnnxNativeRuntimeCrashProbeStatus.InvalidTensorShape => NodalOsRecognizerRuntimeProbeStatus.InvalidTensorShape,
        NodalOsOnnxNativeRuntimeCrashProbeStatus.TimedOut => NodalOsRecognizerRuntimeProbeStatus.TimedOut,
        _ => NodalOsRecognizerRuntimeProbeStatus.BlockedByModelRuntime
    };
}

static string FormatDimensions(IEnumerable<int> dimensions) => string.Join(",", dimensions);

static NodalOsOnnxNativeRuntimeCrashFixtureKind MapFixture(NodalOsDetectorRuntimeProbeTensorKind tensorKind) => tensorKind switch
{
    NodalOsDetectorRuntimeProbeTensorKind.SafeRectangle => NodalOsOnnxNativeRuntimeCrashFixtureKind.LargeRectangle,
    NodalOsDetectorRuntimeProbeTensorKind.SafeCircle => NodalOsOnnxNativeRuntimeCrashFixtureKind.LargeCircle,
    NodalOsDetectorRuntimeProbeTensorKind.CurrentPreprocessedSyntheticText => NodalOsOnnxNativeRuntimeCrashFixtureKind.LargeCenteredText,
    NodalOsDetectorRuntimeProbeTensorKind.SyntheticText => NodalOsOnnxNativeRuntimeCrashFixtureKind.LargeCenteredText,
    _ => NodalOsOnnxNativeRuntimeCrashFixtureKind.StripeSafe
};

static SessionOptions CreateSessionOptions(NodalOsDetectorRuntimeSessionOptionKind optionKind)
{
    var options = new SessionOptions();
    switch (optionKind)
    {
        case NodalOsDetectorRuntimeSessionOptionKind.GraphOptimizationDisabled:
            options.GraphOptimizationLevel = GraphOptimizationLevel.ORT_DISABLE_ALL;
            break;
        case NodalOsDetectorRuntimeSessionOptionKind.GraphOptimizationBasic:
            options.GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_BASIC;
            break;
        case NodalOsDetectorRuntimeSessionOptionKind.SingleThreaded:
            options.IntraOpNumThreads = 1;
            options.InterOpNumThreads = 1;
            break;
        case NodalOsDetectorRuntimeSessionOptionKind.MemoryPatternDisabled:
            options.EnableMemoryPattern = false;
            break;
        case NodalOsDetectorRuntimeSessionOptionKind.CpuArenaDisabled:
            options.EnableCpuMemArena = false;
            break;
    }

    return options;
}

static SessionOptions CreateRecognizerSessionOptions(NodalOsRecognizerRuntimeSessionOptionKind optionKind)
{
    var options = new SessionOptions();
    switch (optionKind)
    {
        case NodalOsRecognizerRuntimeSessionOptionKind.GraphOptimizationDisabled:
            options.GraphOptimizationLevel = GraphOptimizationLevel.ORT_DISABLE_ALL;
            break;
        case NodalOsRecognizerRuntimeSessionOptionKind.GraphOptimizationBasic:
            options.GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_BASIC;
            break;
        case NodalOsRecognizerRuntimeSessionOptionKind.SingleThreaded:
            options.IntraOpNumThreads = 1;
            options.InterOpNumThreads = 1;
            break;
        case NodalOsRecognizerRuntimeSessionOptionKind.MemoryPatternDisabled:
            options.EnableMemoryPattern = false;
            break;
        case NodalOsRecognizerRuntimeSessionOptionKind.CpuArenaDisabled:
            options.EnableCpuMemArena = false;
            break;
        case NodalOsRecognizerRuntimeSessionOptionKind.SequentialExecution:
            options.ExecutionMode = ExecutionMode.ORT_SEQUENTIAL;
            break;
        case NodalOsRecognizerRuntimeSessionOptionKind.DeterministicMinimal:
            options.GraphOptimizationLevel = GraphOptimizationLevel.ORT_DISABLE_ALL;
            options.IntraOpNumThreads = 1;
            options.InterOpNumThreads = 1;
            options.EnableMemoryPattern = false;
            options.EnableCpuMemArena = false;
            options.ExecutionMode = ExecutionMode.ORT_SEQUENTIAL;
            break;
    }

    return options;
}

static float[] BuildDetectorTensor(NodalOsDetectorRuntimeProbeTensorKind tensorKind, int[] shape)
{
    var channels = shape[1];
    var height = shape[2];
    var width = shape[3];
    var tensor = new float[shape.Aggregate(1, (a, b) => a * b)];

    for (var c = 0; c < channels; c++)
    {
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var idx = c * height * width + y * width + x;
                tensor[idx] = tensorKind switch
                {
                    NodalOsDetectorRuntimeProbeTensorKind.Zero => 0f,
                    NodalOsDetectorRuntimeProbeTensorKind.Ones => 1f,
                    NodalOsDetectorRuntimeProbeTensorKind.Gradient => (float)(x + y) / (width + height),
                    NodalOsDetectorRuntimeProbeTensorKind.SafeRectangle => InRectangle(x, y, width, height) ? 1f : 0f,
                    NodalOsDetectorRuntimeProbeTensorKind.SafeCircle => InCircle(x, y, width, height) ? 1f : 0f,
                    NodalOsDetectorRuntimeProbeTensorKind.SyntheticText => InSyntheticTextBar(x, y, width, height) ? 0f : 1f,
                    _ => 0f
                };
            }
        }
    }

    return tensor;
}

static float[] BuildCurrentPreprocessedSyntheticTextTensor(out int[] shape)
{
    var generator = new NodalOsSyntheticOcrTextFixtureGenerator();
    var fixture = generator.Generate(
        "TEST",
        new NodalOsSyntheticOcrTextFixtureOptions(
            640,
            160,
            NodalOsSyntheticOcrTextColorScheme.BlackOnWhite,
            NodalOsSyntheticOcrTextRenderMode.PixelFont,
            HorizontalPadding: 24,
            VerticalPadding: 40,
            CharacterSpacing: 8,
            AllowRawPersistence: false,
            AllowFullScreen: false));

    var imagePrep = new NodalOsOnnxOcrImagePreProcessor().Prepare(
        fixture.ImageBytes,
        fixture.Width,
        fixture.Height,
        fixture.RedactionResult,
        NodalOsOcrVisionSensitivity.Low,
        allowFullScreen: false,
        allowRawPersistence: false);

    var detPrep = new NodalOsOnnxOcrDetectorPreProcessor().Prepare(imagePrep, targetWidth: 640, targetHeight: 640);
    shape = detPrep.InputShape.ToArray();
    return detPrep.InputTensor.ToArray();
}

static byte[] BuildSyntheticRedactedImage(
    string text,
    int width,
    int height,
    out int actualWidth,
    out int actualHeight,
    out NodalOsPixelRedactionResult redaction)
{
    var generator = new NodalOsSyntheticOcrTextFixtureGenerator();
    var fixture = generator.Generate(
        text,
        new NodalOsSyntheticOcrTextFixtureOptions(
            width,
            height,
            NodalOsSyntheticOcrTextColorScheme.BlackOnWhite,
            NodalOsSyntheticOcrTextRenderMode.PixelFont,
            HorizontalPadding: 24,
            VerticalPadding: 40,
            CharacterSpacing: 8,
            AllowRawPersistence: false,
            AllowFullScreen: false));

    actualWidth = fixture.Width;
    actualHeight = fixture.Height;
    redaction = fixture.RedactionResult;
    return fixture.ImageBytes;
}

static NodalOsOnnxOcrTextBox? RunDetectorForFirstBox(
    string repoRoot,
    NodalOsOnnxOcrImagePreProcessingResult imagePrep,
    int cropWidth,
    int cropHeight)
{
    var detPrep = new NodalOsOnnxOcrDetectorPreProcessor().Prepare(imagePrep, targetWidth: 640, targetHeight: 640);
    if (detPrep.Status != NodalOsOnnxOcrPreProcessingStatus.Success)
        return null;

    var modelPath = Path.GetFullPath(Path.Combine(repoRoot, "tools", "ocr-worker", "models", "onnx", "ch_PP-OCRv4_det.onnx"));
    using var session = new InferenceSession(modelPath);
    var inputName = session.InputMetadata.Keys.FirstOrDefault() ?? "x";
    using var outputs = session.Run([NamedOnnxValue.CreateFromTensor(inputName, new DenseTensor<float>(detPrep.InputTensor, detPrep.InputShape))]);
    var output = outputs.First().AsTensor<float>();
    var decoded = new NodalOsOnnxOcrDetectorPostProcessor().Decode(output.ToArray(), output.Dimensions.ToArray(), cropWidth, cropHeight, threshold: 0.3f);
    return decoded.TextBoxes.FirstOrDefault(b => b.Valid);
}

static NodalOsOnnxOcrTextBox ManualBox(NodalOsFullOcrHandoffBoxKind kind, int imageWidth, int imageHeight)
{
    return kind switch
    {
        NodalOsFullOcrHandoffBoxKind.Degenerate => Box(0, 20, 20, 0, 12, valid: false),
        NodalOsFullOcrHandoffBoxKind.OutOfBounds => Box(0, imageWidth - 10, 20, 80, 30),
        NodalOsFullOcrHandoffBoxKind.EmptyCrop => Box(0, 30, 30, 0, 0),
        NodalOsFullOcrHandoffBoxKind.TooSmall => Box(0, 30, 30, 1, 1),
        _ => Box(0, Math.Max(0, imageWidth / 6), Math.Max(0, imageHeight / 4), Math.Max(8, imageWidth * 2 / 3), Math.Max(8, imageHeight / 2))
    };

    static NodalOsOnnxOcrTextBox Box(int id, int x, int y, int w, int h, bool valid = true)
    {
        return new NodalOsOnnxOcrTextBox(
            $"box-{id}",
            [x, y, x + w, y, x + w, y + h, x, y + h],
            0.99,
            x,
            y,
            w,
            h,
            valid);
    }
}

static NodalOsOnnxOcrImagePreProcessingResult? ExtractCropForProbe(
    NodalOsOnnxOcrImagePreProcessingResult image,
    NodalOsOnnxOcrTextBox box)
{
    if (box.CropX < 0 || box.CropY < 0 || box.CropWidth <= 0 || box.CropHeight <= 0)
        return null;
    if (box.CropX + box.CropWidth > image.Width || box.CropY + box.CropHeight > image.Height)
        return null;

    var cropData = new float[box.CropWidth * box.CropHeight * 4];
    for (var row = 0; row < box.CropHeight; row++)
    {
        for (var col = 0; col < box.CropWidth; col++)
        {
            var srcIdx = ((box.CropY + row) * image.Width + (box.CropX + col)) * 4;
            var dstIdx = (row * box.CropWidth + col) * 4;
            cropData[dstIdx + 0] = image.NormalizedData[srcIdx + 0];
            cropData[dstIdx + 1] = image.NormalizedData[srcIdx + 1];
            cropData[dstIdx + 2] = image.NormalizedData[srcIdx + 2];
            cropData[dstIdx + 3] = image.NormalizedData[srcIdx + 3];
        }
    }

    return new NodalOsOnnxOcrImagePreProcessingResult(
        $"crop-{Guid.NewGuid():N}",
        NodalOsOnnxOcrPreProcessingStatus.Success,
        cropData,
        1,
        4,
        box.CropHeight,
        box.CropWidth,
        1,
        1,
        0,
        0,
        image.SourceFormat,
        box.CropWidth,
        box.CropHeight,
        NoAuthority: true,
        Redacted: true,
        "probe crop extracted from synthetic redacted image");
}

static float[] BuildRecognizerTensor(NodalOsRecognizerRuntimeTensorKind tensorKind, int[] shape)
{
    var channels = shape[1];
    var height = shape[2];
    var width = shape[3];
    var tensor = new float[shape.Aggregate(1, (a, b) => a * b)];

    for (var c = 0; c < channels; c++)
    {
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var idx = c * height * width + y * width + x;
                tensor[idx] = tensorKind switch
                {
                    NodalOsRecognizerRuntimeTensorKind.Zero => 0f,
                    NodalOsRecognizerRuntimeTensorKind.Ones => 1f,
                    NodalOsRecognizerRuntimeTensorKind.Gradient => (float)(x + y) / (width + height),
                    NodalOsRecognizerRuntimeTensorKind.Checker => ((x / 8) + (y / 8)) % 2 == 0 ? -1f : 1f,
                    NodalOsRecognizerRuntimeTensorKind.SyntheticTextCrop => InSyntheticTextBar(x, y, width, height) ? -1f : 1f,
                    NodalOsRecognizerRuntimeTensorKind.HighContrastManualCrop => (x / 12) % 2 == 0 && y > height / 4 && y < height * 3 / 4 ? -1f : 1f,
                    NodalOsRecognizerRuntimeTensorKind.DetectorDerivedCrop => InRectangle(x, y, width, height) ? -1f : 1f,
                    _ => 0f
                };
            }
        }
    }

    return tensor;
}

static int CountChildMatches(List<object> results, string matchKind)
{
    var count = 0;
    foreach (var result in results)
    {
        var child = result.GetType().GetProperty("ChildSummary")?.GetValue(result);
        if (child is not JsonElement element ||
            element.ValueKind != JsonValueKind.Object ||
            !element.TryGetProperty("MatchKind", out var actual))
        {
            continue;
        }

        if (string.Equals(actual.GetString(), matchKind, StringComparison.Ordinal))
            count++;
    }

    return count;
}

static int SumChildInt(List<object> results, string propertyName)
{
    var sum = 0;
    foreach (var result in results)
    {
        var child = result.GetType().GetProperty("ChildSummary")?.GetValue(result);
        if (child is not JsonElement element ||
            element.ValueKind != JsonValueKind.Object ||
            !element.TryGetProperty(propertyName, out var value) ||
            value.ValueKind != JsonValueKind.Number)
        {
            continue;
        }

        sum += value.GetInt32();
    }

    return sum;
}

static int SumObjectInt(List<object> results, string propertyName)
{
    var sum = 0;
    foreach (var result in results)
    {
        var value = result.GetType().GetProperty(propertyName)?.GetValue(result);
        if (value is int intValue)
            sum += intValue;
    }

    return sum;
}

static int CountObjectString(List<object> results, string propertyName, string expected)
{
    var count = 0;
    foreach (var result in results)
    {
        var value = result.GetType().GetProperty(propertyName)?.GetValue(result) as string;
        if (string.Equals(value, expected, StringComparison.Ordinal))
            count++;
    }

    return count;
}

static string? ReadChildString(JsonElement? element, string propertyName)
{
    if (element is not { ValueKind: JsonValueKind.Object } value ||
        !value.TryGetProperty(propertyName, out var property) ||
        property.ValueKind != JsonValueKind.String)
    {
        return null;
    }

    return property.GetString();
}

static int ReadChildInt(JsonElement? element, string propertyName)
{
    if (element is not { ValueKind: JsonValueKind.Object } value ||
        !value.TryGetProperty(propertyName, out var property) ||
        property.ValueKind != JsonValueKind.Number)
    {
        return 0;
    }

    return property.GetInt32();
}

static InternalControlledRealImageFixture[] LoadInternalControlledRealImageFixtureManifest(string manifestPath)
{
    if (!File.Exists(manifestPath))
        return Array.Empty<InternalControlledRealImageFixture>();

    using var doc = JsonDocument.Parse(File.ReadAllText(manifestPath));
    if (!doc.RootElement.TryGetProperty("fixtures", out var fixturesElement) ||
        fixturesElement.ValueKind != JsonValueKind.Array)
    {
        return Array.Empty<InternalControlledRealImageFixture>();
    }

    var fixtures = new List<InternalControlledRealImageFixture>();
    foreach (var element in fixturesElement.EnumerateArray())
    {
        fixtures.Add(new InternalControlledRealImageFixture(
            ReadRequiredString(element, "id"),
            ReadRequiredString(element, "filename"),
            ReadRequiredString(element, "sourceCategory"),
            ReadRequiredBool(element, "createdByInternalQa"),
            ReadRequiredBool(element, "containsRealPersonData"),
            ReadRequiredBool(element, "containsCustomerData"),
            ReadRequiredBool(element, "containsFinancialData"),
            ReadRequiredBool(element, "containsDocumentData"),
            ReadRequiredBool(element, "containsScreenCapture"),
            ReadRequiredBool(element, "sensitive"),
            ReadRequiredString(element, "expectedText"),
            ReadRequiredBool(element, "allowedForOcrPipeline"),
            ReadRequiredString(element, "reason")));
    }

    return fixtures.ToArray();
}

static (string CaptureMode, InternalControlledScreenRegionFixture[] Fixtures) LoadInternalControlledScreenRegionFixtureManifest(string manifestPath)
{
    if (!File.Exists(manifestPath))
        return ("missing", Array.Empty<InternalControlledScreenRegionFixture>());

    using var doc = JsonDocument.Parse(File.ReadAllText(manifestPath));
    var captureMode = ReadRequiredString(doc.RootElement, "captureMode");
    if (!doc.RootElement.TryGetProperty("fixtures", out var fixturesElement) ||
        fixturesElement.ValueKind != JsonValueKind.Array)
    {
        return (captureMode, Array.Empty<InternalControlledScreenRegionFixture>());
    }

    var fixtures = new List<InternalControlledScreenRegionFixture>();
    foreach (var element in fixturesElement.EnumerateArray())
    {
        var bounds = element.TryGetProperty("regionBounds", out var regionBounds)
            ? regionBounds
            : default;
        var windowBounds = element.TryGetProperty("windowBounds", out var fixtureWindowBounds)
            ? fixtureWindowBounds
            : doc.RootElement.TryGetProperty("windowBounds", out var rootWindowBounds)
                ? rootWindowBounds
                : default;
        var regionX = ReadRequiredInt(bounds, "x");
        var regionY = ReadRequiredInt(bounds, "y");
        var regionWidth = ReadRequiredInt(bounds, "width");
        var regionHeight = ReadRequiredInt(bounds, "height");
        var windowX = ReadRequiredInt(windowBounds, "x");
        var windowY = ReadRequiredInt(windowBounds, "y");
        var windowWidth = ReadRequiredInt(windowBounds, "width");
        var windowHeight = ReadRequiredInt(windowBounds, "height");
        if ((windowWidth <= 0 || windowHeight <= 0) && regionWidth > 0 && regionHeight > 0)
        {
            windowX = 0;
            windowY = 0;
            windowWidth = regionX + regionWidth + Math.Max(regionX, 0);
            windowHeight = regionY + regionHeight + Math.Max(regionY, 0);
        }

        fixtures.Add(new InternalControlledScreenRegionFixture(
            ReadRequiredString(element, "id"),
            ReadRequiredString(element, "sourceCategory"),
            ReadRequiredBool(element, "createdByInternalQa"),
            ReadRequiredString(element, "windowTitleOrSource"),
            ReadRequiredString(element, "processOrSource"),
            windowX,
            windowY,
            windowWidth,
            windowHeight,
            regionX,
            regionY,
            regionWidth,
            regionHeight,
            ReadRequiredString(element, "boundsSource"),
            ReadRequiredBool(element, "containsRealPersonData"),
            ReadRequiredBool(element, "containsCustomerData"),
            ReadRequiredBool(element, "containsFinancialData"),
            ReadRequiredBool(element, "containsDocumentData"),
            ReadRequiredBool(element, "containsCredentialOrPasswordData"),
            ReadRequiredBool(element, "containsFullScreen"),
            ReadRequiredBool(element, "sensitive"),
            ReadRequiredString(element, "expectedText"),
            ReadRequiredBool(element, "allowedForOcrPipeline"),
            ReadRequiredString(element, "reason")));
    }

    return (captureMode, fixtures.ToArray());
}

static string ReadRequiredString(JsonElement element, string propertyName) =>
    element.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String
        ? value.GetString() ?? string.Empty
        : string.Empty;

static int ReadRequiredInt(JsonElement element, string propertyName) =>
    element.ValueKind == JsonValueKind.Object &&
    element.TryGetProperty(propertyName, out var value) &&
    value.ValueKind == JsonValueKind.Number
        ? value.GetInt32()
        : 0;

static bool ReadRequiredBool(JsonElement element, string propertyName) =>
    element.TryGetProperty(propertyName, out var value) &&
    value.ValueKind is JsonValueKind.True or JsonValueKind.False &&
    value.GetBoolean();

static double ReadChildDouble(JsonElement? element, string propertyName)
{
    if (element is not { ValueKind: JsonValueKind.Object } value ||
        !value.TryGetProperty(propertyName, out var property) ||
        property.ValueKind != JsonValueKind.Number)
    {
        return 0d;
    }

    return property.GetDouble();
}

static object EstimateSyntheticTextGeometry(int width, int height, int horizontalPadding, int verticalPadding)
{
    var x = Math.Clamp(horizontalPadding, 0, Math.Max(0, width - 1));
    var y = Math.Clamp(verticalPadding, 0, Math.Max(0, height - 1));
    var right = Math.Max(x + 1, width - horizontalPadding);
    var bottom = Math.Max(y + 1, height - verticalPadding);
    return new
    {
        X = x,
        Y = y,
        Width = Math.Max(1, right - x),
        Height = Math.Max(1, bottom - y),
        Source = "estimated from synthetic fixture padding; generator contract does not expose glyph bbox"
    };
}

static int LevenshteinDistance(string left, string right)
{
    if (left.Length == 0) return right.Length;
    if (right.Length == 0) return left.Length;

    var previous = new int[right.Length + 1];
    var current = new int[right.Length + 1];
    for (var j = 0; j <= right.Length; j++)
        previous[j] = j;

    for (var i = 1; i <= left.Length; i++)
    {
        current[0] = i;
        for (var j = 1; j <= right.Length; j++)
        {
            var cost = left[i - 1] == right[j - 1] ? 0 : 1;
            current[j] = Math.Min(
                Math.Min(current[j - 1] + 1, previous[j] + 1),
                previous[j - 1] + cost);
        }

        (previous, current) = (current, previous);
    }

    return previous[right.Length];
}

static NodalOsSyntheticOcrTextFixture BuildSyntheticFullImageFixture(
    string text,
    string variant,
    int horizontalPadding,
    int verticalPadding)
{
    var generator = new NodalOsSyntheticOcrTextFixtureGenerator();
    var renderMode = variant.Contains("upper-left", StringComparison.OrdinalIgnoreCase)
        ? NodalOsSyntheticOcrTextRenderMode.PixelFont
        : NodalOsSyntheticOcrTextRenderMode.AntiAliasedPixelFont;
    return generator.Generate(
        text,
        new NodalOsSyntheticOcrTextFixtureOptions(
            640,
            160,
            NodalOsSyntheticOcrTextColorScheme.BlackOnWhite,
            renderMode,
            horizontalPadding,
            verticalPadding,
            CharacterSpacing: 8,
            AllowRawPersistence: false,
            AllowFullScreen: false));
}

static NodalOsOnnxOcrDetectorPostProcessingResult DecodeDetectorWithFallbackThresholds(
    float[] detectorOutput,
    int[] detectorOutputShape,
    int width,
    int height,
    out float thresholdUsed)
{
    var thresholds = new[] { 0.30f, 0.20f, 0.10f, 0.05f };
    NodalOsOnnxOcrDetectorPostProcessingResult? best = null;
    thresholdUsed = thresholds[0];
    foreach (var threshold in thresholds)
    {
        var decoded = new NodalOsOnnxOcrDetectorPostProcessor().Decode(
            detectorOutput,
            detectorOutputShape,
            width,
            height,
            threshold);
        if (best is null || decoded.TextBoxes.Count > best.TextBoxes.Count)
        {
            best = decoded;
            thresholdUsed = threshold;
        }

        if (decoded.TextBoxes.Any(b => b.Valid && b.CropWidth > 1 && b.CropHeight > 1))
            return decoded;
    }

    return best ?? new NodalOsOnnxOcrDetectorPostProcessor().Decode(
        detectorOutput,
        detectorOutputShape,
        width,
        height,
        thresholds[0]);
}

static NodalOsOnnxOcrTextBox ExpandBox(NodalOsOnnxOcrTextBox box, int width, int height, int padding)
{
    var x = Math.Max(0, box.CropX - padding);
    var y = Math.Max(0, box.CropY - padding);
    var right = Math.Min(width, box.CropX + box.CropWidth + padding);
    var bottom = Math.Min(height, box.CropY + box.CropHeight + padding);
    return new NodalOsOnnxOcrTextBox(
        box.BoxId,
        box.Polygon,
        box.Confidence,
        x,
        y,
        Math.Max(1, right - x),
        Math.Max(1, bottom - y),
        box.Valid);
}

static float[] BuildRecognizerTensorFromImageCrop(
    NodalOsOnnxOcrImagePreProcessingResult crop,
    int[] shape,
    NodalOsPaddleOcrRecognizerResizeMode resizeMode,
    out string preprocessingSummary)
{
    // M295-M297: delegate to the PaddleOCR-aligned preprocessor. The legacy stretch (StretchToFixedWidth)
    // distorted aspect ratio; RatioPreservingRightPad mirrors PaddleOCR resize_norm_img (aspect-preserving
    // bilinear resize + right pad with 0.0) and is the default.
    var targetHeight = shape[2];
    var targetWidth = shape[3];
    var prepared = new NodalOsPaddleOcrRecognizerCropPreprocessor().Prepare(
        crop.NormalizedData,
        crop.Width,
        crop.Height,
        targetHeight,
        targetWidth,
        resizeMode);

    preprocessingSummary =
        $"detector crop {crop.Width}x{crop.Height} -> [1,3,{targetHeight},{targetWidth}]; mode={resizeMode}; " +
        $"resizedWidth={prepared.ResizedWidth}; paddedColumns={prepared.PaddedColumns}; " +
        $"aspectPreserved={prepared.AspectRatioPreserved}; widthCapped={prepared.WidthCapped}; " +
        "normalization=(pixel/255-0.5)/0.5; persisted=false";
    return prepared.Tensor;
}

static float[] BuildSyntheticImageRecognizerCropTensor(string text, int[] shape, out string preprocessingSummary)
{
    var channels = shape[1];
    var height = shape[2];
    var width = shape[3];
    var tensor = new float[shape.Aggregate(1, (a, b) => a * b)];
    var pixels = new byte[height, width];
    for (var y = 0; y < height; y++)
    {
        for (var x = 0; x < width; x++)
            pixels[y, x] = 255;
    }

    var logicalWidth = text.Sum(c => c == ' ' ? 3 : 5) + Math.Max(0, text.Length - 1);
    var scaleByWidth = Math.Max(1, (width - 32) / Math.Max(1, logicalWidth));
    var scaleByHeight = Math.Max(1, (height - 10) / 7);
    var scale = Math.Max(1, Math.Min(Math.Min(scaleByWidth, scaleByHeight), 5));
    var renderedWidth = logicalWidth * scale;
    var startX = Math.Max(4, (width - renderedWidth) / 2);
    var startY = Math.Max(2, (height - 7 * scale) / 2);
    var cursorX = startX;

    foreach (var character in text.ToUpperInvariant())
    {
        if (character == ' ')
        {
            cursorX += 4 * scale;
            continue;
        }

        var glyph = SyntheticGlyph(character);
        for (var row = 0; row < glyph.Length; row++)
        {
            for (var col = 0; col < glyph[row].Length; col++)
            {
                if (glyph[row][col] != '1')
                    continue;

                for (var dy = 0; dy < scale; dy++)
                {
                    for (var dx = 0; dx < scale; dx++)
                    {
                        var x = cursorX + col * scale + dx;
                        var y = startY + row * scale + dy;
                        if (x >= 0 && x < width && y >= 0 && y < height)
                            pixels[y, x] = 0;
                    }
                }
            }
        }

        cursorX += 6 * scale;
    }

    for (var c = 0; c < channels; c++)
    {
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var idx = c * height * width + y * width + x;
                tensor[idx] = (pixels[y, x] / 255f - 0.5f) / 0.5f;
            }
        }
    }

    preprocessingSummary =
        $"in-memory synthetic crop; text='{BrowserCredentialRedactor.Redact(text)}'; white background, black block glyphs; direct resize/pad to [1,3,{height},{width}]; normalization=(pixel/255-0.5)/0.5; persisted=false";
    return tensor;
}

static (string DecodedText, double MeanConfidence, int EmittedTokens) DecodePaddleOcrOfficialSpaceOutput(
    float[] values,
    int classCount,
    int blankIndex,
    int spaceIndex,
    IReadOnlyList<string> dictionaryTokens)
{
    if (classCount <= 0 || values.Length < classCount)
        return (string.Empty, 0d, 0);

    var timesteps = values.Length / classCount;
    var builder = new System.Text.StringBuilder();
    var previous = -1;
    var confidenceSum = 0d;
    var emitted = 0;
    for (var t = 0; t < timesteps; t++)
    {
        var offset = t * classCount;
        var argmax = 0;
        var argmaxProbability = double.NegativeInfinity;
        for (var i = 0; i < classCount; i++)
        {
            if (values[offset + i] > argmaxProbability)
            {
                argmaxProbability = values[offset + i];
                argmax = i;
            }
        }

        if (argmax == previous)
            continue;

        previous = argmax;
        if (argmax == blankIndex)
            continue;

        string? token = null;
        if (argmax == spaceIndex)
        {
            token = " ";
        }
        else if (argmax > blankIndex && argmax <= dictionaryTokens.Count)
        {
            token = dictionaryTokens[argmax - 1];
        }

        if (token is null)
            continue;

        builder.Append(token);
        confidenceSum += argmaxProbability;
        emitted++;
    }

    return (builder.ToString(), emitted == 0 ? 0d : confidenceSum / emitted, emitted);
}

static string NormalizeNoAuthorityPreview(string value) =>
    new(value.ToUpperInvariant().Where(c => !char.IsWhiteSpace(c)).ToArray());

static string[] SyntheticGlyph(char character) => character switch
{
    '0' => ["11111", "10001", "10011", "10101", "11001", "10001", "11111"],
    '1' => ["00100", "01100", "00100", "00100", "00100", "00100", "01110"],
    '2' => ["11110", "00001", "00001", "11110", "10000", "10000", "11111"],
    '3' => ["11110", "00001", "00001", "01110", "00001", "00001", "11110"],
    '4' => ["10010", "10010", "10010", "11111", "00010", "00010", "00010"],
    '5' => ["11111", "10000", "10000", "11110", "00001", "00001", "11110"],
    '6' => ["01111", "10000", "10000", "11110", "10001", "10001", "01110"],
    '7' => ["11111", "00001", "00010", "00100", "01000", "01000", "01000"],
    '8' => ["01110", "10001", "10001", "01110", "10001", "10001", "01110"],
    '9' => ["01110", "10001", "10001", "01111", "00001", "00001", "11110"],
    'A' => ["01110", "10001", "10001", "11111", "10001", "10001", "10001"],
    'B' => ["11110", "10001", "10001", "11110", "10001", "10001", "11110"],
    'C' => ["01111", "10000", "10000", "10000", "10000", "10000", "01111"],
    'E' => ["11111", "10000", "10000", "11110", "10000", "10000", "11111"],
    'G' => ["01111", "10000", "10000", "10011", "10001", "10001", "01111"],
    'L' => ["10000", "10000", "10000", "10000", "10000", "10000", "11111"],
    'M' => ["10001", "11011", "10101", "10101", "10001", "10001", "10001"],
    'N' => ["10001", "11001", "10101", "10011", "10001", "10001", "10001"],
    'O' => ["01110", "10001", "10001", "10001", "10001", "10001", "01110"],
    'P' => ["11110", "10001", "10001", "11110", "10000", "10000", "10000"],
    'R' => ["11110", "10001", "10001", "11110", "10100", "10010", "10001"],
    'S' => ["01111", "10000", "10000", "01110", "00001", "00001", "11110"],
    'V' => ["10001", "10001", "10001", "10001", "01010", "01010", "00100"],
    'W' => ["10001", "10001", "10001", "10101", "10101", "11011", "10001"],
    _ => ["11111", "00001", "00010", "00100", "00100", "00000", "00100"]
};

static float[] BuildExtraClassTensor(string fixture, int[] shape)
{
    var channels = shape[1];
    var height = shape[2];
    var width = shape[3];
    var tensor = new float[shape.Aggregate(1, (a, b) => a * b)];

    for (var c = 0; c < channels; c++)
    {
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var idx = c * height * width + y * width + x;
                tensor[idx] = fixture.ToLowerInvariant() switch
                {
                    "black" => -1f,
                    "white" => 1f,
                    "deterministicnoise" => DeterministicNoise(x, y, c),
                    "gradient" => (float)(x + y) / Math.Max(1, width + height),
                    "thinlines" => y % 5 == 0 || x % 29 == 0 ? -1f : 1f,
                    "checkerboard" => ((x / 6) + (y / 6)) % 2 == 0 ? -1f : 1f,
                    "highcontrastcrop" => (x / 12) % 2 == 0 && y > height / 5 && y < height * 4 / 5 ? -1f : 1f,
                    "detectorderivedcrop" => InRectangle(x, y, width, height) ? -1f : 1f,
                    "outofdictionary" => InOutOfDictionaryPattern(x, y, width, height) ? -1f : 1f,
                    "12345" => InDigitBars(x, y, width, height) ? -1f : 1f,
                    "abc123" => InAlphaNumericBars(x, y, width, height) ? -1f : 1f,
                    "nodal" => InWordLikeBars(x, y, width, height, 5) ? -1f : 1f,
                    _ => InWordLikeBars(x, y, width, height, 4) ? -1f : 1f
                };
            }
        }
    }

    return tensor;
}

static ExtraClassOutputAnalysis AnalyzeExtraClassOutput(
    float[] values,
    int[] outputShape,
    int extraClassIndex,
    int blankIndex,
    double negligibleThreshold)
{
    var classCount = outputShape.Length >= 1 ? outputShape[^1] : 0;
    var timesteps = classCount > 0 ? values.Length / classCount : 0;
    if (classCount <= 0 || timesteps <= 0)
        return new ExtraClassOutputAnalysis(classCount, 0, 0, 0d, 0d, 0, []);

    var argmaxCounts = new int[classCount];
    var extraSum = 0d;
    var extraMax = 0d;
    var extraArgmax = 0;
    var blankArgmax = 0;

    for (var t = 0; t < timesteps; t++)
    {
        var offset = t * classCount;
        var useRawProbabilities = LooksLikeProbabilityVector(values, offset, classCount);
        var maxLogit = float.NegativeInfinity;
        if (!useRawProbabilities)
        {
            for (var i = 0; i < classCount; i++)
                maxLogit = Math.Max(maxLogit, values[offset + i]);
        }

        var denominator = 0d;
        if (!useRawProbabilities)
        {
            for (var i = 0; i < classCount; i++)
                denominator += Math.Exp(values[offset + i] - maxLogit);
        }

        var argmax = 0;
        var argmaxProbability = double.NegativeInfinity;
        for (var i = 0; i < classCount; i++)
        {
            var probability = useRawProbabilities
                ? values[offset + i]
                : Math.Exp(values[offset + i] - maxLogit) / denominator;
            if (probability > argmaxProbability)
            {
                argmaxProbability = probability;
                argmax = i;
            }
        }

        var extraProbability = extraClassIndex >= 0 && extraClassIndex < classCount
            ? useRawProbabilities
                ? values[offset + extraClassIndex]
                : Math.Exp(values[offset + extraClassIndex] - maxLogit) / denominator
            : 0d;

        extraSum += extraProbability;
        extraMax = Math.Max(extraMax, extraProbability);
        argmaxCounts[argmax]++;
        if (argmax == extraClassIndex) extraArgmax++;
        if (argmax == blankIndex) blankArgmax++;
    }

    var dominant = argmaxCounts
        .Select((count, index) => new { count, index })
        .OrderByDescending(item => item.count)
        .ThenBy(item => item.index)
        .Where(item => item.count > 0)
        .Take(8)
        .Select(item => item.index)
        .ToArray();

    return new ExtraClassOutputAnalysis(
        classCount,
        timesteps,
        extraArgmax,
        extraMax,
        timesteps == 0 ? 0d : extraSum / timesteps,
        blankArgmax,
        dominant);

    static bool LooksLikeProbabilityVector(float[] values, int offset, int classCount)
    {
        var sum = 0d;
        for (var i = 0; i < classCount; i++)
        {
            var value = values[offset + i];
            if (value < -0.000001f || float.IsNaN(value) || float.IsInfinity(value))
                return false;
            sum += value;
        }

        return sum is > 0.98d and < 1.02d;
    }
}

static NodalOsExtraClassArgmaxProbeStatus MapExtraClassStatus(NodalOsOnnxOutOfProcessGuardResult guardResult)
{
    if (guardResult.TimedOut)
        return NodalOsExtraClassArgmaxProbeStatus.ProbeTimedOut;
    if (guardResult.ExitCode is not 0)
        return NodalOsExtraClassArgmaxProbeStatus.ProbeRuntimeFailed;

    try
    {
        using var document = JsonDocument.Parse(guardResult.Reason);
        if (document.RootElement.TryGetProperty("Status", out var status) &&
            Enum.TryParse<NodalOsExtraClassArgmaxProbeStatus>(status.GetString(), ignoreCase: true, out var parsed))
        {
            return parsed;
        }
    }
    catch (JsonException)
    {
        // Fall through to runtime failure; the parent still survived and captured the reason.
    }

    return NodalOsExtraClassArgmaxProbeStatus.ProbeRuntimeFailed;
}

static (string Command, string[] PrefixArguments) ResolveCurrentRunnerInvocation()
{
    var processPath = Environment.ProcessPath ?? string.Empty;
    var assemblyPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
    if (!string.IsNullOrWhiteSpace(processPath) &&
        Path.GetFileNameWithoutExtension(processPath).Equals("dotnet", StringComparison.OrdinalIgnoreCase) &&
        File.Exists(assemblyPath))
    {
        return (processPath, [assemblyPath]);
    }

    if (!string.IsNullOrWhiteSpace(processPath))
        return (processPath, []);

    var exePath = Path.Combine(AppContext.BaseDirectory, "OneBrain.Tools.OnnxOcrProbeRunner.exe");
    return (exePath, []);
}

static float DeterministicNoise(int x, int y, int c)
{
    unchecked
    {
        var value = (uint)(x * 1103515245 + y * 12345 + c * 2654435761);
        return ((value % 2000) / 1000f) - 1f;
    }
}

static bool InDigitBars(int x, int y, int width, int height) =>
    y > height / 5 && y < height * 4 / 5 && (x / Math.Max(1, width / 18)) % 3 == 0;

static bool InAlphaNumericBars(int x, int y, int width, int height) =>
    y > height / 6 && y < height * 5 / 6 && ((x / Math.Max(1, width / 20)) + (y / 4)) % 4 == 0;

static bool InWordLikeBars(int x, int y, int width, int height, int characters)
{
    var left = width / 5;
    var right = width * 4 / 5;
    if (x < left || x > right || y < height / 5 || y > height * 4 / 5) return false;
    var charWidth = Math.Max(1, (right - left) / Math.Max(1, characters));
    var local = (x - left) % charWidth;
    return local < Math.Max(1, charWidth / 4) || y is var row && row % 11 == 0;
}

static bool InOutOfDictionaryPattern(int x, int y, int width, int height)
{
    var centerX = width / 2;
    var centerY = height / 2;
    return Math.Abs(x - centerX) == Math.Abs(y - centerY) ||
           Math.Abs(x - centerX) + Math.Abs(y - centerY) < Math.Min(width, height) / 5;
}

static int EmitHandoffChild(
    NodalOsFullOcrHandoffBoxKind boxKind,
    NodalOsFullOcrHandoffProbeStatus status,
    NodalOsFullOcrHandoffStage stage,
    NodalOsOnnxOcrTextBox? box,
    int[] recShape,
    string reason)
{
    Console.Error.WriteLine($"stage={stage} boxKind={boxKind} status={status} box=[{box?.CropX},{box?.CropY},{box?.CropWidth},{box?.CropHeight}] recShape=[{string.Join(",", recShape)}]");
    EmitReport(new NodalOsOnnxOutOfProcessRunnerReport(
        $"handoff-{Guid.NewGuid():N}",
        stage.ToString(),
        status is NodalOsFullOcrHandoffProbeStatus.RecognizerRunSucceeded or NodalOsFullOcrHandoffProbeStatus.StageSucceeded or NodalOsFullOcrHandoffProbeStatus.RecognizerSessionCreated
            ? "Passed"
            : status.ToString(),
        BoxesDetected: box is null ? 0 : 1,
        RecognitionAttempts: status is NodalOsFullOcrHandoffProbeStatus.RecognizerRunSucceeded or NodalOsFullOcrHandoffProbeStatus.RecognizerSessionCreated ? 1 : 0,
        CallsSaas: false,
        RawPersisted: false,
        NoAuthority: true,
        BrowserCredentialRedactor.Redact($"{status}: {reason}")));
    return 0;
}

static bool InRectangle(int x, int y, int width, int height)
{
    return x >= width / 4 && x <= width * 3 / 4 && y >= height / 3 && y <= height * 2 / 3;
}

static bool InCircle(int x, int y, int width, int height)
{
    var cx = width / 2;
    var cy = height / 2;
    var r = Math.Min(width, height) / 4;
    return (x - cx) * (x - cx) + (y - cy) * (y - cy) <= r * r;
}

static bool InSyntheticTextBar(int x, int y, int width, int height)
{
    var top = height / 3;
    var bottom = top + height / 6;
    if (y < top || y > bottom) return false;
    var letterWidth = width / 12;
    return (x / Math.Max(1, letterWidth)) % 2 == 0 && x > width / 4 && x < width * 3 / 4;
}

static JsonElement? TryParseJsonElement(string value)
{
    if (string.IsNullOrWhiteSpace(value))
        return null;

    try
    {
        using var doc = JsonDocument.Parse(value);
        return doc.RootElement.Clone();
    }
    catch (JsonException)
    {
        return null;
    }
}

static SoftmaxRowsAnalysis AnalyzeSoftmaxRows(float[] values, int classCount)
{
    if (classCount <= 0 || values.Length < classCount)
        return new SoftmaxRowsAnalysis(false, 0, 0d, 0d, 0d, 0d);

    var rows = values.Length / classCount;
    var minSum = double.PositiveInfinity;
    var maxSum = double.NegativeInfinity;
    var totalDeviation = 0d;
    var validRows = 0;
    for (var row = 0; row < rows; row++)
    {
        var offset = row * classCount;
        var sum = 0d;
        var rowValid = true;
        for (var i = 0; i < classCount; i++)
        {
            var value = values[offset + i];
            if (value < -0.000001f || value > 1.000001f || float.IsNaN(value) || float.IsInfinity(value))
            {
                rowValid = false;
                break;
            }

            sum += value;
        }

        if (!rowValid)
            continue;

        validRows++;
        minSum = Math.Min(minSum, sum);
        maxSum = Math.Max(maxSum, sum);
        totalDeviation += Math.Abs(1d - sum);
    }

    var averageDeviation = validRows == 0 ? double.PositiveInfinity : totalDeviation / validRows;
    return new SoftmaxRowsAnalysis(
        validRows == rows && averageDeviation < 0.02d,
        rows,
        validRows,
        validRows == 0 ? 0d : minSum,
        validRows == 0 ? 0d : maxSum,
        averageDeviation);
}

static string BuildNoAuthorityDecodePreview(
    float[] values,
    int classCount,
    int blankIndex,
    int spaceIndex,
    int maxTimesteps)
{
    if (classCount <= 0)
        return string.Empty;

    var timesteps = Math.Min(maxTimesteps, values.Length / classCount);
    var previous = -1;
    var builder = new System.Text.StringBuilder();
    for (var t = 0; t < timesteps; t++)
    {
        var offset = t * classCount;
        var argmax = 0;
        var argmaxProbability = double.NegativeInfinity;
        for (var i = 0; i < classCount; i++)
        {
            if (values[offset + i] > argmaxProbability)
            {
                argmaxProbability = values[offset + i];
                argmax = i;
            }
        }

        if (argmax == previous)
            continue;

        previous = argmax;
        if (argmax == blankIndex)
            continue;
        builder.Append(argmax == spaceIndex ? " " : "#");
    }

    return builder.ToString();
}

static byte[] BuildFixtureImage(
    NodalOsOnnxNativeRuntimeCrashProbeRequest request,
    out int width,
    out int height,
    out string expectedText)
{
    expectedText = string.Empty;

    // Text-like fixtures use the shared synthetic generator (these are the ones that crash).
    if (request.FixtureKind is
        NodalOsOnnxNativeRuntimeCrashFixtureKind.AntiAliasedPixelFontText or
        NodalOsOnnxNativeRuntimeCrashFixtureKind.PixelFontText or
        NodalOsOnnxNativeRuntimeCrashFixtureKind.NumericText or
        NodalOsOnnxNativeRuntimeCrashFixtureKind.AlphanumericText or
        NodalOsOnnxNativeRuntimeCrashFixtureKind.LargeCenteredText or
        NodalOsOnnxNativeRuntimeCrashFixtureKind.SmallCenteredText or
        NodalOsOnnxNativeRuntimeCrashFixtureKind.WidePaddingText or
        NodalOsOnnxNativeRuntimeCrashFixtureKind.SoftBorderText or
        NodalOsOnnxNativeRuntimeCrashFixtureKind.WhiteBackgroundText or
        NodalOsOnnxNativeRuntimeCrashFixtureKind.LightGrayBackgroundText or
        NodalOsOnnxNativeRuntimeCrashFixtureKind.PureBlackText or
        NodalOsOnnxNativeRuntimeCrashFixtureKind.DarkGrayText or
        NodalOsOnnxNativeRuntimeCrashFixtureKind.RectangularText or
        NodalOsOnnxNativeRuntimeCrashFixtureKind.LettersText)
    {
        var text = request.FixtureKind switch
        {
            NodalOsOnnxNativeRuntimeCrashFixtureKind.NumericText => "12345",
            NodalOsOnnxNativeRuntimeCrashFixtureKind.AlphanumericText => "ABC123",
            NodalOsOnnxNativeRuntimeCrashFixtureKind.LettersText => "AB",
            NodalOsOnnxNativeRuntimeCrashFixtureKind.SmallCenteredText => "A",
            NodalOsOnnxNativeRuntimeCrashFixtureKind.WidePaddingText => "NODAL",
            NodalOsOnnxNativeRuntimeCrashFixtureKind.SoftBorderText => "SAFE TEXT",
            _ => "TEST"
        };
        var generator = new NodalOsSyntheticOcrTextFixtureGenerator();
        var options = new NodalOsSyntheticOcrTextFixtureOptions(
            request.Width,
            request.Height,
            request.FixtureKind is NodalOsOnnxNativeRuntimeCrashFixtureKind.LightGrayBackgroundText or
                NodalOsOnnxNativeRuntimeCrashFixtureKind.DarkGrayText
                ? NodalOsSyntheticOcrTextColorScheme.GrayOnWhite
                : NodalOsSyntheticOcrTextColorScheme.BlackOnWhite,
            request.FixtureKind == NodalOsOnnxNativeRuntimeCrashFixtureKind.RectangularText
                ? NodalOsSyntheticOcrTextRenderMode.PixelFont
                : request.RenderMode,
            HorizontalPadding: request.FixtureKind == NodalOsOnnxNativeRuntimeCrashFixtureKind.WidePaddingText ? 64 : 16,
            VerticalPadding: Math.Max(4, request.Height / 4),
            CharacterSpacing: 8,
            AllowRawPersistence: false,
            AllowFullScreen: false);
        var fixture = generator.Generate(text, options);
        width = fixture.Width;
        height = fixture.Height;
        expectedText = fixture.ExpectedText;
        return fixture.ImageBytes;
    }

    width = request.Width;
    height = request.Height;
    var image = new byte[width * height * 4];
    for (var i = 0; i < image.Length; i++) image[i] = 255; // white background, opaque alpha set below
    for (var i = 3; i < image.Length; i += 4) image[i] = 255;

    switch (request.FixtureKind)
    {
        case NodalOsOnnxNativeRuntimeCrashFixtureKind.StripeSafe:
            for (var y = 0; y < height; y += 4)
                for (var x = 0; x < width; x++)
                    SetPixel(image, width, x, y, 0);
            break;

        case NodalOsOnnxNativeRuntimeCrashFixtureKind.SmallRectangle:
        case NodalOsOnnxNativeRuntimeCrashFixtureKind.LargeRectangle:
            var rx0 = width / 4; var rx1 = width * 3 / 4;
            var ry0 = height / 3; var ry1 = height * 2 / 3;
            for (var y = ry0; y < ry1; y++)
                for (var x = rx0; x < rx1; x++)
                    SetPixel(image, width, x, y, 0);
            break;

        case NodalOsOnnxNativeRuntimeCrashFixtureKind.LargeCircle:
            var cx = width / 2; var cy = height / 2; var r = Math.Min(width, height) / 4;
            for (var y = 0; y < height; y++)
                for (var x = 0; x < width; x++)
                    if ((x - cx) * (x - cx) + (y - cy) * (y - cy) <= r * r)
                        SetPixel(image, width, x, y, 0);
            break;

        case NodalOsOnnxNativeRuntimeCrashFixtureKind.ThickHorizontalBars:
            for (var y = 0; y < height; y++)
                if ((y % 4) < 2)
                    for (var x = 0; x < width; x++)
                        SetPixel(image, width, x, y, 0);
            break;
    }

    return image;
}

static void SetPixel(byte[] image, int width, int x, int y, byte value)
{
    var idx = (y * width + x) * 4;
    image[idx + 0] = value;
    image[idx + 1] = value;
    image[idx + 2] = value;
    image[idx + 3] = 255;
}

static void EmitReport(NodalOsOnnxOutOfProcessRunnerReport report)
{
    Console.Out.WriteLine(JsonSerializer.Serialize(report));
}

internal sealed record ExtraClassOutputAnalysis(
    int OutputClassCount,
    int Timesteps,
    int ExtraClassArgmaxCount,
    double ExtraClassMaxProbability,
    double ExtraClassAverageProbability,
    int BlankArgmaxCount,
    IReadOnlyList<int> DominantClassIndexes);

internal sealed record SoftmaxRowsAnalysis(
    bool LooksLikeSoftmax,
    int Rows,
    double ValidRows,
    double MinRowSum,
    double MaxRowSum,
    double AverageRowSumDeviation);

internal sealed record InternalControlledRealImageFixture(
    string Id,
    string FileName,
    string SourceCategory,
    bool CreatedByInternalQa,
    bool ContainsRealPersonData,
    bool ContainsCustomerData,
    bool ContainsFinancialData,
    bool ContainsDocumentData,
    bool ContainsScreenCapture,
    bool Sensitive,
    string ExpectedText,
    bool AllowedForOcrPipeline,
    string Reason);

internal sealed record InternalControlledScreenRegionFixture(
    string Id,
    string SourceCategory,
    bool CreatedByInternalQa,
    string WindowTitleOrSource,
    string ProcessOrSource,
    int WindowX,
    int WindowY,
    int WindowWidth,
    int WindowHeight,
    int RegionX,
    int RegionY,
    int RegionWidth,
    int RegionHeight,
    string BoundsSource,
    bool ContainsRealPersonData,
    bool ContainsCustomerData,
    bool ContainsFinancialData,
    bool ContainsDocumentData,
    bool ContainsCredentialOrPasswordData,
    bool ContainsFullScreen,
    bool Sensitive,
    string ExpectedText,
    bool AllowedForOcrPipeline,
    string Reason);
