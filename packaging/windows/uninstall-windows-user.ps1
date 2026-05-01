$ErrorActionPreference = "Stop"

$installBase = Join-Path $env:LOCALAPPDATA "CipherSnagemEditor"
$startMenuRoot = Join-Path $env:APPDATA "Microsoft\Windows\Start Menu\Programs\Cipher Snagem Editor"
$desktopRoot = [Environment]::GetFolderPath("DesktopDirectory")

foreach ($shortcut in @(
    (Join-Path $startMenuRoot "Colosseum Tool.lnk"),
    (Join-Path $startMenuRoot "GoD Tool.lnk"),
    (Join-Path $desktopRoot "Colosseum Tool.lnk"),
    (Join-Path $desktopRoot "GoD Tool.lnk")
)) {
    if (Test-Path -LiteralPath $shortcut) {
        Remove-Item -LiteralPath $shortcut -Force
    }
}

if (Test-Path -LiteralPath $startMenuRoot) {
    $remaining = Get-ChildItem -LiteralPath $startMenuRoot -Force
    if ($remaining.Count -eq 0) {
        Remove-Item -LiteralPath $startMenuRoot -Force
    }
}

if (Test-Path -LiteralPath $installBase) {
    Remove-Item -LiteralPath $installBase -Recurse -Force
}

Write-Host "Removed Cipher Snagem Editor per-user install files and shortcuts."
