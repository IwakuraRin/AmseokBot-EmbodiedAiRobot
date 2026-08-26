import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';

export interface WebSession {
  user: {
    id: string;
    userName: string;
    displayName: string;
  };
  roles: string[];
  permissions: string[];
}

export interface BootstrapStatus {
  requiresBootstrap: boolean;
  canInitialize: boolean;
}

export interface BootstrapOwnerRequest {
  token: string;
  userName: string;
  displayName: string;
  password: string;
}

@Injectable({ providedIn: 'root' })
export class SessionStore {
  private readonly http = inject(HttpClient);
  private readonly sessionState = signal<WebSession | null>(null);
  private readonly bootstrapState = signal<BootstrapStatus | null>(null);
  private readonly initializedState = signal(false);

  readonly session = this.sessionState.asReadonly();
  readonly bootstrapStatus = this.bootstrapState.asReadonly();
  readonly initialized = this.initializedState.asReadonly();

  async initialize(): Promise<void> {
    try {
      await this.refreshAntiforgeryToken();
      this.bootstrapState.set(
        await firstValueFrom(this.http.get<BootstrapStatus>('/api/bootstrap/status')),
      );
      await this.refreshSession();
    } finally {
      this.initializedState.set(true);
    }
  }

  async login(userName: string, password: string, rememberMe: boolean): Promise<void> {
    await firstValueFrom(
      this.http.post<void>('/api/auth/login', { userName, password, rememberMe }),
    );
    await this.refreshAntiforgeryToken();
    await this.refreshSession();
  }

  async logout(): Promise<void> {
    await firstValueFrom(this.http.post<void>('/api/auth/logout', {}));
    this.sessionState.set(null);
    await this.refreshAntiforgeryToken();
  }

  async createFirstOwner(request: BootstrapOwnerRequest): Promise<void> {
    await firstValueFrom(this.http.post<void>('/api/bootstrap/owner', request));
    this.bootstrapState.set({ requiresBootstrap: false, canInitialize: false });
  }

  can(permission: string): boolean {
    return this.sessionState()?.permissions.includes(permission) ?? false;
  }

  private async refreshSession(): Promise<void> {
    try {
      this.sessionState.set(await firstValueFrom(this.http.get<WebSession>('/api/session')));
    } catch (error) {
      if (error instanceof HttpErrorResponse && error.status === 401) {
        this.sessionState.set(null);
        return;
      }

      throw error;
    }
  }

  private async refreshAntiforgeryToken(): Promise<void> {
    await firstValueFrom(this.http.get<void>('/api/security/antiforgery'));
  }
}
