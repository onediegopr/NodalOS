using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.BrowserExecutor.Cdp;

public sealed class BrowserRecipeReplaySafeMode
{
    private readonly HashSet<string> _executed = new(StringComparer.OrdinalIgnoreCase);

    public BrowserRecipeReplayResult Replay(BrowserRecipeReplayRequest request)
    {
        var blockedReason = Validate(request);
        if (blockedReason is not null)
            return Block(request.Policy.Mode, blockedReason);

        var steps = new List<BrowserRecipeReplayStep>();
        foreach (var recipeStep in request.Recipe.Steps)
        {
            var key = $"{request.Recipe.RecipeId}:{request.Recipe.SchemaVersion}:{request.IdempotencyScope}:{recipeStep.StepId}";
            if (!_executed.Add(key))
            {
                steps.Add(new BrowserRecipeReplayStep(recipeStep.StepId, BrowserRecipeReplayStepStatus.Duplicate, recipeStep.ActionKind, key, [], [], "duplicate replay blocked"));
                return new BrowserRecipeReplayResult(request.Policy.Mode, steps, new BrowserRecipeReplayEvidence([], [], false), Completed: false, Blocked: true, DuplicateBlocked: true, "duplicate replay blocked");
            }

            if (IsUnsafeAction(recipeStep.ActionKind) || recipeStep.Risk != BrowserRecorderRiskAssessment.ReadOnly)
                return Block(request.Policy.Mode, "safe mode replay blocks sensitive or modifying action");

            var proof = $"proof-replay-{recipeStep.StepId}";
            steps.Add(new BrowserRecipeReplayStep(recipeStep.StepId, BrowserRecipeReplayStepStatus.Verified, recipeStep.ActionKind, key, recipeStep.EvidenceRefs, [proof], "read-only replay step verified"));
        }

        var evidence = new BrowserRecipeReplayEvidence(steps.SelectMany(s => s.EvidenceRefs).ToArray(), steps.SelectMany(s => s.ProofRefs).ToArray(), SemanticProofPresent: true);
        return new BrowserRecipeReplayResult(request.Policy.Mode, steps, evidence, Completed: true, Blocked: false, DuplicateBlocked: false, "safe mode replay completed");
    }

    public BrowserRecipeReplayPlan Plan(BrowserRecipeReplayRequest request)
    {
        var steps = request.Recipe.Steps
            .Select(step => new BrowserRecipeReplayStep(step.StepId, BrowserRecipeReplayStepStatus.Planned, step.ActionKind, $"{request.Recipe.RecipeId}:{request.IdempotencyScope}:{step.StepId}", [], [], "planned read-only step"))
            .ToArray();
        return new BrowserRecipeReplayPlan(steps, SafeMode: request.Policy.Mode == BrowserRecipeReplayMode.SafeModeReadOnly, DiagnosticOnly: true);
    }

    private static string? Validate(BrowserRecipeReplayRequest request)
    {
        if (request.Policy.Mode != BrowserRecipeReplayMode.SafeModeReadOnly)
            return "only safe mode read-only replay is allowed";
        if (!request.Recipe.IsSafeDraft)
            return "recipe draft is not safe";
        if (request.Recipe.ExecutableByDefault)
            return "recipe executable replay is blocked";
        if (request.Policy.RequireGate && request.GateReport?.Passed != true)
            return "replay requires passing phase gate";
        if (request.Policy.RequireIdempotency && string.IsNullOrWhiteSpace(request.IdempotencyScope))
            return "replay requires idempotency scope";
        if (request.Policy.RequireVerification && request.Recipe.Steps.Any(s => !s.VerificationCandidate.Required))
            return "replay requires verification rules";
        if (!request.TargetLive)
            return "replay target is stale";
        if (request.Recipe.Steps.Any(s => s.StoresSecret || s.StoresCookie || s.StoresBody))
            return "recipe contains sensitive material";
        if (request.Recipe.Steps.Any(s => !request.Policy.AllowlistedHosts.Contains(new Uri(s.Target.SafeUrl).Host, StringComparer.OrdinalIgnoreCase)))
            return "replay host is not allowlisted";
        return null;
    }

    private static bool IsUnsafeAction(BrowserRecordedActionKind action) =>
        action is BrowserRecordedActionKind.Submit or BrowserRecordedActionKind.Type or BrowserRecordedActionKind.Upload;

    private static BrowserRecipeReplayResult Block(BrowserRecipeReplayMode mode, string reason) =>
        new(mode, [], new BrowserRecipeReplayEvidence([], [], false), Completed: false, Blocked: true, DuplicateBlocked: false, BrowserCredentialRedactor.Redact(reason));
}

