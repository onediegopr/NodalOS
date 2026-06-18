using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.BrowserExecutor.Cdp;

public sealed class NodalOsAssistedVerificationFixtureSet
{
    private readonly NodalOsAssistedVerificationPolicy policy = new();

    public IReadOnlyList<NodalOsAssistedVerificationFixtureCase> CreateDefaultFixtureCases()
    {
        return new[]
        {
            CreateFixture(
                "assisted-qa-pvc-wall-known-fixture-pass",
                "PVC WALL",
                "PVC WALL",
                NodalOsAssistedVerificationSignalKind.KnownQaFixtureSignal,
                NodalOsAssistedVerificationDecision.VerifiedLowRisk,
                "OCR plus known QA fixture corroboration should pass"),
            CreateFixture(
                "assisted-qa-roma-manual-pass",
                "ROMA",
                "ROMA",
                NodalOsAssistedVerificationSignalKind.ManualExpectedValueSignal,
                NodalOsAssistedVerificationDecision.VerifiedLowRisk,
                "OCR plus manual expected value corroboration should pass"),
            CreateFixture(
                "assisted-qa-pvc-wali-residual-needs-more-evidence",
                "PVC WALI",
                "PVC WALL",
                NodalOsAssistedVerificationSignalKind.KnownQaFixtureSignal,
                NodalOsAssistedVerificationDecision.NeedsMoreEvidence,
                "Minor OCR residual does not pass because current policy requires exact corroboration"),
            CreateFixture(
                "assisted-qa-roma-ocr-only-rejected",
                "ROMA",
                null,
                null,
                NodalOsAssistedVerificationDecision.RejectedOcrOnly,
                "OCR-only exact match must still fail"),
            CreateFixture(
                "assisted-qa-roma-mismatch-needs-more-evidence",
                "ROMA",
                "GENOVA",
                NodalOsAssistedVerificationSignalKind.KnownQaFixtureSignal,
                NodalOsAssistedVerificationDecision.NeedsMoreEvidence,
                "OCR and non-OCR mismatch must not verify"),
            CreateRejectedOcrFixture(),
            CreateUncertainOcrFixture(),
            CreateSensitiveFixture(),
            CreateFullScreenFixture(),
            CreateActionRequestFixture()
        };
    }

    public NodalOsAssistedVerificationFixtureExecutionSummary Execute(IReadOnlyList<NodalOsAssistedVerificationFixtureCase> fixtures)
    {
        var results = fixtures.Select(ExecuteFixture).ToArray();
        var unexpectedPasses = results.Count(r => !r.MatchedExpectation && r.Decision == NodalOsAssistedVerificationDecision.VerifiedLowRisk);
        var unexpectedFailures = results.Count(r => !r.MatchedExpectation && r.Decision != NodalOsAssistedVerificationDecision.VerifiedLowRisk);
        var passingFixturesPassed = results.Count(r =>
            r.ExpectedDecision == NodalOsAssistedVerificationDecision.VerifiedLowRisk &&
            r.Decision == NodalOsAssistedVerificationDecision.VerifiedLowRisk);
        var failingFixturesRejected = results.Count(r =>
            r.ExpectedDecision != NodalOsAssistedVerificationDecision.VerifiedLowRisk &&
            r.MatchedExpectation);

        return new NodalOsAssistedVerificationFixtureExecutionSummary(
            SummaryId: "assisted-verification-fixtures:m339",
            Fixtures: results,
            FixturesTotal: results.Length,
            PassingFixturesPassed: passingFixturesPassed,
            FailingFixturesRejected: failingFixturesRejected,
            UnexpectedPasses: unexpectedPasses,
            UnexpectedFailures: unexpectedFailures);
    }

