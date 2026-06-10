# HITO-022+023 — Real Web Task Pack + App Profiles v1

## 1. Objetivo
Permitir automatización web real controlada con Edge owned session, lectura robusta por HWND/session, usando perfiles declarativos por app/sitio.

## 2. App/Web Profiles v1

4 perfiles iniciales:
- `tools/profiles/web/example-com.json`
- `tools/profiles/web/wikipedia-automation.json`
- `tools/profiles/apps/notepad.json`
- `tools/profiles/apps/edge.json`

Loader: `ProfileLoader.Load(path)` + `ToVariables(profile, prefix?)`.
Variables guardadas bajo el prefijo (saveAs de la receta), con fallback a `profile.{id}`.

## 3. Contrato JSON

```jsonc
{"id":"...","type":"web|app","url":"...","expected":{"titleContains":"...","textContains":"..."},"read":{"preferredProperty":"text","fallbackProperty":"title"},"safety":{"allowForms":false,...}}
```

## 4. `profile.load`

Step que carga un perfil y expone sus campos como variables de receta.

## 5. Recetas web

| Receta | Descripción |
|---|---|
| web-example-profile-report | Carga perfil, abre session, lee, cierra, assert, report |
| web-wikipedia-profile-report | Igual con Wikipedia |
| web-real-task-pack | Ambos en secuencia |

Flujo: load -> open -> read -> close -> assert -> report.

## 6. Safety flags

**IMPORTANTE:** Los flags `safety.*` del perfil son DECLARATIVOS en v1. Se cargan como variables pero NO modifican ni relajan MinimalSafetyGuard ni ApprovalPolicy. En el futuro los perfiles solo podrán RESTRINGIR permisos, nunca ampliarlos.

## 7. Limitaciones

- UIA text = elementos del arbol UIA (chrome elements), NO HTML completo
- Wikipedia depende de red; puede fallar por timeout
- No OCR, no CDP, no compras/logins
- Notepad typing usa fallback (role:Window) porque UIA no expone target exacto

## 8. Próximo bloque: HITO-024+025 Auto-cleanup + Failure Artifacts
