import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map, tap, catchError, of, switchMap } from 'rxjs';
import { AppConfigService } from './app-config.service';

/** Customer auth — InterimFeGate (dev) or Iden password + BuContext (Mode=Iden). */
export interface AuthSession {
  mode: 'dev-bypass' | 'iden';
  accessToken: string;
  userId: string;
  tenantId: string;
  businessId: string;
  buContextId?: string;
}

interface LoginApiResponse {
  accessToken: string;
  tokenType: string;
  userId: string;
  tenantId: string;
  businessId: string;
}

interface IdenTokenResponse {
  access_token?: string;
  accessToken?: string;
  token_type?: string;
  expires_in?: number;
}

interface IdenBuContext {
  bu_context_id?: string;
  id?: string;
  tenant_id?: string;
  tenant_business_id?: string;
  business_id?: string;
}

const ACTIVE_BUSINESS_KEY = 'documate.activeBusinessId';
const ACCESS_TOKEN_KEY = 'documate.interimAccessToken';
const SESSION_META_KEY = 'documate.interimSessionMeta';
const DEFAULT_BUSINESS_ID = '118f4881-9313-4ebb-98cd-57217472d99f';
const LEGACY_PADDED_BUSINESS_ID = '00000000-0000-0000-0000-000000000002';

interface SessionMeta {
  userId: string;
  tenantId: string;
  businessId: string;
  buContextId?: string;
  mode?: 'dev-bypass' | 'iden';
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
      mode: meta.mode ?? 'dev-bypass',
      accessToken,
      userId: meta.userId,
      tenantId: meta.tenantId,
      businessId: resolveInitialBusinessId(meta.businessId),
      buContextId: meta.buContextId,
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

  isUiAuthenticated(): boolean {
    return this.session()?.accessToken != null;
  }

  /**
   * Dev: POST /api/app/auth/login (InterimFeGate).
   * Iden: password grant against idenBaseUrl, then pick first BuContext (or stored business).
   */
  login(username: string, password: string): Observable<boolean> {
    if (this.config.useIdenLogin) {
      return this.loginViaIden(username, password);
    }

    const url = `${this.config.apiBaseUrl}/api/app/auth/login`;
    return this.http.post<LoginApiResponse>(url, { username, password }).pipe(
      tap((res) => {
        this.persistSession({
          mode: 'dev-bypass',
          accessToken: res.accessToken,
          userId: res.userId,
          tenantId: res.tenantId,
          businessId: resolveInitialBusinessId(res.businessId),
        });
      }),
      map(() => true),
      catchError(() => of(false)),
    );
  }

  private loginViaIden(username: string, password: string): Observable<boolean> {
    const base = this.config.idenBaseUrl;
    const tokenUrl = `${base}/auth/token`;
    return this.http
      .post<IdenTokenResponse>(tokenUrl, {
        grant_type: 'password',
        username,
        password,
        software_key: this.config.idenSoftwareKey,
      })
      .pipe(
        switchMap((tok) => {
          const accessToken = tok.access_token ?? tok.accessToken;
          if (!accessToken) {
            return of(false);
          }
          return this.http
            .get<IdenBuContext[] | { items?: IdenBuContext[] }>(`${base}/bu-contexts`, {
              headers: { Authorization: `Bearer ${accessToken}` },
            })
            .pipe(
              map((raw) => {
                const list = Array.isArray(raw) ? raw : (raw.items ?? []);
                const preferredBiz = localStorage.getItem(ACTIVE_BUSINESS_KEY);
                const ctx =
                  list.find(
                    (c) =>
                      preferredBiz &&
                      (c.tenant_business_id === preferredBiz || c.business_id === preferredBiz),
                  ) ?? list[0];
                if (!ctx) {
                  return false;
                }
                const businessId =
                  ctx.tenant_business_id ?? ctx.business_id ?? preferredBiz ?? DEFAULT_BUSINESS_ID;
                this.persistSession({
                  mode: 'iden',
                  accessToken,
                  userId: 'iden-user',
                  tenantId: ctx.tenant_id ?? '',
                  businessId,
                  buContextId: ctx.bu_context_id ?? ctx.id,
                });
                return true;
              }),
              catchError(() => {
                this.persistSession({
                  mode: 'iden',
                  accessToken,
                  userId: 'iden-user',
                  tenantId: '',
                  businessId: resolveInitialBusinessId(DEFAULT_BUSINESS_ID),
                });
                return of(true);
              }),
            );
        }),
        catchError(() => of(false)),
      );
  }

  private persistSession(next: AuthSession): void {
    sessionStorage.setItem(ACCESS_TOKEN_KEY, next.accessToken);
    sessionStorage.setItem(
      SESSION_META_KEY,
      JSON.stringify({
        userId: next.userId,
        tenantId: next.tenantId,
        businessId: next.businessId,
        buContextId: next.buContextId,
        mode: next.mode,
      } satisfies SessionMeta),
    );
    this.session.set(next);
  }

  logout(): void {
    sessionStorage.removeItem(ACCESS_TOKEN_KEY);
    sessionStorage.removeItem(SESSION_META_KEY);
    this.session.set(null);
  }

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
