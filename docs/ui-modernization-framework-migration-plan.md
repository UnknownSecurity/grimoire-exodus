# Grimoire Exodus UI Modernization and Framework Migration Plan

Status: planning only
Target: desktop Grimoire Exodus after v1.7.0
Decision: Vue 3 + TypeScript + Vite in a staged WebView2/WinForms hybrid

## 1. Executive decision

Grimoire Exodus should modernize its presentation layer without rewriting its
game, bot, networking, updater, account-vault, or legacy serialization logic.
The recommended target is a Vue 3 single-page interface, compiled by Vite into
local static assets and hosted by one WebView2 control inside the existing
.NET Framework 4.8 WinForms process.

The AQW surface remains a native child surface owned by C#:

- Flash.ocx continues to be the default through `ActiveXFlashBridge`.
- Experimental Ruffle continues to use its existing WebView2 bridge.
- `ClientSession`, `ClientSessionManager`, and `ClientSessionContext` remain the
  authority for client identity and isolation.
- `Bot`, `BotJson`, `SafeBotSerializationBinder`, `Configuration`, and every
  existing command type remain the authority for GBot behavior and files.
- Vue renders state and requests actions; it does not execute bots, interpret
  packets, decrypt accounts, or read and write GBots itself.

This is a progressive migration. The existing WinForms shell must remain
available behind a rollback switch until the modern shell has passed parity,
compatibility, performance, and recovery testing. The reference image is a
visual direction, not a screen-by-screen clone.

## 2. Evidence from the current repository

### 2.1 Application and UI foundation

The desktop client is a non-SDK .NET Framework 4.8 WinForms application. The
main assembly remains internally named `Grimoire`, even though release tooling
brands the executable as `Grimoire Exodus.exe`.

The current presentation stack is:

| Area | Current technology | Repository evidence | Migration consequence |
| --- | --- | --- | --- |
| Main window and tools | WinForms and `System.Drawing` | `app/Grimoire.UI/Root.cs`, `app/Grimoire.UI/*.cs` | Keep the process and native host; migrate views incrementally. |
| Themed controls | DarkUI `DarkForm` and `Dark*` controls | `app/Grimoire.UI/GrimoireForm.cs`, `app/Grimoire.csproj` | Retain for fallback and not-yet-migrated forms. |
| Native title bars | DWM attributes applied by `GrimoireForm` | `app/Grimoire.UI/GrimoireForm.cs` | Keep native caption behavior first; custom chrome is a later optional step. |
| Home page | Local HTML/CSS/vanilla JavaScript in WinForms `WebBrowser` | `app/Resources/Home/index.html`, `Root.InitializeHome()` | First low-risk screen to move to Vue/WebView2. |
| Home bridge | COM-visible `ObjectForScripting` and synchronous `window.external` calls | `app/Grimoire.UI/GrimoireHomeBridge.cs` | Replace with an allowlisted, versioned asynchronous JSON bridge. |
| Default game renderer | 32-bit ActiveX Flash control | `ActiveXFlashBridge`, `AxInterop.ShockwaveFlashObjects` | Must stay a native C#-owned surface and keep x86 ABI behavior. |
| Experimental renderer | Ruffle hosted in WebView2 | `app/Grimoire.Clients/RuffleFlashBridge.cs` | Keep separate from the UI shell and retain the synchronous `IFlashBridge` contract. |
| Bot manager | Large stateful WinForms form with ten functional tabs | `app/Grimoire.UI/BotManager.cs` | Highest-risk migration; move only after the shell and bridge are proven. |
| Settings | Programmatic WinForms tabs | `app/Grimoire.UI/SettingsForm.cs` | Good early candidate after Home because preferences already have service boundaries. |
| Public website | Separate Nuxt application | `website/package.json` | Do not merge this deployable website with the desktop UI project. |
| Root JavaScript package | Commitlint and Husky only | `package.json` | Add desktop frontend tooling in an isolated directory, not to application runtime. |

The audited UI-related dependency inventory is therefore:

- `System.Windows.Forms`, `System.Drawing`, and WinForms DataVisualization;
- DarkUI forms, controls, menus, and dialogs;
- custom WinForms controls such as `NonClosableVSTabControl`, themed combo
  boxes, scroll-host helpers, owner-drawn menus, and generated designer/RESX
  resources;
- `AxInterop.ShockwaveFlashObjects` for the default native game surface;
- Microsoft WebView2 for the experimental Ruffle surface;
- WinForms `WebBrowser`/Trident, COM automation, local HTML, CSS, and vanilla
  JavaScript for Home;
