# ADR: Browser Runtime Generic Layer Plan (M8.5)

## Context

`BrowserProfileManager`, `BrowserSessionManager`, `BrowserTargetManager`, and `BrowserFrameManager` currently live in `OneBrain.BrowserExecutor.Cdp`.

Conceptually these are browser-runtime abstractions, not CDP-only abstractions. They will also matter for future WebView2, CEF, extension-companion, and hybrid runtimes.

## Decision

Do not move the managers during M8.5. A namespace/project migration now would be a refactor with little immediate runtime value and unnecessary risk.

Document the future target instead:

- `OneBrain.BrowserExecutor.Runtime`; or
- `OneBrain.BrowserRuntime.Core`.

The future generic runtime layer should own:

- profile/session descriptors and policies;
- target/tab/frame registries;
- target selection policy;
- liveness/event model;
- evidence/verification context binding;
- capability/risk declarations shared by runtimes.

## Compatibility Requirements

Future WebView2/CEF runtimes must reuse equivalent contracts and must not create mini-engines or bypass core FSM/Safety/Evidence.

No runtime may:

- be the authoritative brain outside Core/FSM;
- mark success without verification;
- treat socket/process liveness as target liveness;
- use a real user profile without explicit consent;
- log cookies, tokens, localStorage, IndexedDB, or profile contents.

## Migration Plan

1. Keep current CDP implementations stable.
2. Add adapters for new runtime interfaces once Credential Boundary / Human Handoff is formalized.
3. Move managers into the generic project in a test-driven migration.
4. Keep compatibility shims in `.Cdp` until downstream code is migrated.

## Out Of Scope

M8.5 does not implement WebView2, CEF, download/upload management, network capture, recorder, or real profile consent UI.
