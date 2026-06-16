using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("PaddleOcrInstallReadiness")]
[TestCategory("PaddleOcrRuntimeReadiness")]
[TestCategory("PaddleOcrRollback")]
[TestCategory("PaddleOcrExperimental")]
[TestCategory("OcrVisionActivationGate")]
[TestCategory("OcrVisionNoAuthority")]
[TestCategory("OcrVisionPrivacy")]
[TestCategory("BrowserGroundingSnapshot")]
[TestCategory("PrivatePreviewReadiness")]
[TestCategory("LocalPreviewReleaseCandidate")]
[TestCategory("NodalOsNamingAudit")]
[TestCategory("BrowserRuntimePhaseGate")]
public sealed class NodalOsPaddleOcrRuntimeM191Tests
{
    [TestMethod]
    public void Inspector_ReportsEnvironmentHonestly()
    {
        var inspector = new NodalOsPaddleOcrRuntimeInspector();
        var env = inspector.Inspect();

        Assert.IsFalse(string.IsNullOrWhiteSpace(env.EnvironmentId));
        Assert.IsFalse(string.IsNullOrWhiteSpace(env.OsPlatform));
        Assert.IsFalse(string.IsNullOrWhiteSpace(env.Architecture));
        // Python is present in this environment (validated at task start).
        Assert.IsTrue(env.PythonAvailable);
        Assert.IsTrue(env.PipAvailable);
        Assert.IsTrue(env.VenvAvailable);
        // PaddleOCR is not installed globally.
        Assert.IsFalse(env.PaddleOcrInstalled);
        Assert.IsFalse(env.PaddlePaddleInstalled);
    }

    [TestMethod]
    public void Decision_NotReady_WhenPaddleOcrMissing()
    {
        var inspector = new NodalOsPaddleOcrRuntimeInspector();
        var env = inspector.Inspect();
        var decision = inspector.Decide(env);

        Assert.AreEqual(NodalOsPaddleOcrRuntimeDecision.NotReady, decision);
    }

    [TestMethod]
    public void Decision_BlockedByPythonMissing_WhenPythonUnavailable()
    {
        var inspector = new NodalOsPaddleOcrRuntimeInspector();
        var env = inspector.Inspect() with { PythonAvailable = false, PipAvailable = false, VenvAvailable = false };
        var decision = inspector.Decide(env);

        Assert.AreEqual(NodalOsPaddleOcrRuntimeDecision.BlockedByPythonMissing, decision);
    }

    [TestMethod]
    public void HealthCheck_NotInstalled_WhenPaddleOcrMissing()
    {
        var inspector = new NodalOsPaddleOcrRuntimeInspector();
        var env = inspector.Inspect();
        var health = inspector.HealthCheck(env);

        Assert.AreEqual(NodalOsPaddleOcrRuntimeHealthStatus.NotInstalled, health.Status);
        Assert.IsFalse(health.CanImportPaddleOcr);
        Assert.IsTrue(health.NoNetworkRequiredForThisCheck);
    }

    [TestMethod]
    public void RollbackPlan_Exists()
    {
        var inspector = new NodalOsPaddleOcrRuntimeInspector();
        var plan = inspector.RollbackPlan();

        Assert.IsFalse(string.IsNullOrWhiteSpace(plan.PlanId));
        Assert.IsTrue(plan.PackagesToRemove.Contains("paddleocr"));
        Assert.IsTrue(plan.PackagesToRemove.Contains("paddlepaddle"));
        Assert.IsTrue(plan.RequiresConfirmation);
        Assert.IsTrue(plan.NoAuthority);
    }

    [TestMethod]
    public void OperationalStatus_ProductionBlocked()
    {
        var inspector = new NodalOsPaddleOcrRuntimeInspector();
        var env = inspector.Inspect();
        var status = inspector.OperationalStatus(env);

        Assert.IsFalse(status.ProductionPublicEnabled);
        Assert.IsFalse(status.RealSaasEnabled);
        Assert.IsFalse(status.RealOcrProductiveEnabled);
        Assert.IsTrue(status.CropOnly);
        Assert.IsTrue(status.RedactedOnly);
        Assert.IsTrue(status.LocalOnly);
        Assert.IsTrue(status.NoRawPersistence);
        Assert.IsTrue(status.NoFullScreen);
        Assert.IsTrue(status.NoSensitive);
        Assert.IsTrue(status.NoAuthority);
    }

    [TestMethod]
    public void ScriptsAndDocsExist()
    {
        var root = AppDomain.CurrentDomain.BaseDirectory.Contains("bin\\Debug")
            ? Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", ".."))
            : AppDomain.CurrentDomain.BaseDirectory;

        Assert.IsTrue(File.Exists(Path.Combine(root, "docs", "adr", "paddleocr-local-production-grade-runtime-m191-m193.md")));
        Assert.IsTrue(File.Exists(Path.Combine(root, "docs", "reports", "paddleocr-local-runtime-readiness-m191.md")));
        Assert.IsTrue(File.Exists(Path.Combine(root, "docs", "runbooks", "paddleocr-local-worker-operations-m191-m193.md")));
        Assert.IsTrue(File.Exists(Path.Combine(root, "tools", "ocr-worker", "setup-paddleocr.ps1")));
        Assert.IsTrue(File.Exists(Path.Combine(root, "tools", "ocr-worker", "check-paddleocr.ps1")));
        Assert.IsTrue(File.Exists(Path.Combine(root, "tools", "ocr-worker", "run-paddleocr-worker.ps1")));
        Assert.IsTrue(File.Exists(Path.Combine(root, "tools", "ocr-worker", "rollback-paddleocr.ps1")));
    }
}
