param(
    [string]$CleanIsoPath = "",
    [int]$Messages = 50,
    [int]$Assets = 50,
    [switch]$NoBuild
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($CleanIsoPath)) {
    $CleanIsoPath = Join-Path $repoRoot ".local\fixtures\Pokemon Colosseum.iso"
}

$CleanIsoPath = [System.IO.Path]::GetFullPath($CleanIsoPath)
$cliProject = Join-Path $repoRoot "src\CipherSnagemEditor.Cli\CipherSnagemEditor.Cli.csproj"

if (-not (Test-Path -LiteralPath $CleanIsoPath)) {
    throw "Clean ISO fixture not found: $CleanIsoPath"
}
if (-not (Test-Path -LiteralPath $cliProject)) {
    throw "CLI project not found: $cliProject"
}

$dotnetArgs = @("run")
if ($NoBuild) {
    $dotnetArgs += "--no-build"
}

$dotnetArgs += @(
    "--project",
    $cliProject,
    "--",
    "parity-probe",
    $CleanIsoPath,
    "--messages",
    $Messages,
    "--assets",
    $Assets
)

& dotnet @dotnetArgs
if ($LASTEXITCODE -ne 0) {
    throw "Colosseum parity probes failed."
}
