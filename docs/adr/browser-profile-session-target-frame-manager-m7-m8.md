# ADR: Browser Profile/Session and Target/Frame Managers (M7/M8)

## Context

M6 proved the first public read-only site under the core-governed Browser Executor. Before adding more real-world flows, the Browser Runtime Layer needs two structural pieces:

- a Profile/Session Manager;
- a Target/Frame/Tab Manager.

These managers prevent common browser automation failures: using the wrong tab, acting on stale targets, confusing a live CDP connection with a live page, or leaking profile paths/storage into diagnostics.

## M7 Decision: Profile/Session Manager

M7 introduces formal browser profile and session descriptors in the CDP runtime layer:

- `BrowserProfileId`;
- `ManagedBrowserSessionId`;
- `BrowserProfileKind`;
- `BrowserStorageScope`;
- `BrowserProfilePolicy`;
- `BrowserSessionPolicy`;
- `BrowserProfileDescriptor`;
- `BrowserSessionDescriptor`;
- `BrowserProfileManager`;
- `BrowserSessionManager`.

Supported profile kinds:

- `Disposable`;
- `PersistentControlled`;
- `UserProfileWithExplicitConsent`.

The default launcher path remains disposable. The CDP launcher now asks the Profile Manager for a disposable profile instead of constructing the `onebrain-cdp-*` directory directly.

## Profile Rules

- Real user profile is blocked without explicit consent.
- Disposable profile is deleted on close.
- Persistent controlled profile stays under a controlled ONE BRAIN root.
- Session has owner, correlation ID, session ID, profile ID and cleanup policy.
- Closed, expired or disposed sessions are not alive and cannot accept modifying actions.
- Diagnostics redact profile paths and do not expose cookies, tokens, localStorage or IndexedDB.

## M8 Decision: Target/Frame/Tab Manager

M8 introduces target/frame tracking structures:

- `BrowserTargetRegistry`;
- `BrowserTargetManager`;
- `BrowserFrameManager`;
- `BrowserFrameTree`;
- `BrowserFrameContext`;
- `BrowserNavigationEvent`;
- `BrowserPopupPolicy`;
- `BrowserTargetSelectionPolicy`.

Supported target states include:

- `Alive`;
- `Active`;
- `Visible`;
- `UserFacing`;
- `Background`;
- `Detached`;
- `Destroyed`;
- `Stale`;
- `Navigating`;
- `Redirecting`;
- `Popup`;
- `Unknown`.

Supported events include target lifecycle, navigation lifecycle, frame lifecycle, popup/window opening and a placeholder `DownloadStarted` event.

## Target/Frame Rules

- Active tab is not assumed to be the correct target.
- Selection can require explicit target ID and expected host.
- Stale/detached/destroyed targets block modifying actions.
- Detached frames block action and verification context creation.
- Redirect/navigation events advance generation.
- Unknown active/user-facing state is represented as unknown/null, not invented.
- Evidence and verification carry `BrowserTargetContext`, including target and frame IDs.

## Integration

The CDP launcher now creates disposable profiles through `BrowserProfileManager` and creates session identity through `BrowserSessionManager`.

The existing `BrowserTargetContext`, `BrowserEvidence`, and `BrowserVerification` contracts remain the integration boundary for FSM/Safety/Evidence.

M6 live read-only scenario remains unchanged in behavior and still uses temporary profiles.

## Out Of Scope

M7/M8 does not implement:

- real user profile usage;
- login;
- credential vault;
- WebView2 or CEF;
- Download/Upload Manager;
- Network Capture;
- Export Session / Replay;
- Recipe Recorder;
- product UX changes;
- new real sites.

## Risks

- CDP target/frame events are still modeled primarily through manager structures and tests; deeper live CDP event subscription is future work.
- Persistent controlled profiles are modeled and created, but not yet used for authenticated workflows.
- User-facing/visible detection remains explicit/limited until richer browser/window telemetry is added.

## Next Step

The next hito should focus on credential boundary/human handoff or controlled download/network foundations before any state-changing site flows.
