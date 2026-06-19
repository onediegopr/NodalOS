# NODAL OS Legacy Nexa Subsystem Quarantine Plan

## Source

AUDIT-A found legacy `Nexa*` symbols in sensitive or potentially sensitive repository areas.

This is compatibility debt. It is not the operational product identity.

Operational project name: NODAL OS.

## Affected Patterns

Known legacy patterns include:

- `NexaEvidenceRef`;
- `NexaRunReport`;
- `NexaFailureKind`;
- `NexaMission`;
- `NexaAgentTask`;
- `Nexa*` billing, email, credentials, admin and configuration profile surfaces where present.

## Sensitive Domains

The quarantine requirement applies especially to:

- billing;
- email;
- credentials;
- admin;
- configuration profiles;
- cloud/licensing/BYOK boundaries.

## Risk

Legacy `Nexa*` subsystems can create naming ambiguity and accidental binding from future UI, cloud, licensing or BYOK work.

The immediate risk is controlled by disconnection, but cloud/licensing/BYOK must remain blocked until the legacy-sensitive surfaces are quarantined, removed or migrated.

## Blocking Rule

Cloud, licensing and BYOK are blocked until legacy `Nexa*` sensitive subsystems are handled by one of the safe options below and covered by tests.

## Safe Options

- Delete if proven unreferenced.
- Move to archive excluded from build.
- Mark obsolete and isolate from operational NODAL OS surfaces.
- Perform scoped migration to NODAL OS naming with compatibility aliases and tests.

## Prohibited Option

Do not perform broad rename without tests.

## Required Tests Before Closure

- Dependency direction tests for AgentOperations and future UI projects.
- Naming serialization tests for exportable NODAL OS surfaces.
- Cloud/licensing/BYOK block tests while legacy-sensitive `Nexa*` remains.
- Evidence model binding tests proving future UI uses NODAL OS evidence refs.

## M477-M479 Decision

No legacy subsystem is deleted in M477-M479.

The subsystem is documented as non-operational compatibility debt and remains quarantined by tests and roadmap guardrails.
