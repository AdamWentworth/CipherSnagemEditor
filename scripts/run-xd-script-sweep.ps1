param(
    [string]$CleanIsoPath = "",
    [int]$Limit = 0,
    [switch]$StrictByteMatch,
    [switch]$NoBuild
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($CleanIsoPath)) {
    $CleanIsoPath = Join-Path $repoRoot ".local\fixtures\Pokemon XD - Gale of Darkness.iso"
}

$CleanIsoPath = [System.IO.Path]::GetFullPath($CleanIsoPath)
$cliProject = Join-Path $repoRoot "src\CipherSnagemEditor.Cli\CipherSnagemEditor.Cli.csproj"

if (-not (Test-Path -LiteralPath $CleanIsoPath)) {
    throw "Clean XD ISO fixture not found: $CleanIsoPath"
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
    "xd-script-sweep",
    $CleanIsoPath
)

if ($Limit -gt 0) {
    $dotnetArgs += @("--limit", $Limit)
}
if ($StrictByteMatch) {
    $dotnetArgs += "--strict-byte-match"
}

& dotnet @dotnetArgs
if ($LASTEXITCODE -ne 0) {
    throw "XD script sweep failed."
}
