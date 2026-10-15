# Grimoire Exodus v1.1.0 code and performance audit

**Audit date:** 2026-10-14

**Reviewed revision:** `ccbf3c5056fd7bf39a7aafd4d7c2190dc9a606c7` (`main`)

**Scope:** architecture, runtime performance, reliability, security, packaging, dependencies, maintainability, tests, and legacy GBot compatibility

**Original change policy:** research and recommendations only. The audit was subsequently approved for implementation; see the implementation addendum and current outcome below.

## Executive summary

The solution builds successfully and already contains useful safeguards: registration-free local Flash activation, startup/crash diagnostics, an updater with SHA-256 verification and rollback, a restricted GBot serialization binder, DPAPI-protected account passwords, and smoke tests for the most compatibility-sensitive features.

The largest immediate risk is the bot lifetime model. A quick stop/start can leave an old task running after a new cancellation source and configuration have been assigned. That can create two execution loops, duplicate commands, extra Flash and network traffic, and difficult-to-reproduce behavior. The packet spammer has the same shared-token pattern.

The likely dominant performance cost is the synchronous boundary between C# and the in-process Flash ActiveX control. Each call builds/parses XML and many calls additionally deserialize JSON. Game-state properties cross this boundary repeatedly, while several 100–250 ms polling loops run concurrently. Before changing bot timing, calls should be instrumented and then consolidated into per-tick snapshots, packet-fed state, or batched bridge requests.

Long-running sessions are also exposed to unbounded log and packet text controls. Every append grows a Win32 text buffer and some producers synchronously invoke the UI thread. Memory and append cost therefore increase with session length, and packet bursts can stall the UI.

Release quality checks are currently weaker than the code needs. A diagnostic Release rebuild at warning level 4 succeeded but exposed **104 compiler warnings** that the normal `Release|AnyCPU` configuration hides with `WarningLevel=0`. These include 28 unobserved calls and 46 methods marked async without awaiting work. The release workflow also publishes without running the existing smoke tests, signing binaries, or generating checksums/SBOM/provenance.

Recommended order: first establish measurements and lifecycle tests; then repair task ownership and bound logging; next optimize the Flash/packet hot paths; then decompose eager UI startup; finally harden plugins and the release supply chain. Preserve .NET Framework 4.8/x86, private Flash COM activation, serialized type identity, and legacy command/timing behavior throughout.

## Scope and methodology

The review covered the 383 tracked files, including roughly 300 C#, ActionScript, HTML, and WiX source files. The largest concentration of code is the approximately 8.8k-line [BotManager.cs](../app/Grimoire.UI/BotManager.cs), followed by [Root.cs](../app/Grimoire.UI/Root.cs), [Travel.cs](../app/Grimoire.UI/Travel.cs), and the Home HTML.

Methods used:

- Read the startup, bot engine, Flash bridge, proxy/handlers, game state, secondary tools, plugin manager, account vault, updater, installer, release workflow, and existing tests.
- Searched static hot-path indicators: 226 Flash bridge references, 63 `async void` methods, recurring polling loops, fire-and-forget tasks, broad/empty catches, mutable global collections, synchronous UI marshaling, and recursive filesystem scans.
- Built Release successfully and separately forced compiler warning level 4 to expose warnings hidden by the project configuration.
- Inspected the generated installer staging payload: 20,923,588 bytes uncompressed, including an 11,891,688-byte `Flash.ocx`; unused-looking EasyHook/EasyLoad files account for 610,816 bytes.
- Checked pinned NuGet versions against official NuGet registration metadata on the audit date.
- Performed static analysis and build/smoke inspection, not an instrumented production AQW session. Runtime impact statements marked “likely” must be validated with the proposed measurements below.

## Architecture map

