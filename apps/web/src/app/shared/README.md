# Shared boundaries

`shared` only owns business-neutral, reusable capabilities. A component stays inside its feature until at least two features need the same behavior.

- Import shared UI through focused files under `shared/ui`, such as `shared/ui/page-header`.
- Do not place domain models, stores, API clients, or feature orchestration here.
- Do not create a module that re-exports all Angular Material components.
