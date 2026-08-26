import { Component, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { MatCardModule } from '@angular/material/card';
import { ActivatedRoute } from '@angular/router';
import { map } from 'rxjs';
import { PageHeader } from '../../../../shared/ui/page-header';

@Component({
  selector: 'app-plugin-page',
  imports: [MatCardModule, PageHeader],
  templateUrl: './plugin-page.html',
})
export class PluginPage {
  private readonly route = inject(ActivatedRoute);

  protected readonly routeIdentity = toSignal(
    this.route.paramMap.pipe(
      map((params) => ({
        pluginId: params.get('pluginId') ?? '',
        pageId: params.get('pageId') ?? '',
      })),
    ),
    { initialValue: { pluginId: '', pageId: '' } },
  );
}
