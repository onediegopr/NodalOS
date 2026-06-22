# M633 — Screenshot / Console QA Evidence Pack

**Milestone:** M633
**Decision:** `SCREENSHOT_CONSOLE_QA_REQUIRES_USER_ACTION`
**Branch:** `chrome-lab-001-extension-local-ai-bridge`
**Commit:** `4cc0f026016a2ed9cc7f516f5cc16dd1ad043227`
**Date:** 2026-06-22
**Depends on:** M632 Claude Audit (`AUDIT_CONDITIONAL_GO`)

---

## Estado

M633 fue ejecutado en modo **evidence-documentation only**. No se modificó ningún product file. No se capturaron screenshots ni console logs (requiere acción manual del usuario con Chrome instalado).

**Decision: `M633 BLOQUEADO / SCREENSHOT_CONSOLE_QA_REQUIRES_USER_ACTION`**

---

## Artifacts creados

| Artifact | Estado |
|---|---|
| `artifacts/agent-operations/m633/screenshot-console-qa-evidence-pack.json` | Creado |
| `artifacts/agent-operations/m633/manual-screenshot-console-qa-result.json` | Creado — evidencia pendiente |
| `artifacts/agent-operations/m633/console-error-inventory.json` | Creado — inventario pendiente |
| `artifacts/agent-operations/m633/qa-evidence-screenshot-index.json` | Creado — screenshots pendientes |
| `artifacts/agent-operations/m633/post-m632-go-no-go.json` | Creado |

---

## Screenshots requeridos

1. `operate-tab.png` — Sidepanel tab Operar en estado idle
2. `runtime-tab.png` — Sidepanel tab Runtime desconectado
3. `handoff-surface.png` — Handoff surface (opcional si no hay bridge)
4. `console-initial-load.png` — DevTools Console en carga inicial
5. `console-connect-disconnect.png` — DevTools Console tras conectar/desconectar (opcional)

---

## QA steps para el usuario

1. Abrir Chrome
2. Ir a `chrome://extensions`
3. Activar modo desarrollador
4. Cargar sin empaquetar desde: `C:\DESARROLLO\NodalOS\Codigo-m12-audit\browser-extension\onebrain-chrome-lab`
5. Click Reload
6. Abrir sidepanel (icono de extensión o barra lateral)
7. Screenshot tab Operar
8. Screenshot tab Runtime (sin conectar)
9. Click derecho en sidepanel > Inspeccionar
10. Screenshot DevTools Console en carga inicial
11. Verificar 0 errores rojos
12. Verificar sin CSP violations
13. Verificar que el nombre "NODAL OS" aparece visible
14. Verificar que "NEXA" no aparece como texto principal visible

---

## Go/No-Go actual

| Área | Estado |
|---|---|
| Product files modificados | NO |
| JS modificado | NO |
| Runtime modificado | NO |
| Screenshot/console QA | PENDIENTE |
| Release público | NO |
| HTML microcopy patch | NO (espera evidencia) |
| SW strings cleanup | NO (espera evidencia) |
| CSP tightening | NO (espera evidencia) |
| JS changes | NO-GO permanente hasta audit JS dedicado |
| Runtime changes | NO-GO permanente hasta audit CDP |
| Provider/cloud | NO-GO |
| Filesystem | NO-GO |

---

## Hallazgos del M632 que siguen vigentes

- **Riesgo medio C1:** QA objetiva ausente (este hito lo resuelve)
- **Riesgo medio C2:** Strings "NEXA" en mensajes de log del SW (resoluble en M635)
- **Riesgo medio C3:** CSP `connect-src` permite cualquier host (resoluble en M636)
- **Riesgo bajo D1:** Dos h2 en inglés (resoluble en M634)
- **Riesgo bajo D2:** Provider placeholder "OpenAI" hardcodeado (resoluble en M634)

---

## Próximo hito recomendado

**M633B** — Usuario captura screenshots y console logs manualmente y actualiza artifacts.

Una vez completado: **M634 HTML Microcopy Patch** (copy/microcopy visible sin tocar IDs ni listeners).
