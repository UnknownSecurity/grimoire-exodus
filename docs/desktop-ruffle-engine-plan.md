# Desktop Ruffle engine implementation plan

Status: implemented behind the experimental desktop engine selector. The
synthetic bridge, isolation, transport-boundary, fallback, legacy-GBot, and
packaging checks pass; the live AQW acceptance matrix remains a release gate
before Ruffle can be considered stable.

The compatibility baseline remains the private, registration-free 32-bit
`Flash.ocx` engine. Ruffle is opt-in, applies only to clients created after the
preference is saved, and must fail closed without interrupting an existing
Flash client or bot.

## 1. Contract inventory

Grimoire does more than render the AQW SWF. A replacement engine must preserve
all of these contracts per `ClientSession`:

| Contract | Existing ActiveX behavior | Ruffle acceptance gate |
| --- | --- | --- |
| AVM2 and display | Loads `grimoire-exodus.swf`, which loads the current official AQW client | The same checked-in bootstrap SWF reaches 100%, remains interactive, and renders at the existing game surface size |
| C# to ActionScript | Synchronous `CallFunction` with XML values | `IFlashBridge` remains synchronous to callers and returns the same CLR values for every registered callback |
| ActionScript to C# | Synchronous `ExternalInterface.call`; `modifyServers` returns a value immediately | Ruffle calls a synchronous WebView2 host object and receives the modified login response in the same ActionScript stack |
| Packet observation | The bootstrap observes SmartFox events and reports packets through the host bridge | `packetFromClient`, `packetFromServer`, `packet`, `xtPacket`, and `pext` reach the owning session's `Proxy` in order |
| Raw sockets | Flash opens the selected SmartFox TCP endpoint directly | Ruffle's web socket API is mapped through a loopback WebSocket-to-TCP tunnel to the exact server selected from the official login response |
| HTTP assets and login | Flash security policy permits the official AQW origin | A loopback HTTP adapter forwards only `https://game.aq.com/game/` requests, including login POSTs, without logging request bodies or credentials |
| Multi-client ownership | One ActiveX control and bridge per `ClientSession` | Every Ruffle control, localhost token, server map, socket tunnel, proxy, bot, cancellation source, and writable browser profile remains session-safe |
| Legacy GBots | Commands call the engine-neutral `Flash` facade and keep the legacy assembly identity | Existing `.gbot` types and serialization are unchanged; the same command calls work through either `IFlashBridge` implementation |
| Failure behavior | Flash activation errors leave the client unavailable | Ruffle initialization failures are diagnostic-only, never expose secrets, and fall back to Flash before login when it is safe to do so |

## 2. Architecture candidates

### A. Self-hosted Ruffle in WebView2 with a local adapter — selected

The official self-hosted Ruffle package provides AVM2, GPU-backed rendering,
JavaScript `ExternalInterface`, and a configurable WebSocket proxy for
`flash.net.Socket`. WebView2 supplies the WinForms surface. A small per-client
loopback service supplies the pieces a browser sandbox cannot provide:

1. Serve the pinned Ruffle files and checked-in Grimoire SWF from a random,
   unguessable loopback URL.
2. Rewrite only the official AQW game origin to the local adapter so browser
   CORS does not change the game's HTTP behavior.
3. Forward bounded HTTP requests to the fixed official HTTPS origin.
4. Rewrite the authenticated server list to synthetic loopback socket slots.
5. Map each slot to the original host and port and relay binary WebSocket
   frames to that TCP endpoint.
6. Dispatch ActionScript host calls synchronously through a WebView2 host
   object while keeping the owning `ClientSessionContext` active.
7. Keep return-bearing callbacks such as `modifyServers` synchronous, but post
   packet and notification callbacks to one ordered UI queue after the browser
   host call returns. This prevents a packet handler that calls ActionScript
   from re-entering WebView2's blocked host-object stack.
8. Execute bot-to-ActionScript calls on the WebView UI thread and synchronously
   return their JSON result to existing bot code with a bounded timeout.

This route uses supported public Ruffle web APIs and avoids maintaining a Rust
fork. Its main risk is the synchronous compatibility shim over WebView2's
asynchronous script API; calls must be serialized on the UI thread, bounded,
and rejected on re-entrancy instead of hanging the application.

### B. Custom native Ruffle host/helper — deferred fallback

A Rust helper could embed `ruffle_core`, provide a custom
`ExternalInterfaceProvider`, use native TCP sockets, and communicate with
Grimoire through authenticated IPC. It would avoid browser CORS and WebView2
script latency, but upstream exposes no stable C ABI or embeddable desktop
window contract. Maintaining a fork of the desktop renderer and window loop is
too large and fragile for the first experimental engine.

### C. Stock Ruffle desktop executable — rejected

The stock desktop host can render and open sockets, but it does not expose
Grimoire's callbacks. Its `on_callback_available` handler is empty and unknown
host calls return `Undefined`, so bots and packet handling cannot work.

## 3. Selected implementation boundaries

### Preferences and UI

- Add `GameEngineKind` with `FlashActiveX = 0` and `Ruffle = 1`.
- Missing, invalid, or older configuration values resolve to `FlashActiveX`.
- Enable the existing Settings engine selector and clearly label Ruffle as
  experimental.
- Require a warning confirmation when switching from Flash to Ruffle.
- Do not hot-swap or close a running client. The setting affects newly created
  clients and the next application launch.

### Engine-neutral session host

- Keep `IFlashBridge` as the sole bot-facing game API.
- Add an engine-neutral WinForms host control and engine identity while
  retaining the nullable ActiveX control property for legacy plug-ins.
- Add a load operation to the bridge so session creation does not need to know
  whether bytes are assigned to `OcxState` or loaded by Ruffle.
