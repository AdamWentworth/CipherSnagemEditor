# Release And Packaging Notes

The source files under `packaging/` are tracked on purpose. They are packaging
recipes and desktop integration templates, not generated artifacts.

Generated packages belong under ignored `artifacts/`.

## User Downloads

GitHub Releases are the public download surface:

- `colosseum-tool-windows-portable-x64.zip`
- `god-tool-windows-portable-x64.zip`
- `colosseum-tool-ubuntu-debian-x64.deb`
- `god-tool-ubuntu-debian-x64.deb`
- `colosseum-tool-linux-portable-x64.tar.gz`
- `god-tool-linux-portable-x64.tar.gz`

Windows users should choose the zip for the tool they want. Ubuntu/Debian users
should prefer the `.deb` files. Other Linux users can use the portable `.tar.gz`
archives.

The GitHub Packages tab is not used for app downloads. Releases are the right
place for both installer-style downloads and portable archives; Packages is for
developer package registries such as NuGet or containers.

## What Gets Tracked

- GitHub Actions workflows in `.github/workflows/`.
- Linux desktop entry and launcher templates under `packaging/linux/`.
- Windows package README and shortcut helpers under `packaging/windows/`.
- Publish/package scripts under `scripts/`, including Windows PowerShell
  scripts, Linux bash scripts, and the Python `.deb` helper.
- Documentation describing the release process.

## What Does Not Get Tracked

- Published Windows folders.
- `.zip`, `.tar.gz`, and `.deb` release outputs.
- Local Dolphin folders.
- ISOs or extracted ISO contents.
- Generated CM Tool workspaces.

## Local Release Commands

Use the script that matches the platform doing the packaging. Linux maintainers
should not need PowerShell just to produce Linux artifacts.

Windows Colosseum Tool:

```powershell
.\scripts\publish-windows.ps1 -Tool Colosseum -Runtime win-x64
```

Windows GoD Tool:

```powershell
.\scripts\publish-windows.ps1 -Tool GoD -Runtime win-x64
```

Linux Colosseum Tool:

```bash
bash scripts/publish-linux.sh Colosseum linux-x64
```

Linux GoD Tool:

```bash
bash scripts/publish-linux.sh GoD linux-x64
```

From Windows, maintainers can also cross-publish Linux artifacts with
PowerShell:

```powershell
.\scripts\publish-linux.ps1 -Tool Colosseum -Runtime linux-x64
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

The workflow builds and uploads the files listed in [User Downloads](#user-downloads).

The Windows packages are self-contained so normal users should not need to
install the .NET runtime separately.

Each Windows zip contains:

- the app executable and runtime files,
- `README-windows.txt`,
- `install-windows-user.ps1` for optional Start Menu/Desktop shortcuts,
- `uninstall-windows-user.ps1` for removing those per-user shortcuts/files.

## Repository Metadata

Suggested GitHub About description:

```text
Windows/Linux remake of StarsMMD's Pokemon Colosseum and Pokemon XD GoD Tool editors, built with .NET and Avalonia.
```

Suggested topics:

```text
pokemon, pokemon-colosseum, pokemon-xd, gale-of-darkness, gamecube, modding,
romhacking, dotnet, avalonia, windows, linux, god-tool, colosseum-tool,
pokemon-xd-code
```
