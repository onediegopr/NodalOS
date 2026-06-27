# Recipe Connector Eligibility Contract

Phase: 5/9 - Tool Trust Registry + Secrets by Reference.

`RecipeConnectorEligibility` describes whether a connector draft can be considered for reference-only, fixture-only, preview, dry-run, or manual-assist flows.

It does not execute connectors.

## Runtime Modes

- `ReferenceOnly`
- `FixtureOnly`
- `PreviewOnly`
- `DryRunOnly`
- `ManualAssistOnly`
- `LiveBlocked`
- `FutureGated`
- `Disabled`

Live connector execution remains blocked in all Phase 5 outcomes.

## Action Categories

Read-only metadata/data actions can be eligible for preview/fixture when tool trust, secret refs, approval policies, and evidence policies are present.

Mutating or sensitive connector actions require approval and remain live-blocked:

- fiscal submit,
- payment execution,
- message send,
- listing publish,
- price update,
- stock update,
- data delete,
- data mutation.

Unknown connector actions are blocked.

`FutureConnectorRuntime` evidence capture remains blocked/future-gated until connector eligibility policy marks the work reference/fixture-only.
