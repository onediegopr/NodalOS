# NODAL OS Branch Governance

Last updated: 2026-07-15

## Canonical branch

- `main` is the canonical product and integration branch.
- Feature, security, audit and maintenance work use short-lived branches and pull requests into `main`.
- `chrome-lab-001-extension-local-ai-bridge` is a compatibility/lab branch, not the product integration branch.
- `wip/hito-004b-target-window-selection` is historical and must not remain the repository default.

## Actual remote state found by the total technical audit

Repository: `onediegopr/NodalOS`.

Observed before audit closeout:

- GitHub default branch: `wip/hito-004b-target-window-selection`.
- `main` was 1,007 commits ahead and 0 behind that default branch.
- `chrome-lab-001-extension-local-ai-bridge` was behind `main`.
- The repository is public.
- No published GitHub release was identified.

This is a high-severity governance defect because cloning, browsing and external integrations begin from stale history even though active development is merged into `main`.

Decision: `BLOCKED_EXTERNAL_GITHUB_REMOTE_SETTINGS`.

The connected GitHub integration can inspect repository metadata and update branches, files and pull requests, but it does not expose repository-default-branch or branch-protection mutations. The older local CLI check `gh auth status` was also unauthenticated. The remaining setting change is therefore an external repository-administration action, not a source-code blocker.

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

Remote governance requirements:

- `main` is the default branch.
- Pull requests are required before merge.
- Force push is blocked.
- Branch deletion is blocked.
- Required checks use real workflow job names; invented or retired contexts are not allowed.
- Required conversations are resolved.
- A successful local/dev or lab validation does not create production, release or commercial authority.
