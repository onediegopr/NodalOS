using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("OnnxNativeRuntimeCrash")]
[TestCategory("OnnxNativeCrashProbe")]
[TestCategory("OnnxRuntimeCrashIsolation")]
[TestCategory("OnnxRuntimeDotNet")]
[TestCategory("OcrVisionNoAuthority")]
[TestCategory("OcrVisionPrivacy")]
[TestCategory("PixelRedaction")]
[TestCategory("PrivatePreviewReadiness")]
[TestCategory("LocalPreviewReleaseCandidate")]
[TestCategory("NodalOsNamingAudit")]
[TestCategory("BrowserRuntimePhaseGate")]
public sealed class NodalOsOnnxNativeRuntimeCrashProbeMatrixM209Tests
{
    private static NodalOsOnnxNativeRuntimeCrashProbeRequest SafeRequest(
        NodalOsOnnxNativeRuntimeCrashFixtureKind kind = NodalOsOnnxNativeRuntimeCrashFixtureKind.StripeSafe,
        int width = 128,
        int height = 64) =>
        new(
            $"probe-{Guid.NewGuid():N}",
            kind,
            NodalOsSyntheticOcrTextRenderMode.AntiAliasedPixelFont,
            width,
            height,
            NodalOsOnnxNativeRuntimeCrashStage.SessionCreation,
            NodalOsOcrVisionSensitivity.Low,
            FullScreen: false,
            Sensitive: false,
            OriginalRawPersisted: false,
            Synthetic: true,
            NoAuthority: true,
            RunOutOfProcess: false);

    [TestMethod]
    public void CrashProbeRequest_ValidatesFixtureMetadata()
    {
        Assert.IsTrue(SafeRequest().MetadataValid);

        var badId = SafeRequest() with { ProbeId = "" };
        Assert.IsFalse(badId.MetadataValid);

        var badDims = SafeRequest() with { Width = 0 };
        Assert.IsFalse(badDims.MetadataValid);

        var notSynthetic = SafeRequest() with { Synthetic = false };
        Assert.IsFalse(notSynthetic.MetadataValid);

        var hasAuthority = SafeRequest() with { NoAuthority = false };
        Assert.IsFalse(hasAuthority.MetadataValid);
    }

    [TestMethod]
    public void CrashProbeResult_MapsNativeCrashToBlockedStatus()
    {
        Assert.AreEqual(NodalOsOnnxNativeRuntimeCrashProbeStatus.NativeRuntimeCrash,
            NodalOsOnnxNativeRuntimeCrashProbeResult.MapCrashKind(NodalOsOnnxNativeRuntimeCrashKind.NativeAccessViolation));
        Assert.AreEqual(NodalOsOnnxNativeRuntimeCrashProbeStatus.NativeRuntimeCrash,
            NodalOsOnnxNativeRuntimeCrashProbeResult.MapCrashKind(NodalOsOnnxNativeRuntimeCrashKind.NativeAbort));
        Assert.AreEqual(NodalOsOnnxNativeRuntimeCrashProbeStatus.ProcessCrashed,
            NodalOsOnnxNativeRuntimeCrashProbeResult.MapCrashKind(NodalOsOnnxNativeRuntimeCrashKind.ProcessExitNonZero));
        Assert.AreEqual(NodalOsOnnxNativeRuntimeCrashProbeStatus.TimedOut,
            NodalOsOnnxNativeRuntimeCrashProbeResult.MapCrashKind(NodalOsOnnxNativeRuntimeCrashKind.TimedOut));
        Assert.AreEqual(NodalOsOnnxNativeRuntimeCrashProbeStatus.BlockedByModelRuntime,
            NodalOsOnnxNativeRuntimeCrashProbeResult.MapCrashKind(NodalOsOnnxNativeRuntimeCrashKind.InvalidOutput));
        Assert.AreEqual(NodalOsOnnxNativeRuntimeCrashProbeStatus.Passed,
            NodalOsOnnxNativeRuntimeCrashProbeResult.MapCrashKind(NodalOsOnnxNativeRuntimeCrashKind.NoCrash));
    }

