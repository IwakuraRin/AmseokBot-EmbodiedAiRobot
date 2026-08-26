# Privileged Agent architecture

This project is the privileged Windows service boundary. Read the repository `AGENTS.md` and
`ARCHITECTURE.md` before changing it.

- Organize business operations as vertical slices under `Features/<capability>`. Keep local IPC
  and host-specific wiring under `Infrastructure` or at the composition root.
- The Agent is the only host allowed to perform privileged disk, Storage Spaces, volume, and SMB
  operations. Access Windows APIs through focused adapters in `NasForWindows.Windows`; do not
  spread Windows calls through feature orchestration.
- The Agent may depend on `NasForWindows.Contracts`, `NasForWindows.Operations`, and
  `NasForWindows.Windows` through project references.
- The Agent must not reference the API host or `NasForWindows.PluginSdk`.
- Expose management operations only over authenticated, ACL-protected local IPC. Never add a LAN
  listener, arbitrary command execution, PowerShell passthrough, or plugin code loading.
- Keep privileged effects behind narrow interfaces so orchestration can be tested without real
  disks, shares, processes, or the system clock.
- Before adding an operation, contract, or adapter, search the current feature and focused `libs`
  projects for an existing public capability.
