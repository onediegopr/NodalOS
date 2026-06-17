using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("OnnxRuntimeVersionExperiment")]
[TestCategory("RecognizerRuntimeExperiment")]
[TestCategory("RecognizerRuntimeCompatibility")]
[TestCategory("RecognizerCompatibility")]
[TestCategory("FullOcrHandoff")]
[TestCategory("OcrDictionary")]
[TestCategory("CtcDecoder")]
[TestCategory("DetectorRuntimeCompatibility")]
[TestCategory("DetectorCrashProbe")]
[TestCategory("OnnxRuntimeCrashIsolation")]
[TestCategory("OnnxOutOfProcessGuard")]
[TestCategory("OnnxOcrProbeRunner")]
[TestCategory("OnnxModelInventory")]
[TestCategory("OnnxModelVerification")]
[TestCategory("OnnxModelReadiness")]
[TestCategory("GuardedSyntheticTextOcr")]
[TestCategory("OcrVisionNoAuthority")]
[TestCategory("OcrVisionPrivacy")]
[TestCategory("PixelRedaction")]
[TestCategory("OcrRedactionPrecondition")]
[TestCategory("PrivatePreviewReadiness")]
[TestCategory("LocalPreviewReleaseCandidate")]
[TestCategory("BrowserRuntimePhaseGate")]
public sealed class NodalOsOnnxRuntimeVersionExperimentM232M234Tests
{
    private static readonly string RepoRoot = AppDomain.CurrentDomain.BaseDirectory.Contains("bin\\Debug") ||
                                              AppDomain.CurrentDomain.BaseDirectory.Contains("bin\\Release")
        ? Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", ".."))
        : AppDomain.CurrentDomain.BaseDirectory;

    [TestMethod]
    public void ExperimentPlan_IncludesBaselineCandidates_AndIsReversible()
    {
        var project = Path.Combine("src", "OneBrain.BrowserExecutor.Cdp", "OneBrain.BrowserExecutor.Cdp.csproj");
        var plan = new NodalOsOnnxRuntimeVersionExperimentPlanner().CreateDefaultPlan(project);

        CollectionAssert.AreEqual(new[] { "1.18.1", "1.22.1", "1.23.2", "1.25.0" }, plan.CandidateVersions.ToArray());
        Assert.AreEqual("1.18.1", plan.BaselineVersion);
        Assert.IsTrue(plan.Reversible);
        Assert.IsTrue(plan.CpuProviderOnly);
        Assert.IsTrue(plan.ProductiveOcrBlocked);
        Assert.IsTrue(plan.NoAuthority);
    }

    [TestMethod]
    public void Decision_RuntimeSuccessWithDictionaryMismatch_MapsToDictionaryCompletion()
    {
        var report = Decide([
            Result("1.23.2", detectorOk: true, anyRunSucceeded: true, anyCrash: false)
        ], dictionaryMismatch: true, finalPackageVersion: "1.23.2");

        Assert.AreEqual(NodalOsOnnxRuntimeVersionDecision.ReadyForDictionaryCompletion, report.Decision);
        Assert.IsTrue(report.AnyVersionAvoidedCrash);
        Assert.IsTrue(report.DictionaryMismatchStillBlocksDecode);
        Assert.IsFalse(report.BranchLeftAtBaseline);
    }

    [TestMethod]
    public void Decision_RuntimeSuccessWithoutDictionaryMismatch_MapsToRuntimeUpgrade()
    {
        var report = Decide([
            Result("1.25.0", detectorOk: true, anyRunSucceeded: true, anyCrash: false)
        ], dictionaryMismatch: false, finalPackageVersion: "1.25.0");

        Assert.AreEqual(NodalOsOnnxRuntimeVersionDecision.ReadyForOnnxRuntimeUpgrade, report.Decision);
        Assert.IsTrue(report.AnyVersionAvoidedCrash);
    }

    [TestMethod]
    public void Decision_AllRuntimeCrash_MapsToRecognizerModelReplacement()
    {
        var report = Decide([
            Result("1.18.1", detectorOk: true, anyRunSucceeded: false, anyCrash: true),
            Result("1.22.1", detectorOk: true, anyRunSucceeded: false, anyCrash: true),
            Result("1.23.2", detectorOk: true, anyRunSucceeded: false, anyCrash: true),
            Result("1.25.0", detectorOk: true, anyRunSucceeded: false, anyCrash: true)
        ]);

        Assert.AreEqual(NodalOsOnnxRuntimeVersionDecision.ReadyForRecognizerModelReplacement, report.Decision);
        Assert.IsFalse(report.AnyVersionAvoidedCrash);
        Assert.IsTrue(report.BranchLeftAtBaseline);
    }

