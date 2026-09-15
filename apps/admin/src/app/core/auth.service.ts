import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map, tap, catchError, of } from 'rxjs';
import { AppConfigService } from './app-config.service';

export interface AdminAuthSession {
  accessToken: string;
  userId: string;
}

interface LoginApiResponse {
  accessToken: string;
  tokenType: string;
}

interface MeApiResponse {
  userId: string;
  isPlatformAdmin: boolean;
}

const ACCESS_TOKEN_KEY = 'documate.admin.accessToken';
const USER_ID_KEY = 'documate.admin.userId';

function readStoredSession(): AdminAuthSession | null {
  const accessToken = sessionStorage.getItem(ACCESS_TOKEN_KEY);
  const userId = sessionStorage.getItem(USER_ID_KEY);
  if (!accessToken) {
    return null;
  }
  return { accessToken, userId: userId ?? 'ops-admin' };
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly config = inject(AppConfigService);
  private readonly session = signal<AdminAuthSession | null>(readStoredSession());

  readonly current = this.session.asReadonly();

  isUiAuthenticated(): boolean {
    return this.session()?.accessToken != null;
  }

  login(username: string, password: string): Observable<boolean> {
    const url = `${this.config.apiBaseUrl}/api/admin/auth/login`;
    return this.http.post<LoginApiResponse>(url, { username, password }).pipe(
      tap((res) => {
        sessionStorage.setItem(ACCESS_TOKEN_KEY, res.accessToken);
        sessionStorage.setItem(USER_ID_KEY, username);
        this.session.set({ accessToken: res.accessToken, userId: username });
      }),
      map(() => true),
      catchError(() => of(false)),
    );
  }

  /** Confirms PlatformAdmin after login (B1). */
  refreshMe(): Observable<MeApiResponse | null> {
    const url = `${this.config.apiBaseUrl}/api/admin/me`;
    return this.http.get<MeApiResponse>(url).pipe(
      tap((me) => {
        sessionStorage.setItem(USER_ID_KEY, me.userId);
        const current = this.session();
        if (current) {
          this.session.set({ ...current, userId: me.userId });
        }
      }),
      catchError(() => of(null)),
    );
  }

  logout(): void {
    sessionStorage.removeItem(ACCESS_TOKEN_KEY);
    sessionStorage.removeItem(USER_ID_KEY);
    this.session.set(null);
  }

  getAccessToken(): string | null {
    return this.session()?.accessToken ?? null;
  }
}
