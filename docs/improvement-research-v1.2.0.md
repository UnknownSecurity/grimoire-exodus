# Grimoire Exodus v1.2.0 improvement and feature research

Date: 2026-10-15  
Scope: current `main` at v1.2.0  
Mode: research and recommendations only; no application behavior was changed by this audit

## Executive summary

The v1.2.0 work materially improved cancellation ownership, logging, update delivery, app-local Flash activation, startup recovery, and GBot discovery. The next release should now focus on making failure states explicit and measurable instead of adding more automation commands first.

The highest-value next project is an **AQW compatibility and loading state machine**. The Flash bridge already downloads AQW's live version manifest, but the desktop app receives only a download percentage. Manifest failures, SWF failures, a stalled initialization, and an incompatible client can therefore look identical to an ordinary load. Grimoire can safely expose the live client identity, show named stages, retry failures, and prevent bot startup when compatibility is unknown. It should not label a version mismatch as a ban risk because the code has no evidence that a mismatch itself causes bans.

The other immediate priorities are continuous integration before tags, privacy-safe diagnostic export, prompt bot cancellation, bounded packet/log ingestion, and moving startup work off the UI thread. These can all be implemented without changing the legacy `.gbot` schema, serialized type names, `IBotCommand`, or `IBotEngine`.

No local GBot filenames, paths, or corpus quantities are included in this report.

## Compatibility rules for all future work

The following are release gates, not optional preferences:

1. Keep the internal assembly identity `Grimoire` and all existing serialized command type names.
2. Keep `IBotCommand.Execute(IBotEngine)` and the current public `IBotEngine` surface available to old commands and plugins.
3. Continue using the restricted serialization binder in `Grimoire.Botting/BotJson.cs`; never restore unrestricted `TypeNameHandling`.
4. Treat absent fields as legacy input and retain their current safe defaults.
5. Do not rewrite a `.gbot` merely because it was loaded. Save or migrate only after an explicit user action.
6. Store new metadata—favorites, tags, run history, checkpoints, trust decisions—in sidecar application data keyed by a normalized path plus file hash. Do not insert it into legacy bot JSON.
7. Validate every change against a repository-owned, anonymized compatibility corpus. Local libraries may be used privately as an additional check, but their contents and statistics must not enter logs, documentation, screenshots, or release notes.

## What is already strong

- `BotJson` restricts polymorphic deserialization to allowed application and framework types while resolving legacy assembly metadata.
- Bot runs now own cancellation sources and worker tasks, and lifecycle transitions are serialized.
- Update discovery restricts release URLs, asset sizes, redirects, archive paths, and SHA-256 digests.
- Portable updates back up replaced files and roll back an interrupted merge.
- Account passwords are protected with Windows DPAPI for the current user.
- UI text logs are capped, batched, and marshalled to the UI thread.
- Startup diagnostics avoid killing suspected orphan processes and identify sessions by process path and start time.
- The quick-bot index runs outside the UI thread and uses filesystem invalidation.

These controls should be preserved while the remaining gaps are addressed.

## Prioritized engineering findings

| ID | Recommendation | Impact | Effort | Delivery risk | Legacy GBot risk |
| --- | --- | --- | --- | --- | --- |
| P0-1 | Add a staged AQW loader and compatibility gate | Very high | Medium | Medium | None |
| P0-2 | Run build and compatibility checks on pull requests and `main` | Very high | Medium | Low | Reduces risk |
| P0-3 | Make built-in bot waits and event work promptly cancellable | High | Medium | Medium | Low with an internal adapter |
| P0-4 | Add privacy-redacted diagnostic export | High | Small–medium | Low | None |
| P1-1 | Defer startup plugins, Flash creation, and migrations from the UI thread | High | Medium | Medium | None |
| P1-2 | Bound packet/log producer queues and add back-pressure counters | High | Medium | Medium | None |
| P1-3 | Instrument synchronous Flash calls before caching or batching them | High | Medium | Low | None |
| P1-4 | Remove sync-over-async account-site calls | Medium–high | Small | Low | None |
| P1-5 | Strengthen update provenance and post-update recovery | High | Medium–large | Medium | None |
| P1-6 | Add an explicit plugin trust model | High | Medium–large | Medium | Plugin compatibility only |
| P2-1 | Split oversized UI classes behind compatibility-preserving services | Medium | Large | Medium | Low |
| P2-2 | Establish a shared theme, DPI, keyboard, and accessibility layer | Medium | Medium | Low | None |
| P2-3 | Define a supported-runtime and dependency lifecycle | Medium | Medium | Medium | None |

