let port = null;
let portConnected = false;

const state = {
  activeTab: 'operate',
  connection: {
    status: 'disconnected',
    health: 'untested',
    host: '127.0.0.1',
    port: '8787',
    token: '',
    runtime: null
  },
  run: {
    runId: '',
    status: 'idle',
    requestId: '',
    currentTool: '',
    lastResult: '',
    lastError: ''
  },
  operator: {
    goal: '',
    plan: '',
    action: '',
    page: '',
    targetResolution: null,
    verification: null,
    timeline: []
  },
  learning: {
    recording: false,
    draft: null,
    timeline: []
  },
  recipes: {
    items: [],
    selectedId: '',
    parameters: {},
    run: null
  },
  runtime: {
    debug: null,
    lastToolRequest: null,
    lastToolResult: null,
    lastRunStatus: null
  },
  logs: []
};

const el = {
  headerStatus: document.getElementById('headerStatus'),
  globalStopBtn: document.getElementById('globalStopBtn'),
  tabs: Array.from(document.querySelectorAll('.tab')),
  panels: {
    operate: document.getElementById('tab-operate'),
    learn: document.getElementById('tab-learn'),
    recipes: document.getElementById('tab-recipes'),
    runtime: document.getElementById('tab-runtime')
  },
  humanBanner: document.getElementById('humanBanner'),
  humanMessage: document.getElementById('humanMessage'),
  resumeHumanBtn: document.getElementById('resumeHumanBtn'),
  instructionInput: document.getElementById('instructionInput'),
  startRunBtn: document.getElementById('startRunBtn'),
  pauseRunBtn: document.getElementById('pauseRunBtn'),
  resumeRunBtn: document.getElementById('resumeRunBtn'),
  operatorGoal: document.getElementById('operatorGoal'),
  operatorPlan: document.getElementById('operatorPlan'),
  operatorAction: document.getElementById('operatorAction'),
  currentTool: document.getElementById('currentTool'),
  pageUrl: document.getElementById('pageUrl'),
  lastResult: document.getElementById('lastResult'),
  targetIntent: document.getElementById('targetIntent'),
  targetText: document.getElementById('targetText'),
  selectedElementId: document.getElementById('selectedElementId'),
  selectedScore: document.getElementById('selectedScore'),
  selectedSelector: document.getElementById('selectedSelector'),
  selectedReason: document.getElementById('selectedReason'),
  resolveCandidates: document.getElementById('resolveCandidates'),
  verificationStatus: document.getElementById('verificationStatus'),
  verificationUrlChanged: document.getElementById('verificationUrlChanged'),
  verificationTitleChanged: document.getElementById('verificationTitleChanged'),
  verificationDomChanged: document.getElementById('verificationDomChanged'),
  verificationExpected: document.getElementById('verificationExpected'),
  verificationReason: document.getElementById('verificationReason'),
  operatorTimeline: document.getElementById('operatorTimeline'),
  learningName: document.getElementById('learningName'),
  learningDescription: document.getElementById('learningDescription'),
  startLearningBtn: document.getElementById('startLearningBtn'),
  stopLearningBtn: document.getElementById('stopLearningBtn'),
  reviewRecipeBtn: document.getElementById('reviewRecipeBtn'),
  saveRecipeBtn: document.getElementById('saveRecipeBtn'),
  learningStatus: document.getElementById('learningStatus'),
  learningTimeline: document.getElementById('learningTimeline'),
  recipeDraftJson: document.getElementById('recipeDraftJson'),
  recipeList: document.getElementById('recipeList'),
  recipeImportInput: document.getElementById('recipeImportInput'),
  importRecipeBtn: document.getElementById('importRecipeBtn'),
  recipeNameInput: document.getElementById('recipeNameInput'),
  recipeDescriptionInput: document.getElementById('recipeDescriptionInput'),
  recipeStartUrlInput: document.getElementById('recipeStartUrlInput'),
  recipeParameterForm: document.getElementById('recipeParameterForm'),
  recipeJsonEditor: document.getElementById('recipeJsonEditor'),
  runRecipeBtn: document.getElementById('runRecipeBtn'),
  pauseRecipeBtn: document.getElementById('pauseRecipeBtn'),
  resumeRecipeBtn: document.getElementById('resumeRecipeBtn'),
  retryRecipeStepBtn: document.getElementById('retryRecipeStepBtn'),
  skipRecipeStepBtn: document.getElementById('skipRecipeStepBtn'),
  abortRecipeBtn: document.getElementById('abortRecipeBtn'),
  saveRecipeChangesBtn: document.getElementById('saveRecipeChangesBtn'),
  cancelRecipeEditBtn: document.getElementById('cancelRecipeEditBtn'),
  deleteRecipeBtn: document.getElementById('deleteRecipeBtn'),
  exportRecipeBtn: document.getElementById('exportRecipeBtn'),
  recipeRunId: document.getElementById('recipeRunId'),
  recipeRunStatus: document.getElementById('recipeRunStatus'),
  recipeCurrentStep: document.getElementById('recipeCurrentStep'),
  recipeLastError: document.getElementById('recipeLastError'),
  recipeStepTimeline: document.getElementById('recipeStepTimeline'),
  hostInput: document.getElementById('hostInput'),
  portInput: document.getElementById('portInput'),
  tokenInput: document.getElementById('tokenInput'),
  connectBtn: document.getElementById('connectBtn'),
  healthBtn: document.getElementById('healthBtn'),
  reconnectBtn: document.getElementById('reconnectBtn'),
  refreshDebugBtn: document.getElementById('refreshDebugBtn'),
  runtimeConnection: document.getElementById('runtimeConnection'),
  runtimeHost: document.getElementById('runtimeHost'),
  runtimePort: document.getElementById('runtimePort'),
  runtimeHealth: document.getElementById('runtimeHealth'),
  runtimeClients: document.getElementById('runtimeClients'),
  runtimeDiagnostic: document.getElementById('runtimeDiagnostic'),
  runtimeProvider: document.getElementById('runtimeProvider'),
  runtimeModel: document.getElementById('runtimeModel'),
  runtimeApiKey: document.getElementById('runtimeApiKey'),
  runtimeAiError: document.getElementById('runtimeAiError'),
  runtimeSocket: document.getElementById('runtimeSocket'),
  runtimeClientId: document.getElementById('runtimeClientId'),
  runtimeLastSeen: document.getElementById('runtimeLastSeen'),
  runtimeProtocol: document.getElementById('runtimeProtocol'),
  pageTab: document.getElementById('pageTab'),
  runtimeUrl: document.getElementById('runtimeUrl'),
  runtimeContent: document.getElementById('runtimeContent'),
  runId: document.getElementById('runId'),
  runtimeRequestId: document.getElementById('runtimeRequestId'),
  runtimeRunState: document.getElementById('runtimeRunState'),
  runtimeTool: document.getElementById('runtimeTool'),
  runtimeLastError: document.getElementById('runtimeLastError'),
  localLogs: document.getElementById('localLogs'),
  engineLogs: document.getElementById('engineLogs'),
  extensionLogs: document.getElementById('extensionLogs'),
  lastToolRequest: document.getElementById('lastToolRequest'),
  lastToolResult: document.getElementById('lastToolResult'),
  lastRunStatus: document.getElementById('lastRunStatus')
};

