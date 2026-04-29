# Dolphin Smoke Testing

Local Dolphin and ISO fixtures live under `.local/`, which is gitignored. Do not commit emulator binaries, ISOs, memory cards, save states, or generated Dolphin user data.

Current local layout:

```text
.local/
  dolphin/Dolphin-x64/Dolphin.exe
  dolphin-user/
  fixtures/Pokemon Colosseum.iso
artifacts/dolphin-smoke/
```

Run a basic boot/crash smoke test:

```powershell
.\scripts\run-dolphin-smoke.ps1
```

The runner launches Dolphin in batch mode with an isolated user folder, the Null video backend, and no frame limit. If Dolphin stays alive until the timeout, the smoke test passes. If it exits too early, returns a nonzero exit code, or writes known failure patterns to the logs, the smoke test fails.

Useful overrides:

```powershell
.\scripts\run-dolphin-smoke.ps1 `
  -DolphinExe "D:\Tools\Dolphin\Dolphin.exe" `
  -IsoPath "D:\Temp\patched-colosseum.iso" `
  -Seconds 90
```

This is a crash/boot smoke layer, not full gameplay verification. Deeper parity tests should add save states or DTM input recordings that exercise specific patched behavior.
