# Phase A Stabilization Audit Cleanup

Decision target: `GO_PHASE_A_STABILIZATION_AUDIT_CLEANUP_READY`

Scope status: limited cleanup for the SidepanelTokenPatch M615-M618 secret-marker false positive. No feature work, runtime enablement, UI mount, provider/cloud wiring, or broad test hygiene was included.

## Fix Summary

- Replaced the broad textual `sk-` substring guard in the four SidepanelTokenPatch M615-M618 tests with an API-key-shaped regex guard.
- Added minimal proof in each affected test class that `mission-task-*` and `task-*` do not match the API key pattern.
- Added minimal proof in each affected test class that a constructed `sk-...` sample still matches the API key pattern.
- Kept sidepanel product files unchanged.

## False Positive

The previous marker list used a textual `sk-` substring check. That was too broad because benign identifiers such as `mission-task-*` contain the same character sequence across the word boundary in `task-`. The updated guard detects API-key-shaped tokens instead of any incidental `sk-` substring.

Current pattern:

```text
\bsk-[A-Za-z0-9_-]{8,}\b
```

## Validation Evidence

- `dotnet build .\OneBrain.slnx --no-restore`: PASS.
- `dotnet test .\tests\OneBrain.Safety.Tests\OneBrain.Safety.Tests.csproj --no-build`: PASS on retry after the first 240s timeout. Final result: 5839 passed, 37 skipped.
- `dotnet test .\tests\OneBrain.Safety.Tests\OneBrain.Safety.Tests.csproj --no-build --filter "FullyQualifiedName~Evidence"`: PASS, 725 passed.
- `dotnet test .\tests\OneBrain.Safety.Tests\OneBrain.Safety.Tests.csproj --no-build --filter "FullyQualifiedName~Recipe"`: PASS, 155 passed, 1 skipped.
- `dotnet test .\tests\OneBrain.Safety.Tests\OneBrain.Safety.Tests.csproj --no-build --filter "FullyQualifiedName~ChromeLabBridgeTests"`: PASS, 35 passed.
- `dotnet test .\tests\OneBrain.Safety.Tests\OneBrain.Safety.Tests.csproj --no-build --filter "TestCategory=CompanionBridgeBugfix"`: PASS, 4 passed.
- `dotnet test .\tests\OneBrain.Safety.Tests\OneBrain.Safety.Tests.csproj --no-build --filter "TestCategory=DiffPreviewV2ReadOnly"`: PASS, 7 passed.
- `dotnet test .\tests\OneBrain.Recipes.Tests\OneBrain.Recipes.Tests.csproj --no-build`: PASS, 1311 passed.
- `dotnet test .\tests\OneBrain.Recipes.Tests\OneBrain.Recipes.Tests.csproj --no-build --filter "TestCategory=EvidenceIntelligence"`: PASS, 26 passed.
- `npm test` in `stealth-engine`: PASS, delegates to `test:audit-safe`, 29 passed.
- `npm run test:audit-safe` in `stealth-engine`: PASS, 29 passed.
- CloakBrowser/CDP no-extension-default gate: PASS.
- CloakBrowser/CDP minimal-product-surface gate: PASS.
- CloakBrowser/CDP extension-deprecation-hardening gate: PASS.
- CloakBrowser/CDP fork-update-release-pipeline gate: PASS.

## Documented Debt

- `BACKLOG_OPTIONAL_TEST_HYGIENE`: 50 remaining `"s" + "k-"` usages were found outside the M615-M618 fix scope. They were intentionally not migrated because they did not fail this validation block.
- `OneBrain.SemanticState`: tracked state is currently the project file only; build output exists locally after validation. This remains a documented thin/placeholder project surface, not a semantic capability claim.
- Historical warnings remain from build: .NET preview SDK notice, obsolete historical OCR worker diagnostics, two nullable warnings in `NodalOsInstalledExtensionVisualQaCloseoutM631Tests`, and MSTEST0037 in `NodalOsOcrLegacyCleanupM341M343Tests`.
- `stealth-engine/package.json`: benign protected touch status. It was not modified in this cleanup; current script mapping keeps `npm test` audit-safe by default and leaves live tests opt-in.
- Recipe navigation placeholder scan: no blocking placeholder was found in the closed recipe navigation/messaging reports inspected for this cleanup. Older placeholder references remain historical or explicitly non-ready in unrelated reports.

## No-Live / No-Runtime Proof

- Sidepanel product files were not modified.
- Stealth runtime source files were not modified.
- Cloak runtime files were not modified.
- `stealth-engine` test scripts confirm default `npm test` runs `test:audit-safe`; live suites remain under explicit `test:live*` scripts.
- CloakBrowser/CDP gates reported no extension default fallback, no system browser fallback, no UI-triggered CDP live execution, no bridge websocket use from the product surface, and metadata-only evidence.

## Next Recommended Block

Recommended next milestone after this cleanup closes: `EIL_READ_ONLY_UI_MOUNT_AUDIT_SAFE`.

Reason: EIL presenter/viewmodel work is already present, the next step is read-only, and it does not require runtime, live browser/CDP, WCU, OCR, provider, network, or cloud enablement.
