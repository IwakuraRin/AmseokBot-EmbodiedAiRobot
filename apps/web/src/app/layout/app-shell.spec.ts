import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { AppShell } from './shell';

describe('AppShell', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AppShell],
      providers: [provideRouter([])],
    }).compileComponents();
  });

  it('renders the product brand in the top toolbar', async () => {
    const fixture = TestBed.createComponent(AppShell);
    await fixture.whenStable();

    const toolbar = fixture.nativeElement.querySelector('mat-toolbar') as HTMLElement;
    const brand = toolbar.querySelector('.brand-lockup') as HTMLAnchorElement;

    expect(brand.querySelector('.brand-name')?.textContent).toBe('Amseok');
    expect(brand.querySelector('.product-name')?.textContent).toBe('NasForWindows');
    expect(brand.getAttribute('href')).toBe('/dashboard');
    expect(toolbar.querySelector('nav')).toBeNull();
    expect(toolbar.querySelector('button')).toBeNull();
  });

  it('renders only the system overview item in the side navigation', async () => {
    const fixture = TestBed.createComponent(AppShell);
    await fixture.whenStable();

    const navigation = fixture.nativeElement.querySelector('.side-navigation') as HTMLElement;
    const links = navigation.querySelectorAll('mat-nav-list a');

    expect(navigation.getAttribute('aria-label')).toBe('主菜单');
    expect(links).toHaveLength(1);
    expect(links[0].textContent?.trim()).toBe('系统概览');
    expect(links[0].getAttribute('href')).toBe('/dashboard');
    expect(fixture.nativeElement.querySelector('main router-outlet')).not.toBeNull();
  });
});
