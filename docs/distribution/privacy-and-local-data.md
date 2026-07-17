# NODAL OS — Installed product privacy and local data boundary

This is a technical disclosure for the private-beta package. It is not a substitute for the final legal privacy notice required before public distribution.

## Local-first default

NODAL OS runs the Mission Control server on a loopback HTTP origin. The installed application does not expose a public listener and does not require a NODAL OS cloud account.

Product state is stored under the current Windows user, primarily below:

```text
%LOCALAPPDATA%\NodalOS
```

Depending on the product flow, local state can include:

- redacted workspace metadata and an opaque protected reference to the selected root;
- mission goal, plan, action candidate and progress metadata;
- approval decisions bound to a mission and exact scope;
- execution, verification, evidence and rollback metadata;
- app-local snapshots required for a guarded restore plan;
- BYOK provider/model configuration with opaque secret references;
- DPAPI-protected credential bytes;
- opt-in redacted startup/error/process diagnostic events;
- process-smoke state used by engineering validation.

Raw API keys are not stored in JSON. Absolute workspace roots and provider response content are excluded from the public product projections. The BYOK connection test persists a response hash rather than the response body.

## Workspace access

Selecting a workspace permits a bounded local read used to understand and bind the mission. It does not grant general filesystem authority.

The current real mutation boundary is restricted to the exact reviewed and approved `NODAL_HANDOFF.md` candidate. Create/update, verification and rollback remain bound to the selected workspace fingerprint, target and reviewed hashes.

## Provider and network access

Network access occurs only when the operator configures and tests an allowed model route or when another separately authorized capability requires it.

- local OpenAI-compatible routes are restricted to loopback;
- cloud routes require HTTPS and explicit cloud permission;
- automatic fallback remains inside the persisted privacy, capability and budget policy;
- cancellation stops the fallback chain;
- connection testing does not authorize mission execution.

## Logs, evidence and support material

Secrets, raw provider responses, absolute private paths and credential-like values must not enter logs, timeline, evidence, handoff or support reports. Any diagnostic shared outside the device must be reviewed for local usernames, customer content and private repository material.

The `/settings/diagnostics` surface is disabled by default. When the operator enables it, NODAL OS stores a bounded local JSONL file below `%LOCALAPPDATA%\NodalOS\diagnostics` containing only UTC time, an allowlisted event kind/outcome, technical exception type, packaged mode and product version. Retention is limited to 200 events and 128 KB. Exception messages, stack traces, paths, URLs, query strings, request bodies, prompts, provider responses, workspace content, hostname, IP address and user identifiers are not persisted. The operator can clear the event file or disable future recording from the same local settings surface.

## Uninstall

Package uninstall preserves `%LOCALAPPDATA%\NodalOS` by default to avoid silently destroying local mission state or protected configuration. The supplied uninstall script removes this data only when the operator explicitly passes `-RemoveUserData`.

Before removing user data, export any handoff or evidence needed for continuity. User-data deletion is not automatically reversible.

## Telemetry

The private-beta package does not activate a public telemetry, remote crash-reporting, billing or account service. Local diagnostics are not uploaded automatically; sharing them outside the device remains an explicit human action. Any future remote telemetry must be independently designed, opt-in, redacted, disclosed and validated before activation.
