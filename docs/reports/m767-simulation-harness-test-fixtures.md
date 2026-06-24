# M767 Simulation Harness Test Fixtures

M767 adds simulated-only fixtures for the NODAL OS dry-run harness.

The fixtures are fake metadata inputs. They do not call providers, use credentials, write to the filesystem, drive a browser, unlock capabilities, publish releases, submit to Chrome Web Store, modify product files, or modify Bridge/CSP.

## Fixture Status

- Fixture mode: `SIMULATED_FAKE_ONLY`.
- Simulation fixtures: READY.
- Real execution: disabled.
- Live calls: disabled.
- Filesystem writes: disabled.
- Browser actions: disabled.
- Capability unlock: disabled.

## Fixtures

- Fake provider response.
- Fake local model response.
- Fake filesystem read metadata.
- Fake extension bridge event.
- Fake WebSocket bridge event.
- Fake evidence ledger event.
- Fake policy/manual approval denial.
- Fake redaction payload.

## Redaction Payload

The redaction payload uses synthetic placeholders only and covers the nine forbidden fields: secrets, credentials, tokens, cookies, raw user data, raw logs, provider keys, private keys, and browser session data.
