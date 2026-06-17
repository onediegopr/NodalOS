using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.BrowserExecutor.Cdp;

// M271-M273 - PaddleOCR extra-class root cause + experimental decoder policy harness.
//
// Root cause: the observed "dictionary + blank + 1" class count on PP-OCRv4 (95 -> 97) and PP-OCRv5
// (436 -> 438) is the space character appended by PaddleOCR `use_space_char: true`. Official source:
// PaddleOCR ppocr/postprocess/rec_postprocess.py BaseRecLabelDecode.__init__ appends " " when
// use_space_char is true; CTCLabelDecode.add_special_char prepends 'blank' at index 0. Final charset is
// ['blank'] + dictionary + [' '] => N + 2 classes, with the space token at the last index (N + 1).
//
// This service exposes:
//   * the resolved charset layout/root cause (evidence-approved),
//   * a deterministic, hypothesis-only CTC greedy decode harness comparing extra-class policies,
//   * a decision report that approves the space-token *class policy* without enabling productive OCR.
//
// Safety: the experimental decode is no-authority, never persists raw input, never promotes readiness,
// never claims decode success, and never enables productive/shadow OCR.
public sealed class NodalOsPaddleOcrSpaceTokenDecoderService
{
    public const string SpaceTokenEvidenceSource =
        "PaddleOCR ppocr/postprocess/rec_postprocess.py BaseRecLabelDecode.__init__ appends \" \" when " +
        "use_space_char=true (after reading the dictionary, before add_special_char); " +
        "CTCLabelDecode.add_special_char prepends 'blank' at index 0; " +
        "configs/rec/PP-OCRv5/PP-OCRv5_*_rec.yml set use_space_char: true.";

    public NodalOsPaddleOcrCharsetLayout BuildCharsetLayout(string modelFamily, int dictionaryTokenCount)
    {
        // blank at 0, dictionary at 1..N, space at N+1, total N+2.
        var spaceIndex = dictionaryTokenCount + 1;
        return new NodalOsPaddleOcrCharsetLayout(
            $"paddle-charset-layout-{modelFamily}-{Guid.NewGuid():N}",
            modelFamily,
            BlankIndex: 0,
            DictionaryStartIndex: 1,
            DictionaryEndIndexInclusive: dictionaryTokenCount,
            SpaceIndex: spaceIndex,
            DictionaryTokenCount: dictionaryTokenCount,
            TotalClassCount: dictionaryTokenCount + 2,
            UseSpaceChar: true,
            Formula: "blank(0) + dictionary(1.." + dictionaryTokenCount + ") + space(" + spaceIndex + ") = "
                     + (dictionaryTokenCount + 2) + " classes",
            EvidenceSource: SpaceTokenEvidenceSource);
    }

    public NodalOsPaddleOcrSpaceTokenRootCause AuditSpaceTokenRootCause()
    {
        var v4 = BuildCharsetLayout(
            "PP-OCRv4-en",
            NodalOsOcrDictionaryCompatibilityService.OfficialEnglishDictionaryTokenCount);
        var v5 = BuildCharsetLayout(
            "PP-OCRv5-en",
            NodalOsOcrDictionaryCompatibilityService.PaddleOcrV5EnglishDictionaryTokenCount);

        var v4Matches = v4.TotalClassCount
                        == NodalOsOcrDictionaryCompatibilityService.PaddleOcrV4EnglishRecognizerClassCount;
        var v5Matches = v5.TotalClassCount
                        == NodalOsOcrDictionaryCompatibilityService.PaddleOcrV5EnglishObservedRecognizerClassCount;

        var official = new[]
        {
            SpaceTokenEvidenceSource,
            "PP-OCRv4-en: dictionary 95 + blank + space = 97 == observed recognizer class count 97.",
            "PP-OCRv5-en: dictionary 436 + blank + space = 438 == observed recognizer class count 438.",
            "The identical +1 across both families is uniquely explained by use_space_char (exactly one " +
            "appended token), not by EOS/SOS, unknown, padding, parser loss, or an export artifact."
        };

        var behavioral = new[]
        {
            "M262-M264 out-of-process argmax probe: the extra class (index 437) NEVER wins argmax on " +
            "single-token, space-free fixtures - consistent with a space character that has nothing to " +
            "separate.",
            "On the numeric fixture '12345' the extra class reaches max probability 0.2835 while blank wins " +
            "all 40 timesteps - i.e. space is the runner-up in padding/separator columns, exactly the " +
            "behaviour of a real space token competing with the CTC blank.",
            "On blank-heavy extreme fixtures (Black/White/Noise/Gradient) the extra class carries small but " +
            "non-zero probability and still never wins - consistent with space, inconsistent with dead " +
            "padding (which would be ~0) or unknown (which would spike on OutOfDictionary, but it does not)."
        };

        return new NodalOsPaddleOcrSpaceTokenRootCause(
            $"paddle-space-token-root-cause-{Guid.NewGuid():N}",
            NodalOsPaddleOcrSpaceTokenRootCauseStatus.SpaceTokenFromUseSpaceCharConfirmed,
            v4,
            v5,
            AppliesToBothFamilies: v4Matches && v5Matches,
            OutputIsSoftmaxProbabilities: true,
            OutputAxisOrder: "[B,T,C] with B=1, T=timesteps, C=class dimension (last axis); output node is a " +
                             "softmax (softmax_x.tmp_0) so the decoder must not re-apply softmax for confidence",
            ExtraClassResolved: v4Matches && v5Matches,
            ExtraClassMeaning: "space character (\" \") appended by use_space_char=true at the last index (N+1)",
            official,
            behavioral,
            NoAuthority: true,
            BrowserCredentialRedactor.Redact(
                "extra class is the use_space_char space token at index N+1; formula dictionary+blank+space " +
                "matches both PP-OCRv4 (97) and PP-OCRv5 (438)"));
    }

