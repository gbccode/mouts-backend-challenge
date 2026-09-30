import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { AuthService } from '../services/auth.service';

export const authInterceptor: HttpInterceptorFn = (request, next) => {
  const path = request.url.split(/[?#]/)[0].toLowerCase();
  // Relative API URLs work with both the Angular proxy and Docker's Nginx.
  // Never send our bearer token to external URLs, assets or the login endpoint.
  if (!path.startsWith('/api/') || path === '/api/auth' || path === '/api/auth/') {
    return next(request);
  }

  const token = inject(AuthService).getToken();
  return next(token
    ? request.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
    : request);
};
