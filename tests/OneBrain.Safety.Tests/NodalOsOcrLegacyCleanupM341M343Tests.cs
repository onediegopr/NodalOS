using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("OcrLegacyCleanup")]
[TestCategory("OcrAssistedVerificationFixtures")]
[TestCategory("OcrAssistedVerification")]
[TestCategory("AssistedVerificationPolicy")]
[TestCategory("OcrFsmObservation")]
[TestCategory("OcrEvidenceIntegration")]
[TestCategory("OcrVisionNoAuthority")]
[TestCategory("OcrVisionPrivacy")]
[TestCategory("OfficialSpaceToken")]
public sealed class NodalOsOcrLegacyCleanupM341M343Tests
{
    private static readonly string RepoRoot = AppDomain.CurrentDomain.BaseDirectory.Contains("bin\\Debug") ||
                                              AppDomain.CurrentDomain.BaseDirectory.Contains("bin\\Release")
        ? Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", ".."))
        : AppDomain.CurrentDomain.BaseDirectory;

    [TestMethod]
    public void LegacyPythonWorkerSurface_IsDeprecated()
    {
        AssertObsolete(typeof(NodalOsPaddleOcrLocalWorkerAdapter));
        AssertObsolete(typeof(NodalOsPaddleOcrResultNormalizer));
        AssertObsolete(typeof(NodalOsPaddleOcrSyntheticRunService));
        AssertObsolete(typeof(NodalOsPaddleOcrWorkerRequest));
        AssertObsolete(typeof(NodalOsPaddleOcrWorkerResponse));
    }

    [TestMethod]
    public void OnnxRuntimeVersionExperimentSurface_IsDeprecated()
    {
        AssertObsolete(typeof(NodalOsOnnxRuntimeVersionExperimentPlanner));
        AssertObsolete(typeof(NodalOsOnnxRuntimeVersionDecisionService));
    }

    [TestMethod]
    public void ActiveOcrPath_DoesNotUsePythonWorker()
    {
        var activeFiles = new[]
        {
            SourcePath("src", "OneBrain.BrowserExecutor.Cdp", "NodalOsLowRiskOcrObservationServices.cs"),
            SourcePath("src", "OneBrain.BrowserExecutor.Cdp", "NodalOsOcrEvidenceIntegrationServices.cs"),
            SourcePath("src", "OneBrain.BrowserExecutor.Cdp", "NodalOsOcrFsmObservationServices.cs"),
            SourcePath("src", "OneBrain.BrowserExecutor.Cdp", "NodalOsAssistedVerificationServices.cs"),
            SourcePath("tools", "onnx-ocr-probe-runner", "Program.cs")
        };

        foreach (var file in activeFiles)
        {
            var text = File.ReadAllText(file);
            Assert.IsFalse(text.Contains("NodalOsPaddleOcrLocalWorkerAdapter", StringComparison.Ordinal));
            Assert.IsFalse(text.Contains("paddleocr_worker.py", StringComparison.Ordinal));
        }
    }

    [TestMethod]
    public void ActiveOcrPath_UsesPinnedOnnxDotNetRuntime()
    {
        var csproj = File.ReadAllText(SourcePath("src", "OneBrain.BrowserExecutor.Cdp", "OneBrain.BrowserExecutor.Cdp.csproj"));
        StringAssert.Contains(csproj, "Microsoft.ML.OnnxRuntime");
        StringAssert.Contains(csproj, "Version=\"1.22.1\"");
    }