connectPort();
bindEvents();
render();
post({ type: 'loadConfig' });

function bindEvents() {
  el.tabs.forEach((button) => {
    button.addEventListener('click', () => {
      state.activeTab = button.dataset.tab;
      renderTabs();
    });
  });

  el.globalStopBtn.addEventListener('click', () => {
    state.run.status = 'stopped';
    pushTimeline('STOP solicitado');
    post({ type: 'stop' });
    render();
  });
  el.resumeHumanBtn.addEventListener('click', () => {
    el.humanBanner.classList.add('hidden');
    state.run.status = 'running';
    post({ type: 'resumeHuman' });
    post({ type: 'recipeResume' });
    render();
  });

  el.startRunBtn.addEventListener('click', () => {
    state.operator.goal = el.instructionInput.value.trim();
    state.operator.timeline = ['Run iniciado'];
    state.run.status = 'running';
    post({ type: 'startRun', instruction: state.operator.goal });
    render();
  });
  el.pauseRunBtn.addEventListener('click', () => {
    state.run.status = 'paused';
    post({ type: 'pause' });
    render();
  });
  el.resumeRunBtn.addEventListener('click', () => {
    state.run.status = 'running';
    post({ type: 'resume' });
    render();
  });

  el.startLearningBtn.addEventListener('click', () => {
    post({ type: 'learningStart', payload: learningFormPayload() });
  });
  el.stopLearningBtn.addEventListener('click', () => post({ type: 'learningStop' }));
  el.reviewRecipeBtn.addEventListener('click', () => {
    state.activeTab = 'learn';
    renderTabs();
    el.recipeDraftJson.focus();
  });
  el.saveRecipeBtn.addEventListener('click', () => {
    post({ type: 'learningSaveRecipe', payload: learningFormPayload() });
  });

  el.connectBtn.addEventListener('click', () => {
    if (state.connection.status === 'connected' || state.connection.status === 'running' || state.connection.status === 'paused') {
      post({ type: 'disconnect' });
      return;
    }
    state.connection.status = 'connecting';
    post({ type: 'connect', config: currentConfig() });
    render();
  });
  el.healthBtn.addEventListener('click', () => {
    state.connection.health = 'testing';
    post({ type: 'testHealth', config: currentConfig() });
    render();
  });
  el.reconnectBtn.addEventListener('click', () => {
    state.connection.status = 'connecting';
    post({ type: 'connect', config: currentConfig() });
    render();
  });
  el.refreshDebugBtn.addEventListener('click', () => {
    post({ type: 'refreshDebug', config: currentConfig() });
  });

  el.importRecipeBtn.addEventListener('click', () => el.recipeImportInput.click());
  el.recipeImportInput.addEventListener('change', importRecipeFile);
  el.runRecipeBtn.addEventListener('click', () => selectedRecipeId() && post({ type: 'recipeRun', recipeId: selectedRecipeId(), parameters: collectRecipeParameters() }));
  el.pauseRecipeBtn.addEventListener('click', () => post({ type: 'recipePause' }));
  el.resumeRecipeBtn.addEventListener('click', () => post({ type: 'recipeResume' }));
  el.retryRecipeStepBtn.addEventListener('click', () => post({ type: 'recipeRetryStep' }));
  el.skipRecipeStepBtn.addEventListener('click', () => post({ type: 'recipeSkipStep' }));
  el.abortRecipeBtn.addEventListener('click', () => post({ type: 'recipeAbort' }));
  el.saveRecipeChangesBtn.addEventListener('click', saveEditedRecipe);
  el.cancelRecipeEditBtn.addEventListener('click', () => selectRecipe(''));
  el.deleteRecipeBtn.addEventListener('click', () => selectedRecipeId() && post({ type: 'recipeDelete', recipeId: selectedRecipeId() }));
  el.exportRecipeBtn.addEventListener('click', exportSelectedRecipe);
}

