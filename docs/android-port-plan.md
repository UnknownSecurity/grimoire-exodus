# Grimoire Exodus for Android — feasibility and delivery plan

**Status:** Standalone HARMAN AIR client foundation in progress

**Research date:** 2026-10-16

**Product name:** Grimoire Exodus

**Target:** Android phones and tablets in landscape

## 1. Executive summary

Grimoire Exodus Android is a standalone AQW client. It does not depend on,
launch, patch, or control another AQW Android application. HARMAN AIR owns the
game surface, while the checked-in Grimoire ActionScript bootstrap requests the
current AQW version metadata and loads the live game SWF from `game.aq.com`.

The recommended target architecture is:

- a **HARMAN AIR** Android application that owns the AQW display surface;
- an AIR-safe host abstraction that preserves the desktop
  `ExternalInterface` contract and registers the same callbacks locally;
- Kotlin + Jetpack Compose retained for management-UI experiments and future
  native extensions, not as a separate game-hosting application;
- a newly separated, platform-neutral bot model and execution engine;
- a strict legacy `.gbot` import/export adapter with golden compatibility tests;
- Android-native storage, credentials, lifecycle, notifications, updates, and recovery;
- no accessibility scraping, another-app control, APK repackaging, or rooting.

The first live milestone is an AIR proof-of-concept that visibly reaches AQW's
login screen on Android and completes the local bridge handshake. Public
distribution still requires the normal AIR SDK license, branding, terms,
signing, and release reviews, but another Android app is not a technical
dependency.

### Implementation checkpoint 0

The repository now uses a gradual monorepo boundary instead of moving the stable Windows project:

- `app/`, `game-client/`, `installer/`, `tests/`, and `exodus.ps1` retain their existing desktop responsibilities and paths;
- `mobile/` owns the independent Gradle build and Android modules;
- `shared/` contains language-neutral contracts and anonymized compatibility fixtures only;
- the Compose shell uses an inert `GameTransport`, so its early UI and parser
  work cannot accidentally act on a live client;
- the first legacy GBot milestone began with bounded, non-reflective inspection and semantic round-trip testing;
- the 0.2.0 development build is landscape-only and immersive, reuses only
  Grimoire-owned visual assets, and exposes safe document-picker GBot inspection;
- `mobile/air-client/` compiles the Grimoire ActionScript source with HARMAN AIR
  and emits a captive-runtime, arm64 APK;
- the 0.3.0 development client downloads the current AQW SWF at runtime, shows
  byte progress, and keeps the Windows bridge ABI intact.
- the 0.4.0 development client hosts AQW inside the requested Grimoire top bar
  and contextual sidebar, with an in-client focus button that fades the chrome
  and smoothly zooms AQW between framed and full-screen layouts.
- the 0.4.3 development client clips the live 960×550 stage to contain rendering
  and hit targets, moves the focus control to the viewport's top-left, binds the
  profile card to authenticated character state, and lowers mobile render load.
- the 0.4.4 development client fixes touch targeting by retaining AQW's native
  Stage coordinate plane, turns the wrapper into a compact overlay drawer, adds
  persistent mobile global settings, and connects a bounded Bot Manager to an
  explicit fail-closed subset of the legacy command vocabulary.

This boundary is intentionally additive. A later move to `platforms/windows/` would provide little functional value while breaking scripts, project references, release automation, and contributor workflows, so it is deferred unless a separate migration proves that every desktop entry point remains compatible.

## 2. Reality check

“Port everything” contains three different kinds of work:

1. **Product behavior** can usually be recreated: Home, Bot Manager, settings, diagnostics, bot progress, update notices, and theming.
2. **Portable contracts** can be translated with care: command names, command order, configuration values, game models, bot lifecycle rules, and `.gbot` JSON metadata.
3. **Desktop mechanisms** do not port: WinForms, .NET Framework 4.8, ActiveX/COM, `Flash.ocx`, synchronous `ExternalInterface` calls through `AxShockwaveFlash`, Windows hooks, the tray, WiX, MSI, the current updater, and in-process desktop plug-ins.

The desktop client works because Grimoire owns the Flash host and injects an
ActionScript bridge around the live AQW SWF. Android now follows the same
ownership model: Grimoire's AIR package owns the stage and imports the live SWF
with `LoaderContext.allowCodeImport`. It does not inspect another process. A
future AIR Native Extension can add Android-native services inside this same
signed package.

Artix's current Terms of Service also prohibit reverse engineering, protocol interception/emulation/redirection, modifying or adding components to an Artix game, third-party game interfaces, and software that intercepts or mines game information. This is a product stop sign, not merely an engineering inconvenience. Written Artix approval is required before distributing a native integration. This document is an engineering plan, not legal advice.

