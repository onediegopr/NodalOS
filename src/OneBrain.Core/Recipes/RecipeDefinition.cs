namespace OneBrain.Core.Recipes;

public sealed record RecipeDefinition(
    string Name,
    string? Description = null,
    int? DefaultTimeoutMs = null,
    bool StopOnFirstFailure = true,
    int? MaxDurationMs = null)
{
    public string? RecipeId { get; init; }
    public string? DisplayName { get; init; }
    public string? Version { get; init; }
    public string? Category { get; init; }
    public string? SystemTarget { get; init; }
    public string? RegionCountry { get; init; }
    public string? RiskProfileRef { get; init; }
    public List<string> RequiredCapabilities { get; init; } = new();
    public List<string> RequiredToolTrustRefs { get; init; } = new();
    public List<string> RequiredSecretRefs { get; init; } = new();
    public string? InputSchemaRef { get; init; }
    public string? OutputSchemaRef { get; init; }
    public List<RecipeLifecycleStage> LifecycleStages { get; init; } = new();
    public List<RecipeBlock> Blocks { get; init; } = new();
    public string? LimitsRef { get; init; }
    public RecipeRunLimits? RunLimits { get; init; }
    public string? CompleteCriteriaRef { get; init; }
    public RecipeCompleteCriteria? CompleteCriteria { get; init; }
    public string? TerminateCriteriaRef { get; init; }
    public RecipeTerminateCriteria? TerminateCriteria { get; init; }
    public RecipeValidationPolicy? ValidationPolicy { get; init; }
    public RecipeRiskProfile? RuntimeRiskProfile { get; init; }
    public ActionResolutionPolicy? ActionResolutionPolicy { get; init; }
    public List<string> ApprovalCheckpointRefs { get; init; } = new();
    public List<string> EvidenceExpectationRefs { get; init; } = new();
    public string? CreatedBy { get; init; }
    public string? UpdatedBy { get; init; }
    public DateTimeOffset? CreatedAt { get; init; }
    public DateTimeOffset? UpdatedAt { get; init; }
    public List<RecipeStepDefinition> Steps { get; init; } = new();
    public Dictionary<string, string>? Variables { get; init; }
}
