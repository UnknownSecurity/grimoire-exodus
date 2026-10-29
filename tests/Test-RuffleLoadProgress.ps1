[CmdletBinding()]
param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug",

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
$source = Join-Path $PSScriptRoot "RuffleLoadProgressSmoke.cs"
$client = Join-Path $outputDirectory "Grimoire Exodus.exe"
$clientProbe = Join-Path $outputDirectory "Grimoire.dll"
$smokeExecutable = Join-Path $outputDirectory "RuffleLoadProgressSmoke-$PID.exe"
$swf = if ([string]::IsNullOrWhiteSpace($SwfPath)) {
    Join-Path $outputDirectory "grimoire-exodus.swf"
}
else {
    (Resolve-Path -LiteralPath $SwfPath).Path
}

$references = @(
    "System.dll",
    "System.Drawing.dll",
    "System.Windows.Forms.dll",
    $client,
    (Join-Path $outputDirectory "AxInterop.ShockwaveFlashObjects.dll"),
    (Join-Path $outputDirectory "Microsoft.Web.WebView2.Core.dll"),
    (Join-Path $outputDirectory "Microsoft.Web.WebView2.WinForms.dll")
)
foreach ($requiredPath in @(
    $compiler,
    $source,
    $client,
    $swf,
    (Join-Path $outputDirectory "Ruffle\ruffle.js"),
    (Join-Path $outputDirectory "Resources\Ruffle\index.html")
) + $references | Where-Object { $_ -notmatch '^System\.' }) {
    if (-not (Test-Path -LiteralPath $requiredPath)) {
        throw "Required Ruffle load-progress input was not found: $requiredPath"
    }
}
if (Test-Path -LiteralPath $clientProbe) {
    throw "The smoke test will not overwrite the existing assembly probe: $clientProbe"
}

Copy-Item -LiteralPath $client -Destination $clientProbe
try {
    $referenceArguments = $references | ForEach-Object { "/reference:$_" }
    & $compiler /nologo /target:exe /platform:x86 $referenceArguments "/out:$smokeExecutable" $source
    if ($LASTEXITCODE -ne 0) {
        throw "Compiling the Ruffle load-progress smoke test failed with exit code $LASTEXITCODE."
    }

    Push-Location $outputDirectory
    try {
        & $smokeExecutable $swf
        if ($LASTEXITCODE -ne 0) {
            throw "Ruffle load-progress smoke test failed with exit code $LASTEXITCODE."
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
