# Cipher Snagem Lab Boundary

Cipher Snagem Editor is the stable legacy editor parity project. It should stay
focused on the workflows from StarsMMD's original Pokemon Colosseum and Pokemon
XD tools: tables, scripts, patches, randomizers, messages, ISO Explorer, and
safe rebuilds.

Cipher Snagem Lab is the proposed sibling repo for deeper asset tooling.

## Why Keep Lab Separate?

The editor is already useful because it is predictable. Model, VFX, audio, and
map tooling will involve format research, incomplete exporters, experimental
Blender bridges, large fixture comparisons, and many false starts. Keeping that
work in a separate repo protects the stable editor from becoming a half-finished
research workspace.

The split also gives each project a clearer identity:

- **Cipher Snagem Editor** edits known game data and rebuilds ISOs.
- **Cipher Snagem Lab** investigates asset pipelines that may later become
  polished editor features.

## Stays In Cipher Snagem Editor

- Colosseum Tool and GoD Tool desktop releases.
- Legacy editor UI parity.
- ISO open, workspace creation, extraction, import, and rebuild.
- FSYS, LZSS, message, texture, PKX, WZX, DAT, GSW, and THP helper round trips
  when those helpers support existing editor or ISO Explorer workflows.
- Patches, randomizers, script codec support, and table-level data editing.
- Regression tests and closeout probes that protect the legacy tool behavior.

## Belongs In Cipher Snagem Lab

- Full Pokemon model export/import workflows.
- Blender importer/exporter integration.
- Skeleton, animation, material, and texture authoring experiments.
- WZX/move VFX decoding, previewing, exporting, editing, and reinsertion.
- Human character, map, environment, collision, and camera model research.
- Audio/music/SFX extraction, conversion, replacement, and sound-event mapping.
- Project-folder dump/build workflows inspired by the newer Goddess direction.
- Any tool that needs broad experimental fixtures before it is safe for normal
  editor users.

## Good Integration Path

The projects can still share ideas and code. A useful pattern is:

1. Prototype format support in Cipher Snagem Lab.
2. Prove no-loss round trips with fixtures and tests.
3. Add a user-facing workflow only when the behavior is stable.
4. Port mature, well-scoped pieces back into Cipher Snagem Editor if they fit
   the legacy editor experience.

This keeps the editor boring in the best way: it works. Lab can be adventurous
without putting that at risk.
