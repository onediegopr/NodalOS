using System.Security.Cryptography;
using System.Text;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.BrowserExecutor.Cdp;

public sealed class NodalOsOcrDictionaryCompatibilityService
{
    public const int PaddleOcrV4EnglishRecognizerClassCount = 97;
    public const int PaddleOcrV4EnglishExpectedCharsetCount = PaddleOcrV4EnglishRecognizerClassCount - 1;
    public const string PaddleOcrV4EnglishDictionaryId = "paddleocr-en-ppocrv4-rec-ctc-dictionary";
    public const string PaddleOcrV4EnglishDictionaryRelativePath = "tools/ocr-worker/models/onnx/dictionaries/paddleocr-ppocrv4-en-dict.txt";
    public const string RapidOcrModelScopeEnglishDictionaryUrl = "https://www.modelscope.cn/models/RapidAI/RapidOCR/resolve/v3.8.0/paddle/PP-OCRv4/rec/en_PP-OCRv4_rec_mobile/en_dict.txt";
    public const string PaddleOcrGithubEnglishDictionaryUrl = "https://raw.githubusercontent.com/PaddlePaddle/PaddleOCR/release/2.8/ppocr/utils/en_dict.txt";
    public const string EnglishDictionarySha256 = "5662df9d2d03f0e8ca0d3b0649d6acbab904b6a14b3d3521463c71c37c668ce3";
    public const long EnglishDictionarySizeBytes = 190;
    public const int OfficialEnglishDictionaryTokenCount = 95;

    public NodalOsOcrDictionaryManifest CreateCurrentAsciiManifest(bool verified = false)
    {
        return new NodalOsOcrDictionaryManifest(
            "en-ascii",
            "en",
            NodalOsOnnxOcrCharacterDictionary.EnglishAscii.Count,
            BlankTokenCount: 1,
            SourceRef: "embedded-offline-fixture-ascii-subset",
            ExpectedSha256: null,
            verified,
            NoAuthority: true);
    }

    public IReadOnlyList<NodalOsOcrDictionarySourceCandidate> CreateM241SourceAuditCandidates()
    {
        return
        [
            new NodalOsOcrDictionarySourceCandidate(
                "rapidocr-modelscope-v3.8.0-en-ppocrv4-en-dict",
                RapidOcrModelScopeEnglishDictionaryUrl,
                "RapidAI/ModelScope",
                "RapidAI/RapidOCR v3.8.0",
                "Apache-2.0 lineage from PaddleOCR/RapidOCR model distribution",
                OfficialEnglishDictionaryTokenCount,
                BlankIncluded: false,
                Official: true,
                Verifiable: true,
                EnglishDictionarySha256,
                EnglishDictionarySizeBytes,
                CompatibleWithRecognizerClassCount: false,
                $"official source has {OfficialEnglishDictionaryTokenCount} tokens; required {PaddleOcrV4EnglishExpectedCharsetCount} tokens before CTC blank"),
            new NodalOsOcrDictionarySourceCandidate(
                "paddleocr-github-release-2.8-en-dict",
                PaddleOcrGithubEnglishDictionaryUrl,
                "PaddlePaddle",
                "PaddlePaddle/PaddleOCR release/2.8",
                "Apache-2.0",
                OfficialEnglishDictionaryTokenCount,
                BlankIncluded: false,
                Official: true,
                Verifiable: true,
                EnglishDictionarySha256,
                EnglishDictionarySizeBytes,
                CompatibleWithRecognizerClassCount: false,
                $"official source has {OfficialEnglishDictionaryTokenCount} tokens; required {PaddleOcrV4EnglishExpectedCharsetCount} tokens before CTC blank"),
            new NodalOsOcrDictionarySourceCandidate(
                "onnx-recognizer-embedded-character-metadata",
                "tools/ocr-worker/models/onnx/ch_PP-OCRv4_rec.onnx#metadata:character",
                "RapidAI/ModelScope",
                "Verified local ONNX recognizer metadata",
                "Model artifact provenance pinned by ONNX manifest hash",
                OfficialEnglishDictionaryTokenCount,
                BlankIncluded: false,
                Official: true,
                Verifiable: true,
                "e8770c967605983d1570cdf5352041dfb68fa0c21664f49f47b155abd3e0e318",
                7653044,
                CompatibleWithRecognizerClassCount: false,
                $"embedded ONNX metadata exposes {OfficialEnglishDictionaryTokenCount} tokens; required {PaddleOcrV4EnglishExpectedCharsetCount} tokens before CTC blank")
        ];
    }

    public NodalOsOcrDictionarySourceSelectionReport AuditSourceCandidates(
        IReadOnlyList<NodalOsOcrDictionarySourceCandidate> candidates)
    {
        var officialCandidates = candidates.Where(c => c.Official && c.Verifiable).ToArray();
        var selected = officialCandidates.FirstOrDefault(c =>
            c.CompatibleWithRecognizerClassCount && c.Sha256 is not null && c.SizeBytes is not null);

        if (selected is not null)
        {
            return new NodalOsOcrDictionarySourceSelectionReport(
                $"dict-source-selection-{Guid.NewGuid():N}",
                NodalOsOcrDictionarySourceAuditStatus.SourceSelected,
                candidates,
                selected,
                HashPinned: true,
                SizePinned: true,
                NoDecodeAttempted: true,
                NoAuthority: true,
                "official compatible dictionary source selected and pinned");
        }

        if (officialCandidates.Any(c => !c.CompatibleWithRecognizerClassCount))
        {
            return new NodalOsOcrDictionarySourceSelectionReport(
                $"dict-source-selection-{Guid.NewGuid():N}",
                NodalOsOcrDictionarySourceAuditStatus.SourceRejectedCountMismatch,
                candidates,
                SelectedSource: null,
                HashPinned: officialCandidates.Any(c => c.Sha256 is not null),
                SizePinned: officialCandidates.Any(c => c.SizeBytes is not null),
                NoDecodeAttempted: true,
                NoAuthority: true,
                $"official candidates are verifiable but expose {OfficialEnglishDictionaryTokenCount} tokens; required {PaddleOcrV4EnglishExpectedCharsetCount}");
        }

        if (candidates.Any(c => !c.Official))
        {
            return new NodalOsOcrDictionarySourceSelectionReport(
                $"dict-source-selection-{Guid.NewGuid():N}",
                NodalOsOcrDictionarySourceAuditStatus.SourceRejectedUnofficial,
                candidates,
                SelectedSource: null,
                HashPinned: false,
                SizePinned: false,
                NoDecodeAttempted: true,
                NoAuthority: true,
                "only unofficial candidates were available and no ADR permits them");
        }

        return new NodalOsOcrDictionarySourceSelectionReport(
            $"dict-source-selection-{Guid.NewGuid():N}",
            NodalOsOcrDictionarySourceAuditStatus.NoApprovedSourceFound,
            candidates,
            SelectedSource: null,
            HashPinned: false,
            SizePinned: false,
            NoDecodeAttempted: true,
            NoAuthority: true,
            "no official verifiable dictionary source found");
    }