- Win32/DWM interop for title-bar theming and native window behavior;
- the Unity eyedropper control and normal Windows file/message dialogs for
  specialized tools;
- HtmlAgilityPack for content parsing, not as a presentation framework;
- Nuxt, Vue, Vue Router, Tailwind, and Lucide in the separately deployed public
  website only. None currently renders the desktop shell.

`Program` owns startup, single-instance forwarding, exception capture, and
clean-shutdown recording. `Root` then constructs the window, sessions, Home,
quick-load indexing, updates, tray behavior, metrics, and tool entry points.
This makes `Root` both a view and an orchestration hub. A modernization should
reduce that coupling by introducing a C# application facade, not by moving the
same responsibilities into Vue.

### 2.2 Current UI-to-.NET communication

The embedded Home page calls a fixed set of methods such as `GetState`,
`OpenGame`, `OpenClient`, `SaveAccount`, `OpenBotFolder`, issue reporting, and
compatibility checks through `window.external`. Responses are JSON strings.
`Root.RefreshHomeState()` calls a JavaScript function back through the legacy
browser document.

This proves that the application already benefits from a presentation bridge,
but the current contract has limitations:

- calls are synchronous and tied to the legacy `WebBrowser` control;
- the bridge returns broad page-shaped responses instead of domain snapshots;
- method names and JavaScript branches are manually synchronized;
- there is no correlation identifier, cancellation, event sequence, or
  explicit protocol version;
- exceptions are often reduced to display messages rather than stable error
  codes;
- `Root` remains the direct target of most actions.

The new bridge should retain the useful allowlist concept while making the
transport asynchronous, versioned, testable, and independent of a particular
Vue component.

### 2.3 Game and multi-client boundaries

`IFlashBridge` is the engine-neutral, synchronous game boundary. Both
`ActiveXFlashBridge` and `RuffleFlashBridge` implement it. `ClientSession` owns
one bridge, bot engine, game state, option state, bot document, login state,
packet spammer, packet dispatcher, lifetime token, progress, ping, and display
identity. `ClientSessionManager` enforces UI-thread collection changes and owns
the active session.

These are the correct boundaries for the modern UI. The frontend should never
choose a static Flash object or infer the active account. Every command must
carry a session identifier, and C# must resolve that identifier to a live
`ClientSession` before invoking an operation.

The native game surface also creates an important layout constraint. ActiveX
Flash and WebView2 are native/windowed controls. A Vue surface cannot reliably
draw menus, dialogs, transparency, or tooltips over them. The layout therefore
needs an explicit native game viewport slot and an airspace policy rather than
assuming ordinary browser stacking rules.

### 2.4 Bot and legacy GBot boundaries

The existing GBot format is polymorphic JSON. `BotJson.CreateSettings()` uses
`TypeNameHandling.All` together with `SafeBotSerializationBinder`, which maps
the legacy `Grimoire` assembly identity and rejects types outside the approved
command and data model. `BotManager` uses these settings for normal load, save,
clone, and editing paths.

At runtime, `Bot` owns run cancellation and lifecycle state, publishes running
and index changes, and executes `IBotCommand` instances under the captured
`ClientSessionContext`. `BotDocumentState` is per-session editable UI state and
is deliberately not serialized into the GBot contract.

The modernization must preserve all of that. Specifically:

- Vue must not deserialize or serialize `.gbot` files.
- Vue must not manufacture .NET type names.
- C# remains the only load/save boundary and continues to use `BotJson`.
- The bridge exposes command DTOs for editing, then C# maps and validates them
  against existing command types.
- Command order, current index, safe defaults, and per-session document state
  remain authoritative in C#.
- No existing command namespace, property meaning, or internal assembly name is
  changed merely for the new UI.

### 2.5 Current feature surfaces

The migration inventory includes more than the main window:

- Home launcher, accounts, development roadmap, doctor, and issue reporting;
- main menu, client switcher, status metrics, cell/pad controls, quick load,
  update state, and tray behavior;
- Bot Manager tabs: Info, Combat, Item, Map, Quest, Misc, Misc 2, Options,
  Client, and Bots;
- Settings tabs: General, Performance, Key Bindings, Advanced, Version, and
  Logs;
- Bank, Ultra Kit, Travel, loaders/grabbers, Hotkeys, Plugins, Cosmetics,
  Eyedropper, Logs, Notepad, DPS meter, Sets, and command editors;
