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
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\publish-linux.ps1
```

That creates:

```text
artifacts/
  publish-linux-x64/
  packages/
    cipher-snagem-editor-linux-x64/
    cipher-snagem-editor-linux-x64.tar.gz
```

By default the package is self-contained, so the Ubuntu test machine should not
need a separately installed .NET runtime.

For a framework-dependent build:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\publish-linux.ps1 -FrameworkDependent
```

## Ubuntu Test

Copy `artifacts/packages/cipher-snagem-editor-linux-x64.tar.gz` to the Ubuntu
machine, then:

```bash
tar -xzf cipher-snagem-editor-linux-x64.tar.gz
cd cipher-snagem-editor-linux-x64
chmod +x CipherSnagemEditor.App run-cipher-snagem-editor.sh install-linux-user.sh
./run-cipher-snagem-editor.sh
```

Optional per-user install:

```bash
./install-linux-user.sh
```

That installs the app under `~/.local/share/cipher-snagem-editor`, installs the
PNG icon under `~/.local/share/icons`, and creates a desktop entry under
`~/.local/share/applications`.

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
