# API client boundary

This boundary will isolate ASP.NET Core transport details from UI features. Add focused public entry points such as `system.ts` or `plugins.ts`; keep generated clients and transport implementation under `lib/`.

The API client must not import UI, layout, or feature implementation files.
