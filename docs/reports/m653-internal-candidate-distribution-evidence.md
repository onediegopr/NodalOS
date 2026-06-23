# M653 Internal Candidate Distribution Evidence

Decision: `M653 CERRADO / INTERNAL_CANDIDATE_DISTRIBUTION_EVIDENCE_READY`

## Scope

M653 documents internal/local-first distribution evidence.

It does not publish, upload to store, enable public release, or activate blocked capabilities.

## Internal Installation Runbook

1. Use only the approved internal candidate branch and commit.
2. Confirm bridge liveness before extension reload.
3. Run `tools/scripts/bridge-liveness-check.ps1`.
4. Confirm TCP `127.0.0.1:8787`, `/health`, `/runtime`, `/debug`, `/config/public`, and WebSocket upgrade.
5. Load or reload the installed extension manually.
6. Open sidepanel.
7. Open Runtime tab.
8. Confirm Health OK, client observed, heartbeat OK, no reconnecting, and no repeated bridge WebSocket error.
9. Inspect service worker DevTools only for summarized evidence; do not copy secrets or long raw logs.

## Disabled Capabilities

The internal candidate does not enable:

- runtime productive execution,
- provider/cloud,
- filesystem,
- browser automation,
- public release,
- capability unlock.

## Known Issues

- Broad host permissions are acceptable only for internal evidence candidate.
- Public release remains blocked.
- Local bridge is required.
- Provider path remains present but disabled.

## Support Notes

Support channel: internal project owner/release reviewer.

Expected support evidence: liveness output, runtime tab summary, service worker DevTools summary, commit hash, and rollback notes.

## Rollback Notes

Rollback means unloading the installed extension, stopping the local bridge, and returning to the previous known-good internal candidate commit.

No store rollback exists because no public release is published.
