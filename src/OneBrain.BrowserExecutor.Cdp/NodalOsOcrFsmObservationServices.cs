using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.BrowserExecutor.Cdp;

public sealed class NodalOsOcrFsmObservationConsumer
{
    public NodalOsOcrFsmObservationResult Consume(NodalOsOcrFsmObservationInput input)
    {
        var observationContext = new List<NodalOsOcrFsmObservationRanking>();
        var diagnostics = new List<NodalOsOcrFsmObservationRanking>();
        var excluded = new List<NodalOsOcrFsmObservationRanking>();

        foreach (var evaluation in input.EvidenceEvaluations)
        {
            var ranking = BuildRanking(evaluation);
            switch (ranking.Disposition)
            {
                case NodalOsOcrFsmObservationDisposition.ObservationContext:
                    observationContext.Add(ranking);
                    break;
                case NodalOsOcrFsmObservationDisposition.DiagnosticOnly:
                    diagnostics.Add(ranking);
                    break;
                default:
                    excluded.Add(ranking);
                    break;
            }
        }

        var orderedObservationContext = observationContext
            .OrderBy(r => r.RankOrder)
            .ThenByDescending(r => r.RankScore)
            .ToArray();
        var orderedDiagnostics = diagnostics
            .OrderBy(r => r.RankOrder)
            .ThenByDescending(r => r.RankScore)
            .ToArray();
        var orderedExcluded = excluded
            .OrderBy(r => r.RankOrder)
            .ThenByDescending(r => r.RankScore)
            .ToArray();

        return new NodalOsOcrFsmObservationResult(
            $"ocr-fsm-observation:{input.InputId}",
            orderedObservationContext,
            orderedDiagnostics,
            orderedExcluded,
            ReadOnlyObservationOnly: true,
            CanProduceActionPlan: false,
            CanProduceSafeAction: false,
            CanApproveAction: false,
            CanApproveClick: false,
            CanApproveSubmit: false,
            CanApproveSend: false,
            CanApproveDelete: false,
            CanApprovePay: false,
            CanApproveSign: false,
            ProvenancePreserved: orderedObservationContext.Concat(orderedDiagnostics).All(HasProvenance));
    }

    private static NodalOsOcrFsmObservationRanking BuildRanking(NodalOsOcrEvidencePolicyEvaluation evaluation)
    {
        if (evaluation.LedgerStatus == NodalOsOcrEvidenceLedgerStatus.RejectedPolicyViolation || evaluation.Entry is null)
        {
            return new NodalOsOcrFsmObservationRanking(
                ObservationId: evaluation.Entry?.ObservationId ?? "policy-violation",
                EvidenceId: evaluation.Entry?.EvidenceId,
                Disposition: NodalOsOcrFsmObservationDisposition.ExcludedPolicyViolation,
                LedgerStatus: evaluation.LedgerStatus,
                EvidenceUse: evaluation.Entry?.EvidenceUse,
                ConfidenceBand: evaluation.Entry?.ConfidenceBand ?? NodalOsOcrEvidenceConfidenceBand.Rejected,
                RankOrder: 99,
                RankScore: 0d,
                RegionVerified: evaluation.Entry?.RegionVerified ?? false,
                ConfidenceGatePassed: evaluation.Entry?.ConfidenceGatePassed ?? false,
                FingerprintHashMatch: evaluation.Entry?.FingerprintHashMatch ?? false,
                DiffScore: evaluation.Entry?.DiffScore,
                ExactMatch: evaluation.Entry?.ExactMatch ?? false,
                NormalizedMatch: evaluation.Entry?.NormalizedMatch ?? false,
                EditDistance: evaluation.Entry?.EditDistance,
                SourceCategory: evaluation.Entry?.SourceCategory,
                CaptureMode: evaluation.Entry?.CaptureMode,
                WindowTitleOrSource: evaluation.Entry?.WindowTitleOrSource,
                ProcessOrSource: evaluation.Entry?.ProcessOrSource,
                RegionBounds: evaluation.Entry?.RegionBounds,
                Reason: evaluation.Reason);
        }

        if (evaluation.LedgerStatus == NodalOsOcrEvidenceLedgerStatus.AcceptedAuxiliary &&
            CanEnterObservationContext(evaluation.Entry))
        {
            var score = ScoreAccepted(evaluation.Entry);
            return new NodalOsOcrFsmObservationRanking(
                evaluation.Entry.ObservationId,
                evaluation.Entry.EvidenceId,
                NodalOsOcrFsmObservationDisposition.ObservationContext,
                evaluation.LedgerStatus,
                evaluation.Entry.EvidenceUse,
                evaluation.Entry.ConfidenceBand,
                RankOrder: RankOrderForAccepted(evaluation.Entry.ConfidenceBand),
                RankScore: score,
                RegionVerified: evaluation.Entry.RegionVerified,
                ConfidenceGatePassed: evaluation.Entry.ConfidenceGatePassed,
                FingerprintHashMatch: evaluation.Entry.FingerprintHashMatch,
                DiffScore: evaluation.Entry.DiffScore,
                ExactMatch: evaluation.Entry.ExactMatch,
                NormalizedMatch: evaluation.Entry.NormalizedMatch,
                EditDistance: evaluation.Entry.EditDistance,
                SourceCategory: evaluation.Entry.SourceCategory,
                CaptureMode: evaluation.Entry.CaptureMode,
                WindowTitleOrSource: evaluation.Entry.WindowTitleOrSource,
                ProcessOrSource: evaluation.Entry.ProcessOrSource,
                RegionBounds: evaluation.Entry.RegionBounds,
                Reason: evaluation.Reason);
        }

        var diagnosticScore = ScoreDiagnostic(evaluation.Entry);
        return new NodalOsOcrFsmObservationRanking(
            evaluation.Entry.ObservationId,
            evaluation.Entry.EvidenceId,
            NodalOsOcrFsmObservationDisposition.DiagnosticOnly,
            evaluation.LedgerStatus,
            evaluation.Entry.EvidenceUse,
            evaluation.Entry.ConfidenceBand,
            RankOrder: evaluation.LedgerStatus == NodalOsOcrEvidenceLedgerStatus.RecordedDiagnosticUncertain ? 4 : 5,
            RankScore: diagnosticScore,
            RegionVerified: evaluation.Entry.RegionVerified,
            ConfidenceGatePassed: evaluation.Entry.ConfidenceGatePassed,
            FingerprintHashMatch: evaluation.Entry.FingerprintHashMatch,
            DiffScore: evaluation.Entry.DiffScore,
            ExactMatch: evaluation.Entry.ExactMatch,
            NormalizedMatch: evaluation.Entry.NormalizedMatch,
            EditDistance: evaluation.Entry.EditDistance,
            SourceCategory: evaluation.Entry.SourceCategory,
            CaptureMode: evaluation.Entry.CaptureMode,
            WindowTitleOrSource: evaluation.Entry.WindowTitleOrSource,
            ProcessOrSource: evaluation.Entry.ProcessOrSource,
            RegionBounds: evaluation.Entry.RegionBounds,
            Reason: evaluation.Reason);
    }