## 3. Verified research findings

### 3.1 AIR standalone client path

The maintained HARMAN AIR SDK supports Android captive-runtime APKs, runtime
SWF loading, and native extensions. The repository already contains a Grimoire
loader that obtains AQW's version metadata and game SWF from the live AQW
endpoints. The Android path therefore reuses that loader and replaces only its
host boundary:

```text
AQW live SWF from game.aq.com
        ↕ ActionScript objects and events
Grimoire AIR loader + local callback registry
        ↕ typed host adapter / future ANE
Android bot engine and management UI
```

Desktop `ExternalInterface` remains active when available. In AIR, callbacks
are registered locally, external notifications become typed events, website
opening uses AIR APIs, and the unmodified AQW server-list response is preserved.
Device validation must still prove login, map loading, input, networking,
lifecycle, and long-session stability.

### 3.3 Policy and distribution constraints

- Terms, AIR SDK licensing, branding, automation behavior, and distribution
  remain release-review items. They do not require integrating with another
  installed Android application.
- Google Play allows some narrow, deterministic AccessibilityService automation, but prohibits an app from autonomously initiating, planning, and executing actions or decisions through that API. Non-accessibility tools also require declaration, review, prominent disclosure, and consent. Accessibility cannot provide Grimoire's internal state or deterministic packet/game callbacks anyway, so it is rejected as the primary architecture.
- Android's `PackageInstaller` normally requires user intervention for an ordinary app update. A GitHub-distributed Android app may download and verify an APK, but it must hand installation to the system. It cannot silently merge executable files like the Windows portable updater.
- Long-running user-visible work is constrained by Android background-execution and battery rules. A bot session would require an explicit, user-started foreground service and persistent notification, and it must fail safely when Android stops the process.

## 4. Goals and non-goals

### Goals

- Preserve the Grimoire Exodus identity while delivering a first-class Android experience.
- Reach functional parity by capability, not by copying desktop mechanisms.
- Load existing legacy `.gbot` files safely without changing command order or meanings.
- Keep desktop Grimoire working throughout the mobile program.
- Make every feature explicitly aware of the connected game client's supported capabilities and version.
- Protect credentials, bot files, recovery data, and diagnostics on-device.
- Support phones, tablets, rotation, dark/light themes, and touch-first workflows.
- Provide honest compatibility and safety states when the game client changes.

### Non-goals

- Repackaging, launching, modifying, or controlling another AQW Android APK.
- Bypassing another application's sandbox, hooking its process, or using root.
- Reimplementing or hosting Artix's game protocol.
- Shipping Artix game binaries, SWFs, artwork, names, or other assets without a suitable license.
- Pretending an AccessibilityService or screen-coordinate macro has full Grimoire parity.
- Running bots invisibly, restarting them after a crash without consent, or defeating Android battery controls.
- Loading Windows `.dll` plug-ins on Android.
- Replacing the desktop project during the Android build-out.

## 5. Integration options

| Option | Standalone | Potential parity | Main benefit | Main blocker | Decision |
| --- | --- | ---: | --- | --- | --- |
| Grimoire-owned HARMAN AIR app loading the live AQW SWF | Yes | Full | Reuses the existing loader and callback vocabulary | Requires device proof, lifecycle hardening, and release review | **Selected route** |
| AIR Native Extension inside the Grimoire package | Yes | Full | Adds Android services and native UI without a second app | Requires a carefully versioned ANE boundary | **Future native integration** |
| Accessibility, screen capture, or image recognition | Superficially | Low | Does not require an internal client API | Fragile, no internal state, Android/Play policy risk, account risk | **Reject** |
| Root, runtime hooking, instrumentation, or APK repackaging | Superficially | Potentially high | Can reach internals on test devices | Terms, security, signing, compatibility, anti-cheat, and distribution risk | **Reject** |
| Independent protocol/client reimplementation | Yes | Theoretical | Full ownership of UI | Terms prohibit interception/emulation; enormous server and rendering scope | **Reject** |

## 6. Recommended product architecture

### 6.1 Product shape

Build a native Android shell around a replaceable `GameTransport` boundary. Do not let bot commands call Android views, AIR, sockets, or static singletons directly.