    private NodalOsAssistedVerificationFixtureExecutionResult ExecuteFixture(NodalOsAssistedVerificationFixtureCase fixture)
    {
        var result = policy.Evaluate(fixture.Request);
        var ocrSignal = fixture.Request.Signals.FirstOrDefault(s => s.Kind == NodalOsAssistedVerificationSignalKind.OcrAuxiliaryEvidence ||
                                                                    s.Kind == NodalOsAssistedVerificationSignalKind.DiagnosticOnly ||
                                                                    s.Kind == NodalOsAssistedVerificationSignalKind.Rejected);
        var nonOcrSignal = fixture.Request.Signals.FirstOrDefault(s => s.Kind is not NodalOsAssistedVerificationSignalKind.OcrAuxiliaryEvidence and
                                                                           not NodalOsAssistedVerificationSignalKind.DiagnosticOnly and
                                                                           not NodalOsAssistedVerificationSignalKind.Rejected);
        var warnings = BuildWarnings(result, ocrSignal, nonOcrSignal);

        return new NodalOsAssistedVerificationFixtureExecutionResult(
            FixtureId: fixture.FixtureId,
            RiskLevel: fixture.Request.RiskLevel,
            OcrSignalState: ocrSignal is null
                ? null
                : ocrSignal.Kind == NodalOsAssistedVerificationSignalKind.OcrAuxiliaryEvidence ? "AcceptedAuxiliary"
                : ocrSignal.Kind == NodalOsAssistedVerificationSignalKind.DiagnosticOnly ? "Uncertain"
                : "Rejected",
            OcrRecognizedText: ocrSignal?.ObservedText,
            NonOcrSignalKind: nonOcrSignal?.Kind,
            NonOcrExpectedValue: nonOcrSignal?.ExpectedText,
            CorroborationSatisfied: result.NonOcrCorroborationSatisfied,
            Decision: result.Decision,
            ExpectedDecision: fixture.ExpectedDecision,
            MatchedExpectation: result.Decision == fixture.ExpectedDecision,
            Reason: result.Reason,
            Warnings: warnings,
            ActionsAllowed: result.ActionsAllowed,
            CanProduceActionPlan: result.CanProduceActionPlan,
            CanProduceSafeAction: result.CanProduceSafeAction,
            CanApproveClick: result.CanApproveClick,
            CanApproveSubmit: result.CanApproveSubmit,
            CanApproveSend: result.CanApproveSend,
            CanApproveDelete: result.CanApproveDelete,
            CanApprovePay: result.CanApprovePay,
            CanApproveSign: result.CanApproveSign,
            NoAuthority: result.NoAuthority,
            EvidenceOnly: result.EvidenceOnly);
    }

    private static IReadOnlyList<string> BuildWarnings(
        NodalOsAssistedVerificationResult result,
        NodalOsAssistedVerificationSignal? ocrSignal,
        NodalOsAssistedVerificationSignal? nonOcrSignal)
    {
        var warnings = new List<string>();

        if (ocrSignal is not null &&
            nonOcrSignal is not null &&
            !result.NonOcrCorroborationSatisfied &&
            !string.IsNullOrWhiteSpace(ocrSignal.ObservedText) &&
            !string.IsNullOrWhiteSpace(nonOcrSignal.ExpectedText))
        {
            warnings.Add("OCR and non-OCR values did not corroborate");
        }

        if (result.Decision == NodalOsAssistedVerificationDecision.RejectedOcrOnly)
            warnings.Add("Non-OCR corroboration is mandatory");

        return warnings;
    }

    private static NodalOsAssistedVerificationFixtureCase CreateFixture(
        string fixtureId,
        string ocrText,
        string? nonOcrExpected,
        NodalOsAssistedVerificationSignalKind? nonOcrKind,
        NodalOsAssistedVerificationDecision expectedDecision,
        string description)
    {
        var signals = new List<NodalOsAssistedVerificationSignal>
        {
            CreateAcceptedOcrSignal(fixtureId, ocrText, expectedText: ocrText)
        };

        if (nonOcrExpected is not null && nonOcrKind is not null)
        {
            signals.Add(CreateNonOcrSignal(
                $"{fixtureId}:non-ocr",
                nonOcrKind.Value,
                nonOcrExpected));
        }

        return new NodalOsAssistedVerificationFixtureCase(
            fixtureId,
            CreateRequest(fixtureId, signals),
            expectedDecision,
            description);
    }

    private static NodalOsAssistedVerificationFixtureCase CreateRejectedOcrFixture() =>
        new(
            "assisted-qa-rejected-ocr-not-verified",
            CreateRequest("assisted-qa-rejected-ocr-not-verified", new[]
            {
                CreateDiagnosticSignal(
                    "assisted-qa-rejected-ocr-not-verified:ocr",
                    NodalOsAssistedVerificationSignalKind.Rejected,
                    "ROMA",
                    rejected: true)
            }),
            NodalOsAssistedVerificationDecision.NotVerified,
            "Rejected OCR cannot support verification");

