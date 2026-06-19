# NODAL OS M382 - Common Redaction / Sanitizer Service

## Problem Resolved

Agent Operations had multiple marker-based sanitizers with duplicated sensitive marker lists. This created risk of divergent behavior across Run Report, Recipe Manifest, Progress Reporting, and Step Library surfaces.

## Services Created

- `NodalOsSensitiveContentClassifier`
- `NodalOsRedactionService`
- `NodalOsRedactionResult`
- `NodalOsStructuredRedactionResult`
- `NodalOsRedactionMatch`
- `NodalOsRedactionOptions`
- `NodalOsSensitiveContentKind`

## Detection

Field-name detection covers cookie, set-cookie, authorization, password/pass/passwd, secret, api key, access token, refresh token, id token, generic token, and private body fields.

Value-pattern detection covers Bearer tokens, Basic auth values, JWT-like token strings, sensitive query/key-value assignments, cookie/set-cookie headers, authorization markers, password markers, secret markers, and generic token markers.

## False Positive Controls

The classifier avoids obvious safe plain-language values such as:

- `secretary`
- `passenger`
- `tokenization strategy`
- `cookie policy document`

## Compatibility Adapters

Existing public APIs remain in place:

- `NodalOsRunReportSanitizer`
- `NodalOsRecipeManifestValidator`
- `NodalOsAgentProgressReportSanitizer`
- `NodalOsStepLibrarySanitizer`

They now delegate to the common redaction/classification service where applicable.

## Security Properties

- Redaction is deterministic.
- Field names are preserved in structured dictionary redaction.
- Raw sensitive values are not stored in redaction matches.
- Diagnostics use safe reasons only.
- Redaction defaults to `[REDACTED]`.

## What This Does Not Implement

- No persistence DB.
- No UI.
- No sidepanel.
- No runtime report emission.
- No recipe execution.
- No orchestration API.
- No Evidence Ledger bridge.
- No namespace/project move.

## Known Limits

This service is a shared deterministic classifier/redactor, not full semantic DLP. Future persistence and Evidence Ledger integration should treat it as a common baseline and add structured policy checks where needed.

## Next Step

Recommended next milestone: `M383-M385 EvidenceRef Ledger Bridge Design`.
