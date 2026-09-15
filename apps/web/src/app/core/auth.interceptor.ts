import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { AuthService } from './auth.service';

/** Attaches interim Authorization header when present (Iden later). */
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const token = auth.getAccessToken();
  if (!token) {
    return next(req);
  }

  const headers: Record<string, string> = {
    Authorization: `Bearer ${token}`,
  };
  const businessId = auth.getBusinessId();
  if (businessId) {
    headers['X-Business-Id'] = businessId;
  }

  return next(req.clone({ setHeaders: headers }));
};
