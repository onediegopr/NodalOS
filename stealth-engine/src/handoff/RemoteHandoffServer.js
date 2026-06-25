import { WebSocketServer } from 'ws';

export class RemoteHandoffServer {
  constructor(port = 8788) {
    this.wss = null;
    this.port = port;
    this.sessions = new Map();
  }

  start() {
    this.wss = new WebSocketServer({ port: this.port, host: '127.0.0.1' });
    console.log(`[Handoff] Server listening on ws://127.0.0.1:${this.port}`);
  }

  async startHandoff(taskId, page, ws) {
    this.sessions.set(taskId, { page, ws, streaming: true });

    ws.send(JSON.stringify({
      type: 'handoff.start',
      taskId,
      url: page.url(),
      title: await page.title().catch(() => ''),
      instruction: 'CAPTCHA/2FA validation requires human intervention. Take control of the browser.',
      timestamp: Date.now(),
    }));

    this.startScreenshotStream(taskId, page, ws);

    ws.on('message', async (data) => {
      try {
        const msg = JSON.parse(data);
        await this.handleOperatorInput(taskId, page, msg);
      } catch (e) {
        ws.send(JSON.stringify({ type: 'handoff.error', error: e.message }));
      }
    });

    ws.on('close', () => this.stopHandoff(taskId));
  }

  async startScreenshotStream(taskId, page, ws, intervalMs = 500) {
    const session = this.sessions.get(taskId);
    if (!session) return;

    while (session.streaming && ws.readyState === ws.OPEN) {
      try {
        const screenshot = await page.screenshot({ type: 'jpeg', quality: 50, clip: { x: 0, y: 0, width: Math.min(page.viewportSize().width, 1280), height: Math.min(page.viewportSize().height, 720) } });
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

  async handleOperatorInput(taskId, page, msg) {
    const session = this.sessions.get(taskId);
    if (!session) return;

    switch (msg.type) {
      case 'handoff.mousemove': await page.mouse.move(msg.x, msg.y); break;
      case 'handoff.mouseclick': await page.mouse.click(msg.x, msg.y); break;
      case 'handoff.keydown': await page.keyboard.down(msg.key); break;
      case 'handoff.keyup': await page.keyboard.up(msg.key); break;
      case 'handoff.type': await page.keyboard.type(msg.text); break;
      case 'handoff.scroll': await page.mouse.wheel(msg.deltaX || 0, msg.deltaY || 0); break;
      case 'handoff.done':
        session.ws.send(JSON.stringify({ type: 'handoff.completed', taskId, success: true }));
        break;
    }
  }

  stopHandoff(taskId) {
    const session = this.sessions.get(taskId);
    if (session) { session.streaming = false; this.sessions.delete(taskId); }
  }
}
