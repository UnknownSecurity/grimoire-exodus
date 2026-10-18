# Desktop rendering performance research for v1.4.0

## Decision

Do not ship a setting labelled **CPU / GPU / Both**. That wording would imply
that Grimoire can move the AQW client, bot engine, or networking work between
processors. It cannot.

The viable feature is a **Rendering and performance profile** with the current
behavior as the default, experimental Flash presentation modes behind an
explicit opt-in, and separate controls for frame rate, quality, and inactive
clients. Hardware modes must be benchmarked against the bundled 32-bit ActiveX
runtime before they are promoted beyond experimental status.

## Implementation status

The desktop client now exposes the recommended opt-in controls under
**Settings > Performance**:

- Automatic, Direct presentation, and experimental GPU rendering requests;
- Automatic, High, Medium, and Low Flash render quality;
- an optional active-client FPS target; and
- optional inactive-client throttling that never reduces FPS while that
  client's bot is running.

Automatic rendering, automatic quality, the legacy FPS behavior, and disabled
inactive throttling remain the defaults. Direct/GPU values are applied before
the SWF state is loaded, rejected ActiveX property writes fall back to the
control's previous value, and current clients are never recreated implicitly.
The bundled ActiveX runtime accepts the new mode and quality values in the
repository smoke probe. A live AQW benchmark matrix is still required before
the experimental label can be removed.

## What runs where today

The desktop application hosts one `AxShockwaveFlash` ActiveX control for each
client session in the same 32-bit WinForms process. `Root.MultiClient.cs`
creates the control and restores the embedded Grimoire SWF through its
`OcxState`. The SWF then downloads and loads the official AQW client.

The workload is split as follows:

| Work | Processor today | Can Grimoire redirect it? |
| --- | --- | --- |
| ActionScript bytecode, game logic, display-list traversal, timers | CPU | No general switch exists. Rewriting AQW for Stage3D would be required for a fundamental change. |
| Bot engine, proxy, packet handling, JSON/XML bridge, WinForms UI | CPU | It can be profiled and optimized as managed code, but it cannot run on a GPU. |
| Flash scaling, presentation, supported video paths | CPU plus optional GPU assistance selected by Flash and the driver | Partly. The host can request a Flash window mode, but the runtime can reject or fall back from it. |
| Stage3D shaders and buffers | GPU when a hardware context is available, otherwise software fallback | Only content authored to use `Stage3D`/`Context3D` benefits. The checked-in desktop bridge has no such calls. |

The current FPS option is not a renderer switch. `CmdSetFPS`, the toolbar, and
`Root.setFPS()` ultimately assign `Stage.frameRate`. Raising that cap can make
animation smoother when CPU time is available, but it also increases CPU work
for every live client and cannot cure missed frame deadlines.

The bundled interop assemblies expose writable `WMode`, `Quality`, `Quality2`,
and `ScaleMode` properties on `AxShockwaveFlash`. The application currently
does not set a rendering mode or quality explicitly. This makes an isolated
experiment possible, but does not prove that each mode works correctly with
the private Clean Flash runtime and the AQW display list.

## What the platform documentation confirms

- Flash Player enables hardware-accelerated scaling by default when supported.
  Adobe describes it as using the graphics card to scale images and video; it
  also documents software fallback for old or incompatible hardware. This is
  not the same as executing all ActionScript or vector rendering on the GPU.
- The AIR rendering documentation distinguishes `cpu`, `direct`, and `gpu`.
  In particular, `direct` performs composition on the CPU and uses the GPU for
  the final blit. Those AIR descriptor names help explain the pipelines, but an
  AIR descriptor is not used by Grimoire's desktop ActiveX host.
- `Context3D` is the programmable GPU path shared by Flash Player and AIR. It
  uses hardware when possible and software otherwise. The content must request
  and render through a Stage3D context. There are zero `Stage3D` or `Context3D`
  usages in the checked-in desktop ActionScript bridge.
- Windows' DXGI API lets an application that creates its own Direct3D device
  enumerate adapters by GPU preference. Grimoire does not create Flash's
  Direct3D device; the closed ActiveX runtime does. Therefore this API cannot
  select an adapter for an individual embedded client.

Sources:

