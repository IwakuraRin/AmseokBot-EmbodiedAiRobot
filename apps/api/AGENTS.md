# API architecture

This project is the low-privilege ASP.NET Core HTTP boundary. Read the repository `AGENTS.md` and
`ARCHITECTURE.md` before changing it.

Read `WEB_ACCESS_SECURITY_ARCHITECTURE.md` before changing authentication, authorization, Web
users, roles, permissions, Owner bootstrap, sessions, cookies, antiforgery, or audit behavior.

- Organize behavior as vertical slices under `Features/<capability>/<use-case>`. Keep endpoints,
  validation, orchestration, and slice-local models together instead of adding generic
  Controllers/Services/Repositories folders.
- The API authenticates callers, validates requests, serves OpenAPI, and coordinates operations.
  It must not perform privileged disk, Storage Spaces, volume, SMB, or other Windows operations.
- The API may depend on `NasForWindows.Contracts`, `NasForWindows.Operations`, and
  `NasForWindows.PluginSdk` through project references.
- The API must not reference the Agent host or `NasForWindows.Windows`, import their code, or rely
  on their private implementation.
- Communicate with the Agent only through the authenticated, ACL-protected local IPC contract.
  Keep transport and external effects behind narrow interfaces.
- Plugin input is declarative validated data only. Reject executable paths, arbitrary commands,
  PowerShell, DLL loading, JavaScript, and arbitrary endpoints.
- Before adding a contract or operation concept, search the current slice and focused `libs`
  projects and reuse the existing public type when it owns the concept.
