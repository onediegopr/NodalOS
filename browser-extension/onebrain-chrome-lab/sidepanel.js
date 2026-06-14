let port = null;
let portConnected = false;

let connectState = 'disconnected';
let healthState = 'untested';
let runState = 'idle';
let currentRunId = '';

const elements = {
  hostInput: document.getElementById('hostInput'),
  portInput: document.getElementById('portInput'),
  connectBtn: document.getElementById('connectBtn'),
  healthBtn: document.getElementById('healthBtn'),
  statusLine: document.getElementById('statusLine'),
  instructionInput: document.getElementById('instructionInput'),
  startStopBtn: document.getElementById('startStopBtn'),
  pauseResumeBtn: document.getElementById('pauseResumeBtn'),
  humanCard: document.getElementById('humanCard'),
  humanMessage: document.getElementById('humanMessage'),
  resumeHumanBtn: document.getElementById('resumeHumanBtn'),
  pageUrl: document.getElementById('pageUrl'),
  pageTitle: document.getElementById('pageTitle'),
  pageTab: document.getElementById('pageTab'),
  pageReady: document.getElementById('pageReady'),
  runId: document.getElementById('runId'),
  currentTool: document.getElementById('currentTool'),
  lastResult: document.getElementById('lastResult'),
  observedList: document.getElementById('observedList'),
  selectedElementId: document.getElementById('selectedElementId'),
  selectedSelector: document.getElementById('selectedSelector'),
  selectedScore: document.getElementById('selectedScore'),
  resolveCandidates: document.getElementById('resolveCandidates'),
  verificationStatus: document.getElementById('verificationStatus'),
  verificationReason: document.getElementById('verificationReason'),
  verificationMeta: document.getElementById('verificationMeta'),
  logs: document.getElementById('logs')
};

connectPort();
refreshUi();
window.addEventListener('error', (event) => {
  log('local', 'panel error: ' + (event.message || 'unknown'));
});
safePost({ type: 'loadConfig' });

elements.connectBtn.addEventListener('click', () => {
  if (connectState === 'connected' || connectState === 'running' || connectState === 'paused') {
    safePost({ type: 'disconnect' });
    return;
  }

  setConnectState('connecting');
  log('local', 'connect requested: ' + currentConfig().host + ':' + currentConfig().port);
  safePost({ type: 'connect', config: currentConfig() });
});

elements.healthBtn.addEventListener('click', () => {
  setHealthState('testing');
  log('local', 'health requested: ' + currentConfig().host + ':' + currentConfig().port);
  testHealthDirect();
  safePost({ type: 'testHealth', config: currentConfig() });
});

elements.startStopBtn.addEventListener('click', () => {
  if (runState === 'running' || runState === 'paused') {
    resetRunSurface();
    safePost({ type: 'stop' });
    return;
  }

  setRunState('running');
  safePost({ type: 'startRun', instruction: elements.instructionInput.value });
});

elements.pauseResumeBtn.addEventListener('click', () => {
  if (runState === 'running') {
    setRunState('paused');
    safePost({ type: 'pause' });
    return;
  }

  if (runState === 'paused') {
    setRunState('running');
    safePost({ type: 'resume' });
  }
});

elements.resumeHumanBtn.addEventListener('click', () => {
  elements.humanCard.classList.add('hidden');
  safePost({ type: 'resumeHuman' });
});

function handlePortMessage(message) {
  switch (message.type) {
    case 'config':
      elements.hostInput.value = message.config.host || '127.0.0.1';
      elements.portInput.value = message.config.port || '8787';
      return;
    case 'state':
      handleState(message);
      return;
    case 'health':
      handleHealth(message);
      return;
    case 'page':
      handlePage(message.page || {});
      return;
    case 'engineMessage':
      handleEngineMessage(message.message || {});
      return;
    case 'toolResult':
      handleToolResult(message);
      return;
    case 'runStatus':
      handleRunStatus(message.message || {});
      return;
    case 'humanIntervention':
      elements.humanMessage.textContent = message.message || message.reason || 'Human intervention required.';
      elements.humanCard.classList.remove('hidden');
      setRunState('paused');
      log('local', elements.humanMessage.textContent);
      return;
    case 'runStarted':
      handleRunStarted(message.body || {});
      return;
    default:
      return;
  }
}

