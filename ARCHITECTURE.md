# NasForWindows architecture

NasForWindows is a monorepo containing a feature-first Angular frontend and a .NET 10 modular backend.

## Process boundary

```text
Browser -> low-privilege API -> ACL-protected local IPC -> privileged Agent -> Windows APIs
Terminal Manager -> explicit local management endpoints
```

- `apps/web` renders built-in features and validated plugin manifests.
- `apps/api` authenticates users, validates requests, serves OpenAPI, and coordinates operations.
- `apps/agent` is the only host allowed to perform privileged disk, Storage Spaces, volume, and SMB operations.
- `apps/manager` is the local terminal host. Spectre.Console is confined to this presentation boundary.
- The Agent must not expose a LAN listener or arbitrary command execution.

## Frontend boundaries

- `features/<name>` owns one built-in business capability. Root files are public; `lib/` is private.
- `shared` contains only business-neutral reuse and may not depend on features.
- `plugin-host` renders backend-validated JSON manifests through known renderer types.
- Frontend styles use Angular Material theme tokens. Global typography stays in the theme entry;
  boundary-specific presentation stays with its owning component.

## Backend boundaries

- API and Agent code is organized by vertical feature slices, not generic Controllers/Services/Repositories folders.
- `NasForWindows.Contracts` contains transport contracts and depends on no host.
- `NasForWindows.Operations` contains long-running operation concepts and depends on no host.
- `NasForWindows.PluginSdk` contains safe plugin manifest and action contracts.
- `NasForWindows.Windows` isolates Windows-specific adapters, including hardware inventory and
  metrics collection, and is referenced by the Agent only.
- The API must not reference the Agent or Windows adapter project.
- The Agent must not reference the API or Plugin SDK.
- The Manager must not reference the API host, Agent host, or Windows adapter project. Its presentation dependency must not leak into libraries.
- Hardware inventory and metrics flow through typed, current-user ACL-protected local IPC. The API
  never reads WMI, CIM, DXGI, performance counters, disks, or other Windows hardware APIs directly.

## Plugin boundary

Plugin JSON declares navigation, known page renderer types, and action IDs. It may not contain JavaScript, PowerShell, DLL paths, executable paths, or arbitrary endpoints. Third-party code must not be loaded into the privileged Agent process.

## Contracts

ASP.NET Core generates the OpenAPI document. The Angular API client will be generated into `apps/web/src/app/api-client/generated`. Plugin manifests are validated against `contracts/plugin-manifest.schema.json` by the backend before the frontend receives them.
