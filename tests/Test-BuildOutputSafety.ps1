[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"
$rootDirectory = Split-Path -Parent $PSScriptRoot
. (Join-Path $rootDirectory "exodus.ps1")

$testDirectory = Join-Path ([IO.Path]::GetTempPath()) ("grimoire-build-output-" + [Guid]::NewGuid().ToString("N"))
$outputDirectory = Join-Path $testDirectory "Release"
$stagingDirectory = Join-Path $testDirectory "staging"
$lockedExecutable = Join-Path $outputDirectory "Grimoire Exodus.exe"

try {
    New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null
    [IO.File]::WriteAllText($lockedExecutable, "old")

    $lock = [IO.File]::Open($lockedExecutable, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::Read)
    try {
        $rejected = $false
        try {
            Assert-DefaultBuildOutputAvailable $outputDirectory
        }
        catch {
            $rejected = $true
        }
        if (-not $rejected) {
            throw "A locked build output was not rejected before publication."
        }
        if ([IO.File]::ReadAllText($lockedExecutable) -ne "old") {
            throw "The locked build output was modified during its availability check."
        }
    }
    finally {
        $lock.Dispose()
    }

    Assert-DefaultBuildOutputAvailable $outputDirectory
    $incompleteDirectory = Join-Path $testDirectory "incomplete"
    New-Item -ItemType Directory -Path $incompleteDirectory -Force | Out-Null
    [IO.File]::WriteAllText((Join-Path $incompleteDirectory "Grimoire Exodus.exe"), "incomplete")
    $incompleteRejected = $false
    try {
        Assert-CompleteBuildOutput $incompleteDirectory
    }
    catch {
        $incompleteRejected = $true
    }
    if (-not $incompleteRejected) {
        throw "An incomplete build payload was accepted for publication."
    }

    New-Item -ItemType Directory -Path $stagingDirectory -Force | Out-Null
    [IO.File]::WriteAllText((Join-Path $stagingDirectory "Grimoire Exodus.exe"), "new")
    [IO.File]::WriteAllText((Join-Path $stagingDirectory "new-runtime.dll"), "new-runtime")
    Publish-DefaultBuildOutput $stagingDirectory $outputDirectory

    if ([IO.File]::ReadAllText($lockedExecutable) -ne "new") {
        throw "The complete staged build did not replace the prior output."
    }
    if (-not (Test-Path -LiteralPath (Join-Path $outputDirectory "new-runtime.dll"))) {
        throw "The staged build was published without its complete runtime payload."
    }
    if (Get-ChildItem -LiteralPath $testDirectory -Directory -Force |
        Where-Object { $_.Name -like ".Release.backup-*" }) {
        throw "A successful build publication left its backup directory behind."
    }

    Write-Host "Build output lock detection and staged publication passed."
}
finally {
    if (Test-Path -LiteralPath $testDirectory) {
        Remove-Item -LiteralPath $testDirectory -Recurse -Force
    }
}
