[CmdletBinding()]
param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",

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
$source = Join-Path $PSScriptRoot "UpdateServiceSmoke.cs"
$smokeExecutable = Join-Path $outputDirectory "UpdateServiceSmoke.exe"
$legacyAssemblyName = Join-Path $outputDirectory "Grimoire.exe"
$brandedAssemblyName = Join-Path $outputDirectory "Grimoire Exodus.exe"
$grimoireAssembly = if (Test-Path -LiteralPath $legacyAssemblyName) { $legacyAssemblyName } else { $brandedAssemblyName }

foreach ($requiredPath in @($compiler, $source, $grimoireAssembly)) {
    if (-not (Test-Path -LiteralPath $requiredPath)) {
        throw "Required update-service test input was not found: $requiredPath"
    }
}

& $compiler /nologo /target:exe /platform:x86 "/reference:$grimoireAssembly" "/out:$smokeExecutable" $source
if ($LASTEXITCODE -ne 0) {
    throw "Compiling the update-service test failed with exit code $LASTEXITCODE."
}

$temporaryAssemblyAlias = $false
try {
    if (-not (Test-Path -LiteralPath $legacyAssemblyName)) {
        Copy-Item -LiteralPath $brandedAssemblyName -Destination $legacyAssemblyName
        $temporaryAssemblyAlias = $true
    }

    & $smokeExecutable
    if ($LASTEXITCODE -ne 0) {
        throw "Update-service validation failed with exit code $LASTEXITCODE."
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
