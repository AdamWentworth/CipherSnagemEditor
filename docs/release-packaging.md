# Release And Packaging Notes

The source files under `packaging/` are tracked on purpose. They are packaging
recipes and desktop integration templates, not generated artifacts.

Generated packages belong under ignored `artifacts/`.

## What Gets Tracked

- GitHub Actions workflows in `.github/workflows/`.
- Linux desktop entry and launcher templates under `packaging/linux/`.
- Windows package README and shortcut helpers under `packaging/windows/`.
- Publish/package scripts under `scripts/`.
- Documentation describing the release process.

## What Does Not Get Tracked

- Published Windows folders.
- `.zip`, `.tar.gz`, and `.deb` release outputs.
- Local Dolphin folders.
- ISOs or extracted ISO contents.
- Generated CM Tool workspaces.

## Local Release Commands

Windows Colosseum Tool:

```powershell
.\scripts\publish-windows.ps1 -Tool Colosseum -Runtime win-x64
```

Windows GoD Tool:

```powershell
.\scripts\publish-windows.ps1 -Tool GoD -Runtime win-x64
```

Linux Colosseum Tool:

```powershell
.\scripts\publish-linux.ps1 -Tool Colosseum -Runtime linux-x64
```

Linux GoD Tool:

```powershell
.\scripts\publish-linux.ps1 -Tool GoD -Runtime linux-x64
```

## GitHub Releases

The `Release Packages` workflow runs when a tag matching `v*` is pushed, or
manually through GitHub Actions.

Example:

```powershell
git tag v0.1.0
git push origin v0.1.0
```

The workflow builds and uploads:

- `colosseum-tool-win-x64-<version>.zip`
- `god-tool-win-x64-<version>.zip`
- `cipher-snagem-colosseum-tool-linux-x64-<version>.deb`
- `cipher-snagem-god-tool-linux-x64-<version>.deb`
- `colosseum-tool-linux-x64-<version>.tar.gz`
- `god-tool-linux-x64-<version>.tar.gz`

The Windows packages are self-contained so normal users should not need to
install the .NET runtime separately.

Each Windows zip contains:

- the app executable and runtime files,
- `README-windows.txt`,
- `install-windows-user.ps1` for optional Start Menu/Desktop shortcuts,
- `uninstall-windows-user.ps1` for removing those per-user shortcuts/files.
