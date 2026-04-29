param(
    [ValidateSet("linux-x64", "linux-arm64")]
    [string]$Runtime = "linux-x64",
    [string]$Configuration = "Release",
    [string]$OutputRoot = "artifacts",
    [switch]$FrameworkDependent,
    [switch]$NoArchive
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$outputRootFull = [System.IO.Path]::GetFullPath((Join-Path $repoRoot $OutputRoot))
$publishDir = Join-Path $outputRootFull "publish-$Runtime"
$packageDir = Join-Path $outputRootFull "packages\cipher-snagem-editor-$Runtime"
$archivePath = Join-Path $outputRootFull "packages\cipher-snagem-editor-$Runtime.tar.gz"
$projectPath = Join-Path $repoRoot "src\CipherSnagemEditor.App\CipherSnagemEditor.App.csproj"
$linuxTemplateDir = Join-Path $repoRoot "packaging\linux"
$iconPath = Join-Path $repoRoot "assets\ui\app-icons\colosseum\icon-256.png"

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
if (-not (Test-Path -LiteralPath $linuxTemplateDir)) {
    throw "Linux packaging templates not found: $linuxTemplateDir"
}
if (-not (Test-Path -LiteralPath $iconPath)) {
    throw "Linux icon source not found: $iconPath"
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
if (Test-Path -LiteralPath $archivePath) {
    Remove-Item -LiteralPath $archivePath -Force
}

$selfContained = if ($FrameworkDependent) { "false" } else { "true" }
dotnet publish $projectPath `
    -c $Configuration `
    -r $Runtime `
    --self-contained $selfContained `
    -o $publishDir
if ($LASTEXITCODE -ne 0) {
    throw "Linux publish failed for $Runtime."
}

New-Item -ItemType Directory -Force -Path $packageDir | Out-Null
Copy-Item -Path (Join-Path $publishDir "*") -Destination $packageDir -Recurse -Force
Copy-Item -LiteralPath (Join-Path $linuxTemplateDir "run-cipher-snagem-editor.sh") -Destination $packageDir -Force
Copy-Item -LiteralPath (Join-Path $linuxTemplateDir "install-linux-user.sh") -Destination $packageDir -Force
Copy-Item -LiteralPath (Join-Path $linuxTemplateDir "cipher-snagem-editor.desktop") -Destination $packageDir -Force
Copy-Item -LiteralPath (Join-Path $linuxTemplateDir "README-linux.txt") -Destination $packageDir -Force

$resourceDir = Join-Path $packageDir "resources"
New-Item -ItemType Directory -Force -Path $resourceDir | Out-Null
Copy-Item -LiteralPath $iconPath -Destination (Join-Path $resourceDir "cipher-snagem-editor.png") -Force

if (-not $NoArchive) {
    New-Item -ItemType Directory -Force -Path (Split-Path -Parent $archivePath) | Out-Null
    $packageParent = Split-Path -Parent $packageDir
    $packageLeaf = Split-Path -Leaf $packageDir
    tar -czf $archivePath -C $packageParent $packageLeaf
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to create Linux archive: $archivePath"
    }
}

Write-Host "Linux publish complete."
Write-Host "Runtime: $Runtime"
Write-Host "Self-contained: $selfContained"
Write-Host "Package directory: $packageDir"
if (-not $NoArchive) {
    Write-Host "Archive: $archivePath"
}