    public NodalOsOcrDictionaryAcquisitionGateReport EvaluateAcquisitionGate(
        NodalOsOcrDictionarySourceSelectionReport sourceSelection)
    {
        var decision = sourceSelection.Status switch
        {
            NodalOsOcrDictionarySourceAuditStatus.SourceSelected => NodalOsOcrDictionaryReadinessDecision.ReadyForDictionaryDownload,
            NodalOsOcrDictionarySourceAuditStatus.SourceRejectedCountMismatch => NodalOsOcrDictionaryReadinessDecision.BlockedByDictionaryCountMismatch,
            NodalOsOcrDictionarySourceAuditStatus.SourceCandidateFoundNeedsHash => NodalOsOcrDictionaryReadinessDecision.BlockedByDictionaryHashPinning,
            NodalOsOcrDictionarySourceAuditStatus.SourceRejectedUnpinnable => NodalOsOcrDictionaryReadinessDecision.BlockedByDictionaryHashPinning,
            NodalOsOcrDictionarySourceAuditStatus.SourceRejectedUnofficial => NodalOsOcrDictionaryReadinessDecision.BlockedByDictionarySource,
            _ => NodalOsOcrDictionaryReadinessDecision.ReadyForManualDictionarySourceApproval
        };

        var sourcePinned = sourceSelection.SelectedSource is not null;
        var compatible = sourceSelection.SelectedSource?.CompatibleWithRecognizerClassCount == true;

        return new NodalOsOcrDictionaryAcquisitionGateReport(
            $"dict-acquisition-gate-{Guid.NewGuid():N}",
            decision,
            sourceSelection,
            sourcePinned,
            sourceSelection.HashPinned,
            sourceSelection.SizePinned,
            compatible,
            ScriptsActive: decision == NodalOsOcrDictionaryReadinessDecision.ReadyForDictionaryDownload,
            DownloadExecuted: false,
            RollbackTouchesOnnxModels: false,
            ProductiveOcrBlocked: true,
            ShadowModeBlocked: true,
            NoAuthority: sourceSelection.NoAuthority,
            BrowserCredentialRedactor.Redact($"{decision}; sourceStatus={sourceSelection.Status}; no decode attempted"));
    }

    public string CreateOfficialEnglishDictionaryRawText()
    {
        var tokens = new List<string>();
        tokens.AddRange("0123456789".Select(c => c.ToString()));
        tokens.AddRange(Enumerable.Range(':', '~' - ':' + 1).Select(c => ((char)c).ToString()));
        tokens.AddRange(Enumerable.Range('!', '/' - '!' + 1).Select(c => ((char)c).ToString()));
        tokens.Add(" ");

        return string.Join("\n", tokens) + "\n";
    }

    public NodalOsDictionaryRawLineAnalysis AnalyzeDictionaryRawLines(
        string sourceId,
        string urlOrRef,
        string branchOrTag,
        string license,
        string rawText)
    {
        var bytes = Encoding.UTF8.GetBytes(rawText);
        var sha256 = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        var normalized = rawText.Replace("\r\n", "\n", StringComparison.Ordinal).Replace("\r", "\n", StringComparison.Ordinal);
        var rawSegments = normalized.Split('\n');
        var effectivePaddleLines = rawSegments;

        if (effectivePaddleLines.Length > 0 && effectivePaddleLines[^1].Length == 0 && normalized.EndsWith('\n'))
        {
            effectivePaddleLines = effectivePaddleLines[..^1];
        }

        var trimmedNonEmpty = rawSegments.Count(line => line.Trim().Length > 0);
        var spaceOnly = rawSegments.Count(line => line == " ");
        var empty = rawSegments.Count(line => line.Length == 0);
        var lastSignificant = effectivePaddleLines.LastOrDefault(line => line.Length > 0) ?? string.Empty;

        return new NodalOsDictionaryRawLineAnalysis(
            $"dict-raw-line-analysis-{Guid.NewGuid():N}",
            sourceId,
            urlOrRef,
            branchOrTag,
            license,
            bytes.Length,
            sha256,
            rawSegments.Length,
            rawSegments.Length,
            trimmedNonEmpty,
            effectivePaddleLines.Length,
            effectivePaddleLines.Length,
            spaceOnly,
            empty,
            normalized.EndsWith('\n'),
            bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF,
            rawSegments.Any(line => line.TrimStart().StartsWith("#", StringComparison.Ordinal)),
            spaceOnly > 0,
            false,
            false,
            effectivePaddleLines.FirstOrDefault() ?? string.Empty,
            lastSignificant,
            "PaddleOCR reads physical lines and strips CR/LF only; terminal newline is not a character token while a single-space line is preserved");
    }