    // Deterministic greedy CTC decode under a chosen extra-class policy.
    // `probabilities` is a row-major [Timesteps, ClassCount] softmax matrix.
    // `charset` maps class index -> token; index 0 is the CTC blank sentinel, the last index is the space.
    public NodalOsPaddleOcrDecodePolicyExperiment DecodeWithPolicy(
        string fixtureId,
        float[] probabilities,
        int classCount,
        NodalOsPaddleOcrExtraClassDecodePolicyKind policyKind,
        IReadOnlyList<string> charset)
    {
        ArgumentNullException.ThrowIfNull(probabilities);
        ArgumentNullException.ThrowIfNull(charset);
        if (classCount <= 0)
            throw new ArgumentOutOfRangeException(nameof(classCount));

        var timesteps = probabilities.Length / classCount;
        var blankIndex = 0;
        var spaceIndex = classCount - 1;

        var evidenceApproved = policyKind == NodalOsPaddleOcrExtraClassDecodePolicyKind.OfficialSpaceToken;
        var hypothesisOnly = policyKind is NodalOsPaddleOcrExtraClassDecodePolicyKind.HypothesisIgnoreExtraClass
            or NodalOsPaddleOcrExtraClassDecodePolicyKind.HypothesisUnknownToken
            or NodalOsPaddleOcrExtraClassDecodePolicyKind.HypothesisPaddingToken;

        var topK = BuildTopK(probabilities, classCount, timesteps, spaceIndex);

        // The official blank-only policy under-counts by one class and cannot map the model output safely.
        if (policyKind == NodalOsPaddleOcrExtraClassDecodePolicyKind.BlankOnlyClassCountMismatch)
        {
            return new NodalOsPaddleOcrDecodePolicyExperiment(
                $"paddle-decode-experiment-{Guid.NewGuid():N}",
                policyKind,
                EvidenceApproved: true,
                HypothesisOnly: false,
                fixtureId,
                timesteps,
                classCount,
                DecodeAttempted: false,
                DecodedText: string.Empty,
                MeanConfidence: 0d,
                SpaceTokenEmissions: 0,
                ClassCountMismatch: true,
                topK,
                ReadinessPromoted: false,
                ProductiveOcr: false,
                ShadowMode: false,
                NoRawPersistence: true,
                NoSensitive: true,
                NoAuthority: true,
                BrowserCredentialRedactor.Redact(
                    "blank-only policy expects dictionary+blank classes but the model exposes one more " +
                    "(the use_space_char space); decode not attempted"));
        }

        var builder = new System.Text.StringBuilder();
        var confidenceSum = 0d;
        var emitted = 0;
        var spaceEmissions = 0;
        var previousIndex = -1;

        for (var t = 0; t < timesteps; t++)
        {
            var offset = t * classCount;
            var argmax = 0;
            var argmaxProbability = double.NegativeInfinity;
            for (var i = 0; i < classCount; i++)
            {
                double probability = probabilities[offset + i];
                if (probability > argmaxProbability)
                {
                    argmaxProbability = probability;
                    argmax = i;
                }
            }

            var collapsed = argmax == previousIndex;
            previousIndex = argmax;
            if (collapsed || argmax == blankIndex)
                continue;

            var token = MapIndexToToken(argmax, spaceIndex, policyKind, charset);
            if (token is null)
                continue; // dropped (ignore/padding policy)

            builder.Append(token);
            confidenceSum += argmaxProbability;
            emitted++;
            if (argmax == spaceIndex)
                spaceEmissions++;
        }

        var meanConfidence = emitted == 0 ? 0d : confidenceSum / emitted;

        return new NodalOsPaddleOcrDecodePolicyExperiment(
            $"paddle-decode-experiment-{Guid.NewGuid():N}",
            policyKind,
            evidenceApproved,
            hypothesisOnly,
            fixtureId,
            timesteps,
            classCount,
            DecodeAttempted: true,
            DecodedText: builder.ToString(),
            MeanConfidence: meanConfidence,
            SpaceTokenEmissions: spaceEmissions,
            ClassCountMismatch: false,
            topK,
            ReadinessPromoted: false,
            ProductiveOcr: false,
            ShadowMode: false,
            NoRawPersistence: true,
            NoSensitive: true,
            NoAuthority: true,
            BrowserCredentialRedactor.Redact(
                $"hypothesis-only experimental greedy CTC decode under {policyKind}; no authority, no " +
                "readiness promotion, decode success not claimed"));
    }

