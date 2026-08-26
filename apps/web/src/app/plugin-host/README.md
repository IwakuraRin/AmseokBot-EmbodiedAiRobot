# Plugin host boundary

The plugin host renders backend-validated manifests through a fixed set of renderer types. JSON may reference action IDs, but it must never contain executable JavaScript, PowerShell, DLL paths, or arbitrary endpoints.

Only `routes.ts` and `manifest.ts` are public. Files under `lib/` are private implementation details.
