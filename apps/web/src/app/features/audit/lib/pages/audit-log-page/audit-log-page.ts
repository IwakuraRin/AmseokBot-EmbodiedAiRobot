import { HttpClient } from '@angular/common/http';
import { Component, OnInit, inject, signal } from '@angular/core';
import { MatTableModule } from '@angular/material/table';
import { firstValueFrom } from 'rxjs';
import { PageHeader } from '../../../../../shared/ui/page-header';

interface AuditEvent {
  id: number;
  occurredAtUtc: string;
  actorName: string | null;
  action: string;
  targetType: string | null;
  targetId: string | null;
  outcome: string;
  sourceIp: string | null;
  correlationId: string;
}

@Component({
  selector: 'app-audit-log-page',
  imports: [MatTableModule, PageHeader],
  templateUrl: './audit-log-page.html',
})
export class AuditLogPage implements OnInit {
  private readonly http = inject(HttpClient);

  protected readonly events = signal<AuditEvent[]>([]);
  protected readonly displayedColumns = [
    'occurredAtUtc',
    'actorName',
    'action',
    'target',
    'outcome',
    'sourceIp',
  ];

  async ngOnInit(): Promise<void> {
    this.events.set(await firstValueFrom(this.http.get<AuditEvent[]>('/api/audit?take=200')));
  }
}
