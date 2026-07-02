# ADR: NODAL OS Physical Export Policy Deepening Design-Only Protected

Decision target: `GO_NODAL_OS_PHYSICAL_EXPORT_POLICY_DEEPENING_DESIGN_ONLY_PROTECTED_READY`

## Status

Accepted as protected design-only physical export policy deepening if validation passes.

## Context

The controlled execution design track is paused and read-only. Existing Human Review packet export support is an in-memory preview and does not create a physical file, clipboard value or browser download. The next sensitive boundary is physical export policy, because real export would require redaction, consent, destination policy, evidence selection, durable audit trail, retention/deletion and external audit.

Control phrase:

`Physical export policy design may increase. Physical export readiness remains 0%.`

## Decision

Add a deterministic read-only physical export policy fixture:

- `PhysicalExportPolicyDesignOnlyProtected`
- `PhysicalExportReadiness`
- `PhysicalExportCapabilityStatus`
- `PhysicalExportFormatPreview`
- `PhysicalExportBlockedReason`
- `PhysicalExportRedactionRequirement`
- `PhysicalExportDestinationRequirement`
- `PhysicalExportConsentRequirement`
- `PhysicalExportEvidenceSelectionRequirement`
- `PhysicalExportAuditTrailRequirement`
- `PhysicalExportRetentionDeletionRequirement`
- `PhysicalExportAntiCapabilityProof`
- `PhysicalExportFutureImplementationChecklist`

The fixture is produced by `PhysicalExportPolicyDesignOnlyProtectedPresenter.CreateFixture()`. It is an in-memory DTO model. It does not expose physical export, file writer, PDF writer, DOCX writer, JSON file writer, Markdown file writer, clipboard, download, browser download, stream writer, filesystem write, redaction runtime, durable audit trail implementation, service registration, command handler, runtime/live, approval execution, approval mutation, writer/policy productive path or release/commercial readiness.

## Design Areas

1. Readiness: physical export implementation, file write, clipboard, download, PDF, DOCX, JSON file, Markdown file, redaction runtime, durable audit trail implementation and runtime/live readiness remain `0%`.
2. Format previews: PDF, DOCX, JSON, Markdown, clipboard and download are modeled as future previews only. Each format has `IsPreviewOnly=true`, `IsPhysicalOutput=false`, `IsGenerated=false`, `IsDownloaded=false` and `IsCopiedToClipboard=false`.
3. Redaction: future redaction runtime, secret scan and PII policy are required; raw payload export remains forbidden.
4. Consent: explicit future user consent is required, but consent is not implemented and cannot trigger export.
5. Destination: destination policy, workspace boundary and safe path policy are future requirements; external destinations remain blocked.
6. Evidence selection: allowed evidence classes are future refs; sensitive evidence is excluded by default and selection is not implemented.
7. Audit trail: export request, blocked and completed events are future requirements; no audit append or persisted export event exists.
8. Retention/deletion: retention class, deletion eligibility and tombstone requirements are future-only; retention and deletion workflows are not implemented.
9. Negative capability proof: explicit flags keep physical export, file output, clipboard, download, stream writer, filesystem, redaction runtime, audit append, service registration, command handlers, mutation, execution, runtime, provider/cloud, LLM, browser/CDP, WCU/OCR, recipes and release claims unavailable.

## Non-Goals

- No physical export.
- No file creation, file read or file write.
- No clipboard or download.
- No stream writer.
- No PDF or DOCX generation.
- No JSON or Markdown physical output.
- No redaction runtime.
- No durable audit trail real.
- No approval execution.
- No approval mutation.
- No runtime/live.
- No writer/policy productive integration.
- No command handler or service registration.
- No product action or enabled UI control.
- No DB, migration runner, repository or store real.
- No provider/cloud/network.
- No LLM live, semantic/vector backend or durable memory.
- No browser/CDP live, WCU live or OCR live.
- No recipe execution.
- No release/commercial readiness claim.

## Safety Proof

- Physical export implementation readiness: `0%`.
- File write readiness: `0%`.
- Clipboard readiness: `0%`.
- Download readiness: `0%`.
- PDF/DOCX/JSON/Markdown readiness: `0%`.
- Redaction runtime readiness: `0%`.
- Durable audit trail implementation readiness: `0%`.
- Runtime/live readiness: `0%`.
- Approval execution readiness: `0%`.
- Approval mutation readiness: `0%`.
- Product action count: `0`.
- Export action count: `0`.
- File output count: `0`.
- Clipboard action count: `0`.
- Download action count: `0`.
- Release/commercial readiness: `NO-GO`.

## Future Requirements

Any move from this design to real physical export requires a separate protected sequence and explicit user approval. Future work must cover redaction runtime, durable audit trail implementation, destination and workspace boundary policy, explicit user consent, evidence selection, retention/deletion, format renderer security review, runtime gate approval and an external release/commercial claim audit.
