import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { SessionStore } from '../session';

describe('SessionStore', () => {
  let store: SessionStore;
  let httpTesting: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    store = TestBed.inject(SessionStore);
    httpTesting = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpTesting.verify());

  it('loads the bootstrap state and effective permissions from the API', async () => {
    const initialization = store.initialize();

    httpTesting.expectOne('/api/security/antiforgery').flush(null);
    await vi.waitFor(() => {
      httpTesting
        .expectOne('/api/bootstrap/status')
        .flush({ requiresBootstrap: false, canInitialize: false });
    });
    await vi.waitFor(() => {
      httpTesting.expectOne('/api/session').flush({
        user: { id: 'owner-id', userName: 'owner', displayName: 'Owner' },
        roles: ['Owner'],
        permissions: ['storage.destroy'],
      });
    });

    await initialization;

    expect(store.initialized()).toBe(true);
    expect(store.can('storage.destroy')).toBe(true);
    expect(store.can('storage.manage')).toBe(false);
  });

  it('treats an unauthorized session response as signed out', async () => {
    const initialization = store.initialize();

    httpTesting.expectOne('/api/security/antiforgery').flush(null);
    await vi.waitFor(() => {
      httpTesting
        .expectOne('/api/bootstrap/status')
        .flush({ requiresBootstrap: false, canInitialize: false });
    });
    await vi.waitFor(() => {
      httpTesting
        .expectOne('/api/session')
        .flush(null, { status: 401, statusText: 'Unauthorized' });
    });

    await initialization;

    expect(store.session()).toBeNull();
    expect(store.initialized()).toBe(true);
  });
});
