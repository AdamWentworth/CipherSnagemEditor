param(
    [switch]$DesktopShortcut
)

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$candidates = @(
    @{ Exe = "ColosseumTool.exe"; Name = "Colosseum Tool"; Slug = "ColosseumTool" },
    @{ Exe = "GoDTool.exe"; Name = "GoD Tool"; Slug = "GoDTool" }
)

$tool = $null
foreach ($candidate in $candidates) {
    if (Test-Path -LiteralPath (Join-Path $scriptDir $candidate.Exe)) {
        $tool = $candidate
        break
    }
}

if ($null -eq $tool) {
    throw "Could not find ColosseumTool.exe or GoDTool.exe in this folder."
}

$installRoot = Join-Path $env:LOCALAPPDATA "CipherSnagemEditor\$($tool.Slug)"
$startMenuRoot = Join-Path $env:APPDATA "Microsoft\Windows\Start Menu\Programs\Cipher Snagem Editor"
$desktopRoot = [Environment]::GetFolderPath("DesktopDirectory")

New-Item -ItemType Directory -Force -Path $startMenuRoot | Out-Null

$sourceFull = [System.IO.Path]::GetFullPath($scriptDir).TrimEnd('\')
$targetFull = [System.IO.Path]::GetFullPath($installRoot).TrimEnd('\')

if (-not [string]::Equals($sourceFull, $targetFull, [System.StringComparison]::OrdinalIgnoreCase)) {
    if (Test-Path -LiteralPath $installRoot) {
        Remove-Item -LiteralPath $installRoot -Recurse -Force
    }

    New-Item -ItemType Directory -Force -Path $installRoot | Out-Null
    Copy-Item -LiteralPath (Join-Path $scriptDir "*") -Destination $installRoot -Recurse -Force
}

$targetExe = Join-Path $installRoot $tool.Exe
if (-not (Test-Path -LiteralPath $targetExe)) {
    throw "Installed executable was not found: $targetExe"
}

$shell = New-Object -ComObject WScript.Shell

function New-Shortcut([string]$ShortcutPath, [string]$TargetPath, [string]$WorkingDirectory, [string]$Description) {
    $shortcut = $shell.CreateShortcut($ShortcutPath)
    $shortcut.TargetPath = $TargetPath
    $shortcut.WorkingDirectory = $WorkingDirectory
    $shortcut.IconLocation = "$TargetPath,0"
    $shortcut.Description = $Description
    $shortcut.Save()
}

$startShortcut = Join-Path $startMenuRoot "$($tool.Name).lnk"
New-Shortcut `
    -ShortcutPath $startShortcut `
    -TargetPath $targetExe `
    -WorkingDirectory $installRoot `
    -Description "$($tool.Name) from Cipher Snagem Editor"

if ($DesktopShortcut) {
    $desktopShortcut = Join-Path $desktopRoot "$($tool.Name).lnk"
    New-Shortcut `
        -ShortcutPath $desktopShortcut `
        -TargetPath $targetExe `
        -WorkingDirectory $installRoot `
        -Description "$($tool.Name) from Cipher Snagem Editor"
}

Write-Host "Installed $($tool.Name) to $installRoot"
Write-Host "Start Menu shortcut: $startShortcut"
if ($DesktopShortcut) {
    Write-Host "Desktop shortcut: $desktopShortcut"
}
