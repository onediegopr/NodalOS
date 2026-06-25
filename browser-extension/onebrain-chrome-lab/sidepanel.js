let port = null;
let portConnected = false;
const DEMO_STORE_KEY = 'nodal-os.demoMissions.v1';

const state = {
  activeTab: 'operate',
  demo: loadDemoStore(),
  connection: {
    status: 'disconnected',
    health: 'untested',
    host: '127.0.0.1',
    port: '8787',
    token: '',
    hasToken: false,
    tokenStatus: 'missing',
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
    planPreview: null,
    groundingSnapshot: null,
    timeline: []
  },
  learning: {
    recording: false,
    status: 'idle',
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
  handoff: null,
  consent: null,
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
  handoffSurface: document.getElementById('handoffSurface'),
  handoffTitle: document.getElementById('handoffTitle'),
  handoffStatus: document.getElementById('handoffStatus'),
  handoffReason: document.getElementById('handoffReason'),
  handoffSafeUrl: document.getElementById('handoffSafeUrl'),
  handoffInstruction: document.getElementById('handoffInstruction'),
  handoffExpectedAction: document.getElementById('handoffExpectedAction'),
  handoffOptions: document.getElementById('handoffOptions'),
  handoffContinueBtn: document.getElementById('handoffContinueBtn'),
  handoffCancelBtn: document.getElementById('handoffCancelBtn'),
  handoffCopyLogBtn: document.getElementById('handoffCopyLogBtn'),
  consentSurface: document.getElementById('consentSurface'),
  consentTitle: document.getElementById('consentTitle'),
  consentStatus: document.getElementById('consentStatus'),
  consentType: document.getElementById('consentType'),
  consentScope: document.getElementById('consentScope'),
  consentInstruction: document.getElementById('consentInstruction'),
  consentOptions: document.getElementById('consentOptions'),
  consentApproveBtn: document.getElementById('consentApproveBtn'),
  consentDenyBtn: document.getElementById('consentDenyBtn'),
  consentCancelBtn: document.getElementById('consentCancelBtn'),
  consentCopyLogBtn: document.getElementById('consentCopyLogBtn'),
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
  demoStatusBadge: document.getElementById('demoStatusBadge'),
  demoMissionName: document.getElementById('demoMissionName'),
  demoMissionObjective: document.getElementById('demoMissionObjective'),
  missionCreateForm: document.getElementById('missionCreateForm'),
  missionTitleInput: document.getElementById('missionTitleInput'),
  missionDescriptionInput: document.getElementById('missionDescriptionInput'),
  missionList: document.getElementById('missionList'),
  clearDemoHistoryBtn: document.getElementById('clearDemoHistoryBtn'),
  runSafeDemoBtn: document.getElementById('runSafeDemoBtn'),
  copyDemoReportBtn: document.getElementById('copyDemoReportBtn'),
  demoRunState: document.getElementById('demoRunState'),
  demoProgressLabel: document.getElementById('demoProgressLabel'),
  demoProgressBar: document.getElementById('demoProgressBar'),
  demoNextStep: document.getElementById('demoNextStep'),
  demoRunId: document.getElementById('demoRunId'),
  demoTimeline: document.getElementById('demoTimeline'),
  demoHostStatus: document.getElementById('demoHostStatus'),
  demoBridgeStatus: document.getElementById('demoBridgeStatus'),
  demoBrowserClaimStatus: document.getElementById('demoBrowserClaimStatus'),
  demoScopeStatus: document.getElementById('demoScopeStatus'),
  demoCommandKind: document.getElementById('demoCommandKind'),
  demoEvidencePanel: document.getElementById('demoEvidencePanel'),
  demoRunHistory: document.getElementById('demoRunHistory'),
  demoRunCount: document.getElementById('demoRunCount'),
  demoTechnicalReport: document.getElementById('demoTechnicalReport'),
  learningName: document.getElementById('learningName'),
  learningDescription: document.getElementById('learningDescription'),
  startLearningBtn: document.getElementById('startLearningBtn'),
  pauseLearningBtn: document.getElementById('pauseLearningBtn'),
  resumeLearningBtn: document.getElementById('resumeLearningBtn'),
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
  advancedTokenInput: document.getElementById('advancedTokenInput'),
  tokenSetupCard: document.getElementById('tokenSetupCard'),
  saveTokenConnectBtn: document.getElementById('saveTokenConnectBtn'),
  changeTokenBtn: document.getElementById('changeTokenBtn'),
  clearTokenBtn: document.getElementById('clearTokenBtn'),
  clearRuntimeStateBtn: document.getElementById('clearRuntimeStateBtn'),
  connectBtn: document.getElementById('connectBtn'),
  healthBtn: document.getElementById('healthBtn'),
  reconnectBtn: document.getElementById('reconnectBtn'),
  refreshDebugBtn: document.getElementById('refreshDebugBtn'),
  runtimeConnection: document.getElementById('runtimeConnection'),
  runtimeHost: document.getElementById('runtimeHost'),
  runtimePort: document.getElementById('runtimePort'),
  runtimeHealth: document.getElementById('runtimeHealth'),
  runtimeToken: document.getElementById('runtimeToken'),
  runtimeClients: document.getElementById('runtimeClients'),
  runtimeHeartbeat: document.getElementById('runtimeHeartbeat'),
  runtimeDiagnostic: document.getElementById('runtimeDiagnostic'),
  runtimeRecommendation: document.getElementById('runtimeRecommendation'),
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
  el.handoffContinueBtn.addEventListener('click', () => sendHandoffUiEvent('handoff.userCompleted'));
  el.handoffCancelBtn.addEventListener('click', () => sendHandoffUiEvent('handoff.cancelled'));
  el.handoffCopyLogBtn.addEventListener('click', copyHandoffLog);
  el.consentApproveBtn.addEventListener('click', () => sendConsentUiEvent(`${state.consent && state.consent.kind === 'profile' ? 'profileConsent' : 'vaultConsent'}.userApproved`));
  el.consentDenyBtn.addEventListener('click', () => sendConsentUiEvent(`${state.consent && state.consent.kind === 'profile' ? 'profileConsent' : 'vaultConsent'}.userDenied`));
  el.consentCancelBtn.addEventListener('click', () => sendConsentUiEvent(`${state.consent && state.consent.kind === 'profile' ? 'profileConsent' : 'vaultConsent'}.cancelled`));
  el.consentCopyLogBtn.addEventListener('click', copyConsentLog);
  el.missionCreateForm.addEventListener('submit', createMissionFromForm);
  el.clearDemoHistoryBtn.addEventListener('click', clearDemoHistory);
  el.runSafeDemoBtn.addEventListener('click', runSafeDemo);
  el.copyDemoReportBtn.addEventListener('click', copyDemoReport);

  el.startRunBtn.addEventListener('click', () => {
    state.operator.goal = el.instructionInput.value.trim();
    state.operator.timeline = buildStructuredTaskTimeline(state.operator.goal);
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
  el.pauseLearningBtn.addEventListener('click', () => post({ type: 'learningPause' }));
  el.resumeLearningBtn.addEventListener('click', () => post({ type: 'learningResume' }));
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
    state.connection.health = 'testing';
    post({ type: 'testHealth', config: currentConfig() });
    post({ type: 'refreshDebug', config: currentConfig() });
  });
  el.saveTokenConnectBtn.addEventListener('click', () => {
    state.connection.status = 'connecting';
    post({ type: 'saveTokenAndConnect', config: currentConfig() });
    render();
  });
  el.changeTokenBtn.addEventListener('click', () => {
    const token = el.advancedTokenInput.value.trim();
    if (!token) {
      addLog('local', 'Token vacio.');
      render();
      return;
    }
    el.tokenInput.value = token;
    state.connection.status = 'connecting';
    post({ type: 'saveTokenAndConnect', config: currentConfig() });
    render();
  });
  el.clearTokenBtn.addEventListener('click', () => {
    if (confirm('Borrar el token guardado en esta extension?')) {
      el.tokenInput.value = '';
      el.advancedTokenInput.value = '';
      state.connection.token = '';
      state.connection.tokenStatus = 'missing';
      post({ type: 'clearSavedToken' });
      render();
    }
  });
  el.clearRuntimeStateBtn.addEventListener('click', () => {
    if (confirm('Limpiar estado local de runtime sin borrar recetas ni token?')) {
      state.run = { runId: '', status: 'idle', requestId: '', currentTool: '', lastResult: '', lastError: '' };
      state.runtime.lastToolRequest = null;
      state.runtime.lastToolResult = null;
      state.runtime.lastRunStatus = null;
      post({ type: 'clearLocalRuntimeState' });
      render();
    }
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
      state.connection.hasToken = Boolean(state.connection.token);
      state.connection.tokenStatus = state.connection.hasToken ? 'saved' : 'missing';
      el.hostInput.value = state.connection.host;
      el.portInput.value = state.connection.port;
      el.tokenInput.value = state.connection.token;
      break;
    case 'state':
      state.connection.status = message.status || 'disconnected';
      if (message.currentRunId) {
        state.run.runId = message.currentRunId;
      }
      if (message.currentRunId && (message.status === 'running' || message.status === 'paused' || message.status === 'stopped' || message.status === 'error')) {
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
    case 'planPreview':
      applyPlanPreview(message.plan || message.preview || message.body || {});
      break;
    case 'recovery':
    case 'runtimeStagnation':
      applyRecoveryState(message.recovery || message.signal || message.body || message);
      break;
    case 'groundingSnapshot':
    case 'browserGroundingSnapshot':
      applyGroundingSnapshot(message.snapshot || message.grounding || message.body || message);
      break;
    case 'toolResult':
      handleToolResult(message);
      break;
    case 'runStatus':
      handleRunStatus(message.message || {});
      break;
    case 'humanIntervention':
      applyHandoffState(message);
      showHumanBanner(message.message || message.reason || 'Intervención humana requerida');
      break;
    case 'handoff.created':
    case 'handoff.updated':
    case 'handoff.expired':
    case 'handoff.resumeRequested':
    case 'handoff.resumeVerified':
    case 'handoff.resumeRejected':
    case 'handoff.disconnected':
      applyHandoffState(message);
      break;
    case 'vaultConsent.created':
    case 'vaultConsent.updated':
    case 'vaultConsent.expired':
    case 'vaultConsent.revoked':
    case 'vaultConsent.grantedByCore':
    case 'vaultConsent.deniedByCore':
    case 'profileConsent.created':
    case 'profileConsent.updated':
    case 'profileConsent.expired':
    case 'profileConsent.revoked':
    case 'profileConsent.grantedByCore':
    case 'profileConsent.deniedByCore':
      applyConsentState(message);
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
    case 'learningNotice':
      addLog('local', message.message || 'Learning state changed');
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
  if (message.type && /^handoff\./.test(message.type)) {
    applyHandoffState(message);
    return;
  }
  if (message.type && /^(vaultConsent|profileConsent)\./.test(message.type)) {
    applyConsentState(message);
    return;
  }
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
  if (typeof connection.hasToken === 'boolean') {
    state.connection.hasToken = connection.hasToken;
  }
  if (connection.state === 'tokenError') {
    state.connection.tokenStatus = 'invalid';
  } else if (connection.state === 'tokenRequired') {
    state.connection.tokenStatus = 'missing';
  } else if (state.connection.hasToken || state.connection.token) {
    state.connection.tokenStatus = 'saved';
  } else {
    state.connection.tokenStatus = 'missing';
  }
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
  state.learning.status = draft && draft.learningState ? draft.learningState : state.learning.recording ? 'recording' : draft ? 'reviewing' : 'idle';
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
  renderHandoff();
  renderConsent();
  renderLearning();
  renderRecipes();
  renderRuntime();
}

function renderTabs() {
  el.tabs.forEach((button) => button.classList.toggle('active', button.dataset.tab === state.activeTab));
  Object.entries(el.panels).forEach(([name, panel]) => panel.classList.toggle('active', name === state.activeTab));
}

function renderHeader() {
  el.headerStatus.textContent = `Connection: ${connectionLabel()} | Run: ${state.run.runId ? state.run.status || 'idle' : 'idle'} | Tab: ${state.activeTab}`;
}

function renderOperate() {
  renderDemoMissionControl();
  el.operatorGoal.textContent = state.operator.goal || '-';
  el.operatorPlan.textContent = state.operator.planPreview
    ? `Plan preview: ${state.operator.planPreview.status || 'PlanDrafted'}`
    : state.operator.plan || 'observe -> resolveTarget -> action -> verify';
  el.operatorAction.textContent = state.operator.action || '-';
  el.currentTool.textContent = state.run.currentTool || '-';
  el.pageUrl.textContent = state.operator.page || '-';
  el.lastResult.textContent = state.run.lastResult || '-';
  renderTargetResolution();
  renderVerification();
  renderTimeline(el.operatorTimeline, state.operator.timeline);
}

function createDemoSeed() {
  const createdAt = new Date().toISOString();
  const mission = createMissionRecord('Local Operator Demo', 'Ejecutar un no-op visible, generar evidencia demo y proyectar el run en timeline.', createdAt);
  return {
    missions: [mission],
    activeMissionId: mission.id,
    selectedRunId: '',
    missionName: mission.title,
    objective: mission.description,
    status: 'ready',
    statusLabel: 'Listo para probar',
    progress: 0,
    nextStep: 'Próximo paso: tocar Run demo.',
    runId: '',
    commandKind: 'SafeNoOp',
    result: 'Not started',
    evidenceRef: 'evidence:demo:pending',
    startedAt: '',
    completedAt: '',
    timeline: [
      demoTimelineStep('Misión demo lista', 'NODAL OS tiene una misión local preparada para ejecutar un SafeNoOp visible.', 'ready', 'MissionSeed', 'evidence:demo:seed'),
      demoTimelineStep('Esperando run demo', 'Tocá Run demo para proyectar un run no-op en timeline y logs.', 'waiting', 'OperatorAction', 'evidence:demo:pending')
    ],
    logs: [
      {
        label: 'Demo seed',
        value: 'Local Operator Demo guardada localmente. Sin acciones peligrosas.'
      }
    ],
    report: ''
  };
}

function loadDemoStore() {
  try {
    const raw = localStorage.getItem(DEMO_STORE_KEY);
    if (raw) {
      return normalizeDemoStore(JSON.parse(raw));
    }
  } catch (error) {
    console.warn('NODAL OS demo store unavailable', error);
  }
  const seed = createDemoSeed();
  saveDemoStore(seed);
  return seed;
}

function saveDemoStore(store = state.demo) {
  try {
    const payload = {
      missions: store.missions || [],
      activeMissionId: store.activeMissionId || '',
      selectedRunId: store.selectedRunId || ''
    };
    localStorage.setItem(DEMO_STORE_KEY, JSON.stringify(payload));
  } catch (error) {
    console.warn('NODAL OS demo store save failed', error);
  }
}

function normalizeDemoStore(store) {
  const seed = createDemoSeed();
  const missions = Array.isArray(store && store.missions)
    ? store.missions.map(normalizeMissionRecord).filter(Boolean)
    : [];
  const normalized = {
    ...seed,
    missions: missions.length ? missions : seed.missions,
    activeMissionId: store && store.activeMissionId ? store.activeMissionId : '',
    selectedRunId: store && store.selectedRunId ? store.selectedRunId : ''
  };
  if (!normalized.missions.some((mission) => mission.id === normalized.activeMissionId)) {
    normalized.activeMissionId = normalized.missions[0].id;
  }
  if (!selectedDemoRun(normalized)) {
    normalized.selectedRunId = '';
  }
  syncDemoViewFromStore(normalized);
  return normalized;
}

function createMissionRecord(title, description, createdAt = new Date().toISOString()) {
  const id = `mission-${Date.now().toString(36)}-${Math.random().toString(36).slice(2, 7)}`;
  return {
    id,
    title: title || 'Nueva misión',
    description: description || 'Demo local no-op para probar Mission Control.',
    createdAt,
    status: 'ready',
    runs: []
  };
}

function normalizeMissionRecord(mission) {
  if (!mission || typeof mission !== 'object') {
    return null;
  }
  return {
    id: String(mission.id || `mission-${Date.now().toString(36)}`),
    title: String(mission.title || mission.missionName || 'Nueva misión'),
    description: String(mission.description || mission.objective || 'Demo local no-op para probar Mission Control.'),
    createdAt: mission.createdAt || new Date().toISOString(),
    status: mission.status || 'ready',
    runs: Array.isArray(mission.runs) ? mission.runs.map(normalizeRunRecord).filter(Boolean) : []
  };
}

function normalizeRunRecord(run) {
  if (!run || typeof run !== 'object') {
    return null;
  }
  return {
    id: String(run.id || run.runId || `demo-${Date.now().toString(36)}`),
    missionId: String(run.missionId || ''),
    startedAt: run.startedAt || new Date().toISOString(),
    completedAt: run.completedAt || run.startedAt || new Date().toISOString(),
    status: run.status || 'completed',
    commandKind: run.commandKind || 'SafeNoOp',
    result: run.result || 'Completed with no side effects',
    evidenceRef: run.evidenceRef || `evidence:demo:${run.id || 'run'}`,
    timeline: Array.isArray(run.timeline) ? run.timeline : [],
    logs: Array.isArray(run.logs) ? run.logs : [],
    summary: run.summary || 'Run demo no-op completado.'
  };
}

function activeDemoMission(store = state.demo) {
  return (store.missions || []).find((mission) => mission.id === store.activeMissionId)
    || (store.missions || [])[0]
    || null;
}

function selectedDemoRun(store = state.demo) {
  const mission = activeDemoMission(store);
  if (!mission || !Array.isArray(mission.runs)) {
    return null;
  }
  return mission.runs.find((run) => run.id === store.selectedRunId) || mission.runs[0] || null;
}

function syncDemoViewFromStore(store = state.demo) {
  const mission = activeDemoMission(store);
  const run = selectedDemoRun(store);
  if (!mission) {
    return store;
  }

  store.missionName = mission.title;
  store.objective = mission.description || 'Demo local no-op para probar Mission Control.';
  store.status = run ? 'completed' : 'ready';
  store.statusLabel = run ? 'Demo completada' : 'Listo para probar';
  store.progress = run ? 100 : 0;
  store.nextStep = run ? 'Próximo paso: seleccionar otro run o copiar resumen.' : 'Próximo paso: tocar Run demo.';
  store.runId = run ? run.id : '';
  store.commandKind = run ? run.commandKind : 'SafeNoOp';
  store.result = run ? run.result : 'Not started';
  store.evidenceRef = run ? run.evidenceRef : 'evidence:demo:pending';
  store.startedAt = run ? run.startedAt : '';
  store.completedAt = run ? run.completedAt : '';
  store.timeline = run && run.timeline.length
    ? run.timeline
    : [
      demoTimelineStep('Misión lista', `${mission.title} está lista para un run demo local.`, 'ready', 'MissionSeed', 'evidence:demo:seed'),
      demoTimelineStep('Esperando run demo', 'Tocá Run demo para ver timeline, logs y evidencia.', 'waiting', 'OperatorAction', 'evidence:demo:pending')
    ];
  store.logs = run && run.logs.length
    ? run.logs
    : [{ label: 'Misión activa', value: `${mission.title} · ${mission.runs.length} runs guardados` }];
  store.report = composeDemoTechnicalReport(store);
  return store;
}

function createMissionFromForm(event) {
  event.preventDefault();
  const title = el.missionTitleInput.value.trim();
  const description = el.missionDescriptionInput.value.trim();
  if (!title) {
    el.missionTitleInput.focus();
    return;
  }
  const mission = createMissionRecord(title, description || 'Demo local no-op para probar Mission Control.');
  state.demo.missions.unshift(mission);
  state.demo.activeMissionId = mission.id;
  state.demo.selectedRunId = '';
  el.missionTitleInput.value = '';
  el.missionDescriptionInput.value = '';
  syncDemoViewFromStore();
  saveDemoStore();
  addLog('local', { kind: 'MissionCreated', missionId: mission.id, title: mission.title });
  render();
}

function selectDemoMission(missionId) {
  if (!state.demo.missions.some((mission) => mission.id === missionId)) {
    return;
  }
  state.demo.activeMissionId = missionId;
  const mission = activeDemoMission();
  state.demo.selectedRunId = mission && mission.runs[0] ? mission.runs[0].id : '';
  syncDemoViewFromStore();
  saveDemoStore();
  render();
}

function selectDemoRun(runId) {
  const mission = activeDemoMission();
  if (!mission || !mission.runs.some((run) => run.id === runId)) {
    return;
  }
  state.demo.selectedRunId = runId;
  syncDemoViewFromStore();
  saveDemoStore();
  render();
}

function clearDemoHistory() {
  if (!confirm('Limpiar misiones y runs demo locales?')) {
    return;
  }
  state.demo = createDemoSeed();
  saveDemoStore();
  addLog('local', { kind: 'DemoHistoryCleared' });
  render();
}

function demoTimelineStep(title, description, status, nodeType, evidenceRef) {
  return {
    title,
    description,
    status,
    nodeType,
    scopeLabel: 'local-demo',
    riskLevel: 'low',
    redactionSummary: 'redacted demo metadata only',
    safeNextAction: 'Revisar el estado visible en Mission Control.',
    evidenceRefs: [{ label: 'demo', refId: evidenceRef }]
  };
}

function runSafeDemo() {
  const mission = activeDemoMission();
  if (!mission) {
    return;
  }
  const now = new Date();
  const iso = now.toISOString();
  const runId = `demo-${now.getTime().toString(36)}`;
  const evidenceRef = `evidence:demo:${runId}`;
  const timeline = [
    demoTimelineStep('Run started', `Run ${runId} iniciado en modo demo local.`, 'running', 'RunVisible', evidenceRef),
    demoTimelineStep('No-op command accepted', 'Command kind SafeNoOp aceptado para demo visible. No se llamó shell, filesystem ni provider/cloud.', 'accepted', 'SafeNoOp', evidenceRef),
    demoTimelineStep('Evidence generated', 'Se generó evidencia demo redacted en memoria para el panel visible.', 'evidence', 'EvidenceProjection', evidenceRef),
    demoTimelineStep('Run completed', 'Run demo completado; timeline y logs quedaron actualizados visualmente.', 'completed', 'Result', evidenceRef)
  ];
  const logs = [
    { label: 'run id', value: runId },
    { label: 'mission', value: mission.title },
    { label: 'command kind', value: 'SafeNoOp' },
    { label: 'result', value: 'Completed with no side effects' },
    { label: 'timestamp', value: iso },
    { label: 'evidence ref', value: evidenceRef }
  ];
  const run = {
    id: runId,
    missionId: mission.id,
    startedAt: iso,
    completedAt: iso,
    status: 'completed',
    commandKind: 'SafeNoOp',
    result: 'Completed with no side effects',
    evidenceRef,
    timeline,
    logs,
    summary: `${mission.title}: run demo no-op completado.`
  };
  mission.status = 'completed';
  mission.runs.unshift(run);
  state.demo.activeMissionId = mission.id;
  state.demo.selectedRunId = run.id;
  syncDemoViewFromStore();
  saveDemoStore();
  state.run.runId = runId;
  state.run.status = 'completed';
  state.run.currentTool = 'SafeNoOpDemo';
  state.run.lastResult = 'Demo no-op completed';
  state.operator.goal = mission.title;
  state.operator.plan = 'seed -> safe no-op -> evidence -> report';
  state.operator.action = 'Run demo';
  state.operator.timeline = timeline;
  addLog('local', {
    kind: 'SafeNoOpDemo',
    runId,
    missionId: mission.id,
    result: 'completed',
    evidenceRef
  });
  render();
}

function renderDemoMissionControl() {
  syncDemoViewFromStore();
  const demo = state.demo;
  el.demoMissionName.textContent = demo.missionName;
  el.demoMissionObjective.textContent = demo.objective;
  el.demoStatusBadge.textContent = demo.statusLabel;
  el.demoStatusBadge.dataset.status = demo.status;
  el.demoRunState.textContent = demo.status === 'completed' ? 'Completed' : 'Ready';
  el.demoProgressLabel.textContent = `${demo.progress}%`;
  el.demoProgressBar.style.width = `${demo.progress}%`;
  el.demoNextStep.textContent = demo.nextStep;
  el.demoRunId.textContent = demo.runId ? `run: ${demo.runId}` : 'run: pending';
  el.demoCommandKind.textContent = demo.commandKind;
  el.demoHostStatus.textContent = demoHostStatus();
  el.demoBridgeStatus.textContent = demoBridgeStatus();
  el.demoBrowserClaimStatus.textContent = demoBrowserClaimStatus();
  el.demoScopeStatus.textContent = 'No-op local';
  renderDemoMissionList();
  renderDemoRunHistory();
  renderTimeline(el.demoTimeline, demo.timeline);
  el.demoEvidencePanel.innerHTML = demo.logs.map((item) => `
    <div class="demo-log-item">
      <span>${safeHtml(item.label)}</span>
      <strong>${safeHtml(item.value)}</strong>
    </div>`).join('');
  el.demoTechnicalReport.textContent = demo.report || buildDemoTechnicalReport();
}

function renderDemoMissionList() {
  el.missionList.innerHTML = state.demo.missions.map((mission) => {
    const active = mission.id === state.demo.activeMissionId;
    const created = formatDemoDate(mission.createdAt);
    const runCount = Array.isArray(mission.runs) ? mission.runs.length : 0;
    return `
      <button class="mission-list-item${active ? ' active' : ''}" type="button" data-demo-mission-id="${safeHtml(mission.id)}">
        <strong>${safeHtml(mission.title)}</strong>
        <small>${safeHtml(created)} · ${runCount} ${runCount === 1 ? 'run' : 'runs'}</small>
      </button>`;
  }).join('');
  el.missionList.querySelectorAll('[data-demo-mission-id]').forEach((button) => {
    button.addEventListener('click', () => selectDemoMission(button.getAttribute('data-demo-mission-id')));
  });
}

function renderDemoRunHistory() {
  const mission = activeDemoMission();
  const runs = mission && Array.isArray(mission.runs) ? mission.runs : [];
  el.demoRunCount.textContent = `${runs.length} ${runs.length === 1 ? 'run' : 'runs'}`;
  el.demoRunHistory.innerHTML = runs.map((run) => {
    const active = run.id === state.demo.selectedRunId;
    return `
      <button class="run-history-item${active ? ' active' : ''}" type="button" data-demo-run-id="${safeHtml(run.id)}">
        <strong>${safeHtml(formatDemoDate(run.completedAt || run.startedAt))}</strong>
        <small>${safeHtml(run.summary || run.result || 'Run demo local')}</small>
      </button>`;
  }).join('');
  el.demoRunHistory.querySelectorAll('[data-demo-run-id]').forEach((button) => {
    button.addEventListener('click', () => selectDemoRun(button.getAttribute('data-demo-run-id')));
  });
}

function demoHostStatus() {
  const host = state.connection.host || '127.0.0.1';
  const portValue = state.connection.port || '8787';
  const health = state.connection.health || 'untested';
  return `${host}:${portValue} · ${health}`;
}

function demoBridgeStatus() {
  if (state.connection.status === 'connected' || state.connection.status === 'running') {
    return 'Conectado';
  }
  if (state.connection.status === 'connecting') {
    return 'Conectando';
  }
  return 'Sin conectar';
}

function demoBrowserClaimStatus() {
  const tab = state.runtime && state.runtime.debug && state.runtime.debug.tab;
  if (tab && tab.claimStatus) {
    return tab.claimStatus;
  }
  if (state.operator.page) {
    return 'Página observada';
  }
  return 'No activo';
}

function buildDemoTechnicalReport() {
  syncDemoViewFromStore();
  return composeDemoTechnicalReport(state.demo);
}

function composeDemoTechnicalReport(store) {
  const demo = store || state.demo;
  const mission = activeDemoMission(demo);
  const run = selectedDemoRun(demo);
  const lines = [
    'NODAL OS — Demo local',
    `mission: ${demo.missionName}`,
    `mission_id: ${mission ? mission.id : 'none'}`,
    `status: ${demo.statusLabel}`,
    `run_id: ${demo.runId || 'pending'}`,
    `started_at: ${run ? run.startedAt : 'pending'}`,
    `completed_at: ${run ? run.completedAt : 'pending'}`,
    `command_kind: ${demo.commandKind}`,
    `result: ${demo.result}`,
    `evidence_ref: ${demo.evidenceRef}`,
    `timeline: ${(demo.timeline || []).map((step) => step.title || step).join(' -> ')}`,
    `logs: ${(demo.logs || []).map((item) => `${item.label}=${item.value}`).join('; ')}`,
    `host_status: ${demoHostStatus()}`,
    `bridge_status: ${demoBridgeStatus()}`,
    `browser_claim_status: ${demoBrowserClaimStatus()}`,
    'scope: no shell, no filesystem write, no provider/cloud call',
    'mode: demo local visible'
  ];
  return lines.join('\n');
}

function formatDemoDate(value) {
  if (!value) {
    return 'sin fecha';
  }
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) {
    return String(value);
  }
  return date.toLocaleString([], { dateStyle: 'short', timeStyle: 'short' });
}

async function copyDemoReport() {
  const report = buildDemoTechnicalReport();
  state.demo.report = report;
  try {
    await navigator.clipboard.writeText(report);
    addLog('local', { kind: 'DemoReportCopied', runId: state.demo.runId || 'pending' });
  } catch (error) {
    addLog('local', { kind: 'DemoReportCopyFallback', reason: error && error.message ? error.message : 'clipboard unavailable' });
  }
  render();
}

function applyPlanPreview(plan) {
  const safePlan = redactSensitive(plan || {});
  state.operator.planPreview = safePlan;
  state.operator.goal = safePlan.goal || state.operator.goal;
  state.operator.timeline = buildPlanPreviewTimeline(safePlan);
  pushTimeline({
    title: 'Plan preview recibido',
    description: 'Core emitio un plan visible; UI solo renderiza y no ejecuta.',
    status: safePlan.status || 'PlanDrafted',
    nodeType: 'CoreDecision',
    evidenceRefs: [{ label: 'plan', refId: safePlan.planId || 'plan-preview:redacted' }],
    safeNextAction: 'Revisar plan y esperar decision de Core.',
    coreAuthorityRequired: true,
    blockedOptions: ['auto execution from UI', 'submit/pay/sign/delete', 'credentials', 'sensitive sites']
  });
  render();
}

function applyRecoveryState(recovery) {
  const safeRecovery = redactSensitive(recovery || {});
  state.operator.timeline = buildRecoveryTimeline(safeRecovery).concat(state.operator.timeline || []);
  state.run.status = 'paused';
  state.run.lastResult = safeRecovery.state || safeRecovery.kind || 'RecoveryRequired';
  render();
}

function applyGroundingSnapshot(snapshot) {
  const safeSnapshot = redactSensitive(snapshot || {});
  state.operator.groundingSnapshot = safeSnapshot;
  state.operator.timeline = buildGroundingTimeline(safeSnapshot).concat(state.operator.timeline || []);
  state.run.lastResult = safeSnapshot.redactionStatus || safeSnapshot.pageHealth || 'GroundingSnapshot';
  render();
}

function renderHandoff() {
  const handoff = state.handoff;
  el.handoffSurface.classList.toggle('hidden', !handoff);
  if (!handoff) {
    return;
  }
  el.handoffTitle.textContent = 'NODAL OS necesita intervención humana';
  el.handoffStatus.textContent = handoff.displayState || handoff.status || 'WaitingForUser';
  el.handoffReason.textContent = handoff.reason || '-';
  el.handoffSafeUrl.textContent = handoff.safeUrl || '-';
  el.handoffInstruction.textContent = handoff.instruction || '-';
  el.handoffExpectedAction.textContent = handoff.expectedUserAction || '-';
  el.handoffOptions.textContent = (handoff.allowedOptions || []).join(', ') || '-';
  const terminal = ['Cancelled', 'Expired', 'Failed', 'Resumed', 'Blocked'].includes(handoff.displayState);
  el.handoffContinueBtn.disabled = terminal || handoff.displayState !== 'WaitingForUser';
  el.handoffCancelBtn.disabled = terminal;
}

function applyHandoffState(message) {
  const handoff = normalizeHandoff(message);
  if (!handoff) {
    return;
  }
  state.handoff = handoff;
  state.run.status = handoff.displayState === 'Resumed' ? state.run.status : 'paused';
  pushTimeline(`Intervención humana: ${handoff.displayState || handoff.reason || 'WaitingForUser'}`);
}

function normalizeHandoff(message) {
  const presentation = message.presentation || message.handoff || message;
  if (!presentation) {
    return null;
  }
  const displayState = displayStateFrom(message.type, presentation.displayState || presentation.status);
  const reason = presentation.reason || presentation.handoffReason || message.reason || 'UnknownSensitivePrompt';
  return {
    handoffId: redactSensitive(presentation.handoffId || presentation.id || message.handoffId || ''),
    runId: redactSensitive(presentation.runId || message.runId || state.run.runId || ''),
    actionId: redactSensitive(presentation.actionId || message.actionId || ''),
    correlationId: redactSensitive(presentation.correlationId || message.correlationId || ''),
    reason: redactSensitive(reason),
    status: redactSensitive(presentation.status || ''),
    displayState,
    safeTitle: redactSensitive(presentation.safeTitle || presentation.title || ''),
    safeUrl: redactSensitive(presentation.safeUrl || presentation.url || ''),
    instruction: redactSensitive(presentation.instruction || instructionForReason(reason)),
    expectedUserAction: redactSensitive(presentation.expectedUserAction || 'Completá el paso sensible manualmente y avisá cuando esté listo.'),
    allowedOptions: Array.isArray(presentation.allowedOptions) && presentation.allowedOptions.length
      ? presentation.allowedOptions.map(redactSensitive)
      : ['ContinueAfterUserAction', 'Cancel', 'CopyDiagnosticLog'],
    authoritative: false,
    verificationStatus: 'NotVerified',
    redacted: true,
    diagnostics: redactSensitive(presentation.diagnostics || message.diagnostics || '')
  };
}

function displayStateFrom(type, fallback) {
  if (type === 'handoff.expired') return 'Expired';
  if (type === 'handoff.resumeRequested') return 'UserCompletedPendingVerification';
  if (type === 'handoff.resumeVerified') return 'Resumed';
  if (type === 'handoff.resumeRejected') return 'Blocked';
  if (type === 'handoff.disconnected') return 'Blocked';
  if (fallback === 'UserCompleted') return 'UserCompletedPendingVerification';
  if (fallback === 'Cancelled' || fallback === 'Expired' || fallback === 'Failed' || fallback === 'Resumed' || fallback === 'Blocked') return fallback;
  return 'WaitingForUser';
}

function instructionForReason(reason) {
  const normalized = String(reason || '').toLowerCase();
  if (normalized.includes('captcha')) {
    return 'Se detectó CAPTCHA o verificación anti-bot. NODAL OS no intentará resolverlo automáticamente. Resolvelo manualmente y luego presioná "Ya lo hice, continuar".';
  }
  if (normalized.includes('twofactor') || normalized.includes('2fa') || normalized.includes('otp')) {
    return 'Se detectó un paso de doble factor. Completá el código o aprobación desde tu dispositivo y luego presioná "Ya lo hice, continuar".';
  }
  if (normalized.includes('clave')) {
    return 'Se detectó una credencial sensible o clave fiscal. NODAL OS se detuvo para que la completes manualmente. No se guardará ni registrará la clave.';
  }
  return 'Se detectó un paso sensible: login o contraseña. NODAL OS se detuvo para que lo completes manualmente. Cuando termines, presioná "Ya lo hice, continuar".';
}

function sendHandoffUiEvent(type) {
  if (!state.handoff) {
    return;
  }
  const event = {
    type,
    handoffId: state.handoff.handoffId || '',
    runId: state.handoff.runId || state.run.runId || '',
    actionId: state.handoff.actionId || '',
    correlationId: state.handoff.correlationId || '',
    runtimeKind: 'core-governed-companion',
    source: 'chrome-companion',
    authoritative: false,
    verificationStatus: 'NotVerified',
    evidenceRefs: [],
    proofRefs: [],
    redacted: true,
    diagnostics: redactSensitive({
      displayState: state.handoff.displayState,
      reason: state.handoff.reason,
      safeUrl: state.handoff.safeUrl
    })
  };
  state.handoff = {
    ...state.handoff,
    displayState: type === 'handoff.cancelled' ? 'Cancelled' : 'UserCompletedPendingVerification',
    status: type === 'handoff.cancelled' ? 'Cancelled' : 'UserCompleted'
  };
  addLog('extension', event);
  post(event);
  render();
}

async function copyHandoffLog() {
  const log = redactSensitive(JSON.stringify({
    createdAt: new Date().toISOString(),
    runtimeKind: 'core-governed-companion',
    handoff: state.handoff,
    run: state.run,
    diagnostics: state.runtime,
    logs: state.logs.slice(0, 40)
  }, null, 2));
  try {
    await navigator.clipboard.writeText(log);
    addLog('local', 'LOG de handoff copiado.');
  } catch {
    addLog('local', log);
  }
  render();
}

function renderConsent() {
  const consent = state.consent;
  el.consentSurface.classList.toggle('hidden', !consent);
  if (!consent) {
    return;
  }
  el.consentTitle.textContent = 'NODAL OS necesita autorización';
  el.consentStatus.textContent = consent.displayState || consent.status || 'Requested';
  el.consentType.textContent = consent.consentType || '-';
  el.consentScope.textContent = consent.scope || '-';
  el.consentInstruction.textContent = consent.instruction || '-';
  el.consentOptions.textContent = (consent.allowedOptions || []).join(', ') || '-';
  const terminal = ['Denied', 'Expired', 'Revoked', 'GrantedByCore', 'Cancelled', 'Blocked'].includes(consent.displayState);
  el.consentApproveBtn.disabled = terminal;
  el.consentDenyBtn.disabled = terminal;
  el.consentCancelBtn.disabled = terminal;
}

function applyConsentState(message) {
  const consent = normalizeConsent(message);
  if (!consent) {
    return;
  }
  state.consent = consent;
  state.run.status = consent.displayState === 'GrantedByCore' ? state.run.status : 'paused';
  pushTimeline(`Autorización: ${consent.displayState || consent.consentType || 'Requested'}`);
}

function normalizeConsent(message) {
  const presentation = message.presentation || message.consent || message;
  if (!presentation) {
    return null;
  }
  const kind = String(message.type || '').startsWith('profileConsent.') ? 'profile' : 'vault';
  const consentType = presentation.consentType || presentation.type || (kind === 'profile' ? 'ProfileRealConsent' : 'SecretUseConsent');
  return {
    kind,
    consentId: redactSensitive(presentation.consentId || presentation.id || message.consentId || ''),
    runId: redactSensitive(presentation.runId || message.runId || state.run.runId || ''),
    actionId: redactSensitive(presentation.actionId || message.actionId || ''),
    correlationId: redactSensitive(presentation.correlationId || message.correlationId || ''),
    consentType: redactSensitive(consentType),
    scope: redactSensitive(presentation.scope || message.scope || ''),
    status: redactSensitive(presentation.status || ''),
    displayState: consentDisplayStateFrom(message.type, presentation.displayState || presentation.status),
    safeTitle: redactSensitive(presentation.safeTitle || presentation.title || 'NODAL OS necesita autorización'),
    instruction: redactSensitive(presentation.instruction || instructionForConsent(consentType)),
    allowedOptions: Array.isArray(presentation.allowedOptions) && presentation.allowedOptions.length
      ? presentation.allowedOptions.map(redactSensitive)
      : ['AuthorizeIntent', 'DenyIntent', 'Cancel', 'CopyDiagnosticLog'],
    authoritative: false,
    verificationStatus: 'NotVerified',
    redacted: true,
    diagnostics: redactSensitive(presentation.diagnostics || message.diagnostics || '')
  };
}

function consentDisplayStateFrom(type, fallback) {
  if (/\.expired$/.test(type || '')) return 'Expired';
  if (/\.revoked$/.test(type || '')) return 'Revoked';
  if (/\.grantedByCore$/.test(type || '')) return 'GrantedByCore';
  if (/\.deniedByCore$/.test(type || '')) return 'Denied';
  if (fallback === 'Granted') return 'GrantedByCore';
  if (fallback === 'Denied' || fallback === 'Expired' || fallback === 'Revoked' || fallback === 'Cancelled' || fallback === 'Blocked') return fallback;
  return 'Requested';
}

function instructionForConsent(consentType) {
  const normalized = String(consentType || '').toLowerCase();
  if (normalized.includes('profile')) {
    return 'NODAL OS solicita autorización para perfil real futuro. Esto no autoriza secretos ni login.';
  }
  if (normalized.includes('storage')) {
    return 'NODAL OS solicita autorización para guardar una referencia futura. No se mostrará ni guardará el valor secreto en Companion.';
  }
  if (normalized.includes('retrieval')) {
    return 'NODAL OS solicita autorización para recuperar una referencia. Companion no recibirá el valor secreto.';
  }
  if (normalized.includes('cookie')) {
    return 'NODAL OS solicita autorización para cookie o sesión sensible. Esto no autoriza passwords ni tokens.';
  }
  if (normalized.includes('delete') || normalized.includes('rotation')) {
    return 'NODAL OS solicita autorización para cambiar una referencia secreta. Core debe validar antes de ejecutar.';
  }
  return 'NODAL OS solicita autorización scoped. Tu acción en Companion es intención; Core decide.';
}

function sendConsentUiEvent(type) {
  if (!state.consent) {
    return;
  }
  const event = {
    type,
    consentId: state.consent.consentId || '',
    runId: state.consent.runId || state.run.runId || '',
    actionId: state.consent.actionId || '',
    correlationId: state.consent.correlationId || '',
    consentType: state.consent.consentType || '',
    scope: state.consent.scope || '',
    runtimeKind: 'core-governed-companion',
    source: 'chrome-companion',
    authoritative: false,
    verificationStatus: 'NotVerified',
    evidenceRefs: [],
    proofRefs: [],
    redacted: true,
    diagnostics: redactSensitive({
      displayState: state.consent.displayState,
      consentType: state.consent.consentType,
      scope: state.consent.scope
    })
  };
  state.consent = {
    ...state.consent,
    displayState: type.endsWith('.cancelled') ? 'Cancelled' : type.endsWith('.userDenied') ? 'Denied' : 'UserIntentPendingCore',
    status: type.endsWith('.cancelled') ? 'Cancelled' : type.endsWith('.userDenied') ? 'Denied' : 'UserApprovedIntent'
  };
  addLog('extension', event);
  post(event);
  render();
}

async function copyConsentLog() {
  const log = redactSensitive(JSON.stringify({
    createdAt: new Date().toISOString(),
    runtimeKind: 'core-governed-companion',
    consent: state.consent,
    run: state.run,
    diagnostics: state.runtime,
    logs: state.logs.slice(0, 40)
  }, null, 2));
  try {
    await navigator.clipboard.writeText(log);
    addLog('local', 'LOG de autorización copiado.');
  } catch {
    addLog('local', log);
  }
  render();
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
  el.learningStatus.textContent = state.learning.status === 'paused'
    ? 'Aprendizaje pausado. Podes navegar o buscar sin que NODAL OS lo grabe.'
    : state.learning.recording
    ? 'Grabando. NODAL OS está mirando tus acciones. No se guardarán valores sensibles de contraseñas.'
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

  renderTimeline(el.recipeStepTimeline, buildRecipeRunTimeline(run));
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
  const tokenStatus = tokenStatusLabel(connection);
  el.tokenSetupCard.classList.toggle('hidden', tokenStatus !== 'faltante' && tokenStatus !== 'invalido');
  el.runtimeConnection.textContent = connection.state || (connection.connected ? 'connected' : state.connection.status);
  el.runtimeHost.textContent = state.connection.host;
  el.runtimePort.textContent = state.connection.port;
  el.runtimeHealth.textContent = state.connection.health;
  el.runtimeToken.textContent = tokenStatus;
  el.runtimeSocket.textContent = extension.webSocketConnected ? 'connected' : connection.state || 'disconnected';
  el.runtimeClients.textContent = String(connectedCount);
  el.runtimeHeartbeat.textContent = heartbeatLabel(connection, clients);
  el.runtimeDiagnostic.textContent = runtimeDiagnostic(connection, clients);
  el.runtimeRecommendation.textContent = runtimeRecommendation(connection, clients);
  el.runtimeProvider.textContent = ai.provider || 'OpenAI';
  el.runtimeModel.textContent = ai.model || '-';
  el.runtimeApiKey.textContent = ai.hasApiKeyLocal === null || ai.hasApiKeyLocal === undefined ? 'unknown' : ai.hasApiKeyLocal ? 'local key loaded' : 'missing';
  el.runtimeAiError.textContent = ai.lastError || '-';
  el.runtimeClientId.textContent = extension.clientId || connection.clientId || '-';
  el.runtimeLastSeen.textContent = extension.lastSeenAt || connection.lastSeenAt || '-';
  el.runtimeProtocol.textContent = extension.protocolVersion || '-';
  el.pageTab.textContent = extension.tabId || '-';
  el.runtimeUrl.textContent = extension.url || state.operator.page || '-';
  el.runtimeContent.textContent = extension.contentScriptActive ? 'active' : 'unavailable';
  el.runId.textContent = state.run.runId || '-';
  el.runtimeRequestId.textContent = state.run.requestId || run.requestId || '-';
  el.runtimeRunState.textContent = state.run.runId ? state.run.status || run.status || 'idle' : 'idle';
  el.runtimeTool.textContent = state.run.currentTool || run.currentTool || '-';
  el.runtimeLastError.textContent = state.run.lastError || run.lastError || '-';
  renderLogs();
  el.lastToolRequest.textContent = stringify(state.runtime.lastToolRequest);
  el.lastToolResult.textContent = stringify(state.runtime.lastToolResult);
  el.lastRunStatus.textContent = stringify(state.runtime.lastRunStatus);
}

function runtimeDiagnostic(connection, clients) {
  if (!state.connection.runtime && state.connection.health === 'fail') {
    return 'bridge_unreachable';
  }
  if (connection.state === 'tokenRequired') {
    return 'token_required';
  }
  if (connection.state === 'tokenError') {
    return 'invalid_token';
  }
  if (connection.state === 'protocolError') {
    return 'Error de protocolo';
  }
  if (connection.lastError === 'bridge_unreachable') {
    return 'bridge_unreachable';
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

function tokenStatusLabel(connection) {
  if (connection.state === 'tokenError' || state.connection.tokenStatus === 'invalid') {
    return 'invalido';
  }
  if (connection.tokenStatus === 'present' || connection.hasToken || state.connection.hasToken || state.connection.token) {
    return 'guardado';
  }
  if (connection.connected && !state.connection.token) {
    return 'no requerido';
  }
  return 'faltante';
}

function heartbeatLabel(connection, clients) {
  if (connection.connected) {
    return 'OK';
  }
  const firstClient = clients && Array.isArray(clients.clients) ? clients.clients[0] : null;
  if (firstClient && typeof firstClient.lastSeenMs === 'number') {
    return firstClient.lastSeenMs < 45000 ? 'OK' : 'stale';
  }
  return 'unknown';
}

function runtimeRecommendation(connection, clients) {
  if (!state.connection.runtime && state.connection.health === 'fail') {
    return 'Verifica que el bridge este iniciado.';
  }
  if (connection.state === 'tokenRequired') {
    return 'Pega el token de conexion generado por el bridge.';
  }
  if (connection.state === 'tokenError' || state.connection.tokenStatus === 'invalid') {
    return 'El token guardado no coincide con el bridge. Usa Cambiar token y pega ExtensionToken desde config/chrome-lab.local.json.';
  }
  if (connection.lastError === 'bridge_unreachable') {
    return 'Verifica que el bridge este iniciado y vuelve a reconectar.';
  }
  if (!state.connection.hasToken && !state.connection.token) {
    return 'Pega el token de conexion generado por el bridge.';
  }
  if (clients && clients.connectedCount === 0 && !connection.connected) {
    return 'Presiona Reconectar extension o recarga la extension.';
  }
  if (connection.state === 'reconnecting') {
    return 'Esperando reconexion. Si persiste, verifica token y bridge.';
  }
  if (connection.connected) {
    return 'Conexion lista.';
  }
  return connection.lastError || 'Verificar conexion.';
}

function renderLogs() {
  el.localLogs.innerHTML = state.logs.filter((item) => item.direction === 'local').slice(0, 80).map(renderLog).join('');
  el.engineLogs.innerHTML = state.logs.filter((item) => item.direction === 'engine').slice(0, 80).map(renderLog).join('');
  el.extensionLogs.innerHTML = state.logs.filter((item) => item.direction === 'extension').slice(0, 80).map(renderLog).join('');
}

function renderTimeline(node, items) {
  const list = Array.isArray(items) ? items.filter(Boolean).slice(-40) : [];
  node.classList.add('nodal-timeline');
  node.setAttribute('aria-label', 'NODAL OS vertical timeline');
  node.innerHTML = list.length
    ? list.map((item, index) => renderTimelineStep(item, index)).join('')
    : renderTimelineStep({
      title: 'NODAL OS listo',
      description: 'Sin pasos activos. ReadyWithRestrictions no significa produccion.',
      status: 'ready',
      nodeType: 'StatusSummary',
      scopeLabel: 'internal-local',
      riskLevel: 'low',
      safeNextAction: 'Describir una tarea local permitida.'
    }, 0);
}

function renderTimelineStep(item, index) {
  const step = normalizeTimelineStep(item, index);
  const statusClass = cssToken(step.status);
  const nodeClass = cssToken(step.nodeType);
  const substeps = step.subSteps.length
    ? `<ul class="timeline-substeps">${step.subSteps.map(renderTimelineSubStep).join('')}</ul>`
    : '';
  const evidence = step.evidenceRefs.length
    ? `<div class="timeline-evidence"><span>Evidencia</span>${step.evidenceRefs.map(renderEvidenceRef).join('')}</div>`
    : '';
  const grounding = step.groundingSnapshot
    ? renderGroundingCard(step.groundingSnapshot)
    : '';
  const blockers = step.blockers.length
    ? `<div class="timeline-blockers">${step.blockers.map(renderTimelineBlocker).join('')}</div>`
    : '';
  const blockedOptions = step.blockedOptions.length
    ? `<div class="timeline-options"><span>Límites</span>${step.blockedOptions.map((option) => `<b>${safeHtml(option)}</b>`).join('')}</div>`
    : '';
  const safeAction = step.safeNextAction
    ? `<p class="timeline-safe-action"><strong>Next safe action:</strong> ${safeHtml(step.safeNextAction)}</p>`
    : '';
  const authority = step.coreAuthorityRequired
    ? '<span class="timeline-chip core">Core authority required</span>'
    : '<span class="timeline-chip observe">Observe-only</span>';
  const human = step.humanInterventionRequired
    ? '<span class="timeline-chip human">Human intervention</span>'
    : '';

  return `
    <li class="timeline-step status-${statusClass} node-${nodeClass}">
      <span class="timeline-node-dot" aria-hidden="true"></span>
      <article class="timeline-card">
        <header class="timeline-card-head">
          <span class="timeline-order">${step.order}</span>
          <div>
            <h3>${safeHtml(step.title)}</h3>
            <p>${safeHtml(step.description)}</p>
          </div>
          <span class="timeline-badge status-${statusClass}">${safeHtml(step.statusLabel)}</span>
        </header>
        <div class="timeline-meta">
          <span>${safeHtml(step.nodeTypeLabel)}</span>
          <span>${safeHtml(step.scopeLabel)}</span>
          <span>risk: ${safeHtml(step.riskLevel)}</span>
          ${authority}
          ${human}
        </div>
        ${substeps}
        ${grounding}
        ${evidence}
        ${blockers}
        ${safeAction}
        ${blockedOptions}
        <p class="timeline-redaction">${safeHtml(step.redactionSummary)}</p>
      </article>
    </li>`;
}

function renderTimelineSubStep(subStep) {
  const status = cssToken(subStep.status || 'planned');
  return `<li class="timeline-substep status-${status}"><span>${safeHtml(subStep.title)}</span><em>${safeHtml(normalizeTimelineStatus(subStep.status).label)}</em></li>`;
}

function renderEvidenceRef(ref) {
  const item = typeof ref === 'string' ? { refId: ref, label: 'redacted ref' } : ref;
  return `<code>${safeHtml(item.label || 'ref')}: ${safeHtml(item.refId || item.id || item.value || '')}</code>`;
}

function renderGroundingCard(snapshot) {
  const redactionStatus = String(snapshot.redactionStatus || 'Unknown');
  const redactionToken = cssToken(redactionStatus);
  const persistenceAllowed = snapshot.persistenceAllowed !== false
    && !['redactionfailed', 'redaction-failed', 'blockedsensitive', 'blocked-sensitive'].includes(redactionToken);
  const focused = snapshot.focusedElement || snapshot.focused || null;
  const interactables = normalizeGroundedElements(snapshot.visibleInteractables || snapshot.interactables || []);
  const evidenceRefs = normalizeList((snapshot.evidenceRefs || []).map((ref) => ref.refId || ref.id || ref.value || ref));
  const screenshotRef = persistenceAllowed ? (snapshot.screenshotRef || '') : '';
  const warning = persistenceAllowed
    ? ''
    : '<div class="timeline-grounding-warning">Revisar captura: redacción incompleta o contenido sensible. No se guarda captura ni DOM raw.</div>';
  const thumbnail = screenshotRef
    ? `<div class="timeline-grounding-thumb" aria-label="safe redacted screenshot thumbnail">safe thumbnail ref: ${safeHtml(screenshotRef)}</div>`
    : '<div class="timeline-grounding-thumb muted-thumb">no safe screenshot thumbnail</div>';
  const focusedHtml = focused
    ? `<p><strong>Focused:</strong> ${safeHtml(focused.label || focused.role || focused.elementId || 'redacted element')} <small>${safeHtml(focused.redactedSelector || focused.selector || '')}</small></p>`
    : '<p><strong>Focused:</strong> none</p>';
  const elementsHtml = interactables.length
    ? `<ul class="timeline-grounding-elements">${interactables.map((element) => `<li>${safeHtml(element.label || element.role || element.elementId || 'redacted element')} <small>${safeHtml(element.role || '')}</small></li>`).join('')}</ul>`
    : '<p class="timeline-redaction">No visible interactables in redacted snapshot.</p>';

  return `
    <section class="timeline-grounding-card status-${redactionToken}">
      <div class="timeline-grounding-head">
        <strong>Grounding snapshot</strong>
        <span>${safeHtml(snapshot.pageHealth || 'Unknown')}</span>
      </div>
      ${thumbnail}
      ${focusedHtml}
      <p><strong>Risk:</strong> ${safeHtml(snapshot.risk || snapshot.riskSummary || 'low')} · <strong>Redaction:</strong> ${safeHtml(redactionStatus)}</p>
      ${elementsHtml}
      ${evidenceRefs.length ? `<p class="timeline-redaction">Evidence: ${safeHtml(evidenceRefs.join(', '))}</p>` : ''}
      ${warning}
      <p class="timeline-redaction">Debug/evidence only. Screenshot never authorizes action; Core decides.</p>
    </section>`;
}

function renderTimelineBlocker(blocker) {
  const item = typeof blocker === 'string' ? { reason: blocker, expectedOperatorAction: 'Stop and ask Core/human review.' } : blocker;
  return `
    <section class="timeline-blocker-card">
      <strong>Revisar</strong>
      <p>${safeHtml(item.reason || 'Revisar antes de seguir')}</p>
      <small>${safeHtml(item.expectedOperatorAction || 'No bypass allowed.')}</small>
    </section>`;
}

function normalizeTimelineStep(item, index) {
  if (typeof item === 'string') {
    return timelineStepFromText(item, index);
  }

  const status = normalizeTimelineStatus(item.status || item.stepStatus || item.state || 'planned');
  const blockers = Array.isArray(item.blockers) ? item.blockers : [];
  const decision = item.decision || {};
  const statusCard = item.statusCard || {};
  const subSteps = item.subSteps || item.subtasks || item.subtasks || item.children || [];
  const evidenceRefs = item.evidenceRefs || item.evidence || [];
  const blockedOptions = item.blockedOptions || decision.blockedOptions || blockers.flatMap((blocker) => blocker.blockedOptions || []);

  return {
    title: redactSensitive(item.title || item.label || `Paso ${index + 1}`),
    description: redactSensitive(item.description || item.summary || statusCard.summary || ''),
    status: status.value,
    statusLabel: status.label,
    order: item.order || item.index || index + 1,
    nodeType: item.nodeType || (item.node && item.node.nodeType) || 'ExecutionStep',
    nodeTypeLabel: humanizeNodeType(item.nodeType || (item.node && item.node.nodeType) || 'ExecutionStep'),
    subSteps: subSteps.map((sub, subIndex) => normalizeTimelineSubStep(sub, subIndex)),
    evidenceRefs,
    blockers,
    groundingSnapshot: item.groundingSnapshot || item.grounding || null,
    safeNextAction: redactSensitive(item.safeNextAction || decision.safeNextAction || 'Continue only if Core permits.'),
    blockedOptions: normalizeList(blockedOptions),
    coreAuthorityRequired: item.coreAuthorityRequired !== false && decision.coreAuthorityRequired !== false,
    humanInterventionRequired: Boolean(item.humanInterventionRequired || decision.humanInterventionRequired),
    riskLevel: redactSensitive(String(item.riskLevel || statusCard.riskLevel || 'low')).toLowerCase(),
    scopeLabel: redactSensitive(item.scopeLabel || statusCard.scopeLabel || 'internal-local ReadyWithRestrictions'),
    redactionSummary: redactSensitive(item.redactionSummary || 'redacted timeline metadata only; no secrets/cookies/tokens')
  };
}

function timelineStepFromText(text, index, overrides = {}) {
  const status = normalizeTimelineStatus(overrides.status || inferTimelineStatus(text));
  return normalizeTimelineStep({
    title: text,
    description: overrides.description || 'Operator-facing timeline event.',
    status: status.value,
    order: index + 1,
    nodeType: overrides.nodeType || 'ExecutionStep',
    scopeLabel: overrides.scopeLabel || 'internal-local ReadyWithRestrictions',
    riskLevel: overrides.riskLevel || 'low',
    safeNextAction: overrides.safeNextAction || 'Continue only if Core permits.',
    blockedOptions: overrides.blockedOptions || [],
    redactionSummary: 'redacted display text only; no secrets/cookies/tokens'
  }, index);
}

function normalizeTimelineSubStep(subStep, index) {
  if (typeof subStep === 'string') {
    return { title: redactSensitive(subStep), status: 'planned', order: index + 1 };
  }
  return {
    title: redactSensitive(subStep.title || subStep.label || `Subtarea ${index + 1}`),
    status: normalizeTimelineStatus(subStep.status || 'planned').value,
    order: subStep.order || index + 1
  };
}

function buildStructuredTaskTimeline(goal) {
  const summary = goal ? `Pedido: ${goal}` : 'Pedido sin texto. Esperando instruccion local segura.';
  return [
    {
      title: 'Entender pedido',
      description: summary,
      status: 'done',
      nodeType: 'UserRequest',
      subSteps: ['Identificar objetivo', 'Detectar constraints', 'Mantener alcance internal-local'],
      evidenceRefs: [{ label: 'input', refId: 'operator-request:redacted' }],
      safeNextAction: 'Estructurar tarea sin ejecutar acciones sensibles.'
    },
    {
      title: 'Estructurar receta',
      description: 'Reordenar pasos y subtareas en formato timeline antes de ejecutar.',
      status: 'planned',
      nodeType: 'StructuredTask',
      subSteps: ['Crear pasos', 'Identificar subtareas', 'Registrar evidencia requerida'],
      safeNextAction: 'Mostrar receta derivada para revision.'
    },
    {
      title: 'Validar seguridad',
      description: 'Core conserva autoridad; UI/Admin/Companion no aprueban acciones.',
      status: 'blocked',
      nodeType: 'BlockerStep',
      riskLevel: 'prohibited',
      blockers: [{
        reason: 'Credenciales, sitios sensibles, submit/pay/sign/delete y produccion siguen bloqueados.',
        expectedOperatorAction: 'No intentar bypass; pedir intervencion humana/Core si aparece un bloqueo.',
        blockedOptions: ['credentials', 'submit/pay/sign/delete', 'sensitive sites', 'public SaaS', 'external general CDP']
      }],
      safeNextAction: 'Continuar solo con observacion/local read-only permitida.'
    },
    {
      title: 'Proxima accion segura',
      description: 'Ejecutar solo acciones locales/read-only aprobadas por Core.',
      status: 'ready',
      nodeType: 'SafeAction',
      safeNextAction: 'Iniciar run local bajo ReadyWithRestrictions.',
      blockedOptions: ['production', 'SaaS public', 'real credentials', 'real billing/email']
    }
  ];
}

function buildPlanPreviewTimeline(plan) {
  const steps = Array.isArray(plan.steps) ? plan.steps : [];
  const sensitiveActions = normalizeList(plan.sensitiveActionsDetected || plan.sensitiveActions || []);
  const deniedDomains = normalizeList(plan.deniedDomains || []);
  const allowedDomains = normalizeList(plan.allowedDomains || []);
  const policy = plan.policySummary || {};
  const planStatus = normalizePlanStatus(plan.status || 'PlanDrafted');
  const hasSensitive = sensitiveActions.length > 0 || planStatus === 'blocked';

  const header = {
    title: `Plan preview: ${plan.goal || 'Objetivo local'}`,
    description: `Estado: ${plan.status || 'PlanDrafted'}. Plan Core-owned; UI no ejecuta.`,
    status: planStatus,
    nodeType: hasSensitive ? 'BlockerStep' : 'CoreDecision',
    scopeLabel: 'plan-preview internal-local',
    riskLevel: hasSensitive ? 'prohibited' : 'low',
    subSteps: [
      `Allowed domains: ${allowedDomains.join(', ') || 'local-private-preview'}`,
      `Denied domains: ${deniedDomains.join(', ') || 'production, public-saas, sensitive-sites'}`,
      'Core authority required'
    ],
    evidenceRefs: [{ label: 'plan', refId: plan.planId || 'plan-preview:redacted' }],
    blockers: hasSensitive ? [{
      reason: `Sensitive actions detected: ${sensitiveActions.join(', ') || 'blocked by policy'}`,
      expectedOperatorAction: 'Do not execute. Ask Core/human review.',
      blockedOptions: policy.blockedOptions || ['credentials', 'submit/pay/sign/delete', 'sensitive sites']
    }] : [],
    safeNextAction: hasSensitive ? 'Stop and review policy blocker.' : 'Review plan preview; wait for Core decision.',
    blockedOptions: policy.blockedOptions || ['auto execution from UI', 'production/SaaS public', 'external general CDP'],
    coreAuthorityRequired: true,
    humanInterventionRequired: planStatus === 'needs-human' || hasSensitive
  };

  const mappedSteps = steps.map((step, index) => {
    const stepSensitive = normalizeList(step.sensitiveActionsDetected || []).length > 0;
    return {
      title: step.title || `Plan step ${index + 1}`,
      description: step.description || 'Plan preview step; no automatic execution.',
      status: stepSensitive ? 'blocked' : planStatus,
      nodeType: stepSensitive ? 'BlockerStep' : 'StructuredTask',
      scopeLabel: 'plan-preview internal-local',
      riskLevel: stepSensitive ? 'prohibited' : String(step.risk || 'low'),
      evidenceRefs: (step.evidenceRequirements || []).map((req) => ({ label: 'evidence', refId: `plan-evidence:${req}` })),
      blockers: stepSensitive ? [{
        reason: 'Sensitive action detected in plan step.',
        expectedOperatorAction: 'Do not execute from UI.',
        blockedOptions: policy.blockedOptions || ['submit/pay/sign/delete', 'credentials']
      }] : [],
      safeNextAction: stepSensitive ? 'Stop; Core policy review required.' : 'Wait for Core decision.',
      coreAuthorityRequired: true,
      humanInterventionRequired: Boolean(step.humanApprovalRequired || stepSensitive)
    };
  });

  return [header].concat(mappedSteps);
}

function buildRecoveryTimeline(recovery) {
  const signal = recovery.signal || recovery;
  const explanation = recovery.explanation || {};
  const options = Array.isArray(recovery.options) ? recovery.options : [];
  const state = recovery.state || 'RecoveryRequired';
  const evidenceRefs = explanation.evidenceRefs || signal.evidenceRefs || [];
  return [{
    title: `Recovery: ${state}`,
    description: explanation.operatorMessage || explanation.cause || `Runtime stagnation detected: ${signal.kind || 'unknown'}`,
    status: state,
    nodeType: 'HumanIntervention',
    scopeLabel: 'recovery internal-local',
    riskLevel: 'high',
    subSteps: options.map((option) => `${option.label || option.optionId}${option.safe === false ? ' (blocked)' : ''}`),
    evidenceRefs: evidenceRefs.map((refId) => ({ label: 'recovery evidence', refId })),
    blockers: [{
      reason: explanation.cause || signal.kind || 'Recovery required',
      expectedOperatorAction: explanation.requiredHumanAction || 'Review recovery options; Core remains authoritative.',
      blockedOptions: ['credentials', 'captcha/2FA bypass', 'submit/pay/sign/delete', 'sensitive workaround']
    }],
    safeNextAction: recovery.nextSafeAction || 'Stop with evidence, replan, or ask human.',
    blockedOptions: ['credentials', 'captcha/2FA bypass', 'submit/pay/sign/delete', 'sensitive workaround'],
    coreAuthorityRequired: true,
    humanInterventionRequired: true,
    redactionSummary: 'redacted recovery metadata only; no secrets/cookies/tokens'
  }];
}

function buildGroundingTimeline(snapshot) {
  const status = normalizeGroundingStatus(snapshot.redactionStatus, snapshot.pageHealth);
  const blocked = status === 'blocked' || status === 'warning';
  return [{
    title: 'Grounding snapshot',
    description: 'DOM + screenshot metadata for operator debug/evidence. Screenshot is never authority.',
    status,
    nodeType: blocked ? 'BlockerStep' : 'EvidenceStep',
    scopeLabel: 'grounding internal-local',
    riskLevel: blocked ? 'high' : String(snapshot.risk || 'low'),
    subSteps: [
      `Page health: ${snapshot.pageHealth || 'Unknown'}`,
      `DOM hash: ${snapshot.domHash ? 'available redacted' : 'missing'}`,
      `Screenshot hash: ${snapshot.screenshotHash ? 'available redacted' : 'missing'}`
    ],
    evidenceRefs: (snapshot.evidenceRefs || []).map((ref) => ({ label: ref.label || 'grounding evidence', refId: ref.refId || ref.id || ref })),
    blockers: blocked ? [{
      reason: snapshot.riskSummary || 'Grounding snapshot blocked or warning due to redaction/page health.',
      expectedOperatorAction: 'Do not persist raw screenshot/DOM. Ask Core/human review if needed.',
      blockedOptions: ['raw screenshot', 'raw DOM/body', 'cookies/tokens/secrets', 'screenshot-only action']
    }] : [],
    groundingSnapshot: snapshot,
    safeNextAction: blocked ? 'Stop and review redaction/page health.' : 'Review redacted grounding evidence only.',
    blockedOptions: ['screenshot-only action', 'raw DOM/body persistence', 'credentials', 'submit/pay/sign/delete'],
    coreAuthorityRequired: true,
    humanInterventionRequired: blocked,
    redactionSummary: 'redacted grounding metadata only; no raw DOM/body, cookies, tokens, credentials, or sensitive screenshot'
  }];
}

function buildRecipeRunTimeline(run) {
  const results = Array.isArray(run.stepResults) ? run.stepResults : [];
  if (!results.length) {
    return [];
  }
  return results.map((result, index) => {
    const label = run.recipe && run.recipe.steps && run.recipe.steps[index] ? run.recipe.steps[index].label || result.type : result.type;
    const status = normalizeRecipeStatus(result.status);
    return {
      title: `${index + 1}. ${label || 'Recipe step'}`,
      description: result.error || 'Recipe step status rendered as redacted timeline.',
      status,
      nodeType: result.error ? 'BlockerStep' : 'RecipeStep',
      evidenceRefs: result.evidenceRef ? [{ label: 'recipe evidence', refId: result.evidenceRef }] : [],
      blockers: result.error ? [{ reason: result.error, expectedOperatorAction: 'Review blocked recipe step before retry.' }] : [],
      safeNextAction: result.error ? 'Stop and review.' : 'Continue if Core permits.',
      scopeLabel: 'recipe local/private preview',
      riskLevel: result.error ? 'high' : 'low'
    };
  });
}

function normalizeRecipeStatus(status) {
  const value = String(status || '').toLowerCase();
  if (value === 'passed') return 'done';
  if (value === 'running') return 'running';
  if (value === 'failed') return 'failed';
  if (value === 'skipped') return 'skipped';
  return 'planned';
}

function inferTimelineStatus(text) {
  const value = String(text || '').toLowerCase();
  if (value.includes('stop') || value.includes('block') || value.includes('denied')) return 'blocked';
  if (value.includes('fail') || value.includes('error')) return 'failed';
  if (value.includes('human') || value.includes('intervenci')) return 'needsHuman';
  if (value.includes('ok') || value.includes('done') || value.includes('complet')) return 'done';
  if (value.includes('running') || value.includes('iniciado')) return 'running';
  return 'planned';
}

function normalizeTimelineStatus(status) {
  const value = String(status || 'planned').replace(/([a-z])([A-Z])/g, '$1-$2').toLowerCase();
  const map = {
    pending: ['pending', 'Pendiente'],
    planned: ['planned', 'Planned'],
    ready: ['ready', 'Ready'],
    running: ['running', 'Running'],
    done: ['done', 'Done'],
    completed: ['done', 'Done'],
    passed: ['done', 'Done'],
    blocked: ['blocked', 'Revisar'],
    'needs-human': ['needs-human', 'Needs human'],
    needshuman: ['needs-human', 'Needs human'],
    'evidence-required': ['evidence-required', 'Evidence required'],
    evidencerequired: ['evidence-required', 'Evidence required'],
    'evidence-ready': ['evidence-ready', 'Evidence ready'],
    evidenceready: ['evidence-ready', 'Evidence ready'],
    skipped: ['skipped', 'Skipped'],
    warning: ['warning', 'Warning'],
    failed: ['failed', 'Failed'],
    error: ['failed', 'Failed'],
    'plan-drafted': ['planned', 'Plan drafted'],
    plandrafted: ['planned', 'Plan drafted'],
    'plan-preview-ready': ['ready', 'Plan preview ready'],
    planpreviewready: ['ready', 'Plan preview ready'],
    'plan-awaiting-approval': ['needs-human', 'Awaiting approval'],
    planawaitingapproval: ['needs-human', 'Awaiting approval'],
    'plan-approved': ['ready', 'Plan approved'],
    planapproved: ['ready', 'Plan approved'],
    'plan-rejected': ['not-allowed', 'Plan rejected'],
    planrejected: ['not-allowed', 'Plan rejected'],
    'plan-edited-by-human': ['warning', 'Edited by human'],
    planeditedbyhuman: ['warning', 'Edited by human'],
    'execution-started': ['running', 'Execution started'],
    executionstarted: ['running', 'Execution started'],
    'execution-blocked-by-policy': ['blocked', 'Revisar antes de seguir'],
    executionblockedbypolicy: ['blocked', 'Revisar antes de seguir'],
    'recovery-required': ['blocked', 'Recovery required'],
    recoveryrequired: ['blocked', 'Recovery required'],
    'waiting-for-human-input': ['needs-human', 'Waiting for human'],
    waitingforhumaninput: ['needs-human', 'Waiting for human'],
    'not-allowed': ['not-allowed', 'Not allowed'],
    notallowed: ['not-allowed', 'Not allowed']
  };
  const normalized = map[value] || ['planned', 'Planned'];
  return { value: normalized[0], label: normalized[1] };
}

function normalizePlanStatus(status) {
  return normalizeTimelineStatus(status).value;
}

function normalizeGroundingStatus(redactionStatus, pageHealth) {
  const redaction = cssToken(redactionStatus || 'Unknown');
  const health = cssToken(pageHealth || 'Unknown');
  if (redaction === 'redaction-failed' || redaction === 'blockedsensitive' || redaction === 'blocked-sensitive') {
    return 'blocked';
  }
  if (health === 'loading' || health === 'notloaded' || health === 'not-loaded') {
    return 'warning';
  }
  if (health === 'blocked' || health === 'error') {
    return 'blocked';
  }
  return 'evidence-ready';
}

function humanizeNodeType(nodeType) {
  return String(nodeType || 'ExecutionStep')
    .replace(/([a-z])([A-Z])/g, '$1 $2')
    .replace(/-/g, ' ');
}

function normalizeList(values) {
  return (Array.isArray(values) ? values : [])
    .filter(Boolean)
    .map((value) => redactSensitive(String(value)))
    .filter((value, index, list) => list.indexOf(value) === index)
    .slice(0, 8);
}

function normalizeGroundedElements(values) {
  return (Array.isArray(values) ? values : [])
    .filter(Boolean)
    .map((value) => redactSensitive(value))
    .slice(0, 8);
}

function cssToken(value) {
  return String(value || 'planned').replace(/([a-z])([A-Z])/g, '$1-$2').toLowerCase().replace(/[^a-z0-9-]+/g, '-');
}

function safeHtml(value) {
  return escapeHtml(redactSensitive(value == null ? '' : String(value)));
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
  a.download = `${recipe.name || 'nodal-os-recipe'}.json`.replace(/[^a-z0-9._-]+/gi, '_');
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
  const text = redactSensitive(typeof value === 'string' ? value : JSON.stringify(value));
  return text && text.length > 900 ? `${text.slice(0, 900)}...` : text || '';
}

function redactSensitive(value) {
  if (value === null || value === undefined) {
    return value;
  }
  if (typeof value === 'object') {
    return JSON.parse(redactSensitive(JSON.stringify(value)));
  }
  return String(value)
    .replace(/s[k]-[A-Za-z0-9_-]{8,}/gi, '[redacted]')
    .replace(/authorization\s*[:=]\s*bearer\s+[A-Za-z0-9._-]+/gi, 'authorization=[redacted]')
    .replace(/bearer\s+[A-Za-z0-9._-]+/gi, 'bearer [redacted]')
    .replace(/(password|passwd|secret|token|access_token|refresh_token|id_token|api[_-]?key|cookie|set-cookie|authorization|otp|code|clave(?:\s+fiscal)?|sessionid|csrf|xsrf|jwt|client_secret)\s*[:=]\s*[^;\s,}]+/gi, '$1=[redacted]')
    .replace(/\b(CUIT|DNI)\s*[:=]\s*\d{7,11}\b/gi, '$1=[redacted]');
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
