/**
 * Runtime deploy config (copied from public/env.json into the build output).
 * Edit env.json next to index.html on staging/production — no rebuild required.
 */
export interface AppEnv {
  /** API origin, no trailing slash. Use "" for same-origin (reverse proxy). */
  apiBaseUrl: string;
}

export const DEFAULT_APP_ENV: AppEnv = {
  apiBaseUrl: 'http://localhost:5172',
};
