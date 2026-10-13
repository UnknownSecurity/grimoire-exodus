![Grimoire Exodus](docs/assets/grimoire-github-banner.png)

# Grimoire Exodus

Grimoire Exodus is a maintained AQW automation client based on Grimlite Rev. It is developed and maintained by **dsenin**.

## Compatibility rework

The current build supports AQW's R0048 / Game4000r2.swf client. It restores SmartFox packet capture and current game-version loading, and fixes the packet sniffer, bot-engine cancellation, bot and set loading, travel hotkeys, drop rejection, map-state detection, configuration parsing, plugin startup, current cell and pad discovery, and several stability issues.

The checked-in `game-client/grimoire-exodus.swf` bridge is copied into the application output automatically.

## Build from source

Requirements:

- Windows
- Visual Studio 2022 Build Tools with the .NET desktop build tools workload
- The ActiveX Flash runtime used by the original client (only required to run the current game client)

Use the dedicated Exodus developer CLI from PowerShell:

```powershell
.\exodus.ps1 help
.\exodus.ps1 build -Configuration Debug
.\exodus.ps1 build -Configuration Release
.\exodus.ps1 build -Configuration All
.\exodus.ps1 dev
.\exodus.ps1 version -Part patch
.\exodus.ps1 plugin -Name MyPlugin
.\exodus.ps1 man
```

Builds are written to `app\bin\<Configuration>\Grimoire Exodus.exe`. The CLI preserves the legacy internal `Grimoire` assembly identity required by existing dependencies, while leaving only the branded executable in the final output. `dev` performs a verbose Debug build and launches it directly. `plugin` creates a standalone starter workspace under `plugin-workspaces` unless `-OutputDirectory` is supplied.

## Development roadmap

| Status | Feature |
| --- | --- |
| ✅ Completed | Compatibility fixes for the current AQW UI and SWF client |
| ✅ Completed | Grimoire Home with an encrypted, one-click Account Locker |
| ✅ Completed | Conventional commit linting with Commitlint and Husky |
| ✅ Completed | Tag-triggered GitHub Actions release builds |
| Planned | Army Mode with a split-screen multi-client layout |
| Planned | Automatic application updates |
| Planned | A native Windows installer for Grimoire Exodus |
| Planned | A bundled native Flash runtime that removes the separate Clean Flash requirement |
| Planned | Background-process and orphan-state cleanup to prevent UI glitches |
| Planned | Remote control through a companion mobile app |
| Planned | A community-maintained bot script repository |

## Credits

- Current development, maintenance, and compatibility rework: **dsenin**
- Original Grimlite Rev maintainer: [GentleGanku](https://github.com/GentleGanku/GrimliteRev)
- Previous Grimlite developers: [wisp](https://github.com/wispie) and [cat](https://github.com/0zl)
- Previous Grimoire developers: [Biney](https://www.mpgh.net/forum/member.php?u=4062680) and [EmperorArts](https://www.mpgh.net/forum/member.php?u=2374072)
- Related projects and contributors: [Grimlite contributors](https://github.com/0zl/meow/graphs/contributors), [RBot](https://github.com/rodit/RBot), and [vtrsz/GrimliteRev](https://github.com/vtrsz/GrimliteRev)
