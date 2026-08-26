import { Component } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatTableModule } from '@angular/material/table';
import { PageHeader } from '../../../../../shared/ui/page-header';
import { Disk } from '../../model/disk';

@Component({
  selector: 'app-disk-list-page',
  imports: [MatButtonModule, MatTableModule, PageHeader],
  templateUrl: './disk-list-page.html',
})
export class DiskListPage {
  protected readonly displayedColumns = ['name', 'mediaType', 'capacity', 'busType', 'health'];
  protected readonly disks: readonly Disk[] = [
    {
      id: 'disk-0',
      name: '示例磁盘 0',
      mediaType: 'SSD',
      capacity: '931.5 GB',
      busType: 'SATA',
      health: '正常',
    },
    {
      id: 'disk-1',
      name: '示例磁盘 1',
      mediaType: 'HDD',
      capacity: '3.64 TB',
      busType: 'USB',
      health: '正常',
    },
  ];
}
