let port = null;
let portConnected = false;

const els = {
  statusBadge: document.getElementById('statusBadge'),
  hostInput: document.getElementById('hostInput'),
  portInput: document.getElementById('portInput'),
  connectBtn: document.getElementById('connectBtn'),
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

connectPort();
updateConnectButton('disconnected');
setHealthState('fail', 'Health not tested.');

window.addEventListener('error', (event) => {
  setStatus('error', event.message || 'Side panel error');
  log('local', `panel error: ${event.message || 'unknown'}`);
});

safePost({ type: 'loadConfig' });

els.connectBtn.addEventListener('click', () => {
  if (els.connectBtn.dataset.mode === 'disconnect') {
    safePost({ type: 'disconnect' });
    return;
  }

  setStatus('connecting', 'Connecting to bridge...');
  log('local', `connect requested: ${currentConfig().host}:${currentConfig().port}`);
  safePost({ type: 'connect', config: currentConfig() });
});
els.healthBtn.addEventListener('click', async () => {
  setHealthState('testing', 'Testing health...');
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

function handlePortMessage(message) {
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
    setHealthState(message.ok ? 'ok' : 'fail', els.healthStatus.textContent);
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

  if (message.type === 'runStatus') {
    if (message.message && message.message.message) {
      els.lastResult.textContent = message.message.message;
    }
    log('engine->extension', summarize(message.message || message));
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
    if (message.body && message.body.status === 'error') {
      setStatus('error', message.body.message || 'Run failed');
      els.lastResult.textContent = message.body.message || 'Run failed';
    } else if (message.body && message.body.status) {
      setStatus(message.body.status, message.body.message || 'Run started');
      els.lastResult.textContent = message.body.message || 'Run started';
    }
    log('local', summarize(message.body || message));
  }
}

function connectPort() {
  try {
    port = chrome.runtime.connect({ name: 'onebrain-sidepanel' });
    portConnected = true;
    port.onMessage.addListener(handlePortMessage);
    port.onDisconnect.addListener(() => {
      portConnected = false;
      port = null;
      setStatus('disconnected', 'Service worker disconnected; will reconnect on next action.');
      log('local', 'Service worker port disconnected');
    });
    log('local', 'Service worker port connected');
  } catch (error) {
    portConnected = false;
    port = null;
    const text = error && error.message ? error.message : String(error);
    setStatus('error', `Service worker connect failed: ${text}`);
    log('local', `connectPort failed: ${text}`);
  }
}

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
      setHealthState('ok', els.healthStatus.textContent);
      if (els.statusBadge.textContent !== 'connected') {
        setStatus('bridge-ready', 'Bridge health OK; press Connect or Start Run.');
      }
      log('local', els.healthStatus.textContent);
      return;
    }

    els.healthStatus.textContent = `Health failed: HTTP ${response.status}`;
    setHealthState('fail', els.healthStatus.textContent);
    setStatus('error', els.healthStatus.textContent);
    log('local', els.healthStatus.textContent);
  } catch (error) {
    const message = error && error.message ? error.message : String(error);
    els.healthStatus.textContent = `Health failed: ${message}`;
    setHealthState('fail', els.healthStatus.textContent);
    setStatus('error', els.healthStatus.textContent);
    log('local', els.healthStatus.textContent);
  }
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
    const text = error && error.message ? error.message : String(error);
    portConnected = false;
    port = null;
    setStatus('error', `Service worker unavailable: ${text}. Reload extension if this repeats.`);
    log('local', `postMessage failed: ${text}`);
  }
}

function setStatus(status, message) {
  els.statusBadge.textContent = status;
  els.statusBadge.className = `badge ${status}`;
  updateConnectButton(status);
  if (message) {
    els.lastResult.textContent = message;
  }
}

function updateConnectButton(status) {
  const connected = status === 'connected' || status === 'running' || status === 'paused';
  els.connectBtn.dataset.mode = connected ? 'disconnect' : 'connect';
  els.connectBtn.textContent = connected ? 'Disconnect' : 'Connect';
  els.connectBtn.className = `status-btn ${connected ? 'connected' : (status || 'disconnected')}`;
}

function setHealthState(state, text) {
  els.healthBtn.className = `health-btn ${state}`;
  if (text) {
    els.healthStatus.textContent = text;
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
