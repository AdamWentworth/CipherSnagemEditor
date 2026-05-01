param(
    [string]$CleanIsoPath = "",
    [string]$WorkRoot = "",
    [string]$Patch = "",
    [switch]$KeepIsos,
    [switch]$NoBuild
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($CleanIsoPath)) {
    $CleanIsoPath = Join-Path $repoRoot ".local\fixtures\Pokemon XD - Gale of Darkness.iso"
}
if ([string]::IsNullOrWhiteSpace($WorkRoot)) {
    $WorkRoot = Join-Path $repoRoot ".local\xd-patch-matrix-work"
}

$CleanIsoPath = [System.IO.Path]::GetFullPath($CleanIsoPath)
$WorkRoot = [System.IO.Path]::GetFullPath($WorkRoot)
$repoLocal = [System.IO.Path]::GetFullPath((Join-Path $repoRoot ".local"))
$cliProject = Join-Path $repoRoot "src\CipherSnagemEditor.Cli\CipherSnagemEditor.Cli.csproj"

if (-not (Test-Path -LiteralPath $CleanIsoPath)) {
    throw "Clean XD ISO fixture not found: $CleanIsoPath"
}
if (-not (Test-Path -LiteralPath $cliProject)) {
    throw "CLI project not found: $cliProject"
}

if (Test-Path -LiteralPath $WorkRoot) {
    $workRootPrefix = $repoLocal.TrimEnd(
        [System.IO.Path]::DirectorySeparatorChar,
        [System.IO.Path]::AltDirectorySeparatorChar) + [System.IO.Path]::DirectorySeparatorChar

    if (-not $WorkRoot.StartsWith($workRootPrefix, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to clean work root outside repo .local: $WorkRoot"
    }

    Remove-Item -LiteralPath $WorkRoot -Recurse -Force
}
New-Item -ItemType Directory -Force -Path $WorkRoot | Out-Null

$dotnetArgs = @("run")
if ($NoBuild) {
    $dotnetArgs += "--no-build"
}

$dotnetArgs += @(
    "--project",
    $cliProject,
    "--",
    "xd-patch-matrix",
    $CleanIsoPath,
    "--work-root",
    $WorkRoot
)

if (-not [string]::IsNullOrWhiteSpace($Patch)) {
    $dotnetArgs += @("--patch", $Patch)
}
if ($KeepIsos) {
    $dotnetArgs += "--keep-isos"
}

& dotnet @dotnetArgs
if ($LASTEXITCODE -ne 0) {
    throw "XD patch matrix failed."
}

Write-Host ""
Write-Host "XD patch matrix complete."
Write-Host "Work root: $WorkRoot"
