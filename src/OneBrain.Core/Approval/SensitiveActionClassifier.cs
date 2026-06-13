namespace OneBrain.Core.Approval;

public sealed record SensitiveActionClassifierReport(
    IReadOnlyList<string> ApprovalPolicyKinds,
    IReadOnlyList<string> RecipeRunnerKinds,
    IReadOnlyList<string> ProgramKinds,
    IReadOnlyList<string> ProposedCanonicalKinds,
    IReadOnlyList<string> Differences);

public static class SensitiveActionClassifier
{
    private static readonly string[] RecipeRunnerSensitiveKinds =
    [
        "actv.invoke",
        "actv.type",
        "key",
        "app.open",
        "app.close",
        "browser.open",
        "browser.close",
        "safe.click"
    ];

    private static readonly string[] ProgramSensitiveKinds =
    [
        "actv.invoke",
        "actv.type",
        "key",
        "app.open",
        "browser.open",
        "browser.close",
        "safe.click"
    ];

    public static SensitiveActionClassifierReport InspectCurrentBehavior()
    {
        var approvalPolicyKinds = ApprovalPolicy.DefaultPlatformPolicy.SensitiveActionKinds
            .OrderBy(kind => kind, StringComparer.OrdinalIgnoreCase)
            .ToList();
        var recipeRunnerKinds = RecipeRunnerSensitiveKinds
            .OrderBy(kind => kind, StringComparer.OrdinalIgnoreCase)
            .ToList();
        var programKinds = ProgramSensitiveKinds
            .OrderBy(kind => kind, StringComparer.OrdinalIgnoreCase)
            .ToList();
        var proposed = approvalPolicyKinds
            .Concat(recipeRunnerKinds)
            .Concat(programKinds)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(kind => kind, StringComparer.OrdinalIgnoreCase)
            .ToList();
        var differences = proposed
            .Where(kind => !(approvalPolicyKinds.Contains(kind, StringComparer.OrdinalIgnoreCase)
                          && recipeRunnerKinds.Contains(kind, StringComparer.OrdinalIgnoreCase)
                          && programKinds.Contains(kind, StringComparer.OrdinalIgnoreCase)))
            .ToList();

        return new SensitiveActionClassifierReport(
            ApprovalPolicyKinds: approvalPolicyKinds,
            RecipeRunnerKinds: recipeRunnerKinds,
            ProgramKinds: programKinds,
            ProposedCanonicalKinds: proposed,
            Differences: differences);
    }
}