- [Adobe Flash Player display settings](https://www.macromedia.com/support/documentation/en/flashplayer/help/help01.html)
- [HARMAN AIR `renderMode` documentation](https://airsdk.dev/docs/building/application-descriptor-files/elements/initialWindow#rendermode)
- [ActionScript `Context3D` reference](https://airsdk.dev/reference/actionscript/3.0/flash/display3D/Context3D.html)
- [Microsoft `IDXGIFactory6::EnumAdapterByGpuPreference`](https://learn.microsoft.com/en-us/windows/win32/api/dxgi1_6/nf-dxgi1_6-idxgifactory6-enumadapterbygpupreference)

## Answers to the feature questions

### Is GPU acceleration possible?

Partially. The bundled Flash runtime may already use the GPU for supported
scaling and presentation. The ActiveX host can also request modes such as
`direct` or `gpu` through `WMode`, subject to runtime and driver support. This
must be treated as a rendering experiment, not as a promise that AQW becomes a
GPU-rendered game.

### Can AQW or the bot engine be moved to the GPU?

No. AQW's ActionScript and classic display-list work still execute on the CPU.
The .NET bot engine, proxy, serializers, and UI are also CPU code. GPU compute
cannot be substituted without rewriting those workloads for a different
architecture.

Wrapping the existing display list in a bitmap or texture is not a solution.
Flash would first have to rasterize the game on the CPU, after which the GPU
would only composite the resulting image. That adds copies, memory use, input
mapping risk, and latency.

### What would “Both” mean?

It is already the normal pipeline when Flash uses hardware acceleration: CPU
game logic and composition combined with supported GPU scaling, video, or
presentation. It is not a separate scheduler mode.

### Can it be selected per client?

A requested ActiveX `WMode` can in principle be assigned to each control when
the control is created. It should not be changed live: switching profiles
should recreate only that client after the usual running-bot and logged-in
confirmation.

Selecting a physical GPU is different. Windows and graphics drivers generally
apply that preference to the executable or to devices created by the process.
All current clients share `Grimoire Exodus.exe`, and the Flash OCX creates its
own graphics device, so a reliable per-client GPU-adapter selector is not
available. Achieving that would require a future broker plus one worker process
per client, which is outside a safe v1.4.0 change.

## Recommended user-facing design

Add a **Rendering** section to Settings only after the compatibility spike
passes. Keep all new values out of GBot serialization.

### Renderer profile

| Profile | Behavior | Default |
| --- | --- | --- |
| Automatic | Preserve the current ActiveX state and let Flash/Windows choose supported acceleration. | Yes |
| Direct presentation (experimental) | Request `WMode=direct` before loading the client. Describe it accurately as CPU composition with GPU-assisted presentation where supported. | No |
| GPU rendering (experimental) | Request `WMode=gpu` only if the spike proves it works and improves AQW on supported drivers. It must fall back safely. | No |
| Software compatibility | Offer only if testing finds a supported, deterministic per-control mechanism. Do not mislabel `opaque` or another window mode as software without proof. | No |

Never display “GPU active” merely because a mode was requested. Without a
Stage3D context or runtime diagnostic, Grimoire cannot prove which path the OCX
actually selected.

### Per-client performance controls

- Keep the existing per-client FPS cap and clamp it to a documented safe range.
- Add render quality (`High`, `Medium`, `Low`) as an independent per-client
  choice after verifying `Quality2` against AQW. This is more likely to help a
  vector-heavy display list than pretending all rendering moved to the GPU.
- Add optional inactive-client throttling, default off initially. A conservative
  policy would reduce the FPS only for an obscured client with no running bot,
  then restore its exact prior FPS before it becomes active.
- Keep Lag Killer, Hide Players, and Disable Animations independent. A profile
  may suggest them, but it should not silently mutate bot-visible settings.
- Add an “Open Windows graphics settings” help action rather than changing OS or
  driver preferences automatically. Any Windows preference applies to the whole
  executable, not one tab.

### Diagnostics

Add a read-only performance overlay before adding automatic tuning:

- requested FPS and measured FPS per client;
- p50 and p95 frame interval over a short rolling window;
- whole-process CPU and private memory;
- client count and active/inactive state;
- requested renderer profile and quality, explicitly labelled “requested”;
- bridge-call latency and queue/backlog counters without logging packet bodies,
  account data, room names, or bot details.

Actual FPS should be measured in ActionScript with a low-overhead rolling
`ENTER_FRAME` sample and reported through a new additive bridge callback. Do
not infer actual FPS from `Stage.frameRate`, because that is only a target.

## The likely desktop bottlenecks

GPU selection alone does not address the highest-risk constraints:

1. Every Flash instance, the WinForms surface, and synchronous ActiveX bridge
   share one UI process. A busy client can delay tab switching and callbacks.
2. All workspace panels remain live and are switched with z-order. Obscured
   Flash controls may continue advancing and rendering. Hiding controls could
   save work, but must be tested because it can change Flash background timing.
3. Raising all clients to 60 FPS multiplies display-list work. Six clients at a
   60 FPS target are not equivalent to one client at 60 FPS.
4. `CallFunction` and `FlashCall` serialize XML synchronously. The bridge parses
   XML and allocates parameter arrays on repeated calls. This is a managed-code
   optimization target, but it is separate from frame rendering.
5. The required x86 process has a constrained address space. Hardware modes can
   also add driver and video-memory pressure; an unlimited client count remains
   inherently unsafe.

## Focused .NET hot-path scan

Scope: `Flash.cs`, `ActiveXFlashBridge.cs`, `ClientSession.cs`,
`Root.MultiClient.cs`, `Root.cs`, `ClientOptionState.cs`, and `CmdSetFPS.cs`.
This was a static standard-depth scan, not a profiler trace.

### Scan execution checklist

- `.IndexOf("...")` without comparison: 0
- `.Substring(...)`: 0
- literal `.StartsWith`/`.EndsWith` without comparison: 1
- literal `.Contains(...)` candidates: 0
- `async void`: 8; manual review found UI event handlers
- `.Result`/`.Wait()` candidates: 0
- parameterless `.ToLower()`/`.ToUpper()`: 1
- three-or-more `.Replace()` chain: 0
- `params` signatures: 17
- LINQ `.All`/`.Any` on characters: 0
- `static readonly Dictionary`: 0; `FrozenDictionary`: 0
- `new List`: 5; `new Dictionary`: 1; most are fields or cold UI paths
- `StringComparer.CurrentCulture`: 0
- `.Select`/`.Where`/`.Cast`/`.Take`/`.Aggregate`: 5
- `new HttpClient`: 0
- uncached `new JsonSerializerOptions`: 0
- class declarations: 6 sealed and 4 unsealed declarations; the unsealed count
  includes two partial declarations of the inherited `Root` form

### Findings

#### Moderate if profiling confirms bridge pressure: repeated bridge allocations

The synchronous bridge contains `params` arrays, `Concat(...).ToArray()`,
`XElement.Parse`, and LINQ-to-array conversion on callback paths. These costs
scale with bridge traffic and client count. Measure allocation rate and bridge
latency first; then add fixed-arity overloads and replace only proven hot LINQ
paths with allocation-conscious loops. Do not change the XML ABI.

#### Informational: two small string patterns

Boolean XML serialization uses `ToString().ToLower()` and server-list detection
uses culture-sensitive `StartsWith`/`EndsWith`. Both can be made ordinal and
allocation-light in a later focused cleanup, but neither is a credible FPS
bottleneck.

#### Resolved during implementation: FPS command had no asynchronous work

`CmdSetFPS.Execute` now returns a completed `Task` after its synchronous bridge
call without changing the command contract. It also retains the explicit FPS
target in session-only state so tab switching and inactive throttling restore
the GBot's exact value. No `.gbot` field or serialized type changed.

| Severity | Count | Top issue |
| --- | ---: | --- |
| Critical | 0 | None found in the focused scan |
| Moderate | 1 | Possible bridge allocation pressure under multi-client traffic |
| Informational | 1 open, 1 resolved | Small string cleanup; no-op async state machine resolved |

> Static performance findings require profiler and benchmark confirmation. They
> can contain false positives and do not substitute for measurements on the
> bundled runtime and representative hardware.

## Compatibility spike and benchmark matrix

Build a disposable diagnostic branch before exposing settings. The spike must
not alter the default path.

Test these requested modes separately: unchanged/default, `direct`, and `gpu`.
Cross them with `High`/`Medium`/`Low` quality only after each renderer mode is
stable. Test 1, 2, 4, and 6 clients in these scenes:

- login screen and first load;
- idle private room;
- populated public room;
- combat with animations and particles;
- one bot running while switching clients;
- minimize-to-tray, restore, resize, and rapid tab switching;
- graphics context loss or driver fallback where it can be reproduced safely.

Cover integrated-only graphics, hybrid integrated/discrete graphics, and a
known compatibility/older-driver machine. Record:

- actual FPS, p50/p95 frame interval, and dropped/late frames;
- process CPU, private bytes, working set, USER/GDI handles;
- GPU engine utilization and dedicated/shared memory using external diagnostic
  tooling during development, not privacy-sensitive production telemetry;
- load success, black-frame rate, resize correctness, input coordinate accuracy,
  focus, audio, and tab restore behavior;
- bridge latency, network continuity, bot command ordering, and cancellation.

An experimental profile is eligible to ship only if it has no black-screen,
input, focus, bridge, or state-isolation regression and either reduces CPU by
at least 15% at equivalent measured FPS or improves p95 frame time by at least
10% in a repeatable scene. Otherwise retain Automatic only.

## Proposed implementation order

1. Add measured-FPS and frame-time diagnostics without changing rendering.
2. Create a developer-only ActiveX mode/quality probe applied before SWF load.
3. Run the matrix and publish anonymized aggregate results only.
4. If a mode qualifies, add an additive `ClientRenderingProfile` to session
   state and global defaults. Do not serialize it into legacy GBots.
5. Require client recreation to apply a renderer change and use the existing
   bot/login close protections.
6. Add optional quality and inactive-client policies one at a time, each with
   its own compatibility test and rollback to the previous value.
7. Keep Automatic as the default and provide a one-click reset when the client
   fails to render.

## Required validation for an implementation

- Debug and Release builds through `exodus.ps1`.
- Legacy GBot compatibility against the repository-controlled `app/Bot` data.
- Multi-client isolation and client-limit smoke tests.
- Flash activation smoke test using the private app-local OCX.
- Manual 960x620 light/dark UI checks.
- Manual matrix results for black frames, input accuracy, focus, resize, and
  recovery on representative hardware.

Rendering settings alter only local presentation and do not intentionally
change packets or game state, so no additional ban risk is expected. That is
not a guarantee: the feature must avoid timing changes that could alter bot
behavior, and no mode should silently change an existing GBot's commands or
delays.