    private static bool CanEnterObservationContext(NodalOsOcrEvidenceLedgerEntry entry) =>
        entry.EvidenceUse == NodalOsOcrEvidenceUse.AuxiliaryOnly &&
        entry.Authority == NodalOsOcrEvidenceAuthority.NoAuthority &&
        entry.NoAuthority &&
        entry.EvidenceOnly &&
        !entry.ActionAllowed &&
        entry.RegionVerified &&
        entry.ConfidenceGatePassed &&
        entry.FingerprintHashMatch &&
        (entry.DiffScore is null || entry.DiffScore <= 0.01d) &&
        entry.AcceptanceState == NodalOsOcrObservationAcceptanceState.AcceptedEvidence;

    private static int RankOrderForAccepted(NodalOsOcrEvidenceConfidenceBand band) => band switch
    {
        NodalOsOcrEvidenceConfidenceBand.High => 1,
        NodalOsOcrEvidenceConfidenceBand.Medium => 2,
        NodalOsOcrEvidenceConfidenceBand.Low => 3,
        _ => 6
    };

    private static double ScoreAccepted(NodalOsOcrEvidenceLedgerEntry entry)
    {
        var band = entry.ConfidenceBand switch
        {
            NodalOsOcrEvidenceConfidenceBand.High => 300d,
            NodalOsOcrEvidenceConfidenceBand.Medium => 200d,
            NodalOsOcrEvidenceConfidenceBand.Low => 100d,
            _ => 0d
        };
        var confidence = entry.Confidence ?? 0d;
        var diffPenalty = entry.DiffScore ?? 0d;
        var editBonus = entry.EditDistance.HasValue ? Math.Max(0d, 10d - entry.EditDistance.Value) : 0d;
        var exactBonus = entry.ExactMatch ? 25d : entry.NormalizedMatch ? 10d : 0d;
        return band + confidence * 100d + editBonus + exactBonus - diffPenalty * 100d;
    }

    private static double ScoreDiagnostic(NodalOsOcrEvidenceLedgerEntry entry)
    {
        var baseScore = entry.AcceptanceState == NodalOsOcrObservationAcceptanceState.UncertainRequiresExpansion ? 50d : 25d;
        return baseScore + (entry.Confidence ?? 0d) * 10d;
    }

    private static bool HasProvenance(NodalOsOcrFsmObservationRanking ranking) =>
        ranking.SourceCategory is not null &&
        !string.IsNullOrWhiteSpace(ranking.CaptureMode) &&
        !string.IsNullOrWhiteSpace(ranking.WindowTitleOrSource) &&
        !string.IsNullOrWhiteSpace(ranking.ProcessOrSource) &&
        ranking.RegionBounds is not null;
}
