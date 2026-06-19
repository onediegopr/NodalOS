using System.Text.Json;
using System.Text.Json.Serialization;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.BrowserExecutor.Cdp;

public static class NodalOsRecipeManifestJsonSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public static string Serialize(NodalOsRecipeManifest manifest) =>
        JsonSerializer.Serialize(manifest, Options);

    public static NodalOsRecipeManifest Deserialize(string json) =>
        JsonSerializer.Deserialize<NodalOsRecipeManifest>(json, Options) ??
            throw new InvalidOperationException("Recipe manifest JSON did not deserialize.");
}

public sealed class NodalOsRecipeManifestValidator
{
    private static readonly NodalOsRecipeActionKind[] ShadowAllowedActions =
    [
        NodalOsRecipeActionKind.Navigate,
        NodalOsRecipeActionKind.Read,
        NodalOsRecipeActionKind.Extract,
        NodalOsRecipeActionKind.Wait,
        NodalOsRecipeActionKind.AskHuman,
        NodalOsRecipeActionKind.Stop
    ];

    private static readonly NodalOsRedactionService Redaction = new();

    public NodalOsRecipeManifestValidationResult Validate(NodalOsRecipeManifest manifest)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        AddRequired(errors, manifest.RecipeId, "RecipeId is required.");
        AddRequired(errors, manifest.Name, "Name is required.");
        AddRequired(errors, manifest.Version, "Version is required.");
        AddRequired(errors, manifest.GoalTemplate, "GoalTemplate is required.");

        if (manifest.Steps.Count == 0)
            errors.Add("Steps are required.");

        if (manifest.Policy.MaxRuntimeSteps is <= 0)
            errors.Add("MaxRuntimeSteps must be positive when provided.");

        if (manifest.Policy.MaxRuntimeSteps is { } maxRuntimeSteps &&
            manifest.Steps.Count > maxRuntimeSteps)
            errors.Add("MaxRuntimeSteps exceeded.");

        ValidateStepOrder(manifest.Steps, errors);

        foreach (var step in manifest.Steps)
            ValidateStep(step, manifest, errors, warnings);

        ValidateStatus(manifest, errors, warnings);
        ValidateSecretMarkers(manifest, errors);

        var requiresApproval = RequiresApproval(manifest);
        var canPassManifestPolicy = errors.Count == 0 &&
            manifest.Status is NodalOsRecipeStatus.Supervised or NodalOsRecipeStatus.Approved;

        if (manifest.Status == NodalOsRecipeStatus.Supervised && !requiresApproval)
        {
            errors.Add("Supervised manifests require approval.");
            canPassManifestPolicy = false;
        }

