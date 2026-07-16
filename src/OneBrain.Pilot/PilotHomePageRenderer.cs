using System.Net;
using System.Text;
using OneBrain.Core.AI;
using OneBrain.Core.AppProfiles;
using OneBrain.Core.Approval;
using OneBrain.Core.Confidence;
using OneBrain.Core.ExecutorHarness;
using OneBrain.Core.Flows;
using OneBrain.Core.History;
using OneBrain.Core.Memory;
using OneBrain.Core.Recording;
using OneBrain.Core.Recipes.Editing;

namespace OneBrain.Pilot;

public static class PilotHomePageRenderer
{
    public static string Render(PilotPlan? plan = null, PilotExecutionResult? result = null)
    {
        var taskValue = plan?.Intent.OriginalText ?? "";
        var recipeLabel = plan?.Intent.Recipe?.Label ?? "Sin tarea seleccionada";
        var status = result?.Status ?? (plan?.Intent.IsMatch == true ? "plan_ready" : "idle");
        var latestMarkdown = result?.LatestMarkdownPath ?? "";
        var latestHtml = result?.LatestHtmlPath ?? "";

        return $$"""
<!doctype html>
<html lang="es">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <title>ONE BRAIN Pilot</title>
  <style>
    :root {
      --ink: #17211a;
      --muted: #5c6b60;
      --paper: #f5f1e7;
      --panel: rgba(255, 252, 242, 0.86);
      --line: #d7cdb7;
      --accent: #e66b2d;
      --safe: #226b45;
      --info: #245a87;
      --warn: #9a5a10;
      --approval: #b24f16;
      --disabled: #6c706b;
      --shadow: 0 24px 80px rgba(43, 32, 16, 0.16);
    }
    * { box-sizing: border-box; }
    body {
      margin: 0;
      min-height: 100vh;
      color: var(--ink);
      font-family: "Aptos", "Segoe UI", sans-serif;
      background:
        radial-gradient(circle at 10% 10%, rgba(230, 107, 45, 0.16), transparent 30rem),
        linear-gradient(135deg, #f7f2df 0%, #e8efe4 52%, #dfe6d7 100%);
    }
    main { max-width: 1180px; margin: 0 auto; padding: 42px 24px 56px; }
    .hero {
      display: grid;
      grid-template-columns: 1.15fr 0.85fr;
      gap: 28px;
      align-items: stretch;
    }
    .card {
      background: var(--panel);
      border: 1px solid var(--line);
      box-shadow: var(--shadow);
      border-radius: 28px;
      padding: 28px;
      backdrop-filter: blur(14px);
    }
    .eyebrow {
      color: var(--accent);
      font-size: 12px;
      font-weight: 800;
      letter-spacing: 0.16em;
      text-transform: uppercase;
    }
    h1 {
      margin: 12px 0 14px;
      font-family: Georgia, "Times New Roman", serif;
      font-size: clamp(42px, 7vw, 84px);
      line-height: 0.92;
      letter-spacing: -0.055em;
    }
    h2 { margin: 0 0 14px; font-size: 18px; letter-spacing: -0.02em; }
    p { color: var(--muted); line-height: 1.55; }
    textarea {
      width: 100%;
      min-height: 110px;
      resize: vertical;
      border: 1px solid #cbbf9f;
      border-radius: 20px;
      padding: 18px;
      color: var(--ink);
      background: #fffaf0;
      font: inherit;
      outline: none;
    }
    textarea:focus { border-color: var(--accent); box-shadow: 0 0 0 4px rgba(230, 107, 45, 0.16); }
    .actions { display: flex; flex-wrap: wrap; gap: 12px; margin-top: 16px; }
    button, .button {
      border: 0;
      border-radius: 999px;
      padding: 12px 18px;
      color: #fffaf0;
      background: var(--ink);
      font-weight: 800;
      cursor: pointer;
      text-decoration: none;
    }
    button.secondary { background: #6a755d; }
    button.ghost { color: var(--ink); background: #efe6d4; border: 1px solid var(--line); }
    .quick-grid { display: grid; gap: 12px; }
    .quick-grid form { margin: 0; }
    .quick-grid button { width: 100%; text-align: left; }
    .grid { display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 18px; margin-top: 22px; }
    .full { grid-column: 1 / -1; }
    .badge {
      display: inline-flex;
      align-items: center;
      border-radius: 999px;
      padding: 7px 11px;
      font-size: 12px;
      font-weight: 800;
      background: #e9dec8;
      color: var(--ink);
    }
    .badge.safe { background: #dcebdd; color: var(--safe); }
    .badge.info { background: #dbe8f5; color: var(--info); }
    .badge.warn { background: #f3e2bf; color: var(--warn); }
    .badge.approval { background: #f7dcc8; color: var(--approval); }
    .badge.risk, .badge.blocked { background: #f6e7e6; color: #8a352d; }
    .badge.disabled { background: #e7e8e2; color: var(--disabled); }
    .help-inline {
      display: inline-flex;
      align-items: center;
      gap: 6px;
      margin: 4px 8px 4px 0;
      color: var(--muted);
      font-size: 14px;
    }
    .help-inline .badge { padding: 5px 9px; }
    details.help-text {
      margin-top: 12px;
      border: 1px solid var(--line);
      border-radius: 16px;
      padding: 12px 14px;
      background: rgba(255, 253, 246, 0.86);
    }
    details.help-text summary {
      cursor: pointer;
      font-weight: 800;
      color: var(--ink);
    }
    details.help-text p { margin: 10px 0 0; }
    .notice {
      border-left: 4px solid var(--accent);
      padding: 12px 14px;
      border-radius: 16px;
      background: rgba(239, 230, 212, 0.75);
      color: var(--ink);
    }
    .help-box {
      border: 1px solid var(--line);
      border-left: 5px solid var(--info);
      border-radius: 18px;
      padding: 14px 16px;
      background: rgba(255, 253, 246, 0.92);
      margin: 14px 0;
    }
    .help-box strong { color: var(--ink); }
    .step-flow { display: grid; grid-template-columns: repeat(4, minmax(0, 1fr)); gap: 14px; margin-top: 18px; }
    .step-card {
      border: 1px solid var(--line);
      border-radius: 18px;
      background: rgba(255, 253, 246, .9);
      padding: 16px;
      min-height: 182px;
      display: flex;
      flex-direction: column;
      gap: 10px;
    }
    .step-number {
      width: 34px;
      height: 34px;
      border-radius: 50%;
      display: inline-grid;
      place-items: center;
      color: #fffaf0;
      background: var(--ink);
      font-weight: 900;
    }
    .step-card h3 { margin: 0; font-size: 16px; }
    ol, ul { padding-left: 21px; }
    li { margin: 8px 0; color: var(--muted); }
    code {
      display: block;
      overflow: auto;
      border-radius: 16px;
      padding: 14px;
      background: #211f1a;
      color: #fbf1d5;
      white-space: pre-wrap;
    }
    .metric { display: grid; gap: 4px; padding: 12px 0; border-bottom: 1px solid var(--line); }
    .metric strong { font-size: 22px; }
    .path { word-break: break-all; color: var(--ink); }
    @media (max-width: 820px) {
      .hero, .grid, .step-flow { grid-template-columns: 1fr; }
    }
  </style>
</head>
<body>
  <main>
    {{PilotChrome("Home")}}
    <section class="hero">
      <div class="card">
        <p class="notice"><span class="badge info">Separacion demo/real</span> Proba demo segura cuando no tengas datos reales. Las pantallas que usan ejemplos lo muestran como MODO DEMO / SIMULACION SEGURA.</p>
        <div class="eyebrow">ONE BRAIN Pilot / recorrido local seguro</div>
        <h1>ONE BRAIN Pilot</h1>
        <p>Esta interfaz te deja entender, revisar y probar automatizaciones seguras sin tocar sitios vivos ni ejecutar acciones peligrosas. Pilot primero explica, despues planifica, y solo ejecuta tareas permitidas de demo o reporte.</p>
        <p class="notice">ONE BRAIN no hace clicks, no inicia sesion, no acepta cookies, no compra, no paga y no envia nada sin aprobacion.</p>
        {{HelpBox("Modo seguro visible", "Reglas permanentes: 0 clicks, 0 cookies aceptadas, 0 login, 0 carrito, 0 compra y 0 pago. Si falta informacion o no existe un ejecutor seguro, ONE BRAIN bloquea la accion.")}}
        <form method="post" action="/plan">
          <label for="task"><strong>Describi una tarea</strong></label>
          <textarea id="task" name="task" placeholder="genera html demo">{{Html(taskValue)}}</textarea>
          <div class="actions">
            <button type="submit">Mostrar plan seguro</button>
            <button class="secondary" type="submit" formaction="/run">Ejecutar tarea permitida</button>
            <a class="button ghost" href="/guia">Guiarme paso a paso</a>
          </div>
        </form>
        {{HelpText("Que es una tarea automatizable", "Es una tarea permitida y revisable. Nombre tecnico: recipe. En Pilot solo se pueden ejecutar tareas de una lista permitida; no se inventan comandos ni acciones arbitrarias.")}}
      </div>
      <div class="card">
        <h2>Probar ahora</h2>
        <p>Entradas directas a las pruebas mas claras para un usuario nuevo. Todas quedan en modo seguro y local.</p>
        <div class="quick-grid">
          {{QuickAction("Probar demo HTML segura", "genera html demo")}}
          <a class="button ghost" href="/guia">Guiarme paso a paso</a>
          <a class="button ghost" href="/recipes">Ver tareas automatizables</a>
          <a class="button ghost" href="/variables">Revisar datos de la tarea</a>
          <a class="button ghost" href="/approvals/demo">Simular aprobacion humana</a>
          <a class="button ghost" href="/runs">Ver historial</a>
          <a class="button ghost" href="/ai/audit">Ver decisiones de IA</a>
          <a class="button ghost" href="/memory">Ver memoria de procesos</a>
          <a class="button ghost" href="/app-profiles">Ver apps y sitios</a>
          <a class="button ghost" href="/executor-harness">Probar click benigno supervisado</a>
        </div>
        {{HelpText("Que podes probar ahora", "Demo HTML y Markdown, tareas permitidas, datos requeridos, validador, aprobaciones simuladas, historial local, decisiones de IA sin proveedor real, procesos aprendidos y apps/sitios.")}}
      </div>
    </section>

    <section class="grid">
      <div class="card full">
        <h2>Que hace ONE BRAIN Pilot</h2>
        <p>Pilot sirve para recorrer el flujo de una automatizacion segura de punta a punta: entender la tarea, ver su definicion, revisar datos, validar reglas, simular aprobaciones, ejecutar una demo local y auditar el resultado.</p>
        <p>Tambien deja inspeccionar confianza, decisiones de IA, procesos aprendidos y apps/sitios sin requerir OpenAI real, sin reproduccion de acciones y sin acciones comerciales.</p>
      </div>

      <div class="card full">
        <h2>Que no hara por seguridad</h2>
        <ul>
          <li>No hace clicks, login, aceptacion de cookies, carrito, compra ni pago.</li>
          <li>No abre HTML ni evidencia generada automaticamente.</li>
          <li>No ejecuta comandos arbitrarios ni tareas fuera de la lista permitida.</li>
          <li>No hace llamadas reales a OpenAI en este hito.</li>
          <li>No reproduce acciones peligrosas ni genera tareas ejecutables desde una observacion.</li>
        </ul>
      </div>

      <div id="workflow" class="card full">
        <h2>Flujo recomendado paso a paso</h2>
        <div class="step-flow">
          {{GuideStepCard(1, "Elegi una tarea", "Elegir una tarea automatizable o escribir una instruccion simple.", "/recipes", "Demo segura", "safe")}}
          {{GuideStepCard(2, "Revisa los datos necesarios", "Ver que datos requiere la tarea y cuales son sensibles.", "/variables", "Informacion", "info")}}
          {{GuideStepCard(3, "Valida seguridad", "Confirmar que el validador no detecta acciones no permitidas.", "/recipes", "Requiere revision", "warn")}}
          {{GuideStepCard(4, "Simula aprobacion humana", "Ver como una accion sensible queda frenada y auditada.", "/approvals/demo", "Aprobacion humana", "approval")}}
          {{GuideStepCard(5, "Ejecuta una demo segura", "Generar reporte HTML local sin abrir archivos automaticamente.", "/guia?paso=5", "Solo lectura", "safe")}}
          {{GuideStepCard(6, "Revisa historial y evidencia", "Ver rutas de evidencia generada y resultado de ejecucion.", "/runs", "Evidencia generada", "info")}}
          {{GuideStepCard(7, "Revisa decisiones de IA", "Entender que perfil de IA se recomendaria sin llamadas reales.", "/ai/audit", "Configuracion", "info")}}
          {{GuideStepCard(8, "Revisa procesos y apps", "Consultar procesos aprendidos y apps/sitios soportados.", "/memory", "Simulacion segura", "disabled")}}
        </div>
        <p><a class="button" href="/guia">Guiarme paso a paso</a> <a class="button ghost" href="/guia?paso=1">Retomar recorrido</a></p>
      </div>

      <div class="card full">
        <h2>Conceptos basicos</h2>
        {{ConceptHint("Tarea automatizable", "Nombre tecnico: recipe. Es una tarea revisable y permitida por lista segura.")}}
        {{ConceptHint("Datos de la tarea", "Nombre tecnico: variables. Son valores necesarios para preparar o ejecutar una tarea controlada.")}}
        {{ConceptHint("Historial", "Nombre tecnico: runs. Registra ejecuciones locales con estado, seguridad y evidencia generada.")}}
        {{ConceptHint("Aprobacion humana", "Nombre tecnico: human-in-the-loop. Una persona revisa pasos sensibles antes de avanzar.")}}
        {{ConceptHint("Confianza", "Nombre tecnico: confidence. Resume que tan confiable es una tarea o flujo candidato.")}}
        {{ConceptHint("Decisiones de IA", "Nombre tecnico: AI audit. Explica que perfil de IA se recomendaria o bloquearia.")}}
        {{ConceptHint("Apps y sitios", "Nombre tecnico: app profiles. Define capacidades, bloqueos y politica de riesgo por app o sitio.")}}
        {{ConceptHint("Procesos aprendidos", "Nombre tecnico: process memory. Guarda procesos observados para buscarlos y reutilizarlos con supervision.")}}
        {{ConceptHint("Busqueda de procesos", "Nombre tecnico: workflow retrieval. Busca procesos similares sin IA real ni ejecucion automatica.")}}
        {{ConceptHint("Evidencia generada", "Nombre tecnico: artifacts. Archivos locales de resultado, reporte, auditoria o borrador.")}}
        {{ConceptHint("Solo lectura", "Modo que observa o genera reportes sin modificar sitios ni enviar formularios.")}}
        {{ConceptHint("Diagnostico", "Salida controlada cuando un sitio externo no deja continuar y se registra solo la observacion.")}}
        {{ConceptHint("Bloqueo seguro", "Si falta informacion, configuracion o ejecutor seguro, ONE BRAIN bloquea la accion.")}}
      </div>

      <div class="card">
        <h2>Estado de la tarea</h2>
        <div class="metric"><span>Estado</span><strong>{{Html(status)}}</strong></div>
        <div class="metric"><span>Tarea permitida</span><strong>{{Html(recipeLabel)}}</strong></div>
        <div class="metric"><span>Motivo</span><strong>{{Html(plan?.Intent.Reason ?? "esperando_tarea")}}</strong></div>
      </div>

      <div id="safety" class="card">
        <h2>Resumen de seguridad</h2>
        <p><span class="badge safe">0 clicks</span> <span class="badge safe">0 cookies aceptadas</span> <span class="badge safe">0 login</span> <span class="badge safe">0 carrito</span> <span class="badge safe">0 compra</span> <span class="badge safe">0 pago</span></p>
        <p>Modo seguro permanente. No hay agente libre autonomo, comandos arbitrarios, apertura automatica de navegador ni apertura automatica de HTML.</p>
      </div>

      <div class="card">
        <h2>Plan seguro</h2>
        {{PlanList(plan)}}
      </div>

      <div class="card">
        <h2>Capacidades bloqueadas</h2>
        {{BlockedList(plan)}}
      </div>

      <div class="card">
        <h2>Observar y aprender</h2>
        <p>El modo de observacion supervisada muestra una linea de tiempo candidata y anotaciones humanas desde una simulacion segura local. No reproduce acciones reales ni acciones sensibles.</p>
        <p><a class="button ghost" href="/recording/demo">Abrir demo de observacion</a></p>
      </div>

      <div class="card">
        <h2>Flujos supervisados</h2>
        <p>Promover un flujo candidato y recorrerlo paso a paso con control humano. En v0 no hay playback libre ni acciones reales peligrosas.</p>
        <p><a class="button ghost" href="/flows">Ver flujos promovidos</a> <a class="button ghost" href="/playback/demo">Probar playback supervisado</a></p>
      </div>

      <div class="card">
        <h2>Aprobaciones y confianza</h2>
        <p>Revisar una demo supervisada de aprobacion y confianza. Aprobar o rechazar solo registra auditoria; no envia, no compra, no paga y no reproduce acciones.</p>
        <p><a class="button ghost" href="/approvals/demo">Abrir demo de aprobacion</a></p>
      </div>

      <div class="card">
        <h2>Configuracion de IA</h2>
        <p>Configurar y auditar el enrutamiento central de perfiles OpenAI. Pilot muestra secretos enmascarados y solo prueba en seco; no hay llamada real a proveedor.</p>
        <p><a class="button ghost" href="/pilot/legacy/pilot/legacy/pilot/legacy/pilot/legacy/ai/config">Abrir configuracion de IA</a></p>
      </div>

      <div class="card">
        <h2>Historial y decisiones de IA</h2>
        <p>Ver historial local de ejecuciones y decisiones del router IA. La evidencia generada queda bajo `artifacts/` y no se versiona.</p>
        <p><a class="button ghost" href="/runs">Abrir historial</a> <a class="button ghost" href="/ai/audit">Abrir decisiones de IA</a></p>
      </div>

      <div class="card">
        <h2>Tareas automatizables y datos de la tarea</h2>
        <p>Inspeccionar tareas permitidas, crear borradores seguros, revisar datos requeridos y correr validador antes de promover nada. Pilot no sobreescribe tareas estables.</p>
        <p><a class="button ghost" href="/recipes">Abrir tareas automatizables</a> <a class="button ghost" href="/variables">Abrir datos de la tarea</a></p>
      </div>

      <div class="card">
        <h2>Procesos aprendidos</h2>
        <p>Buscar procesos observados o aprendidos por texto, etiquetas, app, dominio, estado y confianza. La busqueda es deterministica y nunca ejecuta acciones.</p>
        <p><a class="button ghost" href="/memory">Abrir memoria de procesos</a></p>
      </div>

      <div class="card">
        <h2>Apps y sitios soportados</h2>
        <p>Inspeccionar perfiles seguros de apps y sitios, capacidades, politica de riesgo y diagnosticos externos fragiles. No habilitan clicks, login, cookies, compra ni pago por defecto.</p>
        <p><a class="button ghost" href="/app-profiles">Abrir perfiles de apps</a></p>
      </div>

      <div class="card full">
        <h2>Resultado de ejecucion</h2>
        {{ExecutionStatusBlock(result)}}
        {{CopyPathField("Ruta del reporte Markdown", latestMarkdown)}}
        {{CopyPathField("Ruta del reporte HTML", latestHtml)}}
        {{CopyPathField("Carpeta de evidencia generada", result?.ArtifactsFolder ?? "artifacts")}}
        {{OutputBlock(result)}}
      </div>
    </section>
  </main>
</body>
</html>
""";
    }

