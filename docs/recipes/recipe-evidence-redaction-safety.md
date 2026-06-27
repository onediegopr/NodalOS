# Recipe Evidence Redaction Safety

Phase: 3/9 - Evidence Pack + Timeline Projection.

Recipe evidence is safe by default when it is reference-only, redacted, and free of raw payloads or secret values.

## Redaction Summary

`RecipeEvidenceRedactionSummary` records:

- whether redaction was applied,
- redaction policy ref,
- sensitive field categories,
- secret refs,
- secret handling status,
- raw payload exposure flag,
- handoff safety flag,
- timeline safety flag,
- blocked reason when redaction policy prevents export.

## Sensitive Categories

Sensitive field categories include:

- secret,
- credential,
- token,
- personal data,
- fiscal data,
- payment data,
- legal data,
- marketplace data,
- business confidential,
- unknown sensitive.

Secret values must never be present. Secret handling is by reference only or redacted.

## Blocking Rules

Evidence completeness blocks when:

- required evidence refs are missing,
- required validation evidence is not run or blocked,
- raw payloads are exposed,
- secret-like values are present,
- evidence is unsafe for handoff,
- evidence is unsafe for timeline,
- live runtime capture mode is requested.

No real screenshot, DOM, accessibility tree, HAR, network log, or filesystem runtime capture is allowed in this phase.
