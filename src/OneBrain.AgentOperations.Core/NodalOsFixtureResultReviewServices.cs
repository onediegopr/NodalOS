using System.Text.Json;
using System.Text.Json.Serialization;
using OneBrain.AgentOperations.Contracts;

namespace OneBrain.AgentOperations.Core;

public sealed class NodalOsFixtureResultReviewService
{
    public NodalOsFixtureResultReview CreateReview(
        NodalOsSyntheticDryRunSimulationResult result,
        NodalOsScanFixtureMatrix matrix)
    {
        var reviewed = matrix.Fixtures.Select(f =>
        {
            var decision = result.PolicyDecisions.Single(d => d.FixtureRef == f.FixtureId);
            var matches = f.ExpectedOutcome.Disposition == decision.SimulatedDisposition;
            return new NodalOsReviewedFixtureResult
            {
                FixtureId = f.FixtureId,
                Category = f.Category,
                ExpectedOutcome = f.ExpectedOutcome.Disposition,
                SimulatedOutcome = decision.SimulatedDisposition,
                MatchesExpectation = matches,
                ReviewStatus = matches ? NodalOsFixtureResultReviewStatus.AcceptedSynthetic : NodalOsFixtureResultReviewStatus.Mismatch,
                ReasonRedacted = "Synthetic result compared against declared expected outcome.",
                RedactionStatusRedacted = f.ExpectedOutcome.RequiresRedaction ? "Redaction required." : "No redaction required for synthetic metadata.",
                NeverSentToLlm = true,
                NeverSentToCloud = true
            };
        }).ToArray();

        return new()
        {
            ReviewId = "fixture-result-review-m550",
            SimulatorResultRef = result.ResultId,
            FixtureMatrixRef = matrix.MatrixId,
            ReviewedFixtureResults = reviewed,
            OpenQuestionsRedacted = ["Which fixtures should be expanded before any future operational pass?"],
            MismatchesRedacted = reviewed.Where(r => !r.MatchesExpectation).Select(r => r.FixtureId).ToArray(),
            RisksRedacted = ["Synthetic review does not prove operational filesystem safety."],
            RequiredPolicyUpdatesRedacted = ["Future audit must confirm policy behavior outside synthetic metadata."],
            RequiredFixtureUpdatesRedacted = ["Add more synthetic edge cases before operational access."],
            IsReviewOnly = true,
            IsNoOp = true,
            CanAuthorizeRealScan = false,
            CanAuthorizeFilesystemAccess = false,
            CanAuthorizeIndexing = false,
            CanAuthorizeLlmContext = false
        };
    }

    public NodalOsFixtureResultReviewActionResult ApplyOption(NodalOsFixtureResultReviewOption option) =>
        new()
        {
            ActionResultId = $"fixture-review-action-{option}",
            Option = option,
            IsNoOp = true,
            MutatesState = false,
            AuthorizesRealScan = false,
            AuthorizesFilesystemAccess = false,
            AuthorizesIndexing = false,
            AuthorizesLlmContext = false,
            UserFacingExplanationRedacted = "Fixture review option is no-op and cannot authorize operational behavior."
        };

    public NodalOsFixtureResultReviewReadiness Evaluate(NodalOsFixtureResultReview review) =>
        new()
        {
            ReadinessId = "fixture-result-review-readiness-m550",
            ReviewRef = review.ReviewId,
            ReadyForSyntheticFixtureReview = true,
            ReadyForRealScan = false,
            ReadyForFilesystemAccess = false,
            ReadyForLlmContext = false
        };
}

public sealed class NodalOsFixtureResultReviewJsonSerializer
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
        WriteIndented = true
    };

    public string SerializeReview(NodalOsFixtureResultReview review) => JsonSerializer.Serialize(review, Options);
    public string SerializeAction(NodalOsFixtureResultReviewActionResult result) => JsonSerializer.Serialize(result, Options);
    public string SerializeReadiness(NodalOsFixtureResultReviewReadiness readiness) => JsonSerializer.Serialize(readiness, Options);
}
