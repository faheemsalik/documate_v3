import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map, tap, catchError, of } from 'rxjs';
import { AppConfigService } from './app-config.service';

/** Phase 1 (J3) client auth placeholder — Band 15 replaces with live Iden. */
export interface AuthSession {
  mode: 'dev-bypass' | 'iden';
  accessToken: string;
  userId: string;
  tenantId: string;
  businessId: string;
}

interface LoginApiResponse {
  accessToken: string;
  tokenType: string;
  userId: string;
  tenantId: string;
  businessId: string;
}

/** DevBypass stand-in Iden ids — real Guids (plan: Iden Guids, not int-padded). */
const ACTIVE_BUSINESS_KEY = 'documate.activeBusinessId';
const ACCESS_TOKEN_KEY = 'documate.interimAccessToken';
const SESSION_META_KEY = 'documate.interimSessionMeta';
const DEFAULT_BUSINESS_ID = '118f4881-9313-4ebb-98cd-57217472d99f';
/** Pre-fix int-padded DevBypass business id — drop from localStorage if present. */
const LEGACY_PADDED_BUSINESS_ID = '00000000-0000-0000-0000-000000000002';

interface SessionMeta {
  userId: string;
  tenantId: string;
  businessId: string;
}

function resolveInitialBusinessId(fallback: string): string {
  const stored = localStorage.getItem(ACTIVE_BUSINESS_KEY);
  if (!stored || stored === LEGACY_PADDED_BUSINESS_ID) {
    localStorage.removeItem(ACTIVE_BUSINESS_KEY);
    return fallback || DEFAULT_BUSINESS_ID;
  }
  return stored;
}

function readStoredSession(): AuthSession | null {
  const accessToken = sessionStorage.getItem(ACCESS_TOKEN_KEY);
  const rawMeta = sessionStorage.getItem(SESSION_META_KEY);
  if (!accessToken || !rawMeta) {
    return null;
  }
  try {
    const meta = JSON.parse(rawMeta) as SessionMeta;
    return {
      mode: 'dev-bypass',
      accessToken,
      userId: meta.userId,
      tenantId: meta.tenantId,
      businessId: resolveInitialBusinessId(meta.businessId),
    };
  } catch {
    sessionStorage.removeItem(ACCESS_TOKEN_KEY);
    sessionStorage.removeItem(SESSION_META_KEY);
    return null;
  }
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly config = inject(AppConfigService);
  private readonly session = signal<AuthSession | null>(readStoredSession());

  readonly current = this.session.asReadonly();

  /** Interim UI gate until Iden OIDC (DQ-1502) — requires API-issued token. */
  isUiAuthenticated(): boolean {
    return this.session()?.accessToken != null;
  }

  /**
   * Calls POST /api/app/auth/login — credentials validated on the API (Auth:InterimFeGate).
   */
  login(username: string, password: string): Observable<boolean> {
    const url = `${this.config.apiBaseUrl}/api/app/auth/login`;
    return this.http.post<LoginApiResponse>(url, { username, password }).pipe(
      tap((res) => {
        const businessId = resolveInitialBusinessId(res.businessId);
        const next: AuthSession = {
          mode: 'dev-bypass',
          accessToken: res.accessToken,
          userId: res.userId,
          tenantId: res.tenantId,
          businessId,
        };
        sessionStorage.setItem(ACCESS_TOKEN_KEY, res.accessToken);
        sessionStorage.setItem(
          SESSION_META_KEY,
          JSON.stringify({
            userId: res.userId,
            tenantId: res.tenantId,
            businessId: res.businessId,
          } satisfies SessionMeta),
        );
        this.session.set(next);
      }),
      map(() => true),
      catchError(() => of(false)),
    );
  }

  logout(): void {
    sessionStorage.removeItem(ACCESS_TOKEN_KEY);
    sessionStorage.removeItem(SESSION_META_KEY);
    this.session.set(null);
  }

  /** Interim opaque token from login API; Iden OIDC later. */
  getAccessToken(): string | null {
    return this.session()?.accessToken ?? null;
  }

  getBusinessId(): string | null {
    return this.session()?.businessId ?? null;
  }

  switchBusiness(businessId: string): void {
    localStorage.setItem(ACTIVE_BUSINESS_KEY, businessId);
    const current = this.session();
    if (current) {
      this.session.set({ ...current, businessId });
    }
  }
}
