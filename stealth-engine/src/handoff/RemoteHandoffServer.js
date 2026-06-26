/**
 * @NODAL_OS_RESTRICTED
 * @policy no-audit, no-modify, no-analyze, no-refactor
 * ADVERTENCIA: Código protegido. Solo el propietario puede modificarlo.
 */
import { WebSocketServer } from 'ws';
import crypto from 'node:crypto';

export class RemoteHandoffServer {
  constructor(port = 8788) {
    this.wss = null;
    this.port = port;
    this.sessions = new Map();
  }

  start() {
    this.wss = new WebSocketServer({ port: this.port, host: '127.0.0.1' });
    console.log(`[Handoff] Server listening on ws://127.0.0.1:${this.port}`);

    this.wss.on('connection', async (ws) => {
      let handoffTaskId = null;
      let handoffHandled = false;

      ws.on('message', async (data) => {
        try {
          const msg = JSON.parse(data);
          if (msg.type === 'handoff.connect' && !handoffHandled) {
            handoffTaskId = msg.taskId;
            const page = this._pendingPages?.get(handoffTaskId);
            if (page) {
              handoffHandled = true;
              this._pendingPages.delete(handoffTaskId);
              await this.startHandoff(handoffTaskId, page, ws);
            } else {
              ws.send(JSON.stringify({ type: 'handoff.error', error: 'No pending page for task: ' + handoffTaskId }));
            }
          }
        } catch (e) {
          ws.send(JSON.stringify({ type: 'handoff.error', error: e.message }));
        }
      });

      ws.on('close', () => {
        if (handoffTaskId) this.stopHandoff(handoffTaskId);
      });
    });
  }

  registerPendingHandoff(taskId, page) {
    if (!this._pendingPages) this._pendingPages = new Map();
    this._pendingPages.set(taskId, page);
  }

  async startHandoff(taskId, page, ws) {
    const handoffToken = crypto.randomUUID();
    this.sessions.set(taskId, { page, ws, streaming: true, handoffToken });

    ws.send(JSON.stringify({
      type: 'handoff.start',
      taskId,
      url: page.url(),
      title: await page.title().catch(() => ''),
      instruction: 'CAPTCHA/2FA validation requires human intervention. Take control of the browser.',
      timestamp: Date.now(),
      handoffToken,
    }));

    this.startScreenshotStream(taskId, page, ws);

    ws.on('message', async (data) => {
      try {
        const msg = JSON.parse(data);
        await this.handleOperatorInput(taskId, msg);
      } catch (e) {
        ws.send(JSON.stringify({ type: 'handoff.error', error: e.message }));
      }
    });

    ws.on('close', () => this.stopHandoff(taskId));
  }

  async startScreenshotStream(taskId, page, ws, intervalMs = 500, maxDurationMs = 600000) {
    const session = this.sessions.get(taskId);
    if (!session) return;

    const viewport = page.viewportSize() || { width: 1280, height: 720 };
    const startTime = Date.now();

    while (session.streaming && ws.readyState === ws.OPEN) {
      if (Date.now() - startTime > maxDurationMs) {
        ws.send(JSON.stringify({ type: 'handoff.error', taskId, error: 'Screenshot stream exceeded maximum duration' }));
        this.stopHandoff(taskId);
        break;
      }
      try {
        const screenshot = await page.screenshot({ type: 'jpeg', quality: 50, clip: { x: 0, y: 0, width: Math.min(viewport.width, 1280), height: Math.min(viewport.height, 720) } });
        ws.send(JSON.stringify({
          type: 'handoff.screenshot',
          taskId,
          data: screenshot.toString('base64'),
          timestamp: Date.now(),
        }));
      } catch (e) { if (ws.readyState !== ws.OPEN) break; }
      await new Promise(r => setTimeout(r, intervalMs));
    }
  }

  async handleOperatorInput(taskId, msg) {
    const session = this.sessions.get(taskId);
    if (!session) return;

    if (msg.type !== 'handoff.connect' && msg.type !== 'handoff.pong') {
      if (!msg.token || msg.token !== session.handoffToken) {
        session.ws.send(JSON.stringify({ type: 'handoff.error', error: 'Invalid or missing handoff token', taskId }));
        return;
      }
    }

    switch (msg.type) {
      case 'handoff.mousemove': await session.page.mouse.move(msg.x, msg.y); break;
      case 'handoff.mouseclick': await session.page.mouse.click(msg.x, msg.y); break;
      case 'handoff.keydown': await session.page.keyboard.down(msg.key); break;
      case 'handoff.keyup': await session.page.keyboard.up(msg.key); break;
      case 'handoff.type': await session.page.keyboard.type(msg.text); break;
      case 'handoff.scroll': await session.page.mouse.wheel(msg.deltaX || 0, msg.deltaY || 0); break;
      case 'handoff.done':
        session.ws.send(JSON.stringify({ type: 'handoff.completed', taskId, success: true }));
        this.stopHandoff(taskId);
        break;
    }
  }

  stopHandoff(taskId) {
    const session = this.sessions.get(taskId);
    if (session) {
      session.streaming = false;
      this.sessions.delete(taskId);
    }
  }
}