        return new NodalOsRecipeManifestValidationResult
        {
            IsValid = errors.Count == 0,
            CanPassManifestPolicy = canPassManifestPolicy,
            CanExecute = canPassManifestPolicy,
            RuntimeExecutionAllowed = false,
            RuntimeExecutionDeferred = true,
            RequiresGlobalPolicyEvaluation = true,
            RequiresApproval = requiresApproval,
            Errors = errors,
            Warnings = warnings
        };
    }

    public NodalOsRecipeManifestValidationResult ValidateCanExecute(NodalOsRecipeManifest manifest) =>
        Validate(manifest);

    public NodalOsRecipeManifestValidationResult ValidateStep(
        NodalOsRecipeStepManifest step,
        NodalOsRecipeManifest manifest)
    {
        var errors = new List<string>();
        var warnings = new List<string>();
        ValidateStep(step, manifest, errors, warnings);

        return new NodalOsRecipeManifestValidationResult
        {
            IsValid = errors.Count == 0,
            CanPassManifestPolicy = false,
            CanExecute = false,
            RuntimeExecutionAllowed = false,
            RuntimeExecutionDeferred = true,
            RequiresGlobalPolicyEvaluation = true,
            RequiresApproval = StepRequiresApproval(step, manifest.Policy),
            Errors = errors,
            Warnings = warnings
        };
    }

    public bool ContainsSensitiveAction(NodalOsRecipeManifest manifest) =>
        manifest.Steps.Any(IsSensitiveAction);

    public bool RequiresApproval(NodalOsRecipeManifest manifest) =>
        manifest.Policy.RequiresHumanApprovalByDefault ||
        manifest.Status == NodalOsRecipeStatus.Supervised ||
        manifest.Steps.Any(step => StepRequiresApproval(step, manifest.Policy));

    private static void ValidateStatus(
        NodalOsRecipeManifest manifest,
        List<string> errors,
        List<string> warnings)
    {
        switch (manifest.Status)
        {
            case NodalOsRecipeStatus.Draft:
                warnings.Add("Draft manifests are valid design artifacts but cannot execute.");
                break;
            case NodalOsRecipeStatus.Shadow:
                warnings.Add("Shadow manifests are observe/simulate only and cannot execute real actions.");
                if (manifest.Steps.Any(step => !ShadowAllowedActions.Contains(step.ActionKind)))
                    errors.Add("Shadow manifests cannot include real action steps.");
                break;
            case NodalOsRecipeStatus.Blocked:
                errors.Add("Blocked manifests cannot execute.");
                break;
            case NodalOsRecipeStatus.Deprecated:
                errors.Add("Deprecated manifests cannot execute in Recipe Manifest V1.");
                break;
            case NodalOsRecipeStatus.Supervised:
                warnings.Add("Supervised manifest may pass manifest policy only; runtime execution remains deferred and requires global policy evaluation.");
                break;
            case NodalOsRecipeStatus.Approved:
                warnings.Add("Approved manifest status is a governance state only; it does not grant runtime execution or bypass global policy.");
                break;
        }
    }

    private static void ValidateStep(
        NodalOsRecipeStepManifest step,
        NodalOsRecipeManifest manifest,
        List<string> errors,
        List<string> warnings)
    {
        AddRequired(errors, step.StepId, "StepId is required.");
        AddRequired(errors, step.Label, "Step Label is required.");

        if (step.Index < 0)
            errors.Add($"Step {step.StepId} index must be non-negative.");

        if (manifest.Policy.DisallowedActionKinds.Contains(step.ActionKind))
            errors.Add($"ActionKind {step.ActionKind} is disallowed by policy.");

        if (manifest.Policy.AllowedActionKinds.Count > 0 &&
            !manifest.Policy.AllowedActionKinds.Contains(step.ActionKind))
            errors.Add($"ActionKind {step.ActionKind} is not included in AllowedActionKinds.");

        if (manifest.Policy.SensitiveActionsBlocked && IsSensitiveAction(step))
            errors.Add($"Sensitive action {step.ActionKind} is blocked by recipe policy.");

        if (StepRequiresApproval(step, manifest.Policy))
            warnings.Add($"Step {step.StepId} requires approval.");

        if (!string.IsNullOrWhiteSpace(step.UrlTemplate) &&
            manifest.AllowedDomains.Count > 0 &&
            !UrlMatchesAllowedDomain(step.UrlTemplate, manifest.AllowedDomains))
            errors.Add($"UrlTemplate for step {step.StepId} is outside AllowedDomains.");
    }

    private static bool StepRequiresApproval(
        NodalOsRecipeStepManifest step,
        NodalOsRecipePolicyManifest policy) =>
        policy.RequiresHumanApprovalByDefault ||
        step.RequiresApproval ||
        step.ActionKind is NodalOsRecipeActionKind.DownloadRequest or NodalOsRecipeActionKind.UploadRequest ||
        (step.ActionKind == NodalOsRecipeActionKind.Type && step.TargetsSensitiveField);

    private static bool IsSensitiveAction(NodalOsRecipeStepManifest step) =>
        step.ActionKind is NodalOsRecipeActionKind.DownloadRequest or NodalOsRecipeActionKind.UploadRequest ||
        (step.ActionKind == NodalOsRecipeActionKind.Type && step.TargetsSensitiveField);

    private static void ValidateStepOrder(IReadOnlyList<NodalOsRecipeStepManifest> steps, List<string> errors)
    {
        if (steps.Select(step => step.Index).Distinct().Count() != steps.Count)
            errors.Add("Step Index values must be unique.");

        if (!steps.Select(step => step.Index).SequenceEqual(steps.Select(step => step.Index).OrderBy(index => index)))
            errors.Add("Step Index values must be ordered.");
    }

    private static void ValidateSecretMarkers(NodalOsRecipeManifest manifest, List<string> errors)
    {
        var values = new List<string?>
        {
            manifest.RecipeId,
            manifest.Name,
            manifest.Description,
            manifest.Version,
            manifest.GoalTemplate
        };

        values.AddRange(manifest.AllowedDomains);
        values.AddRange(manifest.SuccessCriteria);
        values.AddRange(manifest.FailureSignals);
        values.AddRange(manifest.EvidenceRequirements);
        values.AddRange(manifest.Steps.SelectMany(step => new[]
        {
            step.StepId,
            step.Label,
            step.UrlTemplate,
            step.ExpectedOutcome
        }));
        values.AddRange(manifest.Steps.SelectMany(step => step.SelectorHints));
        values.AddRange(manifest.Steps.SelectMany(step => step.FallbackHints));

        if (values.Where(value => !string.IsNullOrWhiteSpace(value)).Any(value => ContainsSecretMarker(value!)))
            errors.Add("Manifest contains secret-like fields.");
    }

    private static bool ContainsSecretMarker(string value) =>
        Redaction.ContainsSensitiveContent(value);

    private static bool UrlMatchesAllowedDomain(string urlTemplate, IReadOnlyList<string> allowedDomains)
    {
        if (!Uri.TryCreate(urlTemplate, UriKind.Absolute, out var uri))
            return false;

        return allowedDomains.Any(domain =>
            uri.Host.Equals(domain, StringComparison.OrdinalIgnoreCase) ||
            uri.Host.EndsWith($".{domain}", StringComparison.OrdinalIgnoreCase));
    }

    private static void AddRequired(List<string> errors, string? value, string message)
    {
        if (string.IsNullOrWhiteSpace(value))
            errors.Add(message);
    }
}

