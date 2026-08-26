# Repository instructions

## Required reading before changes

Before modifying code, read `ARCHITECTURE.md` and every applicable, more deeply scoped
`AGENTS.md` file:

- Frontend work: `apps/web/AGENTS.md`.
- API work: `apps/api/AGENTS.md`.
- Web identity, authorization, user administration, bootstrap, session, or audit work:
  `apps/api/WEB_ACCESS_SECURITY_ARCHITECTURE.md`.
- Privileged Agent work: `apps/agent/AGENTS.md`.
- Terminal Manager work: `apps/manager/AGENTS.md`.
- Shared backend library work: `libs/AGENTS.md`.

Do not start an implementation until the owning boundary, its public entry points, its callers,
and its tests have been identified.

## Repository-wide prohibitions

- Never modify `README.md` or any other README file.
- Never push, force-push, publish a branch, create a pull request, or change a remote unless the
  user has explicitly approved that remote action in the current request. A request to edit,
  commit, or finish code is not permission to push.
- Do not replace or migrate the project's existing frameworks, package manager, test frameworks,
  architectural style, or application layout. Keep Angular with Angular Material on the frontend
  and the existing modular .NET architecture on the backend.
- Do not expose arbitrary PowerShell, shell, executable, DLL, or command execution.
- Do not add generic `shared`, `common`, `helpers`, or `utils` dumping grounds.

## Reuse before implementation

Before implementing any capability, search the owning feature or slice and the established reuse
boundaries for an existing implementation.

- Frontend: inspect `apps/web/src/app/shared`, then the current feature, `layout`, `core`,
  `api-client`, and `plugin-host` as applicable.
- Backend: inspect the current vertical slice and the focused projects under `libs`.

Reuse an existing public capability when it fits. Extend its owner when the new behavior belongs
there. Do not duplicate components, models, services, helpers, contracts, or adapters. Do not move
business-specific behavior into `shared` merely to make it reusable.

## Architecture and dependency rules

- Preserve process, feature, project, and public-entry-point boundaries described in
  `ARCHITECTURE.md` and the scoped `AGENTS.md` files.
- Cross a boundary only through its declared public entry point or project reference. Never
  deep-import another boundary's private implementation.
- Keep dependency direction explicit and acyclic. Do not weaken boundary checks to make a change
  pass.
- Keep Windows, filesystem, network, clock, process, and vendor behavior behind narrow seams.
- Keep tests aligned with the public behavior of the boundary they protect. Do not expose private
  implementation solely for tests.
- Do not add custom frontend styles unless the user explicitly requests that styling change.

## Verification

Run `npm run check` from the repository root before handing off any code change. Report the exact
failing command and cause if a required check cannot pass.