    [TestMethod]
    public void Decision_RestoreOrBuildFailure_MapsToRuntimeRestoreBlock()
    {
        var restoreReport = Decide([Result("1.22.1", restoreOk: false)]);
        var buildReport = Decide([Result("1.23.2", buildOk: false)]);

        Assert.AreEqual(NodalOsOnnxRuntimeVersionDecision.BlockedByRuntimeRestore, restoreReport.Decision);
        Assert.AreEqual(NodalOsOnnxRuntimeVersionDecision.BlockedByRuntimeRestore, buildReport.Decision);
    }

    [TestMethod]
    public void Decision_BlocksShadowProductiveOcr_AndPreservesNoAuthority()
    {
        var report = Decide([
            Result("1.18.1", detectorOk: true, anyRunSucceeded: false, anyCrash: true)
        ]);

        Assert.IsTrue(report.ShadowModeBlocked);
        Assert.IsTrue(report.ProductiveOcrBlocked);
        Assert.IsTrue(report.NoAuthority);
        Assert.IsTrue(report.DetectorSanityRequired);
    }

    [TestMethod]
    public void Report_Artifact_ClaudePrompt_Adr_Exist_ForM234()
    {
        Assert.IsTrue(File.Exists(Path.Combine(RepoRoot, "docs", "reports", "onnx-runtime-version-experiment-m234.md")));
        Assert.IsTrue(File.Exists(Path.Combine(RepoRoot, "artifacts", "ocr-vision-onnx", "m234", "onnx-runtime-version-experiment-summary.json")));
        Assert.IsTrue(File.Exists(Path.Combine(RepoRoot, "docs", "audits", "claude-onnx-runtime-version-experiment-audit-m234.md")));
        Assert.IsTrue(File.Exists(Path.Combine(RepoRoot, "docs", "adr", "onnx-runtime-version-decision-m232-m234.md")));
    }

    private static NodalOsOnnxRuntimeVersionDecisionReport Decide(
        IReadOnlyList<NodalOsOnnxRuntimeVersionExperimentResult> results,
        bool dictionaryMismatch = true,
        string finalPackageVersion = "1.18.1") =>
        new NodalOsOnnxRuntimeVersionDecisionService().Decide(
            results,
            baselineVersion: "1.18.1",
            finalPackageVersion,
            detectorSanityRequired: true,
            dictionaryMismatch,
            parentSurvived: true,
            noRawPersistence: true,
            noAuthority: true);

    private static NodalOsOnnxRuntimeVersionExperimentResult Result(
        string version,
        bool restoreOk = true,
        bool buildOk = true,
        bool detectorOk = false,
        bool anyRunSucceeded = false,
        bool anyCrash = false) =>
        new(
            version,
            version,
            restoreOk ? version : "",
            restoreOk ? $"Microsoft.ML.OnnxRuntime {version}" : "",
            restoreOk,
            buildOk,
            detectorOk,
            RecognizerZeroSucceeded: anyRunSucceeded,
            RecognizerOnesSucceeded: anyRunSucceeded,
            RecognizerGradientSucceeded: anyRunSucceeded,
            RecognizerCropSucceeded: anyRunSucceeded,
            anyRunSucceeded,
            anyCrash,
            anyCrash ? -1073741676 : null,
            anyCrash ? "0xC0000094" : null,
            anyCrash ? "RecognitionRun/session.Run" : "",
            ParentSurvived: true,
            TempFilesCleaned: true,
            RawPersisted: false,
            CallsSaas: false,
            NoAuthority: true,
            !restoreOk ? NodalOsOnnxRuntimeVersionExperimentStatus.RuntimeVersionRestoreFailed :
            !buildOk ? NodalOsOnnxRuntimeVersionExperimentStatus.BuildFailed :
            anyRunSucceeded ? NodalOsOnnxRuntimeVersionExperimentStatus.RecognizerRunSucceeded :
            anyCrash ? NodalOsOnnxRuntimeVersionExperimentStatus.RecognizerNativeRuntimeCrashContained :
            NodalOsOnnxRuntimeVersionExperimentStatus.NotRun,
            anyRunSucceeded ? "recognizer run succeeded" : "test result");
}
