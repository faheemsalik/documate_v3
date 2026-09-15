import { inject } from '@angular/core';
import { AppConfigService } from './app-config.service';

/**
 * Resolves API origin from runtime env.json (via AppConfigService).
 * Must be called in an Angular injection context only
 * (constructor / field initializer / factory) — never inside methods like login().
 */
export function apiBaseUrl(): string {
  return inject(AppConfigService).apiBaseUrl;
}