```text
Compose UI
  ├─ Home / client status
  ├─ Bot library and editor
  ├─ Run monitor and logs
  ├─ Accounts and settings
  └─ Diagnostics and updates
          ↓ intents / immutable state
Application services
  ├─ BotLibrary
  ├─ BotRunCoordinator
  ├─ CompatibilityCenter
  ├─ AccountVault
  ├─ DiagnosticsExporter
  └─ UpdateCoordinator
          ↓
Portable domain
  ├─ Bot document model
  ├─ Command registry and validators
  ├─ Execution state machine
  ├─ Game snapshots and events
  └─ Legacy GBot adapter
          ↓
GameTransport interface
  ├─ AirHostTransport            standalone runtime
  ├─ DesktopCompanionTransport  fallback/MVP
  └─ FakeGameTransport          tests and offline editor
```

### 6.2 Game transport contract

The transport should expose typed capabilities instead of one unbounded reflection-like `Call(name, args)` surface:

- `capabilities`: supported commands, events, live client identity, bridge version;
- immutable snapshots for player, world, inventory, quests, bank, shop, skills, and connection state;
- ordered event streams for packets already approved for use, drops, quests, maps, login, and disconnect;
- typed actions such as join, move, attack, use skill, accept/complete quest, equip, buy, sell, and bank;
- bounded diagnostics that never contain credentials or raw packet bodies by default;
- request identifiers, timeouts, explicit error types, and cancellation;
- a compatibility handshake that fails closed when the AQW client or bridge is unknown.

The bot engine may offer command authors a sequential/synchronous-looking API, but the adapter underneath must use coroutines and serialized message processing. Blocking Android's main thread or assuming the desktop COM call timing would be unsafe.

## 7. Proposed tech stack

Use current stable versions at implementation kickoff and pin them in a Gradle version catalog.

| Area | Recommendation | Reason |
| --- | --- | --- |
| Game runtime | HARMAN AIR + ActionScript 3 | Owns the AQW stage and reuses the current Grimoire bridge source |
| Android services | Kotlin | First-class Android APIs and future ANE implementation |
| Management UI | AIR-first overlay; Compose retained for native experiments | Avoids a second application/activity owning the game surface |
| App architecture | Unidirectional data flow; ViewModels; immutable screen state | Testable lifecycle behavior and predictable recovery |
| Concurrency | Kotlin coroutines, `Flow`, structured concurrency | Cancellation and ordered event processing without UI blocking |
| Dependency injection | Hilt | Android lifecycle integration and replaceable test adapters |
| Local structured data | Room | Bot index, compatibility metadata, recovery markers, and sanitized history |
| Preferences | DataStore | Theme, user options, feature flags, and non-secret settings |
| JSON | `kotlinx.serialization` plus a custom allowlisted legacy reader | Safe modern models while explicitly handling Newtonsoft `$type` metadata |
| Networking | OkHttp with a small typed protocol layer | Mature TLS/WebSocket support for an approved bridge or companion host |
| Secure storage | Android Keystore, encrypted envelopes, optional `BiometricPrompt` | Keeps encryption keys non-exportable and supports user-authenticated access |
| File access | Storage Access Framework | User-controlled import/export without broad storage permissions |
| Deferred work | WorkManager | Release checks, indexing, cleanup, and opt-in exports—not active bot execution |
| Active session | User-started foreground service + persistent notification | Android-compliant visibility and stop control for long-running work |
| Tests | JUnit, coroutine test, Turbine, property tests, Compose UI tests, instrumented device tests | Domain, stream, lifecycle, UI, and device coverage |
| CI artifacts | Signed APK/AAB, checksums, SBOM, provenance, reproducible version metadata | Supply-chain traceability |

### Why not make .NET MAUI the default?

.NET MAUI/current .NET for Android can share C# business logic and is a valid alternative. It does **not** make .NET Framework 4.8, WinForms, ActiveX/COM, Windows hooks, or the current Flash host portable. The bot engine also directly references `Root`, `BotManager`, `LogForm`, static game facades, and `Flash.Call`, so meaningful reuse first requires the same architectural extraction needed for a Kotlin port.

For an Android-only product, Kotlin/Compose has the best platform fit and lowest interop friction with an AIR native extension. Before committing, Phase 1 should time-box a comparison spike:

- Kotlin: parse/round-trip representative legacy fixtures and execute a small fake-transport command chain.
- Current-LTS .NET for Android/MAUI: retarget a deliberately extracted neutral subset and measure package size, startup, Android lifecycle integration, and AIR/Binder interop.

Choose .NET only if the spike proves substantial **real** core reuse without dragging desktop dependencies into the mobile architecture. “Same language” alone is not enough.

## 8. Reuse matrix

