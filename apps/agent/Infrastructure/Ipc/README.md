# Local IPC boundary

The API must communicate with the privileged Agent through an ACL-protected local transport such as a Windows named pipe. This boundary accepts typed commands only; it must never expose arbitrary PowerShell, executable paths, or unrestricted shell arguments.
