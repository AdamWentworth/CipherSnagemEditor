param(
    [string]$DolphinExe = "",
    [string]$IsoPath = "",
    [string]$UserDir = "",
    [string]$OutputDir = "",
    [int]$Seconds = 45,
    [int]$MinimumSeconds = 10,
    [string]$VideoBackend = "Null",
    [switch]$AllowEarlyExit
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($DolphinExe)) {
    $DolphinExe = Join-Path $repoRoot ".local\dolphin\Dolphin-x64\Dolphin.exe"
}
if ([string]::IsNullOrWhiteSpace($IsoPath)) {
    $IsoPath = Join-Path $repoRoot ".local\fixtures\Pokemon Colosseum.iso"
}
if ([string]::IsNullOrWhiteSpace($UserDir)) {
    $UserDir = Join-Path $repoRoot ".local\dolphin-user"
}
if ([string]::IsNullOrWhiteSpace($OutputDir)) {
    $OutputDir = Join-Path $repoRoot "artifacts\dolphin-smoke"
}

$DolphinExe = [System.IO.Path]::GetFullPath($DolphinExe)
$IsoPath = [System.IO.Path]::GetFullPath($IsoPath)
$UserDir = [System.IO.Path]::GetFullPath($UserDir)
$OutputDir = [System.IO.Path]::GetFullPath($OutputDir)

if (-not (Test-Path -LiteralPath $DolphinExe)) {
    throw "Dolphin executable not found: $DolphinExe"
}
if (-not (Test-Path -LiteralPath $IsoPath)) {
    throw "ISO not found: $IsoPath"
}

New-Item -ItemType Directory -Force -Path $UserDir, $OutputDir | Out-Null

$stamp = Get-Date -Format "yyyyMMdd-HHmmss"
$stdoutPath = Join-Path $OutputDir "dolphin-$stamp.out.log"
$stderrPath = Join-Path $OutputDir "dolphin-$stamp.err.log"
$summaryPath = Join-Path $OutputDir "dolphin-$stamp.summary.txt"
$userLogPath = Join-Path $UserDir "Logs\dolphin.log"

function ConvertTo-CommandLineArg([string]$value) {
    if ($value -notmatch '[\s"]') {
        return $value
    }

    return '"' + ($value -replace '\\(?=\\*")', '$0$0' -replace '"', '\"') + '"'
}

$arguments = @(
    "--batch",
    "--user=$UserDir",
    "--exec=$IsoPath",
    "--video_backend=$VideoBackend",
    "--config=Core.Framelimit=0",
    "--config=DSP.Backend=No audio output"
) | ForEach-Object { ConvertTo-CommandLineArg $_ }

$startedAt = Get-Date
$process = Start-Process `
    -FilePath $DolphinExe `
    -ArgumentList ($arguments -join " ") `
    -WorkingDirectory (Split-Path -Parent $DolphinExe) `
    -RedirectStandardOutput $stdoutPath `
    -RedirectStandardError $stderrPath `
    -WindowStyle Hidden `
    -PassThru

$exited = $process.WaitForExit($Seconds * 1000)
$endedAt = Get-Date
$elapsed = ($endedAt - $startedAt).TotalSeconds
$status = "Passed"
$reason = "Dolphin stayed alive for $([Math]::Round($elapsed, 1)) seconds."

if ($exited) {
    $exitCode = $process.ExitCode
    if (-not $AllowEarlyExit -and $elapsed -lt $MinimumSeconds) {
        $status = "Failed"
        $reason = "Dolphin exited after $([Math]::Round($elapsed, 1)) seconds, below the $MinimumSeconds second minimum. Exit code: $exitCode."
    } elseif ($exitCode -ne 0) {
        $status = "Failed"
        $reason = "Dolphin exited with code $exitCode after $([Math]::Round($elapsed, 1)) seconds."
    } else {
        $reason = "Dolphin exited cleanly after $([Math]::Round($elapsed, 1)) seconds."
    }
} else {
    Stop-Process -Id $process.Id -Force
    $process.WaitForExit()
}

$copiedUserLogPath = $null
if (Test-Path -LiteralPath $userLogPath) {
    $copiedUserLogPath = Join-Path $OutputDir "dolphin-$stamp.user.log"
    Copy-Item -LiteralPath $userLogPath -Destination $copiedUserLogPath -Force
}

$logText = ""
foreach ($candidate in @($stdoutPath, $stderrPath, $copiedUserLogPath)) {
    if ($candidate -and (Test-Path -LiteralPath $candidate)) {
        $logText += "`n" + (Get-Content -Raw -LiteralPath $candidate)
    }
}

$badPatterns = @(
    "panic alert",
    "fatal",
    "failed to load",
    "could not boot",
    "exception",
    "segmentation fault"
)

foreach ($pattern in $badPatterns) {
    if ($logText -match [regex]::Escape($pattern)) {
        $status = "Failed"
        $reason = "Dolphin log matched failure pattern: $pattern"
        break
    }
}

@(
    "Status: $status",
    "Reason: $reason",
    "Dolphin: $DolphinExe",
    "ISO: $IsoPath",
    "UserDir: $UserDir",
    "Started: $startedAt",
    "Ended: $endedAt",
    "Stdout: $stdoutPath",
    "Stderr: $stderrPath",
    "UserLog: $copiedUserLogPath"
) | Set-Content -LiteralPath $summaryPath

Get-Content -LiteralPath $summaryPath

if ($status -ne "Passed") {
    exit 1
}
