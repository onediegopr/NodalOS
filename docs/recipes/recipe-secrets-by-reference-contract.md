# Recipe Secrets by Reference Contract

Phase: 5/9 - Tool Trust Registry + Secrets by Reference.

Recipe Runtime represents secrets by reference only. A secret contract can state that a secret is declared or present by reference, but it cannot store, load, reveal, validate, decrypt, rotate, or fetch the secret value.

## Secret Ref

`RecipeSecretRef` includes:

- secret ref id,
- display alias,
- secret kind,
- scope,
- owning tool/system ref,
- presence status,
- rotation metadata ref,
- last verified summary ref,
- redaction policy ref,
- allowed and blocked usage modes,
- operator-visible summary,
- raw value present flag.

`RawValuePresent=true` is a blocking state.

## Secret Kinds and Scopes

Secret kinds include API keys, OAuth tokens, refresh tokens, usernames, passwords, certificates, private keys, session cookies, client secrets, webhook secrets, fiscal certificates, payment credentials, marketplace credentials, ERP connection secrets, database connection secrets, and unknown secrets.

Secret scopes include user, workspace, mission, recipe, tool, organization, environment ref, external vault ref, and unknown.

## Readiness

Readiness blocks when a required secret ref is missing, a raw value is detected, the scope mismatches, or high-risk credentials lack high/critical risk classification or a human/approval path.

No environment variables, secret files, real vaults, APIs, network calls, or credential storage were added.
