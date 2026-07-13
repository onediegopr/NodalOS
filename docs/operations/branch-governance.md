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

## Remote Governance Status - 2026-07-13

Decision: `BLOCKED_EXTERNAL_GITHUB_REMOTE_SETTINGS`.

Local state:

- Repository: `onediegopr/NodalOS`.
- Local branch for product integration: `main`.
- Local HEAD: `e38c4325e48dbfce24bec971d957c39bbc4071eb`.
- `origin/main`: `e38c4325e48dbfce24bec971d957c39bbc4071eb`.
- `origin/chrome-lab-001-extension-local-ai-bridge`: `e38c4325e48dbfce24bec971d957c39bbc4071eb`.
- `origin/main...origin/chrome-lab-001-extension-local-ai-bridge`: `0 0`.
- `chrome-lab-001-extension-local-ai-bridge` remains preserved as the lab/legacy transition branch.

External blocker:

- `gh auth status` is not authenticated in this environment.
- `GH_TOKEN` and `GITHUB_TOKEN` are not present.
- GitHub default branch and branch-protection settings cannot be inspected or changed from this environment without operator-provided GitHub authentication.
- This blocks remote repository settings only; it does not block local roadmap work on `main`.

Required remote setting when credentials are available:

```powershell
gh auth login
gh repo edit onediegopr/NodalOS --default-branch main
```

Required checks must use actual workflow job names from `.github/workflows/tier1-safety.yml`:

- `chromelab-security`
- `secret-scan`

Suggested branch-protection payload, after confirming the exact repository rules supported by the current GitHub plan:

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
- Required checks are the real Tier 1 workflow jobs above; do not invent check names.
- Required conversations must be resolved.
- CODEOWNERS review remains preserved where GitHub reports it as available.
- A successful local/dev or lab validation does not create production, release or commercial authority.