    public static string RenderRecordingDemo(RecipeTimeline timeline)
    {
        return $$"""
<!doctype html>
<html lang="es">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <title>ONE BRAIN Pilot - Observacion supervisada</title>
  <style>
    :root { --ink: #17211a; --muted: #5c6b60; --paper: #f5f1e7; --panel: #fffaf0; --line: #d7cdb7; --accent: #e66b2d; --safe: #226b45; --risk: #8a352d; }
    body { margin: 0; color: var(--ink); font-family: "Aptos", "Segoe UI", sans-serif; background: linear-gradient(135deg, #f7f2df, #e4eadc); }
    main { max-width: 1120px; margin: 0 auto; padding: 40px 24px; }
    .card { background: rgba(255,250,240,.9); border: 1px solid var(--line); border-radius: 26px; padding: 24px; box-shadow: 0 20px 70px rgba(43,32,16,.14); margin-bottom: 18px; }
    h1 { font-family: Georgia, "Times New Roman", serif; font-size: clamp(38px, 6vw, 72px); line-height: .95; margin: 8px 0 12px; letter-spacing: -.045em; }
    h2 { margin-top: 0; }
    p, li { color: var(--muted); line-height: 1.5; }
    .badge { display: inline-block; border-radius: 999px; padding: 5px 10px; font-weight: 800; font-size: 12px; background: #eadfca; white-space: nowrap; }
    .safe { color: var(--safe); background: #dcebdd; }
    .risk { color: var(--risk); background: #f6e7e6; }
    .help-inline { display: inline-flex; align-items: center; gap: 6px; flex-wrap: wrap; }
    table { width: 100%; border-collapse: collapse; background: var(--panel); border-radius: 18px; overflow: hidden; }
    th, td { padding: 12px 14px; border-bottom: 1px solid var(--line); text-align: left; vertical-align: top; }
    th { font-size: 11px; text-transform: uppercase; letter-spacing: .08em; background: #efe6d4; }
    textarea, select, input { width: 100%; border: 1px solid var(--line); border-radius: 14px; padding: 10px; background: #fffdf6; font: inherit; }
    button, .button { border: 0; border-radius: 999px; padding: 11px 16px; color: #fffaf0; background: var(--ink); font-weight: 800; text-decoration: none; }
    .grid { display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 14px; }
    @media (max-width: 780px) { .grid { grid-template-columns: 1fr; } }
  </style>
</head>
<body>
  <main>
    {{PilotChrome("Observacion")}}
    <section class="card">
      <p><span class="badge safe">modo observacion</span> <span class="badge safe">sin reproduccion de acciones</span> <span class="badge safe">no clicks</span></p>
      <h1>Observacion supervisada</h1>
      <p>Esta pantalla muestra una simulacion segura de observacion. Sirve para entender como ONE BRAIN registraria una sesion y la convertiria en una linea de tiempo candidata sin ejecutar acciones reales.</p>
      <p>{{ConceptHint("Procesos aprendidos", "Memoria local de procesos observados o aprendidos. Se consulta y anota; no dispara ejecucion.")}}</p>
      <p><a class="button" href="/">Volver al inicio</a></p>
    </section>

    <section class="card">
      <h2>Linea de tiempo</h2>
      <table>
        <thead>
          <tr><th>Paso</th><th>Tiempo</th><th>Evento</th><th>Ventana/app</th><th>Elemento</th><th>Confianza</th><th>Etiqueta sugerida</th><th>Riesgo</th><th>Aprobacion</th></tr>
        </thead>
        <tbody>
          {{TimelineRows(timeline)}}
        </tbody>
      </table>
    </section>

    <section class="card">
      <h2>Anotaciones humanas</h2>
      <div class="grid">
        <form method="post" action="/recording/demo/annotate">
          <label>Numero de paso<input name="stepNumber" value="1"></label>
          <label>Tipo de anotacion
            <select name="annotationType">
              <option value="search_customer">este bloque es buscar cliente</option>
              <option value="prepare_message">este bloque es preparar mensaje</option>
              <option value="requires_approval">este paso requiere aprobacion</option>
              <option value="variable">este dato debe ser variable</option>
              <option value="ignore">este paso se puede ignorar</option>
              <option value="sensitive">este paso es sensible</option>
              <option value="free_note">nota libre</option>
            </select>
          </label>
          <label>Nota<textarea name="text">nota libre</textarea></label>
          <p><button type="submit">Previsualizar anotacion</button></p>
        </form>
        <div>
          <h3>Anotaciones actuales de la simulacion segura</h3>
          <ul>{{AnnotationRows(timeline)}}</ul>
        </div>
      </div>
    </section>
  </main>
</body>
</html>
""";
    }

    public static string RenderPromotedFlows(IReadOnlyList<PromotedCandidateFlow> flows, string dataOrigin = PilotDataOrigins.Runtime)
    {
        return $$"""
<!doctype html>
<html lang="es">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <title>ONE BRAIN Pilot - Flujos supervisados</title>
  {{SharedPilotStyle()}}
</head>
<body>
  <main>
    {{PilotChrome("Flujos supervisados")}}
    <section class="card">
      {{OriginBanner(dataOrigin)}}
      <p><span class="badge safe">playback supervisado</span> <span class="badge approval">control humano</span> <span class="badge blocked">sin ejecucion libre</span></p>
      <h1>Flujos supervisados</h1>
      <p>Un flujo promovido viene de una linea de tiempo candidata revisada. Solo puede simularse paso a paso, con aprobacion cuando corresponde y sin acciones reales peligrosas.</p>
      <p>{{ConceptHint("Promocion de flujo", "Convierte una linea de tiempo candidata en un flujo apto para playback supervisado. No genera una recipe ejecutable libre.")}}</p>
      <form method="post" action="/flows/demo/promote">
        <button type="submit">Promover demo segura</button>
        <a class="button" href="/playback/demo">Probar simulacion supervisada</a>
      </form>
    </section>
    <section class="card">
      <h2>Flujos disponibles</h2>
      {{EmptyStateNotice(flows.Count == 0, "Todavia no hay flujos promovidos. Podes promover la demo segura para ver el recorrido completo.")}}
      <table>
        <thead><tr><th>Flujo</th><th>Estado</th><th>Riesgo</th><th>Confianza</th><th>Aprobacion</th><th>Pasos</th><th>Accion</th></tr></thead>
        <tbody>{{PromotedFlowRows(flows)}}</tbody>
      </table>
    </section>
  </main>
</body>
</html>
""";
    }

    public static string RenderPromotedFlowDetail(
        PromotedCandidateFlow flow,
        CandidateFlowPromotionResult? promotion = null,
        PromotedFlowArtifactWriteResult? write = null,
        string dataOrigin = PilotDataOrigins.Runtime)
    {
        return $$"""
<!doctype html>
<html lang="es">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <title>ONE BRAIN Pilot - Detalle de flujo supervisado</title>
  {{SharedPilotStyle()}}
</head>
<body>
  <main>
    {{PilotChrome("Detalle de flujo")}}
    <section class="card">
      {{OriginBanner(dataOrigin)}}
      <p><span class="badge safe">supervisado</span> <span class="badge approval">aprobacion si corresponde</span> <span class="badge blocked">autonomo desactivado</span></p>
      <h1>{{Html(flow.Title)}}</h1>
      <p>{{Html(flow.Description)}}</p>
      <p>La ejecucion real esta desactivada en esta version. ONE BRAIN registra la decision, pero no actua sobre otras apps. Este flujo solo permite confirmar una simulacion, saltar si la politica lo permite, o abortar.</p>
      {{PromotionResultBlock(promotion, write)}}
      <p><a class="button" href="/flows">Volver a flujos</a> <a class="button" href="/playback/demo">Iniciar simulacion supervisada</a></p>
    </section>
    <section class="grid">
      <div class="card">
        <h2>Resumen</h2>
        <div class="metric"><span>Estado</span><strong>{{Html(flow.Status)}}</strong></div>
        <div class="metric"><span>Riesgo</span><strong>{{Html(flow.RiskLevel)}}</strong></div>
        <div class="metric"><span>Confianza</span><strong>{{flow.ConfidenceScore}}</strong></div>
        <div class="metric"><span>Aprobacion humana</span><strong>{{SpanishBool(flow.RequiresHumanApproval)}}</strong></div>
        <div class="metric"><span>Playback autonomo</span><strong>{{SpanishBool(flow.AllowsAutonomousPlayback)}}</strong></div>
      </div>
      <div class="card">
        <h2>Variables declaradas</h2>
        <ul>{{ListRows(flow.Variables)}}</ul>
      </div>
    </section>
    <section class="card">
      <h2>Pasos supervisados</h2>
      <table>
        <thead><tr><th>Paso</th><th>Accion</th><th>Riesgo</th><th>Aprobacion</th><th>Executor seguro</th><th>Modo</th><th>Saltar</th></tr></thead>
        <tbody>{{PromotedFlowStepRows(flow.Steps)}}</tbody>
      </table>
    </section>
  </main>
</body>
</html>
""";
    }

    public static string RenderSupervisedPlayback(
        PromotedCandidateFlow flow,
        SupervisedPlaybackSession session,
        SupervisedPlaybackActionResult? actionResult = null,
        SupervisedPlaybackArtifactWriteResult? playbackWrite = null,
        RunHistoryArtifactWriteResult? runWrite = null,
        string dataOrigin = PilotDataOrigins.Runtime)
    {
        var currentStep = flow.Steps.FirstOrDefault(step => step.StepNumber == session.CurrentStepNumber) ?? flow.Steps.FirstOrDefault();
        return $$"""
<!doctype html>
<html lang="es">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <title>ONE BRAIN Pilot - Playback supervisado</title>
  {{SharedPilotStyle()}}
</head>
<body>
  <main>
    {{PilotChrome("Playback supervisado")}}
    <section class="card">
      {{OriginBanner(dataOrigin)}}
      <p><span class="badge safe">paso a paso</span> <span class="badge approval">humano al mando</span> <span class="badge blocked">sin acciones peligrosas</span></p>
      <h1>Playback supervisado</h1>
      <p>Este flujo no ejecuta acciones reales. Este flujo no hace clicks. Este flujo no envia mensajes. Este flujo no compra, no paga y no inicia sesion.</p>
      <p>La ejecucion real esta desactivada en esta version. ONE BRAIN registra la decision, pero no actua sobre otras apps.</p>
      <p>{{ConceptHint("Playback supervisado", "Ejecucion guiada paso a paso. Si hay ambiguedad, accion sensible o falta executor seguro, el flujo se frena.")}}</p>
      {{PlaybackActionBlock(actionResult, playbackWrite, runWrite)}}
    </section>
    <section class="grid">
      <div class="card">
        <h2>Paso actual</h2>
        {{CurrentPlaybackStepBlock(currentStep, session)}}
        <form method="post" action="/playback/demo/confirm">
          <input type="hidden" name="stepNumber" value="{{currentStep?.StepNumber ?? 1}}">
          <label><input type="checkbox" name="approval" value="approved"> Simular aprobacion humana para este paso</label>
          <p><button type="submit">Confirmar paso de demostracion</button></p>
        </form>
        <form method="post" action="/playback/demo/skip">
          <input type="hidden" name="stepNumber" value="{{currentStep?.StepNumber ?? 1}}">
          <p><button type="submit" class="secondary">Saltar paso si la politica lo permite</button></p>
        </form>
        <form method="post" action="/playback/demo/abort">
          <p><button type="submit" class="reject">Abortar flujo</button></p>
        </form>
      </div>
      <div class="card">
        <h2>Resultado</h2>
        <div class="metric"><span>Estado de sesion</span><strong>{{Html(session.Status)}}</strong></div>
        <div class="metric"><span>Playback ID</span><strong>{{Html(session.PlaybackId)}}</strong></div>
        <div class="metric"><span>Flujo</span><strong>{{Html(flow.FlowId)}}</strong></div>
        <p>{{Safety(session.SafetyCounters)}}</p>
      </div>
    </section>
    <section class="card">
      <h2>Linea de pasos</h2>
      <table>
        <thead><tr><th>Paso</th><th>Estado</th><th>Decision</th><th>Aprobacion</th><th>Evidencia</th></tr></thead>
        <tbody>{{PlaybackStepRows(session.Steps)}}</tbody>
      </table>
    </section>
  </main>
</body>
</html>
""";
    }

