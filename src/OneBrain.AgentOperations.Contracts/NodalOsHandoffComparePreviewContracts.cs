namespace OneBrain.AgentOperations.Contracts;

public enum NodalOsHandoffCompareMode
{
    RefsOnly,
    BlockersAndReadiness,
    EvidenceTimelineContextRefs,
    FullPreviewMetadata
}

public sealed record NodalOsHandoffCompareRequest
{
    public required string CompareRequestId { get; init; }

    public required string LeftHandoffRef { get; init; }

    public required string RightHandoffRef { get; init; }

    public required NodalOsHandoffCompareMode CompareMode { get; init; }

    public IReadOnlyList<string> RequestedSectionsRedacted { get; init; } = [];

    public required bool DraftOnly { get; init; }

    public required bool RefOnly { get; init; }
}

public sealed record NodalOsHandoffCompareResult
{
    public required string CompareResultId { get; init; }

    public required string CompareRequestId { get; init; }

    public IReadOnlyList<string> AddedBlockersRedacted { get; init; } = [];

    public IReadOnlyList<string> RemovedBlockersRedacted { get; init; } = [];

    public IReadOnlyList<string> ChangedOpenQuestionsRedacted { get; init; } = [];

    public IReadOnlyList<string> ChangedMissingReadinessGatesRedacted { get; init; } = [];

    public IReadOnlyList<string> ChangedEvidenceRefs { get; init; } = [];

    public IReadOnlyList<string> ChangedTimelineRefs { get; init; } = [];

    public IReadOnlyList<string> ChangedContextRefsRedacted { get; init; } = [];

    public IReadOnlyList<string> ChangedGuardrailsRedacted { get; init; } = [];

    public IReadOnlyList<string> UnchangedSectionsRedacted { get; init; } = [];

    public IReadOnlyList<string> UnverifiedClaimsRedacted { get; init; } = [];

    public required string UserFacingSummaryRedacted { get; init; }

    public required bool RefOnly { get; init; }

    public required bool ContainsRawPayload { get; init; }

    public required bool VerifiesEvidenceContent { get; init; }

    public required bool CallsLlm { get; init; }

    public required bool MutatesFilesystem { get; init; }

    public required bool CallsNetwork { get; init; }

    public required bool ProductivePersistenceUsed { get; init; }
}

public sealed record NodalOsHandoffCompareRender
{
    public required string CompareResultId { get; init; }

    public required string MarkdownRedacted { get; init; }

    public required string HtmlRedacted { get; init; }

    public required bool Deterministic { get; init; }

    public required bool ContainsExternalResource { get; init; }

    public required bool ContainsScript { get; init; }
}
