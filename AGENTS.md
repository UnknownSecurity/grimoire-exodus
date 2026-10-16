# Grimoire Exodus agent guide

## Scope

These instructions apply to the entire repository. Grimoire Exodus is a
Windows-only .NET Framework 4.8 WinForms application with a checked-in
ActionScript game bridge, a separate updater executable, a WiX installer, and
PowerShell build and smoke-test tooling.

Make focused changes. Preserve unrelated work in the working tree, avoid broad
formatting churn, and never modify or stop a running Grimoire process unless the
user explicitly asks for that action.

## Privacy and local-machine boundaries

- Treat account data, credentials, vault contents, bot files outside this
  repository, room names, packet bodies, logs, crash reports, and recovery data
  as private.
- Do not enumerate or report private bot filenames, bot quantities, usernames,
  account labels, absolute user-profile paths, or other machine-specific data.
  Report compatibility checks as pass/fail without identifying the local
  corpus.
- Use repository-relative paths in documentation, examples, diagnostics, and
  committed configuration. Never commit a developer username, home directory,
  interpreter path, or tool installation path.
- Do not scan outside the repository unless the user explicitly places an
  external location in scope. When testing GBot compatibility, prefer the
  repository-controlled `app\Bot` directory over the test script's optional
  per-user default.
- Never commit credentials, account-vault files, `Flash.ocx`, downloaded tools,
  build outputs, updater caches, or private bot data.

## Graph-first repository navigation

This project has a Graphify knowledge graph in `graphify-out/`.

- Before the first grep, broad search, or source read for a codebase question,
  run `graphify reflect --if-stale`, read
  `graphify-out/reflections/LESSONS.md` when present, and query the graph.
- Use `graphify query "<question>"` for broad relationships,
  `graphify path "<A>" "<B>"` for dependency paths, and
  `graphify explain "<concept>"` for a focused component.
- Put the graph-first rule in every subagent prompt that explores this
  repository. Read raw files after the graph identifies the relevant areas.
- Use `graphify-out/GRAPH_REPORT.md` for broad architecture reviews or when a
  scoped query is insufficient. If `graphify-out/wiki/index.md` exists, prefer
  it for broad navigation.
- After source or repository documentation changes, run `graphify update .`.
  Do not hand-edit generated graph JSON or HTML.
- Dirty graph output is expected after updates. Do not discard it merely because
  it is large; `graph.json` is rewritten as a whole.
- Keep machine-specific Graphify state such as `.graphify_python`,
  `.graphify_root`, cache, cost, vocabulary, memory, and reflections untracked.

## Architecture map

- `app/Grimoire.csproj` is the non-SDK main project. It compiles the client,
  bot engine, game model, networking, tools, utilities, and WinForms UI into the
  legacy `Grimoire` assembly. The build script renames the distributable file to
  `Grimoire Exodus.exe` while preserving that internal assembly identity.
- `app/Grimoire/Program.cs` owns startup, global exception handling, and clean
  shutdown recording. `app/Grimoire.UI/Root.cs` is the main orchestration and UI
  hub; it connects Flash, the proxy, bot UI, Home surface, updater, recovery,
  tray behavior, and shutdown confirmation.
- `app/Grimoire.Botting/` contains the bot lifecycle, configuration, safe JSON
  serialization, and command contracts. Command implementations are grouped
  under the `Grimoire.Botting.Commands.*` directories.
- `app/Grimoire.Game/` and `app/Grimoire.Game.Data/` expose live game state and
  serialized models. `app/Grimoire.Networking/` and
  `app/Grimoire.Networking.Handlers/` dispatch SmartFox/XML/JSON/XT traffic.
- `app/Grimoire.Tools/Flash.cs` is the synchronous C# side of the ActiveX
  `ExternalInterface` bridge. `game-client/AS3/grimoire/` is the ActionScript
  side, while `game-client/grimoire-exodus.swf` is the runtime artifact copied
  into builds.
- `app/Resources/Home/index.html` is the embedded Home UI.
  `app/Grimoire.UI/GrimoireHomeBridge.cs` is its native host boundary. Keep the
  JavaScript calls, JSON response shapes, and C# methods synchronized.
- `app/Grimoire.Utils/AppPaths.cs` owns installed-versus-writable storage.
  `StartupDiagnostics.cs`, `UpdateService.cs`, `AtomicFile.cs`, and
  `BoundedTextBuffer.cs` contain reusable safety behavior; use them instead of
  adding parallel implementations.
- `app/Grimoire.Updater/` is a separate executable that applies MSI or portable
  updates after the client exits and rolls back failed portable merges.
- `installer/Product.wxs` defines the per-machine x86 MSI, shortcuts, upgrade
  behavior, and optional launch action. `exodus.ps1` is the canonical build,
  versioning, runtime, installer, and plugin entry point.
- `app/Bot/` is the repository-controlled Community Bot payload. Per-user bots
  are external data and must never be copied into the repository or release
  staging area.
