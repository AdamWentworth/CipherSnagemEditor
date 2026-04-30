# Trainer Editor UI Contract

The Trainer Editor Pokemon card layout is treated as an approved compatibility
surface. It should match the Windows-approved Colosseum Tool layout unless the
contract is intentionally updated.

Contract rules:

- Do not replace the Trainer Pokemon card template with `LegacyPicker`, split
  slot views, or a `Viewbox`-scaled surface.
- Keep the Trainer Editor minimum tool-window size at `1420 x 760`; shrinking
  below this size changes the Pokemon card proportions.
- Keep the Pokemon cards as a 2 x 3 grid with the approved inline
  `TrainerPokemonSlotViewModel` template. The card template is hash-checked in
  tests because tiny spacing changes are visually meaningful here.
- If the card template must change, update the contract test only after visual
  review on a current Windows build and a current Linux build from the same
  commit.

The enforced test is `TrainerEditorUiContractTests`.

Intentional consequence: on small displays or split-screen sizes, this window
may refuse to shrink below the approved size. That is preferable to letting the
per-Pokemon controls compress into an unreadable shape.
