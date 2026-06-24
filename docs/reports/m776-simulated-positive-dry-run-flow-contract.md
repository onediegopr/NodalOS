# M776 Simulated Positive Dry-Run Flow Contract

M776 defines positive simulated dry-run flows for eligible future capabilities only.

The allow decision is `ALLOW_SIMULATED_DRY_RUN`. It is not a real execution allowance and never enables provider/cloud live calls, filesystem writes, browser automation, capability unlock, public release, Chrome Web Store submission, signed public ZIP creation, product file changes, or Bridge/CSP changes.

## Positive Cases

- Simulated local provider/model response.
- Simulated filesystem read metadata.
- Simulated extension bridge event.
- Simulated WebSocket bridge event.
- Simulated evidence ledger append projection.
- Simulated timeline/reporting projection.
- Simulated policy gate allow.
- Simulated manual approval allow.
- Simulated redaction proof creation.
