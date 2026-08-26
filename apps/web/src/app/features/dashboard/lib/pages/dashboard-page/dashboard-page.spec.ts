import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { DashboardPage } from './dashboard-page';

describe('DashboardPage', () => {
  it('renders hardware as text and utilization as separated rows without cards', async () => {
    await TestBed.configureTestingModule({
      imports: [DashboardPage],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();

    const fixture = TestBed.createComponent(DashboardPage);
    fixture.detectChanges();
    const httpTesting = TestBed.inject(HttpTestingController);

    httpTesting.expectOne('/api/system/hardware').flush({
      collectedAt: '2026-08-27T08:00:00Z',
      operatingSystem: 'Windows 11 Pro',
      cpu: {
        model: 'Example CPU',
        physicalCoreCount: 8,
        logicalProcessorCount: 16,
      },
      totalMemoryBytes: 32 * 1024 ** 3,
      gpus: [
        {
          id: 'gpu-1',
          model: 'Example GPU',
          vendor: 'Example',
          memoryKind: 'Dedicated',
          dedicatedMemoryBytes: 8 * 1024 ** 3,
        },
      ],
      physicalDisks: [
        {
          id: 'disk-1',
          model: 'Example SSD',
          serialNumber: null,
          sizeBytes: 1024 ** 4,
          busType: 'NVMe',
        },
      ],
      mainboard: { manufacturer: 'Example', model: 'Mainboard', version: null },
    });
    httpTesting.expectOne('/api/system/metrics').flush({
      sampledAt: '2026-08-27T08:00:02Z',
      sampleIntervalSeconds: 2,
      cpu: { utilizationPercent: 25.5, availability: 'Available' },
      memory: {
        totalBytes: 32 * 1024 ** 3,
        usedBytes: 12 * 1024 ** 3,
        availableBytes: 20 * 1024 ** 3,
        utilizationPercent: 37.5,
      },
      gpus: [
        {
          deviceId: 'gpu-1',
          utilizationPercent: 42,
          memoryUsedBytes: 2 * 1024 ** 3,
          utilizationAvailability: 'Available',
          memoryAvailability: 'Available',
        },
      ],
    });
    fixture.detectChanges();

    const element = fixture.nativeElement as HTMLElement;
    const hardwareText = element.querySelector('.hardware-details')?.textContent;
    const usageItems = element.querySelectorAll('.usage-item');

    expect(hardwareText).toContain('Windows 11 Pro');
    expect(hardwareText).toContain('Example CPU');
    expect(hardwareText).toContain('Example GPU');
    expect(hardwareText).toContain('Example SSD');
    expect(usageItems).toHaveLength(3);
    expect(usageItems[0].textContent).toContain('25.5%');
    expect(usageItems[1].textContent).toContain('12 GB / 32 GB');
    expect(usageItems[2].textContent).toContain('42%');
    expect(element.querySelector('mat-card')).toBeNull();

    fixture.destroy();
    httpTesting.verify();
  });

  it('shows the English 303 Agent hint when hardware is unavailable', async () => {
    await TestBed.configureTestingModule({
      imports: [DashboardPage],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();

    const fixture = TestBed.createComponent(DashboardPage);
    fixture.detectChanges();
    const httpTesting = TestBed.inject(HttpTestingController);

    httpTesting
      .expectOne('/api/system/hardware')
      .flush(null, { status: 503, statusText: 'Service Unavailable' });
    httpTesting.expectOne('/api/system/metrics').flush({
      sampledAt: '2026-08-27T08:00:02Z',
      cpu: { utilizationPercent: null, availability: 'TemporarilyUnavailable' },
      memory: {
        totalBytes: 0,
        usedBytes: 0,
        availableBytes: 0,
        utilizationPercent: 0,
      },
      gpus: [],
    });
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('[role="alert"]')?.textContent.trim()).toBe(
      '303 Agent may not be running.',
    );

    fixture.destroy();
    httpTesting.verify();
  });
});