function handleState(message) {
  setConnectState(message.status || 'disconnected');
  if (message.currentRunId) {
    currentRunId = message.currentRunId;
    elements.runId.textContent = currentRunId;
  }

  if (message.status === 'running') {
    setRunState('running');
  } else if (message.status === 'paused') {
    setRunState('paused');
  }

  log('local', (message.status || 'state') + ': ' + (message.message || ''));
}

function handleHealth(message) {
  setHealthState(message.ok ? 'ok' : 'fail');
  log('local', message.ok
    ? 'Health OK: ' + ((message.body && message.body.service) || 'bridge') + ' ' + ((message.body && message.body.version) || '')
    : 'Health failed: ' + (message.error || 'bridge unavailable'));
}

function handlePage(page) {
  elements.pageUrl.textContent = page.url || '-';
  elements.pageTitle.textContent = page.title || '-';
  elements.pageTab.textContent = page.tabId || '-';
  elements.pageReady.textContent = page.restricted ? 'restricted' : 'available';
}

function handleEngineMessage(message) {
  log('engine->extension', summarize(message));
  if (message.tool) {
    elements.currentTool.textContent = message.tool;
  }
}

function handleToolResult(message) {
  elements.lastResult.textContent = message.success ? 'success' : 'error: ' + (message.error || '');
  log('extension->engine', summarize(message));

  const tool = message.request && message.request.tool ? message.request.tool : '';
  if (tool === 'observePage' && message.result) {
    renderObserved(message.result);
  } else if (tool === 'resolveTarget' && message.result) {
    renderResolution(message.result);
  } else if ((tool === 'clickElement' || tool === 'click') && message.result) {
    renderVerification(message.result);
  } else if ((tool === 'setElementValue' || tool === 'setValue') && message.result) {
    renderVerification(message.result);
  } else if ((tool === 'highlightElement' || tool === 'highlight') && message.result) {
    renderHighlighted(message.result);
  }
}

function handleRunStatus(message) {
  if (message.message) {
    elements.lastResult.textContent = message.message;
  }

  if (message.status === 'running') {
    setRunState('running');
  } else if (message.status === 'paused') {
    setRunState('paused');
  } else if (message.status === 'error' || message.status === 'stopped') {
    setRunState('idle');
    currentRunId = '';
    elements.runId.textContent = '-';
  }

  log('engine->extension', summarize(message));
}

function handleRunStarted(body) {
  if (body.runId) {
    currentRunId = body.runId;
    elements.runId.textContent = body.runId;
  }

  if (body.status === 'error') {
    setRunState('idle');
    currentRunId = '';
    elements.runId.textContent = '-';
  } else if (body.status) {
    setRunState('running');
  }

  elements.lastResult.textContent = body.message || (body.status || 'run started');
  log('local', summarize(body));
}

function renderObserved(observation) {
  const summary = observation.elementCatalogSummary || {};
  const top = observation.topInteractiveElements || [];
  const pills = top.slice(0, 24).map((element) => {
    const label = element.accessibleName || element.visibleText || element.elementId || element.tagName || 'element';
    const risk = Array.isArray(element.riskFlags) && element.riskFlags.length > 0
      ? ' · ' + element.riskFlags.join(',')
      : '';
    return '<span class="pill">' + escapeHtml(label) + risk + '</span>';
  }).join('');

  elements.observedList.innerHTML =
    '<div class="observed-summary">' +
    'elements=' + escapeHtml(summary.totalElements || 0) +
    ' · clickable=' + escapeHtml(summary.clickableElements || 0) +
    ' · credential-risk=' + escapeHtml(summary.credentialLikeElements || 0) +
    '</div>' +
    (pills || 'No visible elements.');
  elements.pageReady.textContent = observation.readyState || 'unknown';
}