| Desktop area | Reuse level | Android treatment |
| --- | --- | --- |
| `.gbot` command names, namespaces, property meanings, defaults, and order | Contract | Preserve through a safe adapter and golden fixtures |
| `BotJson` allowlist/security intent | Design + tests | Reimplement an explicit type-name allowlist; never instantiate arbitrary types |
| `Configuration` and game data POCOs | Model semantics | Generate/port neutral immutable models; remove UI and transport dependencies |
| `IBotCommand` and sequential command behavior | Behavior | Port to suspendable commands using a `BotExecutionContext` |
| Bot lifecycle cancellation and stale-run protection | Design | Reuse the run-ownership rules; implement with structured concurrency |
| Individual command logic | Selective source translation | Port only after dependencies are behind typed game capabilities |
| Network handler parsing | Selective logic | Consume the AIR bridge event feed with input/size limits and typed results |
| ActionScript `grimoire` wrappers and callback vocabulary | Direct source reuse | Compile into the AIR bootstrap through `HostBridge` |
| `Flash.cs`, `AxShockwaveFlash`, `Flash.ocx`, COM manifest | None | Replace with `AirHostTransport`; never ship on Android |
| `Root.cs`, WinForms forms, designers, DarkUI controls | UX behavior only | Redesign as Compose screens and state holders |
| Home HTML and `GrimoireHomeBridge` | Visual/product concepts | Implement natively; do not expose a generic JavaScript bridge |
| Grimoire-owned emblem, icons, colors, and copy | Asset reuse after provenance check | Convert for Android density/vector needs |
| AQW/Artix artwork and binaries | No automatic reuse | Require written asset/redistribution permission |
| `AppPaths` | Requirements only | Replace with app-private storage, SAF import/export, and scoped cache |
| DPAPI account vault | Security intent | Replace with Keystore-backed encryption; never migrate plaintext |
| `UpdateService` and Windows updater | Design principles only | Separate Android release feed; verify signer/digest; system-confirmed install |
| Startup diagnostics and recovery | Behavior | Android session markers and redacted export; never auto-run recovered bots |
| Tray behavior and global keyboard hooks | Not applicable | Persistent notification, quick actions, and in-app shortcuts |
| Windows plug-in DLLs | None | Design a future signed, declarative extension format; no downloaded executable code |
| WiX/MSI/PowerShell packaging | None | Gradle, Android signing, APK/AAB, and Android CI |

The expected result is high reuse of specifications, fixtures, and carefully extracted algorithms, but low direct reuse of the current production binaries and UI code.

## 9. Legacy GBot compatibility

Existing GBots are a public contract. Android support should be additive and must not silently rewrite a user's file.

### 9.1 Safe import pipeline

1. Read through the Storage Access Framework with a hard size limit.
2. Parse JSON into a raw syntax tree with nesting and collection limits.
3. Accept only known `Grimoire` command/model `$type` names and explicitly supported framework collection types.
4. Map old assembly versions and `mscorlib` metadata to neutral command identifiers.
5. Preserve command order and unknown optional fields in a sidecar/raw node for lossless export where safe.
6. Validate labels, nested bot loads, paths, URLs, delays, and required capabilities without running the bot.
7. Show unsupported commands as read-only with an explanation; never guess their behavior.

### 9.2 Export strategy

- **Legacy-compatible export:** emit the existing Newtonsoft-style `$type` shape and internal `Grimoire` type names so current desktop releases can read Android-edited bots.
- **Future format:** design a versioned, platform-neutral `.gbot2` only after desktop and Android both support it. It must never replace legacy import.
- Golden tests must compare command type, order, values, defaults, and desktop↔Android round trips.
- A repository-owned anonymized fixture corpus should cover every command family, old assembly versions, missing fields, invalid types, malformed/deep JSON, and nested loads.

### 9.3 Execution compatibility

Each command is assigned one of:

- `Supported` — behavior and timing pass parity tests;
- `SupportedWithMobileDifference` — difference is documented and safe;
- `UnavailableInClient` — the approved bridge does not expose a required capability;
- `NotYetPorted` — loads and displays but cannot run;
- `RejectedUnsafe` — requires a mechanism outside the approved contract.

A bot may start only when every reachable command is supported or the user explicitly chooses a validated partial/test mode that cannot affect the live game.

## 10. Android UX and navigation

### Primary destinations

- **Home:** client status, compatibility, account, last/recent bot, update state, and diagnostics.
- **Bots:** library search, Community/User filters, favorites, import/export, details, and validation.
- **Editor:** touch-friendly command list, property sheets, labels, drag/reorder, undo, and validation.
- **Run:** start/stop/pause, current command, elapsed time, wait reason, retry/reconnect state, and bounded logs.
- **Game:** embedded HARMAN AIR AQW surface owned by Grimoire Exodus.
- **Tools:** only capabilities explicitly approved by Artix; dangerous/raw tools stay absent unless separately authorized.
- **Settings:** general, appearance, key/quick actions, privacy, compatibility, updates, and destructive reset actions.