- `.github/workflows/tagged-build.yml` builds tagged releases, runs the complete
  compatibility smoke suite, emits checksums, and publishes the MSI and portable
  ZIP.
- `mobile/` contains the independent Kotlin/Jetpack Compose management-shell
  prototype and the standalone HARMAN AIR client. Kotlin modules must depend
  inward through `core/model` and `core/domain`; neither Android surface may
  reference WinForms, ActiveX, the desktop updater, or mutable desktop output.
- `mobile/air-client/` compiles the checked-in ActionScript bridge into a
  landscape-only captive-runtime APK. It loads AQW directly from the official
  live endpoint and must not launch, patch, or control another Android app.
- `game-client/AS3/grimoire/MobileShell.as` is the AIR-only Grimoire wrapper
  around AQW. Preserve its always-accessible focus button, animated framed/full
  layout, and separation from the desktop `ExternalInterface` display path.
- `shared/` contains versioned, language-neutral contracts and synthetic
  fixtures for both ports. It must not contain executable binaries, generated
  platform models, credentials, user bots, or machine-specific paths.

## Compatibility contracts

### Legacy GBots

- Existing `.gbot` files are a public compatibility contract. Preserve command
  type names, namespaces, serialized property meanings, command ordering, safe
  defaults, and the internal `Grimoire` assembly identity.
- Keep `BotJson.CreateSettings()` and `SafeBotSerializationBinder` in the load
  and save path. Never replace the allowlist with unrestricted
  `TypeNameHandling` or arbitrary type resolution.
- Prefer additive, optional fields with safe defaults. If a serialized model or
  command changes, verify both old-file loading and current-format round trips.
- Adding a C# source file is not enough: this is a non-SDK project, so include
  it explicitly in `app/Grimoire.csproj`.

### Flash and ActionScript bridge

- The application depends on the 32-bit ActiveX ABI, synchronous
  `CallFunction`/`FlashCall` behavior, and the live SmartFox connection. Preserve
  `Prefer32Bit`/x86 behavior, `app/Grimoire.manifest`, registration-free COM,
  and the app-local `Flash.ocx` layout.
- Change C# bridge names, ActionScript `ExternalInterface` callbacks, arguments,
  and return shapes together. Do not turn a synchronous bridge call into an
  asynchronous contract without an explicit migration plan.
- ActionScript source changes do not ship until
  `game-client/grimoire-exodus.swf` is rebuilt and checked in. If the required
  authoring toolchain is unavailable, report that limitation instead of claiming
  the runtime artifact was updated.
- The Flash archive and extracted OCX are independently SHA-256 pinned in
  `exodus.ps1`. Do not weaken verification, register Flash system-wide, commit
  the OCX, or use the runtime for untrusted SWFs.

### User data and recovery

- Installed application files may be read-only. Route settings, plugins, sets,
  logs, updates, recovery state, and the encrypted vault through `AppPaths`.
  Do not write new mutable state beside the executable.
- Preserve user-managed bots and all unrelated files during installs and
  updates. Migration copies must not overwrite a destination file that already
  exists.
- Account passwords are protected for the current Windows user. Never return a
  decrypted password in Home state, logs, diagnostics, exceptions, or support
  bundles.
- Recovery may offer the previous bot for review, but it must never start that
  bot automatically. Cleanup must remain bounded to application-owned paths and
  files.

### Updates and releases

- Updates must originate from the official release feed, select the correct
  asset, validate version and payload shape, enforce trusted-host and path
  boundaries, and verify the GitHub digest before launch.
- Portable updates must preserve unrelated and user-owned files, keep a backup
  of replaced files, remove newly created files on failure, and restore the old
  payload as far as possible.
- Do not remove the confirmation path that protects a running bot from an
  accidental close or update restart.
- Release staging must contain `Grimoire Exodus.exe`, `Grimoire.Updater.exe`,
  `Flash.ocx`, `grimoire-exodus.swf`, resources, and the Community Bot directory.
  Never stage an external per-user bot directory.
- Keep main-client and updater versions synchronized. Prefer
  `.\exodus.ps1 version -Part <major|minor|patch|revision>` over manual edits,
  then update release notes and other user-facing version references together.

## Runtime and UI rules

- The client targets .NET Framework 4.8 and configuration-specific C# 7.3.
  Avoid APIs or language features that require modern .NET or a newer compiler.
- Keep WinForms access on the UI thread. Use existing invocation helpers and
  cancellation-aware async flows; avoid blocking waits on the UI thread and
  avoid `async void` except for event handlers.
- Bot runs own their cancellation and lifecycle state. A stopped or superseded
  run must not observe or mutate a newer run's configuration.
- Keep packet and log ingress bounded. Avoid per-packet reflection, repeated
  handler discovery, unbounded queues, or unbounded UI text growth.
