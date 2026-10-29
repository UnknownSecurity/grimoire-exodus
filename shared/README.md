# Shared compatibility artifacts

This directory contains language-neutral contracts and anonymized fixtures used
by the desktop compatibility suite—not executable code. The independent mobile
repository carries its own copy for cross-platform parity testing.

- `contracts/` contains versioned wire and file-format contracts.
- `fixtures/` contains synthetic, privacy-safe inputs for parity tests.

Breaking changes require a new version directory. Existing legacy `.gbot`
types, property meanings, command order, and safe defaults remain compatibility
contracts.