Phones use a compact landscape top navigation and full-screen workspaces.
Tablets retain the landscape navigation rail and list/detail panes. The desktop
layout should inform terminology and grouping, but should not be squeezed into
a narrow viewport.

### Visual design direction

The supplied dark fantasy dashboard is a mood and layout reference, not a screen to clone. Grimoire Exodus should adopt its sense of a focused game command center while using original components, icons, artwork, and interaction patterns.

#### Landscape command center

- Use a slim branded top bar for primary navigation, connection state, and global actions.
- Place character/account context and high-frequency bot controls in a collapsible left rail.
- Keep the game surface dominant, targeting roughly 65–75% of the usable width on wide screens.
- Use a resizable bottom dock for run state, logs/chat, and quick actions rather than desktop-style floating windows.
- Reserve compact right-edge actions for contextual game controls that must remain reachable without covering the play area.
- Keep device, connection, client, room-safety, and bot status in a compact telemetry strip.
- Prefer anchored docks, sheets, overlays, and adaptive panes over freely floating or overlapping windows.

On a phone in landscape, the left rail collapses, navigation becomes icon-first,
and the bottom dock can become a resizable sheet. On tablets and foldables, the
rail and dock may remain visible when the game still has sufficient space. The
application is locked to sensor landscape and uses immersive system-bar hiding;
system bars remain transiently reachable with the Android edge-swipe gesture.

#### Color and component language

Start with these tokens, then validate them on real OLED/LCD devices and in accessibility testing:

| Role | Initial token | Intended use |
| --- | --- | --- |
| Canvas | `#05060B` | App and game-surround background |
| Surface | `#0B0D15` | Rails, docks, and primary panels |
| Elevated surface | `#121522` | Sheets, menus, dialogs, and cards |
| Control surface | `#191C2C` | Inputs, segmented controls, and inactive buttons |
| Violet | `#8B5CF6` | Focus, selection, borders, and primary actions |
| Bright violet | `#A855F7` | Restrained glow and emphasized actions |
| Pale violet | `#C4B5FD` | Secondary emphasis and selected text |
| Primary text | `#F7F5FF` | Titles and essential values |
| Secondary text | `#9B96B3` | Labels, hints, and less-important metadata |
| Success | `#22C55E` | Healthy/private/safe state |
| Warning | `#F59E0B` | Attention and potentially unsafe state |
| Danger | `#EF4444` | Errors, destructive actions, and incompatible client state |
| Informational | `#22D3EE` | Links, diagnostics, and log accents |

Use near-black/navy foundations, layered graphite panels, thin separators, restrained violet borders, and subtle radial highlights. Panels should be rounded rectangles with clear density and hierarchy. Circular controls should be limited to touch actions that genuinely benefit from a thumb-friendly game-control shape; normal navigation and settings remain conventional Android controls. Glow is an accent, not the primary way to communicate state.

#### Typography

- Use a distinctive, properly licensed serif/display family such as Cinzel or Marcellus for the Grimoire wordmark and a small number of major headings.
- Use Inter, Roboto, or an equivalently readable sans-serif family for controls, body copy, menus, logs, and editor content.
- Use tabular numerals for timers, ranks, counters, coordinates, and telemetry so changing values do not shift the layout.
- Verify redistribution, app-embedding, and modification rights before selecting final font files.

#### Accessibility and ownership boundaries

- Use at least 48 dp touch targets, scalable type, high-contrast text, safe-area/system-bar handling, and a reduced-motion option.
- Never rely on red, orange, green, or violet alone; pair status color with an icon, label, or accessible description.
- Provide an accessible high-contrast dark variant if the atmospheric theme cannot meet contrast requirements without losing its intended hierarchy.
- Reuse only Grimoire-owned branding in the initial design. Do not copy the reference image's character art, game HUD, icons, or AQW imagery.
- Treat Artix characters, logos, screenshots, and other game assets as unavailable until written permission and license terms explicitly cover their use.

#### Design acceptance criteria

- The interface has a recognizable Grimoire dark-fantasy identity without imitating Artix's UI or the reference screen.
- The game remains visually dominant in landscape while start, stop, pause, and bot status stay reachable in one tap.
- Every destination works at supported phone, tablet, and foldable widths; portrait provides a deliberate management experience.
- Dark and high-contrast variants pass contrast, large-text, touch-target, screen-reader, and reduced-motion review.

### Android equivalents

