export interface Disk {
  readonly id: string;
  readonly name: string;
  readonly mediaType: 'HDD' | 'SSD';
  readonly capacity: string;
  readonly busType: string;
  readonly health: '正常' | '警告' | '故障';
}
