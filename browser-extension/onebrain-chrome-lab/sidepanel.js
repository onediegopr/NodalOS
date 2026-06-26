let port = null;
let portConnected = false;
const DEMO_STORE_KEY = 'nodal-os.demoMissions.v1';
const DEMO_GUIDANCE_COLLAPSED_KEY = 'nodal-os.demoGuidanceCollapsed.v1';
const BROWSER_SKILLS_SNAPSHOT_KEY = 'nodal-os.browserSkills.snapshots.v1';
const BROWSER_SKILLS_MAX_SNAPSHOTS = 20;
const CDP_BROWSER_SKILLS_SURFACE = {
  runtimeLabel: 'CloakBrowser CDP',
  status: 'Disponible',
  source: 'cloakbrowser-cdp-direct',
  extensionMode: 'legacy / no-default',
  extensionUsed: false,
  systemBrowserUsed: false,
  externalNavigationBlocked: true,
  productFilesModified: false,
  readOnly: true,
  domIndex: 'metadata-only',
  elementCount: 6,
  frictionCount: 5,
  actionMapCount: 6,
  screenshotCaptured: true,
  evidenceAvailable: true
};
const WORKSPACE_STORE_KEY = 'nodal-os.workspaceUnderstanding.v1';
const WORKSPACE_SCAN_LIMITS = {
  maxDepth: 4,
  maxFiles: 520,
  maxFolders: 180,
  maxTreeItems: 90,
  maxKeyFiles: 40
};
const WORKSPACE_IGNORED_DIRS = new Set([
  'node_modules',
  'bin',
  'obj',
  '.git',
  '.vs',
  '.idea',
  '.next',
  'dist',
  'build',
  'coverage',
  '.turbo',
  '.cache',
  '.pnpm-store',
  '.yarn',
  'packages-cache'
]);
const WORKSPACE_IGNORED_FILE_NAMES = new Set(['.git']);
const WORKSPACE_IGNORED_FILE_PREFIXES = ['.env'];
const WORKSPACE_PROTECTED_PATH_PREFIXES = [
  'stealth-engine',
  'stealth-panel',
  'src/onebrain.chromelab.bridge/stealth',
  'src/onebrain.chromelab.bridge/sessions'
];
const WORKSPACE_PROTECTED_PATHS = new Set([
  'docker-compose.yml',
  'scripts/deploy.ps1',
  'scripts/stop.ps1',
  'src/onebrain.chromelab.bridge/dockerfile',
  'src/onebrain.chromelab.bridge/chromelaboptions.cs',
  'src/onebrain.chromelab.bridge/chromelabprotocol.cs',
  'src/onebrain.chromelab.bridge/program.cs',
  'src/onebrain.browserexecutor.cdp/browsercredentialboundaryservice.cs',
  'src/onebrain.browserexecutor.contracts/browsercredentialboundarycontracts.cs',
  'docs/stealth-engine-design.md',
  'docs/stealth-audit-report.md',
  'docs/stealth-reaudit-report.md',
  'docs/unified-friction-integration-design.md',
  'docs/architecture.md',
  'docs/configuration.md',
  'docs/deployment.md',
  'docs/operations.md',
  'docs/roadmap.md',
  'changelog.md'
]);
const MISSION_PLAN_TASK_RANGE = { min: 3, max: 7 };
const PROPOSAL_STATUS = {
  draft: 'Borrador',
  ready: 'Listo para revisar',
  needsContext: 'Requiere contexto',
  reviewed: 'Revisado'
};
const CHANGE_CANDIDATE_STATUS = {
  candidate: 'Candidato',
  reviewing: 'En revisión',
  reviewed: 'Revisado',
  needsContext: 'Necesita contexto',
  discarded: 'Descartado'
};
const CHANGE_CANDIDATE_LIMITS = { min: 2, max: 6 };
const DEMO_SCRIPT_STEPS = [
  'Abrí NODAL OS y presentá Mission Control como centro de misiones locales.',
  'Creá una misión corta para mostrar que el flujo empieza desde una intención simple.',
  'Ejecutá Run demo para generar timeline, evidencia e historial en segundos.',
  'Abrí Historial y reabrí el run para mostrar continuidad local.',
  'Agregá una nota breve para que el run tenga contexto de video.',
  'Copiá el resumen y cerrá con el valor: una demo local, visible y compartible.'
];

