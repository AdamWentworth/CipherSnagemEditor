# Dolphin Smoke Testing

Local Dolphin and ISO fixtures live under `.local/`, which is gitignored. Do not commit emulator binaries, ISOs, memory cards, save states, or generated Dolphin user data.

Current local layout:

```text
.local/
  dolphin/Dolphin-x64/Dolphin.exe
  dolphin-user/
  fixtures/Pokemon Colosseum.iso
artifacts/dolphin-smoke/
```

Run a basic boot/crash smoke test:

```powershell
.\scripts\run-dolphin-smoke.ps1
```

The runner launches Dolphin in batch mode with an isolated user folder, the Null video backend, no frame limit, and muted audio. It writes `Config/DSP.ini` with `Backend = No audio output` and `Volume = 0`, then passes the same values through Dolphin's command-line config overrides. If Dolphin stays alive until the timeout, the smoke test passes. If it exits too early, returns a nonzero exit code, or writes known failure patterns to the logs, the smoke test fails.

Useful overrides:

```powershell
.\scripts\run-dolphin-smoke.ps1 `
  -DolphinExe "D:\Tools\Dolphin\Dolphin.exe" `
  -IsoPath "D:\Temp\patched-colosseum.iso" `
  -Seconds 90 `
  -AudioVolume 0
```

This is a crash/boot smoke layer, not full gameplay verification. Deeper parity tests should add save states or DTM input recordings that exercise specific patched behavior.

Run the Colosseum rebuild matrix:

```powershell
.\scripts\run-colosseum-smoke-matrix.ps1
```

The matrix copies the clean ISO into `.local/smoke-work/`, applies selected CLI mutations to each copy, imports the changed workspace files back into the ISO, then runs the Dolphin smoke check against the result. The default cases cover:

- `clean-boot`: untouched fixture boot
- `patch-disable-save-corruption`: `Start.dol` patch import
- `editor-move`: editor-style `common.fsys` rebuild/import
- `randomizer-species`: combined gift/trainer species randomization, `Start.dol`, and `common.fsys` growth
- `randomizer-shops`: `pocket_menu.fsys` rebuild/import plus `Start.dol` shop script updates

To broaden Start.dol patch coverage, add `-PatchSweep`. This appends the
branch/ASM-heavy Colosseum patches to the matrix, including physical/special
split, soft reset, PC from anywhere, reusable TMs, critical-hit patches, debug
logs, locked-shadow-move type icon removal, and colbtl region unlocking.

Useful targeted run:

```powershell
.\scripts\run-colosseum-smoke-matrix.ps1 `
  -Cases clean-boot,editor-move `
  -Seconds 20 `
  -MinimumSeconds 5
```

Useful patch sweep run:

```powershell
.\scripts\run-colosseum-smoke-matrix.ps1 `
  -PatchSweep `
  -Seconds 20 `
  -MinimumSeconds 5
```

The CLI smoke operation is also callable directly for ad hoc copied ISOs:

```powershell
dotnet run --project .\src\CipherSnagemEditor.Cli\CipherSnagemEditor.Cli.csproj -- `
  smoke-apply "D:\Temp\Pokemon Colosseum - test.iso" editor-move
```

Run local parity probes against real ISO assets:

```powershell
.\scripts\run-colosseum-parity-probes.ps1
```

The probe walks the ignored clean ISO fixture directly and validates semantic
message-table rebuilds, collision parser coverage, and WZX/DAT vertex-color
model parsing without committing any game data. Useful overrides:

```powershell
.\scripts\run-colosseum-parity-probes.ps1 `
  -Messages 100 `
  -Assets 100 `
  -NoBuild
```
