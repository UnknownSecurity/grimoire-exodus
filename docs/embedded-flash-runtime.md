# Desktop game-engine architecture

## Compatibility requirements

Grimoire does more than render a SWF. Its checked-in ActionScript bridge loads
the current AQW AVM2 client, calls the desktop host synchronously through
`ExternalInterface`, exposes the bot API back to C#, and observes SmartFox
traffic so packet handlers and each session's game model remain current.

Every engine therefore has to preserve rendering, AVM2 execution, raw TCP game
networking, synchronous callback return values, packet observation, and
per-client ownership. Legacy `.gbot` models remain above this boundary and do
not change with the selected engine.

## Flash.ocx (default)

The default engine uses the app-local 32-bit ActiveX runtime through
registration-free COM. `app/Grimoire.manifest` maps the Flash CLSID and type
library to `Flash.ocx` beside the application, so Grimoire never registers or
replaces system Flash. `exodus.ps1` verifies the pinned archive and extracted
OCX independently, and `tests/Test-FlashActivation.ps1` verifies that the real
control loads the application-local module.

This remains the recommended compatibility path and the automatic fallback.
The legacy runtime is only for the trusted bundled AQW bridge and must never be
used as a general-purpose player for untrusted SWFs.

## Ruffle in WebView2 (experimental)

The optional engine uses the official self-hosted Ruffle 0.7.1 web package in
an isolated WebView2 control. It is opt-in under Settings → Performance and is
used only for clients created after the setting is saved. Running clients and
bots are never hot-switched.

Each Ruffle client owns:

- its WebView2 surface and writable profile below application-owned data;
- a random-token, loopback-only HTTP origin serving an exact file allowlist;
- a same-origin proxy restricted to `https://game.aq.com/game/`;
- a server map that replaces AQW server destinations with synthetic slots;
- bounded WebSocket-to-TCP tunnels for those slots; and
- a synchronous WebView2 host object that preserves `modifyServers` and the
  existing C#-to-ActionScript bot calls.

The adapter never records HTTP bodies, credentials, packets, server payloads,
room names, or account details. It rejects missing tokens, traversal, general
proxy destinations, private socket destinations, oversized requests/frames,
and unknown static files. All adapter state is owned by one `ClientSession`, so
multi-client packet and bot state stays separated.

The build downloads a pinned Ruffle archive, validates the archive SHA-256,
extracts only the required runtime and license files, then validates every
file independently. WebView2 SDK binaries and their license/notice are staged
with both MSI and portable packages. If either the verified Ruffle payload or
the Microsoft WebView2 Runtime is unavailable, only the requested new client
falls back to Flash.ocx.

## Upstream boundaries

Stock Ruffle desktop was not selected because its host does not expose
Grimoire's callback ABI. The web runtime provides JavaScript
`ExternalInterface`, `callExternalInterface`, URL rewriting, and socket-proxy
configuration, while WebView2 supplies the synchronous host-object boundary.

- Ruffle: <https://github.com/ruffle-rs/ruffle>
- WebView2: <https://developer.microsoft.com/microsoft-edge/webview2/>
- Registration-free COM: <https://learn.microsoft.com/windows/win32/sbscs/registration-free-com>
- App-local Flash source: <https://github.com/darktohka/clean-flash-builds/releases/tag/v1.54>

The detailed decision record, repeated design reviews, failure rules, and live
validation matrix are in [desktop-ruffle-engine-plan.md](desktop-ruffle-engine-plan.md).
