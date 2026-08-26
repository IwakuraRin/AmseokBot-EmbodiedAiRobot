# Frontend architecture

This directory is the Angular frontend. Read the repository `AGENTS.md` and `ARCHITECTURE.md`
before changing it.

## Ownership

- `src/app/app.*` is the composition root. It wires application configuration and top-level
  routes; it does not own feature behavior.
- `src/app/layout` owns the application shell and global navigation layout. Root TypeScript files
  are focused public entry points; `layout/lib` is private implementation.
- `src/app/features/<name>` owns one built-in business capability. Public route/configuration
  entry points live directly at the feature root; implementation belongs under that feature's
  `lib` directory.
- `src/app/shared` owns business-neutral, reusable UI and technical primitives only. A focused
  root file such as `shared/ui/page-header.ts` is a public entry point; nested `lib` directories
  are private.
- `src/app/core` is reserved for app-wide singleton infrastructure, startup concerns, guards,
  interceptors, and global providers. Do not put ordinary components or feature behavior there.
- `src/app/api-client/generated` is generated from the backend OpenAPI document. Do not edit
  generated files manually.
- `src/app/plugin-host` renders backend-validated plugin manifests using known renderer types. It
  must not execute plugin-provided JavaScript or arbitrary endpoints.

## Dependency rules

- Features may depend on Angular, Angular Material, business-neutral `shared` entry points, and
  generated API client entry points. Features may not import another feature.
- Code outside a feature may import that feature only through root public entry points. Never
  import another boundary's `lib` directory.
- `core`, `shared`, `layout`, `api-client`, and `plugin-host` may not depend on built-in features.
- `shared` must remain business-neutral and must not depend on features, layout, or plugin-host.
- The app composition root may compose features, layout, and plugin-host without leaking their
  private implementation.
- Keep dependency cycles forbidden. Preserve and run the existing dependency-cruiser checks.

## Components and styling

- Before adding a component, directive, pipe, model, or service, search `src/app/shared` and the
  owning feature for an existing public capability.
- Put feature-specific components beside their feature. Put shell/navigation components in
  `layout`. Promote something to `shared` only when it is business-neutral and genuinely reused.
- Use Angular Material components and theme tokens. Global typography belongs in the global theme
  entry; component presentation belongs with the owning component.
- Do not add custom styles unless the user explicitly requests that styling change. Do not copy
  Angular Material's framework styles into project style files.

Run `npm run check` in this directory and the repository-level `npm run check` before handoff.
