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

  async load(): Promise<void> {
    try {
      const res = await fetch('env.json', { cache: 'no-store' });
      if (!res.ok) {
        console.warn(`[AppConfig] env.json HTTP ${res.status}; using defaults.`);
        return;
      }
      const parsed = (await res.json()) as Partial<AppEnv>;
      this.env = {
        apiBaseUrl:
          typeof parsed.apiBaseUrl === 'string' ? parsed.apiBaseUrl.trim() : DEFAULT_APP_ENV.apiBaseUrl,
      };
    } catch (err) {
      console.warn('[AppConfig] Failed to load env.json; using defaults.', err);
    }
  }
}
