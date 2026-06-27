# WCU Containment Property Audit 004 Inventory

Block: `WCU-CONTAINMENT-PROPERTY-AUDIT-004 — STATIC SCAN HARNESS + PROTECTED BOUNDARY CONSOLIDATION`

Baseline HEAD: `bb612a93474ca3cf0103c64bacbb746ecabde7cd`

## Guard

- Branch: `chrome-lab-001-extension-local-ai-bridge`
- Origin divergence at start: `0/0`
- Worktree at start: clean
- Untracked `.cs` under `src/` or `tests/`: none
- Protected Stealth Core diff: none
- `WCU-031-036` reopened: no
- Sidepanel/hash baseline debt touched: no

## Existing Checks

| Check | Existing coverage | Classification |
| --- | --- | --- |
| no-live/no-action scan | Manual `rg` scans plus WCU tests for gates, containment properties, bridge/handoff, claim drift | `CONSOLIDATE_IN_CATALOG` |
| protected scope scan | Manual git diff path scan in every WCU block | `CONSOLIDATE_IN_CATALOG` |
| bad wording scan | Claim drift and containment tests inspect reports/prompts/docs | `REUSE_AND_EXTEND` |
| secret scan | Manual `rg` scan with synthetic fixture classification | `DOCUMENT_AND_LOCK` |
| JSON validation | `ConvertFrom-Json` plus tests reading current reports | `REUSE_AS_IS` |
| report/claim consistency | `ComputerUseClaimConsistencyCatalog` and `WindowsComputerUseClaimConsistencyDrift` | `REUSE_AS_IS` |
| WCU containment tests | Fixture-safe categories from foundation through claim drift | `REUSE_AS_IS` |

## New Artifacts

- `ComputerUseStaticScanCatalog`
- `WindowsComputerUseStaticScanBoundaryTests`
- `windows-computer-use-static-scan-harness-matrix-v1.md`
- `windows-computer-use-protected-boundary-consolidation-v1.md`
- `static-scan-report.json`
- `static-scan-report.md`
