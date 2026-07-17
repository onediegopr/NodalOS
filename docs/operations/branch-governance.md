# NODAL OS Branch Governance

Last updated: 2026-07-17

## Canonical branch

- `main` is the canonical product and integration branch.
- Feature, security, audit and maintenance work use short-lived branches and pull requests into `main`.
- `chrome-lab-001-extension-local-ai-bridge` is a compatibility/lab branch, not the product integration branch.
- `wip/hito-004b-target-window-selection` is historical and must not remain the repository default.

## Current remote state

Repository: `onediegopr/NodalOS`.

Confirmed after the private-beta productization sequence:

- `main` contains the canonical product history;
- GitHub still reports `wip/hito-004b-target-window-selection` as the default branch;
- on 2026-07-17 that historical default ref was fast-forwarded to the then-current `main` commit `ebdf429a215799f74d39300d57ca5ec5909fc58f`;
- that ref alignment is temporary containment only: any later commit on `main` can make the historical default stale again until the repository setting is changed;
- the repository remains public;
- no public production release is authorized;
- issue `#27` is the single canonical tracker for default-branch and protection settings;
- duplicate issue `#5` was closed as duplicate.

The code-history defect was temporarily contained at the recorded commit, but repository governance is not closed until metadata reports `default_branch: main` and protection is verified.

Decision: `BLOCKED_EXTERNAL_GITHUB_REMOTE_SETTINGS_TEMPORARY_REF_ALIGNMENT_ONLY`.

The connected GitHub integration can inspect repository metadata and update refs, files, issues and pull requests, but it does not expose repository-default-branch or branch-protection mutations. The remaining setting change is an external repository-administration action, not a source-code blocker.

## Required merge gates

Every pull request into `main` must pass the checks applicable to its scope:

1. `chromelab-security` — ChromeLab bridge/tests and Safety aggregate build.
2. `secret-scan` — added-line credential-pattern scan.
3. `runtime-integration` — Runtime/Recipes build, runtime tests and Pilot product-loop smoke for source/runtime changes.
4. Scope-specific tests for changed behavior.
5. Review of product/runtime/release boundary claims.

The Runtime suite is intentionally not duplicated inside Tier 1 Safety. `runtime-integration` owns that validation for relevant source/test changes.

## Merge policy

- Prefer squash merge for bounded product, security, audit and maintenance changes.
- Never force-push `main`.
- Do not merge a failing or cancelled applicable check.
- Preserve stealth, browser, approval, evidence, semantic verification and local-first boundaries.
- Do not infer production or release authority from a successful local/dev build.
- Keep compatibility branches fast-forwardable; do not rewrite their history.

## Release policy

A commit on `main` is not a release. Release qualification additionally requires:

- a declared product license;
- reproducible installer/update artifacts;
- explicit release authorization;
- signed artifact strategy and rollback instructions;
- deployment inventory update;
- no unresolved critical/high security finding;
- a release-candidate validation record;
- clean-install validation on a fresh supported Windows environment.

## Required remote administration action

Run with an authenticated repository administrator:

```powershell
gh auth status
gh repo edit onediegopr/NodalOS --default-branch main
```

Then configure a branch ruleset or branch protection for `main` that:

- requires pull requests before merge;
- requires `chromelab-security` and `secret-scan`;
- requires the runtime workflow for source/runtime changes without leaving docs-only pull requests permanently pending;
- requires conversation resolution;
- blocks force pushes;
- blocks branch deletion;
- preserves CODEOWNERS review when available.

A classic branch-protection example for the always-running checks is:

```powershell
gh api --method PUT repos/onediegopr/NodalOS/branches/main/protection `
  --field required_status_checks.strict=true `
  --field required_status_checks.contexts[]=chromelab-security `
  --field required_status_checks.contexts[]=secret-scan `
  --field enforce_admins=true `
  --field required_pull_request_reviews.required_approving_review_count=1 `
  --field required_pull_request_reviews.require_code_owner_reviews=true `
  --field required_conversation_resolution=true `
  --field restrictions=null `
  --field allow_force_pushes=false `
  --field allow_deletions=false
```

## Verification checklist

- repository metadata returns `default_branch: main`;
- a clean clone checks out `main`;
- ordinary direct pushes or merges to `main` without a pull request are rejected;
- a docs-only pull request can satisfy all required checks;
- a source/runtime pull request runs and passes `runtime-integration` before merge;
- force pushes are rejected;
- branch deletion is rejected;
- required checks use real workflow job names, not retired or invented contexts;
- resolved conversations are required;
- no successful local/dev, CI or lab validation is treated as production, release or commercial authority.
