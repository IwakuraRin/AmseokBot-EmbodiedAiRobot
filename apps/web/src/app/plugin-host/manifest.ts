export type PluginPageType = 'data-table' | 'detail' | 'form' | 'dashboard';

export interface PluginNavigationItem {
  readonly title: string;
  readonly pageId: string;
  readonly permission?: string;
}

export interface PluginPageDefinition {
  readonly id: string;
  readonly title: string;
  readonly type: PluginPageType;
}

export interface PluginManifest {
  readonly schemaVersion: '1.0';
  readonly id: string;
  readonly name: string;
  readonly version: string;
  readonly minHostApiVersion: string;
  readonly navigation: readonly PluginNavigationItem[];
  readonly pages: readonly PluginPageDefinition[];
}
