/**
 * @NODAL_OS_RESTRICTED
 * @policy no-audit, no-modify, no-analyze, no-refactor
 * ADVERTENCIA: Código protegido. Solo el propietario puede modificarlo.
 */
import crypto from 'node:crypto';
import { cryptoRandom } from '../behavior/AdaptiveBehaviorEngine.js';

const COUNTRY_MAPPING = {
  'US': { timezone: 'America/New_York', locale: 'en-US', languages: ['en-US', 'en'], geolocation: { latitude: 40.71, longitude: -74.0 } },
  'AR': { timezone: 'America/Argentina/Buenos_Aires', locale: 'es-AR', languages: ['es-AR', 'es'], geolocation: { latitude: -34.6, longitude: -58.38 } },
  'GB': { timezone: 'Europe/London', locale: 'en-GB', languages: ['en-GB', 'en'], geolocation: { latitude: 51.5, longitude: -0.12 } },
  'DE': { timezone: 'Europe/Berlin', locale: 'de-DE', languages: ['de-DE', 'de', 'en'], geolocation: { latitude: 52.52, longitude: 13.4 } },
  'BR': { timezone: 'America/Sao_Paulo', locale: 'pt-BR', languages: ['pt-BR', 'pt'], geolocation: { latitude: -23.5, longitude: -46.6 } },
  'ES': { timezone: 'Europe/Madrid', locale: 'es-ES', languages: ['es-ES', 'es'], geolocation: { latitude: 40.4, longitude: -3.7 } },
  'JP': { timezone: 'Asia/Tokyo', locale: 'ja-JP', languages: ['ja-JP', 'ja'], geolocation: { latitude: 35.68, longitude: 139.65 } },
  'FR': { timezone: 'Europe/Paris', locale: 'fr-FR', languages: ['fr-FR', 'fr', 'en'], geolocation: { latitude: 48.86, longitude: 2.35 } },
  'MX': { timezone: 'America/Mexico_City', locale: 'es-MX', languages: ['es-MX', 'es'], geolocation: { latitude: 19.43, longitude: -99.13 } },
  'AU': { timezone: 'Australia/Sydney', locale: 'en-AU', languages: ['en-AU', 'en'], geolocation: { latitude: -33.87, longitude: 151.21 } },
  'CA': { timezone: 'America/Toronto', locale: 'en-CA', languages: ['en-CA', 'en', 'fr-CA'], geolocation: { latitude: 43.65, longitude: -79.38 } },
};

const UA_POOL = {
  'desktop-win-chrome': [
    'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36',
    'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/130.0.0.0 Safari/537.36',
    'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/129.0.0.0 Safari/537.36',
  ],
  'desktop-mac-chrome': [
    'Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36',
    'Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/130.0.0.0 Safari/537.36',
  ],
  'desktop-linux-chrome': [
    'Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36',
  ],
  'mobile-android-chrome': [
    'Mozilla/5.0 (Linux; Android 14; Pixel 8 Pro Build/AP2A.240905.003) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.6778.135 Mobile Safari/537.36',
    'Mozilla/5.0 (Linux; Android 14; SM-S928B Build/UP1A.231005.007) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.6778.135 Mobile Safari/537.36',
  ],
};

const WEBGL_PAIRS = [
  { vendor: 'Google Inc. (Intel)', renderer: 'ANGLE (Intel, Intel(R) UHD Graphics 620 Direct3D11 vs_5_0 ps_5_0, D3D11)' },
  { vendor: 'Google Inc. (NVIDIA)', renderer: 'ANGLE (NVIDIA, NVIDIA GeForce RTX 3060 Direct3D11 vs_5_0 ps_5_0, D3D11)' },
  { vendor: 'Google Inc. (NVIDIA)', renderer: 'ANGLE (NVIDIA, NVIDIA GeForce RTX 4060 Direct3D11 vs_5_0 ps_5_0, D3D11)' },
  { vendor: 'Google Inc. (AMD)', renderer: 'ANGLE (AMD, AMD Radeon RX 580 Direct3D11 vs_5_0 ps_5_0, D3D11)' },
  { vendor: 'Google Inc. (AMD)', renderer: 'ANGLE (AMD, AMD Radeon RX 6800 XT Direct3D11 vs_5_0 ps_5_0, D3D11)' },
  { vendor: 'Google Inc. (Intel)', renderer: 'ANGLE (Intel, Intel(R) Iris(R) Xe Graphics Direct3D11 vs_5_0 ps_5_0, D3D11)' },
  { vendor: 'Google Inc. (Apple)', renderer: 'ANGLE (Apple, Apple M1 Pro, OpenGL 4.1)' },
  { vendor: 'Google Inc. (Apple)', renderer: 'ANGLE (Apple, Apple M2, OpenGL 4.1)' },
];

