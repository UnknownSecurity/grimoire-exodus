# Shared cross-platform artifacts

This directory is the compatibility boundary between Grimoire Exodus ports. It
contains data contracts, schemas, and anonymized fixtures—not executable code.
Keeping this layer language-neutral lets the .NET Framework desktop client and
the Kotlin Android client validate the same behavior without forcing either
platform to adopt the other's runtime or UI framework.

- `contracts/` contains versioned wire and file-format contracts.
- `fixtures/` contains synthetic, privacy-safe inputs for parity tests.

Breaking changes require a new version directory. Existing legacy `.gbot`
types, property meanings, command order, and safe defaults remain compatibility
contracts.
