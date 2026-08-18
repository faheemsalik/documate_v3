import { Injectable, signal } from '@angular/core';

/** Phase 1 (J3) client auth placeholder — Band 15 replaces with live Iden. */
export interface AuthSession {
  mode: 'dev-bypass' | 'iden';
  userId: string;
  tenantId: string;
  businessId: string;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly session = signal<AuthSession | null>({
    mode: 'dev-bypass',
    userId: 'dev-user',
    tenantId: '00000000-0000-0000-0000-000000000001',
    businessId: '00000000-0000-0000-0000-000000000002',
  });

  readonly current = this.session.asReadonly();

  /** Placeholder until Iden OIDC; DevBypass API does not require a bearer. */
  getAccessToken(): string | null {
    return this.session()?.mode === 'iden' ? null : 'dev-bypass';
  }
}