function handleMessage(message) {
  if (!message || !message.type) {
    return;
  }

  switch (message.type) {
    case 'config':
      state.connection.host = message.config.host || '127.0.0.1';
      state.connection.port = message.config.port || '8787';
      state.connection.token = message.config.token || '';
      el.hostInput.value = state.connection.host;
      el.portInput.value = state.connection.port;
      el.tokenInput.value = state.connection.token;
      break;
    case 'state':
      state.connection.status = message.status || 'disconnected';
      if (message.currentRunId) {
        state.run.runId = message.currentRunId;
      }
      if (message.status === 'running' || message.status === 'paused' || message.status === 'stopped' || message.status === 'error') {
        state.run.status = message.status;
      }
      addLog('local', `${message.status}: ${message.message || ''}`);
      break;
    case 'health':
      state.connection.health = message.ok ? 'ok' : 'fail';
      addLog('local', message.ok ? 'Health OK' : `Health failed: ${message.error || ''}`);
      break;
    case 'page':
      state.operator.page = message.page && message.page.url ? message.page.url : '-';
      break;
    case 'engineMessage':
      handleEngineMessage(message.message || {});
      break;
    case 'toolResult':
      handleToolResult(message);
      break;
    case 'runStatus':
      handleRunStatus(message.message || {});
      break;
    case 'humanIntervention':
      showHumanBanner(message.message || message.reason || 'Intervención humana requerida');
      break;
    case 'runStarted':
      handleRunStarted(message.body || {});
      break;
    case 'runtimeSnapshot':
      state.connection.runtime = message.runtime || null;
      hydrateRuntime(message.runtime || {});
      break;
    case 'debugSnapshot':
      state.runtime.debug = message.debug || { error: message.error || 'debug unavailable' };
      break;
    case 'learningState':
      hydrateLearning(message.draft || null);
      break;
    case 'recipes':
      state.recipes.items = Array.isArray(message.recipes) ? message.recipes : [];
      break;
    case 'recipeRunState':
      state.recipes.run = message.run || null;
      break;
    case 'recipeRunParameterRequired':
      state.recipes.selectedId = message.recipe && message.recipe.recipeId ? message.recipe.recipeId : state.recipes.selectedId;
      state.recipes.run = {
        status: 'paused',
        lastError: `Faltan parametros: ${(message.missing || []).join(', ')}`,
        recipe: message.recipe || null,
        stepResults: []
      };
      break;
    case 'recipeError':
      addLog('local', message.message || 'Recipe error');
      break;
    default:
      break;
  }

  render();
}

