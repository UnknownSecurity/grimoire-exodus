[CmdletBinding()]
param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug",

    [switch]$ShowFlash,

    [string]$SwfPath,

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
$source = Join-Path $PSScriptRoot "FlashLoadProgressSmoke.cs"
$manifest = Join-Path $rootDirectory "app\Grimoire.manifest"
$swf = if ([string]::IsNullOrWhiteSpace($SwfPath)) {
    Join-Path $outputDirectory "grimoire-exodus.swf"
}
else {
    (Resolve-Path -LiteralPath $SwfPath).Path
}
$smokeExecutable = Join-Path $outputDirectory "FlashLoadProgressSmoke.exe"
$axInterop = Join-Path $outputDirectory "AxInterop.ShockwaveFlashObjects.dll"
$interop = Join-Path $outputDirectory "Interop.ShockwaveFlashObjects.dll"

foreach ($requiredPath in @($compiler, $source, $manifest, $swf, $axInterop, $interop, (Join-Path $outputDirectory "Flash.ocx"))) {
    if (-not (Test-Path -LiteralPath $requiredPath)) {
        throw "Required Flash load-progress input was not found: $requiredPath"
    }
}

& $compiler /nologo /target:exe /platform:x86 /reference:System.dll /reference:System.Windows.Forms.dll /reference:System.Xml.Linq.dll "/reference:$axInterop" "/reference:$interop" "/win32manifest:$manifest" "/out:$smokeExecutable" $source
if ($LASTEXITCODE -ne 0) {
    throw "Compiling the Flash load-progress smoke test failed with exit code $LASTEXITCODE."
}

try {
    if ($ShowFlash) {
        & $smokeExecutable $swf --visible
    }
    else {
        & $smokeExecutable $swf
    }
    if ($LASTEXITCODE -ne 0) {
        throw "Flash load-progress smoke test failed with exit code $LASTEXITCODE."
    }
}
finally {
    if (Test-Path -LiteralPath $smokeExecutable) {
        Remove-Item -LiteralPath $smokeExecutable -Force
    }
}
