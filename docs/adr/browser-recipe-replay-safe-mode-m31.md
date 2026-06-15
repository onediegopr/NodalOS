# ADR: Browser Recipe Replay Safe Mode M31

## Status

Accepted for M31.

## Decision

Replay is limited to `SafeModeReadOnly`. Productive, sensitive, credential, irreversible, and submit replay modes fail closed.

Each step requires compatible recipe version, passing gate, live target, idempotency scope, read-only action, verification rule, semantic proof, and evidence refs. Duplicate step replay is blocked by idempotency key.

## Out of Scope

Productive replay, submit, login, upload, payment, delete, credential replay, critical sites, request/response bodies, sensitive headers, and Companion authority.

