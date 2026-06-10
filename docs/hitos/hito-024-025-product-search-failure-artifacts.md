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

Cuando un step falla con `stopOnFirstFailure`, el runner:
1. Crea directorio: `artifacts/failures/{ts}-{recipe}-{step}/`
2. Guarda `failure.json` con metadata (step index, kind, error, timestamp, sessions)
3. Intenta captura screenshot: primero vía session owned HWND si existe, luego fullscreen fallback. **Best-effort post-mortem: puede fallar si no hay escritorio accesible.**
4. Guarda `snapshot.txt` con variables `.text`/`.title` disponibles
5. Si hay sesiones owned abiertas, intenta cerrarlas y guarda `{ts}-cleanup.json`

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

- Screenshot de falla es best-effort post-mortem: intenta capturar via session HWND first, luego fullscreen. Puede fallar si el escritorio no es accesible.
- Cleanup cierra todas las sesiones owned, no selectivamente.
- Template validation es estática y prefix-aware (detecta prefijos configurados como conocidos). No es order-aware estricto; variables dinámicas sin prefijo conocido pueden generar falsos positivos.
- Template warnings aparecen en dry-run como advertencia no bloqueante (v1).
- Artifacts locales en `artifacts/failures/` están gitignored.
- DuckDuckGo Lite puede cambiar su layout; si la lectura UIA falla, la receta falla controlada.

## 8. Próximo bloque: HITO-026+027 Real Cleanup + Failure Report Pack
