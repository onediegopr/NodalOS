# NODAL OS M380 - Common Redaction / Sanitizer Audit

## Scope

This audit reviewed marker-based sanitizers and sensitive-content checks used by Agent Operations reporting and manifest surfaces. The block is documentation and core service cleanup only: no runtime actions, no UI, no orchestration API, no recipe execution, no namespace move, and no persistence.

## Search Commands Used

- `Select-String -Path src/**/*.cs, tests/**/*.cs -Pattern "Sanitizer"`
- `Select-String -Path src/**/*.cs, tests/**/*.cs -Pattern "Sanitize"`
- `Select-String -Path src/**/*.cs, tests/**/*.cs -Pattern "Redact"`
- `Select-String -Path src/**/*.cs, tests/**/*.cs -Pattern "Sensitive"`
- `Select-String -Path src/**/*.cs, tests/**/*.cs -Pattern "cookie|authorization|bearer|access_token|refresh_token|api_key|password|secret|set-cookie|private body"`

`rg` was not available in this environment, so `Select-String` was used.

## Current Sanitizers / Redactors Found

| Module | Path | Finding | Risk |
| --- | --- | --- | --- |
| Run Report | `src/OneBrain.BrowserExecutor.Cdp/NodalOsRunReportingServices.cs` | Local `SensitiveMarkers` list and marker replacement. | Divergence from other report sanitizers and substring false positives. |
| Recipe Manifest | `src/OneBrain.BrowserExecutor.Cdp/NodalOsRecipeManifestServices.cs` | Local `SecretMarkers` list used for manifest validation. | Manifest validation could diverge from report sanitization. |
| Agent Progress Reporting | `src/OneBrain.BrowserExecutor.Cdp/NodalOsAgentProgressReportingServices.cs` | Local `SensitiveMarkers` list and recursive report sanitizer. | Future persistence could sanitize differently than RunReport/RecipeManifest. |
| Step Library | `src/OneBrain.BrowserExecutor.Cdp/NodalOsStepLibraryServices.cs` | Local `SecretMarkers` list and label/description sanitizer. | Step metadata validation could drift from recipe/report rules. |
| Browser diagnostics and OCR privacy | Multiple browser/OCR files | Existing specialized redaction/privacy helpers. | Out of this block's migration scope; should remain specialized unless a later core redaction extraction is planned. |

## Duplicate Marker Lists

Duplicated markers included `cookie`, `set-cookie`, `authorization`, `bearer`, `password`, `secret`, `api_key`, `access_token`, `refresh_token`, and `id_token`. Some modules also included `private body`.

## Behavior Differences

- Some sanitizers used raw substring replacement.
- Some validators only rejected secret-like content and did not produce a redacted value.
- Some marker lists included `bearer ` while others used `bearer`.
- No shared classifier existed for field-name detection versus value-pattern detection.
- False-positive handling was not centralized.

## Decision

Create a common redaction contract and service in `BrowserExecutor.*` temporarily, because Agent Operations is still located there and namespace extraction was explicitly deferred. Existing public sanitizers remain as compatibility adapters and delegate to the common service.

## APIs Kept For Compatibility

- `NodalOsRunReportSanitizer`
- `NodalOsRecipeManifestValidator`
- `NodalOsAgentProgressReportSanitizer`
- `NodalOsStepLibrarySanitizer`

## APIs Delegating To Common Service

- RunReport safe checks and string sanitization.
- RecipeManifest secret-like validation.
- AgentProgressReport recursive sanitization and safe checks.
- StepLibrary secret-like checks and label/description sanitization.

## Not Touched

- Browser runtime behavior.
- OCR redaction/privacy pipeline.
- Evidence Ledger integration.
- UI/sidepanel.
- Recipe execution and orchestration.
- Namespace/project moves.

## Risk Notes

The new common service improves consistency but remains intentionally conservative and deterministic. It is not a full DLP engine. Future persistence should use this service as a precondition and may add structured field-level policies.