### P0-1 — AQW loading and compatibility need explicit states

Evidence:

- `game-client/AS3/grimoire/Root.as:83-107` fetches `api/data/gameversion`, builds the live SWF URL, and starts the loader.
- `game-client/AS3/grimoire/Root.as:110-113` reports only the SWF byte percentage.
- There are no `IOErrorEvent`, `SecurityErrorEvent`, HTTP-status, retry, or timeout handlers on the version request or SWF loader.
- `app/Grimoire.UI/Root.cs:1291-1313` treats 100% download as game readiness.
- The manifest's `sFile` and `sVersion` values never reach the C# host, so the requested client-status indicator cannot reliably distinguish supported, changed, and unknown clients.

Recommendation:

- Have the bridge emit structured stages: `manifest`, `swf-download`, `swf-loaded`, `bridge-initializing`, `login-ready`, and `failed`.
- Emit the live `sFile` and `sVersion` values after validating their types and length.
- Add finite timeouts, one automatic retry with jitter, and a visible manual Retry action. A cached SWF may skip most download progress, so stage text is more truthful than forcing a fake percentage.
- Compare the live identity with a checked-in supported-client manifest. If it is unknown, show **Compatibility not yet verified** and disable bot start/raw packet automation by default while still allowing the user to view diagnostics.
- Do not use “ban risk” as a computed status. The client can establish compatibility, not predict enforcement.

This is compatible with all old GBots because the gate sits before execution and does not alter deserialization or commands.

### P0-2 — Tests run only after a release tag is pushed

Evidence:

- `.github/workflows/tagged-build.yml` is triggered only by `v*` tags.
- The current smoke suite covers update selection, portable merge/rollback, startup recovery, Flash activation, and one embedded legacy JSON fixture.
- The compatibility script optionally scans a local folder. That is useful privately, but a clean CI runner does not have that private corpus.
- There are no automated checks for bot lifecycle races, cancellation latency, event-handler failures, packet parsing limits, account-vault corruption, UI startup, installer upgrade/repair/uninstall, or accessibility.

Recommendation:

Create two workflows:

1. A read-only PR/`main` workflow that restores/builds once, runs unit and smoke tests, validates the anonymized GBot fixture corpus, and uploads diagnostics only on failure.
2. The existing tag workflow, which should depend on the same test entry point and package the exact tested outputs.

Add repository-owned fixtures for each command family, missing legacy fields, old assembly versions, unknown/disallowed types, maximum depth, malformed JSON, and cross-bot load commands. Record only generic pass/fail output.

### P0-3 — cancellation does not reach many legacy command waits

Evidence:

- `app/Grimoire.Botting/Bot.cs:248-403` owns and waits for bot, quest, and drop workers correctly.
- `IBotCommand.Execute(IBotEngine)` has no cancellation-token parameter, which is an important compatibility constraint.
- `app/Grimoire.Botting/BotUtilities.cs:46-55` polls with uncancellable delays.
- Built-in commands and `Bot.cs:568-627` contain uncancellable delays and `async void` event handlers.
- An in-flight handler reads the mutable `Configuration` property, so work started by an old run can observe a newly loaded configuration after a restart.

Recommendation:

- Keep both public interfaces unchanged.
- Add an internal run-services object or engine-owned delay helper that exposes the current run token to built-in commands.
- Snapshot the run/configuration in event callbacks, track their tasks, cancel them with the run, and ignore completions from a stale run identifier.
- Add a bounded stop deadline and report which command is still winding down. Do not abort threads.
- Measure `stop requested → all owned workers stopped`; use a target under one second for cancellable waits and clearly label commands that are performing an external operation.

### P0-4 — diagnostics are useful locally but unsafe to share as-is

Evidence:

- `StartupDiagnostics.UpdateActivity` stores the full bot path.
- crash and unclean-session reports include the executable and/or last bot path, and exception text can contain local paths or account-related server responses.
- retention is bounded, which is good, but there is no redacted “copy for support” path.

Recommendation:

Add an **Export diagnostics** action that produces a ZIP only after preview and consent. Replace user-profile paths, bot filenames, account labels/usernames, server room numbers, and packet bodies with stable placeholders. Include version, Flash version, stage timings, compatibility identity, exception type/stack, and a manifest of what was redacted. Keep raw local reports local.

