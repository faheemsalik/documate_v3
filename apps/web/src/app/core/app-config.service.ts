import { Injectable } from '@angular/core';
import { DEFAULT_APP_ENV, type AppEnv } from './app-env';

/**
 * Loads public/env.json at startup so one compiled build can point at
 * staging or production API URLs by swapping that file at deploy time.
 */
@Injectable({ providedIn: 'root' })
export class AppConfigService {
  private env: AppEnv = { ...DEFAULT_APP_ENV };

  /** API origin without trailing slash. */
  get apiBaseUrl(): string {
    return this.env.apiBaseUrl.replace(/\/$/, '');
  }

  /** `interim` (InterimFeGate) or `iden` (SPA → Iden). */
  get authMode(): 'interim' | 'iden' {
    return this.env.authMode === 'iden' ? 'iden' : 'interim';
  }

  /** Iden API origin without trailing slash. */
  get idenBaseUrl(): string {
    return (this.env.idenBaseUrl ?? '').replace(/\/$/, '');
  }

  get idenSoftwareKey(): string {
    return this.env.idenSoftwareKey?.trim() || 'documate';
  }

  /** True only when env authMode=iden and idenBaseUrl is set. */
  get useIdenLogin(): boolean {
    return this.authMode === 'iden' && this.idenBaseUrl.length > 0;
  }

  async load(): Promise<void> {
    try {
      const res = await fetch('env.json', { cache: 'no-store' });
      if (!res.ok) {
        console.warn(`[AppConfig] env.json HTTP ${res.status}; using defaults.`);
        return;
      }
      const parsed = (await res.json()) as Partial<AppEnv>;
      const authModeRaw = typeof parsed.authMode === 'string' ? parsed.authMode.trim().toLowerCase() : '';
      this.env = {
        apiBaseUrl:
          typeof parsed.apiBaseUrl === 'string' ? parsed.apiBaseUrl.trim() : DEFAULT_APP_ENV.apiBaseUrl,
        authMode: authModeRaw === 'iden' ? 'iden' : 'interim',
        idenBaseUrl:
          typeof parsed.idenBaseUrl === 'string' ? parsed.idenBaseUrl.trim() : DEFAULT_APP_ENV.idenBaseUrl,
        idenSoftwareKey:
          typeof parsed.idenSoftwareKey === 'string'
            ? parsed.idenSoftwareKey.trim()
            : DEFAULT_APP_ENV.idenSoftwareKey,
      };
    } catch (err) {
      console.warn('[AppConfig] Failed to load env.json; using defaults.', err);
    }
  }
}
