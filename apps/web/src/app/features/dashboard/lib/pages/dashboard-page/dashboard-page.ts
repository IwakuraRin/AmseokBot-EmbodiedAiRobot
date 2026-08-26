import { HttpClient } from '@angular/common/http';
import { DatePipe } from '@angular/common';
import { Component, DestroyRef, OnInit, computed, inject, signal } from '@angular/core';
import { EMPTY, catchError, interval, startWith, switchMap, take } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

interface HardwareInventory {
  collectedAt: string;
  operatingSystem: string;
  cpu: CpuDevice;
  totalMemoryBytes: number;
  gpus: GpuDevice[];
  physicalDisks: PhysicalDisk[];
  mainboard: Mainboard;
}

interface CpuDevice {
  model: string;
  physicalCoreCount: number;
  logicalProcessorCount: number;
}

interface GpuDevice {
  id: string;
  model: string;
  vendor: string;
  memoryKind: 'Unknown' | 'Dedicated' | 'Shared';
  dedicatedMemoryBytes: number | null;
}

interface PhysicalDisk {
  id: string;
  model: string;
  serialNumber: string | null;
  sizeBytes: number;
  busType: string;
}

interface Mainboard {
  manufacturer: string | null;
  model: string | null;
  version: string | null;
}

interface HardwareMetrics {
  sampledAt: string;
  cpu: CpuMetrics;
  memory: MemoryMetrics;
  gpus: GpuMetrics[];
}

interface CpuMetrics {
  utilizationPercent: number | null;
  availability: MetricAvailability;
}

interface MemoryMetrics {
  totalBytes: number;
  usedBytes: number;
  availableBytes: number;
  utilizationPercent: number;
}

interface GpuMetrics {
  deviceId: string;
  utilizationPercent: number | null;
  memoryUsedBytes: number | null;
  utilizationAvailability: MetricAvailability;
  memoryAvailability: MetricAvailability;
}

type MetricAvailability =
  'Available' | 'Unsupported' | 'DriverUnavailable' | 'PermissionDenied' | 'TemporarilyUnavailable';

interface GpuUsage {
  id: string;
  label: string;
  utilizationPercent: number | null;
  memoryDescription: string | null;
}

@Component({
  selector: 'app-dashboard-page',
  imports: [DatePipe],
  templateUrl: './dashboard-page.html',
  styleUrl: './dashboard-page.scss',
})
export class DashboardPage implements OnInit {
  private readonly http = inject(HttpClient);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly inventory = signal<HardwareInventory | null>(null);
  protected readonly metrics = signal<HardwareMetrics | null>(null);
  protected readonly inventoryUnavailable = signal(false);
  protected readonly metricsUnavailable = signal(false);
  protected readonly gpuUsages = computed<GpuUsage[]>(() => {
    const inventory = this.inventory();
    const metrics = this.metrics();
    if (!inventory) {
      return [];
    }

    return inventory.gpus.map((gpu) => {
      const gpuMetrics = metrics?.gpus.find((candidate) => candidate.deviceId === gpu.id);
      const utilizationPercent =
        gpuMetrics?.utilizationAvailability === 'Available'
          ? (gpuMetrics.utilizationPercent ?? null)
          : null;
      const memoryDescription =
        gpuMetrics?.memoryAvailability === 'Available' && gpuMetrics.memoryUsedBytes !== null
          ? gpu.dedicatedMemoryBytes
            ? `${this.formatBytes(gpuMetrics.memoryUsedBytes)} / ${this.formatBytes(gpu.dedicatedMemoryBytes)}`
            : `已用 ${this.formatBytes(gpuMetrics.memoryUsedBytes)}`
          : null;

      return {
        id: gpu.id,
        label: gpu.model,
        utilizationPercent,
        memoryDescription,
      };
    });
  });

  ngOnInit(): void {
    this.loadInventory();
    this.pollMetrics();
  }

  protected formatBytes(bytes: number): string {
    if (!Number.isFinite(bytes) || bytes <= 0) {
      return '0 GB';
    }

    const gibibytes = bytes / 1024 ** 3;
    return `${new Intl.NumberFormat('zh-CN', { maximumFractionDigits: gibibytes >= 100 ? 0 : 1 }).format(gibibytes)} GB`;
  }

  protected formatMainboard(mainboard: Mainboard): string {
    return (
      [mainboard.manufacturer, mainboard.model, mainboard.version]
        .filter((value): value is string => Boolean(value))
        .join(' · ') || '未知'
    );
  }

  protected formatGpuMemory(gpu: GpuDevice): string {
    if (gpu.memoryKind === 'Shared') {
      return '共享内存';
    }

    return gpu.dedicatedMemoryBytes === null
      ? '显存未知'
      : this.formatBytes(gpu.dedicatedMemoryBytes);
  }

  protected formatPercent(percent: number | null): string {
    return percent === null
      ? '暂不可用'
      : `${new Intl.NumberFormat('zh-CN', { maximumFractionDigits: 1 }).format(percent)}%`;
  }

  protected boundedPercent(percent: number | null): number {
    return Math.min(100, Math.max(0, percent ?? 0));
  }

  private loadInventory(): void {
    this.http
      .get<HardwareInventory>('/api/system/hardware')
      .pipe(
        take(1),
        catchError(() => {
          this.inventoryUnavailable.set(true);
          return EMPTY;
        }),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((inventory) => {
        this.inventory.set(inventory);
        this.inventoryUnavailable.set(false);
      });
  }

  private pollMetrics(): void {
    interval(2_000)
      .pipe(
        startWith(0),
        switchMap(() =>
          this.http.get<HardwareMetrics>('/api/system/metrics').pipe(
            catchError(() => {
              this.metricsUnavailable.set(true);
              return EMPTY;
            }),
          ),
        ),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((metrics) => {
        this.metrics.set(metrics);
        this.metricsUnavailable.set(false);
      });
  }
}
