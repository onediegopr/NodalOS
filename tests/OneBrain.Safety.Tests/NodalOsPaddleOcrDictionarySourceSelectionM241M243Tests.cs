using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("OcrDictionary")]
[TestCategory("CtcDecoder")]
[TestCategory("DictionaryCompatibility")]
[TestCategory("DictionaryAcquisition")]
[TestCategory("DictionarySourceSelection")]
[TestCategory("PaddleOcrDictionary")]
[TestCategory("RecognizerRuntimeExperiment")]
[TestCategory("RecognizerRuntimeCompatibility")]
[TestCategory("FullOcrHandoff")]
[TestCategory("DetectorRuntimeCompatibility")]
[TestCategory("OnnxOutOfProcessGuard")]
[TestCategory("OnnxOcrProbeRunner")]
[TestCategory("OnnxModelInventory")]
[TestCategory("OnnxModelVerification")]
[TestCategory("OnnxModelReadiness")]
[TestCategory("OcrVisionNoAuthority")]
[TestCategory("OcrVisionPrivacy")]
[TestCategory("PixelRedaction")]
[TestCategory("OcrRedactionPrecondition")]
[TestCategory("PrivatePreviewReadiness")]
[TestCategory("LocalPreviewReleaseCandidate")]
[TestCategory("BrowserRuntimePhaseGate")]
public sealed class NodalOsPaddleOcrDictionarySourceSelectionM241M243Tests
{
    private static readonly string RepoRoot = AppDomain.CurrentDomain.BaseDirectory.Contains("bin\\Debug") ||
                                              AppDomain.CurrentDomain.BaseDirectory.Contains("bin\\Release")
        ? Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", ".."))
        : AppDomain.CurrentDomain.BaseDirectory;

    [TestMethod]
    public void SourceAudit_RequiresOfficialVerifiableSource()
    {
        var service = new NodalOsOcrDictionaryCompatibilityService();
        var candidates = service.CreateM241SourceAuditCandidates();

        Assert.IsTrue(candidates.All(c => c.Official));
        Assert.IsTrue(candidates.All(c => c.Verifiable));
        Assert.IsTrue(candidates.Any(c => c.Provider.Contains("RapidAI", StringComparison.Ordinal)));
        Assert.IsTrue(candidates.Any(c => c.Provider.Contains("PaddlePaddle", StringComparison.Ordinal)));
        Assert.IsTrue(candidates.All(c => c.NoHiddenSaasRisk()));
    }

    [TestMethod]
    public void OfficialSource_CountMismatch_IsRejected()
    {
        var service = new NodalOsOcrDictionaryCompatibilityService();
        var report = service.AuditSourceCandidates(service.CreateM241SourceAuditCandidates());

        Assert.AreEqual(NodalOsOcrDictionarySourceAuditStatus.SourceRejectedCountMismatch, report.Status);
        Assert.IsNull(report.SelectedSource);
        Assert.IsTrue(report.HashPinned);
        Assert.IsTrue(report.SizePinned);
        Assert.IsTrue(report.NoDecodeAttempted);
        Assert.IsTrue(report.NoAuthority);
    }

    [TestMethod]
    public void RapidOcrModelScopeCandidate_HasPinnedHashAndSize_ButNotCompatibleWith97()
    {
        var candidate = new NodalOsOcrDictionaryCompatibilityService()
            .CreateM241SourceAuditCandidates()
            .Single(c => c.SourceId == "rapidocr-modelscope-v3.8.0-en-ppocrv4-en-dict");

        Assert.AreEqual(NodalOsOcrDictionaryCompatibilityService.RapidOcrModelScopeEnglishDictionaryUrl, candidate.UrlOrRef);
        Assert.IsTrue(EqualityComparer<int>.Default.Equals(candidate.ExpectedCharacterCount, 95));
        Assert.AreEqual("5662df9d2d03f0e8ca0d3b0649d6acbab904b6a14b3d3521463c71c37c668ce3", candidate.Sha256);
        Assert.AreEqual(190, candidate.SizeBytes);
        Assert.IsFalse(candidate.CompatibleWithRecognizerClassCount);
    }

    [TestMethod]
    public void EmbeddedOnnxCharacterMetadata_IsAudited_ButRejectedByCount()
    {
        var candidate = new NodalOsOcrDictionaryCompatibilityService()
            .CreateM241SourceAuditCandidates()
            .Single(c => c.SourceId == "onnx-recognizer-embedded-character-metadata");

        Assert.AreEqual("tools/ocr-worker/models/onnx/ch_PP-OCRv4_rec.onnx#metadata:character", candidate.UrlOrRef);
        Assert.IsTrue(EqualityComparer<int>.Default.Equals(candidate.ExpectedCharacterCount, 95));
        Assert.AreEqual("e8770c967605983d1570cdf5352041dfb68fa0c21664f49f47b155abd3e0e318", candidate.Sha256);
        Assert.IsFalse(candidate.CompatibleWithRecognizerClassCount);
    }

