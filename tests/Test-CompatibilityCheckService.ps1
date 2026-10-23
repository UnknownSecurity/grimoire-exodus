[CmdletBinding()]
param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug"
)

$ErrorActionPreference = "Stop"
$rootDirectory = Split-Path -Parent $PSScriptRoot
$vswhere = Join-Path ${env:ProgramFiles(x86)} "Microsoft Visual Studio\Installer\vswhere.exe"
$compiler = $null
if (Test-Path -LiteralPath $vswhere) {
    $visualStudio = (& $vswhere -latest -products * -requires Microsoft.Component.MSBuild -property installationPath | Select-Object -First 1)
    if (-not [string]::IsNullOrWhiteSpace($visualStudio)) {
        $roslynCompiler = Join-Path $visualStudio "MSBuild\Current\Bin\Roslyn\csc.exe"
        if (Test-Path -LiteralPath $roslynCompiler) {
            $compiler = $roslynCompiler
        }
    }
}
if ([string]::IsNullOrWhiteSpace($compiler)) {
    throw "A Visual Studio Roslyn compiler is required for the compatibility-check test. Install the .NET desktop build tools."
}
$serviceSource = Join-Path $rootDirectory "app\Grimoire.Utils\CompatibilityCheckService.cs"
$testSource = Join-Path $PSScriptRoot "CompatibilityCheckServiceSmoke.cs"
$testDirectory = Join-Path $PSScriptRoot (".compatibility-check-test-" + [Guid]::NewGuid().ToString("N"))
$smokeExecutable = Join-Path $testDirectory "CompatibilityCheckServiceSmoke.exe"

foreach ($requiredPath in @($compiler, $serviceSource, $testSource)) {
    if (-not (Test-Path -LiteralPath $requiredPath)) {
        throw "Required compatibility-check test input was not found: $requiredPath"
    }
}

try {
    New-Item -ItemType Directory -Path $testDirectory | Out-Null
    & $compiler /nologo /target:exe /platform:x86 /langversion:7.3 /reference:System.Net.Http.dll "/out:$smokeExecutable" $serviceSource $testSource
    if ($LASTEXITCODE -ne 0) {
        throw "Compiling the compatibility-check test failed with exit code $LASTEXITCODE."
    }

    & $smokeExecutable
    if ($LASTEXITCODE -ne 0) {
        throw "Compatibility-check validation failed with exit code $LASTEXITCODE."
    }
}
finally {
    $resolvedTests = [IO.Path]::GetFullPath($PSScriptRoot).TrimEnd([IO.Path]::DirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar
    $resolvedTestDirectory = [IO.Path]::GetFullPath($testDirectory)
    if ($resolvedTestDirectory.StartsWith($resolvedTests, [StringComparison]::OrdinalIgnoreCase) -and
        [IO.Path]::GetFileName($resolvedTestDirectory).StartsWith(".compatibility-check-test-", [StringComparison]::Ordinal)) {
        if (Test-Path -LiteralPath $resolvedTestDirectory) {
            Remove-Item -LiteralPath $resolvedTestDirectory -Recurse -Force
        }
    }
}
