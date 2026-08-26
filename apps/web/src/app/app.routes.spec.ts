import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { SessionStore, WebSession } from './core/session/session';
import { routes } from './app.routes';

describe('application routes', () => {
  const ownerSession = signal<WebSession | null>({
    user: { id: 'owner-id', userName: 'owner', displayName: 'Owner' },
    roles: ['Owner'],
    permissions: ['system.overview.read'],
  });
  const sessionStore = {
    session: ownerSession.asReadonly(),
    bootstrapStatus: signal({ requiresBootstrap: false, canInitialize: false }).asReadonly(),
    can: (permission: string) => ownerSession()?.permissions.includes(permission) ?? false,
    login: async () => undefined,
    logout: async () => ownerSession.set(null),
  };

  beforeEach(() => {
    ownerSession.set({
      user: { id: 'owner-id', userName: 'owner', displayName: 'Owner' },
      roles: ['Owner'],
      permissions: ['system.overview.read'],
    });
    TestBed.configureTestingModule({
      providers: [provideRouter(routes), { provide: SessionStore, useValue: sessionStore }],
    });
  });

  it('loads the public login page through the authentication feature entry point', async () => {
    ownerSession.set(null);
    const harness = await RouterTestingHarness.create('/login');

    expect(harness.routeNativeElement?.textContent).toContain('登录 Amseok');
  });

  it('loads the protected application shell for an authorized session', async () => {
    const harness = await RouterTestingHarness.create('/dashboard');

    expect(harness.routeNativeElement?.textContent).toContain('Amseok');
    expect(harness.routeNativeElement?.textContent).toContain('系统概览');
  });
});
