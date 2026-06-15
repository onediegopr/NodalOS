# ADR: Browser Secret Boundary and Profile Consent Hardening M12.5

## Context

The post-M12 architecture audit found no critical or high-risk issues, but it identified four medium-risk gaps that should be closed before Productive Vault and real profile consent work:

- `BrowserSecretAccessPolicyEvaluator.Decide` exposed a public `forcedDecision` parameter.
- secret-related identifiers were not validated for secret-like content.
- `InMemoryTestSecretVault` lived in the runtime assembly and returned the request reference instead of the stored canonical reference.
- profile consent could be marked `Granted` without authority, source, proof, or challenge binding.

M12.5 is a short hardening milestone. It does not implement productive vault storage, DPAPI, Windows Credential Manager, real login, real profile launch, or real credentials.

## Decision

M12.5 hardens the existing M12 contracts without changing the product surface:

- remove productive ability to force an `Allowed` secret decision;
- validate secret IDs, request IDs, run/action/correlation/profile/session IDs, and audit IDs against secret-like content;
- fail closed on unsafe identifiers;
- return only canonical stored references from the synthetic test vault;
- rename the synthetic vault to `InMemoryTestOnlySecretVault`;
- require authoritative profile consent proof for `Granted` decisions.

## Forced Decision Closure

The public secret policy evaluator now exposes:

- `Decide(request, policy)` for normal evaluation;
- `Deny(request)` for explicit deny paths.

There is no public `forcedDecision` parameter and no productive caller can force `Allowed`.

## Safe Identifiers

`BrowserSafeIdentifierValidator` rejects identifiers containing secret-like content or JWT-like tokens. This covers:

- `SecretId`;
- `RequestId`;
- `RunId`;
- `ActionId`;
- `CorrelationId`;
- `ProfileId`;
- `SessionId`;
- audit IDs.

Unsafe requests fail closed. Audit diagnostics redact fields before storing summaries.

## Test Vault Restriction

`InMemoryTestOnlySecretVault` remains a synthetic test helper only:

- it accepts only values prefixed with `synthetic://`;
- it returns only the stored canonical `BrowserSecretReference`;
- it rejects requests with altered metadata;
- it never returns the stored synthetic value.

This is still not a productive vault and must not be wired into production DI.

## Profile Consent Authority

Profile consent now distinguishes:

- requesting actor;
- approving actor;
- approval source;
- authority kind;
- proof reference;
- consent challenge id;
- whether Companion is authoritative.

`Granted` real-profile consent requires:

- trusted authority: `CorePolicy`, `AdminPolicy`, or `TestHarness`;
- non-companion authority;
- approving actor;
- approval source;
- proof ref;
- matching challenge id;
- non-expired request.

`UserViaCompanionIntent` is not sufficient to grant real profile consent. Companion can signal user intent, but Core remains the authority.

## Out Of Scope

- productive vault implementation;
- DPAPI or Windows Credential Manager;
- real user profile launch;
- real credentials;
- login automation;
- cookies from real profiles;
- AFIP, banks, ERP, or authenticated MercadoLibre;
- WebView2 or CEF;
- recorder;
- network capture;
- download/upload manager.

## Tests

M12.5 adds regression coverage for:

- no productive forced `Allowed`;
- unsafe secret-like IDs fail closed;
- normal safe IDs remain valid;
- synthetic vault only accepts `synthetic://`;
- synthetic vault returns canonical references;
- synthetic vault rejects altered metadata;
- companion-only consent does not grant;
- missing proof does not grant;
- unknown authority does not grant;
- wrong challenge does not grant;
- valid core/test authority can grant scoped consent;
- profile consent still does not authorize secret access.

## Percentages

After M12.5:

- Browser Executor CDP read-only core-governed: 95%.
- Browser Runtime Layer productive completeness: 85%.
- ONE BRAIN global: 81%.

## Next

Proceed to M13/M14 only after this hardening is merged:

- Productive Vault / OS-backed Secret Storage Design.
- Consent UI with authoritative Core validation.
- Real profile launch only after explicit consent and audit are implemented.
