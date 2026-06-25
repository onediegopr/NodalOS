export class FingerprintInjector {
  static getFullInitScript(profile) {
    const p = profile;
    return () => {

      // ── Navigator ──
      Object.defineProperty(Navigator.prototype, 'webdriver',
        { get: () => undefined, configurable: true, enumerable: true });
      Object.defineProperty(Navigator.prototype, 'vendor',
        { get: () => 'Google Inc.', configurable: true });
      Object.defineProperty(Navigator.prototype, 'vendorSub',
        { get: () => '', configurable: true });
      Object.defineProperty(Navigator.prototype, 'platform',
        { get: () => p.platform, configurable: true });
      Object.defineProperty(Navigator.prototype, 'maxTouchPoints',
        { get: () => p.maxTouchPoints, configurable: true });
      Object.defineProperty(Navigator.prototype, 'hardwareConcurrency',
        { get: () => p.hardwareConcurrency, configurable: true });
      Object.defineProperty(Navigator.prototype, 'deviceMemory',
        { get: () => p.deviceMemory, configurable: true });
      if (navigator.languages) {
        Object.defineProperty(Navigator.prototype, 'languages',
          { get: () => Object.freeze([...p.languages]), configurable: true });
        Object.defineProperty(Navigator.prototype, 'language',
          { get: () => p.languages[0], configurable: true });
      }

      // ── Plugins ──
      const pluginArr = Object.create(Array.prototype);
      pluginArr.length = p.plugins.length;
      pluginArr.item = function(i) { return this[i] || null; };
      pluginArr.namedItem = function(n) { for (let i=0;i<this.length;i++) if (this[i].name===n) return this[i]; return null; };
      pluginArr.refresh = function() {};
      p.plugins.forEach((pl, i) => {
        pluginArr[i] = { name: pl.name, filename: pl.filename, description: pl.description, length: 1, item: () => null, namedItem: () => null };
      });
      Object.defineProperty(Navigator.prototype, 'plugins', { get: () => pluginArr, configurable: true });

      const mimeArr = Object.create(Array.prototype);
      mimeArr.length = p.mimeTypes.length;
      mimeArr.item = function(i) { return this[i] || null; };
      mimeArr.namedItem = function(n) { for (let i=0;i<this.length;i++) if (this[i].type===n) return this[i]; return null; };
      p.mimeTypes.forEach((mt, i) => {
        mimeArr[i] = { type: mt.type, suffixes: mt.suffixes, description: '', enabledPlugin: pluginArr[0] || null };
      });
      Object.defineProperty(Navigator.prototype, 'mimeTypes', { get: () => mimeArr, configurable: true });

      // ── Screen ──
      const scr = p.screen;
      ['width','height','availWidth','availHeight','colorDepth','pixelDepth'].forEach(k => {
        Object.defineProperty(Screen.prototype, k, { get: () => scr[k], configurable: true });
      });

      // ── WebGL ──
      const origGetParam = WebGLRenderingContext.prototype.getParameter;
      WebGLRenderingContext.prototype.getParameter = function(param) {
        if (param === 37445) return p.webglVendor;
        if (param === 37446) return p.webglRenderer;
        return origGetParam.call(this, param);
      };
      if (WebGL2RenderingContext) {
        WebGL2RenderingContext.prototype.getParameter = function(param) {
          if (param === 37445) return p.webglVendor;
          if (param === 37446) return p.webglRenderer;
          return origGetParam.call(this, param);
        };
      }

      const origGetExt = WebGLRenderingContext.prototype.getExtension;
      WebGLRenderingContext.prototype.getExtension = function(name) {
        if (name === 'WEBGL_debug_renderer_info') return {
          UNMASKED_VENDOR_WEBGL: 37445,
          UNMASKED_RENDERER_WEBGL: 37446,
        };
        return origGetExt.call(this, name);
      };

      const origGetSupported = WebGLRenderingContext.prototype.getSupportedExtensions;
      WebGLRenderingContext.prototype.getSupportedExtensions = function() {
        const exts = origGetSupported.call(this) || [];
        return [...new Set([...exts, ...p.webglExtensions])];
      };

      // ── Canvas noise ──
      const noiseCanvas = (canvas) => {
        const getCtx = canvas.getContext;
        canvas.getContext = function(type, ...args) {
          const ctx = getCtx.call(this, type, ...args);
          if (type === '2d' && ctx.getImageData) {
            const origGI = ctx.getImageData;
            ctx.getImageData = function(x, y, w, h) {
              const d = origGI.call(this, x, y, w, h);
              if (d && d.data && d.data.length > 4) d.data[~~(Math.random() * (d.data.length - 4))] ^= 1;
              return d;
            };
            const origTD = canvas.toDataURL;
            canvas.toDataURL = function(...dargs) {
              if (ctx.getImageData) {
                try {
                  const img = ctx.getImageData(0, 0, canvas.width || 1, canvas.height || 1);
                  img.data[~~(Math.random() * (img.data.length - 4))] ^= 1;
                  ctx.putImageData(img, 0, 0);
                } catch(e) {}
              }
              return origTD.apply(this, dargs);
            };
          }
          return ctx;
        };
      };
      try {
        const origCE = document.createElement;
        document.createElement = function(tag, ...args) {
          const el = origCE.call(this, tag, ...args);
          if (tag.toLowerCase() === 'canvas') noiseCanvas(el);
          return el;
        };
        document.querySelectorAll('canvas').forEach(noiseCanvas);
      } catch(e) {}

      // ── AudioContext noise ──
      const Ctx = window.AudioContext || window.webkitAudioContext;
      if (Ctx) {
        const origOsc = Ctx.prototype.createOscillator;
        Ctx.prototype.createOscillator = function() {
          const osc = origOsc.call(this);
          const origCn = osc.connect;
          osc.connect = function(dest) {
            try {
              const g = this.context.createGain();
              g.gain.value = 1 + (Math.random() - 0.5) * p.audioNoiseFactor * 2;
              origCn.call(this, g);
              g.connect(dest);
              return g;
            } catch(e) {}
            return origCn.call(this, dest);
          };
          return osc;
        };
        const origAn = Ctx.prototype.createAnalyser;
        Ctx.prototype.createAnalyser = function() {
          const an = origAn.call(this);
          const origBF = an.getByteFrequencyData;
          an.getByteFrequencyData = function(arr) {
            origBF.call(this, arr);
            for (let i = 0; i < Math.min(10, arr.length); i++) arr[i] += Math.random() > 0.5 ? 1 : -1;
          };
          return an;
        };
      }

      // ── WebRTC block ──
      if (window.RTCPeerConnection) {
        const origRTC = window.RTCPeerConnection;
        window.RTCPeerConnection = function(cfg) {
          if (cfg && cfg.iceServers) cfg.iceServers = cfg.iceServers.map(s => ({ ...s, urls: [] }));
          return new origRTC(cfg);
        };
        window.RTCPeerConnection.prototype = origRTC.prototype;
      }
      if (navigator.mediaDevices && navigator.mediaDevices.getUserMedia) {
        navigator.mediaDevices.getUserMedia = () => Promise.reject(new Error('NotAllowedError'));
      }

      // ── Permissions ──
      if (navigator.permissions) {
        const origQ = navigator.permissions.query;
        navigator.permissions.query = function(desc) {
          if (desc && desc.name === 'notifications')
            return Promise.resolve({ state: 'prompt', onchange: null, addEventListener: () => {}, removeEventListener: () => {} });
          return origQ.call(this, desc);
        };
      }

      // ── Chrome runtime ──
      if (!window.chrome) {
        window.chrome = {
          runtime: {
            connect: () => ({ onDisconnect: { addListener: () => {}, disconnect: () => {} } }),
            sendMessage: () => {},
            getManifest: () => ({}),
            getURL: p => 'chrome-extension://' + p,
            id: undefined,
            PlatformOs: { MAC: 'mac', WIN: 'win', ANDROID: 'android', CROS: 'cros', LINUX: 'linux' },
            OnInstalledReason: { CHROME_UPDATE: 'chrome_update', UPDATE: 'update', INSTALL: 'install' },
          },
          loadTimes: () => ({
            requestTime: Date.now() / 1000 - 0.5,
            startLoadTime: Date.now() / 1000 - 0.4,
            finishLoadTime: Date.now() / 1000,
            navigationType: 'Other',
            wasFetchedViaSpdy: true,
            connectionInfo: 'http/1.1',
          }),
          csi: () => ({ startE: Date.now() - 200, onloadT: Date.now() - 100, pageT: 200 + Math.floor(Math.random() * 500), tran: 15 }),
          app: { isInstalled: false, InstallState: { DISABLED: 'disabled', INSTALLED: 'installed', NOT_INSTALLED: 'not_installed' }, RunningState: { CANNOT_RUN: 'cannot_run', READY_TO_RUN: 'ready_to_run', RUNNING: 'running' } },
        };
      }

      // ── Notification ──
      if (typeof Notification !== 'undefined') {
        Object.defineProperty(Notification, 'permission', { get: () => 'default', configurable: true });
      }

      // ── Cleanup automation props ──
      const badKeys = [
        '__playwright', '__playwright__binding__', '__pw_manual__', '__PW_inspect__',
        '__nightmare', '_phantom', 'callPhantom', '_selenium', 'webdriver', 'domAutomation',
        '__webdriver_evaluate', '__webdriver_script_function', '__webdriver_script_func',
        '__fxdriver_evaluate', '__driver_unwrapped', '__webdriver_unwrapped',
        '__lastWatirAlert', '__lastWatirConfirm', '__lastWatirPrompt',
        'callSelenium', '_Selenium_IDE_Recorder',
      ];
      badKeys.forEach(k => { try { delete window[k]; } catch(e) {} });

      // ── CDP bypass ──
      let protoAccess = 0;
      const origDesc = Object.getOwnPropertyDescriptor(Object.prototype, '__proto__');
      if (origDesc) {
        Object.defineProperty(Object.prototype, '__proto__', {
          get() { protoAccess++; if (protoAccess > 1000) return {}; return origDesc.get.call(this); },
          set(v) { return origDesc.set.call(this, v); },
          configurable: true,
        });
      }
    };
  }
}