function handleEngineMessage(message) {
  addLog('engine', message);
  if (message.tool) {
    state.run.currentTool = message.tool;
    state.operator.action = `Solicitando ${message.tool}`;
  }
  if (message.type === 'tool.request') {
    state.runtime.lastToolRequest = message;
    state.run.currentTool = message.tool || '';
    state.run.requestId = message.requestId || '';
    pushTimeline(humanizeToolRequest(message));
  }
}

function handleToolResult(message) {
  addLog('extension', message);
  state.runtime.lastToolResult = message;
  state.run.lastResult = message.success ? 'success' : `error: ${message.error || ''}`;
  if (!message.success) {
    state.run.lastError = message.error || '';
  }

  const tool = message.request && message.request.tool ? message.request.tool : '';
  if (tool === 'observePage' && message.result) {
    pushTimeline('Observé la página');
    state.operator.page = message.result.url || state.operator.page;
  }
  if (tool === 'resolveTarget' && message.result) {
    state.operator.targetResolution = message.result;
    const best = message.result.bestCandidate;
    pushTimeline(best ? `Elegí un target con ${Math.round((best.score || 0) * 100)}% de confianza` : 'No encontré target confiable');
  }
  if ((tool === 'clickElement' || tool === 'setElementValue' || tool === 'click' || tool === 'setValue') && message.result) {
    state.operator.verification = message.result;
    pushTimeline(message.result.verificationStatus ? `Verificación: ${message.result.verificationStatus}` : `${tool} completado`);
  }
}

function handleRunStatus(message) {
  state.runtime.lastRunStatus = message;
  state.run.status = message.status || state.run.status;
  state.run.lastResult = message.message || state.run.lastResult;
  if (message.status === 'error') {
    state.run.lastError = message.message || '';
  }
  addLog('engine', message);
}

function handleRunStarted(body) {
  state.run.runId = body.runId || '';
  state.run.status = body.status === 'error' ? 'error' : 'running';
  state.run.lastResult = body.message || '';
  if (body.status === 'error') {
    state.run.lastError = body.message || '';
  }
  addLog('local', body);
}

function hydrateRuntime(runtime) {
  const run = runtime.run || {};
  const connection = runtime.connection || {};
  state.connection.host = connection.host || state.connection.host;
  state.connection.port = connection.port || state.connection.port;
  state.connection.health = connection.health && connection.health.ok ? 'ok' : state.connection.health;
  state.run.runId = run.runId || state.run.runId;
  state.run.requestId = run.requestId || state.run.requestId;
  state.run.currentTool = run.currentTool || state.run.currentTool;
  state.runtime.lastToolRequest = run.lastToolRequest || state.runtime.lastToolRequest;
  state.runtime.lastToolResult = run.lastToolResult || state.runtime.lastToolResult;
  state.runtime.lastRunStatus = run.lastRunStatus || state.runtime.lastRunStatus;
}

function hydrateLearning(draft) {
  state.learning.draft = draft;
  state.learning.recording = Boolean(draft && draft.recording);
  state.learning.timeline = draft && Array.isArray(draft.steps) ? draft.steps : [];
  if (draft) {
    el.learningName.value = draft.name || el.learningName.value;
    el.learningDescription.value = draft.description || el.learningDescription.value;
  }
}

function render() {
  renderTabs();
  renderHeader();
  renderOperate();
  renderLearning();
  renderRecipes();
  renderRuntime();
}

function renderTabs() {
  el.tabs.forEach((button) => button.classList.toggle('active', button.dataset.tab === state.activeTab));
  Object.entries(el.panels).forEach(([name, panel]) => panel.classList.toggle('active', name === state.activeTab));
}

function renderHeader() {
  el.headerStatus.textContent = `${connectionLabel()} · Run: ${state.run.status || 'idle'} · ${state.activeTab}`;
}

function renderOperate() {
  el.operatorGoal.textContent = state.operator.goal || '-';
  el.operatorPlan.textContent = state.operator.plan || 'observe -> resolveTarget -> action -> verify';
  el.operatorAction.textContent = state.operator.action || '-';
  el.currentTool.textContent = state.run.currentTool || '-';
  el.pageUrl.textContent = state.operator.page || '-';
  el.lastResult.textContent = state.run.lastResult || '-';
  renderTargetResolution();
  renderVerification();
  renderTimeline(el.operatorTimeline, state.operator.timeline);
}

