# M773 Simulated Orchestrator Negative Flow Tests

M773 defines negative simulated orchestrator flow tests for prohibited actions.

All cases return `DENY`, receive `policyGateDecision=DENY`, require ledger projection and evidence envelope records, and preserve no-execution proof flags.

## Negative Cases

- Live provider call.
- Provider credential use.
- Filesystem write.
- Browser action.
- Credential/CAPTCHA/2FA bypass.
- Capability unlock.
- Public release.
- Chrome Web Store submission.
- Signed public ZIP creation.
- Product file modification.
- Bridge/CSP modification.
- Productive enabled request.

No case executes real actions or modifies product, Bridge, or CSP files.
