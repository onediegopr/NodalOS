using System.Text.Json;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

// M210 — Isolated ONNX OCR probe runner.
// This process is launched by NodalOsOnnxOutOfProcessGuard to attempt probes that may crash the
// native ONNX Runtime. If the native runtime crashes, THIS process dies (contained); the parent
// guard maps the crash to a controlled result. Strictly synthetic / redacted / non-sensitive only.
//
// Modes:
//   --self-test <safe|crash|abort|nonzero|timeout|garbage>   Deterministic outcomes (no ONNX load).
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

if (options.ContainsKey("probe"))
{
    return RunProbe(options);
}

Console.Error.WriteLine("usage: --self-test <mode> | --guard-probe --repo-root <dir> [--fixture <kind>] [--width <w>] [--height <h>] | --probe --repo-root <dir> [--request <file>]");
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
        ? root
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
        ? root
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