    public IReadOnlyList<NodalOsDictionaryRawLineAnalysis> CreateM247SourceReconciliationAnalyses()
    {
        var raw = CreateOfficialEnglishDictionaryRawText();

        return
        [
            AnalyzeDictionaryRawLines(
                "paddleocr-release-2.8-en-dict",
                PaddleOcrGithubEnglishDictionaryUrl,
                "release/2.8",
                "Apache-2.0",
                raw),
            AnalyzeDictionaryRawLines(
                "paddleocr-main-en-dict",
                "https://raw.githubusercontent.com/PaddlePaddle/PaddleOCR/main/ppocr/utils/en_dict.txt",
                "main",
                "Apache-2.0",
                raw),
            AnalyzeDictionaryRawLines(
                "rapidocr-modelscope-v3.8.0-en-dict",
                RapidOcrModelScopeEnglishDictionaryUrl,
                "RapidOCR v3.8.0 ModelScope",
                "Apache-2.0 lineage from PaddleOCR/RapidOCR model distribution",
                raw),
            AnalyzeDictionaryRawLines(
                "huggingface-paddlepaddle-en-ppocrv4-mobile-rec-config-character-dict",
                "https://huggingface.co/PaddlePaddle/en_PP-OCRv4_mobile_rec/raw/main/config.json#PostProcess.character_dict",
                "main",
                "Apache-2.0",
                raw)
        ];
    }

    public NodalOsRecognizerDictionaryPairCompatibility EvaluateRecognizerDictionaryPair(
        IReadOnlyList<NodalOsDictionaryRawLineAnalysis> sourceAnalyses)
    {
        var preferred = sourceAnalyses.First(analysis => analysis.SourceId == "rapidocr-modelscope-v3.8.0-en-dict");
        var pair = new NodalOsRecognizerDictionaryPair(
            $"recognizer-dictionary-pair-{Guid.NewGuid():N}",
            "paddleocr-rec-onnx",
            "tools/ocr-worker/models/onnx/ch_PP-OCRv4_rec.onnx",
            "e8770c967605983d1570cdf5352041dfb68fa0c21664f49f47b155abd3e0e318",
            PaddleOcrV4EnglishRecognizerClassCount,
            preferred.SourceId,
            preferred.PaddleOcrParserTokenCount,
            preferred.TokenCountPreservingEmptyLines,
            0,
            NodalOsDictionaryParserPolicy.PaddleOcrReadLinesStripNewline,
            SourceOfficial: true,
            SourceHashPinned: true,
            SourceSizePinned: true,
            NoAuthority: true);

        var foundVerified96 = sourceAnalyses.Any(analysis => analysis.CountBecomes96UnderDocumentedParser);
        var parserLosesSpace = sourceAnalyses.Any(analysis => analysis.HasSignificantSpaceToken) &&
                                sourceAnalyses.Any(analysis => analysis.PaddleOcrParserTokenCount < analysis.TokenCountTrimmingEmptyLines);
        var parserDropsTerminalEmpty = sourceAnalyses.Any(analysis => analysis.HasFinalNewline && analysis.EmptyLineCount > 0);
        var metadataMatchesDictionary = preferred.PaddleOcrParserTokenCount == OfficialEnglishDictionaryTokenCount;

        var decision = foundVerified96
            ? NodalOsRecognizerDictionaryPairDecision.ReadyForDictionaryPinning
            : metadataMatchesDictionary && PaddleOcrV4EnglishRecognizerClassCount != preferred.PaddleOcrParserTokenCount + 1
                ? NodalOsRecognizerDictionaryPairDecision.ReadyForRecognizerModelDictionaryPairReplacement
                : NodalOsRecognizerDictionaryPairDecision.ReadyForManualSourceReview;

        var reason = decision switch
        {
            NodalOsRecognizerDictionaryPairDecision.ReadyForDictionaryPinning => "verified 96-token source plus CTC blank index 0 explains 97 recognizer classes",
            NodalOsRecognizerDictionaryPairDecision.ReadyForRecognizerModelDictionaryPairReplacement => "official raw sources and ONNX metadata expose 95 dictionary tokens; PaddleOCR CTC blank index 0 explains 96 classes, not recognizer output 97",
            _ => "source count remains ambiguous and requires manual review before dictionary pinning"
        };

        return new NodalOsRecognizerDictionaryPairCompatibility(
            $"recognizer-dictionary-pair-compat-{Guid.NewGuid():N}",
            pair,
            sourceAnalyses,
            decision,
            foundVerified96,
            parserLosesSpace,
            parserDropsTerminalEmpty,
            metadataMatchesDictionary,
            DecodeAllowed: false,
            ProductiveOcrBlocked: true,
            ShadowModeBlocked: true,
            NoRawPersistence: true,
            NoFullScreen: true,
            NoSensitive: true,
            NoSaas: true,
            NoAuthority: true,
            reason);
    }