    public static string RenderExecutorHarness(
        ExecutorHarnessTarget target,
        ApprovalRequest approval,
        ApprovalDecision? decision = null,
        ExecutorHarnessRunResult? result = null,
        ExecutorHarnessArtifactWriteResult? evidenceWrite = null,
        ApprovalArtifactWriteResult? approvalWrite = null,
        ApprovalArtifactWriteResult? decisionWrite = null,
        RunHistoryArtifactWriteResult? runWrite = null,
        ExecutorHarnessDryRunExplanation? dryRun = null)
    {
        dryRun ??= ExecutorHarnessService.BuildDryRunExplanation(target, decision);
        return $$"""
<!doctype html>
<html lang="es">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <title>ONE BRAIN Pilot - Executor Harness</title>
  {{SharedPilotStyle()}}
</head>
<body>
  <main>
    {{PilotChrome("Executor harness")}}
    <section class="card">
      <p><span class="badge approval">requiere aprobacion</span> <span class="badge safe">harness local</span> <span class="badge blocked">sin MercadoLibre</span> <span class="badge blocked">sin compra/pago/login/cookies</span></p>
      <h1>Click benigno supervisado</h1>
      <p>Esta pantalla es el primer harness para ejecutar un click real, pero solo sobre un objetivo benigno dentro de ONE BRAIN Pilot. No toca sitios externos, no acepta cookies, no inicia sesion, no compra, no paga y no envia mensajes.</p>
      <p class="notice">Fail-closed: si falta aprobacion, target seguro o executor seguro, ONE BRAIN bloquea la accion y registra evidencia de bloqueo.</p>
      {{ConceptHint("Que se va a hacer", "ONE BRAIN buscara esta misma ventana local de Pilot por titulo, resolvera el boton benigno por nombre y ejecutara un unico click UIA supervisado. Despues verificara el resultado y escribira artifacts locales.")}}
      <p><a class="button ghost" href="/executor-harness/dry-run">Ver dry-run explicable</a> <a class="button ghost" href="/executor-harness/replay">Ver replay de evidencia</a> <a class="button ghost" href="/executor-harness/evidence">Ver indice de evidencia</a></p>
    </section>

    <section class="grid">
      <div class="card">
        <h2>Objetivo benigno del harness</h2>
        <p><span class="badge safe">controlado</span> <span class="badge safe">benigno</span> <span class="badge info">solo local</span></p>
        <button type="button" aria-label="{{Html(target.ExpectedTargetName)}}">{{Html(target.ExpectedTargetName)}}</button>
        <div class="metric"><span>ID</span><strong>{{Html(target.HarnessId)}}</strong></div>
        <div class="metric"><span>Target UIA</span><strong>{{Html(target.TargetRef)}}</strong></div>
        <div class="metric"><span>Ventana</span><strong>{{Html(target.WindowTitleContains)}}</strong></div>
        <div class="metric"><span>Executor seguro</span><strong>{{SpanishBool(target.HasSafeExecutor)}}</strong></div>
      </div>
      <div class="card">
        <h2>Aprobacion requerida</h2>
        <p>Solicitud: <strong>{{Html(approval.ApprovalRequestId)}}</strong></p>
        <p>Estado: <span class="badge approval">{{Html(approval.Status)}}</span></p>
        <p>{{Html(approval.Description)}}</p>
        <p class="notice">Matriz de seguridad: solo permite el target local allowlisted, con aprobacion humana, executor seguro y verificacion posterior. Si algo falta, bloquea antes de hacer click.</p>
        <form method="post" action="/executor-harness/click">
          <p><button type="submit">Aprobar y ejecutar click benigno supervisado</button></p>
        </form>
        <p>Este boton no habilita clicks generales. Solo intenta el objetivo local mostrado a la izquierda.</p>
      </div>
    </section>

    {{ExecutorHarnessDryRunBlock(dryRun)}}
    {{ExecutorHarnessResultBlock(result, evidenceWrite, approvalWrite, decisionWrite, runWrite, decision)}}
  </main>
</body>
</html>
""";
    }

    public static string RenderExecutorHarnessReplay(ExecutorHarnessEvidenceReplay replay)
    {
        return $$"""
<!doctype html>
<html lang="es">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <title>ONE BRAIN Pilot - Replay evidencia harness</title>
  {{SharedPilotStyle()}}
</head>
<body>
  <main>
    {{PilotChrome("Replay evidencia harness")}}
    <section class="card">
      <p><span class="badge info">read-only</span> <span class="badge safe">sin click</span> <span class="badge safe">sin auto-open</span></p>
      <h1>Replay de evidencia del executor harness</h1>
      <p>Esta pantalla reconstruye el ultimo artifact local de <span class="path">artifacts/executor-harness/</span>. No ejecuta acciones, no abre archivos y no hace clicks.</p>
      <p><a class="button" href="/executor-harness">Volver al harness</a></p>
    </section>
    {{ExecutorHarnessReplayBlock(replay)}}
  </main>
</body>
</html>
""";
    }

    public static string RenderExecutorHarnessEvidenceIndex(ExecutorHarnessEvidenceIndex index)
    {
        return $$"""
<!doctype html>
<html lang="es">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <title>ONE BRAIN Pilot - Indice de evidencia harness</title>
  {{SharedPilotStyle()}}
</head>
<body>
  <main>
    {{PilotChrome("Indice evidencia harness")}}
    <section class="card">
      <p><span class="badge info">read-only</span> <span class="badge safe">sin click</span> <span class="badge safe">sin auto-open</span></p>
      <h1>Indice de evidencia del executor harness</h1>
      <p>Lista artifacts locales bajo <span class="path">artifacts/executor-harness/</span>. No abre archivos, no ejecuta acciones y no inventa datos reales si no existen.</p>
      <p><a class="button" href="/executor-harness">Volver al harness</a> <a class="button ghost" href="/executor-harness/replay">Ver replay del ultimo artifact</a></p>
    </section>
    {{ExecutorHarnessEvidenceIndexBlock(index)}}
  </main>
</body>
</html>
""";
    }

    public static string RenderGuide(int step)
    {
        var currentStep = Math.Clamp(step, 1, 8);
        var previousStep = Math.Max(1, currentStep - 1);
        var nextStep = Math.Min(8, currentStep + 1);
        var stepTitle = currentStep switch
        {
            1 => "Elegi una tarea",
            2 => "Revisa los datos necesarios",
            3 => "Valida seguridad",
            4 => "Simula aprobacion humana",
            5 => "Ejecuta una demo segura",
            6 => "Revisa historial y evidencia",
            7 => "Revisa decisiones de IA",
            _ => "Revisa procesos y apps"
        };
        var stepDescription = currentStep switch
        {
            1 => "Mira la lista de tareas automatizables permitidas. ONE BRAIN no inventa tareas desde texto libre.",
            2 => "Revisa que datos necesita la tarea y si alguno es sensible. Los secretos no se muestran completos.",
            3 => "Confirma que la tarea no tenga acciones bloqueadas y que el validador deje claro que puede ejecutarse.",
            4 => "Simula una aprobacion humana. En esta version la decision se registra, pero no ejecuta una accion real.",
            5 => "Ejecuta la demo HTML local. Genera evidencia bajo artifacts y no abre archivos automaticamente.",
            6 => "Mira el historial y las rutas de evidencia generada para entender que paso y donde quedo registrado.",
            7 => "Revisa las decisiones de IA. En esta version no hay llamadas reales a OpenAI.",
            _ => "Consulta procesos aprendidos y apps/sitios soportados. La informacion orienta, no ejecuta acciones."
        };
        var realPath = currentStep switch
        {
            1 => "/recipes",
            2 => "/variables",
            3 => "/recipes",
            4 => "/approvals/demo",
            5 => "/",
            6 => "/runs",
            7 => "/ai/audit",
            _ => "/memory"
        };

        return $$"""
<!doctype html>
<html lang="es">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <title>ONE BRAIN Pilot - Guia paso a paso</title>
  {{SharedPilotStyle()}}
</head>
<body>
  <main>
    {{PilotChrome("Guia paso a paso")}}
    <section class="card">
      <p><span class="badge safe">demo segura</span> <span class="badge info">recorrido guiado</span> <span class="badge approval">aprobacion visible</span></p>
      <h1>Guiarme paso a paso</h1>
      <p>Este recorrido te lleva por la experiencia recomendada sin acciones reales peligrosas. Podes retomar copiando la URL del paso actual.</p>
      {{HelpBox("Como retomar", "Usa el boton Retomar recorrido o guarda la URL de este paso. No hay persistencia compleja todavia.")}}
    </section>

    <section class="grid">
      <div class="card">
        <p><span class="step-number">{{currentStep}}</span> <span class="badge {{GuideBadgeClass(currentStep)}}">{{GuideBadgeLabel(currentStep)}}</span></p>
        <h2>{{Html(stepTitle)}}</h2>
        <p>{{Html(stepDescription)}}</p>
        <p><strong>Que mirar:</strong> {{Html(GuideLookFor(currentStep))}}</p>
        <p><a class="button" href="{{realPath}}">Abrir pantalla real</a></p>
      </div>
      <div class="card">
        <h2>Mapa del recorrido</h2>
        <div class="step-flow">
          {{GuideStepCard(1, "Elegi una tarea", "Lista de tareas permitidas.", "/guia?paso=1", "Demo segura", "safe")}}
          {{GuideStepCard(2, "Revisa datos", "Datos necesarios y sensibilidad.", "/guia?paso=2", "Informacion", "info")}}
          {{GuideStepCard(3, "Valida seguridad", "Reglas y bloqueos visibles.", "/guia?paso=3", "Revision", "warn")}}
          {{GuideStepCard(4, "Aprobacion", "Decision humana simulada.", "/guia?paso=4", "Aprobacion", "approval")}}
          {{GuideStepCard(5, "Demo segura", "Reporte HTML local.", "/guia?paso=5", "Solo lectura", "safe")}}
          {{GuideStepCard(6, "Historial", "Evidencia generada.", "/guia?paso=6", "Evidencia", "info")}}
          {{GuideStepCard(7, "Decisiones IA", "Auditoria sin OpenAI real.", "/guia?paso=7", "Configuracion", "info")}}
          {{GuideStepCard(8, "Procesos y apps", "Memoria y apps soportadas.", "/guia?paso=8", "Simulacion", "disabled")}}
        </div>
      </div>
    </section>

    <section class="card">
      <p>
        <a class="button ghost" href="/guia?paso={{previousStep}}">Anterior</a>
        <a class="button" href="/guia?paso={{nextStep}}">Siguiente</a>
        <a class="button ghost" href="/">Salir de la guia</a>
        <a class="button ghost" href="/guia?paso={{currentStep}}">Retomar recorrido</a>
      </p>
    </section>
  </main>
</body>
</html>
""";
    }

    public static string RenderApprovalDemo(ApprovalRequest request, RecipeConfidenceProfile confidence, ApprovalDecision? decision = null)
    {
        var decisionStatus = decision == null ? "pending_decision" : decision.Decision;
        var decisionReason = decision == null ? "-" : decision.Reason;

        return $$"""
<!doctype html>
<html lang="es">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <title>ONE BRAIN Pilot - Aprobaciones</title>
  <style>
    :root { --ink: #17211a; --muted: #5c6b60; --paper: #f5f1e7; --panel: #fffaf0; --line: #d7cdb7; --accent: #e66b2d; --safe: #226b45; --risk: #8a352d; }
    body { margin: 0; color: var(--ink); font-family: "Aptos", "Segoe UI", sans-serif; background: linear-gradient(135deg, #f7f2df, #e4eadc); }
    main { max-width: 1120px; margin: 0 auto; padding: 40px 24px; }
    .card { background: rgba(255,250,240,.92); border: 1px solid var(--line); border-radius: 26px; padding: 24px; box-shadow: 0 20px 70px rgba(43,32,16,.14); margin-bottom: 18px; }
    h1 { font-family: Georgia, "Times New Roman", serif; font-size: clamp(38px, 6vw, 72px); line-height: .95; margin: 8px 0 12px; letter-spacing: -.045em; }
    h2 { margin-top: 0; }
    p, li { color: var(--muted); line-height: 1.5; }
    .badge { display: inline-block; border-radius: 999px; padding: 5px 10px; font-weight: 800; font-size: 12px; background: #eadfca; white-space: nowrap; }
    .safe { color: var(--safe); background: #dcebdd; }
    .risk { color: var(--risk); background: #f6e7e6; }
    .help-inline { display: inline-flex; align-items: center; gap: 6px; flex-wrap: wrap; }
    .grid { display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 14px; }
    .metric { border-bottom: 1px solid var(--line); padding: 10px 0; }
    .metric strong { display: block; font-size: 20px; color: var(--ink); }
    textarea, input { width: 100%; border: 1px solid var(--line); border-radius: 14px; padding: 10px; background: #fffdf6; font: inherit; }
    button, .button { border: 0; border-radius: 999px; padding: 11px 16px; color: #fffaf0; background: var(--ink); font-weight: 800; text-decoration: none; }
    button.reject { background: var(--risk); }
    code { display: block; white-space: pre-wrap; border-radius: 16px; padding: 14px; background: #211f1a; color: #fbf1d5; }
    @media (max-width: 780px) { .grid { grid-template-columns: 1fr; } }
  </style>
</head>
<body>
  <main>
    {{PilotChrome("Aprobaciones")}}
    <section class="card">
      {{OriginBanner(PilotDataOrigins.DemoFixture)}}
      <p><span class="badge approval">requiere aprobacion humana</span> <span class="badge info">solo auditoria</span> <span class="badge safe">sin ejecucion</span></p>
      <h1>Aprobaciones humanas</h1>
      <p>Esta demo prepara una accion sensible y se frena antes del envio. La decision humana queda auditada, pero en v0 no habilita ninguna ejecucion real.</p>
      <p>ExecutionAllowed=false significa: la ejecucion real esta desactivada en esta version. ONE BRAIN registra la decision, pero no actua sobre otras apps.</p>
      <p>{{ConceptHint("Aprobacion humana", "Nombre tecnico: human-in-the-loop. Una persona revisa pasos sensibles o de baja confianza antes de cualquier intento de ejecucion.")}}</p>
      <p><a class="button" href="/">Volver al inicio</a></p>
    </section>

    <section class="grid">
      <div class="card">
        <h2>Solicitud pendiente</h2>
        <div class="metric"><span>Solicitud</span><strong>{{Html(request.ApprovalRequestId)}}</strong></div>
        <div class="metric"><span>Tipo de accion</span><strong>{{Html(request.ActionKind)}}</strong></div>
        <div class="metric"><span>Riesgo</span><strong>{{Html(request.RiskLevel)}}</strong></div>
        <div class="metric"><span>Bloqueo seguro</span><strong>{{SpanishBool(request.FailClosed)}}</strong></div>
        <div class="metric"><span>Requiere aprobacion</span><strong>{{SpanishBool(request.RequiresApproval)}}</strong></div>
        <p>{{Html(request.Description)}}</p>
        <code>{{Html(request.Preview)}}</code>
      </div>

      <div class="card">
        <h2>Confianza de la tarea</h2>
        <div class="metric"><span>Flujo candidato</span><strong>{{Html(confidence.CandidateFlowId)}}</strong></div>
        <div class="metric"><span>Estado</span><strong>{{Html(confidence.Status)}}</strong></div>
        <div class="metric"><span>Puntaje de confianza</span><strong>{{confidence.ConfidenceScore}}</strong></div>
        <div class="metric"><span>Nivel de riesgo</span><strong>{{Html(confidence.RiskLevel)}}</strong></div>
        <div class="metric"><span>Ejecuciones</span><strong>{{confidence.Runs}}</strong></div>
        <p>Los flujos criticos quedan bloqueados hasta que exista una politica explicita de aprobacion humana. Esta demo no crea una tarea ejecutable.</p>
      </div>
    </section>

    <section class="card">
      <h2>Decision humana</h2>
      <form method="post" action="/approvals/demo/decide">
        <label>Motivo requerido para rechazar<textarea name="reason">No enviar todavia; requiere revision humana.</textarea></label>
        <p>
          <button type="submit" name="decision" value="approved">Aprobar decision demo</button>
          <button class="reject" type="submit" name="decision" value="rejected">Rechazar decision demo</button>
        </p>
      </form>
      <p>Estado de la decision: <strong>{{Html(decisionStatus)}}</strong></p>
      <p>Motivo de la decision: <strong>{{Html(decisionReason)}}</strong></p>
      <p><span class="badge safe">La ejecucion real esta desactivada en esta version.</span> <span class="badge safe">0 clicks</span> <span class="badge safe">0 cookies aceptadas</span> <span class="badge safe">0 login</span> <span class="badge safe">0 carrito</span> <span class="badge safe">0 compra</span> <span class="badge safe">0 pago</span></p>
    </section>
  </main>
</body>
</html>
""";
    }

