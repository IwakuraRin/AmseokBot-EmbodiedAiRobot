# Manager architecture

This project is the local terminal presentation boundary. Read the repository `AGENTS.md` and
`ARCHITECTURE.md` before changing it.

- The Manager presents explicit local management workflows. It does not own storage, sharing,
  plugin, or operation business logic.
- Keep Spectre.Console and all terminal rendering inside this project. No library may depend on
  Spectre.Console or terminal presentation types.
- The Manager must not reference the API host, Agent host, or `NasForWindows.Windows` project.
- Call stable management contracts through a narrow local client seam; do not import host
  implementation or bypass the defined process boundary.
- Do not add arbitrary shell or PowerShell execution.
- Before introducing a model or operation concept, search the focused `libs` projects and reuse
  its public contract where appropriate.
