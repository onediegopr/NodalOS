const params = new URLSearchParams(location.search);
const handoffId = params.get('handoffId') || 'default';
const bridgeHost = params.get('host') || location.hostname;
const bridgePort = params.get('port') || '8788';

let ws = null;
let canvas = document.getElementById('viewport');
let ctx = canvas.getContext('2d');
let statusEl = document.getElementById('status');
let infoEl = document.getElementById('info');
let btnContinue = document.getElementById('btn-continue');

function connect() {
  const url = `ws://${bridgeHost}:${bridgePort}/handoff/${handoffId}`;
  ws = new WebSocket(url);

  ws.onopen = () => {
    statusEl.textContent = 'Connected';
    statusEl.className = 'status connected';
    infoEl.textContent = 'Handoff session active. You can control the browser below.';
    infoEl.className = 'info';
    btnContinue.disabled = false;
  };

  ws.onmessage = (event) => {
    try {
      const msg = JSON.parse(event.data);
      handleMessage(msg);
    } catch (e) {
      console.error('Invalid message:', e);
    }
  };

  ws.onclose = () => {
    statusEl.textContent = 'Disconnected';
    statusEl.className = 'status waiting';
    infoEl.textContent = 'Handoff session ended.';
    btnContinue.disabled = true;
  };

  ws.onerror = () => {
    statusEl.textContent = 'Error';
    statusEl.className = 'status waiting';
    infoEl.textContent = 'Connection error. Try refreshing.';
  };
}

function handleMessage(msg) {
  switch (msg.type) {
    case 'handoff.start':
      infoEl.textContent = msg.instruction || 'Take control of the browser.';
      break;
    case 'handoff.screenshot':
      const img = new Image();
      img.onload = () => {
        canvas.width = img.width;
        canvas.height = img.height;
        ctx.drawImage(img, 0, 0);
      };
      img.src = 'data:image/jpeg;base64,' + msg.data;
      break;
    case 'handoff.completed':
    case 'handoff.error':
      infoEl.textContent = msg.type === 'handoff.completed'
        ? 'Handoff completed successfully.' : 'Error: ' + (msg.error || 'unknown');
      btnContinue.disabled = true;
      break;
  }
}

function sendDone() {
  if (ws && ws.readyState === WebSocket.OPEN) {
    ws.send(JSON.stringify({ type: 'handoff.done' }));
    infoEl.textContent = 'Marked as done. Waiting for verification...';
    btnContinue.disabled = true;
  }
}

function sendCancel() {
  if (ws && ws.readyState === WebSocket.OPEN) {
    ws.send(JSON.stringify({ type: 'handoff.cancel' }));
    infoEl.textContent = 'Cancelled.';
    btnContinue.disabled = true;
  }
}

canvas.addEventListener('mousemove', (e) => {
  if (!ws || ws.readyState !== WebSocket.OPEN) return;
  const rect = canvas.getBoundingClientRect();
  const scaleX = canvas.width / rect.width;
  const scaleY = canvas.height / rect.height;
  ws.send(JSON.stringify({ type: 'handoff.mousemove', x: Math.round(e.offsetX * scaleX), y: Math.round(e.offsetY * scaleY) }));
});

canvas.addEventListener('click', (e) => {
  if (!ws || ws.readyState !== WebSocket.OPEN) return;
  const rect = canvas.getBoundingClientRect();
  const scaleX = canvas.width / rect.width;
  const scaleY = canvas.height / rect.height;
  ws.send(JSON.stringify({ type: 'handoff.mouseclick', x: Math.round(e.offsetX * scaleX), y: Math.round(e.offsetY * scaleY) }));
});

canvas.addEventListener('wheel', (e) => {
  if (!ws || ws.readyState !== WebSocket.OPEN) return;
  e.preventDefault();
  ws.send(JSON.stringify({ type: 'handoff.scroll', deltaX: e.deltaX, deltaY: e.deltaY }));
});

document.addEventListener('keydown', (e) => {
  if (!ws || ws.readyState !== WebSocket.OPEN) return;
  ws.send(JSON.stringify({ type: 'handoff.keydown', key: e.key }));
});

document.addEventListener('keyup', (e) => {
  if (!ws || ws.readyState !== WebSocket.OPEN) return;
  ws.send(JSON.stringify({ type: 'handoff.keyup', key: e.key }));
});

connect();
