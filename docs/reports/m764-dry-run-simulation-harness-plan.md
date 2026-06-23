# M764 Dry-Run Simulation Harness Plan

Project: NODAL OS

M764 defines a planning-only dry-run simulation harness. It simulates metadata-only capability behavior and never performs real runtime, provider, filesystem, browser, release, Store, product, Bridge, or CSP actions.

## Status

- Dry-run simulation harness plan: READY.
- Simulation mode: PLANNING_ONLY / CONTRACT_READY.
- Productive runtime execution: DISABLED.
- Provider/cloud live calls: DISABLED.
- Filesystem write/unlock: DISABLED.
- Browser automation unlock: DISABLED.
- Capability unlock: DISABLED.
- Public release: NO-GO.
- Chrome Web Store: NO-GO.

## Harness Scope

The harness may simulate fake provider responses, fake local model responses, fake filesystem read metadata, fake bridge events, fake WebSocket events, fake ledger append events, fake timeline/report events, fake policy gate decisions, fake manual approval decisions, and fake redaction proofs.

## Hard Boundary

The harness cannot execute real calls, use credentials, write files, automate browsers, unlock capabilities, publish, submit to Store, create signed ZIPs, modify product files, or modify Bridge/CSP.
