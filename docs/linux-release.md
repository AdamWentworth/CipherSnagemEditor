# Linux Release

Cipher Snagem Editor is Windows-first today, but the .NET/Avalonia app can be
published for Linux. Linux artifacts are runtime-specific rather than one binary
for every Linux machine.

Current target:

- `linux-x64`: normal 64-bit Ubuntu desktops and laptops

Future target:

- `linux-arm64`: ARM64 Linux machines, if needed

## Build

From the repo root:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\publish-linux.ps1 -Tool Colosseum
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\publish-linux.ps1 -Tool GoD
```

The script's default Debian package version is kept in
`scripts/publish-linux.ps1`; bump it when producing a new package you want
Ubuntu to treat as an upgrade.

That creates:

```text
artifacts/
  publish-colosseum-tool-linux-x64/
  publish-god-tool-linux-x64/
  packages/
    colosseum-tool-linux-x64/
    god-tool-linux-x64/
    cipher-snagem-colosseum-tool-linux-x64-0.1.12.deb
    cipher-snagem-god-tool-linux-x64-0.1.12.deb
    colosseum-tool-linux-x64.tar.gz
    god-tool-linux-x64.tar.gz
```

By default the package is self-contained, so the Ubuntu test machine should not
need a separately installed .NET runtime.

Self-contained Linux builds also use ReadyToRun by default to reduce cold-start
JIT work on Ubuntu. Use `-NoReadyToRun` if you need a smaller diagnostic build.

The `.deb` is the preferred Ubuntu artifact. Colosseum and GoD packages use
separate package names, install roots, desktop entries, and icons so both can be
installed at the same time.

For a framework-dependent build:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\publish-linux.ps1 -FrameworkDependent
```

## Ubuntu Test

Copy the versioned `.deb`, for example
`artifacts/packages/cipher-snagem-colosseum-tool-linux-x64-0.1.12.deb`, to the
Ubuntu machine, then double-click it in GNOME Files or run:

```bash
sudo apt install ./cipher-snagem-colosseum-tool-linux-x64-0.1.12.deb
```

Launch `Colosseum Tool` or `GoD Tool` from the app grid.

Portable fallback:

```bash
tar -xzf colosseum-tool-linux-x64.tar.gz
cd colosseum-tool-linux-x64
chmod +x ColosseumTool run-cipher-snagem-editor.sh install-linux-user.sh
./run-cipher-snagem-editor.sh
```

Optional per-user install:

```bash
./install-linux-user.sh
```

That installs the app under a tool-specific folder in `~/.local/share`, installs
the PNG icon under `~/.local/share/icons`, and creates a desktop entry under
`~/.local/share/applications`.

The package must contain the runtime image asset tree:

```text
assets/images/ColoTrainers/
assets/images/PokeBody/
assets/images/PokeFace/
assets/images/Types/
```

Windows-built packages may materialize that tree as `Assets/images/...` because
the app also has Avalonia resources under `Assets/`. The runtime resolver accepts
both spellings so the same package works on case-sensitive Linux filesystems.

## Validation Checklist

- App launches without terminal errors.
- Window icon appears in the app/window switcher.
- File picker can open a clean Colosseum ISO.
- Workspace folder is created beside the ISO.
- Trainer Editor opens and displays trainer/Pokemon images.
- Pokemon Stats, Move, Item, Type, Treasure, Table Editor, and ISO Explorer open.
- A small edit can be saved, imported/rebuilt, and reopened.

Linux support should stay marked as experimental until this checklist passes on
the Ubuntu machine.
