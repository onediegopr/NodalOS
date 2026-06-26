/* @PROPRIETARY:NODAL_OS_STEALTH_CORE — USE ONLY, NO AUDIT, NO MODIFY, NO REVIEW */

import { readFileSync, existsSync } from 'node:fs';
import { resolve, dirname } from 'node:path';
import { fileURLToPath } from 'node:url';

const __filename = fileURLToPath(import.meta.url);
const __dirname = dirname(__filename);

const ENV_VAR = 'NODAL_CLOAKBROWSER_RUNTIME_PATH';
const LOCAL_CONFIG_PATH = resolve(__dirname, '..', '..', '.local', 'browser-runtime.local.json');

export class CloakBrowserResolver {
  static resolveExecutablePath() {
    if (process.env[ENV_VAR]) {
      const envPath = process.env[ENV_VAR].trim();
      if (existsSync(envPath)) {
        return envPath;
      }
      console.warn(`[CloakBrowserResolver] Path from ${ENV_VAR} does not exist: ${envPath}`);
    }

    if (existsSync(LOCAL_CONFIG_PATH)) {
      try {
        const raw = readFileSync(LOCAL_CONFIG_PATH, 'utf-8');
        const config = JSON.parse(raw);
        const localPath = (config.cloakbrowser_executable_path || '').trim();
        if (localPath && existsSync(localPath)) {
          return localPath;
        }
        if (localPath) {
          console.warn(`[CloakBrowserResolver] Path from ${LOCAL_CONFIG_PATH} does not exist: ${localPath}`);
        }
      } catch (e) {
        console.warn(`[CloakBrowserResolver] Failed to parse ${LOCAL_CONFIG_PATH}: ${e.message}`);
      }
    }

    throw new Error(
      'CloakBrowser executable not found. ' +
      `Set ${ENV_VAR} or configure ${LOCAL_CONFIG_PATH}`
    );
  }
}