| Desktop behavior | Android behavior |
| --- | --- |
| System tray | Ongoing notification with pause/stop/open actions |
| Desktop shortcut | Launcher icon and optional pinned shortcut |
| Hotkeys | In-app shortcuts, notification actions, and supported hardware-key handling |
| Multiple child windows | Routes, sheets, and list/detail panes |
| File watcher bot dropdown | Scoped bot library index refreshed by user/import events |
| Close confirmation | Stop/leave-running decision when navigating away; no hidden accidental termination |
| Window title theme | System bars and Material theme following light/dark settings |

## 11. Accounts, security, and privacy

- Prefer Artix-owned authentication. Grimoire should receive a short-lived approved session/token or client state rather than handling a password whenever possible.
- If credentials are explicitly required, encrypt them with a per-install Keystore key; offer biometric/device-credential protection; never log, export, sync, or return decrypted passwords to UI state.
- Keep bot files, database, logs, recovery, and update caches in app-private storage. Use SAF for deliberate import/export.
- Pair a desktop companion with an on-screen QR code, ephemeral key exchange, mutual authentication, explicit device naming, revocation, and local-network-first transport. Do not expose the desktop host directly to the internet.
- Bound every event/log queue by item and byte limits. Redact account names, room identifiers, file names, packet bodies, and device paths from support exports.
- Diagnostics export must show a preview and require explicit consent.
- Do not include analytics or crash upload by default. If later added, make it opt-in and privacy-preserving.
- Treat bot files as untrusted input. No reflection-based type creation, script evaluation, dynamic code download, arbitrary file access, or process execution.

## 12. Lifecycle, recovery, and performance rules

- A bot starts only from a visible, explicit user action.
- Active execution owns a foreground service and persistent notification.
- Screen off, rotation, process recreation, network loss, and activity recreation must not duplicate a run.
- A run has a unique ID, cancellation scope, immutable configuration snapshot, and serialized game-action queue.
- On process death or crash, record that a bot was active and offer review/recovery after relaunch. Never automatically resume or execute it.
- On bridge/client version change, pause automation until compatibility is revalidated.
- WorkManager handles release checks, library indexing, retention cleanup, and user-approved export—not the bot loop.
- Measure cold/warm startup, bridge latency, event backlog, command duration, stop latency, memory, battery, and reconnect behavior without recording command arguments or private game data.

Initial performance targets:

- interactive shell in under 2 seconds on the minimum reference device;
- ordinary navigation at smooth frame cadence with no blocking file/network/Keystore work on the main thread;
- stop request acknowledged immediately and cancellable waits ended within 1 second;
- bounded memory during synthetic event/log bursts;
- no duplicate run after any lifecycle recreation;
- an 8-hour soak without unbounded memory, queue, subscription, or wake-lock growth.

## 13. Updates and distribution

Two channels should be planned, but only one selected for the first public build:

### Google Play

- AAB distribution, Play signing, staged rollout, Play in-app updates, and policy review.
- Best user update experience, but automation and AccessibilityService review risk is substantial.
- Downloaded executable code or a dynamic plug-in system must not be used.

### Official GitHub sideload

- Signed APK published from a protected release workflow with SHA-256, SBOM, provenance, and release notes.
- The app can check the official release feed, download to app-owned cache, verify the expected package/signing identity, and open Android's installer flow.
- Installation requires system/user confirmation; do not call this “silent update.”
- Protect against downgrade, wrong package, wrong signer, redirect, oversized payload, partial download, and cache path traversal.

The mobile release feed, version sequence, signing key, and assets must be distinct from Windows even if both live in the same repository. Artix's APK must never be bundled, mirrored, patched, or updated by Grimoire unless the written agreement explicitly permits it.

## 14. Repository layout

Keep the desktop project stable and add Android work under separate roots. The initial implemented boundary is:

```text
mobile/
  app/                         # Android entry point and adaptive Compose shell
  core/designsystem/           # Grimoire visual tokens and reusable components
  core/model/                  # portable Kotlin state and capability model
  core/domain/                 # GameTransport boundary and offline adapter
  core/legacy-gbot/            # bounded, data-only legacy parser
shared/
  contracts/game-bridge/       # versioned schemas to review with Artix
  contracts/gbot/              # neutral legacy-format rules and mappings
  fixtures/gbot/               # anonymized cross-platform fixtures
app/                           # existing Windows client; path remains stable
game-client/                   # existing ActionScript bridge and SWF artifact
installer/                     # existing Windows installer
tests/                         # existing Windows smoke tests
docs/mobile/
```

Feature, persistence, security, transport, and benchmark modules are added only when their first production code exists. This avoids empty architecture and keeps dependency direction visible. The desktop solution can consume shared fixtures or schemas without retargeting the legacy application. Avoid a big-bang extraction from the current non-SDK project.

