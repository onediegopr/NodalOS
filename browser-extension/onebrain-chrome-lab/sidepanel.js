const port = chrome.runtime.connect({ name: 'onebrain-sidepanel' });

const els = {
  statusBadge: document.getElementById('statusBadge'),
  hostInput: document.getElementById('hostInput'),
  portInput: document.getElementById('portInput'),
  connectBtn: document.getElementById('connectBtn'),
  disconnectBtn: document.getElementById('disconnectBtn'),
  healthBtn: document.getElementById('healthBtn'),
  healthStatus: document.getElementById('healthStatus'),
  instructionInput: document.getElementById('instructionInput'),
  startBtn: document.getElementById('startBtn'),
  pauseBtn: document.getElementById('pauseBtn'),
  resumeBtn: document.getElementById('resumeBtn'),
  stopBtn: document.getElementById('stopBtn'),
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
  logs: document.getElementById('logs')
};

port.onDisconnect.addListener(() => {
  setStatus('error', 'Side panel disconnected from service worker. Reload the extension.');
  log('local', 'Service worker port disconnected');
});

window.addEventListener('error', (event) => {
  setStatus('error', event.message || 'Side panel error');
  log('local', `panel error: ${event.message || 'unknown'}`);
});

safePost({ type: 'loadConfig' });

els.connectBtn.addEventListener('click', () => {
  setStatus('connecting', 'Connecting to bridge...');
  log('local', `connect requested: ${currentConfig().host}:${currentConfig().port}`);
  safePost({ type: 'connect', config: currentConfig() });
});
els.disconnectBtn.addEventListener('click', () => safePost({ type: 'disconnect' }));
els.healthBtn.addEventListener('click', async () => {
  els.healthStatus.textContent = 'Testing health...';
  log('local', `health requested: ${currentConfig().host}:${currentConfig().port}`);
  await testHealthDirect();
  safePost({ type: 'testHealth', config: currentConfig() });
});
els.startBtn.addEventListener('click', () => safePost({ type: 'startRun', instruction: els.instructionInput.value }));
els.pauseBtn.addEventListener('click', () => safePost({ type: 'pause' }));
els.resumeBtn.addEventListener('click', () => safePost({ type: 'resume' }));
els.stopBtn.addEventListener('click', () => safePost({ type: 'stop' }));
els.resumeHumanBtn.addEventListener('click', () => {
  els.humanCard.classList.add('hidden');
  safePost({ type: 'resumeHuman' });
});

port.onMessage.addListener((message) => {
  if (message.type === 'config') {
    els.hostInput.value = message.config.host || '127.0.0.1';
    els.portInput.value = message.config.port || '8787';
    return;
  }

  if (message.type === 'state') {
    setStatus(message.status || 'disconnected', message.message || '');
    if (message.currentRunId) {
      els.runId.textContent = message.currentRunId;
    }
    log('local', `${message.status}: ${message.message || ''}`);
    return;
  }

  if (message.type === 'health') {
    els.healthStatus.textContent = message.ok ? 'Health OK' : `Health failed: ${message.error || 'bad response'}`;
    log('local', els.healthStatus.textContent);
    return;
  }

  if (message.type === 'page') {
    const page = message.page || {};
    els.pageUrl.textContent = page.url || '-';
    els.pageTitle.textContent = page.title || '-';
    els.pageTab.textContent = page.tabId || '-';
    els.pageReady.textContent = page.restricted ? 'restricted' : 'available';
    return;
  }

  if (message.type === 'engineMessage') {
    log('engine->extension', summarize(message.message));
    if (message.message && message.message.tool) {
      els.currentTool.textContent = message.message.tool;
    }
    return;
  }

  if (message.type === 'toolResult') {
    els.lastResult.textContent = message.success ? 'success' : `error: ${message.error || ''}`;
    log('extension->engine', summarize(message));
    if (message.result && message.result.inputs) {
      renderObserved(message.result);
    }
    return;
  }

  if (message.type === 'humanIntervention') {
    els.humanMessage.textContent = message.message || message.reason || 'Human intervention required.';
    els.humanCard.classList.remove('hidden');
    setStatus('paused', els.humanMessage.textContent);
    log('local', els.humanMessage.textContent);
    return;
  }

  if (message.type === 'runStarted') {
    if (message.body && message.body.runId) {
      els.runId.textContent = message.body.runId;
    }
    log('local', summarize(message.body || message));
  }
});

function currentConfig() {
  return {
    host: els.hostInput.value.trim() || '127.0.0.1',
    port: els.portInput.value.trim() || '8787'
  };
}

async function testHealthDirect() {
  const config = currentConfig();
  const url = `http://${config.host}:${config.port}/health`;
  try {
    const response = await fetch(url, { cache: 'no-store' });
    const body = await response.json();
    if (response.ok && body && body.ok) {
      els.healthStatus.textContent = `Health OK: ${body.service || 'bridge'} ${body.version || ''}`;
      setStatus('connected', 'Bridge health OK');
      log('local', els.healthStatus.textContent);
      return;
    }

    els.healthStatus.textContent = `Health failed: HTTP ${response.status}`;
    setStatus('error', els.healthStatus.textContent);
    log('local', els.healthStatus.textContent);
  } catch (error) {
    const message = error && error.message ? error.message : String(error);
    els.healthStatus.textContent = `Health failed: ${message}`;
    setStatus('error', els.healthStatus.textContent);
    log('local', els.healthStatus.textContent);
  }
}

function safePost(message) {
  try {
    port.postMessage(message);
  } catch (error) {
    const text = error && error.message ? error.message : String(error);
    setStatus('error', `Service worker unavailable: ${text}`);
    log('local', `postMessage failed: ${text}`);
  }
}

function setStatus(status, message) {
  els.statusBadge.textContent = status;
  els.statusBadge.className = `badge ${status}`;
  if (message) {
    els.lastResult.textContent = message;
  }
}

function log(direction, message) {
  const node = document.createElement('div');
  node.className = 'log';
  const time = new Date().toLocaleTimeString();
  node.innerHTML = `<time>${time}</time> <strong>${escapeHtml(direction)}</strong><br>${escapeHtml(message)}`;
  els.logs.prepend(node);
}

function renderObserved(observation) {
  const inputs = (observation.inputs || []).map((input) => `<span class="pill">${escapeHtml(input.selector || input.tagName)}${input.redacted ? ' · redacted' : ''}</span>`).join('');
  const buttons = (observation.buttons || []).map((button) => `<span class="pill">${escapeHtml(button.text || button.selector)}</span>`).join('');
  const links = (observation.links || []).slice(0, 20).map((link) => `<span class="pill">${escapeHtml(link.text || link.href)}</span>`).join('');
  els.observedList.innerHTML = `${inputs}${buttons}${links}` || 'No visible elements.';
  els.pageReady.textContent = observation.readyState || 'unknown';
}

function summarize(value) {
  const text = typeof value === 'string' ? value : JSON.stringify(value);
  return text.length > 500 ? `${text.slice(0, 500)}...` : text;
}

function escapeHtml(value) {
  return String(value || '')
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;')
    .replace(/'/g, '&#039;');
}
