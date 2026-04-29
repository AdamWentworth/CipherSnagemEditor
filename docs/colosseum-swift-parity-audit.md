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
| Trainer Editor | Near parity | UI layout, colors, trainer search, trainer list, Pokemon cards, dropdown indicators, editable fields, animated Pokemon bodies, and save flow are in place. User has verified edits rebuild into a Dolphin-runnable ISO. |
| Pokemon Stats Editor | Near parity | Swift-style list, type backgrounds, core stat/detail panels, moves, evolutions, EVs, and save flow are present. Needs broad spot-checking across all species after any field offset changes. |
| Move Editor | Near parity | Swift-style list/details, effect labels, animation labels, blue checkboxes, and save flow are present. Category editing now follows Swift: disabled unless the physical/special split patch is detected in `Start.dol`. |
| Item Editor | Near parity | User visually reviewed as strong parity. Saves Start.dol-backed item data. |
| Gift Pokemon Editor | Near parity | Starter Espeon/Umbreon and Agate/Colosseum gift naming were corrected from the Swift manager mapping. Saves Start.dol-backed gift data. |
| Type Editor | Near parity | Swift-style category radio buttons and labeled matchup grid are present. Saves Start.dol-backed type/matchup data. |
| Treasure Editor | Near parity | Swift-style room list and detail layout are present. Treasure room/name resolution was corrected for blank entries. Saves common.rel data. |
| Patches | Catalog parity, backend parity with regression coverage | The Colosseum patch list matches `XGPatcher.swift`, the PowerPC helper encodings used by ASM patches have regression tests, and the copied-ISO Dolphin smoke matrix covers at least one patched `Start.dol` boot path. |
| Randomizer | Backend parity pass complete for Colosseum-visible options | Colosseum-visible options match the Swift controller. The backend now follows the Swift option buckets more closely, including Type 9 exclusion while it is `???`, evolution-line duplicate avoidance, all-randomized-trainer happiness, and Colosseum shop item script IDs. |
| Message Editor | Functional parity target | Message table loading/editing/saving exists. Needs deeper comparison of text control behavior, special characters, and fixed-size table edge cases. |
| Collision Viewer | Visual parity target | Window exists and mirrors the legacy purpose, but collision rendering/import behavior needs source-by-source comparison before calling it complete. |
| Interaction Editor | Near parity | Swift-style list/detail pass is done and saves common.rel interaction data. Needs broad room/script spot checks. |
| Vertex Filters | Functional parity target | Lists exported `wzx.dat`-style model files and applies vertex color filters. Swift source itself is limited and file-dependent, so parity should be judged from exported model fixtures. |
| Table Editor | Parity for Swift capability | Swift has Decode, Encode, and Document; its Edit button is empty. Editable raw tables are therefore not required for Colosseum parity unless we intentionally exceed the Swift tool later. |
| ISO Explorer | Strong backend parity | Export/decode, import/encode, delete, Add File for FSYS archives, FSYS repack, `.msg.json`, LZSS, texture/model helpers, THP-style split/combine, shifting, GID display, FSYS entry identifiers, and copied-ISO Dolphin smoke coverage for rebuilt imports are covered. |

## Highest Confidence Remaining Gaps

1. Deeper patch behavior tests: the smoke matrix confirms patched ISOs boot, but patches that inject branches still need gameplay/save-state verification to prove their runtime effect.
2. Randomizer fixture tests: behavior now tracks the Swift buckets, but deterministic fixture tests would catch regressions in specific table writes.
3. Message special characters: verify newline/control-code round trips against Swift string table behavior.
4. Collision and Vertex Filters: verify on real exported assets because their correctness is mostly visual/data-format dependent.

## Intentional Non-Gaps

- XD-only screens, Pokespot tools, Battle Bingo, and PBR tooling are not Colosseum Tool parity requirements yet.
- The Table Editor does not need general in-app cell editing for parity because the Swift edit action is not implemented.
- macOS traffic-light window controls are not a parity target; the user chose native Windows-style window controls.
