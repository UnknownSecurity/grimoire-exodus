[CmdletBinding()]
param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug",

    [string]$OutputDirectory
)

$ErrorActionPreference = "Stop"
$rootDirectory = Split-Path -Parent $PSScriptRoot
$outputDirectory = if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    Join-Path $rootDirectory "app\bin\$Configuration"
}
else {
    [IO.Path]::GetFullPath($(if ([IO.Path]::IsPathRooted($OutputDirectory)) {
        $OutputDirectory
    } else {
        Join-Path $rootDirectory $OutputDirectory
    }))
}
$compiler = Join-Path $env:WINDIR "Microsoft.NET\Framework\v4.0.30319\csc.exe"
$source = Join-Path $PSScriptRoot "SettingsScrollbarSmoke.cs"
$client = Join-Path $outputDirectory "Grimoire Exodus.exe"
$darkUi = Join-Path $outputDirectory "DarkUI.dll"
$clientProbe = Join-Path $outputDirectory "Grimoire.dll"
$smokeExecutable = Join-Path $outputDirectory "SettingsScrollbarSmoke-$PID.exe"

foreach ($requiredPath in @($compiler, $source, $client, $darkUi, $outputDirectory)) {
    if (-not (Test-Path -LiteralPath $requiredPath)) {
        throw "Required Settings scrollbar smoke-test input was not found: $requiredPath"
    }
}
if (Test-Path -LiteralPath $clientProbe) {
    throw "The smoke test will not overwrite the existing assembly probe: $clientProbe"
}

Copy-Item -LiteralPath $client -Destination $clientProbe
try {
    & $compiler /nologo /target:exe /platform:x86 /reference:System.dll /reference:System.Core.dll /reference:System.Drawing.dll /reference:System.Windows.Forms.dll "/reference:$client" "/reference:$darkUi" "/out:$smokeExecutable" $source
    if ($LASTEXITCODE -ne 0) {
        throw "Compiling the Settings scrollbar smoke test failed with exit code $LASTEXITCODE."
    }

    Push-Location $outputDirectory
    try {
        & $smokeExecutable
        if ($LASTEXITCODE -ne 0) {
            throw "Settings scrollbar smoke test failed with exit code $LASTEXITCODE."
        }
    }
    finally {
        Pop-Location
    }
}
finally {
    foreach ($temporaryPath in @($smokeExecutable, $clientProbe)) {
        if (Test-Path -LiteralPath $temporaryPath) {
            Remove-Item -LiteralPath $temporaryPath -Force
        }
    }
}
