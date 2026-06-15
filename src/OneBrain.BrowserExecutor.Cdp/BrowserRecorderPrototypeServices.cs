using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.BrowserExecutor.Cdp;

public sealed class BrowserRecorderReadOnlyPrototype
{
    public BrowserRecorderSession Start(BrowserRecorderStartRequest request)
    {
        if (!request.AllowlistedHosts.Contains(request.StartUri.Host, StringComparer.OrdinalIgnoreCase))
            throw new InvalidOperationException("Recorder start host is not allowlisted.");
        return new BrowserRecorderSession($"recorder-session-{Guid.NewGuid():N}", request.RunId, DateTimeOffset.UtcNow, ReadOnly: true, Redacted: true);
    }

    public BrowserRecorderSanitizationResult Capture(BrowserRecorderSession session, IEnumerable<BrowserRecorderObservation> observations)
    {
        var steps = observations.Select((observation, index) => CaptureStep(session, observation, index)).ToArray();
        var draft = new BrowserRecorderDraftRecipe(
            $"recipe-draft-{Guid.NewGuid():N}",
            1,
            steps,
            ExecutableByDefault: false,
            Redacted: true,
            new BrowserRecipeVersioningPolicy(1, RequiresMigrationPlan: true, ImmutablePublishedVersions: true));
        return new BrowserRecorderSanitizationResult(draft, SecretsRemoved: true, CookiesRemoved: true, BodiesRemoved: true, FullPathsRemoved: true);
    }

    private static BrowserRecorderCapturedStep CaptureStep(BrowserRecorderSession session, BrowserRecorderObservation observation, int index)
    {
        var target = new BrowserRecorderTargetDescriptor(
            BrowserCredentialRedactor.Redact(observation.SemanticTargets.FirstOrDefault() ?? observation.Title),
            SafeSelector(observation.SemanticTargets.FirstOrDefault()),
            MinimizeUrl(observation.Url),
            observation.Url.Host);
        var risk = observation.HasSubmit || observation.HasForm ? BrowserRecorderRiskAssessment.Risky : BrowserRecorderRiskAssessment.ReadOnly;
        var action = observation.HasSubmit ? BrowserRecordedActionKind.Submit : BrowserRecordedActionKind.Read;
        return new BrowserRecorderCapturedStep(
            $"recorded-step-{index + 1}",
            action,
            target,
            new BrowserRecorderVerificationCandidate($"verify-{index + 1}", BrowserCredentialRedactor.Redact($"verify {observation.Title} visible"), Required: true),
            risk,
            ["target host allowlisted", "read-only recorder session"],
            [$"recorder-evidence:{session.SessionId}:{index + 1}"],
            Executable: false,
            StoresSecret: false,
            StoresCookie: false,
            StoresBody: false);
    }

    private static string SafeSelector(string? semanticTarget)
    {
        if (string.IsNullOrWhiteSpace(semanticTarget))
            return "[data-recorder-target]";
        return $"[aria-label='{BrowserCredentialRedactor.Redact(semanticTarget)}']";
    }

    private static string MinimizeUrl(Uri uri) =>
        BrowserCredentialRedactor.Redact($"{uri.Scheme}://{uri.Host}{uri.AbsolutePath}");
}