    private static NodalOsAssistedVerificationFixtureCase CreateUncertainOcrFixture() =>
        new(
            "assisted-qa-uncertain-ocr-needs-more-evidence",
            CreateRequest("assisted-qa-uncertain-ocr-needs-more-evidence", new[]
            {
                CreateDiagnosticSignal(
                    "assisted-qa-uncertain-ocr-needs-more-evidence:ocr",
                    NodalOsAssistedVerificationSignalKind.DiagnosticOnly,
                    "ROMA",
                    rejected: false)
            }),
            NodalOsAssistedVerificationDecision.NeedsMoreEvidence,
            "Uncertain OCR cannot support verification");

    private static NodalOsAssistedVerificationFixtureCase CreateSensitiveFixture() =>
        new(
            "assisted-qa-sensitive-rejected",
            CreateRequest(
                "assisted-qa-sensitive-rejected",
                new[]
                {
                    CreateAcceptedOcrSignal("assisted-qa-sensitive-rejected:ocr", "ROMA", "ROMA"),
                    CreateNonOcrSignal("assisted-qa-sensitive-rejected:manual", NodalOsAssistedVerificationSignalKind.ManualExpectedValueSignal, "ROMA")
                },
                containsSensitiveData: true),
            NodalOsAssistedVerificationDecision.RejectedSensitive,
            "Sensitive requests are rejected");

    private static NodalOsAssistedVerificationFixtureCase CreateFullScreenFixture() =>
        new(
            "assisted-qa-fullscreen-rejected",
            CreateRequest(
                "assisted-qa-fullscreen-rejected",
                new[]
                {
                    CreateAcceptedOcrSignal("assisted-qa-fullscreen-rejected:ocr", "ROMA", "ROMA"),
                    CreateNonOcrSignal("assisted-qa-fullscreen-rejected:manual", NodalOsAssistedVerificationSignalKind.KnownQaFixtureSignal, "ROMA")
                },
                fullScreen: true),
            NodalOsAssistedVerificationDecision.RejectedFullScreen,
            "Full-screen requests are rejected");

    private static NodalOsAssistedVerificationFixtureCase CreateActionRequestFixture() =>
        new(
            "assisted-qa-action-request-rejected",
            CreateRequest(
                "assisted-qa-action-request-rejected",
                new[]
                {
                    CreateAcceptedOcrSignal("assisted-qa-action-request-rejected:ocr", "ROMA", "ROMA"),
                    CreateNonOcrSignal("assisted-qa-action-request-rejected:manual", NodalOsAssistedVerificationSignalKind.FsmStateSignal, "ROMA")
                },
                actionRequested: true),
            NodalOsAssistedVerificationDecision.RejectedActionRequest,
            "Action requests are rejected");

    private static NodalOsAssistedVerificationRequest CreateRequest(
        string requestId,
        IReadOnlyList<NodalOsAssistedVerificationSignal> signals,
        bool containsSensitiveData = false,
        bool containsDocumentData = false,
        bool containsCredentials = false,
        bool fullScreen = false,
        bool actionRequested = false)
    {
        return new NodalOsAssistedVerificationRequest(
            RequestId: requestId,
            RiskLevel: NodalOsAssistedVerificationRiskLevel.Low,
            LowRiskOnly: true,
            ActionRequested: actionRequested,
            ApprovalRequested: false,
            ContainsSensitiveData: containsSensitiveData,
            ContainsDocumentData: containsDocumentData,
            ContainsCredentials: containsCredentials,
            FullScreen: fullScreen,
            Signals: signals,
            Reason: "controlled-assisted-verification-fixture");
    }

