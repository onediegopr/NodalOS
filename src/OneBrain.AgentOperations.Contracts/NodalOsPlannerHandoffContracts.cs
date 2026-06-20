namespace OneBrain.AgentOperations.Contracts;

public sealed record NodalOsPlannerHandoffPack
{
    public required string HandoffPackId { get; init; }

    public required string MissionRef { get; init; }

    public required string AssignmentRef { get; init; }

    public IReadOnlyList<string> TaskGraphDraftRefs { get; init; } = [];

    public IReadOnlyList<string> ReviewSessionRefs { get; init; } = [];

    public IReadOnlyList<string> SelectedBlockersRedacted { get; init; } = [];

    public IReadOnlyList<string> OpenQuestionsRedacted { get; init; } = [];

    public IReadOnlyList<string> MissingReadinessGatesRedacted { get; init; } = [];

    public IReadOnlyList<string> EvidenceRefs { get; init; } = [];

    public IReadOnlyList<string> TimelineRefs { get; init; } = [];

    public IReadOnlyList<string> ContextRefsRedacted { get; init; } = [];

    public IReadOnlyList<string> GuardrailRefs { get; init; } = [];

    public IReadOnlyList<string> DisclaimersRedacted { get; init; } = [];

    public required string WhatWasReviewedRedacted { get; init; }

    public required string WhatIsBlockedRedacted { get; init; }

    public required string WhatNeedsUserDecisionRedacted { get; init; }

    public required string EvidenceRefsOnlyRedacted { get; init; }

    public required string WhatIsNotVerifiedRedacted { get; init; }

    public required string WhatCannotExecuteRedacted { get; init; }

    public required string RecommendedNextSafeStepRedacted { get; init; }

    public required bool DraftOnly { get; init; }

    public required bool IsAuthoritative { get; init; }

    public required bool Executable { get; init; }

    public required bool PlannerRuntimeUsed { get; init; }

    public required bool CallsLlmProvider { get; init; }

    public required bool CreatesPrompt { get; init; }

    public required bool RuntimeExecutionAllowed { get; init; }

    public required bool FilesystemAccessUsed { get; init; }

    public required bool VerifiesEvidenceContent { get; init; }
}

public sealed record NodalOsPlannerHandoffRender
{
    public required string HandoffPackId { get; init; }

    public required string MarkdownRedacted { get; init; }

    public required string HtmlRedacted { get; init; }

    public required bool Deterministic { get; init; }

    public required bool ContainsRawPayload { get; init; }

    public required bool ContainsExternalResource { get; init; }
}