const state = {
  activeTab: 'operate',
  demo: loadDemoStore(),
  browserSkills: loadBrowserSkillStore(),
  workspace: loadWorkspaceStore(),
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
  demoGuidanceCard: document.getElementById('demoGuidanceCard'),
  demoGuidanceBody: document.getElementById('demoGuidanceBody'),
  toggleGuidanceBtn: document.getElementById('toggleGuidanceBtn'),
  guideStepMission: document.getElementById('guideStepMission'),
  guideStepRun: document.getElementById('guideStepRun'),
  guideStepCopy: document.getElementById('guideStepCopy'),
  demoReadyCard: document.getElementById('demoReadyCard'),
  demoReadyLabel: document.getElementById('demoReadyLabel'),
  demoReadyText: document.getElementById('demoReadyText'),
  missionCreateForm: document.getElementById('missionCreateForm'),
  missionTitleInput: document.getElementById('missionTitleInput'),
  missionDescriptionInput: document.getElementById('missionDescriptionInput'),
  missionList: document.getElementById('missionList'),
  clearDemoHistoryBtn: document.getElementById('clearDemoHistoryBtn'),
  editMissionBtn: document.getElementById('editMissionBtn'),
  missionEditFields: document.getElementById('missionEditFields'),
  editMissionTitleInput: document.getElementById('editMissionTitleInput'),
  editMissionDescriptionInput: document.getElementById('editMissionDescriptionInput'),
  saveMissionBtn: document.getElementById('saveMissionBtn'),
  cancelMissionEditBtn: document.getElementById('cancelMissionEditBtn'),
  deleteMissionBtn: document.getElementById('deleteMissionBtn'),
  regenerateMissionPlanBtn: document.getElementById('regenerateMissionPlanBtn'),
  copyMissionPlanBtn: document.getElementById('copyMissionPlanBtn'),
  clearMissionPlanBtn: document.getElementById('clearMissionPlanBtn'),
  missionPlanContext: document.getElementById('missionPlanContext'),
  missionPlanChips: document.getElementById('missionPlanChips'),
  missionTaskGraph: document.getElementById('missionTaskGraph'),
  generateProposalBtn: document.getElementById('generateProposalBtn'),
  regenerateProposalBtn: document.getElementById('regenerateProposalBtn'),
  copyProposalBtn: document.getElementById('copyProposalBtn'),
  reviewProposalBtn: document.getElementById('reviewProposalBtn'),
  clearProposalBtn: document.getElementById('clearProposalBtn'),
  missionProposalContext: document.getElementById('missionProposalContext'),
  missionProposalFlags: document.getElementById('missionProposalFlags'),
  missionProposalSummary: document.getElementById('missionProposalSummary'),
  missionProposalDetails: document.getElementById('missionProposalDetails'),
  generateCandidatesBtn: document.getElementById('generateCandidatesBtn'),
  regenerateCandidatesBtn: document.getElementById('regenerateCandidatesBtn'),
  copyCandidatesBtn: document.getElementById('copyCandidatesBtn'),
  reviewCandidatesBtn: document.getElementById('reviewCandidatesBtn'),
  clearCandidatesBtn: document.getElementById('clearCandidatesBtn'),
  changeCandidateContext: document.getElementById('changeCandidateContext'),
  changeCandidateFlags: document.getElementById('changeCandidateFlags'),
  changeCandidateSummary: document.getElementById('changeCandidateSummary'),
  changeCandidateList: document.getElementById('changeCandidateList'),
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
  demoScriptPanel: document.getElementById('demoScriptPanel'),
  copyDemoScriptBtn: document.getElementById('copyDemoScriptBtn'),
  runNoteInput: document.getElementById('runNoteInput'),
  runNoteState: document.getElementById('runNoteState'),
  saveRunNoteBtn: document.getElementById('saveRunNoteBtn'),
  clearRunNoteBtn: document.getElementById('clearRunNoteBtn'),
  demoTechnicalReport: document.getElementById('demoTechnicalReport'),
  captureBrowserTabBtn: document.getElementById('captureBrowserTabBtn'),
  indexBrowserPageBtn: document.getElementById('indexBrowserPageBtn'),
  copyBrowserSkillSummaryBtn: document.getElementById('copyBrowserSkillSummaryBtn'),
  clearBrowserSnapshotsBtn: document.getElementById('clearBrowserSnapshotsBtn'),
  openWorkspaceBtn: document.getElementById('openWorkspaceBtn'),
  openWorkspaceFallbackBtn: document.getElementById('openWorkspaceFallbackBtn'),
  workspaceDirectoryInput: document.getElementById('workspaceDirectoryInput'),
  rescanWorkspaceBtn: document.getElementById('rescanWorkspaceBtn'),
  copyWorkspaceEvidenceBtn: document.getElementById('copyWorkspaceEvidenceBtn'),
  clearWorkspaceBtn: document.getElementById('clearWorkspaceBtn'),
  workspaceStatus: document.getElementById('workspaceStatus'),
  workspaceName: document.getElementById('workspaceName'),
  workspacePath: document.getElementById('workspacePath'),
  workspaceReadMode: document.getElementById('workspaceReadMode'),
  workspaceCounts: document.getElementById('workspaceCounts'),
  workspaceStack: document.getElementById('workspaceStack'),
  workspaceSignals: document.getElementById('workspaceSignals'),
  workspaceKeyFiles: document.getElementById('workspaceKeyFiles'),
  workspaceTree: document.getElementById('workspaceTree'),
  workspaceEvidence: document.getElementById('workspaceEvidence'),
  missionWorkspaceContext: document.getElementById('missionWorkspaceContext'),
  browserSkillStatus: document.getElementById('browserSkillStatus'),
  browserSkillUrl: document.getElementById('browserSkillUrl'),
  browserSkillTitleValue: document.getElementById('browserSkillTitleValue'),
  browserSkillElementCount: document.getElementById('browserSkillElementCount'),
  browserSkillFriction: document.getElementById('browserSkillFriction'),
  cdpRuntimeLabel: document.getElementById('cdpRuntimeLabel'),
  cdpBrowserSkillStatus: document.getElementById('cdpBrowserSkillStatus'),
  cdpExtensionMode: document.getElementById('cdpExtensionMode'),
  cdpElementCount: document.getElementById('cdpElementCount'),
  cdpFrictionCount: document.getElementById('cdpFrictionCount'),
  cdpActionMapCount: document.getElementById('cdpActionMapCount'),
  cdpSourceState: document.getElementById('cdpSourceState'),
  cdpDomIndexState: document.getElementById('cdpDomIndexState'),
  cdpEvidenceState: document.getElementById('cdpEvidenceState'),
  cdpScreenshotState: document.getElementById('cdpScreenshotState'),
  cdpExtensionUsedState: document.getElementById('cdpExtensionUsedState'),
  cdpSystemBrowserState: document.getElementById('cdpSystemBrowserState'),
  cdpExternalNavState: document.getElementById('cdpExternalNavState'),
  cdpFilesModifiedState: document.getElementById('cdpFilesModifiedState'),
  copyCdpBrowserSkillSummaryBtn: document.getElementById('copyCdpBrowserSkillSummaryBtn'),
  browserIndexedElements: document.getElementById('browserIndexedElements'),
  browserEvidencePanel: document.getElementById('browserEvidencePanel'),
  browserSnapshotHistory: document.getElementById('browserSnapshotHistory'),
  browserCaptchaState: document.getElementById('browserCaptchaState'),
  browserProxyState: document.getElementById('browserProxyState'),
  browserStealthState: document.getElementById('browserStealthState'),
  browserSessionResilienceState: document.getElementById('browserSessionResilienceState'),
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
  el.toggleGuidanceBtn.addEventListener('click', toggleDemoGuidance);
  el.missionCreateForm.addEventListener('submit', createMissionFromForm);
  el.clearDemoHistoryBtn.addEventListener('click', clearDemoHistory);
  el.editMissionBtn.addEventListener('click', beginEditMission);
  el.saveMissionBtn.addEventListener('click', saveMissionEdit);
  el.cancelMissionEditBtn.addEventListener('click', cancelMissionEdit);
  el.deleteMissionBtn.addEventListener('click', deleteActiveMission);
  el.regenerateMissionPlanBtn.addEventListener('click', regenerateMissionPlan);
  el.copyMissionPlanBtn.addEventListener('click', copyMissionPlan);
  el.clearMissionPlanBtn.addEventListener('click', clearMissionPlan);
  el.generateProposalBtn.addEventListener('click', generateProposal);
  el.regenerateProposalBtn.addEventListener('click', regenerateProposal);
  el.copyProposalBtn.addEventListener('click', copyProposal);
  el.reviewProposalBtn.addEventListener('click', markProposalReviewed);
  el.clearProposalBtn.addEventListener('click', clearProposal);
  el.generateCandidatesBtn.addEventListener('click', generateChangeCandidates);
  el.regenerateCandidatesBtn.addEventListener('click', regenerateChangeCandidates);
  el.copyCandidatesBtn.addEventListener('click', copyChangeCandidates);
  el.reviewCandidatesBtn.addEventListener('click', markChangeCandidatesReviewed);
  el.clearCandidatesBtn.addEventListener('click', clearChangeCandidates);
  el.runSafeDemoBtn.addEventListener('click', runSafeDemo);
  el.copyDemoReportBtn.addEventListener('click', copyDemoReport);
  el.copyDemoScriptBtn.addEventListener('click', copyDemoScript);
  el.saveRunNoteBtn.addEventListener('click', saveRunNote);
  el.clearRunNoteBtn.addEventListener('click', clearRunNote);
  el.captureBrowserTabBtn.addEventListener('click', captureBrowserActiveTab);
  el.indexBrowserPageBtn.addEventListener('click', indexBrowserActivePage);
  el.copyBrowserSkillSummaryBtn.addEventListener('click', copyBrowserSkillSummary);
  el.copyCdpBrowserSkillSummaryBtn.addEventListener('click', copyCdpBrowserSkillSummary);
  el.clearBrowserSnapshotsBtn.addEventListener('click', clearBrowserSnapshotHistory);
  el.openWorkspaceBtn.addEventListener('click', openWorkspaceDirectory);
  el.openWorkspaceFallbackBtn.addEventListener('click', openWorkspaceDirectoryInput);
  el.workspaceDirectoryInput.addEventListener('change', handleWorkspaceDirectoryInput);
  el.rescanWorkspaceBtn.addEventListener('click', rescanWorkspaceDirectory);
  el.copyWorkspaceEvidenceBtn.addEventListener('click', copyWorkspaceEvidence);
  el.clearWorkspaceBtn.addEventListener('click', clearWorkspaceUnderstanding);

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
  renderWorkspaceUnderstanding();
  renderBrowserSkills();
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
    schemaVersion: 2,
    missions: [mission],
    activeMissionId: mission.id,
    selectedRunId: '',
    editingMission: false,
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

function loadDemoGuidanceCollapsed() {
  try {
    return localStorage.getItem(DEMO_GUIDANCE_COLLAPSED_KEY) === 'true';
  } catch {
    return false;
  }
}

function saveDemoGuidanceCollapsed(collapsed) {
  try {
    localStorage.setItem(DEMO_GUIDANCE_COLLAPSED_KEY, collapsed ? 'true' : 'false');
  } catch (error) {
    console.warn('NODAL OS demo guidance preference unavailable', error);
  }
}

function loadBrowserSkillStore() {
  try {
    const raw = localStorage.getItem(BROWSER_SKILLS_SNAPSHOT_KEY);
    if (raw) {
      const parsed = JSON.parse(raw);
      const snapshots = Array.isArray(parsed && parsed.snapshots)
        ? parsed.snapshots.map(normalizeBrowserSkillSnapshot).filter(Boolean)
        : [];
      return {
        snapshots,
        selectedSnapshotId: parsed && parsed.selectedSnapshotId && snapshots.some((item) => item.id === parsed.selectedSnapshotId)
          ? parsed.selectedSnapshotId
          : snapshots[0] ? snapshots[0].id : '',
        status: 'idle',
        statusMessage: 'Listo para capturar la pestaña activa.',
        lastError: ''
      };
    }
  } catch (error) {
    console.warn('NODAL OS browser skills store unavailable', error);
  }
  return {
    snapshots: [],
    selectedSnapshotId: '',
    status: 'idle',
    statusMessage: 'Listo para capturar la pestaña activa.',
    lastError: ''
  };
}

function saveBrowserSkillStore(store = state.browserSkills) {
  try {
    const payload = {
      schemaVersion: 1,
      snapshots: (store.snapshots || []).slice(0, BROWSER_SKILLS_MAX_SNAPSHOTS),
      selectedSnapshotId: store.selectedSnapshotId || ''
    };
    localStorage.setItem(BROWSER_SKILLS_SNAPSHOT_KEY, JSON.stringify(payload));
  } catch (error) {
    console.warn('NODAL OS browser skills store save failed', error);
  }
}

function createEmptyWorkspaceStore() {
  return {
    schemaVersion: 1,
    status: 'empty',
    statusMessage: 'Seleccioná una carpeta para entender el proyecto.',
    name: '',
    pathLabel: '',
    source: '',
    openedAt: '',
    scannedAt: '',
    counts: { files: 0, folders: 0, ignoredFolders: 0, listedItems: 0 },
    stacks: [],
    keyFiles: [],
    treeItems: [],
    signals: [],
    evidence: null,
    context: null,
    error: '',
    directoryHandle: null
  };
}

function loadWorkspaceStore() {
  try {
    const raw = localStorage.getItem(WORKSPACE_STORE_KEY);
    if (raw) {
      return normalizeWorkspaceStore(JSON.parse(raw));
    }
  } catch (error) {
    console.warn('NODAL OS workspace store unavailable', error);
  }
  return createEmptyWorkspaceStore();
}

function saveWorkspaceStore(store = state.workspace) {
  try {
    const payload = {
      schemaVersion: 1,
      status: store.status === 'ready' ? 'metadata' : store.status || 'empty',
      statusMessage: store.status === 'ready'
        ? 'Resumen guardado. Releé para actualizar estructura.'
        : store.statusMessage || '',
      name: store.name || '',
      pathLabel: store.pathLabel || '',
      source: store.source || '',
      openedAt: store.openedAt || '',
      scannedAt: store.scannedAt || '',
      counts: store.counts || { files: 0, folders: 0, ignoredFolders: 0, listedItems: 0 },
      stacks: store.stacks || [],
      keyFiles: store.keyFiles || [],
      treeItems: store.treeItems || [],
      signals: store.signals || [],
      evidence: store.evidence || null,
      context: store.context || createWorkspaceContextContract(store),
      error: store.error || ''
    };
    localStorage.setItem(WORKSPACE_STORE_KEY, JSON.stringify(payload));
  } catch (error) {
    console.warn('NODAL OS workspace store save failed', error);
  }
}

function normalizeWorkspaceStore(store) {
  if (!store || typeof store !== 'object') {
    return createEmptyWorkspaceStore();
  }
  return {
    ...createEmptyWorkspaceStore(),
    schemaVersion: 1,
    status: String(store.status || 'metadata'),
    statusMessage: String(store.statusMessage || 'Resumen guardado. Releé para actualizar estructura.'),
    name: String(store.name || ''),
    pathLabel: String(store.pathLabel || ''),
    source: String(store.source || ''),
    openedAt: String(store.openedAt || ''),
    scannedAt: String(store.scannedAt || ''),
    counts: normalizeWorkspaceCounts(store.counts),
    stacks: normalizeWorkspaceTextList(store.stacks),
    keyFiles: normalizeWorkspaceTextList(store.keyFiles).slice(0, WORKSPACE_SCAN_LIMITS.maxKeyFiles),
    treeItems: Array.isArray(store.treeItems)
      ? store.treeItems.map(normalizeWorkspaceTreeItem).filter(Boolean).slice(0, WORKSPACE_SCAN_LIMITS.maxTreeItems)
      : [],
    signals: Array.isArray(store.signals)
      ? store.signals.map(normalizeWorkspaceSignal).filter(Boolean)
      : [],
    evidence: store.evidence && typeof store.evidence === 'object' ? store.evidence : null,
    context: store.context && typeof store.context === 'object'
      ? normalizeWorkspaceContextContract(store.context)
      : createWorkspaceContextContract(store),
    error: String(store.error || ''),
    directoryHandle: null
  };
}

function normalizeWorkspaceCounts(counts) {
  return {
    files: Number.isFinite(counts && counts.files) ? counts.files : 0,
    folders: Number.isFinite(counts && counts.folders) ? counts.folders : 0,
    ignoredFolders: Number.isFinite(counts && counts.ignoredFolders) ? counts.ignoredFolders : 0,
    listedItems: Number.isFinite(counts && counts.listedItems) ? counts.listedItems : 0
  };
}

function normalizeWorkspaceTextList(items) {
  return Array.isArray(items)
    ? items.map((item) => String(item || '').trim()).filter(Boolean)
    : [];
}

function normalizeWorkspaceTreeItem(item) {
  if (!item || typeof item !== 'object') {
    return null;
  }
  return {
    path: String(item.path || ''),
    kind: item.kind === 'directory' ? 'directory' : 'file',
    depth: Number.isFinite(item.depth) ? item.depth : 0,
    important: Boolean(item.important)
  };
}

function normalizeWorkspaceSignal(item) {
  if (!item || typeof item !== 'object') {
    return null;
  }
  return {
    label: String(item.label || ''),
    value: String(item.value || '')
  };
}

function createWorkspaceContextContract(workspace) {
  const source = workspace || {};
  const counts = normalizeWorkspaceCounts(source.counts);
  return normalizeWorkspaceContextContract({
    workspaceName: source.name || '',
    source: source.source || '',
    selectedAt: source.openedAt || source.selectedAt || '',
    lastReadAt: source.scannedAt || source.lastReadAt || '',
    fileCount: counts.files,
    directoryCount: counts.folders,
    stacks: source.stacks || [],
    keyFiles: source.keyFiles || [],
    treeSummary: source.treeItems || [],
    evidenceSummary: source.evidence && source.evidence.summary ? source.evidence.summary : '',
    readOnly: true,
    commandsExecuted: false,
    filesModified: false
  });
}

function normalizeWorkspaceContextContract(context) {
  return {
    workspaceName: String(context.workspaceName || ''),
    source: String(context.source || ''),
    selectedAt: String(context.selectedAt || ''),
    lastReadAt: String(context.lastReadAt || ''),
    fileCount: Number.isFinite(context.fileCount) ? context.fileCount : 0,
    directoryCount: Number.isFinite(context.directoryCount) ? context.directoryCount : 0,
    stacks: normalizeWorkspaceTextList(context.stacks),
    keyFiles: normalizeWorkspaceTextList(context.keyFiles).slice(0, 12),
    treeSummary: Array.isArray(context.treeSummary)
      ? context.treeSummary.map(normalizeWorkspaceTreeItem).filter(Boolean).slice(0, 20)
      : [],
    evidenceSummary: String(context.evidenceSummary || ''),
    readOnly: context.readOnly !== false,
    commandsExecuted: context.commandsExecuted === true,
    filesModified: context.filesModified === true
  };
}

function normalizeBrowserSkillSnapshot(snapshot) {
  if (!snapshot || typeof snapshot !== 'object') {
    return null;
  }
  const elements = Array.isArray(snapshot.elements)
    ? snapshot.elements.map(normalizeBrowserIndexedElement).filter(Boolean)
    : [];
  const frictionEvents = Array.isArray(snapshot.frictionEvents)
    ? snapshot.frictionEvents.map(normalizeBrowserFrictionEvent).filter(Boolean)
    : [];
  return {
    id: String(snapshot.id || `browser-snapshot-${Date.now().toString(36)}`),
    url: String(snapshot.url || ''),
    title: String(snapshot.title || ''),
    capturedAt: snapshot.capturedAt || new Date().toISOString(),
    source: String(snapshot.source || 'sidepanel'),
    status: String(snapshot.status || 'captured'),
    elements,
    frictionEvents,
    summary: String(snapshot.summary || ''),
    suggestedAction: String(snapshot.suggestedAction || browserSuggestedAction(frictionEvents)),
    missionId: String(snapshot.missionId || ''),
    missionTitle: String(snapshot.missionTitle || ''),
    capabilitySummary: snapshot.capabilitySummary || browserCapabilitySummary(frictionEvents)
  };
}

function normalizeBrowserIndexedElement(element) {
  if (!element || typeof element !== 'object') {
    return null;
  }
  return {
    tag: String(element.tag || ''),
    role: String(element.role || ''),
    label: String(element.label || element.text || ''),
    selector: String(element.selector || ''),
    visible: element.visible !== false,
    confidence: Number.isFinite(element.confidence) ? element.confidence : 0.7
  };
}

function normalizeBrowserFrictionEvent(event) {
  if (!event || typeof event !== 'object') {
    return null;
  }
  return {
    type: String(event.type || 'unknown'),
    label: String(event.label || event.type || 'Fricción detectada'),
    evidence: String(event.evidence || ''),
    suggestedAction: String(event.suggestedAction || '')
  };
}

function saveDemoStore(store = state.demo) {
  try {
    const payload = {
      schemaVersion: 2,
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
    schemaVersion: 2,
    missions: missions.length ? missions : seed.missions,
    activeMissionId: store && store.activeMissionId ? store.activeMissionId : '',
    selectedRunId: store && store.selectedRunId ? store.selectedRunId : '',
    editingMission: false
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
    updatedAt: createdAt,
    status: 'ready',
    runs: [],
    browserSkillSnapshots: [],
    workspace: null,
    missionContext: null,
    plan: null,
    proposal: null,
    changeCandidates: []
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
    updatedAt: mission.updatedAt || mission.createdAt || new Date().toISOString(),
    status: mission.status || 'ready',
    runs: Array.isArray(mission.runs) ? mission.runs.map(normalizeRunRecord).filter(Boolean) : [],
    browserSkillSnapshots: Array.isArray(mission.browserSkillSnapshots)
      ? mission.browserSkillSnapshots.map(normalizeBrowserSkillSnapshot).filter(Boolean)
      : [],
    workspace: normalizeMissionWorkspaceSummary(mission.workspace),
    missionContext: normalizeMissionContext(mission.missionContext),
    plan: normalizeMissionPlan(mission.plan),
    proposal: normalizeMissionProposal(mission.proposal),
    changeCandidates: normalizeChangeCandidates(mission.changeCandidates)
  };
}

function normalizeMissionWorkspaceSummary(workspace) {
  if (!workspace || typeof workspace !== 'object') {
    return null;
  }
  return {
    name: String(workspace.name || ''),
    pathLabel: String(workspace.pathLabel || ''),
    scannedAt: String(workspace.scannedAt || ''),
    status: String(workspace.status || ''),
    source: String(workspace.source || ''),
    selectedAt: String(workspace.selectedAt || ''),
    stacks: normalizeWorkspaceTextList(workspace.stacks),
    files: Number.isFinite(workspace.files) ? workspace.files : 0,
    folders: Number.isFinite(workspace.folders) ? workspace.folders : 0,
    keyFiles: normalizeWorkspaceTextList(workspace.keyFiles).slice(0, 10),
    treeSummary: Array.isArray(workspace.treeSummary)
      ? workspace.treeSummary.map(normalizeWorkspaceTreeItem).filter(Boolean).slice(0, 12)
      : [],
    projectSignals: Array.isArray(workspace.projectSignals)
      ? workspace.projectSignals.map(normalizeWorkspaceSignal).filter(Boolean)
      : []
  };
}

function normalizeMissionContext(context) {
  if (!context || typeof context !== 'object') {
    return null;
  }
  return {
    missionId: String(context.missionId || ''),
    missionTitle: String(context.missionTitle || ''),
    workspaceName: String(context.workspaceName || ''),
    workspaceSource: String(context.workspaceSource || ''),
    workspaceSelectedAt: String(context.workspaceSelectedAt || ''),
    workspaceLastReadAt: String(context.workspaceLastReadAt || ''),
    stacks: normalizeWorkspaceTextList(context.stacks),
    keyFiles: normalizeWorkspaceTextList(context.keyFiles).slice(0, 10),
    treeSummary: Array.isArray(context.treeSummary)
      ? context.treeSummary.map(normalizeWorkspaceTreeItem).filter(Boolean).slice(0, 12)
      : [],
    projectSignals: Array.isArray(context.projectSignals)
      ? context.projectSignals.map(normalizeWorkspaceSignal).filter(Boolean)
      : [],
    readOnly: context.readOnly !== false,
    commandsExecuted: context.commandsExecuted === true,
    filesModified: context.filesModified === true
  };
}

function normalizeMissionPlan(plan) {
  if (!plan || typeof plan !== 'object') {
    return null;
  }
  const tasks = Array.isArray(plan.tasks)
    ? plan.tasks.map(normalizeMissionPlanTask).filter(Boolean).slice(0, MISSION_PLAN_TASK_RANGE.max)
    : [];
  if (!tasks.length) {
    return null;
  }
  return {
    id: String(plan.id || `plan-${Date.now().toString(36)}`),
    generatedAt: String(plan.generatedAt || new Date().toISOString()),
    source: String(plan.source || 'local-deterministic'),
    summary: String(plan.summary || 'Plan inicial local'),
    contextUsed: normalizeWorkspaceTextList(plan.contextUsed),
    readOnly: plan.readOnly !== false,
    commandsExecuted: plan.commandsExecuted === true,
    filesModified: plan.filesModified === true,
    tasks
  };
}

function normalizeMissionPlanTask(task) {
  if (!task || typeof task !== 'object') {
    return null;
  }
  return {
    id: String(task.id || `task-${Date.now().toString(36)}`),
    title: String(task.title || 'Paso local'),
    status: String(task.status || 'Por hacer'),
    reason: String(task.reason || 'Paso sugerido por contexto local.'),
    source: String(task.source || 'local-deterministic'),
    dependsOn: normalizeWorkspaceTextList(task.dependsOn),
    evidenceRefs: normalizeWorkspaceTextList(task.evidenceRefs),
    workspaceSignals: normalizeWorkspaceTextList(task.workspaceSignals),
    readOnly: task.readOnly !== false
  };
}

function normalizeMissionProposal(proposal) {
  if (!proposal || typeof proposal !== 'object') {
    return null;
  }
  const tasks = Array.isArray(proposal.tasks)
    ? proposal.tasks.map(normalizeProposalTask).filter(Boolean).slice(0, MISSION_PLAN_TASK_RANGE.max)
    : [];
  if (!tasks.length) {
    return null;
  }
  const now = new Date().toISOString();
  return {
    proposalId: String(proposal.proposalId || `proposal-${Date.now().toString(36)}`),
    missionId: String(proposal.missionId || ''),
    missionTitle: String(proposal.missionTitle || ''),
    workspaceName: String(proposal.workspaceName || ''),
    workspaceSource: String(proposal.workspaceSource || ''),
    createdAt: String(proposal.createdAt || now),
    updatedAt: String(proposal.updatedAt || proposal.createdAt || now),
    status: normalizeProposalStatus(proposal.status),
    summary: String(proposal.summary || 'Propuesta local para revisar.'),
    contextUsed: normalizeWorkspaceTextList(proposal.contextUsed).slice(0, 12),
    tasks,
    suggestedReviewOrder: normalizeWorkspaceTextList(proposal.suggestedReviewOrder).slice(0, MISSION_PLAN_TASK_RANGE.max),
    relevantAreas: normalizeWorkspaceTextList(proposal.relevantAreas).filter((item) => !isWorkspaceProtectedPath(item)).slice(0, 10),
    assumptions: normalizeWorkspaceTextList(proposal.assumptions).slice(0, 6),
    risks: normalizeWorkspaceTextList(proposal.risks).slice(0, 6),
    evidence: normalizeWorkspaceTextList(proposal.evidence).slice(0, 8),
    nextHumanDecision: String(proposal.nextHumanDecision || 'Revisar la propuesta y decidir el próximo paso.'),
    readOnly: proposal.readOnly !== false,
    commandsExecuted: proposal.commandsExecuted === true,
    filesModified: proposal.filesModified === true,
    diffGenerated: proposal.diffGenerated === true,
    executionReady: proposal.executionReady === true
  };
}

function normalizeProposalTask(task) {
  if (!task || typeof task !== 'object') {
    return null;
  }
  return {
    id: String(task.id || `proposal-task-${Date.now().toString(36)}`),
    title: String(task.title || 'Tarea a revisar'),
    reason: String(task.reason || 'Derivada del plan inicial local.'),
    status: String(task.status || 'Por hacer'),
    sourceTaskId: String(task.sourceTaskId || ''),
    needsHumanReview: task.needsHumanReview !== false,
    readOnly: task.readOnly !== false
  };
}

function normalizeProposalStatus(status) {
  const value = String(status || PROPOSAL_STATUS.draft).toLowerCase();
  if (value === PROPOSAL_STATUS.ready.toLowerCase() || value === 'ready') return PROPOSAL_STATUS.ready;
  if (value === PROPOSAL_STATUS.needsContext.toLowerCase() || value === 'needs-context') return PROPOSAL_STATUS.needsContext;
  if (value === PROPOSAL_STATUS.reviewed.toLowerCase() || value === 'reviewed') return PROPOSAL_STATUS.reviewed;
  return PROPOSAL_STATUS.draft;
}

function normalizeChangeCandidates(candidates) {
  return Array.isArray(candidates)
    ? candidates.map(normalizeChangeCandidate).filter(Boolean).slice(0, CHANGE_CANDIDATE_LIMITS.max)
    : [];
}

function normalizeChangeCandidate(candidate) {
  if (!candidate || typeof candidate !== 'object') {
    return null;
  }
  return {
    candidateId: String(candidate.candidateId || `candidate-${Date.now().toString(36)}`),
    proposalId: String(candidate.proposalId || ''),
    missionId: String(candidate.missionId || ''),
    sourceTaskId: String(candidate.sourceTaskId || ''),
    title: String(candidate.title || 'Cambio candidato'),
    area: sanitizeCandidateTarget(candidate.area || 'Área por revisar'),
    likelyTarget: sanitizeCandidateTarget(candidate.likelyTarget || 'sin objetivo específico'),
    targetKind: normalizeCandidateTargetKind(candidate.targetKind),
    intent: String(candidate.intent || 'Preparar revisión futura.'),
    reason: String(candidate.reason || 'Derivado de propuesta y TaskGraph local.'),
    evidenceRefs: normalizeWorkspaceTextList(candidate.evidenceRefs).slice(0, 8),
    workspaceSignals: normalizeWorkspaceTextList(candidate.workspaceSignals).slice(0, 8),
    riskLevel: normalizeCandidateRisk(candidate.riskLevel),
    humanReviewNeeded: candidate.humanReviewNeeded !== false,
    status: normalizeCandidateStatus(candidate.status),
    readOnly: candidate.readOnly !== false,
    diffGenerated: candidate.diffGenerated === true,
    patchGenerated: candidate.patchGenerated === true,
    commandsExecuted: candidate.commandsExecuted === true,
    filesModified: candidate.filesModified === true,
    executionReady: candidate.executionReady === true
  };
}

function normalizeCandidateStatus(status) {
  const value = String(status || CHANGE_CANDIDATE_STATUS.candidate).toLowerCase();
  if (value === CHANGE_CANDIDATE_STATUS.reviewing.toLowerCase() || value === 'reviewing') return CHANGE_CANDIDATE_STATUS.reviewing;
  if (value === CHANGE_CANDIDATE_STATUS.reviewed.toLowerCase() || value === 'reviewed') return CHANGE_CANDIDATE_STATUS.reviewed;
  if (value === CHANGE_CANDIDATE_STATUS.needsContext.toLowerCase() || value === 'needs-context') return CHANGE_CANDIDATE_STATUS.needsContext;
  if (value === CHANGE_CANDIDATE_STATUS.discarded.toLowerCase() || value === 'discarded') return CHANGE_CANDIDATE_STATUS.discarded;
  return CHANGE_CANDIDATE_STATUS.candidate;
}

function normalizeCandidateTargetKind(kind) {
  const value = String(kind || '').toLowerCase();
  if (['archivo probable', 'carpeta probable', 'documentación', 'test/validación', 'configuración', 'ui', 'unknown'].includes(value)) {
    return value;
  }
  return 'unknown';
}

function normalizeCandidateRisk(risk) {
  const value = String(risk || 'bajo').toLowerCase();
  return ['bajo', 'medio', 'alto'].includes(value) ? value : 'bajo';
}

function sanitizeCandidateTarget(value) {
  const target = String(value || '').trim();
  if (!target || isWorkspaceProtectedPath(target)) {
    return 'sin objetivo específico';
  }
  return target;
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
    note: String(run.note || run.title || ''),
    durationMs: Number.isFinite(run.durationMs) ? run.durationMs : 640,
    commandKind: run.commandKind || 'SafeNoOp',
    result: run.result || 'Completed with no side effects',
    evidenceRef: run.evidenceRef || `evidence:demo:${run.id || 'run'}`,
    timeline: Array.isArray(run.timeline) ? run.timeline : [],
    logs: Array.isArray(run.logs) ? run.logs : [],
    summary: run.summary || 'Run demo no-op completado.',
    missionContext: normalizeMissionContext(run.missionContext),
    plan: normalizeMissionPlan(run.plan),
    proposal: normalizeMissionProposal(run.proposal),
    changeCandidates: normalizeChangeCandidates(run.changeCandidates)
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
  mission.workspace = workspaceMissionSummary();
  mission.missionContext = buildMissionWorkspaceContext(mission);
  mission.plan = generateMissionPlanSkeleton(mission);
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

function beginEditMission() {
  const mission = activeDemoMission();
  if (!mission) {
    return;
  }
  state.demo.editingMission = true;
  el.editMissionTitleInput.value = mission.title;
  el.editMissionDescriptionInput.value = mission.description || '';
  render();
  el.editMissionTitleInput.focus();
}

function saveMissionEdit() {
  const mission = activeDemoMission();
  if (!mission) {
    return;
  }
  const title = el.editMissionTitleInput.value.trim();
  if (!title) {
    el.editMissionTitleInput.focus();
    return;
  }
  mission.title = title;
  mission.description = el.editMissionDescriptionInput.value.trim() || 'Demo local no-op para probar Mission Control.';
  mission.workspace = workspaceMissionSummary() || mission.workspace;
  mission.missionContext = buildMissionWorkspaceContext(mission) || mission.missionContext;
  mission.plan = generateMissionPlanSkeleton(mission);
  mission.proposal = null;
  mission.changeCandidates = [];
  mission.changeCandidates = [];
  mission.updatedAt = new Date().toISOString();
  state.demo.editingMission = false;
  syncDemoViewFromStore();
  saveDemoStore();
  addLog('local', { kind: 'MissionEdited', missionId: mission.id, title: mission.title });
  render();
}

function cancelMissionEdit() {
  state.demo.editingMission = false;
  render();
}

function deleteActiveMission() {
  const mission = activeDemoMission();
  if (!mission) {
    return;
  }
  if (!confirm('Borrar misión demo y sus runs locales?')) {
    return;
  }
  state.demo.missions = state.demo.missions.filter((item) => item.id !== mission.id);
  if (!state.demo.missions.length) {
    state.demo = createDemoSeed();
  } else {
    state.demo.activeMissionId = state.demo.missions[0].id;
    state.demo.selectedRunId = state.demo.missions[0].runs[0] ? state.demo.missions[0].runs[0].id : '';
    state.demo.editingMission = false;
  }
  syncDemoViewFromStore();
  saveDemoStore();
  addLog('local', { kind: 'MissionDeleted', missionId: mission.id, title: mission.title });
  render();
}

function selectDemoMission(missionId) {
  if (!state.demo.missions.some((mission) => mission.id === missionId)) {
    return;
  }
  state.demo.activeMissionId = missionId;
  const mission = activeDemoMission();
  state.demo.selectedRunId = mission && mission.runs[0] ? mission.runs[0].id : '';
  state.demo.editingMission = false;
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

function saveRunNote() {
  const run = selectedDemoRun();
  const mission = activeDemoMission();
  if (!run || !mission) {
    return;
  }
  run.note = el.runNoteInput.value.trim();
  run.summary = run.note ? `${run.note} · ${mission.title}` : `${mission.title}: run demo no-op completado.`;
  syncDemoViewFromStore();
  saveDemoStore();
  addLog('local', { kind: 'RunNoteSaved', runId: run.id, note: run.note || 'empty' });
  render();
}

function clearRunNote() {
  const run = selectedDemoRun();
  const mission = activeDemoMission();
  if (!run || !mission) {
    return;
  }
  run.note = '';
  run.summary = `${mission.title}: run demo no-op completado.`;
  syncDemoViewFromStore();
  saveDemoStore();
  addLog('local', { kind: 'RunNoteCleared', runId: run.id });
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
  mission.missionContext = buildMissionWorkspaceContext(mission) || mission.missionContext;
  const plan = ensureMissionPlan(mission);
  const proposal = mission.proposal || null;
  const changeCandidates = normalizeChangeCandidates(mission.changeCandidates);
  const now = new Date();
  const iso = now.toISOString();
  const runId = `demo-${now.getTime().toString(36)}`;
  const evidenceRef = `evidence:demo:${runId}`;
  const timeline = [
    demoTimelineStep('Run iniciado', `Run ${runId} arrancó en modo demo local.`, 'running', 'RunVisible', evidenceRef),
    demoTimelineStep('No-op aceptado', 'La acción demo fue aceptada sin shell, filesystem ni cloud.', 'accepted', 'SafeNoOp', evidenceRef),
    demoTimelineStep('Evidencia lista', 'Se generó un resumen demo en memoria para el panel visible.', 'evidence', 'EvidenceProjection', evidenceRef),
    demoTimelineStep('Run completado', 'Timeline, historial y logs quedaron actualizados para la grabación.', 'completed', 'Result', evidenceRef)
  ];
  if (state.workspace && state.workspace.name) {
    timeline.splice(1, 0, demoTimelineStep(
      'Workspace en contexto',
      `${state.workspace.name} leído en modo solo lectura para orientar la misión.`,
      'evidence-ready',
      'WorkspaceContext',
      `workspace:${state.workspace.name}`));
  }
  if (plan && plan.tasks.length) {
    timeline.splice(timeline.length - 1, 0, demoTimelineStep(
      'Plan inicial listo',
      `${plan.tasks.length} tareas locales quedaron disponibles para revisar.`,
      'evidence-ready',
      'TaskGraph',
      `plan:${plan.id}`));
  }
  if (proposal) {
    timeline.splice(timeline.length - 1, 0, demoTimelineStep(
      'Propuesta revisable incluida',
      `${proposal.status}: ${proposal.summary}`,
      'evidence-ready',
      'ProposalDraft',
      `proposal:${proposal.proposalId}`));
  }
  if (changeCandidates.length) {
    timeline.splice(timeline.length - 1, 0, demoTimelineStep(
      'Cambios candidatos incluidos',
      `${changeCandidates.length} candidatos quedaron listos para revisión humana futura.`,
      'evidence-ready',
      'ChangeCandidatePreview',
      `candidates:${runId}`));
  }
  const logs = [
    { label: 'run id', value: runId },
    { label: 'mission', value: mission.title },
    { label: 'workspace', value: state.workspace && state.workspace.name ? state.workspace.name : 'sin workspace' },
    { label: 'plan tasks', value: plan ? String(plan.tasks.length) : '0' },
    { label: 'proposal', value: proposal ? proposal.status : 'sin propuesta' },
    { label: 'proposal tasks', value: proposal ? String(proposal.tasks.length) : '0' },
    { label: 'change candidates', value: String(changeCandidates.length) },
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
    note: '',
    durationMs: 640,
    commandKind: 'SafeNoOp',
    result: 'Completed with no side effects',
    evidenceRef,
    timeline,
    logs,
    summary: `${mission.title}: run demo no-op completado.`,
    missionContext: mission.missionContext || null,
    plan: plan || null,
    proposal: proposal || null,
    changeCandidates
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
  const workspace = state.workspace || createEmptyWorkspaceStore();
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
  el.missionWorkspaceContext.textContent = workspace.name
    ? `Workspace activo: ${workspace.name} · ${(workspace.stacks || []).slice(0, 3).join(' + ') || 'Proyecto local'}`
    : 'Sin workspace activo todavía.';
  renderDemoGuidance();
  renderMissionEditor();
  renderDemoMissionList();
  renderDemoRunHistory();
  renderDemoScript();
  renderRunNoteEditor();
  renderMissionPlan();
  renderMissionProposal();
  renderChangeCandidates();
  renderTimeline(el.demoTimeline, demo.timeline);
  el.demoEvidencePanel.innerHTML = demo.logs.map((item) => `
    <div class="demo-log-item">
      <span>${safeHtml(item.label)}</span>
      <strong>${safeHtml(item.value)}</strong>
    </div>`).join('');
  el.demoTechnicalReport.textContent = demo.report || buildDemoTechnicalReport();
}

function renderMissionPlan() {
  const mission = activeDemoMission();
  const plan = mission ? mission.plan : null;
  const context = mission ? mission.missionContext : null;
  el.regenerateMissionPlanBtn.disabled = !mission;
  el.copyMissionPlanBtn.disabled = !mission || !plan;
  el.clearMissionPlanBtn.disabled = !mission || !plan;
  el.missionPlanContext.textContent = mission
    ? planContextLabel(mission, context, plan)
    : 'El plan aparece cuando hay una misión activa.';
  el.missionPlanChips.innerHTML = missionPlanChips(mission, context, plan)
    .map((chip) => `<span>${safeHtml(chip)}</span>`)
    .join('');
  const tasks = plan && Array.isArray(plan.tasks) ? plan.tasks : [];
  if (!tasks.length) {
    el.missionTaskGraph.innerHTML = '<li class="mission-task-item"><strong>Sin plan inicial</strong><p>Usá Regenerar plan local para crear tareas revisables.</p></li>';
    return;
  }
  el.missionTaskGraph.innerHTML = tasks.map((task, index) => `
    <li class="mission-task-item">
      <div class="mission-task-meta">
        <span>${index + 1}</span>
        <span>${safeHtml(task.status)}</span>
        <span>${safeHtml(missionPlanSourceLabel(task.source))}</span>
        ${task.dependsOn.length ? `<span>${task.dependsOn.length === 1 ? 'Depende de paso previo' : `Depende de ${task.dependsOn.length} pasos previos`}</span>` : ''}
      </div>
      <strong>${safeHtml(task.title)}</strong>
      <p>${safeHtml(task.reason)}</p>
    </li>`).join('');
}

function missionPlanSourceLabel(source) {
  return source === 'local-deterministic' ? 'Plan local' : source || 'Plan local';
}

function planContextLabel(mission, context, plan) {
  if (!mission) {
    return 'El plan aparece cuando hay una misión activa.';
  }
  if (context && context.workspaceName) {
    return `${mission.title} usa ${context.workspaceName} como contexto local de solo lectura.`;
  }
  if (plan) {
    return `${mission.title} tiene un plan inicial local sin workspace activo.`;
  }
  return `${mission.title} todavía no tiene plan inicial.`;
}

function missionPlanChips(mission, context, plan) {
  const chips = [];
  if (mission) chips.push('Misión activa');
  if (context && context.workspaceName) {
    chips.push(`Workspace: ${context.workspaceName}`);
    if (context.workspaceSource) chips.push(`Origen: ${workspaceSourceLabel(context.workspaceSource) || context.workspaceSource}`);
    for (const stack of (context.stacks || []).slice(0, 3)) chips.push(stack);
  } else {
    chips.push('Sin workspace');
  }
  if (plan) {
    chips.push(`${plan.tasks.length} tareas`);
    chips.push('Solo lectura');
  }
  return chips;
}

function regenerateMissionPlan() {
  const mission = activeDemoMission();
  if (!mission) {
    return;
  }
  mission.workspace = workspaceMissionSummary() || mission.workspace;
  mission.missionContext = buildMissionWorkspaceContext(mission) || mission.missionContext;
  mission.plan = generateMissionPlanSkeleton(mission);
  mission.proposal = null;
  mission.updatedAt = new Date().toISOString();
  syncDemoViewFromStore();
  saveDemoStore();
  addLog('local', { kind: 'MissionPlanGenerated', missionId: mission.id, tasks: mission.plan.tasks.length });
  render();
}

function clearMissionPlan() {
  const mission = activeDemoMission();
  if (!mission || !mission.plan) {
    return;
  }
  mission.plan = null;
  mission.proposal = null;
  mission.changeCandidates = [];
  mission.updatedAt = new Date().toISOString();
  syncDemoViewFromStore();
  saveDemoStore();
  addLog('local', { kind: 'MissionPlanCleared', missionId: mission.id });
  render();
}

async function copyMissionPlan() {
  const summary = buildMissionPlanSummary();
  try {
    await navigator.clipboard.writeText(summary);
    addLog('local', { kind: 'MissionPlanCopied', missionId: activeDemoMission() ? activeDemoMission().id : 'none' });
  } catch (error) {
    addLog('local', { kind: 'MissionPlanCopyFallback', reason: error && error.message ? error.message : 'clipboard unavailable' });
  }
  render();
}

function buildMissionPlanSummary(mission = activeDemoMission()) {
  if (!mission) {
    return 'NODAL OS — Plan inicial\nmission: none';
  }
  const plan = ensureMissionPlan(mission);
  const context = mission.missionContext || buildMissionWorkspaceContext(mission);
  return [
    'NODAL OS — Plan inicial',
    `mission: ${mission.title}`,
    `mission_id: ${mission.id}`,
    `workspace: ${context && context.workspaceName ? context.workspaceName : 'sin workspace'}`,
    `workspace_source: ${context && context.workspaceSource ? context.workspaceSource : 'sin origen'}`,
    `stacks: ${context && context.stacks.length ? context.stacks.join(' + ') : 'sin workspace'}`,
    `key_files: ${context && context.keyFiles.length ? context.keyFiles.join('; ') : 'sin archivos clave'}`,
    `plan_id: ${plan ? plan.id : 'none'}`,
    `tasks: ${plan ? plan.tasks.length : 0}`,
    `generated_at: ${plan ? plan.generatedAt : 'pending'}`,
    `read_only: ${plan ? plan.readOnly === true : true}`,
    `commands_executed: ${plan ? plan.commandsExecuted === true : false}`,
    `files_modified: ${plan ? plan.filesModified === true : false}`,
    ...(plan ? plan.tasks.map((task, index) => `${index + 1}. ${task.title} [${task.status}] - ${task.reason}`) : [])
  ].join('\n');
}

function renderMissionProposal() {
  const mission = activeDemoMission();
  const proposal = mission ? mission.proposal : null;
  el.generateProposalBtn.disabled = !mission;
  el.regenerateProposalBtn.disabled = !mission;
  el.copyProposalBtn.disabled = !mission || !proposal;
  el.reviewProposalBtn.disabled = !mission || !proposal || proposal.status === PROPOSAL_STATUS.reviewed;
  el.clearProposalBtn.disabled = !mission || !proposal;
  el.missionProposalContext.textContent = mission
    ? proposalContextLabel(mission, proposal)
    : 'Generá una propuesta revisable desde el plan inicial.';
  const flags = proposal
    ? proposalFlags(proposal)
    : ['Sin propuesta', 'Solo lectura', 'Sin diff', 'Sin ejecución'];
  el.missionProposalFlags.innerHTML = flags.map((flag) => `<span>${safeHtml(flag)}</span>`).join('');
  if (!proposal) {
    el.missionProposalSummary.innerHTML = '<strong>Sin propuesta todavía</strong><p>Usá Generar propuesta para convertir el TaskGraph en un borrador revisable.</p>';
    el.missionProposalDetails.innerHTML = '';
    return;
  }
  el.missionProposalSummary.innerHTML = `
    <strong>${safeHtml(proposal.status)}</strong>
    <p>${safeHtml(proposal.summary)}</p>`;
  el.missionProposalDetails.innerHTML = [
    proposalDetailBlock('Contexto usado', proposal.contextUsed),
    proposalDetailBlock('Tareas propuestas', proposal.tasks.map((task) => `${task.title}: ${task.reason}`)),
    proposalDetailBlock('Áreas relevantes', proposal.relevantAreas),
    proposalDetailBlock('Supuestos y riesgos', [...proposal.assumptions, ...proposal.risks]),
    proposalDetailBlock('Evidencia', proposal.evidence),
    proposalDetailBlock('Siguiente decisión', [proposal.nextHumanDecision])
  ].join('');
}

function proposalContextLabel(mission, proposal) {
  if (!proposal) {
    return `${mission.title} puede generar una propuesta local desde su plan inicial.`;
  }
  const workspaceLabel = proposal.workspaceName || 'sin workspace';
  return `${mission.title} tiene una propuesta ${proposal.status.toLowerCase()} usando ${workspaceLabel}.`;
}

function proposalFlags(proposal) {
  return [
    proposal.status,
    proposal.readOnly ? 'Solo lectura' : 'Lectura no confirmada',
    proposal.diffGenerated ? 'Diff generado' : 'Sin diff',
    proposal.commandsExecuted ? 'Con comandos' : 'Sin ejecución',
    proposal.filesModified ? 'Con modificaciones' : 'No se modificaron archivos'
  ];
}

function proposalDetailBlock(title, items) {
  const values = (Array.isArray(items) ? items : []).filter(Boolean).slice(0, 6);
  return `
    <article class="mission-proposal-detail">
      <span>${safeHtml(title)}</span>
      <ul>${(values.length ? values : ['Sin datos todavía']).map((item) => `<li>${safeHtml(item)}</li>`).join('')}</ul>
    </article>`;
}

function generateProposal() {
  const mission = activeDemoMission();
  if (!mission) {
    return;
  }
  mission.missionContext = buildMissionWorkspaceContext(mission) || mission.missionContext || null;
  mission.plan = ensureMissionPlan(mission);
  mission.proposal = generateMissionProposalDraft(mission);
  mission.changeCandidates = [];
  mission.updatedAt = new Date().toISOString();
  syncDemoViewFromStore();
  saveDemoStore();
  addLog('local', { kind: 'ProposalGenerated', missionId: mission.id, proposalId: mission.proposal.proposalId });
  render();
}

function regenerateProposal() {
  generateProposal();
}

async function copyProposal() {
  const summary = buildProposalSummary();
  try {
    await navigator.clipboard.writeText(summary);
    addLog('local', { kind: 'ProposalCopied', missionId: activeDemoMission() ? activeDemoMission().id : 'none' });
  } catch (error) {
    addLog('local', { kind: 'ProposalCopyFallback', reason: error && error.message ? error.message : 'clipboard unavailable' });
  }
  render();
}

function markProposalReviewed() {
  const mission = activeDemoMission();
  if (!mission || !mission.proposal) {
    return;
  }
  mission.proposal.status = PROPOSAL_STATUS.reviewed;
  mission.proposal.updatedAt = new Date().toISOString();
  mission.updatedAt = mission.proposal.updatedAt;
  syncDemoViewFromStore();
  saveDemoStore();
  addLog('local', { kind: 'ProposalReviewed', missionId: mission.id, proposalId: mission.proposal.proposalId });
  render();
}

function clearProposal() {
  const mission = activeDemoMission();
  if (!mission || !mission.proposal) {
    return;
  }
  mission.proposal = null;
  mission.changeCandidates = [];
  mission.updatedAt = new Date().toISOString();
  syncDemoViewFromStore();
  saveDemoStore();
  addLog('local', { kind: 'ProposalCleared', missionId: mission.id });
  render();
}

function buildProposalSummary(mission = activeDemoMission()) {
  if (!mission) {
    return 'NODAL OS — Propuesta\nmission: none';
  }
  const proposal = mission.proposal || generateMissionProposalDraft(mission);
  return [
    'NODAL OS — Propuesta revisable',
    `proposal_id: ${proposal.proposalId}`,
    `mission: ${proposal.missionTitle}`,
    `mission_id: ${proposal.missionId}`,
    `workspace: ${proposal.workspaceName || 'sin workspace'}`,
    `workspace_source: ${proposal.workspaceSource || 'sin origen'}`,
    `status: ${proposal.status}`,
    `summary: ${proposal.summary}`,
    `context_used: ${proposal.contextUsed.join(' + ') || 'misión local'}`,
    `relevant_areas: ${proposal.relevantAreas.join('; ') || 'sin áreas específicas'}`,
    `tasks: ${proposal.tasks.length}`,
    ...proposal.tasks.map((task, index) => `${index + 1}. ${task.title} - ${task.reason}`),
    `assumptions: ${proposal.assumptions.join('; ') || 'sin supuestos adicionales'}`,
    `risks: ${proposal.risks.join('; ') || 'sin riesgos adicionales'}`,
    `evidence: ${proposal.evidence.join('; ') || 'sin evidencia adicional'}`,
    `next_human_decision: ${proposal.nextHumanDecision}`,
    `read_only: ${proposal.readOnly === true}`,
    `diff_generated: ${proposal.diffGenerated === true}`,
    `commands_executed: ${proposal.commandsExecuted === true}`,
    `files_modified: ${proposal.filesModified === true}`,
    `execution_ready: ${proposal.executionReady === true}`
  ].join('\n');
}

function renderChangeCandidates() {
  const mission = activeDemoMission();
  const candidates = mission ? normalizeChangeCandidates(mission.changeCandidates) : [];
  el.generateCandidatesBtn.disabled = !mission;
  el.regenerateCandidatesBtn.disabled = !mission;
  el.copyCandidatesBtn.disabled = !mission || !candidates.length;
  el.reviewCandidatesBtn.disabled = !mission || !candidates.length || candidates.every((candidate) => candidate.status === CHANGE_CANDIDATE_STATUS.reviewed);
  el.clearCandidatesBtn.disabled = !mission || !candidates.length;
  el.changeCandidateContext.textContent = mission
    ? changeCandidateContextLabel(mission, candidates)
    : 'Convertí la propuesta en cambios candidatos para revisar, sin diff ni patch.';
  const flags = candidates.length
    ? changeCandidateFlags(candidates)
    : ['Sin candidatos', 'Solo lectura', 'Sin diff', 'Sin patch', 'Sin ejecución'];
  el.changeCandidateFlags.innerHTML = flags.map((flag) => `<span>${safeHtml(flag)}</span>`).join('');
  if (!candidates.length) {
    el.changeCandidateSummary.innerHTML = '<strong>Sin candidatos todavía</strong><p>Usá Generar candidatos para preparar una vista revisable, sin cambios en archivos.</p>';
    el.changeCandidateList.innerHTML = '';
    return;
  }
  el.changeCandidateSummary.innerHTML = `
    <strong>${candidates.length} cambios candidatos</strong>
    <p>Vista previa local para decidir qué revisar después. No se generó diff ni patch.</p>`;
  el.changeCandidateList.innerHTML = candidates.map((candidate, index) => `
    <article class="change-candidate-item">
      <div class="change-candidate-meta">
        <span>${index + 1}</span>
        <span>${safeHtml(candidate.status)}</span>
        <span>${safeHtml(candidate.targetKind)}</span>
        <span>Riesgo ${safeHtml(candidate.riskLevel)}</span>
      </div>
      <strong>${safeHtml(candidate.title)}</strong>
      <p><b>Área:</b> ${safeHtml(candidate.area)} · <b>Objetivo probable:</b> ${safeHtml(candidate.likelyTarget)}</p>
      <p>${safeHtml(candidate.reason)}</p>
      <p><b>Revisión humana:</b> ${candidate.humanReviewNeeded ? 'requerida antes de cualquier diff futuro' : 'no marcada'}</p>
    </article>`).join('');
}

function changeCandidateContextLabel(mission, candidates) {
  if (!candidates.length) {
    return `${mission.title} puede convertir su propuesta en candidatos read-only.`;
  }
  return `${mission.title} tiene ${candidates.length} candidatos para revisión futura.`;
}

function changeCandidateFlags(candidates) {
  return [
    `${candidates.length} candidatos`,
    'Solo lectura',
    candidates.some((candidate) => candidate.diffGenerated) ? 'Diff generado' : 'Sin diff',
    candidates.some((candidate) => candidate.patchGenerated) ? 'Patch generado' : 'Sin patch',
    candidates.some((candidate) => candidate.commandsExecuted) ? 'Con comandos' : 'Sin ejecución',
    candidates.some((candidate) => candidate.filesModified) ? 'Con modificaciones' : 'No se modificaron archivos'
  ];
}

function generateChangeCandidates() {
  const mission = activeDemoMission();
  if (!mission) {
    return;
  }
  mission.missionContext = buildMissionWorkspaceContext(mission) || mission.missionContext || null;
  mission.plan = ensureMissionPlan(mission);
  if (!mission.proposal) {
    mission.proposal = generateMissionProposalDraft(mission);
  }
  mission.changeCandidates = generateReadOnlyChangeCandidates(mission);
  mission.updatedAt = new Date().toISOString();
  syncDemoViewFromStore();
  saveDemoStore();
  addLog('local', { kind: 'ChangeCandidatesGenerated', missionId: mission.id, candidates: mission.changeCandidates.length });
  render();
}

function regenerateChangeCandidates() {
  generateChangeCandidates();
}

async function copyChangeCandidates() {
  const summary = buildChangeCandidateSummary();
  try {
    await navigator.clipboard.writeText(summary);
    addLog('local', { kind: 'ChangeCandidatesCopied', missionId: activeDemoMission() ? activeDemoMission().id : 'none' });
  } catch (error) {
    addLog('local', { kind: 'ChangeCandidatesCopyFallback', reason: error && error.message ? error.message : 'clipboard unavailable' });
  }
  render();
}

function markChangeCandidatesReviewed() {
  const mission = activeDemoMission();
  if (!mission || !mission.changeCandidates || !mission.changeCandidates.length) {
    return;
  }
  mission.changeCandidates = mission.changeCandidates.map((candidate) => normalizeChangeCandidate({
    ...candidate,
    status: CHANGE_CANDIDATE_STATUS.reviewed
  })).filter(Boolean);
  mission.updatedAt = new Date().toISOString();
  syncDemoViewFromStore();
  saveDemoStore();
  addLog('local', { kind: 'ChangeCandidatesReviewed', missionId: mission.id, candidates: mission.changeCandidates.length });
  render();
}

function clearChangeCandidates() {
  const mission = activeDemoMission();
  if (!mission || !mission.changeCandidates || !mission.changeCandidates.length) {
    return;
  }
  mission.changeCandidates = [];
  mission.updatedAt = new Date().toISOString();
  syncDemoViewFromStore();
  saveDemoStore();
  addLog('local', { kind: 'ChangeCandidatesCleared', missionId: mission.id });
  render();
}

function buildChangeCandidateSummary(mission = activeDemoMission()) {
  if (!mission) {
    return 'NODAL OS — Cambios candidatos\nmission: none';
  }
  const candidates = mission.changeCandidates && mission.changeCandidates.length
    ? normalizeChangeCandidates(mission.changeCandidates)
    : generateReadOnlyChangeCandidates(mission);
  const proposal = mission.proposal || generateMissionProposalDraft(mission);
  const context = mission.missionContext || buildMissionWorkspaceContext(mission);
  return [
    'NODAL OS — Cambios candidatos',
    `mission: ${mission.title}`,
    `mission_id: ${mission.id}`,
    `workspace: ${context && context.workspaceName ? context.workspaceName : 'sin workspace'}`,
    `workspace_source: ${context && context.workspaceSource ? context.workspaceSource : 'sin origen'}`,
    `proposal_id: ${proposal ? proposal.proposalId : 'sin propuesta'}`,
    `candidate_count: ${candidates.length}`,
    ...candidates.map((candidate, index) => [
      `${index + 1}. ${candidate.title}`,
      `   area: ${candidate.area}`,
      `   likely_target: ${candidate.likelyTarget}`,
      `   target_kind: ${candidate.targetKind}`,
      `   reason: ${candidate.reason}`,
      `   risk: ${candidate.riskLevel}`,
      `   human_review_needed: ${candidate.humanReviewNeeded}`,
      `   read_only: ${candidate.readOnly === true}`,
      `   diff_generated: ${candidate.diffGenerated === true}`,
      `   patch_generated: ${candidate.patchGenerated === true}`,
      `   commands_executed: ${candidate.commandsExecuted === true}`,
      `   files_modified: ${candidate.filesModified === true}`
    ].join('\n')),
    'next_human_review: Revisar candidatos y decidir si el próximo bloque prepara un diff preview también read-only.'
  ].join('\n');
}

function renderWorkspaceUnderstanding() {
  const workspace = state.workspace || createEmptyWorkspaceStore();
  const counts = workspace.counts || { files: 0, folders: 0, ignoredFolders: 0, listedItems: 0 };
  el.workspaceStatus.textContent = workspaceStatusLabel(workspace);
  el.workspaceName.textContent = workspace.name || 'Seleccioná una carpeta';
  el.workspacePath.textContent = workspace.pathLabel || 'No seleccionada';
  el.workspaceReadMode.textContent = workspace.source
    ? `Solo lectura · ${workspaceSourceLabel(workspace.source)}`
    : 'Solo lectura';
  el.workspaceCounts.textContent = `${counts.files} archivos · ${counts.folders} carpetas`;
  el.workspaceStack.textContent = (workspace.stacks || []).slice(0, 3).join(' + ') || 'sin lectura';
  el.rescanWorkspaceBtn.disabled = !workspace.name && !isWorkspacePickerAvailable();
  el.copyWorkspaceEvidenceBtn.disabled = !workspace.name && !workspace.evidence && workspace.status !== 'BLOCKED_BY_CURRENT_BROWSER_CAPABILITIES';
  el.clearWorkspaceBtn.disabled = !workspace.name && workspace.status !== 'BLOCKED_BY_CURRENT_BROWSER_CAPABILITIES';
  renderWorkspaceSignals(workspace);
  renderWorkspaceKeyFiles(workspace);
  renderWorkspaceTree(workspace);
  renderWorkspaceEvidence(workspace);
}

function workspaceStatusLabel(workspace) {
  if (!workspace || workspace.status === 'empty') {
    return 'Sin workspace';
  }
  if (workspace.status === 'reading') {
    return 'Leyendo...';
  }
  if (workspace.status === 'ready') {
    return 'Leído';
  }
  if (workspace.status === 'metadata') {
    return 'Resumen guardado';
  }
  if (workspace.status === 'BLOCKED_BY_CURRENT_BROWSER_CAPABILITIES') {
    return 'No disponible';
  }
  return workspace.statusMessage || workspace.status || 'Workspace';
}

function workspaceSourceLabel(source) {
  if (source === 'directory-picker') {
    return 'acceso de carpeta';
  }
  if (source === 'file-directory-input') {
    return 'modo compatible';
  }
  return '';
}

function renderWorkspaceSignals(workspace) {
  const signals = Array.isArray(workspace.signals) ? workspace.signals : [];
  if (!signals.length) {
    el.workspaceSignals.innerHTML = '<p class="workspace-empty-state">Abrí un workspace para ver tipo de proyecto, tests, scripts y señales principales.</p>';
    return;
  }
  el.workspaceSignals.innerHTML = signals.map((item) => `
    <div class="workspace-signal">
      <span>${safeHtml(item.label)}</span>
      <strong>${safeHtml(item.value)}</strong>
    </div>`).join('');
}

function renderWorkspaceKeyFiles(workspace) {
  const keyFiles = Array.isArray(workspace.keyFiles) ? workspace.keyFiles : [];
  if (!keyFiles.length) {
    el.workspaceKeyFiles.innerHTML = '<p class="workspace-empty-state">Los archivos clave aparecen después de la lectura read-only.</p>';
    return;
  }
  el.workspaceKeyFiles.innerHTML = keyFiles.slice(0, 24).map((path) => `
    <div class="workspace-key-file">
      <span>${safeHtml(workspaceKeyLabel(path))}</span>
      <strong>${safeHtml(path)}</strong>
    </div>`).join('');
}

function renderWorkspaceTree(workspace) {
  const treeItems = Array.isArray(workspace.treeItems) ? workspace.treeItems : [];
  if (!treeItems.length) {
    el.workspaceTree.innerHTML = '<p class="workspace-empty-state">El árbol resumido se arma con límites de profundidad y carpetas pesadas ignoradas.</p>';
    return;
  }
  el.workspaceTree.innerHTML = treeItems.slice(0, WORKSPACE_SCAN_LIMITS.maxTreeItems).map((item) => `
    <div class="workspace-tree-item${item.important ? ' important' : ''}">
      <span>${item.kind === 'directory' ? 'carpeta' : 'archivo'} · nivel ${item.depth}</span>
      <strong>${safeHtml(item.path)}</strong>
    </div>`).join('');
}

function renderWorkspaceEvidence(workspace) {
  if (workspace.status === 'BLOCKED_BY_CURRENT_BROWSER_CAPABILITIES') {
    el.workspaceEvidence.innerHTML = `
      <span><strong>Bloqueado:</strong> ${safeHtml(workspace.error || 'selector no disponible')}</span>
      <span>No se leyó estructura.</span>`;
    return;
  }
  if (!workspace.evidence) {
    el.workspaceEvidence.innerHTML = '<span><strong>Workspace:</strong> pendiente de selección.</span><span>No se ejecutaron comandos.</span><span>No se modificaron archivos.</span>';
    return;
  }
  const evidence = workspace.evidence;
  el.workspaceEvidence.innerHTML = `
    <span><strong>${safeHtml(evidence.summary || 'Workspace leído')}</strong></span>
    <span>${safeHtml(formatDemoDate(evidence.timestamp))}</span>
    <span>${safeHtml(evidence.files)} archivos / ${safeHtml(evidence.folders)} carpetas</span>
    <span>${safeHtml((evidence.stacks || []).join(' + ') || 'Proyecto local')}</span>
    <span>No se ejecutaron comandos.</span>
    <span>No se modificaron archivos.</span>`;
}

async function copyWorkspaceEvidence() {
  const workspace = state.workspace || createEmptyWorkspaceStore();
  if (!workspace.name && !workspace.evidence && workspace.status !== 'BLOCKED_BY_CURRENT_BROWSER_CAPABILITIES') {
    addLog('local', { kind: 'WorkspaceEvidenceCopySkipped', reason: 'no workspace' });
    render();
    return;
  }
  const summary = buildWorkspaceEvidenceSummary(workspace);
  try {
    await navigator.clipboard.writeText(summary);
    addLog('local', { kind: 'WorkspaceEvidenceCopied', workspace: workspace.name || 'none' });
  } catch (error) {
    addLog('local', { kind: 'WorkspaceEvidenceCopyFallback', reason: error && error.message ? error.message : 'clipboard unavailable' });
  }
  render();
}

function buildWorkspaceEvidenceSummary(workspace) {
  const current = workspace || createEmptyWorkspaceStore();
  const context = current.context || createWorkspaceContextContract(current);
  const counts = current.counts || { files: 0, folders: 0, ignoredFolders: 0 };
  return [
    'NODAL OS — Workspace',
    `workspace: ${current.name || 'sin workspace'}`,
    `status: ${workspaceStatusLabel(current)}`,
    `selected_at: ${context.selectedAt || current.openedAt || 'sin fecha'}`,
    `last_read_at: ${context.lastReadAt || current.scannedAt || 'sin lectura'}`,
    `source: ${context.source || current.source || 'sin origen'}`,
    `files: ${counts.files}`,
    `folders: ${counts.folders}`,
    `ignored_folders: ${counts.ignoredFolders}`,
    `stacks: ${(current.stacks || []).join(' + ') || 'sin lectura'}`,
    `key_files: ${(current.keyFiles || []).slice(0, 12).join('; ') || 'sin archivos clave'}`,
    `tree_summary: ${(current.treeItems || []).slice(0, 12).map((item) => item.path).join('; ') || 'sin árbol'}`,
    `evidence: ${current.evidence && current.evidence.summary ? current.evidence.summary : current.error || 'pendiente'}`,
    `read_only: ${context.readOnly === true}`,
    `commands_executed: ${context.commandsExecuted === true}`,
    `files_modified: ${context.filesModified === true}`
  ].join('\n');
}

function workspaceKeyLabel(path) {
  const lower = String(path || '').toLowerCase();
  if (lower.endsWith('.sln') || lower.endsWith('.slnx')) return 'solución .NET';
  if (lower.endsWith('.csproj')) return 'proyecto C#';
  if (lower.endsWith('package.json')) return 'Node package';
  if (lower.endsWith('manifest.json')) return 'manifest';
  if (lower.includes('browser-extension')) return 'extensión';
  if (lower.startsWith('src') || lower.includes('/src/')) return 'código fuente';
  if (lower.startsWith('tests') || lower.includes('/tests/')) return 'tests';
  if (lower.startsWith('scripts') || lower.includes('/scripts/')) return 'scripts';
  if (lower.startsWith('docs') || lower.includes('/docs/')) return 'docs';
  if (lower.includes('docker')) return 'docker';
  return 'archivo clave';
}

function renderDemoGuidance() {
  const collapsed = loadDemoGuidanceCollapsed();
  const status = demoRecordingReadiness();
  el.demoGuidanceCard.classList.toggle('is-collapsed', collapsed);
  el.toggleGuidanceBtn.textContent = collapsed ? 'Mostrar guía' : 'Minimizar';
  el.toggleGuidanceBtn.setAttribute('aria-expanded', String(!collapsed));
  setGuidedStep(el.guideStepMission, status.hasMission);
  setGuidedStep(el.guideStepRun, status.hasRun);
  setGuidedStep(el.guideStepCopy, status.canCopy);
  el.demoReadyCard.classList.toggle('is-ready', status.ready);
  el.demoReadyLabel.textContent = status.ready ? 'Lista para grabar' : 'Prepará un run para grabar';
  el.demoReadyText.textContent = status.ready
    ? 'Tenés misión, run, timeline y resumen copiable para una demo corta.'
    : 'La demo queda lista cuando hay misión, run, timeline y resumen.';
}

function setGuidedStep(node, complete) {
  node.classList.toggle('is-complete', Boolean(complete));
  node.setAttribute('aria-current', complete ? 'step' : 'false');
}

function demoRecordingReadiness() {
  const mission = activeDemoMission();
  const run = selectedDemoRun();
  const timelineVisible = Array.isArray(state.demo.timeline) && state.demo.timeline.length > 0;
  const reportReady = Boolean(state.demo.report || state.demo.runId);
  return {
    hasMission: Boolean(mission),
    hasRun: Boolean(run),
    canCopy: Boolean(run && timelineVisible && reportReady),
    ready: Boolean(mission && run && timelineVisible && reportReady)
  };
}

function toggleDemoGuidance() {
  saveDemoGuidanceCollapsed(!loadDemoGuidanceCollapsed());
  renderDemoGuidance();
}

function renderDemoMissionList() {
  el.missionList.innerHTML = state.demo.missions.map((mission) => {
    const active = mission.id === state.demo.activeMissionId;
    const created = formatDemoDate(mission.createdAt);
    const runCount = Array.isArray(mission.runs) ? mission.runs.length : 0;
    return `
      <button class="mission-list-item${active ? ' active' : ''}" type="button" data-demo-mission-id="${safeHtml(mission.id)}">
        <strong>${safeHtml(mission.title)}</strong>
        <small>${safeHtml(mission.description || 'Demo local')}</small>
        <span class="mission-list-meta">
          <span>${safeHtml(created)}</span>
          <span>${runCount} ${runCount === 1 ? 'run' : 'runs'}</span>
          <span>${active ? 'activa' : 'ver'}</span>
        </span>
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
        <strong>${safeHtml(run.note || 'Run demo local')}</strong>
        <small>${safeHtml(run.summary || run.result || 'Run demo local')}</small>
        <span class="run-history-meta">
          <span>${safeHtml(formatDemoDate(run.completedAt || run.startedAt))}</span>
          <span>${safeHtml(formatDuration(run.durationMs))}</span>
          <span>${Array.isArray(run.timeline) ? run.timeline.length : 0} eventos</span>
          <span>${safeHtml(run.status || 'completed')}</span>
          <span>Ver</span>
        </span>
      </button>`;
  }).join('');
  el.demoRunHistory.querySelectorAll('[data-demo-run-id]').forEach((button) => {
    button.addEventListener('click', () => selectDemoRun(button.getAttribute('data-demo-run-id')));
  });
}

function renderMissionEditor() {
  const mission = activeDemoMission();
  const editing = Boolean(state.demo.editingMission && mission);
  el.editMissionBtn.textContent = editing ? 'Editando' : 'Editar';
  el.editMissionBtn.disabled = !mission;
  el.missionEditFields.classList.toggle('hidden', !editing);
  if (editing && document.activeElement !== el.editMissionTitleInput && document.activeElement !== el.editMissionDescriptionInput) {
    el.editMissionTitleInput.value = mission.title;
    el.editMissionDescriptionInput.value = mission.description || '';
  }
}

function renderRunNoteEditor() {
  const run = selectedDemoRun();
  el.runNoteState.textContent = run ? 'run seleccionado' : 'sin run';
  el.runNoteInput.disabled = !run;
  el.saveRunNoteBtn.disabled = !run;
  el.clearRunNoteBtn.disabled = !run;
  if (!run) {
    el.runNoteInput.value = '';
    el.runNoteInput.placeholder = 'Ejecuta un run demo para agregar nota.';
    return;
  }
  if (document.activeElement !== el.runNoteInput) {
    el.runNoteInput.value = run.note || '';
  }
  el.runNoteInput.placeholder = run.note ? 'Editar nota del run' : 'Demo para video, prueba cliente, run de validación visual';
}

function renderDemoScript() {
  el.demoScriptPanel.innerHTML = DEMO_SCRIPT_STEPS
    .map((step) => `<li>${safeHtml(step)}</li>`)
    .join('');
}

function demoHostStatus(connection = state.connection) {
  const host = connection.host || '127.0.0.1';
  const portValue = connection.port || '8787';
  const health = connection.health || 'untested';
  return `${host}:${portValue} · ${health}`;
}

function demoBridgeStatus(connection = state.connection) {
  if (connection.status === 'connected' || connection.status === 'running') {
    return 'Conectado';
  }
  if (connection.status === 'connecting') {
    return 'Conectando';
  }
  return 'Sin conectar';
}

function demoBrowserClaimStatus(runtime = state.runtime, operatorPage = state.operator.page) {
  const tab = runtime && runtime.debug && runtime.debug.tab;
  if (tab && tab.claimStatus) {
    return tab.claimStatus;
  }
  if (operatorPage) {
    return 'Página observada';
  }
  return 'No activo';
}

function buildDemoTechnicalReport() {
  syncDemoViewFromStore();
  return composeDemoTechnicalReport(state.demo, {
    connection: state.connection,
    runtime: state.runtime,
    operatorPage: state.operator.page
  });
}

function composeDemoTechnicalReport(store, context = {}) {
  const demo = store || state.demo;
  const mission = activeDemoMission(demo);
  const run = selectedDemoRun(demo);
  const connectionContext = context.connection || { host: '127.0.0.1', port: '8787', health: 'untested', status: 'disconnected' };
  const runtimeContext = context.runtime || null;
  const operatorPageContext = context.operatorPage || '';
  const workspace = state.workspace || createEmptyWorkspaceStore();
  const plan = run && run.plan ? run.plan : mission && mission.plan ? mission.plan : null;
  const proposal = run && run.proposal ? run.proposal : mission && mission.proposal ? mission.proposal : null;
  const changeCandidates = run && run.changeCandidates ? normalizeChangeCandidates(run.changeCandidates) : mission ? normalizeChangeCandidates(mission.changeCandidates) : [];
  const missionContext = run && run.missionContext ? run.missionContext : mission && mission.missionContext ? mission.missionContext : null;
  const lines = [
    'NODAL OS — Demo local',
    `mission: ${demo.missionName}`,
    `mission_id: ${mission ? mission.id : 'none'}`,
    `description: ${mission ? mission.description : 'pending'}`,
    `workspace: ${workspace.name || 'sin workspace'}`,
    `mission_context_workspace: ${missionContext && missionContext.workspaceName ? missionContext.workspaceName : 'sin workspace'}`,
    `workspace_source: ${missionContext && missionContext.workspaceSource ? missionContext.workspaceSource : workspace.source || 'sin origen'}`,
    `workspace_status: ${workspaceStatusLabel(workspace)}`,
    `workspace_stack: ${(workspace.stacks || []).join(' + ') || 'sin lectura'}`,
    `workspace_counts: ${workspace.counts ? workspace.counts.files : 0} files / ${workspace.counts ? workspace.counts.folders : 0} folders`,
    `plan_id: ${plan ? plan.id : 'sin plan'}`,
    `plan_tasks: ${plan ? plan.tasks.length : 0}`,
    `plan_summary: ${plan ? plan.summary : 'sin plan inicial'}`,
    `proposal_id: ${proposal ? proposal.proposalId : 'sin propuesta'}`,
    `proposal_status: ${proposal ? proposal.status : 'sin propuesta'}`,
    `proposal_summary: ${proposal ? proposal.summary : 'sin propuesta revisable'}`,
    `proposal_tasks: ${proposal ? proposal.tasks.length : 0}`,
    `proposal_read_only: ${proposal ? proposal.readOnly === true : true}`,
    `proposal_diff_generated: ${proposal ? proposal.diffGenerated === true : false}`,
    `proposal_commands_executed: ${proposal ? proposal.commandsExecuted === true : false}`,
    `proposal_files_modified: ${proposal ? proposal.filesModified === true : false}`,
    `change_candidates: ${changeCandidates.length}`,
    `candidate_read_only: ${changeCandidates.length ? changeCandidates.every((candidate) => candidate.readOnly === true) : true}`,
    `candidate_diff_generated: ${changeCandidates.some((candidate) => candidate.diffGenerated === true)}`,
    `candidate_patch_generated: ${changeCandidates.some((candidate) => candidate.patchGenerated === true)}`,
    `candidate_commands_executed: ${changeCandidates.some((candidate) => candidate.commandsExecuted === true)}`,
    `candidate_files_modified: ${changeCandidates.some((candidate) => candidate.filesModified === true)}`,
    `status: ${demo.statusLabel}`,
    `run_id: ${demo.runId || 'pending'}`,
    `run_note: ${run && run.note ? run.note : 'sin nota'}`,
    `started_at: ${run ? run.startedAt : 'pending'}`,
    `completed_at: ${run ? run.completedAt : 'pending'}`,
    `duration: ${run ? formatDuration(run.durationMs) : 'pending'}`,
    `command_kind: ${demo.commandKind}`,
    `result: ${demo.result}`,
    `evidence_ref: ${demo.evidenceRef}`,
    `timeline: ${(demo.timeline || []).map((step) => step.title || step).join(' -> ')}`,
    `taskgraph: ${plan ? plan.tasks.map((task) => `${task.title} [${task.status}]`).join(' -> ') : 'sin tareas'}`,
    `proposal_review_order: ${proposal ? proposal.suggestedReviewOrder.join(' -> ') : 'sin propuesta'}`,
    `candidate_preview: ${changeCandidates.length ? changeCandidates.map((candidate) => `${candidate.title} -> ${candidate.likelyTarget}`).join(' | ') : 'sin candidatos'}`,
    `logs: ${(demo.logs || []).map((item) => `${item.label}=${item.value}`).join('; ')}`,
    `host_status: ${demoHostStatus(connectionContext)}`,
    `bridge_status: ${demoBridgeStatus(connectionContext)}`,
    `browser_status: ${demoBrowserClaimStatus(runtimeContext, operatorPageContext)}`,
    'scope: no shell, no filesystem write, no provider/cloud call',
    'mode: demo local visible',
    'recording_flow: Mission Control -> Run demo -> Historial -> Copiar resumen'
  ];
  return lines.join('\n');
}

async function copyDemoScript() {
  const mission = activeDemoMission();
  const script = [
    'NODAL OS — Demo script',
    `Misión sugerida: ${mission ? mission.title : 'Nueva misión'}`,
    ...DEMO_SCRIPT_STEPS.map((step, index) => `${index + 1}. ${step}`)
  ].join('\n');
  try {
    await navigator.clipboard.writeText(script);
    addLog('local', { kind: 'DemoScriptCopied', missionId: mission ? mission.id : 'none' });
  } catch (error) {
    addLog('local', { kind: 'DemoScriptCopyFallback', reason: error && error.message ? error.message : 'clipboard unavailable' });
  }
  render();
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

function formatDuration(value) {
  const duration = Number.isFinite(value) ? value : 0;
  if (duration <= 0) {
    return 'instantáneo';
  }
  if (duration < 1000) {
    return `${duration} ms`;
  }
  return `${(duration / 1000).toFixed(1)} s`;
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

async function openWorkspaceDirectory() {
  if (!isWorkspacePickerAvailable()) {
    setWorkspaceBlocked('El selector de carpeta no está disponible en este contexto.');
    render();
    return;
  }

  state.workspace.status = 'reading';
  state.workspace.statusMessage = 'Leyendo estructura del proyecto...';
  state.workspace.error = '';
  renderWorkspaceUnderstanding();

  try {
    const directoryHandle = await window.showDirectoryPicker({ mode: 'read' });
    state.workspace = await scanWorkspaceDirectory(directoryHandle);
    state.workspace.directoryHandle = directoryHandle;
    attachWorkspaceToActiveMission();
    saveWorkspaceStore();
    saveDemoStore();
    addLog('local', {
      kind: 'WorkspaceRead',
      name: state.workspace.name,
      files: state.workspace.counts.files,
      folders: state.workspace.counts.folders,
      stacks: state.workspace.stacks.join(', ') || 'none'
    });
  } catch (error) {
    if (error && error.name === 'AbortError') {
      state.workspace.status = state.workspace.name ? 'metadata' : 'empty';
      state.workspace.statusMessage = 'Selección cancelada.';
    } else {
      setWorkspaceBlocked(toMessage(error));
    }
  }
  render();
}

function openWorkspaceDirectoryInput() {
  if (!el.workspaceDirectoryInput) {
    setWorkspaceBlocked('El selector compatible de carpeta no está disponible en este contexto.');
    render();
    return;
  }
  el.workspaceDirectoryInput.value = '';
  el.workspaceDirectoryInput.click();
}

async function handleWorkspaceDirectoryInput(event) {
  const files = Array.from(event && event.target && event.target.files ? event.target.files : []);
  if (!files.length) {
    state.workspace.status = state.workspace.name ? 'metadata' : 'empty';
    state.workspace.statusMessage = 'Selección cancelada.';
    render();
    return;
  }

  state.workspace.status = 'reading';
  state.workspace.statusMessage = 'Leyendo carpeta en modo compatible...';
  state.workspace.error = '';
  renderWorkspaceUnderstanding();

  try {
    state.workspace = await scanWorkspaceFileList(files);
    state.workspace.directoryHandle = null;
    attachWorkspaceToActiveMission();
    saveWorkspaceStore();
    saveDemoStore();
    addLog('local', {
      kind: 'WorkspaceReadCompatible',
      name: state.workspace.name,
      files: state.workspace.counts.files,
      folders: state.workspace.counts.folders,
      stacks: state.workspace.stacks.join(', ') || 'none'
    });
  } catch (error) {
    setWorkspaceBlocked(toMessage(error));
  }
  render();
}

async function rescanWorkspaceDirectory() {
  if (state.workspace.directoryHandle) {
    state.workspace.status = 'reading';
    state.workspace.statusMessage = 'Releyendo workspace...';
    renderWorkspaceUnderstanding();
    try {
      const directoryHandle = state.workspace.directoryHandle;
      state.workspace = await scanWorkspaceDirectory(directoryHandle);
      state.workspace.directoryHandle = directoryHandle;
      attachWorkspaceToActiveMission();
      saveWorkspaceStore();
      saveDemoStore();
      addLog('local', { kind: 'WorkspaceRescan', name: state.workspace.name });
    } catch (error) {
      setWorkspaceBlocked(toMessage(error));
    }
    render();
    return;
  }
  if (state.workspace.source === 'file-directory-input') {
    state.workspace.status = 'metadata';
    state.workspace.statusMessage = 'Seleccioná nuevamente la carpeta compatible para releer.';
    renderWorkspaceUnderstanding();
    openWorkspaceDirectoryInput();
    return;
  }
  await openWorkspaceDirectory();
}

function clearWorkspaceUnderstanding() {
  if (state.workspace.name && !confirm('Quitar workspace activo de Mission Control?')) {
    return;
  }
  state.workspace = createEmptyWorkspaceStore();
  saveWorkspaceStore();
  attachWorkspaceToActiveMission();
  saveDemoStore();
  addLog('local', { kind: 'WorkspaceCleared' });
  render();
}

function isWorkspacePickerAvailable() {
  return typeof window !== 'undefined' && typeof window.showDirectoryPicker === 'function';
}

function setWorkspaceBlocked(reason) {
  const timestamp = new Date().toISOString();
  state.workspace = {
    ...state.workspace,
    status: 'BLOCKED_BY_CURRENT_BROWSER_CAPABILITIES',
    statusMessage: 'No pude abrir el workspace desde este contexto.',
    error: reason || 'Workspace picker unavailable',
    evidence: {
      summary: 'Workspace no leído.',
      timestamp: new Date().toISOString(),
      readOnly: true,
      noCommands: true,
      noWrites: true
    },
    context: createWorkspaceContextContract({
      ...state.workspace,
      source: state.workspace.source || '',
      scannedAt: timestamp,
      evidence: { summary: 'Workspace no leído.' }
    })
  };
  saveWorkspaceStore();
  addLog('local', { kind: 'WorkspaceOpenBlocked', reason: state.workspace.error });
}

async function scanWorkspaceDirectory(directoryHandle) {
  const now = new Date().toISOString();
  const scan = createWorkspaceScan(directoryHandle && directoryHandle.name ? directoryHandle.name : 'workspace');

  await walkWorkspaceDirectory(directoryHandle, '', 0, scan);
  return finalizeWorkspaceScan(scan, now, {
    source: 'directory-picker',
    pathLabel: `${scan.rootName} (ruta absoluta no expuesta por el navegador)`,
    statusMessage: scan.truncated
      ? 'Workspace leído en modo solo lectura con límites aplicados.'
      : 'Workspace leído en modo solo lectura.'
  });
}

async function scanWorkspaceFileList(files) {
  const now = new Date().toISOString();
  const normalizedFiles = files
    .map((file) => ({
      name: String(file.name || '').trim(),
      relativePath: cleanWorkspacePath(file.webkitRelativePath || file.name || ''),
      hasRelativePath: Boolean(file.webkitRelativePath)
    }))
    .filter((file) => file.name && file.relativePath);
  if (!normalizedFiles.length) {
    throw new Error('La carpeta compatible no expuso archivos legibles.');
  }

  const firstRelative = normalizedFiles.find((file) => file.hasRelativePath);
  const rootName = firstRelative && firstRelative.relativePath.includes('/')
    ? firstRelative.relativePath.split('/')[0]
    : 'workspace compatible';
  const scan = createWorkspaceScan(rootName);
  scan.hasRelativePaths = normalizedFiles.some((file) => file.hasRelativePath);

  for (const file of normalizedFiles) {
    if (scan.truncated) {
      break;
    }
    const itemPath = workspacePathWithoutRoot(file.relativePath, scan.rootName);
    addWorkspaceFileListPath(scan, itemPath || file.name, file.hasRelativePath);
  }

  const relativeNote = scan.hasRelativePaths
    ? 'Workspace leído en modo compatible.'
    : 'Archivos leídos en modo compatible; el navegador no expuso árbol relativo completo.';
  return finalizeWorkspaceScan(scan, now, {
    source: 'file-directory-input',
    pathLabel: `${scan.rootName} (modo compatible del navegador)`,
    statusMessage: scan.truncated
      ? `${relativeNote} Límites aplicados.`
      : relativeNote
  });
}

function createWorkspaceScan(rootName) {
  return {
    rootName,
    counts: { files: 0, folders: 0, ignoredFolders: 0, listedItems: 0 },
    treeItems: [],
    keyFiles: [],
    seenDirs: new Set(),
    ignoredDirs: new Set(),
    flags: {
      dotnet: false,
      node: false,
      typescript: false,
      chromeExtension: false,
      playwright: false,
      scripts: false,
      tests: false,
      docs: false,
      docker: false,
      browserExtension: false,
      source: false
    },
    truncated: false,
    hasRelativePaths: true
  };
}

function finalizeWorkspaceScan(scan, now, options) {
  const stacks = workspaceStacksFromFlags(scan.flags);
  const signals = workspaceSignalsFromScan(scan, stacks);
  const evidence = {
    summary: 'Workspace leído',
    timestamp: now,
    workspace: scan.rootName,
    source: options.source,
    files: scan.counts.files,
    folders: scan.counts.folders,
    stacks,
    keyFiles: scan.keyFiles.slice(0, 12),
    readOnly: true,
    noCommands: true,
    noWrites: true,
    truncated: scan.truncated
  };
  const context = createWorkspaceContextContract({
    name: scan.rootName,
    source: options.source,
    openedAt: now,
    scannedAt: now,
    counts: scan.counts,
    stacks,
    keyFiles: scan.keyFiles,
    treeItems: scan.treeItems,
    evidence
  });

  return normalizeWorkspaceStore({
    status: 'ready',
    statusMessage: options.statusMessage,
    name: scan.rootName,
    pathLabel: options.pathLabel,
    source: options.source,
    openedAt: now,
    scannedAt: now,
    counts: scan.counts,
    stacks,
    keyFiles: scan.keyFiles,
    treeItems: scan.treeItems,
    signals,
    evidence,
    context,
    error: ''
  });
}

function cleanWorkspacePath(path) {
  return String(path || '').replace(/\\/g, '/').replace(/^\/+/, '').replace(/\/+/g, '/').trim();
}

function workspacePathWithoutRoot(relativePath, rootName) {
  const normalized = cleanWorkspacePath(relativePath);
  const rootPrefix = `${rootName}/`;
  return normalized.startsWith(rootPrefix) ? normalized.slice(rootPrefix.length) : normalized;
}

async function walkWorkspaceDirectory(directoryHandle, relativePath, depth, scan) {
  if (depth > WORKSPACE_SCAN_LIMITS.maxDepth || scan.truncated) {
    scan.truncated = true;
    return;
  }

  const entries = [];
  for await (const [name, handle] of directoryHandle.entries()) {
    entries.push([name, handle]);
  }
  entries.sort(([leftName, leftHandle], [rightName, rightHandle]) => {
    if (leftHandle.kind !== rightHandle.kind) {
      return leftHandle.kind === 'directory' ? -1 : 1;
    }
    return leftName.localeCompare(rightName);
  });

  for (const [name, handle] of entries) {
    if (scan.truncated) {
      return;
    }
    const itemPath = relativePath ? `${relativePath}/${name}` : name;
    if (isWorkspaceProtectedPath(itemPath)) {
      continue;
    }
    if (handle.kind === 'directory') {
      if (WORKSPACE_IGNORED_DIRS.has(name)) {
        scan.counts.ignoredFolders++;
        continue;
      }
      scan.counts.folders++;
      trackWorkspaceSignals(itemPath, handle.kind, scan);
      addWorkspaceTreeItem(scan, itemPath, handle.kind, depth, isWorkspaceKeyPath(itemPath, handle.kind));
      if (scan.counts.folders >= WORKSPACE_SCAN_LIMITS.maxFolders) {
        scan.truncated = true;
        return;
      }
      await walkWorkspaceDirectory(handle, itemPath, depth + 1, scan);
    } else if (handle.kind === 'file') {
      if (isWorkspaceIgnoredFileName(name)) {
        continue;
      }
      scan.counts.files++;
      trackWorkspaceSignals(itemPath, handle.kind, scan);
      const important = isWorkspaceKeyPath(itemPath, handle.kind);
      addWorkspaceTreeItem(scan, itemPath, handle.kind, depth, important);
      if (important && scan.keyFiles.length < WORKSPACE_SCAN_LIMITS.maxKeyFiles) {
        scan.keyFiles.push(itemPath);
      }
      if (scan.counts.files >= WORKSPACE_SCAN_LIMITS.maxFiles) {
        scan.truncated = true;
        return;
      }
    }
  }
}

function addWorkspaceFileListPath(scan, relativePath, hasRelativePath) {
  const normalized = cleanWorkspacePath(relativePath);
  if (!normalized) {
    return;
  }
  if (isWorkspaceProtectedPath(normalized)) {
    return;
  }
  const parts = normalized.split('/').filter(Boolean);
  const fileName = parts.pop();
  if (!fileName) {
    return;
  }
  if (isWorkspaceIgnoredFileName(fileName)) {
    return;
  }

  const ignoredSegment = parts.find((part) => WORKSPACE_IGNORED_DIRS.has(part));
  if (ignoredSegment) {
    if (!scan.ignoredDirs.has(ignoredSegment)) {
      scan.ignoredDirs.add(ignoredSegment);
      scan.counts.ignoredFolders++;
    }
    return;
  }

  let currentPath = '';
  for (let index = 0; index < parts.length; index += 1) {
    if (scan.truncated) {
      return;
    }
    const depth = index;
    if (depth > WORKSPACE_SCAN_LIMITS.maxDepth) {
      scan.truncated = true;
      return;
    }
    currentPath = currentPath ? `${currentPath}/${parts[index]}` : parts[index];
    if (!scan.seenDirs.has(currentPath)) {
      scan.seenDirs.add(currentPath);
      scan.counts.folders++;
      trackWorkspaceSignals(currentPath, 'directory', scan);
      addWorkspaceTreeItem(scan, currentPath, 'directory', depth, isWorkspaceKeyPath(currentPath, 'directory'));
      if (scan.counts.folders >= WORKSPACE_SCAN_LIMITS.maxFolders) {
        scan.truncated = true;
        return;
      }
    }
  }

  const filePath = parts.length ? `${parts.join('/')}/${fileName}` : fileName;
  const fileDepth = hasRelativePath ? parts.length : 0;
  if (fileDepth > WORKSPACE_SCAN_LIMITS.maxDepth) {
    scan.truncated = true;
    return;
  }
  scan.counts.files++;
  trackWorkspaceSignals(filePath, 'file', scan);
  const important = isWorkspaceKeyPath(filePath, 'file');
  addWorkspaceTreeItem(scan, filePath, 'file', fileDepth, important);
  if (important && scan.keyFiles.length < WORKSPACE_SCAN_LIMITS.maxKeyFiles) {
    scan.keyFiles.push(filePath);
  }
  if (scan.counts.files >= WORKSPACE_SCAN_LIMITS.maxFiles) {
    scan.truncated = true;
  }
}

function isWorkspaceProtectedPath(path) {
  const normalized = cleanWorkspacePath(path).toLowerCase();
  return WORKSPACE_PROTECTED_PATHS.has(normalized)
    || WORKSPACE_PROTECTED_PATH_PREFIXES.some((prefix) => normalized === prefix || normalized.startsWith(`${prefix}/`));
}

function isWorkspaceIgnoredFileName(name) {
  const normalized = String(name || '').toLowerCase();
  return WORKSPACE_IGNORED_FILE_NAMES.has(normalized)
    || WORKSPACE_IGNORED_FILE_PREFIXES.some((prefix) => normalized.startsWith(prefix));
}

function addWorkspaceTreeItem(scan, path, kind, depth, important) {
  if (scan.treeItems.length >= WORKSPACE_SCAN_LIMITS.maxTreeItems) {
    return;
  }
  scan.treeItems.push({ path, kind, depth, important });
  scan.counts.listedItems = scan.treeItems.length;
}

function isWorkspaceKeyPath(path, kind) {
  const normalized = path.replace(/\\/g, '/');
  const lower = normalized.toLowerCase();
  const fileName = lower.split('/').pop() || lower;
  if (kind === 'directory') {
    return ['src', 'tests', 'browser-extension', 'scripts', 'docs'].includes(fileName);
  }
  return fileName === 'package.json'
    || fileName === 'tsconfig.json'
    || fileName.startsWith('vite.config.')
    || fileName === 'manifest.json'
    || fileName.endsWith('.sln')
    || fileName.endsWith('.slnx')
    || fileName.endsWith('.csproj')
    || fileName === 'program.cs'
    || /^appsettings.*\.json$/i.test(fileName)
    || fileName.startsWith('readme')
    || fileName === 'dockerfile'
    || fileName.startsWith('docker-compose')
    || fileName.startsWith('playwright.config.')
    || fileName === '.gitignore';
}

function trackWorkspaceSignals(path, kind, scan) {
  const normalized = path.replace(/\\/g, '/');
  const lower = normalized.toLowerCase();
  const fileName = lower.split('/').pop() || lower;
  if (kind === 'directory') {
    if (fileName === 'src') scan.flags.source = true;
    if (fileName === 'tests' || fileName.endsWith('.tests')) scan.flags.tests = true;
    if (fileName === 'browser-extension') scan.flags.browserExtension = true;
    if (fileName === 'scripts') scan.flags.scripts = true;
    if (fileName === 'docs') scan.flags.docs = true;
    return;
  }
  if (fileName === 'package.json') scan.flags.node = true;
  if (fileName === 'tsconfig.json' || lower.endsWith('.ts') || lower.endsWith('.tsx')) scan.flags.typescript = true;
  if (fileName.endsWith('.sln') || fileName.endsWith('.slnx') || fileName.endsWith('.csproj') || lower.endsWith('.cs')) scan.flags.dotnet = true;
  if (fileName === 'manifest.json' && lower.includes('browser-extension/')) scan.flags.chromeExtension = true;
  if (fileName.startsWith('playwright.config.')) scan.flags.playwright = true;
  if (fileName === 'dockerfile' || fileName.startsWith('docker-compose')) scan.flags.docker = true;
}

function workspaceStacksFromFlags(flags) {
  const stacks = [];
  if (flags.dotnet) stacks.push('.NET / C#');
  if (flags.node || flags.typescript) stacks.push(flags.typescript ? 'Node / JS / TS' : 'Node / JS');
  if (flags.chromeExtension) stacks.push('Chrome Extension');
  if (flags.playwright) stacks.push('Playwright');
  if (flags.scripts) stacks.push('Scripts');
  if (flags.tests) stacks.push('Tests');
  if (flags.docs) stacks.push('Docs');
  if (flags.docker) stacks.push('Docker');
  return stacks.length ? stacks : ['Proyecto local'];
}

function workspaceSignalsFromScan(scan, stacks) {
  return [
    { label: 'Tipo probable', value: stacks.slice(0, 3).join(' + ') || 'Proyecto local' },
    { label: 'Archivos visibles', value: String(scan.counts.files) },
    { label: 'Carpetas visibles', value: String(scan.counts.folders) },
    { label: 'Ignoradas por peso', value: String(scan.counts.ignoredFolders) },
    { label: 'Tests', value: scan.flags.tests ? 'detectados' : 'sin señal' },
    { label: 'Scripts', value: scan.flags.scripts ? 'detectados' : 'sin señal' },
    { label: 'Browser extension', value: scan.flags.browserExtension || scan.flags.chromeExtension ? 'detectada' : 'sin señal' },
    { label: 'Solución .NET', value: scan.flags.dotnet ? 'detectada' : 'sin señal' }
  ];
}

function attachWorkspaceToActiveMission() {
  const mission = activeDemoMission();
  if (!mission) {
    return;
  }
  mission.workspace = workspaceMissionSummary();
  mission.missionContext = buildMissionWorkspaceContext(mission);
  mission.plan = generateMissionPlanSkeleton(mission);
  mission.proposal = null;
  mission.updatedAt = new Date().toISOString();
}

function workspaceMissionSummary() {
  const workspace = state.workspace || createEmptyWorkspaceStore();
  if (!workspace.name) {
    return null;
  }
  return {
    name: workspace.name,
    pathLabel: workspace.pathLabel,
    source: workspace.source || '',
    selectedAt: workspace.openedAt || '',
    scannedAt: workspace.scannedAt,
    status: workspace.status,
    stacks: workspace.stacks || [],
    files: workspace.counts ? workspace.counts.files : 0,
    folders: workspace.counts ? workspace.counts.folders : 0,
    keyFiles: (workspace.keyFiles || []).filter((path) => !isWorkspaceProtectedPath(path)).slice(0, 10),
    treeSummary: (workspace.treeItems || []).filter((item) => item && !isWorkspaceProtectedPath(item.path)).slice(0, 12),
    projectSignals: workspace.signals || []
  };
}

function buildMissionWorkspaceContext(mission) {
  if (!mission || !state.workspace || !state.workspace.name) {
    return null;
  }
  const workspace = state.workspace;
  return {
    missionId: mission.id,
    missionTitle: mission.title,
    workspaceName: workspace.name,
    workspaceSource: workspace.source || '',
    workspaceSelectedAt: workspace.openedAt || '',
    workspaceLastReadAt: workspace.scannedAt || '',
    stacks: workspace.stacks || [],
    keyFiles: (workspace.keyFiles || []).filter((path) => !isWorkspaceProtectedPath(path)).slice(0, 10),
    treeSummary: (workspace.treeItems || []).filter((item) => item && !isWorkspaceProtectedPath(item.path)).slice(0, 12),
    projectSignals: workspace.signals || [],
    readOnly: true,
    commandsExecuted: false,
    filesModified: false
  };
}

function ensureMissionPlan(mission) {
  if (!mission) {
    return null;
  }
  if (!mission.plan) {
    mission.missionContext = buildMissionWorkspaceContext(mission) || mission.missionContext || null;
    mission.plan = generateMissionPlanSkeleton(mission);
  }
  return mission.plan;
}

function generateMissionPlanSkeleton(mission) {
  const currentMission = mission || activeDemoMission();
  const context = currentMission ? (buildMissionWorkspaceContext(currentMission) || currentMission.missionContext) : null;
  const stacks = context ? context.stacks || [] : [];
  const keyFiles = context ? context.keyFiles || [] : [];
  const hasStack = (needle) => stacks.some((stack) => stack.toLowerCase().includes(needle));
  const now = new Date().toISOString();
  const tasks = [
    missionPlanTask('understand-goal', 'Entender objetivo', 'En revisión', 'Alinear intención, alcance y resultado esperado.', [], ['mission']),
    missionPlanTask('review-context', context ? 'Revisar contexto del workspace' : 'Revisar contexto disponible', context ? 'Listo' : 'Requiere contexto', context
      ? `Usar ${context.workspaceName} en modo solo lectura.`
      : 'No hay workspace activo; el plan queda genérico.', ['understand-goal'], context ? ['workspace'] : ['mission'])
  ];

  if (hasStack('.net') || hasStack('c#')) {
    tasks.push(missionPlanTask('review-dotnet', 'Revisar solución y tests .NET', 'Por hacer', 'La lectura detectó señales .NET/C#.', ['review-context'], ['.NET / C#']));
  }
  if (hasStack('chrome extension')) {
    tasks.push(missionPlanTask('review-extension', 'Revisar extensión Chrome visible', 'Por hacer', 'La lectura detectó manifest o estructura de extensión.', ['review-context'], ['Chrome Extension']));
  }
  if (hasStack('scripts')) {
    tasks.push(missionPlanTask('review-scripts', 'Identificar validaciones disponibles', 'Por hacer', 'La lectura detectó scripts útiles para validar sin ejecutar desde producto.', ['review-context'], ['Scripts']));
  }
  if (hasStack('docs')) {
    tasks.push(missionPlanTask('review-docs', 'Revisar documentación útil', 'Por hacer', 'La lectura detectó documentación del proyecto.', ['review-context'], ['Docs']));
  }

  tasks.push(missionPlanTask('propose-next', 'Proponer próximos pasos', 'Por hacer', 'Convertir contexto en una secuencia de trabajo revisable.', ['review-context'], ['planning']));
  tasks.push(missionPlanTask('summarize-evidence', 'Resumir evidencia', 'Por hacer', 'Dejar un resumen copiable de contexto y plan.', ['propose-next'], ['evidence']));

  const boundedTasks = tasks.slice(0, MISSION_PLAN_TASK_RANGE.max);
  while (boundedTasks.length < MISSION_PLAN_TASK_RANGE.min) {
    boundedTasks.push(missionPlanTask(`step-${boundedTasks.length + 1}`, 'Preparar siguiente paso', 'Por hacer', 'Completar el plan local con un paso revisable.', [], ['planning']));
  }

  return normalizeMissionPlan({
    id: `plan-${Date.now().toString(36)}`,
    generatedAt: now,
    source: 'local-deterministic',
    summary: context
      ? `Plan inicial local usando ${context.workspaceName}.`
      : 'Plan inicial local sin workspace activo.',
    contextUsed: [
      currentMission ? currentMission.title : 'misión local',
      ...(stacks.length ? stacks.slice(0, 4) : ['sin workspace']),
      ...keyFiles.slice(0, 3)
    ],
    readOnly: true,
    commandsExecuted: false,
    filesModified: false,
    tasks: boundedTasks
  });
}

function missionPlanTask(id, title, status, reason, dependsOn = [], workspaceSignals = []) {
  return {
    id,
    title,
    status,
    reason,
    source: 'local-deterministic',
    dependsOn,
    evidenceRefs: [],
    workspaceSignals,
    readOnly: true
  };
}

function generateMissionProposalDraft(mission) {
  const currentMission = mission || activeDemoMission();
  if (!currentMission) {
    return null;
  }
  currentMission.missionContext = buildMissionWorkspaceContext(currentMission) || currentMission.missionContext || null;
  const context = currentMission.missionContext;
  const plan = ensureMissionPlan(currentMission);
  const tasks = plan && Array.isArray(plan.tasks) ? plan.tasks : [];
  const stacks = context ? context.stacks || [] : [];
  const keyFiles = context ? context.keyFiles || [] : [];
  const now = new Date().toISOString();
  const relevantAreas = proposalRelevantAreas(stacks, keyFiles);
  const hasWorkspace = Boolean(context && context.workspaceName);
  const proposalTasks = tasks.map((task) => ({
    id: `proposal-${task.id}`,
    title: task.title,
    reason: proposalTaskReason(task),
    status: task.status || 'Por hacer',
    sourceTaskId: task.id,
    needsHumanReview: true,
    readOnly: true
  }));

  return normalizeMissionProposal({
    proposalId: `proposal-${Date.now().toString(36)}`,
    missionId: currentMission.id,
    missionTitle: currentMission.title,
    workspaceName: hasWorkspace ? context.workspaceName : '',
    workspaceSource: hasWorkspace ? context.workspaceSource : '',
    createdAt: now,
    updatedAt: now,
    status: hasWorkspace ? PROPOSAL_STATUS.ready : PROPOSAL_STATUS.draft,
    summary: hasWorkspace
      ? `Se propone revisar ${currentMission.title} usando ${context.workspaceName} como contexto local.`
      : `Se propone revisar ${currentMission.title} con contexto de misión y plan local.`,
    contextUsed: [
      currentMission.title,
      ...(hasWorkspace ? [context.workspaceName, workspaceSourceLabel(context.workspaceSource) || context.workspaceSource] : ['sin workspace activo']),
      ...stacks.slice(0, 4),
      ...keyFiles.slice(0, 4)
    ],
    tasks: proposalTasks,
    suggestedReviewOrder: proposalTasks.map((task) => task.title),
    relevantAreas,
    assumptions: proposalAssumptions(hasWorkspace, stacks),
    risks: proposalRisks(hasWorkspace, stacks),
    evidence: proposalEvidence(context, plan),
    nextHumanDecision: 'Revisar la propuesta y decidir si el próximo bloque prepara diff read-only o pide más contexto.',
    readOnly: true,
    commandsExecuted: false,
    filesModified: false,
    diffGenerated: false,
    executionReady: false
  });
}

function proposalTaskReason(task) {
  return `Se sugiere revisar: ${task.reason || task.title}`;
}

function proposalRelevantAreas(stacks, keyFiles) {
  const areas = [];
  const hasStack = (needle) => stacks.some((stack) => stack.toLowerCase().includes(needle));
  if (hasStack('.net') || hasStack('c#')) areas.push('Solución y tests .NET');
  if (hasStack('chrome extension')) areas.push('Extensión Chrome visible');
  if (hasStack('scripts')) areas.push('Scripts de validación disponibles');
  if (hasStack('docs')) areas.push('Documentación del proyecto');
  for (const file of keyFiles) {
    if (!isWorkspaceProtectedPath(file)) areas.push(file);
  }
  return [...new Set(areas)].slice(0, 10);
}

function proposalAssumptions(hasWorkspace, stacks) {
  const assumptions = ['La propuesta se generó localmente desde Mission Context y TaskGraph.'];
  if (!hasWorkspace) assumptions.push('No hay workspace activo; la propuesta queda genérica.');
  if (stacks.length) assumptions.push(`Las señales detectadas son: ${stacks.slice(0, 4).join(', ')}.`);
  return assumptions;
}

function proposalRisks(hasWorkspace, stacks) {
  const risks = ['La propuesta no valida cambios de código ni comportamiento runtime.'];
  if (!hasWorkspace) risks.push('Falta contexto de workspace para priorizar áreas concretas.');
  if (stacks.some((stack) => stack.toLowerCase().includes('chrome extension'))) {
    risks.push('Cambios futuros de extensión deben verificarse con el harness instalado.');
  }
  return risks;
}

function proposalEvidence(context, plan) {
  const evidence = [
    plan ? `TaskGraph local: ${plan.tasks.length} tareas` : 'TaskGraph no disponible',
    'readOnly=true',
    'diffGenerated=false',
    'commandsExecuted=false',
    'filesModified=false'
  ];
  if (context && context.workspaceName) {
    evidence.unshift(`Workspace: ${context.workspaceName}`);
    if (context.stacks && context.stacks.length) evidence.push(`Stacks: ${context.stacks.slice(0, 4).join(' + ')}`);
    if (context.keyFiles && context.keyFiles.length) evidence.push(`Key files: ${context.keyFiles.slice(0, 4).join('; ')}`);
  }
  return evidence;
}

function generateReadOnlyChangeCandidates(mission) {
  const currentMission = mission || activeDemoMission();
  if (!currentMission) {
    return [];
  }
  currentMission.missionContext = buildMissionWorkspaceContext(currentMission) || currentMission.missionContext || null;
  currentMission.plan = ensureMissionPlan(currentMission);
  currentMission.proposal = currentMission.proposal || generateMissionProposalDraft(currentMission);
  const context = currentMission.missionContext;
  const proposal = currentMission.proposal;
  const stacks = context ? context.stacks || [] : [];
  const keyFiles = context ? (context.keyFiles || []).filter((path) => !isWorkspaceProtectedPath(path)) : [];
  const hasStack = (needle) => stacks.some((stack) => stack.toLowerCase().includes(needle));
  const baseTasks = proposal && Array.isArray(proposal.tasks) ? proposal.tasks : [];
  const candidates = [];

  const addCandidate = (candidate) => {
    if (candidates.length >= CHANGE_CANDIDATE_LIMITS.max) {
      return;
    }
    const normalized = normalizeChangeCandidate(candidate);
    if (!normalized || isWorkspaceProtectedPath(normalized.likelyTarget) || candidates.some((item) => item.title === normalized.title)) {
      return;
    }
    candidates.push(normalized);
  };

  if (hasStack('chrome extension')) {
    addCandidate(changeCandidateFromSignal(currentMission, proposal, {
      sourceTaskId: findSourceTaskId(baseTasks, 'extensión') || findSourceTaskId(baseTasks, 'contexto'),
      title: 'Revisar Mission Control visible',
      area: 'Extensión Chrome',
      likelyTarget: keyFiles.find((file) => /sidepanel\.(html|js|css)$/i.test(file)) || 'browser-extension/onebrain-chrome-lab',
      targetKind: 'ui',
      intent: 'Preparar revisión de la experiencia visible.',
      reason: 'La propuesta y el workspace apuntan a flujo visible de extensión.',
      riskLevel: 'medio'
    }));
  }
  if (hasStack('.net') || hasStack('c#')) {
    addCandidate(changeCandidateFromSignal(currentMission, proposal, {
      sourceTaskId: findSourceTaskId(baseTasks, '.net') || findSourceTaskId(baseTasks, 'tests'),
      title: 'Preparar validación de Mission Control',
      area: 'Tests de producto',
      likelyTarget: keyFiles.find((file) => /tests|\.csproj|\.slnx$/i.test(file)) || 'tests',
      targetKind: 'test/validación',
      intent: 'Preparar cobertura futura sin ejecutar desde producto.',
      reason: 'La propuesta detectó señales .NET/C# y tareas de validación.',
      riskLevel: 'bajo'
    }));
  }
  if (hasStack('scripts')) {
    addCandidate(changeCandidateFromSignal(currentMission, proposal, {
      sourceTaskId: findSourceTaskId(baseTasks, 'validaciones') || findSourceTaskId(baseTasks, 'scripts'),
      title: 'Revisar validaciones disponibles',
      area: 'Scripts',
      likelyTarget: keyFiles.find((file) => /scripts/i.test(file)) || 'scripts',
      targetKind: 'test/validación',
      intent: 'Identificar qué validaciones podrían usarse en un bloque futuro.',
      reason: 'El workspace indica scripts disponibles como apoyo de revisión.',
      riskLevel: 'bajo'
    }));
  }
  if (hasStack('docs')) {
    addCandidate(changeCandidateFromSignal(currentMission, proposal, {
      sourceTaskId: findSourceTaskId(baseTasks, 'documentación') || findSourceTaskId(baseTasks, 'evidencia'),
      title: 'Revisar documentación visible',
      area: 'Documentación',
      likelyTarget: keyFiles.find((file) => /readme|docs/i.test(file)) || 'README/docs',
      targetKind: 'documentación',
      intent: 'Preparar revisión documental futura.',
      reason: 'La propuesta detectó documentación como contexto útil.',
      riskLevel: 'bajo'
    }));
  }

  for (const task of baseTasks) {
    addCandidate(changeCandidateFromSignal(currentMission, proposal, {
      sourceTaskId: task.sourceTaskId || task.id,
      title: `Preparar revisión: ${task.title}`,
      area: proposal && proposal.workspaceName ? proposal.workspaceName : 'Misión local',
      likelyTarget: firstSafeRelevantArea(proposal) || 'sin objetivo específico',
      targetKind: 'unknown',
      intent: 'Convertir tarea propuesta en revisión futura.',
      reason: task.reason || 'Derivado de propuesta local.',
      riskLevel: 'bajo'
    }));
  }

  while (candidates.length < CHANGE_CANDIDATE_LIMITS.min) {
    addCandidate(changeCandidateFromSignal(currentMission, proposal, {
      sourceTaskId: baseTasks[candidates.length] ? baseTasks[candidates.length].id : '',
      title: candidates.length === 0 ? 'Revisar intención de la misión' : 'Preparar siguiente revisión humana',
      area: proposal && proposal.workspaceName ? proposal.workspaceName : 'Misión local',
      likelyTarget: firstSafeRelevantArea(proposal) || 'sin objetivo específico',
      targetKind: 'unknown',
      intent: 'Preparar revisión futura sin generar cambios.',
      reason: 'No hay señales suficientes para un objetivo más específico.',
      riskLevel: 'bajo'
    }));
  }

  return candidates.slice(0, CHANGE_CANDIDATE_LIMITS.max);
}

function changeCandidateFromSignal(mission, proposal, input) {
  const evidenceRefs = [
    proposal ? `proposal:${proposal.proposalId}` : 'proposal:none',
    input.sourceTaskId ? `task:${input.sourceTaskId}` : 'task:none'
  ];
  return {
    candidateId: `candidate-${Date.now().toString(36)}-${Math.random().toString(36).slice(2, 6)}`,
    proposalId: proposal ? proposal.proposalId : '',
    missionId: mission ? mission.id : '',
    sourceTaskId: input.sourceTaskId || '',
    title: input.title,
    area: input.area,
    likelyTarget: input.likelyTarget,
    targetKind: input.targetKind,
    intent: input.intent,
    reason: input.reason,
    evidenceRefs,
    workspaceSignals: proposal ? proposal.contextUsed || [] : [],
    riskLevel: input.riskLevel || 'bajo',
    humanReviewNeeded: true,
    status: CHANGE_CANDIDATE_STATUS.candidate,
    readOnly: true,
    diffGenerated: false,
    patchGenerated: false,
    commandsExecuted: false,
    filesModified: false,
    executionReady: false
  };
}

function findSourceTaskId(tasks, needle) {
  const value = String(needle || '').toLowerCase();
  const match = (tasks || []).find((task) => `${task.title} ${task.reason}`.toLowerCase().includes(value));
  return match ? match.sourceTaskId || match.id : '';
}

function firstSafeRelevantArea(proposal) {
  const areas = proposal && Array.isArray(proposal.relevantAreas) ? proposal.relevantAreas : [];
  return areas.find((area) => !isWorkspaceProtectedPath(area)) || '';
}

async function captureBrowserActiveTab() {
  await captureBrowserSkillSnapshot({ includeDom: false });
}

async function indexBrowserActivePage() {
  await captureBrowserSkillSnapshot({ includeDom: true });
}

async function captureBrowserSkillSnapshot(options = {}) {
  const includeDom = Boolean(options.includeDom);
  state.browserSkills.status = includeDom ? 'indexing' : 'capturing';
  state.browserSkills.statusMessage = includeDom ? 'Indexando página activa...' : 'Capturando pestaña activa...';
  state.browserSkills.lastError = '';
  renderBrowserSkills();

  try {
    const tab = await readActiveBrowserTab();
    let pageState = null;
    let technicalBlock = '';
    if (includeDom) {
      const canIndex = isBrowserScriptingAvailable() && /^https?:\/\//i.test(tab.url || '');
      if (canIndex) {
        try {
          pageState = await executeBrowserPageIndex(tab.id);
        } catch (error) {
          technicalBlock = toMessage(error);
        }
      } else {
        technicalBlock = browserIndexingBlockReason(tab);
      }
    }
    const snapshot = buildBrowserSkillSnapshot(tab, pageState, {
      includeDom,
      indexed: Boolean(pageState),
      technicalBlock
    });
    persistBrowserSkillSnapshot(snapshot);
    state.browserSkills.status = snapshot.status === 'NOT_IMPLEMENTED_BLOCKED_BY_CURRENT_EXTENSION_CAPABILITIES' ? 'limited' : 'captured';
    state.browserSkills.statusMessage = snapshot.status === 'indexed'
      ? 'Página indexada y guardada en historial.'
      : snapshot.status === 'captured'
        ? 'Pestaña capturada. Usá Indexar página para leer elementos visibles.'
        : 'No pude leer la pestaña desde este contexto.';
    addLog('local', {
      kind: 'BrowserSkillSnapshot',
      snapshotId: snapshot.id,
      status: snapshot.status,
      elements: snapshot.elements.length,
      friction: snapshot.frictionEvents.map((item) => item.type).join(', ') || 'none'
    });
  } catch (error) {
    const snapshot = createUnavailableBrowserSnapshot(toMessage(error));
    persistBrowserSkillSnapshot(snapshot);
    state.browserSkills.status = 'limited';
    state.browserSkills.statusMessage = 'No pude leer la pestaña desde este contexto.';
    state.browserSkills.lastError = snapshot.summary;
    addLog('local', {
      kind: 'BrowserSkillSnapshotUnavailable',
      status: snapshot.status,
      reason: snapshot.summary
    });
  }

  render();
}

function isBrowserTabsAvailable() {
  return typeof chrome !== 'undefined' && Boolean(chrome.tabs && typeof chrome.tabs.query === 'function');
}

function isBrowserScriptingAvailable() {
  return typeof chrome !== 'undefined' && Boolean(chrome.scripting && typeof chrome.scripting.executeScript === 'function');
}

async function readActiveBrowserTab() {
  if (!isBrowserTabsAvailable()) {
    throw new Error('chrome.tabs.query no está disponible fuera del sidepanel instalado.');
  }
  const tabs = await chrome.tabs.query({ active: true, lastFocusedWindow: true });
  const tab = tabs && tabs[0];
  if (!tab || typeof tab.id !== 'number') {
    throw new Error('No se encontró una pestaña activa legible.');
  }
  return tab;
}

function browserIndexingBlockReason(tab) {
  if (!isBrowserScriptingAvailable()) {
    return 'chrome.scripting.executeScript no está disponible en este contexto.';
  }
  if (!/^https?:\/\//i.test(tab && tab.url ? tab.url : '')) {
    return 'La indexación DOM sólo está disponible para páginas http/https desde la extensión instalada.';
  }
  return '';
}

async function executeBrowserPageIndex(tabId) {
  if (!isBrowserScriptingAvailable()) {
    throw new Error('chrome.scripting.executeScript no está disponible en este contexto.');
  }
  const result = await chrome.scripting.executeScript({
    target: { tabId },
    func: collectBrowserSkillPageState
  });
  const pageState = result && result[0] && result[0].result ? result[0].result : null;
  if (!pageState) {
    throw new Error('La página no devolvió un snapshot indexable.');
  }
  return pageState;
}

function buildBrowserSkillSnapshot(tab, pageState, options = {}) {
  const mission = activeDemoMission();
  const capturedAt = new Date().toISOString();
  const elements = Array.isArray(pageState && pageState.elements)
    ? pageState.elements.map(normalizeBrowserIndexedElement).filter(Boolean)
    : [];
  const frictionEvents = Array.isArray(pageState && pageState.frictionEvents)
    ? pageState.frictionEvents.map(normalizeBrowserFrictionEvent).filter(Boolean)
    : [];
  const url = safeBrowserUrl(pageState && pageState.url ? pageState.url : tab.url || '');
  const title = redactSensitive(pageState && pageState.title ? pageState.title : tab.title || 'Pestaña activa');
  const technicalBlock = options.technicalBlock || '';
  const status = technicalBlock
    ? 'NOT_IMPLEMENTED_BLOCKED_BY_CURRENT_EXTENSION_CAPABILITIES'
    : options.indexed
      ? 'indexed'
      : 'captured';
  const suggestedAction = technicalBlock
    ? 'Abrir NODAL OS como extensión instalada sobre una página http/https.'
    : browserSuggestedAction(frictionEvents);
  const summary = technicalBlock
    ? technicalBlock
    : `${title || 'Pestaña activa'} · ${elements.length} elementos · ${browserFrictionLabel(frictionEvents)}`;

  return normalizeBrowserSkillSnapshot({
    id: `browser-snapshot-${Date.now().toString(36)}-${Math.random().toString(36).slice(2, 7)}`,
    url,
    title,
    capturedAt,
    source: options.indexed ? 'chrome.scripting.executeScript' : 'chrome.tabs.query',
    status,
    elements,
    frictionEvents,
    summary,
    suggestedAction,
    missionId: mission ? mission.id : '',
    missionTitle: mission ? mission.title : '',
    capabilitySummary: browserCapabilitySummary(frictionEvents)
  });
}

function createUnavailableBrowserSnapshot(reason) {
  const mission = activeDemoMission();
  const frictionEvents = [{
    type: 'extension_context_unavailable',
    label: 'Contexto de extensión no disponible',
    evidence: reason,
    suggestedAction: 'Abrir el sidepanel desde la extensión instalada.'
  }];
  return normalizeBrowserSkillSnapshot({
    id: `browser-snapshot-${Date.now().toString(36)}-${Math.random().toString(36).slice(2, 7)}`,
    url: '',
    title: 'Sin captura de pestaña',
    capturedAt: new Date().toISOString(),
    source: 'sidepanel-local-context',
    status: 'NOT_IMPLEMENTED_BLOCKED_BY_CURRENT_EXTENSION_CAPABILITIES',
    elements: [],
    frictionEvents,
    summary: reason || 'No pude leer la pestaña desde este contexto.',
    suggestedAction: 'Abrir el sidepanel real instalado y volver a capturar.',
    missionId: mission ? mission.id : '',
    missionTitle: mission ? mission.title : '',
    capabilitySummary: browserCapabilitySummary(frictionEvents)
  });
}

function persistBrowserSkillSnapshot(snapshot) {
  const normalized = normalizeBrowserSkillSnapshot(snapshot);
  if (!normalized) {
    return;
  }
  state.browserSkills.snapshots = [
    normalized,
    ...(state.browserSkills.snapshots || []).filter((item) => item.id !== normalized.id)
  ].slice(0, BROWSER_SKILLS_MAX_SNAPSHOTS);
  state.browserSkills.selectedSnapshotId = normalized.id;
  attachBrowserSkillSnapshotToMission(normalized);
  saveBrowserSkillStore();
}

function attachBrowserSkillSnapshotToMission(snapshot) {
  const mission = activeDemoMission();
  if (!mission) {
    return;
  }
  const summary = {
    ...snapshot,
    elements: snapshot.elements.slice(0, 12),
    frictionEvents: snapshot.frictionEvents.slice(0, 6)
  };
  mission.browserSkillSnapshots = [
    summary,
    ...(mission.browserSkillSnapshots || []).filter((item) => item.id !== snapshot.id)
  ].slice(0, 8);

  const run = selectedDemoRun() || (mission.runs && mission.runs[0]) || null;
  if (run) {
    const evidenceRef = `browser-skill:${snapshot.id}`;
    const alreadyLinked = Array.isArray(run.timeline)
      && run.timeline.some((step) => JSON.stringify(step.evidenceRefs || []).includes(evidenceRef));
    if (!alreadyLinked) {
      run.timeline.push(demoTimelineStep(
        'Browser Skill capturado',
        `${snapshot.elements.length} elementos indexados en ${snapshot.title || snapshot.url || 'pestaña activa'}.`,
        'evidence-ready',
        'BrowserSkill',
        evidenceRef));
    }
    run.logs = [
      { label: 'browser skill', value: `${snapshot.elements.length} elementos · ${browserFrictionLabel(snapshot.frictionEvents)}` },
      ...(Array.isArray(run.logs) ? run.logs : [])
    ].slice(0, 12);
    run.summary = `${mission.title}: run demo con Browser Skill ${snapshot.id}.`;
  }

  syncDemoViewFromStore();
  saveDemoStore();
}

function selectedBrowserSkillSnapshot() {
  const snapshots = state.browserSkills.snapshots || [];
  return snapshots.find((item) => item.id === state.browserSkills.selectedSnapshotId) || snapshots[0] || null;
}

function selectBrowserSkillSnapshot(snapshotId) {
  if (!(state.browserSkills.snapshots || []).some((item) => item.id === snapshotId)) {
    return;
  }
  state.browserSkills.selectedSnapshotId = snapshotId;
  saveBrowserSkillStore();
  renderBrowserSkills();
}

function clearBrowserSnapshotHistory() {
  if (!confirm('Limpiar historial local de Browser Skills?')) {
    return;
  }
  state.browserSkills.snapshots = [];
  state.browserSkills.selectedSnapshotId = '';
  state.browserSkills.status = 'idle';
  state.browserSkills.statusMessage = 'Historial limpio. Capturá una pestaña para empezar.';
  saveBrowserSkillStore();
  addLog('local', { kind: 'BrowserSkillHistoryCleared' });
  render();
}

function renderBrowserSkills() {
  const snapshot = selectedBrowserSkillSnapshot();
  const statusMessage = state.browserSkills.statusMessage || 'Listo para capturar la pestaña activa.';
  el.browserSkillStatus.textContent = snapshot ? statusLabelForBrowserSnapshot(snapshot) : statusMessage;
  el.browserSkillUrl.textContent = snapshot && snapshot.url ? compactText(snapshot.url, 72) : 'Sin captura';
  el.browserSkillTitleValue.textContent = snapshot && snapshot.title ? compactText(snapshot.title, 72) : 'Sin captura';
  el.browserSkillElementCount.textContent = snapshot ? String(snapshot.elements.length) : '0';
  el.browserSkillFriction.textContent = snapshot ? browserFrictionLabel(snapshot.frictionEvents) : 'Sin señales';
  el.browserCaptchaState.textContent = snapshot ? snapshot.capabilitySummary.captcha : 'sin señales';
  el.browserProxyState.textContent = snapshot ? snapshot.capabilitySummary.proxy : 'no configurado en esta demo';
  el.browserStealthState.textContent = snapshot ? snapshot.capabilitySummary.stealth : 'no activo en esta demo';
  el.browserSessionResilienceState.textContent = snapshot ? snapshot.capabilitySummary.sessionResilience : 'sin fricción detectada';
  renderBrowserIndexedElements(snapshot);
  renderBrowserEvidence(snapshot);
  renderBrowserSnapshotHistory();
  renderCdpBrowserSkillsSurface();
}

function renderCdpBrowserSkillsSurface() {
  const model = CDP_BROWSER_SKILLS_SURFACE;
  el.cdpRuntimeLabel.textContent = model.runtimeLabel;
  el.cdpBrowserSkillStatus.textContent = model.status;
  el.cdpExtensionMode.textContent = model.extensionMode;
  el.cdpElementCount.textContent = String(model.elementCount);
  el.cdpFrictionCount.textContent = `${model.frictionCount} señales`;
  el.cdpActionMapCount.textContent = `${model.actionMapCount} acciones`;
  el.cdpSourceState.textContent = model.source;
  el.cdpDomIndexState.textContent = model.domIndex;
  el.cdpEvidenceState.textContent = model.evidenceAvailable ? 'disponible' : 'sin evidencia';
  el.cdpScreenshotState.textContent = model.screenshotCaptured ? 'capturado en página controlada' : 'no capturado';
  el.cdpExtensionUsedState.textContent = model.extensionUsed ? 'false' : 'true';
  el.cdpSystemBrowserState.textContent = model.systemBrowserUsed ? 'false' : 'true';
  el.cdpExternalNavState.textContent = model.externalNavigationBlocked ? 'true' : 'false';
  el.cdpFilesModifiedState.textContent = model.productFilesModified ? 'false' : 'true';
}

function renderBrowserIndexedElements(snapshot) {
  const elements = snapshot && Array.isArray(snapshot.elements) ? snapshot.elements : [];
  if (!elements.length) {
    el.browserIndexedElements.innerHTML = '<p class="browser-empty-state">Capturá e indexá una página para ver links, botones, inputs y headings.</p>';
    return;
  }
  el.browserIndexedElements.innerHTML = elements.slice(0, 24).map((item) => `
    <div class="browser-element-row">
      <span>${safeHtml(item.role || item.tag || 'elemento')}</span>
      <strong>${safeHtml(compactText(item.label || item.selector || 'sin etiqueta', 78))}</strong>
      <small>${safeHtml(item.selector || item.tag)} · ${Math.round((item.confidence || 0) * 100)}%</small>
    </div>`).join('');
}

function renderBrowserEvidence(snapshot) {
  if (!snapshot) {
    el.browserEvidencePanel.innerHTML = '<p class="browser-empty-state">La evidencia aparece después de capturar una pestaña.</p>';
    return;
  }
  const friction = snapshot.frictionEvents.length
    ? snapshot.frictionEvents.map((event) => `<li>${safeHtml(event.label)}${event.evidence ? ` · ${safeHtml(compactText(event.evidence, 72))}` : ''}</li>`).join('')
    : '<li>Sin fricción visible.</li>';
  el.browserEvidencePanel.innerHTML = `
    <dl>
      <dt>Snapshot</dt><dd>${safeHtml(snapshot.id)}</dd>
      <dt>Estado</dt><dd>${safeHtml(statusLabelForBrowserSnapshot(snapshot))}</dd>
      <dt>Fuente</dt><dd>${safeHtml(snapshot.source)}</dd>
      <dt>Fecha</dt><dd>${safeHtml(formatDemoDate(snapshot.capturedAt))}</dd>
      <dt>Misión</dt><dd>${safeHtml(snapshot.missionTitle || 'sin misión asociada')}</dd>
      <dt>Acción sugerida</dt><dd>${safeHtml(snapshot.suggestedAction || browserSuggestedAction(snapshot.frictionEvents))}</dd>
    </dl>
    <div class="browser-friction-list">
      <strong>Fricción detectada</strong>
      <ul>${friction}</ul>
    </div>`;
}

function renderBrowserSnapshotHistory() {
  const snapshots = state.browserSkills.snapshots || [];
  if (!snapshots.length) {
    el.browserSnapshotHistory.innerHTML = '<p class="browser-empty-state">Sin snapshots todavía.</p>';
    return;
  }
  el.browserSnapshotHistory.innerHTML = snapshots.map((snapshot) => {
    const active = snapshot.id === state.browserSkills.selectedSnapshotId;
    return `
      <button class="browser-snapshot-item${active ? ' active' : ''}" type="button" data-browser-snapshot-id="${safeHtml(snapshot.id)}">
        <strong>${safeHtml(compactText(snapshot.title || snapshot.url || 'Pestaña capturada', 64))}</strong>
        <small>${safeHtml(formatDemoDate(snapshot.capturedAt))} · ${snapshot.elements.length} elementos</small>
        <span>${safeHtml(browserFrictionLabel(snapshot.frictionEvents))}</span>
      </button>`;
  }).join('');
  el.browserSnapshotHistory.querySelectorAll('[data-browser-snapshot-id]').forEach((button) => {
    button.addEventListener('click', () => selectBrowserSkillSnapshot(button.getAttribute('data-browser-snapshot-id')));
  });
}

async function copyBrowserSkillSummary() {
  const snapshot = selectedBrowserSkillSnapshot();
  if (!snapshot) {
    addLog('local', { kind: 'BrowserSkillCopySkipped', reason: 'no snapshot' });
    render();
    return;
  }
  const summary = buildBrowserSkillSummary(snapshot);
  try {
    await navigator.clipboard.writeText(summary);
    addLog('local', { kind: 'BrowserSkillSummaryCopied', snapshotId: snapshot.id });
  } catch (error) {
    addLog('local', { kind: 'BrowserSkillSummaryCopyFallback', reason: error && error.message ? error.message : 'clipboard unavailable' });
  }
  render();
}

async function copyCdpBrowserSkillSummary() {
  const summary = buildCdpBrowserSkillSummary();
  window.__nodalLastCdpBrowserSkillsSummary = summary;
  try {
    await navigator.clipboard.writeText(summary);
    addLog('local', { kind: 'CdpBrowserSkillSummaryCopied', source: CDP_BROWSER_SKILLS_SURFACE.source });
  } catch (error) {
    addLog('local', { kind: 'CdpBrowserSkillSummaryCopyFallback', reason: error && error.message ? error.message : 'clipboard unavailable' });
  }
  render();
}

function buildCdpBrowserSkillSummary() {
  const model = CDP_BROWSER_SKILLS_SURFACE;
  return [
    'NODAL OS — Browser Skills CDP',
    `runtime: ${model.runtimeLabel}`,
    `source: ${model.source}`,
    `status: ${model.status}`,
    `readOnly: ${model.readOnly}`,
    `domIndex: ${model.domIndex}`,
    `interactiveElements: ${model.elementCount}`,
    `frictionSignals: ${model.frictionCount}`,
    `actionMapEntries: ${model.actionMapCount}`,
    `evidenceAvailable: ${model.evidenceAvailable}`,
    `screenshotCaptured: ${model.screenshotCaptured}`,
    `extensionUsed: ${model.extensionUsed}`,
    `systemBrowserUsed: ${model.systemBrowserUsed}`,
    `externalNavigationBlocked: ${model.externalNavigationBlocked}`,
    `productFilesModified: ${model.productFilesModified}`,
    `extensionMode: ${model.extensionMode}`,
    'scope: página controlada/data URL verificada por harness CDP',
    'rawDomStored: false',
    'inputValuesStored: false',
    'cookiesOrStorageStored: false'
  ].join('\n');
}

function buildBrowserSkillSummary(snapshot) {
  const elements = snapshot.elements.slice(0, 12).map((item) => `- ${item.role || item.tag}: ${item.label || item.selector || 'sin etiqueta'}`);
  const friction = snapshot.frictionEvents.length
    ? snapshot.frictionEvents.map((event) => `- ${event.label}: ${event.evidence || event.suggestedAction || 'detectado'}`)
    : ['- Sin fricción visible'];
  return [
    'NODAL OS — Browser Skill',
    `snapshot_id: ${snapshot.id}`,
    `captured_at: ${snapshot.capturedAt}`,
    `url: ${snapshot.url || 'sin url'}`,
    `title: ${snapshot.title || 'sin título'}`,
    `status: ${snapshot.status}`,
    `mission: ${snapshot.missionTitle || 'sin misión asociada'}`,
    `elements_found: ${snapshot.elements.length}`,
    `friction: ${browserFrictionLabel(snapshot.frictionEvents)}`,
    `suggested_action: ${snapshot.suggestedAction || browserSuggestedAction(snapshot.frictionEvents)}`,
    'indexed_elements:',
    ...(elements.length ? elements : ['- Sin elementos indexados']),
    'friction_events:',
    ...friction,
    `captcha: ${snapshot.capabilitySummary.captcha}`,
    `proxy: ${snapshot.capabilitySummary.proxy}`,
    `stealth: ${snapshot.capabilitySummary.stealth}`,
    `session_resilience: ${snapshot.capabilitySummary.sessionResilience}`,
    'BrowserAct: referencia externa no usada en runtime'
  ].join('\n');
}

function statusLabelForBrowserSnapshot(snapshot) {
  if (!snapshot) {
    return 'Esperando captura';
  }
  if (snapshot.status === 'indexed') {
    return 'Página indexada';
  }
  if (snapshot.status === 'captured') {
    return 'Pestaña capturada';
  }
  if (snapshot.status === 'NOT_IMPLEMENTED_BLOCKED_BY_CURRENT_EXTENSION_CAPABILITIES') {
    return 'Captura no disponible en este contexto';
  }
  return snapshot.status || 'Captura registrada';
}

function browserFrictionLabel(events) {
  const list = Array.isArray(events) ? events : [];
  if (!list.length) {
    return 'Sin señales';
  }
  return list.map((item) => item.label || item.type).join(', ');
}

function browserSuggestedAction(events) {
  const list = Array.isArray(events) ? events : [];
  if (!list.length) {
    return 'Continuar con revisión de elementos y evidencia.';
  }
  if (list.some((item) => item.type === 'captcha_visible')) {
    return 'Revisar captcha manualmente antes de continuar.';
  }
  if (list.some((item) => item.type === 'login_required' || item.type === 'session_expired')) {
    return 'Confirmar sesión manualmente y volver a capturar.';
  }
  if (list.some((item) => item.type === 'access_restricted')) {
    return 'Revisar acceso o permisos de la página.';
  }
  if (list.some((item) => item.type === 'empty_or_error')) {
    return 'Recargar la página o revisar la URL.';
  }
  return list[0].suggestedAction || 'Revisar la señal detectada.';
}

function browserCapabilitySummary(events) {
  const list = Array.isArray(events) ? events : [];
  const hasCaptcha = list.some((item) => item.type === 'captcha_visible');
  const sessionIssue = list.find((item) => item.type === 'session_expired' || item.type === 'login_required' || item.type === 'access_restricted');
  return {
    captcha: hasCaptcha ? 'detectado; revisión humana sugerida' : 'no detectado',
    proxy: 'no configurado en esta demo',
    stealth: 'no activo en esta demo',
    sessionResilience: sessionIssue ? sessionIssue.suggestedAction || 'revisar sesión manualmente' : 'sin fricción detectada'
  };
}

function safeBrowserUrl(value) {
  const raw = String(value || '');
  if (!raw) {
    return '';
  }
  try {
    const url = new URL(raw);
    const sensitiveKeys = ['token', 'access_token', 'refresh_token', 'id_token', 'api_key', 'apikey', 'key', 'secret', 'password', 'session', 'cookie', 'code'];
    sensitiveKeys.forEach((key) => {
      if (url.searchParams.has(key)) {
        url.searchParams.set(key, '[redacted]');
      }
    });
    return redactSensitive(url.toString());
  } catch {
    return redactSensitive(raw);
  }
}

function compactText(value, maxLength = 80) {
  const text = String(value || '').replace(/\s+/g, ' ').trim();
  return text.length > maxLength ? `${text.slice(0, Math.max(0, maxLength - 1))}…` : text;
}

function collectBrowserSkillPageState() {
  const now = new Date().toISOString();
  const bodyText = ((document.body && document.body.innerText) || '').replace(/\s+/g, ' ').trim();
  const selectors = [
    'a[href]',
    'button',
    'input',
    'textarea',
    'select',
    'h1',
    'h2',
    'h3',
    'form',
    '[role="button"]',
    '[role="link"]'
  ];

  function scrub(value, maxLength) {
    return String(value || '')
      .replace(/\s+/g, ' ')
      .replace(/(password|passwd|secret|token|access_token|refresh_token|api[_-]?key|cookie|authorization)\s*[:=]\s*[^;\s,}]+/gi, '$1=[redacted]')
      .trim()
      .slice(0, maxLength || 120);
  }

  function isVisible(element) {
    if (!element || !(element instanceof Element)) {
      return false;
    }
    const style = window.getComputedStyle(element);
    const rect = element.getBoundingClientRect();
    return style.display !== 'none'
      && style.visibility !== 'hidden'
      && Number(style.opacity || 1) > 0
      && rect.width > 0
      && rect.height > 0;
  }

  function cssEscape(value) {
    if (window.CSS && typeof window.CSS.escape === 'function') {
      return window.CSS.escape(String(value));
    }
    return String(value).replace(/["\\#.:,[\]>+~*'=]/g, '\\$&');
  }

  function selectorFor(element) {
    const tag = element.tagName.toLowerCase();
    if (element.id) {
      return `#${cssEscape(element.id)}`;
    }
    const aria = element.getAttribute('aria-label');
    if (aria) {
      return `${tag}[aria-label="${scrub(aria, 60).replace(/"/g, '\\"')}"]`;
    }
    const name = element.getAttribute('name');
    if (name) {
      return `${tag}[name="${scrub(name, 60).replace(/"/g, '\\"')}"]`;
    }
    const type = element.getAttribute('type');
    if (type && (tag === 'input' || tag === 'button')) {
      return `${tag}[type="${scrub(type, 24).replace(/"/g, '\\"')}"]`;
    }
    const siblings = Array.from(element.parentElement ? element.parentElement.children : []);
    const sameTag = siblings.filter((item) => item.tagName === element.tagName);
    const index = sameTag.indexOf(element) + 1;
    return index > 0 ? `${tag}:nth-of-type(${index})` : tag;
  }

  function roleFor(element) {
    const explicit = element.getAttribute('role');
    if (explicit) {
      return scrub(explicit, 40);
    }
    const tag = element.tagName.toLowerCase();
    if (tag === 'a') return 'link';
    if (tag === 'button') return 'button';
    if (tag === 'textarea') return 'textbox';
    if (tag === 'select') return 'select';
    if (/^h[1-6]$/.test(tag)) return 'heading';
    if (tag === 'form') return 'form';
    if (tag === 'input') {
      const type = (element.getAttribute('type') || 'text').toLowerCase();
      if (type === 'checkbox' || type === 'radio') return type;
      if (type === 'submit') return 'button';
      return 'textbox';
    }
    return tag;
  }

  function labelFor(element) {
    const tag = element.tagName.toLowerCase();
    const labelledBy = element.getAttribute('aria-labelledby');
    const labelledByText = labelledBy
      ? labelledBy.split(/\s+/).map((id) => {
        const ref = document.getElementById(id);
        return ref ? ref.innerText : '';
      }).join(' ')
      : '';
    const labelNode = element.id ? document.querySelector(`label[for="${cssEscape(element.id)}"]`) : null;
    const label = element.getAttribute('aria-label')
      || labelledByText
      || (labelNode && labelNode.innerText)
      || element.getAttribute('placeholder')
      || element.getAttribute('name')
      || (tag === 'input' ? element.getAttribute('type') : '')
      || element.innerText
      || element.textContent
      || tag;
    return scrub(label, 96);
  }

  const elements = Array.from(document.querySelectorAll(selectors.join(',')))
    .filter(isVisible)
    .slice(0, 60)
    .map((element) => ({
      tag: element.tagName.toLowerCase(),
      role: roleFor(element),
      label: labelFor(element),
      selector: selectorFor(element),
      visible: true,
      confidence: element.id || element.getAttribute('aria-label') ? 0.92 : 0.72
    }));

  const frictionEvents = [];
  function addFriction(type, label, evidence, suggestedAction) {
    if (!frictionEvents.some((item) => item.type === type)) {
      frictionEvents.push({ type, label, evidence: scrub(evidence, 96), suggestedAction });
    }
  }

  const text = bodyText.toLowerCase();
  const title = document.title || '';
  if (/captcha|recaptcha|hcaptcha|verify you are human|i'?m not a robot|no soy un robot|verifica que eres humano/i.test(bodyText)
    || document.querySelector('[class*="captcha" i], [id*="captcha" i], iframe[src*="captcha" i]')) {
    addFriction('captcha_visible', 'Captcha visible', 'captcha marker', 'Resolver o revisar captcha manualmente.');
  }
  if (document.querySelector('input[type="password"], input[name*="email" i], input[type="email"]')
    || /sign in|log in|login|iniciar sesión|ingresar|password|contraseña/i.test(bodyText)) {
    addFriction('login_required', 'Login requerido', 'login/password marker', 'Confirmar sesión manualmente.');
  }
  if (/access denied|forbidden|not authorized|blocked|unavailable|acceso denegado|prohibido|no autorizado/i.test(bodyText)) {
    addFriction('access_restricted', 'Acceso restringido', 'access restriction marker', 'Revisar permisos de acceso.');
  }
  if (!bodyText || bodyText.length < 20 || /error|not found|404|500|server error/i.test(title)) {
    addFriction('empty_or_error', 'Página vacía o error', title || 'low text content', 'Recargar o revisar la URL.');
  }
  if (/session expired|session timeout|sesión expirada|sesion expirada|tu sesión expiró|expired session/i.test(text)) {
    addFriction('session_expired', 'Sesión expirada', 'session marker', 'Renovar sesión manualmente.');
  }

  const summary = `${title || location.hostname || 'Página'} · ${elements.length} elementos · ${frictionEvents.length ? frictionEvents.map((item) => item.label).join(', ') : 'sin fricción visible'}`;
  return {
    url: location.href,
    title,
    timestamp: now,
    elements,
    frictionEvents,
    summary,
    source: 'page-dom'
  };
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
      reason: `Acciones sensibles detectadas: ${sensitiveActions.join(', ') || 'requiere revisión'}`,
      expectedOperatorAction: 'No ejecutar desde la UI. Revisar antes de seguir.',
      blockedOptions: policy.blockedOptions || ['credentials', 'submit/pay/sign/delete', 'sensitive sites']
    }] : [],
    safeNextAction: hasSensitive ? 'Revisar antes de seguir.' : 'Revisar el plan; esperar decisión de Core.',
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
        reason: 'Acción sensible detectada en este paso.',
        expectedOperatorAction: 'No ejecutar desde la UI.',
        blockedOptions: policy.blockedOptions || ['submit/pay/sign/delete', 'credentials']
      }] : [],
      safeNextAction: stepSensitive ? 'Revisar con Core antes de seguir.' : 'Esperar decisión de Core.',
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
    planned: ['planned', 'Por hacer'],
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
    'plan-drafted': ['planned', 'Plan armado'],
    plandrafted: ['planned', 'Plan armado'],
    'plan-preview-ready': ['ready', 'Plan listo para revisar'],
    planpreviewready: ['ready', 'Plan listo para revisar'],
    'plan-awaiting-approval': ['needs-human', 'Esperando revisión'],
    planawaitingapproval: ['needs-human', 'Esperando revisión'],
    'plan-approved': ['ready', 'Plan aprobado'],
    planapproved: ['ready', 'Plan aprobado'],
    'plan-rejected': ['not-allowed', 'Plan rechazado'],
    planrejected: ['not-allowed', 'Plan rechazado'],
    'plan-edited-by-human': ['warning', 'Editado por humano'],
    planeditedbyhuman: ['warning', 'Editado por humano'],
    'execution-started': ['running', 'Ejecución iniciada'],
    executionstarted: ['running', 'Ejecución iniciada'],
    'execution-blocked-by-policy': ['blocked', 'Revisar antes de seguir'],
    executionblockedbypolicy: ['blocked', 'Revisar antes de seguir'],
    'recovery-required': ['blocked', 'Revisión requerida'],
    recoveryrequired: ['blocked', 'Revisión requerida'],
    'waiting-for-human-input': ['needs-human', 'Esperando intervención'],
    waitingforhumaninput: ['needs-human', 'Esperando intervención'],
    'not-allowed': ['not-allowed', 'No permitido'],
    notallowed: ['not-allowed', 'No permitido']
  };
  const normalized = map[value] || ['planned', 'Por hacer'];
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