- packet logger/sniffer, spammer, and tamperer;
- native alerts, confirmations, file pickers, and crash/recovery prompts.

These should be migrated by workflow, not by mechanically converting every
WinForms control into a similarly positioned web control.

## 3. Vue versus Nuxt

| Criterion | Vue 3 + Vite | Nuxt | Result for Grimoire Exodus |
| --- | --- | --- | --- |
| Local desktop SPA | Direct fit | Supported, but through a larger meta-framework | Vue wins. |
| Server-side rendering | Not included | Core capability | Not needed; all assets are local. |
| Server routes and deployment | Not included | Built in | Not needed; C# is the application host. |
| Static build | Simple Vite output | Requires Nuxt generation rules | Vue is easier to package and debug. |
| Router | Optional Vue Router | File-based routing included | Explicit routes are preferable for a desktop shell. |
| Bridge integration | Direct TypeScript client over WebView2 messages | Possible, with extra runtime conventions | Vue has fewer layers between UI and C#. |
| Bundle and startup | Smaller baseline and less hydration work | Larger framework/runtime surface | Vue is preferable for a game companion. |
| Existing repository use | Vue is already known through the website | Nuxt is used by the separate website | Reuse knowledge, not the website runtime. |
| Long-term maintenance | Explicit, conventional SPA architecture | Valuable when Nuxt features are used | Nuxt would add features the desktop app does not consume. |

### Recommendation

Use Vue 3, TypeScript, Vite, Pinia, Vue Router in hash or memory mode, and local
Lucide icons. Use CSS custom properties as the source of truth for the design
system. Tailwind may be used as a build-time utility layer if the team wants
it, but semantic components and tokens must remain authoritative so the UI is
not a collection of copied utility strings.

Nuxt remains appropriate for the public website. It should not host the
desktop shell, and no Node server should ship or run with Grimoire Exodus.

## 4. Proposed integration architecture

### 4.1 Target layers

```text
Vue desktop shell
  AppShell / routes / component state / accessibility
  Pinia presentation stores
  Typed bridge client
              |
              | versioned JSON request, response, and event envelopes
              v
C# WebView2 host
  navigation and origin policy
  message validation, correlation, cancellation, UI-thread dispatch
              |
              v
C# application facade
  SessionService        BotService          AccountService
  PreferenceService     ToolService         UpdateService
  Log/DiagnosticService FileDialogService   NotificationService
              |
              v
Existing application core
  ClientSession / IFlashBridge / GameSessionState / Proxy
  Bot / BotJson / commands / BotDocumentState
  AccountVault / AppPreferences / AppPaths / recovery / updater

Native game surface host (sibling of the Vue WebView2 control)
  ActiveX Flash or the existing Ruffle WebView2 control
```

The application facade is the important seam. Vue must not call `Root`
directly. During transition, `Root` may implement or delegate to facade
services, but the public bridge contract should only know service interfaces
and DTOs.

### 4.2 Transport contract

Use `CoreWebView2.WebMessageReceived` for requests and
`PostWebMessageAsJson` for responses and events. Avoid exposing an automatic
host object with arbitrary public methods.

Example request:

```json
{
  "protocol": "1.0",
  "id": "request-42",
  "method": "bots.start",
  "params": {
    "sessionId": "..."
  }
}
```

Example response:

```json
{
  "protocol": "1.0",
  "id": "request-42",
  "ok": false,
  "error": {
    "code": "BOT_DOCUMENT_INVALID",
    "message": "The bot has no commands to run.",
    "retryable": false
  }
}
```

Example event:

```json
{
  "protocol": "1.0",
  "event": "bot.indexChanged",
  "sequence": 184,
  "payload": {
    "sessionId": "...",
    "index": 12
  }
}
```

Bridge requirements:

- allowlist every method in a registry; unknown methods fail closed;
- validate protocol version, request size, required fields, and session ID;
- use stable error codes and user-safe messages while recording technical
  detail only in bounded diagnostics;
- marshal WinForms and session-collection mutations to the UI thread;
- provide cancellation and timeouts for update checks, diagnostics, file
  operations, and other long-running requests;
- coalesce metrics and high-frequency state events instead of flooding the UI;
- use bounded log batches and virtualized lists;
- attach monotonically increasing state revisions so Vue can reject stale
  events after switching clients;
- send a complete redacted bootstrap snapshot before incremental events;
- never send decrypted passwords, packet secrets, private filesystem paths, or
  unrestricted exception objects;
- keep all file dialogs and path validation in C#;
- make the TypeScript contract and C# DTO contract testable from saved fixtures.