## 15. Testing and compatibility gates

### Every pull request

- format, lint, static analysis, dependency review, secret scanning;
- domain and command unit tests;
- legacy fixture import/export and desktop↔Android golden comparison;
- malformed/untrusted GBot fuzz/property tests;
- fake-transport deterministic execution tests;
- Compose accessibility, theme, phone/tablet, and state-restoration tests;
- privacy checks that exported diagnostics contain no fixture secrets or paths.

### Bridge releases

- approved AQW client identity and bridge compatibility handshake;
- action/event ordering, timeout, cancellation, reconnect, and version mismatch tests;
- server/client rate and payload bounds;
- device tests across the supported Android/API/CPU matrix;
- manual login, map transition, disconnect, background, screen-off, notification stop, crash, and recovery tests.

### Release gates

- all legacy commands classified; no silently skipped command;
- supported commands pass parity scenarios against the desktop behavior specification;
- signer, digest, SBOM, and provenance verified;
- no Artix asset or binary included outside the written license;
- AIR/AQW compatibility, terms, branding, and distribution review;
- threat-model, data-safety, policy, and recovery review complete;
- staged/beta soak before public availability.

## 16. Phased roadmap

Effort ranges assume one experienced engineer working full-time and are deliberately broad. An evenings/weekends side project will take substantially longer.

### Phase 0 — architecture and offline foundation (complete)

- Preserve the desktop tree and add `mobile/` and `shared/` boundaries.
- Build the landscape Compose prototype, safe GBot inspection, bridge schema,
  and Grimoire-owned visual assets.
- Select HARMAN AIR as the game runtime.

### Phase 1 — standalone AIR client proof (in progress)

- ✅ Compile the existing ActionScript loader with HARMAN AIR.
- ✅ Replace direct `ExternalInterface` use with the dual desktop/AIR host bridge.
- ✅ Package a landscape, fullscreen, captive-runtime arm64 APK.
- ✅ Add the Grimoire top menu, contextual sidebar, live game frame, and animated
  focus-mode toggle without changing the desktop host path.
- Prove live AQW version lookup, download progress, SWF import, login, input,
  networking, and lifecycle behavior on a physical Android device.
- Prove ActionScript↔ANE/native round trips after the game-only client works.
- Compare Kotlin and current-LTS .NET prototypes using legacy GBot parsing and a fake transport.
- Define bridge and GBot schemas, error model, capability handshake, and compatibility fixtures.
- Produce a security threat model and distribution recommendation.

**Exit gate:** AQW visibly reaches login on a device, the local bridge initializes,
and no desktop regression is detected. No public app yet.

### Phase 2 — integrated product foundation (6–10 weeks)

- Compose shell, adaptive navigation, theme, persistence, secure vault, diagnostics, and update metadata.
- Build the design-token/component library, landscape phone/tablet workspaces, immersive system UI behavior, and accessible theme variants before polishing individual screens.
- Fake transport and offline Bot Manager/editor.
- Foreground-service lifecycle and crash recovery without a live account.

### Phase 3 — legacy GBot engine and core commands (10–20 weeks)

- Safe import/export, Bot Doctor, neutral execution context, run ownership, and capability preflight.
- Port the most common navigation, combat, skill, delay, item, and quest commands first.
- Add desktop↔Android behavior fixtures for every supported command.

### Phase 4 — game integration and product parity (12–28 weeks)

- Licensed transport, live snapshots/events, login/client states, reconnect, update compatibility gate.
- Account Locker, bot library/editor, run timeline, inventory/bank/shop/quest workflows.
- Port remaining supported options and commands by domain.

### Phase 5 — advanced tools and plug-in replacement (12–24+ weeks)

- Only implement packet/tamper/spam or similar tools if Artix authorizes each capability in writing.
- Replace DLL plug-ins with signed/declarative extensions or omit them.
- Multi-client/Army Mode requires a separate multi-runtime architecture and is not assumed feasible on one Android process/device.

### Phase 6 — hardening and release (8–12 weeks)

- Device matrix, accessibility, battery, thermal, network, crash, recovery, security, and soak tests.
- Policy/data-safety declarations, signing protection, staged beta, support flow, and release documentation.

### Overall range

- **Standalone core bot product:** roughly 9–15 full-time engineer months.
- **Broad desktop feature parity:** roughly 15–24+ full-time engineer months, with schedule dominated by the bridge, command count, Artix review, and advanced-tool authorization.
- **Solo side-project cadence:** expect multiple years for broad parity unless scope or staffing changes.

These ranges are planning estimates, not commitments.

