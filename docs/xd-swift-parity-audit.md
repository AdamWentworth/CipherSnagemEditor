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

## Known Gaps

- Script codec coverage is strong enough for smoke confidence, but not yet a
  full every-script sweep.
- Runtime patch proof still ultimately needs manual Dolphin checks or a deeper
  save-state/DTM automation layer.
