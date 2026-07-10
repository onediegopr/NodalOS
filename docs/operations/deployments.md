# NODAL OS Deployment Inventory

Last updated: 2026-07-10

## Current posture

No production deployment, public product runtime, release channel or commercial environment is authorized by this repository state.

## Environments

| Environment | Source | Host / binding | Purpose | Data class | Authentication | Status | Rollback |
|---|---|---|---|---|---|---|---|
| ChromeLab local bridge | `src/OneBrain.ChromeLab.Bridge` | `127.0.0.1:8787` by default | Local extension bridge and controlled lab runtime | Local operator/runtime metadata | Connection token; protected HTTP endpoints require `X-Nodal-Bridge-Token`; WebSocket requires authenticated hello | Local/dev only | Stop process; remove local config only with operator approval |
| ChromeLab LAN candidate | same | Explicit `--allow-lan` plus private address | Bounded lab connectivity | Local lab metadata | Token required; origin and private-address checks | Default-off / NO-GO for productive use | Disable `AllowLan`; bind loopback |
| ChromeLab stealth channel | same | `/ws/stealth` | Optional protected runner channel | Synthetic/lab task data | `StealthEnabled=true` and token required | Default-off | Disable `StealthEnabled`; disconnect runner |
| Test-owned static target | `apps/nexa-test-owned-target` | Repository recommendation: `lab.nodalos.com.ar` | Synthetic read-only external proof target | Synthetic pages only | No credentials, cookies, analytics or mutations | Deployment not verified | Remove Vercel deployment/domain binding |
| Legacy test host | historical | `nexalab.nodalos.com.ar` | Previous lab host | None authorized | N/A | Deactivated; must not be reused | N/A |
| Production | none | none | Product runtime | Not authorized | Not defined | NO-GO | N/A |

## Required local security configuration

| Setting | Default | Purpose |
|---|---|---|
| `NODAL_OS_CHROME_BRIDGE_TOKEN` | generated local token | Canonical bridge authentication token |
| `NODAL_OS_CHROME_EXTENSION_IDS` | empty | Optional comma/semicolon-separated Chrome extension ID allowlist |
| `NODAL_OS_ENABLE_LOCAL_PAIRING` | `false` | Explicitly enables the loopback token-pairing endpoint |
| `OPENAI_API_KEY` | absent | Optional provider credential; must stay outside repo and responses |
| `AllowLan` | `false` | Explicit LAN opt-in; does not remove authentication requirements |
| `StealthEnabled` | `false` | Explicit stealth-channel opt-in |

## Deployment rules

- Loopback is the only default bridge binding.
- LAN and stealth are independent explicit opt-ins.
- Control and diagnostic HTTP endpoints require the bridge token.
- The extension WebSocket must authenticate with `extension.hello` before operational messages.
- Public health/config surfaces remain minimal and non-authoritative.
- Local pairing is disabled unless the operator explicitly enables it.
- No deployment proves product authority, release readiness, commercial readiness or completion of deferred external-proof gates.
- Secrets, connection tokens, customer data and raw credentials must not be committed or uploaded as workflow artifacts.

## Release prerequisites

1. Canonical default branch aligned with the active integration history.
2. Required Tier 1 build/test checks passing.
3. Secret scan and dependency review enabled.
4. Signed, reproducible installer/update path.
5. Explicit product/release authority decision.
6. Verified rollback and incident owner.

## Ownership

Until a separate operations owner is assigned, repository owner `onediegopr` is the accountable operator for local/dev and synthetic lab deployments.
