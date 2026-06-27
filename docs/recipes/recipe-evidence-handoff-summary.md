# Recipe Evidence Handoff Summary

Phase: 3/9 - Evidence Pack + Timeline Projection.

`RecipeEvidenceHandoffSummary` and `RecipeEvidenceExportSummary` provide operator-visible summaries of recipe evidence by reference.

## Included

- recipe id and version,
- run id,
- run status,
- evidence completeness,
- timeline projection status,
- blocking issues,
- safe next action,
- operator-visible redacted summary,
- redaction summary,
- artifact refs.

## Omitted

The handoff/export summary explicitly omits:

- raw screenshots,
- raw DOM,
- raw accessibility trees,
- HAR/network logs,
- secret values,
- raw payloads,
- clipboard content,
- executable replay data.

## Safety

Handoff summaries are safe by default only when redaction has no raw secret exposure and evidence is safe for handoff. Auth, challenge, 2FA, or CAPTCHA failure evidence must request human intervention or block; it must not recommend bypass or automated solving.
