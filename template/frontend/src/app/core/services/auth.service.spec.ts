import { DOCUMENT } from '@angular/common';
import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { ActivatedRouteSnapshot, provideRouter, Router, RouterStateSnapshot, UrlTree } from '@angular/router';
import { authGuard } from '../guards/auth.guard';
import { authInterceptor } from '../interceptors/auth.interceptor';
import { AuthService } from './auth.service';

describe('Authentication integration', () => {
  const storageKey = 'mouts.auth.token';
  const credentials = { email: 'user@example.com', password: 'test-password' };
  let http: HttpTestingController;

  function token(exp = Math.floor(Date.now() / 1000) + 3600): string {
    const encode = (value: object) => btoa(JSON.stringify(value)).replace(/=/g, '').replace(/\+/g, '-').replace(/\//g, '_');
    return `${encode({ alg: 'HS256', typ: 'JWT' })}.${encode({ exp })}.test-signature`;
  }

  function login(jwt: string): void {
    TestBed.inject(AuthService).login(credentials).subscribe();
    http.expectOne({ method: 'POST', url: '/api/Auth' }).flush({
      success: true, message: 'User authenticated successfully', errors: [],
      data: { token: jwt, email: credentials.email, name: 'User', role: 'Admin' },
    });
  }

  beforeEach(() => {
    sessionStorage.removeItem(storageKey);
    TestBed.configureTestingModule({ providers: [
      provideHttpClient(withInterceptors([authInterceptor])),
      provideHttpClientTesting(),
      provideRouter([]),
    ] });
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    http.verify();
    sessionStorage.removeItem(storageKey);
  });

  it('logs in through the backend contract, persists only the token and logs out', () => {
    const service = TestBed.inject(AuthService);
    const jwt = token();
    service.login(credentials).subscribe((response) => {
      expect(response.data?.email).toBe(credentials.email);
      expect(response.data?.role).toBe('Admin');
    });
    const request = http.expectOne({ method: 'POST', url: '/api/Auth' });
    expect(request.request.body).toEqual(credentials);
    expect(request.request.headers.has('Authorization')).toBeFalse();
    request.flush({ success: true, message: '', errors: [], data: { token: jwt, email: credentials.email, name: 'User', role: 'Admin' } });

    expect(service.getToken()).toBe(jwt);
    expect(service.isAuthenticated()).toBeTrue();
    expect(sessionStorage.getItem(storageKey)).toBe(jwt);
    service.logout();
    expect(service.isAuthenticated()).toBeFalse();
    expect(sessionStorage.getItem(storageKey)).toBeNull();
  });

  it('restores an unexpired session after reload', () => {
    const jwt = token();
    sessionStorage.setItem(storageKey, jwt);
    expect(TestBed.inject(AuthService).getToken()).toBe(jwt);
  });

  for (const value of ['invalid', 'a.e30.c', 'a.@@@.c']) {
    it(`clears a malformed stored token (${value})`, () => {
      sessionStorage.setItem(storageKey, value);
      expect(TestBed.inject(AuthService).isAuthenticated()).toBeFalse();
      expect(sessionStorage.getItem(storageKey)).toBeNull();
    });
  }

  it('rechecks token expiry when time passes during the same session', () => {
    jasmine.clock().install();
    try {
      const now = Date.now();
      jasmine.clock().mockDate(new Date(now));
      sessionStorage.setItem(storageKey, token(Math.floor(now / 1000) + 1));
      const service = TestBed.inject(AuthService);
      expect(service.isAuthenticated()).toBeTrue();
      jasmine.clock().mockDate(new Date(now + 2000));
      expect(service.isAuthenticated()).toBeFalse();
      expect(sessionStorage.getItem(storageKey)).toBeNull();
    } finally {
      jasmine.clock().uninstall();
    }
  });

  it('does not create a session after a failed login', () => {
    const service = TestBed.inject(AuthService);
    service.login(credentials).subscribe({
      next: () => fail('Expected login failure'),
      error: (error) => expect(error.status).toBe(401),
    });
    http.expectOne('/api/Auth').flush({ success: false, message: 'Invalid credentials', errors: [] }, { status: 401, statusText: 'Unauthorized' });
    expect(service.isAuthenticated()).toBeFalse();
    expect(sessionStorage.getItem(storageKey)).toBeNull();
  });

  it('rejects an expired token returned by login', () => {
    const service = TestBed.inject(AuthService);
    service.login(credentials).subscribe({
      next: () => fail('Expected invalid session failure'),
      error: (error) => expect(error.message).toContain('valid session token'),
    });
    http.expectOne('/api/Auth').flush({ success: true, message: '', errors: [], data: { token: token(1) } });
    expect(service.isAuthenticated()).toBeFalse();
  });

  it('works in memory when browser storage is blocked', () => {
    const document = TestBed.inject(DOCUMENT);
    const storage = spyOnProperty(document.defaultView!, 'sessionStorage', 'get').and.throwError('Storage blocked');
    try {
      const jwt = token();
      login(jwt);
      const service = TestBed.inject(AuthService);
      expect(service.getToken()).toBe(jwt);
      service.logout();
      expect(service.getToken()).toBeNull();
    } finally {
      storage.and.callThrough();
    }
  });

  it('attaches the session bearer token to relative API calls', () => {
    const jwt = token();
    login(jwt);
    TestBed.inject(HttpClient).get('/api/sales/123').subscribe();
    const request = http.expectOne('/api/sales/123');
    expect(request.request.headers.get('Authorization')).toBe(`Bearer ${jwt}`);
    request.flush({});
  });

  it('does not attach credentials to external URLs, assets, API lookalikes or login', () => {
    login(token());
    for (const url of ['https://example.com/api/sales', '//example.com/api/sales', '/assets/config.json', '/apiary', '/api/Auth', '/api/auth/?test=1']) {
      TestBed.inject(HttpClient).get(url).subscribe();
      const request = http.expectOne(url);
      expect(request.request.headers.has('Authorization')).withContext(url).toBeFalse();
      request.flush({});
    }
  });

  it('does not attach an expired session token', () => {
    sessionStorage.setItem(storageKey, token(1));
    TestBed.inject(HttpClient).get('/api/sales/123').subscribe();
    const request = http.expectOne('/api/sales/123');
    expect(request.request.headers.has('Authorization')).toBeFalse();
    request.flush({});
  });

  it('redirects unauthenticated routes to login while preserving the intended URL', () => {
    const result = TestBed.runInInjectionContext(() => authGuard(
      {} as ActivatedRouteSnapshot, { url: '/sales?page=2' } as RouterStateSnapshot,
    ));
    expect(result instanceof UrlTree).toBeTrue();
    expect(TestBed.inject(Router).serializeUrl(result as UrlTree)).toBe('/login?returnUrl=%2Fsales%3Fpage%3D2');
  });

  it('allows routes with a current session', () => {
    sessionStorage.setItem(storageKey, token());
    const result = TestBed.runInInjectionContext(() => authGuard(
      {} as ActivatedRouteSnapshot, { url: '/sales' } as RouterStateSnapshot,
    ));
    expect(result).toBeTrue();
  });
});
