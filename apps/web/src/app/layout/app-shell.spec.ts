import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { SessionStore, WebSession } from '../core/session/session';
import { AppShell } from './shell';

const viewerSession: WebSession = {
  user: { id: 'viewer-id', userName: 'viewer', displayName: 'Viewer' },
  roles: ['Viewer'],
  permissions: ['system.overview.read', 'storage.read', 'shares.read', 'operations.read'],
};

const session = signal<WebSession | null>(viewerSession);
const sessionStore = {
  session: session.asReadonly(),
  can: (permission: string) => session()?.permissions.includes(permission) ?? false,
  logout: async () => session.set(null),
};

describe('AppShell', () => {
  beforeEach(async () => {
    session.set(viewerSession);
    await TestBed.configureTestingModule({
      imports: [AppShell],
      providers: [provideRouter([]), { provide: SessionStore, useValue: sessionStore }],
    }).compileComponents();
  });

  it('renders the product brand in the top toolbar', async () => {
    const fixture = TestBed.createComponent(AppShell);
    await fixture.whenStable();

    const toolbar = fixture.nativeElement.querySelector('mat-toolbar') as HTMLElement;
    const brand = toolbar.querySelector('.brand-lockup') as HTMLAnchorElement;
    const accountActions = toolbar.querySelector('.account-actions') as HTMLElement;
    const accountName = accountActions.querySelector('.account-name') as HTMLElement;
    const logoutButton = accountActions.querySelector('.logout-button') as HTMLButtonElement;

    expect(brand.querySelector('.brand-name')?.textContent).toBe('Amseok');
    expect(brand.querySelector('.product-name')?.textContent).toBe('NasForWindows');
    expect(brand.getAttribute('href')).toBe('/dashboard');
    expect(accountName.textContent?.trim()).toBe('Viewer');
    expect(accountName.nextElementSibling).toBe(logoutButton);
    expect(toolbar.querySelector('nav')).toBeNull();
    expect(logoutButton.textContent?.trim()).toBe('退出登录');
  });

  it('renders only navigation allowed by the current permissions', async () => {
    const fixture = TestBed.createComponent(AppShell);
    await fixture.whenStable();

    const navigation = fixture.nativeElement.querySelector('.side-navigation') as HTMLElement;
    const links = navigation.querySelectorAll('mat-nav-list a');

    expect(navigation.getAttribute('aria-label')).toBe('主菜单');
    expect(links).toHaveLength(2);
    expect(links[0].textContent?.trim()).toBe('系统概览');
    expect(links[0].getAttribute('href')).toBe('/dashboard');
    expect(links[1].textContent?.trim()).toBe('磁盘');
    expect(links[1].getAttribute('href')).toBe('/disks');
    expect(fixture.nativeElement.querySelector('main router-outlet')).not.toBeNull();
  });

  it('groups Web users and audit under system management', async () => {
    session.set({
      ...viewerSession,
      permissions: ['system.overview.read', 'storage.read', 'web.users.manage', 'audit.read'],
    });
    const fixture = TestBed.createComponent(AppShell);
    await fixture.whenStable();

    const navigation = fixture.nativeElement.querySelector('.side-navigation') as HTMLElement;
    const dashboardLink = navigation.querySelector('a[href="/dashboard"]') as HTMLAnchorElement;
    const toggle = navigation.querySelector('.system-management-toggle') as HTMLButtonElement;

    expect(toggle.textContent).toContain('系统管理');
    expect(toggle.getAttribute('aria-expanded')).toBe('false');
    expect(dashboardLink.nextElementSibling).toBe(toggle);
    expect(navigation.querySelector('.nested-navigation')).toBeNull();

    toggle.click();
    fixture.detectChanges();
    await fixture.whenStable();

    const childLinks = navigation.querySelectorAll('.nested-navigation a');
    expect(toggle.getAttribute('aria-expanded')).toBe('true');
    expect(childLinks).toHaveLength(2);
    expect(childLinks[0].textContent?.trim()).toBe('Web 用户');
    expect(childLinks[0].getAttribute('href')).toBe('/web-users');
    expect(childLinks[1].textContent?.trim()).toBe('审计日志');
    expect(childLinks[1].getAttribute('href')).toBe('/audit');
  });
});