### 4.3 Suggested method domains

The initial bridge surface should be intentionally small:

- `app.getBootstrap`, `app.getCapabilities`, `app.openExternalLink`;
- `sessions.list`, `sessions.create`, `sessions.activate`, `sessions.close`;
- `bots.getDocument`, `bots.load`, `bots.save`, `bots.start`, `bots.pause`,
  `bots.resume`, `bots.stop`;
- `bots.getCommandCatalog`, `bots.insertCommand`, `bots.updateCommand`,
  `bots.moveCommand`, `bots.removeCommand`;
- `accounts.listRedacted`, `accounts.save`, `accounts.remove`,
  `accounts.openInNewClient`;
- `preferences.get`, `preferences.update`;
- `logs.getSnapshot`, `logs.copySupportReport`;
- `updates.check`, `updates.prepare`;
- `tools.open` for an explicit enum of supported tools;
- `ui.setGameViewport` to synchronize the native game surface rectangle.

Do not create generic methods such as `invokeDotNet`, `callFlash`, `readFile`,
or `openProcess`.

### 4.4 State ownership

C# is authoritative for sessions, accounts, bot documents, bot execution,
packets, updates, preferences, logs, and game state. Pinia stores hold only:

- the latest redacted C# snapshot;
- current route and selected presentation state;
- filters, searches, expanded panels, and temporary form drafts;
- pending request and validation state.

Sensitive or domain-critical state must not be persisted to browser storage.
Existing `AppPaths` and encrypted account-vault behavior remain the only
persistence paths for application data.

### 4.5 Hosting and native game airspace

Use one WebView2 control for the Vue shell. Serve built assets from an
application-owned local virtual host or another locked local origin. Block
unexpected navigation, new windows, permissions, downloads, and remote script
execution. Bundle all fonts, icons, styles, and scripts locally with a strict
Content Security Policy.

The Vue layout reserves a `GameViewportSlot`. A `ResizeObserver` reports its
pixel rectangle and scale to C#. C# positions and clips the active session's
native `HostControl` over that rectangle. This permits the surrounding Vue UI
to match the reference layout while keeping the existing renderer untouched.

Airspace rules:

- Vue popovers and tooltips must not depend on drawing over the game rectangle.
- Full-shell modal dialogs either temporarily hide the game surface or use a
  native themed dialog until a safe composition solution is verified.
- The game surface must be hidden while Home or another full-page workspace is
  active.
- Only the active client's surface is visible unless a separately tested split
  view is enabled.
- Resizing must preserve the current game aspect/scale policy and accurate
  Flash click coordinates.
- DPI, Windows display scaling, minimize/restore, tray restore, and monitor
  movement require dedicated tests.

### 4.6 WebView2 availability and fallback

Today WebView2 is optional because only experimental Ruffle needs it. Making
the modern shell depend on WebView2 changes the installation contract.

During migration:

1. detect WebView2 before constructing the modern shell;
2. use the legacy WinForms shell if it is unavailable or the modern shell
   fails its startup health check;
3. add an installer strategy for Evergreen WebView2 or a documented fixed
   runtime only after package-size and offline-install tradeoffs are measured;
4. keep a safe-start option that bypasses the modern shell for recovery;
5. never make the availability of the Vue shell change Flash.ocx or bot
   behavior.

## 5. UI structure based on the reference image

### 5.1 What to adopt

The reference has a strong, game-oriented hierarchy worth adopting:

- dark navy surfaces with purple focus and action accents;
- a compact global navigation bar;
- a contextual left rail with clear active state;
- client tabs and client-creation controls directly above the game;
- the AQW client as the visual center of gravity;
- a lower operational dock for bot state, active script, and logs;
- a persistent status strip for version, connection, ping, and quick load;
- outlined icons, compact cards, restrained glow, and subtle glass-like depth.

Do not copy the exact artwork, character identity, account values, game version,
or every duplicated navigation item. The new design should apply Grimoire's
own brand assets and actual application state.

### 5.2 Proposed component hierarchy

