namespace OneBrain.Core.Approval;

public enum ActionSensitivity
{
    Benign = 0,
    Sensitive = 1,
    Unknown = 2
}

public sealed record SensitiveActionClassifierReport(
    IReadOnlyList<string> ApprovalPolicyKinds,
    IReadOnlyList<string> RecipeRunnerKinds,
    IReadOnlyList<string> ProgramKinds,
    IReadOnlyList<string> CanonicalSensitiveStepKinds,
    IReadOnlyList<string> CanonicalBenignStepKinds,
    IReadOnlyList<string> StepKindDifferences);

public static class SensitiveActionClassifier
{
    private static readonly HashSet<string> CanonicalSensitiveKinds = new(StringComparer.OrdinalIgnoreCase)
    {
        "actv.invoke",
        "actv.type",
        "key",
        "app.open",
        "app.close",
        "browser.open",
        "browser.close",
        "safe.click",
        "safe.type"
    };

    private static readonly HashSet<string> CanonicalBenignKinds = new(StringComparer.OrdinalIgnoreCase)
    {
        "approval.manifest",
        "artifact.summarizeproductevidence",
        "artifact.writeproductevidence",
        "assert.contains",
        "assert.equals",
        "browser.read",
        "debug.hang",
        "delay",
        "diagnose.msaa",
        "discover.actionableelements",
        "extract.productevidence",
        "extract.visiblefields",
        "if",
        "note",
        "plan.safenavigation",
        "preflight.click",
        "profile.load",
        "report.writeproductevidencehtml",
        "report.writeproductevidencemarkdown",
        "safe.read",
        "sleep",
        "snapshot.read",
        "target.observe",
        "target.observe.desktop",
        "visual.capture",
        "visual.capture.element",
        "visual.capture.window",
        "visual.verify.changed",
        "wait"
    };

    public static IReadOnlyList<string> GetCanonicalSensitiveStepKinds() =>
        CanonicalSensitiveKinds.OrderBy(kind => kind, StringComparer.OrdinalIgnoreCase).ToList();

    public static IReadOnlyList<string> GetCanonicalBenignStepKinds() =>
        CanonicalBenignKinds.OrderBy(kind => kind, StringComparer.OrdinalIgnoreCase).ToList();

    public static ActionSensitivity ClassifyStepKind(string? kind)
    {
        var normalized = Normalize(kind);
        if (normalized.Length == 0)
            return ActionSensitivity.Unknown;

        if (CanonicalSensitiveKinds.Contains(normalized))
            return ActionSensitivity.Sensitive;

        if (CanonicalBenignKinds.Contains(normalized))
            return ActionSensitivity.Benign;

        return ActionSensitivity.Unknown;
    }

    public static bool IsSensitiveStepKind(string? kind)
    {
        return ClassifyStepKind(kind) is ActionSensitivity.Sensitive or ActionSensitivity.Unknown;
    }

    public static SensitiveActionClassifierReport InspectCurrentBehavior()
    {
        var approvalPolicyKinds = ApprovalPolicy.DefaultPlatformPolicy.SensitiveActionKinds
            .OrderBy(kind => kind, StringComparer.OrdinalIgnoreCase)
            .ToList();
        var recipeRunnerKinds = GetCanonicalSensitiveStepKinds();
        var programKinds = GetCanonicalSensitiveStepKinds();
        var canonicalSensitive = GetCanonicalSensitiveStepKinds();
        var canonicalBenign = GetCanonicalBenignStepKinds();
        var differences = recipeRunnerKinds
            .Concat(programKinds)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Where(kind => !canonicalSensitive.Contains(kind, StringComparer.OrdinalIgnoreCase))
            .ToList();

        return new SensitiveActionClassifierReport(
            ApprovalPolicyKinds: approvalPolicyKinds,
            RecipeRunnerKinds: recipeRunnerKinds,
            ProgramKinds: programKinds,
            CanonicalSensitiveStepKinds: canonicalSensitive,
            CanonicalBenignStepKinds: canonicalBenign,
            StepKindDifferences: differences);
    }

    private static string Normalize(string? kind) =>
        string.IsNullOrWhiteSpace(kind) ? "" : kind.Trim().ToLowerInvariant();
}
