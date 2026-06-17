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

if (options.ContainsKey("recognizer-runtime-child"))
{
    return RunRecognizerRuntimeChild(options);
}

if (options.ContainsKey("probe"))
{
    return RunProbe(options);
}

Console.Error.WriteLine("usage: --self-test <mode> | --guard-probe --repo-root <dir> [--fixture <kind>] [--width <w>] [--height <h>] | --detector-crash-probe --repo-root <dir> | --detector-crash-child --repo-root <dir> --tensor <kind> --session-option <kind> | --handoff-crash-probe --repo-root <dir> | --recognizer-runtime-probe --repo-root <dir> | --probe --repo-root <dir> [--request <file>]");
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
    var runner = Environment.ProcessPath ?? Path.Combine(AppContext.BaseDirectory, "OneBrain.Tools.OnnxOcrProbeRunner.exe");
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

    var modelPath = Path.GetFullPath(Path.Combine(repoRoot, "tools", "ocr-worker", "models", "onnx", "ch_PP-OCRv4_rec.onnx"));
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
        ? root
        : Directory.GetCurrentDirectory();
    var runner = Environment.ProcessPath ?? Path.Combine(AppContext.BaseDirectory, "OneBrain.Tools.OnnxOcrProbeRunner.exe");
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
            ["--recognizer-runtime-child", "--repo-root", repoRoot, "--tensor", tensorKind.ToString()],
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

static int RunRecognizerRuntimeChild(Dictionary<string, string> options)
{
    var repoRoot = options.TryGetValue("repo-root", out var root) && Directory.Exists(root)
        ? root
        : Directory.GetCurrentDirectory();
    var tensorKind = options.TryGetValue("tensor", out var tensorText) &&
                     Enum.TryParse<NodalOsRecognizerRuntimeTensorKind>(tensorText, ignoreCase: true, out var parsedTensor)
        ? parsedTensor
        : NodalOsRecognizerRuntimeTensorKind.Zero;

    var shape = new[] { 1, 3, 32, 320 };
    var tensor = BuildRecognizerTensor(tensorKind, shape);
    var stats = NodalOsDetectorRecognizerCompatibilityDiagnosisBuilder.CalculateStats(tensor, shape, "NCHW", "RGB");
    if (stats.HasNaN || stats.HasInfinity || tensor.Length != shape.Aggregate(1, (a, b) => a * b))
    {
        EmitReport(new NodalOsOnnxOutOfProcessRunnerReport(
            $"rec-runtime-{Guid.NewGuid():N}", "RecognizerTensorPreparation", "InvalidTensorShape", null, null,
            false, false, true, "invalid recognizer tensor stats/shape before runtime"));
        return 0;
    }

    var modelPath = Path.GetFullPath(Path.Combine(repoRoot, "tools", "ocr-worker", "models", "onnx", "ch_PP-OCRv4_rec.onnx"));
    Console.Error.WriteLine($"stage=model-file model={modelPath} exists={File.Exists(modelPath)}");
    Console.Error.WriteLine($"stage=tensor tensor={tensorKind} shape=[{string.Join(",", shape)}] min={stats.Min:R} max={stats.Max:R} mean={stats.Mean:R}");

    using var session = new InferenceSession(modelPath);
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

    var dictionary = new NodalOsOcrDictionaryCompatibilityService().Evaluate(
        new NodalOsOcrDictionaryCompatibilityService().CreateCurrentAsciiManifest(verified: true),
        classCount);
    var status = dictionary.Status == NodalOsOcrDictionaryCompatibilityStatus.ClassCountMismatch
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
        $"recognizer run succeeded; tensor={tensorKind}; outputs={string.Join(";", outputShapes)}; classCount={classCount}; dictionary={dictionary.Status}"));
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
