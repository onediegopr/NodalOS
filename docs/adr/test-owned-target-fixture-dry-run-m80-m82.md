# M80-M82 Test-Owned Target Fixture And Dry-Run Binding

## Status

Accepted as local synthetic preparation only.

## Context

M77-M79 defined the external test-owned target contract, opt-in proof harness, and read-only evidence pack. NEXA still has no real external proof because no approved external target has been executed.

## Decision

M80 adds a synthetic test-owned target fixture. It is approved by the same M77 read-only contract but uses a fixture/dev host and explicit synthetic ownership. Fixture approval is not external/live proof.

M81 adds a synthetic scenario catalog covering read-only landing, product list, document, and table/report pages, plus blocked disabled form, login, checkout/payment, and destructive action pages. All data is synthetic and local/model-only.

M82 binds fixture target, scenarios, proof harness, and evidence pack in dry-run mode. It does not execute network traffic and never closes M51/M65. Readiness may show fixture/dry-run preparation but external/live remains not validated.

## Non-goals

- No internet navigation.
- No third-party sites.
- No sensitive sites.
- No credentials.
- No submit/pay/sign/delete.
- No public SaaS.
- No M51/M65 closure.

## Consequences

The external proof pipeline can now be tested locally end-to-end as dry-run evidence. The next real step remains provisioning and validating a real test-owned external target through opt-in live proof.