    [TestMethod]
    public void HistoricalArtifacts_AreReferenceOnly_NotWrittenByActiveCode()
    {
        var exactNames = new[]
        {
            "ocr-vision-evaluation-summary.json",
            "synthetic-worker-run-summary.json"
        };

        var activeFiles = Directory
            .GetFiles(SourcePath("src"), "*.*", SearchOption.AllDirectories)
            .Concat(Directory.GetFiles(SourcePath("tools"), "*.*", SearchOption.AllDirectories))
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase) &&
                           !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase) &&
                           Path.GetExtension(path) is ".cs" or ".ps1" or ".py" or ".csproj");

        foreach (var file in activeFiles)
        {
            var text = File.ReadAllText(file);
            foreach (var name in exactNames)
                Assert.IsFalse(text.Contains(name, StringComparison.Ordinal), $"Active code unexpectedly references tracked artifact {name} in {file}");
        }
    }

    [TestMethod]
    public void AssistedVerificationPolicy_RemainsReadOnlyAndNoAuthority()
    {
        var summary = new NodalOsAssistedVerificationFixtureSet().Execute(
            new NodalOsAssistedVerificationFixtureSet().CreateDefaultFixtureCases());
        var verified = summary.Fixtures.Single(f => f.FixtureId == "assisted-qa-pvc-wall-known-fixture-pass");

        Assert.AreEqual(NodalOsAssistedVerificationDecision.VerifiedLowRisk, verified.Decision);
        Assert.IsFalse(verified.ActionsAllowed);
        Assert.IsFalse(verified.CanProduceActionPlan);
        Assert.IsFalse(verified.CanProduceSafeAction);
        Assert.IsFalse(verified.CanApproveClick);
        Assert.IsFalse(verified.CanApproveSubmit);
        Assert.IsFalse(verified.CanApproveSend);
        Assert.IsFalse(verified.CanApproveDelete);
        Assert.IsFalse(verified.CanApprovePay);
        Assert.IsFalse(verified.CanApproveSign);
        Assert.IsTrue(verified.NoAuthority);
        Assert.IsTrue(verified.EvidenceOnly);
    }

    [TestMethod]
    public void Artifact_ValidatesM343Flags()
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(SourcePath(
            "artifacts", "ocr-vision-onnx", "m343", "ocr-line-cleanup-summary.json")));
        var root = doc.RootElement;

        Assert.AreEqual("M341-M343", root.GetProperty("milestone").GetString());
        Assert.AreEqual("OCR_LINE_CLEAN_WITH_LEGACY_DEPRECATION_NOT_REMOVAL", root.GetProperty("readinessDecision").GetString());
        Assert.AreEqual("OCR_LINE_AUDIT_PASS_WITH_MINOR_CLEANUP", root.GetProperty("ocrLineAuditDecision").GetString());
        Assert.IsTrue(root.GetProperty("pythonWorkerReferenceGraphCompleted").GetBoolean());
        Assert.IsFalse(root.GetProperty("pythonWorkerRemoved").GetBoolean());
        Assert.IsTrue(root.GetProperty("pythonWorkerDeprecated").GetBoolean());
        Assert.IsFalse(root.GetProperty("onnxRuntimeExperimentSimplified").GetBoolean());
        Assert.IsTrue(root.GetProperty("onnxRuntimeExperimentDeprecated").GetBoolean());
        Assert.IsTrue(root.GetProperty("dirtyArtifactHygieneCompleted").GetBoolean());
        Assert.IsTrue(root.GetProperty("m177DirtyResolved").GetBoolean());
        Assert.IsTrue(root.GetProperty("m183DirtyResolved").GetBoolean());
        Assert.IsTrue(root.GetProperty("ocrFinalMasterSummaryCreated").GetBoolean());
        Assert.IsTrue(root.GetProperty("noAuthorityPreserved").GetBoolean());
    }

    [TestMethod]
    public void Git_DoesNotTrackOnnxModels_OrGitignoredDictionaries_ForM343()
    {
        Assert.AreEqual(string.Empty, RunGit("ls-files", "*.onnx").Trim());
        Assert.AreEqual(string.Empty, RunGit("ls-files", "tools/ocr-worker/models/onnx/dictionaries/*").Trim());
    }

    private static void AssertObsolete(MemberInfo member) =>
        Assert.IsTrue(member.GetCustomAttribute<ObsoleteAttribute>() is not null, $"{member.Name} should be marked obsolete.");

    private static string SourcePath(params string[] parts) => Path.Combine(new[] { RepoRoot }.Concat(parts).ToArray());

    private static string RunGit(params string[] args)
    {
        var psi = new ProcessStartInfo("git")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = RepoRoot
        };

        foreach (var arg in args)
            psi.ArgumentList.Add(arg);

        using var process = Process.Start(psi);
        Assert.IsNotNull(process);
        var stdout = process.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();
        Assert.IsTrue(process.WaitForExit(10000), stderr);
        Assert.AreEqual(0, process.ExitCode, stderr);
        return stdout;
    }
}
