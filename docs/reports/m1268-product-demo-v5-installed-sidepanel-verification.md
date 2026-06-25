# Product Demo v5 - Installed sidepanel verification

## 1. Decision

`PRODUCT_DEMO_V5_INSTALLED_SIDEPANEL_VERIFICATION_BLOCKED_BY_CHROME_EXTENSION_LOAD_LIMITATION`

El bloque M1257-M1268 intentó verificar el sidepanel real instalado desde `browser-extension/onebrain-chrome-lab`. La carga instalada no quedó validada porque Chrome no registró la extensión unpacked mediante `--load-extension` en el perfil temporal usado para QA. No se inventa QA instalada.

## 2. Que se verifico en sidepanel real

- Se confirmó que `manifest.json` declara `side_panel.default_path = sidepanel.html`.
- Se confirmó que `service_worker.js` configura `chrome.sidePanel.setPanelBehavior({ openPanelOnActionClick: true })`.
- Se intentó cargar NODAL OS como extensión unpacked desde `browser-extension/onebrain-chrome-lab` en un perfil temporal de Chrome con remote debugging.
- Chrome expuso targets de componentes internos, pero no registró NODAL OS en `Preferences` ni como target de extensión.
- Se intentó abrir `chrome-extension://<id>/sidepanel.html`, pero el `<id>` detectado correspondía a una extensión componente de Chrome, no a NODAL OS.
- Se repitió la prueba con una extensión mínima temporal y tampoco fue registrada por `--load-extension`, lo que apunta a limitación del entorno de Chrome/automation y no a un fallo específico de Mission Control.

## 3. Que se ajusto

No se ajustó UI en este bloque porque no hubo evidencia real del contenedor lateral instalado para justificar cambios de spacing/copy/layout.

Se agregó esta nota técnica para separar claramente:

- Verificación HTML local previa: pasada en M1256.
- Flujo Mission Control vía CDP local previo: pasado en M1256.
- Verificación installed sidepanel real: intentada, bloqueada por carga unpacked no registrada en Chrome.

## 4. Como probarlo

Checklist manual recomendado para Diego:

1. Abrir `chrome://extensions`.
2. Activar Developer mode.
3. Tocar Load unpacked.
4. Seleccionar `C:\DESARROLLO\NodalOS\Codigo-m12-audit\browser-extension\onebrain-chrome-lab`.
5. Abrir NODAL OS desde el icono de extensión para que aparezca el sidepanel.
6. Validar Mission Control.
7. Crear misión.
8. Editar misión.
9. Ejecutar Run demo.
10. Agregar nota al run.
11. Abrir Historial.
12. Copiar script.
13. Copiar resumen.

## 5. Que quedo pendiente

- Ver sidepanel real instalado dentro del contenedor lateral de Chrome.
- Capturar screenshot del sidepanel instalado real.
- Confirmar consola del sidepanel instalado real sin errores críticos.
- Repetir el dry run completo de 60-90 segundos dentro del contenedor lateral instalado.

## 6. Validaciones

Resultados del bloque:

- `dotnet build .\OneBrain.slnx --no-restore`: PASS con warnings legacy existentes.
- `node --check browser-extension/onebrain-chrome-lab/sidepanel.js`: PASS.
- `dotnet test .\tests\OneBrain.Safety.Tests\OneBrain.Safety.Tests.csproj --no-build --filter "TestCategory=M1161M1172"`: PASS.
- `git diff --check`: PASS con aviso LF/CRLF del test modificado.
- secret scan simple: PASS.
- bad UX wording scan simple en Mission Control principal: PASS.

QA de Chrome ejecutada:

- Intento de carga unpacked de NODAL OS con `--load-extension`: bloqueado, extensión no registrada.
- Intento de navegación a `chrome-extension://.../sidepanel.html`: no válido porque el id detectado no era NODAL OS.
- Prueba con extensión mínima temporal: también no registrada por `--load-extension`.

## 7. Archivos modificados

- `docs/reports/m1268-product-demo-v5-installed-sidepanel-verification.md`
- `tests/OneBrain.Safety.Tests/NodalOsProductVisibleLocalDemoM1161M1172Tests.cs`

## 8. Proximo paso

`M1269-M1280 - Installed Sidepanel Manual Load Verification`

Objetivo: cargar NODAL OS manualmente desde `chrome://extensions`, abrir el sidepanel real, ejecutar el dry run grabable y ajustar sólo si el contenedor lateral real muestra problemas visuales.
