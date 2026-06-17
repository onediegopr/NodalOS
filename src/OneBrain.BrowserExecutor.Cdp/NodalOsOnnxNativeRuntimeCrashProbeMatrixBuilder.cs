using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.BrowserExecutor.Cdp;

// M209 — ONNX native runtime crash reproduction matrix builder.
// Produces a controlled matrix of fixture x dimension x stage probes. The matrix classifies
// each probe as safe in-process, quarantined (guard-only), or blocked before runtime.
// The builder NEVER executes the native runtime; it only models the probe plan.
public sealed class NodalOsOnnxNativeRuntimeCrashProbeMatrixBuilder
{
    // Dimensions to probe (M209 task 4): fixed shapes plus detector/recognizer expected shapes.
    private static readonly (int Width, int Height)[] DefaultDimensions =
    {
        (128, 64),
        (256, 96),
        (320, 128),
        (640, 640),   // detector-friendly square (PaddleOCR det static-ish)
        (320, 48)     // recognizer expected strip
    };

    private static readonly NodalOsOnnxNativeRuntimeCrashFixtureKind[] DefaultFixtures =
    {
        NodalOsOnnxNativeRuntimeCrashFixtureKind.StripeSafe,
        NodalOsOnnxNativeRuntimeCrashFixtureKind.SmallRectangle,
        NodalOsOnnxNativeRuntimeCrashFixtureKind.LargeRectangle,
        NodalOsOnnxNativeRuntimeCrashFixtureKind.LargeCircle,
        NodalOsOnnxNativeRuntimeCrashFixtureKind.AntiAliasedPixelFontText,
        NodalOsOnnxNativeRuntimeCrashFixtureKind.PixelFontText,
        NodalOsOnnxNativeRuntimeCrashFixtureKind.ThickHorizontalBars,
        NodalOsOnnxNativeRuntimeCrashFixtureKind.NumericText,
        NodalOsOnnxNativeRuntimeCrashFixtureKind.AlphanumericText
    };

    // Stages that involve the native runtime (where a crash can occur).
    private static readonly NodalOsOnnxNativeRuntimeCrashStage[] RuntimeStages =
    {
        NodalOsOnnxNativeRuntimeCrashStage.SessionCreation,
        NodalOsOnnxNativeRuntimeCrashStage.DetectionRun,
        NodalOsOnnxNativeRuntimeCrashStage.RecognitionRun
    };

    public NodalOsOnnxNativeRuntimeCrashProbeMatrix BuildDefaultMatrix()
    {
        var entries = new List<NodalOsOnnxNativeRuntimeCrashProbeMatrixEntry>();

        foreach (var fixture in DefaultFixtures)
        {
            foreach (var (width, height) in DefaultDimensions)
            {
                foreach (var stage in RuntimeStages)
                {
                    var renderMode = fixture is NodalOsOnnxNativeRuntimeCrashFixtureKind.PixelFontText
                        ? NodalOsSyntheticOcrTextRenderMode.PixelFont
                        : NodalOsSyntheticOcrTextRenderMode.AntiAliasedPixelFont;

                    var request = new NodalOsOnnxNativeRuntimeCrashProbeRequest(
                        $"probe-{fixture}-{width}x{height}-{stage}-{Guid.NewGuid():N}",
                        fixture,
                        renderMode,
                        width,
                        height,
                        stage,
                        NodalOsOcrVisionSensitivity.Low,
                        FullScreen: false,
                        Sensitive: false,
                        OriginalRawPersisted: false,
                        Synthetic: true,
                        NoAuthority: true,
                        RunOutOfProcess: fixture is not (
                            NodalOsOnnxNativeRuntimeCrashFixtureKind.StripeSafe or
                            NodalOsOnnxNativeRuntimeCrashFixtureKind.SmallRectangle or
                            NodalOsOnnxNativeRuntimeCrashFixtureKind.LargeRectangle or
                            NodalOsOnnxNativeRuntimeCrashFixtureKind.LargeCircle));

                    entries.Add(BuildEntry(request));
                }
            }
        }

        return Assemble(entries);
    }

