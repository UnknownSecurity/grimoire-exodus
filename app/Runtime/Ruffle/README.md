# Ruffle runtime

This directory is populated by `exodus.ps1` with the pinned official
`@ruffle-rs/ruffle` self-hosted web runtime. Downloaded JavaScript, WebAssembly,
and license files are build artifacts and are intentionally not committed.

The build verifies both the package archive and each allowlisted runtime file
before copying it into an application output. Ruffle is optional at runtime;
Flash.ocx remains the default and compatibility fallback.
