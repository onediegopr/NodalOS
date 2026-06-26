/**
 * @NODAL_OS_RESTRICTED
 * @policy no-audit, no-modify, no-analyze, no-refactor
 * ADVERTENCIA: Código protegido. Solo el propietario puede modificarlo.
 */
export class BlockDetector {
  static async detect(page, response) {
    if (response) {
      const status = response.status();
      if (status === 403) return this.buildSignal('AccessDeniedDetected', `HTTP ${status}`, `HTTP ${status} Forbidden`, status);
      if (status === 429) return this.buildSignal('RateLimitDetected', `HTTP ${status}`, `HTTP ${status} Too Many Requests`, status);
      if (status === 503) return this.buildSignal('ServiceUnavailable', `HTTP ${status}`, `HTTP ${status} Service Unavailable`, status);
    }

    const patterns = [
      [/access denied/i, 'AccessDeniedDetected'],
      [/blocked/i, 'BotBlockDetected'],
      [/too many requests/i, 'RateLimitDetected'],
      [/just a moment/i, 'BotBlockDetected'],
      [/checking your browser/i, 'BotBlockDetected'],
      [/ddos protection/i, 'BotBlockDetected'],
      [/are you a (human|bot)/i, 'BotBlockDetected'],
      [/security check/i, 'BotBlockDetected'],
    ];

    try {
      const title = (await page.title().catch(() => '')) || '';
      const url = page.url() || '';

      for (const [rx, kind] of patterns) {
        if (rx.test(title) || rx.test(url)) {
          return this.buildSignal(kind, url.substring(0, 100), title || url);
        }
      }

      const bodyText = await page.evaluate(() => document.body ? document.body.innerText : '');
      for (const [rx, kind] of patterns) {
        if (rx.test(bodyText)) {
          return this.buildSignal(kind, bodyText.substring(0, 200), 'Block page pattern matched in body text');
        }
      }
    } catch (e) { }

    return null;
  }

  static buildSignal(kind, blockPattern, reason, blockHttpCode = null) {
    return {
      kind,
      severity: 'Critical',
      source: 'stealth-block-detector',
      blockHttpCode,
      blockPattern,
      redactedEvidence: `Block detected: ${kind}`,
      reason,
      frameId: 'main',
      autoSolvable: false,
      solverRecommendation: 'none',
    };
  }
}