const WEBGL_EXTENSIONS = [
  'ANGLE_instanced_arrays', 'EXT_blend_minmax', 'EXT_color_buffer_half_float',
  'EXT_disjoint_timer_query', 'EXT_float_blend', 'EXT_frag_depth',
  'EXT_shader_texture_lod', 'EXT_texture_compression_bptc',
  'EXT_texture_compression_rgtc', 'EXT_texture_filter_anisotropic',
  'OES_element_index_uint', 'OES_fbo_render_mipmap', 'OES_standard_derivatives',
  'OES_texture_float', 'OES_texture_float_linear', 'OES_texture_half_float',
  'OES_texture_half_float_linear', 'OES_vertex_array_object', 'WEBGL_color_buffer_float',
  'WEBGL_compressed_texture_s3tc', 'WEBGL_depth_texture', 'WEBGL_draw_buffers',
  'WEBGL_multi_draw',
];

const FONTS_WIN = [
  'Arial', 'Arial Black', 'Calibri', 'Cambria', 'Cambria Math', 'Candara',
  'Comic Sans MS', 'Consolas', 'Constantia', 'Corbel', 'Courier New',
  'Georgia', 'Impact', 'Lucida Console', 'Lucida Sans Unicode',
  'Microsoft Sans Serif', 'MS Gothic', 'MS PGothic', 'Palatino Linotype',
  'Segoe UI', 'Segoe UI Light', 'Segoe UI Semibold', 'Segoe UI Symbol',
  'Tahoma', 'Times New Roman', 'Trebuchet MS', 'Verdana', 'Webdings',
  'Wingdings', 'Dubai', 'Franklin Gothic',
];

const FONTS_MAC = [
  'American Typewriter', 'Andale Mono', 'Apple Chancery', 'Arial',
  'Arial Black', 'Arial Narrow', 'Arial Rounded MT Bold', 'Baskerville',
  'Big Caslon', 'Brush Script MT', 'Chalkboard', 'Cochin', 'Comic Sans MS',
  'Copperplate', 'Courier', 'Courier New', 'Didot', 'Futura', 'Geneva',
  'Georgia', 'Gill Sans', 'Helvetica', 'Helvetica Neue', 'Herculanum',
  'Hoefler Text', 'Impact', 'Lucida Grande', 'Marker Felt', 'Monaco',
  'Optima', 'Palatino', 'Papyrus', 'Skia', 'Tahoma', 'Times',
  'Times New Roman', 'Trebuchet MS', 'Verdana', 'Zapfino', 'SF Pro',
];

const PLUGINS_DESKTOP = [
  { name: 'Chrome PDF Plugin', filename: 'internal-pdf-viewer', description: 'Portable Document Format' },
  { name: 'Chrome PDF Viewer', filename: 'mhjfbmdgcfjbbpaeojofohoefgiehjai', description: '' },
  { name: 'Native Client', filename: 'internal-nacl-plugin', description: '' },
];

const MIME_TYPES = [
  { type: 'application/pdf', suffixes: 'pdf' },
  { type: 'text/pdf', suffixes: 'pdf' },
];

