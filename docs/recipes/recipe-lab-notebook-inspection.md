# Recipe Lab Notebook Inspection

Phase: 7/9 - Recipe Lab + Locator Repair Studio.

`RecipeLabNotebook` and `RecipeLabCell` provide inspection-only notebook cells.

## Cell Kinds

- Overview
- Preflight
- Workitem
- TriggerObservation
- ToolTrust
- SecretReference
- ActionResolution
- PlannedAction
- Validation
- Evidence
- Timeline
- HumanIntervention
- ApprovalNarrative
- Failure
- SafeNextAction
- LocatorCandidate
- LocatorRepair
- Handoff
- Cleanup

## Rules

- Every cell is inspection-only.
- Planned action cells cannot execute.
- Locator repair cells cannot apply changes.
- Trigger observation cells cannot start runs.
- Secret reference cells show aliases/refs only.
- Approval narrative cells cannot approve live runtime.
- Evidence cells are reference-only.