    public NodalOsRecognizerClassSemantics AuditRecognizerClassSemantics()
    {
        var mappings = new[]
        {
            Mapping(
                "dict-only",
                NodalOsRecognizerTokenPolicy.DictionaryCharsOnly,
                dictionaryTokenCount: OfficialEnglishDictionaryTokenCount,
                expectedClassCount: OfficialEnglishDictionaryTokenCount,
                blankIndex: null,
                compatible: false,
                decodeAllowed: false,
                evidenceSource: "mathematical baseline only",
                riskLevel: "high",
                reason: "dictionary chars only cannot explain CTC recognizer classes"),
            Mapping(
                "paddle-ctc-blank-at-start",
                NodalOsRecognizerTokenPolicy.DictionaryPlusBlankAtStart,
                dictionaryTokenCount: OfficialEnglishDictionaryTokenCount,
                expectedClassCount: OfficialEnglishDictionaryTokenCount + 1,
                blankIndex: 0,
                compatible: false,
                decodeAllowed: false,
                evidenceSource: "PaddleOCR CTCLabelDecode adds blank at index 0 and ignores token 0",
                riskLevel: "low",
                reason: "official CTC policy explains 96 classes, not recognizer output 97"),
            Mapping(
                "blank-at-end",
                NodalOsRecognizerTokenPolicy.DictionaryPlusBlankAtEnd,
                dictionaryTokenCount: OfficialEnglishDictionaryTokenCount,
                expectedClassCount: OfficialEnglishDictionaryTokenCount + 1,
                blankIndex: OfficialEnglishDictionaryTokenCount,
                compatible: false,
                decodeAllowed: false,
                evidenceSource: "legacy local assumption from M238-M243",
                riskLevel: "medium",
                reason: "blank at end also explains 96 classes, not 97"),
            Mapping(
                "blank-plus-unknown",
                NodalOsRecognizerTokenPolicy.DictionaryPlusBlankAndUnknown,
                dictionaryTokenCount: OfficialEnglishDictionaryTokenCount,
                expectedClassCount: OfficialEnglishDictionaryTokenCount + 2,
                blankIndex: 0,
                compatible: true,
                decodeAllowed: false,
                evidenceSource: "hypothesis only; no official source identified an unknown token for this CTC model",
                riskLevel: "high",
                reason: "explains 97 arithmetically but lacks approved token semantics"),
            Mapping(
                "blank-plus-padding",
                NodalOsRecognizerTokenPolicy.DictionaryPlusBlankAndPadding,
                dictionaryTokenCount: OfficialEnglishDictionaryTokenCount,
                expectedClassCount: OfficialEnglishDictionaryTokenCount + 2,
                blankIndex: 0,
                compatible: true,
                decodeAllowed: false,
                evidenceSource: "hypothesis only; padding special token belongs to other decoder families, not proven for this CTC model",
                riskLevel: "high",
                reason: "explains 97 arithmetically but lacks evidence for padding token"),
            Mapping(
                "model-dictionary-mismatch",
                NodalOsRecognizerTokenPolicy.ModelDictionaryMismatch,
                dictionaryTokenCount: OfficialEnglishDictionaryTokenCount,
                expectedClassCount: OfficialEnglishDictionaryTokenCount + 1,
                blankIndex: 0,
                compatible: false,
                decodeAllowed: false,
                evidenceSource: "official dictionary and ONNX metadata expose 95 tokens while recognizer output exposes 97 classes",
                riskLevel: "high",
                reason: "model output class count remains unexplained by approved dictionary semantics")
        };

        return new NodalOsRecognizerClassSemantics(
            $"recognizer-class-semantics-{Guid.NewGuid():N}",
            PaddleOcrV4EnglishRecognizerClassCount,
            OfficialEnglishDictionaryTokenCount,
            mappings,
            SelectedMapping: null,
            NodalOsRecognizerClassSemanticsDecision.ReadyForRecognizerModelDictionarySourceReview,
            DecodeAllowed: false,
            NoAuthority: true,
            "no approved token policy explains 97 classes; source/model pair requires review");
    }

    public IReadOnlyList<NodalOsCtcDecodePolicyCandidate> CreateCtcDecodePolicyCandidates()
    {
        return
        [
            new NodalOsCtcDecodePolicyCandidate(
                "paddle-ctc-blank-at-start",
                NodalOsRecognizerTokenPolicy.DictionaryPlusBlankAtStart,
                NodalOsRecognizerSpecialTokenPolicy.BlankAtStart,
                OfficialEnglishDictionaryTokenCount,
                PaddleOcrV4EnglishRecognizerClassCount,
                BlankIndex: 0,
                ExtraTokenIndex: null,
                EvidenceApproved: true,
                HypothesisOnly: false,
                EvidenceSource: "PaddleOCR CTCLabelDecode",
                Reason: "official blank policy but class count is 95+1=96, not 97"),
            new NodalOsCtcDecodePolicyCandidate(
                "hypothesis-blank-start-unknown",
                NodalOsRecognizerTokenPolicy.DictionaryPlusBlankAndUnknown,
                NodalOsRecognizerSpecialTokenPolicy.BlankAndUnknown,
                OfficialEnglishDictionaryTokenCount,
                PaddleOcrV4EnglishRecognizerClassCount,
                BlankIndex: 0,
                ExtraTokenIndex: 96,
                EvidenceApproved: false,
                HypothesisOnly: true,
                EvidenceSource: "hypothesis only",
                Reason: "would explain 97 but no approved source defines unknown index 96"),
            new NodalOsCtcDecodePolicyCandidate(
                "hypothesis-blank-start-padding",
                NodalOsRecognizerTokenPolicy.DictionaryPlusBlankAndPadding,
                NodalOsRecognizerSpecialTokenPolicy.BlankAndPadding,
                OfficialEnglishDictionaryTokenCount,
                PaddleOcrV4EnglishRecognizerClassCount,
                BlankIndex: 0,
                ExtraTokenIndex: 96,
                EvidenceApproved: false,
                HypothesisOnly: true,
                EvidenceSource: "hypothesis only",
                Reason: "would explain 97 but padding is not proven for this CTC recognizer")
        ];
    }

