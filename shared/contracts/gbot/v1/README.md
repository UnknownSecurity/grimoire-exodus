# Legacy GBot compatibility contract

Version 1 describes the existing Newtonsoft JSON shape used by desktop
Grimoire. It is not a new file format.

Compatibility requirements:

- the root type remains `Grimoire.Botting.Configuration`;
- commands remain ordered entries under `Commands.$values`;
- command type names remain in `Grimoire.Botting.Commands.*`;
- existing property names and meanings are preserved;
- missing optional fields receive the same safe defaults as the desktop app;
- unknown or unsupported commands may be inspected but never executed;
- parsing is bounded and data-only—no runtime type loading or reflection;
- legacy-compatible export requires semantic round-trip tests for every
  supported command.

The synthetic fixture in `fixtures/gbot/v1` mirrors the desktop compatibility
smoke test without containing local bot or account information. Mobile-specific
parsers and execution subsets live in
[`UnknownSecurity/grimoire-mobile`](https://github.com/UnknownSecurity/grimoire-mobile).