    private static NodalOsAssistedVerificationSignal CreateAcceptedOcrSignal(
        string signalId,
        string observedText,
        string expectedText) =>
        new(
            SignalId: signalId,
            Kind: NodalOsAssistedVerificationSignalKind.OcrAuxiliaryEvidence,
            SupportsVerification: true,
            DiagnosticOnly: false,
            Rejected: false,
            Source: "FixtureOcrAcceptedAuxiliary",
            ExpectedText: expectedText,
            ObservedText: observedText,
            NormalizedText: observedText,
            ExactMatch: string.Equals(observedText, expectedText, StringComparison.Ordinal),
            NormalizedMatch: false,
            EditDistance: ComputeEditDistance(observedText, expectedText),
            ConfidenceBand: NodalOsOcrEvidenceConfidenceBand.High,
            RegionVerified: true,
            ConfidenceGatePassed: true,
            FingerprintHashMatch: true,
            DiffScore: 0d,
            NoAuthority: true,
            EvidenceOnly: true,
            ActionAllowed: false,
            Reason: "fixture accepted OCR auxiliary evidence",
            SourceCategory: NodalOsOcrObservationSource.RealQaWindowRegion,
            CaptureMode: "real-qa-window-region",
            WindowTitleOrSource: "NODAL OS OCR QA Window",
            ProcessOrSource: "OneBrain.Tools.QaWindowHost",
            RegionBounds: new NodalOsScreenRegionBounds(70, 54, 660, 180));

    private static NodalOsAssistedVerificationSignal CreateDiagnosticSignal(
        string signalId,
        NodalOsAssistedVerificationSignalKind kind,
        string observedText,
        bool rejected) =>
        new(
            SignalId: signalId,
            Kind: kind,
            SupportsVerification: false,
            DiagnosticOnly: !rejected,
            Rejected: rejected,
            Source: rejected ? "FixtureOcrRejected" : "FixtureOcrUncertain",
            ExpectedText: observedText,
            ObservedText: observedText,
            NormalizedText: observedText,
            ExactMatch: true,
            NormalizedMatch: true,
            EditDistance: 0,
            ConfidenceBand: rejected ? NodalOsOcrEvidenceConfidenceBand.Rejected : NodalOsOcrEvidenceConfidenceBand.Low,
            RegionVerified: !rejected,
            ConfidenceGatePassed: false,
            FingerprintHashMatch: !rejected,
            DiffScore: rejected ? 1d : 0.2d,
            NoAuthority: true,
            EvidenceOnly: true,
            ActionAllowed: false,
            Reason: rejected ? "fixture rejected OCR evidence" : "fixture uncertain OCR evidence",
            SourceCategory: NodalOsOcrObservationSource.RealQaWindowRegion,
            CaptureMode: "real-qa-window-region",
            WindowTitleOrSource: "NODAL OS OCR QA Window",
            ProcessOrSource: "OneBrain.Tools.QaWindowHost",
            RegionBounds: new NodalOsScreenRegionBounds(70, 54, 660, 180));

    private static NodalOsAssistedVerificationSignal CreateNonOcrSignal(
        string signalId,
        NodalOsAssistedVerificationSignalKind kind,
        string expectedText) =>
        new(
            SignalId: signalId,
            Kind: kind,
            SupportsVerification: true,
            DiagnosticOnly: false,
            Rejected: false,
            Source: "ControlledQaNonOcrSignal",
            ExpectedText: expectedText,
            ObservedText: expectedText,
            NormalizedText: expectedText,
            ExactMatch: true,
            NormalizedMatch: true,
            EditDistance: 0,
            ConfidenceBand: NodalOsOcrEvidenceConfidenceBand.High,
            RegionVerified: true,
            ConfidenceGatePassed: true,
            FingerprintHashMatch: true,
            DiffScore: 0d,
            NoAuthority: true,
            EvidenceOnly: true,
            ActionAllowed: false,
            Reason: "fixture non-OCR corroboration",
            SourceCategory: null,
            CaptureMode: null,
            WindowTitleOrSource: null,
            ProcessOrSource: null,
            RegionBounds: null);

    private static int ComputeEditDistance(string left, string right)
    {
        var d = new int[left.Length + 1, right.Length + 1];
        for (var i = 0; i <= left.Length; i++)
            d[i, 0] = i;
        for (var j = 0; j <= right.Length; j++)
            d[0, j] = j;

        for (var i = 1; i <= left.Length; i++)
        {
            for (var j = 1; j <= right.Length; j++)
            {
                var cost = left[i - 1] == right[j - 1] ? 0 : 1;
                d[i, j] = Math.Min(
                    Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                    d[i - 1, j - 1] + cost);
            }
        }

        return d[left.Length, right.Length];
    }
}
