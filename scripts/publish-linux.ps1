param(
    [ValidateSet("Colosseum", "GoD")]
    [string]$Tool = "Colosseum",
    [ValidateSet("linux-x64", "linux-arm64")]
    [string]$Runtime = "linux-x64",
    [string]$Configuration = "Release",
    [string]$OutputRoot = "artifacts",
    [switch]$FrameworkDependent,
    [switch]$NoArchive,
    [switch]$NoDeb,
    [switch]$NoReadyToRun,
    [string]$PackageVersion = "0.1.12"
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$toolSlug = if ($Tool -eq "GoD") { "god-tool" } else { "colosseum-tool" }
$appName = if ($Tool -eq "GoD") { "GoD Tool" } else { "Colosseum Tool" }
$appComment = if ($Tool -eq "GoD") {
    "Pokemon XD Gale of Darkness modding editor"
} else {
    "Pokemon Colosseum modding editor"
}
$packageName = if ($Tool -eq "GoD") { "cipher-snagem-god-tool" } else { "cipher-snagem-colosseum-tool" }
$iconName = $packageName
$projectRelativePath = if ($Tool -eq "GoD") {
    "src/CipherSnagemEditor.GoDTool/CipherSnagemEditor.GoDTool.csproj"
} else {
    "src/CipherSnagemEditor.ColosseumTool/CipherSnagemEditor.ColosseumTool.csproj"
}
$launcherExecutable = if ($Tool -eq "GoD") { "GoDTool" } else { "ColosseumTool" }
$outputRootFull = [System.IO.Path]::GetFullPath((Join-Path $repoRoot $OutputRoot))
$publishDir = Join-Path $outputRootFull "publish-$toolSlug-$Runtime"
$packageDir = Join-Path $outputRootFull "packages\$toolSlug-$Runtime"
$runtimeLabel = $Runtime.Replace("linux-", "")
$archivePath = Join-Path $outputRootFull "packages\$toolSlug-linux-portable-$runtimeLabel.tar.gz"
$debPath = Join-Path $outputRootFull "packages\$toolSlug-ubuntu-debian-$runtimeLabel.deb"
$archiveFileName = Split-Path -Leaf $archivePath
$debFileName = Split-Path -Leaf $debPath
$projectPath = Join-Path $repoRoot $projectRelativePath
$linuxTemplateDir = Join-Path $repoRoot "packaging/linux"
$iconPath = if ($Tool -eq "GoD") {
    Join-Path $repoRoot "assets/ui/app-icons/xd/icon-32.png"
} else {
    Join-Path $repoRoot "assets/ui/app-icons/colosseum/icon-256.png"
}
$debScriptPath = Join-Path $repoRoot "scripts/create-linux-deb.py"

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
Copy-Item -LiteralPath (Join-Path $linuxTemplateDir "README-linux.txt") -Destination $packageDir -Force
Copy-Item -LiteralPath (Join-Path $linuxTemplateDir "cipher-snagem-editor.desktop") -Destination (Join-Path $packageDir "$packageName.desktop") -Force

$templateReplacements = @{
    "@APP_NAME@" = $appName
    "@APP_COMMENT@" = $appComment
    "@APP_SLUG@" = $packageName
    "@EXECUTABLE@" = $launcherExecutable
    "@ICON_NAME@" = $iconName
    "@DEB_FILE@" = $debFileName
    "@ARCHIVE_FILE@" = $archiveFileName
    "@PACKAGE_DIR@" = "$toolSlug-$Runtime"
}

foreach ($templateFile in @(
    "run-cipher-snagem-editor.sh",
    "install-linux-user.sh",
    "README-linux.txt",
    "$packageName.desktop"
)) {
    $templatePath = Join-Path $packageDir $templateFile
    $text = Get-Content -LiteralPath $templatePath -Raw
    foreach ($key in $templateReplacements.Keys) {
        $text = $text.Replace($key, $templateReplacements[$key])
    }
    Set-Content -LiteralPath $templatePath -Value $text -NoNewline
}

$resourceDir = Join-Path $packageDir "resources"
New-Item -ItemType Directory -Force -Path $resourceDir | Out-Null
Copy-Item -LiteralPath $iconPath -Destination (Join-Path $resourceDir "$iconName.png") -Force

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
        --version $PackageVersion `
        --package-name $packageName `
        --install-root "/opt/$packageName" `
        --app-name $appName `
        --comment $appComment `
        --icon-name $iconName `
        --executable $launcherExecutable
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to create Ubuntu package: $debPath"
    }

}

Write-Host "Linux publish complete."
Write-Host "Tool: $Tool"
Write-Host "Runtime: $Runtime"
Write-Host "Self-contained: $selfContained"
Write-Host "ReadyToRun: $readyToRun"
Write-Host "Package directory: $packageDir"
if (-not $NoArchive) {
    Write-Host "Archive: $archivePath"
}
if (-not $NoDeb) {
    Write-Host "Ubuntu package: $debPath"
}
