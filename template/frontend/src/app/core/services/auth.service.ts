import { DOCUMENT } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { tap } from 'rxjs';
import { ApiResponse } from '../models/api-response.model';
import { AuthResult, LoginInput } from '../models/auth.model';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly document = inject(DOCUMENT);
  private readonly storageKey = 'mouts.auth.token';
  private token: string | null = this.readToken();

  login(input: LoginInput) {
    return this.http.post<ApiResponse<AuthResult>>('/api/Auth', input).pipe(
      tap((response) => {
        const token = response.data?.token;
        if (!response.success || !token || !this.isTokenCurrent(token)) {
          throw new Error('The login response did not contain a valid session token.');
        }

        this.token = token;
        try {
          this.document.defaultView?.sessionStorage.setItem(this.storageKey, token);
        } catch {
          // Keep the session in memory when browser storage is unavailable.
        }
      }),
    );
  }

  getToken(): string | null {
    if (this.token && !this.isTokenCurrent(this.token)) {
      this.logout();
    }
    return this.token;
  }

  isAuthenticated(): boolean {
    return this.getToken() !== null;
  }

  logout(): void {
    this.token = null;
    try {
      this.document.defaultView?.sessionStorage.removeItem(this.storageKey);
    } catch {
      // The in-memory session has still been cleared.
    }
  }

  private readToken(): string | null {
    try {
      return this.document.defaultView?.sessionStorage.getItem(this.storageKey) ?? null;
    } catch {
      return null;
    }
  }

  /** Checks local expiry only; the API remains responsible for JWT verification. */
  private isTokenCurrent(token: string): boolean {
    try {
      const parts = token.split('.');
      if (parts.length !== 3 || parts.some((part) => !part)) {
        return false;
      }

      const base64 = parts[1].replace(/-/g, '+').replace(/_/g, '/');
      const payload: unknown = JSON.parse(atob(base64.padEnd(Math.ceil(base64.length / 4) * 4, '=')));
      if (!payload || typeof payload !== 'object' || !('exp' in payload)) {
        return false;
      }

      return typeof payload.exp === 'number'
        && Number.isFinite(payload.exp)
        && payload.exp > Date.now() / 1000;
    } catch {
      return false;
    }
  }
}