function renderTargetResolution() {
  const resolution = state.operator.targetResolution;
  const best = resolution && resolution.bestCandidate ? resolution.bestCandidate : null;
  el.targetIntent.textContent = resolution ? resolution.intent || '-' : '-';
  el.targetText.textContent = resolution ? resolution.targetText || '-' : '-';
  el.selectedElementId.textContent = best ? best.elementId || '-' : '-';
  el.selectedScore.textContent = best && typeof best.score === 'number' ? best.score.toFixed(2) : '-';
  el.selectedSelector.textContent = best && best.bestSelector ? best.bestSelector.selector || '-' : '-';
  el.selectedReason.textContent = best ? best.reason || '-' : '-';

  const candidates = resolution && Array.isArray(resolution.candidates) ? resolution.candidates : [];
  el.resolveCandidates.innerHTML = candidates.length
    ? candidates.map(renderCandidate).join('')
    : 'No candidates yet.';
}

function renderCandidate(candidate) {
  const element = candidate.element || {};
  const label = element.accessibleName || element.visibleText || candidate.elementId || '-';
  const selector = candidate.bestSelector && candidate.bestSelector.selector ? candidate.bestSelector.selector : '-';
  const score = typeof candidate.score === 'number' ? candidate.score.toFixed(2) : '-';
  return `<div class="candidate"><div class="candidate-head"><strong>${escapeHtml(label)}</strong><span>${escapeHtml(candidate.elementId || '-')}</span></div><div class="candidate-meta">score ${escapeHtml(score)} · ${escapeHtml(selector)}</div><div class="candidate-reason">${escapeHtml(candidate.reason || '')}</div></div>`;
}

function renderVerification() {
  const verification = state.operator.verification || {};
  el.verificationStatus.textContent = verification.verificationStatus || '-';
  el.verificationUrlChanged.textContent = asYesNo(verification.urlChanged);
  el.verificationTitleChanged.textContent = asYesNo(verification.titleChanged);
  el.verificationDomChanged.textContent = asYesNo(verification.domChanged);
  el.verificationExpected.textContent = asYesNo(verification.expectedConditionMet);
  el.verificationReason.textContent = verification.reason || '-';
}

function renderLearning() {
  const draft = state.learning.draft;
  el.learningStatus.textContent = state.learning.recording
    ? 'Grabando. NEXA está mirando tus acciones. No se guardarán valores sensibles de contraseñas.'
    : 'No grabando';
  renderTimeline(el.learningTimeline, state.learning.timeline.map(humanizeLearningStep));
  el.recipeDraftJson.value = draft ? JSON.stringify(recipeDraftFromLearning(draft), null, 2) : '';
}

function renderRecipes() {
  el.recipeList.innerHTML = state.recipes.items.length
    ? state.recipes.items.map(renderRecipeItem).join('')
    : 'No hay recetas guardadas.';
  document.querySelectorAll('[data-recipe-action]').forEach((button) => {
    button.addEventListener('click', handleRecipeAction);
  });
  const selected = selectedRecipe();
  el.recipeNameInput.value = selected ? selected.name || '' : '';
  el.recipeDescriptionInput.value = selected ? selected.description || '' : '';
  el.recipeStartUrlInput.value = selected ? selected.startUrl || '' : '';
  el.recipeJsonEditor.value = selected ? JSON.stringify(selected, null, 2) : '';
  renderRecipeParameters(selected);
  renderRecipeRun();
}

function renderRecipeItem(recipe) {
  return `<div class="recipe-item"><div class="recipe-head"><strong>${escapeHtml(recipe.name || 'Receta')}</strong><span>schema ${escapeHtml(recipe.schemaVersion || 1)}</span></div><div class="recipe-meta">${escapeHtml(recipe.description || '')}</div><div class="recipe-meta">${escapeHtml(recipe.createdAt || '-')} · ${escapeHtml((recipe.steps || []).length)} pasos · ${escapeHtml((recipe.parameters || []).length)} parametros</div><div class="recipe-actions"><button data-recipe-action="open" data-recipe-id="${escapeHtml(recipe.recipeId)}">Editar</button><button data-recipe-action="run" data-recipe-id="${escapeHtml(recipe.recipeId)}">Ejecutar</button><button data-recipe-action="duplicate" data-recipe-id="${escapeHtml(recipe.recipeId)}">Duplicar</button><button data-recipe-action="delete" data-recipe-id="${escapeHtml(recipe.recipeId)}">Borrar</button><button data-recipe-action="export" data-recipe-id="${escapeHtml(recipe.recipeId)}">Exportar JSON</button></div></div>`;
}

