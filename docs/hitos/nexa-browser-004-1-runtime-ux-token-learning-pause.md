# HITO NEXA Browser-004.1 - Runtime UX + Token Setup + Learning Pause

## Objetivo

Browser-004.1 corrige problemas de uso real detectados despues de Browser-004:

* el token de conexion se vuelve explicito y persistente;
* Runtime separa estado de conexion, estado de run y tab activa;
* los botones tecnicos se renombran con acciones comprensibles;
* Learning Mode agrega pausa/reanudacion para evitar contaminar recetas.

No trabaja Windows/UIA, no usa CDP/Playwright en producto, no agrega OCR y no hardcodea AFIP/ARCA.

## Config local

La configuracion local principal es:

```text
config/chrome-lab.local.json
```

Formato esperado:

```json
{
  "OpenAiApiKey": "TU_API_KEY_OPENAI",
  "ExtensionToken": "nexa_9f8a3c2e7b4d41b6a90f",
  "Host": "127.0.0.1",
  "Port": 8787,
  "AllowLan": false
}
```

El archivo esta ignorado por git mediante `config/*.local.json`.

Prioridad de API key:

1. `OPENAI_API_KEY`
2. `config/chrome-lab.local.json` / `OpenAiApiKey`
3. `ApiKey.txt`

`ApiKey.txt` sigue funcionando como fallback.

## ExtensionToken

`ExtensionToken` es distinto de la API key de OpenAI.

* Empareja extension Chrome NEXA con bridge local NEXA.
* No se envia a OpenAI.
* No se expone en `/debug`, `/runtime`, `/clients` ni `/config/public`.
* La extension lo guarda en `chrome.storage.local`.
* No se guarda en `chrome.storage.session`.

Si el bridge arranca sin `ExtensionToken`, genera uno fuerte con prefijo `nexa_`, lo guarda en `config/chrome-lab.local.json` y lo muestra en consola una vez para setup inicial.

## Runtime UX

Runtime ahora muestra estados separados:

* `Connection`
* `Run`
* `Tab`

Tambien muestra:

* Bridge OK/FAIL
* Token guardado/faltante/invalido/no requerido
* WebSocket
* Clients
* Heartbeat
* diagnostico humano
* accion recomendada

Cambios de botones:

* `Refresh Debug` -> `Verificar conexion`
* `Reconnect` -> `Reconectar extension`
* nuevo `Limpiar estado local`
* nuevo setup de token `Guardar y conectar`
* avanzado: `Cambiar token` y `Borrar token guardado`

`Limpiar estado local` no borra recetas, drafts de aprendizaje ni token guardado.

## Learning Pause

Learning Mode agrega:

* `Pausar aprendizaje`
* `Reanudar aprendizaje`

Mientras esta pausado:

* no captura clicks;
* no captura inputs;
* no captura selects;
* no captura navegaciones;
* no convierte eventos de pausa/reanudar en pasos ejecutables.

## Smoke manual documentado

Pendiente de ejecucion manual real en Chrome despues de recargar la extension:

1. Config local: borrar/renombrar config local de prueba, arrancar bridge y verificar generacion de `ExtensionToken`.
2. Primera conexion: pegar token una vez, guardar y conectar, verificar clients count 1 y heartbeat OK.
3. Siguiente arranque: recargar extension y verificar que no vuelve a pedir token.
4. Token invalido: pegar token incorrecto y verificar estado `Token invalido` sin loop de reconexion.
5. Verificar conexion: confirmar bridge, clients, WebSocket y ultimo error claro.
6. Learning pause: capturar A, pausar, navegar/clickear, reanudar, capturar B; la receta debe contener A y B, no lo pausado.
7. Limpiar estado local: confirmar que no borra recetas ni token.

## Validacion automatica

Se agregaron locks/tests para:

* parsing de `config/chrome-lab.local.json`;
* fallback `ApiKey.txt`;
* generacion/persistencia de `ExtensionToken`;
* no exposicion de token/API key;
* UX de token y botones claros;
* learning pause/resume sin captura mientras esta pausado.

## Proximo hito recomendado

Browser-005: Candidate Repair UI y seleccion manual asistida de candidatos cuando `resolveTarget` tenga baja confianza o ambiguedad.