    public static string RenderAIConfigConsole(IReadOnlyList<AIModelProfile> profiles, AIModelRouterResult? testResult = null)
    {
        return $$"""
<!doctype html>
<html lang="es">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <title>ONE BRAIN Pilot - Configuracion de IA</title>
  <style>
    :root { --ink: #17211a; --muted: #5c6b60; --paper: #f5f1e7; --panel: #fffaf0; --line: #d7cdb7; --safe: #226b45; --risk: #8a352d; }
    body { margin: 0; color: var(--ink); font-family: "Aptos", "Segoe UI", sans-serif; background: linear-gradient(135deg, #f7f2df, #e4eadc); }
    main { max-width: 1180px; margin: 0 auto; padding: 40px 24px; }
    .card { background: rgba(255,250,240,.92); border: 1px solid var(--line); border-radius: 26px; padding: 24px; box-shadow: 0 20px 70px rgba(43,32,16,.14); margin-bottom: 18px; }
    h1 { font-family: Georgia, "Times New Roman", serif; font-size: clamp(38px, 6vw, 72px); line-height: .95; margin: 8px 0 12px; letter-spacing: -.045em; }
    p, li { color: var(--muted); line-height: 1.5; }
    table { width: 100%; border-collapse: collapse; background: var(--panel); border-radius: 18px; overflow: hidden; }
    th, td { padding: 11px 12px; border-bottom: 1px solid var(--line); text-align: left; vertical-align: top; }
    th { font-size: 11px; text-transform: uppercase; letter-spacing: .08em; background: #efe6d4; }
    .badge { display: inline-block; border-radius: 999px; padding: 5px 10px; font-weight: 800; font-size: 12px; background: #eadfca; white-space: nowrap; }
    .safe { color: var(--safe); background: #dcebdd; }
    .risk { color: var(--risk); background: #f6e7e6; }
    .help-inline { display: inline-flex; align-items: center; gap: 6px; flex-wrap: wrap; }
    button, .button { border: 0; border-radius: 999px; padding: 11px 16px; color: #fffaf0; background: var(--ink); font-weight: 800; text-decoration: none; }
    code { display: block; white-space: pre-wrap; border-radius: 16px; padding: 14px; background: #211f1a; color: #fbf1d5; }
  </style>
</head>
<body>
  <main>
    {{PilotChrome("Configuracion de IA")}}
    <section class="card">
      <p><span class="badge info">OpenAI configurado como proveedor principal futuro</span> <span class="badge safe">router central</span> <span class="badge safe">secretos enmascarados</span> <span class="badge disabled">prueba en seco</span></p>
      <h1>Configuracion de IA</h1>
      <p>Esta consola explica como ONE BRAIN decidiria que perfil de modelo usar. Lee configuracion local, enmascara secretos y hace pruebas deterministicas sin llamar a OpenAI.</p>
      <p>{{ConceptHint("Decisiones de IA", "Registro local que explica por que un perfil IA fue elegido, bloqueado o escalado.")}}</p>
      <p><a class="button" href="/">Volver al inicio</a></p>
    </section>

    <section class="card">
      <h2>Perfiles oficiales</h2>
      <table>
        <thead>
          <tr><th>Perfil</th><th>Proveedor</th><th>Modelo</th><th>Secreto</th><th>Activo</th><th>Presupuesto mensual</th><th>Por tarea</th><th>Riesgo maximo</th><th>Alternativa</th><th>Estado</th><th>Registro de uso</th></tr>
        </thead>
        <tbody>
          {{AIProfileRows(profiles)}}
        </tbody>
      </table>
    </section>

    <section class="card">
      <h2>Probar configuracion</h2>
      <form method="post" action="/pilot/legacy/pilot/legacy/pilot/legacy/pilot/legacy/pilot/legacy/pilot/legacy/pilot/legacy/pilot/legacy/ai/config/test">
        <p><button type="submit">Correr prueba en seco del router</button></p>
      </form>
      {{AIRoutingResultBlock(testResult)}}
      <p>Ninguna API key se muestra completa. Si falta modelo, key o configuracion, ONE BRAIN bloquea la llamada futura por seguridad.</p>
    </section>
  </main>
</body>
</html>
""";
    }

    public static string RenderRunHistory(IReadOnlyList<RunHistoryRecord> runs, string dataOrigin = PilotDataOrigins.Runtime)
    {
        return $$"""
<!doctype html>
<html lang="es">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <title>ONE BRAIN Pilot - Historial</title>
  <style>
    :root { --ink: #17211a; --muted: #5c6b60; --paper: #f5f1e7; --panel: #fffaf0; --line: #d7cdb7; --safe: #226b45; --risk: #8a352d; --warn: #9a5a10; }
    body { margin: 0; color: var(--ink); font-family: "Aptos", "Segoe UI", sans-serif; background: linear-gradient(135deg, #f7f2df, #e4eadc); }
    main { max-width: 1220px; margin: 0 auto; padding: 40px 24px; }
    .card { background: rgba(255,250,240,.92); border: 1px solid var(--line); border-radius: 26px; padding: 24px; box-shadow: 0 20px 70px rgba(43,32,16,.14); margin-bottom: 18px; }
    h1 { font-family: Georgia, "Times New Roman", serif; font-size: clamp(38px, 6vw, 72px); line-height: .95; margin: 8px 0 12px; letter-spacing: -.045em; }
    p, li { color: var(--muted); line-height: 1.5; }
    table { width: 100%; border-collapse: collapse; background: var(--panel); border-radius: 18px; overflow: hidden; }
    th, td { padding: 11px 12px; border-bottom: 1px solid var(--line); text-align: left; vertical-align: top; }
    th { font-size: 11px; text-transform: uppercase; letter-spacing: .08em; background: #efe6d4; }
    .badge { display: inline-block; border-radius: 999px; padding: 5px 10px; font-weight: 800; font-size: 12px; background: #eadfca; white-space: nowrap; }
    .safe { color: var(--safe); background: #dcebdd; }
    .risk { color: var(--risk); background: #f6e7e6; }
    .warn { color: var(--warn); background: #f3e2bf; }
    .help-inline { display: inline-flex; align-items: center; gap: 6px; flex-wrap: wrap; }
    .path { word-break: break-all; }
    .button { display: inline-block; border-radius: 999px; padding: 11px 16px; color: #fffaf0; background: var(--ink); font-weight: 800; text-decoration: none; }
  </style>
</head>
<body>
  <main>
    {{PilotChrome("Historial")}}
    <section class="card">
      {{OriginBanner(dataOrigin)}}
      <p><span class="badge safe">evidencia generada local solamente</span> <span class="badge safe">sin secretos guardados</span> <span class="badge safe">base 0 clicks</span></p>
      <h1>Historial</h1>
      <p>Esta pantalla muestra ejecuciones locales con estado, seguridad, evidencia generada y referencias a aprobaciones, confianza y decisiones de IA. Si no hay datos reales todavia, Pilot lo indica y puede mostrar un ejemplo de demostracion separado.</p>
      <p>{{ConceptHint("Evidencia generada", "Archivo local generado por una ejecucion: HTML, Markdown, JSON de resumen, aprobaciones o auditoria.")}}</p>
      <p><a class="button" href="/">Volver al inicio</a></p>
    </section>

    <section class="card">
      <h2>Corridas recientes</h2>
      {{EmptyStateNotice(runs.Count == 0, "Todavia no hay ejecuciones registradas. Aca apareceran corridas locales, estado, seguridad y evidencia generada. Podes seguir con la guia paso a paso.")}}
      <table>
        <thead>
          <tr><th>Ejecucion</th><th>Estado</th><th>Hora</th><th>Origen</th><th>Tarea / flujo</th><th>Seguridad</th><th>Aprobacion</th><th>Confianza / IA</th><th>Evidencia</th><th>Error</th></tr>
        </thead>
        <tbody>
          {{RunHistoryRows(runs)}}
        </tbody>
      </table>
    </section>
  </main>
</body>
</html>
""";
    }

    public static string RenderAIAuditLog(IReadOnlyList<AIAuditRecord> audits, string dataOrigin = PilotDataOrigins.Runtime)
    {
        return $$"""
<!doctype html>
<html lang="es">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <title>ONE BRAIN Pilot - Decisiones de IA</title>
  <style>
    :root { --ink: #17211a; --muted: #5c6b60; --paper: #f5f1e7; --panel: #fffaf0; --line: #d7cdb7; --safe: #226b45; --risk: #8a352d; --warn: #9a5a10; }
    body { margin: 0; color: var(--ink); font-family: "Aptos", "Segoe UI", sans-serif; background: linear-gradient(135deg, #f7f2df, #e4eadc); }
    main { max-width: 1220px; margin: 0 auto; padding: 40px 24px; }
    .card { background: rgba(255,250,240,.92); border: 1px solid var(--line); border-radius: 26px; padding: 24px; box-shadow: 0 20px 70px rgba(43,32,16,.14); margin-bottom: 18px; }
    h1 { font-family: Georgia, "Times New Roman", serif; font-size: clamp(38px, 6vw, 72px); line-height: .95; margin: 8px 0 12px; letter-spacing: -.045em; }
    p, li { color: var(--muted); line-height: 1.5; }
    table { width: 100%; border-collapse: collapse; background: var(--panel); border-radius: 18px; overflow: hidden; }
    th, td { padding: 11px 12px; border-bottom: 1px solid var(--line); text-align: left; vertical-align: top; }
    th { font-size: 11px; text-transform: uppercase; letter-spacing: .08em; background: #efe6d4; }
    .badge { display: inline-block; border-radius: 999px; padding: 5px 10px; font-weight: 800; font-size: 12px; background: #eadfca; white-space: nowrap; }
    .safe { color: var(--safe); background: #dcebdd; }
    .risk { color: var(--risk); background: #f6e7e6; }
    .warn { color: var(--warn); background: #f3e2bf; }
    .help-inline { display: inline-flex; align-items: center; gap: 6px; flex-wrap: wrap; }
    .button { display: inline-block; border-radius: 999px; padding: 11px 16px; color: #fffaf0; background: var(--ink); font-weight: 800; text-decoration: none; }
  </style>
</head>
<body>
  <main>
    {{PilotChrome("Decisiones de IA")}}
    <section class="card">
      {{OriginBanner(dataOrigin)}}
      <p><span class="badge safe">sin llamada a proveedor</span> <span class="badge safe">sin prompts completos por defecto</span> <span class="badge safe">sin API keys</span></p>
      <h1>Decisiones de IA</h1>
      <p>Esta bitacora muestra por que el router recomendaria un perfil IA, si hubo alternativa, presupuesto, escalado por riesgo o bloqueo seguro. En este hito solo hay decisiones locales o ejemplos simulados claramente marcados.</p>
      <p>{{ConceptHint("Bloqueo seguro", "Si falta configuracion, presupuesto o seguridad suficiente, la decision se bloquea en lugar de avanzar.")}}</p>
      <p><a class="button" href="/">Volver al inicio</a> <a class="button" href="/pilot/legacy/pilot/legacy/pilot/legacy/pilot/legacy/ai/config">Ver configuracion IA</a></p>
    </section>

    <section class="card">
      <h2>Decisiones del router IA</h2>
      {{EmptyStateNotice(audits.Count == 0, "Todavia no hay decisiones de IA registradas. Aca apareceran recomendaciones, bloqueos y motivos del router. Esta pantalla es solo lectura y no llama a proveedores.")}}
      <table>
        <thead>
          <tr><th>Auditoria</th><th>Perfil</th><th>Proveedor/modelo</th><th>Tarea/riesgo</th><th>Vision</th><th>Aprobacion humana</th><th>Alternativa</th><th>Presupuesto</th><th>Costo/tokens</th><th>Estado/motivo</th></tr>
        </thead>
        <tbody>
          {{AIAuditRows(audits)}}
        </tbody>
      </table>
    </section>
  </main>
</body>
</html>
""";
    }

    public static string RenderRecipeList(IReadOnlyList<RecipeEditorModel> recipes, string? message = null)
    {
        return $$"""
<!doctype html>
<html lang="es">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <title>ONE BRAIN Pilot - Tareas automatizables</title>
  {{SharedPilotStyle()}}
</head>
<body>
  <main>
    {{PilotChrome("Tareas")}}
    <section class="card">
      <p><span class="badge safe">lista permitida</span> <span class="badge disabled">solo borradores</span> <span class="badge blocked">sin comandos arbitrarios</span></p>
      <h1>Tareas automatizables</h1>
      <p>Una tarea automatizable es una accion permitida y revisable. Nombre tecnico: recipe. Esta pantalla deja inspeccionar tareas permitidas y crear borradores seguros de metadata sin tocar el JSON estable ni ejecutar nada desde el editor.</p>
      <p>{{ConceptHint("Diagnostico", "Modo controlado para registrar observaciones cuando un sitio externo bloquea o no muestra el contenido esperado.")}}</p>
      <p><a class="button" href="/">Volver al inicio</a> <a class="button" href="/variables">Siguiente paso recomendado: revisar datos</a></p>
      {{MessageBlock(message)}}
    </section>
    <section class="card">
      <h2>Tareas permitidas</h2>
      {{EmptyStateNotice(recipes.Count == 0, "Todavia no hay tareas cargadas en esta vista. Aca apareceran tareas permitidas y revisables. Podes volver a la guia para seguir el recorrido.")}}
      <table>
        <thead><tr><th>Tarea</th><th>Riesgo</th><th>Confianza</th><th>Ruta</th><th>Pasos</th><th>Acciones</th></tr></thead>
        <tbody>{{RecipeListRows(recipes)}}</tbody>
      </table>
    </section>
  </main>
</body>
</html>
""";
    }

    public static string RenderRecipeDetail(
        RecipeEditorModel recipe,
        RecipeValidationResult validation,
        IReadOnlyList<RecipeVariableDefinition> variables,
        RecipeDraft? draft = null,
        RecipeDraftArtifactWriteResult? draftWrite = null)
    {
        return $$"""
<!doctype html>
<html lang="es">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <title>ONE BRAIN Pilot - Detalle de tarea</title>
  {{SharedPilotStyle()}}
</head>
<body>
  <main>
    {{PilotChrome("Detalle de tarea")}}
    <section class="card">
      <p><span class="badge safe">solo campos seguros</span> <span class="badge info">borrador de evidencia</span> <span class="badge blocked">sin editar acciones</span></p>
      <h1>{{Html(recipe.Title)}}</h1>
      <p>{{Html(recipe.Description)}}</p>
      <p>{{ConceptHint("Confianza", "Resume si la tarea esta nueva, supervisada o estable, y ayuda a decidir cuanto control humano conviene mantener.")}}</p>
      <p><a class="button" href="/recipes">Volver a tareas automatizables</a> <a class="button" href="/recipes/{{Html(recipe.RecipeId)}}/variables">Ver datos de esta tarea</a></p>
      {{DraftResultBlock(draft, draftWrite)}}
    </section>
    <section class="grid">
      <div class="card">
        <h2>Resumen de la tarea</h2>
        <div class="metric"><span>ID tecnico</span><strong>{{Html(recipe.RecipeId)}}</strong></div>
        <div class="metric"><span>Nivel de riesgo</span><strong>{{Html(recipe.RiskLevel)}}</strong></div>
        <div class="metric"><span>Confianza</span><strong>{{Html(recipe.ConfidenceStatus)}}</strong></div>
        <div class="metric"><span>Ruta</span><strong class="path">{{Html(recipe.RecipePath)}}</strong></div>
      </div>
      <div class="card">
        <h2>Borrador seguro</h2>
        <form method="post" action="/recipes/{{Html(recipe.RecipeId)}}/edit">
          <label>Titulo<input name="title" value="{{Html(recipe.Title)}}"></label>
          <label>Descripcion<textarea name="description">{{Html(recipe.Description)}}</textarea></label>
          <label>Etiquetas CSV<input name="tags" value="{{Html(string.Join(", ", recipe.Tags))}}"></label>
          <label>Notas CSV<input name="notes" value="{{Html(string.Join(", ", recipe.Notes))}}"></label>
          <input type="hidden" name="unsafe.kind" value="">
          <p><button type="submit">Guardar draft candidato</button></p>
        </form>
        <p>Campos como tipo de paso, argumentos, rutas, comandos, acciones de navegador, login, cookies, pago, compra, envio y click no se editan libremente desde Pilot.</p>
      </div>
    </section>
    <section class="card">
      <h2>Pasos explicados para humanos</h2>
      <table>
        <thead><tr><th>#</th><th>ID de paso</th><th>Tipo</th><th>Etiqueta</th><th>Riesgo</th><th>Aprobacion</th></tr></thead>
        <tbody>{{RecipeStepRows(recipe.Steps)}}</tbody>
      </table>
    </section>
    <section class="grid">
      <div class="card">
        <h2>Resultado del validador</h2>
        <p>Puede ejecutarse: <strong>{{SpanishBool(validation.CanRun)}}</strong> / Puede promoverse: <strong>{{SpanishBool(validation.CanPromote)}}</strong></p>
        <table>
          <thead><tr><th>Severidad</th><th>Codigo</th><th>Campo</th><th>Mensaje</th><th>Correccion</th></tr></thead>
          <tbody>{{ValidationRows(validation.Issues)}}</tbody>
        </table>
      </div>
      <div class="card">
        <h2>Variables de esta tarea</h2>
        {{EmptyStateNotice(variables.Count == 0, "Esta tarea no expone datos detectados en este momento. Eso no habilita ejecucion extra ni edicion peligrosa. Volve a la guia para seguir el recorrido.")}}
        <table>
          <thead><tr><th>Nombre</th><th>Tipo</th><th>Requerido</th><th>Sensibilidad</th><th>Valor</th></tr></thead>
          <tbody>{{VariableRows(variables)}}</tbody>
        </table>
      </div>
    </section>
  </main>
</body>
</html>
""";
    }

