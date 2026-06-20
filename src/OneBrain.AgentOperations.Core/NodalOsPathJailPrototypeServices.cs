using System.Text.Json;
using System.Text.Json.Serialization;
using OneBrain.AgentOperations.Contracts;

namespace OneBrain.AgentOperations.Core;

public sealed class NodalOsPathJailPrototypeService
{
    public NodalOsPathJailPrototypeContract CreatePrototype(
        string workspaceRef = "workspace-ref-m547",
        string rootPathRef = "synthetic-root-ref-m547") =>
        new()
        {
            PrototypeId = "path-jail-prototype-contract-m547",
            WorkspaceRef = workspaceRef,
            RootPathRef = rootPathRef,
            SyntheticRootOnly = true,
            UsesRealFilesystem = false,
            PerformsRealCanonicalization = false,
            PerformsDirectoryListing = false,
            PerformsFileRead = false,
            PerformsFileHash = false,
            CanMutateFilesystem = false,
            CanAuthorizeScan = false,
            IsPrototypeOnly = true
        };

    public IReadOnlyList<NodalOsPathJailCandidatePreview> CreateCandidates() =>
        [
            Candidate("candidate-root", "synthetic://workspace", NodalOsPathJailPrototypePathKind.SyntheticRoot, true, false, false, false, false, false, NodalOsPathJailPrototypeExpectedDecision.AllowedForFuturePreview),
            Candidate("candidate-source", "synthetic://workspace/src/app.cs", NodalOsPathJailPrototypePathKind.SyntheticSource, true, false, false, false, false, false, NodalOsPathJailPrototypeExpectedDecision.AllowedForFuturePreview),
            Candidate("candidate-dependency", "synthetic://workspace/vendor/pkg", NodalOsPathJailPrototypePathKind.SyntheticDependencyFolder, true, false, false, false, false, false, NodalOsPathJailPrototypeExpectedDecision.RequiresReview),
            Candidate("candidate-generated", "synthetic://workspace/build/output", NodalOsPathJailPrototypePathKind.SyntheticGeneratedOutput, true, false, false, false, false, false, NodalOsPathJailPrototypeExpectedDecision.RequiresReview),
            Candidate("candidate-hidden", "synthetic://workspace/.hidden", NodalOsPathJailPrototypePathKind.SyntheticHiddenItem, true, false, false, false, false, false, NodalOsPathJailPrototypeExpectedDecision.RequiresReview),
            Candidate("candidate-env", "synthetic://workspace/env-marker", NodalOsPathJailPrototypePathKind.SyntheticEnvironmentMarker, true, false, false, false, false, false, NodalOsPathJailPrototypeExpectedDecision.RequiresReview),
            Candidate("candidate-media", "synthetic://workspace/assets/media", NodalOsPathJailPrototypePathKind.SyntheticBinaryMedia, true, false, false, false, false, false, NodalOsPathJailPrototypeExpectedDecision.RequiresReview),
            Candidate("candidate-link", "synthetic://workspace/link-like", NodalOsPathJailPrototypePathKind.SyntheticSymlinkLike, true, false, true, false, false, false, NodalOsPathJailPrototypeExpectedDecision.BlockedSymlinkLike),
            Candidate("candidate-outside", "synthetic://outside/workspace", NodalOsPathJailPrototypePathKind.SyntheticOutsideJail, false, true, false, false, false, false, NodalOsPathJailPrototypeExpectedDecision.BlockedOutsideJail),
            Candidate("candidate-case", "synthetic://workspace/SRC/App.cs", NodalOsPathJailPrototypePathKind.SyntheticCaseVariant, true, false, false, true, false, false, NodalOsPathJailPrototypeExpectedDecision.RequiresReview),
            Candidate("candidate-deep", "synthetic://workspace/deep/tree/ref", NodalOsPathJailPrototypePathKind.SyntheticDeepTree, true, false, false, false, false, false, NodalOsPathJailPrototypeExpectedDecision.RequiresReview)
        ];

