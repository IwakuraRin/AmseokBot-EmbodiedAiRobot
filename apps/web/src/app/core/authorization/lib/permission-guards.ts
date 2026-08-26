import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { SessionStore } from '../../session/session';

export const authenticatedGuard: CanActivateFn = () => {
  const sessionStore = inject(SessionStore);
  const router = inject(Router);
  const bootstrap = sessionStore.bootstrapStatus();

  if (sessionStore.session()) {
    return true;
  }

  if (bootstrap?.requiresBootstrap && bootstrap.canInitialize) {
    return router.createUrlTree(['/setup']);
  }

  return router.createUrlTree(['/login']);
};

export const setupGuard: CanActivateFn = () => {
  const sessionStore = inject(SessionStore);
  const router = inject(Router);
  const bootstrap = sessionStore.bootstrapStatus();

  return bootstrap?.requiresBootstrap && bootstrap.canInitialize
    ? true
    : router.createUrlTree(['/login']);
};

export function permissionGuard(permission: string): CanActivateFn {
  return () => {
    const sessionStore = inject(SessionStore);
    const router = inject(Router);

    if (!sessionStore.session()) {
      return router.createUrlTree(['/login']);
    }

    return sessionStore.can(permission) ? true : router.createUrlTree(['/dashboard']);
  };
}