public static class NodalOsRecipeManifestFixtures
{
    public static readonly DateTimeOffset FixedTimestamp = new(2026, 6, 18, 0, 0, 0, TimeSpan.Zero);

    public static NodalOsRecipeManifest ReadOnlyRecipe() =>
        BaseRecipe(
            recipeId: "recipe-read-only-001",
            name: "Read-only fixture recipe",
            status: NodalOsRecipeStatus.Approved,
            steps:
            [
                Step("step-001", 0, "Navigate to fixture", NodalOsRecipeActionKind.Navigate, "https://example.invalid/fixture"),
                Step("step-002", 1, "Read fixture", NodalOsRecipeActionKind.Read, "https://example.invalid/fixture")
            ],
            policy: new NodalOsRecipePolicyManifest
            {
                AllowedActionKinds =
                [
                    NodalOsRecipeActionKind.Navigate,
                    NodalOsRecipeActionKind.Read,
                    NodalOsRecipeActionKind.Extract,
                    NodalOsRecipeActionKind.Wait,
                    NodalOsRecipeActionKind.Stop
                ],
                SensitiveActionsBlocked = true,
                MaxRuntimeSteps = 5
            });

    public static NodalOsRecipeManifest SupervisedRecipe() =>
        BaseRecipe(
            recipeId: "recipe-supervised-001",
            name: "Supervised fixture recipe",
            status: NodalOsRecipeStatus.Supervised,
            steps:
            [
                Step("step-001", 0, "Read fixture", NodalOsRecipeActionKind.Read, "https://example.invalid/fixture", requiresApproval: true)
            ],
            policy: new NodalOsRecipePolicyManifest
            {
                RequiresHumanApprovalByDefault = true,
                AllowedActionKinds = [NodalOsRecipeActionKind.Read],
                SensitiveActionsBlocked = true,
                MaxRuntimeSteps = 3
            });

    public static NodalOsRecipeManifest BlockedRecipe() =>
        ReadOnlyRecipe() with { RecipeId = "recipe-blocked-001", Name = "Blocked fixture recipe", Status = NodalOsRecipeStatus.Blocked };

    public static NodalOsRecipeManifest UnsafeDisallowedRecipe() =>
        BaseRecipe(
            recipeId: "recipe-unsafe-001",
            name: "Unsafe fixture recipe",
            status: NodalOsRecipeStatus.Approved,
            steps:
            [
                Step("step-001", 0, "Upload fixture", NodalOsRecipeActionKind.UploadRequest, "https://example.invalid/upload")
            ],
            policy: new NodalOsRecipePolicyManifest
            {
                AllowedActionKinds = [NodalOsRecipeActionKind.UploadRequest],
                DisallowedActionKinds = [NodalOsRecipeActionKind.UploadRequest],
                SensitiveActionsBlocked = true,
                MaxRuntimeSteps = 2
            });

    public static NodalOsRecipeStepManifest Step(
        string stepId,
        int index,
        string label,
        NodalOsRecipeActionKind actionKind,
        string? urlTemplate = null,
        bool requiresApproval = false,
        bool targetsSensitiveField = false) =>
        new()
        {
            StepId = stepId,
            Index = index,
            Label = label,
            ActionKind = actionKind,
            UrlTemplate = urlTemplate,
            SelectorHints = ["#fixture"],
            FallbackHints = ["fixture text"],
            RequiresApproval = requiresApproval,
            ExpectedOutcome = "Fixture outcome only; no runtime action.",
            TargetsSensitiveField = targetsSensitiveField
        };

    private static NodalOsRecipeManifest BaseRecipe(
        string recipeId,
        string name,
        NodalOsRecipeStatus status,
        IReadOnlyList<NodalOsRecipeStepManifest> steps,
        NodalOsRecipePolicyManifest policy) =>
        new()
        {
            RecipeId = recipeId,
            Name = name,
            Description = "Controlled Recipe Manifest V1 fixture.",
            Version = "1.0.0",
            Status = status,
            GoalTemplate = "Observe controlled fixture data.",
            AllowedDomains = ["example.invalid"],
            Steps = steps,
            Policy = policy,
            SuccessCriteria = ["fixture observed"],
            FailureSignals = ["fixture unavailable"],
            EvidenceRequirements = ["run-report", "policy-decision"],
            CreatedAt = FixedTimestamp,
            UpdatedAt = FixedTimestamp
        };
}
