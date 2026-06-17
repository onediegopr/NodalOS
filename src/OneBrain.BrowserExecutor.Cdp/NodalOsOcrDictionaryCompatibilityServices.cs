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
    public const int PaddleOcrV5EnglishDictionaryTokenCount = 436;
    public const int PaddleOcrV5EnglishExpectedClassCountWithBlank = PaddleOcrV5EnglishDictionaryTokenCount + 1;
    public const int PaddleOcrV5EnglishObservedRecognizerClassCount = 438;

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

    public IReadOnlyList<NodalOsRecognizerDictionaryPairCandidate> CreateM250ReplacementCandidates()
    {
        return
        [
            new NodalOsRecognizerDictionaryPairCandidate(
                "rapidocr-modelscope-ppocrv5-en-mobile-onnx",
                new NodalOsRecognizerDictionaryPairCandidateSource(
                    "rapidocr-default-models-ppocrv5-en",
                    "RapidAI/RapidOCR + ModelScope",
                    "https://www.modelscope.cn/models/RapidAI/RapidOCR/resolve/v3.8.0/onnx/PP-OCRv5/rec/en_PP-OCRv5_rec_mobile.onnx",
                    "https://www.modelscope.cn/models/RapidAI/RapidOCR/resolve/v3.8.0/paddle/PP-OCRv5/rec/en_PP-OCRv5_rec_mobile/ppocrv5_en_dict.txt",
                    "Apache-2.0 lineage",
                    "RapidOCR default_models.yaml lists ONNX recognizer SHA and explicit dict_url",
                    Official: true,
                    Verifiable: true),
                "en_PP-OCRv5_rec_mobile.onnx",
                "ppocrv5_en_dict.txt",
                "PP-OCRv5 mobile recognition",
                "en",
                "NCHW [1,3,48,W] or model metadata to be confirmed during acquisition",
                ExpectedOutputClassCount: 437,
                DictionaryTokenCount: 436,
                CtcBlankIndex: 0,
                OnnxAvailable: true,
                ModelHashPinned: true,
                ModelSizePinned: false,
                "c3461add59bb4323ecba96a492ab75e06dda42467c9e3d0c18db5d1d21924be8",
                ModelSizeBytes: null,
                DictionaryExplicit: true,
                DictionaryHashPinned: true,
                DictionarySizePinned: true,
                "e025a66d31f327ba0c232e03f407ae8d105e1e709e7ccb3f408aa778c24e70d6",
                1416,
                CompatibleWithOnnxRuntime1221: true,
                "runtime not yet tested locally; model size not pinned until controlled acquisition"),
            new NodalOsRecognizerDictionaryPairCandidate(
                "rapidocr-modelscope-ppocrv5-latin-mobile-onnx",
                new NodalOsRecognizerDictionaryPairCandidateSource(
                    "rapidocr-default-models-ppocrv5-latin",
                    "RapidAI/RapidOCR + ModelScope",
                    "https://www.modelscope.cn/models/RapidAI/RapidOCR/resolve/v3.8.0/onnx/PP-OCRv5/rec/latin_PP-OCRv5_rec_mobile.onnx",
                    "https://www.modelscope.cn/models/RapidAI/RapidOCR/resolve/v3.8.0/paddle/PP-OCRv5/rec/latin_PP-OCRv5_rec_mobile/ppocrv5_latin_dict.txt",
                    "Apache-2.0 lineage",
                    "RapidOCR default_models.yaml lists ONNX recognizer SHA and explicit dict_url",
                    Official: true,
                    Verifiable: true),
                "latin_PP-OCRv5_rec_mobile.onnx",
                "ppocrv5_latin_dict.txt",
                "PP-OCRv5 mobile recognition",
                "latin",
                "NCHW [1,3,48,W] or model metadata to be confirmed during acquisition",
                ExpectedOutputClassCount: 503,
                DictionaryTokenCount: 502,
                CtcBlankIndex: 0,
                OnnxAvailable: true,
                ModelHashPinned: true,
                ModelSizePinned: false,
                "b20bd37c168a570f583afbc8cd7925603890efbcdc000a59e22c269d160b5f5a",
                ModelSizeBytes: null,
                DictionaryExplicit: true,
                DictionaryHashPinned: true,
                DictionarySizePinned: true,
                "3c0a8a79b612653c25f765271714f71281e4e955962c153e272b7b8c1d2b13ff",
                1634,
                CompatibleWithOnnxRuntime1221: true,
                "broader charset than required for current English synthetic fixtures; higher migration impact"),
            new NodalOsRecognizerDictionaryPairCandidate(
                "rapidocr-modelscope-ppocrv4-en-current-onnx",
                new NodalOsRecognizerDictionaryPairCandidateSource(
                    "rapidocr-default-models-ppocrv4-en-current",
                    "RapidAI/RapidOCR + ModelScope",
                    RapidOcrModelScopeEnglishDictionaryUrl.Replace("/paddle/", "/onnx/").Replace("/en_PP-OCRv4_rec_mobile/en_dict.txt", ".onnx"),
                    RapidOcrModelScopeEnglishDictionaryUrl,
                    "Apache-2.0 lineage",
                    "current verified model/dictionary pair under investigation",
                    Official: true,
                    Verifiable: true),
                "en_PP-OCRv4_rec_mobile.onnx",
                "en_dict.txt",
                "PP-OCRv4 mobile recognition",
                "en",
                "NCHW [1,3,32,W]",
                ExpectedOutputClassCount: 97,
                DictionaryTokenCount: OfficialEnglishDictionaryTokenCount,
                CtcBlankIndex: 0,
                OnnxAvailable: true,
                ModelHashPinned: true,
                ModelSizePinned: true,
                "e8770c967605983d1570cdf5352041dfb68fa0c21664f49f47b155abd3e0e318",
                7653044,
                DictionaryExplicit: true,
                DictionaryHashPinned: true,
                DictionarySizePinned: true,
                EnglishDictionarySha256,
                EnglishDictionarySizeBytes,
                CompatibleWithOnnxRuntime1221: true,
                "rejected because 95 dictionary tokens plus CTC blank index 0 explains 96 classes, not 97"),
            new NodalOsRecognizerDictionaryPairCandidate(
                "paddleocr-huggingface-ppocrv4-mobile-rec",
                new NodalOsRecognizerDictionaryPairCandidateSource(
                    "huggingface-paddlepaddle-ppocrv4-mobile-rec",
                    "PaddlePaddle/HuggingFace",
                    "https://huggingface.co/PaddlePaddle/PP-OCRv4_mobile_rec",
                    null,
                    "Apache-2.0",
                    "model card/config source only; ONNX availability not established",
                    Official: true,
                    Verifiable: true),
                "PP-OCRv4_mobile_rec",
                "not-explicit",
                "PP-OCRv4 mobile recognition",
                "multi",
                "unknown",
                ExpectedOutputClassCount: null,
                DictionaryTokenCount: null,
                CtcBlankIndex: 0,
                OnnxAvailable: false,
                ModelHashPinned: false,
                ModelSizePinned: false,
                ModelSha256: null,
                ModelSizeBytes: null,
                DictionaryExplicit: false,
                DictionaryHashPinned: false,
                DictionarySizePinned: false,
                DictionarySha256: null,
                DictionarySizeBytes: null,
                CompatibleWithOnnxRuntime1221: false,
                "metadata-only candidate; no explicit ONNX+dictionary pair")
        ];
    }

    public NodalOsRecognizerDictionaryPairCandidateAudit AuditReplacementCandidate(
        NodalOsRecognizerDictionaryPairCandidate candidate)
    {
        var decision =
            !candidate.Source.Official ? NodalOsRecognizerDictionaryPairCandidateDecision.RejectedUnofficial :
            !candidate.OnnxAvailable ? NodalOsRecognizerDictionaryPairCandidateDecision.RejectedNoOnnx :
            !candidate.DictionaryExplicit ? NodalOsRecognizerDictionaryPairCandidateDecision.RejectedNoExplicitDictionary :
            !candidate.ModelHashPinned || !candidate.DictionaryHashPinned || !candidate.DictionarySizePinned ? NodalOsRecognizerDictionaryPairCandidateDecision.RejectedUnpinnable :
            candidate.ExpectedOutputClassCount is not null && candidate.DictionaryTokenCount is not null &&
            candidate.ExpectedOutputClassCount != candidate.DictionaryTokenCount + 1 ? NodalOsRecognizerDictionaryPairCandidateDecision.RejectedCountMismatch :
            candidate.CandidateId.Contains("latin", StringComparison.OrdinalIgnoreCase) ? NodalOsRecognizerDictionaryPairCandidateDecision.CandidateNeedsManualReview :
            NodalOsRecognizerDictionaryPairCandidateDecision.CandidateAcceptedForAcquisition;

        var reason = decision switch
        {
            NodalOsRecognizerDictionaryPairCandidateDecision.CandidateAcceptedForAcquisition => "official ONNX recognizer and explicit dictionary are pinnable and class count is explained by dictionary tokens plus CTC blank",
            NodalOsRecognizerDictionaryPairCandidateDecision.CandidateNeedsManualReview => "candidate is explicit and pinnable but has broader language scope and higher migration impact",
            NodalOsRecognizerDictionaryPairCandidateDecision.RejectedCountMismatch => "candidate model output class count is not explained by dictionary token count plus CTC blank",
            NodalOsRecognizerDictionaryPairCandidateDecision.RejectedNoOnnx => "candidate lacks approved ONNX model availability",
            NodalOsRecognizerDictionaryPairCandidateDecision.RejectedNoExplicitDictionary => "candidate lacks explicit dictionary URL/ref",
            NodalOsRecognizerDictionaryPairCandidateDecision.RejectedUnpinnable => "candidate lacks required hash/size pinning evidence",
            NodalOsRecognizerDictionaryPairCandidateDecision.RejectedUnofficial => "candidate source is not official/verifiable",
            _ => "candidate has unresolved model risk"
        };

        return new NodalOsRecognizerDictionaryPairCandidateAudit(
            $"recognizer-dictionary-pair-candidate-audit-{Guid.NewGuid():N}",
            candidate,
            decision,
            DecodeAttempted: false,
            NoAuthority: true,
            reason);
    }

    public NodalOsRecognizerDictionaryPairCompatibilityMatrix CreateM251CompatibilityMatrix()
    {
        var audits = CreateM250ReplacementCandidates()
            .Select(AuditReplacementCandidate)
            .ToArray();
        var selected = audits.FirstOrDefault(audit =>
            audit.Decision == NodalOsRecognizerDictionaryPairCandidateDecision.CandidateAcceptedForAcquisition);

        var decision = selected is not null
            ? NodalOsRecognizerDictionaryPairReplacementDecision.ReadyForRecognizerDictionaryPairAcquisition
            : audits.Any(audit => audit.Decision == NodalOsRecognizerDictionaryPairCandidateDecision.CandidateNeedsManualReview)
                ? NodalOsRecognizerDictionaryPairReplacementDecision.ReadyForManualPairApproval
                : audits.Any(audit => audit.Decision == NodalOsRecognizerDictionaryPairCandidateDecision.RejectedUnpinnable)
                    ? NodalOsRecognizerDictionaryPairReplacementDecision.BlockedByUnpinnablePair
                    : NodalOsRecognizerDictionaryPairReplacementDecision.BlockedByNoCompatibleRecognizerDictionaryPair;

        return new NodalOsRecognizerDictionaryPairCompatibilityMatrix(
            $"recognizer-dictionary-pair-compatibility-matrix-{Guid.NewGuid():N}",
            audits,
            selected,
            decision,
            ProductiveOcrBlocked: true,
            ShadowModeBlocked: true,
            DecodeBlocked: true,
            NoRawPersistence: true,
            NoFullScreen: true,
            NoSensitive: true,
            NoSaas: true,
            NoAuthority: true,
            selected is null
                ? "no candidate reached acquisition readiness"
                : $"selected {selected.Candidate.CandidateId} for controlled acquisition planning; no model download or decode executed");
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

    public NodalOsPaddleOcrExtraClassSemantics AuditPaddleOcrExtraClassSemantics()
    {
        var candidates = new[]
        {
            ExtraClassCandidate(
                "official-ctc-blank-only",
                "dictionary + PaddleOCR CTC blank",
                "dictionary tokens + 1 blank at index 0",
                supportsV4: false,
                supportsV5: false,
                evidenceApproved: true,
                evidenceSource: "PaddleOCR CTCLabelDecode.add_special_char prepends blank and get_ignored_tokens returns [0]",
                risk: "low",
                decodeAllowed: false,
                "official CTC blank policy explains PP-OCRv4 96 classes and PP-OCRv5 437 classes, not observed 97/438"),
            ExtraClassCandidate(
                "hypothesis-ignored-extra-class",
                "dictionary + blank + ignored extra class",
                "dictionary tokens + 1 blank + 1 ignored/reserved class",
                supportsV4: true,
                supportsV5: true,
                evidenceApproved: false,
                evidenceSource: "hypothesis only; no PaddleOCR/RapidOCR CTC postprocessor source identified an ignored extra class beyond blank",
                risk: "high",
                decodeAllowed: false,
                "matches both observed output counts arithmetically but would require silently ignoring a model class without evidence"),
            ExtraClassCandidate(
                "hypothesis-unknown-token",
                "dictionary + blank + unknown",
                "dictionary tokens + 1 blank + 1 unknown token",
                supportsV4: true,
                supportsV5: true,
                evidenceApproved: false,
                evidenceSource: "hypothesis only; PaddleOCR CTCLabelDecode does not add unknown for CTC models",
                risk: "high",
                decodeAllowed: false,
                "unknown token semantics are present in other decoder families, not approved for this CTC recognizer pair"),
            ExtraClassCandidate(
                "hypothesis-padding-token",
                "dictionary + blank + padding",
                "dictionary tokens + 1 blank + 1 padding token",
                supportsV4: true,
                supportsV5: true,
                evidenceApproved: false,
                evidenceSource: "hypothesis only; padding appears in non-CTC decoder variants, not in CTCLabelDecode",
                risk: "high",
                decodeAllowed: false,
                "padding token would explain the count but lacks evidence for PP-OCRv4/PP-OCRv5 CTC"),
            ExtraClassCandidate(
                "rejected-terminal-empty-line-token",
                "terminal newline as token",
                "dictionary raw line segments + blank",
                supportsV4: false,
                supportsV5: false,
                evidenceApproved: false,
                evidenceSource: "PaddleOCR dictionary parser reads physical lines and strips CR/LF; terminal newline is not a token",
                risk: "medium",
                decodeAllowed: false,
                "raw dictionary analysis showed terminal empty segment is not significant under PaddleOCR parser"),
            ExtraClassCandidate(
                "model-export-or-pair-mismatch",
                "model export artifact or dictionary/model mismatch",
                "observed output class count is dictionary + blank + 1 with no approved token semantics",
                supportsV4: true,
                supportsV5: true,
                evidenceApproved: true,
                evidenceSource: "local ONNX runtime metadata and verified dictionary counts from M247-M255",
                risk: "high",
                decodeAllowed: false,
                "safest evidenced finding is unresolved extra class semantics, not an approved decoder mapping")
        };

        return new NodalOsPaddleOcrExtraClassSemantics(
            $"paddle-extra-class-semantics-{Guid.NewGuid():N}",
            OfficialEnglishDictionaryTokenCount,
            PpOcrV4BlankCount: 1,
            PaddleOcrV4EnglishRecognizerClassCount,
            PaddleOcrV5EnglishDictionaryTokenCount,
            PpOcrV5BlankCount: 1,
            PaddleOcrV5EnglishObservedRecognizerClassCount,
            candidates,
            ApprovedCandidate: null,
            OfficialBlankOnlyPolicyInsufficient: true,
            ExtraClassSemanticsResolved: false,
            DecodeAllowed: false,
            NoAuthority: true,
            "PP-OCRv4 and PP-OCRv5 both expose dictionary+blank+1, but no official CTC source approves the extra class semantics");
    }

    public IReadOnlyList<NodalOsPaddleOcrDecodeClassPolicy> CreatePaddleOcrDecodeClassPolicies()
    {
        return
        [
            DecodeClassPolicy(
                "ppocrv5-official-blank-only",
                "PP-OCRv5 dictionary + CTC blank",
                PaddleOcrV5EnglishDictionaryTokenCount,
                blankIndex: 0,
                PaddleOcrV5EnglishObservedRecognizerClassCount,
                expectedClassCount: PaddleOcrV5EnglishExpectedClassCountWithBlank,
                extraClassIndex: null,
                evidenceApproved: true,
                hypothesisOnly: false,
                evidenceSource: "PaddleOCR CTCLabelDecode official blank policy",
                allowsDecode: false,
                "official policy is approved but class count mismatches observed PP-OCRv5 output"),
            DecodeClassPolicy(
                "ppocrv5-hypothesis-ignore-extra",
                "PP-OCRv5 ignore extra class",
                PaddleOcrV5EnglishDictionaryTokenCount,
                blankIndex: 0,
                PaddleOcrV5EnglishObservedRecognizerClassCount,
                expectedClassCount: PaddleOcrV5EnglishObservedRecognizerClassCount,
                extraClassIndex: PaddleOcrV5EnglishObservedRecognizerClassCount - 1,
                evidenceApproved: false,
                hypothesisOnly: true,
                evidenceSource: "hypothesis only",
                allowsDecode: false,
                "ignoring the extra class would be unsafe without postprocessor evidence"),
            DecodeClassPolicy(
                "ppocrv5-hypothesis-unknown",
                "PP-OCRv5 unknown extra token",
                PaddleOcrV5EnglishDictionaryTokenCount,
                blankIndex: 0,
                PaddleOcrV5EnglishObservedRecognizerClassCount,
                expectedClassCount: PaddleOcrV5EnglishObservedRecognizerClassCount,
                extraClassIndex: PaddleOcrV5EnglishObservedRecognizerClassCount - 1,
                evidenceApproved: false,
                hypothesisOnly: true,
                evidenceSource: "hypothesis only",
                allowsDecode: false,
                "unknown token has not been proven for PP-OCRv5 CTC"),
            DecodeClassPolicy(
                "ppocrv5-hypothesis-padding",
                "PP-OCRv5 padding extra token",
                PaddleOcrV5EnglishDictionaryTokenCount,
                blankIndex: 0,
                PaddleOcrV5EnglishObservedRecognizerClassCount,
                expectedClassCount: PaddleOcrV5EnglishObservedRecognizerClassCount,
                extraClassIndex: PaddleOcrV5EnglishObservedRecognizerClassCount - 1,
                evidenceApproved: false,
                hypothesisOnly: true,
                evidenceSource: "hypothesis only",
                allowsDecode: false,
                "padding token has not been proven for PP-OCRv5 CTC")
        ];
    }

    public NodalOsPaddleOcrDecodePolicyApproval EvaluatePaddleOcrDecodeClassPolicy(
        NodalOsPaddleOcrDecodeClassPolicy policy)
    {
        if (policy.HypothesisOnly || !policy.EvidenceApproved)
        {
            return DecodePolicyApproval(
                policy,
                NodalOsPaddleOcrDecodePolicyExperimentStatus.HypothesisOnly,
                decodeAllowed: false,
                "policy is hypothesis-only or lacks evidence; decode blocked");
        }

        if (policy.ExpectedClassCount != policy.ModelClassCount)
        {
            return DecodePolicyApproval(
                policy,
                NodalOsPaddleOcrDecodePolicyExperimentStatus.Rejected,
                decodeAllowed: false,
                $"policy expects {policy.ExpectedClassCount} classes but model exposes {policy.ModelClassCount}");
        }

        if (!policy.AllowsDecode)
        {
            return DecodePolicyApproval(
                policy,
                NodalOsPaddleOcrDecodePolicyExperimentStatus.DecodeBlocked,
                decodeAllowed: false,
                "policy is evidenced but explicitly blocks decode");
        }

        return DecodePolicyApproval(
            policy,
            NodalOsPaddleOcrDecodePolicyExperimentStatus.Approved,
            decodeAllowed: true,
            "approved decode policy; text remains no-authority and requires human review");
    }

    public NodalOsPaddleOcrDecodePolicyDecisionReport DecidePaddleOcrDecodePolicy(
        NodalOsPaddleOcrExtraClassSemantics semantics,
        IReadOnlyList<NodalOsPaddleOcrDecodePolicyApproval> approvals)
    {
        var approved = approvals.Any(a =>
            a.Status == NodalOsPaddleOcrDecodePolicyExperimentStatus.Approved &&
            a.DecodeAllowed);
        var hypothesisOnly = approvals.Any(a => a.Status == NodalOsPaddleOcrDecodePolicyExperimentStatus.HypothesisOnly);
        var rejected = approvals.Any(a => a.Status == NodalOsPaddleOcrDecodePolicyExperimentStatus.Rejected);

        var decision = approved
            ? NodalOsPaddleOcrExtraClassDecision.ReadyForApprovedDecodePolicy
            : semantics.ExtraClassSemanticsResolved
                ? NodalOsPaddleOcrExtraClassDecision.ReadyForManualDecodePolicyApproval
                : hypothesisOnly || rejected
                    ? NodalOsPaddleOcrExtraClassDecision.BlockedByExtraClassSemantics
                    : NodalOsPaddleOcrExtraClassDecision.BlockedByDecodePolicyRisk;

        return new NodalOsPaddleOcrDecodePolicyDecisionReport(
            $"paddle-extra-class-decision-{Guid.NewGuid():N}",
            semantics,
            approvals,
            decision,
            ProductiveOcrBlocked: true,
            ShadowModeBlocked: true,
            DecodeSuccessClaimed: false,
            NoRawPersistence: true,
            NoFullScreen: true,
            NoSensitive: true,
            NoSaas: true,
            NoAuthority: semantics.NoAuthority && approvals.All(a => a.NoAuthority),
            BrowserCredentialRedactor.Redact($"{decision}; approvedDecode={approved}; decode success not claimed"));
    }

    public NodalOsOcrRecognizerReplacementStrategyMatrix CreateRecognizerReplacementStrategyMatrix()
    {
        var strategies = new[]
        {
            Strategy(
                "claude-extra-class-deep-audit",
                "External deep semantics audit before any decode approval",
                Candidate(
                    "paddleocr-rapidocr-current-plus-candidate",
                    "Current PP-OCRv4 and candidate PP-OCRv5 evidence package",
                    "official PaddleOCR/RapidOCR sources plus local verified runtime evidence",
                    localOffline: true,
                    onnxRuntimeSupported: true,
                    dictionaryExplicit: true,
                    classCountClear: false,
                    noSaas: true,
                    noAuthorityCompatible: true,
                    risk: "low implementation risk; decision risk delegated to explicit audit",
                    maintenance: "minimal code churn; preserves existing guardrails"),
                NodalOsOcrRecognizerReplacementStrategyStatus.Recommended,
                rank: 1,
                requiresManualApproval: false,
                decodeAutoApproved: false,
                "best next step because no official source currently approves the extra class semantics"),
            Strategy(
                "manual-extra-class-policy-approval",
                "Continue PP-OCRv5 with manual decode policy approval",
                Candidate(
                    "ppocrv5-en-extra-class-policy",
                    "PP-OCRv5 English candidate with manually approved extra-class handling",
                    "verified ModelScope/RapidOCR pair, but extra class semantics unresolved",
                    localOffline: true,
                    onnxRuntimeSupported: true,
                    dictionaryExplicit: true,
                    classCountClear: false,
                    noSaas: true,
                    noAuthorityCompatible: true,
                    risk: "high; would approve hypothesis-only policy unless external evidence is added",
                    maintenance: "low code churn but high audit burden"),
                NodalOsOcrRecognizerReplacementStrategyStatus.ViableNeedsApproval,
                rank: 2,
                requiresManualApproval: true,
                decodeAutoApproved: false,
                "manual approval cannot auto-enable decode or productive OCR"),
            Strategy(
                "recognizer-model-replacement-search",
                "Search for another explicit recognizer+dictionary pair",
                Candidate(
                    "future-explicit-recognizer-dictionary-pair",
                    "Alternative ONNX recognizer with class count fully explained by dictionary policy",
                    "must be official, pinnable, local, and explicit",
                    localOffline: true,
                    onnxRuntimeSupported: true,
                    dictionaryExplicit: true,
                    classCountClear: true,
                    noSaas: true,
                    noAuthorityCompatible: true,
                    risk: "medium; acquisition/runtime compatibility unknown",
                    maintenance: "medium; new manifest/download/verify/runtime smoke required"),
                NodalOsOcrRecognizerReplacementStrategyStatus.ViableNeedsResearch,
                rank: 3,
                requiresManualApproval: false,
                decodeAutoApproved: false,
                "preferred over inference-only extra-class policy if Claude audit cannot resolve semantics"),
            Strategy(
                "rapidocr-postprocessor-convention-adoption",
                "Adopt RapidOCR postprocessor conventions directly",
                Candidate(
                    "rapidocr-postprocessor-port",
                    "RapidOCR postprocessor convention port",
                    "official RapidOCR implementation, but requires legal/architecture review before porting",
                    localOffline: true,
                    onnxRuntimeSupported: true,
                    dictionaryExplicit: true,
                    classCountClear: false,
                    noSaas: true,
                    noAuthorityCompatible: true,
                    risk: "medium-high; may still not explain extra class without metadata behavior",
                    maintenance: "medium-high; porting and license/provenance review required"),
                NodalOsOcrRecognizerReplacementStrategyStatus.ViableNeedsApproval,
                rank: 4,
                requiresManualApproval: true,
                decodeAutoApproved: false,
                "cannot be used as silent approval of extra class semantics"),
            Strategy(
                "alternative-local-ocr-family-review",
                "Review another local OCR ONNX family",
                Candidate(
                    "alternative-local-onnx-ocr-family",
                    "Non-Paddle local OCR family",
                    "source must be official/verifiable and offline-compatible",
                    localOffline: true,
                    onnxRuntimeSupported: true,
                    dictionaryExplicit: false,
                    classCountClear: false,
                    noSaas: true,
                    noAuthorityCompatible: true,
                    risk: "medium-high; integration and quality unknown",
                    maintenance: "high; new model family contracts and probes required"),
                NodalOsOcrRecognizerReplacementStrategyStatus.ViableNeedsResearch,
                rank: 5,
                requiresManualApproval: false,
                decodeAutoApproved: false,
                "valid fallback if PaddleOCR/RapidOCR class semantics remain unresolved"),
            Strategy(
                "tesseract-local-fallback",
                "Tesseract local fallback",
                Candidate(
                    "tesseract-local-fallback",
                    "Tesseract OCR local fallback",
                    "mature local OCR, non-ONNX and lower-priority for current architecture",
                    localOffline: true,
                    onnxRuntimeSupported: false,
                    dictionaryExplicit: true,
                    classCountClear: true,
                    noSaas: true,
                    noAuthorityCompatible: true,
                    risk: "medium; architecture mismatch and quality variance",
                    maintenance: "high; separate runtime/deployment path"),
                NodalOsOcrRecognizerReplacementStrategyStatus.ViableNeedsResearch,
                rank: 6,
                requiresManualApproval: false,
                decodeAutoApproved: false,
                "local fallback only, not a replacement for current ONNX decision gate"),
            Strategy(
                "keep-ocr-blocked",
                "Keep OCR blocked until clean pair exists",
                Candidate(
                    "blocked-state",
                    "No recognizer decode",
                    "current safest operational state",
                    localOffline: true,
                    onnxRuntimeSupported: true,
                    dictionaryExplicit: false,
                    classCountClear: false,
                    noSaas: true,
                    noAuthorityCompatible: true,
                    risk: "low safety risk; no feature progress",
                    maintenance: "low"),
                NodalOsOcrRecognizerReplacementStrategyStatus.Blocked,
                rank: 7,
                requiresManualApproval: false,
                decodeAutoApproved: false,
                "safe default if no strategy is approved")
        };

        var recommended = strategies.OrderBy(strategy => strategy.Rank).First();

        return new NodalOsOcrRecognizerReplacementStrategyMatrix(
            $"ocr-recognizer-replacement-strategy-{Guid.NewGuid():N}",
            strategies,
            recommended,
            NodalOsOcrRecognizerReplacementDecision.ReadyForClaudeExtraClassAudit,
            ExtraClassUnresolved: true,
            ProductiveOcrBlocked: true,
            ShadowModeBlocked: true,
            DecodeBlocked: true,
            NoRawPersistence: true,
            NoFullScreen: true,
            NoSensitive: true,
            NoSaas: true,
            NoAuthority: true,
            "external semantics audit is required before manual policy approval or model-family replacement");
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

    private static NodalOsPaddleOcrExtraClassCandidate ExtraClassCandidate(
        string candidateId,
        string name,
        string expectedClassCountFormula,
        bool supportsV4,
        bool supportsV5,
        bool evidenceApproved,
        string evidenceSource,
        string risk,
        bool decodeAllowed,
        string reason) =>
        new(
            candidateId,
            name,
            expectedClassCountFormula,
            supportsV4,
            supportsV5,
            evidenceApproved,
            evidenceSource,
            risk,
            decodeAllowed,
            BrowserCredentialRedactor.Redact(reason));

    private static NodalOsPaddleOcrDecodeClassPolicy DecodeClassPolicy(
        string policyId,
        string name,
        int dictionaryTokenCount,
        int blankIndex,
        int modelClassCount,
        int expectedClassCount,
        int? extraClassIndex,
        bool evidenceApproved,
        bool hypothesisOnly,
        string evidenceSource,
        bool allowsDecode,
        string reason) =>
        new(
            policyId,
            name,
            dictionaryTokenCount,
            blankIndex,
            modelClassCount,
            expectedClassCount,
            extraClassIndex,
            evidenceApproved,
            hypothesisOnly,
            evidenceSource,
            allowsDecode,
            BrowserCredentialRedactor.Redact(reason));

    private static NodalOsPaddleOcrDecodePolicyApproval DecodePolicyApproval(
        NodalOsPaddleOcrDecodeClassPolicy policy,
        NodalOsPaddleOcrDecodePolicyExperimentStatus status,
        bool decodeAllowed,
        string reason) =>
        new(
            $"paddle-decode-policy-{Guid.NewGuid():N}",
            policy,
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

    private static NodalOsOcrModelFamilyCandidate Candidate(
        string candidateId,
        string name,
        string sourceQuality,
        bool localOffline,
        bool onnxRuntimeSupported,
        bool dictionaryExplicit,
        bool classCountClear,
        bool noSaas,
        bool noAuthorityCompatible,
        string risk,
        string maintenance) =>
        new(
            candidateId,
            name,
            sourceQuality,
            localOffline,
            onnxRuntimeSupported,
            dictionaryExplicit,
            classCountClear,
            noSaas,
            noAuthorityCompatible,
            BrowserCredentialRedactor.Redact(risk),
            BrowserCredentialRedactor.Redact(maintenance));

    private static NodalOsOcrRecognizerReplacementStrategy Strategy(
        string strategyId,
        string name,
        NodalOsOcrModelFamilyCandidate candidate,
        NodalOsOcrRecognizerReplacementStrategyStatus status,
        int rank,
        bool requiresManualApproval,
        bool decodeAutoApproved,
        string reason) =>
        new(
            strategyId,
            name,
            candidate,
            status,
            rank,
            requiresManualApproval,
            decodeAutoApproved,
            ProductiveOcrBlocked: true,
            ShadowModeBlocked: true,
            NoAuthority: true,
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