function renderResolution(result) {
  const best = result.bestCandidate || null;
  elements.selectedElementId.textContent = best && best.elementId ? best.elementId : '-';
  elements.selectedSelector.textContent = best && best.bestSelector && best.bestSelector.selector
    ? best.bestSelector.selector
    : '-';
  elements.selectedScore.textContent = best && typeof best.score === 'number'
    ? best.score.toFixed(2)
    : '-';

  const candidates = Array.isArray(result.candidates) ? result.candidates : [];
  if (candidates.length === 0) {
    elements.resolveCandidates.textContent = 'No candidates returned.';
    return;
  }

  elements.resolveCandidates.innerHTML = candidates.map((candidate) => {
    const label = candidate.element && (candidate.element.accessibleName || candidate.element.visibleText || candidate.element.elementId)
      ? candidate.element.accessibleName || candidate.element.visibleText || candidate.element.elementId
      : candidate.elementId;
    const selector = candidate.bestSelector && candidate.bestSelector.selector ? candidate.bestSelector.selector : '-';
    const reason = candidate.reason || (Array.isArray(candidate.reasons) ? candidate.reasons.join(' · ') : '');
    return '<div class="candidate">' +
      '<div class="candidate-head"><strong>' + escapeHtml(label || '-') + '</strong><span>' + escapeHtml(candidate.elementId || '-') + '</span></div>' +
      '<div class="candidate-meta">score ' + escapeHtml((candidate.score || 0).toFixed ? candidate.score.toFixed(2) : candidate.score || '-') + ' · ' + escapeHtml(selector) + '</div>' +
      '<div class="candidate-reason">' + escapeHtml(reason) + '</div>' +
      '</div>';
  }).join('');
}

function renderVerification(result) {
  elements.verificationStatus.textContent = result.verificationStatus || '-';
  elements.verificationReason.textContent = result.reason || '-';
  const details = [
    'beforeUrl=' + (result.beforeUrl || '-'),
    'afterUrl=' + (result.afterUrl || '-'),
    'urlChanged=' + Boolean(result.urlChanged),
    'domChanged=' + Boolean(result.domChanged),
    'expected=' + Boolean(result.expectedConditionMet)
  ];
  elements.verificationMeta.textContent = details.join(' · ');

  if (result.elementId) {
    elements.selectedElementId.textContent = result.elementId;
  }
  if (result.bestSelector && result.bestSelector.selector) {
    elements.selectedSelector.textContent = result.bestSelector.selector;
  }
}

function renderHighlighted(result) {
  if (result.elementId) {
    elements.selectedElementId.textContent = result.elementId;
  }
  elements.verificationStatus.textContent = 'highlighted';
  elements.verificationReason.textContent = 'Candidate highlighted before action or pause.';
  elements.verificationMeta.textContent = result.elementId ? 'element=' + result.elementId : 'highlighted';
}

function connectPort() {
  try {
    port = chrome.runtime.connect({ name: 'onebrain-sidepanel' });
    portConnected = true;
    port.onMessage.addListener(handlePortMessage);
    port.onDisconnect.addListener(() => {
      portConnected = false;
      port = null;
      setConnectState('disconnected');
      log('local', 'Service worker port disconnected');
    });
    log('local', 'Service worker port connected');
  } catch (error) {
    portConnected = false;
    port = null;
    setConnectState('error');
    log('local', 'connectPort failed: ' + toMessage(error));
  }
}

function currentConfig() {
  return {
    host: elements.hostInput.value.trim() || '127.0.0.1',
    port: elements.portInput.value.trim() || '8787'
  };
}

function testHealthDirect() {
  return fetch('http://' + currentConfig().host + ':' + currentConfig().port + '/health', { cache: 'no-store' })
    .then((response) => response.json().then((body) => ({ response, body })))
    .then((result) => {
      if (result.response.ok && result.body && result.body.ok) {
        setHealthState('ok');
        if (connectState !== 'connected' && connectState !== 'running' && connectState !== 'paused') {
          setConnectState('bridge-ready');
        }
        log('local', 'Health OK: ' + (result.body.service || 'bridge') + ' ' + (result.body.version || ''));
        return;
      }
      setHealthState('fail');
      log('local', 'Health failed: HTTP ' + result.response.status);
    })
    .catch((error) => {
      setHealthState('fail');
      log('local', 'Health failed: ' + toMessage(error));
    });
}

function safePost(message) {
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
    setConnectState('error');
    log('local', 'postMessage failed: ' + toMessage(error));
  }
}

function setConnectState(state) {
  connectState = state;
  refreshUi();
}

function setHealthState(state) {
  healthState = state;
  refreshUi();
}

function setRunState(state) {
  runState = state;
  refreshUi();
}

function refreshUi() {
  refreshConnectButton();
  refreshHealthButton();
  refreshRunButtons();
  refreshStatusLine();
}