    // Builds a single entry, enforcing the pre-runtime safety gate. Unsafe declared requests
    // (full-screen / sensitive / raw-persisted / non-synthetic) are blocked before any runtime.
    public NodalOsOnnxNativeRuntimeCrashProbeMatrixEntry BuildEntry(NodalOsOnnxNativeRuntimeCrashProbeRequest request)
    {
        var safety = request.ClassifySafety();

        if (!request.MetadataValid)
        {
            return new NodalOsOnnxNativeRuntimeCrashProbeMatrixEntry(
                $"entry-{Guid.NewGuid():N}",
                request,
                NodalOsOnnxNativeRuntimeCrashFixtureSafety.BlockedBeforeRuntime,
                NodalOsOnnxNativeRuntimeCrashProbeStatus.UnsupportedFixture,
                AllowedInProcess: false,
                RequiresGuard: false,
                BlockedBeforeRuntime: true,
                BrowserCredentialRedactor.Redact("probe metadata invalid; blocked before runtime"));
        }

        return safety switch
        {
            NodalOsOnnxNativeRuntimeCrashFixtureSafety.BlockedBeforeRuntime => new NodalOsOnnxNativeRuntimeCrashProbeMatrixEntry(
                $"entry-{Guid.NewGuid():N}",
                request,
                safety,
                NodalOsOnnxNativeRuntimeCrashProbeStatus.UnsupportedFixture,
                AllowedInProcess: false,
                RequiresGuard: false,
                BlockedBeforeRuntime: true,
                BrowserCredentialRedactor.Redact("unsafe fixture (full-screen/sensitive/raw/non-synthetic) blocked before runtime")),

            NodalOsOnnxNativeRuntimeCrashFixtureSafety.SafeInProcess => new NodalOsOnnxNativeRuntimeCrashProbeMatrixEntry(
                $"entry-{Guid.NewGuid():N}",
                request,
                safety,
                NodalOsOnnxNativeRuntimeCrashProbeStatus.NoTextDetected,
                AllowedInProcess: true,
                RequiresGuard: false,
                BlockedBeforeRuntime: false,
                BrowserCredentialRedactor.Redact("safe shape fixture proven not to crash native host; allowed in-process diagnostic")),

            _ => new NodalOsOnnxNativeRuntimeCrashProbeMatrixEntry(
                $"entry-{Guid.NewGuid():N}",
                request,
                safety,
                NodalOsOnnxNativeRuntimeCrashProbeStatus.SkippedQuarantined,
                AllowedInProcess: false,
                RequiresGuard: true,
                BlockedBeforeRuntime: false,
                BrowserCredentialRedactor.Redact("text-like fixture may crash native host; quarantined to out-of-process guard only"))
        };
    }

    private static NodalOsOnnxNativeRuntimeCrashProbeMatrix Assemble(
        List<NodalOsOnnxNativeRuntimeCrashProbeMatrixEntry> entries)
    {
        return new NodalOsOnnxNativeRuntimeCrashProbeMatrix(
            $"onnx-crash-matrix-{Guid.NewGuid():N}",
            entries,
            entries.Count(e => e.FixtureSafety == NodalOsOnnxNativeRuntimeCrashFixtureSafety.SafeInProcess),
            entries.Count(e => e.FixtureSafety == NodalOsOnnxNativeRuntimeCrashFixtureSafety.QuarantinedOutOfProcessOnly),
            entries.Count(e => e.FixtureSafety == NodalOsOnnxNativeRuntimeCrashFixtureSafety.BlockedBeforeRuntime),
            NoSaas: true,
            NoRawPersistence: entries.All(e => !e.Request.OriginalRawPersisted),
            NoFullScreen: entries.All(e => !e.Request.FullScreen),
            NoSensitive: entries.All(e => !e.Request.Sensitive),
            NoAuthority: entries.All(e => e.Request.NoAuthority),
            DateTimeOffset.UtcNow);
    }
}