    public static string RenderVariables(IReadOnlyList<RecipeVariableDefinition> variables, string? title = null)
    {
        return $$"""
<!doctype html>
<html lang="es">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <title>ONE BRAIN Pilot - Datos de la tarea</title>
  {{SharedPilotStyle()}}
</head>
<body>
  <main>
    {{PilotChrome("Datos de la tarea")}}
    <section class="card">
      <p><span class="badge safe">sin ejecucion</span> <span class="badge info">valores sensibles enmascarados</span> <span class="badge blocked">sin secretos normales</span></p>
      <h1>{{Html(title ?? "Datos de la tarea")}}</h1>
      <p>Los datos de la tarea son valores que una tarea necesita para preparar reportes o ejecutarse de forma controlada. Nombre tecnico: variables. Los valores sensibles se muestran enmascarados y los secretos no deben vivir como texto plano dentro de una tarea.</p>
      <p>{{ConceptHint("Sensibilidad", "Cada dato puede ser publico, interno, sensible o secreto. Eso define como se muestra y valida.")}}</p>
      <p><a class="button" href="/">Volver al inicio</a> <a class="button" href="/recipes">Ver tareas</a> <a class="button" href="/approvals/demo">Siguiente paso recomendado: aprobaciones</a></p>
    </section>
    <section class="card">
      <h2>Datos detectados</h2>
      {{EmptyStateNotice(variables.Count == 0, "No se detectaron datos en esta vista. Aca apareceran datos requeridos por tareas permitidas. Pilot no inventa valores ni rellena secretos automaticamente.")}}
      <table>
        <thead><tr><th>Nombre</th><th>Tipo</th><th>Requerido</th><th>Valor por defecto/ejemplo</th><th>Sensibilidad</th><th>Reglas</th></tr></thead>
        <tbody>{{VariableManagerRows(variables)}}</tbody>
      </table>
    </section>
  </main>
</body>
</html>
""";
    }

    public static string RenderProcessMemory(IReadOnlyList<ProcessMemoryEntry> entries, WorkflowRetrievalResult retrieval, string dataOrigin = PilotDataOrigins.Runtime)
    {
        return $$"""
<!doctype html>
<html lang="es">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <title>ONE BRAIN Pilot - Procesos aprendidos</title>
  {{SharedPilotStyle()}}
</head>
<body>
  <main>
    {{PilotChrome("Procesos aprendidos")}}
    <section class="card">
      {{OriginBanner(dataOrigin)}}
      <p><span class="badge info">solo busqueda</span> <span class="badge safe">sin ejecucion</span> <span class="badge disabled">sin embeddings</span> <span class="badge safe">sin llamada a OpenAI</span></p>
      <h1>Procesos aprendidos</h1>
      <p>Esta memoria ayuda a buscar procesos observados o aprendidos por texto, etiqueta, app o dominio. La busqueda es deterministica y solo sugiere; no ejecuta flujos.</p>
      <p>{{ConceptHint("Busqueda de procesos", "Busqueda de procesos parecidos para orientar al usuario. No dispara tareas ni reproduce acciones.")}}</p>
      <p><a class="button" href="/">Volver al inicio</a> <a class="button" href="/app-profiles">Siguiente paso recomendado: apps y sitios</a></p>
      <form method="get" action="/memory/search" class="grid">
        <label>Texto<input name="q" value="{{Html(retrieval.Query.Text)}}"></label>
        <label>Etiqueta<input name="tag" value="{{Html(string.Join(',', retrieval.Query.Tags ?? []))}}"></label>
        <label>App/sitio<input name="appOrSite" value="{{Html(retrieval.Query.AppOrSite)}}"></label>
        <label>Dominio<input name="domain" value="{{Html(retrieval.Query.Domain)}}"></label>
        <label>Estado<input name="status" value="{{Html(retrieval.Query.Status)}}"></label>
          <p><button type="submit">Buscar en memoria</button></p>
      </form>
    </section>
    <section class="grid">
      <div class="card">
        <h2>Entradas de memoria</h2>
        {{EmptyStateNotice(entries.Count == 0, "Todavia no hay procesos aprendidos cargados en esta vista. Aca apareceran observaciones y flujos candidatos cuando existan datos locales.")}}
        <table>
          <thead><tr><th>Proceso</th><th>Estado</th><th>App/sitio</th><th>Dominio</th><th>Riesgo</th><th>Confianza</th><th>Etiquetas</th></tr></thead>
          <tbody>{{ProcessMemoryRows(entries)}}</tbody>
        </table>
      </div>
      <div class="card">
        <h2>Resultados de busqueda</h2>
        {{EmptyStateNotice(retrieval.Matches.Count == 0, "La busqueda no encontro procesos parecidos con este criterio. Aca apareceran sugerencias cuando haya procesos relacionados. Pilot no ejecuta nada igual.")}}
        <table>
          <thead><tr><th>Coincidencia</th><th>Puntaje</th><th>Seguro</th><th>Revision</th><th>Vinculos</th><th>Motivos</th></tr></thead>
          <tbody>{{WorkflowRetrievalRows(retrieval.Matches)}}</tbody>
        </table>
      </div>
    </section>
  </main>
</body>
</html>
""";
    }

    public static string RenderProcessMemoryDetail(ProcessMemoryEntry? entry, string dataOrigin = PilotDataOrigins.Runtime)
    {
        if (entry == null)
            return RenderNotFound("Proceso aprendido no encontrado", "/memory", "Volver a procesos");

        return $$"""
<!doctype html>
<html lang="es">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <title>ONE BRAIN Pilot - Detalle de proceso aprendido</title>
  {{SharedPilotStyle()}}
</head>
<body>
  <main>
    {{PilotChrome("Detalle de proceso")}}
    <section class="card">
      {{OriginBanner(dataOrigin)}}
      <p><span class="badge info">detalle de proceso</span> <span class="badge safe">sin ejecucion</span> <span class="badge warn">revision humana para sugerencias riesgosas</span></p>
      <h1>{{Html(entry.Title)}}</h1>
      <p>{{Html(entry.Description)}}</p>
      <p><a class="button" href="/memory">Volver a memoria</a></p>
    </section>
    <section class="grid">
      <div class="card">
        <h2>Resumen del proceso</h2>
        <div class="metric"><span>ID</span><strong>{{Html(entry.Id)}}</strong></div>
        <div class="metric"><span>Estado</span><strong>{{Html(entry.Status)}}</strong></div>
        <div class="metric"><span>Origen</span><strong>{{Html(entry.Source)}}</strong></div>
        <div class="metric"><span>App/sitio</span><strong>{{Html(entry.AppOrSite)}}</strong></div>
        <div class="metric"><span>Dominio</span><strong>{{Html(entry.Domain)}}</strong></div>
        <div class="metric"><span>Riesgo / confianza</span><strong>{{Html(entry.RiskLevel)}} / {{entry.ConfidenceScore}}</strong></div>
      </div>
      <div class="card">
        <h2>Vinculos y referencias</h2>
        <ul>{{ProcessMemoryLinkRows(entry)}}</ul>
      </div>
    </section>
    <section class="grid">
      <div class="card">
        <h2>Resumen de pasos</h2>
        <ul>{{ListRows(entry.Summary.StepSummaries)}}</ul>
      </div>
      <div class="card">
        <h2>Evidencia generada</h2>
        <ul>{{ProcessMemoryEvidenceRows(entry)}}</ul>
      </div>
    </section>
  </main>
</body>
</html>
""";
    }

    public static string RenderAppProfiles(IReadOnlyList<AppProfile> profiles, string dataOrigin = PilotDataOrigins.Runtime)
    {
        return $$"""
<!doctype html>
<html lang="es">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <title>ONE BRAIN Pilot - Apps y sitios</title>
  {{SharedPilotStyle()}}
</head>
<body>
  <main>
    {{PilotChrome("Apps y sitios")}}
    <section class="card">
      {{OriginBanner(dataOrigin)}}
      <p><span class="badge info">gestor de perfiles v0</span> <span class="badge safe">sin ejecucion</span> <span class="badge blocked">login/cookies/pago/compra bloqueados</span></p>
      <h1>Apps y sitios</h1>
      <p>Los perfiles de apps y sitios describen que sabe hacer ONE BRAIN con una app o sitio y que bloquea por politica. Los perfiles externos fragiles requieren diagnostico controlado; no habilitan acciones inseguras por defecto.</p>
      <p>{{ConceptHint("Apps y sitios", "Nombre tecnico: app profile. Ficha versionada con capacidades, bloqueos y politica de riesgo para una app, web app o simulacion segura.")}}</p>
      <p><a class="button" href="/">Volver al inicio</a> <a class="button" href="/memory">Ver memoria de procesos</a></p>
    </section>
    <section class="card">
      <h2>Perfiles disponibles</h2>
        {{EmptyStateNotice(profiles.Count == 0, "No hay perfiles cargados en esta vista. Aca apareceran apps y sitios soportados cuando existan perfiles locales. Eso no habilita clicks reales ni acciones inseguras.")}}
      <table>
        <thead><tr><th>Perfil</th><th>Tipo</th><th>Estado</th><th>Dominio/proceso</th><th>Capacidades</th><th>Politica</th><th>Validacion</th></tr></thead>
        <tbody>{{AppProfileRows(profiles)}}</tbody>
      </table>
    </section>
  </main>
</body>
</html>
""";
    }

    public static string RenderAppProfileDetail(AppProfile? profile, string dataOrigin = PilotDataOrigins.Runtime)
    {
        if (profile == null)
            return RenderNotFound("App o sitio no encontrado", "/app-profiles", "Volver a apps y sitios");

        var validation = AppProfilePolicy.Validate(profile);
        return $$"""
<!doctype html>
<html lang="es">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <title>ONE BRAIN Pilot - Detalle de app o sitio</title>
  {{SharedPilotStyle()}}
</head>
<body>
  <main>
    {{PilotChrome("Detalle de app o sitio")}}
    <section class="card">
      {{OriginBanner(dataOrigin)}}
      <p><span class="badge safe">solo lectura por defecto</span> <span class="badge info">politica de diagnostico visible</span> <span class="badge blocked">acciones inseguras bloqueadas</span></p>
      <h1>{{Html(profile.Name)}}</h1>
      <p>{{ConceptHint("Solo lectura", "Capacidad restringida a observacion o lectura. No implica envio, compra, pago ni login.")}}</p>
      <p><a class="button" href="/app-profiles">Volver a perfiles de apps</a></p>
    </section>
    <section class="grid">
      <div class="card">
        <h2>Perfil</h2>
        <div class="metric"><span>ID</span><strong>{{Html(profile.Id)}}</strong></div>
        <div class="metric"><span>Tipo/estado</span><strong>{{Html(profile.Kind)}} / {{Html(profile.Status)}}</strong></div>
        <div class="metric"><span>Dominio</span><strong>{{Html(profile.SiteDomain ?? "-")}}</strong></div>
        <div class="metric"><span>Proceso</span><strong>{{Html(profile.ProcessName ?? "-")}}</strong></div>
        <div class="metric"><span>Version</span><strong>{{profile.Version.Version}} / {{Html(profile.Version.Status)}}</strong></div>
      </div>
      <div class="card">
        <h2>Politica de riesgo</h2>
        <ul>
          <li>Solo lectura por defecto: {{SpanishBool(profile.RiskPolicy.ReadOnlyByDefault)}}</li>
          <li>Diagnostico permitido: {{SpanishBool(profile.RiskPolicy.DiagnosticAllowed)}}</li>
          <li>Envio requiere aprobacion: {{SpanishBool(profile.RiskPolicy.RequiresApprovalForSubmit)}}</li>
          <li>Bloquea login: {{SpanishBool(profile.RiskPolicy.BlocksLogin)}}</li>
          <li>Bloquea cookies: {{SpanishBool(profile.RiskPolicy.BlocksCookies)}}</li>
          <li>Bloquea pago: {{SpanishBool(profile.RiskPolicy.BlocksPayment)}}</li>
          <li>Bloquea compra: {{SpanishBool(profile.RiskPolicy.BlocksPurchase)}}</li>
          <li>Permite click seguro: {{SpanishBool(profile.RiskPolicy.AllowsSafeClick)}}</li>
        </ul>
      </div>
    </section>
    <section class="grid">
      <div class="card">
        <h2>Capacidades</h2>
        <ul>{{ListRows(profile.SupportedCapabilities)}}</ul>
      </div>
      <div class="card">
        <h2>Resultado de validacion</h2>
        <p>Puede activarse: <strong>{{SpanishBool(validation.CanActivate)}}</strong> / Requiere validacion antes de promocionar: <strong>{{SpanishBool(validation.RequiresValidationBeforePromotion)}}</strong></p>
        <table>
          <thead><tr><th>Severidad</th><th>Codigo</th><th>Mensaje</th><th>Como corregir</th></tr></thead>
          <tbody>{{AppProfileIssueRows(validation.Issues)}}</tbody>
        </table>
      </div>
    </section>
  </main>
</body>
</html>
""";
    }

    private static string RenderNotFound(string title, string backPath, string backLabel)
    {
        return $$"""
<!doctype html>
<html lang="es">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <title>ONE BRAIN Pilot - No encontrado</title>
  {{SharedPilotStyle()}}
</head>
<body>
  <main>
    {{PilotChrome("No encontrado")}}
    <section class="card">
      <p><span class="badge warn">no encontrado</span> <span class="badge safe">sin ejecucion</span></p>
      <h1>{{Html(title)}}</h1>
      <p>El elemento pedido no existe en las simulaciones seguras locales ni en la evidencia generada disponible ahora. Esto no dispara busquedas externas ni ejecucion automatica.</p>
      <p><a class="button" href="{{Html(backPath)}}">{{Html(backLabel)}}</a> <a class="button" href="/">Volver al inicio</a></p>
    </section>
  </main>
</body>
</html>
""";
    }

    private static string PilotChrome(string currentPage)
    {
        return $$"""
<section class="card" aria-label="Navegacion y seguridad de Pilot">
  <p><span class="badge info">Pantalla: {{Html(currentPage)}}</span> <span class="badge safe">interfaz local</span> <span class="badge safe">sin apertura automatica</span></p>
  <nav style="display:flex;flex-wrap:wrap;gap:10px;margin:10px 0 14px">
    <a class="button" href="/">Inicio</a>
    <a class="button" href="/guia">Guiarme paso a paso</a>
    <a class="button" href="/recipes">Tareas</a>
    <a class="button" href="/runs">Historial</a>
    <a class="button" href="/pilot/legacy/pilot/legacy/pilot/legacy/pilot/legacy/ai/config">Configuracion</a>
  </nav>
  <p>Links secundarios: <a href="/variables">datos</a> - <a href="/flows">flujos supervisados</a> - <a href="/playback/demo">playback demo</a> - <a href="/executor-harness">click benigno supervisado</a> - <a href="/approvals/demo">aprobaciones</a> - <a href="/ai/audit">decisiones de IA</a> - <a href="/memory">procesos aprendidos</a> - <a href="/app-profiles">apps y sitios</a></p>
  {{SafetyAlwaysVisible()}}
</section>
""";
    }

    private static string OriginBanner(string origin)
    {
        if (PilotDataOrigins.IsDemoLike(origin))
        {
            return $$"""
<div class="notice" aria-label="Origen de datos demo">
  <p><span class="badge warn">MODO DEMO / SIMULACION SEGURA</span> <span class="badge disabled">Origen: {{Html(PilotDataOrigins.Label(origin))}}</span> <span class="badge safe">Sin ejecucion real</span></p>
  <p>Estos datos no vienen de una ejecucion real del usuario. Sirven para mostrar como funciona ONE BRAIN.</p>
  <p>No hay datos reales todavia. Mostrando ejemplo de demostracion. No se hacen clicks, no se envian mensajes, no se compra, no se paga y no se inicia sesion.</p>
</div>
""";
        }

        return $$"""
<div class="notice" aria-label="Origen de datos runtime">
  <p><span class="badge safe">Dato real</span> <span class="badge info">Origen: {{Html(PilotDataOrigins.Label(origin))}}</span> <span class="badge safe">Evidencia runtime</span></p>
  <p>Origen: Datos locales generados por esta instalacion. Las rutas se muestran para copiar manualmente; Pilot no abre evidencia automaticamente.</p>
</div>
""";
    }

