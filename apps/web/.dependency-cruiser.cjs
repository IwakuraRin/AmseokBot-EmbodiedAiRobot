// @ts-check

/** @type {import('dependency-cruiser').IConfiguration} */
module.exports = {
  forbidden: [
    {
      name: 'feature-private-from-outside',
      comment: 'Feature lib folders are private.',
      severity: 'error',
      from: { pathNot: '^src/app/features/' },
      to: { path: '^src/app/features/[^/]+/lib/' },
    },
    {
      name: 'no-cross-feature-imports',
      comment: 'Features are composed by the app and may not depend on one another.',
      severity: 'error',
      from: { path: '^src/app/features/([^/]+)/' },
      to: { path: '^src/app/features/', pathNot: '^src/app/features/$1/' },
    },
    {
      name: 'shared-ui-private-from-outside',
      comment: 'Shared UI is consumed through focused public entry points.',
      severity: 'error',
      from: { pathNot: '^src/app/shared/ui/' },
      to: { path: '^src/app/shared/ui/lib/' },
    },
    {
      name: 'layout-private-from-outside',
      comment: 'Layout implementation is private.',
      severity: 'error',
      from: { pathNot: '^src/app/layout/' },
      to: { path: '^src/app/layout/lib/' },
    },
    {
      name: 'plugin-host-private-from-outside',
      comment: 'Plugin host implementation is private.',
      severity: 'error',
      from: { pathNot: '^src/app/plugin-host/' },
      to: { path: '^src/app/plugin-host/lib/' },
    },
    {
      name: 'lower-layers-do-not-import-features',
      comment: 'Lower-level boundaries do not depend on built-in features.',
      severity: 'error',
      from: { path: '^src/app/(core|shared|layout|api-client|plugin-host)/' },
      to: { path: '^src/app/features/' },
    },
    {
      name: 'no-circular',
      comment: 'Dependency cycles are forbidden.',
      severity: 'error',
      from: {},
      to: { circular: true },
    },
  ],
  options: {
    doNotFollow: { path: 'node_modules' },
    tsConfig: { fileName: 'tsconfig.json' },
    enhancedResolveOptions: {
      extensions: ['.ts', '.js', '.json'],
    },
  },
};
