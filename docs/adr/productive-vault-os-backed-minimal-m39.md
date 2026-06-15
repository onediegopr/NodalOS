# ADR M39: Productive Vault OS-backed Minimal

## Status

Accepted for M39.

## Context

Earlier vault milestones established reference-only boundaries, consent, policy, audit, and sandbox vault behavior. M39 demonstrates a minimal OS-backed provider while keeping real customer credentials blocked.

## Decision

M39 implements a minimal DPAPI CurrentUser provider for synthetic/test secrets only.

Provider mode:

- `OsBackedDpapiCurrentUser`.

The provider stores protected bytes bound to the current Windows user via DPAPI. Public results expose only references, provider kind, lifecycle metadata, reasons, and audit references. Public DTOs never contain raw secret values.

## Guarantees

The provider provides:

- OS-backed local user binding through DPAPI CurrentUser;
- synthetic-only store guard;
- core-only retrieval boundary;
- scoped consent requirement;
- policy requirement;
- phase gate requirement;
- license requirement for `ProductiveVault`;
- revoke/delete blocking future retrieval;
- redacted audit without values.

## Non-Guarantees

M39 does not provide:

- external vault support;
- HSM-backed custody;
- cross-machine portability;
- backup/restore workflow;
- customer credential onboarding;
- raw personal credential use;
- profile raw support;
- automatic login to real external sites.

DPAPI CurrentUser binds data to the current Windows user. Copying protected data to another user or machine is not a supported recovery path.

## License and Gate Integration

`ProductiveVault` remains disabled by default. It can pass only in a controlled admin/test context with explicit entitlement and manual admin override. Runtime gate checks distinguish:

- design-only vault;
- sandbox local encrypted vault;
- OS-backed minimal vault;
- production external vault;
- unknown provider.

The gate fails if public DTOs expose values, provider health is not OK, the provider is unknown, or ProductiveVault is enabled as a default plan feature.

## Synthetic Secrets Only

Tests use values such as:

- `synthetic-os-backed-password`;
- `synthetic-api-token-not-real`;
- `synthetic-login-secret`.

These are synthetic fixtures. Real personal, commercial, customer, fiscal, banking, ERP, or production credentials remain prohibited.

## Audit

Store, retrieve, and delete/revoke produce redacted audit events containing request ids, operation, provider kind, reference id, decision, timestamp, and reason. Audit/export must not include secret values.

## Out of Scope

M39 does not enable:

- real credential storage for customers;
- AFIP, banking, ERP, fiscal, or financial flows;
- raw user Chrome profiles;
- productive recorder/replay;
- payment or submit actions.

## Future Work

Before real customer credentials, the project still needs production key custody decisions, backup/restore design, operator procedures, customer consent/legal controls, incident response, and a final external-vault/OS-provider choice.
