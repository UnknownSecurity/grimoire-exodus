[CmdletBinding()]
param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$rootDirectory = Split-Path -Parent $PSScriptRoot
$updater = Join-Path $rootDirectory "app\bin\$Configuration\Grimoire.Updater.exe"
$testDirectory = Join-Path $PSScriptRoot (".updater-test-" + [Guid]::NewGuid().ToString("N"))

if (-not (Test-Path -LiteralPath $updater)) {
    throw "The updater helper was not found: $updater"
}

function Set-TestFile([string]$Path, [string]$Content) {
    $parent = Split-Path -Parent $Path
    New-Item -ItemType Directory -Path $parent -Force | Out-Null
    [IO.File]::WriteAllText($Path, $Content)
}

function New-TestPayload([string]$Directory, [string]$Marker) {
    New-Item -ItemType Directory -Path $Directory -Force | Out-Null
    foreach ($name in @("Grimoire Exodus.exe", "Grimoire.Updater.exe", "Flash.ocx", "grimoire-exodus.swf")) {
        Set-TestFile (Join-Path $Directory $name) "${Marker}-$name"
    }
    Set-TestFile (Join-Path $Directory "a-overwrite.txt") "${Marker}-overwrite"
    Set-TestFile (Join-Path $Directory "new-file.dll") "${Marker}-new"
}

function Invoke-TestUpdater([string[]]$UpdaterArguments) {
    $startInfo = [Diagnostics.ProcessStartInfo]::new()
    $startInfo.FileName = $updater
    $startInfo.UseShellExecute = $false
    $startInfo.CreateNoWindow = $true
    foreach ($argument in $UpdaterArguments) {
        $startInfo.ArgumentList.Add($argument)
    }
    $process = [Diagnostics.Process]::Start($startInfo)
    $process.WaitForExit()
    $exitCode = $process.ExitCode
    $process.Dispose()
    return $exitCode
}

try {
    $successInstall = Join-Path $testDirectory "success-install"
    $successPayload = Join-Path $testDirectory "success-cache\1.2.0\payload"
    New-TestPayload $successPayload "new"
    Set-TestFile (Join-Path $successInstall "Grimoire Exodus.exe") "old-exe"
    Set-TestFile (Join-Path $successInstall "a-overwrite.txt") "old-overwrite"
    Set-TestFile (Join-Path $successInstall "legacy-user-bot.gbot") "preserve-me"
    $successLog = Join-Path $testDirectory "success.log"

    $successExitCode = Invoke-TestUpdater @(
        "--process-id", ([int]::MaxValue).ToString(),
        "--target", $successInstall,
        "--restart", (Join-Path $successInstall "Grimoire Exodus.exe"),
        "--log", $successLog,
        "--source", $successPayload,
        "--no-restart", "--no-ui")
    if ($successExitCode -ne 0) {
        throw "The portable updater success test failed with exit code $successExitCode."
    }
    if ([IO.File]::ReadAllText((Join-Path $successInstall "a-overwrite.txt")) -ne "new-overwrite") {
        throw "The updater did not replace an application file."
    }
    if ([IO.File]::ReadAllText((Join-Path $successInstall "legacy-user-bot.gbot")) -ne "preserve-me") {
        throw "The updater modified an unrelated legacy bot file."
    }
    if (-not (Test-Path -LiteralPath (Join-Path $successInstall "new-file.dll"))) {
        throw "The updater did not merge a new application file."
    }

    $rollbackInstall = Join-Path $testDirectory "rollback-install"
    $rollbackPayload = Join-Path $testDirectory "rollback-cache\1.2.0\payload"
    New-TestPayload $rollbackPayload "new"
    Set-TestFile (Join-Path $rollbackPayload "z-locked.dll") "new-locked"
    Set-TestFile (Join-Path $rollbackInstall "Grimoire Exodus.exe") "old-exe"
    Set-TestFile (Join-Path $rollbackInstall "a-overwrite.txt") "old-overwrite"
    Set-TestFile (Join-Path $rollbackInstall "z-locked.dll") "old-locked"
    $rollbackLog = Join-Path $testDirectory "rollback.log"
    $lockedStream = [IO.File]::Open(
        (Join-Path $rollbackInstall "z-locked.dll"),
        [IO.FileMode]::Open,
        [IO.FileAccess]::ReadWrite,
        [IO.FileShare]::None)
    try {
        $rollbackExitCode = Invoke-TestUpdater @(
            "--process-id", ([int]::MaxValue).ToString(),
            "--target", $rollbackInstall,
            "--restart", (Join-Path $rollbackInstall "Grimoire Exodus.exe"),
            "--log", $rollbackLog,
            "--source", $rollbackPayload,
            "--no-restart", "--no-ui")
        if ($rollbackExitCode -eq 0) {
            throw "The updater unexpectedly succeeded while a destination file was locked."
        }
    }
    finally {
        $lockedStream.Dispose()
    }

    if ([IO.File]::ReadAllText((Join-Path $rollbackInstall "a-overwrite.txt")) -ne "old-overwrite") {
        throw "The updater did not roll back a replaced file after failure."
    }
    if (Test-Path -LiteralPath (Join-Path $rollbackInstall "new-file.dll")) {
        throw "The updater did not remove a newly created file during rollback."
    }

    Write-Host "Portable merge, legacy-file preservation, and rollback passed."
}
finally {
    if (Test-Path -LiteralPath $testDirectory) {
        Remove-Item -LiteralPath $testDirectory -Recurse -Force
    }
}
