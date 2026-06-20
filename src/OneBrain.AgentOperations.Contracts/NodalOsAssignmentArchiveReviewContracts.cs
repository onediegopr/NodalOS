namespace OneBrain.AgentOperations.Contracts;

public sealed record NodalOsAssignmentArchiveReviewMilestone
{
    public required string MilestoneId { get; init; }

    public required string LabelRedacted { get; init; }

    public required string DecisionRedacted { get; init; }

    public string? CommitRef { get; init; }

    public IReadOnlyList<string> ArtifactRefs { get; init; } = [];

    public IReadOnlyList<string> EvidenceRefs { get; init; } = [];

    public IReadOnlyList<string> TimelineRefs { get; init; } = [];

    public required string WhatIsCompleteRedacted { get; init; }

    public required string WhatRemainsMockRedacted { get; init; }

    public required string CannotBePromotedToRuntimeRedacted { get; init; }

    public IReadOnlyList<string> AuditTriggersRedacted { get; init; } = [];
}

public sealed record NodalOsAssignmentArchiveReviewStatus
{
    public required bool CanArchiveAsGovernanceBaseline { get; init; }

    public required bool CanUseAsRuntimeBaseline { get; init; }

    public required bool CanUseAsPlannerImplementation { get; init; }

    public required bool CanUseAsLlmPromptSource { get; init; }

    public required bool CanUseAsFilesystemAuthority { get; init; }
}

public sealed record NodalOsAssignmentArchiveReview
{
    public required string ArchiveReviewId { get; init; }

    public IReadOnlyList<NodalOsAssignmentArchiveReviewMilestone> ClosedMilestones { get; init; } = [];

    public required NodalOsAssignmentArchiveReviewStatus ArchiveStatus { get; init; }

    public IReadOnlyList<string> ArchiveWarningsRedacted { get; init; } = [];

    public IReadOnlyList<string> EvidenceRefs { get; init; } = [];

    public IReadOnlyList<string> TimelineRefs { get; init; } = [];

    public IReadOnlyList<string> GuardrailRefs { get; init; } = [];

    public required bool DraftOnly { get; init; }

    public required bool CanAuthorizeExecution { get; init; }

    public required bool CanCallPlanner { get; init; }

    public required bool CanCallLlm { get; init; }

    public required bool CanAccessFilesystem { get; init; }

    public required bool CanCallCloud { get; init; }

    public DateTimeOffset CreatedAt { get; init; }
}
