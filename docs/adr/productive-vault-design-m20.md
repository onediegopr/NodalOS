# M20 - Productive Vault Design

## Context

The Browser Runtime has formal credential boundary, human handoff, vault consent contracts, persistent audit ledger, HMAC integrity, and companion consent UI surfaces. It still must not store or retrieve real secrets.

M20 is a design hito. It documents the productive vault architecture and adds guardrail contracts, but does not implement a real vault.

## Why No Real Vault Yet

Real vault activation requires unresolved production controls:

- key custody;
- OS-backed storage activation policy;
- emergency revoke;
- backup and restore process;
- consent UI authority challenge;
- tenant/company/person/worker scoping;
- malware and same-user process threat mitigation;
- operational audit review;
- migration and rotation procedures.

Until these are complete, productive providers remain design-only or fail-closed.

## Security Requirements

- Reference-only end-to-end.
- Core authority remains the only decision authority.
- Companion never receives secret values.
- Unknown secret or unknown provider fails closed.
- Consent does not equal authorization.
- Retrieval must be scoped and audited.
- Values never appear in logs, evidence, protocol, UI, exports, or audit ledger.
- Rotation and revocation are required before activation.
- No automatic login without explicit policy and verification.

## Threat Model

Threats considered:

- same-user malware requesting decrypt operations;
- local administrator compromise;
- copied disk or copied profile;
- stolen audit package;
- malicious companion message;
- consent replay;
- provider misconfiguration;
- over-broad tenant/person scope;
- unredacted diagnostic leak;
- backup restore to the wrong machine/user.

M20 does not solve all of these. It documents them and keeps providers fail-closed until the next phase.

## Storage Candidates

### A. DPAPI CurrentUser

Advantages:

- strong Windows user binding;
- simple local deployment;
- disk copy alone is usually insufficient.

Risks:

- same-user malware can request decrypt operations;
- migration and backup are hard;
- user profile compromise is high impact.

Complexity: medium.

Multiuser: per Windows user.

Portability: low without explicit migration.

Backup/restore: requires export/re-encrypt and recovery policy.

Rotation: re-protect material and audit every rotation.

Audit: record provider kind, reference, scope, consent, decision, no value.

### B. DPAPI LocalMachine

Advantages:

- works for service-style local agents;
- machine-bound storage.

Risks:

- may be too broad for multi-user systems;
- local privileged compromise has broad impact.

Complexity: medium.

Multiuser: machine-wide with strict ACL requirements.

Portability: low.

Backup/restore: requires machine recovery and re-keying.

Rotation: rotate protected blobs and review ACL.

Audit: bind machine/process/tenant/profile/session.

### C. Windows Credential Manager

Advantages:

- native OS credential store;
- user-facing management;
- aligns with Windows policy.

Risks:

- enumeration and access policy must be tightly controlled;
- UI consent semantics need hardening.

Complexity: medium.

Multiuser: per user and OS credential scope.

Portability: medium depending on Windows profile policy.

Backup/restore: depends on OS account and enterprise policy.

Rotation: update credential records by reference.

Audit: record credential target reference and decision only.

### D. OS-backed encrypted file

Advantages:

- explicit manifest and portable format;
- easier deterministic backup workflow.

Risks:

- key custody becomes the hard problem;
- file copy risk depends on key storage;
- migration must avoid leaking key material.

Complexity: high.

Multiuser: requires explicit key hierarchy.

Portability: medium if designed.

Backup/restore: encrypted backups and restore challenges required.

Rotation: key wrapping and manifest migration.

Audit: seal manifest hash and key reference only.

### E. External Vault Future

Advantages:

- enterprise governance;
- centralized policy;
- provider-native rotation and audit.

Risks:

- network dependency;
- tenant isolation complexity;
- availability and disaster recovery risks;
- local agent still needs scoped access policy.

Complexity: high.

Multiuser: provider role/tenant model.

Portability: high by provider design.

Backup/restore: provider-specific.

Rotation: provider-native.

Audit: correlate external audit ids with local HMAC ledger.

## HMAC Audit Ledger Integration

Vault operations must append audit events for:

- provider selected;
- storage requested;
- retrieval requested;
- use requested;
- deletion requested;
- rotation requested;
- export attempted;
- fail-closed;
- consent requested;
- consent granted or denied;
- consent expired or revoked.

Audit material stores references, provider kind, scope, actor, consent id, proof ref, decision, and reason. It never stores secret values.

## Failure Modes

- Unknown provider: fail closed.
- Unsupported provider: fail closed.
- Missing consent: requires consent or denied.
- Companion-only approval: not authoritative.
- Missing proof/challenge: denied.
- Expired/revoked consent: denied.
- Redaction uncertainty: blocked or redacted, never persisted raw.

## Decision

M20 adds `ProductiveVaultDesign` and `ProductiveVaultDesignGuard` contracts. These formalize the design and assert that real vault activation remains disabled.

No productive storage or retrieval is implemented in M20.

## Preconditions For Future Activation

Before M21/M22/M23 can enable real vault behavior:

- choose provider and key custody model;
- implement real consent challenge/authority;
- define backup/restore/revoke procedures;
- threat-model same-user process access;
- connect audit review workflow;
- run security audit against implementation;
- keep companion non-authoritative.
