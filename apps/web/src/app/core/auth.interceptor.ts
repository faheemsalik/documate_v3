import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { AuthService } from './auth.service';

/** Attaches Bearer + X-Business-Id; on 401 clears session and sends user to login. */
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const router = inject(Router);
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

  return next(req.clone({ setHeaders: headers })).pipe(
    catchError((err: unknown) => {
      if (err instanceof HttpErrorResponse && err.status === 401) {
        auth.logout();
        void router.navigate(['/login'], {
          queryParams: { returnUrl: router.url },
        });
      }
      return throwError(() => err);
    }),
  );
};
