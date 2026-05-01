# XD Swift Parity Audit

The XD/GoD Tool target is the legacy Pokemon XD path from `Pokemon-XD-Code`.
The Windows/Linux implementation shares the same app shell and editor surface as
the Colosseum Tool, but ships as a separate GoD Tool release.

## Closeout Probe

`scripts/run-xd-closeout.ps1` copies the ignored clean XD ISO fixture and runs:

- XD ISO open/workspace checks.
- XD editor parser checks for trainer, Pokemon stats, moves, TM/HM rows, items,
  Pokespots, gift Pokemon, message tables, types, treasures, interactions, and
  shadow Pokemon.
- Editor save/reopen checks for trainer Pokemon, Pokemon stats, moves, items,
  types, treasures, gift Pokemon, shadow Pokemon, Pokespots, interactions, and
  messages.
- Trainer Editor data resolution checks for battle metadata and party species.
- Representative patch apply/reopen check using the max catch-rate patch.
- Randomizer write/reopen check for common data, TM/HM, treasure, and type paths.
- ISO Explorer export/import/add/delete smoke.
- Script codec export/import/reopen smoke for standalone `.scd` scripts and
  embedded `common.rel` TCOD scripts.

Run it from the repo root:

```powershell
.\scripts\run-xd-closeout.ps1
```

The probe mutates only the copied ISO under `.local\closeout-work`.

## Script Sweep

`scripts/run-xd-script-sweep.ps1` runs an in-memory compiler/decompiler sweep
against the clean XD ISO fixture:

- walks XD `.fsys` archives,
- decompiles standalone `.scd` scripts,
- decompiles embedded TCOD scripts in `.rel` containers such as `common.rel`,
- compiles the generated `.xds` back to SCD bytes,
- verifies the compiled output still parses,
- reports byte-identical output separately from rebuilt-but-parseable output.

Run the full sweep:

```powershell
.\scripts\run-xd-script-sweep.ps1
```

Run a smaller development sample:

```powershell
.\scripts\run-xd-script-sweep.ps1 -Limit 25
```

Run the strict parity proof:

```powershell
.\scripts\run-xd-script-sweep.ps1 -StrictByteMatch
```

The current strict sweep checks all 235 XD scripts from the clean ISO fixture and
requires byte-identical compile output for every generated `.xds` file.

## Patch Matrix

`scripts/run-xd-patch-matrix.ps1` copies the ignored clean XD ISO fixture once per
patch, applies the patch, reopens the ISO, and verifies that the shared XD editor
tables still parse. It also checks direct table effects where we can prove them
without Dolphin, including:

- physical/special split move category data,
- trade and item evolution rewrites,
- any-Pokemon-can-learn-any-TM flags,
- max catch-rate writes for base and shadow Pokemon,
- all-single and all-double battle style rewrites.

For DOL/ASM patches, the matrix now also performs direct `Start.dol` state
readback. Most runtime patches are verified by applying the same patcher to an
in-memory clone and requiring byte-identical output; the Gen 6 critical-hit
multiplier patch is verified by checking its patched entry branch and allocated
free-space routine.

Run the full matrix:

```powershell
.\scripts\run-xd-patch-matrix.ps1
```

Run one patch while developing:

```powershell
.\scripts\run-xd-patch-matrix.ps1 -Patch PokemonHaveMaxCatchRate
```

Successful patch ISO copies are deleted by default so the matrix does not keep
dozens of full disc images. Use `-KeepIsos` when a specific run needs preserved
outputs.

## Dolphin Runtime Smoke Matrix

`scripts/run-xd-smoke-matrix.ps1` is the runtime side of the XD closeout. It
copies the clean XD ISO fixture per case, applies the requested XD smoke
operation, and boots the result through Dolphin with muted audio:

- `AudioBackend = No audio output`
- `AudioVolume = 0`
- `VideoBackend = Null` by default for headless smoke runs

The default case list keeps the runtime run manageable while covering clean boot,
representative DOL/ASM patches, an editor write, and the script codec path:

```powershell
.\scripts\run-xd-smoke-matrix.ps1
```

Run a quick local proof without launching Dolphin:

```powershell
.\scripts\run-xd-smoke-matrix.ps1 -Cases clean-boot,patch-disable-save-corruption -SkipDolphin
```

Run the full patch boot sweep when we want maximum runtime evidence:

```powershell
.\scripts\run-xd-smoke-matrix.ps1 -PatchSweep
```

Each case writes a copied ISO under `.local\xd-smoke-work` and Dolphin logs under
`artifacts\xd-dolphin-smoke`.

## Known Gaps

- Script codec coverage now includes full strict byte-identical recompilation for
  all 235 XD scripts in the clean ISO fixture. Remaining script work is focused
  on expanding the editable high-level language, not preserving unedited bytes.
- Patch matrix coverage proves application/reopen stability and table-visible
  effects, plus direct `Start.dol` byte-state readback for DOL/ASM patches. The
  Dolphin runtime smoke matrix proves patched ISOs boot without obvious emulator
  crash/log failure. Exact in-battle behavior for runtime-only assembly patches
  still needs manual Dolphin checks or a deeper save-state/DTM automation layer.
