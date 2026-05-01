param(
    [string]$CleanIsoPath = "",
    [string]$WorkRoot = "",
    [switch]$NoBuild
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($CleanIsoPath)) {
    $CleanIsoPath = Join-Path $repoRoot ".local\fixtures\Pokemon XD - Gale of Darkness.iso"
}
if ([string]::IsNullOrWhiteSpace($WorkRoot)) {
    $WorkRoot = Join-Path $repoRoot ".local\closeout-work"
}

$CleanIsoPath = [System.IO.Path]::GetFullPath($CleanIsoPath)
$WorkRoot = [System.IO.Path]::GetFullPath($WorkRoot)
$cliProject = Join-Path $repoRoot "src\CipherSnagemEditor.Cli\CipherSnagemEditor.Cli.csproj"

if (-not (Test-Path -LiteralPath $CleanIsoPath)) {
    throw "Clean XD ISO fixture not found: $CleanIsoPath"
}
if (-not (Test-Path -LiteralPath $cliProject)) {
    throw "CLI project not found: $cliProject"
}

if (Test-Path -LiteralPath $WorkRoot) {
    Remove-Item -LiteralPath $WorkRoot -Recurse -Force
}
New-Item -ItemType Directory -Force -Path $WorkRoot | Out-Null

$closeoutIso = Join-Path $WorkRoot "xd-closeout.iso"
Copy-Item -LiteralPath $CleanIsoPath -Destination $closeoutIso -Force

$dotnetArgs = @("run")
if ($NoBuild) {
    $dotnetArgs += "--no-build"
}

$dotnetArgs += @(
    "--project",
    $cliProject,
    "--",
    "xd-closeout-probe",
    $closeoutIso
)

& dotnet @dotnetArgs
if ($LASTEXITCODE -ne 0) {
    throw "XD closeout probe failed."
}

Write-Host ""
Write-Host "XD closeout checks complete."
Write-Host "Mutated test ISO: $closeoutIso"
