# Grimoire Exodus AIR client

This module is the standalone Android game runtime. It compiles the checked-in
Grimoire ActionScript source with HARMAN AIR, downloads the current AQW client
from AQW's own version endpoint at runtime, and packages a captive-runtime APK.
It does not bundle or depend on another Android application.
The loading surface, launcher icon, and in-game command-center wrapper reuse
the Grimoire-owned dark/violet visual language from the Compose prototype.

The AIR bridge preserves the existing desktop `ExternalInterface` callbacks.
When no desktop host exists, the same callbacks are retained in a local
registry for the Android controller. Server-list responses fall back to the
original AQW response so login remains functional without a native filter.

## Prerequisites

- HARMAN AIR SDK 51.4, either in `AIR_HOME` or the ignored
  `.tools/AIRSDK_51.4.1.2` directory;
- JDK 17 or 21;
- the AIR SDK license appropriate for the developer or organization.

The AIR SDK and signing material are development tools. They must not be
committed or placed in an APK or release archive.

## Build

From the repository root:

```powershell
.\mobile\air-client\build-air.ps1 -Configuration Debug
```

The development APK is written to `dist/`. It is signed with an ignored local
development certificate and must not be published as a release build.

For a release build, provide a protected keystore through
`GRIMOIRE_ANDROID_KEYSTORE` and `GRIMOIRE_ANDROID_KEYSTORE_PASSWORD`, then use
`-Configuration Release`. Never commit the keystore or its password.

## Runtime boundary

The packaged bootstrap performs these steps:

1. requests AQW's current version metadata over HTTPS;
2. downloads the selected AQW SWF into memory;
3. shows real byte progress in the AIR stage;
4. imports the SWF with `LoaderContext.allowCodeImport`;
5. initializes the Grimoire callback registry after AQW is ready.

After initialization, AQW remains a direct child of the AIR stage while the
top menu and sidebar are drawn in a separate overlay. AIR maps the physical
Android display into AQW's native 960×550 Stage plane. The wrapper never moves
or rescales AQW itself, fixing the mismatch between legacy Stage-based pointer
reads and the displayed touch target. A local 960×550 scroll rectangle still
contains world rendering and hit targets. The compact drawer overlays the left
edge only while it is open, and the top-left focus button hides or restores the
wrapper without taking permanent game space.

The AIR Bot Manager lists packaged Community bots and bots imported through the
Android document picker. Its compact list is paged so every entry remains
reachable without expanding the overlay. Imports are copied into
application-owned storage, bounded by file and command limits, and parsed as
JSON data without resolving Newtonsoft `$type` names. Start, pause/resume, stop,
refresh, and imported-bot removal are live. A cooperative mobile runner supports
a foundational subset of legacy timing, join/movement, combat, skill, quest,
inventory, label/index, and stop/restart commands. An unknown command stops the
run with its type in the visible status; this is not yet command-for-command
parity with desktop.

The settings drawer persists General, Key Bindings, and Advanced preferences.
Mobile adaptations include start-in-focus, status visibility, back-button exit
confirmation, reduced motion, keep-awake, 30/60 FPS, render quality, hardware
F-key shortcuts, login-field clearing, app-owned settings reset, and an official
GitHub mobile-release check. Android has no desktop tray behavior, so it is not
presented as a mobile option.

The profile card remains anonymous until AQW confirms `loadInventoryBig`, then
reads the authenticated character name and any available level/class data from
the live game state. Typed credentials are never treated as an authenticated
profile. Mobile AIR targets 30 FPS at standard display density to reduce the
rendering and pixel-fill cost of the legacy vector scene; desktop frame and
display behavior are unchanged.

Because AIR imports the main AQW client from downloaded bytes, the bootstrap
restores AQW's official HTTPS game and asset origins before login. This keeps
the post-login `Assets_*.swf` request away from AIR's synthetic
`app:/[[DYNAMIC]]` URL and prevents the AQW “Client Initialization Failed” path.

AQW itself remains visually dominant. The mobile runner is intentionally
smaller than the mature desktop engine; subsequent milestones will add commands
only behind compatibility fixtures and live-client validation.

## Validation status

The bootstrap has been smoke-tested with AIR Debug Launcher in the mobile
profile: it retrieved the live version metadata, downloaded the current AQW SWF
to 100%, initialized the client, and loaded the login-screen background and
title. The APK manifest and arm64 payload have also been inspected and its APK
Signature Scheme v2 signature verified. A physical Android device or configured
emulator is still required to validate rendering, touch input, login, sockets,
system-bar behavior, pause/resume, and long-session stability on Android.
