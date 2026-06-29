# Phase D Context Packet Read-Only Export Preview

Decision target: `GO_PHASE_D_CONTEXT_PACKET_READ_ONLY_EXPORT_PREVIEW_READY`

## Decision

Add a deterministic, fixture-safe, in-memory export preview presenter for the Workspace Context Packet.

The preview is built from `WorkspaceContextPacketReadOnlySurfacePresenter.CreateFixture()` and keeps the existing Phase D guard chain visible:

- `WorkspaceContextAuthorityFreshnessGuard`;
- `WorkspaceContextSelectionLockExclusionGuard`;
- `WorkspaceMemoryCandidateContradictionRiskGuard`;
- context packet surface sections, warnings, blockers, disabled notices and no-side-effect proof.

## Scope

This ADR covers export preview contracts and presenter only. The preview is a copy-ready in-memory DTO/string. It does not create files, use clipboard APIs, start browser downloads, register product services, read workspace files, write files, create durable memory, use DBs, call provider/cloud/network, enable semantic/vector backends, start LLM live calls, execute migrations, or touch runtime/live/browser/CDP/WCU/OCR.

## Manifest Policy

The read-only manifest must keep these values:

- physical file created: false;
- clipboard used: false;
- browser download started: false;
- product actions count: 0;
- export actions count: 0;
- contains excluded payload values: false;
- contains sensitive-value-like content: false;
- contains durable memory: false.

## Sections

The preview contains 26 deterministic sections: manifest, executive summary, workspace identity, packet summary, selected/locked/excluded context, guard summaries, candidate summaries, safe next step, human review, warnings, blockers, disabled notices, no-side-effect proof, documented debt and next recommended block.

## Safety Rules

- Preview is not physical export.
- Packet is not durable memory.
- Candidate is not memory.
- No trust by default.
- Unsafe or missing state is shown as warning/blocker.
- Raw/sensitive source values are represented only as sanitized exclusions.
- Provider/cloud, semantic/vector, durable memory, runtime/live, physical export, clipboard and browser download remain disabled.

## Future Unlock Requirements

Any physical export, clipboard integration, browser download, visible UI action, real workspace source, durable memory, provider/semantic backend or runtime behavior requires a separate explicit hito with tests, manual QA, source policy, no-side-effect review and closeout audit.
