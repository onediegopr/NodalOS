# HITO-024+025 — Product Search Safe Prototype + Failure Artifacts

## 1. Objetivo
Primer paso hacia búsqueda web segura: abrir URL de búsqueda (DuckDuckGo Lite), leer resultados, cerrar navegador. Sin login, sin carrito, sin compra, sin pago. Agregar captura de artifact on-failure.

## 2. Perfiles

| Perfil | URL | Propósito |
|---|---|---|
| `wikipedia-laptop.json` | Wikipedia Laptop | Baseline estable |
| `duckduckgo-lite-laptop.json` | DDG Lite `?q=laptop` | Búsqueda segura sin interacción |

## 3. Receta

`product-search-report.json`: load profile → browser.open session → browser.read title+text → browser.close → assert → note.

## 4. Failure artifacts

Cuando un step falla con `stopOnFirstFailure`, el runner intenta capturar screenshot de la ventana activa en `artifacts/failures/{timestamp}-{recipe}-{step}.png`. Es best-effort: si falla, no bloquea.

## 5. Cleanup on failure

Si la receta se detiene por fallo, el runner intenta cerrar cualquier BrowserSession owned abierta vía `WM_CLOSE`. Best-effort.

## 6. Safety

- Sin login, sin carrito, sin compra, sin pago.
- Sin CAPTCHA bypass.
- Sin stealth.
- `browser.close` garantizado antes de asserts.
- `dry-run` clasifica todo.
- `--approval deny` bloquea sensibles.

## 7. Limitaciones

- DuckDuckGo Lite puede cambiar su layout; si la lectura UIA falla, la receta falla controlada.
- Captura de falla captura ventana activa, no necesariamente la del browser si otra ventana tiene foco.
- Cleanup cierra todas las sesiones owned, no selectivamente.

## 8. Próximo bloque: HITO-026+027 Real Cleanup + Failure Report Pack
