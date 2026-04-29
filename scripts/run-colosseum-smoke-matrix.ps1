param(
    [string]$CleanIsoPath = "",
    [string]$WorkRoot = "",
    [string]$DolphinExe = "",
    [string]$DolphinUserDir = "",
    [string]$DolphinOutputRoot = "",
    [string[]]$Cases = @(
        "clean-boot",
        "patch-disable-save-corruption",
        "editor-move",
        "randomizer-species",
        "randomizer-shops"
    ),
    [int]$Seconds = 30,
    [int]$MinimumSeconds = 8,
    [string]$VideoBackend = "Null",
    [string]$AudioBackend = "No audio output",
    [int]$AudioVolume = 0,
    [switch]$PatchSweep,
    [switch]$SkipDolphin,
    [switch]$NoBuild
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($CleanIsoPath)) {
    $CleanIsoPath = Join-Path $repoRoot ".local\fixtures\Pokemon Colosseum.iso"
}
if ([string]::IsNullOrWhiteSpace($WorkRoot)) {
    $WorkRoot = Join-Path $repoRoot ".local\smoke-work"
}
if ([string]::IsNullOrWhiteSpace($DolphinOutputRoot)) {
    $DolphinOutputRoot = Join-Path $repoRoot "artifacts\dolphin-smoke"
}

$CleanIsoPath = [System.IO.Path]::GetFullPath($CleanIsoPath)
$WorkRoot = [System.IO.Path]::GetFullPath($WorkRoot)
$DolphinOutputRoot = [System.IO.Path]::GetFullPath($DolphinOutputRoot)
$cliProject = Join-Path $repoRoot "src\CipherSnagemEditor.Cli\CipherSnagemEditor.Cli.csproj"
$dolphinScript = Join-Path $repoRoot "scripts\run-dolphin-smoke.ps1"

if (-not (Test-Path -LiteralPath $CleanIsoPath)) {
    throw "Clean ISO fixture not found: $CleanIsoPath"
}
if (-not (Test-Path -LiteralPath $cliProject)) {
    throw "CLI project not found: $cliProject"
}
if (-not (Test-Path -LiteralPath $dolphinScript)) {
    throw "Dolphin smoke script not found: $dolphinScript"
}

New-Item -ItemType Directory -Force -Path $WorkRoot, $DolphinOutputRoot | Out-Null

if ($PatchSweep) {
    $Cases = @(
        $Cases
        "patch:PhysicalSpecialSplitApply"
        "patch:AddSoftReset"
        "patch:LoadPcFromAnywhere"
        "patch:InfiniteTms"
        "patch:Gen6CritMultipliers"
        "patch:Gen7CritRatios"
        "patch:EnableDebugLogs"
        "patch:NoTypeIconForLockedMoves"
        "patch:RemoveColbtlRegionLock"
    ) | Select-Object -Unique
}

$Cases = @(
    foreach ($case in $Cases) {
        $case -split "," | ForEach-Object { $_.Trim() } | Where-Object { -not [string]::IsNullOrWhiteSpace($_) }
    }
) | Select-Object -Unique

function Assert-UnderPath([string]$Path, [string]$Root) {
    $fullPath = [System.IO.Path]::GetFullPath($Path)
    $fullRoot = [System.IO.Path]::GetFullPath($Root).TrimEnd(
        [System.IO.Path]::DirectorySeparatorChar,
        [System.IO.Path]::AltDirectorySeparatorChar)
    $rootPrefix = $fullRoot + [System.IO.Path]::DirectorySeparatorChar

    if (-not $fullPath.StartsWith($rootPrefix, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to operate outside work root. Path: $fullPath Root: $fullRoot"
    }
}

function Get-CaseOperation([string]$CaseName) {
    switch ($CaseName) {
        "clean-boot" { return "" }
        "patch-disable-save-corruption" { return "patch:DisableSaveCorruption" }
        "patch-gen7-crit-ratios" { return "patch:Gen7CritRatios" }
        "editor-move" { return "editor-move" }
        "randomizer-species" { return "randomizer-species" }
        "randomizer-shops" { return "randomizer-shops" }
        default { return $CaseName }
    }
}

function Invoke-DotnetCli([string]$IsoPath, [string]$Operation) {
    $dotnetArgs = @("run")
    if ($NoBuild) {
        $dotnetArgs += "--no-build"
    }

    $dotnetArgs += @(
        "--project",
        $cliProject,
        "--",
        "smoke-apply",
        $IsoPath,
        $Operation
    )

    & dotnet @dotnetArgs
    if ($LASTEXITCODE -ne 0) {
        throw "CLI smoke operation failed: $Operation"
    }
}

function Invoke-DolphinSmoke([string]$IsoPath, [string]$CaseOutputDir) {
    $dolphinArgs = @(
        "-NoProfile",
        "-ExecutionPolicy",
        "Bypass",
        "-File",
        $dolphinScript,
        "-IsoPath",
        $IsoPath,
        "-OutputDir",
        $CaseOutputDir,
        "-Seconds",
        $Seconds,
        "-MinimumSeconds",
        $MinimumSeconds,
        "-VideoBackend",
        $VideoBackend,
        "-AudioBackend",
        $AudioBackend,
        "-AudioVolume",
        $AudioVolume
    )

    if (-not [string]::IsNullOrWhiteSpace($DolphinExe)) {
        $dolphinArgs += @("-DolphinExe", $DolphinExe)
    }
    if (-not [string]::IsNullOrWhiteSpace($DolphinUserDir)) {
        $dolphinArgs += @("-UserDir", $DolphinUserDir)
    }

    & powershell @dolphinArgs
    if ($LASTEXITCODE -ne 0) {
        throw "Dolphin smoke failed for ISO: $IsoPath"
    }
}

$results = New-Object System.Collections.Generic.List[object]
foreach ($case in $Cases) {
    $safeCase = $case -replace '[^\w\.-]+', '-'
    $caseRoot = [System.IO.Path]::GetFullPath((Join-Path $WorkRoot $safeCase))
    Assert-UnderPath -Path $caseRoot -Root $WorkRoot

    if (Test-Path -LiteralPath $caseRoot) {
        Remove-Item -LiteralPath $caseRoot -Recurse -Force
    }

    New-Item -ItemType Directory -Force -Path $caseRoot | Out-Null
    $caseIsoPath = Join-Path $caseRoot "$safeCase.iso"
    Copy-Item -LiteralPath $CleanIsoPath -Destination $caseIsoPath -Force

    $operation = Get-CaseOperation -CaseName $case
    Write-Host "== $case =="
    Write-Host "ISO: $caseIsoPath"
    if (-not [string]::IsNullOrWhiteSpace($operation)) {
        Write-Host "Operation: $operation"
        Invoke-DotnetCli -IsoPath $caseIsoPath -Operation $operation
    } else {
        Write-Host "Operation: clean boot"
    }

    $caseOutputDir = Join-Path $DolphinOutputRoot $safeCase
    if (-not $SkipDolphin) {
        Invoke-DolphinSmoke -IsoPath $caseIsoPath -CaseOutputDir $caseOutputDir
    }

    $results.Add([pscustomobject]@{
        Case = $case
        Operation = if ([string]::IsNullOrWhiteSpace($operation)) { "clean boot" } else { $operation }
        Iso = $caseIsoPath
        Logs = $caseOutputDir
    })
}

Write-Host ""
Write-Host "Smoke matrix complete."
$results | Format-Table -AutoSize
