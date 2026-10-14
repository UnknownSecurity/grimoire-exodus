# App-local Flash runtime

Grimoire Exodus uses a private, registration-free 32-bit ActiveX runtime so
the application can run without a system-wide Clean Flash installation.

Run `./exodus.ps1 runtime` (or any normal `build`/`installer` command) to
download the pinned runtime, verify both SHA-256 hashes, and place it here as
`Flash.ocx`. The binary is intentionally not committed to source control.

Runtime source: <https://github.com/darktohka/clean-flash-builds/releases/tag/v1.54>

- Archive: `ChineseFlash-Patched-Win-34.0.0.376.7z`
- Archive SHA-256: `19A8D1036110AF024DC877CA96A9184835C3E944FB14CA8D509662F00BF1BD31`
- Extracted OCX SHA-256: `4E1CAA0B6805E5D4E4F737C4389B8E94E92CA49E7D7AEF17C3773A9D744DCC42`

See the repository `README.md` third-party software section before redistributing a build.
