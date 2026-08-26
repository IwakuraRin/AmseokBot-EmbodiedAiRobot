# Web Access Security Architecture

## Scope

This architecture governs management-platform authentication and authorization. It does not
govern Windows or SMB accounts.

- Web users decide who can enter and operate the management platform.
- Windows/SMB users decide who can access shares and files.
- The two identities are never implicitly linked. A future mapping must be explicit and audited.

The first phase includes local Owner bootstrap, Cookie authentication, fixed roles, policy-based
authorization, session discovery, Web-user administration, and audit records. Custom roles,
plugin-defined grants, Windows domain authentication, multi-factor authentication, and SMB-user
mapping are later phases.

## Process and trust boundaries

```text
Browser -> HTTPS + HttpOnly Cookie -> low-privilege API
Manager -> loopback bootstrap endpoint -> low-privilege API
API -> ACL-protected typed IPC -> privileged Agent -> Windows APIs
```

- The API owns Web users, passwords, roles, permissions, sessions, and their SQLite database.
- The Manager is presentation only. It asks the API to generate a bootstrap token and displays it;
  it never opens or writes the identity database.
- The Agent never reads the identity database, browser Cookie, antiforgery token, role, or
  permission table. It trusts only the API service identity over protected local IPC and still
  validates typed operation parameters and system state.
- Browser identity or actor ID may cross IPC only as audit metadata, never as Agent authorization.
- Arbitrary PowerShell, command lines, executable paths, DLL paths, and plugin code are forbidden.

## Fixed roles and permissions

The role names are fixed and cannot be created, renamed, or deleted through the product.
Authorization checks permissions, never role names at individual endpoints.

| Role | Permissions |
| --- | --- |
| Owner | Every core permission |
| Operator | `system.overview.read`, `storage.read`, `storage.manage`, `shares.read`, `shares.manage`, `operations.read`, `operations.cancel` |
| Viewer | `system.overview.read`, `storage.read`, `shares.read`, `operations.read` |

The core permission catalog is owned by the API:

- `system.overview.read`
- `storage.read`
- `storage.manage`
- `storage.destroy`
- `shares.read`
- `shares.manage`
- `operations.read`
- `operations.cancel`
- `web.users.manage`
- `plugins.manage`
- `settings.manage`
- `audit.read`

Formatting a disk, deleting a storage pool, deleting a volume, or performing any other irreversible
storage action requires `storage.destroy`; `storage.manage` is insufficient. Cancelling a
destructive operation also requires the original destructive permission.

Permissions default to denied. Every API endpoint is authenticated by the fallback policy unless
it explicitly opts into anonymous access, and every protected operation declares its permission
policy. Unknown permissions never succeed.

## Authentication and browser security

- ASP.NET Core Identity owns user names, password hashing, roles, security stamps, lockout, and
  authentication tokens.
- EF Core SQLite stores Web users, fixed-role assignments, bootstrap state, and audit events.
- No public registration endpoint exists.
- The authentication Cookie is HttpOnly, Secure in production, same-site, host-only, and never
  stored in browser JavaScript storage.
- Production traffic uses HTTPS and HSTS.
- A separate JavaScript-readable `XSRF-TOKEN` Cookie is paired with the `X-XSRF-TOKEN` header.
  Every browser-originated POST, PUT, PATCH, and DELETE request validates antiforgery tokens.
- Login throttling uses independent IP and normalized-user-name limits. Error responses do not
  reveal whether a user exists, is disabled, is locked, or supplied a bad password.
- Data Protection keys are persisted outside the application binaries and protected with Windows
  DPAPI in production.

The authentication Cookie identifies the user. Permission resolution checks the current enabled
user and current role assignment through one API-owned resolver. `/api/session` uses that same
resolver so UI capabilities and server authorization cannot drift.

## First Owner bootstrap

1. The API starts with fixed roles but no users and an incomplete bootstrap state.
2. The local Manager requests token generation over a loopback-only endpoint.
3. The API generates a cryptographically random token, stores only its hash and ten-minute expiry,
   and returns the raw token once for terminal display.
4. A user enters the token and new Owner credentials in a browser running on the API host.
5. The API atomically consumes the token, creates the first enabled Owner, and permanently marks
   bootstrap complete.

The raw token is never placed in a URL or log. Token generation and Owner creation are rejected
off loopback. Only one active token exists. The last enabled Owner cannot be deleted, disabled, or
demoted; this invariant is enforced inside the API's user-administration transaction, not by UI.

## Frontend responsibility

`GET /api/session` returns the current user, role names, and effective permissions. Angular uses
permissions only to improve navigation, route, and button behavior. It never branches on a role
name for security and never treats a hidden control as authorization. The API reauthorizes every
request.

Built-in navigation may reference core permission identifiers, but the role-to-permission mapping
exists only in the API. API response types are generated through OpenAPI when generation is
available.

## Audit

Security-relevant outcomes are recorded with UTC time, actor, action, target, outcome, source IP,
request correlation ID, and sanitized details. Login attempts, bootstrap, Web-user changes,
authorization failures, and destructive operations are auditable. Passwords, Cookie values,
antiforgery values, raw bootstrap tokens, and other secrets are never recorded.

Audit records are append-only through product APIs. Reading them requires `audit.read`; no product
endpoint deletes or rewrites them.

## Code ownership

```text
apps/api/Features/WebAccess/
  Authentication/
  Authorization/
  Bootstrap/
  Persistence/
  Session/
  Users/

apps/api/Features/Audit/

apps/web/src/app/core/
  authorization/
  session/

apps/web/src/app/features/
  authentication/

apps/web/src/app/layout/lib/app-shell/
```

`WebAccess` is one cohesive API boundary because authentication, fixed roles, permission
resolution, bootstrap, and user administration share one identity model and transaction boundary.
Audit remains a focused feature with a narrow writer interface used by WebAccess.