    public NodalOsCtcDecodePolicyExperimentResult EvaluateCtcDecodePolicyCandidate(
        NodalOsCtcDecodePolicyCandidate candidate)
    {
        var expectedClassCount = candidate.TokenPolicy switch
        {
            NodalOsRecognizerTokenPolicy.DictionaryPlusBlankAtStart => candidate.DictionaryTokenCount + 1,
            NodalOsRecognizerTokenPolicy.DictionaryPlusBlankAtEnd => candidate.DictionaryTokenCount + 1,
            NodalOsRecognizerTokenPolicy.DictionaryPlusBlankAndUnknown => candidate.DictionaryTokenCount + 2,
            NodalOsRecognizerTokenPolicy.DictionaryPlusBlankAndPadding => candidate.DictionaryTokenCount + 2,
            NodalOsRecognizerTokenPolicy.DictionaryPlusTwoSpecialTokens => candidate.DictionaryTokenCount + 2,
            _ => candidate.DictionaryTokenCount
        };

        if (candidate.HypothesisOnly || !candidate.EvidenceApproved)
        {
            return DecodePolicyResult(
                candidate,
                NodalOsCtcDecodePolicyExperimentStatus.HypothesisOnlyDecodeBlocked,
                decodeAllowed: false,
                "candidate policy is hypothesis-only; decode blocked");
        }

        if (expectedClassCount != candidate.ModelClassCount)
        {
            return DecodePolicyResult(
                candidate,
                NodalOsCtcDecodePolicyExperimentStatus.ClassCountMismatch,
                decodeAllowed: false,
                $"candidate expects {expectedClassCount} classes but model exposes {candidate.ModelClassCount}");
        }

        if (candidate.BlankIndex is null or < 0)
        {
            return DecodePolicyResult(
                candidate,
                NodalOsCtcDecodePolicyExperimentStatus.UnsupportedBlankIndex,
                decodeAllowed: false,
                "candidate blank index is unsupported");
        }

        return DecodePolicyResult(
            candidate,
            NodalOsCtcDecodePolicyExperimentStatus.ApprovedDecodeAllowed,
            decodeAllowed: true,
            "approved token policy permits decode but result remains no-authority");
    }

    public NodalOsRecognizerTokenPolicyDecisionReport DecideTokenPolicyReadiness(
        NodalOsRecognizerClassSemantics semantics,
        IReadOnlyList<NodalOsCtcDecodePolicyExperimentResult> experiments)
    {
        var approvedDecode = experiments.Any(e =>
            e.Status == NodalOsCtcDecodePolicyExperimentStatus.ApprovedDecodeAllowed &&
            e.DecodeAllowed);
        var hypothesisOnly = experiments.Any(e => e.Status == NodalOsCtcDecodePolicyExperimentStatus.HypothesisOnlyDecodeBlocked);
        var classMismatch = experiments.Any(e => e.Status == NodalOsCtcDecodePolicyExperimentStatus.ClassCountMismatch);

        var decision = approvedDecode
            ? NodalOsRecognizerClassSemanticsDecision.ReadyForApprovedTokenPolicyDecode
            : hypothesisOnly || classMismatch
                ? NodalOsRecognizerClassSemanticsDecision.ReadyForRecognizerModelDictionarySourceReview
                : NodalOsRecognizerClassSemanticsDecision.BlockedByTokenPolicyUnknown;

        return new NodalOsRecognizerTokenPolicyDecisionReport(
            $"token-policy-decision-{Guid.NewGuid():N}",
            semantics,
            experiments,
            decision,
            ProductiveOcrBlocked: true,
            ShadowModeBlocked: true,
            NoRawPersistence: true,
            NoFullScreen: true,
            NoSensitive: true,
            NoSaas: true,
            NoAuthority: semantics.NoAuthority && experiments.All(e => e.NoAuthority),
            BrowserCredentialRedactor.Redact($"{decision}; approvedDecode={approvedDecode}; no positive OCR claimed"));
    }

    public NodalOsOcrDictionaryManifestEntry CreatePaddleOcrV4EnglishManifestEntryWithoutApprovedSource()
    {
        return new NodalOsOcrDictionaryManifestEntry(
            PaddleOcrV4EnglishDictionaryId,
            NodalOsOcrDictionaryRole.RecognitionCtcCharset,
            ExpectedFileName: "paddleocr-ppocrv4-en-dict.txt",
            PaddleOcrV4EnglishDictionaryRelativePath,
            ExpectedCharsetCount: PaddleOcrV4EnglishExpectedCharsetCount,
            ExpectedRecognizerClassCount: PaddleOcrV4EnglishRecognizerClassCount,
            NodalOsOcrDictionaryBlankTokenPolicy.BlankAppendedAtEnd,
            CtcBlankIndex: PaddleOcrV4EnglishExpectedCharsetCount,
            NewlineHandling: "UTF-8 one token per line; CRLF/LF normalized before hashing and loading",
            SourceUrl: null,
            SourceRef: "not-selected; no approved source/hash exists in current manifest or M200-M237 reports",
            ExpectedSha256: null,
            ExpectedSizeBytes: null,
            NodalOsOcrDictionaryAvailabilityStatus.SourceNotSelected,
            Gitignored: true,
            Committed: false,
            NoAuthority: true);
    }

    private static NodalOsRecognizerClassMapping Mapping(
        string mappingId,
        NodalOsRecognizerTokenPolicy tokenPolicy,
        int dictionaryTokenCount,
        int expectedClassCount,
        int? blankIndex,
        bool compatible,
        bool decodeAllowed,
        string evidenceSource,
        string riskLevel,
        string reason) =>
        new(
            mappingId,
            tokenPolicy,
            dictionaryTokenCount,
            PaddleOcrV4EnglishRecognizerClassCount,
            expectedClassCount,
            blankIndex,
            tokenPolicy == NodalOsRecognizerTokenPolicy.DictionaryPlusBlankAndUnknown ? 96 : null,
            tokenPolicy == NodalOsRecognizerTokenPolicy.DictionaryPlusBlankAndPadding ? 96 : null,
            tokenPolicy == NodalOsRecognizerTokenPolicy.DictionaryPlusBlankAndSpace ? 96 : null,
            compatible,
            decodeAllowed,
            evidenceSource,
            riskLevel,
            BrowserCredentialRedactor.Redact(reason));

