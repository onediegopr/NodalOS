# NODAL OS - Durable Audit Trail Append-Only Minimal Implementation

## Decision

GO_IMPLEMENTED_LOCAL_TEST_SAFE

## Scope

Implemented the minimal durable append-only audit trail capability after
explicit user GO.

Capability:

```text
DURABLE_AUDIT_TRAIL_APPEND_ONLY_MINIMAL
```

## What Was Implemented

- Isolated core component:
  `src/OneBrain.Core/Approval/DurableAuditTrailAppendOnlyMinimal.cs`
- Local/test-safe append-only JSONL ledger.
- Supported event kind locked to `approval.reviewed`.
- Sequence numbers.
- Previous-hash and event-hash chain.
- Verification of existing ledger before append.
- Refusal to append when existing ledger integrity fails.
- Raw payload rejection.
- Secret-like content rejection.
- Explicit no product action, no network, no DB migration, no command-handler
  and no release/commercial readiness flags.

## Storage Boundary

- Storage is explicit and caller-provided through
  `DurableAuditTrailAppendOnlyMinimalPolicy.StorageRoot`.
- Default policy restricts storage to the local temp boundary unless
  `AllowLocalTestStorageOnly=false` is explicitly chosen by a future approved
  caller.
- No service registration was added.
- No command handler was added.
- No product runtime was opened.
- No DB or migration was added.
- No network/provider call was added.

## Tests

- `tests/OneBrain.Recipes.Tests/DurableAuditTrailAppendOnlyMinimalTests.cs`
- `tests/OneBrain.Safety.Tests/DurableAuditTrailAppendOnlyMinimalSafetyTests.cs`

Validated:

- two-event append with hash chain;
- JSONL persistence without product action or release readiness;
- fail-closed when disabled;
- fail-closed for unexpected event kind;
- fail-closed outside local temp storage boundary by default;
- raw payload rejection;
- secret-like content rejection;
- tamper detection;
- refusal to append after tampering;
- no command/runtime registration surface.

## Validation Commands

```powershell
dotnet test tests/OneBrain.Recipes.Tests/OneBrain.Recipes.Tests.csproj --filter DurableAuditTrailAppendOnlyMinimal --no-restore
dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --filter DurableAuditTrailAppendOnlyMinimal --no-restore
dotnet test tests/OneBrain.Recipes.Tests/OneBrain.Recipes.Tests.csproj --filter DurableAuditTrailAppendOnly --no-restore
dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --filter DurableAuditTrailAppendOnly --no-restore
```

## Non-Goals Preserved

- No external runtime.
- No product action.
- No approval mutation store.
- No writer/policy productive integration.
- No service registration.
- No command handler.
- No DB/migration.
- No cloud/provider network.
- No release/commercial readiness.
- No stash modification.

## Remaining Gates

- Post-implementation external audit is still required before any enablement
  beyond local/test-safe use.
- Product/runtime integration remains blocked until separately approved.