    [TestMethod]
    public void MissingHashOrSize_PreventsAcquisitionReady()
    {
        var candidates = new[]
        {
            new NodalOsOcrDictionarySourceCandidate(
                "official-compatible-no-hash",
                "https://example.invalid/en_dict.txt",
                "OfficialFixture",
                "OfficialFixture/Repo",
                "Apache-2.0",
                ExpectedCharacterCount: 96,
                BlankIncluded: false,
                Official: true,
                Verifiable: true,
                Sha256: null,
                SizeBytes: null,
                CompatibleWithRecognizerClassCount: true,
                Risk: "hash and size missing")
        };

        var report = new NodalOsOcrDictionaryCompatibilityService().AuditSourceCandidates(candidates);

        Assert.AreNotEqual(NodalOsOcrDictionarySourceAuditStatus.SourceSelected, report.Status);
        Assert.IsFalse(report.HashPinned);
        Assert.IsFalse(report.SizePinned);
    }

    [TestMethod]
    public void AcquisitionGate_Blocks_WhenOnlyOfficialCountMismatchSourcesExist()
    {
        var service = new NodalOsOcrDictionaryCompatibilityService();
        var sourceReport = service.AuditSourceCandidates(service.CreateM241SourceAuditCandidates());
        var gate = service.EvaluateAcquisitionGate(sourceReport);

        Assert.AreEqual(NodalOsOcrDictionaryReadinessDecision.BlockedByDictionaryCountMismatch, gate.Decision);
        Assert.IsFalse(gate.SourcePinned);
        Assert.IsTrue(gate.HashPinned);
        Assert.IsTrue(gate.SizePinned);
        Assert.IsFalse(gate.CharacterCountCompatible);
        Assert.IsFalse(gate.ScriptsActive);
        Assert.IsFalse(gate.DownloadExecuted);
        Assert.IsFalse(gate.RollbackTouchesOnnxModels);
        Assert.IsTrue(gate.ProductiveOcrBlocked);
        Assert.IsTrue(gate.ShadowModeBlocked);
        Assert.IsTrue(gate.NoAuthority);
    }

    [TestMethod]
    public void UnofficialSource_RejectedUnlessAdrPermits()
    {
        var candidates = new[]
        {
            new NodalOsOcrDictionarySourceCandidate(
                "third-party-dict",
                "https://example.invalid/third-party.txt",
                "ThirdParty",
                "Unknown",
                "Unknown",
                ExpectedCharacterCount: 96,
                BlankIncluded: false,
                Official: false,
                Verifiable: true,
                Sha256: "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
                SizeBytes: 200,
                CompatibleWithRecognizerClassCount: true,
                Risk: "unofficial source")
        };

        var report = new NodalOsOcrDictionaryCompatibilityService().AuditSourceCandidates(candidates);

        Assert.AreEqual(NodalOsOcrDictionarySourceAuditStatus.SourceRejectedUnofficial, report.Status);
        Assert.IsNull(report.SelectedSource);
    }

    [TestMethod]
    public void NoDictionaryMismatchDecode_IsAttempted()
    {
        var service = new NodalOsOcrDictionaryCompatibilityService();
        var compatibility = service.Evaluate(service.CreateCurrentAsciiManifest(verified: true), 97);
        var report = service.AuditSourceCandidates(service.CreateM241SourceAuditCandidates());

        Assert.AreEqual(NodalOsOcrDictionaryCompatibilityStatus.ClassCountMismatch, compatibility.Status);
        Assert.IsFalse(compatibility.CtcDecoderCompatibility.DecodeAllowed);
        Assert.IsTrue(report.NoDecodeAttempted);
    }

    [TestMethod]
    public void Report_Artifact_ClaudePrompt_Adr_Exist_ForM243()
    {
        Assert.IsTrue(File.Exists(Path.Combine(RepoRoot, "docs", "reports", "paddleocr-dictionary-source-selection-m243.md")));
        Assert.IsTrue(File.Exists(Path.Combine(RepoRoot, "artifacts", "ocr-vision-onnx", "m243", "paddleocr-dictionary-source-selection-summary.json")));
        Assert.IsTrue(File.Exists(Path.Combine(RepoRoot, "docs", "audits", "claude-paddleocr-dictionary-source-selection-audit-m243.md")));
        Assert.IsTrue(File.Exists(Path.Combine(RepoRoot, "docs", "adr", "paddleocr-dictionary-source-selection-m241-m243.md")));
    }
}

internal static class NodalOsDictionarySourceCandidateTestExtensions
{
    public static bool NoHiddenSaasRisk(this NodalOsOcrDictionarySourceCandidate candidate) =>
        !candidate.UrlOrRef.Contains("api_key", StringComparison.OrdinalIgnoreCase) &&
        !candidate.UrlOrRef.Contains("saas", StringComparison.OrdinalIgnoreCase);
}