### P1-1 — startup still performs expensive work on the UI thread

Evidence:

- `Root` constructs Home, bot watchers, tray UI, metrics, and other services synchronously.
- `Root_Load` synchronously enumerates and executes every plugin, creates the ActiveX Flash control, reads the bridge SWF, initializes proxy handlers, and shows Home.
- resolving `AppPaths.ExternalBotsDirectory` can create and migrate bot directories synchronously. Redirected or cloud-backed Documents storage can make this slow.
- update checking is asynchronous and should remain so.

Recommendation:

Render the shell first, then run named startup stages with timing and cancellation. Keep ActiveX construction on the STA UI thread but defer it until the first idle cycle. Enumerate/migrate files and inspect plugin metadata in background work. Load third-party plugin code only after the window is responsive, with a safe-start option that skips plugins.

### P1-2 — displayed logs are bounded; their pending queues are not

Evidence:

- `BoundedTextBuffer` caps control text and batches UI writes.
- Its `ConcurrentQueue<string>` has no item or byte limit. A packet burst can consume memory faster than the UI drains it.
- `Proxy.PublishExtensionResponse` parses server-controlled JSON without a length guard before allocating a `JObject` tree.
- packet capture, tamperer, and spammer features can produce high-rate input.

Recommendation:

Use a bounded channel/ring buffer with a byte budget, drop-oldest behavior for display-only logs, and a visible “messages skipped” counter. Apply maximum packet length and nesting limits before parsing or displaying. Preserve ordered delivery for state handlers; diagnostics may be sampled, but game-state messages may not be silently reordered.

### P1-3 — measure Flash calls before changing their semantics

Evidence:

- `Flash.Call` synchronously crosses COM, builds XML, invokes `CallFunction`, parses XML, and often deserializes JSON.
- game properties call it frequently from polling loops and commands.
- most failures return `default`, making a transport error indistinguishable from a real `false`, `0`, or `null`.

Recommendation:

First add low-overhead per-method counters, elapsed-time histograms, failure reasons, and UI-thread attribution. Log aggregates, never arguments. Then optimize only measured hot calls. Safe options include packet-fed immutable snapshots and short-lived per-tick caches, but each candidate needs freshness tests because bot behavior depends on timing.

### P1-4 — the account buyback path blocks async work

Evidence:

- `AutoBuyBack.SendPost` calls `.Result` inside an async method.
- `Dispose` calls `.Wait()` on a logout request.
- exceptions are swallowed into empty strings, losing the difference between authentication, timeout, parsing, and network failure.

Recommendation:

Await the HTTP request and response end to end, add cancellation and finite timeouts, URL-encode every form field, return typed failure results, and make logout best-effort asynchronous. Never log credentials or response bodies containing account data.

### P1-5 — updater integrity is good, but trust and recovery can improve

Evidence:

- update assets are constrained to the official repository, HTTPS, trusted redirect hosts, a size ceiling, GitHub's SHA-256 asset digest, and safe ZIP paths.
- a GitHub account/repository compromise could replace both an asset and its digest; there is no independent signing identity.
- portable backups are deleted immediately after a successful merge. A syntactically valid update that cannot start has no automatic previous-version recovery.
- release actions use moving tags such as `actions/checkout@v4` rather than immutable commit SHAs.

Recommendation:

- Authenticode-sign the executable, updater, and MSI once certificate operations can be protected.
- Publish an SBOM and GitHub artifact attestation; pin third-party actions to full commit SHAs.
- Keep one previous application payload until the updated app writes a healthy-start marker. Offer rollback if startup fails repeatedly.
- Add clean-install, in-place upgrade, repair, uninstall, denied-elevation, locked-file, low-disk, and offline test cases.

### P1-6 — plugins are fully trusted and cannot actually unload

Evidence:

- `Assembly.LoadFile` loads arbitrary DLL code into the main .NET Framework AppDomain.
- startup automatically executes every DLL in the plugin folder.
- the Unload button calls a plugin method and removes the list entry, but the assembly and any surviving threads/event handlers remain loaded.

Recommendation:

