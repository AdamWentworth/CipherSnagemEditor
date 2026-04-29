param(
    [string]$CleanIsoPath = "",
    [string]$WorkRoot = "",
    [int]$Messages = 75,
    [int]$Assets = 75,
    [switch]$NoBuild
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($CleanIsoPath)) {
    $CleanIsoPath = Join-Path $repoRoot ".local\fixtures\Pokemon Colosseum.iso"
}
if ([string]::IsNullOrWhiteSpace($WorkRoot)) {
    $WorkRoot = Join-Path $repoRoot ".local\closeout-work"
}

$CleanIsoPath = [System.IO.Path]::GetFullPath($CleanIsoPath)
$WorkRoot = [System.IO.Path]::GetFullPath($WorkRoot)
$cliProject = Join-Path $repoRoot "src\CipherSnagemEditor.Cli\CipherSnagemEditor.Cli.csproj"
$parityScript = Join-Path $repoRoot "scripts\run-colosseum-parity-probes.ps1"

if (-not (Test-Path -LiteralPath $CleanIsoPath)) {
    throw "Clean ISO fixture not found: $CleanIsoPath"
}
if (-not (Test-Path -LiteralPath $cliProject)) {
    throw "CLI project not found: $cliProject"
}
if (-not (Test-Path -LiteralPath $parityScript)) {
    throw "Parity script not found: $parityScript"
}

if (Test-Path -LiteralPath $WorkRoot) {
    Remove-Item -LiteralPath $WorkRoot -Recurse -Force
}
New-Item -ItemType Directory -Force -Path $WorkRoot | Out-Null

$closeoutIso = Join-Path $WorkRoot "colosseum-closeout.iso"
Copy-Item -LiteralPath $CleanIsoPath -Destination $closeoutIso -Force

$dotnetArgs = @("run")
if ($NoBuild) {
    $dotnetArgs += "--no-build"
}

$dotnetArgs += @(
    "--project",
    $cliProject,
    "--",
    "closeout-probe",
    $closeoutIso
)

& dotnet @dotnetArgs
if ($LASTEXITCODE -ne 0) {
    throw "Colosseum closeout probe failed."
}

$parityArgs = @(
    "-NoProfile",
    "-ExecutionPolicy",
    "Bypass",
    "-File",
    $parityScript,
    "-CleanIsoPath",
    $closeoutIso,
    "-Messages",
    $Messages,
    "-Assets",
    $Assets
)
if ($NoBuild) {
    $parityArgs += "-NoBuild"
}

& powershell @parityArgs
if ($LASTEXITCODE -ne 0) {
    throw "Colosseum parity probes failed."
}

Write-Host ""
Write-Host "Colosseum closeout checks complete."
Write-Host "Mutated test ISO: $closeoutIso"
