[CmdletBinding()]
param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug",

    [ValidateSet("Automatic", "Direct", "Gpu")]
    [string]$RenderingProfile = "Automatic",

    [ValidateSet("Automatic", "High", "Medium", "Low")]
    [string]$RenderQuality = "Automatic",

    [switch]$PropertyOnly,

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
$source = Join-Path $PSScriptRoot "RenderingProfileSmoke.cs"
$manifest = Join-Path $rootDirectory "app\Grimoire.manifest"
$smokeExecutable = Join-Path $outputDirectory "RenderingProfileSmoke-$PID.exe"
$axInterop = Join-Path $outputDirectory "AxInterop.ShockwaveFlashObjects.dll"
$interop = Join-Path $outputDirectory "Interop.ShockwaveFlashObjects.dll"
$swf = Join-Path $outputDirectory "grimoire-exodus.swf"

foreach ($requiredPath in @($compiler, $source, $manifest, $axInterop, $interop, $swf, (Join-Path $outputDirectory "Flash.ocx"))) {
    if (-not (Test-Path -LiteralPath $requiredPath)) {
        throw "Required rendering-profile smoke-test input was not found: $requiredPath"
    }
}

& $compiler /nologo /target:exe /platform:x86 /reference:System.dll /reference:System.Windows.Forms.dll /reference:System.Xml.Linq.dll "/reference:$axInterop" "/reference:$interop" "/win32manifest:$manifest" "/out:$smokeExecutable" $source
if ($LASTEXITCODE -ne 0) {
    throw "Compiling the rendering-profile smoke test failed with exit code $LASTEXITCODE."
}

try {
    $probeArguments = @($swf, $RenderingProfile, $RenderQuality)
    if ($PropertyOnly) {
        $probeArguments += "property-only"
    }
    & $smokeExecutable @probeArguments
    if ($LASTEXITCODE -ne 0) {
        throw "Rendering-profile smoke test failed with exit code $LASTEXITCODE."
    }
}
finally {
    if (Test-Path -LiteralPath $smokeExecutable) {
        Remove-Item -LiteralPath $smokeExecutable -Force
    }
}