    private static string SafetyAlwaysVisible()
    {
        return """
<p aria-label="Always visible safety summary">
  <span class="badge safe">Modo seguro visible</span>
  <span class="badge safe">0 clicks</span>
  <span class="badge safe">0 cookies aceptadas</span>
  <span class="badge safe">0 login</span>
  <span class="badge safe">0 carrito</span>
  <span class="badge safe">0 compra</span>
  <span class="badge safe">0 pago</span>
</p>
<p>ONE BRAIN no hace clicks, no inicia sesion, no acepta cookies, no compra, no paga y no envia nada sin aprobacion.</p>
""";
    }

    private static string HelpTooltip(string label, string description)
    {
        return $$"""<span class="help-inline"><span class="badge warn" title="{{Html(description)}}">?</span><span title="{{Html(description)}}">{{Html(label)}}</span></span>""";
    }

    private static string HelpBox(string title, string description)
    {
        return $$"""
<div class="help-box">
  <p><span class="badge info">i</span> <strong>{{Html(title)}}</strong></p>
  <p>{{Html(description)}}</p>
</div>
""";
    }

    private static string HelpText(string summary, string description)
    {
        return $$"""
<details class="help-text">
  <summary>{{Html(summary)}}</summary>
  <p>{{Html(description)}}</p>
</details>
""";
    }

    private static string ConceptHint(string term, string description)
    {
        return $$"""
<div class="help-box">
  <p><span class="badge info">i</span> <strong>{{Html(term)}}</strong></p>
  <p>{{Html(description)}}</p>
</div>
""";
    }

    private static string GuideStepCard(int number, string title, string description, string href, string badge, string badgeClass)
    {
        return $$"""
<div class="step-card">
  <span class="step-number">{{number}}</span>
  <h3>{{Html(title)}}</h3>
  <p>{{Html(description)}}</p>
  <p><span class="badge {{Html(badgeClass)}}">{{Html(badge)}}</span></p>
  <p><a class="button ghost" href="{{Html(href)}}">Abrir paso</a></p>
</div>
""";
    }

    private static string GuideBadgeClass(int step)
    {
        return step switch
        {
            1 or 5 => "safe",
            2 or 6 or 7 => "info",
            3 => "warn",
            4 => "approval",
            _ => "disabled"
        };
    }

    private static string GuideBadgeLabel(int step)
    {
        return step switch
        {
            1 => "Demo segura",
            2 => "Informacion",
            3 => "Requiere revision",
            4 => "Requiere aprobacion humana",
            5 => "Solo lectura",
            6 => "Evidencia generada",
            7 => "Configuracion",
            _ => "Simulacion segura"
        };
    }

    private static string GuideLookFor(int step)
    {
        return step switch
        {
            1 => "nombre, descripcion, riesgo y acciones permitidas.",
            2 => "datos requeridos, sensibilidad y valores enmascarados.",
            3 => "errores, bloqueos y recomendacion del validador.",
            4 => "accion sensible, motivo de rechazo y ejecucion real desactivada.",
            5 => "plan seguro, resultado y ruta del reporte HTML.",
            6 => "estado, salida, seguridad y rutas de evidencia generada.",
            7 => "perfil recomendado, riesgo, presupuesto y motivo de bloqueo.",
            _ => "capacidades permitidas, diagnostico y bloqueos por politica."
        };
    }

    private static string EmptyStateNotice(bool isEmpty, string message)
    {
        if (!isEmpty)
            return string.Empty;

        return "<p class=\"notice\">" + Html(message) + "</p>";
    }

    private static string ExecutionStatusBlock(PilotExecutionResult? result)
    {
        if (result == null)
            return "<p><span class=\"badge warn\">Todavia no hay una ejecucion UI</span> Elegi una accion segura o escribi una tarea para ver el plan.</p>";

        var badgeClass = result.Success ? "safe" : "risk";
        var status = result.Success ? "OK" : "fallida";
        return $$"""
<p><span class="badge {{badgeClass}}">Estado {{Html(status)}}</span> <strong>{{Html(result.Status)}}</strong></p>
<p>Tarea ejecutada: <span class="path">{{Html(result.RecipePath ?? "-")}}</span></p>
<p>Codigo de salida: <strong>{{Html(result.ExitCode?.ToString() ?? "-")}}</strong></p>
""";
    }

    private static string CopyPathField(string label, string? value)
    {
        var displayValue = string.IsNullOrWhiteSpace(value) ? "-" : value;
        return $$"""
<label>{{Html(label)}} <small>Selecciona y copia manualmente; Pilot nunca abre archivos automaticamente.</small>
  <input readonly value="{{Html(displayValue)}}">
</label>
""";
    }

    private static string TimelineRows(RecipeTimeline timeline)
    {
        var builder = new StringBuilder();
        foreach (var step in timeline.Steps)
        {
            var riskClass = step.RiskLevel == "high" ? "risk" : "safe";
            builder.Append("<tr>")
                .Append("<td>").Append(step.StepNumber).Append("</td>")
                .Append("<td>").Append(step.OffsetMs).Append("ms</td>")
                .Append("<td>").Append(Html(step.EventType)).Append("</td>")
                .Append("<td>").Append(Html(step.WindowOrApp)).Append("</td>")
                .Append("<td>").Append(Html(step.ElementSummary)).Append("</td>")
                .Append("<td>").Append(step.Confidence.ToString("0.00")).Append("</td>")
                .Append("<td>").Append(Html(step.SuggestedActionLabel)).Append("</td>")
                .Append("<td><span class=\"badge ").Append(riskClass).Append("\">").Append(Html(TranslateRisk(step.RiskLevel))).Append("</span></td>")
                .Append("<td>").Append(step.RequiresApproval ? "requiere aprobacion" : "no requiere").Append("</td>")
                .Append("</tr>");
        }

        return builder.ToString();
    }

    private static string AnnotationRows(RecipeTimeline timeline)
    {
        var builder = new StringBuilder();
        foreach (var annotation in timeline.Annotations)
        {
            builder.Append("<li>")
                .Append(Html(annotation.AnnotationType))
                .Append(" paso ")
                .Append(Html(annotation.StepNumber?.ToString() ?? "-"))
                .Append(": ")
                .Append(Html(annotation.Text))
                .Append("</li>");
        }

        return builder.Length == 0 ? "<li>Todavia no hay anotaciones.</li>" : builder.ToString();
    }

    private static string QuickAction(string label, string task)
    {
        return $$"""
<form method="post" action="/plan">
  <input type="hidden" name="task" value="{{Html(task)}}">
  <button type="submit" formaction="/plan">{{Html(label)}} / ver plan</button>
  <button type="submit" formaction="/run" class="secondary">{{Html(label)}} / ejecutar</button>
</form>
""";
    }

    private static string PlanList(PilotPlan? plan)
    {
        if (plan == null)
            return "<p>Todavia no hay plan.</p>";

        var builder = new StringBuilder("<ol>");
        foreach (var step in plan.Steps)
            builder.Append("<li>").Append(Html(step)).Append("</li>");
        builder.Append("</ol>");
        return builder.ToString();
    }

    private static string BlockedList(PilotPlan? plan)
    {
        var blocked = plan?.BlockedCapabilities ?? Array.Empty<string>();
        var builder = new StringBuilder("<ul>");
        foreach (var capability in blocked)
            builder.Append("<li>").Append(Html(capability)).Append("</li>");
        builder.Append("</ul>");
        return builder.ToString();
    }

    private static string OutputBlock(PilotExecutionResult? result)
    {
        if (result == null)
            return "<p>Todavia no se ejecuto ninguna tarea.</p>";

        var output = string.IsNullOrWhiteSpace(result.StandardError)
            ? result.StandardOutput
            : result.StandardOutput + Environment.NewLine + result.StandardError;

        if (string.IsNullOrWhiteSpace(output))
            return "<p>El proceso no devolvio salida visible.</p>";

        return "<code>" + Html(output.Trim()) + "</code>";
    }

    private static string AIProfileRows(IReadOnlyList<AIModelProfile> profiles)
    {
        var builder = new StringBuilder();
        foreach (var profile in profiles)
        {
            var status = BuildAIProfileStatus(profile);
            var statusClass = status == "configured" ? "safe" : "risk";
            builder.Append("<tr>")
                .Append("<td>").Append(Html(profile.DisplayName)).Append("<br><small>").Append(Html(profile.ProfileId)).Append("</small></td>")
                .Append("<td>").Append(Html(profile.Provider)).Append("</td>")
                .Append("<td>").Append(Html(string.IsNullOrWhiteSpace(profile.Model) ? "[not configured]" : profile.Model)).Append("</td>")
                .Append("<td>").Append(Html(profile.ApiKeyMasked)).Append("<br><small>").Append(Html(profile.ApiKeySecretName)).Append("</small></td>")
                .Append("<td>").Append(profile.Enabled ? "activo" : "deshabilitado").Append("</td>")
                .Append("<td>").Append(profile.MonthlyBudgetUsd.ToString("0.00")).Append("</td>")
                .Append("<td>").Append(profile.MaxCostPerTaskUsd.ToString("0.00")).Append("</td>")
                .Append("<td>").Append(Html(profile.MaxRiskLevel)).Append("</td>")
                .Append("<td>").Append(Html(string.IsNullOrWhiteSpace(profile.FallbackProfileId) ? "-" : profile.FallbackProfileId)).Append("</td>")
                .Append("<td><span class=\"badge ").Append(statusClass).Append("\">").Append(Html(status == "configured" ? "configurado" : "incompleto")).Append("</span></td>")
                .Append("<td>").Append(profile.UsageLoggingEnabled ? "activo" : "deshabilitado").Append("</td>")
                .Append("</tr>");
        }

        return builder.ToString();
    }

    private static string AIRoutingResultBlock(AIModelRouterResult? result)
    {
        if (result == null)
            return "<p>Todavia no se ejecuto una prueba en seco.</p>";

        return "<code>" + Html($"""
status={result.Decision.Status}
success={result.Decision.Success}
selectedProfile={result.Decision.SelectedProfileId ?? "-"}
reason={result.Decision.Reason}
failClosed={result.Decision.FailClosed}
wouldCallProvider={result.Decision.WouldCallProvider}
""") + "</code>";
    }

    private static string RunHistoryRows(IReadOnlyList<RunHistoryRecord> runs)
    {
        if (runs.Count == 0)
            return "<tr><td colspan=\"10\">Todavia no hay historial de ejecuciones.</td></tr>";

        var builder = new StringBuilder();
        foreach (var run in runs)
        {
            var statusClass = run.Status is "succeeded" or "diagnostic" ? "safe" : run.Status == "blocked" ? "warn" : "risk";
            builder.Append("<tr>")
                .Append("<td>").Append(Html(run.RunId)).Append("</td>")
                .Append("<td><span class=\"badge ").Append(statusClass).Append("\">").Append(Html(run.Status)).Append("</span></td>")
                .Append("<td>").Append(Html(run.StartedAtUtc)).Append("<br>").Append(Html(run.EndedAtUtc ?? "-")).Append("</td>")
                .Append("<td>").Append(Html(run.Source)).Append("</td>")
                .Append("<td>").Append(Html(run.RecipeId ?? run.CandidateFlowId ?? "-")).Append("</td>")
                .Append("<td>").Append(Safety(run.SafetyCounters)).Append("</td>")
                .Append("<td>").Append(Html(run.ApprovalDecisionId ?? run.ApprovalRequestId ?? "-")).Append("</td>")
                .Append("<td>").Append(Html(run.ConfidenceId ?? "-")).Append("<br>").Append(Html(run.AiRoutingDecisionId ?? "-")).Append("</td>")
                .Append("<td class=\"path\">").Append(Html(string.Join("\n", run.ArtifactPaths))).Append("</td>")
                .Append("<td>").Append(Html(string.IsNullOrWhiteSpace(run.ErrorSummary) ? "-" : run.ErrorSummary)).Append("</td>")
                .Append("</tr>");
        }

        return builder.ToString();
    }

    private static string AIAuditRows(IReadOnlyList<AIAuditRecord> audits)
    {
        if (audits.Count == 0)
            return "<tr><td colspan=\"10\">Todavia no hay decisiones de IA.</td></tr>";

        var builder = new StringBuilder();
        foreach (var audit in audits)
        {
            var statusClass = audit.ResultStatus is "routed" or "simulado" ? "safe" : "risk";
            builder.Append("<tr>")
                .Append("<td>").Append(Html(audit.AiAuditId)).Append("<br>").Append(Html(audit.TimestampUtc)).Append("</td>")
                .Append("<td>").Append(Html(audit.RecommendedProfileId ?? "-")).Append("<br>usado: ").Append(Html(audit.UsedProfileId ?? "-")).Append("</td>")
                .Append("<td>").Append(Html(audit.Provider)).Append("<br>").Append(Html(audit.Model)).Append("</td>")
                .Append("<td>").Append(Html(audit.TaskType)).Append("<br>").Append(Html(audit.RiskLevel)).Append("</td>")
                .Append("<td>").Append(SpanishBool(audit.RequiresVision)).Append("</td>")
                .Append("<td>").Append(SpanishBool(audit.RequiresHumanApproval)).Append("</td>")
                .Append("<td>").Append(audit.FallbackUsed ? Html($"{audit.FallbackFrom} -> {audit.FallbackTo}") : "-").Append("</td>")
                .Append("<td>").Append(Html(audit.BudgetDecision)).Append("</td>")
                .Append("<td>est ").Append(audit.EstimatedCostUsd.ToString("0.0000")).Append("<br>actual ").Append(audit.ActualCostUsd?.ToString("0.0000") ?? "-").Append("<br>tokens ").Append(Html($"{audit.TokensIn?.ToString() ?? "-"} / {audit.TokensOut?.ToString() ?? "-"}")).Append("</td>")
                .Append("<td><span class=\"badge ").Append(statusClass).Append("\">").Append(Html(audit.ResultStatus)).Append("</span><br>").Append(Html(audit.Reason)).Append("</td>")
                .Append("</tr>");
        }

        return builder.ToString();
    }

    private static string RecipeListRows(IReadOnlyList<RecipeEditorModel> recipes)
    {
        if (recipes.Count == 0)
            return "<tr><td colspan=\"6\">No hay tareas permitidas.</td></tr>";

        var builder = new StringBuilder();
        foreach (var recipe in recipes)
        {
            builder.Append("<tr>")
                .Append("<td>").Append(Html(recipe.Title)).Append("<br><small>").Append(Html(recipe.RecipeId)).Append("</small></td>")
                .Append("<td>").Append(Html(recipe.RiskLevel)).Append("</td>")
                .Append("<td>").Append(Html(recipe.ConfidenceStatus)).Append("</td>")
                .Append("<td class=\"path\">").Append(Html(recipe.RecipePath)).Append("</td>")
                .Append("<td>").Append(recipe.Steps.Count).Append("</td>")
                .Append("<td><a class=\"button\" href=\"/recipes/").Append(Html(recipe.RecipeId)).Append("\">Abrir</a></td>")
                .Append("</tr>");
        }

        return builder.ToString();
    }

    private static string RecipeStepRows(IReadOnlyList<RecipeEditorStepSummary> steps)
    {
        var builder = new StringBuilder();
        foreach (var step in steps)
        {
            var riskClass = step.RiskLevel == "high" ? "risk" : "safe";
            builder.Append("<tr>")
                .Append("<td>").Append(step.StepNumber).Append("</td>")
                .Append("<td>").Append(Html(step.StepId ?? "-")).Append("</td>")
                .Append("<td>").Append(Html(step.Kind)).Append("</td>")
                .Append("<td>").Append(Html(step.HumanLabel)).Append("</td>")
                .Append("<td><span class=\"badge ").Append(riskClass).Append("\">").Append(Html(step.RiskLevel)).Append("</span></td>")
                .Append("<td>").Append(step.RequiresApproval ? "requiere aprobacion" : "no requiere").Append("</td>")
                .Append("</tr>");
        }

        return builder.ToString();
    }

    private static string ValidationRows(IReadOnlyList<RecipeValidationIssue> issues)
    {
        if (issues.Count == 0)
            return "<tr><td colspan=\"5\">No hay problemas de validacion.</td></tr>";

        var builder = new StringBuilder();
        foreach (var issue in issues)
        {
            var severityClass = issue.Severity is "blocked" or "error" ? "risk" : issue.Severity == "warning" ? "warn" : "safe";
            builder.Append("<tr>")
                .Append("<td><span class=\"badge ").Append(severityClass).Append("\">").Append(Html(issue.Severity)).Append("</span></td>")
                .Append("<td>").Append(Html(issue.Code)).Append("</td>")
                .Append("<td>").Append(Html(issue.FieldPath)).Append("</td>")
                .Append("<td>").Append(Html(issue.Message)).Append("</td>")
                .Append("<td>").Append(Html(issue.Remediation)).Append("</td>")
                .Append("</tr>");
        }

        return builder.ToString();
    }

