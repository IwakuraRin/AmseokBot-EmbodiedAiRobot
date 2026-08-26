# Feature-first layout

Each built-in capability owns its pages, feature-only UI, data access, and domain model. Only root files such as `routes.ts` are public entry points; `lib/` is private.

Planned features: `dashboard`, `disks`, `storage-pools`, `arrays`, `volumes`, `shares`, `operations`, `settings`, and `plugin-management`.

Features must not deep-import one another. Cross-feature workflows are composed by the app route layer or coordinated through backend APIs.