function renderRecipeParameters(recipe) {
  if (!recipe || !Array.isArray(recipe.parameters) || recipe.parameters.length === 0) {
    el.recipeParameterForm.innerHTML = '<p class="muted">Sin parametros requeridos.</p>';
    return;
  }

  el.recipeParameterForm.innerHTML = recipe.parameters.map((parameter) => {
    const type = parameter.type === 'number' || parameter.type === 'money' ? 'number' : parameter.type === 'date' ? 'date' : parameter.type === 'boolean' ? 'checkbox' : 'text';
    const value = state.recipes.parameters[parameter.name] || parameter.defaultValue || '';
    if (type === 'checkbox') {
      return `<label>${escapeHtml(parameter.label || parameter.name)} <input data-recipe-param="${escapeHtml(parameter.name)}" type="checkbox" ${value ? 'checked' : ''}></label>`;
    }
    return `<label>${escapeHtml(parameter.label || parameter.name)} <input data-recipe-param="${escapeHtml(parameter.name)}" type="${type}" value="${escapeHtml(value)}"></label>`;
  }).join('');
}

function renderRecipeRun() {
  const run = state.recipes.run || {};
  el.recipeRunId.textContent = run.recipeRunId || '-';
  el.recipeRunStatus.textContent = run.status || 'idle';
  el.recipeCurrentStep.textContent = run.currentStepLabel ? `${Number(run.currentStepIndex || 0) + 1}/${run.recipe && run.recipe.steps ? run.recipe.steps.length : '-'} ${run.currentStepLabel}` : '-';
  el.recipeLastError.textContent = run.lastError || '-';

  const results = Array.isArray(run.stepResults) ? run.stepResults : [];
  el.recipeStepTimeline.innerHTML = results.length
    ? results.map((result, index) => {
      const marker = result.status === 'passed' ? '✓' : result.status === 'running' ? '▶' : result.status === 'failed' ? '!' : result.status === 'skipped' ? '-' : '○';
      const label = run.recipe && run.recipe.steps && run.recipe.steps[index] ? run.recipe.steps[index].label || result.type : result.type;
      return `<li>${escapeHtml(marker)} ${index + 1}. ${escapeHtml(label)} · ${escapeHtml(result.status)}${result.error ? ' · ' + escapeHtml(result.error) : ''}</li>`;
    }).join('')
    : '<li>-</li>';
}

function renderRuntime() {
  const runtime = state.connection.runtime || {};
  const connection = runtime.connection || {};
  const debug = connection.debug || state.runtime.debug || {};
  const clients = debug.clients || {};
  const connectedCount = typeof clients.connectedCount === 'number' ? clients.connectedCount : '-';
  const ai = runtime.ai || {};
  const extension = runtime.extension || {};
  const run = runtime.run || {};
  el.runtimeConnection.textContent = connection.state || (connection.connected ? 'connected' : state.connection.status);
  el.runtimeHost.textContent = state.connection.host;
  el.runtimePort.textContent = state.connection.port;
  el.runtimeHealth.textContent = state.connection.health;
  el.runtimeClients.textContent = String(connectedCount);
  el.runtimeDiagnostic.textContent = runtimeDiagnostic(connection, clients);
  el.runtimeProvider.textContent = ai.provider || 'OpenAI';
  el.runtimeModel.textContent = ai.model || '-';
  el.runtimeApiKey.textContent = ai.hasApiKeyLocal === null || ai.hasApiKeyLocal === undefined ? 'unknown' : ai.hasApiKeyLocal ? 'local key loaded' : 'missing';
  el.runtimeAiError.textContent = ai.lastError || '-';
  el.runtimeSocket.textContent = extension.webSocketConnected ? 'connected' : connection.state || 'disconnected';
  el.runtimeClientId.textContent = extension.clientId || connection.clientId || '-';
  el.runtimeLastSeen.textContent = extension.lastSeenAt || connection.lastSeenAt || '-';
  el.runtimeProtocol.textContent = extension.protocolVersion || '-';
  el.pageTab.textContent = extension.tabId || '-';
  el.runtimeUrl.textContent = extension.url || state.operator.page || '-';
  el.runtimeContent.textContent = extension.contentScriptActive ? 'active' : 'unavailable';
  el.runId.textContent = state.run.runId || '-';
  el.runtimeRequestId.textContent = state.run.requestId || run.requestId || '-';
  el.runtimeRunState.textContent = state.run.status || run.status || 'idle';
  el.runtimeTool.textContent = state.run.currentTool || run.currentTool || '-';
  el.runtimeLastError.textContent = state.run.lastError || run.lastError || '-';
  renderLogs();
  el.lastToolRequest.textContent = stringify(state.runtime.lastToolRequest);
  el.lastToolResult.textContent = stringify(state.runtime.lastToolResult);
  el.lastRunStatus.textContent = stringify(state.runtime.lastRunStatus);
}

