using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("OcrDictionary")]
[TestCategory("CtcDecoder")]
[TestCategory("DictionaryCompatibility")]
[TestCategory("DictionaryAcquisition")]
[TestCategory("PaddleOcrDictionary")]
[TestCategory("RecognizerRuntimeExperiment")]
[TestCategory("RecognizerRuntimeCompatibility")]
[TestCategory("RecognizerCompatibility")]
[TestCategory("FullOcrHandoff")]
[TestCategory("DetectorRuntimeCompatibility")]
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
public sealed class NodalOsPaddleOcrDictionaryCompatibilityM238M240Tests
{
    private static readonly string RepoRoot = AppDomain.CurrentDomain.BaseDirectory.Contains("bin\\Debug") ||
                                              AppDomain.CurrentDomain.BaseDirectory.Contains("bin\\Release")
        ? Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", ".."))
        : AppDomain.CurrentDomain.BaseDirectory;

    [TestMethod]
    public void AsciiDictionaryMismatch_BlocksDecode_ForRecognizer97()
    {
        var service = new NodalOsOcrDictionaryCompatibilityService();
        var result = service.Evaluate(service.CreateCurrentAsciiManifest(verified: true), recognizerOutputClassCount: 97);

        Assert.AreEqual(NodalOsOcrDictionaryCompatibilityStatus.ClassCountMismatch, result.Status);
        Assert.AreEqual(86, result.Dictionary!.CharacterCount);
        Assert.AreEqual(87, result.CtcDecoderCompatibility.DictionaryClassCountIncludingBlank);
        Assert.IsFalse(result.CtcDecoderCompatibility.DecodeAllowed);
        Assert.IsFalse(result.RecognitionSuccessAllowed);
        Assert.IsTrue(result.NoAuthority);
    }

    [TestMethod]
    public void MissingDictionary_BlocksDecode()
    {
        var result = new NodalOsOcrDictionaryCompatibilityService().Evaluate(null, recognizerOutputClassCount: 97);

        Assert.AreEqual(NodalOsOcrDictionaryCompatibilityStatus.MissingDictionary, result.Status);
        Assert.IsFalse(result.CtcDecoderCompatibility.DecodeAllowed);
        Assert.IsFalse(result.RecognitionSuccessAllowed);
    }

    [TestMethod]
    public void CompatibleDictionaryCount_AllowsDecodeGate_ButRemainsNoAuthority()
    {
        var dictionary = new NodalOsOcrDictionaryManifest(
            "paddleocr-en-ppocrv4-test",
            "en",
            CharacterCount: 96,
            BlankTokenCount: 1,
            SourceRef: "unit-test-approved-source",
            ExpectedSha256: "unit-test-sha256",
            Verified: true,
            NoAuthority: true);

        var result = new NodalOsOcrDictionaryCompatibilityService().Evaluate(dictionary, recognizerOutputClassCount: 97);

        Assert.AreEqual(NodalOsOcrDictionaryCompatibilityStatus.Compatible, result.Status);
        Assert.IsTrue(result.CtcDecoderCompatibility.DecodeAllowed);
        Assert.IsTrue(result.RequiresHumanReview);
        Assert.IsTrue(result.NoAuthority);
    }

    [TestMethod]
    public void PaddleOcrManifest_RecordsRequired97ClassPolicy_WithoutInventingSourceOrHash()
    {
        var entry = new NodalOsOcrDictionaryCompatibilityService().CreatePaddleOcrV4EnglishManifestEntryWithoutApprovedSource();

        Assert.AreEqual("paddleocr-en-ppocrv4-rec-ctc-dictionary", entry.DictionaryId);
        Assert.IsTrue(EqualityComparer<int>.Default.Equals(entry.ExpectedCharsetCount, 96));
        Assert.IsTrue(EqualityComparer<int>.Default.Equals(entry.ExpectedRecognizerClassCount, 97));
        Assert.AreEqual(NodalOsOcrDictionaryBlankTokenPolicy.BlankAppendedAtEnd, entry.BlankTokenPolicy);
        Assert.AreEqual(96, entry.CtcBlankIndex);
        Assert.IsNull(entry.SourceUrl);
        Assert.IsNull(entry.ExpectedSha256);
        Assert.IsNull(entry.ExpectedSizeBytes);
        Assert.AreEqual(NodalOsOcrDictionaryAvailabilityStatus.SourceNotSelected, entry.AcquisitionStatus);
        Assert.IsTrue(entry.Gitignored);
        Assert.IsFalse(entry.Committed);
    }

    [TestMethod]
    public void SourceNotSelected_CreatesBlockedAcquisitionPlan()
    {
        var service = new NodalOsOcrDictionaryCompatibilityService();
        var entry = service.CreatePaddleOcrV4EnglishManifestEntryWithoutApprovedSource();
        var plan = service.CreateSourceSelectionAcquisitionPlan(entry);

        Assert.IsFalse(plan.SourceApproved);
        Assert.IsFalse(plan.DownloadAllowed);
        Assert.AreEqual("READY_FOR_DICTIONARY_SOURCE_SELECTION", plan.Decision);
        Assert.IsTrue(plan.PlannedScripts.Any(s => s.EndsWith("download-dictionaries.ps1", StringComparison.Ordinal)));
        Assert.IsTrue(plan.Commands.All(c => c.Contains("dictionaries", StringComparison.Ordinal)));
        Assert.IsTrue(plan.NoSaas);
        Assert.IsTrue(plan.NoAuthority);
    }

