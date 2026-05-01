# Testing

## Basic Checks

Install the .NET 10 SDK, then build and run the normal test suite:

```powershell
dotnet build CipherSnagemEditor.slnx
dotnet test CipherSnagemEditor.slnx --no-build
```

These checks do not require game files. Additional parity and smoke tests can be
run locally when clean ISO fixtures and Dolphin are available.

## Local Fixture Layout

Recommended local layout:

```text
.local/
  fixtures/
    Pokemon Colosseum.iso
    Pokemon XD - Gale of Darkness.iso
  dolphin/
  dolphin-user/
```

## Colosseum Checks

Run the main closeout probe:

```powershell
.\scripts\run-colosseum-closeout.ps1
```

Run rebuild/runtime smoke checks through Dolphin:

```powershell
.\scripts\run-colosseum-smoke-matrix.ps1
```

Run parser/codec probes against real assets from the clean ISO:

```powershell
.\scripts\run-colosseum-parity-probes.ps1
```

## Pokemon XD Checks

Run the main XD closeout probe:

```powershell
.\scripts\run-xd-closeout.ps1
```

Run the XD script compiler/decompiler sweep:

```powershell
.\scripts\run-xd-script-sweep.ps1 -StrictByteMatch
```

Run XD patch verification:

```powershell
.\scripts\run-xd-patch-matrix.ps1
```

Run XD Dolphin smoke checks:

```powershell
.\scripts\run-xd-smoke-matrix.ps1
```

## Dolphin Notes

Dolphin smoke tests are boot/crash checks. They are useful for proving that a
rebuilt ISO still starts, but they do not prove every gameplay branch or patch
effect. Deep gameplay verification still requires manual testing or future
save-state/DTM automation.

The smoke scripts configure Dolphin with muted audio for automated runs.