export class FingerprintProfile {
  constructor(data = {}) {
    this.profileId = data.profileId || crypto.randomUUID();
    this.deviceType = data.deviceType || 'desktop';
    this.os = data.os || 'Windows';
    this.browser = data.browser || 'Chrome';
    this.viewport = data.viewport || { width: 1920, height: 1040 };
    this.deviceScaleFactor = data.deviceScaleFactor ?? 1;
    this.isMobile = data.isMobile || false;
    this.hasTouch = data.hasTouch || false;
    this.maxTouchPoints = data.maxTouchPoints ?? 0;
    this.userAgent = data.userAgent || UA_POOL['desktop-win-chrome'][0];
    this.platform = data.platform || 'Win32';
    this.languages = data.languages || ['en-US', 'en'];
    this.hardwareConcurrency = data.hardwareConcurrency || 8;
    this.deviceMemory = data.deviceMemory || 8;
    this.plugins = data.plugins || PLUGINS_DESKTOP;
    this.mimeTypes = data.mimeTypes || MIME_TYPES;
    this.webglVendor = data.webglVendor || 'Google Inc. (Intel)';
    this.webglRenderer = data.webglRenderer || 'ANGLE (Intel, Intel(R) UHD Graphics 620 Direct3D11 vs_5_0 ps_5_0, D3D11)';
    this.webglExtensions = data.webglExtensions || WEBGL_EXTENSIONS;
    this.timezone = data.timezone || 'America/New_York';
    this.locale = data.locale || 'en-US';
    this.geolocation = data.geolocation || { latitude: 40.71, longitude: -74.0 };
    this.country = data.country || 'US';
    this.permissions = data.permissions || ['geolocation', 'notifications'];
    this.screen = data.screen || { width: 1920, height: 1080, availWidth: 1920, availHeight: 1040, colorDepth: 24, pixelDepth: 24 };
    this.fonts = data.fonts || (data.os === 'macOS' ? FONTS_MAC : FONTS_WIN);
    this.audioNoiseFactor = data.audioNoiseFactor ?? (cryptoRandom() * 0.0003 + 0.00005);
    this.canvasNoiseFactor = data.canvasNoiseFactor ?? (cryptoRandom() * 0.0002 + 0.00003);
    this.colorScheme = data.colorScheme || 'light';
    this.reducedMotion = data.reducedMotion || 'no-preference';
  }

  static ensureCoherence(profile, proxyCountry) {
    const mapping = COUNTRY_MAPPING[proxyCountry];
    if (!mapping) return profile;
    return new FingerprintProfile({ ...profile, ...mapping, country: proxyCountry });
  }
}

const pick = (arr) => arr[Math.floor(cryptoRandom() * arr.length)];

export class FingerprintGenerator {
  static generate(options = {}) {
    const dt = options.deviceType || 'desktop';
    const os = options.os || 'Windows';
    const presetKey = options.preset || `${dt}-${os.toLowerCase()}-chrome`;
    const uaList = UA_POOL[presetKey] || UA_POOL['desktop-win-chrome'];
    const webgl = pick(WEBGL_PAIRS);
    const isMobile = dt === 'mobile' || dt === 'tablet';

    const viewports = isMobile
      ? pick([{ width: 412, height: 915 }, { width: 393, height: 852 }, { width: 414, height: 896 }])
      : pick([{ width: 1920, height: 1040 }, { width: 1366, height: 768 }, { width: 2560, height: 1440 }]);

    const fonts = os === 'macOS' ? FONTS_MAC : FONTS_WIN;
    const concurrencies = isMobile ? [4, 8] : [4, 8, 12, 16];
    const memories = isMobile ? [2, 4, 6, 8] : [4, 8, 16, 32];

    return new FingerprintProfile({
      profileId: crypto.randomUUID(),
      deviceType: dt,
      os,
      browser: 'Chrome',
      viewport: viewports,
      screen: {
        width: viewports.width,
        height: viewports.height + (isMobile ? 40 : 100),
        availWidth: viewports.width,
        availHeight: viewports.height,
        colorDepth: 24,
        pixelDepth: 24,
      },
      deviceScaleFactor: isMobile ? pick([2, 3, 3.5]) : pick([1, 1.25, 1.5]),
      isMobile,
      hasTouch: isMobile,
      maxTouchPoints: isMobile ? 5 : 0,
      userAgent: pick(uaList),
      platform: os === 'Windows' ? 'Win32' : os === 'macOS' ? 'MacIntel' : os === 'Android' ? 'Linux armv8l' : 'Linux x86_64',
      languages: options.languages || (isMobile ? ['en-US', 'en'] : ['en-US', 'en']),
      hardwareConcurrency: pick(concurrencies),
      deviceMemory: pick(memories),
      plugins: PLUGINS_DESKTOP,
      mimeTypes: MIME_TYPES,
      webglVendor: webgl.vendor,
      webglRenderer: webgl.renderer,
      webglExtensions: WEBGL_EXTENSIONS,
      timezone: options.timezone || 'America/New_York',
      locale: options.locale || 'en-US',
      geolocation: options.geolocation || { latitude: 40.71, longitude: -74.0 },
      country: options.country || 'US',
      permissions: ['geolocation', 'notifications'],
      fonts,
      audioNoiseFactor: cryptoRandom() * 0.0003 + 0.00005,
      canvasNoiseFactor: cryptoRandom() * 0.0002 + 0.00003,
    });
  }

  static loadProfile(data) {
    return new FingerprintProfile(data);
  }
}