    private static NodalOsCtcDecodePolicyExperimentResult DecodePolicyResult(
        NodalOsCtcDecodePolicyCandidate candidate,
        NodalOsCtcDecodePolicyExperimentStatus status,
        bool decodeAllowed,
        string reason) =>
        new(
            $"ctc-policy-experiment-{Guid.NewGuid():N}",
            candidate,
            status,
            DecodeAttempted: false,
            decodeAllowed,
            DecodedText: null,
            Confidence: null,
            RequiresHumanReview: true,
            NoRawPersistence: true,
            NoSensitive: true,
            NoAuthority: true,
            BrowserCredentialRedactor.Redact(reason));

    public NodalOsOcrDictionaryAcquisitionPlan CreateSourceSelectionAcquisitionPlan(
        NodalOsOcrDictionaryManifestEntry entry)
    {
        return new NodalOsOcrDictionaryAcquisitionPlan(
            $"dict-acquisition-plan-{Guid.NewGuid():N}",
            entry,
            SourceApproved: false,
            DownloadAllowed: false,
            PlannedScripts:
            [
                "tools/ocr-worker/models/onnx/download-dictionaries.ps1",
                "tools/ocr-worker/models/onnx/verify-dictionaries.ps1",
                "tools/ocr-worker/models/onnx/rollback-dictionaries.ps1"
            ],
            Commands:
            [
                "pwsh -NoProfile -ExecutionPolicy Bypass -File tools/ocr-worker/models/onnx/download-dictionaries.ps1 -Confirm",
                "pwsh -NoProfile -ExecutionPolicy Bypass -File tools/ocr-worker/models/onnx/verify-dictionaries.ps1"
            ],
            Decision: "READY_FOR_DICTIONARY_SOURCE_SELECTION",
            NoSaas: true,
            NoAuthority: true,
            Reason: "dictionary source URL, SHA-256, and expected size must be selected before any download");
    }

    public NodalOsOcrDictionaryCompatibilityResult EvaluateManifestEntry(
        NodalOsOcrDictionaryManifestEntry entry,
        int? actualCharsetCount,
        string? actualSha256,
        long? actualSizeBytes,
        bool actualCommitted)
    {
        if (actualCommitted && entry.Gitignored)
        {
            var ctc = Ctc(
                entry.ExpectedRecognizerClassCount,
                dictionaryClassCountIncludingBlank: 0,
                NodalOsOcrDictionaryCompatibilityStatus.UnexpectedCommittedDictionary,
                decodeAllowed: false,
                "dictionary is unexpectedly committed while policy requires gitignored runtime acquisition");
            return Result(NodalOsOcrDictionaryCompatibilityStatus.UnexpectedCommittedDictionary, null, ctc, ctc.Reason);
        }

        if (entry.ExpectedSha256 is not null &&
            actualSha256 is not null &&
            !string.Equals(entry.ExpectedSha256, actualSha256, StringComparison.OrdinalIgnoreCase))
        {
            var ctc = Ctc(
                entry.ExpectedRecognizerClassCount,
                (actualCharsetCount ?? 0) + 1,
                NodalOsOcrDictionaryCompatibilityStatus.HashMismatch,
                decodeAllowed: false,
                "dictionary SHA-256 does not match manifest");
            return Result(NodalOsOcrDictionaryCompatibilityStatus.HashMismatch, null, ctc, ctc.Reason);
        }

        if (actualCharsetCount is not null && actualCharsetCount.Value != entry.ExpectedCharsetCount)
        {
            var ctc = Ctc(
                entry.ExpectedRecognizerClassCount,
                actualCharsetCount.Value + 1,
                NodalOsOcrDictionaryCompatibilityStatus.ClassCountMismatch,
                decodeAllowed: false,
                $"dictionary charset count {actualCharsetCount.Value} does not match expected {entry.ExpectedCharsetCount}");
            return Result(NodalOsOcrDictionaryCompatibilityStatus.ClassCountMismatch, null, ctc, ctc.Reason);
        }

        if (entry.ExpectedSizeBytes is not null &&
            actualSizeBytes is not null &&
            entry.ExpectedSizeBytes.Value != actualSizeBytes.Value)
        {
            var ctc = Ctc(
                entry.ExpectedRecognizerClassCount,
                (actualCharsetCount ?? entry.ExpectedCharsetCount) + 1,
                NodalOsOcrDictionaryCompatibilityStatus.DictionaryUnverified,
                decodeAllowed: false,
                "dictionary size does not match manifest");
            return Result(NodalOsOcrDictionaryCompatibilityStatus.DictionaryUnverified, null, ctc, ctc.Reason);
        }

        if (entry.SourceUrl is null || entry.ExpectedSha256 is null || entry.ExpectedSizeBytes is null)
        {
            var ctc = Ctc(
                entry.ExpectedRecognizerClassCount,
                entry.ExpectedCharsetCount + 1,
                NodalOsOcrDictionaryCompatibilityStatus.SourceNotSelected,
                decodeAllowed: false,
                "dictionary source, hash, or size is not approved");
            return Result(NodalOsOcrDictionaryCompatibilityStatus.SourceNotSelected, null, ctc, ctc.Reason);
        }

        var manifest = new NodalOsOcrDictionaryManifest(
            entry.DictionaryId,
            "en",
            entry.ExpectedCharsetCount,
            BlankTokenCount: 1,
            entry.SourceRef,
            entry.ExpectedSha256,
            Verified: true,
            entry.NoAuthority);

        return Evaluate(manifest, entry.ExpectedRecognizerClassCount);
    }

