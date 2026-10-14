[CmdletBinding()]
param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug"
)

$ErrorActionPreference = "Stop"
$rootDirectory = Split-Path -Parent $PSScriptRoot
$outputDirectory = Join-Path $rootDirectory "app\bin\$Configuration"
$compiler = Join-Path ([Runtime.InteropServices.RuntimeEnvironment]::GetRuntimeDirectory()) "csc.exe"
$source = Join-Path $PSScriptRoot "FlashActivationSmoke.cs"
$manifest = Join-Path $rootDirectory "app\Grimoire.manifest"
$smokeExecutable = Join-Path $outputDirectory "FlashActivationSmoke.exe"
$axInterop = Join-Path $outputDirectory "AxInterop.ShockwaveFlashObjects.dll"
$interop = Join-Path $outputDirectory "Interop.ShockwaveFlashObjects.dll"

foreach ($requiredPath in @($compiler, $source, $manifest, $axInterop, $interop, (Join-Path $outputDirectory "Flash.ocx"))) {
    if (-not (Test-Path -LiteralPath $requiredPath)) {
        throw "Required smoke-test input was not found: $requiredPath"
    }
}

& $compiler /nologo /target:exe /platform:x86 /reference:System.Windows.Forms.dll "/reference:$axInterop" "/reference:$interop" "/win32manifest:$manifest" "/out:$smokeExecutable" $source
if ($LASTEXITCODE -ne 0) {
    throw "Compiling the Flash activation smoke test failed with exit code $LASTEXITCODE."
}

try {
    & $smokeExecutable
    if ($LASTEXITCODE -ne 0) {
        throw "App-local Flash activation failed with exit code $LASTEXITCODE."
    }
}
finally {
    if (Test-Path -LiteralPath $smokeExecutable) {
        Remove-Item -LiteralPath $smokeExecutable -Force
    }
}
