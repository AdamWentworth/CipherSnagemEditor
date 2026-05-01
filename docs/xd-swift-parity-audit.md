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

`-StrictByteMatch` is available when we intentionally want to fail on any
non-identical rebuild, but normal parity work treats rebuilt parseable scripts as
useful evidence rather than a failure.

## Known Gaps

- Script codec coverage is now broad enough to catch decode/compile regressions,
  but byte-identical high-level recompilation is not required yet.
- Runtime patch proof still ultimately needs manual Dolphin checks or a deeper
  save-state/DTM automation layer.