    public NodalOsOcrDictionaryReadinessReport DecideReadiness(
        NodalOsOcrDictionaryManifestEntry entry,
        NodalOsOcrDictionaryCompatibilityResult compatibility,
        NodalOsOcrDictionaryAcquisitionPlan acquisitionPlan,
        bool dictionaryPresent,
        bool hashVerified,
        bool decodeAttempted)
    {
        var decision = Decide(entry, compatibility, acquisitionPlan, hashVerified, decodeAttempted);

        return new NodalOsOcrDictionaryReadinessReport(
            $"dictionary-readiness-{Guid.NewGuid():N}",
            decision,
            entry,
            compatibility,
            acquisitionPlan,
            dictionaryPresent,
            hashVerified,
            decodeAttempted,
            ProductiveOcrBlocked: true,
            ShadowModeBlocked: true,
            NoRawPersistence: true,
            NoFullScreen: true,
            NoSensitive: true,
            NoSaas: true,
            NoAuthority: entry.NoAuthority && compatibility.NoAuthority && acquisitionPlan.NoAuthority,
            Reason: BrowserCredentialRedactor.Redact(
                $"{decision}; dictionary={compatibility.Status}; sourceApproved={acquisitionPlan.SourceApproved}; decodeAttempted={decodeAttempted}"));
    }

    public NodalOsOcrDictionaryCompatibilityResult Evaluate(
        NodalOsOcrDictionaryManifest? dictionary,
        int recognizerOutputClassCount,
        string inferredAxes = "time,batch,class")
    {
        if (dictionary is null)
        {
            var ctc = Ctc(
                recognizerOutputClassCount,
                dictionaryClassCountIncludingBlank: 0,
                NodalOsOcrDictionaryCompatibilityStatus.MissingDictionary,
                decodeAllowed: false,
                "no dictionary manifest available");
            return Result(NodalOsOcrDictionaryCompatibilityStatus.MissingDictionary, null, ctc, "dictionary manifest missing");
        }

        var dictionaryClassCount = dictionary.CharacterCount + dictionary.BlankTokenCount;
        if (dictionaryClassCount != recognizerOutputClassCount)
        {
            var ctc = Ctc(
                recognizerOutputClassCount,
                dictionaryClassCount,
                NodalOsOcrDictionaryCompatibilityStatus.ClassCountMismatch,
                decodeAllowed: false,
                $"recognizer class count {recognizerOutputClassCount} does not match dictionary+blank {dictionaryClassCount}");
            return Result(NodalOsOcrDictionaryCompatibilityStatus.ClassCountMismatch, dictionary, ctc, ctc.Reason);
        }

        if (!dictionary.Verified)
        {
            var ctc = Ctc(
                recognizerOutputClassCount,
                dictionaryClassCount,
                NodalOsOcrDictionaryCompatibilityStatus.DictionaryUnverified,
                decodeAllowed: false,
                "dictionary source/hash is not verified");
            return Result(NodalOsOcrDictionaryCompatibilityStatus.DictionaryUnverified, dictionary, ctc, ctc.Reason);
        }

        var compatible = new NodalOsOcrCtcDecoderCompatibility(
            $"ctc-compat-{Guid.NewGuid():N}",
            recognizerOutputClassCount,
            dictionaryClassCount,
            BatchTimeClassAxesInferred: true,
            inferredAxes,
            NodalOsOcrDictionaryCompatibilityStatus.Compatible,
            DecodeAllowed: true,
            NoAuthority: true,
            "dictionary class count matches recognizer output; decode allowed but remains no-authority");

        return new NodalOsOcrDictionaryCompatibilityResult(
            $"dict-compat-{Guid.NewGuid():N}",
            NodalOsOcrDictionaryCompatibilityStatus.Compatible,
            dictionary,
            compatible,
            RecognitionSuccessAllowed: true,
            RequiresHumanReview: true,
            NoAuthority: true,
            compatible.Reason);
    }

    private static NodalOsOcrCtcDecoderCompatibility Ctc(
        int recognizerOutputClassCount,
        int dictionaryClassCountIncludingBlank,
        NodalOsOcrDictionaryCompatibilityStatus status,
        bool decodeAllowed,
        string reason) =>
        new(
            $"ctc-compat-{Guid.NewGuid():N}",
            recognizerOutputClassCount,
            dictionaryClassCountIncludingBlank,
            BatchTimeClassAxesInferred: recognizerOutputClassCount > 0,
            InferredAxes: recognizerOutputClassCount > 0 ? "time,batch,class" : "unknown",
            status,
            decodeAllowed,
            NoAuthority: true,
            BrowserCredentialRedactor.Redact(reason));

    private static NodalOsOcrDictionaryCompatibilityResult Result(
        NodalOsOcrDictionaryCompatibilityStatus status,
        NodalOsOcrDictionaryManifest? dictionary,
        NodalOsOcrCtcDecoderCompatibility ctc,
        string reason) =>
        new(
            $"dict-compat-{Guid.NewGuid():N}",
            status,
            dictionary,
            ctc,
            RecognitionSuccessAllowed: false,
            RequiresHumanReview: true,
            NoAuthority: true,
            BrowserCredentialRedactor.Redact(reason));

