param(
    [ValidateSet("Colosseum", "GoD")]
    [string]$Tool = "Colosseum",
    [ValidateSet("win-x64", "win-arm64")]
    [string]$Runtime = "win-x64",
    [string]$Configuration = "Release",
    [string]$OutputRoot = "artifacts",
    [switch]$FrameworkDependent,
    [switch]$NoArchive,
    [switch]$NoReadyToRun,
    [string]$PackageVersion = "0.1.12"
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$toolSlug = if ($Tool -eq "GoD") { "god-tool" } else { "colosseum-tool" }
$projectRelativePath = if ($Tool -eq "GoD") {
    "src\CipherSnagemEditor.GoDTool\CipherSnagemEditor.GoDTool.csproj"
} else {
    "src\CipherSnagemEditor.ColosseumTool\CipherSnagemEditor.ColosseumTool.csproj"
}
$projectPath = Join-Path $repoRoot $projectRelativePath
$windowsTemplateDir = Join-Path $repoRoot "packaging\windows"
$outputRootFull = [System.IO.Path]::GetFullPath((Join-Path $repoRoot $OutputRoot))
$publishDir = Join-Path $outputRootFull "publish-$toolSlug-$Runtime"
$packageDir = Join-Path $outputRootFull "packages\$toolSlug-$Runtime"
$runtimeLabel = $Runtime.Replace("win-", "")
$archivePath = Join-Path $outputRootFull "packages\$toolSlug-windows-portable-$runtimeLabel.zip"

function Assert-UnderPath([string]$Path, [string]$Root) {
    $fullPath = [System.IO.Path]::GetFullPath($Path)
    $fullRoot = [System.IO.Path]::GetFullPath($Root).TrimEnd(
        [System.IO.Path]::DirectorySeparatorChar,
        [System.IO.Path]::AltDirectorySeparatorChar)
    $rootPrefix = $fullRoot + [System.IO.Path]::DirectorySeparatorChar

    if ($fullPath -ne $fullRoot -and -not $fullPath.StartsWith($rootPrefix, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to operate outside output root. Path: $fullPath Root: $fullRoot"
    }
}

if (-not (Test-Path -LiteralPath $projectPath)) {
    throw "App project not found: $projectPath"
}
if (-not (Test-Path -LiteralPath $windowsTemplateDir)) {
    throw "Windows packaging templates not found: $windowsTemplateDir"
}

New-Item -ItemType Directory -Force -Path $outputRootFull | Out-Null
Assert-UnderPath -Path $publishDir -Root $outputRootFull
Assert-UnderPath -Path $packageDir -Root $outputRootFull
Assert-UnderPath -Path $archivePath -Root $outputRootFull

foreach ($path in @($publishDir, $packageDir)) {
    if (Test-Path -LiteralPath $path) {
        Remove-Item -LiteralPath $path -Recurse -Force
    }
}
foreach ($path in @($archivePath)) {
    if (Test-Path -LiteralPath $path) {
        Remove-Item -LiteralPath $path -Force
    }
}

$selfContained = if ($FrameworkDependent) { "false" } else { "true" }
$readyToRun = if ($FrameworkDependent -or $NoReadyToRun) { "false" } else { "true" }
dotnet publish $projectPath `
    -c $Configuration `
    -r $Runtime `
    --self-contained $selfContained `
    -p:PublishReadyToRun=$readyToRun `
    -o $publishDir
if ($LASTEXITCODE -ne 0) {
    throw "Windows publish failed for $Runtime."
}

New-Item -ItemType Directory -Force -Path $packageDir | Out-Null
Copy-Item -Path (Join-Path $publishDir "*") -Destination $packageDir -Recurse -Force
Copy-Item -LiteralPath (Join-Path $windowsTemplateDir "README-windows.txt") -Destination $packageDir -Force
Copy-Item -LiteralPath (Join-Path $windowsTemplateDir "install-windows-user.ps1") -Destination $packageDir -Force
Copy-Item -LiteralPath (Join-Path $windowsTemplateDir "uninstall-windows-user.ps1") -Destination $packageDir -Force

if (-not $NoArchive) {
    New-Item -ItemType Directory -Force -Path (Split-Path -Parent $archivePath) | Out-Null
    Compress-Archive -Path (Join-Path $packageDir "*") -DestinationPath $archivePath -Force
}

Write-Host "Windows publish complete."
Write-Host "Tool: $Tool"
Write-Host "Runtime: $Runtime"
Write-Host "Self-contained: $selfContained"
Write-Host "ReadyToRun: $readyToRun"
Write-Host "Package directory: $packageDir"
if (-not $NoArchive) {
    Write-Host "Archive: $archivePath"
}
