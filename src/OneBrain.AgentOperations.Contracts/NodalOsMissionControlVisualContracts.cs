namespace OneBrain.AgentOperations.Contracts;

public enum NodalOsMissionControlDesktopBreakpointKind
{
    CompactDesktop,
    StandardDesktop,
    WideDesktop,
    UltrawideControlRoom
}

public enum NodalOsMissionControlDensityMode
{
    Comfortable,
    Compact,
    DenseLogHeavy
}

public enum NodalOsMissionControlPanelBehavior
{
    Expanded,
    Collapsed,
    HiddenDisabled
}

public sealed record NodalOsMissionControlLayoutBreakpoint
{
    public required NodalOsMissionControlDesktopBreakpointKind BreakpointKind { get; init; }

    public required int MinimumWidthPx { get; init; }

    public required NodalOsMissionControlDensityMode DensityMode { get; init; }

    public required NodalOsMissionControlPanelBehavior SidebarBehavior { get; init; }

    public required NodalOsMissionControlPanelBehavior RightPanelBehavior { get; init; }

    public required NodalOsMissionControlPanelBehavior BottomLogPanelBehavior { get; init; }

    public required string TimelineDensityRedacted { get; init; }

    public required string ApprovalCardBehaviorRedacted { get; init; }

    public required string EvidencePanelBehaviorRedacted { get; init; }

    public required string GuardrailExplainerBehaviorRedacted { get; init; }

    public required string OnboardingCardBehaviorRedacted { get; init; }
}

public sealed record NodalOsMissionControlLayoutSpec
{
    public required string LayoutSpecId { get; init; }

    public required string ProjectOperationalName { get; init; }

    public IReadOnlyList<NodalOsMissionControlLayoutBreakpoint> Breakpoints { get; init; } = [];

    public required bool ReadOnlyBadgeAlwaysVisible { get; init; }

    public required bool NoRuntimeBadgeAlwaysVisible { get; init; }

    public required bool DisabledControlsRemainVisible { get; init; }

    public required bool ModalTrapAllowed { get; init; }

    public required bool CanExecuteOrMutateState { get; init; }

    public required bool CallsBrowserRuntime { get; init; }

    public required bool UsesExternalCssFramework { get; init; }

    public DateTimeOffset CreatedAt { get; init; }
}

public sealed record NodalOsMissionControlStaticUxAcceptancePack
{
    public required string AcceptancePackId { get; init; }

    public required string ProjectOperationalName { get; init; }

    public required string RenderedPreviewArtifactRef { get; init; }

    public IReadOnlyList<string> UxChecklistRedacted { get; init; } = [];

    public IReadOnlyList<string> GuardrailChecklistRedacted { get; init; } = [];

    public IReadOnlyList<string> VisualDirectionChecklistRedacted { get; init; } = [];

    public IReadOnlyList<string> ContentChecklistRedacted { get; init; } = [];

    public IReadOnlyList<string> NamingChecklistRedacted { get; init; } = [];

    public IReadOnlyList<string> AccessibilityBasicsChecklistRedacted { get; init; } = [];

    public IReadOnlyList<string> NextUxGapsRedacted { get; init; } = [];

    public required bool ReadOnlyUi { get; init; }

    public required bool CanAuthorizeExecution { get; init; }

    public required bool RuntimeExecutionAllowed { get; init; }

    public required bool ProductiveFrontendAppIntroduced { get; init; }

    public DateTimeOffset CreatedAt { get; init; }
}

