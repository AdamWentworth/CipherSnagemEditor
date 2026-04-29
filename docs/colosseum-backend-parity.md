# Colosseum Backend Parity

This note tracks the backend behaviors copied from the Swift Colosseum Tool so the ISO Explorer does not regress while the UI keeps moving toward 1-to-1 parity.

## Covered

- ISO workspace layout uses the legacy sibling `"<iso name> CM Tool"` folder and `Game Files/<stem>/` extraction folders.
- ISO import can replace larger files by shifting later ISO files and updating the FST metadata.
- FSYS archives can extract, repack, preserve compression state, delete entries through the legacy marker, and import edited workspace files.
- FSYS archives can add a new compressed inner file with a user-provided 4-digit identifier and import the expanded archive back into the ISO.
- `.msg.json` files are encoded back into `.msg` tables before FSYS repack.
- LZSS compressed FSYS entries are decoded on extract and re-encoded on import.
- `.gtx` and `.atx` textures decode to PNG and import matching PNG edits.
- `.gsw` texture bundles extract/import embedded GTX textures.
- DAT and room-data model files extract/import embedded textures.
- Colosseum `.pkx` model containers export a sibling `.pkx.dat` and import edits back into the `.pkx`.
- `.wzx` files export embedded DAT models as `_<index>.wzx.dat` files and import same-size edited models back into the `.wzx`.
- THP-style `.thh`/`.thd` pairs combine into `.thp` on decode and split edited `.thp` files back into `.thh`/`.thd` before FSYS repack.

## Regression Coverage

- `ProjectContextTests.DecodesAndRepacksPkxDatThroughFsysIsoExplorerFlow`
- `ProjectContextTests.DecodesAndRepacksWzxEmbeddedModelsThroughFsysIsoExplorerFlow`
- `ProjectContextTests.CombinesAndSplitsThpHeaderBodyThroughFsysIsoExplorerFlow`
- `ProjectContextTests.AddsFileToFsysAndImportsArchiveIntoIso`
- `ProjectContextTests.EncodesMessageJsonAndPacksFsysWorkspaceFile`
- `ProjectContextTests.ImportsLargerFileByShiftingLaterIsoFiles`
- `FsysArchiveTests` for direct FSYS add-file pointer/name/details updates, including pointer-table expansion.
- `ColosseumLegacyFileCodecsTests` for direct PKX, WZX, and THP codec behavior.
- `ColosseumTextureCodecTests` for texture, DAT texture, and GSW texture behavior.
- `scripts/run-colosseum-smoke-matrix.ps1` for copied-ISO Dolphin boot smoke checks after `Start.dol`, `common.fsys`, and `pocket_menu.fsys` rebuild/import paths.

## Intentional Gaps

- Colosseum script decompile/compile is not treated as stable parity yet. The Swift tool only decompiles Colosseum scripts behind experimental settings, and the Colosseum compiler path is stubbed/incomplete in the referenced source.
- Broad "dump every texture from every file" behavior is covered for the formats we currently identify and edit in the app, but it is not a promise that every possible binary blob in the ISO has a typed texture parser.
- XD/PBR-specific script, model, and texture helpers remain out of scope until the XD mode is started.
