import { Component } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { PageHeader } from '../../../../shared/ui/page-header';

@Component({
  selector: 'app-plugin-catalog-page',
  imports: [MatCardModule, PageHeader],
  templateUrl: './plugin-catalog-page.html',
})
export class PluginCatalogPage {}
