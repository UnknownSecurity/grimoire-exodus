# Embedded Flash runtime research

## Requirements found in Grimoire

Grimoire is not just displaying a SWF. The checked-in ActionScript 3 bridge:

- loads the current AQW AVM2 game client at runtime;
- calls C# synchronously through Flash `ExternalInterface`;
- exposes hundreds of callbacks back to C# through `CallFunction` and
  `FlashCall` XML values;
- observes the game's SmartFox events so the proxy, packet logger, and bot
  engine receive the same network state as the visible client.

Any replacement therefore has to preserve rendering, AVM2 behavior, raw game
networking, and the bidirectional host ABI at the same time.

## Options evaluated

### Ruffle in WebView2

Ruffle is the strongest open-source Flash emulator and is worth revisiting.
Its web build is not a drop-in host here, however: a browser WebView cannot
open the AQW client's raw SmartFox TCP connection without an additional
WebSocket-to-TCP service, and Grimoire's ActiveX event/call bridge would have
to be replaced with an asynchronous JavaScript bridge.

### Ruffle desktop/custom host

Ruffle core has AVM2 socket and ExternalInterface implementations, but its
stock desktop host does not expose Grimoire's callbacks: its desktop
`on_callback_available` handler is empty and unknown host calls return
`Undefined`. A viable Ruffle migration would therefore be a separate project:
build a custom Rust host/FFI surface, port the synchronous bridge, validate the
complete AQW client, and re-test every automation API.

Relevant upstream source:

- <https://github.com/ruffle-rs/ruffle/blob/master/desktop/src/backends/external_interface.rs>
- <https://github.com/ruffle-rs/ruffle/blob/master/core/src/avm2/globals/flash/net/socket.rs>

### System-wide Clean Flash

This preserves behavior but keeps the existing external prerequisite, writes
global COM registration, can conflict with other Flash versions, and requires
a separate install/update lifecycle. It does not meet the v1.1.0 goal.

### Private registration-free ActiveX runtime (selected)

Windows side-by-side manifests support registration-free COM activation. By
declaring the Flash CLSID under the app's private `Flash.ocx`, the existing
`AxShockwaveFlash` host and all of its events remain unchanged while no global
Flash registration is needed. The file travels with Grimoire and is serviced
only when Grimoire is updated.

Microsoft references:

- <https://learn.microsoft.com/windows/win32/sbscs/registration-free-com>
- <https://learn.microsoft.com/windows/win32/sbscs/comclass>
- <https://learn.microsoft.com/windows/win32/sbscs/typelib>

Runtime source:

- <https://github.com/darktohka/clean-flash-builds/releases/tag/v1.54>

## Implementation and safeguards

- `app/Grimoire.manifest` maps the Flash CLSID and type library to the private
  `Flash.ocx` next to the application executable.
- `exodus.ps1 runtime` downloads a fixed release URL, verifies the archive,
  extracts only the 32-bit release ActiveX control, and verifies that file too.
- The archive and OCX hashes are pinned independently.
- The native installer includes the already-verified OCX but does not register
  it. Uninstalling Grimoire therefore cannot disturb another Flash consumer.
- `tests/Test-FlashActivation.ps1` instantiates the real
  `AxShockwaveFlashObjects.AxShockwaveFlash` control and fails unless the loaded
  OCX module path is the application-local file.

This is a compatibility solution for trusted AQW content, not a restoration of
general-purpose browser Flash. The legacy runtime must not be used for
untrusted SWFs.
