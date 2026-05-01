# Cipher Snagem Editor

![.NET](https://img.shields.io/badge/.NET-10-512BD4)
![Avalonia](https://img.shields.io/badge/UI-Avalonia-8B44AC)
![License](https://img.shields.io/badge/license-GPL--2.0--only-blue)
![Platform](https://img.shields.io/badge/platform-Windows%20%7C%20Linux-lightgrey)

Cipher Snagem Editor is a Windows-first, cross-platform .NET/Avalonia remake of
the legacy Pokemon Colosseum and Pokemon XD: Gale of Darkness modding tools from
the `Pokemon-XD-Code` project.

The repository contains one shared codebase and two desktop release targets:

- `Colosseum Tool` for Pokemon Colosseum.
- `GoD Tool` for Pokemon XD: Gale of Darkness.

The goal of this repository is preservation and practical parity with the
original Swift/macOS tools: familiar editor windows, equivalent data behavior,
safe ISO workspace flows, and repeatable rebuilds on modern Windows and Linux.

## ⬇️ Download

Most users should download a prebuilt package from the
[GitHub Releases page](https://github.com/AdamWentworth/CipherSnagemEditor/releases)
instead of cloning the source.

Recommended downloads:

- Windows Colosseum: `colosseum-tool-windows-portable-x64.zip`
- Windows XD/GoD: `god-tool-windows-portable-x64.zip`
- Ubuntu/Debian Colosseum: `colosseum-tool-ubuntu-debian-x64.deb`
- Ubuntu/Debian XD/GoD: `god-tool-ubuntu-debian-x64.deb`

Optional portable Linux archives are also attached for non-Debian systems:

- Linux Colosseum: `colosseum-tool-linux-portable-x64.tar.gz`
- Linux XD/GoD: `god-tool-linux-portable-x64.tar.gz`

Windows zips are self-contained: extract the zip and run `ColosseumTool.exe` or
`GoDTool.exe`. Each zip also includes an optional per-user shortcut installer.

GitHub's **Packages** tab is not used for app downloads. Releases are the right
place for both installer-style downloads and portable archives; Packages is for
developer package registries such as NuGet or containers.

## ✅ Status

This repo is treated as the stable legacy-editor parity line.

- Colosseum Tool: priority editor, patch, randomizer, table, ISO Explorer, and
  helper codec parity are implemented and covered by tests/probes.
- GoD Tool: XD editor parity is implemented for the legacy editor surface,
  including Shadow Pokemon, Pokespot, patch/randomizer, ISO Explorer, and XD
  script codec workflows.
- Windows x64 is the primary release target.
- Linux x64 packages are supported experimentally with a `.deb` and portable
  archive.
- macOS packaging is not the current focus, though the Avalonia codebase is
  intended to remain portable where practical.

## 🌟 Original Work And Credit

This project exists because of the original **Gale of Darkness Tool** and
**Colosseum Tool** created by **Stars Momodu** / **@StarsMMD**.

The legacy project identifies the tools as:

- `GoD Tool` for Pokemon XD: Gale of Darkness
- `Colosseum Tool` for Pokemon Colosseum
- source repository: `https://github.com/PekanMmd/Pokemon-XD-Code.git`

Cipher Snagem Editor is not an attempt to erase or rebrand that work. It is a
Windows and cross-platform C# remake/fork effort built by studying the Swift
source, UI storyboards, data parsers, binary formats, and behavior of StarsMMD's
original tools. The intent is to preserve that workflow for users who cannot or
do not want to depend on the original macOS app.

## 🧰 What This Editor Does

Core workflows:

- Open Pokemon Colosseum and Pokemon XD ISO/GCM images.
- Create and reuse the legacy sibling workspace folder beside the opened ISO.
- Detect supported GameCube regions.
- Extract, edit, import, and rebuild game files through the ISO Explorer.
- Decode/repack FSYS archives and LZSS-compressed entries.
- Decode/re-encode message tables.
- Export/import supported texture containers.
- Export/import supported PKX, WZX, DAT, GSW, and THP-style helper formats.
- Apply supported patches and randomizer writes.
- Save editor changes back into the workspace and rebuild them into the ISO.

Colosseum Tool editor surface:

- Trainer Editor
- Pokemon Stats Editor
- Move Editor
- Item Editor
- Gift Pokemon Editor
- Type Editor
- Treasure Editor
- Patches
- Randomizer
- Message Editor
- Collision Viewer
- Interaction Editor
- Vertex Filters
- Table Editor
- ISO Explorer

GoD Tool editor surface:

- Trainer Editor
- Pokemon Stats Editor
- Move Editor
- Item Editor
- Gift Pokemon Editor
- Type Editor
- Treasure Editor
- Patches
- Randomizer
- Message Editor
- Interaction Editor
- Shadow Pokemon Editor
- Pokespot Editor
- Script compiler/decompiler workflows
- Table Editor
- ISO Explorer

## 🧪 What This Editor Is Not

This repository is not a general Pokemon asset-authoring suite. It intentionally
does not try to own every future model, VFX, Blender, map, music, and audio
workflow inside the stable editor codebase.

Those experimental workflows should live outside the stable editor until they
are mature enough for normal users.

This repository is also not a Nintendo, Genius Sonority, or Pokemon asset dump.
No game files are included or required in source control.

## 📁 Repository Layout

```text
CipherSnagemEditor.slnx
Directory.Build.props
src/
  CipherSnagemEditor.App/             shared Avalonia desktop UI
  CipherSnagemEditor.ColosseumTool/   Colosseum Tool executable launcher
  CipherSnagemEditor.GoDTool/         GoD Tool executable launcher
  CipherSnagemEditor.Core/            binary, ISO, FSYS, script, text, shared logic
  CipherSnagemEditor.Colosseum/       Colosseum-specific tables and models
  CipherSnagemEditor.XD/              XD-specific tables, patches, scripts, models
  CipherSnagemEditor.Cli/             command-line inspection/test harness
tests/
  CipherSnagemEditor.Tests.csproj     unit and parity-focused test project
  *Tests.cs                           unit and parity-focused tests
assets/
  images/                             source-provided Pokemon/trainer/type art
  json/                               source-provided lookup data
  ui/                                 source-provided UI art from the original tools
docs/
  *.md                                scope, testing, and packaging notes
scripts/
  *.ps1                               build, closeout, script, and smoke probes
packaging/
  linux/                              Linux desktop/package helper files
  windows/                            Windows release README and shortcut helpers
```

Packaging-only app assets live under `src/CipherSnagemEditor.App/Assets` or the
specific launcher project that needs them. Reusable legacy art belongs under the
root `assets/` tree and is linked into the app bundle.

Ignored local folders:

- `.local/` for clean ISO fixtures, copied test ISOs, Dolphin sandboxes, and
  other private local data.
- `artifacts/` for publish output, packages, logs, screenshots, and temporary
  release files.
- `bin/` and `obj/` for .NET build output.

## 🛠️ Development Setup

Install the .NET 10 SDK, then verify:

```powershell
dotnet --info
```

Build:

```powershell
dotnet build CipherSnagemEditor.slnx
```

Test:

```powershell
dotnet test CipherSnagemEditor.slnx --no-build
```

## 🔒 Local Fixtures

Automated parity probes expect private clean ISO fixtures under `.local/fixtures`
when you want full coverage:

```text
.local/
  fixtures/
    Pokemon Colosseum.iso
    Pokemon XD - Gale of Darkness.iso
```

These files are intentionally ignored and must never be committed.

## 🧾 Verification Commands

Colosseum closeout:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\run-colosseum-closeout.ps1
```

Colosseum Dolphin smoke matrix:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\run-colosseum-smoke-matrix.ps1
```

XD closeout:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\run-xd-closeout.ps1
```

XD strict script recompilation sweep:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\run-xd-script-sweep.ps1 -StrictByteMatch
```

XD patch matrix:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\run-xd-patch-matrix.ps1
```

XD Dolphin smoke matrix:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\run-xd-smoke-matrix.ps1
```

## 📦 Publishing

GitHub releases can provide ready-to-use Windows and Linux packages. The
packaging recipes are tracked in this repo; generated packages are ignored under
`artifacts/`.

See `docs/release-packaging.md` for the release workflow and local packaging
commands.

Publish a Windows Colosseum Tool build:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\publish-windows.ps1 -Tool Colosseum
```

Publish a Windows GoD Tool build:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\publish-windows.ps1 -Tool GoD
```

Publish Linux x64 packages:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\publish-linux.ps1
```

## 📚 Documentation

- `docs/scope.md` describes supported editor workflows and what is out of scope.
- `docs/testing.md` explains the normal test suite and optional local ISO/Dolphin
  smoke checks.
- `docs/release-packaging.md` explains tracked packaging files, ignored release
  artifacts, and GitHub release builds.

## ⚖️ Legal And Data Hygiene

No Nintendo, Genius Sonority, or Pokemon game files belong in this repository.
Do not commit ISOs, extracted ISO contents, save files, generated CM Tool
workspaces, Dolphin user folders, or local reference dumps.

This repository carries GPL-2.0-only licensing metadata to remain compatible
with the legacy source. See `LICENSE`.

Pokemon, Pokemon Colosseum, Pokemon XD: Gale of Darkness, and related names are
owned by their respective rights holders. This is an unofficial fan modding
tooling project.
