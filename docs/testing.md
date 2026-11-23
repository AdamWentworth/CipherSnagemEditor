# Testing

## Basic Checks

Install the .NET 10 SDK, then build and run the normal test suite:

```powershell
dotnet build CipherSnagemEditor.slnx
dotnet test CipherSnagemEditor.slnx --no-build
```

These checks do not require game files. Additional parity and smoke tests can be
run locally when clean ISO fixtures and Dolphin are available.

## Local Fixture Layout

Recommended local layout:

```text
.local/
  fixtures/
    Pokemon Colosseum.iso
    Pokemon XD - Gale of Darkness.iso
  dolphin-user/
```

Point `DOLPHIN_EXE` at the shared Dolphin installation. On Adam's development
machine that is:

```powershell
$env:DOLPHIN_EXE = "D:\DevTools\Emulators\Dolphin\Dolphin.exe"
```

Passing `-DolphinExe` to a smoke script still overrides the environment value.
If neither is supplied, the script retains a `.local\dolphin\Dolphin.exe`
fallback for self-contained checkouts.

## Colosseum Checks

Run the main closeout probe:

```powershell
.\scripts\run-colosseum-closeout.ps1
```

Run rebuild/runtime smoke checks through Dolphin:

```powershell
.\scripts\run-colosseum-smoke-matrix.ps1
```

Run parser/codec probes against real assets from the clean ISO:

```powershell
.\scripts\run-colosseum-parity-probes.ps1
```

### Rebuild Parity

Export and reimport the Colosseum party menu, battle menus, and common data
with no edits.
Start with a verified stock ISO. The probe checks the original types for Ice
Punch, Leer, and Sandstorm before repacking. It needs space for one ISO in the
system temp directory and compares the entire disc after the rebuild.

```powershell
dotnet run --project src/CipherSnagemEditor.Cli -- colosseum-rebuild-probe ".local/fixtures/Pokemon Colosseum.iso"
```

To keep a rebuilt ISO for playtesting, pass a new output path as the last
argument. The probe saves it after parity passes.

To check an edited Colosseum or XD rebuild against GoD Tool, start both tools
from the same clean disc and apply the same edits. Compare the two resulting
images with GoD Tool's output as the expected image.

```powershell
dotnet run --project src/CipherSnagemEditor.Cli -- compare-rebuild ".local/god-tool.iso" ".local/cipher-rebuilt.iso"
```

The comparison reads both images and checks the boot data, apploader,
executable, file paths, and every file's bytes. Disc file offsets and padding
can differ. A changed FSYS archive is reported by member name and ID when its
contents differ. Different archive packaging also fails the strict check,
even if the decoded member data matches. The basic test suite exercises the
comparison with synthetic fixtures. Keep actual game images under `.local/`.

## Pokemon XD Checks

Run the main XD closeout probe:

```powershell
.\scripts\run-xd-closeout.ps1
```

Run the XD script compiler/decompiler sweep:

```powershell
.\scripts\run-xd-script-sweep.ps1 -StrictByteMatch
```

Run XD patch verification:

```powershell
.\scripts\run-xd-patch-matrix.ps1
```

Run XD Dolphin smoke checks:

```powershell
.\scripts\run-xd-smoke-matrix.ps1
```

## Dolphin Notes

Dolphin smoke tests are boot/crash checks. They are useful for proving that a
rebuilt ISO still starts, but they do not prove every gameplay branch or patch
effect. Deep gameplay verification still requires manual testing or future
save-state/DTM automation.

The smoke scripts configure Dolphin with muted audio for automated runs.
