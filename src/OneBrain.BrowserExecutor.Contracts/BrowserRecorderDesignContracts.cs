namespace OneBrain.BrowserExecutor.Contracts;

public enum BrowserRecordedActionKind
{
    Read,
    Navigate,
    Click,
    Type,
    Submit,
    Download,
    Upload
}

public enum BrowserRecordedRiskClassification
{
    Low,
    Modifying,
    Irreversible,
    Sensitive
}

public sealed record BrowserRecordedTargetDescriptor(
    string SemanticName,
    string SafeSelector,
    string SafeUrl);

public sealed record BrowserRecordedVerificationRule(
    string RuleId,
    string Description,
    bool Required);

public sealed record BrowserRecordedStepDraft(
    string StepId,
    BrowserRecordedActionKind ActionKind,
    BrowserRecordedTargetDescriptor Target,
    BrowserRecordedRiskClassification Risk,
    IReadOnlyList<BrowserRecordedVerificationRule> VerificationRules,
    bool RequiresApproval,
    bool RequiresIdempotency,
    bool StoresSecret,
    bool StoresCookie,
    bool StoresBody)
{
    public bool IsSafeDraft =>
        !StoresSecret &&
        !StoresCookie &&
        !StoresBody &&
        VerificationRules.Any(r => r.Required) &&
        (Risk != BrowserRecordedRiskClassification.Irreversible || RequiresApproval) &&
        (Risk == BrowserRecordedRiskClassification.Low || RequiresIdempotency || RequiresApproval);
}

public sealed record BrowserRecipeRedactionPolicy(bool RedactSecrets, bool RedactCookies, bool RedactBodies, bool MinimizeUrls, bool RemoveFullLocalPaths);

public sealed record BrowserRecipeApprovalPolicy(bool RequireHumanApprovalForIrreversibleActions, bool RequirePolicyApprovalForModifyingActions);

public sealed record BrowserRecipeVersioningPolicy(int CurrentSchemaVersion, bool RequiresMigrationPlan, bool ImmutablePublishedVersions);

public sealed record BrowserRecipeDraft(
    string RecipeId,
    IReadOnlyList<BrowserRecordedStepDraft> Steps,
    BrowserRecipeRedactionPolicy RedactionPolicy,
    BrowserRecipeApprovalPolicy ApprovalPolicy,
    BrowserRecipeVersioningPolicy VersioningPolicy,
    bool ExecutableReplayEnabled,
    bool DesignOnly)
{
    public bool IsSafeDesign =>
        DesignOnly &&
        !ExecutableReplayEnabled &&
        RedactionPolicy.RedactSecrets &&
        RedactionPolicy.RedactCookies &&
        RedactionPolicy.RedactBodies &&
        Steps.All(s => s.IsSafeDraft) &&
        VersioningPolicy.CurrentSchemaVersion > 0;
}

public sealed class BrowserRecorderDesign
{
    public bool Enabled => false;
    public bool ExecutableReplayEnabled => false;

    public BrowserRecipeDraft Sanitize(BrowserRecipeDraft draft)
    {
        var steps = draft.Steps.Select(step => step with
        {
            Target = step.Target with
            {
                SafeUrl = BrowserCredentialRedactor.Redact(MinimizeUrl(step.Target.SafeUrl)),
                SafeSelector = BrowserCredentialRedactor.Redact(step.Target.SafeSelector),
                SemanticName = BrowserCredentialRedactor.Redact(step.Target.SemanticName)
            },
            StoresSecret = false,
            StoresCookie = false,
            StoresBody = false
        }).ToArray();
        return draft with
        {
            Steps = steps,
            ExecutableReplayEnabled = false,
            DesignOnly = true,
            RedactionPolicy = draft.RedactionPolicy with { RedactSecrets = true, RedactCookies = true, RedactBodies = true, MinimizeUrls = true, RemoveFullLocalPaths = true }
        };
    }

    private static string MinimizeUrl(string value)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri))
            return value;
        return $"{uri.Scheme}://{uri.Host}{uri.AbsolutePath}";
    }
}