```text
AppShell
├─ NativeTitleBar (initially Windows-owned)
├─ GlobalNavigation
│  ├─ BrandMark
│  ├─ PrimaryRoutes
│  ├─ UpdateIndicator
│  └─ SettingsAction
├─ Workspace
│  ├─ ContextSidebar
│  │  ├─ RouteActions
│  │  ├─ ClientHealthCard
│  │  ├─ BotControlCard
│  │  └─ ActiveAccountCard
│  └─ MainWorkspace
│     ├─ ClientWorkspaceHeader
│     │  ├─ ClientTabStrip
│     │  ├─ ViewModeActions
│     │  └─ NewClientAction
│     ├─ GameViewportSlot
│     └─ OperationsDock
│        ├─ BotStatusPanel
│        ├─ ActiveDocumentPanel
│        └─ LogPanel
└─ StatusBar
   ├─ VersionStatus
   ├─ ConnectionStatus
   ├─ PingStatus
   ├─ RuntimeStatus
   └─ QuickLoadAction
```

The global navigation selects a major workspace. The left sidebar changes to
show contextual actions for that workspace. This avoids making the top bar and
left rail two copies of the same navigation, which is the main hierarchy issue
in the reference.

### 5.3 Major workspaces

#### Home

Modernize the current launcher, account locker, client selector, Bot Manager
summary, Doctor, roadmap, and issue-report accordions. It should show clear
empty, loading, degraded, and ready states without exposing credentials.

#### Bots

Replace the current ten-tab editing experience with a three-pane workspace:

- left: command categories, search, templates, and loaded bot metadata;
- center: virtualized ordered action list with the active action highlighted;
- right: typed editor/inspector for the selected command;
- dock: skills, quests, drops, boosts, client options, validation, and run log.

The category vocabulary should still map to the existing Info, Combat, Item,
Map, Quest, Misc, Options, Client, and saved Bots capabilities. Reorganizing the
presentation must not change serialized commands.

#### Accounts

Show redacted locker entries, cached character portrait, level/class metadata,
active-client assignment, add/remove actions, and one-click new-client launch.
Passwords are write-only form values and are cleared immediately after the C#
request completes.

#### Scripts and Quick Load

Provide searchable Community and external bot sources without revealing local
bot counts in support output. Keep native file selection and safe C# loading.
Quick Load becomes a command palette reachable from the status bar and a
keyboard gesture.

#### Packets

Unify logger/sniffer, spammer, and tamperer into subviews. Packet collection is
paused by default where it is paused today. Lists must be bounded and
virtualized; raw packet bodies remain private and are never included in normal
telemetry or UI crash messages.

#### Tools and Plugins

Expose Bank, Ultra Kit, Travel, loaders/grabbers, Cosmetics, DPS, Sets,
Eyedropper, Notepad, and plugin management through explicit routes, drawers,
or native-form launchers. Native forms can remain during early phases. Plugin
loading and trust behavior must not be broadened by the new shell.

#### Settings

Retain the current order and behavior: General, Performance, Key Bindings,
Advanced, Version, Logs. Use a single page with tab/section navigation,
consistent scroll containers, sticky save feedback, and native confirmations
for destructive reset actions.

### 5.4 Design tokens

The palette below is a starting point to prototype and contrast-test, not a
commitment to exact screenshot sampling:

```css
--color-canvas:        #0b0918;
--color-surface-1:     #121022;
--color-surface-2:     #19162d;
--color-surface-3:     #24203a;
--color-border:        #35304f;
--color-border-accent: #7c3aed;
--color-accent:        #a855f7;
--color-accent-strong: #c064ff;
--color-text:          #f4f1fb;
--color-text-muted:    #aaa4bd;
--color-success:       #38d98b;
--color-warning:       #f4b740;
--color-danger:        #ff647c;
--color-info:          #6fb8ff;
```

Use an included, redistributable variable font only after its license is added
to the repository. Until then use `Segoe UI Variable`, `Segoe UI`, sans-serif.
Suggested density tokens are 4, 8, 12, 16, 24, and 32 pixels; corner radii are
6, 10, and 14 pixels. Motion should use 120–180 ms transitions, respect
reduced-motion preferences, and avoid continuous glow or blur animation.

Dark mode is the primary visual direction, but light mode remains a current
capability and needs an equivalent token set before final migration.

### 5.5 Responsive behavior

The supported baseline remains the current 960×620 client area.

| Width | Behavior |
| --- | --- |
| 960–1099 | Sidebar collapses to an icon rail; lower dock shows one selected panel; top navigation moves secondary items to overflow; game remains primary. |
| 1100–1399 | Full contextual sidebar; lower dock uses two panes or a tabbed third pane; client tabs scroll with compact controls. |
| 1400+ | Full sidebar and three-column lower dock; optional split view may be offered if renderer and click-coordinate tests pass. |

Height rules:

- the game viewport receives the first claim on vertical space;
- the operations dock is resizable, collapsible, and remembers only a
  presentation preference;