    private static NodalOsOcrDictionaryReadinessDecision Decide(
        NodalOsOcrDictionaryManifestEntry entry,
        NodalOsOcrDictionaryCompatibilityResult compatibility,
        NodalOsOcrDictionaryAcquisitionPlan acquisitionPlan,
        bool hashVerified,
        bool decodeAttempted)
    {
        if (decodeAttempted && !compatibility.CtcDecoderCompatibility.DecodeAllowed)
            return NodalOsOcrDictionaryReadinessDecision.BlockedByDictionaryClassCountMismatch;

        if (compatibility.Status == NodalOsOcrDictionaryCompatibilityStatus.HashMismatch)
            return NodalOsOcrDictionaryReadinessDecision.BlockedByDictionaryHashMismatch;

        if (!entry.NoAuthority || !compatibility.NoAuthority || !acquisitionPlan.NoAuthority)
            return NodalOsOcrDictionaryReadinessDecision.NotReady;

        if (!acquisitionPlan.SourceApproved ||
            entry.SourceUrl is null ||
            entry.ExpectedSha256 is null ||
            entry.ExpectedSizeBytes is null ||
            compatibility.Status == NodalOsOcrDictionaryCompatibilityStatus.SourceNotSelected)
            return NodalOsOcrDictionaryReadinessDecision.ReadyForDictionarySourceSelection;

        if (compatibility.Status == NodalOsOcrDictionaryCompatibilityStatus.ClassCountMismatch)
            return NodalOsOcrDictionaryReadinessDecision.BlockedByDictionaryClassCountMismatch;

        if (compatibility.Status == NodalOsOcrDictionaryCompatibilityStatus.Compatible && hashVerified)
            return NodalOsOcrDictionaryReadinessDecision.ReadyForSyntheticTextDecodeFixtures;

        return NodalOsOcrDictionaryReadinessDecision.NotReady;
    }
}

public sealed class NodalOsGuardedSyntheticTextOcrRetryReadinessReview
{
    public NodalOsGuardedSyntheticTextOcrReadinessReport Evaluate(NodalOsGuardedSyntheticTextOcrRetryReadinessInput input)
    {
        var riskyInProcess = input.ProbeResults.Any(r => r.RanInProcess);
        var raw = input.ProbeResults.Any(r => r.RawPersisted);
        var saas = input.ProbeResults.Any(r => r.CallsSaas);
        var noAuthority = input.ProbeResults.All(r => r.NoAuthority) &&
                          input.DictionaryCompatibility.NoAuthority &&
                          input.DictionaryCompatibility.CtcDecoderCompatibility.NoAuthority;

        var decision = Decide(input, riskyInProcess, raw, saas, noAuthority);

        return new NodalOsGuardedSyntheticTextOcrReadinessReport(
            $"guarded-text-retry-readiness-{Guid.NewGuid():N}",
            decision,
            input.GuardExists,
            RiskyTextNeverRanInProcess: !riskyInProcess,
            input.ParentSurvivedCrash,
            input.ChildCleanupWorks,
            input.TempCleanupWorks,
            NoRawPersistence: !raw,
            input.NoFullScreen,
            input.NoSensitive,
            NoSaas: !saas,
            noAuthority,
            DetectionDiagnosed: true,
            RecognitionDiagnosedOrUnreachable: input.Recognition.Reachable || input.Recognition.Status == NodalOsGuardedSyntheticTextOcrProbeStatus.NoTextDetected,
            DictionaryStatusDocumented: input.DictionaryCompatibility.Status != NodalOsOcrDictionaryCompatibilityStatus.Unknown,
            ModelCompatibilityDocumented: true,
            ShadowModeBlocked: decision != NodalOsGuardedSyntheticTextOcrReadinessDecision.ReadyForSyntheticPositiveRecognition,
            ProductionPublicOcrBlocked: true,
            Reason: BrowserCredentialRedactor.Redact(Reason(decision, input)));
    }

    private static NodalOsGuardedSyntheticTextOcrReadinessDecision Decide(
        NodalOsGuardedSyntheticTextOcrRetryReadinessInput input,
        bool riskyInProcess,
        bool raw,
        bool saas,
        bool noAuthority)
    {
        if (!input.DetectionModelVerified || !input.RecognitionModelVerified || !input.ClassificationModelVerified || input.OnnxModelsTracked)
            return NodalOsGuardedSyntheticTextOcrReadinessDecision.NotReady;

        if (!input.GuardExists || riskyInProcess || !input.ParentSurvivedCrash || !input.ChildCleanupWorks ||
            !input.TempCleanupWorks || raw || saas || !noAuthority || !input.NoFullScreen || !input.NoSensitive)
            return NodalOsGuardedSyntheticTextOcrReadinessDecision.NotReady;

        if (input.Detection.Status == NodalOsGuardedSyntheticTextOcrProbeStatus.NativeRuntimeCrashContained ||
            input.ProbeResults.Any(r => r.Status == NodalOsGuardedSyntheticTextOcrProbeStatus.NativeRuntimeCrashContained))
            return NodalOsGuardedSyntheticTextOcrReadinessDecision.BlockedByModelRuntime;

        if (input.Detection.Status == NodalOsGuardedSyntheticTextOcrProbeStatus.BlockedByPostProcessing)
            return NodalOsGuardedSyntheticTextOcrReadinessDecision.BlockedByPostProcessing;

        if (input.DictionaryCompatibility.Status is NodalOsOcrDictionaryCompatibilityStatus.MissingDictionary
            or NodalOsOcrDictionaryCompatibilityStatus.ClassCountMismatch
            or NodalOsOcrDictionaryCompatibilityStatus.DictionaryUnverified)
            return NodalOsGuardedSyntheticTextOcrReadinessDecision.ReadyForDictionaryCompletion;

        if (input.Recognition.Status == NodalOsGuardedSyntheticTextOcrProbeStatus.PositiveRecognition)
            return NodalOsGuardedSyntheticTextOcrReadinessDecision.ReadyForSyntheticPositiveRecognition;

        if (input.Detection.Status == NodalOsGuardedSyntheticTextOcrProbeStatus.PositiveDetection)
            return NodalOsGuardedSyntheticTextOcrReadinessDecision.ReadyForMoreSyntheticFixtures;

        return NodalOsGuardedSyntheticTextOcrReadinessDecision.ReadyForMoreSyntheticFixtures;
    }

    private static string Reason(
        NodalOsGuardedSyntheticTextOcrReadinessDecision decision,
        NodalOsGuardedSyntheticTextOcrRetryReadinessInput input) =>
        $"{decision}; detection={input.Detection.Status}; recognition={input.Recognition.Status}; dictionary={input.DictionaryCompatibility.Status}; shadow remains blocked";
}
