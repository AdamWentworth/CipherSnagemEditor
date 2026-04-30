param(
    [ValidateSet("linux-x64", "linux-arm64")]
    [string]$Runtime = "linux-x64",
    [string]$Configuration = "Release",
    [string]$OutputRoot = "artifacts",
    [switch]$FrameworkDependent,
    [switch]$NoArchive,
    [switch]$NoDeb,
    [switch]$NoReadyToRun,
    [string]$PackageVersion = "0.1.10"
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$outputRootFull = [System.IO.Path]::GetFullPath((Join-Path $repoRoot $OutputRoot))
$publishDir = Join-Path $outputRootFull "publish-$Runtime"
$packageDir = Join-Path $outputRootFull "packages\cipher-snagem-editor-$Runtime"
$archivePath = Join-Path $outputRootFull "packages\cipher-snagem-editor-$Runtime.tar.gz"
$debPath = Join-Path $outputRootFull "packages\cipher-snagem-editor-$Runtime.deb"
$versionedDebPath = Join-Path $outputRootFull "packages\cipher-snagem-editor-$Runtime-$PackageVersion.deb"
$projectPath = Join-Path $repoRoot "src\CipherSnagemEditor.App\CipherSnagemEditor.App.csproj"
$linuxTemplateDir = Join-Path $repoRoot "packaging\linux"
$iconPath = Join-Path $repoRoot "assets\ui\app-icons\colosseum\icon-256.png"
$debScriptPath = Join-Path $repoRoot "scripts\create-linux-deb.py"

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
if (-not (Test-Path -LiteralPath $debScriptPath)) {
    throw "Linux deb helper not found: $debScriptPath"
}

New-Item -ItemType Directory -Force -Path $outputRootFull | Out-Null
Assert-UnderPath -Path $publishDir -Root $outputRootFull
Assert-UnderPath -Path $packageDir -Root $outputRootFull
Assert-UnderPath -Path $archivePath -Root $outputRootFull
Assert-UnderPath -Path $debPath -Root $outputRootFull
Assert-UnderPath -Path $versionedDebPath -Root $outputRootFull

foreach ($path in @($publishDir, $packageDir)) {
    if (Test-Path -LiteralPath $path) {
        Remove-Item -LiteralPath $path -Recurse -Force
    }
}
if (Test-Path -LiteralPath $archivePath) {
    Remove-Item -LiteralPath $archivePath -Force
}
if (Test-Path -LiteralPath $debPath) {
    Remove-Item -LiteralPath $debPath -Force
}
if (Test-Path -LiteralPath $versionedDebPath) {
    Remove-Item -LiteralPath $versionedDebPath -Force
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

if (-not $NoDeb) {
    $python = Get-Command python -ErrorAction SilentlyContinue
    if ($null -eq $python) {
        throw "Python 3 is required to create the Ubuntu .deb package. Re-run with -NoDeb to build only the portable tarball."
    }

    & $python.Source $debScriptPath `
        --package-dir $packageDir `
        --output $debPath `
        --runtime $Runtime `
        --version $PackageVersion
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to create Ubuntu package: $debPath"
    }

    Copy-Item -LiteralPath $debPath -Destination $versionedDebPath -Force
}

Write-Host "Linux publish complete."
Write-Host "Runtime: $Runtime"
Write-Host "Self-contained: $selfContained"
Write-Host "ReadyToRun: $readyToRun"
Write-Host "Package directory: $packageDir"
if (-not $NoArchive) {
    Write-Host "Archive: $archivePath"
}
if (-not $NoDeb) {
    Write-Host "Ubuntu package: $debPath"
    Write-Host "Versioned Ubuntu package: $versionedDebPath"
}