- virtualized lists scroll internally instead of expanding the window;
- no horizontal page scrollbar is allowed;
- focus and selected items must remain visible after resize;
- at 960×620, all critical start/stop/client-switch controls remain reachable
  without opening a secondary window.

### 5.6 Interaction, loading, and errors

- Use skeletons only for content that is genuinely loading; use progress and a
  numeric percentage for known AQW/SWF progress.
- A stalled or faulted client shows the current diagnostic action without
  hiding the other client tabs.
- Use toasts for successful non-destructive actions, inline validation for form
  errors, and modal confirmation only for destructive or shutdown actions.
- Preserve close confirmation, update restart confirmation, tray restore, and
  crash recovery as native safeguards.
- Bot start, stop, pause, resume, and active index must update from C# events,
  not optimistic UI assumptions.
- Every icon-only control needs an accessible name and tooltip.
- Keyboard navigation, visible focus, 4.5:1 text contrast, scalable text, and
  screen-reader labels are acceptance requirements.

## 6. Compatibility and risk assessment

| Risk | Severity | Mitigation and gate |
| --- | --- | --- |
| GBot type or property drift | Critical | Browser never serializes GBots; C# continues through `BotJson`; run legacy load and round-trip suite on every bot-related phase. |
| Wrong-client bot or packet action | Critical | Every request includes `sessionId`; resolve through `ClientSessionManager`; add two-client isolation tests for each migrated action. |
| ActiveX/WebView2 airspace and click mismatch | Critical | Prove native viewport positioning, DPI conversion, clipping, and focus in a spike before building the shell around it. |
| WebView2 missing or broken | High | Retain legacy shell fallback and safe-start; define installer/runtime strategy before default enablement. |
| `Root` remains a god object behind a new UI | High | Move orchestration behind application-facade interfaces before migrating complex screens. |
| Bot Manager parity is underestimated | High | Treat it as its own milestone with command-by-command inventory and golden GBot fixtures. |
| Existing plugins touch WinForms controls | High | Preserve documented compatibility members during transition; inventory plugin-facing UI dependencies before removal. |
| UI event flood harms game FPS | High | Coalesce metrics, bound logs, virtualize lists, and measure with one and multiple clients. |
| Native dialogs and web focus conflict | Medium | Central dialog service; temporarily suspend shell shortcuts; restore focus to the invoking control. |
| Node tooling leaks into runtime | Medium | Build static assets in CI; ship only hashed output and licenses; no Node executable or server in releases. |
| Remote content introduces security/privacy risk | High | Local assets only, strict origin/CSP, blocked navigation and permissions, redacted DTOs. |
| Theme or accessibility regression | Medium | Token tests, visual snapshots at both themes and target sizes, keyboard-only acceptance pass. |

### Non-negotiable preservation rules

- Keep .NET Framework 4.8 and configuration-compatible C# 7.3 until a separate
  runtime migration is explicitly approved.
- Keep x86/Prefer32Bit, registration-free Flash, the synchronous
  `IFlashBridge`, and the app-local runtime layout.
- Keep the internal `Grimoire` assembly identity and all legacy command type
  names.
- Keep `BotJson.CreateSettings()` and `SafeBotSerializationBinder` in every
  GBot load/save path.
- Keep per-session bot, game, packet, option, and document ownership.
- Keep user data under `AppPaths`; do not move secrets into browser storage.
- Keep unrelated user bots and files untouched by install and update flows.
- Keep the legacy shell until the rollback and recovery gates pass.

## 7. Proposed repository structure

This is a planning target, not a change to make before the architecture spike:

```text
desktop-ui/
  package.json
  vite.config.ts
  src/
    app/
    bridge/
    components/
    features/
      accounts/
      bots/
      clients/
      logs/
      packets/
      settings/
      tools/
      updates/
    stores/
    styles/
    types/
  tests/

app/
  Grimoire.UI.Modern/
    ModernShellHost.cs
    GameViewportCoordinator.cs
    ModernUiBridge.cs
    BridgeRequestRouter.cs
    Dtos/
    Services/
  Resources/
    Shell/                 # generated Vite output copied by the build
```

Generated frontend output should not be edited by hand. The non-SDK project
must explicitly include new C# files and packaged resources. The public
`website/` remains independent.

## 8. Phased implementation roadmap

### Phase 1 — Architecture audit and contract freeze

Objectives:

- finish the form, workflow, plugin, command, and bridge inventory;
- record current behavior at 960×620 and larger sizes in both themes;
- define baseline startup, memory, idle CPU, resize, and multi-client FPS data;
- freeze the GBot and session compatibility contracts.

