# Cipher Snagem Editor

Cipher Snagem Editor is a Windows-first, cross-platform .NET/Avalonia remake of
the legacy Pokemon Colosseum and Pokemon XD modding tools from the
`Pokemon-XD-Code` project.

The first milestone is 1:1 Colosseum Tool parity: same editor windows, same
data model behavior, same ISO workspace flow, and the same practical workflows
that made the original macOS Swift app useful. XD support is planned as a
future branch of the same app once the Colosseum path is solid.

## Original Work And Credit

This project exists because of the original **Gale of Darkness Tool** and
**Colosseum Tool** created by **Stars Momodu** / **@StarsMmd**.

The legacy source identifies the tools as:

- `GoD Tool` for Pokemon XD: Gale of Darkness
- `Colosseum Tool` for Pokemon Colosseum
- source code: `https://github.com/PekanMmd/Pokemon-XD-Code.git`

Cipher Snagem Editor is not an attempt to erase or rebrand that work. It is a
Windows and cross-platform C# remake/fork effort built from studying the Swift
code, UI storyboards, data parsers, and behavior of StarsMmd's original tools.
The goal is to preserve that workflow on modern Windows while gradually making
the codebase easier to maintain and extend.

## Current Scope

Current target:

- Pokemon Colosseum
- Windows desktop
- .NET 10
- Avalonia UI
- 1:1 parity with the Swift Colosseum Tool where practical

Implemented or in progress:

- ISO loading and region detection
- CM Tool workspace creation
- Main tool menu
- Trainer Editor
- Pokemon Stats Editor
- Move Editor
- Item Editor
- Gift Pokemon Editor
- Type Editor
- Treasure Editor
- Interaction Editor
- Table Editor
- ISO Explorer

Future scope:

- Pokemon XD: Gale of Darkness support
- deeper patcher/randomizer parity
- cross-platform packaging for macOS and Linux

## Repository Layout

```text
CipherSnagemEditor.slnx
src/
  CipherSnagemEditor.App/         Avalonia desktop UI
  CipherSnagemEditor.Core/        binary, ISO, FSYS, text, and shared logic
  CipherSnagemEditor.Colosseum/   Colosseum-specific tables and models
  CipherSnagemEditor.Cli/         command-line inspection/test harness
tests/
  CipherSnagemEditor.Tests/       unit and parity-focused tests
```

Local-only folders:

- `assets/` is ignored and used for local reference assets copied from the
  legacy tool or extracted during parity work.
- `artifacts/` is ignored and used for local publish builds, screenshots, and
  temporary outputs.

Tracked app assets belong under `src/CipherSnagemEditor.App/Assets`.

## Development Setup

Install the .NET 10 SDK from Microsoft, then verify:

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

Publish a Windows build:

```powershell
dotnet publish src\CipherSnagemEditor.App\CipherSnagemEditor.App.csproj -c Release -r win-x64 --self-contained false -o artifacts\publish-win-x64
```

## Legal And Data Hygiene

No Nintendo, Genius Sonority, or Pokemon game files belong in this repository.
Do not commit ISOs, extracted ISO contents, save files, generated CM Tool
workspaces, or local reference asset dumps.

This repository carries GPL-2.0-only licensing metadata to remain compatible
with the legacy source. See `LICENSE`.

Pokemon, Pokemon Colosseum, Pokemon XD: Gale of Darkness, and related names are
owned by their respective rights holders. This is an unofficial fan modding
tooling project.
