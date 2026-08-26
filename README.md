# NasForWindows

Monorepo for a Windows storage-management application.

## Repository layout

- `apps/web`: Angular 22 + Angular Material frontend.
- `apps/api`: low-privilege ASP.NET Core API and frontend host.
- `apps/agent`: privileged Windows Service for destructive storage operations.
- `apps/manager`: local terminal management host using Spectre.Console.
- `libs`: typed contracts, operation models, plugin SDK, and Windows adapters.
- `contracts`: OpenAPI and plugin-manifest schemas.
- `tests`: API, Agent, and architecture tests.

## Development commands

```bash
npm run dev:web
npm run dev:api
npm run dev:agent
npm run dev:manager
npm run check
```

The API and Agent are separate local processes. The browser must never communicate with the privileged Agent directly.