Deliverables:

- architecture decision records for shell, bridge, and game airspace;
- current-to-target screen matrix;
- plugin-facing compatibility inventory;
- golden bridge snapshots and synthetic GBot fixtures;
- performance and accessibility baseline.

Dependencies: none.
Primary risk: missing a rarely used native tool or plugin dependency.
Acceptance: every current entry point is accounted for, and all existing smoke
tests pass without product behavior changes.

### Phase 2 — Framework selection proof and integration spike

Objectives:

- create a minimal Vue/Vite shell behind a developer-only feature flag;
- establish the versioned JSON bridge;
- prove the native ActiveX and Ruffle viewport slot at common DPI values;
- prove missing-WebView2 fallback and safe-start behavior.

Deliverables:

- Vue shell showing a redacted bootstrap snapshot;
- request/response/event contract library;
- native viewport coordinator spike;
- automated bridge validation and malformed-message tests;
- measured WebView2 runtime/package options.

Dependencies: Phase 1 contracts.
Primary risk: airspace, focus, or click-coordinate mismatch.
Acceptance: Flash.ocx login/game interaction remains accurate while the shell
resizes, moves across monitors, minimizes, restores, and switches clients; the
legacy shell still opens when the spike fails.

### Phase 3 — UI foundation and design system

Objectives:

- implement tokens, typography, icons, layout primitives, accessibility, and
  reusable state components;
- implement the global shell, contextual sidebar, status bar, notifications,
  dialog coordination, and responsive breakpoints;
- establish frontend unit, component, and visual regression tests.

Deliverables:

- dark and light token themes;
- reusable buttons, fields, cards, tabs, tables, lists, empty states, progress,
  toasts, and confirmation patterns;
- app shell matching the reference hierarchy with Grimoire assets;
- performance instrumentation for bridge and render rates.

Dependencies: successful Phase 2 spike.
Primary risk: visual polish proceeding before native layout constraints are
settled.
Acceptance: shell works keyboard-only at 960×620, 1280×720, and a high-DPI
configuration with no page-level horizontal scrollbar.

### Phase 4 — Low-risk feature migration

Objectives:

- migrate Home, Doctor, issue reporting, update indicator, account locker,
  client selector, Settings, Logs, and Quick Load;
- keep destructive confirmations and file selection native where appropriate;
- remove no legacy screen yet.

Deliverables:

- modern Home and Settings routes;
- redacted account and client stores;
- bounded/virtualized log view;
- accessibility and failure-state coverage.

Dependencies: facade services for accounts, preferences, diagnostics, updates,
and file dialogs.
Primary risk: accidentally returning credentials or private paths.
Acceptance: feature parity in both shells, privacy tests pass, and changing a
setting produces the same persisted behavior after restart.

### Phase 5 — Client workspace and operational dock

Objectives:

- migrate client tabs, connection state, game loading progress, metrics, ping,
  account identity, cell/pad actions, and the operations dock;
- integrate native game surface positioning into the production shell;
- retain the current multi-client limit and experimental settings.

Deliverables:

- client workspace from the component map;
- Bot Status, Active Document, and Logs panels;
- active/inactive session event filtering;
- responsive compact and expanded layouts.

Dependencies: stable viewport coordinator and session facade.
Primary risk: wrong-session events or performance regression with multiple
clients.
Acceptance: client switching never mixes account, bot, quest, options, ping,
or logs; running bots are not throttled or stopped by UI navigation.

### Phase 6 — Bot Manager migration

Objectives:

- build the command catalog, ordered action list, inspector, metadata editor,
  skills, quests, drops, boosts, and options surfaces;
- show active execution index and bot lifecycle in real time;
- preserve native C# serialization and command execution.

Deliverables:

- command-by-command editor parity matrix;
- C# DTO mappers and validators for every supported command;
- load/save/edit/run workflows;
- undo/redo for presentation edits without changing GBot output semantics;
- legacy shell side-by-side comparison tooling for development.

Dependencies: bridge stability and a complete command inventory.
Primary risk: subtle loss of defaults, ordering, or command-specific behavior.
Acceptance: repository fixtures and the repository-controlled Community Bot
directory load, round-trip, and execute through unchanged C# paths; current and
legacy GBot tests pass; active action highlighting matches `Bot.Index`.

### Phase 7 — Tools, packets, and plugin workflows

Objectives:

