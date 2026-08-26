# Backend library architecture

These projects are focused backend library boundaries. Read the repository `AGENTS.md` and
`ARCHITECTURE.md` before changing them.

## Project ownership

- `NasForWindows.Contracts` owns host-independent transport contracts. It depends on no host and
  contains no orchestration or Windows implementation.
- `NasForWindows.Operations` owns host-independent long-running operation concepts. It depends on
  no host and contains no presentation or transport implementation.
- `NasForWindows.PluginSdk` owns safe, declarative plugin manifest and action contracts. It must
  not expose executable code, PowerShell, DLL paths, arbitrary endpoints, or privileged APIs.
- `NasForWindows.Windows` owns Windows-specific adapters for disks, Storage Spaces, volumes, SMB,
  and related OS behavior. The privileged Agent is its only host consumer.

## Boundary rules

- Keep each library's public surface small and intentional. Do not add a type to a library only
  because two callers happen to need similar code; place it with the concept's true owner.
- Do not add generic shared/common/helpers/utils libraries or folders.
- Libraries must not reference application hosts. They must not depend on Angular, ASP.NET host
  composition, Spectre.Console, or terminal presentation.
- Isolate filesystem, network, clock, process, Windows API, and vendor behavior behind focused
  interfaces when substitution or testing requires a seam.
- Before adding a type or adapter, search all focused library projects and the calling slice for
  an existing public capability. Extend the correct owner instead of creating a duplicate.
- Preserve an acyclic dependency graph and the project-reference restrictions enforced by the
  architecture tests.