Short term: safe-start without plugins, per-plugin enablement, hash/publisher display, explicit first-load confirmation, startup timeout reporting, and quarantine after repeated startup crashes. Longer term: a separate plugin host process with a narrow versioned IPC contract. Keep the current in-process interface as a legacy mode until plugin authors can migrate.

### P2-1 — UI and runtime responsibilities remain tightly coupled

Evidence:

- `BotManager.cs` is a very large generated-and-behavioral class.
- `Root.cs` owns Home hosting, Flash lifecycle, updater UI, bot indexing, metrics, tray behavior, diagnostics, theming, and most menus.
- runtime code reads WinForms controls directly for configuration and status.

Recommendation:

Extract testable services one seam at a time: `GameHost`, `BotLibrary`, `BotRunCoordinator`, `UpdateCoordinator`, `DiagnosticsExporter`, and immutable settings snapshots. Leave public/static facades in place as adapters for legacy plugins and commands. Do not perform a big-bang rewrite.

### P2-2 — accessibility and theme behavior are inconsistent

Evidence:

- native dark-title handling exists only in `Root`; secondary windows do not share it.
- most controls rely on fixed pixel sizes and only a small subset has explicit accessible names/descriptions.
- the Home surface runs in the legacy WinForms `WebBrowser` engine and suppresses script errors, which makes browser-specific failures hard to diagnose.

Recommendation:

Create a shared form/theme base, keyboard navigation audit, logical focus order, high-contrast palette, 100/125/150/200% DPI test matrix, and screen-reader labels for icon-only controls. Prefer a native Home surface or a carefully packaged modern web runtime only after measuring footprint and deployment impact. Until then, expose suppressed Home errors to diagnostics in development builds.

### P2-3 — the platform and dependency lifecycle needs an explicit plan

The application deliberately targets .NET Framework 4.8 and x86 because the Flash ActiveX ABI requires it. That is a valid compatibility choice, but the packages, checked-in interop/UI binaries, Flash runtime, and WiX 3 toolchain need scheduled review. Automate dependency inventory and vulnerability alerts, document provenance and hashes for checked-in binaries, and separate “must stay for ABI compatibility” from unused historical files.

Long term, Ruffle or a custom host is not a drop-in replacement: Grimoire depends on AVM2 behavior, synchronous `ExternalInterface`, and the SmartFox connection exposed by the live AQW client. Treat replacement as a separately funded prototype with compatibility milestones rather than a routine runtime update.

## Feature candidates

### Best small-to-medium features

| Feature | User value | Compatibility approach |
| --- | --- | --- |
| Bot Doctor | Validate a bot without running it; report missing labels, unreachable load targets, unsafe URLs, extreme delays, and unsupported command types | Read-only analysis; never rewrite the file |
| Search, favorites, recent bots, and tags | Makes a large library usable without exposing or renaming files | Sidecar index in local app data |
| Compatibility Center | Shows AQW manifest identity, Flash version, bridge health, last successful check, and a safe retry | No `.gbot` changes |
| Start preflight | Checks login, inventory capacity, required items, quests, server, and compatibility before a run | Optional warnings; execution format unchanged |
| Execution timeline | Shows current command, elapsed time, wait reason, retry count, and last state transition | Observe the engine; do not serialize runtime state |
| Session summary | Local totals for duration, reconnects, deaths, accepted drops, and errors | Opt-in local sidecar; never upload automatically |
| Privacy-safe support bundle | One-click redacted diagnostics with preview | New export only |
| Safe mode | Start without plugins, hotkeys, packet capture, or auto-recovery after a crash | Startup flag/config only |
| Offline bot editor | Edit and validate GBots before loading Flash or signing in | Reuse the safe binder and command model |
| Dangerous-tools lock | Hide raw packets/spammer/tamperer until an advanced mode is explicitly enabled | UI setting only; legacy commands still deserialize |

### Medium projects

#### Community bot catalog with trust metadata

Publish a small signed manifest containing bot name, author, version, description, minimum Grimoire version, SHA-256, review status, and download URL. Install into a dedicated managed folder and never overwrite user-modified files. The catalog should not execute remote content directly and should show diffs/metadata before replacement.

#### Opt-in bot checkpoints

Save the current bot hash, command index, map, and timestamp to a sidecar checkpoint. Recovery should load the bot for review and offer resume only if the hash still matches; never auto-run after a crash. Commands with non-idempotent effects need a warning or must resume from a declared safe label.

#### Character cards in Account Locker

