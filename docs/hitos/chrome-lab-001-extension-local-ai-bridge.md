# CHROME-LAB-001 - Extension + Local AI Bridge + LAN Client

## Objetivo

Este lab agrega un banco de pruebas browser-first separado del motor seguro Windows/UIA.

Topologia:

```text
PC A
  ONE BRAIN Chrome Lab Bridge
  OpenAI API key local
  HTTP/WebSocket :8787

PC B
  Chrome
  ONE BRAIN Chrome Lab extension
  Side panel
  Content script DOM tools
```

El lab no reemplaza `safe.click`, `safe.read` ni `safe.type`.

## Motor local

Proyecto:

```text
src/OneBrain.ChromeLab.Bridge
```

Ejecutar:

```powershell
dotnet run --project src/OneBrain.ChromeLab.Bridge -- --host 0.0.0.0 --port 8787
```

Self-test:

```powershell
dotnet run --project src/OneBrain.ChromeLab.Bridge -- --host 127.0.0.1 --port 8787 --self-test
```

Endpoints:

* `GET /health`
* `GET /config/public`
* `POST /api/runs`
* `POST /api/runs/{runId}/stop`
* `POST /api/runs/{runId}/pause`
* `POST /api/runs/{runId}/resume`
* `WS /ws/extension`

## OpenAI

Provider hardcodeado:

```text
OpenAI
```

Modelo default:

```text
gpt-4.1-mini
```

La API key no se commitea ni se expone a la extension.

Fuentes permitidas:

```text
OPENAI_API_KEY
config/chrome-lab.local.json
```

Placeholder documental:

```text
PUT_YOUR_OPENAI_API_KEY_IN_ENV_OPENAI_API_KEY
```

`config/chrome-lab.local.json` esta ignorado por Git.

Ejemplo local no commiteable:

```json
{
  "openAiApiKey": "PUT_YOUR_OPENAI_API_KEY_IN_ENV_OPENAI_API_KEY"
}
```

Si falta key:

* el bridge levanta;
* `/config/public` reporta `hasApiKey=false`;
* los runs IA devuelven error claro;
* tests y self-test no hacen llamadas reales.

## Extension Chrome

Carpeta:

```text
browser-extension/onebrain-chrome-lab
```

Instalacion:

1. Abrir `chrome://extensions`.
2. Activar Developer mode.
3. Cargar `Load unpacked`.
4. Seleccionar `browser-extension/onebrain-chrome-lab`.

Manifest:

* Manifest V3.
* Side panel.
* Service worker.
* Content script local.
* Sin scripts remotos.
* Sin `eval`.

## Side panel

Incluye:

* estado de conexion;
* IP/puerto del motor;
* Test Health;
* Start Run;
* Pause / Resume;
* STOP grande y sticky;
* estado de pagina actual;
* estado de tool;
* banner de intervencion humana;
* logs;
* elementos observados;
* indicador de no captura de credenciales.

## Protocolo

Version:

```text
chrome-lab-v1
```

Mensajes principales:

* `extension.hello`
* `engine.hello`
* `tool.request`
* `tool.result`
* `run.pause`
* `run.resume`
* `run.stop`
* `run.status`

Cada tool request usa:

* `runId`
* `requestId`
* `tool`
* `args`

## Tools permitidas

* `observePage`
* `getCurrentTab`
* `navigate`
* `query`
* `read`
* `click`
* `setValue`
* `selectOption`
* `scrollIntoView`
* `waitForSelector`
* `highlight`
* `clearHighlight`
* `pauseForHuman`
* `stop`

El bridge valida allowlist, selectores razonables y URLs `http/https`.

## Guardas

STOP:

* limpia cola local;
* avisa al bridge;
* bloquea tool calls pendientes.

Credenciales:

* no se devuelven valores de password;
* `setValue` bloquea password, token y OTP;
* captcha/2FA/login dispara pausa;
* no hay auto-submit de login sensible;
* el usuario completa manualmente y luego presiona Resume.

Paginas restringidas:

* `chrome://`
* `edge://`
* `extension://`

URLs bloqueadas:

* `javascript:`
* `data:`
* `file:`
* esquemas no `http/https`

## LAN

Bridge:

```powershell
dotnet run --project src/OneBrain.ChromeLab.Bridge -- --host 0.0.0.0 --port 8787
```

Extension:

* default `127.0.0.1`;
* para PC B usar la IP LAN de PC A, por ejemplo `192.168.1.50`;
* probar con `Test Health`;
* abrir firewall Windows puerto TCP `8787` si hace falta;
* no exponer el puerto a internet.

## Demo AFIP

Instruccion sugerida:

```text
Entrar a https://www.afip.gob.ar/ y avanzar hasta el punto donde se requieran credenciales. Cuando aparezca login, clave fiscal, CUIT, captcha o 2FA, pausar e indicarme que complete manualmente. Luego, al presionar Resume, continuar observando.
```

Comportamiento esperado:

* puede navegar a paginas publicas;
* puede observar links/botones;
* ante login, clave fiscal, CUIT, captcha o 2FA, pausa;
* no escribe credenciales;
* no guarda credenciales;
* no saltea captcha.

## Limites conocidos

* El loop IA completo queda minimo: el bridge inicia runs y puede pedir observacion a la extension.
* No hay Native Messaging.
* No hay CDP/Playwright.
* No hay OCR/VLM.
* No hay memoria/RAG.
* No hay recorder completo.

## Validacion

* `dotnet build OneBrain.slnx`
* `dotnet test OneBrain.slnx`
* `dotnet run --project src/OneBrain.ChromeLab.Bridge -- --host 127.0.0.1 --port 8787 --self-test`
* secret scan del diff

## Proximo paso

CHROME-LAB-002:

* instalar extension unpacked;
* correr motor en PC A;
* conectar desde PC B por IP;
* probar navegacion publica;
* probar pausa ante AFIP/login;
* ajustar UI/UX segun comportamiento real.
