/**
 * Runtime deploy config (copied from public/env.json into the build output).
 * Edit env.json next to index.html on staging/production — no rebuild required.
 */
export interface AppEnv {
  /** API origin, no trailing slash. Use "" for same-origin (reverse proxy). */
  apiBaseUrl: string;
  /**
   * Customer login mode:
   * - `interim` — Documate InterimFeGate (localhost / DevBypass). Ignore idenBaseUrl for login.
   * - `iden` — SPA authenticates against Iden (`idenBaseUrl` required).
   */
  authMode?: 'interim' | 'iden';
  /**
   * Iden API origin (no trailing slash), e.g. http://localhost:5301/api/v1.
   * Used only when authMode=iden.
   */
  idenBaseUrl?: string;
  /** Software key / client hint for Iden login. */
  idenSoftwareKey?: string;
}

export const DEFAULT_APP_ENV: AppEnv = {
  apiBaseUrl: 'http://localhost:5172',
  authMode: 'interim',
  idenBaseUrl: '',
  idenSoftwareKey: 'documate',
};
