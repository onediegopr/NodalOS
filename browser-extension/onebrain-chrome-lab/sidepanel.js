let port = null;
let portConnected = false;

var _connectState = 'disconnected';
var _healthState = 'untested';
var _runState = 'idle';
var _currentRunId = '';

var ICON = {
  CONNECT: '\u26AB',
  DISCONNECT: '\u26AA',
  HEALTH_OK: '\u2713',
  HEALTH_FAIL: '\u2717',
  HEALTH_TESTING: '\u2026',
  HEART: '\u2764',
  START: '\u25B6',
  STOP: '\u25A0',
  PAUSE: '\u23F8',
  RESUME: '\u25B6'
};

var els = {
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
  logs: document.getElementById('logs')
};

connectPort();
refreshAllIcons();
refreshStatusLine();

window.addEventListener('error', function (event) {
  log('local', 'panel error: ' + (event.message || 'unknown'));
});

safePost({ type: 'loadConfig' });

els.connectBtn.addEventListener('click', function () {
  if (_connectState === 'connected' || _connectState === 'running' || _connectState === 'paused') {
    safePost({ type: 'disconnect' });
    return;
  }
  setConnectState('connecting');
  log('local', 'connect requested: ' + currentConfig().host + ':' + currentConfig().port);
  safePost({ type: 'connect', config: currentConfig() });
});

els.healthBtn.addEventListener('click', function () {
  setHealthState('testing');
  log('local', 'health requested: ' + currentConfig().host + ':' + currentConfig().port);
  testHealthDirect();
  safePost({ type: 'testHealth', config: currentConfig() });
});

els.startStopBtn.addEventListener('click', function () {
  if (_runState === 'running' || _runState === 'paused') {
    setRunState('idle');
    _currentRunId = '';
    els.runId.textContent = '-';
    els.currentTool.textContent = '-';
    els.lastResult.textContent = '-';
    els.observedList.innerHTML = 'No observation yet.';
    els.humanCard.classList.add('hidden');
    safePost({ type: 'stop' });
    return;
  }
  setRunState('running');
  safePost({ type: 'startRun', instruction: els.instructionInput.value });
});

els.pauseResumeBtn.addEventListener('click', function () {
  if (_runState === 'running') {
    setRunState('paused');
    safePost({ type: 'pause' });
    return;
  }
  if (_runState === 'paused') {
    setRunState('running');
    safePost({ type: 'resume' });
    return;
  }
});

els.resumeHumanBtn.addEventListener('click', function () {
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
    setConnectState(message.status || 'disconnected');
    if (message.currentRunId) {
      _currentRunId = message.currentRunId;
      els.runId.textContent = message.currentRunId;
    }
    if (message.status === 'running') {
      setRunState('running');
    } else if (message.status === 'paused') {
      setRunState('paused');
    }
    log('local', message.status + ': ' + (message.message || ''));
    return;
  }

  if (message.type === 'health') {
    if (message.ok) {
      setHealthState('ok');
    } else {
      setHealthState('fail');
    }
    log('local', els.statusLine.textContent);
    return;
  }

  if (message.type === 'page') {
    var page = message.page || {};
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
    els.lastResult.textContent = message.success ? 'success' : 'error: ' + (message.error || '');
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
    var status = message.message && message.message.status ? message.message.status : '';
    if (status === 'running') {
      setRunState('running');
    } else if (status === 'paused') {
      setRunState('paused');
    } else if (status === 'error' || status === 'stopped') {
      setRunState('idle');
      _currentRunId = '';
      els.runId.textContent = '-';
    }
    log('engine->extension', summarize(message.message || message));
    return;
  }

  if (message.type === 'humanIntervention') {
    els.humanMessage.textContent = message.message || message.reason || 'Human intervention required.';
    els.humanCard.classList.remove('hidden');
    setRunState('paused');
    log('local', els.humanMessage.textContent);
    return;
  }

  if (message.type === 'runStarted') {
    if (message.body && message.body.runId) {
      _currentRunId = message.body.runId;
      els.runId.textContent = message.body.runId;
    }
    if (message.body && message.body.status === 'error') {
      setRunState('idle');
      _currentRunId = '';
      els.runId.textContent = '-';
      els.lastResult.textContent = message.body.message || 'Run failed';
      log('local', summarize(message.body || message));
    } else if (message.body && message.body.status) {
      setRunState('running');
      els.lastResult.textContent = message.body.message || 'Run started';
      log('local', summarize(message.body || message));
    }
    return;
  }
}

