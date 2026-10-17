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
$testSource = Join-Path $PSScriptRoot "StartupDiagnosticsSmoke.cs"
$diagnosticsSource = Join-Path $rootDirectory "app\Grimoire.Utils\StartupDiagnostics.cs"
$jsonAssembly = Join-Path $outputDirectory "Newtonsoft.Json.dll"
$smokeExecutable = Join-Path $outputDirectory "StartupDiagnosticsSmoke.exe"
$testDirectory = Join-Path $PSScriptRoot (".diagnostics-test-" + [Guid]::NewGuid().ToString("N"))

foreach ($requiredPath in @($compiler, $testSource, $diagnosticsSource, $jsonAssembly)) {
    if (-not (Test-Path -LiteralPath $requiredPath)) {
        throw "Required startup-diagnostics test input was not found: $requiredPath"
    }
}

& $compiler /nologo /target:exe /platform:x86 "/reference:$jsonAssembly" "/out:$smokeExecutable" $diagnosticsSource $testSource
if ($LASTEXITCODE -ne 0) {
    throw "Compiling the startup-diagnostics test failed with exit code $LASTEXITCODE."
}

try {
    New-Item -ItemType Directory -Path $testDirectory | Out-Null
    $env:GRIMOIRE_DIAGNOSTICS_TEST_ROOT = $testDirectory

    & $smokeExecutable leave-marker
    if ($LASTEXITCODE -ne 0) {
        throw "Creating a simulated unclean session failed with exit code $LASTEXITCODE."
    }

    & $smokeExecutable recover
    if ($LASTEXITCODE -ne 0) {
        throw "Startup diagnostics recovery failed with exit code $LASTEXITCODE."
    }
}
finally {
    Remove-Item Env:\GRIMOIRE_DIAGNOSTICS_TEST_ROOT -ErrorAction SilentlyContinue
    if (Test-Path -LiteralPath $smokeExecutable) {
        Remove-Item -LiteralPath $smokeExecutable -Force
    }

    $resolvedTests = [IO.Path]::GetFullPath($PSScriptRoot).TrimEnd([IO.Path]::DirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar
    $resolvedTestDirectory = [IO.Path]::GetFullPath($testDirectory)
    if ($resolvedTestDirectory.StartsWith($resolvedTests, [StringComparison]::OrdinalIgnoreCase) -and
        [IO.Path]::GetFileName($resolvedTestDirectory).StartsWith(".diagnostics-test-", [StringComparison]::Ordinal)) {
        if (Test-Path -LiteralPath $resolvedTestDirectory) {
            Remove-Item -LiteralPath $resolvedTestDirectory -Recurse -Force
        }
    }
}
