# Contributing to Grimoire Exodus

Thanks for helping improve Grimoire Exodus. By participating, you agree to
follow the [Code of Conduct](CODE_OF_CONDUCT.md).

## Before opening an issue

- Search existing issues to avoid duplicates.
- Use the bug or feature issue form and include enough detail to reproduce the problem.
- Never post passwords, account-vault contents, access tokens, private bot data, or other secrets.
- Report vulnerabilities privately as described in [SECURITY.md](SECURITY.md).

## Development setup

You need Windows and Visual Studio 2022 Build Tools with the .NET desktop build
tools workload. The first build also needs internet access to download the
hash-pinned private Flash runtime.

```powershell
git clone https://github.com/UnknownSecurity/grimoire-exodus.git
cd grimoire-exodus
.\exodus.ps1 build -Configuration Debug
```

Create a focused branch from `main`. Keep unrelated formatting or generated
output out of the change. Do not commit `Flash.ocx`, build output, local account
data, or credentials.

## Testing changes

Run the checks relevant to your change:

```powershell
.\exodus.ps1 build -Configuration Debug
git diff --check
```

For release, runtime, or installer changes, also run:

```powershell
.\exodus.ps1 installer
```

Confirm UI changes at the default 960×620 window size. Changes to the private
Flash activation path should also run `tests\Test-FlashActivation.ps1` from a
Developer PowerShell environment.

## Commits and pull requests

- Use a conventional commit message such as `fix: prevent accidental shutdown`.
- Keep each commit understandable and limited to one purpose.
- Explain the user-visible behavior and technical approach in the pull request.
- List the checks you ran and include screenshots for visible UI changes.
- Link related issues with `Fixes #123` when appropriate.
- Update documentation when behavior, setup, or release packaging changes.

Maintainers may request changes before merging. A contribution is not accepted
until it has been reviewed and merged.
