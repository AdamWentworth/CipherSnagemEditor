# Project Scope

Cipher Snagem Editor is focused on preserving and modernizing the legacy
Pokemon Colosseum and Pokemon XD editor workflows from StarsMMD's original
tools.

The project ships two desktop applications from one shared codebase:

- **Colosseum Tool** for Pokemon Colosseum.
- **GoD Tool** for Pokemon XD: Gale of Darkness.

## Supported Workflows

Both tools are designed around editing a clean ISO/GCM image through a generated
workspace folder. The app can open an ISO, extract/edit supported files, save
changes into the workspace, and import/rebuild modified files back into the ISO.

Core supported areas:

- ISO loading and region detection.
- Legacy-style workspace creation.
- FSYS extraction, import, repack, add, and delete flows.
- LZSS decode/re-encode for supported archive entries.
- Message table decode/edit/rebuild.
- Texture and model-adjacent helper import/export where implemented.
- Patch and randomizer workflows.
- Script compiler/decompiler workflows for Pokemon XD.

## Editor Coverage

Colosseum Tool includes:

- Trainer Editor
- Pokemon Stats Editor
- Move Editor
- Item Editor
- Gift Pokemon Editor
- Type Editor
- Treasure Editor
- Patches
- Randomizer
- Message Editor
- Collision Viewer
- Interaction Editor
- Vertex Filters
- Table Editor
- ISO Explorer

GoD Tool includes:

- Trainer Editor
- Pokemon Stats Editor
- Move Editor
- Item Editor
- Gift Pokemon Editor
- Type Editor
- Treasure Editor
- Patches
- Randomizer
- Message Editor
- Interaction Editor
- Shadow Pokemon Editor
- Pokespot Editor
- Script compiler/decompiler workflows
- Table Editor
- ISO Explorer

## Out Of Scope

This repository is not a general asset-authoring suite. Full Blender workflows,
custom model import/export, animation editing, move VFX authoring, map editing,
and custom audio pipelines are not part of the stable editor surface.

Those areas may be explored in separate experimental tooling, but they should
not destabilize the editor/rebuild workflows in this repository.

## Data Policy

No game files are included in this repository. Do not commit ISOs, GCMs,
extracted ISO contents, save files, generated workspaces, or emulator user data.