- Generalize workspace focus and visibility to the host control; keep Flash
  presentation properties limited to ActiveX.

### Ruffle host and transport

- One `RuffleFlashBridge` and one loopback host per Ruffle client.
- Bind only `127.0.0.1` on an operating-system-selected port.
- Require a random token in every HTTP and WebSocket path.
- Serve an allowlist of exact Ruffle runtime files; never expose arbitrary
  filesystem paths.
- Proxy only the fixed `https://game.aq.com/game/` origin.
- Bound request headers, request bodies, response sizes where buffering is
  required, bridge arguments, frame sizes, timeouts, and concurrent tunnels.
- Never log HTTP request bodies, account data, packets, room names, or complete
  remote URLs containing query data.
- Dispose the listener, HTTP client, WebView, sockets, and cancellation source
  with the owning session.

### Failure and fallback

- Check the Ruffle file manifest and WebView2 Runtime before choosing Ruffle.
- If either prerequisite is unavailable, create that client with Flash and
  show one actionable warning.
- If Ruffle fails before the SWF becomes ready and no bot or login is active,
  replace only that failed session with a Flash session.
- Never automatically replace a ready/logged-in client or a client with a
  running bot. Mark it faulted and preserve the normal close confirmation.
- A bridge call timeout returns the same safe default used by the ActiveX
  bridge and records a privacy-safe diagnostic.

## 4. Supply chain and packaging

- Pin `@ruffle-rs/ruffle` 0.7.1 from the official npm registry.
- Verify the package SHA-256 before extracting it and verify every shipped JS,
  WASM, and license file independently.
- Pin the official `Microsoft.Web.WebView2` NuGet SDK and retain its loader
  layout in portable and MSI output.
- Include the Ruffle MIT and Apache-2.0 license files in the runtime directory
  and document the dependency in the repository notices.
- Never commit downloaded Ruffle binaries, WebView2 binaries, caches, or user
  data. Release staging must fail when required Ruffle files are absent.
- Preserve `Flash.ocx` in every package because it remains the default and the
  safe fallback.

## 5. Repeated design review

### Review 1 — bot and bridge parity

- All built-in bot commands reach the game through `Flash.ResolveBridge()`;
  no GBot model or serialized command needs to change.
- Ruffle must reproduce the two distinct return paths: normal primitive calls
  and callbacks that return a JSON string for typed deserialization.
- `modifyServers` is the only current outbound ActionScript call that needs an
  immediate non-null return. It must run inside the session context before the
  response is rewritten for socket slots.
- Packet calls remain ordered because each ActionScript call synchronously
  enters the owning bridge before returning.

Result: the adapter can preserve the public bot ABI without changing legacy
GBot files. Automated bridge tests and live AQW acceptance remain mandatory.

### Review 2 — multi-client, threading, and recovery

- Each page has its own random origin path, server map, WebSocket paths, and
  bridge host object.
- Script calls are marshalled to the owning WebView UI thread and serialized
  there. No static Ruffle player or server map is allowed.
- Background/inactive-client visibility policy continues to operate on the
  engine-neutral host control.
- Session cancellation closes only that client's network activity.
- Fallback is limited to pre-ready clients so recovery cannot silently stop a
  bot or logged-in character.

Result: the selected architecture is compatible with current session
isolation if every new mutable object remains session-owned.

### Review 3 — deployment, rollback, and legacy behavior

- Configuration defaults, missing runtime files, unsupported Windows systems,
  and absent WebView2 all select Flash.
- Existing Flash builds continue through the unchanged ActiveX path.
- Portable merge and MSI upgrades treat Ruffle as application-owned immutable
  payload while writable WebView state stays under `AppPaths`.
- The updater still validates the release asset digest and payload boundaries;
  required-file validation expands rather than weakens.
- Removing Ruffle files makes only the experimental engine unavailable and
  does not prevent the default Flash engine from starting.

Result: Ruffle can be added without weakening the current release or recovery
contracts.

## 6. Validation gates

Current validation gates are:

1. **Passed:** preference default, persistence, invalid-value fallback, and
   opt-in warning paths.
2. **Passed:** Ruffle package hash, per-file hash, license, release-staging,
   updater-payload, and installer-harvest checks.
3. **Passed synthetically:** loopback token and path checks, exact runtime
   allowlisting, socket-slot isolation, WebSocket handshake/masking/control
   rules, frame limits, and private-destination rejection. Live HTTP and TCP
   behavior is still covered by gate 7.
4. **Passed synthetically:** bridge conversion for null, Boolean, number,
   string, array, object, and JSON-string results.
5. **Passed:** per-session Ruffle callback progress isolation, client-session
   state isolation, concurrent bot ownership, and a live hidden WebView2 probe
   that verifies a deferred host callback can call back into ActionScript
   without blocking. Simultaneous live AQW packet traffic is still covered by
   gate 7.
6. **Passed:** Debug and Release builds, repository-controlled legacy GBot
   compatibility, Flash activation, multi-client Flash activation,
   client-session isolation, updater selection/merge/rollback, Doctor,
   support-report privacy, and release-equivalent tests.
7. **Not run while an existing Grimoire instance is in use:** manual live AQW
   acceptance for loading, login, server selection, map join,
   combat, quests, drops, shops, bank, packets, chat commands, reconnect,
   account selection, and representative legacy GBots.
8. **Not run while an existing Grimoire instance is in use:** manual light/dark
   Settings verification and 960×620 layout verification.

Passing source and synthetic tests proves the adapter contract; it does not by
itself prove complete AQW/Ruffle parity. The engine remains explicitly
experimental until the live acceptance matrix has been exercised on a clean
machine and on a machine with multiple clients.
