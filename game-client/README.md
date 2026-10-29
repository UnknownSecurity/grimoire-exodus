# Desktop game client

The ActionScript sources under `AS3/` and `grimoire-exodus.swf` belong to the
Windows desktop client. The checked-in SWF is loaded by the app-local ActiveX
Flash Player and must remain a Flash Player-compatible publish output.

Android source and AIR packaging now live in
[`UnknownSecurity/grimoire-mobile`](https://github.com/UnknownSecurity/grimoire-mobile).
Never copy a mobile AIR compiler output over the desktop SWF: an AIR-targeted
artifact can report a valid ActiveX ready state while executing none of the
desktop bootstrap callbacks, leaving Home at `Loading AQW 0%`.

The desktop artifact is SHA-256 pinned by `exodus.ps1`. An intentional desktop
publish must update that pin in the same change. Mobile releases are versioned
and built independently in the mobile repository.

After intentionally replacing the desktop SWF, build the Windows client and run:

```powershell
.\tests\Test-FlashLoadProgress.ps1 -Configuration Debug
```

The check loads the built SWF through the same app-local ActiveX path and must
observe AQW download progress above zero.