    public NodalOsPaddleOcrSpaceTokenDecisionReport DecideSpaceTokenPolicy(
        NodalOsPaddleOcrSpaceTokenRootCause rootCause,
        IReadOnlyList<NodalOsPaddleOcrDecodePolicyExperiment> experiments)
    {
        ArgumentNullException.ThrowIfNull(rootCause);
        ArgumentNullException.ThrowIfNull(experiments);

        var resolved = rootCause.ExtraClassResolved
                       && rootCause.Status
                       == NodalOsPaddleOcrSpaceTokenRootCauseStatus.SpaceTokenFromUseSpaceCharConfirmed;

        // The space-token *class policy* is officially evidenced, so the extra-class semantics gate is
        // resolved. Productive decode stays blocked by the project-wide no-productive-OCR posture; this
        // gate only approves the mapping, never decode success.
        var decision = resolved
            ? NodalOsPaddleOcrExtraClassDecision.ReadyForApprovedDecodePolicy
            : NodalOsPaddleOcrExtraClassDecision.BlockedByExtraClassSemantics;

        var noAuthority = rootCause.NoAuthority && experiments.All(e => e.NoAuthority);

        return new NodalOsPaddleOcrSpaceTokenDecisionReport(
            $"paddle-space-token-decision-{Guid.NewGuid():N}",
            rootCause,
            experiments,
            decision,
            ExtraClassSemanticsResolved: resolved,
            ApprovedPolicyIsSpaceToken: resolved,
            DecodeSuccessClaimed: false,
            ProductiveOcrBlocked: true,
            ShadowModeBlocked: true,
            NoRawPersistence: true,
            NoFullScreen: true,
            NoSensitive: true,
            NoSaas: true,
            NoAuthority: noAuthority,
            BrowserCredentialRedactor.Redact(
                $"{decision}; extra class resolved as use_space_char space token; space-token class policy " +
                "approved by official evidence; productive OCR and decode success remain blocked"));
    }

    private static string? MapIndexToToken(
        int index,
        int spaceIndex,
        NodalOsPaddleOcrExtraClassDecodePolicyKind policyKind,
        IReadOnlyList<string> charset)
    {
        if (index != spaceIndex)
            return index >= 0 && index < charset.Count ? charset[index] : null;

        return policyKind switch
        {
            // Official: the extra class is a space character.
            NodalOsPaddleOcrExtraClassDecodePolicyKind.OfficialSpaceToken => " ",
            // Hypothesis: silently drop the extra class (previously blocked - loses real spaces).
            NodalOsPaddleOcrExtraClassDecodePolicyKind.HypothesisIgnoreExtraClass => null,
            // Hypothesis: treat as unknown replacement char.
            NodalOsPaddleOcrExtraClassDecodePolicyKind.HypothesisUnknownToken => "�",
            // Hypothesis: treat as padding -> dropped.
            NodalOsPaddleOcrExtraClassDecodePolicyKind.HypothesisPaddingToken => null,
            _ => null
        };
    }

    private static IReadOnlyList<NodalOsPaddleOcrTimestepTopK> BuildTopK(
        float[] probabilities,
        int classCount,
        int timesteps,
        int spaceIndex)
    {
        var result = new List<NodalOsPaddleOcrTimestepTopK>(timesteps);
        for (var t = 0; t < timesteps; t++)
        {
            var offset = t * classCount;
            var topIndex = 0;
            var topProbability = double.NegativeInfinity;
            var secondIndex = -1;
            var secondProbability = double.NegativeInfinity;

            for (var i = 0; i < classCount; i++)
            {
                double probability = probabilities[offset + i];
                if (probability > topProbability)
                {
                    secondIndex = topIndex;
                    secondProbability = topProbability;
                    topIndex = i;
                    topProbability = probability;
                }
                else if (probability > secondProbability)
                {
                    secondIndex = i;
                    secondProbability = probability;
                }
            }

            var spaceProbability = spaceIndex >= 0 && spaceIndex < classCount
                ? probabilities[offset + spaceIndex]
                : 0d;
            var spaceIsTopTwo = topIndex == spaceIndex || secondIndex == spaceIndex;

            result.Add(new NodalOsPaddleOcrTimestepTopK(
                t,
                topIndex,
                topProbability,
                secondIndex,
                secondProbability < 0 ? 0d : secondProbability,
                spaceProbability,
                spaceIsTopTwo));
        }

        return result;
    }
}
