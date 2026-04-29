# Colosseum Swift Parity Audit

Audited against the local Swift source at `D:\Projects\Pokemon-XD-Code` on 2026-04-29.

The target for the first app mode is the legacy Colosseum Tool path from `Pokemon-XD-Code`, not the newer XD/Goddess work. XD and PBR screens remain future modes unless the same source files are shared by the Colosseum Tool.

## Source Menu

The Swift Colosseum home menu is defined in `GoDHomeViewController.swift` as:

1. Trainer Editor
2. Pokemon Stats Editor
3. Move Editor
4. Item Editor
5. Gift Pokemon Editor
6. Type Editor
7. Treasure Editor
8. Patches
9. Randomizer
10. Message Editor
11. Collision Viewer
12. Interaction Editor
13. Vertex Filters
14. Table Editor
15. ISO Explorer

`ColosseumToolCatalog` matches this order and maps each entry to the corresponding Swift segue/source controller.

## Current Parity Status

| Tool | Status | Notes |
| --- | --- | --- |
| Home/Menu | Parity with Windows convention | Menu order, colors, tool-cell asset, window spawning, and app mode are aligned. The window buttons intentionally use Windows placement/behavior. |
| Trainer Editor | Near parity | UI layout, colors, trainer search, trainer list, Pokemon cards, dropdown indicators, editable fields, animated Pokemon bodies, and save flow are in place. User has verified edits rebuild into a Dolphin-runnable ISO. The closeout probe now verifies trainer Pokemon save/import/reopen against a copied clean ISO. |
| Pokemon Stats Editor | Near parity | Swift-style list, type backgrounds, core stat/detail panels, moves, evolutions, EVs, and save flow are present. The closeout probe now verifies Pokemon stat save/import/reopen against a copied clean ISO. Needs broad spot-checking across all species after any field offset changes. |
| Move Editor | Near parity | Swift-style list/details, effect labels, animation labels, blue checkboxes, and save flow are present. Category editing now follows Swift: disabled unless the physical/special split patch is detected in `Start.dol`. The closeout probe now verifies move save/import/reopen against a copied clean ISO. |
| Item Editor | Near parity | User visually reviewed as strong parity. Saves Start.dol-backed item data. The closeout probe now verifies item save/import/reopen against a copied clean ISO. |
| Gift Pokemon Editor | Near parity | Starter Espeon/Umbreon and Agate/Colosseum gift naming were corrected from the Swift manager mapping. Saves Start.dol-backed gift data. The closeout probe now verifies a gift Pokemon save/import/reopen against a copied clean ISO. |
| Type Editor | Near parity | Swift-style category radio buttons and labeled matchup grid are present. Saves Start.dol-backed type/matchup data. The closeout probe now verifies type matchup save/import/reopen against a copied clean ISO. |
| Treasure Editor | Near parity | Swift-style room list and detail layout are present. Treasure room/name resolution was corrected for blank entries. Saves common.rel data. The closeout probe now verifies treasure save/import/reopen against a copied clean ISO. |
| Patches | Catalog parity, backend parity with regression coverage | The Colosseum patch list matches `XGPatcher.swift`, the PowerPC helper encodings used by ASM patches have regression tests, the patch sweep applies/imports the branch-heavy patches against copied ISOs, Dolphin boot smoke covers representative patched `Start.dol` paths, and the closeout probe verifies physical/special split detection after save/import/reopen. |
| Randomizer | Backend parity pass complete for Colosseum-visible options | Colosseum-visible options match the Swift controller. The backend now follows the Swift option buckets more closely, including Type 9 exclusion while it is `???`, evolution-line duplicate avoidance, all-randomized-trainer happiness, Colosseum shop item script IDs, skipping blank move row `0` during Move Type randomization, and snapshotting TM rows before writing randomized TM moves. The closeout probe verifies randomizer writes/imports/reopens common.rel, Start.dol, and pocket_menu.rel paths. |
| Message Editor | Functional parity target | Message table loading/editing/saving exists. Swift-style special tokens preserve payload bytes such as `[Pause]{1e}` and `[Spec Colour]{01020304}`. Parsed tables now preserve their original byte length when replacements fit, refuse fixed-size overflows when growth is disabled, and JSON imports update listed strings without dropping omitted strings. The local parity probe round-trips real extracted ISO message tables semantically. The window layout, row text, selection behavior, editable/disabled state, and ID search now track the Swift controller more closely. |
| Collision Viewer | Visual parity target | Window exists and mirrors the legacy purpose. The parser now has regression coverage for the Swift collision table layout, interactable regions, section indexes, normals, and coordinate scaling. The local parity probe parses real collision files from the clean ISO. The extra non-Swift status overlay was removed and the picker placement now matches the Metal view more closely. Real visual spot checks are still useful. |
| Interaction Editor | Near parity | Swift-style list/detail pass is done and saves common.rel interaction data. Needs broad room/script spot checks. |
| Vertex Filters | Functional parity target | Lists exported `wzx.dat`-style model files and applies vertex color filters. Swift filter formulas and no-op behavior are covered by tests, and the local parity probe parses real WZX/DAT model assets from the clean ISO. The preview grid now follows Swift's hue/saturation/value/alpha sorting and square-root profile sizing. Full visual parity should still be judged from exported model fixtures. |
| Table Editor | Parity for Swift capability | Swift has Decode, Encode, and Document; its Edit button is empty. Editable raw tables are therefore not required for Colosseum parity unless we intentionally exceed the Swift tool later. |
| ISO Explorer | Strong backend parity | Export/decode, import/encode, delete, Add File for FSYS archives, FSYS repack, `.msg.json`, LZSS, texture/model helpers, THP-style split/combine, shifting, GID display, FSYS entry identifiers, and copied-ISO Dolphin smoke coverage for rebuilt imports are covered. |

## Closeout Probe

`scripts/run-colosseum-closeout.ps1` copies the ignored clean ISO fixture and runs:

- priority editor save/import/reopen checks for Trainer, Pokemon Stats, Move, Item, Type, Treasure, and Gift Pokemon
- representative patch apply/import/reopen for the physical/special split path
- randomizer write/import/reopen for common.rel, Start.dol, and pocket_menu.rel paths
- message, collision, and vertex/model parity probes against the mutated copied ISO

The latest local run on 2026-04-29 passed against `GC6E`, including 75 message tables, 75 collision files, and 75 WZX probes.

## Highest Confidence Remaining Gaps

1. Deeper patch behavior tests: the smoke matrix and closeout probe confirm patched ISOs rebuild and boot/parse, but patches that inject branches still need gameplay/save-state or DTM input verification to prove their runtime effect.
2. Randomizer determinism: behavior now tracks the Swift buckets and survives real ISO import/reopen, but deterministic fixture tests would catch regressions in exact table writes if we later make the RNG injectable.
3. Message Editor text-control polish: backend real-file rebuild probes pass, but the window itself still deserves manual spot checks for multiline/special-token editing.
4. Collision and Vertex Filters: parser probes run on real assets, but final confidence still requires visual spot checks because correctness is partly visual.

## Intentional Non-Gaps

- XD-only screens, Pokespot tools, Battle Bingo, and PBR tooling are not Colosseum Tool parity requirements yet.
- The Table Editor does not need general in-app cell editing for parity because the Swift edit action is not implemented.
- macOS traffic-light window controls are not a parity target; the user chose native Windows-style window controls.
