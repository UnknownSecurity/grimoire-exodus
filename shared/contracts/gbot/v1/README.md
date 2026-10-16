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
- legacy-compatible export is added only after semantic desktop-to-Android
  round-trip tests cover every supported command.

The current Android AIR milestone implements safe inspection and an explicit,
fail-closed execution subset. Supported command names are handled by a fixed
dispatcher; unknown commands are never resolved from `$type` and are skipped.
Rewriting/export remains disabled. The synthetic fixture in `fixtures/gbot/v1`
mirrors the desktop compatibility smoke test without containing local bot or
account information.