| Area | Main responsibility | Important boundary |
|---|---|---|
| `Grimoire.UI` | Root window, Home, Bot Manager, tools | WinForms UI thread and eager form construction |
| `Grimoire.Botting` | GBot configuration and execution | Long-running async lifecycle and legacy serialization |
| `Grimoire.Tools.Flash` | C# ↔ ActionScript bridge | Synchronous COM `CallFunction`, XML, and JSON |
| `Grimoire.Networking` | Local proxy and packet dispatch | Concurrent sockets, handler dispatch, UI packet logging |
| `Grimoire.Game*` | Player/world/inventory state | Mix of Flash queries and packet-populated mutable globals |
| `Grimoire.Tools.Plugins` | Third-party plugin loading | Full-trust assemblies in the default AppDomain |
| `Grimoire.Utils.UpdateService` | GitHub release discovery/preparation | Network async; hashing/extraction/copy currently synchronous |
| `Grimoire.Updater` | Out-of-process merge/rollback | Filesystem replacement and restart |
| WiX/workflow | MSI and portable release | Trust, reproducibility, test gating, and artifact integrity |

## Prioritized findings

| ID | Priority | Finding | Main impact |
|---|---|---|---|
| R1 | **P0** | Bot and spammer runs do not own immutable, awaited lifetimes | Duplicate execution, packet bursts, races |
| P1 | **P1** | Synchronous Flash boundary is called per property and per loop | UI/worker stalls, XML/JSON allocation, CPU |
| P2 | **P1** | Multiple continuous 100–250 ms pollers duplicate work | Persistent idle/bot load and COM traffic |
| P3 | **P1** | Logs and packet windows grow without bounds | Long-session memory growth and UI freezes |
| N1 | **P1** | Packet dispatch scans/allocates per packet; async handlers are `void` | Burst overhead and escaped exceptions |
| U1 | **P1** | Startup constructs and loads heavy components synchronously | Slow first paint and fragile startup |
| Q1 | **P1** | Release warnings are disabled | Real correctness defects remain hidden |
| U2 | **P1** | Updater prepares files on the captured UI context | Frozen UI during hash/extract/copy |
| S1 | **P1** | Plugins are auto-loaded in-process at full trust | Arbitrary-code and lifecycle risk |
| C1 | **P2** | Quick-bot menu recursively rescans on every open | Delayed/frozen dropdown on large/network folders |
| C2 | **P2** | Configuration rewrites whole files per value | I/O churn, torn writes, multi-instance races |
| G1 | **P2** | Aura countdown model is dead/leaky and duration math is wrong | Incorrect conditions and retained objects |
| M1 | **P2** | Large forms, global mutable state, and dead code hinder change | Regression risk and slow development |
| D1 | **P2** | Dependencies/payload contain removal candidates | Larger package and maintenance surface |
| S2 | **P2** | Release artifacts are unsigned and workflow is weakly gated | SmartScreen friction and supply-chain risk |

## Detailed performance and reliability findings

### R1 — Bot run ownership and shutdown are unsafe

Evidence:

- [`OnExecute`](../app/Grimoire.Botting/Bot.cs#L154) and [`OnCancel`](../app/Grimoire.Botting/Bot.cs#L205) are `async void`, so callers cannot observe completion or failure.
- Each start overwrites `_ctsBot`, then launches quest and execution tasks at [Bot.cs:194](../app/Grimoire.Botting/Bot.cs#L194) and [Bot.cs:197](../app/Grimoire.Botting/Bot.cs#L197).
- Stop cancels the current source but does not await `_botTask` or `_questTask` before allowing another start.
- `Activate` and `CheckQuests` repeatedly read mutable instance fields such as `_ctsBot`, `Configuration`, and `IsRunning`, instead of capturing a run-specific token/configuration.
- Event handlers are subscribed per start. A previous run can overlap with new subscriptions/state during rapid stop/start.
- [Spammer.cs:27](../app/Grimoire.Tools/Spammer.cs#L27) similarly replaces its shared cancellation source and fire-and-forgets `Task.Run(Spam)`; the old loop can observe the new source.

Failure scenario: run A is cancelled; run B immediately replaces `_ctsBot`; run A reaches its next condition and sees run B’s non-cancelled source and mutable configuration. Both loops can then execute commands.

Recommendation:

1. Introduce one run-session object containing an immutable configuration snapshot, cancellation source, run ID, execution task, and auxiliary tasks.
2. Make start/stop/pause return `Task`; serialize transitions and await full shutdown before starting another session.
3. Capture the token/session in every loop; never read the replaceable global token to decide whether an older loop should exit.
4. Dispose old cancellation sources and make event subscription/unsubscription idempotent.
5. Add a compatibility test that performs 100 rapid start/stop/start cycles and proves exactly one command loop, quest loop, drop loop, and settings loop remain.

Do not change command ordering or default delays as part of the lifecycle repair.

### P1 — Flash/ExternalInterface is the likely dominant hot path

[Flash.cs:107](../app/Grimoire.Tools/Flash.cs#L107) constructs an XML request, synchronously calls the ActiveX control, parses XML, and converts the response. Generic getters at [Flash.cs:79](../app/Grimoire.Tools/Flash.cs#L79) additionally deserialize JSON. Exceptions are frequently converted into default values, making a failed bridge call indistinguishable from valid `0`, `false`, or `null` state.

This cost is multiplied by property-style access. Health, mana, map, cell, pad, target state, inventory, factions, monsters, and players are often fetched independently. For example, [CmdLog.cs:27](../app/Grimoire.Botting.Commands.Misc/CmdLog.cs#L27) evaluates many properties and creates nine regular expressions for one log command; item/faction placeholders can repeatedly materialize the same lists.

Recommendation:

- Instrument every bridge call first: function/path, duration, payload bytes, success/failure, thread, and calls per second. Aggregate rather than logging every call.
- Create a read-only per-tick player/world snapshot for logically related values.
- Prefer packet-driven state updates where packet ordering makes the state reliable.
- Batch related getters into one ActionScript bridge call where compatibility permits.
- Cache immutable/slow-changing data such as faction metadata; invalidate it from known packets/events.
- Precompile static `CmdLog` regexes and read inventory/factions once per command.
- Surface bridge-health counters separately from normal game values.

Acceptance target should be based on a baseline, then reduce bridge calls per bot command and p95 call duration without changing observed legacy GBot behavior.

### P2 — Continuous polling causes avoidable background load

Current loops include:

- Proxy client work every 100 ms at [Proxy.cs:252](../app/Grimoire.Networking/Proxy.cs#L252).
- Drop rejection every 100 ms at [DropStack.cs:131](../app/Grimoire.Game/DropStack.cs#L131), rebuilding and sending the same whitelist even when unchanged.
- Bot settings every configurable interval, 250 ms by default, at [OptionsManager.cs:378](../app/Grimoire.Botting/OptionsManager.cs#L378).
- Root metrics every second, including enumeration of every active network adapter at [Root.cs:671](../app/Grimoire.UI/Root.cs#L671). These bytes are system-wide, not Grimoire-only.

Recommendation:

- Replace polling with packet/state events where reliable.
- Use dirty flags and only reapply settings when a value changes or a map/login event invalidates state.
- Send a drop whitelist once per configuration change/login/map reset rather than ten times per second.
- Pass cancellation tokens into all delays and use adaptive/backoff intervals while idle.
- Cache network adapters or label the metric explicitly as system-wide. If the intended metric is Grimoire traffic, count proxy bytes directly.

### P3 — Logging and packet display are unbounded

[LogForm.cs:130](../app/Grimoire.UI/LogForm.cs#L130) appends each message directly to text controls. Cross-thread calls use synchronous `Invoke`. Packet paths append directly at [Proxy.cs:313](../app/Grimoire.Networking/Proxy.cs#L313). No retention cap was found.

As a session grows, both managed strings and the native edit control become more expensive. Synchronous UI marshaling also blocks packet/bot producers behind rendering.

Recommendation:

- Put each log source behind a bounded channel/ring buffer.
- Drain batches on a UI timer with `BeginInvoke` rather than invoking once per record.
- Keep a configurable maximum number of characters/lines and preserve the most recent records.
- Offer optional rolling files with size/count limits for diagnostics.
- Pause packet rendering when its window is hidden; retain only the bounded queue.

Validation: an eight-hour scripted session and a 1,000 packets/sec burst must show bounded private working set and a responsive UI.

### N1 — Packet dispatch scans and allocates for every packet

[Proxy.cs:141](../app/Grimoire.Networking/Proxy.cs#L141) filters each handler list with LINQ and calls `ToArray()` per XT/JSON/XML packet. Each handler’s `HandledCommands.Contains` is also scanned. Registration changes appear far less frequent than dispatch, so this work belongs at registration time.

The handler interfaces return `void` ([IJsonMessageHandler.cs](../app/Grimoire.Networking/IJsonMessageHandler.cs), [IXtMessageHandler.cs](../app/Grimoire.Networking/IXtMessageHandler.cs)). Several implementations use `async void`; exceptions after the first await bypass the surrounding dispatch `try/catch`.

Recommendation:

- Precompute command → immutable handler-array dictionaries and atomically replace a snapshot when registration changes.
- Change contracts to `Task HandleAsync(...)`, await/observe them under a defined ordering policy, and record handler-specific failures.
- Add backpressure or bounded queues if handlers cannot keep up with the socket.
- Benchmark 100, 500, and 1,000 packets/sec with packet logging both on and off.

### U1 — Startup is synchronous and eagerly constructs heavy UI

[Root.cs:842](../app/Grimoire.UI/Root.cs#L842) loads hotkeys, scans and executes every plugin DLL, initializes Home, constructs/activates Flash, and registers proxy handlers on the UI thread. The Root constructor also calls `triggerToggle`, which touches the Bot Manager and Travel singletons. Bot Manager’s single source file contains hundreds of control constructions and 137 explicit click-handler registrations.

`PreloadedPlugins = new PluginManager()` at [Root.cs:204](../app/Grimoire.UI/Root.cs#L204) constructs a form as a service; [PluginManager.cs:30](../app/Grimoire.UI/PluginManager.cs#L30) defines another singleton manager.

Recommendation:

- Establish “process start → responsive Home” and “Home → Flash ready” timings before refactoring.
- Show the lightweight Home shell first, then initialize Flash after first paint with visible state/progress.
- Lazy-create Bot Manager, Travel, logs, and secondary forms when first opened.
- Split plugin discovery/metadata from form construction; scan metadata off the UI thread and execute only enabled plugins.
- Separate designer/feature panels/services from `BotManager.cs` and `Root.cs` without renaming serialized command types.

### U2 — Update preparation can block the UI

Network download is asynchronous, but after its awaits, [PrepareUpdateAsync](../app/Grimoire.Utils/UpdateService.cs#L106) computes hashes, deletes directories, extracts ZIP entries, validates payloads, and copies the updater synchronously. It is called from the UI flow at [Root.cs:966](../app/Grimoire.UI/Root.cs#L966), so continuations can freeze the window on slower disks or antivirus scans.

The release selector skips drafts but not prereleases ([UpdateService.cs:50](../app/Grimoire.Utils/UpdateService.cs#L50)); it can offer a numerically higher prerelease to stable users.

Recommendation:

- Run hash/extract/validation/copy on a dedicated worker with cancellation and stage progress.
- Retain the good out-of-process apply/backup/rollback design.
- Define stable and prerelease channels; stable must skip prereleases unless the user opts in.
- Test locked files, disk-full, corrupt archive, cancellation, downgrade/equal version, and interrupted restart.

### C1 — Quick-bot discovery blocks the dropdown

Every dropdown opening calls `RefreshQuickBots` at [Root.cs:368](../app/Grimoire.UI/Root.cs#L368), clears/recreates menu items, and recursively scans both bundled and Documents bot folders using `Directory.GetFiles(..., AllDirectories)` at [Root.cs:415](../app/Grimoire.UI/Root.cs#L415). Documents can be redirected to OneDrive or a network location.

Recommendation: build a cached index in the background, invalidate it with `FileSystemWatcher` plus a manual refresh fallback, deduplicate normalized paths, and update the menu from an immutable result. Test 100, 1,000, and 10,000 files plus an unavailable/slow redirected Documents directory.

### Correctness defects that also generate load

- [BankForm.cs:99](../app/Grimoire.UI/BankForm.cs#L99) and [BankForm.cs:122](../app/Grimoire.UI/BankForm.cs#L122) call `Task.Delay(80)` without awaiting it. Bulk bank transfers therefore burst immediately, increasing throttling/rejection risk.
- [Hotkeys.cs:354](../app/Grimoire.UI/Hotkeys.cs#L354) passes a tuple `(input, StringComparison.OrdinalIgnoreCase)` to `Items.IndexOf` rather than searching for the server string; it likely always returns `-1`.
- [DPSForm.cs:35](../app/Grimoire.UI/DPSForm.cs#L35) concatenates damage values and parses the result rather than summing them. No producer that populates its dictionary was found.
- Many delays omit a cancellation token, so stop/shutdown latency can extend until the delay expires.

## Compiler quality gate

The normal `Release|AnyCPU` block explicitly sets `WarningLevel=0` at [Grimoire.csproj:50](../app/Grimoire.csproj#L50). A forced warning-level-4 Release build completed with 104 warnings:

| Warning | Count | Meaning in this codebase |
|---|---:|---|
| CS1998 | 46 | Async method has no await |
| CS4014 | 28 | Task/call is not awaited |
| CS0162 | 8 | Unreachable code |
| CS0169 | 7 | Unused private field |
| CS0649 | 7 | Field never assigned |
| CS0108 | 4 | Member hides inherited member |
| CS0168 | 2 | Declared exception never used |
| CS0067 | 1 | Event never used |
| CS0252 | 1 | Possible unintended reference comparison |

Recommendation: check in a warning baseline, restore warning level 4, fail CI only on newly introduced warnings initially, and burn down the baseline by category. Address CS4014/lifecycle warnings before cosmetic unused-member warnings. Do not mass-change timing-sensitive async code without behavior tests.

## State, configuration, and maintainability

### Global mutable state

The architecture relies heavily on static singleton forms and mutable static collections for world/player/bot state. Packet threads, bot tasks, and the UI can observe or change the same lists. This makes ownership unclear and creates enumeration/modification races.

Recommendation: move toward immutable snapshots or explicitly locked state stores, with packet handlers as the writers and UI/bot logic as readers. Make the migration one domain at a time; preserve public command types and facades for legacy bots/plugins.

### Configuration writes

[Config.cs:35](../app/Grimoire.Botting/Config.cs#L35) and [BotClientConfig.cs:35](../app/Grimoire.Botting/BotClientConfig.cs#L35) sort and rewrite the whole file synchronously. Frequent individual `SetValue` calls multiply I/O, and direct replacement is vulnerable to process interruption or two instances writing concurrently.

Recommendation: debounce/batch changes, write a temporary file in the same directory, flush, and atomically replace the destination. Add a cross-process mutex if multiple instances are supported. Preserve existing keys and migration behavior.

### Aura model

`World.auraCountdown` is declared at [World.cs:160](../app/Grimoire.Game/World.cs#L160), but no invocation was found. [Aura.countdown](../app/Grimoire.Game.Data/Aura.cs#L45) therefore appears dead while its event subscription can retain aura objects. `Duration` uses `TimeSpan.Seconds`, which is only the seconds component, not total elapsed seconds. Aura packet updates repeatedly search and materialize lists at [HandlerPlayer.cs:216](../app/Grimoire.Networking.Handlers/HandlerPlayer.cs#L216).

Recommendation: replace the event-per-aura scheme with keyed aura records and packet-driven removal or one scheduler; use total elapsed time and update the located aura once.

### Oversized/dead code

- Split the approximately 8.8k-line Bot Manager into designer/partial files, feature panels, and non-UI services.
- Split Root startup, quick-bot indexing, metrics, updater UI, tray behavior, and Flash hosting into focused components.
- `HandlerJSON.cs` is mostly commented-out obsolete handler code; remove it from active source history after verifying no reference is required.
- `BotManager.richTextBox2_TextChanged` returns before old Word conversion code, leaving unreachable code and an Office dependency candidate.

## Security and trust review

### Positive controls to preserve

- Account locker passwords are protected with Windows DPAPI `CurrentUser` at [AccountVault.cs:166](../app/Grimoire.Tools/AccountVault.cs#L166), and vault access is synchronized.
- Polymorphic GBot JSON uses an allow-list binder at [BotJson.cs:31](../app/Grimoire.Botting/BotJson.cs#L31), rather than unrestricted `TypeNameHandling`.
- Portable updates verify SHA-256, validate required files, prevent ZIP path traversal, and apply via a separate helper with backup/rollback.
- Local registration-free COM keeps the Flash dependency app-scoped rather than registering it machine-wide.

### S1 — Plugins execute at full trust and cannot truly unload

Startup loads every DLL in the Plugins directory. [GrimoirePlugin.cs:62](../app/Grimoire.Tools.Plugins/GrimoirePlugin.cs#L62) uses `Assembly.LoadFile`, then runs plugin code in the main process. “Unload” calls a plugin callback and removes a list entry, but an assembly loaded into the default .NET Framework AppDomain remains loaded. Re-selecting a DLL can create duplicate instances/subscriptions.

Recommendation:

- Do not execute a newly dropped DLL automatically. Maintain an explicit enabled-plugin manifest.
- Show source path, hash, publisher/signature, declared compatibility, and a clear trust warning.
- Prevent duplicate path/assembly/plugin IDs.
- If true unload/isolation is required, use a dedicated AppDomain with a narrow marshal-by-reference API or a separate process. A process boundary is stronger.
- Provide plugin API/version negotiation before internal refactors.

### Additional security hardening

- [BotManager.cs:8488](../app/Grimoire.UI/BotManager.cs#L8488) passes bot-supplied link text to `Process.Start`. Accept only absolute HTTP/HTTPS URLs and show the destination before opening.
- Add GBot maximum file size, JSON depth, collection count, and string-length limits even though its type binder is restricted.
- Flash Player is end-of-life and runs in-process. Treat this build as trusted AQW-only software, keep it isolated from arbitrary SWFs/content, and document the residual risk.
- Broad/empty catches—especially Flash getters returning defaults—reduce detection. Record rate-limited structured errors with operation context.

## Dependencies and release payload

Official NuGet metadata checked on 2026-10-14 showed these newer stable versions:

| Package | Pinned | Latest stable checked |
|---|---:|---:|
| Newtonsoft.Json | 13.0.1 | 13.0.4 |
| HtmlAgilityPack | 1.11.42 | 1.13.1 |
| System.Memory | 4.5.4 | 4.6.3 |
| System.Buffers | 4.5.1 | 4.6.1 |
| System.Numerics.Vectors | 4.5.0 | 4.6.1 |
| System.Runtime.CompilerServices.Unsafe | 6.0.0 | 6.1.2 |
| System.Threading.Tasks.Extensions | 4.5.4 | 4.6.3 |

The sampled official metadata did not flag those pinned versions as deprecated or vulnerable. This is an update-maintenance finding, not a claim of a known vulnerability. Upgrade one dependency group at a time and run legacy/Flash/updater smoke tests.

Removal candidates, subject to runtime tracing and clean-machine packaging tests:

- `Portable.BouncyCastle` appears only in `packages.config`.
- `BrotliSharpLib` has no source reference found.
- `Microsoft.Office.Interop.Word` is reachable only from code following an unconditional return.
- EasyHook/EasyLoad binaries (610,816 staged bytes) have no C# source reference found.

Flash itself accounts for about 57% of the 20.9 MB uncompressed stage and is not removable while the current architecture is retained.

## Installer and release workflow

Findings:

- Binaries and MSI are not Authenticode-signed, contributing to SmartScreen/publisher warnings.
- [tagged-build.yml:5](../.github/workflows/tagged-build.yml#L5) runs for every tag (`"*"`), not only release-version tags.
- The workflow builds and publishes but does not run the existing smoke tests first.
- Actions use floating major tags rather than reviewed immutable commit SHAs.
- No published checksum file, SBOM, build provenance, or attestation is produced.
- Current action versions emitted Node runtime deprecation warnings in the inspected GitHub run; update to supported action revisions.
- [Product.wxs:15](../installer/Product.wxs#L15) declares `perMachine`, while shortcut key paths use HKCU at [Product.wxs:53](../installer/Product.wxs#L53) and [Product.wxs:72](../installer/Product.wxs#L72). Review component scope and test standard/admin, single-user/multi-user, upgrade, repair, uninstall, desktop shortcut, and restart behavior. Strict local ICE validation previously reported 216/217-class issues.

Recommendation:

1. Restrict release triggers to `v*` plus an explicit semantic-version validation step.
2. Build once, run all smoke tests against the staged bits, and publish only on success.
3. Sign EXE/DLL/MSI with an Authenticode certificate and timestamp signatures.
4. Pin third-party actions by commit SHA and use least-privilege permissions per job.
5. Publish SHA-256 checksums, an SBOM, and GitHub artifact provenance/attestations.
6. Test update-from-previous-release and MSI major upgrade on a clean Windows VM.

## Tests and observability gaps

Existing targeted smoke tests are valuable and should remain:

- private Flash activation
- legacy GBot deserialization
- startup/crash recovery
- updater selection
- updater merge and rollback

Missing coverage:

- no automated bot lifecycle/duplicate-loop test
- no packet-handler ordering/concurrency test
- no long-session memory/log retention test
- no performance baseline or regression threshold
- no quick-bot large-directory/redirected-folder test
- no stable-versus-prerelease update-channel test
- no plugin duplication/unload/trust test
- no clean-machine MSI install/upgrade/uninstall matrix in CI
- no structured counters for Flash calls, packet queues, handler latency, dropped logs, or active bot run ID

Observability should be lightweight and opt-in for detailed traces. Default telemetry should remain local unless a separate consented design is approved.

## Proposed performance test matrix

| Scenario | Measurements / pass condition |
|---|---|
| Cold start | process start → responsive Home p50/p95; Home → Flash ready p50/p95 |
| Idle, 5 and 30 min | CPU, private working set, UI-thread stalls, Flash calls/sec |
| Bot lifecycle ×100 | exactly one active run/quest/drop/settings loop after every restart |
| Representative legacy GBots | command order, timing tolerance, packets, inventory/quest outcomes unchanged |
| Eight-hour run | bounded logs/memory; stop and window interactions remain responsive |
| Packet burst | 100/500/1,000 packets/sec; allocations, p95 dispatch latency, queue depth |
| Flash bridge | calls/sec, p50/p95/p99 duration and bytes by function/path |
| Quick-bot menu | open time with 100/1k/10k files and slow/unavailable Documents path |
| Updater | responsive UI during download/hash/extract; cancel/corrupt/locked/disk-full paths |
| Plugins | startup time and memory with 0/10/50 plugins; duplicate load prevented |
| Installer | clean install, desktop/start shortcut, launch, repair, upgrade, uninstall, non-admin |

Record the machine, Windows version, storage type, antivirus state, AQW state, plugin count, bot used, and Release commit for every comparison. Optimize only after repeatable baselines exist.

## Phased roadmap

### Phase 0 — Baseline and guardrails

- Add Flash, packet, task/run, UI-stall, and bounded-memory counters.
- Check in performance scenarios and legacy behavior fixtures.
- Restore compiler warnings with a baseline/new-warning gate.
- Establish cold-start, idle, bot-loop, packet-burst, and long-session baselines.

### Phase 1 — Correctness and bounded resources

- Give bot/spammer/update/background operations explicit awaited ownership.
- Fix unawaited banking delay and cancellation-aware waits.
- Bound and batch logs/packet views.
- Add atomic/debounced configuration writes.

### Phase 2 — Hot paths

- Cache/batch Flash reads based on measurements.
- Move reliable state to packet-driven snapshots.
- Index packet handlers by command.
- Replace unchanged 100 ms setting/drop traffic with events/dirty flags.

### Phase 3 — Startup and UI decomposition

- Lazy-create heavy forms and initialize Flash after first paint.
- Separate service state from WinForms singletons.
- Cache quick-bot discovery.
- Split Bot Manager and Root into focused components while preserving type identity.

### Phase 4 — Trust and delivery

- Introduce explicit plugin enable/trust/isolation controls.
- Sign binaries/MSI and add checksums, SBOM, provenance, and clean-VM release gates.
- Remove dependencies/payload only after runtime trace and compatibility validation.

## Legacy compatibility guardrails

Future work must retain:

- **.NET Framework 4.8 and 32-bit execution** while the 32-bit Flash ActiveX control remains.
- Registration-free, private `Flash.ocx` COM activation and existing deployment manifest behavior.
- Assembly identity resolution, namespaces, command class names, and serialized property shapes used by older `.gbot` files.
- `IBotCommand` behavior and observable command ordering.
- Existing config/storage migrations and the Documents-based bot/account paths.
- Timing-sensitive game behavior unless a change is guarded by legacy fixtures and opt-in compatibility settings.
- Updater rollback and preservation of user bots/configuration.

A Ruffle or custom native game host should be treated as a separate long-term project, not an optimization patch. It requires replacing the synchronous ExternalInterface contract and raw SmartFox bridge and validating every bot-facing API.

## External references

- [Adobe Flash Player end of life](https://www.adobe.com/products/flashplayer/end-of-life.html)
- [Microsoft: registration-free COM](https://learn.microsoft.com/windows/win32/sbscs/registration-free-com)
- [Microsoft: async return types and `async void`](https://learn.microsoft.com/dotnet/csharp/asynchronous-programming/async-return-types)
- [Microsoft: thread-safe WinForms control access](https://learn.microsoft.com/dotnet/desktop/winforms/controls/how-to-make-thread-safe-calls)
- [Microsoft: .NET Framework support policy](https://dotnet.microsoft.com/platform/support/policy/dotnet-framework)
- [GitHub Actions security hardening](https://docs.github.com/actions/security-for-github-actions/security-guides/security-hardening-for-github-actions)
- [Microsoft SignTool](https://learn.microsoft.com/windows/win32/seccrypto/signtool)
- [NuGet package search and metadata](https://www.nuget.org/packages)

## Audit outcome

The original audit pass changed no application behavior. The approved implementation pass on 2026-10-15 subsequently addressed the compatibility-safe findings below.

## Implementation addendum — 2026-10-15

Implemented without changing the public `IBotEngine`/`IBotCommand` contracts or serialized command identities:

- Run-owned bot, quest, drop, settings, and spammer cancellation/task lifetimes; serialized lifecycle transitions; idempotent event subscriptions; and UI-thread scheduling for lifecycle work invoked by legacy commands.
- Bounded, batched, asynchronous UI buffers for main, chat, drop, and packet logs.
- Pre-indexed packet-handler dispatch and observed asynchronous handler work while retaining the existing synchronous handler interfaces for plugin compatibility.
- Cancellation-aware/adaptive polling, cached network-adapter discovery, and a truthful `SYSTEM NET` metric label.
- Background cached GBot indexing for bundled and `Documents\Grimoire\Bot` files, with watcher invalidation and normalized-path deduplication.
- Atomic same-directory configuration replacement and in-process synchronization.
- Correct aura expiry/refresh/removal behavior and removal of repeated aura-list scans.
- Worker-thread update hashing, extraction, validation, cleanup, and copying; stable/prerelease channel selection tests remain explicit.
- Warning level 4 in Release, targeted correction of high-risk unobserved work, the banking delay and immediate-login lookup defects, GBot size/depth limits, HTTP(S)-only bot links, and duplicate plugin-path prevention.
- Removal of unused Brotli, Bouncy Castle, Office Interop, and EasyHook/EasyLoad payloads. The legacy document-conversion entry point is retained without requiring Word.
- Release smoke-test gating, version-tag filtering, published SHA-256 checksums, and installer shortcut scope alignment.
- A real Flash SWF loading indicator on Home, including a visible percentage while a requested game/account waits for readiness.

Compatibility verification on 2026-10-15:

- Release build: passed.
- The legacy fixture and the available local compatibility corpus passed deserialization and current-format round-trip validation with command structures unchanged. Local corpus details are intentionally omitted.
- Update selection, startup recovery, updater merge/rollback, and registration-free Flash activation smoke tests: passed.
- WiX source compilation and MSI linking: passed. Full local ICE validation could not run because Windows Installer was unavailable in the verification session; the release workflow retains normal WiX validation on GitHub's Windows runner.

The following recommendations are intentionally not represented as completed optimizations because they require production AQW measurements, external credentials/infrastructure, or a compatibility-breaking architecture project:

- Broad Flash state caching/batching and packet-fed state replacement. These can change timing and state freshness and should follow live call-rate/p95 measurements with representative bots.
- Changing public packet-handler interfaces to `Task`, process/AppDomain plugin isolation, or splitting serialized/public types. Compatibility-preserving safeguards were applied instead.
- Authenticode signing, SBOM/provenance, and clean-VM upgrade matrices. Signing requires a protected certificate and the latter items belong in release infrastructure.
- Ruffle/custom game-host replacement, which remains a separate project rather than a v1.1.0 optimization patch.
