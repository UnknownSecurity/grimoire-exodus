# Game client build targets

The ActionScript sources under `AS3/` are shared by the Windows and Android
clients, but their compiled SWF files are not interchangeable.

- `grimoire-exodus.swf` is the Windows desktop bootstrap loaded by the app-local
  ActiveX Flash Player. It must remain a Flash Player-compatible publish output.
- `../mobile/air-client/build-air.ps1` compiles an AIR bootstrap into its own
  ignored staging directory for Android packaging.

Never copy the AIR compiler output over `grimoire-exodus.swf`. An AIR-targeted
SWF can report a valid ActiveX ready state while executing none of the bootstrap
callbacks, which leaves the Windows Home screen at `Loading AQW 0%`.

The desktop artifact is SHA-256 pinned by `exodus.ps1`. An intentional desktop
publish must update that pin in the same change; an Android AIR build must not.

After intentionally replacing the desktop SWF, build the Windows client and run:

```powershell
.\tests\Test-FlashLoadProgress.ps1 -Configuration Debug
```

The check loads the built SWF through the same app-local ActiveX path and must
observe AQW download progress above zero.
