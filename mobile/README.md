# Grimoire Exodus Mobile

This directory contains the additive Android port. The existing Windows client,
updater, installer, ActionScript bridge, and release paths remain in their
current locations and do not depend on this Gradle build.

## Current milestone (0.4.4-dev)

The Android work now has two additive surfaces:

- a Kotlin and Jetpack Compose management-shell prototype;
- a HARMAN AIR captive-runtime client under `air-client/` that loads AQW
  directly from AQW's live version endpoint;
- an AIR-native Grimoire command-center wrapper with the requested top menu,
  compact overlay drawer, authenticated character status, and dark violet game
  frame;
- an always-available in-client focus button that hides or restores the wrapper
  without translating or scaling AQW away from its native pointer plane;
- a clipped 960×550 AQW stage with AIR-managed physical display scaling, so
  rendered targets and Android X/Y input share the same logical coordinates;
- an immersive, landscape-only dark-purple command center that hides Android's
  status and navigation bars during play;
- Grimoire-owned launcher, emblem, and banner artwork;
- a typed `GameTransport` boundary with an inert offline implementation for
  Compose-side development;
- a versioned, fail-closed game-bridge handshake validator;
- Android document-picker import backed by a defensive, bounded, data-only
  legacy `.gbot` reader, plus repository-controlled Community bots;
- a compact, paged mobile Bot Manager with select/import/refresh/delete,
  start/pause/stop, and a cooperative fail-closed runner for foundational map,
  combat, quest, item, timing, and flow-control commands;
- persistent General, hardware Key Bindings, and Advanced settings adapted from
  the desktop global settings surface;
- shared, anonymized compatibility fixtures and versioned bridge contracts.

The AIR client is the standalone game runtime; it does not launch, wrap, patch,
or control another installed AQW Android application. It downloads the current
AQW SWF at runtime instead of bundling an AQW binary. An unsupported GBot
command stops the run at that command rather than being guessed, dynamically
resolved, or skipped into an invalid state.

## HARMAN AIR integration decision

HARMAN AIR is the selected runtime. AIR owns the game surface and the existing
ActionScript bridge now has a host abstraction: desktop builds continue to use
`ExternalInterface`, while AIR builds retain callbacks locally and provide
safe standalone fallbacks. The previous idea of integrating with another AQW
Android APK is no longer part of the architecture.

## Modules

| Module | Responsibility |
| --- | --- |
| `app` | Android entry point and adaptive Compose shell |
| `air-client` | Standalone HARMAN AIR AQW runtime and APK build |
| `core:designsystem` | Grimoire-owned colors, type, shapes, and reusable surfaces |
| `core:model` | Platform-neutral mobile state and bridge model |
| `core:domain` | Transport boundary and safe offline implementation |
| `core:legacy-gbot` | Bounded, non-reflective legacy `.gbot` inspection |

Language-neutral contracts and fixtures live in `../shared` so Windows tests,
Android tests, documentation, and future tooling can consume the same source of
truth without either app importing the other's platform code.

## Build

From this directory:

```powershell
.\gradlew.bat test
.\gradlew.bat :app:assembleDebug
.\air-client\build-air.ps1 -Configuration Debug
```

The Android build uses its own wrapper and caches. It never invokes
`../exodus.ps1`, edits the desktop output, or launches either application.

The application ID is provisional until signing and distribution ownership are
approved. Do not publish development builds as an official release.
