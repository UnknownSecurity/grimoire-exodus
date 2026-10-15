[CmdletBinding()]
param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug",
    [string]$BotDirectory
)

$ErrorActionPreference = "Stop"
$rootDirectory = Split-Path -Parent $PSScriptRoot
$outputDirectory = Join-Path $rootDirectory "app\bin\$Configuration"
$compiler = Join-Path $env:WINDIR "Microsoft.NET\Framework\v4.0.30319\csc.exe"
$source = Join-Path $PSScriptRoot "LegacyGbotCompatibilitySmoke.cs"
$smokeExecutable = Join-Path $outputDirectory "LegacyGbotCompatibilitySmoke.exe"
$legacyAssemblyName = Join-Path $outputDirectory "Grimoire.exe"
$brandedAssemblyName = Join-Path $outputDirectory "Grimoire Exodus.exe"
$grimoireAssembly = if (Test-Path -LiteralPath $legacyAssemblyName) {
    $legacyAssemblyName
}
else {
    $brandedAssemblyName
}
$jsonAssembly = Join-Path $outputDirectory "Newtonsoft.Json.dll"

foreach ($requiredPath in @($compiler, $source, $grimoireAssembly, $jsonAssembly)) {
    if (-not (Test-Path -LiteralPath $requiredPath)) {
        throw "Required legacy-compatibility test input was not found: $requiredPath"
    }
}

& $compiler /nologo /target:exe /platform:x86 "/reference:$grimoireAssembly" "/reference:$jsonAssembly" "/out:$smokeExecutable" $source
if ($LASTEXITCODE -ne 0) {
    throw "Compiling the legacy GBot compatibility test failed with exit code $LASTEXITCODE."
}

$temporaryAssemblyAlias = $false
try {
    if (-not (Test-Path -LiteralPath $legacyAssemblyName)) {
        Copy-Item -LiteralPath $brandedAssemblyName -Destination $legacyAssemblyName
        $temporaryAssemblyAlias = $true
    }

    if ([string]::IsNullOrWhiteSpace($BotDirectory)) {
        $defaultBotDirectory = Join-Path ([Environment]::GetFolderPath([Environment+SpecialFolder]::MyDocuments)) "Grimoire\Bot"
        if (Test-Path -LiteralPath $defaultBotDirectory) {
            $BotDirectory = $defaultBotDirectory
        }
    }

    if ([string]::IsNullOrWhiteSpace($BotDirectory)) {
        & $smokeExecutable
    }
    else {
        & $smokeExecutable $BotDirectory
    }
    if ($LASTEXITCODE -ne 0) {
        throw "Legacy GBot compatibility failed with exit code $LASTEXITCODE."
    }
}
finally {
    if (Test-Path -LiteralPath $smokeExecutable) {
        Remove-Item -LiteralPath $smokeExecutable -Force
    }
    if ($temporaryAssemblyAlias -and (Test-Path -LiteralPath $legacyAssemblyName)) {
        Remove-Item -LiteralPath $legacyAssemblyName -Force
    }
}
