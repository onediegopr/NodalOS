# M1288B - Native Browser Skills product implementation

## Decisión

NATIVE_BROWSER_SKILLS_PRODUCT_IMPLEMENTATION_READY

## Qué se implementó realmente

- Browser Skills visible dentro de Mission Control.
- Botón `Capturar pestaña` con lectura de pestaña activa vía `chrome.tabs.query`.
- Botón `Indexar página` con lectura DOM vía `chrome.scripting.executeScript`.
- BrowserStateSnapshot local con `id`, `url`, `title`, `capturedAt`, `source`, `status`, elementos, fricción y acción sugerida.
- Indexación de links, botones, inputs, textareas, selects, headings y forms visibles.
- Detección heurística de fricción: captcha visible, login requerido, acceso restringido, página vacía/error y sesión expirada.
- Browser Evidence visible con URL/título/fecha/fuente/fricción/acción sugerida.
- Historial local de snapshots en `localStorage` bajo `nodal-os.browserSkills.snapshots.v1`.
- Selección y limpieza simple del historial.
- Copia de resumen Browser Skill.
- Integración con misión activa: snapshot asociado a misión, evento agregado al timeline del run seleccionado y evidencia resumida en logs.
- Representación de producto para captcha/proxy/stealth/sesión sin bypass ni runtime externo.

## Qué se ve en producto

En Mission Control aparece una sección `Browser Skills` con:

- Capturar pestaña.
- Indexar página.
- Estado de captura.
- URL y título capturados.
- Conteo de elementos.
- Fricción detectada.
- Lista compacta de elementos indexados.
- Panel Browser Evidence.
- Historial de snapshots.
- Copiar resumen Browser Skill.

La card previa de Modo avanzado ya no vende la feature como `planned`; ahora apunta a la superficie real de Mission Control y aclara que BrowserAct no se usa en runtime.

## APIs usadas

- `chrome.tabs.query` para encontrar la pestaña activa.
- `chrome.scripting.executeScript` para ejecutar una función de lectura DOM en la pestaña activa.
- `localStorage` para historial local de snapshots.

El manifest ya tenía `activeTab`, `tabs`, `scripting`, `sidePanel`, `host_permissions` para `http/https` y content script. No se modificó manifest, service worker, content script, Bridge ni CSP.

## Qué no pudo implementarse y por qué

- En `file://sidepanel.html` local no existen `chrome.tabs` ni `chrome.scripting`; la UI muestra `No pude leer la pestaña desde este contexto.` y registra `NOT_IMPLEMENTED_BLOCKED_BY_CURRENT_EXTENSION_CAPABILITIES`.
- La captura real de URL/DOM requiere abrir NODAL OS como extensión instalada en Chrome sobre una página `http/https`.
- No se verificó el sidepanel instalado en este bloque; queda para el hito retomado de verificación instalada.

## Qué no se agregó

- No se agregó dependencia BrowserAct.
- No se agregó runtime BrowserAct.
- No se agregó CAPTCHA solver.
- No se agregó stealth evasivo.
- No se agregó proxy global.
- No se tocó provider/cloud.
- No se tocó runtime real.
- No se tocó PC Commander real.
- No se tocó release/store.
- No se agregaron artifacts o governance packs.

## Cómo probarlo

1. Cargar o recargar la extensión desde `browser-extension/onebrain-chrome-lab`.
2. Abrir el sidepanel de NODAL OS.
3. Ir a Mission Control.
4. En `Browser Skills`, tocar `Capturar pestaña`.
5. Tocar `Indexar página` sobre una pestaña `http/https`.
6. Revisar elementos indexados, Browser Evidence e historial.
7. Tocar `Copiar resumen Browser Skill`.
8. Si hay misión activa con run, revisar timeline/logs para ver el snapshot asociado.

## Validaciones

- `dotnet build .\OneBrain.slnx --no-restore`: PASS con warnings preexistentes de .NET preview/legacy OCR.
- `node --check browser-extension\onebrain-chrome-lab\sidepanel.js`: PASS.
- `dotnet test .\tests\OneBrain.Safety.Tests\OneBrain.Safety.Tests.csproj --no-build --filter "TestCategory=NativeBrowserSkillsProduct"`: PASS, 6/6.
- `dotnet test .\tests\OneBrain.Safety.Tests\OneBrain.Safety.Tests.csproj --no-build --filter "TestCategory=M1269A"`: PASS, 8/8.
- `dotnet test .\tests\OneBrain.Safety.Tests\OneBrain.Safety.Tests.csproj --no-build --filter "TestCategory=M1161M1172"`: PASS, 17/17.
- Chrome DevTools local HTML QA: Browser Skills visible, botones responden, estado honesto sin `chrome.tabs`, consola limpia.
- `git diff --check`: PASS.
- Secret scan sobre archivos modificados: PASS.
- BrowserAct dependency scan sobre project/package files: PASS.
- Browser Skills UX wording scan: PASS.

## Próximo paso

Retomar M1269-M1280: Installed Sidepanel Manual Verification + Final Demo Polish.
