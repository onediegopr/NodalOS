using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("OcrRuntimeStrategy")]
[TestCategory("OnnxRuntimeDotNet")]
[TestCategory("OnnxOcrNoAuthority")]
[TestCategory("OcrVisionActivationGate")]
[TestCategory("OcrVisionNoAuthority")]
[TestCategory("OcrVisionPrivacy")]
[TestCategory("BrowserGroundingSnapshot")]
[TestCategory("PrivatePreviewReadiness")]
[TestCategory("LocalPreviewReleaseCandidate")]
[TestCategory("NodalOsNamingAudit")]
[TestCategory("BrowserRuntimePhaseGate")]
public sealed class NodalOsOcrRuntimeStrategyM194Tests
{
    [TestMethod]
    public void Strategy_Primary_IsOnnxRuntimeDotNet()
    {
        var strategy = new NodalOsOcrRuntimeStrategyService().CurrentStrategy();

        Assert.AreEqual(NodalOsOcrRuntimeKind.OnnxRuntimeDotNet, strategy.PrimaryRuntime);
        Assert.AreEqual(NodalOsOcrRuntimePriority.Primary, MapPriority(strategy.PrimaryRuntime));
    }

    [TestMethod]
    public void Strategy_Secondary_IsCppLocalWorker()
    {
        var strategy = new NodalOsOcrRuntimeStrategyService().CurrentStrategy();

        Assert.AreEqual(NodalOsOcrRuntimeKind.CppLocalWorker, strategy.SecondaryRuntime);
    }

    [TestMethod]
    public void Strategy_Fallback_IsPythonPaddleOcrLegacy()
    {
        var strategy = new NodalOsOcrRuntimeStrategyService().CurrentStrategy();

        Assert.AreEqual(NodalOsOcrRuntimeKind.PythonPaddleOcrLegacy, strategy.FallbackRuntime);
    }

    [TestMethod]
    public void Strategy_SaasDisabledAndProductionBlocked()
    {
        var strategy = new NodalOsOcrRuntimeStrategyService().CurrentStrategy();

        Assert.IsTrue(strategy.SaasDisabledByDefault);
        Assert.IsTrue(strategy.ProductionPublicOcrBlocked);
    }

    [TestMethod]
    public void Strategy_RequiresNoPython_NoSaas_NoApiKey()
    {
        var svc = new NodalOsOcrRuntimeStrategyService();

        Assert.IsFalse(svc.RequiresPython(NodalOsOcrRuntimeKind.OnnxRuntimeDotNet));
        Assert.IsFalse(svc.RequiresSaas(NodalOsOcrRuntimeKind.OnnxRuntimeDotNet));
        Assert.IsFalse(svc.RequiresApiKey(NodalOsOcrRuntimeKind.OnnxRuntimeDotNet));
    }

    [TestMethod]
    public void Decision_BlockedByMissingDependency_WhenOnnxPackageMissing()
    {
        var svc = new NodalOsOcrRuntimeStrategyService();
        var decision = svc.Decide(onnxRuntimePackageAvailable: false, onnxModelAvailable: false, modelVerified: false);

        Assert.AreEqual(NodalOsOcrRuntimeDecision.BlockedByMissingDependency, decision);
    }

    [TestMethod]
    public void Decision_BlockedByMissingModel_WhenModelMissing()
    {
        var svc = new NodalOsOcrRuntimeStrategyService();
        var decision = svc.Decide(onnxRuntimePackageAvailable: true, onnxModelAvailable: false, modelVerified: false);

        Assert.AreEqual(NodalOsOcrRuntimeDecision.BlockedByMissingModel, decision);
    }

    [TestMethod]
    public void Decision_PrimaryReady_WhenModelVerified()
    {
        var svc = new NodalOsOcrRuntimeStrategyService();
        var decision = svc.Decide(onnxRuntimePackageAvailable: true, onnxModelAvailable: true, modelVerified: true);

        Assert.AreEqual(NodalOsOcrRuntimeDecision.PrimaryReady, decision);
    }

    [TestMethod]
    public void Strategy_DocsExist()
    {
        var root = AppDomain.CurrentDomain.BaseDirectory.Contains("bin\\Debug")
            ? Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", ".."))
            : AppDomain.CurrentDomain.BaseDirectory;

        Assert.IsTrue(File.Exists(Path.Combine(root, "docs", "adr", "ocr-runtime-strategy-onnx-dotnet-m194-m196.md")));
        Assert.IsTrue(File.Exists(Path.Combine(root, "docs", "reports", "ocr-runtime-strategy-pivot-m194.md")));
    }

    private static NodalOsOcrRuntimePriority MapPriority(NodalOsOcrRuntimeKind kind) =>
        kind switch
        {
            NodalOsOcrRuntimeKind.OnnxRuntimeDotNet => NodalOsOcrRuntimePriority.Primary,
            NodalOsOcrRuntimeKind.CppLocalWorker => NodalOsOcrRuntimePriority.Secondary,
            NodalOsOcrRuntimeKind.PythonPaddleOcrLegacy => NodalOsOcrRuntimePriority.Fallback,
            NodalOsOcrRuntimeKind.HumanReview => NodalOsOcrRuntimePriority.LastResort,
            _ => NodalOsOcrRuntimePriority.Disabled
        };
}