## 17. Stop/go gates

### Gate A — standalone AIR runtime

**Go:** the Grimoire APK reaches AQW login and keeps input, networking, and the
local callback registry stable across lifecycle changes.

**Stop:** the live SWF cannot execute correctly in the AIR application sandbox.

### Gate B — supported technical bridge

**Go:** the bridge supplies typed state/events/actions, version identity, cancellation, and a stable lifecycle.

**Stop:** the plan depends on accessibility, root, repackaging, protocol interception, or undocumented private interfaces.

### Gate C — legacy compatibility

**Go:** repository fixtures round-trip desktop↔Android with command type, order, values, and defaults preserved.

**Stop:** unknown types execute, files are silently rewritten, or common GBots lose behavior.

### Gate D — Android lifecycle

**Go:** a user-started foreground session survives allowed lifecycle changes, stops promptly, and never duplicates or auto-resumes after a crash.

**Stop:** reliable operation requires hidden background execution, battery-policy bypass, or permanent wake locks.

### Gate E — public distribution

**Go:** signing, update, privacy, Artix, store/sideload, and asset reviews pass.

**Stop:** the build contains unlicensed AQW assets/binaries, a self-modifying code path, secret material, or an unapproved capability.

## 18. Decisions needed from the project owner

1. Is public distribution planned through Google Play, GitHub sideloading, or both?
2. Is exact desktop UI parity desired, or is behavior parity with a touch-first redesign acceptable? This plan recommends the latter.
3. Which Android versions, device classes, and minimum hardware should be supported?
4. Is future iOS support important enough to affect the AIR/Android boundary?

## 19. Release-policy questions

- Are packet-level tools, bot scripting, multiple clients, account storage, and background execution permitted?
- What client identity/version should Grimoire validate, and how will breaking changes be communicated?
- May Grimoire reuse or display any AQW name, logo, screenshot, artwork, SWF, or APK content?
- May releases be distributed on Google Play/GitHub, and who owns the signing/update pipeline?
- Can Artix supply test accounts/environment and a security/contact process for bridge issues?

## 20. Recommendation

Proceed with the standalone HARMAN AIR client. AIR owns the game surface and
loads AQW directly; the local host bridge is the foundation for mobile bot
controls. The immediate gate is physical-device proof of login and bridge
initialization, followed by lifecycle hardening and then the first safe GBot
execution slice. Keep Compose work additive until an AIR overlay or ANE proves
the final single-package UI boundary.

## 21. Sources

### Official Artix sources

- [AdventureQuest Worlds Android APK (Experimental)](https://www.artix.com/experiments/aqwandroid/)
- [Artix Entertainment Terms of Service](https://www.artix.com/policy-terms/)
- [Artix Entertainment brand and IP use](https://www.artix.com/community/brand-and-assets-usage/)

### Android and distribution sources

- [Jetpack Compose overview](https://developer.android.com/compose)
- [Android background task scheduling and WorkManager](https://developer.android.com/develop/background-work/background-tasks/persistent)
- [Android Keystore](https://developer.android.com/privacy-and-security/keystore)
- [Android PackageInstaller](https://developer.android.com/reference/android/content/pm/PackageInstaller)
- [Google Play AccessibilityService API policy](https://support.google.com/googleplay/android-developer/answer/10964491)

### AIR and alternative-stack sources

- [AIR SDK features and native extensions](https://airsdk.dev/docs/features)
- [.NET MAUI overview](https://learn.microsoft.com/en-us/dotnet/maui/what-is-maui)

### Repository evidence used for this plan

- [`app/Grimoire.Botting/BotJson.cs`](../app/Grimoire.Botting/BotJson.cs)
- [`app/Grimoire.Botting/Bot.cs`](../app/Grimoire.Botting/Bot.cs)
- [`app/Grimoire.Tools/Flash.cs`](../app/Grimoire.Tools/Flash.cs)
- [`app/Grimoire.Networking/Proxy.cs`](../app/Grimoire.Networking/Proxy.cs)
- [`app/Grimoire.Game/Player.cs`](../app/Grimoire.Game/Player.cs)
- [`app/Grimoire.UI/Root.cs`](../app/Grimoire.UI/Root.cs)
- [`game-client/AS3/grimoire/Root.as`](../game-client/AS3/grimoire/Root.as)
- [`tests/LegacyGbotCompatibilitySmoke.cs`](../tests/LegacyGbotCompatibilitySmoke.cs)
- [`docs/code-performance-audit-v1.1.0.md`](code-performance-audit-v1.1.0.md)
- [`docs/improvement-research-v1.2.0.md`](improvement-research-v1.2.0.md)
