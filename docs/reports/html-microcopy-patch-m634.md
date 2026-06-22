# M634 - HTML Microcopy Patch

**Milestone:** M634  
**Decision:** `HTML_MICROCOPY_PATCH_READY`  
**Branch:** `chrome-lab-001-extension-local-ai-bridge`  
**Base commit:** `f00ae28f037b8c605bdc0b813d8c59727121be7d`  
**Date:** 2026-06-22

## Scope

This milestone applied a minimal visible-text-only patch to `browser-extension/onebrain-chrome-lab/sidepanel.html`.

No JavaScript, CSS, manifest, runtime, WebSocket, storage, permission, protocol, port, or alarm behavior was changed.

## Microcopy Changes

| Before | After |
|---|---|
| `Target Resolution` | `Resolución del objetivo` |
| `Verification` | `Verificación` |
| `guardara` | `guardará` |
| `volvera` | `volverá` |
| `invalido` | `inválido` |
| `reemplazalo` | `reemplázalo` |

## Guardrails

- HTML only: yes
- Text only: yes
- DOM structure changed: no
- IDs changed: no
- Classes changed: no
- `data-*` attributes changed: no
- Scripts changed: no
- CSS changed: no
- Manifest changed: no
- Runtime changed: no

## Go / No-Go

- SW visible strings cleanup: GO for next dedicated milestone
- CSP tightening candidate: NO-GO
- JS changes: NO-GO
- Runtime changes: NO-GO
- Public release: NO-GO
- Post-patch manual reload QA: required

## Next Milestone

`M635 SW Visible Strings Cleanup or M634A Manual Reload QA After Microcopy`
