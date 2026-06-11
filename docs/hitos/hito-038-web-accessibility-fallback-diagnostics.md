# HITO-038 — Web Accessibility Fallback Diagnostics

**Status**: Cerrado (diagnostics completados)  
**Branch**: `hito-038-039-web-accessibility-noncommercial-click`  
**HITO-039**: Deferred  

---

## Qué se intentó

### 1. Child HWND UIA probing (WebTargetResolver)

- Se enumeraron HWNDs top-level + child HWNDs (EnumChildWindows recursivo, depth=3)
- Para cada HWND: `UIA3Automation.FromHandle(hwnd)` → `FindAllDescendants(max=500)`
- Se buscó target `"Abrir información"` en fixture local y `"More information..."` en example.com

### 2. RenderWidgetHostHWND directo

- Se identificaron child HWNDs de clase `Chrome_RenderWidgetHostHWND`
- Se aplicó UIA `FromHandle` directamente sobre ellos (OBJID_CLIENT + OBJID_WINDOW)
- Se verificó que `UiaRootAvailable=true` en todos los child HWNDs

### 3. MSAA / IAccessible probing

- `AccessibleObjectFromWindow` con OBJID_CLIENT (0xFFFFFFFC) y OBJID_WINDOW (0x00000000)
- `IAccessible.accChildCount` → children → recorrido depth-first
- Límites: MaxNodes=2000, MaxDepth=30
- Roles de interés: ROLE_SYSTEM_LINK, ROLE_SYSTEM_PUSHBUTTON, ROLE_SYSTEM_CLIENT/DOCUMENT

### 4. Fixture web accesible local

- `tools/fixtures/web/accessible-safe-click.html`
- HTML con `aria-label="Abrir información"`, `role="link"`, `tabindex="0"`, `role="region"`
- Abierto en Edge con `--force-renderer-accessibility`

### 5. example.com

- Página externa estándar con link "More information..."
- Abierto en Edge con `--force-renderer-accessibility`

---

## Resultados

### Edge web content — UIA

| Métrica | Resultado |
|---------|-----------|
| HWNDs top-level encontrados | 2-3 (Chrome_WidgetWin_1) |
| HWNDs child (RenderWidgetHostHWND) | 3-5 por ventana |
| UIA descendants en top-level HWND | ~50 (chrome buttons/tabs, NO page content) |
| UIA descendants en RenderWidgetHostHWND | **0** (root available, tree empty) |
| Candidates para "Abrir información" | 0 |
| Candidates para "More information..." | 0 |

### Edge web content — MSAA

| Métrica | Resultado |
|---------|-----------|
| HWNDs sondeados | 6-7 (TOP + CHD) |
| MSAA nodes totales | **0** |
| Links encontrados | 0 |
| Buttons encontrados | 0 |
| Role "document" / "client" encontrado | 0 |

### WhatsApp counterexample (comprobación de API)

| Métrica | Resultado |
|---------|-----------|
| HWND top-level | Chrome_WidgetWin_1 (WhatsApp) |
| UIA descendants top-level | 500 (con 20 buttons) |
| HWND child (RenderWidgetHostHWND) | Chrome_RenderWidgetHostHWND |
| UIA descendants child HWND | **500** (con 24 buttons) |
| Conclusión | **La API/approach funciona cuando la app construye accessibility tree** |

---

## Conclusión

**Edge Chromium no expone web content accessibility (UIA ni MSAA) para páginas web estándar** incluso con `--force-renderer-accessibility`. El flag no es suficiente para que el proceso renderer construya el árbol de accesibilidad.

El counterexample de WhatsApp Electron confirma que:
1. La enumeración child-HWND es correcta
2. `UIA3Automation.FromHandle(RenderWidgetHostHWND)` funciona
3. `FindAllDescendants()` encuentra page content cuando el árbol existe
4. WhatsApp (Electron con ARIA nativa) sí expone 500+ elementos

---

## Lo que NO se hizo

| Restricción | Cumplida |
|------------|----------|
| 0 clicks web ejecutados | Sí |
| 0 JS injection | Sí |
| 0 DevTools | Sí |
| 0 Selenium / WebDriver | Sí |
| 0 OCR | Sí |
| 0 coordenadas inventadas | Sí |
| 0 Mercado Libre clicks | Sí |
| 0 ecommerce clicks | Sí |
| 0 login | Sí |
| 0 carrito | Sí |
| 0 compra | Sí |
| 0 pago | Sí |

---

## Próximo bloque recomendado

**HITO-039+040 — Browser Accessibility Strategy + Firefox/Alternative Non-Commercial Safe Click Spike**

Propuesta:
1. Probar Firefox (Gecko) — expone MSAA/UIA por defecto con `--enable-caret-browsing`
2. Evaluar si Electron-based apps con ARIA (WhatsApp, VS Code, Slack) son targets válidos para non-commercial safe click
3. Considerar engine-level accessibility hooks post-Deferred

---

## Archivos en este hito

| Archivo | Tipo |
|--------|------|
| `src/OneBrain.Cli/Safety/WebTargetResolver.cs` | Modificado: child-HWND + UIA diagnostics |
| `src/OneBrain.Cli/Accessibility/MsaaAccessibleReader.cs` | Nuevo: IAccessible COM probing |
| `src/OneBrain.Cli/Recipes/RecipeRunner.cs` | Modificado: +diagnose.msaa handler |
| `tools/fixtures/web/accessible-safe-click.html` | Nuevo: fixture con ARIA |
| `tools/recipes/msaa-web-fixture-diagnostics.json` | Nuevo: MSAA fixture diagnosis |
| `tools/recipes/msaa-example-diagnostics.json` | Nuevo: MSAA example.com diagnosis |
| `tools/recipes/web-fixture-safe-click-report.json` | Nuevo: web fixture safe.click (diagnostic) |
| `docs/hitos/hito-038-web-accessibility-fallback-diagnostics.md` | Nuevo: este documento |