    private static string VariableRows(IReadOnlyList<RecipeVariableDefinition> variables)
    {
        if (variables.Count == 0)
            return "<tr><td colspan=\"5\">No se detectaron datos.</td></tr>";

        var builder = new StringBuilder();
        foreach (var variable in variables)
        {
            builder.Append("<tr>")
                .Append("<td>").Append(Html(variable.Name)).Append("</td>")
                .Append("<td>").Append(Html(variable.Type)).Append("</td>")
                .Append("<td>").Append(SpanishBool(variable.Required)).Append("</td>")
                .Append("<td>").Append(Html(variable.Sensitivity)).Append("</td>")
                .Append("<td>").Append(Html(RecipeVariableManager.DisplayValue(variable))).Append("</td>")
                .Append("</tr>");
        }

        return builder.ToString();
    }

    private static string VariableManagerRows(IReadOnlyList<RecipeVariableDefinition> variables)
    {
        if (variables.Count == 0)
            return "<tr><td colspan=\"6\">No se detectaron datos.</td></tr>";

        var builder = new StringBuilder();
        foreach (var variable in variables)
        {
            builder.Append("<tr>")
                .Append("<td>").Append(Html(variable.Name)).Append("</td>")
                .Append("<td>").Append(Html(variable.Type)).Append("</td>")
                .Append("<td>").Append(SpanishBool(variable.Required)).Append("</td>")
                .Append("<td>").Append(Html(RecipeVariableManager.DisplayValue(variable))).Append("</td>")
                .Append("<td>").Append(Html(variable.Sensitivity)).Append(variable.Redacted ? " / enmascarado" : "").Append("</td>")
                .Append("<td>").Append(Html(variable.Regex ?? "-")).Append("</td>")
                .Append("</tr>");
        }

        return builder.ToString();
    }

    private static string ProcessMemoryRows(IReadOnlyList<ProcessMemoryEntry> entries)
    {
        if (entries.Count == 0)
            return "<tr><td colspan=\"7\">No hay procesos aprendidos.</td></tr>";

        var builder = new StringBuilder();
        foreach (var entry in entries)
        {
            var statusClass = entry.Status is ProcessMemoryStatuses.Stable or ProcessMemoryStatuses.Supervised ? "safe" :
                entry.Status is ProcessMemoryStatuses.Rejected or ProcessMemoryStatuses.Archived ? "risk" : "warn";
            builder.Append("<tr>")
                .Append("<td><a href=\"/memory/").Append(Html(entry.Id)).Append("\">").Append(Html(entry.Title)).Append("</a><br><small>").Append(Html(entry.Id)).Append("</small></td>")
                .Append("<td><span class=\"badge ").Append(statusClass).Append("\">").Append(Html(entry.Status)).Append("</span></td>")
                .Append("<td>").Append(Html(entry.AppOrSite)).Append("</td>")
                .Append("<td>").Append(Html(entry.Domain)).Append("</td>")
                .Append("<td>").Append(Html(entry.RiskLevel)).Append("</td>")
                .Append("<td>").Append(entry.ConfidenceScore).Append("</td>")
                .Append("<td>").Append(Html(string.Join(", ", entry.Tags))).Append("</td>")
                .Append("</tr>");
        }

        return builder.ToString();
    }

    private static string WorkflowRetrievalRows(IReadOnlyList<WorkflowRetrievalMatch> matches)
    {
        if (matches.Count == 0)
            return "<tr><td colspan=\"6\">Todavia no hay coincidencias de busqueda.</td></tr>";

        var builder = new StringBuilder();
        foreach (var match in matches)
        {
            var safeClass = match.SafeToSuggest ? "safe" : "risk";
            builder.Append("<tr>")
                .Append("<td><a href=\"/memory/").Append(Html(match.ProcessMemoryId)).Append("\">").Append(Html(match.Title)).Append("</a><br><small>").Append(Html(match.ProcessMemoryId)).Append("</small></td>")
                .Append("<td>").Append(match.Score.ToString("0.00")).Append("</td>")
                .Append("<td><span class=\"badge ").Append(safeClass).Append("\">").Append(match.SafeToSuggest ? "seguro para sugerir" : "no sugerir").Append("</span></td>")
                .Append("<td>").Append(match.RequiresHumanReview ? "requiere revision" : "no requiere").Append("</td>")
                .Append("<td>").Append(Html(FirstNonEmpty(match.RecipeId, match.CandidateFlowId, match.TimelineId, "-"))).Append("</td>")
                .Append("<td>").Append(Html(string.Join("; ", match.Reasons))).Append("</td>")
                .Append("</tr>");
        }

        return builder.ToString();
    }

    private static string ProcessMemoryLinkRows(ProcessMemoryEntry entry)
    {
        var rows = new[]
        {
            ("recordingSessionId", entry.Links.RecordingSessionId),
            ("timelineId", entry.Links.TimelineId),
            ("candidateFlowId", entry.Links.CandidateFlowId),
            ("recipeDraftId", entry.Links.RecipeDraftId),
            ("recipeId", entry.Links.RecipeId),
            ("approvalRequestId", entry.Links.ApprovalRequestId),
            ("approvalDecisionId", entry.Links.ApprovalDecisionId),
            ("runId", entry.Links.RunId),
            ("aiAuditId", entry.Links.AiAuditId),
            ("confidenceId", entry.Links.ConfidenceId)
        };

        var builder = new StringBuilder();
        foreach (var (name, value) in rows.Where(row => !string.IsNullOrWhiteSpace(row.Item2)))
            builder.Append("<li>").Append(Html(name)).Append(": ").Append(Html(value)).Append("</li>");

        return builder.Length == 0 ? "<li>No hay vinculos todavia.</li>" : builder.ToString();
    }

    private static string ProcessMemoryEvidenceRows(ProcessMemoryEntry entry)
    {
        var builder = new StringBuilder();
        foreach (var link in entry.EvidenceLinks)
            builder.Append("<li>").Append(Html(link.Kind)).Append(": <span class=\"path\">").Append(Html(link.RelativePath)).Append("</span> - ").Append(Html(link.Label)).Append("</li>");
        foreach (var path in entry.ArtifactPaths)
            builder.Append("<li>evidencia generada: <span class=\"path\">").Append(Html(path)).Append("</span></li>");

        return builder.Length == 0 ? "<li>No hay evidencia generada vinculada.</li>" : builder.ToString();
    }

    private static string AppProfileRows(IReadOnlyList<AppProfile> profiles)
    {
        if (profiles.Count == 0)
            return "<tr><td colspan=\"7\">No hay apps ni sitios cargados.</td></tr>";

        var builder = new StringBuilder();
        foreach (var profile in profiles)
        {
            var validation = AppProfilePolicy.Validate(profile);
            var statusClass = validation.CanActivate ? "safe" : "risk";
            builder.Append("<tr>")
                .Append("<td><a href=\"/app-profiles/").Append(Html(profile.Id)).Append("\">").Append(Html(profile.Name)).Append("</a><br><small>").Append(Html(profile.Id)).Append("</small></td>")
                .Append("<td>").Append(Html(profile.Kind)).Append("</td>")
                .Append("<td>").Append(Html(profile.Status)).Append("</td>")
                .Append("<td>").Append(Html(profile.SiteDomain ?? profile.ProcessName ?? "-")).Append("</td>")
                .Append("<td>").Append(Html(string.Join(", ", profile.SupportedCapabilities))).Append("</td>")
                .Append("<td>").Append(Html($"solo lectura={SpanishBool(profile.RiskPolicy.ReadOnlyByDefault)}; diagnostico={SpanishBool(profile.RiskPolicy.DiagnosticAllowed)}; login bloqueado={SpanishBool(profile.RiskPolicy.BlocksLogin)}; pago bloqueado={SpanishBool(profile.RiskPolicy.BlocksPayment)}; compra bloqueada={SpanishBool(profile.RiskPolicy.BlocksPurchase)}")).Append("</td>")
                .Append("<td><span class=\"badge ").Append(statusClass).Append("\">").Append(validation.CanActivate ? "valido" : "bloqueado").Append("</span></td>")
                .Append("</tr>");
        }

        return builder.ToString();
    }

    private static string AppProfileIssueRows(IReadOnlyList<AppProfileValidationIssue> issues)
    {
        if (issues.Count == 0)
            return "<tr><td colspan=\"4\">No hay problemas de validacion.</td></tr>";

        var builder = new StringBuilder();
        foreach (var issue in issues)
        {
            var severityClass = issue.Severity is "blocked" or "error" ? "risk" : issue.Severity == "warning" ? "warn" : "safe";
            builder.Append("<tr>")
                .Append("<td><span class=\"badge ").Append(severityClass).Append("\">").Append(Html(issue.Severity)).Append("</span></td>")
                .Append("<td>").Append(Html(issue.Code)).Append("</td>")
                .Append("<td>").Append(Html(issue.Message)).Append("</td>")
                .Append("<td>").Append(Html(issue.Remediation)).Append("</td>")
                .Append("</tr>");
        }

        return builder.ToString();
    }

    private static string ListRows(IEnumerable<string> items)
    {
        var builder = new StringBuilder();
        foreach (var item in items)
            builder.Append("<li>").Append(Html(item)).Append("</li>");

        return builder.Length == 0 ? "<li>-</li>" : builder.ToString();
    }

    private static string PromotedFlowRows(IReadOnlyList<PromotedCandidateFlow> flows)
    {
        if (flows.Count == 0)
            return "<tr><td colspan=\"7\">Todavia no hay flujos promovidos.</td></tr>";

        var builder = new StringBuilder();
        foreach (var flow in flows)
        {
            builder.Append("<tr>")
                .Append("<td>").Append(Html(flow.Title)).Append("<br><small>").Append(Html(flow.FlowId)).Append("</small></td>")
                .Append("<td><span class=\"badge safe\">").Append(Html(flow.Status)).Append("</span></td>")
                .Append("<td><span class=\"badge ").Append(RiskBadgeClass(flow.RiskLevel)).Append("\">").Append(Html(TranslateRisk(flow.RiskLevel))).Append("</span></td>")
                .Append("<td>").Append(flow.ConfidenceScore).Append("</td>")
                .Append("<td>").Append(flow.RequiresHumanApproval ? "requiere aprobacion" : "no requiere").Append("</td>")
                .Append("<td>").Append(flow.Steps.Count).Append("</td>")
                .Append("<td><a class=\"button\" href=\"/flows/demo\">Detalle</a> <a class=\"button\" href=\"/playback/demo\">Playback</a></td>")
                .Append("</tr>");
        }

        return builder.ToString();
    }

    private static string PromotedFlowStepRows(IReadOnlyList<PromotedFlowStep> steps)
    {
        if (steps.Count == 0)
            return "<tr><td colspan=\"7\">Este flujo no tiene pasos.</td></tr>";

        var builder = new StringBuilder();
        foreach (var step in steps)
        {
            builder.Append("<tr>")
                .Append("<td>").Append(step.StepNumber).Append("</td>")
                .Append("<td>").Append(Html(step.ActionKind)).Append("<br><small>").Append(Html(step.Title)).Append("</small></td>")
                .Append("<td><span class=\"badge ").Append(RiskBadgeClass(step.RiskLevel)).Append("\">").Append(Html(TranslateRisk(step.RiskLevel))).Append("</span></td>")
                .Append("<td>").Append(step.RequiresApproval ? "si" : "no").Append("</td>")
                .Append("<td>").Append(step.HasSafeExecutor ? "si" : "no").Append("</td>")
                .Append("<td>").Append(Html(step.ExecutionMode)).Append("</td>")
                .Append("<td>").Append(step.CanSkip ? "si" : "no").Append("</td>")
                .Append("</tr>");
        }

        return builder.ToString();
    }

    private static string PlaybackStepRows(IReadOnlyList<SupervisedPlaybackStepState> steps)
    {
        if (steps.Count == 0)
            return "<tr><td colspan=\"5\">No hay pasos de playback.</td></tr>";

        var builder = new StringBuilder();
        foreach (var step in steps)
        {
            var statusClass = step.Status switch
            {
                SupervisedPlaybackStepStatuses.Confirmed => "safe",
                SupervisedPlaybackStepStatuses.Skipped => "warn",
                SupervisedPlaybackStepStatuses.Blocked => "blocked",
                SupervisedPlaybackStepStatuses.Aborted => "risk",
                _ => "info"
            };
            builder.Append("<tr>")
                .Append("<td>").Append(step.StepNumber).Append("</td>")
                .Append("<td><span class=\"badge ").Append(statusClass).Append("\">").Append(Html(step.Status)).Append("</span></td>")
                .Append("<td>").Append(Html(string.IsNullOrWhiteSpace(step.Decision) ? "-" : step.Decision)).Append("</td>")
                .Append("<td>").Append(Html(FirstNonEmpty(step.ApprovalDecisionId, step.ApprovalRequestId, "-"))).Append("</td>")
                .Append("<td>").Append(Html(string.IsNullOrWhiteSpace(step.EvidenceSummary) ? "-" : step.EvidenceSummary)).Append("</td>")
                .Append("</tr>");
        }

        return builder.ToString();
    }

    private static string PromotionResultBlock(CandidateFlowPromotionResult? promotion, PromotedFlowArtifactWriteResult? write)
    {
        if (promotion == null)
            return "";

        var badgeClass = promotion.Success ? "safe" : "blocked";
        var writeDetail = write == null ? "" : write.Success ? $" Evidencia generada: {write.RelativePath}" : $" Error de artifact: {write.Error}";
        return "<p><span class=\"badge " + badgeClass + "\">promocion " + Html(promotion.Status) + "</span> " +
               Html(string.Join("; ", promotion.Issues.Concat(promotion.Notes))) +
               Html(writeDetail) + "</p>";
    }

    private static string PlaybackActionBlock(
        SupervisedPlaybackActionResult? actionResult,
        SupervisedPlaybackArtifactWriteResult? playbackWrite,
        RunHistoryArtifactWriteResult? runWrite)
    {
        if (actionResult == null)
            return "<p class=\"notice\">Todavia no se confirmo ningun paso. Revisa el paso actual y elegi confirmar, saltar o abortar.</p>";

        var badgeClass = actionResult.Success ? "safe" : "blocked";
        var artifacts = new[]
        {
            playbackWrite?.Success == true ? playbackWrite.RelativePath : "",
            runWrite?.Success == true ? runWrite.RelativePath : ""
        }.Where(value => !string.IsNullOrWhiteSpace(value));

        return "<p><span class=\"badge " + badgeClass + "\">" + Html(actionResult.Message) + "</span></p>" +
               "<p>Evidencia: " + Html(string.Join("; ", actionResult.Evidence)) + "</p>" +
               "<p>Rutas de evidencia generada: <span class=\"path\">" + Html(string.Join("; ", artifacts)) + "</span></p>";
    }

    private static string ExecutorHarnessResultBlock(
        ExecutorHarnessRunResult? result,
        ExecutorHarnessArtifactWriteResult? evidenceWrite,
        ApprovalArtifactWriteResult? approvalWrite,
        ApprovalArtifactWriteResult? decisionWrite,
        RunHistoryArtifactWriteResult? runWrite,
        ApprovalDecision? decision)
    {
        if (result == null)
            return """
<section class="card">
  <h2>Resultado y evidencia</h2>
  <p class="notice">Todavia no se ejecuto el harness. Primero revisa el objetivo benigno y confirma la aprobacion humana.</p>
  <p><span class="badge safe">0 cookies</span> <span class="badge safe">0 login</span> <span class="badge safe">0 carrito</span> <span class="badge safe">0 compra</span> <span class="badge safe">0 pago</span></p>
</section>
""";

        var badgeClass = result.Success ? "safe" : "blocked";
        var paths = new[]
        {
            evidenceWrite?.Success == true ? evidenceWrite.RelativePath : "",
            approvalWrite?.Success == true ? approvalWrite.RelativePath : "",
            decisionWrite?.Success == true ? decisionWrite.RelativePath : "",
            runWrite?.Success == true ? runWrite.RelativePath : ""
        }.Where(value => !string.IsNullOrWhiteSpace(value)).ToList();

        return $$"""
<section class="card">
  <h2>Resultado y evidencia</h2>
  <p><span class="badge {{badgeClass}}">{{Html(result.Status)}}</span> {{Html(result.Message)}}</p>
  <p>Decision: <strong>{{Html(decision?.Decision ?? "-")}}</strong> / ExecutionAllowed: <strong>{{SpanishBool(decision?.ExecutionAllowed == true)}}</strong></p>
  <p>Verificacion posterior: target encontrado={{SpanishBool(result.Verification.TargetFound)}}; click observado={{SpanishBool(result.Verification.ClickObserved)}}; estado={{Html(result.Verification.Status)}}.</p>
  <p>Resolucion de target: <strong>{{Html(result.TargetResolution?.Status ?? "-")}}</strong> - {{Html(result.TargetResolution?.Message ?? "-")}}</p>
  <p>Matriz de seguridad: <strong>{{Html(result.SafetyMatrix?.Status ?? "-")}}</strong></p>
  <p>Bloqueos de matriz: {{Html(result.SafetyMatrix == null || result.SafetyMatrix.Blocked.Count == 0 ? "sin bloqueos" : string.Join("; ", result.SafetyMatrix.Blocked))}}</p>
  <p>Safety del run: {{Safety(result.RunHistory.SafetyCounters)}}</p>
  <p>Evidencia: {{Html(string.Join("; ", result.Evidence))}}</p>
  <p>Rutas locales generadas: <span class="path">{{Html(paths.Count == 0 ? "sin artifacts escritos" : string.Join("; ", paths))}}</span></p>
  <p><span class="badge safe">0 cookies</span> <span class="badge safe">0 login</span> <span class="badge safe">0 carrito</span> <span class="badge safe">0 compra</span> <span class="badge safe">0 pago</span></p>
</section>
""";
    }

