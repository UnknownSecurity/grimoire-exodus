# Desktop UI shell

This directory contains the opt-in Vue 3 shell used by the desktop UI
modernization spike. It is build-time tooling only; Node.js and an HTTP server
are not shipped or started with Grimoire Exodus.

Build the packaged local assets from the repository root:

```powershell
Set-Location .\desktop-ui
npm install
npm run build
```

The build writes static files to `app\Resources\Shell`. The .NET project copies
that directory into the application output.

The legacy WinForms shell remains the default. Start a development build with
the modern shell by passing `--modern-ui` or setting
`GRIMOIRE_EXODUS_MODERN_UI=1`. Pass `--legacy-ui` to force the recovery shell;
that override always wins. The switch is deliberately not persisted during the
architecture spike.

The browser bridge is versioned and allowlisted. It does not expose arbitrary
filesystem, process, Flash, packet, account-vault, or .NET reflection access.
