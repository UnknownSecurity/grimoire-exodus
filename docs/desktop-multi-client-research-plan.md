# Grimoire Exodus desktop multi-client research and implementation plan

**Status:** Opt-in experimental desktop mode with a six-client safety cap and advanced unlimited override implemented; extended soak and optional split-screen remain future work

**Research date:** 2026-10-17

**Target:** Windows desktop, .NET Framework 4.8, x86 WinForms

## 1. Executive summary

Desktop multi-client support is feasible, but it is not a matter of adding more
Flash controls to tabs. The current application is architected around one
process-wide Flash control, one packet dispatcher, one player/world state, one
bot engine, one Bot Manager, and one set of toolbar options. Adding a second
control before separating those owners would create a serious risk that a bot,
packet handler, hotkey, or toolbar action intended for one character acts on
another.

The recommended design is one visible Grimoire shell with a collection of
independent `ClientSession` objects. Each session owns its Flash control,
synchronous bridge, packet dispatcher, game state, bot runtime, option state,
login flow, logs, cancellation, and lifecycle. A process-global
`ClientSessionManager` owns the collection and identifies the client currently
selected in the UI. Updates, the tray icon, global settings, account vault, bot
catalog, theming, and process metrics remain application-wide.

The first public multi-client version should display one client at a time in a
non-closable client tab strip while all opened sessions remain connected in the
background. Each client gets its own close action and state indicator. The
existing toolbar remains compact, but every client-affecting item is rebound to
the selected session. Switching tabs changes what the toolbar displays; it
must never change which session owns an already-running bot.

Implementation should begin with a disposable two-control Flash experiment.
The project uses an unmanaged ActiveX control through `AxHost`, and the client
is a 32-bit STA application. The experiment must prove that two controls can
load independently, return `ExternalInterface` values to the correct sender,
stay alive while not selected, accept accurate input after tab switches, and
dispose without destabilizing the other control. If that gate fails, the team
should stop and evaluate a shell-plus-worker-process design instead of hiding
the failure behind UI work.

Legacy `.gbot` files do not need a schema migration. Existing command type
names, namespaces, JSON settings, `IBotCommand`, `IBotEngine`, and the internal
`Grimoire` assembly identity must remain unchanged. The migration happens
behind those contracts by introducing explicit session services and temporary
compatibility facades.

The research sections preserve the design rationale. Section 15 records the
implemented opt-in six-client mode, advanced unlimited override, and remaining
follow-up work.

## 2. Product outcome

The intended user flow is:

1. The user opens Grimoire Exodus once.
2. Home offers **New Client** and lists the clients already open in this shell.
3. Opening an account or a blank game creates a client, or fills an unused
   client that has not begun login.
4. A compact client strip switches the visible AQW surface.
5. Bot controls, login/server controls, cells and pads, ping, status, packet
   tools, and gameplay options always represent the selected client.
6. Clients that are not selected remain connected and may continue their own
   bot run.
7. Closing one client affects only that client. Closing or updating the whole
   application accounts for every active client and bot.
8. When multi-client is stable, launching Grimoire again asks the existing
   shell to focus itself or add a new client instead of opening another full
   application window.

### Initial scope

- Multiple independent AQW clients in one Grimoire shell.
- One visible game surface at a time.
- Independent login, game state, packet routing, options, bot document, bot
  execution, logs, and recovery state for every client.
- A toolbar whose client effects and checked states are scoped to the selected
  client.
- Independent client close, disconnect, fault, and retry behavior.
- Global tray, updater, settings, theme, device metrics, account vault, and bot
  catalog.
- Compatibility with existing `.gbot` files and the current app-local Flash
  runtime.

### Deferred scope

- Side-by-side or split-screen rendering. It can be added after the one-visible
  client design is stable, but rendering several Flash controls simultaneously
  has a different performance and input profile.
- Automatically reopening accounts or restarting bots after a crash. Recovery
  may offer the previous bot for review, but must not start it.
- Synchronizing one toolbar action across all clients. A deliberate **Apply to
  all clients** feature can be designed later; ordinary toolbar clicks must
  affect one session only.
- Cross-client coordinated bot commands. Existing commands remain scoped to
  the bot's own session.
- Replacing or modernizing the Flash bridge ABI.

## 3. Research findings

### 3.1 The current application has one implicit session

The current runtime path is effectively:

```text
Root
  -> one AxShockwaveFlash control
  -> static Flash bridge
  -> static Proxy dispatcher
  -> static Player / World / BotData state
  -> singleton Bot + singleton BotManager
  -> singleton tools and logs
```

The following are not currently isolated:

| Area | Current single-client assumption | Multi-client consequence |
| --- | --- | --- |
| Shell | `Root.Instance` owns one `Game`, one loading state, one login token, and one running flag | Root must become a shell and delegate client state to sessions |
| Flash | `Flash.flash`, `Flash.Instance`, callback events, and calls are static | Calls and callbacks could target the last-created control |
| Callback response | `ProcessFlashCall` returns values through `Root.Instance.Client` rather than the event sender | A server-list callback could answer the wrong SWF |
| Packets | `Proxy.Instance`, `ReceivedFromGame`, handler lists, capture flags, and the closing token are global | Packet handlers and capture tools could mix clients |
| Game model | `Player`, `World`, `BotData`, auras, drops, map, server, login, bank, quests, and events contain mutable static state | State from one character can overwrite another |
| Bot engine | `Bot.Instance`, current configuration, indexes, drop tracking, and lifecycle tasks are shared | Only one safe bot run is possible today |
| Bot options | `OptionsManager` stores flags, loops, handler registrations, and cancellation statically | Effects and background loops would move with the active tab |
| Bot Manager | `BotManager.Instance` holds both the editing UI and active engine | The form is currently the model as well as the view |
| Hotkeys | Static actions call `Root.Instance`, `BotManager.Instance`, and static game APIs | A key press has no durable client target |
| Tools | Bank, travel, packet tools, logs, cosmetics, and other forms are singletons | Each form currently assumes one global game |
| Home | The bridge exposes one ready/running client called `Client 01` | Home needs a bounded, secret-free session list and targeted actions |
| Recovery | Startup diagnostics record one bot path/running state | Recovery cannot describe several independent sessions |
| Shutdown/update | Confirmation checks one client and one bot | App-wide operations must aggregate all sessions |

This is why a UI-first implementation is unsafe. Session ownership must be
introduced below the UI before a second live client is enabled.

### 3.2 The packet layer has no local listening-port conflict

`Proxy` is a dispatcher for packet data received from the SWF bridge. It does
not create a local `TcpListener`, bind a socket, or reserve a per-process proxy
port. Multi-client therefore does not require allocating local ports. Each
session still needs its own `ProxySession`, handler instances, capture flags,
buffers, and events because the current dispatcher and its consumers contain
mutable state.

Handler registries may eventually share immutable command-to-handler metadata.
Actual handler objects should initially be constructed per session because
some handlers own cancellation or otherwise read and mutate live session
state.

### 3.3 ActiveX hosting is the first technical gate

The entry point is correctly marked `[STAThread]`, and the existing Flash
control is created on the WinForms UI thread with `BeginInit`, `OcxState`, and
`EndInit`. That threading and initialization model must be preserved.

Microsoft documents that Windows Forms hosts ActiveX through the unmanaged
`AxHost`/COM interop layer, and that hosted ActiveX controls have lifecycle,
input, and event behavior different from ordinary managed controls. The
documentation supports the chosen technology but does not guarantee that this
specific Flash OCX and SWF remain independent when several instances share one
process. That behavior must be measured with the exact checked-in runtime.

Research references:

- [Considerations when hosting an ActiveX control on a Windows Form](https://learn.microsoft.com/en-us/dotnet/desktop/winforms/controls/considerations-when-hosting-an-activex-control-on-a-windows-form)
- [`System.Windows.Forms.AxHost` for .NET Framework 4.8](https://learn.microsoft.com/en-us/dotnet/api/system.windows.forms.axhost?view=netframework-4.8.1)

The proof must cover more than successful construction. It must test:

- two controls created and disposed independently on the same STA UI thread;
- identical `OcxState` initialization from the app-local runtime;
- callback-to-sender routing and synchronous `SetReturnValue` behavior;
- independent load progress and login screens;
- no shared Flash variables or callback results;
- accurate mouse and keyboard focus after repeated tab switches;
- a hidden/non-selected control continuing network and game timers;
- one control reloading or failing without corrupting the other;
- private bytes, handles, GDI objects, CPU, and callback latency over a soak;
- clean shutdown in either creation order.

The application is x86, so address-space pressure is a real product limit.
The initial maximum client count must be selected from measurements rather
than guessed. Two clients were appropriate for the first experiment. The
current public default is six, with an explicitly warned unlimited override;
neither removes the physical x86 process limits.

### 3.4 The bot engine already has useful lifecycle groundwork

`Bot` now serializes lifecycle transitions, owns a cancellation source per run,
tracks its worker tasks, and ignores a superseded run in its main loop. Those
improvements are a good starting point. The remaining problem is target
ownership: the run still reads global `Player`, `World`, `BotData`,
`OptionsManager`, `Proxy.Instance`, and `BotManager.Instance` state.

A multi-client bot must capture a `ClientSession` when it starts. Changing the
selected tab must never change that reference. All bot-created tasks, command
execution, quest/drop event handlers, delays, relogin, and shutdown work must
run inside that session context.

The following static mutable data must also become per-run or per-session:

- `Bot.Configurations`, `Bot.OldIndex`, and `Bot.DropsInInventory`;
- `Configuration.Tempvalues` and `Configuration.Tempvariable`;
- `BotData` fields and skill/quest collections;
- `BotUtilities.ActiveBot`, `BusyClient`, `HasLoadedQuests`, and
  `ShouldUseSkill`;
- every `OptionsManager` flag, loop, handler registration, task, and token.

These changes do not require serialized property changes. The static members
can remain as compatibility facades while their storage moves behind a session
or run context.

### 3.5 Global services should stay global

Not everything should be duplicated. These are process-wide by design:

- app update discovery, download, validation, and updater launch;
- tray icon and process visibility;
- theme and native title-bar theme;
- Settings and app preferences such as close confirmation and minimize to tray;
- process CPU, uptime, and network totals;
- account vault storage;
- Community/External bot catalog and filesystem watchers;
- plugin catalog and loaded assemblies;
- app-level startup/crash marker;
- the root window and Home page.

Their actions may still need to inspect all sessions. For example, update
restart must report and safely stop every active bot before closing the shell.

## 4. Recommended architecture

```text
Root shell (one WinForms Form)
  |-- Home
  |-- global toolbar items, settings, updater, tray, metrics
  |-- ClientSessionManager
  |     |-- ActiveSession
  |     |-- ClientSession 01
  |     |     |-- ClientWorkspace + AxShockwaveFlash
  |     |     |-- FlashBridge
  |     |     |-- ProxySession + handler set
  |     |     |-- GameSessionState
  |     |     |-- BotSession + BotEngine
  |     |     |-- ClientOptionState
  |     |     |-- LoginSession
  |     |     `-- bounded logs/diagnostics
  |     `-- ClientSession 02 ...
  |-- ToolbarSessionBinder
  `-- session-aware tool views
```

### 4.1 `ClientSession`

`ClientSession` is the unit of isolation and disposal. It should have an
immutable ID and own:

- a generated display label such as `Client 01`;
- a lifecycle state;
- one `ClientWorkspace` and `AxShockwaveFlash` control;
- one instance-based Flash bridge;
- one packet dispatcher and handler set;
- one game-state container;
- one bot document/editor state and one bot engine;
- one option/effect controller;
- login/account-fill state and cancellation;
- ping and client compatibility state;
- bounded log and packet buffers;
- a session cancellation source and asynchronous stop/dispose path.

The session ID, not a tab index or display name, is the durable in-memory key.
Indexes change when clients close. Account credentials must not become session
identifiers and must never be included in logs, tab tooltips, IPC, or recovery
reports.

Suggested lifecycle:

```text
Created -> Initializing -> Ready -> Connecting -> LoggedIn
   |            |           |          |             |
   +------------+-----------+----------+-------------+
                            v
                         Faulted

Any non-disposed state -> Closing -> Disposed
```

Bot state is separate from client lifecycle: `Stopped`, `Starting`, `Running`,
`Pausing`, `Paused`, `Stopping`, or `Faulted`. A client can remain connected
when its bot stops or faults.

### 4.2 `ClientSessionManager`

The manager owns collection changes and the selected session. It should expose
read-only session snapshots and events such as:

- `SessionAdded`;
- `SessionRemoving` / `SessionRemoved`;
- `ActiveSessionChanging` / `ActiveSessionChanged`;
- `SessionStateChanged`;
- aggregate counts needed by close, tray, update, and recovery UI.

All collection and ActiveX-control changes occur on the WinForms UI thread.
Background services may read immutable snapshots, but they must request
mutations through the UI synchronization context.

The manager must enforce a measured client limit and reject a new client with
a clear message before constructing another ActiveX control when the limit is
reached.

### 4.3 Instance Flash bridge

Replace global control ownership with an instance service, conceptually:

```csharp
interface IFlashBridge
{
    T Call<T>(string function, params object[] arguments);
    object Call(string function, Type resultType, params object[] arguments);
    string GetGameObject(string path);
    void SetGameObject(string path, object value);
}
```

The implementation holds exactly one control. Its event handler is attached as
a closure or instance method for that session, so `SetReturnValue`, packet
logging, progress, and errors use the sender's bridge. Calls from bot tasks must
be marshalled synchronously to the control's owning UI thread to preserve the
existing synchronous bridge contract. UI code must not synchronously wait for
bot tasks, or it could deadlock that marshal.

The ActionScript callback names, parameters, return XML, and checked-in SWF do
not need to change merely to add sessions. If the bridge ABI changes for another
reason, C# and ActionScript must still change together and the runtime SWF must
be rebuilt.

### 4.4 Session context and legacy compatibility facades

Rewriting every legacy command in one change would create unnecessary GBot
risk. Introduce a scoped `ClientSessionContext` backed by `AsyncLocal` and use
it as a migration bridge:

1. Every session-owned ingress pushes its session before invoking existing
   code: Flash callbacks, packet handlers, bot tasks, option loops, timers,
   relogin, tool actions, and hotkeys.
2. Existing static APIs such as `Flash.Call`, `Player`, `World`, `BotData`,
   `OptionsManager`, `Proxy.Instance`, and `Bot.Instance` temporarily forward
   to the scoped session.
3. A direct UI action may fall back to `ClientSessionManager.ActiveSession`.
4. Background work must not use that fallback. In development builds, missing
   background context should fail loudly or emit a privacy-safe diagnostic.
5. New code receives explicit session services instead of introducing new
   static calls.

`AsyncLocal` is preferable to thread-static storage because bot work crosses
`await` and task boundaries. It is still a compatibility adapter, not the final
dependency model. Critical bot and packet paths should progressively accept
explicit services so target ownership is visible in method signatures.

### 4.5 Per-session game state and packet dispatch

Create a `GameSessionState` that owns mutable values currently stored on
`Player`, `World`, `BotData`, `Map`, bank/quest helpers, and aura/drop events.
Static game facades resolve this state through the current session during the
migration.

Create one `ProxySession` per client. Incoming bridge data follows this path:

```text
Flash control event
  -> owning FlashBridge
  -> owning ProxySession
  -> owning handler instances
  -> owning GameSessionState and BotSession
  -> session-specific logs/UI notifications
```

The process-wide `AppClosingToken` should become an app shutdown token linked
to each session's own token. Stopping one client cancels only that session.
Closing the app cancels the parent token and therefore every child.

No raw packet from one session may be published on another session's event
stream. Packet capture views should bind to a selected session or an explicit
multi-session view whose rows carry a non-secret client label.

### 4.6 Per-session bot model

Separate the Bot Manager's model from its controls:

- `BotDocumentState`: commands, skills, quests, drops, boosts, items, dirty
  state, source path, and display name;
- `BotRunState`: immutable run ID, captured session, captured configuration,
  index, variables, cancellation, tasks, and error;
- `BotSession`: the document plus its engine and runtime option state;
- `BotManager`: a view bound to the currently selected `BotSession`.

Before changing the selected client, the view commits pending edits to the old
document, detaches event subscriptions, binds the new document, and then
refreshes controls. A running engine never reads live checkboxes from the form.
It receives a snapshot when the run starts. This removes the current coupling
where background code reads `BotManager.Instance.chk...`.

`BotManager.Instance` can remain for binary/source compatibility as the one
active-session view. Its public members continue to represent the selected
client. Built-in background work must no longer depend on those controls.

A loaded GBot belongs to one session. Quick-loading a bot replaces the selected
session's document only after the same running-bot protections used today.
Other clients and their documents are unaffected.

### 4.7 Client workspace and selector

Add a compact, themed client strip above the game workspace or directly below
the main toolbar. Recommended first-version behavior:

- Home is a shell view, not a fake client.
- Each client tab shows a generated label and small connection/bot indicator.
- Tabs themselves are not closed through a tiny tab close glyph. A clear
  **Close Client** command invokes the proper confirmation and lifecycle path.
- Only the selected `ClientWorkspace` is presented to the user.
- Selecting a tab gives focus to its Flash control after binding the toolbar.
- Reordering tabs changes presentation only; session IDs and bot targets remain
  stable.
- Empty, loading, disconnected, faulted, and ready states have visible
  placeholders instead of a black or stale game surface.

Because hidden ActiveX behavior must be proven, the spike should compare two
hosting strategies:

1. tab pages that toggle `Visible`;
2. stacked workspace panels where only the selected panel is brought forward.

Choose the approach that preserves background timers and input correctness with
the least CPU/GDI cost. Do not add speculative background FPS throttling until
tests prove it does not alter bot timing or client behavior.

## 5. Toolbar isolation contract

The toolbar should be a projection of the selected session, not the storage for
session state. A `ToolbarSessionBinder` listens to active-session changes and
updates controls while an `isBinding` guard prevents programmatic refreshes
from invoking effects.

When a user changes an item:

1. capture the selected session reference;
2. validate that the session is still active and in a compatible lifecycle
   state;
3. update that session's model/controller;
4. apply the effect through that session's Flash bridge;
5. refresh only from the resulting session state.

The captured reference matters. An asynchronous handler must not ask for
`ActiveSession` again after an `await`, because the user may have switched tabs.

| Control or action | Ownership | Required behavior |
| --- | --- | --- |
| Home | Global shell | Shows Home without stopping or retargeting clients |
| New/switch/close client | Session manager | Creates, selects, or safely disposes one session |
| Start/stop/pause bot | Per session | Operates the selected session's engine; a running engine retains its original session |
| Bot Manager / load bot / quick bot | Catalog global, document per session | Catalog is shared; selection and loaded document belong to one client |
| Immediate login and server | Per session | Uses that session's login token and bridge |
| Cell, pad, get position, jump | Per session | Reads and writes only the selected game state |
| Ping text/monitor | Per session | Displays the selected client's latest ping and preference |
| Set FPS | Per session | Stores desired FPS per client and calls only its bridge |
| Bank/reload | Per session | Uses the selected client's player, room, and proxy |
| Infinite range | Per session | Independent checked state and bridge effect |
| Provoke / provoke all | Per session | Independent loop, packet routing, and stop cleanup |
| Enemy magnet | Per session | Independent option loop |
| Lag killer | Per session | Independent checked state and bridge effect |
| Hide players | Per session | Independent checked state and bridge effect |
| Skip cutscenes | Per session | Independent option loop and bot cell/pad state |
| Disable animations | Per session | Independent checked state and bridge effect |
| Untarget self | Per session | Independent option loop |
| Autosave state | Per session | Independent timer, token, and account target |
| Walk speed | Per session | Independent desired value and option loop |
| Status/traffic indicator | Per session | Recomputed from that client's compatibility, room, and option state |
| Packet logger/tamper/spammer | Per session view | Requires an explicit target; capture streams never mix silently |
| Travel, loaders, cosmetics, bank, DPS | Active-session tool view | Window title and selector show its current binding; async actions capture it |
| Header visibility | Global shell | Keeps current app-wide behavior |
| Settings | Global shell | Global by default; any client defaults must be labeled as defaults |
| Theme | Global shell | Applies to all open forms and workspaces |
| Device metrics | Global process | Continues to describe the whole process |
| Update button | Global app | Checks one release and coordinates all sessions before restart |

### Toolbar states

- **Home/no active client:** disable client-specific items.
- **Initializing:** allow switching and safe close; disable game calls.
- **Ready/login screen:** enable account/login/server actions and safe visual
  effects supported at that stage.
- **Logged in:** enable normal client tools.
- **Bot running:** disable bot replacement for that client, not for other
  clients.
- **Faulted/disconnected:** show retry/close and preserve diagnostics; never
  leave checked controls implying an effect is still active.
- **Switch in progress:** bind atomically so controls never briefly show one
  client's check state while invoking another.

Hotkeys target the client selected at the instant the key is handled. A hotkey
action captures that session before starting asynchronous work. The first
version should not add global shortcuts that start or stop every bot.

## 6. Home, accounts, tools, and plugins

### Home bridge

Replace the single `clientRunning` response with a bounded array of safe client
snapshots containing only:

- session ID;
- generated display label;
- lifecycle state;
- load percentage/stage;
- whether it is selected;
- whether a bot is running;
- a non-secret profile display value only when the existing product policy
  permits it.

Home actions receive and validate a session ID. They must not accept arbitrary
paths, process commands, or decrypted credentials. `OpenAccount` may pass the
retrieved credential object directly to a newly created session in memory, but
the password never appears in a bridge response.

### Tool windows

Retain one instance of each large tool window initially, but make its binding
explicit. The title should show a generated client label, and a session switch
must either rebind safely or keep the window pinned to the client it was opened
for. Recommended defaults:

- Bot Manager, bank, cosmetics, travel, loaders, DPS, eye dropper, packet tools,
  and logs bind to a selected/pinned session.
- Settings, Hotkeys, Plugin Manager, and Notepad remain global.
- Packet and log views expose an explicit client selector and never merge data
  without labeling it.

Async tool operations capture the binding at start. Closing a bound client
cancels its tool work and moves the view to a safe disconnected state.

### Plugins

Existing plugins execute in-process and generally rely on static APIs. Preserve
the current plugin interface, but define its multi-client compatibility
semantics honestly:

- legacy plugin calls from UI event handlers target the selected session;
- a plugin operation started inside a documented session scope retains that
  session across asynchronous continuations;
- legacy plugins that create unscoped background threads cannot be guaranteed
  multi-client safe;
- add an optional session-aware plugin interface in a later phase without
  removing `IGrimoirePlugin`;
- the Plugin Manager should display whether a plugin declares session support.

Do not silently load plugins once per session. Assembly loading and plugin
lifecycle remain process-global unless a new plugin contract explicitly says
otherwise.

## 7. Single-application behavior

Do not block multiple processes until the session architecture is stable. A
premature mutex would remove the user's only current workaround.

After multi-client passes its release gates, add a per-Windows-user,
per-installation instance broker:

- a named mutex identifies the primary shell;
- a named pipe secured to the current Windows user carries small commands;
- a second launch sends `activate`, `new-client`, or an explicitly requested
  file-open command, then exits;
- credentials and account-vault contents are never sent over the pipe;
- malformed, oversized, or unknown requests are rejected;
- if the primary process is unresponsive, the second process reports recovery
  choices rather than assuming it is safe to kill anything;
- installed and intentionally separate portable copies use different
  installation identities so they do not capture each other's requests.

The broker is a convenience boundary, not the client-state transport. All game
state remains inside the primary shell's session manager.

## 8. Persistence, recovery, shutdown, and updates

### Settings

Keep app preferences global. Split the current `BotClientConfig` meanings into:

- global defaults copied into a newly created client;
- runtime `ClientOptionState` owned by each session;
- explicitly labeled per-client overrides only if persistence is added later.

Do not write one session's live check state over another's. The first version
should persist defaults, not automatically restore live effects or accounts.

### Recovery

Extend the process session marker additively with bounded client summaries:

- generated session ID/label;
- lifecycle stage;
- whether a bot was running;
- the bot recovery reference needed for review;
- last update time.

Do not record passwords, vault records, packet bodies, room identifiers, or raw
character data. On recovery, offer bot documents for review per affected
session but never reconnect accounts or start bots automatically.

### Closing one client

1. Capture the target session.
2. If its bot is running, show a client-specific confirmation.
3. Cancel login, options, bot, packet, and tool work for that session.
4. Wait with bounded deadlines and report a generic failure if work does not
   stop.
5. Detach callbacks and handlers.
6. Dispose its ActiveX control on the UI thread.
7. Remove it from the manager and select a safe neighboring client or Home.

Nothing in this path may cancel the app token or another session.

### Closing or updating the app

App close and update restart should use aggregate counts, for example the
number of connected clients and running bots, without showing private account
or bot names. After confirmation, stop all sessions in parallel where safe,
dispose ActiveX controls serially on the UI thread, then complete the existing
clean-shutdown/update path.

An update remains process-global and must not begin its restart while any
session still owns unresolved shutdown work. Portable merge and MSI behavior
remain unchanged.

### Tray

Keep one tray icon. Its tooltip and menu may show aggregate state and generated
client labels. Restore returns to the previously selected session. Minimize to
tray does not pause or retarget any session.

## 9. Implementation phases and gates

Each phase should be a focused, reviewable change that leaves the ordinary
one-client workflow usable.

### Phase 0 — establish test seams and baselines

- Record one-client startup, idle, logged-in, and bot-running CPU, private
  bytes, handle/GDI counts, and callback latency using sanitized output.
- Add interfaces for a Flash bridge, session clock/delay, and diagnostics
  without changing behavior.
- Add fake bridges and deterministic session tests.
- Add a feature flag that remains off by default until the later release gate.

**Exit gate:** existing one-client build, Flash activation, startup diagnostics,
updates, and legacy GBot tests pass with no user-visible regression.

### Phase 1 — two-Flash-control feasibility spike

- Create a temporary harness using the exact app-local OCX, interop assemblies,
  manifest, `OcxState`, and checked-in SWF.
- Exercise the ActiveX cases listed in section 3.3.
- Compare hidden-tab and stacked-panel hosting.
- Run repeated create/switch/reload/dispose cycles and an extended soak.
- Keep the harness/test, but do not merge prototype shortcuts into production
  classes.

**Go:** controls, callbacks, input, background behavior, and resource use are
independent and stable.

**No-go:** shared runtime state, stopped hidden clients, unfixable input/focus
errors, or unsafe resource growth. At no-go, investigate one shell with
isolated worker processes and a supported rendering/focus strategy; do not use
ad-hoc cross-process `SetParent` as a quick fix.

### Phase 2 — introduce session ownership with one live client

- Add `ClientSession`, `ClientSessionManager`, lifecycle states, and session
  cancellation.
- Move the existing control into a `ClientWorkspace` owned by the first
  session.
- Add the scoped session context and development guards.
- Keep exactly one session enabled in normal UI.

**Exit gate:** the app behaves like the current release with one client, and
all existing calls resolve to that explicit session.

### Phase 3 — isolate Flash, packets, and game state

- Make the Flash bridge instance-based and route callbacks by sender.
- Create a proxy and handler set per session.
- Move mutable `Player`, `World`, map, bank, quest, aura, drop, and login state
  into `GameSessionState`.
- Keep legacy static facades routing through the scoped session.
- Add two fake sessions that deliberately interleave events.

**Exit gate:** no test can make a callback, packet, event, or state mutation
cross sessions.

### Phase 4 — isolate bot execution and options

- Add `BotSession`, `BotDocumentState`, and per-run state.
- Move bot variables, indexes, drop tracking, bot data, utilities, and option
  loops out of static storage.
- Remove built-in bot runtime reads from Bot Manager controls.
- Capture the session and configuration at start.
- Make every owned task and event callback cancellable and stale-run aware.

**Exit gate:** two fake or live bots can run concurrently; switching the active
session repeatedly cannot change either bot's target or configuration.

### Phase 5 — add client UI and toolbar binding

- Add the themed client strip and session placeholders.
- Add New Client, Switch, and Close Client flows.
- Implement `ToolbarSessionBinder` and the ownership table in section 5.
- Rebind lower-panel cell/pad/ping/status controls.
- Make hotkeys capture the selected session.
- Validate the default 960×620 layout, keyboard navigation, DPI behavior, and
  both themes.

**Exit gate:** all toolbar effects remain independent through rapid switching,
loading, disconnect, bot start/stop, and client close.

### Phase 6 — bind Home, tools, recovery, tray, and updater

- Extend the Home bridge with bounded session snapshots and targeted actions.
- Rebind or pin tool windows explicitly.
- Extend recovery activity to multiple sessions.
- Aggregate close, tray, full-reset, and update behavior.
- Define legacy plugin active-session behavior and optional session-aware
  capability metadata.

**Exit gate:** all app-wide lifecycle operations account for every session and
never disclose secret or machine-specific data.

### Phase 7 — enable multiple live clients behind an opt-in flag

- Enable the measured client limit for testers.
- Run two- and three-client soaks where supported.
- Compare one multi-client shell against the equivalent number of current
  single-client processes.
- Collect only aggregate, privacy-safe diagnostics.
- Resolve resource, input, and lifecycle regressions before default enablement.

**Exit gate:** the release matrix passes and no target-misrouting event is
observed.

### Phase 8 — single-instance broker and public rollout

- Add the per-user/per-installation mutex and secured command pipe.
- Forward focus/new-client/file-open requests from later launches.
- Preserve a safe fallback when the primary shell is unresponsive.
- Turn the multi-client feature on by default only after upgrade, installer,
  portable, and rollback validation.

**Exit gate:** normal second launch activates the existing shell, while
recovery and intentionally separate installations still work safely.

## 10. Validation plan

### Automated tests to add

- `Test-ClientSessionIsolation.ps1`: fake bridges, interleaved calls, state,
  events, cancellation, and disposal.
- `Test-MultiFlashActivation.ps1`: two real controls, handles, independent
  initialization, callback sender routing, switching, and disposal.
- `Test-MultiClientBotIsolation.ps1`: concurrent engines retain their owning
  session and Flash bridge across awaits and rapid active-client switching;
  stopping one run does not stop or stall the other. Independent relogin and
  live-game event coverage remains to be added.
- `Test-ToolbarSessionBinding.ps1`: checked/enabled/value state, event
  suppression, captured async target, and empty/faulted states.
- `Test-MultiClientRecovery.ps1`: additive recovery schema, bounded summaries,
  clean shutdown, and no automatic restart.
- `Test-SingleInstanceBroker.ps1`: same-user forwarding, malformed request
  rejection, size limits, timeout, and per-installation separation.

Continue running:

- Debug and Release builds through `exodus.ps1`;
- repository-controlled legacy GBot compatibility and round trips;
- update selection and updater merge/rollback tests;
- startup diagnostics tests;
- Flash activation tests;
- installer and portable release-equivalent validation when packaging changes.

### Manual matrix

| Scenario | Required result |
| --- | --- |
| Two clients load concurrently | Progress, errors, and readiness remain independent |
| Login both clients | Credentials and server selection reach only the chosen client |
| Start different GBots | Each bot retains its client through tab switches |
| Toggle every toolbar effect | Only the selected client changes; returning to each tab restores its state |
| Use cell/pad/jump rapidly while switching | Captured action never reaches the new tab accidentally |
| Packet capture on one client | No unlabeled packet from the other client appears |
| Hide/minimize/restore | All sessions continue according to tested policy; focus returns accurately |
| Close an idle client | Other clients remain untouched |
| Close a client with a bot | Only that client prompts and stops |
| Close/update app with several bots | One aggregate confirmation, bounded clean stop, correct updater handoff |
| Crash and restart | Recovery offers documents for review and starts no bot/account |
| Light/dark at 960×620 | Client strip, toolbar, tool windows, dialogs, and ActiveX area render correctly |
| Legacy GBot corpus | Load and explicit save round-trip without type/order/meaning regression |
| Legacy plugin | Existing load/unload works; active-session limitation is visible and truthful |

### Performance and stability gates

Establish actual baselines in Phase 0, then use these provisional gates:

- zero cross-session bridge calls, packet events, game-state mutations, or bot
  actions;
- no unbounded packet, log, callback, or diagnostic queue;
- no monotonic private-byte, handle, or GDI growth after warm-up during a
  one-hour six-client opt-in soak, plus bounded override stress testing;
- client switching remains responsive and does not recreate the ActiveX
  control;
- bot stop remains bounded, with cancellable waits targeting completion within
  one second when no external game operation is in flight;
- aggregate CPU and memory are measured against the same number of current
  separate instances before choosing the public client limit;
- the process retains safe x86 address-space headroom under the worst supported
  client count, map transitions, and loaded tool windows.

Do not claim a supported maximum from a developer machine alone. Validate on a
lower-memory supported Windows system as well.

## 11. Risk register

| Risk | Severity | Mitigation / release gate |
| --- | --- | --- |
| Running bot is retargeted by tab switch | Critical | Captured session per run, scoped ingress, explicit services, isolation tests |
| Flash callback answers the wrong control | Critical | Instance handler uses event sender/owning bridge; two-control callback test |
| Static game state leaks between accounts | Critical | Per-session state storage; interleaved event tests; development guard |
| Hidden Flash client pauses or loses network timers | High | Mandatory real-runtime spike; choose proven host strategy |
| x86 memory/handle exhaustion | High | Opt-in multi-client mode, six-client safety cap, explicit override warning, resource telemetry, soak tests, graceful initialization failure |
| UI-thread/COM deadlock | High | Synchronous UI marshal only at bridge boundary; no UI sync-over-async; stress tests |
| Bot Manager edits/runs the wrong document | High | Separate model from view, atomic detach/rebind, engine configuration snapshot |
| Packet/log data mixes clients | High | Per-session streams/buffers and explicit labeled aggregate view |
| One client close cancels all clients | High | Linked app/session tokens with ownership tests |
| Update/reset interrupts an unknown running bot | High | Aggregate lifecycle coordinator and confirmation |
| Legacy plugin creates unscoped background work | High | Compatibility semantics, capability label, optional v2 session API, diagnostics |
| Programmatic toolbar refresh fires an effect | Medium | Binding guard, model-first updates, binder tests |
| ActiveX focus/input fails after switching | Medium-high | Repeated focus, DPI, mouse, keyboard, and tab-switch tests |
| Single-instance broker blocks recovery | Medium-high | Add last, bounded timeout, no process killing, per-install identity |
| Recovery stores sensitive multi-client data | Medium-high | Bounded safe schema, no credentials/packets/rooms, privacy review |

## 12. Compatibility and rollback strategy

- Keep multi-client behind a feature flag until Phase 7 exits.
- The default path through Phases 0–6 creates one session and should remain
  behaviorally equivalent to the current app.
- Keep serialized command classes, property meanings, namespaces, binder,
  `IBotCommand`, `IBotEngine`, and assembly identity unchanged.
- Add new C# files explicitly to the non-SDK project.
- Do not change the checked-in SWF unless the bridge contract actually changes.
- Keep the old single-client construction path available for one development
  cycle as a rollback switch, then remove it only after release soak results.
- Do not enable the single-instance mutex before multi-client is the default;
  users must retain the old multi-process workaround during development.
- If the real ActiveX spike fails, discard the spike rather than layering tabs
  over shared static state. Reassess process isolation as a separate project.

## 13. Recommended first implementation slice

The safest first code change after approval is not the tab strip. It is:

1. add `IFlashBridge` and an adapter around the existing one control;
2. add `ClientSession`/`ClientSessionManager` with a one-session limit;
3. route the current callback and calls through that session without changing
   visible behavior;
4. add fake isolation tests;
5. add the separate real two-control feasibility test.

This slice proves the central boundary, keeps the normal application on one
client, and provides a clean point to stop if the Flash runtime does not support
the required independence.

## 14. Final recommendation

Proceed, but treat multi-client as an isolation program rather than a UI
feature. The order is non-negotiable for account safety:

```text
prove multiple Flash controls
  -> introduce one-session ownership
  -> isolate bridge, packets, and game state
  -> isolate bots and options
  -> bind tabs and toolbar
  -> integrate lifecycle and tools
  -> enable multiple clients
  -> redirect second launches
```

The most important acceptance rule is simple: changing the selected client may
change what the user sees, but it must never change what an existing bot,
packet handler, timer, login flow, or asynchronous toolbar action controls.

## 15. Implementation status

The production shell now starts in the legacy-compatible single-client mode.
An opt-in experimental setting enables up to six live clients with one AQW
surface visible at a time. A separate Advanced setting removes the software cap
while keeping the actual x86 memory, handle, and CPU limits explicit. The
implementation includes:

- `ClientSession`, `ClientSessionManager`, lifecycle cancellation, and scoped
  legacy API routing are active in the normal one-client startup path.
- Flash callbacks and synchronous calls resolve the owning control; packet
  dispatchers and handler sets are owned per session.
- mutable player, world, map, server, bot-data, bot-variable, and option state
  is stored per session while the legacy static APIs remain available.
- each session owns a bot engine, editable `BotDocumentState`, login operation,
  load progress, and latest ping sample.
- Bot Manager commits the old document, detaches captured engine callbacks,
  binds the selected session, and suppresses option effects during a
  programmatic rebind.
- bot start, stop, pause, resume, nested bot loading, option workers, immediate
  login, hotkeys, and the migrated toolbar actions retain a captured session
  instead of following a later selection.
- the toolbar now projects per-session option, FPS, bot, login, cell/pad, and
  ping state through a binding guard.
- a themed client strip creates, selects, and closes clients without recreating
  the other ActiveX control. Home exposes the same bounded client list and can
  add or open a selected client.
- each client owns its load progress, account-fill timer, Flash control, proxy,
  game state, bot engine, bot document, options, and lifetime cancellation.
- closing a logged-in client or one with a running bot requires confirmation
  and disposes only that session. Whole-app close and update checks aggregate
  running bots and logged-in clients.
- later launches from the same Windows user and installation use a secured,
  bounded local pipe to restore the existing shell and request an additional client
  instead of creating another full Grimoire process.
- a deterministic concurrent-bot smoke test now runs two real `Bot` engines,
  switches the active session repeatedly, verifies client ownership before and
  after command awaits, and confirms that stopping one run leaves the other
  running and making progress.
- the packet spammer worker is owned and cancelled per session. Packet Sniffer,
  Packet Tamperer, and Packet Spammer windows now bind to the selected client
  when opened, include the generated client label in their title, and route
  capture/send operations only through that binding. Capture output from other
  sessions is rejected rather than merged into an unlabeled view.

The real-control smoke test loads the checked-in bootstrap in two app-local
ActiveX controls concurrently, observes AQW progress from both callback
senders, and proves that disposing one control leaves the other alive. Fake
session and concurrent-bot checks cover state, configuration, command targeting,
independent stops, and disposal. The repository-controlled legacy GBot suite
continues to load and round-trip the existing format.

Remaining follow-up work is intentionally outside the initial multi-client
release:

1. extended two-account login, repeated mouse/keyboard switching, map-change,
   hidden-client timer, and lower-memory soak testing;
2. explicit pinning or aggregate views for the remaining legacy singleton tool
   windows and general logs;
3. additive multi-session recovery summaries without credentials, packets,
   rooms, account labels, or automatic bot/account restart;
4. optional split-screen rendering only after its separate performance and
   input-accuracy gates pass;
5. measured x86 memory, handle, CPU, and long-duration stability guidance for
   users who opt into the unlimited override.