function connectPort() {
  try {
    port = chrome.runtime.connect({ name: 'onebrain-sidepanel' });
    portConnected = true;
    port.onMessage.addListener(handlePortMessage);
    port.onDisconnect.addListener(function () {
      portConnected = false;
      port = null;
      setConnectState('disconnected');
      log('local', 'Service worker port disconnected');
    });
    log('local', 'Service worker port connected');
  } catch (error) {
    portConnected = false;
    port = null;
    var text = error && error.message ? error.message : String(error);
    setConnectState('error');
    log('local', 'connectPort failed: ' + text);
  }
}

function currentConfig() {
  return {
    host: els.hostInput.value.trim() || '127.0.0.1',
    port: els.portInput.value.trim() || '8787'
  };
}

function testHealthDirect() {
  return fetch('http://' + currentConfig().host + ':' + currentConfig().port + '/health', { cache: 'no-store' })
    .then(function (response) { return response.json().then(function (body) { return { response: response, body: body }; }); })
    .then(function (result) {
      if (result.response.ok && result.body && result.body.ok) {
        setHealthState('ok');
        if (_connectState !== 'connected' && _connectState !== 'running' && _connectState !== 'paused') {
          setConnectState('bridge-ready');
        }
        log('local', 'Health OK: ' + (result.body.service || 'bridge') + ' ' + (result.body.version || ''));
        return;
      }
      setHealthState('fail');
      log('local', 'Health failed: HTTP ' + result.response.status);
    })
    .catch(function (error) {
      var message = error && error.message ? error.message : String(error);
      setHealthState('fail');
      log('local', 'Health failed: ' + message);
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
    var text = error && error.message ? error.message : String(error);
    portConnected = false;
    port = null;
    setConnectState('error');
    log('local', 'postMessage failed: ' + text);
  }
}

function setConnectState(state) {
  _connectState = state;
  refreshConnectIcon();
  refreshStatusLine();
  updateActionButtons();
}

function setHealthState(state) {
  _healthState = state;
  refreshHealthIcon();
  refreshStatusLine();
}

function setRunState(state) {
  _runState = state;
  refreshActionIcons();
  refreshStatusLine();
  updateActionButtons();
}

function refreshConnectIcon() {
  var btn = els.connectBtn;
  switch (_connectState) {
    case 'connected':
    case 'running':
      btn.innerHTML = ICON.CONNECT;
      btn.className = 'icon-btn connected';
      btn.title = 'Disconnect';
      break;
    case 'connecting':
    case 'bridge-ready':
      btn.innerHTML = ICON.CONNECT;
      btn.className = 'icon-btn connecting';
      btn.title = 'Connecting...';
      break;
    case 'paused':
      btn.innerHTML = ICON.CONNECT;
      btn.className = 'icon-btn paused';
      btn.title = 'Disconnect';
      break;
    case 'disconnected':
    default:
      btn.innerHTML = ICON.DISCONNECT;
      btn.className = 'icon-btn disconnected';
      btn.title = 'Connect';
      break;
    case 'error':
      btn.innerHTML = ICON.DISCONNECT;
      btn.className = 'icon-btn error';
      btn.title = 'Connect';
      break;
  }
}

function refreshHealthIcon() {
  var btn = els.healthBtn;
  switch (_healthState) {
    case 'ok':
      btn.innerHTML = ICON.HEALTH_OK;
      btn.className = 'icon-btn ok';
      btn.title = 'Health OK';
      break;
    case 'fail':
      btn.innerHTML = ICON.HEALTH_FAIL;
      btn.className = 'icon-btn fail';
      btn.title = 'Test Health';
      break;
    case 'testing':
      btn.innerHTML = ICON.HEALTH_TESTING;
      btn.className = 'icon-btn testing';
      btn.title = 'Testing...';
      break;
    default:
      btn.innerHTML = ICON.HEART;
      btn.className = 'icon-btn fail';
      btn.title = 'Test Health';
      break;
  }
}

function refreshActionIcons() {
  var ss = els.startStopBtn;
  var pr = els.pauseResumeBtn;
  switch (_runState) {
    case 'running':
      ss.innerHTML = ICON.STOP;
      ss.className = 'icon-btn action stop';
      ss.title = 'Stop';
      pr.innerHTML = ICON.PAUSE;
      pr.className = 'icon-btn action paused';
      pr.title = 'Pause';
      break;
    case 'paused':
      ss.innerHTML = ICON.STOP;
      ss.className = 'icon-btn action stop';
      ss.title = 'Stop';
      pr.innerHTML = ICON.RESUME;
      pr.className = 'icon-btn action resume';
      pr.title = 'Resume';
      break;
    default:
      ss.innerHTML = ICON.START;
      ss.className = 'icon-btn action start';
      ss.title = 'Start Run';
      pr.innerHTML = ICON.PAUSE;
      pr.className = 'icon-btn action paused disabled';
      pr.title = 'Pause';
      break;
  }
}

function updateActionButtons() {
  var pr = els.pauseResumeBtn;
  if (_runState === 'running') {
    pr.disabled = false;
  } else if (_runState === 'paused') {
    pr.disabled = false;
  } else {
    pr.disabled = true;
  }
}

function refreshAllIcons() {
  refreshConnectIcon();
  refreshHealthIcon();
  refreshActionIcons();
}

function refreshStatusLine() {
  var parts = [];
  var cssClass = '';

  switch (_connectState) {
    case 'connected':
      parts.push('Connected');
      cssClass = 'connected';
      break;
    case 'connecting':
      parts.push('Connecting...');
      cssClass = 'connecting';
      break;
    case 'bridge-ready':
      parts.push('Bridge ready');
      cssClass = 'bridge-ready';
      break;
    case 'error':
      parts.push('Error');
      cssClass = 'error';
      break;
    case 'paused':
      parts.push('Connected');
      cssClass = 'paused';
      break;
    case 'running':
      parts.push('Connected');
      cssClass = 'running';
      break;
    default:
      parts.push('Disconnected');
      cssClass = 'disconnected';
      break;
  }

  switch (_healthState) {
    case 'ok':
      parts.push('Health OK');
      break;
    case 'fail':
      parts.push('Health not tested');
      break;
    case 'testing':
      parts.push('Testing health...');
      break;
    default:
      parts.push('Health not tested');
      break;
  }

  switch (_runState) {
    case 'running':
      parts.push('Running');
      break;
    case 'paused':
      parts.push('Paused');
      break;
    case 'stopped':
      parts.push('Stopped');
      break;
    case 'error':
      parts.push('Error');
      break;
  }

  els.statusLine.textContent = parts.join(' \u00B7 ');
  els.statusLine.className = 'status-line ' + cssClass;
}

function log(direction, message) {
  var node = document.createElement('div');
  node.className = 'log';
  var time = new Date().toLocaleTimeString();
  node.innerHTML = '<time>' + time + '</time> <strong>' + escapeHtml(direction) + '</strong><br>' + escapeHtml(message);
  els.logs.prepend(node);
}

function renderObserved(observation) {
  var inputs = (observation.inputs || []).map(function (input) {
    return '<span class="pill">' + escapeHtml(input.selector || input.tagName) + (input.redacted ? ' · redacted' : '') + '</span>';
  }).join('');
  var buttons = (observation.buttons || []).map(function (button) {
    return '<span class="pill">' + escapeHtml(button.text || button.selector) + '</span>';
  }).join('');
  var links = (observation.links || []).slice(0, 20).map(function (link) {
    return '<span class="pill">' + escapeHtml(link.text || link.href) + '</span>';
  }).join('');
  els.observedList.innerHTML = (inputs + buttons + links) || 'No visible elements.';
  els.pageReady.textContent = observation.readyState || 'unknown';
}

function summarize(value) {
  var text = typeof value === 'string' ? value : JSON.stringify(value);
  return text.length > 500 ? text.slice(0, 500) + '...' : text;
}

function escapeHtml(value) {
  return String(value || '')
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;')
    .replace(/'/g, '&#039;');
}