function runtimeDiagnostic(connection, clients) {
  if (!state.connection.runtime && state.connection.health === 'fail') {
    return 'Bridge caido';
  }
  if (connection.state === 'tokenError') {
    return 'Error de token';
  }
  if (connection.state === 'protocolError') {
    return 'Error de protocolo';
  }
  if (connection.connected) {
    return 'Conectado';
  }
  if (clients && clients.connectedCount === 0) {
    return 'Bridge OK, extension no conectada';
  }
  if (state.connection.status === 'connecting' || connection.state === 'reconnecting') {
    return 'Conectando';
  }
  return connection.lastError || '-';
}

function renderLogs() {
  el.localLogs.innerHTML = state.logs.filter((item) => item.direction === 'local').slice(0, 80).map(renderLog).join('');
  el.engineLogs.innerHTML = state.logs.filter((item) => item.direction === 'engine').slice(0, 80).map(renderLog).join('');
  el.extensionLogs.innerHTML = state.logs.filter((item) => item.direction === 'extension').slice(0, 80).map(renderLog).join('');
}

function renderTimeline(node, items) {
  const list = Array.isArray(items) ? items.filter(Boolean).slice(-40) : [];
  node.innerHTML = list.length
    ? list.map((item) => `<li>${escapeHtml(String(item))}</li>`).join('')
    : '<li>-</li>';
}

function handleRecipeAction(event) {
  const action = event.currentTarget.dataset.recipeAction;
  const recipeId = event.currentTarget.dataset.recipeId;
  if (action === 'open') {
    selectRecipe(recipeId);
  } else if (action === 'run') {
    selectRecipe(recipeId);
    post({ type: 'recipeRun', recipeId, parameters: collectRecipeParameters() });
  } else if (action === 'duplicate') {
    post({ type: 'recipeDuplicate', recipeId });
  } else if (action === 'delete') {
    post({ type: 'recipeDelete', recipeId });
  } else if (action === 'export') {
    exportRecipe(recipeId);
  }
}

function selectRecipe(recipeId) {
  state.recipes.selectedId = recipeId || '';
  renderRecipes();
}

function selectedRecipeId() {
  return state.recipes.selectedId;
}

function selectedRecipe() {
  return state.recipes.items.find((recipe) => recipe.recipeId === state.recipes.selectedId) || null;
}

function saveEditedRecipe() {
  const selected = selectedRecipe();
  if (!selected) {
    return;
  }
  let edited = selected;
  try {
    edited = JSON.parse(el.recipeJsonEditor.value || '{}');
  } catch {
    addLog('local', 'Recipe JSON inválido');
    render();
    return;
  }
  edited.recipeId = selected.recipeId;
  edited.name = el.recipeNameInput.value || edited.name || 'Receta';
  edited.description = el.recipeDescriptionInput.value || edited.description || '';
  edited.startUrl = el.recipeStartUrlInput.value || edited.startUrl || '';
  post({ type: 'recipeSave', recipe: edited });
}

function collectRecipeParameters() {
  const values = {};
  document.querySelectorAll('[data-recipe-param]').forEach((input) => {
    const name = input.getAttribute('data-recipe-param');
    values[name] = input.type === 'checkbox' ? input.checked : input.value;
  });
  state.recipes.parameters = values;
  return values;
}

function exportSelectedRecipe() {
  const selected = selectedRecipe();
  if (selected) {
    exportRecipe(selected.recipeId);
  }
}

function exportRecipe(recipeId) {
  const recipe = state.recipes.items.find((item) => item.recipeId === recipeId);
  if (!recipe) {
    return;
  }
  const blob = new Blob([JSON.stringify(recipe, null, 2)], { type: 'application/json' });
  const url = URL.createObjectURL(blob);
  const a = document.createElement('a');
  a.href = url;
  a.download = `${recipe.name || 'nexa-recipe'}.json`.replace(/[^a-z0-9._-]+/gi, '_');
  a.click();
  URL.revokeObjectURL(url);
}

function importRecipeFile(event) {
  const file = event.target.files && event.target.files[0];
  if (!file) {
    return;
  }
  const reader = new FileReader();
  reader.onload = () => {
    try {
      const recipe = JSON.parse(String(reader.result || '{}'));
      recipe.recipeId = recipe.recipeId || `recipe-${Date.now().toString(36)}`;
      post({ type: 'recipeSave', recipe });
    } catch {
      addLog('local', 'No se pudo importar JSON.');
      render();
    }
  };
  reader.readAsText(file);
}