    private static string ExecutorHarnessDryRunBlock(ExecutorHarnessDryRunExplanation dryRun)
    {
        var contract = dryRun.Contract;
        var badgeClass = dryRun.WouldExecute ? "approval" : "blocked";
        return $$"""
<section class="card">
  <h2>Dry-run explicable antes de ejecutar</h2>
  <p><span class="badge {{badgeClass}}">{{Html(dryRun.Status)}}</span> {{Html(dryRun.Summary)}}</p>
  <div class="metric"><span>Elemento que se tocaria</span><strong>{{Html(dryRun.Element)}}</strong></div>
  <div class="metric"><span>Por que fue seleccionado</span><strong>{{Html(dryRun.SelectionReason)}}</strong></div>
  <div class="metric"><span>Contrato</span><strong>{{Html(contract.ContractId)}}</strong></div>
  <div class="metric"><span>Target resuelto</span><strong>{{Html(contract.ResolvedTarget.Status)}} - {{Html(contract.ResolvedTarget.Message)}}</strong></div>
  <div class="metric"><span>Matriz de seguridad</span><strong>{{Html(contract.SafetyMatrix.Status)}}</strong></div>
  <div class="metric"><span>ExecutionAllowed</span><strong>{{SpanishBool(contract.ApprovalState.ExecutionAllowed)}}</strong></div>
  <div class="metric"><span>Dry-run only</span><strong>{{SpanishBool(contract.PreActionState.DryRunOnly)}}</strong></div>
  <p><strong>Reglas aplicadas:</strong> {{Html(string.Join("; ", dryRun.SafetyRules))}}</p>
  <p><strong>Condiciones que bloquearian:</strong> {{Html(dryRun.BlockingConditions.Count == 0 ? "sin bloqueos" : string.Join("; ", dryRun.BlockingConditions))}}</p>
  <p><strong>Expectativa post-accion:</strong> ventana visible, target visible, nombre {{Html(contract.PostActionExpectation.ExpectedTargetName)}} y {{contract.PostActionExpectation.ExpectedClickCount}} click.</p>
  <p class="notice">Este dry-run no llama al executor UIA, no hace click y no escribe evidencia runtime.</p>
</section>
""";
    }

    private static string ExecutorHarnessReplayBlock(ExecutorHarnessEvidenceReplay replay)
    {
        if (replay.Evidence == null)
        {
            return $$"""
<section class="card">
  <h2>Estado de replay</h2>
  <p><span class="badge disabled">{{Html(replay.Status)}}</span> {{Html(replay.Message)}}</p>
  <p>No hay evidencia runtime del harness todavia. Cuando exista un artifact local, aparecera aca como reconstruccion read-only.</p>
  <p>{{Html(string.Join("; ", replay.Notes))}}</p>
</section>
""";
        }

        var evidence = replay.Evidence;
        var contract = evidence.InteractionContract;
        return $$"""
<section class="card">
  <h2>Artifact reconstruido</h2>
  <p><span class="badge info">{{Html(replay.Status)}}</span> {{Html(replay.Message)}}</p>
  <div class="metric"><span>Ruta de evidencia</span><strong>{{Html(replay.RelativePath)}}</strong></div>
  <div class="metric"><span>Evidence ID</span><strong>{{Html(evidence.EvidenceId)}}</strong></div>
  <div class="metric"><span>Harness</span><strong>{{Html(evidence.HarnessId)}}</strong></div>
  <div class="metric"><span>Approval request</span><strong>{{Html(evidence.ApprovalRequestId)}}</strong></div>
  <div class="metric"><span>Approval decision</span><strong>{{Html(evidence.ApprovalDecisionId ?? "-")}}</strong></div>
  <div class="metric"><span>Trace link</span><strong>{{Html(replay.TraceLink?.TraceId ?? "-")}}</strong></div>
  <div class="metric"><span>Post-state</span><strong>{{Html(evidence.Verification.Status)}} - target={{SpanishBool(evidence.Verification.TargetFound)}} click={{SpanishBool(evidence.Verification.ClickObserved)}}</strong></div>
  <div class="metric"><span>Safety counters</span><strong>{{Safety(evidence.SafetyCounters)}}</strong></div>
  <p><strong>Contrato:</strong> {{Html(contract == null ? "artifact antiguo sin contrato embebido" : contract.ContractId)}}</p>
  <p><strong>Resolucion de target:</strong> {{Html(contract == null ? "-" : $"{contract.ResolvedTarget.Status} - {contract.ResolvedTarget.Message}")}}</p>
  <p><strong>Matriz:</strong> {{Html(contract == null ? "-" : contract.SafetyMatrix.Status)}}</p>
  <p><strong>Comando:</strong> {{Html(contract == null ? "-" : $"{contract.ActionKind} -> {contract.TargetConstraints.TargetRef}")}}</p>
  <p><strong>Run trace:</strong> {{Html(replay.TraceLink == null ? "-" : $"{replay.TraceLink.RunId}; approval={replay.TraceLink.ApprovalDecision}; post={replay.TraceLink.PostStateResult}")}}</p>
  <p><strong>Notas:</strong> {{Html(string.Join("; ", evidence.Notes.Concat(replay.Notes)))}}</p>
</section>
""";
    }

    private static string ExecutorHarnessEvidenceIndexBlock(ExecutorHarnessEvidenceIndex index)
    {
        if (index.Items.Count == 0)
        {
            return $$"""
<section class="card">
  <h2>Estado del indice</h2>
  <p><span class="badge disabled">{{Html(index.Status)}}</span> {{Html(index.Message)}}</p>
  <p>No hay evidencia runtime del harness todavia. Esta pantalla no muestra fixtures como datos reales.</p>
  <p>{{Html(string.Join("; ", index.Notes))}}</p>
</section>
""";
        }

        return $$"""
<section class="card">
  <h2>Evidencia local indexada</h2>
  <p><span class="badge info">{{Html(index.Status)}}</span> {{Html(index.Message)}}</p>
  <table>
    <thead><tr><th>Evidence</th><th>Accion</th><th>Safety</th><th>Verificacion</th><th>Trace</th><th>Ruta</th></tr></thead>
    <tbody>{{ExecutorHarnessEvidenceIndexRows(index.Items)}}</tbody>
  </table>
  <p class="notice">El indice es read-only: no abre artifacts, no hace clicks y no ejecuta replay activo.</p>
</section>
""";
    }

    private static string ExecutorHarnessEvidenceIndexRows(IReadOnlyList<ExecutorHarnessEvidenceIndexItem> items)
    {
        var builder = new StringBuilder();
        foreach (var item in items)
        {
            var trace = item.TraceLink;
            builder.Append("<tr>")
                .Append("<td>").Append(Html(item.EvidenceId)).Append("<br><small>").Append(Html(item.TimestampUtc)).Append("</small></td>")
                .Append("<td>").Append(Html(item.ActionKind)).Append("<br><small>").Append(Html(item.HarnessId)).Append("</small></td>")
                .Append("<td><span class=\"badge ").Append(item.SafetyDecision == "allowed" ? "safe" : "blocked").Append("\">")
                .Append(Html(item.SafetyDecision)).Append("</span></td>")
                .Append("<td>").Append(Html(item.VerificationResult)).Append("</td>")
                .Append("<td>").Append(Html(trace.TraceId)).Append("<br><small>")
                .Append(Html(trace.IsSynthetic ? "trace local sintetico, no run id real completo" : trace.RunId))
                .Append("</small><br><small>")
                .Append(Html($"approval={trace.ApprovalDecision}; post={trace.PostStateResult}"))
                .Append("</small></td>")
                .Append("<td><span class=\"path\">").Append(Html(item.LogicalPath)).Append("</span><br><a href=\"")
                .Append(Html(item.ReplayPath)).Append("\">Replay read-only</a></td>")
                .Append("</tr>");
        }

        return builder.ToString();
    }

    private static string CurrentPlaybackStepBlock(PromotedFlowStep? step, SupervisedPlaybackSession session)
    {
        if (step == null)
            return "<p>No hay paso actual. El flujo puede estar terminado.</p>";

        return $$"""
<div class="metric"><span>Paso</span><strong>{{step.StepNumber}}</strong></div>
<div class="metric"><span>Accion</span><strong>{{Html(step.ActionKind)}}</strong></div>
<div class="metric"><span>Descripcion</span><strong>{{Html(step.Description)}}</strong></div>
<div class="metric"><span>Riesgo</span><strong>{{Html(TranslateRisk(step.RiskLevel))}}</strong></div>
<div class="metric"><span>Aprobacion</span><strong>{{(step.RequiresApproval ? "requiere aprobacion" : "no requiere")}}</strong></div>
<div class="metric"><span>Executor seguro</span><strong>{{SpanishBool(step.HasSafeExecutor)}}</strong></div>
<p class="notice">Si el paso es sensible o no hay executor seguro, ONE BRAIN frena y registra evidencia. No ejecuta acciones reales.</p>
""";
    }

    private static string RiskBadgeClass(string risk)
    {
        return risk?.ToLowerInvariant() switch
        {
            "low" => "safe",
            "medium" => "warn",
            "high" => "approval",
            "critical" => "blocked",
            _ => "info"
        };
    }

    private static string DraftResultBlock(RecipeDraft? draft, RecipeDraftArtifactWriteResult? write)
    {
        if (draft == null)
            return "";

        var status = write?.Success == true ? "Borrador guardado" : "Borrador rechazado";
        var detail = write?.Success == true ? write.RelativePath : write?.Error ?? string.Join("; ", draft.ValidationNotes);
        return "<p><span class=\"badge " + (write?.Success == true ? "safe" : "risk") + "\">" + Html(status) + "</span> " + Html(detail) + "</p>";
    }

    private static string MessageBlock(string? message)
    {
        return string.IsNullOrWhiteSpace(message) ? "" : "<p><span class=\"badge warn\">" + Html(message) + "</span></p>";
    }

    private static string SpanishBool(bool value)
    {
        return value ? "si" : "no";
    }

    private static string TranslateRisk(string? risk)
    {
        return risk?.ToLowerInvariant() switch
        {
            "low" => "bajo",
            "medium" => "medio",
            "high" => "alto",
            "critical" => "critico",
            _ => string.IsNullOrWhiteSpace(risk) ? "-" : risk
        };
    }

    private static string SharedPilotStyle()
    {
        return """
<style>
  :root { --ink: #17211a; --muted: #5c6b60; --paper: #f5f1e7; --panel: #fffaf0; --line: #d7cdb7; --safe: #226b45; --info: #245a87; --risk: #8a352d; --warn: #9a5a10; --approval: #b24f16; --disabled: #6c706b; }
  body { margin: 0; color: var(--ink); font-family: "Aptos", "Segoe UI", sans-serif; background: linear-gradient(135deg, #f7f2df, #e4eadc); }
  main { max-width: 1220px; margin: 0 auto; padding: 40px 24px; }
  .card { background: rgba(255,250,240,.92); border: 1px solid var(--line); border-radius: 26px; padding: 24px; box-shadow: 0 20px 70px rgba(43,32,16,.14); margin-bottom: 18px; }
  h1 { font-family: Georgia, "Times New Roman", serif; font-size: clamp(38px, 6vw, 72px); line-height: .95; margin: 8px 0 12px; letter-spacing: -.045em; }
  p, li { color: var(--muted); line-height: 1.5; }
  table { width: 100%; border-collapse: collapse; background: var(--panel); border-radius: 18px; overflow: hidden; }
  th, td { padding: 11px 12px; border-bottom: 1px solid var(--line); text-align: left; vertical-align: top; }
  th { font-size: 11px; text-transform: uppercase; letter-spacing: .08em; background: #efe6d4; }
  .grid { display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 14px; }
  .badge { display: inline-block; border-radius: 999px; padding: 5px 10px; font-weight: 800; font-size: 12px; background: #eadfca; white-space: nowrap; }
  .safe { color: var(--safe); background: #dcebdd; }
  .info { color: var(--info); background: #dbe8f5; }
  .risk { color: var(--risk); background: #f6e7e6; }
  .warn { color: var(--warn); background: #f3e2bf; }
  .approval { color: var(--approval); background: #f7dcc8; }
  .blocked { color: var(--risk); background: #f6e7e6; }
  .disabled { color: var(--disabled); background: #e7e8e2; }
  .help-inline { display: inline-flex; align-items: center; gap: 6px; flex-wrap: wrap; margin: 4px 8px 4px 0; color: var(--muted); }
  .help-box { border: 1px solid var(--line); border-left: 5px solid var(--info); border-radius: 18px; padding: 14px 16px; background: rgba(255, 253, 246, 0.92); margin: 14px 0; }
  .help-box strong { color: var(--ink); }
  details.help-text { margin-top: 12px; border: 1px solid var(--line); border-radius: 16px; padding: 12px 14px; background: rgba(255, 253, 246, 0.86); }
  details.help-text summary { cursor: pointer; font-weight: 800; color: var(--ink); }
  details.help-text p { margin: 10px 0 0; }
  .notice { border-left: 4px solid #e66b2d; padding: 12px 14px; border-radius: 16px; background: rgba(239, 230, 212, 0.75); color: var(--ink); }
  .step-flow { display: grid; grid-template-columns: repeat(4, minmax(0, 1fr)); gap: 14px; margin-top: 18px; }
  .step-card { border: 1px solid var(--line); border-radius: 18px; background: rgba(255,253,246,.9); padding: 16px; min-height: 170px; display: flex; flex-direction: column; gap: 10px; }
  .step-number { width: 34px; height: 34px; border-radius: 50%; display: inline-grid; place-items: center; color: #fffaf0; background: var(--ink); font-weight: 900; }
  .step-card h3 { margin: 0; font-size: 16px; }
  .path { word-break: break-all; }
  .metric { border-bottom: 1px solid var(--line); padding: 10px 0; }
  .metric strong { display: block; font-size: 18px; color: var(--ink); }
  textarea, input { width: 100%; border: 1px solid var(--line); border-radius: 14px; padding: 10px; background: #fffdf6; font: inherit; margin: 6px 0 10px; }
  input[type="checkbox"] { width: auto; margin-right: 8px; }
  button, .button { display: inline-block; border: 0; border-radius: 999px; padding: 11px 16px; color: #fffaf0; background: var(--ink); font-weight: 800; text-decoration: none; }
  button.secondary { background: #6a755d; }
  button.reject { background: var(--risk); }
  @media (max-width: 780px) { .grid, .step-flow { grid-template-columns: 1fr; } }
</style>
""";
    }

    private static string Safety(RunSafetyCounters counters)
    {
        return Html($"clicks={counters.Clicks}; cookies={counters.CookiesAccepted}; login={counters.Login}; cart={counters.Cart}; purchase={counters.Purchase}; payment={counters.Payment}");
    }

    private static string FirstNonEmpty(params string?[] values)
    {
        return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? "";
    }

    private static string BuildAIProfileStatus(AIModelProfile profile)
    {
        if (!profile.Enabled)
            return "disabled";
        if (string.IsNullOrWhiteSpace(profile.Model))
            return "missing_model";
        if (!profile.ApiKeyConfigured && !string.Equals(profile.Provider, AIProviderKinds.Mock, StringComparison.OrdinalIgnoreCase))
            return "missing_key";
        return "configured";
    }

    private static string Html(string? value)
    {
        return WebUtility.HtmlEncode(value ?? "");
    }
}

