# Evidence Index

## Manual Test Evidence

| Evidence | Current E5 Count | Purpose |
| --- | ---: | --- |
| Product Ledger Safety focused tests | 281/281 PASS | Broad Product Ledger local/dev safety evidence. |
| Product Ledger Recipes focused tests | 72/72 PASS | Recipe-facing Product Ledger evidence. |
| `ProductLedgerLocalDevCanonGuardTests` | 6/6 PASS | Canon drift and overclaim guard. |
| `TestCategory=NodalOsTier1Safety` | 133/133 PASS | Manual Tier 1 safety slice. |
| `TestCategory=ProductLedger` | 75/75 PASS | Product Ledger labeled Safety tests. |
| `TestCategory=NoRuntimeWiring` | 107/107 PASS | No-runtime-wiring evidence. |
| `TestCategory=NoAuthority` | 69/69 PASS | No-authority evidence. |
| `TestCategory=NoDoubleTruth` | 69/69 PASS | No-double-truth evidence. |
| `TestCategory=PublicProductBlock` | 52/52 PASS | Public/product blocker evidence. |
| `TestCategory=ProductionRouteBlock` | 45/45 PASS | Production route blocker evidence. |
| `TestCategory=ReleaseCommercialBlock` | 39/39 PASS | Release/commercial blocker evidence. |
| `NodalOsStaticGuardCatalogTests` | 9/9 PASS | Static guard catalog evidence. |

## Discovery Evidence

- Safety MSTest discovery: 6475 tests.
- Recipes MSTest discovery: 1580 tests.

## Blocked Capabilities To Verify

- Latest pointer.
- Read precedence.
- Product authority.
- Runtime/product enablement.
- DB/cloud/network/provider.
- KMS/WORM/external durable trust.
- Command/shell/subprocess execution.
- Browser/CDP/WCU/OCR/Recipes live automation.
- UI product actions.
- CI enforcement.
- Release/commercial/public readiness.
