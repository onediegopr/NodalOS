# NODAL OS Branch Governance

Last updated: 2026-07-10

## Canonical branches

- `main` is the canonical product and integration branch.
- `chrome-lab-001-extension-local-ai-bridge` is a temporary compatibility branch and must remain fast-forward aligned with `main` until repository settings use `main` as the default branch.
- Feature, security and maintenance work must use short-lived branches and pull requests into `main`.

## Required merge gates

Every pull request into `main` must pass:

1. Tier 1 ChromeLab restore/build/test.
2. Safety aggregate build.
3. Pull-request added-line secret scan.
4. Scope-specific tests for changed behavior.
5. Review of product/runtime/release boundary claims.

## Merge policy

- Prefer squash merge for bounded product, security and maintenance changes.
- Never force-push `main`.
- Do not merge a failing or cancelled required check.
- Preserve protected stealth, browser, approval, evidence and local-first boundaries.
- Do not infer production or release authority from a successful local/dev build.

## Release policy

A commit on `main` is not a release by itself. Release qualification additionally requires:

- reproducible installer/update artifacts;
- explicit release authorization;
- signed artifacts and rollback instructions;
- deployment inventory update;
- no unresolved critical/high security finding;
- a release candidate validation record.

## Default-branch transition

Repository settings must select `main` as default and require pull requests plus the Tier 1 checks. Until that setting is applied, both canonical and compatibility branch refs must point to the same commit after each accepted integration.