    public NodalOsPathJailPolicyDecisionPreview Decide(NodalOsPathJailCandidatePreview candidate) =>
        new()
        {
            DecisionPreviewId = $"policy-preview-{candidate.CandidateId}",
            CandidateRef = candidate.CandidateId,
            AllowedForFutureScanPreview = candidate.ExpectedPolicyDecision == NodalOsPathJailPrototypeExpectedDecision.AllowedForFuturePreview,
            BlockedReasonRedacted = candidate.ExpectedPolicyDecision.ToString(),
            RequiresUserReview = candidate.ExpectedPolicyDecision != NodalOsPathJailPrototypeExpectedDecision.AllowedForFuturePreview,
            RequiresAudit = true,
            EvidenceRefs = ["evidence-path-jail-prototype-ref-only"],
            TimelineRefs = ["timeline-path-jail-prototype-m547"],
            GuardrailRefs = ["guardrail-path-jail-prototype-only", "guardrail-synthetic-paths-only"]
        };

    public NodalOsPathJailPrototypeReadiness Evaluate(NodalOsPathJailPrototypeContract prototype) =>
        new()
        {
            ReadinessId = "path-jail-prototype-readiness-m547",
            PrototypeRef = prototype.PrototypeId,
            ReadyForRealPathJail = false,
            ReadyForRealCanonicalization = false,
            ReadyForDirectoryListing = false,
            ReadyForFileRead = false,
            ReadyForFileHash = false,
            ReadyForRealScan = false,
            MissingRequirementsRedacted =
            [
                "Operational canonicalization audit.",
                "Jail containment proof.",
                "Cancellation semantics.",
                "No-mutation proof."
            ],
            BlockersRedacted =
            [
                "Prototype contract is synthetic-only.",
                "No operational filesystem access is available.",
                "No scan authorization is available."
            ]
        };

    private static NodalOsPathJailCandidatePreview Candidate(
        string id,
        string syntheticPath,
        NodalOsPathJailPrototypePathKind kind,
        bool inside,
        bool outside,
        bool linkLike,
        bool caseVariant,
        bool share,
        bool driveBoundary,
        NodalOsPathJailPrototypeExpectedDecision expected) =>
        new()
        {
            CandidateId = id,
            SyntheticPathRedacted = syntheticPath,
            PathKind = kind,
            DeclaredInsideJail = inside,
            DeclaredOutsideJail = outside,
            DeclaredSymlink = linkLike,
            DeclaredCaseVariant = caseVariant,
            DeclaredNetworkShare = share,
            DeclaredDriveBoundary = driveBoundary,
            ExpectedPolicyDecision = expected,
            UserFacingExplanationRedacted = $"{kind} is a synthetic candidate for policy preview only."
        };
}

public sealed class NodalOsPathJailPrototypeJsonSerializer
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
        WriteIndented = true
    };

    public string SerializePrototype(NodalOsPathJailPrototypeContract prototype) =>
        JsonSerializer.Serialize(prototype, Options);

    public string SerializeCandidates(IReadOnlyList<NodalOsPathJailCandidatePreview> candidates) =>
        JsonSerializer.Serialize(candidates, Options);

    public string SerializeDecision(NodalOsPathJailPolicyDecisionPreview decision) =>
        JsonSerializer.Serialize(decision, Options);

    public string SerializeReadiness(NodalOsPathJailPrototypeReadiness readiness) =>
        JsonSerializer.Serialize(readiness, Options);
}

public static class NodalOsPathJailPrototypeFixtures
{
    public static NodalOsPathJailPrototypeContract Prototype() =>
        new NodalOsPathJailPrototypeService().CreatePrototype();

    public static IReadOnlyList<NodalOsPathJailCandidatePreview> Candidates() =>
        new NodalOsPathJailPrototypeService().CreateCandidates();
}