This is technically possible only as a best-effort, opt-in enhancement. Fetch public character-page data after the user opens or refreshes an account, cache a small sanitized image/summary, rate-limit requests, and fall back to the existing label when markup changes. Do not scrape during every Home refresh, do not send passwords, and do not treat the public page as an authentication source.

#### Update channel and rollback UI

Allow Stable and opt-in Prerelease channels, show release notes before installation, expose download/apply status, retain one healthy previous version, and provide an explicit rollback action. Channel choice belongs in app settings, not a GBot.

### Large projects

- A multi-client/Army Mode coordinator with a process per client, resource limits, independent crash containment, and explicit account assignment.
- An out-of-process plugin SDK and compatibility shim.
- A modern replacement for the Flash/AQW host only after the prototype proves AVM2, `ExternalInterface`, networking, and bot-state parity.
- A visual bot-flow editor with a lossless legacy command-list view. The legacy list remains the source representation until round-trip parity is proven.

## Performance validation plan

The code review identifies likely hot paths, not measured production bottlenecks. Before optimizing semantics, add a local benchmark/diagnostic mode with argument-free aggregates for:

- cold and warm startup stage duration;
- Home-ready, Flash-created, manifest-loaded, SWF-loaded, bridge-ready, and login-ready times;
- Flash calls per method, p50/p95/p99 latency, error count, and UI-thread time;
- packet rate, parse time, handler time, queue depth, dropped diagnostic messages, and largest accepted packet;
- bot command duration, cancellation latency, relog attempts, and stale-run callback rejections;
- UI heartbeat delay and working-set/private-byte growth;
- bot-library scan duration for local, cloud-backed, unavailable, and permission-denied directories.

Use 30-minute active and 8-hour soak sessions with packet capture off and on. Capture only aggregate method names and timings—never credentials, packet bodies, bot filenames, account names, or local library statistics.

Suggested acceptance targets:

- shell visible and interactive before optional plugin scanning;
- no UI heartbeat pause above 100 ms during ordinary bot execution;
- cancellable waits stop within one second;
- log/packet memory remains bounded under a synthetic burst;
- no growth in event subscriptions across repeated start/stop/reload cycles;
- portable update either completes or restores all prior application files;
- every anonymized legacy fixture round-trips with command type, order, and values preserved.

## Recommended delivery order

### Milestone A — guardrails

1. PR/`main` CI and anonymized compatibility fixtures.
2. Privacy-safe diagnostics export and generic test output.
3. Performance counters and a repeatable baseline.
4. Loader error events, timeouts, retries, and named stages.

### Milestone B — safe runtime

1. Supported-client identity and fail-closed automation gate.
2. Cancellable built-in waits and tracked run-event tasks.
3. Bounded packet/log ingress.
4. Async account-site operations.

### Milestone C — product improvements

1. Bot Doctor and start preflight.
2. Bot-library search/favorites/recent items.
3. Execution timeline and local session summary.
4. Character cards as an opt-in best-effort enhancement.

### Milestone D — trust and maintainability

1. Signed releases, SBOM, provenance, and previous-version recovery.
2. Plugin safe mode and trust center.
3. Progressive `Root`/`BotManager` decomposition.
4. Accessibility, DPI, and shared window theming.

## External references

- [Adobe Flash Player end-of-life](https://www.adobe.com/products/flashplayer/end-of-life.html)
- [Microsoft .NET Framework lifecycle FAQ](https://learn.microsoft.com/en-us/lifecycle/faq/dotnet-framework)
- [WinForms WebBrowser control overview](https://learn.microsoft.com/en-us/dotnet/desktop/winforms/controls/webbrowser-control-windows-forms)
- [GitHub artifact attestations](https://docs.github.com/en/actions/security-for-github-actions/using-artifact-attestations)
- [GitHub secure use guidance for Actions](https://docs.github.com/en/actions/security-for-github-actions/security-guides/security-hardening-for-github-actions)
- [OWASP deserialization guidance](https://cheatsheetseries.owasp.org/cheatsheets/Deserialization_Cheat_Sheet.html)

## Audit conclusion

Grimoire Exodus is ready for incremental hardening, not a rewrite. The best next feature is the Compatibility Center backed by a real loader state machine, followed by Bot Doctor and library search. The best next engineering investment is CI plus an anonymized fixture corpus. Together, those changes make future feature work safer while preserving every legacy GBot contract.
