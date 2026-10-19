[CmdletBinding()]
param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$rootDirectory = Split-Path -Parent $PSScriptRoot
$compiler = Join-Path $env:WINDIR "Microsoft.NET\Framework\v4.0.30319\csc.exe"
$testSource = Join-Path $PSScriptRoot "SupportDiagnosticsSmoke.cs"
$diagnosticsSource = Join-Path $rootDirectory "app\Grimoire.Utils\SupportDiagnostics.cs"
$testDirectory = Join-Path $PSScriptRoot (".support-diagnostics-test-" + [Guid]::NewGuid().ToString("N"))
$smokeExecutable = Join-Path $testDirectory "SupportDiagnosticsSmoke.exe"

foreach ($requiredPath in @($compiler, $testSource, $diagnosticsSource)) {
    if (-not (Test-Path -LiteralPath $requiredPath)) {
        throw "Required support-diagnostics test input was not found: $requiredPath"
    }
}

try {
    New-Item -ItemType Directory -Path $testDirectory | Out-Null
    $env:GRIMOIRE_SUPPORT_TEST_ROOT = $testDirectory

    & $compiler /nologo /target:exe /platform:x86 "/out:$smokeExecutable" $diagnosticsSource $testSource
    if ($LASTEXITCODE -ne 0) {
        throw "Compiling the support-diagnostics test failed with exit code $LASTEXITCODE."
    }

    & $smokeExecutable
    if ($LASTEXITCODE -ne 0) {
        throw "Support diagnostics validation failed with exit code $LASTEXITCODE."
    }
}
finally {
    Remove-Item Env:\GRIMOIRE_SUPPORT_TEST_ROOT -ErrorAction SilentlyContinue
    $resolvedTests = [IO.Path]::GetFullPath($PSScriptRoot).TrimEnd([IO.Path]::DirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar
    $resolvedTestDirectory = [IO.Path]::GetFullPath($testDirectory)
    if ($resolvedTestDirectory.StartsWith($resolvedTests, [StringComparison]::OrdinalIgnoreCase) -and
        [IO.Path]::GetFileName($resolvedTestDirectory).StartsWith(".support-diagnostics-test-", [StringComparison]::Ordinal)) {
        if (Test-Path -LiteralPath $resolvedTestDirectory) {
            Remove-Item -LiteralPath $resolvedTestDirectory -Recurse -Force
        }
    }
}