- Do not edit generated `*.Designer.cs`, `.resx`, or settings designer files
  unless the change genuinely belongs there. Keep manual UI behavior in the
  corresponding non-designer partial class.
- Validate visible UI changes at the default 960×620 window size and with both
  light and dark theme behavior where applicable. Preserve non-closable manager
  tabs, tray restore behavior, and close confirmation while a bot is running.
- Home bridge methods must validate inputs and return structured responses.
  Do not expose arbitrary filesystem access, process execution, or secret data
  to the embedded page.
- Plugins execute in-process with full trust. Do not silently broaden discovery
  or auto-load arbitrary DLL locations, and do not claim unload provides process
  isolation.

## Build and validation

Use PowerShell from the repository root. Prefer the developer CLI over direct
MSBuild invocation because it restores packages, verifies the private runtime,
and completes branded output correctly.

Baseline for source changes:

```powershell
.\exodus.ps1 build -Configuration Debug
git diff --check
```

Run the tests relevant to the changed area after building the same
configuration:

```powershell
# Bot commands, models, serialization, or loading. This path stays in-repo.
.\tests\Test-LegacyGbotCompatibility.ps1 -Configuration Debug -BotDirectory .\app\Bot

# Release selection, download validation, or UpdateService changes.
.\tests\Test-UpdateService.ps1 -Configuration Debug

# Portable merge, preservation, or rollback changes.
.\tests\Test-UpdaterMerge.ps1 -Configuration Debug

# Startup markers, orphan cleanup, crash reports, or recovery changes.
.\tests\Test-StartupDiagnostics.ps1 -Configuration Debug

# Manifest, ActiveX, interop, or app-local Flash changes.
.\tests\Test-FlashActivation.ps1 -Configuration Debug
```

For installer, packaging, updater, runtime, or release-workflow changes, build
Release and run the release-equivalent suite:

```powershell
.\exodus.ps1 installer
.\exodus.ps1 build -Configuration Release
.\tests\Test-LegacyGbotCompatibility.ps1 -Configuration Release -BotDirectory .\app\Bot
.\tests\Test-UpdateService.ps1 -Configuration Release
.\tests\Test-StartupDiagnostics.ps1 -Configuration Release
.\tests\Test-UpdaterMerge.ps1 -Configuration Release
.\tests\Test-FlashActivation.ps1 -Configuration Release
```

For changes under `mobile/` or `shared/`, validate the independent Android
workspace from `mobile/` without launching an emulator or either application:

```powershell
.\gradlew.bat test :app:assembleDebug :app:lintDebug
```

For standalone AIR changes, also build the development APK from the repository
root without launching either application:

```powershell
.\mobile\air-client\build-air.ps1 -Configuration Debug
```

Keep `OfflineGameTransport` as the fail-closed Compose default until the native
management shell is deliberately connected to the AIR callback registry. Do
not use accessibility automation, runtime hooking, root, packet interception,
or a repackaged third-party APK as a substitute. Cross-platform fixture changes
must also pass the desktop compatibility smoke test against the
repository-controlled bot directory.

Additional rules:

- The first build and installer build may download hash-pinned dependencies.
  State this clearly if network access or the Windows toolchain is unavailable.
- Do not use `.\exodus.ps1 dev` as a validation command unless launching the
  client is requested; it opens the application.
- For Home or WinForms UI changes, perform focused manual verification when a
  graphical session is available and state what could not be checked otherwise.
- `package.json` is only development tooling for Commitlint and Husky; it is not
  the application build or test entry point.
- Do not create tags, publish releases, upload assets, or alter an existing
  release unless the user explicitly requests those external actions.

## Change-specific checklist

- New C# file or resource: add the correct `Compile`, `Content`, or `None` entry
  to `app/Grimoire.csproj` and verify copy-to-output behavior.
- New bot command: preserve `IBotCommand` semantics, editor/load behavior, safe
  serialization, command order, cancellation, and legacy round-trip tests.
- Game protocol change: inspect the proxy handler, game model, Flash bridge, and
  ActionScript bridge together; do not patch only one layer of the flow.
- Home change: update `index.html` and `GrimoireHomeBridge` together when the host
  contract changes; check loading, empty, failure, and running-client states.
- Updater change: verify selection, digest validation, containment, required
  payload files, merge success, rollback failure, and user-file preservation.
- Installer change: verify upgrade behavior, desktop shortcut selection,
  launch-after-setup, Program Files write restrictions, and portable payload
  parity.
- Documentation or release change: keep README, release notes, installer/build
  behavior, and workflow claims consistent with the actual implementation.

## Completion standard

Before handing work back:

- Review the final diff for accidental generated files, binaries, secrets,
  private paths, and unrelated edits.
- Report the behavior changed, the exact checks run, and any validation that was
  unavailable. Do not include private local data in the report.
- Update Graphify after repository changes and include intentional graph-output
  changes only when they are part of the team's chosen tracking policy.
