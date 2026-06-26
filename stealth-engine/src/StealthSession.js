import { chromium as vanillaChromium } from 'playwright';
import { CloakBrowserResolver } from './runtime/CloakBrowserResolver.js';
import { BehaviorProfile } from './behavior/BehaviorProfile.js';
import { AdaptiveBehaviorEngine } from './behavior/AdaptiveBehaviorEngine.js';
import { HumanMouse } from './behavior/HumanMouse.js';
import { HumanKeyboard } from './behavior/HumanKeyboard.js';
import { HumanScroll } from './behavior/HumanScroll.js';
import { HumanNavigation } from './behavior/HumanNavigation.js';

let chromium = vanillaChromium;
let stealthPlugin = null;
try {
  const { chromium: extraChromium } = await import('playwright-extra');
  const { default: StealthPlugin } = await import('puppeteer-extra-plugin-stealth');
  chromium = extraChromium;
  stealthPlugin = StealthPlugin();
} catch {
  // playwright-extra/puppeteer-extra-plugin-stealth no instalado; se usa Playwright vanilla
}

export class StealthSession {
  constructor(config) {
    this.taskId = config.taskId;
    this.instruction = config.instruction;
    this.profile = config.profile;
    this.proxy = config.proxy;
    this.proxyId = config.proxyId || null;
    this.behaviorName = config.behaviorProfile || 'casual';
    this.tlsFingerprint = config.tlsFingerprint || { enabled: false };
    this.browser = null;
    this.context = null;
    this.page = null;
    this.behavior = null;
  }

  async initialize() {
    const bp = new BehaviorProfile(this.behaviorName);
    const adaptive = new AdaptiveBehaviorEngine(bp, {
      adaptive: true,
      profileVariance: 0.2,
    });
    const ap = adaptive.getProfile();
    this.behavior = {
      profile: ap,
      mouse: new HumanMouse(ap),
      keyboard: new HumanKeyboard(ap),
      scroll: new HumanScroll(ap),
      navigation: null,
    };
    this.behavior.navigation = new HumanNavigation(this.behavior.mouse);

    const executablePath = CloakBrowserResolver.resolveExecutablePath();

    const launchArgs = this.buildLaunchArgs();

    if (this.tlsFingerprint.enabled && stealthPlugin && chromium.use) {
      try {
        chromium.use(stealthPlugin);
      } catch (e) {
        console.warn('[StealthSession] Could not enable stealth plugin:', e.message);
      }
    }

    this.browser = await chromium.launch({
      headless: false,
      executablePath,
      args: launchArgs,
      ignoreDefaultArgs: [
        '--enable-automation',
        '--disable-component-extensions-with-background-pages',
      ],
    });

    const ctxOpts = {
      viewport: this.profile.viewport,
      deviceScaleFactor: this.profile.deviceScaleFactor || 1,
      isMobile: this.profile.isMobile || false,
      hasTouch: this.profile.hasTouch || false,
      userAgent: this.profile.userAgent,
      locale: this.profile.locale,
      timezoneId: this.profile.timezone,
      geolocation: this.profile.geolocation,
      permissions: this.profile.permissions || [],
      bypassCSP: true,
      ignoreHTTPSErrors: true,
    };

    if (this.proxy && this.proxy.server) {
      ctxOpts.proxy = {
        server: this.proxy.server,
        username: this.proxy.username || undefined,
        password: this.proxy.password || undefined,
      };
    }

    this.context = await this.browser.newContext(ctxOpts);

    this.page = await this.context.newPage();
    return this;
  }

  buildLaunchArgs() {
    const args = [
      '--no-sandbox',
      '--disable-setuid-sandbox',
      '--disable-infobars',
      '--disable-blink-features=AutomationControlled',
      '--disable-features=IsolateOrigins,site-per-process',
      '--no-first-run',
      '--no-default-browser-check',
      '--disable-background-networking',
      '--disable-sync',
      '--disable-default-apps',
      '--hide-scrollbars',
      '--mute-audio',
      '--disable-hang-monitor',
      '--disable-prompt-on-repost',
      '--disable-client-side-phishing-detection',
      '--disable-component-update',
      `--window-size=${this.profile.viewport.width},${this.profile.viewport.height}`,
    ];

    if (this.proxy && this.proxy.server) {
      args.push(`--proxy-server=${this.proxy.server}`);
    }

    return args;
  }

  async navigate(url) {
    return this.page.goto(url, { waitUntil: 'domcontentloaded', timeout: 45000 });
  }

  async dispose() {
    if (this.context) await this.context.close().catch(() => { });
    if (this.browser) await this.browser.close().catch(() => { });
  }
}
