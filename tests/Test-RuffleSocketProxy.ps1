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
$csharpCompiler = Join-Path $env:WINDIR "Microsoft.NET\Framework\v4.0.30319\csc.exe"
$csharpSource = Join-Path $PSScriptRoot "RuffleSocketProxySmoke.cs"
$smokeExecutable = Join-Path $outputDirectory "RuffleSocketProxySmoke-$PID.exe"
$client = Join-Path $outputDirectory "Grimoire Exodus.exe"
$references = @(
    "System.dll"
)

foreach ($requiredPath in @(
    $csharpCompiler,
    $csharpSource,
    $client
) + $references | Where-Object { $_ -notmatch '^System\.' }) {
    if (-not (Test-Path -LiteralPath $requiredPath -PathType Leaf)) {
        throw "Required Ruffle socket-proxy smoke-test input was not found: $requiredPath"
    }
}
try {
    $referenceArguments = $references | ForEach-Object { "/reference:$_" }
    & $csharpCompiler /nologo /target:exe /platform:x86 $referenceArguments "/out:$smokeExecutable" $csharpSource
    if ($LASTEXITCODE -ne 0) {
        throw "Compiling the Ruffle socket-proxy smoke test failed with exit code $LASTEXITCODE."
    }

    Push-Location $outputDirectory
    try {
        & $smokeExecutable
        if ($LASTEXITCODE -ne 0) {
            throw "Ruffle socket-proxy smoke test failed with exit code $LASTEXITCODE."
        }
    }
    finally {
        Pop-Location
    }
}
finally {
    foreach ($temporaryPath in @($smokeExecutable)) {
        if (Test-Path -LiteralPath $temporaryPath) {
            Remove-Item -LiteralPath $temporaryPath -Force
        }
    }
}