function connectPort() {
  try {
    port = chrome.runtime.connect({ name: 'onebrain-sidepanel' });
    portConnected = true;
    port.onMessage.addListener(handleMessage);
    port.onDisconnect.addListener(() => {
      portConnected = false;
      port = null;
      state.connection.status = 'disconnected';
      addLog('local', 'Service worker port disconnected');
      render();
    });
    addLog('local', 'Service worker port connected');
  } catch (error) {
    portConnected = false;
    port = null;
    state.connection.status = 'error';
    addLog('local', toMessage(error));
  }
}

function post(message) {
  try {
    if (!port || !portConnected) {
      connectPort();
    }
    if (!port || !portConnected) {
      throw new Error('service worker port not connected');
    }
    port.postMessage(message);
  } catch (error) {
    portConnected = false;
    port = null;
    state.connection.status = 'error';
    addLog('local', toMessage(error));
    render();
  }
}

function currentConfig() {
  state.connection.host = el.hostInput.value.trim() || '127.0.0.1';
  state.connection.port = el.portInput.value.trim() || '8787';
  state.connection.token = el.tokenInput.value.trim();
  return { host: state.connection.host, port: state.connection.port, token: state.connection.token };
}

function learningFormPayload() {
  return {
    name: el.learningName.value.trim() || 'Nueva receta',
    description: el.learningDescription.value.trim()
  };
}

function recipeDraftFromLearning(draft) {
  return {
    schemaVersion: draft.schemaVersion || 1,
    recipeId: draft.recipeId,
    name: draft.name,
    description: draft.description,
    createdAt: draft.createdAt,
    updatedAt: draft.updatedAt,
    startUrl: draft.startUrl,
    steps: draft.steps || [],
    parameters: draft.parameters || [],
    sensitiveFields: draft.sensitiveFields || [],
    humanCheckpoints: draft.humanCheckpoints || [],
    safety: draft.safety || {},
    metadata: draft.metadata || {}
  };
}

function humanizeToolRequest(message) {
  const tool = message.tool || 'tool';
  if (tool === 'observePage') {
    return 'Observé la página';
  }
  if (tool === 'resolveTarget') {
    return `Busqué "${(message.args && message.args.targetText) || 'target'}"`;
  }
  if (tool === 'clickElement') {
    return 'Hice click sobre el target elegido';
  }
  if (tool === 'setElementValue') {
    return 'Escribí en un campo no sensible';
  }
  return `Ejecuté ${tool}`;
}

function humanizeLearningStep(step) {
  const label = step.target && (step.target.accessibleName || step.target.visibleText)
    ? step.target.accessibleName || step.target.visibleText
    : step.target && (step.target.semantic || step.target.observedText)
      ? step.target.semantic || step.target.observedText
    : step.url || '';
  const actionType = step.actionType || step.type || 'step';
  if (actionType === 'navigate') {
    return `Navegaste a ${step.url}`;
  }
  if (step.valueRedacted) {
    return `${actionType}: campo sensible redactado`;
  }
  return `${actionType}: ${label || '-'}`;
}

function pushTimeline(text) {
  if (!text) {
    return;
  }
  state.operator.timeline.push(text);
  state.operator.timeline = state.operator.timeline.slice(-40);
}

function showHumanBanner(message) {
  state.run.status = 'paused';
  el.humanMessage.textContent = message || 'Completá credenciales, captcha o 2FA y luego presioná Reanudar.';
  el.humanBanner.classList.remove('hidden');
  pushTimeline('Pausa humana requerida');
}

function addLog(direction, payload) {
  state.logs.unshift({
    at: new Date().toLocaleTimeString(),
    direction,
    payload
  });
  state.logs = state.logs.slice(0, 240);
}

function renderLog(item) {
  return `<div class="log-item"><strong>${escapeHtml(item.at)}</strong><br>${escapeHtml(summarize(item.payload))}</div>`;
}

function connectionLabel() {
  if (state.connection.status === 'connected' || state.connection.status === 'running') {
    return 'Connected';
  }
  if (state.connection.status === 'paused') {
    return 'Connected';
  }
  if (state.connection.status === 'connecting') {
    return 'Connecting';
  }
  return state.connection.status || 'Disconnected';
}

function asYesNo(value) {
  if (value === true) {
    return 'sí';
  }
  if (value === false) {
    return 'no';
  }
  return '-';
}

function stringify(value) {
  return value ? JSON.stringify(value, null, 2) : '-';
}

function summarize(value) {
  const text = typeof value === 'string' ? value : JSON.stringify(value);
  return text && text.length > 900 ? `${text.slice(0, 900)}...` : text || '';
}

function escapeHtml(value) {
  return String(value || '')
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;')
    .replace(/'/g, '&#039;');
}

function toMessage(error) {
  return error && error.message ? error.message : String(error);
}