    [TestMethod]
    public void ManifestEntry_SourceNotSelected_BlocksDecodeAndMapsToSourceSelection()
    {
        var service = new NodalOsOcrDictionaryCompatibilityService();
        var entry = service.CreatePaddleOcrV4EnglishManifestEntryWithoutApprovedSource();
        var compatibility = service.EvaluateManifestEntry(entry, actualCharsetCount: null, actualSha256: null, actualSizeBytes: null, actualCommitted: false);
        var readiness = service.DecideReadiness(
            entry,
            compatibility,
            service.CreateSourceSelectionAcquisitionPlan(entry),
            dictionaryPresent: false,
            hashVerified: false,
            decodeAttempted: false);

        Assert.AreEqual(NodalOsOcrDictionaryCompatibilityStatus.SourceNotSelected, compatibility.Status);
        Assert.IsFalse(compatibility.CtcDecoderCompatibility.DecodeAllowed);
        Assert.AreEqual(NodalOsOcrDictionaryReadinessDecision.ReadyForDictionarySourceSelection, readiness.Decision);
        Assert.IsFalse(readiness.DecodeAttempted);
        Assert.IsTrue(readiness.ProductiveOcrBlocked);
        Assert.IsTrue(readiness.ShadowModeBlocked);
    }

    [TestMethod]
    public void HashMismatch_BlocksReadiness()
    {
        var service = new NodalOsOcrDictionaryCompatibilityService();
        var entry = service.CreatePaddleOcrV4EnglishManifestEntryWithoutApprovedSource() with
        {
            SourceUrl = "https://example.invalid/paddleocr-ppocrv4-en-dict.txt",
            SourceRef = "unit-test-approved-source",
            ExpectedSha256 = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
            ExpectedSizeBytes = 1234,
            AcquisitionStatus = NodalOsOcrDictionaryAvailabilityStatus.PresentAndVerified
        };

        var compatibility = service.EvaluateManifestEntry(
            entry,
            actualCharsetCount: 96,
            actualSha256: "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb",
            actualSizeBytes: 1234,
            actualCommitted: false);

        var readiness = service.DecideReadiness(
            entry,
            compatibility,
            service.CreateSourceSelectionAcquisitionPlan(entry),
            dictionaryPresent: true,
            hashVerified: false,
            decodeAttempted: false);

        Assert.AreEqual(NodalOsOcrDictionaryCompatibilityStatus.HashMismatch, compatibility.Status);
        Assert.AreEqual(NodalOsOcrDictionaryReadinessDecision.BlockedByDictionaryHashMismatch, readiness.Decision);
    }

    [TestMethod]
    public void DecodeAttemptWithIncompatibleDictionary_BlocksAsClassCountMismatch()
    {
        var service = new NodalOsOcrDictionaryCompatibilityService();
        var entry = service.CreatePaddleOcrV4EnglishManifestEntryWithoutApprovedSource();
        var compatibility = service.Evaluate(service.CreateCurrentAsciiManifest(verified: true), recognizerOutputClassCount: 97);

        var readiness = service.DecideReadiness(
            entry,
            compatibility,
            service.CreateSourceSelectionAcquisitionPlan(entry),
            dictionaryPresent: true,
            hashVerified: false,
            decodeAttempted: true);

        Assert.AreEqual(NodalOsOcrDictionaryReadinessDecision.BlockedByDictionaryClassCountMismatch, readiness.Decision);
        Assert.IsTrue(readiness.DecodeAttempted);
    }

    [TestMethod]
    public void UnexpectedCommittedDictionaryPolicy_IsExplicit()
    {
        var service = new NodalOsOcrDictionaryCompatibilityService();
        var entry = service.CreatePaddleOcrV4EnglishManifestEntryWithoutApprovedSource();

        var compatibility = service.EvaluateManifestEntry(
            entry,
            actualCharsetCount: 96,
            actualSha256: null,
            actualSizeBytes: null,
            actualCommitted: true);

        Assert.AreEqual(NodalOsOcrDictionaryCompatibilityStatus.UnexpectedCommittedDictionary, compatibility.Status);
        Assert.IsFalse(compatibility.CtcDecoderCompatibility.DecodeAllowed);
    }

    [TestMethod]
    public void Report_Artifact_ClaudePrompt_Adr_Exist_ForM240()
    {
        Assert.IsTrue(File.Exists(Path.Combine(RepoRoot, "docs", "reports", "paddleocr-dictionary-ctc-compatibility-m240.md")));
        Assert.IsTrue(File.Exists(Path.Combine(RepoRoot, "artifacts", "ocr-vision-onnx", "m240", "paddleocr-dictionary-ctc-compatibility-summary.json")));
        Assert.IsTrue(File.Exists(Path.Combine(RepoRoot, "docs", "audits", "claude-paddleocr-dictionary-ctc-compatibility-audit-m240.md")));
        Assert.IsTrue(File.Exists(Path.Combine(RepoRoot, "docs", "adr", "paddleocr-dictionary-ctc-decision-m238-m240.md")));
    }
}