    [TestMethod]
    public void CrashMatrix_IncludesSafeAndQuarantinedFixtures()
    {
        var matrix = new NodalOsOnnxNativeRuntimeCrashProbeMatrixBuilder().BuildDefaultMatrix();

        Assert.IsTrue(matrix.Entries.Count > 0);
        Assert.IsTrue(matrix.SafeFixtureCount > 0, "matrix must include safe in-process fixtures");
        Assert.IsTrue(matrix.QuarantinedFixtureCount > 0, "matrix must include quarantined text fixtures");

        Assert.IsTrue(matrix.Entries.Any(e =>
            e.Request.FixtureKind == NodalOsOnnxNativeRuntimeCrashFixtureKind.StripeSafe &&
            e.FixtureSafety == NodalOsOnnxNativeRuntimeCrashFixtureSafety.SafeInProcess));

        Assert.IsTrue(matrix.Entries.Any(e =>
            e.Request.FixtureKind == NodalOsOnnxNativeRuntimeCrashFixtureKind.PixelFontText &&
            e.FixtureSafety == NodalOsOnnxNativeRuntimeCrashFixtureSafety.QuarantinedOutOfProcessOnly &&
            e.RequiresGuard));
    }

    [TestMethod]
    public void SafeFixtures_AreInProcess_TextFixtures_RequireGuard()
    {
        var builder = new NodalOsOnnxNativeRuntimeCrashProbeMatrixBuilder();

        var safe = builder.BuildEntry(SafeRequest(NodalOsOnnxNativeRuntimeCrashFixtureKind.LargeCircle, 640, 640));
        Assert.IsTrue(safe.AllowedInProcess);
        Assert.IsFalse(safe.RequiresGuard);

        var text = builder.BuildEntry(SafeRequest(NodalOsOnnxNativeRuntimeCrashFixtureKind.AntiAliasedPixelFontText, 256, 96));
        Assert.IsFalse(text.AllowedInProcess);
        Assert.IsTrue(text.RequiresGuard);
#pragma warning disable MSTEST0017 // 'ExpectedStatus' property name confuses the arg-order analyzer; order is correct.
        Assert.AreEqual(NodalOsOnnxNativeRuntimeCrashProbeStatus.SkippedQuarantined, text.ExpectedStatus);
#pragma warning restore MSTEST0017
    }

    [TestMethod]
    public void UnsafeFixtures_AreBlockedBeforeRuntime()
    {
        var builder = new NodalOsOnnxNativeRuntimeCrashProbeMatrixBuilder();

        var fullScreen = builder.BuildEntry(SafeRequest() with { FullScreen = true });
        Assert.IsTrue(fullScreen.BlockedBeforeRuntime);
        Assert.IsFalse(fullScreen.AllowedInProcess);
        Assert.IsFalse(fullScreen.RequiresGuard);

        var sensitive = builder.BuildEntry(SafeRequest() with { Sensitive = true });
        Assert.IsTrue(sensitive.BlockedBeforeRuntime);

        var raw = builder.BuildEntry(SafeRequest() with { OriginalRawPersisted = true });
        Assert.IsTrue(raw.BlockedBeforeRuntime);

        var sensitiveSurface = builder.BuildEntry(SafeRequest() with { Sensitivity = NodalOsOcrVisionSensitivity.SensitiveSurface });
        Assert.IsTrue(sensitiveSurface.BlockedBeforeRuntime);
    }

    [TestMethod]
    public void Matrix_PreservesNoAuthority_NoSaas_NoRaw_NoFullScreen_NoSensitive()
    {
        var matrix = new NodalOsOnnxNativeRuntimeCrashProbeMatrixBuilder().BuildDefaultMatrix();
        Assert.IsTrue(matrix.NoAuthority);
        Assert.IsTrue(matrix.NoSaas);
        Assert.IsTrue(matrix.NoRawPersistence);
        Assert.IsTrue(matrix.NoFullScreen);
        Assert.IsTrue(matrix.NoSensitive);
    }

    [TestMethod]
    public void Matrix_DifferentiatesRuntimeStages()
    {
        var matrix = new NodalOsOnnxNativeRuntimeCrashProbeMatrixBuilder().BuildDefaultMatrix();
        var stages = matrix.Entries.Select(e => e.Request.Stage).Distinct().ToList();
        Assert.IsTrue(stages.Contains(NodalOsOnnxNativeRuntimeCrashStage.SessionCreation));
        Assert.IsTrue(stages.Contains(NodalOsOnnxNativeRuntimeCrashStage.DetectionRun));
        Assert.IsTrue(stages.Contains(NodalOsOnnxNativeRuntimeCrashStage.RecognitionRun));
    }
}