function refreshConnectButton() {
  const button = elements.connectBtn;
  switch (connectState) {
    case 'connected':
    case 'running':
      button.textContent = 'Disconnect';
      button.className = 'action-btn connected';
      break;
    case 'paused':
      button.textContent = 'Disconnect';
      button.className = 'action-btn paused';
      break;
    case 'connecting':
      button.textContent = 'Connecting';
      button.className = 'action-btn connecting';
      break;
    case 'bridge-ready':
      button.textContent = 'Connect';
      button.className = 'action-btn bridge-ready';
      break;
    case 'error':
      button.textContent = 'Connect';
      button.className = 'action-btn error';
      break;
    default:
      button.textContent = 'Connect';
      button.className = 'action-btn disconnected';
      break;
  }
}

function refreshHealthButton() {
  const button = elements.healthBtn;
  switch (healthState) {
    case 'ok':
      button.textContent = 'Health OK';
      button.className = 'action-btn ok';
      break;
    case 'testing':
      button.textContent = 'Testing';
      button.className = 'action-btn testing';
      break;
    case 'fail':
      button.textContent = 'Health Fail';
      button.className = 'action-btn fail';
      break;
    default:
      button.textContent = 'Test Health';
      button.className = 'action-btn fail';
      break;
  }
}

function refreshRunButtons() {
  const startStop = elements.startStopBtn;
  const pauseResume = elements.pauseResumeBtn;

  if (runState === 'running') {
    startStop.textContent = 'STOP';
    startStop.className = 'action-btn stop';
    pauseResume.textContent = 'Pause';
    pauseResume.className = 'action-btn paused';
    pauseResume.disabled = false;
    return;
  }

  if (runState === 'paused') {
    startStop.textContent = 'STOP';
    startStop.className = 'action-btn stop';
    pauseResume.textContent = 'Resume';
    pauseResume.className = 'action-btn resume';
    pauseResume.disabled = false;
    return;
  }

  startStop.textContent = 'Start Run';
  startStop.className = 'action-btn start';
  pauseResume.textContent = 'Pause';
  pauseResume.className = 'action-btn paused disabled';
  pauseResume.disabled = true;
}

function refreshStatusLine() {
  const parts = [];
  let cssClass = '';

  switch (connectState) {
    case 'connected':
      parts.push('Connected');
      cssClass = 'connected';
      break;
    case 'connecting':
      parts.push('Connecting');
      cssClass = 'connecting';
      break;
    case 'bridge-ready':
      parts.push('Bridge ready');
      cssClass = 'bridge-ready';
      break;
    case 'paused':
      parts.push('Connected');
      cssClass = 'paused';
      break;
    case 'running':
      parts.push('Connected');
      cssClass = 'running';
      break;
    case 'error':
      parts.push('Error');
      cssClass = 'error';
      break;
    default:
      parts.push('Disconnected');
      cssClass = 'disconnected';
      break;
  }

  switch (healthState) {
    case 'ok':
      parts.push('Health OK');
      break;
    case 'testing':
      parts.push('Testing health');
      break;
    default:
      parts.push('Health not tested');
      break;
  }

  if (runState === 'running') {
    parts.push('Running');
  } else if (runState === 'paused') {
    parts.push('Paused');
  }

  elements.statusLine.textContent = parts.join(' · ');
  elements.statusLine.className = 'status-line ' + cssClass;
}

function resetRunSurface() {
  setRunState('idle');
  currentRunId = '';
  elements.runId.textContent = '-';
  elements.currentTool.textContent = '-';
  elements.lastResult.textContent = '-';
  elements.observedList.innerHTML = 'No observation yet.';
  elements.selectedElementId.textContent = '-';
  elements.selectedSelector.textContent = '-';
  elements.selectedScore.textContent = '-';
  elements.resolveCandidates.textContent = 'No candidates yet.';
  elements.verificationStatus.textContent = '-';
  elements.verificationReason.textContent = '-';
  elements.verificationMeta.textContent = 'No verification yet.';
  elements.humanCard.classList.add('hidden');
}

function log(direction, message) {
  const node = document.createElement('div');
  node.className = 'log';
  const time = new Date().toLocaleTimeString();
  node.innerHTML = '<time>' + time + '</time> <strong>' + escapeHtml(direction) + '</strong><br>' + escapeHtml(message);
  elements.logs.prepend(node);
}

function summarize(value) {
  const text = typeof value === 'string' ? value : JSON.stringify(value);
  return text.length > 700 ? text.slice(0, 700) + '...' : text;
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