- migrate or wrap Bank, Ultra Kit, Travel, packet tools, plugins, cosmetics,
  DPS, sets, loaders, and editors;
- retain a native launcher for components that do not yet benefit from a web
  rewrite;
- audit plugin compatibility before any public WinForms member is retired.

Deliverables:

- tools and packet routes with bounded data flow;
- explicit native-form adapter registry;
- plugin compatibility report and migration API, if one is needed;
- privacy tests for packet and diagnostic data.

Dependencies: stable session-specific facade.
Primary risk: plugins relying on undocumented form controls or static globals.
Acceptance: every existing menu action has a working modern equivalent or an
intentional native adapter; plugin discovery scope is unchanged.

### Phase 8 — Integration, regression, and final migration

Objectives:

- run extended single- and multi-client soak tests;
- validate installer, portable ZIP, updates, rollback, recovery, tray, themes,
  DPI, accessibility, and performance;
- make the modern shell default only after rollback confidence is proven;
- remove legacy UI pieces only after at least one stable release with fallback.

Deliverables:

- full parity checklist and signed-off known differences;
- release-equivalent automation including frontend build integrity;
- WebView2 installer/portable strategy;
- support diagnostics for shell startup without private data;
- documented safe-start and rollback procedure.

Dependencies: all earlier phases.
Primary risk: removing the fallback too early.
Acceptance: all desktop smoke suites pass; no legacy GBot or session-isolation
regression; release payload works with supported install methods; the modern
shell meets measured CPU, memory, input-latency, and resize budgets; recovery
can return to the legacy shell without data loss.

## 9. Validation strategy

### Automated gates

- keep the existing Debug and Release build suites;
- run legacy GBot load and round-trip compatibility for every bot DTO change;
- add bridge contract tests in C# and TypeScript using the same JSON fixtures;
- add two-client tests for every state-changing session method;
- test malformed, oversized, stale, cancelled, and unknown bridge messages;
- test missing WebView2, failed navigation, renderer process exit, and fallback;
- test game viewport coordinate conversion at 100%, 125%, 150%, and 200% DPI;
- test bounded logs and event coalescing under packet load;
- run frontend unit, component, accessibility, and visual snapshot suites;
- verify Vite output contains no remote runtime dependency or source-map secret.

### Manual gates

- default 960×620 and large-window layouts in dark and light themes;
- Flash.ocx login, server selection, movement, combat, and precise clicking;
- Ruffle smoke behavior without changing its experimental status;
- start, stop, pause, resume, load, edit, save, and quick-load a GBot;
- multiple clients with distinct accounts, bots, quests, and tool windows;
- tray minimize/restore, close confirmation, update restart, and crash recovery;
- keyboard-only navigation, screen-reader naming, focus restoration, and text
  scaling;
- extended soak while a bot is running, with the UI opened and collapsed.

### Performance budgets

Set final numeric budgets from Phase 1 measurements. Initial guardrails are:

- idle modern shell adds no sustained busy loop and remains near idle CPU;
- metrics and bot-index events never render faster than the UI can consume;
- log and packet views remain bounded and virtualized;
- a local bridge action has no visible input delay under normal load;
- resizing the shell does not stall bot execution or corrupt game coordinates;
- one added shell WebView2 is measured separately from any Ruffle client;
- multi-client performance is compared against the same renderer and FPS policy
  in the legacy shell.

## 10. Decisions required before implementation

The following decisions belong in the architecture spike, not in the first
production patch:

1. Evergreen WebView2 installation versus a fixed runtime for portable users.
2. Exact native viewport composition strategy after DPI/airspace testing.
3. Whether Tailwind is used as a utility layer or the shell uses scoped CSS
   over the same design tokens.
4. Font choice and licensing.
5. How long the legacy shell remains user-selectable after the modern shell is
   made default.
6. Which plugin-facing WinForms members are contractual and which are internal.
7. Whether custom window chrome is worth the accessibility, snapping, and DPI
   risk after the native-title-bar version ships.

## 11. Recommended first implementation slice

After approval, begin only with Phase 1 and the Phase 2 spike:

- add no bot editor yet;
- add no custom title bar yet;
- do not change the default renderer;
- do not change GBot models or serialization;
- render a Vue shell with Home/state placeholders;
- prove the JSON bridge, WebView2 fallback, and native game viewport at 960×620
  and high DPI;
- publish the measurements and updated risk decision before feature migration.

This slice is deliberately small. If native game composition or WebView2
availability cannot meet the acceptance gates, the design can change without
having duplicated the Bot Manager or disturbed the existing application core.
