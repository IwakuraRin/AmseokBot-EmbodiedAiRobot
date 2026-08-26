import { TestBed } from '@angular/core/testing';
import { DashboardPage } from './dashboard-page';

describe('DashboardPage', () => {
  it('keeps the system overview content area empty', async () => {
    await TestBed.configureTestingModule({
      imports: [DashboardPage],
    }).compileComponents();

    const fixture = TestBed.createComponent(DashboardPage);
    await fixture.whenStable();

    expect(fixture.nativeElement.textContent.trim()).toBe('');
    expect(fixture.nativeElement.children).toHaveLength(0);
  });
});
