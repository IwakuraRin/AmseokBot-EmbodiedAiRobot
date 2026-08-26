# Repository instructions

Read `ARCHITECTURE.md` before changing code.

- Preserve process, feature, project, and public-entry-point boundaries.
- Frontend features may not deep-import another feature's `lib/` folder.
- The API may not reference the Agent or `NasForWindows.Windows`.
- The Agent may not reference the API or `NasForWindows.PluginSdk`.
- The Manager may not reference the API, Agent, or `NasForWindows.Windows` projects.
- Keep Spectre.Console inside the Manager presentation boundary; libraries must not depend on it.
- Do not add generic `shared`, `common`, `helpers`, or `utils` dumping grounds.
- Keep Windows, filesystem, network, clock, and process behavior behind narrow seams.
- Do not expose arbitrary PowerShell or shell execution.
- Do not add custom frontend styles unless the user explicitly changes that decision.
- Run `npm run check` before handing off code changes.
